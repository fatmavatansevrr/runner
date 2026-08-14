using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

/// <summary>
/// Adapts an already-resolved Core prescription pace source. It never calls
/// PaceSourceResolver, GoalFeasibilityResolver, or creates a competing
/// evidence hierarchy.
/// </summary>
internal static class PreparationRunwayPaceContextAdapter
{
    public static PreparationRunwayPaceContext FromAuthoritativeCoreContext(
        string candidateKey,
        int candidateVersion,
        ResolvedPrescriptionPaceSource resolved,
        TargetFinishTimeSource? targetFinishTimeSource,
        string evidenceProvenance)
    {
        var state = resolved.Source switch
        {
            PrescriptionPaceSource.RecentRace => PreparationRunwayPaceEvidenceState.ProvidedRecentRace,
            PrescriptionPaceSource.TargetGoal when targetFinishTimeSource == TargetFinishTimeSource.ProductAverage => PreparationRunwayPaceEvidenceState.TargetTimeProductAverage,
            PrescriptionPaceSource.TargetGoal => PreparationRunwayPaceEvidenceState.TargetTimeUserProvided,
            PrescriptionPaceSource.EffortOnly when resolved.RawOutputValue == "NONE" => PreparationRunwayPaceEvidenceState.MissingPaceEvidence,
            PrescriptionPaceSource.Unresolved when resolved.RawOutputValue == "TARGET_TIME" && resolved.GoalFeasibilityValue == "UNSUPPORTED" => PreparationRunwayPaceEvidenceState.TargetTimeRequestedGoalInfeasible,
            _ => PreparationRunwayPaceEvidenceState.PaceSourceUnsupported,
        };

        return new PreparationRunwayPaceContext(
            candidateKey, candidateVersion, resolved, state, targetFinishTimeSource,
            evidenceProvenance, $"{resolved.RawConditionType}:{resolved.RawStatus}:{resolved.RawOutputValue ?? "null"}:{resolved.GoalFeasibilityValue}:{resolved.Reason}");
    }
}
