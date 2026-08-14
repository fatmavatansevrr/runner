namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.5 — transforms an already-resolved Phase
/// 4F.2/4F.4 <see cref="GeneratedCatalogPlanSkeleton"/> plus a race plan's
/// authoritative PreferredDays/LongRunDayPreference into a fully dated
/// <see cref="DatedGeneratedCatalogPlanSkeleton"/>. Assigns only calendar
/// dates to already-structural session slots — never a workout prescription,
/// distance, duration, pace, intensity, segment, or weekly volume.
/// </summary>
internal interface ICatalogWeekSkeletonCalendarMaterializer
{
    DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context);
}

/// <inheritdoc cref="ICatalogWeekSkeletonCalendarMaterializer"/>
/// <remarks>
/// Deterministic and fully dependency-free by construction: this class has
/// no constructor dependencies at all (no database, clock, HTTP/request,
/// route-decider, resolver, or catalog-loader access is even possible — none
/// is injected). <see cref="Materialize"/> is a pure function of its
/// <see cref="CatalogCalendarAssignmentContext"/> argument and never mutates
/// <see cref="CatalogCalendarAssignmentContext.PlanSkeleton"/>.
///
/// Algorithm summary (see PHASE4F_5_CALENDAR_DAY_ASSIGNMENT_POLICY_AND_MATERIALIZER.md
/// for the full narrative):
/// 1. Validate the source skeleton's structural role composition (exactly one
///    KEY_SESSION, two EASY_SUPPORT, one LONG_RUN per week; no REST/OPTIONAL).
/// 2. Validate PreferredDays/LongRunDayPreference per the race-plan hard-constraint
///    policy (required, exactly 4 distinct days, LongRunDayPreference ∈ PreferredDays).
/// 3. For every plan-relative week, map each preferred weekday to the unique
///    date inside that week's own [StartDate, EndDate] range (works regardless
///    of which weekday the week starts on — no Monday alignment).
/// 4. LONG_RUN is fixed to LongRunDayPreference's mapped date in every week.
/// 5. For each week, rank the remaining 3 dates as KEY_SESSION candidates:
///    only dates >= 2 calendar days from that week's own LONG_RUN date qualify;
///    ranked by (a) descending distance from LONG_RUN, (b) ascending
///    (chronologically earlier) date as the tie-break. ("Preserve structural
///    slot ordering" from the product decision is a no-op tie-break here:
///    there is exactly one KEY_SESSION slot to place per week, so no second
///    slot-ordering signal exists beyond the date itself.)
/// 6. A bounded depth-first backtracking search picks one KEY_SESSION
///    candidate per week, in WeekNumber order, trying each week's own
///    ranked candidates in order and checking only the two cross-week
///    adjacency facts that can ever be affected (this week's fixed LONG_RUN
///    vs the previous week's chosen KEY_SESSION; this week's candidate
///    KEY_SESSION vs the previous week's fixed LONG_RUN) — the search space
///    is at most 3 candidates × 12 weeks, trivially bounded. The first
///    complete valid assignment found (in this fixed, deterministic
///    exploration order) is returned — never randomized, never dependent on
///    hash-set iteration order.
/// 7. The two EASY_SUPPORT slots receive the two remaining dates, matched by
///    ascending original <see cref="GeneratedCatalogSessionSlotSkeleton.SlotOrderInWeek"/>
///    to ascending chronological date order — deterministic, no reliance on
///    collection iteration order.
/// 8. If no full-plan assignment satisfies every constraint, throws
///    <see cref="CatalogPreferredDayConfigurationUnsafeException"/> — never a
///    partial result, never a moved/dropped session, never a substituted
///    default weekday pattern.
/// </remarks>
internal sealed class CatalogWeekSkeletonCalendarMaterializer : ICatalogWeekSkeletonCalendarMaterializer
{
    private const int MinimumKeySessionToLongRunSeparationDays = 2;

