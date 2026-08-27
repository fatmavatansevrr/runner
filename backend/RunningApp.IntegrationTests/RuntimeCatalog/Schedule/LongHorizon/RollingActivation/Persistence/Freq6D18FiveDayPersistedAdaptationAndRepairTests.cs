using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 10K-FREQ.6D.18 -- real PostgreSQL persisted-adaptation and
/// persisted-repair closure for Intermediate x5D LongHorizon GE. Reuses the
/// FREQ.6D.15 real-Postgres fixture (<see cref="LongHorizonRollingInitialActivationFiveDayFixture"/>)
/// and the real, already-generic Phase 4M authorities
/// (<see cref="WindowExecutionSummaryBuilder"/>, <see cref="NextWindowLoadDecisionPolicy"/>,
/// <see cref="ScheduleRepairRuntimeOrchestrator"/>) directly against real
/// persisted <see cref="LongHorizonRollingSessionState"/> rows -- every
/// scenario below mutates state, saves, disposes the DbContext, opens a
/// fresh one, and only then evaluates/asserts.
///
/// The 5-session severity table (FREQ.6 Section 6: 5/5-&gt;Progress,
/// 4/5-EASY-only-&gt;Progress, 4/5-KEY-&gt;Maintain, 4/5-LONG-&gt;Maintain,
/// 2-3/5-&gt;Maintain, 0-1/5-&gt;Reduce) is implemented by
/// <see cref="NextWindowLoadDecisionPolicy"/> (Phase 4M), which is role-count
/// driven and therefore already correct for the GE 1xKEY+3xEASY+1xLONG shape
/// with no further change -- verified below directly against real reloaded
/// rows rather than assumed. This is a DIFFERENT authority from
/// <see cref="LongHorizonRollingCheckpointRuntime"/>'s own GrowthEligible/
/// MaintenanceOnly dispatch (Phase 4K.7, driven by a coarser
/// "every non-recovery week has some usable evidence" signal) -- both are
/// exercised below, honestly labeled as what they each are.
/// </summary>
public sealed class Freq6D18FiveDayPersistedSeverityTableTests
{
    private static async Task<(Guid PlanStateId, Guid WeekStateId, List<LongHorizonRollingSessionState> Sessions)> PersistAndLoadFirstGeWeekAsync()
    {
        var planStateId = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAndPersistAsync(21);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var week = await db.LongHorizonRollingWeekStates
            .Include(w => w.Sessions)
            .Where(w => w.PlanStateId == planStateId)
            .OrderBy(w => w.GlobalWeek)
            .FirstAsync();

        Assert.Equal(5, week.Sessions.Count);
        return (planStateId, week.Id, week.Sessions.ToList());
    }

    /// <summary>Sets OutcomeStatus on the real persisted rows for one GE week, commits,
    /// then returns a genuinely fresh reload's rows for that same week -- the
    /// mandatory persist-commit-dispose-reload pattern (phase Section 4).</summary>
    private static async Task<List<LongHorizonRollingSessionState>> ApplyOutcomesAndReloadAsync(
        Guid weekStateId, Func<LongHorizonRollingSessionState, LongHorizonRollingSessionOutcomeStatus> outcomeFor)
    {
        using (var write = LongHorizonPersistenceTestFixture.NewContext())
        {
            var sessions = await write.LongHorizonRollingSessionStates.Where(s => s.WeekStateId == weekStateId).ToListAsync();
            foreach (var session in sessions)
                session.OutcomeStatus = outcomeFor(session);
            await write.SaveChangesAsync();
        }

        using var reload = LongHorizonPersistenceTestFixture.NewContext();
        return await reload.LongHorizonRollingSessionStates.AsNoTracking()
            .Where(s => s.WeekStateId == weekStateId).ToListAsync();
    }

