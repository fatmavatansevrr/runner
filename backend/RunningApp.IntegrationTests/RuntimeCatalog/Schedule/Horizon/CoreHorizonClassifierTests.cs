using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Horizon;

public sealed class CoreHorizonClassifierTests
{
    private static readonly DateOnly Start = new(2026, 1, 1);

    private static CoreHorizonDecision ClassifyDays(int days) =>
        CoreHorizonClassifier.Classify(new(Start, Start.AddDays(days), 8, 12, 14));

    [Theory]
    [InlineData(7, (int)CoreHorizonMode.Unsupported)]
    [InlineData(8, (int)CoreHorizonMode.CompressedCore)]
    [InlineData(9, (int)CoreHorizonMode.CompressedCore)]
    [InlineData(10, (int)CoreHorizonMode.CompressedCore)]
    [InlineData(11, (int)CoreHorizonMode.CompressedCore)]
    [InlineData(12, (int)CoreHorizonMode.PreferredCore)]
    [InlineData(13, (int)CoreHorizonMode.ExtendedCore)]
    [InlineData(14, (int)CoreHorizonMode.ExtendedCore)]
    [InlineData(15, (int)CoreHorizonMode.PreparationRunwayPlusCore)]
    [InlineData(20, (int)CoreHorizonMode.PreparationRunwayPlusCore)]
    public void ExactWeekHorizons_ClassifyAgainstCatalogBounds(int weeks, int expectedMode)
    {
        var result = ClassifyDays(weeks * 7);

        Assert.Equal(weeks * 7, result.AvailableDays);
        Assert.Equal(weeks, result.AvailableFullWeeks);
        Assert.Equal(0, result.LeadingPartialDays);
        Assert.Equal((CoreHorizonMode)expectedMode, result.Mode);
        Assert.Equal((8, 12, 14), (result.MinimumCoreWeeks, result.PreferredCoreWeeks, result.MaximumCoreWeeks));
        Assert.Contains(CoreHorizonClassifier.Version, result.Rules);
    }

    [Theory]
    [InlineData(7, 6, (int)CoreHorizonMode.Unsupported)]
    [InlineData(8, 1, (int)CoreHorizonMode.CompressedCore)]
    [InlineData(11, 6, (int)CoreHorizonMode.CompressedCore)]
    [InlineData(12, 1, (int)CoreHorizonMode.ExtendedCore)]
    [InlineData(13, 6, (int)CoreHorizonMode.ExtendedCore)]
    [InlineData(14, 1, (int)CoreHorizonMode.PreparationRunwayPlusCore)]
    public void PartialDayHorizons_AreClassifiedByDays_NotCeilingWeeks(
        int fullWeeks, int partialDays, int expectedMode)
    {
        var result = ClassifyDays((fullWeeks * 7) + partialDays);

        Assert.Equal(fullWeeks, result.AvailableFullWeeks);
        Assert.Equal(partialDays, result.LeadingPartialDays);
        Assert.Equal((CoreHorizonMode)expectedMode, result.Mode);
    }

    [Fact]
    public void BelowMinimum_DoesNotInventReadinessOnlyRouting()
    {
        var result = ClassifyDays(7 * 7);

        Assert.Equal(CoreHorizonMode.Unsupported, result.Mode);
        Assert.Equal(CoreHorizonDecisionReason.BelowMinimumCore, result.Reason);
        Assert.NotEqual(CoreHorizonMode.ReadinessOnly, result.Mode);
    }

    [Fact]
    public void RaceBeforeStart_IsInvalidInput()
    {
        var result = CoreHorizonClassifier.Classify(new(Start, Start.AddDays(-1), 8, 12, 14));

        Assert.Equal(CoreHorizonMode.InvalidInput, result.Mode);
        Assert.Equal(CoreHorizonDecisionReason.InvalidDateRange, result.Reason);
        Assert.Equal(0, result.AvailableDays);
    }

    [Theory]
    [InlineData(0, 12, 14)]
    [InlineData(12, 8, 14)]
    [InlineData(8, 15, 14)]
    public void InvalidCoreBounds_FailClosed(int minimum, int preferred, int maximum)
    {
        var result = CoreHorizonClassifier.Classify(new(Start, Start.AddDays(84), minimum, preferred, maximum));

        Assert.Equal(CoreHorizonMode.InvalidInput, result.Mode);
        Assert.Equal(CoreHorizonDecisionReason.InvalidCoreBounds, result.Reason);
    }

    [Fact]
    public void Classifier_HasOnlyTheCanonicalPolicyCallSite()
    {
        var repo = RepoRoot();
        var application = Path.Combine(repo, "backend", "RunningApp.Application");
        var ownFile = Path.Combine(application, "RuntimeCatalog", "Schedule", "Horizon", "CoreHorizonClassifier.cs");
        var references = Directory.EnumerateFiles(application, "*.cs", SearchOption.AllDirectories)
            .Where(path => !string.Equals(path, ownFile, StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains(nameof(CoreHorizonClassifier), StringComparison.Ordinal))
            .ToArray();

        var reference = Assert.Single(references);
        Assert.Equal("RaceHorizonPolicy.cs", Path.GetFileName(reference));
        var source = File.ReadAllText(ownFile);
        Assert.DoesNotContain("RaceHorizonPolicy.Classify", source);
        Assert.DoesNotContain("PreparationRunwayPlanner", source);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
