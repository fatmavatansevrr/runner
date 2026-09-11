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
/// Phase HM.11 — the same class of test harness as <see cref="Hm2FullDarkVerticalSliceTests"/>/
/// <see cref="Hm8Intermediate3DFullDarkVerticalSliceTests"/>, exercising the real, unmodified
/// production pipeline against the new dark-only HALF_MARATHON__5D__INTERMEDIATE candidate,
/// implementing HM.9's frozen second-KEY-lane structural authority
/// (PHASE_HM_9_INTERMEDIATE_5D_SECOND_KEY_LANE_AUTHORITY_CLOSURE.md) and HM.10's frozen numeric
/// authority (PHASE_HM_10_INTERMEDIATE_5D_NUMERIC_AUTHORITY_CLOSURE.md). RUN_LAYOUT_5D =
/// {KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN} -- LaneOrdinal 0 is the
/// unchanged primary HM progression; LaneOrdinal 1 is the new secondary-KEY lane
/// (half-marathon-workout-progression.v2.json, consumed only through the new
/// HALF_MARATHON_MASTER v2 / HALF_MARATHON__5D__INTERMEDIATE combination -- 3D/4D remain on
/// v1/v1, byte-identical).
/// </summary>
public sealed class Hm11Intermediate5DFullDarkVerticalSliceTests
{
    private readonly ITestOutputHelper _output;