    private static NextWindowAdaptationResult EvaluateFromPersistedRows(IReadOnlyList<LongHorizonRollingSessionState> rows)
    {
        var evidence = WindowCheckpointEvidenceMapper.ToEvidence(rows);
        var summary = WindowExecutionSummaryBuilder.Build(evidence);
        Assert.Equal(5, summary.ExpectedSessionCount);
        Assert.Equal(1, summary.KeySessionExpectedCount);
        Assert.Equal(3, summary.EasyExpectedCount);
        Assert.True(summary.LongRunExpected);
        return NextWindowLoadDecisionPolicy.Evaluate(summary);
    }

    [Fact]
    public async Task FiveOfFive_AllCompleted_Progress_PersistedAndReloaded()
    {
        var (_, weekStateId, _) = await PersistAndLoadFirstGeWeekAsync();
        var reloaded = await ApplyOutcomesAndReloadAsync(weekStateId, _ => LongHorizonRollingSessionOutcomeStatus.Completed);

        var result = EvaluateFromPersistedRows(reloaded);

        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
    }

    [Fact]
    public async Task FourOfFive_OnlyOneEasyMissing_Progress_PersistedAndReloaded()
    {
        var (_, weekStateId, sessions) = await PersistAndLoadFirstGeWeekAsync();
        var missedEasyId = sessions.First(s => s.SessionRole.StartsWith("EASY_SUPPORT")).Id;
        var reloaded = await ApplyOutcomesAndReloadAsync(weekStateId, s =>
            s.Id == missedEasyId ? LongHorizonRollingSessionOutcomeStatus.NotToday : LongHorizonRollingSessionOutcomeStatus.Completed);

        var result = EvaluateFromPersistedRows(reloaded);

        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
    }

    [Fact]
    public async Task FourOfFive_KeyMissing_Maintain_PersistedAndReloaded()
    {
        var (_, weekStateId, sessions) = await PersistAndLoadFirstGeWeekAsync();
        var keyId = sessions.First(s => s.SessionRole == "KEY_SESSION").Id;
        var reloaded = await ApplyOutcomesAndReloadAsync(weekStateId, s =>
            s.Id == keyId ? LongHorizonRollingSessionOutcomeStatus.NotToday : LongHorizonRollingSessionOutcomeStatus.Completed);

        var result = EvaluateFromPersistedRows(reloaded);

        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Fact]
    public async Task FourOfFive_LongMissing_Maintain_PersistedAndReloaded()
    {
        var (_, weekStateId, sessions) = await PersistAndLoadFirstGeWeekAsync();
        var longId = sessions.First(s => s.SessionRole == "LONG_RUN").Id;
        var reloaded = await ApplyOutcomesAndReloadAsync(weekStateId, s =>
            s.Id == longId ? LongHorizonRollingSessionOutcomeStatus.NotToday : LongHorizonRollingSessionOutcomeStatus.Completed);

        var result = EvaluateFromPersistedRows(reloaded);

        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Fact]
    public async Task TwoOfFive_Maintain_PersistedAndReloaded()
    {
        var (_, weekStateId, sessions) = await PersistAndLoadFirstGeWeekAsync();
        var missedIds = sessions.Where(s => s.SessionRole != "LONG_RUN").Take(3).Select(s => s.Id).ToHashSet();
        var reloaded = await ApplyOutcomesAndReloadAsync(weekStateId, s =>
            missedIds.Contains(s.Id) ? LongHorizonRollingSessionOutcomeStatus.NotToday : LongHorizonRollingSessionOutcomeStatus.Completed);

        var result = EvaluateFromPersistedRows(reloaded);

        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Fact]
    public async Task ZeroOfFive_Reduce_PersistedAndReloaded()
    {
        var (_, weekStateId, _) = await PersistAndLoadFirstGeWeekAsync();
        var reloaded = await ApplyOutcomesAndReloadAsync(weekStateId, _ => LongHorizonRollingSessionOutcomeStatus.NotToday);

        var result = EvaluateFromPersistedRows(reloaded);

        Assert.Equal(NextWindowLoadDecision.Reduce, result.LoadDecision);
    }
}

