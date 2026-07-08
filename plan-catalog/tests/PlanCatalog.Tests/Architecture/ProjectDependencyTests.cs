using System.Xml.Linq;
using Xunit;

namespace PlanCatalog.Tests.Architecture;

public sealed class ProjectDependencyTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found above test output directory.");
    }

    private static IReadOnlyList<string> ProjectReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        return doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")!.Value.Replace('\\', '/'))
            .ToList();
    }

    [Fact]
    public void Contracts_HasNoProjectReferences()
    {
        var root = RepoRoot();
        var refs = ProjectReferences(Path.Combine(root, "src", "PlanCatalog.Contracts", "PlanCatalog.Contracts.csproj"));
        Assert.Empty(refs);
    }

    [Fact]
    public void Core_OnlyReferencesContracts()
    {
        var root = RepoRoot();
        var refs = ProjectReferences(Path.Combine(root, "src", "PlanCatalog.Core", "PlanCatalog.Core.csproj"));
        Assert.Single(refs);
        Assert.Contains(refs, r => r.Contains("PlanCatalog.Contracts", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_OnlyReferencesCore()
    {
        var root = RepoRoot();
        var refs = ProjectReferences(Path.Combine(root, "src", "PlanCatalog.Infrastructure", "PlanCatalog.Infrastructure.csproj"));
        Assert.Single(refs);
        Assert.Contains(refs, r => r.Contains("PlanCatalog.Core", StringComparison.Ordinal));
    }

    [Fact]
    public void Cli_OnlyReferencesInfrastructure()
    {
        var root = RepoRoot();
        var refs = ProjectReferences(Path.Combine(root, "src", "PlanCatalog.Cli", "PlanCatalog.Cli.csproj"));
        Assert.Single(refs);
        Assert.Contains(refs, r => r.Contains("PlanCatalog.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void NoPlanCatalogProject_ReferencesBackend()
    {
        var root = RepoRoot();
        var csprojFiles = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories);

        foreach (var csproj in csprojFiles)
        {
            var refs = ProjectReferences(csproj);
            Assert.All(refs, r => Assert.DoesNotContain("RunningApp", r, StringComparison.OrdinalIgnoreCase));
            Assert.All(refs, r => Assert.DoesNotContain("/backend/", r, StringComparison.OrdinalIgnoreCase));
        }
    }
}
