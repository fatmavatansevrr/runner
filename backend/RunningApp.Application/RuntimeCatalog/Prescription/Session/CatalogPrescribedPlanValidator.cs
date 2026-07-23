using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal static class CatalogPrescribedPlanValidator
{
    private const double ToleranceKm = V1FourDaySessionVolumeAllocationPolicy.ToleranceKm;

    public static CatalogSessionPrescriptionValidationResult Validate(
        BoundCatalogPlan boundPlan,
        CatalogVolumeAndLongRunPlan volumePlan,
        IReadOnlyList<CatalogPrescribedWeek> prescribedWeeks)
    {
        var errors = new List<string>();
        var boundSessions = boundPlan.Weeks.SelectMany(w => w.Sessions).ToList();
        var prescribedSessions = prescribedWeeks.SelectMany(w => w.Sessions).ToList();
        if (boundSessions.Count != prescribedSessions.Count)
        {
            errors.Add("PRESCRIPTION_SESSION_COUNT_MISMATCH");
        }

        foreach (var week in prescribedWeeks)
        {
            var volumeWeek = volumePlan.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRun = volumePlan.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            if (Math.Abs(week.AccountedWeeklyDistanceKm - volumeWeek.PlannedWeeklyVolumeKm) > ToleranceKm)
            {
                errors.Add($"WEEK_{week.WeekNumber}_DISTANCE_MISMATCH");
            }
            var prescribedLongRun = week.Sessions.SingleOrDefault(s => s.StructuralRole == "LONG_RUN");
            if (prescribedLongRun is null || Math.Abs(prescribedLongRun.PlannedDistanceKm - longRun.PlannedLongRunDistanceKm) > ToleranceKm)
            {
                errors.Add($"WEEK_{week.WeekNumber}_LONG_RUN_MISMATCH");
            }
            if (week.Sessions.Any(s => !s.ValidationResult.IsValid))
            {
                errors.Add($"WEEK_{week.WeekNumber}_INVALID_SESSION");
            }
        }

        return new CatalogSessionPrescriptionValidationResult(errors.Count == 0, errors);
    }
}
