namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal sealed record LongHorizonCheckpointStateEvaluation(
    LongHorizonCheckpointDecision Decision,
    ValidatedSustainableLoad? EffectiveLoad);

internal static class LongHorizonCheckpointStateEvaluator
{
    public static LongHorizonCheckpointStateEvaluation Evaluate(
        LongHorizonCheckpointEvidenceSnapshot snapshot,
        ValidatedSustainableLoad? currentLoad,
        LongHorizonPriorValidatedAnchor? priorAnchor,
        (int StartGlobalWeek, int EndGlobalWeek) nextBoundary,
        Guid decisionId)
    {
        var freshPrior = ValidatePrior(priorAnchor);
        LongHorizonCheckpointOutcome outcome;
        LongHorizonCheckpointReasonCode? reason = null;
        ValidatedSustainableLoad? effective = currentLoad;

        if (snapshot.SafetyState == LongHorizonSafetyState.UnresolvedSafetyCritical)
        {
            outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked;
            reason = LongHorizonCheckpointReasonCode.SafetyReassessmentRequired;
        }
        else if (!snapshot.WindowCalendarPeriodEnded)
        {
            outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked;
            reason = LongHorizonCheckpointReasonCode.CheckpointWindowNotComplete;
        }
        else if (!snapshot.AllSessionsTerminal && freshPrior is not null && AvailabilityFeasible(snapshot))
        {
            outcome = LongHorizonCheckpointOutcome.MaintenanceOnly;
            reason = LongHorizonCheckpointReasonCode.CheckpointWindowNotComplete;
            effective = freshPrior;
        }
        else if (!snapshot.AllSessionsTerminal)
        {
            outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked;
            reason = LongHorizonCheckpointReasonCode.CheckpointWindowNotComplete;
            effective = null;
        }
        else if (currentLoad is null && snapshot.ActualWeeklyVolumesKm.Count > 0
            && snapshot.CompletedLongRunsKm.Count == 0)
        {
            outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked;
            reason = LongHorizonCheckpointReasonCode.ValidatedLongRunEvidenceUnavailable;
        }
        else if (currentLoad is null && freshPrior is null)
        {
            outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked;
            reason = LongHorizonCheckpointReasonCode.ValidatedLoadUnavailable;
        }
        else if (currentLoad is null)
        {
            outcome = LongHorizonCheckpointOutcome.MaintenanceOnly;
            reason = LongHorizonCheckpointReasonCode.CheckpointEvidenceStale;
            effective = freshPrior;
        }
        else if (currentLoad.LongRunKm is null)
        {
            outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked;
            reason = LongHorizonCheckpointReasonCode.ValidatedLongRunEvidenceUnavailable;
        }
        else if (!AvailabilityFeasible(snapshot))
        {
            outcome = LongHorizonCheckpointOutcome.NumericActivationBlocked;
            reason = LongHorizonCheckpointReasonCode.NumericWindowInfeasible;
        }
        else if (!snapshot.GrowthConfidenceSatisfied)
        {
            outcome = LongHorizonCheckpointOutcome.MaintenanceOnly;
            reason = LongHorizonCheckpointReasonCode.AdherenceConfidenceInsufficientForGrowth;
        }
        else
        {
            outcome = LongHorizonCheckpointOutcome.GrowthEligible;
        }

        var decision = new LongHorizonCheckpointDecision
        {
            DecisionId = decisionId,
            EvidenceSnapshotId = snapshot.CheckpointId,
            Outcome = outcome,
            AuthoritativeReason = reason is null ? null : LongHorizonReasonCode.FromCheckpoint(reason.Value),
            ValidatedLoad = effective,
            MaintenanceAnchorWeeklyVolumeKm = outcome == LongHorizonCheckpointOutcome.MaintenanceOnly ? effective?.WeeklyVolumeKm : null,
            ActivationWindowBoundary = nextBoundary,
            SafetyPriorityApplied = snapshot.SafetyState == LongHorizonSafetyState.UnresolvedSafetyCritical,
            CreatedAt = null,
            PolicyProvenance = "PHASE4K_3_EIGHT_CASE_PRIORITY_TABLE+PHASE4K_7_RUNTIME_V1",
        };
        LongHorizonCheckpointDecisionValidator.Validate(decision);
        return new LongHorizonCheckpointStateEvaluation(decision, effective);
    }

    private static ValidatedSustainableLoad? ValidatePrior(LongHorizonPriorValidatedAnchor? prior)
    {
        if (prior is null || !prior.IsFreshForCurrentInvocation)
            return null;
        var load = prior.Load;
        if (load.WeeklyLoadSource.Source != LongHorizonEvidenceSource.PriorValidatedCheckpointLoad
            || load.LongRunSource.Source != LongHorizonEvidenceSource.PriorValidatedCheckpointLoad
            || load.ValidationStatus != LongHorizonValidationStatus.Valid
            || load.WeeklyVolumeKm is not (> 0) || load.LongRunKm is not (> 0))
        {
            throw new LongHorizonCheckpointDecisionInvalidException(
                "MAINTENANCE_ANCHOR_UNAVAILABLE: prior anchor must be fresh, valid, complete, and carry PriorValidatedCheckpointLoad authority.");
        }
        LongHorizonValidatedSustainableLoadValidator.Validate(load);
        return load;
    }

    private static bool AvailabilityFeasible(LongHorizonCheckpointEvidenceSnapshot snapshot) =>
        snapshot.Availability.Count == 4 && snapshot.Availability.Distinct().Count() == 4;
}
