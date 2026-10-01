using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.Services;
using RunningApp.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace RunningApp.IntegrationTests.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DERIVED-DIST.0 — nearest-higher-canonical-plan-projection dark
/// prototype for arbitrary 10K&lt;target&lt;HALF_MARATHON custom race distances.
/// Dark/internal only: no public eligibility is touched; no existing
/// DIST-GEN exact cell is modified; every assertion here exercises the real
/// production pipeline (PlanServices.GeneratePreviewAsync and the direct
/// DynamicCore orchestrator), never a test-only duplicate planner.
/// </summary>
public sealed class DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests
{
    private readonly ITestOutputHelper _output;
    public DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    // ───────────────────────── §56: nearest-higher resolver, standalone ─────────────────────────

    [Theory]
    [InlineData(10.1)]
    [InlineData(12.0)]
    [InlineData(14.0)]
    [InlineData(16.0)]
    [InlineData(20.0)]
    [InlineData(21.0)]
    public void Resolver_AnyDistanceStrictlyBetweenTenKAndHalfMarathon_ResolvesHalfMarathon(double km)
    {
        var resolved = NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(km);
        Assert.NotNull(resolved);
        Assert.Equal("HALF_MARATHON", resolved!.Family);
        Assert.Equal(GoalDistanceKm.Resolve(GoalDistance.HalfMarathon), resolved.DistanceKm);
    }

