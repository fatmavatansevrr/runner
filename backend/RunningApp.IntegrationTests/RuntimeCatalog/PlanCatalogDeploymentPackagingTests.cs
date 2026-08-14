using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net;
using System.Net.Http.Json;
using RunningApp.Application.RuntimeCatalog;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog;

public sealed class PlanCatalogDeploymentPackagingTests
{
    internal const int ExpectedRuntimeCatalogJsonFiles = 78;
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RunningApp.sln")))
            directory = directory.Parent;
        return directory?.Parent?.FullName
            ?? throw new InvalidOperationException("Repository root was not found.");
    }

    private static string SourceCatalog() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");

    [Fact]
    public void ExplicitAbsoluteOverride_IsAuthoritative()
    {
        var resolution = PlanCatalogRootResolver.Resolve(SourceCatalog(), Path.GetTempPath(), false);
        Assert.Equal(PlanCatalogRootSource.ExplicitConfiguration, resolution.Source);
        Assert.Equal(Path.GetFullPath(SourceCatalog()), resolution.CatalogRootPath);
    }

    [Fact]
    public void ExplicitRelativeOverride_IsRootedAtContentRoot_NotCurrentDirectory()
    {
        var contentRoot = Path.Combine(RepoRoot(), "backend", "RunningApp.Api");
        var resolution = PlanCatalogRootResolver.Resolve("../../plan-catalog/catalog", contentRoot, true);
        Assert.Equal(Path.GetFullPath(SourceCatalog()), resolution.CatalogRootPath);
    }

    [Fact]
    public void PackagedFallback_IsPreferredWhenNoOverrideExists()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"catalog-package-{Guid.NewGuid():N}");
        var packaged = Path.Combine(contentRoot, "plan-catalog", "catalog");
        Directory.CreateDirectory(packaged);
        try
        {
            var resolution = PlanCatalogRootResolver.Resolve(null, contentRoot, false);
            Assert.Equal(PlanCatalogRootSource.PackagedContentRoot, resolution.Source);
            Assert.Equal(Path.GetFullPath(packaged), resolution.CatalogRootPath);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public void ProductionWithoutOverrideOrPackage_FailsClearly()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"catalog-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRoot);
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                PlanCatalogRootResolver.Resolve(null, contentRoot, false));
            Assert.Contains("Runtime plan catalog is missing", exception.Message);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public void RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe()
    {
        var inventory = PlanCatalogPackageValidator.Validate(SourceCatalog());
        Assert.Equal(ExpectedRuntimeCatalogJsonFiles, inventory.JsonFileCount);
        Assert.Equal(inventory.RelativePaths.Count, inventory.RelativePaths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(inventory.RelativePaths, path =>
        {
            Assert.DoesNotContain('\\', path);
            using var _ = JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceCatalog(), path.Replace('/', Path.DirectorySeparatorChar))));
        });
    }

    [Fact]
    public void ApiProjectPackagesOnlyCatalogJsonIntoDeterministicTarget()
    {
        var project = File.ReadAllText(Path.Combine(RepoRoot(), "backend", "RunningApp.Api", "RunningApp.Api.csproj"));
        Assert.Contains("plan-catalog\\catalog\\**\\*.json", project);
        Assert.Contains("Link=\"plan-catalog\\catalog\\%(RecursiveDir)%(Filename)%(Extension)\"", project);
        Assert.Contains("CopyToPublishDirectory=\"PreserveNewest\"", project);
        Assert.DoesNotContain("plan-catalog\\artifacts", project);
        Assert.DoesNotContain("plan-catalog\\docs", project);
        Assert.DoesNotContain("plan-catalog\\tests", project);
    }
}

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PackagedPlanCatalogRealHttpSmokeTests
{
    [Fact]
    public async Task ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend")))
            directory = directory.Parent;
        var repo = directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
        var packagedCatalog = Path.Combine(
            repo, "backend", "RunningApp.Api", "bin", "Release", "net9.0", "plan-catalog", "catalog");
        Assert.Equal(PlanCatalogDeploymentPackagingTests.ExpectedRuntimeCatalogJsonFiles,
            PlanCatalogPackageValidator.Validate(packagedCatalog).JsonFileCount);

        await using var factory = new CustomWebApplicationFactory(
            "Development",
            new Dictionary<string, string?> { ["PlanCatalog:CatalogRootPath"] = packagedCatalog });
        using var client = factory.CreateClient();
        (await client.PostAsync("/api/v1/testing/reset", null)).EnsureSuccessStatusCode();

        var start = new DateOnly(2026, 9, 7);
        var response = await client.PostAsJsonAsync("/api/v1/plans/generate-preview/race/long-horizon", new
        {
            goal_distance = "ten_k",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = start.ToString("yyyy-MM-dd"),
            preferred_days = new[] { "mon", "wed", "fri", "sun" },
            long_run_day = "sun",
            race_date = start.AddDays(21 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average",
            recent_weekly_volume_km = 20.0,
            recent_longest_run_km = 8.0,
            recent_runs_per_week = 4,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal(1, json!["contract_version"]!.GetValue<int>());
        Assert.Equal(21, json["total_weeks"]!.GetValue<int>());
        Assert.Equal(21, json["structural_roadmap"]!.AsArray().Count);
    }
}
