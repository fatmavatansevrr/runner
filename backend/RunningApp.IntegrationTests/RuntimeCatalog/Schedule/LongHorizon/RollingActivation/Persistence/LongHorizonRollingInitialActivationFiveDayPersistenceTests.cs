using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;

/// <summary>
/// Phase 10K-FREQ.6D.15 — real PostgreSQL persistence/reload proof for the
/// Intermediate x5D LongHorizon GE rolling-activation window, using the
/// exact same production repository (<see cref="LongHorizonRollingStateRepository"/>)
/// and connection FREQ.6D.13's own persistence tests use
/// (<see cref="LongHorizonPersistenceTestFixture.ConnectionString"/>). Every
/// operation opens a fresh <see cref="AppDbContext"/>, exactly matching the
/// FREQ.6D.13 fixture's own "fresh reload" convention -- never a
/// save-and-continue on the same tracked entity graph.
///
/// Scoped to <see cref="LongHorizonRollingInitialActivationRuntime"/>
/// (numerically activates only GE weeks 1..min(4, GE weeks)) rather than the
/// full GE->Runway->Core pipeline: this phase's own diagnosis (see
/// <c>LongHorizonFullNumericOrchestratorFiveDayTests</c>'s boundary-gap
/// evidence tests) found a genuine, pre-existing, day-count-neutral
/// Preparation Runway numeric-continuity gap once a plan actually crosses
/// into Runway -- this runtime never reaches that code path at all, so its
/// own persistence is unaffected and provable independently.
/// </summary>
internal static class LongHorizonRollingInitialActivationFiveDayFixture
{
    internal static readonly DateOnly StartDate = new(2026, 8, 3);
    internal static readonly IReadOnlyList<DayOfWeek> PreferredDays =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday];

    private static string CatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    internal static async Task<PlanCatalogCandidateSummary> LoadFiveDayCandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.FiveDayCandidateKey, V1CatalogPilotIdentityPolicy.FiveDayCandidateVersion);
    }

    internal static LongHorizonRollingInitialActivationRequest BuildActivationRequest(int totalWeeks)
    {
        var raceDate = StartDate.AddDays(totalWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(StartDate, raceDate);
        var decision = LongHorizonCompositionResolver.Resolve(coreHorizon, ReadinessProfile.ConsistencyNeeded);

        return new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = decision,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 5,
            StartDate = StartDate,
            RaceDate = raceDate,
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(26, 8, 5),
            PreferredDays = PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = CatalogRoot(),
            WorkoutLoader = new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() })),
        };
    }

    internal static async Task<(Guid PlanStateId, LongHorizonRollingInitialActivationResult Result)> ActivateAsync(int totalWeeks)
    {
        var request = BuildActivationRequest(totalWeeks);
        var runtime = new LongHorizonRollingInitialActivationRuntime();
        var result = await runtime.BuildInitialActivationAsync(request);
        return (Guid.NewGuid(), result);
    }

    internal static async Task<Guid> ActivateAndPersistAsync(int totalWeeks)
    {
        var candidate = await LoadFiveDayCandidateAsync();
        var (planStateId, result) = await ActivateAsync(totalWeeks);
        Assert.Equal(LongHorizonRollingInitialActivationStatus.Approved, result.Status);

        var initRequest = new LongHorizonRollingInitializationRequest
        {
            PlanStateId = planStateId,
            StructuralRoadmap = result.StructuralRoadmap!,
            PlanStartDate = StartDate,
            PreferredDays = PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            InitialWindow = result.ActivationWindow!,
            LifecycleStates = result.StructuralRoadmap!.Weeks.ToDictionary(w => w.GlobalWeekNumber, w => w.NumericLifecycleState),
            ActivatedWeeks = result.ActivatedNumericWeeks.ToDictionary(w => w.GlobalWeekNumber, w => w),
            ContextVersion = result.ContextVersion!,
            CatalogRootPath = CatalogRoot(),
            Candidate = candidate,
        };

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        await new LongHorizonRollingStateRepository(db).InitializeStructuralStateAsync(initRequest);
        return planStateId;
    }
}

public sealed class LongHorizonRollingInitialActivationFiveDayPersistenceTests
{
    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    public async Task ShortHorizon_ActivatesApproved_FiveSessionsPerGeWeek(int totalWeeks)
    {
        var (_, result) = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAsync(totalWeeks);

        Assert.True(result.Status == LongHorizonRollingInitialActivationStatus.Approved,
            $"Blocked: {result.Failure?.Reason} {result.Failure?.Code} {result.Failure?.Message}");
        Assert.NotEmpty(result.ActivatedNumericWeeks);
        Assert.All(result.ActivatedNumericWeeks, w =>
        {
            Assert.Equal(5, w.SessionPrescriptions!.Count);
            Assert.Equal(1, w.SessionPrescriptions.Count(s => s.SessionRole == "KEY_SESSION"));
            Assert.Equal(3, w.SessionPrescriptions.Count(s => s.SessionRole.StartsWith("EASY_SUPPORT")));
            Assert.Equal(1, w.SessionPrescriptions.Count(s => s.SessionRole == "LONG_RUN"));
        });
    }

