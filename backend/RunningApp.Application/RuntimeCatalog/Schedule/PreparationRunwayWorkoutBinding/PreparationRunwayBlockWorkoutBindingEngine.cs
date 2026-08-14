namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

/// <summary>
/// Backend Integration Phase 4G.6A.4B — production-owned, internal, generic,
/// pure (no I/O) binder that combines an already-resolved block-week
/// allocation count with an already-loaded, ordered progression definition
/// and selects the corresponding ordered workout references by exact-prefix
/// semantics. It does not recompute allocation and consumes no upstream
/// horizon/policy quantity at all (no elapsed-week count, no per-block
/// minimum/maximum/weight/priority) -- it reads only the final resolved
/// count and the progression shape, and it does not read any file directly -- see
/// <see cref="PreparationRunwayBlockProgressionCatalogReader"/> for the
/// separate, narrow, dark loader that produces a
/// <see cref="PreparationRunwayBlockProgressionDefinition{TKey}"/> from the
/// real catalog, and <see cref="PreparationRunwayBlockWorkoutReferenceValidator"/>
/// for the separate, narrow, catalog-aware validator that checks the
/// selected workout references actually exist and match the expected
/// semantic. This class is dark: not registered in DI, not invoked by any
/// orchestrator, produces no calendar date, no dated week, no
/// TrainingWeek/TrainingDay, and no public schedule DTO.
/// </summary>
internal static class PreparationRunwayBlockWorkoutBindingEngine
{
    /// <summary>
    /// Runs the full binder algorithm (validate request, canonically order
    /// progression steps, handle zero allocation, validate capacity, select
    /// the exact prefix, validate final invariants). Deterministic: repeated
    /// calls with the same input produce identical output, and the
    /// progression definition's own step array order never affects the
    /// result (steps are always re-sorted by StepNumber before selection).
    /// </summary>
    public static PreparationRunwayBlockWorkoutBindingResult<TKey> Bind<TKey>(
        PreparationRunwayBlockWorkoutBindingRequest<TKey> request)
        where TKey : notnull
    {
        var trace = new List<string> { $"BlockKey={request.BlockKey}, AllocatedWeeks={request.AllocatedWeeks}" };

        // ── Step 1: validate request ─────────────────────────────────────
        if (request.AllocatedWeeks < 0)
            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.InvalidBindingRequest, "AllocatedWeeks cannot be negative.", trace);

        var definition = request.ProgressionDefinition;

        if (definition is null)
        {
            // Preferred behavior for an unsupported/unmapped block: a
            // typed MissingProgressionDefinition result -- except when
            // AllocatedWeeks==0, where there is nothing to select and no
            // progression is needed at all.
            if (request.AllocatedWeeks == 0)
            {
                trace.Add("No progression definition supplied; AllocatedWeeks=0, returning empty success.");
                return PreparationRunwayBlockWorkoutBindingResult<TKey>.Success(
                    new PreparationRunwayBlockWorkoutBinding<TKey>(request.BlockKey, 0, []), trace);
            }

            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.MissingProgressionDefinition,
                $"No progression definition was supplied for block '{request.BlockKey}', but AllocatedWeeks={request.AllocatedWeeks} > 0.", trace);
        }

        if (string.IsNullOrWhiteSpace(definition.ProgressionId) || definition.Version < 1)
            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.InvalidBindingRequest, "Progression definition has an invalid ProgressionId or Version.", trace);

        if (!EqualityComparer<TKey>.Default.Equals(definition.BlockKey, request.BlockKey))
            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.BlockKeyMismatch,
                $"Progression definition's BlockKey '{definition.BlockKey}' does not match the requested BlockKey '{request.BlockKey}'.", trace);

        if (definition.OrderedSteps is null)
            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.InvalidProgressionOrder, "Progression definition has no step collection.", trace);

        foreach (var step in definition.OrderedSteps)
        {
            if (step.StepNumber < 1)
                return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.InvalidProgressionOrder, $"Step number {step.StepNumber} must be positive.", trace);
            if (string.IsNullOrWhiteSpace(step.WorkoutId) || step.WorkoutVersion < 1)
                return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.InvalidProgressionOrder, $"Step {step.StepNumber} has an invalid workout reference.", trace);
        }

        var stepNumbers = definition.OrderedSteps.Select(s => s.StepNumber).ToArray();
        if (stepNumbers.Distinct().Count() != stepNumbers.Length)
            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.DuplicateProgressionStep, "Progression definition contains duplicate step numbers.", trace);

        // ── Step 2: canonically order progression steps (never trust source-array order) ──
        var canonicalSteps = definition.OrderedSteps.OrderBy(s => s.StepNumber).ToArray();
        if (!canonicalSteps.Select(s => s.StepNumber).SequenceEqual(Enumerable.Range(1, canonicalSteps.Length)))
            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.NonContiguousProgression,
                $"Progression definition step numbers must be exactly 1..{canonicalSteps.Length} with no gaps or missing Step 1.", trace);

        trace.Add($"Canonical step order: {string.Join(",", canonicalSteps.Select(s => $"{s.StepNumber}:{s.WorkoutId}v{s.WorkoutVersion}"))}");

        // ── Step 3: handle zero allocation ───────────────────────────────
        if (request.AllocatedWeeks == 0)
        {
            trace.Add("AllocatedWeeks=0, returning empty success.");
            return PreparationRunwayBlockWorkoutBindingResult<TKey>.Success(
                new PreparationRunwayBlockWorkoutBinding<TKey>(request.BlockKey, 0, []), trace);
        }

        // ── Step 4: validate progression capacity ────────────────────────
        if (request.AllocatedWeeks > canonicalSteps.Length)
            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.ProgressionCapacityExceeded,
                $"AllocatedWeeks={request.AllocatedWeeks} exceeds the progression's own capacity of {canonicalSteps.Length} step(s).", trace);

        // ── Step 5: select exact prefix ───────────────────────────────────
        var selected = canonicalSteps.Take(request.AllocatedWeeks)
            .Select(s => new PreparationRunwayWorkoutReference(s.WorkoutId, s.WorkoutVersion))
            .ToArray();

        // ── Step 7 (final invariant validation; Step 6 -- catalog-aware
        // reference validation -- is intentionally out of this pure engine's
        // scope, see PreparationRunwayBlockWorkoutReferenceValidator) ──────
        if (selected.Length != request.AllocatedWeeks)
            return Failure<TKey>(PreparationRunwayWorkoutBindingFailureCode.BindingInvariantViolation,
                $"Selected reference count ({selected.Length}) does not equal AllocatedWeeks ({request.AllocatedWeeks}).", trace);

        trace.Add($"Selected: {string.Join(",", selected.Select(r => $"{r.WorkoutId}v{r.WorkoutVersion}"))}");
        return PreparationRunwayBlockWorkoutBindingResult<TKey>.Success(
            new PreparationRunwayBlockWorkoutBinding<TKey>(request.BlockKey, request.AllocatedWeeks, selected), trace);
    }

    private static PreparationRunwayBlockWorkoutBindingResult<TKey> Failure<TKey>(
        PreparationRunwayWorkoutBindingFailureCode code, string reason, List<string> trace) where TKey : notnull
    {
        trace.Add($"FAILED: {code} -- {reason}");
        return PreparationRunwayBlockWorkoutBindingResult<TKey>.Failure(code, reason, trace);
    }
}

