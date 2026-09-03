using System;
using System.Linq;
using System.Threading.Tasks;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Volume;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Session;

/// <summary>
/// Phase 10K-GEN.24 — direct user decision resolving GEN.23's disclosed gap
/// (§5 of that report): explicit-zero readiness is non-representable at all
/// 7 governed Beginner×3D Core horizons because the borrowed 9.5km
/// Beginner×4D explicit-zero starting default sits below the unchanged
/// 12.0km normal-week 3D floor, so week 1 itself is infeasible independent
/// of horizon or GEN.23's own taper-minima mechanism.
///
/// Beginner×3D is NOT reclassified as non-support by this phase — it
/// remains SUPPORTED. The distinction is at the request/readiness level,
/// mirroring GEN.9's exact pattern for Advanced's missing/zero readiness
/// (PRODUCT_INELIGIBLE via a typed exception, not a frequency-level
/// non-support classification):
///
///   Beginner × 3D                =  SUPPORTED
///   Missing readiness            =  ELIGIBLE   (GEN.23, representable)
///   Positive observed readiness  =  ELIGIBLE   (GEN.23, representable)
///   Explicit-zero readiness      =  PRODUCT_INELIGIBLE   (this phase)
///
/// This phase does NOT raise the borrowed 9.5km starting default, does NOT
/// invent a new Beginner×3D-specific explicit-zero default, and does NOT
/// touch Beginner×4D's own explicit-zero handling (verified zero-delta
/// below) — only Beginner×3D's response to an explicit-zero request
/// changes, from two disclosed raw/untyped failure shapes (GEN.23 §5) to
/// one clean, correctly-classified, typed rejection.
/// </summary>
public sealed class Gen24BeginnerThreeDayExplicitZeroIneligibilityTests
{
    // ── Explicit-zero: PRODUCT_INELIGIBLE, uniformly, at every governed
    //    Core horizon — replacing GEN.23's two disclosed raw failure
    //    shapes with one clean, typed rejection. ──────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task ExplicitZero_AllGovernedHorizons_FailsClosed_WithClearTypedRejection(int weeks)
    {
        var candidate = await Gen23BeginnerThreeDayCoreTests.RealBeginnerThreeDayCandidateAsync();
        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(
            () => Gen23BeginnerThreeDayCoreTests.BuildAsync(candidate, weeks, Gen23BeginnerThreeDayCoreTests.ReadinessState.ExplicitZero));

        var ineligible = Assert.IsType<BeginnerThreeDayExplicitZeroReadinessProductIneligibleException>(wrapper.InnerException);
        Assert.IsAssignableFrom<CatalogProductIneligibleException>(ineligible);
        Assert.Equal(BeginnerThreeDayExplicitZeroReadinessProductIneligibleException.Reason, ineligible.Code);
        Assert.Equal("BEGINNER_THREE_DAY_EXPLICIT_ZERO_READINESS_NOT_ELIGIBLE", ineligible.Code);
        Assert.Contains("PRODUCT_INELIGIBLE", ineligible.Message);
        Assert.Contains("explicit-zero", ineligible.Message);
        // Never the generic/pre-existing raw exceptions GEN.23 §5 disclosed
        // for this same readiness state before this phase's decision.
        Assert.IsNotType<BeginnerThreeDayCoreProductIneligibleException>(ineligible);
    }

    /// <summary>
    /// The rejection fires at readiness-resolution time — before the
    /// taper-eligibility gate or any per-week session floor is ever
    /// reached — confirmed by asserting it fires identically for both of
    /// GEN.23's previously-distinct failure-shape horizon bands (8-11 and
    /// 12-14), not just one of them.
    /// </summary>
    [Fact]
    public void FrozenAuthority_NoNumericValueChanged_ExplicitZeroDefaultStillNineFive()
    {
        // The reused Beginner×4D explicit-zero default is untouched — this
        // phase rejects the request, it does not raise the number.
        Assert.Equal(9.5d, V1BeginnerFourDayMissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm);
        Assert.Equal(12d, V1BeginnerFourDayMissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm);
        // Beginner×3D's own taper-minima triple (GEN.23) also untouched.
        Assert.Equal(8.5d, V1BeginnerThreeDayVolumeEligibilityPolicy.MinimumFullLayoutTaperWeeklyVolumeKm);
    }

