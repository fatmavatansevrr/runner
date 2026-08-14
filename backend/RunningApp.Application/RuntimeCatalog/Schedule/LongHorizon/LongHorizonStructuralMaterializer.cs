using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.5 — the single dark, unwired structural materializer joining the
/// Phase 4I.3 typed <see cref="LongHorizonCompositionDecision"/>, the Phase
/// 4I.5 GE structural selector (<see cref="LongHorizonGeStructuralSelector"/>,
/// itself a backend mirror of Phase 4I.4's plan-catalog selector), and the
/// EXISTING, UNCHANGED Preparation Runway structural materializer
/// (<see cref="PreparationRunwayWeekMaterializer"/>, reached via the same
/// allocation/progression-loading/binding stages
/// <c>TenKPreparationRunwayDarkOrchestrator</c> already uses) and Core
/// structural materializer (<see cref="CatalogStageToWeekMaterializer"/>)
/// into one contiguous, globally-numbered 21-52 week skeleton.
///
/// Never recalculates GE/Runway/Core durations or profile (both are taken
/// verbatim from the already-validated <see cref="LongHorizonCompositionDecision"/>),
/// never selects a date (an arbitrary, discarded anchor date is fed to the
/// Core structural materializer only because its context type requires one;
/// no date field from that intermediate result is ever copied into the
/// output skeleton -- see Part 20/24 of the phase document), never executes
/// numeric progression or pace, never persists, and exposes no public DTO.
/// Not called from any live request path -- reachable by tests only, per the
/// established <c>LongHorizonCompositionResolver</c>/
/// <c>TenKPreparationRunwayDarkOrchestrator</c> precedent.
/// </summary>
internal static class LongHorizonStructuralMaterializer
{
    public const string MaterializerId = "LONG_HORIZON_STRUCTURAL_MATERIALIZER";
    public const string MaterializerVersion = "PHASE_4I_5_V1";

    public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int CandidateVersion = 10;

    private static readonly IReadOnlyList<string> CoreStageSequence = ["FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER"];

    private static readonly IReadOnlyList<CatalogStageWeekAllocation> CoreStageAllocations =
    [
        new("FOUNDATION", 3),
        new("BUILD", 4),
        new("RACE_SPECIFIC", 4),
        new("TAPER", 1),
    ];

    private static readonly PlanCatalogReference CoreRunLayout = new("RUN_LAYOUT_4D", 2);
    private static readonly IReadOnlyList<string> CoreRunLayoutSlotRoles = ["KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN"];

    /// <summary>Arbitrary, never-surfaced anchor date -- <see cref="CatalogStageToWeekMaterializationContext"/> requires one, but no date field of its result is ever copied into <see cref="LongHorizonGeneratedStructuralSkeleton"/> (Phase 4I.5 explicitly performs no calendar execution).</summary>
    private static readonly DateOnly DiscardedAnchorDate = new(2000, 1, 1);

