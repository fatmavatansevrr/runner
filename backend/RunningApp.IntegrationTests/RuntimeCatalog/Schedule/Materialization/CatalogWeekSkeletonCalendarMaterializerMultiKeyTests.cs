using System;
using System.Linq;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 10K-FREQ.6D.4D.5B — unit-level tests for
/// <see cref="CatalogWeekSkeletonCalendarMaterializer"/>'s multi-KEY_SESSION
/// generalization (the real Intermediate 5D shape: 2 KEY_SESSION + 2
/// EASY_SUPPORT + 1 LONG_RUN), using synthetic hand-built fixtures rather
/// than the real catalog (see <see cref="Freq6D4D5BReal5DDarkPlanTests"/> for
/// the real RUN_LAYOUT_5D/TEN_K__5D__INTERMEDIATE end-to-end coverage).
/// </summary>
public sealed class CatalogWeekSkeletonCalendarMaterializerMultiKeyTests
{
    private static readonly CatalogWeekSkeletonCalendarMaterializer Materializer = new();
    private static readonly string[] FiveDaySlotRoleOrder = { "KEY_SESSION", "EASY_SUPPORT", "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" };
    private static readonly DateOnly MondayStart = new(2026, 8, 3); // Monday

    private static GeneratedCatalogPlanSkeleton FiveDaySkeleton(int weekCount = 1) =>
        CatalogCalendarAssignmentFixtures.BuildSkeleton(MondayStart, weekCount, FiveDaySlotRoleOrder);

    // ── Original defect reproduction (pre-5B this threw; now must succeed) ──

    [Fact]
    public void Materialize_FiveDayTwoKeySkeleton_NoLongerThrowsRoleStructureInvalid()
    {
        var skeleton = FiveDaySkeleton();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        var dated = Materializer.Materialize(context);

        Assert.Single(dated.Weeks);
        Assert.Equal(2, dated.Weeks[0].SessionSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
    }

    // ── §20-22: direct KEY<->KEY separation results ──────────────────────────

    [Fact]
    public void TueWed_ConsecutiveKeyDates_RejectedWhenNoOtherLegalPairExists()
    {
        // Mon/Tue/Wed/Thu/Fri, LONG=Tue: after excluding Mon/Wed (too close
        // to Tue's LONG_RUN under the unchanged KEY<->LONG rule), only
        // {Thu, Fri} remain KEY-eligible; abs(Thu-Fri)=1 < 2 is exactly the
        // Tue/Wed-shaped consecutive rejection this phase's own §20 names,
        // reproduced with a genuinely forced pair (no alternative exists).
        var skeleton = FiveDaySkeleton();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Tuesday);

        Assert.Throws<CatalogPreferredDayConfigurationUnsafeException>(() => Materializer.Materialize(context));
    }

    [Fact]
    public void TueThu_MinimumLegalSeparation_IsAccepted()
    {
        // Tue/Wed/Thu/Sat/Sun, LONG=Sat: Sun is excluded (1 day from LONG),
        // leaving {Tue, Wed, Thu} KEY-eligible. Ranked by descending distance
        // from LONG (Tue=4, Wed=3, Thu=2) the deterministic search tries
        // (Tue,Wed) first (sep=1, invalid, skipped) then (Tue,Thu) -- exactly
        // 2 apart, the frozen minimum -- which is accepted.
        var skeleton = FiveDaySkeleton();
        var days = new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Saturday);

        var dated = Materializer.Materialize(context);
        var keyDates = dated.Weeks[0].SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").Select(s => s.SessionDate).OrderBy(d => d).ToList();
        var keyDays = keyDates.Select(d => d.DayOfWeek).ToList();

