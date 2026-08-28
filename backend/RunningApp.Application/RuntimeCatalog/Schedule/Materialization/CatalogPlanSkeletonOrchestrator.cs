namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.3 — the immutable input to
/// <see cref="ICatalogPlanSkeletonOrchestrator"/>. Carries the already-loaded,
/// already-eligible candidate (loaded and eligibility-checked by the
/// existing <c>ICatalogCandidateEligibilityGate</c>/<c>IPlanCatalogBundleLoader</c>
/// — this orchestrator never loads or searches the catalog itself) plus the
/// caller's own expected identity, used only for defensive integrity
/// checking (Validation steps 1–2/5: proving the loaded bundle is really the
/// one the caller asked for, not a substituted or stale one).
/// </summary>
internal sealed class CatalogPlanSkeletonOrchestrationContext
{
    /// <summary>The already-loaded, already-eligible candidate (e.g. via <c>ICatalogCandidateEligibilityGate.LoadForPublicPreviewAsync</c>/<c>LoadForInternalDryRunAsync</c>). Never reloaded, never re-searched.</summary>
    public required PlanCatalogCandidateSummary Candidate { get; init; }

    /// <summary>The caller's own expected candidate key — must equal <see cref="Candidate"/>'s own <c>CandidateKey</c>.</summary>
    public required string ExpectedCandidateKey { get; init; }

    /// <summary>The caller's own expected candidate version — must equal <see cref="Candidate"/>'s own <c>CandidateVersion</c>.</summary>
    public required int ExpectedCandidateVersion { get; init; }

    /// <summary>The caller's own expected master-template pin — must equal <see cref="Candidate"/>'s own <c>MasterTemplate</c>.</summary>
    public required PlanCatalogReference ExpectedMasterTemplate { get; init; }

    /// <summary>The caller's own expected run-layout pin — must equal <see cref="Candidate"/>'s own <c>Layout</c>.</summary>
    public required PlanCatalogReference ExpectedRunLayout { get; init; }

    /// <summary>The user-selected plan start date, preserved exactly through to the skeleton (Phase 4F.1 Decision 5).</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>The single frozen domain evaluation date — never read from the wall clock by this orchestrator or anything it calls.</summary>
    public required DateOnly AsOfDate { get; init; }
}

/// <summary>
/// Backend Integration Phase 4F.3 — the complete, internal-only result of
/// one orchestration attempt. Never returned raw JSON; never exposed through
/// any public API DTO (see the phase's own public-DTO-boundary tests).
/// </summary>
internal sealed class CatalogPlanSkeletonOrchestrationResult
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required IReadOnlyDictionary<string, PlanCatalogReference> DependencyVersions { get; init; }
    public required CatalogPhaseAllocation PhaseAllocation { get; init; }
    public required CatalogRunLayoutSlots RunLayout { get; init; }
    public required GeneratedCatalogPlanSkeleton Skeleton { get; init; }
    public required GeneratedCatalogPlanSkeletonValidationResult Validation { get; init; }
}

/// <summary>
/// Backend Integration Phase 4F.3 — translates a resolved
/// <see cref="CatalogPhaseAllocation"/> (phase terminology) and
/// <see cref="CatalogRunLayoutSlots"/> into the Phase 4F.2
/// <see cref="CatalogStageToWeekMaterializationContext"/> (stage
/// terminology). This IS the explicit terminology adapter the phase-vs-stage
/// governance decision calls for — see this type's own doc comment on
/// <see cref="Create"/> for the exact mapping and the terminology debt it
/// documents rather than silently resolves.
/// </summary>
internal interface ICatalogStageToWeekContextFactory
{
    CatalogStageToWeekMaterializationContext Create(
        CatalogPlanSkeletonOrchestrationContext input,
        CatalogPhaseAllocation phaseAllocation,
        CatalogRunLayoutSlots runLayout);
}

