using System.Text.Json;
using System.Text.Json.Nodes;

namespace PlanCatalog.Cli.Commands;

/// <summary>
/// Records that a published release is superseded/non-production without mutating its immutable
/// release directory — see brief §16 (RETIRED/superseded state never rewrites history).
/// </summary>
internal static class SupersedeReleaseCommand
{
    public static int Supersede(string releaseVersion, string reason, string? supersededByVersion, bool json)
    {
        var ledgerPath = Path.Combine(CliPaths.ArtifactsDirectory, "appsel-plan-catalog", "release-status.json");
        Directory.CreateDirectory(Path.GetDirectoryName(ledgerPath)!);

        var entries = File.Exists(ledgerPath)
            ? JsonNode.Parse(File.ReadAllText(ledgerPath))!.AsArray()
            : [];

        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (string.Equals(entries[i]!["releaseVersion"]!.GetValue<string>(), releaseVersion, StringComparison.Ordinal))
            {
                entries.RemoveAt(i);
            }
        }

        entries.Add(new JsonObject
        {
            ["releaseVersion"] = releaseVersion,
            ["status"] = "SUPERSEDED",
            ["reason"] = reason,
            ["supersededByVersion"] = supersededByVersion,
            ["recordedAtUtc"] = DateTime.UtcNow.ToString("O")
        });

        File.WriteAllText(ledgerPath, entries.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return CliOutput.Report("supersede-release", true, [], data: new { releaseVersion, reason, supersededByVersion }, json);
    }
}
