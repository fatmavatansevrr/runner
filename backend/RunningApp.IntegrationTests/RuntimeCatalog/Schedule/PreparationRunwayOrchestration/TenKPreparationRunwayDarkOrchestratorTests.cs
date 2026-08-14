using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

public sealed class TenKPreparationRunwayDarkOrchestratorTests
{
    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static readonly Task<PlanCatalogCandidateSummary> CandidateTask = LoadCandidateAsync();
    private static readonly IReadOnlyList<DayOfWeek> MonWedFriSun =
        [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday];

    public static IEnumerable<object[]> HorizonProfileMatrix =>
        Enumerable.Range(15, 6).SelectMany(weeks => new[] { "READY", "NOT_READY" }
            .Select(readiness => new object[] { weeks, readiness }));

    [Theory]
    [MemberData(nameof(HorizonProfileMatrix))]
    public async Task OrchestrateAsync_All15To20Horizons_BothProfiles_EndToEndSuccess(int totalWeeks, string readinessValue)
    {
        var request = await RequestAsync(totalWeeks, 0, readinessValue, PaceEvidence.RecentRace, MonWedFriSun, DayOfWeek.Sunday);
        var result = await Orchestrator().OrchestrateAsync(request);

        AssertSuccess(result, totalWeeks, readinessValue);
    }

