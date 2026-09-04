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
/// Phase 10K-GEN.29 -- real dark verification (no HTTP, no public gate, no
/// PostgreSQL persistence) of the GEN.28 §9 (Candidate C) role-conditioned
/// Pattern-A/Pattern-B content selection mechanism this phase implements,
/// plus the frozen user decision for AerobicStrength: Pattern A weeks use
/// the existing, genuine AerobicStrength QUALITY content unchanged; Pattern
/// B weeks use EASY_STANDARD as EASY_SUPPORT (no new content authored,
/// APPROVED_PRODUCT_DEFAULT, WITH_EXPLICIT_STIMULUS_REDUCTION). Exercises
/// the real, unmodified <see cref="PreparationRunwayWeekMaterializer"/>
/// against the real block-progression catalog for each of the four Runway
/// blocks individually (bypassing <see cref="PreparationRunwayBlockAllocationEngine"/>,
/// same isolation technique <c>Gen27TwoDayPreparationRunwayDarkVerificationTests</c>
/// already established, for deterministic control over exactly how many
/// block-local weeks -- and therefore which pattern letters -- each block
/// spans). Implements GEN.28 §15's required test contract for: Pattern A/B
/// materialization (including the AerobicStrength split specifically), no
/// KEY content forced onto EASY_SUPPORT/LONG_RUN, progression ordinal/index
/// semantics (block-local-week, regression guard against occurrence-based
/// drift), and block/A-B boundary correctness. See
/// <c>Gen29TwoDayRunwayDarkOrchestrationTests</c> for the full end-to-end
/// orchestrator-level coverage (admission gates, numeric dispatch, calendar
/// composition, long-run clamp, zero-delta for other frequencies).
/// </summary>
public sealed class Gen29TwoDayRunwayBlockRoleReconciliationTests
{
    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    // ── AerobicStrength: the frozen decision under direct test ──────────────

