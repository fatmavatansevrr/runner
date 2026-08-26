using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>Phase 4K.8's five typed outcomes.</summary>
internal enum LongHorizonRollingJitActivationOutcome
{
    GeRunwayMixedWindowActivated,
    RunwayWindowActivated,
    RunwayCoreMixedWindowActivated,
    CoreWindowActivated,
    JitWindowBlocked,
}

/// <summary>
/// Phase 4K.8 — the runtime's request. Deliberately accepts, rather than
/// internally computes, every "existing, unchanged pipeline" output the
/// phase's own prompt names: <see cref="ConditionResults"/> (the caller's
/// own <c>RuntimeConditionResolutionService.ResolveAllResults</c> call --
/// exactly mirroring how the real, unmodified
/// <c>TenKPreparationRunwayCoreGenerationRequest</c>/<c>DynamicCoreCalendarMaterializationContext</c>
/// already take this as a pre-resolved input, never compute it themselves),
/// <see cref="ResolvedCoreWeekOneTarget"/> (the caller's own
/// <c>TenKPreparationRunwayCoreGenerator</c> + <c>PreparationRunwayCoreWeekOneTargetAdapter</c>
/// call), and <see cref="AvailableCoreWeeks"/> (the caller's own real Core
/// generation output, already-numeric, that this runtime only selects/bounds
/// from -- Phase 4K.8's own Part 14 explicitly permits "a bounded adapter").
/// This runtime's own, genuinely new logic is window selection, the
/// direction guard, full Runway materialization/locking/bounded exposure,
/// GE/Runway/Core segment stitching, and atomicity/lifecycle/versioning.
/// </summary>
internal sealed record LongHorizonRollingJitActivationRequest
{
    public required LongHorizonStructuralRoadmap StructuralRoadmap { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public required RollingNumericActivationWindow PreviousActivatedWindow { get; init; }
    public required LongHorizonCheckpointEvidenceSnapshot EvidenceSnapshot { get; init; }
    public required ValidatedSustainableLoad ValidatedLoad { get; init; }
    public LongHorizonCheckpointDecision? GeCheckpointDecision { get; init; }

    /// <summary>
    /// The GE portion's already-materialized weeks, produced by the same
    /// real GE Growth/Maintenance materializers Phase 4K.7's own checkpoint
    /// runtime uses -- this runtime never recomputes GE numeric progression,
    /// it only stitches these into the same atomic mixed window as the
    /// Runway portion. Required exactly when the selected window's start
    /// segment is GeneralEndurance.
    /// </summary>
    public IReadOnlyList<ActivatedNumericWeek>? GeActivatedWeeks { get; init; }
    public required LongHorizonContextVersion PreviousContextVersion { get; init; }
    public required ReadinessProfile ReadinessProfile { get; init; }
    public required IReadOnlyList<DayOfWeek> CurrentAvailability { get; init; }
    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required DateOnly CheckpointDate { get; init; }
    public required DateOnly RaceDate { get; init; }
    public required LongHorizonSafetyState SafetyState { get; init; }
    public required IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults { get; init; }

    /// <summary>Phase 10K-FREQ.6D.13 — the candidate's own resolved session cardinality, replacing the prior hardcoded 4-day availability assumption. Defaults to 4 for every existing caller (zero-delta).</summary>
    public int DaysPerWeek { get; init; } = 4;

    // Populated only when the selected window includes any Runway week AND
    // no compatible ExistingRunwayPrescription is supplied (first Runway entry).
    public PreparationRunwayCoreWeekOneNumericTarget? ResolvedCoreWeekOneTarget { get; init; }
    public PreparationRunwayStartingLoadEvidence? RunwayStartingLoadEvidence { get; init; }
    public IReadOnlyList<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>>? RunwayStructuralWeeks { get; init; }

    // Reused, never regenerated, on Runway-continuation windows.
    public LongHorizonLockedCoreWeekOneTarget? ExistingLockedCoreTarget { get; init; }
    public ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>? ExistingRunwayPrescription { get; init; }

    // Populated only when the selected window includes any Core week -- the
    // caller's own real, unchanged Core generation output (already numeric),
    // from which this runtime only selects/bounds the requested weeks.
    public IReadOnlyList<LongHorizonJitCoreCandidateWeek>? AvailableCoreWeeks { get; init; }

    /// <summary>The overall plan's Week-1 start date, needed for calendar continuity across all three segments.</summary>
    public required DateOnly PlanStartDate { get; init; }
}

/// <summary>
/// Phase 4K.8 Part 14 — a single Core week candidate from the caller's own
/// real, unchanged Core generation output, not yet activated. Deliberately
/// NOT an <see cref="ActivatedNumericWeek"/> -- that type's own validator
/// requires NumericPending weeks to carry only null executable fields, and
/// this candidate legitimately carries real (already-computed) numeric
/// values while still being unselected/non-executable. Only the runtime's
/// own selection converts a chosen candidate into a real NumericActivated
/// <see cref="ActivatedNumericWeek"/>.
/// </summary>
internal sealed record LongHorizonJitCoreCandidateWeek
{
    public required int GlobalWeekNumber { get; init; }
    public required double WeeklyVolumeKm { get; init; }
    public required double LongRunKm { get; init; }
    public required IReadOnlyList<LongHorizonSessionPrescriptionReference> SessionPrescriptions { get; init; }
    public required string Stage { get; init; }
}

/// <summary>Phase 4K.8 Part 20 — the runtime's result. Never exposed publicly.</summary>
internal sealed record LongHorizonRollingJitActivationResult
{
    public required LongHorizonRollingJitActivationOutcome Outcome { get; init; }
    public required IReadOnlyDictionary<int, LongHorizonNumericLifecycleState> LifecycleStates { get; init; }
    public RollingNumericActivationWindow? ActivationWindow { get; init; }
    public required IReadOnlyList<ActivatedNumericWeek> NewlyActivatedWeeks { get; init; }
    public LongHorizonJitContextDecision? JitContext { get; init; }
    public LongHorizonLockedCoreWeekOneTarget? CoreTargetLock { get; init; }
    public ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>? RunwayPrescription { get; init; }
    public BoundedPreparationRunwayPrescriptionSlice<PreparationRunwayBlockType>? RunwaySlice { get; init; }
    public required LongHorizonContextVersion ContextVersion { get; init; }
    public LongHorizonReasonCode? AuthoritativeReason { get; init; }
    public required IReadOnlyList<string> ValidationStages { get; init; }
}

/// <summary>
/// Phase 4K.8 Part 4/Part 5A.6 — the exact Phase 4K.5A-approved mapping from
/// checkpoint-validated evidence to the existing, unchanged Core generator's
/// onboarding-shaped input fields. A pure record, no behavior -- the actual
/// invocation of the real Core generator using these values remains the
/// caller's responsibility (see <see cref="LongHorizonRollingJitActivationRequest"/>'s
/// own doc comment).
/// </summary>
internal sealed record LongHorizonJitCoreInputMapping
{
    public required double RecentWeeklyVolumeKm { get; init; }
    public required double RecentLongestRunKm { get; init; }
    public int? RecentRunsPerWeek { get; init; }
    public required LongHorizonEvidenceAuthorityRecord WeeklyVolumeAuthority { get; init; }
    public required LongHorizonEvidenceAuthorityRecord LongRunAuthority { get; init; }
}

/// <summary>Phase 4K.8 Part 4 — builds <see cref="LongHorizonJitCoreInputMapping"/> from a validated load, never from onboarding evidence.</summary>
internal static class LongHorizonJitCoreInputAdapter
{
    public static LongHorizonJitCoreInputMapping Map(ValidatedSustainableLoad validatedLoad, int? exactCompletedFrequency)
    {
        if (validatedLoad.ValidationStatus != LongHorizonValidationStatus.Valid
            || validatedLoad.WeeklyVolumeKm is null || validatedLoad.LongRunKm is null)
        {
            throw new LongHorizonJitContextInvalidException(
                "LongHorizonJitCoreInputAdapter.Map requires a Valid ValidatedSustainableLoad with non-null WeeklyVolumeKm/LongRunKm.");
        }

        return new LongHorizonJitCoreInputMapping
        {
            RecentWeeklyVolumeKm = validatedLoad.WeeklyVolumeKm.Value,
            RecentLongestRunKm = validatedLoad.LongRunKm.Value,
            RecentRunsPerWeek = exactCompletedFrequency,
            WeeklyVolumeAuthority = validatedLoad.WeeklyLoadSource,
            LongRunAuthority = validatedLoad.LongRunSource,
        };
    }
}
