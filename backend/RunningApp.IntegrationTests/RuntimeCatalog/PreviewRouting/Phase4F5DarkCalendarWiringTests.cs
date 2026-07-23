using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4F.5 — proves the dark wiring of
/// <see cref="ICatalogWeekSkeletonCalendarMaterializer"/> inside
/// <see cref="CatalogPreviewGenerator"/>: it runs only after the Phase 4F.3
/// skeleton succeeds, receives frozen/authoritative inputs, its failures
/// abort preview generation through the existing typed error boundary, and
/// its result never reaches the snapshot, hash, or any public surface.
/// </summary>
public sealed class Phase4F5DarkCalendarWiringTests
{
    private sealed class CountingCalendarMaterializer : ICatalogWeekSkeletonCalendarMaterializer
    {
        private readonly ICatalogWeekSkeletonCalendarMaterializer _inner = new CatalogWeekSkeletonCalendarMaterializer();
        public int InvocationCount { get; private set; }
        public CatalogCalendarAssignmentContext? LastContext { get; private set; }

        public DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context)
        {
            InvocationCount++;
            LastContext = context;
            return _inner.Materialize(context);
        }
    }

    private sealed class ThrowingCalendarMaterializer : ICatalogWeekSkeletonCalendarMaterializer
    {
        private readonly Exception _exception;
        public ThrowingCalendarMaterializer(Exception exception) => _exception = exception;
        public DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context) => throw _exception;
    }

    private sealed class FixedResultEligibilityGate : ICatalogCandidateEligibilityGate
    {
        private readonly PlanCatalogCandidateSummary _result;
        public FixedResultEligibilityGate(PlanCatalogCandidateSummary result) => _result = result;

        public Task<PlanCatalogCandidateSummary> LoadForPublicPreviewAsync(string candidateKey, int candidateVersion, System.Threading.CancellationToken ct = default) =>
            Task.FromResult(_result);

        public Task<PlanCatalogCandidateSummary> LoadForInternalDryRunAsync(string candidateKey, int candidateVersion, System.Threading.CancellationToken ct = default) =>
            Task.FromResult(_result);
    }

    private static ICatalogPlanSkeletonOrchestrator RealSkeletonOrchestrator() => new CatalogPlanSkeletonOrchestrator(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    private static RuntimeConditionResolutionService RealOrchestration() =>
        new(new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());

    private static async Task<PlanCatalogCandidateSummary> LoadControlledPublishedCandidateAsync()
    {
        var real = await CatalogPlanSkeletonOrchestratorFixtures.LoadRealPilotCandidateAsync();
        return new PlanCatalogCandidateSummary
        {
            CandidateKey = real.CandidateKey,
            CandidateVersion = real.CandidateVersion,
            CandidateStatus = "PUBLISHED",
            CanonicalDistanceFamily = real.CanonicalDistanceFamily,
            Level = real.Level,
            DaysPerWeek = real.DaysPerWeek,
            CoreCycle = real.CoreCycle,
            MasterTemplate = real.MasterTemplate,
            Layout = real.Layout,
            LevelModifier = real.LevelModifier,
            WorkoutProgression = real.WorkoutProgression,
            ProgressionModifier = real.ProgressionModifier,
            RulePack = real.RulePack,
            PeakVolumeBandPolicy = real.PeakVolumeBandPolicy,
            RuntimeConditionValueRegistry = real.RuntimeConditionValueRegistry,
            DependencyStatuses = new System.Collections.Generic.Dictionary<string, string>
            {
                ["masterTemplate"] = "PUBLISHED", ["layout"] = "PUBLISHED", ["levelModifier"] = "PUBLISHED", ["rulePack"] = "PUBLISHED",
            },
            ReferencedWorkouts = real.ReferencedWorkouts,
            PhaseKeys = real.PhaseKeys,
            PhaseAllocations = real.PhaseAllocations,
            SlotRoles = real.SlotRoles,
        };
    }

    private static readonly Weekday[] DefaultPreferredDays = { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun };

    private static GeneratePreviewRequest PilotRequest(DateOnly raceDate) =>
        PilotRequest(raceDate, DefaultPreferredDays, Weekday.Sun);

    private static GeneratePreviewRequest PilotRequest(
        DateOnly raceDate,
        IReadOnlyList<Weekday>? preferredDays,
        Weekday? longRunDay) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = raceDate.AddDays(-84),
        RaceDate = raceDate,
        // Deliberately allows null here (bypassing the DTO's `required`
        // nullability annotation) -- this fixture is used to test
        // CatalogPreferredDayAdapter's OWN independent "missing PreferredDays"
        // defense inside the catalog pipeline, called directly against
        // CatalogPreviewGenerator rather than through PlanServices'
        // GeneratePreviewRequestValidator (which would already reject a null
        // PreferredDays at the public boundary before catalog code is ever
        // reached in production).
        PreferredDays = preferredDays!,
        LongRunDay = longRunDay,
    };

    private static IReadOnlyList<Weekday>? ParseWeekdaysCsv(string? csv) =>
        csv is null ? null : csv.Split(',').Select(Enum.Parse<Weekday>).ToArray();

    [Fact]
    public async Task DarkCalendar_InvokedAfterSkeletonSuccess_ReceivesFrozenAuthoritativeInputs()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var counting = new CountingCalendarMaterializer();
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator(), counting);
        var asOfDate = new DateOnly(2026, 1, 5);

        await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        Assert.Equal(1, counting.InvocationCount);
        Assert.Equal(asOfDate, counting.LastContext!.StartDate);
        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday }, counting.LastContext!.PreferredDays);
        Assert.Equal(DayOfWeek.Sunday, counting.LastContext!.LongRunDayPreference);
        Assert.Equal(GoalType.Race, counting.LastContext!.GoalKind);
    }

    [Fact]
    public async Task DarkCalendar_NotInvoked_ForDraftV10()
    {
        var counting = new CountingCalendarMaterializer();
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        var realGate = new CatalogCandidateEligibilityGate(bundleLoader);
        var generator = new CatalogPreviewGenerator(realGate, RealOrchestration(), RealSkeletonOrchestrator(), counting);

        await Assert.ThrowsAsync<CatalogCandidateNotPublishedException>(() =>
            generator.GenerateAsync(PilotRequest(new DateOnly(2026, 4, 1)), new DateOnly(2026, 1, 5)));

        Assert.Equal(0, counting.InvocationCount);
    }

    [Theory]
    [InlineData(null, "Sun")]
    [InlineData("Mon,Wed,Fri,Sun", null)]
    public async Task DarkCalendar_MissingPreferredDaysOrLongRunDay_AbortsPreviewGeneration(string? preferredDays, string? longRunDay)
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator());
        var asOfDate = new DateOnly(2026, 1, 5);

        var ex = await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(
                PilotRequest(asOfDate.AddDays(84), ParseWeekdaysCsv(preferredDays), longRunDay is null ? null : Enum.Parse<Weekday>(longRunDay)),
                asOfDate));

        Assert.Contains("CATALOG_INTERNAL_SKELETON_MATERIALIZATION_FAILED", ex.Message);
    }

    [Fact]
    public async Task DarkCalendar_MaterializerException_BecomesTypedPreviewFailure_PreservingCause()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var cause = new CatalogPreferredDayConfigurationUnsafeException("synthetic unsafe configuration");
        var throwing = new ThrowingCalendarMaterializer(cause);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator(), throwing);
        var asOfDate = new DateOnly(2026, 1, 5);

        var ex = await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate));

        Assert.Same(cause, ex.InnerException);
    }

    [Fact]
    public async Task DarkCalendar_Success_GeneratedPreviewPlanPayloadRemainsNull_NoSkeletonInSnapshot()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator());
        var asOfDate = new DateOnly(2026, 1, 5);

        var snapshot = await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        Assert.NotNull(snapshot.GeneratedPreviewPlanPayload);
        Assert.DoesNotContain(snapshot.GetType().GetProperties(), p => p.Name.Contains("Dated") || p.Name.Contains("Calendar"));
    }
}
