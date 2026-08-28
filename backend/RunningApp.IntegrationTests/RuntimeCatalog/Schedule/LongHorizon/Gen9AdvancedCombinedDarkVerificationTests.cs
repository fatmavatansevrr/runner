using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 10K-GEN.9 -- combined Advanced 3D/4D/5D/6D Core/Preparation
/// Runway/LongHorizon implementation and dark verification. Reuses the exact
/// same dark-only patterns FREQ.6D.14/15/19/26 established (internal runtime
/// classes called directly, never public HTTP -- the public gate remains
/// closed for every Advanced frequency throughout this phase). No fabricated
/// Core rows: the full-lifecycle test drives the real production
/// LongHorizonRollingCheckpointRuntime / LongHorizonRollingRestartContinuationService
/// chain, identical to Freq6D26SixDayFixture's own proof for Intermediate x6D.
///
/// Disclosed scope: this phase verifies structural materialization and
/// numeric authority for all four Advanced frequencies, and one full
/// real-PostgreSQL GE-&gt;Runway-&gt;Core lifecycle for Advanced x5D (the
/// highest-value proof point, exercising both the LongHorizon rolling
/// pipeline and the new Advanced dual-KEY profile binding simultaneously).
/// It does not additionally repeat the same full real-Postgres lifecycle for
/// 3D/4D/6D -- their structural/numeric authority is verified directly
/// instead, consistent with FREQ.6D.26's own disclosed-coverage precedent.
/// </summary>
internal static class Gen9AdvancedFixture
{
    internal static readonly DateOnly StartDate = new(2026, 9, 7);

    internal static string CatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    internal static IReadOnlyList<DayOfWeek> PreferredDays(int daysPerWeek) => daysPerWeek switch
    {
        3 => [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Sunday],
        4 => [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
        5 => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
        6 => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday],
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek)),
    };

    internal static LongHorizonRollingInitialActivationRequest BuildActivationRequest(int totalWeeks, int daysPerWeek)
    {
        var raceDate = StartDate.AddDays(totalWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(StartDate, raceDate);
        var decision = LongHorizonCompositionResolver.Resolve(coreHorizon, ReadinessProfile.ConsistencyNeeded);

        return new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = decision,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Advanced,
            DaysPerWeek = daysPerWeek,
            StartDate = StartDate,
            RaceDate = raceDate,
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(38, 12, daysPerWeek),
            PreferredDays = PreferredDays(daysPerWeek),
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = CatalogRoot(),
            WorkoutLoader = new Application.RuntimeCatalog.Schedule.Binding.CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() })),
        };
    }
}

// ── Numeric authority: VolumeSafetyPolicy, PeakVolumeBand, ResolvedPeakReference ──

public sealed class Gen9AdvancedNumericAuthorityTests
{
    [Fact]
    public void ForAdvancedDaysPerWeek_ResolvesExpectedPolicyPerFrequency()
    {
        Assert.Same(VolumeSafetyPolicy.Advanced3D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.Advanced4D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.Advanced5D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(5));
        Assert.Same(VolumeSafetyPolicy.Advanced6D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => VolumeSafetyPolicy.ForAdvancedDaysPerWeek(7));
    }

    [Theory]
    [InlineData(3, 40d)]
    [InlineData(4, 45d)]
    [InlineData(5, 50d)]
    [InlineData(6, 51d)]
    public void ResolvedPeakReference_MatchesGen8FrozenValue(int daysPerWeek, double expected)
    {
        Assert.Equal(expected, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(daysPerWeek).ResolvedPeakReference.Value);
        Assert.Equal(ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope,
            VolumeSafetyPolicy.ForAdvancedDaysPerWeek(daysPerWeek).ResolvedPeakReference.Provenance);
    }

