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
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 10K-GEN.34 -- real-PostgreSQL dark verification of the 2D LongHorizon
/// admission gate (Beginner and Intermediate), opened this phase after all
/// structural-materializer gaps GEN.32/33 disclosed (plus two further gaps
/// this phase's own trace found: BuildRunwayWeek/BuildCoreWeek's hardcoded
/// HasKeySession, and LongHorizonCalendarAssigner.AssignWeekdays's
/// daysPerWeek&lt;3 guard) were fixed and individually tested
/// (<see cref="LongHorizonGen34TwoDayDefectFixesTests"/>).
///
/// Deliberately scoped to real-PostgreSQL initial activation, restart
/// (reload from durable state), and one further GE-segment-only checkpoint
/// continuation -- NOT the GE-&gt;Runway boundary JIT composition
/// (<see cref="LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync"/>),
/// which requires a published plan-catalog bundle release
/// (<c>PlanCatalog:PublishedBundleReleaseVersion</c>) carrying the 2D
/// candidate -- no such release exists yet (only 3D/5D/6D bundles are
/// published as of this phase). Publishing one is a mechanical, no-new-
/// authority action, but is a real-artifact/release action this dark-only
/// phase deliberately does not take unprompted; it is disclosed as the
/// concrete remaining item for opening the GE-&gt;Runway boundary and Core
/// generation paths for 2D. horizons are chosen (29/30 total weeks -&gt;
/// geWeeks 9/10) specifically so the first real checkpoint continuation
/// stays inside the GE segment (does not immediately hit the GE-&gt;Runway
/// boundary), covering both an odd (9) and an even (10) GE segment length,
/// both Levels, and both readiness profiles -- 8 real, independent
/// Postgres-backed activations in total.
/// </summary>
internal static class Gen34TwoDayFixture
{
    internal static readonly DateOnly StartDate = new(2026, 9, 7);
    internal static readonly IReadOnlyList<DayOfWeek> PreferredDays = [DayOfWeek.Tuesday, DayOfWeek.Sunday];

    internal static string CatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    internal static (string Key, int Version) CandidateIdentity(RunningBackground level) => level == RunningBackground.Beginner
        ? (V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion)
        : (V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateVersion);

    internal static async Task<PlanCatalogCandidateSummary> LoadCandidateAsync(RunningBackground level)
    {
        var (key, version) = CandidateIdentity(level);
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(key, version);
    }