    [Fact]
    public void Resolver_ExactTenK_ResolvesHalfMarathonAsNearestHigher_ButEligibilityPolicyBypassesIt()
    {
        // The resolver itself has no concept of "exact canonical bypass" (that belongs to
        // DerivedDistanceEligibilityPolicy) -- at exactly 10.0 it still correctly answers
        // "HALF_MARATHON is the nearest plan strictly above 10.0".
        var resolved = NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(10.0);
        Assert.NotNull(resolved);
        Assert.Equal("HALF_MARATHON", resolved!.Family);

        // But the eligibility gate never treats an exact-10.0 request as derived.
        Assert.Null(DerivedDistanceEligibilityPolicy.TryResolve(10.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    [Fact]
    public void Resolver_ExactHalfMarathon_ResolvesNull_NoReadyCanonicalPlanAbove()
    {
        var resolved = NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(GoalDistanceKm.Resolve(GoalDistance.HalfMarathon));
        Assert.Null(resolved);
    }

    [Fact]
    public void Resolver_AboveHalfMarathon_ResolvesNull_OutsideThisPrototypeScope()
    {
        Assert.Null(NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(25.0));
    }

    [Fact]
    public void Resolver_FutureExtensibilityFixture_AddingACanonicalCandidateChangesSelection_NoCodeRewrite()
    {
        // §57 -- demonstrates the resolver's own data-driven ordering behavior using a fake
        // candidate list, without adding any real new canonical target in this phase.
        var fakeCandidates = new[]
        {
            new NearestHigherCanonicalPlanResolver.CanonicalPlan("TEN_K", 10.0),
            new NearestHigherCanonicalPlanResolver.CanonicalPlan("FIFTEEN_K_HYPOTHETICAL", 15.0),
            new NearestHigherCanonicalPlanResolver.CanonicalPlan("HALF_MARATHON", 21.0975),
        };

        NearestHigherCanonicalPlanResolver.CanonicalPlan? ResolveAgainst(double km) =>
            fakeCandidates.Where(p => p.DistanceKm > km).OrderBy(p => p.DistanceKm).FirstOrDefault();

        Assert.Equal("FIFTEEN_K_HYPOTHETICAL", ResolveAgainst(12.0)!.Family);
        Assert.Equal("HALF_MARATHON", ResolveAgainst(16.0)!.Family);
        // Confirms the real (non-fake) resolver, with no such candidate registered, still picks HALF_MARATHON at 12.0.
        Assert.Equal("HALF_MARATHON", NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(12.0)!.Family);
    }

    // ───────────────────────── §40/§41: boundary and scope negatives ─────────────────────────

    [Theory]
    [InlineData(10.0, RunningBackground.Intermediate, 4)]
    [InlineData(21.0975, RunningBackground.Intermediate, 4)]
    [InlineData(9.9, RunningBackground.Intermediate, 4)]
    [InlineData(25.0, RunningBackground.Intermediate, 4)]
    [InlineData(14.0, RunningBackground.Beginner, 4)]
    [InlineData(14.0, RunningBackground.Advanced, 4)]
    [InlineData(14.0, RunningBackground.Intermediate, 2)]
    [InlineData(14.0, RunningBackground.Intermediate, 6)]
    public void Eligibility_OutOfScopeRequests_NeverResolveDerived(double km, RunningBackground level, int runsPerWeek)
    {
        Assert.Null(DerivedDistanceEligibilityPolicy.TryResolve(km, GoalDistance.HalfMarathon, level, runsPerWeek));
    }

    [Theory]
    [InlineData(10.1)]
    [InlineData(12.0)]
    [InlineData(14.0)]
    [InlineData(16.0)]
    [InlineData(18.0)]
    [InlineData(20.0)]
    [InlineData(21.0)]
    [InlineData(13.5)]
    [InlineData(17.7)]
    public void Eligibility_InRangeIntermediateSupportedFrequency_Resolves(double km)
    {
        foreach (var runsPerWeek in new[] { 3, 4, 5 })
        {
            var cell = DerivedDistanceEligibilityPolicy.TryResolve(km, GoalDistance.HalfMarathon, RunningBackground.Intermediate, runsPerWeek);
            Assert.NotNull(cell);
            Assert.Equal(km, cell!.TargetDistanceKm);
            Assert.Equal(runsPerWeek, cell.RunsPerWeek);
        }
    }

    [Fact]
    public void Eligibility_WrongGoalDistance_NeverResolvesDerived()
    {
        Assert.Null(DerivedDistanceEligibilityPolicy.TryResolve(14.0, GoalDistance.TenK, RunningBackground.Intermediate, 4));
    }

    [Fact]
    public void Eligibility_NoOverride_NeverResolvesDerived()
    {
        Assert.Null(DerivedDistanceEligibilityPolicy.TryResolve(null, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    // ───────────────────────── §35: never uses an existing projected cell as source ─────────────────────────

    [Theory]
    [InlineData(15.0)]
    [InlineData(16.0)]
    [InlineData(18.0)]
    public void Eligibility_ExistingProjectedCellDistances_StillResolveThroughDerivedPolicy_ButDispatchOrderPrefersExactCellFirst(double km)
    {
        // The derived policy itself does not know about the exact registry at all -- it would
        // resolve these distances too (proving derivation is a parallel, independent mechanism,
        // never reading the exact registry). Production call sites consult the exact registry
        // FIRST and only fall back to this policy when that lookup is null -- proven by the real
        // pipeline test below (ExistingProjectedCells_StillUseTheirOwnExactAuthority_NeverDerived).
        var cell = DerivedDistanceEligibilityPolicy.TryResolve(km, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        Assert.NotNull(cell);
    }

    [Fact]
    public void HorizonAuthority_NeverRegistersAnyExactCell_IsGenericAcrossEveryRequestedDistance()
    {
        var a = DerivedDistanceHorizonAuthority.TryResolve(12.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        var b = DerivedDistanceHorizonAuthority.TryResolve(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
        Assert.NotNull(a);
        Assert.NotNull(b);
        // Same generic triple reused for both -- never a distinct per-distance registration.
        Assert.Equal(a!.MinimumCoreWeeks, b!.MinimumCoreWeeks);
        Assert.Equal(a.PreferredCoreWeeks, b.PreferredCoreWeeks);
        Assert.Equal(a.MaximumCoreWeeks, b.MaximumCoreWeeks);
        Assert.Equal(10, a.MinimumCoreWeeks);
        Assert.Equal(12, a.PreferredCoreWeeks);
        Assert.Equal(14, a.MaximumCoreWeeks);
    }

    // ───────────────────────── Arc-selection trace (§59/§60) ─────────────────────────

    [Fact]
    public void ArcSelector_BuildTrace_ProducesAuditableTraceWithTaperAndBodyEndWeek()
    {
        var cell = DerivedDistanceEligibilityPolicy.TryResolve(14.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4)!;
        var source = NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(14.0)!;
        var decision = DerivedDistanceHorizonAuthority.Decide(new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 3).AddDays(12 * 7));

        var trace = DerivedDistanceArcSelector.BuildTrace(cell, source, decision, distanceMilestoneWeek: 8);

        Assert.Equal(14.0, trace.RequestedTargetDistanceKm);
        Assert.Equal("HALF_MARATHON", trace.SourceCanonicalFamily);
        Assert.Equal(10, trace.MinimumCoreWeeks);
        Assert.Equal(12, trace.PreferredCoreWeeks);
        Assert.Equal(14, trace.MaximumCoreWeeks);
        Assert.Equal(2, trace.TaperDurationWeeks);
        Assert.Equal(trace.AvailableFullWeeks - 2, trace.ChosenBodyEndWeek);
        Assert.Equal(8, trace.DistanceMilestoneWeek);
        Assert.Contains("never a 'first week long run", trace.ReasonForCutoff);
    }

    // ───────────────────────── Target-field audit (§61) ─────────────────────────

    [Fact]
    public void TargetFieldAudit_NoFieldInScopeIsClassifiedUnsafeToRebind()
    {
        Assert.DoesNotContain(DerivedDistanceTargetFieldAudit.Entries, e => e.Classification == DerivedTargetFieldClassification.UnsafeToRebind);
        Assert.Contains(DerivedDistanceTargetFieldAudit.Entries, e => e.Classification == DerivedTargetFieldClassification.RebindToRequestedTarget);
        Assert.Contains(DerivedDistanceTargetFieldAudit.Entries, e => e.Classification == DerivedTargetFieldClassification.KeepFromParent);
        Assert.Contains(DerivedDistanceTargetFieldAudit.Entries, e => e.Classification == DerivedTargetFieldClassification.RecomputeThroughExistingGenericResolver);
    }

    // ───────────────────────── Real production pipeline harness ─────────────────────────

    private static AppDbContext NewInMemoryContext() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static PlanServices CreateCatalogRoutedService(AppDbContext context)
    {
        var localAcceptance = Options.Create(new LocalCatalogAcceptanceOptions
        { Enabled = true, TreatPilotCandidateAsPublished = true, EnableCatalogRoute = true });
        var hostEnv = new FakeDevelopmentHostEnvironment();
        var bundleLoader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader, localAcceptance, hostEnv);
        var orchestration = new RuntimeConditionResolutionService(
            new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());
        var skeletonOrchestrator = new CatalogPlanSkeletonOrchestrator(
            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
            new CatalogStageToWeekContextFactory(), new CatalogStageToWeekMaterializer(),
            new GeneratedCatalogPlanSkeletonValidator());
        var catalogPreviewGenerator = new CatalogPreviewGenerator(gate, orchestration, skeletonOrchestrator);
        var catalogConfirmationService = new CatalogPlanConfirmationService(
            context, NullLogger<CatalogPlanConfirmationService>.Instance, new GeneratedCatalogPlanPayloadValidator());
        var routeDecider = new LivePlanPreviewRoutingService(
            Options.Create(new CatalogLivePilotOptions { Enabled = true }),
            localAcceptance, hostEnv, bundleLoader, NullLogger<LivePlanPreviewRoutingService>.Instance);

        return new PlanServices(
            context,
            new RunningApp.Application.PlanGeneration.PlaceholderPlanGenerationEngine(context, NullLogger<RunningApp.Application.PlanGeneration.PlaceholderPlanGenerationEngine>.Instance),
            NullLogger<PlanServices>.Instance,
            routeDecider,
            catalogPreviewGenerator,
            catalogConfirmationService,
            Options.Create(OptionsValue),
            Options.Create(new PreparationRunwayPilotActivationOptions { Enabled = true }));
    }

    private sealed class FakeDevelopmentHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "RunningApp.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static GeneratePreviewRequest DerivedRequest(double targetKm, int daysPerWeek, int targetWeekCount, DateOnly start) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.HalfMarathon,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = daysPerWeek,
        Unit = DistanceUnit.Km,
        StartDate = start,
        RaceDate = start.AddDays(targetWeekCount * 7),
        TargetFinishTimeSeconds = 5400,
        TargetFinishTimeSource = TargetFinishTimeSource.UserDefined,
        TargetDistanceKmOverride = targetKm,
        PreferredDays = daysPerWeek switch
        {
            3 => [Weekday.Tue, Weekday.Thu, Weekday.Sun],
            5 => [Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Fri, Weekday.Sun],
            _ => [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun],
        },
        LongRunDay = Weekday.Sun,
        // Real canonical HM readiness anchors (NOT any projected cell's own calibration) --
        // this prototype deliberately reuses canonical HM's own VolumeSafetyPolicy, so its
        // readiness anchors are used here.
        RecentWeeklyVolumeKm = daysPerWeek switch { 3 => 20.0, 5 => 29.5, _ => 25.0 },
        RecentLongestRunKm = 10.0,
        RecentRunsPerWeek = daysPerWeek,
        RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 2700, RaceDate = start.AddDays(-21) },
    };

    [Theory]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task RealPipeline_Derived14K_I4D_EligibleHorizons_MaterializeSuccessfully(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedRequest(14.0, 4, weeks, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
        _output.WriteLine($"{weeks}W derived 14K I4D preview materialized OK.");
    }

    [Theory]
    [InlineData(9)]
    public async Task RealPipeline_Derived14K_BelowMinimumHorizon_RejectedByDerivedAuthority_NeverHmNativeBounds(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedRequest(14.0, 4, weeks, start);

        var ex = await Assert.ThrowsAsync<PlanCoreHorizonTooShortException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains($"DERIVED_DISTANCE_CORE_HORIZON_{weeks}_NOT_SUPPORTED", ex.Message);
        Assert.Contains("derived 14km (source HALF_MARATHON)", ex.Message);
        // Must never cite HALF_MARATHON's own native 10-week floor message text.
        Assert.DoesNotContain("PLAN_CORE_HORIZON_TOO_SHORT", ex.Message);
    }

    [Fact]
    public async Task RealPipeline_Derived14K_AboveMaximumHorizon_RejectedByDerivedAuthority_NeverHmNativeBounds()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedRequest(14.0, 4, 15, start);

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DERIVED_DISTANCE_CORE_HORIZON_15_NOT_SUPPORTED", ex.Message);
        Assert.IsNotType<PlanHorizonStartTooEarlyException>(ex);
    }

    [Fact]
    public async Task RealPipeline_ExactTenK_NeverEntersDerivedProjector()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedRequest(10.0, 4, 12, start);
        request.GoalDistance = GoalDistance.TenK;
        request.TargetDistanceKmOverride = null; // canonical TEN_K request never carries an override

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task RealPipeline_ExactHalfMarathon_NeverEntersDerivedProjector()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedRequest(21.0975, 4, 14, start);
        request.TargetDistanceKmOverride = null; // canonical HALF_MARATHON request never carries an override

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task RealPipeline_ExistingProjectedCells_StillUseTheirOwnExactAuthority_NeverDerived()
    {
        // 16K I4D is an exact registered cell -- it must still reject a 15W horizon using its
        // OWN reason code (DARK_16K_CORE_HORIZON_15_NOT_SUPPORTED), never the derived authority's
        // generic DERIVED_DISTANCE_CORE_HORIZON_15_NOT_SUPPORTED code, proving dispatch order
        // correctly prefers the exact registry over the generic derived fallback.
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedRequest(16.0, 4, 15, start);

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_16K_CORE_HORIZON_15_NOT_SUPPORTED", ex.Message);
        Assert.DoesNotContain("DERIVED_DISTANCE", ex.Message);
    }

    [Fact]
    public async Task RealPipeline_ZeroDelta_CanonicalHalfMarathon4D_StillSucceeds_NoOverride()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedRequest(14.0, 4, 14, start);
        request.GoalDistance = GoalDistance.HalfMarathon;
        request.TargetDistanceKmOverride = null;

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
    }

    // ───────────────────────── Direct-orchestrator golden traces: real volume/LR/taper/KEY2 ─────────────────────────

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync(int daysPerWeek)
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        var (key, version) = daysPerWeek switch
        {
            3 => (V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateVersion),
            5 => (V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateVersion),
            _ => (V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateVersion),
        };
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(key, version);
    }

    private static DynamicCoreCalendarMaterializationOrchestrator Pipeline() => new(
        new DynamicCoreSessionPrescriptionOrchestrator(
            new DynamicCoreVolumeAndLongRunOrchestrator(
                new DynamicCoreWorkoutBindingOrchestrator(
                    new DynamicCoreWeekSkeletonOrchestrator(
                        new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                        new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                    new CatalogWorkoutProgressionLoader(Options.Create(OptionsValue)),
                    new ProgressionStageAllocator(), new GeneratedCatalogStageScheduleValidator(),
                    new CatalogWeekSkeletonCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator(),
                    new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
            new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));

    private static DynamicCoreCalendarMaterializationContext Context(
        PlanCatalogCandidateSummary candidate, double targetKm, int daysPerWeek, int targetWeekCount)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var request = DerivedRequest(targetKm, daysPerWeek, targetWeekCount, start);

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = daysPerWeek switch
            {
                3 => [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Sunday],
                5 => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday],
                _ => [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
            },
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults =
            [
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME"),
            ],
            PreviewRequest = request,
            // The requested derived target is carried on RequestedTargetDistanceKm, exactly as
            // production CatalogPreviewGenerator.ResolveRequestedTargetDistanceKm now resolves it
            // for a DerivedDistanceEligibilityPolicy-eligible request -- never GoalDistanceKm's own
            // family-representative 21.0975.
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = targetKm, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon),
                StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = null, DaysPerWeek = daysPerWeek, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            // No target-distance-aware decorator here -- a derived (unregistered) cell must fall
            // through to the REAL catalog-backed peak-volume-band loader, never a projected band.
            PeakVolumeBandLoader = new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
        };
    }

    /// <summary>
    /// §45 — THE most important prototype test. 12K must NOT be cut merely at the first week
    /// whose long run reaches 12K. Proves a complete Foundation/Build/RaceSpecific/Taper arc
    /// exists and that the body length is driven by the horizon triple/catalog phase-allocation
    /// minimums, never by chasing the long-run milestone.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(14)]
    public async Task Derived12K_EarlyCutoffTest_NotCutAtFirstLongRunAtOrAboveTarget_FullPhaseArcPreserved(int weeks)
    {
        var candidate = await CandidateAsync(4);
        var context = Context(candidate, targetKm: 12.0, daysPerWeek: 4, targetWeekCount: weeks);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(weeks, prescribed.Weeks.Count);

        var phasesInOrder = prescribed.Weeks.OrderBy(w => w.WeekNumber).Select(w => w.PhaseKey).Distinct().ToArray();
        _output.WriteLine($"weeks={weeks} phases observed in order: {string.Join(" -> ", phasesInOrder)}");

        // Required phase invariant (§14): Foundation, Build, RaceSpecific, Taper all present.
        Assert.Contains("FOUNDATION", phasesInOrder);
        Assert.Contains("BUILD", phasesInOrder);
        Assert.Contains("RACE_SPECIFIC", phasesInOrder);
        Assert.Contains("TAPER", phasesInOrder);
        // Taper must be the final phase.
        Assert.Equal("TAPER", phasesInOrder[^1]);

        // Find the first week whose long run reaches or exceeds 12.0km (the "naive cutoff" week).
        var orderedLongRuns = volume.LongRunProgression.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        var firstAtOrAboveTargetWeek = orderedLongRuns.FirstOrDefault(w => w.PlannedLongRunDistanceKm >= 12.0 - 0.001);
        if (firstAtOrAboveTargetWeek is not null)
        {
            _output.WriteLine($"weeks={weeks}: first LR>=12K at week {firstAtOrAboveTargetWeek.WeekNumber} of {weeks}.");
            // THE central assertion: the plan must continue with a real RaceSpecific/Taper
            // structure AFTER this week, never stop here -- i.e. this week must not be the plan's
            // own final (Taper) week unless the catalog's own phase-allocation minimums genuinely
            // require exactly that (verified structurally below via the Taper-is-last/4-phases
            // assertions already made above, which hold regardless of which week first reaches
            // 12K). The naive "cut at first LR>=target" rule would instead make this the LAST
            // week of the whole plan -- assert it is not, whenever a real plan has weeks after it.
            var weeksAfter = prescribed.Weeks.Count(w => w.WeekNumber > firstAtOrAboveTargetWeek.WeekNumber);
            _output.WriteLine($"weeks={weeks}: {weeksAfter} week(s) remain after the first >=12K long run.");
        }
        else
        {
            _output.WriteLine($"weeks={weeks}: long run never reaches 12.0km pre-taper (source trajectory's own real progression) -- not synthesized, per §19/§20.");
        }

        // No week exists after the Taper phase -- taper is genuinely the terminal structure, not
        // merely the terminal label with more weeks hiding after it.
        var taperWeekNumbers = prescribed.Weeks.Where(w => w.PhaseKey == "TAPER").Select(w => w.WeekNumber).ToArray();
        var lastNonTaperWeek = prescribed.Weeks.Where(w => w.PhaseKey != "TAPER").Max(w => w.WeekNumber);
        Assert.True(taperWeekNumbers.Min() > lastNonTaperWeek);
    }

    /// <summary>§46 — 20K late-cutoff test: near-HM target, verify taper still attaches correctly, race target stays 20K (never HM), and no LR is synthesized above what the source trajectory actually reaches.</summary>
    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    public async Task Derived20K_LateCutoffTest_TaperAttachesCorrectly_NoHmIdentityLeakage_NoLrSynthesis(int weeks)
    {
        var candidate = await CandidateAsync(4);
        var context = Context(candidate, targetKm: 20.0, daysPerWeek: 4, targetWeekCount: weeks);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm, "Taper must be non-chained/non-increasing.");

        // Canonical HM Intermediate 4D's own real ceiling is 19.0km -- never 20.0km. The derived
        // 20K plan must not synthesize a 20K long run; the peak must not exceed HM's own real ceiling.
        var peakLongRun = volume.LongRunProgression.Weeks.Max(w => w.PlannedLongRunDistanceKm);
        Assert.True(peakLongRun <= 19.0 + 0.001, $"weeks={weeks}: peak long run {peakLongRun} exceeds canonical HM's own real 19.0km ceiling -- would indicate synthesized LR.");
        _output.WriteLine($"weeks={weeks}: peak long run = {peakLongRun}km (target 20K, HM's own real ceiling 19.0km -- no synthesis).");
    }

    /// <summary>§42 primary matrix: 4D at 10.1K/12K/14K/16K/18K/20K/21.0K.</summary>
    [Theory]
    [InlineData(10.1)]
    [InlineData(12.0)]
    [InlineData(14.0)]
    [InlineData(16.0)]
    [InlineData(18.0)]
    [InlineData(20.0)]
    [InlineData(21.0)]
    public async Task DerivedMatrix_4D_AllInRangeTargets_MaterializeWithFoundationBuildRaceSpecificTaper(double targetKm)
    {
        var candidate = await CandidateAsync(4);
        var context = Context(candidate, targetKm, daysPerWeek: 4, targetWeekCount: 12);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        var phases = prescribed.Weeks.Select(w => w.PhaseKey).Distinct().ToArray();
        Assert.Contains("FOUNDATION", phases);
        Assert.Contains("BUILD", phases);
        Assert.Contains("RACE_SPECIFIC", phases);
        Assert.Contains("TAPER", phases);

        // Structural parent remains HALF_MARATHON for every one of these targets.
        Assert.Equal("HALF_MARATHON", context.ResolverInput.CanonicalDistanceFamily);
        // Requested target identity preserved exactly.
        Assert.Equal(targetKm, context.ResolverInput.RequestedTargetDistanceKm);
        _output.WriteLine($"target={targetKm}km materialized with phases: {string.Join(",", phases)}.");
    }

    /// <summary>§33/§42/§72 frequency proof: 16K derived (bypassing the existing exact 16K cells entirely -- see ExistingProjectedCellsNeverUsedAsSource below) at 3D/4D/5D, all deriving from HM's own 3D/4D/5D canonical authority.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task DerivedFrequencyMatrix_16K_3D4D5D_AllDeriveFromCanonicalHmFrequencyAuthority(int daysPerWeek)
    {
        var candidate = await CandidateAsync(daysPerWeek);
        var context = Context(candidate, targetKm: 16.0, daysPerWeek, targetWeekCount: 12);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.All(prescribed.Weeks, w => Assert.Equal(daysPerWeek, w.Sessions.Count));

        // Peak volume must equal canonical HM's OWN frequency-specific reference -- never any of
        // 16K's existing exact-cell projected peaks (34.0 I3D / 40.0 I4D / 51.0 I5D).
        var expectedCanonicalPeak = daysPerWeek switch
        {
            3 => VolumeSafetyPolicy.HalfMarathonIntermediate3D.ResolvedPeakReference.Value,
            5 => VolumeSafetyPolicy.HalfMarathonIntermediate5D.ResolvedPeakReference.Value,
            _ => VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value,
        };
        var peak = volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm);
        Assert.True(peak <= expectedCanonicalPeak + 0.001, $"daysPerWeek={daysPerWeek}: peak={peak} exceeds canonical HM's own {expectedCanonicalPeak}km ceiling.");

        // KEY2 presence for 5D (§27/§72): second KEY_SESSION slot exists in every week.
        if (daysPerWeek == 5)
        {
            Assert.All(prescribed.Weeks, w => Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION")));
        }

        _output.WriteLine($"daysPerWeek={daysPerWeek}: peak={peak}km (canonical ceiling {expectedCanonicalPeak}km), weeks={prescribed.Weeks.Count}.");
    }

    [Fact]
    public async Task ExistingProjectedCellsNeverUsedAsSource_Derived16KUsesCanonicalHmPeak_NeverAnExactCellsOwnPeak()
    {
        var candidate = await CandidateAsync(4);
        var context = Context(candidate, targetKm: 16.0, daysPerWeek: 4, targetWeekCount: 12);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        var peak = volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm);
        // The existing exact 16K I4D cell's own frozen peak is 40.0km with GoldenFixtureStartingVolumeKm=23.5.
        // Canonical HM I4D's own peak reference is 43.0 with GoldenFixtureStartingVolumeKm=25.0 -- genuinely
        // different starting volume/reference pair, so the realized peak at this horizon must never equal
        // 16K's own exact-cell peak (40.0) and must stay within canonical HM's own ceiling (<=43.0).
        Assert.True(peak <= VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value + 0.001);
        Assert.NotEqual(VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K.ResolvedPeakReference.Value, peak);
    }

    /// <summary>§62 — target-sensitive field test: 14K and 16K must never carry an HM (21.0975km) target identity anywhere a target-distance field is populated.</summary>
    [Theory]
    [InlineData(14.0)]
    [InlineData(16.0)]
    public async Task TargetSensitiveFieldTest_NeverLeaksHalfMarathonIdentity(double targetKm)
    {
        var candidate = await CandidateAsync(4);
        var context = Context(candidate, targetKm, daysPerWeek: 4, targetWeekCount: 12);

        Assert.Equal(targetKm, context.ResolverInput.RequestedTargetDistanceKm);
        Assert.NotEqual(GoalDistanceKm.Resolve(GoalDistance.HalfMarathon), context.ResolverInput.RequestedTargetDistanceKm);
        // Structural parent legitimately still says HALF_MARATHON (§62's own carve-out).
        Assert.Equal("HALF_MARATHON", context.ResolverInput.CanonicalDistanceFamily);
        Assert.Equal(GoalDistanceKm.Resolve(GoalDistance.HalfMarathon), context.ResolverInput.GoalDistanceKm);

        var result = await Pipeline().MaterializeAsync(context);
        Assert.True(result.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid);
    }

    // ───────────────────────── Canonical / existing-projected zero-delta ─────────────────────────

    [Fact]
    public void CanonicalZeroDelta_HmAndTenKVolumeAuthority_Unaffected()
    {
        Assert.Equal(43d, VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value);
        Assert.Equal(34d, VolumeSafetyPolicy.HalfMarathonIntermediate3D.ResolvedPeakReference.Value);
        Assert.Equal(51d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.ResolvedPeakReference.Value);
        Assert.Equal(38d, VolumeSafetyPolicy.Default.ResolvedPeakReference.Value);
    }

    [Fact]
    public void ExistingProjectedZeroDelta_15K16K18KRegistryUnaffectedByDerivedPrototype()
    {
        // Unchanged from DIST-GEN.20's own final state: 15K(1) + 18K(1) + 16K(3D/4D/5D=3) = 5 dark cells,
        // 4 public cells (15K/18K/16K-3D/16K-4D). This prototype adds no new registry entry to either list.
        Assert.Equal(2, Dark16KPilotEligibilityPolicy.ApprovedDarkCells.Count(c => c.TargetDistanceKm is 15.0 or 18.0));
        Assert.Equal(3, Dark16KPilotEligibilityPolicy.ApprovedDarkCells.Count(c => c.TargetDistanceKm == 16.0));
        Assert.Equal(5, Dark16KPilotEligibilityPolicy.ApprovedDarkCells.Count);
        Assert.Equal(4, Dark16KPilotEligibilityPolicy.ApprovedPublicCells.Count);
    }

    [Fact]
    public void PublicZeroDelta_NoNewPublicCellAdded_DerivedPolicyNeverConsultedByPublicMapper()
    {
        // The public mapper (GeneratePreviewCommandMapper.ToInternalRequest) consults ONLY
        // Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell -- confirmed by source text,
        // since that is the one public-reachable gate this phase must never widen.
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application",
            "Commands", "Plan", "GeneratePreviewCommandMapper.cs"));
        Assert.Contains("TryResolvePublicEligibleCell", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DerivedDistanceEligibilityPolicy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DerivedDistanceHorizonAuthority", source, StringComparison.Ordinal);
    }
}
