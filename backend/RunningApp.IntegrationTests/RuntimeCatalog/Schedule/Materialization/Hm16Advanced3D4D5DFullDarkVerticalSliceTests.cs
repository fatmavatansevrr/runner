using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit.Abstractions;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase HM.16 — the same class of test harness as <see cref="Hm8Intermediate3DFullDarkVerticalSliceTests"/>/
/// <see cref="Hm11Intermediate5DFullDarkVerticalSliceTests"/>/<see cref="Hm14Beginner3D4DFullDarkVerticalSliceTests"/>,
/// exercising the real, unmodified production pipeline against the three new dark-only
/// HALF_MARATHON__3D__ADVANCED / HALF_MARATHON__4D__ADVANCED / HALF_MARATHON__5D__ADVANCED
/// candidates, implementing HM.15's frozen numeric and structural authority
/// (PHASE_HM_15_ADVANCED_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md). 3D/4D are single-KEY-lane
/// (structurally identical to Intermediate/Beginner 3D/4D, just Advanced's own numeric authority).
/// 5D is the genuinely novel cell: KEY2 (LaneOrdinal 1) is a real second true-hard stimulus via
/// THRESHOLD_TEMPO in Build/RaceSpecific (never HM_PACE), reverting to EASY_EQUIVALENT in
/// Foundation/Taper -- the opposite structural choice from Intermediate 5D's own FARTLEK-based
/// light-quality KEY2, proven here to remain completely unaffected (zero-delta) by this phase.
/// </summary>
public sealed class Hm16Advanced3D4D5DFullDarkVerticalSliceTests
{
    private readonly ITestOutputHelper _output;

