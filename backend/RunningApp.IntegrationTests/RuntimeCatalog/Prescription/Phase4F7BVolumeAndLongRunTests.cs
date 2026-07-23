using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription;

public sealed class Phase4F7BVolumeAndLongRunTests
{
    private static readonly DateOnly AsOfDate = new(2026, 7, 15);

    [Fact]
    public void DefaultTwelveWeekPlan_GeneratesOneWeeklyVolumeAndLongRunPerWeek()
    {
        var plan = Build(12);

        Assert.Equal(12, plan.WeeklyVolumePlan.Weeks.Count);
        Assert.Equal(12, plan.LongRunProgression.Weeks.Count);
        Assert.Equal(24, plan.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.NotEqual(42, plan.WeeklyVolumePlan.PeakVolumeKm);
        Assert.Equal(PeakBandClassification.WithinTypicalPeakBand, plan.WeeklyVolumePlan.ReachablePeakDecision.Classification);
        Assert.All(plan.WeeklyVolumePlan.Weeks, w => Assert.InRange(w.PlannedWeeklyVolumeKm, 0, 42));
        Assert.All(plan.LongRunProgression.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm <= w.PlannedWeeklyVolumeKm));
        Assert.True(plan.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(plan.LongRunProgression.ValidationResult.IsValid);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(18)]
    [InlineData(20)]
    public void RecentWeeklyVolume_BelowTypicalPeakBand_IsNotRaisedToThirty(double reported)
    {
        var plan = Build(12, r => r.RecentWeeklyVolumeKm = reported);

        Assert.Equal(reported, plan.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.Equal(CatalogVolumeClamp.None, plan.WeeklyVolumePlan.Weeks[0].AppliedClamp);
        Assert.Equal(WeeklyVolumeAnchorSource.RecentFourWeekAverage, plan.WeeklyVolumePlan.Weeks[0].AnchorSource);
    }

    [Fact]
    public void MissingAndExplicitZeroRecentVolume_DoNotUsePeakBandMinimumFallback()
    {
        var missing = Build(12, r => r.RecentWeeklyVolumeKm = null);
        var zero = Build(12, r => r.RecentWeeklyVolumeKm = 0);

        Assert.Equal(16, missing.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.Equal(12, zero.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.NotEqual(30, missing.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.NotEqual(30, zero.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.Equal(WeeklyVolumeAnchorSource.LevelConservativeDefault, missing.WeeklyVolumePlan.StartingVolumeDecision.AnchorSource);
        Assert.Equal(WeeklyVolumeAnchorSource.NoRecentRunningDefault, zero.WeeklyVolumePlan.StartingVolumeDecision.AnchorSource);
    }

    [Fact]
    public void InvalidRecentVolume_ThrowsTypedFailure()
    {
        Assert.Throws<CatalogVolumeInvalidReadinessInputException>(() => Build(12, r => r.RecentWeeklyVolumeKm = -1));
    }

    [Fact]
    public void WeeklyCurve_IsDeterministicProgressiveAndTaperReduced()
    {
        var first = Build(12);
        var second = Build(12);

        Assert.Equal(first.WeeklyVolumePlan.Weeks.Select(w => w.PlannedWeeklyVolumeKm), second.WeeklyVolumePlan.Weeks.Select(w => w.PlannedWeeklyVolumeKm));
        var nonTaper = first.WeeklyVolumePlan.Weeks.Where(w => !w.IsTaperWeek).ToList();
        Assert.All(nonTaper.Zip(nonTaper.Skip(1)), p => Assert.True(p.Second.PlannedWeeklyVolumeKm >= p.First.PlannedWeeklyVolumeKm));
        var taper = first.WeeklyVolumePlan.Weeks.Single(w => w.IsTaperWeek);
        var preTaper = first.WeeklyVolumePlan.Weeks.Last(w => !w.IsTaperWeek);
        Assert.True(taper.PlannedWeeklyVolumeKm < preTaper.PlannedWeeklyVolumeKm);
        Assert.All(first.WeeklyVolumePlan.Weeks, w => Assert.False(w.IsRecoveryOrDeloadWeek));
    }

    [Fact]
    public void LongRunAnchors_ValidMissingZeroAndInconsistentInputsAreHandledSafely()
    {
        var valid = Build(12, r => r.RecentLongestRunKm = 14);
        Assert.Equal(LongRunAnchorSource.Recent30DayLongestRun, valid.LongRunProgression.Weeks[0].LongRunAnchorSource);
        Assert.True(valid.LongRunProgression.Weeks[0].PlannedLongRunDistanceKm <= 14);

        var missing = Build(12, r => r.RecentLongestRunKm = null);
        Assert.Equal(LongRunAnchorSource.WeeklyVolumeDerived, missing.LongRunProgression.Weeks[0].LongRunAnchorSource);

        var zero = Build(12, r => r.RecentLongestRunKm = 0);
        Assert.Equal(LongRunAnchorSource.WeeklyVolumeDerived, zero.LongRunProgression.Weeks[0].LongRunAnchorSource);
        Assert.True(zero.LongRunProgression.Weeks[0].PlannedLongRunDistanceKm > 0);

        var inconsistent = Build(12, r =>
        {
            r.RecentWeeklyVolumeKm = 20;
            r.RecentLongestRunKm = 25;
        });
        var week1 = inconsistent.LongRunProgression.Weeks[0];
        Assert.Equal(PrescriptionInputState.Inconsistent, week1.RecentLongestRunState);
        Assert.Equal(LongRunAnchorSource.WeeklyVolumeDerived, week1.LongRunAnchorSource);
        Assert.Equal(CatalogVolumeClamp.ConservativeClamp, week1.CompatibilityClamp);
        Assert.NotEqual(25, week1.PlannedLongRunDistanceKm);
    }

    [Fact]
    public void LongRunProgression_RespectsShareBoundsAndTaperReduction()
    {
        var plan = Build(12);

        Assert.Equal(0.30, plan.LongRunProgression.WeeklyShareDecision.PreferredMinimumShare);
        Assert.Equal(0.36, plan.LongRunProgression.WeeklyShareDecision.PreferredMaximumShare);
        Assert.Equal(0.40, plan.LongRunProgression.WeeklyShareDecision.HardCapShare);
        Assert.All(plan.LongRunProgression.Weeks, w => Assert.InRange(w.LongRunShareOfWeeklyVolume, 0.30, 0.40));
        var taper = plan.LongRunProgression.Weeks.Last();
        var preTaper = plan.LongRunProgression.Weeks[^2];
        Assert.True(taper.PlannedLongRunDistanceKm < preTaper.PlannedLongRunDistanceKm);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(14)]
    public void SupportedCycleLengths_GenerateForEveryBoundWeek(int weeks)
    {
        var plan = Build(weeks);

        Assert.Equal(weeks, plan.WeeklyVolumePlan.Weeks.Count);
        Assert.Equal(weeks, plan.LongRunProgression.Weeks.Count);
        Assert.Equal(weeks, plan.WeeklyVolumePlan.Weeks.Select(w => w.WeekNumber).Distinct().Count());
    }

    [Fact]
    public void ReachablePeak_BelowTypicalBand_IsValidAndClassified()
    {
        var plan = Build(8, r => r.RecentWeeklyVolumeKm = 14);

        Assert.True(plan.WeeklyVolumePlan.PeakVolumeKm < 30);
        Assert.Equal(PeakBandClassification.BelowTypicalPeakButValid, plan.WeeklyVolumePlan.ReachablePeakDecision.Classification);
        Assert.All(plan.WeeklyVolumePlan.Weeks.Where(w => !w.IsTaperWeek), w => Assert.True(w.PlannedWeeklyVolumeKm <= plan.WeeklyVolumePlan.PeakVolumeKm));
    }

    [Fact]
    public void TaperMultiplier_IsCanonicalAndNotSupersededValue()
    {
        var plan = Build(12);

        Assert.Equal(0.53, plan.WeeklyVolumePlan.TaperVolumeDecision.Multiplier);
        Assert.InRange(plan.WeeklyVolumePlan.TaperVolumeDecision.ReductionPercent, 0.41, 0.60);
        Assert.DoesNotContain("0.65", string.Join(" ", plan.WeeklyVolumePlan.DecisionTrace.Select(t => t.ChangeRule)));
    }

    [Fact]
    public void UnsupportedCycleLength_ThrowsTypedException()
    {
        Assert.Throws<CatalogVolumeUnsupportedCycleLengthException>(() => Build(15));
    }

    [Fact]
    public void OutputHasNoPaceDurationSegmentRecoveryOrSessionPrescriptionFields()
    {
        var types = new[] { typeof(CatalogWeeklyVolumeWeek), typeof(CatalogLongRunWeek), typeof(CatalogWeeklyVolumePlan), typeof(CatalogLongRunProgression) };
        var forbidden = new[] { "Pace", "Duration", "Segment", "Repetition", "RecoverySeconds", "EasySupportDistance", "KeySessionDistance" };
        foreach (var type in types)
        {
            var names = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(p => p.Name).ToList();
            foreach (var fragment in forbidden)
            {
                Assert.DoesNotContain(names, n => n.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void InputOrderIndependence_AndRaceSpecificZeroSlack_DoNotAffectNumericGeneration()
    {
        var plan = Build(12);
        var reversed = Build(12, boundPlan: BoundPlan(12, reverseWeeks: true));

        Assert.Equal(plan.WeeklyVolumePlan.Weeks.Select(w => (w.WeekNumber, w.PlannedWeeklyVolumeKm)), reversed.WeeklyVolumePlan.Weeks.Select(w => (w.WeekNumber, w.PlannedWeeklyVolumeKm)));
        Assert.Contains(plan.WeeklyVolumePlan.Weeks, w => w.PhaseKey == "RACE_SPECIFIC" && w.PlannedWeeklyVolumeKm > 0);
    }

    [Theory]
    [InlineData(null, 16, "LevelConservativeDefault")]
    [InlineData(0.0, 12, "NoRecentRunningDefault")]
    public void Phase4F7B2_MissingAndZeroWeeklyVolume_FollowVersionedPolicy(double? weeklyVolume, double expectedStart, string expectedSource)
    {
        var plan = Build(12, r => r.RecentWeeklyVolumeKm = weeklyVolume);

        Assert.Equal(expectedStart, plan.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.Equal(expectedSource, plan.WeeklyVolumePlan.StartingVolumeDecision.AnchorSource.ToString());
        Assert.Equal(CatalogEvidenceBasis.ProductPracticeInformed, plan.WeeklyVolumePlan.StartingVolumeDecision.EvidenceBasis);
        Assert.Equal(CatalogDecisionStatus.ExplicitProductDefault, plan.WeeklyVolumePlan.StartingVolumeDecision.DecisionStatus);
        Assert.Contains(V1MissingReadinessStartingVolumePolicy.PolicyKey, plan.WeeklyVolumePlan.StartingVolumeDecision.Provenance);
        Assert.Equal("INTERMEDIATE", Candidate().Level);
    }

    [Theory]
    [InlineData(8, null, 16, 21.5)]
    [InlineData(12, null, 16, 25.5)]
    [InlineData(14, null, 16, 27.0)]
    [InlineData(8, 0.0, 12, 16.0)]
    [InlineData(12, 0.0, 12, 19.0)]
    [InlineData(14, 0.0, 12, 20.5)]
    public void Phase4F7B2_MissingAndZeroWeeklyVolume_AreFeasibleAcrossSupportedCycles(
        int weeks,
        double? weeklyVolume,
        double expectedStart,
        double expectedPeak)
    {
        var plan = Build(weeks, r => r.RecentWeeklyVolumeKm = weeklyVolume);
        var week1 = plan.WeeklyVolumePlan.Weeks[0];
        var longRun1 = plan.LongRunProgression.Weeks[0];

        Assert.Equal(expectedStart, week1.PlannedWeeklyVolumeKm);
        Assert.Equal(expectedPeak, plan.WeeklyVolumePlan.PeakVolumeKm);
        Assert.Equal(PeakBandClassification.BelowTypicalPeakButValid, plan.WeeklyVolumePlan.ReachablePeakDecision.Classification);
        Assert.InRange(longRun1.LongRunShareOfWeeklyVolume, 0.30, 0.36);
        Assert.True(longRun1.PlannedLongRunDistanceKm <= longRun1.PlannedWeeklyVolumeKm * 0.40);
        Assert.True(week1.PlannedWeeklyVolumeKm - longRun1.PlannedLongRunDistanceKm > 0);
        Assert.Equal(4, BoundPlan(weeks).Weeks[0].Sessions.Count);
    }

    [Fact]
    public void Phase4F7B2_ValidPositiveAndInvalidReadiness_BehaviorRemainsUnchanged()
    {
        var valid = Build(12, r => r.RecentWeeklyVolumeKm = 14);
        Assert.Equal(14, valid.WeeklyVolumePlan.FirstWeekVolumeKm);
        Assert.Equal(WeeklyVolumeAnchorSource.RecentFourWeekAverage, valid.WeeklyVolumePlan.StartingVolumeDecision.AnchorSource);

        Assert.Throws<CatalogVolumeInvalidReadinessInputException>(() => Build(12, r => r.RecentWeeklyVolumeKm = -1));
    }

    [Fact]
    public void Phase4F7B2_NoPacePublicPayloadConfirmPersistenceOrSessionAllocationChanges()
    {
        var plan = Build(12, r => r.RecentWeeklyVolumeKm = null);
        var forbidden = new[] { "Pace", "Duration", "Segment", "Repetition", "RecoverySeconds", "EasySupportDistance", "KeySessionDistance" };

        foreach (var type in new[] { typeof(CatalogWeeklyVolumeWeek), typeof(CatalogLongRunWeek), typeof(CatalogWeeklyVolumePlan), typeof(CatalogLongRunProgression) })
        {
            var names = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(p => p.Name).ToList();
            foreach (var fragment in forbidden)
            {
                Assert.DoesNotContain(names, n => n.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }

        Assert.True(plan.WeeklyVolumePlan.ValidationResult.IsValid);
        Assert.True(plan.LongRunProgression.ValidationResult.IsValid);
    }

    [Fact]
    public void Phase4F7C_SessionPrescription_CreatesOnePrescriptionPerBoundSession_AndReconcilesWeeklyDistance()
    {
        var prescribed = BuildPrescribed(12);

        Assert.Equal(48, prescribed.Sessions.Count);
        Assert.All(prescribed.Weeks, week =>
        {
            Assert.Equal(4, week.Sessions.Count);
            Assert.Equal(week.PlannedWeeklyVolumeKm, week.AccountedWeeklyDistanceKm);
        });
        Assert.True(prescribed.ValidationResult.IsValid);
    }

    [Theory]
    [InlineData(null, 16)]
    [InlineData(0.0, 12)]
    [InlineData(24.0, 24)]
    public void Phase4F7C_LowVolumeAndFixtureStarts_RemainFeasible(double? weeklyVolume, double expectedFirstWeek)
    {
        var prescribed = BuildPrescribed(12, r => r.RecentWeeklyVolumeKm = weeklyVolume);
        var week1 = prescribed.Weeks[0];

        Assert.Equal(expectedFirstWeek, week1.PlannedWeeklyVolumeKm);
        Assert.Equal(4, week1.Sessions.Count);
        Assert.All(week1.Sessions, s => Assert.True(s.PlannedDistanceKm > 0));
        Assert.Equal(week1.PlannedWeeklyVolumeKm, week1.Sessions.Sum(s => s.PlannedDistanceKm));
    }

    [Fact]
    public void Phase4F7C_LongRunDistance_IsPreservedFrom4F7B()
    {
        var volume = Build(12);
        var prescribed = BuildPrescribed(12);

        Assert.Equal(
            volume.LongRunProgression.Weeks.Select(w => w.PlannedLongRunDistanceKm),
            prescribed.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "LONG_RUN").PlannedDistanceKm));
        Assert.All(prescribed.Weeks, w => Assert.True(w.Sessions.Single(s => s.StructuralRole == "LONG_RUN").PlannedDistanceKm <= w.PlannedWeeklyVolumeKm * 0.40));
    }

    [Fact]
    public void Phase4F7C_EasyAndLongRunPrescriptions_UseEffortOnlyDistanceFirstBaseline()
    {
        var prescribed = BuildPrescribed(12);
        var easy = prescribed.Sessions.First(s => s.StructuralRole == "EASY_SUPPORT");
        var longRun = prescribed.Sessions.First(s => s.StructuralRole == "LONG_RUN");

        Assert.Equal(CatalogPrescriptionMode.Distance, easy.Prescription.PrescriptionMode);
        Assert.Equal(CatalogDistanceAccountingMode.ExactSessionTotal, easy.Prescription.DistanceAccountingMode);
        Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, easy.Prescription.PacePrescription.Kind);
        Assert.Equal("EASY", easy.Prescription.EffortGuidance);
        Assert.Equal(CatalogDurationKind.Unresolved, easy.Prescription.DurationPrescription.Kind);
        Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, longRun.Prescription.PacePrescription.Kind);
        Assert.Equal("LONG_RUN_EASY_CONTROLLED", longRun.Prescription.EffortGuidance);
    }

    [Fact]
    public void Phase4F7C_FartlekAndThreshold_PreserveCatalogComponentOrderWithoutInventedExactPace()
    {
        var fartlek = BuildPrescribed(12, boundPlan: BoundPlan(12, keyWorkoutKey: "FARTLEK", keyWorkoutVersion: 4))
            .Sessions.First(s => s.WorkoutDefinitionKey == "FARTLEK");
        var threshold = BuildPrescribed(12, boundPlan: BoundPlan(12, keyWorkoutKey: "THRESHOLD_TEMPO", keyWorkoutVersion: 4))
            .Sessions.First(s => s.WorkoutDefinitionKey == "THRESHOLD_TEMPO");

        Assert.Equal(new[] { "WARM_UP", "MAIN_SET", "RECOVERY", "COOL_DOWN" }, fartlek.Prescription.OrderedSegments.Select(s => s.ComponentType));
        Assert.Equal(new[] { "WARM_UP", "MAIN_SET", "COOL_DOWN" }, threshold.Prescription.OrderedSegments.Select(s => s.ComponentType));
        Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, fartlek.Prescription.PacePrescription.Kind);
        Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, threshold.Prescription.PacePrescription.Kind);
        Assert.Equal(fartlek.PlannedDistanceKm, fartlek.Prescription.OrderedSegments.Sum(s => s.DistanceKm));
        Assert.Equal(threshold.PlannedDistanceKm, threshold.Prescription.OrderedSegments.Sum(s => s.DistanceKm));
    }

    [Fact]
    public void Phase4F7C_GoalPaceTenK_RequiresFeasibleGoalAndDerivesExactPace()
    {
        var prescribed = BuildPrescribed(12, boundPlan: BoundPlan(12, keyWorkoutKey: "GOAL_PACE_TEN_K", keyWorkoutVersion: 2));
        var goal = prescribed.Sessions.First(s => s.WorkoutDefinitionKey == "GOAL_PACE_TEN_K");

        Assert.Equal(CatalogPacePrescriptionKind.ExactPace, goal.Prescription.PacePrescription.Kind);
        Assert.Equal(300, goal.Prescription.PacePrescription.SecondsPerKilometer);
        Assert.Equal(CatalogPaceSourceSelection.TargetGoalDerived, goal.Prescription.PacePrescription.Source);

        Assert.Throws<CatalogGoalPacePrescriptionUnsupportedException>(() => BuildPrescribed(
            12,
            boundPlan: BoundPlan(12, keyWorkoutKey: "GOAL_PACE_TEN_K", keyWorkoutVersion: 2),
            goalFeasibility: "UNSUPPORTED"));
    }

    [Fact]
    public void Phase4F7C_TaperSharpen_BaselinePendingAndIdentityPreserved()
    {
        var prescribed = BuildPrescribed(12);
        var taperKey = prescribed.Sessions.Single(s => s.PhaseKey == "TAPER" && s.StructuralRole == "KEY_SESSION");

        Assert.Equal("TAPER_SHARPEN", taperKey.ProgressionStageKey);
        Assert.Equal("KEY_SESSION", taperKey.StructuralRole);
        Assert.Equal("EASY_STANDARD", taperKey.WorkoutDefinitionKey);
        Assert.Equal(CatalogSessionPrescriptionStatus.BaselinePrescribedSharpeningPending, taperKey.Prescription.Status);
        Assert.DoesNotContain(taperKey.Prescription.OrderedSegments, s => s.ComponentType.Contains("STRIDE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Phase4F7D_CapabilityAndPolicy_AreDeterministicLeastInvasiveRuntimeContract()
    {
        var capability = V1TaperSharpenPrescriptionPolicy.AssessCapability();
        var selection = V1TaperSharpenPrescriptionPolicy.SelectComponents(5);
        var decision = V1TaperSharpenPrescriptionPolicy.DecisionFor(selection);

        Assert.Equal(TaperSharpenCapabilityClassification.SupportedByAdditiveRuntimePrescriptionContract, capability.Classification);
        Assert.Equal("V1_TAPER_SHARPEN_PRESCRIPTION_POLICY", V1TaperSharpenPrescriptionPolicy.PolicyKey);
        Assert.Equal(1, V1TaperSharpenPrescriptionPolicy.PolicyVersion);
        Assert.Equal("CONTROLLED_SHARPENING", selection.SharpeningComponentType);
        Assert.Equal("after_easy_baseline_before_easy_recovery", selection.Placement);
        Assert.Equal(V1TaperSharpenPrescriptionPolicy.PolicyKey, decision.PolicyKey);
        Assert.Contains("target goal pace remain unresolved", decision.PaceAndEffortRule);
    }

    [Fact]
    public void Phase4F7D_FinalTaperSharpen_PreservesIdentityAndRemovesPendingState()
    {
        var final = BuildFinalPrescribed(12);
        var taperKey = final.Sessions.Single(s => s.PhaseKey == "TAPER" && s.StructuralRole == "KEY_SESSION");

        Assert.Equal("TAPER", taperKey.PhaseKey);
        Assert.Equal("TAPER_SHARPEN", taperKey.ProgressionStageKey);
        Assert.Equal("KEY_SESSION", taperKey.StructuralRole);
        Assert.Equal("EASY_STANDARD", taperKey.WorkoutDefinitionKey);
        Assert.Equal(CatalogSessionPrescriptionStatus.FinalPrescriptionComplete, taperKey.Prescription.Status);
        Assert.DoesNotContain(final.Sessions, s => s.Prescription.Status == CatalogSessionPrescriptionStatus.BaselinePrescribedSharpeningPending);
        Assert.True(final.ValidationResult.IsValid);
    }

    [Fact]
    public void Phase4F7D_FinalTaperSharpen_IsMateriallyDistinctWithoutWholeRunAcceleration()
    {
        var final = BuildFinalPrescribed(12);
        var taperKey = final.Sessions.Single(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s));

        Assert.Equal(new[] { "EASY_BASELINE", "CONTROLLED_SHARPENING", "EASY_RECOVERY" }, taperKey.Prescription.OrderedSegments.Select(s => s.ComponentType));
        Assert.Equal(new[] { 5.0, 1.5, 0.5 }, taperKey.Prescription.OrderedSegments.Select(s => s.DistanceKm));
        Assert.Contains(taperKey.Prescription.OrderedSegments, s => s.ComponentType == "CONTROLLED_SHARPENING" && s.IntensityDescriptor == "CONTROLLED_FAST_RELAXED");
        Assert.Contains(taperKey.Prescription.OrderedSegments, s => s.ComponentType == "EASY_BASELINE" && s.IntensityDescriptor == "EASY");
        Assert.Contains(taperKey.Prescription.OrderedSegments, s => s.ComponentType == "EASY_RECOVERY" && s.IntensityDescriptor == "EASY_RECOVERY");
        Assert.False(taperKey.Prescription.OrderedSegments.All(s => s.IntensityDescriptor == "CONTROLLED_FAST_RELAXED"));
        Assert.All(taperKey.Prescription.OrderedSegments, s => Assert.Equal(CatalogPacePrescriptionKind.EffortOnly, s.PacePrescription.Kind));
        Assert.All(taperKey.Prescription.OrderedSegments, s => Assert.NotEqual(CatalogPaceSourceSelection.TargetGoalDerived, s.PacePrescription.Source));
        Assert.Equal(CatalogDurationKind.Unresolved, taperKey.Prescription.DurationPrescription.Kind);
    }

    [Fact]
    public void Phase4F7D_Finalization_DoesNotChangeWeeklyLongRunOrSessionVolume()
    {
        var baseline = BuildPrescribed(12);
        var final = BuildFinalPrescribed(12);

        Assert.Equal(baseline.Weeks.Select(w => w.PlannedWeeklyVolumeKm), final.Weeks.Select(w => w.PlannedWeeklyVolumeKm));
        Assert.Equal(
            baseline.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "LONG_RUN").PlannedDistanceKm),
            final.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "LONG_RUN").PlannedDistanceKm));
        Assert.Equal(
            baseline.Sessions.Single(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s)).PlannedDistanceKm,
            final.Sessions.Single(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s)).PlannedDistanceKm);
        Assert.Equal(
            final.Sessions.Single(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s)).PlannedDistanceKm,
            final.Sessions.Single(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s)).Prescription.OrderedSegments.Sum(s => s.DistanceKm));
    }

    [Theory]
    [InlineData(8, null)]
    [InlineData(12, null)]
    [InlineData(14, null)]
    [InlineData(12, 0.0)]
    [InlineData(14, 0.0)]
    [InlineData(12, 24.0)]
    public void Phase4F7D_LowVolumeAndSupportedCycles_RemainFeasible(int weeks, double? weeklyVolume)
    {
        var final = BuildFinalPrescribed(weeks, r => r.RecentWeeklyVolumeKm = weeklyVolume);
        var taperKey = final.Sessions.Single(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s));

        Assert.True(final.ValidationResult.IsValid);
        Assert.InRange(taperKey.PlannedDistanceKm, 3, double.MaxValue);
        Assert.Equal(CatalogSessionPrescriptionStatus.FinalPrescriptionComplete, taperKey.Prescription.Status);
        Assert.Equal(taperKey.PlannedDistanceKm, taperKey.Prescription.OrderedSegments.Sum(s => s.DistanceKm));
        Assert.All(final.Weeks, week => Assert.Equal(week.PlannedWeeklyVolumeKm, week.AccountedWeeklyDistanceKm));
    }

