namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.4's two JIT context outcomes.</summary>
internal enum LongHorizonJitOutcome
{
    JitContextApproved,
    JitContextBlocked,
}

/// <summary>
/// Phase 4K.5 Part 10 — a typed representation of Phase 4K.4's approved
/// Runway/Core just-in-time context resolution result. Represents the
/// decision shape only -- the resolution logic itself (calling
/// RuntimeConditionResolutionService, the Core generator, etc.) is out of
/// scope for this phase and remains dark/unwired.
/// </summary>
internal sealed record LongHorizonJitContextDecision
{
    public required Guid DecisionId { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public required int ActivationBoundaryWeek { get; init; }
    public required int ActivationWindowStartWeek { get; init; }
    public required int ActivationWindowEndWeek { get; init; }
    public required IReadOnlyList<LongHorizonStructuralSegmentType> SegmentsCovered { get; init; }
    public required bool RunwayIncluded { get; init; }
    public required bool CoreIncluded { get; init; }
    public required bool ResolvedAtomically { get; init; }
    public LongHorizonLockedCoreWeekOneTarget? LockedCoreWeekOneTarget { get; init; }
    public LongHorizonEvidenceAuthorityRecord? WeeklyLoadAuthority { get; init; }
    public LongHorizonEvidenceAuthorityRecord? LongRunAuthority { get; init; }
    public LongHorizonEvidenceAuthorityRecord? PaceSourceAuthority { get; init; }
    public LongHorizonEvidenceAuthorityRecord? TargetTimeAuthority { get; init; }
    public LongHorizonEvidenceAuthorityRecord? GoalFeasibilityAuthority { get; init; }
    public LongHorizonEvidenceAuthorityRecord? CurrentProductionSourceMetadata { get; init; }
    public LongHorizonEvidenceAuthorityStatus? RollingAuthorityStatus { get; init; }
    public required Guid EvidenceSnapshotId { get; init; }
    public required IReadOnlyList<string> RequiredValidators { get; init; }
    public required LongHorizonJitOutcome Outcome { get; init; }
    public LongHorizonReasonCode? AuthoritativeReason { get; init; }
    public string? Provenance { get; init; }
}

/// <summary>
/// Phase 4K.5 Part 10 — enforces Part 10's own required invariant: if any
/// Runway week is activated, the Core Week-1 target must be present,
/// atomic, locked, and its evidence authority explicit (never silently
/// defaulted -- <see cref="LongHorizonEvidenceAuthorityRecord.Create"/>
/// already refuses the one forbidden combination at construction time; this
/// validator enforces the surrounding JIT-decision-level requirements).
/// </summary>
internal static class LongHorizonJitContextValidator
{
    public static void Validate(LongHorizonJitContextDecision decision)
    {
        if (decision.Outcome == LongHorizonJitOutcome.JitContextBlocked)
        {
            ValidateBlocked(decision);
            return;
        }

        ValidateApproved(decision);
    }

    private static void ValidateBlocked(LongHorizonJitContextDecision decision)
    {
        if (decision.AuthoritativeReason is null)
        {
            throw new LongHorizonJitContextInvalidException(
                "JitContextBlocked must carry exactly one authoritative reason (Phase 4K.4 §20).");
        }

        if (decision.LockedCoreWeekOneTarget is not null)
        {
            throw new LongHorizonJitContextInvalidException(
                "JitContextBlocked must not carry an executable Core Week-1 target.");
        }
    }

    private static void ValidateApproved(LongHorizonJitContextDecision decision)
    {
        if (decision.AuthoritativeReason is not null)
        {
            throw new LongHorizonJitContextInvalidException(
                "JitContextApproved must not carry a blocked reason.");
        }

        if (!decision.RunwayIncluded)
        {
            return;
        }

        // Primary decision (Phase 4K.4 §2/§4, required invariant): if any
        // Runway week is activated, the Core Week-1 target must be
        // non-null, atomically resolved, locked for the activated Runway
        // weeks, and its evidence authority status explicit.
        if (decision.LockedCoreWeekOneTarget is null)
        {
            throw new LongHorizonJitContextInvalidException(
                "A JitContextApproved decision including Runway must carry a non-null LockedCoreWeekOneTarget " +
                "-- Runway's numeric materializer cannot execute without it (Phase 4K.4 §4).");
        }

        if (!decision.ResolvedAtomically)
        {
            throw new LongHorizonJitContextInvalidException(
                "A JitContextApproved decision including Runway must indicate atomic Runway+Core resolution (Phase 4K.4 §9).");
        }

        LongHorizonCoreTargetLockValidator.Validate(decision.LockedCoreWeekOneTarget);

        if (decision.RollingAuthorityStatus is null)
        {
            throw new LongHorizonJitContextInvalidException(
                "A JitContextApproved decision including Runway must state an explicit RollingAuthorityStatus " +
                "-- it must never be left implicit (Phase 4K.5 Critical Authority Distinction).");
        }
    }
}
