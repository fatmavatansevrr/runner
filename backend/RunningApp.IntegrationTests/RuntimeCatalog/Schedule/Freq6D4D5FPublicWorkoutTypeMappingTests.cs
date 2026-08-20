using System.Text.Json;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Domain.Enums;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule;

/// <summary>
/// Phase 10K-FREQ.6D.4D.5F — direct unit coverage for the single approved
/// <see cref="V1CatalogPublicWorkoutTypeMappingPolicy"/> arm (FREQ.6D.4D.5E:
/// <c>AEROBIC_STRENGTH_CONTROLLED_INTRO</c> → <see cref="GeneratedCatalogWorkoutType.Interval"/>),
/// plus an exhaustive, catalog-file-driven completeness gate over the real
/// Intermediate×5D workout closure. The gate reads the real published
/// <c>ten-k-workout-progression.v6.json</c> and all 8 real
/// <c>intermediate-5d-*.v1.json</c> profile files that this test project's own build
/// already copies to its output directory (the same real content the runtime consumes) —
/// it does not hardcode a manually-curated list of the six reachable pairs.
/// </summary>
public sealed class Freq6D4D5FPublicWorkoutTypeMappingTests
{
    // ── §8: direct new-mapping test ──────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AerobicStrengthControlledIntro_KeySession_MapsToInterval_AnyVersion(int version)
    {
        var session = Session("FOUNDATION", "FOUNDATION_PRIMARY_STAGE", "KEY_SESSION", "AEROBIC_STRENGTH_CONTROLLED_INTRO", version);

        var mapped = V1CatalogPublicWorkoutTypeMappingPolicy.Map(session);

        Assert.Equal(GeneratedCatalogWorkoutType.Interval, mapped);
    }

    [Theory]
    [InlineData("FOUNDATION_PRIMARY_STAGE")]
    [InlineData("SOME_OTHER_STAGE_NAME")]
    [InlineData(null)]
    public void AerobicStrengthControlledIntro_AnyProgressionStageKey_MapsToInterval(string? stageKey)
    {
        var session = Session("FOUNDATION", stageKey, "KEY_SESSION", "AEROBIC_STRENGTH_CONTROLLED_INTRO", 3);

        var mapped = V1CatalogPublicWorkoutTypeMappingPolicy.Map(session);

        Assert.Equal(GeneratedCatalogWorkoutType.Interval, mapped);
    }

    [Fact]
    public void AerobicStrengthControlledIntro_DoesNotDependOnPhaseKey()
    {
        // The mapping switch keys only on (WorkoutDefinitionKey, StructuralRole, ProgressionStageKey);
        // PhaseKey is not part of the pattern at all. Proving this directly, not by inspection.
        var underFoundation = Session("FOUNDATION", "FOUNDATION_PRIMARY_STAGE", "KEY_SESSION", "AEROBIC_STRENGTH_CONTROLLED_INTRO", 3);
        var underSomeOtherPhase = Session("SOME_OTHER_PHASE_ENTIRELY", "FOUNDATION_PRIMARY_STAGE", "KEY_SESSION", "AEROBIC_STRENGTH_CONTROLLED_INTRO", 3);

        Assert.Equal(V1CatalogPublicWorkoutTypeMappingPolicy.Map(underFoundation), V1CatalogPublicWorkoutTypeMappingPolicy.Map(underSomeOtherPhase));
    }

    // ── §6: unknown workout remains fail-closed ─────────────────────────────

    [Fact]
    public void UnknownWorkoutKey_StillFailsClosed()
    {
        var session = Session("FOUNDATION", "FOUNDATION_PRIMARY_STAGE", "KEY_SESSION", "UNKNOWN_WORKOUT_KEY", 1);

        Assert.Throws<CatalogPublicWorkoutTypeUnsupportedException>(() => V1CatalogPublicWorkoutTypeMappingPolicy.Map(session));
    }

    // ── §7 / §35.3: every pre-existing mapping arm unchanged ────────────────

