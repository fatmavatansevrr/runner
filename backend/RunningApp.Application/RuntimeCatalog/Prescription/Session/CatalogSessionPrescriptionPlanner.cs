using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Session;

internal interface ICatalogSessionPrescriptionPlanner
{
    CatalogPrescribedPlan Build(CatalogSessionPrescriptionRequest request);
}

internal sealed class CatalogSessionPrescriptionPlanner : ICatalogSessionPrescriptionPlanner
{
    private const string ComponentSelectionPolicy = "V1_COMPONENT_RANGE_SELECTION_POLICY";
    private const double ToleranceKm = V1FourDaySessionVolumeAllocationPolicy.ToleranceKm;

    public CatalogPrescribedPlan Build(CatalogSessionPrescriptionRequest request)
    {
        var weeks = new List<CatalogPrescribedWeek>();
        var sessions = new List<CatalogPrescribedSession>();
        var weeklyByNumber = request.VolumePlan.WeeklyVolumePlan.Weeks.ToDictionary(w => w.WeekNumber);
        var longRunByNumber = request.VolumePlan.LongRunProgression.Weeks.ToDictionary(w => w.WeekNumber);

        foreach (var boundWeek in request.BoundPlan.Weeks.OrderBy(w => w.WeekNumber))
        {
            var weekly = weeklyByNumber[boundWeek.WeekNumber];
            var longRun = longRunByNumber[boundWeek.WeekNumber];
            var allocation = V1FourDaySessionVolumeAllocationPolicy.Allocate(weekly, longRun, boundWeek.Sessions);
            var easyOrdinal = 0;
            var weekSessions = new List<CatalogPrescribedSession>();
            foreach (var session in boundWeek.Sessions.OrderBy(s => s.Date).ThenBy(s => s.StructuralRole))
            {
                var currentEasyOrdinal = session.StructuralRole == "EASY_SUPPORT" ? easyOrdinal++ : -1;
                weekSessions.Add(BuildSession(request, weekly, longRun, allocation, session, currentEasyOrdinal));
            }

            var accounted = Round(weekSessions.Sum(s => s.PlannedDistanceKm));
            weeks.Add(new CatalogPrescribedWeek
            {
                WeekNumber = boundWeek.WeekNumber,
                PhaseKey = boundWeek.PhaseKey,
                PlannedWeeklyVolumeKm = weekly.PlannedWeeklyVolumeKm,
                AccountedWeeklyDistanceKm = accounted,
                AllocationTrace = allocation.Trace,
                Sessions = weekSessions
            });
            sessions.AddRange(weekSessions);
        }

        var validation = CatalogPrescribedPlanValidator.Validate(request.BoundPlan, request.VolumePlan, weeks);
        var plan = new CatalogPrescribedPlan
        {
            CandidateKey = request.Candidate.CandidateKey,
            CandidateVersion = request.Candidate.CandidateVersion,
            Weeks = weeks,
            Sessions = sessions,
            ValidationResult = validation
        };

        if (!validation.IsValid)
        {
            throw new CatalogSessionPrescriptionInvalidException(string.Join(", ", validation.Errors));
        }

        return plan;
    }

