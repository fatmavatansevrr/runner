using System.Text.RegularExpressions;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayEngine;

/// <summary>
/// Backend Integration Phase 4G.6A.3B — governance proofs for the new
/// production-owned <c>PreparationRunwayEngine</c> folder, mirroring
/// the established pattern in <c>PreparationRunwayContractsTests.cs</c>
/// (production ownership, dark/unwired status, no DI registration, no live
/// invocation). This is an ADDITIONAL, narrowly-scoped governance surface
/// for the new folder -- it does not modify, weaken, or replace the
/// existing PreparationRunway contracts-folder gate, which was found (see
/// PHASE4G_6A_3B_...md section 4) to already permit this new folder without
/// any change, since its own scans explicitly exclude the PreparationRunway
/// contracts folder path and its forbidden-symbol lists were never
/// scoped to the new engine's differently-named types.
/// </summary>
public sealed class PreparationRunwayAllocationDarkGovernanceTests
{
    [Fact]
    public void ProductionEngine_ExistsInTheProductionApplicationAssembly()
    {
        var path = Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayEngine", "PreparationRunwayBlockAllocationEngine.cs");
        Assert.True(File.Exists(path), "The generic allocation engine must be owned by the production RunningApp.Application assembly.");
        Assert.Contains("namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;", File.ReadAllText(path));
    }

    [Fact]
    public void NoAllocatorEngineFileRemainsOwnedByTheIntegrationTestProject()
    {
        var obsoletePath = Path.Combine(RepoRoot(), "backend", "RunningApp.IntegrationTests", "RuntimeCatalog", "Schedule", "PreparationRunway", "PreparationRunwayBlockAllocator.cs");
        Assert.False(File.Exists(obsoletePath), "Phase 4G.6A.3's test-project-owned reference engine must be removed after extraction -- no duplicate allocator may remain.");
    }

    [Fact]
    public void DarkReachability_NoDiRegistrationOrInvocationOutsideApprovedDarkOrchestrator()
    {
        var findings = ProductionSources()
            .SelectMany(file => FindForbiddenWiring(file.Path, file.Source))
            .ToArray();

        Assert.Empty(findings);
    }

    [Fact]
    public void DarkReachability_AdversarialWiringWouldBeDetected()
    {
        var findings = FindForbiddenWiring("Program.cs", "services.AddScoped<PreparationRunwayBlockAllocationEngine>()");
        Assert.NotEmpty(findings);

        var findings2 = FindForbiddenWiring("CatalogPreviewGenerator.cs", "PreparationRunwayBlockAllocationEngine.Allocate(runwayWeeks, policies)");
        Assert.NotEmpty(findings2);
    }

    private static IReadOnlyList<(string Path, string Source)> ProductionSources()
    {
        var repo = RepoRoot();
        return new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repo, "backend", project), "*.cs", SearchOption.AllDirectories))
            // Phase 4G.6A.4H and Phase 4I.5 add exactly two approved
            // production-owned dark consumers (TenKPreparationRunwayDarkOrchestrator
            // and LongHorizonStructuralMaterializer, respectively -- the latter
            // reuses the same allocation/binding/week-materialization stages for
            // its own 21-52 week structural join, stopping before any
            // numeric/calendar/pace stage). Excluding these internal folders does
            // not permit API, preview, service, infrastructure, or persistence
            // reachability.
            .Where(path => !path.Contains("\\bin\\") && !path.Contains("\\obj\\") &&
                           !path.Contains(Path.Combine("Schedule", "PreparationRunwayEngine")) &&
                           !path.Contains(Path.Combine("Schedule", "PreparationRunwayOrchestration")) &&
                           !path.Contains(Path.Combine("Schedule", "LongHorizon")))
            .Select(path => (Path: path, Source: File.ReadAllText(path)))
            .ToArray();
    }

    private static IEnumerable<string> FindForbiddenWiring(string path, string source)
    {
        var forbidden = new[]
        {
            "PreparationRunwayBlockAllocationEngine", "TenKPreparationRunwayAllocationPolicyFactory",
        };
        return forbidden
            .Where(symbol => Regex.IsMatch(source, $@"\b{Regex.Escape(symbol)}\b"))
            .Select(symbol => $"{path}: wires or invokes {symbol}");
    }

    private static string RepoRoot() =>
        RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
}