    [Fact]
    public async Task AerobicStrength_TwoWeekBlock_PatternA_UsesGenuineQualityContent_PatternB_UsesEasyStandardAsEasySupport()
    {
        // AerobicStrength's real catalog capacity is exactly 2 steps
        // (_INTRO then _PROGRESSED) -- allocate both, starting at global week
        // 1 (Pattern A) so week 1 = Pattern A, week 2 = Pattern B, exercising
        // both halves of the frozen decision in one materialization.
        var request = await BuildSingleBlockRequestAsync(
            PreparationRunwayBlockType.AerobicStrength, "TEN_K_AEROBIC_STRENGTH_PROGRESSION", allocatedWeeks: 2);

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        var weeks = result.Weeks!.OrderBy(w => w.RunwayWeekNumber).ToArray();
        Assert.Equal(2, weeks.Length);

        // Pattern A week (global week 1, odd): genuine QUALITY content,
        // unchanged, anchored on KeySession -- exactly the pre-existing
        // Pattern-A behavior, per "Pattern-A weeks ... unchanged".
        var weekA = weeks[0];
        Assert.Equal(1, weekA.RunwayWeekNumber);
        var anchorA = Assert.Single(weekA.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
        Assert.Equal(PreparationRunwaySlotRole.KeySession, anchorA.SlotRole);
        Assert.Equal("AEROBIC_STRENGTH_CONTROLLED_INTRO", anchorA.WorkoutId);
        Assert.DoesNotContain(weekA.OrderedWorkoutSlots, s => s.SlotRole == PreparationRunwaySlotRole.EasySupport);

        // Pattern B week (global week 2, even): NOT scientifically
        // equivalent, NOT a hidden QUALITY substitute -- literally
        // EASY_STANDARD, placed as EASY_SUPPORT, sourced from the
        // materializer's own anchor mechanism (not the generic support
        // default), carrying the block's real progression-step provenance.
        var weekB = weeks[1];
        Assert.Equal(2, weekB.RunwayWeekNumber);
        var anchorB = Assert.Single(weekB.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
        Assert.Equal(PreparationRunwaySlotRole.EasySupport, anchorB.SlotRole);
        Assert.Equal("EASY_STANDARD", anchorB.WorkoutId);
        Assert.Equal(5, anchorB.WorkoutVersion);
        Assert.DoesNotContain(weekB.OrderedWorkoutSlots, s => s.SlotRole == PreparationRunwaySlotRole.KeySession);
        // No AEROBIC_STRENGTH_CONTROLLED_* content anywhere on the Pattern-B week.
        Assert.DoesNotContain(weekB.OrderedWorkoutSlots, s => s.WorkoutId.StartsWith("AEROBIC_STRENGTH_CONTROLLED", StringComparison.Ordinal));

        // Both weeks keep the real progression provenance -- block-local-week
        // indexing is untouched by which content was ultimately selected.
        Assert.Equal(1, weekA.ProgressionStepNumber);
        Assert.Equal(2, weekB.ProgressionStepNumber);
        Assert.Equal("TEN_K_AEROBIC_STRENGTH_PROGRESSION", weekA.ProgressionId);
        Assert.Equal("TEN_K_AEROBIC_STRENGTH_PROGRESSION", weekB.ProgressionId);
    }

    [Fact]
    public async Task AerobicStrength_SingleWeekBlock_StartingOnPatternB_UsesEasyStandard_NeverDropsTheWeek()
    {
        // A one-week AerobicStrength allocation that happens to start on an
        // even global week (Pattern B): the block's step-1 content must
        // still materialize (progression is block-local-week bound, GEN.28
        // §4 -- never silently skipped), using the Pattern-B alternate.
        var request = await BuildSingleBlockRequestAsync(
            PreparationRunwayBlockType.AerobicStrength, "TEN_K_AEROBIC_STRENGTH_PROGRESSION", allocatedWeeks: 1,
            startingRunwayWeekRoles: PatternBRoles);

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        var week = Assert.Single(result.Weeks!);
        var anchor = Assert.Single(week.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
        Assert.Equal(PreparationRunwaySlotRole.EasySupport, anchor.SlotRole);
        Assert.Equal("EASY_STANDARD", anchor.WorkoutId);
        Assert.Equal(1, week.ProgressionStepNumber);
    }

    // ── Consistency / PreSpecificTransition: mechanical, no new content ─────

    [Theory]
    [InlineData("CONSISTENCY", "TEN_K_CONSISTENCY_PROGRESSION")]
    [InlineData("PRE_SPECIFIC_TRANSITION", "TEN_K_PRE_SPECIFIC_TRANSITION_PROGRESSION")]
    public async Task KeySessionAnchoredEasyStandardBlock_PatternB_RedirectsToEasySupport_SameContentNoInvention(
        string blockName, string progressionKey)
    {
        var block = ParseBlock(blockName);
        var request = await BuildSingleBlockRequestAsync(block, progressionKey, allocatedWeeks: 1, startingRunwayWeekRoles: PatternBRoles);

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        var week = Assert.Single(result.Weeks!);
        var anchor = Assert.Single(week.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
        Assert.Equal(PreparationRunwaySlotRole.EasySupport, anchor.SlotRole);
        Assert.Equal("EASY_STANDARD", anchor.WorkoutId);
        Assert.Equal(5, anchor.WorkoutVersion);
    }

    [Theory]
    [InlineData("CONSISTENCY", "TEN_K_CONSISTENCY_PROGRESSION")]
    [InlineData("PRE_SPECIFIC_TRANSITION", "TEN_K_PRE_SPECIFIC_TRANSITION_PROGRESSION")]
    public async Task KeySessionAnchoredEasyStandardBlock_PatternA_UnchangedFromExistingBehavior(
        string blockName, string progressionKey)
    {
        var block = ParseBlock(blockName);
        var request = await BuildSingleBlockRequestAsync(block, progressionKey, allocatedWeeks: 1, startingRunwayWeekRoles: PatternARoles);

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        var week = Assert.Single(result.Weeks!);
        var anchor = Assert.Single(week.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
        Assert.Equal(PreparationRunwaySlotRole.KeySession, anchor.SlotRole);
        Assert.Equal("EASY_STANDARD", anchor.WorkoutId);
    }

    // ── GeneralEndurance: zero conflict, confirm no change ───────────────────

    [Fact]
    public async Task GeneralEndurance_BothPatterns_AlwaysAnchorsLongRun_ZeroConflict()
    {
        var request = await BuildSingleBlockRequestAsync(
            PreparationRunwayBlockType.GeneralEndurance, "TEN_K_GENERAL_ENDURANCE_PROGRESSION", allocatedWeeks: 4);

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.All(result.Weeks!, week =>
        {
            var anchor = Assert.Single(week.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
            Assert.Equal(PreparationRunwaySlotRole.LongRun, anchor.SlotRole);
            Assert.Equal("LONG_RUN_STANDARD", anchor.WorkoutId);
        });
    }

    // ── Regression guards: no KEY content ever forced onto an incompatible role ──

    [Fact]
    public async Task NoWeek_EverPlacesKeySessionOnlyContent_OntoEasySupportOrLongRunRole()
    {
        // Across every block's real materialization (both patterns), no
        // slot with role EASY_SUPPORT or LONG_RUN ever carries the real
        // QUALITY-family AerobicStrength content -- the family-compatibility
        // check (PreparationRunwayWeekMaterializer.ValidateReferenceForRoleAsync)
        // would already reject this, but this test asserts it as an explicit,
        // named regression guard per GEN.28 §15's required test contract.
        var request = await BuildSingleBlockRequestAsync(
            PreparationRunwayBlockType.AerobicStrength, "TEN_K_AEROBIC_STRENGTH_PROGRESSION", allocatedWeeks: 2);
        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);

        foreach (var week in result.Weeks!)
        {
            foreach (var slot in week.OrderedWorkoutSlots.Where(s =>
                         s.SlotRole is PreparationRunwaySlotRole.EasySupport or PreparationRunwaySlotRole.LongRun))
            {
                Assert.DoesNotContain("AEROBIC_STRENGTH_CONTROLLED", slot.WorkoutId, StringComparison.Ordinal);
            }
        }
    }

    // ── Progression ordinal semantics: block-local-week, never occurrence-based ──

    [Fact]
    public async Task ProgressionStepNumber_IsBlockLocalWeek_NeverRoleOccurrenceCount_AcrossBothPatterns()
    {
        // GEN.28 §4/§6: the nth progression item means "week n of this
        // block," never "nth KEY exposure." AerobicStrength's Pattern-B week
        // materializes no KEY_SESSION content at all, yet its progression
        // step number must still advance to 2 (not stay at a "1st KEY
        // exposure" count of 1, and not skip) -- a direct regression guard
        // against occurrence-based re-indexing (Candidate B, found INVALID).
        var request = await BuildSingleBlockRequestAsync(
            PreparationRunwayBlockType.AerobicStrength, "TEN_K_AEROBIC_STRENGTH_PROGRESSION", allocatedWeeks: 2);
        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        var weeks = result.Weeks!.OrderBy(w => w.RunwayWeekNumber).ToArray();

        Assert.Equal([1, 2], weeks.Select(w => w.BlockWeekOrdinal));
        Assert.Equal([1, 2], weeks.Select(w => w.ProgressionStepNumber));
    }

    // ── Block boundary / A-B boundary correctness ────────────────────────────

    [Fact]
    public async Task BlockBoundary_AerobicStrengthFollowingGeneralEndurance_ContinuesGlobalOrdinalAndPatternCorrectly()
    {
        // Real multi-block materialization: GeneralEndurance (3 weeks,
        // global 1-3) immediately followed by AerobicStrength (2 weeks,
        // global 4-5) in the same request -- proves the A/B pattern is keyed
        // by the contiguous GLOBAL week number across a block boundary, not
        // reset or renumbered per block, and that AerobicStrength's own
        // Pattern-A/B split still resolves correctly starting mid-horizon at
        // whatever parity the preceding block's length happens to leave it at.
        var geDefinition = await LoadTypedAsync(PreparationRunwayBlockType.GeneralEndurance, "TEN_K_GENERAL_ENDURANCE_PROGRESSION");
        var asDefinition = await LoadTypedAsync(PreparationRunwayBlockType.AerobicStrength, "TEN_K_AEROBIC_STRENGTH_PROGRESSION");

        var geAllocation = new PreparationRunwayBlockAllocationOutcome<PreparationRunwayBlockType>(
            PreparationRunwayBlockType.GeneralEndurance, 3, CanonicalOrder: 2);
        var asAllocation = new PreparationRunwayBlockAllocationOutcome<PreparationRunwayBlockType>(
            PreparationRunwayBlockType.AerobicStrength, 2, CanonicalOrder: 3);

        var geBinding = BindOrThrow(geDefinition, geAllocation);
        var asBinding = BindOrThrow(asDefinition, asAllocation);

        var request = new PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>(
            "TEN_K__2D__INTERMEDIATE", "TEN_K__2D__INTERMEDIATE", 1,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(2),
            [geAllocation, asAllocation],
            [
                new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
                    geAllocation.BlockKey, geBinding, geDefinition.ProgressionId, geDefinition.Version,
                    Enumerable.Range(1, geAllocation.AllocatedWeeks).ToArray()),
                new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
                    asAllocation.BlockKey, asBinding, asDefinition.ProgressionId, asDefinition.Version,
                    Enumerable.Range(1, asAllocation.AllocatedWeeks).ToArray()),
            ],
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(2),
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy());

        var result = await PreparationRunwayWeekMaterializer.MaterializeAsync(request, Loader());
        Assert.True(result.IsSuccess, result.FailureReason);
        var weeks = result.Weeks!.OrderBy(w => w.RunwayWeekNumber).ToArray();

        Assert.Equal(Enumerable.Range(1, 5), weeks.Select(w => w.RunwayWeekNumber));
        Assert.Equal(
            [PreparationRunwayBlockType.GeneralEndurance, PreparationRunwayBlockType.GeneralEndurance, PreparationRunwayBlockType.GeneralEndurance,
             PreparationRunwayBlockType.AerobicStrength, PreparationRunwayBlockType.AerobicStrength],
            weeks.Select(w => w.BlockType));

        // Global week 4 (AerobicStrength block-local week 1) is Pattern A
        // (even global-week-count boundary from a 3-week GE block => next
        // week is global 4, even => Pattern B). Assert against the real
        // resolved role shape rather than a hardcoded assumption.
        var week4 = weeks.Single(w => w.RunwayWeekNumber == 4);
        var week5 = weeks.Single(w => w.RunwayWeekNumber == 5);
        var week4IsPatternA = week4.OrderedWorkoutSlots.Any(s => s.SlotRole == PreparationRunwaySlotRole.KeySession);
        var week5IsPatternA = week5.OrderedWorkoutSlots.Any(s => s.SlotRole == PreparationRunwaySlotRole.KeySession);
        Assert.NotEqual(week4IsPatternA, week5IsPatternA); // consecutive weeks always alternate
        Assert.Equal(week4.RunwayWeekNumber % 2 == 1, week4IsPatternA); // odd global week => Pattern A, per the frozen convention

        var week4Anchor = Assert.Single(week4.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
        var week5Anchor = Assert.Single(week5.OrderedWorkoutSlots, s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
        Assert.Equal(week4IsPatternA ? "AEROBIC_STRENGTH_CONTROLLED_INTRO" : "EASY_STANDARD", week4Anchor.WorkoutId);
        Assert.Equal(week5IsPatternA ? "AEROBIC_STRENGTH_CONTROLLED_PROGRESSED" : "EASY_STANDARD", week5Anchor.WorkoutId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static int CanonicalOrderFor(PreparationRunwayBlockType block) =>
        TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(2)
            .Single(p => p.BlockKey == block).CanonicalOrder;

    private static PreparationRunwayBlockType ParseBlock(string blockName) => blockName switch
    {
        "CONSISTENCY" => PreparationRunwayBlockType.Consistency,
        "PRE_SPECIFIC_TRANSITION" => PreparationRunwayBlockType.PreSpecificTransition,
        _ => throw new ArgumentOutOfRangeException(nameof(blockName)),
    };

    private static readonly IReadOnlyList<PreparationRunwaySlotRole> PatternARoles =
        [PreparationRunwaySlotRole.KeySession, PreparationRunwaySlotRole.LongRun];
    private static readonly IReadOnlyList<PreparationRunwaySlotRole> PatternBRoles =
        [PreparationRunwaySlotRole.EasySupport, PreparationRunwaySlotRole.LongRun];

    private static async Task<PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>> LoadTypedAsync(
        PreparationRunwayBlockType block, string progressionKey)
    {
        var catalogDefinition = await PreparationRunwayBlockProgressionCatalogReader.LoadAsync(CatalogRoot(), progressionKey, 1);
        return new PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>(
            catalogDefinition.ProgressionId, catalogDefinition.Version, block, catalogDefinition.OrderedSteps);
    }

    private static PreparationRunwayBlockWorkoutBinding<PreparationRunwayBlockType> BindOrThrow(
        PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType> definition,
        PreparationRunwayBlockAllocationOutcome<PreparationRunwayBlockType> allocation)
    {
        var bound = PreparationRunwayBlockWorkoutBindingEngine.Bind(
            new PreparationRunwayBlockWorkoutBindingRequest<PreparationRunwayBlockType>(allocation.BlockKey, allocation.AllocatedWeeks, definition));
        Assert.True(bound.IsSuccess, bound.FailureReason);
        return bound.Binding!;
    }

    /// <summary>
    /// Builds a real, single-block 2D materialization request for the given
    /// block/progression, bypassing <see cref="PreparationRunwayBlockAllocationEngine"/>
    /// for deterministic control of exactly which global weeks (and
    /// therefore which pattern letters) the block spans -- the same
    /// isolation technique <c>Gen27TwoDayPreparationRunwayDarkVerificationTests</c>
    /// already established. When <paramref name="startingRunwayWeekRoles"/>
    /// is supplied, a synthetic single-entry pattern layout starting exactly
    /// at that shape is used (to deterministically force a 1-week block onto
    /// Pattern A or Pattern B); otherwise the real, frozen 2-week Model B
    /// pattern (GEN.11 §1) is used starting at global week 1.
    /// </summary>
    private static async Task<PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>> BuildSingleBlockRequestAsync(
        PreparationRunwayBlockType block, string progressionKey, int allocatedWeeks,
        IReadOnlyList<PreparationRunwaySlotRole>? startingRunwayWeekRoles = null)
    {
        var definition = await LoadTypedAsync(block, progressionKey);
        var allocation = new PreparationRunwayBlockAllocationOutcome<PreparationRunwayBlockType>(block, allocatedWeeks, CanonicalOrder: CanonicalOrderFor(block));
        var binding = BindOrThrow(definition, allocation);

        var layout = startingRunwayWeekRoles is null
            ? TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(2)
            : new PreparationRunwayCanonicalWeeklyLayout(
                new PlanCatalogReference("PREPARATION_RUNWAY_LAYOUT_2D_MODEL_B_V1", 1),
                startingRunwayWeekRoles, [startingRunwayWeekRoles], PatternPeriodWeeks: 1);

        return new PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>(
            "TEN_K__2D__INTERMEDIATE", "TEN_K__2D__INTERMEDIATE", 1,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
            TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion,
            layout,
            [allocation],
            [
                new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
                    block, binding, definition.ProgressionId, definition.Version, Enumerable.Range(1, allocatedWeeks).ToArray()),
            ],
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(2),
            TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy());
    }
}
