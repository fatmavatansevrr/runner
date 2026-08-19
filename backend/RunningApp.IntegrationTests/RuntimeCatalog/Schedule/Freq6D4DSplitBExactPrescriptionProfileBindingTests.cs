using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split B — exact prescription-profile candidate resolution on
/// <see cref="CatalogWorkoutBinder"/>, additive to Split A's lane/stage identity. No
/// RUN_LAYOUT_5D catalog artifact exists yet (same disclosed gap Split A worked around), so
/// these exercise the binder directly with hand-constructed synthetic 2-KEY input, following
/// the same precedent as <see cref="Freq6D4DSplitADualKeyLaneStageBindingTests"/>.
/// </summary>
public sealed class Freq6D4DSplitBExactPrescriptionProfileBindingTests
{
    private static DatedGeneratedCatalogPlanSkeleton SyntheticDatedSkeleton(string phaseKey, IReadOnlyList<string> roles)
    {
        var start = new DateOnly(2026, 1, 5);
        var slots = roles.Select((role, i) => new DatedGeneratedCatalogSessionSlotSkeleton(
            i + 1, $"{role}_{i + 1}", role, start.AddDays(i), DayOfWeek.Monday,
            new CatalogSessionCalendarProvenance($"{role}_{i + 1}", role, DayOfWeek.Monday, start.AddDays(i), "TEST"))).ToList();

        var week = new DatedGeneratedCatalogWeekSkeleton(1, start, start.AddDays(6), phaseKey, 1, 1, slots,
            new CatalogWeekCalendarProvenance(1, phaseKey, 1, CatalogCalendarAssignmentPolicy.RaceHardConstraint));

        return new DatedGeneratedCatalogPlanSkeleton("1", start, start.AddDays(6), 1, new[] { week },
            new CatalogCalendarMaterializationProvenance("SYNTHETIC", 1, start, start,
                new[] { DayOfWeek.Monday }, DayOfWeek.Monday, "TEST", 1, new Dictionary<string, PlanCatalogReference>()));
    }

    private static CatalogWorkoutProgressionStage Stage(
        string key, int order, PlanCatalogReference workoutCandidate, params PlanCatalogReference[] profileCandidates) => new()
    {
        ProgressionStageKey = key, RelativeOrder = order, MinimumExposures = 1, MaximumExposures = 1,
        CompressionBehavior = CatalogStageCompressionBehavior.Compressible, ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
        Requires = Array.Empty<CatalogRuntimeEligibilityCondition>(), FallbackStageKey = null,
        WorkoutCandidateReferences = new[] { workoutCandidate },
        PrescriptionProfileCandidateKeys = profileCandidates,
    };

    private static ScheduledProgressionWeek StageWeek(string phaseKey, string stageKey, int laneOrdinal) => new()
    {
        WeekNumber = 1, PhaseKey = phaseKey, ProgressionStageKey = stageKey, LaneOrdinal = laneOrdinal,
        StageRelativeOrder = 1, ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned, AllocationReason = "TEST",
    };

    private static GeneratedCatalogStageSchedule SyntheticLaneStageSchedule(string phaseKey, params (string StageKey, int LaneOrdinal)[] lanes) => new()
    {
        CandidateKey = "SYNTHETIC", CandidateVersion = 1, ProgressionArtifactKey = "SYNTHETIC_PROGRESSION", ProgressionArtifactVersion = 1,
        AllocatorVersion = ProgressionStageAllocatorVersion.V1,
        Weeks = lanes.Select(l => StageWeek(phaseKey, l.StageKey, l.LaneOrdinal)).ToList(),
        Trace = new StageAllocationDecisionTrace { Steps = Array.Empty<StageAllocationDecisionTraceStep>() },
    };

    private sealed class FakeWorkoutDefinitionLoader(IReadOnlyDictionary<(string Key, int Version), CatalogWorkoutDefinitionSummary> byRef) : ICatalogWorkoutDefinitionLoader
    {
        public Task<CatalogWorkoutDefinitionSummary> LoadAsync(PlanCatalogReference reference, CancellationToken ct = default) =>
            byRef.TryGetValue((reference.Key, reference.Version), out var definition)
                ? Task.FromResult(definition)
                : throw new PlanCatalogLoadException($"'{reference.Key}' v{reference.Version} not found.");
    }

