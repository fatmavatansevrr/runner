using System.Collections.Generic;
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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres",
            });
        });
    }
}
