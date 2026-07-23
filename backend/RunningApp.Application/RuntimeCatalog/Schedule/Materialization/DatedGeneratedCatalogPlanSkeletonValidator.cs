namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>Backend Integration Phase 4F.5 — enumerates every structural way a <see cref="DatedGeneratedCatalogPlanSkeleton"/> can fail validation, mirroring the Phase 4F.2 <see cref="GeneratedCatalogPlanSkeletonValidationError"/> convention.</summary>
internal enum DatedGeneratedCatalogPlanSkeletonValidationError
{
    UnsupportedSchemaVersion,
    PlanEndDateInconsistentWithFinalWeek,
    ActualWeekCountMismatch,
    WeekNumbersNotConsecutiveFromOne,
    WeekDateRangeIncorrect,
    SessionSlotCountIncorrect,
    DuplicateSessionDateWithinWeek,
    SessionDateOutsideOwningWeek,
    SessionWeekdayDateMismatch,
    SessionWeekdayNotInPreferredDays,
    NotEveryPreferredDayUsedInWeek,
    LongRunDateNotOnLongRunDayPreference,
    RoleCountIncorrect,
    KeySessionLongRunSeparationViolated,
    CrossWeekSeparationViolated,
    ProvenanceMissing,
}

/// <summary>Result of validating a <see cref="DatedGeneratedCatalogPlanSkeleton"/>.</summary>
internal sealed class DatedGeneratedCatalogPlanSkeletonValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<DatedGeneratedCatalogPlanSkeletonValidationError> Errors { get; init; }

    public static DatedGeneratedCatalogPlanSkeletonValidationResult Valid() =>
        new() { IsValid = true, Errors = Array.Empty<DatedGeneratedCatalogPlanSkeletonValidationError>() };

    public static DatedGeneratedCatalogPlanSkeletonValidationResult Invalid(IReadOnlyList<DatedGeneratedCatalogPlanSkeletonValidationError> errors) =>
        new() { IsValid = false, Errors = errors };
}

internal interface IDatedGeneratedCatalogPlanSkeletonValidator
{
    DatedGeneratedCatalogPlanSkeletonValidationResult Validate(DatedGeneratedCatalogPlanSkeleton skeleton, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDayPreference);
}

/// <inheritdoc cref="IDatedGeneratedCatalogPlanSkeletonValidator"/>
/// <remarks>No DB, clock, HTTP, resolver, or catalog-loader dependency — pure structural validation of an already-built skeleton.</remarks>
internal sealed class DatedGeneratedCatalogPlanSkeletonValidator : IDatedGeneratedCatalogPlanSkeletonValidator
{
    private const int MinimumKeySessionToLongRunSeparationDays = 2;

    public DatedGeneratedCatalogPlanSkeletonValidationResult Validate(
        DatedGeneratedCatalogPlanSkeleton skeleton, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDayPreference)
    {
        var errors = new List<DatedGeneratedCatalogPlanSkeletonValidationError>();

        if (skeleton.SchemaVersion != DatedGeneratedCatalogPlanSkeleton.CurrentSchemaVersion)
        {
            errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.UnsupportedSchemaVersion);
        }

