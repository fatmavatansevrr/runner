using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Binding;

/// <summary>
/// Backend Integration Phase 4F.6B — immutable input to <see cref="ICatalogWorkoutBinder"/>.
/// Carries the already-dated structural schedule (Phase 4F.5), the already-scheduled
/// fine-grained KEY_SESSION stage assignments (Phase 4F.6A), the already-loaded workout
/// progression definition (reused from the Phase 4F.6A step, never reloaded), and the
/// already-resolved candidate summary (for its ReferencedWorkouts closure). The binder never
/// reloads the catalog, never re-evaluates a runtime condition, and never recomputes a
/// fallback — it consumes Phase 4F.6A's effective stage assignments exactly as given.
/// </summary>
internal sealed class CatalogWorkoutBindingContext
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }
    public required DatedGeneratedCatalogPlanSkeleton DatedSkeleton { get; init; }
    public required GeneratedCatalogStageSchedule StageSchedule { get; init; }
    public required CatalogWorkoutProgressionDefinition Progression { get; init; }
    public required IReadOnlyList<PlanCatalogReference> ReferencedWorkouts { get; init; }
    public required ICatalogWorkoutDefinitionLoader WorkoutDefinitionLoader { get; init; }
}

internal interface ICatalogWorkoutBinder
{
    Task<BoundCatalogPlan> BindAsync(CatalogWorkoutBindingContext context, CancellationToken ct = default);
}

