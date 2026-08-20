using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Prescription;

internal interface ICatalogPrescriptionContextBuilder
{
    CatalogPlanPrescriptionContext Build(CatalogPrescriptionContextBuildRequest request);
}

internal sealed record CatalogPrescriptionContextBuildRequest(
    GeneratePreviewRequest PreviewRequest,
    DateOnly AsOfDate,
    PlanCatalogCandidateSummary Candidate,
    ResolverInputSnapshot ResolverInput,
    IReadOnlyList<RuntimeConditionResolutionResult> ConditionResults,
    BoundCatalogPlan BoundPlan,
    IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> WorkoutDefinitions);

internal sealed class CatalogPrescriptionContextBuilder : ICatalogPrescriptionContextBuilder
{
    public CatalogPlanPrescriptionContext Build(CatalogPrescriptionContextBuildRequest request)
    {
        var goalDistanceKm = CatalogGoalDistanceResolver.Resolve(
            request.Candidate.CanonicalDistanceFamily, request.PreviewRequest.GoalDistance, request.ResolverInput.RequestedTargetDistanceKm);

        var readiness = CatalogPrescriptionInputNormalizer.Normalize(request.PreviewRequest, request.AsOfDate);
        var weekly = SelectWeeklyVolumeAnchor(readiness, request.Candidate);
        var longRun = SelectLongRunAnchor(readiness);
        var pace = MapPaceSource(request.ConditionResults, request.PreviewRequest);
        GuardGoalFeasibilityPaceBoundary(pace);

        var input = new CatalogPrescriptionInputSnapshot
        {
            CandidateKey = request.Candidate.CandidateKey,
            CandidateVersion = request.Candidate.CandidateVersion,
            GoalType = request.PreviewRequest.GoalType,
            GoalDistance = request.PreviewRequest.GoalDistance,
            GoalDistanceKm = goalDistanceKm,
            Level = request.PreviewRequest.Level,
            DaysPerWeek = request.PreviewRequest.DaysPerWeek,
            StartDate = request.ResolverInput.StartDate ?? request.AsOfDate,
            RaceDate = request.PreviewRequest.RaceDate,
            WeeksUntilRace = request.PreviewRequest.RaceDate is { } raceDate
                ? RaceHorizonPolicy.Decide(
                    request.ResolverInput.StartDate ?? request.AsOfDate, raceDate,
                    request.Candidate.CoreCycle.MinimumWeeks, request.Candidate.CoreCycle.DefaultWeeks,
                    request.Candidate.CoreCycle.MaximumWeeks ?? int.MaxValue / 7).AvailableFullWeeks
                : null,
            TargetFinishTimeSeconds = request.PreviewRequest.TargetFinishTimeSeconds,
            Unit = request.PreviewRequest.Unit,
            PreferredPace = request.PreviewRequest.PreferredPace,
            Readiness = readiness,
            LevelModifier = request.Candidate.LevelModifier,
            ProgressionModifier = request.Candidate.ProgressionModifier,
            PeakVolumeBandPolicy = request.Candidate.PeakVolumeBandPolicy,
            RuntimeConditionValueRegistry = request.Candidate.RuntimeConditionValueRegistry,
            SourceCandidateProvenance = $"{request.Candidate.CandidateKey} v{request.Candidate.CandidateVersion}",
        };

        var inventories = BuildRuleInventories(request.WorkoutDefinitions.Values);
        var ownership = BuildOwnershipMatrix(inventories);
        var sessions = request.BoundPlan.Weeks
            .SelectMany(w => w.Sessions)
            .Select(s => BuildSessionContext(s, request.WorkoutDefinitions, weekly, longRun, pace))
            .OrderBy(s => s.WeekNumber)
            .ThenBy(s => s.Date)
            .ToList();

        var weeks = sessions
            .GroupBy(s => s.WeekNumber)
            .OrderBy(g => g.Key)
            .Select(g => (IReadOnlyList<CatalogSessionPrescriptionContext>)g.ToList())
            .ToList();

        var trace = BuildTrace(input, weekly, longRun, pace, ownership);
        var validation = CatalogPrescriptionContextValidator.Validate(request.BoundPlan, sessions, input, ownership);

        return new CatalogPlanPrescriptionContext
        {
            InputSnapshot = input,
            BoundPlanIdentity = $"{request.BoundPlan.CandidateKey} v{request.BoundPlan.CandidateVersion} / {request.BoundPlan.BinderVersion}",
            PlanLevelReadiness = readiness,
            WeeklyVolumeAnchor = weekly,
            LongRunAnchor = longRun,
            PaceSource = pace,
            WeekContexts = weeks,
            SessionContexts = sessions,
            CatalogRuleInventory = inventories,
            OwnershipMatrix = ownership,
            DecisionTrace = trace,
            ValidationResult = validation,
            TaperSharpenCapability = ClassifyTaperSharpenCapability(request.WorkoutDefinitions),
        };
    }