    public Hm16Advanced3D4D5DFullDarkVerticalSliceTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // ============================================================================================
    // 3D golden 14W trace. RecentWeeklyVolumeKm = HalfMarathonAdvanced3D's own
    // GoldenFixtureStartingVolumeKm (24.5) -> resolved peak = ResolvedPeakReference.Value = 42.0km.
    // ============================================================================================
    [Fact]
    public async Task PreferredFourteenWeekCore_ThreeDay_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync(3);
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, daysPerWeek: 3, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(42, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(3, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        // Advanced's up-to-2-hard capacity is genuine unused headroom at 3D (HM.15 §5) -- exactly
        // one KEY_SESSION per week, never a second, never invented.
        Assert.All(prescribed.Weeks, w => Assert.Single(w.Sessions.Where(s => s.StructuralRole == "KEY_SESSION")));

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(42d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.46d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        Assert.Equal(29.5d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(18.0d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");
        Assert.NotEqual(Math.Round(taper[0].PlannedWeeklyVolumeKm * 0.43d, 1), taper[1].PlannedWeeklyVolumeKm);

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key = week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
            var easy = week.Sessions.Single(s => s.StructuralRole == "EASY_SUPPORT");
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            _output.WriteLine(
                $"Adv3D W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}|lr={longRunWeek.PlannedLongRunDistanceKm:F1}|share={longRunWeek.LongRunShareOfWeeklyVolume:P2}" +
                $"|key={key.ProgressionStageKey}|{key.WorkoutDefinitionKey} dist={key.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|easy_dist={easy.Prescription.DistancePrescription.PlannedDistanceKm:F1}|long_dist={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
    }

    // ============================================================================================
    // 4D golden 14W trace. RecentWeeklyVolumeKm = HalfMarathonAdvanced4D's own
    // GoldenFixtureStartingVolumeKm (31.0) -> resolved peak = 53.0km.
    // ============================================================================================
    [Fact]
    public async Task PreferredFourteenWeekCore_FourDay_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync(4);
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, daysPerWeek: 4, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(56, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(prescribed.Weeks, w => Assert.Single(w.Sessions.Where(s => s.StructuralRole == "KEY_SESSION")));
        Assert.All(prescribed.Weeks, w => Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT")));

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(53d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.40d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        Assert.Equal(37.0d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(23.0d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key = week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            _output.WriteLine(
                $"Adv4D W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}|lr={longRunWeek.PlannedLongRunDistanceKm:F1}|share={longRunWeek.LongRunShareOfWeeklyVolume:P2}" +
                $"|key={key.ProgressionStageKey}|{key.WorkoutDefinitionKey} dist={key.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|long_dist={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
    }

    // ============================================================================================
    // 5D golden 14W trace. RecentWeeklyVolumeKm = HalfMarathonAdvanced5D's own
    // GoldenFixtureStartingVolumeKm (36.0) -> resolved peak = 62.0km. This is the genuinely novel
    // cell: KEY2 (LaneOrdinal 1) becomes a real second true-hard stimulus via THRESHOLD_TEMPO in
    // Build/RaceSpecific, never HM_PACE anywhere.
    // ============================================================================================
    [Fact]
    public async Task PreferredFourteenWeekCore_FiveDay_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync(5);
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, daysPerWeek: 5, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;
        var dated = result.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(70, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(5, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        // Primary (LaneOrdinal 0) KEY stage sequence is byte-identical to every other HM cell's own
        // primary progression -- HM.15's frozen authority: KEY1 is completely unchanged.
        Assert.Equal(new[]
        {
            "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE",
            "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL", "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION"
        }, prescribed.Weeks.Select(w => PrimaryKey(w).ProgressionStageKey).ToArray());

        // Secondary (LaneOrdinal 1) KEY stage sequence -- HM.15 §7's frozen per-phase semantics:
        // Foundation/Taper = EASY_EQUIVALENT (identical stage keys to Intermediate 5D);
        // Build/RaceSpecific = the new Advanced-specific THRESHOLD_TEMPO stages, NOT FARTLEK.
        Assert.Equal(new[]
        {
            "FOUNDATION_HM_SECONDARY_EASY", "FOUNDATION_HM_SECONDARY_EASY", "FOUNDATION_HM_SECONDARY_EASY",
            "BUILD_HM_SECONDARY_THRESHOLD_ADVANCED", "BUILD_HM_SECONDARY_THRESHOLD_ADVANCED", "BUILD_HM_SECONDARY_THRESHOLD_ADVANCED", "BUILD_HM_SECONDARY_THRESHOLD_ADVANCED",
            "RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED", "RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED", "RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED",
            "RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED", "RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED",
            "TAPER_HM_SECONDARY_EASY", "TAPER_HM_SECONDARY_EASY"
        }, prescribed.Weeks.Select(w => SecondaryKey(w).ProgressionStageKey).ToArray());

        // Foundation/Taper bind EASY_STANDARD (not a true hard stimulus); Build/RaceSpecific bind
        // THRESHOLD_TEMPO -- a genuine second true-hard stimulus, HM.15's central novel decision.
        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "FOUNDATION" or "TAPER"),
            w => Assert.Equal("EASY_STANDARD", SecondaryKey(w).WorkoutDefinitionKey));
        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "BUILD" or "RACE_SPECIFIC"),
            w => Assert.Equal("THRESHOLD_TEMPO", SecondaryKey(w).WorkoutDefinitionKey));

        // KEY2 NEVER binds HM_PACE anywhere at any horizon (HM.15 §7/§8, deliberate exclusion).
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "HM_PACE");
        // Never a third hard stimulus and never FARTLEK on the secondary lane (that is
        // Intermediate 5D's own, different, model -- explicitly not reused here).
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "FARTLEK");

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(62d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.36d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        Assert.Equal(43.5d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(26.5d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");

        // Calendar/spacing proof: both KEY dates remain >=2 days apart at every week.
        foreach (var week in prescribed.Weeks)
        {
            var keyDates = week.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").Select(s => s.Date).OrderBy(d => d).ToArray();
            Assert.Equal(2, keyDates.Length);
            Assert.True(Math.Abs(keyDates[1].DayNumber - keyDates[0].DayNumber) >= 2,
                $"Week {week.WeekNumber}: KEY_SESSION dates {keyDates[0]}/{keyDates[1]} are closer than the required ~48h/2-day minimum.");
        }

        // Calendar-hardness proof (§10/§28 of the phase report): Build/RaceSpecific now resolve
        // BOTH KEY roles as TrueHard (unlike Intermediate 5D, where only KEY1 is TrueHard in those
        // phases) -- the existing generic family-based hardness mechanism handles this with zero
        // new code, and the same >=48h true-hard spacing invariant is proven to still hold.
        foreach (var week in dated.Weeks)
        {
            var keys = week.SessionSlots.Where(slot => slot.StructuralRole == "KEY_SESSION")
                .OrderBy(slot => slot.SlotOrderInWeek).ToArray();
            var expected = week.PhaseKey switch
            {
                "FOUNDATION" => new[] { CatalogCalendarSessionHardness.EasyEquivalent, CatalogCalendarSessionHardness.EasyEquivalent },
                "BUILD" or "RACE_SPECIFIC" => new[] { CatalogCalendarSessionHardness.TrueHard, CatalogCalendarSessionHardness.TrueHard },
                "TAPER" => new[] { CatalogCalendarSessionHardness.TrueHard, CatalogCalendarSessionHardness.EasyEquivalent },
                _ => throw new InvalidOperationException($"Unexpected phase {week.PhaseKey}."),
            };
            Assert.Equal(expected, keys.Select(key => key.Provenance.Hardness).ToArray());
        }

        var hardDates = dated.Weeks.SelectMany(week => week.SessionSlots)
            .Where(slot => slot.Provenance.Hardness == CatalogCalendarSessionHardness.TrueHard)
            .Select(slot => slot.SessionDate).OrderBy(date => date).ToArray();
        for (var i = 1; i < hardDates.Length; i++)
        {
            Assert.True(hardDates[i].DayNumber - hardDates[i - 1].DayNumber >= 2,
                $"True-hard sessions {hardDates[i - 1]} and {hardDates[i]} violate the 48h floor.");
        }

        // Max-2-true-hard-stimuli held (never 3): every week has exactly 2 KEY_SESSION roles total
        // (the layout's own structural ceiling) -- no third hard session anywhere, any phase.
        Assert.All(prescribed.Weeks, w => Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION")));

        // ========================================================================================
        // §10 two-threshold-same-week audit: in Build/RaceSpecific, KEY1 (BUILD_THRESHOLD_DEVELOPMENT)
        // and KEY2 (BUILD_HM_SECONDARY_THRESHOLD_ADVANCED) both resolve to THRESHOLD_TEMPO in the
        // same week. Verify the existing, unmodified, generic multi-KEY session-volume allocator
        // (FourDaySessionDistanceAllocationPolicy.Allocate, equal-split across KeySessionCount)
        // produces two real, distinguishable, non-degenerate volumes for both roles -- no invented
        // dose number, no silent downgrade of either KEY back to a light stimulus.
        // ========================================================================================
        // KEY1's own first Build week uses BUILD_FASTER_THAN_HM_INTRO (FARTLEK) before threshold
        // work begins (unchanged, level-blind primary-lane behavior, identical to every other HM
        // cell) -- the genuine two-threshold-same-week condition only arises once KEY1 itself has
        // also reached BUILD_THRESHOLD_DEVELOPMENT/RACE_SPECIFIC_HM_INTRODUCTION's own
        // THRESHOLD_TEMPO stages, which KEY2 (BUILD_HM_SECONDARY_THRESHOLD_ADVANCED /
        // RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED) always resolves to THRESHOLD_TEMPO from
        // its very first exposure. This loop filters to weeks where the condition is real.
        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "BUILD" or "RACE_SPECIFIC"),
            w => Assert.Equal("THRESHOLD_TEMPO", SecondaryKey(w).WorkoutDefinitionKey));
        foreach (var week in prescribed.Weeks.Where(w =>
            w.PhaseKey is "BUILD" or "RACE_SPECIFIC" && PrimaryKey(w).WorkoutDefinitionKey == "THRESHOLD_TEMPO"))
        {
            var key1 = PrimaryKey(week);
            var key2 = SecondaryKey(week);
            Assert.Equal("THRESHOLD_TEMPO", key1.WorkoutDefinitionKey);
            Assert.Equal("THRESHOLD_TEMPO", key2.WorkoutDefinitionKey);
            var d1 = key1.Prescription.DistancePrescription.PlannedDistanceKm;
            var d2 = key2.Prescription.DistancePrescription.PlannedDistanceKm;
            // Non-degenerate: both comfortably above the generic minimum KEY floor (3.0km).
            Assert.True(d1 >= 3.0d, $"W{week.WeekNumber} KEY1 THRESHOLD_TEMPO volume {d1} is degenerate.");
            Assert.True(d2 >= 3.0d, $"W{week.WeekNumber} KEY2 THRESHOLD_TEMPO volume {d2} is degenerate.");
            _output.WriteLine($"Adv5D §10 two-threshold W{week.WeekNumber}: KEY1={d1:F1}km KEY2={d2:F1}km (equal-split generic allocator)");
        }

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key1 = PrimaryKey(week);
            var key2 = SecondaryKey(week);
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            _output.WriteLine(
                $"Adv5D W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}|lr={longRunWeek.PlannedLongRunDistanceKm:F1}|share={longRunWeek.LongRunShareOfWeeklyVolume:P2}" +
                $"|key1={key1.ProgressionStageKey}|{key1.WorkoutDefinitionKey}@{key1.Date} dist={key1.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|key2={key2.ProgressionStageKey}|{key2.WorkoutDefinitionKey}@{key2.Date} dist={key2.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|long={longRun.WorkoutDefinitionKey}@{longRun.Date} dist={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
    }

    // ============================================================================================
    // 10-16W full dark materialization for all three frequencies.
    // ============================================================================================
    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_ThreeDay_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks) =>
        await AssertNonPreferredHorizon(3, targetWeekCount, expectedPhaseWeeks, peak: 42d, lrHardCap: 0.46d);

    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_FourDay_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks) =>
        await AssertNonPreferredHorizon(4, targetWeekCount, expectedPhaseWeeks, peak: 53d, lrHardCap: 0.40d);

    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_FiveDay_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks) =>
        await AssertNonPreferredHorizon(5, targetWeekCount, expectedPhaseWeeks, peak: 62d, lrHardCap: 0.36d);

    private async Task AssertNonPreferredHorizon(int daysPerWeek, int targetWeekCount, int[] expectedPhaseWeeks, double peak, double lrHardCap)
    {
        var candidate = await CandidateAsync(daysPerWeek);

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);
        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, allocation.Phases.Select(p => p.PhaseKey));
        Assert.Equal(expectedPhaseWeeks, allocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.Equal(2, allocation.Phases.Single(p => p.PhaseKey == "TAPER").AllocatedWeeks);

        var result = await Pipeline().MaterializeAsync(Context(candidate, daysPerWeek, exactPaceEvidence: true, targetWeekCount));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(targetWeekCount, prescribed.Weeks.Count);
        Assert.Equal(targetWeekCount * daysPerWeek, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(daysPerWeek, w.Sessions.Count));
        Assert.Equal(expectedPhaseWeeks, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        var expectedKeyCount = daysPerWeek == 5 ? 2 : 1;
        Assert.All(prescribed.Weeks, w => Assert.Equal(expectedKeyCount, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION")));

        if (daysPerWeek == 5)
        {
            // KEY2 never binds HM_PACE at any horizon; never a third hard stimulus.
            Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "HM_PACE");
            Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "FARTLEK");
            foreach (var week in prescribed.Weeks)
            {
                var keyDates = week.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").Select(s => s.Date).OrderBy(d => d).ToArray();
                Assert.Equal(2, keyDates.Length);
                Assert.True(Math.Abs(keyDates[1].DayNumber - keyDates[0].DayNumber) >= 2);
            }
        }

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);
        if (targetWeekCount < 14)
        {
            Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= peak + 0.001d,
                $"daysPerWeek={daysPerWeek} targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} unexpectedly reached/exceeded the {peak}km reference despite fewer non-taper transitions than the calibration count.");
        }
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= peak + 0.001d,
            $"daysPerWeek={daysPerWeek} targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} exceeds the {peak}km reference ceiling.");
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= lrHardCap + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });

        // Weekly growth safety -- Preferred=7%, Hard=8%, AbsoluteWeeklyIncreaseCapKm=2.5 for ALL
        // three Advanced frequencies (HM.15 §13 -- unlike Intermediate 3D's own 2.0km outlier).
        const double absoluteCap = 2.5d;
        var orderedVolumeWeeks = volume.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        for (var i = 1; i < orderedVolumeWeeks.Length; i++)
        {
            var prev = orderedVolumeWeeks[i - 1];
            var cur = orderedVolumeWeeks[i];
            if (cur.IsTaperWeek || prev.IsTaperWeek) continue;
            var deltaKm = cur.PlannedWeeklyVolumeKm - prev.PlannedWeeklyVolumeKm;
            Assert.True(deltaKm <= absoluteCap + 0.001d,
                $"daysPerWeek={daysPerWeek} targetWeekCount={targetWeekCount}: W{prev.WeekNumber}->W{cur.WeekNumber} deltaKm={deltaKm:F2} exceeds the {absoluteCap}km absolute weekly-increase cap.");
        }

        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm,
            $"Taper must be non-chained/non-increasing for every horizon (week1={taperWeeks[0].PlannedWeeklyVolumeKm}, week2={taperWeeks[1].PlannedWeeklyVolumeKm}).");