    private static CatalogWorkoutDefinitionSummary Definition(string key, int version, string phaseKey) => new()
    {
        Key = key, Version = version, EligiblePhases = new[] { phaseKey }, Status = "VALIDATED",
        Family = "QUALITY", AllowedPrescriptionModes = new[] { "MIXED" }, AllowedDistanceAccountingModes = new[] { "ESTIMATED_SESSION_TOTAL" },
        Components = Array.Empty<CatalogWorkoutComponentSummary>(),
    };

    private static readonly PlanCatalogReference Lane0Workout = new("LANE0_WORKOUT", 1);
    private static readonly PlanCatalogReference Lane1Workout = new("LANE1_WORKOUT", 1);
    private static readonly PlanCatalogReference EasyRef = new("EASY_STANDARD", 4);
    private static readonly PlanCatalogReference LongRunRef = new("LONG_RUN_STANDARD", 4);
    private static readonly PlanCatalogReference Lane0Profile = new("FND_PRIMARY", 1);
    private static readonly PlanCatalogReference Lane1Profile = new("FND_SECONDARY_CONTROLLED", 1);
    private static readonly PlanCatalogReference Lane1ProfileAlt = new("FND_SECONDARY_CONTROLLED", 2);

    private static IReadOnlyDictionary<(string, int), CatalogWorkoutDefinitionSummary> Definitions(string phaseKey) =>
        new Dictionary<(string, int), CatalogWorkoutDefinitionSummary>
        {
            [(Lane0Workout.Key, Lane0Workout.Version)] = Definition(Lane0Workout.Key, Lane0Workout.Version, phaseKey),
            [(Lane1Workout.Key, Lane1Workout.Version)] = Definition(Lane1Workout.Key, Lane1Workout.Version, phaseKey),
            [(EasyRef.Key, EasyRef.Version)] = Definition(EasyRef.Key, EasyRef.Version, phaseKey),
            [(LongRunRef.Key, LongRunRef.Version)] = Definition(LongRunRef.Key, LongRunRef.Version, phaseKey),
        };

    private static CatalogWorkoutBindingContext Context(string phaseKey, CatalogWorkoutProgressionStage lane0Stage, CatalogWorkoutProgressionStage lane1Stage)
    {
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "P", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = new[]
            {
                new CatalogPhaseWorkoutProgression
                {
                    PhaseKey = phaseKey,
                    Stages = Array.Empty<CatalogWorkoutProgressionStage>(),
                    Lanes = new[]
                    {
                        new CatalogWorkoutProgressionLane { LaneOrdinal = 0, Stages = new[] { lane0Stage } },
                        new CatalogWorkoutProgressionLane { LaneOrdinal = 1, Stages = new[] { lane1Stage } },
                    },
                },
            },
        };

        return new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1,
            DatedSkeleton = SyntheticDatedSkeleton(phaseKey, new[] { "KEY_SESSION", "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }),
            StageSchedule = SyntheticLaneStageSchedule(phaseKey, (lane0Stage.ProgressionStageKey, 0), (lane1Stage.ProgressionStageKey, 1)),
            Progression = progression,
            ReferencedWorkouts = new[] { Lane0Workout, Lane1Workout, EasyRef, LongRunRef },
            WorkoutDefinitionLoader = new FakeWorkoutDefinitionLoader(Definitions(phaseKey)),
        };
    }

    [Fact]
    public async Task ExactlyOneCandidate_BothLanes_BindToDistinctExactProfileKeyAndVersion()
    {
        var binder = new CatalogWorkoutBinder();
        var plan = await binder.BindAsync(Context("FOUNDATION",
            Stage("LANE0_STAGE", 1, Lane0Workout, Lane0Profile),
            Stage("LANE1_STAGE", 1, Lane1Workout, Lane1Profile)));

        var keySessions = plan.Weeks.Single().Sessions.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.LaneOrdinal).ToList();
        Assert.Equal(2, keySessions.Count);

        Assert.Equal(Lane0Profile.Key, keySessions[0].PrescriptionProfileKey);
        Assert.Equal(Lane0Profile.Version, keySessions[0].PrescriptionProfileVersion);
        Assert.Equal(Lane1Profile.Key, keySessions[1].PrescriptionProfileKey);
        Assert.Equal(Lane1Profile.Version, keySessions[1].PrescriptionProfileVersion);

