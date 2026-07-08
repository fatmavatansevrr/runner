using System.Reflection;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// PlanCatalog.Contracts is the stable published boundary between Process A and future Process B.
/// It must only carry types Process B may legitimately consume: VersionedCatalogReference,
/// CatalogArtifactReference, PublishedTemplateBundle, CatalogReleaseManifest, DocumentTypes, and the
/// stable shared enums that appear in those shapes. Authoring-only concepts (validation results,
/// draft lifecycle metadata) must live in PlanCatalog.Core instead.
/// </summary>
public sealed class PublishedBoundaryTests
{
    private static readonly Assembly ContractsAssembly = typeof(PlanCatalog.Contracts.DocumentTypes).Assembly;
    private static readonly Assembly CoreAssembly = typeof(PlanCatalog.Core.Catalog.CatalogSourceSnapshot).Assembly;

    private static IEnumerable<Type> ContractsPublicTypes() =>
        ContractsAssembly.GetTypes().Where(t => t.IsPublic);

    [Fact]
    public void ValidationIssue_IsNotInContracts()
    {
        Assert.DoesNotContain(ContractsPublicTypes(), t => t.Name == "ValidationIssue");
    }

    [Fact]
    public void ValidationResult_IsNotInContracts()
    {
        Assert.DoesNotContain(ContractsPublicTypes(), t => t.Name == "ValidationResult");
    }

    [Fact]
    public void ValidationSeverity_IsNotInContracts()
    {
        Assert.DoesNotContain(ContractsPublicTypes(), t => t.Name == "ValidationSeverity");
    }

    [Fact]
    public void CatalogDocumentMetadata_IsNotInContracts()
    {
        Assert.DoesNotContain(ContractsPublicTypes(), t => t.Name == "CatalogDocumentMetadata");
    }

    [Fact]
    public void CatalogStatus_IsNotInContracts()
    {
        Assert.DoesNotContain(ContractsPublicTypes(), t => t.Name == "CatalogStatus");
    }

    [Fact]
    public void ValidationIssue_And_ValidationResult_LiveInCore()
    {
        Assert.Contains(CoreAssembly.GetTypes(), t => t.Name == "ValidationIssue" && t.Namespace == "PlanCatalog.Core.Validation");
        Assert.Contains(CoreAssembly.GetTypes(), t => t.Name == "ValidationResult" && t.Namespace == "PlanCatalog.Core.Validation");
    }

    [Fact]
    public void CatalogDocumentMetadata_LivesInCore()
    {
        Assert.Contains(CoreAssembly.GetTypes(), t => t.Name == "CatalogDocumentMetadata" && t.Namespace == "PlanCatalog.Core.Metadata");
    }

    [Fact]
    public void Contracts_HasNoDependencyOnAuthoringValidationOrMetadataNamespaces()
    {
        // Contracts must never need to reference Core — this is a structural corollary of §2.1,
        // but assert it directly against the compiled assembly rather than trusting the .csproj alone.
        var referencedAssemblyNames = ContractsAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain(referencedAssemblyNames, name => name == "PlanCatalog.Core");
    }

    [Fact]
    public void Contracts_PublishedTypes_AreLimitedToKnownBoundaryShapes()
    {
        var allowedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "DocumentTypes",
            "VersionedCatalogReference",
            "CatalogArtifactReference",
            "PublishedTemplateBundle",
            "CatalogReleaseManifest",
            "UnconfirmedContentWarning",
            "ReleaseChannel",
            // Stable shared enums referenced by published/authoring shapes alike.
            "DistanceFamily", "PhaseKey", "PhaseIntent", "WorkoutFamily", "SlotRole",
            "RunningExperience", "RuntimeConditionType", "StageCompressionBehavior",
            "StageExtensionBehavior", "PrescriptionMode", "WorkoutComponentType", "DistanceAccountingMode"
        };

        var unexpected = ContractsPublicTypes()
            .Where(t => !t.IsNested)
            .Select(t => t.Name)
            .Where(name => !allowedNames.Contains(name))
            .ToList();

        Assert.Empty(unexpected);
    }
}
