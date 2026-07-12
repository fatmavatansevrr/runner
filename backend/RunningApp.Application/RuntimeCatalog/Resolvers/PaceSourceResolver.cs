using System.Globalization;

namespace RunningApp.Application.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.2 — second concrete runtime-condition
/// resolver: PACE_SOURCE_IN only. Produces no output for
/// TIME_ADEQUACY_IN/CORE_ENTRY_READINESS_IN/GOAL_FEASIBILITY_IN/PLAN_MODE_IN.
///
/// V1 scope (Phase 4A.3 owner-approved, unchanged here): PACE_SOURCE_IN
/// remains source-type only — its registry-valid output is exactly one of
/// NONE/RECENT_RACE/ESTIMATED/TARGET_TIME. Recency confidence and the
/// evidence hierarchy (certified race / time trial / structured test /
/// user-reported pace / effort-only) are trace metadata only, never the
/// OutputValue. No pace projection, Riegel conversion, or race-time
/// equivalence is computed here — this resolver only decides WHERE pace
/// evidence comes from, not what the pace actually is.
///
/// NOT wired into IPlanGenerationEngine/PlanServices/PlaceholderPlanGenerationEngine.
/// Does not require RuntimeResolverContext.CoreCycle and does not read or
/// mutate RuntimeResolverContext.PriorResults — it is independent of
/// TimeAdequacyResolver and of any other resolver.
/// </summary>
public sealed class PaceSourceResolver : IPaceSourceResolver
{
    /// <summary>Exposed as a const for tests/callers that need the literal without an instance.</summary>
    public const string ConditionTypeValue = "PACE_SOURCE_IN";

    public string ConditionType => ConditionTypeValue;

    private const string OutputNone = "NONE";
    private const string OutputRecentRace = "RECENT_RACE";
    private const string OutputTargetTime = "TARGET_TIME";
    // OutputEstimated ("ESTIMATED") is a registry-valid value this resolver
    // never emits in V1 — see the class doc and PHASE4D_2 documentation for
    // why: no repository-evidenced estimate method exists yet.

    /// <summary>
    /// Output priority, in order:
    /// <list type="number">
    /// <item><b>RECENT_RACE</b> — only when ALL THREE of RecentRaceDistanceKm,
    /// RecentRaceFinishTimeSeconds, and RecentRaceDate are present. Partial
    /// recent-race evidence (any one or two of the three) is never treated
    /// as RECENT_RACE — it falls through to the next tier, with a warning
    /// recorded that partial evidence was ignored.</item>
    /// <item><b>TARGET_TIME</b> — when recent-race evidence is not complete
    /// but TargetFinishTimeSeconds is present and positive. A user's target
    /// time is a GOAL, not proof of current fitness — it is never converted
    /// into or conflated with RECENT_RACE.</item>
    /// <item><b>ESTIMATED</b> — never emitted in V1 (see class doc).</item>
    /// <item><b>NONE</b> — when no usable evidence exists at all. This is a
    /// normal, Evaluated, registry-valid outcome — NOT a NotEvaluated status.
    /// Absence of pace evidence is not a resolver failure; PACE_SOURCE_IN's
    /// own registry vocabulary already has a value for exactly this case.</item>
    /// </list>
    /// Invalid numeric values (e.g. negative RecentRaceDistanceKm) are
    /// assumed already rejected by Phase 4B's existing PlanServices
    /// validation before a ResolverInputSnapshot is ever built — this
    /// resolver does not re-validate them.
    /// </summary>
    public RuntimeConditionResolutionResult Resolve(RuntimeResolverContext context)
    {
        var input = context.InputSnapshot;

        var hasCompleteRecentRace =
            input.RecentRaceDistanceKm is not null &&
            input.RecentRaceFinishTimeSeconds is not null &&
            input.RecentRaceDate is not null;

        var hasAnyRecentRaceField =
            input.RecentRaceDistanceKm is not null ||
            input.RecentRaceFinishTimeSeconds is not null ||
            input.RecentRaceDate is not null;

        var hasPartialRecentRace = hasAnyRecentRaceField && !hasCompleteRecentRace;

        if (hasCompleteRecentRace)
        {
            return ResolveRecentRace(context, input);
        }

        var warnings = new List<string>();
        if (hasPartialRecentRace)
        {
            warnings.Add(
                "Partial recent race evidence was provided (not all of RecentRaceDistanceKm/" +
                "RecentRaceFinishTimeSeconds/RecentRaceDate were present) and was ignored — RECENT_RACE " +
                "requires all three fields.");
        }

        if (input.TargetFinishTimeSeconds is > 0)
        {
            var metadata = new Dictionary<string, string>
            {
                ["targetFinishTimeSeconds"] = input.TargetFinishTimeSeconds.Value.ToString(CultureInfo.InvariantCulture),
            };
            return RuntimeConditionResolutionResult.Evaluated(
                ConditionType, OutputTargetTime, "TARGET_FINISH_TIME_PROVIDED", input,
                warnings: warnings, metadata: metadata);
        }

        // No usable recent race, no target time. V1 does not implement an
        // approved ESTIMATED-evidence method (see class doc), so this falls
        // through to NONE — a normal Evaluated outcome, not NotEvaluated.
        return RuntimeConditionResolutionResult.Evaluated(
            ConditionType, OutputNone, "NO_PACE_EVIDENCE_PROVIDED", input, warnings: warnings);
    }

