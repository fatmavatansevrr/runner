using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal static class CatalogFinalPrescribedPlanValidator
{
    private const double ToleranceKm = V1FourDaySessionVolumeAllocationPolicy.ToleranceKm;

    public static CatalogFinalPrescribedPlanValidationResult Validate(
        BoundCatalogPlan boundPlan,
        CatalogVolumeAndLongRunPlan volumePlan,
        CatalogPrescribedPlan prescribedPlan,
        PlanCatalogCandidateSummary candidate)
    {
        var errors = new List<string>();
        var boundSessions = boundPlan.Weeks.SelectMany(w => w.Sessions).OrderBy(s => (s.WeekNumber, s.Date, s.StructuralRole)).ToList();
        var prescribedSessions = prescribedPlan.Sessions.OrderBy(s => (s.WeekNumber, s.Date, s.StructuralRole)).ToList();

        if (boundPlan.Weeks.Count != prescribedPlan.Weeks.Count) errors.Add("FINAL_WEEK_COUNT_MISMATCH");
        if (boundSessions.Count != prescribedSessions.Count) errors.Add("FINAL_SESSION_COUNT_MISMATCH");
        if (prescribedPlan.Weeks.Any(w =>
            w.Sessions.Count != boundPlan.Weeks.Single(b => b.WeekNumber == w.WeekNumber).Sessions.Count))
            errors.Add("FINAL_WEEK_SESSION_COUNT_INVALID");
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
            // Phase 10K-GEN.20 -- generalized from a hardcoded structural
            // proxy ("this week has exactly 3 sessions" => 3D's 0.42 cap,
            // else a blanket 0.40 for every other frequency) to the real,
            // candidate-specific VolumeSafetyPolicy.LongRunHardCapShare the
            // upstream volume planner (CatalogVolumeAndLongRunPlanner) already
            // enforces for this exact candidate -- this defensive final-stage
            // check must consult the SAME real authority, not a second,
            // independently-guessed proxy. The prior proxy was never wrong
            // for 3D/4D/5D/6D (5D/6D's real 0.36 cap is always tighter than
            // the fallback 0.40, so the fallback never bound), but it was
            // wrong for 2D: a 2D week's real session count is 2 (never 3),
            // so it fell into the "else" 0.40 branch -- tighter than 2D's
            // real, GEN.11-approved 0.60 cap, wrongly rejecting every valid
            // 2D plan. Zero-delta for every other frequency, verified by full
            // regression (the resolved value is byte-identical to the prior
            // proxy's own value in each case).
            var hardCap = ResolveLongRunHardCapShare(candidate);
            if (prescribedLongRun is not null && prescribedLongRun.PlannedDistanceKm > week.PlannedWeeklyVolumeKm * hardCap + ToleranceKm)
            {
                errors.Add($"FINAL_WEEK_{week.WeekNumber}_LONG_RUN_SHARE_EXCEEDS_CAP");
            }
            if (week.Sessions.Any(s => s.PlannedDistanceKm <= 0)) errors.Add($"FINAL_WEEK_{week.WeekNumber}_NON_POSITIVE_SESSION");
            foreach (var session in week.Sessions)
            {
                ValidateSession(session, errors);
            }
        }

        ValidateTaperCompleteness(prescribedPlan, errors);

        return new CatalogFinalPrescribedPlanValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Phase 10K-GEN.20 -- mirrors <see cref="CatalogVolumeAndLongRunPlanner"/>'s
    /// own per-candidate <see cref="VolumeSafetyPolicy"/> dispatch (the single
    /// real authority for a candidate's long-run hard-cap share), rather than
    /// re-deriving it from structural session-count. Every branch here
    /// reproduces an already-existing, already-approved policy value
    /// verbatim -- no new numeric authority.
    /// </summary>
    private static double ResolveLongRunHardCapShare(PlanCatalogCandidateSummary candidate)
    {
        if (candidate.Level == "ADVANCED")
        {
            return VolumeSafetyPolicy.ForAdvancedDaysPerWeek(candidate.DaysPerWeek).LongRunHardCapShare;
        }
        if (candidate.Level == "NEW")
        {
            // Phase 10K-GEN.23 -- recurring-defect-family fix (the same
            // "structural-session-count proxy instead of the real
            // per-candidate VolumeSafetyPolicy" shape GEN.10/GEN.12/GEN.17
            // (x2)/GEN.19/GEN.20 already found and fixed): before this
            // phase, every Beginner DaysPerWeek other than 2 silently fell
            // into the BeginnerFourDay branch. That was never wrong before
            // now (4D was the only other admitted Beginner frequency), but
            // it would have been the identical wrong-cap defect GEN.20 §101
            // describes for 2D the moment Beginner x3D existed. Adds an
            // explicit 3D branch (VolumeSafetyPolicy.ThreeDayBeginner's own
            // 0.42 cap, matching ThreeDayIntermediate's normal-week cap
            // exactly -- required so the unchanged 5.0km normal-week LONG
            // floor stays satisfiable) BEFORE it could ever be silently
            // exercised. Byte-identical for 2D/4D.
            return candidate.DaysPerWeek switch
            {
                2 => VolumeSafetyPolicy.Beginner2D.LongRunHardCapShare,
                3 => VolumeSafetyPolicy.ThreeDayBeginner.LongRunHardCapShare,
                _ => VolumeSafetyPolicy.BeginnerFourDay.LongRunHardCapShare,
            };
        }
        return candidate.DaysPerWeek switch
        {
            2 => VolumeSafetyPolicy.Intermediate2D.LongRunHardCapShare,
            3 => VolumeSafetyPolicy.ThreeDayIntermediate.LongRunHardCapShare,
            5 => VolumeSafetyPolicy.FiveDayIntermediate.LongRunHardCapShare,
            6 => VolumeSafetyPolicy.SixDayIntermediate.LongRunHardCapShare,
            _ => VolumeSafetyPolicy.Default.LongRunHardCapShare,
        };
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

    /// <summary>
    /// Phase 10K-FREQ.6D.4D.5D — the same Legacy/ProfileBacked partition
    /// principle FREQ.6D.4D.5D applied to <c>CatalogPrescriptionContextValidator</c>,
    /// found necessary a second time here (a second, closely-related
    /// occurrence of the identical root cause disclosed by FREQ.6D.4D.5C,
    /// not a new independent blocker): this validator's own
    /// <c>taperSharpen.Count != 1</c> check hardcoded the same legacy
    /// <c>V1_TAPER_SHARPEN_PRESCRIPTION_POLICY</c> identity unconditionally.
    /// For Legacy Taper KEY_SESSION instances the exact pre-existing
    /// requirement (exactly one, matching the full <see cref="ValidateTaperSharpen"/>
    /// content-structure check) is unchanged. For ProfileBacked instances
    /// (real 5D dual-lane Taper) this validator performs no additional
    /// check — their completeness was already proven upstream by Split-C's
    /// fail-closed exact-execution-resolution guarantee before this final
    /// plan could ever be reached with a ProfileBacked session present.
    /// </summary>
    private static void ValidateTaperCompleteness(CatalogPrescribedPlan prescribedPlan, List<string> errors)
    {
        var taperKeySessions = prescribedPlan.Sessions.Where(s => s.PhaseKey == "TAPER" && s.StructuralRole == "KEY_SESSION").ToList();
        var legacyTaperKeySessions = taperKeySessions.Where(s => s.PrescriptionSource is CatalogSessionPrescriptionSource.Legacy).ToList();
        var profileBackedTaperKeySessions = taperKeySessions.Where(s => s.PrescriptionSource is CatalogSessionPrescriptionSource.ProfileBacked).ToList();

        if (legacyTaperKeySessions.Count > 0)
        {
            var legacyTaperSharpen = legacyTaperKeySessions.Where(V1TaperSharpenPrescriptionPolicy.IsTaperSharpen).ToList();
            if (legacyTaperKeySessions.Count != 1 || legacyTaperSharpen.Count != 1)
            {
                errors.Add("FINAL_TAPER_SHARPEN_COUNT_INVALID");
            }
            else
            {
                ValidateTaperSharpen(legacyTaperSharpen[0], errors);
            }
        }
        else if (profileBackedTaperKeySessions.Count == 0)
        {
            // Neither Legacy nor ProfileBacked Taper completeness authority
            // present at all -- the exact pre-existing "zero Taper sessions"
            // failure mode, unchanged.
            errors.Add("FINAL_TAPER_SHARPEN_COUNT_INVALID");
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

        var validation = CatalogFinalPrescribedPlanValidator.Validate(request.BoundPlan, request.VolumePlan, finalPlan, request.Candidate);
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