    [Theory]
    [InlineData(15, 0)]
    [InlineData(16, 1)]
    [InlineData(17, 2)]
    [InlineData(18, 3)]
    [InlineData(19, 4)]
    [InlineData(20, 5)]
    [InlineData(20, 6)]
    public async Task LeadingPartialDays_ZeroThroughSix_AreAlignmentOnly(int totalWeeks, int remainder)
    {
        var request = await RequestAsync(totalWeeks, remainder, remainder % 2 == 0 ? "READY" : "NOT_READY",
            PaceEvidence.RecentRace, MonWedFriSun, DayOfWeek.Sunday, new DateOnly(2026, 8, 5));
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, result.Failure?.Reason);
        Assert.Equal(remainder, result.HorizonDecision!.LeadingPartialDays);
        Assert.True(result.FinalInvariants!.NoAlignmentRemainderSessions);
        Assert.All(result.CalendarComposition!.DatedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots),
            s => Assert.True(s.SessionDate >= request.StartDate.AddDays(remainder)));
    }

    [Theory]
    [InlineData("READY", PaceEvidence.RecentRace)]
    [InlineData("READY", PaceEvidence.UserTarget)]
    [InlineData("READY", PaceEvidence.ProductAverage)]
    [InlineData("READY", PaceEvidence.Missing)]
    [InlineData("NOT_READY", PaceEvidence.RecentRace)]
    [InlineData("NOT_READY_MISSING", PaceEvidence.Missing)]
    [InlineData("NOT_READY_NO_BASE", PaceEvidence.Missing)]
    internal async Task EvidenceAndPaceSourceMatrix_UsesOneSharedCoreContext(string readiness, PaceEvidence pace)
    {
        var request = await RequestAsync(18, 2, readiness, pace, MonWedFriSun, DayOfWeek.Sunday);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, result.Failure?.Reason);
        Assert.True(result.FinalInvariants!.ProvenanceComplete);
        Assert.All(result.PacedRunway!.PacedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots), slot =>
        {
            Assert.Equal(pace == PaceEvidence.ProductAverage ? TargetFinishTimeSource.ProductAverage :
                pace == PaceEvidence.UserTarget ? TargetFinishTimeSource.UserDefined : null,
                slot.PaceProvenance.TargetFinishTimeSource);
            Assert.NotEqual(CatalogPaceSourceSelection.TargetGoalDerived, slot.PacePrescription.Source);
        });
    }

    [Theory]
    [InlineData(2026, 8, 5, PreferredLayout.MonWedFriSun)]
    [InlineData(2026, 8, 29, PreferredLayout.TueThuSatSun)]
    [InlineData(2026, 12, 30, PreferredLayout.MonWedFriSun)]
    internal async Task CalendarLayouts_MidweekMonthAndYearCrossings_PreservePreferences(
        int year, int month, int day, PreferredLayout layout)
    {
        var preferred = layout == PreferredLayout.MonWedFriSun
            ? MonWedFriSun
            : new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var request = await RequestAsync(17, 3, "READY", PaceEvidence.ProductAverage,
            preferred, DayOfWeek.Sunday, new DateOnly(year, month, day));
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, result.Failure?.Reason);
        Assert.True(result.FinalInvariants!.PreferredDaysValid);
        Assert.True(result.FinalInvariants.LongRunDayValid);
        Assert.All(result.CalendarComposition!.DatedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots),
            s => Assert.Contains(s.SessionDayOfWeek, preferred));
    }

    [Fact]
    public async Task RepeatedCallsAndInputCollectionOrder_AreValueIdentical()
    {
        var request = await RequestAsync(20, 4, "READY", PaceEvidence.RecentRace, MonWedFriSun, DayOfWeek.Sunday);
        var first = await Orchestrator().OrchestrateAsync(request);
        var second = await Orchestrator().OrchestrateAsync(request);
        var reorderedRequest = await RequestAsync(20, 4, "READY", PaceEvidence.RecentRace,
            MonWedFriSun.Reverse().ToArray(), DayOfWeek.Sunday);
        var reordered = await Orchestrator().OrchestrateAsync(reorderedRequest with
            { ConditionResults = reorderedRequest.ConditionResults.Reverse().ToArray() });

        Assert.Equal(Flatten(first), Flatten(second));
        Assert.Equal(Flatten(first), Flatten(reordered));
        Assert.Equal(FlattenTrace(first), FlattenTrace(second));
    }

    [Theory]
    [InlineData(14)]
    [InlineData(21)]
    [InlineData(52)]
    [InlineData(53)]
    public async Task OutOfScopeHorizons_FailAtHorizon_WithNoPartialOutput(int totalWeeks)
    {
        var request = await RequestAsync(totalWeeks, 0, "READY", PaceEvidence.RecentRace, MonWedFriSun, DayOfWeek.Sunday);
        var result = await Orchestrator().OrchestrateAsync(request);

        AssertFailure(result, TenKPreparationRunwayOrchestrationStage.Horizon,
            TenKPreparationRunwayOrchestrationFailureCode.HorizonNotDirectRunway);
    }

    [Fact]
    public async Task ProgressionLoadFailure_PropagatesOriginalStageAndNoPartialOutput()
    {
        var request = await RequestAsync(15, 0, "READY", PaceEvidence.RecentRace, MonWedFriSun, DayOfWeek.Sunday);
        var result = await TestOrchestrator(progression: new ThrowingProgressionLoader()).OrchestrateAsync(request);
        AssertFailure(result, TenKPreparationRunwayOrchestrationStage.ProgressionLoading,
            TenKPreparationRunwayOrchestrationFailureCode.ProgressionLoadingFailed);
        Assert.Equal(nameof(InvalidOperationException), result.Failure!.OriginalFailureCode);
    }

    [Fact]
    public async Task BinderFailure_PropagatesOriginalTypedCodeAndNoPartialOutput()
    {
        var request = await RequestAsync(15, 0, "READY", PaceEvidence.RecentRace, MonWedFriSun, DayOfWeek.Sunday);
        var result = await TestOrchestrator(progression: new EmptyProgressionLoader()).OrchestrateAsync(request);
        AssertFailure(result, TenKPreparationRunwayOrchestrationStage.WorkoutBinding,
            TenKPreparationRunwayOrchestrationFailureCode.WorkoutBindingFailed);
        Assert.Equal(PreparationRunwayWorkoutBindingFailureCode.ProgressionCapacityExceeded.ToString(), result.Failure!.OriginalFailureCode);
    }

    [Theory]
    [InlineData(FailingStage.Core, TenKPreparationRunwayOrchestrationStage.CoreGeneration, TenKPreparationRunwayOrchestrationFailureCode.CoreGenerationFailed)]
    [InlineData(FailingStage.Numeric, TenKPreparationRunwayOrchestrationStage.NumericMaterialization, TenKPreparationRunwayOrchestrationFailureCode.NumericMaterializationFailed)]
    [InlineData(FailingStage.Calendar, TenKPreparationRunwayOrchestrationStage.CalendarComposition, TenKPreparationRunwayOrchestrationFailureCode.CalendarCompositionFailed)]
    [InlineData(FailingStage.Pace, TenKPreparationRunwayOrchestrationStage.PaceMaterialization, TenKPreparationRunwayOrchestrationFailureCode.PaceMaterializationFailed)]
    [InlineData(FailingStage.Final, TenKPreparationRunwayOrchestrationStage.FinalInvariantValidation, TenKPreparationRunwayOrchestrationFailureCode.FinalInvariantValidationFailed)]
    internal async Task DownstreamFailures_StopImmediatelyAndExposeNoPartialPlan(
        FailingStage failing, TenKPreparationRunwayOrchestrationStage expectedStage, TenKPreparationRunwayOrchestrationFailureCode expectedCode)
    {
        var request = await RequestAsync(15, 0, "READY", PaceEvidence.RecentRace, MonWedFriSun, DayOfWeek.Sunday);
        var result = await TestOrchestrator(
            core: failing == FailingStage.Core ? new ThrowingCoreGenerator() : null,
            numeric: failing == FailingStage.Numeric ? new FailingNumericStage() : null,
            calendar: failing == FailingStage.Calendar ? new FailingCalendarStage() : null,
            pace: failing == FailingStage.Pace ? new FailingPaceStage() : null,
            final: failing == FailingStage.Final ? new FailingFinalValidator() : null).OrchestrateAsync(request);
        AssertFailure(result, expectedStage, expectedCode);
    }

    [Fact]
    public async Task LiveCandidateBundleIdentityAndReferences_AreUnchangedByOrchestration()
    {
        var candidate = await CandidateTask;
        var before = string.Join('|', candidate.ReferencedWorkouts.Select(r => $"{r.Key}v{r.Version}"));
        var request = await RequestAsync(15, 0, "READY", PaceEvidence.RecentRace, MonWedFriSun, DayOfWeek.Sunday);
        var result = await Orchestrator().OrchestrateAsync(request);
        Assert.True(result.IsSuccess, result.Failure?.Reason);
        Assert.Equal(before, string.Join('|', candidate.ReferencedWorkouts.Select(r => $"{r.Key}v{r.Version}")));
        Assert.DoesNotContain(candidate.ReferencedWorkouts, r => r.Key.StartsWith("AEROBIC_STRENGTH_CONTROLLED", StringComparison.Ordinal));
    }

    [Fact]
    public void SourceIsProductionOwnedDark_NoPublicDtoEfControllerPreviewConfirmOrPersistenceReachability()
    {
        var repo = TestPlanServicesFactory.RepoRoot();
        var sourceDir = Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayOrchestration");
        var sources = Directory.GetFiles(sourceDir, "*.cs").Select(File.ReadAllText).ToArray();
        Assert.False(typeof(TenKPreparationRunwayDarkOrchestrator).IsPublic);
        Assert.All(sources, source =>
        {
            Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain("GeneratedCatalogPlanPayload", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CatalogPublicPreviewMaterializer", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IPlanGeneration", source, StringComparison.Ordinal);
        });

        // Phase 4G.6B: CatalogPreviewGenerator.cs is now the single,
        // intentional, scoped public entrypoint into this orchestrator (see
        // GeneratePreparationRunwayPreviewAsync) -- excluded here by name,
        // not by folder, so any OTHER file in PreviewRouting gaining a
        // reference still fails this test. RunningApp.Api and
        // RunningApp.Persistence (controllers, EF, confirm/persistence code)
        // remain fully forbidden -- the orchestrator is still reachable only
        // through the one typed, non-persistable preview path, never
        // directly from a controller or from EF.
        const string legitimatePublicEntrypoint = "CatalogPreviewGenerator.cs";
        foreach (var relative in new[]
                 {
                     "backend/RunningApp.Api", "backend/RunningApp.Persistence",
                     "backend/RunningApp.Application/RuntimeCatalog/PreviewRouting",
                     "backend/RunningApp.Application/Services",
                 })
        {
            var root = Path.Combine(repo, relative.Replace('/', Path.DirectorySeparatorChar));
            var hits = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Where(p => !Path.GetFileName(p).Equals(legitimatePublicEntrypoint, StringComparison.Ordinal))
                .Where(p => File.ReadAllText(p).Contains(nameof(TenKPreparationRunwayDarkOrchestrator), StringComparison.Ordinal));
            Assert.Empty(hits);
        }
    }

    private static TenKPreparationRunwayDarkOrchestrator Orchestrator() =>
        TenKPreparationRunwayDarkOrchestratorFactory.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });

    private static TenKPreparationRunwayDarkOrchestrator TestOrchestrator(
        ITenKPreparationRunwayProgressionLoader? progression = null,
        ITenKPreparationRunwayCoreGenerator? core = null,
        ITenKPreparationRunwayNumericStage? numeric = null,
        ITenKPreparationRunwayCalendarStage? calendar = null,
        ITenKPreparationRunwayPaceStage? pace = null,
        ITenKPreparationRunwayFinalInvariantValidator? final = null)
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });
        var workoutLoader = new CatalogWorkoutDefinitionLoader(options);
        var peakLoader = new CatalogPeakVolumeBandLoader(options);
        var corePipeline = new DynamicCoreCalendarMaterializationOrchestrator(
            new DynamicCoreSessionPrescriptionOrchestrator(
                new DynamicCoreVolumeAndLongRunOrchestrator(
                    new DynamicCoreWorkoutBindingOrchestrator(
                        new DynamicCoreWeekSkeletonOrchestrator(
                            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                        new CatalogWorkoutProgressionLoader(options), new ProgressionStageAllocator(),
                        new GeneratedCatalogStageScheduleValidator(), new CatalogWeekSkeletonCalendarMaterializer(),
                        new DatedGeneratedCatalogPlanSkeletonValidator(), new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                    new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
                new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));
        return new TenKPreparationRunwayDarkOrchestrator(
            workoutLoader,
            progression ?? new TenKPreparationRunwayProgressionLoader(CatalogRoot, workoutLoader),
            core ?? new TenKPreparationRunwayCoreGenerator(corePipeline, workoutLoader, peakLoader),
            numeric ?? new TenKPreparationRunwayNumericStage(),
            calendar ?? new TenKPreparationRunwayCalendarStage(new PreparationRunwayCalendarComposer(
                new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator())),
            pace ?? new TenKPreparationRunwayPaceStage(),
            final ?? new TenKPreparationRunwayFinalInvariantValidator());
    }

    private static async Task<PlanCatalogCandidateSummary> LoadCandidateAsync()
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    private static async Task<TenKPreparationRunwayDarkOrchestrationRequest> RequestAsync(
        int totalWeeks,
        int remainder,
        string readinessValue,
        PaceEvidence paceEvidence,
        IReadOnlyList<DayOfWeek> preferredDays,
        DayOfWeek longRunDay,
        DateOnly? startOverride = null)
    {
        var candidate = await CandidateTask;
        var start = startOverride ?? new DateOnly(2026, 8, 3);
        var race = start.AddDays(totalWeeks * 7 + remainder);
        var ready = readinessValue == "READY";
        var noBase = readinessValue == "NOT_READY_NO_BASE";
        double? weekly = noBase ? 0 : ready ? 24 : 12;
        double? longest = noBase ? 0 : ready ? 9 : 5;
        if (readinessValue == "NOT_READY_MISSING") { weekly = null; longest = null; }

        int? targetSeconds = paceEvidence == PaceEvidence.Missing ? null : paceEvidence == PaceEvidence.ProductAverage ? 3480 : 3000;
        var targetSource = paceEvidence switch
        {
            PaceEvidence.ProductAverage => TargetFinishTimeSource.ProductAverage,
            PaceEvidence.UserTarget => TargetFinishTimeSource.UserDefined,
            _ => (TargetFinishTimeSource?)null,
        };
        var recentRace = paceEvidence == PaceEvidence.RecentRace
            ? new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = start.AddDays(-21) }
            : null;
        var weekdays = preferredDays.Select(ToWeekday).ToArray();
        var preview = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
            DaysPerWeek = 4, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = targetSeconds, TargetFinishTimeSource = targetSource,
            PreferredDays = weekdays, LongRunDay = ToWeekday(longRunDay),
            RecentWeeklyVolumeKm = weekly, RecentLongestRunKm = longest, RecentRunsPerWeek = noBase ? 0 : 4,
            RecentRace = recentRace,
        };
        var resolver = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK, GoalDistanceKm = 10, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = targetSeconds, TargetFinishTimeSource = targetSource,
            DaysPerWeek = 4, PreferredDays = weekdays, LongRunDay = ToWeekday(longRunDay), Level = RunningBackground.Intermediate,
            RecentWeeklyVolumeKm = weekly, RecentLongestRunKm = longest, RecentRunsPerWeek = noBase ? 0 : 4,
            RecentRaceDistanceKm = recentRace is null ? null : 10,
            RecentRaceFinishTimeSeconds = recentRace?.FinishTimeSeconds,
            RecentRaceDate = recentRace?.RaceDate,
        };
        var readiness = RuntimeConditionResolutionResult.Evaluated(
            CoreEntryReadinessResolver.ConditionTypeValue,
            readinessValue.StartsWith("NOT_READY", StringComparison.Ordinal) ? "NOT_READY" : readinessValue,
            readinessValue == "READY" ? "CORE_ENTRY_READY" : "CORE_ENTRY_NOT_READY");
        var conditions = new List<RuntimeConditionResolutionResult> { readiness };
        conditions.Add(paceEvidence switch
        {
            PaceEvidence.RecentRace => RuntimeConditionResolutionResult.Evaluated(PaceSourceResolver.ConditionTypeValue, "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
            PaceEvidence.Missing => RuntimeConditionResolutionResult.Evaluated(PaceSourceResolver.ConditionTypeValue, "NONE", "NO_PACE_EVIDENCE_PROVIDED"),
            _ => RuntimeConditionResolutionResult.Evaluated(PaceSourceResolver.ConditionTypeValue, "TARGET_TIME", "TARGET_FINISH_TIME_PROVIDED"),
        });
        conditions.Add(paceEvidence switch
        {
            PaceEvidence.Missing => RuntimeConditionResolutionResult.NotEvaluated(GoalFeasibilityResolver.ConditionTypeValue, "NO_TARGET", resolver),
            PaceEvidence.ProductAverage => RuntimeConditionResolutionResult.Evaluated(GoalFeasibilityResolver.ConditionTypeValue, "CHALLENGING", "PACE_SOURCE_TARGET_TIME_PRODUCT_AVERAGE_ACCEPTED"),
            _ => RuntimeConditionResolutionResult.Evaluated(GoalFeasibilityResolver.ConditionTypeValue, "REALISTIC", "WITHIN_REALISTIC_BAND"),
        });
        return new TenKPreparationRunwayDarkOrchestrationRequest(
            candidate, start, race, start, preferredDays, longRunDay, readiness, conditions,
            preview, resolver, PreparationRunwayQuantityUnit.Kilometers);
    }

    private static void AssertSuccess(
        TenKPreparationRunwayDarkOrchestrationResult result, int totalWeeks, string readinessValue)
    {
        Assert.True(result.IsSuccess, result.Failure?.Reason);
        Assert.Null(result.Failure);
        Assert.Equal(totalWeeks, result.HorizonDecision!.AvailableFullWeeks);
        Assert.Equal(totalWeeks - 12, result.StructuralRunway!.Weeks!.Count);
        Assert.Equal(12, result.CoreResult!.PrescriptionResult.FinalPrescribedPlan.Weeks.Count);
        Assert.Equal(totalWeeks, result.CalendarComposition!.OrderedCombinedWeeks!.Count);
        Assert.Equal(totalWeeks * 4, result.FinalInvariants!.TotalSessions);
        Assert.True(result.FinalInvariants.IsValid);
        Assert.True(result.FinalInvariants.NumericContinuity);
        Assert.True(result.FinalInvariants.PaceContinuity);
        Assert.True(result.FinalInvariants.ProvenanceComplete);
        Assert.Equal(readinessValue == "READY" ? PreparationRunwayAllocationProfile.CoreEntryReady : PreparationRunwayAllocationProfile.ConsistencyNeeded, result.Profile);
        Assert.Equal(12, result.Trace.Select(t => t.Stage).Distinct().Count());
        Assert.Equal(TenKPreparationRunwayOrchestrationStage.Horizon, result.Trace.First().Stage);
        Assert.Equal(TenKPreparationRunwayOrchestrationStage.FinalInvariantValidation, result.Trace.Last().Stage);
        Assert.True(result.Trace.Select(t => (int)t.Stage).SequenceEqual(result.Trace.Select(t => (int)t.Stage).OrderBy(v => v)),
            "Conceptual stages must execute monotonically in the mandated order.");
    }

    private static void AssertFailure(
        TenKPreparationRunwayDarkOrchestrationResult result,
        TenKPreparationRunwayOrchestrationStage stage,
        TenKPreparationRunwayOrchestrationFailureCode code)
    {
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Failure);
        Assert.Equal(stage, result.Failure.Stage);
        Assert.Equal(code, result.Failure.Code);
        Assert.Null(result.HorizonDecision);
        Assert.Null(result.Profile);
        Assert.Null(result.AllocationResult);
        Assert.Null(result.LoadedProgressions);
        Assert.Null(result.Bindings);
        Assert.Null(result.StructuralRunway);
        Assert.Null(result.CoreResult);
        Assert.Null(result.NumericRunway);
        Assert.Null(result.CalendarComposition);
        Assert.Null(result.PacedRunway);
        Assert.Null(result.FinalInvariants);
        Assert.Equal(stage, result.Trace.Last().Stage);
        Assert.Equal("FAIL", result.Trace.Last().Outcome);
    }

    private static string Flatten(TenKPreparationRunwayDarkOrchestrationResult result) =>
        string.Join('|', result.PacedRunway!.PacedRunwayWeeks!.SelectMany(w => w.StructuralOrderedSlots).Select(s =>
            $"{s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutId}:{s.OriginalSlot.PrescribedSlot.PlannedDistanceKm}:{s.OriginalSlot.SessionDate:yyyy-MM-dd}:{s.PacePrescription.EffortLabel}")) +
        "||" + string.Join('|', result.CalendarComposition!.DatedCoreWeeks!.SelectMany(w => w.CoreWeek.SessionSlots).Select(s =>
            $"{s.StructuralRole}:{s.SessionDate:yyyy-MM-dd}"));

    private static string FlattenTrace(TenKPreparationRunwayDarkOrchestrationResult result) =>
        string.Join('|', result.Trace.Select(t =>
            $"{t.Sequence}:{t.Stage}:{t.Subject}:{t.SourceKey}:{t.SourceVersion}:{t.Outcome}:" +
            string.Join(',', t.Details.OrderBy(d => d.Key).Select(d => $"{d.Key}={d.Value}"))));

    private static Weekday ToWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekday.Mon,
        DayOfWeek.Tuesday => Weekday.Tue,
        DayOfWeek.Wednesday => Weekday.Wed,
        DayOfWeek.Thursday => Weekday.Thu,
        DayOfWeek.Friday => Weekday.Fri,
        DayOfWeek.Saturday => Weekday.Sat,
        DayOfWeek.Sunday => Weekday.Sun,
        _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };

    private sealed class ThrowingProgressionLoader : ITenKPreparationRunwayProgressionLoader
    {
        public Task<PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>> LoadValidatedAsync(
            PreparationRunwayBlockType block, PlanCatalogReference reference, CancellationToken ct) =>
            throw new InvalidOperationException("INJECTED_PROGRESSION_LOAD_FAILURE");
    }

    private sealed class EmptyProgressionLoader : ITenKPreparationRunwayProgressionLoader
    {
        public Task<PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>> LoadValidatedAsync(
            PreparationRunwayBlockType block, PlanCatalogReference reference, CancellationToken ct) =>
            Task.FromResult(new PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>(
                reference.Key, reference.Version, block, []));
    }

    private sealed class ThrowingCoreGenerator : ITenKPreparationRunwayCoreGenerator
    {
        public Task<DynamicCoreCalendarMaterializationResult> GenerateAsync(
            TenKPreparationRunwayCoreGenerationRequest request, CancellationToken ct) =>
            throw new InvalidOperationException("INJECTED_CORE_FAILURE");
    }

    private sealed class FailingNumericStage : ITenKPreparationRunwayNumericStage
    {
        public PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> Materialize(
            PreparationRunwayNumericMaterializationRequest<PreparationRunwayBlockType> request) =>
            PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType>.Failure(
                PreparationRunwayNumericMaterializationFailureCode.RunwayProgressionInfeasible,
                "INJECTED_NUMERIC_FAILURE", []);
    }

    private sealed class FailingCalendarStage : ITenKPreparationRunwayCalendarStage
    {
        public PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> Compose(
            PreparationRunwayCalendarCompositionRequest<PreparationRunwayBlockType> request) =>
            PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType>.Failure(
                PreparationRunwayCalendarCompositionFailureCode.RunwayCoreContinuityViolation,
                "INJECTED_CALENDAR_FAILURE", []);
    }

    private sealed class FailingPaceStage : ITenKPreparationRunwayPaceStage
    {
        public PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> Materialize(
            PreparationRunwayPaceMaterializationRequest<PreparationRunwayBlockType> request) =>
            PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType>.Failure(
                PreparationRunwayPaceMaterializationFailureCode.PaceContinuityViolation,
                "INJECTED_PACE_FAILURE", []);
    }

    private sealed class FailingFinalValidator : ITenKPreparationRunwayFinalInvariantValidator
    {
        private readonly TenKPreparationRunwayFinalInvariantValidator _inner = new();

        public TenKPreparationRunwayFinalInvariantReport Validate(
            TenKPreparationRunwayDarkOrchestrationRequest request,
            int runwayWeeks,
            PreparationRunwayAllocationEngineResult<PreparationRunwayBlockType> allocation,
            IReadOnlyList<TenKPreparationRunwayLoadedProgression> progressions,
            IReadOnlyList<TenKPreparationRunwayResolvedBinding> bindings,
            PreparationRunwayWeekMaterializationResult<PreparationRunwayBlockType> structural,
            DynamicCoreCalendarMaterializationResult core,
            PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> numeric,
            PreparationRunwayCalendarCompositionResult<PreparationRunwayBlockType> calendar,
            PreparationRunwayPaceMaterializationResult<PreparationRunwayBlockType> pace) =>
            _inner.Validate(request, runwayWeeks, allocation, progressions, bindings, structural, core, numeric, calendar, pace) with
            {
                IsValid = false,
                Findings = ["INJECTED_FINAL_INVARIANT_FAILURE"],
            };
    }

    internal enum PaceEvidence { RecentRace, UserTarget, ProductAverage, Missing }
    internal enum PreferredLayout { MonWedFriSun, TueThuSatSun }
    internal enum FailingStage { Core, Numeric, Calendar, Pace, Final }
}