        // §25 Advanced 5D taper-tension feasibility: even at the reduced Taper W2 volume, real
        // session-volume feasibility holds for every role (no negative residual, no floor
        // violation) -- verified via the plan's own validity, not merely assumed.
        Assert.True(prescribed.Weeks.Last(w => w.PhaseKey == "TAPER").Sessions.All(
            s => s.Prescription.DistancePrescription.PlannedDistanceKm >= 0));

        _output.WriteLine(
            $"H{daysPerWeek}D-{targetWeekCount}|vol=[{string.Join(',', volume.WeeklyVolumePlan.Weeks.Select(w => w.PlannedWeeklyVolumeKm.ToString("F1")))}]" +
            $"|lr=[{string.Join(',', volume.LongRunProgression.Weeks.Select(w => w.PlannedLongRunDistanceKm.ToString("F1")))}]" +
            $"|peak={volume.WeeklyVolumePlan.PeakVolumeKm:F1}|taper={taperWeeks[0].PlannedWeeklyVolumeKm:F1}/{taperWeeks[1].PlannedWeeklyVolumeKm:F1}");

        // HM stays dark regardless of newly-feasible catalog headroom.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Advanced, daysPerWeek));
    }

    // Boundary proof: 9W/17W remain infeasible for all three frequencies.
    [Theory]
    [InlineData(3, 9)]
    [InlineData(3, 17)]
    [InlineData(4, 9)]
    [InlineData(4, 17)]
    [InlineData(5, 9)]
    [InlineData(5, 17)]
    public async Task OutOfCanonicalRangeHorizons_RemainInfeasible(int daysPerWeek, int targetWeekCount)
    {
        var candidate = await CandidateAsync(daysPerWeek);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);

        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Empty(allocation.Phases);
        Assert.Contains(targetWeekCount < 10 ? "TARGET_BELOW_SUM_OF_MINIMUMS" : "TARGET_ABOVE_SUM_OF_MAXIMUMS", allocation.ReasonCode);
        await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() =>
            Pipeline().MaterializeAsync(Context(candidate, daysPerWeek, exactPaceEvidence: true, targetWeekCount)));
    }

    // ============================================================================================
    // HM_PACE binds when independent evidence qualifies (KEY1 only); falls back correctly (no
    // fabricated pace from desired finish time) when it doesn't -- for all three frequencies.
    // KEY2 (5D only) is explicitly proven to never bind HM_PACE regardless of evidence.
    // ============================================================================================
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task ExactPaceEvidence_BindsHmPaceForKey1Only(int daysPerWeek)
    {
        var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: true));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        Assert.Contains(prescribed.Sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
        Assert.True(prescribed.ValidationResult.IsValid);

        if (daysPerWeek == 5)
        {
            Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "HM_PACE");
            Assert.All(prescribed.Weeks, w => Assert.Equal(0, w.Sessions.Count(s => s.LaneOrdinal == 1 && s.WorkoutDefinitionKey == "HM_PACE")));
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task MissingIndependentExactPaceEvidence_FallsBackAndNeverFabricatesFromDesiredFinishTime(int daysPerWeek)
    {
        foreach (var targetWeekCount in new[] { 10, 11, 12, 13, 14, 15, 16 })
        {
            var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: false, targetWeekCount));
            var sessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions;

            Assert.DoesNotContain(sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
            Assert.DoesNotContain(sessions, s =>
                s.WorkoutDefinitionKey is not ("GOAL_PACE_TEN_K" or "HM_PACE") &&
                s.Prescription.PacePrescription.Kind == CatalogPacePrescriptionKind.ExactPace);
            Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        }
    }

    // ============================================================================================
    // Calibration safety: missing/explicit-zero readiness never substitutes the calibration-only
    // starting volume -- fails closed identically to Intermediate/Beginner.
    // ============================================================================================
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task MissingWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume(int daysPerWeek)
    {
        var context = Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = null;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() => Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task ZeroWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume(int daysPerWeek)
    {
        var context = Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = 0;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() => Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    // ============================================================================================
    // Identity/policy encoding proofs.
    // ============================================================================================
    [Fact]
    public void HalfMarathonAdvancedThreeAndFourAndFiveDayIdentity_RemainOutsideEveryPublicGate()
    {
        foreach (var daysPerWeek in new[] { 3, 4, 5 })
        {
            Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
                GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Advanced, daysPerWeek));
            Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
                GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Advanced, daysPerWeek));
        }
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayAdvancedCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Advanced, 3));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFourDayAdvancedCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Advanced, 4));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayAdvancedCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Advanced, 5));
    }

    [Fact]
    public void HalfMarathonAdvanced3D_ExactPolicyEncoding()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonAdvanced3D, VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek(3));
        var policy = VolumeSafetyPolicy.HalfMarathonAdvanced3D;
        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(24.5d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(42.0d, policy.ResolvedPeakReference.Value);
        Assert.Equal(11, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.34d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.40d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.38d, policy.LongRunSelectionShare);
        Assert.Equal(0.46d, policy.LongRunHardCapShare);
        Assert.Equal(19.0d, policy.PreferredAbsolutePeakLongRunKm);
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.TaperVolumeMultipliers);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);
    }

    [Fact]
    public void HalfMarathonAdvanced4D_ExactPolicyEncoding()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonAdvanced4D, VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek(4));
        var policy = VolumeSafetyPolicy.HalfMarathonAdvanced4D;
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(31.0d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(53.0d, policy.ResolvedPeakReference.Value);
        Assert.Equal(11, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.30d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.33d, policy.LongRunSelectionShare);
        Assert.Equal(0.40d, policy.LongRunHardCapShare);
        Assert.Equal(19.0d, policy.PreferredAbsolutePeakLongRunKm);
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.TaperVolumeMultipliers);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);
    }

    [Fact]
    public void HalfMarathonAdvanced5D_ExactPolicyEncoding()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonAdvanced5D, VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek(5));
        var policy = VolumeSafetyPolicy.HalfMarathonAdvanced5D;
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(36.0d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(62.0d, policy.ResolvedPeakReference.Value);
        Assert.Equal(11, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.28d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.28d, policy.LongRunSelectionShare);
        Assert.Equal(0.36d, policy.LongRunHardCapShare);
        Assert.Equal(19.0d, policy.PreferredAbsolutePeakLongRunKm);
        // Unlike HalfMarathonIntermediate5D's own 12.0km, HM.15 §17 found no comparable
        // feasibility trigger for Advanced 5D -- explicitly N/A.
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.TaperVolumeMultipliers);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);
    }

    // ============================================================================================
    // Zero-delta regression: Beginner (3D/4D/5D-ineligible), Intermediate (all three cells,
    // specifically re-confirming Intermediate 5D's own KEY2 STAYS FARTLEK-based), and live 10K
    // Advanced 3D/4D/5D/6D remain byte-identical after this phase's additions.
    // ============================================================================================
    [Fact]
    public void BeginnerHalfMarathonCells_ZeroDelta_AfterAdvancedDispatchAddition()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonBeginner3D, VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonBeginner4D, VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(4));
        Assert.Equal(27.0d, VolumeSafetyPolicy.HalfMarathonBeginner3D.ResolvedPeakReference.Value);
        Assert.Equal(33.0d, VolumeSafetyPolicy.HalfMarathonBeginner4D.ResolvedPeakReference.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(5));
        Assert.Null(V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Beginner, 5));
    }

    [Fact]
    public void IntermediateHalfMarathonCells_ZeroDelta_AfterAdvancedDispatchAddition()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate3D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate4D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate5D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(5));
        Assert.Equal(34.0d, VolumeSafetyPolicy.HalfMarathonIntermediate3D.ResolvedPeakReference.Value);
        Assert.Equal(43.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value);
        Assert.Equal(51.0d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.ResolvedPeakReference.Value);

        // Critical isolation check: Intermediate 5D's own KEY2 stays FARTLEK-based light-quality,
        // completely unaffected by Advanced 5D's own different (THRESHOLD_TEMPO true-hard) model.
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));
    }

    [Fact]
    public async Task IntermediateFiveDaySecondaryLane_StaysFartlekBased_UnaffectedByAdvancedFiveDay()
    {
        var candidate = await CandidateAsyncFor(
            V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateVersion);
        var context = IntermediateFiveDayContext(candidate, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));

        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "BUILD" or "RACE_SPECIFIC"),
            w => Assert.Equal("FARTLEK", SecondaryKey(w).WorkoutDefinitionKey));
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "THRESHOLD_TEMPO");
    }

    [Fact]
    public void TenKAdvancedThreeFourFiveSixDay_ZeroDelta_AfterHalfMarathonAdvancedAddition()
    {
        Assert.Same(VolumeSafetyPolicy.Advanced3D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.Advanced4D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.Advanced5D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(5));
        Assert.Same(VolumeSafetyPolicy.Advanced6D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(6));
        Assert.Equal(40d, VolumeSafetyPolicy.Advanced3D.ResolvedPeakReference.Value);
        Assert.Equal(45d, VolumeSafetyPolicy.Advanced4D.ResolvedPeakReference.Value);
        Assert.Equal(50d, VolumeSafetyPolicy.Advanced5D.ResolvedPeakReference.Value);
        Assert.Equal(51d, VolumeSafetyPolicy.Advanced6D.ResolvedPeakReference.Value);
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, 3));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, 4));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, 5));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, 6));
    }

    // ============================================================================================
    // Adaptation smoke tests for all three frequencies.
    // ============================================================================================
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task AdaptationSmoke_SingleKeyLane_ProducesConservativeDecisions(int daysPerWeek)
    {
        var prescribed = (await Pipeline().MaterializeAsync(Context(await CandidateAsync(daysPerWeek), daysPerWeek, exactPaceEvidence: true)))
            .PrescriptionResult.FinalPrescribedPlan;
        var buildWeek = prescribed.Weeks.First(week => week.PhaseKey == "BUILD");
        var key = buildWeek.Sessions.Single(s => s.StructuralRole == "KEY_SESSION");
        var easy = buildWeek.Sessions.First(s => s.StructuralRole == "EASY_SUPPORT");
        var longRun = buildWeek.Sessions.Single(s => s.StructuralRole == "LONG_RUN");

        Assert.Equal(daysPerWeek == 3 ? NextWindowLoadDecision.Maintain : NextWindowLoadDecision.ProgressAsPlanned,
            AdaptationDecision(buildWeek, missed: null));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, key));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, longRun));
        _ = easy;
    }

    [Fact]
    public async Task AdaptationSmoke_FiveDaySecondaryKeyMissed_IsConservative()
    {
        var prescribed = (await Pipeline().MaterializeAsync(Context(await CandidateAsync(5), 5, exactPaceEvidence: true)))
            .PrescriptionResult.FinalPrescribedPlan;
        var buildWeek = prescribed.Weeks.First(week => week.PhaseKey == "BUILD");
        var primary = PrimaryKey(buildWeek);
        var secondary = SecondaryKey(buildWeek);
        var easy = buildWeek.Sessions.First(session => session.StructuralRole == "EASY_SUPPORT");
        var longRun = buildWeek.Sessions.Single(session => session.StructuralRole == "LONG_RUN");

        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, AdaptationDecision(buildWeek, missed: null));
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, AdaptationDecision(buildWeek, easy));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, primary));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, secondary));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, longRun));
    }

    private static NextWindowLoadDecision AdaptationDecision(CatalogPrescribedWeek week, CatalogPrescribedSession? missed)
    {
        var evidence = week.Sessions.Select(session => new LogicalSessionEvidence(
            Guid.NewGuid(),
            session.StructuralRole switch
            {
                "KEY_SESSION" => PreparationRunwaySlotRole.KeySession,
                "EASY_SUPPORT" => PreparationRunwaySlotRole.EasySupport,
                "LONG_RUN" => PreparationRunwaySlotRole.LongRun,
                _ => throw new InvalidOperationException($"Unexpected role {session.StructuralRole}."),
            },
            ReferenceEquals(session, missed)
                ? LongHorizonRollingSessionOutcomeStatus.NotToday
                : LongHorizonRollingSessionOutcomeStatus.Completed,
            SessionPlanningStatus.Active,
            NotTodayReason: ReferenceEquals(session, missed) ? NotTodayReasonCode.ScheduleConflict : null)).ToArray();

        return NextWindowLoadDecisionPolicy.Evaluate(WindowExecutionSummaryBuilder.Build(evidence)).LoadDecision;
    }

    private static CatalogPrescribedSession PrimaryKey(CatalogPrescribedWeek week) =>
        week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION" && (s.LaneOrdinal ?? 0) == 0);

    private static CatalogPrescribedSession SecondaryKey(CatalogPrescribedWeek week) =>
        week.Sessions.Single(s => s.StructuralRole == "KEY_SESSION" && s.LaneOrdinal == 1);

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync(int daysPerWeek)
    {
        var (key, version) = daysPerWeek switch
        {
            3 => (V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayAdvancedCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayAdvancedCandidateVersion),
            4 => (V1CatalogPilotIdentityPolicy.HalfMarathonFourDayAdvancedCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonFourDayAdvancedCandidateVersion),
            5 => (V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayAdvancedCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayAdvancedCandidateVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek)),
        };
        return await CandidateAsyncFor(key, version);
    }

    private static async Task<PlanCatalogCandidateSummary> CandidateAsyncFor(string key, int version)
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(key, version);
    }

    private static DynamicCoreCalendarMaterializationOrchestrator Pipeline() => new(
        new DynamicCoreSessionPrescriptionOrchestrator(
            new DynamicCoreVolumeAndLongRunOrchestrator(
                new DynamicCoreWorkoutBindingOrchestrator(
                    new DynamicCoreWeekSkeletonOrchestrator(
                        new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                        new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                    new CatalogWorkoutProgressionLoader(Options.Create(OptionsValue)),
                    new ProgressionStageAllocator(), new GeneratedCatalogStageScheduleValidator(),
                    new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator(),
                    new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
            new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));

    private static DynamicCoreCalendarMaterializationContext Context(
        PlanCatalogCandidateSummary candidate,
        int daysPerWeek,
        bool exactPaceEvidence,
        int targetWeekCount = 14)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var goalKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon);
        // Golden-fixture RecentWeeklyVolumeKm equals each policy's own GoldenFixtureStartingVolumeKm
        // (24.5/31.0/36.0), so the 14W preferred-horizon test resolves cleanly to the selected peak
        // with no residual scaling.
        var recentWeeklyVolumeKm = daysPerWeek switch { 3 => 24.5, 4 => 31.0, _ => 36.0 };
        var recentLongestRunKm = daysPerWeek switch { 3 => 12d, 4 => 14d, _ => 16d };
        var preferredDays = daysPerWeek switch
        {
            3 => new[] { Weekday.Tue, Weekday.Thu, Weekday.Sun },
            4 => new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
            _ => new[] { Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Fri, Weekday.Sun },
        };
        var preferredDaysOfWeek = daysPerWeek switch
        {
            3 => new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Sunday },
            4 => new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday },
            _ => new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday },
        };

        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.HalfMarathon, Level = RunningBackground.Advanced,
            DaysPerWeek = daysPerWeek, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = exactPaceEvidence ? 5700 : null,
            PreferredDays = preferredDays, LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = recentWeeklyVolumeKm, RecentLongestRunKm = recentLongestRunKm, RecentRunsPerWeek = daysPerWeek,
            RecentRace = exactPaceEvidence
                ? new RecentRaceInput { Distance = GoalDistance.HalfMarathon, FinishTimeSeconds = 6000, RaceDate = start.AddDays(-21) }
                : null,
        };

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = preferredDaysOfWeek,
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults = exactPaceEvidence
                ? [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND")]
                : [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME")],
            PreviewRequest = request,
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = goalKm, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = goalKm, StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, DaysPerWeek = daysPerWeek, Level = RunningBackground.Advanced,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }

    /// <summary>Minimal Intermediate 5D context, mirroring Hm11's own fixture, used only for the
    /// isolation check that Advanced 5D's new KEY2 model does not leak into Intermediate 5D.</summary>
    private static DynamicCoreCalendarMaterializationContext IntermediateFiveDayContext(
        PlanCatalogCandidateSummary candidate, bool exactPaceEvidence, int targetWeekCount = 14)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var goalKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon);
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.HalfMarathon, Level = RunningBackground.Intermediate,
            DaysPerWeek = 5, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = exactPaceEvidence ? 5700 : null,
            PreferredDays = [Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Fri, Weekday.Sun], LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 29.5, RecentLongestRunKm = 12, RecentRunsPerWeek = 5,
            RecentRace = exactPaceEvidence
                ? new RecentRaceInput { Distance = GoalDistance.HalfMarathon, FinishTimeSeconds = 6000, RaceDate = start.AddDays(-21) }
                : null,
        };

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday],
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults = exactPaceEvidence
                ? [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND")]
                : [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME")],
            PreviewRequest = request,
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = goalKm, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = goalKm, StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, DaysPerWeek = 5, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }
}
