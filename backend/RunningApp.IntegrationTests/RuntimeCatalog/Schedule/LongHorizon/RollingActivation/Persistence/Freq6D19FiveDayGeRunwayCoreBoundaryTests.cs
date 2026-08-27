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
/// Phase 10K-FREQ.6D.19 -- real PostgreSQL proof that a real Intermediate x5D
/// LongHorizon plan reaches canonical Core organically (persisted GE ->
/// persisted Runway entry -> persisted first Core window), all through the
/// real production chain (<see cref="LongHorizonRollingCheckpointRuntime"/>,
/// <see cref="LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync"/>),
/// never a fabricated Core row. Every "restart" below opens a fresh
/// <see cref="RunningApp.Persistence.AppDbContext"/> -- DbContext A is
/// created/mutated/saved and disposed before DbContext B is opened to
/// reload and continue, per this phase's own real-DB test-quality mandate.
/// </summary>
public sealed class Freq6D19OrganicGeRunwayCoreBoundaryTests
{
    private static async Task<(Guid PlanStateId, RollingNumericActivationWindow CoreWindow)> DriveToFirstCoreWindowAsync()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await Freq6D19FiveDayGeRunwayCoreBoundaryFixture.InitializePlanAsync(21);
        var date = Freq6D19FiveDayGeRunwayCoreBoundaryFixture.StartDate.AddDays(29);
        var window = initialWindow;

        // GE=1 week (window1, already persisted by InitializePlanAsync). Runway=8 weeks
        // [2,9] (window2=[2,5] -- real persisted GE->Runway transition; window3=[6,9]).
        // Core begins at week 10 (window4=[10,13] -- real organic Runway->Core transition).
        for (var i = 0; i < 2; i++)
        {
            var call = await Freq6D19FiveDayGeRunwayCoreBoundaryFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        Assert.Equal((6, 9), (window.StartGlobalWeek, window.EndGlobalWeek));

        var coreEntry = await Freq6D19FiveDayGeRunwayCoreBoundaryFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, coreEntry.Outcome);
        var coreWindow = coreEntry.Snapshot!.DarkState.CurrentWindow;
        Assert.Equal((10, 13), (coreWindow.StartGlobalWeek, coreWindow.EndGlobalWeek));
        Assert.Equal(LongHorizonStructuralSegmentType.Core, coreWindow.SegmentsCovered.Single());

