using System;
using System.Collections.Generic;
using System.Linq;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Prescriptions;
using PlanCatalog.Contracts.References;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split C — wires the real <see cref="CatalogSessionPrescriptionPlanner"/>
/// through the existing, previously-dormant <see cref="CatalogSessionPrescriptionSource"/> and
/// <see cref="ExecutionPrescriptionIndex"/> (FREQ.6D.3D) for genuinely bound ProfileBacked sessions,
/// consuming the exact profile lineage Split B materializes on <see cref="BoundCatalogSession"/>. No
/// real RUN_LAYOUT_5D/combination exists yet, so this exercises the real end-to-end planner (not a
/// synthetic binder-only fixture, unlike Split A/B) with a hand-constructed dual-lane BoundCatalogPlan
/// — the same established precedent this whole engagement uses whenever no real 5D catalog artifact
/// exists to load from disk.
/// </summary>
public sealed class Freq6D4DSplitCProfileBackedSessionConsumptionTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 15);

    // ───────────────────────── Shared fixture helpers (mirrors Phase4F7BVolumeAndLongRunTests) ─────────────────────────

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
            LongRunDay = Weekday.Sat,
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9,
            RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = AsOfDate.AddDays(-21) }
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
        CandidateKey = "SYNTHETIC_5D_TEST_CANDIDATE",
        CandidateVersion = 1,
        CandidateStatus = "DRAFT",
        CanonicalDistanceFamily = "TEN_K",
        Level = "INTERMEDIATE",
        DaysPerWeek = 4,
        CoreCycle = new PlanCatalogCoreCycle(8, 12, 14),
        MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 6),
        Layout = new PlanCatalogReference("FOUR_DAY_STANDARD", 2),
        LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
        WorkoutProgression = new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 5),
        ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2),
        RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
        PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 3),
        RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
        DependencyStatuses = new Dictionary<string, string>(),
        ReferencedWorkouts = new[] { new PlanCatalogReference("EASY_STANDARD", 4), new PlanCatalogReference("LONG_RUN_STANDARD", 4) },
        PhaseKeys = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
        PhaseAllocations = new[] { new PlanCatalogPhaseAllocation("FOUNDATION", 3), new PlanCatalogPhaseAllocation("BUILD", 4), new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 4), new PlanCatalogPhaseAllocation("TAPER", 1) },
        SlotRoles = new[] { "KEY_SESSION", "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }
    };

    private static IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> WorkoutDefinitions() =>
        new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal)
        {
            ["EASY_STANDARD"] = Definition("EASY_STANDARD", 4, "EASY", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["LONG_RUN_STANDARD"] = Definition("LONG_RUN_STANDARD", 4, "LONG_RUN", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["THRESHOLD_TEMPO"] = Definition("THRESHOLD_TEMPO", 4, "QUALITY", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC" }, new[] { "MIXED" }, new[] { "ESTIMATED_SESSION_TOTAL" },
                new[] { Component(1, "WARM_UP", "EASY"), Component(2, "MAIN_SET", "THRESHOLD"), Component(3, "COOL_DOWN", "EASY") }),
            ["FARTLEK"] = Definition("FARTLEK", 4, "QUALITY", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC" }, new[] { "MIXED" }, new[] { "ESTIMATED_SESSION_TOTAL" },
                new[] { Component(1, "WARM_UP", "EASY"), Component(2, "MAIN_SET", "SURGE_AND_FLOAT"), Component(3, "COOL_DOWN", "EASY") }),
        };

    private static CatalogWorkoutComponentSummary Component(int order, string type, string intensity) => new(order, type, intensity);

    private static CatalogWorkoutDefinitionSummary Definition(string key, int version, string family, IReadOnlyList<string> phases, IReadOnlyList<string> modes, IReadOnlyList<string> accountingModes, IReadOnlyList<CatalogWorkoutComponentSummary>? components = null) => new()
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

    // ───────────────────────── Dual-lane bound plan (5 sessions/week: 2 KEY + 2 EASY + 1 LONG) ─────────────────────────

    private static BoundCatalogPlan DualLaneBoundPlan(
        int weeks,
        string? lane0ProfileKey = "PROFILE_A", int? lane0ProfileVersion = 1, string lane0WorkoutKey = "THRESHOLD_TEMPO",
        string? lane1ProfileKey = "PROFILE_B", int? lane1ProfileVersion = 1, string lane1WorkoutKey = "FARTLEK")
    {
        var boundWeeks = Enumerable.Range(1, weeks).Select(week =>
        {
            var phase = week switch
            {
                <= 2 => "FOUNDATION",
                _ when week == weeks => "TAPER",
                _ when week >= Math.Max(3, weeks - 4) => "RACE_SPECIFIC",
                _ => "BUILD"
            };
            var stage = phase == "TAPER" ? "TAPER_SHARPEN" : $"{phase}_STAGE";
            var monday = new DateOnly(2026, 7, 20).AddDays((week - 1) * 7);
            return new BoundCatalogWeek
            {
                WeekNumber = week,
                PhaseKey = phase,
                Sessions = new[]
                {
                    KeySession(week, monday, phase, stage, 0, lane0WorkoutKey, lane0ProfileKey, lane0ProfileVersion),
                    KeySession(week, monday.AddDays(1), phase, stage, 1, lane1WorkoutKey, lane1ProfileKey, lane1ProfileVersion),
                    FixedSession(week, monday.AddDays(2), phase, "EASY_SUPPORT", "EASY_STANDARD"),
                    FixedSession(week, monday.AddDays(4), phase, "EASY_SUPPORT", "EASY_STANDARD"),
                    FixedSession(week, monday.AddDays(6), phase, "LONG_RUN", "LONG_RUN_STANDARD"),
                }
            };
        }).ToList();

        return new BoundCatalogPlan
        {
            CandidateKey = "SYNTHETIC_5D_TEST_CANDIDATE",
            CandidateVersion = 1,
            BinderVersion = "CATALOG_WORKOUT_BINDER_V1",
            Weeks = boundWeeks,
            Trace = new WorkoutBindingDecisionTrace { Steps = Array.Empty<WorkoutBindingDecisionTraceStep>() }
        };
    }

    private static BoundCatalogSession KeySession(
        int week, DateOnly date, string phase, string stage, int laneOrdinal, string workoutKey, string? profileKey, int? profileVersion) => new()
    {
        WeekNumber = week, Date = date, PhaseKey = phase, ProgressionStageKey = stage, LaneOrdinal = laneOrdinal,
        PrescriptionProfileKey = profileKey, PrescriptionProfileVersion = profileVersion,
        StructuralRole = "KEY_SESSION", WorkoutDefinitionKey = workoutKey, WorkoutDefinitionVersion = 4,
        BindingMode = CatalogWorkoutBindingMode.StageControlled,
        BindingPolicyKey = "TEN_K_WORKOUT_PROGRESSION_V1", BindingPolicyVersion = 5,
        SourceArtifactKey = "TEN_K_WORKOUT_PROGRESSION_V1", SourceArtifactVersion = 5,
        ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned, FallbackOrigin = null, BindingReason = "TEST"
    };

    private static BoundCatalogSession FixedSession(int week, DateOnly date, string phase, string role, string workoutKey) => new()
    {
        WeekNumber = week, Date = date, PhaseKey = phase, ProgressionStageKey = null, LaneOrdinal = null,
        PrescriptionProfileKey = null, PrescriptionProfileVersion = null,
        StructuralRole = role, WorkoutDefinitionKey = workoutKey, WorkoutDefinitionVersion = 4,
        BindingMode = CatalogWorkoutBindingMode.FixedDefault,
        BindingPolicyKey = "V1_FIXED_DEFAULT", BindingPolicyVersion = 1,
        SourceArtifactKey = "V1_FIXED_DEFAULT", SourceArtifactVersion = 1,
        ConditionOutcome = null, FallbackOrigin = null, BindingReason = "TEST"
    };

    // ───────────────────────── ExecutionPrescriptionIndex / bundle fixture ─────────────────────────

    private static ExecutableWorkoutPrescription Execution(
        string profileKey, int profileVersion, string workoutKey, int workoutVersion,
        ExecutablePrescriptionDoseCategory dose, int repetitions, int workValueMeters, int recoveryValueMeters, int recoveryCount) => new()
    {
        ContractSchemaVersion = 1,
        SourceProfile = new CatalogArtifactReference { DocumentType = "WORKOUT_PRESCRIPTION_PROFILE", Key = profileKey, Version = profileVersion, ContentHash = $"{profileKey}-hash" },
        SourceWorkout = new CatalogArtifactReference { DocumentType = "WORKOUT_DEFINITION", Key = workoutKey, Version = workoutVersion, ContentHash = $"{workoutKey}-hash" },
        DoseCategory = dose,
        DistanceAccountingMode = DistanceAccountingMode.EstimatedSessionTotal,
        Components =
        [
            new ExecutablePrescriptionComponent
            {
                SequenceOrder = 1, ComponentType = WorkoutComponentType.MainSet, StructureMode = ExecutablePrescriptionStructureMode.Repeated,
                Work = new ExecutableWorkQuantity { Unit = ExecutableQuantityUnit.Meters, Value = workValueMeters },
                RepetitionCount = repetitions,
                Recovery = new ExecutableRecovery { Unit = ExecutableQuantityUnit.Meters, Value = recoveryValueMeters, Mode = ExecutableRecoveryMode.Jog, Placement = ExecutableRecoveryPlacement.BetweenRepetitions, RecoveryCount = recoveryCount },
                Intensity = new ExecutableIntensityTarget { Mode = ExecutableIntensityMode.PaceBased, DescriptorKey = "THRESHOLD_PACE" }
            }
        ]
    };

    private static ExecutionPrescriptionIndex IndexWithBothLanes() => ExecutionPrescriptionIndex.Build(BundleWith(
        Execution("PROFILE_A", 1, "THRESHOLD_TEMPO", 4, ExecutablePrescriptionDoseCategory.Primary, 4, 1600, 400, 3),
        Execution("PROFILE_B", 1, "FARTLEK", 4, ExecutablePrescriptionDoseCategory.SecondaryControlled, 10, 60, 60, 9)));

    private static PlanCatalog.Contracts.Bundles.PublishedTemplateBundle BundleWith(params ExecutableWorkoutPrescription[] executions) => new()
    {
        BundleKey = "SYNTHETIC_5D_TEST_CANDIDATE", BundleVersion = 1,
        Combination = ArtifactRef("TEMPLATE_COMBINATION", "SYNTHETIC_5D_TEST_CANDIDATE", 1),
        MasterTemplate = ArtifactRef("PLAN_TEMPLATE", "TEN_K_MASTER", 6),
        Layout = ArtifactRef("RUN_LAYOUT", "FOUR_DAY_STANDARD", 2),
        LevelModifier = ArtifactRef("LEVEL_MODIFIER", "INTERMEDIATE_MODIFIER", 6),
        WorkoutProgression = ArtifactRef("WORKOUT_PROGRESSION", "TEN_K_WORKOUT_PROGRESSION_V1", 5),
        ProgressionModifier = ArtifactRef("PROGRESSION_MODIFIER", "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2),
        RulePack = ArtifactRef("RULE_PACK", "APPSEL_RACE_PLAN_V1", 4),
        RuntimeConditionValueRegistry = ArtifactRef("RUNTIME_CONDITION_VALUE_REGISTRY", "RUNTIME_CONDITION_VALUES_V1", 2),
        PeakVolumeBandPolicy = ArtifactRef("PEAK_VOLUME_BAND_POLICY", "PEAK_VOLUME_BANDS_V1", 3),
        Workouts = [ArtifactRef("WORKOUT_DEFINITION", "THRESHOLD_TEMPO", 4), ArtifactRef("WORKOUT_DEFINITION", "FARTLEK", 4)],
        ExecutionPrescriptions = executions,
        BundleContentHash = "test-bundle-hash",
    };

    private static CatalogArtifactReference ArtifactRef(string documentType, string key, int version) =>
        new() { DocumentType = documentType, Key = key, Version = version, ContentHash = $"{key}-hash" };

    // ───────────────────────── End-to-end planner invocation ─────────────────────────

    private static CatalogPrescribedPlan BuildPrescribed(BoundCatalogPlan bound, ExecutionPrescriptionIndex? index)
    {
        var request = Request();
        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request, AsOfDate, Candidate(), InputSnapshot(),
            new[] { Result("PACE_SOURCE_IN", "RECENT_RACE"), Result("GOAL_FEASIBILITY_IN", "REALISTIC") },
            bound, WorkoutDefinitions()));
        var volume = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(
            Candidate(), bound, prescription, new CatalogPeakVolumeBand("TEN_K", "INTERMEDIATE", 5, 30, 42, "PEAK_VOLUME_BANDS_V1", 3)));

        return new CatalogSessionPrescriptionPlanner().Build(new CatalogSessionPrescriptionRequest(
            Candidate(), bound, prescription, volume, WorkoutDefinitions(), index));
    }

    // ───────────────────────── Tests ─────────────────────────

    [Fact]
    public void Legacy_BothProfileFieldsNull_ClassifiesLegacy_UsesExistingComputedPrescription()
    {
        var bound = DualLaneBoundPlan(8, lane0ProfileKey: null, lane0ProfileVersion: null, lane1ProfileKey: null, lane1ProfileVersion: null);
        var plan = BuildPrescribed(bound, index: null);

        var key0 = plan.Sessions.First(s => s.WeekNumber == 1 && s.WorkoutDefinitionKey == "THRESHOLD_TEMPO");
        var legacy = Assert.IsType<CatalogSessionPrescriptionSource.Legacy>(key0.PrescriptionSource);
        Assert.Same(key0.Prescription, legacy.Prescription);
    }

    [Fact]
    public void ProfileBacked_BothExactFieldsPresent_ResolvesExactExecutionPrescription()
    {
        var bound = DualLaneBoundPlan(8);
        var plan = BuildPrescribed(bound, IndexWithBothLanes());

        var key0 = plan.Sessions.First(s => s.WeekNumber == 1 && s.WorkoutDefinitionKey == "THRESHOLD_TEMPO");
        var profileBacked = Assert.IsType<CatalogSessionPrescriptionSource.ProfileBacked>(key0.PrescriptionSource);
        Assert.Equal("PROFILE_A", profileBacked.Prescription.SourceProfile.Key);
        Assert.Equal(1, profileBacked.Prescription.SourceProfile.Version);
    }

    [Fact]
    public void PartialLineage_KeyOnly_ThrowsInvalidLineage()
    {
        var bound = DualLaneBoundPlan(8, lane0ProfileVersion: null);

        var ex = Assert.Throws<CatalogSessionPrescriptionInvalidProfileLineageException>(() => BuildPrescribed(bound, IndexWithBothLanes()));
        Assert.Equal("CATALOG_SESSION_PRESCRIPTION_INVALID_PROFILE_LINEAGE", ex.Code);
    }

    [Fact]
    public void PartialLineage_VersionOnly_ThrowsInvalidLineage()
    {
        var bound = DualLaneBoundPlan(8, lane0ProfileKey: null);

        Assert.Throws<CatalogSessionPrescriptionInvalidProfileLineageException>(() => BuildPrescribed(bound, IndexWithBothLanes()));
    }

    [Fact]
    public void ProfileBacked_MissingExecutionIndex_NeverFallsBackToLegacy_ThrowsTypedException()
    {
        var bound = DualLaneBoundPlan(8);

        var ex = Assert.Throws<CatalogSessionPrescriptionMissingExecutionPrescriptionException>(() => BuildPrescribed(bound, index: null));
        Assert.Equal("CATALOG_SESSION_PRESCRIPTION_MISSING_EXECUTION_PRESCRIPTION", ex.Code);
    }

    [Fact]
    public void ProfileBacked_ExactProfileMissingFromBundle_FailsClosed()
    {
        var bound = DualLaneBoundPlan(8, lane0ProfileKey: "PROFILE_NOT_IN_BUNDLE");

        Assert.Throws<ExecutionPrescriptionNotFoundException>(() => BuildPrescribed(bound, IndexWithBothLanes()));
    }

    [Fact]
    public void ProfileBacked_WrongExactVersion_FailsClosed_DoesNotUseOtherVersion()
    {
        var bound = DualLaneBoundPlan(8, lane0ProfileVersion: 99);

        Assert.Throws<ExecutionPrescriptionNotFoundException>(() => BuildPrescribed(bound, IndexWithBothLanes()));
    }

    [Fact]
    public void ProfileBacked_WorkoutProvenanceMismatch_FailsClosed()
    {
        // Lane0's stage-resolved workout is THRESHOLD_TEMPO, but its exact profile's own execution
        // provenance (per IndexWithBothLanes) also names THRESHOLD_TEMPO — swap in a bundle where
        // PROFILE_A's SourceWorkout diverges from the bound session's own WorkoutDefinitionKey.
        var divergentIndex = ExecutionPrescriptionIndex.Build(BundleWith(
            Execution("PROFILE_A", 1, "FARTLEK", 4, ExecutablePrescriptionDoseCategory.Primary, 4, 1600, 400, 3),
            Execution("PROFILE_B", 1, "FARTLEK", 4, ExecutablePrescriptionDoseCategory.SecondaryControlled, 10, 60, 60, 9)));
        var bound = DualLaneBoundPlan(8);

        Assert.Throws<CatalogSessionPrescriptionProfileWorkoutMismatchException>(() => BuildPrescribed(bound, divergentIndex));
    }

    [Fact]
    public void Legacy_WithNullExecutionLibrary_ContinuesWorkingExactly_RealFourDayCompatibilityCase()
    {
        // The real, existing 3D/4D single-KEY shape: no profile lineage, no index. Must produce a
        // fully valid legacy-classified plan, unchanged.
        var bound = FourDayLegacyBoundPlan(8);
        var plan = BuildPrescribed(bound, index: null);

        Assert.True(plan.ValidationResult.IsValid);
        Assert.All(plan.Sessions, s => Assert.IsType<CatalogSessionPrescriptionSource.Legacy>(s.PrescriptionSource));
    }

    [Fact]
    public void Legacy_WithNonNullUnrelatedLibrary_StillUsesLegacyPath_LibraryExistenceDoesNotUpgrade()
    {
        var bound = FourDayLegacyBoundPlan(8);
        var plan = BuildPrescribed(bound, IndexWithBothLanes());

        Assert.All(plan.Sessions, s => Assert.IsType<CatalogSessionPrescriptionSource.Legacy>(s.PrescriptionSource));
    }

    [Fact]
    public void DualLane_TwoExactProfiles_ConsumedIndependently_NoDoseCategoryInspectionAtConsumerTime()
    {
        var bound = DualLaneBoundPlan(8);
        var plan = BuildPrescribed(bound, IndexWithBothLanes());

        var week1 = plan.Weeks.Single(w => w.WeekNumber == 1);
        var lane0 = week1.Sessions.Single(s => s.WorkoutDefinitionKey == "THRESHOLD_TEMPO");
        var lane1 = week1.Sessions.Single(s => s.WorkoutDefinitionKey == "FARTLEK");

        var source0 = Assert.IsType<CatalogSessionPrescriptionSource.ProfileBacked>(lane0.PrescriptionSource);
        var source1 = Assert.IsType<CatalogSessionPrescriptionSource.ProfileBacked>(lane1.PrescriptionSource);
        Assert.Equal("PROFILE_A", source0.Prescription.SourceProfile.Key);
        Assert.Equal("PROFILE_B", source1.Prescription.SourceProfile.Key);
    }

    [Fact]
    public void SameStage_DifferentLane_ResolvesDistinctExactProfileExecutions()
    {
        var bound = DualLaneBoundPlan(8);
        var plan = BuildPrescribed(bound, IndexWithBothLanes());

        var week1 = plan.Weeks.Single(w => w.WeekNumber == 1);
        var lane0 = week1.Sessions.Single(s => s.StructuralRole == "KEY_SESSION" && s.WorkoutDefinitionKey == "THRESHOLD_TEMPO");
        var lane1 = week1.Sessions.Single(s => s.StructuralRole == "KEY_SESSION" && s.WorkoutDefinitionKey == "FARTLEK");

        Assert.Equal(lane0.ProgressionStageKey is not null, lane1.ProgressionStageKey is not null);
        var source0 = (CatalogSessionPrescriptionSource.ProfileBacked)lane0.PrescriptionSource!;
        var source1 = (CatalogSessionPrescriptionSource.ProfileBacked)lane1.PrescriptionSource!;
        Assert.NotEqual(source0.Prescription.SourceProfile.Key, source1.Prescription.SourceProfile.Key);
    }

    [Fact]
    public void ContentFidelity_ProfileBackedPrescriptionMatchesBundlePayloadExactly_NoRebuild()
    {
        var bound = DualLaneBoundPlan(8);
        var plan = BuildPrescribed(bound, IndexWithBothLanes());

        var lane1 = plan.Weeks.Single(w => w.WeekNumber == 1).Sessions.Single(s => s.WorkoutDefinitionKey == "FARTLEK");
        var source = (CatalogSessionPrescriptionSource.ProfileBacked)lane1.PrescriptionSource!;

        // BLD-S-shaped: 10 x 60s work, 60s Jog recovery BetweenRepetitions, RecoveryCount 9.
        var component = source.Prescription.Components.Single();
        Assert.Equal(10, component.RepetitionCount);
        Assert.Equal(60, component.Work.Value);
        Assert.Equal(ExecutableQuantityUnit.Meters, component.Work.Unit);
        Assert.Equal(60, component.Recovery!.Value);
        Assert.Equal(9, component.Recovery!.RecoveryCount);
        Assert.Equal(ExecutableRecoveryPlacement.BetweenRepetitions, component.Recovery!.Placement);
        Assert.Equal(ExecutablePrescriptionDoseCategory.SecondaryControlled, source.Prescription.DoseCategory);
        Assert.Equal("FARTLEK", source.Prescription.SourceWorkout.Key);
    }

    [Fact]
    public void CalendarOrderIndependence_LaneOrderByDateDiffersFromStructuralOrder_ProfileIdentityUnchanged()
    {
        // Lane 1 (SecondaryControlled/FARTLEK) is scheduled EARLIER by calendar date than Lane 0 in
        // week 1 only — proves date order never re-derives which profile a lane consumes.
        var original = DualLaneBoundPlan(8);
        var reorderedWeek1 = new BoundCatalogWeek
        {
            WeekNumber = 1,
            PhaseKey = "FOUNDATION",
            Sessions = new[]
            {
                KeySession(1, new DateOnly(2026, 7, 20), "FOUNDATION", "FOUNDATION_STAGE", 1, "FARTLEK", "PROFILE_B", 1),
                KeySession(1, new DateOnly(2026, 7, 22), "FOUNDATION", "FOUNDATION_STAGE", 0, "THRESHOLD_TEMPO", "PROFILE_A", 1),
                FixedSession(1, new DateOnly(2026, 7, 23), "FOUNDATION", "EASY_SUPPORT", "EASY_STANDARD"),
                FixedSession(1, new DateOnly(2026, 7, 24), "FOUNDATION", "EASY_SUPPORT", "EASY_STANDARD"),
                FixedSession(1, new DateOnly(2026, 7, 26), "FOUNDATION", "LONG_RUN", "LONG_RUN_STANDARD"),
            }
        };
        var bound = new BoundCatalogPlan
        {
            CandidateKey = original.CandidateKey,
            CandidateVersion = original.CandidateVersion,
            BinderVersion = original.BinderVersion,
            Weeks = original.Weeks.Select(w => w.WeekNumber == 1 ? reorderedWeek1 : w).ToList(),
            Trace = original.Trace,
        };

        var plan = BuildPrescribed(bound, IndexWithBothLanes());

        var earlierByDate = plan.Sessions.Single(s => s.Date == new DateOnly(2026, 7, 20));
        var laterByDate = plan.Sessions.Single(s => s.Date == new DateOnly(2026, 7, 22));
        var earlierSource = (CatalogSessionPrescriptionSource.ProfileBacked)earlierByDate.PrescriptionSource!;
        var laterSource = (CatalogSessionPrescriptionSource.ProfileBacked)laterByDate.PrescriptionSource!;

        Assert.Equal("PROFILE_B", earlierSource.Prescription.SourceProfile.Key);
        Assert.Equal("PROFILE_A", laterSource.Prescription.SourceProfile.Key);
    }

    [Fact]
    public void LaneOrdinal_ProgressionStageKey_ProfileKeyVersion_AllPreservedThroughInternalMaterialization()
    {
        var bound = DualLaneBoundPlan(8);
        var plan = BuildPrescribed(bound, IndexWithBothLanes());

        var week1 = plan.Weeks.Single(w => w.WeekNumber == 1);
        var lane0 = week1.Sessions.Single(s => s.WorkoutDefinitionKey == "THRESHOLD_TEMPO");

        Assert.Equal("FOUNDATION_STAGE", lane0.ProgressionStageKey);
        var source = (CatalogSessionPrescriptionSource.ProfileBacked)lane0.PrescriptionSource!;
        Assert.Equal("PROFILE_A", source.Prescription.SourceProfile.Key);
        Assert.Equal(1, source.Prescription.SourceProfile.Version);
    }

    [Fact]
    public void NoRuntimeProjection_ResolvedExecutionIsTheExactBundleInstance()
    {
        var index = IndexWithBothLanes();
        var expected = index.ResolveExact(new VersionedCatalogReference { DocumentType = "WORKOUT_PRESCRIPTION_PROFILE", Key = "PROFILE_A", Version = 1 });
        var bound = DualLaneBoundPlan(8);

        var plan = BuildPrescribed(bound, index);

        var lane0 = plan.Weeks.Single(w => w.WeekNumber == 1).Sessions.Single(s => s.WorkoutDefinitionKey == "THRESHOLD_TEMPO");
        var source = (CatalogSessionPrescriptionSource.ProfileBacked)lane0.PrescriptionSource!;
        Assert.Same(expected, source.Prescription);
    }

    private static BoundCatalogPlan FourDayLegacyBoundPlan(int weeks)
    {
        var boundWeeks = Enumerable.Range(1, weeks).Select(week =>
        {
            var phase = week switch { <= 2 => "FOUNDATION", _ when week == weeks => "TAPER", _ when week >= Math.Max(3, weeks - 4) => "RACE_SPECIFIC", _ => "BUILD" };
            var stage = phase == "TAPER" ? "TAPER_SHARPEN" : $"{phase}_STAGE";
            var monday = new DateOnly(2026, 7, 20).AddDays((week - 1) * 7);
            return new BoundCatalogWeek
            {
                WeekNumber = week, PhaseKey = phase,
                Sessions = new[]
                {
                    KeySession(week, monday, phase, stage, 0, "EASY_STANDARD", null, null),
                    FixedSession(week, monday.AddDays(2), phase, "EASY_SUPPORT", "EASY_STANDARD"),
                    FixedSession(week, monday.AddDays(4), phase, "EASY_SUPPORT", "EASY_STANDARD"),
                    FixedSession(week, monday.AddDays(6), phase, "LONG_RUN", "LONG_RUN_STANDARD"),
                }
            };
        }).ToList();

        return new BoundCatalogPlan
        {
            CandidateKey = "SYNTHETIC_5D_TEST_CANDIDATE", CandidateVersion = 1, BinderVersion = "CATALOG_WORKOUT_BINDER_V1",
            Weeks = boundWeeks, Trace = new WorkoutBindingDecisionTrace { Steps = Array.Empty<WorkoutBindingDecisionTraceStep>() },
        };
    }
}
