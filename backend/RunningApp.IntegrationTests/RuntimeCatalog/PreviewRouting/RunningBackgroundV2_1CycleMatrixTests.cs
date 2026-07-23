using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Running Background V2.1 — Intermediate pilot cycle-selection matrix.
/// Exercises the REAL production classes (<see cref="V1LiveCatalogPilotRoutingPolicy"/>,
/// <see cref="PlanCatalogBundleLoader"/>, <see cref="LivePlanPreviewRoutingService"/>)
/// against the real on-disk TEN_K__4D__INTERMEDIATE v10 candidate and its
/// real TEN_K_MASTER v6 core-cycle bounds (minimumWeeks=8, defaultWeeks=12,
/// maximumWeeks=14) — no duplicated test arithmetic for the bounds
/// themselves. This proves the fix for UnsupportedCycleLength: bounds now
/// come from <see cref="PlanCatalogCandidateSummary.CoreCycle"/> via the
/// real bundle loader, not a hardcoded literal.
/// </summary>
public sealed class RunningBackgroundV2_1CycleMatrixTests
{
    private static readonly DateOnly AsOfDate = new(2026, 1, 5);

    private static GeneratePreviewRequest PilotRequest(DateOnly asOfDate, int weeks) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = asOfDate,
        RaceDate = asOfDate.AddDays(weeks * 7),
        PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = Weekday.Sun,
    };

    private static GeneratePreviewRequest PilotRequest(int weeks) => PilotRequest(AsOfDate, weeks);

    private static async Task<(int Minimum, int? Maximum)> LoadRealCoreCycleBoundsAsync()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        var summary = await loader.LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        return (summary.CoreCycle.MinimumWeeks, summary.CoreCycle.MaximumWeeks);
    }

    [Fact]
    public async Task RealCandidate_CoreCycleBounds_AreEightToFourteen()
    {
        var (minimum, maximum) = await LoadRealCoreCycleBoundsAsync();

        Assert.Equal(8, minimum);
        Assert.Equal(14, maximum);
    }

    [Fact]
    public async Task ExactTwelveWeekCycleLength_RoutesCatalogLive()
    {
        var (minimum, maximum) = await LoadRealCoreCycleBoundsAsync();
        var request = PilotRequest(12);

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(request, AsOfDate, minimum, maximum, "PUBLISHED", activationEnabled: true);

        Assert.Equal(LivePlanPreviewRoute.CatalogLive, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.PilotPublishedAndActivated, decision.Reason);
        Assert.Equal(12, decision.CycleLengthWeeks);
        LivePlanPreviewRouteDecisionValidator.Validate(decision);
    }

    // ── Phase 4G.2 temporary safety constraint ────────────────────────────
    // The candidate's coreCycle bounds nominally advertise 8-14 weeks as "in
    // range", but CatalogPhaseAllocationResolver is not yet horizon-aware —
    // it always emits its fixed default ~12-week allocation regardless of
    // the accepted cycle length (live-verified overshoot at 8 weeks,
    // undershoot at 20 weeks). Only the one exact length proven safe (12) is
    // routed to catalog generation; every other nominally-in-range horizon
    // is recognized but not yet implemented.
    [Theory]
    [InlineData(8)]   // minimum supported per coreCycle bounds, but not the implemented length
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(14)]  // maximum supported per coreCycle bounds, but not the implemented length
    public async Task InRangeButNotExactTwelve_RoutesCoreLengthNotImplemented(int weeks)
    {
        var (minimum, maximum) = await LoadRealCoreCycleBoundsAsync();
        var request = PilotRequest(weeks);
        if (weeks == 8)
        {
            // Avoid the separate, already-confirmed 8-week-explicit-zero
            // infeasibility branch so this test isolates cycle-length
            // classification specifically (that branch is now unreachable
            // for weeks==8 regardless, since CoreLengthRecognizedButNotImplemented
            // is checked first — see EightWeekExplicitZero_IsNowCoreLengthNotImplemented_NotInfeasible).
            request.RecentWeeklyVolumeKm = 20;
        }

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(request, AsOfDate, minimum, maximum, "PUBLISHED", activationEnabled: true);

        Assert.Equal(LivePlanPreviewRoute.CatalogCoreLengthNotImplemented, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.CoreLengthRecognizedButNotImplemented, decision.Reason);
        Assert.Equal(weeks, decision.CycleLengthWeeks);
        Assert.False(decision.FallbackPermitted);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    public async Task AboveMaximum_IsUnsupportedCycleLength_NoClampNoFallback(int weeks)
    {
        // Documented existing policy (Phase 4F.8.2 audit): an inclusive range
        // check, not clamp-to-max or nearest-supported-cycle. A request more
        // than the maximum supported weeks out is rejected outright.
        var (minimum, maximum) = await LoadRealCoreCycleBoundsAsync();

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(PilotRequest(weeks), AsOfDate, minimum, maximum, "PUBLISHED", activationEnabled: true);

        Assert.Equal(LivePlanPreviewRoute.CatalogRequestUnsupported, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.UnsupportedCycleLength, decision.Reason);
        Assert.False(decision.FallbackPermitted);
        Assert.Equal(weeks, decision.CycleLengthWeeks);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    public async Task BelowMinimum_IsUnsupportedCycleLength_InsufficientTime(int weeks)
    {
        var (minimum, maximum) = await LoadRealCoreCycleBoundsAsync();

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(PilotRequest(weeks), AsOfDate, minimum, maximum, "PUBLISHED", activationEnabled: true);

        Assert.Equal(LivePlanPreviewRoute.CatalogRequestUnsupported, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.UnsupportedCycleLength, decision.Reason);
        Assert.False(decision.FallbackPermitted);
    }

    [Fact]
    public async Task EightWeekExplicitZero_IsNowCoreLengthNotImplemented_NotInfeasible()
    {
        // Phase 4G.2 superseded this: the known-infeasible-explicit-zero-at-
        // 8-weeks branch is now unreachable — CoreLengthRecognizedButNotImplemented
        // is checked first (an 8-week horizon is rejected regardless of
        // RecentWeeklyVolumeKm, since 8 weeks is not the one implemented
        // exact core length). The KnownInfeasibleEightWeekExplicitZero
        // reason/route remain defined for whenever 8-week generation is
        // reactivated by a future horizon-aware composition phase.
        var (minimum, maximum) = await LoadRealCoreCycleBoundsAsync();
        var request = PilotRequest(8);
        request.RecentWeeklyVolumeKm = 0;

        var decision = V1LiveCatalogPilotRoutingPolicy.Evaluate(request, AsOfDate, minimum, maximum, "PUBLISHED", activationEnabled: true);

        Assert.Equal(LivePlanPreviewRoute.CatalogCoreLengthNotImplemented, decision.Route);
        Assert.Equal(LivePlanPreviewRouteReason.CoreLengthRecognizedButNotImplemented, decision.Reason);
        Assert.False(decision.FallbackPermitted);
    }

    [Fact]
    public async Task DeterministicAsOfDate_SameInputsProduceSameDecision()
    {
        var (minimum, maximum) = await LoadRealCoreCycleBoundsAsync();
        var request = PilotRequest(12);

        var first = V1LiveCatalogPilotRoutingPolicy.Evaluate(request, AsOfDate, minimum, maximum, "PUBLISHED", activationEnabled: true);
        var second = V1LiveCatalogPilotRoutingPolicy.Evaluate(request, AsOfDate, minimum, maximum, "PUBLISHED", activationEnabled: true);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Service_Decide_RealBundleLoaderRealDraftCandidate_LocalAcceptanceOverride_TwelveWeeks_RoutesCatalog()
    {
        // Full production Decide() path against the real on-disk candidate
        // (still DRAFT — never published by this test) via the real
        // PlanCatalogBundleLoader, proving the end-to-end fix rather than
        // just the isolated policy function.
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        var service = new LivePlanPreviewRoutingService(
            Options.Create(new CatalogLivePilotOptions { Enabled = true }),
            Options.Create(new LocalCatalogAcceptanceOptions
            {
                Enabled = true,
                TreatPilotCandidateAsPublished = true,
                EnableCatalogRoute = true,
            }),
            new FakeHostEnvironment(Environments.Development),
            loader,
            NullLogger<LivePlanPreviewRoutingService>.Instance);

        var decision = service.Decide(PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 12));

        Assert.Equal(GenerationSource.Catalog, decision.Source);
        Assert.Equal(nameof(LivePlanPreviewRouteReason.PilotPublishedAndActivated), decision.RouteReason);
    }

    [Fact]
    public void Service_Decide_RealBundleLoader_TwentyWeeksOut_StillUnsupported_EvenWithLocalAcceptanceOverride()
    {
        // The local-acceptance override only ever changes the effective
        // lifecycle status, never the cycle-length bounds — a request that
        // is genuinely out of range must still fail, override or not.
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        var service = new LivePlanPreviewRoutingService(
            Options.Create(new CatalogLivePilotOptions { Enabled = true }),
            Options.Create(new LocalCatalogAcceptanceOptions
            {
                Enabled = true,
                TreatPilotCandidateAsPublished = true,
                EnableCatalogRoute = true,
            }),
            new FakeHostEnvironment(Environments.Development),
            loader,
            NullLogger<LivePlanPreviewRoutingService>.Instance);

        var ex = Assert.Throws<CatalogLivePilotRequestUnsupportedException>(() =>
            service.Decide(PilotRequest(DateOnly.FromDateTime(DateTime.UtcNow), weeks: 20)));

        Assert.Contains("unsupported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "RunningApp.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