    [Theory]
    [InlineData(3, 34, 46)]
    [InlineData(4, 38, 52)]
    [InlineData(5, 42, 58)]
    [InlineData(6, 42, 60)]
    public async Task PeakVolumeBand_ResolvesGen7FrozenBand_ForAdvanced(int runsPerWeek, double min, double max)
    {
        var loader = new CatalogPeakVolumeBandLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = Gen9AdvancedFixture.CatalogRoot() }));
        var band = await loader.LoadAsync(new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 6), "TEN_K", "ADVANCED", runsPerWeek);
        Assert.Equal(min, band.MinimumKm);
        Assert.Equal(max, band.MaximumKm);
    }

    [Fact]
    public void Progression_And_Taper_AreLevelAndFrequencyInvariant_ForAdvanced()
    {
        foreach (var policy in new[] { VolumeSafetyPolicy.Advanced3D, VolumeSafetyPolicy.Advanced4D, VolumeSafetyPolicy.Advanced5D, VolumeSafetyPolicy.Advanced6D })
        {
            Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
            Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
            Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
            Assert.Equal(0.53d, policy.TaperVolumeMultiplier);
        }
    }

    [Fact]
    public void LongRunShares_MatchGen7FrequencyOwnedFigures()
    {
        Assert.Equal(VolumeSafetyPolicy.ThreeDayIntermediate.LongRunPreferredMinimumShare, VolumeSafetyPolicy.Advanced3D.LongRunPreferredMinimumShare);
        Assert.Equal(VolumeSafetyPolicy.Default.LongRunPreferredMinimumShare, VolumeSafetyPolicy.Advanced4D.LongRunPreferredMinimumShare);
        Assert.Equal(VolumeSafetyPolicy.FiveDayIntermediate.LongRunPreferredMinimumShare, VolumeSafetyPolicy.Advanced5D.LongRunPreferredMinimumShare);
        Assert.Equal(VolumeSafetyPolicy.SixDayIntermediate.LongRunPreferredMinimumShare, VolumeSafetyPolicy.Advanced6D.LongRunPreferredMinimumShare);
    }
}

// ── Missing/zero readiness: PRODUCT_INELIGIBLE (GE-level, Level-agnostic executor) ──

public sealed class Gen9AdvancedReadinessTests
{
    private static readonly IReadOnlyList<LongHorizonGeWeekDescriptor> OneWeek =
        LongHorizonGeStructuralSelector.Select(1, ReadinessProfile.ConsistencyNeeded, easySupportCount: 3);

    [Fact]
    public void MissingReadiness_ThrowsProductIneligible_ForAdvanced()
    {
        var baseline = new LongHorizonGeEntryBaselineInput(null, null, 5);
        Assert.Throws<LongHorizonGeMissingReadinessProductIneligibleException>(
            () => LongHorizonGeNumericExecutor.Execute(OneWeek, baseline, VolumeSafetyPolicy.Advanced5D, applyTargetCap: true));
    }

    [Fact]
    public void ZeroReadiness_ThrowsProductIneligible_ForAdvanced()
    {
        var baseline = new LongHorizonGeEntryBaselineInput(0, null, 5);
        Assert.Throws<LongHorizonGeExplicitZeroReadinessProductIneligibleException>(
            () => LongHorizonGeNumericExecutor.Execute(OneWeek, baseline, VolumeSafetyPolicy.Advanced5D, applyTargetCap: true));
    }

    [Fact]
    public void PositiveReadiness_Succeeds_ForAdvanced()
    {
        var baseline = new LongHorizonGeEntryBaselineInput(38, 12, 5);
        var result = LongHorizonGeNumericExecutor.Execute(OneWeek, baseline, VolumeSafetyPolicy.Advanced5D, applyTargetCap: true);
        Assert.Single(result);
        Assert.Equal(38, result[0].TotalVolumeKm);
    }
}

// ── Dual-KEY catalog content: 8 Advanced profiles, workout-progression v7 lane wiring ──

