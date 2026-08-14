namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4L.4C -- classifies a persisted block's internal reason code into
/// whether retry without new evidence can ever legitimately succeed. Every
/// reason code below comes from the existing, unmodified Phase 4K.3/4K.4
/// taxonomy (<see cref="LongHorizonCheckpointReasonCode"/>,
/// <see cref="LongHorizonJitReasonCode"/>) -- no new reason code is invented
/// here, only a recovery-authority mapping over the existing ones.
///
/// Investigation finding (Phase 4L.4C Part 3): this repository has no public
/// evidence-update, activity-import, profile-refresh or recovery-window
/// capability. Every block this endpoint can produce is triggered only after
/// the activation eligibility gate has already required the current window's
/// evidence to be fully terminal (immutable, one-shot completion/not-today).
/// So for every reason EXCEPT CheckpointWindowNotComplete (a genuinely
/// self-resolving real-calendar-time condition already encoded in the
/// existing evidence aggregator's own periodEnded check -- not a new
/// "wall-clock as evidence" rule invented here), no amount of waiting or
/// retrying changes the outcome: the plan requires a regenerated preview
/// (existing cancel + generate-preview + confirm flow) or, for
/// evidence-contradiction/lifecycle-integrity reasons, operational review.
/// </summary>
internal enum LongHorizonBlockRecoveryClass
{
    /// <summary>The block resolves on its own once real calendar time
    /// genuinely advances past the window's end date -- no new evidence
    /// submission is required or possible.</summary>
    RecoverableWithElapsedCalendarTime,

    /// <summary>The blocking evidence is durable and immutable; the
    /// confirmed rolling plan cannot legitimately continue. The user must
    /// cancel and confirm a new preview (existing capability, not new).</summary>
    RequiresRegeneratePreview,

    /// <summary>Indicates a safety flag, contradictory evidence, or a
    /// lifecycle/timing integrity condition that must not be self-serviced
    /// by a retry click.</summary>
    OperationalSupportRequired,
}

internal static class LongHorizonBlockRecoveryClassification
{
    public static LongHorizonBlockRecoveryClass Classify(string internalReasonCode) => internalReasonCode switch
    {
        nameof(LongHorizonCheckpointReasonCode.CheckpointWindowNotComplete) => LongHorizonBlockRecoveryClass.RecoverableWithElapsedCalendarTime,
        nameof(LongHorizonCheckpointReasonCode.SafetyReassessmentRequired) => LongHorizonBlockRecoveryClass.OperationalSupportRequired,
        nameof(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved) => LongHorizonBlockRecoveryClass.OperationalSupportRequired,
        nameof(LongHorizonJitReasonCode.JitEvidenceConflictUnresolved) => LongHorizonBlockRecoveryClass.OperationalSupportRequired,
        nameof(LongHorizonJitReasonCode.JitActivationBoundaryMissed) => LongHorizonBlockRecoveryClass.OperationalSupportRequired,
        // ValidatedLoadUnavailable, ValidatedLongRunEvidenceUnavailable,
        // NumericWindowInfeasible, RunwayJitContextUnavailable,
        // CoreJitContextUnavailable, JitValidatedLoadUnavailable,
        // JitValidatedLongRunUnavailable, JitPaceSourceUnresolved,
        // JitGoalFeasibilityUnresolved, JitAvailabilityInfeasible,
        // JitSegmentTransitionInfeasible, and any unrecognized code all
        // reflect durable, immutable evidence about the confirmed plan.
        _ => LongHorizonBlockRecoveryClass.RequiresRegeneratePreview,
    };

    public static bool IsRetryEligibleWithoutNewEvidence(LongHorizonBlockRecoveryClass recoveryClass) =>
        recoveryClass == LongHorizonBlockRecoveryClass.RecoverableWithElapsedCalendarTime;
}