    private static CatalogPrescribedSession BuildSession(
        CatalogSessionPrescriptionRequest request,
        Volume.CatalogWeeklyVolumeWeek weekly,
        Volume.CatalogLongRunWeek longRun,
        V1FourDayWeekAllocation allocation,
        BoundCatalogSession session,
        int easySupportOrdinal)
    {
        var distance = DistanceFor(session, allocation, easySupportOrdinal);
        var definition = request.WorkoutDefinitions[session.WorkoutDefinitionKey];
        var mode = ModeFor(definition);
        var accountingMode = AccountingModeFor(definition);
        var pace = PaceFor(session, request.PrescriptionContext, definition);
        var segments = SegmentsFor(definition, distance, pace);
        var duration = DurationFor(distance, pace, segments);
        var status = session.PhaseKey == "TAPER" &&
                     session.ProgressionStageKey == "TAPER_SHARPEN" &&
                     session.StructuralRole == "KEY_SESSION" &&
                     session.WorkoutDefinitionKey == "EASY_STANDARD"
            ? CatalogSessionPrescriptionStatus.BaselinePrescribedSharpeningPending
            : CatalogSessionPrescriptionStatus.Complete;
        var validation = ValidateSession(definition, distance, segments, status);
        var rawPace = request.PrescriptionContext.PaceSource.RawOutputValue ?? request.PrescriptionContext.PaceSource.RawStatus.ToString();
        var trace = new SessionPrescriptionDecisionTrace(
            session.WeekNumber,
            session.Date,
            session.WorkoutDefinitionKey,
            weekly.PlannedWeeklyVolumeKm,
            longRun.PlannedLongRunDistanceKm,
            allocation.ResidualVolumeKm,
            distance,
            $"{V1FourDaySessionVolumeAllocationPolicy.PolicyKey} v{V1FourDaySessionVolumeAllocationPolicy.PolicyVersion}",
            mode.ToString(),
            ComponentSelectionPolicy,
            rawPace,
            request.PrescriptionContext.PaceSource.GoalFeasibilityValue,
            pace.Source,
            RejectedPaceSources(session, request.PrescriptionContext, definition),
            "round_nearest_0.5km_then_adjust_second_easy_support",
            accountingMode.ToString(),
            duration.Kind.ToString(),
            session.FallbackOrigin,
            pace.Kind == CatalogPacePrescriptionKind.Unresolved ? ["NUMERIC_PACE"] : []);

        return new CatalogPrescribedSession
        {
            WeekNumber = session.WeekNumber,
            Date = session.Date,
            PhaseKey = session.PhaseKey,
            ProgressionStageKey = session.ProgressionStageKey,
            StructuralRole = session.StructuralRole,
            WorkoutDefinitionKey = session.WorkoutDefinitionKey,
            WorkoutDefinitionVersion = session.WorkoutDefinitionVersion,
            PlannedDistanceKm = distance,
            PlannedDuration = duration.Kind is CatalogDurationKind.Prescribed or CatalogDurationKind.DerivedFromSegments ? duration.Duration : null,
            EstimatedDuration = duration.Kind == CatalogDurationKind.EstimatedFromPace ? duration.Duration : null,
            Prescription = new CatalogWorkoutPrescription
            {
                PrescriptionMode = mode,
                DistanceAccountingMode = accountingMode,
                DistancePrescription = new CatalogDistancePrescription(distance, accountingMode.ToString(), "nearest_0.5km"),
                DurationPrescription = duration,
                PacePrescription = pace,
                EffortGuidance = EffortFor(definition, session),
                OrderedSegments = segments,
                Status = status
            },
            BindingProvenance = $"{session.BindingPolicyKey} v{session.BindingPolicyVersion}; {session.SourceArtifactKey} v{session.SourceArtifactVersion}",
            PaceSourceProvenance = trace.RawPaceSource,
            VolumeAllocationProvenance = trace.AllocationPolicy,
            FallbackProvenance = session.FallbackOrigin,
            DecisionTrace = trace,
            ValidationResult = validation
        };
    }

    private static double DistanceFor(BoundCatalogSession session, V1FourDayWeekAllocation allocation, int easySupportOrdinal) =>
        session.StructuralRole switch
        {
            "LONG_RUN" => allocation.LongRunDistanceKm,
            "KEY_SESSION" => allocation.KeySessionDistanceKm,
            "EASY_SUPPORT" => easySupportOrdinal == 0 ? allocation.FirstEasySupportDistanceKm : allocation.SecondEasySupportDistanceKm,
            _ => throw new CatalogSessionPrescriptionInfeasibleException($"Unsupported structural role '{session.StructuralRole}'.")
        };

    private static CatalogPrescriptionMode ModeFor(CatalogWorkoutDefinitionSummary definition)
    {
        if (definition.AllowedPrescriptionModes.Contains("DISTANCE")) return CatalogPrescriptionMode.Distance;
        if (definition.AllowedPrescriptionModes.Contains("MIXED")) return CatalogPrescriptionMode.Mixed;
        if (definition.AllowedPrescriptionModes.Contains("PACE_BASED")) return CatalogPrescriptionMode.PaceBased;
        throw new CatalogSessionPrescriptionInfeasibleException($"Workout '{definition.Key}' has no supported prescription mode.");
    }

