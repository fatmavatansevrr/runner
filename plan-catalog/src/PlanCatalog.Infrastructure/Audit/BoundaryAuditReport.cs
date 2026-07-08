using System.Xml.Linq;

namespace PlanCatalog.Infrastructure.Audit;

public sealed record BoundaryAuditReport
{
    public required IReadOnlyList<string> ContractsFiles { get; init; }
    public required IReadOnlyList<string> CoreFiles { get; init; }
    public required string ValidationIssueLocation { get; init; }
    public required string ValidationResultLocation { get; init; }
    public required string CatalogDocumentMetadataLocation { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> ProjectReferenceGraph { get; init; }
    public required IReadOnlyList<string> ViolationsFound { get; init; }
    public required IReadOnlyList<string> CorrectionsMade { get; init; }

    public static BoundaryAuditReport Build(string repoRoot)
    {
        var contractsDir = Path.Combine(repoRoot, "src", "PlanCatalog.Contracts");
        var coreDir = Path.Combine(repoRoot, "src", "PlanCatalog.Core");

        var contractsAsm = typeof(PlanCatalog.Contracts.DocumentTypes).Assembly;
        var coreAsm = typeof(PlanCatalog.Core.Catalog.CatalogSourceSnapshot).Assembly;

        string LocationOf(string typeName)
        {
            var t = coreAsm.GetTypes().FirstOrDefault(x => x.Name == typeName)
                ?? contractsAsm.GetTypes().FirstOrDefault(x => x.Name == typeName);
            return t is null ? "NOT FOUND" : $"{t.Assembly.GetName().Name} :: {t.Namespace}.{t.Name}";
        }

        var projectFiles = new[]
        {
            Path.Combine(repoRoot, "src", "PlanCatalog.Contracts", "PlanCatalog.Contracts.csproj"),
            Path.Combine(repoRoot, "src", "PlanCatalog.Core", "PlanCatalog.Core.csproj"),
            Path.Combine(repoRoot, "src", "PlanCatalog.Infrastructure", "PlanCatalog.Infrastructure.csproj"),
            Path.Combine(repoRoot, "src", "PlanCatalog.Cli", "PlanCatalog.Cli.csproj"),
            Path.Combine(repoRoot, "tests", "PlanCatalog.Tests", "PlanCatalog.Tests.csproj"),
        };

        var graph = projectFiles
            .Where(File.Exists)
            .ToDictionary(
                p => Path.GetFileNameWithoutExtension(p),
                p => (IReadOnlyList<string>)XDocument.Load(p).Descendants("ProjectReference")
                    .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")!.Value))
                    .ToList());

        return new BoundaryAuditReport
        {
            ContractsFiles = SourceFiles(contractsDir),
            CoreFiles = SourceFiles(coreDir),
            ValidationIssueLocation = LocationOf("ValidationIssue"),
            ValidationResultLocation = LocationOf("ValidationResult"),
            CatalogDocumentMetadataLocation = LocationOf("CatalogDocumentMetadata"),
            ProjectReferenceGraph = graph,
            ViolationsFound =
            [
                "ValidationIssue, ValidationResult, and ValidationSeverity were defined in PlanCatalog.Contracts/Validation/ — authoring-only types on the published boundary.",
                "CatalogDocumentMetadata and CatalogStatus (carrying DRAFT/VALIDATED/PUBLISHED/RETIRED lifecycle state) were defined in PlanCatalog.Contracts/Metadata and /Enums — authoring lifecycle state on the published boundary.",
            ],
            CorrectionsMade =
            [
                "Moved ValidationSeverity, ValidationIssue, ValidationResult to PlanCatalog.Core/Validation/ (same namespace already used by all validator classes).",
                "Moved CatalogStatus to PlanCatalog.Core/Enums/ and CatalogDocumentMetadata to PlanCatalog.Core/Metadata/.",
                "Updated every consuming file across Core/Infrastructure/Cli/Tests to reference the new Core-namespaced types.",
                "PublishedTemplateBundle and CatalogReleaseManifest were audited and confirmed already boundary-clean (they only reference CatalogArtifactReference, which never carries lifecycle state) — no change needed there.",
                "Added PublishedBoundaryTests asserting Contracts contains only the documented published-boundary type set, going forward.",
            ]
        };
    }

    private static IReadOnlyList<string> SourceFiles(string projectDir) =>
        Directory.Exists(projectDir)
            ? Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                            !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(f => Path.GetRelativePath(projectDir, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList()
            : [];
}
