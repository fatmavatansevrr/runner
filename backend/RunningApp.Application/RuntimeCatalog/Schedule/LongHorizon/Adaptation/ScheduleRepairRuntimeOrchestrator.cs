using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.3 -- live NotToday -> adaptation orchestration (Rev3.1 §3/§4.1).
/// This is the ONLY new orchestration surface: it maps the runtime reason,
/// short-circuits the candidate query when the outcome is already
/// structurally certain (§I), queries real candidates when needed, and calls
/// the frozen 4M.1 <see cref="ScheduleRepairPolicy"/> / 4M.2
/// <see cref="ScheduleRepairPersistenceService"/> authorities unchanged. It
/// owns no product rule of its own -- every branch here either reuses an
/// existing authority call or is pure plumbing (DTO/exception translation).
/// </summary>
internal sealed class ScheduleRepairRuntimeOutcome
{
    public required LongHorizonScheduleRepairActionKind Action { get; init; }
    public required bool SafetyFlag { get; init; }
    public required bool IsIdempotentReplay { get; init; }
    public Guid? ReplacementSessionId { get; init; }
    public DateOnly? ReplacementDate { get; init; }
}

internal enum LongHorizonScheduleRepairActionKind
{
    Skip,
    RescheduleToEmptySlot,
    SubstituteFutureEasy,
}

internal static class ScheduleRepairRuntimeOrchestrator
{
    /// <summary>
    /// Runs steps 3-9 of the live orchestration chain against an
    /// already-committed-or-replayed NotToday trigger session. Safe to call
    /// on both a freshly-committed NotToday transition and an
    /// IdempotentReplay of one: <see cref="ScheduleRepairPersistenceService"/>
    /// is itself idempotent on TriggerSessionId, which is exactly what makes
    /// this call safely re-runnable if a prior attempt's second (adaptation)
    /// transaction failed after the first (NotToday) transaction had already
    /// committed -- see Phase 4M.3 doc §27.
    /// </summary>
    public static async Task<ScheduleRepairRuntimeOutcome> RunAsync(
        AppDbContext db, ILoggerFactory loggerFactory, LongHorizonRollingSessionState trigger, CancellationToken ct)
    {
        var aggregate = trigger.Week.Plan;

        var role = AdaptationSessionRoleResolver.Resolve(trigger.SessionRole);

        var meaning = RuntimeNotTodayReasonMapper.Map(trigger.NotTodayReason
            ?? throw new LongHorizonAdaptationIntegrityViolationException("Trigger session has no not-today reason recorded."));
        var reasonCode = RuntimeNotTodayReasonMapper.ToReasonCode(meaning);
        var phase = new AdaptationPhaseIdentity(trigger.Week.SegmentType, trigger.Week.Stage);
        var isTaper = string.Equals(trigger.Week.Stage, nameof(TrainingWeekType.Taper), StringComparison.Ordinal);

        var repairTrigger = new ScheduleRepairTrigger(trigger.Id, role, phase, isTaper, reasonCode, trigger.OutcomeStatus);

        // Section I short-circuit: reuse the exact same canonical preconditions
        // ScheduleRepairPolicy itself checks before ever touching a candidate
        // list, so an empty-candidate call to the policy below is guaranteed to
        // produce the identical Skip decision a real query would have produced.
        var needsCandidateQuery = role == PreparationRunwaySlotRole.KeySession
            || (role == PreparationRunwaySlotRole.LongRun && !isTaper);
        needsCandidateQuery &= !ReasonClassificationPolicy.BlocksReschedule(reasonCode);

        IReadOnlyList<ScheduleRepairCandidate> emptySlotCandidates = Array.Empty<ScheduleRepairCandidate>();
        IReadOnlyList<ScheduleRepairCandidate> futureEasyCandidates = Array.Empty<ScheduleRepairCandidate>();
        if (needsCandidateQuery)
        {
            emptySlotCandidates = ScheduleRepairCandidateProvider.GetEmptySlotCandidates(aggregate, trigger);
            futureEasyCandidates = ScheduleRepairCandidateProvider.GetFutureEasySubstitutionCandidates(aggregate, trigger);
        }

        var decision = ScheduleRepairPolicy.Evaluate(repairTrigger, emptySlotCandidates, futureEasyCandidates);

        Guid? targetWeekStateId = null;
        if (decision.Action == ScheduleRepairAction.RescheduleToEmptySlot)
        {
            var targetDate = decision.SelectedEmptySlotDate
                ?? throw new LongHorizonAdaptationIntegrityViolationException("RescheduleToEmptySlot decision is missing its selected date.");
            targetWeekStateId = aggregate.Weeks
                .SingleOrDefault(w => targetDate >= w.StructuralStartDate && targetDate <= w.StructuralEndDate)?.Id
                ?? throw new LongHorizonAdaptationIntegrityViolationException("Selected empty-slot date does not fall inside any known week.");
        }

        var persistenceRequest = new ScheduleRepairPersistenceRequest(
            trigger.Week.PlanStateId, repairTrigger, decision,
            aggregate.CurrentWindowStartWeek, aggregate.CurrentWindowEndWeek, targetWeekStateId);

        // The candidate query above is already done and its result (decision)
        // is fixed -- everything ScheduleRepairPersistenceService needs from
        // here on is a genuinely fresh read. LoadOwnedAsync eager-loads the
        // whole plan's session graph (including any future EASY_SUPPORT
        // session that could become a live substitution target) into this
        // same DbContext, so without clearing the tracker here,
        // ScheduleRepairPersistenceService's own "revalidate at commit time"
        // queries would resolve to the already-tracked (potentially stale)
        // in-memory instances via EF's identity map instead of the real
        // current row -- silently defeating its StaleTarget/StaleTrigger
        // guarantee. This is a 4M.3-side integration fix only: it does not
        // change one line of the frozen 4M.2 persistence service itself.
        db.ChangeTracker.Clear();

        var persistenceService = new ScheduleRepairPersistenceService(db, loggerFactory.CreateLogger<ScheduleRepairPersistenceService>());
        var result = await persistenceService.PersistAsync(persistenceRequest, ct);

        switch (result.Outcome)
        {
            case AdaptationPersistenceOutcome.Committed:
            case AdaptationPersistenceOutcome.IdempotentReplay:
                return await BuildOutcomeAsync(db, result, decision.SafetyFlag,
                    isReplay: result.Outcome == AdaptationPersistenceOutcome.IdempotentReplay, ct);
            case AdaptationPersistenceOutcome.StaleTarget:
                throw new LongHorizonAdaptationStaleTargetException(result.FailureReason ?? "The selected schedule-repair target is no longer valid.");
            case AdaptationPersistenceOutcome.StaleTrigger:
                throw new LongHorizonAdaptationStaleTriggerException(result.FailureReason ?? "The trigger session is no longer eligible for schedule repair.");
            case AdaptationPersistenceOutcome.ConcurrencyConflict:
                throw new LongHorizonAdaptationConcurrencyConflictException(result.FailureReason ?? "A concurrent schedule-repair commit could not be resolved.");
            case AdaptationPersistenceOutcome.IntegrityViolation:
                throw new LongHorizonAdaptationIntegrityViolationException(result.FailureReason ?? "Schedule repair could not be persisted due to an internal integrity violation.");
            default:
                throw new LongHorizonAdaptationIntegrityViolationException("Unrecognized schedule-repair persistence outcome.");
        }
    }