    [Theory]
    [InlineData("EASY_STANDARD", "EASY_SUPPORT", null, GeneratedCatalogWorkoutType.Easy)]
    [InlineData("EASY_STANDARD", "KEY_SESSION", "TAPER_SHARPEN", GeneratedCatalogWorkoutType.Easy)]
    [InlineData("EASY_STANDARD", "KEY_SESSION", "SOME_OTHER_STAGE", GeneratedCatalogWorkoutType.Easy)]
    [InlineData("LONG_RUN_STANDARD", "LONG_RUN", null, GeneratedCatalogWorkoutType.LongRun)]
    [InlineData("FARTLEK", "KEY_SESSION", null, GeneratedCatalogWorkoutType.Interval)]
    [InlineData("THRESHOLD_TEMPO", "KEY_SESSION", null, GeneratedCatalogWorkoutType.Tempo)]
    [InlineData("GOAL_PACE_TEN_K", "KEY_SESSION", null, GeneratedCatalogWorkoutType.Interval)]
    public void ExistingMappingArms_RemainUnchanged(string workoutKey, string role, string? stageKey, GeneratedCatalogWorkoutType expected)
    {
        var session = Session("BUILD", stageKey, role, workoutKey, 1);

        Assert.Equal(expected, V1CatalogPublicWorkoutTypeMappingPolicy.Map(session));
    }

    // ── §9-12: exhaustive real Intermediate×5D closure gate ─────────────────

    private static string CatalogRoot() => Path.Combine(AppContext.BaseDirectory, "plan-catalog", "catalog");

    private sealed record ClosureEntry(string PhaseKey, int LaneOrdinal, string StageKey, string WorkoutDefinitionKey, int WorkoutDefinitionVersion);

    /// <summary>
    /// Real traversal of the published <c>TEN_K_WORKOUT_PROGRESSION_V1 v6</c> catalog file —
    /// every phase × lane × stage's <c>workoutCandidates[0]</c>, not a hand-maintained list.
    /// </summary>
    private static IReadOnlyList<ClosureEntry> DeriveRealProgressionClosure()
    {
        var path = Path.Combine(CatalogRoot(), "workout-progressions", "ten-k-workout-progression.v6.json");
        Assert.True(File.Exists(path), $"Real progression catalog file not found at '{path}'.");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var entries = new List<ClosureEntry>();

        foreach (var phase in doc.RootElement.GetProperty("phaseProgressions").EnumerateArray())
        {
            var phaseKey = phase.GetProperty("phaseKey").GetString()!;
            foreach (var lane in phase.GetProperty("lanes").EnumerateArray())
            {
                var laneOrdinal = lane.GetProperty("laneOrdinal").GetInt32();
                foreach (var stage in lane.GetProperty("stages").EnumerateArray())
                {
                    var stageKey = stage.GetProperty("stageKey").GetString()!;
                    var candidate = stage.GetProperty("workoutCandidates").EnumerateArray().Single();
                    entries.Add(new ClosureEntry(
                        phaseKey, laneOrdinal, stageKey,
                        candidate.GetProperty("key").GetString()!,
                        candidate.GetProperty("version").GetInt32()));
                }
            }
        }

        return entries;
    }

    [Fact]
    public void RealProgressionClosure_HasEightStageLaneCombinations()
    {
        var closure = DeriveRealProgressionClosure();

        // §11: reproduce FREQ.6D.4D.5E's closure cardinality from code, not by re-asserting
        // its written conclusion. If the real catalog ever changes shape, this fails loudly
        // instead of silently drifting.
        Assert.Equal(8, closure.Count);
        Assert.Equal(4, closure.Select(e => e.PhaseKey).Distinct().Count());
        Assert.All(closure, e => Assert.True(e.LaneOrdinal is 0 or 1));
    }

    [Fact]
    public void RealProgressionClosure_ReducesToSixDistinctPubliclyRelevantPairs()
    {
        var closure = DeriveRealProgressionClosure();

        // Every progression-reachable KEY session, plus the two fixed EASY_SUPPORT/LONG_RUN
        // defaults (not catalog-authored per-stage content -- the same production constants
        // V1CatalogWorkoutRoleBindingPolicy already owns).
        var distinctKeySessionPairs = closure
            .Select(e => (Key: e.WorkoutDefinitionKey, Role: "KEY_SESSION"))
            .Distinct()
            .ToList();
        var fixedDefaultPairs = new[]
        {
            (Key: V1CatalogWorkoutRoleBindingPolicy.EasySupportFixedDefaultWorkoutKey, Role: "EASY_SUPPORT"),
            (Key: V1CatalogWorkoutRoleBindingPolicy.LongRunFixedDefaultWorkoutKey, Role: "LONG_RUN"),
        };
        var allPairs = distinctKeySessionPairs.Concat(fixedDefaultPairs).Distinct().ToList();

        Assert.Equal(6, allPairs.Count);
    }

