using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.Services;
using RunningApp.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace RunningApp.IntegrationTests.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DIST-GEN.2 — dark-only 16K x INTERMEDIATE x 4D target-distance
/// projection. Every test here reaches the request pipeline ONLY through the
/// internal <see cref="GeneratePreviewRequest.TargetDistanceKmOverride"/> seam
/// (never through any public wire DTO, which carries no such field) —
/// mirroring the same "production-owned but not publicly reachable" pattern
/// <see cref="V1CatalogPilotIdentityPolicy"/>'s own internal 3-arg
/// <c>ResolveCandidate</c> overload established for HM's own dark-only 5D/6D
/// candidates.
/// </summary>
public sealed class Dark16KTargetDistanceProjectionTests
{
    private readonly ITestOutputHelper _output;
    public Dark16KTargetDistanceProjectionTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // ── §6 Pilot eligibility gate ────────────────────────────────────────────

    [Theory]
    [InlineData(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, true)]
    [InlineData(12.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(20.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(16.0, GoalDistance.HalfMarathon, RunningBackground.Beginner, 4, false)]
    [InlineData(16.0, GoalDistance.HalfMarathon, RunningBackground.Advanced, 4, false)]
    [InlineData(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3, false)]
    [InlineData(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5, false)]
    [InlineData(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 6, false)]
    [InlineData(16.0, GoalDistance.TenK, RunningBackground.Intermediate, 4, false)]
    [InlineData(null, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    public void EligibilityGate_OnlyExactFrozenTripleIsEligible(
        double? targetDistanceKm, GoalDistance goalDistance, RunningBackground level, int daysPerWeek, bool expectedEligible)
    {
        Assert.Equal(expectedEligible, Dark16KPilotEligibilityPolicy.IsEligible(targetDistanceKm, goalDistance, level, daysPerWeek));
    }

    [Fact]
    public void EligibilityGate_CandidateShapedOverload_OnlyExactTripleEligible()
    {
        Assert.True(Dark16KPilotEligibilityPolicy.IsEligible(16.0, "HALF_MARATHON", "INTERMEDIATE", 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsEligible(12.0, "HALF_MARATHON", "INTERMEDIATE", 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsEligible(16.0, "TEN_K", "INTERMEDIATE", 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsEligible(16.0, "HALF_MARATHON", "ADVANCED", 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsEligible(16.0, "HALF_MARATHON", "INTERMEDIATE", 5));
    }

    // ── §5 Distance resolution: CanonicalDistanceFamilyResolver ─────────────

    [Theory]
    [InlineData(16.0, GoalDistance.HalfMarathon)]
    [InlineData(12.0, GoalDistance.HalfMarathon)]
    [InlineData(15.0, GoalDistance.HalfMarathon)]
    [InlineData(18.0, GoalDistance.HalfMarathon)]
    [InlineData(20.0, GoalDistance.HalfMarathon)]
    [InlineData(9.5, GoalDistance.TenK)]
    public void CanonicalDistanceFamilyResolver_ClassifiesButNeverGrantsEligibilityAlone(double km, GoalDistance expectedFamily)
    {
        var resolver = new CanonicalDistanceFamilyResolver();
        var resolution = resolver.Resolve(km);
        Assert.Equal(expectedFamily, resolution.CanonicalDistanceFamily);
        // Preserves the exact requested value, never rounds to the family's own canonical constant.
        Assert.Equal(km, resolution.RequestedTargetDistanceKm);

        // Classification alone never implies dark-materialization eligibility --
        // only the exact (16.0, Intermediate, 4) triple is eligible, per §6.
        var eligible = Dark16KPilotEligibilityPolicy.IsEligible(km, expectedFamily, RunningBackground.Intermediate, 4);
        Assert.Equal(km == 16.0 && expectedFamily == GoalDistance.HalfMarathon, eligible);
    }

    // ── §16 Peak-volume/taper/long-run authority (VolumeSafetyPolicy) ───────

    [Fact]
    public void VolumeSafetyPolicy_16K_EncodesExactFrozenValues()
    {
        var policy = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K;
        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(23.5d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(40.0d, policy.ResolvedPeakReference.Value);
        Assert.Equal(9, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.ResolvedTaperVolumeMultipliers);
        Assert.Equal(0.30d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.33d, policy.LongRunSelectionShare);
        Assert.Equal(0.40d, policy.LongRunHardCapShare);
        Assert.Equal(15.0d, policy.PreferredAbsolutePeakLongRunKm);
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);

        // Zero-delta: canonical HM policy is untouched.
        var canonical = VolumeSafetyPolicy.HalfMarathonIntermediate4D;
        Assert.Equal(43d, canonical.ResolvedPeakReference.Value);
        Assert.Equal(25d, canonical.GoldenFixtureStartingVolumeKm);
        Assert.Equal(19d, canonical.PreferredAbsolutePeakLongRunKm);
    }

    [Fact]
    public void PeakVolumeBandPolicy_16K_EncodesFrozenBand_DistinctFromCanonicalHmBand()
    {
        Assert.Equal(34.0d, TargetDistance16KPeakVolumeBandPolicy.MinimumKm);
        Assert.Equal(46.0d, TargetDistance16KPeakVolumeBandPolicy.MaximumKm);
        var band = TargetDistance16KPeakVolumeBandPolicy.Build("HALF_MARATHON", "INTERMEDIATE", 4, "PEAK_VOLUME_BAND_POLICY", 10);
        Assert.Equal(34.0d, band.MinimumKm);
        Assert.Equal(46.0d, band.MaximumKm);
    }

    // ── §9 Horizon authority: explicit, not TEN_K's fallback arm ────────────

    [Fact]
    public void HorizonPolicy_16K_ExactFrozenBounds()
    {
        // PHASE DIST-GEN.2A/2B: MinimumCoreWeeks corrected from 8 to 10 --
        // 8-9W are below the reused HALF_MARATHON catalog identity's own real
        // phase-allocation minimum-week sum and are deferred to a future
        // compressed-core track. PreferredCoreWeeks/MaximumCoreWeeks unchanged.
        Assert.Equal(10, TargetDistance16KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(12, TargetDistance16KHorizonPolicy.PreferredCoreWeeks);
        Assert.Equal(14, TargetDistance16KHorizonPolicy.MaximumCoreWeeks);
    }

    [Theory]
    [InlineData(7, RaceHorizonClassification.BelowMinimum)]
    [InlineData(8, RaceHorizonClassification.BelowMinimum)]
    [InlineData(9, RaceHorizonClassification.BelowMinimum)]
    [InlineData(10, RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(12, RaceHorizonClassification.ExactStandaloneCoreSupported)]
    [InlineData(14, RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(15, RaceHorizonClassification.CompositionRequired)]
    public void HorizonPolicy_16K_ClassifiesExactlyAtItsOwnFrozenBoundaries(int weeks, RaceHorizonClassification expected)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(weeks * 7); // exact N-week-0-day boundary (elapsedDays = weeks*7, remainder 0)
        var classification = TargetDistance16KHorizonPolicy.Classify(start, race);
        Assert.Equal(expected, classification);
    }

    [Fact]
    public void HorizonPolicy_16K_ReasonCode_IsDistinctFromTenKAndHalfMarathonOwnCodes()
    {
        var reasonCode = TargetDistance16KHorizonPolicy.GetUnsupportedReasonCode(7);
        Assert.Equal("DARK_16K_CORE_HORIZON_7_NOT_SUPPORTED", reasonCode);
        // TEN_K's own reason-code shape is deliberately different text (CORE_HORIZON_*_NOT_IMPLEMENTED),
        // proving these are two distinct, independently-traceable authorities, not the same call site.
        var tenKReasonCode = RaceHorizonPolicy.GetCoreHorizonUnsupportedReasonCode(7);
        Assert.NotEqual(reasonCode, tenKReasonCode);
        Assert.DoesNotContain("DARK_16K", tenKReasonCode);
    }

    // ── §27/§28 Real production pipeline: full PlanServices.GeneratePreviewAsync ──

    private static AppDbContext NewInMemoryContext() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>
    /// Real catalog routing requires CatalogLivePilotOptions.Enabled=true
    /// (defaults false) to route HALF_MARATHON/TEN_K requests to the catalog
    /// engine instead of the legacy SQL engine -- this mirrors how every
    /// other real-catalog-routing test in this project enables it explicitly,
    /// never a dark-16K-specific override.
    /// </summary>
    /// <summary>
    /// Mirrors <see cref="TestPlanServicesFactory.Create"/>'s own internal
    /// wiring exactly, except both the route decider AND the candidate
    /// eligibility gate are given the same LocalCatalogAcceptance/Development
    /// override this repo's own real appsettings.Development.json uses in
    /// dev/test environments (mirroring how HM's own real dev/CI routing
    /// reaches the catalog engine despite the HM candidate's own real
    /// lifecycle status remaining "VALIDATED", not yet "PUBLISHED" -- an
    /// unrelated, pre-existing repo condition this phase does not change).
    /// </summary>
    private static PlanServices CreateCatalogRoutedService(AppDbContext context)
    {
        var localAcceptance = Options.Create(new LocalCatalogAcceptanceOptions
        { Enabled = true, TreatPilotCandidateAsPublished = true, EnableCatalogRoute = true });
        var hostEnv = new FakeDevelopmentHostEnvironment();
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader, localAcceptance, hostEnv);
        var orchestration = new RuntimeConditionResolutionService(
            new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());
        var skeletonOrchestrator = new CatalogPlanSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekContextFactory(), new CatalogStageToWeekMaterializer(),
            new GeneratedCatalogPlanSkeletonValidator());
        var catalogPreviewGenerator = new CatalogPreviewGenerator(gate, orchestration, skeletonOrchestrator);
        var catalogConfirmationService = new CatalogPlanConfirmationService(
            context, NullLogger<CatalogPlanConfirmationService>.Instance, new GeneratedCatalogPlanPayloadValidator());
        var routeDecider = new LivePlanPreviewRoutingService(
            Options.Create(new CatalogLivePilotOptions { Enabled = true }),
            localAcceptance, hostEnv, bundleLoader, NullLogger<LivePlanPreviewRoutingService>.Instance);

        return new PlanServices(
            context,
            new RunningApp.Application.PlanGeneration.PlaceholderPlanGenerationEngine(context, NullLogger<RunningApp.Application.PlanGeneration.PlaceholderPlanGenerationEngine>.Instance),
            NullLogger<PlanServices>.Instance,
            routeDecider,
            catalogPreviewGenerator,
            catalogConfirmationService,
            Options.Create(OptionsValue),
            Options.Create(new PreparationRunwayPilotActivationOptions { Enabled = true }));
    }

    private sealed class FakeDevelopmentHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "RunningApp.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static GeneratePreviewRequest Dark16KRequest(int targetWeekCount, DateOnly start) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.HalfMarathon,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = start,
        RaceDate = start.AddDays(targetWeekCount * 7), // exact N-week-0-day boundary
        TargetFinishTimeSeconds = 5400,
        TargetFinishTimeSource = TargetFinishTimeSource.UserDefined,
        TargetDistanceKmOverride = 16.0,
        PreferredDays = [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun],
        LongRunDay = Weekday.Sun,
        RecentWeeklyVolumeKm = 20,
        RecentLongestRunKm = 10,
        RecentRunsPerWeek = 4,
        RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 2700, RaceDate = start.AddDays(-21) },
    };

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task RealPipeline_EligibleHorizons_MaterializeSuccessfully(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KRequest(weeks, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        Assert.NotNull(response);
        _output.WriteLine($"{weeks}W dark 16K preview materialized OK.");
    }

    /// <summary>
    /// PHASE DIST-GEN.2A/2B: proves the horizon GATE (TargetDistance16KHorizonPolicy,
    /// now 10/12/14) correctly REJECTS an 8-week request itself, before ever
    /// reaching phase allocation. This supersedes DIST-GEN.2's own
    /// <c>RealPipeline_EightWeekHorizon_PassesHorizonGate_ButFailsPhaseAllocation_ForCatalogReuseReasons</c>
    /// test (removed), which proved the OPPOSITE under the since-corrected
    /// MinimumCoreWeeks=8 authority: that 8W passed the horizon gate and only
    /// failed later, downstream, at phase allocation
    /// (<c>DynamicCoreWeekSkeletonInfeasibleException: TARGET_BELOW_SUM_OF_MINIMUMS</c>,
    /// sumOfMinimums=10). DIST-GEN.2A corrected the horizon authority itself
    /// to 10 so that this now-known-infeasible case is rejected at the
    /// correct, earlier layer with the correct, attributable reason code
    /// (see <see cref="RealPipeline_BelowMinimumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds"/>,
    /// which now covers 7W/8W/9W).
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public async Task RealPipeline_BelowMinimumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KRequest(weeks, start);

        var ex = await Assert.ThrowsAsync<PlanCoreHorizonTooShortException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_16K_CORE_HORIZON", ex.Message);
        Assert.Contains("dark 16K target-distance", ex.Message);
        // Never HALF_MARATHON's own message shape (which cites its real 10-week minimum).
        Assert.DoesNotContain("HALF_MARATHON standalone core minimum (10", ex.Message);
    }

    [Theory]
    [InlineData(15)]
    public async Task RealPipeline_AboveMaximumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KRequest(weeks, start);

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_16K_CORE_HORIZON", ex.Message);
        Assert.IsNotType<PlanHorizonStartTooEarlyException>(ex);
    }

    [Fact]
    public async Task RealPipeline_ZeroDelta_CanonicalHalfMarathon14W_StillSucceeds_NoOverride()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KRequest(14, start);
        request.TargetDistanceKmOverride = null; // canonical HM request, no dark override

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task RealPipeline_ZeroDelta_CanonicalHalfMarathon_TooShortForItsOwnBounds_StillUsesHmOwnMinimum()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KRequest(8, start); // 8W is below HM's OWN 10W minimum
        request.TargetDistanceKmOverride = null; // canonical -- must use HM's real 10/14/16, not 8/12/14

        var ex = await Assert.ThrowsAsync<PlanCoreHorizonTooShortException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("HALF_MARATHON standalone core minimum (10", ex.Message);
        Assert.DoesNotContain("DARK_16K", ex.Message);
    }

    // ── §26/§17 Real production volume/taper materialization at the CatalogVolumeAndLongRunPlanner layer ──

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateVersion);
    }

    private static DynamicCoreCalendarMaterializationOrchestrator Pipeline() => new(
        new DynamicCoreSessionPrescriptionOrchestrator(
            new DynamicCoreVolumeAndLongRunOrchestrator(
                new DynamicCoreWorkoutBindingOrchestrator(
                    new DynamicCoreWeekSkeletonOrchestrator(
                        new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                        new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                    new CatalogWorkoutProgressionLoader(Options.Create(OptionsValue)),
                    new ProgressionStageAllocator(), new GeneratedCatalogStageScheduleValidator(),
                    new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator(),
                    new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
            new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));

    private static DynamicCoreCalendarMaterializationContext Context(PlanCatalogCandidateSummary candidate, int targetWeekCount)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var request = Dark16KRequest(targetWeekCount, start);

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults =
            [
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME"),
            ],
            PreviewRequest = request,
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = 16.0, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon),
                StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = null, DaysPerWeek = 4, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new TargetDistance16KAwarePeakVolumeBandLoaderTestAccessor(Options.Create(OptionsValue)),
        };
    }

    [Fact]
    public async Task GoldenTrace_12W_MaterializesWithFrozen16KNumericsNotHmNumerics()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 12);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(12, prescribed.Weeks.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count));

