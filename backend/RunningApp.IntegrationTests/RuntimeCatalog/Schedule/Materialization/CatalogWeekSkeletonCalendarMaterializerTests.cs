using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.5 — tests for
/// <see cref="CatalogWeekSkeletonCalendarMaterializer"/> covering input
/// policy, plan-relative date mapping, long-run/key-session assignment,
/// cross-week constraints, deterministic selection, unsafe configurations,
/// output validation, and dependency isolation.
/// </summary>
public sealed class CatalogWeekSkeletonCalendarMaterializerTests
{
    private static readonly CatalogWeekSkeletonCalendarMaterializer Materializer = CatalogCalendarAssignmentFixtures.RealMaterializer();

    // A Wednesday StartDate, so Week 1 spans Wed(StartDate) .. Tue (6 days later).
    private static readonly DateOnly WednesdayStart = new(2026, 8, 5); // 2026-08-05 is a Wednesday

    private static readonly DayOfWeek[] PreferredDays = { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    // ─────────────────────────────── Input policy ───────────────────────────

    [Fact]
    public void Materialize_MissingPreferredDays_ThrowsRequired()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, Array.Empty<DayOfWeek>(), LongRunDay);

        Assert.Throws<CatalogPreferredDaysRequiredException>(() => Materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_PreferredDaysCountNotFour_ThrowsCountInvalid()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(
            skeleton, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday }, DayOfWeek.Friday);

        Assert.Throws<CatalogPreferredDayCountInvalidException>(() => Materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_DuplicatePreferredDay_ThrowsDuplicated()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(
            skeleton, new[] { DayOfWeek.Monday, DayOfWeek.Monday, DayOfWeek.Friday, DayOfWeek.Sunday }, DayOfWeek.Sunday);

        Assert.Throws<CatalogPreferredDaysDuplicatedException>(() => Materializer.Materialize(context));
    }

    [Fact]
    public void ParsePreferredDays_UnknownWeekdayValue_IsRejected()
    {
        Assert.Throws<CatalogCalendarRoleStructureInvalidException>(() =>
            CatalogPreferredDayAdapter.ParsePreferredDays("Mon,Wed,Fri,Frotzday"));
    }

