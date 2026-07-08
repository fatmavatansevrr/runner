using System.Text.Json.Nodes;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// Repository-wide invariant: for every published artifact identity (documentType, key, version), every
/// historical release manifest under artifacts/appsel-plan-catalog/ must pin exactly one content hash.
/// A mismatch means an already-published, supposedly-immutable artifact was mutated in place — the same
/// class of defect investigated in combination-immutability-investigation.md (COMB-IMMUT-001). Known,
/// pre-existing mismatches are accounted for individually and explicitly via
/// artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json (no wildcards, no normalization) — any
/// new, unregistered mismatch still fails this test. See artifacts/audits/cross-release-hash-consistency-audit.md.
/// </summary>
public sealed class CrossReleaseHashConsistencyTests
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

    private sealed record ArtifactHashRecord(string DocumentType, string Key, int Version, string ContentHash, string ReleaseVersion);

    private static List<ArtifactHashRecord> ReadAllArtifactHashRecords(string releaseFamilyDirectory)
    {
        var records = new List<ArtifactHashRecord>();

        foreach (var releaseDir in Directory.GetDirectories(releaseFamilyDirectory))
        {
            var releaseVersion = Path.GetFileName(releaseDir);
            var manifestPath = Path.Combine(releaseDir, "release-manifest.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!;

            foreach (var array in new[] { "artifacts", "bundles" })
            {
                foreach (var node in manifest[array]?.AsArray() ?? [])
                {
                    records.Add(new ArtifactHashRecord(
                        node!["documentType"]!.GetValue<string>(),
                        node["key"]!.GetValue<string>(),
                        node["version"]!.GetValue<int>(),
                        node["contentHash"]!.GetValue<string>(),
                        releaseVersion));
                }
            }
        }

        return records;
    }

    /// <summary>
    /// Verifies one identity group against the exception registry. Returns null if the group is
    /// consistent or fully accounted for; otherwise returns a diagnostic message describing exactly
    /// what is unaccounted for.
    /// </summary>
    private static string? VerifyGroup(
        (string DocumentType, string Key, int Version) identity,
        List<ArtifactHashRecord> records,
        CrossReleaseHashExceptionRegistry exceptions)
    {
        var distinctHashes = records.Select(r => r.ContentHash).Distinct().ToList();
        if (distinctHashes.Count <= 1)
        {
            return null;
        }

        if (!exceptions.TryGet(identity.DocumentType, identity.Key, identity.Version, out var entry) || entry is null)
        {
            return $"UNREGISTERED mismatch for {identity.DocumentType}/{identity.Key} v{identity.Version}: " +
                   string.Join("; ", records.Select(r => $"{r.ReleaseVersion}={r.ContentHash}"));
        }

        var problems = new List<string>();

        foreach (var record in records)
        {
            if (record.ContentHash == entry.CanonicalContentHash)
            {
                continue;
            }

            var anomaly = entry.Anomalies.FirstOrDefault(a => a.ReleaseVersion == record.ReleaseVersion);
            if (anomaly is null)
            {
                problems.Add($"release '{record.ReleaseVersion}' has hash '{record.ContentHash}' which is neither the canonical hash nor a registered anomaly");
            }
            else if (anomaly.ObservedContentHash != record.ContentHash)
            {
                problems.Add($"release '{record.ReleaseVersion}' has hash '{record.ContentHash}' but the registered anomaly expects exactly '{anomaly.ObservedContentHash}'");
            }
        }

        var registeredButNotObserved = entry.Anomalies
            .Where(a => !records.Any(r => r.ReleaseVersion == a.ReleaseVersion))
            .Select(a => $"registered anomaly for release '{a.ReleaseVersion}' was not observed in any manifest")
            .ToList();
        problems.AddRange(registeredButNotObserved);

        return problems.Count == 0 ? null : $"{identity.DocumentType}/{identity.Key} v{identity.Version}: " + string.Join(" | ", problems);
    }

    [Fact]
    public void AllHistoricalReleases_SameArtifactIdentityAlwaysHasSameHash()
    {
        var releaseFamilyDirectory = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog");
        var exceptions = CrossReleaseHashExceptionRegistry.Load(Path.Combine(releaseFamilyDirectory, "cross-release-hash-exceptions.json"));

        var allRecords = ReadAllArtifactHashRecords(releaseFamilyDirectory);
        Assert.NotEmpty(allRecords);

        var groups = allRecords.GroupBy(r => (r.DocumentType, r.Key, r.Version));

        var failures = new List<string>();
        foreach (var group in groups)
        {
            var problem = VerifyGroup(group.Key, group.ToList(), exceptions);
            if (problem is not null)
            {
                failures.Add(problem);
            }
        }

        Assert.True(failures.Count == 0, "Cross-release hash consistency violations found:\n" + string.Join("\n", failures));
    }

    [Fact]
    public void ScanCoversAllExpectedArtifactTypesAndAtLeastTheKnownReleases()
    {
        var releaseFamilyDirectory = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog");
        var allRecords = ReadAllArtifactHashRecords(releaseFamilyDirectory);

        var releaseVersionsSeen = allRecords.Select(r => r.ReleaseVersion).Distinct().ToList();
        foreach (var expectedRelease in new[] { "1.0.0", "0.1.0-pilot", "0.2.0-pilot", "0.3.0-pilot", "0.4.0-pilot" })
        {
            Assert.Contains(expectedRelease, releaseVersionsSeen);
        }

        var documentTypesSeen = allRecords.Select(r => r.DocumentType).Distinct().ToList();
        foreach (var expectedType in new[]
        {
            "TEMPLATE_COMBINATION", "PLAN_TEMPLATE", "RUN_LAYOUT", "LEVEL_MODIFIER",
            "PROGRESSION_MODIFIER", "WORKOUT_PROGRESSION", "WORKOUT_DEFINITION",
            "RULE_PACK", "PEAK_VOLUME_BAND_POLICY", "RUNTIME_CONDITION_VALUE_REGISTRY",
            "PUBLISHED_TEMPLATE_BUNDLE"
        })
        {
            Assert.Contains(expectedType, documentTypesSeen);
        }
    }

    [Fact]
    public void ExceptionRegistry_EveryAnomalyIsActuallyObservedInSomeRelease()
    {
        var releaseFamilyDirectory = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog");
        var exceptions = CrossReleaseHashExceptionRegistry.Load(Path.Combine(releaseFamilyDirectory, "cross-release-hash-exceptions.json"));
        var allRecords = ReadAllArtifactHashRecords(releaseFamilyDirectory);

        foreach (var identity in new (string, string, int)[]
        {
            ("TEMPLATE_COMBINATION", "TEN_K__4D__INTERMEDIATE", 1),
            ("WORKOUT_DEFINITION", "EASY_STANDARD", 1),
            ("WORKOUT_DEFINITION", "FARTLEK", 1),
            ("WORKOUT_DEFINITION", "LONG_RUN_STANDARD", 1),
            ("WORKOUT_DEFINITION", "THRESHOLD_TEMPO", 1),
            ("PEAK_VOLUME_BAND_POLICY", "PEAK_VOLUME_BANDS_V1", 1),
            ("PUBLISHED_TEMPLATE_BUNDLE", "TEN_K__4D__INTERMEDIATE", 1),
        })
        {
            Assert.True(exceptions.TryGet(identity.Item1, identity.Item2, identity.Item3, out var entry));
            foreach (var anomaly in entry!.Anomalies)
            {
                Assert.Contains(allRecords, r =>
                    r.DocumentType == identity.Item1 && r.Key == identity.Item2 && r.Version == identity.Item3 &&
                    r.ReleaseVersion == anomaly.ReleaseVersion && r.ContentHash == anomaly.ObservedContentHash);
            }
        }
    }

    [Fact]
    public void RemediatedArtifacts_NewCorrectedVersions_RequireNoExceptionEntries()
    {
        // WORKOUT-IMMUT-001 / PEAK-POLICY-IMMUT-001 / CASCADE-001: the genuinely new corrected artifacts
        // (workout v2 x4, peak-volume-policy v2, rule pack v2, combination v3) have never been published
        // before, so they must NOT appear in the exception registry at all — only the restored v1 (and
        // its already-superseded mutated historical releases) needs exceptions.
        var releaseFamilyDirectory = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog");
        var exceptions = CrossReleaseHashExceptionRegistry.Load(Path.Combine(releaseFamilyDirectory, "cross-release-hash-exceptions.json"));

        foreach (var identity in new (string DocumentType, string Key, int Version)[]
        {
            ("WORKOUT_DEFINITION", "EASY_STANDARD", 2),
            ("WORKOUT_DEFINITION", "FARTLEK", 2),
            ("WORKOUT_DEFINITION", "LONG_RUN_STANDARD", 2),
            ("WORKOUT_DEFINITION", "THRESHOLD_TEMPO", 2),
            ("PEAK_VOLUME_BAND_POLICY", "PEAK_VOLUME_BANDS_V1", 2),
            ("RULE_PACK", "APPSEL_RACE_PLAN_V1", 2),
            ("TEMPLATE_COMBINATION", "TEN_K__4D__INTERMEDIATE", 3),
        })
        {
            Assert.False(exceptions.TryGet(identity.DocumentType, identity.Key, identity.Version, out _),
                $"{identity.DocumentType}/{identity.Key} v{identity.Version} must not be a registered exception — it has never been published before.");
        }
    }

    [Fact]
    public void UnregisteredMismatch_FailsTheConsistencyCheck_UsingAnIsolatedFixture()
    {
        // Fully isolated, disposable fixture — never the real artifacts/appsel-plan-catalog/ tree.
        var tempRoot = Path.Combine(Path.GetTempPath(), "plan-catalog-hash-consistency-negative", Guid.NewGuid().ToString("N"));
        try
        {
            var releaseA = Path.Combine(tempRoot, "release-a");
            var releaseB = Path.Combine(tempRoot, "release-b");
            Directory.CreateDirectory(releaseA);
            Directory.CreateDirectory(releaseB);

            File.WriteAllText(Path.Combine(releaseA, "release-manifest.json"),
                """{"artifacts":[{"documentType":"WORKOUT_DEFINITION","key":"FAKE_WORKOUT","version":1,"contentHash":"aaa"}],"bundles":[]}""");
            File.WriteAllText(Path.Combine(releaseB, "release-manifest.json"),
                """{"artifacts":[{"documentType":"WORKOUT_DEFINITION","key":"FAKE_WORKOUT","version":1,"contentHash":"bbb"}],"bundles":[]}""");

            var emptyExceptionsPath = Path.Combine(tempRoot, "cross-release-hash-exceptions.json");
            File.WriteAllText(emptyExceptionsPath, """{"exceptions":[]}""");

            var records = ReadAllArtifactHashRecords(tempRoot);
            var exceptions = CrossReleaseHashExceptionRegistry.Load(emptyExceptionsPath);

            var problem = VerifyGroup(("WORKOUT_DEFINITION", "FAKE_WORKOUT", 1), records, exceptions);

            Assert.NotNull(problem);
            Assert.Contains("UNREGISTERED", problem, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }
}
