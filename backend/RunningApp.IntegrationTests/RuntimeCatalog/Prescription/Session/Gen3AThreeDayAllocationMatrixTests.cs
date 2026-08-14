using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Session;

public sealed class Gen3AThreeDayAllocationMatrixTests
{
    public static IEnumerable<object[]> ApprovedMatrix()
    {
        yield return new object[] { 12d, 4d, 3d, 5d };
        yield return new object[] { 14d, 5d, 3.5d, 5.5d };
        yield return new object[] { 16d, 5.5d, 4d, 6.5d };
        yield return new object[] { 18d, 6.5d, 4.5d, 7d };
        yield return new object[] { 20d, 7d, 5d, 8d };
        yield return new object[] { 22d, 7.5d, 5.5d, 9d };
        yield return new object[] { 24d, 8.5d, 6d, 9.5d };
        yield return new object[] { 26d, 9d, 6.5d, 10.5d };
        yield return new object[] { 28d, 10d, 7d, 11d };
        yield return new object[] { 30d, 10.5d, 7.5d, 12d };
        yield return new object[] { 32d, 11d, 8d, 13d };
    }

    [Theory]
    [MemberData(nameof(ApprovedMatrix))]
    public void Allocate_MatchesApprovedMatrix_AndAllHardConstraints(double weekly, double key, double easy, double longer)
    {
        var first = Allocate(weekly);
        var repeat = Allocate(weekly);

        Assert.Equal(key, first.KeySessionDistanceKm);
        Assert.Equal(easy, first.FirstEasySupportDistanceKm);
        Assert.Equal(longer, first.LongRunDistanceKm);
        Assert.Equal(weekly, first.KeySessionDistanceKm + first.FirstEasySupportDistanceKm + first.LongRunDistanceKm);
        Assert.True(first.KeySessionDistanceKm >= 4d);
        Assert.True(first.FirstEasySupportDistanceKm >= 3d);
        Assert.True(first.LongRunDistanceKm >= 5d);
        Assert.True(first.LongRunDistanceKm / weekly <= 0.42d);
        Assert.All(new[] { first.KeySessionDistanceKm, first.FirstEasySupportDistanceKm, first.LongRunDistanceKm }, value => Assert.Equal(0d, value * 2d % 1d));
        Assert.Equal(first, repeat);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(11.5d)]
    [InlineData(11.75d)]
    public void Allocate_BelowFloorOrNonReconcilable_FailsClosed(double weekly) =>
        Assert.Throws<CatalogSessionPrescriptionInfeasibleException>(() => Allocate(weekly));

    [Theory]
    [InlineData(12d, 4.5d)] // LONG minimum
    [InlineData(12d, 5.5d)] // 42% hard cap
    [InlineData(20d, 8.25d)] // 0.5km granularity
    public void Allocate_InvalidResolvedLongRun_FailsClosedDeterministically(double weekly, double resolvedLong)
    {
        var first = Assert.Throws<CatalogSessionPrescriptionInfeasibleException>(() => Allocate(weekly, resolvedLong));
        var repeat = Assert.Throws<CatalogSessionPrescriptionInfeasibleException>(() => Allocate(weekly, resolvedLong));
        Assert.Equal(first.Message, repeat.Message);
    }

    [Fact]
    public void Allocate_EqualErrorTie_UsesStableStructuralOrder()
    {
        var values = Enumerable.Range(0, 10).Select(_ => Allocate(14d)).ToArray();
        Assert.All(values, value =>
        {
            Assert.Equal(5d, value.KeySessionDistanceKm);
            Assert.Equal(3.5d, value.FirstEasySupportDistanceKm);
            Assert.Equal(5.5d, value.LongRunDistanceKm);
        });
    }

    private static V1FourDayWeekAllocation Allocate(double weekly) =>
        V1ThreeDaySessionVolumeAllocationPolicy.Allocate(Week(weekly), LongRun(weekly), Sessions());

    private static V1FourDayWeekAllocation Allocate(double weekly, double resolvedLong) =>
        V1ThreeDaySessionVolumeAllocationPolicy.Allocate(Week(weekly), LongRun(weekly) with
        {
            PlannedLongRunDistanceKm = resolvedLong,
            LongRunShareOfWeeklyVolume = resolvedLong / weekly
        }, Sessions());

    private static CatalogWeeklyVolumeWeek Week(double volume) => new()
    {
        WeekNumber = 1, PhaseKey = "BUILD", PlannedWeeklyVolumeKm = volume, ChangeKm = 0,
        VolumeClassification = "STEADY", IsRecoveryOrDeloadWeek = false, IsTaperWeek = false,
        AnchorSource = WeeklyVolumeAnchorSource.RecentFourWeekAverage,
        CatalogBounds = new CatalogVolumeBounds(22, 32, "TEN_K_INTERMEDIATE", 1),
        AppliedClamp = CatalogVolumeClamp.None, DecisionReason = "test", SourceArtifactKey = "test",
        SourceArtifactVersion = 1, Provenance = "test"
    };

    private static CatalogLongRunWeek LongRun(double volume) => new()
    {
        WeekNumber = 1, PhaseKey = "BUILD", PlannedLongRunDistanceKm = Math.Max(5, Math.Round(volume * .4 * 2) / 2),
        PlannedWeeklyVolumeKm = volume, LongRunShareOfWeeklyVolume = .4,
        LongRunAnchorSource = LongRunAnchorSource.WeeklyVolumeDerived,
        RecentLongestRunState = PrescriptionInputState.NotProvided, CompatibilityClamp = CatalogVolumeClamp.None,
        CatalogBounds = new CatalogVolumeBounds(22, 32, "TEN_K_INTERMEDIATE", 1), ChangeFromPreviousWeekKm = 0,
        DecisionReason = "test", SourceArtifactKey = "test", SourceArtifactVersion = 1, Provenance = "test"
    };

    private static IReadOnlyList<BoundCatalogSession> Sessions() =>
        new[] { Session("KEY_SESSION"), Session("EASY_SUPPORT"), Session("LONG_RUN") };

    private static BoundCatalogSession Session(string role) => new()
    {
        WeekNumber = 1, Date = new DateOnly(2026, 8, 3), PhaseKey = "BUILD", StructuralRole = role,
        WorkoutDefinitionKey = "test", WorkoutDefinitionVersion = 1, BindingMode = CatalogWorkoutBindingMode.FixedDefault,
        BindingPolicyKey = "test", BindingPolicyVersion = 1, SourceArtifactKey = "test", SourceArtifactVersion = 1,
        ConditionOutcome = null, BindingReason = "test"
    };
}
