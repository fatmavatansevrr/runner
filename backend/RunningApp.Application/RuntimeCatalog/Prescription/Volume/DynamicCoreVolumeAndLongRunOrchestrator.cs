using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Backend Integration Phase 4G.5F typed input to
/// <see cref="IDynamicCoreVolumeAndLongRunOrchestrator"/>. Generalizes the
/// live 12-week weekly-volume/long-run planning step to any mathematically
/// feasible standalone-core target week count (8-14 for
/// <c>TEN_K_MASTER v6</c>), by chaining Phase 4G.5E's binding orchestrator
/// into the real, unmodified <see cref="ICatalogVolumeAndLongRunPlanner"/>.
/// The live catalog composition reaches this layer through the dynamic
/// calendar pipeline; horizon admission remains upstream.
/// </summary>
internal sealed class DynamicCoreVolumeAndLongRunContext
{
    public required PlanCatalogCandidateSummary Candidate { get; init; }

    /// <summary>The requested standalone-core week count. Consumed exactly as given.</summary>
    public required int TargetWeekCount { get; init; }

    public required DateOnly StartDate { get; init; }
    public required DateOnly AsOfDate { get; init; }

    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDayPreference { get; init; }

    public required IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults { get; init; }

    /// <summary>
    /// Carries the runner-readiness fields (RecentWeeklyVolumeKm,
    /// RecentLongestRunKm, RecentRunsPerWeek) that drive
    /// <see cref="ICatalogVolumeAndLongRunPlanner"/>'s starting-volume/long-run
    /// anchor selection -- this is the "runner profile" input varied across
    /// this phase's test matrix. Never read or reinterpreted by this
    /// orchestrator itself; passed through unmodified to the real
    /// <see cref="ICatalogPrescriptionContextBuilder"/>.
    /// </summary>
    public required GeneratePreviewRequest PreviewRequest { get; init; }

    public required Resolvers.ResolverInputSnapshot ResolverInput { get; init; }

    public required ICatalogWorkoutDefinitionLoader WorkoutDefinitionLoader { get; init; }
    public required ICatalogPeakVolumeBandLoader PeakVolumeBandLoader { get; init; }
}

/// <summary>
/// Backend Integration Phase 4G.5F result — the binding-pipeline result plus
/// the final volume/long-run plan. <see cref="PrescriptionContext"/> was
/// added by Phase 4G.5G (a minimal, transparent adaptation to this
/// already-dark, already-owned type -- not a change to any existing
/// production component): the real <see cref="ICatalogSessionPrescriptionPlanner"/>
/// this orchestrator's own output feeds into (Phase 4G.5G) requires the same
/// <see cref="CatalogPlanPrescriptionContext"/> this orchestrator already
/// builds internally, and re-deriving it a second time downstream would
/// duplicate, not reuse, existing logic -- see
/// PHASE4G_5G_DYNAMIC_CORE_PACE_PRESCRIPTION.md for the exact rationale.
/// </summary>
internal sealed class DynamicCoreVolumeAndLongRunResult
{
    public required DynamicCoreWorkoutBindingResult BindingResult { get; init; }
    public required Prescription.CatalogPlanPrescriptionContext PrescriptionContext { get; init; }
    public required CatalogVolumeAndLongRunPlan VolumeAndLongRunPlan { get; init; }
}

