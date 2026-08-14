using System.Text.Json;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;

public sealed class PreparationRunwayCalendarComposerTests
{
    private static readonly DayOfWeek[] MonWedFriSun = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday];
    private static readonly DayOfWeek[] TueThuSatSun = [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public static IEnumerable<object[]> ProfileLengthMatrix()
    {
        foreach (PreparationRunwayAllocationProfile profile in Enum.GetValues<PreparationRunwayAllocationProfile>())
            foreach (var weeks in Enumerable.Range(3, 6))
                yield return [profile, weeks];
    }

    [Theory]
    [MemberData(nameof(ProfileLengthMatrix))]
    internal void BothProfiles_AllThreeToEightWeekRunways_ComposeWithExactCoreBoundary(
        PreparationRunwayAllocationProfile profile, int runwayWeeks)
    {
        var days = profile == PreparationRunwayAllocationProfile.ConsistencyNeeded ? MonWedFriSun : TueThuSatSun;
        var request = Request(profile, runwayWeeks, new DateOnly(2026, 7, 29), 0, days, DayOfWeek.Sunday);

        var result = Composer().Compose(request);

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(runwayWeeks, result.DatedRunwayWeeks!.Count);
        Assert.Equal(12, result.DatedCoreWeeks!.Count);
        Assert.Equal(runwayWeeks + 12, result.OrderedCombinedWeeks!.Count);
        Assert.Equal(Enumerable.Range(1, runwayWeeks + 12), result.OrderedCombinedWeeks.Select(w => w.GlobalWeekNumber));
        Assert.Equal(Enumerable.Range(1, runwayWeeks), result.DatedRunwayWeeks.Select(w => w.SegmentLocalWeekNumber));
        Assert.Equal(Enumerable.Range(1, 12), result.DatedCoreWeeks.Select(w => w.SegmentLocalWeekNumber));
        Assert.Equal(runwayWeeks + 1, result.DatedCoreWeeks[0].GlobalWeekNumber);
        Assert.Equal("FOUNDATION", result.DatedCoreWeeks[0].CoreWeek.PhaseKey);
        Assert.Equal(PreparationRunwayBlockType.PreSpecificTransition, result.DatedRunwayWeeks[^1].PrescribedWeek.StructuralWeek.BlockType);
        Assert.True(result.ContinuityAnalysis!.IsValid);
        Assert.True(result.ContinuityAnalysis.RunwayCoreWindowsContiguous);
        Assert.True(result.ContinuityAnalysis.CrossBoundarySpacingValid);
        Assert.True(result.ContinuityAnalysis.NumericBoundaryPreserved);
        AssertRunwayDates(result.DatedRunwayWeeks, days, DayOfWeek.Sunday);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void EveryLeadingPartialDayRemainder_IsAlignmentOnly(int remainder)
    {
        var start = new DateOnly(2026, 12, 10);
        var result = Composer().Compose(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 3, start, remainder,
            MonWedFriSun, DayOfWeek.Sunday));

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(remainder, result.DateAuthority!.HorizonDecision.LeadingPartialDays);
        Assert.Equal(start.AddDays(remainder), result.ContinuityAnalysis!.RunwayStartDate);
        Assert.Equal(3, result.DatedRunwayWeeks!.Count);
        Assert.Equal(15, result.OrderedCombinedWeeks!.Count);
        Assert.DoesNotContain(result.DatedRunwayWeeks.SelectMany(w => w.StructuralOrderedSlots),
            s => s.SessionDate < start.AddDays(remainder));
        Assert.Equal(result.ContinuityAnalysis.RaceDateBoundary, result.ContinuityAnalysis.FinalCoreEndDate.AddDays(1));
    }

    [Fact]
    public void MidWeekStart_MonthBoundary_AndYearBoundary_Work()
    {
        var fixtures = new[]
        {
            new DateOnly(2026, 7, 29),
            new DateOnly(2026, 1, 29),
            new DateOnly(2026, 12, 20),
        };
        foreach (var start in fixtures)
        {
            var result = Composer().Compose(Request(
                PreparationRunwayAllocationProfile.CoreEntryReady, 5, start, 3,
                TueThuSatSun, DayOfWeek.Sunday));
            Assert.True(result.IsSuccess, result.FailureReason);
            Assert.Equal(start.AddDays(3), result.DatedRunwayWeeks![0].StartDate);
            Assert.Equal(result.DatedRunwayWeeks[^1].EndDate.AddDays(1), result.DatedCoreWeeks![0].CoreWeek.StartDate);
        }
    }

    [Fact]
    public void StructuralOrderAndChronologicalOrder_AreBothPreserved()
    {
        var result = Composer().Compose(Request(
            PreparationRunwayAllocationProfile.CoreEntryReady, 5, new DateOnly(2026, 7, 29), 2,
            TueThuSatSun, DayOfWeek.Sunday));
        Assert.True(result.IsSuccess, result.FailureReason);
        foreach (var week in result.DatedRunwayWeeks!)
        {
            Assert.Equal(new[] { 1, 2, 3, 4 }, week.StructuralOrderedSlots.Select(s => s.PrescribedSlot.StructuralSlot.SlotOrdinal));
            Assert.Equal(week.ChronologicalSlots.Select(s => s.SessionDate).OrderBy(d => d), week.ChronologicalSlots.Select(s => s.SessionDate));
            Assert.Equal(week.PrescribedWeek.OrderedSlots.Select(s => s.PlannedDistanceKm), week.StructuralOrderedSlots.Select(s => s.PrescribedSlot.PlannedDistanceKm));
            Assert.Same(week.PrescribedWeek.StructuralWeek.Provenance, week.PrescribedWeek.StructuralWeek.Provenance);
        }
    }

    [Fact]
    public void InputOrderDoesNotChangeAssignedDates_AndRepeatedCallsAreValueIdentical()
    {
        var request = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 6,
            new DateOnly(2026, 7, 29), 4, MonWedFriSun, DayOfWeek.Sunday);
        var reordered = request with { PreferredDays = request.PreferredDays.Reverse().ToArray() };
        var first = Composer().Compose(request);
        var second = Composer().Compose(request);
        var reversed = Composer().Compose(reordered);

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.Equal(FlattenDates(first), FlattenDates(reversed));
    }

    [Theory]
    [MemberData(nameof(InvalidPreferredDays))]
    internal void InvalidPreferredDays_FailAtomically(DayOfWeek[] days)
    {
        var request = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            new DateOnly(2026, 7, 29), 0, days, DayOfWeek.Sunday);
        AssertFailure(Composer().Compose(request), PreparationRunwayCalendarCompositionFailureCode.PreferredDaysInvalid);
    }

    public static IEnumerable<object[]> InvalidPreferredDays()
    {
        yield return [new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Sunday }];
        yield return [new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday }];
        yield return [new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Wednesday, DayOfWeek.Sunday }];
    }

    [Fact]
    public void LongRunDayOutsidePreferredDays_FailsAtomically()
    {
        var request = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            new DateOnly(2026, 7, 29), 0, MonWedFriSun, DayOfWeek.Saturday);
        AssertFailure(Composer().Compose(request), PreparationRunwayCalendarCompositionFailureCode.LongRunDayNotPreferred);
    }

    [Fact]
    public void RunwayCountAndWrongCoreStartDate_FailWithDistinctCodes()
    {
        var mismatch = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            new DateOnly(2026, 7, 29), 0, MonWedFriSun, DayOfWeek.Sunday);
        var fourWeekAuthority = Authority(new DateOnly(2026, 7, 29), 4, 0);
        mismatch = mismatch with { DateAuthority = fourWeekAuthority };
        AssertFailure(Composer().Compose(mismatch), PreparationRunwayCalendarCompositionFailureCode.RunwayWeekCountMismatch);

        var wrongCore = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            new DateOnly(2026, 7, 29), 0, MonWedFriSun, DayOfWeek.Sunday);
        wrongCore = wrongCore with { UndatedCoreSkeleton = ShiftCoreSkeleton(wrongCore.UndatedCoreSkeleton, 1) };
        AssertFailure(Composer().Compose(wrongCore), PreparationRunwayCalendarCompositionFailureCode.CoreStartDateMismatch);
    }

    [Fact]
    public void MissingTransition_AndNumericMutation_FailAtomically()
    {
        var request = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            new DateOnly(2026, 7, 29), 0, MonWedFriSun, DayOfWeek.Sunday);
        var weeks = request.NumericRunway.PrescribedWeeks!.ToArray();
        var last = weeks[^1];
        weeks[^1] = last with { StructuralWeek = last.StructuralWeek with { BlockType = PreparationRunwayBlockType.GeneralEndurance } };
        var noTransition = request with { NumericRunway = request.NumericRunway with { PrescribedWeeks = weeks } };
        AssertFailure(Composer().Compose(noTransition), PreparationRunwayCalendarCompositionFailureCode.SegmentOrderInvalid);

        var mutatedWeeks = request.NumericRunway.PrescribedWeeks!.ToArray();
        var mutated = mutatedWeeks[0];
        mutatedWeeks[0] = mutated with { PlannedWeeklyVolumeKm = mutated.PlannedWeeklyVolumeKm + 0.5d };
        var numericMutation = request with { NumericRunway = request.NumericRunway with { PrescribedWeeks = mutatedWeeks } };
        AssertFailure(Composer().Compose(numericMutation), PreparationRunwayCalendarCompositionFailureCode.NumericPrescriptionChanged);
    }

    [Fact]
    public void BindingCount_CoreCount_AndMissingFoundation_FailAtomically()
    {
        var request = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            new DateOnly(2026, 7, 29), 0, MonWedFriSun, DayOfWeek.Sunday);

        var runwayWeeks = request.NumericRunway.PrescribedWeeks!.ToArray();
        runwayWeeks[0] = runwayWeeks[0] with { OrderedSlots = runwayWeeks[0].OrderedSlots.Take(3).ToArray() };
        AssertFailure(Composer().Compose(request with
        {
            NumericRunway = request.NumericRunway with { PrescribedWeeks = runwayWeeks },
        }), PreparationRunwayCalendarCompositionFailureCode.NumericPrescriptionChanged);

        AssertFailure(Composer().Compose(request with
        {
            UndatedCoreSkeleton = CloneCore(request.UndatedCoreSkeleton,
                request.UndatedCoreSkeleton.Weeks.Take(11).ToArray(), 11, request.UndatedCoreSkeleton.StartDate.AddDays(76)),
        }), PreparationRunwayCalendarCompositionFailureCode.CoreWeekCountMismatch);

        var coreWeeks = request.UndatedCoreSkeleton.Weeks.ToArray();
        coreWeeks[0] = CloneCoreWeek(coreWeeks[0], "BUILD");
        AssertFailure(Composer().Compose(request with
        {
            UndatedCoreSkeleton = CloneCore(request.UndatedCoreSkeleton, coreWeeks, 12, request.UndatedCoreSkeleton.EndDate),
        }), PreparationRunwayCalendarCompositionFailureCode.SegmentOrderInvalid);
    }

    [Fact]
    public void IncorrectLeadingPartialDays_FailsAuthorityValidation()
    {
        var request = Request(PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            new DateOnly(2026, 7, 29), 2, MonWedFriSun, DayOfWeek.Sunday);
        request = request with { DateAuthority = request.DateAuthority with { RunwayStartDate = request.DateAuthority.RunwayStartDate.AddDays(1) } };
        AssertFailure(Composer().Compose(request), PreparationRunwayCalendarCompositionFailureCode.LeadingPartialDayMismatch);
    }

    [Theory]
    [InlineData(Mutation.DuplicateDate, PreparationRunwayCalendarCompositionFailureCode.DuplicateWorkoutDate)]
    [InlineData(Mutation.OutsideWeek, PreparationRunwayCalendarCompositionFailureCode.WorkoutDateOutsideWeek)]
    [InlineData(Mutation.UnsafeSeparation, PreparationRunwayCalendarCompositionFailureCode.RoleDayAssignmentInvalid)]
    [InlineData(Mutation.CoreOverlap, PreparationRunwayCalendarCompositionFailureCode.RunwayCoreDateOverlap)]
    [InlineData(Mutation.CoreGap, PreparationRunwayCalendarCompositionFailureCode.RunwayCoreDateGap)]
    [InlineData(Mutation.GlobalNumber, PreparationRunwayCalendarCompositionFailureCode.GlobalWeekNumberInvalid)]
    internal void MutatedCalendarOutputs_FailAtomicallyWithTypedCodes(
        Mutation mutation, PreparationRunwayCalendarCompositionFailureCode expected)
    {
        var request = Request(PreparationRunwayAllocationProfile.CoreEntryReady, 5,
            new DateOnly(2026, 7, 29), 0, MonWedFriSun, DayOfWeek.Sunday);
        var materializer = new MutatingMaterializer(new CatalogWeekSkeletonCalendarMaterializer(), mutation);
        AssertFailure(Composer(materializer).Compose(request), expected);
    }

    [Fact]
    public void SourceIsProductionOwnedDarkAndDoesNotDuplicateHorizonArithmeticOrAddPacePersistence()
    {
        var repo = RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
        var sourceDir = Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayCalendarComposition");
        var sources = Directory.GetFiles(sourceDir, "*.cs").Select(File.ReadAllText).ToArray();
        Assert.False(typeof(PreparationRunwayCalendarComposer).IsPublic);
        Assert.All(sources, source =>
        {
            Assert.DoesNotContain("RaceDate.DayNumber -", source, StringComparison.Ordinal);
            Assert.DoesNotContain("-((", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetFinishTime", source, StringComparison.Ordinal);
            Assert.DoesNotContain("PacePrescription", source, StringComparison.Ordinal);
        });
        foreach (var relative in new[] { "backend/RunningApp.Api", "backend/RunningApp.Persistence", "backend/RunningApp.Application/RuntimeCatalog/PreviewRouting" })
        {
            var root = Path.Combine(repo, relative.Replace('/', Path.DirectorySeparatorChar));
            var hits = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Where(p => File.ReadAllText(p).Contains("PreparationRunwayCalendarComposer", StringComparison.Ordinal));
            Assert.Empty(hits);
        }
    }

    private static PreparationRunwayCalendarComposer Composer(ICatalogWeekSkeletonCalendarMaterializer? materializer = null) =>
        new(materializer ?? new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator());

    private static PreparationRunwayCalendarCompositionRequest<PreparationRunwayBlockType> Request(
        PreparationRunwayAllocationProfile profile,
        int runwayWeeks,
        DateOnly start,
        int remainder,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek? longRunDay)
    {
        var authority = Authority(start, runwayWeeks, remainder);
        var target = Target(24, 8);
        var numeric = Numeric(profile, runwayWeeks, target);
        return new PreparationRunwayCalendarCompositionRequest<PreparationRunwayBlockType>(
            authority, preferredDays, longRunDay, profile,
            "TEN_K__4D__INTERMEDIATE", 10, numeric, target,
            CoreSkeleton(authority.RunwayDateDecision.CoreStartDate),
            TenKPreparationRunwayCalendarCompositionPolicyFactory.Build());
    }

    private static PreparationRunwayCalendarAuthority Authority(DateOnly start, int runwayWeeks, int remainder)
    {
        var race = start.AddDays((runwayWeeks + 12) * 7 + remainder);
        var context = new CoreHorizonContext(start, race, 8, 12, 14);
        return PreparationRunwayCalendarAuthorityAdapter.Create(context, CoreHorizonClassifier.Classify(context));
    }

    private static PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> Numeric(
        PreparationRunwayAllocationProfile profile,
        int runwayWeeks,
        PreparationRunwayCoreWeekOneNumericTarget target)
    {
        var request = new PreparationRunwayNumericMaterializationRequest<PreparationRunwayBlockType>(
            profile,
            StructuralWeeks(profile, runwayWeeks),
            new PreparationRunwayStartingLoadEvidence(
                PreparationRunwayLoadEvidenceState.Provided, 24,
                PreparationRunwayLoadEvidenceState.Provided, 9, 4,
                "calendar-composition real numeric materializer fixture"),
            target,
            TenKPreparationRunwayNumericPolicyFactory.Build(),
            PreparationRunwayQuantityUnit.Kilometers);
        var result = PreparationRunwayNumericMaterializer.Materialize(request);
        Assert.True(result.IsSuccess, result.FailureReason);
        return result;
    }

    private static PreparationRunwayCoreWeekOneNumericTarget Target(double weekly, double longRun)
    {
        var allocation = FourDaySessionDistanceAllocationPolicy.Allocate(weekly, longRun);
        return new PreparationRunwayCoreWeekOneNumericTarget(
            "TEN_K__4D__INTERMEDIATE", 10, weekly, longRun,
            [
                new(PreparationRunwaySlotRole.KeySession, 1, allocation.KeySessionDistanceKm),
                new(PreparationRunwaySlotRole.EasySupport, 1, allocation.FirstEasySupportDistanceKm),
                new(PreparationRunwaySlotRole.EasySupport, 2, allocation.SecondEasySupportDistanceKm),
                new(PreparationRunwaySlotRole.LongRun, 1, allocation.LongRunDistanceKm),
            ], "authoritative Core target fixture", "existing Core planner/allocation behavior");
    }

    private static IReadOnlyList<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>> StructuralWeeks(
        PreparationRunwayAllocationProfile profile, int runwayWeeks)
    {
        var allocations = PreparationRunwayBlockAllocationEngine.Allocate(runwayWeeks, TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(profile));
        Assert.True(allocations.IsSuccess, allocations.FailureReason);
        var result = new List<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>>();
        var number = 1;
        foreach (var allocation in allocations.Allocations!.OrderBy(a => a.CanonicalOrder).Where(a => a.AllocatedWeeks > 0))
        {
            for (var local = 1; local <= allocation.AllocatedWeeks; local++)
            {
                result.Add(new PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>(
                    number++, allocation.BlockKey, local, $"TEN_K_{allocation.BlockKey.ToString().ToUpperInvariant()}_PROGRESSION", 1, local, allocation.CanonicalOrder,
                    [
                        Slot(allocation.BlockKey, local, PreparationRunwaySlotRole.KeySession, 1, 1),
                        Slot(allocation.BlockKey, local, PreparationRunwaySlotRole.EasySupport, 2, 1),
                        Slot(allocation.BlockKey, local, PreparationRunwaySlotRole.EasySupport, 3, 2),
                        Slot(allocation.BlockKey, local, PreparationRunwaySlotRole.LongRun, 4, 1),
                    ],
                    new PreparationRunwayMaterializedWeekProvenance(profile.ToString(), "TEN_K__4D__INTERMEDIATE", 10,
                        "TEN_K_PREPARATION_RUNWAY_ALLOCATION_POLICY", 1, "TEN_K_PREPARATION_RUNWAY_SUPPORT_WORKOUT_POLICY", 1,
                        new PlanCatalogReference("RUN_LAYOUT_4D", 2))));
            }
        }
        return result;
    }

    private static PreparationRunwayMaterializedWorkoutSlot<PreparationRunwayBlockType> Slot(
        PreparationRunwayBlockType block, int step, PreparationRunwaySlotRole role, int ordinal, int roleOrdinal) =>
        new(role, ordinal, roleOrdinal, role == PreparationRunwaySlotRole.LongRun ? "LONG_RUN_STANDARD" : "EASY_STANDARD", 5,
            block, $"TEN_K_{block.ToString().ToUpperInvariant()}_PROGRESSION", 1, step,
            PreparationRunwayWorkoutSlotSource.SupportPolicy, "TEN_K_PREPARATION_RUNWAY_SUPPORT_WORKOUT_POLICY", 1);

    private static GeneratedCatalogPlanSkeleton CoreSkeleton(DateOnly start)
    {
        var phases = Enumerable.Range(1, 12).Select(week => week switch
        {
            <= 3 => (Key: "FOUNDATION", Index: week, Count: 3),
            <= 7 => (Key: "BUILD", Index: week - 3, Count: 4),
            <= 11 => (Key: "RACE_SPECIFIC", Index: week - 7, Count: 4),
            _ => (Key: "TAPER", Index: 1, Count: 1),
        }).ToArray();
        var layout = new PlanCatalogReference("RUN_LAYOUT_4D", 2);
        var weeks = phases.Select((phase, index) => new GeneratedCatalogWeekSkeleton
        {
            WeekNumber = index + 1,
            StartDate = start.AddDays(index * 7),
            EndDate = start.AddDays(index * 7 + 6),
            StageKey = phase.Key,
            StageWeekIndex = phase.Index,
            StageWeekCount = phase.Count,
            SessionSlots = new[]
            {
                CoreSlot(1, "KEY_SESSION_1", "KEY_SESSION", phase.Key, layout),
                CoreSlot(2, "EASY_SUPPORT_1", "EASY_SUPPORT", phase.Key, layout),
                CoreSlot(3, "EASY_SUPPORT_2", "EASY_SUPPORT", phase.Key, layout),
                CoreSlot(4, "LONG_RUN_1", "LONG_RUN", phase.Key, layout),
            },
            Provenance = new GeneratedCatalogWeekSkeletonProvenance { StageKey = phase.Key, SourcePhaseKey = phase.Key },
        }).ToArray();
        var dependencies = new Dictionary<string, PlanCatalogReference> { ["RUN_LAYOUT"] = layout };
        return new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = 1, StartDate = start, EndDate = start.AddDays(83), PlannedWeekCount = 12, DaysPerWeek = 4,
            CanonicalDistanceFamily = "TEN_K", CandidateKey = "TEN_K__4D__INTERMEDIATE", CandidateVersion = 10,
            DependencyVersions = dependencies, Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = "TEN_K__4D__INTERMEDIATE", CandidateVersion = 10, DependencyVersions = dependencies,
                AsOfDate = start, MaterializerVersion = "TEST_EXISTING_CORE_SKELETON",
            },
        };
    }

    private static GeneratedCatalogSessionSlotSkeleton CoreSlot(
        int order, string key, string role, string phase, PlanCatalogReference layout) => new()
        {
            SlotOrderInWeek = order, LayoutSlotKey = key, StructuralRole = role,
            Provenance = new GeneratedCatalogSessionSlotSkeletonProvenance { SourceStageKey = phase, SourceLayout = layout },
        };

    private static GeneratedCatalogPlanSkeleton ShiftCoreSkeleton(GeneratedCatalogPlanSkeleton core, int days) => new()
    {
        SchemaVersion = core.SchemaVersion, StartDate = core.StartDate.AddDays(days), EndDate = core.EndDate.AddDays(days),
        PlannedWeekCount = core.PlannedWeekCount, DaysPerWeek = core.DaysPerWeek, CanonicalDistanceFamily = core.CanonicalDistanceFamily,
        CandidateKey = core.CandidateKey, CandidateVersion = core.CandidateVersion, DependencyVersions = core.DependencyVersions,
        Weeks = core.Weeks.Select(w => new GeneratedCatalogWeekSkeleton
        {
            WeekNumber = w.WeekNumber, StartDate = w.StartDate.AddDays(days), EndDate = w.EndDate.AddDays(days), StageKey = w.StageKey,
            StageWeekIndex = w.StageWeekIndex, StageWeekCount = w.StageWeekCount, SessionSlots = w.SessionSlots, Provenance = w.Provenance,
        }).ToArray(), Provenance = core.Provenance,
    };

    private static GeneratedCatalogPlanSkeleton CloneCore(
        GeneratedCatalogPlanSkeleton core,
        IReadOnlyList<GeneratedCatalogWeekSkeleton> weeks,
        int count,
        DateOnly end) => new()
    {
        SchemaVersion = core.SchemaVersion, StartDate = core.StartDate, EndDate = end,
        PlannedWeekCount = count, DaysPerWeek = core.DaysPerWeek,
        CanonicalDistanceFamily = core.CanonicalDistanceFamily, CandidateKey = core.CandidateKey,
        CandidateVersion = core.CandidateVersion, DependencyVersions = core.DependencyVersions,
        Weeks = weeks, Provenance = core.Provenance,
    };

    private static GeneratedCatalogWeekSkeleton CloneCoreWeek(GeneratedCatalogWeekSkeleton week, string phase) => new()
    {
        WeekNumber = week.WeekNumber, StartDate = week.StartDate, EndDate = week.EndDate,
        StageKey = phase, StageWeekIndex = week.StageWeekIndex, StageWeekCount = week.StageWeekCount,
        SessionSlots = week.SessionSlots,
        Provenance = new GeneratedCatalogWeekSkeletonProvenance { StageKey = phase, SourcePhaseKey = phase },
    };

    private static void AssertRunwayDates(
        IReadOnlyList<PreparationRunwayDatedWeek<PreparationRunwayBlockType>> weeks,
        IReadOnlyList<DayOfWeek> preferred,
        DayOfWeek longRunDay)
    {
        foreach (var week in weeks)
        {
            Assert.Equal(4, week.StructuralOrderedSlots.Count);
            Assert.Equal(4, week.StructuralOrderedSlots.Select(s => s.SessionDate).Distinct().Count());
            Assert.All(week.StructuralOrderedSlots, slot =>
            {
                Assert.Contains(slot.SessionDayOfWeek, preferred);
                Assert.InRange(slot.SessionDate, week.StartDate, week.EndDate);
                Assert.Equal(slot.PrescribedSlot.PlannedDistanceKm,
                    week.PrescribedWeek.OrderedSlots.Single(s => s.StructuralSlot.SlotOrdinal == slot.PrescribedSlot.StructuralSlot.SlotOrdinal).PlannedDistanceKm);
            });
            Assert.Equal(longRunDay, Assert.Single(week.StructuralOrderedSlots,
                s => s.PrescribedSlot.StructuralSlot.SlotRole == PreparationRunwaySlotRole.LongRun).SessionDayOfWeek);
        }
    }

    private static string FlattenDates(PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> result) =>
        string.Join('|', result.OrderedCombinedWeeks!.SelectMany(w => w.RunwayWeek?.StructuralOrderedSlots.Select(s => s.SessionDate.ToString("yyyy-MM-dd")) ??
            w.CoreWeek!.CoreWeek.SessionSlots.Select(s => s.SessionDate.ToString("yyyy-MM-dd"))));

    private static void AssertFailure(
        PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> result,
        PreparationRunwayCalendarCompositionFailureCode code)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.FailureCode);
        Assert.Null(result.DatedRunwayWeeks);
        Assert.Null(result.DatedCoreWeeks);
        Assert.Null(result.OrderedCombinedWeeks);
        Assert.Null(result.ContinuityAnalysis);
    }

    internal enum Mutation { DuplicateDate, OutsideWeek, UnsafeSeparation, CoreOverlap, CoreGap, GlobalNumber }

    private sealed class MutatingMaterializer : ICatalogWeekSkeletonCalendarMaterializer
    {
        private readonly ICatalogWeekSkeletonCalendarMaterializer _inner;
        private readonly Mutation _mutation;
        private int _calls;

        public MutatingMaterializer(ICatalogWeekSkeletonCalendarMaterializer inner, Mutation mutation)
        {
            _inner = inner;
            _mutation = mutation;
        }

        public DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context)
        {
            _calls++;
            var result = _inner.Materialize(context);
            var coreStartIndex = result.Weeks.Count - 12;
            if (_mutation is Mutation.CoreOverlap or Mutation.CoreGap)
            {
                var weeks = result.Weeks.ToArray();
                var delta = _mutation == Mutation.CoreOverlap ? -1 : 1;
                for (var i = coreStartIndex; i < weeks.Length; i++) weeks[i] = ShiftWeek(weeks[i], delta);
                return result with { Weeks = weeks };
            }
            if (_mutation == Mutation.GlobalNumber)
            {
                var weeks = result.Weeks.ToArray();
                var changed = weeks[coreStartIndex].WeekNumber + 1;
                weeks[coreStartIndex] = weeks[coreStartIndex] with
                {
                    WeekNumber = changed,
                    Provenance = weeks[coreStartIndex].Provenance with { WeekNumber = changed },
                };
                return result with { Weeks = weeks };
            }

            var runwayWeeks = result.Weeks.ToArray();
            var first = runwayWeeks[0];
            var slots = first.SessionSlots.ToArray();
            if (_mutation == Mutation.DuplicateDate)
                slots[1] = slots[1] with { SessionDate = slots[0].SessionDate, SessionDayOfWeek = slots[0].SessionDayOfWeek };
            if (_mutation == Mutation.OutsideWeek)
                slots[1] = slots[1] with { SessionDate = first.EndDate.AddDays(1), SessionDayOfWeek = first.EndDate.AddDays(1).DayOfWeek };
            if (_mutation == Mutation.UnsafeSeparation)
            {
                var longRun = slots.Single(s => s.StructuralRole == "LONG_RUN");
                var keyIndex = Array.FindIndex(slots, s => s.StructuralRole == "KEY_SESSION");
                var adjacent = longRun.SessionDate.AddDays(-1);
                slots[keyIndex] = slots[keyIndex] with { SessionDate = adjacent, SessionDayOfWeek = adjacent.DayOfWeek };
            }
            runwayWeeks[0] = first with { SessionSlots = slots };
            return result with { Weeks = runwayWeeks };
        }

        private static DatedGeneratedCatalogWeekSkeleton ShiftWeek(DatedGeneratedCatalogWeekSkeleton week, int days) => week with
        {
            StartDate = week.StartDate.AddDays(days),
            EndDate = week.EndDate.AddDays(days),
            SessionSlots = week.SessionSlots.Select(s => s with
            {
                SessionDate = s.SessionDate.AddDays(days),
                SessionDayOfWeek = s.SessionDate.AddDays(days).DayOfWeek,
            }).ToArray(),
        };
    }
}
