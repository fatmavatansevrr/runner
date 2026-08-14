using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

public sealed class LongHorizonPostgresConstraintRollbackTests
{
    private const string ActivationUnique = "IX_LongHorizonActivationWindowRecords_IdempotencyKey";
    private const string SessionUnique = "IX_LongHorizonRollingSessionStates_WeekStateId_SessionOrdinal";
    // Phase 4M.2: a second, independent unique index now exists on this
    // entity -- the partial unique index on AdaptedFromSessionId that
    // enforces the Rev3.1 "at most one direct replacement child per source
    // session" invariant at the database level (see
    // ScheduleRepairPersistenceService's own doc comment and
    // PHASE4M_2_...md Section 10). This is a real, required schema change,
    // not a regression -- the assertion below was updated to match by
    // selecting on the indexed property, not by assuming singularity.
    private const string SessionAdaptedFromUnique = "IX_LongHorizonRollingSessionStates_AdaptedFromSessionId";
    private const string CoreContextUnique = "IX_LongHorizonCoreContextRecords_PlanStateId_ContextVersionSequence";
    private const string CoreContextUniquePostgres = "IX_LongHorizonCoreContextRecords_PlanStateId_ContextVersionSequ";
    private const string BlockPlanForeignKey = "FK_LongHorizonBlockRetryRecords_LongHorizonRollingPlanStates_P~";

