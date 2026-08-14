namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5 Part 6 — a typed representation of Phase 4K.2's approved
/// validated-sustainable-load OUTPUT. This phase represents the shape only
/// -- the actual mean-of-non-recovery-weeks calculation (Phase 4K.2 §2/§3)
/// is not implemented here.
/// </summary>
internal sealed record ValidatedSustainableLoad
{
    public double? WeeklyVolumeKm { get; init; }
    public double? LongRunKm { get; init; }
    public required int EvidenceWindowStartWeek { get; init; }
    public required int EvidenceWindowEndWeek { get; init; }
    public required IReadOnlyList<int> CompletedEvidenceWeekNumbers { get; init; }
    public required IReadOnlyList<int> ExcludedRecoveryWeekNumbers { get; init; }
    public required LongHorizonEvidenceAuthorityRecord WeeklyLoadSource { get; init; }
    public required LongHorizonEvidenceAuthorityRecord LongRunSource { get; init; }
    public required string RoundingPolicy { get; init; }
    public required string LongRunCapPolicy { get; init; }
    public required LongHorizonValidationStatus ValidationStatus { get; init; }
    public string? Provenance { get; init; }
    public LongHorizonContextVersion? ContextVersion { get; init; }
}

/// <summary>
/// Phase 4K.5 Part 6 — validates that a Valid load carries real values
/// sourced from actual completed evidence, never from a planned target, and
/// that recovery weeks are never double-counted as completed evidence.
/// </summary>
internal static class LongHorizonValidatedSustainableLoadValidator
{
    public static void Validate(ValidatedSustainableLoad load)
    {
        if (load.ValidationStatus == LongHorizonValidationStatus.Valid)
        {
            if (load.WeeklyVolumeKm is null || load.LongRunKm is null)
            {
                throw new LongHorizonCheckpointDecisionInvalidException(
                    "A Valid ValidatedSustainableLoad must carry non-null WeeklyVolumeKm and LongRunKm.");
            }

            if (load.WeeklyVolumeKm <= 0 || load.LongRunKm <= 0)
            {
                throw new LongHorizonCheckpointDecisionInvalidException(
                    "A Valid ValidatedSustainableLoad's WeeklyVolumeKm and LongRunKm must be positive.");
            }

            if (load.WeeklyLoadSource.Source != LongHorizonEvidenceSource.CompletedTrainingHistory
                && load.WeeklyLoadSource.Source != LongHorizonEvidenceSource.PriorValidatedCheckpointLoad)
            {
                throw new LongHorizonCheckpointDecisionInvalidException(
                    "WeeklyLoadSource must be actual completed evidence (CompletedTrainingHistory or PriorValidatedCheckpointLoad) -- " +
                    "a planned weekly target can never be marked validated (Phase 4K.2 §2).");
            }

            if (load.LongRunSource.Source != LongHorizonEvidenceSource.CompletedTrainingHistory
                && load.LongRunSource.Source != LongHorizonEvidenceSource.PriorValidatedCheckpointLoad)
            {
                throw new LongHorizonCheckpointDecisionInvalidException(
                    "LongRunSource must be actual completed evidence (CompletedTrainingHistory or PriorValidatedCheckpointLoad).");
            }
        }
        else if (load.WeeklyVolumeKm is not null || load.LongRunKm is not null)
        {
            throw new LongHorizonCheckpointDecisionInvalidException(
                "An Unavailable ValidatedSustainableLoad must carry null WeeklyVolumeKm and LongRunKm.");
        }

        if (load.CompletedEvidenceWeekNumbers.Intersect(load.ExcludedRecoveryWeekNumbers).Any())
        {
            throw new LongHorizonCheckpointDecisionInvalidException(
                "A week cannot be both a completed-evidence week and an excluded recovery week (Phase 4K.2 §3).");
        }
    }
}
