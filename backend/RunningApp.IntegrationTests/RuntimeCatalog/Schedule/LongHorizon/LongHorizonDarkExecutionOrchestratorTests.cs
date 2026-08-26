using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6 — production-level tests for the GE numeric executor, the Core
/// workout-binding executor, and the joined
/// <see cref="LongHorizonDarkExecutionOrchestrator"/>. Uses the same real,
/// file-system-backed catalog loader pattern established in
/// <see cref="LongHorizonStructuralMaterializerTests"/> -- no database, no
/// network.
/// </summary>
public sealed class LongHorizonDarkExecutionOrchestratorTests
{
    private static readonly DateOnly Anchor = new(2000, 1, 1);
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

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

    private static readonly LongHorizonGeEntryBaselineInput TypicalBaseline = new(RecentWeeklyVolumeKm: 20, RecentLongestRunKm: 8, RecentRunsPerWeek: 3);
    private static readonly LongHorizonGeEntryBaselineInput LowBaseline = new(RecentWeeklyVolumeKm: 8, RecentLongestRunKm: 3, RecentRunsPerWeek: 2);
    private static readonly LongHorizonGeEntryBaselineInput HighBaseline = new(RecentWeeklyVolumeKm: 45, RecentLongestRunKm: 16, RecentRunsPerWeek: 5);

    private static Task<LongHorizonExecutedSchedule> ExecuteAsync(
        int totalWeeks, ReadinessProfile profile, LongHorizonGeEntryBaselineInput? baseline = null) =>
        LongHorizonDarkExecutionOrchestrator.ExecuteAsync(
            Decide(totalWeeks, profile), new DateOnly(2026, 8, 3) /* a Monday */, baseline ?? TypicalBaseline,
            PreferredDays, LongRunDay, CatalogRoot(), Loader());

    // ── GE numeric executor unit tests ───────────────────────────────────

    [Fact]
    public void MissingBaseline_ThrowsInsteadOfInventingADefault()
    {
        // Phase 10K-FREQ.6D.14: missing readiness now fails closed as the typed
        // LongHorizonGeMissingReadinessProductIneligibleException (still an
        // InvalidOperationException, per FREQ.6D.12's approved PRODUCT_INELIGIBLE
        // decision) instead of a generic, untyped one.
        var descriptors = LongHorizonGeStructuralSelector.Select(4, ReadinessProfile.ConsistencyNeeded);
        var ex = Assert.Throws<LongHorizonGeMissingReadinessProductIneligibleException>(() =>
            LongHorizonGeNumericExecutor.Execute(descriptors, new LongHorizonGeEntryBaselineInput(null, null, null)));
        Assert.Equal("LONG_HORIZON_GE_MISSING_READINESS_PRODUCT_INELIGIBLE", ex.Code);
    }

    [Fact]
    public void ExplicitZeroBaseline_ThrowsTypedProductIneligible()
    {
        var descriptors = LongHorizonGeStructuralSelector.Select(4, ReadinessProfile.ConsistencyNeeded);
        var ex = Assert.Throws<LongHorizonGeExplicitZeroReadinessProductIneligibleException>(() =>
            LongHorizonGeNumericExecutor.Execute(descriptors, new LongHorizonGeEntryBaselineInput(0, null, null)));
        Assert.Equal("LONG_HORIZON_GE_EXPLICIT_ZERO_READINESS_PRODUCT_INELIGIBLE", ex.Code);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(45)]
    public void FirstWeek_EqualsEntryBaselineUnprogressed(double baselineKm)
    {
        var descriptors = LongHorizonGeStructuralSelector.Select(8, ReadinessProfile.ConsistencyNeeded);
        var result = LongHorizonGeNumericExecutor.Execute(descriptors, new LongHorizonGeEntryBaselineInput(baselineKm, null, null));
        Assert.Equal(baselineKm, result[0].TotalVolumeKm);
    }

