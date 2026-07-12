using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.1 (amended 4D.1.5, 4D.1.6) — first concrete
/// runtime-condition resolver: TIME_ADEQUACY_IN only. Produces no output for
/// PACE_SOURCE_IN/CORE_ENTRY_READINESS_IN/GOAL_FEASIBILITY_IN/PLAN_MODE_IN.
///
/// Core-cycle week thresholds (minimum/default/maximum) are NEVER hardcoded
/// here — they are supplied by the caller via <see cref="RuntimeResolverContext.CoreCycle"/>
/// (itself read verbatim from the candidate's PLAN_TEMPLATE document, e.g.
/// <c>templates/ten-k-master.v6.json</c>'s <c>coreCycle</c> object). This
/// keeps the resolver distance-family-agnostic.
///
/// 4D.1.5 amendment: implements <see cref="ITimeAdequacyResolver"/> directly
/// using the shared <see cref="RuntimeConditionResolutionResult"/>'s
/// <see cref="RuntimeConditionResolutionStatus.NotEvaluated"/> status for the
/// missing-optional-evidence case (no more resolver-specific wrapper type).
///
/// 4D.1.6 amendment: the former TIME_ADEQUACY-specific two-argument
/// <c>Resolve(ResolverInputSnapshot, PlanCatalogCoreCycle)</c> shape is
/// replaced by the shared <see cref="RuntimeResolverContext"/>, so this
/// resolver's public surface is now exactly one method,
/// <see cref="IRuntimeConditionResolver.Resolve"/>, matching every other
/// resolver interface. A missing <see cref="RuntimeResolverContext.CoreCycle"/>
/// is treated as a resolver configuration/context error (see the method doc
/// below), not as missing user evidence — it throws, it does not return
/// NotEvaluated.
///
/// NOT wired into IPlanGenerationEngine/PlanServices/PlaceholderPlanGenerationEngine.
/// Does not implement PLAN_MODE_IN.READINESS_ONLY routing or any
/// compressed-readiness override — those remain out of scope for this phase.
/// </summary>
public sealed class TimeAdequacyResolver : ITimeAdequacyResolver
{
    /// <summary>Exposed as a const for tests/callers that need the literal without an instance.</summary>
    public const string ConditionTypeValue = "TIME_ADEQUACY_IN";

    public string ConditionType => ConditionTypeValue;

    private const string OutputAdequate = "ADEQUATE";
    private const string OutputCompressed = "COMPRESSED";
    private const string OutputInsufficient = "INSUFFICIENT";

