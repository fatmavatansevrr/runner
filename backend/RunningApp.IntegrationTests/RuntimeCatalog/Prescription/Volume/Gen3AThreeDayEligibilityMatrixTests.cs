using Xunit;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Volume;

public sealed class Gen3AThreeDayEligibilityMatrixTests
{
    public static IEnumerable<object[]> ExplicitZeroMatrix()
    {
        yield return new object[] { 8, 17.5d, 9.5d, false };
        yield return new object[] { 9, 18.5d, 10d, false };
        yield return new object[] { 10, 19.5d, 10.5d, false };
        yield return new object[] { 11, 21d, 11d, false };
        yield return new object[] { 12, 22.5d, 12d, true };
        yield return new object[] { 13, 24d, 12.5d, true };
        yield return new object[] { 14, 25.5d, 13.5d, true };
    }

    [Theory]
    [MemberData(nameof(ExplicitZeroMatrix))]
    public async Task ExplicitZero_UsesApprovedProjectionAndTypedEligibility(int weeks, double preTaper, double taper, bool eligible)
    {
        var candidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealThreeDayCandidateAsync();
        if (!eligible)
        {
            var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() =>
                DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(candidate, weeks, DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.ExplicitZeroEvidence));
            var typed = Assert.IsType<ThreeDayCoreProductIneligibleException>(wrapper.InnerException);
            Assert.Equal(ThreeDayCoreProductIneligibleException.Reason, typed.Code);
            Assert.Contains($"{taper:0.##}km", typed.Message);
            return;
        }

        var result = await DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(candidate, weeks, DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.ExplicitZeroEvidence);
        var rows = result.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks;
        Assert.Equal(preTaper, rows[^2].PlannedWeeklyVolumeKm);
        Assert.Equal(taper, rows[^1].PlannedWeeklyVolumeKm);
    }

    [Theory]
    [InlineData(8, 23.5d, 12.5d)]
    [InlineData(10, 27d, 14.5d)]
    public async Task MissingReadiness_PassesTaperGate_WithApprovedProjection(int weeks, double preTaper, double taper)
    {
        var candidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealThreeDayCandidateAsync();
        var result = await DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(candidate, weeks, DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.RecentVolumeMissing);
        var rows = result.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks;
        Assert.Equal(preTaper, rows[^2].PlannedWeeklyVolumeKm);
        Assert.Equal(taper, rows[^1].PlannedWeeklyVolumeKm);
    }
}
