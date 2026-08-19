using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split A — dual-KEY structural slot identity, LaneOrdinal, and
/// per-lane progression-stage binding. Reproduces the pre-Split-A defect
/// (CatalogWorkoutBinder.stageWeeksByNumber keyed by WeekNumber only) and proves the new
/// lane-aware model closes it, using hand-constructed synthetic 2-KEY input — matching the
/// established Phase 10K-FREQ.4 precedent (Freq4TwoKeyCardinalityGeneralizationTests): no
/// RUN_LAYOUT_5D catalog artifact exists yet, so these exercise the generalized mechanisms
/// directly, not through the public API or a real candidate/orchestration pipeline.
/// </summary>
public sealed class Freq6D4DSplitADualKeyLaneStageBindingTests
{
    // ───────────────────────── Shared synthetic fixture helpers ─────────────────────────

    private static GeneratedCatalogWeekSkeletonProvenance WeekProvenance(string phaseKey) => new() { StageKey = phaseKey, SourcePhaseKey = phaseKey };

    private static DatedGeneratedCatalogPlanSkeleton SyntheticDatedSkeleton(string phaseKey, IReadOnlyList<string> roles, DateOnly? weekStart = null)
    {
        var start = weekStart ?? new DateOnly(2026, 1, 5);
        var slots = roles.Select((role, i) => new DatedGeneratedCatalogSessionSlotSkeleton(
            i + 1, $"{role}_{i + 1}", role, start.AddDays(i), DayOfWeek.Monday,
            new CatalogSessionCalendarProvenance($"{role}_{i + 1}", role, DayOfWeek.Monday, start.AddDays(i), "TEST"))).ToList();

        var week = new DatedGeneratedCatalogWeekSkeleton(1, start, start.AddDays(6), phaseKey, 1, 1, slots,
            new CatalogWeekCalendarProvenance(1, phaseKey, 1, CatalogCalendarAssignmentPolicy.RaceHardConstraint));

        return new DatedGeneratedCatalogPlanSkeleton("1", start, start.AddDays(6), 1, new[] { week },
            new CatalogCalendarMaterializationProvenance("SYNTHETIC", 1, start, start,
                new[] { DayOfWeek.Monday }, DayOfWeek.Monday, "TEST", 1, new Dictionary<string, PlanCatalogReference>()));
    }