    [Fact]
    public void LowBaseline_InfeasibleForFourViableSessions_FailsClosed()
    {
        // 8km/week with no recent-longest-run anchor: the 33% long-run share (2.5km rounded)
        // leaves a 5.5km residual, below the existing 6km (3+1.5+1.5) V1 session-minimum floor.
        var descriptors = LongHorizonGeStructuralSelector.Select(8, ReadinessProfile.ConsistencyNeeded);
        Assert.Throws<RunningApp.Application.RuntimeCatalog.Prescription.Session.CatalogSessionPrescriptionInfeasibleException>(() =>
            LongHorizonGeNumericExecutor.Execute(descriptors, new LongHorizonGeEntryBaselineInput(8, null, null)));
    }

    [Fact]
    public void DevelopmentProgression_NeverExceedsPreferredRatioOrAbsoluteCap()
    {
        var descriptors = LongHorizonGeStructuralSelector.Select(8, ReadinessProfile.ConsistencyNeeded);
        var result = LongHorizonGeNumericExecutor.Execute(descriptors, HighBaseline);

        for (var i = 1; i < result.Count; i++)
        {
            if (descriptors[i].IsRecoveryWeek || descriptors[i - 1].IsRecoveryWeek) continue;
            var prev = result[i - 1].TotalVolumeKm;
            var increase = result[i].TotalVolumeKm - prev;
            Assert.True(increase <= prev * 0.08 + 0.01, $"Week {i + 1}: increase {increase}km exceeds the 8% hard cap.");
            Assert.True(increase <= 2.5 + 0.01, $"Week {i + 1}: increase {increase}km exceeds the 2.5km absolute cap.");
        }
    }

    [Fact]
    public void Recovery_Applies85PercentAndNeverCreatesANewPeak()
    {
        var descriptors = LongHorizonGeStructuralSelector.Select(8, ReadinessProfile.ConsistencyNeeded);
        var result = LongHorizonGeNumericExecutor.Execute(descriptors, TypicalBaseline);

        var recoveryIndex = descriptors.ToList().FindIndex(d => d.IsRecoveryWeek);
        var priorPeak = result[recoveryIndex - 1].TotalVolumeKm;
        var recoveryVolume = result[recoveryIndex].TotalVolumeKm;

        Assert.True(recoveryVolume < priorPeak);
        Assert.True(priorPeak - recoveryVolume >= 0.5 - 0.001);
        Assert.Equal(Math.Round(priorPeak * 0.85 / 0.5, MidpointRounding.AwayFromZero) * 0.5, recoveryVolume, 3);
    }

    [Fact]
    public void PostRecovery_ReturnsTowardPriorPeak_NotFromRecoveryValue()
    {
        // GE12 = 3 mesocycles; mesocycle 2's Development1 (week 5) must progress from
        // mesocycle 1's peak (week 3), never from week 4's (recovery) reduced value.
        var descriptors = LongHorizonGeStructuralSelector.Select(12, ReadinessProfile.ConsistencyNeeded);
        var result = LongHorizonGeNumericExecutor.Execute(descriptors, TypicalBaseline);

        var mesocycle1Peak = result[2].TotalVolumeKm; // week 3 (Development3 of mesocycle 1)
        var recoveryVolume = result[3].TotalVolumeKm; // week 4 (recovery)
        var mesocycle2Development1 = result[4].TotalVolumeKm; // week 5

        Assert.True(mesocycle2Development1 > recoveryVolume);
        // The post-recovery development week's cap is applied relative to the prior peak, not
        // the recovery week -- so it must fall within the prior-peak-relative progression cap.
        Assert.True(mesocycle2Development1 <= Math.Round(mesocycle1Peak * 1.08 / 0.5, MidpointRounding.AwayFromZero) * 0.5 + 0.001);
    }

    [Fact]
    public void EightRecoveryWeeksAtGe32_AllValidNoSawtoothDrift()
    {
        var descriptors = LongHorizonGeStructuralSelector.Select(32, ReadinessProfile.CoreEntryReady);
        var result = LongHorizonGeNumericExecutor.Execute(descriptors, TypicalBaseline);

        var recoveryIndices = descriptors.Select((d, i) => (d, i)).Where(x => x.d.IsRecoveryWeek).Select(x => x.i).ToList();
        Assert.Equal(8, recoveryIndices.Count);
        foreach (var idx in recoveryIndices)
        {
            Assert.True(result[idx].TotalVolumeKm < result[idx - 1].TotalVolumeKm);
        }
        Assert.All(result, r => Assert.True(r.TotalVolumeKm > 0));
    }