public sealed class Gen9AdvancedDualKeyProfileTests
{
    [Theory]
    [InlineData("ADVANCED_FOUNDATION_PRIMARY", "AEROBIC_STRENGTH_CONTROLLED_INTRO", 3, "REPEATED")]
    [InlineData("ADVANCED_FOUNDATION_SECONDARY_CONTROLLED", "THRESHOLD_TEMPO", 5, "CONTINUOUS")]
    [InlineData("ADVANCED_BUILD_PRIMARY", "THRESHOLD_TEMPO", 4, "CONTINUOUS")]
    [InlineData("ADVANCED_BUILD_SECONDARY_CONTROLLED", "FARTLEK", 5, "REPEATED")]
    [InlineData("ADVANCED_RACE_SPECIFIC_PRIMARY", "GOAL_PACE_TEN_K", 2, "CONTINUOUS")]
    [InlineData("ADVANCED_RACE_SPECIFIC_SECONDARY_CONTROLLED", "THRESHOLD_TEMPO", 4, "CONTINUOUS")]
    [InlineData("ADVANCED_TAPER_PRIMARY", "GOAL_PACE_TEN_K", 3, "CONTINUOUS")]
    [InlineData("ADVANCED_TAPER_SECONDARY_CONTROLLED", "FARTLEK", 5, "REPEATED")]
    public void AdvancedProfile_LoadsWithGen8FrozenWorkoutRefAndMainSetMode(
        string profileKey, string expectedWorkoutKey, int expectedWorkoutVersion, string expectedMainSetMode)
    {
        var path = Path.Combine(Gen9AdvancedFixture.CatalogRoot(), "prescription-profiles",
            profileKey.ToLowerInvariant().Replace('_', '-') + ".v1.json");
        Assert.True(File.Exists(path), $"Expected catalog file at {path}");
        var json = File.ReadAllText(path);
        Assert.Contains($"\"key\": \"{profileKey}\"", json);
        Assert.Contains($"\"key\": \"{expectedWorkoutKey}\"", json);
        Assert.Contains($"\"version\": {expectedWorkoutVersion}", json);
        Assert.Contains($"\"structureMode\": \"{expectedMainSetMode}\"", json);
    }

    [Fact]
    public async Task WorkoutProgressionV7_LanesReferenceAdvancedProfiles_NotIntermediate()
    {
        var loader = new CatalogWorkoutProgressionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = Gen9AdvancedFixture.CatalogRoot() }));
        var progression = await loader.LoadAsync(new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 7));

        foreach (var phase in new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" })
        {
            var phaseProgression = progression.PhaseProgressions.Single(p => p.PhaseKey == phase);
            Assert.Equal(2, phaseProgression.Lanes.Count);
            foreach (var lane in phaseProgression.Lanes)
            {
                var stage = Assert.Single(lane.Stages);
                var profileKey = Assert.Single(stage.PrescriptionProfileCandidateKeys).Key;
                Assert.StartsWith("ADVANCED_", profileKey);
                Assert.DoesNotContain("INTERMEDIATE", profileKey);
            }
        }
    }

    [Fact]
    public async Task WorkoutProgressionV6_IntermediateLanes_RemainUnchanged()
    {
        // Byte-identical-content regression: authoring v7 must not have touched v6.
        var loader = new CatalogWorkoutProgressionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = Gen9AdvancedFixture.CatalogRoot() }));
        var progression = await loader.LoadAsync(new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 6));
        var buildLane0 = progression.PhaseProgressions.Single(p => p.PhaseKey == "BUILD").Lanes.Single(l => l.LaneOrdinal == 0);
        var profileKey = Assert.Single(Assert.Single(buildLane0.Stages).PrescriptionProfileCandidateKeys).Key;
        Assert.Equal("INTERMEDIATE_5D_BUILD_PRIMARY", profileKey);
    }
}

// ── Structural materialization: RunLayout, GE/Runway/Core shape, candidate identity ──

public sealed class Gen9AdvancedStructuralMaterializationTests
{
    [Theory]
    [InlineData(3, 1, 1, 1)]
    [InlineData(4, 1, 2, 1)]
    [InlineData(5, 2, 2, 1)]
    [InlineData(6, 2, 3, 1)]
    public async Task StructuralRoadmap_ResolvesExactAdvancedIdentityAndCoreShape(int daysPerWeek, int expectedKey, int expectedEasy, int expectedLong)
    {
        var request = Gen9AdvancedFixture.BuildActivationRequest(21, daysPerWeek);
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            request.CompositionDecision, request.CatalogRoot, request.WorkoutLoader, default, daysPerWeek, RunningBackground.Advanced);