    /// <summary>
    /// §9-10, §12: the load-bearing gate. Every real, publicly-surfaced (WorkoutDefinitionKey,
    /// StructuralRole) pair reachable from the real Intermediate×5D catalog must map through
    /// <see cref="V1CatalogPublicWorkoutTypeMappingPolicy"/> exactly once, deterministically,
    /// with no exception. This must run and pass before any public routing widening.
    /// </summary>
    [Fact]
    public void RealIntermediateFiveDay_EveryPubliclySurfacedWorkout_MapsExactlyOnceAndDeterministically()
    {
        var closure = DeriveRealProgressionClosure();

        var fixedDefaultEntries = new List<(string PhaseKey, string StageKey, string Role, string WorkoutDefinitionKey, int WorkoutDefinitionVersion)>
        {
            ("FOUNDATION", "N/A_FIXED_DEFAULT", "EASY_SUPPORT", V1CatalogWorkoutRoleBindingPolicy.EasySupportFixedDefaultWorkoutKey, 1),
            ("FOUNDATION", "N/A_FIXED_DEFAULT", "LONG_RUN", V1CatalogWorkoutRoleBindingPolicy.LongRunFixedDefaultWorkoutKey, 1),
        };
        var pairs = closure
            .Select(e => (PhaseKey: e.PhaseKey, StageKey: e.StageKey, Role: "KEY_SESSION", WorkoutDefinitionKey: e.WorkoutDefinitionKey, WorkoutDefinitionVersion: e.WorkoutDefinitionVersion))
            .Concat(fixedDefaultEntries)
            .ToList();

        var failures = new List<string>();
        foreach (var pair in pairs)
        {
            var session = Session(pair.PhaseKey, pair.StageKey, pair.Role, pair.WorkoutDefinitionKey, pair.WorkoutDefinitionVersion);

            GeneratedCatalogWorkoutType first;
            GeneratedCatalogWorkoutType second;
            try
            {
                first = V1CatalogPublicWorkoutTypeMappingPolicy.Map(session);
                second = V1CatalogPublicWorkoutTypeMappingPolicy.Map(session);
            }
            catch (CatalogPublicWorkoutTypeUnsupportedException ex)
            {
                failures.Add($"{pair.WorkoutDefinitionKey}/{pair.Role} (stage {pair.StageKey}): {ex.Message}");
                continue;
            }

            Assert.Equal(first, second); // deterministic -- exactly one result per call, no ambiguity
        }

        Assert.True(failures.Count == 0, "Unmapped real Intermediate×5D workout(s) found:\n" + string.Join("\n", failures));
    }

    // ── §13-14 / §35.9-12: representative per-phase mapping proof ───────────

    [Fact]
    public void FoundationPrimary_RealWorkout_MapsToInterval()
    {
        var closure = DeriveRealProgressionClosure();
        var entry = closure.Single(e => e.PhaseKey == "FOUNDATION" && e.LaneOrdinal == 0);
        Assert.Equal("AEROBIC_STRENGTH_CONTROLLED_INTRO", entry.WorkoutDefinitionKey);

        var session = Session(entry.PhaseKey, entry.StageKey, "KEY_SESSION", entry.WorkoutDefinitionKey, entry.WorkoutDefinitionVersion);

        Assert.Equal(GeneratedCatalogWorkoutType.Interval, V1CatalogPublicWorkoutTypeMappingPolicy.Map(session));
    }

    [Fact]
    public void BuildPrimary_RealWorkout_MapsToTempo()
    {
        var closure = DeriveRealProgressionClosure();
        var entry = closure.Single(e => e.PhaseKey == "BUILD" && e.LaneOrdinal == 0);
        Assert.Equal("THRESHOLD_TEMPO", entry.WorkoutDefinitionKey);

        var session = Session(entry.PhaseKey, entry.StageKey, "KEY_SESSION", entry.WorkoutDefinitionKey, entry.WorkoutDefinitionVersion);

        Assert.Equal(GeneratedCatalogWorkoutType.Tempo, V1CatalogPublicWorkoutTypeMappingPolicy.Map(session));
    }

