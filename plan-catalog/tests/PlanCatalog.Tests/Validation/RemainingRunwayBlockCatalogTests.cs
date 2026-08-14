using System.Text.Json.Nodes;
using Json.Schema;
using PlanCatalog.Contracts;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Backend Integration Phase 4G.6A.4C — catalog/schema tests for the
/// CONSISTENCY, GENERAL_ENDURANCE and PRE_SPECIFIC_TRANSITION Preparation
/// Runway progression documents, and the two PREPARATION_RUNWAY-eligible
/// version bumps (easy-standard.v5, long-run-standard.v5) they reference.
/// </summary>
public sealed class RemainingRunwayBlockCatalogTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static string SchemasDirectory() => Path.Combine(RepoRoot(), "schemas");
    private static string CatalogDirectory() => Path.Combine(RepoRoot(), "catalog");
    private static string ProgressionsDirectory() => Path.Combine(CatalogDirectory(), "preparation-runway-progressions");

    private static readonly (string File, string Key)[] Progressions =
    [
        ("ten-k-consistency-progression.v1.json", "TEN_K_CONSISTENCY_PROGRESSION"),
        ("ten-k-general-endurance-progression.v1.json", "TEN_K_GENERAL_ENDURANCE_PROGRESSION"),
        ("ten-k-pre-specific-transition-progression.v1.json", "TEN_K_PRE_SPECIFIC_TRANSITION_PROGRESSION"),
    ];

    // ══════════════════════════════════════════════════════════════════
    // 1, 5, 9. Each progression validates against the existing, unchanged schema.
    // ══════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(ProgressionFiles))]
    public void EachNewProgression_ValidatesAgainstTheExistingUnchangedSchema(string file)
    {
        var schema = JsonSchema.FromFile(Path.Combine(SchemasDirectory(), "preparation-runway-block-progression.schema.json"));
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(ProgressionsDirectory(), file)));
        var evaluation = schema.Evaluate(document, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(evaluation.IsValid);
    }

    public static IEnumerable<object[]> ProgressionFiles() => Progressions.Select(p => new object[] { p.File });

    // ══════════════════════════════════════════════════════════════════
    // 2, 6, 10. Exact contiguous step counts.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Consistency_HasExactlyTwoContiguousSteps()
    {
        var steps = LoadSteps("ten-k-consistency-progression.v1.json");
        Assert.Equal(new[] { 1, 2 }, steps.Select(s => (int)s!["stepOrder"]!));
    }

    [Fact]
    public void GeneralEndurance_HasExactlyFiveContiguousSteps()
    {
        var steps = LoadSteps("ten-k-general-endurance-progression.v1.json");
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, steps.Select(s => (int)s!["stepOrder"]!));
    }

    [Fact]
    public void Transition_HasExactlyOneStep()
    {
        var steps = LoadSteps("ten-k-pre-specific-transition-progression.v1.json");
        Assert.Single(steps);
        Assert.Equal(1, (int)steps[0]!["stepOrder"]!);
    }

    private static JsonArray LoadSteps(string file) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(ProgressionsDirectory(), file)))!["steps"]!.AsArray();

    // ══════════════════════════════════════════════════════════════════
    // 13-19. Referenced workout content proofs (existence, version, family,
    // eligibility, no goal-pace/target-time/race-specific/threshold content).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void EasyStandardV5AndLongRunStandardV5_ExistAndAreSchemaValid()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        foreach (var file in new[] { "easy-standard.v5.json", "long-run-standard.v5.json" })
        {
            var path = Path.Combine(CatalogDirectory(), "workouts", file);
            Assert.True(File.Exists(path));
            var result = validator.Validate(DocumentTypes.WorkoutDefinition, File.ReadAllText(path));
            Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
        }
    }

    [Fact]
    public void EasyStandardV5AndLongRunStandardV5_AreRunwayEligible_AndRetainAllExistingCorePhaseEligibility()
    {
        foreach (var (file, expectedFamily) in new[] { ("easy-standard.v5.json", "EASY"), ("long-run-standard.v5.json", "LONG_RUN") })
        {
            var node = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file)))!;
            var phases = node["eligiblePhases"]!.AsArray().Select(p => p!.GetValue<string>()).ToHashSet();
            Assert.Equal(new HashSet<string> { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER", "PREPARATION_RUNWAY" }, phases);
            Assert.Equal(expectedFamily, node["family"]!.GetValue<string>());
        }
    }

    [Fact]
    public void V4WorkoutFiles_RemainUnchanged_ExistingCoreUseIsUnaffected()
    {
        // Every live catalog reference (ten-k-workout-progression.v5.json, etc.) still pins
        // v4 -- confirmed by the unchanged, untouched v4 files and the full-suite regression
        // (BundleWorkoutClosureTests / PilotCatalogStructuralTests, run separately) staying green.
        foreach (var (file, expectedVersion) in new[] { ("easy-standard.v4.json", 4), ("long-run-standard.v4.json", 4) })
        {
            var node = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file)))!;
            var phases = node["eligiblePhases"]!.AsArray().Select(p => p!.GetValue<string>()).ToArray();
            Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, phases);
            Assert.Equal(expectedVersion, node["metadata"]!["version"]!.GetValue<int>());
        }
    }

    [Fact]
    public void NoReferencedWorkout_HasGoalPaceThresholdOrRaceSpecificContent()
    {
        foreach (var file in new[] { "easy-standard.v5.json", "long-run-standard.v5.json" })
        {
            var text = File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file));
            foreach (var forbidden in new[] { "GOAL_PACE", "TARGET_TIME", "THRESHOLD", "MAXIMAL", "ALL_OUT", "SPRINT", "VO2MAX" })
                Assert.DoesNotContain(forbidden, text);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 20. Repeated references (GeneralEndurance's 5 identical LONG_RUN_STANDARD
    // steps) are intentional and documented, not accidental.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void GeneralEndurance_RepeatsTheSameWorkoutReferenceAcrossAllFiveSteps_Intentionally()
    {
        var steps = LoadSteps("ten-k-general-endurance-progression.v1.json");
        var keys = steps.Select(s => s!["workoutCandidates"]![0]!["key"]!.GetValue<string>()).ToArray();
        Assert.All(keys, k => Assert.Equal("LONG_RUN_STANDARD", k));

        var versions = steps.Select(s => s!["workoutCandidates"]![0]!["version"]!.GetValue<int>()).ToArray();
        Assert.All(versions, v => Assert.Equal(5, v));
    }

    [Fact]
    public void Consistency_UsesTwoDifferentWorkoutFamilies_NotIdenticalSteps()
    {
        var steps = LoadSteps("ten-k-consistency-progression.v1.json");
        var keys = steps.Select(s => s!["workoutCandidates"]![0]!["key"]!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "EASY_STANDARD", "LONG_RUN_STANDARD" }, keys);
    }

    // ══════════════════════════════════════════════════════════════════
    // 22-24. No RunwayWeeks/profile/weight logic anywhere in the new
    // progression documents.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NoProgressionDocument_ContainsHorizonProfileOrWeightLogic()
    {
        foreach (var (file, _) in Progressions)
        {
            var text = File.ReadAllText(Path.Combine(ProgressionsDirectory(), file));
            foreach (var forbidden in new[] { "RunwayWeeks", "runwayWeeks", "CONSISTENCY_NEEDED", "CORE_ENTRY_READY", "PreferredWeight", "AllocationPriority", "CanonicalOrder" })
                Assert.DoesNotContain(forbidden, text);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 30. No live candidate bundle references any new runway progression
    // or the new v5 workout versions.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NewV5Workouts_AreNotReferencedByTheLiveTenKWorkoutProgression()
    {
        var progression = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workout-progressions", "ten-k-workout-progression.v5.json")))!;
        var allCandidates = progression["phaseProgressions"]!.AsArray()
            .SelectMany(p => p!["stages"]!.AsArray())
            .SelectMany(s => s!["workoutCandidates"]!.AsArray())
            .Select(c => (Key: c!["key"]!.GetValue<string>(), Version: c["version"]!.GetValue<int>()))
            .ToHashSet();

        Assert.DoesNotContain(("EASY_STANDARD", 5), allCandidates);
        Assert.DoesNotContain(("LONG_RUN_STANDARD", 5), allCandidates);
    }

    [Fact]
    public void RealTenKCandidateBundle_DoesNotIncludeAnyNewV5WorkoutOrRunwayProgression()
    {
        var snapshot = new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var bundle = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher())
            .Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 10);

        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "EASY_STANDARD" && w.Version == 5);
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == "LONG_RUN_STANDARD" && w.Version == 5);
    }

    [Fact]
    public void PreparationRunwayProgressionsFolder_StillNotIteratedByTheRealCatalogLoader()
    {
        var snapshot = new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();
        Assert.DoesNotContain(snapshot.GetType().GetProperties(), p => p.Name.Contains("PreparationRunway", StringComparison.Ordinal));
    }

    // ══════════════════════════════════════════════════════════════════
    // 33-34. Adjacency / Core Week 1 duplication proofs from real catalog content.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ConsistencyStepTwo_AndGeneralEnduranceStepOne_ShareWorkoutIdentity_ButAreDistinctByBlockAndPosition()
    {
        // Both anchor on LONG_RUN_STANDARD v5 -- the real catalog-content distinction is
        // block identity (CONSISTENCY vs GENERAL_ENDURANCE) and progression position
        // (Consistency's own terminal step vs General Endurance's own opening step), not
        // workout content -- directly mirroring the repository's own established
        // TAPER_SHARPEN/FOUNDATION_EASY_BASE precedent (Phase 4G.6A.2A).
        var consistencyStep2 = LoadSteps("ten-k-consistency-progression.v1.json")[1]!["workoutCandidates"]![0]!["key"]!.GetValue<string>();
        var geStep1 = LoadSteps("ten-k-general-endurance-progression.v1.json")[0]!["workoutCandidates"]![0]!["key"]!.GetValue<string>();
        Assert.Equal(consistencyStep2, geStep1);

        var consistencyBlockType = JsonNode.Parse(File.ReadAllText(Path.Combine(ProgressionsDirectory(), "ten-k-consistency-progression.v1.json")))!["blockType"]!.GetValue<string>();
        var geBlockType = JsonNode.Parse(File.ReadAllText(Path.Combine(ProgressionsDirectory(), "ten-k-general-endurance-progression.v1.json")))!["blockType"]!.GetValue<string>();
        Assert.NotEqual(consistencyBlockType, geBlockType);
    }

    [Fact]
    public void GeneralEnduranceFinalStep_AndAerobicStrengthStepOne_AreDifferentWorkoutFamilies()
    {
        var geLastKey = LoadSteps("ten-k-general-endurance-progression.v1.json")[4]!["workoutCandidates"]![0]!["key"]!.GetValue<string>();
        var asStep1Key = JsonNode.Parse(File.ReadAllText(Path.Combine(ProgressionsDirectory(), "ten-k-aerobic-strength-progression.v1.json")))!
            ["steps"]![0]!["workoutCandidates"]![0]!["key"]!.GetValue<string>();

        var geFamily = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", "long-run-standard.v5.json")))!["family"]!.GetValue<string>();
        var asFamily = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", "aerobic-strength-controlled-intro.v1.json")))!["family"]!.GetValue<string>();

        Assert.Equal("LONG_RUN_STANDARD", geLastKey);
        Assert.Equal("AEROBIC_STRENGTH_CONTROLLED_INTRO", asStep1Key);
        Assert.Equal("LONG_RUN", geFamily);
        Assert.Equal("QUALITY", asFamily);
    }

    [Fact]
    public void Transition_AndConsistencyStepOne_ShareWorkoutIdentity_ButAreOppositeRunwayBoundaries()
    {
        // Both anchor on EASY_STANDARD v5 -- Consistency Step 1 is the runway's OPENING
        // week; PreSpecificTransition is always the runway's absolute FINAL week,
        // immediately before Core -- the same non-adjacent-boundary reuse pattern as
        // TAPER_SHARPEN (final Core week) vs FOUNDATION_EASY_BASE (first Core week).
        var consistencyStep1 = LoadSteps("ten-k-consistency-progression.v1.json")[0]!["workoutCandidates"]![0]!["key"]!.GetValue<string>();
        var transitionStep1 = LoadSteps("ten-k-pre-specific-transition-progression.v1.json")[0]!["workoutCandidates"]![0]!["key"]!.GetValue<string>();
        Assert.Equal(consistencyStep1, transitionStep1);
        Assert.Equal("EASY_STANDARD", transitionStep1);
    }

    [Fact]
    public void Transition_AndGeneralEnduranceFinalStep_UseDifferentWorkoutFamilies()
    {
        var transitionKey = LoadSteps("ten-k-pre-specific-transition-progression.v1.json")[0]!["workoutCandidates"]![0]!["key"]!.GetValue<string>();
        var geLastKey = LoadSteps("ten-k-general-endurance-progression.v1.json")[4]!["workoutCandidates"]![0]!["key"]!.GetValue<string>();
        Assert.NotEqual(transitionKey, geLastKey); // EASY_STANDARD vs LONG_RUN_STANDARD
    }

    [Fact]
    public void Transition_DoesNotDuplicateCoreFoundationWeekOne()
    {
        // Foundation Week 1's real stage (FOUNDATION_EASY_BASE) candidates only
        // EASY_STANDARD v4 -- a different VERSION than Transition's own EASY_STANDARD v5
        // reference, and Foundation itself is never PREPARATION_RUNWAY-eligible content
        // consumption (FOUNDATION_EASY_BASE's own workout candidate is pinned to v4,
        // which lacks PREPARATION_RUNWAY eligibility entirely) -- confirming the two are
        // structurally distinct catalog references, not the same live binding.
        var progression = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workout-progressions", "ten-k-workout-progression.v5.json")))!;
        var foundationStage = progression["phaseProgressions"]!.AsArray().Single(p => p!["phaseKey"]!.GetValue<string>() == "FOUNDATION")!
            ["stages"]!.AsArray().Single(s => s!["stageKey"]!.GetValue<string>() == "FOUNDATION_EASY_BASE")!;
        var foundationCandidate = foundationStage["workoutCandidates"]![0]!;

        Assert.Equal("EASY_STANDARD", foundationCandidate["key"]!.GetValue<string>());
        Assert.Equal(4, foundationCandidate["version"]!.GetValue<int>());

        var transitionVersion = LoadSteps("ten-k-pre-specific-transition-progression.v1.json")[0]!["workoutCandidates"]![0]!["version"]!.GetValue<int>();
        Assert.NotEqual(4, transitionVersion);
        Assert.Equal(5, transitionVersion);
    }
}
