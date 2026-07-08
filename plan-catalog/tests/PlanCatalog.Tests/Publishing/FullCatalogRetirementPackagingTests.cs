using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Ports;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// Proves that a combination which is itself RETIRED (not merely one with a retired dependency) is
/// excluded from full-catalog packaging in any NEW release, while remaining available for historical
/// release verification. Uses a fully isolated, disposable fixture — never the real permanent catalog
/// data or the real retirements.json ledger — per artifacts/audits/full-catalog-retirement-packaging-audit.md.
/// </summary>
public sealed class FullCatalogRetirementPackagingTests : IDisposable
{
    private readonly string _artifactsRoot = Path.Combine(Path.GetTempPath(), "plan-catalog-tests-retirement", Guid.NewGuid().ToString("N"));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private sealed class FakeRetirementLedger(params (string DocumentType, string Key, int Version)[] retired) : IRetirementLedger
    {
        public bool IsRetired(string documentType, string key, int version) =>
            retired.Contains((documentType, key, version));
    }

    private sealed class InMemoryCatalogSourceRepository(CatalogSourceSnapshot snapshot) : ICatalogSourceRepository
    {
        public CatalogSourceSnapshot LoadSnapshot() => snapshot;
    }

    /// <summary>Two independent, isolated combinations sharing the same valid dependency graph — one active, one to be retired.</summary>
    private static (CatalogSourceSnapshot Snapshot, TemplateCombinationDefinition Active, TemplateCombinationDefinition Retired) BuildTwoCombinationSnapshot()
    {
        var fixture = new CombinationFixture();
        var activeCombination = fixture.Combination;
        var retiredCombination = fixture.Combination with
        {
            Metadata = Meta.Of(DocumentTypes.TemplateCombination, "RETIREMENT_TEST_COMBINATION", status: CatalogStatus.Published)
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate)
            .With(fixture.Layout)
            .With(fixture.LevelModifier)
            .With(fixture.WorkoutProgression)
            .With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout)
            .With(fixture.LongRunWorkout)
            .With(fixture.ThresholdWorkout)
            .With(fixture.Registry)
            .With(fixture.PeakVolumeBandPolicy)
            .With(fixture.RulePack)
            .With(activeCombination)
            .With(retiredCombination)
            .Build();

