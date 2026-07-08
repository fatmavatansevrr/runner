using System.Text.Json;
using System.Text.Json.Nodes;

namespace PlanCatalog.Cli.Commands;

/// <summary>
/// Records a retirement decision for a published artifact key/version without mutating any
/// immutable release directory — see brief §16 (RETIRED never overwrites historical bundles).
/// </summary>
internal static class RetireCommand
{
    public static int Retire(string documentType, string key, int version, bool json)
    {
        var ledgerPath = Path.Combine(CliPaths.ArtifactsDirectory, "appsel-plan-catalog", "retirements.json");
        Directory.CreateDirectory(Path.GetDirectoryName(ledgerPath)!);

        var entries = File.Exists(ledgerPath)
            ? JsonNode.Parse(File.ReadAllText(ledgerPath))!.AsArray()
            : [];

        var alreadyRetired = entries.Any(e =>
            string.Equals(e!["documentType"]!.GetValue<string>(), documentType, StringComparison.Ordinal) &&
            string.Equals(e["key"]!.GetValue<string>(), key, StringComparison.Ordinal) &&
            e["version"]!.GetValue<int>() == version);

        if (!alreadyRetired)
        {
            entries.Add(new JsonObject
            {
                ["documentType"] = documentType,
                ["key"] = key,
                ["version"] = version,
                ["retiredAtUtc"] = DateTime.UtcNow.ToString("O")
            });

            File.WriteAllText(ledgerPath, entries.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        return CliOutput.Report("retire", true, [], data: new { documentType, key, version, alreadyRetired }, json);
    }
}
