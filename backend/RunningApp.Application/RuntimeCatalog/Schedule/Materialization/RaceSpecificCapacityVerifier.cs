using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

internal enum RaceSpecificCapacityOutcome
{
    Pass,
    Fail,
    DecisionRequired,
    NotApplicable,
}

internal enum RaceSpecificCapacityFindingCode
{
    AllocationNotMathematicallyFeasible,
    RaceSpecificPhaseMissing,
    RaceSpecificPhaseDuplicate,
    RaceSpecificProgressionMissing,
    RaceSpecificProgressionDuplicate,
    RaceSpecificStageKeyMissing,
    RaceSpecificStageKeyDuplicate,
    RaceSpecificFallbackTargetMissingOrDuplicate,
    WeeklyKeySessionSlotContractInvalid,
    RaceSpecificGuaranteedCapacityShortfall,
    RaceSpecificConditionalCapacityShortfall,
    RaceSpecificConditionalCapacityUnresolved,
    ExactFitZeroGuaranteedSlack,
    ExactFitZeroWorstCaseSlack,
}

internal sealed record RaceSpecificCapacityFinding(
    RaceSpecificCapacityFindingCode Code,
    string Message,
    string? StageKey = null,
    int? ExposureCount = null,
    int? Shortfall = null);

internal sealed record RaceSpecificCapacityVerificationResult(
    int TargetWeeks,
    int AllocatedRaceSpecificWeeks,
    int AvailableSlots,
    int UnconditionalRequiredExposureCount,
    int ConditionalRequiredExposureCount,
    int OptionalExposureCount,
    int GuaranteedSlack,
    int? WorstCaseSlack,
    RaceSpecificCapacityOutcome Outcome,
    IReadOnlyList<RaceSpecificCapacityFinding> Findings);

/// <summary>
/// Phase 4G.3B.3a: pure, dark structural-capacity check for the Race-Specific
/// phase only. One progression exposure occupies the run layout's one
/// KEY_SESSION slot for a phase week. This verifier does not resolve
/// conditions, choose stages, order stages, or perform workout/volume safety
/// checks, and it is not called by any production request path.
///
/// Retroactive addendum (Phase 4G.3B validation follow-up pass): the model
/// actually implemented here deliberately deviates from this phase's
/// original spec, which described a simpler protected/conditional
/// stage-count model with a binary Pass/Fail/DecisionRequired outcome. This
/// implementation instead computes GuaranteedSlack (available slots minus
/// unconditional-only demand) versus WorstCaseSlack (also subtracting
/// resolved conditional demand), and -- for a one-hop conditional-primary /
/// unconditional-fallback pair specifically -- counts the pair as consuming
/// max(primary.MinimumExposures, fallback.MinimumExposures) rather than the
/// sum of both, since at most one of the pair is ever actually scheduled.
/// Chained or ambiguous conditional/fallback topologies are deliberately
/// left DecisionRequired rather than guessed at. This deviation was not
/// flagged in any durable governance artifact at delivery time (no
/// PHASE4G_3B_3* document exists) -- this addendum is that missing flag,
/// added retroactively. The deviation's discriminating power (Fail/
/// DecisionRequired firing on a genuine shortfall, not just Pass on the one
/// real 8-week case that happens to work out) is covered by
/// RaceSpecificCapacityVerifierTests.Verify_DeterminateSimultaneousConditionalDemandShortfall_Fails
/// and Verify_SyntheticConditionalFallbackChainIsDecisionRequired_NotGuessed.
/// </summary>
internal static class RaceSpecificCapacityVerifier
{
    private const string RaceSpecificPhaseKey = "RACE_SPECIFIC";
    private const string KeySessionRole = "KEY_SESSION";

