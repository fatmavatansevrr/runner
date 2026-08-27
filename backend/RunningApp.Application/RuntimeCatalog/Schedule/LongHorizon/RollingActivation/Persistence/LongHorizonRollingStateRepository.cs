using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2 -- EF-Core-backed implementation. Uses the same AppDbContext
/// instance the rest of RunningApp.Application already injects directly
/// (no separate Infrastructure repository layer exists in this codebase --
/// confirmed absent by direct inspection, so this follows the established
/// convention rather than introducing a new one). Relies on EF's own
/// implicit per-SaveChangesAsync transaction for atomicity (the existing
/// codebase never uses explicit BeginTransaction either).
/// </summary>
internal sealed class LongHorizonRollingStateRepository : ILongHorizonRollingStateRepository
{
    private readonly AppDbContext _db;
    private readonly ILongHorizonPersistenceFailureInjector _failureInjector;
    private readonly ILongHorizonPersistenceConstraintMutation _constraintMutation;

    public LongHorizonRollingStateRepository(
        AppDbContext db,
        ILongHorizonPersistenceFailureInjector? failureInjector = null,
        ILongHorizonPersistenceConstraintMutation? constraintMutation = null)
    {
        _db = db;
        _failureInjector = failureInjector ?? NoOpLongHorizonPersistenceFailureInjector.Instance;
        _constraintMutation = constraintMutation ?? NoOpLongHorizonPersistenceConstraintMutation.Instance;
    }

    public async Task<LongHorizonRollingRestartSnapshot> InitializeStructuralStateAsync(
        LongHorizonRollingInitializationRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _db.LongHorizonRollingPlanStates
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanStateId, cancellationToken);
        if (existing is not null)
        {
            // Idempotent replay: structural state for this plan anchor already exists.
            return await ReconstructAsync(request.PlanStateId, request.CatalogRootPath, request.Candidate, cancellationToken)
                ?? throw new InvalidOperationException("Plan state row exists but failed to reconstruct.");
        }

        var roadmap = request.StructuralRoadmap;
        var now = DateTime.UtcNow;

