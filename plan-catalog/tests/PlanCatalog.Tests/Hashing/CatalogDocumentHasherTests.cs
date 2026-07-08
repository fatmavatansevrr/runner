using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Hashing;

public sealed class CatalogDocumentHasherTests
{
    private readonly SystemTextJsonCanonicalSerializer _serializer = new();
    private readonly Sha256ContentHasher _hasher = new();

    private static ProgressionModifierDefinition Sample(string? contentHash = null) => new()
    {
        Metadata = Meta.Of("PROGRESSION_MODIFIER", "INTERMEDIATE_PROGRESSION_MODIFIER_V1", status: CatalogStatus.Published) with { ContentHash = contentHash },
        Experience = RunningExperience.Intermediate,
        MaximumComplexityTier = 2,
        MaximumHardSessionsPerWeek = 1,
        MainSetDoseMultiplier = 1.0m,
        AllowGoalPaceRehearsal = true,
        AllowSecondHardStimulus = false
    };

    [Fact]
    public void SameContent_ProducesSameHash()
    {
        var hashA = CatalogDocumentHasher.ComputeContentHash(_serializer, _hasher, Sample());
        var hashB = CatalogDocumentHasher.ComputeContentHash(_serializer, _hasher, Sample());

        Assert.Equal(hashA, hashB);
    }

    [Fact]
    public void ContentHashField_DoesNotAffectItsOwnHash()
    {
        var hashWithNull = CatalogDocumentHasher.ComputeContentHash(_serializer, _hasher, Sample(contentHash: null));
        var hashWithPlaceholder = CatalogDocumentHasher.ComputeContentHash(_serializer, _hasher, Sample(contentHash: "deadbeef"));

        Assert.Equal(hashWithNull, hashWithPlaceholder);
    }

    [Fact]
    public void ContentChange_ProducesDifferentHash()
    {
        var hashA = CatalogDocumentHasher.ComputeContentHash(_serializer, _hasher, Sample());
        var hashB = CatalogDocumentHasher.ComputeContentHash(_serializer, _hasher, Sample() with { MaximumHardSessionsPerWeek = 2 });

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public void Hash_IsLowercaseHex64()
    {
        var hash = CatalogDocumentHasher.ComputeContentHash(_serializer, _hasher, Sample());

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }
}
