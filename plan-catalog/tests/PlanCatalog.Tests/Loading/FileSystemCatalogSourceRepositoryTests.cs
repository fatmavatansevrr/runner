using PlanCatalog.Infrastructure.Repositories;
using Xunit;

namespace PlanCatalog.Tests.Loading;

public sealed class FileSystemCatalogSourceRepositoryTests
{
    private static string CatalogDirectory() => Path.Combine(AppContext.BaseDirectory, "TestCatalog");

    [Fact]
    public void LoadSnapshot_LoadsRuntimeConditionValueRegistry()
    {
        var repository = new FileSystemCatalogSourceRepository(CatalogDirectory());
        var snapshot = repository.LoadSnapshot();

        Assert.Contains(snapshot.RuntimeConditionValueRegistries, r => r.Metadata.Key == "RUNTIME_CONDITION_VALUES_V1");
        Assert.Empty(snapshot.PrescriptionProfiles); // Historical catalogs are not synthesized into profiles.
    }

    [Fact]
    public void LoadSnapshot_MissingSubfolder_ReturnsEmptyList()
    {
        var repository = new FileSystemCatalogSourceRepository(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var snapshot = repository.LoadSnapshot();

        Assert.Empty(snapshot.PlanTemplates);
    }
}