    private static async Task<ScheduleRepairRuntimeOutcome> BuildOutcomeAsync(
        AppDbContext db, AdaptationPersistenceResult result, bool safetyFlag, bool isReplay, CancellationToken ct)
    {
        // The persisted facts (ReplacementSessionId/SupersededSessionId), not the
        // freshly recomputed decision, are the source of truth for what action
        // actually committed -- this is correct for both Committed and
        // IdempotentReplay alike (see this file's own doc comment).
        var action = result.ReplacementSessionId is null
            ? LongHorizonScheduleRepairActionKind.Skip
            : result.SupersededSessionId is null
                ? LongHorizonScheduleRepairActionKind.RescheduleToEmptySlot
                : LongHorizonScheduleRepairActionKind.SubstituteFutureEasy;

        DateOnly? replacementDate = null;
        if (result.ReplacementSessionId is { } replacementId)
        {
            replacementDate = await db.LongHorizonRollingSessionStates.AsNoTracking()
                .Where(s => s.Id == replacementId).Select(s => s.AssignedDate).SingleAsync(ct);
        }

        return new ScheduleRepairRuntimeOutcome
        {
            Action = action,
            SafetyFlag = safetyFlag,
            IsIdempotentReplay = isReplay,
            ReplacementSessionId = result.ReplacementSessionId,
            ReplacementDate = replacementDate,
        };
    }
}
