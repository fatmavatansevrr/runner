using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 4G.3B.4b, verifier-of-verifiers 0/9 -- a composition-only layer, not
/// a tenth verifier. Invokes all nine canonical standalone safety verifiers
/// (see PHASE4G_3B_3_SAFETY_VERIFICATION_PIPELINE_PLANNING.md) in a fixed
/// order against one already-produced typed context, preserves every typed
/// result unchanged, and aggregates their normalized outcomes and findings.
///
/// This is composition and aggregation ONLY: it performs no materialization,
/// allocation, binding, policy resolution, condition resolution, governance
/// lookup, or new safety calculation. It duplicates no verifier's internal
/// logic. It does not build its own inputs -- <see cref="SafetyVerificationContext"/>
/// must already contain every already-produced artifact the nine verifiers
/// need. See PHASE4G_3B_4B_SAFETY_VERIFICATION_ORCHESTRATOR.md for the full
/// design record, outcome-normalization table, and real 8-14 matrix.
///
/// AllocationOrderCorrectnessVerifier is deliberately NOT invoked here -- it
/// predates and sits outside the canonical nine (see
/// PHASE4G_3B_3_SAFETY_VERIFICATION_PIPELINE_PLANNING.md's own framing
/// decision): it answers whether a mechanically selected phase split depends
/// on the still-open TD-ALLOCATION-PRIORITY-001 allocation-priority
/// decision, a different question from whether the SUPPLIED allocation and
/// its generated artifacts satisfy the nine post-allocation safety checks.
/// A future support-decision layer must combine both, plus support-registry
/// state and open activation governance decisions, before any horizon is
/// activation-ready -- that combination is explicitly out of scope here.
///
/// Not called from any live request path. Not registered in DI. Not wired
/// into CatalogPreviewGenerator, PlanServices, any controller, or startup.
/// </summary>
internal enum SafetyVerificationOverallOutcome
{
    Pass,
    Fail,
    DecisionRequired,
    NotApplicable,
}

internal enum CanonicalSafetyVerifier
{
    PhaseConstraint,
    RaceSpecificCapacity,
    StageReachability,
    WorkoutExposure,
    GoalPaceReachability,
    ReadinessEligibility,
    VolumeProgression,
    LongRunProgression,
    RaceDateAlignment,
}

internal sealed record SafetyVerificationFinding(
    CanonicalSafetyVerifier SourceVerifier,
    string Code,
    string Message);

internal sealed record SafetyVerifierRunSummary(
    CanonicalSafetyVerifier Verifier,
    string OriginalOutcome,
    SafetyVerificationOverallOutcome NormalizedOutcome,
    IReadOnlyList<SafetyVerificationFinding> Findings);

/// <summary>
/// Immutable typed input to <see cref="SafetyVerificationOrchestrator.Run"/>.
/// Every field maps to at least one of the nine canonical verifiers' exact
/// current <c>Verify</c> signature -- see PHASE4G_3B_4B_SAFETY_VERIFICATION_ORCHESTRATOR.md
/// section 3 for the full input-to-verifier dependency table. Contains only
/// already-produced artifacts: no service object, DbContext, IServiceProvider,
/// controller/request DTO, filesystem path, catalog root, clock, environment/
/// configuration object, governance document representation, or lazy factory.
/// Callers (tests, or a future support-decision layer) are responsible for
/// producing every field via the already-existing real components before
/// calling Run -- this orchestrator never constructs any of them itself.
/// </summary>
internal sealed record SafetyVerificationContext(
    PhaseAllocationResult Allocation,
    CatalogWorkoutProgressionDefinition Progression,
    IReadOnlyList<string> WeeklySlotRoles,
    IReadOnlyList<RuntimeConditionResolutionResult> RuntimeConditions,
    DatedGeneratedCatalogPlanSkeleton DatedSchedule,
    BoundCatalogPlan BoundPlan,
    CatalogWorkoutProgressionStage GoalPaceStage,
    IReadOnlySet<string> RegisteredGoalFeasibilityValues,
    CatalogWeeklyVolumePlan VolumePlan,
    VolumeSafetyPolicy Policy,
    CatalogLongRunProgression LongRunPlan,
    DateOnly RaceDate);

