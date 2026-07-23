using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.Services;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.PlanGeneration;

/// <summary>
/// Fail-closed safety fix for long-horizon race requests (temporary
/// constraint until preparation + race-core composition is implemented).
/// Exercises <see cref="PlanServices.GeneratePreviewAsync"/> directly with
/// counting fakes standing in for the legacy engine and catalog generator,
/// so these tests prove the guard fires BEFORE either path is ever reached —
/// not merely that the controller returns the right status code while an
/// invalid preview might already have been persisted. See
/// <see cref="RaceHorizonPolicy"/> and
/// <see cref="Sw12LongHorizonFailClosedEndToEndTests"/> for the live HTTP
/// acceptance coverage of the same invariant.
/// </summary>
public sealed class LongHorizonFailClosedTests
{
    private static GeneratePreviewRequest RaceRequest(DateOnly startDate, DateOnly raceDate, GoalDistance goalDistance = GoalDistance.TenK, RunningBackground level = RunningBackground.Intermediate, int daysPerWeek = 4) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = goalDistance,
        Level = level,
        DaysPerWeek = daysPerWeek,
        Unit = DistanceUnit.Km,
        StartDate = startDate,
        RaceDate = raceDate,
        PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = Weekday.Sun,
        TargetFinishTimeSeconds = 3600,
    };

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static PlanServices CreatePlanServices(
        AppDbContext context, CountingPlanGenerationEngine legacyEngine, CountingCatalogPreviewGenerator catalogPreviewGenerator, bool catalogEnabled = true)
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = System.IO.Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);

        var routeDecider = new LivePlanPreviewRoutingService(
            Options.Create(new CatalogLivePilotOptions { Enabled = catalogEnabled }),
            Options.Create(new LocalCatalogAcceptanceOptions()),
            new FakeHostEnvironment("Production"),
            bundleLoader,
            NullLogger<LivePlanPreviewRoutingService>.Instance);

        return new PlanServices(
            context,
            legacyEngine,
            NullLogger<PlanServices>.Instance,
            routeDecider,
            catalogPreviewGenerator,
            new RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPlanConfirmationService(
                context,
                NullLogger<RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPlanConfirmationService>.Instance,
                new GeneratedCatalogPlanPayloadValidator()));
    }

    // ── 1/2/3/4/5: long horizon fails closed, before legacy or catalog, no persistence ──

    [Fact]
    public async Task TwentyWeekHorizon_ThrowsPlanHorizonCompositionRequired_BeforeLegacyOrCatalog_NoPreviewPersisted()
    {
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var request = RaceRequest(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 20).AddDays(20 * 7));

        await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
        Assert.Empty(context.TrainingPlans);
        Assert.Empty(context.TrainingWeeks);
        Assert.Empty(context.TrainingDays);
    }

    [Fact]
    public async Task VerifiedRegressionDates_StartDate20260525_RaceDate20261012_ThrowsPlanHorizonCompositionRequired()
    {
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var request = RaceRequest(new DateOnly(2026, 5, 25), new DateOnly(2026, 10, 12));

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
        Assert.Contains("preparation block", ex.Message);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    public async Task JustAboveMaximum_Throws(int weeks)
    {
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(weeks * 7));

        await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
    }

    [Fact]
    public async Task NonPilotIdentity_LongHorizon_AlsoFailsClosed_NoLegacyFallback()
    {
        // The guard runs before route decision — it must reject a long
        // horizon for ANY race identity, not just the one hardcoded catalog
        // pilot candidate (TEN_K__4D__INTERMEDIATE). This is the exact class
        // of request that previously reached PlaceholderPlanGenerationEngine
        // (the legacy path) with no horizon check at all.
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(20 * 7), goalDistance: GoalDistance.FiveK, level: RunningBackground.Beginner, daysPerWeek: 3);
        request.PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri };
        request.LongRunDay = Weekday.Fri;

        await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
    }

    // ── 6/7/8: regression proof — recognized horizon exceeding generated core length must fail, never silently truncate ──

    [Fact]
    public async Task RegressionGuard_TwentyWeekHorizon_NeverProducesATwelveWeekPreview()
    {
        // The exact shape of the old bug: total recognized horizon (20 weeks)
        // is greater than the standalone core length that would otherwise be
        // silently selected (12 weeks), with no explicit preparation-block
        // metadata anywhere in the request. Generation must fail — a 12-week
        // (or any) preview must never be returned.
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var request = RaceRequest(new DateOnly(2026, 5, 25), new DateOnly(2026, 10, 12));
        var availableWeeks = RaceHorizonPolicy.CalculateAvailableWeeks(request.StartDate, request.RaceDate!.Value);
        Assert.True(availableWeeks > RaceHorizonPolicy.MaximumSupportedStandaloneWeeks);

        await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));
    }

    // ── Exact-12-week horizon: guard does not block, request reaches routing ──

    [Fact]
    public async Task ExactTwelveWeekHorizon_DoesNotThrowAnyHorizonException_ReachesRouting()
    {
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(12 * 7));

        // Reaches either the legacy engine (throws PlanTemplateNotAvailable,
        // no seeded template for this identity) or the catalog generator
        // (throws the counting marker) — either way, NOT a horizon exception.
        var ex = await Record.ExceptionAsync(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.NotNull(ex);
        Assert.IsNotType<PlanHorizonCompositionRequiredException>(ex);
        Assert.IsNotType<PlanCoreHorizonUnsupportedException>(ex);
        Assert.True(legacy.WasCalled || catalog.Calls > 0);
    }

    // ── 8-11 and 13-14 weeks: recognized but not yet safely implemented ────

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task InRangeButNotExactTwelve_ThrowsPlanCoreHorizonUnsupported_BeforeLegacyOrCatalog_NoPreviewPersisted(int weeks)
    {
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(weeks * 7));

        var ex = await Assert.ThrowsAsync<PlanCoreHorizonUnsupportedException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.Contains($"CORE_HORIZON_{weeks}_NOT_IMPLEMENTED", ex.Message);
        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
        Assert.Empty(context.TrainingPlans);
        Assert.Empty(context.TrainingWeeks);
        Assert.Empty(context.TrainingDays);
    }

    [Fact]
    public void RaceHorizonPolicy_EightThroughFourteenExceptTwelve_ClassifyAsCoreLengthRecognizedButNotImplemented()
    {
        foreach (var weeks in new[] { 8, 9, 10, 11, 13, 14 })
        {
            Assert.Equal(RaceHorizonClassification.CoreLengthRecognizedButNotImplemented, RaceHorizonPolicy.Classify(weeks));
        }
        Assert.Equal(RaceHorizonClassification.ExactStandaloneCoreSupported, RaceHorizonPolicy.Classify(12));
    }

    [Fact]
    public void RaceHorizonPolicy_BelowMinimum_ClassifiesAsBelowMinimum_NotCoreHorizonUnsupported()
    {
        // Explicit product decision (per this task's scope): below-minimum
        // horizons are a separate, pre-existing concern this policy does not
        // touch — must never be silently mapped to PLAN_CORE_HORIZON_UNSUPPORTED.
        for (var weeks = 1; weeks < RaceHorizonPolicy.MinimumSupportedStandaloneWeeks; weeks++)
        {
            Assert.Equal(RaceHorizonClassification.BelowMinimum, RaceHorizonPolicy.Classify(weeks));
        }
    }

    // ── K. Routing/service consistency: one canonical decision ──────────────

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(20)]
    public async Task RoutingPolicyAndServiceLayerClassification_Agree(int weeks)
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = System.IO.Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        var candidate = await bundleLoader.LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(weeks * 7));
        var classification = RaceHorizonPolicy.Classify(weeks);

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(
            request, DateOnly.FromDateTime(DateTime.UtcNow),
            candidate.CoreCycle.MinimumWeeks, candidate.CoreCycle.MaximumWeeks, "PUBLISHED", activationEnabled: true);

        switch (classification)
        {
            case RaceHorizonClassification.ExactStandaloneCoreSupported:
                Assert.Equal(LivePlanPreviewRoute.CatalogLive, decision.Route);
                break;
            case RaceHorizonClassification.CoreLengthRecognizedButNotImplemented:
                Assert.Equal(LivePlanPreviewRoute.CatalogCoreLengthNotImplemented, decision.Route);
                break;
            case RaceHorizonClassification.CompositionRequired:
            case RaceHorizonClassification.BelowMinimum:
                Assert.Equal(LivePlanPreviewRoute.CatalogRequestUnsupported, decision.Route);
                break;
        }

        // No layer may ever claim catalog eligibility (CatalogLive) once the
        // canonical classification says otherwise.
        if (classification != RaceHorizonClassification.ExactStandaloneCoreSupported)
        {
            Assert.NotEqual(LivePlanPreviewRoute.CatalogLive, decision.Route);
        }
    }

    // ── RaceHorizonPolicy unit coverage ─────────────────────────────────────

    [Fact]
    public void RaceHorizonPolicy_VerifiedRegressionDates_CalculatesTwentyWeeks()
    {
        var weeks = RaceHorizonPolicy.CalculateAvailableWeeks(new DateOnly(2026, 5, 25), new DateOnly(2026, 10, 12));
        Assert.Equal(20, weeks);
        Assert.True(RaceHorizonPolicy.RequiresLongHorizonComposition(weeks));
    }

    [Fact]
    public void RaceHorizonPolicy_TwelveWeekCase_CalculatesTwelveWeeks_WithinSupportedRange()
    {
        var weeks = RaceHorizonPolicy.CalculateAvailableWeeks(new DateOnly(2026, 7, 20), new DateOnly(2026, 10, 12));
        Assert.Equal(12, weeks);
        Assert.True(RaceHorizonPolicy.IsWithinSupportedStandaloneRange(weeks));
        Assert.False(RaceHorizonPolicy.RequiresLongHorizonComposition(weeks));
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    public void RaceHorizonPolicy_BoundaryValues_ClassifyCorrectly(int weeks, bool requiresComposition)
    {
        Assert.Equal(requiresComposition, RaceHorizonPolicy.RequiresLongHorizonComposition(weeks));
    }

    // ── fakes ────────────────────────────────────────────────────────────────

    private sealed class CountingPlanGenerationEngine : IPlanGenerationEngine
    {
        public bool WasCalled { get; private set; }

        public Task<TemplateSelectionResult> SelectTemplateAsync(GeneratePreviewRequest request, CancellationToken ct = default)
        {
            WasCalled = true;
            throw new PlanTemplateNotAvailableException("No exact legacy template is available for this request.");
        }
    }

    private sealed class CountingCatalogPreviewGenerator : ICatalogPreviewGenerator
    {
        public int Calls { get; private set; }

        public Task<CatalogPreviewSnapshot> GenerateAsync(GeneratePreviewRequest request, DateOnly asOfDate, CancellationToken ct = default)
        {
            Calls++;
            throw new MarkerCatalogInvokedException();
        }
    }

    private sealed class MarkerCatalogInvokedException : Exception { }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "RunningApp.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
