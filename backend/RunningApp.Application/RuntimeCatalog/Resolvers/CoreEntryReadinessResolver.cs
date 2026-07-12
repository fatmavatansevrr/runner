using System.Globalization;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.3 (activated 4D.3.1) — third concrete
/// runtime-condition resolver: CORE_ENTRY_READINESS_IN only. Produces no
/// output for TIME_ADEQUACY_IN/PACE_SOURCE_IN/GOAL_FEASIBILITY_IN/PLAN_MODE_IN.
///
/// Phase 4D.3 shipped this resolver as an always-NotEvaluated skeleton
/// because no owner-approved READY/CAUTION/NOT_READY threshold mapping
/// existed in the repository (see TD-CORE-READINESS-001,
/// PHASE4D_3_CORE_ENTRY_READINESS_THRESHOLDS_DECISION_REQUIRED.md). Phase
/// 4D.3.1 activates real classification logic using the owner-approved V1
/// thresholds recorded as an internal canonical product decision (see
/// PHASE4D_3_1_CORE_ENTRY_READINESS_THRESHOLD_DECISION_AND_RESOLVER_ACTIVATION.md,
/// EV-008). These thresholds are a product decision, not scientific
/// literature — no external sourcing is claimed or implied.
///
/// Approved V1 thresholds (RecentWeeklyVolumeKm in km, RecentLongestRunKm in km):
/// <list type="bullet">
/// <item>READY: RecentWeeklyVolumeKm >= 15 AND RecentLongestRunKm >= 6.</item>
/// <item>NOT_READY: RecentWeeklyVolumeKm &lt; 8 OR RecentLongestRunKm &lt; 4
/// (checked BEFORE the READY check, so a low value on either field always
/// wins over a high value on the other — e.g. weekly=20/longest=3 is
/// NOT_READY, not READY).</item>
/// <item>CAUTION: everything else with both fields present (i.e. neither
/// NOT_READY nor READY matched — this covers the [8,15) weekly band and the
/// [4,6) longest-run band), OR exactly one of the two fields is present
/// ("partial core evidence") regardless of that single field's value.</item>
/// <item>Both fields missing: NOT_READY if GoalType is Race (a race-based
/// performance plan is assumed to require core-readiness evidence); a
/// registry-valid NotEvaluated-equivalent otherwise -- see the Resolve
/// doc comment for the exact NotEvaluated reasonCode used for Habit and
/// unknown/null GoalType.</item>
/// </list>
///
/// <see cref="ResolverInputSnapshot.RecentRunsPerWeek"/> is used as metadata
/// only in V1 — it is NEVER a hard gate for any output value, per the
/// explicit owner decision. STANDARD is never emitted under any input.
///
/// NOT wired into IPlanGenerationEngine/PlanServices/PlaceholderPlanGenerationEngine.
/// Does not require CoreCycle, does not read or mutate PriorResults, and does
/// not depend on TimeAdequacyResolver or PaceSourceResolver.
/// </summary>
public sealed class CoreEntryReadinessResolver : ICoreEntryReadinessResolver
{
    /// <summary>Exposed as a const for tests/callers that need the literal without an instance.</summary>
    public const string ConditionTypeValue = "CORE_ENTRY_READINESS_IN";

    public string ConditionType => ConditionTypeValue;

    private const string OutputReady = "READY";
    private const string OutputCaution = "CAUTION";
    private const string OutputNotReady = "NOT_READY";

    private const double WeeklyVolumeThresholdReady = 15.0;
    private const double WeeklyVolumeThresholdNotReady = 8.0;
    private const double LongestRunThresholdReady = 6.0;
    private const double LongestRunThresholdNotReady = 4.0;