    public DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context)
    {
        var skeleton = context.PlanSkeleton;

        ValidateSkeletonRoleStructure(skeleton);
        var preferredDays = ValidatePreferredDays(context.PreferredDays, skeleton.DaysPerWeek);
        var longRunDay = ValidateLongRunDay(context.LongRunDayPreference, preferredDays);

        if (skeleton.StartDate != context.StartDate)
        {
            throw new CatalogCalendarRoleStructureInvalidException(
                $"Context StartDate {context.StartDate} does not match source skeleton StartDate {skeleton.StartDate}.");
        }

        var weekPlans = skeleton.Weeks
            .OrderBy(w => w.WeekNumber)
            .Select(week => BuildWeekPlan(week, preferredDays, longRunDay))
            .ToList();

        var chosenKeySessionDates = new DateOnly?[weekPlans.Count];
        if (!TryAssignKeySessionDates(weekPlans, chosenKeySessionDates, 0))
        {
            throw new CatalogPreferredDayConfigurationUnsafeException(
                "No deterministic full-plan assignment satisfies the KEY_SESSION/LONG_RUN separation invariant " +
                "(same-week or cross-week) for the supplied PreferredDays/LongRunDayPreference combination. " +
                "No session was moved to an unselected date, no role was changed, and no default weekday " +
                "pattern was substituted.");
        }

        var datedWeeks = new List<DatedGeneratedCatalogWeekSkeleton>(weekPlans.Count);
        for (var i = 0; i < weekPlans.Count; i++)
        {
            datedWeeks.Add(BuildDatedWeek(weekPlans[i], chosenKeySessionDates[i]!.Value, longRunDay));
        }

        var provenance = new CatalogCalendarMaterializationProvenance(
            skeleton.CandidateKey,
            skeleton.CandidateVersion,
            context.Provenance.AsOfDate,
            context.StartDate,
            preferredDays,
            longRunDay,
            CatalogCalendarDayMaterializerVersion.V1,
            skeleton.SchemaVersion,
            skeleton.DependencyVersions);

        return new DatedGeneratedCatalogPlanSkeleton(
            DatedGeneratedCatalogPlanSkeleton.CurrentSchemaVersion,
            skeleton.StartDate,
            skeleton.EndDate,
            skeleton.PlannedWeekCount,
            datedWeeks,
            provenance);
    }

    // ── Step 1: source skeleton structural-role validation ──────────────────

    private static void ValidateSkeletonRoleStructure(GeneratedCatalogPlanSkeleton skeleton)
    {
        if (skeleton.DaysPerWeek is not (3 or 4))
        {
            throw new CatalogCalendarRoleStructureInvalidException(
                $"Core calendar assignment supports resolved 3D/4D layouts, but the source skeleton declares {skeleton.DaysPerWeek}.");
        }

        foreach (var week in skeleton.Weeks)
        {
            if (week.SessionSlots.Count != skeleton.DaysPerWeek)
            {
                throw new CatalogCalendarRoleStructureInvalidException(
                    $"Week {week.WeekNumber} has {week.SessionSlots.Count} session slots; expected {skeleton.DaysPerWeek} from resolved RunLayout.");
            }

            var roleCounts = week.SessionSlots.GroupBy(s => s.StructuralRole).ToDictionary(g => g.Key, g => g.Count());

            foreach (var role in roleCounts.Keys)
            {
                if (role.Contains("REST", StringComparison.OrdinalIgnoreCase) || role.Contains("OPTIONAL", StringComparison.OrdinalIgnoreCase))
                {
                    throw new CatalogCalendarRoleStructureInvalidException(
                        $"Week {week.WeekNumber} declares an unsupported structural role '{role}'.");
                }
            }

            var keyCount = roleCounts.GetValueOrDefault("KEY_SESSION");
            var easyCount = roleCounts.GetValueOrDefault("EASY_SUPPORT");
            var longCount = roleCounts.GetValueOrDefault("LONG_RUN");

            var expectedEasy = skeleton.DaysPerWeek - 2;
            if (keyCount != 1 || easyCount != expectedEasy || longCount != 1 || keyCount + easyCount + longCount != skeleton.DaysPerWeek)
            {
                throw new CatalogCalendarRoleStructureInvalidException(
                    $"Week {week.WeekNumber} does not match resolved RunLayout cardinality: expected KEY_SESSION=1, " +
                    $"EASY_SUPPORT={expectedEasy}, LONG_RUN=1; found KEY_SESSION={keyCount}, EASY_SUPPORT={easyCount}, LONG_RUN={longCount}.");
            }

            var expectedEnd = week.StartDate.AddDays(6);
            if (week.EndDate != expectedEnd)
            {
                throw new CatalogCalendarRoleStructureInvalidException(
                    $"Week {week.WeekNumber} EndDate {week.EndDate} does not equal StartDate + 6 days ({expectedEnd}).");
            }
        }
    }

    // ── Step 2: PreferredDays / LongRunDayPreference domain validation ───────

    private static IReadOnlyList<DayOfWeek> ValidatePreferredDays(IReadOnlyList<DayOfWeek> preferredDays, int expectedCount)
    {
        if (preferredDays.Count == 0)
        {
            throw new CatalogPreferredDaysRequiredException(
                "Race-plan catalog calendar assignment requires PreferredDays, but none was supplied.");
        }

        if (preferredDays.Count != expectedCount)
        {
            throw new CatalogPreferredDayCountInvalidException(
                $"PreferredDays.Count must equal resolved RunLayout.RunsPerWeek ({expectedCount}), but {preferredDays.Count} were supplied.");
        }

        if (preferredDays.Distinct().Count() != preferredDays.Count)
        {
            throw new CatalogPreferredDaysDuplicatedException(
                $"PreferredDays contains a duplicate weekday: [{string.Join(", ", preferredDays)}].");
        }

        return preferredDays;
    }

    private static DayOfWeek ValidateLongRunDay(DayOfWeek? longRunDayPreference, IReadOnlyList<DayOfWeek> preferredDays)
    {
        if (longRunDayPreference is null)
        {
            throw new CatalogLongRunDayRequiredException(
                "Race-plan catalog calendar assignment requires LongRunDayPreference, but none was supplied.");
        }

        if (!preferredDays.Contains(longRunDayPreference.Value))
        {
            throw new CatalogLongRunDayNotPreferredException(
                $"LongRunDayPreference '{longRunDayPreference.Value}' does not belong to PreferredDays " +
                $"[{string.Join(", ", preferredDays)}].");
        }

        return longRunDayPreference.Value;
    }

    // ── Steps 3-5: per-week candidate structure ──────────────────────────────

    private sealed record WeekPlan(
        GeneratedCatalogWeekSkeleton Source,
        DateOnly LongRunDate,
        IReadOnlyDictionary<DayOfWeek, DateOnly> DateByWeekday,
        IReadOnlyList<DateOnly> KeySessionCandidates);

    private static WeekPlan BuildWeekPlan(GeneratedCatalogWeekSkeleton week, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay)
    {
        var dateByWeekday = preferredDays.ToDictionary(day => day, day => MapWeekdayToDateInWeek(week.StartDate, day));
        var longRunDate = dateByWeekday[longRunDay];

        var candidates = preferredDays
            .Where(day => day != longRunDay)
            .Select(day => dateByWeekday[day])
            .Where(date => DaySeparation(date, longRunDate) >= MinimumKeySessionToLongRunSeparationDays)
            .OrderByDescending(date => DaySeparation(date, longRunDate))
            .ThenBy(date => date.DayNumber)
            .ToList();

        return new WeekPlan(week, longRunDate, dateByWeekday, candidates);
    }

    /// <summary>
    /// Maps <paramref name="targetWeekday"/> to the unique date inside
    /// [<paramref name="weekStart"/>, weekStart + 6] having that weekday.
    /// Works regardless of which weekday <paramref name="weekStart"/> itself
    /// falls on — no Monday-based week-number API is used.
    /// </summary>
    private static DateOnly MapWeekdayToDateInWeek(DateOnly weekStart, DayOfWeek targetWeekday)
    {
        var offset = ((int)targetWeekday - (int)weekStart.DayOfWeek + 7) % 7;
        return weekStart.AddDays(offset);
    }

    private static int DaySeparation(DateOnly a, DateOnly b) => Math.Abs(a.DayNumber - b.DayNumber);

    // ── Step 6: bounded cross-week backtracking search ───────────────────────

    private static bool TryAssignKeySessionDates(IReadOnlyList<WeekPlan> weekPlans, DateOnly?[] chosen, int weekIndex)
    {
        if (weekIndex == weekPlans.Count)
        {
            return true;
        }

        var plan = weekPlans[weekIndex];

        if (weekIndex > 0)
        {
            // This week's fixed LONG_RUN date vs the previous week's already-chosen KEY_SESSION date.
            var previousKeySessionDate = chosen[weekIndex - 1]!.Value;
            if (DaySeparation(plan.LongRunDate, previousKeySessionDate) < MinimumKeySessionToLongRunSeparationDays)
            {
                return false;
            }
        }

        foreach (var candidate in plan.KeySessionCandidates)
        {
            if (weekIndex > 0)
            {
                // This week's candidate KEY_SESSION date vs the previous week's fixed LONG_RUN date.
                var previousLongRunDate = weekPlans[weekIndex - 1].LongRunDate;
                if (DaySeparation(candidate, previousLongRunDate) < MinimumKeySessionToLongRunSeparationDays)
                {
                    continue;
                }
            }

            chosen[weekIndex] = candidate;
            if (TryAssignKeySessionDates(weekPlans, chosen, weekIndex + 1))
            {
                return true;
            }

            chosen[weekIndex] = null;
        }

        return false;
    }

    // ── Steps 7-8: building the final dated week ─────────────────────────────

    private static DatedGeneratedCatalogWeekSkeleton BuildDatedWeek(WeekPlan plan, DateOnly keySessionDate, DayOfWeek longRunDay)
    {
        var week = plan.Source;
        var remainingForEasy = plan.DateByWeekday.Values
            .Where(date => date != plan.LongRunDate && date != keySessionDate)
            .OrderBy(date => date.DayNumber)
            .ToList();

        var datedSlots = new List<DatedGeneratedCatalogSessionSlotSkeleton>(week.SessionSlots.Count);
        var easyIndex = 0;

        foreach (var slot in week.SessionSlots.OrderBy(s => s.SlotOrderInWeek))
        {
            DateOnly assignedDate;
            string assignmentRule;

            if (slot.StructuralRole == "LONG_RUN")
            {
                assignedDate = plan.LongRunDate;
                assignmentRule = "LONG_RUN_FIXED_TO_LONG_RUN_DAY_PREFERENCE";
            }
            else if (slot.StructuralRole == "KEY_SESSION")
            {
                assignedDate = keySessionDate;
                assignmentRule = "KEY_SESSION_DETERMINISTIC_MAX_SEPARATION_FROM_LONG_RUN";
            }
            else
            {
                assignedDate = remainingForEasy[easyIndex];
                assignmentRule = "EASY_SUPPORT_REMAINING_DATE_ASCENDING_SLOT_ORDER";
                easyIndex++;
            }

            datedSlots.Add(new DatedGeneratedCatalogSessionSlotSkeleton(
                slot.SlotOrderInWeek,
                slot.LayoutSlotKey,
                slot.StructuralRole,
                assignedDate,
                assignedDate.DayOfWeek,
                new CatalogSessionCalendarProvenance(
                    slot.LayoutSlotKey,
                    slot.StructuralRole,
                    assignedDate.DayOfWeek,
                    assignedDate,
                    assignmentRule)));
        }

        return new DatedGeneratedCatalogWeekSkeleton(
            week.WeekNumber,
            week.StartDate,
            week.EndDate,
            week.StageKey,
            week.StageWeekIndex,
            week.StageWeekCount,
            datedSlots.OrderBy(s => s.SlotOrderInWeek).ToList(),
            new CatalogWeekCalendarProvenance(week.WeekNumber, week.StageKey, week.WeekNumber, CatalogCalendarAssignmentPolicy.RaceHardConstraint));
    }
}
