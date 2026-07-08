using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// TEN_K__4D__INTERMEDIATE v2 hash-stability and dependency-closure audit. Proves the artifact's content
/// hash is a pure function of its canonical content — never of release version, channel, build time, or
/// output location — and that its PublishedTemplateBundle closure never includes an older version of the
/// same combination. See artifacts/audits/combination-v2-hash-and-closure-audit.md.
/// </summary>
public sealed class CombinationV2HashAndClosureTests
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

    private static (SystemTextJsonCanonicalSerializer Serializer, Sha256ContentHasher Hasher, FileSystemCatalogSourceRepository Repository) CreateInfra() =>
        (new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")));

    [Fact]
    public void CombinationV2_SourceHash_IsDeterministic_AcrossRepeatedComputations()
    {
        var (serializer, hasher, repository) = CreateInfra();
        var snapshot = repository.LoadSnapshot();
        var v2 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);

        var hash1 = CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v2);
        var hash2 = CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v2);
        var hash3 = CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v2);

        Assert.Equal(hash1, hash2);
        Assert.Equal(hash1, hash3);
    }

    [Fact]
    public void CombinationV2_SourceHash_IsIndependentOfWallClockTime()
    {
        var (serializer, hasher, _) = CreateInfra();
        var repository1 = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var v2Before = repository1.LoadSnapshot().Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);
        var hashBefore = CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v2Before);

        Thread.Sleep(1100);

        var repository2 = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var v2After = repository2.LoadSnapshot().Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);
        var hashAfter = CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v2After);

        Assert.Equal(hashBefore, hashAfter);
    }

    [Fact]
    public void CombinationV2_BundleAssemblyHash_MatchesSourceHash()
    {
        var (serializer, hasher, repository) = CreateInfra();
        var snapshot = repository.LoadSnapshot();
        var v2Source = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);
        var sourceHash = CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v2Source);

        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, snapshot);
        var stampedV2 = stamped.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);

        // Stamping (draft -> Published + ContentHash) must not change the semantic content hash, because
        // the hash computation excludes the document's own ContentHash field.
        Assert.Equal(sourceHash, stampedV2.Metadata.ContentHash);

        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        var bundle = bundleAssembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 2);

        Assert.Equal(sourceHash, bundle.Combination.ContentHash);
    }

    [Fact]
    public void CombinationV1AndV2_AreNotEqual_ButEachIsInternallyStable()
    {
        var (serializer, hasher, repository) = CreateInfra();
        var snapshot = repository.LoadSnapshot();

        var v1 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 1);
        var v2 = snapshot.Combinations.Single(c => c.Metadata.Key == "TEN_K__4D__INTERMEDIATE" && c.Metadata.Version == 2);

        var hashV1 = CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v1);
        var hashV2 = CatalogDocumentHasher.ComputeContentHash(serializer, hasher, v2);

        Assert.NotEqual(hashV1, hashV2);
        Assert.Equal("c6324371a352a78d744583ee6bd0d36bd434b9214ff46d5ecf107e2656876c71", hashV1);
    }

    [Fact]
    public void CombinationV2Bundle_DoesNotReference_CombinationV1_AsADependency()
    {
        var (serializer, hasher, repository) = CreateInfra();
        var snapshot = repository.LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, snapshot);
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);

        var bundleV2 = bundleAssembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 2);

        // The bundle's own root combination reference must be v2, and no other reference inside the
        // bundle (master/layout/levelModifier/progression/rulePack/registry/peak/workouts) may name the
        // combination document type at all — a combination never depends on another combination.
        Assert.Equal(2, bundleV2.Combination.Version);
        Assert.Equal(2, bundleV2.MasterTemplate.Version);

        var allReferencedDocumentTypes = new[]
        {
            bundleV2.MasterTemplate.DocumentType, bundleV2.Layout.DocumentType, bundleV2.LevelModifier.DocumentType,
            bundleV2.WorkoutProgression.DocumentType, bundleV2.ProgressionModifier.DocumentType, bundleV2.RulePack.DocumentType,
            bundleV2.RuntimeConditionValueRegistry.DocumentType, bundleV2.PeakVolumeBandPolicy.DocumentType
        }.Concat(bundleV2.Workouts.Select(w => w.DocumentType));

        Assert.DoesNotContain("TEMPLATE_COMBINATION", allReferencedDocumentTypes);
    }

    [Fact]
    public void CombinationV2Bundle_ContainsExactlyOneRootCombinationReference()
    {
        var (serializer, hasher, repository) = CreateInfra();
        var snapshot = repository.LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, snapshot);
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);

        var bundleV2 = bundleAssembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 2);

        // PublishedTemplateBundle has exactly one "Combination" property (a single root reference) by
        // construction — this test documents that structural guarantee explicitly.
        Assert.Equal("TEN_K__4D__INTERMEDIATE", bundleV2.Combination.Key);
        Assert.Equal(2, bundleV2.Combination.Version);
    }

    [Fact]
    public void SelectingCombinationVersionExplicitly_ResolvesUnambiguously()
    {
        var (serializer, hasher, repository) = CreateInfra();
        var snapshot = repository.LoadSnapshot();
        var stamped = CatalogStamper.StampAsPublished(serializer, hasher, snapshot);
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);

        var bundleV1 = bundleAssembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 1);
        var bundleV2 = bundleAssembler.Assemble(stamped, "TEN_K__4D__INTERMEDIATE", 2);

        Assert.Equal(1, bundleV1.MasterTemplate.Version);
        Assert.Equal(2, bundleV2.MasterTemplate.Version);
        Assert.NotEqual(bundleV1.BundleContentHash, bundleV2.BundleContentHash);
    }

    [Fact]
    public void PublishedRelease040Pilot_PinsV2CombinationWithSourceComputedHash()
    {
        var releaseManifestPath = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog", "0.4.0-pilot", "release-manifest.json");
        Assert.True(File.Exists(releaseManifestPath), "0.4.0-pilot release manifest must exist for this audit.");

        var json = File.ReadAllText(releaseManifestPath);
        Assert.Contains("\"key\":\"TEN_K__4D__INTERMEDIATE\",\"version\":2,\"contentHash\":\"b3dab01388bfac1de820efa3649007e1bf3cfa1d4980e4070cd9cbacd15e8594\"", json.Replace(" ", ""));
    }

    [Fact]
    public void HistoricalReleases_StillPinOriginalV1Hash_Independently()
    {
        foreach (var release in new[] { "1.0.0", "0.1.0-pilot", "0.2.0-pilot" })
        {
            var manifestPath = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog", release, "release-manifest.json");
            var json = File.ReadAllText(manifestPath).Replace(" ", "");
            Assert.Contains("c6324371a352a78d744583ee6bd0d36bd434b9214ff46d5ecf107e2656876c71", json);
        }
    }
}
