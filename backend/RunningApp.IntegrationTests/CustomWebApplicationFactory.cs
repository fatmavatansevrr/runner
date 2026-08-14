using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunningApp.Persistence;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Boots the real Api host (real controllers, real DbContext, real Postgres)
/// in-process. Forces the Development environment so the dev-only
/// /api/v1/testing/reset endpoint is reachable, and pins the connection
/// string explicitly so tests don't depend on appsettings file resolution.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environmentName;
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

    public CustomWebApplicationFactory()
        : this("Development", new Dictionary<string, string?>())
    {
    }

    internal CustomWebApplicationFactory(
        string environmentName,
        IReadOnlyDictionary<string, string?> configurationOverrides)
    {
        _environmentName = environmentName;
        _configurationOverrides = configurationOverrides;
    }

    public TestDbConnectionOpenCounter ConnectionOpenCounter { get; } = new();

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
        builder.UseEnvironment(_environmentName);
        // Top-level Program.cs reads Auth:Provider while services are being
        // registered, before ConfigureAppConfiguration callbacks are applied
        // by WebApplicationFactory. Host settings make test overrides visible
        // during that early startup decision as well as through IConfiguration.
        foreach (var (key, value) in _configurationOverrides)
        {
            if (value is not null)
            {
                builder.UseSetting(key, value);
            }
        }

        // The real Windows host adds EventLog by default. A fresh restricted
        // CI identity cannot create/write that machine-level source, and a
        // logging failure must never wrap the database exception a test is
        // deliberately exercising. Keep diagnosable console logging while
        // making the integration host entirely test-local and portable.
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        // Test-only instrumentation appended to the real AppDbContext.
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(ConnectionOpenCounter);
            services.AddSingleton<TestDbConnectionOpenInterceptor>();
            // Backend Integration Phase 4G.6C.2: mid-transaction failure-injection
            // seam (see TransactionFailureInjection.cs) -- registered ONLY here,
            // never in RunningApp.Api/Program.cs. Disabled by default (no-op
            // unless a test explicitly sets TransactionFailureInjectionState.FailWhenSqlContains/FailOnCommit).
            services.AddSingleton<TransactionFailureInjectionState>();
            services.AddSingleton<TransactionFailureInjectionCommandInterceptor>();
            services.AddSingleton<TransactionFailureInjectionTransactionInterceptor>();
            services.AddDbContext<AppDbContext>((sp, options) =>
                options.AddInterceptors(
                    sp.GetRequiredService<TestDbConnectionOpenInterceptor>(),
                    sp.GetRequiredService<TransactionFailureInjectionCommandInterceptor>(),
                    sp.GetRequiredService<TransactionFailureInjectionTransactionInterceptor>()));
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres",
                ["PlanCatalog:CatalogRootPath"] = Path.Combine(RepoRoot(), "plan-catalog", "catalog"),
            };
            foreach (var (key, value) in _configurationOverrides)
            {
                values[key] = value;
            }

            config.AddInMemoryCollection(values);
        });
    }
}

public sealed class TestDbConnectionOpenCounter
{
    private int _count;
    public int Count => Volatile.Read(ref _count);
    public void Reset() => Interlocked.Exchange(ref _count, 0);
    internal void Increment() => Interlocked.Increment(ref _count);
}

public sealed class TestDbConnectionOpenInterceptor : DbConnectionInterceptor
{
    private readonly TestDbConnectionOpenCounter _counter;

    public TestDbConnectionOpenInterceptor(TestDbConnectionOpenCounter counter) => _counter = counter;

    public override InterceptionResult ConnectionOpening(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        _counter.Increment();
        return result;
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        _counter.Increment();
        return ValueTask.FromResult(result);
    }
}