        return (planStateId, coreWindow);
    }

    [Fact]
    public async Task OrganicFirstCoreWeek_TwoKeyTwoEasyOneLong_WithDistinctLaneAndSlotIdentity_AfterFreshReload()
    {
        var (planStateId, _) = await DriveToFirstCoreWindowAsync();

        // Fresh, independent DbContext -- never the tracked graph the continuation calls used.
        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var firstCoreWeek = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == 10)
            .ToListAsync();

        Assert.Equal(5, firstCoreWeek.Count);
        Assert.Equal(2, firstCoreWeek.Count(s => s.SessionRole == "KEY_SESSION"));
        Assert.Equal(2, firstCoreWeek.Count(s => s.SessionRole == "EASY_SUPPORT"));
        Assert.Equal(1, firstCoreWeek.Count(s => s.SessionRole == "LONG_RUN"));

        var keySessions = firstCoreWeek.Where(s => s.SessionRole == "KEY_SESSION").ToList();
        Assert.Contains(keySessions, s => s.LaneOrdinal == 0);
        Assert.Contains(keySessions, s => s.LaneOrdinal == 1);
        Assert.Equal(2, keySessions.Select(s => s.LaneOrdinal).Distinct().Count());

        // Repeated EASY_SUPPORT sessions remain independently addressable (distinct SlotOrdinal,
        // no LaneOrdinal ambiguity since EASY never carries lane identity).
        var easySessions = firstCoreWeek.Where(s => s.SessionRole == "EASY_SUPPORT").ToList();
        Assert.All(easySessions, s => Assert.Null(s.LaneOrdinal));
        Assert.Equal(2, easySessions.Select(s => s.SlotOrdinal).Distinct().Count());

        // All five sessions in the week carry distinct, valid SlotOrdinal identity -- no
        // StructuralRole-grouped FIFO reconstruction risk.
        Assert.All(firstCoreWeek, s => Assert.NotNull(s.SlotOrdinal));
        Assert.Equal(5, firstCoreWeek.Select(s => s.SlotOrdinal).Distinct().Count());
    }

    [Fact]
    public async Task OrganicCoreKeySessions_ProfileBackedLineageSurvives_AfterFreshReload()
    {
        var (planStateId, _) = await DriveToFirstCoreWindowAsync();

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var keySessions = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == 10 && s.SessionRole == "KEY_SESSION")
            .ToListAsync();

        Assert.Equal(2, keySessions.Count);
        Assert.All(keySessions, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.ProgressionStageKey));
            // ProfileBacked: both CatalogPrescriptionProfileKey/Version present together
            // (never a ProfileBacked->Legacy fallback -- both-null-or-both-present invariant).
            Assert.False(string.IsNullOrWhiteSpace(s.CatalogPrescriptionProfileKey));
            Assert.NotNull(s.CatalogPrescriptionProfileVersion);
            Assert.False(string.IsNullOrWhiteSpace(s.WorkoutKey));
            Assert.NotNull(s.WorkoutVersion);
        });
    }

    [Fact]
    public async Task SecondaryKeyRepair_UsingRealRepairService_PreservesLane1_PrimaryStaysLane0_AfterFreshReload()
    {
        var (planStateId, _) = await DriveToFirstCoreWindowAsync();

        Guid triggerId;
        string? sourceProgressionStageKey;
        string? sourceProfileKey;
        int? sourceProfileVersion;
        using (var write = LongHorizonPersistenceTestFixture.NewContext())
        {
            var week = await write.LongHorizonRollingWeekStates.Include(w => w.Sessions)
                .SingleAsync(w => w.PlanStateId == planStateId && w.GlobalWeek == 10);
            var secondaryKey = week.Sessions.Single(s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);
            secondaryKey.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.NotToday;
            secondaryKey.NotTodayReason = "schedule";
            secondaryKey.NotTodayRecordedAtUtc = DateTime.UtcNow;
            triggerId = secondaryKey.Id;
            sourceProgressionStageKey = secondaryKey.ProgressionStageKey;
            sourceProfileKey = secondaryKey.CatalogPrescriptionProfileKey;
            sourceProfileVersion = secondaryKey.CatalogPrescriptionProfileVersion;
            await write.SaveChangesAsync();
        }

        LongHorizonScheduleRepairActionKind action;
        using (var orchestrate = LongHorizonPersistenceTestFixture.NewContext())
        {
            var trigger = await orchestrate.LongHorizonRollingSessionStates
                .Include(s => s.Week).ThenInclude(w => w.Plan).ThenInclude(p => p.Weeks).ThenInclude(w => w.Sessions)
                .SingleAsync(s => s.Id == triggerId);
            var outcome = await ScheduleRepairRuntimeOrchestrator.RunAsync(orchestrate, NullLoggerFactory.Instance, trigger, default);
            action = outcome.Action;
        }

        // Fresh, independent reload -- never the DbContext the orchestrator call used.
        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        Assert.NotEqual(LongHorizonScheduleRepairActionKind.Skip, action);

        var replacement = await verify.LongHorizonRollingSessionStates.AsNoTracking()
            .SingleAsync(s => s.AdaptedFromSessionId == triggerId);
        Assert.Equal("KEY_SESSION", replacement.SessionRole);
        Assert.Equal(1, replacement.LaneOrdinal); // secondary KEY repair must not redefine its lane.
        Assert.NotNull(replacement.SlotOrdinal);
        Assert.Equal(sourceProgressionStageKey, replacement.ProgressionStageKey);
        Assert.Equal(sourceProfileKey, replacement.CatalogPrescriptionProfileKey);
        Assert.Equal(sourceProfileVersion, replacement.CatalogPrescriptionProfileVersion);

        // Primary KEY (lane0) is untouched by the lane1 repair.
        var primary = await verify.LongHorizonRollingSessionStates.AsNoTracking()
            .SingleAsync(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == 10
                && s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
        Assert.Equal(0, primary.LaneOrdinal);

        // Repeated EASY identity is unaffected by the KEY repair.
        var easyAfter = await verify.LongHorizonRollingSessionStates.AsNoTracking()
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == 10 && s.SessionRole == "EASY_SUPPORT")
            .ToListAsync();
        Assert.Equal(2, easyAfter.Count);
        Assert.Equal(2, easyAfter.Select(s => s.SlotOrdinal).Distinct().Count());
    }

    [Fact]
    public async Task RepairThenContinuation_NextWindowMaterializesDeterministically_NoDuplicateOrLostSession()
    {
        var (planStateId, initialWindow, candidate, catalogRoot) = await Freq6D19FiveDayGeRunwayCoreBoundaryFixture.InitializePlanAsync(21);
        var date = Freq6D19FiveDayGeRunwayCoreBoundaryFixture.StartDate.AddDays(29);
        var window = initialWindow;
        for (var i = 0; i < 2; i++)
        {
            var call = await Freq6D19FiveDayGeRunwayCoreBoundaryFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
            Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call.Outcome);
            window = call.Snapshot!.DarkState.CurrentWindow;
            date = date.AddDays(28);
        }
        var coreEntry = await Freq6D19FiveDayGeRunwayCoreBoundaryFixture.AdvanceOneWindowAsync(planStateId, window, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, coreEntry.Outcome);
        var coreWindow = coreEntry.Snapshot!.DarkState.CurrentWindow;
        date = date.AddDays(28);

        // Repair the secondary KEY in week 10 before continuing.
        Guid triggerId;
        using (var write = LongHorizonPersistenceTestFixture.NewContext())
        {
            var week10 = await write.LongHorizonRollingWeekStates.Include(w => w.Sessions)
                .SingleAsync(w => w.PlanStateId == planStateId && w.GlobalWeek == 10);
            var secondaryKey = week10.Sessions.Single(s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);
            secondaryKey.OutcomeStatus = LongHorizonRollingSessionOutcomeStatus.NotToday;
            secondaryKey.NotTodayReason = "schedule";
            secondaryKey.NotTodayRecordedAtUtc = DateTime.UtcNow;
            triggerId = secondaryKey.Id;
            await write.SaveChangesAsync();
        }
        using (var orchestrate = LongHorizonPersistenceTestFixture.NewContext())
        {
            var trigger = await orchestrate.LongHorizonRollingSessionStates
                .Include(s => s.Week).ThenInclude(w => w.Plan).ThenInclude(p => p.Weeks).ThenInclude(w => w.Sessions)
                .SingleAsync(s => s.Id == triggerId);
            await ScheduleRepairRuntimeOrchestrator.RunAsync(orchestrate, NullLoggerFactory.Instance, trigger, default);
        }

        // Continue past the repaired window -- fresh reload drives the next real continuation call.
        var next = await Freq6D19FiveDayGeRunwayCoreBoundaryFixture.AdvanceOneWindowAsync(planStateId, coreWindow, date, catalogRoot, candidate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, next.Outcome);
        var nextWindow = next.Snapshot!.DarkState.CurrentWindow;
        Assert.Equal((14, 17), (nextWindow.StartGlobalWeek, nextWindow.EndGlobalWeek));

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var week14Sessions = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == 14)
            .ToListAsync();
        Assert.Equal(5, week14Sessions.Count);
        Assert.Equal(5, week14Sessions.Select(s => s.SlotOrdinal).Distinct().Count());
        Assert.Equal(2, week14Sessions.Count(s => s.SessionRole == "KEY_SESSION"));
        Assert.Contains(week14Sessions, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
        Assert.Contains(week14Sessions, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);

        // Week 10's own repair replacement still resolves execution lineage after this
        // further continuation (repair must not break profile/execution resolution).
        var replacement = await verify.LongHorizonRollingSessionStates.AsNoTracking()
            .SingleAsync(s => s.AdaptedFromSessionId == triggerId);
        Assert.False(string.IsNullOrWhiteSpace(replacement.CatalogPrescriptionProfileKey));
        Assert.NotNull(replacement.CatalogPrescriptionProfileVersion);
    }
}
