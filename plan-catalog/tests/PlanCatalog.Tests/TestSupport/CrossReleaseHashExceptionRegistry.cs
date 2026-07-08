using System.Text.Json.Nodes;

namespace PlanCatalog.Tests.TestSupport;

/// <summary>
/// Reads artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json — the narrow, explicit,
/// immutable registry of known pre-existing cross-release content-hash mismatches. See
/// artifacts/audits/cross-release-hash-consistency-audit.md.
/// </summary>
public sealed class CrossReleaseHashExceptionRegistry
{
    public sealed record Anomaly(string ReleaseVersion, string ObservedContentHash, string Reason, string SupersedingRelease, string AuditReference);
    public sealed record ExceptionEntry(string DocumentType, string Key, int Version, string CanonicalContentHash, IReadOnlyList<Anomaly> Anomalies);

    private readonly Dictionary<(string DocumentType, string Key, int Version), ExceptionEntry> _entries;

    public static CrossReleaseHashExceptionRegistry Load(string filePath)
    {
        var root = JsonNode.Parse(File.ReadAllText(filePath)) ?? throw new InvalidOperationException($"Failed to parse '{filePath}'.");
        var entries = new List<ExceptionEntry>();

        foreach (var node in root["exceptions"]!.AsArray())
        {
            var documentType = node!["documentType"]!.GetValue<string>();
            var key = node["key"]!.GetValue<string>();
            var version = node["version"]!.GetValue<int>();
            var canonicalHash = node["canonicalContentHash"]!.GetValue<string>();

            var anomalies = node["anomalies"]!.AsArray()
                .Select(a => new Anomaly(
                    a!["releaseVersion"]!.GetValue<string>(),
                    a["observedContentHash"]!.GetValue<string>(),
                    a["reason"]!.GetValue<string>(),
                    a["supersedingRelease"]!.GetValue<string>(),
                    a["auditReference"]!.GetValue<string>()))
                .ToList();

            entries.Add(new ExceptionEntry(documentType, key, version, canonicalHash, anomalies));
        }

        return new CrossReleaseHashExceptionRegistry(entries);
    }

    private CrossReleaseHashExceptionRegistry(IEnumerable<ExceptionEntry> entries)
    {
        _entries = entries.ToDictionary(e => (e.DocumentType, e.Key, e.Version));
    }

    public bool TryGet(string documentType, string key, int version, out ExceptionEntry? entry) =>
        _entries.TryGetValue((documentType, key, version), out entry);
}