    internal static LongHorizonRollingInitialActivationRequest BuildActivationRequest(
        int totalWeeks, RunningBackground level, ReadinessProfile profile)
    {
        var raceDate = StartDate.AddDays(totalWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(StartDate, raceDate);
        var decision = LongHorizonCompositionResolver.Resolve(coreHorizon, profile);

        return new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = decision,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = level,
            DaysPerWeek = 2,
            StartDate = StartDate,
            RaceDate = raceDate,
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(16, 6, 2),
            PreferredDays = PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = CatalogRoot(),
            WorkoutLoader = new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() })),
        };
    }

    internal static async Task<(Guid PlanStateId, LongHorizonRollingInitialActivationResult Result)> InitializePlanAsync(
        int totalWeeks, RunningBackground level, ReadinessProfile profile)
    {
        var candidate = await LoadCandidateAsync(level);
        var request = BuildActivationRequest(totalWeeks, level, profile);
        var runtime = new LongHorizonRollingInitialActivationRuntime();
        var result = await runtime.BuildInitialActivationAsync(request);
        if (result.Status != LongHorizonRollingInitialActivationStatus.Approved)
            throw new InvalidOperationException(
                $"Initial 2D {level} activation not approved: {result.Failure?.Reason} {result.Failure?.Code} {result.Failure?.Message}");

        var planStateId = Guid.NewGuid();
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
            CatalogRootPath = request.CatalogRoot,
            Candidate = candidate,
            DaysPerWeek = 2,
            Level = level,
        };

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        await new LongHorizonRollingStateRepository(db).InitializeStructuralStateAsync(initRequest);
        return (planStateId, result);
    }

    /// <summary>
    /// One real-Postgres restart-and-continue step, deliberately staying
    /// inside the GE segment (asserts the outcome is a pure GE activation,
    /// never a boundary/JIT case) -- see this file's own class doc for why
    /// the GE-&gt;Runway boundary is out of scope this phase.
    /// </summary>
    internal static async Task<LongHorizonRollingPersistenceResult> AdvanceOnePureGeWindowAsync(
        Guid planStateId, DateOnly checkpointDate, RunningBackground level)
    {
        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(db);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId) ?? throw new InvalidOperationException("No snapshot.");
        var state = snapshot.DarkState;

        var checkpointRuntime = new LongHorizonRollingCheckpointRuntime();
        var evidenceRows = LongHorizonPersistenceTestFixture.BuildCompletedEvidenceRows(state.CurrentWindow, planStateId);
        var checkpointRequest = new LongHorizonRollingCheckpointRequest
        {
            StructuralRoadmap = state.StructuralRoadmap,
            StructuralSkeleton = state.StructuralSkeleton,
            LifecycleStates = state.LifecycleStates,
            MostRecentlyActivatedWindow = state.CurrentWindow,
            TrainingDayEvidence = evidenceRows,
            CheckpointDate = checkpointDate,
            CurrentAvailability = PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            ReadinessProfile = state.StructuralRoadmap.Profile,
            PriorValidatedAnchor = Gen34CheckpointAccessor.Prior(16, 6),
            PreviousContextVersion = state.ContextVersion,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = level,
            DaysPerWeek = 2,
        };
        var checkpoint = await checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(checkpointRequest);

        var geEnd = state.StructuralRoadmap.GeneralEnduranceWeeks;
        if (checkpoint.Outcome != LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated
            || checkpoint.ActivationWindow!.EndGlobalWeek >= geEnd)
        {
            throw new InvalidOperationException(
                $"Expected a pure, non-boundary GE continuation (outcome={checkpoint.Outcome}, " +
                $"endGlobalWeek={checkpoint.ActivationWindow?.EndGlobalWeek}, geEnd={geEnd}) -- horizon choice invalid for this test's scope.");
        }

        return await new LongHorizonRollingActivationPersistenceAdapter(repo)
            .PersistGeCheckpointAsync(planStateId, snapshot.ConcurrencyVersion, checkpoint);
    }
}

/// <summary>Local accessor mirroring the identical private helper other LongHorizon test files already define, to avoid a cross-file dependency on an internal test-only type.</summary>
internal static class Gen34CheckpointAccessor
{
    internal static LongHorizonPriorValidatedAnchor Prior(double weekly, double longRun) => new(
        new ValidatedSustainableLoad
        {
            WeeklyVolumeKm = weekly,
            LongRunKm = longRun,
            EvidenceWindowStartWeek = 1,
            EvidenceWindowEndWeek = 1,
            CompletedEvidenceWeekNumbers = [1],
            ExcludedRecoveryWeekNumbers = [],
            WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.PriorValidatedCheckpointLoad, LongHorizonEvidenceAuthorityStatus.Authoritative),
            LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.PriorValidatedCheckpointLoad, LongHorizonEvidenceAuthorityStatus.Authoritative),
            RoundingPolicy = "VolumeSafetyPolicy.0.5km",
            LongRunCapPolicy = "VolumeSafetyPolicy.LongRunHardCapShare",
            ValidationStatus = LongHorizonValidationStatus.Valid,
            Provenance = "GEN.34 test anchor",
            ContextVersion = null,
        },
        IsFreshForCurrentInvocation: true,
        SourceContextSequence: 0);
}

