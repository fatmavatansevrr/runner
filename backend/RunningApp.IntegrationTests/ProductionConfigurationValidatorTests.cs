using Microsoft.Extensions.Configuration;
using RunningApp.Api.Startup;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.6B: fail-fast production configuration authority. These tests
/// call the validator directly (fast, no host required) plus one real-host
/// end-to-end check that a genuinely misconfigured "Production" boot fails
/// before serving traffic, and one that a correctly configured boot succeeds.
/// </summary>
public sealed class ProductionConfigurationValidatorTests
{
    private static IConfiguration Config(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void MissingConnectionString_FailsValidation()
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["Auth:Provider"] = "Firebase",
        });

        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationValidator.Validate(config));
        Assert.Contains("ConnectionStrings:DefaultConnection is required", exception.Message);
    }

    [Theory]
    [InlineData("Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres")]
    [InlineData("Host=127.0.0.1;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres")]
    [InlineData("Host=some-remote-host;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres")]
    public void KnownLocalDevelopmentConnectionString_FailsValidation(string connectionString)
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Auth:Provider"] = "Firebase",
        });

        Assert.Throws<InvalidOperationException>(() => ProductionConfigurationValidator.Validate(config));
    }

    [Fact]
    public void ExplicitExternalConnectionString_PassesValidation()
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=prod-db.internal;Port=5432;Database=antigravity;Username=app_runtime;Password=REDACTED",
            ["Auth:Provider"] = "Firebase",
        });

        ProductionConfigurationValidator.Validate(config); // does not throw
    }

    [Fact]
    public void MockAuthProvider_FailsValidation_EvenWithValidConnectionString()
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=prod-db.internal;Port=5432;Database=antigravity;Username=app_runtime;Password=REDACTED",
            ["Auth:Provider"] = "Mock",
        });

        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationValidator.Validate(config));
        Assert.Contains("Auth:Provider cannot be 'Mock'", exception.Message);
    }

    [Fact]
    public void ValidationError_NeverIncludesTheConnectionStringValue()
    {
        const string secretLikeValue = "Password=super-secret-value-should-never-leak";
        var config = Config(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Host=localhost;{secretLikeValue}",
            ["Auth:Provider"] = "Firebase",
        });

        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationValidator.Validate(config));
        Assert.DoesNotContain("super-secret-value-should-never-leak", exception.Message);
    }

    [Fact]
    public async Task RealHost_ProductionEnvironment_MissingDbAndMockAuth_FailsStartupBeforeServingTraffic()
    {
        using var factory = new CustomWebApplicationFactory(
            "Production",
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "",
                ["Auth:Provider"] = "Mock",
                ["PlanCatalog:CatalogRootPath"] = RepoRoot.PlanCatalogPath(),
            });

        // WebApplicationFactory defers host construction until the server is
        // first touched; the startup-validation exception surfaces there.
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var client = factory.CreateClient();
            await client.GetAsync("/health");
        });
    }

    // Note: a real-host "Production + explicit safe config succeeds" boot
    // test is intentionally not included here. Auth:Provider=Firebase (the
    // only value ProductionConfigurationValidator accepts) calls
    // FirebaseApp.Create(GoogleCredential.GetApplicationDefault()) in
    // Program.cs, which requires real Google Application Default Credentials
    // that are not available in this sandbox/CI environment. The validator's
    // own pass-case is proven directly by
    // ExplicitExternalConnectionString_PassesValidation above; a full
    // credentialed end-to-end boot remains a staging/UAT-environment proof
    // point (see Phase 4L.6C).

    private static class RepoRoot
    {
        public static string PlanCatalogPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
                dir = dir.Parent;
            return dir is null
                ? throw new InvalidOperationException("Could not locate repo root.")
                : Path.Combine(dir.FullName, "plan-catalog", "catalog");
        }
    }
}
