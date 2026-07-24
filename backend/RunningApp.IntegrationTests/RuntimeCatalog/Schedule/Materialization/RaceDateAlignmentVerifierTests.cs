using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class RaceDateAlignmentVerifierTests
{
    private static readonly DateOnly StartDate = new(2026, 7, 20);

    [Fact]
    public async Task Verify_RealTwelveWeekPilotSchedule_OutcomeIsPass_FinalSessionDateMatchesEstablishedLiveResult()
    {
        var raceDate = new DateOnly(2026, 10, 12);
        var schedule = await RealDatedScheduleAsync(12);

        var result = RaceDateAlignmentVerifier.Verify(schedule, raceDate);

        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
        Assert.Equal(new DateOnly(2026, 10, 11), result.FinalSessionDate);
        Assert.All(result.Checks, c => Assert.True(c.Passed));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Verify_RealNonPilotWeekCounts_ReportsActualOutcomeAndFinalSessionDate(int weeks)
    {
        var raceDate = StartDate.AddDays(weeks * 7);
        var schedule = await RealDatedScheduleAsync(weeks);

        var result = RaceDateAlignmentVerifier.Verify(schedule, raceDate);

        // Investigated first via the real pipeline before asserting -- see
        // this phase's final report for the full per-target table.
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.Outcome);
        Assert.Empty(result.Findings);
        Assert.Equal(raceDate.AddDays(-1), result.FinalSessionDate);
    }

    [Fact]
    public void Verify_SyntheticSessionAfterRaceDate_OutcomeIsFail_NamesExactDate()
    {
        var raceDate = new DateOnly(2026, 10, 12);
        var schedule = SyntheticSchedule(raceDate,
            SyntheticWeek(1, "BUILD",
                Session(1, "KEY_SESSION", new DateOnly(2026, 10, 13), DayOfWeek.Tuesday))); // after race

        var result = RaceDateAlignmentVerifier.Verify(schedule, raceDate);

        Assert.Equal(RaceDateAlignmentOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("SESSION_AFTER_RACE_DATE") && f.Contains("2026-10-13"));
    }

    [Fact]
    public void Verify_SyntheticFinalSessionOutsideTolerance_OutcomeIsFail_DistinctFromAfterRaceCase()
    {
        var raceDate = new DateOnly(2026, 10, 12);
        // Final session 9 days before race -- outside the 0-7 day tolerance.
        var schedule = SyntheticSchedule(raceDate,
            SyntheticWeek(1, "BUILD",
                Session(1, "KEY_SESSION", raceDate.AddDays(-9), DayOfWeek.Sunday)));

        var result = RaceDateAlignmentVerifier.Verify(schedule, raceDate);

        Assert.Equal(RaceDateAlignmentOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("FINAL_SESSION_OUTSIDE_TOLERANCE"));
        Assert.DoesNotContain(result.Findings, f => f.Contains("SESSION_AFTER_RACE_DATE"));
    }

    [Fact]
    public void Verify_SyntheticSessionOnNonPreferredDay_OutcomeIsFail()
    {
        var raceDate = new DateOnly(2026, 10, 12);
        // Sunday is not a preferred day in this synthetic plan (only Mon/Wed/Fri).
        var schedule = SyntheticSchedule(raceDate,
            SyntheticWeek(1, "BUILD",
                Session(1, "KEY_SESSION", new DateOnly(2026, 10, 4), DayOfWeek.Sunday)),
            preferredDays: [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]);

        var result = RaceDateAlignmentVerifier.Verify(schedule, raceDate);

        Assert.Equal(RaceDateAlignmentOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Contains("SESSION_NOT_ON_PREFERRED_DAY"));
    }

    [Fact]
    public void Verify_MultipleSimultaneousViolations_AllCollected_NotJustFirst()
    {
        var raceDate = new DateOnly(2026, 10, 12);
        var schedule = SyntheticSchedule(raceDate,
            SyntheticWeek(1, "BUILD",
                Session(1, "KEY_SESSION", new DateOnly(2026, 10, 13), DayOfWeek.Tuesday),   // after race
                Session(2, "EASY_SUPPORT", new DateOnly(2026, 10, 5), DayOfWeek.Monday)),
            preferredDays: [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]);

        var result = RaceDateAlignmentVerifier.Verify(schedule, raceDate);

        Assert.Equal(RaceDateAlignmentOutcome.Fail, result.Outcome);
        Assert.True(result.Findings.Count >= 2, $"Expected at least 2 findings, got {result.Findings.Count}: {string.Join(" | ", result.Findings)}");
        Assert.Contains(result.Findings, f => f.Contains("SESSION_AFTER_RACE_DATE"));
        Assert.Contains(result.Findings, f => f.Contains("FINAL_SESSION_OUTSIDE_TOLERANCE"));
    }

    [Fact]
    public void Verify_EmptySchedule_IsNotApplicable()
    {
        var raceDate = new DateOnly(2026, 10, 12);
        var schedule = new DatedGeneratedCatalogPlanSkeleton(
            DatedGeneratedCatalogPlanSkeleton.CurrentSchemaVersion, StartDate, StartDate, 0, [],
            new CatalogCalendarMaterializationProvenance("SYNTHETIC", 1, StartDate, StartDate,
                [DayOfWeek.Monday], DayOfWeek.Monday, CatalogCalendarDayMaterializerVersion.V1, 1, new Dictionary<string, PlanCatalogReference>()));

        var result = RaceDateAlignmentVerifier.Verify(schedule, raceDate);

        Assert.Equal(RaceDateAlignmentOutcome.NotApplicable, result.Outcome);
        Assert.Empty(result.Checks);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public async Task Verify_CalledTwiceWithIdenticalInput_ProducesIdenticalOutput()
    {
        var raceDate = new DateOnly(2026, 10, 12);
        var schedule = await RealDatedScheduleAsync(10);
        var otherRaceDate = StartDate.AddDays(10 * 7);

        var first = RaceDateAlignmentVerifier.Verify(schedule, otherRaceDate);
        var second = RaceDateAlignmentVerifier.Verify(schedule, otherRaceDate);

        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.Checks, second.Checks);
        Assert.Equal(first.Findings, second.Findings);
        Assert.Equal(first.FinalSessionDate, second.FinalSessionDate);
    }

    [Fact]
    public void Verify_IsPureFunction_TwoParametersOnly()
    {
        var method = typeof(RaceDateAlignmentVerifier).GetMethod(nameof(RaceDateAlignmentVerifier.Verify), BindingFlags.Public | BindingFlags.Static)!;

        Assert.Equal(2, method.GetParameters().Length);
        Assert.Equal(typeof(DatedGeneratedCatalogPlanSkeleton), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(DateOnly), method.GetParameters()[1].ParameterType);
    }

    [Fact]
    public void RaceDateAlignmentVerifier_HasNoCallSiteInApplicationOrApiProductionCode()
    {
        DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(nameof(RaceDateAlignmentVerifier));
    }

    [Fact]
    public void RaceDateAlignmentVerifier_DoesNotCallMaterializerAllocatorSchedulerOrBinder()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
            "Schedule", "Materialization", "RaceDateAlignmentVerifier.cs"));

        Assert.DoesNotContain("new CatalogWeekSkeletonCalendarMaterializer(", source);
        Assert.DoesNotContain("new CatalogPhaseAllocationResolver(", source);
        Assert.DoesNotContain("new ProgressionStageAllocator(", source);
        Assert.DoesNotContain("new CatalogWorkoutBinder(", source);
        Assert.DoesNotContain("new PlanCatalogBundleLoader(", source);
    }

    // ── Real-pipeline construction (read-only: real candidate, real allocator, ──
    // ── real progression/dated-calendar materialization -- no live wiring) ──

    private static async Task<DatedGeneratedCatalogPlanSkeleton> RealDatedScheduleAsync(int weeks)
    {
        var root = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = root });

        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance)
            .LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, weeks);
        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);

        var context = new CatalogStageToWeekMaterializationContext
        {
            StartDate = StartDate, AsOfDate = StartDate, PlannedWeekCount = weeks, DaysPerWeek = candidate.SlotRoles.Count,
            CanonicalDistanceFamily = progression.DistanceFamily, CandidateKey = candidate.CandidateKey, CandidateVersion = candidate.CandidateVersion,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
            SelectedStageSequence = allocation.Phases.Select(p => p.PhaseKey).ToList(),
            StageWeekAllocations = allocation.Phases.Select(p => new CatalogStageWeekAllocation(p.PhaseKey, p.AllocatedWeeks)).ToList(),
            RunLayout = candidate.Layout, RunLayoutSlotRoles = candidate.SlotRoles,
        };
        var plainSkeleton = new CatalogStageToWeekMaterializer().Materialize(context).Skeleton;
        var provenance = new CatalogCalendarMaterializationProvenance(candidate.CandidateKey, candidate.CandidateVersion, context.AsOfDate, context.StartDate,
            [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], DayOfWeek.Sunday, CatalogCalendarDayMaterializerVersion.V1, plainSkeleton.SchemaVersion, new Dictionary<string, PlanCatalogReference>());
        return new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(context.StartDate, GoalType.Race,
            provenance.PreferredDays, DayOfWeek.Sunday, plainSkeleton, CatalogCalendarAssignmentPolicy.RaceHardConstraint, provenance));
    }

    private static DatedGeneratedCatalogPlanSkeleton SyntheticSchedule(DateOnly raceDate, DatedGeneratedCatalogWeekSkeleton week, IReadOnlyList<DayOfWeek>? preferredDays = null) =>
        SyntheticScheduleMulti(raceDate, [week], preferredDays);

    private static DatedGeneratedCatalogPlanSkeleton SyntheticScheduleMulti(DateOnly raceDate, IReadOnlyList<DatedGeneratedCatalogWeekSkeleton> weeks, IReadOnlyList<DayOfWeek>? preferredDays = null) => new(
        DatedGeneratedCatalogPlanSkeleton.CurrentSchemaVersion, StartDate, weeks.Max(w => w.SessionSlots.Max(s => s.SessionDate)), weeks.Count, weeks,
        new CatalogCalendarMaterializationProvenance("SYNTHETIC", 1, StartDate, StartDate,
            preferredDays ?? [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], DayOfWeek.Sunday, CatalogCalendarDayMaterializerVersion.V1, 1, new Dictionary<string, PlanCatalogReference>()));

    private static DatedGeneratedCatalogWeekSkeleton SyntheticWeek(int weekNumber, string phaseKey, params DatedGeneratedCatalogSessionSlotSkeleton[] sessions) => new(
        weekNumber, sessions.Min(s => s.SessionDate), sessions.Max(s => s.SessionDate), phaseKey, 1, 1, sessions,
        new CatalogWeekCalendarProvenance(weekNumber, phaseKey, weekNumber, CatalogCalendarAssignmentPolicy.RaceHardConstraint));

    private static DatedGeneratedCatalogSessionSlotSkeleton Session(int slotOrder, string role, DateOnly date, DayOfWeek dayOfWeek) => new(
        slotOrder, $"SLOT_{slotOrder}", role, date, dayOfWeek,
        new CatalogSessionCalendarProvenance($"SLOT_{slotOrder}", role, dayOfWeek, date, "SYNTHETIC"));
}