internal sealed record SafetyVerificationPipelineResult(
    int TargetWeeks,

    PhaseConstraintVerificationResult PhaseConstraint,
    RaceSpecificCapacityVerificationResult RaceSpecificCapacity,
    StageReachabilityVerificationResult StageReachability,
    WorkoutExposureVerificationResult WorkoutExposure,
    GoalPaceReachabilityVerificationResult GoalPaceReachability,
    ReadinessEligibilityVerificationResult ReadinessEligibility,
    VolumeProgressionVerificationResult VolumeProgression,
    LongRunProgressionVerificationResult LongRunProgression,
    RaceDateAlignmentVerificationResult RaceDateAlignment,

    IReadOnlyList<SafetyVerifierRunSummary> OrderedSummaries,
    SafetyVerificationOverallOutcome OverallOutcome,
    IReadOnlyList<SafetyVerificationFinding> AggregatedFindings);

/// <inheritdoc cref="SafetyVerificationPipelineResult"/>
internal static class SafetyVerificationOrchestrator
{
    public static SafetyVerificationPipelineResult Run(SafetyVerificationContext context)
    {
        // ── Fixed canonical execution order (1-9). All nine run exactly ──
        // once, unconditionally -- no short-circuit after Fail/DecisionRequired/
        // NotApplicable/an informational result. Each call is exactly the
        // verifier's own public Verify method; no logic is inlined or copied.
        var phaseConstraint = PhaseConstraintVerifier.Verify(context.Allocation);
        var raceSpecificCapacity = RaceSpecificCapacityVerifier.Verify(context.Allocation, context.Progression, context.WeeklySlotRoles);
        var stageReachability = StageReachabilityVerifier.Verify(context.Allocation, context.Progression, context.RuntimeConditions, context.WeeklySlotRoles);
        var workoutExposure = WorkoutExposureVerifier.Verify(context.Allocation, context.DatedSchedule, context.BoundPlan, context.Progression, context.WeeklySlotRoles);
        var goalPaceReachability = GoalPaceReachabilityVerifier.Verify(context.Allocation, context.Progression, context.WeeklySlotRoles, context.GoalPaceStage, context.RegisteredGoalFeasibilityValues);
        var readinessEligibility = ReadinessEligibilityVerifier.Verify(context.Allocation);
        var volumeProgression = VolumeProgressionVerifier.Verify(context.VolumePlan, context.Policy);
        var longRunProgression = LongRunProgressionVerifier.Verify(context.VolumePlan, context.LongRunPlan, context.Policy);
        var raceDateAlignment = RaceDateAlignmentVerifier.Verify(context.DatedSchedule, context.RaceDate);

        var summaries = new List<SafetyVerifierRunSummary>
        {
            Summarize(CanonicalSafetyVerifier.PhaseConstraint, phaseConstraint.Outcome, Normalize(phaseConstraint.Outcome),
                phaseConstraint.Findings.Select(f => AdaptStringFinding(CanonicalSafetyVerifier.PhaseConstraint, f)).ToList()),

            Summarize(CanonicalSafetyVerifier.RaceSpecificCapacity, raceSpecificCapacity.Outcome, Normalize(raceSpecificCapacity.Outcome),
                raceSpecificCapacity.Findings.Select(f => AdaptTypedFinding(CanonicalSafetyVerifier.RaceSpecificCapacity, f.Code.ToString(), f.Message)).ToList()),

            Summarize(CanonicalSafetyVerifier.StageReachability, stageReachability.Outcome, Normalize(stageReachability.Outcome),
                stageReachability.Findings.Select(f => AdaptTypedFinding(CanonicalSafetyVerifier.StageReachability, f.Code.ToString(), f.Message)).ToList()),

            Summarize(CanonicalSafetyVerifier.WorkoutExposure, workoutExposure.Outcome, Normalize(workoutExposure.Outcome),
                workoutExposure.Findings.Select(f => AdaptTypedFinding(CanonicalSafetyVerifier.WorkoutExposure, f.Code.ToString(), f.Message)).ToList()),

            Summarize(CanonicalSafetyVerifier.GoalPaceReachability, goalPaceReachability.OverallOutcome, Normalize(goalPaceReachability.OverallOutcome),
                goalPaceReachability.OutcomeChecks.Select(c => AdaptTypedFinding(CanonicalSafetyVerifier.GoalPaceReachability, c.Status.ToString(), c.ReasonCode)).ToList()),

            Summarize(CanonicalSafetyVerifier.ReadinessEligibility, readinessEligibility.Outcome, Normalize(readinessEligibility.Outcome),
                readinessEligibility.Findings.Select(f => AdaptStringFinding(CanonicalSafetyVerifier.ReadinessEligibility, f)).ToList()),

            Summarize(CanonicalSafetyVerifier.VolumeProgression, volumeProgression.Outcome, Normalize(volumeProgression.Outcome),
                volumeProgression.Findings.Select(f => AdaptStringFinding(CanonicalSafetyVerifier.VolumeProgression, f)).ToList()),

            Summarize(CanonicalSafetyVerifier.LongRunProgression, longRunProgression.Outcome, Normalize(longRunProgression.Outcome),
                longRunProgression.Findings.Select(f => AdaptStringFinding(CanonicalSafetyVerifier.LongRunProgression, f)).ToList()),

            Summarize(CanonicalSafetyVerifier.RaceDateAlignment, raceDateAlignment.Outcome, Normalize(raceDateAlignment.Outcome),
                raceDateAlignment.Findings.Select(f => AdaptStringFinding(CanonicalSafetyVerifier.RaceDateAlignment, f)).ToList()),
        };

        var overall = AggregateOverallOutcome(context.Allocation.IsMathematicallyFeasible, summaries);
        var aggregatedFindings = summaries.SelectMany(s => s.Findings).ToList();

        return new SafetyVerificationPipelineResult(
            context.Allocation.TargetWeeks,
            phaseConstraint, raceSpecificCapacity, stageReachability, workoutExposure,
            goalPaceReachability, readinessEligibility, volumeProgression, longRunProgression, raceDateAlignment,
            summaries, overall, aggregatedFindings);
    }