        var expectedCandidateKey = daysPerWeek switch
        {
            3 => LongHorizonStructuralMaterializer.CandidateKeyAdvancedThreeDay,
            4 => LongHorizonStructuralMaterializer.CandidateKeyAdvancedFourDay,
            5 => LongHorizonStructuralMaterializer.CandidateKeyAdvancedFiveDay,
            6 => LongHorizonStructuralMaterializer.CandidateKeyAdvancedSixDay,
            _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek)),
        };
        Assert.Equal(expectedCandidateKey, skeleton.CandidateKey);
        Assert.Equal(8, skeleton.PreparationRunwayWeeks);
        Assert.Equal(12, skeleton.CoreWeeks);

        var firstCoreWeek = skeleton.Weeks.First(w => w.Segment == LongHorizonSegmentType.Core);
        Assert.Equal(expectedKey, firstCoreWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
        Assert.Equal(expectedEasy, firstCoreWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT"));
        Assert.Equal(expectedLong, firstCoreWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "LONG_RUN"));

        var geWeek = skeleton.Weeks[0];
        Assert.Equal(1, geWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
        Assert.Equal(daysPerWeek - 2, geWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, geWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "LONG_RUN"));
    }

    [Fact]
    public async Task IntermediateSixDay_StructuralIdentity_RemainsUnchanged()
    {
        // Zero-delta regression: the Advanced level-aware overload must not alter
        // the pre-existing Intermediate default-level call shape.
        var request = Gen9AdvancedFixture.BuildActivationRequest(21, 6);
        var intermediateRequest = new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = request.CompositionDecision,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 6,
            StartDate = Gen9AdvancedFixture.StartDate,
            RaceDate = request.RaceDate,
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(26, 8, 6),
            PreferredDays = Gen9AdvancedFixture.PreferredDays(6),
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = request.CatalogRoot,
            WorkoutLoader = request.WorkoutLoader,
        };
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            intermediateRequest.CompositionDecision, intermediateRequest.CatalogRoot, intermediateRequest.WorkoutLoader, default, 6);
        Assert.Equal("TEN_K__6D__INTERMEDIATE", skeleton.CandidateKey);
    }
}

// ── Full lifecycle: Advanced x5D, real PostgreSQL, organic dual-KEY Core with Advanced profiles ──

