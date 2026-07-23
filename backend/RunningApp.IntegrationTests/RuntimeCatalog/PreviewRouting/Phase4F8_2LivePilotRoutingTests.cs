using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.PlanGeneration;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.Services;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

public sealed class Phase4F8_2LivePilotRoutingTests
{
    private static readonly DateOnly AsOfDate = new(2026, 1, 5);

    private static GeneratePreviewRequest PilotRequest(int weeks = 12) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = AsOfDate,
        RaceDate = AsOfDate.AddDays(weeks * 7),
        PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = Weekday.Sun,
        TargetFinishTimeSeconds = 3600,
    };

    [Fact]
    public void Phase4F8_2_PolicyIdentity_IsStable()
    {
        Assert.Equal("V1_LIVE_CATALOG_PILOT_ROUTING_POLICY", V1LiveCatalogPilotRoutingPolicy.PolicyKey);
        Assert.Equal(1, V1LiveCatalogPilotRoutingPolicy.PolicyVersion);
        Assert.Equal("TEN_K__4D__INTERMEDIATE", V1LiveCatalogPilotRoutingPolicy.CandidateKey);
        Assert.Equal(10, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
    }

    [Fact]
    public void Phase4F8_2_ExactPilot_PublishedAndEnabled_RoutesCatalogLive()
    {
        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(PilotRequest(), AsOfDate, 8, 14, "PUBLISHED", activationEnabled: true);

        Assert.Equal(LivePlanPreviewRoute.CatalogLive, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.PilotPublishedAndActivated, decision.Reason);
        Assert.False(decision.FallbackPermitted);
        LivePlanPreviewRouteDecisionValidator.Validate(decision);
    }

    [Theory]
    [InlineData(nameof(GeneratePreviewRequest.GoalDistance))]
    [InlineData(nameof(GeneratePreviewRequest.Level))]
    [InlineData(nameof(GeneratePreviewRequest.DaysPerWeek))]
    [InlineData(nameof(GeneratePreviewRequest.GoalType))]
    public void Phase4F8_2_NonPilotRequest_RoutesLegacyWithoutCatalog(string fieldToChange)
    {
        var request = PilotRequest();
        switch (fieldToChange)
        {
            case nameof(GeneratePreviewRequest.GoalDistance): request.GoalDistance = GoalDistance.FiveK; break;
            case nameof(GeneratePreviewRequest.Level): request.Level = RunningBackground.Beginner; break;
            case nameof(GeneratePreviewRequest.DaysPerWeek): request.DaysPerWeek = 5; break;
            case nameof(GeneratePreviewRequest.GoalType): request.GoalType = GoalType.Habit; break;
        }

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(request, AsOfDate, 8, 14, "PUBLISHED", activationEnabled: true);

        Assert.Equal(LivePlanPreviewRoute.Legacy, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.NotPilotRequest, decision.Reason);
        Assert.True(decision.FallbackPermitted);
    }

    [Theory]
    [InlineData("DRAFT", false, "CatalogSupportedButNotPublished", "PilotCandidateNotPublished")]
    [InlineData("DRAFT", true, "CatalogSupportedButNotPublished", "PilotCandidateNotPublished")]
    [InlineData("PUBLISHED", false, "CatalogSupportedButActivationDisabled", "PilotActivationDisabled")]
    [InlineData("PUBLISHED", true, "CatalogLive", "PilotPublishedAndActivated")]
    public void Phase4F8_2_LifecycleAndActivation_DualGateIsDeterministic(
        string status,
        bool enabled,
        string expectedRoute,
        string expectedReason)
    {
        var first = V1LiveCatalogPilotRoutingPolicy.Evaluate(PilotRequest(), AsOfDate, 8, 14, status, enabled);
        var second = V1LiveCatalogPilotRoutingPolicy.Evaluate(PilotRequest(), AsOfDate, 8, 14, status, enabled);

        Assert.Equal(expectedRoute, first.Route.ToString());
        Assert.Equal(expectedReason, first.Reason.ToString());
        Assert.Equal(first, second);
        LivePlanPreviewRouteDecisionValidator.Validate(first);
    }

    [Fact]
    public void Phase4F8_2_ActivationDefaultsDisabled()
    {
        var options = new CatalogLivePilotOptions();

        Assert.False(options.Enabled);
    }

    [Fact]
    public void Phase4F8_2_UnsupportedCycle_IsTypedNoFallback()
    {
        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(PilotRequest(weeks: 15), AsOfDate, 8, 14, "PUBLISHED", activationEnabled: true);

        Assert.Equal(LivePlanPreviewRoute.CatalogRequestUnsupported, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.UnsupportedCycleLength, decision.Reason);
        Assert.False(decision.FallbackPermitted);
    }

    [Fact]
    public void Phase4F8_2_EightWeekExplicitZero_IsNowCoreLengthNotImplemented()
    {
        // Phase 4G.2 superseded this: 8 weeks is rejected as
        // CoreLengthRecognizedButNotImplemented before the explicit-zero
        // infeasibility branch is ever reached, regardless of
        // RecentWeeklyVolumeKm.
        var request = PilotRequest(weeks: 8);
        request.RecentWeeklyVolumeKm = 0;

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(request, AsOfDate, 8, 14, "DRAFT", activationEnabled: false);

        Assert.Equal(LivePlanPreviewRoute.CatalogCoreLengthNotImplemented, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.CoreLengthRecognizedButNotImplemented, decision.Reason);
        Assert.False(decision.FallbackPermitted);
    }

    [Fact]
    public void Phase4F8_2_Service_DefaultDisabledRealDraft_ReturnsLegacy()
    {
        var service = NewRoutingService(new FixedStatusLoader("DRAFT"));

        var decision = service.Decide(PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12));

        Assert.Equal(GenerationSource.LegacySql, decision.Source);
        Assert.Equal(nameof(LivePlanPreviewRouteReason.PilotCandidateNotPublished), decision.RouteReason);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Phase 4F.9.3 — Development-only local-acceptance override
    // (LocalCatalogAcceptanceOptions). The real candidate stays DRAFT
    // (FixedStatusLoader("DRAFT") below) in every one of these tests; only
    // the route decision's effective lifecycle status may be swapped, and
    // only when every independent gate holds.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase4F9_3_LocalAcceptance_DevelopmentAllFlagsTrue_RealDraftCandidate_ReturnsCatalog()
    {
        var localOptions = new LocalCatalogAcceptanceOptions
        {
            Enabled = true,
            TreatPilotCandidateAsPublished = true,
            EnableCatalogRoute = true,
        };
        var service = NewRoutingService(
            new FixedStatusLoader("DRAFT"),
            catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = true },
            localAcceptanceOptions: localOptions,
            environmentName: Environments.Development);

        var decision = service.Decide(PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12));

        Assert.Equal(GenerationSource.Catalog, decision.Source);
        Assert.Equal(nameof(LivePlanPreviewRouteReason.PilotPublishedAndActivated), decision.RouteReason);
    }

    [Fact]
    public void Phase4F9_3_LocalAcceptance_ProductionEnvironment_AllFlagsTrue_StillReturnsLegacy()
    {
        // The environment gate is checked independently of configuration —
        // even if every LocalCatalogAcceptance flag were somehow true outside
        // Development, the override must not activate.
        var localOptions = new LocalCatalogAcceptanceOptions
        {
            Enabled = true,
            TreatPilotCandidateAsPublished = true,
            EnableCatalogRoute = true,
        };
        var service = NewRoutingService(
            new FixedStatusLoader("DRAFT"),
            catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = true },
            localAcceptanceOptions: localOptions,
            environmentName: Environments.Production);

        var decision = service.Decide(PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12));

        Assert.Equal(GenerationSource.LegacySql, decision.Source);
        Assert.Equal(nameof(LivePlanPreviewRouteReason.PilotCandidateNotPublished), decision.RouteReason);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Phase4F9_3_LocalAcceptance_DevelopmentButAnySingleFlagFalse_StillReturnsLegacy(
        bool enabled, bool treatAsPublished, bool enableCatalogRoute)
    {
        var localOptions = new LocalCatalogAcceptanceOptions
        {
            Enabled = enabled,
            TreatPilotCandidateAsPublished = treatAsPublished,
            EnableCatalogRoute = enableCatalogRoute,
        };
        var service = NewRoutingService(
            new FixedStatusLoader("DRAFT"),
            catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = true },
            localAcceptanceOptions: localOptions,
            environmentName: Environments.Development);

        var decision = service.Decide(PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12));

        Assert.Equal(GenerationSource.LegacySql, decision.Source);
    }

    [Fact]
    public void Phase4F9_3_LocalAcceptance_OverrideActiveButCatalogLivePilotDisabled_StillReturnsLegacy()
    {
        // The override only fixes the lifecycle-status input; the pre-existing,
        // independently default-false CatalogLivePilot.Enabled activation gate
        // is untouched and still required.
        var localOptions = new LocalCatalogAcceptanceOptions
        {
            Enabled = true,
            TreatPilotCandidateAsPublished = true,
            EnableCatalogRoute = true,
        };
        var service = NewRoutingService(
            new FixedStatusLoader("DRAFT"),
            catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = false },
            localAcceptanceOptions: localOptions,
            environmentName: Environments.Development);

        var decision = service.Decide(PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12));

        Assert.Equal(GenerationSource.LegacySql, decision.Source);
        Assert.Equal(nameof(LivePlanPreviewRouteReason.PilotActivationDisabled), decision.RouteReason);
    }

    [Fact]
    public void Phase4F8_2_Service_PublishedEnabled_ReturnsCatalog()
    {
        var service = NewRoutingService(new FixedStatusLoader("PUBLISHED"), catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = true });

        var decision = service.Decide(PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12));

        Assert.Equal(GenerationSource.Catalog, decision.Source);
        Assert.Equal(nameof(LivePlanPreviewRouteReason.PilotPublishedAndActivated), decision.RouteReason);
    }

    [Fact]
    public async Task Phase4F8_2_PublishedEnabledPlanServices_InvokesCatalogExactlyOnce()
    {
        await using var context = NewContext();
        var catalog = new CountingCatalogPreviewGenerator();
        var legacy = new CountingPlanGenerationEngine();
        var service = CreatePlanServices(
            context,
            legacy,
            NewRoutingService(new FixedStatusLoader("PUBLISHED"), catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = true }),
            catalog);

        await Assert.ThrowsAsync<MarkerCatalogInvokedException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12)));

        Assert.False(legacy.WasCalled);
        Assert.Equal(1, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public async Task Phase4F8_2_LegacyRequest_InvokesLegacyExactlyOnce()
    {
        await using var context = NewContext();
        var catalog = new CountingCatalogPreviewGenerator();
        var legacy = new CountingPlanGenerationEngine();
        var service = CreatePlanServices(
            context,
            legacy,
            NewRoutingService(new FixedStatusLoader("PUBLISHED"), catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = true }),
            catalog);
        var request = PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12);
        request.GoalDistance = GoalDistance.FiveK;

        await Assert.ThrowsAsync<PlanTemplateNotAvailableException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.True(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public async Task Phase4F8_2_InvalidPilotRequest_DoesNotFallback()
    {
        // Generate-preview contract alignment: RaceDate is a shared Race-plan
        // request-shape requirement now (GeneratePreviewRequestValidator),
        // enforced BEFORE route decision even runs -- a null RaceDate on a
        // Race request is a plain 400 ArgumentException, not a catalog-pilot-
        // specific CatalogLivePilotRequestUnsupportedException. This is the
        // intended "don't conflate request validation with catalog
        // feasibility" boundary: the invariant this test actually guards
        // (invalid input never falls back to legacy, never reaches the
        // catalog generator, never persists a preview) still holds -- it's
        // simply enforced one layer earlier than before.
        await using var context = NewContext();
        var catalog = new CountingCatalogPreviewGenerator();
        var legacy = new CountingPlanGenerationEngine();
        var service = CreatePlanServices(
            context,
            legacy,
            NewRoutingService(new FixedStatusLoader("PUBLISHED"), catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = true }),
            catalog);
        var request = PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12);
        request.RaceDate = null;

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public async Task Phase4F8_2_EightWeekExplicitZero_DoesNotFallbackOrSnapshot()
    {
        // Phase 4G.2 superseded this: PlanServices.GeneratePreviewAsync's
        // upstream horizon guard now rejects 8 weeks as
        // PlanCoreHorizonUnsupportedException before route decision ever
        // runs — so before either the routing-level explicit-zero check or
        // the catalog generator is ever reached.
        await using var context = NewContext();
        var catalog = new CountingCatalogPreviewGenerator();
        var legacy = new CountingPlanGenerationEngine();
        var service = CreatePlanServices(
            context,
            legacy,
            NewRoutingService(new FixedStatusLoader("PUBLISHED"), catalogLivePilotOptions: new CatalogLivePilotOptions { Enabled = true }),
            catalog);
        var request = PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 8);
        request.RecentWeeklyVolumeKm = 0;

        await Assert.ThrowsAsync<RunningApp.Application.Exceptions.PlanCoreHorizonUnsupportedException>(() =>
            service.GeneratePreviewAsync(Guid.NewGuid(), request));

        Assert.False(legacy.WasCalled);
        Assert.Equal(0, catalog.Calls);
        Assert.Empty(context.PlanPreviews);
    }

    [Fact]
    public async Task Phase4F8_2_RealV10Candidate_RemainsDraft()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = System.IO.Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);

        var summary = await loader.LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);

        Assert.Equal("DRAFT", summary.CandidateStatus);
    }

    private static GeneratePreviewRequest PilotRequest(DateOnly asOfDate, int weeks)
    {
        // StartDate must move with asOfDate too, not just RaceDate -- the
        // available horizon is StartDate-to-RaceDate (RaceHorizonPolicy), not
        // "now"-to-RaceDate. Leaving StartDate pinned at the class-level
        // AsOfDate constant while RaceDate tracks the real current time (as
        // this helper previously did) desynchronizes the two and produces an
        // artificially huge horizon unrelated to what the test intends.
        var request = PilotRequest(weeks);
        request.StartDate = asOfDate;
        request.RaceDate = asOfDate.AddDays(weeks * 7);
        return request;
    }

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static PlanServices CreatePlanServices(
        AppDbContext context,
        IPlanGenerationEngine legacyEngine,
        IGenerationRouteDecider routeDecider,
        ICatalogPreviewGenerator catalogPreviewGenerator)
    {
        return new PlanServices(
            context,
            legacyEngine,
            NullLogger<PlanServices>.Instance,
            routeDecider,
            catalogPreviewGenerator,
            new CatalogPlanConfirmationService(
                context,
                NullLogger<CatalogPlanConfirmationService>.Instance,
                new GeneratedCatalogPlanPayloadValidator()));
    }

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

    /// <summary>Minimal fake for constructing LivePlanPreviewRoutingService directly in tests (Phase 4F.9.3 added an IHostEnvironment dependency for the Development-only local-acceptance override).</summary>
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "RunningApp.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static LivePlanPreviewRoutingService NewRoutingService(
        IPlanCatalogBundleLoader loader,
        CatalogLivePilotOptions? catalogLivePilotOptions = null,
        LocalCatalogAcceptanceOptions? localAcceptanceOptions = null,
        string environmentName = "Production") =>
        new(
            Options.Create(catalogLivePilotOptions ?? new CatalogLivePilotOptions()),
            Options.Create(localAcceptanceOptions ?? new LocalCatalogAcceptanceOptions()),
            new FakeHostEnvironment(environmentName),
            loader,
            NullLogger<LivePlanPreviewRoutingService>.Instance);

    private sealed class FixedStatusLoader : IPlanCatalogBundleLoader
    {
        private readonly string _status;

        public FixedStatusLoader(string status) => _status = status;

        public Task<PlanCatalogCandidateSummary> LoadCandidateAsync(string candidateKey, int candidateVersion, CancellationToken ct = default) =>
            Task.FromResult(new PlanCatalogCandidateSummary
            {
                CandidateKey = candidateKey,
                CandidateVersion = candidateVersion,
                CandidateStatus = _status,
                CanonicalDistanceFamily = "TEN_K",
                Level = "INTERMEDIATE",
                DaysPerWeek = 4,
                CoreCycle = new PlanCatalogCoreCycle(8, 12, 14),
                MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 6),
                Layout = new PlanCatalogReference("RUN_LAYOUT_4D", 2),
                LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
                WorkoutProgression = new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 5),
                ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2),
                RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
                PeakVolumeBandPolicy = new PlanCatalogReference("TEN_K_PEAK_VOLUME_BANDS_V1", 1),
                RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 1),
                DependencyStatuses = new Dictionary<string, string>
                {
                    ["masterTemplate"] = "PUBLISHED",
                    ["layout"] = "PUBLISHED",
                    ["levelModifier"] = "PUBLISHED",
                    ["rulePack"] = "PUBLISHED",
                },
                ReferencedWorkouts = Array.Empty<PlanCatalogReference>(),
                PhaseKeys = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" },
                PhaseAllocations = new[]
                {
                    new PlanCatalogPhaseAllocation("FOUNDATION", 3),
                    new PlanCatalogPhaseAllocation("BUILD", 4),
                    new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 3),
                    new PlanCatalogPhaseAllocation("TAPER", 2),
                },
                SlotRoles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" },
            });
    }
}
