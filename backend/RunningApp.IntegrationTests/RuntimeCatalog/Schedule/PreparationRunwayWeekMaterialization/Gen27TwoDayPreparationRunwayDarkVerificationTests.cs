using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

/// <summary>
/// Phase 10K-GEN.27 -- dark verification (no HTTP, no public gate, no
/// PostgreSQL persistence) for the new 2D Preparation Runway repeating-
/// pattern SELECTION mechanism that GEN.19 §2 found entirely absent and
/// GEN.26 Q1/Q2 resolved architecturally (Hypothesis (a): Runway's A/B
/// structure is byte-identical to Core's, continuing the same global week
/// ordinal). Exercises the real, unmodified <see cref="PreparationRunwayWeekMaterializer"/>
/// -- no fabricated skeleton -- using a single real block
/// (<c>TEN_K_GENERAL_ENDURANCE_PROGRESSION</c>, whose real catalog content
/// is LONG_RUN-family and already anchors LONG_RUN unmodified for every
/// existing frequency) to isolate the newly-added pattern-selection logic
/// (<c>PreparationRunwayCanonicalWeeklyLayout.WeeklyPatternRoles</c>/
/// <c>PatternPeriodWeeks</c>, <c>PreparationRunwayWeeklyShape.IsValidTwoDayModelB</c>,
/// and the materializer's per-week <c>ResolveWeekRoles</c>) from the
/// still-unresolved anchor/content-placement question below.
///
/// Disclosed scope, per this phase's own honest DONE (PARTIAL) governance
/// report (matching GEN.12/GEN.19 precedent): a real dark-verification
/// attempt against the FULL real block-progression catalog (all four
/// Runway blocks, not just GeneralEndurance) empirically disproved this
/// phase's initial hypothesis that every progression step's anchor could
/// uniformly map to LONG_RUN for 2D -- <c>TEN_K_CONSISTENCY_PROGRESSION</c>
/// step 1 and <c>TEN_K_AEROBIC_STRENGTH_PROGRESSION</c>'s real anchor
/// content is EASY/QUALITY-family, authored for the KEY_SESSION role
/// specifically, and fails <see cref="PreparationRunwayWeekMaterializer"/>'s
/// own family-compatibility check when forced onto LONG_RUN.
/// <see cref="TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies"/>
/// (at the time this file was written) therefore threw for
/// <c>daysPerWeek == 2</c> rather than returning a plausible-looking-but-
/// wrong policy -- the real, newly-confirmed (not merely theorized)
/// architecture question this left open was resolved by GEN.28 §9
/// (Candidate C) and implemented by GEN.29 (role-conditioned Pattern-A/
/// Pattern-B content selection inside <see cref="PreparationRunwayWeekMaterializer"/>
/// itself); <c>BuildBlockRolePolicies(2)</c> no longer throws (see this
/// class's own updated test below), and <c>Gen29TwoDayRunwayBlockRoleReconciliationTests</c>
/// covers the resolved behavior end-to-end. This file's own GeneralEndurance-
/// only scenario is preserved unchanged since GeneralEndurance had zero
/// reconciliation conflict to begin with (GEN.28 §3). This file does NOT
/// exercise <c>TenKPreparationRunwayNumericPolicyFactory</c>, the
/// Preparation Runway calendar composer, real PostgreSQL persistence, or
/// the combined Runway+Core plan orchestrator -- all confirmed still
/// absent/ungated for 2D.
/// </summary>
public sealed class Gen27TwoDayPreparationRunwayDarkVerificationTests
{
    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    [Fact]
    public async Task TwoDayRunway_AlternatesModelBPatternByGlobalWeekParity_AcrossFullEightWeekHorizon()
    {
        // TEN_K_GENERAL_ENDURANCE_PROGRESSION's real catalog capacity is 5
        // steps -- 5 weeks is used here (still spans both a run of
        // Pattern-A-starting-odd and Pattern-B-starting-even weeks: 1,3,5
        // are Pattern A and 2,4 are Pattern B) rather than a fabricated
        // 8-week progression.
        const int runwayWeeks = 5;
        var request = await BuildSingleBlockTwoDayRequestAsync(runwayWeeks);

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());