/// <inheritdoc cref="ICatalogStageToWeekContextFactory"/>
internal sealed class CatalogStageToWeekContextFactory : ICatalogStageToWeekContextFactory
{
    /// <summary>
    /// Terminology adapter (Phase 4F.3 Stage 4 decision): <paramref name="phaseAllocation"/>'s
    /// entries use precise phase terminology (<see cref="CatalogPhaseAllocationEntry.PhaseKey"/>/
    /// <see cref="CatalogPhaseAllocationEntry.PhaseWeekCount"/>), translated
    /// here into Phase 4F.2's existing <see cref="CatalogStageWeekAllocation"/>
    /// shape (<c>StageKey</c>/<c>WeekCount</c>) purely because that is the
    /// literal field name Phase 4F.2 already established and this phase does
    /// not perform a broad rename. The values themselves are NOT
    /// workout-selection <c>stageKey</c>s — they remain phase-granularity
    /// identities throughout; only the C# property name differs. This is
    /// documented terminology debt, not a silent semantic change.
    /// </summary>
    public CatalogStageToWeekMaterializationContext Create(
        CatalogPlanSkeletonOrchestrationContext input,
        CatalogPhaseAllocation phaseAllocation,
        CatalogRunLayoutSlots runLayout)
    {
        var candidate = input.Candidate;

        // Only the four directly-loaded dependency roles -- matching
        // PlanCatalogCandidateSummary.DependencyStatuses' own established
        // scope (Phase 4E.1), not a full 8-role bundle.
        var dependencyVersions = new Dictionary<string, PlanCatalogReference>
        {
            ["masterTemplate"] = candidate.MasterTemplate,
            ["layout"] = candidate.Layout,
            ["levelModifier"] = candidate.LevelModifier,
            ["rulePack"] = candidate.RulePack,
        };

        var selectedStageSequence = phaseAllocation.Entries.Select(e => e.PhaseKey).ToList();
        var stageWeekAllocations = phaseAllocation.Entries
            .Select(e => new CatalogStageWeekAllocation(e.PhaseKey, e.PhaseWeekCount))
            .ToList();

        return new CatalogStageToWeekMaterializationContext
        {
            StartDate = input.StartDate,
            AsOfDate = input.AsOfDate,
            PlannedWeekCount = phaseAllocation.TotalWeeks,
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
    }
}

/// <summary>
/// Backend Integration Phase 4F.3 — connects the live catalog loader and
/// pilot candidate dependency graph to the Phase 4F.2
/// <see cref="ICatalogStageToWeekMaterializer"/>. Internal only: as of this
/// phase, nothing in the live preview/confirm request path calls this
/// orchestrator (Option A — see the phase's own documentation for the
/// rationale). Never accesses HTTP context, the authenticated user, the wall
/// clock, or the database; never reruns route selection, candidate
/// reselection, or runtime-condition resolution; never mutates the loaded
/// candidate; never persists anything.
/// </summary>
internal interface ICatalogPlanSkeletonOrchestrator
{
    CatalogPlanSkeletonOrchestrationResult Build(CatalogPlanSkeletonOrchestrationContext context);
}

/// <inheritdoc cref="ICatalogPlanSkeletonOrchestrator"/>
/// <remarks>
/// Validates in the exact order documented in
/// PHASE4F_3_LIVE_CATALOG_STAGE_ALLOCATION_AND_SKELETON_INTEGRATION.md:
/// (1) candidate identity integrity, (2) master-template reference match,
/// (3) planned-week-count authority (<c>candidate.CoreCycle.DefaultWeeks</c>),
/// (4) phase-allocation validity (incl. total-vs-authority mismatch),
/// (5) run-layout reference match, (6) run-layout slot validity,
/// (7) materialization-context construction, (8) Phase 4F.2 materialization,
/// (9) skeleton output validation. Stops immediately and throws a typed
/// exception on the first failure — never returns a partial skeleton.
/// </remarks>
internal sealed class CatalogPlanSkeletonOrchestrator : ICatalogPlanSkeletonOrchestrator
{
    private readonly ICatalogPhaseAllocationResolver _phaseAllocationResolver;
    private readonly ICatalogRunLayoutResolver _runLayoutResolver;
    private readonly ICatalogStageToWeekContextFactory _contextFactory;
    private readonly ICatalogStageToWeekMaterializer _materializer;
    private readonly IGeneratedCatalogPlanSkeletonValidator _skeletonValidator;

    public CatalogPlanSkeletonOrchestrator(
        ICatalogPhaseAllocationResolver phaseAllocationResolver,
        ICatalogRunLayoutResolver runLayoutResolver,
        ICatalogStageToWeekContextFactory contextFactory,
        ICatalogStageToWeekMaterializer materializer,
        IGeneratedCatalogPlanSkeletonValidator skeletonValidator)
    {
        _phaseAllocationResolver = phaseAllocationResolver;
        _runLayoutResolver = runLayoutResolver;
        _contextFactory = contextFactory;
        _materializer = materializer;
        _skeletonValidator = skeletonValidator;
    }

