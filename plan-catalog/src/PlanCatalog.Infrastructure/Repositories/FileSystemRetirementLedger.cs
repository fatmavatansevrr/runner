using System.Text.Json.Nodes;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Infrastructure.Repositories;

/// <summary>Reads the retirement ledger written by the CLI <c>retire</c> command.</summary>
public sealed class FileSystemRetirementLedger : IRetirementLedger
{
    private readonly HashSet<(string DocumentType, string Key, int Version)> _retired = [];

    public FileSystemRetirementLedger(string ledgerFilePath)
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

            var documentType = entry["documentType"]!.GetValue<string>();
            var key = entry["key"]!.GetValue<string>();
            var version = entry["version"]!.GetValue<int>();
            _retired.Add((documentType, key, version));
        }
    }

    public bool IsRetired(string documentType, string key, int version) =>
        _retired.Contains((documentType, key, version));
}
