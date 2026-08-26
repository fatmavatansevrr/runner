using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

/// <summary>
/// Applies non-race-specific effort contracts to the already-dated runway.
/// Production-owned but deliberately dark: no DI, preview, or persistence caller exists.
/// </summary>
internal static class PreparationRunwayPaceMaterializer
{
    private static readonly string[] ForbiddenTokens =
        ["GOAL_PACE", "RACE_SPECIFIC", "THRESHOLD", "MAXIMAL", "ALL_OUT", "VO2MAX", "TARGET_TIME"];

    public static PreparationRunwayPaceMaterializationResult<TKey> Materialize<TKey>(
        PreparationRunwayPaceMaterializationRequest<TKey> request) where TKey : notnull
    {
        var trace = new List<string>();
        var invalid = ValidateRequest(request);
        if (invalid is not null) return Fail<TKey>(invalid.Value.Code, invalid.Value.Reason, trace);

        try
        {
            var rules = request.Policy.WorkoutRules.ToDictionary(r => (r.WorkoutId, r.WorkoutVersion));
            var weeks = new List<PreparationRunwayPacedWeek<TKey>>();
            foreach (var week in request.DatedCombinedPlan.DatedRunwayWeeks!.OrderBy(w => w.GlobalWeekNumber))
            {
                var paced = new List<PreparationRunwayPacedSlot<TKey>>();
                foreach (var slot in week.StructuralOrderedSlots.OrderBy(s => s.PrescribedSlot.StructuralSlot.SlotOrdinal))
                {
                    var structural = slot.PrescribedSlot.StructuralSlot;
                    if (!rules.TryGetValue((structural.WorkoutId, structural.WorkoutVersion), out var rule))
                        return Fail<TKey>(PreparationRunwayPaceMaterializationFailureCode.WorkoutPacePolicyMissing,
                            $"No approved pace rule exists for {structural.WorkoutId} v{structural.WorkoutVersion}.", trace);
                    if (rule.PermittedRepresentation != CatalogPacePrescriptionKind.EffortOnly || rule.SelectedSource != CatalogPaceSourceSelection.EffortOnly)
                        return Fail<TKey>(PreparationRunwayPaceMaterializationFailureCode.WorkoutPacePolicyIncompatible,
                            $"Runway rule for {structural.WorkoutId} is not effort-only.", trace);
                    if (ForbiddenTokens.Any(t => rule.EffortLabel.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        return Fail<TKey>(ForbiddenCode(rule.EffortLabel),
                            $"Forbidden runway pace semantic '{rule.EffortLabel}'.", trace);

                    var prescription = new CatalogPacePrescription(
                        CatalogPacePrescriptionKind.EffortOnly, null, null, null,
                        CatalogPaceSourceSelection.EffortOnly, "CONTROLLED_EFFORT_CANONICAL_NO_NUMERIC_DERIVATION",
                        rule.EffortLabel,
                        $"{request.Policy.PolicyKey} v{request.Policy.PolicyVersion}; {rule.SemanticBasis}; numeric rounding not applicable");
                    var provenance = new PreparationRunwayPaceProvenance(
                        request.PaceContext.EvidenceState,
                        request.PaceContext.ResolvedPaceSource.Source,
                        request.PaceContext.TargetFinishTimeSource,
                        request.PaceContext.ResolverDecision,
                        $"{rule.WorkoutId} v{rule.WorkoutVersion} -> {rule.EffortLabel}",
                        "EffortOnly; no numeric derivation; no rounding",
                        request.Policy.ContinuityRule);
                    paced.Add(new PreparationRunwayPacedSlot<TKey>(slot, prescription, provenance));
                }

                var chronological = paced.OrderBy(s => s.OriginalSlot.SessionDate)
                    .ThenBy(s => s.OriginalSlot.PrescribedSlot.StructuralSlot.SlotOrdinal).ToArray();
                weeks.Add(new PreparationRunwayPacedWeek<TKey>(week, paced, chronological));
            }

            var mutation = ValidatePreservation(request, weeks);
            if (mutation is not null) return Fail<TKey>(mutation.Value.Code, mutation.Value.Reason, trace);

            var final = weeks.OrderBy(w => w.OriginalWeek.GlobalWeekNumber).Last();
            var checks = BuildContinuityChecks(final, request.CoreWeekOnePaceTarget);
            var allPaces = weeks.SelectMany(w => w.StructuralOrderedSlots).Select(s => s.PacePrescription).ToArray();
            var noGoal = allPaces.All(p => p.Source != CatalogPaceSourceSelection.TargetGoalDerived && p.Kind != CatalogPacePrescriptionKind.RelativeToGoalPace && !p.EffortLabel.Contains("GOAL_PACE", StringComparison.OrdinalIgnoreCase));
            var noRace = allPaces.All(p => !p.EffortLabel.Contains("RACE_SPECIFIC", StringComparison.OrdinalIgnoreCase));
            var noThreshold = allPaces.All(p => !p.EffortLabel.Contains("THRESHOLD", StringComparison.OrdinalIgnoreCase));
            var compatible = checks.All(c => c.IsCompatible);
            var continuity = new PreparationRunwayPaceContinuityAnalysis(
                checks, compatible, noRace, noGoal, noThreshold,
                compatible && noRace && noGoal && noThreshold,
                request.CoreWeekOnePaceTarget.SourceProvenance + "; typed effort compatibility, no invented tolerance");
            if (!continuity.IsValid)
                return Fail<TKey>(PreparationRunwayPaceMaterializationFailureCode.PaceContinuityViolation,
                    "Final Transition pace is not compatible with authoritative Core Week 1.", trace);

            foreach (var week in weeks)
                foreach (var slot in week.StructuralOrderedSlots)
                    trace.Add($"week={week.OriginalWeek.SegmentLocalWeekNumber}; slot={slot.OriginalSlot.PrescribedSlot.StructuralSlot.SlotOrdinal}; workout={slot.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutId} v{slot.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutVersion}; pace={slot.PacePrescription.Kind}:{slot.PacePrescription.EffortLabel}; evidence={request.PaceContext.EvidenceState}");
            trace.Add("distance_mutation=false; date_mutation=false; goal_pace=false; race_specific_pace=false; threshold_pace=false");
            return PreparationRunwayPaceMaterializationResult<TKey>.Success(weeks, request.CoreWeekOnePaceTarget, continuity, trace);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Fail<TKey>(PreparationRunwayPaceMaterializationFailureCode.PaceMaterializationInvariantViolation,
                exception.Message, trace);
        }
    }

    private static (PreparationRunwayPaceMaterializationFailureCode Code, string Reason)? ValidateRequest<TKey>(
        PreparationRunwayPaceMaterializationRequest<TKey>? request) where TKey : notnull
    {
        if (request is null || request.DatedCombinedPlan is null || request.PaceContext is null || request.CoreWeekOnePaceTarget is null || request.Policy is null)
            return (PreparationRunwayPaceMaterializationFailureCode.InvalidPaceMaterializationRequest, "All pace-materialization inputs are required.");
        if (!request.DatedCombinedPlan.IsSuccess || request.DatedCombinedPlan.DatedRunwayWeeks is null || request.DatedCombinedPlan.DatedRunwayWeeks.Count is < 3 or > 8)
            return (PreparationRunwayPaceMaterializationFailureCode.InvalidPaceMaterializationRequest, "A successful 3..8-week dated runway is required.");
        if (request.Policy.PolicyKey != TenKPreparationRunwayPacePolicyFactory.PolicyKey || request.Policy.PolicyVersion != TenKPreparationRunwayPacePolicyFactory.PolicyVersion)
            return (PreparationRunwayPaceMaterializationFailureCode.InvalidPaceMaterializationRequest, "Approved TEN_K runway pace policy is required.");
        if (!RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayCandidate(request.PaceContext.CandidateKey, request.PaceContext.CandidateVersion))
            return (PreparationRunwayPaceMaterializationFailureCode.PaceContextUnavailable, "Pace context is not for an approved Preparation Runway candidate.");
        var targetKeyCount = request.CoreWeekOnePaceTarget.OrderedSlots.Count(s => s.Role == PreparationRunwaySlotRole.KeySession);
        var targetLongCount = request.CoreWeekOnePaceTarget.OrderedSlots.Count(s => s.Role == PreparationRunwaySlotRole.LongRun);
        if (request.CoreWeekOnePaceTarget.CandidateKey != request.PaceContext.CandidateKey || request.CoreWeekOnePaceTarget.CandidateVersion != request.PaceContext.CandidateVersion ||
            targetKeyCount < 1 || targetLongCount != 1)
            return (PreparationRunwayPaceMaterializationFailureCode.CoreWeekOnePaceTargetUnavailable, "A complete matching Core Week 1 pace target is required.");
        if (request.PaceContext.EvidenceState == PreparationRunwayPaceEvidenceState.PaceSourceUnsupported)
            return (PreparationRunwayPaceMaterializationFailureCode.PaceSourceUnsupported, "The authoritative Core pace context has no approved prescription-usable source.");
        if (request.PaceContext.EvidenceState == PreparationRunwayPaceEvidenceState.TargetTimeProductAverage && request.PaceContext.TargetFinishTimeSource != Domain.Enums.TargetFinishTimeSource.ProductAverage ||
            request.PaceContext.EvidenceState == PreparationRunwayPaceEvidenceState.TargetTimeUserProvided && request.PaceContext.TargetFinishTimeSource == Domain.Enums.TargetFinishTimeSource.ProductAverage ||
            request.PaceContext.EvidenceState == PreparationRunwayPaceEvidenceState.MissingPaceEvidence && request.PaceContext.ResolvedPaceSource.Source != PrescriptionPaceSource.EffortOnly)
            return (PreparationRunwayPaceMaterializationFailureCode.PaceEvidenceContradiction, "Pace evidence state contradicts authoritative source provenance.");
        if (request.CoreWeekOnePaceTarget.OrderedSlots.Any(s => !ValidPace(s.PacePrescription)))
            return (PreparationRunwayPaceMaterializationFailureCode.CoreWeekOnePaceTargetUnavailable, "Core Week 1 contains an invalid pace representation.");
        return null;
    }

    private static bool ValidPace(CatalogPacePrescription pace) => pace.Kind switch
    {
        CatalogPacePrescriptionKind.EffortOnly => pace.SecondsPerKilometer is null && pace.FasterBoundSecondsPerKilometer is null && pace.SlowerBoundSecondsPerKilometer is null && !string.IsNullOrWhiteSpace(pace.EffortLabel),
        CatalogPacePrescriptionKind.ExactPace => pace.SecondsPerKilometer is > 0 && pace.FasterBoundSecondsPerKilometer is null && pace.SlowerBoundSecondsPerKilometer is null,
        CatalogPacePrescriptionKind.PaceRange => pace.FasterBoundSecondsPerKilometer is > 0 && pace.SlowerBoundSecondsPerKilometer >= pace.FasterBoundSecondsPerKilometer,
        _ => false,
    };

    /// <summary>
    /// Phase 10K-FREQ.6D.7: per FREQ.6D.6, per-slot role-count equality is not
    /// a Core-entry compatibility dimension -- so a Runway slot with no
    /// structurally-corresponding Core Week 1 target slot (e.g. Runway's 3rd
    /// EASY_SUPPORT against a 2-EASY Core target, or a would-be 2nd KEY
    /// against a 1-KEY Core target) is simply not checked here, rather than
    /// treated as a failure. Every existing Intermediate 4D case has an exact
    /// 1:1 role/ordinal correspondence and is checked byte-for-byte as before.
    /// </summary>
    private static IReadOnlyList<PreparationRunwayPaceContinuityCheck> BuildContinuityChecks<TKey>(
        PreparationRunwayPacedWeek<TKey> final,
        PreparationRunwayCoreWeekOnePaceTarget core) where TKey : notnull =>
        final.StructuralOrderedSlots
            .Select(slot => (slot, structural: slot.OriginalSlot.PrescribedSlot.StructuralSlot))
            .Select(x => (x.slot, x.structural,
                target: core.OrderedSlots.SingleOrDefault(t => t.Role == x.structural.SlotRole && t.RoleOrdinal == x.structural.RoleOrdinal)))
            .Where(x => x.target is not null)
            .Select(x =>
            {
                var compatible = Compatible(x.slot.PacePrescription, x.target!.PacePrescription);
                return new PreparationRunwayPaceContinuityCheck(
                    x.structural.SlotRole, x.structural.RoleOrdinal, x.slot.PacePrescription, x.target!.PacePrescription,
                    compatible, compatible ? "same canonical effort label" : "pace representation or effort label differs");
            }).ToArray();

    private static bool Compatible(CatalogPacePrescription runway, CatalogPacePrescription core) =>
        runway.Kind == CatalogPacePrescriptionKind.EffortOnly &&
        core.Kind == CatalogPacePrescriptionKind.EffortOnly &&
        string.Equals(runway.EffortLabel, core.EffortLabel, StringComparison.Ordinal);

    private static (PreparationRunwayPaceMaterializationFailureCode Code, string Reason)? ValidatePreservation<TKey>(
        PreparationRunwayPaceMaterializationRequest<TKey> request,
        IReadOnlyList<PreparationRunwayPacedWeek<TKey>> weeks) where TKey : notnull
    {
        var originals = request.DatedCombinedPlan.DatedRunwayWeeks!.OrderBy(w => w.GlobalWeekNumber).ToArray();
        if (weeks.Count != originals.Length) return (PreparationRunwayPaceMaterializationFailureCode.PaceMaterializationInvariantViolation, "Week count changed.");
        for (var i = 0; i < originals.Length; i++)
        {
            var before = originals[i].StructuralOrderedSlots.OrderBy(s => s.PrescribedSlot.StructuralSlot.SlotOrdinal).ToArray();
            var after = weeks[i].StructuralOrderedSlots.OrderBy(s => s.OriginalSlot.PrescribedSlot.StructuralSlot.SlotOrdinal).ToArray();
            if (before.Length != after.Length || before.Select(s => s.PrescribedSlot.PlannedDistanceKm).SequenceEqual(after.Select(s => s.OriginalSlot.PrescribedSlot.PlannedDistanceKm)) is false)
                return (PreparationRunwayPaceMaterializationFailureCode.DistanceMutationDetected, "Slot distances changed during pace materialization.");
            if (before.Select(s => s.SessionDate).SequenceEqual(after.Select(s => s.OriginalSlot.SessionDate)) is false)
                return (PreparationRunwayPaceMaterializationFailureCode.DateMutationDetected, "Session dates changed during pace materialization.");
            if (before.Select(s => (s.PrescribedSlot.StructuralSlot.WorkoutId, s.PrescribedSlot.StructuralSlot.WorkoutVersion)).SequenceEqual(
                    after.Select(s => (s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutId, s.OriginalSlot.PrescribedSlot.StructuralSlot.WorkoutVersion))) is false)
                return (PreparationRunwayPaceMaterializationFailureCode.PaceMaterializationInvariantViolation, "Workout identity changed during pace materialization.");
        }
        return null;
    }

    private static PreparationRunwayPaceMaterializationFailureCode ForbiddenCode(string value) =>
        value.Contains("GOAL_PACE", StringComparison.OrdinalIgnoreCase) ? PreparationRunwayPaceMaterializationFailureCode.GoalPaceNotAllowedInRunway :
        value.Contains("THRESHOLD", StringComparison.OrdinalIgnoreCase) ? PreparationRunwayPaceMaterializationFailureCode.ThresholdPaceNotAllowedInRunway :
        PreparationRunwayPaceMaterializationFailureCode.RaceSpecificPaceNotAllowedInRunway;

    private static PreparationRunwayPaceMaterializationResult<TKey> Fail<TKey>(
        PreparationRunwayPaceMaterializationFailureCode code, string reason, IReadOnlyList<string> trace) where TKey : notnull =>
        PreparationRunwayPaceMaterializationResult<TKey>.Failure(code, reason, trace.ToArray());
}
