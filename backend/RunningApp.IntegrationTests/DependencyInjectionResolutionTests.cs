using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Provider-independent DI-resolution smoke test. Builds the REAL production
/// service provider using the actual registration path in
/// <c>RunningApp.Api/Program.cs</c> (via <see cref="CustomWebApplicationFactory"/>,
/// which boots <c>WebApplicationFactory&lt;Program&gt;</c> against the real
/// top-level-statement <c>Program</c> — no hand-copied duplicate
/// <c>ServiceCollection</c> registration list is maintained here).
///
/// <para>
/// <see cref="CustomWebApplicationFactory"/> forces the "Development"
/// environment (see its own doc comment), which has two effects this test
/// relies on:
/// </para>
/// <list type="bullet">
/// <item>
/// <c>appsettings.Development.json</c> sets <c>Auth:Provider = "Mock"</c>,
/// so <c>Program.cs</c> takes the mock-auth branch and never calls
/// <c>FirebaseApp.Create</c> / <c>GoogleCredential.GetApplicationDefault()</c>
/// — this test does not require any Firebase credential to be configured.
/// </item>
/// <item>
/// ASP.NET Core's default host-building behavior sets
/// <c>ServiceProviderOptions.ValidateScopes</c> and <c>ValidateOnBuild</c> to
/// <see langword="true"/> whenever <c>IHostEnvironment.IsDevelopment()</c> is
/// true, so the real host is already built with scope validation enabled —
/// no separate hand-rolled <c>BuildServiceProvider(validateScopes: true)</c>
/// call is needed to get that guarantee.
/// </item>
/// </list>
///
/// <para>
/// No real PostgreSQL connection is opened by anything in this test.
/// <c>AddDbContext&lt;AppDbContext&gt;</c> registers <c>AppDbContext</c>
/// lazily — constructing the host, and constructing/resolving each service
/// below (including <see cref="ICatalogPlanConfirmationService"/>, whose
/// constructor merely stores its injected <c>AppDbContext</c> without
/// touching it), does not cause Npgsql to open a socket. This test never
/// calls any repository/query method that would.
/// </para>
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class DependencyInjectionResolutionTests
{
    private readonly CustomWebApplicationFactory _factory;

    public DependencyInjectionResolutionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void RealHost_ResolvesGenerationRouteDecider()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGenerationRouteDecider>();
        Assert.NotNull(service);
    }

    [Fact]
    public void RealHost_ResolvesCatalogPreviewGenerator()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICatalogPreviewGenerator>();
        Assert.NotNull(service);
    }

    [Fact]
    public void RealHost_ResolvesGeneratedCatalogPlanPayloadValidator()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGeneratedCatalogPlanPayloadValidator>();
        Assert.NotNull(service);
    }

    [Fact]
    public void RealHost_ResolvesCatalogPlanConfirmationService()
    {
        using var scope = _factory.Services.CreateScope();
        // Constructing this service only stores its injected AppDbContext —
        // it never opens a connection. See class-level doc comment.
        var service = scope.ServiceProvider.GetRequiredService<ICatalogPlanConfirmationService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void RealHost_ResolvesCatalogPeakVolumeBandLoader()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICatalogPeakVolumeBandLoader>();
        Assert.NotNull(service);
    }

    [Fact]
    public void RealHost_CatalogLivePilotOptions_DefaultsToDisabled()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<CatalogLivePilotOptions>>();
        Assert.False(options.Value.Enabled);
    }

    [Fact]
    public void RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection()
    {
        // Resolving every target type from a single scope proves the whole
        // graph is wired without requiring a live PostgreSQL connection —
        // if any registration were missing or mis-scoped, ASP.NET Core's
        // Development-mode ValidateScopes/ValidateOnBuild would have already
        // failed host construction itself (see class-level doc comment).
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<IGenerationRouteDecider>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPreviewGenerator>());
        Assert.NotNull(sp.GetRequiredService<IGeneratedCatalogPlanPayloadValidator>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPlanConfirmationService>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPeakVolumeBandLoader>());
        Assert.False(sp.GetRequiredService<IOptions<CatalogLivePilotOptions>>().Value.Enabled);
    }
}