        // Lane-aware, not stage-key-aware: proves resolution is keyed off the lane the stage
        // belongs to, not a coincidence of the stage's own key/content.
        Assert.NotEqual(keySessions[0].PrescriptionProfileKey, keySessions[1].PrescriptionProfileKey);
    }

    [Fact]
    public async Task ZeroCandidates_SessionRemainsLegacy_BothFieldsNull_NoError()
    {
        var binder = new CatalogWorkoutBinder();
        var plan = await binder.BindAsync(Context("FOUNDATION",
            Stage("LANE0_STAGE", 1, Lane0Workout),
            Stage("LANE1_STAGE", 1, Lane1Workout)));

        var keySessions = plan.Weeks.Single().Sessions.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
        Assert.All(keySessions, s =>
        {
            Assert.Null(s.PrescriptionProfileKey);
            Assert.Null(s.PrescriptionProfileVersion);
        });
        // Workout-definition binding (the pre-existing, unrelated authority) is unaffected.
        Assert.All(keySessions, s => Assert.NotNull(s.WorkoutDefinitionKey));
    }

    [Fact]
    public async Task MoreThanOneCandidate_ThrowsAmbiguousTypedException()
    {
        var binder = new CatalogWorkoutBinder();
        var ctx = Context("FOUNDATION",
            Stage("LANE0_STAGE", 1, Lane0Workout, Lane0Profile),
            Stage("LANE1_STAGE", 1, Lane1Workout, Lane1Profile, Lane1ProfileAlt));

        var ex = await Assert.ThrowsAsync<CatalogWorkoutBindingAmbiguousPrescriptionProfileCandidateException>(() => binder.BindAsync(ctx));
        Assert.Contains("LANE1_STAGE", ex.Message);
    }

    [Fact]
    public async Task FixedDefaultSessions_AlwaysNullProfileLineage_RegardlessOfKeySessionCandidates()
    {
        var binder = new CatalogWorkoutBinder();
        var plan = await binder.BindAsync(Context("FOUNDATION",
            Stage("LANE0_STAGE", 1, Lane0Workout, Lane0Profile),
            Stage("LANE1_STAGE", 1, Lane1Workout, Lane1Profile)));

        var fixedDefaultSessions = plan.Weeks.Single().Sessions.Where(s => s.StructuralRole != "KEY_SESSION").ToList();
        Assert.NotEmpty(fixedDefaultSessions);
        Assert.All(fixedDefaultSessions, s =>
        {
            Assert.Null(s.PrescriptionProfileKey);
            Assert.Null(s.PrescriptionProfileVersion);
        });
    }

    [Fact]
    public async Task SingleKeyLegacyLayout_NoCandidatesDeclared_RemainsLegacy_WorkoutBindingUnaffected()
    {
        // Mirrors real Intermediate×3D/4D: one KEY_SESSION slot, lane 0, no profile authoring.
        var binder = new CatalogWorkoutBinder();
        var ctx = new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1,
            DatedSkeleton = SyntheticDatedSkeleton("FOUNDATION", new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" }),
            StageSchedule = SyntheticLaneStageSchedule("FOUNDATION", ("LANE0_STAGE", 0)),
            Progression = new CatalogWorkoutProgressionDefinition
            {
                Key = "P", Version = 1, DistanceFamily = "TEN_K",
                PhaseProgressions = new[]
                {
                    new CatalogPhaseWorkoutProgression { PhaseKey = "FOUNDATION", Stages = new[] { Stage("LANE0_STAGE", 1, Lane0Workout) } },
                },
            },
            ReferencedWorkouts = new[] { Lane0Workout, EasyRef, LongRunRef },
            WorkoutDefinitionLoader = new FakeWorkoutDefinitionLoader(Definitions("FOUNDATION")),
        };

        var plan = await binder.BindAsync(ctx);
        var keySession = plan.Weeks.Single().Sessions.Single(s => s.StructuralRole == "KEY_SESSION");

        Assert.Equal(0, keySession.LaneOrdinal);
        Assert.Null(keySession.PrescriptionProfileKey);
        Assert.Null(keySession.PrescriptionProfileVersion);
        Assert.Equal(Lane0Workout.Key, keySession.WorkoutDefinitionKey);
    }
}
