namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal static class V1TaperSharpenPrescriptionPolicy
{
    public const string PolicyKey = "V1_TAPER_SHARPEN_PRESCRIPTION_POLICY";
    public const int PolicyVersion = 1;
    public const double MinSessionDistanceKm = 3d;
    public const double MinEasyBaselineKm = 1.5d;
    public const double RecoveryDistanceKm = 0.5d;
    public const double MinSharpeningDistanceKm = 0.5d;
    public const double MaxSharpeningDistanceKm = 1.5d;

    public static TaperSharpenCapabilityResult AssessCapability() => new(
        TaperSharpenCapabilityClassification.SupportedByAdditiveRuntimePrescriptionContract,
        "EASY_STANDARD v4 exposes DISTANCE plus EXACT_SESSION_TOTAL and no catalog components; Phase 4F.7C's internal CatalogPrescriptionSegment contract can add stage-specific runtime components without changing workout identity or catalog artifacts.",
        "Complete TAPER_SHARPEN by replacing the baseline SESSION_TOTAL segment with additive runtime EASY_BASELINE, CONTROLLED_SHARPENING, and EASY_RECOVERY segments.");

    public static CatalogPrescribedSession Complete(CatalogPrescribedSession session)
    {
        if (!IsTaperSharpen(session))
        {
            throw new CatalogTaperSharpenSchemaUnsupportedException(
                "V1 taper-sharpen policy only applies to TAPER/TAPER_SHARPEN/KEY_SESSION/EASY_STANDARD.");
        }

        var selection = SelectComponents(session.PlannedDistanceKm);
        var easyPace = EffortOnly("EASY", "V1 taper-sharpen keeps easy baseline effort-only; no numeric easy pace producer exists.");
        var sharpenPace = EffortOnly("CONTROLLED_FAST_RELAXED", "V1 taper-sharpen uses controlled effort-only sharpening; target 10K pace is not borrowed automatically.");
        var recoveryPace = EffortOnly("EASY_RECOVERY", "V1 taper-sharpen requires an easy recovery component after the controlled stimulus.");

        var segments = new[]
        {
            new CatalogPrescriptionSegment(1, selection.EasyBaselineComponentType, "EASY", selection.EasyBaselineDistanceKm, null, easyPace, true),
            new CatalogPrescriptionSegment(2, selection.SharpeningComponentType, "CONTROLLED_FAST_RELAXED", selection.SharpeningDistanceKm, null, sharpenPace, true),
            new CatalogPrescriptionSegment(3, selection.RecoveryComponentType, "EASY_RECOVERY", selection.RecoveryDistanceKm, null, recoveryPace, true)
        };

        var accounted = Round(segments.Where(s => s.CountsTowardSessionDistance).Sum(s => s.DistanceKm));
        if (Math.Abs(accounted - session.PlannedDistanceKm) > V1FourDaySessionVolumeAllocationPolicy.ToleranceKm)
        {
            throw new CatalogTaperSharpenDistanceAccountingException(
                $"TAPER_SHARPEN segment distance {accounted}km does not reconcile to assigned session distance {session.PlannedDistanceKm}km.");
        }

        var decision = DecisionFor(selection);
        var validation = Validate(session.PlannedDistanceKm, segments);
        if (!validation.IsValid)
        {
            throw new CatalogTaperSharpenPrescriptionInfeasibleException(string.Join(", ", validation.Errors));
        }

        return session with
        {
            PlannedDuration = null,
            EstimatedDuration = null,
            PaceSourceProvenance = "EFFORT_ONLY; TARGET_GOAL_PACE_NOT_PERMITTED_FOR_TAPER_SHARPEN; ESTIMATED_NO_V1_PRODUCER",
            VolumeAllocationProvenance = $"{session.VolumeAllocationProvenance}; {PolicyKey} v{PolicyVersion}",
            DecisionTrace = session.DecisionTrace with
            {
                SelectedComponentPolicy = $"{PolicyKey} v{PolicyVersion}",
                SelectedPaceSource = CatalogPaceSourceSelection.EffortOnly,
                RejectedPaceSources = session.DecisionTrace.RejectedPaceSources
                    .Concat(["TARGET_GOAL_PACE_NOT_PERMITTED_FOR_TAPER_SHARPEN", "ESTIMATED_NO_V1_PRODUCER"])
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                DistanceAccountingMode = CatalogDistanceAccountingMode.ExactSessionTotal.ToString(),
                DurationSemantics = CatalogDurationKind.Unresolved.ToString(),
                UnresolvedFields = ["NUMERIC_PACE", "DURATION"]
            },
            Prescription = session.Prescription with
            {
                PrescriptionMode = CatalogPrescriptionMode.Distance,
                DistanceAccountingMode = CatalogDistanceAccountingMode.ExactSessionTotal,
                DistancePrescription = new CatalogDistancePrescription(session.PlannedDistanceKm, CatalogDistanceAccountingMode.ExactSessionTotal.ToString(), "nearest_0.5km"),
                DurationPrescription = new CatalogDurationPrescription(CatalogDurationKind.Unresolved, null, decision.DurationRule),
                PacePrescription = EffortOnly("EASY_WITH_CONTROLLED_SHARPENING", decision.PaceAndEffortRule),
                EffortGuidance = "EASY_WITH_CONTROLLED_SHARPENING",
                OrderedSegments = segments,
                Status = CatalogSessionPrescriptionStatus.FinalPrescriptionComplete
            },
            ValidationResult = validation
        };
    }

    public static bool IsTaperSharpen(CatalogPrescribedSession session) =>
        session.PhaseKey == "TAPER" &&
        session.ProgressionStageKey == "TAPER_SHARPEN" &&
        session.StructuralRole == "KEY_SESSION" &&
        session.WorkoutDefinitionKey == "EASY_STANDARD";

    internal static TaperSharpenComponentSelection SelectComponents(double sessionDistanceKm)
    {
        if (sessionDistanceKm < MinSessionDistanceKm)
        {
            throw new CatalogTaperSharpenPrescriptionInfeasibleException(
                $"TAPER_SHARPEN requires at least {MinSessionDistanceKm}km; assigned session distance was {sessionDistanceKm}km.");
        }

        var sharpening = Math.Clamp(Round(sessionDistanceKm * 0.20d), MinSharpeningDistanceKm, MaxSharpeningDistanceKm);
        var recovery = RecoveryDistanceKm;
        var easy = Round(sessionDistanceKm - sharpening - recovery);
        if (easy < MinEasyBaselineKm)
        {
            sharpening = MinSharpeningDistanceKm;
            recovery = RecoveryDistanceKm;
            easy = Round(sessionDistanceKm - sharpening - recovery);
        }

        if (easy < MinEasyBaselineKm)
        {
            throw new CatalogTaperSharpenPrescriptionInfeasibleException(
                $"TAPER_SHARPEN cannot preserve {MinEasyBaselineKm}km easy baseline plus sharpening/recovery inside {sessionDistanceKm}km.");
        }

        return new TaperSharpenComponentSelection(
            "EASY_BASELINE",
            "CONTROLLED_SHARPENING",
            "EASY_RECOVERY",
            easy,
            sharpening,
            recovery,
            "after_easy_baseline_before_easy_recovery",
            "20% of assigned taper key-session distance, rounded to 0.5km, clamped to 0.5-1.5km; recovery fixed at 0.5km; easy baseline receives the remainder.");
    }

    internal static TaperSharpenPrescriptionDecision DecisionFor(TaperSharpenComponentSelection selection) => new(
        PolicyKey,
        PolicyVersion,
        AssessCapability(),
        selection,
        "Effort-only: EASY baseline, CONTROLLED_FAST_RELAXED sharpening, EASY_RECOVERY; numeric pace and target goal pace remain unresolved unless a future canonical source exists.",
        "ExactSessionTotal; all three runtime components count toward the already-assigned taper key-session distance.",
        "Unresolved duration because no defensible numeric pace exists for easy/recovery/sharpening effort labels.",
        "AUD-508; AUD-523..AUD-530; PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md");

    private static CatalogPacePrescription EffortOnly(string effortLabel, string trace) => new(
        CatalogPacePrescriptionKind.EffortOnly,
        null,
        null,
        null,
        CatalogPaceSourceSelection.EffortOnly,
        "NUMERIC_PACE_UNRESOLVED",
        effortLabel,
        trace);

    private static CatalogSessionPrescriptionValidationResult Validate(double distance, IReadOnlyList<CatalogPrescriptionSegment> segments)
    {
        var errors = new List<string>();
        if (segments.Select(s => s.SequenceOrder).ToArray() is not [1, 2, 3]) errors.Add("TAPER_SHARPEN_COMPONENT_ORDER_INVALID");
        if (segments.Select(s => s.ComponentType).ToArray() is not ["EASY_BASELINE", "CONTROLLED_SHARPENING", "EASY_RECOVERY"])
        {
            errors.Add("TAPER_SHARPEN_COMPONENT_TYPES_INVALID");
        }
        if (segments[0].DistanceKm < MinEasyBaselineKm) errors.Add("TAPER_SHARPEN_EASY_BASELINE_TOO_SMALL");
        if (segments[1].DistanceKm is < MinSharpeningDistanceKm or > MaxSharpeningDistanceKm) errors.Add("TAPER_SHARPEN_DOSE_OUT_OF_BOUNDS");
        if (segments[2].DistanceKm != RecoveryDistanceKm) errors.Add("TAPER_SHARPEN_RECOVERY_INVALID");
        if (segments.Any(s => s.PacePrescription.Kind != CatalogPacePrescriptionKind.EffortOnly)) errors.Add("TAPER_SHARPEN_UNSUPPORTED_NUMERIC_PACE");
        if (segments.All(s => s.IntensityDescriptor == "CONTROLLED_FAST_RELAXED")) errors.Add("TAPER_SHARPEN_WHOLE_RUN_ACCELERATED");
        if (Math.Abs(Round(segments.Sum(s => s.DistanceKm)) - distance) > V1FourDaySessionVolumeAllocationPolicy.ToleranceKm) errors.Add("TAPER_SHARPEN_DISTANCE_MISMATCH");
        return new CatalogSessionPrescriptionValidationResult(errors.Count == 0, errors);
    }

    private static double Round(double value) => V1FourDaySessionVolumeAllocationPolicy.Round(value);
}
