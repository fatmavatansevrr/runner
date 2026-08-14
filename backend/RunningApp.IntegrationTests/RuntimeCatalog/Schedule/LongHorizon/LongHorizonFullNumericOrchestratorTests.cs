using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6A — production-level tests for
/// <see cref="LongHorizonFullNumericOrchestrator"/>, which completes the
/// Phase 4I.6 gap by invoking the REAL, unchanged Preparation Runway and
/// Core numeric/pace/calendar pipelines (via real runtime-condition
/// resolution, not a fabricated stand-in). Real file-system catalog only —
/// no database, no network.
/// </summary>
public sealed class LongHorizonFullNumericOrchestratorTests
{
    private static readonly DateOnly Anchor = new(2000, 1, 1);
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;
    private static readonly LongHorizonGeEntryBaselineInput TypicalBaseline = new(RecentWeeklyVolumeKm: 20, RecentLongestRunKm: 8, RecentRunsPerWeek: 3);
    // Bounded below the candidate's own peak-volume-band reachability ceiling for a
    // 12-week Core build at the fixed pilot target pace (45km was proven unreachable
    // by the real CatalogVolumeAndLongRunPlanner.ResolvePeak rule -- a legitimate
    // existing domain rejection, not a defect).
    private static readonly LongHorizonGeEntryBaselineInput HighBaseline = new(RecentWeeklyVolumeKm: 30, RecentLongestRunKm: 12, RecentRunsPerWeek: 5);

    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    private static LongHorizonCompositionDecision Decide(int availableFullWeeks, ReadinessProfile profile)
    {
        var raceDate = Anchor.AddDays(availableFullWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(Anchor, raceDate);
        return LongHorizonCompositionResolver.Resolve(coreHorizon, profile);
    }

    private static Task<LongHorizonExecutedSchedule> ExecuteAsync(
        int totalWeeks, ReadinessProfile profile, LongHorizonGeEntryBaselineInput? baseline = null)
    {
        var startDate = new DateOnly(2026, 8, 3); // a Monday
        var raceDate = startDate.AddDays(totalWeeks * 7);
        return LongHorizonFullNumericOrchestrator.ExecuteAsync(
            Decide(totalWeeks, profile), startDate, raceDate, baseline ?? TypicalBaseline,
            PreferredDays, LongRunDay, CatalogRoot(), Loader());
    }

    [Theory]
    [InlineData(21, ReadinessProfile.ConsistencyNeeded)]
    [InlineData(21, ReadinessProfile.CoreEntryReady)]
    [InlineData(24, ReadinessProfile.ConsistencyNeeded)]
    internal async Task RepresentativeHorizons_ProduceFullyNumericPacedDatedSchedule(int totalWeeks, ReadinessProfile profile)
    {
        var schedule = await ExecuteAsync(totalWeeks, profile);

        Assert.Equal(totalWeeks, schedule.Weeks.Count);
        Assert.True(schedule.GeNumericExecutionComplete);
        Assert.True(schedule.RunwayNumericExecutionComplete);
        Assert.True(schedule.CoreNumericExecutionComplete);

        foreach (var week in schedule.Weeks)
        {
            Assert.NotNull(week.TotalVolumeKm);
            Assert.NotNull(week.LongRunDistanceKm);
            Assert.Equal(4, week.OrderedSlots.Count);
            foreach (var slot in week.OrderedSlots)
            {
                Assert.False(string.IsNullOrWhiteSpace(slot.WorkoutKey));
                Assert.True(slot.WorkoutVersion > 0);
                Assert.NotNull(slot.AssignedDate);
                Assert.NotNull(slot.PlannedDistanceKm);
                Assert.True(slot.PlannedDistanceKm > 0);
            }
        }
    }

    [Fact]
    public async Task RunwaySegment_AllEightWeeksNumericPacedDated()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        var runwayWeeks = schedule.Weeks.Where(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway).ToList();
        Assert.Equal(8, runwayWeeks.Count);
        Assert.All(runwayWeeks, w => Assert.True(w.TotalVolumeKm > 0));
        Assert.All(runwayWeeks, w => Assert.True(w.LongRunDistanceKm > 0));
    }