    public static RaceSpecificCapacityVerificationResult Verify(
        PhaseAllocationResult allocation,
        CatalogWorkoutProgressionDefinition progression,
        IReadOnlyList<string> weeklySlotRoles)
    {
        if (!allocation.IsMathematicallyFeasible)
        {
            return Result(allocation.TargetWeeks, outcome: RaceSpecificCapacityOutcome.NotApplicable,
                findings: [Finding(RaceSpecificCapacityFindingCode.AllocationNotMathematicallyFeasible,
                    "The phase allocation is not mathematically feasible, so Race-Specific capacity is not applicable.")]);
        }

        var allocatedMatches = allocation.Phases.Where(p => p.PhaseKey == RaceSpecificPhaseKey).ToList();
        if (allocatedMatches.Count != 1)
        {
            var code = allocatedMatches.Count == 0
                ? RaceSpecificCapacityFindingCode.RaceSpecificPhaseMissing
                : RaceSpecificCapacityFindingCode.RaceSpecificPhaseDuplicate;
            return Result(allocation.TargetWeeks, outcome: RaceSpecificCapacityOutcome.Fail,
                findings: [Finding(code, $"Expected exactly one {RaceSpecificPhaseKey} allocation, found {allocatedMatches.Count}.")]);
        }

        var progressionMatches = progression.PhaseProgressions.Where(p => p.PhaseKey == RaceSpecificPhaseKey).ToList();
        if (progressionMatches.Count != 1)
        {
            var code = progressionMatches.Count == 0
                ? RaceSpecificCapacityFindingCode.RaceSpecificProgressionMissing
                : RaceSpecificCapacityFindingCode.RaceSpecificProgressionDuplicate;
            return Result(allocation.TargetWeeks, allocatedMatches[0].AllocatedWeeks,
                outcome: RaceSpecificCapacityOutcome.Fail,
                findings: [Finding(code, $"Expected exactly one {RaceSpecificPhaseKey} workout progression, found {progressionMatches.Count}.")]);
        }

        var keySessionSlotsPerWeek = weeklySlotRoles.Count(r => r == KeySessionRole);
        if (keySessionSlotsPerWeek != 1)
        {
            return Result(allocation.TargetWeeks, allocatedMatches[0].AllocatedWeeks,
                outcome: RaceSpecificCapacityOutcome.Fail,
                findings: [Finding(RaceSpecificCapacityFindingCode.WeeklyKeySessionSlotContractInvalid,
                    $"The weekly role contract must expose exactly one {KeySessionRole} slot, found {keySessionSlotsPerWeek}.")]);
        }

        var stages = progressionMatches[0].Stages;
        if (stages.Any(s => string.IsNullOrWhiteSpace(s.ProgressionStageKey)))
        {
            return Result(allocation.TargetWeeks, allocatedMatches[0].AllocatedWeeks,
                outcome: RaceSpecificCapacityOutcome.Fail,
                findings: [Finding(RaceSpecificCapacityFindingCode.RaceSpecificStageKeyMissing,
                    "Race-Specific progression contains a missing or blank stage key.")]);
        }

        var duplicateKey = stages.GroupBy(s => s.ProgressionStageKey).FirstOrDefault(g => g.Count() > 1)?.Key;
        if (duplicateKey is not null)
        {
            return Result(allocation.TargetWeeks, allocatedMatches[0].AllocatedWeeks,
                outcome: RaceSpecificCapacityOutcome.Fail,
                findings: [Finding(RaceSpecificCapacityFindingCode.RaceSpecificStageKeyDuplicate,
                    $"Race-Specific progression contains duplicate stage key '{duplicateKey}'.", duplicateKey)]);
        }

        var stagesByKey = stages.ToDictionary(s => s.ProgressionStageKey);
        var fallbackTargetKeys = new HashSet<string>();
        foreach (var stage in stages.Where(s => !string.IsNullOrWhiteSpace(s.FallbackStageKey)))
        {
            if (!stagesByKey.ContainsKey(stage.FallbackStageKey!))
            {
                return Result(allocation.TargetWeeks, allocatedMatches[0].AllocatedWeeks,
                    outcome: RaceSpecificCapacityOutcome.Fail,
                    findings: [Finding(RaceSpecificCapacityFindingCode.RaceSpecificFallbackTargetMissingOrDuplicate,
                        $"Stage '{stage.ProgressionStageKey}' references missing fallback '{stage.FallbackStageKey}'.",
                        stage.ProgressionStageKey)]);
            }

            fallbackTargetKeys.Add(stage.FallbackStageKey!);
        }

        var topLevelStages = stages.Where(s => !fallbackTargetKeys.Contains(s.ProgressionStageKey)).ToList();
        // The schema has no protected/required/skippable flag. MinimumExposures is
        // therefore the only explicit capacity floor: a top-level stage whose floor
        // is zero is omittable for this capacity check. OptionalExposureCount reports
        // its declared maximum possible exposure slots, never part of the required floor.
        var optionalStages = topLevelStages.Where(s => s.MinimumExposures == 0).ToList();
        var requiredTopLevelStages = topLevelStages.Where(s => s.MinimumExposures > 0).ToList();
        var unconditional = requiredTopLevelStages.Where(s => s.Requires.Count == 0).ToList();
        var conditional = requiredTopLevelStages.Where(s => s.Requires.Count > 0).ToList();
        var unconditionalCount = unconditional.Sum(s => s.MinimumExposures);
        var optionalCount = optionalStages.Sum(s => s.MaximumExposures);
        var conditionalCount = 0;
        var conditionalUnresolved = false;
        var conditionalContributors = new List<string>();

        foreach (var stage in conditional)
        {
            conditionalContributors.Add(stage.ProgressionStageKey);
            if (stage.FallbackStageKey is null)
            {
                conditionalCount += stage.MinimumExposures;
                continue;
            }

            var fallback = stagesByKey[stage.FallbackStageKey];
            // The active catalog's one-hop conditional-primary -> unconditional-fallback
            // topology is fully defined by ProgressionStageAllocator as one alternative
            // exposure obligation. More complex conditional/fallback chains are left for
            // StageReachabilityVerifier rather than guessed here.
            if (fallback.Requires.Count > 0 || fallback.FallbackStageKey is not null)
            {
                conditionalUnresolved = true;
                continue;
            }

            conditionalCount += Math.Max(stage.MinimumExposures, fallback.MinimumExposures);
        }

        var allocatedWeeks = allocatedMatches[0].AllocatedWeeks;
        var availableSlots = allocatedWeeks; // exactly one verified KEY_SESSION slot per phase week
        var guaranteedSlack = availableSlots - unconditionalCount;
        var findings = new List<RaceSpecificCapacityFinding>();

        if (unconditionalCount > availableSlots)
        {
            var shortfall = unconditionalCount - availableSlots;
            findings.Add(Finding(RaceSpecificCapacityFindingCode.RaceSpecificGuaranteedCapacityShortfall,
                $"Race-Specific has {availableSlots} available exposure slot(s) but unconditional stages require {unconditionalCount}; shortfall={shortfall}; contributors={string.Join(",", unconditional.Select(s => $"{s.ProgressionStageKey}:{s.MinimumExposures}"))}.",
                exposureCount: unconditionalCount, shortfall: shortfall));
            return Result(allocation.TargetWeeks, allocatedWeeks, availableSlots, unconditionalCount,
                conditionalCount, optionalCount, guaranteedSlack, null, RaceSpecificCapacityOutcome.Fail, findings);
        }

        if (conditionalUnresolved)
        {
            findings.Add(Finding(RaceSpecificCapacityFindingCode.RaceSpecificConditionalCapacityUnresolved,
                "Conditional exposure demand uses a conditional or chained fallback topology whose simultaneous/substitution demand is outside this verifier's proven one-slot semantics.",
                string.Join(",", conditionalContributors)));
            return Result(allocation.TargetWeeks, allocatedWeeks, availableSlots, unconditionalCount,
                conditionalCount, optionalCount, guaranteedSlack, null, RaceSpecificCapacityOutcome.DecisionRequired, findings);
        }

        var worstCaseSlack = availableSlots - unconditionalCount - conditionalCount;
        if (worstCaseSlack < 0)
        {
            findings.Add(Finding(RaceSpecificCapacityFindingCode.RaceSpecificConditionalCapacityShortfall,
                $"Race-Specific has {availableSlots} available exposure slot(s), {unconditionalCount} unconditional and {conditionalCount} conditional required exposure(s); shortfall={-worstCaseSlack}.",
                string.Join(",", conditionalContributors), conditionalCount, -worstCaseSlack));
            return Result(allocation.TargetWeeks, allocatedWeeks, availableSlots, unconditionalCount,
                conditionalCount, optionalCount, guaranteedSlack, worstCaseSlack, RaceSpecificCapacityOutcome.Fail, findings);
        }

        if (guaranteedSlack == 0 && conditionalCount == 0)
        {
            findings.Add(Finding(RaceSpecificCapacityFindingCode.ExactFitZeroGuaranteedSlack,
                "Available slots exactly equal unconditional required exposures; guaranteed slack is zero."));
        }
        else if (worstCaseSlack == 0)
        {
            findings.Add(Finding(RaceSpecificCapacityFindingCode.ExactFitZeroWorstCaseSlack,
                "Available slots exactly equal unconditional plus conditional alternative exposure demand; worst-case slack is zero."));
        }

        return Result(allocation.TargetWeeks, allocatedWeeks, availableSlots, unconditionalCount,
            conditionalCount, optionalCount, guaranteedSlack, worstCaseSlack, RaceSpecificCapacityOutcome.Pass, findings);
    }

    private static RaceSpecificCapacityFinding Finding(
        RaceSpecificCapacityFindingCode code, string message, string? stageKey = null,
        int? exposureCount = null, int? shortfall = null) =>
        new(code, message, stageKey, exposureCount, shortfall);

    private static RaceSpecificCapacityVerificationResult Result(
        int targetWeeks, int allocatedWeeks = 0, int availableSlots = 0,
        int unconditional = 0, int conditional = 0, int optional = 0,
        int guaranteedSlack = 0, int? worstCaseSlack = null,
        RaceSpecificCapacityOutcome outcome = RaceSpecificCapacityOutcome.Fail,
        IReadOnlyList<RaceSpecificCapacityFinding>? findings = null) =>
        new(targetWeeks, allocatedWeeks, availableSlots, unconditional, conditional, optional,
            guaranteedSlack, worstCaseSlack, outcome, findings ?? Array.Empty<RaceSpecificCapacityFinding>());
}