/// <summary>A single ordered progression step: which whole workout document to use.</summary>
internal sealed record PreparationRunwayBlockProgressionStep(
    int StepNumber,
    string WorkoutId,
    int WorkoutVersion);

/// <summary>
/// A canonical, ordered progression mapping for one block. Generic over an
/// arbitrary, stable block-key type so the engine can be proven generic
/// (see the genericity test) independent of any specific catalog/candidate.
/// </summary>
internal sealed record PreparationRunwayBlockProgressionDefinition<TKey>(
    string ProgressionId,
    int Version,
    TKey BlockKey,
    IReadOnlyList<PreparationRunwayBlockProgressionStep> OrderedSteps) where TKey : notnull;

/// <summary>
/// Binder input. <see cref="ProgressionDefinition"/> is null when no
/// canonical progression exists for this block (an unsupported/unmapped
/// block) -- see <see cref="PreparationRunwayWorkoutBindingFailureCode.MissingProgressionDefinition"/>.
/// </summary>
internal sealed record PreparationRunwayBlockWorkoutBindingRequest<TKey>(
    TKey BlockKey,
    int AllocatedWeeks,
    PreparationRunwayBlockProgressionDefinition<TKey>? ProgressionDefinition) where TKey : notnull;

/// <summary>One resolved workout reference (key + version) -- not a full workout document.</summary>
internal sealed record PreparationRunwayWorkoutReference(string WorkoutId, int WorkoutVersion);

/// <summary>Successful binding output. OrderedWorkoutReferences.Count always equals AllocatedWeeks.</summary>
internal sealed record PreparationRunwayBlockWorkoutBinding<TKey>(
    TKey BlockKey,
    int AllocatedWeeks,
    IReadOnlyList<PreparationRunwayWorkoutReference> OrderedWorkoutReferences) where TKey : notnull;

internal enum PreparationRunwayWorkoutBindingFailureCode
{
    InvalidBindingRequest,
    BlockKeyMismatch,
    InvalidProgressionOrder,
    DuplicateProgressionStep,
    NonContiguousProgression,
    ProgressionCapacityExceeded,
    MissingProgressionDefinition,
    WorkoutReferenceNotFound,
    WorkoutVersionNotFound,
    WorkoutNotRunwayEligible,
    WorkoutSemanticMismatch,
    BindingInvariantViolation,
}

/// <summary>Success/failure result. Failure never carries a partial binding.</summary>
internal sealed record PreparationRunwayBlockWorkoutBindingResult<TKey>(
    bool IsSuccess,
    PreparationRunwayBlockWorkoutBinding<TKey>? Binding,
    PreparationRunwayWorkoutBindingFailureCode? FailureCode,
    string? FailureReason,
    IReadOnlyList<string> Trace) where TKey : notnull
{
    public static PreparationRunwayBlockWorkoutBindingResult<TKey> Success(PreparationRunwayBlockWorkoutBinding<TKey> binding, IReadOnlyList<string> trace) =>
        new(true, binding, null, null, trace);

    public static PreparationRunwayBlockWorkoutBindingResult<TKey> Failure(PreparationRunwayWorkoutBindingFailureCode code, string reason, IReadOnlyList<string> trace) =>
        new(false, null, code, reason, trace);
}