public sealed class Gen9AdvancedFiveDayFullLifecycleTests
{
    [Fact]
    public async Task AdvancedFiveDayLongHorizon_ReachesOrganicCoreWithAdvancedDualKeyProfiles_AfterRealPostgresRestart()
    {
        var request = Gen9AdvancedFixture.BuildActivationRequest(21, 5);
        var runtime = new LongHorizonRollingInitialActivationRuntime();
        var result = await runtime.BuildInitialActivationAsync(request);
        Assert.Equal(LongHorizonRollingInitialActivationStatus.Approved, result.Status);

        var planStateId = Guid.NewGuid();
        var candidate = await new CatalogCandidateEligibilityGate(
            new PlanCatalogBundleLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = request.CatalogRoot }), NullLogger<PlanCatalogBundleLoader>.Instance))
            .LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.AdvancedFiveDayCandidateKey, V1CatalogPilotIdentityPolicy.AdvancedFiveDayCandidateVersion);

        var initRequest = new LongHorizonRollingInitializationRequest
        {
            PlanStateId = planStateId,
            StructuralRoadmap = result.StructuralRoadmap!,
            PlanStartDate = Gen9AdvancedFixture.StartDate,
            PreferredDays = Gen9AdvancedFixture.PreferredDays(5),
            LongRunDay = DayOfWeek.Sunday,
            InitialWindow = result.ActivationWindow!,
            LifecycleStates = result.StructuralRoadmap!.Weeks.ToDictionary(w => w.GlobalWeekNumber, w => w.NumericLifecycleState),
            ActivatedWeeks = result.ActivatedNumericWeeks.ToDictionary(w => w.GlobalWeekNumber, w => w),
            ContextVersion = result.ContextVersion!,
            CatalogRootPath = request.CatalogRoot,
            Candidate = candidate,
            DaysPerWeek = 5,
        };

        using (var initDb = LongHorizonPersistenceTestFixture.NewContext())
        {
            await new LongHorizonRollingStateRepository(initDb).InitializeStructuralStateAsync(initRequest);
        }

        async Task<LongHorizonRollingPersistenceResult> AdvanceAsync(DateOnly checkpointDate)
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
                CurrentAvailability = Gen9AdvancedFixture.PreferredDays(5),
                LongRunDay = DayOfWeek.Sunday,
                SafetyState = LongHorizonSafetyState.Clear,
                ReadinessProfile = state.StructuralRoadmap.Profile,
                PriorValidatedAnchor = new LongHorizonPriorValidatedAnchor(
                    new ValidatedSustainableLoad
                    {
                        WeeklyVolumeKm = 42,
                        LongRunKm = 13,
                        EvidenceWindowStartWeek = 1,
                        EvidenceWindowEndWeek = 1,
                        CompletedEvidenceWeekNumbers = [1],
                        ExcludedRecoveryWeekNumbers = [],
                        WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
                        LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
                        RoundingPolicy = "VolumeSafetyPolicy.0.5km",
                        LongRunCapPolicy = "VolumeSafetyPolicy.LongRunHardCapShare=0.36",
                        ValidationStatus = LongHorizonValidationStatus.Valid,
                        Provenance = "GEN.9 test anchor",
                        ContextVersion = null,
                    },
                    IsFreshForCurrentInvocation: true,
                    SourceContextSequence: 0),
                PreviousContextVersion = state.ContextVersion,
                GoalType = GoalType.Race,
                GoalDistance = GoalDistance.TenK,
                Level = RunningBackground.Advanced,
                DaysPerWeek = 5,
            };
            var checkpoint = await checkpointRuntime.EvaluateAndActivateNextGeWindowAsync(checkpointRequest);

            var geEnd = state.StructuralRoadmap.GeneralEnduranceWeeks;
            var reachesGeBoundary = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated
                && checkpoint.ActivationWindow!.EndGlobalWeek == geEnd;
            var pureGe = checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated && !reachesGeBoundary;

            if (pureGe)
                return await new LongHorizonRollingActivationPersistenceAdapter(repo).PersistGeCheckpointAsync(planStateId, snapshot.ConcurrencyVersion, checkpoint);

            var continuation = new LongHorizonRollingRestartContinuationService(repo);
            return await continuation.ContinueJitCompositionAsync(
                planStateId, checkpoint.EvidenceSnapshot!, checkpoint.ValidatedLoad ?? new ValidatedSustainableLoad
                {
                    WeeklyVolumeKm = 42, LongRunKm = 13, EvidenceWindowStartWeek = 1, EvidenceWindowEndWeek = 1,
                    CompletedEvidenceWeekNumbers = [1], ExcludedRecoveryWeekNumbers = [],
                    WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
                    LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
                    RoundingPolicy = "VolumeSafetyPolicy.0.5km", LongRunCapPolicy = "VolumeSafetyPolicy.LongRunHardCapShare=0.36",
                    ValidationStatus = LongHorizonValidationStatus.Valid, Provenance = "GEN.9 test anchor", ContextVersion = null,
                },
                checkpoint.EvidenceSnapshot!.CompletedRunsCount == 0 ? null : 5,
                checkpoint.CheckpointDecision,
                checkpoint.Outcome == LongHorizonRollingCheckpointRuntimeOutcome.NextGeWindowActivated ? checkpoint.NewlyActivatedWeeks : null,
                Gen9AdvancedFixture.StartDate, Gen9AdvancedFixture.StartDate.AddDays(state.StructuralRoadmap.TotalWeeks * 7),
                Gen9AdvancedFixture.PreferredDays(5), DayOfWeek.Sunday, request.CatalogRoot,
                lifecycleStatesOverride: state.LifecycleStates,
                // Real, published release: plan-catalog/artifacts/appsel-plan-catalog/1.3.0/bundles/TEN_K__5D__ADVANCED.v1.json
                // (this phase's own publish, additive over 1.2.0 -- see governance report).
                publishedBundleReleaseVersion: "1.3.0",
                targetFinishTimeSeconds: 2700,
                targetFinishTimeSource: TargetFinishTimeSource.ProductAverage);
        }

        var checkpointDate = Gen9AdvancedFixture.StartDate.AddDays(14);
        var last = await AdvanceAsync(checkpointDate);
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, last.Outcome);

        for (var i = 0; i < 3 && last.Outcome == LongHorizonRollingPersistenceOutcome.Success; i++)
        {
            var nextDate = checkpointDate.AddDays(28 * (i + 1));
            last = await AdvanceAsync(nextDate);
        }
        Assert.Equal(LongHorizonRollingPersistenceOutcome.Success, last.Outcome);

        using var db = LongHorizonPersistenceTestFixture.NewContext();
        var firstCoreWeek = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == planStateId && s.Week.GlobalWeek == 10).ToListAsync();

        Assert.Equal(5, firstCoreWeek.Count);
        Assert.Equal(2, firstCoreWeek.Count(s => s.SessionRole == "KEY_SESSION"));
        var lane0 = Assert.Single(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 0);
        var lane1 = Assert.Single(firstCoreWeek, s => s.SessionRole == "KEY_SESSION" && s.LaneOrdinal == 1);
        Assert.StartsWith("ADVANCED_", lane0.CatalogPrescriptionProfileKey);
        Assert.StartsWith("ADVANCED_", lane1.CatalogPrescriptionProfileKey);
        Assert.DoesNotContain("INTERMEDIATE", lane0.CatalogPrescriptionProfileKey);
        Assert.DoesNotContain("INTERMEDIATE", lane1.CatalogPrescriptionProfileKey);

        var plan = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == planStateId);
        Assert.Equal(5, plan.DaysPerWeek);
    }
}

