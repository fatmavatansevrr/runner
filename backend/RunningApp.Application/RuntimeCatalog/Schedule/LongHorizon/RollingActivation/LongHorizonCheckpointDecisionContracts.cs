namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.3's three mutually-exclusive checkpoint outcomes.</summary>
internal enum LongHorizonCheckpointOutcome
{
    GrowthEligible,
    MaintenanceOnly,
    NumericActivationBlocked,
}

/// <summary>
/// Phase 4K.5 Part 7 — a typed representation of Phase 4K.3's approved
/// checkpoint decision (the output of the eight-case deterministic
/// transition table, Phase 4K.3 §17). This phase represents the decision
/// shape only -- the transition table's evaluation logic itself, and any
/// aggregation from persisted evidence, are out of scope.
/// </summary>
internal sealed record LongHorizonCheckpointDecision
{
    public required Guid DecisionId { get; init; }
    public required Guid EvidenceSnapshotId { get; init; }
    public required LongHorizonCheckpointOutcome Outcome { get; init; }
    public LongHorizonReasonCode? AuthoritativeReason { get; init; }
    public ValidatedSustainableLoad? ValidatedLoad { get; init; }
    public double? MaintenanceAnchorWeeklyVolumeKm { get; init; }
    public required (int StartGlobalWeek, int EndGlobalWeek) ActivationWindowBoundary { get; init; }
    public required bool SafetyPriorityApplied { get; init; }
    public DateTime? CreatedAt { get; init; }
    public required string PolicyProvenance { get; init; }
}

/// <summary>
/// Phase 4K.5 Part 7 — enforces the reason-carrying rules Phase 4K.3 §17/§18
/// requires: GrowthEligible never carries a reason; MaintenanceOnly and
/// Blocked always carry exactly one, and Blocked's must be from the
/// checkpoint taxonomy (a JIT-category reason belongs to
/// <c>LongHorizonJitContextDecision</c>, never here).
/// </summary>
internal static class LongHorizonCheckpointDecisionValidator
{
    public static void Validate(LongHorizonCheckpointDecision decision)
    {
        switch (decision.Outcome)
        {
            case LongHorizonCheckpointOutcome.GrowthEligible:
                if (decision.AuthoritativeReason is not null)
                {
                    throw new LongHorizonCheckpointDecisionInvalidException(
                        "GrowthEligible must not carry a failure/decision reason (Phase 4K.3 §17).");
                }

                if (decision.ValidatedLoad is null || decision.ValidatedLoad.ValidationStatus != LongHorizonValidationStatus.Valid)
                {
                    throw new LongHorizonCheckpointDecisionInvalidException(
                        "GrowthEligible requires a Valid ValidatedSustainableLoad (Phase 4K.3 §11).");
                }

                break;

            case LongHorizonCheckpointOutcome.MaintenanceOnly:
                RequireExactlyOneCheckpointReason(decision, "MaintenanceOnly");
                break;

            case LongHorizonCheckpointOutcome.NumericActivationBlocked:
                RequireExactlyOneCheckpointReason(decision, "NumericActivationBlocked");
                break;

            default:
                throw new LongHorizonCheckpointDecisionInvalidException($"Unhandled checkpoint outcome {decision.Outcome}.");
        }

        if (decision.ActivationWindowBoundary.EndGlobalWeek < decision.ActivationWindowBoundary.StartGlobalWeek)
        {
            throw new LongHorizonCheckpointDecisionInvalidException(
                "ActivationWindowBoundary.EndGlobalWeek must be >= StartGlobalWeek.");
        }
    }

    private static void RequireExactlyOneCheckpointReason(LongHorizonCheckpointDecision decision, string outcomeName)
    {
        if (decision.AuthoritativeReason is not { } reason)
        {
            throw new LongHorizonCheckpointDecisionInvalidException(
                $"{outcomeName} must carry exactly one authoritative/decision-provenance reason (Phase 4K.3 §17).");
        }

        if (reason.Category != LongHorizonReasonCodeCategory.Checkpoint)
        {
            throw new LongHorizonCheckpointDecisionInvalidException(
                $"{outcomeName}'s reason must be a checkpoint-taxonomy reason, not a JIT-taxonomy reason.");
        }
    }
}
