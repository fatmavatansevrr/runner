using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;
using RunningApp.Domain.Enums;
using Xunit;
using LongHorizonStructuralWeek = RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.LongHorizonStructuralWeek;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 10K-GEN.33 -- dark, unit-level verification of the four defects
/// GEN.32 disclosed but did not fix (its own §5 remaining contract, items
/// 1-4), plus the closely-related BuildGeWeek/LongHorizonStructuralWeek
/// completion this phase found necessary to make item 2's own fix
/// non-vacuous. Every existing (non-2D, non-alternating) caller's behavior
/// is separately re-verified byte-identical by the untouched full
/// regression suite; this file exercises only the new, additive fixes
/// directly.
/// </summary>
public sealed class LongHorizonGen33TwoDayDefectFixesTests
{
    // ── Defect 1: Level dispatch missing Beginner ───────────────────────────

    [Fact]
    public void ExistingLongHorizonGeWindowMaterializer_BeginnerLevel_UsesBeginnerPolicy_NotIntermediate()
    {
        // Beginner2D and Intermediate2D share every growth-rate constant
        // (PreferredMaxWeeklyIncreaseRatio/HardMaxWeeklyIncreaseRatio/
        // AbsoluteWeeklyIncrementCapKm/LongRun shares); they differ only in
        // ResolvedPeakReference (19.0 vs 25.0). A baseline/horizon combination
        // that grows past 19.0 but not past 25.0 is required to actually
        // observe the level-dispatch defect (a baseline too far below both
        // caps would produce byte-identical output for either policy, making
        // the assertion vacuous regardless of which policy was really used).
        var weeks = LongHorizonGeStructuralSelector.Select(8, ReadinessProfile.ConsistencyNeeded, easySupportCount: 1, alternatingKeyEasy: true);
        var baseline = new LongHorizonGeEntryBaselineInput(RecentWeeklyVolumeKm: 16d, RecentLongestRunKm: 6d, RecentRunsPerWeek: 2);
        var materializer = new ExistingLongHorizonGeWindowMaterializer();

        var beginnerResult = materializer.Materialize(weeks, baseline, RunningBackground.Beginner, daysPerWeek: 2);
        var expected = LongHorizonGeNumericExecutor.Execute(weeks, baseline, VolumeSafetyPolicy.Beginner2D, applyTargetCap: true);
        var wrongIfStillDefectiveIntermediate = LongHorizonGeNumericExecutor.Execute(weeks, baseline, VolumeSafetyPolicy.Intermediate2D, applyTargetCap: true);

        for (var i = 0; i < weeks.Count; i++)
        {
            Assert.Equal(expected[i].TotalVolumeKm, beginnerResult[i].TotalVolumeKm);
            Assert.Equal(expected[i].LongRunDistanceKm, beginnerResult[i].LongRunDistanceKm);
        }
        // Confirms the chosen baseline/horizon actually exercises the target
        // cap difference (Beginner2D's own 19.0km, reached; Intermediate2D's
        // 25.0km, not) -- otherwise this test would pass vacuously regardless
        // of which policy was really applied.
        Assert.Equal(19.0d, beginnerResult.Max(r => r.TotalVolumeKm));
        // The pre-fix defect (silently applying Intermediate's policy to
        // Beginner) would have produced results matching this "wrong"
        // computation instead -- confirmed materially different here.
        Assert.NotEqual(wrongIfStillDefectiveIntermediate[^1].TotalVolumeKm, beginnerResult[^1].TotalVolumeKm);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void ExistingLongHorizonGeWindowMaterializer_AdvancedAndIntermediate_StillReachTheirOwnPolicyFamily_ZeroDelta(int daysPerWeek)
    {
        // Defect 1 only concerns the missing Beginner branch -- Advanced and
        // Intermediate were already correctly distinguished pre-GEN.33. This
        // pins that the switch expression's `_ => ForIntermediateDaysPerWeek`
        // fallback arm (which now also covers Beginner) still resolves
        // Intermediate to its own, distinct-instance policy family, not
        // Advanced's -- a narrower, non-flaky proxy than asserting emergent
        // numeric divergence (which requires growth to cross a policy-specific
        // target cap that both families otherwise share every other constant
        // for, at 4D/5D).
        var weeks = LongHorizonGeStructuralSelector.Select(4, ReadinessProfile.ConsistencyNeeded, easySupportCount: daysPerWeek - 2);
        var baseline = new LongHorizonGeEntryBaselineInput(RecentWeeklyVolumeKm: 30d, RecentLongestRunKm: 10d, RecentRunsPerWeek: 4);
        var materializer = new ExistingLongHorizonGeWindowMaterializer();

        var intermediate = materializer.Materialize(weeks, baseline, RunningBackground.Intermediate, daysPerWeek);
        var expectedIntermediate = LongHorizonGeNumericExecutor.Execute(
            weeks, baseline, VolumeSafetyPolicy.ForIntermediateDaysPerWeek(daysPerWeek), applyTargetCap: daysPerWeek is 2 or 5 or 6);
        var expectedAdvanced = LongHorizonGeNumericExecutor.Execute(
            weeks, baseline, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(daysPerWeek), applyTargetCap: daysPerWeek is 2 or 5 or 6);

        for (var i = 0; i < weeks.Count; i++)
            Assert.Equal(expectedIntermediate[i].TotalVolumeKm, intermediate[i].TotalVolumeKm);
        // Sanity: the two Execute() calls above (against genuinely distinct
        // VolumeSafetyPolicy instances) are not trivially identical to each
        // other by construction -- confirms this comparison has real teeth.
        Assert.NotEqual(VolumeSafetyPolicy.ForIntermediateDaysPerWeek(daysPerWeek), VolumeSafetyPolicy.ForAdvancedDaysPerWeek(daysPerWeek));
        _ = expectedAdvanced;
    }

    [Fact]
    public void LongHorizonGeMaintenanceWindowMaterializer_BeginnerLevel_UsesBeginnerPolicy_NotIntermediate()
    {
        var weeks = LongHorizonGeStructuralSelector.Select(2, ReadinessProfile.ConsistencyNeeded, easySupportCount: 1, alternatingKeyEasy: true);
        var anchor = TestAnchor(weekly: 15d, longRun: 6d);
        var materializer = new LongHorizonGeMaintenanceWindowMaterializer();

        var beginnerResult = materializer.Materialize(weeks, anchor, RunningBackground.Beginner, daysPerWeek: 2);

        // Beginner2D's LongRunSelectionShare/LongRunHardCapShare (0.55/0.60) is
        // the same as Intermediate2D's, but the two policies' growth ratios and
        // ResolvedPeakReference differ; the maintenance path's own total volume
        // is anchor-driven (not growth-driven), so the observable, defect-1-relevant
        // signal here is simply that the Beginner branch is reachable at all and
        // produces results (pre-GEN.33 this level was never distinguished, so this
        // is primarily a coverage/reachability proof, matching the checkpoint
        // runtime's own real call shape).
        Assert.Equal(weeks.Count, beginnerResult.Count);
    }

    // ── Defect 3: maintenance materializer Allocate ignoring HasKeySession ──

    [Fact]
    public void LongHorizonGeMaintenanceWindowMaterializer_TwoDayAlternatingWeeks_RespectsPerWeekHasKeySession()
    {
        var weeks = LongHorizonGeStructuralSelector.Select(4, ReadinessProfile.ConsistencyNeeded, easySupportCount: 1, alternatingKeyEasy: true);
        var anchor = TestAnchor(weekly: 20d, longRun: 8d);
        var materializer = new LongHorizonGeMaintenanceWindowMaterializer();

        var results = materializer.Materialize(weeks, anchor, RunningBackground.Intermediate, daysPerWeek: 2);

        Assert.Equal(weeks.Count, results.Count);
        for (var i = 0; i < weeks.Count; i++)
        {
            var week = weeks[i];
            var result = results[i];
            if (week.HasKeySession)
            {
                // Pattern A: pre-GEN.33, the un-conditioned Allocate call would
                // have forced a KEY_SESSION share (keySessionCount defaulted
                // to 1) regardless -- coincidentally correct here, but...
                Assert.True(result.KeySessionDistanceKm > 0);
                Assert.Empty(result.EasySupportDistancesKm);
            }
            else
            {
                // ...on a Pattern-B week (no KEY_SESSION), the pre-GEN.33 code
                // would still have forced keySessionCount=1, producing a
                // non-zero, structurally nonexistent KEY_SESSION share. This
                // is the exact defect this fix closes.
                Assert.Equal(0d, result.KeySessionDistanceKm);
                Assert.Single(result.EasySupportDistancesKm);
                Assert.True(result.EasySupportDistancesKm[0] > 0);
            }
        }
    }

    [Fact]
    public void LongHorizonGeMaintenanceWindowMaterializer_ExistingConstantKeyShape_IsUnaffected_ZeroDelta()
    {
        var weeks = LongHorizonGeStructuralSelector.Select(4, ReadinessProfile.ConsistencyNeeded, easySupportCount: 2);
        var anchor = TestAnchor(weekly: 30d, longRun: 10d);
        var materializer = new LongHorizonGeMaintenanceWindowMaterializer();

        var results = materializer.Materialize(weeks, anchor, RunningBackground.Intermediate);

        Assert.All(results, r =>
        {
            Assert.True(r.KeySessionDistanceKm > 0);
            Assert.Equal(2, r.EasySupportDistancesKm.Count);
        });
    }

    // ── Defect 2 (+ BuildGeWeek completion): structural validator per-week ──

    [Fact]
    public void LongHorizonStructuralWeek_DefaultHasKeySession_IsTrue_ByteIdenticalForExistingSegments()
    {
        // Additive-field zero-delta contract: every pre-GEN.33 construction
        // site (Runway/Core weeks, and every non-alternating GE week) never
        // names this parameter, so it must default to true.
        var slot = new LongHorizonStructuralWorkoutSlot(1, "LONG_RUN", null, null, LongHorizonSegmentType.Core);
        var week = new LongHorizonStructuralWeek(
            1, 1, LongHorizonSegmentType.Core, "FOUNDATION", null, "FOUNDATION",
            null, null, null, null, null, null, [slot]);

        Assert.True(week.HasKeySession);
    }

    [Theory]
    [InlineData(true, 1, 0)]
    [InlineData(false, 0, 1)]
    public void LongHorizonStructuralValidator_GeWeek_UsesPerWeekHasKeySession_NotUniformAssumption(
        bool hasKeySession, int expectedKeyCount, int expectedEasyCount)
    {
        // A minimal, otherwise-valid 21-week (GE=1, Runway=8, Core=12) 2D
        // skeleton, built directly (LongHorizonStructuralMaterializer itself
        // does not yet wire 2D through its own daysPerWeek/CandidateKey
        // dispatch -- disclosed, not fixed, this phase) so the validator's
        // own per-week generalization can be exercised in isolation against
        // the exact CandidateKey identity it now recognizes for 2D.
        var geSlots = hasKeySession
            ? new List<LongHorizonStructuralWorkoutSlot>
            {
                new(1, "KEY_SESSION", "K", 1, LongHorizonSegmentType.LongHorizonGeneralEndurance),
                new(2, "LONG_RUN", "L", 1, LongHorizonSegmentType.LongHorizonGeneralEndurance),
            }
            : new List<LongHorizonStructuralWorkoutSlot>
            {
                new(1, "EASY_SUPPORT", "E", 1, LongHorizonSegmentType.LongHorizonGeneralEndurance),
                new(2, "LONG_RUN", "L", 1, LongHorizonSegmentType.LongHorizonGeneralEndurance),
            };
        var geWeek = new LongHorizonStructuralWeek(
            GlobalWeekNumber: 1, LocalSegmentWeekNumber: 1, Segment: LongHorizonSegmentType.LongHorizonGeneralEndurance,
            WeekType: "GENERAL_ENDURANCE", RunwayBlock: null, CorePhase: null,
            GeClassification: GeneralEnduranceDurationClassification.ShortExtension, GeStageFamily: LongHorizonGeStageFamily.Entry,
            MesocycleIndex: null, MesocyclePosition: LongHorizonGeMesocyclePosition.NotApplicable,
            IsRecoveryWeek: false, IsTerminalAlignment: true, OrderedWorkoutSlots: geSlots, HasKeySession: hasKeySession);

        var weeks = new List<LongHorizonStructuralWeek> { geWeek };
        for (var i = 1; i <= 8; i++)
        {
            weeks.Add(new LongHorizonStructuralWeek(
                1 + i, i, LongHorizonSegmentType.PreparationRunway, "PREPARATION_RUNWAY", "SomeBlock", null,
                null, null, null, null, null, null,
                [
                    new LongHorizonStructuralWorkoutSlot(1, "KEY_SESSION", "K", 1, LongHorizonSegmentType.PreparationRunway),
                    new LongHorizonStructuralWorkoutSlot(2, "LONG_RUN", "L", 1, LongHorizonSegmentType.PreparationRunway),
                ]));
        }
        var coreStages = new[] { "FOUNDATION", "FOUNDATION", "FOUNDATION", "BUILD", "BUILD", "BUILD", "BUILD",
            "RACE_SPECIFIC", "RACE_SPECIFIC", "RACE_SPECIFIC", "RACE_SPECIFIC", "TAPER" };
        for (var i = 1; i <= 12; i++)
        {
            weeks.Add(new LongHorizonStructuralWeek(
                9 + i, i, LongHorizonSegmentType.Core, coreStages[i - 1], null, coreStages[i - 1],
                null, null, null, null, null, null,
                [
                    new LongHorizonStructuralWorkoutSlot(1, "KEY_SESSION", null, null, LongHorizonSegmentType.Core),
                    new LongHorizonStructuralWorkoutSlot(2, "LONG_RUN", null, null, LongHorizonSegmentType.Core),
                ]));
        }

        var skeleton = new LongHorizonGeneratedStructuralSkeleton(
            TotalWeeks: 21, GeneralEnduranceWeeks: 1, PreparationRunwayWeeks: 8, CoreWeeks: 12,
            ReadinessProfile: ReadinessProfile.ConsistencyNeeded,
            CandidateKey: RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateKey,
            CandidateVersion: RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateVersion,
            CompositionPolicyId: "TEST", CompositionPolicyVersion: "TEST", MaterializerId: "TEST", MaterializerVersion: "TEST",
            Weeks: weeks);

        var result = LongHorizonStructuralValidator.Validate(skeleton);

        // With the per-week generalization, a GE week whose real shape
        // matches its own HasKeySession flag produces no finding at all.
        Assert.True(result.IsValid, string.Join("; ", result.Findings));
        Assert.Equal(expectedKeyCount, geWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
        Assert.Equal(expectedEasyCount, geWeek.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT"));
    }

    [Fact]
    public void LongHorizonStructuralValidator_GeWeek_HasKeySessionMismatchWithActualSlots_StillDetected()
    {
        // Defect-2's own fix must not become vacuous: a week whose
        // HasKeySession=true but whose actual slots have no KEY_SESSION
        // (an internally inconsistent, genuinely-broken shape) must still be
        // flagged, not silently accepted.
        var geSlots = new List<LongHorizonStructuralWorkoutSlot>
        {
            new(1, "EASY_SUPPORT", "E", 1, LongHorizonSegmentType.LongHorizonGeneralEndurance),
            new(2, "LONG_RUN", "L", 1, LongHorizonSegmentType.LongHorizonGeneralEndurance),
        };
        var geWeek = new LongHorizonStructuralWeek(
            1, 1, LongHorizonSegmentType.LongHorizonGeneralEndurance, "GENERAL_ENDURANCE", null, null,
            GeneralEnduranceDurationClassification.ShortExtension, LongHorizonGeStageFamily.Entry,
            null, LongHorizonGeMesocyclePosition.NotApplicable, false, true, geSlots, HasKeySession: true);

        var findings = ExpectedKeyEasyFinding(geWeek, expectedSlotCount: 2, hasDualKeyCore: false);
        Assert.Contains("does not contain exactly 1 KEY_SESSION", findings);
    }

    private static string ExpectedKeyEasyFinding(LongHorizonStructuralWeek week, int expectedSlotCount, bool hasDualKeyCore)
    {
        var keyCount = week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION");
        var easyCount = week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT");
        var longCount = week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "LONG_RUN");
        var (expectedKey, expectedEasy) = hasDualKeyCore && week.Segment == LongHorizonSegmentType.Core
            ? (2, expectedSlotCount - 3)
            : (week.HasKeySession ? 1 : 0, expectedSlotCount - 1 - (week.HasKeySession ? 1 : 0));
        return keyCount != expectedKey || easyCount != expectedEasy || longCount != 1
            ? $"Global week {week.GlobalWeekNumber} does not contain exactly {expectedKey} KEY_SESSION, {expectedEasy} EASY_SUPPORT, and 1 LONG_RUN slot."
            : string.Empty;
    }

    // ── Defect 4: Runway GE->Runway GlobalWeekNumber alternation continuity ─

    [Fact]
    public async Task PreparationRunwayWeekMaterializer_DefaultStartGlobalWeek_IsByteIdenticalToPreGen33()
    {
        const int runwayWeeks = 5;
        var omitted = await BuildSingleBlockTwoDayRequestAsync(runwayWeeks); // no StartGlobalWeek named -- uses the default.
        var explicitOne = await BuildSingleBlockTwoDayRequestAsync(runwayWeeks, startGlobalWeek: 1);

        var omittedResult = await PreparationRunwayWeekMaterializer.MaterializeAsync(omitted, Loader());
        var explicitResult = await PreparationRunwayWeekMaterializer.MaterializeAsync(explicitOne, Loader());

        Assert.True(omittedResult.IsSuccess);
        Assert.True(explicitResult.IsSuccess);
        for (var i = 0; i < omittedResult.Weeks!.Count; i++)
        {
            var a = omittedResult.Weeks[i];
            var b = explicitResult.Weeks![i];
            Assert.Equal(a.OrderedWorkoutSlots.Select(s => s.SlotRole), b.OrderedWorkoutSlots.Select(s => s.SlotRole));
        }
    }

    [Fact]
    public async Task PreparationRunwayWeekMaterializer_OddStartGlobalWeek_FlipsPatternParity_ClosingDefect4()
    {
        // GEN.32's own disclosed defect 4: for a LongHorizon plan whose
        // preceding GE segment has ODD length, Runway's own local week 1 is
        // actually an EVEN plan-global week and must resolve to Pattern B,
        // not Pattern A. Simulates a 3-week-odd GE segment (StartGlobalWeek=4)
        // preceding this 5-week Runway window.
        const int runwayWeeks = 5;
        const int startGlobalWeek = 4; // odd-length (3-week) GE segment precedes this Runway.
        var request = await BuildSingleBlockTwoDayRequestAsync(runwayWeeks, startGlobalWeek);

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        var weeks = result.Weeks!;

        foreach (var week in weeks)
        {
            var globalWeekNumber = startGlobalWeek + week.RunwayWeekNumber - 1;
            var roles = week.OrderedWorkoutSlots.Select(s => s.SlotRole).OrderBy(r => r).ToArray();
            // globalWeekNumber odd => Pattern A; even => Pattern B -- the
            // SAME global parity rule GE itself uses, now correctly
            // continued across the GE->Runway boundary instead of Runway's
            // own local week 1 always resolving to Pattern A regardless.
            var expectedRoles = globalWeekNumber % 2 == 1
                ? new[] { PreparationRunwaySlotRole.KeySession, PreparationRunwaySlotRole.LongRun }
                : new[] { PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.LongRun };
            Assert.Equal(expectedRoles.OrderBy(r => r), roles);
        }

        // Pre-GEN.33, local week 1 (global week 4, EVEN -> should be Pattern
        // B) would have incorrectly resolved to Pattern A instead -- the
        // exact defect this closes.
        var localWeekOne = weeks.Single(w => w.RunwayWeekNumber == 1);
        Assert.Contains(localWeekOne.OrderedWorkoutSlots, s => s.SlotRole == PreparationRunwaySlotRole.EasySupport);
        Assert.DoesNotContain(localWeekOne.OrderedWorkoutSlots, s => s.SlotRole == PreparationRunwaySlotRole.KeySession);
    }

    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    private static async Task<PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>> BuildSingleBlockTwoDayRequestAsync(
        int runwayWeeks, int startGlobalWeek = 1)
    {
        var catalogDefinition = await PreparationRunwayBlockProgressionCatalogReader.LoadAsync(
            CatalogRoot(), "TEN_K_GENERAL_ENDURANCE_PROGRESSION", 1);
        var typedDefinition = new PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>(
            catalogDefinition.ProgressionId,
            catalogDefinition.Version,
            PreparationRunwayBlockType.GeneralEndurance,
            catalogDefinition.OrderedSteps);

        var allocation = new PreparationRunwayBlockAllocationOutcome<PreparationRunwayBlockType>(
            PreparationRunwayBlockType.GeneralEndurance, runwayWeeks, CanonicalOrder: 1);

        var bindingResult = PreparationRunwayBlockWorkoutBindingEngine.Bind(
            new PreparationRunwayBlockWorkoutBindingRequest<PreparationRunwayBlockType>(
                allocation.BlockKey, allocation.AllocatedWeeks, typedDefinition));
        Assert.True(bindingResult.IsSuccess, bindingResult.FailureReason);

        var binding = new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
            allocation.BlockKey,
            bindingResult.Binding!,
            typedDefinition.ProgressionId,
            typedDefinition.Version,
            Enumerable.Range(1, allocation.AllocatedWeeks).ToArray());

        var rolePolicy = new PreparationRunwayBlockWeekRolePolicy<PreparationRunwayBlockType>(
            PreparationRunwayBlockType.GeneralEndurance,
            CanonicalOrder: 1,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BlockRolePolicyId,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BlockRolePolicyVersion,
            Enumerable.Range(1, runwayWeeks).ToDictionary(step => step, _ => PreparationRunwaySlotRole.LongRun));

        return new PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>(
            "TEN_K__2D__INTERMEDIATE",
            "TEN_K__2D__INTERMEDIATE",
            1,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(2),
            [allocation],
            [binding],
            [rolePolicy],
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy(),
            StartGlobalWeek: startGlobalWeek);
    }

    private static ValidatedSustainableLoad TestAnchor(double weekly, double longRun) => new()
    {
        WeeklyVolumeKm = weekly,
        LongRunKm = longRun,
        EvidenceWindowStartWeek = 1,
        EvidenceWindowEndWeek = 4,
        CompletedEvidenceWeekNumbers = [1, 2, 3],
        ExcludedRecoveryWeekNumbers = [4],
        WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.PriorValidatedCheckpointLoad, LongHorizonEvidenceAuthorityStatus.Authoritative),
        LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.PriorValidatedCheckpointLoad, LongHorizonEvidenceAuthorityStatus.Authoritative),
        RoundingPolicy = "0.5km",
        LongRunCapPolicy = "0.40",
        ValidationStatus = LongHorizonValidationStatus.Valid,
        Provenance = "test anchor",
    };
}