/// <summary>Thrown when the real, unmodified <see cref="ICatalogVolumeAndLongRunPlanner"/> rejects the request.</summary>
internal sealed class DynamicCoreVolumeAndLongRunFailedException : Exception
{
    public DynamicCoreVolumeAndLongRunFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Backend Integration Phase 4G.5F — generalizes the live 12-week weekly-
/// volume/long-run planning step to any mathematically feasible standalone-
/// core target week count, by chaining Phase 4G.5E's
/// <see cref="IDynamicCoreWorkoutBindingOrchestrator"/> into the real,
/// unmodified <see cref="ICatalogPrescriptionContextBuilder"/>
/// and <see cref="ICatalogVolumeAndLongRunPlanner"/>.
///
/// <see cref="ICatalogVolumeAndLongRunPlanner"/> was inspected first (per
/// this phase's own instruction) and found to already be fully generic over
/// week count: its only week-count constraint is a runtime range check
/// against <c>candidate.CoreCycle.MinimumWeeks</c>/<c>MaximumWeeks</c>
/// (8-14 for this candidate, already satisfied), and its reachable-peak
/// formula (<c>ResolvePeak</c>) already scales the peak proportionally to
/// the plan's own non-taper transition count rather than assuming a fixed
/// 12-week/10-transition shape. No structural obstacle to direct reuse was
/// found (matching Phase 4G.5D's <c>CatalogStageToWeekMaterializer</c> and
/// Phase 4G.5E's <c>CatalogWorkoutBinder</c>/<c>ProgressionStageAllocator</c>
/// precedent) -- this orchestrator therefore performs no volume/long-run
/// calculation of its own; it is pure composition, calling the real
/// planner's real public entry point directly.
///
/// Production reachability is transitive through
/// <c>CatalogPreviewGenerator</c>'s dynamic calendar composition. This layer
/// remains an internal composition step rather than an independent route.
/// </summary>
internal interface IDynamicCoreVolumeAndLongRunOrchestrator
{
    Task<DynamicCoreVolumeAndLongRunResult> PlanAsync(DynamicCoreVolumeAndLongRunContext context, CancellationToken ct = default);
}

/// <inheritdoc cref="IDynamicCoreVolumeAndLongRunOrchestrator"/>
internal sealed class DynamicCoreVolumeAndLongRunOrchestrator : IDynamicCoreVolumeAndLongRunOrchestrator
{
    private readonly IDynamicCoreWorkoutBindingOrchestrator _bindingOrchestrator;
    private readonly ICatalogPrescriptionContextBuilder _prescriptionContextBuilder;
    private readonly ICatalogVolumeAndLongRunPlanner _volumeAndLongRunPlanner;

    public DynamicCoreVolumeAndLongRunOrchestrator(
        IDynamicCoreWorkoutBindingOrchestrator bindingOrchestrator,
        ICatalogPrescriptionContextBuilder prescriptionContextBuilder,
        ICatalogVolumeAndLongRunPlanner volumeAndLongRunPlanner)
    {
        _bindingOrchestrator = bindingOrchestrator;
        _prescriptionContextBuilder = prescriptionContextBuilder;
        _volumeAndLongRunPlanner = volumeAndLongRunPlanner;
    }

    public async Task<DynamicCoreVolumeAndLongRunResult> PlanAsync(DynamicCoreVolumeAndLongRunContext context, CancellationToken ct = default)
    {
        var candidate = context.Candidate;

        // Step 1: Phase 4G.5E dynamic workout-binding pipeline (8-14 weeks) --
        // reused unmodified.
        var bindingResult = await _bindingOrchestrator.BindAsync(new DynamicCoreWorkoutBindingContext
        {
            Candidate = candidate,
            TargetWeekCount = context.TargetWeekCount,
            StartDate = context.StartDate,
            AsOfDate = context.AsOfDate,
            PreferredDays = context.PreferredDays,
            LongRunDayPreference = context.LongRunDayPreference,
            ConditionResults = context.ConditionResults,
            WorkoutDefinitionLoader = context.WorkoutDefinitionLoader,
        }, ct);

        // Step 2: resolved workout-definition closure -- needed by the
        // prescription-context builder for its own eligibility checks.
        var definitions = new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal);
        foreach (var reference in candidate.ReferencedWorkouts)
        {
            definitions[reference.Key] = await context.WorkoutDefinitionLoader.LoadAsync(reference, ct);
        }

        // Step 3: prescription context (readiness anchors) -- unmodified.
        var prescriptionContext = _prescriptionContextBuilder.Build(new CatalogPrescriptionContextBuildRequest(
            context.PreviewRequest, context.AsOfDate, candidate, context.ResolverInput, context.ConditionResults,
            bindingResult.BoundPlan, definitions));

        // Step 4: peak-volume band -- unmodified.
        var peakVolumeBand = await context.PeakVolumeBandLoader.LoadAsync(
            candidate.PeakVolumeBandPolicy, candidate.CanonicalDistanceFamily, candidate.Level, candidate.DaysPerWeek, ct);

        // Step 5: the real, unmodified weekly-volume/long-run planner,
        // called directly.
        CatalogVolumeAndLongRunPlan volumeAndLongRunPlan;
        try
        {
            volumeAndLongRunPlan = _volumeAndLongRunPlanner.Build(new CatalogVolumePlanningRequest(
                candidate, bindingResult.BoundPlan, prescriptionContext, peakVolumeBand));
        }
        catch (CatalogVolumePlanningException ex)
        {
            throw new DynamicCoreVolumeAndLongRunFailedException(
                $"Volume/long-run planning failed for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount}: {ex.Message}", ex);
        }

        return new DynamicCoreVolumeAndLongRunResult
        {
            BindingResult = bindingResult,
            PrescriptionContext = prescriptionContext,
            VolumeAndLongRunPlan = volumeAndLongRunPlan,
        };
    }
}