    private static CatalogDistanceAccountingMode AccountingModeFor(CatalogWorkoutDefinitionSummary definition)
    {
        if (definition.AllowedDistanceAccountingModes.Contains("EXACT_SESSION_TOTAL")) return CatalogDistanceAccountingMode.ExactSessionTotal;
        if (definition.AllowedDistanceAccountingModes.Contains("ESTIMATED_SESSION_TOTAL")) return CatalogDistanceAccountingMode.EstimatedSessionTotal;
        return CatalogDistanceAccountingMode.PrescribedTotal;
    }

    private static CatalogPacePrescription PaceFor(
        BoundCatalogSession session,
        CatalogPlanPrescriptionContext context,
        CatalogWorkoutDefinitionSummary definition)
    {
        var feasibility = context.PaceSource.GoalFeasibilityValue;
        if (session.WorkoutDefinitionKey == "GOAL_PACE_TEN_K")
        {
            if (feasibility is not ("REALISTIC" or "CHALLENGING"))
            {
                throw new CatalogGoalPacePrescriptionUnsupportedException("GOAL_PACE_TEN_K requires REALISTIC or CHALLENGING goal feasibility.");
            }

            if (context.InputSnapshot.TargetFinishTimeSeconds is not { } targetSeconds)
            {
                throw new CatalogGoalPacePrescriptionUnsupportedException("GOAL_PACE_TEN_K requires TargetFinishTimeSeconds.");
            }

            var secondsPerKm = Math.Round(targetSeconds / context.InputSnapshot.GoalDistanceKm, 2, MidpointRounding.AwayFromZero);
            return new CatalogPacePrescription(
                CatalogPacePrescriptionKind.ExactPace,
                secondsPerKm,
                null,
                null,
                CatalogPaceSourceSelection.TargetGoalDerived,
                "GOAL_FEASIBILITY_" + feasibility,
                "GOAL_PACE",
                "TargetFinishTimeSeconds / GoalDistanceKm");
        }

        return new CatalogPacePrescription(
            CatalogPacePrescriptionKind.EffortOnly,
            null,
            null,
            null,
            definition.Key switch
            {
                "EASY_STANDARD" or "LONG_RUN_STANDARD" => CatalogPaceSourceSelection.EffortOnly,
                _ => CatalogPaceSourceSelection.Unresolved
            },
            "NUMERIC_PACE_UNRESOLVED",
            EffortFor(definition, session),
            "No approved easy/long-run/fartlek/threshold pace derivation is present; PreferredPace is context only and ESTIMATED has no producer.");
    }

    private static IReadOnlyList<string> RejectedPaceSources(
        BoundCatalogSession session,
        CatalogPlanPrescriptionContext context,
        CatalogWorkoutDefinitionSummary definition)
    {
        var rejected = new List<string>();
        if (session.WorkoutDefinitionKey != "GOAL_PACE_TEN_K")
        {
            rejected.Add("TARGET_GOAL_PACE_NOT_PERMITTED_FOR_" + definition.Key);
        }
        if (context.PaceSource.Source == PrescriptionPaceSource.PreferredPace)
        {
            rejected.Add("PREFERRED_PACE_CONTEXT_NOT_VERIFIED_PERFORMANCE_PACE");
        }
        rejected.Add("ESTIMATED_NO_V1_PRODUCER");
        return rejected;
    }

