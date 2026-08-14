using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Volume;

public sealed class Gen4DBeginnerFourDayCoreTests
{
    public static IEnumerable<object[]> MissingMatrix()
    {
        yield return new object[] { 8, 17.5d, 9.5d };
        yield return new object[] { 9, 18.5d, 10d };
        yield return new object[] { 10, 19d, 10d };
        yield return new object[] { 11, 20d, 10.5d };
        yield return new object[] { 12, 21d, 11d };
        yield return new object[] { 13, 22d, 11.5d };
        yield return new object[] { 14, 23d, 12d };
    }

    public static IEnumerable<object[]> ExplicitZeroMatrix()
    {
        yield return new object[] { 8, 7.5d, false };
        yield return new object[] { 9, 7.5d, false };
        yield return new object[] { 10, 8d, false };
        yield return new object[] { 11, 8.5d, false };
        yield return new object[] { 12, 8.5d, false };
        yield return new object[] { 13, 9.5d, true };
        yield return new object[] { 14, 9.5d, true };
    }

    [Fact]
    public void Policy_FrozenValuesAndProvenance_AreExact()
    {
        var policy = VolumeSafetyPolicy.BeginnerFourDay;
        Assert.Equal(12d, policy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(21d, policy.ResolvedPeakReference.Value);
        Assert.Equal(ResolvedPeakReferenceProvenance.ProductDefaultWithEvidenceEnvelope, policy.ResolvedPeakReference.Provenance);
        Assert.Equal(.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(.53d, policy.TaperVolumeMultiplier);
        Assert.Equal(9d, V1BeginnerFourDayVolumeEligibilityPolicy.MinimumFullLayoutWeeklyVolumeKm);
        Assert.Equal(17d, V1BeginnerFourDayVolumeEligibilityPolicy.TaperBreakEvenPreTaperKm);
        Assert.Equal(12d, V1BeginnerFourDayMissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm);
        Assert.Equal(9.5d, V1BeginnerFourDayMissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm);
        Assert.Equal(38d, VolumeSafetyPolicy.Default.ResolvedPeakReference.Value);
        Assert.Equal(ResolvedPeakReferenceProvenance.GoldenFixtureDerived, VolumeSafetyPolicy.Default.ResolvedPeakReference.Provenance);
    }

    [Theory]
    [MemberData(nameof(MissingMatrix))]
    public async Task MissingReadiness_AllHorizonsGenerate_WithFourDayCardinality(int weeks, double preTaper, double taper)
    {
        var candidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealBeginnerFourDayCandidateAsync();
        var result = await DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(
            candidate, weeks, DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.RecentVolumeMissing);

        Assert.Equal(12d, result.VolumeAndLongRunPlan.WeeklyVolumePlan.StartingVolumeDecision.SelectedStartingVolumeKm);
        Assert.Equal(preTaper, result.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks[^2].PlannedWeeklyVolumeKm);
        Assert.Equal(taper, result.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks[^1].PlannedWeeklyVolumeKm);
        Assert.All(result.BindingResult.BoundPlan.Weeks, week =>
        {
            Assert.Equal(1, week.Sessions.Count(s => s.StructuralRole == "KEY_SESSION"));
            Assert.Equal(2, week.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT"));
            Assert.Equal(1, week.Sessions.Count(s => s.StructuralRole == "LONG_RUN"));
        });
    }

    [Theory]
    [MemberData(nameof(ExplicitZeroMatrix))]
    public async Task ExplicitZero_UsesTypedEligibilityBoundary(int weeks, double taper, bool eligible)
    {
        var candidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealBeginnerFourDayCandidateAsync();
        var operation = () => DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(
            candidate, weeks, DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.ExplicitZeroEvidence);

        if (!eligible)
        {
            var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(operation);
            var typed = Assert.IsType<BeginnerFourDayCoreProductIneligibleException>(wrapper.InnerException);
            Assert.Equal(BeginnerFourDayCoreProductIneligibleException.Reason, typed.Code);
            Assert.Contains($"{taper:0.##}km", typed.Message);
            return;
        }

        var result = await operation();
        Assert.Equal(9.5d, result.VolumeAndLongRunPlan.WeeklyVolumePlan.StartingVolumeDecision.SelectedStartingVolumeKm);
        Assert.Equal(taper, result.VolumeAndLongRunPlan.WeeklyVolumePlan.Weeks.Single(w => w.IsTaperWeek).PlannedWeeklyVolumeKm);
    }

    [Fact]
    public async Task Candidate_IsPublic_AndOnlyBeginnerFourDayWasWidened()
    {
        // GEN.4E: Beginner 4D is now publicly reachable identity-wise.
        // Every other untested cell remains explicitly closed -- this is an
        // exact enumerated allow-list (V1CatalogPilotIdentityPolicy), not a
        // derived/inferred rule, so accidental admission of a new cell would
        // fail this test immediately.
        var candidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealBeginnerFourDayCandidateAsync();
        Assert.Equal("NEW", candidate.Level);
        Assert.Equal(4, candidate.DaysPerWeek);
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 3));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Advanced, 4));
        Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Beginner, 5));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 3));
        Assert.True(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(
            GoalType.Race, GoalDistance.TenK, RunningBackground.Intermediate, 4));
    }

    [Fact]
    public async Task Beginner_DoesNotSelectDeferredFartlekOrThresholdWorkouts()
    {
        var candidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealBeginnerFourDayCandidateAsync();
        var result = await DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(
            candidate, 14, DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.RecentVolumeMissing);
        var selected = result.BindingResult.BoundPlan.Weeks.SelectMany(w => w.Sessions).Select(s => s.WorkoutDefinitionKey);
        Assert.DoesNotContain("FARTLEK", selected);
        Assert.DoesNotContain("THRESHOLD_TEMPO", selected);
    }

    [Fact]
    public async Task IneligibilityException_IsCatchableAsSharedProductIneligibleBase_MatchingCatalogPreviewGeneratorRouting()
    {
        // GEN.4D.2: CatalogPreviewGenerator translates product-ineligibility to
        // HTTP 422 by catching the shared CatalogProductIneligibleException base
        // (not each concrete Level/Frequency exception type). This locks in that
        // Beginner 4D's typed ineligibility failure really is an instance of that
        // shared base, so the translation continues to fire even though this
        // test -- like the rest of this file -- exercises the orchestrator
        // directly rather than CatalogPreviewGenerator's HTTP surface (Beginner
        // 4D has no public route to reach that surface through; see the GEN.4D.2
        // closure note for why true through-generator coverage is out of scope
        // here).
        var candidate = await DynamicCoreVolumeAndLongRunOrchestratorTests.RealBeginnerFourDayCandidateAsync();
        var wrapper = await Assert.ThrowsAsync<DynamicCoreVolumeAndLongRunFailedException>(() =>
            DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync(
                candidate, 8, DynamicCoreVolumeAndLongRunOrchestratorTests.RunnerProfile.ExplicitZeroEvidence));

        var ineligible = Assert.IsType<BeginnerFourDayCoreProductIneligibleException>(wrapper.InnerException);
        Assert.IsAssignableFrom<CatalogProductIneligibleException>(ineligible);
        Assert.Equal(BeginnerFourDayCoreProductIneligibleException.Reason, ineligible.Code);
    }
}
