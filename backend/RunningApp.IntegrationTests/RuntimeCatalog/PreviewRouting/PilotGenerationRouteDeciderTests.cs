using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — proves the route decision itself:
/// TEN_K/INTERMEDIATE/4D (Race, TenK, RunningRegularly, 4 days) is identified
/// as the catalog pilot route, and every other combination remains on the
/// existing legacy SQL route. Pure decision logic, no catalog/DB access.
/// </summary>
public sealed class PilotGenerationRouteDeciderTests
{
    private readonly PilotGenerationRouteDecider _decider = new();

    private static GeneratePreviewRequest PilotRequest() => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.RunningRegularly,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
    };

    [Fact]
    public void Decide_TenKIntermediate4Day_ReturnsCatalogWithPilotMatchReason()
    {
        var decision = _decider.Decide(PilotRequest());

        Assert.Equal(GenerationSource.Catalog, decision.Source);
        Assert.Equal("PILOT_TEN_K_INTERMEDIATE_4D_MATCH", decision.RouteReason);
    }

    [Theory]
    [InlineData(nameof(GeneratePreviewRequest.GoalType))]
    [InlineData(nameof(GeneratePreviewRequest.GoalDistance))]
    [InlineData(nameof(GeneratePreviewRequest.Level))]
    [InlineData(nameof(GeneratePreviewRequest.DaysPerWeek))]
    public void Decide_AnySingleCriterionMismatched_ReturnsLegacySql(string fieldToChange)
    {
        var request = PilotRequest();

        switch (fieldToChange)
        {
            case nameof(GeneratePreviewRequest.GoalType):
                request.GoalType = GoalType.Habit;
                break;
            case nameof(GeneratePreviewRequest.GoalDistance):
                request.GoalDistance = GoalDistance.FiveK;
                break;
            case nameof(GeneratePreviewRequest.Level):
                request.Level = RunningBackground.NewToRunning;
                break;
            case nameof(GeneratePreviewRequest.DaysPerWeek):
                request.DaysPerWeek = 3;
                break;
        }

        var decision = _decider.Decide(request);

        Assert.Equal(GenerationSource.LegacySql, decision.Source);
        Assert.Equal("NOT_PILOT_COMBINATION", decision.RouteReason);
    }

    [Fact]
    public void Decide_SeededHabit5KCombination_ReturnsLegacySql()
    {
        var decision = _decider.Decide(new GeneratePreviewRequest
        {
            GoalType = GoalType.Habit,
            GoalDistance = GoalDistance.FiveK,
            Level = RunningBackground.NewToRunning,
            DaysPerWeek = 3,
            Unit = DistanceUnit.Km,
        });

        Assert.Equal(GenerationSource.LegacySql, decision.Source);
    }
}
