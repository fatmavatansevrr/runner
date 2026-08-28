using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 10K-FREQ.6D.26 -- combined Intermediate x6D Core/Preparation
/// Runway/LongHorizon implementation and dark verification. Reuses the exact
/// same dark-only patterns FREQ.6D.14/15/19 established for 5D (internal
/// runtime classes called directly, never public HTTP -- the public gate
/// remains closed for 6D throughout this phase). No fabricated Core rows:
/// every session observed here is produced by the real production
/// LongHorizonRollingCheckpointRuntime / LongHorizonRollingRestartContinuationService
/// chain.
/// </summary>
internal static class Freq6D26SixDayFixture
{
    internal const string SixDayCandidateKey = "TEN_K__6D__INTERMEDIATE";
    internal const int SixDayCandidateVersion = 1;

    internal static readonly DateOnly StartDate = new(2026, 9, 7);
    internal static readonly IReadOnlyList<DayOfWeek> PreferredDays =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday];

    internal static string CatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    internal static async Task<PlanCatalogCandidateSummary> LoadSixDayCandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(SixDayCandidateKey, SixDayCandidateVersion);
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
            DaysPerWeek = 6,
            StartDate = StartDate,
            RaceDate = raceDate,
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(26, 8, 6),
            PreferredDays = PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = CatalogRoot(),
            WorkoutLoader = new Application.RuntimeCatalog.Schedule.Binding.CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() })),
        };
    }

    internal static async Task<(Guid PlanStateId, RollingNumericActivationWindow InitialWindow, PlanCatalogCandidateSummary Candidate, string CatalogRoot)>
        InitializePlanAsync(int totalWeeks)
    {
        var candidate = await LoadSixDayCandidateAsync();
        var request = BuildActivationRequest(totalWeeks);
        var runtime = new LongHorizonRollingInitialActivationRuntime();
        var result = await runtime.BuildInitialActivationAsync(request);
        if (result.Status != LongHorizonRollingInitialActivationStatus.Approved)
            throw new InvalidOperationException($"Initial 6D activation not approved: {result.Failure?.Reason} {result.Failure?.Code} {result.Failure?.Message}");

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
            DaysPerWeek = 6,
        };

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        await new LongHorizonRollingStateRepository(db).InitializeStructuralStateAsync(initRequest);
        return (planStateId, result.ActivationWindow!, candidate, request.CatalogRoot);
    }

    internal static async Task<LongHorizonRollingPersistenceResult> AdvanceOneWindowAsync(
        Guid planStateId, DateOnly checkpointDate, string catalogRoot)
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
            PriorValidatedAnchor = LongHorizonRollingCheckpointRuntimeTestsAccessor.Prior(26, 8),
            PreviousContextVersion = state.ContextVersion,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 6,
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
                "MoreTrainingDataNeeded", "freq6d26-boundary", checkpointDate, true);
        }

        var continuation = new LongHorizonRollingRestartContinuationService(repo);
        var jitResult = await continuation.ContinueJitCompositionAsync(
            planStateId, checkpoint.EvidenceSnapshot!, checkpoint.ValidatedLoad ?? LongHorizonRollingCheckpointRuntimeTestsAccessor.Prior(26, 8).Load,
            checkpoint.EvidenceSnapshot!.CompletedRunsCount == 0 ? null : 6,
            checkpoint.CheckpointDecision,
            checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated ? checkpoint.NewlyActivatedWeeks : null,
            StartDate, StartDate.AddDays(state.StructuralRoadmap.TotalWeeks * 7),
            PreferredDays, DayOfWeek.Sunday, catalogRoot,
            lifecycleStatesOverride: state.LifecycleStates,
            // Real, already-published release (backend/RunningApp.Api/appsettings.json
            // PlanCatalog:PublishedBundleReleaseVersion) -- this phase published
            // plan-catalog/artifacts/appsel-plan-catalog/1.2.0/bundles/TEN_K__6D__INTERMEDIATE.v1.json,
            // a byte-identical superset of 1.1.0 additionally carrying the 6D candidate.
            publishedBundleReleaseVersion: "1.2.0",
            targetFinishTimeSeconds: 3480,
            targetFinishTimeSource: TargetFinishTimeSource.ProductAverage);

        if (jitResult.Outcome == LongHorizonRollingPersistenceOutcome.ConcurrencyConflict
            || jitResult.Outcome == LongHorizonRollingPersistenceOutcome.IdempotentReplay)
        {
            return jitResult;
        }

        if (jitResult.Outcome != LongHorizonRollingPersistenceOutcome.Success || jitResult.Snapshot!.DarkState.CurrentWindow.Status != LongHorizonActivationWindowStatus.Activated)
        {
            using var diagnosticDb = LongHorizonPersistenceTestFixture.NewContext();
            var plan = await diagnosticDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
            throw new InvalidOperationException(
                $"JIT composition did not activate as expected -- persistence outcome {jitResult.Outcome}, plan blocked reason {plan.CurrentBlockedInternalReasonCode}.");
        }

        return jitResult;
    }
}

