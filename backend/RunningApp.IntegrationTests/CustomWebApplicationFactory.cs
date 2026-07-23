using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Boots the real Api host (real controllers, real DbContext, real Postgres)
/// in-process. Forces the Development environment so the dev-only
/// /api/v1/testing/reset endpoint is reachable, and pins the connection
/// string explicitly so tests don't depend on appsettings file resolution.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// appsettings.Development.json's PlanCatalog:CatalogRootPath
    /// ("../../plan-catalog/catalog") is relative to the Api project's own
    /// content root, which only resolves correctly when the process's
    /// working directory is that project (e.g. `dotnet run` from
    /// RunningApp.Api). Under `dotnet test`, the working directory is the
    /// test host's own output directory, so the same relative path resolves
    /// to a nonexistent location and any live-catalog-routed request (e.g.
    /// TenK/Intermediate/4-day) fails to load its candidate. Override with
    /// an absolute path computed the same way TestPlanServicesFactory.RepoRoot()
    /// already does for the non-HTTP catalog tests.
    /// </summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (expected a directory containing both 'backend' and 'plan-catalog').");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres",
                ["PlanCatalog:CatalogRootPath"] = Path.Combine(RepoRoot(), "plan-catalog", "catalog"),
            });
        });
    }
}
