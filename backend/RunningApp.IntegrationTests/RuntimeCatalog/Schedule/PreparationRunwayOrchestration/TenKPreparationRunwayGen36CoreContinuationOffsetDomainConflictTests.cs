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
/// Phase 10K-GEN.36 -- real user decision "DECISION ON GEN.35" authorized an
/// additive continuation-offset parameter on Core's own real generator, to
/// close the anchor-continuity gap GEN.35 §2.3/§9 disclosed (Core's real
/// content pipeline always restarted its own Pattern-A/B alternation at
/// local week 1, ignoring the true GlobalWeekNumber a LongHorizon GE+Runway
/// segment had already established).
///
/// GEN.36 added the authorized parameter -- <see cref="TenKPreparationRunwayDarkOrchestrationRequest.CoreStartGlobalWeek"/>,
/// threaded additively (default 1, zero-delta) all the way down to
/// <see cref="Materialization.CatalogStageToWeekMaterializationContext.StartGlobalWeek"/>
/// (GEN.34's own mechanism) through six intermediate Dynamic-Core-* orchestrator
/// context types. This test documents, precisely and permanently, why GEN.36
/// did NOT enable this parameter at its one real intended call site
/// (<see cref="LongHorizon.RollingActivation.LongHorizonRollingJitCompositionOrchestrator"/>,
/// which would have supplied the Core segment's own authoritative
/// <c>StructuralRoadmap</c> <c>StartGlobalWeek</c>): doing so, for exactly the
/// odd-<c>GeneralEnduranceWeeks</c> case this whole arc exists to fix, causes
/// Core's own real local week 1 to correctly land on Pattern B
/// (EASY_SUPPORT+LONG_RUN, zero KEY_SESSION) -- which trips a genuine,
/// previously-latent, PRE-EXISTING hard requirement in
/// <see cref="PreparationRunwayPacePrescription.PreparationRunwayCoreWeekOnePaceAdapter.FromAuthoritativeCoreBehavior"/>
/// that Core's own Foundation Week 1 carry at least one KEY_SESSION in order
/// to derive an authoritative pace target for Runway's own numeric-continuity
/// ramp. This requirement was never exercised before GEN.36 because Core's
/// local week 1 was always, unconditionally, Pattern A.
///
/// This is a genuine product/pace-continuity numeric-authority question --
/// which week's pace should anchor Runway's own ramp when Core's true local
/// week 1 is the EASY-only pattern -- not a mechanical wiring gap (contrast
/// with the sibling <see cref="PreparationRunwayNumericMaterialization.PreparationRunwayCoreWeekOneTargetAdapter"/>,
/// which GEN.29 already generalized to tolerate a zero-KEY_SESSION week).
/// GEN.36's own governing decision explicitly required stopping and reporting
/// DOMAIN_DECISION_REQUIRED rather than resolving this unprompted, so this
/// test asserts the CURRENT, disclosed, still-blocked behavior -- it is
/// expected to keep failing with this exact message until a future phase's
/// own explicit decision resolves the underlying pace-anchor question one way
/// or another (e.g. by relaxing the adapter to fall back to the next
/// KEY_SESSION week, or by choosing a different anchor entirely). It must
/// never be "fixed" by simply loosening the adapter without that decision.
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
    public async Task CoreStartGlobalWeekEven_CausesCoreWeekOnePatternB_TripsPreExistingKeySessionPaceAnchorRequirement()
    {
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion);
        var request = await BuildRequestAsync(candidate, RunningBackground.Beginner, totalWeeks: 15, readinessValue: "READY", weekly: 12d, longest: 5d);

        // The only variable relative to every existing, passing 2D dark-orchestration
        // test: a non-default CoreStartGlobalWeek, mirroring the real value LongHorizon's
        // JIT composition orchestrator would supply for the odd-GeneralEnduranceWeeks case.
        var requestWithEvenCoreAnchor = request with { CoreStartGlobalWeek = 10 };

        var result = await Orchestrator().OrchestrateAsync(requestWithEvenCoreAnchor);

        // Currently, and by design of this disclosed-not-fixed decision, this composition
        // fails -- not succeeds with a wrong anchor (that was GEN.35's own disclosed gap,
        // now closed for the caller shape that never enables this parameter), but fails
        // outright, because Runway's own pace-continuity adapter cannot yet derive an
        // authoritative pace target from a Core Week 1 that carries zero KEY_SESSION.
        Assert.False(result.IsSuccess);
        Assert.Equal(TenKPreparationRunwayOrchestrationStage.FinalInvariantValidation, result.Failure!.Stage);
        Assert.Equal(TenKPreparationRunwayOrchestrationFailureCode.OrchestrationInvariantViolation, result.Failure.Code);
        Assert.Contains("Authoritative Core Foundation Week 1 pace target is unavailable", result.Failure.Reason, StringComparison.Ordinal);
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
