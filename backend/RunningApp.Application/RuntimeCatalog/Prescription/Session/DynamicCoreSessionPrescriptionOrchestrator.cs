using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

/// <summary>
/// Backend Integration Phase 4G.5G typed input to
/// <see cref="IDynamicCoreSessionPrescriptionOrchestrator"/>. Generalizes the
/// live 12-week pace/effort/duration/repetition/segment prescription step to
/// any mathematically feasible standalone-core target week count (8-14 for
/// <c>TEN_K_MASTER v6</c>), by chaining Phase 4G.5F's volume/long-run
/// orchestrator into the real, unmodified <see cref="ICatalogSessionPrescriptionPlanner"/>
/// and <see cref="ICatalogFinalPrescribedPlanFinalizer"/>.
/// The live catalog composition reaches this layer through the dynamic
/// calendar pipeline; it does not independently admit a horizon.
/// </summary>
internal sealed class DynamicCoreSessionPrescriptionContext
{
    public required PlanCatalogCandidateSummary Candidate { get; init; }

    /// <summary>The requested standalone-core week count. Consumed exactly as given.</summary>
    public required int TargetWeekCount { get; init; }

    public required DateOnly StartDate { get; init; }
    public required DateOnly AsOfDate { get; init; }

    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDayPreference { get; init; }

    /// <summary>
    /// Already-resolved runtime-condition results, including PACE_SOURCE_IN
    /// and GOAL_FEASIBILITY_IN -- this orchestrator never calls
    /// PaceSourceResolver/GoalFeasibilityResolver/RuntimeConditionResolutionService
    /// itself. Consumed exactly as given, mirroring how the Phase 4G.5F and
    /// Phase 4G.5E dark orchestrators this one chains onto already treat
    /// ConditionResults as an external input.
    /// </summary>
    public required IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults { get; init; }

    public required DTOs.Plan.GeneratePreviewRequest PreviewRequest { get; init; }
    public required ResolverInputSnapshot ResolverInput { get; init; }

    public required ICatalogWorkoutDefinitionLoader WorkoutDefinitionLoader { get; init; }
    public required ICatalogPeakVolumeBandLoader PeakVolumeBandLoader { get; init; }
}

/// <summary>Backend Integration Phase 4G.5G result — every intermediate artifact plus the final prescribed plan.</summary>
internal sealed class DynamicCoreSessionPrescriptionResult
{
    public required DynamicCoreVolumeAndLongRunResult VolumeResult { get; init; }
    public required CatalogPrescribedPlan BaselinePrescribedPlan { get; init; }
    public required CatalogPrescribedPlan FinalPrescribedPlan { get; init; }
}

/// <summary>Thrown when the real, unmodified session-prescription planner or finalizer rejects the request.</summary>
internal sealed class DynamicCoreSessionPrescriptionFailedException : Exception
{
    public DynamicCoreSessionPrescriptionFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Backend Integration Phase 4G.5G — generalizes the live 12-week
/// pace/effort/duration/repetition/segment prescription step to any
/// mathematically feasible standalone-core target week count, by chaining
/// Phase 4G.5F's <see cref="IDynamicCoreVolumeAndLongRunOrchestrator"/>
/// into the real, unmodified
/// <see cref="ICatalogSessionPrescriptionPlanner"/> (which itself resolves
/// per-session pace via <see cref="CatalogPlanPrescriptionContext"/>'s
/// already-computed pace-source classification -- see
/// <see cref="Prescription.CatalogPrescriptionContextBuilder.Build"/>,
/// unmodified, unchanged since Phase 4G.5E/4G.5F) and
/// <see cref="ICatalogFinalPrescribedPlanFinalizer"/> (which applies
/// <see cref="V1TaperSharpenPrescriptionPolicy"/> -- the existing,
/// already-established "sharpening via prescription, not workout swap"
/// design, AUD-507/AUD-508 -- unmodified).
///
/// This orchestrator performs NO pace derivation, session-parameter
/// calculation, or taper-sharpening logic of its own. It is pure
/// composition. Pace-source resolution (<c>PaceSourceResolver</c>,
/// <c>GoalFeasibilityResolver</c>) is explicitly NOT called by this
/// orchestrator -- <see cref="DynamicCoreSessionPrescriptionContext.ConditionResults"/>
/// is an external input, exactly mirroring how Phase 4G.5E/4G.5F already
/// treat it. The fail-closed philosophy for missing/insufficient pace
/// evidence (Phase 4G.3B.6.1's characterization: reject before generating,
/// never silently substitute) is therefore preserved by construction: this
/// orchestrator never invents a permissive fallback -- it consumes whatever
/// <see cref="ICatalogSessionPrescriptionPlanner"/>'s own existing
/// <c>GuardGoalFeasibilityPaceBoundary</c>/pace-mapping logic (inside the
/// unmodified <see cref="Prescription.CatalogPrescriptionContextBuilder"/>)
/// already enforces, and propagates its exceptions unchanged.
///
/// One minimal, transparent adaptation was required for direct reuse (see
/// PHASE4G_5G_DYNAMIC_CORE_PACE_PRESCRIPTION.md for the full rationale):
/// <see cref="DynamicCoreVolumeAndLongRunResult"/> (Phase 4G.5F, itself
/// dark and owned by this same session) did not previously expose the
/// <see cref="CatalogPlanPrescriptionContext"/> it already builds
/// internally, which <see cref="ICatalogSessionPrescriptionPlanner"/>
/// requires as an input. Re-deriving it a second time here would have
/// duplicated existing logic rather than reusing it, so that type gained
/// one new field instead -- no production planner/resolver/finalizer logic
/// was touched.
///
/// Production reachability is transitive through
/// <c>CatalogPreviewGenerator</c>'s dynamic calendar composition. This layer
/// remains an internal composition step rather than an independent route.
/// </summary>
internal interface IDynamicCoreSessionPrescriptionOrchestrator
{
    Task<DynamicCoreSessionPrescriptionResult> PrescribeAsync(DynamicCoreSessionPrescriptionContext context, CancellationToken ct = default);
}

/// <inheritdoc cref="IDynamicCoreSessionPrescriptionOrchestrator"/>
internal sealed class DynamicCoreSessionPrescriptionOrchestrator : IDynamicCoreSessionPrescriptionOrchestrator
{
    private readonly IDynamicCoreVolumeAndLongRunOrchestrator _volumeOrchestrator;
    private readonly ICatalogSessionPrescriptionPlanner _sessionPrescriptionPlanner;
    private readonly ICatalogFinalPrescribedPlanFinalizer _finalPrescribedPlanFinalizer;