    [Fact]
    public void ConstraintInventory_UsesConfiguredPostgresAndContainsNoLongHorizonCheckConstraint()
    {
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Contains("Npgsql", db.Database.ProviderName);

        var model = db.GetService<IDesignTimeModel>().Model;
        Assert.Equal(ActivationUnique, model.FindEntityType(typeof(LongHorizonActivationWindowRecord))!
            .GetIndexes().Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(LongHorizonActivationWindowRecord.IdempotencyKey)])).GetDatabaseName());
        var sessionUniqueIndexes = model.FindEntityType(typeof(LongHorizonRollingSessionState))!
            .GetIndexes().Where(i => i.IsUnique).ToList();
        Assert.Equal(2, sessionUniqueIndexes.Count);
        Assert.Equal(SessionUnique, sessionUniqueIndexes
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(LongHorizonRollingSessionState.WeekStateId), nameof(LongHorizonRollingSessionState.SessionOrdinal)]))
            .GetDatabaseName());
        Assert.Equal(SessionAdaptedFromUnique, sessionUniqueIndexes
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(LongHorizonRollingSessionState.AdaptedFromSessionId)]))
            .GetDatabaseName());
        Assert.Equal(CoreContextUnique, model.FindEntityType(typeof(LongHorizonCoreContextRecord))!
            .GetIndexes().Single(i => i.IsUnique).GetDatabaseName());
        Assert.Empty(model.GetEntityTypes().Where(e => e.ClrType.Name.StartsWith("LongHorizon", StringComparison.Ordinal))
            .SelectMany(e => e.GetCheckConstraints()));
    }

    [Fact]
    public async Task MixedActivation_DuplicateOwnership_23505_RollsBackAndCorrectedReplayCommitsOnce()
    {
        var state = await DriveToBoundaryAsync(26, 2);
        var before = await CaptureAsync(state.PlanStateId);

        await using (var failed = NewMutatingContext())
        {
            var seam = ConstraintMutation.DuplicateActivation();
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(
                    state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate,
                    new LongHorizonRollingStateRepository(failed, constraintMutation: seam)));
            AssertPostgres(exception, PostgresErrorCodes.UniqueViolation, ActivationUnique);
            Assert.True(seam.Fired);
        }

        await AssertUnchangedFromFreshContextAsync(state.PlanStateId, before);
        var replay = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
            state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);
        await using var verify = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(1, await verify.LongHorizonActivationWindowRecords.CountAsync(a =>
            a.PlanStateId == state.PlanStateId && a.StartGlobalWeek == replay.Snapshot!.DarkState.CurrentWindow.StartGlobalWeek));
    }

    [Fact]
    public async Task CoreOnlyActivation_DuplicateSession_23505_RollsBackAndCorrectedReplayCommitsOnce()
    {
        var state = await DriveToBoundaryAsync(21, 2);
        var before = await CaptureAsync(state.PlanStateId);

        await using (var failed = NewMutatingContext())
        {
            var seam = ConstraintMutation.DuplicateSession();
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(
                    state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate,
                    new LongHorizonRollingStateRepository(failed, constraintMutation: seam)));
            AssertPostgres(exception, PostgresErrorCodes.UniqueViolation, SessionUnique);
        }

        await AssertUnchangedFromFreshContextAsync(state.PlanStateId, before);
        var replay = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
            state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);
        await using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var duplicateGroups = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == state.PlanStateId)
            .GroupBy(s => new { s.WeekStateId, s.SessionOrdinal }).Where(g => g.Count() > 1).CountAsync();
        Assert.Equal(0, duplicateGroups);
    }

    [Fact]
    public async Task FutureCoreRefresh_DuplicatePlanScopedContext_23505_PreservesV1AndReplayCreatesOneV2()
    {
        var state = await DriveToBoundaryAsync(25, 4);
        await using var beforeDb = LongHorizonPersistenceTestFixture.NewContext();
        var beforeRepo = new LongHorizonRollingStateRepository(beforeDb);
        var beforeRestart = await beforeRepo.LoadRestartSnapshotAsync(state.PlanStateId);
        var v1 = await beforeRepo.GetActiveCoreContextAsync(state.PlanStateId);
        var before = await CaptureAsync(state.PlanStateId);
        var request = BuildRefreshRequest(state, beforeRestart!);

        await using (var failed = NewMutatingContext())
        {
            var seam = ConstraintMutation.DuplicateCoreContext();
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                new LongHorizonFutureCoreRefreshOrchestrator(
                    new LongHorizonRollingStateRepository(failed, constraintMutation: seam)).RefreshAsync(request));
            AssertPostgres(exception, PostgresErrorCodes.UniqueViolation, CoreContextUniquePostgres);
        }

        await AssertUnchangedFromFreshContextAsync(state.PlanStateId, before);
        await using (var postDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var row = await postDb.LongHorizonCoreContextRecords.SingleAsync(c => c.Id == v1!.CoreContextId);
            Assert.Equal(LongHorizonPersistedCoreContextStatus.Active, row.Status);
            Assert.Null(row.SupersededByContextId);
        }

        await using var replayDb = LongHorizonPersistenceTestFixture.NewContext();
        var replay = await new LongHorizonFutureCoreRefreshOrchestrator(new LongHorizonRollingStateRepository(replayDb)).RefreshAsync(request);
        Assert.Equal(LongHorizonFutureCoreRefreshOutcome.Refreshed, replay.Outcome);
        await using var verify = LongHorizonPersistenceTestFixture.NewContext();
        Assert.Equal(before.CoreContexts + 1, await verify.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == state.PlanStateId));
        Assert.Equal(1, await verify.LongHorizonCoreContextRecords.CountAsync(c => c.PlanStateId == state.PlanStateId && c.Status == LongHorizonPersistedCoreContextStatus.Active));
    }

    [Fact]
    public async Task Block_InvalidPlanForeignKey_23503_RollsBackAndCorrectedReplayCommitsOnce()
    {
        var (planStateId, _, _, _) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var before = await CaptureAsync(planStateId);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);

        await using (var failed = NewMutatingContext())
        {
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(
                    failed, constraintMutation: ConstraintMutation.InvalidBlockRetryForeignKey()))
                .PersistBlockAsync(planStateId, before.Xmin, 5, 8, LongHorizonReasonCode.SafetyReassessmentRequired,
                    "SafetyReviewRequired", "constraint-block", date, true));
            AssertPostgres(exception, PostgresErrorCodes.ForeignKeyViolation, BlockPlanForeignKey);
        }

        await AssertUnchangedFromFreshContextAsync(planStateId, before);
        await using var replayDb = LongHorizonPersistenceTestFixture.NewContext();
        var replay = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(replayDb))
            .PersistBlockAsync(planStateId, before.Xmin, 5, 8, LongHorizonReasonCode.SafetyReassessmentRequired,
                "SafetyReviewRequired", "constraint-block", date, true);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);
    }

    [Fact]
    public async Task Retry_InvalidPlanForeignKey_23503_RollsBackBlockedStateAndCorrectedReplayCommitsOnce()
    {
        var (planStateId, _, _, _) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(25);
        var blockDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        await using (var blockDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            var initial = await new LongHorizonRollingStateRepository(blockDb).LoadRestartSnapshotAsync(planStateId);
            var result = await new LongHorizonRollingBlockPersistenceAdapter(new LongHorizonRollingStateRepository(blockDb))
                .PersistBlockAsync(planStateId, initial!.ConcurrencyVersion, 5, 8, LongHorizonReasonCode.SafetyReassessmentRequired,
                    "SafetyReviewRequired", "constraint-retry-block", blockDate, true);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, result.Outcome);
        }

        var before = await CaptureAsync(planStateId);
        var request = new LongHorizonRollingRetryPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = before.Xmin,
            RestoredGlobalWeekStart = 5,
            RestoredGlobalWeekEnd = 8,
            RetryCheckpointDate = blockDate.AddDays(1),
            ChangedEvidenceFingerprint = "constraint-retry-changed",
            RelatedDecisionId = Guid.NewGuid(),
            IdempotencyKey = $"constraint-retry:{planStateId}",
        };

        await using (var failed = NewMutatingContext())
        {
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                new LongHorizonRollingStateRepository(failed, constraintMutation: ConstraintMutation.InvalidBlockRetryForeignKey())
                    .SaveRetryRestorationAsync(request));
            AssertPostgres(exception, PostgresErrorCodes.ForeignKeyViolation, BlockPlanForeignKey);
        }

        await AssertUnchangedFromFreshContextAsync(planStateId, before);
        await using var replayDb = LongHorizonPersistenceTestFixture.NewContext();
        var replay = await new LongHorizonRollingStateRepository(replayDb).SaveRetryRestorationAsync(request);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);
    }

    [Fact]
    public async Task CoreOnlyActivation_NullIdempotencyKey_23502_RollsBackCompleteUnitOfWork()
    {
        var state = await DriveToBoundaryAsync(21, 2);
        var before = await CaptureAsync(state.PlanStateId);
        await using (var failed = NewMutatingContext())
        {
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsyncWithInjector(
                    state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate,
                    new LongHorizonRollingStateRepository(failed, constraintMutation: ConstraintMutation.NullActivationIdempotencyKey())));
            var postgres = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal(PostgresErrorCodes.NotNullViolation, postgres.SqlState);
            Assert.Equal("IdempotencyKey", postgres.ColumnName);
        }
        await AssertUnchangedFromFreshContextAsync(state.PlanStateId, before);
        var replay = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
            state.PlanStateId, state.Window, state.Date, state.CatalogRoot, state.Candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, replay.Outcome);
    }

    [Fact]
    public void ConstraintMutationSeam_IsInternalNoOpByDefaultAndNotApiRegistered()
    {
        Assert.False(typeof(ILongHorizonPersistenceConstraintMutation).IsPublic);
        Assert.False(typeof(NoOpLongHorizonPersistenceConstraintMutation).IsPublic);
        var root = RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
        Assert.DoesNotContain(nameof(ILongHorizonPersistenceConstraintMutation), File.ReadAllText(Path.Combine(root, "backend", "RunningApp.Api", "Program.cs")));
        Assert.DoesNotContain(nameof(ILongHorizonPersistenceConstraintMutation), File.ReadAllText(Path.Combine(root, "backend", "RunningApp.Api", "Controllers", "PlansController.cs")));
    }

    private static AppDbContext NewMutatingContext() => LongHorizonPersistenceTestFixture.NewContext();

    private static void AssertPostgres(DbUpdateException exception, string sqlState, string constraint)
    {
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(sqlState, postgres.SqlState);
        Assert.Equal(constraint, postgres.ConstraintName);
    }

    private static async Task<BoundaryState> DriveToBoundaryAsync(int totalWeeks, int advances)
    {
        var initialized = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(totalWeeks);
        var date = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var window = initialized.InitialWindow;
        for (var index = 0; index < advances; index++)
        {
            var call = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(
                initialized.PlanStateId, window, date, initialized.CatalogRoot, initialized.Candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        return new BoundaryState(initialized.PlanStateId, initialized.Candidate, initialized.CatalogRoot, date, window);
    }

    private static LongHorizonFutureCoreRefreshRequest BuildRefreshRequest(BoundaryState state, LongHorizonRollingRestartSnapshot snapshot) => new()
    {
        PlanStateId = state.PlanStateId,
        ExpectedAggregateVersion = snapshot.ConcurrencyVersion,
        RequestedAsOfDate = state.Date,
        TrainingDayEvidence = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(state.Window, state.PlanStateId),
        CurrentAvailability = LongHorizonFullLifecycleTestFixture.PreferredDays,
        LongRunDay = DayOfWeek.Sunday,
        SafetyState = LongHorizonSafetyState.Clear,
        PlanStartDate = LongHorizonFullLifecycleTestFixture.StartDate,
        RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(25 * 7),
        CatalogRootPath = state.CatalogRoot,
    };

    private static async Task<RawSnapshot> CaptureAsync(Guid planStateId)
    {
        await using var db = LongHorizonPersistenceTestFixture.NewContext();
        var plan = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        var xmin = await db.LongHorizonRollingPlanStates.AsNoTracking().Where(p => p.Id == planStateId)
            .Select(p => EF.Property<uint>(p, "xmin")).SingleAsync();
        var rows = new
        {
            Plan = new { plan.CurrentLifecycleStatus, plan.CurrentWindowStartWeek, plan.CurrentWindowEndWeek, plan.LastActivatedGlobalWeek, plan.ActiveContextVersionSequence, plan.ActiveContextVersionId, plan.CurrentBlockedInternalReasonCode, plan.BlockedAt, plan.RetryEligible },
            Weeks = await db.LongHorizonRollingWeekStates.AsNoTracking().Where(w => w.PlanStateId == planStateId).OrderBy(w => w.GlobalWeek)
                .Select(w => new { w.Id, w.GlobalWeek, w.LifecycleState, w.WeeklyVolumeKm, w.LongRunKm, w.ActivationContextVersionSequence, w.BlockedDecisionId }).ToListAsync(),
            Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Where(s => s.Week.PlanStateId == planStateId).OrderBy(s => s.WeekStateId).ThenBy(s => s.SessionOrdinal)
                .Select(s => new { s.Id, s.WeekStateId, s.SessionOrdinal, s.DistanceKm, s.ActivationContextVersionSequence }).ToListAsync(),
            Activations = await db.LongHorizonActivationWindowRecords.AsNoTracking().Where(a => a.PlanStateId == planStateId).OrderBy(a => a.ActivatedAtUtc)
                .Select(a => new { a.Id, a.StartGlobalWeek, a.EndGlobalWeek, a.IdempotencyKey, a.CoreContextId, a.RunwayPrescriptionId, a.TargetLockId, a.CheckpointDecisionId }).ToListAsync(),
            Checkpoints = await db.LongHorizonCheckpointRecords.AsNoTracking().Where(c => c.PlanStateId == planStateId).OrderBy(c => c.AsOfDate).ThenBy(c => c.SourceWindowStartWeek)
                .Select(c => new { c.Id, c.AsOfDate, c.SourceWindowStartWeek, c.SourceWindowEndWeek, c.EvidenceFingerprint, c.ContextVersionSequence, c.Decision }).ToListAsync(),
            Runway = await db.LongHorizonRunwayStates.AsNoTracking().Where(r => r.PlanStateId == planStateId).OrderBy(r => r.Id)
                .Select(r => new { r.Id, r.TargetLockId, r.TargetContextVersionSequence, r.LockedRunwayStartGlobalWeek, r.LockedRunwayEndGlobalWeek, r.FullPrescriptionId, r.FullPrescriptionVersion, r.PrescriptionPayloadJson, r.CalendarCompositionIdentity, r.CalendarProjectionPayloadJson, r.TargetLockPayloadJson }).ToListAsync(),
            Contexts = await db.LongHorizonCoreContextRecords.AsNoTracking().Where(c => c.PlanStateId == planStateId).OrderBy(c => c.ContextVersionSequence)
                .Select(c => new { c.Id, c.ContextVersionSequence, c.Status, c.SupersededByContextId, c.EvidenceFingerprint, c.ConditionResultSummaryJson, c.ValidatedLoadAuthoritySummary, c.GeneratedCoreResultIdentity, c.SelectedCoreWeeksPayloadJson }).ToListAsync(),
            Blocks = await db.LongHorizonBlockRetryRecords.AsNoTracking().Where(b => b.PlanStateId == planStateId).OrderBy(b => b.CreatedAtUtc)
                .Select(b => new { b.Id, b.EventType, b.BlockedGlobalWeekStart, b.BlockedGlobalWeekEnd, b.EvidenceFingerprint }).ToListAsync(),
        };
        return new RawSnapshot(xmin, JsonSerializer.Serialize(rows), rows.Sessions.Count, rows.Activations.Count, rows.Contexts.Count, rows.Blocks.Count);
    }

    private static async Task AssertUnchangedFromFreshContextAsync(Guid planStateId, RawSnapshot before)
    {
        var after = await CaptureAsync(planStateId);
        Assert.Equal(before, after);
        await using var fresh = LongHorizonPersistenceTestFixture.NewContext();
        var reconstructed = await new LongHorizonRollingStateRepository(fresh).LoadRestartSnapshotAsync(planStateId);
        Assert.NotNull(reconstructed);
        Assert.Equal(before.Xmin, reconstructed!.ConcurrencyVersion);
        var plan = await fresh.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        LongHorizonRollingPersistenceIntegrityValidator.ValidateReconstructedState(plan, reconstructed.DarkState);
    }

    private sealed record BoundaryState(Guid PlanStateId, PlanCatalogCandidateSummary Candidate, string CatalogRoot, DateOnly Date, RollingNumericActivationWindow Window);
    private sealed record RawSnapshot(uint Xmin, string CanonicalJson, int Sessions, int Activations, int CoreContexts, int BlockRetries);

    private enum MutationKind { DuplicateActivation, DuplicateSession, DuplicateCoreContext, InvalidBlockRetryForeignKey, NullActivationIdempotencyKey }

    private sealed class ConstraintMutation(MutationKind kind) : ILongHorizonPersistenceConstraintMutation
    {
        public bool Fired { get; private set; }
        public static ConstraintMutation DuplicateActivation() => new(MutationKind.DuplicateActivation);
        public static ConstraintMutation DuplicateSession() => new(MutationKind.DuplicateSession);
        public static ConstraintMutation DuplicateCoreContext() => new(MutationKind.DuplicateCoreContext);
        public static ConstraintMutation InvalidBlockRetryForeignKey() => new(MutationKind.InvalidBlockRetryForeignKey);
        public static ConstraintMutation NullActivationIdempotencyKey() => new(MutationKind.NullActivationIdempotencyKey);

        public void Stage(AppDbContext db, LongHorizonPersistenceOperation operation, Guid planStateId)
        {
            if (Fired) return;
            Fired = true;
            switch (kind)
            {
                case MutationKind.DuplicateActivation:
                    var activation = db.ChangeTracker.Entries<LongHorizonActivationWindowRecord>().Single(e => e.State == EntityState.Added).Entity;
                    db.LongHorizonActivationWindowRecords.Add(new LongHorizonActivationWindowRecord
                    {
                        Id = Guid.NewGuid(), PlanStateId = activation.PlanStateId, StartGlobalWeek = activation.StartGlobalWeek,
                        EndGlobalWeek = activation.EndGlobalWeek, Outcome = activation.Outcome, ContextVersionSequence = activation.ContextVersionSequence,
                        ContextVersionId = activation.ContextVersionId, ActivatedAtUtc = activation.ActivatedAtUtc,
                        IdempotencyKey = activation.IdempotencyKey, ContractVersion = activation.ContractVersion,
                    });
                    break;
                case MutationKind.DuplicateSession:
                    var session = db.ChangeTracker.Entries<LongHorizonRollingSessionState>().First(e => e.State == EntityState.Added).Entity;
                    db.LongHorizonRollingSessionStates.Add(new LongHorizonRollingSessionState
                    {
                        Id = Guid.NewGuid(), WeekStateId = session.WeekStateId, SessionOrdinal = session.SessionOrdinal,
                        SessionRole = session.SessionRole, WorkoutKey = session.WorkoutKey, WorkoutVersion = session.WorkoutVersion,
                        DistanceKm = session.DistanceKm, AssignedDate = session.AssignedDate,
                        ActivationContextVersionSequence = session.ActivationContextVersionSequence, Provenance = session.Provenance,
                    });
                    break;
                case MutationKind.DuplicateCoreContext:
                    var context = db.ChangeTracker.Entries<LongHorizonCoreContextRecord>().Single(e => e.State == EntityState.Added).Entity;
                    db.LongHorizonCoreContextRecords.Add(new LongHorizonCoreContextRecord
                    {
                        Id = Guid.NewGuid(), PlanStateId = context.PlanStateId, ContextVersionSequence = context.ContextVersionSequence,
                        EffectiveFromGlobalWeek = context.EffectiveFromGlobalWeek, EffectiveToGlobalWeek = context.EffectiveToGlobalWeek,
                        AsOfDate = context.AsOfDate, ConditionResultSummaryJson = context.ConditionResultSummaryJson,
                        ValidatedLoadAuthoritySummary = context.ValidatedLoadAuthoritySummary,
                        GeneratedCoreResultIdentity = context.GeneratedCoreResultIdentity,
                        SelectedCoreWeeksPayloadJson = context.SelectedCoreWeeksPayloadJson, Status = context.Status,
                    });
                    break;
                case MutationKind.InvalidBlockRetryForeignKey:
                    db.LongHorizonBlockRetryRecords.Add(new LongHorizonBlockRetryRecord
                    {
                        Id = Guid.NewGuid(), PlanStateId = Guid.NewGuid(), EventType = LongHorizonPersistedBlockRetryEventType.Block,
                        BlockedGlobalWeekStart = 1, BlockedGlobalWeekEnd = 1, PublicReasonCategory = "TestOnly",
                        InternalReasonCode = "TEST_ONLY_INVALID_FK", EvidenceFingerprint = "test-only-invalid-fk",
                        CheckpointDate = DateOnly.FromDateTime(DateTime.UtcNow), CreatedAtUtc = DateTime.UtcNow,
                    });
                    break;
                case MutationKind.NullActivationIdempotencyKey:
                    db.ChangeTracker.Entries<LongHorizonActivationWindowRecord>().Single(e => e.State == EntityState.Added).Entity.IdempotencyKey = null!;
                    break;
            }
        }
    }
}