    public static async Task<LongHorizonGeneratedStructuralSkeleton> MaterializeAsync(
        LongHorizonCompositionDecision decision,
        string catalogRoot,
        ICatalogWorkoutDefinitionLoader workoutLoader,
        CancellationToken ct = default)
    {
        if (decision is null) throw new ArgumentNullException(nameof(decision));
        if (workoutLoader is null) throw new ArgumentNullException(nameof(workoutLoader));
        if (string.IsNullOrWhiteSpace(catalogRoot)) throw new ArgumentException("catalogRoot is required.", nameof(catalogRoot));

        if (decision.HorizonPath != LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore)
            throw new InvalidOperationException(
                $"LongHorizonStructuralMaterializer only accepts HorizonPath={LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore}; received {decision.HorizonPath}.");
        if (decision.Eligibility != LongHorizonEligibility.SupportedLongHorizon)
            throw new InvalidOperationException($"Decision eligibility must be SupportedLongHorizon; was {decision.Eligibility}.");
        if (decision.ReadinessProfile is not { } profile)
            throw new InvalidOperationException("Decision must carry a resolved ReadinessProfile.");
        if (decision.GeneralEnduranceWeeks is not { } geWeeks || geWeeks is < 1 or > 32)
            throw new InvalidOperationException($"GeneralEnduranceWeeks must be 1..32; was {decision.GeneralEnduranceWeeks?.ToString() ?? "null"}.");
        if (decision.PreparationRunwayWeeks != 8)
            throw new InvalidOperationException($"PreparationRunwayWeeks must be exactly 8; was {decision.PreparationRunwayWeeks}.");
        if (decision.CoreWeeks != 12)
            throw new InvalidOperationException($"CoreWeeks must be exactly 12; was {decision.CoreWeeks}.");
        if (decision.AvailableFullWeeks != geWeeks + 8 + 12)
            throw new InvalidOperationException(
                $"AvailableFullWeeks ({decision.AvailableFullWeeks}) does not equal GE+Runway+Core ({geWeeks}+8+12).");

        var geDescriptors = LongHorizonGeStructuralSelector.Select(geWeeks, profile);
        if (geDescriptors.Count != geWeeks)
            throw new InvalidOperationException($"GE selector returned {geDescriptors.Count} weeks, expected {geWeeks}.");

        var runwayWeeks = await MaterializeRunwayAsync(profile, catalogRoot, workoutLoader, ct);
        if (runwayWeeks.Count != 8)
            throw new InvalidOperationException($"Preparation Runway materializer returned {runwayWeeks.Count} weeks, expected 8.");

        var coreWeeks = MaterializeCore();
        if (coreWeeks.Count != 12)
            throw new InvalidOperationException($"Core materializer returned {coreWeeks.Count} weeks, expected 12.");

        var weeks = new List<LongHorizonStructuralWeek>(geWeeks + 8 + 12);
        var globalWeekNumber = 1;

        foreach (var ge in geDescriptors)
            weeks.Add(BuildGeWeek(ge, globalWeekNumber++));

        foreach (var rw in runwayWeeks)
            weeks.Add(BuildRunwayWeek(rw, globalWeekNumber++));

        foreach (var cw in coreWeeks)
            weeks.Add(BuildCoreWeek(cw, globalWeekNumber++));

        var skeleton = new LongHorizonGeneratedStructuralSkeleton(
            TotalWeeks: decision.AvailableFullWeeks,
            GeneralEnduranceWeeks: geWeeks,
            PreparationRunwayWeeks: 8,
            CoreWeeks: 12,
            ReadinessProfile: profile,
            CandidateKey: CandidateKey,
            CandidateVersion: CandidateVersion,
            CompositionPolicyId: decision.PolicyId,
            CompositionPolicyVersion: decision.PolicyVersion,
            MaterializerId: MaterializerId,
            MaterializerVersion: MaterializerVersion,
            Weeks: weeks);

        var validation = LongHorizonStructuralValidator.Validate(skeleton);
        if (!validation.IsValid)
            throw new InvalidOperationException(
                "LongHorizonStructuralMaterializer produced an invalid skeleton: " + string.Join("; ", validation.Findings));

        return skeleton;
    }

    // ── GE segment ───────────────────────────────────────────────────────

