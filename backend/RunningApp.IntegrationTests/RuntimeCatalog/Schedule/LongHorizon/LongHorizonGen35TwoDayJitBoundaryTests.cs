using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 10K-GEN.35 -- executes GEN.34 §10's disclosed remaining contract now
/// that the 2D plan-catalog bundle release (1.4.0, additive over 1.3.0) is
/// published: real-PostgreSQL proof that a 2D LongHorizon plan reaches
/// canonical Core organically through the real production chain
/// (<see cref="LongHorizonRollingCheckpointRuntime"/> ->
/// <see cref="LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync"/>),
/// mirroring <see cref="RollingActivation.Persistence.Freq6D19FiveDayGeRunwayCoreBoundaryFixture"/>'s
/// exact shape for 5D, generalized to DaysPerWeek=2 and both Levels.
///
/// Horizons are chosen so a real 2D GE-parity difference is exercised end to
/// end: totalWeeks=21 (geWeeks=1, odd) and totalWeeks=22 (geWeeks=2, even) --
/// the identical two skeleton-level horizons GEN.34 §1 Gap-4 verification
/// already used for the structural layer alone; this phase reaches the same
/// horizons through the real persisted/JIT path for the first time. For both
/// horizons, the fixed-length composition (GE=totalWeeks-20, Runway=8,
/// Core=12; GEN.26 §3.1/GEN.30) means: GE is fully consumed by the initial
/// activation window (min(4, geWeeks) == geWeeks for geWeeks&lt;=2), so the very
/// first checkpoint call already observes
/// <see cref="LongHorizonRollingCheckpointRuntimeOutcome.GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached"/>
/// and goes straight to JIT composition -- exactly like 5D's own totalWeeks=21
/// case (GE=1). Three sequential JIT-composition calls are required to reach
/// the first Core week in both cases (Runway's fixed 8 weeks / the 4-week
/// window chunk size = 2 Runway windows, then one more call crosses into
/// Core) -- the second of the three calls is the real GE-&gt;Runway boundary
/// crossing, the third is the real Runway-&gt;Core boundary crossing.
///
/// Deliberately does NOT reuse <see cref="Gen34TwoDayFixture"/>'s own
/// OnboardingBaseline(16, 6, 2) for initial activation: that baseline was
/// proven correct for GEN.34's own GE-segment-only scope (never compared
/// against a Core Week-1 numeric target), but this phase's own real
/// end-to-end trace (see GEN.35 report §2) found it is already ABOVE the
/// low early Core Week-1 long-run target 2D's much lower PeakVolumeBand
/// ([16,22]km Beginner / [20,30]km Intermediate, GEN.11) produces --
/// tripping Phase 4K.8A's pre-existing, correctly-restrictive
/// AboveTarget-unsupported direction guard
/// (<c>PreparationRunwayDirectionGuard</c>) for a reason that has nothing to
/// do with 2D-specific code: 5D's own working FREQ.6D.19 fixture uses a much
/// higher baseline (26, 8, 5) that stays safely below 5D's own, much larger
/// PeakVolumeBand target. A realistic, low "just starting out" baseline
/// (8km weekly / 3km long run) is used here instead, chosen to stay below
/// 2D's lowest real Core Week-1 target at every governed horizon -- not a
/// new numeric authority, just a test-scenario input choice appropriate to
/// 2D's own already-approved (GEN.11) volume range.
/// </summary>
internal static class Gen35TwoDayJitBoundaryFixture
{
    internal static LongHorizonRollingInitialActivationRequest BuildActivationRequest(
        int totalWeeks, RunningBackground level, ReadinessProfile profile)
    {
        var raceDate = Gen34TwoDayFixture.StartDate.AddDays(totalWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(Gen34TwoDayFixture.StartDate, raceDate);
        var decision = LongHorizonCompositionResolver.Resolve(coreHorizon, profile);
        var catalogRoot = Gen34TwoDayFixture.CatalogRoot();

        return new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = decision,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = level,
            DaysPerWeek = 2,
            StartDate = Gen34TwoDayFixture.StartDate,
            RaceDate = raceDate,
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(8, 3, 2),
            PreferredDays = Gen34TwoDayFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = catalogRoot,
            WorkoutLoader = new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = catalogRoot })),
        };
    }

    internal static async Task<(Guid PlanStateId, LongHorizonRollingInitialActivationResult Result)> InitializePlanAsync(
        int totalWeeks, RunningBackground level, ReadinessProfile profile)
    {
        var candidate = await Gen34TwoDayFixture.LoadCandidateAsync(level);
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
            PlanStartDate = Gen34TwoDayFixture.StartDate,
            PreferredDays = Gen34TwoDayFixture.PreferredDays,
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

    private static LongHorizonPriorValidatedAnchor PriorAnchor() => Gen34CheckpointAccessor.Prior(8, 3);

    /// <summary>
    /// One real checkpoint evaluation + (since the GE segment is always
    /// already exhausted for these deliberately-short horizons, or the
    /// window is already inside Runway/Core) one real JIT composition call,
    /// persisted via the real production adapters -- the 2D analogue of
    /// <see cref="Freq6D19FiveDayGeRunwayCoreBoundaryFixture.AdvanceOneWindowAsync"/>.
    /// </summary>
    internal static async Task<LongHorizonRollingPersistenceResult> AdvanceOneWindowAsync(
        Guid planStateId, DateOnly checkpointDate, RunningBackground level, string catalogRoot, int totalWeeks)
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
            CurrentAvailability = Gen34TwoDayFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            SafetyState = LongHorizonSafetyState.Clear,
            ReadinessProfile = state.StructuralRoadmap.Profile,
            PriorValidatedAnchor = PriorAnchor(),
            PreviousContextVersion = state.ContextVersion,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = level,
            DaysPerWeek = 2,
        };
        var checkpoint = await checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(checkpointRequest);

        var geEnd = state.StructuralRoadmap.GeneralEnduranceWeeks;
        var reachesGeBoundary = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated
            && checkpoint.ActivationWindow!.EndGlobalWeek == geEnd;
        var pureGe = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated && !reachesGeBoundary;

        if (pureGe)
        {
            return await new LongHorizonRollingActivationPersistenceAdapter(repo)
                .PersistGeCheckpointAsync(planStateId, snapshot.ConcurrencyVersion, checkpoint);
        }

        if (checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowBlocked)
        {
            return await new LongHorizonRollingBlockPersistenceAdapter(repo).PersistBlockAsync(
                planStateId, snapshot.ConcurrencyVersion,
                state.CurrentWindow.EndGlobalWeek + 1, Math.Min(state.CurrentWindow.EndGlobalWeek + 4, state.StructuralRoadmap.TotalWeeks),
                checkpoint.AuthoritativeReason ?? LongHorizonReasonCode.FromCheckpoint(LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved),
                "MoreTrainingDataNeeded", "gen35-boundary", checkpointDate, true);
        }

        // Reaches (or is already past) the GE boundary, or is a pure Runway/Core continuation
        // (the checkpoint runtime's own GE-only scope never applies once GE is behind us) --
        // real JIT composition, same chain as 5D/4D's own FREQ.6D.19/GEN.9 fixtures.
        var continuation = new LongHorizonRollingRestartContinuationService(repo);
        var jitResult = await continuation.ContinueJitCompositionAsync(
            planStateId, checkpoint.EvidenceSnapshot!, checkpoint.ValidatedLoad ?? PriorAnchor().Load,
            checkpoint.EvidenceSnapshot!.CompletedRunsCount == 0 ? null : 2,
            checkpoint.CheckpointDecision,
            checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated ? checkpoint.NewlyActivatedWeeks : null,
            Gen34TwoDayFixture.StartDate, Gen34TwoDayFixture.StartDate.AddDays(totalWeeks * 7),
            Gen34TwoDayFixture.PreferredDays, DayOfWeek.Sunday, catalogRoot,
            lifecycleStatesOverride: state.LifecycleStates,
            // Real, newly-published release (GEN.35: plan-catalog/artifacts/appsel-plan-catalog/1.4.0/bundles/
            // TEN_K__2D__BEGINNER.v1.json / TEN_K__2D__INTERMEDIATE.v1.json) -- this is the exact bundle
            // GEN.34 §4.2 disclosed as the sole remaining blocker for reaching this call at all.
            publishedBundleReleaseVersion: "1.4.0",
            // Reuses GEN.29's own established 2D dark-orchestration convention value (Gen29TwoDayRunwayDarkOrchestrationTests.cs,
            // targetSeconds = 3600) -- not a new number or new product classification.
            targetFinishTimeSeconds: 3600,
            targetFinishTimeSource: TargetFinishTimeSource.ProductAverage);

        if (jitResult.Outcome == LongHorizonRollingPersistenceOutcome.ConcurrencyConflict
            || jitResult.Outcome == LongHorizonRollingPersistenceOutcome.IdempotentReplay)
        {
            return jitResult;
        }

        if (jitResult.IsBlock || jitResult.Outcome != LongHorizonRollingPersistenceOutcome.Success || jitResult.Snapshot!.DarkState.CurrentWindow.Status != LongHorizonActivationWindowStatus.Activated)
        {
            using var diagnosticDb = LongHorizonPersistenceTestFixture.NewContext();
            var plan = await diagnosticDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
            throw new InvalidOperationException(
                $"JIT composition did not activate as expected -- persistence outcome {jitResult.Outcome}, plan blocked reason {plan.CurrentBlockedInternalReasonCode}.");
        }

        return jitResult;
    }
}

