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
/// Phase 10K-FREQ.6D.4D.5B — generalized from an exactly-one-KEY_SESSION-
/// per-week assumption to any keyCount >= 1 (e.g. the real Intermediate 5D
/// layout, 2 KEY_SESSION + 2 EASY_SUPPORT + 1 LONG_RUN), per the frozen
/// FREQ.6D.4D.5A rule (<see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays"/>).
/// For keyCount == 1 every step below reduces exactly to the pre-5B
/// algorithm (proven by full legacy 3D/4D/Beginner×4D regression, not
/// merely asserted) — this is one generic solver, not two competing
/// algorithms.
///
/// Algorithm summary (see PHASE4F_5_CALENDAR_DAY_ASSIGNMENT_POLICY_AND_MATERIALIZER.md
/// for the original narrative; this remark documents the 5B generalization):
/// 1. Validate the source skeleton's structural role composition (keyCount >= 1
///    KEY_SESSION, DaysPerWeek - keyCount - 1 EASY_SUPPORT, one LONG_RUN per
///    week; no REST/OPTIONAL; DaysPerWeek in {3, 4, 5} — the real layouts
///    this catalog authors today).
/// 2. Validate PreferredDays/LongRunDayPreference per the race-plan hard-constraint
///    policy (required, exactly DaysPerWeek distinct days, LongRunDayPreference ∈ PreferredDays).
/// 3. For every plan-relative week, map each preferred weekday to the unique
///    date inside that week's own [StartDate, EndDate] range (works regardless
///    of which weekday the week starts on — no Monday alignment).
/// 4. LONG_RUN is fixed to LongRunDayPreference's mapped date in every week.
/// 5. For each week, rank the remaining (DaysPerWeek - 1) dates as
///    KEY_SESSION candidates: only dates >= <see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays"/>
///    calendar days from that week's own LONG_RUN date qualify; ranked by
///    (a) descending distance from LONG_RUN, (b) ascending (chronologically
///    earlier) date as the tie-break — unchanged from the pre-5B algorithm;
///    this ranking is itself the "existing materializer authority" FREQ.6D.4D.5A
///    §14 permits reusing instead of raw chronological order.
/// 6. A bounded depth-first backtracking search picks one <em>keyCount-sized
///    combination</em> of KEY_SESSION dates per week (not a single date), in
///    WeekNumber order, enumerating each week's own ranked candidates as
///    ascending-index combinations (so keyCount == 1 degenerates to trying
///    each candidate singly, in the exact original order) and rejecting any
///    combination whose dates are not all pairwise >= <see cref="DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays"/>
///    apart (a no-op filter for keyCount == 1 — no pairs exist), while still
///    checking the same two cross-week LONG_RUN adjacency facts as before
///    (now against every date in the combination, not a single scalar). The
///    search space is bounded by C(DaysPerWeek - 1, keyCount) combinations
///    per week (at most C(6,2) = 15 for the real 5D shape) × the plan's week
///    count — trivially bounded for every supported horizon (8-14 weeks).
///    The first complete valid assignment found (in this fixed, deterministic
///    exploration order) is returned — never randomized, never dependent on
///    hash-set iteration order.
/// 7. The two EASY_SUPPORT slots receive the two remaining dates, matched by
///    ascending original <see cref="GeneratedCatalogSessionSlotSkeleton.SlotOrderInWeek"/>
///    to ascending chronological date order — unchanged from the pre-5B
///    algorithm. The keyCount KEY_SESSION slots receive the keyCount chosen
///    dates the identical way: ascending <see cref="GeneratedCatalogSessionSlotSkeleton.SlotOrderInWeek"/>
///    maps to ascending chosen date — the same, already-existing tie-break
///    convention applied to a second repeated role, per FREQ.6D.4D.5A §19/§20
///    (not a new decision). This means the layout-authored earlier KEY_SESSION
///    slot (lower SlotOrderInWeek, e.g. RUN_LAYOUT_5D's LaneOrdinal 0) always
///    receives the chronologically earlier of the two chosen dates — a
///    byproduct of the tie-break, never a hard requirement the search itself
///    enforces (LaneOrdinal is never derived from the assigned date; it is
///    already fixed upstream by the binder before this class ever runs).
/// 8. If no full-plan assignment satisfies every constraint, throws
///    <see cref="CatalogPreferredDayConfigurationUnsafeException"/> — never a
///    partial result, never a moved/dropped session, never a substituted
///    default weekday pattern, never a fallback to a different DaysPerWeek/
///    RunLayout.
/// </remarks>
internal sealed class CatalogWeekSkeletonCalendarMaterializer : ICatalogWeekSkeletonCalendarMaterializer
{
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

