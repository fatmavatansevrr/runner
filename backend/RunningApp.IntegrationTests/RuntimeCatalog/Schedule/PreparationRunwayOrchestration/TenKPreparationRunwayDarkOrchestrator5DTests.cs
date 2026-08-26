using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

/// <summary>
/// Phase 10K-FREQ.6D.7 — dark end-to-end proof that the generalized
/// Preparation Runway pipeline produces the FREQ.6D.6-approved Intermediate
/// 5D shape (1 KEY_SESSION + 3 EASY_SUPPORT + 1 LONG_RUN every full Runway
/// week, 5 sessions/week, dual KEY only from real Core Week 1 onward) across
/// all six 15-20 week horizons, reusing the exact same
/// <see cref="TenKPreparationRunwayDarkOrchestrator"/> the 4D pilot uses --
/// no separate 5D orchestrator exists.
/// </summary>
public sealed class TenKPreparationRunwayDarkOrchestrator5DTests
{
    private const string RealPublishedBundleReleaseVersion = "1.1.0";
    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static readonly Task<PlanCatalogCandidateSummary> CandidateTask = LoadCandidateAsync();
    private static readonly IReadOnlyList<DayOfWeek> MonTueThuFriSun =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday];

    public static IEnumerable<object[]> HorizonProfileMatrix =>
        Enumerable.Range(15, 6).SelectMany(weeks => new[] { "READY", "NOT_READY" }
            .Select(readiness => new object[] { weeks, readiness }));

    [Theory]
    [MemberData(nameof(HorizonProfileMatrix))]
    public async Task OrchestrateAsync_All15To20Horizons_BothProfiles_ExactApprovedFiveDayShape(int totalWeeks, string readinessValue)
    {
        var request = await RequestAsync(totalWeeks, readinessValue, weekly: readinessValue == "READY" ? 24d : 18d, longest: readinessValue == "READY" ? 9d : 6d);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, $"{result.Failure?.Stage}/{result.Failure?.Code}: {result.Failure?.Reason}");
        Assert.Equal("TEN_K__5D__INTERMEDIATE", result.CoreResult!.PrescriptionResult.FinalPrescribedPlan.CandidateKey);
        Assert.Equal(totalWeeks, result.HorizonDecision!.AvailableFullWeeks);
        Assert.Equal(totalWeeks - 12, result.StructuralRunway!.Weeks!.Count);
        Assert.Equal(12, result.CoreResult.PrescriptionResult.FinalPrescribedPlan.Weeks.Count);
        Assert.Equal(totalWeeks, result.CalendarComposition!.OrderedCombinedWeeks!.Count);

        // FREQ.6D.6: every full Runway week is exactly 1 KEY + 3 EASY + 1 LONG (5 sessions).
        Assert.All(result.StructuralRunway.Weeks, week =>
        {
            Assert.Equal(5, week.OrderedWorkoutSlots.Count);
            Assert.Equal(1, week.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.KeySession));
            Assert.Equal(3, week.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.EasySupport));
            Assert.Equal(1, week.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.LongRun));
        });

        // Second KEY appears only at real Core Week 1 -- never gradually inside Runway.
        var coreWeekOne = result.CoreResult.PrescriptionResult.FinalPrescribedPlan.Weeks.OrderBy(w => w.WeekNumber).First();
        Assert.Equal(5, coreWeekOne.Sessions.Count);
        Assert.Equal(2, coreWeekOne.Sessions.Count(s => s.StructuralRole == "KEY_SESSION"));
        Assert.Equal(2, coreWeekOne.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT"));
        Assert.Equal(1, coreWeekOne.Sessions.Count(s => s.StructuralRole == "LONG_RUN"));

        // 5 sessions every week across the whole combined horizon.
        Assert.Equal(totalWeeks * 5, result.FinalInvariants!.TotalSessions);
        Assert.True(result.FinalInvariants.IsValid, string.Join(",", result.FinalInvariants.Findings));
        Assert.True(result.FinalInvariants.NumericContinuity);
        Assert.True(result.FinalInvariants.PaceContinuity);
        Assert.True(result.FinalInvariants.ProvenanceComplete);

        // Allocation/rounding: every Runway week's slot sum equals its planned weekly total.
        Assert.All(result.NumericRunway!.PrescribedWeeks!, w =>
            Assert.True(Math.Abs(w.OrderedSlots.Sum(s => s.PlannedDistanceKm) - w.PlannedWeeklyVolumeKm) <= 0.001d));
        Assert.All(result.NumericRunway.PrescribedWeeks.SelectMany(w => w.OrderedSlots),
            s => Assert.True(s.PlannedDistanceKm > 0));

        // Core-entry continuity per FREQ.6D.6: total weekly volume and long-run distance match
        // exactly at the boundary; per-slot KEY/EASY equality does not apply (1K+3E vs 2K+2E).
        var continuity = result.NumericRunway.ContinuityAnalysis!;
        Assert.True(Math.Abs(continuity.WeeklyVolumeChangeKm) <= continuity.ToleranceKm);
        Assert.True(Math.Abs(continuity.LongRunChangeKm) <= continuity.ToleranceKm);
    }

    // Phase 10K-FREQ.6D.7 originally exercised only the missing-evidence
    // case here, resolving to the (accidental, 4D-scoped) 16km fallback --
    // see the by-then-superseded comment this replaces. Phase 10K-FREQ.6D.9
    // reconciled the FREQ.6C numeric authority for Intermediate x5D
    // (missing=26.0km, explicit-zero=19.5km, distinct from the 4D
    // defaults) and Phase 10K-FREQ.6D.10 wired it into both
    // CatalogVolumeAndLongRunPlanner (Core) and
    // TenKPreparationRunwayNumericPolicyFactory (Runway), so missing
    // readiness now resolves the dedicated 26.0km value here.
    [Theory]
    [InlineData(null, null, 26d)]
    public async Task StartingWeeklyVolume_Missing_ResolvesViaExistingCanonicalAuthority(
        double? weekly, double? longest, double expectedFirstWeekKm)
    {
        var readinessValue = "NOT_READY_MISSING";
        var request = await RequestAsync(15, readinessValue, weekly, longest);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, result.Failure?.Reason);
        var firstRunwayWeek = result.NumericRunway!.PrescribedWeeks!.OrderBy(w => w.StructuralWeek.RunwayWeekNumber).First();
        Assert.Equal(expectedFirstWeekKm, firstRunwayWeek.PlannedWeeklyVolumeKm);
    }

    [Fact]
    public async Task StartingWeeklyVolume_PositiveObserved_UsesProvidedValueNotDefault()
    {
        var request = await RequestAsync(15, "READY", weekly: 20d, longest: 7d);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, result.Failure?.Reason);
        var firstRunwayWeek = result.NumericRunway!.PrescribedWeeks!.OrderBy(w => w.StructuralWeek.RunwayWeekNumber).First();
        Assert.Equal(20d, firstRunwayWeek.PlannedWeeklyVolumeKm);
    }

    [Fact]
    public async Task FourDayCandidate_StillProducesExactLegacyFourSlotShape_NoRegressionFromSharedEngineChanges()
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        var candidate = await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
        var preferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday };
        var request = await BuildRequestAsync(candidate, 4, preferredDays, 15, "READY", 24d, 9d);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, result.Failure?.Reason);
        Assert.All(result.StructuralRunway!.Weeks!, week =>
        {
            Assert.Equal(4, week.OrderedWorkoutSlots.Count);
            Assert.Equal(1, week.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.KeySession));
            Assert.Equal(2, week.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.EasySupport));
            Assert.Equal(1, week.OrderedWorkoutSlots.Count(s => s.SlotRole == PreparationRunwaySlotRole.LongRun));
        });
        Assert.Equal(15 * 4, result.FinalInvariants!.TotalSessions);
    }

    private static TenKPreparationRunwayDarkOrchestrator Orchestrator() =>
        TenKPreparationRunwayDarkOrchestratorFactory.Create(new PlanCatalogOptions
        {
            CatalogRootPath = CatalogRoot,
            PublishedBundleReleaseVersion = RealPublishedBundleReleaseVersion,
        });

    private static async Task<PlanCatalogCandidateSummary> LoadCandidateAsync()
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.FiveDayCandidateKey, V1CatalogPilotIdentityPolicy.FiveDayCandidateVersion);
    }

    private static async Task<TenKPreparationRunwayDarkOrchestrationRequest> RequestAsync(
        int totalWeeks, string readinessValue, double? weekly, double? longest)
    {
        var candidate = await CandidateTask;
        return await BuildRequestAsync(candidate, 5, MonTueThuFriSun, totalWeeks, readinessValue, weekly, longest);
    }

    private static async Task<TenKPreparationRunwayDarkOrchestrationRequest> BuildRequestAsync(
        PlanCatalogCandidateSummary candidate, int daysPerWeek, IReadOnlyList<DayOfWeek> preferredDays,
        int totalWeeks, string readinessValue, double? weekly, double? longest)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(totalWeeks * 7);
        var longRunDay = DayOfWeek.Sunday;
        var noBase = readinessValue == "NOT_READY_NO_BASE";

        int? targetSeconds = 3000;
        var weekdays = preferredDays.Select(ToWeekday).ToArray();
        var preview = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
            DaysPerWeek = daysPerWeek, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = targetSeconds, TargetFinishTimeSource = null,
            PreferredDays = weekdays, LongRunDay = ToWeekday(longRunDay),
            RecentWeeklyVolumeKm = weekly, RecentLongestRunKm = longest, RecentRunsPerWeek = noBase ? 0 : daysPerWeek,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = start.AddDays(-21) },
        };
        var resolver = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK, GoalDistanceKm = 10, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = targetSeconds, TargetFinishTimeSource = null,
            DaysPerWeek = daysPerWeek, PreferredDays = weekdays, LongRunDay = ToWeekday(longRunDay), Level = RunningBackground.Intermediate,
            RecentWeeklyVolumeKm = weekly, RecentLongestRunKm = longest, RecentRunsPerWeek = noBase ? 0 : daysPerWeek,
            RecentRaceDistanceKm = 10,
            RecentRaceFinishTimeSeconds = 3000,
            RecentRaceDate = start.AddDays(-21),
        };
        var readiness = RuntimeConditionResolutionResult.Evaluated(
            CoreEntryReadinessResolver.ConditionTypeValue,
            readinessValue.StartsWith("NOT_READY", StringComparison.Ordinal) ? "NOT_READY" : readinessValue,
            readinessValue == "READY" ? "CORE_ENTRY_READY" : "CORE_ENTRY_NOT_READY");
        var conditions = new List<RuntimeConditionResolutionResult>
        {
            readiness,
            RuntimeConditionResolutionResult.Evaluated(PaceSourceResolver.ConditionTypeValue, "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
            RuntimeConditionResolutionResult.Evaluated(GoalFeasibilityResolver.ConditionTypeValue, "REALISTIC", "WITHIN_REALISTIC_BAND"),
        };
        return new TenKPreparationRunwayDarkOrchestrationRequest(
            candidate, start, race, start, preferredDays, longRunDay, readiness, conditions,
            preview, resolver, PreparationRunwayQuantityUnit.Kilometers);
    }

    private static Weekday ToWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekday.Mon,
        DayOfWeek.Tuesday => Weekday.Tue,
        DayOfWeek.Wednesday => Weekday.Wed,
        DayOfWeek.Thursday => Weekday.Thu,
        DayOfWeek.Friday => Weekday.Fri,
        DayOfWeek.Saturday => Weekday.Sat,
        DayOfWeek.Sunday => Weekday.Sun,
        _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };
}
