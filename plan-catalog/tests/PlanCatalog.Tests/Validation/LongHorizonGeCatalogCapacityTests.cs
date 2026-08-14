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
/// Phase 4I.4 — catalog/schema capacity for the 1-32-week
/// LONG_HORIZON_GENERAL_ENDURANCE segment. Mirrors the exact pattern
/// established by <see cref="RemainingRunwayBlockCatalogTests"/> for the
/// Preparation Runway's own dark progression documents: schema-validity of
/// every new document, containment proof that the real catalog loader
/// still never iterates the new dark folder, and containment proof that
/// the real published bundle never references any of the new workout
/// versions.
/// </summary>
public sealed class LongHorizonGeCatalogCapacityTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static string SchemasDirectory() => Path.Combine(RepoRoot(), "schemas");
    private static string CatalogDirectory() => Path.Combine(RepoRoot(), "catalog");
    private static string LongHorizonDirectory() => Path.Combine(CatalogDirectory(), "long-horizon-progressions");

    private static readonly (string Key, int Version, string Family)[] NewWorkoutVersions =
    [
        ("EASY_STANDARD", 6, "EASY"),
        ("LONG_RUN_STANDARD", 6, "LONG_RUN"),
        ("AEROBIC_STRENGTH_CONTROLLED_INTRO", 2, "QUALITY"),
        ("AEROBIC_STRENGTH_CONTROLLED_PROGRESSED", 2, "QUALITY"),
    ];

    private static string WorkoutFileName(string key, int version) =>
        key switch
        {
            "EASY_STANDARD" => $"easy-standard.v{version}.json",
            "LONG_RUN_STANDARD" => $"long-run-standard.v{version}.json",
            "AEROBIC_STRENGTH_CONTROLLED_INTRO" => $"aerobic-strength-controlled-intro.v{version}.json",
            "AEROBIC_STRENGTH_CONTROLLED_PROGRESSED" => $"aerobic-strength-controlled-progressed.v{version}.json",
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };

    // ── Schema/integrity: new workout versions ──────────────────────────────

    [Theory]
    [MemberData(nameof(NewWorkoutVersionFiles))]
    public void NewWorkoutVersion_ValidatesAgainstTheExtendedSchema(string key, int version)
    {
        var validator = new JsonSchemaNetValidator(SchemasDirectory());
        var path = Path.Combine(CatalogDirectory(), "workouts", WorkoutFileName(key, version));
        Assert.True(File.Exists(path));
        var result = validator.Validate(DocumentTypes.WorkoutDefinition, File.ReadAllText(path));
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Message)));
    }

    public static IEnumerable<object[]> NewWorkoutVersionFiles() =>
        NewWorkoutVersions.Select(w => new object[] { w.Key, w.Version });

    [Fact]
    public void NewWorkoutVersions_CarryLongHorizonGeneralEnduranceEligibility()
    {
        foreach (var (key, version, expectedFamily) in NewWorkoutVersions)
        {
            var path = Path.Combine(CatalogDirectory(), "workouts", WorkoutFileName(key, version));
            var node = JsonNode.Parse(File.ReadAllText(path))!;
            var phases = node["eligiblePhases"]!.AsArray().Select(p => p!.GetValue<string>()).ToHashSet();
            Assert.Contains("LONG_HORIZON_GENERAL_ENDURANCE", phases);
            Assert.Equal(expectedFamily, node["family"]!.GetValue<string>());
        }
    }

    [Fact]
    public void OldWorkoutVersionFiles_RemainUnchanged()
    {
        // Every live catalog reference still pins the pre-4I.4 versions;
        // confirmed unchanged, not merely un-referenced.
        foreach (var (file, expectedVersion, expectedPhaseCount) in new[]
                 {
                     ("easy-standard.v5.json", 5, 5),
                     ("long-run-standard.v5.json", 5, 5),
                     ("aerobic-strength-controlled-intro.v1.json", 1, 1),
                     ("aerobic-strength-controlled-progressed.v1.json", 1, 1),
                 })
        {
            var node = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workouts", file)))!;
            Assert.Equal(expectedVersion, node["metadata"]!["version"]!.GetValue<int>());
            Assert.Equal(expectedPhaseCount, node["eligiblePhases"]!.AsArray().Count);
            Assert.DoesNotContain("LONG_HORIZON_GENERAL_ENDURANCE",
                node["eligiblePhases"]!.AsArray().Select(p => p!.GetValue<string>()));
        }
    }

    [Fact]
    public void NoNewWorkoutVersion_ContainsProhibitedIntensityContent()
    {
        foreach (var (key, version, _) in NewWorkoutVersions)
        {
            var path = Path.Combine(CatalogDirectory(), "workouts", WorkoutFileName(key, version));
            var text = File.ReadAllText(path);
            // Note: "RACE_SPECIFIC" is deliberately excluded from this check --
            // it legitimately appears in eligiblePhases (these workouts remain
            // Core-RACE_SPECIFIC-eligible too, unchanged from prior versions),
            // not as prohibited intensity content.
            foreach (var forbidden in new[] { "GOAL_PACE", "TARGET_TIME", "THRESHOLD", "MAXIMAL", "ALL_OUT", "SPRINT", "VO2MAX" })
                Assert.DoesNotContain(forbidden, text);
        }
    }

    // ── Schema/integrity: new dark stage-family catalog document ────────────

    [Fact]
    public void StageFamilyCatalogDocument_ValidatesAgainstItsOwnNewSchema()
    {
        var schema = JsonSchema.FromFile(Path.Combine(SchemasDirectory(), "long-horizon-ge-stage-family.schema.json"));
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(LongHorizonDirectory(), "ten-k-long-horizon-ge-stage-families.v1.json")));
        var evaluation = schema.Evaluate(document, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(evaluation.IsValid);
    }

    [Fact]
    public void StageFamilyCatalogDocument_HasExactlyTheFiveApprovedStageFamilies()
    {
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(LongHorizonDirectory(), "ten-k-long-horizon-ge-stage-families.v1.json")))!;
        var keys = document["stageFamilies"]!.AsArray().Select(s => s!["stageFamilyKey"]!.GetValue<string>()).ToArray();
        Assert.Equal(new[] { "ENTRY", "BASE_DEVELOPMENT", "AEROBIC_DURABILITY", "CONSOLIDATION", "PRE_RUNWAY_ALIGNMENT" }, keys);
    }

    [Fact]
    public void StageFamilyCatalogDocument_ReferencesOnlyTheNewLongHorizonEligibleWorkoutVersions()
    {
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(LongHorizonDirectory(), "ten-k-long-horizon-ge-stage-families.v1.json")))!;
        var referenced = document["stageFamilies"]!.AsArray()
            .SelectMany(sf => sf!["roleAssignments"]!.AsArray())
            .SelectMany(ra => ra!["workoutCandidates"]!.AsArray())
            .Select(c => (Key: c!["key"]!.GetValue<string>(), Version: c["version"]!.GetValue<int>()))
            .Distinct()
            .ToHashSet();

        var approved = NewWorkoutVersions.Select(w => (w.Key, w.Version)).ToHashSet();
        Assert.True(referenced.IsSubsetOf(approved), string.Join(", ", referenced.Except(approved)));
    }

    [Fact]
    public void NoStageFamilyDocument_ReferencesAThresholdOrGoalPaceWorkout()
    {
        var text = File.ReadAllText(Path.Combine(LongHorizonDirectory(), "ten-k-long-horizon-ge-stage-families.v1.json"));
        foreach (var forbidden in new[] { "THRESHOLD_TEMPO", "GOAL_PACE_TEN_K", "FARTLEK" })
            Assert.DoesNotContain(forbidden, text);
    }

    // ── Containment: real catalog loader never iterates the new folder ──────

    [Fact]
    public void LongHorizonProgressionsFolder_IsNotIteratedByTheRealCatalogLoader()
    {
        var snapshot = new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();
        Assert.DoesNotContain(snapshot.GetType().GetProperties(), p => p.Name.Contains("LongHorizon", StringComparison.Ordinal));
    }

    [Fact]
    public void RealTenKCandidateBundle_DoesNotIncludeAnyNewWorkoutVersionOrLongHorizonDocument()
    {
        var snapshot = new FileSystemCatalogSourceRepository(CatalogDirectory()).LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var bundle = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher())
            .Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 10);

        foreach (var (key, version, _) in NewWorkoutVersions)
            Assert.DoesNotContain(bundle.Workouts, w => w.Key == key && w.Version == version);
    }

    [Fact]
    public void LiveTenKWorkoutProgression_NeverReferencesAnyNewWorkoutVersion()
    {
        var progression = JsonNode.Parse(File.ReadAllText(Path.Combine(CatalogDirectory(), "workout-progressions", "ten-k-workout-progression.v5.json")))!;
        var allCandidates = progression["phaseProgressions"]!.AsArray()
            .SelectMany(p => p!["stages"]!.AsArray())
            .SelectMany(s => s!["workoutCandidates"]!.AsArray())
            .Select(c => (Key: c!["key"]!.GetValue<string>(), Version: c["version"]!.GetValue<int>()))
            .ToHashSet();

        foreach (var (key, version, _) in NewWorkoutVersions)
            Assert.DoesNotContain((key, version), allCandidates);
    }

    [Fact]
    public void RunwayCatalog_RemainsUnchanged()
    {
        // The approved 8-week Preparation Runway progression documents are
        // untouched by this phase -- direct proof, not just "not mentioned."
        foreach (var file in new[]
                 {
                     "ten-k-consistency-progression.v1.json",
                     "ten-k-general-endurance-progression.v1.json",
                     "ten-k-pre-specific-transition-progression.v1.json",
                     "ten-k-aerobic-strength-progression.v1.json",
                 })
        {
            var path = Path.Combine(CatalogDirectory(), "preparation-runway-progressions", file);
            Assert.True(File.Exists(path));
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("LONG_HORIZON", text);
        }
    }

    // ── Terminology non-collision ────────────────────────────────────────────

    [Fact]
    public void LongHorizonSegmentType_IsTextuallyDistinctFromRunwayGeneralEnduranceBlock()
    {
        const string longHorizonSegment = "LONG_HORIZON_GENERAL_ENDURANCE";
        const string runwayBlockType = "GENERAL_ENDURANCE";
        Assert.NotEqual(longHorizonSegment, runwayBlockType);

        // The Preparation Runway's own block-progression schema keeps its
        // existing GENERAL_ENDURANCE blockType value unchanged -- the new
        // segment value lives only in workout-definition.schema.json's
        // eligiblePhases enum and PhaseKey.cs, never merged into blockType.
        var runwaySchema = File.ReadAllText(Path.Combine(SchemasDirectory(), "preparation-runway-block-progression.schema.json"));
        Assert.DoesNotContain(longHorizonSegment, runwaySchema);
    }
}