        var chosenKeySessionDates = new IReadOnlyList<DateOnly>?[weekPlans.Count];
        if (!TryAssignKeySessionDates(weekPlans, chosenKeySessionDates, 0))
        {
            throw new CatalogPreferredDayConfigurationUnsafeException(
                "No deterministic full-plan assignment satisfies the KEY_SESSION/LONG_RUN and " +
                "KEY_SESSION/KEY_SESSION separation invariants (same-week or cross-week) for the " +
                "supplied PreferredDays/LongRunDayPreference combination. No session was moved to " +
                "an unselected date, no session was dropped, no role was changed, and no default " +
                "weekday pattern was substituted.");
        }

        var datedWeeks = new List<DatedGeneratedCatalogWeekSkeleton>(weekPlans.Count);
        for (var i = 0; i < weekPlans.Count; i++)
        {
            datedWeeks.Add(BuildDatedWeek(weekPlans[i], chosenKeySessionDates[i]!, longRunDay));
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
        // Phase 10K-FREQ.6D.4D.5B: admits the real 5D layout (2 KEY_SESSION)
        // alongside the pre-existing 3D/4D layouts. Not opened to arbitrary
        // DaysPerWeek -- these are the only real layouts this catalog authors
        // today (FREQ.6D.4D.5B §5: "do not widen implementation beyond what
        // the real root cause requires").
        if (skeleton.DaysPerWeek is not (3 or 4 or 5))
        {
            throw new CatalogCalendarRoleStructureInvalidException(
                $"Core calendar assignment supports resolved 3D/4D/5D layouts, but the source skeleton declares {skeleton.DaysPerWeek}.");
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

            // Phase 10K-FREQ.6D.4D.5B: generalized from an exact KEY_SESSION
            // == 1 assumption to any keyCount >= 1, mirroring the identical
            // generalization FREQ.4 already applied to the separate output
            // validator (DatedGeneratedCatalogPlanSkeletonValidator). For
            // keyCount == 1 this reduces exactly to the pre-5B formula
            // (DaysPerWeek - 1 - 1 == DaysPerWeek - 2).
            var expectedEasy = skeleton.DaysPerWeek - keyCount - 1;
            if (keyCount < 1 || easyCount != expectedEasy || longCount != 1 || keyCount + easyCount + longCount != skeleton.DaysPerWeek)
            {
                throw new CatalogCalendarRoleStructureInvalidException(
                    $"Week {week.WeekNumber} does not match resolved RunLayout cardinality: expected KEY_SESSION>=1, " +
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
        int KeyCount,
        IReadOnlyDictionary<DayOfWeek, DateOnly> DateByWeekday,
        IReadOnlyList<DateOnly> KeySessionCandidates);

    private static WeekPlan BuildWeekPlan(GeneratedCatalogWeekSkeleton week, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDay)
    {
        var dateByWeekday = preferredDays.ToDictionary(day => day, day => MapWeekdayToDateInWeek(week.StartDate, day));
        var longRunDate = dateByWeekday[longRunDay];
        var keyCount = week.SessionSlots.Count(s => s.StructuralRole == "KEY_SESSION");

        var candidates = preferredDays
            .Where(day => day != longRunDay)
            .Select(day => dateByWeekday[day])
            .Where(date => DaySeparation(date, longRunDate) >= DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays)
            .OrderByDescending(date => DaySeparation(date, longRunDate))
            .ThenBy(date => date.DayNumber)
            .ToList();

        return new WeekPlan(week, longRunDate, keyCount, dateByWeekday, candidates);
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

    /// <summary>
    /// Yields every <paramref name="k"/>-sized subset of <paramref name="items"/>
    /// as ascending-index combinations (indices i_0 &lt; i_1 &lt; ... &lt; i_(k-1)),
    /// preserving <paramref name="items"/>' own order — for k == 1 this yields
    /// each item singly, in list order, identical to a plain foreach.
    /// Deterministic; never randomized; never dependent on hash-set order.
    /// </summary>
    private static IEnumerable<IReadOnlyList<DateOnly>> Combinations(IReadOnlyList<DateOnly> items, int k)
    {
        if (k <= 0 || k > items.Count)
        {
            yield break;
        }

        var indices = new int[k];
        for (var i = 0; i < k; i++)
        {
            indices[i] = i;
        }

        while (true)
        {
            yield return indices.Select(i => items[i]).ToList();

            var slot = k - 1;
            while (slot >= 0 && indices[slot] == items.Count - k + slot)
            {
                slot--;
            }

            if (slot < 0)
            {
                yield break;
            }

            indices[slot]++;
            for (var i = slot + 1; i < k; i++)
            {
                indices[i] = indices[i - 1] + 1;
            }
        }
    }

    private static bool AllPairsSatisfyKeyToKeySeparation(IReadOnlyList<DateOnly> combination)
    {
        for (var i = 0; i < combination.Count; i++)
        {
            for (var j = i + 1; j < combination.Count; j++)
            {
                if (DaySeparation(combination[i], combination[j]) < DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // ── Step 6: bounded cross-week backtracking search ───────────────────────

    private static bool TryAssignKeySessionDates(IReadOnlyList<WeekPlan> weekPlans, IReadOnlyList<DateOnly>?[] chosen, int weekIndex)
    {
        if (weekIndex == weekPlans.Count)
        {
            return true;
        }

        var plan = weekPlans[weekIndex];

        if (weekIndex > 0)
        {
            // This week's fixed LONG_RUN date vs every one of the previous
            // week's already-chosen KEY_SESSION dates.
            var previousKeySessionDates = chosen[weekIndex - 1]!;
            foreach (var previousKeySessionDate in previousKeySessionDates)
            {
                if (DaySeparation(plan.LongRunDate, previousKeySessionDate) < DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays)
                {
                    return false;
                }
            }
        }

        foreach (var combination in Combinations(plan.KeySessionCandidates, plan.KeyCount))
        {
            if (!AllPairsSatisfyKeyToKeySeparation(combination))
            {
                continue;
            }

            if (weekIndex > 0)
            {
                // Every date in this week's candidate combination vs the
                // previous week's fixed LONG_RUN date.
                var previousLongRunDate = weekPlans[weekIndex - 1].LongRunDate;
                if (combination.Any(candidate => DaySeparation(candidate, previousLongRunDate) < DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays))
                {
                    continue;
                }
            }

            chosen[weekIndex] = combination;
            if (TryAssignKeySessionDates(weekPlans, chosen, weekIndex + 1))
            {
                return true;
            }

            chosen[weekIndex] = null;
        }

        return false;
    }

    // ── Steps 7-8: building the final dated week ─────────────────────────────

    private static DatedGeneratedCatalogWeekSkeleton BuildDatedWeek(WeekPlan plan, IReadOnlyList<DateOnly> keySessionDates, DayOfWeek longRunDay)
    {
        var week = plan.Source;
        // Ascending SlotOrderInWeek -> ascending chosen date, the same
        // already-existing tie-break convention EASY_SUPPORT already used,
        // now also applied to the (possibly repeated) KEY_SESSION role -- not
        // a new decision (FREQ.6D.4D.5A §19/§20). This never derives lane
        // identity from the date: LaneOrdinal is fixed upstream by the
        // binder before this class ever runs; this is purely a deterministic
        // tie-break for which of the keyCount chosen dates goes to which
        // already-existing structural slot.
        var sortedKeySessionDates = keySessionDates.OrderBy(date => date.DayNumber).ToList();
        var keySessionDateSet = new HashSet<DateOnly>(sortedKeySessionDates);
        var remainingForEasy = plan.DateByWeekday.Values
            .Where(date => date != plan.LongRunDate && !keySessionDateSet.Contains(date))
            .OrderBy(date => date.DayNumber)
            .ToList();

        var datedSlots = new List<DatedGeneratedCatalogSessionSlotSkeleton>(week.SessionSlots.Count);
        var easyIndex = 0;
        var keyIndex = 0;

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
                assignedDate = sortedKeySessionDates[keyIndex];
                assignmentRule = "KEY_SESSION_DETERMINISTIC_MAX_SEPARATION_FROM_LONG_RUN_AND_KEY_TO_KEY";
                keyIndex++;
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