        Assert.True(result.IsSuccess, result.FailureReason);
        var weeks = result.Weeks!;

        // GEN.19's own confirmed gap: contiguous global week numbers, never
        // reset (GEN.11 §1/§11, GEN.26 Q1). For a standalone Runway product
        // with no preceding GE segment, this contiguous runway week number
        // IS the global ordinal.
        Assert.Equal(Enumerable.Range(1, runwayWeeks), weeks.Select(w => w.RunwayWeekNumber));

        foreach (var week in weeks)
        {
            var roles = week.OrderedWorkoutSlots.Select(s => s.SlotRole).OrderBy(r => r).ToArray();
            var isPatternAWeek = week.RunwayWeekNumber % 2 == 1;

            var expectedRoles = isPatternAWeek
                ? new[] { PreparationRunwaySlotRole.KeySession, PreparationRunwaySlotRole.LongRun }
                : new[] { PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.LongRun };
            Assert.Equal(expectedRoles.OrderBy(r => r), roles);

            // The block's real, progression-bound LONG_RUN-family anchor
            // lands on LONG_RUN every week regardless of pattern letter --
            // LONG_RUN is the one role present in both Pattern A and B.
            var anchor = Assert.Single(week.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
            Assert.Equal(PreparationRunwaySlotRole.LongRun, anchor.SlotRole);

            // The non-anchor slot (KEY_SESSION on Pattern A, EASY_SUPPORT on
            // Pattern B) is filled by the existing, unchanged support-policy
            // default -- no new catalog authority.
            var nonAnchor = Assert.Single(week.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.SupportPolicy);
            Assert.Equal("EASY_STANDARD", nonAnchor.WorkoutId);

            Assert.Equal(new PlanCatalogReference("PREPARATION_RUNWAY_LAYOUT_2D_MODEL_B_V1", 1), week.Provenance.SourceLayout);
        }

        Assert.Equal(3, weeks.Count(w => w.RunwayWeekNumber % 2 == 1));
        Assert.Equal(2, weeks.Count(w => w.RunwayWeekNumber % 2 == 0));
    }

    [Fact]
    public void TwoDayBlockRolePolicies_NoLongerThrows_GEN29ResolvedTheReconciliation()
    {
        // Phase 10K-GEN.29 superseded this guard: GEN.28 §9 (Candidate C) and
        // this phase's frozen AerobicStrength governing decision resolved the
        // anchor/content-family reconciliation this test used to guard as an
        // open gap. BuildBlockRolePolicies(2) now returns the same,
        // frequency-independent block-role policy set every other frequency
        // already uses -- the real fix lives in
        // PreparationRunwayWeekMaterializer's role-conditioned Pattern-A/
        // Pattern-B content selection, not in this policy. See
        // Gen29TwoDayRunwayBlockRoleReconciliationTests for the full,
        // GEN.28 §15-required test contract covering the actual resolution.
        var policies = TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(2);
        var fourDayPolicies = TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(4);
        Assert.Equal(fourDayPolicies.Count, policies.Count);
        foreach (var block in fourDayPolicies)
        {
            var twoDay = policies.Single(p => p.BlockKey == block.BlockKey);
            Assert.Equal(block.AnchorRoleByProgressionStep, twoDay.AnchorRoleByProgressionStep);
        }
    }