    public CatalogPlanSkeletonOrchestrationResult Build(CatalogPlanSkeletonOrchestrationContext context)
    {
        var candidate = context.Candidate;

        // ── Step 1: candidate identity integrity ────────────────────────────
        if (candidate.CandidateKey != context.ExpectedCandidateKey || candidate.CandidateVersion != context.ExpectedCandidateVersion)
        {
            throw new CatalogSkeletonContextInvalidException(
                $"Orchestration context expected candidate '{context.ExpectedCandidateKey}' v{context.ExpectedCandidateVersion}, " +
                $"but the loaded candidate is '{candidate.CandidateKey}' v{candidate.CandidateVersion}. The orchestrator " +
                "never re-searches or reselects a candidate -- this indicates the wrong loaded bundle was supplied.");
        }

        // ── Step 2: master-template reference match ─────────────────────────
        if (candidate.MasterTemplate != context.ExpectedMasterTemplate)
        {
            throw new CatalogMasterTemplateReferenceMismatchException(
                $"Expected master template '{context.ExpectedMasterTemplate.Key}' v{context.ExpectedMasterTemplate.Version}, " +
                $"but the loaded candidate's pinned master template is '{candidate.MasterTemplate.Key}' " +
                $"v{candidate.MasterTemplate.Version}.");
        }

        // ── Step 3: planned-week-count authority ────────────────────────────
        var plannedWeekCount = candidate.CoreCycle.DefaultWeeks;

        // ── Step 4: phase-allocation validity ────────────────────────────────
        var phaseAllocation = _phaseAllocationResolver.Resolve(candidate);
        if (phaseAllocation.TotalWeeks != plannedWeekCount)
        {
            throw new CatalogPhaseAllocationTotalMismatchException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}''s phase allocation totals " +
                $"{phaseAllocation.TotalWeeks} weeks, but the authoritative planned week count (CoreCycle.DefaultWeeks) " +
                $"is {plannedWeekCount}. Never normalized, truncated, or padded.");
        }

        // ── Step 5: run-layout reference match ───────────────────────────────
        if (candidate.Layout != context.ExpectedRunLayout)
        {
            throw new CatalogRunLayoutReferenceMismatchException(
                $"Expected run layout '{context.ExpectedRunLayout.Key}' v{context.ExpectedRunLayout.Version}, but the " +
                $"loaded candidate's pinned layout is '{candidate.Layout.Key}' v{candidate.Layout.Version}.");
        }

        // ── Step 6: run-layout slot validity ─────────────────────────────────
        var runLayout = _runLayoutResolver.Resolve(candidate);

        // ── Step 7: materialization-context construction ────────────────────
        var materializationContext = _contextFactory.Create(context, phaseAllocation, runLayout);

        // ── Step 8: Phase 4F.2 materialization ───────────────────────────────
        GeneratedCatalogPlanSkeleton skeleton;
        try
        {
            skeleton = _materializer.Materialize(materializationContext).Skeleton;
        }
        catch (Exception ex) when (ex is CatalogStageAllocationInvalidException or CatalogStageSequenceInvalidException
            or CatalogStageWeekCountMismatchException or CatalogRunLayoutInvalidException or CatalogSessionSlotCountMismatchException)
        {
            throw new CatalogPlanSkeletonOrchestrationFailedException(
                $"Phase 4F.2 materialization failed for candidate '{candidate.CandidateKey}' " +
                $"v{candidate.CandidateVersion}': {ex.Message}", ex);
        }

        // ── Step 9: skeleton output validation ───────────────────────────────
        var validation = _skeletonValidator.Validate(skeleton);
        if (!validation.IsValid)
        {
            throw new CatalogPlanSkeletonOrchestrationFailedException(
                $"Materialized skeleton for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                "failed output validation: " + string.Join(", ", validation.Errors) + ".");
        }

        return new CatalogPlanSkeletonOrchestrationResult
        {
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = materializationContext.DependencyVersions,
            PhaseAllocation = phaseAllocation,
            RunLayout = runLayout,
            Skeleton = skeleton,
            Validation = validation,
        };
    }
}
