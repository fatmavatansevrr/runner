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
/// Database-connection absence is measured separately from graph resolution.
/// <see cref="RealHost_ServiceResolution_OpensNoDatabaseConnection"/> uses the
/// test host's EF Core connection-opening interceptor/counter, so the claim is
/// executable without changing the production registration path.
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

    /// <summary>
    /// Pure unit-level check of the type's own CLR default — no host, no
    /// configuration, no DI. Split out (Phase 4G.4C.0) from the former
    /// <c>RealHost_CatalogLivePilotOptions_DefaultsToDisabled</c>, which
    /// incorrectly asserted this same expectation against the real,
    /// Development-forced host instead of the bare type default — see
    /// CROSS_PHASE_4G_4A_TO_4G_4B_V_AND_BACKEND_BASELINE_INDEPENDENT_AUDIT.md
    /// Part 8 (`STALE_TEST_EXPECTATION`, confirmed from source).
    /// </summary>
    [Fact]
    public void CatalogLivePilotOptions_TypeDefault_IsDisabled() =>
        Assert.False(new CatalogLivePilotOptions().Enabled);

    /// <summary>
    /// Confirms the REAL Development host's effective, intentional value —
    /// distinct from the type default above. `appsettings.Development.json`
    /// explicitly sets <c>CatalogLivePilot:Enabled = true</c> and is tracked/
    /// committed (not a local-only file), a deliberate per-environment
    /// design decision independently re-verified in the same audit's Part 8.
    /// This test intentionally asserts <see langword="true"/>, not
    /// <see langword="false"/> — asserting the type default here would be
    /// exactly the stale-expectation defect this split was created to fix.
    /// </summary>
    [Fact]
    public void RealHost_CatalogLivePilotOptions_DevelopmentEffectiveValue_IsEnabled()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<CatalogLivePilotOptions>>();
        Assert.True(options.Value.Enabled);
    }

    /// <summary>
    /// Resolving the five catalog target services from a single scope proves
    /// their graph is wired — if any registration were
    /// missing or mis-scoped, ASP.NET Core's Development-mode
    /// ValidateScopes/ValidateOnBuild would have already failed host
    /// construction itself (see class-level doc comment). Renamed (Phase
    /// 4G.4C.0) from ...WithNoDbConnection: that name and its final
    /// assertion overstated coverage — resolving without an exception does
    /// not, by itself, prove no database connection was opened. The
    /// feature-flag value assertion this test previously also carried has
    /// been removed entirely (it belonged to the type-default/real-host
    /// split above, not here) and the no-DB-connection claim now has its
    /// own instrumented test below instead of being silently implied by this
    /// test's old name.
    /// </summary>
    [Fact]
    public void RealHost_CatalogTargetServices_ResolveFromOneScope()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<IGenerationRouteDecider>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPreviewGenerator>());
        Assert.NotNull(sp.GetRequiredService<IGeneratedCatalogPlanPayloadValidator>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPlanConfirmationService>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPeakVolumeBandLoader>());
    }

    /// <summary>
    /// Phase 4G.5M closes the earlier unmeasured gap with test-only EF Core
    /// instrumentation. The counter is reset after host construction, then
    /// observes every connection-opening callback while the five catalog
    /// target services resolve from one scope. Zero callbacks proves that this
    /// resolution operation itself performs no database I/O.
    /// </summary>
    [Fact]
    public void RealHost_ServiceResolution_OpensNoDatabaseConnection()
    {
        _factory.ConnectionOpenCounter.Reset();

        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        Assert.NotNull(sp.GetRequiredService<IGenerationRouteDecider>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPreviewGenerator>());
        Assert.NotNull(sp.GetRequiredService<IGeneratedCatalogPlanPayloadValidator>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPlanConfirmationService>());
        Assert.NotNull(sp.GetRequiredService<ICatalogPeakVolumeBandLoader>());

        Assert.Equal(0, _factory.ConnectionOpenCounter.Count);
    }
}
