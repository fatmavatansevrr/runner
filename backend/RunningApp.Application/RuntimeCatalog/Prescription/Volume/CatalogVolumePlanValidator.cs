using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

internal static class CatalogVolumePlanValidator
{
    public static CatalogVolumeValidationResult ValidateWeeklyPlan(BoundCatalogPlan boundPlan, CatalogWeeklyVolumePlan plan)
    {
        var errors = new List<string>();
        var boundWeeks = boundPlan.Weeks.OrderBy(w => w.WeekNumber).ToList();
        if (boundWeeks.Count != plan.Weeks.Count)
        {
            errors.Add("WEEKLY_VOLUME_WEEK_COUNT_MISMATCH");
        }

        if (plan.Weeks.Select(w => w.WeekNumber).Distinct().Count() != plan.Weeks.Count)
        {
            errors.Add("WEEKLY_VOLUME_WEEK_NUMBERS_NOT_UNIQUE");
        }

        foreach (var bound in boundWeeks)
        {
            var planned = plan.Weeks.SingleOrDefault(w => w.WeekNumber == bound.WeekNumber);
            if (planned is null)
            {
                errors.Add($"WEEKLY_VOLUME_WEEK_MISSING:{bound.WeekNumber}");
                continue;
            }

            if (planned.PhaseKey != bound.PhaseKey)
            {
                errors.Add($"WEEKLY_VOLUME_PHASE_MISMATCH:{bound.WeekNumber}");
            }

            if (planned.PlannedWeeklyVolumeKm < 0)
            {
                errors.Add($"WEEKLY_VOLUME_NEGATIVE:{bound.WeekNumber}");
            }

            if (planned.PlannedWeeklyVolumeKm > plan.PeakVolumeKm || planned.PlannedWeeklyVolumeKm > plan.CatalogBounds.MaximumKm)
            {
                errors.Add($"WEEKLY_VOLUME_ABOVE_PEAK_OR_CATALOG_MAX:{bound.WeekNumber}");
            }

            if (string.IsNullOrWhiteSpace(planned.SourceArtifactKey) || string.IsNullOrWhiteSpace(planned.Provenance))
            {
                errors.Add($"WEEKLY_VOLUME_PROVENANCE_MISSING:{bound.WeekNumber}");
            }
        }

        var first = plan.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        if (first is not null && Math.Abs(first.PlannedWeeklyVolumeKm - plan.FirstWeekVolumeKm) > 0.001)
        {
            errors.Add("WEEK1_VOLUME_DOES_NOT_MATCH_FIRST_WEEK_SELECTION");
        }

        if (plan.ReachablePeakDecision.Classification != PeakBandClassification.BelowTypicalPeakButValid &&
            plan.PeakVolumeKm < plan.CatalogBounds.MinimumKm)
        {
            errors.Add("PEAK_VOLUME_OUTSIDE_CATALOG_BOUNDS");
        }

        if (plan.PeakVolumeKm > plan.CatalogBounds.MaximumKm)
        {
            errors.Add("PEAK_VOLUME_OUTSIDE_CATALOG_BOUNDS");
        }

        var taper = plan.Weeks.Where(w => w.IsTaperWeek).ToList();
        if (taper.Count > 0)
        {
            var preTaper = plan.Weeks.Where(w => !w.IsTaperWeek && w.WeekNumber < taper[0].WeekNumber).OrderBy(w => w.WeekNumber).LastOrDefault();
            if (preTaper is not null && taper[0].PlannedWeeklyVolumeKm >= preTaper.PlannedWeeklyVolumeKm)
            {
                errors.Add("TAPER_WEEKLY_VOLUME_NOT_REDUCED");
            }
        }

        foreach (var week in plan.Weeks)
        {
            if (week.PreviousWeekVolumeKm is null && week.ChangeKm != 0)
            {
                errors.Add($"WEEKLY_CHANGE_INVALID_FOR_FIRST_WEEK:{week.WeekNumber}");
            }

            if (week.PreviousWeekVolumeKm is { } prev && Math.Abs((week.PlannedWeeklyVolumeKm - prev) - week.ChangeKm) > 0.001)
            {
                errors.Add($"WEEKLY_CHANGE_KM_INACCURATE:{week.WeekNumber}");
            }
        }

        return new CatalogVolumeValidationResult(errors.Count == 0, errors);
    }

    public static CatalogVolumeValidationResult ValidateLongRunPlan(BoundCatalogPlan boundPlan, CatalogWeeklyVolumePlan weeklyPlan, CatalogLongRunProgression longRun)
    {
        var errors = new List<string>();
        if (weeklyPlan.Weeks.Count != longRun.Weeks.Count)
        {
            errors.Add("LONG_RUN_WEEK_COUNT_MISMATCH");
        }

        foreach (var weekly in weeklyPlan.Weeks)
        {
            var planned = longRun.Weeks.SingleOrDefault(w => w.WeekNumber == weekly.WeekNumber);
            if (planned is null)
            {
                errors.Add($"LONG_RUN_WEEK_MISSING:{weekly.WeekNumber}");
                continue;
            }

            if (planned.PhaseKey != weekly.PhaseKey)
            {
                errors.Add($"LONG_RUN_PHASE_MISMATCH:{weekly.WeekNumber}");
            }

            if (planned.PlannedLongRunDistanceKm < 0)
            {
                errors.Add($"LONG_RUN_NEGATIVE:{weekly.WeekNumber}");
            }

            if (planned.PlannedLongRunDistanceKm > planned.PlannedWeeklyVolumeKm)
            {
                errors.Add($"LONG_RUN_EXCEEDS_WEEKLY_VOLUME:{weekly.WeekNumber}");
            }

            if (planned.PlannedLongRunDistanceKm < planned.CatalogBounds.MinimumKm || planned.PlannedLongRunDistanceKm > planned.CatalogBounds.MaximumKm)
            {
                errors.Add($"LONG_RUN_OUTSIDE_COMPATIBILITY_BOUNDS:{weekly.WeekNumber}");
            }

            var boundLongRun = boundPlan.Weeks.Single(w => w.WeekNumber == weekly.WeekNumber).Sessions.SingleOrDefault(s => s.StructuralRole == "LONG_RUN");
            if (boundLongRun?.WorkoutDefinitionKey != "LONG_RUN_STANDARD")
            {
                errors.Add($"LONG_RUN_WORKOUT_IDENTITY_CHANGED_OR_MISSING:{weekly.WeekNumber}");
            }

            if (string.IsNullOrWhiteSpace(planned.SourceArtifactKey) || string.IsNullOrWhiteSpace(planned.Provenance))
            {
                errors.Add($"LONG_RUN_PROVENANCE_MISSING:{weekly.WeekNumber}");
            }
        }

        var taper = longRun.Weeks.Where(w => weeklyPlan.Weeks.Single(x => x.WeekNumber == w.WeekNumber).IsTaperWeek).ToList();
        if (taper.Count > 0)
        {
            var preTaper = longRun.Weeks.Where(w => w.WeekNumber < taper[0].WeekNumber).OrderBy(w => w.WeekNumber).LastOrDefault();
            if (preTaper is not null && taper[0].PlannedLongRunDistanceKm >= preTaper.PlannedLongRunDistanceKm)
            {
                errors.Add("TAPER_LONG_RUN_NOT_REDUCED");
            }
        }

        return new CatalogVolumeValidationResult(errors.Count == 0, errors);
    }
}