// ── Isolation: public gate closed for Advanced 2D/7D; Intermediate/Beginner/Experienced unaffected ──
// Phase 10K-GEN.10 -- Advanced 3D/4D/5D/6D are now legitimately, publicly
// activated (see Gen10AdvancedCombinedPublicActivationTests). This class's
// own former blanket "Advanced is unsupported at every frequency" assertion
// is corrected to reflect that: only 2D (OUT_OF_V1, never designed) and 7D
// (PRODUCT_NON_SUPPORT, GEN.7) remain closed by construction.

public sealed class Gen9AdvancedIsolationTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void PublicIdentityPolicy_RecognizesAdvancedActivatedFrequencies(int daysPerWeek)
    {
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, daysPerWeek));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, daysPerWeek));
        var (key, version) = V1CatalogPilotIdentityPolicy.ResolveCandidate(RunningBackground.Advanced, daysPerWeek);
        Assert.Equal($"TEN_K__{daysPerWeek}D__ADVANCED", key);
        Assert.Equal(1, version);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    public void PublicIdentityPolicy_DoesNotRecognizeAdvancedOutOfScopeFrequencies(int daysPerWeek)
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, daysPerWeek));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, daysPerWeek));
        Assert.Throws<ArgumentOutOfRangeException>(() => V1CatalogPilotIdentityPolicy.ResolveCandidate(RunningBackground.Advanced, daysPerWeek));
    }

    [Fact]
    public void ExperiencedLevel_RemainsUnrecognized_ForEveryFrequency()
    {
        foreach (var days in new[] { 3, 4, 5, 6, 7 })
            Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Experienced, days));
    }

    [Fact]
    public void IntermediateAndBeginnerIdentity_RemainUnchanged()
    {
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 4));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 5));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 6));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 5));
    }

    [Fact]
    public void IntermediateVolumePolicies_RemainByteIdentical()
    {
        Assert.Equal(38d, VolumeSafetyPolicy.Default.ResolvedPeakReference.Value);
        Assert.Equal(44.5d, VolumeSafetyPolicy.FiveDayIntermediate.ResolvedPeakReference.Value);
        Assert.Equal(44.5d, VolumeSafetyPolicy.SixDayIntermediate.ResolvedPeakReference.Value);
        Assert.Equal(21d, VolumeSafetyPolicy.BeginnerFourDay.ResolvedPeakReference.Value);
    }
}