    private static LongHorizonStructuralWeek BuildGeWeek(LongHorizonGeWeekDescriptor ge, int globalWeekNumber)
    {
        var slots = new List<LongHorizonStructuralWorkoutSlot>(4)
        {
            BuildGeSlot(1, "KEY_SESSION", ge.Roles[LongHorizonGeWeekRole.KeySession]),
            BuildGeSlot(2, "EASY_SUPPORT", ge.Roles[LongHorizonGeWeekRole.EasySupportA]),
            BuildGeSlot(3, "EASY_SUPPORT", ge.Roles[LongHorizonGeWeekRole.EasySupportB]),
            BuildGeSlot(4, "LONG_RUN", ge.Roles[LongHorizonGeWeekRole.LongRun]),
        };

        return new LongHorizonStructuralWeek(
            GlobalWeekNumber: globalWeekNumber,
            LocalSegmentWeekNumber: ge.WeekIndex,
            Segment: LongHorizonSegmentType.LongHorizonGeneralEndurance,
            WeekType: LongHorizonGeWeekDescriptor.LongHorizonGeneralEnduranceSegmentType,
            RunwayBlock: null,
            CorePhase: null,
            GeClassification: ge.Classification,
            GeStageFamily: ge.StageFamily,
            MesocycleIndex: ge.MesocycleIndex,
            MesocyclePosition: ge.MesocyclePosition,
            IsRecoveryWeek: ge.IsRecoveryWeek,
            IsTerminalAlignment: ge.IsTerminalAlignment,
            OrderedWorkoutSlots: slots);
    }

    private static LongHorizonStructuralWorkoutSlot BuildGeSlot(int index, string role, LongHorizonGeWorkoutReference reference) =>
        new(index, role, reference.Key, reference.Version, LongHorizonSegmentType.LongHorizonGeneralEndurance);

    // ── Preparation Runway segment (reuses the existing, unchanged materializer) ──

    private static async Task<IReadOnlyList<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>>> MaterializeRunwayAsync(
        ReadinessProfile profile, string catalogRoot, ICatalogWorkoutDefinitionLoader workoutLoader, CancellationToken ct)
    {
        var allocationProfile = profile == ReadinessProfile.ConsistencyNeeded
            ? PreparationRunwayAllocationProfile.ConsistencyNeeded
            : PreparationRunwayAllocationProfile.CoreEntryReady;

        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(allocationProfile);
        var allocationResult = PreparationRunwayBlockAllocationEngine.Allocate(8, policies);
        if (!allocationResult.IsSuccess || allocationResult.Allocations is null)
            throw new InvalidOperationException($"Preparation Runway allocation failed: {allocationResult.FailureReason}");

        var referencePolicy = TenKPreparationRunwayProgressionPolicyFactory.Build();
        var progressionLoader = new TenKPreparationRunwayProgressionLoader(catalogRoot, workoutLoader);

        var loadedByBlock = new Dictionary<PreparationRunwayBlockType, PreparationRunwayBlockProgressionDefinition<PreparationRunwayBlockType>>();
        foreach (var block in allocationResult.Allocations.Where(a => a.AllocatedWeeks > 0).OrderBy(a => a.CanonicalOrder))
        {
            if (!referencePolicy.TryGetValue(block.BlockKey, out var reference))
                throw new InvalidOperationException($"No typed progression reference exists for positive block '{block.BlockKey}'.");
            loadedByBlock[block.BlockKey] = await progressionLoader.LoadValidatedAsync(block.BlockKey, reference, ct);
        }

        var bindings = new List<PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>>();
        foreach (var block in allocationResult.Allocations.Where(a => a.AllocatedWeeks > 0).OrderBy(a => a.CanonicalOrder))
        {
            var definition = loadedByBlock[block.BlockKey];
            var bound = PreparationRunwayBlockWorkoutBindingEngine.Bind(
                new PreparationRunwayBlockWorkoutBindingRequest<PreparationRunwayBlockType>(block.BlockKey, block.AllocatedWeeks, definition));
            if (!bound.IsSuccess || bound.Binding is null)
                throw new InvalidOperationException($"Preparation Runway workout binding failed for block '{block.BlockKey}': {bound.FailureReason}");
            var steps = Enumerable.Range(1, block.AllocatedWeeks).ToArray();
            bindings.Add(new PreparationRunwayMaterializationBlockBinding<PreparationRunwayBlockType>(
                block.BlockKey, bound.Binding, definition.ProgressionId, definition.Version, steps));
        }

        var structural = await PreparationRunwayWeekMaterializer.MaterializeAsync(
            new PreparationRunwayWeekMaterializationRequest<PreparationRunwayBlockType>(
                allocationProfile.ToString(),
                CandidateKey,
                CandidateVersion,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyId,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.AllocationPolicyVersion,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(),
                allocationResult.Allocations,
                bindings,
                TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(),
                TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildSupportPolicy()),
            workoutLoader, ct);

        if (!structural.IsSuccess || structural.Weeks is null)
            throw new InvalidOperationException($"Preparation Runway structural materialization failed: {structural.FailureReason}");

        return structural.Weeks;
    }

