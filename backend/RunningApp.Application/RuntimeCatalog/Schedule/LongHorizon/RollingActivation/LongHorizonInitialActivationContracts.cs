namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5 Part 14 — a typed record distinguishing a plan's very first
/// numeric window (Phase 4K.3 §16/§19: uses the existing, unmodified Phase
/// 4I.6 onboarding-evidence baseline, not checkpoint machinery) from every
/// subsequent, checkpoint- or JIT-driven activation.
/// </summary>
internal sealed record LongHorizonInitialActivationContext
{
    public required Guid DecisionId { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public required LongHorizonInitialActivationSource ActivationSource { get; init; }
    public required LongHorizonEvidenceAuthorityRecord EvidenceSource { get; init; }
    public required bool SafetyValidationApplied { get; init; }
    public required bool FeasibilityValidationApplied { get; init; }
}

/// <summary>
/// Phase 4K.5 Part 14 — the required distinctions: initial activation must
/// not require completed-history evidence, must remain subject to safety
/// and feasibility validation, and must never be misclassified as
/// checkpoint-derived validated load.
/// </summary>
internal static class LongHorizonInitialActivationValidator
{
    public static void Validate(LongHorizonInitialActivationContext context)
    {
        if (context.DecisionId == Guid.Empty || context.ContextVersion.VersionId == Guid.Empty
            || context.ContextVersion.Sequence != 1)
        {
            throw new LongHorizonJitContextInvalidException(
                "Initial activation requires deterministic non-empty decision/context identities at sequence 1.");
        }

        if (!context.SafetyValidationApplied || !context.FeasibilityValidationApplied)
        {
            throw new LongHorizonJitContextInvalidException(
                "Initial activation remains subject to safety and feasibility validation even though it skips checkpoint machinery (Phase 4K.3 §16).");
        }

        if (context.ActivationSource == LongHorizonInitialActivationSource.InitialOnboardingActivation
            && context.EvidenceSource.Source == LongHorizonEvidenceSource.CompletedTrainingHistory)
        {
            throw new LongHorizonJitContextInvalidException(
                "InitialOnboardingActivation must not be sourced from CompletedTrainingHistory -- no completed-history " +
                "evidence exists yet at the very first numeric window (Phase 4K.3 §16); it must not be misclassified as " +
                "checkpoint-derived validated load.");
        }

        if (context.ActivationSource == LongHorizonInitialActivationSource.CheckpointRollingActivation
            && context.EvidenceSource.Source == LongHorizonEvidenceSource.OriginalOnboardingEvidence)
        {
            throw new LongHorizonJitContextInvalidException(
                "CheckpointRollingActivation must use checkpoint-derived evidence (CompletedTrainingHistory or " +
                "PriorValidatedCheckpointLoad), not OriginalOnboardingEvidence -- the two activation sources are " +
                "deliberately distinct (Phase 4K.3 §16, Phase 4K.5 Part 14).");
        }
    }
}
