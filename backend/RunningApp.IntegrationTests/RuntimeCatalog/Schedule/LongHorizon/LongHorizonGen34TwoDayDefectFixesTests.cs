using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 10K-GEN.34 -- dark, unit-level verification of the four
/// structural-materializer gaps GEN.33 disclosed but did not fix (its own §8
/// remaining contract, items 1-4), plus two further gaps this phase's own
/// additional trace found (items 5-6 below), before the admission gate is
/// opened. Every existing (non-2D) caller's behavior is separately
/// re-verified byte-identical by the untouched full regression suite; this
/// file exercises only the new, additive fixes directly.
/// </summary>
public sealed class LongHorizonGen34TwoDayDefectFixesTests
{
    private static string RepoRoot() => TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    private static LongHorizonCompositionDecision BuildDecision(int totalWeeks, ReadinessProfile profile)
    {
        var startDate = new DateOnly(2026, 9, 7);
        var raceDate = startDate.AddDays(totalWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(startDate, raceDate);
        return LongHorizonCompositionResolver.Resolve(coreHorizon, profile);
    }

    // ── Item 1: MaterializeAsync's own daysPerWeek guard rejected 2 ─────────

    [Theory]
    [InlineData(21, RunningBackground.Beginner)]
    [InlineData(21, RunningBackground.Intermediate)]
    [InlineData(52, RunningBackground.Beginner)]
    [InlineData(52, RunningBackground.Intermediate)]
    public async Task MaterializeAsync_TwoDay_NoLongerRejected_ProducesValidSkeleton(int totalWeeks, RunningBackground level)
    {
        var decision = BuildDecision(totalWeeks, ReadinessProfile.ConsistencyNeeded);
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            decision, CatalogRoot(), Loader(), default, daysPerWeek: 2, level);

        Assert.Equal(totalWeeks, skeleton.TotalWeeks);
        Assert.Equal(totalWeeks, skeleton.Weeks.Count);
        // MaterializeAsync itself throws if LongHorizonStructuralValidator
        // rejects the produced skeleton -- a successful return already
        // proves validity; re-asserted explicitly here for clarity.
        var validation = LongHorizonStructuralValidator.Validate(skeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Findings));
    }

    [Fact]
    public async Task MaterializeAsync_UnsupportedDaysPerWeek_StillFailsClosed()
    {
        var decision = BuildDecision(21, ReadinessProfile.ConsistencyNeeded);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonStructuralMaterializer.MaterializeAsync(decision, CatalogRoot(), Loader(), default, daysPerWeek: 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonStructuralMaterializer.MaterializeAsync(decision, CatalogRoot(), Loader(), default, daysPerWeek: 7));
    }

    // ── Item 2: CandidateKey/CandidateVersion dispatch had no 2D case ───────

    [Fact]
    public async Task MaterializeAsync_TwoDayBeginner_ResolvesTwoDayBeginnerIdentity_NotFourDayIntermediateDefault()
    {
        var decision = BuildDecision(21, ReadinessProfile.ConsistencyNeeded);
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            decision, CatalogRoot(), Loader(), default, daysPerWeek: 2, RunningBackground.Beginner);

        Assert.Equal(V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, skeleton.CandidateKey);
        Assert.Equal(V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion, skeleton.CandidateVersion);
        // Pre-GEN.34, the bare default arm would have silently mislabeled
        // this as the 4D Intermediate identity instead.
        Assert.NotEqual(LongHorizonStructuralMaterializer.CandidateKey, skeleton.CandidateKey);
    }

    [Fact]
    public async Task MaterializeAsync_TwoDayIntermediate_ResolvesTwoDayIntermediateIdentity_NotFourDayIntermediateDefault()
    {
        var decision = BuildDecision(21, ReadinessProfile.CoreEntryReady);
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            decision, CatalogRoot(), Loader(), default, daysPerWeek: 2, RunningBackground.Intermediate);

        Assert.Equal(V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateKey, skeleton.CandidateKey);
        Assert.Equal(V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateVersion, skeleton.CandidateVersion);
        Assert.NotEqual(LongHorizonStructuralMaterializer.CandidateKey, skeleton.CandidateKey);
    }

    // ── Item 3: GE-selector call never passed alternatingKeyEasy ────────────
    // Determination (required by the governing prompt): was this INERT or
    // WRONG-OUTPUT before the fix? Evidence: MaterializeAsync's own
    // daysPerWeek guard (item 1) threw for daysPerWeek==2 *before* reaching
    // the GE-selector call site at all, so no real 2D request could ever
    // reach the un-conditioned call -- it was dark/unreachable, exactly like
    // every other line in this method for 2D. Separately, and independently:
    // for every daysPerWeek this call site *was* reachable for pre-GEN.34
    // (3, 4, 5, 6), LongHorizonGeCardinality.Resolve's own alternatingKeyEasy
    // component is `false` for every value != 2 -- identical to the value
    // Select's own default parameter already supplied when omitted. So even
    // considered in isolation from the (also-blocking) daysPerWeek guard,
    // passing the resolved tuple explicitly is a literal no-op for every
    // pre-GEN.34-reachable input. Conclusion: INERT, on two independent
    // grounds -- never reachable, and even if reached, never distinguishable
    // from omitting it. No 2D GE week was ever materialized with the wrong
    // (non-alternating) shape by this specific call site, because no 2D
    // request could reach it at all.
    [Fact]
    public void LongHorizonGeCardinality_AlternatingKeyEasy_IsFalseForEveryPreGen34ReachableDaysPerWeek()
    {
        foreach (var daysPerWeek in new[] { 3, 4, 5, 6 })
        {
            var (easySupportCount, alternatingKeyEasy) = LongHorizonGeCardinality.Resolve(daysPerWeek);
            Assert.False(alternatingKeyEasy);
            Assert.Equal(daysPerWeek - 2, easySupportCount);
        }
    }

    [Fact]
    public async Task MaterializeAsync_TwoDay_GeWeeksNowGenuinelyAlternate_ClosingItem3()
    {
        var decision = BuildDecision(21, ReadinessProfile.ConsistencyNeeded); // geWeeks = 1
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            decision, CatalogRoot(), Loader(), default, daysPerWeek: 2, RunningBackground.Intermediate);

        var geWeeks = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();
        foreach (var week in geWeeks)
        {
            var keyCount = week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION");
            var easyCount = week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT");
            if (week.GlobalWeekNumber % 2 == 1)
            {
                Assert.Equal(1, keyCount);
                Assert.Equal(0, easyCount);
            }
            else
            {
                Assert.Equal(0, keyCount);
                Assert.Equal(1, easyCount);
            }
        }
    }

    // ── Item 4: MaterializeCore had no 2D case (defaulted to RUN_LAYOUT_4D) ─

    [Theory]
    [InlineData(21)] // geWeeks=1 (odd)
    [InlineData(22)] // geWeeks=2 (even)
    public async Task MaterializeAsync_TwoDay_CoreWeeksUseRunLayout2D_AndAlternateContinuingGlobalParity(int totalWeeks)
    {
        var decision = BuildDecision(totalWeeks, ReadinessProfile.ConsistencyNeeded);
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            decision, CatalogRoot(), Loader(), default, daysPerWeek: 2, RunningBackground.Intermediate);

        var coreWeeks = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.Core).ToList();
        Assert.Equal(12, coreWeeks.Count);
        foreach (var week in coreWeeks)
        {
            Assert.Equal(2, week.OrderedWorkoutSlots.Count);
            Assert.Equal(1, week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "LONG_RUN"));

            if (week.WeekType == "TAPER")
            {
                // GEN.11 §5's structural taper override: TAPER always uses
                // Pattern A regardless of global parity.
                Assert.Equal(1, week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
                continue;
            }

            var expectedPatternA = week.GlobalWeekNumber % 2 == 1;
            Assert.Equal(expectedPatternA ? 1 : 0, week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION"));
            Assert.Equal(expectedPatternA ? 0 : 1, week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT"));
        }
    }

    // ── Item 5 (this phase's own additional find): BuildRunwayWeek/BuildCoreWeek
    // hardcoded HasKeySession=true regardless of actual materialized content ──

    [Theory]
    [InlineData(21)]
    [InlineData(22)]
    public async Task MaterializeAsync_TwoDay_RunwayAndCoreWeeks_HasKeySessionMatchesActualSlots(int totalWeeks)
    {
        var decision = BuildDecision(totalWeeks, ReadinessProfile.ConsistencyNeeded);
        var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
            decision, CatalogRoot(), Loader(), default, daysPerWeek: 2, RunningBackground.Intermediate);

        foreach (var week in skeleton.Weeks.Where(w => w.Segment is LongHorizonSegmentType.PreparationRunway or LongHorizonSegmentType.Core))
        {
            var actualHasKey = week.OrderedWorkoutSlots.Any(s => s.StructuralRole == "KEY_SESSION");
            Assert.Equal(actualHasKey, week.HasKeySession);
        }

        // Confirms this assertion has real teeth: at least one Runway and one
        // Core week in this skeleton genuinely has HasKeySession==false
        // (a Pattern-B week) -- were BuildRunwayWeek/BuildCoreWeek still
        // hardcoding the record's default (true), this specific assertion
        // would fail for that week instead of passing vacuously.
        Assert.Contains(skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.PreparationRunway), w => !w.HasKeySession);
        Assert.Contains(skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.Core && w.WeekType != "TAPER"), w => !w.HasKeySession);
    }

    [Fact]
    public async Task MaterializeAsync_TwoDay_FullSkeleton_PassesStructuralValidator_IncludingHasKeySessionCheck()
    {
        // LongHorizonStructuralValidator's own per-week HasKeySession check
        // (GEN.33 §2 defect 2) would have falsely flagged every Runway/Core
        // Pattern-B week had item 5 not been fixed (the record's default,
        // true, would disagree with that week's real 0 KEY_SESSION slots).
        // MaterializeAsync itself throws on validator failure, so a
        // successful, non-throwing call across a broad totalWeeks sweep is
        // itself the strongest proof this fix works end-to-end.
        foreach (var totalWeeks in new[] { 21, 24, 32, 41, 52 })
        {
            var decision = BuildDecision(totalWeeks, ReadinessProfile.CoreEntryReady);
            var skeleton = await LongHorizonStructuralMaterializer.MaterializeAsync(
                decision, CatalogRoot(), Loader(), default, daysPerWeek: 2, RunningBackground.Beginner);
            var validation = LongHorizonStructuralValidator.Validate(skeleton);
            Assert.True(validation.IsValid, $"totalWeeks={totalWeeks}: " + string.Join("; ", validation.Findings));
        }
    }

    // ── Item 6 (this phase's own additional find): LongHorizonCalendarAssigner
    // .AssignWeekdays hard-rejected daysPerWeek<3 and never mapped both a 2D
    // week's alternative role spellings to its single shared weekday ────────

    [Fact]
    public void AssignWeekdays_TwoDay_NoLongerThrows_AndMapsBothRoleSpellingsToTheSameDay()
    {
        var assignments = LongHorizonCalendarAssigner.AssignWeekdays(
            [DayOfWeek.Tuesday, DayOfWeek.Sunday], DayOfWeek.Sunday, daysPerWeek: 2);

        Assert.Equal(DayOfWeek.Sunday, assignments["LONG_RUN"]);
        Assert.Equal(DayOfWeek.Tuesday, assignments["KEY_SESSION"]);
        // The genuinely new part of this fix: a Pattern-B week's own
        // RoleKeyFor lookup asks for "EASY_SUPPORT_1", which pre-fix was
        // entirely absent from this dictionary (a KeyNotFoundException at
        // MapActivatedWeek time) -- it must resolve to the SAME weekday as
        // KEY_SESSION, since a 2D week never has both roles simultaneously.
        Assert.Equal(DayOfWeek.Tuesday, assignments["EASY_SUPPORT_1"]);
    }

    [Fact]
    public void AssignWeekdays_ThreeDayAndUp_StillRejectsBelowTwo_AndUnaffectedAboveTwo()
    {
        Assert.Throws<InvalidOperationException>(() =>
            LongHorizonCalendarAssigner.AssignWeekdays([DayOfWeek.Sunday], DayOfWeek.Sunday, daysPerWeek: 1));

        var fourDay = LongHorizonCalendarAssigner.AssignWeekdays(
            [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], DayOfWeek.Sunday, daysPerWeek: 4);
        Assert.Equal(4, fourDay.Count);
        Assert.False(fourDay.ContainsKey("EASY_SUPPORT_1") && fourDay["EASY_SUPPORT_1"] == fourDay["KEY_SESSION"]);
    }

    // ── Eligibility gate widening ────────────────────────────────────────────

    [Theory]
    [InlineData(RunningBackground.Beginner, 21)]
    [InlineData(RunningBackground.Beginner, 22)]
    [InlineData(RunningBackground.Intermediate, 21)]
    [InlineData(RunningBackground.Intermediate, 22)]
    public void LongHorizonRollingInitialActivationInputValidator_TwoDay_NowEligible(RunningBackground level, int totalWeeks)
    {
        var decision = BuildDecision(totalWeeks, ReadinessProfile.ConsistencyNeeded);
        var request = new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = decision,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = level,
            DaysPerWeek = 2,
            StartDate = new DateOnly(2026, 9, 7),
            RaceDate = new DateOnly(2026, 9, 7).AddDays(totalWeeks * 7),
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(16, 6, 2),
            PreferredDays = [DayOfWeek.Tuesday, DayOfWeek.Sunday],
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = CatalogRoot(),
            WorkoutLoader = Loader(),
        };

        // Pre-GEN.34, this threw LONG_HORIZON_ROLLING_INITIAL_ELIGIBILITY_INVALID
        // for every Level at daysPerWeek==2 (Beginner was never eligible for
        // any daysPerWeek; Intermediate was eligible only at 4/5/6).
        var exception = Record.Exception(() => LongHorizonRollingInitialActivationInputValidator.Validate(request));
        Assert.Null(exception);
    }

    [Fact]
    public void LongHorizonRollingInitialActivationInputValidator_AdvancedTwoDay_StillNotEligible()
    {
        var decision = BuildDecision(21, ReadinessProfile.ConsistencyNeeded);
        var request = new LongHorizonRollingInitialActivationRequest
        {
            CompositionDecision = decision,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Advanced,
            DaysPerWeek = 2,
            StartDate = new DateOnly(2026, 9, 7),
            RaceDate = new DateOnly(2026, 9, 7).AddDays(21 * 7),
            OnboardingBaseline = new LongHorizonGeEntryBaselineInput(16, 6, 2),
            PreferredDays = [DayOfWeek.Tuesday, DayOfWeek.Sunday],
            LongRunDay = DayOfWeek.Sunday,
            CatalogRoot = CatalogRoot(),
            WorkoutLoader = Loader(),
        };

        // No Advanced x2D identity is approved -- this gate deliberately was
        // not widened for Advanced.
        Assert.Throws<LongHorizonRollingInitialActivationException>(() =>
            LongHorizonRollingInitialActivationInputValidator.Validate(request));
    }
}
