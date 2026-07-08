using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Ports;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

public sealed class CatalogPublisherTests : IDisposable
{
    private readonly string _artifactsRoot = Path.Combine(Path.GetTempPath(), "plan-catalog-tests", Guid.NewGuid().ToString("N"));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private CatalogPublisher CreatePublisher(out FileSystemPublishedArtifactRepository publishedRepository)
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var sourceRepository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var schemaValidator = new JsonSchemaNetValidator(Path.Combine(RepoRoot(), "schemas"));
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        publishedRepository = new FileSystemPublishedArtifactRepository(_artifactsRoot);

        return new CatalogPublisher(sourceRepository, schemaValidator, serializer, hasher, bundleAssembler, publishedRepository, NullRetirementLedger.Instance);
    }

    [Fact]
    public void Publish_PilotChannelWithAllowUnconfirmed_ProducesManifestWithBundleAndArtifacts()
    {
        var publisher = CreatePublisher(out _);

        var manifest = publisher.Publish("test-1", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        Assert.NotEmpty(manifest.Artifacts);
        // TEN_K__4D__INTERMEDIATE exists as four immutable, independently-versioned combinations
        // (v1 pre-dates the TAPER fix; v2 is the TAPER-corrected version; v3 is the corrected version
        // referencing the peak-volume-policy-remediated RulePack v2; v4 is the deterministic-graph
        // candidate referencing exact-versioned workout progression/level-modifier — see
        // deterministic-graph-part2-migration.md) — each gets its own bundle.
        Assert.Equal(4, manifest.Bundles.Count(b => b.Key == "TEN_K__4D__INTERMEDIATE"));
        Assert.False(string.IsNullOrEmpty(manifest.ManifestContentHash));
        Assert.Equal(ReleaseChannel.Pilot, manifest.Channel);
        Assert.NotEmpty(manifest.UnconfirmedContentWarnings);
    }

    [Fact]
    public void Publish_PilotChannelWithoutExplicitFlag_ThrowsBecauseContentIsUnconfirmed()
    {
        var publisher = CreatePublisher(out _);

        var ex = Assert.Throws<CatalogValidationException>(() => publisher.Publish("test-1b", ReleaseChannel.Pilot, allowUnconfirmedContent: false));
        Assert.Contains(ex.Result.Issues, i => i.Code == "PUBLISH_UNCONFIRMED_CONTENT_REQUIRES_EXPLICIT_FLAG");
    }

    [Fact]
    public void Publish_ProductionChannel_NeverAcceptsUnconfirmedContentEvenWithFlag()
    {
        var publisher = CreatePublisher(out _);

        var ex = Assert.Throws<CatalogValidationException>(() => publisher.Publish("test-1c", ReleaseChannel.Production, allowUnconfirmedContent: true));
        Assert.Contains(ex.Result.Issues, i => i.Code == "PUBLISH_PRODUCTION_CONTAINS_UNCONFIRMED_CONTENT");
    }

    [Fact]
    public void Publish_WritesFilesToImmutableReleaseDirectory()
    {
        var publisher = CreatePublisher(out var repository);

        publisher.Publish("test-2", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        Assert.True(repository.ReleaseExists("test-2"));
        var manifestOnDisk = repository.ReadManifest("test-2");
        Assert.NotEmpty(manifestOnDisk.Artifacts);
    }

    [Fact]
    public void Publish_SameSourceTwice_ProducesSameManifestHash()
    {
        var publisherA = CreatePublisher(out _);
        var manifestA = publisherA.Publish("test-3a", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        var artifactsRootB = Path.Combine(Path.GetTempPath(), "plan-catalog-tests", Guid.NewGuid().ToString("N"));
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var sourceRepository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var schemaValidator = new JsonSchemaNetValidator(Path.Combine(RepoRoot(), "schemas"));
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        var repositoryB = new FileSystemPublishedArtifactRepository(artifactsRootB);
        var publisherB = new CatalogPublisher(sourceRepository, schemaValidator, serializer, hasher, bundleAssembler, repositoryB, NullRetirementLedger.Instance);

        var manifestB = publisherB.Publish("test-3a", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        var bundleHashA = manifestA.Bundles.OrderBy(b => b.Version).Select(b => b.ContentHash).ToList();
        var bundleHashB = manifestB.Bundles.OrderBy(b => b.Version).Select(b => b.ContentHash).ToList();

        Assert.Equal(bundleHashA, bundleHashB);

        try { Directory.Delete(artifactsRootB, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public void Publish_ExistingReleaseVersion_Throws()
    {
        var publisher = CreatePublisher(out _);
        publisher.Publish("test-4", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        Assert.Throws<InvalidOperationException>(() => publisher.Publish("test-4", ReleaseChannel.Pilot, allowUnconfirmedContent: true));
    }

    [Fact]
    public void Publish_InvalidCatalog_ThrowsCatalogValidationExceptionAndLeavesNoPartialRelease()
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var brokenCatalogDir = Path.Combine(Path.GetTempPath(), "plan-catalog-tests-broken", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(brokenCatalogDir, "layouts"));
        File.WriteAllText(Path.Combine(brokenCatalogDir, "layouts", "bad.json"), """
        {
          "metadata": { "documentType": "RUN_LAYOUT", "schemaVersion": 1, "key": "BAD_LAYOUT", "version": 1, "status": "VALIDATED" },
          "runsPerWeek": 4,
          "slots": [
            { "sequenceOrder": 1, "role": "EASY_SUPPORT" },
            { "sequenceOrder": 2, "role": "EASY_SUPPORT" },
            { "sequenceOrder": 3, "role": "EASY_SUPPORT" },
            { "sequenceOrder": 4, "role": "EASY_SUPPORT" }
          ]
        }
        """);

        var sourceRepository = new FileSystemCatalogSourceRepository(brokenCatalogDir);
        var schemaValidator = new JsonSchemaNetValidator(Path.Combine(RepoRoot(), "schemas"));
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        var publishedRepository = new FileSystemPublishedArtifactRepository(_artifactsRoot);
        var publisher = new CatalogPublisher(sourceRepository, schemaValidator, serializer, hasher, bundleAssembler, publishedRepository, NullRetirementLedger.Instance);

        Assert.Throws<CatalogValidationException>(() => publisher.Publish("test-5", ReleaseChannel.Pilot, allowUnconfirmedContent: true));
        Assert.False(publishedRepository.ReleaseExists("test-5"));
        Assert.False(Directory.Exists(Path.Combine(_artifactsRoot, "appsel-plan-catalog", "test-5")));

        try { Directory.Delete(brokenCatalogDir, recursive: true); } catch { /* best-effort cleanup */ }
    }

    public void Dispose()
    {
        if (Directory.Exists(_artifactsRoot))
        {
            try { Directory.Delete(_artifactsRoot, recursive: true); } catch { /* best-effort cleanup */ }
        }

        GC.SuppressFinalize(this);
    }
}
