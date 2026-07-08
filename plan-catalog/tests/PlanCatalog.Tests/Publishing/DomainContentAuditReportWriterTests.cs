using System.Text.Json;
using PlanCatalog.Infrastructure.Audit;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

public sealed class DomainContentAuditReportWriterTests
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

    [Fact]
    public void Write_ProducesJsonAndMarkdownReportsUnderArtifactsAudits()
    {
        var (jsonPath, mdPath) = DomainContentAuditReportWriter.Write(RepoRoot());

        Assert.True(File.Exists(jsonPath));
        Assert.True(File.Exists(mdPath));
        Assert.EndsWith("ten-k-pilot-domain-decision-audit.json", jsonPath, StringComparison.Ordinal);
        Assert.EndsWith("ten-k-pilot-domain-decision-audit.md", mdPath, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("boundaryAudit", out var boundary));
        Assert.Contains("PlanCatalog.Core", boundary.GetProperty("validationIssueLocation").GetString());
        Assert.Contains("PlanCatalog.Core", boundary.GetProperty("catalogDocumentMetadataLocation").GetString());

        Assert.True(root.TryGetProperty("domainContentDecisions", out var decisions));
        Assert.True(decisions.GetArrayLength() > 0);

        Assert.True(root.TryGetProperty("summary", out var summary));
        Assert.True(summary.GetProperty("placeholderUnconfirmed").GetInt32() > 0);
        Assert.True(summary.GetProperty("canonicalConfirmed").GetInt32() > 0);
    }

    [Fact]
    public void Write_MarkdownReportListsEveryEntryAndBlockingCount()
    {
        var (_, mdPath) = DomainContentAuditReportWriter.Write(RepoRoot());
        var content = File.ReadAllText(mdPath);

        Assert.Contains("Domain content decisions", content, StringComparison.Ordinal);
        Assert.Contains("PLACEHOLDER_UNCONFIRMED", content, StringComparison.Ordinal);
        Assert.Contains("CANONICAL_CONFIRMED", content, StringComparison.Ordinal);
        Assert.Contains("TEN_K_MASTER", content, StringComparison.Ordinal);
    }
}
