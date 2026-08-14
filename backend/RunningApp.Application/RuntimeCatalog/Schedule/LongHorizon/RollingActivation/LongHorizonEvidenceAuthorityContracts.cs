namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5 — where a piece of rolling-activation evidence came from.
/// Distinct from <see cref="LongHorizonEvidenceAuthorityStatus"/>: a source
/// says WHAT the evidence is; an authority status says whether it may
/// currently be trusted as the rolling runtime's final answer.
/// </summary>
internal enum LongHorizonEvidenceSource
{
    CompletedTrainingHistory,
    PriorValidatedCheckpointLoad,
    OriginalOnboardingEvidence,
    PlannedGeneralEnduranceExit,
    RuntimeConditionResolution,
    ProductAverageTargetTime,
    RecentRaceEvidence,
    UserExplicitTargetTime,
}

/// <summary>
/// Phase 4K.5 — whether a given <see cref="LongHorizonEvidenceSource"/> may
/// be treated as the rolling runtime's final answer for a given field.
/// <see cref="LegacyCurrentProductionSource"/> and
/// <see cref="UnresolvedForRollingRuntime"/> are deliberately NOT the same
/// thing as <see cref="Authoritative"/> -- see
/// <see cref="LongHorizonEvidenceAuthorityRecord.Create"/>, which refuses to
/// construct a record that silently conflates them.
/// </summary>
internal enum LongHorizonEvidenceAuthorityStatus
{
    /// <summary>Approved as the final rolling-runtime answer for this field.</summary>
    Authoritative,

    /// <summary>Not the primary answer, but an approved fallback when the primary is unavailable.</summary>
    FallbackApproved,

    /// <summary>Retained for consistency-checking/provenance display only -- never used to compute a numeric value.</summary>
    ProvenanceOnly,

    /// <summary>Describes what today's unmodified, non-rolling production code actually does -- not a statement that the rolling runtime should do the same.</summary>
    LegacyCurrentProductionSource,

    /// <summary>The rolling runtime's final authority for this field has not been approved by any governance decision. Must never be silently treated as <see cref="Authoritative"/>.</summary>
    UnresolvedForRollingRuntime,
}

/// <summary>
/// Phase 4K.5 — a typed (source, authority-status) pair, with a guard
/// (<see cref="Create"/>) that refuses the one combination Phase 4K.5's own
/// "Critical Authority Distinction" section forbids: marking
/// <see cref="LongHorizonEvidenceSource.OriginalOnboardingEvidence"/> as
/// <see cref="LongHorizonEvidenceAuthorityStatus.Authoritative"/>. Phase
/// 4K.5A resolved <c>TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001</c>
/// by classifying onboarding evidence as legacy provenance only, so this
/// guard remains valid after closure rather than depending on an OPEN-TD
/// state.
/// </summary>
internal sealed record LongHorizonEvidenceAuthorityRecord
{
    public required LongHorizonEvidenceSource Source { get; init; }
    public required LongHorizonEvidenceAuthorityStatus AuthorityStatus { get; init; }
    public string? Note { get; init; }

    /// <summary>
    /// The only supported construction path. Deliberately not a public
    /// positional record constructor -- every call site must go through
    /// this guard.
    /// </summary>
    public static LongHorizonEvidenceAuthorityRecord Create(
        LongHorizonEvidenceSource source,
        LongHorizonEvidenceAuthorityStatus authorityStatus,
        string? note = null)
    {
        if (authorityStatus == LongHorizonEvidenceAuthorityStatus.Authoritative
            && source == LongHorizonEvidenceSource.OriginalOnboardingEvidence)
        {
            throw new LongHorizonEvidenceAuthorityDefaultingException(
                "OriginalOnboardingEvidence cannot be marked Authoritative for rolling-runtime evidence resolution -- " +
                "Phase 4K.5A classifies it as legacy provenance only. Use LegacyCurrentProductionSource " +
                "to describe today's unmodified pipeline; rolling Core resolution uses current validated " +
                "checkpoint evidence through the unchanged Core generator input boundary.");
        }

        return new LongHorizonEvidenceAuthorityRecord
        {
            Source = source,
            AuthorityStatus = authorityStatus,
            Note = note,
        };
    }
}

