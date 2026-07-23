using RunningApp.Application.Common;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// Parity test between the backend's <see cref="CanonicalTargetFinishTimePolicy"/>
/// and the Flutter client's <c>AverageFinishTimePolicy</c>
/// (mobile/lib/core/models/average_finish_time_policy.dart). No shared
/// codegen artifact exists in this repo, so parity is enforced by literal
/// assertion on both sides — this test is the backend half.
/// </summary>
public sealed class CanonicalTargetFinishTimePolicyTests
{
    [Fact]
    public void FiveK_MatchesFlutterCanonicalValue() =>
        Assert.Equal(1680, CanonicalTargetFinishTimePolicy.GetCanonicalSeconds(GoalDistance.FiveK));

    [Fact]
    public void TenK_MatchesFlutterCanonicalValue() =>
        Assert.Equal(3480, CanonicalTargetFinishTimePolicy.GetCanonicalSeconds(GoalDistance.TenK));

    [Fact]
    public void HalfMarathon_MatchesFlutterCanonicalValue() =>
        Assert.Equal(7500, CanonicalTargetFinishTimePolicy.GetCanonicalSeconds(GoalDistance.HalfMarathon));

    [Fact]
    public void Marathon_MatchesFlutterCanonicalValue() =>
        Assert.Equal(15660, CanonicalTargetFinishTimePolicy.GetCanonicalSeconds(GoalDistance.Marathon));

    [Fact]
    public void Custom_HasNoCanonicalValue() =>
        Assert.Null(CanonicalTargetFinishTimePolicy.GetCanonicalSeconds(GoalDistance.Custom));
}
