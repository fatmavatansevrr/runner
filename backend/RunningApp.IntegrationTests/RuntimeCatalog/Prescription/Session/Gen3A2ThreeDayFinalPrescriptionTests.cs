using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Session;

public sealed class Gen3A2ThreeDayFinalPrescriptionTests
{
    public static IEnumerable<object[]> EligibleCases()
    {
        yield return new object[] { 12, 20d, 8d, 3 };
        yield return new object[] { 12, null!, 8d, 3 };
        yield return new object[] { 12, 0d, 0d, 0 };
        yield return new object[] { 14, 20d, 8d, 3 };
    }

    [Theory]
    [MemberData(nameof(EligibleCases))]
    public async Task RealThreeDayCandidate_ReachesValidFinalPrescription(
        int weeks, double? recentWeekly, double? longest, int runs)
    {
        var candidate = await DynamicCoreSessionPrescriptionOrchestratorTests.RealThreeDayCandidateAsync();
        var result = await DynamicCoreSessionPrescriptionOrchestratorTests.BuildAsync(
            candidate, weeks, DynamicCoreSessionPrescriptionOrchestratorTests.PaceSourceCategory.RecentRace,
            recentWeekly, longest, runs);

        Assert.True(result.FinalPrescribedPlan.ValidationResult.IsValid,
            string.Join("; ", result.FinalPrescribedPlan.ValidationResult.Errors));
        Assert.Equal(weeks, result.FinalPrescribedPlan.Weeks.Count);
        Assert.All(result.FinalPrescribedPlan.Weeks, week =>
        {
            Assert.Equal(3, week.Sessions.Count);
            Assert.Single(week.Sessions, s => s.StructuralRole == "KEY_SESSION");
            Assert.Single(week.Sessions, s => s.StructuralRole == "EASY_SUPPORT");
            Assert.Single(week.Sessions, s => s.StructuralRole == "LONG_RUN");
            Assert.All(week.Sessions, session =>
            {
                Assert.False(string.IsNullOrWhiteSpace(session.WorkoutDefinitionKey));
                Assert.True(session.PlannedDistanceKm > 0);
                Assert.NotNull(session.Prescription);
                Assert.False(string.IsNullOrWhiteSpace(session.Prescription.EffortGuidance));
            });
            Assert.Equal(week.PlannedWeeklyVolumeKm, week.Sessions.Sum(s => s.PlannedDistanceKm), 3);
        });

        var taper = Assert.Single(result.FinalPrescribedPlan.Weeks, w => w.PhaseKey == "TAPER");
        Assert.True(taper.PlannedWeeklyVolumeKm >= 12d);
        Assert.True(taper.Sessions.Single(s => s.StructuralRole == "KEY_SESSION").PlannedDistanceKm >= 4d);
        Assert.True(taper.Sessions.Single(s => s.StructuralRole == "EASY_SUPPORT").PlannedDistanceKm >= 3d);
        Assert.True(taper.Sessions.Single(s => s.StructuralRole == "LONG_RUN").PlannedDistanceKm >= 5d);
        var sharpen = Assert.Single(taper.Sessions, s => s.ProgressionStageKey == "TAPER_SHARPEN");
        Assert.Contains(sharpen.Prescription.OrderedSegments, s => s.ComponentType == "CONTROLLED_SHARPENING");
    }

    [Fact]
    public async Task EightWeekExplicitZero_FailsTypedBeforeFinalOutput()
    {
        var candidate = await DynamicCoreSessionPrescriptionOrchestratorTests.RealThreeDayCandidateAsync();
        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() =>
            DynamicCoreSessionPrescriptionOrchestratorTests.BuildAsync(candidate, 8,
                DynamicCoreSessionPrescriptionOrchestratorTests.PaceSourceCategory.RecentRace, 0, 0, 0));
        Assert.IsType<ThreeDayCoreProductIneligibleException>(wrapper.InnerException);
    }

    [Fact]
    public async Task ExistingPayloadPersistenceShape_RepresentsThreeRolesWithoutSchemaChange()
    {
        var candidate = await DynamicCoreSessionPrescriptionOrchestratorTests.RealThreeDayCandidateAsync();
        var result = await DynamicCoreSessionPrescriptionOrchestratorTests.BuildAsync(candidate, 12,
            DynamicCoreSessionPrescriptionOrchestratorTests.PaceSourceCategory.RecentRace, 20, 8, 3);
        var start = new DateOnly(2026, 8, 3);
        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            new GeneratePreviewRequest
            {
                GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
                DaysPerWeek = 3, Unit = DistanceUnit.Km, StartDate = start, RaceDate = start.AddDays(84),
                PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Sun }, LongRunDay = Weekday.Sun,
                RecentWeeklyVolumeKm = 20, RecentLongestRunKm = 8, RecentRunsPerWeek = 3,
                TargetFinishTimeSeconds = 3000
            }, candidate, start, start, result.VolumeResult.VolumeAndLongRunPlan, result.FinalPrescribedPlan));

        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(3, materialized.Payload.DaysPerWeek);
        Assert.All(materialized.Payload.Weeks, week =>
        {
            Assert.Equal(3, week.Sessions.Count);
            Assert.Equal(new[] { "EASY_SUPPORT", "KEY_SESSION", "LONG_RUN" },
                week.Sessions.Select(s => s.Provenance.SourceLayoutSlotRole).OrderBy(x => x).ToArray());
            Assert.Equal(new[] { 1, 2, 3 }, week.Sessions.Select(s => s.SessionOrderInWeek).OrderBy(x => x).ToArray());
            Assert.All(week.Sessions, day =>
            {
                Assert.False(string.IsNullOrWhiteSpace(day.Provenance.SourceWorkoutKey));
                Assert.False(string.IsNullOrWhiteSpace(day.Provenance.SourceStageKey));
            });
        });
    }
}