        Assert.Equal(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, keyDays);
        Assert.Equal(2, Math.Abs(keyDates[0].DayNumber - keyDates[1].DayNumber));
    }

    [Fact]
    public void TueFri_LargerSeparation_RemainsValid_NotEqualityOnlySeparation()
    {
        // Mon/Tue/Wed/Fri/Sun, LONG=Sun: Tue and Fri are 3 days apart --
        // confirms the rule is >= 2, not == 2.
        var skeleton = FiveDaySkeleton();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        var dated = Materializer.Materialize(context);
        var keyDates = dated.Weeks[0].SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").Select(s => s.SessionDate).OrderBy(d => d).ToList();

        Assert.True(Math.Abs(keyDates[0].DayNumber - keyDates[1].DayNumber) >= 2);
    }

    // ── §17: KEY<->LONG preserved independently for every KEY instance ──────

    [Fact]
    public void KeyToLongSeparation_EnforcedIndependentlyForBothKeyInstances_NotOnlyTheFirst()
    {
        var skeleton = FiveDaySkeleton(weekCount: 6);
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, week =>
        {
            var longRun = week.SessionSlots.Single(s => s.StructuralRole == "LONG_RUN");
            var keySlots = week.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
            Assert.Equal(2, keySlots.Count);
            Assert.All(keySlots, key => Assert.True(Math.Abs(key.SessionDate.DayNumber - longRun.SessionDate.DayNumber) >= 2));
        });
    }

    // ── §26: no role collapse ─────────────────────────────────────────────

    [Fact]
    public void TwoDistinctDatedKeySessionSlots_NeverCollapseIntoOne()
    {
        var skeleton = FiveDaySkeleton(weekCount: 3);
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, week =>
        {
            var keySlots = week.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").ToList();
            Assert.Equal(2, keySlots.Count);
            Assert.Equal(2, keySlots.Select(s => s.SessionDate).Distinct().Count());
            Assert.Equal(2, keySlots.Select(s => s.LayoutSlotKey).Distinct().Count());
        });
    }

    // ── §13/§46: deterministic slot-order -> date-order tie-break ───────────

    [Fact]
    public void FirstCanonicalKeySlot_AlwaysReceivesEarlierChosenDate_SecondReceivesLater()
    {
        var skeleton = FiveDaySkeleton(weekCount: 4);
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        var dated = Materializer.Materialize(context);

        Assert.All(dated.Weeks, week =>
        {
            var keySlots = week.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.SlotOrderInWeek).ToList();
            Assert.True(keySlots[0].SlotOrderInWeek < keySlots[1].SlotOrderInWeek);
            Assert.True(keySlots[0].SessionDate < keySlots[1].SessionDate);
        });
    }

    // ── §27: lineage preservation -- only Date is newly assigned ────────────

    [Fact]
    public void StructuralLineageFields_SurviveUnchanged_OnlyDateIsNew()
    {
        var skeleton = FiveDaySkeleton();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        var dated = Materializer.Materialize(context);

        var sourceSlots = skeleton.Weeks[0].SessionSlots.OrderBy(s => s.SlotOrderInWeek).ToList();
        var datedSlots = dated.Weeks[0].SessionSlots.OrderBy(s => s.SlotOrderInWeek).ToList();
        for (var i = 0; i < sourceSlots.Count; i++)
        {
            Assert.Equal(sourceSlots[i].SlotOrderInWeek, datedSlots[i].SlotOrderInWeek);
            Assert.Equal(sourceSlots[i].LayoutSlotKey, datedSlots[i].LayoutSlotKey);
            Assert.Equal(sourceSlots[i].StructuralRole, datedSlots[i].StructuralRole);
        }
    }

    // ── §9/§10: EASY_SUPPORT adjacency semantics unchanged ──────────────────

    [Fact]
    public void EasySupport_MayBeAdjacentToKeyOrLongOrEachOther_NoNewRestriction()
    {
        var skeleton = FiveDaySkeleton();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        // No throw is the assertion -- EASY_SUPPORT has no adjacency
        // restriction in the algorithm, unchanged by this generalization.
        var dated = Materializer.Materialize(context);
        Assert.Equal(2, dated.Weeks[0].SessionSlots.Count(s => s.StructuralRole == "EASY_SUPPORT"));
    }

    // ── §15/§18: no-legal-assignment fails typed, never silently coerces ────

    [Fact]
    public void NoLegalAssignment_ThrowsTypedFailure_NeverDropsASessionOrMovesLongRun()
    {
        var skeleton = FiveDaySkeleton();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Tuesday);

        var ex = Assert.Throws<CatalogPreferredDayConfigurationUnsafeException>(() => Materializer.Materialize(context));
        Assert.Contains("KEY_SESSION/KEY_SESSION", ex.Message);
    }

    // ── §45: determinism, including under reversed input collection order ──

    [Fact]
    public void Materialize_SameFiveDayInput_ProducesIdenticalOutputAcrossRepeatedRuns()
    {
        var skeleton = FiveDaySkeleton(weekCount: 3);
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        var first = Materializer.Materialize(context);
        var second = Materializer.Materialize(context);

        Assert.Equal(
            first.Weeks.Select(w => w.SessionSlots.Select(s => (s.StructuralRole, s.SlotOrderInWeek, s.SessionDate))),
            second.Weeks.Select(w => w.SessionSlots.Select(s => (s.StructuralRole, s.SlotOrderInWeek, s.SessionDate))));
    }

    [Fact]
    public void Materialize_ReversedPreferredDaysCollectionOrder_ProducesIdenticalOutput()
    {
        var skeleton = FiveDaySkeleton(weekCount: 3);
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var reversed = days.Reverse().ToArray();

        var result1 = Materializer.Materialize(CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday));
        var result2 = Materializer.Materialize(CatalogCalendarAssignmentFixtures.BuildContext(skeleton, reversed, DayOfWeek.Sunday));

        Assert.Equal(
            result1.Weeks.Select(w => w.SessionSlots.Select(s => (s.StructuralRole, s.SessionDate))),
            result2.Weeks.Select(w => w.SessionSlots.Select(s => (s.StructuralRole, s.SessionDate))));
    }

    // ── §29: validator defense-in-depth agrees with the materializer ───────

    [Fact]
    public void MaterializedOutput_IndependentlyPassesDatedSkeletonValidator()
    {
        var skeleton = FiveDaySkeleton(weekCount: 4);
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var context = CatalogCalendarAssignmentFixtures.BuildContext(skeleton, days, DayOfWeek.Sunday);

        var dated = Materializer.Materialize(context);
        var validation = new DatedGeneratedCatalogPlanSkeletonValidator().Validate(dated, days, DayOfWeek.Sunday);

        Assert.True(validation.IsValid, string.Join(", ", validation.Errors));
    }

    // ── Single numeric authority (§29 of the originating prompt) ────────────

    [Fact]
    public void KeyToKeySeparationConstant_IsOwnedByTheValidator_NotDuplicated()
    {
        // Structural proof that the materializer does not maintain its own
        // second, independently-drifting numeric literal for either rule --
        // both are the validator's own internal constants, reused by
        // reference (confirmed by direct source inspection: no `private
        // const int Minimum...` remains on the materializer type itself).
        var materializerFields = typeof(CatalogWeekSkeletonCalendarMaterializer)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance)
            .Where(f => f.IsLiteral && f.FieldType == typeof(int));

        Assert.Empty(materializerFields);
        Assert.Equal(2, DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays);
        Assert.Equal(2, DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays);
    }
}
