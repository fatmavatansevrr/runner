using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
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
/// PHASE DERIVED-DIST.4A — structural PUBLIC activation proof for the
/// TEN_K-sourced derived interval (5K&lt;target&lt;TEN_K), superseding
/// DERIVED-DIST.4's own dark/internal-only verification file
/// (<c>DerivedDist4TenKSourcedSubTenKVerificationTests</c>, deleted by this
/// phase). <see cref="DerivedDistanceEligibilityPolicy.AllowTenKSourcedDerivationForInternalVerificationOnly"/>
/// no longer exists — every test here exercises the REAL generic generation
/// machinery (CatalogPreviewGenerator, CatalogVolumeAndLongRunPlanner,
/// DerivedDistanceHorizonAuthority, DerivedDistanceArcSelector) exactly as a
/// real public request now does, with zero test-only switch to set or reset.
/// </summary>
public sealed class DerivedDist4ATenKSourcedSubTenKPublicActivationTests
{
    private readonly ITestOutputHelper _output;
    public DerivedDist4ATenKSourcedSubTenKPublicActivationTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    // PublishedBundleReleaseVersion must be set for IPublishedTemplateBundleLoader to resolve
    // anything (it short-circuits to null otherwise -- see PublishedTemplateBundleLoader's own
    // doc comment). "1.4.0" mirrors backend/RunningApp.Api/appsettings.json's own real production
    // value, under which a real TEN_K__5D__INTERMEDIATE.v1.json published bundle already exists
    // (confirmed present on disk) -- this is NOT a new/invented release, just reusing the real one.
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot, PublishedBundleReleaseVersion = "1.4.0" };

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
        // A TEN_K Intermediate 5D candidate's own catalog-declared KEY_SESSION genuinely is
        // ProfileBacked and requires a REAL ExecutionPrescriptionIndex resolved from a REAL
        // published bundle -- exactly what production's own DI-wired PublishedTemplateBundleLoader
        // already supplies for every real TEN_K Intermediate 5D request today.
        var publishedBundleLoader = new RunningApp.Application.RuntimeCatalog.Prescription.Execution.PublishedTemplateBundleLoader(
            OptionsValue, CatalogRoot);
        var catalogPreviewGenerator = new CatalogPreviewGenerator(
            gate,
            orchestration,
            skeletonOrchestrator,
            new CatalogWeekSkeletonCalendarMaterializer(),
            new DatedGeneratedCatalogPlanSkeletonValidator(),
            new CatalogWorkoutProgressionLoader(Options.Create(OptionsValue)),
            new ProgressionStageAllocator(),
            new GeneratedCatalogStageScheduleValidator(),
            new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            new CatalogWorkoutBinder(),
            new BoundCatalogPlanValidator(),
            new CatalogPeakVolumeBandLoader(Options.Create(OptionsValue)),
            new CatalogVolumeAndLongRunPlanner(),
            new CatalogSessionPrescriptionPlanner(),
            new CatalogFinalPrescribedPlanFinalizer(),
            new CatalogPublicPreviewMaterializer(),
            publishedBundleLoader);
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

    /// <summary>TEN_K-sourced derived request (5K&lt;target&lt;TEN_K) — GoalDistance=TenK, now structurally public.</summary>
    private static GeneratePreviewRequest DerivedTenKSourcedRequest(double targetKm, int daysPerWeek, int targetWeekCount, DateOnly start) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = daysPerWeek,
        Unit = DistanceUnit.Km,
        StartDate = start,
        RaceDate = start.AddDays(targetWeekCount * 7),
        TargetFinishTimeSeconds = 2400,
        TargetFinishTimeSource = TargetFinishTimeSource.UserDefined,
        TargetDistanceKmOverride = targetKm,
        PreferredDays = daysPerWeek switch
        {
            3 => [Weekday.Tue, Weekday.Thu, Weekday.Sun],
            5 => [Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Fri, Weekday.Sun],
            _ => [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun],
        },
        LongRunDay = Weekday.Sun,
        RecentWeeklyVolumeKm = daysPerWeek switch { 3 => 18.0, 5 => 27.0, _ => 22.0 },
        RecentLongestRunKm = 8.0,
        RecentRunsPerWeek = daysPerWeek,
        RecentRace = new RecentRaceInput { Distance = GoalDistance.FiveK, FinishTimeSeconds = 1200, RaceDate = start.AddDays(-21) },
    };

    // ───────────────────────── Routing/source proof ─────────────────────────

    [Theory]
    [InlineData(6.0)]
    [InlineData(7.0)]
    [InlineData(8.0)]
    [InlineData(9.0)]
    public void Resolver_AnyDistanceStrictlyBetweenFiveKAndTenK_ResolvesTenK(double km)
    {
        var resolved = NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(km);
        Assert.NotNull(resolved);
        Assert.Equal("TEN_K", resolved!.Family);
        Assert.Equal(GoalDistanceKm.Resolve(GoalDistance.TenK), resolved.DistanceKm);
    }

    [Fact]
    public void EligibilityPolicy_TenKSourced_IsStructurallyEligibleByDefault_NoSwitchRequired()
    {
        // DERIVED-DIST.4A §19/§20/§51: the dark-only AllowTenKSourcedDerivationForInternalVerificationOnly
        // switch has been removed. A 7K TEN_K-sourced Intermediate x 4D request is eligible purely
        // through gate (A) (product subset) + gate (B) (TEN_K's own public Level×Frequency matrix) --
        // the SAME structural composition the HALF_MARATHON-sourced interval already used from
        // DERIVED-DIST.2 onward.
        var cell = DerivedDistanceEligibilityPolicy.TryResolve(7.0, GoalDistance.TenK, RunningBackground.Intermediate, 4);
        Assert.NotNull(cell);
        Assert.Equal(7.0, cell!.TargetDistanceKm);
        Assert.Equal(GoalDistance.TenK, cell.ParentDistanceFamily);
    }

    [Fact]
    public void EligibilityPolicy_MismatchedFamily_StillRejected_HalfMarathonRequestForSub10KTarget()
    {
        // A request carrying GoalDistance=HalfMarathon for a km the resolver would
        // assign to TEN_K must still be rejected -- the generalized family check
        // (not merely the old hardcoded floor) is what is doing the rejecting now.
        Assert.Null(DerivedDistanceEligibilityPolicy.TryResolve(7.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
    }

    [Fact]
    public void EligibilityPolicy_FiveKFlavoredRequest_NeverEligible()
    {
        // Explicit proof this phase did NOT open a back door to sub-5K: a
        // FiveK-flavored request is rejected, because
        // ExpectedRequestGoalDistanceFor("TEN_K") != GoalDistance.FiveK.
        Assert.Null(DerivedDistanceEligibilityPolicy.TryResolve(3.0, GoalDistance.FiveK, RunningBackground.Intermediate, 4));
    }

    [Fact]
    public void EligibilityPolicy_UnsupportedLevelOrFrequency_StillRejected_ProductSubsetGateStillRestricts()
    {
        // Gate (A), the shared V1 product subset (Intermediate x {3,4,5}), still restricts this
        // interval exactly as it already restricts the HALF_MARATHON-sourced one -- e.g. Beginner
        // or Intermediate 6D remain ineligible even though TEN_K's own public matrix (gate B) admits
        // Beginner 2D/3D/4D and Intermediate 6D for canonical TEN_K itself.
        Assert.Null(DerivedDistanceEligibilityPolicy.TryResolve(7.0, GoalDistance.TenK, RunningBackground.Beginner, 3));
        Assert.Null(DerivedDistanceEligibilityPolicy.TryResolve(7.0, GoalDistance.TenK, RunningBackground.Intermediate, 6));
    }

    // ───────────────────────── Real end-to-end generation, 6K-9K ─────────────────────────

    public static readonly object[][] IntegerTargetsByFrequency =
    [
        [6.0, 3], [6.0, 4], [6.0, 5],
        [7.0, 3], [7.0, 4], [7.0, 5],
        [8.0, 3], [8.0, 4], [8.0, 5],
        [9.0, 3], [9.0, 4], [9.0, 5],
    ];

    [Theory]
    [MemberData(nameof(IntegerTargetsByFrequency))]
    public async Task RealPipeline_TenKSourced_6To9K_Intermediate345D_GeneratesSuccessfully_WithExactTargetIdentity(double km, int days)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedTenKSourcedRequest(km, days, 12, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        Assert.NotNull(response);
        Assert.Equal(GoalDistance.TenK, response.GoalDistance);
        Assert.Equal(km, response.RequestedTargetDistanceKm);
        Assert.Equal(12, response.Weeks.Count);
        Assert.Equal(days, response.Weeks[0].Days.Count(d => d.DayType != TrainingDayType.Rest));

        var peakWeeklyVolume = response.Weeks.Select(w => w.Days.Sum(d => d.DistanceKm)).Max();
        var peakLongRun = response.Weeks.SelectMany(w => w.Days)
            .Where(d => d.DayType == TrainingDayType.LongRun)
            .Select(d => d.DistanceKm)
            .DefaultIfEmpty(0.0)
            .Max();
        var distinctNonRestDayTypes = response.Weeks.SelectMany(w => w.Days)
            .Where(d => d.DayType != TrainingDayType.Rest)
            .Select(d => d.DayType)
            .Distinct()
            .Count();

        _output.WriteLine($"{km}km I{days}D (TEN_K-sourced, PUBLIC): weeks={response.Weeks.Count}, peakWeeklyVolumeKm={peakWeeklyVolume:0.00}, peakLongRunKm={peakLongRun:0.00}, distinctDayTypes={distinctNonRestDayTypes}");

        // Non-degenerate phase/day-type variety (mirrors DERIVED-DIST.3's own 33-cell assertion shape).
        Assert.True(distinctNonRestDayTypes >= 2, $"{km}km I{days}D produced a flat/degenerate day-type set.");
        // No parent-distance-identity leakage: TEN_K's own real family-representative 10.0 must never appear as the requested target.
        Assert.NotEqual(10.0, response.RequestedTargetDistanceKm);
    }

    [Fact]
    public async Task RealizedPeakVolumeAndLongRun_NumericCapture_SixToNineKm_Intermediate4D()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var rows = new System.Collections.Generic.List<string> { "TargetKm,PeakWeeklyVolumeKm,PeakLongRunKm,LrToTargetRatio,QualitySessionCount" };

        for (var km = 6; km <= 9; km++)
        {
            var request = DerivedTenKSourcedRequest(km, 4, 12, start);
            var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

            var peakWeeklyVolume = response.Weeks.Select(w => w.Days.Sum(d => d.DistanceKm)).Max();
            var peakLongRun = response.Weeks.SelectMany(w => w.Days)
                .Where(d => d.DayType == TrainingDayType.LongRun)
                .Select(d => d.DistanceKm)
                .DefaultIfEmpty(0.0)
                .Max();
            var qualitySessions = response.Weeks.SelectMany(w => w.Days)
                .Count(d => d.DayType is TrainingDayType.Interval or TrainingDayType.Tempo);
            var ratio = peakWeeklyVolume > 0 ? peakLongRun / peakWeeklyVolume : 0.0;

            rows.Add($"{km},{peakWeeklyVolume:0.00},{peakLongRun:0.00},{ratio:0.000},{qualitySessions}");
        }

        foreach (var row in rows)
        {
            _output.WriteLine(row);
        }
        Assert.Equal(5, rows.Count); // header + 4 integer targets (6-9)
    }

    [Theory]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task RealPipeline_TenKSourced_HorizonBoundary_UsesTenKsOwn8To14Window_Not10To14(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedTenKSourcedRequest(7.0, 4, weeks, start);

        if (weeks == 7)
        {
            // Below TEN_K's own real minimum (8) -- must reject. This is the single week count
            // that WOULD succeed if this interval wrongly reused HALF_MARATHON's 10-week minimum
            // instead of TEN_K's own real 8-week minimum (7 is below both, so this alone doesn't
            // discriminate) -- see the 8-week-boundary assertion below for the discriminating proof.
            await Assert.ThrowsAnyAsync<Exception>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        }
        else
        {
            var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
            Assert.NotNull(response);
            Assert.Equal(weeks, response.Weeks.Count);
        }
    }

    [Fact]
    public async Task RealPipeline_TenKSourced_EightWeekHorizon_Succeeds_ProvingTenKsOwnLowerMinimumIsActuallyUsed()
    {
        // The discriminating proof: 8 weeks is BELOW HALF_MARATHON's reused 10-week minimum but
        // EXACTLY AT TEN_K's own real 8-week minimum. Success here proves the generalized
        // DerivedDistanceHorizonAuthority is actually resolving TEN_K's own TenKMinimumCoreWeeks=8
        // triple for this cell, not silently falling back to the HALF_MARATHON-sourced interval's
        // 10/12/14 triple.
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedTenKSourcedRequest(7.0, 4, 8, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
        Assert.Equal(8, response.Weeks.Count);
    }

    [Fact]
    public async Task RealPipeline_ExactTenK_StillNeverRoutesDerived_ZeroDeltaForCanonicalTenK()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedTenKSourcedRequest(10.0, 4, 12, start);
        request.TargetDistanceKmOverride = null; // canonical TEN_K request never carries an override

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
        Assert.Null(response.RequestedTargetDistanceKm);
    }

    [Fact]
    public async Task RealPipeline_ExactHalfMarathon_And_LiveDerivedInterval_Unaffected_ByTenKActivation()
    {
        // Zero-delta proof for the LIVE 10K-HM derived interval: activating the TEN_K-sourced
        // interval must not change an 11K HALF_MARATHON-sourced derived request's behavior --
        // it behaves exactly as DERIVED-DIST.3 already proved.
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.HalfMarathon,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = start,
            RaceDate = start.AddDays(12 * 7),
            TargetFinishTimeSeconds = 5400,
            TargetFinishTimeSource = TargetFinishTimeSource.UserDefined,
            TargetDistanceKmOverride = 11.0,
            PreferredDays = [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun],
            LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 25.0,
            RecentLongestRunKm = 10.0,
            RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 2700, RaceDate = start.AddDays(-21) },
        };

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
        Assert.Equal(11.0, response.RequestedTargetDistanceKm);
        Assert.Equal(GoalDistance.HalfMarathon, response.GoalDistance);
    }

    // ───────────────────────── Persistence prototype, 7K representative ─────────────────────────

    [Fact]
    public async Task PersistencePrototype_7K_PreviewConfirmReadBack_ExactTargetIdentitySurvives()
    {
        // In-memory EF Core round-trip (preview -> confirm -> TrainingPlan read-back), NOT real
        // PostgreSQL -- a narrower proof than DERIVED-DIST.3's own real-Postgres persistence
        // sweep for the live HM interval, but still a REAL PlanServices.ConfirmPlanAsync call
        // through CatalogPlanConfirmationService, never a hand-rolled test-only writer.
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedTenKSourcedRequest(7.0, 4, 12, start);
        var userId = Guid.NewGuid();

        var preview = await service.GeneratePreviewAsync(userId, request);
        Assert.Equal(7.0, preview.RequestedTargetDistanceKm);

        var confirm = await service.ConfirmPlanAsync(userId, new ConfirmPlanRequest { PreviewId = preview.PreviewId });
        Assert.Equal("active", confirm.Status, ignoreCase: true);

        var persisted = await context.TrainingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == confirm.PlanId);
        Assert.NotNull(persisted);
        Assert.Equal(7.0, persisted!.RequestedTargetDistanceKm);
        Assert.Equal(GoalDistance.TenK, persisted.GoalDistance);

        _output.WriteLine($"7K TEN_K-sourced persistence: PlanId={confirm.PlanId}, RequestedTargetDistanceKm={persisted.RequestedTargetDistanceKm}, GoalDistance={persisted.GoalDistance}");
    }

    // ───────────────────────── Three-model boundary: 9.9 / 10.0 / 10.1 ─────────────────────────

    [Fact]
    public async Task Boundary_NineNine_TenZero_TenOne_ThreeModelComparison_NoSourceCrossover()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);

        // 9.9 -- TEN_K-sourced derived (now public).
        var r99 = await service.GeneratePreviewAsync(Guid.NewGuid(), DerivedTenKSourcedRequest(9.9, 4, 12, start));
        Assert.Equal(9.9, r99.RequestedTargetDistanceKm);
        Assert.Equal(GoalDistance.TenK, r99.GoalDistance);

        // 10.0 -- canonical TEN_K, no override, must never route derived.
        var exactTenKRequest = DerivedTenKSourcedRequest(10.0, 4, 12, start);
        exactTenKRequest.TargetDistanceKmOverride = null;
        var r100 = await service.GeneratePreviewAsync(Guid.NewGuid(), exactTenKRequest);
        Assert.Null(r100.RequestedTargetDistanceKm);
        Assert.Equal(GoalDistance.TenK, r100.GoalDistance);

        // 10.1 -- the LIVE production HALF_MARATHON-sourced derived interval (DERIVED-DIST.0-.3),
        // completely independent of the TEN_K-sourced interval's own activation -- must still route to HALF_MARATHON.
        var hmSourcedRequest = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.HalfMarathon,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = start,
            RaceDate = start.AddDays(12 * 7),
            TargetFinishTimeSeconds = 5400,
            TargetFinishTimeSource = TargetFinishTimeSource.UserDefined,
            TargetDistanceKmOverride = 10.1,
            PreferredDays = [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun],
            LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = 25.0,
            RecentLongestRunKm = 10.0,
            RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 2700, RaceDate = start.AddDays(-21) },
        };
        var r101 = await service.GeneratePreviewAsync(Guid.NewGuid(), hmSourcedRequest);
        Assert.Equal(10.1, r101.RequestedTargetDistanceKm);
        Assert.Equal(GoalDistance.HalfMarathon, r101.GoalDistance);

        var peak99 = r99.Weeks.Select(w => w.Days.Sum(d => d.DistanceKm)).Max();
        var peak101 = r101.Weeks.Select(w => w.Days.Sum(d => d.DistanceKm)).Max();
        _output.WriteLine($"9.9 (TEN_K-sourced, PUBLIC): weeks={r99.Weeks.Count}, peakWeeklyVolumeKm={peak99:0.00}");
        _output.WriteLine($"10.0 (canonical TEN_K): weeks={r100.Weeks.Count}");
        _output.WriteLine($"10.1 (HALF_MARATHON-sourced, LIVE): weeks={r101.Weeks.Count}, peakWeeklyVolumeKm={peak101:0.00}");

        // No source crossover: 9.9 must never carry HALF_MARATHON identity; 10.1 must never carry TEN_K identity.
        Assert.NotEqual(GoalDistance.HalfMarathon, r99.GoalDistance);
        Assert.NotEqual(GoalDistance.TenK, r101.GoalDistance);
    }

    // ───────────────────────── Decimal 5K-10K proof ─────────────────────────

    [Theory]
    [InlineData(5.1)]
    [InlineData(7.5)]
    public async Task RealPipeline_DecimalTargets_TenKSourced_ExactTargetIdentitySurvives_NoSnapping(double km)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DerivedTenKSourcedRequest(km, 4, 12, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
        Assert.Equal(km, response.RequestedTargetDistanceKm);
        Assert.Equal(GoalDistance.TenK, response.GoalDistance);
    }
}