    private static CatalogSessionPrescriptionContext BuildSessionContext(
        BoundCatalogSession session,
        IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> definitions,
        WeeklyVolumeAnchorDecision weekly,
        LongRunAnchorDecision longRun,
        ResolvedPrescriptionPaceSource pace)
    {
        var definition = definitions[session.WorkoutDefinitionKey];
        return new CatalogSessionPrescriptionContext
        {
            WeekNumber = session.WeekNumber,
            Date = session.Date,
            PhaseKey = session.PhaseKey,
            ProgressionStageKey = session.ProgressionStageKey,
            StructuralRole = session.StructuralRole,
            WorkoutDefinitionKey = session.WorkoutDefinitionKey,
            WorkoutDefinitionVersion = session.WorkoutDefinitionVersion,
            PrescriptionProfileKey = session.PrescriptionProfileKey,
            PrescriptionProfileVersion = session.PrescriptionProfileVersion,
            PrimaryMeasurementBasis = MeasurementBasisFor(definition),
            AllowedPrescriptionModes = definition.AllowedPrescriptionModes,
            WeeklyVolumeAnchor = weekly,
            LongRunAnchor = definition.Family == "LONG_RUN" ? longRun : null,
            PaceSource = pace,
            GoalFeasibilityValue = pace.GoalFeasibilityValue,
            FallbackOrigin = session.FallbackOrigin,
            BindingPolicyProvenance = $"{session.BindingPolicyKey} v{session.BindingPolicyVersion}",
            SourceArtifactProvenance = $"{session.SourceArtifactKey} v{session.SourceArtifactVersion}",
        };
    }

    internal static WeeklyVolumeAnchorDecision SelectWeeklyVolumeAnchor(NormalizedRunningReadiness readiness, PlanCatalogCandidateSummary candidate)
    {
        if (readiness.WeeklyVolume.State == PrescriptionInputState.Available && readiness.WeeklyVolume.Kilometers is > 0)
        {
            return new WeeklyVolumeAnchorDecision(WeeklyVolumeAnchorSource.RecentFourWeekAverage,
                ["RecentWeeklyVolumeKm"], ["LEVEL_CONSERVATIVE_DEFAULT", "CATALOG_MINIMUM_DEFAULT"],
                "", $"{candidate.PeakVolumeBandPolicy.Key} v{candidate.PeakVolumeBandPolicy.Version}");
        }

        if (readiness.WeeklyVolume.State == PrescriptionInputState.Available && readiness.WeeklyVolume.Kilometers == 0)
        {
            return new WeeklyVolumeAnchorDecision(WeeklyVolumeAnchorSource.LevelConservativeDefault,
                ["RecentWeeklyVolumeKm=0"], ["RECENT_FOUR_WEEK_AVERAGE"],
                "explicit_zero_recent_volume", $"{candidate.PeakVolumeBandPolicy.Key} v{candidate.PeakVolumeBandPolicy.Version}");
        }

        if (readiness.WeeklyVolume.State == PrescriptionInputState.NotProvided)
        {
            return new WeeklyVolumeAnchorDecision(WeeklyVolumeAnchorSource.LevelConservativeDefault,
                [], ["RECENT_FOUR_WEEK_AVERAGE"],
                "missing_recent_volume_intermediate_conservative_default", $"{candidate.PeakVolumeBandPolicy.Key} v{candidate.PeakVolumeBandPolicy.Version}");
        }

        return new WeeklyVolumeAnchorDecision(WeeklyVolumeAnchorSource.Unresolved, [], ["RECENT_FOUR_WEEK_AVERAGE"], "weekly_volume_input_invalid_or_inconsistent",
            $"{candidate.PeakVolumeBandPolicy.Key} v{candidate.PeakVolumeBandPolicy.Version}");
    }

