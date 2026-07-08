using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Ports;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Repositories;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Golden;

/// <summary>
/// WORKOUT-IMMUT-001: proves that EASY_STANDARD, FARTLEK, LONG_RUN_STANDARD, and THRESHOLD_TEMPO were
/// correctly remediated — v1 restored to its exact earliest historically-published content, v2 created
/// as a genuinely new, distinct, deterministically-hashed artifact carrying the corrected content — see
/// artifacts/audits/published-workout-immutability-remediation.md.
/// </summary>
public sealed class WorkoutArtifactImmutabilityTests
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

    private static Core.Catalog.CatalogSourceSnapshot LoadSnapshot() =>
        new FileSystemCatalogSourceRepository(Path.Combine(RepoRoot(), "catalog")).LoadSnapshot();

    private static string ComputeHash<T>(T document)
    {
        var serializer = new SystemTextJsonCanonicalSerializer();
        var hasher = new Sha256ContentHasher();
        return CatalogDocumentHasher.ComputeHashExcludingField(serializer, hasher, document!, "contentHash");
    }

    public static IEnumerable<object[]> WorkoutKeysAndEarliestHistoricalHashes() =>
    [
        ["EASY_STANDARD", "37990edcaf4f9253528bd41ae7428b4555f4c9fd95f3452930cf6db688086b80"],
        ["FARTLEK", "8652ed9aa01a0909ab1efffdacf1e029a164bd5784b505351b7296d6a5f89482"],
        ["LONG_RUN_STANDARD", "92d43af58804bf4f77ec2d97b85a127780b5065557ef86ccc5dbf3265f315452"],
        ["THRESHOLD_TEMPO", "3da4f9606f91e91323e0cf5aeb6763bbad6149319f037f32d44581f5a5522621"],
    ];

    [Theory]
    [MemberData(nameof(WorkoutKeysAndEarliestHistoricalHashes))]
    public void RestoredWorkoutV1_MatchesEarliestHistoricalReleaseHash(string key, string earliestHistoricalHash)
    {
        var snapshot = LoadSnapshot();
        var v1 = snapshot.Workouts.Single(w => w.Metadata.Key == key && w.Metadata.Version == 1);

        Assert.Equal(earliestHistoricalHash, ComputeHash(v1));
    }

    [Theory]
    [MemberData(nameof(WorkoutKeysAndEarliestHistoricalHashes))]
    public void WorkoutV2_HasDistinctDeterministicHash(string key, string v1Hash)
    {
        var snapshot = LoadSnapshot();
        var v2 = snapshot.Workouts.Single(w => w.Metadata.Key == key && w.Metadata.Version == 2);

        var hashA = ComputeHash(v2);
        var hashB = ComputeHash(v2);

        Assert.Equal(hashA, hashB);
        Assert.NotEqual(v1Hash, hashA);
    }

    [Theory]
    [InlineData("EASY_STANDARD")]
    [InlineData("FARTLEK")]
    [InlineData("LONG_RUN_STANDARD")]
    [InlineData("THRESHOLD_TEMPO")]
    public void V1AndV2_CoexistWithoutDuplicateKeyVersionErrors(string key)
    {
        var snapshot = LoadSnapshot();
        var result = Core.Validation.CatalogGraphValidator.Validate(snapshot);

        Assert.DoesNotContain(result.Issues, i => i.Code == "GRAPH_DUPLICATE_KEY_VERSION" && i.Message.Contains(key, StringComparison.Ordinal));
        Assert.Equal(2, snapshot.Workouts.Count(w => w.Metadata.Key == key));
    }

    [Theory]
    [InlineData("EASY_STANDARD")]
    [InlineData("FARTLEK")]
    [InlineData("LONG_RUN_STANDARD")]
    [InlineData("THRESHOLD_TEMPO")]
    public void CurrentActiveResolution_SelectsV2_DeterministicallyAndRetirementAware(string key)
    {
        var snapshot = LoadSnapshot();

        // No retirement ledger supplied: highest non-retired version wins, deterministically.
        var resolved = snapshot.FindWorkout(key);
        Assert.NotNull(resolved);
        Assert.Equal(2, resolved!.Metadata.Version);

        // Retiring v2 must deterministically fall back to v1, not fail or pick arbitrarily.
        var ledger = new FakeRetirementLedger((DocumentTypes.WorkoutDefinition, key, 2));
        var resolvedAfterRetirement = snapshot.FindWorkout(key, ledger);
        Assert.NotNull(resolvedAfterRetirement);
        Assert.Equal(1, resolvedAfterRetirement!.Metadata.Version);
    }

    [Fact]
    public void HistoricalReleases_StillResolveTheirOriginallyPublishedWorkoutV1Content()
    {
        var releaseFamilyDirectory = Path.Combine(RepoRoot(), "artifacts", "appsel-plan-catalog");

        foreach (var (release, key, expectedHash) in new[]
        {
            ("1.0.0", "EASY_STANDARD", "37990edcaf4f9253528bd41ae7428b4555f4c9fd95f3452930cf6db688086b80"),
            ("0.1.0-pilot", "FARTLEK", "8652ed9aa01a0909ab1efffdacf1e029a164bd5784b505351b7296d6a5f89482"),
        })
        {
            var path = Path.Combine(releaseFamilyDirectory, release, "workouts", $"{key}.v1.json");
            Assert.True(File.Exists(path), $"Expected historical workout file missing: {path}");

            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
            Assert.Equal(expectedHash, json["metadata"]!["contentHash"]!.GetValue<string>());
        }
    }

    private sealed class FakeRetirementLedger(params (string DocumentType, string Key, int Version)[] retired) : IRetirementLedger
    {
        public bool IsRetired(string documentType, string key, int version) =>
            retired.Contains((documentType, key, version));
    }
}
