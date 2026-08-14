using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 4L.2C Parts 15 &amp; 17 -- replay/idempotency and corruption/fail-closed
/// proofs, narrowly scoped to the exact continuation-advancement invariant
/// this phase resolves (not the full Phase 4L.2B failure-injection matrix).
/// </summary>
public sealed class LongHorizonRunwayContinuationReplayAndCorruptionTests
{
    /// <summary>
    /// Replaying the exact same already-persisted activation request (same
    /// IdempotencyKey) must return IdempotentReplay and must not create a
    /// second activation row or double-activate the same weeks.
    /// </summary>
    [Fact]
    public async Task ReplayingIdenticalActivationRequest_ReturnsIdempotentReplayWithoutDuplication()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var firstEntry = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, firstEntry.Outcome);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var priorRow = await db.LongHorizonActivationWindowRecords
            .Where(a => a.PlanStateId == planStateId).OrderByDescending(a => a.ActivatedAtUtc).FirstAsync();

        var window = firstEntry.Snapshot!.DarkState.CurrentWindow;
        var replayRequest = new LongHorizonRollingActivationPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = firstEntry.Snapshot.ConcurrencyVersion,
            ActivatedWindow = window,
            LifecycleStates = firstEntry.Snapshot.DarkState.LifecycleStates,
            ContextVersion = firstEntry.Snapshot.DarkState.ContextVersion,
            CheckpointDecisionId = null,
            Checkpoint = null,
            IdempotencyKey = priorRow.IdempotencyKey,
        };

        var replay = await repo.SaveActivationSuccessAsync(replayRequest);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.IdempotentReplay, replay.Outcome);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var rowCount = await verify.LongHorizonActivationWindowRecords.CountAsync(a => a.IdempotencyKey == priorRow.IdempotencyKey);
        Assert.Equal(1, rowCount);
    }

    /// <summary>
    /// Corruption/fail-closed: attempting to activate a week that is not
    /// NumericPending/StructurallyPlanned (e.g. already NumericActivated) must
    /// fail with IntegrityViolation, never silently overwrite the week.
    /// </summary>
    [Fact]
    public async Task ReactivatingAlreadyActivatedWeek_FailsClosedWithIntegrityViolation()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await LongHorizonRunwayCoreRestartFixture.InitializePlanAsync(21);
        var date1 = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(29);
        var firstEntry = await LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync(planStateId, initialWindow, date1, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, firstEntry.Outcome);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
        var window = firstEntry.Snapshot!.DarkState.CurrentWindow; // already-activated weeks 2-5

        var corruptRequest = new LongHorizonRollingActivationPersistenceRequest
        {
            PlanStateId = planStateId,
            ExpectedConcurrencyVersion = snapshot!.ConcurrencyVersion,
            ActivatedWindow = window,
            LifecycleStates = snapshot.DarkState.LifecycleStates,
            ContextVersion = snapshot.DarkState.ContextVersion,
            CheckpointDecisionId = null,
            Checkpoint = null,
            IdempotencyKey = $"corruption-probe:{Guid.NewGuid()}",
        };

        var result = await repo.SaveActivationSuccessAsync(corruptRequest);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.IntegrityViolation, result.Outcome);
        Assert.Contains("not Pending", result.FailureReason);
    }
}
