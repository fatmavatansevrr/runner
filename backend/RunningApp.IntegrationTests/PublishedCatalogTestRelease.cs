using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Manifests;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Ports;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Test-only immutable release produced by the real Process A publisher. The
/// source repository wrapper promotes draft metadata in memory to VALIDATED;
/// real authoring files are never copied, rewritten, or published in place.
/// </summary>
public sealed class PublishedCatalogTestRelease : IDisposable
{
    private const string ReleaseVersion = "phase4g5o-v10-test";
    private readonly string _artifactsRoot = Path.Combine(Path.GetTempPath(), "runningapp-phase4g5o", Guid.NewGuid().ToString("N"));

    public PublishedCatalogTestRelease()
        : this(retireV10: false)
    {
    }

    internal PublishedCatalogTestRelease(bool retireV10)
    {
        var repoRoot = RepoRoot();
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        var source = new PublishableTestCatalogSourceRepository(
            new FileSystemCatalogSourceRepository(Path.Combine(repoRoot, "plan-catalog", "catalog")));
        var published = new FileSystemPublishedArtifactRepository(_artifactsRoot);
        var publisher = new CatalogPublisher(
            source,
            new JsonSchemaNetValidator(Path.Combine(repoRoot, "plan-catalog", "schemas")),
            serializer,
            hasher,
            new CatalogBundleAssembler(serializer, hasher),
            published,
            retireV10 ? new V10RetirementLedger() : NullRetirementLedger.Instance);

        Manifest = publisher.Publish(ReleaseVersion, ReleaseChannel.Pilot, allowUnconfirmedContent: true);
        ReleaseRoot = Path.Combine(_artifactsRoot, "appsel-plan-catalog", ReleaseVersion);

        // Process A predates the two Long-Horizon catalog document families,
        // while the deployable API package requires the complete canonical
        // runtime directory inventory. Keep this test release representative
        // of the API publish package without changing publisher semantics.
        CopyRuntimeDirectory(repoRoot, "long-horizon-progressions");
        CopyRuntimeDirectory(repoRoot, "preparation-runway-progressions");
    }

    public string ReleaseRoot { get; }
    public CatalogReleaseManifest Manifest { get; }

    public void Dispose()
    {
        if (Directory.Exists(_artifactsRoot))
        {
            try { Directory.Delete(_artifactsRoot, recursive: true); } catch { /* best-effort test cleanup */ }
        }
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !(Directory.Exists(Path.Combine(directory.FullName, "backend")) &&
                 Directory.Exists(Path.Combine(directory.FullName, "plan-catalog"))))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private void CopyRuntimeDirectory(string repoRoot, string directoryName)
    {
        var sourceRoot = Path.Combine(repoRoot, "plan-catalog", "catalog", directoryName);
        var targetRoot = Path.Combine(ReleaseRoot, directoryName);
        Directory.CreateDirectory(targetRoot);

        foreach (var sourcePath in Directory.EnumerateFiles(sourceRoot, "*.json", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, sourcePath);
            var targetPath = Path.Combine(targetRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(sourcePath, targetPath, overwrite: false);
        }
    }

    private sealed class V10RetirementLedger : IRetirementLedger
    {
        public bool IsRetired(string documentType, string key, int version) =>
            documentType == "TEMPLATE_COMBINATION" &&
            key == "TEN_K__4D__INTERMEDIATE" &&
            version == 10;
    }

    private sealed class PublishableTestCatalogSourceRepository(ICatalogSourceRepository inner) : ICatalogSourceRepository
    {
        public CatalogSourceSnapshot LoadSnapshot()
        {
            var source = inner.LoadSnapshot();
            return source with
            {
                PlanTemplates = source.PlanTemplates.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                RunLayouts = source.RunLayouts.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                LevelModifiers = source.LevelModifiers.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                WorkoutProgressions = source.WorkoutProgressions.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                ProgressionModifiers = source.ProgressionModifiers.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                Workouts = source.Workouts.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                RuntimeConditionValueRegistries = source.RuntimeConditionValueRegistries.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                PeakVolumeBandPolicies = source.PeakVolumeBandPolicies.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                RulePacks = source.RulePacks.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
                Combinations = source.Combinations.Select(x => x with { Metadata = Promote(x.Metadata) }).ToList(),
            };
        }

        private static PlanCatalog.Core.Metadata.CatalogDocumentMetadata Promote(
            PlanCatalog.Core.Metadata.CatalogDocumentMetadata metadata) =>
            metadata.Status == CatalogStatus.Draft
                ? metadata with { Status = CatalogStatus.Validated, ContentHash = null }
                : metadata;
    }
}