    internal static LongRunAnchorDecision SelectLongRunAnchor(NormalizedRunningReadiness readiness)
    {
        if (readiness.LongestRun.State == PrescriptionInputState.Available && readiness.LongestRun.Kilometers is > 0)
        {
            return new LongRunAnchorDecision(LongRunAnchorSource.Recent30DayLongestRun, ["RecentLongestRunKm"], [],
                "compatible_with_recent_weekly_volume_and_catalog_long_run_bounds_later");
        }

        if (readiness.WeeklyVolume.State == PrescriptionInputState.Available)
        {
            return new LongRunAnchorDecision(LongRunAnchorSource.WeeklyVolumeDerived, ["RecentWeeklyVolumeKm"], ["RECENT_30_DAY_LONGEST_RUN"],
                "must_check_recent_longest_run_weekly_volume_and_total_allocation_later");
        }

        return new LongRunAnchorDecision(LongRunAnchorSource.LevelConservativeDefault, [], ["RECENT_30_DAY_LONGEST_RUN", "WEEKLY_VOLUME_DERIVED"],
            "missing_recent_long_run_and_volume");
    }

    internal static ResolvedPrescriptionPaceSource MapPaceSource(IReadOnlyList<RuntimeConditionResolutionResult> results, GeneratePreviewRequest request)
    {
        var pace = results.FirstOrDefault(r => r.ConditionType == PaceSourceResolver.ConditionTypeValue);
        var feasibility = results.FirstOrDefault(r => r.ConditionType == GoalFeasibilityResolver.ConditionTypeValue);
        var feasibilityValue = feasibility?.OutputValue ?? "NOT_EVALUATED";

        if (pace is null)
        {
            return new ResolvedPrescriptionPaceSource(PrescriptionPaceSource.Unresolved, "PACE_SOURCE_IN",
                RuntimeConditionResolutionStatus.NotEvaluated, null, feasibilityValue, "missing_pace_source_result");
        }

        var mapped = pace.OutputValue switch
        {
            "RECENT_RACE" => PrescriptionPaceSource.RecentRace,
            "TARGET_TIME" when feasibilityValue is "REALISTIC" or "CHALLENGING" => PrescriptionPaceSource.TargetGoal,
            "TARGET_TIME" => PrescriptionPaceSource.Unresolved,
            "ESTIMATED" => PrescriptionPaceSource.EstimatedCurrentFitness,
            "NONE" when request.PreferredPace is not null => PrescriptionPaceSource.PreferredPace,
            "NONE" => PrescriptionPaceSource.EffortOnly,
            _ => PrescriptionPaceSource.Unresolved
        };

        return new ResolvedPrescriptionPaceSource(mapped, pace.ConditionType, pace.Status, pace.OutputValue, feasibilityValue,
            mapped == PrescriptionPaceSource.Unresolved ? "pace_source_not_prescription_usable" : pace.ReasonCode);
    }

    private static void GuardGoalFeasibilityPaceBoundary(ResolvedPrescriptionPaceSource pace)
    {
        if (pace.GoalFeasibilityValue == "UNSUPPORTED" && pace.Source == PrescriptionPaceSource.TargetGoal)
        {
            throw new CatalogPrescriptionContractException(
                "INFEASIBLE_GOAL_CANNOT_USE_TARGET_GOAL_PACE",
                "Goal feasibility is ineligible/unsupported, so target goal pace cannot be the active prescription pace source.");
        }
    }

