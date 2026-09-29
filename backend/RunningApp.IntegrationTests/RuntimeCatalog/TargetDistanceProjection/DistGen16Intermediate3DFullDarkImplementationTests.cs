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
using Cell = RunningApp.Application.RuntimeCatalog.TargetDistanceProjection.Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell;

namespace RunningApp.IntegrationTests.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DIST-GEN.16 — dark-only <c>16K × Intermediate × 3D</c> full
/// implementation: the FIRST projected-distance × non-4D-frequency cell,
/// registered as a genuinely distinct <see cref="ProjectedTargetAuthority"/>
/// from 16K's own already-shipped 4D cell, resolved through the exact same
/// typed <see cref="ProjectedTargetAuthorityRegistry"/> boundary DIST-GEN.13
/// established. Every numeric value exercised here is consumed exactly as
/// frozen by DIST-GEN.15/15B — nothing here derives, adjusts, or re-decides
/// any number.
///
/// Mirrors <see cref="Dark16KTargetDistanceProjectionTests"/>'s own real-pipeline
/// harness pattern exactly, adapted for <c>DaysPerWeek = 3</c> and this cell's
/// own frozen authority table.
/// </summary>
public sealed class DistGen16Intermediate3DFullDarkImplementationTests
{
    private readonly ITestOutputHelper _output;
    public DistGen16Intermediate3DFullDarkImplementationTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    private static Cell I3DCell => new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3);
    private static Cell I4DCell => new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);

    // ── §6/§7 Registry orthogonality: two genuinely distinct authorities ─────

    [Fact]
    public void Registry_16KI3DAndI4D_ResolveAsGenuinelyDistinctAuthorities()
    {
        var i3D = ProjectedTargetAuthorityRegistry.TryResolve(I3DCell);
        var i4D = ProjectedTargetAuthorityRegistry.TryResolve(I4DCell);

        Assert.NotNull(i3D);
        Assert.NotNull(i4D);
        Assert.NotEqual(i3D!.Cell, i4D!.Cell);
        Assert.False(ReferenceEquals(i3D.VolumeSafetyPolicy, i4D.VolumeSafetyPolicy));

        // Different: peak-volume band, selected peak, calibration.
        Assert.Equal(28.0d, i3D.PeakVolumeBandMinimumKm);
        Assert.Equal(40.0d, i3D.PeakVolumeBandMaximumKm);
        Assert.Equal(34.0d, i3D.VolumeSafetyPolicy.ResolvedPeakReference.Value);
        Assert.Equal(20.0d, i3D.VolumeSafetyPolicy.GoldenFixtureStartingVolumeKm);

        Assert.Equal(34.0d, i4D.PeakVolumeBandMinimumKm);
        Assert.Equal(46.0d, i4D.PeakVolumeBandMaximumKm);
        Assert.Equal(40.0d, i4D.VolumeSafetyPolicy.ResolvedPeakReference.Value);
        Assert.Equal(23.5d, i4D.VolumeSafetyPolicy.GoldenFixtureStartingVolumeKm);

        // Different: absolute weekly cap and LR-share quadruple.
        Assert.Equal(2.0d, i3D.VolumeSafetyPolicy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(2.5d, i4D.VolumeSafetyPolicy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal((0.34d, 0.40d, 0.38d, 0.46d), (
            i3D.VolumeSafetyPolicy.LongRunPreferredMinimumShare, i3D.VolumeSafetyPolicy.LongRunPreferredMaximumShare,
            i3D.VolumeSafetyPolicy.LongRunSelectionShare, i3D.VolumeSafetyPolicy.LongRunHardCapShare));
        Assert.Equal((0.30d, 0.36d, 0.33d, 0.40d), (
            i4D.VolumeSafetyPolicy.LongRunPreferredMinimumShare, i4D.VolumeSafetyPolicy.LongRunPreferredMaximumShare,
            i4D.VolumeSafetyPolicy.LongRunSelectionShare, i4D.VolumeSafetyPolicy.LongRunHardCapShare));

        // Same: target-race-pace-driving TargetDistanceKm, horizon-preferred-max, absolute-peak-LR.
        Assert.Equal(i4D.Cell.TargetDistanceKm, i3D.Cell.TargetDistanceKm);
        Assert.Equal(i4D.PreferredCoreWeeks, i3D.PreferredCoreWeeks);
        Assert.Equal(i4D.MaximumCoreWeeks, i3D.MaximumCoreWeeks);
        Assert.Equal(i4D.MinimumCoreWeeks, i3D.MinimumCoreWeeks);
        Assert.Equal(15.0d, i3D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(15.0d, i4D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(i4D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm, i3D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);

        // RunsPerWeek is genuinely the discriminating field.
        Assert.Equal(3, i3D.Cell.RunsPerWeek);
        Assert.Equal(4, i4D.Cell.RunsPerWeek);
    }

    // ── Exact-cell provenance: disabling the registration fails clean, never falls back to HM I3D ──

    [Fact]
    public void Registry_WithoutThe16KI3DRegistration_ResolvesNull_NeverFallsBackToCanonicalHmI3D()
    {
        var allExceptI3D = ProjectedTargetAuthorityRegistry.All.Where(a => a.Cell != I3DCell).ToArray();
        Assert.Equal(ProjectedTargetAuthorityRegistry.All.Count - 1, allExceptI3D.Length);

        var rebuiltIndex = ProjectedTargetAuthorityRegistry.BuildIndex(allExceptI3D);
        Assert.False(rebuiltIndex.ContainsKey(I3DCell));
        Assert.True(rebuiltIndex.TryGetValue(I3DCell, out var missing) is false && missing is null);

        // Confirm the canonical HM I3D policy object was never silently
        // substituted into the rebuilt index -- lookup failure must be null,
        // never a fall-through to the canonical object.
        Assert.DoesNotContain(rebuiltIndex.Values, a => ReferenceEquals(a.VolumeSafetyPolicy, VolumeSafetyPolicy.HalfMarathonIntermediate3D));
    }

    [Fact]
    public void Registry_DuplicateRegistrationOfNew16KI3DCell_FailsLoudly()
    {
        var i3D = ProjectedTargetAuthorityRegistry.TryResolve(I3DCell);
        Assert.NotNull(i3D);
        var ex = Assert.Throws<InvalidOperationException>(() => ProjectedTargetAuthorityRegistry.BuildIndex([i3D!, i3D!]));
        Assert.Contains("registered more than once", ex.Message, StringComparison.Ordinal);
    }

    // ── No I4D contamination ──────────────────────────────────────────────

    [Fact]
    public void I3DAuthority_NeverResolvesAnyI4DNumericValue()
    {
        var authority = ProjectedTargetAuthorityRegistry.TryResolve(I3DCell)!;
        var policy = authority.VolumeSafetyPolicy;

        Assert.NotEqual(2.5d, policy.AbsoluteWeeklyIncrementCapKm);
        Assert.NotEqual(34.0d, authority.PeakVolumeBandMinimumKm);
        Assert.NotEqual(46.0d, authority.PeakVolumeBandMaximumKm);
        Assert.NotEqual(40.0d, policy.ResolvedPeakReference.Value);
        Assert.NotEqual(23.5d, policy.GoldenFixtureStartingVolumeKm);
        Assert.NotEqual(0.30d, policy.LongRunPreferredMinimumShare);
        Assert.NotEqual(0.36d, policy.LongRunPreferredMaximumShare);
        Assert.NotEqual(0.33d, policy.LongRunSelectionShare);
        Assert.NotEqual(0.40d, policy.LongRunHardCapShare);
    }

    // ── No HM-parent-canonical fallback (value equality != authority equality, DIST-GEN.15B §3) ──

    [Fact]
    public void I3DAuthority_ValuesEqualCanonicalHmI3D_ButIsAGenuinelyIndependentInstance()
    {
        var i3D = ProjectedTargetAuthorityRegistry.TryResolve(I3DCell)!;
        var canonicalHmI3D = VolumeSafetyPolicy.HalfMarathonIntermediate3D;

        // The three exact-cell fields numerically equal HM I3D's own (DIST-GEN.15B §3-4) ...
        Assert.Equal(canonicalHmI3D.GoldenFixtureStartingVolumeKm, i3D.VolumeSafetyPolicy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(canonicalHmI3D.ResolvedPeakReference.Value, i3D.VolumeSafetyPolicy.ResolvedPeakReference.Value);
        Assert.Equal(28.0d, TargetDistance16KIntermediate3DPeakVolumeBandPolicy.MinimumKm);
        Assert.Equal(40.0d, TargetDistance16KIntermediate3DPeakVolumeBandPolicy.MaximumKm);

        // ... but is NEVER the same object, and 16K's own PreferredAbsolutePeakLongRunKm/
        // PreferredCoreWeeks/MaximumCoreWeeks/AbsoluteWeeklyIncrementCapKm genuinely
        // differ from HM I3D's own -- proving this is an independent exact-cell
        // authority, not a reference to or fallback onto the canonical object.
        Assert.False(ReferenceEquals(i3D.VolumeSafetyPolicy, canonicalHmI3D));
        Assert.NotEqual(canonicalHmI3D.PreferredAbsolutePeakLongRunKm, i3D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(19.0d, canonicalHmI3D.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(15.0d, i3D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        // Absolute weekly cap happens to also numerically agree (both 2.0) --
        // another instance of value-equality that is not authority-equality.
        Assert.Equal(2.0d, canonicalHmI3D.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(2.0d, i3D.VolumeSafetyPolicy.AbsoluteWeeklyIncrementCapKm);

        // Structural source-text proof: 16K's own peak-volume-band/taper
        // EXECUTABLE CODE (doc comments stripped, since they legitimately
        // mention the canonical type by name for contrast/explanation) never
        // literally reads from the canonical HalfMarathonIntermediate3D static
        // member.
        foreach (var fileName in new[] { "TargetDistance16KIntermediate3DPeakVolumeBandPolicy.cs", "TargetDistance16KIntermediate3DTaperVolumePolicy.cs" })
        {
            var executableLines = File.ReadAllLines(Path.Combine(
                    TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application",
                    "RuntimeCatalog", "Prescription", "Volume", fileName))
                .Where(l => !l.TrimStart().StartsWith("///", StringComparison.Ordinal));
            var code = string.Join("\n", executableLines);
            Assert.DoesNotContain("VolumeSafetyPolicy.HalfMarathonIntermediate3D", code, StringComparison.Ordinal);
        }
    }

    // ── Wrong-cell negatives ──────────────────────────────────────────────

    [Theory]
    [InlineData(15.0, RunningBackground.Intermediate, 3)]
    [InlineData(18.0, RunningBackground.Intermediate, 3)]
    [InlineData(12.0, RunningBackground.Intermediate, 3)]
    [InlineData(20.0, RunningBackground.Intermediate, 3)]
    [InlineData(16.0, RunningBackground.Beginner, 3)]
    [InlineData(16.0, RunningBackground.Advanced, 3)]
    [InlineData(16.0, RunningBackground.Intermediate, 5)]
    [InlineData(16.0, RunningBackground.Intermediate, 6)]
    public void Registry_WrongCell_NeverResolves(double km, RunningBackground level, int runsPerWeek)
    {
        Assert.Null(ProjectedTargetAuthorityRegistry.TryResolve(new Cell(km, GoalDistance.HalfMarathon, level, runsPerWeek)));
        Assert.Null(Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(km, GoalDistance.HalfMarathon, level, runsPerWeek));
    }

    // ── Public registry state as of DIST-GEN.16 (this phase's own dark-only
    // implementation): 16K I3D was DARK ONLY at the time DIST-GEN.16 closed.
    // PHASE DIST-GEN.17 subsequently made this exact cell publicly reachable
    // via its own, separately-authored ApprovedPublicCells entry -- this test
    // is updated in place (not superseded by a new file) to describe the
    // registry's CURRENT state, per DIST-GEN.17's own zero-delta obligation
    // to this file. See DistGen17PublicActivationTests for the full public
    // activation proof.
    [Fact]
    public void PublicRegistry_16KI3D_NowPublic_AsOfDistGen17()
    {
        Assert.Contains(I3DCell, Dark16KPilotEligibilityPolicy.ApprovedPublicCells);
        Assert.Contains(I3DCell, Dark16KPilotEligibilityPolicy.ApprovedDarkCells);
        Assert.True(Dark16KPilotEligibilityPolicy.IsPubliclyEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
        // 16K's own 4D public grant remains unaffected.
        Assert.True(Dark16KPilotEligibilityPolicy.IsPubliclyEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        // Public registry now has 4 entries (15/16(4D)/18, all 4D, plus 16K I3D).
        Assert.Equal(4, Dark16KPilotEligibilityPolicy.ApprovedPublicCells.Count);
        Assert.Single(Dark16KPilotEligibilityPolicy.ApprovedPublicCells, c => c.RunsPerWeek == 3);
        Assert.Equal(3, Dark16KPilotEligibilityPolicy.ApprovedPublicCells.Count(c => c.RunsPerWeek == 4));
    }

    // ── Real production pipeline: full PlanServices.GeneratePreviewAsync ────

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

    private static GeneratePreviewRequest DarkRequest(int daysPerWeek, int targetWeekCount, DateOnly start) => new()
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
        TargetDistanceKmOverride = 16.0,
        PreferredDays = daysPerWeek == 3 ? [Weekday.Tue, Weekday.Thu, Weekday.Sun] : [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun],
        LongRunDay = Weekday.Sun,
        RecentWeeklyVolumeKm = 20,
        RecentLongestRunKm = 10,
        RecentRunsPerWeek = daysPerWeek,
        RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 2700, RaceDate = start.AddDays(-21) },
    };

    private static GeneratePreviewRequest Dark16KI3DRequest(int targetWeekCount, DateOnly start) => DarkRequest(3, targetWeekCount, start);

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task RealPipeline_EligibleHorizons_MaterializeSuccessfully(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KI3DRequest(weeks, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        Assert.NotNull(response);
        _output.WriteLine($"{weeks}W dark 16K I3D preview materialized OK.");
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public async Task RealPipeline_BelowMinimumHorizon_RejectedByOwnTyped16KI3DAuthority(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KI3DRequest(weeks, start);

        var ex = await Assert.ThrowsAsync<PlanCoreHorizonTooShortException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains($"DARK_16K_I3D_CORE_HORIZON_{weeks}_NOT_SUPPORTED", ex.Message);
        Assert.Contains("dark 16K Intermediate 3D", ex.Message);
        // Never the generic allocator's own below-sum-of-minimums message, never HM's own message, never 4D's own reason code.
        Assert.DoesNotContain("TARGET_BELOW_SUM_OF_MINIMUMS", ex.Message);
        Assert.DoesNotContain("HALF_MARATHON standalone core minimum (10", ex.Message);
        Assert.DoesNotContain("DARK_16K_CORE_HORIZON", ex.Message.Replace("DARK_16K_I3D_CORE_HORIZON", string.Empty));
    }

    [Fact]
    public async Task RealPipeline_AboveMaximumHorizon_RejectedByOwnTyped16KI3DAuthority()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KI3DRequest(15, start);

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_16K_I3D_CORE_HORIZON_15_NOT_SUPPORTED", ex.Message);
        Assert.IsNotType<PlanHorizonStartTooEarlyException>(ex);
    }

    [Fact]
    public async Task RealPipeline_ZeroDelta_16KI4D_StillMaterializesUnaffected()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = DarkRequest(4, 12, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
    }

    [Fact]
    public async Task RealPipeline_ZeroDelta_CanonicalHalfMarathon3D_StillSucceeds_NoOverride()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KI3DRequest(14, start);
        request.TargetDistanceKmOverride = null; // canonical HM 3D request, no dark override

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
    }

    // ── Direct-orchestrator golden traces: real volume/LR/taper materialization ──

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateVersion);
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

    private static DynamicCoreCalendarMaterializationContext Context(PlanCatalogCandidateSummary candidate, int targetWeekCount)
    {
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(targetWeekCount * 7 - 1);
        var request = Dark16KI3DRequest(targetWeekCount, start);

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Sunday],
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults =
            [
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME"),
            ],
            PreviewRequest = request,
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = 16.0, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon),
                StartDate = start, RaceDate = race,
                TargetFinishTimeSeconds = null, DaysPerWeek = 3, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new TargetDistance16KI3DAwarePeakVolumeBandLoaderTestAccessor(Options.Create(OptionsValue)),
        };
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI3DNumerics(int weeks)
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: weeks);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(weeks, prescribed.Weeks.Count);

        // Exactly 1 KEY + 1 EASY + 1 LONG per week, no KEY2/secondary-quality lane.
        Assert.All(prescribed.Weeks, w =>
        {
            Assert.Equal(3, w.Sessions.Count);
            Assert.Single(w.Sessions, s => s.StructuralRole == "KEY_SESSION");
            Assert.Single(w.Sessions, s => s.StructuralRole == "EASY_SUPPORT");
            Assert.Single(w.Sessions, s => s.StructuralRole == "LONG_RUN");
        });

        // Peak volume must reflect this cell's own 34.0km reference, never 16K I4D's 40.0 or HM4D's 43.0.
        var peak = volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm);
        Assert.True(peak <= 34.0d + 0.001d, $"weeks={weeks}: peak={peak} exceeds the frozen 34.0km ceiling.");
        Assert.NotEqual(40.0d, peak);
        Assert.NotEqual(43.0d, peak);

        // Long run respects the 0.34/0.40/0.38/0.46 shares and the 15.0km absolute ceiling.
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.PlannedLongRunDistanceKm < 16.0d);
            Assert.True(w.PlannedLongRunDistanceKm <= 15.0d + 0.001d);
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.46d + 0.001d);
        });

        // Non-chained taper: both weeks computed from the fixed 34.0km-or-below anchor.
        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm,
            "Taper must be non-chained/non-increasing.");

        // Weekly deltas respect the 2.0km absolute cap (16K I3D's own -- NOT I4D's 2.5).
        var ordered = volume.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        for (var i = 1; i < ordered.Length; i++)
        {
            if (!ordered[i].IsTaperWeek && !ordered[i - 1].IsTaperWeek)
            {
                Assert.True(ordered[i].PlannedWeeklyVolumeKm - ordered[i - 1].PlannedWeeklyVolumeKm <= 2.0d + 0.001d,
                    $"weeks={weeks}: weekly delta exceeds the 2.0km absolute cap.");
            }
        }

        foreach (var week in volume.WeeklyVolumePlan.Weeks)
        {
            var lr = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var phaseWeek = prescribed.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            _output.WriteLine($"weeks={weeks} W{week.WeekNumber:D2}|{phaseWeek.PhaseKey}|vol={week.PlannedWeeklyVolumeKm:F1}|lr={lr.PlannedLongRunDistanceKm:F1}|lrShare={lr.LongRunShareOfWeeklyVolume:P1}|taper={week.IsTaperWeek}");
        }
    }

    /// <summary>
    /// Explicit hard-cap-share clamp proof (DIST-GEN.15B §6): at the 34.0km
    /// peak, 34.0 x 0.46 = 15.64km would exceed the 15.0km absolute ceiling --
    /// this must clamp to 15.0km, never materialize 15.64km. Exercised at the
    /// 14W horizon, whose RACE_SPECIFIC-phase weeks are closest to the peak.
    /// </summary>
    [Fact]
    public async Task GoldenTrace_14W_HardCapShareClampsToAbsoluteCeiling_NeverExceedsIt()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 14);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        Assert.All(volume.LongRunProgression.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm <= 15.0d + 0.001d));
        // The unclamped hard-cap-share value (15.64km) must never appear.
        Assert.DoesNotContain(volume.LongRunProgression.Weeks, w => w.PlannedLongRunDistanceKm > 15.0d + 0.001d);
    }

    [Fact]
    public async Task NineWeekHorizon_RejectedTypedNot4DOrHmFallback()
    {
        var candidate = await CandidateAsync();
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, 9);
        // The generic allocator itself would reject 9W below the reused catalog
        // identity's own real minimum-week sum -- but the REAL production path
        // (PlanServices.GeneratePreviewAsync) rejects earlier still, via this
        // cell's own typed horizon authority (proven above by
        // RealPipeline_BelowMinimumHorizon_RejectedByOwnTyped16KI3DAuthority),
        // never reaching this allocator-level exception in production.
        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Contains("TARGET_BELOW_SUM_OF_MINIMUMS", allocation.ReasonCode);
    }

    // ── Cross-frequency comparison trace: 16K I3D vs I4D at a comparable 12W horizon ──

    [Fact]
    public async Task CrossFrequencyTrace_16KI3DAndI4D_At12W_ShareIdentityButDifferAuthority()
    {
        var i3DCandidate = await CandidateAsync();
        var i3DContext = Context(i3DCandidate, targetWeekCount: 12);
        var i3DResult = await Pipeline().MaterializeAsync(i3DContext);

        var i4DLoader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        var i4DCandidate = await new CatalogCandidateEligibilityGate(i4DLoader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateVersion);

        var start = new DateOnly(2026, 8, 3);
        var i4DRequest = DarkRequest(4, 12, start);
        var i4DContext = new DynamicCoreCalendarMaterializationContext
        {
            Candidate = i4DCandidate, TargetWeekCount = 12, StartDate = start, RaceDate = start.AddDays(12 * 7 - 1), AsOfDate = start,
            PreferredDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday],
            LongRunDayPreference = DayOfWeek.Sunday,
            ConditionResults =
            [
                RuntimeConditionResolutionResult.Evaluated("PACE_SOURCE_IN", "NONE", "NO_PACE_EVIDENCE"),
                RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", "NO_TARGET_TIME"),
            ],
            PreviewRequest = i4DRequest,
            ResolverInput = new ResolverInputSnapshot
            {
                RequestedTargetDistanceKm = 16.0, CanonicalDistanceFamily = "HALF_MARATHON", GoalType = GoalType.Race,
                GoalDistance = GoalDistance.HalfMarathon, GoalDistanceKm = GoalDistanceKm.Resolve(GoalDistance.HalfMarathon),
                StartDate = start, RaceDate = start.AddDays(12 * 7 - 1),
                TargetFinishTimeSeconds = null, DaysPerWeek = 4, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new TargetDistance16KAwarePeakVolumeBandLoaderCrossCheckAccessor(Options.Create(OptionsValue)),
        };
        var i4DResult = await Pipeline().MaterializeAsync(i4DContext);

        var i3DVolume = i3DResult.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var i4DVolume = i4DResult.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        // Identical identity fields.
        Assert.Equal(i4DContext.ResolverInput.RequestedTargetDistanceKm, i3DContext.ResolverInput.RequestedTargetDistanceKm);
        Assert.Equal(i4DContext.ResolverInput.CanonicalDistanceFamily, i3DContext.ResolverInput.CanonicalDistanceFamily);
        Assert.Equal(i4DContext.ResolverInput.Level, i3DContext.ResolverInput.Level);
        Assert.All(i3DVolume.LongRunProgression.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm <= 15.0d + 0.001d));
        Assert.All(i4DVolume.LongRunProgression.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm <= 15.0d + 0.001d));

        // Different: RunsPerWeek, RunLayout (session count), absolute cap, peak band/selected-peak/calibration.
        Assert.NotEqual(i4DContext.ResolverInput.DaysPerWeek, i3DContext.ResolverInput.DaysPerWeek);
        Assert.All(i3DResult.PrescriptionResult.FinalPrescribedPlan.Weeks, w => Assert.Equal(3, w.Sessions.Count));
        Assert.All(i4DResult.PrescriptionResult.FinalPrescribedPlan.Weeks, w => Assert.Equal(4, w.Sessions.Count));
        Assert.True(i3DVolume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm) <= 34.0d + 0.001d);
        Assert.True(i4DVolume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm) <= 40.0d + 0.001d);
    }

    // ── Canonical/projected zero-delta ───────────────────────────────────────

    [Fact]
    public void CanonicalZeroDelta_HmI3DAndI4D_TenKI3DAndI4D_UnaffectedByNewRegistration()
    {
        Assert.Equal(0.34d, VolumeSafetyPolicy.HalfMarathonIntermediate3D.LongRunPreferredMinimumShare);
        Assert.Equal(19.0d, VolumeSafetyPolicy.HalfMarathonIntermediate3D.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(34.0d, VolumeSafetyPolicy.HalfMarathonIntermediate3D.ResolvedPeakReference.Value);
        Assert.Equal(20.0d, VolumeSafetyPolicy.HalfMarathonIntermediate3D.GoldenFixtureStartingVolumeKm);

        Assert.Equal(43.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value);
        Assert.Equal(19.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4D.PreferredAbsolutePeakLongRunKm);

        Assert.Equal(22.5d, VolumeSafetyPolicy.ThreeDayIntermediate.ResolvedPeakReference.Value);
        Assert.Equal(2.0d, VolumeSafetyPolicy.ThreeDayIntermediate.AbsoluteWeeklyIncrementCapKm);

        Assert.Equal(38.0d, VolumeSafetyPolicy.Default.ResolvedPeakReference.Value);

        // 15K/16K/18K's own I4D authority also unaffected.
        Assert.Equal(14.5d, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(15.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(16.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K.PreferredAbsolutePeakLongRunKm);
    }
}

/// <summary>Test-only accessor mirroring the production <see cref="TargetDistance16KAwarePeakVolumeBandLoader"/> decision for direct-orchestrator 16K I3D tests that bypass <see cref="CatalogPreviewGenerator"/>.</summary>
internal sealed class TargetDistance16KI3DAwarePeakVolumeBandLoaderTestAccessor : ICatalogPeakVolumeBandLoader
{
    private readonly ICatalogPeakVolumeBandLoader _inner;
    public TargetDistance16KI3DAwarePeakVolumeBandLoaderTestAccessor(IOptions<PlanCatalogOptions> options) => _inner = new CatalogPeakVolumeBandLoader(options);

    public Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default) =>
        Task.FromResult(TargetDistance16KIntermediate3DPeakVolumeBandPolicy.Build(distanceFamily, experience, runsPerWeek, reference.Key, reference.Version));
}

/// <summary>Same-shape accessor for the 16K I4D leg of the cross-frequency comparison trace.</summary>
internal sealed class TargetDistance16KAwarePeakVolumeBandLoaderCrossCheckAccessor : ICatalogPeakVolumeBandLoader
{
    private readonly ICatalogPeakVolumeBandLoader _inner;
    public TargetDistance16KAwarePeakVolumeBandLoaderCrossCheckAccessor(IOptions<PlanCatalogOptions> options) => _inner = new CatalogPeakVolumeBandLoader(options);

    public Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default) =>
        Task.FromResult(TargetDistance16KPeakVolumeBandPolicy.Build(distanceFamily, experience, runsPerWeek, reference.Key, reference.Version));
}