    [Fact]
    public void WeeklyDistribution_SlotsAlwaysSumToWeeklyTotal()
    {
        var descriptors = LongHorizonGeStructuralSelector.Select(20, ReadinessProfile.ConsistencyNeeded);
        var result = LongHorizonGeNumericExecutor.Execute(descriptors, TypicalBaseline);

        foreach (var week in result)
        {
            var sum = week.KeySessionDistanceKm + week.FirstEasySupportDistanceKm + week.SecondEasySupportDistanceKm + week.LongRunDistanceKm;
            Assert.True(Math.Abs(sum - week.TotalVolumeKm) < 0.01, $"Week {week.WeekIndex}: {sum} != {week.TotalVolumeKm}");
        }
    }

    [Fact]
    public void Determinism_RepeatedExecutionProducesIdenticalNumericSequence()
    {
        var descriptors = LongHorizonGeStructuralSelector.Select(20, ReadinessProfile.CoreEntryReady);
        var first = LongHorizonGeNumericExecutor.Execute(descriptors, TypicalBaseline);
        var second = LongHorizonGeNumericExecutor.Execute(descriptors, TypicalBaseline);
        Assert.Equal(first, second);
    }

    // ── Orchestrator end-to-end tests ────────────────────────────────────

    [Theory]
    [InlineData(21, ReadinessProfile.ConsistencyNeeded)]
    [InlineData(21, ReadinessProfile.CoreEntryReady)]
    [InlineData(24, ReadinessProfile.ConsistencyNeeded)]
    [InlineData(32, ReadinessProfile.CoreEntryReady)]
    internal async Task RepresentativeHorizons_ProduceAFullyValidExecutedSchedule(int totalWeeks, ReadinessProfile profile)
    {
        var schedule = await ExecuteAsync(totalWeeks, profile);

        Assert.Equal(totalWeeks, schedule.Weeks.Count);
        Assert.True(schedule.GeNumericExecutionComplete);
        Assert.False(schedule.RunwayNumericExecutionComplete);
        Assert.False(schedule.CoreNumericExecutionComplete);

        var validation = LongHorizonExecutionValidator.Validate(schedule);
        Assert.True(validation.IsValid, string.Join("; ", validation.Findings));
    }

