using System.Text.Json;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

public sealed class Phase4K8ADirectionDiagnosticsTests
{
    [Theory]
    [InlineData(PreparationRunwayAllocationProfile.ConsistencyNeeded)]
    [InlineData(PreparationRunwayAllocationProfile.CoreEntryReady)]
    internal void WeeklyBelowBuilds_EqualIsFlat_AboveFailsClosed(PreparationRunwayAllocationProfile profile)
    {
        var below = Materialize(profile, 18, 6, 20, 6.5);
        var equal = Materialize(profile, 20, 6.5, 20, 6.5);
        var above = Materialize(profile, 22, 7, 20, 6.5);

        Assert.True(below.IsSuccess, below.FailureReason);
        Assert.All(below.PrescribedWeeks!.Skip(1), week => Assert.True(week.NumericTrace.WeeklyChangeKm >= 0));
        Assert.True(equal.IsSuccess, equal.FailureReason);
        Assert.All(equal.PrescribedWeeks!, week => Assert.Equal(20, week.PlannedWeeklyVolumeKm));
        Assert.False(above.IsSuccess);
        Assert.Equal(PreparationRunwayNumericMaterializationFailureCode.RunwayProgressionInfeasible, above.FailureCode);
        Assert.Null(above.PrescribedWeeks);
    }

    [Theory]
    [InlineData(5.0, "BELOW")]
    [InlineData(6.5, "EQUAL")]
    [InlineData(9.0, "ABOVE_RAW_NORMALIZED")]
    public void LongRunDirection_IsIndependentAndExistingBandNormalizationPrecedesInterpolation(double rawLongRun, string expected)
    {
        var result = Materialize(PreparationRunwayAllocationProfile.ConsistencyNeeded, 20, rawLongRun, 20, 6.5);

        Assert.True(result.IsSuccess, result.FailureReason);
        var weeks = result.PrescribedWeeks!;
        Assert.All(weeks.Skip(1), week => Assert.True(week.NumericTrace.WeeklyChangeKm >= 0));
        Assert.All(weeks.Skip(1), week => Assert.True(
            week.PlannedLongRunDistanceKm + 0.001 >= weeks[week.StructuralWeek.RunwayWeekNumber - 2].PlannedLongRunDistanceKm));
        if (expected == "ABOVE_RAW_NORMALIZED")
            Assert.Equal(6.5, weeks[0].PlannedLongRunDistanceKm);
    }

    [Fact]
    public void WeeklyEqualButEffectiveLongRunStillAboveTarget_FailsClosedIndependently()
    {
        var result = Materialize(PreparationRunwayAllocationProfile.CoreEntryReady, 20, 8, 20, 5.5);
        Assert.False(result.IsSuccess);
        Assert.Equal(PreparationRunwayNumericMaterializationFailureCode.LongRunContinuityViolation, result.FailureCode);
        Assert.Null(result.PrescribedWeeks);
    }

    private static PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> Materialize(
        PreparationRunwayAllocationProfile profile, double startWeekly, double startLongRun,
        double targetWeekly, double targetLongRun) => PreparationRunwayNumericMaterializer.Materialize(
        PreparationRunwayNumericMaterializerTests.Request(profile, 8,
            PreparationRunwayNumericMaterializerTests.Evidence(
                PreparationRunwayLoadEvidenceState.Provided, startWeekly,
                PreparationRunwayLoadEvidenceState.Provided, startLongRun),
            PreparationRunwayNumericMaterializerTests.Target(targetWeekly, targetLongRun)));
}

public sealed class Phase4K8AFullDurationDiagnosticsTests
{
    public static IEnumerable<object[]> ProfilesAndDurations()
    {
        foreach (PreparationRunwayAllocationProfile profile in Enum.GetValues<PreparationRunwayAllocationProfile>())
            foreach (var duration in Enumerable.Range(3, 6))
                yield return [profile, duration];
    }

    [Theory]
    [MemberData(nameof(ProfilesAndDurations))]
    internal void FullAuthority_PreservesLocalWeeksAndOneTerminalTransition(
        PreparationRunwayAllocationProfile profile, int duration)
    {
        var result = Full(profile, duration);
        Assert.True(result.IsSuccess, result.FailureReason);
        var weeks = result.PrescribedWeeks!;
        Assert.Equal(Enumerable.Range(1, duration), weeks.Select(week => week.StructuralWeek.RunwayWeekNumber));
        Assert.Single(weeks, week => week.StructuralWeek.BlockType == PreparationRunwayBlockType.PreSpecificTransition);
        Assert.Equal(PreparationRunwayBlockType.PreSpecificTransition, weeks[^1].StructuralWeek.BlockType);
    }

    [Theory]
    [MemberData(nameof(ProfilesAndDurations))]
    internal void BoundedExposureSlices_AreExactReferencesFromOneFullImmutableAuthority(
        PreparationRunwayAllocationProfile profile, int duration)
    {
        var full = Full(profile, duration).PrescribedWeeks!;
        var ranges = SliceRanges(duration);
        foreach (var (start, count) in ranges)
        {
            var exposed = full.Skip(start).Take(count).ToArray();
            var authoritative = full.ToArray()[start..(start + count)];
            Assert.Equal(JsonSerializer.Serialize(authoritative), JsonSerializer.Serialize(exposed));
            Assert.Equal(Enumerable.Range(start + 1, count), exposed.Select(week => week.StructuralWeek.RunwayWeekNumber));
            if (start + count < duration)
                Assert.DoesNotContain(exposed, week => week.StructuralWeek.BlockType == PreparationRunwayBlockType.PreSpecificTransition);
        }
    }