    // ── Overall aggregation ───────────────────────────────────────────────────
    //
    // 1. Root allocation mathematically infeasible -> OverallOutcome is
    //    ALWAYS NotApplicable, regardless of any individual verifier's own
    //    normalized tier. All nine typed results/summaries are still
    //    returned (see Run above -- every verifier still runs).
    //
    // 2. Root allocation feasible -> Fail > DecisionRequired > Pass among
    //    the nine normalized tiers. A per-verifier NotApplicable that occurs
    //    while the root allocation is feasible can only come from a
    //    NON-root trigger specific to that verifier (see the
    //    per-verifier Normalize functions' own doc comments for exactly
    //    which triggers those are) -- per this phase's own instruction not
    //    to silently collapse a non-root NotApplicable into overall
    //    NotApplicable, any such per-verifier NotApplicable is treated as
    //    Fail for aggregation purposes here: it represents a genuine
    //    inability to confirm safety for the supplied context, which is
    //    closer to "verification did not complete" than to "nothing to
    //    check because there is no root allocation." The per-verifier
    //    summary itself still preserves the true NotApplicable tier
    //    unchanged, for transparency (see summaries above).
    private static SafetyVerificationOverallOutcome AggregateOverallOutcome(
        bool allocationIsMathematicallyFeasible, IReadOnlyList<SafetyVerifierRunSummary> summaries)
    {
        if (!allocationIsMathematicallyFeasible)
        {
            return SafetyVerificationOverallOutcome.NotApplicable;
        }

        var forAggregation = summaries
            .Select(s => s.NormalizedOutcome == SafetyVerificationOverallOutcome.NotApplicable
                ? SafetyVerificationOverallOutcome.Fail
                : s.NormalizedOutcome)
            .ToList();

        if (forAggregation.Contains(SafetyVerificationOverallOutcome.Fail))
        {
            return SafetyVerificationOverallOutcome.Fail;
        }

        if (forAggregation.Contains(SafetyVerificationOverallOutcome.DecisionRequired))
        {
            return SafetyVerificationOverallOutcome.DecisionRequired;
        }

        return SafetyVerificationOverallOutcome.Pass;
    }

