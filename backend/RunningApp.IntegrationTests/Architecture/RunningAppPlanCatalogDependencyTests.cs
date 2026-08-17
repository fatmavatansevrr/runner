using System.Reflection;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;

namespace RunningApp.IntegrationTests.Architecture;

/// <summary>
/// Phase 10K-FREQ.6D.3D — proves the frozen dependency direction between RunningApp.Application and
/// the PlanCatalog assemblies: RunningApp.Application may reference PlanCatalog.Contracts (the
/// immutable Process A→B execution-value boundary), and must never reference PlanCatalog.Core
/// (authoring authority) or PlanCatalog.Infrastructure (Core→Contracts projection authority).
///
/// Reflection-based, matching this repository's existing convention
/// (plan-catalog/tests/PlanCatalog.Tests/Architecture/PublishedBoundaryTests.cs and
/// ProjectDependencyTests.cs both use reflection/XML parsing, not a third-party architecture-test
/// library — no NetArchTest package exists anywhere in this repository).
/// </summary>
public sealed class RunningAppPlanCatalogDependencyTests
{
    private static Assembly RunningAppApplicationAssembly => typeof(ExecutionPrescriptionIndex).Assembly;

    [Fact]
    public void RunningAppApplication_References_PlanCatalogContracts()
    {
        var referenced = RunningAppApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();

        Assert.Contains("PlanCatalog.Contracts", referenced);
    }

    [Fact]
    public void RunningAppApplication_DoesNotReference_PlanCatalogCore()
    {
        var referenced = RunningAppApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();

        Assert.DoesNotContain("PlanCatalog.Core", referenced);
    }

    [Fact]
    public void RunningAppApplication_DoesNotReference_PlanCatalogInfrastructure()
    {
        var referenced = RunningAppApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();

        Assert.DoesNotContain("PlanCatalog.Infrastructure", referenced);
    }

    [Fact]
    public void RunningAppApplicationCsproj_HasNoTransitiveCoreOrInfrastructureProjectReference()
    {
        var csprojPath = FindRunningAppApplicationCsproj();
        var content = File.ReadAllText(csprojPath);

        Assert.DoesNotContain("PlanCatalog.Core.csproj", content);
        Assert.DoesNotContain("PlanCatalog.Infrastructure.csproj", content);
        Assert.Contains("PlanCatalog.Contracts.csproj", content);
    }

    private static string FindRunningAppApplicationCsproj()
    {
        // Matches the existing RepoRoot() convention in PlanCatalogDeploymentPackagingTests.cs:
        // walk up from the test binary's output directory until the "backend" solution folder
        // (containing RunningApp.sln) is found.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RunningApp.sln")))
        {
            directory = directory.Parent;
        }

        var backendRoot = directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the backend solution root (RunningApp.sln) from the test base directory.");

        var csprojPath = Path.Combine(backendRoot, "RunningApp.Application", "RunningApp.Application.csproj");
        if (!File.Exists(csprojPath))
        {
            throw new InvalidOperationException($"Expected to find RunningApp.Application.csproj at '{csprojPath}'.");
        }

        return csprojPath;
    }
}
