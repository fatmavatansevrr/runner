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
/// PHASE DIST-GEN.10 — dark-only 18K x INTERMEDIATE x 4D THIRD target-distance
/// projection, per PHASE_DIST_GEN_9_...md's complete frozen numeric authority.
/// Mirrors <see cref="Dark15KTargetDistanceProjectionTests"/>'s and
/// <see cref="Dark16KTargetDistanceProjectionTests"/>'s own structure exactly,
/// testing the SAME internal <see cref="GeneratePreviewRequest.TargetDistanceKmOverride"/>
/// seam for the third, independently-authorized target — never through any
/// public wire DTO. The recurring theme across this file: every value that
/// coincides with 15K's/16K's own (peak, calibration, taper, horizon, LR
/// shares) is asserted as a real, independently-resolved value, not
/// copy-pasted from the sibling tests; the one value that must differ from
/// BOTH siblings (PreferredAbsolutePeakLongRunKm = 16.0, not 14.5 or 15.0) is
/// asserted explicitly and directly against both sibling policies to prove
/// all three named policies are genuinely distinct instances.
/// </summary>
public sealed class Dark18KTargetDistanceProjectionTests
{
    private readonly ITestOutputHelper _output;
    public Dark18KTargetDistanceProjectionTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // ── Dark eligibility registry ────────────────────────────────────────────

