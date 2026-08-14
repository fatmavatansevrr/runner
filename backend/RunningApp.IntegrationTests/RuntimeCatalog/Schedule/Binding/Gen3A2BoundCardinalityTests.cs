using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Volume;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Binding;

public sealed class Gen3A2BoundCardinalityTests
{
    [Fact]
    public async Task Validator_DerivesThreeAndFourDayCardinality_FromSourceSkeleton()
    {
        var threeCandidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealThreeDayCandidateAsync();
        var three = await DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(threeCandidate, 12,
            DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.ExplicitZeroEvidence);
        Assert.True(new BoundCatalogPlanValidator().Validate(
            three.BindingResult.BoundPlan, three.BindingResult.DatedSkeleton).IsValid);

        AssertInvalidAfterMutatingFirstWeek(three.BindingResult, sessions =>
            sessions.Where(s => s.StructuralRole != "EASY_SUPPORT").ToArray());
        AssertInvalidAfterMutatingFirstWeek(three.BindingResult, sessions =>
            sessions.Append(sessions.Single(s => s.StructuralRole == "EASY_SUPPORT")).ToArray());

        var fourCandidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealCandidateAsync();
        var four = await DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(fourCandidate, 12,
            DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.CurrentPilotProfile);
        Assert.True(new BoundCatalogPlanValidator().Validate(
            four.BindingResult.BoundPlan, four.BindingResult.DatedSkeleton).IsValid);

        AssertInvalidAfterMutatingFirstWeek(four.BindingResult, sessions =>
            sessions.Where((s, i) => s.StructuralRole != "EASY_SUPPORT" ||
                                     i == sessions.ToList().FindIndex(x => x.StructuralRole == "EASY_SUPPORT")).ToArray());
        AssertInvalidAfterMutatingFirstWeek(four.BindingResult, sessions =>
            sessions.Append(sessions.Single(s => s.StructuralRole == "KEY_SESSION")).ToArray());
    }

    private static void AssertInvalidAfterMutatingFirstWeek(
        DynamicCoreWorkoutBindingResult result,
        Func<IReadOnlyList<BoundCatalogSession>, IReadOnlyList<BoundCatalogSession>> mutate)
    {
        var original = result.BoundPlan;
        var first = original.Weeks.OrderBy(w => w.WeekNumber).First();
        var changed = new BoundCatalogWeek
        {
            WeekNumber = first.WeekNumber, PhaseKey = first.PhaseKey, Sessions = mutate(first.Sessions)
        };
        var plan = new BoundCatalogPlan
        {
            CandidateKey = original.CandidateKey, CandidateVersion = original.CandidateVersion,
            BinderVersion = original.BinderVersion, Trace = original.Trace,
            Weeks = original.Weeks.Select(w => w.WeekNumber == first.WeekNumber ? changed : w).ToArray()
        };
        Assert.False(new BoundCatalogPlanValidator().Validate(plan, result.DatedSkeleton).IsValid);
    }
}
