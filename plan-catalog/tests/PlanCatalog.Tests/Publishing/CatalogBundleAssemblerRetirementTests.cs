using PlanCatalog.Contracts;
using PlanCatalog.Core.Ports;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

public sealed class CatalogBundleAssemblerRetirementTests
{
    private static readonly SystemTextJsonCanonicalSerializer Serializer = new();
    private static readonly Sha256ContentHasher Hasher = new();

    private sealed class FakeRetirementLedger(params (string DocumentType, string Key, int Version)[] retired) : IRetirementLedger
    {
        public bool IsRetired(string documentType, string key, int version) =>
            retired.Contains((documentType, key, version));
    }

    [Fact]
    public void Assemble_RetiredDependency_ThrowsAndRefusesNewBundle()
    {
        var fixture = new CombinationFixture();
        var snapshot = CatalogStamper.StampAsPublished(Serializer, Hasher, fixture.BuildSnapshot());
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);
        var ledger = new FakeRetirementLedger((DocumentTypes.ProgressionModifier, fixture.ProgressionModifier.Metadata.Key, fixture.ProgressionModifier.Metadata.Version));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            assembler.Assemble(snapshot, fixture.Combination.Metadata.Key, fixture.Combination.Metadata.Version, ledger));

        Assert.Contains("RETIRED", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Assemble_NoRetirement_Succeeds()
    {
        var fixture = new CombinationFixture();
        var snapshot = CatalogStamper.StampAsPublished(Serializer, Hasher, fixture.BuildSnapshot());
        var assembler = new CatalogBundleAssembler(Serializer, Hasher);

        var bundle = assembler.Assemble(snapshot, fixture.Combination.Metadata.Key, fixture.Combination.Metadata.Version);

        Assert.Equal(fixture.Combination.Metadata.Key, bundle.BundleKey);
    }
}