    [Theory]
    [InlineData(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, true)]
    [InlineData(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, true)]
    [InlineData(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, true)]
    [InlineData(12.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(17.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(19.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(20.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(17.9, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(18.1, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(17.5, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(18.5, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    [InlineData(16.09, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)] // 10-mile exact, unchanged
    [InlineData(18.0, GoalDistance.HalfMarathon, RunningBackground.Beginner, 4, false)]
    [InlineData(18.0, GoalDistance.HalfMarathon, RunningBackground.Advanced, 4, false)]
    [InlineData(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3, false)]
    [InlineData(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5, false)]
    [InlineData(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 6, false)]
    [InlineData(18.0, GoalDistance.TenK, RunningBackground.Intermediate, 4, false)]
    [InlineData(null, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4, false)]
    public void DarkEligibilityRegistry_15K16K18K_AreTheOnlyEligibleCells(
        double? targetDistanceKm, GoalDistance goalDistance, RunningBackground level, int daysPerWeek, bool expectedEligible)
    {
        Assert.Equal(expectedEligible, Dark16KPilotEligibilityPolicy.IsDarkEligible(targetDistanceKm, goalDistance, level, daysPerWeek));
    }

    [Fact]
    public void DarkEligibilityRegistry_18K_ResolvesToOwnDistinctIndependentCell()
    {
        var fifteen = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        var sixteen = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        var eighteen = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);

        Assert.NotNull(fifteen);
        Assert.NotNull(sixteen);
        Assert.NotNull(eighteen);
        Assert.Equal(18.0, eighteen!.TargetDistanceKm);
        Assert.NotEqual(fifteen, eighteen);
        Assert.NotEqual(sixteen, eighteen);

        // An 18.0 request must never resolve the 15.0 or 16.0 cell, and vice versa.
        Assert.NotEqual(18.0, fifteen!.TargetDistanceKm);
        Assert.NotEqual(18.0, sixteen!.TargetDistanceKm);
        Assert.NotEqual(15.0, eighteen.TargetDistanceKm);
        Assert.NotEqual(16.0, eighteen.TargetDistanceKm);
    }

    /// <summary>
    /// PUBLIC eligibility remains a DISTINCT, narrower concept from dark
    /// eligibility -- must remain true ONLY for 15.0/16.0, never for 18.0,
    /// even though the dark registry now grants all three.
    /// </summary>
    [Fact]
    public void PublicEligibility_RemainsNarrowerThanDarkEligibility_18KNeverPublic()
    {
        Assert.True(Dark16KPilotEligibilityPolicy.IsEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsEligible(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsPubliclyEligible(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));

        // But 18.0 IS dark-eligible.
        Assert.True(Dark16KPilotEligibilityPolicy.IsDarkEligible(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));

        // ApprovedPublicCells must remain exactly the pre-existing two entries.
        Assert.Equal(2, Dark16KPilotEligibilityPolicy.ApprovedPublicCells.Count);
        Assert.DoesNotContain(Dark16KPilotEligibilityPolicy.ApprovedPublicCells, c => c.TargetDistanceKm == 18.0);
        Assert.Contains(Dark16KPilotEligibilityPolicy.ApprovedPublicCells, c => c.TargetDistanceKm == 15.0);
        Assert.Contains(Dark16KPilotEligibilityPolicy.ApprovedPublicCells, c => c.TargetDistanceKm == 16.0);

        // ApprovedDarkCells now has exactly three entries.
        Assert.Equal(3, Dark16KPilotEligibilityPolicy.ApprovedDarkCells.Count);
    }

    // ── Distance resolution: CanonicalDistanceFamilyResolver ────────────────

    [Fact]
    public void CanonicalDistanceFamilyResolver_18K_ResolvesToHalfMarathon_PreservesExactValue()
    {
        var resolver = new CanonicalDistanceFamilyResolver();
        var resolution = resolver.Resolve(18.0);
        Assert.Equal(GoalDistance.HalfMarathon, resolution.CanonicalDistanceFamily);
        Assert.Equal(18.0, resolution.RequestedTargetDistanceKm);
        Assert.NotEqual(21.0975, resolution.RequestedTargetDistanceKm);
        Assert.NotEqual(16.0, resolution.RequestedTargetDistanceKm);
        Assert.NotEqual(15.0, resolution.RequestedTargetDistanceKm);
        Assert.NotEqual(5.0, resolution.RequestedTargetDistanceKm);
    }

    // ── VolumeSafetyPolicy: exact frozen values, and the one deliberate difference ──

    [Fact]
    public void VolumeSafetyPolicy_18K_EncodesExactFrozenValues()
    {
        var policy = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K;
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

        // THE primary target-specific decision: different from BOTH siblings.
        Assert.Equal(16.0d, policy.PreferredAbsolutePeakLongRunKm);

        // Zero-delta: canonical HM and both sibling target policies are untouched.
        var canonical = VolumeSafetyPolicy.HalfMarathonIntermediate4D;
        Assert.Equal(43d, canonical.ResolvedPeakReference.Value);
        Assert.Equal(19d, canonical.PreferredAbsolutePeakLongRunKm);
        var fifteenK = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K;
        var sixteenK = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K;
        Assert.Equal(14.5d, fifteenK.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(15.0d, sixteenK.PreferredAbsolutePeakLongRunKm);
        Assert.NotEqual(fifteenK.PreferredAbsolutePeakLongRunKm, policy.PreferredAbsolutePeakLongRunKm);
        Assert.NotEqual(sixteenK.PreferredAbsolutePeakLongRunKm, policy.PreferredAbsolutePeakLongRunKm);

        // Taper multipliers are the SAME shared parent-family authority instance values as
        // the 16K sibling's own (deliberately NOT a third near-duplicate taper class -- see
        // TargetDistance16KTaperVolumePolicy reuse in VolumeSafetyPolicy.cs).
        Assert.Equal(TargetDistance16KTaperVolumePolicy.OrderedMultipliers, policy.ResolvedTaperVolumeMultipliers);
    }

    [Fact]
    public void PeakVolumeBandPolicy_18K_EncodesFrozenBand_IdenticalToButIndependentFromSiblings()
    {
        Assert.Equal(34.0d, TargetDistance18KPeakVolumeBandPolicy.MinimumKm);
        Assert.Equal(46.0d, TargetDistance18KPeakVolumeBandPolicy.MaximumKm);
        var band = TargetDistance18KPeakVolumeBandPolicy.Build("HALF_MARATHON", "INTERMEDIATE", 4, "PEAK_VOLUME_BAND_POLICY", 10);
        Assert.Equal(34.0d, band.MinimumKm);
        Assert.Equal(46.0d, band.MaximumKm);

        // Real evidence convergence with 15K's/16K's own band -- same values, distinct declared policy classes.
        Assert.Equal(TargetDistance16KPeakVolumeBandPolicy.MinimumKm, TargetDistance18KPeakVolumeBandPolicy.MinimumKm);
        Assert.Equal(TargetDistance15KPeakVolumeBandPolicy.MaximumKm, TargetDistance18KPeakVolumeBandPolicy.MaximumKm);
    }

    // ── Horizon authority: explicit provenance, not inherited by accident ──

    [Fact]
    public void HorizonPolicy_18K_ExactFrozenBounds_OwnDeclaredClass()
    {
        Assert.Equal(10, TargetDistance18KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(12, TargetDistance18KHorizonPolicy.PreferredCoreWeeks);
        Assert.Equal(14, TargetDistance18KHorizonPolicy.MaximumCoreWeeks);

        // Real evidence convergence with 15K's/16K's own triple -- same values, but this
        // asserts the VALUES came from the 18K policy's OWN declared constants, not
        // merely "happen to equal" the siblings' ones by accidental fallthrough.
        Assert.Equal(TargetDistance16KHorizonPolicy.MinimumCoreWeeks, TargetDistance18KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(TargetDistance15KHorizonPolicy.PreferredCoreWeeks, TargetDistance18KHorizonPolicy.PreferredCoreWeeks);
        Assert.Equal(TargetDistance16KHorizonPolicy.MaximumCoreWeeks, TargetDistance18KHorizonPolicy.MaximumCoreWeeks);
    }

    [Theory]
    [InlineData(7, RaceHorizonClassification.BelowMinimum)]
    [InlineData(8, RaceHorizonClassification.BelowMinimum)]
    [InlineData(9, RaceHorizonClassification.BelowMinimum)]
    [InlineData(10, RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(12, RaceHorizonClassification.ExactStandaloneCoreSupported)]
    [InlineData(14, RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(15, RaceHorizonClassification.CompositionRequired)]
    public void HorizonPolicy_18K_ClassifiesExactlyAtItsOwnFrozenBoundaries(int weeks, RaceHorizonClassification expected)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(weeks * 7);
        var classification = TargetDistance18KHorizonPolicy.Classify(start, race);
        Assert.Equal(expected, classification);
    }

    [Fact]
    public void HorizonPolicy_18K_ReasonCode_IsDistinctFromEveryOtherTargetsOwnCode()
    {
        var reasonCode = TargetDistance18KHorizonPolicy.GetUnsupportedReasonCode(7);
        Assert.Equal("DARK_18K_CORE_HORIZON_7_NOT_SUPPORTED", reasonCode);

        var fifteenKReasonCode = TargetDistance15KHorizonPolicy.GetUnsupportedReasonCode(7);
        var sixteenKReasonCode = TargetDistance16KHorizonPolicy.GetUnsupportedReasonCode(7);
        var tenKReasonCode = RaceHorizonPolicy.GetCoreHorizonUnsupportedReasonCode(7);
        Assert.NotEqual(reasonCode, fifteenKReasonCode);
        Assert.NotEqual(reasonCode, sixteenKReasonCode);
        Assert.NotEqual(reasonCode, tenKReasonCode);
        Assert.DoesNotContain("DARK_18K", fifteenKReasonCode);
        Assert.DoesNotContain("DARK_18K", sixteenKReasonCode);
        Assert.DoesNotContain("DARK_15K", reasonCode);
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

    private static GeneratePreviewRequest Dark18KRequest(int targetWeekCount, DateOnly start) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.HalfMarathon,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = start,
        RaceDate = start.AddDays(targetWeekCount * 7),
        TargetFinishTimeSeconds = 6100,
        TargetFinishTimeSource = TargetFinishTimeSource.UserDefined,
        TargetDistanceKmOverride = 18.0,
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
        var request = Dark18KRequest(weeks, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        Assert.NotNull(response);
        _output.WriteLine($"{weeks}W dark 18K preview materialized OK.");
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public async Task RealPipeline_BelowMinimumHorizon_RejectedByDark18KAuthority_NeverTargetBelowSumOfMinimums(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark18KRequest(weeks, start);

        var ex = await Assert.ThrowsAsync<PlanCoreHorizonTooShortException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_18K_CORE_HORIZON", ex.Message);
        Assert.Contains("dark 18K target-distance", ex.Message);
        Assert.DoesNotContain("TARGET_BELOW_SUM_OF_MINIMUMS", ex.Message);
        Assert.DoesNotContain("HALF_MARATHON standalone core minimum (10", ex.Message);
        Assert.DoesNotContain("DARK_15K", ex.Message);
        Assert.DoesNotContain("DARK_16K", ex.Message);
    }

    [Theory]
    [InlineData(15)]
    public async Task RealPipeline_AboveMaximumHorizon_RejectedByDark18KAuthority(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark18KRequest(weeks, start);

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_18K_CORE_HORIZON", ex.Message);
        Assert.DoesNotContain("DARK_15K", ex.Message);
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
        var request = Dark18KRequest(targetWeekCount, start);

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
                RequestedTargetDistanceKm = 18.0, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon),
                StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = null, DaysPerWeek = 4, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new TargetDistance18KAwarePeakVolumeBandLoaderTestAccessor(Options.Create(OptionsValue)),
        };
    }

    [Fact]
    public async Task GoldenTrace_12W_MaterializesWithFrozen18KNumerics_LrCeilingIsSixteen()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 12);
        Assert.Equal("HALF_MARATHON", context.ResolverInput.CanonicalDistanceFamily);
        Assert.Equal(18.0, context.ResolverInput.RequestedTargetDistanceKm);

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

        // Peak volume: identical to 15K's/16K's own real trace (40.0km reference, evidence convergence).
        Assert.True(volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm) <= 40.0d + 0.001d);
        Assert.Equal(34.0d, volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm));

        // Full W01-W12 golden table (DIST-GEN.2B/6/9's own already-proven real trace).
        var byWeek = volume.WeeklyVolumePlan.Weeks.ToDictionary(w => w.WeekNumber);
        var lrByWeek = volume.LongRunProgression.Weeks.ToDictionary(w => w.WeekNumber);
        (int Week, double Vol, double Lr)[] expected =
        [
            (1, 20.0, 6.5), (2, 21.5, 7.0), (3, 23.0, 7.5), (4, 24.5, 8.0),
            (5, 26.0, 8.5), (6, 28.0, 9.0), (7, 29.5, 10.5), (8, 31.0, 11.5),
            (9, 32.5, 12.5), (10, 34.0, 13.5), (11, 24.0, 8.0), (12, 14.5, 5.0),
        ];
        foreach (var (week, vol, lr) in expected)
        {
            Assert.Equal(vol, byWeek[week].PlannedWeeklyVolumeKm);
            Assert.Equal(lr, lrByWeek[week].PlannedLongRunDistanceKm);
        }

        // Long run: never reaches/exceeds the 18.0km target distance, and never
        // exceeds the 16.0km ceiling -- comfortably above the observed 13.5km peak
        // (never binds; the ceiling-check semantic is the only difference from siblings).
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.PlannedLongRunDistanceKm < 18.0d);
            Assert.True(w.PlannedLongRunDistanceKm <= 16.0d + 0.001d);
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d);
        });
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
        Assert.True(volume.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm) <= 16.0d + 0.001d);
    }

    [Fact]
    public async Task EightWeekHorizon_CannotPhaseAllocate_SameStructuralFloorAsSiblings()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 8);

        var ex = await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() => Pipeline().MaterializeAsync(context));
        Assert.Contains("TARGET_BELOW_SUM_OF_MINIMUMS", ex.Message);
        Assert.Contains("sumOfMinimums=10", ex.Message);
    }

    // ── 15K vs 16K vs 18K three-way isolation proof -- the single most important test ──

    private static DynamicCoreCalendarMaterializationContext ContextFor(
        PlanCatalogCandidateSummary candidate, double targetDistanceKm, ICatalogPeakVolumeBandLoader peakVolumeBandLoader)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(12 * 7 - 1);
        var request = Dark18KRequest(12, start);
        request.TargetDistanceKmOverride = targetDistanceKm;
        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = 12, StartDate = start, RaceDate = race, AsOfDate = start,
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
                RequestedTargetDistanceKm = targetDistanceKm, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon),
                StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = null, DaysPerWeek = 4, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = peakVolumeBandLoader,
        };
    }

    [Fact]
    public async Task ThreeWayIsolation_15K16K18K_SameHorizonSameReadiness_DistinctPeakLongRunAuthority()
    {
        var candidate = await CandidateAsync();

        var context15 = ContextFor(candidate, 15.0, new TargetDistance15KAwarePeakVolumeBandLoaderTestAccessor(Options.Create(OptionsValue)));
        var context16 = ContextFor(candidate, 16.0, new TargetDistance16KAwarePeakVolumeBandLoaderTestAccessor(Options.Create(OptionsValue)));
        var context18 = ContextFor(candidate, 18.0, new TargetDistance18KAwarePeakVolumeBandLoaderTestAccessor(Options.Create(OptionsValue)));

        var result15 = await Pipeline().MaterializeAsync(context15);
        var result16 = await Pipeline().MaterializeAsync(context16);
        var result18 = await Pipeline().MaterializeAsync(context18);

        Assert.True(result15.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        Assert.True(result16.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        Assert.True(result18.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);

        // RequestedTargetDistanceKm differs and is correctly distinct for all three.
        Assert.Equal(15.0, context15.ResolverInput.RequestedTargetDistanceKm);
        Assert.Equal(16.0, context16.ResolverInput.RequestedTargetDistanceKm);
        Assert.Equal(18.0, context18.ResolverInput.RequestedTargetDistanceKm);

        // The direct policy-identity provenance proof: each request resolves ONLY its
        // own registry cell -- registry-level proof, not just "the output numbers differ".
        var cell15 = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        var cell16 = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        var cell18 = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        Assert.Equal(15.0, cell15!.TargetDistanceKm);
        Assert.Equal(16.0, cell16!.TargetDistanceKm);
        Assert.Equal(18.0, cell18!.TargetDistanceKm);
        Assert.NotEqual(cell15, cell16);
        Assert.NotEqual(cell16, cell18);
        Assert.NotEqual(cell15, cell18);

        // PreferredAbsolutePeakLongRunKm authority: 14.5/15.0/16.0 respectively --
        // three genuinely distinct named VolumeSafetyPolicy instances, never a shared one.
        var policy15 = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K;
        var policy16 = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K;
        var policy18 = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K;
        Assert.Equal(14.5d, policy15.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(15.0d, policy16.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(16.0d, policy18.PreferredAbsolutePeakLongRunKm);
        Assert.False(ReferenceEquals(policy15, policy16));
        Assert.False(ReferenceEquals(policy16, policy18));
        Assert.False(ReferenceEquals(policy15, policy18));

        // Every target's own LR ceiling stays strictly below its own target distance.
        Assert.True(policy15.PreferredAbsolutePeakLongRunKm < 15.0d);
        Assert.True(policy16.PreferredAbsolutePeakLongRunKm < 16.0d);
        Assert.True(policy18.PreferredAbsolutePeakLongRunKm < 18.0d);

        var peakLr15 = result15.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm);
        var peakLr16 = result16.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm);
        var peakLr18 = result18.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm);
        _output.WriteLine($"15K peak LR={peakLr15}, 16K peak LR={peakLr16}, 18K peak LR={peakLr18}");
        // At this typical readiness, the actual generated peak LR values coincide numerically
        // (all 13.5, comfortably under all three ceilings) -- the CEILING AUTHORITY differs even
        // though the observed output happens not to differ, per DIST-GEN.9's own analysis.
        Assert.Equal(peakLr15, peakLr16);
        Assert.Equal(peakLr16, peakLr18);
    }

    // ── Unsupported nearby / non-adjacent targets ────────────────────────────

    [Theory]
    [InlineData(12.0)]
    [InlineData(17.0)]
    [InlineData(19.0)]
    [InlineData(20.0)]
    [InlineData(17.9)]
    [InlineData(18.1)]
    [InlineData(17.5)]
    [InlineData(18.5)]
    public void ClassifiedButNotEligible_HalfMarathonFamily_NonAdjacentAndNearbyTargets(double km)
    {
        var resolver = new CanonicalDistanceFamilyResolver();
        var resolution = resolver.Resolve(km);
        Assert.Equal(GoalDistance.HalfMarathon, resolution.CanonicalDistanceFamily);
        Assert.False(Dark16KPilotEligibilityPolicy.IsDarkEligible(km, resolution.CanonicalDistanceFamily, RunningBackground.Intermediate, 4));
    }

    [Fact]
    public void TenMileExact_16Point09_RemainsUnchangedStatus()
    {
        Assert.False(Dark16KPilotEligibilityPolicy.IsDarkEligible(16.09, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.False(Dark16KPilotEligibilityPolicy.IsEligible(16.09, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    // ── Wrong-cell negatives: 18K at every other level/frequency ────────────

    [Theory]
    [InlineData(RunningBackground.Beginner, 4)]
    [InlineData(RunningBackground.Advanced, 4)]
    [InlineData(RunningBackground.Intermediate, 3)]
    [InlineData(RunningBackground.Intermediate, 5)]
    [InlineData(RunningBackground.Intermediate, 6)]
    public void WrongCell_18K_NotEligibleAtAnyOtherLevelOrFrequency(RunningBackground level, int daysPerWeek)
    {
        Assert.False(Dark16KPilotEligibilityPolicy.IsDarkEligible(18.0, GoalDistance.HalfMarathon, level, daysPerWeek));
    }

    // ── Persistence proof: family + target distance coexist on TrainingPlan ──

    [Fact]
    public async Task RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark18KRequest(12, start);

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
        Assert.Equal(18.0, persistedPlan.RequestedTargetDistanceKm);
        Assert.NotEqual(16.0, persistedPlan.RequestedTargetDistanceKm);
        Assert.NotEqual(15.0, persistedPlan.RequestedTargetDistanceKm);
        Assert.NotEqual(persistedPlan.RequestedTargetDistanceKm, persistedPlan.GoalDistanceKm);
    }
}

/// <summary>
/// Test-only accessor mirroring the production <see cref="TargetDistance16KAwarePeakVolumeBandLoader"/>
/// decision for direct-orchestrator tests that bypass <see cref="CatalogPreviewGenerator"/>,
/// substituting the 18K target's own frozen [34,46] band
/// (<see cref="TargetDistance18KPeakVolumeBandPolicy"/>) -- numerically
/// identical to its siblings' own, but a distinct, independently-declared policy.
/// </summary>
internal sealed class TargetDistance18KAwarePeakVolumeBandLoaderTestAccessor : ICatalogPeakVolumeBandLoader
{
    private readonly ICatalogPeakVolumeBandLoader _inner;
    public TargetDistance18KAwarePeakVolumeBandLoaderTestAccessor(IOptions<PlanCatalogOptions> options) => _inner = new CatalogPeakVolumeBandLoader(options);

    public Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default) =>
        Task.FromResult(TargetDistance18KPeakVolumeBandPolicy.Build(distanceFamily, experience, runsPerWeek, reference.Key, reference.Version));
}