    public Hm11Intermediate5DFullDarkVerticalSliceTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // Golden fixture: RecentWeeklyVolumeKm equals HalfMarathonIntermediate5D's own
    // GoldenFixtureStartingVolumeKm (29.5), so the 14W (11-transition, exactly the calibration
    // point) resolved peak is exactly ResolvedPeakReference.Value = 51.0km -- the same clean,
    // self-consistent, no-residual-scaling pattern already proved for 4D (25->43) and 3D (20->34).
    [Fact]
    public async Task PreferredFourteenWeekCore_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync();
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;
        var dated = result.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks
            .GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(70, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(5, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s =>
            Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        // Primary (LaneOrdinal 0) KEY stage sequence is byte-identical to 4D's/3D's own --
        // HM.9's frozen authority: the primary progression is completely unchanged.
        Assert.Equal(new[]
        {
            "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE",
            "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL", "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION"
        }, prescribed.Weeks.Select(w => PrimaryKey(w).ProgressionStageKey).ToArray());

        // Secondary (LaneOrdinal 1) KEY stage sequence -- HM.9's frozen per-phase semantics:
        // Foundation/Taper = EASY_EQUIVALENT; Build/RaceSpecific = FARTLEK (goal feasibility
        // is evaluated, so the preferred FARTLEK stage is eligible; exact pace is not a
        // secondary-lane requirement).
        Assert.Equal(new[]
        {
            "FOUNDATION_HM_SECONDARY_EASY", "FOUNDATION_HM_SECONDARY_EASY", "FOUNDATION_HM_SECONDARY_EASY",
            "BUILD_HM_SECONDARY_FARTLEK", "BUILD_HM_SECONDARY_FARTLEK", "BUILD_HM_SECONDARY_FARTLEK", "BUILD_HM_SECONDARY_FARTLEK",
            "RACE_SPECIFIC_HM_SECONDARY_FARTLEK", "RACE_SPECIFIC_HM_SECONDARY_FARTLEK", "RACE_SPECIFIC_HM_SECONDARY_FARTLEK",
            "RACE_SPECIFIC_HM_SECONDARY_FARTLEK", "RACE_SPECIFIC_HM_SECONDARY_FARTLEK",
            "TAPER_HM_SECONDARY_EASY", "TAPER_HM_SECONDARY_EASY"
        }, prescribed.Weeks.Select(w => SecondaryKey(w).ProgressionStageKey).ToArray());

        // HM.9 secondary-KEY hardness/workout-family proof: Foundation/Taper bind EASY_STANDARD
        // (not a true hard stimulus); Build/RaceSpecific bind FARTLEK (LIGHT_QUALITY).
        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "FOUNDATION" or "TAPER"),
            w => Assert.Equal("EASY_STANDARD", SecondaryKey(w).WorkoutDefinitionKey));
        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "BUILD" or "RACE_SPECIFIC"),
            w => Assert.Equal("FARTLEK", SecondaryKey(w).WorkoutDefinitionKey));

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(51d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.36d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        Assert.Equal(35.5d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(22.0d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");

        // Calendar/spacing proof (Section 9/10) -- this preferred-day fixture permits the
        // historical maximum-separation placement, so both KEY dates remain >=2 days apart;
        // the generic hardness seam below additionally proves that this is enforced as a
        // hard constraint only when both resolved workouts are true-hard.
        foreach (var week in prescribed.Weeks)
        {
            var keyDates = week.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").Select(s => s.Date).OrderBy(d => d).ToArray();
            Assert.Equal(2, keyDates.Length);
            Assert.True(Math.Abs(keyDates[1].DayNumber - keyDates[0].DayNumber) >= 2,
                $"Week {week.WeekNumber}: KEY_SESSION dates {keyDates[0]}/{keyDates[1]} are closer than the required ~48h/2-day minimum.");
            var longRunDate = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN").Date;
            Assert.All(keyDates, d => Assert.True(Math.Abs(d.DayNumber - longRunDate.DayNumber) >= 2));
        }

        // The calendar now consumes generic workout-family hardness resolved after stage
        // fallback and before date placement; it does not infer hardness from KEY_SESSION.
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

        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key1 = PrimaryKey(week);
            var key2 = SecondaryKey(week);
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            var easy = week.Sessions.Where(s => s.StructuralRole == "EASY_SUPPORT").OrderBy(s => s.Date).ToArray();
            var datedWeek = dated.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var datedKeys = datedWeek.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION")
                .OrderBy(s => s.SlotOrderInWeek).ToArray();
            var taperPosition = volumeWeek.IsTaperWeek
                ? $"{volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek && w.WeekNumber <= week.WeekNumber).Count()}/2"
                : "none";
            _output.WriteLine(
                $"W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}|lr={longRunWeek.PlannedLongRunDistanceKm:F1}|share={longRunWeek.LongRunShareOfWeeklyVolume:P2}" +
                $"|key1={key1.ProgressionStageKey}|{key1.WorkoutDefinitionKey}@{key1.Date} dist={key1.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|key2={key2.ProgressionStageKey}|{key2.WorkoutDefinitionKey}@{key2.Date} dist={key2.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|key2hard={datedKeys[1].Provenance.Hardness}|easy={easy[0].WorkoutDefinitionKey}/{easy[1].WorkoutDefinitionKey}" +
                $"|long={longRun.WorkoutDefinitionKey}@{longRun.Date} dist={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|days={string.Join(',', week.Sessions.OrderBy(s => s.Date).Select(s => s.Date.DayOfWeek.ToString()[..3]))}" +
                $"|taper={taperPosition}|binding={key1.BindingProvenance}|reason={volumeWeek.DecisionReason}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
    }

    // HM.11 -- 10W-16W full dark materialization, mirroring Hm2/Hm8's own NonPreferredCoreLengths
    // theory exactly. Phase allocation is frequency-generic (HM.4/HM.6, unchanged, reused
    // verbatim) -- the same exact 7-row oracle table 4D/3D already prove applies identically here.
    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks)
    {
        var candidate = await CandidateAsync();

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);
        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, allocation.Phases.Select(p => p.PhaseKey));
        Assert.Equal(expectedPhaseWeeks, allocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.Equal(2, allocation.Phases.Single(p => p.PhaseKey == "TAPER").AllocatedWeeks);
        Assert.True(allocation.Phases.Single(p => p.PhaseKey == "RACE_SPECIFIC").AllocatedWeeks <= 5);

        var result = await Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(targetWeekCount, prescribed.Weeks.Count);
        Assert.Equal(targetWeekCount * 5, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(5, w.Sessions.Count));
        Assert.Equal(expectedPhaseWeeks, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s =>
            Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        var primaryStages = prescribed.Weeks.Select(w => PrimaryKey(w).ProgressionStageKey).ToArray();
        var knownPrimaryStages = new HashSet<string?>
        {
            "FOUNDATION_AEROBIC_BASE", "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT_CURRENT_FITNESS_FALLBACK",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL_CURRENT_FITNESS_FALLBACK",
            "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION_EFFORT_FALLBACK",
        };
        Assert.All(primaryStages, s => Assert.Contains(s, knownPrimaryStages));
        Assert.Equal(2, primaryStages.Count(s => s is "RACE_SPECIFIC_HM_REHEARSAL" or "RACE_SPECIFIC_HM_REHEARSAL_CURRENT_FITNESS_FALLBACK"));
        Assert.Equal(2, primaryStages.Count(s => s is "TAPER_HM_ACTIVATION" or "TAPER_HM_ACTIVATION_EFFORT_FALLBACK"));

        var secondaryStages = prescribed.Weeks.Select(w => SecondaryKey(w).ProgressionStageKey).ToArray();
        var knownSecondaryStages = new HashSet<string?>
        {
            "FOUNDATION_HM_SECONDARY_EASY",
            "BUILD_HM_SECONDARY_FARTLEK", "BUILD_HM_SECONDARY_EASY_FALLBACK",
            "RACE_SPECIFIC_HM_SECONDARY_FARTLEK", "RACE_SPECIFIC_HM_SECONDARY_EASY_FALLBACK",
            "TAPER_HM_SECONDARY_EASY",
        };
        Assert.All(secondaryStages, s => Assert.Contains(s, knownSecondaryStages));
        Assert.Equal(2, secondaryStages.Count(s => s == "TAPER_HM_SECONDARY_EASY"));

        // Two-true-hard-stimulus budget (HM.9 §12/§19): with exact pace evidence supplied,
        // Build/RaceSpecific weeks carry primary (hard) + secondary FARTLEK (light-quality) --
        // never a THRESHOLD_TEMPO/HM_PACE-class workout on the secondary lane.
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey is "HM_PACE" or "THRESHOLD_TEMPO");

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);
        // Short horizons (10W-13W, fewer than 11 transitions) must never be forced to chase
        // the 51.0km reference peak.
        if (targetWeekCount < 14)
        {
            Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= 51d + 0.001d,
                $"targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} unexpectedly reached/exceeded the 51.0km reference despite fewer non-taper transitions than the calibration count.");
        }
        // Long horizons (15W/16W, more than 11 transitions) must never extrapolate past 51.0km
        // (the exact HM.5.3 mechanism, reused via ResolvedPeakReferenceIsSelectedCeiling=true).
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= 51d + 0.001d,
            $"targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} exceeds the 51.0km reference ceiling.");
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.36d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm,
            $"Taper must be non-chained/non-increasing for every horizon (week1={taperWeeks[0].PlannedWeeklyVolumeKm}, week2={taperWeeks[1].PlannedWeeklyVolumeKm}).");

        var preTaperAnchor = volume.WeeklyVolumePlan.Weeks.Last(w => !w.IsTaperWeek).PlannedWeeklyVolumeKm;
        _output.WriteLine(
            $"H{targetWeekCount}|phase={string.Join('/', expectedPhaseWeeks)}|vol=[{string.Join(',', volume.WeeklyVolumePlan.Weeks.Select(w => w.PlannedWeeklyVolumeKm.ToString("F1")))}]" +
            $"|lr=[{string.Join(',', volume.LongRunProgression.Weeks.Select(w => w.PlannedLongRunDistanceKm.ToString("F1")))}]" +
            $"|share=[{string.Join(',', volume.LongRunProgression.Weeks.Select(w => w.LongRunShareOfWeeklyVolume.ToString("P2")))}]" +
            $"|peak={volume.WeeklyVolumePlan.PeakVolumeKm:F1}|peakLR={volume.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm):F1}" +
            $"|anchor={preTaperAnchor:F1}|taper={taperWeeks[0].PlannedWeeklyVolumeKm:F1}/{taperWeeks[1].PlannedWeeklyVolumeKm:F1}");
        _output.WriteLine($"H{targetWeekCount}|primary=[{string.Join(',', primaryStages)}]|secondary=[{string.Join(',', secondaryStages)}]");
        _output.WriteLine(
            $"H{targetWeekCount}|growth=[{string.Join(';', volume.WeeklyVolumePlan.Weeks.Where(w => !w.IsTaperWeek && w.PreviousWeekVolumeKm.HasValue).Select(w => $"{w.PreviousWeekVolumeKm:F1}>{w.PlannedWeeklyVolumeKm:F1}:d{w.ChangeKm:F1}:p{w.ChangePercent:P2}:{w.AppliedClamp}"))}]" +
            $"|calendar=[{string.Join(';', prescribed.Weeks.Select(w => $"W{w.WeekNumber}:{string.Join(',', w.Sessions.OrderBy(s => s.Date).Select(s => s.Date.DayOfWeek.ToString()[..3]))}"))}]");

        // Calendar spacing holds for every horizon, not merely the 14W golden case.
        foreach (var week in prescribed.Weeks)
        {
            var keyDates = week.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").Select(s => s.Date).OrderBy(d => d).ToArray();
            Assert.Equal(2, keyDates.Length);
            Assert.True(Math.Abs(keyDates[1].DayNumber - keyDates[0].DayNumber) >= 2);
        }

        // HM stays dark regardless of newly-feasible catalog headroom.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));
    }

    // Boundary proof: 9W/17W remain infeasible, exactly as 4D/3D already prove (shared,
    // frequency-generic phase-allocation authority; no 5D-specific horizon table).
    [Theory]
    [InlineData(9)]
    [InlineData(17)]
    public async Task OutOfCanonicalRangeHorizons_RemainInfeasible(int targetWeekCount)
    {
        var candidate = await CandidateAsync();

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);

        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Empty(allocation.Phases);
        Assert.Contains(targetWeekCount < 10 ? "TARGET_BELOW_SUM_OF_MINIMUMS" : "TARGET_ABOVE_SUM_OF_MAXIMUMS", allocation.ReasonCode);
        await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() =>
            Pipeline().MaterializeAsync(Context(candidate, exactPaceEvidence: true, targetWeekCount)));
    }

    // HM.9's own named "downgrade secondary quality" fallback (HM-LIT-21), made live via the
    // existing GOAL_FEASIBILITY_IN eligibility seam (deliberately NOT PACE_SOURCE_IN): when
    // goal feasibility is not evaluated, the preferred secondary stages
    // BUILD_HM_SECONDARY_FARTLEK/RACE_SPECIFIC_HM_SECONDARY_FARTLEK become ineligible and fall
    // back to their EASY_STANDARD-bound fallback stage. FARTLEK's own pace prescription is
    // effort-only regardless (HM.10 §19) -- this fallback governs STAGE SELECTION, a distinct
    // eligibility axis from pace resolution.
    [Fact]
    public async Task PreferredSecondaryStageIneligible_SecondaryLaneFallsBackToEasyStandard()
    {
        var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(), exactPaceEvidence: false));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));

        Assert.DoesNotContain(prescribed.Sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
        Assert.DoesNotContain(prescribed.Sessions, s => s.LaneOrdinal == 1 && s.WorkoutDefinitionKey == "FARTLEK");

        foreach (var week in prescribed.Weeks.Where(w => w.PhaseKey is "BUILD" or "RACE_SPECIFIC"))
        {
            var secondary = SecondaryKey(week);
            Assert.Equal("EASY_STANDARD", secondary.WorkoutDefinitionKey);
            Assert.NotNull(secondary.FallbackProvenance);
        }
    // Foundation/Taper are unaffected -- they never carried an eligibility-gated stage.
        foreach (var week in prescribed.Weeks.Where(w => w.PhaseKey is "FOUNDATION" or "TAPER"))
        {
            Assert.Equal("EASY_STANDARD", SecondaryKey(week).WorkoutDefinitionKey);
        }
    }

    [Fact]
    public async Task EffortOnlyFallbackMode_HoldsForEveryFeasibleHorizon()
    {
        foreach (var targetWeekCount in new[] { 10, 11, 12, 13, 14, 15, 16 })
        {
            var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(), exactPaceEvidence: false, targetWeekCount));
            var sessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions;

            Assert.DoesNotContain(sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
            Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        }
    }

    [Fact]
    public async Task NoIndependentExactFitnessEvidence_ProductAverageGoalStillUsesEffortOnlySecondaryFartlek()
    {
        var result = await Pipeline().MaterializeAsync(
            Context(await CandidateAsync(), exactPaceEvidence: false, productAverageGoal: true));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.DoesNotContain(prescribed.Sessions, session => session.WorkoutDefinitionKey == "HM_PACE");

        var secondaryQuality = prescribed.Weeks
            .Where(week => week.PhaseKey is "BUILD" or "RACE_SPECIFIC")
            .Select(SecondaryKey).ToArray();
        Assert.NotEmpty(secondaryQuality);
        Assert.All(secondaryQuality, session =>
        {
            Assert.Equal("FARTLEK", session.WorkoutDefinitionKey);
            Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, session.Prescription.PacePrescription.Kind);
        });
    }

    [Fact]
    public async Task MissingWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume()
    {
        var context = Context(await CandidateAsync(), exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = null;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() =>
            Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    [Fact]
    public async Task ZeroWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume()
    {
        var context = Context(await CandidateAsync(), exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = 0;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() =>
            Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    [Fact]
    public async Task StartingLongRunCap_BoundsFirstWeekWithoutReplacingReadinessEvidence()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, exactPaceEvidence: true);
        context.PreviewRequest.RecentWeeklyVolumeKm = 50;
        context.PreviewRequest.RecentLongestRunKm = 16;
        var result = await Pipeline().MaterializeAsync(context);
        Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        Assert.Equal(12d, result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.LongRunProgression.Weeks[0].PlannedLongRunDistanceKm);

        var noHistory = Context(candidate, exactPaceEvidence: true);
        noHistory.PreviewRequest.RecentWeeklyVolumeKm = 20;
        noHistory.PreviewRequest.RecentLongestRunKm = null;
        var noHistoryResult = await Pipeline().MaterializeAsync(noHistory);
        var firstLongRun = noHistoryResult.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan.LongRunProgression.Weeks[0];
        Assert.True(firstLongRun.PlannedLongRunDistanceKm < 12d);
        Assert.Equal(LongRunAnchorSource.WeeklyVolumeDerived, firstLongRun.LongRunAnchorSource);
    }

    [Fact]
    public void HalfMarathonFiveDayIdentity_RemainsOutsideEveryPublicGate()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));
    }

    [Fact]
    public void HalfMarathonFourAndThreeDayIdentity_ZeroDelta_AfterFiveDayDispatchGeneralization()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate4D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate3D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate5D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(5));

        var policy = VolumeSafetyPolicy.HalfMarathonIntermediate5D;
        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(29.5d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(51d, policy.ResolvedPeakReference.Value);
        Assert.Equal(11, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.28d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.28d, policy.LongRunSelectionShare);
        Assert.Equal(0.36d, policy.LongRunHardCapShare);
        Assert.Equal(19d, policy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(12d, policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.TaperVolumeMultipliers);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);
    }

    [Fact]
    public async Task FiveSessionAdaptationSmoke_IsConservativeAndLaneBlindForTheTwoKeyRoles()
    {
        var prescribed = (await Pipeline().MaterializeAsync(
            Context(await CandidateAsync(), exactPaceEvidence: true)))
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

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateVersion);
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
        bool exactPaceEvidence,
        int targetWeekCount = 14,
        bool productAverageGoal = false)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var goalKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon);
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.HalfMarathon, Level = RunningBackground.Intermediate,
            DaysPerWeek = 5, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = exactPaceEvidence ? 5700 : productAverageGoal ? 7500 : null,
            TargetFinishTimeSource = productAverageGoal ? TargetFinishTimeSource.ProductAverage : exactPaceEvidence ? TargetFinishTimeSource.UserDefined : null,
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
                : productAverageGoal
                    ? [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "TARGET_TIME", "TARGET_FINISH_TIME_PROVIDED"),
                       RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "CHALLENGING", "PACE_SOURCE_TARGET_TIME_PRODUCT_AVERAGE_ACCEPTED")]
                : [RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                   RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME")],
            PreviewRequest = request,
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = goalKm, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = goalKm, StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, TargetFinishTimeSource = request.TargetFinishTimeSource,
                DaysPerWeek = 5, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }
}
