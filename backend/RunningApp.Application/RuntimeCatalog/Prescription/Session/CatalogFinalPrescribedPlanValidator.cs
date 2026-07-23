using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal static class CatalogFinalPrescribedPlanValidator
{
    private const double ToleranceKm = V1FourDaySessionVolumeAllocationPolicy.ToleranceKm;

    public static CatalogFinalPrescribedPlanValidationResult Validate(
        BoundCatalogPlan boundPlan,
        CatalogVolumeAndLongRunPlan volumePlan,
        CatalogPrescribedPlan prescribedPlan)
    {
        var errors = new List<string>();
        var boundSessions = boundPlan.Weeks.SelectMany(w => w.Sessions).OrderBy(s => (s.WeekNumber, s.Date, s.StructuralRole)).ToList();
        var prescribedSessions = prescribedPlan.Sessions.OrderBy(s => (s.WeekNumber, s.Date, s.StructuralRole)).ToList();

        if (boundPlan.Weeks.Count != prescribedPlan.Weeks.Count) errors.Add("FINAL_WEEK_COUNT_MISMATCH");
        if (boundSessions.Count != prescribedSessions.Count) errors.Add("FINAL_SESSION_COUNT_MISMATCH");
        if (prescribedPlan.Weeks.Any(w => w.Sessions.Count != 4)) errors.Add("FINAL_WEEK_SESSION_COUNT_INVALID");
        if (prescribedPlan.Sessions.Any(s => s.Prescription.Status == CatalogSessionPrescriptionStatus.BaselinePrescribedSharpeningPending))
        {
            errors.Add("FINAL_PENDING_PRESCRIPTION_STATE");
        }
        if (prescribedPlan.Sessions.Any(s => !s.ValidationResult.IsValid)) errors.Add("FINAL_INVALID_SESSION");

        for (var i = 0; i < Math.Min(boundSessions.Count, prescribedSessions.Count); i++)
        {
            var bound = boundSessions[i];
            var prescribed = prescribedSessions[i];
            if (bound.WeekNumber != prescribed.WeekNumber ||
                bound.Date != prescribed.Date ||
                bound.PhaseKey != prescribed.PhaseKey ||
                bound.ProgressionStageKey != prescribed.ProgressionStageKey ||
                bound.StructuralRole != prescribed.StructuralRole ||
                bound.WorkoutDefinitionKey != prescribed.WorkoutDefinitionKey ||
                bound.WorkoutDefinitionVersion != prescribed.WorkoutDefinitionVersion)
            {
                errors.Add($"FINAL_BOUND_SESSION_IDENTITY_MISMATCH_{i}");
            }
        }

        foreach (var week in prescribedPlan.Weeks)
        {
            var volumeWeek = volumePlan.WeeklyVolumePlan.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var longRun = volumePlan.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            if (Math.Abs(week.AccountedWeeklyDistanceKm - volumeWeek.PlannedWeeklyVolumeKm) > ToleranceKm)
            {
                errors.Add($"FINAL_WEEK_{week.WeekNumber}_DISTANCE_MISMATCH");
            }

            var sessionTotal = V1FourDaySessionVolumeAllocationPolicy.Round(week.Sessions.Sum(s => s.PlannedDistanceKm));
            if (Math.Abs(sessionTotal - week.AccountedWeeklyDistanceKm) > ToleranceKm)
            {
                errors.Add($"FINAL_WEEK_{week.WeekNumber}_SESSION_TOTAL_MISMATCH");
            }

            var prescribedLongRun = week.Sessions.SingleOrDefault(s => s.StructuralRole == "LONG_RUN");
            if (prescribedLongRun is null || Math.Abs(prescribedLongRun.PlannedDistanceKm - longRun.PlannedLongRunDistanceKm) > ToleranceKm)
            {
                errors.Add($"FINAL_WEEK_{week.WeekNumber}_LONG_RUN_MISMATCH");
            }
            if (prescribedLongRun is not null && prescribedLongRun.PlannedDistanceKm > week.PlannedWeeklyVolumeKm * 0.40d + ToleranceKm)
            {
                errors.Add($"FINAL_WEEK_{week.WeekNumber}_LONG_RUN_SHARE_EXCEEDS_CAP");
            }
            if (week.Sessions.Any(s => s.PlannedDistanceKm <= 0)) errors.Add($"FINAL_WEEK_{week.WeekNumber}_NON_POSITIVE_SESSION");
            foreach (var session in week.Sessions)
            {
                ValidateSession(session, errors);
            }
        }

        var taperSharpen = prescribedPlan.Sessions.Where(V1TaperSharpenPrescriptionPolicy.IsTaperSharpen).ToList();
        if (taperSharpen.Count != 1)
        {
            errors.Add("FINAL_TAPER_SHARPEN_COUNT_INVALID");
        }
        else
        {
            ValidateTaperSharpen(taperSharpen[0], errors);
        }

        return new CatalogFinalPrescribedPlanValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateSession(CatalogPrescribedSession session, List<string> errors)
    {
        var segments = session.Prescription.OrderedSegments;
        if (segments.Count == 0) errors.Add($"FINAL_SESSION_{session.WeekNumber}_{session.StructuralRole}_NO_SEGMENTS");
        if (segments.Any(s => s.DistanceKm < 0)) errors.Add($"FINAL_SESSION_{session.WeekNumber}_{session.StructuralRole}_NEGATIVE_SEGMENT");
        var accounted = V1FourDaySessionVolumeAllocationPolicy.Round(segments.Where(s => s.CountsTowardSessionDistance).Sum(s => s.DistanceKm));
        if (Math.Abs(accounted - session.PlannedDistanceKm) > ToleranceKm)
        {
            errors.Add($"FINAL_SESSION_{session.WeekNumber}_{session.StructuralRole}_DISTANCE_ACCOUNTING_MISMATCH");
        }
        if (session.WorkoutDefinitionKey != "GOAL_PACE_TEN_K" &&
            (session.Prescription.PacePrescription.Kind == CatalogPacePrescriptionKind.ExactPace ||
             segments.Any(s => s.PacePrescription.Kind == CatalogPacePrescriptionKind.ExactPace)))
        {
            errors.Add($"FINAL_SESSION_{session.WeekNumber}_{session.StructuralRole}_UNSUPPORTED_EXACT_PACE");
        }
        if (session.Prescription.DurationPrescription.Kind == CatalogDurationKind.EstimatedFromPace &&
            session.Prescription.PacePrescription.Kind != CatalogPacePrescriptionKind.ExactPace)
        {
            errors.Add($"FINAL_SESSION_{session.WeekNumber}_{session.StructuralRole}_UNSUPPORTED_DURATION");
        }
    }

    private static void ValidateTaperSharpen(CatalogPrescribedSession session, List<string> errors)
    {
        if (session.PhaseKey != "TAPER") errors.Add("FINAL_TAPER_SHARPEN_PHASE_CHANGED");
        if (session.ProgressionStageKey != "TAPER_SHARPEN") errors.Add("FINAL_TAPER_SHARPEN_STAGE_CHANGED");
        if (session.StructuralRole != "KEY_SESSION") errors.Add("FINAL_TAPER_SHARPEN_ROLE_CHANGED");
        if (session.WorkoutDefinitionKey != "EASY_STANDARD") errors.Add("FINAL_TAPER_SHARPEN_WORKOUT_CHANGED");
        if (session.Prescription.Status != CatalogSessionPrescriptionStatus.FinalPrescriptionComplete) errors.Add("FINAL_TAPER_SHARPEN_STATUS_INVALID");
        var componentTypes = session.Prescription.OrderedSegments.Select(s => s.ComponentType).ToArray();
        if (!componentTypes.SequenceEqual(["EASY_BASELINE", "CONTROLLED_SHARPENING", "EASY_RECOVERY"]))
        {
            errors.Add("FINAL_TAPER_SHARPEN_COMPONENTS_INVALID");
        }
        if (!session.Prescription.OrderedSegments.Any(s => s.ComponentType == "CONTROLLED_SHARPENING")) errors.Add("FINAL_TAPER_SHARPEN_NO_STIMULUS");
        if (session.Prescription.OrderedSegments.All(s => s.IntensityDescriptor == "CONTROLLED_FAST_RELAXED")) errors.Add("FINAL_TAPER_SHARPEN_WHOLE_RUN_ACCELERATED");
        if (session.Prescription.OrderedSegments.Any(s => s.PacePrescription.Source == CatalogPaceSourceSelection.TargetGoalDerived)) errors.Add("FINAL_TAPER_SHARPEN_BORROWED_GOAL_PACE");
        if (session.Prescription.OrderedSegments.Any(s => s.Duration is not null)) errors.Add("FINAL_TAPER_SHARPEN_DURATION_UNSUPPORTED");
        if (!session.VolumeAllocationProvenance.Contains(V1TaperSharpenPrescriptionPolicy.PolicyKey, StringComparison.Ordinal)) errors.Add("FINAL_TAPER_SHARPEN_POLICY_PROVENANCE_MISSING");
    }
}

internal interface ICatalogFinalPrescribedPlanFinalizer
{
    CatalogPrescribedPlan Complete(CatalogFinalPrescriptionRequest request);
}

internal sealed class CatalogFinalPrescribedPlanFinalizer : ICatalogFinalPrescribedPlanFinalizer
{
    public CatalogPrescribedPlan Complete(CatalogFinalPrescriptionRequest request)
    {
        if (request.BaselinePlan.Sessions.Any(s => s.Prescription.Status == CatalogSessionPrescriptionStatus.BaselinePrescribedSharpeningPending &&
                                                  !V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s)))
        {
            throw new CatalogPendingPrescriptionStateException("Pending prescription state exists outside the supported TAPER_SHARPEN completion policy.");
        }

        var finalWeeks = request.BaselinePlan.Weeks
            .Select(week =>
            {
                var finalSessions = week.Sessions
                    .Select(s => V1TaperSharpenPrescriptionPolicy.IsTaperSharpen(s)
                        ? V1TaperSharpenPrescriptionPolicy.Complete(s)
                        : s)
                    .ToArray();
                return week with { Sessions = finalSessions };
            })
            .ToArray();

        var finalPlan = request.BaselinePlan with
        {
            Weeks = finalWeeks,
            Sessions = finalWeeks.SelectMany(w => w.Sessions).ToArray()
        };

        var validation = CatalogFinalPrescribedPlanValidator.Validate(request.BoundPlan, request.VolumePlan, finalPlan);
        finalPlan = finalPlan with
        {
            ValidationResult = new CatalogSessionPrescriptionValidationResult(validation.IsValid, validation.Errors)
        };

        if (!validation.IsValid)
        {
            if (validation.Errors.Contains("FINAL_PENDING_PRESCRIPTION_STATE", StringComparer.Ordinal))
            {
                throw new CatalogPendingPrescriptionStateException(string.Join(", ", validation.Errors));
            }
            throw new CatalogFinalPrescribedPlanInvalidException(string.Join(", ", validation.Errors));
        }

        return finalPlan;
    }
}
