using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

/// <summary>
/// Phase 10K-GEN.36 authorized an additive continuation-offset parameter on
/// Core's own real generator (<see cref="TenKPreparationRunwayDarkOrchestrationRequest.CoreStartGlobalWeek"/>,
/// threaded to <see cref="Materialization.CatalogStageToWeekMaterializationContext.StartGlobalWeek"/>)
/// but found that activating it at the one real intended call site, for the
/// odd-<c>GeneralEnduranceWeeks</c> case this whole arc exists to fix, made
/// Core's own real local week 1 correctly land on Pattern B
/// (EASY_SUPPORT+LONG_RUN, zero KEY_SESSION) -- which tripped a hard,
/// pre-existing requirement in
/// <see cref="PreparationRunwayPacePrescription.PreparationRunwayCoreWeekOnePaceAdapter.FromAuthoritativeCoreBehavior"/>
/// that Core's own Foundation Week 1 carry at least one KEY_SESSION. GEN.36
/// disclosed this as DOMAIN_DECISION_REQUIRED rather than resolving it
/// unprompted, and reverted the activation line.
///
/// Phase 10K-GEN.37 -- "DECISION ON GEN.36" resolved it: the pace adapter now
/// anchors to the first Core week within local weeks 1-2 that carries a
/// KEY_SESSION (Core's strict Pattern-A/B alternation guarantees one of the
/// first two weeks always does). This test replaces GEN.36's own
/// still-blocked-conflict assertion (which is now, by that test's own doc
/// comment, expected to fail) with an assertion of the new, decided, working
/// behavior: the same even-<c>CoreStartGlobalWeek</c> request that previously
/// failed at <c>FinalInvariantValidation</c> now succeeds, having anchored
/// Runway's own pace-continuity ramp to Core's local week 2 instead of its
/// (EASY-only) local week 1.
/// </summary>
public sealed class TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests
{
    private const string RealPublishedBundleReleaseVersion = "1.1.0";
    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static readonly IReadOnlyList<DayOfWeek> TueSun = [DayOfWeek.Tuesday, DayOfWeek.Sunday];

    /// <summary>
    /// A real 15-week 2D Beginner Runway/Core dark composition, with the
    /// newly-added <see cref="TenKPreparationRunwayDarkOrchestrationRequest.CoreStartGlobalWeek"/>
    /// set to 10 (even) -- reproducing exactly the arithmetic
    /// <c>LongHorizonRollingJitCompositionOrchestrator</c> would have supplied
    /// for the real totalWeeks=21/geWeeks=1 (odd) LongHorizon scenario
    /// (<c>coreSegment.StartGlobalWeek == GeneralEnduranceWeeks + 8 + 1 == 1 + 8 + 1 == 10</c>,
    /// GEN.34's own established convention). Every other input mirrors
    /// <c>Gen29TwoDayRunwayDarkOrchestrationTests</c>'s own established 2D
    /// Beginner Runway/Core request shape exactly -- the only variable is the
    /// new, non-default offset.
    /// </summary>
    [Fact]
    public async Task CoreStartGlobalWeekEven_CausesCoreWeekOnePatternB_AnchorsPaceToWeekTwo_Succeeds()
    {
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion);
        // Phase 10K-GEN.37 -- lowered from GEN.36's own (weekly:12, longest:5): anchoring to
        // Core's local week 2 (this test's whole point) targets a slightly higher numeric
        // value than local week 1 would have (Foundation volume progresses week over week),
        // so the same starting point GEN.36 used to reproduce the pace-anchor conflict now
        // requires a marginally steeper Runway ramp to reach it. This lower, still-realistic
        // starting point (mirroring Gen35TwoDayJitBoundaryFixture's own deliberately-low
        // onboarding baseline, chosen for the identical reason: "stay below Core's Week-1
        // target at every governed horizon") gives Runway's fixed 8-week ramp enough room to
        // reach the new anchor within PreparationRunwayNumericPolicy's approved per-week
        // change limits -- not a new numeric authority, just a test-scenario input choice.
        var request = await BuildRequestAsync(candidate, RunningBackground.Beginner, totalWeeks: 15, readinessValue: "READY", weekly: 8d, longest: 3d);

        // The only variable relative to every existing, passing 2D dark-orchestration
        // test: a non-default CoreStartGlobalWeek, mirroring the real value LongHorizon's
        // JIT composition orchestrator would supply for the odd-GeneralEnduranceWeeks case.
        var requestWithEvenCoreAnchor = request with { CoreStartGlobalWeek = 10 };

        var result = await Orchestrator().OrchestrateAsync(requestWithEvenCoreAnchor);

        // Phase 10K-GEN.37 -- "DECISION ON GEN.36": Core's local week 1 correctly lands
        // on Pattern B (EASY_SUPPORT+LONG_RUN, zero KEY_SESSION) for this even offset, and
        // the pace adapter now anchors to local week 2 (guaranteed Pattern A by Core's
        // strict alternation) instead of failing. This composition succeeds.
        Assert.True(result.IsSuccess, $"{result.Failure?.Stage}/{result.Failure?.Code}: {result.Failure?.Reason}");

