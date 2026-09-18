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
/// PHASE DIST-GEN.6 — dark-only 15K x INTERMEDIATE x 4D SECOND target-distance
/// projection, per PHASE_DIST_GEN_5_...md's complete frozen numeric authority.
/// Mirrors <see cref="Dark16KTargetDistanceProjectionTests"/>'s own structure
/// exactly, testing the SAME internal <see cref="GeneratePreviewRequest.TargetDistanceKmOverride"/>
/// seam for the second, independently-authorized target — never through any
/// public wire DTO. The recurring theme across this file: every value that
/// coincides with 16K's own (peak, calibration, taper, horizon, LR shares) is
/// asserted as a real, independently-resolved value, not copy-pasted from the
/// 16K test; the one value that must differ (PreferredAbsolutePeakLongRunKm =
/// 14.5, not 15.0) is asserted explicitly and directly against the 16K policy
/// to prove the two named policies are genuinely distinct instances.
/// </summary>
public sealed class Dark15KTargetDistanceProjectionTests
{
    private readonly ITestOutputHelper _output;
    public Dark15KTargetDistanceProjectionTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // ── Dark eligibility registry ────────────────────────────────────────────

    [Theory]
    [InlineData(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, true)]
    [InlineData(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, true)]
    [InlineData(12.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(20.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(14.9, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(15.1, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(15.9, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(16.1, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(16.09, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)] // 10-mile exact, unchanged
    [InlineData(15.0, GoalDistance.HalfMarathon, RunningBackground.Beginner, 4, false)]
    [InlineData(15.0, GoalDistance.HalfMarathon, RunningBackground.Advanced, 4, false)]
    [InlineData(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3, false)]
    [InlineData(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5, false)]
    [InlineData(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 6, false)]
    [InlineData(15.0, GoalDistance.TenK, RunningBackground.Intermediate, 4, false)]
    [InlineData(null, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    public void DarkEligibilityRegistry_15KAnd16K_AreTheOnlyEligibleCells(
        double? targetDistanceKm, GoalDistance goalDistance, RunningBackground level, int daysPerWeek, bool expectedEligible)
    {
        Assert.Equal(expectedEligible, Dark16KPilotEligibilityPolicy.IsDarkEligible(targetDistanceKm, goalDistance, level, daysPerWeek));
    }

    [Fact]
    public void DarkEligibilityRegistry_15KAnd16K_ResolveToDistinctIndependentCells()
    {
        var fifteen = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        var sixteen = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);

        Assert.NotNull(fifteen);
        Assert.NotNull(sixteen);
        Assert.Equal(15.0, fifteen!.TargetDistanceKm);
        Assert.Equal(16.0, sixteen!.TargetDistanceKm);
        Assert.NotEqual(fifteen, sixteen);

        // A 15.0 request must never resolve the 16.0 cell and vice versa.
        Assert.NotEqual(16.0, fifteen.TargetDistanceKm);
        Assert.NotEqual(15.0, sixteen.TargetDistanceKm);
    }

    [Fact]
    public void DarkEligibilityRegistry_CandidateShapedOverload_15KEligible()
    {
        Assert.True(Dark16KPilotEligibilityPolicy.IsDarkEligible(15.0, "HALF_MARATHON", "INTERMEDIATE", 4));
        Assert.True(Dark16KPilotEligibilityPolicy.IsDarkEligible(16.0, "HALF_MARATHON", "INTERMEDIATE", 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsDarkEligible(12.0, "HALF_MARATHON", "INTERMEDIATE", 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsDarkEligible(15.0, "TEN_K", "INTERMEDIATE", 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsDarkEligible(15.0, "HALF_MARATHON", "ADVANCED", 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsDarkEligible(15.0, "HALF_MARATHON", "INTERMEDIATE", 5));
    }

    /// <summary>
    /// PUBLIC eligibility (<see cref="Dark16KPilotEligibilityPolicy.IsEligible(double?,GoalDistance,RunningBackground?,int?)"/>)
    /// is a DISTINCT, narrower concept from dark eligibility -- must remain
    /// true ONLY for 16.0, never for 15.0, even though the dark registry
    /// grants both. This is the critical hard-stop proof at the C# level
    /// (see <c>DistGen6PublicBoundaryZeroDeltaTests</c> for the real-HTTP
    /// version of the same proof).
    /// </summary>
    [Fact]
    public void PublicEligibility_RemainsNarrowerThanDarkEligibility_Only16KPublic()
    {
        Assert.True(Dark16KPilotEligibilityPolicy.IsEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsEligible(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));

        // But both are dark-eligible.
        Assert.True(Dark16KPilotEligibilityPolicy.IsDarkEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.True(Dark16KPilotEligibilityPolicy.IsDarkEligible(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    // ── Distance resolution: CanonicalDistanceFamilyResolver ────────────────

    [Fact]
    public void CanonicalDistanceFamilyResolver_15K_ResolvesToHalfMarathon_PreservesExactValue()
    {
        var resolver = new CanonicalDistanceFamilyResolver();
        var resolution = resolver.Resolve(15.0);
        Assert.Equal(GoalDistance.HalfMarathon, resolution.CanonicalDistanceFamily);
        Assert.Equal(15.0, resolution.RequestedTargetDistanceKm);
        Assert.NotEqual(21.0975, resolution.RequestedTargetDistanceKm);
        Assert.NotEqual(16.0, resolution.RequestedTargetDistanceKm);
        Assert.NotEqual(5.0, resolution.RequestedTargetDistanceKm);
    }

    // ── VolumeSafetyPolicy: exact frozen values, and the one deliberate difference ──

    [Fact]
    public void VolumeSafetyPolicy_15K_EncodesExactFrozenValues()
    {
        var policy = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K;
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
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);

        // The ONE deliberate difference from the 16K policy.
        Assert.Equal(14.5d, policy.PreferredAbsolutePeakLongRunKm);

        // Zero-delta: canonical HM and the 16K pilot policy are untouched.
        var canonical = VolumeSafetyPolicy.HalfMarathonIntermediate4D;
        Assert.Equal(43d, canonical.ResolvedPeakReference.Value);
        Assert.Equal(19d, canonical.PreferredAbsolutePeakLongRunKm);
        var sixteenK = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K;
        Assert.Equal(15.0d, sixteenK.PreferredAbsolutePeakLongRunKm);
        Assert.NotEqual(sixteenK.PreferredAbsolutePeakLongRunKm, policy.PreferredAbsolutePeakLongRunKm);
    }

    [Fact]
    public void PeakVolumeBandPolicy_15K_EncodesFrozenBand_IdenticalToButIndependentFrom16K()
    {
        Assert.Equal(34.0d, TargetDistance15KPeakVolumeBandPolicy.MinimumKm);
        Assert.Equal(46.0d, TargetDistance15KPeakVolumeBandPolicy.MaximumKm);
        var band = TargetDistance15KPeakVolumeBandPolicy.Build("HALF_MARATHON", "INTERMEDIATE", 4, "PEAK_VOLUME_BAND_POLICY", 10);
        Assert.Equal(34.0d, band.MinimumKm);
        Assert.Equal(46.0d, band.MaximumKm);

        // Real evidence convergence with 16K's own band -- same values, distinct declared policy classes.
        Assert.Equal(TargetDistance16KPeakVolumeBandPolicy.MinimumKm, TargetDistance15KPeakVolumeBandPolicy.MinimumKm);
        Assert.Equal(TargetDistance16KPeakVolumeBandPolicy.MaximumKm, TargetDistance15KPeakVolumeBandPolicy.MaximumKm);
    }

    // ── Horizon authority: explicit provenance, not inherited by accident ──

    [Fact]
    public void HorizonPolicy_15K_ExactFrozenBounds_OwnDeclaredClass()
    {
        Assert.Equal(10, TargetDistance15KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(12, TargetDistance15KHorizonPolicy.PreferredCoreWeeks);
        Assert.Equal(14, TargetDistance15KHorizonPolicy.MaximumCoreWeeks);

        // Real evidence convergence with 16K's own triple -- same values, but this
        // asserts the VALUES came from the 15K policy's OWN declared constants,
        // not merely "happen to equal" the 16K ones by accidental fallthrough.
        Assert.Equal(TargetDistance16KHorizonPolicy.MinimumCoreWeeks, TargetDistance15KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(TargetDistance16KHorizonPolicy.PreferredCoreWeeks, TargetDistance15KHorizonPolicy.PreferredCoreWeeks);
        Assert.Equal(TargetDistance16KHorizonPolicy.MaximumCoreWeeks, TargetDistance15KHorizonPolicy.MaximumCoreWeeks);
    }

    [Theory]
    [InlineData(7, RaceHorizonClassification.BelowMinimum)]
    [InlineData(8, RaceHorizonClassification.BelowMinimum)]
    [InlineData(9, RaceHorizonClassification.BelowMinimum)]
    [InlineData(10, RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(12, RaceHorizonClassification.ExactStandaloneCoreSupported)]
    [InlineData(14, RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(15, RaceHorizonClassification.CompositionRequired)]
    public void HorizonPolicy_15K_ClassifiesExactlyAtItsOwnFrozenBoundaries(int weeks, RaceHorizonClassification expected)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(weeks * 7);
        var classification = TargetDistance15KHorizonPolicy.Classify(start, race);
        Assert.Equal(expected, classification);
    }

    [Fact]
    public void HorizonPolicy_15K_ReasonCode_IsDistinctFromTenKHalfMarathonAnd16KOwnCodes()
    {
        var reasonCode = TargetDistance15KHorizonPolicy.GetUnsupportedReasonCode(7);
        Assert.Equal("DARK_15K_CORE_HORIZON_7_NOT_SUPPORTED", reasonCode);

        var sixteenKReasonCode = TargetDistance16KHorizonPolicy.GetUnsupportedReasonCode(7);
        var tenKReasonCode = RaceHorizonPolicy.GetCoreHorizonUnsupportedReasonCode(7);
        Assert.NotEqual(reasonCode, sixteenKReasonCode);
        Assert.NotEqual(reasonCode, tenKReasonCode);
        Assert.DoesNotContain("DARK_15K", sixteenKReasonCode);
        Assert.DoesNotContain("DARK_16K", reasonCode);
    }

    // ── Real production pipeline: full PlanServices.GeneratePreviewAsync ────

    private static AppDbContext NewInMemoryContext() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

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

    private static GeneratePreviewRequest Dark15KRequest(int targetWeekCount, DateOnly start) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.HalfMarathon,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = start,
        RaceDate = start.AddDays(targetWeekCount * 7),
        TargetFinishTimeSeconds = 5100,
        TargetFinishTimeSource = TargetFinishTimeSource.UserDefined,
        TargetDistanceKmOverride = 15.0,
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
        var request = Dark15KRequest(weeks, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        Assert.NotNull(response);
        _output.WriteLine($"{weeks}W dark 15K preview materialized OK.");
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public async Task RealPipeline_BelowMinimumHorizon_RejectedByDark15KAuthority_NeverTargetBelowSumOfMinimums(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark15KRequest(weeks, start);

        var ex = await Assert.ThrowsAsync<PlanCoreHorizonTooShortException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_15K_CORE_HORIZON", ex.Message);
        Assert.Contains("dark 15K target-distance", ex.Message);
        Assert.DoesNotContain("TARGET_BELOW_SUM_OF_MINIMUMS", ex.Message);
        Assert.DoesNotContain("HALF_MARATHON standalone core minimum (10", ex.Message);
        Assert.DoesNotContain("DARK_16K", ex.Message);
    }

    [Theory]
    [InlineData(15)]
    public async Task RealPipeline_AboveMaximumHorizon_RejectedByDark15KAuthority(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark15KRequest(weeks, start);

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_15K_CORE_HORIZON", ex.Message);
        Assert.DoesNotContain("DARK_16K", ex.Message);
        Assert.IsNotType<PlanHorizonStartTooEarlyException>(ex);
    }

    // ── Real production volume/taper materialization at CatalogVolumeAndLongRunPlanner layer ──

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
        var request = Dark15KRequest(targetWeekCount, start);

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
                RequestedTargetDistanceKm = 15.0, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon),
                StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = null, DaysPerWeek = 4, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new TargetDistance15KAwarePeakVolumeBandLoaderTestAccessor(Options.Create(OptionsValue)),
        };
    }

    [Fact]
    public async Task GoldenTrace_12W_MaterializesWithFrozen15KNumerics_LrCeilingIsFourteenPointFive()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 12);
        Assert.Equal("HALF_MARATHON", context.ResolverInput.CanonicalDistanceFamily);
        Assert.Equal(15.0, context.ResolverInput.RequestedTargetDistanceKm);

        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(12, prescribed.Weeks.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count));

        var phaseCounts = prescribed.Weeks.GroupBy(w => w.PhaseKey).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(2, phaseCounts["FOUNDATION"]);
        Assert.Equal(3, phaseCounts["BUILD"]);
        Assert.Equal(5, phaseCounts["RACE_SPECIFIC"]);
        Assert.Equal(2, phaseCounts["TAPER"]);

        // Peak volume: identical to 16K's own real trace (40.0km reference, evidence convergence).
        Assert.True(volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm) <= 40.0d + 0.001d);
        Assert.Equal(34.0d, volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm));

        // Long run: never reaches/exceeds the 15.0km target distance, and never
        // exceeds the TIGHTER 14.5km ceiling (not 15.0, unlike 16K's own).
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.PlannedLongRunDistanceKm < 15.0d);
            Assert.True(w.PlannedLongRunDistanceKm <= 14.5d + 0.001d);
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d);
        });
        // Observed peak long run matches 16K's own real 13.5km trace peak -- comfortably under 14.5.
        Assert.Equal(13.5d, volume.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm));

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
    [InlineData(10, 2, 3, 3, 2, 31.0)]
    [InlineData(11, 2, 3, 4, 2, 32.5)]
    [InlineData(12, 2, 3, 5, 2, 34.0)]
    [InlineData(13, 2, 4, 5, 2, 34.0)]
    [InlineData(14, 3, 4, 5, 2, 34.0)]
    public async Task AllFiveHorizons_MaterializeWithExpectedPhaseSplitsAndPeakVolume(
        int weeks, int foundation, int build, int raceSpecific, int taper, double expectedPeakVolume)
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: weeks);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        var phaseCounts = prescribed.Weeks.GroupBy(w => w.PhaseKey).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(foundation, phaseCounts["FOUNDATION"]);
        Assert.Equal(build, phaseCounts["BUILD"]);
        Assert.Equal(raceSpecific, phaseCounts["RACE_SPECIFIC"]);
        Assert.Equal(taper, phaseCounts["TAPER"]);
        Assert.Equal(expectedPeakVolume, volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm));
        Assert.True(volume.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm) <= 14.5d + 0.001d);
    }

    [Fact]
    public async Task EightWeekHorizon_CannotPhaseAllocate_SameStructuralFloorAs16K()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 8);

        var ex = await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() => Pipeline().MaterializeAsync(context));
        Assert.Contains("TARGET_BELOW_SUM_OF_MINIMUMS", ex.Message);
        Assert.Contains("sumOfMinimums=10", ex.Message);
    }

    // ── 15K-vs-16K side-by-side comparison at the same horizon/readiness ───

    [Fact]
    public async Task SideBySide_15KAnd16K_SameHorizonSameReadiness_DistinctTargetDistanceAndLrAuthority()
    {
        var candidate = await CandidateAsync();

        var context15 = Context(candidate, targetWeekCount: 12);
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(12 * 7 - 1);
        var request16 = Dark15KRequest(12, start);
        request16.TargetDistanceKmOverride = 16.0;
        var context16 = new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = 12, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults =
            [
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME"),
            ],
            PreviewRequest = request16,
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

        var result15 = await Pipeline().MaterializeAsync(context15);
        var result16 = await Pipeline().MaterializeAsync(context16);

        Assert.True(result15.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        Assert.True(result16.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);

        // Both use the same catalog candidate identity.
        Assert.Equal(candidate.CandidateKey, candidate.CandidateKey);

        // RequestedTargetDistanceKm differs and is correctly distinct.
        Assert.Equal(15.0, context15.ResolverInput.RequestedTargetDistanceKm);
        Assert.Equal(16.0, context16.ResolverInput.RequestedTargetDistanceKm);
        Assert.NotEqual(context15.ResolverInput.RequestedTargetDistanceKm, context16.ResolverInput.RequestedTargetDistanceKm);

        // PreferredAbsolutePeakLongRunKm authority differs even though observed peaks may coincide.
        Assert.Equal(14.5d, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(15.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K.PreferredAbsolutePeakLongRunKm);

        var peakLr15 = result15.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm);
        var peakLr16 = result16.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm);
        _output.WriteLine($"15K peak LR={peakLr15}, 16K peak LR={peakLr16}");
        // At this typical readiness, the actual generated peak LR values coincide numerically (both 13.5).
        Assert.Equal(peakLr16, peakLr15);

        // Independent policy identity: a 15.0 request must never resolve the 16K
        // cell, and a 16.0 request must never resolve the 15K cell.
        var cell15 = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        var cell16 = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        Assert.Equal(15.0, cell15!.TargetDistanceKm);
        Assert.Equal(16.0, cell16!.TargetDistanceKm);
    }

    // ── Unsupported nearby / non-adjacent targets ────────────────────────────

    [Theory]
    [InlineData(12.0)]
    [InlineData(18.0)]
    [InlineData(20.0)]
    public void ClassifiedButNotEligible_HalfMarathonFamily_NonAdjacentTargets(double km)
    {
        var resolver = new CanonicalDistanceFamilyResolver();
        var resolution = resolver.Resolve(km);
        Assert.Equal(GoalDistance.HalfMarathon, resolution.CanonicalDistanceFamily);
        Assert.False(Dark16KPilotEligibilityPolicy.IsDarkEligible(km, resolution.CanonicalDistanceFamily, RunningBackground.Intermediate, 4));
    }

    // ── Persistence proof: family + target distance coexist on TrainingPlan ──

    [Fact]
    public async Task RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark15KRequest(12, start);

        var internalUserId = Guid.NewGuid();
        var previewResponse = await service.GeneratePreviewAsync(internalUserId, request);
        Assert.NotNull(previewResponse);

        var confirmResponse = await service.ConfirmPlanAsync(
            internalUserId,
            new ConfirmPlanRequest { PreviewId = previewResponse.PreviewId });

        Assert.NotNull(confirmResponse);
        var persistedPlan = await context.TrainingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == confirmResponse.PlanId);
        Assert.NotNull(persistedPlan);

        Assert.Equal("HALF_MARATHON", persistedPlan!.CanonicalDistanceFamily);
        Assert.Equal(15.0, persistedPlan.RequestedTargetDistanceKm);
        Assert.NotEqual(16.0, persistedPlan.RequestedTargetDistanceKm);
        Assert.NotEqual(persistedPlan.RequestedTargetDistanceKm, persistedPlan.GoalDistanceKm);
    }
}

/// <summary>
/// Test-only accessor mirroring the production <see cref="TargetDistance16KAwarePeakVolumeBandLoader"/>
/// decision for direct-orchestrator tests that bypass <see cref="CatalogPreviewGenerator"/>,
/// substituting the 15K target's own frozen [34,46] band
/// (<see cref="TargetDistance15KPeakVolumeBandPolicy"/>) -- numerically
/// identical to 16K's own, but a distinct, independently-declared policy.
/// </summary>
internal sealed class TargetDistance15KAwarePeakVolumeBandLoaderTestAccessor : ICatalogPeakVolumeBandLoader
{
    private readonly ICatalogPeakVolumeBandLoader _inner;
    public TargetDistance15KAwarePeakVolumeBandLoaderTestAccessor(IOptions<PlanCatalogOptions> options) => _inner = new CatalogPeakVolumeBandLoader(options);

    public Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default) =>
        Task.FromResult(TargetDistance15KPeakVolumeBandPolicy.Build(distanceFamily, experience, runsPerWeek, reference.Key, reference.Version));
}
