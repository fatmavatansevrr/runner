using System.Text.RegularExpressions;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Horizon;

public sealed class RaceHorizonPolicyAuthorityTests
{
    private static readonly DateOnly Start = new(2026, 8, 3);

    [Theory]
    [InlineData(55, 7, 6, "Unsupported", RaceHorizonClassification.BelowMinimum)]
    [InlineData(56, 8, 0, "CompressedCore", RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(57, 8, 1, "CompressedCore", RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(83, 11, 6, "CompressedCore", RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(84, 12, 0, "PreferredCore", RaceHorizonClassification.ExactStandaloneCoreSupported)]
    [InlineData(85, 12, 1, "ExtendedCore", RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(98, 14, 0, "ExtendedCore", RaceHorizonClassification.StandaloneCoreSupported)]
    [InlineData(99, 14, 1, "PreparationRunwayPlusCore", RaceHorizonClassification.CompositionRequired)]
    public void Decide_IsTheSingleDayAccurateAuthority(
        int elapsedDays, int fullWeeks, int remainingDays,
        string mode, RaceHorizonClassification classification)
    {
        var decision = RaceHorizonPolicy.Decide(Start, Start.AddDays(elapsedDays));

        Assert.Equal(elapsedDays, decision.AvailableDays);
        Assert.Equal(fullWeeks, decision.AvailableFullWeeks);
        Assert.Equal(remainingDays, decision.LeadingPartialDays);
        Assert.Equal(mode, decision.Mode.ToString());
        Assert.Equal(classification, RaceHorizonPolicy.Classify(decision));
        Assert.Equal(fullWeeks, RaceHorizonPolicy.CalculateAvailableWeeks(Start, Start.AddDays(elapsedDays)));
    }

    [Fact]
    public void RaceHorizonPolicy_ContainsNoIndependentArithmeticOrCeiling()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "Common", "RaceHorizonPolicy.cs"));

        Assert.DoesNotContain("Math.Ceiling", source, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"DayNumber\s*-\s*.*DayNumber|/\s*7"), source);
        Assert.Contains("CoreHorizonClassifier.Classify", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ExactTwelve_RemainsPreferred_NotDynamicCompressedOrExtended()
    {
        var decision = RaceHorizonPolicy.Decide(Start, Start.AddDays(84));
        Assert.Equal(CoreHorizonMode.PreferredCore, decision.Mode);
        Assert.Equal(RaceHorizonClassification.ExactStandaloneCoreSupported, RaceHorizonPolicy.Classify(decision));
    }
}
