using System.Text.RegularExpressions;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

/// <summary>
/// Backend Integration Phase 4G.6A.4B — governance proofs for the new
/// production-owned <c>PreparationRunwayWorkoutBinding</c> folder, mirroring
/// the established pattern from Phase 4G.6A.3B's own governance tests
/// (production ownership, dark/unwired status, no DI registration, no live
/// invocation, no test-project-owned duplicate engine).
/// </summary>
public sealed class PreparationRunwayWorkoutBindingDarkGovernanceTests
{
    private static string RepoRoot() =>
        RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();

    [Fact]
    public void ProductionBinder_ExistsInTheProductionApplicationAssembly()
    {
        var path = Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayWorkoutBinding", "PreparationRunwayBlockWorkoutBindingEngine.cs");
        Assert.True(File.Exists(path), "The binder engine must be owned by the production RunningApp.Application assembly.");
        Assert.Contains("namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;", File.ReadAllText(path));
    }

    [Fact]
    public void NoBinderEngineFileIsOwnedByTheIntegrationTestProject()
    {
        var testProjectRoot = Path.Combine(RepoRoot(), "backend", "RunningApp.IntegrationTests");
        var files = Directory.GetFiles(testProjectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("\\bin\\") && !p.Contains("\\obj\\") && Path.GetFileName(p) != "PreparationRunwayWorkoutBindingDarkGovernanceTests.cs");

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            // The real engine's own class name must never be REDEFINED (declared) in the test
            // project -- only referenced/consumed, which is expected and fine.
            Assert.DoesNotContain("internal static class PreparationRunwayBlockWorkoutBindingEngine", source);
        }
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
        Assert.NotEmpty(FindForbiddenWiring("Program.cs", "services.AddScoped<PreparationRunwayBlockWorkoutBindingEngine>()"));
        Assert.NotEmpty(FindForbiddenWiring("CatalogPreviewGenerator.cs", "PreparationRunwayBlockWorkoutBindingEngine.Bind(request)"));
        Assert.NotEmpty(FindForbiddenWiring("CatalogWorkoutBinder.cs", "PreparationRunwayBlockProgressionCatalogReader.LoadAsync(root, key, version)"));
    }

    [Fact]
    public void PublicReachability_NoPublicDtoOrControllerReferencesTheBinderTypes()
    {
        var forbidden = new[]
        {
            "PreparationRunwayBlockWorkoutBindingRequest", "PreparationRunwayBlockWorkoutBindingResult",
            "PreparationRunwayBlockWorkoutBinding", "PreparationRunwayWorkoutReference",
            "PreparationRunwayBlockProgressionDefinition", "PreparationRunwayBlockProgressionStep",
        };

        var apiFiles = Directory.GetFiles(Path.Combine(RepoRoot(), "backend", "RunningApp.Api"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("\\bin\\") && !p.Contains("\\obj\\"));
        var dtoFiles = Directory.GetFiles(Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "DTOs"), "*.cs", SearchOption.AllDirectories);

        foreach (var file in apiFiles.Concat(dtoFiles))
        {
            var source = File.ReadAllText(file);
            foreach (var symbol in forbidden)
            {
                Assert.DoesNotContain(symbol, source);
            }
        }
    }

    [Fact]
    public void PersistenceReachability_NoEfOrPersistenceProjectReferencesTheBinderTypes()
    {
        var forbidden = new[] { "PreparationRunwayBlockWorkoutBindingEngine", "PreparationRunwayBlockProgressionCatalogReader", "PreparationRunwayBlockWorkoutReferenceValidator" };
        var persistenceFiles = Directory.GetFiles(Path.Combine(RepoRoot(), "backend", "RunningApp.Persistence"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("\\bin\\") && !p.Contains("\\obj\\"));

        foreach (var file in persistenceFiles)
        {
            var source = File.ReadAllText(file);
            foreach (var symbol in forbidden)
            {
                Assert.DoesNotContain(symbol, source);
            }
        }
    }

    private static IReadOnlyList<(string Path, string Source)> ProductionSources()
    {
        var repo = RepoRoot();
        return new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repo, "backend", project), "*.cs", SearchOption.AllDirectories))
            // Phase 4G.6A.4H and Phase 4I.5 (LongHorizonStructuralMaterializer,
            // reusing the same binding stage for its own 21-52 week structural
            // join, never for numeric/calendar/pace) are the two approved dark
            // composition consumers.
            .Where(path => !path.Contains("\\bin\\") && !path.Contains("\\obj\\") &&
                           !path.Contains(Path.Combine("Schedule", "PreparationRunwayWorkoutBinding")) &&
                           !path.Contains(Path.Combine("Schedule", "PreparationRunwayOrchestration")) &&
                           !path.Contains(Path.Combine("Schedule", "LongHorizon")))
            .Select(path => (Path: path, Source: File.ReadAllText(path)))
            .ToArray();
    }

    private static IEnumerable<string> FindForbiddenWiring(string path, string source)
    {
        var forbidden = new[]
        {
            "PreparationRunwayBlockWorkoutBindingEngine", "PreparationRunwayBlockProgressionCatalogReader",
            "PreparationRunwayBlockWorkoutReferenceValidator",
        };
        return forbidden
            // Phase 4G.6A.4D is an explicitly approved dark consumer of the
            // separate catalog-aware reference validator. It still may not
            // invoke the binder or progression reader, and the materializer's
            // own governance tests prove it is absent from DI/live/public/
            // persistence paths. Treating this typed dark validation call as
            // live binder reachability would be a false positive.
            .Where(symbol => !(symbol == "PreparationRunwayBlockWorkoutReferenceValidator" &&
                               path.Contains(Path.Combine("Schedule", "PreparationRunwayWeekMaterialization"))))
            .Where(symbol => Regex.IsMatch(source, $@"\b{Regex.Escape(symbol)}\b"))
            .Select(symbol => $"{path}: wires or invokes {symbol}");
    }
}
