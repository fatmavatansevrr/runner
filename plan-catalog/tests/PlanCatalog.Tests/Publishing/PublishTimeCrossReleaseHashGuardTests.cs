using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Ports;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// Proves the publish-time cross-release hash-consistency guard described in
/// artifacts/audits/cross-release-hash-consistency-audit.md: a NEW publish must be rejected if it would
/// re-publish an already-published (documentType, key, version) identity under a different content hash
/// — the exact class of defect that produced the 0.3.0-pilot combination mutation
/// (combination-immutability-investigation.md). Uses a fully isolated in-memory
/// <see cref="ICatalogSourceRepository"/> fixture and a disposable temp artifacts directory — never the
/// real catalog/ or real artifacts/appsel-plan-catalog/ trees.
/// </summary>
public sealed class PublishTimeCrossReleaseHashGuardTests : IDisposable
{
    private readonly string _artifactsRoot = Path.Combine(Path.GetTempPath(), "plan-catalog-tests-crossrelease-guard", Guid.NewGuid().ToString("N"));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private sealed class InMemoryCatalogSourceRepository(CatalogSourceSnapshot snapshot) : ICatalogSourceRepository
    {
        public CatalogSourceSnapshot LoadSnapshot() => snapshot;
    }

    private sealed class FakeCrossReleaseHashExceptionRegistry(
        params (string DocumentType, string Key, int Version, string ReleaseVersion, string ObservedContentHash)[] known)
        : ICrossReleaseHashExceptionRegistry
    {
        public bool IsKnownException(string documentType, string key, int version, string releaseVersion, string observedContentHash) =>
            known.Contains((documentType, key, version, releaseVersion, observedContentHash));
    }

    private CatalogPublisher CreatePublisher(CatalogSourceSnapshot snapshot, ICrossReleaseHashExceptionRegistry? exceptionRegistry, out FileSystemPublishedArtifactRepository publishedRepository)
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var sourceRepository = new InMemoryCatalogSourceRepository(snapshot);
        var schemaValidator = new JsonSchemaNetValidator(Path.Combine(RepoRoot(), "schemas"));
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        publishedRepository = new FileSystemPublishedArtifactRepository(_artifactsRoot);