public sealed class Gen35TwoDayJitBoundaryTests
{
    public static IEnumerable<object[]> LevelAndParity()
    {
        foreach (var level in new[] { RunningBackground.Beginner, RunningBackground.Intermediate })
        foreach (var totalWeeks in new[] { 21, 22 }) // geWeeks 1 (odd) / 2 (even)
            yield return [level, totalWeeks];
    }

    private static async Task<(Guid PlanStateId, RollingNumericActivationWindow CoreWindow, int GeWeeks, int RunwayEnd, int FirstCoreWeek)> DriveToFirstCoreWindowAsync(
        RunningBackground level, int totalWeeks)
    {
        var (planStateId, initial) = await Gen35TwoDayJitBoundaryFixture.InitializePlanAsync(totalWeeks, level, ReadinessProfile.CoreEntryReady);
        var geWeeks = initial.StructuralSkeleton!.GeneralEnduranceWeeks;
        Assert.Equal(totalWeeks - 20, geWeeks);
        Assert.True(geWeeks is 1 or 2, "Test horizons are deliberately the two GE-parity minimums (1=odd, 2=even).");
        // GE is fully consumed by the initial activation window at this length -- confirmed, not assumed.
        Assert.Equal(geWeeks, initial.ActivationWindow!.EndGlobalWeek);

        var runwayEnd = geWeeks + 8;
        var firstCoreWeek = runwayEnd + 1;
        var catalogRoot = Gen34TwoDayFixture.CatalogRoot();
        var date = Gen34TwoDayFixture.StartDate.AddDays((geWeeks + 1) * 7);

        // Call 1: GE -> Runway boundary crossing (real JIT composition; the GE segment is
        // already exhausted, so this is the very first checkpoint call for this plan).
        var call1 = await Gen35TwoDayJitBoundaryFixture.AdvanceOneWindowAsync(planStateId, date, level, catalogRoot, totalWeeks);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call1.Outcome);
        var window = call1.Snapshot!.DarkState.CurrentWindow;
        Assert.Equal(geWeeks + 1, window.StartGlobalWeek);
        Assert.True(window.EndGlobalWeek < runwayEnd, "First Runway window should not yet reach the Runway->Core boundary.");
        date = date.AddDays(28);