    // ── Zero-delta re-verification: missing readiness and positive-observed
    //    readiness (both band boundaries) remain fully representable at
    //    every governed Core horizon — re-run, not assumed. ─────────────

    public static TheoryData<int, Gen23BeginnerThreeDayCoreTests.ReadinessState> StillRepresentableStates()
    {
        var data = new TheoryData<int, Gen23BeginnerThreeDayCoreTests.ReadinessState>();
        foreach (var weeks in new[] { 8, 9, 10, 11, 12, 13, 14 })
        {
            data.Add(weeks, Gen23BeginnerThreeDayCoreTests.ReadinessState.MissingReadiness);
            data.Add(weeks, Gen23BeginnerThreeDayCoreTests.ReadinessState.PositiveObservedBandLower);
            data.Add(weeks, Gen23BeginnerThreeDayCoreTests.ReadinessState.PositiveObservedBandUpper);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(StillRepresentableStates))]
    public async Task MissingAndPositiveObserved_RemainRepresentable_EveryGovernedHorizon_ZeroDelta(
        int weeks, Gen23BeginnerThreeDayCoreTests.ReadinessState state)
    {
        var candidate = await Gen23BeginnerThreeDayCoreTests.RealBeginnerThreeDayCandidateAsync();
        var result = await Gen23BeginnerThreeDayCoreTests.BuildAsync(candidate, weeks, state);

        Assert.True(result.FinalPrescribedPlan.ValidationResult.IsValid,
            string.Join("; ", result.FinalPrescribedPlan.ValidationResult.Errors));
        Assert.Equal(weeks, result.FinalPrescribedPlan.Weeks.Count);

        var taperVolume = result.VolumeResult.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks.Single(w => w.IsTaperWeek).PlannedWeeklyVolumeKm;
        Assert.True(taperVolume >= 8.5d, $"weeks={weeks} state={state}: taper volume {taperVolume} below the 8.5km floor.");
    }

    // ── Zero-delta: Beginner×4D's own explicit-zero handling is completely
    //    untouched by this phase — same typed exception, same eligibility
    //    boundary, same 9.5km default where eligible. ───────────────────

    [Theory]
    [InlineData(8, false)]   // taper 7.5km, ineligible -- BeginnerFourDayCoreProductIneligibleException, unchanged
    [InlineData(13, true)]   // taper 9.5km, eligible -- 9.5km start resolves unchanged
    public async Task BeginnerFourDay_ExplicitZeroHandling_IsCompletelyUnaffected_ZeroDelta(int weeks, bool eligible)
    {
        var candidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealBeginnerFourDayCandidateAsync();
        var operation = () => DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(
            candidate, weeks, DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.ExplicitZeroEvidence);

        if (!eligible)
        {
            var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(operation);
            var typed = Assert.IsType<BeginnerFourDayCoreProductIneligibleException>(wrapper.InnerException);
            Assert.Equal(BeginnerFourDayCoreProductIneligibleException.Reason, typed.Code);
            // Never the new Beginner×3D-specific exception this phase adds.
            Assert.IsNotType<BeginnerThreeDayExplicitZeroReadinessProductIneligibleException>(typed);
            return;
        }

        var result = await operation();
        Assert.Equal(9.5d, result.VolumeAndLongRunPlan.WeeklyVolumePlan.StartingVolumeDecision.SelectedStartingVolumeKm);
    }

    // ── Zero-delta: Intermediate×3D's own starting-volume dispatch is
    //    completely unaffected (the new check is scoped exactly to
    //    ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner)). ──

    [Fact]
    public async Task IntermediateThreeDay_ZeroDelta_Unaffected()
    {
        var candidate = await DynamicCoreSessionPrescriptionOrchestratorTests.RealThreeDayCandidateAsync();
        var result = await DynamicCoreSessionPrescriptionOrchestratorTests.BuildAsync(
            candidate, 12, DynamicCoreSessionPrescriptionOrchestratorTests.PaceSourceCategory.RecentRace);

        Assert.True(result.FinalPrescribedPlan.ValidationResult.IsValid);
    }

    // ── Public gate remains untouched — dark-only, exactly like GEN.23. ──

    [Fact]
    public void PublicGate_RemainsClosed_BeginnerThreeDayNotWidened()
    {
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 3));
        // Neighboring identities remain unaffected.
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 4));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 3));
    }
}