/// <summary>Exposes the private <c>Prior</c> helper another test file already defines, avoiding a duplicate.</summary>
internal static class LongHorizonRollingCheckpointRuntimeTestsAccessor
{
    internal static LongHorizonPriorValidatedAnchor Prior(double weekly = 20, double longRun = 7) => new(
        new ValidatedSustainableLoad
        {
            WeeklyVolumeKm = weekly,
            LongRunKm = longRun,
            EvidenceWindowStartWeek = 1,
            EvidenceWindowEndWeek = 1,
            CompletedEvidenceWeekNumbers = [1],
            ExcludedRecoveryWeekNumbers = [],
            WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
            LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
            RoundingPolicy = "VolumeSafetyPolicy.0.5km",
            LongRunCapPolicy = "VolumeSafetyPolicy.LongRunHardCapShare=0.36",
            ValidationStatus = LongHorizonValidationStatus.Valid,
            Provenance = "Test anchor",
            ContextVersion = null,
        },
        IsFreshForCurrentInvocation: true,
        SourceContextSequence: 0);
}

// ── §1: structural materialization (RunLayout, PeakVolumeBand, ResolvedPeakReference) ──

public sealed class Freq6D26StructuralMaterializationTests
{
    [Theory]
    [InlineData(21)]
    [InlineData(32)]
    [InlineData(52)]
    public async Task SixDayStructuralRoadmap_ResolvesExactSixDayIdentityAndShape(int totalWeeks)
    {
        var request = Freq6D26SixDayFixture.BuildActivationRequest(totalWeeks);
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            request.CompositionDecision, request.CatalogRoot, request.WorkoutLoader, default, request.DaysPerWeek);

        Assert.Equal(LongHorizonStructuralMaterializer.CandidateKeySixDay, skeleton.CandidateKey);
        Assert.Equal(LongHorizonStructuralMaterializer.CandidateVersionSixDay, skeleton.CandidateVersion);
        Assert.Equal(totalWeeks, skeleton.TotalWeeks);
        Assert.Equal(8, skeleton.PreparationRunwayWeeks);
        Assert.Equal(12, skeleton.CoreWeeks);

        var geWeek = skeleton.Weeks[0];
        Assert.Equal(6, geWeek.OrderedWorkoutSlots.Count);
        Assert.Equal(1, geWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
        Assert.Equal(4, geWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, geWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "LONG_RUN"));

        var firstCoreWeek = skeleton.Weeks.First(w => w.Segment == LongHorizonSegmentType.Core);
        Assert.Equal(6, firstCoreWeek.OrderedWorkoutSlots.Count);
        Assert.Equal(2, firstCoreWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
        Assert.Equal(3, firstCoreWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, firstCoreWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "LONG_RUN"));
    }

    [Fact]
    public async Task PeakVolumeBand_ResolvesExact36To50_ForIntermediateSixDay()
    {
        var loader = new CatalogPeakVolumeBandLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = Freq6D26SixDayFixture.CatalogRoot() }));
        var band = await loader.LoadAsync(new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 5), "TEN_K", "INTERMEDIATE", 6);
        Assert.Equal(36, band.MinimumKm);
        Assert.Equal(50, band.MaximumKm);
    }

    [Fact]
    public void ResolvedPeakReference_Is44Point5_ForSixDayIntermediate()
    {
        Assert.Equal(44.5, VolumeSafetyPolicy.SixDayIntermediate.ResolvedPeakReference.Value);
        Assert.Equal(26.0, VolumeSafetyPolicy.SixDayIntermediate.GoldenFixtureStartingVolumeKm);
    }

    [Fact]
    public void ForIntermediateDaysPerWeek_ResolvesExpectedPolicyPerFrequency()
    {
        Assert.Same(VolumeSafetyPolicy.Default, VolumeSafetyPolicy.ForIntermediateDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.FiveDayIntermediate, VolumeSafetyPolicy.ForIntermediateDaysPerWeek(5));
        Assert.Same(VolumeSafetyPolicy.SixDayIntermediate, VolumeSafetyPolicy.ForIntermediateDaysPerWeek(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => VolumeSafetyPolicy.ForIntermediateDaysPerWeek(7));
    }
}