        return new CatalogPublisher(sourceRepository, schemaValidator, serializer, hasher, bundleAssembler, publishedRepository, NullRetirementLedger.Instance, exceptionRegistry);
    }

    [Fact]
    public void Publish_SameContentTwiceUnderDifferentReleaseVersions_NeverConflicts()
    {
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();

        var publisher = CreatePublisher(snapshot, exceptionRegistry: null, out _);
        publisher.Publish("guard-1a", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        // A second release built from the exact same, unchanged source must never conflict with itself.
        publisher.Publish("guard-1b", ReleaseChannel.Pilot, allowUnconfirmedContent: true);
    }

    [Fact]
    public void Publish_WithMutatedDependencyUnderSameVersion_IsRejected_AndWritesNoPartialRelease()
    {
        var fixture = new CombinationFixture();
        var originalSnapshot = fixture.BuildSnapshot();

        var publisher = CreatePublisher(originalSnapshot, exceptionRegistry: null, out var repository);
        publisher.Publish("guard-2a", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        // Mutate ProgressionModifier's content in place while keeping the same declared version — exactly
        // the class of defect this guard exists to catch.
        var mutatedProgressionModifier = fixture.ProgressionModifier with { MainSetDoseMultiplier = 1.5m };
        var mutatedSnapshot = originalSnapshot with
        {
            ProgressionModifiers = [mutatedProgressionModifier]
        };

        var publisherForMutatedBuild = CreatePublisher(mutatedSnapshot, exceptionRegistry: null, out var repositoryForMutatedBuild);

        var ex = Assert.Throws<CatalogValidationException>(() =>
            publisherForMutatedBuild.Publish("guard-2b", ReleaseChannel.Pilot, allowUnconfirmedContent: true));

        Assert.Contains(ex.Result.Issues, i => i.Code == "PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION");
        Assert.False(repositoryForMutatedBuild.ReleaseExists("guard-2b"));
        Assert.False(Directory.Exists(Path.Combine(_artifactsRoot, "appsel-plan-catalog", "guard-2b")));

        // The first (correct) release must remain completely unaffected.
        Assert.True(repository.ReleaseExists("guard-2a"));
    }

    [Fact]
    public void Publish_WithMutatedDependency_ButRegisteredAsAnException_Succeeds()
    {
        var fixture = new CombinationFixture();
        var originalSnapshot = fixture.BuildSnapshot();

        var publisher = CreatePublisher(originalSnapshot, exceptionRegistry: null, out var repositoryA);
        var originalManifest = publisher.Publish("guard-3a", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        var mutatedProgressionModifier = fixture.ProgressionModifier with { MainSetDoseMultiplier = 1.5m };
        var mutatedSnapshot = originalSnapshot with
        {
            ProgressionModifiers = [mutatedProgressionModifier]
        };

        var originalProgressionModifierHash = originalManifest.Artifacts
            .Single(a => a.DocumentType == PlanCatalog.Contracts.DocumentTypes.ProgressionModifier).ContentHash;
        var originalBundleHash = originalManifest.Bundles.Single().ContentHash;

        // Mutating the ProgressionModifier also changes the derived bundle hash (the bundle is computed
        // over the full resolved dependency closure) — both identities' guard-3a hash must be registered.
        var exceptionRegistry = new FakeCrossReleaseHashExceptionRegistry(
            (PlanCatalog.Contracts.DocumentTypes.ProgressionModifier, fixture.ProgressionModifier.Metadata.Key, fixture.ProgressionModifier.Metadata.Version, "guard-3a", originalProgressionModifierHash),
            (PlanCatalog.Contracts.DocumentTypes.PublishedTemplateBundle, fixture.Combination.Metadata.Key, fixture.Combination.Metadata.Version, "guard-3a", originalBundleHash));

        var publisherForMutatedBuild = CreatePublisher(mutatedSnapshot, exceptionRegistry, out var repositoryForMutatedBuild);

        // Should NOT throw: the only divergence is explicitly registered as a known exception.
        publisherForMutatedBuild.Publish("guard-3b", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        Assert.True(repositoryForMutatedBuild.ReleaseExists("guard-3b"));
    }

    [Fact]
    public void RealCatalog_BuildReleasePreview_UsingRealExceptionRegistry_HasNoUnexpectedCrossReleaseMismatches()
    {
        // Proves that today's real catalog/ source, checked against the real, already-published
        // artifacts/appsel-plan-catalog/ releases and the real cross-release-hash-exceptions.json,
        // still builds cleanly — the known historical defects are fully accounted for and do not block
        // a legitimate future publish.
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var sourceRepository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var schemaValidator = new JsonSchemaNetValidator(Path.Combine(RepoRoot(), "schemas"));
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        var realPublishedRepository = new FileSystemPublishedArtifactRepository(Path.Combine(RepoRoot(), "artifacts"));
        var realExceptionRegistry = new FileSystemCrossReleaseHashExceptionRegistry(
            Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog", "cross-release-hash-exceptions.json"));

        var publisher = new CatalogPublisher(sourceRepository, schemaValidator, serializer, hasher, bundleAssembler, realPublishedRepository, NullRetirementLedger.Instance, realExceptionRegistry);

        // Must not throw CatalogValidationException with PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION.
        var manifest = publisher.BuildPreview("cross-release-guard-real-catalog-preview", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        Assert.NotEmpty(manifest.Artifacts);
    }

    [Fact]
    public void RepublishingChangedContentUnderARestoredV1Identity_FailsAgainstRealHistory_AndWritesNoPartialRelease()
    {
        // Uses the REAL catalog/ source (loaded once, then a single field swapped back to the mutated
        // content EASY_STANDARD v1 used to carry) and the REAL, already-published
        // artifacts/appsel-plan-catalog/ history + real exceptions file — but writes only to a disposable
        // temp release directory, never touching any real release. Proves that a hand-edited v1 that
        // reintroduces the historical mutation (or any other unregistered content) is rejected outright.
        var realSourceRepository = new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog"));
        var realSnapshot = realSourceRepository.LoadSnapshot();

        var easyStandardV1 = realSnapshot.Workouts.Single(w => w.Metadata.Key == "EASY_STANDARD" && w.Metadata.Version == 1);
        var reMutatedEasyStandardV1 = easyStandardV1 with
        {
            AllowedPrescriptionModes = [PlanCatalog.Contracts.Enums.PrescriptionMode.Distance],
            AllowedDistanceAccountingModes = [PlanCatalog.Contracts.Enums.DistanceAccountingMode.ExactSessionTotal]
        };

        var tamperedSnapshot = realSnapshot with
        {
            Workouts = realSnapshot.Workouts.Select(w => w.Metadata.Key == "EASY_STANDARD" && w.Metadata.Version == 1 ? reMutatedEasyStandardV1 : w).ToList()
        };

        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var schemaValidator = new JsonSchemaNetValidator(Path.Combine(RepoRoot(), "schemas"));
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        var realExceptionRegistry = new FileSystemCrossReleaseHashExceptionRegistry(
            Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog", "cross-release-hash-exceptions.json"));
        var tamperedSourceRepository = new InMemoryCatalogSourceRepository(tamperedSnapshot);
        var publishedRepositoryPointingAtRealHistory = new FileSystemPublishedArtifactRepository(Path.Combine(RepoRoot(), "artifacts"));

        var publisher = new CatalogPublisher(tamperedSourceRepository, schemaValidator, serializer, hasher, bundleAssembler, publishedRepositoryPointingAtRealHistory, NullRetirementLedger.Instance, realExceptionRegistry);

        var ex = Assert.Throws<CatalogValidationException>(() =>
            publisher.Publish("tampered-preview-should-never-write", ReleaseChannel.Pilot, allowUnconfirmedContent: true));

        Assert.Contains(ex.Result.Issues, i => i.Code == "PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION" && i.Message.Contains("EASY_STANDARD", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog", "tampered-preview-should-never-write")));
    }

    public void Dispose()
    {
        try { Directory.Delete(_artifactsRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }
}