        // Call 2: pure Runway continuation, reaching exactly the Runway segment's own end.
        var call2 = await Gen35TwoDayJitBoundaryFixture.AdvanceOneWindowAsync(planStateId, date, level, catalogRoot, totalWeeks);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call2.Outcome);
        window = call2.Snapshot!.DarkState.CurrentWindow;
        Assert.Equal(runwayEnd, window.EndGlobalWeek);
        date = date.AddDays(28);

        // Call 3: the real Runway -> Core boundary crossing.
        var call3 = await Gen35TwoDayJitBoundaryFixture.AdvanceOneWindowAsync(planStateId, date, level, catalogRoot, totalWeeks);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, call3.Outcome);
        var coreWindow = call3.Snapshot!.DarkState.CurrentWindow;
        Assert.Equal(firstCoreWeek, coreWindow.StartGlobalWeek);
        Assert.Equal(LongHorizonStructuralSegmentType.Core, coreWindow.SegmentsCovered.Single());

        return (planStateId, coreWindow, geWeeks, runwayEnd, firstCoreWeek);
    }

    [Theory]
    [MemberData(nameof(LevelAndParity))]
    public async Task GeToRunwayToCore_RealJitBoundary_SucceedsAgainstRealPostgres_ThroughPublished140Bundle(
        RunningBackground level, int totalWeeks)
    {
        var (planStateId, coreWindow, geWeeks, runwayEnd, firstCoreWeek) = await DriveToFirstCoreWindowAsync(level, totalWeeks);

        Assert.True(coreWindow.EndGlobalWeek <= totalWeeks);

        // GlobalWeekNumber continuity across the full GE->Runway->Core chain, proven from a
        // fresh, independent restart reload -- never the in-memory graph the driving calls used.
        using var verify = LongHorizonPersistenceTestFixture.NewContext();
        var repo = new LongHorizonRollingStateRepository(verify);
        var snapshot = await repo.LoadRestartSnapshotAsync(planStateId);
        Assert.NotNull(snapshot);
        var fullRange = snapshot!.DarkState.StructuralRoadmap.GlobalWeekNumbers;
        Assert.Equal(Enumerable.Range(1, totalWeeks), fullRange);
        Assert.Equal(coreWindow.EndGlobalWeek, snapshot.DarkState.CurrentWindow.EndGlobalWeek);

        // The first real Core week has a valid, well-formed 2D structural shape: exactly 2
        // slots, a LONG_RUN, and exactly one of KEY_SESSION/EASY_SUPPORT (never both/neither).
        var firstCoreSessions = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == firstCoreWeek)
            .ToListAsync();
        Assert.Equal(2, firstCoreSessions.Count);
        Assert.Contains(firstCoreSessions, s => s.SessionRole == "LONG_RUN");
        var coreHasKey = firstCoreSessions.Any(s => s.SessionRole == "KEY_SESSION");
        var coreHasEasy = firstCoreSessions.Any(s => s.SessionRole.StartsWith("EASY_SUPPORT", StringComparison.Ordinal));
        Assert.True(coreHasKey != coreHasEasy, $"First Core week must carry exactly one of KEY_SESSION/EASY_SUPPORT (roles: {string.Join("+", firstCoreSessions.Select(s => s.SessionRole))}).");
        Assert.All(firstCoreSessions, s => Assert.NotNull(s.SlotOrdinal));
        Assert.Equal(2, firstCoreSessions.Select(s => s.SlotOrdinal).Distinct().Count());

        // Every activated GE + Runway week (weeks 1..runwayEnd) alternates strictly by
        // GlobalWeekNumber parity, with no double-up or skip at the GE->Runway boundary --
        // the exact continuity gap this phase's own real end-to-end verification found and
        // fixed (GEN.35 report §2: TenKPreparationRunwayDarkOrchestrator's own Runway-
        // generation call site never threaded GEN.33's StartGlobalWeek, so an odd-length GE
        // segment (geWeeks=1) silently double-KEY'd weeks 1 and 2 before this phase's fix).
        var geAndRunwaySessions = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek <= runwayEnd)
            .OrderBy(s => s.Week.GlobalWeek)
            .Select(s => new { s.Week.GlobalWeek, s.SessionRole })
            .GroupBy(x => x.GlobalWeek)
            .ToListAsync();
        Assert.Equal(Enumerable.Range(1, runwayEnd), geAndRunwaySessions.Select(g => g.Key));
        foreach (var week in geAndRunwaySessions)
        {
            var roles = week.Select(s => s.SessionRole).ToList();
            var hasKey = roles.Any(r => r == "KEY_SESSION");
            var hasEasy = roles.Any(r => r.StartsWith("EASY_SUPPORT", StringComparison.Ordinal));
            var expectedKey = week.Key % 2 == 1; // odd GlobalWeekNumber => Pattern-A, strict throughout GE+Runway
            Assert.True(hasKey != hasEasy, $"Week {week.Key} must carry exactly one of KEY_SESSION/EASY_SUPPORT, never both/neither (roles: {string.Join("+", roles)}).");
            Assert.Equal(expectedKey, hasKey);
        }

        // DISCLOSED, NOT FIXED THIS PHASE (GEN.35, re-confirmed unresolved by GEN.36):
        // Core's own real content generator (DynamicCoreSessionPrescriptionOrchestrator, the
        // same pipeline CatalogPreviewGenerator uses for already-PUBLICLY_ACTIVE standalone
        // 2D/3D/4D/5D/6D Core generation) still always anchors its own internal Pattern-A/B
        // alternation to its own local week 1 = Pattern-A in this real production call site --
        // so a LongHorizon Core segment does not (yet) continue GE+Runway's own established
        // parity when GeneralEnduranceWeeks+8 (Runway's fixed length) is odd. GEN.36 built the
        // additive plumbing (CoreStartGlobalWeek, threaded end to end down to
        // CatalogStageToWeekMaterializationContext.StartGlobalWeek, GEN.34's own mechanism) but
        // deliberately did NOT enable it here: doing so causes Core's own local week 1 to land
        // on Pattern B (EASY_SUPPORT+LONG_RUN, zero KEY_SESSION) for exactly this odd-GE-parity
        // case, which trips PreparationRunwayCoreWeekOnePaceAdapter's pre-existing hard
        // requirement that Core's own Foundation Week 1 carry a KEY_SESSION to derive an
        // authoritative pace target -- a genuine, previously-latent pace-continuity numeric-
        // authority question (not a wiring gap), disclosed and classified
        // DOMAIN_DECISION_REQUIRED by GEN.36 rather than resolved unprompted. See
        // TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests for the isolated,
        // dark, permanent reproduction of this exact conflict.
        var coreSessions = await verify.LongHorizonRollingSessionStates
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek > runwayEnd)
            .OrderBy(s => s.Week.GlobalWeek)
            .Select(s => new { s.Week.GlobalWeek, s.SessionRole })
            .GroupBy(x => x.GlobalWeek)
            .ToListAsync();
        Assert.NotEmpty(coreSessions);
        foreach (var week in coreSessions)
        {
            var roles = week.Select(s => s.SessionRole).ToList();
            var hasKey = roles.Any(r => r == "KEY_SESSION");
            var hasEasy = roles.Any(r => r.StartsWith("EASY_SUPPORT", StringComparison.Ordinal));
            Assert.True(hasKey != hasEasy, $"Core week {week.Key} must carry exactly one of KEY_SESSION/EASY_SUPPORT, never both/neither (roles: {string.Join("+", roles)}).");
            // Core's own local alternation period (2 weeks), anchored at its own week 1 =
            // Pattern-A -- the disclosed, still-not-globally-continuous behavior documented above.
            var coreLocalWeekNumber = week.Key - runwayEnd;
            var expectedKeyLocally = coreLocalWeekNumber % 2 == 1;
            Assert.Equal(expectedKeyLocally, hasKey);
        }
    }
}
