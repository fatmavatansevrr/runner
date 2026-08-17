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
    /// <summary>Phase 10K-FREQ.4: KEY-to-KEY same-week separation, for layouts with more than one KEY_SESSION per week.</summary>
    KeySessionKeySessionSeparationViolated,
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
    /// <summary>Internal (not private) so Phase 4M.3's ScheduleRepairSpacingValidator
    /// can reuse this exact canonical threshold for live candidate spacing checks
    /// instead of duplicating the constant -- no new spacing rule/value introduced.</summary>
    internal const int MinimumKeySessionToLongRunSeparationDays = 2;

    /// <summary>
    /// Phase 10K-FREQ.4: KEY-to-KEY same-week separation, for layouts with
    /// more than one KEY_SESSION per week (e.g. a hypothetical Intermediate
    /// 5D layout). FREQ.3 §D.3 confirmed no such rule existed anywhere
    /// before this. Reuses <see cref="MinimumKeySessionToLongRunSeparationDays"/>'s
    /// exact value as an embedded, disclosed placeholder default -- FREQ.3
    /// §D.2 confirmed that value itself is a PRODUCT_DEFAULT (real
    /// literature supports only a qualitative "easy day between hard
    /// sessions" convention, not a specific hours/days scientific minimum),
    /// not independently re-derived here. Flagged for a future KEY1/KEY2-
    /// pairing-specific evidence phase to revisit if warranted -- not a
    /// final numeric decision.
    /// </summary>
    internal const int MinimumKeySessionToKeySessionSeparationDays = MinimumKeySessionToLongRunSeparationDays;

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
        IReadOnlyList<DateOnly> previousWeekKeySessionDates = [];

        foreach (var week in orderedWeeks)
        {
            var expectedWeekEnd = week.StartDate.AddDays(6);
            if (week.EndDate != expectedWeekEnd)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.WeekDateRangeIncorrect);
            }

            if (week.SessionSlots.Count != preferredDays.Count)
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

            // Phase 10K-FREQ.4: generalized from an exact KEY_SESSION == 1
            // assumption to any keyCount >= 1 (e.g. a hypothetical
            // Intermediate 5D layout with 2 KEY_SESSION slots). For
            // keyCount == 1 this reduces exactly to the pre-FREQ.4 formula
            // (preferredDays.Count - 1 - 1 == preferredDays.Count - 2).
            var roleCounts = week.SessionSlots.GroupBy(s => s.StructuralRole).ToDictionary(g => g.Key, g => g.Count());
            var keyCount = roleCounts.GetValueOrDefault("KEY_SESSION");
            if (keyCount < 1 ||
                roleCounts.GetValueOrDefault("EASY_SUPPORT") != preferredDays.Count - keyCount - 1 ||
                roleCounts.GetValueOrDefault("LONG_RUN") != 1)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.RoleCountIncorrect);
            }

            var longRunSlot = week.SessionSlots.FirstOrDefault(s => s.StructuralRole == "LONG_RUN");
            // Phase 10K-FREQ.4: was FirstOrDefault -- silently validated only
            // the first KEY_SESSION slot in a multi-KEY week, never flagging
            // a spacing violation on the second+ instance. Now enumerates
            // every KEY_SESSION slot in the week.
            var keySessionSlots = week.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.SessionDate).ToList();

            if (longRunSlot is not null && longRunSlot.SessionDayOfWeek != longRunDayPreference)
            {
                errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.LongRunDateNotOnLongRunDayPreference);
            }

            if (longRunSlot is not null && keySessionSlots.Count > 0)
            {
                // Accumulate as booleans, add each error at most once per week --
                // preserves the exact pre-FREQ.4 single-add-per-violation-type
                // semantics for the keyCount == 1 case (verified by regression),
                // rather than adding duplicate error entries per KEY instance.
                var anySameWeekSeparationViolated = false;
                var anyCrossWeekSeparationViolated = false;

                foreach (var keySessionSlot in keySessionSlots)
                {
                    var separation = Math.Abs(longRunSlot.SessionDate.DayNumber - keySessionSlot.SessionDate.DayNumber);
                    if (separation < MinimumKeySessionToLongRunSeparationDays)
                    {
                        anySameWeekSeparationViolated = true;
                    }

                    if (previousWeekLongRunDate is not null)
                    {
                        var crossSeparationA = Math.Abs(keySessionSlot.SessionDate.DayNumber - previousWeekLongRunDate.Value.DayNumber);
                        if (crossSeparationA < MinimumKeySessionToLongRunSeparationDays)
                        {
                            anyCrossWeekSeparationViolated = true;
                        }
                    }
                }

                foreach (var previousKeySessionDate in previousWeekKeySessionDates)
                {
                    var crossSeparationB = Math.Abs(longRunSlot.SessionDate.DayNumber - previousKeySessionDate.DayNumber);
                    if (crossSeparationB < MinimumKeySessionToLongRunSeparationDays)
                    {
                        anyCrossWeekSeparationViolated = true;
                    }
                }

                if (anySameWeekSeparationViolated)
                {
                    errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.KeySessionLongRunSeparationViolated);
                }
                if (previousWeekLongRunDate is not null && anyCrossWeekSeparationViolated)
                {
                    errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.CrossWeekSeparationViolated);
                }

                // Phase 10K-FREQ.4, Section C: new KEY<->KEY same-week
                // separation check. No-op for keyCount == 1 (no pairs exist).
                var anyKeyToKeySeparationViolated = false;
                for (var i = 0; i < keySessionSlots.Count; i++)
                {
                    for (var j = i + 1; j < keySessionSlots.Count; j++)
                    {
                        var keyToKeySeparation = Math.Abs(keySessionSlots[i].SessionDate.DayNumber - keySessionSlots[j].SessionDate.DayNumber);
                        if (keyToKeySeparation < MinimumKeySessionToKeySessionSeparationDays)
                        {
                            anyKeyToKeySeparationViolated = true;
                        }
                    }
                }
                if (anyKeyToKeySeparationViolated)
                {
                    errors.Add(DatedGeneratedCatalogPlanSkeletonValidationError.KeySessionKeySessionSeparationViolated);
                }

                previousWeekLongRunDate = longRunSlot.SessionDate;
                previousWeekKeySessionDates = keySessionSlots.Select(s => s.SessionDate).ToList();
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
