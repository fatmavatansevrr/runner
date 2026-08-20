using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription;

/// <summary>
/// Phase 10K-FREQ.6D.4D.5D — direct unit tests for
/// <see cref="CatalogPrescriptionContextBuilder"/>'s Taper-completeness
/// partition (FREQ.6D.4D.5C's approved decision): Legacy Taper KEY_SESSION
/// instances still require the exact <c>TAPER_SHARPEN</c>/<c>EASY_STANDARD</c>
/// identity unchanged; ProfileBacked instances are exempt from that literal
/// check (their completeness is proven downstream by Split C's fail-closed
/// execution-resolution guarantee, not re-implemented here). No existing
/// dedicated test of this validator existed before this phase (confirmed by
/// FREQ.6D.4D.5C's own repository search) -- this file is the first.
/// </summary>
public sealed class Freq6D4D5DTaperCompletenessPartitionTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 20);

    // ── §34.1-2: historical legacy identity, unchanged ──────────────────────

    [Fact]
    public void LegacyValidTaperSharpenIdentity_Passes()
    {
        var context = BuildOneWeekTaperContext(
            keyStage: "TAPER_SHARPEN", keyWorkout: ("EASY_STANDARD", 4), profileKey: null, profileVersion: null);

        Assert.True(context.ValidationResult.IsValid, string.Join(", ", context.ValidationResult.Errors));
    }

    [Fact]
    public void LegacyMissingTaperSharpenIdentity_Fails()
    {
        // Legacy (no profile lineage) but the wrong stage/workout identity --
        // the exact pre-existing failure mode, unchanged.
        var context = BuildOneWeekTaperContext(
            keyStage: "SOME_OTHER_STAGE", keyWorkout: ("EASY_STANDARD", 4), profileKey: null, profileVersion: null);

        Assert.False(context.ValidationResult.IsValid);
        Assert.Contains("TAPER_SHARPEN_CONTEXT_MISSING", context.ValidationResult.Errors);
    }

    // ── §34.9: malformed 5D Legacy-classified counterexample (from FREQ.6D.4D.5C §25) ──

    [Fact]
    public void MalformedLegacyClassifiedFiveDayStage_StillFails()
    {
        // Legacy-classified (no profile lineage) but real-5D-shaped stage/workout
        // identity that is neither TAPER_SHARPEN/EASY_STANDARD -- the exact
        // counterexample FREQ.6D.4D.5C §25 constructed to prove the partition
        // does not collapse into "accept anything."
        var context = BuildOneWeekTaperContext(
            keyStage: "TAPER_SECONDARY_STAGE", keyWorkout: ("FARTLEK", 4), profileKey: null, profileVersion: null);

        Assert.False(context.ValidationResult.IsValid);
        Assert.Contains("TAPER_SHARPEN_CONTEXT_MISSING", context.ValidationResult.Errors);
    }

    // ── §34.6-7/13-14: real 5D-shaped ProfileBacked stages, no stage-name check ──

    [Fact]
    public void ProfileBackedTaperPrimaryStage_PassesWithoutTaperSharpenIdentity()
    {
        var context = BuildOneWeekTaperContext(
            keyStage: "TAPER_PRIMARY_STAGE", keyWorkout: ("GOAL_PACE_TEN_K", 2),
            profileKey: "INTERMEDIATE_5D_TAPER_PRIMARY", profileVersion: 1);

        Assert.True(context.ValidationResult.IsValid, string.Join(", ", context.ValidationResult.Errors));
    }

    [Fact]
    public void ProfileBackedTaperSecondaryStage_PassesWithoutTaperSharpenIdentity()
    {
        var context = BuildOneWeekTaperContext(
            keyStage: "TAPER_SECONDARY_STAGE", keyWorkout: ("FARTLEK", 4),
            profileKey: "INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED", profileVersion: 1);

        Assert.True(context.ValidationResult.IsValid, string.Join(", ", context.ValidationResult.Errors));
    }

    [Fact]
    public void ArbitraryValidProfileBackedStageName_DoesNotRequireTaperSharpen()
    {
        // A deliberately arbitrary, never-before-seen stage name -- proves no
        // stage-name allow-list exists (FREQ.6D.4D.5D §13): validity comes
        // from ProfileBacked classification alone, not any literal string.
        var context = BuildOneWeekTaperContext(
            keyStage: "COMPLETELY_ARBITRARY_STAGE_NAME_XYZ", keyWorkout: ("GOAL_PACE_TEN_K", 2),
            profileKey: "SOME_OTHER_REAL_PROFILE", profileVersion: 3);

        Assert.True(context.ValidationResult.IsValid, string.Join(", ", context.ValidationResult.Errors));
    }

    [Fact]
    public void FullRealFiveDayTaper_BothLanesProfileBacked_Passes()
    {
        var context = BuildTwoKeyTaperContext(
            key0Stage: "TAPER_PRIMARY_STAGE", key0Workout: ("GOAL_PACE_TEN_K", 2), key0ProfileKey: "INTERMEDIATE_5D_TAPER_PRIMARY", key0ProfileVersion: 1,
            key1Stage: "TAPER_SECONDARY_STAGE", key1Workout: ("FARTLEK", 4), key1ProfileKey: "INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED", key1ProfileVersion: 1);

        Assert.True(context.ValidationResult.IsValid, string.Join(", ", context.ValidationResult.Errors));
    }

    // ── §34.10: partial profile lineage always fails, never treated as Legacy ──

    [Theory]
    [InlineData("INTERMEDIATE_5D_TAPER_PRIMARY", null)]
    [InlineData(null, 1)]
    public void PartialProfileLineage_AlwaysFails_NeverTreatedAsLegacy(string? key, int? version)
    {
        var context = BuildOneWeekTaperContext(
            keyStage: "TAPER_PRIMARY_STAGE", keyWorkout: ("GOAL_PACE_TEN_K", 2), profileKey: key, profileVersion: version);

        Assert.False(context.ValidationResult.IsValid);
        Assert.Contains("TAPER_KEY_SESSION_PARTIAL_PROFILE_LINEAGE", context.ValidationResult.Errors);
        // Distinct failure taxonomy (FREQ.6D.4D.5D §38): partial lineage is its
        // own error, never silently folded into TAPER_SHARPEN_CONTEXT_MISSING
        // (though that may co-occur if no other completeness authority exists).
    }

    // ── §34.11-12: downstream ProfileBacked execution guarantees are untouched ──
    // (Confirmed by design/code-read, not re-implemented here per FREQ.6D.4D.5D
    // §16: CatalogPrescriptionContextValidator never inspects ExecutionPrescriptions
    // or calls ExecutionPrescriptionIndex.ResolveExact. Missing-execution-index,
    // missing-profile, and wrong-profile-version are Split C's existing, unmodified
    // CatalogSessionPrescriptionMissingExecutionPrescriptionException /
  // ExecutionPrescriptionIndex.ResolveExact fail-closed paths, downstream of and
    // independent from this context-builder-level validator -- exercised end-to-end
    // in Freq6D4D5BReal5DDarkPlanTests / real public E2E, not duplicated here.)

    // ── Structural proof: no stage-name allow-list exists in source ─────────

    [Fact]
    public void NoStageNameAllowListExists_ValidatorLogicHasNoFiveDayStageComparisons()
    {
        // The real 5D stage names may legitimately appear in doc-comment prose
        // (explaining the partition with real examples); the invariant is that
        // no *comparison* against them exists anywhere in the validator's code.
        var source = System.IO.File.ReadAllText(FindSourceFile());
        Assert.DoesNotContain("== \"TAPER_PRIMARY_STAGE\"", source);
        Assert.DoesNotContain("== \"TAPER_SECONDARY_STAGE\"", source);
        Assert.DoesNotContain("\"TAPER_PRIMARY_STAGE\" ==", source);
        Assert.DoesNotContain("\"TAPER_SECONDARY_STAGE\" ==", source);
    }

    private static string FindSourceFile()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "backend")))
        {
            dir = dir.Parent;
        }

        var repoRoot = dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
        return System.IO.Path.Combine(repoRoot, "backend", "RunningApp.Application", "RuntimeCatalog", "Prescription", "CatalogPrescriptionContextBuilder.cs");
    }

    // ─────────────────────────────────────── fixtures ───────────────────────────────────────

    private static CatalogPlanPrescriptionContext BuildOneWeekTaperContext(
        string keyStage, (string Key, int Version) keyWorkout, string? profileKey, int? profileVersion) =>
        BuildContext(new[]
        {
            KeySession(1, keyStage, keyWorkout, profileKey, profileVersion, laneOrdinal: null),
        });

    private static CatalogPlanPrescriptionContext BuildTwoKeyTaperContext(
        string key0Stage, (string Key, int Version) key0Workout, string key0ProfileKey, int key0ProfileVersion,
        string key1Stage, (string Key, int Version) key1Workout, string key1ProfileKey, int key1ProfileVersion) =>
        BuildContext(new[]
        {
            KeySession(1, key0Stage, key0Workout, key0ProfileKey, key0ProfileVersion, laneOrdinal: 0),
            KeySession(3, key1Stage, key1Workout, key1ProfileKey, key1ProfileVersion, laneOrdinal: 1),
        });

    private static BoundCatalogSession KeySession(
        int slotOrder, string stage, (string Key, int Version) workout, string? profileKey, int? profileVersion, int? laneOrdinal) => new()
    {
        WeekNumber = 1,
        Date = AsOfDate.AddDays(slotOrder),
        PhaseKey = "TAPER",
        ProgressionStageKey = stage,
        LaneOrdinal = laneOrdinal,
        PrescriptionProfileKey = profileKey,
        PrescriptionProfileVersion = profileVersion,
        StructuralRole = "KEY_SESSION",
        WorkoutDefinitionKey = workout.Key,
        WorkoutDefinitionVersion = workout.Version,
        BindingMode = CatalogWorkoutBindingMode.StageControlled,
        BindingPolicyKey = "TEST_PROGRESSION",
        BindingPolicyVersion = 1,
        SourceArtifactKey = "TEST_PROGRESSION",
        SourceArtifactVersion = 1,
        ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned,
        FallbackOrigin = null,
        BindingReason = "TEST",
    };

    private static BoundCatalogSession FixedSession(int slotOrder, string role, string workoutKey, int workoutVersion) => new()
    {
        WeekNumber = 1,
        Date = AsOfDate.AddDays(slotOrder),
        PhaseKey = "TAPER",
        ProgressionStageKey = null,
        LaneOrdinal = null,
        PrescriptionProfileKey = null,
        PrescriptionProfileVersion = null,
        StructuralRole = role,
        WorkoutDefinitionKey = workoutKey,
        WorkoutDefinitionVersion = workoutVersion,
        BindingMode = CatalogWorkoutBindingMode.FixedDefault,
        BindingPolicyKey = "TEST_PROGRESSION",
        BindingPolicyVersion = 1,
        SourceArtifactKey = "TEST_PROGRESSION",
        SourceArtifactVersion = 1,
        ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned,
        FallbackOrigin = null,
        BindingReason = "TEST",
    };

    private static CatalogPlanPrescriptionContext BuildContext(IReadOnlyList<BoundCatalogSession> keySessions)
    {
        var sessions = new List<BoundCatalogSession>(keySessions)
        {
            FixedSession(2, "EASY_SUPPORT", "EASY_STANDARD", 4),
            FixedSession(4, "EASY_SUPPORT", "EASY_STANDARD", 4),
            FixedSession(6, "LONG_RUN", "LONG_RUN_STANDARD", 4),
        };

        var boundPlan = new BoundCatalogPlan
        {
            CandidateKey = "TEST_CANDIDATE",
            CandidateVersion = 1,
            BinderVersion = "TEST_BINDER_V1",
            Weeks = new[] { new BoundCatalogWeek { WeekNumber = 1, PhaseKey = "TAPER", Sessions = sessions } },
            Trace = new WorkoutBindingDecisionTrace { Steps = Array.Empty<WorkoutBindingDecisionTraceStep>() },
        };

        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 5,
            Unit = DistanceUnit.Km,
            StartDate = AsOfDate,
            RaceDate = AsOfDate.AddDays(56),
            TargetFinishTimeSeconds = 2700,
            PreferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 30,
            RecentLongestRunKm = 10,
            RecentRunsPerWeek = 5,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 2700, RaceDate = AsOfDate.AddDays(-21) },
        };

        var resolverInput = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10d,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10d,
            StartDate = AsOfDate,
            RaceDate = AsOfDate.AddDays(56),
            TargetFinishTimeSeconds = 2700,
            DaysPerWeek = 5,
            Level = RunningBackground.Intermediate,
        };

        var candidate = new PlanCatalogCandidateSummary
        {
            CandidateKey = "TEST_CANDIDATE",
            CandidateVersion = 1,
            CandidateStatus = "DRAFT",
            CanonicalDistanceFamily = "TEN_K",
            Level = "INTERMEDIATE",
            DaysPerWeek = 5,
            CoreCycle = new PlanCatalogCoreCycle(8, 12, 14),
            MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 7),
            Layout = new PlanCatalogReference("RUN_LAYOUT_5D", 1),
            LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 7),
            WorkoutProgression = new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 6),
            ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER_V1", 3),
            RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
            PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 4),
            RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES", 2),
            DependencyStatuses = new Dictionary<string, string>(),
            ReferencedWorkouts = Array.Empty<PlanCatalogReference>(),
            PhaseKeys = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
            PhaseAllocations = new[] { new PlanCatalogPhaseAllocation("TAPER", 1) },
            SlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" },
        };

        var definitions = new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal)
        {
            ["EASY_STANDARD"] = Definition("EASY_STANDARD", 4, "EASY", new[] { "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["LONG_RUN_STANDARD"] = Definition("LONG_RUN_STANDARD", 4, "LONG_RUN", new[] { "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["FARTLEK"] = Definition("FARTLEK", 4, "QUALITY", new[] { "TAPER" }, new[] { "DISTANCE", "MIXED" }, new[] { "ESTIMATED_SESSION_TOTAL" }),
            ["GOAL_PACE_TEN_K"] = Definition("GOAL_PACE_TEN_K", 2, "QUALITY", new[] { "TAPER" }, new[] { "PACE_BASED" }, new[] { "ESTIMATED_SESSION_TOTAL" }),
        };

        return new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request,
            AsOfDate,
            candidate,
            resolverInput,
            new[]
            {
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "TEST"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "TEST"),
            },
            boundPlan,
            definitions));
    }

    private static CatalogWorkoutDefinitionSummary Definition(
        string key, int version, string family, IReadOnlyList<string> phases, IReadOnlyList<string> modes, IReadOnlyList<string> accountingModes) => new()
    {
        Key = key,
        Version = version,
        Status = "VALIDATED",
        Family = family,
        EligiblePhases = phases,
        AllowedPrescriptionModes = modes,
        AllowedDistanceAccountingModes = accountingModes,
        Components = Array.Empty<CatalogWorkoutComponentSummary>(),
    };
}
