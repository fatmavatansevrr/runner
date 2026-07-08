using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// Milestone F: immutable candidate version cascade, against the REAL catalog's new candidate artifacts
/// (TEN_K_WORKOUT_PROGRESSION_V1 v2, INTERMEDIATE_MODIFIER v2, TEN_K_MASTER v3,
/// TEN_K__4D__INTERMEDIATE v4). Tests 33-36 of the Part 2 required-tests list — see
/// artifacts/audits/deterministic-graph-part2-migration.md.
/// </summary>
public sealed class CandidateArtifactTests
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

    private static Core.Catalog.CatalogSourceSnapshot LoadSnapshot() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();

    private static string ComputeHash<T>(T document)
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        return CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, document!, "contentHash");
    }

    [Fact]
    public void ExistingPublishedArtifacts_RemainByteForByteAndHashUnchanged()
    {
        // Test 33 — spot-check the artifacts most at risk of accidental in-place edit by this migration.
        var snapshot = LoadSnapshot();

        var progressionV1 = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 1);
        Assert.Equal("a4856b47bf385ad29c148412480620b2584ddf7b0e0fa177664dc3455baf6281", ComputeHash(progressionV1));

        var levelModifierV1 = snapshot.LevelModifiers.Single(l => l.Metadata.Key == "INTERMEDIATE_MODIFIER" && l.Metadata.Version == 1);
        Assert.Equal("c5e9d601f2756495c921676cb323872539eb65f901135b1240e27034861bff34", ComputeHash(levelModifierV1));

        var masterV2 = snapshot.PlanTemplates.Single(t => t.Metadata.Key == "TEN_K_MASTER" && t.Metadata.Version == 2);
        Assert.Equal(1, masterV2.Metadata.SchemaVersion);
        Assert.NotNull(masterV2.RequiredRules);

        var combinationV3 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 3);
        Assert.Equal(2, combinationV3.RulePack.Version);
    }

    [Fact]
    public void EveryNewCandidateArtifact_Validates()
    {
        // Test 34.
        var snapshot = LoadSnapshot();

        var result = CatalogGraphValidator.Validate(snapshot);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => $"{i.Code}: {i.Message}")));
    }

    [Fact]
    public void CandidateCombination_Validates()
    {
        // Test 35.
        var snapshot = LoadSnapshot();
        var candidate = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 4);

        var structuralResult = TemplateCombinationValidator.Validate(candidate, snapshot);
        var graphResult = CandidatePublishGraphValidator.Validate(snapshot, candidate);

        Assert.True(structuralResult.IsValid, string.Join("; ", structuralResult.Issues.Select(i => $"{i.Code}: {i.Message}")));
        Assert.True(graphResult.IsValid, string.Join("; ", graphResult.Issues.Select(i => $"{i.Code}: {i.Message}")));
    }

    [Fact]
    public void CandidateGraph_UsesExactDependencyReferencesOnly()
    {
        // Test 36.
        var snapshot = LoadSnapshot();

        var progressionV2 = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 2);
        var levelModifierV2 = snapshot.LevelModifiers.Single(l => l.Metadata.Key == "INTERMEDIATE_MODIFIER" && l.Metadata.Version == 2);
        var masterV3 = snapshot.PlanTemplates.Single(t => t.Metadata.Key == "TEN_K_MASTER" && t.Metadata.Version == 3);

        Assert.Equal(2, progressionV2.Metadata.SchemaVersion);
        Assert.All(progressionV2.PhaseProgressions.SelectMany(p => p.Stages), s =>
        {
            Assert.Null(s.WorkoutCandidateKeys);
            Assert.NotNull(s.WorkoutCandidates);
        });

        Assert.Equal(2, levelModifierV2.Metadata.SchemaVersion);
        Assert.Null(levelModifierV2.EligibleWorkoutKeys);
        Assert.NotNull(levelModifierV2.EligibleWorkouts);

        Assert.Equal(2, masterV3.Metadata.SchemaVersion);
        Assert.Null(masterV3.RequiredRules);
        Assert.NotNull(masterV3.RequiredRuleKeys);
        Assert.Equal(2, masterV3.WorkoutProgression.Version);
    }
}
