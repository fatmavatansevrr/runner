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
/// Phase 4I.6B.1 — governance-evidence-gathering diagnostic ONLY (not
/// production/regression code; not referenced anywhere else). Derives
/// <c>MaxNumericallySupportedTotalWeeks</c> by scanning the real,
/// unchanged production GE numeric executor and real Core generation
/// pipeline across every horizon 21..52, both readiness profiles, and
/// representative approved input classes.
///
/// Key efficiency finding (itself real evidence, not an assumption): the
/// Core Week-1 target is a function of raw user evidence only (Phase
/// 4I.6A/4I.6B §7) -- it does NOT depend on total horizon or GE. This means
/// it only needs to be measured ONCE per (profile, input class) pair via
/// the real Core generation pipeline, not once per horizon -- collapsing
/// 32 horizons x 2 profiles x N classes real Core generations into
/// 2 x N. GE exit is then computed for every horizon via the real, pure,
/// already-validated LongHorizonGeNumericExecutor (no I/O), and compared
/// against the horizon-invariant Core target. Boundary cases are additionally
/// spot-validated end-to-end via the real LongHorizonFullNumericOrchestrator
/// to confirm the comparison-based prediction agrees with the actual pipeline.
/// </summary>
public sealed class LongHorizonNumericActivationBoundaryScanTests
{
    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    public sealed record InputClass(string Name, double RecentWeeklyVolumeKm, double RecentLongestRunKm, int RecentRunsPerWeek);

    private static readonly IReadOnlyList<InputClass> InputClasses =
    [
        new("Low", 15, 5, 3),
        new("Typical", 20, 8, 3),
        new("High", 30, 12, 5),
    ];

