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
/// Phase HM-X1.3 — the same class of dark-only vertical-slice test harness as
/// <see cref="Hm11Intermediate5DFullDarkVerticalSliceTests"/>/<see cref="Hm16Advanced3D4D5DFullDarkVerticalSliceTests"/>,
/// exercising the real, unmodified production pipeline against the two new dark-only
/// HALF_MARATHON__6D__INTERMEDIATE / HALF_MARATHON__6D__ADVANCED candidates, implementing
/// HM-X1.2's frozen numeric/structural authority
/// (PHASE_HM_X1_2_6D_INTERMEDIATE_ADVANCED_STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md).
/// Candidates are loaded via <see cref="CatalogCandidateEligibilityGate.LoadForInternalDryRunAsync"/>
/// directly by (CandidateKey, CandidateVersion) — bypassing the public
/// <see cref="V1CatalogPilotIdentityPolicy.IsSupportedIdentity"/> gate entirely, exactly as
/// every prior HM dark-implementation phase's own test harness does. Both cells reuse
/// RUN_LAYOUT_6D (2×KEY_SESSION, 3×EASY_SUPPORT, 1×LONG_RUN, unmodified) and the existing
/// HALF_MARATHON_WORKOUT_PROGRESSION v2/v3 (unmodified) via new, minimal, additive
/// HALF_MARATHON_MASTER v4/v5 (byte-identical to v2/v3 except supportedRunsPerWeek widened
/// to include 6 — required because PlanCatalog.Core's CatalogGraphValidator hard-fails
/// TC_LAYOUT_RUNS_PER_WEEK_NOT_SUPPORTED otherwise).
/// </summary>
public sealed class HmX13Intermediate6DAdvanced6DFullDarkVerticalSliceTests
{
    private readonly ITestOutputHelper _output;

