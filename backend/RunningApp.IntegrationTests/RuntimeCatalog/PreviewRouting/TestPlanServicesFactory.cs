using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.Services;
using RunningApp.Persistence;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — a fully-wired <see cref="PlanServices"/>
/// for tests, now that <see cref="PlanServices"/> takes two additional
/// required constructor dependencies
/// (<see cref="IGenerationRouteDecider"/>, <see cref="ICatalogPreviewGenerator"/>).
/// Centralizes the wiring so every *NotWiredToGenerationTests file (Phase
/// 4C-4D.5.1) and SafeTemplateSelectionTests (Phase 0) construct
/// <see cref="PlanServices"/> identically, using the REAL plan-catalog
/// source tree (read-only) for the eligibility gate/loader, exactly as
/// every other real-catalog-reading test in this project already does.
///
/// Backend Integration Phase 4E.2: adds <see cref="ICatalogPlanConfirmationService"/>
/// as an additional required constructor dependency. The confirmation service
/// depends only on <see cref="AppDbContext"/> and a logger — it does NOT
/// depend on any catalog loading, routing, or resolver component.
/// </summary>
public static class TestPlanServicesFactory
{
    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (expected a directory containing both 'backend' and 'plan-catalog').");
    }

    private static string RealCatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");

    public static PlanServices Create(
        AppDbContext context,
        IPlanGenerationEngine? engine = null,
        IGenerationRouteDecider? routeDecider = null)
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = RealCatalogRoot() }),
            NullLogger<PlanCatalogBundleLoader>.Instance);

        var gate = new CatalogCandidateEligibilityGate(bundleLoader);

        var orchestration = new RuntimeConditionResolutionService(
            new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());

        var skeletonOrchestrator = new CatalogPlanSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(),
            new CatalogRunLayoutResolver(),
            new CatalogStageToWeekContextFactory(),
            new CatalogStageToWeekMaterializer(),
            new GeneratedCatalogPlanSkeletonValidator());

        var catalogPreviewGenerator = new CatalogPreviewGenerator(gate, orchestration, skeletonOrchestrator);

        var catalogConfirmationService = new CatalogPlanConfirmationService(
            context,
            NullLogger<CatalogPlanConfirmationService>.Instance,
            new GeneratedCatalogPlanPayloadValidator());

        return new PlanServices(
            context,
            engine ?? new PlaceholderPlanGenerationEngine(context, NullLogger<PlaceholderPlanGenerationEngine>.Instance),
            NullLogger<PlanServices>.Instance,
            routeDecider ?? new LivePlanPreviewRoutingService(
                Options.Create(new CatalogLivePilotOptions()),
                Options.Create(new LocalCatalogAcceptanceOptions()),
                new FakeHostEnvironment("Production"),
                bundleLoader,
                NullLogger<LivePlanPreviewRoutingService>.Instance),
            catalogPreviewGenerator,
            catalogConfirmationService);
    }

    /// <summary>Minimal fake for LivePlanPreviewRoutingService's IHostEnvironment dependency (Phase 4F.9.3 local-acceptance override).</summary>
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "RunningApp.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