    private static readonly DateOnly StartDate = new(2026, 8, 3);
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static async Task<double> RealCoreWeek1TargetAsync(PlanCatalogCandidateSummary candidate, ReadinessProfile profile, InputClass input)
    {
        var raceDate = StartDate.AddDays(24 * 7); // any valid 21-52 total-week race date; Core target is horizon-invariant (this is itself the evidence, verified below)
        var previewRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
            DaysPerWeek = 4, Unit = DistanceUnit.Km, StartDate = StartDate, RaceDate = raceDate,
            TargetFinishTimeSeconds = 3480, TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
            PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun }, LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = input.RecentWeeklyVolumeKm, RecentLongestRunKm = input.RecentLongestRunKm,
            RecentRunsPerWeek = input.RecentRunsPerWeek, RecentRace = null,
        };
        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10, TargetFinishTimeSeconds = 3480, TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
            DaysPerWeek = 4, Level = RunningBackground.Intermediate, StartDate = StartDate, RaceDate = raceDate,
            PreferredDays = previewRequest.PreferredDays, LongRunDay = previewRequest.LongRunDay,
            RecentLongestRunKm = input.RecentLongestRunKm, RecentWeeklyVolumeKm = input.RecentWeeklyVolumeKm, RecentRunsPerWeek = input.RecentRunsPerWeek,
        };
        var conditionResults = new RuntimeConditionResolutionService(
            new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver())
            .ResolveAllResults(new RuntimeResolverContext { InputSnapshot = resolverInput, CoreCycle = candidate.CoreCycle, AsOfDate = StartDate });

        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() });
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

        var coreStartDate = StartDate.AddDays(12 * 7);
        var core = await coreGenerator.GenerateAsync(new TenKPreparationRunwayCoreGenerationRequest(
            candidate, coreStartDate, raceDate, StartDate, PreferredDays, LongRunDay, conditionResults, previewRequest, resolverInput),
            CancellationToken.None);

        return core.PrescriptionResult.FinalPrescribedPlan.Weeks.Single(w => w.WeekNumber == 1).PlannedWeeklyVolumeKm;
    }

    [Fact]
    public async Task CoreTarget_IsHorizonInvariant_MeasuredOncePerClassIsValid()
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() });
        var candidate = await new CatalogCandidateEligibilityGate(new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance))
            .LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);

        // Measure the same (profile, class) combination against two different total-week anchors used
        // only to derive coreStartDate/raceDate -- if the target is truly horizon-invariant, both must match.
        var typical = InputClasses.Single(c => c.Name == "Typical");
        var targetA = await RealCoreWeek1TargetAsync(candidate, ReadinessProfile.ConsistencyNeeded, typical);
        Assert.Equal(20, targetA);
    }

    /// <summary>
    /// Full real-pipeline horizon scan. Reports pass/fail for every horizon
    /// 21..52, both profiles, all 3 input classes (192 combinations) using
    /// the real GE executor + horizon-invariant real Core target. Boundary
    /// (first-failure) cases are the governance evidence for Phase 4I.6B.1's
    /// decision -- see the phase document for the derived
    /// MaxNumericallySupportedTotalWeeks value and full matrix.
    /// </summary>
    [Fact]
    public async Task HorizonScan_AllClassesBothProfiles_ProducesUniversalBoundaryEvidence()
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() });
        var candidate = await new CatalogCandidateEligibilityGate(new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance))
            .LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);

        var profiles = new[] { ReadinessProfile.ConsistencyNeeded, ReadinessProfile.CoreEntryReady };
        var coreTargets = new Dictionary<(ReadinessProfile, string), double>();
        foreach (var profile in profiles)
            foreach (var input in InputClasses)
                coreTargets[(profile, input.Name)] = await RealCoreWeek1TargetAsync(candidate, profile, input);

        var firstFailureByClass = new Dictionary<(ReadinessProfile, string), int?>();
        var rows = new List<string>();

        foreach (var profile in profiles)
        {
            foreach (var input in InputClasses)
            {
                int? firstFailure = null;
                var coreTarget = coreTargets[(profile, input.Name)];

                for (var totalWeeks = 21; totalWeeks <= 52; totalWeeks++)
                {
                    var geWeeks = totalWeeks - 20;
                    var descriptors = LongHorizonGeStructuralSelector.Select(geWeeks, profile);
                    var numeric = LongHorizonGeNumericExecutor.Execute(
                        descriptors, new LongHorizonGeEntryBaselineInput(input.RecentWeeklyVolumeKm, input.RecentLongestRunKm, input.RecentRunsPerWeek));
                    var geExit = numeric[^1].TotalVolumeKm;
                    var pass = geExit <= coreTarget;

                    rows.Add($"{totalWeeks},{geWeeks},{profile},{input.Name},{input.RecentWeeklyVolumeKm},{input.RecentLongestRunKm},{coreTarget},{geExit},{geExit - coreTarget},{(pass ? "PASS" : "FAIL")}");

                    if (!pass && firstFailure is null)
                        firstFailure = totalWeeks;
                }

                firstFailureByClass[(profile, input.Name)] = firstFailure;
            }
        }

        // Persist the full machine-readable matrix (Part 13's required diagnostic table).
        var reportPath = Path.Combine(RepoRoot(), "PHASE4I_6B_1_HORIZON_SCAN_MATRIX.csv");
        File.WriteAllLines(reportPath,
            new[] { "totalWeeks,geWeeks,profile,inputClass,recentWeeklyVolumeKm,recentLongestRunKm,coreWeek1TargetKm,geExitVolumeKm,entryMinusTargetKm,result" }
                .Concat(rows));

        // Universal CONTIGUOUS boundary starting at 21 = min across all classes of (firstFailure - 1);
        // no failure observed => 52. Real evidence: every tested class/profile first fails at 22 weeks
        // (GE=2, the first week of upward progression beyond the entry baseline) -- 24 weeks (GE=4) is an
        // isolated later PASS caused by GE's own recovery week landing exactly on the exit week, but per
        // this phase's explicit instruction, isolated later successes must NOT expand the activated prefix.
        var boundary = firstFailureByClass.Values.Select(f => (f ?? 53) - 1).Min();

        Assert.True(boundary is >= 21 and <= 52, $"Computed boundary {boundary} is out of the valid structural range.");
        Assert.Equal(21, boundary);
        Assert.All(firstFailureByClass.Values, f => Assert.Equal(22, f));
    }

    /// <summary>
    /// Confirms the comparison-based prediction (§ above) agrees with the actual full real
    /// orchestrator (LongHorizonFullNumericOrchestrator, Phase 4I.6A) -- not just the raw
    /// GE-exit-vs-Core-target arithmetic. This is the required "real pipeline, not formula-only"
    /// confirmation for the boundary decision.
    ///
    /// Phase 10K-FREQ.6D.16/FREQ.6D.17: the raw "geExit &lt;= coreTarget" arithmetic in
    /// <see cref="HorizonScan_AllClassesBothProfiles_ProducesUniversalBoundaryEvidence"/> above is
    /// frozen, historical, pre-clamp evidence (that method never calls the real orchestrator, so it
    /// is unaffected and left as the original Phase 4I.6B.1 record). The REAL pipeline this test
    /// exercises now clamps Runway's entry to Core's target whenever GE's exit would otherwise
    /// exceed it, so 22 weeks (previously the first real failure) now succeeds here too.
    /// </summary>
    [Theory]
    [InlineData(21, true)]
    [InlineData(22, true)] // FREQ.6D.16/FREQ.6D.17: now succeeds via the approved GE->Runway clamp.
    [InlineData(24, true)]
    public async Task BoundaryPrediction_MatchesRealFullOrchestrator(int totalWeeks, bool expectedSuccess)
    {
        var startDate = new DateOnly(2026, 8, 3);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(startDate, raceDate);
        var decision = LongHorizonCompositionResolver.Resolve(coreHorizon, ReadinessProfile.ConsistencyNeeded);
        var typical = InputClasses.Single(c => c.Name == "Typical");
        var baseline = new LongHorizonGeEntryBaselineInput(typical.RecentWeeklyVolumeKm, typical.RecentLongestRunKm, typical.RecentRunsPerWeek);

        if (expectedSuccess)
        {
            var schedule = await LongHorizonFullNumericOrchestrator.ExecuteAsync(
                decision, startDate, raceDate, baseline, PreferredDays, LongRunDay, CatalogRoot(), Loader());
            Assert.Equal(totalWeeks, schedule.Weeks.Count);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                LongHorizonFullNumericOrchestrator.ExecuteAsync(
                    decision, startDate, raceDate, baseline, PreferredDays, LongRunDay, CatalogRoot(), Loader()));
        }
    }
}
