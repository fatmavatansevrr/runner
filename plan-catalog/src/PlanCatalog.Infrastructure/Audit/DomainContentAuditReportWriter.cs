using System.Text;
using System.Text.Json;
using PlanCatalog.Core.Audit;
using PlanCatalog.Infrastructure.Serialization;

namespace PlanCatalog.Infrastructure.Audit;

public static class DomainContentAuditReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string[] GroupOrder =
    [
        "vocabulary", "workout-progression", "workout-definitions", "progression-modifier",
        "runtime-condition-registry", "peak-volume-policy", "phase-metadata", "layout-metadata", "technical-metadata"
    ];

    private static string Classify(ContentDecisionStatus status) => UpperSnakeCaseNamingPolicy.Instance.ConvertName(status.ToString());

    public static (string JsonPath, string MarkdownPath) Write(string repoRoot)
    {
        var auditsDir = Path.Combine(repoRoot, "artifacts", "audits");
        Directory.CreateDirectory(auditsDir);

        var boundary = BoundaryAuditReport.Build(repoRoot);
        var entries = PilotDomainContentAudit.Entries;

        var jsonPath = Path.Combine(auditsDir, "ten-k-pilot-domain-decision-audit.json");
        var mdPath = Path.Combine(auditsDir, "ten-k-pilot-domain-decision-audit.md");

        File.WriteAllText(jsonPath, BuildJson(boundary, entries));
        File.WriteAllText(mdPath, BuildMarkdown(boundary, entries));

        return (jsonPath, mdPath);
    }

    private static string BuildJson(BoundaryAuditReport boundary, IReadOnlyList<DomainContentDecision> entries)
    {
        var document = new
        {
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            boundaryAudit = new
            {
                contractsFiles = boundary.ContractsFiles,
                coreFiles = boundary.CoreFiles,
                validationIssueLocation = boundary.ValidationIssueLocation,
                validationResultLocation = boundary.ValidationResultLocation,
                catalogDocumentMetadataLocation = boundary.CatalogDocumentMetadataLocation,
                projectReferenceGraph = boundary.ProjectReferenceGraph,
                violationsFound = boundary.ViolationsFound,
                correctionsMade = boundary.CorrectionsMade
            },
            domainContentDecisions = entries.Select(e => new
            {
                entryId = e.EntryId,
                group = e.Group,
                documentType = e.DocumentType,
                key = e.Key,
                version = e.Version,
                jsonPath = e.JsonPath,
                currentValue = e.CurrentValue,
                classification = Classify(e.Classification),
                sourceFile = e.SourceFile,
                sourceSectionOrReason = e.SourceSectionOrReason,
                isBlocking = e.IsBlocking,
                requiredDecision = e.RequiredDecision,
                affectedValidators = e.AffectedValidators,
                affectedBundlesOrReleases = e.AffectedBundlesOrReleases,
                productionPublishAllowed = e.ProductionPublishAllowed
            }),
            summary = new
            {
                totalEntries = entries.Count,
                canonicalConfirmed = entries.Count(e => e.Classification == ContentDecisionStatus.CanonicalConfirmed),
                explicitProductDefault = entries.Count(e => e.Classification == ContentDecisionStatus.ExplicitProductDefault),
                placeholderUnconfirmed = entries.Count(e => e.Classification == ContentDecisionStatus.PlaceholderUnconfirmed),
                technicalOnly = entries.Count(e => e.Classification == ContentDecisionStatus.TechnicalOnly),
                blockingEntries = entries.Count(e => e.IsBlocking)
            }
        };

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    private static string BuildMarkdown(BoundaryAuditReport boundary, IReadOnlyList<DomainContentDecision> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# TEN_K / 4D / INTERMEDIATE Pilot — Domain Content Decision Audit");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:O}");
        sb.AppendLine();
        sb.AppendLine("Reconciled against Golden Fixture v3 per the source-governance hierarchy in `plan-catalog/docs/README.md`. " +
                       "See `canonical-source-preflight.md` for pre-flight results and `ten-k-pilot-vocabulary-decisions.md` for " +
                       "the dedicated vocabulary-ownership review.");
        sb.AppendLine();

        sb.AppendLine("## Boundary audit");
        sb.AppendLine();
        sb.AppendLine($"- `ValidationIssue` location: **{boundary.ValidationIssueLocation}**");
        sb.AppendLine($"- `ValidationResult` location: **{boundary.ValidationResultLocation}**");
        sb.AppendLine($"- `CatalogDocumentMetadata` location: **{boundary.CatalogDocumentMetadataLocation}**");
        sb.AppendLine();
        sb.AppendLine("### Project reference graph");
        foreach (var (project, refs) in boundary.ProjectReferenceGraph)
        {
            sb.AppendLine($"- `{project}` → {(refs.Count == 0 ? "(none)" : string.Join(", ", refs))}");
        }
        sb.AppendLine();
        sb.AppendLine("### Violations found");
        foreach (var v in boundary.ViolationsFound)
        {
            sb.AppendLine($"- {v}");
        }
        sb.AppendLine();
        sb.AppendLine("### Corrections made");
        foreach (var c in boundary.CorrectionsMade)
        {
            sb.AppendLine($"- {c}");
        }
        sb.AppendLine();

        sb.AppendLine("## Domain content decisions (reconciliation table)");
        sb.AppendLine();
        sb.AppendLine($"Total: {entries.Count} | Canonical confirmed: {entries.Count(e => e.Classification == ContentDecisionStatus.CanonicalConfirmed)} | " +
                       $"Placeholder unconfirmed (blocking): {entries.Count(e => e.IsBlocking)} | Technical only: {entries.Count(e => e.Classification == ContentDecisionStatus.TechnicalOnly)}");
        sb.AppendLine();

        var byGroup = entries.ToLookup(e => e.Group);
        foreach (var group in GroupOrder)
        {
            var groupEntries = byGroup[group].OrderBy(e => e.EntryId, StringComparer.Ordinal).ToList();
            if (groupEntries.Count == 0)
            {
                continue;
            }

            sb.AppendLine($"### Group: {group}");
            sb.AppendLine();
            sb.AppendLine("| ID | Document | Key | JSON Path | Classification | Blocking | Prod. allowed | Reason |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var e in groupEntries)
            {
                sb.AppendLine($"| {e.EntryId} | {e.DocumentType} | {e.Key} | `{e.JsonPath}` | {Classify(e.Classification)} | {(e.IsBlocking ? "YES" : "no")} | {(e.ProductionPublishAllowed ? "yes" : "NO")} | {e.SourceSectionOrReason} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Production publish guidance");
        sb.AppendLine();
        sb.AppendLine("The TEN_K__4D__INTERMEDIATE bundle still contains PLACEHOLDER_UNCONFIRMED content (phase min/max-week " +
                       "splits, phase intents/priorities, most workout-progression stage dosage, workout complexity tiers and " +
                       "generated-label component ownership, progression-modifier dosage, three runtime-condition value sets, " +
                       "and 9 of 12 peak-volume rows). **Production-channel publish remains blocked** by `PublishReadinessValidator` " +
                       "regardless of `--allow-unconfirmed-content`; a non-production channel (`Pilot`/`Draft`) requires that flag explicitly.");

        return sb.ToString();
    }
}
