using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 Part 23 -- translates a real, already-succeeded dark
/// activation/composition result into a persistence request and delegates
/// to the repository. Never recomputes numeric or calendar fields -- every
/// value it writes is copied verbatim from the runtime result.
/// </summary>
internal sealed class LongHorizonRollingActivationPersistenceAdapter
{
    private readonly ILongHorizonRollingStateRepository _repository;

    public LongHorizonRollingActivationPersistenceAdapter(ILongHorizonRollingStateRepository repository) => _repository = repository;

    /// <summary>
    /// Phase 4L.2C root-cause fix: the Runway prescription/target-lock/calendar
    /// projection graphs contain ValueTuple-typed properties (e.g.
    /// <c>LockedForActivatedRunwayWeekRange</c>). System.Text.Json's default
    /// options only serialize/deserialize properties, not public fields, and
    /// ValueTuple exposes its data as public fields (Item1/Item2) -- so a
    /// default-options round-trip silently zeroes them out. This options
    /// instance must be used symmetrically at every serialize call here and
    /// every matching deserialize call in LongHorizonRollingStateReconstructionService.
    /// </summary>
    internal static readonly JsonSerializerOptions FullFidelityJsonOptions = new() { IncludeFields = true };

    /// <summary>GE-window activation (Phase 4K.7 checkpoint runtime result).</summary>
    public Task<LongHorizonRollingPersistenceResult> PersistGeCheckpointAsync(
        Guid planStateId, uint expectedConcurrencyVersion, LongHorizonRollingCheckpointResult result, CancellationToken cancellationToken = default)
    {
        if (result.ActivationWindow is null)
            throw new InvalidOperationException("Cannot persist a checkpoint result with no ActivationWindow.");

        LongHorizonCheckpointRecordInput? checkpointInput = result.CheckpointDecision is { } decision && result.EvidenceSnapshot is { } evidence
            ? new LongHorizonCheckpointRecordInput
            {
                AsOfDate = evidence.CheckpointDate,
                SourceWindowStartWeek = evidence.ActivatedWindowStartWeek,
                SourceWindowEndWeek = evidence.ActivatedWindowEndWeek,
                EvidenceFingerprint = EvidenceFingerprint(evidence),
                ValidatedWeeklyVolumeKm = result.ValidatedLoad?.WeeklyVolumeKm,
                ValidatedLongRunKm = result.ValidatedLoad?.LongRunKm,
                AuthorityClassification = decision.Outcome.ToString(),
                Decision = decision.Outcome switch
                {
                    LongHorizonCheckpointOutcome.GrowthEligible => LongHorizonPersistedCheckpointDecision.GrowthEligible,
                    LongHorizonCheckpointOutcome.MaintenanceOnly => LongHorizonPersistedCheckpointDecision.MaintenanceOnly,
                    _ => LongHorizonPersistedCheckpointDecision.Blocked,
                },
                AuthoritativeReasonCode = decision.AuthoritativeReason?.Code,
            }
            : null;

        var request = new LongHorizonRollingActivationPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = expectedConcurrencyVersion,
            ActivatedWindow = result.ActivationWindow,
            LifecycleStates = result.LifecycleStates,
            ContextVersion = result.ContextVersion,
            CheckpointDecisionId = result.CheckpointDecision?.DecisionId,
            Checkpoint = checkpointInput,
            IdempotencyKey = $"activation:{planStateId}:{result.ActivationWindow.WindowId}:{result.ContextVersion.Sequence}",
        };