    public HmX13Intermediate6DAdvanced6DFullDarkVerticalSliceTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // ============================================================================================
    // Identity/policy encoding proofs — dark/public separation.
    // ============================================================================================
    [Fact]
    public void HalfMarathonSixDayIntermediateAndAdvanced_ArePubliclyActivated_ForCoreOnly()
    {
        // HM-X1.4 superseded this dark-only-for-Core-too assertion: the two cells this test
        // originally proved were dark are now the exact, sole, intentional public-activation
        // scope of HM-X1.4. This is not a regression in HM-X1.3 -- it is the planned next step
        // HM-X1.3 §38/HM-X1.4-readiness explicitly anticipated.
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 6));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Advanced, 6));

        // Preparation Runway/LongHorizon remain untouched and closed -- HM-X1.4 activates Core
        // only, per its own explicit scope boundary.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 6));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity(
            GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Advanced, 6));

        // The non-throwing public-routing check now resolves both identities for real.
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonSixDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 6));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonSixDayAdvancedCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Advanced, 6));

        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonSixDayIntermediateCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Intermediate, 6));
        Assert.Equal((V1CatalogPilotIdentityPolicy.HalfMarathonSixDayAdvancedCandidateKey, 1),
            V1CatalogPilotIdentityPolicy.ResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Advanced, 6));
    }

    [Fact]
    public void BeginnerAndExperienced_SixDayHalfMarathon_DoNotExist()
    {
        // Negative proof: no HALF_MARATHON x BEGINNER x6D or x EXPERIENCED x6D combination/policy
        // exists anywhere. Beginner's dispatcher has no 5D arm (PRODUCT_INELIGIBLE) and, a
        // fortiori, no 6D arm either.
        Assert.Throws<ArgumentOutOfRangeException>(() => VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(6));
        Assert.False(File.Exists(Path.Combine(CatalogRoot, "combinations", "half-marathon-6d-beginner.v1.json")));
        Assert.False(File.Exists(Path.Combine(CatalogRoot, "combinations", "half-marathon-6d-experienced.v1.json")));
        // Experienced exists as a RunningBackground enum member (product-wide, cross-distance),
        // but EXP.0 froze it PRODUCT_WIDE_EXPERIENCED_LEVEL_REMAINS_EXCLUDED -- confirmed here by
        // direct read: no VolumeSafetyPolicy dispatcher (HM or 10K, any frequency) has an
        // Experienced-scoped arm, and the public/dark HALF_MARATHON identity switch
        // (V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency's 3-arg overload) never mentions
        // RunningBackground.Experienced in any of its arms -- it is structurally unreachable, not
        // merely untested.
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Experienced, 6));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Experienced, 5));
        Assert.Null(V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Experienced, 6));
    }

    [Fact]
    public void HalfMarathonIntermediate6D_ExactPolicyEncoding()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate6D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(6));
        var policy = VolumeSafetyPolicy.HalfMarathonIntermediate6D;
        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(29.5d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(51.0d, policy.ResolvedPeakReference.Value);
        Assert.Equal(11, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.28d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.28d, policy.LongRunSelectionShare);
        Assert.Equal(0.36d, policy.LongRunHardCapShare);
        Assert.Equal(19.0d, policy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(12.0d, policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.TaperVolumeMultipliers);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);

        // Byte-identical to Intermediate 5D on every field except (trivially) the record identity
        // itself -- HM-X1.2's own band-width-stability zero-delta finding.
        var five = VolumeSafetyPolicy.HalfMarathonIntermediate5D;
        Assert.Equal(five.PreferredMaxWeeklyIncreaseRatio, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(five.AbsoluteWeeklyIncrementCapKm, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(five.GoldenFixtureStartingVolumeKm, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(five.ResolvedPeakReference.Value, policy.ResolvedPeakReference.Value);
        Assert.Equal(five.LongRunSelectionShare, policy.LongRunSelectionShare);
        Assert.Equal(five.LongRunHardCapShare, policy.LongRunHardCapShare);
        Assert.Equal(five.StartingAbsoluteLongRunCapKm, policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(five.TaperVolumeMultipliers, policy.TaperVolumeMultipliers);
    }

    [Fact]
    public void HalfMarathonAdvanced6D_ExactPolicyEncoding()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonAdvanced6D, VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek(6));
        var policy = VolumeSafetyPolicy.HalfMarathonAdvanced6D;
        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(36.5d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(63.0d, policy.ResolvedPeakReference.Value);
        Assert.Equal(11, policy.GoldenFixtureNonTaperTransitions);
        Assert.Equal(0.28d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.28d, policy.LongRunSelectionShare);
        Assert.Equal(0.36d, policy.LongRunHardCapShare);
        Assert.Equal(19.0d, policy.PreferredAbsolutePeakLongRunKm);
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.TaperVolumeMultipliers);
        Assert.True(policy.ResolvedPeakReferenceIsSelectedCeiling);
    }

    // ============================================================================================
    // Zero-delta regression: every existing HM cell and 10K's own 6D remain byte-identical.
    // ============================================================================================
    [Fact]
    public void ExistingHalfMarathonCells_ZeroDelta_AfterSixDayDispatchAddition()
    {
        Assert.Same(VolumeSafetyPolicy.HalfMarathonBeginner3D, VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonBeginner4D, VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(5));

        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate3D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate4D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonIntermediate5D, VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(5));
        Assert.Equal(34.0d, VolumeSafetyPolicy.HalfMarathonIntermediate3D.ResolvedPeakReference.Value);
        Assert.Equal(43.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value);
        Assert.Equal(51.0d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.ResolvedPeakReference.Value);

        Assert.Same(VolumeSafetyPolicy.HalfMarathonAdvanced3D, VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek(3));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonAdvanced4D, VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek(4));
        Assert.Same(VolumeSafetyPolicy.HalfMarathonAdvanced5D, VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek(5));
        Assert.Equal(42.0d, VolumeSafetyPolicy.HalfMarathonAdvanced3D.ResolvedPeakReference.Value);
        Assert.Equal(53.0d, VolumeSafetyPolicy.HalfMarathonAdvanced4D.ResolvedPeakReference.Value);
        Assert.Equal(62.0d, VolumeSafetyPolicy.HalfMarathonAdvanced5D.ResolvedPeakReference.Value);

        // Public gate is byte-identical: still the frozen 8-cell V1 matrix, HALF_MARATHON x6D
        // absent for both levels.
        foreach (var (level, days) in new[]
        {
            (RunningBackground.Beginner, 3), (RunningBackground.Beginner, 4),
            (RunningBackground.Intermediate, 3), (RunningBackground.Intermediate, 4), (RunningBackground.Intermediate, 5),
            (RunningBackground.Advanced, 3), (RunningBackground.Advanced, 4), (RunningBackground.Advanced, 5),
        })
        {
            Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.HalfMarathon, level, days));
        }
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.HalfMarathon, RunningBackground.Beginner, 5));
    }

    [Fact]
    public void TenKSixDay_ZeroDelta_AfterHalfMarathonSixDayAddition()
    {
        Assert.Same(VolumeSafetyPolicy.SixDayIntermediate, VolumeSafetyPolicy.ForIntermediateDaysPerWeek(6));
        Assert.Same(VolumeSafetyPolicy.Advanced6D, VolumeSafetyPolicy.ForAdvancedDaysPerWeek(6));
        Assert.Equal(44.5d, VolumeSafetyPolicy.SixDayIntermediate.ResolvedPeakReference.Value);
        Assert.Equal(51d, VolumeSafetyPolicy.Advanced6D.ResolvedPeakReference.Value);
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 6));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, 6));
    }

    // ============================================================================================
    // Intermediate 14W golden trace.
    // ============================================================================================
    [Fact]
    public async Task PreferredFourteenWeekCore_IntermediateSixDay_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync(RunningBackground.Intermediate);
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, RunningBackground.Intermediate, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;
        var dated = result.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(84, prescribed.Sessions.Count); // 14 weeks * 6 sessions
        Assert.All(prescribed.Weeks, w => Assert.Equal(6, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));

        // Structural: exactly 2 KEY_SESSION (never a 3rd) and exactly 3 EASY_SUPPORT (the one
        // slot 6D adds over 5D) every week; each non-degenerate (above the generic 1.5km floor).
        Assert.All(prescribed.Weeks, w => Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION")));
        Assert.All(prescribed.Weeks, w => Assert.Equal(3, w.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT")));
        Assert.All(prescribed.Weeks, w => Assert.All(
            w.Sessions.Where(s => s.StructuralRole == "EASY_SUPPORT"),
            easy => Assert.True(easy.Prescription.DistancePrescription.PlannedDistanceKm >= 1.5d - 0.001d,
                $"W{w.WeekNumber} EASY_SUPPORT session {easy.Prescription.DistancePrescription.PlannedDistanceKm}km is degenerate.")));

        // KEY1 (LaneOrdinal 0) primary progression is byte-identical to every other HM cell.
        Assert.Equal(new[]
        {
            "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE",
            "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL", "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION"
        }, prescribed.Weeks.Select(w => PrimaryKey(w).ProgressionStageKey).ToArray());

        // KEY2 (LaneOrdinal 1) stays light-quality FARTLEK in Build/RaceSpecific, EASY_STANDARD in
        // Foundation/Taper -- HM-X1.2 §3/§5 frozen structural authority. Never true-hard, never
        // HM_PACE.
        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "FOUNDATION" or "TAPER"),
            w => Assert.Equal("EASY_STANDARD", SecondaryKey(w).WorkoutDefinitionKey));
        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "BUILD" or "RACE_SPECIFIC"),
            w => Assert.Equal("FARTLEK", SecondaryKey(w).WorkoutDefinitionKey));
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "HM_PACE");
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "THRESHOLD_TEMPO");

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
        // 51.0 * 0.70 = 35.7 -> rounded to the 0.5km catalog grid = 35.5;
        // 51.0 * 0.43 = 21.93 -> rounded to the 0.5km catalog grid = 22.0.
        // Byte-identical to Intermediate 5D's own already-shipped taper trace (peak unchanged).
        Assert.Equal(35.5d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(22.0d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");

        _output.WriteLine("Intermediate 6D 14W golden table: Week|Phase|WeeklyVolume|KEY1|KEY2|EASY1-3|LONG|LRshare|TaperMult");
        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key1 = PrimaryKey(week);
            var key2 = SecondaryKey(week);
            var easies = week.Sessions.Where(s => s.StructuralRole == "EASY_SUPPORT").ToArray();
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            _output.WriteLine(
                $"Int6D W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}" +
                $"|key1={key1.WorkoutDefinitionKey}@{key1.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|key2={key2.WorkoutDefinitionKey}@{key2.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|easy=[{string.Join(',', easies.Select(e => e.Prescription.DistancePrescription.PlannedDistanceKm.ToString("F1")))}]" +
                $"|long={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}|lrShare={longRunWeek.LongRunShareOfWeeklyVolume:P2}");
        }

        // Calendar six-day proof: all 6 sessions land on distinct dates within the preferred set,
        // 1 non-running day (Saturday), and KEY-to-KEY/KEY-to-LONG spacing holds.
        foreach (var week in dated.Weeks)
        {
            var dates = week.SessionSlots.Select(s => s.SessionDate).Distinct().ToArray();
            Assert.Equal(6, dates.Length);
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
    }

    // ============================================================================================
    // Advanced 14W golden trace -- the genuinely novel cell: KEY2 becomes a real second
    // true-hard THRESHOLD_TEMPO stimulus in Build/RaceSpecific, never HM_PACE.
    // ============================================================================================
    [Fact]
    public async Task PreferredFourteenWeekCore_AdvancedSixDay_TraversesRealPipelineAndMaterializesValidDarkPreview()
    {
        var candidate = await CandidateAsync(RunningBackground.Advanced);
        Assert.Equal(new PlanCatalogCoreCycle(10, 14, 16), candidate.CoreCycle);
        var context = Context(candidate, RunningBackground.Advanced, exactPaceEvidence: true);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;
        var dated = result.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(new[] { 3, 4, 5, 2 }, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.Equal(14, prescribed.Weeks.Count);
        Assert.Equal(84, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(6, w.Sessions.Count));
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(prescribed.Weeks, w => Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION")));
        Assert.All(prescribed.Weeks, w => Assert.Equal(3, w.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT")));
        Assert.All(prescribed.Weeks, w => Assert.All(
            w.Sessions.Where(s => s.StructuralRole == "EASY_SUPPORT"),
            easy => Assert.True(easy.Prescription.DistancePrescription.PlannedDistanceKm >= 1.5d - 0.001d)));

        Assert.Equal(new[]
        {
            "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE", "FOUNDATION_AEROBIC_BASE",
            "BUILD_FASTER_THAN_HM_INTRO", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT", "BUILD_THRESHOLD_DEVELOPMENT",
            "RACE_SPECIFIC_HM_INTRODUCTION", "RACE_SPECIFIC_HM_DEVELOPMENT", "RACE_SPECIFIC_HM_DEVELOPMENT",
            "RACE_SPECIFIC_HM_REHEARSAL", "RACE_SPECIFIC_HM_REHEARSAL", "TAPER_HM_ACTIVATION", "TAPER_HM_ACTIVATION"
        }, prescribed.Weeks.Select(w => PrimaryKey(w).ProgressionStageKey).ToArray());

        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "FOUNDATION" or "TAPER"),
            w => Assert.Equal("EASY_STANDARD", SecondaryKey(w).WorkoutDefinitionKey));
        Assert.All(prescribed.Weeks.Where(w => w.PhaseKey is "BUILD" or "RACE_SPECIFIC"),
            w => Assert.Equal("THRESHOLD_TEMPO", SecondaryKey(w).WorkoutDefinitionKey));
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "HM_PACE");
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "FARTLEK");

        // Never a third hard stimulus: exactly 2 KEY_SESSION roles every week, and hard-spacing
        // (>=2 days) holds between both KEY dates every week.
        foreach (var week in prescribed.Weeks)
        {
            var keyDates = week.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").Select(s => s.Date).OrderBy(d => d).ToArray();
            Assert.Equal(2, keyDates.Length);
            Assert.True(Math.Abs(keyDates[1].DayNumber - keyDates[0].DayNumber) >= 2,
                $"Week {week.WeekNumber}: KEY_SESSION dates {keyDates[0]}/{keyDates[1]} are closer than the required 2-day minimum.");
        }

        // True-hard spacing at the calendar-hardness level (both KEY roles real dates).
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
                $"True-hard sessions {hardDates[i - 1]} and {hardDates[i]} violate the 2-day floor.");
        }

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid, string.Join("; ", volume.WeeklyVolumePlan.ValidationResult.Errors));
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid, string.Join("; ", volume.LongRunProgression.ValidationResult.Errors));
        Assert.Equal(63d, volume.WeeklyVolumePlan.PeakVolumeKm);
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.36d + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });
        var taper = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).ToArray();
        Assert.Equal(2, taper.Length);
        // 63.0 * 0.70 = 44.1 -> rounded to the 0.5km catalog grid = 44.0;
        // 63.0 * 0.43 = 27.09 -> rounded to the 0.5km catalog grid = 27.0.
        Assert.Equal(44.0d, taper[0].PlannedWeeklyVolumeKm);
        Assert.Equal(27.0d, taper[1].PlannedWeeklyVolumeKm);
        Assert.True(taper[0].PlannedWeeklyVolumeKm >= taper[1].PlannedWeeklyVolumeKm);

        // Two-true-hard-same-week dose feasibility: both KEY1/KEY2 THRESHOLD_TEMPO sessions are
        // real, distinguishable, non-degenerate volumes.
        foreach (var week in prescribed.Weeks.Where(w =>
            w.PhaseKey is "BUILD" or "RACE_SPECIFIC" && PrimaryKey(w).WorkoutDefinitionKey == "THRESHOLD_TEMPO"))
        {
            var key1 = PrimaryKey(week);
            var key2 = SecondaryKey(week);
            var d1 = key1.Prescription.DistancePrescription.PlannedDistanceKm;
            var d2 = key2.Prescription.DistancePrescription.PlannedDistanceKm;
            Assert.True(d1 >= 3.0d, $"W{week.WeekNumber} KEY1 THRESHOLD_TEMPO volume {d1} is degenerate.");
            Assert.True(d2 >= 3.0d, $"W{week.WeekNumber} KEY2 THRESHOLD_TEMPO volume {d2} is degenerate.");
        }

        _output.WriteLine("Advanced 6D 14W golden table: Week|Phase|WeeklyVolume|KEY1|KEY2|EASY1-3|LONG|LRshare|TrueHardCount");
        foreach (var week in prescribed.Weeks)
        {
            var volumeWeek = volume.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRunWeek = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key1 = PrimaryKey(week);
            var key2 = SecondaryKey(week);
            var easies = week.Sessions.Where(s => s.StructuralRole == "EASY_SUPPORT").ToArray();
            var longRun = week.Sessions.Single(s => s.StructuralRole == "LONG_RUN");
            var trueHardCount = new[] { key1, key2 }.Count(k => k.WorkoutDefinitionKey is "THRESHOLD_TEMPO" or "HM_PACE");
            _output.WriteLine(
                $"Adv6D W{week.WeekNumber:D2}|{week.PhaseKey}|vol={volumeWeek.PlannedWeeklyVolumeKm:F1}" +
                $"|key1={key1.WorkoutDefinitionKey}@{key1.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|key2={key2.WorkoutDefinitionKey}@{key2.Prescription.DistancePrescription.PlannedDistanceKm:F1}" +
                $"|easy=[{string.Join(',', easies.Select(e => e.Prescription.DistancePrescription.PlannedDistanceKm.ToString("F1")))}]" +
                $"|long={longRun.Prescription.DistancePrescription.PlannedDistanceKm:F1}|lrShare={longRunWeek.LongRunShareOfWeeklyVolume:P2}|trueHard={trueHardCount}");
        }

        var materialized = new CatalogPublicPreviewMaterializer().Materialize(new CatalogPublicPreviewMaterializationRequest(
            context.PreviewRequest, candidate, context.StartDate, context.AsOfDate, volume, prescribed));
        Assert.True(materialized.ValidationResult.IsValid);
        Assert.Equal(14, materialized.Payload.PlannedWeekCount);
    }

    // ============================================================================================
    // 10-16W full dark materialization for both levels.
    // ============================================================================================
    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_IntermediateSixDay_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks) =>
        await AssertHorizon(RunningBackground.Intermediate, targetWeekCount, expectedPhaseWeeks, peak: 51d, lrHardCap: 0.36d);

    [Theory]
    [InlineData(10, new[] { 2, 3, 3, 2 })]
    [InlineData(11, new[] { 2, 3, 4, 2 })]
    [InlineData(12, new[] { 2, 3, 5, 2 })]
    [InlineData(13, new[] { 2, 4, 5, 2 })]
    [InlineData(14, new[] { 3, 4, 5, 2 })]
    [InlineData(15, new[] { 4, 4, 5, 2 })]
    [InlineData(16, new[] { 4, 5, 5, 2 })]
    public async Task NonPreferredCoreLengths_AdvancedSixDay_TraverseRealPipelineAndMaterializeValidDarkPreviews(
        int targetWeekCount, int[] expectedPhaseWeeks) =>
        await AssertHorizon(RunningBackground.Advanced, targetWeekCount, expectedPhaseWeeks, peak: 63d, lrHardCap: 0.36d);

    private async Task AssertHorizon(RunningBackground level, int targetWeekCount, int[] expectedPhaseWeeks, double peak, double lrHardCap)
    {
        var candidate = await CandidateAsync(level);

        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.True(allocation.IsMathematicallyFeasible, allocation.ReasonCode);
        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, allocation.Phases.Select(p => p.PhaseKey));
        Assert.Equal(expectedPhaseWeeks, allocation.Phases.Select(p => p.AllocatedWeeks));
        Assert.Equal(2, allocation.Phases.Single(p => p.PhaseKey == "TAPER").AllocatedWeeks);

        var result = await Pipeline().MaterializeAsync(Context(candidate, level, exactPaceEvidence: true, targetWeekCount));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var bound = result.PrescriptionResult.VolumeResult.BindingResult.BoundPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(targetWeekCount, prescribed.Weeks.Count);
        Assert.Equal(targetWeekCount * 6, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, w => Assert.Equal(6, w.Sessions.Count));
        Assert.Equal(expectedPhaseWeeks, prescribed.Weeks.GroupBy(w => w.PhaseKey).Select(g => g.Count()).ToArray());
        Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)));
        Assert.All(prescribed.Weeks, w => Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION")));
        Assert.All(prescribed.Weeks, w => Assert.Equal(3, w.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT")));
        Assert.All(prescribed.Weeks, w => Assert.All(
            w.Sessions.Where(s => s.StructuralRole == "EASY_SUPPORT"),
            easy => Assert.True(easy.Prescription.DistancePrescription.PlannedDistanceKm >= 1.5d - 0.001d)));

        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "HM_PACE");
        foreach (var week in prescribed.Weeks)
        {
            var keyDates = week.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").Select(s => s.Date).OrderBy(d => d).ToArray();
            Assert.Equal(2, keyDates.Length);
            Assert.True(Math.Abs(keyDates[1].DayNumber - keyDates[0].DayNumber) >= 2);
        }

        Assert.True(volume.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(volume.LongRunProgression.ValidationResult.IsValid);
        // HM.5.3 invariant: short horizons may safely stay below the selected peak reference;
        // never extrapolate above it, at any horizon.
        Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= peak + 0.001d,
            $"level={level} targetWeekCount={targetWeekCount}: PeakVolumeKm={volume.WeeklyVolumePlan.PeakVolumeKm} exceeds the {peak}km reference ceiling.");
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.LongRunShareOfWeeklyVolume <= lrHardCap + 0.001d);
            Assert.True(w.PlannedLongRunDistanceKm <= 19d + 0.001d);
        });

        const double absoluteCap = 2.5d;
        var orderedVolumeWeeks = volume.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        for (var i = 1; i < orderedVolumeWeeks.Length; i++)
        {
            var prev = orderedVolumeWeeks[i - 1];
            var cur = orderedVolumeWeeks[i];
            if (cur.IsTaperWeek || prev.IsTaperWeek) continue;
            var deltaKm = cur.PlannedWeeklyVolumeKm - prev.PlannedWeeklyVolumeKm;
            Assert.True(deltaKm <= absoluteCap + 0.001d,
                $"level={level} targetWeekCount={targetWeekCount}: W{prev.WeekNumber}->W{cur.WeekNumber} deltaKm={deltaKm:F2} exceeds the {absoluteCap}km absolute weekly-increase cap.");
        }

        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm,
            $"Taper must be non-chained/non-increasing for every horizon (week1={taperWeeks[0].PlannedWeeklyVolumeKm}, week2={taperWeeks[1].PlannedWeeklyVolumeKm}).");
        Assert.True(prescribed.Weeks.Last(w => w.PhaseKey == "TAPER").Sessions.All(
            s => s.Prescription.DistancePrescription.PlannedDistanceKm >= 0));

        _output.WriteLine(
            $"{level}6D-{targetWeekCount}|vol=[{string.Join(',', volume.WeeklyVolumePlan.Weeks.Select(w => w.PlannedWeeklyVolumeKm.ToString("F1")))}]" +
            $"|peak={volume.WeeklyVolumePlan.PeakVolumeKm:F1}|taper={taperWeeks[0].PlannedWeeklyVolumeKm:F1}/{taperWeeks[1].PlannedWeeklyVolumeKm:F1}");

        // HM-X1.4 superseded this dark-only assertion: HALF_MARATHON x6D Intermediate/Advanced
        // are now the exact, sole, intentional public-activation scope of HM-X1.4 -- this is not
        // a regression in HM-X1.3, it is the planned next step HM-X1.3 §38 explicitly
        // anticipated. See HmX14IntermediateAdvancedSixDayPublicActivationTests for the real
        // HTTP/PostgreSQL public-activation proof.
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.HalfMarathon, level, 6));
    }

    // Boundary proof: 9W/17W remain infeasible for the low-level infeasibility signal (mirroring
    // the exact established HM-dark-phase pattern, e.g. Hm16's own OutOfCanonicalRangeHorizons_RemainInfeasible).
    // Separately: RaceHorizonPolicy.DecideForDistance/Classify (PlanServices.GeneratePreviewAsync's
    // own real HALF_MARATHON 9W/17W typed-exception gate, PlanCoreHorizonTooShortException /
    // PlanHorizonStartTooEarlyException, PLAN_CORE_HORIZON_TOO_SHORT / PLAN_HORIZON_START_TOO_EARLY)
    // takes ONLY (DateOnly startDate, DateOnly raceDate, GoalDistance distance) as parameters --
    // no Level, no DaysPerWeek -- and runs BEFORE any candidate/routing/identity resolution in
    // PlanServices.GeneratePreviewAsync (see PlanServices.cs lines 130-207). This is a direct,
    // provable-by-signature guarantee that the 9W/17W typed rejection for HALF_MARATHON is
    // completely level/frequency-independent: it fires identically for 6D as it already does for
    // every other frequency, with zero new code required and zero risk of Standard-Core-only
    // Runway/LongHorizon behavior being accidentally wired for 6D by this phase.
    [Theory]
    [InlineData(9)]
    [InlineData(17)]
    public async Task OutOfCanonicalRangeHorizons_IntermediateSixDay_RemainInfeasible(int targetWeekCount)
    {
        var candidate = await CandidateAsync(RunningBackground.Intermediate);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Empty(allocation.Phases);
        await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() =>
            Pipeline().MaterializeAsync(Context(candidate, RunningBackground.Intermediate, exactPaceEvidence: true, targetWeekCount)));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(17)]
    public async Task OutOfCanonicalRangeHorizons_AdvancedSixDay_RemainInfeasible(int targetWeekCount)
    {
        var candidate = await CandidateAsync(RunningBackground.Advanced);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, targetWeekCount);
        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Empty(allocation.Phases);
        await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() =>
            Pipeline().MaterializeAsync(Context(candidate, RunningBackground.Advanced, exactPaceEvidence: true, targetWeekCount)));
    }

    // ============================================================================================
    // Calibration safety: missing/explicit-zero readiness never substitutes the calibration-only
    // starting volume -- fails closed identically to every other HM cell.
    // ============================================================================================
    [Theory]
    [InlineData(RunningBackground.Intermediate)]
    [InlineData(RunningBackground.Advanced)]
    public async Task MissingWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume(RunningBackground level)
    {
        var context = Context(await CandidateAsync(level), level, exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = null;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() => Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    [Theory]
    [InlineData(RunningBackground.Intermediate)]
    [InlineData(RunningBackground.Advanced)]
    public async Task ZeroWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume(RunningBackground level)
    {
        var context = Context(await CandidateAsync(level), level, exactPaceEvidence: false);
        context.PreviewRequest.RecentWeeklyVolumeKm = 0;

        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() => Pipeline().MaterializeAsync(context));
        var cause = Assert.IsType<CatalogVolumeInvalidReadinessInputException>(wrapper.InnerException);
        Assert.Contains("no approved missing/zero starting-volume fallback", cause.Message);
    }

    // ============================================================================================
    // Pace/fallback proof.
    // ============================================================================================
    [Theory]
    [InlineData(RunningBackground.Intermediate)]
    [InlineData(RunningBackground.Advanced)]
    public async Task ExactPaceEvidence_BindsHmPaceForKey1Only(RunningBackground level)
    {
        var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(level), level, exactPaceEvidence: true));
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;
        Assert.Contains(prescribed.Sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
        Assert.True(prescribed.ValidationResult.IsValid);
        Assert.DoesNotContain(prescribed.Weeks, w => SecondaryKey(w).WorkoutDefinitionKey == "HM_PACE");
        Assert.All(prescribed.Weeks, w => Assert.Equal(0, w.Sessions.Count(s => s.LaneOrdinal == 1 && s.WorkoutDefinitionKey == "HM_PACE")));
    }

    [Theory]
    [InlineData(RunningBackground.Intermediate)]
    [InlineData(RunningBackground.Advanced)]
    public async Task MissingIndependentExactPaceEvidence_FallsBackAndNeverFabricatesFromDesiredFinishTime(RunningBackground level)
    {
        foreach (var targetWeekCount in new[] { 10, 11, 12, 13, 14, 15, 16 })
        {
            var result = await Pipeline().MaterializeAsync(Context(await CandidateAsync(level), level, exactPaceEvidence: false, targetWeekCount));
            var sessions = result.PrescriptionResult.FinalPrescribedPlan.Sessions;
            Assert.DoesNotContain(sessions, s => s.WorkoutDefinitionKey == "HM_PACE");
            Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
        }
    }

    // ============================================================================================
    // Adaptation smoke tests.
    // ============================================================================================
    [Theory]
    [InlineData(RunningBackground.Intermediate)]
    [InlineData(RunningBackground.Advanced)]
    public async Task AdaptationSmoke_AllComplete_ProducesConservativeDecisions(RunningBackground level)
    {
        var prescribed = (await Pipeline().MaterializeAsync(Context(await CandidateAsync(level), level, exactPaceEvidence: true)))
            .PrescriptionResult.FinalPrescribedPlan;
        var buildWeek = prescribed.Weeks.First(week => week.PhaseKey == "BUILD");
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, AdaptationDecision(buildWeek, missed: null));
    }

    [Theory]
    [InlineData(RunningBackground.Intermediate)]
    [InlineData(RunningBackground.Advanced)]
    public async Task AdaptationSmoke_EachRoleMissed_IsConservative(RunningBackground level)
    {
        var prescribed = (await Pipeline().MaterializeAsync(Context(await CandidateAsync(level), level, exactPaceEvidence: true)))
            .PrescriptionResult.FinalPrescribedPlan;
        var buildWeek = prescribed.Weeks.First(week => week.PhaseKey == "BUILD");
        var primary = PrimaryKey(buildWeek);
        var secondary = SecondaryKey(buildWeek);
        var easy = buildWeek.Sessions.First(s => s.StructuralRole == "EASY_SUPPORT");
        var longRun = buildWeek.Sessions.Single(s => s.StructuralRole == "LONG_RUN");

        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, primary));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, secondary));
        Assert.Equal(NextWindowLoadDecision.Maintain, AdaptationDecision(buildWeek, longRun));
        Assert.Equal(NextWindowLoadDecision.ProgressAsPlanned, AdaptationDecision(buildWeek, easy));
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

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync(RunningBackground level)
    {
        var (key, version) = level switch
        {
            RunningBackground.Intermediate => (V1CatalogPilotIdentityPolicy.HalfMarathonSixDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonSixDayIntermediateCandidateVersion),
            RunningBackground.Advanced => (V1CatalogPilotIdentityPolicy.HalfMarathonSixDayAdvancedCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonSixDayAdvancedCandidateVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(level)),
        };
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
        RunningBackground level,
        bool exactPaceEvidence,
        int targetWeekCount = 14)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var goalKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon);
        // Golden-fixture RecentWeeklyVolumeKm equals each policy's own GoldenFixtureStartingVolumeKm
        // (29.5/36.5), so the 14W preferred-horizon test resolves cleanly to the selected peak
        // with no residual scaling.
        var recentWeeklyVolumeKm = level == RunningBackground.Intermediate ? 29.5 : 36.5;
        var recentLongestRunKm = level == RunningBackground.Intermediate ? 12d : 16d;
        // 6 running days + Saturday rest, mirroring 10K's own live 6D preferred-day precedent.
        var preferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Wed, Weekday.Thu, Weekday.Fri, Weekday.Sun };
        var preferredDaysOfWeek = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday,
        };

        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.HalfMarathon, Level = level,
            DaysPerWeek = 6, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = exactPaceEvidence ? 5700 : null,
            PreferredDays = preferredDays, LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = recentWeeklyVolumeKm, RecentLongestRunKm = recentLongestRunKm, RecentRunsPerWeek = 6,
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
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds, DaysPerWeek = 6, Level = level,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }
}
