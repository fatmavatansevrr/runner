using System;
using System.IO;
using System.Linq;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;
using Cell = RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE DIST-GEN.13 — authority-PROVENANCE proofs for the shared-authority
/// ownership normalization and the typed dispatch-cascade generalization.
///
/// These tests deliberately prove WHERE each already-frozen value is declared,
/// not WHAT it is. The "what" is already proven, byte-for-byte, by the
/// completely unmodified Dark15K/Dark16K/Dark18K projection suites and the
/// DistGen3/6/7/11 public-activation suites — those remain this phase's actual
/// zero-delta evidence, and this file adds to them rather than replacing any
/// of them. Nothing here freezes a new number or introduces a new authority.
/// </summary>
public sealed class DistGen13SharedAuthorityNormalizationTests
{
    private static Cell CellFor(double km) => new(km, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);

    // ── Registry shape: exactly one authority per approved dark cell ─────────

    [Fact]
    public void Registry_HasExactlyOneAuthorityPerApprovedDarkCell_NoMoreNoFewer()
    {
        var darkCells = Dark16KPilotEligibilityPolicy.ApprovedDarkCells;
        Assert.Equal(3, darkCells.Count);
        Assert.Equal(darkCells.Count, ProjectedTargetAuthorityRegistry.All.Count);

        foreach (var cell in darkCells)
        {
            var authority = ProjectedTargetAuthorityRegistry.TryResolve(cell);
            Assert.NotNull(authority);
            Assert.Equal(cell, authority!.Cell);
        }

        // Every registered authority's own cell is itself an approved dark cell —
        // an authority can never exist for a cell the dark gate never approved.
        foreach (var authority in ProjectedTargetAuthorityRegistry.All)
        {
            Assert.Contains(authority.Cell, darkCells);
        }
    }