    [Fact]
    public void FourDayRunway_LayoutAndBlockRolePolicies_AreByteIdenticalAfterGen27()
    {
        // Zero-delta guard: GEN.27 parameterized BuildBlockRolePolicies by
        // DaysPerWeek. This asserts the pre-GEN.27 (4D) call still produces
        // the exact same anchor-role assignments as before.
        var policies = TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(4);

        var consistency = policies.Single(p => p.BlockKey == PreparationRunwayBlockType.Consistency);
        Assert.Equal(PreparationRunwaySlotRole.KeySession, consistency.AnchorRoleByProgressionStep[1]);
        Assert.Equal(PreparationRunwaySlotRole.LongRun, consistency.AnchorRoleByProgressionStep[2]);

        var aerobic = policies.Single(p => p.BlockKey == PreparationRunwayBlockType.AerobicStrength);
        Assert.All(aerobic.AnchorRoleByProgressionStep.Values, r => Assert.Equal(PreparationRunwaySlotRole.KeySession, r));

        var layout = TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(4);
        Assert.Null(layout.WeeklyPatternRoles);
        Assert.Null(layout.PatternPeriodWeeks);
        Assert.Equal(
        [
            PreparationRunwaySlotRole.KeySession,
            PreparationRunwaySlotRole.EasySupport,
            PreparationRunwaySlotRole.EasySupport,
            PreparationRunwaySlotRole.LongRun,
        ], layout.OrderedRoles);
    }

    /// <summary>
    /// Builds a real (not fabricated skeleton) single-block 8-week 2D Runway
    /// materialization request, bypassing <see cref="PreparationRunwayBlockAllocationEngine"/>
    /// (which enforces the real multi-block allocation mix, always including
    /// at least one QUALITY/EASY-anchored block -- exactly the still-open
    /// question this file does not attempt to resolve) in favor of a single,
    /// hand-built <see cref="PreparationRunwayBlockAllocationOutcome{TKey}"/>
    /// for GeneralEndurance alone, whose real catalog progression is already
    /// LONG_RUN-family and anchors LONG_RUN unmodified for every existing
    /// frequency -- isolating the pattern-selection mechanism under real
    /// (not synthetic) workout content.
    /// </summary>
    private static async Task<PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>> BuildSingleBlockTwoDayRequestAsync(
        int runwayWeeks)
    {
        var catalogDefinition = await PreparationRunwayBlockProgressionCatalogReader.LoadAsync(
            CatalogRoot(), "TEN_K_GENERAL_ENDURANCE_PROGRESSION", 1);
        var typedDefinition = new PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>(
            catalogDefinition.ProgressionId,
            catalogDefinition.Version,
            PreparationRunwayBlockType.GeneralEndurance,
            catalogDefinition.OrderedSteps);

        var allocation = new PreparationRunwayBlockAllocationOutcome<PreparationRunwayBlockType>(
            PreparationRunwayBlockType.GeneralEndurance, runwayWeeks, CanonicalOrder: 1);

        var bindingResult = PreparationRunwayBlockWorkoutBindingEngine.Bind(
            new PreparationRunwayBlockWorkoutBindingRequest<PreparationRunwayBlockType>(
                allocation.BlockKey, allocation.AllocatedWeeks, typedDefinition));
        Assert.True(bindingResult.IsSuccess, bindingResult.FailureReason);

        var binding = new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
            allocation.BlockKey,
            bindingResult.Binding!,
            typedDefinition.ProgressionId,
            typedDefinition.Version,
            Enumerable.Range(1, allocation.AllocatedWeeks).ToArray());

        var rolePolicy = new PreparationRunwayBlockWeekRolePolicy<PreparationRunwayBlockType>(
            PreparationRunwayBlockType.GeneralEndurance,
            CanonicalOrder: 1,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BlockRolePolicyId,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BlockRolePolicyVersion,
            Enumerable.Range(1, runwayWeeks).ToDictionary(step => step, _ => PreparationRunwaySlotRole.LongRun));

        return new PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>(
            "TEN_K__2D__INTERMEDIATE",
            "TEN_K__2D__INTERMEDIATE",
            1,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(2),
            [allocation],
            [binding],
            [rolePolicy],
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy());
    }
}