    [Fact]
    public void Phase4F7D_ExplicitZeroEightWeekPath_RemainsBlockedByExisting4F7CAllocationMinimum()
    {
        var exception = Assert.Throws<CatalogSessionPrescriptionInfeasibleException>(() =>
            BuildFinalPrescribed(8, r => r.RecentWeeklyVolumeKm = 0));

        Assert.Contains("cannot support V1 key/easy minimums", exception.Message);
    }

    [Fact]
    public void Phase4F7D_TooSmallTaperAllocation_ReturnsTypedFailureWithoutVolumeIncrease()
    {
        var baseline = BuildPrescribed(12);
        var taperKey = baseline.Sessions.Single(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s));
        var tooSmall = taperKey with
        {
            PlannedDistanceKm = 2.5,
            Prescription = taperKey.Prescription with
            {
                DistancePrescription = taperKey.Prescription.DistancePrescription with { PlannedDistanceKm = 2.5 },
                OrderedSegments = [taperKey.Prescription.OrderedSegments[0] with { DistanceKm = 2.5 }]
            }
        };

        Assert.Throws<CatalogTaperSharpenPrescriptionInfeasibleException>(() => V1TaperSharpenPrescriptionPolicy.Complete(tooSmall));
    }

    [Fact]
    public void Phase4F7D_FinalPlanValidation_CoversStructurePaceProvenanceAndDeterminism()
    {
        var first = BuildFinalPrescribed(12);
        var second = BuildFinalPrescribed(12, boundPlan: BoundPlan(12, reverseWeeks: true));
        var volume = Build(12);

        Assert.Equal(48, first.Sessions.Count);
        Assert.All(first.Weeks, w => Assert.Equal(4, w.Sessions.Count));
        Assert.DoesNotContain(first.Sessions, s => s.Prescription.Status == CatalogSessionPrescriptionStatus.BaselinePrescribedSharpeningPending);
        Assert.Equal(volume.WeeklyVolumePlan.Weeks.Select(w => w.PlannedWeeklyVolumeKm), first.Weeks.Select(w => w.PlannedWeeklyVolumeKm));
        Assert.Equal(volume.LongRunProgression.Weeks.Select(w => w.PlannedLongRunDistanceKm), first.Weeks.Select(w => w.Sessions.Single(s => s.StructuralRole == "LONG_RUN").PlannedDistanceKm));
        Assert.All(first.Sessions.Where(s => s.WorkoutDefinitionKey != "GOAL_PACE_TEN_K"), s => Assert.NotEqual(CatalogPacePrescriptionKind.ExactPace, s.Prescription.PacePrescription.Kind));
        Assert.Contains(V1TaperSharpenPrescriptionPolicy.PolicyKey, first.Sessions.Single(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s)).VolumeAllocationProvenance);
        Assert.Equal(
            first.Sessions.OrderBy(s => (s.WeekNumber, s.Date, s.StructuralRole)).Select(s => (s.WeekNumber, s.Date, s.StructuralRole, s.WorkoutDefinitionKey, s.PlannedDistanceKm)),
            second.Sessions.OrderBy(s => (s.WeekNumber, s.Date, s.StructuralRole)).Select(s => (s.WeekNumber, s.Date, s.StructuralRole, s.WorkoutDefinitionKey, s.PlannedDistanceKm)));
    }

    private static CatalogVolumeAndLongRunPlan Build(int weeks, Action<GeneratePreviewRequest>? configure = null, BoundCatalogPlan? boundPlan = null)
    {
        var request = Request(configure);
        var bound = boundPlan ?? BoundPlan(weeks);
        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request,
            AsOfDate,
            Candidate(),
            InputSnapshot(),
            new[] { Result("PACE_SOURCE_IN", "RECENT_RACE"), Result("GOAL_FEASIBILITY_IN", "REALISTIC") },
            bound,
            WorkoutDefinitions()));

        return new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(
            Candidate(),
            bound,
            prescription,
            new CatalogPeakVolumeBand("TEN_K", "INTERMEDIATE", 4, 30, 42, "PEAK_VOLUME_BANDS_V1", 3)));
    }

    private static CatalogPrescribedPlan BuildPrescribed(
        int weeks,
        Action<GeneratePreviewRequest>? configure = null,
        BoundCatalogPlan? boundPlan = null,
        string paceSource = "RECENT_RACE",
        string goalFeasibility = "REALISTIC")
    {
        var request = Request(configure);
        var bound = boundPlan ?? BoundPlan(weeks);
        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request,
            AsOfDate,
            Candidate(),
            InputSnapshot(),
            new[] { Result("PACE_SOURCE_IN", paceSource), Result("GOAL_FEASIBILITY_IN", goalFeasibility) },
            bound,
            WorkoutDefinitions()));
        var volume = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(
            Candidate(),
            bound,
            prescription,
            new CatalogPeakVolumeBand("TEN_K", "INTERMEDIATE", 4, 30, 42, "PEAK_VOLUME_BANDS_V1", 3)));

        return new CatalogSessionPrescriptionPlanner().Build(new CatalogSessionPrescriptionRequest(
            Candidate(),
            bound,
            prescription,
            volume,
            WorkoutDefinitions()));
    }

    private static CatalogPrescribedPlan BuildFinalPrescribed(
        int weeks,
        Action<GeneratePreviewRequest>? configure = null,
        BoundCatalogPlan? boundPlan = null,
        string paceSource = "RECENT_RACE",
        string goalFeasibility = "REALISTIC")
    {
        var request = Request(configure);
        var bound = boundPlan ?? BoundPlan(weeks);
        var prescription = new CatalogPrescriptionContextBuilder().Build(new CatalogPrescriptionContextBuildRequest(
            request,
            AsOfDate,
            Candidate(),
            InputSnapshot(),
            new[] { Result("PACE_SOURCE_IN", paceSource), Result("GOAL_FEASIBILITY_IN", goalFeasibility) },
            bound,
            WorkoutDefinitions()));
        var volume = new CatalogVolumeAndLongRunPlanner().Build(new CatalogVolumePlanningRequest(
            Candidate(),
            bound,
            prescription,
            new CatalogPeakVolumeBand("TEN_K", "INTERMEDIATE", 4, 30, 42, "PEAK_VOLUME_BANDS_V1", 3)));
        var baseline = new CatalogSessionPrescriptionPlanner().Build(new CatalogSessionPrescriptionRequest(
            Candidate(),
            bound,
            prescription,
            volume,
            WorkoutDefinitions()));

        return new CatalogFinalPrescribedPlanFinalizer().Complete(new CatalogFinalPrescriptionRequest(
            Candidate(),
            bound,
            volume,
            baseline));
    }

    private static GeneratePreviewRequest Request(Action<GeneratePreviewRequest>? configure = null)
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = new DateOnly(2026, 7, 20),
            RaceDate = new DateOnly(2026, 10, 4),
            TargetFinishTimeSeconds = 3000,
            PreferredDays = new[] { Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Sat },
            LongRunDay = Weekday.Sat,
            RecentWeeklyVolumeKm = 24,
            RecentLongestRunKm = 9,
            RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = AsOfDate.AddDays(-21) }
        };

        configure?.Invoke(request);
        return request;
    }

    private static ResolverInputSnapshot InputSnapshot() => new()
    {
        RequestedTargetDistanceKm = 10d,
        CanonicalDistanceFamily = "TEN_K",
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        GoalDistanceKm = 10d,
        StartDate = new DateOnly(2026, 7, 20),
        RaceDate = new DateOnly(2026, 10, 4),
        TargetFinishTimeSeconds = 3000,
        DaysPerWeek = 4,
        Level = RunningBackground.Intermediate
    };

    private static RuntimeConditionResolutionResult Result(string conditionType, string outputValue) =>
        RuntimeConditionResolutionResult.Evaluated(conditionType, outputValue, "TEST");

    private static PlanCatalogCandidateSummary Candidate() => new()
    {
        CandidateKey = "TEN_K__4D__INTERMEDIATE",
        CandidateVersion = 10,
        CandidateStatus = "DRAFT",
        CanonicalDistanceFamily = "TEN_K",
        Level = "INTERMEDIATE",
        DaysPerWeek = 4,
        CoreCycle = new PlanCatalogCoreCycle(8, 12, 14),
        MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 6),
        Layout = new PlanCatalogReference("FOUR_DAY_STANDARD", 2),
        LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
        WorkoutProgression = new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 5),
        ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2),
        RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
        PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 3),
        RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
        DependencyStatuses = new Dictionary<string, string>(),
        ReferencedWorkouts = new[] { new PlanCatalogReference("EASY_STANDARD", 4), new PlanCatalogReference("LONG_RUN_STANDARD", 4) },
        PhaseKeys = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
        PhaseAllocations = new[] { new PlanCatalogPhaseAllocation("FOUNDATION", 3), new PlanCatalogPhaseAllocation("BUILD", 4), new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 4), new PlanCatalogPhaseAllocation("TAPER", 1) },
        SlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }
    };

    private static IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> WorkoutDefinitions() =>
        new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal)
        {
            ["EASY_STANDARD"] = Definition("EASY_STANDARD", 4, "EASY", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["LONG_RUN_STANDARD"] = Definition("LONG_RUN_STANDARD", 4, "LONG_RUN", new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, new[] { "DISTANCE" }, new[] { "EXACT_SESSION_TOTAL" }),
            ["FARTLEK"] = Definition("FARTLEK", 4, "QUALITY", new[] { "BUILD" }, new[] { "MIXED" }, new[] { "ESTIMATED_SESSION_TOTAL" },
                new[] { Component(1, "WARM_UP", "EASY"), Component(2, "MAIN_SET", "SURGE_AND_FLOAT"), Component(3, "RECOVERY", "EASY_JOG"), Component(4, "COOL_DOWN", "EASY") }),
            ["THRESHOLD_TEMPO"] = Definition("THRESHOLD_TEMPO", 4, "QUALITY", new[] { "BUILD", "RACE_SPECIFIC" }, new[] { "MIXED" }, new[] { "ESTIMATED_SESSION_TOTAL" },
                new[] { Component(1, "WARM_UP", "EASY"), Component(2, "MAIN_SET", "THRESHOLD"), Component(3, "COOL_DOWN", "EASY") }),
            ["GOAL_PACE_TEN_K"] = Definition("GOAL_PACE_TEN_K", 2, "QUALITY", new[] { "RACE_SPECIFIC" }, new[] { "PACE_BASED" }, Array.Empty<string>(),
                new[] { Component(1, "WARM_UP", "EASY"), Component(2, "MAIN_SET", "GOAL_PACE"), Component(3, "COOL_DOWN", "EASY") })
        };

    private static CatalogWorkoutComponentSummary Component(int order, string type, string intensity) => new(order, type, intensity);

    private static CatalogWorkoutDefinitionSummary Definition(string key, int version, string family, IReadOnlyList<string> phases, IReadOnlyList<string> modes, IReadOnlyList<string> accountingModes, IReadOnlyList<CatalogWorkoutComponentSummary>? components = null) => new()
    {
        Key = key,
        Version = version,
        Status = "DRAFT",
        Family = family,
        EligiblePhases = phases,
        AllowedPrescriptionModes = modes,
        AllowedDistanceAccountingModes = accountingModes,
        Components = components ?? Array.Empty<CatalogWorkoutComponentSummary>()
    };

    private static BoundCatalogPlan BoundPlan(int weeks, bool reverseWeeks = false, string keyWorkoutKey = "EASY_STANDARD", int keyWorkoutVersion = 4)
    {
        var weekNumbers = Enumerable.Range(1, weeks).ToList();
        if (reverseWeeks)
        {
            weekNumbers.Reverse();
        }

        var boundWeeks = weekNumbers.Select(week =>
        {
            var phase = week switch
            {
                <= 2 => "FOUNDATION",
                _ when week == weeks => "TAPER",
                _ when week >= Math.Max(3, weeks - 4) => "RACE_SPECIFIC",
                _ => "BUILD"
            };
            var stage = phase == "TAPER" ? "TAPER_SHARPEN" : $"{phase}_STAGE";
            var monday = new DateOnly(2026, 7, 20).AddDays((week - 1) * 7);
            return new BoundCatalogWeek
            {
                WeekNumber = week,
                PhaseKey = phase,
                Sessions = new[]
                {
                    Session(week, monday, phase, "KEY_SESSION", stage, keyWorkoutKey, keyWorkoutVersion),
                    Session(week, monday.AddDays(2), phase, "EASY_SUPPORT", null, "EASY_STANDARD"),
                    Session(week, monday.AddDays(4), phase, "EASY_SUPPORT", null, "EASY_STANDARD"),
                    Session(week, monday.AddDays(6), phase, "LONG_RUN", null, "LONG_RUN_STANDARD")
                }
            };
        }).ToList();

        return new BoundCatalogPlan
        {
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            BinderVersion = "CATALOG_WORKOUT_BINDER_V1",
            Weeks = boundWeeks,
            Trace = new WorkoutBindingDecisionTrace { Steps = Array.Empty<WorkoutBindingDecisionTraceStep>() }
        };
    }

    private static BoundCatalogSession Session(int week, DateOnly date, string phase, string role, string? stage, string workoutKey, int workoutVersion = 4) => new()
    {
        WeekNumber = week,
        Date = date,
        PhaseKey = phase,
        ProgressionStageKey = stage,
        StructuralRole = role,
        WorkoutDefinitionKey = workoutKey,
        WorkoutDefinitionVersion = workoutVersion,
        BindingMode = role == "KEY_SESSION" ? CatalogWorkoutBindingMode.StageControlled : CatalogWorkoutBindingMode.FixedDefault,
        BindingPolicyKey = "TEN_K_WORKOUT_PROGRESSION_V1",
        BindingPolicyVersion = 5,
        SourceArtifactKey = "TEN_K_WORKOUT_PROGRESSION_V1",
        SourceArtifactVersion = 5,
        ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned,
        FallbackOrigin = null,
        BindingReason = "TEST"
    };
}