        var coreWeekOne = result.CoreResult!.PrescriptionResult.FinalPrescribedPlan.Weeks
            .OrderBy(w => w.WeekNumber).First();
        Assert.DoesNotContain(coreWeekOne.Sessions, s => s.StructuralRole == "KEY_SESSION");

        var coreWeekTwo = result.CoreResult!.PrescriptionResult.FinalPrescribedPlan.Weeks
            .OrderBy(w => w.WeekNumber).Skip(1).First();
        Assert.Single(coreWeekTwo.Sessions, s => s.StructuralRole == "KEY_SESSION");
    }

    /// <summary>
    /// Sanity control: the exact same request shape with the default
    /// CoreStartGlobalWeek (1, i.e. every pre-GEN.36 caller's implicit value)
    /// still succeeds -- confirming the conflict above is specific to a
    /// non-default, even offset, not a pre-existing failure in this request
    /// shape generally, and that GEN.36's additive plumbing is genuinely
    /// inert (zero-delta) whenever the new parameter is left at its default.
    /// </summary>
    [Fact]
    public async Task CoreStartGlobalWeekDefault_SameRequestShape_StillSucceeds_ConfirmingZeroDeltaAtDefault()
    {
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion);
        var request = await BuildRequestAsync(candidate, RunningBackground.Beginner, totalWeeks: 15, readinessValue: "READY", weekly: 12d, longest: 5d);

        Assert.Equal(1, request.CoreStartGlobalWeek);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, $"{result.Failure?.Stage}/{result.Failure?.Code}: {result.Failure?.Reason}");
    }

    private static TenKPreparationRunwayDarkOrchestrator Orchestrator() =>
        TenKPreparationRunwayDarkOrchestratorFactory.Create(new PlanCatalogOptions
        {
            CatalogRootPath = CatalogRoot,
            PublishedBundleReleaseVersion = RealPublishedBundleReleaseVersion,
        });

    private static async Task<PlanCatalogCandidateSummary> LoadCandidateAsync(string key, int version)
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(key, version);
    }

    private static async Task<TenKPreparationRunwayDarkOrchestrationRequest> BuildRequestAsync(
        PlanCatalogCandidateSummary candidate, RunningBackground level, int totalWeeks, string readinessValue,
        double weekly, double longest)
    {
        var days = TueSun;
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(totalWeeks * 7);
        var longRunDay = DayOfWeek.Sunday;

        int? targetSeconds = 3600;
        var weekdays = days.Select(ToWeekday).ToArray();
        var preview = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = level,
            DaysPerWeek = 2, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = targetSeconds, TargetFinishTimeSource = null,
            PreferredDays = weekdays, LongRunDay = ToWeekday(longRunDay),
            RecentWeeklyVolumeKm = weekly, RecentLongestRunKm = longest, RecentRunsPerWeek = 2,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3600, RaceDate = start.AddDays(-21) },
        };
        var resolver = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK, GoalDistanceKm = 10, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = targetSeconds, TargetFinishTimeSource = null,
            DaysPerWeek = 2, PreferredDays = weekdays, LongRunDay = ToWeekday(longRunDay), Level = level,
            RecentWeeklyVolumeKm = weekly, RecentLongestRunKm = longest, RecentRunsPerWeek = 2,
            RecentRaceDistanceKm = 10,
            RecentRaceFinishTimeSeconds = 3600,
            RecentRaceDate = start.AddDays(-21),
        };
        var readiness = RuntimeConditionResolutionResult.Evaluated(
            CoreEntryReadinessResolver.ConditionTypeValue,
            readinessValue,
            readinessValue == "READY" ? "CORE_ENTRY_READY" : "CORE_ENTRY_NOT_READY");
        var conditions = new List<RuntimeConditionResolutionResult>
        {
            readiness,
            RuntimeConditionResolutionResult.Evaluated(PaceSourceResolver.ConditionTypeValue, "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
            RuntimeConditionResolutionResult.Evaluated(GoalFeasibilityResolver.ConditionTypeValue, "REALISTIC", "WITHIN_REALISTIC_BAND"),
        };
        return new TenKPreparationRunwayDarkOrchestrationRequest(
            candidate, start, race, start, days, longRunDay, readiness, conditions,
            preview, resolver, PreparationRunwayQuantityUnit.Kilometers);
    }

    private static Weekday ToWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekday.Mon,
        DayOfWeek.Tuesday => Weekday.Tue,
        DayOfWeek.Wednesday => Weekday.Wed,
        DayOfWeek.Thursday => Weekday.Thu,
        DayOfWeek.Friday => Weekday.Fri,
        DayOfWeek.Saturday => Weekday.Sat,
        DayOfWeek.Sunday => Weekday.Sun,
        _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };
}