    internal static IReadOnlyList<WorkoutPrescriptionRuleInventory> BuildRuleInventories(IEnumerable<CatalogWorkoutDefinitionSummary> definitions) =>
        definitions.OrderBy(d => d.Key).Select(d => new WorkoutPrescriptionRuleInventory(
            d.Key,
            d.Version,
            d.Family,
            CatalogFixedFieldsFor(d),
            ["allowedPrescriptionModes", "allowedDistanceAccountingModes", "components"],
            d.Components.Count == 0 ? ["components", "finalDistance", "duration", "paceRange", "repetitions", "recovery"] : ["finalDistance", "duration", "paceRange", "repetitions", "recovery"],
            ["paceSource", "recentWeeklyVolume", "recentLongestRun", "goalFeasibility"],
            ["phaseKey", "progressionStageKey", "structuralRole"],
            ["weeklyVolumeAnchor", "longRunAnchor"],
            ["prescriptionPaceSource"],
            FutureDecisionFieldsFor(d),
            MeasurementBasisFor(d))).ToList();

    private static IReadOnlyList<string> CatalogFixedFieldsFor(CatalogWorkoutDefinitionSummary d) =>
        ["family", "eligiblePhases", $"allowedPrescriptionModes=[{string.Join(",", d.AllowedPrescriptionModes)}]"];

    private static IReadOnlyList<string> FutureDecisionFieldsFor(CatalogWorkoutDefinitionSummary d) => d.Key switch
    {
        "EASY_STANDARD" => ["stage-specific TAPER_SHARPEN dose form"],
        "LONG_RUN_STANDARD" => ["long-run progression dosage"],
        "FARTLEK" => ["work/recovery durations and repetition count"],
        "THRESHOLD_TEMPO" => ["continuous tempo duration/dose"],
        "GOAL_PACE_TEN_K" => ["goal-pace dose after feasibility gate"],
        _ => []
    };

    internal static WorkoutMeasurementBasis MeasurementBasisFor(CatalogWorkoutDefinitionSummary d)
    {
        if (d.AllowedPrescriptionModes.Contains("DISTANCE"))
        {
            return WorkoutMeasurementBasis.DistanceFirst;
        }

        if (d.AllowedPrescriptionModes.Contains("PACE_BASED"))
        {
            return WorkoutMeasurementBasis.CatalogModeSelected;
        }

        if (d.Components.Count > 0)
        {
            return WorkoutMeasurementBasis.ComponentFirst;
        }

        return WorkoutMeasurementBasis.Unresolved;
    }

    internal static IReadOnlyList<CatalogRuleOwnershipRecord> BuildOwnershipMatrix(IReadOnlyList<WorkoutPrescriptionRuleInventory> inventories)
    {
        var rows = new List<CatalogRuleOwnershipRecord>();
        foreach (var inventory in inventories)
        {
            rows.Add(new(inventory.WorkoutKey, "workout identity", $"{inventory.WorkoutKey} v{inventory.WorkoutVersion}", PrescriptionRuleOwnership.CatalogFixed, "bound plan", "4F.6B"));
            rows.Add(new(inventory.WorkoutKey, "allowed prescription mode", string.Join(",", inventory.CatalogBoundedFields), PrescriptionRuleOwnership.CatalogBoundedRuntimeSelected, "catalog workout definition", "4F.7C"));
            rows.Add(new(inventory.WorkoutKey, "final dose", "absent", PrescriptionRuleOwnership.RuntimeUserDerived, "readiness + catalog bounds", "4F.7B/4F.7C"));
            rows.Add(new(inventory.WorkoutKey, "display duration", "absent", PrescriptionRuleOwnership.DerivedDisplayValue, "future dose + pace", "4F.8"));
        }

        return rows;
    }

    private static TaperSharpenCapability ClassifyTaperSharpenCapability(IReadOnlyDictionary<string, CatalogWorkoutDefinitionSummary> definitions)
    {
        var easy = definitions["EASY_STANDARD"];
        return easy.AllowedPrescriptionModes.Contains("DISTANCE") && easy.Components.Count == 0
            ? TaperSharpenCapability.ContractExtensionRequired
            : TaperSharpenCapability.Inconsistent;
    }