        // Peak volume must reflect the pilot's own 40.0km reference, never HM's own 43.0km.
        Assert.True(volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm) <= 40.0d + 0.001d);
        Assert.NotEqual(43.0d, volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm));

        // Long run never reaches/exceeds the 16.0km target distance, and never exceeds the 15.0km ceiling.
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.PlannedLongRunDistanceKm < 16.0d);
            Assert.True(w.PlannedLongRunDistanceKm <= 15.0d + 0.001d);
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d);
        });

        // Non-chained taper: both weeks computed from the fixed 40.0km anchor, never W2 = W1 x 0.43.
        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        var w1 = taperWeeks[0].PlannedWeeklyVolumeKm;
        var w2 = taperWeeks[1].PlannedWeeklyVolumeKm;
        Assert.True(Math.Abs(w2 - w1 * 0.43d) > 0.5d, "Taper W2 must not equal W1 x 0.43 (chained) -- must be anchor x 0.43.");

        foreach (var week in volume.WeeklyVolumePlan.Weeks)
        {
            var lr = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var phaseWeek = prescribed.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            _output.WriteLine($"W{week.WeekNumber:D2}|{phaseWeek.PhaseKey}|vol={week.PlannedWeeklyVolumeKm:F1}|lr={lr.PlannedLongRunDistanceKm:F1}|lrShare={lr.LongRunShareOfWeeklyVolume:P1}|taper={week.IsTaperWeek}");
        }
    }

    /// <summary>
    /// PHASE DIST-GEN.2B: new lower-boundary golden trace, added because
    /// DIST-GEN.2A corrected MinimumCoreWeeks from 8 to 10, making 10W the
    /// new minimum-boundary success case for the dark 16K Standard Core
    /// track (mirrors <see cref="GoldenTrace_12W_MaterializesWithFrozen16KNumericsNotHmNumerics"/>'s
    /// own style). Confirms family/target identity coexist correctly and the
    /// real phase split at this new boundary is FOUNDATION=2/BUILD=3/
    /// RACE_SPECIFIC=3/TAPER=2, inherited unchanged from the reused
    /// HALF_MARATHON catalog identity's own real minimum-week allocation.
    /// </summary>
    [Fact]
    public async Task GoldenTrace_10W_LowerBoundary_MaterializesWithFrozen16KNumericsNotHmNumerics()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 10);
        Assert.Equal("HALF_MARATHON", context.ResolverInput.CanonicalDistanceFamily);
        Assert.Equal(16.0, context.ResolverInput.RequestedTargetDistanceKm);

        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(10, prescribed.Weeks.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count));

        // Real phase split at the new 10W minimum boundary: 2/3/3/2, inherited
        // unchanged from the reused HALF_MARATHON catalog identity's own
        // real minimum-week phase-allocation sum.
        var phaseCounts = prescribed.Weeks.GroupBy(w => w.PhaseKey).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(2, phaseCounts["FOUNDATION"]);
        Assert.Equal(3, phaseCounts["BUILD"]);
        Assert.Equal(3, phaseCounts["RACE_SPECIFIC"]);
        Assert.Equal(2, phaseCounts["TAPER"]);

        // Peak volume must reflect the pilot's own 40.0km reference, never HM's own 43.0km.
        Assert.True(volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm) <= 40.0d + 0.001d);
        Assert.NotEqual(43.0d, volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm));

        // Long run never reaches/exceeds the 16.0km target distance, and never exceeds the 15.0km ceiling.
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.PlannedLongRunDistanceKm < 16.0d);
            Assert.True(w.PlannedLongRunDistanceKm <= 15.0d + 0.001d);
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d);
        });

        // Non-chained taper: both weeks computed from the fixed anchor, never W2 = W1 x 0.43.
        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        var w1 = taperWeeks[0].PlannedWeeklyVolumeKm;
        var w2 = taperWeeks[1].PlannedWeeklyVolumeKm;
        Assert.True(Math.Abs(w2 - w1 * 0.43d) > 0.5d, "Taper W2 must not equal W1 x 0.43 (chained) -- must be anchor x 0.43.");

        foreach (var week in volume.WeeklyVolumePlan.Weeks)
        {
            var lr = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var phaseWeek = prescribed.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            _output.WriteLine($"W{week.WeekNumber:D2}|{phaseWeek.PhaseKey}|vol={week.PlannedWeeklyVolumeKm:F1}|lr={lr.PlannedLongRunDistanceKm:F1}|lrShare={lr.LongRunShareOfWeeklyVolume:P1}|taper={week.IsTaperWeek}");
        }
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task TwoOfThreeHorizons_MaterializeWithinFrozenPeakAndNeverForcePeak(int weeks)
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: weeks);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join(";", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm) <= 40.0d + 0.001d);
        // Weekly deltas respect the 2.5km cap.
        var ordered = volume.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        for (var i = 1; i < ordered.Length; i++)
        {
            if (!ordered[i].IsTaperWeek && !ordered[i - 1].IsTaperWeek)
            {
                Assert.True(ordered[i].PlannedWeeklyVolumeKm - ordered[i - 1].PlannedWeeklyVolumeKm <= 2.5d + 0.001d);
            }
        }
    }

    /// <summary>
    /// GENUINE FINDING, now HISTORICAL CONTEXT for the DIST-GEN.2A correction:
    /// the reused, unmodified HALF_MARATHON__4D__INTERMEDIATE catalog
    /// identity's own phase-allocation minimum-week sum
    /// (Foundation+Build+RaceSpecific+Taper minimums, as authored in
    /// HALF_MARATHON_MASTER's phase-allocation policy) is 10 weeks --
    /// inherited unchanged from HALF_MARATHON's own real
    /// MinimumSupportedStandaloneWeeks, per DIST-GEN.1's own catalog-identity-reuse
    /// constraint (§6: zero catalog file changes). This test calls the
    /// low-level materialization pipeline DIRECTLY, bypassing
    /// <c>PlanServices.GeneratePreviewAsync</c>'s own horizon gate entirely,
    /// so it still demonstrates this real, unchanged allocator fact even
    /// after DIST-GEN.2A/2B corrected <see cref="TargetDistance16KHorizonPolicy.MinimumCoreWeeks"/>
    /// from 8 to 10. In the REAL production pipeline (see
    /// <see cref="RealPipeline_BelowMinimumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds"/>),
    /// an 8-week request is now rejected earlier, by the horizon gate itself
    /// (<c>PlanCoreHorizonTooShortException</c>), and this allocator-level
    /// exception is no longer the user/caller-visible failure mode for 8W --
    /// it remains true only for a caller that bypasses the horizon gate, as
    /// this direct-orchestrator test deliberately does.
    /// </summary>
    [Fact]
    public async Task EightWeekHorizon_CannotPhaseAllocate_BecauseReusedCatalogIdentityOwnMinimumSumIsTen()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 8);

        var ex = await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() => Pipeline().MaterializeAsync(context));
        Assert.Contains("TARGET_BELOW_SUM_OF_MINIMUMS", ex.Message);
        Assert.Contains("sumOfMinimums=10", ex.Message);
    }

    // ── §30 Persistence proof: family + target distance coexist on TrainingPlan ──

    [Fact]
    public async Task RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KRequest(12, start);

        var internalUserId = Guid.NewGuid();
        var previewResponse = await service.GeneratePreviewAsync(internalUserId, request);
        Assert.NotNull(previewResponse);

        var previewRow = await context.PlanPreviews.AsNoTracking().FirstOrDefaultAsync(p => p.Id == previewResponse.PreviewId);
        Assert.NotNull(previewRow);
        _output.WriteLine($"Preview persisted: id={previewRow!.Id}, generationSource in payload: {previewRow.PreviewPayloadJson[..Math.Min(200, previewRow.PreviewPayloadJson.Length)]}");

        var confirmResponse = await service.ConfirmPlanAsync(
            internalUserId,
            new ConfirmPlanRequest { PreviewId = previewResponse.PreviewId });

        Assert.NotNull(confirmResponse);
        var persistedPlan = await context.TrainingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == confirmResponse.PlanId);
        Assert.NotNull(persistedPlan);

        _output.WriteLine($"Persisted TrainingPlan: CanonicalDistanceFamily={persistedPlan!.CanonicalDistanceFamily}, RequestedTargetDistanceKm={persistedPlan.RequestedTargetDistanceKm}, GoalDistanceKm={persistedPlan.GoalDistanceKm}");

        Assert.Equal("HALF_MARATHON", persistedPlan.CanonicalDistanceFamily);
        Assert.Equal(16.0, persistedPlan.RequestedTargetDistanceKm);
        // GoalDistanceKm stays the family-representative constant -- the two fields never collapse into each other.
        Assert.NotEqual(persistedPlan.RequestedTargetDistanceKm, persistedPlan.GoalDistanceKm);
    }
}

/// <summary>
/// Test-only accessor mirroring the production <see cref="TargetDistance16KAwarePeakVolumeBandLoader"/>
/// decision for direct-orchestrator tests that bypass <see cref="CatalogPreviewGenerator"/>
/// (and therefore never see its own internally-constructed decorator instance).
/// Delegates to the exact same production <see cref="TargetDistance16KPeakVolumeBandPolicy"/>
/// used by the real decorator -- no numeric authority is duplicated here.
/// </summary>
internal sealed class TargetDistance16KAwarePeakVolumeBandLoaderTestAccessor : ICatalogPeakVolumeBandLoader
{
    private readonly ICatalogPeakVolumeBandLoader _inner;
    public TargetDistance16KAwarePeakVolumeBandLoaderTestAccessor(IOptions<PlanCatalogOptions> options) => _inner = new CatalogPeakVolumeBandLoader(options);

    public Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default) =>
        Task.FromResult(TargetDistance16KPeakVolumeBandPolicy.Build(distanceFamily, experience, runsPerWeek, reference.Key, reference.Version));
}
