using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6B — governance-evidence-gathering diagnostic ONLY (not a
/// production/regression test suite; not referenced by any production code).
/// Extracts REAL observed GE-exit vs. Core-Week-1-target values for the
/// documented failing cases, using exactly the same real, unchanged
/// production authorities Phase 4I.6A's orchestrator already uses (GE
/// numeric executor + real Core generation) -- never a fabricated or
/// invented number. Used to populate the Phase 4I.6B governance decision's
/// own worked examples.
/// </summary>
public sealed class LongHorizonRunwayEntryAboveCoreTargetPolicyDiagnosticTests
{
    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static readonly LongHorizonGeEntryBaselineInput TypicalBaseline = new(20, 8, 3);

    /// <summary>
    /// Real, observed evidence backing Phase 4I.6B's decision. Excess grows
    /// steeply with horizon length because GE's own approved development
    /// progression (Phase 4I.2A, unmodified) compounds over more weeks while
    /// Core's Week-1 target is a fixed function of raw user evidence,
    /// independent of horizon -- confirming the magnitude problem is real,
    /// not a rounding/edge artifact.
    /// </summary>
    [Theory]
    [InlineData(28, 4.0)]
    [InlineData(40, 21.5)]
    [InlineData(52, 41.0)]
    public async Task DiagnosticEvidence_GeExitVsCoreTarget(int totalWeeks, double expectedExcessKm)
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() });
        var bundleLoader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        var candidate = await new CatalogCandidateEligibilityGate(bundleLoader)
            .LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);

        var geWeeks = totalWeeks - 20;
        var geDescriptors = LongHorizonGeStructuralSelector.Select(geWeeks, ReadinessProfile.ConsistencyNeeded);
        var geNumeric = LongHorizonGeNumericExecutor.Execute(geDescriptors, TypicalBaseline);
        var geExit = LongHorizonGeExitState.From(geDescriptors, geNumeric, ReadinessProfile.ConsistencyNeeded);

        var startDate = new DateOnly(2026, 8, 3);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
            DaysPerWeek = 4, Unit = DistanceUnit.Km, StartDate = startDate, RaceDate = raceDate,
            TargetFinishTimeSeconds = 3480, TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun }, LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = TypicalBaseline.RecentWeeklyVolumeKm, RecentLongestRunKm = TypicalBaseline.RecentLongestRunKm,
            RecentRunsPerWeek = TypicalBaseline.RecentRunsPerWeek, RecentRace = null,
        };
        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10, TargetFinishTimeSeconds = 3480, TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
            DaysPerWeek = 4, Level = RunningBackground.Intermediate, StartDate = startDate, RaceDate = raceDate,
            PreferredDays = previewRequest.PreferredDays, LongRunDay = previewRequest.LongRunDay,
            RecentLongestRunKm = TypicalBaseline.RecentLongestRunKm, RecentWeeklyVolumeKm = TypicalBaseline.RecentWeeklyVolumeKm,
            RecentRunsPerWeek = TypicalBaseline.RecentRunsPerWeek,
        };
        var conditionResults = new RuntimeConditionResolutionService(
            new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver())
            .ResolveAllResults(new RuntimeResolverContext { InputSnapshot = resolverInput, CoreCycle = candidate.CoreCycle, AsOfDate = startDate });

        var corePipeline = new DynamicCoreCalendarMaterializationOrchestrator(
            new DynamicCoreSessionPrescriptionOrchestrator(
                new DynamicCoreVolumeAndLongRunOrchestrator(
                    new DynamicCoreWorkoutBindingOrchestrator(
                        new DynamicCoreWeekSkeletonOrchestrator(
                            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                        new CatalogWorkoutProgressionLoader(options), new ProgressionStageAllocator(),
                        new GeneratedCatalogStageScheduleValidator(), new CatalogWeekSkeletonCalendarMaterializer(),
                        new DatedGeneratedCatalogPlanSkeletonValidator(), new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                    new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
                new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));
        var coreGenerator = new TenKPreparationRunwayCoreGenerator(
            corePipeline, new CatalogWorkoutDefinitionLoader(options), new CatalogPeakVolumeBandLoader(options));

        var coreStartDate = startDate.AddDays((geWeeks + 8) * 7);
        var core = await coreGenerator.GenerateAsync(new TenKPreparationRunwayCoreGenerationRequest(
            candidate, coreStartDate, raceDate, startDate, previewRequest.PreferredDays.Select(ToWeekday).ToArray(),
            ToWeekday(previewRequest.LongRunDay!.Value), conditionResults, previewRequest, resolverInput), CancellationToken.None);

        var coreWeek1 = core.PrescriptionResult.FinalPrescribedPlan.Weeks.Single(w => w.WeekNumber == 1);
        var coreWeek1LongRun = coreWeek1.Sessions.Single(s => s.StructuralRole == "LONG_RUN").PlannedDistanceKm;
        var excessVolume = geExit.FinalWeeklyVolumeKm - coreWeek1.PlannedWeeklyVolumeKm;

        Assert.True(excessVolume > 0, $"Expected GE exit to exceed Core Week 1 target at {totalWeeks} weeks (evidence for the Phase 4I.6B trigger condition).");
        Assert.Equal(expectedExcessKm, excessVolume, 1);
        Assert.Equal(20, coreWeek1.PlannedWeeklyVolumeKm); // Core's own target is horizon-independent (raw-evidence-derived, confirming no circularity).
    }

    private static DayOfWeek ToWeekday(Weekday day) => day switch
    {
        Weekday.Mon => DayOfWeek.Monday, Weekday.Tue => DayOfWeek.Tuesday, Weekday.Wed => DayOfWeek.Wednesday,
        Weekday.Thu => DayOfWeek.Thursday, Weekday.Fri => DayOfWeek.Friday, Weekday.Sat => DayOfWeek.Saturday,
        Weekday.Sun => DayOfWeek.Sunday, _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };
}
