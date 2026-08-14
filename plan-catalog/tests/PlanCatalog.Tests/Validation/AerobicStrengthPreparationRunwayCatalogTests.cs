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
/// Backend Integration Phase 4G.6A.4A — catalog/schema tests for the two new
/// AerobicStrength workout definitions, the workout-definition eligiblePhases
/// extension (PREPARATION_RUNWAY), and the new, dark, unreferenced
/// Preparation Runway block-progression mapping document. All assertions
/// run against the REAL repository catalog/schemas (not synthetic fixtures)
/// unless explicitly noted.
/// </summary>
public sealed class AerobicStrengthPreparationRunwayCatalogTests
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

    private const string IntroKey = "AEROBIC_STRENGTH_CONTROLLED_INTRO";
    private const string ProgressedKey = "AEROBIC_STRENGTH_CONTROLLED_PROGRESSED";

    // ══════════════════════════════════════════════════════════════════
    // 1-2. New workouts validate; every existing workout still validates.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NewAerobicStrengthWorkouts_ValidateAgainstTheCanonicalWorkoutDefinitionSchema()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());

        foreach (var file in new[] { "aerobic-strength-controlled-intro.v1.json", "aerobic-strength-controlled-progressed.v1.json" })
        {
            var json = File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file));
            var result = validator.Validate(DocumentTypes.WorkoutDefinition, json);
            Assert.True(result.IsValid, $"{file}: {string.Join("; ", result.Issues.Select(i => i.Message))}");
        }
    }

    [Fact]
    public void AllExistingAndNewWorkoutDefinitions_StillValidateAgainstTheSchema()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var files = Directory.GetFiles(Path.Combine(CatalogDirectory(), "workouts"), "*.json");

        // 18 original + 2 AerobicStrength (Phase 4G.6A.4A) + 2 Phase 4G.6A.4C
        // PREPARATION_RUNWAY-eligible version bumps (easy-standard.v5, long-run-standard.v5) = 22,
        // + 4 Phase 4I.4 LONG_HORIZON_GENERAL_ENDURANCE-eligible version bumps
        // (easy-standard.v6, long-run-standard.v6, aerobic-strength-controlled-intro.v2,
        // aerobic-strength-controlled-progressed.v2) = 26.
        Assert.Equal(26, files.Length);

        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var result = validator.Validate(DocumentTypes.WorkoutDefinition, json);
            Assert.True(result.IsValid, $"{Path.GetFileName(file)}: {string.Join("; ", result.Issues.Select(i => i.Message))}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 3-5. Preparation Runway eligibility value accepted; unknown rejected;
    // existing phase values remain accepted (backward compatibility).
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void PreparationRunwayEligibilityValue_IsAccepted()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var json = MinimalWorkoutJson(eligiblePhases: "[\"PREPARATION_RUNWAY\"]");
        var result = validator.Validate(DocumentTypes.WorkoutDefinition, json);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void UnknownEligibilityValue_IsRejected()
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var json = MinimalWorkoutJson(eligiblePhases: "[\"NOT_A_REAL_PHASE\"]");
        var result = validator.Validate(DocumentTypes.WorkoutDefinition, json);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("FOUNDATION")]
    [InlineData("BUILD")]
    [InlineData("RACE_SPECIFIC")]
    [InlineData("TAPER")]
    public void ExistingPhaseValues_RemainAccepted(string phase)
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var json = MinimalWorkoutJson(eligiblePhases: $"[\"{phase}\"]");
        var result = validator.Validate(DocumentTypes.WorkoutDefinition, json);
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    private static string MinimalWorkoutJson(string eligiblePhases) => $$"""
        {
          "metadata": { "documentType": "WORKOUT_DEFINITION", "schemaVersion": 3, "key": "SCHEMA_PROBE", "version": 1, "status": "DRAFT" },
          "family": "QUALITY",
          "eligiblePhases": {{eligiblePhases}},
          "allowedPrescriptionModes": ["MIXED"]
        }
        """;

    // ══════════════════════════════════════════════════════════════════
    // 6-12. Progression mapping: both steps validate, exactly two ordered
    // steps, step 1 precedes step 2, one/two-week catalog-contract
    // selection, no third step.
    // ══════════════════════════════════════════════════════════════════

    private static JsonSchema ProgressionSchema() => JsonSchema.FromFile(Path.Combine(SchemasDirectory(), "preparation-runway-block-progression.schema.json"));
    private static string ProgressionDocumentPath() => Path.Combine(CatalogDirectory(), "preparation-runway-progressions", "ten-k-aerobic-strength-progression.v1.json");
    private static JsonNode ProgressionDocument() => JsonNode.Parse(File.ReadAllText(ProgressionDocumentPath()))!;

    [Fact]
    public void ProgressionMapping_ValidatesAgainstItsSchema()
    {
        var evaluation = ProgressionSchema().Evaluate(ProgressionDocument(), new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(evaluation.IsValid);
    }

    [Fact]
    public void ProgressionMapping_ContainsExactlyTwoOrderedSteps_StepOnePrecedesStepTwo()
    {
        var steps = ProgressionDocument()["steps"]!.AsArray();
        Assert.Equal(2, steps.Count);

        var orders = steps.Select(s => (int)s!["stepOrder"]!).ToArray();
        Assert.Equal(new[] { 1, 2 }, orders);

        Assert.Equal(IntroKey, steps[0]!["workoutCandidates"]![0]!["key"]!.GetValue<string>());
        Assert.Equal(ProgressedKey, steps[1]!["workoutCandidates"]![0]!["key"]!.GetValue<string>());
    }

    [Fact]
    public void NoThirdAerobicStrengthStepExists()
    {
        var steps = ProgressionDocument()["steps"]!.AsArray();
        Assert.DoesNotContain(steps, s => (int)s!["stepOrder"]! == 3);
    }

    /// <summary>
    /// Illustrative catalog-contract selection proof only -- NOT a production
    /// binding. Proves the DATA supports correct one-week/two-week
    /// selection; the actual runtime binder (Phase 4G.6A.4B) is not
    /// implemented here.
    /// </summary>
    private static IReadOnlyList<string> SelectStepKeysForCatalogContractProofOnly(JsonNode document, int allocatedWeeks)
    {
        var steps = document["steps"]!.AsArray().OrderBy(s => (int)s!["stepOrder"]!).ToList();
        return steps.Take(allocatedWeeks).Select(s => s!["stepKey"]!.GetValue<string>()).ToArray();
    }

    [Fact]
    public void OneAllocatedWeek_SelectsStepOneOnly()
    {
        var selected = SelectStepKeysForCatalogContractProofOnly(ProgressionDocument(), 1);
        Assert.Equal(new[] { "AEROBIC_STRENGTH_STEP_1_INTRO" }, selected);
    }

    [Fact]
    public void TwoAllocatedWeeks_SelectsStepOneThenStepTwo()
    {
        var selected = SelectStepKeysForCatalogContractProofOnly(ProgressionDocument(), 2);
        Assert.Equal(new[] { "AEROBIC_STRENGTH_STEP_1_INTRO", "AEROBIC_STRENGTH_STEP_2_PROGRESSED" }, selected);
    }

    // ══════════════════════════════════════════════════════════════════
    // 13-18. Structural absence proofs.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NoHorizonSpecificBranchOrRunwayWeeksInspection_ExistsInAnyNewCatalogFile()
    {
        foreach (var path in NewFilePaths())
        {
            var text = File.ReadAllText(path);
            foreach (var forbidden in new[] { "RunwayWeeks", "runwayWeeks", "RUNWAY_WEEKS" })
                Assert.DoesNotContain(forbidden, text);
        }
    }

    [Fact]
    public void NoGoalPaceOrTargetFinishTimeDependency_ExistsInTheNewWorkouts()
    {
        foreach (var file in new[] { "aerobic-strength-controlled-intro.v1.json", "aerobic-strength-controlled-progressed.v1.json" })
        {
            var text = File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file));
            foreach (var forbidden in new[] { "GOAL_FEASIBILITY", "GOAL_PACE", "TargetFinishTime", "targetFinishTime", "TARGET_TIME" })
                Assert.DoesNotContain(forbidden, text);
        }
    }

    [Fact]
    public void NoRaceSpecificTagExists()
    {
        foreach (var file in new[] { "aerobic-strength-controlled-intro.v1.json", "aerobic-strength-controlled-progressed.v1.json" })
        {
            var text = File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file));
            Assert.DoesNotContain("\"RACE_SPECIFIC\"", text);
            Assert.DoesNotContain("\"family\": \"RACE\"", text);
        }
    }

    [Fact]
    public void NoThresholdDominantOrMaximalAllOutClassificationExists()
    {
        foreach (var file in new[] { "aerobic-strength-controlled-intro.v1.json", "aerobic-strength-controlled-progressed.v1.json" })
        {
            var text = File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file));
            foreach (var forbidden in new[] { "THRESHOLD", "MAXIMAL", "ALL_OUT", "SPRINT", "VO2MAX" })
                Assert.DoesNotContain(forbidden, text);
        }
    }

    private static IEnumerable<string> NewFilePaths()
    {
        yield return Path.Combine(CatalogDirectory(), "workouts", "aerobic-strength-controlled-intro.v1.json");
        yield return Path.Combine(CatalogDirectory(), "workouts", "aerobic-strength-controlled-progressed.v1.json");
        yield return ProgressionDocumentPath();
    }

    // ══════════════════════════════════════════════════════════════════
    // 19-21. Adjacency / duplication proofs.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Objective_DiffersFromGeneralEnduranceContent_FamilyIsQualityNotEasyOrLongRun()
    {
        // GeneralEndurance-family content in this catalog is EASY_STANDARD/LONG_RUN_STANDARD
        // (family EASY/LONG_RUN, no components). AerobicStrength is family QUALITY with
        // an explicit controlled-power MAIN_SET component -- structurally distinct.
        foreach (var file in new[] { "aerobic-strength-controlled-intro.v1.json", "aerobic-strength-controlled-progressed.v1.json" })
        {
            var text = File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file));
            Assert.Contains("\"family\": \"QUALITY\"", text);
            Assert.Contains("CONTROLLED_AEROBIC_POWER", text);
        }
    }

    [Fact]
    public void Objective_DiffersFromPreSpecificTransitionContent_NotEligibleForAnyCorePhase()
    {
        // PreSpecificTransition (Phase 4G.6A.2A) is content-supported via EASY_STANDARD/
        // LONG_RUN_STANDARD, both of which ARE eligible in FOUNDATION/BUILD/RACE_SPECIFIC/TAPER.
        // AerobicStrength's new workouts are eligible ONLY in PREPARATION_RUNWAY -- they can
        // never be selected for a Transition (or any Core) week by the existing eligibility
        // mechanism, structurally guaranteeing no overlap.
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        foreach (var file in new[] { "aerobic-strength-controlled-intro.v1.json", "aerobic-strength-controlled-progressed.v1.json" })
        {
            var json = File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file));
            var node = JsonNode.Parse(json)!;
            var phases = node["eligiblePhases"]!.AsArray().Select(p => p!.GetValue<string>()).ToArray();
            Assert.Equal(new[] { "PREPARATION_RUNWAY" }, phases);
        }
    }

    [Fact]
    public void CoreWeekOne_NeverReachesTheNewAerobicStrengthWorkouts_ViaRealFoundationEligibility()
    {
        // FOUNDATION_EASY_BASE (Core Week 1's real stage, ten-k-workout-progression.v5.json)
        // only ever candidates EASY_STANDARD. Its own eligiblePhases include FOUNDATION;
        // the new AerobicStrength workouts declare PREPARATION_RUNWAY only -- disjoint sets,
        // so no eligibility-based mechanism could ever substitute one for the other.
        var progression = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workout-progressions", "ten-k-workout-progression.v5.json")))!;
        var foundationPhase = progression["phaseProgressions"]!.AsArray().Single(p => p!["phaseKey"]!.GetValue<string>() == "FOUNDATION")!;
        var foundationStage = foundationPhase["stages"]!.AsArray().Single(s => s!["stageKey"]!.GetValue<string>() == "FOUNDATION_EASY_BASE")!;
        var candidateKeys = foundationStage["workoutCandidates"]!.AsArray().Select(c => c!["key"]!.GetValue<string>()).ToArray();

        Assert.Equal(new[] { "EASY_STANDARD" }, candidateKeys);
        Assert.DoesNotContain(IntroKey, candidateKeys);
        Assert.DoesNotContain(ProgressedKey, candidateKeys);
    }

    // ══════════════════════════════════════════════════════════════════
    // 22-23. Version/reference integrity; catalog index inclusion.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ProgressionMapping_ReferencesExistCatalogWorkoutsAtTheDeclaredVersion()
    {
        var progressionNode = ProgressionDocument();
        var candidates = progressionNode["steps"]!.AsArray()
            .SelectMany(s => s!["workoutCandidates"]!.AsArray())
            .Select(c => (Key: c!["key"]!.GetValue<string>(), Version: c["version"]!.GetValue<int>()))
            .ToArray();

        foreach (var (key, version) in candidates)
        {
            var expectedFile = key == IntroKey ? "aerobic-strength-controlled-intro.v1.json" : "aerobic-strength-controlled-progressed.v1.json";
            var workoutJson = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", expectedFile)))!;
            Assert.Equal(key, workoutJson["metadata"]!["key"]!.GetValue<string>());
            Assert.Equal(version, workoutJson["metadata"]!["version"]!.GetValue<int>());
        }
    }

    [Fact]
    public void RealCatalogLoader_LoadsTheTwoNewWorkoutDefinitions()
    {
        var snapshot = new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();
        Assert.Contains(snapshot.Workouts, w => w.Metadata.Key == IntroKey && w.Metadata.Version == 1);
        Assert.Contains(snapshot.Workouts, w => w.Metadata.Key == ProgressedKey && w.Metadata.Version == 1);
    }

    // ══════════════════════════════════════════════════════════════════
    // Dark reachability: the new workouts must remain unreferenced by the
    // live TEN_K progression and excluded from the real published bundle
    // for TEN_K__4D__INTERMEDIATE v10.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void NewAerobicStrengthWorkouts_AreNotReferencedByTheLiveTenKWorkoutProgression()
    {
        var progression = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workout-progressions", "ten-k-workout-progression.v5.json")))!;
        var allCandidateKeys = progression["phaseProgressions"]!.AsArray()
            .SelectMany(p => p!["stages"]!.AsArray())
            .SelectMany(s => s!["workoutCandidates"]!.AsArray())
            .Select(c => c!["key"]!.GetValue<string>())
            .ToHashSet();

        Assert.DoesNotContain(IntroKey, allCandidateKeys);
        Assert.DoesNotContain(ProgressedKey, allCandidateKeys);
    }

    [Fact]
    public void RealTenKCandidateBundle_DoesNotIncludeTheNewAerobicStrengthWorkouts()
    {
        var snapshot = new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var bundle = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher())
            .Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 10);

        Assert.DoesNotContain(bundle.Workouts, w => w.Key == IntroKey);
        Assert.DoesNotContain(bundle.Workouts, w => w.Key == ProgressedKey);
    }

    [Fact]
    public void PreparationRunwayProgressionsFolder_IsNotIteratedByTheRealCatalogLoader()
    {
        // The generic-folder loader only reads 10 fixed subfolders (confirmed by direct
        // source inspection of FileSystemCatalogSourceRepository); this document type has
        // no corresponding snapshot collection and no folder-name match, so it cannot be
        // silently pulled into any live snapshot.
        var snapshot = new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();
        var snapshotType = snapshot.GetType();
        Assert.DoesNotContain(snapshotType.GetProperties(), p => p.Name.Contains("PreparationRunway", StringComparison.Ordinal));
    }
}
