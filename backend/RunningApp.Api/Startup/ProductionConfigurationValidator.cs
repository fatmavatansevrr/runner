using Microsoft.Extensions.Configuration;

namespace RunningApp.Api.Startup;

/// <summary>
/// Phase 4L.6B: Production:Enabled controls the fail-fast startup gate below.
/// Existing tests that boot the real host under the "Production" environment
/// name purely to exercise catalog-tier Production-only routing/fallback
/// behavior (not full production security posture) set this to false — see
/// PublishedCatalogNonDevelopmentEndToEndTests.Factory.
/// </summary>
public sealed record ProductionConfigurationValidationOptions
{
    public const string SectionName = "ProductionConfigurationValidation";
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// Fail-fast production configuration authority. Runs once at startup, before
/// the host begins accepting traffic, so a misconfigured deployment never
/// serves a single request instead of failing on the first one. Every check
/// here is a real Phase 4L.6 release blocker (checked-in localhost DB
/// fallback, Mock auth reachable in Production); it deliberately does not
/// duplicate the plan catalog's own fail-fast validation
/// (<see cref="RunningApp.Application.RuntimeCatalog.PlanCatalogPackageValidator"/>),
/// which already runs earlier in Program.cs and already cannot resolve the
/// Development repository fallback outside Development.
/// </summary>
public static class ProductionConfigurationValidator
{
    // Substrings of connection strings that identify a known local/development
    // authority. Matching is deliberately broad (not exact-string) so a
    // deployment cannot accidentally pass validation by reusing the
    // Development password against a differently named host.
    private static readonly string[] KnownInsecureConnectionFragments =
    [
        "Host=localhost",
        "Host=127.0.0.1",
        "Password=postgres",
    ];

    public static void Validate(IConfiguration configuration)
    {
        var errors = new List<string>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add(
                "ConnectionStrings:DefaultConnection is required in Production and must be supplied " +
                "by external configuration (environment variable, secret manager, or deployment-platform " +
                "secret) — it is intentionally absent from appsettings.json.");
        }
        else if (KnownInsecureConnectionFragments.Any(fragment =>
                     connectionString.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(
                "ConnectionStrings:DefaultConnection resolves to a known local/development database " +
                "authority and cannot be used in Production.");
        }

        var authProvider = configuration["Auth:Provider"] ?? "Firebase";
        if (string.Equals(authProvider, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Auth:Provider cannot be 'Mock' in Production.");
        }

        if (errors.Count > 0)
        {
            // Deliberately no secret value (connection string contents, auth
            // config) is included below — only the names of the settings and
            // a generic description of what is wrong with each.
            throw new InvalidOperationException(
                "Production configuration validation failed:\n - " + string.Join("\n - ", errors));
        }
    }
}
