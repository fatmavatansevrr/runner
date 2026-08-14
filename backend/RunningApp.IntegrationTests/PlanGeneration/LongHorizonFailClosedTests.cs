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
        AppDbContext context, CountingPlanGenerationEngine legacyEngine, CountingCatalogPreviewGenerator catalogPreviewGenerator,
        bool catalogEnabled = true, bool preparationRunwayPilotActivationEnabled = true)
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
                new GeneratedCatalogPlanPayloadValidator()),
            Options.Create(new PlanCatalogOptions { CatalogRootPath = System.IO.Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            Options.Create(new RunningApp.Application.RuntimeCatalog.PreviewRouting.PreparationRunwayPilotActivationOptions { Enabled = preparationRunwayPilotActivationEnabled }));
    }

    // ── 1/2/3/4/5: long horizon fails closed, before legacy or catalog, no persistence ──

    [Fact]
    public async Task TwentyOneWeekHorizon_ThrowsPlanHorizonCompositionRequired_BeforeLegacyOrCatalog_NoPreviewPersisted()
    {
        // Backend Integration Phase 4G.6B: 20 weeks (pilot scope) is now
        // ACTIVATED via the Preparation Runway route -- see
        // PilotScope_TwentyWeekHorizon_RoutesToPreparationRunwayPreview_NotCompositionRequired
        // below. 21+ weeks remains unsupported and unchanged; this test was
        // retargeted from 20 to 21 weeks to keep testing the still-genuine
        // fail-closed invariant.
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var request = RaceRequest(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 20).AddDays(21 * 7));

        await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
        Assert.Empty(context.TrainingPlans);
        Assert.Empty(context.TrainingWeeks);
        Assert.Empty(context.TrainingDays);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    public async Task PilotScope_FifteenToTwentyWeekHorizon_RoutesToPreparationRunwayPreview_NotCompositionRequired(int weeks)
    {
        // Backend Integration Phase 4G.6B: the exact pilot-scope 15-20 week
        // route now invokes ICatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync
        // (proven here via the counting fake, which throws a distinct marker
        // exception) instead of the old unconditional PlanHorizonCompositionRequiredException.
        // No PlanPreview row is created when the underlying generator throws.
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(weeks * 7));

        var ex = await Record.ExceptionAsync(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.NotNull(ex);
        Assert.IsNotType<PlanHorizonCompositionRequiredException>(ex);
        Assert.False(legacy.WasCalled);
        Assert.Equal(1, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
    }

    // ── Phase 4G.6B.1: activation gate disabled/enabled rollback behavior ────

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    public async Task DisabledGate_ExactPilotFifteenToTwentyWeeks_RestoresPreActivationUnsupportedBehavior(int weeks)
    {
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog, preparationRunwayPilotActivationEnabled: false);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(weeks * 7));

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.Contains("preparation block", ex.Message);
        Assert.False(legacy.WasCalled);
        // Disabled gate must prevent the orchestrator (via the generator
        // seam) from ever being invoked -- not merely map its result to a
        // different status code.
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
        Assert.Empty(context.TrainingPlans);
        Assert.Empty(context.TrainingWeeks);
        Assert.Empty(context.TrainingDays);
    }

    [Fact]
    public async Task DisabledGate_EightToFourteenWeeks_Unaffected()
    {
        // The gate controls ONLY the 15-20 week pilot-scope branch -- 8-14
        // week requests never reach it at all, disabled or not.
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog, preparationRunwayPilotActivationEnabled: false);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(12 * 7));

        var ex = await Record.ExceptionAsync(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));

        // 8-14 week requests never reach the Preparation Runway gate at all
        // (it's a branch only inside the CompositionRequired classification) --
        // this must reach whichever of the normal legacy/catalog routes this
        // identity/candidate-status combination resolves to, never the
        // PlanHorizonCompositionRequiredException the disabled gate produces
        // for 15-20 week requests above.
        Assert.NotNull(ex);
        Assert.IsNotType<PlanHorizonCompositionRequiredException>(ex);
        Assert.True(legacy.WasCalled || catalog.Calls > 0);
    }

    [Fact]
    public async Task EnabledVsDisabledGate_OtherCandidateIdentity_UnaffectedEitherWay()
    {
        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(17 * 7), goalDistance: GoalDistance.FiveK, level: RunningBackground.Beginner, daysPerWeek: 3);
        request.PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri };
        request.LongRunDay = Weekday.Fri;

        foreach (var enabled in new[] { true, false })
        {
            await using var context = NewContext();
            var legacy = new CountingPlanGenerationEngine();
            var catalog = new CountingCatalogPreviewGenerator();
            var service = CreatePlanServices(context, legacy, catalog, preparationRunwayPilotActivationEnabled: enabled);

            await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
                service.GeneratePreviewAsync(Guid.NewGuid(), request));

            Assert.Equal(0, catalog.Calls);
        }
    }

    [Theory]
    [InlineData(52, true)]
    [InlineData(52, false)]
    [InlineData(53, true)]
    [InlineData(53, false)]
    public async Task FiftyTwoAndAbove_GateStateDoesNotAlterResult_ExactPilotIdentity(int weeks, bool gateEnabled)
    {
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog, preparationRunwayPilotActivationEnabled: gateEnabled);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(weeks * 7));

        await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public async Task IneligiblePartialDayBoundaries_NeverInvokeCatalogCompositionOrPersistTrainingGraph()
    {
        var startDate = new DateOnly(2026, 7, 20);
        foreach (var (elapsedDays, expectedException) in new[]
        {
            (55, typeof(CatalogLivePilotRequestUnsupportedException)), // 7w6d
            (99, typeof(PlanHorizonCompositionRequiredException)),     // 14w1d
            (104, typeof(PlanHorizonCompositionRequiredException)),    // 14w6d
            // 105 (15w0d) removed: Phase 4G.6B activates this exact pilot-scope
            // horizon via the Preparation Runway route -- see
            // PilotScope_FifteenToTwentyWeekHorizon_RoutesToPreparationRunwayPreview_NotCompositionRequired.
            (147, typeof(PlanHorizonCompositionRequiredException)),    // 21w0d, still unsupported
        })
        {
            await using var context = NewContext();
            var legacy = new CountingPlanGenerationEngine();
            var catalog = new CountingCatalogPreviewGenerator();
            var service = CreatePlanServices(context, legacy, catalog);

            var exception = await Record.ExceptionAsync(() => service.GeneratePreviewAsync(
                Guid.NewGuid(), RaceRequest(startDate, startDate.AddDays(elapsedDays))));

            Assert.NotNull(exception);
            Assert.Equal(expectedException, exception.GetType());
            Assert.False(legacy.WasCalled);
            Assert.Equal(0, catalog.Calls); // sole live composition point was not reached; all five dynamic orchestrators are downstream
            Assert.Empty(context.PlanPreviews);
            Assert.Empty(context.TrainingPlans);
            Assert.Empty(context.TrainingWeeks);
            Assert.Empty(context.TrainingDays);
        }
    }

    [Fact]
    public async Task VerifiedRegressionDates_NonPilotIdentity_StartDate20260525_RaceDate20261012_ThrowsPlanHorizonCompositionRequired()
    {
        // Retargeted to a non-pilot-scope identity (Phase 4G.6B activates
        // this exact 20-week span for the pilot candidate specifically --
        // see PilotScope_FifteenToTwentyWeekHorizon_RoutesToPreparationRunwayPreview_NotCompositionRequired).
        // The underlying date-arithmetic regression this test guards against
        // is identity-agnostic, so a non-pilot identity still proves it.
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var request = RaceRequest(new DateOnly(2026, 5, 25), new DateOnly(2026, 10, 12), goalDistance: GoalDistance.FiveK, level: RunningBackground.Beginner, daysPerWeek: 3);
        request.PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri };
        request.LongRunDay = Weekday.Fri;

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
    public async Task JustAboveMaximum_NonPilotIdentity_Throws(int weeks)
    {
        // Retargeted to a non-pilot-scope identity -- see
        // PilotScope_FifteenToTwentyWeekHorizon_RoutesToPreparationRunwayPreview_NotCompositionRequired
        // for the now-activated pilot-scope 15-20 week behavior.
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(weeks * 7), goalDistance: GoalDistance.FiveK, level: RunningBackground.Beginner, daysPerWeek: 3);
        request.PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri };
        request.LongRunDay = Weekday.Fri;

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
    public async Task RegressionGuard_NonPilotIdentity_TwentyWeekHorizon_NeverProducesATwelveWeekPreview()
    {
        // The exact shape of the old bug: total recognized horizon (20 weeks)
        // is greater than the standalone core length that would otherwise be
        // silently selected (12 weeks), with no explicit preparation-block
        // metadata anywhere in the request. Generation must fail — a 12-week
        // (or any) preview must never be returned. Retargeted to a non-pilot
        // identity (Phase 4G.6B activates this exact horizon for the pilot
        // candidate specifically); the underlying regression this test
        // guards against is identity-agnostic.
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var request = RaceRequest(new DateOnly(2026, 5, 25), new DateOnly(2026, 10, 12), goalDistance: GoalDistance.FiveK, level: RunningBackground.Beginner, daysPerWeek: 3);
        request.PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri };
        request.LongRunDay = Weekday.Fri;
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
    public async Task ActivatedStandaloneCoreHorizon_ReachesRoutingWithoutHorizonException(int weeks)
    {
        await using var context = NewContext();
        var legacy = new CountingPlanGenerationEngine();
        var catalog = new CountingCatalogPreviewGenerator();
        var service = CreatePlanServices(context, legacy, catalog);

        var startDate = new DateOnly(2026, 7, 20);
        var request = RaceRequest(startDate, startDate.AddDays(weeks * 7));

        var ex = await Record.ExceptionAsync(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.NotNull(ex);
        Assert.IsNotType<PlanHorizonCompositionRequiredException>(ex);
        Assert.IsNotType<PlanCoreHorizonUnsupportedException>(ex);
        Assert.True(legacy.WasCalled || catalog.Calls > 0);
    }

    [Fact]
    public void RaceHorizonPolicy_EightThroughFourteen_AreActivatedStandaloneCore()
    {
        foreach (var weeks in new[] { 8, 9, 10, 11, 13, 14 })
        {
            Assert.Equal(RaceHorizonClassification.StandaloneCoreSupported, RaceHorizonPolicy.Classify(weeks));
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
            case RaceHorizonClassification.StandaloneCoreSupported:
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
        if (classification is not RaceHorizonClassification.ExactStandaloneCoreSupported
            and not RaceHorizonClassification.StandaloneCoreSupported)
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

        public Task<(CatalogPreviewSnapshot Snapshot, IReadOnlyList<PreviewWeekDto> Weeks)> GeneratePreparationRunwayPreviewAsync(
            GeneratePreviewRequest request, RunningApp.Application.RuntimeCatalog.Schedule.Horizon.CoreHorizonDecision horizonDecision,
            DateOnly asOfDate, PlanCatalogOptions catalogOptions, bool confirmationEnabled = false, CancellationToken ct = default)
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
