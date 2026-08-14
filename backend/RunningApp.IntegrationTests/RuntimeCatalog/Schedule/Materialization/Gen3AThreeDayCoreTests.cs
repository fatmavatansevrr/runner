using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class Gen3AThreeDayCoreTests
{
    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var root = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
        var loader = new PlanCatalogBundleLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = root }), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await loader.LoadCandidateAsync("TEN_K__3D__INTERMEDIATE", 1);
    }

    [Fact]
    public async Task Candidate_IsManifestComposition_WithOrderedThreeDayLayout()
    {
        var candidate = await CandidateAsync();
        Assert.Equal("TEN_K_MASTER", candidate.MasterTemplate.Key);
        Assert.Equal("INTERMEDIATE", candidate.Level);
        Assert.Equal(3, candidate.DaysPerWeek);
        Assert.Equal(new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" }, candidate.SlotRoles);
    }

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task DynamicAuthority_ProducesCanonicalThreeRoleWeeks(int weeks)
    {
        var candidate = await CandidateAsync();
        var orchestrator = new DynamicCoreWeekSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator());
        var result = orchestrator.Build(new DynamicCoreWeekSkeletonOrchestrationContext
        {
            Candidate = candidate, TargetWeekCount = weeks,
            StartDate = new DateOnly(2026, 8, 5), AsOfDate = new DateOnly(2026, 8, 5)
        });

        Assert.True(result.Validation.IsValid);
        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, result.PhaseAllocation.Phases.Select(p => p.PhaseKey));
        Assert.All(result.Skeleton.Weeks, week =>
        {
            Assert.Equal(3, week.SessionSlots.Count);
            Assert.Equal(1, week.SessionSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
            Assert.Equal(1, week.SessionSlots.Count(s => s.StructuralRole == "EASY_SUPPORT"));
            Assert.Equal(1, week.SessionSlots.Count(s => s.StructuralRole == "LONG_RUN"));
        });
    }

    public static IEnumerable<object[]> ValidCalendarPatterns()
    {
        yield return new object[] { new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Sunday }, DayOfWeek.Sunday };
        yield return new object[] { new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday }, DayOfWeek.Tuesday };
        yield return new object[] { new[] { DayOfWeek.Monday, DayOfWeek.Friday, DayOfWeek.Sunday }, DayOfWeek.Friday };
        yield return new object[] { new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Saturday }, DayOfWeek.Saturday };
    }

    [Theory]
    [MemberData(nameof(ValidCalendarPatterns))]
    public void Calendar_ThreeDayPatterns_PreserveResolvedCardinalityAndSeparation(DayOfWeek[] preferred, DayOfWeek longRunDay)
    {
        var start = new DateOnly(2026, 8, 3);
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(start,
            slotRoleOrder: new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" });
        var dated = new CatalogWeekSkeletonCalendarMaterializer().Materialize(
            CatalogCalendarAssignmentFixtures.BuildContext(skeleton, preferred, longRunDay));

        Assert.All(dated.Weeks, week =>
        {
            Assert.Equal(3, week.SessionSlots.Select(s => s.SessionDate).Distinct().Count());
            Assert.Equal(longRunDay, week.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN").SessionDayOfWeek);
            Assert.Single(week.SessionSlots, s => s.StructuralRole == "KEY_SESSION");
            Assert.Single(week.SessionSlots, s => s.StructuralRole == "EASY_SUPPORT");
            var key = week.SessionSlots.Single(s => s.StructuralRole == "KEY_SESSION").SessionDate;
            var longer = week.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN").SessionDate;
            Assert.True(Math.Abs(key.DayNumber - longer.DayNumber) >= 2);
        });
        for (var i = 1; i < dated.Weeks.Count; i++)
        {
            var previousLong = dated.Weeks[i - 1].SessionSlots.Single(s => s.StructuralRole == "LONG_RUN").SessionDate;
            var currentKey = dated.Weeks[i].SessionSlots.Single(s => s.StructuralRole == "KEY_SESSION").SessionDate;
            Assert.True(Math.Abs(currentKey.DayNumber - previousLong.DayNumber) >= 2);
        }
    }

    [Fact]
    public void Calendar_NoCompleteCrossWeekAssignment_FailsClosed()
    {
        var start = new DateOnly(2026, 8, 3);
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(start,
            slotRoleOrder: new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" });
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton,
            new[] { DayOfWeek.Monday, DayOfWeek.Saturday, DayOfWeek.Sunday }, DayOfWeek.Sunday);
        Assert.Throws<CatalogPreferredDayConfigurationUnsafeException>(() =>
            new CatalogWeekSkeletonCalendarMaterializer().Materialize(context));
    }
}