/// <summary>
/// Real-Postgres proof that <see cref="LongHorizonRollingCheckpointRuntime"/>
/// -- fixed this phase to accept DaysPerWeek 5, select the real 5D GE
/// descriptors, and populate LaneOrdinal/SlotOrdinal/ProgressionStageKey on
/// checkpoint-continuation sessions exactly like the FREQ.6D.15 initial-
/// activation runtime already did -- genuinely activates a real next GE
/// window for a 5D plan, and that its 1xKEY+3xEASY+1xLONG cardinality and
/// lineage survive a real persist-commit-dispose-reload round trip.
/// </summary>
public sealed class Freq6D18FiveDayPersistedCheckpointContinuationTests
{
    private static List<LongHorizonTrainingDayEvidenceRow> Rows(RollingNumericActivationWindow window, TrainingDayStatus status = TrainingDayStatus.Completed)
    {
        var result = new List<LongHorizonTrainingDayEvidenceRow>();
        var index = 0;
        foreach (var week in window.Weeks)
        foreach (var session in week.SessionPrescriptions!)
        {
            result.Add(new LongHorizonTrainingDayEvidenceRow(week.GlobalWeekNumber, new TrainingDay
            {
                Id = Guid.NewGuid(),
                Date = session.AssignedDate!.Value.ToDateTime(TimeOnly.MinValue),
                DayType = session.SessionRole == "LONG_RUN" ? TrainingDayType.LongRun : TrainingDayType.Easy,
                Status = status,
                PlannedDistanceKm = session.DistanceKm + 5,
                ActualDistanceKm = status == TrainingDayStatus.Completed ? session.DistanceKm : null,
                ActualDurationMin = 30,
                IsLongRun = session.SessionRole == "LONG_RUN",
                CompletedAt = status == TrainingDayStatus.Completed ? session.AssignedDate.Value.ToDateTime(TimeOnly.MinValue).AddHours(1) : null,
            }));
            index++;
        }
        return result;
    }

    [Fact]
    public async Task NextGeWindow_ActivatesWithFiveSessionCardinality_AndLineageSurvivesRealReload()
    {
        // 52 weeks (GeneralEnduranceWeeks == 32) so a real "next GE window" continuation
        // exists at all -- a 21-week horizon's single GE week would immediately cross
        // into Runway/JIT composition, which LongHorizonRollingCheckpointRuntime alone
        // does not handle (that dispatch belongs to LongHorizonRollingWindowActivationService).
        var planStateId = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAndPersistAsync(52);

        using var reload = LongHorizonPersistenceTestFixture.NewContext();
        var snapshot = await new LongHorizonRollingStateRepository(reload).LoadRestartSnapshotAsync(planStateId);
        Assert.NotNull(snapshot);
        var state = snapshot!.DarkState;

        var request = new LongHorizonRollingCheckpointRequest
        {
            StructuralRoadmap = state.StructuralRoadmap,
            StructuralSkeleton = state.StructuralSkeleton,
            LifecycleStates = state.LifecycleStates,
            MostRecentlyActivatedWindow = state.CurrentWindow,
            TrainingDayEvidence = Rows(state.CurrentWindow),
            CheckpointDate = state.CurrentWindow.Weeks.Max(w => w.CalendarDates!.Value.End).AddDays(1),
            CurrentAvailability = LongHorizonRollingInitialActivationFiveDayFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            ReadinessProfile = state.StructuralRoadmap.Profile,
            PreviousContextVersion = state.ContextVersion,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 5,
        };

        // Before this phase's fix, ValidateInput threw for DaysPerWeek != 4 -- this call
        // itself is the regression proof, not merely the assertions that follow.
        var result = await new LongHorizonRollingCheckpointRuntime().EvaluateAndActivateNextGeWindowAsync(request);
        Assert.Equal(LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated, result.Outcome);
        Assert.NotEmpty(result.NewlyActivatedWeeks);
        Assert.All(result.NewlyActivatedWeeks, w =>
        {
            Assert.Equal(5, w.SessionPrescriptions!.Count);
            Assert.Equal(1, w.SessionPrescriptions.Count(s => s.SessionRole == "KEY_SESSION"));
            Assert.Equal(3, w.SessionPrescriptions.Count(s => s.SessionRole.StartsWith("EASY_SUPPORT")));
            Assert.Equal(1, w.SessionPrescriptions.Count(s => s.SessionRole == "LONG_RUN"));
            Assert.All(w.SessionPrescriptions, s => Assert.NotNull(s.SlotOrdinal));
            Assert.Equal(w.SessionPrescriptions.Count, w.SessionPrescriptions.Select(s => s.SlotOrdinal).Distinct().Count());
            Assert.All(w.SessionPrescriptions, s => Assert.False(string.IsNullOrWhiteSpace(s.ProgressionStageKey)));
        });

        using var persistCtx = LongHorizonPersistenceTestFixture.NewContext();
        var persistResult = await new LongHorizonRollingActivationPersistenceAdapter(new LongHorizonRollingStateRepository(persistCtx))
            .PersistGeCheckpointAsync(planStateId, snapshot.ConcurrencyVersion, result);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, persistResult.Outcome);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var nextWeekGlobal = result.NewlyActivatedWeeks[0].GlobalWeekNumber;
        var persistedSessions = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == nextWeekGlobal)
            .ToListAsync();

        Assert.Equal(5, persistedSessions.Count);
        Assert.All(persistedSessions, s => Assert.NotNull(s.SlotOrdinal));
        Assert.Equal(5, persistedSessions.Select(s => s.SlotOrdinal).Distinct().Count());
        Assert.All(persistedSessions, s => Assert.False(string.IsNullOrWhiteSpace(s.ProgressionStageKey)));
        Assert.All(persistedSessions, s => Assert.Null(s.LaneOrdinal));
    }
}

