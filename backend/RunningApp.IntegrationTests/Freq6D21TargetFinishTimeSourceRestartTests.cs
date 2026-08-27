using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Application.Services;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 10K-FREQ.6D.21 -- real PostgreSQL proof that
/// <see cref="RunningApp.Domain.Entities.TrainingPlan.TargetFinishTimeSource"/>
/// (FREQ.6D.20's approved plan-level persistence authority) survives a
/// genuine process-style restart and reaches real Core generation. Drives
/// the same real production service (<see cref="LongHorizonRollingWindowActivationService"/>)
/// FREQ.6D.19's own organic GE->Runway->Core fixture reaches internally --
/// never the public HTTP preview/confirm surface, which correctly remains
/// gated to 4D-only (`LONG_HORIZON_PILOT_UNSUPPORTED`) and must not be
/// touched by this phase (§43). Every "restart" opens a fresh
/// <see cref="AppDbContext"/> and a fresh service instance -- DbContext A is
/// created/mutated/saved and disposed before DbContext B reloads and
/// continues.
/// </summary>
public sealed class Freq6D21TargetFinishTimeSourceRestartTests
{
    private static async Task<(Guid PlanId, Guid RollingId, Guid UserId)> CreateConfirmedFiveDayPlanAsync(
        int? targetFinishTimeSeconds, TargetFinishTimeSource? targetFinishTimeSource)
    {
        var (rollingId, initialWindow, candidate, catalogRoot) = await Freq6D19FiveDayGeRunwayCoreBoundaryFixture.InitializePlanAsync(21);
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        db.Users.Add(new User
        {
            Id = userId,
            ExternalUserId = $"freq6d21-{userId}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        // Mirrors exactly what CatalogPlanConfirmationService/LongHorizonPublicPlanService
        // already persist at real confirmation -- this phase's own fixed write boundary,
        // reused directly rather than re-implemented, since InitializePlanAsync already
        // performs the structural-state half of what confirm does.
        db.TrainingPlans.Add(new TrainingPlan
        {
            Id = planId,
            InternalUserId = userId,
            Status = TrainingPlanStatus.Active,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 5,
            Unit = DistanceUnit.Km,
            RaceDate = Freq6D19FiveDayGeRunwayCoreBoundaryFixture.StartDate.AddDays(21 * 7),
            TargetFinishTimeSeconds = targetFinishTimeSeconds,
            TargetFinishTimeSource = targetFinishTimeSource,
            StartedAt = DateTime.SpecifyKind(Freq6D19FiveDayGeRunwayCoreBoundaryFixture.StartDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            EstimatedEndDate = DateTime.SpecifyKind(Freq6D19FiveDayGeRunwayCoreBoundaryFixture.StartDate.AddDays(21 * 7).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            ScheduleStrategy = PlanScheduleStrategy.RollingLongHorizon,
            LongHorizonRollingPlanStateId = rollingId,
            PreferredDays = "[1,2,4,5,0]",
            LongRunDay = "Sun",
        });
        await db.SaveChangesAsync();

        return (planId, rollingId, userId);
    }

    private static async Task CompleteCurrentWindowAsync(Guid rollingId)
    {
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == rollingId);
        var sessions = await db.LongHorizonRollingSessionStates.Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek
                && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek
                && s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Planned)
            .ToListAsync();
        foreach (var session in sessions)
        {
            session.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.Completed;
            session.ActualDistanceKm = session.DistanceKm;
            session.ActualDurationMinutes = 30;
            session.CompletedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private static async Task<LongHorizonActivateNextWindowResponse> CompleteAndActivateAsync(Guid userId, Guid rollingId)
    {
        await CompleteCurrentWindowAsync(rollingId);
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var service = new LongHorizonRollingWindowActivationService(db, NullLogger<LongHorizonRollingWindowActivationService>.Instance, NoOpLongHorizonPersistenceFailureInjector.Instance, "1.1.0");
        return await service.ActivateNextWindowAsync(userId, new LongHorizonActivateNextWindowRequest());
    }

    /// <summary>
    /// The principal closure proof (phase §30): a real Intermediate x5D plan
    /// carrying a real, plan-level-persisted ProductAverage
    /// TargetFinishTimeSource is driven through the real production
    /// continuation service three times, each against fresh Postgres state,
    /// reaching the first real, organically-materialized Core window (weeks
    /// 10-13) whose full 12-week Core plan includes a GOAL_PACE_TEN_K
    /// workout -- proving the source read back from the persisted TrainingPlan
    /// row (never from any in-memory request) resolves goal-feasibility
    /// evidence end to end.
    /// </summary>
    [Fact]
    public async Task ProductAverage_OrganicRestartThroughCore_SucceedsAndPreservesSource()
    {
        var (planId, rollingId, userId) = await CreateConfirmedFiveDayPlanAsync(3480, TargetFinishTimeSource.ProductAverage);

        var w1 = await CompleteAndActivateAsync(userId, rollingId);
        Assert.Equal(LongHorizonContinuationOutcome.Activated, w1.Outcome);
        var w2 = await CompleteAndActivateAsync(userId, rollingId);
        Assert.Equal(LongHorizonContinuationOutcome.Activated, w2.Outcome);
        var w3 = await CompleteAndActivateAsync(userId, rollingId);
        Assert.Equal(LongHorizonContinuationOutcome.Activated, w3.Outcome);
        Assert.Equal(10, w3.ActivatedWindowRange!.StartGlobalWeek);
        Assert.Equal(13, w3.ActivatedWindowRange!.EndGlobalWeek);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var firstCoreWeek = await verify.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek == 10).ToListAsync();
        Assert.Equal(5, firstCoreWeek.Count);
        Assert.Equal(2, firstCoreWeek.Count(s => s.SessionRole == "KEY_SESSION"));
        Assert.Contains(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
        Assert.Contains(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);

        var planAfter = await verify.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(TargetFinishTimeSource.ProductAverage, planAfter.TargetFinishTimeSource);
    }

    /// <summary>
    /// Control (phase §13/§32): a genuine UserDefined target with no
    /// independent recent-race evidence is a real, unrelated, pre-existing
    /// product rule that leaves GoalFeasibilityResolver NotEvaluated --
    /// GOAL_PACE_TEN_K correctly still fails closed for this case, exactly as
    /// it did before this phase. What must NOT happen: an unrelated crash, or
    /// UserDefined being silently reclassified as ProductAverage to make the
    /// failure go away.
    /// </summary>
    [Fact]
    public async Task UserDefined_WithoutIndependentEvidence_FailsClosedTyped_NeverReclassified()
    {
        var (planId, rollingId, userId) = await CreateConfirmedFiveDayPlanAsync(3480, TargetFinishTimeSource.UserDefined);

        // Core generation (and therefore GOAL_PACE_TEN_K validation) is attempted
        // eagerly on the very first Runway-entry continuation call, not deferred
        // until real Core weeks are reached (established by FREQ.6D.19's own
        // reconnaissance) -- so the typed block surfaces on this first call.
        await CompleteCurrentWindowAsync(rollingId);
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var service = new LongHorizonRollingWindowActivationService(db, NullLogger<LongHorizonRollingWindowActivationService>.Instance, NoOpLongHorizonPersistenceFailureInjector.Instance, "1.1.0");
        await Assert.ThrowsAsync<LongHorizonContinuationBlockedException>(
            () => service.ActivateNextWindowAsync(userId, new LongHorizonActivateNextWindowRequest()));

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var planAfter = await verify.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(TargetFinishTimeSource.UserDefined, planAfter.TargetFinishTimeSource);
    }

    /// <summary>
    /// Historical-legacy control (phase §15/§17): simulates a plan confirmed
    /// before this phase shipped (TargetFinishTimeSeconds present,
    /// TargetFinishTimeSource null -- UNKNOWN_LEGACY, never fabricated).
    /// Restart must neither infer a source nor crash ungracefully when Core
    /// generation genuinely needs one.
    /// </summary>
    [Fact]
    public async Task HistoricalNullSource_NeverInferred_FailsClosedTypedAtCore()
    {
        var (planId, rollingId, userId) = await CreateConfirmedFiveDayPlanAsync(3480, targetFinishTimeSource: null);

        // See the identical note in UserDefined_WithoutIndependentEvidence_... --
        // Core generation is attempted eagerly on the first Runway-entry call.
        await CompleteCurrentWindowAsync(rollingId);
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var service = new LongHorizonRollingWindowActivationService(db, NullLogger<LongHorizonRollingWindowActivationService>.Instance, NoOpLongHorizonPersistenceFailureInjector.Instance, "1.1.0");
        await Assert.ThrowsAsync<LongHorizonContinuationBlockedException>(
            () => service.ActivateNextWindowAsync(userId, new LongHorizonActivateNextWindowRequest()));

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var planAfter = await verify.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Null(planAfter.TargetFinishTimeSource); // never inferred/fabricated as a side effect of the failed attempt.
        Assert.Equal(3480, planAfter.TargetFinishTimeSeconds);
    }

    /// <summary>
    /// Governance guard (phase §35): the canonical durable owner remains
    /// TrainingPlan only -- TargetFinishTimeSource must not be newly
    /// persisted on LongHorizonRollingPlanState or LongHorizonRollingSessionState.
    /// </summary>
    [Fact]
    public void RollingStateEntities_DoNotDuplicateTargetFinishTimeSource()
    {
        Assert.DoesNotContain(
            typeof(LongHorizonRollingPlanState).GetProperties(),
            p => p.Name == "TargetFinishTimeSource");
        Assert.DoesNotContain(
            typeof(LongHorizonRollingSessionState).GetProperties(),
            p => p.Name == "TargetFinishTimeSource");
    }
}
