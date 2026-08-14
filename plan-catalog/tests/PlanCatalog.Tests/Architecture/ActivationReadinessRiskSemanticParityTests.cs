using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

/// <summary>
/// JSON is the structured machine-readable authority; Markdown is its human
/// semantic projection. Parity deliberately compares stable identifiers and
/// markers, never full prose, punctuation, wrapping, or Markdown styling.
/// </summary>
public sealed class ActivationReadinessRiskSemanticParityTests
{
    private static readonly string[] RequiredIds =
    {
        "TD-COREHORIZON-ALLOCATOR-UNWIRED-001",
        "TD-NOTEVALUATED-FALLBACK-001",
        "TD-PACESOURCE-001",
        "TD-PACESOURCE-002",
        "TD-ALLOCATION-PRIORITY-001",
        "TD-FOUNDATION-COMPRESSION-001",
        "TD-VOLUME-CAP-UNENFORCED-001",
        "TD-RUNWAY-VALIDATOR-EXHAUSTIVENESS-001",
    };

    private static readonly IReadOnlyDictionary<string, string[]> StableMarkers =
        new Dictionary<string, string[]>
        {
            [RequiredIds[0]] = new[] { "UNWIRED_COMPONENT_INTEGRATION_UNDECIDED", "CoreHorizonDecision", "AvailableFullWeeks" },
            [RequiredIds[1]] = new[] { "DECISION_REQUIRED", "PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE", "Phase 4G.3B.6.1" },
            [RequiredIds[2]] = new[] { "DEFERRED_REGISTRY_VALUE_NOT_EMITTED", "PACE_SOURCE_IN", "ESTIMATED" },
            [RequiredIds[3]] = new[] { "UNWIRED_CONTEXT_FIELD_LIFECYCLE_UNDECIDED", "AsOfDate", "PaceSourceResolver" },
            [RequiredIds[4]] = new[] { "CATALOG_AUTHORING_DECISION_REQUIRED", "Foundation-first", "Phase 4G.3B.8" },
            [RequiredIds[5]] = new[] { "DECISION_REQUIRED", "CORE_ENTRY_READINESS_IN", "Phase 4G.3B.9" },
            [RequiredIds[6]] = new[] { "DECISION_REQUIRED", "HardMaxWeeklyIncreaseRatio", "Phase 4G.3B.7" },
            [RequiredIds[7]] = new[] { "DECISION_REQUIRED", "PreparationRunwayPlusCore", "Phase 4G.4B.1" },
        };

    [Fact]
    public void RequiredRecords_HaveStableSemanticParityAcrossCanonicalJsonAndMarkdownProjection()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var jsonById = document.RootElement.GetProperty("risks").EnumerateArray()
            .ToDictionary(r => r.GetProperty("id").GetString()!, r => r.Clone());
        var markdownById = ParseMarkdownRows();

        foreach (var id in RequiredIds)
        {
            Assert.True(jsonById.TryGetValue(id, out var json), $"Canonical JSON is missing {id}.");
            Assert.True(markdownById.TryGetValue(id, out var row), $"Markdown projection is missing {id}.");

            var jsonStatus = json.GetProperty("status").GetString();
            Assert.Equal(jsonStatus, NormalizeStatus(row.Status));
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("classification").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("appliesToCandidateRootsFrom").GetString()));

            var jsonText = json.GetRawText();
            foreach (var marker in StableMarkers[id])
            {
                Assert.Contains(marker, jsonText, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(marker, row.FullText, StringComparison.OrdinalIgnoreCase);
            }

            var scopeTokens = ScopeTokens(json.GetProperty("appliesToCandidateRootsFrom").GetString()!);
            Assert.All(scopeTokens, token => Assert.Contains(NormalizeDashes(token), NormalizeDashes(row.Scope), StringComparison.OrdinalIgnoreCase));

            var source = json.GetProperty("source").GetString()!;
            var evidenceFiles = Regex.Matches(source, @"[A-Za-z0-9_.-]+\.(?:cs|md|json)")
                .Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            Assert.NotEmpty(evidenceFiles);
            var representedEvidence = evidenceFiles.Where(file =>
                row.FullText.Contains(file, StringComparison.OrdinalIgnoreCase) ||
                row.FullText.Contains(Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase));
            Assert.All(representedEvidence, file => Assert.Contains(Path.GetFileNameWithoutExtension(file),
                row.FullText, StringComparison.OrdinalIgnoreCase));

            if (jsonStatus == "CLOSED")
            {
                Assert.True(json.TryGetProperty("closureNote", out var closure));
                Assert.False(string.IsNullOrWhiteSpace(closure.GetString()));
                var phase = Regex.Match(closure.GetString()!, @"Phase\s+4G\.[0-9A-Za-z.]+", RegexOptions.IgnoreCase);
                Assert.True(phase.Success, $"{id} closure lacks a stable phase marker.");
                Assert.Contains(phase.Value, row.FullText, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Assert.Equal("OPEN", jsonStatus);
            }
        }
    }

    [Fact]
    public void RequiredOpenClosedStates_ArePreservedExactly()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(JsonPath()));
        var statuses = document.RootElement.GetProperty("risks").EnumerateArray()
            .ToDictionary(r => r.GetProperty("id").GetString()!, r => r.GetProperty("status").GetString());

        Assert.Equal("CLOSED", statuses["TD-COREHORIZON-ALLOCATOR-UNWIRED-001"]);
        Assert.Equal("OPEN", statuses["TD-NOTEVALUATED-FALLBACK-001"]);
        Assert.Equal("OPEN", statuses["TD-PACESOURCE-001"]);
        Assert.Equal("OPEN", statuses["TD-PACESOURCE-002"]);
    }

    private sealed record MarkdownRow(string Statement, string Scope, string Status, string FullText);

    private static Dictionary<string, MarkdownRow> ParseMarkdownRows()
    {
        var lines = File.ReadAllLines(MarkdownPath());
        var header = Array.FindIndex(lines, line => line.TrimStart().StartsWith("| ID |", StringComparison.Ordinal));
        Assert.True(header >= 0);
        var result = new Dictionary<string, MarkdownRow>();
        for (var i = header + 2; i < lines.Length && lines[i].TrimStart().StartsWith('|'); i++)
        {
            var cells = lines[i].Trim().Trim('|').Split('|');
            Assert.True(cells.Length >= 5, $"Malformed Markdown governance row at line {i + 1}.");
            var id = cells[0].Trim().Trim('`');
            result[id] = new MarkdownRow(cells[1].Trim(), cells[3].Trim(), cells[^1].Trim(), lines[i]);
        }
        return result;
    }

    private static IEnumerable<string> ScopeTokens(string scope)
    {
        var preferred = new[]
        {
            "TEN_K__4D__INTERMEDIATE", "PACE_SOURCE_IN", "Foundation", "8-14",
            "Preparation Runway", "CatalogVolumeAndLongRunPlanner", "candidate"
        };
        var selected = preferred.Where(token => scope.Contains(token, StringComparison.OrdinalIgnoreCase)).ToArray();
        return selected.Length > 0 ? selected : new[] { "candidate" };
    }

    private static string NormalizeStatus(string status) =>
        status.Replace("*", string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    private static string NormalizeDashes(string value) => value.Replace('–', '-').Replace('—', '-');

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "artifacts", "audits")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Plan-catalog root not found.");
    }

    private static string JsonPath() => Path.Combine(Root(), "artifacts", "audits", "activation-readiness-risks.json");
    private static string MarkdownPath() => Path.Combine(Root(), "artifacts", "audits", "activation-readiness-risks.md");
}
