using System.Text.Json;
using System.Text.RegularExpressions;
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
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 4G.5I test-only harness. It is deliberately compiled in the test
/// assembly, has no DI registration, and is never called by a production
/// request path. The Phase 4G.5H orchestrator already directly composes the
/// four preceding dark orchestrators, so this harness delegates to that
/// existing top-level composition rather than reproducing any algorithm.
/// </summary>
internal sealed class DynamicCoreDarkEndToEndHarness
{
    private readonly DynamicCoreCalendarMaterializationOrchestrator _orchestrator;

    public DynamicCoreDarkEndToEndHarness(DynamicCoreCalendarMaterializationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    public async Task<DynamicCoreDarkEndToEndResult> BuildAsync(
        DynamicCoreCalendarMaterializationContext context,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _orchestrator.MaterializeAsync(context, cancellationToken);
        var prescribed = schedule.PrescriptionResult.FinalPrescribedPlan;
        if (!prescribed.ValidationResult.IsValid || schedule.RaceDateAlignment.Outcome != RaceDateAlignmentOutcome.Pass)
        {
            throw new InvalidOperationException("DARK_FINAL_SCHEDULE_INVALID");
        }

        return new(schedule, ProjectPersistableEntities(context, prescribed));
    }

    private static DynamicCorePersistabilityProjection ProjectPersistableEntities(
        DynamicCoreCalendarMaterializationContext context,
        CatalogPrescribedPlan prescribed)
    {
        var now = context.AsOfDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var plan = new TrainingPlan
        {
            Id = Guid.NewGuid(), Status = TrainingPlanStatus.Active,
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate, DaysPerWeek = context.Candidate.DaysPerWeek,
            Unit = DistanceUnit.Km, RaceDate = context.RaceDate,
            StartedAt = context.StartDate.ToDateTime(TimeOnly.MinValue),
            EstimatedEndDate = context.RaceDate.ToDateTime(TimeOnly.MinValue), CreatedAt = now,
            CatalogCandidateKey = context.Candidate.CandidateKey,
            CatalogCandidateVersion = context.Candidate.CandidateVersion,
            CanonicalDistanceFamily = context.Candidate.CanonicalDistanceFamily,
            GenerationSource = "DARK_TEST_ONLY_NOT_PERSISTED",
        };

        var weeks = new List<TrainingWeek>();
        var days = new List<TrainingDay>();
        foreach (var prescribedWeek in prescribed.Weeks.OrderBy(w => w.WeekNumber))
        {
            var weekId = Guid.NewGuid();
            var week = new TrainingWeek
            {
                Id = weekId, PlanId = plan.Id, Plan = plan,
                WeekNumber = prescribedWeek.WeekNumber,
                PlannedVolumeKm = prescribedWeek.PlannedWeeklyVolumeKm,
                StartDate = context.StartDate.AddDays((prescribedWeek.WeekNumber - 1) * 7).ToDateTime(TimeOnly.MinValue),
                CreatedAt = now, CatalogPhaseKey = prescribedWeek.PhaseKey,
            };
            weeks.Add(week);
            plan.Weeks.Add(week);

            foreach (var session in prescribedWeek.Sessions.OrderBy(s => s.Date))
            {
                var day = new TrainingDay
                {
                    Id = Guid.NewGuid(), PlanId = plan.Id, WeekId = weekId, Plan = plan, Week = week,
                    Date = session.Date.ToDateTime(TimeOnly.MinValue), DayType = TrainingDayType.Easy,
                    Title = session.WorkoutDefinitionKey, Description = session.EffortGuidanceOrFallback(),
                    PlannedDistanceKm = session.PlannedDistanceKm,
                    PlannedDurationMin = (int)Math.Round((session.PlannedDuration ?? session.EstimatedDuration ?? TimeSpan.Zero).TotalMinutes),
                    IsLongRun = session.StructuralRole == "LONG_RUN", CreatedAt = now, UpdatedAt = now,
                    CatalogPhaseKey = session.PhaseKey,
                    CatalogProgressionStageKey = session.ProgressionStageKey,
                    CatalogWorkoutDefinitionKey = session.WorkoutDefinitionKey,
                    CatalogWorkoutDefinitionVersion = session.WorkoutDefinitionVersion,
                    CatalogStructuralRole = session.StructuralRole,
                    CatalogPrescriptionJson = JsonSerializer.Serialize(session.Prescription),
                    CatalogPrescriptionSchemaVersion = 1,
                    GenerationSource = "DARK_TEST_ONLY_NOT_PERSISTED",
                };
                days.Add(day);
                week.Days.Add(day);
            }
        }

        return new(plan, weeks, days);
    }
}

internal static class CatalogPrescribedSessionDarkProjectionExtensions
{
    public static string EffortGuidanceOrFallback(this CatalogPrescribedSession session) =>
        string.IsNullOrWhiteSpace(session.Prescription.EffortGuidance) ? session.WorkoutDefinitionKey : session.Prescription.EffortGuidance;
}

internal sealed record DynamicCorePersistabilityProjection(
    TrainingPlan Plan,
    IReadOnlyList<TrainingWeek> Weeks,
    IReadOnlyList<TrainingDay> Days);

internal sealed record DynamicCoreDarkEndToEndResult(
    DynamicCoreCalendarMaterializationResult Schedule,
    DynamicCorePersistabilityProjection Persistability);

public sealed class DynamicCoreDarkEndToEndHarnessTests
{
    private static string CatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions CatalogOptions() => new() { CatalogRootPath = CatalogRoot() };

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(CatalogOptions()), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    private static DynamicCoreDarkEndToEndHarness Harness() => new(
        new DynamicCoreCalendarMaterializationOrchestrator(
            new DynamicCoreSessionPrescriptionOrchestrator(
                new DynamicCoreVolumeAndLongRunOrchestrator(
                    new DynamicCoreWorkoutBindingOrchestrator(
                        new DynamicCoreWeekSkeletonOrchestrator(
                            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                        new CatalogWorkoutProgressionLoader(Options.Create(CatalogOptions())),
                        new ProgressionStageAllocator(), new GeneratedCatalogStageScheduleValidator(),
                        new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator(),
                        new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                    new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
                new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer())));

    private static DynamicCoreCalendarMaterializationContext Context(PlanCatalogCandidateSummary candidate, int weeks)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(weeks * 7 - 1);
        var options = Options.Create(CatalogOptions());
        return new()
        {
            Candidate = candidate, TargetWeekCount = weeks, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday },
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults = new[]
            {
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "REALISTIC", "WITHIN_REALISTIC_BAND"),
            },
            PreviewRequest = new GeneratePreviewRequest
            {
                GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
                DaysPerWeek = 4, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = 3000,
                PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun }, LongRunDay = Weekday.Sun,
                RecentWeeklyVolumeKm = 24, RecentLongestRunKm = 9, RecentRunsPerWeek = 4,
                RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = start.AddDays(-21) },
            },
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.TenK, GoalDistanceKm = 10, StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = 3000, DaysPerWeek = 4, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(options),
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(options),
        };
    }

    public static IEnumerable<object[]> Horizons() => Enumerable.Range(8, 7).Select(w => new object[] { w });

    [Theory]
    [MemberData(nameof(Horizons))]
    public async Task BuildAsync_AllEightToFourteenHorizons_AreCompleteValidAndPersistable(int weeks)
    {
        var result = await Harness().BuildAsync(Context(await CandidateAsync(), weeks));
        var prescribed = result.Schedule.PrescriptionResult.FinalPrescribedPlan;
        var dated = result.Schedule.PrescriptionResult.VolumeResult.BindingResult.DatedSkeleton;

        Assert.True(prescribed.ValidationResult.IsValid);
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.Schedule.RaceDateAlignment.Outcome);
        Assert.Equal(weeks, prescribed.Weeks.Count);
        Assert.Equal(weeks * 4, prescribed.Sessions.Count);
        Assert.All(prescribed.Sessions, session =>
        {
            Assert.False(string.IsNullOrWhiteSpace(session.WorkoutDefinitionKey));
            Assert.True(session.ValidationResult.IsValid);
            Assert.True(session.PlannedDistanceKm > 0);
        });
        Assert.All(result.Schedule.RaceDateAlignment.Checks, check => Assert.True(check.Passed, check.Detail));
        Assert.Equal(weeks, dated.Weeks.Count);

        Assert.Equal(weeks, result.Persistability.Weeks.Count);
        Assert.Equal(weeks * 4, result.Persistability.Days.Count);
        Assert.Equal(result.Persistability.Weeks.Count, result.Persistability.Plan.Weeks.Count);
        Assert.All(result.Persistability.Days, day =>
        {
            Assert.NotNull(day.CatalogWorkoutDefinitionKey);
            Assert.NotNull(day.CatalogPhaseKey);
            Assert.NotNull(day.CatalogPrescriptionJson);
        });
    }

    [Fact]
    public async Task BuildAsync_InvalidDarkInput_FailsClosed()
    {
        var candidate = await CandidateAsync();
        await Assert.ThrowsAsync<DynamicCoreWeekSkeletonInfeasibleException>(() => Harness().BuildAsync(Context(candidate, 7)));
    }

    [Fact]
    public async Task BuildAsync_IsSideEffectFreeAndCreatesNoPersistenceMutation()
    {
        var result = await Harness().BuildAsync(Context(await CandidateAsync(), 12));
        Assert.Equal("DARK_TEST_ONLY_NOT_PERSISTED", result.Persistability.Plan.GenerationSource);
        var dependencies = typeof(DynamicCoreDarkEndToEndHarness)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .ToArray();
        Assert.Equal(new[] { typeof(DynamicCoreCalendarMaterializationOrchestrator) }, dependencies);
    }

    [Fact]
    public async Task TwelveWeekStableProjection_IsValueIdenticalAcrossIndependentCompositions()
    {
        var candidate = await CandidateAsync();
        var first = await Harness().BuildAsync(Context(candidate, 12));
        var second = await Harness().BuildAsync(Context(candidate, 12));

        static string Stable(DynamicCoreDarkEndToEndResult result) => JsonSerializer.Serialize(
            result.Schedule.PrescriptionResult.FinalPrescribedPlan.Weeks.Select(w => new
            {
                w.WeekNumber, w.PhaseKey, w.PlannedWeeklyVolumeKm,
                Sessions = w.Sessions.Select(s => new { s.Date, s.StructuralRole, s.WorkoutDefinitionKey, s.PlannedDistanceKm }),
            }));

        Assert.Equal(Stable(first), Stable(second));
        Assert.Equal(12, first.Persistability.Weeks.Count);
        Assert.Equal(48, first.Persistability.Days.Count);
    }

    [Fact]
    public void Reachability_OnlyCalendarOrchestratorHasTheApprovedLivePreviewCaller()
    {
        var allowedDarkCallerBySymbol = new Dictionary<string, string?>
        {
            ["DynamicCoreWeekSkeletonOrchestrator"] = "DynamicCoreWorkoutBindingOrchestrator.cs",
            ["DynamicCoreWorkoutBindingOrchestrator"] = "DynamicCoreVolumeAndLongRunOrchestrator.cs",
            ["DynamicCoreVolumeAndLongRunOrchestrator"] = "DynamicCoreSessionPrescriptionOrchestrator.cs",
            ["DynamicCoreSessionPrescriptionOrchestrator"] = "DynamicCoreCalendarMaterializationOrchestrator.cs",
            ["DynamicCoreCalendarMaterializationOrchestrator"] = "CatalogPreviewGenerator.cs",
            ["DynamicCoreDarkEndToEndHarness"] = null,
        };
        var production = ProductionFiles().ToArray();
        foreach (var (symbol, allowedCaller) in allowedDarkCallerBySymbol)
        {
            var definingFile = symbol + ".cs";
            var unexpected = production
                .Where(path => !Path.GetFileName(path).Equals(definingFile, StringComparison.OrdinalIgnoreCase))
                .Where(path => allowedCaller is null || !Path.GetFileName(path).Equals(allowedCaller, StringComparison.OrdinalIgnoreCase))
                .Where(path => !Path.GetFileName(path).Equals("CatalogPreviewGenerator.cs", StringComparison.OrdinalIgnoreCase))
                // Phase 4G.6A.4H's sole production-owned dark composition boundary.
                // API, service, preview-routing, persistence, and every other production path remain scanned.
                .Where(path => !path.Contains(Path.Combine("Schedule", "PreparationRunwayOrchestration"), StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains(Path.Combine("Schedule", "LongHorizon"), StringComparison.OrdinalIgnoreCase))
                .Where(path => Regex.IsMatch(StripCommentsAndStrings(File.ReadAllText(path)), $@"\b{symbol}\b"))
                .ToArray();
            Assert.Empty(unexpected);
        }

        var preview = File.ReadAllText(Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "PreviewRouting", "CatalogPreviewGenerator.cs"));
        var planServices = File.ReadAllText(Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "Services", "PlanServices.cs"));
        Assert.DoesNotContain("DynamicCoreDarkEndToEndHarness", StripCommentsAndStrings(preview));
        Assert.All(allowedDarkCallerBySymbol.Keys.Where(symbol => symbol != "DynamicCoreDarkEndToEndHarness"), symbol =>
        {
            Assert.Contains(symbol, StripCommentsAndStrings(preview));
            Assert.DoesNotContain(symbol, StripCommentsAndStrings(planServices));
        });
    }

    [Fact]
    public void Reachability_OtherEightVerifierRestrictionsRemainStrictAndOptInIsInert()
    {
        foreach (var verifier in new[]
        {
            "PhaseConstraintVerifier", "RaceSpecificCapacityVerifier", "StageReachabilityVerifier",
            "WorkoutExposureVerifier", "GoalPaceReachabilityVerifier", "ReadinessEligibilityVerifier",
            "VolumeProgressionVerifier", "LongRunProgressionVerifier",
        })
        {
            DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(verifier);
        }

        DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(
            "RaceDateAlignmentVerifier", new[] { "DynamicCoreCalendarMaterializationOrchestrator.cs" });
    }

    private static IEnumerable<string> ProductionFiles()
    {
        var repo = TestPlanServicesFactory.RepoRoot();
        return new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repo, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }

    private static string StripCommentsAndStrings(string source) =>
        Regex.Replace(Regex.Replace(source, @"/\*.*?\*/|//.*?$", "", RegexOptions.Singleline | RegexOptions.Multiline), "\"(?:\\\\.|[^\"\\\\])*\"", "");

}
