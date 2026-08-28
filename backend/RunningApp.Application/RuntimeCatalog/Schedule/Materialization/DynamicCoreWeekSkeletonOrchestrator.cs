namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4G.5D typed input to
/// <see cref="IDynamicCoreWeekSkeletonOrchestrator"/>. Unlike the fixed-week
/// Phase 4F.3 <see cref="CatalogPlanSkeletonOrchestrationContext"/> (which
/// always uses <c>candidate.CoreCycle.DefaultWeeks</c>), this context accepts
/// an explicit <see cref="TargetWeekCount"/> for any mathematically feasible
/// standalone-core length (8-14 for <c>TEN_K_MASTER v6</c>).
/// The live catalog composition now reaches this layer through the dynamic
/// calendar pipeline; horizon admission remains owned upstream.
/// </summary>
internal sealed class DynamicCoreWeekSkeletonOrchestrationContext
{
    /// <summary>The already-loaded candidate. Never reloaded or re-searched.</summary>
    public required PlanCatalogCandidateSummary Candidate { get; init; }

    /// <summary>The requested standalone-core week count. Consumed exactly as given -- this orchestrator never derives, guesses, or defaults it.</summary>
    public required int TargetWeekCount { get; init; }

    public required DateOnly StartDate { get; init; }

    public required DateOnly AsOfDate { get; init; }
}

/// <summary>Backend Integration Phase 4G.5D result — mirrors <see cref="CatalogPlanSkeletonOrchestrationResult"/>'s shape, plus the generic <see cref="PhaseAllocationResult"/> that produced it.</summary>
internal sealed class DynamicCoreWeekSkeletonOrchestrationResult
{
    public required PhaseAllocationResult PhaseAllocation { get; init; }
    public required GeneratedCatalogPlanSkeleton Skeleton { get; init; }
    public required GeneratedCatalogPlanSkeletonValidationResult Validation { get; init; }
}

/// <summary>Thrown when <see cref="ICatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary, int)"/> reports no mathematically feasible allocation for the requested target week count.</summary>
internal sealed class DynamicCoreWeekSkeletonInfeasibleException : Exception
{
    public DynamicCoreWeekSkeletonInfeasibleException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when the existing Phase 4F.2 materializer or Phase 4F.2 output validator rejects the translated allocation.</summary>
internal sealed class DynamicCoreWeekSkeletonMaterializationFailedException : Exception
{
    public DynamicCoreWeekSkeletonMaterializationFailedException(string message) : base(message)
    {
    }

    public DynamicCoreWeekSkeletonMaterializationFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Backend Integration Phase 4G.5D — generalizes Phase 4F.3's
/// <see cref="CatalogPlanSkeletonOrchestrator"/> to any mathematically
/// feasible standalone-core target week count, by sourcing the phase
/// allocation from <see cref="ICatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary, int)"/>
/// (the existing, already-closed-governance, generic allocator overload —
/// see TD-ALLOCATION-PRIORITY-001 and TD-FOUNDATION-COMPRESSION-001) instead
/// of the fixed <c>Resolve(candidate)</c> overload that always returns
/// <c>candidate.CoreCycle.DefaultWeeks</c>.
///
/// This orchestrator performs NO week-layout, session-slot, or date-boundary
/// logic of its own: every week/slot/date computation is delegated,
/// unmodified, to the existing Phase 4F.2 <see cref="ICatalogStageToWeekMaterializer"/>
/// (whose own Decision 1 date convention -- week N start = StartDate +
/// (N-1)*7 days, end = start+6 days -- is reused exactly as already
/// established; no new date-boundary convention is derived here) and Phase
/// 4F.2 <see cref="IGeneratedCatalogPlanSkeletonValidator"/>. This
/// orchestrator's only job is translating <see cref="PhaseAllocationResult.Phases"/>
/// (consumed in the exact order the resolver returns it -- never reordered,
/// re-derived, or re-validated) into the materializer's existing
/// <see cref="CatalogStageWeekAllocation"/> shape, mirroring the terminology
/// adapter <see cref="CatalogStageToWeekContextFactory"/> already established
/// for the fixed-week path.
///
/// Zero production call sites: not invoked by <c>CatalogPreviewGenerator</c>,
/// any controller, or any live request path. See
/// <c>DynamicCoreWeekSkeletonOrchestratorTests.DarkReachability_NoProductionCallSite</c>
/// for the structural reachability proof.
/// </summary>
internal interface IDynamicCoreWeekSkeletonOrchestrator
{
    DynamicCoreWeekSkeletonOrchestrationResult Build(DynamicCoreWeekSkeletonOrchestrationContext context);
}

/// <inheritdoc cref="IDynamicCoreWeekSkeletonOrchestrator"/>
internal sealed class DynamicCoreWeekSkeletonOrchestrator : IDynamicCoreWeekSkeletonOrchestrator
{
    public const string Version = "PHASE_4G_5D_DYNAMIC_CORE_WEEK_SKELETON_ORCHESTRATOR_V1";