    [Fact]
    public async Task CoreSegment_AllTwelveWeeksNumericPacedDated_FoundationBuildRaceSpecificTaperPreserved()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.CoreEntryReady);
        var coreWeeks = schedule.Weeks.Where(w => w.Structural.Segment == LongHorizonSegmentType.Core).ToList();
        Assert.Equal(12, coreWeeks.Count);
        Assert.Equal(3, coreWeeks.Count(w => w.Structural.CorePhase == "FOUNDATION"));
        Assert.Equal(4, coreWeeks.Count(w => w.Structural.CorePhase == "BUILD"));
        Assert.Equal(4, coreWeeks.Count(w => w.Structural.CorePhase == "RACE_SPECIFIC"));
        Assert.Equal(1, coreWeeks.Count(w => w.Structural.CorePhase == "TAPER"));
        Assert.All(coreWeeks, w => Assert.True(w.TotalVolumeKm > 0));
    }

    [Fact]
    public async Task GeToRunwayVolumeContinuity_WithinApprovedCaps()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        var lastGe = schedule.Weeks.Last(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance);
        var firstRunway = schedule.Weeks.First(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway);

        var maxAllowed = Math.Max(lastGe.TotalVolumeKm!.Value * 0.08, 2.5) + 0.01;
        Assert.True(firstRunway.TotalVolumeKm!.Value <= lastGe.TotalVolumeKm!.Value + maxAllowed,
            $"Runway Week 1 ({firstRunway.TotalVolumeKm}) exceeds GE exit ({lastGe.TotalVolumeKm}) by more than approved caps.");
    }

    [Fact]
    public async Task RunwayToCoreTransition_CoreWeekOneIsFoundation_NoDuplicateTransitionWeek()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        var lastRunway = schedule.Weeks.Last(w => w.Structural.Segment == LongHorizonSegmentType.PreparationRunway);
        var firstCore = schedule.Weeks.First(w => w.Structural.Segment == LongHorizonSegmentType.Core);

        Assert.Equal("PreSpecificTransition", lastRunway.Structural.RunwayBlock);
        Assert.Equal("FOUNDATION", firstCore.Structural.CorePhase);
        Assert.Equal(lastRunway.Structural.GlobalWeekNumber + 1, firstCore.Structural.GlobalWeekNumber);
    }

    [Fact]
    public async Task Determinism_RepeatedExecutionProducesIdenticalResult()
    {
        var first = await ExecuteAsync(24, ReadinessProfile.CoreEntryReady);
        var second = await ExecuteAsync(24, ReadinessProfile.CoreEntryReady);

        Assert.Equal(
            first.Weeks.Select(w => (w.TotalVolumeKm, w.LongRunDistanceKm, w.OrderedSlots.Select(s => (s.WorkoutKey, s.WorkoutVersion, s.AssignedDate, s.PlannedDistanceKm)))),
            second.Weeks.Select(w => (w.TotalVolumeKm, w.LongRunDistanceKm, w.OrderedSlots.Select(s => (s.WorkoutKey, s.WorkoutVersion, s.AssignedDate, s.PlannedDistanceKm)))));
    }

    [Fact]
    public async Task TwentyWeeks_RejectedByOrchestrator()
    {
        var startDate = new DateOnly(2026, 8, 3);
        var raceDate = startDate.AddDays(20 * 7);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonFullNumericOrchestrator.ExecuteAsync(
                Decide(20, ReadinessProfile.ConsistencyNeeded), startDate, raceDate, TypicalBaseline,
                PreferredDays, LongRunDay, CatalogRoot(), Loader()));
    }

    // ── Existing-pipeline compatibility gap (Phase 4I.6A's own primary finding) ──
    //
    // The existing Preparation Runway numeric materializer has no accepted rule
    // for "entry evidence exceeds the Core Week-1 boundary target" outside of
    // Taper (Taper is Core-internal, not a Runway concept). Because GE's approved
    // development-progression caps compound over as many as 32 weeks while Core's
    // own Week-1 target is independently computed from the SAME raw evidence over
    // a fixed 12-week peak-volume-band ceiling, GE's exit volume can legitimately
    // exceed that independently-computed Core boundary once GE is long enough --
    // and the existing Runway numeric engine fails closed (correctly: this is
    // real, existing, unmodified production validation, not a new rule this
    // phase invented) rather than silently producing an invalid downward ramp.
    // This is a genuine, disclosed "existing pipeline compatibility gap" -- see
    // the phase document's own non-implementation statement.
    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    public async Task ShortGeHorizons_FullNumericSucceeds_TypicalBaseline(int totalWeeks)
    {
        var schedule = await ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, TypicalBaseline);
        Assert.Equal(totalWeeks, schedule.Weeks.Count);
        Assert.All(schedule.Weeks, w => Assert.NotNull(w.TotalVolumeKm));
    }

    [Theory]
    [InlineData(28)]
    [InlineData(40)]
    [InlineData(52)]
    public async Task LongerGeHorizons_FailClosed_ExistingRunwayNoNonTaperReductionRule(int totalWeeks)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExecuteAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded, TypicalBaseline));
        Assert.Contains("no approved non-taper runway reduction rule exists", ex.Message);
    }
}
