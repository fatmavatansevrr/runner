using System.IO;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.6B: Production must not run the AllowAny CORS policy. Development
/// keeps it (Swagger/local tooling); Production is restrictive by default and
/// only opens to origins explicitly listed in Cors:AllowedOrigins.
/// </summary>
public sealed class CorsPolicyProductionSecurityTests
{
    private static string CatalogRootPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
            dir = dir.Parent;
        return dir is null
            ? throw new InvalidOperationException("Could not locate repo root.")
            : Path.Combine(dir.FullName, "plan-catalog", "catalog");
    }

    [Fact]
    public async Task Development_AllowsAnyOrigin()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://example.com");

        using var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Production_WithNoConfiguredOrigins_RejectsBrowserCrossOriginRequest()
    {
        using var factory = new CustomWebApplicationFactory(
            "Production",
            new Dictionary<string, string?>
            {
                ["Auth:Provider"] = "Mock",
                ["ProductionConfigurationValidation:Enabled"] = "false",
                ["PlanCatalog:CatalogRootPath"] = CatalogRootPath(),
            });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://example.com");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Production_WithExplicitAllowedOrigin_AllowsOnlyThatOrigin()
    {
        using var factory = new CustomWebApplicationFactory(
            "Production",
            new Dictionary<string, string?>
            {
                ["Auth:Provider"] = "Mock",
                ["ProductionConfigurationValidation:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = "https://approved.example.com",
                ["PlanCatalog:CatalogRootPath"] = CatalogRootPath(),
            });
        using var client = factory.CreateClient();

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/health");
        allowed.Headers.Add("Origin", "https://approved.example.com");
        using var allowedResponse = await client.SendAsync(allowed);
        Assert.True(allowedResponse.Headers.Contains("Access-Control-Allow-Origin"));

        using var rejected = new HttpRequestMessage(HttpMethod.Get, "/health");
        rejected.Headers.Add("Origin", "https://not-approved.example.com");
        using var rejectedResponse = await client.SendAsync(rejected);
        Assert.False(rejectedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