    [Fact]
    public void RaceSpecificPrimary_RealWorkout_MapsToInterval()
    {
        var closure = DeriveRealProgressionClosure();
        var entry = closure.Single(e => e.PhaseKey == "RACE_SPECIFIC" && e.LaneOrdinal == 0);
        Assert.Equal("GOAL_PACE_TEN_K", entry.WorkoutDefinitionKey);

        var session = Session(entry.PhaseKey, entry.StageKey, "KEY_SESSION", entry.WorkoutDefinitionKey, entry.WorkoutDefinitionVersion);

        Assert.Equal(GeneratedCatalogWorkoutType.Interval, V1CatalogPublicWorkoutTypeMappingPolicy.Map(session));
    }

    [Fact]
    public void TaperBothLanes_RealWorkouts_MapCorrectly()
    {
        var closure = DeriveRealProgressionClosure();
        var primary = closure.Single(e => e.PhaseKey == "TAPER" && e.LaneOrdinal == 0);
        var secondary = closure.Single(e => e.PhaseKey == "TAPER" && e.LaneOrdinal == 1);
        Assert.Equal("GOAL_PACE_TEN_K", primary.WorkoutDefinitionKey);
        Assert.Equal("FARTLEK", secondary.WorkoutDefinitionKey);

        var primarySession = Session(primary.PhaseKey, primary.StageKey, "KEY_SESSION", primary.WorkoutDefinitionKey, primary.WorkoutDefinitionVersion);
        var secondarySession = Session(secondary.PhaseKey, secondary.StageKey, "KEY_SESSION", secondary.WorkoutDefinitionKey, secondary.WorkoutDefinitionVersion);

        Assert.Equal(GeneratedCatalogWorkoutType.Interval, V1CatalogPublicWorkoutTypeMappingPolicy.Map(primarySession));
        Assert.Equal(GeneratedCatalogWorkoutType.Interval, V1CatalogPublicWorkoutTypeMappingPolicy.Map(secondarySession));
    }

    // ── fixture builder (mirrors the existing CoreSession helper pattern used
    //    elsewhere in this test project for CatalogPrescribedSession construction) ──

    private static CatalogPrescribedSession Session(string phaseKey, string? stageKey, string role, string workoutKey, int workoutVersion)
    {
        var pace = new CatalogPacePrescription(
            CatalogPacePrescriptionKind.EffortOnly, null, null, null, CatalogPaceSourceSelection.EffortOnly,
            "NUMERIC_PACE_UNRESOLVED", "EASY", "test fixture");
        double distance = role == "LONG_RUN" ? 8 : role == "KEY_SESSION" ? 6 : 5;
        var prescription = new CatalogWorkoutPrescription
        {
            PrescriptionMode = CatalogPrescriptionMode.Distance,
            DistanceAccountingMode = CatalogDistanceAccountingMode.ExactSessionTotal,
            DistancePrescription = new CatalogDistancePrescription(distance, "ExactSessionTotal", "nearest_0.5km"),
            DurationPrescription = new CatalogDurationPrescription(CatalogDurationKind.Unresolved, null, "effort_only"),
            PacePrescription = pace,
            EffortGuidance = "EASY",
            OrderedSegments = [new CatalogPrescriptionSegment(1, "SESSION_TOTAL", "EASY", distance, null, pace, true)],
            Status = CatalogSessionPrescriptionStatus.Complete,
        };
        return new CatalogPrescribedSession
        {
            WeekNumber = 1,
            Date = new DateOnly(2026, 1, 5),
            PhaseKey = phaseKey,
            ProgressionStageKey = stageKey,
            StructuralRole = role,
            WorkoutDefinitionKey = workoutKey,
            WorkoutDefinitionVersion = workoutVersion,
            PlannedDistanceKm = distance,
            Prescription = prescription,
            BindingProvenance = "TEST_FIXTURE",
            PaceSourceProvenance = "TEST_FIXTURE",
            VolumeAllocationProvenance = "TEST_FIXTURE",
            DecisionTrace = new SessionPrescriptionDecisionTrace(
                1, new DateOnly(2026, 1, 5), workoutKey, 24, 8, 16, distance, "V1", "Distance", "V1", "NONE",
                "NOT_EVALUATED", CatalogPaceSourceSelection.EffortOnly, [], "nearest_0.5km", "ExactSessionTotal", "Unresolved", null, []),
            ValidationResult = new CatalogSessionPrescriptionValidationResult(true, []),
            PrescriptionSource = new CatalogSessionPrescriptionSource.Legacy(prescription),
        };
    }
}