    private RuntimeConditionResolutionResult ResolveRecentRace(RuntimeResolverContext context, ResolverInputSnapshot input)
    {
        var raceDate = input.RecentRaceDate!.Value;

        var metadata = new Dictionary<string, string>
        {
            ["recentRaceDistanceKm"] = input.RecentRaceDistanceKm!.Value.ToString(CultureInfo.InvariantCulture),
            ["recentRaceFinishTimeSeconds"] = input.RecentRaceFinishTimeSeconds!.Value.ToString(CultureInfo.InvariantCulture),
            ["recentRaceDate"] = raceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };

        string? confidenceLabel = null;
        if (context.AsOfDate is { } asOfDate)
        {
            var ageDays = asOfDate.DayNumber - raceDate.DayNumber;
            if (ageDays < 0)
            {
                throw new ArgumentException(
                    $"RecentRaceDate ({raceDate:yyyy-MM-dd}) is after RuntimeResolverContext.AsOfDate " +
                    $"({asOfDate:yyyy-MM-dd}) — a recent race result cannot be dated in the future relative to " +
                    "the resolution reference date.");
            }

            metadata["raceResultAgeDays"] = ageDays.ToString(CultureInfo.InvariantCulture);
            confidenceLabel = ComputePaceRecencyConfidence(ageDays);
            metadata["paceRecencyConfidence"] = confidenceLabel;
        }
        else
        {
            // Do not invent a reference date — report explicitly that
            // recency could not be computed rather than guessing.
            metadata["paceRecencyConfidence"] = "NOT_COMPUTED_NO_REFERENCE_DATE";
        }

        return RuntimeConditionResolutionResult.Evaluated(
            ConditionType, OutputRecentRace, "RECENT_RACE_RESULT_PROVIDED", input,
            confidenceLabel: confidenceLabel, metadata: metadata);
    }

    /// <summary>
    /// Appsel V1 recency confidence ladder, approved for trace-metadata use
    /// only (never as PACE_SOURCE_IN's OutputValue) per
    /// PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md §C: "Recency
    /// confidence ladder is stored as trace metadata, not registry output."
    /// </summary>
    private static string ComputePaceRecencyConfidence(int ageDays) => ageDays switch
    {
        <= 30 => "FULL",
        <= 60 => "HIGH",
        <= 90 => "MODERATE",
        <= 180 => "LOW_CONFIRMATION_NEEDED",
        _ => "NOT_USABLE_AS_PACE_ANCHOR",
    };
}
