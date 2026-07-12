using System.Globalization;
using System.Linq;

namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.4 — fourth concrete runtime-condition
/// resolver: GOAL_FEASIBILITY_IN only. Produces no output for
/// TIME_ADEQUACY_IN/PACE_SOURCE_IN/CORE_ENTRY_READINESS_IN/PLAN_MODE_IN.
///
/// V1 scope (Phase 4A.3 owner-approved, unchanged here): runtime output stays
/// registry-simple — REALISTIC/CHALLENGING/UNSUPPORTED/NOT_REQUESTED only.
/// The richer 5-class Appsel V1 aggressiveness band
/// (CONSERVATIVE/REALISTIC/CHALLENGING/STRETCH/CURRENTLY_UNSUPPORTED) is
/// carried as <c>Metadata["aggressivenessBand"]</c> only, per the same
/// owner-approved scope decision — never as OutputValue.
///
/// DEPENDS on prior CORE_ENTRY_READINESS_IN, TIME_ADEQUACY_IN, and
/// PACE_SOURCE_IN results via <see cref="RuntimeResolverContext.PriorResults"/>.
/// Missing or NotEvaluated dependencies are NEVER silently ignored — see
/// <see cref="Resolve"/> for the exact handling of each, and
/// PHASE4D_4_GOAL_FEASIBILITY_RESOLVER_IMPLEMENTATION.md for the full
/// evidentiary basis and every documented judgment call.
///
/// Race-time projection (when PACE_SOURCE_IN=RECENT_RACE) uses the Riegel
/// formula exactly as evidenced in
/// docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json's
/// PACE_CONVERSION step (RIEGEL_CONVERSION_5K_TO_10K: exponent=1.06), and the
/// REALISTIC/CHALLENGING/UNSUPPORTED ratio boundaries exactly as evidenced in
/// the same fixture's GOAL_FEASIBILITY_RESOLVER step
/// (BASE_GAP_CLASSIFICATION: realisticMaxRatio=0.03, challengingMaxRatio=0.06).
/// No formula parameter or threshold in this resolver was invented.
///
/// NOT wired into IPlanGenerationEngine/PlanServices/PlaceholderPlanGenerationEngine.
/// Does not require CoreCycle. Never mutates PriorResults.
/// </summary>
public sealed class GoalFeasibilityResolver : IGoalFeasibilityResolver
{
    /// <summary>Exposed as a const for tests/callers that need the literal without an instance.</summary>
    public const string ConditionTypeValue = "GOAL_FEASIBILITY_IN";

    public string ConditionType => ConditionTypeValue;

    private const string OutputRealistic = "REALISTIC";
    private const string OutputChallenging = "CHALLENGING";
    private const string OutputUnsupported = "UNSUPPORTED";
    private const string OutputNotRequested = "NOT_REQUESTED";

    /// <summary>Evidenced in golden-fixture-v3's GOAL_FEASIBILITY_RESOLVER / BASE_GAP_CLASSIFICATION facts.</summary>
    private const double RealisticMaxRatio = 0.03;

    /// <summary>Evidenced in the same fixture step.</summary>
    private const double ChallengingMaxRatio = 0.06;

    /// <summary>Appsel V1 aggressiveness-band upper bound for STRETCH, per docs/canonical/appsel-v1-canonical-decisions.md §B.1 (owner-approved for metadata use only, Phase 4A.3).</summary>
    private const double StretchMaxRatio = 0.10;

    /// <summary>Evidenced in golden-fixture-v3's PACE_CONVERSION / RIEGEL_CONVERSION_5K_TO_10K facts.</summary>
    private const double RiegelExponent = 1.06;