    private static string RunwayRoleLabel(PreparationRunwaySlotRole role) => role switch
    {
        PreparationRunwaySlotRole.KeySession => "KEY_SESSION",
        PreparationRunwaySlotRole.EasySupport => "EASY_SUPPORT",
        PreparationRunwaySlotRole.LongRun => "LONG_RUN",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static LongHorizonStructuralWeek BuildRunwayWeek(PreparationRunwayMaterializedWeek<PreparationRunwayBlockType> rw, int globalWeekNumber)
    {
        var slots = rw.OrderedWorkoutSlots
            .OrderBy(s => s.SlotOrdinal)
            .Select(s => new LongHorizonStructuralWorkoutSlot(
                s.SlotOrdinal, RunwayRoleLabel(s.SlotRole), s.WorkoutId, s.WorkoutVersion, LongHorizonSegmentType.PreparationRunway))
            .ToList();

        return new LongHorizonStructuralWeek(
            GlobalWeekNumber: globalWeekNumber,
            LocalSegmentWeekNumber: rw.RunwayWeekNumber,
            Segment: LongHorizonSegmentType.PreparationRunway,
            WeekType: "PREPARATION_RUNWAY",
            RunwayBlock: rw.BlockType.ToString(),
            CorePhase: null,
            GeClassification: null,
            GeStageFamily: null,
            MesocycleIndex: null,
            MesocyclePosition: null,
            IsRecoveryWeek: null,
            IsTerminalAlignment: null,
            OrderedWorkoutSlots: slots);
    }

    // ── Core segment (reuses the existing, unchanged pure structural materializer) ──

    private static IReadOnlyList<GeneratedCatalogWeekSkeleton> MaterializeCore()
    {
        var context = new CatalogStageToWeekMaterializationContext
        {
            StartDate = DiscardedAnchorDate,
            AsOfDate = DiscardedAnchorDate,
            PlannedWeekCount = 12,
            DaysPerWeek = 4,
            CanonicalDistanceFamily = "TEN_K",
            CandidateKey = CandidateKey,
            CandidateVersion = CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference> { ["layout"] = CoreRunLayout },
            SelectedStageSequence = CoreStageSequence,
            StageWeekAllocations = CoreStageAllocations,
            RunLayout = CoreRunLayout,
            RunLayoutSlotRoles = CoreRunLayoutSlotRoles,
        };

        var materializer = new CatalogStageToWeekMaterializer();
        return materializer.Materialize(context).Skeleton.Weeks;
    }

    private static LongHorizonStructuralWeek BuildCoreWeek(GeneratedCatalogWeekSkeleton cw, int globalWeekNumber)
    {
        var slots = cw.SessionSlots
            .OrderBy(s => s.SlotOrderInWeek)
            .Select(s => new LongHorizonStructuralWorkoutSlot(
                s.SlotOrderInWeek, s.StructuralRole, null, null, LongHorizonSegmentType.Core))
            .ToList();

        return new LongHorizonStructuralWeek(
            GlobalWeekNumber: globalWeekNumber,
            LocalSegmentWeekNumber: cw.WeekNumber,
            Segment: LongHorizonSegmentType.Core,
            WeekType: cw.StageKey,
            RunwayBlock: null,
            CorePhase: cw.StageKey,
            GeClassification: null,
            GeStageFamily: null,
            MesocycleIndex: null,
            MesocyclePosition: null,
            IsRecoveryWeek: null,
            IsTerminalAlignment: null,
            OrderedWorkoutSlots: slots);
    }
}