        if (skeleton.Weeks.Count != skeleton.PlannedWeekCount)
        {
            errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.ActualWeekCountMismatch);
        }

        var expectedEnd = skeleton.StartDate.AddDays(skeleton.PlannedWeekCount * 7 - 1);
        if (skeleton.EndDate != expectedEnd)
        {
            errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.PlanEndDateInconsistentWithFinalWeek);
        }

        var orderedWeeks = skeleton.Weeks.OrderBy(w => w.WeekNumber).ToList();
        for (var i = 0; i < orderedWeeks.Count; i++)
        {
            if (orderedWeeks[i].WeekNumber != i + 1)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.WeekNumbersNotConsecutiveFromOne);
                break;
            }
        }

        DateOnly? previousWeekLongRunDate = null;
        DateOnly? previousWeekKeySessionDate = null;

        foreach (var week in orderedWeeks)
        {
            var expectedWeekEnd = week.StartDate.AddDays(6);
            if (week.EndDate != expectedWeekEnd)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.WeekDateRangeIncorrect);
            }

            if (week.SessionSlots.Count != 4)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.SessionSlotCountIncorrect);
            }

            if (week.Provenance is null)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.ProvenanceMissing);
            }

            var dates = week.SessionSlots.Select(s => s.SessionDate).ToList();
            if (dates.Distinct().Count() != dates.Count)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.DuplicateSessionDateWithinWeek);
            }

            foreach (var slot in week.SessionSlots)
            {
                if (slot.SessionDate < week.StartDate || slot.SessionDate > week.EndDate)
                {
                    errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.SessionDateOutsideOwningWeek);
                }

                if (slot.SessionDate.DayOfWeek != slot.SessionDayOfWeek)
                {
                    errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.SessionWeekdayDateMismatch);
                }

                if (!preferredDays.Contains(slot.SessionDayOfWeek))
                {
                    errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.SessionWeekdayNotInPreferredDays);
                }

                if (slot.Provenance is null)
                {
                    errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.ProvenanceMissing);
                }
            }

            var usedWeekdaySet = week.SessionSlots.Select(s => s.SessionDayOfWeek).ToHashSet();
            var preferredDaySet = preferredDays.ToHashSet();
            if (!usedWeekdaySet.SetEquals(preferredDaySet))
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.NotEveryPreferredDayUsedInWeek);
            }

            var roleCounts = week.SessionSlots.GroupBy(s => s.StructuralRole).ToDictionary(g => g.Key, g => g.Count());
            if (roleCounts.GetValueOrDefault("KEY_SESSION") != 1 ||
                roleCounts.GetValueOrDefault("EASY_SUPPORT") != 2 ||
                roleCounts.GetValueOrDefault("LONG_RUN") != 1)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.RoleCountIncorrect);
            }

            var longRunSlot = week.SessionSlots.FirstOrDefault(s => s.StructuralRole == "LONG_RUN");
            var keySessionSlot = week.SessionSlots.FirstOrDefault(s => s.StructuralRole == "KEY_SESSION");

            if (longRunSlot is not null && longRunSlot.SessionDayOfWeek != longRunDayPreference)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.LongRunDateNotOnLongRunDayPreference);
            }

            if (longRunSlot is not null && keySessionSlot is not null)
            {
                var separation = Math.Abs(longRunSlot.SessionDate.DayNumber - keySessionSlot.SessionDate.DayNumber);
                if (separation < MinimumKeySessionToLongRunSeparationDays)
                {
                    errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.KeySessionLongRunSeparationViolated);
                }

                if (previousWeekLongRunDate is not null)
                {
                    var crossSeparationA = Math.Abs(keySessionSlot.SessionDate.DayNumber - previousWeekLongRunDate.Value.DayNumber);
                    var crossSeparationB = Math.Abs(longRunSlot.SessionDate.DayNumber - previousWeekKeySessionDate!.Value.DayNumber);
                    if (crossSeparationA < MinimumKeySessionToLongRunSeparationDays || crossSeparationB < MinimumKeySessionToLongRunSeparationDays)
                    {
                        errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.CrossWeekSeparationViolated);
                    }
                }

                previousWeekLongRunDate = longRunSlot.SessionDate;
                previousWeekKeySessionDate = keySessionSlot.SessionDate;
            }
        }

        if (skeleton.Provenance is null)
        {
            errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.ProvenanceMissing);
        }

        return errors.Count == 0
            ? DatedGeneratedCatalogPlanSkeletonValidationResult.Valid()
            : DatedGeneratedCatalogPlanSkeletonValidationResult.Invalid(errors);
    }
}