    public DynamicCoreSessionPrescriptionOrchestrator(
        IDynamicCoreVolumeAndLongRunOrchestrator volumeOrchestrator,
        ICatalogSessionPrescriptionPlanner sessionPrescriptionPlanner,
        ICatalogFinalPrescribedPlanFinalizer finalPrescribedPlanFinalizer)
    {
        _volumeOrchestrator = volumeOrchestrator;
        _sessionPrescriptionPlanner = sessionPrescriptionPlanner;
        _finalPrescribedPlanFinalizer = finalPrescribedPlanFinalizer;
    }

    public async Task<DynamicCoreSessionPrescriptionResult> PrescribeAsync(DynamicCoreSessionPrescriptionContext context, CancellationToken ct = default)
    {
        var candidate = context.Candidate;

        // Step 1: Phase 4G.5F dynamic volume/long-run pipeline (8-14 weeks) -- reused unmodified.
        var volumeResult = await _volumeOrchestrator.PlanAsync(new DynamicCoreVolumeAndLongRunContext
        {
            Candidate = candidate,
            TargetWeekCount = context.TargetWeekCount,
            StartDate = context.StartDate,
            AsOfDate = context.AsOfDate,
            PreferredDays = context.PreferredDays,
            LongRunDayPreference = context.LongRunDayPreference,
            ConditionResults = context.ConditionResults,
            PreviewRequest = context.PreviewRequest,
            ResolverInput = context.ResolverInput,
            WorkoutDefinitionLoader = context.WorkoutDefinitionLoader,
            PeakVolumeBandLoader = context.PeakVolumeBandLoader,
        }, ct);

        // Step 2: resolved workout-definition closure -- needed by the
        // session-prescription planner for its own segment/distance logic.
        var definitions = new Dictionary<string, CatalogWorkoutDefinitionSummary>(StringComparer.Ordinal);
        foreach (var reference in candidate.ReferencedWorkouts)
        {
            definitions[reference.Key] = await context.WorkoutDefinitionLoader.LoadAsync(reference, ct);
        }

        // Step 3: baseline per-session prescription (Phase 4F.7A) -- pace,
        // duration, repetition, segments -- the real, unmodified planner,
        // called directly.
        CatalogPrescribedPlan baselinePlan;
        try
        {
            baselinePlan = _sessionPrescriptionPlanner.Build(new CatalogSessionPrescriptionRequest(
                candidate, volumeResult.BindingResult.BoundPlan, volumeResult.PrescriptionContext,
                volumeResult.VolumeAndLongRunPlan, definitions));
        }
        catch (CatalogSessionPrescriptionException ex)
        {
            throw new DynamicCoreSessionPrescriptionFailedException(
                $"Session prescription failed for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount}: {ex.Message}", ex);
        }

        if (!baselinePlan.ValidationResult.IsValid)
        {
            throw new DynamicCoreSessionPrescriptionFailedException(
                $"Baseline session prescription for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount} failed output validation: " +
                string.Join(", ", baselinePlan.ValidationResult.Errors) + ".", new InvalidOperationException("BASELINE_PRESCRIPTION_INVALID"));
        }

        // Step 4: TAPER_SHARPEN completion (V1TaperSharpenPrescriptionPolicy,
        // AUD-507/AUD-508's existing "sharpening via prescription, not
        // workout swap" design) plus final validation -- the real,
        // unmodified finalizer, called directly.
        CatalogPrescribedPlan finalPlan;
        try
        {
            finalPlan = _finalPrescribedPlanFinalizer.Complete(new CatalogFinalPrescriptionRequest(
                candidate, volumeResult.BindingResult.BoundPlan, volumeResult.VolumeAndLongRunPlan, baselinePlan));
        }
        catch (CatalogSessionPrescriptionException ex)
        {
            throw new DynamicCoreSessionPrescriptionFailedException(
                $"Final prescribed-plan completion failed for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount}: {ex.Message}", ex);
        }

        if (!finalPlan.ValidationResult.IsValid)
        {
            throw new DynamicCoreSessionPrescriptionFailedException(
                $"Final prescribed plan for candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' " +
                $"at targetWeekCount={context.TargetWeekCount} failed output validation: " +
                string.Join(", ", finalPlan.ValidationResult.Errors) + ".", new InvalidOperationException("FINAL_PRESCRIPTION_INVALID"));
        }

        return new DynamicCoreSessionPrescriptionResult
        {
            VolumeResult = volumeResult,
            BaselinePrescribedPlan = baselinePlan,
            FinalPrescribedPlan = finalPlan,
        };
    }
}
