using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

public sealed class PreparationRunwayPaceMaterializerTests
{
    public static IEnumerable<object[]> Matrix =>
        Enum.GetValues<PreparationRunwayAllocationProfile>()
            .SelectMany(profile => Enumerable.Range(3, 6).Select(weeks => new object[] { profile, weeks }));

    [Theory]
    [MemberData(nameof(Matrix))]
    internal void BothProfiles_AllRunwayLengths_AllSlotsPaced_TransitionCompatible_InvariantsPreserved(
        PreparationRunwayAllocationProfile profile, int runwayWeeks)
    {
        var request = Request(profile, runwayWeeks, RecentRaceContext());
        var result = PreparationRunwayPaceMaterializer.Materialize(request);

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(runwayWeeks, result.PacedRunwayWeeks!.Count);
        Assert.All(result.PacedRunwayWeeks, week => Assert.Equal(4, week.StructuralOrderedSlots.Count));
        Assert.All(result.PacedRunwayWeeks.SelectMany(w => w.StructuralOrderedSlots), slot =>
        {
            Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, slot.PacePrescription.Kind);
            Assert.Equal(CatalogPaceSourceSelection.EffortOnly, slot.PacePrescription.Source);
            Assert.Null(slot.PacePrescription.SecondsPerKilometer);
            Assert.DoesNotContain("GOAL_PACE", slot.PacePrescription.EffortLabel, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("THRESHOLD", slot.PacePrescription.EffortLabel, StringComparison.OrdinalIgnoreCase);
            Assert.True(slot.OriginalSlot.PrescribedSlot.PlannedDistanceKm > 0);
        });
        Assert.True(result.ContinuityAnalysis!.IsValid);
        Assert.True(result.ContinuityAnalysis.AllTransitionSlotsCompatible);
        Assert.True(result.ContinuityAnalysis.NoGoalPace);
        Assert.True(result.ContinuityAnalysis.NoRaceSpecificPace);
        Assert.True(result.ContinuityAnalysis.NoThresholdPace);
        Assert.Equal(
            request.DatedCombinedPlan.DatedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots).Select(s => s.SessionDate),
            result.PacedRunwayWeeks.SelectMany(w => w.StructuralOrderedSlots).Select(s => s.OriginalSlot.SessionDate));
        Assert.Equal(
            request.DatedCombinedPlan.DatedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots).Select(s => (s.PrescribedSlot.StructuralSlot.WorkoutId, s.PrescribedSlot.StructuralSlot.WorkoutVersion)),
            result.PacedRunwayWeeks.SelectMany(w => w.StructuralOrderedSlots).Select(s => (s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutId, s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutVersion)));
    }

    [Theory]
    [InlineData(PrescriptionPaceSource.RecentRace, null, PreparationRunwayPaceEvidenceState.ProvidedRecentRace)]
    [InlineData(PrescriptionPaceSource.TargetGoal, TargetFinishTimeSource.UserDefined, PreparationRunwayPaceEvidenceState.TargetTimeUserProvided)]
    [InlineData(PrescriptionPaceSource.TargetGoal, TargetFinishTimeSource.ProductAverage, PreparationRunwayPaceEvidenceState.TargetTimeProductAverage)]
    [InlineData(PrescriptionPaceSource.EffortOnly, null, PreparationRunwayPaceEvidenceState.MissingPaceEvidence)]
    internal void ContextAdapter_ReusesResolvedCoreSource_AndPreservesEvidenceDistinctions(
        PrescriptionPaceSource source, TargetFinishTimeSource? targetSource, PreparationRunwayPaceEvidenceState expected)
    {
        var raw = source == PrescriptionPaceSource.RecentRace ? "RECENT_RACE" : source == PrescriptionPaceSource.TargetGoal ? "TARGET_TIME" : "NONE";
        var resolved = Resolved(source, raw);
        var context = PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext(
            "TEN_K__4D__INTERMEDIATE", 10, resolved, targetSource, "authoritative fixture");

        Assert.Equal(expected, context.EvidenceState);
        Assert.Same(resolved, context.ResolvedPaceSource);
        Assert.Equal(targetSource, context.TargetFinishTimeSource);
    }

    [Theory]
    [InlineData(PaceFixture.RecentTenK)]
    [InlineData(PaceFixture.RecentFiveK)]
    [InlineData(PaceFixture.UserTarget)]
    [InlineData(PaceFixture.ProductAverage)]
    [InlineData(PaceFixture.Missing)]
    [InlineData(PaceFixture.SlowConservativeTarget)]
    [InlineData(PaceFixture.MissingVolumeEvidence)]
    [InlineData(PaceFixture.RoundingEdge)]
    internal void RepresentativeAuthoritativePaceFixtures_UseControlledEffortWithoutNumericSynthesis(PaceFixture fixture)
    {
        var context = fixture switch
        {
            PaceFixture.RecentTenK or PaceFixture.RecentFiveK or PaceFixture.MissingVolumeEvidence => RecentRaceContext(),
            PaceFixture.UserTarget or PaceFixture.SlowConservativeTarget or PaceFixture.RoundingEdge => TargetContext(TargetFinishTimeSource.UserDefined),
            PaceFixture.ProductAverage => TargetContext(TargetFinishTimeSource.ProductAverage),
            _ => MissingContext(),
        };
        var result = PreparationRunwayPaceMaterializer.Materialize(Request(PreparationRunwayAllocationProfile.CoreEntryReady, 8, context));

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.All(result.PacedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots), s =>
        {
            Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, s.PacePrescription.Kind);
            Assert.Null(s.PacePrescription.SecondsPerKilometer);
            Assert.Null(s.PacePrescription.FasterBoundSecondsPerKilometer);
            Assert.Null(s.PacePrescription.SlowerBoundSecondsPerKilometer);
        });
        if (fixture == PaceFixture.ProductAverage)
            Assert.All(result.PacedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots), s =>
                Assert.Equal(TargetFinishTimeSource.ProductAverage, s.PaceProvenance.TargetFinishTimeSource));
    }

    [Fact]
    public void AerobicStrengthIntroAndProgressed_UseCatalogControlledEfforts_ProgressionIsStructural()
    {
        var result = PreparationRunwayPaceMaterializer.Materialize(
            Request(PreparationRunwayAllocationProfile.CoreEntryReady, 8, RecentRaceContext()));
        Assert.True(result.IsSuccess, result.FailureReason);
        var slots = result.PacedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots).ToArray();
        Assert.Equal("CONTROLLED_AEROBIC_POWER_INTRO", Assert.Single(slots,
            s => s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutId == "AEROBIC_STRENGTH_CONTROLLED_INTRO").PacePrescription.EffortLabel);
        Assert.Equal("CONTROLLED_AEROBIC_POWER_PROGRESSED", Assert.Single(slots,
            s => s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutId == "AEROBIC_STRENGTH_CONTROLLED_PROGRESSED").PacePrescription.EffortLabel);
        Assert.All(slots.Where(s => s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutId.StartsWith("AEROBIC_STRENGTH", StringComparison.Ordinal)),
            s => Assert.Contains("Catalog-authored", s.PacePrescription.DerivationTrace, StringComparison.Ordinal));
    }

    [Fact]
    public void CoreWeekOneAdapter_ReadsExistingPrescribedOutputWithoutRecalculation()
    {
        var date = new DateOnly(2026, 10, 5);
        var sessions = new[]
        {
            CoreSession("KEY_SESSION", date, "EASY_STANDARD", 5, "EASY"),
            CoreSession("EASY_SUPPORT", date.AddDays(2), "EASY_STANDARD", 5, "EASY"),
            CoreSession("EASY_SUPPORT", date.AddDays(4), "EASY_STANDARD", 5, "EASY"),
            CoreSession("LONG_RUN", date.AddDays(6), "LONG_RUN_STANDARD", 5, "LONG_RUN_EASY_CONTROLLED"),
        };
        var week = new CatalogPrescribedWeek
        {
            WeekNumber = 1, PhaseKey = "FOUNDATION", PlannedWeeklyVolumeKm = 24, AccountedWeeklyDistanceKm = 24,
            AllocationTrace = new SessionVolumeAllocationTrace(1, 24, 8, 16, 6, 5, 5, "V1", 1), Sessions = sessions,
        };
        var plan = new CatalogPrescribedPlan
        {
            CandidateKey = "TEN_K__4D__INTERMEDIATE", CandidateVersion = 10,
            Weeks = [week], Sessions = sessions, ValidationResult = new CatalogSessionPrescriptionValidationResult(true, []),
        };

        var target = PreparationRunwayCoreWeekOnePaceAdapter.FromAuthoritativeCoreBehavior(plan);

        Assert.Equal(4, target.OrderedSlots.Count);
        Assert.Equal("EASY", target.OrderedSlots.Single(s => s.Role == PreparationRunwaySlotRole.KeySession).PacePrescription.EffortLabel);
        Assert.Equal("LONG_RUN_EASY_CONTROLLED", target.OrderedSlots.Single(s => s.Role == PreparationRunwaySlotRole.LongRun).PacePrescription.EffortLabel);
        Assert.All(target.OrderedSlots, s => Assert.Equal("CORE_SOURCE", s.PaceSourceProvenance));
    }

    [Fact]
    public void UnsupportedResolvedSource_FailsAtomically()
    {
        var context = PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext(
            "TEN_K__4D__INTERMEDIATE", 10,
            Resolved(PrescriptionPaceSource.Unresolved, "TARGET_TIME", RuntimeConditionResolutionStatus.NotEvaluated),
            TargetFinishTimeSource.UserDefined, "unsupported user target");
        AssertFailure(PreparationRunwayPaceMaterializer.Materialize(
            Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3, context)),
            PreparationRunwayPaceMaterializationFailureCode.PaceSourceUnsupported);
    }

    // A target time was requested but GOAL_FEASIBILITY_IN resolved
    // UNSUPPORTED because CoreEntryReadiness is NOT_READY -- exactly the
    // population the ConsistencyNeeded runway profile exists to serve. The
    // authoritative Core pace source is legitimately Unresolved (no numeric
    // target-goal pace should be derived from an infeasible goal), but this
    // must still succeed with effort-only prescriptions, not fail atomically
    // like a genuinely broken/NotEvaluated source.
    [Fact]
    public void TargetTimeRequestedGoalInfeasible_CoreNotReady_SucceedsWithEffortOnlyPacing()
    {
        var context = PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext(
            "TEN_K__4D__INTERMEDIATE", 10,
            Resolved(PrescriptionPaceSource.Unresolved, "TARGET_TIME", RuntimeConditionResolutionStatus.Evaluated, goalFeasibilityValue: "UNSUPPORTED"),
            TargetFinishTimeSource.ProductAverage, "target time requested but goal feasibility unsupported (core entry not ready)");

        Assert.Equal(PreparationRunwayPaceEvidenceState.TargetTimeRequestedGoalInfeasible, context.EvidenceState);

        var result = PreparationRunwayPaceMaterializer.Materialize(
            Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3, context));

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.All(result.PacedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots), s =>
        {
            Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, s.PacePrescription.Kind);
            Assert.Null(s.PacePrescription.SecondsPerKilometer);
        });
    }

    [Fact]
    public void FastTarget_DoesNotCreateRaceSpecificPace()
    {
        var result = PreparationRunwayPaceMaterializer.Materialize(
            Request(PreparationRunwayAllocationProfile.CoreEntryReady, 8, TargetContext(TargetFinishTimeSource.ProductAverage)));
        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.True(result.ContinuityAnalysis!.NoGoalPace);
        Assert.True(result.ContinuityAnalysis.NoRaceSpecificPace);
    }

    [Fact]
    public void MissingWorkoutRule_FailsAtomically()
    {
        var request = Request(PreparationRunwayAllocationProfile.CoreEntryReady, 8, RecentRaceContext());
        request = request with { Policy = request.Policy with { WorkoutRules = request.Policy.WorkoutRules.Where(r => r.WorkoutId != "AEROBIC_STRENGTH_CONTROLLED_INTRO").ToArray() } };
        AssertFailure(PreparationRunwayPaceMaterializer.Materialize(request), PreparationRunwayPaceMaterializationFailureCode.WorkoutPacePolicyMissing);
    }

    [Fact]
    public void ForbiddenGoalOrThresholdRule_FailsTyped()
    {
        var request = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3, RecentRaceContext());
        var rules = request.Policy.WorkoutRules.Select(r => r.WorkoutId == "EASY_STANDARD" ? r with { EffortLabel = "GOAL_PACE" } : r).ToArray();
        AssertFailure(PreparationRunwayPaceMaterializer.Materialize(request with { Policy = request.Policy with { WorkoutRules = rules } }),
            PreparationRunwayPaceMaterializationFailureCode.GoalPaceNotAllowedInRunway);
        rules = request.Policy.WorkoutRules.Select(r => r.WorkoutId == "EASY_STANDARD" ? r with { EffortLabel = "THRESHOLD_EFFORT" } : r).ToArray();
        AssertFailure(PreparationRunwayPaceMaterializer.Materialize(request with { Policy = request.Policy with { WorkoutRules = rules } }),
            PreparationRunwayPaceMaterializationFailureCode.ThresholdPaceNotAllowedInRunway);
    }

    [Fact]
    public void CoreWeekOneMismatch_FailsContinuityWithoutPartialOutput()
    {
        var request = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3, RecentRaceContext());
        var target = request.CoreWeekOnePaceTarget with
        {
            OrderedSlots = request.CoreWeekOnePaceTarget.OrderedSlots.Select(s => s.Role == PreparationRunwaySlotRole.LongRun
                ? s with { PacePrescription = Effort("UNMATCHED_LONG_RUN") }
                : s).ToArray(),
        };
        AssertFailure(PreparationRunwayPaceMaterializer.Materialize(request with { CoreWeekOnePaceTarget = target }),
            PreparationRunwayPaceMaterializationFailureCode.PaceContinuityViolation);
    }

    [Fact]
    public void RepeatedCallsAndPolicyInputOrder_AreValueIdentical()
    {
        var request = Request(PreparationRunwayAllocationProfile.CoreEntryReady, 8, RecentRaceContext());
        var first = PreparationRunwayPaceMaterializer.Materialize(request);
        var second = PreparationRunwayPaceMaterializer.Materialize(request);
        var reversed = PreparationRunwayPaceMaterializer.Materialize(request with
        {
            Policy = request.Policy with { WorkoutRules = request.Policy.WorkoutRules.Reverse().ToArray() },
        });
        Assert.Equal(Flatten(first), Flatten(second));
        Assert.Equal(Flatten(first), Flatten(reversed));
    }

    [Fact]
    public void SourceIsProductionOwnedDark_Unwired_NoResolverDuplication_NoPublicOrPersistenceReachability()
    {
        var repo = RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
        var relative = Path.Combine("backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayPacePrescription");
        var sourceDir = Path.Combine(repo, relative);
        var sources = Directory.GetFiles(sourceDir, "*.cs").Select(File.ReadAllText).ToArray();
        Assert.False(typeof(PreparationRunwayPaceMaterializer).IsPublic);
        Assert.All(sources, source =>
        {
            Assert.DoesNotContain("new PaceSourceResolver", source, StringComparison.Ordinal);
            Assert.DoesNotContain(".Resolve(new RuntimeResolverContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CatalogPublicPreviewMaterializer", source, StringComparison.Ordinal);
        });
        foreach (var productionPath in new[]
                 {
                     "backend/RunningApp.Api", "backend/RunningApp.Persistence",
                     "backend/RunningApp.Application/RuntimeCatalog/PreviewRouting",
                 })
        {
            var root = Path.Combine(repo, productionPath.Replace('/', Path.DirectorySeparatorChar));
            var hits = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Where(p => File.ReadAllText(p).Contains(nameof(PreparationRunwayPaceMaterializer), StringComparison.Ordinal));
            Assert.Empty(hits);
        }
    }

    private static PreparationRunwayPaceMaterializationRequest<PreparationRunwayBlockType> Request(
        PreparationRunwayAllocationProfile profile, int weeks, PreparationRunwayPaceContext context) =>
        new(Calendar(profile, weeks), context, CoreTarget(), TenKPreparationRunwayPacePolicyFactory.Build());

    private static PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> Calendar(
        PreparationRunwayAllocationProfile profile, int count)
    {
        var start = new DateOnly(2026, 8, 3);
        var segment = new PreparationRunwaySegmentProvenance(
            PreparationRunwaySegmentType.PreparationRunway, 1, start, start.AddDays(count * 7 - 1), 1, count,
            "TEST_CALENDAR", 1, "TEN_K__4D__INTERMEDIATE", 10, profile.ToString());
        var weeks = Enumerable.Range(1, count).Select(weekNumber => DatedWeek(profile, count, weekNumber, start, segment)).ToArray();
        return new PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType>(
            true, null, weeks, [], null, [segment], null, null, null, ["authoritative dated fixture"]);
    }

    private static PreparationRunwayDatedWeek<PreparationRunwayBlockType> DatedWeek(
        PreparationRunwayAllocationProfile profile, int total, int number, DateOnly planStart, PreparationRunwaySegmentProvenance segment)
    {
        var isAerobicStrength = profile == PreparationRunwayAllocationProfile.CoreEntryReady &&
                                (number == total - 1 || total >= 5 && number == total - 2);
        var block = number == total ? PreparationRunwayBlockType.PreSpecificTransition :
            isAerobicStrength ? PreparationRunwayBlockType.AerobicStrength :
            profile == PreparationRunwayAllocationProfile.ConsistencyNeeded && number <= 2 ? PreparationRunwayBlockType.Consistency :
            PreparationRunwayBlockType.GeneralEndurance;
        var blockStep = block == PreparationRunwayBlockType.AerobicStrength && total >= 5 && number == total - 1 ? 2 : 1;
        var key = block == PreparationRunwayBlockType.AerobicStrength
            ? blockStep == 1 ? ("AEROBIC_STRENGTH_CONTROLLED_INTRO", 1) : ("AEROBIC_STRENGTH_CONTROLLED_PROGRESSED", 1)
            : ("EASY_STANDARD", 5);
        var structuralSlots = new[]
        {
            Structural(block, blockStep, 1, PreparationRunwaySlotRole.KeySession, 1, key.Item1, key.Item2, 6d),
            Structural(block, blockStep, 2, PreparationRunwaySlotRole.EasySupport, 1, "EASY_STANDARD", 5, 5d),
            Structural(block, blockStep, 3, PreparationRunwaySlotRole.EasySupport, 2, "EASY_STANDARD", 5, 5d),
            Structural(block, blockStep, 4, PreparationRunwaySlotRole.LongRun, 1, "LONG_RUN_STANDARD", 5, 8d),
        };
        var materialized = new PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>(
            number, block, 1, "TEST_PROGRESSION", 1, blockStep, (int)block + 1,
            structuralSlots.Select(s => s.StructuralSlot).ToArray(),
            new PreparationRunwayMaterializedWeekProvenance(profile.ToString(), "TEN_K__4D__INTERMEDIATE", 10,
                "TEST_ALLOCATION", 1, "TEST_SUPPORT", 1, new PlanCatalogReference("RUN_LAYOUT_4D", 2)));
        var numeric = new PreparationRunwayPrescribedWeek<PreparationRunwayBlockType>(
            materialized, 24, 8, structuralSlots,
            new PreparationRunwayNumericDecisionTrace(number, block.ToString(), 24, 24, 24, 0, 0, 8, 8, 1d / 3d,
                "test", "test", "none", ["test"]));
        var weekStart = planStart.AddDays((number - 1) * 7);
        var dates = new[] { weekStart, weekStart.AddDays(2), weekStart.AddDays(4), weekStart.AddDays(6) };
        var dated = structuralSlots.Select((slot, index) => new PreparationRunwayDatedSlot<PreparationRunwayBlockType>(
            slot, dates[index], dates[index].DayOfWeek,
            new CatalogSessionCalendarProvenance($"SLOT_{index + 1}", slot.StructuralSlot.SlotRole.ToString(), dates[index].DayOfWeek, dates[index], "test"))).ToArray();
        return new PreparationRunwayDatedWeek<PreparationRunwayBlockType>(
            number, number, weekStart, weekStart.AddDays(6), numeric, dated, dated.OrderBy(s => s.SessionDate).ToArray(), segment);
    }

    private static PreparationRunwayPrescribedSlot<PreparationRunwayBlockType> Structural(
        PreparationRunwayBlockType block, int step, int ordinal, PreparationRunwaySlotRole role, int roleOrdinal,
        string workout, int version, double distance) => new(
        new PreparationRunwayMaterializedWorkoutSlot<PreparationRunwayBlockType>(
            role, ordinal, roleOrdinal, workout, version, block, "TEST_PROGRESSION", 1, step,
            role == PreparationRunwaySlotRole.KeySession ? PreparationRunwayWorkoutSlotSource.Anchor : PreparationRunwayWorkoutSlotSource.SupportPolicy,
            "TEST_MATERIALIZATION", 1),
        distance, PreparationRunwayQuantityUnit.Kilometers, "test numeric provenance");

    private static PreparationRunwayCoreWeekOnePaceTarget CoreTarget() => new(
        "TEN_K__4D__INTERMEDIATE", 10,
        [
            new(PreparationRunwaySlotRole.KeySession, 1, "EASY_STANDARD", 5, Effort("EASY"), "Core planner"),
            new(PreparationRunwaySlotRole.EasySupport, 1, "EASY_STANDARD", 5, Effort("EASY"), "Core planner"),
            new(PreparationRunwaySlotRole.EasySupport, 2, "EASY_STANDARD", 5, Effort("EASY"), "Core planner"),
            new(PreparationRunwaySlotRole.LongRun, 1, "LONG_RUN_STANDARD", 5, Effort("LONG_RUN_EASY_CONTROLLED"), "Core planner"),
        ], "authoritative Core Foundation Week 1 fixture matching existing planner output");

    private static CatalogPacePrescription Effort(string label) => new(
        CatalogPacePrescriptionKind.EffortOnly, null, null, null, CatalogPaceSourceSelection.EffortOnly,
        "NUMERIC_PACE_UNRESOLVED", label, "existing Core effort-only behavior");

    private static CatalogPrescribedSession CoreSession(
        string role, DateOnly date, string workout, int version, string effort)
    {
        var pace = Effort(effort);
        var prescription = new CatalogWorkoutPrescription
        {
            PrescriptionMode = CatalogPrescriptionMode.Distance,
            DistanceAccountingMode = CatalogDistanceAccountingMode.ExactSessionTotal,
            DistancePrescription = new CatalogDistancePrescription(role == "LONG_RUN" ? 8 : role == "KEY_SESSION" ? 6 : 5, "ExactSessionTotal", "nearest_0.5km"),
            DurationPrescription = new CatalogDurationPrescription(CatalogDurationKind.Unresolved, null, "effort_only"),
            PacePrescription = pace,
            EffortGuidance = effort,
            OrderedSegments = [new CatalogPrescriptionSegment(1, "SESSION_TOTAL", effort, role == "LONG_RUN" ? 8 : role == "KEY_SESSION" ? 6 : 5, null, pace, true)],
            Status = CatalogSessionPrescriptionStatus.Complete,
        };
        return new CatalogPrescribedSession
        {
            WeekNumber = 1, Date = date, PhaseKey = "FOUNDATION", ProgressionStageKey = "FOUNDATION_EASY_BASE",
            StructuralRole = role, WorkoutDefinitionKey = workout, WorkoutDefinitionVersion = version,
            PlannedDistanceKm = role == "LONG_RUN" ? 8 : role == "KEY_SESSION" ? 6 : 5,
            Prescription = prescription, BindingProvenance = "CORE_BINDING", PaceSourceProvenance = "CORE_SOURCE",
            VolumeAllocationProvenance = "CORE_VOLUME",
            DecisionTrace = new SessionPrescriptionDecisionTrace(1, date, workout, 24, 8, 16,
                role == "LONG_RUN" ? 8 : role == "KEY_SESSION" ? 6 : 5, "V1", "Distance", "V1", "NONE",
                "NOT_EVALUATED", CatalogPaceSourceSelection.EffortOnly, [], "nearest_0.5km", "ExactSessionTotal", "Unresolved", null, []),
            ValidationResult = new CatalogSessionPrescriptionValidationResult(true, []),
        };
    }

    private static PreparationRunwayPaceContext RecentRaceContext() => PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext(
        "TEN_K__4D__INTERMEDIATE", 10, Resolved(PrescriptionPaceSource.RecentRace, "RECENT_RACE"), null,
        "complete recent-race evidence; distance may be 5K or 10K; no projection performed here");

    private static PreparationRunwayPaceContext TargetContext(TargetFinishTimeSource source) => PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext(
        "TEN_K__4D__INTERMEDIATE", 10, Resolved(PrescriptionPaceSource.TargetGoal, "TARGET_TIME"), source,
        source == TargetFinishTimeSource.ProductAverage ? "product-average target, not athlete evidence" : "user target with approved resolved feasibility");

    private static PreparationRunwayPaceContext MissingContext() => PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext(
        "TEN_K__4D__INTERMEDIATE", 10, Resolved(PrescriptionPaceSource.EffortOnly, "NONE"), null,
        "no independent pace evidence; controlled effort only");

    private static ResolvedPrescriptionPaceSource Resolved(
        PrescriptionPaceSource source, string raw, RuntimeConditionResolutionStatus status = RuntimeConditionResolutionStatus.Evaluated,
        string? goalFeasibilityValue = null) =>
        new(source, PaceSourceResolver.ConditionTypeValue, status, raw,
            goalFeasibilityValue ?? (source == PrescriptionPaceSource.TargetGoal ? "REALISTIC" : "NOT_EVALUATED"), "authoritative_resolver_fixture");

    private static string Flatten(PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> result) =>
        string.Join('|', result.PacedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots).Select(s =>
            $"{s.OriginalSlot.SessionDate:yyyy-MM-dd}:{s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutId}:{s.PacePrescription.Kind}:{s.PacePrescription.EffortLabel}:{s.PaceProvenance.EvidenceState}"));

    private static void AssertFailure(
        PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> result,
        PreparationRunwayPaceMaterializationFailureCode code)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.FailureCode);
        Assert.Null(result.PacedRunwayWeeks);
        Assert.Null(result.UnchangedCoreWeekOneTarget);
        Assert.Null(result.ContinuityAnalysis);
    }

    internal enum PaceFixture
    {
        RecentTenK,
        RecentFiveK,
        UserTarget,
        ProductAverage,
        Missing,
        SlowConservativeTarget,
        MissingVolumeEvidence,
        RoundingEdge,
    }
}
