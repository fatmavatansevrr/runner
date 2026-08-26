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
        // Backend Integration Phase 10K-FREQ.6D.4D Split A: EffectiveLanes (not .Stages
        // directly) so a lane-authored phase's per-lane stage candidates are correctly
        // included in the closure — degenerates to the original .Stages-only closure for
        // every existing single-lane progression artifact (EffectiveLanes normalizes that
        // case to one implicit lane wrapping Stages).
        var closure = context.ReferencedWorkouts
            .Concat(context.Progression.PhaseProgressions.SelectMany(p => p.EffectiveLanes).SelectMany(l => l.Stages).SelectMany(s => s.WorkoutCandidateReferences))
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

        // Backend Integration Phase 10K-FREQ.6D.4D Split A: keyed by (WeekNumber, LaneOrdinal),
        // never WeekNumber alone — the pre-Split-A defect this phase closes. Two lane-schedule
        // rows for the same (week, lane) is a genuine authoring/allocation-output defect, never
        // silently collapsed.
        var stageWeeksByWeekAndLane = new Dictionary<(int WeekNumber, int LaneOrdinal), ScheduledProgressionWeek>();
        foreach (var stageWeek in context.StageSchedule.Weeks)
        {
            if (!stageWeeksByWeekAndLane.TryAdd((stageWeek.WeekNumber, stageWeek.LaneOrdinal), stageWeek))
            {
                throw new CatalogWorkoutBindingDuplicateLaneStageAssignmentException(
                    $"Week {stageWeek.WeekNumber} lane {stageWeek.LaneOrdinal} has more than one progression-stage assignment.");
            }
        }

        var weeksOut = new List<BoundCatalogWeek>();
        var traceOut = new List<WorkoutBindingDecisionTraceStep>();

        foreach (var datedWeek in context.DatedSkeleton.Weeks.OrderBy(w => w.WeekNumber))
        {
            var sessionsOut = new List<BoundCatalogSession>();

            // Backend Integration Phase 10K-FREQ.6D.4D Split A: the structural ordinal for a
            // repeated structural role within this week — a zero-based rank over same-role
            // slots ordered by the canonical SlotOrderInWeek (never calendar date, weekday,
            // workout/profile identity, or dictionary/enumeration order). For StageControlled
            // roles (today: only KEY_SESSION), this ordinal IS the LaneOrdinal, by the
            // architecture's own binding rule (structural ordinal N binds to LaneOrdinal N).
            var structuralOrdinalByRole = new Dictionary<string, int>();

            foreach (var slot in datedWeek.SessionSlots.OrderBy(s => s.SlotOrderInWeek))
            {
                var mode = V1CatalogWorkoutRoleBindingPolicy.ModeFor(slot.StructuralRole);
                structuralOrdinalByRole.TryGetValue(slot.StructuralRole, out var structuralOrdinal);
                structuralOrdinalByRole[slot.StructuralRole] = structuralOrdinal + 1;

                BoundCatalogSession session;
                WorkoutBindingDecisionTraceStep trace;

                if (mode == CatalogWorkoutBindingMode.StageControlled)
                {
                    var laneOrdinal = structuralOrdinal;

                    if (!stageWeeksByWeekAndLane.TryGetValue((datedWeek.WeekNumber, laneOrdinal), out var stageWeek) || string.IsNullOrWhiteSpace(stageWeek.ProgressionStageKey))
                    {
                        throw new CatalogWorkoutBindingMissingProgressionStageException(
                            $"Week {datedWeek.WeekNumber} lane {laneOrdinal} has a KEY_SESSION slot but no matching Phase 4F.6A stage assignment.");
                    }

                    var stageDefinition = context.Progression.PhaseProgressions
                        .SelectMany(p => p.EffectiveLanes)
                        .Where(l => l.LaneOrdinal == laneOrdinal)
                        .SelectMany(l => l.Stages)
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
                    var beginnerDeferredFallback = V1BeginnerWorkoutEligibilityPolicy.IsDeferred(
                        context.CandidateKey, candidateReference.Key);
                    if (beginnerDeferredFallback)
                    {
                        candidateReference = context.ReferencedWorkouts.Single(r =>
                            r.Key == V1BeginnerWorkoutEligibilityPolicy.DeferredFallbackWorkoutKey);
                    }
                    var definition = await ResolveDefinitionAsync(candidateReference);
                    ValidateInClosureAndPhase(definition, datedWeek.PhaseKey);

                    // Backend Integration Phase 10K-FREQ.6D.4D Split B: exact prescription-profile
                    // resolution, additive to workout-definition binding above. Zero declared
                    // candidates leaves the session Legacy (both fields null) — no error, per the
                    // additive/degenerate-default legacy boundary. Exactly one candidate makes the
                    // session ProfileBacked. More than one is ambiguous and fails closed; RunningApp
                    // never chooses among candidates by DoseCategory, phase, or any other search —
                    // that invariant is enforced catalog-side, at publish time (PlanCatalog's
                    // PrescriptionProfileLaneDoseValidator), not rediscovered here.
                    string? prescriptionProfileKey = null;
                    int? prescriptionProfileVersion = null;
                    if (stageDefinition.PrescriptionProfileCandidateKeys.Count == 1)
                    {
                        var profileCandidate = stageDefinition.PrescriptionProfileCandidateKeys[0];
                        prescriptionProfileKey = profileCandidate.Key;
                        prescriptionProfileVersion = profileCandidate.Version;
                    }
                    else if (stageDefinition.PrescriptionProfileCandidateKeys.Count > 1)
                    {
                        throw new CatalogWorkoutBindingAmbiguousPrescriptionProfileCandidateException(
                            $"Stage '{stageDefinition.ProgressionStageKey}' lane {laneOrdinal} declares " +
                            $"{stageDefinition.PrescriptionProfileCandidateKeys.Count} prescription-profile candidates — " +
                            "no multi-profile selection policy exists; a stage becomes ProfileBacked only with exactly one candidate.");
                    }

                    session = new BoundCatalogSession
                    {
                        WeekNumber = datedWeek.WeekNumber,
                        Date = slot.SessionDate,
                        PhaseKey = datedWeek.PhaseKey,
                        ProgressionStageKey = stageWeek.ProgressionStageKey,
                        LaneOrdinal = laneOrdinal,
                        SlotOrdinal = slot.SlotOrderInWeek,
                        PrescriptionProfileKey = prescriptionProfileKey,
                        PrescriptionProfileVersion = prescriptionProfileVersion,
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
                        BindingReason = beginnerDeferredFallback
                            ? "BEGINNER_DEFERRED_WORKOUT_FALLBACK"
                            : "STAGE_CONTROLLED_CANDIDATE_RESOLUTION",
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
                        LaneOrdinal = null,
                        SlotOrdinal = slot.SlotOrderInWeek,
                        PrescriptionProfileKey = null,
                        PrescriptionProfileVersion = null,
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

            // Backend Integration Phase 10K-FREQ.6D.4D Split A: RunLayout — not a catalog lane
            // declaration — remains the sole structural-cardinality authority. A catalog
            // progression may not manufacture an extra structural session by declaring more
            // lanes for a week than that week actually has StageControlled structural slots.
            var declaredLaneCount = stageWeeksByWeekAndLane.Keys.Count(k => k.WeekNumber == datedWeek.WeekNumber);
            var structuralKeySlotCount = structuralOrdinalByRole.GetValueOrDefault(ScheduledProgressionWeek.KeySessionStructuralRole);
            if (declaredLaneCount > structuralKeySlotCount)
            {
                throw new CatalogWorkoutBindingLaneCountMismatchException(
                    $"Week {datedWeek.WeekNumber} has {declaredLaneCount} declared progression lane(s) but only {structuralKeySlotCount} " +
                    $"'{ScheduledProgressionWeek.KeySessionStructuralRole}' structural slot(s) — a catalog lane cannot manufacture an extra structural session.");
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
