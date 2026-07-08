using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.Manifests;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Serialization;

/// <summary>
/// A release-manifest.json written before Channel/UnconfirmedContentWarnings existed must still
/// deserialize — historical release verification must never break when the manifest shape grows.
/// </summary>
public sealed class CatalogReleaseManifestBackwardCompatibilityTests
{
    [Fact]
    public void LegacyManifestWithoutChannelOrWarnings_DeserializesWithDraftChannelDefault()
    {
        var legacyJson = """
        {
          "releaseKey": "appsel-plan-catalog",
          "releaseVersion": "1.0.0",
          "artifacts": [],
          "bundles": [],
          "manifestContentHash": "deadbeef"
        }
        """;

        var serializer = new SystemTextJsonCanonicalSerializer();
        var manifest = serializer.Deserialize<CatalogReleaseManifest>(legacyJson);

        Assert.Equal(ReleaseChannel.Draft, manifest.Channel);
        Assert.Empty(manifest.UnconfirmedContentWarnings);
        Assert.Equal("1.0.0", manifest.ReleaseVersion);
    }
}