        return _repository.SaveActivationSuccessAsync(request, cancellationToken);
    }

    /// <summary>Runway/Core JIT composition activation (Phase 4K.8C composition result).</summary>
    public Task<LongHorizonRollingPersistenceResult> PersistJitCompositionAsync(
        Guid planStateId, uint expectedConcurrencyVersion, LongHorizonRollingJitCompositionResult result, CancellationToken cancellationToken = default)
    {
        if (result.ActivationResult?.RunwayPrescription is null && result.ActivationResult?.NewlyActivatedWeeks is null)
        {
            // Still allow pure GE-continuation compositions to fall through with no Runway/Core payload.
        }
        var activation = result.ActivationResult ?? throw new InvalidOperationException("Cannot persist a composition result with no ActivationResult.");

        LongHorizonRunwayStateInput? runwayInput = activation.RunwayPrescription is { } prescription
            ? new LongHorizonRunwayStateInput
            {
                TargetLockId = activation.CoreTargetLock?.CreatedByDecisionId ?? Guid.NewGuid(),
                TargetContextVersionSequence = result.ContextVersion.Sequence,
                LockedRunwayStartGlobalWeek = prescription.StartGlobalWeek,
                LockedRunwayEndGlobalWeek = prescription.EndGlobalWeek,
                CoreWeekOneWeeklyTargetKm = activation.CoreTargetLock?.TargetWeeklyVolumeKm ?? 0,
                CoreWeekOneLongRunTargetKm = activation.CoreTargetLock?.TargetLongRunKm ?? 0,
                FullPrescriptionId = prescription.PrescriptionId.ToString()!,
                FullPrescriptionVersion = prescription.PrescriptionVersion.Version.Sequence,
                // Full-fidelity round-trip: the real ImmutablePreparationRunwayPrescription
                // is serialized directly (not a hand-summarized shape), so reconstruction
                // deserializes the exact same object rather than an approximation.
                PrescriptionPayloadJson = JsonSerializer.Serialize(prescription, FullFidelityJsonOptions),
                CalendarCompositionIdentity = result.RealCompositionResult?.CalendarComposition?.GetType().Name ?? "Unavailable",
                CalendarProjectionPayloadJson = result.FullRunwayCalendarProjection is { } projection
                    ? JsonSerializer.Serialize(projection, FullFidelityJsonOptions)
                    : null,
                TargetLockPayloadJson = activation.CoreTargetLock is { } targetLock
                    ? JsonSerializer.Serialize(targetLock, FullFidelityJsonOptions)
                    : null,
            }
            : null;

        LongHorizonCoreContextInput? coreInput = result.BoundedCoreSelection is { } core
            ? new LongHorizonCoreContextInput
            {
                CoreContextId = core.CoreContextId,
                ContextVersionSequence = core.CoreContextVersion.Sequence,
                EffectiveFromGlobalWeek = core.SelectedStartGlobalWeek,
                EffectiveToGlobalWeek = core.SelectedEndGlobalWeek,
                AsOfDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ConditionResultSummaryJson = JsonSerializer.Serialize(result.ResolvedConditionResults?.Select(r => r.ConditionType) ?? []),
                ValidatedLoadAuthoritySummary = core.FullCoreResultProvenance,
                GeneratedCoreResultIdentity = core.FullCoreResultProvenance,
                SelectedCoreWeeksPayloadJson = JsonSerializer.Serialize(core.SelectedWeeks.Select(w => new { w.GlobalWeekNumber, w.WeeklyVolumeKm, w.LongRunKm, w.Stage })),
            }
            : null;

        var request = new LongHorizonRollingActivationPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = expectedConcurrencyVersion,
            ActivatedWindow = activation.ActivationWindow ?? throw new InvalidOperationException("No ActivationWindow on ActivationResult."),
            LifecycleStates = activation.LifecycleStates,
            ContextVersion = result.ContextVersion,
            RunwayState = runwayInput,
            CoreContext = coreInput,
            IdempotencyKey = $"jit:{planStateId}:{activation.ActivationWindow!.WindowId}:{result.ContextVersion.Sequence}",
        };

        return _repository.SaveActivationSuccessAsync(request, cancellationToken);
    }

    internal static string EvidenceFingerprint(LongHorizonCheckpointEvidenceSnapshot snapshot)
    {
        var raw = $"{snapshot.CheckpointDate:O}|{snapshot.ActivatedWindowStartWeek}|{snapshot.ActivatedWindowEndWeek}|{snapshot.CheckpointId}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