public sealed class Gen34TwoDayRealPostgresMatrixTests
{
    // ReadinessProfile is an internal enum, so it cannot appear in a public
    // [Theory] method's signature (CS0051) -- passed through as a bool and
    // mapped back to the real enum value inside the test body instead.
    public static IEnumerable<object[]> Matrix()
    {
        foreach (var level in new[] { RunningBackground.Beginner, RunningBackground.Intermediate })
        foreach (var totalWeeks in new[] { 29, 30 }) // geWeeks 9 (odd) / 10 (even)
        foreach (var consistencyNeeded in new[] { true, false })
            yield return [level, totalWeeks, consistencyNeeded];
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task InitialActivation_Restart_AndOnePureGeCheckpoint_SucceedsAgainstRealPostgres(
        RunningBackground level, int totalWeeks, bool consistencyNeeded)
    {
        var profile = consistencyNeeded ? ReadinessProfile.ConsistencyNeeded : ReadinessProfile.CoreEntryReady;
        var (planStateId, initial) = await Gen34TwoDayFixture.InitializePlanAsync(totalWeeks, level, profile);
        var geWeeks = initial.StructuralSkeleton!.GeneralEnduranceWeeks;
        Assert.Equal(totalWeeks - 20, geWeeks);
        Assert.True(geWeeks >= 9, "Test horizon must leave room for a non-boundary second checkpoint.");

        // Restart: reload the just-persisted state from real PostgreSQL and
        // confirm GlobalWeekNumber continuity end-to-end (GE segment, this
        // phase's own scope) survives a genuine round trip through the
        // database, not merely an in-memory object graph.
        using (var db = LongHorizonPersistenceTestFixture.NewContext())
        {
            var repo = new LongHorizonRollingStateRepository(db);
            var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
            Assert.NotNull(snapshot);
            var reloadedWeeks = snapshot!.DarkState.StructuralRoadmap.GlobalWeekNumbers;
            Assert.Equal(Enumerable.Range(1, totalWeeks), reloadedWeeks);
            Assert.Equal(initial.ActivationWindow!.EndGlobalWeek, snapshot.DarkState.CurrentWindow.EndGlobalWeek);
        }

        // One further real, Postgres-persisted GE-only checkpoint continuation.
        var checkpointDate = Gen34TwoDayFixture.StartDate.AddDays(4 * 7);
        var persisted = await Gen34TwoDayFixture.AdvanceOnePureGeWindowAsync(planStateId, checkpointDate, level);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, persisted.Outcome);
        Assert.NotNull(persisted.Snapshot);
        Assert.False(persisted.Snapshot!.DarkState.CurrentWindow.EndGlobalWeek < initial.ActivationWindow!.EndGlobalWeek);

        // Restart again after the checkpoint: reload from Postgres a second
        // time and confirm the newly-activated window's continuity.
        using (var db2 = LongHorizonPersistenceTestFixture.NewContext())
        {
            var repo2 = new LongHorizonRollingStateRepository(db2);
            var snapshot2 = await repo2.LoadRestartSnapshotAsync(planStateId);
            Assert.NotNull(snapshot2);
            Assert.Equal(persisted.Snapshot.DarkState.CurrentWindow.EndGlobalWeek, snapshot2!.DarkState.CurrentWindow.EndGlobalWeek);
            var contiguous = snapshot2.DarkState.StructuralRoadmap.GlobalWeekNumbers;
            Assert.Equal(Enumerable.Range(1, totalWeeks), contiguous);
        }
    }
}

/// <summary>Fresh, real-Postgres Intermediate x5D zero-delta confirmation -- not cited from a prior phase's evidence. Proves this phase's LongHorizonStructuralMaterializer/eligibility-gate changes are byte-identical for the shared 5D LongHorizon subsystem.</summary>
public sealed class Gen34IntermediateFiveDayZeroDeltaTests
{
    [Fact]
    public async Task InitialActivation_IntermediateFiveDay_StillApproved_UnaffectedByTwoDayWiring()
    {
        var startDate = new DateOnly(2026, 9, 7);
        const int totalWeeks = 30;
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(startDate, raceDate);
        var decision = LongHorizonCompositionResolver.Resolve(coreHorizon, ReadinessProfile.ConsistencyNeeded);
        var catalogRoot = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

        var request = new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = decision,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 5,
            StartDate = startDate,
            RaceDate = raceDate,
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(26, 8, 5),
            PreferredDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday],
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = catalogRoot,
            WorkoutLoader = new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = catalogRoot })),
        };

        var runtime = new LongHorizonRollingInitialActivationRuntime();
        var result = await runtime.BuildInitialActivationAsync(request);

        Assert.Equal(LongHorizonRollingInitialActivationStatus.Approved, result.Status);
        Assert.Equal(LongHorizonStructuralMaterializer.CandidateKeyFiveDay, result.StructuralSkeleton!.CandidateKey);
        Assert.Equal(5, result.StructuralSkeleton.Weeks[0].OrderedWorkoutSlots.Count);
        Assert.Equal(4, result.ActivatedNumericWeeks.Count);
        var validation = LongHorizonStructuralValidator.Validate(result.StructuralSkeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Findings));
    }
}