    private static IReadOnlyList<CatalogPrescriptionSegment> SegmentsFor(
        CatalogWorkoutDefinitionSummary definition,
        double distance,
        CatalogPacePrescription pace)
    {
        if (definition.Components.Count == 0)
        {
            return [new CatalogPrescriptionSegment(1, "SESSION_TOTAL", definition.Family, distance, null, pace, true)];
        }

        var ordered = definition.Components.OrderBy(c => c.SequenceOrder).ToList();
        var remaining = distance;
        var segments = new List<CatalogPrescriptionSegment>();
        foreach (var component in ordered)
        {
            double componentDistance;
            if (component.SequenceOrder == ordered[^1].SequenceOrder)
            {
                componentDistance = remaining;
            }
            else if (component.ComponentType is "WARM_UP" or "COOL_DOWN")
            {
                componentDistance = Math.Min(1.5d, Math.Max(1d, Round(distance * 0.20d)));
            }
            else if (component.ComponentType == "RECOVERY")
            {
                componentDistance = Math.Max(0.5d, Round(distance * 0.10d));
            }
            else
            {
                componentDistance = Math.Max(1d, Round(distance * 0.50d));
            }

            componentDistance = Math.Min(componentDistance, remaining);
            remaining = Round(remaining - componentDistance);
            segments.Add(new CatalogPrescriptionSegment(
                component.SequenceOrder,
                component.ComponentType,
                component.IntensityDescriptor,
                componentDistance,
                null,
                pace,
                true));
        }

        var delta = Round(distance - segments.Sum(s => s.DistanceKm));
        if (Math.Abs(delta) > ToleranceKm)
        {
            var last = segments[^1];
            segments[^1] = last with { DistanceKm = Round(last.DistanceKm + delta) };
        }

        return segments;
    }

    private static CatalogDurationPrescription DurationFor(
        double distance,
        CatalogPacePrescription pace,
        IReadOnlyList<CatalogPrescriptionSegment> segments)
    {
        if (pace.Kind == CatalogPacePrescriptionKind.ExactPace && pace.SecondsPerKilometer is { } secondsPerKm)
        {
            return new CatalogDurationPrescription(
                CatalogDurationKind.EstimatedFromPace,
                TimeSpan.FromSeconds(Math.Round(distance * secondsPerKm, 0, MidpointRounding.AwayFromZero)),
                "distance_times_exact_goal_pace_estimate");
        }

        if (segments.All(s => s.Duration is not null))
        {
            return new CatalogDurationPrescription(CatalogDurationKind.DerivedFromSegments, TimeSpan.FromSeconds(segments.Sum(s => s.Duration!.Value.TotalSeconds)), "sum_segment_durations");
        }

        return new CatalogDurationPrescription(CatalogDurationKind.Unresolved, null, "effort_only_no_defensible_numeric_pace");
    }

    private static string EffortFor(CatalogWorkoutDefinitionSummary definition, BoundCatalogSession session) =>
        session.ProgressionStageKey == "TAPER_SHARPEN"
            ? "EASY_BASELINE_SHARPENING_PENDING"
            : definition.Key switch
            {
                "LONG_RUN_STANDARD" => "LONG_RUN_EASY_CONTROLLED",
                "FARTLEK" => "SURGE_AND_FLOAT",
                "THRESHOLD_TEMPO" => "THRESHOLD_EFFORT",
                "GOAL_PACE_TEN_K" => "GOAL_PACE",
                _ => "EASY"
            };

    private static CatalogSessionPrescriptionValidationResult ValidateSession(
        CatalogWorkoutDefinitionSummary definition,
        double distance,
        IReadOnlyList<CatalogPrescriptionSegment> segments,
        CatalogSessionPrescriptionStatus status)
    {
        var errors = new List<string>();
        if (distance <= 0) errors.Add("SESSION_DISTANCE_NON_POSITIVE");
        if (segments.Count == 0) errors.Add("NO_SEGMENTS");
        if (segments.Any(s => s.DistanceKm < 0)) errors.Add("NEGATIVE_SEGMENT_DISTANCE");
        if (Math.Abs(Round(segments.Where(s => s.CountsTowardSessionDistance).Sum(s => s.DistanceKm)) - distance) > ToleranceKm)
        {
            errors.Add("SEGMENT_DISTANCE_ACCOUNTING_MISMATCH");
        }
        if (definition.Components.Count > 0 && !definition.Components.OrderBy(c => c.SequenceOrder).Select(c => c.ComponentType).SequenceEqual(segments.Select(s => s.ComponentType)))
        {
            errors.Add("COMPONENT_ORDER_MISMATCH");
        }
        if (status == CatalogSessionPrescriptionStatus.BaselinePrescribedSharpeningPending && definition.Key != "EASY_STANDARD")
        {
            errors.Add("SHARPENING_PENDING_ONLY_ALLOWED_FOR_EASY_STANDARD");
        }
        return new CatalogSessionPrescriptionValidationResult(errors.Count == 0, errors);
    }

    internal static double Round(double value) => V1FourDaySessionVolumeAllocationPolicy.Round(value);
}