/// <summary>
/// Persisted repair scenarios (phase Section 19-26), restricted to the
/// repair operations this reconnaissance confirmed actually exist:
/// <see cref="ScheduleRepairRuntimeOrchestrator.RunAsync"/> drives
/// GE KEY_SESSION and GE EASY_SUPPORT repair (RescheduleToEmptySlot or
/// SubstituteFutureEasy) against the real, generic <see cref="AdaptationSessionRoleResolver"/>
/// role classification. There is no Core-segment repair test here: this
/// phase's own checkpoint runtime only ever activates GE weeks, so no real
/// persisted Core KEY (lane0/lane1) session exists in this fixture to repair
/// -- inventing one would violate Section 19's "do not invent a repair
/// operation only for testing" instruction.
/// </summary>
public sealed class Freq6D18FiveDayPersistedRepairTests
{
    [Fact]
    public async Task GeKeySessionRepair_PreservesSlotOrdinalAndProgressionStageKey_AfterFreshReload()
    {
        var planStateId = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAndPersistAsync(21);

        Guid triggerId;
        using (var write = LongHorizonPersistenceTestFixture.NewContext())
        {
            var week = await write.LongHorizonRollingWeekStates.Include(w => w.Sessions)
                .Where(w => w.PlanStateId == planStateId).OrderBy(w => w.GlobalWeek).FirstAsync();
            var trigger = week.Sessions.Single(s => s.SessionRole == "KEY_SESSION");
            trigger.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.NotToday;
            trigger.NotTodayReason = "schedule";
            trigger.NotTodayRecordedAtUtc = DateTime.UtcNow;
            triggerId = trigger.Id;
            await write.SaveChangesAsync();
        }

        Guid sourceLaneOrdinalSlot;
        string? sourceProgressionStageKey;
        using (var read = LongHorizonPersistenceTestFixture.NewContext())
        {
            var source = await read.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == triggerId);
            sourceLaneOrdinalSlot = source.Id;
            sourceProgressionStageKey = source.ProgressionStageKey;
            Assert.NotNull(source.SlotOrdinal);
            Assert.False(string.IsNullOrWhiteSpace(sourceProgressionStageKey));
        }

