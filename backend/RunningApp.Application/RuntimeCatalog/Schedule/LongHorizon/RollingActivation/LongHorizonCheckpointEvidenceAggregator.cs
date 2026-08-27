using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal sealed record LongHorizonTrainingDayEvidenceRow(int GlobalWeekNumber, TrainingDay TrainingDay);

internal sealed record LongHorizonPriorValidatedAnchor(
    ValidatedSustainableLoad Load,
    bool IsFreshForCurrentInvocation,
    int SourceContextSequence);

internal sealed record LongHorizonCheckpointAggregationResult(
    LongHorizonCheckpointEvidenceSnapshot Snapshot,
    ValidatedSustainableLoad? CurrentValidatedLoad);

/// <summary>Phase 4K.7 adapter/aggregator over supplied real TrainingDay entities; no persistence loading.</summary>
internal static class LongHorizonCheckpointEvidenceAggregator
{
    public static LongHorizonCheckpointAggregationResult Aggregate(
        LongHorizonStructuralRoadmap roadmap,
        RollingNumericActivationWindow previousWindow,
        IReadOnlyList<LongHorizonTrainingDayEvidenceRow> suppliedRows,
        DateOnly checkpointDate,
        IReadOnlyList<DayOfWeek> availability,
        LongHorizonSafetyState safetyState,
        LongHorizonPriorValidatedAnchor? priorAnchor,
        LongHorizonContextVersion contextVersion,
        Guid checkpointId,
        int daysPerWeek = 4)
    {
        // Phase 10K-FREQ.6D.18 -- selects the already-approved FiveDayIntermediate
        // policy's own LongRunHardCapShare (0.36) for 5D evidence, mirroring the
        // same daysPerWeek-derived-from-request pattern already used elsewhere in
        // this runtime; byte-identical for every 4D caller (VolumeSafetyPolicy.Default, 0.40).
        var policy = daysPerWeek == 5 ? VolumeSafetyPolicy.FiveDayIntermediate : VolumeSafetyPolicy.Default;
        var windowWeeks = Enumerable.Range(previousWindow.StartGlobalWeek, previousWindow.ActualWindowSizeWeeks).ToHashSet();
        var rows = suppliedRows
            .Where(row => windowWeeks.Contains(row.GlobalWeekNumber) && row.TrainingDay.DayType != TrainingDayType.Rest)
            .ToList();
        ValidateRows(rows);

        var expectedSessionCount = roadmap.Weeks
            .Where(week => windowWeeks.Contains(week.GlobalWeekNumber))
            .Sum(week => week.StructuralWorkoutRoles.Count);
        var terminal = rows.Count == expectedSessionCount && rows.All(row => IsTerminal(row.TrainingDay.Status));
        var windowEnd = previousWindow.Weeks
            .Where(week => week.CalendarDates is not null)
            .Select(week => week.CalendarDates!.Value.End)
            .DefaultIfEmpty(checkpointDate)
            .Max();
        var periodEnded = checkpointDate > windowEnd;

        var recoveryWeeks = roadmap.Weeks
            .Where(week => windowWeeks.Contains(week.GlobalWeekNumber)
                && week.Stage.Contains("Consolidation", StringComparison.OrdinalIgnoreCase))
            .Select(week => week.GlobalWeekNumber).ToHashSet();
        var nonRecovery = windowWeeks.Except(recoveryWeeks).OrderBy(week => week).ToList();
        var usableByWeek = rows
            .Where(row => row.TrainingDay.Status == TrainingDayStatus.Completed && row.TrainingDay.ActualDistanceKm is > 0)
            .GroupBy(row => row.GlobalWeekNumber)
            .ToDictionary(group => group.Key, group => Round(group.Sum(row => row.TrainingDay.ActualDistanceKm!.Value)));
        var usableEvidenceWeeks = nonRecovery.Where(usableByWeek.ContainsKey).ToList();
        var growthConfidence = nonRecovery.All(usableByWeek.ContainsKey);

        var completedLongRuns = rows
            .Where(row => row.TrainingDay.Status == TrainingDayStatus.Completed
                && row.TrainingDay.IsLongRun && row.TrainingDay.ActualDistanceKm is > 0
                && !recoveryWeeks.Contains(row.GlobalWeekNumber))
            .Select(row => row.TrainingDay.ActualDistanceKm!.Value).ToList();
        var completed = rows.Count(row => row.TrainingDay.Status == TrainingDayStatus.Completed);
        var missed = rows.Count(row => row.TrainingDay.Status is TrainingDayStatus.Missed or TrainingDayStatus.Skipped or TrainingDayStatus.SoftMissed);
        var adherence = expectedSessionCount == 0 ? 0 : Math.Round((double)completed / expectedSessionCount * 100d, 1);

        ValidatedSustainableLoad? load = null;
        if (usableEvidenceWeeks.Count > 0 && completedLongRuns.Count > 0)
        {
            var weekly = Round(usableEvidenceWeeks.Average(week => usableByWeek[week]));
            var longRunMean = Round(completedLongRuns.Average());
            var longRunCap = Round(weekly * policy.LongRunHardCapShare);
            var longRun = Math.Min(longRunMean, longRunCap);
            load = new ValidatedSustainableLoad
            {
                WeeklyVolumeKm = weekly,
                LongRunKm = longRun,
                EvidenceWindowStartWeek = previousWindow.StartGlobalWeek,
                EvidenceWindowEndWeek = previousWindow.EndGlobalWeek,
                CompletedEvidenceWeekNumbers = usableEvidenceWeeks,
                ExcludedRecoveryWeekNumbers = recoveryWeeks.OrderBy(week => week).ToList(),
                WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
                LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
                RoundingPolicy = $"VolumeSafetyPolicy.{policy.RoundingIncrementKm:0.0}km",
                LongRunCapPolicy = $"VolumeSafetyPolicy.LongRunHardCapShare={policy.LongRunHardCapShare:0.00}",
                ValidationStatus = LongHorizonValidationStatus.Valid,
                Provenance = "Completed TrainingDay.ActualDistanceKm only; recovery weeks excluded from weekly mean",
                ContextVersion = contextVersion,
            };
            LongHorizonValidatedSustainableLoadValidator.Validate(load);
        }

        var snapshot = new LongHorizonCheckpointEvidenceSnapshot
        {
            CheckpointId = checkpointId,
            CheckpointDate = checkpointDate,
            ActivatedWindowStartWeek = previousWindow.StartGlobalWeek,
            ActivatedWindowEndWeek = previousWindow.EndGlobalWeek,
            WindowCalendarPeriodEnded = periodEnded,
            AllSessionsTerminal = terminal,
            ActualWeeklyVolumesKm = usableEvidenceWeeks.Select(week => usableByWeek[week]).ToList(),
            ActualWeeklyVolumeByGlobalWeek = usableByWeek,
            UsableNonRecoveryEvidenceWeeks = usableEvidenceWeeks,
            ExcludedRecoveryWeeks = recoveryWeeks.OrderBy(week => week).ToList(),
            GrowthConfidenceSatisfied = growthConfidence,
            CompletedLongRunsKm = completedLongRuns,
            CompletedRunsCount = completed,
            PlannedRunsCount = expectedSessionCount,
            AdherenceRatePercent = adherence,
            MissedSessionCount = missed,
            PriorValidatedWeeklyLoadKm = priorAnchor is { IsFreshForCurrentInvocation: true } ? priorAnchor.Load.WeeklyVolumeKm : null,
            PriorValidatedLongRunKm = priorAnchor is { IsFreshForCurrentInvocation: true } ? priorAnchor.Load.LongRunKm : null,
            Availability = availability,
            SafetyState = safetyState,
            CurrentSegment = LongHorizonStructuralSegmentType.GeneralEndurance,
            WeeksUntilRunway = roadmap.GeneralEnduranceWeeks - previousWindow.EndGlobalWeek,
            EvidenceSourceMetadata = LongHorizonEvidenceAuthorityRecord.Create(LongHorizonEvidenceSource.CompletedTrainingHistory, LongHorizonEvidenceAuthorityStatus.Authoritative),
            ContextVersion = contextVersion,
        };
        LongHorizonCheckpointEvidenceSnapshotValidator.Validate(snapshot);
        return new LongHorizonCheckpointAggregationResult(snapshot, load);
    }

    private static void ValidateRows(IReadOnlyList<LongHorizonTrainingDayEvidenceRow> rows)
    {
        foreach (var row in rows)
        {
            var day = row.TrainingDay;
            if (day.ActualDistanceKm is < 0
                || (day.Status != TrainingDayStatus.Completed && day.ActualDistanceKm is not null))
            {
                throw new LongHorizonCheckpointDecisionInvalidException(
                    $"EVIDENCE_CONFLICT_UNRESOLVED: TrainingDay {day.Id} carries contradictory status/actual-distance evidence.");
            }
        }
    }

    internal static bool IsTerminal(TrainingDayStatus status) => status is
        TrainingDayStatus.Completed or TrainingDayStatus.Missed or TrainingDayStatus.Skipped or TrainingDayStatus.SoftMissed;

    internal static double Round(double value) =>
        Math.Round(value / VolumeSafetyPolicy.Default.RoundingIncrementKm, MidpointRounding.AwayFromZero)
        * VolumeSafetyPolicy.Default.RoundingIncrementKm;
}
