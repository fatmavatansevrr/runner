namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5 — typed exception base for every rolling-activation contract
/// validator in this namespace, matching the existing
/// <c>CatalogSessionPrescriptionException</c>/<c>CatalogVolumeException</c>
/// convention (typed <see cref="Code"/> + <see cref="InvalidOperationException"/>).
/// Dark and unwired -- not thrown by any production/live request path.
/// </summary>
internal abstract class LongHorizonRollingContractException : InvalidOperationException
{
    public string Code { get; }

    protected LongHorizonRollingContractException(string code, string message) : base(message)
    {
        Code = code;
    }
}

internal sealed class LongHorizonIllegalLifecycleTransitionException : LongHorizonRollingContractException
{
    public LongHorizonIllegalLifecycleTransitionException(string message) : base("LONG_HORIZON_ILLEGAL_LIFECYCLE_TRANSITION", message) { }
}

internal sealed class LongHorizonStructuralRoadmapInvalidException : LongHorizonRollingContractException
{
    public LongHorizonStructuralRoadmapInvalidException(string message) : base("LONG_HORIZON_STRUCTURAL_ROADMAP_INVALID", message) { }
}

internal sealed class LongHorizonActivationWindowInvalidException : LongHorizonRollingContractException
{
    public LongHorizonActivationWindowInvalidException(string message) : base("LONG_HORIZON_ACTIVATION_WINDOW_INVALID", message) { }
}

internal sealed class LongHorizonMixedWindowAtomicityViolationException : LongHorizonRollingContractException
{
    public LongHorizonMixedWindowAtomicityViolationException(string message) : base("LONG_HORIZON_MIXED_WINDOW_ATOMICITY_VIOLATION", message) { }
}

internal sealed class LongHorizonNumericWeekInvalidException : LongHorizonRollingContractException
{
    public LongHorizonNumericWeekInvalidException(string message) : base("LONG_HORIZON_NUMERIC_WEEK_INVALID", message) { }
}

internal sealed class LongHorizonCheckpointDecisionInvalidException : LongHorizonRollingContractException
{
    public LongHorizonCheckpointDecisionInvalidException(string message) : base("LONG_HORIZON_CHECKPOINT_DECISION_INVALID", message) { }
}

internal sealed class LongHorizonEvidenceAuthorityDefaultingException : LongHorizonRollingContractException
{
    public LongHorizonEvidenceAuthorityDefaultingException(string message) : base("LONG_HORIZON_EVIDENCE_AUTHORITY_SILENT_DEFAULTING_REJECTED", message) { }
}

internal sealed class LongHorizonJitContextInvalidException : LongHorizonRollingContractException
{
    public LongHorizonJitContextInvalidException(string message) : base("LONG_HORIZON_JIT_CONTEXT_INVALID", message) { }
}

internal sealed class LongHorizonLockedTargetImmutabilityViolationException : LongHorizonRollingContractException
{
    public LongHorizonLockedTargetImmutabilityViolationException(string message) : base("LONG_HORIZON_LOCKED_TARGET_IMMUTABILITY_VIOLATION", message) { }
}

/// <summary>Phase 10K-FREQ.6D.13 — a session's persisted lineage carries exactly one of PrescriptionProfileKey/Version, never one alone. Fail closed; never silently normalized to Legacy.</summary>
internal sealed class LongHorizonPartialProfileLineageException : LongHorizonRollingContractException
{
    public LongHorizonPartialProfileLineageException(string message) : base("LONG_HORIZON_PARTIAL_PROFILE_LINEAGE", message) { }
}

/// <summary>Phase 10K-FREQ.6D.13 — two sessions in the same logical week share an invalid canonical (StructuralRole, LaneOrdinal, SlotOrdinal) identity.</summary>
internal sealed class LongHorizonDuplicateSessionIdentityException : LongHorizonRollingContractException
{
    public LongHorizonDuplicateSessionIdentityException(string message) : base("LONG_HORIZON_DUPLICATE_SESSION_IDENTITY", message) { }
}

/// <summary>
/// Phase 10K-FREQ.6D.13 — the canonical lineage invariants FREQ.6D.11 approved,
/// enforced once at the exact persistence write boundary (mirroring
/// <c>CatalogSessionPrescriptionPlanner.ResolvePrescriptionSource</c>'s
/// existing both-null-or-both-present profile-pair check, §17/§38 of the
/// FREQ.6D.11/FREQ.6D.12 reports) rather than a second, LongHorizon-specific
/// weaker semantics.
/// </summary>
internal static class LongHorizonLineageValidator
{
    public static void ValidateProfilePair(string? profileKey, int? profileVersion, int globalWeekNumber, string sessionRole)
    {
        if (profileKey is null != profileVersion is null)
        {
            throw new LongHorizonPartialProfileLineageException(
                $"Week {globalWeekNumber} {sessionRole}: partial prescription-profile lineage " +
                $"(ProfileKey='{profileKey}', ProfileVersion={profileVersion}) — a session must carry both exact fields " +
                "(ProfileBacked) or neither (Legacy), never one alone.");
        }
    }

    public static void ValidateNoDuplicateIdentity(
        int globalWeekNumber, IReadOnlyList<LongHorizonSessionPrescriptionReference> sessions)
    {
        // Only sessions that actually carry the new lineage (SlotOrdinal
        // non-null -- populated by the real Core JIT path, §28/§56) are
        // checked. Runway/GE-sourced sessions never populate it (their own
        // construction sites are unchanged, §40 -- SlotOrdinal is not a
        // meaningless null forced onto every session, it is legitimately
        // absent for a source that doesn't produce it yet), so treating
        // every all-null session as one colliding identity would reject
        // ordinary, already-valid weeks with multiple EASY_SUPPORT slots.
        var seen = new HashSet<(string Role, int Lane, int Slot)>();
        foreach (var session in sessions)
        {
            if (session.SlotOrdinal is not { } slot) continue;
            var key = (session.SessionRole, session.LaneOrdinal ?? -1, slot);
            if (!seen.Add(key))
            {
                throw new LongHorizonDuplicateSessionIdentityException(
                    $"Week {globalWeekNumber}: duplicate canonical identity (StructuralRole={key.Item1}, " +
                    $"LaneOrdinal={session.LaneOrdinal}, SlotOrdinal={key.Item3}) across two sessions in the same week.");
            }
        }
    }
}