        var plan = new LongHorizonRollingPlanState
        {
            Id = request.PlanStateId,
            TotalWeeks = roadmap.TotalWeeks,
            ReadinessProfile = roadmap.Profile.ToString(),
            StartDate = request.PlanStartDate,
            RaceDate = roadmap.RaceDate,
            GoalType = "Race",
            GoalDistance = "TenK",
            Level = "Intermediate",
            DaysPerWeek = request.DaysPerWeek,
            PreferredDaysCsv = string.Join(",", request.PreferredDays),
            LongRunDay = request.LongRunDay.ToString(),
            CandidateKey = request.Candidate.CandidateKey,
            CandidateVersion = request.Candidate.CandidateVersion,
            CatalogRootPath = request.CatalogRootPath,
            CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivated,
            CurrentWindowStartWeek = request.InitialWindow.StartGlobalWeek,
            CurrentWindowEndWeek = request.InitialWindow.EndGlobalWeek,
            LastActivatedGlobalWeek = request.InitialWindow.EndGlobalWeek,
            LatestCheckpointDate = null,
            ActiveContextVersionSequence = request.ContextVersion.Sequence,
            ActiveContextVersionId = request.ContextVersion.VersionId,
            RetryEligible = false,
            PersistenceContractVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        _db.LongHorizonRollingPlanStates.Add(plan);

        foreach (var globalWeek in roadmap.GlobalWeekNumbers.OrderBy(w => w))
        {
            var structuralWeek = roadmap.Weeks.Single(w => w.GlobalWeekNumber == globalWeek);
            var lifecycleState = request.LifecycleStates.GetValueOrDefault(globalWeek, LongHorizonNumericLifecycleState.StructurallyPlanned);
            var (start, end) = structuralWeek.CalendarRange
                ?? (request.PlanStartDate.AddDays((globalWeek - 1) * 7), request.PlanStartDate.AddDays((globalWeek - 1) * 7 + 6));

            var weekRow = new LongHorizonRollingWeekState
            {
                Id = Guid.NewGuid(),
                PlanStateId = plan.Id,
                GlobalWeek = globalWeek,
                SegmentType = MapSegment(structuralWeek.SegmentType),
                Stage = structuralWeek.Stage,
                StructuralStartDate = start,
                StructuralEndDate = end,
                LifecycleState = MapLifecycle(lifecycleState),
            };

            if (request.ActivatedWeeks.TryGetValue(globalWeek, out var activated) && lifecycleState == LongHorizonNumericLifecycleState.NumericActivated)
            {
                weekRow.WeeklyVolumeKm = activated.TotalWeeklyVolumeKm;
                weekRow.LongRunKm = activated.LongRunKm;
                weekRow.ActivationContextVersionSequence = request.ContextVersion.Sequence;
                weekRow.ActivatedAtUtc = now;

                if (activated.SessionPrescriptions is { } toValidate)
                    LongHorizonLineageValidator.ValidateNoDuplicateIdentity(globalWeek, toValidate);
                var ordinal = 0;
                foreach (var session in activated.SessionPrescriptions ?? [])
                {
                    LongHorizonLineageValidator.ValidateProfilePair(session.ProfileKey, session.ProfileVersion, globalWeek, session.SessionRole);
                    _db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
                    {
                        Id = Guid.NewGuid(),
                        WeekStateId = weekRow.Id,
                        SessionOrdinal = session.SessionOrdinal ?? ordinal++,
                        SessionRole = session.SessionRole,
                        LaneOrdinal = session.LaneOrdinal,
                        SlotOrdinal = session.SlotOrdinal,
                        ProgressionStageKey = session.ProgressionStageKey,
                        CatalogPrescriptionProfileKey = session.ProfileKey,
                        CatalogPrescriptionProfileVersion = session.ProfileVersion,
                        WorkoutKey = session.WorkoutKey,
                        WorkoutVersion = session.WorkoutVersion,
                        DistanceKm = session.DistanceKm,
                        AssignedDate = session.AssignedDate ?? start,
                        ActivationContextVersionSequence = request.ContextVersion.Sequence,
                        Provenance = session.Source ?? "InitialStructuralPersistence",
                    });
                }
            }

            _db.LongHorizonRollingWeekStates.Add(weekRow);
        }

        _db.LongHorizonActivationWindowRecords.Add(new LongHorizonActivationWindowRecord
        {
            Id = Guid.NewGuid(),
            PlanStateId = plan.Id,
            StartGlobalWeek = request.InitialWindow.StartGlobalWeek,
            EndGlobalWeek = request.InitialWindow.EndGlobalWeek,
            Outcome = LongHorizonPersistedActivationOutcome.Activated,
            ContextVersionSequence = request.ContextVersion.Sequence,
            ContextVersionId = request.ContextVersion.VersionId,
            ActivatedAtUtc = now,
            IdempotencyKey = $"init:{plan.Id}:{request.InitialWindow.StartGlobalWeek}-{request.InitialWindow.EndGlobalWeek}",
            ContractVersion = 1,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return await ReconstructAsync(plan.Id, request.CatalogRootPath, request.Candidate, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reconstruct snapshot immediately after initialization.");
    }

    public async Task<LongHorizonRollingRestartSnapshot?> LoadRestartSnapshotAsync(
        Guid planStateId, CancellationToken cancellationToken = default)
    {
        var plan = await _db.LongHorizonRollingPlanStates.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planStateId, cancellationToken);
        if (plan is null) return null;

        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = plan.CatalogRootPath });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        var candidate = await new CatalogCandidateEligibilityGate(loader)
            .LoadForInternalDryRunAsync(plan.CandidateKey, plan.CandidateVersion);