/// <summary>
/// Phase 4K.5 — the approved (Phase 4K.4/4K.5) evidence-source/authority
/// mapping for every rolling-activation evidence field this phase's parent
/// prompt names explicitly. A static catalog rather than per-call-site
/// literals, so the approved mapping exists in exactly one place.
/// </summary>
internal static class LongHorizonEvidenceAuthorityCatalog
{
    /// <summary>Approved rolling direction (Phase 4K.4 §12, Phase 4K.5): Runway entry weekly-volume evidence is the last completed GE checkpoint's validated actual load.</summary>
    public static LongHorizonEvidenceAuthorityRecord RunwayRollingWeeklyLoadAuthority { get; } =
        LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.CompletedTrainingHistory,
            LongHorizonEvidenceAuthorityStatus.Authoritative,
            "Phase 4K.2's ValidatedSustainableWeeklyVolumeKm from the last completed GE checkpoint.");

    /// <summary>Fallback when the most recent checkpoint's own evidence is stale (Phase 4K.3 §6/§9): a prior still-valid validated checkpoint load.</summary>
    public static LongHorizonEvidenceAuthorityRecord RunwayRollingWeeklyLoadFallback { get; } =
        LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.PriorValidatedCheckpointLoad,
            LongHorizonEvidenceAuthorityStatus.FallbackApproved,
            "Used only when the current window's own evidence is not fresh enough (Phase 4K.3).");

    /// <summary>Mirrors <see cref="RunwayRollingWeeklyLoadAuthority"/> for long-run evidence.</summary>
    public static LongHorizonEvidenceAuthorityRecord RunwayRollingLongRunAuthority { get; } =
        LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.CompletedTrainingHistory,
            LongHorizonEvidenceAuthorityStatus.Authoritative,
            "Phase 4K.2's ValidatedSustainableLongRunKm from the last completed GE checkpoint.");

    /// <summary>Planned GE exit retained for provenance/consistency-checking only -- never used to compute the rolling Runway entry value (Phase 4K.4 §12).</summary>
    public static LongHorizonEvidenceAuthorityRecord PlannedGeExitProvenance { get; } =
        LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.PlannedGeneralEnduranceExit,
            LongHorizonEvidenceAuthorityStatus.ProvenanceOnly,
            "May be retained for consistency checks; must never override lower actual capacity nor force Runway upward (Phase 4K.4 §12).");

    /// <summary>What today's unmodified, non-rolling production code actually does: LongHorizonGeExitState.From derives Runway entry from planned GE progression. Unchanged by Phase 4K.4/4K.5.</summary>
    public static LongHorizonEvidenceAuthorityRecord PlannedGeExitLegacyProductionSource { get; } =
        LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.PlannedGeneralEnduranceExit,
            LongHorizonEvidenceAuthorityStatus.LegacyCurrentProductionSource,
            "LongHorizonGeExitState.From remains valid only for the non-rolling/full-upfront execution path (Phase 4K.4 §12).");

    /// <summary>What today's unmodified production code actually does: Core's Week-1 target is derived from original onboarding evidence via the existing TenKPreparationRunwayCoreGenerator/DynamicCoreCalendarMaterializationOrchestrator pipeline. Unchanged by Phase 4K.4/4K.5.</summary>
    public static LongHorizonEvidenceAuthorityRecord CoreWeekOneCurrentProductionSource { get; } =
        LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.OriginalOnboardingEvidence,
            LongHorizonEvidenceAuthorityStatus.LegacyCurrentProductionSource,
            "Describes today's unmodified pipeline only -- not a claim that this remains correct for the rolling runtime.");

    /// <summary>
    /// The rolling runtime's approved Core Week-1 evidence authority (Phase
    /// 4K.5A, Option A). The latest valid checkpoint's
    /// ValidatedSustainableLoad is mapped to RecentWeeklyVolumeKm and
    /// RecentLongestRunKm at the existing Core input boundary; the unchanged
    /// Core generator remains the final target authority.
    /// </summary>
    public static LongHorizonEvidenceAuthorityRecord CoreWeekOneRollingAuthority { get; } =
        LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.CompletedTrainingHistory,
            LongHorizonEvidenceAuthorityStatus.Authoritative,
            "Phase 4K.5A Option A: latest valid checkpoint evidence enters the existing unchanged Core generator through RecentWeeklyVolumeKm/RecentLongestRunKm; original onboarding evidence is legacy provenance only.");

    /// <summary>RuntimeConditionResolutionService's own inputs (race date, target finish time) are fixed from plan creation -- re-resolved, not re-sourced, at each JIT checkpoint (Phase 4K.4 §17).</summary>
    public static LongHorizonEvidenceAuthorityRecord PaceAndTargetTimeAuthority { get; } =
        LongHorizonEvidenceAuthorityRecord.Create(
            LongHorizonEvidenceSource.RuntimeConditionResolution,
            LongHorizonEvidenceAuthorityStatus.Authoritative,
            "Existing, unmodified RuntimeConditionResolutionService, re-resolved with a current AsOfDate at each JIT checkpoint (Phase 4K.4 §17).");
}

/// <summary>
/// Phase 4K.5 — how a numeric window's context was produced, kept distinct
/// from checkpoint-derived validated load so an initial plan's onboarding
/// evidence is never misclassified as checkpoint evidence (Phase 4K.3 §16/§19,
/// Phase 4K.5 Part 14).
/// </summary>
internal enum LongHorizonInitialActivationSource
{
    InitialOnboardingActivation,
    CheckpointRollingActivation,
    RunwayJitActivation,
    CoreFutureRefresh,
}