    private static IReadOnlyList<PrescriptionTraceRecord> BuildTrace(
        CatalogPrescriptionInputSnapshot input,
        WeeklyVolumeAnchorDecision weekly,
        LongRunAnchorDecision longRun,
        ResolvedPrescriptionPaceSource pace,
        IReadOnlyList<CatalogRuleOwnershipRecord> ownership) =>
        [
            new("GoalDistanceKm", input.GoalDistance.ToString(), input.GoalDistanceKm.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), PrescriptionInputState.Available.ToString(), "CatalogGoalDistanceResolver", "typed TEN_K mapping with request/catalog consistency check"),
            new("WeeklyVolumeAnchor", string.Join(",", weekly.AvailableInputs), weekly.Source.ToString(), input.Readiness.WeeklyVolume.State.ToString(), "CatalogPrescriptionContextBuilder", weekly.DefaultReason),
            new("LongRunAnchor", string.Join(",", longRun.AvailableInputs), longRun.Source.ToString(), input.Readiness.LongestRun.State.ToString(), "CatalogPrescriptionContextBuilder", longRun.ConstraintReference),
            new("PaceSource", pace.RawOutputValue ?? "null", pace.Source.ToString(), pace.RawStatus.ToString(), pace.RawConditionType, pace.Reason),
            new("OwnershipRows", ownership.Count.ToString(), ownership.Count.ToString(), PrescriptionInputState.Available.ToString(), "CatalogRuleOwnershipRecord", "catalog/runtime boundary complete")
        ];
}

internal static class CatalogGoalDistanceResolver
{
    public const double TenKDistanceKm = 10d;

    public static double Resolve(string catalogDistanceFamily, GoalDistance requestGoalDistance, double? requestedTargetDistanceKm = null)
    {
        var catalogKm = catalogDistanceFamily switch
        {
            "TEN_K" => TenKDistanceKm,
            _ => throw new CatalogPrescriptionContractException("UNSUPPORTED_CATALOG_GOAL_DISTANCE", $"Unsupported catalog distance family '{catalogDistanceFamily}'.")
        };

        var requestKm = requestGoalDistance switch
        {
            GoalDistance.TenK => TenKDistanceKm,
            _ => throw new CatalogPrescriptionContractException("UNSUPPORTED_REQUEST_GOAL_DISTANCE", $"Unsupported request goal distance '{requestGoalDistance}' for V1 pilot.")
        };

        if (Math.Abs(catalogKm - requestKm) > 0.001)
        {
            throw new CatalogPrescriptionContractException("GOAL_DISTANCE_REQUEST_CATALOG_MISMATCH", "Request goal distance disagrees with catalog candidate distance.");
        }

        if (requestedTargetDistanceKm is { } requested && Math.Abs(catalogKm - requested) > 0.001)
        {
            throw new CatalogPrescriptionContractException("GOAL_DISTANCE_REQUEST_CATALOG_MISMATCH", "Resolved request target distance disagrees with catalog candidate distance.");
        }

        return catalogKm;
    }
}

internal static class CatalogPrescriptionInputNormalizer
{
    public static NormalizedRunningReadiness Normalize(GeneratePreviewRequest request, DateOnly asOfDate)
    {
        var weekly = NormalizeDistance(request.RecentWeeklyVolumeKm, "RecentWeeklyVolumeKm", request.Unit.ToString(), 28);
        var longest = NormalizeDistance(request.RecentLongestRunKm, "RecentLongestRunKm", request.Unit.ToString(), 30);
        var issues = new List<string>();

        if (weekly.State == PrescriptionInputState.Available && longest.State == PrescriptionInputState.Available &&
            weekly.Kilometers is { } w && longest.Kilometers is { } l && l > w)
        {
            longest = longest with
            {
                State = PrescriptionInputState.Inconsistent,
                Issues = longest.Issues.Concat(["LONGEST_RUN_EXCEEDS_WEEKLY_AVERAGE_LOW_CONFIDENCE"]).ToList()
            };
            issues.Add("LONGEST_RUN_EXCEEDS_WEEKLY_AVERAGE_LOW_CONFIDENCE");
        }

        var runsState = request.RecentRunsPerWeek switch
        {
            null => PrescriptionInputState.NotProvided,
            < 0 => PrescriptionInputState.Invalid,
            > 14 => PrescriptionInputState.Invalid,
            _ => PrescriptionInputState.Available
        };

        var race = NormalizeRecentRace(request, asOfDate);
        issues.AddRange(weekly.Issues);
        issues.AddRange(longest.Issues);
        issues.AddRange(race.Issues);

        return new NormalizedRunningReadiness(weekly, longest, runsState, request.RecentRunsPerWeek, race, issues);
    }

