using System.Text.Json.Nodes;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Infrastructure.Repositories;

/// <summary>Reads the release-status ledger written by the CLI <c>supersede-release</c> command.</summary>
public sealed class FileSystemReleaseStatusLedger : IReleaseStatusLedger
{
    private readonly Dictionary<string, ReleaseStatusEntry> _entries = new(StringComparer.Ordinal);

    public FileSystemReleaseStatusLedger(string ledgerFilePath)
    {
        if (!File.Exists(ledgerFilePath))
        {
            return;
        }

        var entries = JsonNode.Parse(File.ReadAllText(ledgerFilePath))?.AsArray() ?? [];
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            var releaseVersion = entry["releaseVersion"]!.GetValue<string>();
            var reason = entry["reason"]!.GetValue<string>();
            var supersededBy = entry["supersededByVersion"]?.GetValue<string?>();
            _entries[releaseVersion] = new ReleaseStatusEntry(releaseVersion, reason, supersededBy);
        }
    }

    public ReleaseStatusEntry? GetSupersededStatus(string releaseVersion) =>
        _entries.GetValueOrDefault(releaseVersion);
}
