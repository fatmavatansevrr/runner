using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;

/// <summary>
/// Dates an already-valid numeric runway and an existing 12-week Core
/// skeleton through the unmodified Phase 4F.5 materializer, then composes
/// internal segment/global views. No public or persistence call site exists.
/// </summary>
internal sealed class PreparationRunwayCalendarComposer
{
    private readonly ICatalogWeekSkeletonCalendarMaterializer _calendarMaterializer;
    private readonly IDatedGeneratedCatalogPlanSkeletonValidator _datedValidator;

    public PreparationRunwayCalendarComposer(
        ICatalogWeekSkeletonCalendarMaterializer calendarMaterializer,
        IDatedGeneratedCatalogPlanSkeletonValidator datedValidator)
    {
        _calendarMaterializer = calendarMaterializer;
        _datedValidator = datedValidator;
    }

    public PreparationRunwayCalendarCompositionResult<TKey> Compose<TKey>(
        PreparationRunwayCalendarCompositionRequest<TKey> request) where TKey : notnull
    {
        var trace = new List<string>();
        var invalid = ValidateRequest(request, trace);
        if (invalid is not null) return Fail<TKey>(invalid.Value.Code, invalid.Value.Reason, trace);

        try
        {
            var authority = request.DateAuthority;
            var runwaySkeleton = PreparationRunwayCalendarSkeletonAdapter.Adapt(request);
            trace.Add($"alignment_start={authority.HorizonContext.StartDate:yyyy-MM-dd}; leading_partial_days={authority.HorizonDecision.LeadingPartialDays}; runway_start={authority.RunwayStartDate:yyyy-MM-dd}");
            trace.Add($"core_start={authority.RunwayDateDecision.CoreStartDate:yyyy-MM-dd}; race_boundary={authority.HorizonContext.RaceDate:yyyy-MM-dd}");

            // The existing Phase 4F.5 policy owns cross-week KEY/LONG
            // separation. Date the continuous structural sequence once so
            // that its unchanged backtracking search can see the segment
            // boundary; then restore segment-local Core numbering in the
            // composition wrapper below. Standalone Core paths are untouched.
            var combinedUndated = BuildCombinedUndatedSkeleton(runwaySkeleton, request.UndatedCoreSkeleton, request);
            var combinedDated = Materialize(combinedUndated, request, authority.RunwayStartDate);
            var runwayDated = SliceDatedSkeleton(combinedDated, 0, runwaySkeleton.PlannedWeekCount, 0, runwaySkeleton.StartDate, runwaySkeleton.EndDate);
            var coreDated = SliceDatedSkeleton(combinedDated, runwaySkeleton.PlannedWeekCount,
                request.UndatedCoreSkeleton.PlannedWeekCount, runwaySkeleton.PlannedWeekCount,
                request.UndatedCoreSkeleton.StartDate, request.UndatedCoreSkeleton.EndDate);

            var finalRunwayWindowEnd = runwayDated.Weeks.OrderBy(w => w.WeekNumber).Last().EndDate;
            var firstCoreWindowStart = coreDated.Weeks.OrderBy(w => w.WeekNumber).First().StartDate;
            if (finalRunwayWindowEnd >= firstCoreWindowStart)
                return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.RunwayCoreDateOverlap,
                    "Dated runway overlaps the dated Core segment.", trace);
            if (finalRunwayWindowEnd.AddDays(1) < firstCoreWindowStart)
                return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.RunwayCoreDateGap,
                    "An unexplained gap exists between dated runway and Core segments.", trace);
            if (runwayDated.Weeks.OrderBy(w => w.WeekNumber).Select((w, i) => w.StartDate == authority.RunwayStartDate.AddDays(i * 7)).Any(valid => !valid))
                return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.RunwayWindowInvalid,
                    "A dated runway week differs from its authoritative seven-day window.", trace);
            if (coreDated.Weeks.OrderBy(w => w.WeekNumber).Select((w, i) => w.StartDate == authority.RunwayDateDecision.CoreStartDate.AddDays(i * 7)).Any(valid => !valid))
                return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.CoreStartDateMismatch,
                    "A dated Core week differs from its authoritative seven-day window.", trace);

            var runwayValidation = _datedValidator.Validate(runwayDated, request.PreferredDays, request.LongRunDayPreference!.Value);
            if (!runwayValidation.IsValid) return ValidationFailure<TKey>(runwayValidation, "runway", trace);
            var coreValidation = _datedValidator.Validate(coreDated, request.PreferredDays, request.LongRunDayPreference.Value);
            if (!coreValidation.IsValid) return ValidationFailure<TKey>(coreValidation, "core", trace);

            var runwaySegment = new PreparationRunwaySegmentProvenance(
                PreparationRunwaySegmentType.PreparationRunway, 1,
                runwayDated.StartDate, runwayDated.EndDate, 1, runwayDated.PlannedWeekCount,
                request.Policy.PolicyKey, request.Policy.PolicyVersion,
                request.CandidateKey, request.CandidateVersion, request.Profile.ToString());
            var coreSegment = new PreparationRunwaySegmentProvenance(
                PreparationRunwaySegmentType.RaceCore, 2,
                coreDated.StartDate, coreDated.EndDate, 1, coreDated.PlannedWeekCount,
                CatalogCalendarDayMaterializerVersion.V1, 1,
                request.CandidateKey, request.CandidateVersion, null);

            var datedRunwayWeeks = BuildDatedRunwayWeeks(request, runwayDated, runwaySegment);
            var datedCoreWeeks = coreDated.Weeks.OrderBy(w => w.WeekNumber)
                .Select(w => new PreparationRunwayDatedCoreWeek(
                    runwayDated.PlannedWeekCount + w.WeekNumber, w.WeekNumber, w, coreSegment))
                .ToArray();

            var combinedValidation = _datedValidator.Validate(
                combinedDated, request.PreferredDays, request.LongRunDayPreference.Value);
            if (!combinedValidation.IsValid)
                return ValidationFailure<TKey>(combinedValidation, "combined runway/Core boundary", trace);

            var combined = datedRunwayWeeks.Select(w => new PreparationRunwayComposedWeek<TKey>(
                    w.GlobalWeekNumber, w.SegmentLocalWeekNumber, PreparationRunwaySegmentType.PreparationRunway,
                    w.StartDate, w.EndDate, w, null))
                .Concat(datedCoreWeeks.Select(w => new PreparationRunwayComposedWeek<TKey>(
                    w.GlobalWeekNumber, w.SegmentLocalWeekNumber, PreparationRunwaySegmentType.RaceCore,
                    w.CoreWeek.StartDate, w.CoreWeek.EndDate, null, w)))
                .OrderBy(w => w.GlobalWeekNumber)
                .ToArray();

            if (!combined.Select(w => w.GlobalWeekNumber).SequenceEqual(Enumerable.Range(1, combined.Length)))
                return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.GlobalWeekNumberInvalid,
                    "Combined global week numbering is not contiguous from one.", trace);

            var continuity = AnalyzeContinuity(request, runwayDated, coreDated, combinedValidation);
            if (!continuity.IsValid)
                return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.RunwayCoreContinuityViolation,
                    "Runway/Core date or numeric continuity failed.", trace);

            foreach (var week in combined)
            {
                trace.Add($"global_week={week.GlobalWeekNumber}; segment={week.SegmentType}; local_week={week.SegmentLocalWeekNumber}; window={week.StartDate:yyyy-MM-dd}..{week.EndDate:yyyy-MM-dd}");
                if (week.RunwayWeek is not null)
                    foreach (var slot in week.RunwayWeek.ChronologicalSlots)
                        trace.Add($"runway_week={week.SegmentLocalWeekNumber}; slot={slot.PrescribedSlot.StructuralSlot.SlotOrdinal}; role={slot.PrescribedSlot.StructuralSlot.SlotRole}; date={slot.SessionDate:yyyy-MM-dd}");
            }

            trace.Add("combined_continuity=exact; partial_days_are_alignment_only; numeric_values_preserved=true");
            return PreparationRunwayCalendarCompositionResult<TKey>.Success(
                authority, datedRunwayWeeks, datedCoreWeeks, combined,
                [runwaySegment, coreSegment], continuity, trace);
        }
        catch (CatalogPreferredDaysRequiredException exception)
        {
            return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.PreferredDaysInvalid, exception.Message, trace);
        }
        catch (CatalogPreferredDayCountInvalidException exception)
        {
            return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.PreferredDaysInvalid, exception.Message, trace);
        }
        catch (CatalogPreferredDaysDuplicatedException exception)
        {
            return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.PreferredDaysInvalid, exception.Message, trace);
        }
        catch (CatalogLongRunDayRequiredException exception)
        {
            return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.LongRunDayNotPreferred, exception.Message, trace);
        }
        catch (CatalogLongRunDayNotPreferredException exception)
        {
            return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.LongRunDayNotPreferred, exception.Message, trace);
        }
        catch (CatalogPreferredDayConfigurationUnsafeException exception)
        {
            return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.RoleDayAssignmentInvalid, exception.Message, trace);
        }
        catch (CatalogCalendarRoleStructureInvalidException exception)
        {
            return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.RoleDayAssignmentInvalid, exception.Message, trace);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return Fail<TKey>(PreparationRunwayCalendarCompositionFailureCode.CalendarCompositionInvariantViolation, exception.Message, trace);
        }
    }

    private DatedGeneratedCatalogPlanSkeleton Materialize<TKey>(
        GeneratedCatalogPlanSkeleton skeleton,
        PreparationRunwayCalendarCompositionRequest<TKey> request,
        DateOnly startDate) where TKey : notnull =>
        _calendarMaterializer.Materialize(new CatalogCalendarAssignmentContext(
            startDate,
            Domain.Enums.GoalType.Race,
            request.PreferredDays,
            request.LongRunDayPreference,
            skeleton,
            request.Policy.AssignmentPolicy,
            new CatalogCalendarMaterializationProvenance(
                request.CandidateKey,
                request.CandidateVersion,
                request.DateAuthority.HorizonContext.StartDate,
                startDate,
                request.PreferredDays,
                request.LongRunDayPreference!.Value,
                request.Policy.CalendarMaterializerVersion,
                skeleton.SchemaVersion,
                skeleton.DependencyVersions)));

    private static (PreparationRunwayCalendarCompositionFailureCode Code, string Reason)? ValidateRequest<TKey>(
        PreparationRunwayCalendarCompositionRequest<TKey>? request,
        ICollection<string> trace) where TKey : notnull
    {
        if (request is null || request.DateAuthority is null || request.NumericRunway is null ||
            request.CoreWeekOneNumericTarget is null || request.UndatedCoreSkeleton is null || request.Policy is null)
            return (PreparationRunwayCalendarCompositionFailureCode.InvalidCalendarCompositionRequest, "All calendar-composition inputs are required.");
        if (request.Policy.PolicyKey != TenKPreparationRunwayCalendarCompositionPolicyFactory.PolicyKey ||
            request.Policy.PolicyVersion != TenKPreparationRunwayCalendarCompositionPolicyFactory.PolicyVersion)
            return (PreparationRunwayCalendarCompositionFailureCode.InvalidCalendarCompositionRequest, "Approved TEN_K calendar composition policy is required.");
        if (!PreviewRouting.V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayCandidate(request.CandidateKey, request.CandidateVersion))
            return (PreparationRunwayCalendarCompositionFailureCode.InvalidCalendarCompositionRequest, "Composer is scoped to the approved Intermediate 4D/5D Preparation Runway candidates.");

        var authority = request.DateAuthority;
        var canonical = authority.HorizonDecision;
        if (canonical.Mode != CoreHorizonMode.PreparationRunwayPlusCore ||
            canonical.MinimumCoreWeeks != authority.HorizonContext.MinimumCoreWeeks ||
            canonical.PreferredCoreWeeks != authority.HorizonContext.PreferredCoreWeeks ||
            canonical.MaximumCoreWeeks != authority.HorizonContext.MaximumCoreWeeks ||
            canonical.AvailableDays != canonical.AvailableFullWeeks * 7 + canonical.LeadingPartialDays)
            return (PreparationRunwayCalendarCompositionFailureCode.InvalidDateAuthority, "Horizon decision is not the canonical runway-plus-Core decision.");
        if (canonical.LeadingPartialDays is < 0 or > 6)
            return (PreparationRunwayCalendarCompositionFailureCode.LeadingPartialDayMismatch, "LeadingPartialDays must be in 0..6.");

        var derived = PreparationRunwayDateAuthority.Derive(
            authority.HorizonContext.StartDate, canonical.AvailableFullWeeks,
            canonical.LeadingPartialDays, canonical.PreferredCoreWeeks);
        var expectedRunwayStart = authority.HorizonContext.StartDate.AddDays(canonical.LeadingPartialDays);
        if (derived != authority.RunwayDateDecision || authority.RunwayStartDate != expectedRunwayStart)
            return (PreparationRunwayCalendarCompositionFailureCode.LeadingPartialDayMismatch, "Stored runway date derivation does not match canonical authority.");
        if (derived.CoreStartDate.AddDays(canonical.PreferredCoreWeeks * 7) != authority.HorizonContext.RaceDate)
            return (PreparationRunwayCalendarCompositionFailureCode.InvalidDateAuthority, "Core boundary does not close exactly at RaceDate.");

        if (!request.NumericRunway.IsSuccess || request.NumericRunway.PrescribedWeeks is null ||
            request.NumericRunway.ContinuityAnalysis is null || !request.NumericRunway.ContinuityAnalysis.IsWithinTolerance)
            return (PreparationRunwayCalendarCompositionFailureCode.InvalidCalendarCompositionRequest, "A successful continuity-validated numeric runway is required.");
        var runwayWeeks = request.NumericRunway.PrescribedWeeks.OrderBy(w => w.StructuralWeek.RunwayWeekNumber).ToArray();
        if (runwayWeeks.Length != derived.RunwayFullWeeks)
            return (PreparationRunwayCalendarCompositionFailureCode.RunwayWeekCountMismatch, "Numeric runway week count does not match authoritative full runway weeks.");
        if (runwayWeeks.Length is < 3 or > 8)
            return (PreparationRunwayCalendarCompositionFailureCode.RunwayWeekCountMismatch, "Pilot runway must contain 3..8 full weeks.");
        if (!string.Equals(runwayWeeks[^1].StructuralWeek.BlockType?.ToString(), "PreSpecificTransition", StringComparison.Ordinal))
            return (PreparationRunwayCalendarCompositionFailureCode.SegmentOrderInvalid, "Final runway week must be PreSpecificTransition.");
        foreach (var week in runwayWeeks)
        {
            if (!PreparationRunwayWeeklyShape.IsValid(week.OrderedSlots.Select(s => s.StructuralSlot.SlotRole).ToArray()) ||
                Math.Abs(week.OrderedSlots.Sum(s => s.PlannedDistanceKm) - week.PlannedWeeklyVolumeKm) > 0.001d ||
                Math.Abs(week.OrderedSlots.Single(s => s.StructuralSlot.SlotRole == PreparationRunwaySlotRole.LongRun).PlannedDistanceKm - week.PlannedLongRunDistanceKm) > 0.001d)
                return (PreparationRunwayCalendarCompositionFailureCode.NumericPrescriptionChanged, "Numeric runway totals or slot quantities are inconsistent.");
        }

        var target = request.CoreWeekOneNumericTarget;
        var final = runwayWeeks[^1];
        // Phase 10K-FREQ.6D.7: per FREQ.6D.6, only total weekly volume and long-run
        // distance are the Core-entry compatibility authority. Per-slot KEY/EASY
        // continuity only applies when Runway's final week and the Core target share
        // the exact same role composition (every existing Intermediate 4D case, 1
        // KEY + 2 EASY on both sides). When the approved structure legitimately
        // redistributes KEY/EASY counts across the boundary (Intermediate 5D: 1 KEY
        // + 3 EASY -> 2 KEY + 2 EASY), per-slot values are not comparable even for
        // roles that happen to share an ordinal (e.g. Runway's sole KEY carries a
        // different share of weekly volume than either of Core's two KEY sessions).
        var finalKeyCount = final.OrderedSlots.Count(s => s.StructuralSlot.SlotRole == PreparationRunwaySlotRole.KeySession);
        var finalEasyCount = final.OrderedSlots.Count(s => s.StructuralSlot.SlotRole == PreparationRunwaySlotRole.EasySupport);
        var targetKeyCount = target.OrderedSlots.Count(t => t.Role == PreparationRunwaySlotRole.KeySession);
        var targetEasyCount = target.OrderedSlots.Count(t => t.Role == PreparationRunwaySlotRole.EasySupport);
        var roleCompositionMatches = finalKeyCount == targetKeyCount && finalEasyCount == targetEasyCount;
        if (Math.Abs(final.PlannedWeeklyVolumeKm - target.WeeklyVolumeKm) > 0.001d ||
            Math.Abs(final.PlannedLongRunDistanceKm - target.LongRunDistanceKm) > 0.001d ||
            (roleCompositionMatches && target.OrderedSlots.Any(t =>
                Math.Abs(final.OrderedSlots.Single(s => s.StructuralSlot.SlotRole == t.Role && s.StructuralSlot.RoleOrdinal == t.RoleOrdinal).PlannedDistanceKm - t.DistanceKm) > 0.001d)))
            return (PreparationRunwayCalendarCompositionFailureCode.NumericPrescriptionChanged, "Final runway numeric boundary no longer equals Core Week 1 target.");

        var core = request.UndatedCoreSkeleton;
        if (core.PlannedWeekCount != canonical.PreferredCoreWeeks || core.Weeks.Count != canonical.PreferredCoreWeeks)
            return (PreparationRunwayCalendarCompositionFailureCode.CoreWeekCountMismatch, "Core skeleton must contain exactly the preferred Core week count.");
        if (core.StartDate != derived.CoreStartDate)
            return (PreparationRunwayCalendarCompositionFailureCode.CoreStartDateMismatch, "Core skeleton StartDate differs from authoritative CoreStartDate.");
        if (core.CandidateKey != request.CandidateKey || core.CandidateVersion != request.CandidateVersion)
            return (PreparationRunwayCalendarCompositionFailureCode.InvalidCalendarCompositionRequest, "Core candidate identity differs from composition identity.");
        var firstCore = core.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        if (firstCore is null || firstCore.StageKey != "FOUNDATION")
            return (PreparationRunwayCalendarCompositionFailureCode.SegmentOrderInvalid, "Core Week 1 must be FOUNDATION.");

        trace.Add($"authority={authority.SourcePolicyKey} v{authority.SourcePolicyVersion}; available_full_weeks={canonical.AvailableFullWeeks}");
        trace.Add($"preferred_days={string.Join(',', request.PreferredDays.OrderBy(d => (int)d))}; long_run_day={request.LongRunDayPreference}");
        return null;
    }

    private static IReadOnlyList<PreparationRunwayDatedWeek<TKey>> BuildDatedRunwayWeeks<TKey>(
        PreparationRunwayCalendarCompositionRequest<TKey> request,
        DatedGeneratedCatalogPlanSkeleton dated,
        PreparationRunwaySegmentProvenance segment) where TKey : notnull
    {
        var prescribed = request.NumericRunway.PrescribedWeeks!.OrderBy(w => w.StructuralWeek.RunwayWeekNumber).ToArray();
        return dated.Weeks.OrderBy(w => w.WeekNumber).Select((week, index) =>
        {
            var source = prescribed[index];
            var slots = week.SessionSlots.OrderBy(s => s.SlotOrderInWeek).Select(datedSlot =>
            {
                var numericSlot = source.OrderedSlots.Single(s => s.StructuralSlot.SlotOrdinal == datedSlot.SlotOrderInWeek);
                return new PreparationRunwayDatedSlot<TKey>(numericSlot, datedSlot.SessionDate, datedSlot.SessionDayOfWeek, datedSlot.Provenance);
            }).ToArray();
            return new PreparationRunwayDatedWeek<TKey>(
                week.WeekNumber, week.WeekNumber, week.StartDate, week.EndDate, source,
                slots, slots.OrderBy(s => s.SessionDate).ThenBy(s => s.PrescribedSlot.StructuralSlot.SlotOrdinal).ToArray(), segment);
        }).ToArray();
    }

    private static GeneratedCatalogPlanSkeleton BuildCombinedUndatedSkeleton<TKey>(
        GeneratedCatalogPlanSkeleton runway,
        GeneratedCatalogPlanSkeleton core,
        PreparationRunwayCalendarCompositionRequest<TKey> request) where TKey : notnull
    {
        var weeks = runway.Weeks.OrderBy(w => w.WeekNumber)
            .Concat(core.Weeks.OrderBy(w => w.WeekNumber).Select(w => new GeneratedCatalogWeekSkeleton
            {
                WeekNumber = runway.PlannedWeekCount + w.WeekNumber,
                StartDate = w.StartDate,
                EndDate = w.EndDate,
                StageKey = w.StageKey,
                StageWeekIndex = w.StageWeekIndex,
                StageWeekCount = w.StageWeekCount,
                SessionSlots = w.SessionSlots,
                Provenance = w.Provenance,
            })).ToArray();
        return new GeneratedCatalogPlanSkeleton
        {
            SchemaVersion = GeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            StartDate = runway.StartDate,
            EndDate = core.EndDate,
            PlannedWeekCount = weeks.Length,
            DaysPerWeek = core.DaysPerWeek,
            CanonicalDistanceFamily = core.CanonicalDistanceFamily,
            CandidateKey = request.CandidateKey,
            CandidateVersion = request.CandidateVersion,
            DependencyVersions = core.DependencyVersions,
            Weeks = weeks,
            Provenance = new GeneratedCatalogPlanSkeletonProvenance
            {
                CandidateKey = request.CandidateKey,
                CandidateVersion = request.CandidateVersion,
                DependencyVersions = core.DependencyVersions,
                AsOfDate = request.DateAuthority.HorizonContext.StartDate,
                MaterializerVersion = PreparationRunwayCalendarSkeletonAdapter.MaterializerVersion,
            },
        };
    }

    private static DatedGeneratedCatalogPlanSkeleton SliceDatedSkeleton(
        DatedGeneratedCatalogPlanSkeleton combined,
        int skip,
        int count,
        int globalOffset,
        DateOnly start,
        DateOnly end)
    {
        var weeks = combined.Weeks.OrderBy(w => w.WeekNumber).Skip(skip).Take(count)
            .Select(w => w with
            {
                WeekNumber = w.WeekNumber - globalOffset,
                Provenance = w.Provenance with
                {
                    WeekNumber = w.WeekNumber - globalOffset,
                    SourceSkeletonWeekNumber = w.WeekNumber - globalOffset,
                },
            }).ToArray();
        return new DatedGeneratedCatalogPlanSkeleton(
            combined.SchemaVersion, start, end, count, weeks,
            combined.Provenance with { StartDate = start });
    }

    private static PreparationRunwayCalendarContinuityAnalysis AnalyzeContinuity<TKey>(
        PreparationRunwayCalendarCompositionRequest<TKey> request,
        DatedGeneratedCatalogPlanSkeleton runway,
        DatedGeneratedCatalogPlanSkeleton core,
        DatedGeneratedCatalogPlanSkeletonValidationResult combinedValidation) where TKey : notnull
    {
        var finalRunwayEnd = runway.Weeks.OrderBy(w => w.WeekNumber).Last().EndDate;
        var firstCoreStart = core.Weeks.OrderBy(w => w.WeekNumber).First().StartDate;
        var windowsContiguous = finalRunwayEnd.AddDays(1) == firstCoreStart;
        var numeric = request.NumericRunway.ContinuityAnalysis!.IsWithinTolerance;
        var raceBoundary = core.EndDate.AddDays(1) == request.DateAuthority.HorizonContext.RaceDate;
        var valid = windowsContiguous && combinedValidation.IsValid && numeric && raceBoundary;
        return new PreparationRunwayCalendarContinuityAnalysis(
            request.DateAuthority.HorizonContext.StartDate,
            request.DateAuthority.HorizonDecision.LeadingPartialDays,
            runway.StartDate,
            finalRunwayEnd,
            firstCoreStart,
            core.EndDate,
            request.DateAuthority.HorizonContext.RaceDate,
            windowsContiguous,
            combinedValidation.IsValid,
            numeric,
            valid,
            "canonical horizon decision + PreparationRunwayDateAuthority + Phase 4F.5 calendar materializer/validator");
    }

    private static PreparationRunwayCalendarCompositionResult<TKey> ValidationFailure<TKey>(
        DatedGeneratedCatalogPlanSkeletonValidationResult validation,
        string segment,
        IReadOnlyList<string> trace) where TKey : notnull
    {
        var code = validation.Errors.Any(e => e == DatedGeneratedCatalogPlanSkeletonValidationError.DuplicateSessionDateWithinWeek)
            ? PreparationRunwayCalendarCompositionFailureCode.DuplicateWorkoutDate
            : validation.Errors.Any(e => e == DatedGeneratedCatalogPlanSkeletonValidationError.SessionDateOutsideOwningWeek)
                ? PreparationRunwayCalendarCompositionFailureCode.WorkoutDateOutsideWeek
                : validation.Errors.Any(e => e is DatedGeneratedCatalogPlanSkeletonValidationError.KeySessionLongRunSeparationViolated or DatedGeneratedCatalogPlanSkeletonValidationError.CrossWeekSeparationViolated)
                    ? PreparationRunwayCalendarCompositionFailureCode.RoleDayAssignmentInvalid
                    : validation.Errors.Any(e => e == DatedGeneratedCatalogPlanSkeletonValidationError.WeekNumbersNotConsecutiveFromOne)
                        ? PreparationRunwayCalendarCompositionFailureCode.GlobalWeekNumberInvalid
                    : PreparationRunwayCalendarCompositionFailureCode.CalendarCompositionInvariantViolation;
        return Fail<TKey>(code, $"Dated {segment} skeleton failed validation: {string.Join(',', validation.Errors)}", trace);
    }

    private static PreparationRunwayCalendarCompositionResult<TKey> Fail<TKey>(
        PreparationRunwayCalendarCompositionFailureCode code,
        string reason,
        IReadOnlyList<string> trace) where TKey : notnull =>
        PreparationRunwayCalendarCompositionResult<TKey>.Failure(code, reason, trace.ToArray());
}