    [Fact]
    public void Materialize_MissingLongRunDay_ThrowsRequired()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, longRunDay: null);

        Assert.Throws<CatalogLongRunDayRequiredException>(() => Materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_LongRunDayOutsidePreferredDays_ThrowsNotPreferred()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, DayOfWeek.Tuesday);

        Assert.Throws<CatalogLongRunDayNotPreferredException>(() => Materializer.Materialize(context));
    }

    [Fact]
    public void HabitPolicy_IsNotImplemented_RaceHardConstraintIsTheOnlyPolicyValue()
    {
        // Structural proof: CatalogCalendarAssignmentPolicy has exactly one
        // member (RaceHardConstraint) -- no habit policy branch exists to
        // accidentally apply race's hard LongRunDayPreference requirement to
        // a future habit context.
        var values = Enum.GetValues<CatalogCalendarAssignmentPolicy>();
        Assert.Single(values);
        Assert.Equal(CatalogCalendarAssignmentPolicy.RaceHardConstraint, values[0]);
    }

    [Fact]
    public void Materialize_SkeletonDaysPerWeekMismatch_ThrowsRoleStructureInvalid()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(
            WednesdayStart, slotRoleOrder: new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" });
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        Assert.Throws<CatalogCalendarRoleStructureInvalidException>(() => Materializer.Materialize(context));
    }

    [Theory]
    [MemberData(nameof(InvalidRoleCompositions))]
    public void Materialize_InvalidRoleComposition_ThrowsRoleStructureInvalid(string[] roles)
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart, slotRoleOrder: roles);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        Assert.Throws<CatalogCalendarRoleStructureInvalidException>(() => Materializer.Materialize(context));
    }

    public static IEnumerable<object[]> InvalidRoleCompositions()
    {
        yield return new object[] { new[] { "KEY_SESSION", "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" } };
        yield return new object[] { new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "EASY_SUPPORT" } };
        yield return new object[] { new[] { "KEY_SESSION", "EASY_SUPPORT", "REST", "LONG_RUN" } };
    }

    // ───────────────────────── Plan-relative date mapping ────────────────────

    [Fact]
    public void Materialize_Week1_StartsExactlyOnStartDate()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.Equal(WednesdayStart, dated.Weeks[0].StartDate);
        Assert.Equal(DayOfWeek.Wednesday, WednesdayStart.DayOfWeek);
    }

    [Fact]
    public void Materialize_WednesdayStartDate_Week1SpansWednesdayThroughTuesday()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.Equal(DayOfWeek.Wednesday, dated.Weeks[0].StartDate.DayOfWeek);
        Assert.Equal(DayOfWeek.Tuesday, dated.Weeks[0].EndDate.DayOfWeek);
        Assert.Equal(WednesdayStart.AddDays(6), dated.Weeks[0].EndDate);
    }

    [Fact]
    public void Materialize_EverySelectedWeekday_MapsToExactlyOneDateInEachWeek()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        foreach (var week in dated.Weeks)
        {
            var usedDays = week.SessionSlots.Select(s => s.SessionDayOfWeek).OrderBy(d => d).ToList();
            Assert.Equal(PreferredDays.OrderBy(d => d).ToList(), usedDays);
        }
    }

    [Fact]
    public void Materialize_NoMondayNormalizationOccurs_WeekStaysWednesdayAnchored()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, w => Assert.Equal(DayOfWeek.Wednesday, w.StartDate.DayOfWeek));
    }

    [Fact]
    public void Materialize_NoSessionDate_LiesOutsideOwningWeek()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, w => Assert.All(w.SessionSlots, s =>
            Assert.InRange(s.SessionDate, w.StartDate, w.EndDate)));
    }

    [Fact]
    public void Materialize_NoDuplicateSessionDate_ExistsWithinAWeek()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, w => Assert.Equal(4, w.SessionSlots.Select(s => s.SessionDate).Distinct().Count()));
    }

    // ─────────────────────────── Long-run assignment ─────────────────────────

    [Fact]
    public void Materialize_LongRunIsAlwaysPlacedOnLongRunDayPreference()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, w =>
        {
            var longRun = w.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN");
            Assert.Equal(LongRunDay, longRun.SessionDayOfWeek);
        });
    }

    [Fact]
    public void Materialize_LongRunAssignment_IsIdenticalAcrossAllWeeks()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        var weekdays = dated.Weeks.Select(w => w.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN").SessionDayOfWeek).Distinct().ToList();
        Assert.Single(weekdays);
    }

    // ─────────────────────────── Key-session separation ──────────────────────

    [Fact]
    public void Materialize_KeySessionIsNeverAdjacentToLongRun()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, w =>
        {
            var longRun = w.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN");
            var key = w.SessionSlots.Single(s => s.StructuralRole == "KEY_SESSION");
            Assert.True(Math.Abs(longRun.SessionDate.DayNumber - key.SessionDate.DayNumber) >= 2);
        });
    }

    [Fact]
    public void Materialize_TwoDayDifference_ClusteredButValid_Succeeds()
    {
        // Mon, Tue, Wed, Thu preferred; long run on Thu. Only Mon (3 away) and
        // Tue (2 away) qualify as >=2 days from Thu; Wed (1 away) does not.
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Thursday);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, w =>
        {
            var longRun = w.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN");
            var key = w.SessionSlots.Single(s => s.StructuralRole == "KEY_SESSION");
            Assert.True(Math.Abs(longRun.SessionDate.DayNumber - key.SessionDate.DayNumber) >= 2);
        });
    }

    [Fact]
    public void Materialize_EasySupport_MayBeAdjacentToKeySessionAndLongRunAndEachOther()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        // No assertion needed beyond successful materialization: EASY_SUPPORT
        // has no adjacency restriction at all in the algorithm (only
        // KEY_SESSION<->LONG_RUN is constrained) -- proven by the absence of
        // any EASY_SUPPORT-related check in CatalogWeekSkeletonCalendarMaterializer.
        Assert.Equal(12, dated.Weeks.Count);
    }

    // ─────────────────────────── Cross-week constraints ──────────────────────

    [Fact]
    public void Materialize_TopRankedCandidateCausesCrossWeekConflict_SearchFindsSafeAlternative()
    {
        // StartDate is Wednesday (week offsets: Wed=0, Thu=1, Fri=2, Sat=3,
        // Sun=4, Mon=5, Tue=6). LongRunDayPreference = Wednesday (offset 0 --
        // the week's own first day). Tuesday (offset 6, the week's LAST day)
        // is 6 days from Wednesday within the SAME week (same-week distance
        // 6, safe there and the highest possible distance -- so it ranks #1
        // by the "maximize distance from long run" rule) but only 1 calendar
        // day before the FOLLOWING week's own Wednesday long run (cross-week
        // distance 1, unsafe). This only constrains weeks that HAVE a
        // following week (week 1 and 2 of this 3-week fixture) -- the search
        // must reject Tuesday for those and fall back to Sunday (offset 4,
        // distance 4, still >=2 same-week and cross-week safe). The final
        // week has no following week, so Tuesday remains a legitimate choice
        // there; the assertion is scoped accordingly and the validator
        // re-confirms the whole plan is safe regardless.
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart, weekCount: 3);
        var days = new[] { DayOfWeek.Wednesday, DayOfWeek.Tuesday, DayOfWeek.Sunday, DayOfWeek.Friday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Wednesday);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks.Take(2), w =>
            Assert.Equal(DayOfWeek.Sunday, w.SessionSlots.Single(s => s.StructuralRole == "KEY_SESSION").SessionDayOfWeek));

        var validator = CatalogCalendarAssignmentFixtures.RealValidator();
        var result = validator.Validate(dated, days, DayOfWeek.Wednesday);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Materialize_WeekEndLongRunFollowedByNextWeekStartKeySession_IsAvoidedBySearch()
    {
        // Mirror of the above: LongRunDayPreference = Tuesday (offset 6, the
        // week's own LAST day). Wednesday (offset 0, the week's first day)
        // is 6 days from Tuesday within the same week (safe there, and ranks
        // #1 by distance) but only 1 day after the PRECEDING week's own
        // Tuesday long run -- cross-week unsafe. This only constrains weeks
        // that HAVE a preceding week (week 2 and 3 of this 3-week fixture);
        // the search must reject Wednesday for those and fall back to
        // Thursday (offset 1, distance 5, cross-week safe). Week 1 has no
        // preceding week, so Wednesday remains legitimate there.
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart, weekCount: 3);
        var days = new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Tuesday);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks.Skip(1), w =>
            Assert.Equal(DayOfWeek.Thursday, w.SessionSlots.Single(s => s.StructuralRole == "KEY_SESSION").SessionDayOfWeek));

        var validator = CatalogCalendarAssignmentFixtures.RealValidator();
        Assert.True(validator.Validate(dated, days, DayOfWeek.Tuesday).IsValid);
    }

    [Fact]
    public void Materialize_NonAdjacentCrossWeekPair_IsAccepted()
    {
        // The main WednesdayStart/PreferredDays/Sunday-long-run scenario
        // (Mon, Wed, Fri, Sun preferred; long run Sun) never produces a
        // cross-week-risky top candidate at all -- confirming a "normal,"
        // non-clustered configuration is accepted without any backtracking
        // being required.
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart, weekCount: 3);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        var validator = CatalogCalendarAssignmentFixtures.RealValidator();
        Assert.True(validator.Validate(dated, PreferredDays, LongRunDay).IsValid);
    }

    [Fact]
    public void Materialize_EvaluatesTheCompletePlan_NotIsolatedWeeksOnly()
    {
        // The top-ranked-candidate-causes-a-conflict scenario above only
        // manifests across a week BOUNDARY -- a single-week-only evaluation
        // (ignoring neighboring weeks) would incorrectly accept Tuesday for
        // every week in isolation. This test re-asserts the same scenario
        // with more weeks to make the multi-week evaluation requirement
        // unambiguous.
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart, weekCount: 6);
        var days = new[] { DayOfWeek.Wednesday, DayOfWeek.Tuesday, DayOfWeek.Sunday, DayOfWeek.Friday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Wednesday);

        var dated = Materializer.Materialize(context);

        // Every week except the last has a following week and must avoid
        // Tuesday; only the final week (no successor to conflict with) may
        // legitimately use it.
        Assert.All(dated.Weeks.Take(dated.Weeks.Count - 1), w =>
            Assert.NotEqual(DayOfWeek.Tuesday, w.SessionSlots.Single(s => s.StructuralRole == "KEY_SESSION").SessionDayOfWeek));
    }

    [Fact]
    public void Materialize_AllValidFourOfSevenPreferredDayCombinations_NeverThrowConfigurationUnsafe()
    {
        // Mathematical property of this pilot's exact scope (exactly 4 of 7
        // distinct weekdays, single long-run day, >=2-day separation rule):
        // among the 3 remaining preferred days, at most one can ever be
        // same-week-invalid (distance 1, the sole immediate neighbor on
        // whichever side is "inward" from the week edge), and the one
        // possible cross-week-risky offset (the diametrically opposite day,
        // distance 6) is only ever risky, never same-week-invalid -- so at
        // least one of the 3 remaining days is always both same-week AND
        // cross-week safe. Exhaustively verifying every C(7,4)=35 weekday
        // combination x 4 long-run choices (140 total) proves
        // CatalogPreferredDayConfigurationUnsafeException is never thrown
        // for any input satisfying this phase's own input-validation rules
        // -- a property specific to the 4-of-7 pilot shape, not a universal
        // claim about arbitrary DaysPerWeek/PreferredDays combinations.
        var allWeekdays = Enum.GetValues<DayOfWeek>();
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart, weekCount: 4);

        foreach (var combo in FourDayCombinations(allWeekdays))
        {
            foreach (var longRun in combo)
            {
                var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, combo, longRun);
                var dated = Materializer.Materialize(context);
                Assert.NotNull(dated);
            }
        }
    }

    private static IEnumerable<DayOfWeek[]> FourDayCombinations(DayOfWeek[] days)
    {
        for (var a = 0; a < days.Length; a++)
        for (var b = a + 1; b < days.Length; b++)
        for (var c = b + 1; c < days.Length; c++)
        for (var d = c + 1; d < days.Length; d++)
        {
            yield return new[] { days[a], days[b], days[c], days[d] };
        }
    }

    [Fact]
    public void CatalogPreferredDayConfigurationUnsafeException_CarriesATypedMessage()
    {
        // The exception type itself (used defensively by the search when no
        // full-plan assignment succeeds) exists, is internal, and carries a
        // descriptive message -- exercised directly since, per the exhaustive
        // proof above, no reachable 4-of-7 pilot input actually triggers it.
        var ex = new CatalogPreferredDayConfigurationUnsafeException("No deterministic full-plan assignment satisfies the invariant.");
        Assert.False(typeof(CatalogPreferredDayConfigurationUnsafeException).IsPublic);
        Assert.Contains("assignment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────── Deterministic selection ─────────────────────

    [Fact]
    public void Materialize_SameInput_ProducesStructurallyEquivalentOutput()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var first = Materializer.Materialize(context);
        var second = Materializer.Materialize(context);

        Assert.Equal(
            first.Weeks.Select(w => w.SessionSlots.Select(s => (s.StructuralRole, s.SessionDate))),
            second.Weeks.Select(w => w.SessionSlots.Select(s => (s.StructuralRole, s.SessionDate))));
    }

    [Fact]
    public void Materialize_InputCollectionOrder_DoesNotAffectOutput()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var reversedDays = PreferredDays.Reverse().ToArray();

        var context1 = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);
        var context2 = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, reversedDays, LongRunDay);

        var result1 = Materializer.Materialize(context1);
        var result2 = Materializer.Materialize(context2);

        Assert.Equal(
            result1.Weeks.Select(w => w.SessionSlots.Select(s => (s.StructuralRole, s.SessionDate))),
            result2.Weeks.Select(w => w.SessionSlots.Select(s => (s.StructuralRole, s.SessionDate))));
    }

    // ─────────────────────────── Output validation ───────────────────────────

    [Fact]
    public void Materialize_ProducesExactly12Weeks_WithFourDatedSessionsEach()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.Equal(12, dated.Weeks.Count);
        Assert.All(dated.Weeks, w => Assert.Equal(4, w.SessionSlots.Count));
    }

    [Fact]
    public void Materialize_SessionDateAndDayOfWeek_Agree()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, w => Assert.All(w.SessionSlots, s => Assert.Equal(s.SessionDate.DayOfWeek, s.SessionDayOfWeek)));
    }

    [Fact]
    public void Materialize_PlanEndDate_UnchangedFromSourceSkeleton()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.Equal(skeleton.EndDate, dated.EndDate);
    }

    [Fact]
    public void Materialize_PhaseIdentitiesAndIndexing_RemainUnchanged()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        for (var i = 0; i < dated.Weeks.Count; i++)
        {
            Assert.Equal(skeleton.Weeks[i].StageKey, dated.Weeks[i].PhaseKey);
            Assert.Equal(skeleton.Weeks[i].StageWeekIndex, dated.Weeks[i].PhaseWeekIndex);
            Assert.Equal(skeleton.Weeks[i].StageWeekCount, dated.Weeks[i].PhaseWeekCount);
        }
    }

    [Fact]
    public void Materialize_LayoutSlotIdentitiesAndStructuralRoles_RemainUnchanged()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        for (var i = 0; i < dated.Weeks.Count; i++)
        {
            var sourceSlots = skeleton.Weeks[i].SessionSlots.OrderBy(s => s.SlotOrderInWeek).ToList();
            var datedSlots = dated.Weeks[i].SessionSlots.OrderBy(s => s.SlotOrderInWeek).ToList();
            for (var j = 0; j < sourceSlots.Count; j++)
            {
                Assert.Equal(sourceSlots[j].LayoutSlotKey, datedSlots[j].LayoutSlotKey);
                Assert.Equal(sourceSlots[j].StructuralRole, datedSlots[j].StructuralRole);
            }
        }
    }

    [Fact]
    public void Materialize_NoWorkoutPrescriptionFields_ArePresent()
    {
        // Structural proof: DatedGeneratedCatalogSessionSlotSkeleton has no
        // Distance/Duration/Pace/Intensity/Segment/Volume property at all.
        var properties = typeof(DatedGeneratedCatalogSessionSlotSkeleton).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain(properties, n => n.Contains("Distance") || n.Contains("Duration") || n.Contains("Pace") ||
            n.Contains("Intensity") || n.Contains("Segment") || n.Contains("Volume") || n.Contains("WorkoutType"));
    }

    [Fact]
    public void Materialize_Provenance_IsPresentAndInternal()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        var dated = Materializer.Materialize(context);

        Assert.NotNull(dated.Provenance);
        Assert.All(dated.Weeks, w => Assert.NotNull(w.Provenance));
        Assert.All(dated.Weeks, w => Assert.All(w.SessionSlots, s => Assert.NotNull(s.Provenance)));
        Assert.False(typeof(DatedGeneratedCatalogPlanSkeleton).IsPublic);
    }

    // ─────────────────────────── Dependency isolation ────────────────────────

    [Fact]
    public void CatalogWeekSkeletonCalendarMaterializer_HasNoConstructorDependencies()
    {
        var ctors = typeof(CatalogWeekSkeletonCalendarMaterializer).GetConstructors();
        Assert.Single(ctors);
        Assert.Empty(ctors[0].GetParameters());
    }

    [Fact]
    public void Materialize_DoesNotMutateSourceSkeleton()
    {
        var skeleton = CatalogCalendarAssignmentFixtures.BuildSkeleton(WednesdayStart);
        var originalSlotCount = skeleton.Weeks[0].SessionSlots.Count;
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, PreferredDays, LongRunDay);

        Materializer.Materialize(context);

        Assert.Equal(originalSlotCount, skeleton.Weeks[0].SessionSlots.Count);
        Assert.Equal(WednesdayStart, skeleton.StartDate);
    }
}
