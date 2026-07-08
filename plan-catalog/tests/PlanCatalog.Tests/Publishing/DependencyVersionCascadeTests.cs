using PlanCatalog.Contracts;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// CASCADE-001: proves the dependency-version cascade triggered by WORKOUT-IMMUT-001 /
/// PEAK-POLICY-IMMUT-001 was handled immutably — no already-published parent artifact was mutated in
/// place, and every changed pinned reference produced a new parent version. See
/// artifacts/audits/dependency-version-cascade-audit.md.
/// </summary>
public sealed class DependencyVersionCascadeTests
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
    public void RulePackV1_IsUnchanged_StillReferencesRestoredPeakPolicyV1()
    {
        var snapshot = LoadSnapshot();
        var v1 = snapshot.RulePacks.Single(r => r.Metadata.Key == "APPSEL_RACE_PLAN_V1" && r.Metadata.Version == 1);

        // Same content hash pinned by every release since 1.0.0 — untouched by this remediation.
        Assert.Equal("020f9aac902b7816d8d4b4f01a82b143df8d5feb23e8cc75f6d9fda36be66a89", ComputeHash(v1));
        Assert.Equal(1, v1.PeakVolumeBandPolicy.Version);
    }

    [Fact]
    public void RulePackV2_IsNew_ReferencesCorrectedPeakPolicyV2()
    {
        var snapshot = LoadSnapshot();
        var v2 = snapshot.RulePacks.Single(r => r.Metadata.Key == "APPSEL_RACE_PLAN_V1" && r.Metadata.Version == 2);

        Assert.Equal(2, v2.PeakVolumeBandPolicy.Version);
        Assert.Equal(1, v2.RuntimeConditionValueRegistry.Version);
    }

    [Fact]
    public void LegacyWorkoutProgressionAndLevelModifierV1_RemainByteUnchanged()
    {
        var snapshot = LoadSnapshot();

        // At the time this test was originally written (WORKOUT-IMMUT-001/PEAK-POLICY-IMMUT-001), both
        // referenced workouts only by unversioned key and had no version bump. The deterministic-graph
        // migration (Part 2) has since created a genuinely new v2 of each (exact versioned references,
        // see exact-workout-reference-migration.md) — but the original, already-published v1 of each
        // must remain byte-for-byte unchanged, which this test now asserts explicitly.
        var progression = snapshot.WorkoutProgressions.Single(p => p.Metadata.Key == "TEN_K_WORKOUT_PROGRESSION_V1" && p.Metadata.Version == 1);
        var levelModifier = snapshot.LevelModifiers.Single(l => l.Metadata.Key == "INTERMEDIATE_MODIFIER" && l.Metadata.Version == 1);

        Assert.Equal("a4856b47bf385ad29c148412480620b2584ddf7b0e0fa177664dc3455baf6281", ComputeHash(progression));
        Assert.Equal("c5e9d601f2756495c921676cb323872539eb65f901135b1240e27034861bff34", ComputeHash(levelModifier));
    }

    [Fact]
    public void CombinationV1AndV2_AreUnchanged_OnlyV3IsNew()
    {
        var snapshot = LoadSnapshot();

        var v1 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 1);
        var v2 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);
        var v3 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 3);

        Assert.Equal("c6324371a352a78d744583ee6bd0d36bd434b9214ff46d5ecf107e2656876c71", ComputeHash(v1));
        Assert.Equal("b3dab01388bfac1de820efa3649007e1bf3cfa1d4980e4070cd9cbacd15e8594", ComputeHash(v2));
        Assert.Equal(1, v1.RulePack.Version);
        Assert.Equal(1, v2.RulePack.Version);
        Assert.Equal(2, v3.RulePack.Version);
        Assert.Equal(v2.MasterTemplate, v3.MasterTemplate);
        Assert.Equal(v2.Layout, v3.Layout);
        Assert.Equal(v2.LevelModifier, v3.LevelModifier);
    }

    [Fact]
    public void ActiveCombinationV3_ResolvesAFullyConsistentVersionedGraph()
    {
        var snapshot = LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var assembler = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher());

        var bundle = assembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 3);

        Assert.Equal("TEN_K_MASTER", bundle.MasterTemplate.Key);
        Assert.Equal(2, bundle.MasterTemplate.Version);
        Assert.Equal("APPSEL_RACE_PLAN_V1", bundle.RulePack.Key);
        Assert.Equal(2, bundle.RulePack.Version);
        Assert.Equal("RUN_LAYOUT_4D", bundle.Layout.Key);
        Assert.Equal(1, bundle.Layout.Version);
        Assert.Equal("INTERMEDIATE_MODIFIER", bundle.LevelModifier.Key);
        Assert.Equal(1, bundle.LevelModifier.Version);

        // Every effective workout must resolve to the corrected v2, not the restored-legacy v1.
        Assert.NotEmpty(bundle.Workouts);
        Assert.All(bundle.Workouts.Where(w => w.Key != "GOAL_PACE_TEN_K"), w => Assert.Equal(2, w.Version));
    }

    [Fact]
    public void HistoricalCombinations_RemainIndependentlyVerifiable()
    {
        var snapshot = LoadSnapshot();
        var result1 = TemplateCombinationValidator.Validate(snapshot.Combinations.Single(c => c.Metadata.Version == 1), snapshot);
        var result2 = TemplateCombinationValidator.Validate(snapshot.Combinations.Single(c => c.Metadata.Version == 2), snapshot);
        var result3 = TemplateCombinationValidator.Validate(snapshot.Combinations.Single(c => c.Metadata.Version == 3), snapshot);

        Assert.True(result1.IsValid);
        Assert.True(result2.IsValid);
        Assert.True(result3.IsValid);
    }

    [Fact]
    public void NoDuplicateKeyVersionAcrossAnyRemediatedIdentity()
    {
        var snapshot = LoadSnapshot();
        var result = CatalogGraphValidator.Validate(snapshot);

        Assert.DoesNotContain(result.Issues, i => i.Code == "GRAPH_DUPLICATE_KEY_VERSION");
    }
}