    public RuntimeConditionResolutionResult Resolve(RuntimeResolverContext context)
    {
        var input = context.InputSnapshot;

        // Section 4 (task spec): "no target finish time requested" is checked
        // first and unconditionally -- feasibility of an unrequested goal is
        // NOT_REQUESTED regardless of readiness/time-adequacy/pace-source
        // dependency state.
        if (input.TargetFinishTimeSeconds is null)
        {
            return RuntimeConditionResolutionResult.Evaluated(
                ConditionType, OutputNotRequested, "TARGET_FINISH_TIME_NOT_REQUESTED", input);
        }

        var metadata = new Dictionary<string, string>();

        // ── CORE_ENTRY_READINESS_IN dependency ──────────────────────────────
        var coreEntry = FindPriorResult(context, CoreEntryReadinessResolver.ConditionTypeValue);
        if (coreEntry is null)
        {
            return RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "MISSING_CORE_ENTRY_READINESS_RESULT", input);
        }
        if (coreEntry.Status == RuntimeConditionResolutionStatus.NotEvaluated)
        {
            return RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "CORE_ENTRY_READINESS_NOT_EVALUATED", input);
        }
        if (coreEntry.OutputValue == "NOT_READY")
        {
            return RuntimeConditionResolutionResult.Evaluated(
                ConditionType, OutputUnsupported, "CORE_ENTRY_NOT_READY", input);
        }
        if (coreEntry.OutputValue == "CAUTION")
        {
            // Task instruction: continue, but never silently upgrade to
            // REALISTIC -- record that a caution-level dependency was in
            // play so any consumer of this result can see it influenced
            // confidence, even though it does not change OutputValue here.
            metadata["coreEntryReadinessCaution"] = "true";
        }
        // READY: continue with no extra metadata.

        // ── TIME_ADEQUACY_IN dependency ─────────────────────────────────────
        var timeAdequacy = FindPriorResult(context, TimeAdequacyResolver.ConditionTypeValue);
        if (timeAdequacy is null)
        {
            return RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "MISSING_TIME_ADEQUACY_RESULT", input, metadata: metadata);
        }
        if (timeAdequacy.Status == RuntimeConditionResolutionStatus.NotEvaluated)
        {
            return RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "TIME_ADEQUACY_NOT_EVALUATED", input, metadata: metadata);
        }
        if (timeAdequacy.OutputValue == "INSUFFICIENT")
        {
            // No fixture evidence resolves whether INSUFFICIENT maps
            // directly to UNSUPPORTED or is meant to be handled by a future
            // PLAN_MODE_IN resolver instead (golden-fixture-v3's
            // TIME_ADEQUACY_MODIFIER rule exists conceptually -- it is
            // present in the trace -- but its only evidenced instance is
            // "NOT_APPLIED" for the ADEQUATE case; no INSUFFICIENT/COMPRESSED
            // example exists anywhere in the repo). Per explicit "do not
            // guess" instruction: NotEvaluated, not a guessed UNSUPPORTED.
            // See PHASE4D_4 doc, DECISION_REQUIRED item.
            return RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED", input, metadata: metadata);
        }
        if (timeAdequacy.OutputValue == "COMPRESSED")
        {
            metadata["timeAdequacyCompressed"] = "true";
        }
        // ADEQUATE: continue with no extra metadata.

        // ── PACE_SOURCE_IN dependency ────────────────────────────────────────
        var paceSource = FindPriorResult(context, PaceSourceResolver.ConditionTypeValue);
        if (paceSource is null)
        {
            return RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "MISSING_PACE_SOURCE_RESULT", input, metadata: metadata);
        }
        if (paceSource.Status == RuntimeConditionResolutionStatus.NotEvaluated)
        {
            return RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "PACE_SOURCE_NOT_EVALUATED", input, metadata: metadata);
        }

        return paceSource.OutputValue switch
        {
            "RECENT_RACE" => ResolveFromRecentRace(input, metadata),

            // Section 5: target time exists but pace evidence is NONE -- no
            // repo/product evidence supports choosing UNSUPPORTED over
            // NotEvaluated for this case, so NotEvaluated per "do not guess".
            "NONE" => RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "PACE_SOURCE_NONE_TARGET_TIME_REQUESTED", input, metadata: metadata),

            // Section 6: target time is a user goal, not independent
            // current-fitness evidence -- never validate it against itself.
            "TARGET_TIME" => RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", input, metadata: metadata),

            // Section 8: ESTIMATED is registry-valid but PaceSourceResolver
            // never emits it (TD-PACESOURCE-001) and no approved estimate
            // methodology exists to compute feasibility from it either.
            "ESTIMATED" => RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "PACE_SOURCE_ESTIMATED_NO_APPROVED_METHOD", input, metadata: metadata),

            _ => RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "UNKNOWN_PACE_SOURCE_OUTPUT_VALUE", input, metadata: metadata),
        };
    }

    /// <summary>
    /// Riegel projection + ratio classification, using only the exact
    /// exponent (1.06) and ratio boundaries (0.03 / 0.06) evidenced in
    /// golden-fixture-v3. Requires RecentRaceDistanceKm, RecentRaceFinishTimeSeconds
    /// (guaranteed present by PaceSourceResolver's own RECENT_RACE contract --
    /// it never emits RECENT_RACE without all three recent-race fields) and a
    /// target distance (RequestedTargetDistanceKm preferred, falling back to
    /// GoalDistanceKm only if the former is absent).
    /// </summary>
    private RuntimeConditionResolutionResult ResolveFromRecentRace(ResolverInputSnapshot input, Dictionary<string, string> metadata)
    {
        var sourceDistanceKm = input.RecentRaceDistanceKm;
        var sourceTimeSeconds = input.RecentRaceFinishTimeSeconds;
        var targetDistanceKm = input.RequestedTargetDistanceKm ?? input.GoalDistanceKm;
        var goalTimeSeconds = input.TargetFinishTimeSeconds;

        if (sourceDistanceKm is null || sourceTimeSeconds is null || targetDistanceKm is null || goalTimeSeconds is null)
        {
            return RuntimeConditionResolutionResult.NotEvaluated(
                ConditionType, "MISSING_PROJECTION_INPUT", input, metadata: metadata);
        }

        var predictedTimeSeconds = sourceTimeSeconds.Value * Math.Pow(targetDistanceKm.Value / sourceDistanceKm.Value, RiegelExponent);
        var goalGapRatio = (predictedTimeSeconds - goalTimeSeconds.Value) / predictedTimeSeconds;

        metadata["projectionMethod"] = "RIEGEL_V1";
        metadata["riegelExponent"] = RiegelExponent.ToString(CultureInfo.InvariantCulture);
        metadata["sourceDistanceKm"] = sourceDistanceKm.Value.ToString(CultureInfo.InvariantCulture);
        metadata["sourceTimeSeconds"] = sourceTimeSeconds.Value.ToString(CultureInfo.InvariantCulture);
        metadata["targetDistanceKm"] = targetDistanceKm.Value.ToString(CultureInfo.InvariantCulture);
        metadata["projectedFinishTimeSeconds"] = predictedTimeSeconds.ToString("0.##", CultureInfo.InvariantCulture);
        metadata["targetFinishTimeSeconds"] = goalTimeSeconds.Value.ToString(CultureInfo.InvariantCulture);
        metadata["targetDeltaPercent"] = (goalGapRatio * 100).ToString("0.##", CultureInfo.InvariantCulture);
        metadata["aggressivenessBand"] = ComputeAggressivenessBand(goalGapRatio);

        string outputValue;
        string reasonCode;
        if (goalGapRatio <= RealisticMaxRatio)
        {
            outputValue = OutputRealistic;
            reasonCode = "WITHIN_REALISTIC_BAND";
        }
        else if (goalGapRatio <= ChallengingMaxRatio)
        {
            outputValue = OutputChallenging;
            reasonCode = "WITHIN_CHALLENGING_BAND";
        }
        else
        {
            outputValue = OutputUnsupported;
            reasonCode = "EXCEEDS_CHALLENGING_BAND";
        }

        return RuntimeConditionResolutionResult.Evaluated(ConditionType, outputValue, reasonCode, input, metadata: metadata);
    }

    /// <summary>
    /// Appsel V1 5-class aggressiveness band, metadata-only (Phase 4A.3
    /// owner-approved scope), sourced from
    /// docs/canonical/appsel-v1-canonical-decisions.md §B.1: CONSERVATIVE
    /// (target equal to or slower than evidence), REALISTIC (0-3% faster),
    /// CHALLENGING (&gt;3-6% faster), STRETCH (&gt;6-10% faster),
    /// CURRENTLY_UNSUPPORTED (&gt;10% faster). Never returned as OutputValue.
    /// </summary>
    private static string ComputeAggressivenessBand(double goalGapRatio) => goalGapRatio switch
    {
        <= 0 => "CONSERVATIVE",
        <= RealisticMaxRatio => "REALISTIC",
        <= ChallengingMaxRatio => "CHALLENGING",
        <= StretchMaxRatio => "STRETCH",
        _ => "CURRENTLY_UNSUPPORTED",
    };

    private static RuntimeConditionResolutionResult? FindPriorResult(RuntimeResolverContext context, string conditionType) =>
        context.PriorResults?.FirstOrDefault(r => r.ConditionType == conditionType);
}