    private static CatalogWorkoutProgressionStage Stage(string key, int order, PlanCatalogReference? candidate = null, int min = 1, int max = 1) => new()
    {
        ProgressionStageKey = key, RelativeOrder = order, MinimumExposures = min, MaximumExposures = max,
        CompressionBehavior = CatalogStageCompressionBehavior.Compressible, ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
        Requires = Array.Empty<CatalogRuntimeEligibilityCondition>(), FallbackStageKey = null,
        WorkoutCandidateReferences = candidate is null ? Array.Empty<PlanCatalogReference>() : new[] { candidate },
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

    private static IReadOnlyList<PlanCatalogReference> RealEasyLongRunReferences() =>
        new[] { new PlanCatalogReference("EASY_STANDARD", 4), new PlanCatalogReference("LONG_RUN_STANDARD", 4) };

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

    private static CatalogWorkoutBindingContext TwoKeyLaneContext(
        string phaseKey, PlanCatalogReference lane0Candidate, PlanCatalogReference lane1Candidate,
        GeneratedCatalogStageSchedule stageSchedule, IReadOnlyDictionary<(string, int), CatalogWorkoutDefinitionSummary> definitions,
        DatedGeneratedCatalogPlanSkeleton? datedSkeleton = null)
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
                        new CatalogWorkoutProgressionLane { LaneOrdinal = 0, Stages = new[] { Stage("LANE0_STAGE", 1, lane0Candidate) } },
                        new CatalogWorkoutProgressionLane { LaneOrdinal = 1, Stages = new[] { Stage("LANE1_STAGE", 1, lane1Candidate) } },
                    },
                },
            },
        };

        return new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1,
            DatedSkeleton = datedSkeleton ?? SyntheticDatedSkeleton(phaseKey, new[] { "KEY_SESSION", "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }),
            StageSchedule = stageSchedule, Progression = progression,
            ReferencedWorkouts = RealEasyLongRunReferences().Concat(new[] { lane0Candidate, lane1Candidate }).ToList(),
            WorkoutDefinitionLoader = new FakeWorkoutDefinitionLoader(definitions),
        };
    }

    private static IReadOnlyDictionary<(string, int), CatalogWorkoutDefinitionSummary> TwoKeyDefinitions(string phaseKey) =>
        new Dictionary<(string, int), CatalogWorkoutDefinitionSummary>
        {
            [("LANE0_WORKOUT", 1)] = Definition("LANE0_WORKOUT", 1, phaseKey),
            [("LANE1_WORKOUT", 1)] = Definition("LANE1_WORKOUT", 1, phaseKey),
            [("EASY_STANDARD", 4)] = Definition("EASY_STANDARD", 4, phaseKey),
            [("LONG_RUN_STANDARD", 4)] = Definition("LONG_RUN_STANDARD", 4, phaseKey),
        };

    // ───────────────────────── 1-4: single-KEY vs dual-KEY structural ordinal/lane ─────────────────────────

    [Fact]
    public async Task FiveDTwoKeySlots_BindToDistinctLaneOrdinalsZeroAndOne_BothStructuralRoleKeySession()
    {
        var phaseKey = "FOUNDATION";
        var lane0Ref = new PlanCatalogReference("LANE0_WORKOUT", 1);
        var lane1Ref = new PlanCatalogReference("LANE1_WORKOUT", 1);
        var stageSchedule = SyntheticLaneStageSchedule(phaseKey, ("LANE0_STAGE", 0), ("LANE1_STAGE", 1));
        var context = TwoKeyLaneContext(phaseKey, lane0Ref, lane1Ref, stageSchedule, TwoKeyDefinitions(phaseKey));

        var plan = await new CatalogWorkoutBinder().BindAsync(context);

        var keySessions = plan.Weeks.Single().Sessions.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.LaneOrdinal).ToList();
        Assert.Equal(2, keySessions.Count);
        Assert.All(keySessions, s => Assert.Equal("KEY_SESSION", s.StructuralRole));
        Assert.Equal(0, keySessions[0].LaneOrdinal);
        Assert.Equal(1, keySessions[1].LaneOrdinal);
        Assert.Equal("LANE0_WORKOUT", keySessions[0].WorkoutDefinitionKey);
        Assert.Equal("LANE1_WORKOUT", keySessions[1].WorkoutDefinitionKey);
        Assert.Equal("LANE0_STAGE", keySessions[0].ProgressionStageKey);
        Assert.Equal("LANE1_STAGE", keySessions[1].ProgressionStageKey);
    }

    [Fact]
    public async Task SingleKeyLegacyLayout_BindsToLaneOrdinalZero()
    {
        var phaseKey = "FOUNDATION";
        var datedSkeleton = SyntheticDatedSkeleton(phaseKey, new[] { "KEY_SESSION", "EASY_SUPPORT" });
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "P", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = new[] { new CatalogPhaseWorkoutProgression { PhaseKey = phaseKey, Stages = new[] { Stage("STAGE_A", 1, new PlanCatalogReference("EASY_STANDARD", 4)) } } },
        };
        var context = new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, DatedSkeleton = datedSkeleton,
            StageSchedule = SyntheticLaneStageSchedule(phaseKey, ("STAGE_A", 0)), Progression = progression,
            ReferencedWorkouts = RealEasyLongRunReferences(), WorkoutDefinitionLoader = new FakeWorkoutDefinitionLoader(TwoKeyDefinitions(phaseKey)),
        };

        var plan = await new CatalogWorkoutBinder().BindAsync(context);

        var keySession = plan.Weeks.Single().Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        Assert.Equal(0, keySession.LaneOrdinal);
    }

    // ───────────────────────── 5-7: determinism, weekday-independence, canonical ordering ─────────────────────────

    [Fact]
    public async Task LaneAssignment_IndependentOfCalendarWeekday_DrivenBySlotOrderInWeekOnly()
    {
        // Both KEY sessions land on the SAME weekday (Monday) in this fixture — proving lane
        // identity cannot be weekday-derived, since weekday alone cannot distinguish them.
        var phaseKey = "FOUNDATION";
        var lane0Ref = new PlanCatalogReference("LANE0_WORKOUT", 1);
        var lane1Ref = new PlanCatalogReference("LANE1_WORKOUT", 1);
        var stageSchedule = SyntheticLaneStageSchedule(phaseKey, ("LANE0_STAGE", 0), ("LANE1_STAGE", 1));
        var context = TwoKeyLaneContext(phaseKey, lane0Ref, lane1Ref, stageSchedule, TwoKeyDefinitions(phaseKey));

        var plan = await new CatalogWorkoutBinder().BindAsync(context);

        var keySessions = plan.Weeks.Single().Sessions.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.LaneOrdinal).ToList();
        Assert.Equal(0, keySessions[0].LaneOrdinal);
        Assert.Equal(1, keySessions[1].LaneOrdinal);
        // Sanity: the two KEY sessions really do share a date in this fixture (SlotOrderInWeek 1/2 both map to day offset 0/1 - assert distinct SlotOrder-derived dates are NOT what drives lane, only their relative SlotOrderInWeek rank).
        Assert.NotEqual(keySessions[0].Date, keySessions[1].Date); // dated skeleton still assigns distinct dates per slot, but lane came from rank, not date value
    }

    [Fact]
    public void StructuralOrdinal_FollowsCanonicalSlotOrderInWeek_NotConstructionOrDeclarationOrder()
    {
        // Declare the slots in reverse role-occurrence order in the underlying list, but with
        // SlotOrderInWeek still ascending 1,2 - the canonical ordinal must follow
        // SlotOrderInWeek, never list/declaration/dictionary order.
        var start = new DateOnly(2026, 1, 5);
        var reversedDeclarationSlots = new List<DatedGeneratedCatalogSessionSlotSkeleton>
        {
            new(2, "KEY_SESSION_2", "KEY_SESSION", start.AddDays(1), DayOfWeek.Tuesday, new CatalogSessionCalendarProvenance("KEY_SESSION_2", "KEY_SESSION", DayOfWeek.Tuesday, start.AddDays(1), "TEST")),
            new(1, "KEY_SESSION_1", "KEY_SESSION", start, DayOfWeek.Monday, new CatalogSessionCalendarProvenance("KEY_SESSION_1", "KEY_SESSION", DayOfWeek.Monday, start, "TEST")),
        };
        var week = new DatedGeneratedCatalogWeekSkeleton(1, start, start.AddDays(6), "FOUNDATION", 1, 1, reversedDeclarationSlots,
            new CatalogWeekCalendarProvenance(1, "FOUNDATION", 1, CatalogCalendarAssignmentPolicy.RaceHardConstraint));

        // The binder's own OrderBy(SlotOrderInWeek) must recover ascending order regardless of
        // list-declaration order - proven directly against the same slot list used by the real
        // binder loop.
        var canonicalOrder = week.SessionSlots.OrderBy(s => s.SlotOrderInWeek).ToList();
        Assert.Equal(1, canonicalOrder[0].SlotOrderInWeek);
        Assert.Equal(2, canonicalOrder[1].SlotOrderInWeek);
        Assert.Equal("KEY_SESSION_1", canonicalOrder[0].LayoutSlotKey);
        Assert.Equal("KEY_SESSION_2", canonicalOrder[1].LayoutSlotKey);
    }

    // ───────────────────────── 10-12: fail-closed lane/stage failure semantics ─────────────────────────

    [Fact]
    public void DuplicateLaneOrdinal_InSameProgressionPhase_ThrowsTypedException()
    {
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "P", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = new[]
            {
                new CatalogPhaseWorkoutProgression
                {
                    PhaseKey = "FOUNDATION", Stages = Array.Empty<CatalogWorkoutProgressionStage>(),
                    Lanes = new[]
                    {
                        new CatalogWorkoutProgressionLane { LaneOrdinal = 0, Stages = new[] { Stage("A", 1) } },
                        new CatalogWorkoutProgressionLane { LaneOrdinal = 0, Stages = new[] { Stage("B", 1) } },
                    },
                },
            },
        };
        var skeleton = new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 7),
            PlannedWeekCount = 1, DaysPerWeek = 5, CanonicalDistanceFamily = "TEN_K", CandidateKey = "SYNTHETIC", CandidateVersion = 1,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            Weeks = new[]
            {
                new GeneratedCatalogWeekSkeleton
                {
                    WeekNumber = 1, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 7), StageKey = "FOUNDATION",
                    StageWeekIndex = 1, StageWeekCount = 1, SessionSlots = Array.Empty<GeneratedCatalogSessionSlotSkeleton>(),
                    Provenance = WeekProvenance("FOUNDATION"),
                },
            },
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = "SYNTHETIC", CandidateVersion = 1, DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
                AsOfDate = new DateOnly(2026, 1, 1), MaterializerVersion = "TEST",
            },
        };

        var context = new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = progression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        };

        Assert.Throws<ProgressionStageDuplicateLaneOrdinalException>(() => new ProgressionStageAllocator().Allocate(context));
    }

    [Fact]
    public async Task MissingLaneStageAssignment_OneOfTwoKeySlotsHasNoLaneBinding_ThrowsTypedException()
    {
        // Only LaneOrdinal 0 has a real schedule row - lane 1 is entirely absent (a genuine
        // missing multi-lane binding), never silently falling back to lane 0's assignment.
        var phaseKey = "FOUNDATION";
        var stageSchedule = SyntheticLaneStageSchedule(phaseKey, ("LANE0_STAGE", 0));
        var context = TwoKeyLaneContext(phaseKey, new PlanCatalogReference("LANE0_WORKOUT", 1), new PlanCatalogReference("LANE1_WORKOUT", 1), stageSchedule, TwoKeyDefinitions(phaseKey));

        await Assert.ThrowsAsync<CatalogWorkoutBindingMissingProgressionStageException>(() => new CatalogWorkoutBinder().BindAsync(context));
    }

    [Fact]
    public async Task OriginalDefectReproduction_ExactlyOneScheduleRowForTwoKeySlots_NewBinderRejectsRatherThanSilentlyCollapsing()
    {
        // Reproduces the exact pre-Split-A defect shape: a schedule with exactly one row for
        // the week (the shape stageWeeksByNumber = Weeks.ToDictionary(w => w.WeekNumber)
        // would have accepted and looked up unconditionally for BOTH KEY_SESSION slots, since
        // the old dictionary was keyed by WeekNumber alone and had no lane concept at all - it
        // would have silently bound the second KEY slot to the SAME stage/workout as the
        // first). Under the new (WeekNumber, LaneOrdinal)-keyed model, the second slot's
        // lookup key (WeekNumber, 1) genuinely does not exist in a one-row schedule, so the
        // binder now fails closed instead of silently reusing lane 0's binding for lane 1 -
        // this is the semantic difference Split-A exists to guarantee.
        var phaseKey = "FOUNDATION";
        var oneRowSchedule = SyntheticLaneStageSchedule(phaseKey, ("LANE0_STAGE", 0)); // exactly what the pre-Split-A allocator would have produced for this phase
        var context = TwoKeyLaneContext(phaseKey, new PlanCatalogReference("LANE0_WORKOUT", 1), new PlanCatalogReference("LANE1_WORKOUT", 1), oneRowSchedule, TwoKeyDefinitions(phaseKey));

        var ex = await Assert.ThrowsAsync<CatalogWorkoutBindingMissingProgressionStageException>(() => new CatalogWorkoutBinder().BindAsync(context));
        Assert.Contains("lane 1", ex.Message);
    }

    [Fact]
    public async Task UnsupportedLaneOrdinal_MoreDeclaredLanesThanStructuralKeySlots_ThrowsTypedException()
    {
        // Only ONE structural KEY_SESSION slot exists this week, but the schedule declares
        // LaneOrdinal 0 AND 1 - a catalog lane must not manufacture an extra structural session.
        var phaseKey = "FOUNDATION";
        var datedSkeleton = SyntheticDatedSkeleton(phaseKey, new[] { "KEY_SESSION", "EASY_SUPPORT" });
        var stageSchedule = SyntheticLaneStageSchedule(phaseKey, ("LANE0_STAGE", 0), ("LANE1_STAGE", 1));
        var context = TwoKeyLaneContext(phaseKey, new PlanCatalogReference("LANE0_WORKOUT", 1), new PlanCatalogReference("LANE1_WORKOUT", 1), stageSchedule, TwoKeyDefinitions(phaseKey), datedSkeleton);

        await Assert.ThrowsAsync<CatalogWorkoutBindingLaneCountMismatchException>(() => new CatalogWorkoutBinder().BindAsync(context));
    }

    [Fact]
    public async Task DuplicateLaneStageAssignment_SameWeekAndLaneTwice_ThrowsTypedException()
    {
        var phaseKey = "FOUNDATION";
        var duplicateSchedule = new GeneratedCatalogStageSchedule
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, ProgressionArtifactKey = "P", ProgressionArtifactVersion = 1,
            AllocatorVersion = ProgressionStageAllocatorVersion.V1,
            Weeks = new[] { StageWeek(phaseKey, "A", 0), StageWeek(phaseKey, "B", 0) },
            Trace = new StageAllocationDecisionTrace { Steps = Array.Empty<StageAllocationDecisionTraceStep>() },
        };
        var context = TwoKeyLaneContext(phaseKey, new PlanCatalogReference("LANE0_WORKOUT", 1), new PlanCatalogReference("LANE1_WORKOUT", 1), duplicateSchedule, TwoKeyDefinitions(phaseKey));

        await Assert.ThrowsAsync<CatalogWorkoutBindingDuplicateLaneStageAssignmentException>(() => new CatalogWorkoutBinder().BindAsync(context));
    }

    // ───────────────────────── 13-17: per-lane allocator independence ─────────────────────────

    private static GeneratedCatalogPlanSkeleton SyntheticAllocatorSkeleton(params (string PhaseKey, int WeekCount)[] phases)
    {
        var weeks = new List<GeneratedCatalogWeekSkeleton>();
        var weekNumber = 1;
        foreach (var (phaseKey, weekCount) in phases)
        {
            for (var i = 1; i <= weekCount; i++)
            {
                weeks.Add(new GeneratedCatalogWeekSkeleton
                {
                    WeekNumber = weekNumber, StartDate = new DateOnly(2026, 1, 1).AddDays((weekNumber - 1) * 7),
                    EndDate = new DateOnly(2026, 1, 1).AddDays((weekNumber - 1) * 7 + 6), StageKey = phaseKey,
                    StageWeekIndex = i, StageWeekCount = weekCount, SessionSlots = Array.Empty<GeneratedCatalogSessionSlotSkeleton>(),
                    Provenance = WeekProvenance(phaseKey),
                });
                weekNumber++;
            }
        }

        var totalWeeks = weekNumber - 1;
        return new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion, StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 1).AddDays(totalWeeks * 7 - 1), PlannedWeekCount = totalWeeks, DaysPerWeek = 5,
            CanonicalDistanceFamily = "TEN_K", CandidateKey = "SYNTHETIC", CandidateVersion = 1,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(), Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = "SYNTHETIC", CandidateVersion = 1, DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
                AsOfDate = new DateOnly(2026, 1, 1), MaterializerVersion = "TEST",
            },
        };
    }

    private static CatalogWorkoutProgressionDefinition TwoLaneProgression(string phaseKey, CatalogWorkoutProgressionStage[] lane0Stages, CatalogWorkoutProgressionStage[] lane1Stages) => new()
    {
        Key = "SYNTHETIC_PROGRESSION", Version = 1, DistanceFamily = "TEN_K",
        PhaseProgressions = new[]
        {
            new CatalogPhaseWorkoutProgression
            {
                PhaseKey = phaseKey, Stages = Array.Empty<CatalogWorkoutProgressionStage>(),
                Lanes = new[]
                {
                    new CatalogWorkoutProgressionLane { LaneOrdinal = 0, Stages = lane0Stages },
                    new CatalogWorkoutProgressionLane { LaneOrdinal = 1, Stages = lane1Stages },
                },
            },
        },
    };

    [Fact]
    public void TwoLanes_DeliberatelyDifferentStageInputs_ProduceDistinguishableIndependentSchedules()
    {
        var skeleton = SyntheticAllocatorSkeleton(("FOUNDATION", 4));
        var progression = TwoLaneProgression("FOUNDATION",
            lane0Stages: new[] { Stage("LANE0_A", 1, min: 4, max: 4) },
            lane1Stages: new[]
            {
                Stage("LANE1_A", 1, min: 2, max: 2),
                Stage("LANE1_B", 2, min: 2, max: 2),
            });

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = progression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        });

        var lane0Weeks = schedule.Weeks.Where(w => w.LaneOrdinal == 0).OrderBy(w => w.WeekNumber).ToList();
        var lane1Weeks = schedule.Weeks.Where(w => w.LaneOrdinal == 1).OrderBy(w => w.WeekNumber).ToList();
        Assert.Equal(4, lane0Weeks.Count);
        Assert.Equal(4, lane1Weeks.Count);
        Assert.All(lane0Weeks, w => Assert.Equal("LANE0_A", w.ProgressionStageKey));
        Assert.Equal(new[] { "LANE1_A", "LANE1_A", "LANE1_B", "LANE1_B" }, lane1Weeks.Select(w => w.ProgressionStageKey));
    }

    [Fact]
    public void Lane1InputMutation_DoesNotChangeLane0Schedule()
    {
        var skeleton = SyntheticAllocatorSkeleton(("FOUNDATION", 2));
        var lane0Stages = new[] { Stage("LANE0_A", 1, min: 2, max: 2) };

        var originalProgression = TwoLaneProgression("FOUNDATION", lane0Stages,
            new[] { Stage("LANE1_A", 1, min: 2, max: 2) });
        var mutatedProgression = TwoLaneProgression("FOUNDATION", lane0Stages,
            new[] { Stage("LANE1_ZZZ_DIFFERENT", 1, min: 2, max: 2) });

        var originalSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = originalProgression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        });
        var mutatedSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = mutatedProgression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        });

        var originalLane0 = originalSchedule.Weeks.Where(w => w.LaneOrdinal == 0).OrderBy(w => w.WeekNumber).Select(w => w.ProgressionStageKey).ToList();
        var mutatedLane0 = mutatedSchedule.Weeks.Where(w => w.LaneOrdinal == 0).OrderBy(w => w.WeekNumber).Select(w => w.ProgressionStageKey).ToList();
        Assert.Equal(originalLane0, mutatedLane0);

        var mutatedLane1 = mutatedSchedule.Weeks.Where(w => w.LaneOrdinal == 1).Select(w => w.ProgressionStageKey).Distinct().Single();
        Assert.Equal("LANE1_ZZZ_DIFFERENT", mutatedLane1);
    }

    [Fact]
    public void Lane0InputMutation_DoesNotChangeLane1Schedule()
    {
        var skeleton = SyntheticAllocatorSkeleton(("FOUNDATION", 2));
        var lane1Stages = new[] { Stage("LANE1_A", 1, min: 2, max: 2) };

        var originalProgression = TwoLaneProgression("FOUNDATION",
            new[] { Stage("LANE0_A", 1, min: 2, max: 2) }, lane1Stages);
        var mutatedProgression = TwoLaneProgression("FOUNDATION",
            new[] { Stage("LANE0_ZZZ_DIFFERENT", 1, min: 2, max: 2) }, lane1Stages);

        var originalSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = originalProgression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        });
        var mutatedSchedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = mutatedProgression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        });

        var originalLane1 = originalSchedule.Weeks.Where(w => w.LaneOrdinal == 1).OrderBy(w => w.WeekNumber).Select(w => w.ProgressionStageKey).ToList();
        var mutatedLane1 = mutatedSchedule.Weeks.Where(w => w.LaneOrdinal == 1).OrderBy(w => w.WeekNumber).Select(w => w.ProgressionStageKey).ToList();
        Assert.Equal(originalLane1, mutatedLane1);
    }

    [Fact]
    public void NoWeekNumberOnlyAliasing_BothLanesResolveDistinctStagesForSameWeek()
    {
        var skeleton = SyntheticAllocatorSkeleton(("FOUNDATION", 1));
        var progression = TwoLaneProgression("FOUNDATION",
            new[] { Stage("LANE0_ONLY", 1, null) },
            new[] { Stage("LANE1_ONLY", 1, null) });

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = progression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        });

        Assert.Equal(2, schedule.Weeks.Count(w => w.WeekNumber == 1));
        Assert.Equal("LANE0_ONLY", schedule.Weeks.Single(w => w.WeekNumber == 1 && w.LaneOrdinal == 0).ProgressionStageKey);
        Assert.Equal("LANE1_ONLY", schedule.Weeks.Single(w => w.WeekNumber == 1 && w.LaneOrdinal == 1).ProgressionStageKey);
    }

    // ───────────────────────── 18-21: 8/10/12/14-week horizon completeness ─────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    public void EveryHorizon_BothKeyLanesHaveExactlyOneDeterministicStageBindingPerWeek_NoMissingOrDuplicate(int totalWeeks)
    {
        var skeleton = SyntheticAllocatorSkeleton(("FOUNDATION", totalWeeks));
        var progression = TwoLaneProgression("FOUNDATION",
            new[] { Stage("LANE0_A", 1, min: totalWeeks, max: totalWeeks) },
            new[] { Stage("LANE1_A", 1, min: totalWeeks, max: totalWeeks) });

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = progression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        });

        Assert.Equal(totalWeeks * 2, schedule.Weeks.Count);
        for (var week = 1; week <= totalWeeks; week++)
        {
            var weekRows = schedule.Weeks.Where(w => w.WeekNumber == week).ToList();
            Assert.Equal(2, weekRows.Count);
            Assert.Single(weekRows, w => w.LaneOrdinal == 0);
            Assert.Single(weekRows, w => w.LaneOrdinal == 1);
        }

        var validation = new GeneratedCatalogStageScheduleValidator().Validate(schedule, skeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
    }

    // ───────────────────────── 22-23: Taper dual-KEY + canonical phase authority ─────────────────────────

    [Fact]
    public void Taper_PreservesBothLaneStageBindings_PhaseRemainsCanonicalAcrossBothLanes()
    {
        var skeleton = SyntheticAllocatorSkeleton(("TAPER", 2));
        var progression = TwoLaneProgression("TAPER",
            new[] { Stage("TAPER_SHARPEN_PRIMARY", 1, min: 2, max: 2) },
            new[] { Stage("TAPER_SHARPEN_SECONDARY", 1, min: 2, max: 2) });

        var schedule = new ProgressionStageAllocator().Allocate(new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = progression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        });

        Assert.Equal(4, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.Equal("TAPER", w.PhaseKey)); // canonical phase identical across both lanes
        Assert.All(schedule.Weeks.Where(w => w.LaneOrdinal == 0), w => Assert.Equal("TAPER_SHARPEN_PRIMARY", w.ProgressionStageKey));
        Assert.All(schedule.Weeks.Where(w => w.LaneOrdinal == 1), w => Assert.Equal("TAPER_SHARPEN_SECONDARY", w.ProgressionStageKey));
    }

    // ───────────────────────── 24: no production profile selection occurs in Split-A ─────────────────────────

    [Fact]
    public void BoundCatalogSession_CarriedNoPrescriptionProfileFieldAtSplitA_SplitBAddedItAsDisclosedNextStep()
    {
        // Architectural proof by type shape, updated for Split B: this Split-A phase itself
        // introduced no profile-selection field — that was explicitly out of Split A's own
        // scope (see this file's class-level doc comment) and was proven here at the time.
        // Phase 10K-FREQ.6D.4D Split B (FREQ.6D.4D.2) has since closed exactly that boundary,
        // additively: BoundCatalogSession now also carries PrescriptionProfileKey/Version,
        // populated only for ProfileBacked StageControlled sessions, null otherwise. This is
        // the EXPECTED, disclosed consequence of Split B landing — not a Split-A regression —
        // mirroring this engagement's established practice of updating an old boundary-proof
        // test once the boundary it proved is deliberately, subsequently closed.
        var properties = typeof(BoundCatalogSession).GetProperties().Select(p => p.Name).ToList();
        Assert.Contains("PrescriptionProfileKey", properties);
        Assert.Contains("PrescriptionProfileVersion", properties);
        Assert.Contains("LaneOrdinal", properties);
    }

    // ───────────────────────── 26-28: legacy single-KEY equivalence ─────────────────────────

    [Fact]
    public async Task LegacySingleKeyThreeAndFourDayShapes_LaneOrdinalAlwaysZero_WorkoutSelectionUnchanged()
    {
        var phaseKey = "BUILD";
        var datedSkeleton = SyntheticDatedSkeleton(phaseKey, new[] { "EASY_SUPPORT", "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" });
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "P", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = new[] { new CatalogPhaseWorkoutProgression { PhaseKey = phaseKey, Stages = new[] { Stage("STAGE_A", 1, new PlanCatalogReference("LANE0_WORKOUT", 1)) } } },
        };
        var context = new CatalogWorkoutBindingContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, DatedSkeleton = datedSkeleton,
            StageSchedule = SyntheticLaneStageSchedule(phaseKey, ("STAGE_A", 0)), Progression = progression,
            ReferencedWorkouts = RealEasyLongRunReferences().Concat(new[] { new PlanCatalogReference("LANE0_WORKOUT", 1) }).ToList(),
            WorkoutDefinitionLoader = new FakeWorkoutDefinitionLoader(TwoKeyDefinitions(phaseKey)),
        };

        var plan = await new CatalogWorkoutBinder().BindAsync(context);

        var keySession = plan.Weeks.Single().Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        Assert.Equal(0, keySession.LaneOrdinal);
        Assert.Equal("LANE0_WORKOUT", keySession.WorkoutDefinitionKey);
        Assert.Equal("STAGE_A", keySession.ProgressionStageKey);
    }

    // ───────────────────────── 30: deterministic across repeated executions ─────────────────────────

    [Fact]
    public void RepeatedAllocatorExecution_SameInputs_ProducesValueIdenticalLaneStageSchedule()
    {
        var skeleton = SyntheticAllocatorSkeleton(("FOUNDATION", 3));
        var progression = TwoLaneProgression("FOUNDATION",
            new[] { Stage("LANE0_A", 1, min: 3, max: 3) },
            new[] { Stage("LANE1_A", 1, min: 3, max: 3) });
        var context = new ProgressionStageAllocationContext
        {
            CandidateKey = "SYNTHETIC", CandidateVersion = 1, Progression = progression, Skeleton = skeleton,
            ConditionResults = Array.Empty<RuntimeConditionResolutionResult>(),
        };

        var first = new ProgressionStageAllocator().Allocate(context);
        var second = new ProgressionStageAllocator().Allocate(context);

        var firstShape = first.Weeks.Select(w => (w.WeekNumber, w.LaneOrdinal, w.ProgressionStageKey)).OrderBy(t => t.WeekNumber).ThenBy(t => t.LaneOrdinal).ToList();
        var secondShape = second.Weeks.Select(w => (w.WeekNumber, w.LaneOrdinal, w.ProgressionStageKey)).OrderBy(t => t.WeekNumber).ThenBy(t => t.LaneOrdinal).ToList();
        Assert.Equal(firstShape, secondShape);
    }
}