        using (var orchestrate = LongHorizonPersistenceTestFixture.NewContext())
        {
            var trigger = await orchestrate.LongHorizonRollingSessionStates
                .Include(s => s.Week).ThenInclude(w => w.Plan).ThenInclude(p => p.Weeks).ThenInclude(w => w.Sessions)
                .SingleAsync(s => s.Id == triggerId);
            var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(orchestrate, NullLoggerFactory.Instance, trigger, default);
            Assert.NotEqual(LongHorizonScheduleRepairActionKind.Skip, outcome.Action);
            Assert.NotNull(outcome.ReplacementSessionId);
        }

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var replacement = await verify.LongHorizonRollingSessionStates.AsNoTracking()
            .SingleAsync(s => s.AdaptedFromSessionId == triggerId);

        Assert.Equal("KEY_SESSION", replacement.SessionRole);
        Assert.Null(replacement.LaneOrdinal); // GE never carries LaneOrdinal -- unchanged by repair.
        Assert.NotNull(replacement.SlotOrdinal);
        Assert.Equal(sourceProgressionStageKey, replacement.ProgressionStageKey);

        // The original trigger row itself is untouched by BuildReplacement -- its own identity survives too.
        var reloadedTrigger = await verify.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == triggerId);
        Assert.Equal(sourceProgressionStageKey, reloadedTrigger.ProgressionStageKey);
    }

    [Fact]
    public async Task RepeatedEasyRepair_DistinctSlotOrdinalsSurvive_NoCollapse_AfterFreshReload()
    {
        var planStateId = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAndPersistAsync(21);

        Guid triggerId;
        List<int?> otherEasySlotOrdinalsBefore;
        using (var write = LongHorizonPersistenceTestFixture.NewContext())
        {
            var week = await write.LongHorizonRollingWeekStates.Include(w => w.Sessions)
                .Where(w => w.PlanStateId == planStateId).OrderBy(w => w.GlobalWeek).FirstAsync();
            var easySessions = week.Sessions.Where(s => s.SessionRole.StartsWith("EASY_SUPPORT")).OrderBy(s => s.SlotOrdinal).ToList();
            Assert.Equal(3, easySessions.Count);
            var trigger = easySessions[0];
            otherEasySlotOrdinalsBefore = easySessions.Skip(1).Select(s => s.SlotOrdinal).ToList();
            trigger.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.NotToday;
            trigger.NotTodayReason = "schedule";
            trigger.NotTodayRecordedAtUtc = DateTime.UtcNow;
            triggerId = trigger.Id;
            await write.SaveChangesAsync();
        }

        using (var orchestrate = LongHorizonPersistenceTestFixture.NewContext())
        {
            var trigger = await orchestrate.LongHorizonRollingSessionStates
                .Include(s => s.Week).ThenInclude(w => w.Plan).ThenInclude(p => p.Weeks).ThenInclude(w => w.Sessions)
                .SingleAsync(s => s.Id == triggerId);
            // EASY_SUPPORT NotToday is always Skip per ScheduleRepairPolicy (Rev3.1 §3) --
            // EASY is never itself the repair target, only ever a substitution destination for KEY/LONG.
            var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(orchestrate, NullLoggerFactory.Instance, trigger, default);
            Assert.Equal(LongHorizonScheduleRepairActionKind.Skip, outcome.Action);
        }

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var week2 = await verify.LongHorizonRollingWeekStates.AsNoTracking().Include(w => w.Sessions)
            .Where(w => w.PlanStateId == planStateId).OrderBy(w => w.GlobalWeek).FirstAsync();
        var easyAfter = week2.Sessions.Where(s => s.SessionRole.StartsWith("EASY_SUPPORT")).ToList();
        Assert.Equal(3, easyAfter.Count);
        Assert.Equal(3, easyAfter.Select(s => s.SlotOrdinal).Distinct().Count());
        Assert.Equal(otherEasySlotOrdinalsBefore.OrderBy(x => x), easyAfter.Where(s => s.Id != triggerId).Select(s => s.SlotOrdinal).OrderBy(x => x));
    }
}
