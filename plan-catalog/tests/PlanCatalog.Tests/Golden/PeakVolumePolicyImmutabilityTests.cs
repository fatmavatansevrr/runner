using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Audit;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Golden;

/// <summary>
/// PEAK-POLICY-IMMUT-001: proves PEAK_VOLUME_BANDS_V1 was correctly remediated — v1 restored to its
/// exact earliest historically-published (1.0.0) content, v2 created as a genuinely new, distinct,
/// deterministically-hashed artifact carrying the corrected INTERMEDIATE rows. See
/// artifacts/audits/peak-volume-policy-immutability-remediation.md.
/// </summary>
public sealed class PeakVolumePolicyImmutabilityTests
{
    private const string EarliestHistoricalV1Hash = "c6eb3bc444fa7fe1e6624f75328bd31623b0855226c46213cd91172c228fb762";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static Core.Catalog.CatalogSourceSnapshot LoadSnapshot() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();

    private static string ComputeHash<T>(T document)
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        return CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, document!, "contentHash");
    }

    [Fact]
    public void RestoredPolicyV1_MatchesEarliestHistoricalReleaseHash()
    {
        var snapshot = LoadSnapshot();
        var v1 = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 1);

        Assert.Equal(EarliestHistoricalV1Hash, ComputeHash(v1));
    }

    [Fact]
    public void CorrectedPolicyV2_HasDistinctDeterministicHash()
    {
        var snapshot = LoadSnapshot();
        var v2 = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 2);

        var hashA = ComputeHash(v2);
        var hashB = ComputeHash(v2);

        Assert.Equal(hashA, hashB);
        Assert.NotEqual(EarliestHistoricalV1Hash, hashA);
    }

    [Fact]
    public void CorrectedPolicyV2_PreservesConfirmedIntermediateBands()
    {
        var snapshot = LoadSnapshot();
        var v2 = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 2);

        Assert.Contains(v2.Entries, e => e.DistanceFamily == DistanceFamily.TenK && e.Experience == RunningExperience.Intermediate && e.RunsPerWeek == 3 && e.MinimumKm == 22m && e.MaximumKm == 32m);
        Assert.Contains(v2.Entries, e => e.DistanceFamily == DistanceFamily.TenK && e.Experience == RunningExperience.Intermediate && e.RunsPerWeek == 4 && e.MinimumKm == 30m && e.MaximumKm == 42m);
        Assert.Contains(v2.Entries, e => e.DistanceFamily == DistanceFamily.TenK && e.Experience == RunningExperience.Intermediate && e.RunsPerWeek == 5 && e.MinimumKm == 36m && e.MaximumKm == 50m);
    }

    [Fact]
    public void RestoredPolicyV1_DoesNotInventNewOrAdvancedOrExperiencedRows()
    {
        var snapshot = LoadSnapshot();
        var v1 = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 1);
        var v2 = snapshot.PeakVolumeBandPolicies.Single(p => p.Metadata.Key == "PEAK_VOLUME_BANDS_V1" && p.Metadata.Version == 2);

        var v1NonIntermediate = v1.Entries.Where(e => e.Experience != RunningExperience.Intermediate).Select(e => (e.Experience, e.RunsPerWeek, e.MinimumKm, e.MaximumKm)).OrderBy(t => t.Experience).ThenBy(t => t.RunsPerWeek).ToList();
        var v2NonIntermediate = v2.Entries.Where(e => e.Experience != RunningExperience.Intermediate).Select(e => (e.Experience, e.RunsPerWeek, e.MinimumKm, e.MaximumKm)).OrderBy(t => t.Experience).ThenBy(t => t.RunsPerWeek).ToList();

        Assert.Equal(9, v1NonIntermediate.Count);
        Assert.Equal(v1NonIntermediate, v2NonIntermediate);
    }

    [Fact]
    public void UnconfirmedBands_RemainBlockingForProductionPublish()
    {
        Assert.True(PilotDomainContentAudit.HasBlockingUnconfirmedContent(DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 1));
        Assert.True(PilotDomainContentAudit.HasBlockingUnconfirmedContent(DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 2));
    }

    [Fact]
    public void ConfirmedIntermediateRows_AreNotBlocking_OnV2()
    {
        var blocking = PilotDomainContentAudit.BlockingEntriesFor(DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 2);

        Assert.DoesNotContain(blocking, e => e.JsonPath.Contains("INTERMEDIATE", StringComparison.Ordinal));
    }
}