// ── §22-26: 6-session Adaptation state table (FREQ.6D.23's frozen model) ──

public sealed class Freq6D26SixSessionAdaptationTests
{
    private static WindowExecutionSummary Summary(int keyExpected, int keyCompleted, bool longExpected, bool longCompleted, int easyExpected, int easyCompleted) => new(
        ExpectedSessionCount: 6,
        EffectiveCompletedCount: keyCompleted + (longCompleted ? 1 : 0) + easyCompleted,
        KeySessionExpectedCount: keyExpected,
        KeySessionCompletedCount: keyCompleted,
        LongRunExpected: longExpected,
        LongRunCompleted: longCompleted,
        EasyExpectedCount: easyExpected,
        EasyCompletedCount: easyCompleted,
        UnrecoveredNotTodayCount: 0,
        SupersededByAdaptationCount: 0,
        HasSafetyFlag: false);

    [Fact]
    public void SixOfSix_FullAdherence_ResolvesProgress()
    {
        var result = NextWindowLoadDecisionPolicyAccessor.Evaluate(Summary(2, 2, true, true, 3, 3));
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
    }

    [Fact]
    public void FiveOfSix_OnlyEasyMissing_ResolvesProgress()
    {
        var result = NextWindowLoadDecisionPolicyAccessor.Evaluate(Summary(2, 2, true, true, 3, 2));
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, result.LoadDecision);
    }

    [Fact]
    public void FiveOfSix_KeyLane0OrLane1Missing_ResolvesMaintain()
    {
        var result = NextWindowLoadDecisionPolicyAccessor.Evaluate(Summary(2, 1, true, true, 3, 3));
        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Fact]
    public void FiveOfSix_LongMissing_ResolvesMaintain()
    {
        var result = NextWindowLoadDecisionPolicyAccessor.Evaluate(Summary(2, 2, true, false, 3, 3));
        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Theory]
    [InlineData(2, false, 2)] // 4/6: both KEY, no long, two easy
    [InlineData(1, true, 2)]  // 4/6: one KEY, long done, two easy -- role-blind at count=4
    public void FourOfSix_RoleBlind_ResolvesMaintain(int keyCompleted, bool longCompleted, int easyCompleted)
    {
        // Expected counts are always the fixed 6D structural shape (2 KEY, 1 LONG, 3 EASY) -- only completed counts vary.
        var summary = Summary(2, keyCompleted, true, longCompleted, 3, easyCompleted);
        Assert.Equal(4, summary.EffectiveCompletedCount);
        var result = NextWindowLoadDecisionPolicyAccessor.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Theory]
    [InlineData(2, false, 1)] // 3/6
    [InlineData(2, false, 0)] // 2/6
    public void TwoOrThreeOfSix_ResolvesMaintain(int keyCompleted, bool longCompleted, int easyCompleted)
    {
        var summary = Summary(2, keyCompleted, true, longCompleted, 3, easyCompleted);
        var result = NextWindowLoadDecisionPolicyAccessor.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.Maintain, result.LoadDecision);
    }

    [Theory]
    [InlineData(1, false, 0)] // 1/6
    [InlineData(0, false, 0)] // 0/6
    public void ZeroOrOneOfSix_ResolvesReduce(int keyCompleted, bool longCompleted, int easyCompleted)
    {
        var summary = Summary(2, keyCompleted, true, longCompleted, 3, easyCompleted);
        var result = NextWindowLoadDecisionPolicyAccessor.Evaluate(summary);
        Assert.Equal(NextWindowLoadDecision.Reduce, result.LoadDecision);
    }

    [Fact]
    public void Monotonicity_WorseAdherenceNeverOutranksBetterAdherence()
    {
        // Completed count descends 6 -> 0, always role-blind except at the
        // boundary (handled by the two dedicated role-gate tests above).
        // Distributes completed count across EASY first (up to 3), then LONG, then KEY.
        NextWindowLoadDecision DecisionForCompleted(int completed)
        {
            var easy = Math.Min(3, completed);
            var remaining = completed - easy;
            var longDone = remaining > 0;
            remaining -= longDone ? 1 : 0;
            var key = Math.Max(0, remaining);
            return NextWindowLoadDecisionPolicyAccessor.Evaluate(Summary(2, key, true, longDone, 3, easy)).LoadDecision;
        }

        var decisions = Enumerable.Range(0, 7).Reverse().Select(DecisionForCompleted).ToList();
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, decisions[0]); // 6/6
        Assert.Equal(NextWindowLoadDecision.Reduce, decisions[^1]); // 0/6
        Assert.Equal(NextWindowLoadDecision.Reduce, decisions[^2]); // 1/6
        Assert.All(decisions.Skip(1).Take(4), d => Assert.NotEqual(NextWindowLoadDecision.Reduce, d)); // 2..5 never Reduce
    }
}