    [Fact]
    public void IndependentSliceRestart_IsNotEquivalentAndIsRejectedAsAuthority()
    {
        var full = Full(PreparationRunwayAllocationProfile.ConsistencyNeeded, 8).PrescribedWeeks!;
        var restarted = PreparationRunwayNumericMaterializer.Materialize(
            PreparationRunwayNumericMaterializerTests.Request(
                PreparationRunwayAllocationProfile.ConsistencyNeeded, 4,
                PreparationRunwayNumericMaterializerTests.Evidence(
                    PreparationRunwayLoadEvidenceState.Provided, 20,
                    PreparationRunwayLoadEvidenceState.Provided, 5.5),
                PreparationRunwayNumericMaterializerTests.Target(24, 8)));
        Assert.False(restarted.IsSuccess);
        Assert.Equal(PreparationRunwayNumericMaterializationFailureCode.WeeklyChangeLimitExceeded, restarted.FailureCode);
        Assert.Null(restarted.PrescribedWeeks);
        Assert.Equal([20d, 21d, 22d, 23d], full.Skip(2).Take(4).Select(week => week.PlannedWeeklyVolumeKm));
    }

    private static PreparationRunwayNumericMaterializationResult<PreparationRunwayBlockType> Full(
        PreparationRunwayAllocationProfile profile, int duration)
    {
        var start = duration switch { 3 => 22.5, 4 => 21, 5 => 20, 6 => 19, 7 => 18.5, _ => 18 };
        return PreparationRunwayNumericMaterializer.Materialize(
            PreparationRunwayNumericMaterializerTests.Request(profile, duration,
                PreparationRunwayNumericMaterializerTests.Evidence(
                    PreparationRunwayLoadEvidenceState.Provided, start,
                    PreparationRunwayLoadEvidenceState.Missing, null),
                PreparationRunwayNumericMaterializerTests.Target(24, 8)));
    }

    private static IReadOnlyList<(int Start, int Count)> SliceRanges(int duration)
    {
        var ranges = new HashSet<(int, int)>
        {
            (0, 1),
            (0, Math.Min(2, duration)),
            (0, Math.Min(4, duration)),
            (Math.Max(0, (duration - Math.Min(4, duration)) / 2), Math.Min(4, duration)),
            (Math.Max(0, duration - Math.Min(4, duration)), Math.Min(4, duration)),
            (duration - 1, 1),
        };
        return ranges.OrderBy(range => range.Item1).ThenBy(range => range.Item2).ToArray();
    }
}

public sealed class Phase4K8ATargetLockRefreshDiagnosticsTests
{
    [Fact]
    public void OneLockCoversWholeRunway_AllSlicesReuseIt_OverlapRefreshFails()
    {
        var initialVersion = LongHorizonContextVersion.Initial(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var decisionId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var original = new LongHorizonLockedCoreWeekOneTarget
        {
            TargetWeeklyVolumeKm = 20,
            TargetLongRunKm = 6.5,
            Source = LongHorizonEvidenceAuthorityCatalog.CoreWeekOneRollingAuthority,
            AuthorityStatus = LongHorizonEvidenceAuthorityStatus.Authoritative,
            ContextVersion = initialVersion,
            LockedForActivatedRunwayWeekRange = (9, 16),
            CreatedByDecisionId = decisionId,
        };
        LongHorizonCoreTargetLockValidator.Validate(original);

        var slices = new[] { (9, 12), (13, 16) };
        Assert.All(slices, slice =>
        {
            Assert.True(slice.Item1 >= original.LockedForActivatedRunwayWeekRange.StartGlobalWeek);
            Assert.True(slice.Item2 <= original.LockedForActivatedRunwayWeekRange.EndGlobalWeek);
            Assert.Equal(decisionId, original.CreatedByDecisionId);
        });

        var overlapping = original with
        {
            ContextVersion = initialVersion.Next(Guid.Parse("10000000-0000-0000-0000-000000000002")),
            LockedForActivatedRunwayWeekRange = (13, 16),
            CreatedByDecisionId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
        };
        Assert.Throws<LongHorizonLockedTargetImmutabilityViolationException>(
            () => LongHorizonCoreTargetLockValidator.ValidateRefresh(original, overlapping));
    }

    [Fact]
    public void FutureCoreOnlyRefresh_IsNonOverlappingLaterAndLeavesOriginalUnchanged()
    {
        var first = Build((9, 16), 1, "10000000-0000-0000-0000-000000000001", "20000000-0000-0000-0000-000000000001");
        var future = Build((17, 20), 2, "10000000-0000-0000-0000-000000000002", "20000000-0000-0000-0000-000000000002");
        LongHorizonCoreTargetLockValidator.ValidateRefresh(first, future);
        Assert.Equal((9, 16), first.LockedForActivatedRunwayWeekRange);
        Assert.Equal(1, first.ContextVersion.Sequence);
    }

    private static LongHorizonLockedCoreWeekOneTarget Build(
        (int, int) range, int sequence, string versionId, string decisionId) => new()
    {
        TargetWeeklyVolumeKm = 20,
        TargetLongRunKm = 6.5,
        Source = LongHorizonEvidenceAuthorityCatalog.CoreWeekOneRollingAuthority,
        AuthorityStatus = LongHorizonEvidenceAuthorityStatus.Authoritative,
        ContextVersion = new LongHorizonContextVersion { VersionId = Guid.Parse(versionId), Sequence = sequence },
        LockedForActivatedRunwayWeekRange = range,
        CreatedByDecisionId = Guid.Parse(decisionId),
    };
}