        return (snapshot, activeCombination, retiredCombination);
    }

    private CatalogPublisher CreatePublisher(CatalogSourceSnapshot snapshot, IRetirementLedger retirementLedger, out FileSystemPublishedArtifactRepository publishedRepository)
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var sourceRepository = new InMemoryCatalogSourceRepository(snapshot);
        var schemaValidator = new JsonSchemaNetValidator(Path.Combine(RepoRoot(), "schemas"));
        var bundleAssembler = new CatalogBundleAssembler(serializer, hasher);
        publishedRepository = new FileSystemPublishedArtifactRepository(_artifactsRoot);

        return new CatalogPublisher(sourceRepository, schemaValidator, serializer, hasher, bundleAssembler, publishedRepository, retirementLedger);
    }

    [Fact]
    public void BuildRelease_WithNoRetiredCombinations_PackagesBothAsBundlesAndArtifacts()
    {
        var (snapshot, active, retired) = BuildTwoCombinationSnapshot();
        var publisher = CreatePublisher(snapshot, new FakeRetirementLedger(), out _);

        var manifest = publisher.BuildPreview("preview-1", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        Assert.Contains(manifest.Bundles, b => b.Key == active.Metadata.Key);
        Assert.Contains(manifest.Bundles, b => b.Key == retired.Metadata.Key);
        Assert.Contains(manifest.Artifacts, a => a.DocumentType == DocumentTypes.TemplateCombination && a.Key == retired.Metadata.Key);
    }

    [Fact]
    public void BuildRelease_WithOneCombinationRetired_ExcludesItFromBundlesAndArtifacts_ButKeepsTheOtherActive()
    {
        var (snapshot, active, retired) = BuildTwoCombinationSnapshot();
        var ledger = new FakeRetirementLedger((DocumentTypes.TemplateCombination, retired.Metadata.Key, retired.Metadata.Version));
        var publisher = CreatePublisher(snapshot, ledger, out _);

        var manifest = publisher.BuildPreview("preview-2", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        Assert.Contains(manifest.Bundles, b => b.Key == active.Metadata.Key);
        Assert.DoesNotContain(manifest.Bundles, b => b.Key == retired.Metadata.Key);
        Assert.DoesNotContain(manifest.Artifacts, a => a.DocumentType == DocumentTypes.TemplateCombination && a.Key == retired.Metadata.Key);
        Assert.Contains(manifest.Artifacts, a => a.DocumentType == DocumentTypes.TemplateCombination && a.Key == active.Metadata.Key);
    }

    [Fact]
    public void Publish_WithOneCombinationRetired_WritesReleaseWithOnlyTheActiveCombinationBundle()
    {
        var (snapshot, active, retired) = BuildTwoCombinationSnapshot();
        var ledger = new FakeRetirementLedger((DocumentTypes.TemplateCombination, retired.Metadata.Key, retired.Metadata.Version));
        var publisher = CreatePublisher(snapshot, ledger, out var repository);

        publisher.Publish("release-retirement-1", ReleaseChannel.Pilot, allowUnconfirmedContent: true);

        var manifestOnDisk = repository.ReadManifest("release-retirement-1");
        Assert.Contains(manifestOnDisk.Bundles, b => b.Key == active.Metadata.Key);
        Assert.DoesNotContain(manifestOnDisk.Bundles, b => b.Key == retired.Metadata.Key);
        Assert.False(File.Exists(Path.Combine(_artifactsRoot, "appsel-plan-catalog", "release-retirement-1", "bundles", $"{retired.Metadata.Key}.v{retired.Metadata.Version}.json")));
        Assert.True(File.Exists(Path.Combine(_artifactsRoot, "appsel-plan-catalog", "release-retirement-1", "bundles", $"{active.Metadata.Key}.v{active.Metadata.Version}.json")));
    }

    [Fact]
    public void BuildBundle_ExplicitlyRequestingARetiredCombination_ThrowsWithSuggestedCode()
    {
        var (snapshot, _, retired) = BuildTwoCombinationSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var ledger = new FakeRetirementLedger((DocumentTypes.TemplateCombination, retired.Metadata.Key, retired.Metadata.Version));
        var assembler = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            assembler.Assemble(stamped, retired.Metadata.Key, retired.Metadata.Version, ledger));

        Assert.Contains("RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBundle_ExplicitlyRequestingANonRetiredCombination_StillSucceeds()
    {
        var (snapshot, active, retired) = BuildTwoCombinationSnapshot();
        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var ledger = new FakeRetirementLedger((DocumentTypes.TemplateCombination, retired.Metadata.Key, retired.Metadata.Version));
        var assembler = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher());

        var bundle = assembler.Assemble(stamped, active.Metadata.Key, active.Metadata.Version, ledger);

        Assert.Equal(active.Metadata.Key, bundle.BundleKey);
    }

    [Fact]
    public void RetiredCombinationSourceFile_IsNotDeleted_AndRemainsInSnapshot()
    {
        var (snapshot, _, retired) = BuildTwoCombinationSnapshot();

        Assert.Contains(snapshot.Combinations, c => c.Metadata.Key == retired.Metadata.Key);
    }

    [Fact]
    public void HistoricalRelease_BuiltBeforeRetirement_StillVerifiesAfterCombinationIsLaterRetired()
    {
        var (snapshot, active, retired) = BuildTwoCombinationSnapshot();

        // Build and publish the historical release BEFORE the combination is retired.
        var publisherBeforeRetirement = CreatePublisher(snapshot, new FakeRetirementLedger(), out var repository);
        publisherBeforeRetirement.Publish("release-retirement-historical", ReleaseChannel.Pilot, allowUnconfirmedContent: true);
        var historicalManifest = repository.ReadManifest("release-retirement-historical");
        Assert.Contains(historicalManifest.Bundles, b => b.Key == retired.Metadata.Key);

        // Now the combination becomes retired. A NEW build must exclude it, but the historical release's
        // own files are untouched (retirement never mutates history) and remain independently readable/verifiable.
        var ledgerAfterRetirement = new FakeRetirementLedger((DocumentTypes.TemplateCombination, retired.Metadata.Key, retired.Metadata.Version));
        var publisherAfterRetirement = CreatePublisher(snapshot, ledgerAfterRetirement, out var repositoryAfterRetirement);
        publisherAfterRetirement.Publish("release-retirement-new", ReleaseChannel.Pilot, allowUnconfirmedContent: true);
        var newManifest = repositoryAfterRetirement.ReadManifest("release-retirement-new");

        Assert.DoesNotContain(newManifest.Bundles, b => b.Key == retired.Metadata.Key);
        Assert.Contains(newManifest.Bundles, b => b.Key == active.Metadata.Key);

        var reReadHistoricalManifest = repository.ReadManifest("release-retirement-historical");
        Assert.Contains(reReadHistoricalManifest.Bundles, b => b.Key == retired.Metadata.Key);
        Assert.Equal(
            historicalManifest.Bundles.First(b => b.Key == retired.Metadata.Key).ContentHash,
            reReadHistoricalManifest.Bundles.First(b => b.Key == retired.Metadata.Key).ContentHash);
    }

    public void Dispose()
    {
        try { Directory.Delete(_artifactsRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }
}