    // ── Per-verifier outcome normalization -- one explicit exhaustive typed ──
    // ── switch per verifier, no reflection/ToString()/wildcard branch. ──────
    // See PHASE4G_3B_4B_SAFETY_VERIFICATION_ORCHESTRATOR.md section 7 for the
    // full table and the reasoning behind every mapping.

    /// <summary>Only NotApplicable trigger is root allocation infeasibility.</summary>
    private static SafetyVerificationOverallOutcome Normalize(PhaseConstraintVerificationOutcome outcome) => outcome switch
    {
        PhaseConstraintVerificationOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        PhaseConstraintVerificationOutcome.Fail => SafetyVerificationOverallOutcome.Fail,
        PhaseConstraintVerificationOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(PhaseConstraintVerificationOutcome)} value: {outcome}."),
    };

    /// <summary>Only NotApplicable trigger is root allocation infeasibility.</summary>
    private static SafetyVerificationOverallOutcome Normalize(RaceSpecificCapacityOutcome outcome) => outcome switch
    {
        RaceSpecificCapacityOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        RaceSpecificCapacityOutcome.Fail => SafetyVerificationOverallOutcome.Fail,
        RaceSpecificCapacityOutcome.DecisionRequired => SafetyVerificationOverallOutcome.DecisionRequired,
        RaceSpecificCapacityOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(RaceSpecificCapacityOutcome)} value: {outcome}."),
    };

    /// <summary>Only NotApplicable trigger is root allocation infeasibility.</summary>
    private static SafetyVerificationOverallOutcome Normalize(StageReachabilityOutcome outcome) => outcome switch
    {
        StageReachabilityOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        StageReachabilityOutcome.Fail => SafetyVerificationOverallOutcome.Fail,
        StageReachabilityOutcome.DecisionRequired => SafetyVerificationOverallOutcome.DecisionRequired,
        StageReachabilityOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(StageReachabilityOutcome)} value: {outcome}."),
    };

    /// <summary>Only NotApplicable trigger is root allocation infeasibility. DecisionRequired is declared by the enum but never produced by current Verify logic -- still mapped exhaustively.</summary>
    private static SafetyVerificationOverallOutcome Normalize(WorkoutExposureOutcome outcome) => outcome switch
    {
        WorkoutExposureOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        WorkoutExposureOutcome.Fail => SafetyVerificationOverallOutcome.Fail,
        WorkoutExposureOutcome.DecisionRequired => SafetyVerificationOverallOutcome.DecisionRequired,
        WorkoutExposureOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(WorkoutExposureOutcome)} value: {outcome}."),
    };

    /// <summary>
    /// NotApplicable has TWO triggers: root allocation infeasibility, AND a
    /// non-root "unexpected stage shape" case (the supplied GoalPaceStage
    /// does not match this verifier's expected shape) -- the enum itself
    /// does not distinguish them. PassWithOpenRisk maps to DecisionRequired
    /// per this phase's explicit mapping principle (an open, not-yet-
    /// product-approved TD-NOTEVALUATED-FALLBACK-001 gap, not a clean pass).
    /// </summary>
    private static SafetyVerificationOverallOutcome Normalize(GoalPaceReachabilityOutcome outcome) => outcome switch
    {
        GoalPaceReachabilityOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        GoalPaceReachabilityOutcome.PassWithOpenRisk => SafetyVerificationOverallOutcome.DecisionRequired,
        GoalPaceReachabilityOutcome.Fail => SafetyVerificationOverallOutcome.Fail,
        GoalPaceReachabilityOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(GoalPaceReachabilityOutcome)} value: {outcome}."),
    };

    /// <summary>Only NotApplicable trigger is root allocation infeasibility. This enum has no Fail value at all -- a below-catalog-minimum allocation is DecisionRequired, never Fail (see TD-FOUNDATION-COMPRESSION-001).</summary>
    private static SafetyVerificationOverallOutcome Normalize(ReadinessEligibilityOutcome outcome) => outcome switch
    {
        ReadinessEligibilityOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        ReadinessEligibilityOutcome.DecisionRequired => SafetyVerificationOverallOutcome.DecisionRequired,
        ReadinessEligibilityOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(ReadinessEligibilityOutcome)} value: {outcome}."),
    };

    /// <summary>NotApplicable trigger (volumePlan.Weeks.Count &lt; 2) is NOT tied to Allocation.IsMathematicallyFeasible at all -- a non-root trigger, structurally independent of the root allocation.</summary>
    private static SafetyVerificationOverallOutcome Normalize(VolumeProgressionOutcome outcome) => outcome switch
    {
        VolumeProgressionOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        VolumeProgressionOutcome.Fail => SafetyVerificationOverallOutcome.Fail,
        VolumeProgressionOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(VolumeProgressionOutcome)} value: {outcome}."),
    };

    /// <summary>NotApplicable trigger (longRunPlan.Weeks.Count == 0) is NOT tied to Allocation.IsMathematicallyFeasible at all -- a non-root trigger.</summary>
    private static SafetyVerificationOverallOutcome Normalize(LongRunProgressionOutcome outcome) => outcome switch
    {
        LongRunProgressionOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        LongRunProgressionOutcome.Fail => SafetyVerificationOverallOutcome.Fail,
        LongRunProgressionOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(LongRunProgressionOutcome)} value: {outcome}."),
    };

    /// <summary>NotApplicable trigger (empty dated schedule) is NOT tied to Allocation.IsMathematicallyFeasible at all -- a non-root trigger.</summary>
    private static SafetyVerificationOverallOutcome Normalize(RaceDateAlignmentOutcome outcome) => outcome switch
    {
        RaceDateAlignmentOutcome.Pass => SafetyVerificationOverallOutcome.Pass,
        RaceDateAlignmentOutcome.Fail => SafetyVerificationOverallOutcome.Fail,
        RaceDateAlignmentOutcome.NotApplicable => SafetyVerificationOverallOutcome.NotApplicable,
        _ => throw new NotSupportedException($"Unrecognized {nameof(RaceDateAlignmentOutcome)} value: {outcome}."),
    };

    // ── Finding adapters -- explicit, no reflection, no serialize/reparse ────

    /// <summary>
    /// Five verifiers (PhaseConstraint, ReadinessEligibility, VolumeProgression,
    /// LongRunProgression, RaceDateAlignment) expose findings as plain strings
    /// only, each consistently prefixed with an UPPER_SNAKE_CASE code token
    /// followed by ": " (verified by direct inspection of every finding call
    /// site in all five source files). The exact string is preserved verbatim
    /// as Message; the leading token is extracted as Code -- the smallest
    /// stable code each verifier's own string contract actually supports,
    /// not a guess at semantic meaning.
    /// </summary>
    private static SafetyVerificationFinding AdaptStringFinding(CanonicalSafetyVerifier source, string message) =>
        new(source, ExtractLeadingCode(message), message);

    /// <summary>Four verifiers (RaceSpecificCapacity, StageReachability, WorkoutExposure, GoalPaceReachability) expose an already-typed code (a Finding.Code enum for the first three; GoalPaceOutcomeCheck.Status for the fourth) -- used directly, never re-derived from message text.</summary>
    private static SafetyVerificationFinding AdaptTypedFinding(CanonicalSafetyVerifier source, string typedCode, string message) =>
        new(source, typedCode, message);

    private static string ExtractLeadingCode(string message)
    {
        var colonIndex = message.IndexOf(": ", StringComparison.Ordinal);
        return colonIndex > 0 ? message[..colonIndex] : message;
    }

    private static SafetyVerifierRunSummary Summarize(
        CanonicalSafetyVerifier verifier, object originalOutcome, SafetyVerificationOverallOutcome normalized,
        IReadOnlyList<SafetyVerificationFinding> findings) =>
        new(verifier, originalOutcome.ToString() ?? string.Empty, normalized, findings);
}
