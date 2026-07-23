using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription;

public sealed class Phase4F7APrescriptionContextTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 15);

    [Fact]
    public void GoalDistanceResolver_DerivesTenKAndRejectsMismatches()
    {
        Assert.Equal(10d, CatalogGoalDistanceResolver.Resolve("TEN_K", GoalDistance.TenK));

        var unsupported = Assert.Throws<CatalogPrescriptionContractException>(() =>
            CatalogGoalDistanceResolver.Resolve("TEN_K", GoalDistance.HalfMarathon));
        Assert.Equal("UNSUPPORTED_REQUEST_GOAL_DISTANCE", unsupported.Code);

        var mismatch = Assert.Throws<CatalogPrescriptionContractException>(() =>
            CatalogGoalDistanceResolver.Resolve("TEN_K", GoalDistance.TenK, 5d));
        Assert.Equal("GOAL_DISTANCE_REQUEST_CATALOG_MISMATCH", mismatch.Code);
    }

    [Fact]
    public void CatalogPreviewGenerator_NoLongerContainsLiteralGoalDistanceHardcode()
    {
        var sourcePath = System.IO.Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "PreviewRouting", "CatalogPreviewGenerator.cs");
        var source = System.IO.File.ReadAllText(sourcePath);

        Assert.DoesNotContain("GoalDistanceKm = 10.0", source, StringComparison.Ordinal);
        Assert.Contains("CatalogGoalDistanceResolver.Resolve", source, StringComparison.Ordinal);
    }

    [Fact]
    public void InputNormalizer_ClassifiesFitnessEvidenceWithoutGeneratingDose()
    {
        var complete = CatalogPrescriptionInputNormalizer.Normalize(Request(r =>
        {
            r.RecentWeeklyVolumeKm = 32;
            r.RecentLongestRunKm = 11;
            r.RecentRunsPerWeek = 4;
            r.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = AsOfDate.AddDays(-14) };
        }), AsOfDate);

        Assert.Equal(PrescriptionInputState.Available, complete.WeeklyVolume.State);
        Assert.Equal(PrescriptionInputState.Available, complete.LongestRun.State);
        Assert.Equal(PrescriptionInputState.Available, complete.RecentRace.State);

        var missing = CatalogPrescriptionInputNormalizer.Normalize(Request(), AsOfDate);
        Assert.Equal(PrescriptionInputState.NotProvided, missing.WeeklyVolume.State);
        Assert.Equal(PrescriptionInputState.NotProvided, missing.LongestRun.State);
        Assert.Equal(PrescriptionInputState.NotProvided, missing.RecentRace.State);

        var zero = CatalogPrescriptionInputNormalizer.Normalize(Request(r =>
        {
            r.RecentWeeklyVolumeKm = 0;
            r.RecentLongestRunKm = 0;
        }), AsOfDate);
        Assert.Equal(PrescriptionInputState.Available, zero.WeeklyVolume.State);
        Assert.Equal(0, zero.WeeklyVolume.Kilometers);

        var inconsistent = CatalogPrescriptionInputNormalizer.Normalize(Request(r =>
        {
            r.RecentWeeklyVolumeKm = 20;
            r.RecentLongestRunKm = 25;
            r.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = AsOfDate.AddDays(1) };
        }), AsOfDate);

        Assert.Equal(PrescriptionInputState.Inconsistent, inconsistent.LongestRun.State);
        Assert.Equal(25, inconsistent.LongestRun.Kilometers);
        Assert.Contains("LONGEST_RUN_EXCEEDS_WEEKLY_AVERAGE_LOW_CONFIDENCE", inconsistent.Issues);
        Assert.Equal(PrescriptionInputState.Invalid, inconsistent.RecentRace.State);
        Assert.Contains("RECENT_RACE_DATE_IN_FUTURE", inconsistent.RecentRace.Issues);

        // RecentRace is now a single atomic nested object (RecentRaceInput), so
        // "partial" supply (some-but-not-all-three-fields) is no longer
        // representable at the type level -- it's either absent (NotProvided)
        // or present with a concrete (possibly invalid) FinishTimeSeconds/RaceDate.
        // Leaving FinishTimeSeconds/RaceDate at their CLR defaults now surfaces
        // as Invalid (RECENT_RACE_FINISH_TIME_INVALID), not Incomplete.
        var partialRace = CatalogPrescriptionInputNormalizer.Normalize(
            Request(r => r.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK }), AsOfDate);
        Assert.Equal(PrescriptionInputState.Invalid, partialRace.RecentRace.State);
        Assert.Contains("RECENT_RACE_FINISH_TIME_INVALID", partialRace.RecentRace.Issues);
    }

    [Fact]
    public void AnchorSelection_UsesRecentEvidenceOrIntermediateConservativeDefaults()
    {
        var candidate = Candidate();
        var evidence = CatalogPrescriptionInputNormalizer.Normalize(Request(r =>
        {
            r.RecentWeeklyVolumeKm = 34;
            r.RecentLongestRunKm = 12;
        }), AsOfDate);
        Assert.Equal(WeeklyVolumeAnchorSource.RecentFourWeekAverage, CatalogPrescriptionContextBuilder.SelectWeeklyVolumeAnchor(evidence, candidate).Source);
        Assert.Equal(LongRunAnchorSource.Recent30DayLongestRun, CatalogPrescriptionContextBuilder.SelectLongRunAnchor(evidence).Source);

        var missing = CatalogPrescriptionInputNormalizer.Normalize(Request(), AsOfDate);
        var weekly = CatalogPrescriptionContextBuilder.SelectWeeklyVolumeAnchor(missing, candidate);
        Assert.Equal(WeeklyVolumeAnchorSource.LevelConservativeDefault, weekly.Source);
        Assert.Contains("intermediate", weekly.DefaultReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LongRunAnchorSource.LevelConservativeDefault, CatalogPrescriptionContextBuilder.SelectLongRunAnchor(missing).Source);

        var zero = CatalogPrescriptionInputNormalizer.Normalize(Request(r => r.RecentWeeklyVolumeKm = 0), AsOfDate);
        Assert.Equal(WeeklyVolumeAnchorSource.LevelConservativeDefault, CatalogPrescriptionContextBuilder.SelectWeeklyVolumeAnchor(zero, candidate).Source);
        Assert.Equal(LongRunAnchorSource.WeeklyVolumeDerived, CatalogPrescriptionContextBuilder.SelectLongRunAnchor(zero).Source);

        var inconsistent = CatalogPrescriptionInputNormalizer.Normalize(Request(r =>
        {
            r.RecentWeeklyVolumeKm = 20;
            r.RecentLongestRunKm = 25;
        }), AsOfDate);
        Assert.Equal(WeeklyVolumeAnchorSource.RecentFourWeekAverage, CatalogPrescriptionContextBuilder.SelectWeeklyVolumeAnchor(inconsistent, candidate).Source);
        var longRun = CatalogPrescriptionContextBuilder.SelectLongRunAnchor(inconsistent);
        Assert.Equal(LongRunAnchorSource.WeeklyVolumeDerived, longRun.Source);
        Assert.Contains("RECENT_30_DAY_LONGEST_RUN", longRun.RejectedAlternatives);
    }

    [Fact]
    public void PaceSourceMapping_RespectsRuntimeConditionOutputsAndFeasibilityGate()
    {
        var recentRace = CatalogPrescriptionContextBuilder.MapPaceSource(
            new[] { Result("PACE_SOURCE_IN", "RECENT_RACE"), Result("GOAL_FEASIBILITY_IN", "REALISTIC") },
            Request());
        Assert.Equal(PrescriptionPaceSource.RecentRace, recentRace.Source);

        var feasibleTarget = CatalogPrescriptionContextBuilder.MapPaceSource(
            new[] { Result("PACE_SOURCE_IN", "TARGET_TIME"), Result("GOAL_FEASIBILITY_IN", "CHALLENGING") },
            Request());
        Assert.Equal(PrescriptionPaceSource.TargetGoal, feasibleTarget.Source);

        var unsupportedTarget = CatalogPrescriptionContextBuilder.MapPaceSource(
            new[] { Result("PACE_SOURCE_IN", "TARGET_TIME"), Result("GOAL_FEASIBILITY_IN", "UNSUPPORTED") },
            Request());
        Assert.Equal(PrescriptionPaceSource.Unresolved, unsupportedTarget.Source);

        var notEvaluated = CatalogPrescriptionContextBuilder.MapPaceSource(
            new[] { RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "MISSING_RECENT_RACE_AND_TARGET_TIME") },
            Request());
        Assert.Equal(PrescriptionPaceSource.Unresolved, notEvaluated.Source);
        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, notEvaluated.RawStatus);
    }

    [Fact]
    public void WorkoutRuleInventory_CoversCurrentV10WorkoutDefinitionShapes()
    {
        var definitions = WorkoutDefinitions().Values.ToList();
        var basis = definitions.ToDictionary(d => d.Key, CatalogPrescriptionContextBuilder.MeasurementBasisFor, StringComparer.Ordinal);

        Assert.Equal(WorkoutMeasurementBasis.DistanceFirst, basis["EASY_STANDARD"]);
        Assert.Equal(WorkoutMeasurementBasis.DistanceFirst, basis["LONG_RUN_STANDARD"]);
        Assert.Equal(WorkoutMeasurementBasis.DistanceFirst, basis["FARTLEK"]);
        Assert.Equal(WorkoutMeasurementBasis.DistanceFirst, basis["THRESHOLD_TEMPO"]);
        Assert.Equal(WorkoutMeasurementBasis.CatalogModeSelected, basis["GOAL_PACE_TEN_K"]);

        var inventory = CatalogPrescriptionContextBuilder.BuildRuleInventories(definitions);
        Assert.Equal(5, inventory.Count);
        Assert.All(inventory, i => Assert.NotEmpty(i.CatalogBoundedFields));
        Assert.All(inventory, i => Assert.NotEmpty(i.RuntimeUserDerivedFields));
        Assert.All(inventory, i => Assert.DoesNotContain("generatedDose", i.CatalogFixedFields));
    }

    [Fact]
    public void ContextBuilder_AttachesOneDarkContextPerBoundSessionAndPreservesTaperSharpen()
    {
        var context = BuildContext();

        Assert.True(context.ValidationResult.IsValid, string.Join("; ", context.ValidationResult.Errors));
        Assert.Equal(48, context.SessionContexts.Count);
        Assert.Equal(12, context.WeekContexts.Count);
        Assert.Equal(TaperSharpenCapability.ContractExtensionRequired, context.TaperSharpenCapability);
        Assert.Equal(WeeklyVolumeAnchorSource.RecentFourWeekAverage, context.WeeklyVolumeAnchor.Source);
        Assert.Equal(LongRunAnchorSource.Recent30DayLongestRun, context.LongRunAnchor.Source);
        Assert.Equal(PrescriptionPaceSource.RecentRace, context.PaceSource.Source);

        var taperSharpen = context.SessionContexts.Where(s => s.PhaseKey == "TAPER" && s.StructuralRole == "KEY_SESSION").ToList();
        Assert.Equal(2, taperSharpen.Count);
        Assert.All(taperSharpen, s => Assert.Equal("TAPER_SHARPEN", s.ProgressionStageKey));
        Assert.All(taperSharpen, s => Assert.Equal("EASY_STANDARD", s.WorkoutDefinitionKey));
        Assert.All(taperSharpen, s => Assert.Equal(WorkoutMeasurementBasis.DistanceFirst, s.PrimaryMeasurementBasis));
    }

    [Fact]
    public void ContextBuilder_OutputIsDeterministicAndKeepsPrescriptionFieldsDark()
    {
        var first = BuildContext();
        var second = BuildContext();

        var firstShape = first.SessionContexts.Select(s => (s.WeekNumber, s.Date, s.PhaseKey, s.ProgressionStageKey, s.StructuralRole, s.WorkoutDefinitionKey, s.PrimaryMeasurementBasis)).ToList();
        var secondShape = second.SessionContexts.Select(s => (s.WeekNumber, s.Date, s.PhaseKey, s.ProgressionStageKey, s.StructuralRole, s.WorkoutDefinitionKey, s.PrimaryMeasurementBasis)).ToList();
        Assert.Equal(firstShape, secondShape);
        Assert.Equal(first.DecisionTrace.Select(t => (t.Subject, t.Source, t.Reason)), second.DecisionTrace.Select(t => (t.Subject, t.Source, t.Reason)));

        var publicProperties = typeof(GeneratePreviewResponse).GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => p.Name).ToList();
        Assert.DoesNotContain(publicProperties, p => p.Contains("Prescription", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicProperties, p => p.Contains("CatalogRule", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ContextContracts_DoNotExposeFinalNumericPrescriptionDoseFields()
    {
        var contractTypes = new[]
        {
            typeof(CatalogSessionPrescriptionContext),
            typeof(CatalogPlanPrescriptionContext),
            typeof(WeeklyVolumeAnchorDecision),
            typeof(LongRunAnchorDecision),
            typeof(ResolvedPrescriptionPaceSource)
        };

        var forbiddenFragments = new[] { "PrescribedDistance", "PrescribedDuration", "PrescribedPace", "Repetitions", "RecoverySeconds", "SegmentDistance", "LongRunDistance", "TaperDose" };
        foreach (var type in contractTypes)
        {
            var names = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(p => p.Name);
            foreach (var fragment in forbiddenFragments)
            {
                Assert.DoesNotContain(names, n => n.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private static CatalogPlanPrescriptionContext BuildContext()
    {
        var request = Request(r =>
        {
            r.RecentWeeklyVolumeKm = 34;
            r.RecentLongestRunKm = 12;
            r.RecentRunsPerWeek = 4;
            r.RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = AsOfDate.AddDays(-21) };
        });

        return new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request,
            AsOfDate,
            Candidate(),
            InputSnapshot(),
            new[] { Result("PACE_SOURCE_IN", "RECENT_RACE"), Result("GOAL_FEASIBILITY_IN", "REALISTIC") },
            BoundPlan(),
            WorkoutDefinitions()));
    }

    private static GeneratePreviewRequest Request(Action<GeneratePreviewRequest>? configure = null)
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            RaceDate = new DateOnly(2026, 10, 4),
            TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Sat },
            LongRunDay = Weekday.Sat
        };

        configure?.Invoke(request);
        return request;
    }

    private static ResolverInputSnapshot InputSnapshot() => new()
    {
        RequestedTargetDistanceKm = 10d,
        CanonicalDistanceFamily = "TEN_K",
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        GoalDistanceKm = 10d,
        StartDate = new DateOnly(2026, 7, 20),
        RaceDate = new DateOnly(2026, 10, 4),
        TargetFinishTimeSeconds = 3000,
        DaysPerWeek = 4,
        Level = RunningBackground.Intermediate
    };

    private static RuntimeConditionResolutionResult Result(string conditionType, string outputValue) =>
        RuntimeConditionResolutionResult.Evaluated(conditionType, outputValue, "TEST");

    private static PlanCatalogCandidateSummary Candidate() => new()
    {
        CandidateKey = "TEN_K__4D__INTERMEDIATE",
        CandidateVersion = 10,
        CandidateStatus = "DRAFT",
        CanonicalDistanceFamily = "TEN_K",
        Level = "INTERMEDIATE",
        DaysPerWeek = 4,
        CoreCycle = new PlanCatalogCoreCycle(8, 12, 14),
        MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 6),
        Layout = new PlanCatalogReference("FOUR_DAY_STANDARD", 2),
        LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
        WorkoutProgression = new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 5),
        ProgressionModifier = new PlanCatalogReference("PROGRESSION_MODIFIER", 2),
        RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
        PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 3),
        RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES", 2),
        DependencyStatuses = new Dictionary<string, string>(),
        ReferencedWorkouts = new[]
        {
            new PlanCatalogReference("EASY_STANDARD", 4),
            new PlanCatalogReference("LONG_RUN_STANDARD", 4),
            new PlanCatalogReference("FARTLEK", 4),
            new PlanCatalogReference("THRESHOLD_TEMPO", 4),
            new PlanCatalogReference("GOAL_PACE_TEN_K", 2)
        },
        PhaseKeys = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
        PhaseAllocations = new[]
        {
            new PlanCatalogPhaseAllocation("FOUNDATION", 2),
            new PlanCatalogPhaseAllocation("BUILD", 4),
            new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 4),
            new PlanCatalogPhaseAllocation("TAPER", 2)
        },
        SlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }
    };

    private static IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> WorkoutDefinitions() =>
        new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal)
        {
            ["EASY_STANDARD"] = Definition("EASY_STANDARD", 4, "EASY", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["LONG_RUN_STANDARD"] = Definition("LONG_RUN_STANDARD", 4, "LONG_RUN", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["FARTLEK"] = Definition("FARTLEK", 4, "QUALITY", new[] { "BUILD" }, new[] { "DISTANCE", "MIXED" }, new[] { "ESTIMATED_SESSION_TOTAL" }, Components("WARM_UP", "EASY", "MAIN_SET", "SURGE_AND_FLOAT", "RECOVERY", "EASY_JOG", "COOL_DOWN", "EASY")),
            ["THRESHOLD_TEMPO"] = Definition("THRESHOLD_TEMPO", 4, "QUALITY", new[] { "BUILD", "RACE_SPECIFIC" }, new[] { "DISTANCE", "MIXED" }, new[] { "ESTIMATED_SESSION_TOTAL" }, Components("WARM_UP", "EASY", "MAIN_SET", "THRESHOLD", "COOL_DOWN", "EASY")),
            ["GOAL_PACE_TEN_K"] = Definition("GOAL_PACE_TEN_K", 2, "QUALITY", new[] { "RACE_SPECIFIC" }, new[] { "PACE_BASED" }, new[] { "ESTIMATED_SESSION_TOTAL" }, Components("WARM_UP", "EASY", "MAIN_SET", "GOAL_PACE", "COOL_DOWN", "EASY"))
        };

    private static CatalogWorkoutDefinitionSummary Definition(
        string key,
        int version,
        string family,
        IReadOnlyList<string> phases,
        IReadOnlyList<string> modes,
        IReadOnlyList<string> accountingModes,
        IReadOnlyList<CatalogWorkoutComponentSummary>? components = null) => new()
        {
            Key = key,
            Version = version,
            Status = "DRAFT",
            Family = family,
            EligiblePhases = phases,
            AllowedPrescriptionModes = modes,
            AllowedDistanceAccountingModes = accountingModes,
            Components = components ?? Array.Empty<CatalogWorkoutComponentSummary>()
        };

    private static IReadOnlyList<CatalogWorkoutComponentSummary> Components(params string[] pairs) =>
        pairs.Chunk(2).Select((pair, i) => new CatalogWorkoutComponentSummary(i + 1, pair[0], pair[1])).ToList();

    private static BoundCatalogPlan BoundPlan()
    {
        var sessions = Enumerable.Range(1, 12)
            .SelectMany(week =>
            {
                var phase = week switch
                {
                    <= 2 => "FOUNDATION",
                    <= 6 => "BUILD",
                    <= 10 => "RACE_SPECIFIC",
                    _ => "TAPER"
                };

                var keyStage = week switch
                {
                    <= 2 => "FOUNDATION_EASY_BASE",
                    <= 4 => "FARTLEK_INTRO",
                    <= 6 => "THRESHOLD_TEMPO_BUILD",
                    <= 10 => "GOAL_PACE_REHEARSAL",
                    _ => "TAPER_SHARPEN"
                };

                var workout = keyStage switch
                {
                    "FARTLEK_INTRO" => ("FARTLEK", 4),
                    "THRESHOLD_TEMPO_BUILD" => ("THRESHOLD_TEMPO", 4),
                    "GOAL_PACE_REHEARSAL" => ("GOAL_PACE_TEN_K", 2),
                    _ => ("EASY_STANDARD", 4)
                };

                var monday = new DateOnly(2026, 7, 20).AddDays((week - 1) * 7);
                return new[]
                {
                    Session(week, monday, phase, "KEY_SESSION", keyStage, workout.Item1, workout.Item2, CatalogWorkoutBindingMode.StageControlled),
                    Session(week, monday.AddDays(2), phase, "EASY_SUPPORT", null, "EASY_STANDARD", 4, CatalogWorkoutBindingMode.FixedDefault),
                    Session(week, monday.AddDays(4), phase, "EASY_SUPPORT", null, "EASY_STANDARD", 4, CatalogWorkoutBindingMode.FixedDefault),
                    Session(week, monday.AddDays(6), phase, "LONG_RUN", null, "LONG_RUN_STANDARD", 4, CatalogWorkoutBindingMode.FixedDefault)
                };
            })
            .ToList();

        return new BoundCatalogPlan
        {
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            BinderVersion = "CATALOG_WORKOUT_BINDER_V1",
            Weeks = sessions.GroupBy(s => s.WeekNumber).Select(g => new BoundCatalogWeek { WeekNumber = g.Key, PhaseKey = g.First().PhaseKey, Sessions = g.ToList() }).ToList(),
            Trace = new WorkoutBindingDecisionTrace { Steps = Array.Empty<WorkoutBindingDecisionTraceStep>() }
        };
    }

    private static BoundCatalogSession Session(
        int week,
        DateOnly date,
        string phase,
        string role,
        string? stage,
        string workoutKey,
        int workoutVersion,
        CatalogWorkoutBindingMode mode) => new()
        {
            WeekNumber = week,
            Date = date,
            PhaseKey = phase,
            ProgressionStageKey = stage,
            StructuralRole = role,
            WorkoutDefinitionKey = workoutKey,
            WorkoutDefinitionVersion = workoutVersion,
            BindingMode = mode,
            BindingPolicyKey = "TEN_K_WORKOUT_PROGRESSION_V1",
            BindingPolicyVersion = 5,
            SourceArtifactKey = "TEN_K_WORKOUT_PROGRESSION_V1",
            SourceArtifactVersion = 5,
            ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned,
            FallbackOrigin = null,
            BindingReason = "TEST"
        };
}