/// <summary>
/// Backend Integration Phase 4F.6B — binds every dated structural run slot to one exact,
/// versioned workout definition per <see cref="V1CatalogWorkoutRoleBindingPolicy"/>. See
/// PHASE4F_6B_V1_EXACT_WORKOUT_BINDING.md for the full write-up. Assigns workout identity
/// only — never pace/distance/duration/volume/repetitions/recovery/segments/public workout
/// type (Phase 4F.7/4F.8).
/// </summary>
internal sealed class CatalogWorkoutBinder : ICatalogWorkoutBinder
{
    public async Task<BoundCatalogPlan> BindAsync(CatalogWorkoutBindingContext context, CancellationToken ct = default)
    {
        // Resolved dependency closure (Step A.1's WorkoutClosureResolver concept, replicated
        // here from data already loaded rather than re-deriving it in plan-catalog): the
        // level modifier's eligible-workouts list, union every progression stage's own
        // candidate reference(s).
        var closure = context.ReferencedWorkouts
            .Concat(context.Progression.PhaseProgressions.SelectMany(p => p.Stages).SelectMany(s => s.WorkoutCandidateReferences))
            .Select(r => (r.Key, r.Version))
            .ToHashSet();

        var definitionCache = new Dictionary<(string Key, int Version), CatalogWorkoutDefinitionSummary>();

        async Task<CatalogWorkoutDefinitionSummary> ResolveDefinitionAsync(PlanCatalogReference reference)
        {
            if (definitionCache.TryGetValue((reference.Key, reference.Version), out var cached))
            {
                return cached;
            }

            CatalogWorkoutDefinitionSummary definition;
            try
            {
                definition = await context.WorkoutDefinitionLoader.LoadAsync(reference, ct);
            }
            catch (Exceptions.PlanCatalogLoadException ex)
            {
                throw new CatalogWorkoutBindingCandidateNotFoundException(
                    $"Workout definition '{reference.Key}' v{reference.Version} could not be loaded from the resolved catalog bundle: {ex.Message}");
            }

            if (definition.Key != reference.Key || definition.Version != reference.Version)
            {
                throw new CatalogWorkoutBindingVersionMismatchException(
                    $"Requested workout definition '{reference.Key}' v{reference.Version}, but the loaded document reports " +
                    $"'{definition.Key}' v{definition.Version}.");
            }

            definitionCache[(reference.Key, reference.Version)] = definition;
            return definition;
        }

        void ValidateInClosureAndPhase(CatalogWorkoutDefinitionSummary definition, string phaseKey)
        {
            if (!closure.Contains((definition.Key, definition.Version)))
            {
                throw new CatalogWorkoutBindingOutsideDependencyClosureException(
                    $"Workout definition '{definition.Key}' v{definition.Version} is not present in the resolved dependency " +
                    "closure (level-modifier eligible workouts ∪ progression-stage candidates).");
            }

            if (definition.EligiblePhases.Count > 0 && !definition.EligiblePhases.Contains(phaseKey))
            {
                throw new CatalogWorkoutBindingDefinitionInvalidException(
                    $"Workout definition '{definition.Key}' v{definition.Version} is not eligible for phase '{phaseKey}' " +
                    $"(eligiblePhases: {string.Join(", ", definition.EligiblePhases)}).");
            }

            if (string.IsNullOrWhiteSpace(definition.Status))
            {
                throw new CatalogWorkoutBindingDefinitionInvalidException(
                    $"Workout definition '{definition.Key}' v{definition.Version} has no lifecycle status.");
            }
        }

        var stageWeeksByNumber = context.StageSchedule.Weeks.ToDictionary(w => w.WeekNumber);
        var weeksOut = new List<BoundCatalogWeek>();
        var traceOut = new List<WorkoutBindingDecisionTraceStep>();

        foreach (var datedWeek in context.DatedSkeleton.Weeks.OrderBy(w => w.WeekNumber))
        {
            var sessionsOut = new List<BoundCatalogSession>();

            foreach (var slot in datedWeek.SessionSlots.OrderBy(s => s.SlotOrderInWeek))
            {
                var mode = V1CatalogWorkoutRoleBindingPolicy.ModeFor(slot.StructuralRole);

                BoundCatalogSession session;
                WorkoutBindingDecisionTraceStep trace;

                if (mode == CatalogWorkoutBindingMode.StageControlled)
                {
                    if (!stageWeeksByNumber.TryGetValue(datedWeek.WeekNumber, out var stageWeek) || string.IsNullOrWhiteSpace(stageWeek.ProgressionStageKey))
                    {
                        throw new CatalogWorkoutBindingMissingProgressionStageException(
                            $"Week {datedWeek.WeekNumber} has a KEY_SESSION slot but no matching Phase 4F.6A stage assignment.");
                    }

                    var stageDefinition = context.Progression.PhaseProgressions
                        .SelectMany(p => p.Stages)
                        .FirstOrDefault(s => s.ProgressionStageKey == stageWeek.ProgressionStageKey);

                    if (stageDefinition is null)
                    {
                        throw new CatalogWorkoutBindingUnknownProgressionStageException(
                            $"Week {datedWeek.WeekNumber}'s assigned ProgressionStageKey '{stageWeek.ProgressionStageKey}' does not " +
                            $"match any stage in workout progression '{context.Progression.Key}' v{context.Progression.Version}.");
                    }

                    if (stageDefinition.WorkoutCandidateReferences.Count == 0)
                    {
                        throw new CatalogWorkoutBindingMissingCandidateReferenceException(
                            $"Stage '{stageDefinition.ProgressionStageKey}' declares zero workout-candidate references.");
                    }

                    if (stageDefinition.WorkoutCandidateReferences.Count > 1)
                    {
                        throw new CatalogWorkoutBindingAmbiguousCandidateException(
                            $"Stage '{stageDefinition.ProgressionStageKey}' declares {stageDefinition.WorkoutCandidateReferences.Count} " +
                            "workout-candidate references — V1 does not implement a multi-workout selection policy.");
                    }

                    var candidateReference = stageDefinition.WorkoutCandidateReferences[0];
                    var definition = await ResolveDefinitionAsync(candidateReference);
                    ValidateInClosureAndPhase(definition, datedWeek.PhaseKey);

                    session = new BoundCatalogSession
                    {
                        WeekNumber = datedWeek.WeekNumber,
                        Date = slot.SessionDate,
                        PhaseKey = datedWeek.PhaseKey,
                        ProgressionStageKey = stageWeek.ProgressionStageKey,
                        StructuralRole = slot.StructuralRole,
                        WorkoutDefinitionKey = definition.Key,
                        WorkoutDefinitionVersion = definition.Version,
                        BindingMode = mode,
                        BindingPolicyKey = V1CatalogWorkoutRoleBindingPolicy.PolicyKey,
                        BindingPolicyVersion = V1CatalogWorkoutRoleBindingPolicy.PolicyVersion,
                        SourceArtifactKey = context.Progression.Key,
                        SourceArtifactVersion = context.Progression.Version,
                        ConditionOutcome = stageWeek.ConditionOutcome,
                        FallbackOrigin = stageWeek.FallbackOrigin,
                        BindingReason = "STAGE_CONTROLLED_CANDIDATE_RESOLUTION",
                    };

                    trace = new WorkoutBindingDecisionTraceStep
                    {
                        WeekNumber = datedWeek.WeekNumber, Date = slot.SessionDate, StructuralRole = slot.StructuralRole, PhaseKey = datedWeek.PhaseKey,
                        ProgressionStageKey = stageWeek.ProgressionStageKey, RequestedStageKey = stageWeek.RequestedProgressionStageKey ?? stageWeek.ProgressionStageKey,
                        EffectiveStageKey = stageWeek.ProgressionStageKey, BindingMode = mode,
                        ConfiguredDefaultOrStageCandidate = $"{candidateReference.Key} v{candidateReference.Version}",
                        ResolvedWorkoutKey = definition.Key, ResolvedWorkoutVersion = definition.Version,
                        ConditionOutcome = stageWeek.ConditionOutcome, FallbackOrigin = stageWeek.FallbackOrigin,
                        PolicyKey = V1CatalogWorkoutRoleBindingPolicy.PolicyKey, PolicyVersion = V1CatalogWorkoutRoleBindingPolicy.PolicyVersion,
                        SourceArtifactKey = context.Progression.Key, SourceArtifactVersion = context.Progression.Version,
                        ValidationResult = "VALID",
                    };
                }
                else
                {
                    var fixedDefaultKey = V1CatalogWorkoutRoleBindingPolicy.FixedDefaultWorkoutKeyFor(slot.StructuralRole);
                    var referencedMatch = context.ReferencedWorkouts.Where(r => r.Key == fixedDefaultKey).ToList();

                    if (referencedMatch.Count == 0)
                    {
                        throw new CatalogWorkoutBindingCandidateNotFoundException(
                            $"Fixed-default workout '{fixedDefaultKey}' for role '{slot.StructuralRole}' is not present in the " +
                            "resolved catalog bundle's referenced-workouts list.");
                    }

                    if (referencedMatch.Count > 1)
                    {
                        throw new CatalogWorkoutBindingAmbiguousCandidateException(
                            $"Fixed-default workout '{fixedDefaultKey}' matches {referencedMatch.Count} referenced-workout entries.");
                    }

                    var reference = referencedMatch[0];
                    var definition = await ResolveDefinitionAsync(reference);
                    ValidateInClosureAndPhase(definition, datedWeek.PhaseKey);

                    session = new BoundCatalogSession
                    {
                        WeekNumber = datedWeek.WeekNumber,
                        Date = slot.SessionDate,
                        PhaseKey = datedWeek.PhaseKey,
                        ProgressionStageKey = null,
                        StructuralRole = slot.StructuralRole,
                        WorkoutDefinitionKey = definition.Key,
                        WorkoutDefinitionVersion = definition.Version,
                        BindingMode = mode,
                        BindingPolicyKey = V1CatalogWorkoutRoleBindingPolicy.PolicyKey,
                        BindingPolicyVersion = V1CatalogWorkoutRoleBindingPolicy.PolicyVersion,
                        SourceArtifactKey = V1CatalogWorkoutRoleBindingPolicy.PolicyKey,
                        SourceArtifactVersion = V1CatalogWorkoutRoleBindingPolicy.PolicyVersion,
                        ConditionOutcome = null,
                        FallbackOrigin = null,
                        BindingReason = "FIXED_DEFAULT_ALLOCATION",
                    };

                    trace = new WorkoutBindingDecisionTraceStep
                    {
                        WeekNumber = datedWeek.WeekNumber, Date = slot.SessionDate, StructuralRole = slot.StructuralRole, PhaseKey = datedWeek.PhaseKey,
                        ProgressionStageKey = null, RequestedStageKey = null, EffectiveStageKey = null, BindingMode = mode,
                        ConfiguredDefaultOrStageCandidate = $"{reference.Key} v{reference.Version}",
                        ResolvedWorkoutKey = definition.Key, ResolvedWorkoutVersion = definition.Version,
                        ConditionOutcome = null, FallbackOrigin = null,
                        PolicyKey = V1CatalogWorkoutRoleBindingPolicy.PolicyKey, PolicyVersion = V1CatalogWorkoutRoleBindingPolicy.PolicyVersion,
                        SourceArtifactKey = V1CatalogWorkoutRoleBindingPolicy.PolicyKey, SourceArtifactVersion = V1CatalogWorkoutRoleBindingPolicy.PolicyVersion,
                        ValidationResult = "VALID",
                    };
                }

                sessionsOut.Add(session);
                traceOut.Add(trace);
            }

            weeksOut.Add(new BoundCatalogWeek { WeekNumber = datedWeek.WeekNumber, PhaseKey = datedWeek.PhaseKey, Sessions = sessionsOut });
        }

        return new BoundCatalogPlan
        {
            CandidateKey = context.CandidateKey,
            CandidateVersion = context.CandidateVersion,
            BinderVersion = CatalogWorkoutBinderVersion.V1,
            Weeks = weeksOut,
            Trace = new WorkoutBindingDecisionTrace { Steps = traceOut },
        };
    }
}