    private readonly ICatalogPhaseAllocationResolver _phaseAllocationResolver;
    private readonly ICatalogRunLayoutResolver _runLayoutResolver;
    private readonly ICatalogStageToWeekMaterializer _materializer;
    private readonly IGeneratedCatalogPlanSkeletonValidator _skeletonValidator;

    public DynamicCoreWeekSkeletonOrchestrator(
        ICatalogPhaseAllocationResolver phaseAllocationResolver,
        ICatalogRunLayoutResolver runLayoutResolver,
        ICatalogStageToWeekMaterializer materializer,
        IGeneratedCatalogPlanSkeletonValidator skeletonValidator)
    {
        _phaseAllocationResolver = phaseAllocationResolver;
        _runLayoutResolver = runLayoutResolver;
        _materializer = materializer;
        _skeletonValidator = skeletonValidator;
    }

    public DynamicCoreWeekSkeletonOrchestrationResult Build(DynamicCoreWeekSkeletonOrchestrationContext context)
    {
        var candidate = context.Candidate;

        // Step 1: mechanical phase allocation for the requested target week
        // count. Consumed exactly as returned -- this orchestrator never
        // reorders Phases, never re-derives compression/extension priority,
        // and never re-validates whether the allocation is "approved" (that
        // is AllocationOrderCorrectnessVerifier's own, separate, closed
        // scope).
        var allocationResult = _phaseAllocationResolver.Resolve(candidate, context.TargetWeekCount);

        if (!allocationResult.IsMathematicallyFeasible)
        {
            throw new DynamicCoreWeekSkeletonInfeasibleException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has no mathematically " +
                $"feasible phase allocation for targetWeekCount={context.TargetWeekCount}: {allocationResult.ReasonCode}.");
        }

        // Step 2: run-layout slots -- identical resolver to the fixed-week path.
        var runLayout = _runLayoutResolver.Resolve(candidate);

        // Only the four directly-loaded dependency roles, matching
        // CatalogStageToWeekContextFactory.Create's own established scope.
        var dependencyVersions = new Dictionary<string, PlanCatalogReference>
        {
            ["masterTemplate"] = candidate.MasterTemplate,
            ["layout"] = candidate.Layout,
            ["levelModifier"] = candidate.LevelModifier,
            ["rulePack"] = candidate.RulePack,
        };

        // Step 3: terminology adapter -- PhaseAllocationResult.Phases (phase
        // terminology) into CatalogStageWeekAllocation (the materializer's
        // existing stage-terminology shape), order preserved exactly.
        var selectedStageSequence = allocationResult.Phases.Select(p => p.PhaseKey).ToList();
        var stageWeekAllocations = allocationResult.Phases
            .Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks))
            .ToList();

        var materializationContext = new CatalogStageToWeekMaterializationContext
        {
            StartDate = context.StartDate,
            AsOfDate = context.AsOfDate,
            PlannedWeekCount = context.TargetWeekCount,
            DaysPerWeek = runLayout.StructuralRoles.Count,
            CanonicalDistanceFamily = candidate.CanonicalDistanceFamily,
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = dependencyVersions,
            SelectedStageSequence = selectedStageSequence,
            StageWeekAllocations = stageWeekAllocations,
            RunLayout = runLayout.Layout,
            RunLayoutSlotRoles = runLayout.StructuralRoles,
            RunLayoutWeeklyPatternRoles = runLayout.WeeklyPatternRoles,
            PatternPeriodWeeks = runLayout.PatternPeriodWeeks,
        };

        // Step 4: delegate to the existing, unmodified Phase 4F.2 materializer.
        GeneratedCatalogPlanSkeleton skeleton;
        try
        {
            skeleton = _materializer.Materialize(materializationContext).Skeleton;
        }
        catch (Exception ex) when (ex is CatalogStageAllocationInvalidException or CatalogStageSequenceInvalidException
            or CatalogStageWeekCountMismatchException or CatalogRunLayoutInvalidException or CatalogSessionSlotCountMismatchException)
        {
            throw new DynamicCoreWeekSkeletonMaterializationFailedException(
                $"Materialization failed for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount}: {ex.Message}", ex);
        }

        // Step 5: delegate to the existing, unmodified Phase 4F.2 output validator.
        var validation = _skeletonValidator.Validate(skeleton);
        if (!validation.IsValid)
        {
            throw new DynamicCoreWeekSkeletonMaterializationFailedException(
                $"Materialized skeleton for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount} failed output validation: " +
                string.Join(", ", validation.Errors) + ".");
        }

        return new DynamicCoreWeekSkeletonOrchestrationResult
        {
            PhaseAllocation = allocationResult,
            Skeleton = skeleton,
            Validation = validation,
        };
    }
}
