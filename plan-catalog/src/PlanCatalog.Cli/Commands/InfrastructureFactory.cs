using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Schema;
using PlanCatalog.Infrastructure.Serialization;

namespace PlanCatalog.Cli.Commands;

internal static class InfrastructureFactory
{
    public static SystemTextJsonCanonicalSerializer Serializer { get; } = new();
    public static Sha256ContentHasher Hasher { get; } = new();

    public static FileSystemCatalogSourceRepository CreateSourceRepository() => new(CliPaths.CatalogDirectory);

    public static JsonSchemaNetValidator CreateSchemaValidator() => new(CliPaths.SchemasDirectory);

    public static FileSystemPublishedArtifactRepository CreatePublishedRepository() => new(CliPaths.ArtifactsDirectory);

    public static CatalogBundleAssembler CreateBundleAssembler() => new(Serializer, Hasher);

    public static FileSystemRetirementLedger CreateRetirementLedger() =>
        new(Path.Combine(CliPaths.ArtifactsDirectory, "appsel-plan-catalog", "retirements.json"));

    public static FileSystemReleaseStatusLedger CreateReleaseStatusLedger() =>
        new(Path.Combine(CliPaths.ArtifactsDirectory, "appsel-plan-catalog", "release-status.json"));

    public static FileSystemCrossReleaseHashExceptionRegistry CreateCrossReleaseHashExceptionRegistry() =>
        new(Path.Combine(CliPaths.ArtifactsDirectory, "appsel-plan-catalog", "cross-release-hash-exceptions.json"));

    public static CatalogPublisher CreatePublisher() => new(
        CreateSourceRepository(),
        CreateSchemaValidator(),
        Serializer,
        Hasher,
        CreateBundleAssembler(),
        CreatePublishedRepository(),
        CreateRetirementLedger(),
        CreateCrossReleaseHashExceptionRegistry());
}