    [Fact]
    public async Task EveryGeCoreRunwaySlot_HasNonNullWorkoutIdentity()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        foreach (var week in schedule.Weeks)
        {
            foreach (var slot in week.OrderedSlots)
            {
                Assert.False(string.IsNullOrWhiteSpace(slot.WorkoutKey));
                Assert.True(slot.WorkoutVersion > 0);
            }
        }
    }

    [Fact]
    public async Task CoreWeeks_MatchExistingStandaloneWorkoutSelectionPattern()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        var coreWeeks = schedule.Weeks.Where(w => w.Structural.Segment == LongHorizonSegmentType.Core).ToList();

        Assert.Equal(12, coreWeeks.Count);
        var foundationWeek1 = coreWeeks.First(w => w.Structural.CorePhase == "FOUNDATION");
        var keySession = foundationWeek1.OrderedSlots.Single(s => s.Structural.StructuralRole == "KEY_SESSION");
        Assert.Equal("EASY_STANDARD", keySession.WorkoutKey); // matches CatalogWorkoutBinderTests.KeySession_BindsToStageCandidate

        var easySupport = coreWeeks.SelectMany(w => w.OrderedSlots).Where(s => s.Structural.StructuralRole == "EASY_SUPPORT").ToList();
        Assert.All(easySupport, s => Assert.Equal("EASY_STANDARD", s.WorkoutKey));

        var longRuns = coreWeeks.SelectMany(w => w.OrderedSlots).Where(s => s.Structural.StructuralRole == "LONG_RUN").ToList();
        Assert.Equal(12, longRuns.Count);
        Assert.All(longRuns, s => Assert.Equal("LONG_RUN_STANDARD", s.WorkoutKey));
    }

    [Fact]
    public async Task EveryWeek_HasNonNullAssignedDatesNoDuplicates()
    {
        var schedule = await ExecuteAsync(28, ReadinessProfile.CoreEntryReady);
        var allDates = new List<DateOnly>();
        foreach (var week in schedule.Weeks)
        {
            foreach (var slot in week.OrderedSlots)
            {
                Assert.NotNull(slot.AssignedDate);
                Assert.NotNull(slot.AssignedWeekday);
                allDates.Add(slot.AssignedDate!.Value);
            }
        }
        Assert.Equal(allDates.Count, allDates.Distinct().Count());
    }

    [Fact]
    public async Task LongRunSlots_AlwaysAssignedToLongRunDayPreference()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        var longRunSlots = schedule.Weeks.SelectMany(w => w.OrderedSlots).Where(s => s.IsLongRun);
        Assert.All(longRunSlots, s => Assert.Equal(LongRunDay, s.AssignedWeekday));
    }

    [Theory]
    [InlineData(ReadinessProfile.ConsistencyNeeded)]
    [InlineData(ReadinessProfile.CoreEntryReady)]
    internal async Task FiftyTwoWeekMaximum_FullyBoundNumericGeDatedThroughout(ReadinessProfile profile)
    {
        var schedule = await ExecuteAsync(52, profile, HighBaseline);

        Assert.Equal(52, schedule.Weeks.Count);
        Assert.Equal(208, schedule.Weeks.Sum(w => w.OrderedSlots.Count));
        var geWeeks = schedule.Weeks.Where(w => w.Structural.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();
        Assert.Equal(32, geWeeks.Count);
        Assert.Equal(8, geWeeks.Count(w => w.Structural.IsRecoveryWeek == true));
        Assert.All(geWeeks, w => Assert.NotNull(w.TotalVolumeKm));

        var validation = LongHorizonExecutionValidator.Validate(schedule);
        Assert.True(validation.IsValid, string.Join("; ", validation.Findings));
    }

    [Fact]
    public async Task Determinism_RepeatedOrchestratorExecutionProducesIdenticalResult()
    {
        var first = await ExecuteAsync(25, ReadinessProfile.CoreEntryReady);
        var second = await ExecuteAsync(25, ReadinessProfile.CoreEntryReady);

        Assert.Equal(
            first.Weeks.Select(w => (w.TotalVolumeKm, w.LongRunDistanceKm, w.OrderedSlots.Select(s => (s.WorkoutKey, s.WorkoutVersion, s.AssignedDate, s.PlannedDistanceKm)))),
            second.Weeks.Select(w => (w.TotalVolumeKm, w.LongRunDistanceKm, w.OrderedSlots.Select(s => (s.WorkoutKey, s.WorkoutVersion, s.AssignedDate, s.PlannedDistanceKm)))));
    }

    [Fact]
    public async Task TwentyWeeks_RejectedByOrchestrator()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await LongHorizonDarkExecutionOrchestrator.ExecuteAsync(
                Decide(20, ReadinessProfile.ConsistencyNeeded), new DateOnly(2026, 8, 3), TypicalBaseline,
                PreferredDays, LongRunDay, CatalogRoot(), Loader()));
    }

    // ── Validator direct tests ───────────────────────────────────────────

    [Fact]
    public async Task Validator_RunwayOrCoreNumericPresent_Fails()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        var broken = schedule with { RunwayNumericExecutionComplete = true };
        Assert.False(LongHorizonExecutionValidator.Validate(broken).IsValid);
    }

    [Fact]
    public async Task Validator_MissingWorkoutKey_Fails()
    {
        var schedule = await ExecuteAsync(24, ReadinessProfile.ConsistencyNeeded);
        var weeks = schedule.Weeks.ToList();
        var slots = weeks[0].OrderedSlots.ToList();
        slots[0] = slots[0] with { WorkoutKey = "" };
        weeks[0] = weeks[0] with { OrderedSlots = slots };
        var broken = schedule with { Weeks = weeks };
        Assert.False(LongHorizonExecutionValidator.Validate(broken).IsValid);
    }
}
