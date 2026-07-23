using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog;

public sealed class Phase4G3B1TypedPhaseConstraintLoadingTests
{
    private const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    private const int CandidateVersion = 10;

    [Fact]
    public async Task JsonThroughRuntimeSummary_PreservesEveryPhaseConstraint()
    {
        var summary = await Loader(RealCatalogRoot()).LoadCandidateAsync(CandidateKey, CandidateVersion);

        Assert.Equal(
            new[]
            {
                new PlanCatalogPhaseAllocation("FOUNDATION", 2, 3, 4, 1, 1, false),
                new PlanCatalogPhaseAllocation("BUILD", 3, 4, 5, 2, 2, false),
                new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 2, 4, 4, 3, 3, false),
                new PlanCatalogPhaseAllocation("TAPER", 1, 1, 1, 4, 4, true),
            },
            summary.PhaseAllocations);
    }

    [Theory]
    [InlineData("minimumWeeks", -1, "negative")]
    [InlineData("preferredWeeks", 1, "minimumWeeks <= preferredWeeks <= maximumWeeks")]
    [InlineData("maximumWeeks", 1, "minimumWeeks <= preferredWeeks <= maximumWeeks")]
    [InlineData("compressionPriority", 0, "positive")]
    [InlineData("extensionPriority", 0, "positive")]
    public async Task InvalidConstraint_FailsClosed(string field, int value, string expectedMessage)
    {
        await WithMutatedTemplateAsync(template => template["phases"]![0]![field] = value, expectedMessage);
    }

    [Fact]
    public async Task MissingRequiredMetadata_FailsClosed()
    {
        await WithMutatedTemplateAsync(
            template => template["phases"]![0]!.AsObject().Remove("isCompressionProtected"),
            "isCompressionProtected");
    }

    [Fact]
    public async Task DuplicatePhaseDefinition_FailsClosed()
    {
        await WithMutatedTemplateAsync(
            template => template["phases"]![1]!["phaseKey"] = "FOUNDATION",
            "duplicate");
    }

    [Fact]
    public async Task MissingPilotPhase_FailsClosed()
    {
        await WithMutatedTemplateAsync(
            template => template["phases"]!.AsArray().RemoveAt(3),
            "missing required pilot phase");
    }

    private static async Task WithMutatedTemplateAsync(Action<JsonObject> mutate, string expectedMessage)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"phase-contract-{Guid.NewGuid()}");
        CopyDirectory(RealCatalogRoot(), tempRoot);
        try
        {
            var templatePath = Path.Combine(tempRoot, "templates", "ten-k-master.v6.json");
            var template = JsonNode.Parse(await File.ReadAllTextAsync(templatePath))!.AsObject();
            mutate(template);
            await File.WriteAllTextAsync(templatePath, template.ToJsonString());

            var ex = await Assert.ThrowsAsync<PlanCatalogLoadException>(
                () => Loader(tempRoot).LoadCandidateAsync(CandidateKey, CandidateVersion));
            Assert.Contains(expectedMessage, ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static PlanCatalogBundleLoader Loader(string root) =>
        new(Options.Create(new PlanCatalogOptions { CatalogRootPath = root }), NullLogger<PlanCatalogBundleLoader>.Instance);

    private static string RealCatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !(Directory.Exists(Path.Combine(directory.FullName, "backend")) && Directory.Exists(Path.Combine(directory.FullName, "plan-catalog"))))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