    private static NormalizedDistanceInput NormalizeDistance(double? value, string field, string unit, int windowDays)
    {
        if (value is null)
        {
            return new(PrescriptionInputState.NotProvided, null, field, unit, null, windowDays, []);
        }

        if (value < 0)
        {
            return new(PrescriptionInputState.Invalid, null, field, unit, value, windowDays, [$"{field}_NEGATIVE"]);
        }

        return new(PrescriptionInputState.Available, value.Value, field, unit, value, windowDays, []);
    }

    private static NormalizedRecentRaceInput NormalizeRecentRace(GeneratePreviewRequest request, DateOnly asOfDate)
    {
        var recentRace = request.RecentRace;
        if (recentRace is null)
        {
            return new(PrescriptionInputState.NotProvided, null, null, null, "NOT_APPLICABLE", []);
        }

        var recentRaceDistanceKm = GoalDistanceKm.Resolve(recentRace.Distance);
        var recentRaceFinishTimeSeconds = recentRace.FinishTimeSeconds;
        var recentRaceDate = recentRace.RaceDate;

        var issues = new List<string>();

        if (recentRaceDistanceKm <= 0)
        {
            issues.Add("RECENT_RACE_DISTANCE_INVALID");
        }

        if (recentRaceFinishTimeSeconds <= 0)
        {
            issues.Add("RECENT_RACE_FINISH_TIME_INVALID");
        }

        if (recentRaceDate > asOfDate)
        {
            issues.Add("RECENT_RACE_DATE_IN_FUTURE");
        }

        var state = issues.Count == 0 ? PrescriptionInputState.Available : PrescriptionInputState.Invalid;
        return new(state, recentRaceDistanceKm, recentRaceFinishTimeSeconds, recentRaceDate,
            "RECENCY_POLICY_REFERENCE_EXISTS_IN_PACE_SOURCE_TRACE_NO_NEW_THRESHOLD", issues);
    }
}