        return await ReconstructAsync(planStateId, plan.CatalogRootPath, candidate, cancellationToken);
    }

    public async Task<LongHorizonRollingPersistenceResult> SaveActivationSuccessAsync(
        LongHorizonRollingActivationPersistenceRequest request, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.LongHorizonActivationWindowRecords.AsNoTracking()
            .AnyAsync(a => a.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (duplicate)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.IdempotentReplay };
        }

        var plan = await _db.LongHorizonRollingPlanStates
            .Include(p => p.Weeks)
            .FirstOrDefaultAsync(p => p.Id == request.PlanStateId, cancellationToken);
        if (plan is null)
        {
            return new LongHorizonRollingPersistenceResult
            {
                Outcome = LongHorizonRollingPersistenceOutcome.IntegrityViolation,
                FailureReason = "Plan state not found.",
            };
        }

        SetOriginalConcurrencyToken(plan, request.ExpectedConcurrencyVersion);

        var activationOp = request.ActivatedWindow.SegmentsCovered.Contains(LongHorizonStructuralSegmentType.PreparationRunway)
            && request.ActivatedWindow.SegmentsCovered.Contains(LongHorizonStructuralSegmentType.Core)
                ? LongHorizonPersistenceOperation.MixedRunwayCoreActivation
                : request.ActivatedWindow.SegmentsCovered.Contains(LongHorizonStructuralSegmentType.Core)
                    ? LongHorizonPersistenceOperation.CoreOnlyActivation
                    : LongHorizonPersistenceOperation.InitialPersistence;
        _failureInjector.MaybeThrow(activationOp, LongHorizonPersistenceFailpoint.AfterVersionValidation, plan.Id);

        var now = DateTime.UtcNow;
        var window = request.ActivatedWindow;
        foreach (var week in window.Weeks)
        {
            var weekRow = plan.Weeks.SingleOrDefault(w => w.GlobalWeek == week.GlobalWeekNumber)
                ?? throw new InvalidOperationException($"No structural week row for global week {week.GlobalWeekNumber}.");

            if (weekRow.LifecycleState != LongHorizonPersistedLifecycleState.NumericPending
                && weekRow.LifecycleState != LongHorizonPersistedLifecycleState.StructurallyPlanned)
            {
                return new LongHorizonRollingPersistenceResult
                {
                    Outcome = LongHorizonRollingPersistenceOutcome.IntegrityViolation,
                    FailureReason = $"Week {week.GlobalWeekNumber} is {weekRow.LifecycleState}, not Pending -- cannot activate.",
                };
            }

            weekRow.LifecycleState = LongHorizonPersistedLifecycleState.NumericActivated;
            weekRow.WeeklyVolumeKm = week.TotalWeeklyVolumeKm;
            weekRow.LongRunKm = week.LongRunKm;
            weekRow.ActivationContextVersionSequence = request.ContextVersion.Sequence;
            weekRow.ActivatedAtUtc = now;
            weekRow.BlockedReasonCode = null;
            weekRow.BlockedDecisionId = null;

            if (week.SessionPrescriptions is { } toValidate)
                LongHorizonLineageValidator.ValidateNoDuplicateIdentity(week.GlobalWeekNumber, toValidate);
            var ordinal = 0;
            foreach (var session in week.SessionPrescriptions ?? [])
            {
                LongHorizonLineageValidator.ValidateProfilePair(session.ProfileKey, session.ProfileVersion, week.GlobalWeekNumber, session.SessionRole);
                _db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
                {
                    Id = Guid.NewGuid(),
                    WeekStateId = weekRow.Id,
                    SessionOrdinal = session.SessionOrdinal ?? ordinal++,
                    SessionRole = session.SessionRole,
                    LaneOrdinal = session.LaneOrdinal,
                    SlotOrdinal = session.SlotOrdinal,
                    ProgressionStageKey = session.ProgressionStageKey,
                    CatalogPrescriptionProfileKey = session.ProfileKey,
                    CatalogPrescriptionProfileVersion = session.ProfileVersion,
                    WorkoutKey = session.WorkoutKey,
                    WorkoutVersion = session.WorkoutVersion,
                    DistanceKm = session.DistanceKm,
                    AssignedDate = session.AssignedDate ?? weekRow.StructuralStartDate,
                    ActivationContextVersionSequence = request.ContextVersion.Sequence,
                    Provenance = session.Source ?? "ActivationPersistenceAdapter",
                });
            }
        }

        plan.CurrentWindowStartWeek = window.StartGlobalWeek;
        plan.CurrentWindowEndWeek = window.EndGlobalWeek;
        plan.LastActivatedGlobalWeek = window.EndGlobalWeek;
        plan.ActiveContextVersionSequence = request.ContextVersion.Sequence;
        plan.ActiveContextVersionId = request.ContextVersion.VersionId;
        plan.CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivated;
        plan.CurrentBlockedPublicReasonCategory = null;
        plan.CurrentBlockedInternalReasonCode = null;
        plan.BlockedAt = null;
        plan.RetryEligible = false;
        plan.UpdatedAtUtc = now;

        if (request.Checkpoint is { } checkpoint)
        {
            var checkpointExists = await _db.LongHorizonCheckpointRecords.AsNoTracking().AnyAsync(
                c => c.PlanStateId == plan.Id && c.AsOfDate == checkpoint.AsOfDate && c.SourceWindowStartWeek == checkpoint.SourceWindowStartWeek,
                cancellationToken);
            if (!checkpointExists)
            {
                _db.LongHorizonCheckpointRecords.Add(new LongHorizonCheckpointRecord
                {
                    Id = request.CheckpointDecisionId ?? Guid.NewGuid(),
                    PlanStateId = plan.Id,
                    AsOfDate = checkpoint.AsOfDate,
                    SourceWindowStartWeek = checkpoint.SourceWindowStartWeek,
                    SourceWindowEndWeek = checkpoint.SourceWindowEndWeek,
                    EvidenceFingerprint = checkpoint.EvidenceFingerprint,
                    ValidatedWeeklyVolumeKm = checkpoint.ValidatedWeeklyVolumeKm,
                    ValidatedLongRunKm = checkpoint.ValidatedLongRunKm,
                    CompletedFrequency = checkpoint.CompletedFrequency,
                    AuthorityClassification = checkpoint.AuthorityClassification,
                    Decision = checkpoint.Decision,
                    AuthoritativeReasonCode = checkpoint.AuthoritativeReasonCode,
                    ContextVersionSequence = request.ContextVersion.Sequence,
                });
                plan.LatestCheckpointDate = checkpoint.AsOfDate;
            }
        }

        if (request.RunwayState is { } runway)
        {
            var runwayExists = await _db.LongHorizonRunwayStates.AsNoTracking().AnyAsync(r => r.PlanStateId == plan.Id, cancellationToken);
            if (!runwayExists)
            {
                _db.LongHorizonRunwayStates.Add(new LongHorizonRunwayState
                {
                    Id = Guid.NewGuid(),
                    PlanStateId = plan.Id,
                    TargetLockId = runway.TargetLockId,
                    TargetContextVersionSequence = runway.TargetContextVersionSequence,
                    LockedRunwayStartGlobalWeek = runway.LockedRunwayStartGlobalWeek,
                    LockedRunwayEndGlobalWeek = runway.LockedRunwayEndGlobalWeek,
                    CoreWeekOneWeeklyTargetKm = runway.CoreWeekOneWeeklyTargetKm,
                    CoreWeekOneLongRunTargetKm = runway.CoreWeekOneLongRunTargetKm,
                    FullPrescriptionId = runway.FullPrescriptionId,
                    FullPrescriptionVersion = runway.FullPrescriptionVersion,
                    PrescriptionPayloadJson = runway.PrescriptionPayloadJson,
                    CalendarCompositionIdentity = runway.CalendarCompositionIdentity,
                    CalendarProjectionPayloadJson = runway.CalendarProjectionPayloadJson,
                    TargetLockPayloadJson = runway.TargetLockPayloadJson,
                    CreatedDecisionId = request.CheckpointDecisionId,
                    CreatedAtUtc = now,
                });
            }
            // Mid-Runway regeneration prevention: if a RunwayState already exists,
            // the caller-supplied runway input (if any) is silently NOT re-persisted --
            // the original locked row remains sole authority.
        }

        if (request.CoreContext is { } core)
        {
            var contextExists = await _db.LongHorizonCoreContextRecords.AsNoTracking().AnyAsync(
                c => c.PlanStateId == plan.Id && c.ContextVersionSequence == core.ContextVersionSequence, cancellationToken);
            if (!contextExists)
            {
                var newContextId = StableGuid($"{plan.Id}|{core.CoreContextId}");
                await _db.LongHorizonCoreContextRecords
                    .Where(c => c.PlanStateId == plan.Id && c.Status == LongHorizonPersistedCoreContextStatus.Active)
                    .ForEachAsync(c =>
                    {
                        c.Status = LongHorizonPersistedCoreContextStatus.Superseded;
                        c.SupersededByContextId = newContextId;
                    }, cancellationToken);

                _db.LongHorizonCoreContextRecords.Add(new LongHorizonCoreContextRecord
                {
                    // Phase 4L.2A fix: core.CoreContextId is deterministic from Phase 4K.8C's
                    // own request content, which can coincide across DIFFERENT plans with
                    // similar inputs (proven by a real cross-plan PK collision in this
                    // phase's own Postgres testing). The persisted row identity is scoped
                    // per-plan via StableGuid(plan.Id + core.CoreContextId) instead --
                    // still fully deterministic (idempotent replay for the SAME plan
                    // produces the SAME row id), just no longer globally cross-plan shared.
                    Id = newContextId,
                    PlanStateId = plan.Id,
                    ContextVersionSequence = core.ContextVersionSequence,
                    EffectiveFromGlobalWeek = core.EffectiveFromGlobalWeek,
                    EffectiveToGlobalWeek = core.EffectiveToGlobalWeek,
                    AsOfDate = core.AsOfDate,
                    ConditionResultSummaryJson = core.ConditionResultSummaryJson,
                    ValidatedLoadAuthoritySummary = core.ValidatedLoadAuthoritySummary,
                    GeneratedCoreResultIdentity = core.GeneratedCoreResultIdentity,
                    SelectedCoreWeeksPayloadJson = core.SelectedCoreWeeksPayloadJson,
                    EvidenceFingerprint = core.EvidenceFingerprint,
                    CreatedDecisionId = request.CheckpointDecisionId,
                    Status = LongHorizonPersistedCoreContextStatus.Active,
                });
            }
        }
        _failureInjector.MaybeThrow(activationOp, LongHorizonPersistenceFailpoint.AfterContextInsert, plan.Id);

        _db.LongHorizonActivationWindowRecords.Add(new LongHorizonActivationWindowRecord
        {
            Id = Guid.NewGuid(),
            PlanStateId = plan.Id,
            StartGlobalWeek = window.StartGlobalWeek,
            EndGlobalWeek = window.EndGlobalWeek,
            Outcome = LongHorizonPersistedActivationOutcome.Activated,
            ContextVersionSequence = request.ContextVersion.Sequence,
            ContextVersionId = request.ContextVersion.VersionId,
            CheckpointDecisionId = request.CheckpointDecisionId,
            CoreContextId = request.CoreContext?.CoreContextId,
            RunwayPrescriptionId = request.RunwayState?.FullPrescriptionId,
            TargetLockId = request.RunwayState?.TargetLockId,
            ActivatedAtUtc = now,
            IdempotencyKey = request.IdempotencyKey,
            ContractVersion = 1,
        });
        _failureInjector.MaybeThrow(activationOp, LongHorizonPersistenceFailpoint.AfterActivationWindowInsert, plan.Id);
        _failureInjector.MaybeThrow(activationOp, LongHorizonPersistenceFailpoint.AfterWeekUpdates, plan.Id);
        _failureInjector.MaybeThrow(activationOp, LongHorizonPersistenceFailpoint.AfterSessionInserts, plan.Id);
        _failureInjector.MaybeThrow(activationOp, LongHorizonPersistenceFailpoint.BeforeCommit, plan.Id);
        _constraintMutation.Stage(_db, activationOp, plan.Id);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.ConcurrencyConflict };
        }

        _failureInjector.MaybeThrow(activationOp, LongHorizonPersistenceFailpoint.AfterCommitBeforeAcknowledgement, plan.Id);

        var reloaded = await ReloadCandidateAndReconstructAsync(plan.Id, cancellationToken);
        return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.Success, Snapshot = reloaded };
    }

    public async Task<LongHorizonRollingPersistenceResult> SaveBlockAsync(
        LongHorizonRollingBlockPersistenceRequest request, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.LongHorizonBlockRetryRecords.AsNoTracking().AnyAsync(
            b => b.PlanStateId == request.PlanStateId && b.EventType == LongHorizonPersistedBlockRetryEventType.Block
                 && b.BlockedGlobalWeekStart == request.BlockedGlobalWeekStart && b.CheckpointDate == request.CheckpointDate,
            cancellationToken);
        if (duplicate)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.IdempotentReplay };
        }

        var plan = await _db.LongHorizonRollingPlanStates
            .Include(p => p.Weeks)
            .FirstOrDefaultAsync(p => p.Id == request.PlanStateId, cancellationToken);
        if (plan is null)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.IntegrityViolation, FailureReason = "Plan state not found." };
        }

        SetOriginalConcurrencyToken(plan, request.ExpectedConcurrencyVersion);

        var blockRecordId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        for (var week = request.BlockedGlobalWeekStart; week <= request.BlockedGlobalWeekEnd; week++)
        {
            var weekRow = plan.Weeks.Single(w => w.GlobalWeek == week);
            weekRow.LifecycleState = LongHorizonPersistedLifecycleState.NumericActivationBlocked;
            weekRow.BlockedReasonCode = request.InternalReasonCode;
            weekRow.BlockedDecisionId = blockRecordId;
        }

        plan.CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivationBlocked;
        plan.CurrentBlockedPublicReasonCategory = request.PublicReasonCategory;
        plan.CurrentBlockedInternalReasonCode = request.InternalReasonCode;
        plan.BlockedAt = request.CheckpointDate;
        plan.RetryEligible = request.RetryEligible;
        plan.LatestCheckpointDate = request.CheckpointDate;
        plan.UpdatedAtUtc = now;

        _db.LongHorizonBlockRetryRecords.Add(new LongHorizonBlockRetryRecord
        {
            Id = blockRecordId,
            PlanStateId = plan.Id,
            EventType = LongHorizonPersistedBlockRetryEventType.Block,
            BlockedGlobalWeekStart = request.BlockedGlobalWeekStart,
            BlockedGlobalWeekEnd = request.BlockedGlobalWeekEnd,
            PublicReasonCategory = request.PublicReasonCategory,
            InternalReasonCode = request.InternalReasonCode,
            EvidenceFingerprint = request.EvidenceFingerprint,
            CheckpointDate = request.CheckpointDate,
            RetryEligible = request.RetryEligible,
            RelatedDecisionId = request.RelatedDecisionId,
            CreatedAtUtc = now,
        });
        _failureInjector.MaybeThrow(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.AfterActivationWindowInsert, plan.Id);
        _failureInjector.MaybeThrow(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.BeforeCommit, plan.Id);
        _constraintMutation.Stage(_db, LongHorizonPersistenceOperation.BlockPersistence, plan.Id);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.ConcurrencyConflict };
        }

        _failureInjector.MaybeThrow(LongHorizonPersistenceOperation.BlockPersistence, LongHorizonPersistenceFailpoint.AfterCommitBeforeAcknowledgement, plan.Id);

        var reloaded = await ReloadCandidateAndReconstructAsync(plan.Id, cancellationToken);
        return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.Success, Snapshot = reloaded };
    }

    public async Task<LongHorizonRollingPersistenceResult> SaveRetryRestorationAsync(
        LongHorizonRollingRetryPersistenceRequest request, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.LongHorizonBlockRetryRecords.AsNoTracking().AnyAsync(
            r => r.PlanStateId == request.PlanStateId && r.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored
                 && r.BlockedGlobalWeekStart == request.RestoredGlobalWeekStart && r.CheckpointDate == request.RetryCheckpointDate,
            cancellationToken);
        if (duplicate)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.IdempotentReplay };
        }

        var plan = await _db.LongHorizonRollingPlanStates
            .Include(p => p.Weeks)
            .FirstOrDefaultAsync(p => p.Id == request.PlanStateId, cancellationToken);
        if (plan is null)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.IntegrityViolation, FailureReason = "Plan state not found." };
        }

        if (plan.CurrentLifecycleStatus != LongHorizonPersistedLifecycleState.NumericActivationBlocked)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.IntegrityViolation, FailureReason = "Plan is not currently Blocked -- direct Blocked->Activated or a retry with no prior block is forbidden." };
        }

        if (plan.LatestCheckpointDate is { } lastDate && request.RetryCheckpointDate <= lastDate)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.IntegrityViolation, FailureReason = "RetryCheckpointDate must be strictly later than the prior checkpoint (same-date retry rejected)." };
        }

        var lastBlock = await _db.LongHorizonBlockRetryRecords.AsNoTracking()
            .Where(b => b.PlanStateId == plan.Id && b.EventType == LongHorizonPersistedBlockRetryEventType.Block)
            .OrderByDescending(b => b.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (lastBlock is not null && lastBlock.EvidenceFingerprint == request.ChangedEvidenceFingerprint)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.IntegrityViolation, FailureReason = "Evidence fingerprint unchanged since the block (unchanged-evidence retry rejected)." };
        }

        SetOriginalConcurrencyToken(plan, request.ExpectedConcurrencyVersion);

        var now = DateTime.UtcNow;
        for (var week = request.RestoredGlobalWeekStart; week <= request.RestoredGlobalWeekEnd; week++)
        {
            var weekRow = plan.Weeks.Single(w => w.GlobalWeek == week);
            weekRow.LifecycleState = LongHorizonPersistedLifecycleState.NumericPending;
            weekRow.BlockedReasonCode = null;
            weekRow.BlockedDecisionId = null;
        }

        plan.CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericPending;
        plan.CurrentBlockedPublicReasonCategory = null;
        plan.CurrentBlockedInternalReasonCode = null;
        plan.BlockedAt = null;
        plan.RetryEligible = false;
        plan.LatestCheckpointDate = request.RetryCheckpointDate;
        plan.UpdatedAtUtc = now;

        _db.LongHorizonBlockRetryRecords.Add(new LongHorizonBlockRetryRecord
        {
            Id = Guid.NewGuid(),
            PlanStateId = plan.Id,
            EventType = LongHorizonPersistedBlockRetryEventType.RetryRestored,
            BlockedGlobalWeekStart = request.RestoredGlobalWeekStart,
            BlockedGlobalWeekEnd = request.RestoredGlobalWeekEnd,
            PublicReasonCategory = "RestoredToPending",
            InternalReasonCode = "RETRY_RESTORED",
            EvidenceFingerprint = request.ChangedEvidenceFingerprint,
            CheckpointDate = request.RetryCheckpointDate,
            RetryEligible = false,
            RelatedDecisionId = request.RelatedDecisionId,
            CreatedAtUtc = now,
        });
        _failureInjector.MaybeThrow(LongHorizonPersistenceOperation.RetryPersistence, LongHorizonPersistenceFailpoint.AfterActivationWindowInsert, plan.Id);
        _failureInjector.MaybeThrow(LongHorizonPersistenceOperation.RetryPersistence, LongHorizonPersistenceFailpoint.BeforeCommit, plan.Id);
        _constraintMutation.Stage(_db, LongHorizonPersistenceOperation.RetryPersistence, plan.Id);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.ConcurrencyConflict };
        }

        _failureInjector.MaybeThrow(LongHorizonPersistenceOperation.RetryPersistence, LongHorizonPersistenceFailpoint.AfterCommitBeforeAcknowledgement, plan.Id);

        var reloaded = await ReloadCandidateAndReconstructAsync(plan.Id, cancellationToken);
        return new LongHorizonRollingPersistenceResult { Outcome = LongHorizonRollingPersistenceOutcome.Success, Snapshot = reloaded };
    }

    public async Task<LongHorizonActiveCoreContextSnapshot?> GetActiveCoreContextAsync(
        Guid planStateId, CancellationToken cancellationToken = default)
    {
        var row = await _db.LongHorizonCoreContextRecords.AsNoTracking()
            .Where(c => c.PlanStateId == planStateId && c.Status == LongHorizonPersistedCoreContextStatus.Active)
            .OrderByDescending(c => c.ContextVersionSequence)
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        return new LongHorizonActiveCoreContextSnapshot
        {
            CoreContextId = row.Id,
            ContextVersionSequence = row.ContextVersionSequence,
            EffectiveFromGlobalWeek = row.EffectiveFromGlobalWeek,
            EffectiveToGlobalWeek = row.EffectiveToGlobalWeek,
            AsOfDate = row.AsOfDate,
            ConditionResultSummaryJson = row.ConditionResultSummaryJson,
            EvidenceFingerprint = row.EvidenceFingerprint,
        };
    }

    public async Task SetCoreContextEvidenceFingerprintAsync(
        Guid coreContextId, string evidenceFingerprint, CancellationToken cancellationToken = default)
    {
        var row = await _db.LongHorizonCoreContextRecords.SingleAsync(c => c.Id == coreContextId, cancellationToken);
        row.EvidenceFingerprint = evidenceFingerprint;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Guid StableGuid(string seed) => new(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed)).AsSpan(0, 16));

    private void SetOriginalConcurrencyToken(LongHorizonRollingPlanState plan, uint expected)
    {
        _db.Entry(plan).Property<uint>("xmin").OriginalValue = expected;
    }

    private async Task<LongHorizonRollingRestartSnapshot?> ReloadCandidateAndReconstructAsync(Guid planStateId, CancellationToken cancellationToken)
    {
        var plan = await _db.LongHorizonRollingPlanStates.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planStateId, cancellationToken);
        if (plan is null) return null;
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = plan.CatalogRootPath });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        var candidate = await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(plan.CandidateKey, plan.CandidateVersion);
        return await ReconstructAsync(planStateId, plan.CatalogRootPath, candidate, cancellationToken);
    }

    private async Task<LongHorizonRollingRestartSnapshot?> ReconstructAsync(
        Guid planStateId, string catalogRootPath, PlanCatalogCandidateSummary candidate, CancellationToken cancellationToken)
        => await LongHorizonRollingStateReconstructionService.ReconstructAsync(_db, planStateId, catalogRootPath, candidate, cancellationToken);

    internal static LongHorizonPersistedSegmentType MapSegment(LongHorizonStructuralSegmentType segment) => segment switch
    {
        LongHorizonStructuralSegmentType.GeneralEndurance => LongHorizonPersistedSegmentType.GeneralEndurance,
        LongHorizonStructuralSegmentType.PreparationRunway => LongHorizonPersistedSegmentType.PreparationRunway,
        LongHorizonStructuralSegmentType.Core => LongHorizonPersistedSegmentType.Core,
        _ => throw new ArgumentOutOfRangeException(nameof(segment)),
    };

    internal static LongHorizonPersistedLifecycleState MapLifecycle(LongHorizonNumericLifecycleState state) => state switch
    {
        LongHorizonNumericLifecycleState.StructurallyPlanned => LongHorizonPersistedLifecycleState.StructurallyPlanned,
        LongHorizonNumericLifecycleState.NumericPending => LongHorizonPersistedLifecycleState.NumericPending,
        LongHorizonNumericLifecycleState.NumericActivated => LongHorizonPersistedLifecycleState.NumericActivated,
        LongHorizonNumericLifecycleState.NumericActivationBlocked => LongHorizonPersistedLifecycleState.NumericActivationBlocked,
        LongHorizonNumericLifecycleState.Completed => LongHorizonPersistedLifecycleState.Completed,
        LongHorizonNumericLifecycleState.Missed => LongHorizonPersistedLifecycleState.Missed,
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };
}