    /// <summary>
    /// Classifies core-entry readiness. When both
    /// <see cref="ResolverInputSnapshot.RecentWeeklyVolumeKm"/> and
    /// <see cref="ResolverInputSnapshot.RecentLongestRunKm"/> are missing:
    /// <list type="bullet">
    /// <item>GoalType == Race: returns Evaluated/NOT_READY, reasonCode
    /// CORE_ENTRY_NOT_READY — a race-based performance plan is assumed to
    /// require core-readiness evidence, mirroring TimeAdequacyResolver's own
    /// GoalType-based race/non-race distinction (Phase 4D.1.5).</item>
    /// <item>GoalType == Habit: returns NotEvaluated, reasonCode
    /// CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT — no repo
    /// evidence requires core-readiness classification for a non-race/habit
    /// plan.</item>
    /// <item>GoalType == null (unknown): also returns NotEvaluated with the
    /// same reasonCode. This is a deliberate, documented conservative
    /// choice: the resolver cannot assume a "race-based performance plan
    /// context" it has no evidence for, so it does not guess NOT_READY for
    /// an unidentified plan type — mirroring TimeAdequacyResolver's own
    /// precedent of not assuming race context for a null GoalType.</item>
    /// </list>
    /// </summary>
    public RuntimeConditionResolutionResult Resolve(RuntimeResolverContext context)
    {
        var input = context.InputSnapshot;
        var weekly = input.RecentWeeklyVolumeKm;
        var longest = input.RecentLongestRunKm;

        var metadata = BuildBaseMetadata(input);

        if (weekly is null && longest is null)
        {
            return ResolveBothMissing(input, metadata);
        }

        if (weekly is null || longest is null)
        {
            metadata["triggeredCriterion"] = weekly is null
                ? "RecentWeeklyVolumeKm missing (partial core evidence)"
                : "RecentLongestRunKm missing (partial core evidence)";

            return RuntimeConditionResolutionResult.Evaluated(
                ConditionType, OutputCaution, "CORE_ENTRY_CAUTION", input, metadata: metadata);
        }

        // Both present.
        if (weekly < WeeklyVolumeThresholdNotReady || longest < LongestRunThresholdNotReady)
        {
            metadata["triggeredCriterion"] = weekly < WeeklyVolumeThresholdNotReady
                ? $"RecentWeeklyVolumeKm ({FormatKm(weekly.Value)}) < {FormatKm(WeeklyVolumeThresholdNotReady)}"
                : $"RecentLongestRunKm ({FormatKm(longest.Value)}) < {FormatKm(LongestRunThresholdNotReady)}";

            return RuntimeConditionResolutionResult.Evaluated(
                ConditionType, OutputNotReady, "CORE_ENTRY_NOT_READY", input, metadata: metadata);
        }

        if (weekly >= WeeklyVolumeThresholdReady && longest >= LongestRunThresholdReady)
        {
            return RuntimeConditionResolutionResult.Evaluated(
                ConditionType, OutputReady, "CORE_ENTRY_READY", input, metadata: metadata);
        }

        metadata["triggeredCriterion"] = weekly < WeeklyVolumeThresholdReady
            ? $"RecentWeeklyVolumeKm ({FormatKm(weekly.Value)}) in [{FormatKm(WeeklyVolumeThresholdNotReady)}, {FormatKm(WeeklyVolumeThresholdReady)})"
            : $"RecentLongestRunKm ({FormatKm(longest.Value)}) in [{FormatKm(LongestRunThresholdNotReady)}, {FormatKm(LongestRunThresholdReady)})";

        return RuntimeConditionResolutionResult.Evaluated(
            ConditionType, OutputCaution, "CORE_ENTRY_CAUTION", input, metadata: metadata);
    }

    private RuntimeConditionResolutionResult ResolveBothMissing(ResolverInputSnapshot input, Dictionary<string, string> metadata)
    {
        if (input.GoalType == GoalType.Race)
        {
            metadata["triggeredCriterion"] = "Both RecentWeeklyVolumeKm and RecentLongestRunKm missing in a race-based performance plan context";
            return RuntimeConditionResolutionResult.Evaluated(
                ConditionType, OutputNotReady, "CORE_ENTRY_NOT_READY", input, metadata: metadata);
        }

        var reasonCode = "CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT";
        var warnings = input.GoalType is null
            ? new[] { "GoalType is unknown -- cannot assume race-based performance plan context, so both-missing core evidence is not classified as NOT_READY." }
            : Array.Empty<string>();

        return RuntimeConditionResolutionResult.NotEvaluated(ConditionType, reasonCode, input, warnings: warnings, metadata: metadata);
    }

    private static Dictionary<string, string> BuildBaseMetadata(ResolverInputSnapshot input)
    {
        var metadata = new Dictionary<string, string>
        {
            ["weeklyVolumeThresholdReady"] = FormatKm(WeeklyVolumeThresholdReady),
            ["weeklyVolumeThresholdNotReady"] = FormatKm(WeeklyVolumeThresholdNotReady),
            ["longestRunThresholdReady"] = FormatKm(LongestRunThresholdReady),
            ["longestRunThresholdNotReady"] = FormatKm(LongestRunThresholdNotReady),
            ["recentRunsPerWeekUsedAsHardGate"] = "false",
            ["thresholdSource"] = "owner-approved internal canonical decision (Phase 4D.3.1)",
        };

        if (input.RecentWeeklyVolumeKm is { } weekly)
        {
            metadata["recentWeeklyVolumeKm"] = FormatKm(weekly);
        }

        if (input.RecentLongestRunKm is { } longest)
        {
            metadata["recentLongestRunKm"] = FormatKm(longest);
        }

        if (input.RecentRunsPerWeek is { } runsPerWeek)
        {
            metadata["recentRunsPerWeek"] = runsPerWeek.ToString(CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    private static string FormatKm(double value) => value.ToString(CultureInfo.InvariantCulture);
}