internal static class CatalogPrescriptionContextValidator
{
    public static CatalogPrescriptionValidationResult Validate(
        BoundCatalogPlan boundPlan,
        IReadOnlyList<CatalogSessionPrescriptionContext> sessions,
        CatalogPrescriptionInputSnapshot input,
        IReadOnlyList<CatalogRuleOwnershipRecord> ownership)
    {
        var errors = new List<string>();
        var boundSessionCount = boundPlan.Weeks.Sum(w => w.Sessions.Count);
        if (boundSessionCount != sessions.Count)
        {
            errors.Add("PRESCRIPTION_CONTEXT_SESSION_COUNT_MISMATCH");
        }

        if (input.GoalDistanceKm <= 0)
        {
            errors.Add("GOAL_DISTANCE_INVALID");
        }

        if (sessions.Any(s => s.WorkoutDefinitionKey == "EASY_STANDARD" && s.StructuralRole == "EASY_SUPPORT" && s.ProgressionStageKey is not null))
        {
            errors.Add("EASY_SUPPORT_MUST_NOT_HAVE_STAGE");
        }

        ValidateTaperCompleteness(sessions, errors);

        if (sessions.Any(s => s.GoalFeasibilityValue == "UNSUPPORTED" && s.PaceSource.Source == PrescriptionPaceSource.TargetGoal))
        {
            errors.Add("INFEASIBLE_GOAL_CANNOT_USE_TARGET_GOAL_PACE");
        }

        if (ownership.Count == 0)
        {
            errors.Add("OWNERSHIP_MATRIX_EMPTY");
        }

        return new CatalogPrescriptionValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Phase 10K-FREQ.6D.4D.5D — partitions Taper KEY_SESSION completeness validation along the
    /// same Legacy/ProfileBacked classification <see cref="RunningApp.Application.RuntimeCatalog.Prescription.Session.CatalogSessionPrescriptionPlanner"/>
    /// already uses (Split C), per the FREQ.6D.4D.5C decision
    /// (<c>TAPER_COMPLETENESS_EXISTING_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT</c>).
    ///
    /// <c>TAPER_SHARPEN</c>/<c>EASY_STANDARD</c> is the real, LEGACY_TAPER_RUNTIME_ONLY prescription
    /// identity <c>V1_TAPER_SHARPEN_PRESCRIPTION_POLICY</c> (Phase 4F.7D) defines for the pre-catalog-
    /// architecture 3D/4D/Beginner×4D pilot — it was never canonical Taper stage vocabulary, and
    /// remains exactly as strict as before for every Legacy (non-ProfileBacked) Taper KEY_SESSION
    /// instance. ProfileBacked Taper KEY_SESSION instances (e.g. the real Intermediate×5D
    /// <c>TAPER_PRIMARY_STAGE</c>/<c>TAPER_SECONDARY_STAGE</c>) are exempt from this identity check
    /// entirely — their completeness is already independently, and more strongly, proven downstream
    /// by <see cref="RunningApp.Application.RuntimeCatalog.Prescription.Session.CatalogSessionPrescriptionPlanner"/>'s
    /// fail-closed exact-execution-resolution guarantee (missing index, missing profile, wrong
    /// version, and workout-provenance mismatch are all already typed, fail-closed exceptions there
    /// — never re-implemented or duplicated here, per the FREQ.6D.4D.5D prompt's own §16).
    ///
    /// Partial prescription-profile lineage (exactly one of <see cref="CatalogSessionPrescriptionContext.PrescriptionProfileKey"/>/
    /// <see cref="CatalogSessionPrescriptionContext.PrescriptionProfileVersion"/> set) is never treated
    /// as Legacy merely to preserve old behavior — it is its own distinct, always-fail-closed
    /// condition, kept separate from <c>TAPER_SHARPEN_CONTEXT_MISSING</c> so the two failure causes
    /// are never collapsed into one generic message.
    /// </summary>
    private static void ValidateTaperCompleteness(IReadOnlyList<CatalogSessionPrescriptionContext> sessions, List<string> errors)
    {
        var taperKeySessions = sessions.Where(s => s.PhaseKey == "TAPER" && s.StructuralRole == "KEY_SESSION").ToList();

        var partialLineage = taperKeySessions
            .Where(s => (s.PrescriptionProfileKey is null) != (s.PrescriptionProfileVersion is null))
            .ToList();
        if (partialLineage.Count > 0)
        {
            errors.Add("TAPER_KEY_SESSION_PARTIAL_PROFILE_LINEAGE");
        }

        var legacyTaperKeySessions = taperKeySessions
            .Where(s => s.PrescriptionProfileKey is null && s.PrescriptionProfileVersion is null)
            .ToList();
        var profileBackedTaperKeySessions = taperKeySessions
            .Where(s => s.PrescriptionProfileKey is not null && s.PrescriptionProfileVersion is not null)
            .ToList();

        bool IsLegacySharpenIdentity(CatalogSessionPrescriptionContext s) =>
            s.ProgressionStageKey == "TAPER_SHARPEN" && s.WorkoutDefinitionKey == "EASY_STANDARD";

        var everyLegacyInstanceValid = legacyTaperKeySessions.Count == 0 || legacyTaperKeySessions.All(IsLegacySharpenIdentity);
        var anyCompletenessAuthorityPresent = profileBackedTaperKeySessions.Count > 0 || legacyTaperKeySessions.Any(IsLegacySharpenIdentity);

        if (!everyLegacyInstanceValid || !anyCompletenessAuthorityPresent)
        {
            errors.Add("TAPER_SHARPEN_CONTEXT_MISSING");
        }
    }
}
