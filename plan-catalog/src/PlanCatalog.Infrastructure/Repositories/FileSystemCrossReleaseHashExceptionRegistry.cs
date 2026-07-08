using System.Text.Json.Nodes;
using PlanCatalog.Core.Ports;

namespace PlanCatalog.Infrastructure.Repositories;

/// <summary>Reads artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json.</summary>
public sealed class FileSystemCrossReleaseHashExceptionRegistry : ICrossReleaseHashExceptionRegistry
{
    private readonly HashSet<(string DocumentType, string Key, int Version, string ReleaseVersion, string ObservedContentHash)> _anomalies = [];

    public FileSystemCrossReleaseHashExceptionRegistry(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var root = JsonNode.Parse(File.ReadAllText(filePath));
        foreach (var entry in root?["exceptions"]?.AsArray() ?? [])
        {
            if (entry is null)
            {
                continue;
            }

            var documentType = entry["documentType"]!.GetValue<string>();
            var key = entry["key"]!.GetValue<string>();
            var version = entry["version"]!.GetValue<int>();

            foreach (var anomaly in entry["anomalies"]?.AsArray() ?? [])
            {
                if (anomaly is null)
                {
                    continue;
                }

                var releaseVersion = anomaly["releaseVersion"]!.GetValue<string>();
                var observedHash = anomaly["observedContentHash"]!.GetValue<string>();
                _anomalies.Add((documentType, key, version, releaseVersion, observedHash));
            }
        }
    }

    public bool IsKnownException(string documentType, string key, int version, string releaseVersion, string observedContentHash) =>
        _anomalies.Contains((documentType, key, version, releaseVersion, observedContentHash));
}