    [Fact]
    public void Registry_DuplicateRegistration_FailsLoudlyAtConstruction_NeverSilentlyPicksOne()
    {
        var one = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(16.0));
        Assert.NotNull(one);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProjectedTargetAuthorityRegistry.BuildIndex([one!, one!]));
        Assert.Contains("registered more than once", ex.Message, StringComparison.Ordinal);
    }

    // ── Fail-closed lookup ──────────────────────────────────────────────────

    [Fact]
    public void Registry_NullCell_ResolvesNothing()
    {
        Assert.Null(ProjectedTargetAuthorityRegistry.TryResolve(null));
    }

    [Theory]
    [InlineData(12.0)]
    [InlineData(14.0)]
    [InlineData(17.0)]
    [InlineData(19.0)]
    [InlineData(20.0)]
    [InlineData(16.09)]
    [InlineData(15.5)]
    [InlineData(18.1)]
    public void Registry_UnregisteredTargetDistance_ResolvesNothing_NeverAProximityMatch(double km)
    {
        // Neither via a hand-constructed cell...
        Assert.Null(ProjectedTargetAuthorityRegistry.TryResolve(CellFor(km)));

        // ...nor via the only supported entry path — a dark-gate-resolved cell,
        // which itself refuses these targets, so the registry is never reached.
        var darkCell = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(
            km, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        Assert.Null(darkCell);
        Assert.Null(ProjectedTargetAuthorityRegistry.TryResolve(darkCell));
    }

    [Theory]
    [InlineData(15.0, RunningBackground.Beginner, 4)]
    [InlineData(16.0, RunningBackground.Advanced, 4)]
    [InlineData(18.0, RunningBackground.Beginner, 4)]
    [InlineData(15.0, RunningBackground.Intermediate, 3)]
    [InlineData(16.0, RunningBackground.Intermediate, 5)]
    [InlineData(18.0, RunningBackground.Intermediate, 6)]
    public void Registry_WrongLevelOrFrequency_ResolvesNothing_KeyIsTheFullQuadruple(
        double km, RunningBackground level, int runsPerWeek)
    {
        Assert.Null(ProjectedTargetAuthorityRegistry.TryResolve(new Cell(km, GoalDistance.HalfMarathon, level, runsPerWeek)));
    }

    [Fact]
    public void Registry_WrongParentFamily_ResolvesNothing()
    {
        Assert.Null(ProjectedTargetAuthorityRegistry.TryResolve(new Cell(16.0, GoalDistance.TenK, RunningBackground.Intermediate, 4)));
        Assert.Null(ProjectedTargetAuthorityRegistry.TryResolve(new Cell(16.0, GoalDistance.FiveK, RunningBackground.Intermediate, 4)));
    }

    // ── Exact-target authority stays exact, and stays in its own policy file ──

    [Theory]
    [InlineData(15.0, "dark 15K", 14.5)]
    [InlineData(16.0, "dark 16K", 15.0)]
    [InlineData(18.0, "dark 18K", 16.0)]
    public void ResolvedAuthority_ExactTargetFields_ComeFromThatTargetsOwnPolicy(
        double km, string expectedLabel, double expectedPeakLongRunKm)
    {
        var authority = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(km));
        Assert.NotNull(authority);

        Assert.Equal(expectedLabel, authority!.PilotLabel);
        Assert.Equal(km, authority.Cell.TargetDistanceKm);

        // EXACT-TARGET horizon (DIST-GEN.12 §54) — independently frozen per target.
        Assert.Equal(12, authority.PreferredCoreWeeks);
        Assert.Equal(14, authority.MaximumCoreWeeks);

        // EXACT-TARGET peak-volume bounds and selected peak.
        Assert.Equal(34.0d, authority.PeakVolumeBandMinimumKm);
        Assert.Equal(46.0d, authority.PeakVolumeBandMaximumKm);
        Assert.Equal(40.0d, authority.VolumeSafetyPolicy.ResolvedPeakReference.Value);

        // EXACT-TARGET calibration.
        Assert.Equal(23.5d, authority.VolumeSafetyPolicy.GoldenFixtureStartingVolumeKm);

        // THE exact-target field that must never be shared: three distinct values.
        Assert.Equal(expectedPeakLongRunKm, authority.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.True(authority.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm < km);
    }

    [Fact]
    public void ResolvedAuthority_PerTargetPolicyInstances_AreStillThreeDistinctObjects()
    {
        var a15 = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(15.0))!;
        var a16 = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(16.0))!;
        var a18 = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(18.0))!;

        Assert.True(ReferenceEquals(a15.VolumeSafetyPolicy, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K));
        Assert.True(ReferenceEquals(a16.VolumeSafetyPolicy, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K));
        Assert.True(ReferenceEquals(a18.VolumeSafetyPolicy, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K));

        Assert.False(ReferenceEquals(a15.VolumeSafetyPolicy, a16.VolumeSafetyPolicy));
        Assert.False(ReferenceEquals(a16.VolumeSafetyPolicy, a18.VolumeSafetyPolicy));
        Assert.False(ReferenceEquals(a15.VolumeSafetyPolicy, a18.VolumeSafetyPolicy));

        // The three absolute peak-LR ceilings remain genuinely different.
        Assert.Equal(14.5d, a15.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(15.0d, a16.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(16.0d, a18.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
    }

    [Theory]
    [InlineData(15.0, "DARK_15K_CORE_HORIZON_7_NOT_SUPPORTED")]
    [InlineData(16.0, "DARK_16K_CORE_HORIZON_7_NOT_SUPPORTED")]
    [InlineData(18.0, "DARK_18K_CORE_HORIZON_7_NOT_SUPPORTED")]
    public void ResolvedAuthority_ReasonCode_IsStillThatTargetsOwnDistinctCode(double km, string expected)
    {
        var authority = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(km))!;
        Assert.Equal(expected, authority.GetUnsupportedReasonCode(7));
    }

    [Theory]
    [InlineData(15.0)]
    [InlineData(16.0)]
    [InlineData(18.0)]
    public void ResolvedAuthority_HorizonDecision_IsIdenticalToThatTargetsOwnPolicyCall(double km)
    {
        var authority = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(km))!;
        var start = new DateOnly(2026, 8, 3);

        foreach (var weeks in new[] { 7, 8, 9, 10, 11, 12, 13, 14, 15 })
        {
            var race = start.AddDays(weeks * 7);
            var viaAuthority = authority.ClassifyHorizon(authority.DecideHorizon(start, race));
            var viaOwnPolicy = km switch
            {
                15.0 => TargetDistance15KHorizonPolicy.Classify(start, race),
                16.0 => TargetDistance16KHorizonPolicy.Classify(start, race),
                _ => TargetDistance18KHorizonPolicy.Classify(start, race),
            };
            Assert.Equal(viaOwnPolicy, viaAuthority);
        }
    }

    // ── Shared level/frequency authority: provenance, via one named owner ────

    [Theory]
    [InlineData(15.0)]
    [InlineData(16.0)]
    [InlineData(18.0)]
    public void SharedGrowthAuthority_OwnsGrowthCapAndLongRunShares_ForEveryProjectedTarget(double km)
    {
        var policy = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(km))!.VolumeSafetyPolicy;

        Assert.Equal(IntermediateFourDayGrowthAuthority.PreferredWeeklyIncreaseRate, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(IntermediateFourDayGrowthAuthority.HardWeeklyIncreaseRate, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(IntermediateFourDayGrowthAuthority.AbsoluteWeeklyIncreaseCapKm, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(IntermediateFourDayGrowthAuthority.LongRunPreferredMinimumShare, policy.LongRunPreferredMinimumShare);
        Assert.Equal(IntermediateFourDayGrowthAuthority.LongRunPreferredMaximumShare, policy.LongRunPreferredMaximumShare);
        Assert.Equal(IntermediateFourDayGrowthAuthority.LongRunSelectionShare, policy.LongRunSelectionShare);
        Assert.Equal(IntermediateFourDayGrowthAuthority.LongRunHardCapShare, policy.LongRunHardCapShare);
        Assert.Equal(IntermediateFourDayGrowthAuthority.StartingAbsoluteLongRunCapKm, policy.StartingAbsoluteLongRunCapKm);

        // And the values are exactly the already-frozen ones — zero delta.
        Assert.Equal(0.07d, policy.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, policy.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(0.30d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.33d, policy.LongRunSelectionShare);
        Assert.Equal(0.40d, policy.LongRunHardCapShare);
        Assert.Null(policy.StartingAbsoluteLongRunCapKm);
    }

    [Fact]
    public void SharedGrowthAuthority_IsScopedToIntermediateFourDay_AndDeclaresNoExactTargetField()
    {
        Assert.Equal(RunningBackground.Intermediate, IntermediateFourDayGrowthAuthority.ScopeLevel);
        Assert.Equal(4, IntermediateFourDayGrowthAuthority.ScopeRunsPerWeek);

        // The shared component must never acquire an exact-target field.
        var memberNames = typeof(IntermediateFourDayGrowthAuthority).GetMembers().Select(m => m.Name).ToArray();
        foreach (var forbidden in new[]
                 {
                     "PreferredAbsolutePeakLongRunKm", "ResolvedPeakReference", "SelectedPeakReference",
                     "CalibrationStartingVolumeKm", "GoldenFixtureStartingVolumeKm",
                     "PeakVolumeBandMinimumKm", "PeakVolumeBandMaximumKm",
                     "PreferredCoreWeeks", "MaximumCoreWeeks",
                 })
        {
            Assert.DoesNotContain(forbidden, memberNames);
        }
    }

    // ── Shared parent-family taper authority: one source, referenced by all ──

    [Theory]
    [InlineData(15.0)]
    [InlineData(16.0)]
    [InlineData(18.0)]
    public void ParentTaperAuthority_OwnsTaperForEveryProjectedTarget_ByReferenceNotByCopy(double km)
    {
        var policy = ProjectedTargetAuthorityRegistry.TryResolve(CellFor(km))!.VolumeSafetyPolicy;

        Assert.True(ReferenceEquals(HalfMarathonParentTaperAuthority.OrderedMultipliers, policy.TaperVolumeMultipliers));
        Assert.Equal(HalfMarathonParentTaperAuthority.TaperWeek2VolumeMultiplier, policy.TaperVolumeMultiplier);
        Assert.Equal(HalfMarathonParentTaperAuthority.TaperDurationWeeks, policy.ResolvedTaperVolumeMultipliers.Count);

        // Zero delta against the already-frozen values.
        Assert.Equal(new[] { 0.70d, 0.43d }, policy.ResolvedTaperVolumeMultipliers);
        Assert.Equal(0.43d, policy.TaperVolumeMultiplier);
    }

    [Fact]
    public void ParentTaperAuthority_CanonicalHalfMarathonReferencesTheSameSource_ValuesUnchanged()
    {
        foreach (var policy in new[] { VolumeSafetyPolicy.HalfMarathonIntermediate4D, VolumeSafetyPolicy.HalfMarathonIntermediate6D })
        {
            Assert.Equal(new[] { 0.70d, 0.43d }, policy.ResolvedTaperVolumeMultipliers);
            Assert.Equal(0.43d, policy.TaperVolumeMultiplier);
            Assert.True(ReferenceEquals(HalfMarathonParentTaperAuthority.OrderedMultipliers, policy.TaperVolumeMultipliers));
        }

        // Each cell's own pre-taper anchor is EXACT-TARGET and stays genuinely
        // different — it was deliberately NOT moved into the shared component.
        Assert.Equal(43.0d, HalfMarathonIntermediate4DTaperVolumePolicy.ResolvedPeakReferenceKm);
        Assert.Equal(51.0d, HalfMarathonIntermediate6DTaperVolumePolicy.ResolvedPeakReferenceKm);
        Assert.Equal(40.0d, TargetDistance15KTaperVolumePolicy.ResolvedPeakReferenceKm);
        Assert.Equal(40.0d, TargetDistance16KTaperVolumePolicy.ResolvedPeakReferenceKm);
        Assert.Equal(43.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value);
        Assert.Equal(51.0d, VolumeSafetyPolicy.HalfMarathonIntermediate6D.ResolvedPeakReference.Value);

        // Canonical HM's own growth/cap/LR-share values are untouched by this phase.
        var hm4D = VolumeSafetyPolicy.HalfMarathonIntermediate4D;
        Assert.Equal(0.07d, hm4D.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, hm4D.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, hm4D.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(25d, hm4D.GoldenFixtureStartingVolumeKm);
        Assert.Equal(19d, hm4D.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(0.33d, hm4D.LongRunSelectionShare);
        Assert.Null(hm4D.StartingAbsoluteLongRunCapKm);
    }

    [Fact]
    public void ParentTaperAuthority_NeverAppliesToTenK_WhoseTaperIsGenuinelyDifferent()
    {
        var tenK = VolumeSafetyPolicy.Default;
        Assert.Null(tenK.TaperVolumeMultipliers);
        Assert.Equal(0.53d, tenK.TaperVolumeMultiplier);
        Assert.Equal(new[] { 0.53d }, tenK.ResolvedTaperVolumeMultipliers);
        Assert.NotEqual(HalfMarathonParentTaperAuthority.TaperWeek2VolumeMultiplier, tenK.TaperVolumeMultiplier);

        // TEN_K's own growth/cap/LR-share values are also untouched.
        Assert.Equal(0.07d, tenK.PreferredMaxWeeklyIncreaseRatio);
        Assert.Equal(0.08d, tenK.HardMaxWeeklyIncreaseRatio);
        Assert.Equal(2.5d, tenK.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(24d, tenK.GoldenFixtureStartingVolumeKm);
        Assert.Equal(38d, tenK.ResolvedPeakReference.Value);
        Assert.Null(tenK.PreferredAbsolutePeakLongRunKm);
    }

    // ── Scope containment: the shared components leak nowhere ────────────────

    [Fact]
    public void SharedGrowthAuthority_DoesNotApplyToHalfMarathonIntermediate6D()
    {
        var sixDay = VolumeSafetyPolicy.HalfMarathonIntermediate6D;
        Assert.Equal(0.28d, sixDay.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, sixDay.LongRunPreferredMaximumShare);
        Assert.Equal(0.28d, sixDay.LongRunSelectionShare);
        Assert.Equal(0.36d, sixDay.LongRunHardCapShare);
        Assert.Equal(12.0d, sixDay.StartingAbsoluteLongRunCapKm);

        Assert.NotEqual(IntermediateFourDayGrowthAuthority.LongRunPreferredMinimumShare, sixDay.LongRunPreferredMinimumShare);
        Assert.NotEqual(IntermediateFourDayGrowthAuthority.LongRunSelectionShare, sixDay.LongRunSelectionShare);
        Assert.NotEqual(IntermediateFourDayGrowthAuthority.LongRunHardCapShare, sixDay.LongRunHardCapShare);
        Assert.NotEqual(IntermediateFourDayGrowthAuthority.StartingAbsoluteLongRunCapKm, sixDay.StartingAbsoluteLongRunCapKm);
    }

    [Fact]
    public void SharedGrowthAuthority_DoesNotApplyToOtherFrequenciesOrLevels()
    {
        var threeDay = VolumeSafetyPolicy.HalfMarathonIntermediate3D;
        Assert.Equal(2.0d, threeDay.AbsoluteWeeklyIncrementCapKm);
        Assert.NotEqual(IntermediateFourDayGrowthAuthority.AbsoluteWeeklyIncreaseCapKm, threeDay.AbsoluteWeeklyIncrementCapKm);
        Assert.NotEqual(IntermediateFourDayGrowthAuthority.LongRunSelectionShare, threeDay.LongRunSelectionShare);

        var fiveDay = VolumeSafetyPolicy.HalfMarathonIntermediate5D;
        Assert.NotNull(fiveDay.StartingAbsoluteLongRunCapKm);
        Assert.NotEqual(IntermediateFourDayGrowthAuthority.StartingAbsoluteLongRunCapKm, fiveDay.StartingAbsoluteLongRunCapKm);

        // Beginner HM cells genuinely differ in value as well as in authority.
        Assert.Equal(0.38d, VolumeSafetyPolicy.HalfMarathonBeginner3D.LongRunSelectionShare);
        Assert.NotEqual(
            IntermediateFourDayGrowthAuthority.LongRunSelectionShare,
            VolumeSafetyPolicy.HalfMarathonBeginner3D.LongRunSelectionShare);

        // HALF_MARATHON × ADVANCED × 4D is the important case for DIST-GEN.8's
        // "value equality is not authority equality" rule: its quadruple and cap
        // happen to COINCIDE numerically with the Intermediate×4D component's,
        // yet it is a separate, independently-frozen Advanced authority (HM.15)
        // and is deliberately NOT a consumer of the shared component. Its values
        // are asserted unchanged here; the fact that it does not reference the
        // shared component is proven structurally by
        // SharedGrowthAuthority_IsReferencedByExactlyTheThreeProjectedTargets.
        var advanced4D = VolumeSafetyPolicy.HalfMarathonAdvanced4D;
        Assert.Equal(0.30d, advanced4D.LongRunPreferredMinimumShare);
        Assert.Equal(0.36d, advanced4D.LongRunPreferredMaximumShare);
        Assert.Equal(0.33d, advanced4D.LongRunSelectionShare);
        Assert.Equal(0.40d, advanced4D.LongRunHardCapShare);
        Assert.Equal(2.5d, advanced4D.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(19.0d, advanced4D.PreferredAbsolutePeakLongRunKm);
    }

    /// <summary>
    /// The structural scope-containment proof: the shared Intermediate×4D
    /// component is referenced by EXACTLY the three projected-target
    /// <see cref="VolumeSafetyPolicy"/> instances and by nothing else in
    /// <c>VolumeSafetyPolicy.cs</c>. Every other named instance — TEN_K's
    /// <c>Default</c>, canonical HM 3D/4D/5D/6D, every Beginner and Advanced
    /// cell — still declares its own literals, including those whose values
    /// happen to coincide. This is what stops the component from silently
    /// widening to a cell whose authority was never governed as shared.
    /// </summary>
    [Fact]
    public void SharedGrowthAuthority_IsReferencedByExactlyTheThreeProjectedTargets()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application",
            "RuntimeCatalog", "Prescription", "Volume", "VolumeSafetyPolicy.cs"));

        // Seven references per consuming instance (two growth rates, the
        // absolute cap, the four-member long-run share quadruple) plus one
        // StartingAbsoluteLongRunCapKm reference = 8, times exactly 3 targets.
        var referenceCount = source.Split("IntermediateFourDayGrowthAuthority.").Length - 1;
        Assert.Equal(24, referenceCount);

        // And the shared component is never referenced from a non-4D-Intermediate
        // taper/peak policy file either.
        foreach (var otherFile in new[] { "HalfMarathonIntermediate4DTaperVolumePolicy.cs", "HalfMarathonIntermediate6DTaperVolumePolicy.cs" })
        {
            var other = File.ReadAllText(Path.Combine(
                TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application",
                "RuntimeCatalog", "Prescription", "Volume", otherFile));
            Assert.DoesNotContain("IntermediateFourDayGrowthAuthority", other, StringComparison.Ordinal);
        }
    }

    // ── Parent-family minimum-core-weeks ownership ───────────────────────────

    [Fact]
    public void MinimumCoreWeeks_IsTheParentFamilyStructuralConstant_ValueUnchangedAtTen()
    {
        Assert.Equal(10, RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks);
        Assert.Equal(RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks, TargetDistance15KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks, TargetDistance16KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks, TargetDistance18KHorizonPolicy.MinimumCoreWeeks);

        Assert.Equal(10, TargetDistance15KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(10, TargetDistance16KHorizonPolicy.MinimumCoreWeeks);
        Assert.Equal(10, TargetDistance18KHorizonPolicy.MinimumCoreWeeks);

        foreach (var authority in ProjectedTargetAuthorityRegistry.All)
        {
            Assert.Equal(10, authority.MinimumCoreWeeks);
        }
    }

    // ── No band / no proximity match / no fitted value in the new code ───────

    [Fact]
    public void NewDistGen13Sources_ContainNoRangeProximityOrFittedValueConcept()
    {
        var repo = TestPlanServicesFactory.RepoRoot();
        string[] files =
        [
            Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "TargetDistanceProjection", "ProjectedTargetAuthority.cs"),
            Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "TargetDistanceProjection", "ProjectedTargetAuthorityRegistry.cs"),
            Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "Prescription", "Volume", "IntermediateFourDayGrowthAuthority.cs"),
            Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "Prescription", "Volume", "HalfMarathonParentTaperAuthority.cs"),
        ];

        // Doc comments are allowed to say these concepts are ABSENT; executable
        // code may not contain them at all. Comment lines are therefore stripped
        // before scanning.
        string[] forbidden = ["nearest", "Nearest", "Interpolat", "interpolat", "Lerp", "lerp", "midpoint", "Midpoint", "minTarget", "maxTarget"];

        foreach (var file in files)
        {
            Assert.True(File.Exists(file), $"Expected DIST-GEN.13 source file missing: {file}");
            var code = string.Join(
                "\n",
                File.ReadAllLines(file).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));

            foreach (var token in forbidden)
            {
                Assert.DoesNotContain(token, code, StringComparison.Ordinal);
            }
        }
    }
}