/// <summary>
/// Phase 10K-FREQ.6D.26 -- <c>NextWindowLoadDecisionPolicy.Evaluate</c> is
/// internal; this thin accessor exists only because the class itself has no
/// public surface and adding one would be a scope-creeping API change this
/// phase does not need -- InternalsVisibleTo already grants the test project
/// access, so this simply forwards.
/// </summary>
internal static class NextWindowLoadDecisionPolicyAccessor
{
    internal static NextWindowAdaptationResult Evaluate(WindowExecutionSummary summary) => NextWindowLoadDecisionPolicy.Evaluate(summary);
}

// ── §31: full GE->Runway->Core organic dual-KEY lifecycle, real PostgreSQL ──

public sealed class Freq6D26FullLifecycleTests
{
    [Fact]
    public async Task SixDayLongHorizon_ReachesOrganicCoreWithDualKey_AfterRealPostgresRestart()
    {
        var (planStateId, initialWindow, _, catalogRoot) = await Freq6D26SixDayFixture.InitializePlanAsync(21);
        Assert.Equal(1, initialWindow.StartGlobalWeek);
        Assert.Equal(1, initialWindow.EndGlobalWeek);

        // Window1 (GE, week1) -> Window2 (Runway weeks 2-9 entered eagerly per the
        // established FREQ.6D.19/21 finding that Core generation is attempted on
        // the first Runway-entry continuation call).
        var checkpointDate = Freq6D26SixDayFixture.StartDate.AddDays(14);
        var w2 = await Freq6D26SixDayFixture.AdvanceOneWindowAsync(planStateId, checkpointDate, catalogRoot);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, w2.Outcome);

        using (var verify = LongHorizonPersistenceTestFixture.NewContext())
        {
            var plan = await verify.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
            Assert.Equal(6, plan.DaysPerWeek);
        }

        // Continue through Runway (weeks 2-9) until Core (week 10) organically materializes.
        LongHorizonRollingPersistenceResult last = w2;
        for (var i = 0; i < 3 && last.Outcome == LongHorizonRollingPersistenceOutcome.Success; i++)
        {
            var nextDate = checkpointDate.AddDays(28 * (i + 1));
            last = await Freq6D26SixDayFixture.AdvanceOneWindowAsync(planStateId, nextDate, catalogRoot);
        }
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, last.Outcome);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var firstCoreWeek = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == 10).ToListAsync();

        Assert.Equal(6, firstCoreWeek.Count);
        Assert.Equal(2, firstCoreWeek.Count(s => s.SessionRole == "KEY_SESSION"));
        Assert.Contains(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
        Assert.Contains(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);
        Assert.Equal(3, firstCoreWeek.Count(s => s.SessionRole == "EASY_SUPPORT"));
        Assert.Equal(3, firstCoreWeek.Where(s => s.SessionRole == "EASY_SUPPORT").Select(s => s.SlotOrdinal).Distinct().Count());
        Assert.Equal(1, firstCoreWeek.Count(s => s.SessionRole == "LONG_RUN"));

        var plan2 = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        Assert.Equal(6, plan2.DaysPerWeek);
    }
}

// ── §54-55 (as of FREQ.6D.26): Beginner/Advanced isolation and 7D non-support
// remain closed. Phase 10K-FREQ.6D.27 subsequently opened the public
// Intermediate x6D gate itself (by product/repository design, not a
// regression) -- the two assertions that specifically asserted "6D public
// gate closed" were superseded there and are covered instead by
// Freq6D27IntermediateSixDayPublicActivationTests' own activation proof.
// Phase 10K-GEN.10 subsequently opened the public Advanced x6D gate itself
// too (by product/repository design, not a regression) -- Advanced x6D
// removed from this "still closed" assertion and covered instead by
// Gen9AdvancedIsolationTests.PublicIdentityPolicy_RecognizesAdvancedActivatedFrequencies
// and Gen10AdvancedCombinedPublicActivationTests' own activation proof.
// This class now asserts only what remains permanently true post-activation:
// Beginner x6D and Intermediate x7D never gained public routing. ──

public sealed class Freq6D26IsolationTests
{
    [Fact]
    public void PublicIdentityPolicy_DoesNotRecognizeBeginnerSixDay()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 6));
    }

    [Fact]
    public void PublicIdentityPolicy_DoesNotRecognizeIntermediateSevenDay()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 7));
    }

    [Fact]
    public void PreparationRunwayIdentityPolicy_DoesNotRecognizeSevenDay()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 7));
    }
}