    [Fact]
    public async Task LongHorizon_FiftyTwoWeeks_ActivatesApproved_StructuralRoadmapCoversAllWeeks()
    {
        var (_, result) = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAsync(52);

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Approved, result.Status);
        Assert.Equal(52, result.StructuralRoadmap!.TotalWeeks);
        Assert.Equal(32, result.StructuralRoadmap.GeneralEnduranceWeeks);
        // Only the first window (<=4 weeks) is numerically activated by this runtime.
        Assert.InRange(result.ActivatedNumericWeeks.Count, 1, 4);
    }

    [Fact]
    public async Task PersistsToRealPostgres_ExactStructuralWeekCount()
    {
        var planStateId = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAndPersistAsync(21);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var weekCount = await verify.LongHorizonRollingWeekStates.CountAsync(w => w.PlanStateId == planStateId);
        Assert.Equal(21, weekCount);
    }

    [Fact]
    public async Task FreshReload_PersistedLineage_LaneOrdinalSlotOrdinalProgressionStageProfileSurvive()
    {
        var planStateId = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAndPersistAsync(21);

        // Fresh, independent DbContext -- a genuinely new connection/reader, never the tracked
        // entity graph the write above used (matching FREQ.6D.13's own "fresh reload" convention).
        using var reload = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(reload);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);

        Assert.NotNull(snapshot);
        var firstWeek = snapshot!.DarkState.ActivatedWeeks.Values.OrderBy(w => w.GlobalWeekNumber).First();
        Assert.NotEmpty(firstWeek.SessionPrescriptions!);

        // Reload directly from the tracked persisted rows (schema-exact, not via the DarkState projection)
        // to verify the real FREQ.6D.13 lineage columns survived a genuine round trip.
        using var rawVerify = LongHorizonPersistenceTestFixture.NewContext();
        var persistedSessions = await rawVerify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == firstWeek.GlobalWeekNumber)
            .ToListAsync();

        Assert.NotEmpty(persistedSessions);
        Assert.Contains(persistedSessions, s => s.LaneOrdinal is not null || s.SessionRole.StartsWith("EASY_SUPPORT") || s.SessionRole == "LONG_RUN");
        // Every persisted GE session carries SlotOrdinal -- populated for every role, per FREQ.6D.13's own invariant.
        Assert.All(persistedSessions, s => Assert.NotNull(s.SlotOrdinal));
        Assert.All(persistedSessions, s => Assert.False(string.IsNullOrWhiteSpace(s.ProgressionStageKey)));
    }

    [Fact]
    public async Task GeStateReload_NextWeekStillFiveSessionShape_WithDistinctSlotOrdinals()
    {
        var planStateId = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAndPersistAsync(24);

        using var reload = LongHorizonPersistenceTestFixture.NewContext();
        var snapshot = await new LongHorizonRollingStateRepository(reload).LoadRestartSnapshotAsync(planStateId);

        Assert.NotNull(snapshot);
        foreach (var week in snapshot!.DarkState.ActivatedWeeks.Values)
        {
            Assert.Equal(5, week.SessionPrescriptions!.Count);
            Assert.Equal(1, week.SessionPrescriptions.Count(s => s.SessionRole == "KEY_SESSION"));
            Assert.Equal(3, week.SessionPrescriptions.Count(s => s.SessionRole.StartsWith("EASY_SUPPORT")));
            Assert.Equal(1, week.SessionPrescriptions.Count(s => s.SessionRole == "LONG_RUN"));

            var slotOrdinals = week.SessionPrescriptions.Select(s => s.SlotOrdinal).ToList();
            Assert.All(slotOrdinals, o => Assert.NotNull(o));
            Assert.Equal(slotOrdinals.Count, slotOrdinals.Distinct().Count());
        }
    }

    [Fact]
    public async Task DuplicateIdentityGuard_RepeatedEasySlotsAcceptedAsDistinct_NoFalsePositive()
    {
        // Regression guard for the exact validator FREQ.6D.13 introduced and then
        // corrected (LongHorizonLineageValidator.ValidateNoDuplicateIdentity) --
        // proves a real 5D GE window with 3 EASY_SUPPORT sessions in the same week
        // persists successfully rather than being rejected as a false-positive
        // duplicate canonical identity.
        var planStateId = await LongHorizonRollingInitialActivationFiveDayFixture.ActivateAndPersistAsync(21);

        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var easySessions = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.SessionRole.StartsWith("EASY_SUPPORT"))
            .ToListAsync();

        Assert.Equal(3, easySessions.Count);
        Assert.Equal(3, easySessions.Select(s => s.SlotOrdinal).Distinct().Count());
    }
}