    /// <summary>
    /// Computes availableWeeks as <c>floor(availableDays / 7)</c> using
    /// integer division on the (non-negative, validated) day count between
    /// startDate and raceDate — documented explicitly because the task
    /// instructs "do not guess silently" on rounding. Exactly 84 days (12
    /// weeks) yields availableWeeks=12; exactly 56 days (8 weeks) yields
    /// availableWeeks=8.
    ///
    /// Missing-date handling depends on <see cref="ResolverInputSnapshot.GoalType"/>
    /// (Phase 4D.1.5 owner-approved validation-layer clarification):
    /// <list type="bullet">
    /// <item>GoalType == Race: startDate and raceDate are REQUIRED request
    /// fields for a race plan (frontend + backend defensive validation are
    /// expected to have already rejected a race-goal request missing either).
    /// A resolver seeing a race-goal snapshot with either date missing throws
    /// <see cref="ArgumentException"/> — this is invalid input reaching the
    /// resolver, not a NotEvaluated case.</item>
    /// <item>GoalType == Habit, or GoalType is null/unknown: a habit-goal
    /// plan structurally never carries a race date (see
    /// PlanServices.ConfirmPlanAsync's own existing isRace branching, which
    /// unconditionally nulls RaceDate for non-race plans) — missing dates
    /// here are the NORMAL, expected state, not omitted input. Returns
    /// NotEvaluated ("NOT_APPLICABLE_NON_RACE_PLAN" for a confirmed
    /// non-race GoalType, or "MISSING_PLAN_TYPE_CONTEXT" if GoalType itself
    /// is unknown — the resolver cannot assume upstream validation ran for a
    /// plan type it cannot identify, so it does not throw on an unconfirmed
    /// assumption).</item>
    /// </list>
    /// If both dates ARE present regardless of GoalType, resolution proceeds
    /// normally — GoalType only affects the missing-date branch.
    ///
    /// <see cref="RuntimeResolverContext.CoreCycle"/> is REQUIRED for this
    /// resolver (unlike, e.g., a future PACE_SOURCE_IN resolver that would
    /// not need it) — TIME_ADEQUACY_IN cannot be computed at all without
    /// core-cycle boundaries. Its absence is treated as a resolver
    /// configuration/context error, not missing user evidence: it throws
    /// <see cref="InvalidOperationException"/> rather than returning
    /// NotEvaluated. Rationale: NotEvaluated is reserved for optional,
    /// legitimately-absent USER evidence (per Phase 4D.1.5); CoreCycle is
    /// catalog/config context the CALLER is responsible for supplying
    /// whenever it invokes this resolver at all — a caller invoking
    /// TimeAdequacyResolver without a CoreCycle is a programming/wiring
    /// mistake, not a normal runtime state a client request could ever
    /// legitimately produce.
    /// </summary>
    public RuntimeConditionResolutionResult Resolve(RuntimeResolverContext context)
    {
        if (context.CoreCycle is not { } coreCycle)
        {
            throw new InvalidOperationException(
                "TimeAdequacyResolver requires RuntimeResolverContext.CoreCycle to be set — TIME_ADEQUACY_IN " +
                "cannot be evaluated without core-cycle week boundaries from the candidate's PLAN_TEMPLATE. " +
                "This is a resolver configuration/context error (the caller must load and attach a " +
                "PlanCatalogCoreCycle before invoking this resolver), not missing user evidence.");
        }

        var input = context.InputSnapshot;

        if (input.StartDate is null || input.RaceDate is null)
        {
            if (input.GoalType == GoalType.Race)
            {
                var missingField = input.StartDate is null ? nameof(input.StartDate) : nameof(input.RaceDate);
                throw new ArgumentException(
                    $"{missingField} is required for a race-goal-type plan but was missing. Race-plan required " +
                    "fields must be rejected by frontend/backend validation before reaching a resolver — this is " +
                    "invalid input, not a NotEvaluated case.");
            }

            var reasonCode = input.GoalType is null
                ? "MISSING_PLAN_TYPE_CONTEXT"
                : "NOT_APPLICABLE_NON_RACE_PLAN";

            return RuntimeConditionResolutionResult.NotEvaluated(ConditionType, reasonCode, input);
        }

        var startDate = input.StartDate.Value;
        var raceDate = input.RaceDate.Value;

        if (raceDate < startDate)
        {
            throw new ArgumentException(
                $"RaceDate ({raceDate:yyyy-MM-dd}) must not be before StartDate ({startDate:yyyy-MM-dd}).");
        }

        var availableDays = raceDate.DayNumber - startDate.DayNumber;
        var availableWeeks = availableDays / 7; // integer division = floor for non-negative operands

        string outputValue;
        string reasonCode2;
        if (availableWeeks >= coreCycle.DefaultWeeks)
        {
            outputValue = OutputAdequate;
            reasonCode2 = "MEETS_DEFAULT_CORE_DURATION";
        }
        else if (availableWeeks >= coreCycle.MinimumWeeks)
        {
            outputValue = OutputCompressed;
            reasonCode2 = "BELOW_DEFAULT_BUT_MEETS_MINIMUM_CORE_DURATION";
        }
        else
        {
            outputValue = OutputInsufficient;
            reasonCode2 = "BELOW_MINIMUM_CORE_DURATION";
        }

        var metadata = new Dictionary<string, string>
        {
            ["availableWeeks"] = availableWeeks.ToString(),
            ["availableDays"] = availableDays.ToString(),
            ["minimumCoreWeeks"] = coreCycle.MinimumWeeks.ToString(),
            ["defaultCoreWeeks"] = coreCycle.DefaultWeeks.ToString(),
            ["roundingRule"] = "FLOOR_AVAILABLE_DAYS_DIV_7",
        };
        if (coreCycle.MaximumWeeks is not null)
        {
            metadata["maximumCoreWeeks"] = coreCycle.MaximumWeeks.Value.ToString();
        }

        return RuntimeConditionResolutionResult.Evaluated(
            ConditionType, outputValue, reasonCode2, input, metadata: metadata);
    }
}
