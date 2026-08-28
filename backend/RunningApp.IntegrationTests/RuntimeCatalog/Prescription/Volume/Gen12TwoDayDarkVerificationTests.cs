using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// Phase 10K-GEN.12 -- dark verification (no public HTTP, public gate
/// deliberately untouched) for the real, unmodified structural-skeleton and
/// calendar-assignment stages of the Core generation pipeline against the
/// real <c>TEN_K__2D__BEGINNER v1</c> / <c>TEN_K__2D__INTERMEDIATE v1</c>
/// catalog candidates, implementing GEN.11's Model B repeating-pattern
/// authority. No fabricated skeleton.
///
/// Disclosed scope (this phase's own DONE (PARTIAL) classification -- see
/// the phase report): the workout-content BINDING stage
/// (<see cref="RunningApp.Application.RuntimeCatalog.Schedule.Binding.CatalogWorkoutBinder"/>,
/// which assigns actual prescribed workout/pace content into Pattern A's
/// KEY_SESSION slot week-by-week) surfaces a real, deeper gap in
/// <see cref="RunningApp.Application.RuntimeCatalog.Schedule.Progression.ProgressionStageAllocator"/>:
/// it allocates progression-stage exposure across every literal calendar
/// week in a phase, with no concept of "this week has zero structural slots
/// for this lane" -- a real architectural question (how should exposure
/// pacing count when only half the weeks are eligible for the quality
/// lane?) requiring its own dedicated design pass, not a same-shape
/// mechanical generalization like the three defects this phase did fix
/// (below). This file therefore verifies the skeleton/calendar stages in
/// isolation, not through the full <see cref="DynamicCoreVolumeAndLongRunOrchestrator"/>
/// pipeline (which requires binding to succeed).
/// </summary>
public sealed class Gen12TwoDayDarkVerificationTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 5); // Wednesday
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays = new[] { DayOfWeek.Wednesday, DayOfWeek.Sunday };
    private const DayOfWeek LongRunDay = DayOfWeek.Sunday;

    private static string RealCatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions RealOptions() => new() { CatalogRootPath = RealCatalogRoot() };

    internal static async Task<PlanCatalogCandidateSummary> CandidateAsync(string key)
    {
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(RealOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);
        return await gate.LoadForInternalDryRunAsync(key, 1);
    }

    private static GeneratedCatalogPlanSkeleton BuildSkeleton(PlanCatalogCandidateSummary candidate, int targetWeekCount)
    {
        var orchestrator = new DynamicCoreWeekSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator());

        var result = orchestrator.Build(new DynamicCoreWeekSkeletonOrchestrationContext
        {
            Candidate = candidate,
            TargetWeekCount = targetWeekCount,
            StartDate = StartDate,
            AsOfDate = StartDate,
        });
        return result.Skeleton;
    }

    private static DatedGeneratedCatalogPlanSkeleton MaterializeCalendar(
        GeneratedCatalogPlanSkeleton skeleton, PlanCatalogCandidateSummary candidate, IReadOnlyList<DayOfWeek>? preferredDays = null)
    {
        var days = preferredDays ?? PreferredDays;
        var provenance = new CatalogCalendarMaterializationProvenance(
            candidate.CandidateKey, candidate.CandidateVersion, StartDate, StartDate,
            days, LongRunDay, CatalogCalendarDayMaterializerVersion.V1, skeleton.SchemaVersion,
            new Dictionary<string, PlanCatalogReference>());
        return new CatalogWeekSkeletonCalendarMaterializer().Materialize(new CatalogCalendarAssignmentContext(
            StartDate, GoalType.Race, days, LongRunDay, skeleton, CatalogCalendarAssignmentPolicy.RaceHardConstraint, provenance));
    }

    // ── Real catalog identity/capacity: candidate resolves, RunLayout carries the pattern ──

    [Theory]
    [InlineData("TEN_K__2D__BEGINNER")]
    [InlineData("TEN_K__2D__INTERMEDIATE")]
    public async Task Candidate_ResolvesRealTwoDayIdentity_WithRepeatingPattern(string candidateKey)
    {
        var candidate = await CandidateAsync(candidateKey);
        Assert.Equal(2, candidate.DaysPerWeek);
        Assert.NotNull(candidate.WeeklyPatternRoles);
        Assert.Equal(2, candidate.PatternPeriodWeeks);
        Assert.Equal(new[] { "KEY_SESSION", "LONG_RUN" }, candidate.WeeklyPatternRoles![0]);
        Assert.Equal(new[] { "EASY_SUPPORT", "LONG_RUN" }, candidate.WeeklyPatternRoles[1]);
    }

    // ── Structural: Model B A/B alternation, taper override, global week ordinal ──

    [Theory]
    [InlineData("TEN_K__2D__BEGINNER", 8)]
    [InlineData("TEN_K__2D__BEGINNER", 12)]
    [InlineData("TEN_K__2D__BEGINNER", 14)]
    [InlineData("TEN_K__2D__INTERMEDIATE", 8)]
    [InlineData("TEN_K__2D__INTERMEDIATE", 12)]
    [InlineData("TEN_K__2D__INTERMEDIATE", 14)]
    public async Task CoreSkeleton_AlternatesPatternAAndB_TaperAlwaysPatternA(string candidateKey, int targetWeekCount)
    {
        var candidate = await CandidateAsync(candidateKey);
        var skeleton = BuildSkeleton(candidate, targetWeekCount);

        Assert.Equal(targetWeekCount, skeleton.Weeks.Count);
        Assert.All(skeleton.Weeks, w => Assert.Equal(2, w.SessionSlots.Count));
        Assert.All(skeleton.Weeks, w => Assert.Contains(w.SessionSlots, s => s.StructuralRole == "LONG_RUN"));

        foreach (var week in skeleton.Weeks)
        {
            var nonLongRole = week.SessionSlots.Single(s => s.StructuralRole != "LONG_RUN").StructuralRole;
            if (week.StageKey == "TAPER")
            {
                Assert.Equal("KEY_SESSION", nonLongRole);
            }
            else
            {
                // GEN.11 §1/§11: frozen global week-ordinal sequence, never
                // reset at a Foundation/Build/RaceSpecific phase boundary.
                var expected = week.WeekNumber % 2 == 1 ? "KEY_SESSION" : "EASY_SUPPORT";
                Assert.Equal(expected, nonLongRole);
            }
        }
    }

    // ── Calendar: real day assignment succeeds for a Pattern-B (zero-KEY_SESSION) week ──

    [Theory]
    [InlineData("TEN_K__2D__BEGINNER")]
    [InlineData("TEN_K__2D__INTERMEDIATE")]
    public async Task CalendarAssignment_Succeeds_ForZeroKeySessionPatternBWeek(string candidateKey)
    {
        var candidate = await CandidateAsync(candidateKey);
        var skeleton = BuildSkeleton(candidate, 12);
        var dated = MaterializeCalendar(skeleton, candidate);

        Assert.Equal(12, dated.Weeks.Count);
        var patternBWeek = dated.Weeks.Single(w => w.WeekNumber == 2);
        Assert.Equal(2, patternBWeek.SessionSlots.Count);
        Assert.Contains(patternBWeek.SessionSlots, s => s.StructuralRole == "EASY_SUPPORT");
        Assert.Contains(patternBWeek.SessionSlots, s => s.StructuralRole == "LONG_RUN");
        Assert.DoesNotContain(patternBWeek.SessionSlots, s => s.StructuralRole == "KEY_SESSION");

        // Every slot lands on one of the two preferred weekdays, no default substituted.
        Assert.All(dated.Weeks, w => Assert.All(w.SessionSlots, s => Assert.Contains(s.SessionDayOfWeek, PreferredDays)));
    }

    // ── Zero-delta: existing frequencies' structural generation unaffected ──

    [Fact]
    public async Task ExistingFrequencies_RemainByteIdentical_ZeroDelta()
    {
        var beginnerFourDay = await CandidateAsync("TEN_K__4D__BEGINNER");
        Assert.Null(beginnerFourDay.WeeklyPatternRoles);
        Assert.Null(beginnerFourDay.PatternPeriodWeeks);

        var intermediateThreeDay = await CandidateAsync("TEN_K__3D__INTERMEDIATE");
        Assert.Null(intermediateThreeDay.WeeklyPatternRoles);
        Assert.Null(intermediateThreeDay.PatternPeriodWeeks);

        var skeleton = BuildSkeleton(beginnerFourDay, 12);
        Assert.All(skeleton.Weeks, w => Assert.Equal(4, w.SessionSlots.Count));
        Assert.All(skeleton.Weeks, w => Assert.Equal(1, w.SessionSlots.Count(s => s.StructuralRole == "KEY_SESSION")));

        var dated = MaterializeCalendar(skeleton, beginnerFourDay,
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday });
        Assert.Equal(12, dated.Weeks.Count);
    }
}
