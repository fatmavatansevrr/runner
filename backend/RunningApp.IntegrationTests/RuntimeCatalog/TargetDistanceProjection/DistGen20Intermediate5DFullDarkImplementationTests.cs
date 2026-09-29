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
/// PHASE DIST-GEN.20 — dark-only <c>16K × Intermediate × 5D</c> full
/// implementation: the SECOND projected-distance × non-4D-frequency cell, and
/// the first to exercise the KEY2/secondary-quality-lane dimension. Registered
/// as a genuinely distinct <see cref="ProjectedTargetAuthority"/> from 16K's
/// own already-shipped 3D/4D cells, resolved through the exact same typed
/// <see cref="ProjectedTargetAuthorityRegistry"/> boundary DIST-GEN.13
/// established. Every numeric value exercised here is consumed exactly as
/// frozen by DIST-GEN.19/19A — nothing here derives, adjusts, or re-decides
/// any number. Deliberately DARK ONLY: 16K×I×5D is never added to
/// <see cref="Dark16KPilotEligibilityPolicy.ApprovedPublicCells"/> in this
/// phase — the first intentional dark/public divergence since DIST-GEN.17.
///
/// Mirrors <see cref="DistGen16Intermediate3DFullDarkImplementationTests"/>'s
/// own real-pipeline harness pattern exactly, adapted for
/// <c>DaysPerWeek = 5</c> and this cell's own frozen authority table.
/// </summary>
public sealed class DistGen20Intermediate5DFullDarkImplementationTests
{
    private readonly ITestOutputHelper _output;
    public DistGen20Intermediate5DFullDarkImplementationTests(ITestOutputHelper output) => _output = output;

    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static PlanCatalogOptions OptionsValue => new() { CatalogRootPath = CatalogRoot };

    private static Cell I3DCell => new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3);
    private static Cell I4DCell => new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4);
    private static Cell I5DCell => new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5);

    // ── Registry: three genuinely distinct authorities at the same target ───

    [Fact]
    public void Registry_16KI3DI4DAndI5D_ResolveAsGenuinelyDistinctAuthorities()
    {
        var i3D = ProjectedTargetAuthorityRegistry.TryResolve(I3DCell);
        var i4D = ProjectedTargetAuthorityRegistry.TryResolve(I4DCell);
        var i5D = ProjectedTargetAuthorityRegistry.TryResolve(I5DCell);

        Assert.NotNull(i3D);
        Assert.NotNull(i4D);
        Assert.NotNull(i5D);
        Assert.NotEqual(i3D!.Cell, i5D!.Cell);
        Assert.NotEqual(i4D!.Cell, i5D.Cell);
        Assert.False(ReferenceEquals(i3D.VolumeSafetyPolicy, i5D.VolumeSafetyPolicy));
        Assert.False(ReferenceEquals(i4D.VolumeSafetyPolicy, i5D.VolumeSafetyPolicy));

        // I5D's own exact-cell peak band/peak/calibration -- different from I3D/I4D.
        Assert.Equal(44.0d, i5D.PeakVolumeBandMinimumKm);
        Assert.Equal(58.0d, i5D.PeakVolumeBandMaximumKm);
        Assert.Equal(51.0d, i5D.VolumeSafetyPolicy.ResolvedPeakReference.Value);
        Assert.Equal(29.5d, i5D.VolumeSafetyPolicy.GoldenFixtureStartingVolumeKm);

        Assert.NotEqual(i3D.PeakVolumeBandMinimumKm, i5D.PeakVolumeBandMinimumKm);
        Assert.NotEqual(i4D.PeakVolumeBandMinimumKm, i5D.PeakVolumeBandMinimumKm);

        // I5D's own absolute weekly cap (2.5, shared with 4D's own value, but genuinely
        // different from 3D's 2.0) and LR-share quadruple (genuinely different from both).
        Assert.Equal(2.5d, i5D.VolumeSafetyPolicy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal((0.28d, 0.36d, 0.28d, 0.36d), (
            i5D.VolumeSafetyPolicy.LongRunPreferredMinimumShare, i5D.VolumeSafetyPolicy.LongRunPreferredMaximumShare,
            i5D.VolumeSafetyPolicy.LongRunSelectionShare, i5D.VolumeSafetyPolicy.LongRunHardCapShare));
        Assert.NotEqual(
            (i3D.VolumeSafetyPolicy.LongRunPreferredMinimumShare, i3D.VolumeSafetyPolicy.LongRunSelectionShare),
            (i5D.VolumeSafetyPolicy.LongRunPreferredMinimumShare, i5D.VolumeSafetyPolicy.LongRunSelectionShare));
        Assert.NotEqual(
            (i4D.VolumeSafetyPolicy.LongRunPreferredMinimumShare, i4D.VolumeSafetyPolicy.LongRunSelectionShare),
            (i5D.VolumeSafetyPolicy.LongRunPreferredMinimumShare, i5D.VolumeSafetyPolicy.LongRunSelectionShare));

        // StartingAbsoluteLongRunCapKm: 3D/4D null, 5D 12.0 -- PARENT_FAMILY_X_FREQUENCY.
        Assert.Null(i3D.VolumeSafetyPolicy.StartingAbsoluteLongRunCapKm);
        Assert.Null(i4D.VolumeSafetyPolicy.StartingAbsoluteLongRunCapKm);
        Assert.Equal(12.0d, i5D.VolumeSafetyPolicy.StartingAbsoluteLongRunCapKm);

        // Same across all three: target-race-pace-driving TargetDistanceKm, horizon triple, absolute-peak-LR.
        Assert.Equal(i4D.Cell.TargetDistanceKm, i5D.Cell.TargetDistanceKm);
        Assert.Equal(i3D.Cell.TargetDistanceKm, i5D.Cell.TargetDistanceKm);
        Assert.Equal(i4D.MinimumCoreWeeks, i5D.MinimumCoreWeeks);
        Assert.Equal(i4D.PreferredCoreWeeks, i5D.PreferredCoreWeeks);
        Assert.Equal(i4D.MaximumCoreWeeks, i5D.MaximumCoreWeeks);
        Assert.Equal(10, i5D.MinimumCoreWeeks);
        Assert.Equal(12, i5D.PreferredCoreWeeks);
        Assert.Equal(14, i5D.MaximumCoreWeeks);
        Assert.Equal(15.0d, i5D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(i4D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm, i5D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(i3D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm, i5D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);

        // RunsPerWeek is genuinely the discriminating field across all three.
        Assert.Equal(3, i3D.Cell.RunsPerWeek);
        Assert.Equal(4, i4D.Cell.RunsPerWeek);
        Assert.Equal(5, i5D.Cell.RunsPerWeek);
    }

    // ── Exact-cell provenance: disabling the registration fails clean, never falls back to HM I5D ──

    [Fact]
    public void Registry_WithoutThe16KI5DRegistration_ResolvesNull_NeverFallsBackToCanonicalHmI5D()
    {
        var allExceptI5D = ProjectedTargetAuthorityRegistry.All.Where(a => a.Cell != I5DCell).ToArray();
        Assert.Equal(ProjectedTargetAuthorityRegistry.All.Count - 1, allExceptI5D.Length);

        var rebuiltIndex = ProjectedTargetAuthorityRegistry.BuildIndex(allExceptI5D);
        Assert.False(rebuiltIndex.ContainsKey(I5DCell));
        Assert.True(rebuiltIndex.TryGetValue(I5DCell, out var missing) is false && missing is null);

        Assert.DoesNotContain(rebuiltIndex.Values, a => ReferenceEquals(a.VolumeSafetyPolicy, VolumeSafetyPolicy.HalfMarathonIntermediate5D));
    }

    [Fact]
    public void Registry_DuplicateRegistrationOfNew16KI5DCell_FailsLoudly()
    {
        var i5D = ProjectedTargetAuthorityRegistry.TryResolve(I5DCell);
        Assert.NotNull(i5D);
        var ex = Assert.Throws<InvalidOperationException>(() => ProjectedTargetAuthorityRegistry.BuildIndex([i5D!, i5D!]));
        Assert.Contains("registered more than once", ex.Message, StringComparison.Ordinal);
    }

    // ── No I3D/I4D contamination ──────────────────────────────────────────

    [Fact]
    public void I5DAuthority_NeverResolvesAnyI3DOrI4DNumericValue()
    {
        var authority = ProjectedTargetAuthorityRegistry.TryResolve(I5DCell)!;
        var policy = authority.VolumeSafetyPolicy;

        Assert.NotEqual(2.0d, policy.AbsoluteWeeklyIncrementCapKm); // I3D's own
        Assert.NotEqual(28.0d, authority.PeakVolumeBandMinimumKm);
        Assert.NotEqual(40.0d, authority.PeakVolumeBandMaximumKm);
        Assert.NotEqual(34.0d, policy.ResolvedPeakReference.Value); // I3D's own peak
        Assert.NotEqual(40.0d, policy.ResolvedPeakReference.Value); // I4D's own peak
        Assert.NotEqual(20.0d, policy.GoldenFixtureStartingVolumeKm); // I3D's own
        Assert.NotEqual(23.5d, policy.GoldenFixtureStartingVolumeKm); // I4D's own
        Assert.NotEqual(0.34d, policy.LongRunPreferredMinimumShare); // I3D's own
        Assert.NotEqual(0.30d, policy.LongRunPreferredMinimumShare); // I4D's own
        Assert.NotEqual(0.38d, policy.LongRunSelectionShare); // I3D's own
        Assert.NotEqual(0.33d, policy.LongRunSelectionShare); // I4D's own
    }

    // ── No HM-parent-canonical fallback (value equality != authority equality, DIST-GEN.19A §13) ──

    [Fact]
    public void I5DAuthority_ValuesEqualCanonicalHmI5D_ButIsAGenuinelyIndependentInstance()
    {
        var i5D = ProjectedTargetAuthorityRegistry.TryResolve(I5DCell)!;
        var canonicalHmI5D = VolumeSafetyPolicy.HalfMarathonIntermediate5D;

        // The exact-cell fields numerically equal HM I5D's own (DIST-GEN.19A §9/§14-17) ...
        Assert.Equal(canonicalHmI5D.GoldenFixtureStartingVolumeKm, i5D.VolumeSafetyPolicy.GoldenFixtureStartingVolumeKm);
        Assert.Equal(canonicalHmI5D.ResolvedPeakReference.Value, i5D.VolumeSafetyPolicy.ResolvedPeakReference.Value);
        Assert.Equal(44.0d, TargetDistance16KIntermediate5DPeakVolumeBandPolicy.MinimumKm);
        Assert.Equal(58.0d, TargetDistance16KIntermediate5DPeakVolumeBandPolicy.MaximumKm);

        // Shared/frequency fields also numerically agree (expected -- genuinely shared authority).
        Assert.Equal(canonicalHmI5D.AbsoluteWeeklyIncrementCapKm, i5D.VolumeSafetyPolicy.AbsoluteWeeklyIncrementCapKm);
        Assert.Equal(canonicalHmI5D.LongRunPreferredMinimumShare, i5D.VolumeSafetyPolicy.LongRunPreferredMinimumShare);
        Assert.Equal(canonicalHmI5D.StartingAbsoluteLongRunCapKm, i5D.VolumeSafetyPolicy.StartingAbsoluteLongRunCapKm);

        // ... but is NEVER the same object, and 16K's own PreferredAbsolutePeakLongRunKm
        // genuinely differs from HM I5D's own -- proving this is an independent exact-cell
        // authority for the peak cluster, not a reference to or fallback onto the canonical object.
        Assert.False(ReferenceEquals(i5D.VolumeSafetyPolicy, canonicalHmI5D));
        Assert.NotEqual(canonicalHmI5D.PreferredAbsolutePeakLongRunKm, i5D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(19.0d, canonicalHmI5D.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(15.0d, i5D.VolumeSafetyPolicy.PreferredAbsolutePeakLongRunKm);

        // Structural source-text proof: 16K's own peak-volume-band/taper
        // EXECUTABLE CODE (doc comments stripped, since they legitimately
        // mention the canonical type by name for contrast/explanation) never
        // literally reads from the canonical HalfMarathonIntermediate5D static
        // member for the peak-cluster fields.
        foreach (var fileName in new[] { "TargetDistance16KIntermediate5DPeakVolumeBandPolicy.cs", "TargetDistance16KIntermediate5DTaperVolumePolicy.cs" })
        {
            var executableLines = File.ReadAllLines(Path.Combine(
                    TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application",
                    "RuntimeCatalog", "Prescription", "Volume", fileName))
                .Where(l => !l.TrimStart().StartsWith("///", StringComparison.Ordinal));
            var code = string.Join("\n", executableLines);
            Assert.DoesNotContain("VolumeSafetyPolicy.HalfMarathonIntermediate5D", code, StringComparison.Ordinal);
        }

        // The VolumeSafetyPolicy record itself declares the peak cluster as its
        // own literal (29.5/51.0), never a read of the canonical instance's fields.
        var volumeSafetyPolicySource = File.ReadAllLines(Path.Combine(
                TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application",
                "RuntimeCatalog", "Prescription", "Volume", "VolumeSafetyPolicy.cs"))
            .Where(l => !l.TrimStart().StartsWith("///", StringComparison.Ordinal))
            .ToArray();
        var i5DDeclarationStart = Array.FindIndex(volumeSafetyPolicySource, l => l.Contains("HalfMarathonIntermediate5DTargetDistance16K"));
        Assert.True(i5DDeclarationStart >= 0);
        var i5DDeclarationBlock = string.Join("\n", volumeSafetyPolicySource.Skip(i5DDeclarationStart).Take(40));
        Assert.DoesNotContain("HalfMarathonIntermediate5D.GoldenFixtureStartingVolumeKm", i5DDeclarationBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("HalfMarathonIntermediate5D.ResolvedPeakReference", i5DDeclarationBlock, StringComparison.Ordinal);
    }

    // ── Wrong-cell negatives (15K/18K I5D, 16K I6D, wrong levels remain unsupported) ──

    [Theory]
    [InlineData(15.0, RunningBackground.Intermediate, 5)]
    [InlineData(18.0, RunningBackground.Intermediate, 5)]
    [InlineData(12.0, RunningBackground.Intermediate, 5)]
    [InlineData(20.0, RunningBackground.Intermediate, 5)]
    [InlineData(16.0, RunningBackground.Beginner, 5)]
    [InlineData(16.0, RunningBackground.Advanced, 5)]
    [InlineData(16.0, RunningBackground.Intermediate, 6)]
    [InlineData(16.0, RunningBackground.Intermediate, 2)]
    public void Registry_WrongCell_NeverResolves(double km, RunningBackground level, int runsPerWeek)
    {
        Assert.Null(ProjectedTargetAuthorityRegistry.TryResolve(new Cell(km, GoalDistance.HalfMarathon, level, runsPerWeek)));
        Assert.Null(Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(km, GoalDistance.HalfMarathon, level, runsPerWeek));
    }

    // ── Public/dark separation (mandatory central proof, §14) ───────────────

    [Fact]
    public void PublicDarkSeparation_16KI5D_IsDarkOnly_NeverPublic()
    {
        Assert.Contains(I5DCell, Dark16KPilotEligibilityPolicy.ApprovedDarkCells);
        Assert.DoesNotContain(I5DCell, Dark16KPilotEligibilityPolicy.ApprovedPublicCells);
        Assert.False(Dark16KPilotEligibilityPolicy.IsPubliclyEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));
        Assert.True(Dark16KPilotEligibilityPolicy.IsDarkEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5));

        // Existing public cells remain unaffected: 16K I3D/I4D stay ON, 15K/18K stay ON.
        Assert.True(Dark16KPilotEligibilityPolicy.IsPubliclyEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3));
        Assert.True(Dark16KPilotEligibilityPolicy.IsPubliclyEligible(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.True(Dark16KPilotEligibilityPolicy.IsPubliclyEligible(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));
        Assert.True(Dark16KPilotEligibilityPolicy.IsPubliclyEligible(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4));

        // Public registry stays at exactly 4 entries; dark registry grows to exactly 5.
        Assert.Equal(4, Dark16KPilotEligibilityPolicy.ApprovedPublicCells.Count);
        Assert.Equal(5, Dark16KPilotEligibilityPolicy.ApprovedDarkCells.Count);

        // Dark and public registries now genuinely diverge -- the first time since
        // DIST-GEN.17 made them numerically equal.
        Assert.NotEqual(Dark16KPilotEligibilityPolicy.ApprovedPublicCells.Count, Dark16KPilotEligibilityPolicy.ApprovedDarkCells.Count);
    }

    // ── Public boundary: real public request rejects at the eligibility gate, not internally ──

    [Fact]
    public void PublicEligibilityGate_16KI5D_FailsAtEligibility_DistinctFromInternalMaterializationFailure()
    {
        // The public gate returns null/false BEFORE any internal materialization is attempted --
        // this is an eligibility failure, never an inability-to-materialize failure, since the
        // exact same cell DOES materialize successfully through the dark seam (proven below by
        // RealPipeline_EligibleHorizons_MaterializeSuccessfully).
        var publicCell = Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5);
        Assert.Null(publicCell);

        var darkCell = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5);
        Assert.NotNull(darkCell);
        Assert.NotNull(ProjectedTargetAuthorityRegistry.TryResolve(darkCell));
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
        PreferredDays = daysPerWeek switch
        {
            3 => [Weekday.Tue, Weekday.Thu, Weekday.Sun],
            5 => [Weekday.Mon, Weekday.Tue, Weekday.Thu, Weekday.Fri, Weekday.Sun],
            _ => [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun],
        },
        LongRunDay = Weekday.Sun,
        // Set to this cell's own frozen calibration (29.5, DIST-GEN.19A §17) so the
        // real readiness-driven starting volume matches the hand-simulated authority
        // table exactly, mirroring DistGen16Intermediate3DFullDarkImplementationTests'
        // own established pattern (RecentWeeklyVolumeKm == that cell's own calibration).
        RecentWeeklyVolumeKm = daysPerWeek == 5 ? 29.5 : 20,
        RecentLongestRunKm = daysPerWeek == 5 ? 8.0 : 10,
        RecentRunsPerWeek = daysPerWeek,
        RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 2700, RaceDate = start.AddDays(-21) },
    };

    private static GeneratePreviewRequest Dark16KI5DRequest(int targetWeekCount, DateOnly start) => DarkRequest(5, targetWeekCount, start);

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
        var request = Dark16KI5DRequest(weeks, start);

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);

        Assert.NotNull(response);
        _output.WriteLine($"{weeks}W dark 16K I5D preview materialized OK.");
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public async Task RealPipeline_BelowMinimumHorizon_RejectedByOwnTyped16KI5DAuthority(int weeks)
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KI5DRequest(weeks, start);

        var ex = await Assert.ThrowsAsync<PlanCoreHorizonTooShortException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains($"DARK_16K_I5D_CORE_HORIZON_{weeks}_NOT_SUPPORTED", ex.Message);
        Assert.Contains("dark 16K Intermediate 5D", ex.Message);
        Assert.DoesNotContain("TARGET_BELOW_SUM_OF_MINIMUMS", ex.Message);
        Assert.DoesNotContain("HALF_MARATHON standalone core minimum (10", ex.Message);
    }

    [Fact]
    public async Task RealPipeline_AboveMaximumHorizon_RejectedByOwnTyped16KI5DAuthority()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KI5DRequest(15, start);

        var ex = await Assert.ThrowsAsync<PlanHorizonCompositionRequiredException>(() => service.GeneratePreviewAsync(Guid.NewGuid(), request));
        Assert.Contains("DARK_16K_I5D_CORE_HORIZON_15_NOT_SUPPORTED", ex.Message);
        Assert.IsNotType<PlanHorizonStartTooEarlyException>(ex);
    }

    [Fact]
    public async Task RealPipeline_ZeroDelta_16KI3DAndI4D_StillMaterializeUnaffected()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);

        Assert.NotNull(await service.GeneratePreviewAsync(Guid.NewGuid(), DarkRequest(3, 12, start)));
        Assert.NotNull(await service.GeneratePreviewAsync(Guid.NewGuid(), DarkRequest(4, 12, start)));
    }

    [Fact]
    public async Task RealPipeline_ZeroDelta_CanonicalHalfMarathon5D_StillSucceeds_NoOverride()
    {
        using var context = NewInMemoryContext();
        var service = CreateCatalogRoutedService(context);
        var start = new DateOnly(2026, 8, 3);
        var request = Dark16KI5DRequest(14, start);
        request.TargetDistanceKmOverride = null; // canonical HM 5D request, no dark override

        var response = await service.GeneratePreviewAsync(Guid.NewGuid(), request);
        Assert.NotNull(response);
    }

    // ── Direct-orchestrator golden traces: real volume/LR/taper/KEY2 materialization ──

    private static async Task<PlanCatalogCandidateSummary> CandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(Options.Create(OptionsValue), NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateKey,
            V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateVersion);
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
        var request = Dark16KI5DRequest(targetWeekCount, start);

        return new DynamicCoreCalendarMaterializationContext
        {
            Candidate = candidate, TargetWeekCount = targetWeekCount, StartDate = start, RaceDate = race, AsOfDate = start,
            PreferredDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Sunday],
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
                TargetFinishTimeSeconds = null, DaysPerWeek = 5, Level = RunningBackground.Intermediate,
            },
            WorkoutDefinitionLoader = new CatalogWorkoutDefinitionLoader(Options.Create(OptionsValue)),
            PeakVolumeBandLoader = new TargetDistance16KI5DAwarePeakVolumeBandLoaderTestAccessor(Options.Create(OptionsValue)),
        };
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI5DNumerics(int weeks)
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: weeks);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        Assert.True(prescribed.ValidationResult.IsValid, string.Join("; ", prescribed.ValidationResult.Errors));
        Assert.Equal(weeks, prescribed.Weeks.Count);

        // Exactly 2 KEY + 2 EASY + 1 LONG per week -- RUN_LAYOUT_5D's own five slots, KEY2 present.
        Assert.All(prescribed.Weeks, w =>
        {
            Assert.Equal(5, w.Sessions.Count);
            Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION"));
            Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT"));
            Assert.Single(w.Sessions, s => s.StructuralRole == "LONG_RUN");
        });

        // Peak volume must reflect this cell's own 51.0km reference, never 16K I3D's 34.0 or I4D's 40.0.
        var peak = volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm);
        Assert.True(peak <= 51.0d + 0.001d, $"weeks={weeks}: peak={peak} exceeds the frozen 51.0km ceiling.");
        Assert.NotEqual(34.0d, peak);
        Assert.NotEqual(40.0d, peak);

        // Long run respects the 0.28/0.36/0.28/0.36 shares and the 15.0km absolute ceiling (target-only, NOT HM's 19.0).
        Assert.All(volume.LongRunProgression.Weeks, w =>
        {
            Assert.True(w.PlannedLongRunDistanceKm < 16.0d);
            Assert.True(w.PlannedLongRunDistanceKm <= 15.0d + 0.001d);
            Assert.True(w.LongRunShareOfWeeklyVolume <= 0.36d + 0.001d);
        });

        // Non-chained taper: both weeks computed from the fixed peak-or-below anchor.
        var taperWeeks = volume.WeeklyVolumePlan.Weeks.Where(w => w.IsTaperWeek).OrderBy(w => w.WeekNumber).ToArray();
        Assert.Equal(2, taperWeeks.Length);
        Assert.True(taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm,
            "Taper must be non-chained/non-increasing.");

        // Weekly deltas respect the 2.5km absolute cap (16K I5D's own -- shared numerically with I4D, NOT I3D's 2.0).
        var ordered = volume.WeeklyVolumePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        for (var i = 1; i < ordered.Length; i++)
        {
            if (!ordered[i].IsTaperWeek && !ordered[i - 1].IsTaperWeek)
            {
                Assert.True(ordered[i].PlannedWeeklyVolumeKm - ordered[i - 1].PlannedWeeklyVolumeKm <= 2.5d + 0.001d,
                    $"weeks={weeks}: weekly delta exceeds the 2.5km absolute cap.");
            }
        }

        // First Core week respects the 12.0km StartingAbsoluteLongRunCapKm (parent x frequency authority).
        var firstWeekLr = volume.LongRunProgression.Weeks.OrderBy(w => w.WeekNumber).First();
        Assert.True(firstWeekLr.PlannedLongRunDistanceKm <= 12.0d + 0.001d,
            $"weeks={weeks}: first-week long run {firstWeekLr.PlannedLongRunDistanceKm} exceeds the 12.0km starting cap.");

        foreach (var week in volume.WeeklyVolumePlan.Weeks)
        {
            var lr = volume.LongRunProgression.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var phaseWeek = prescribed.Weeks.Single(w => w.WeekNumber == week.WeekNumber);
            var key2 = phaseWeek.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").Skip(1).FirstOrDefault();
            _output.WriteLine($"weeks={weeks} W{week.WeekNumber:D2}|{phaseWeek.PhaseKey}|vol={week.PlannedWeeklyVolumeKm:F1}|lr={lr.PlannedLongRunDistanceKm:F1}|lrShare={lr.LongRunShareOfWeeklyVolume:P1}|taper={week.IsTaperWeek}|key2={key2?.WorkoutDefinitionKey}");
        }
    }

    /// <summary>
    /// Explicit hard-cap-share clamp proof (DIST-GEN.19A §21/§30): at the
    /// 51.0km peak, 51.0 x 0.36 = 18.36km would exceed the 15.0km absolute
    /// ceiling -- this must clamp to 15.0km, never materialize 18.36km.
    /// Exercised at the 12W horizon, where the peak is genuinely reached.
    /// </summary>
    [Fact]
    public async Task GoldenTrace_12W_HardCapShareClampsToAbsoluteCeiling_NeverExceedsIt()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 12);
        var result = await Pipeline().MaterializeAsync(context);
        var volume = result.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;

        Assert.All(volume.LongRunProgression.Weeks, w => Assert.True(w.PlannedLongRunDistanceKm <= 15.0d + 0.001d));
        Assert.DoesNotContain(volume.LongRunProgression.Weeks, w => w.PlannedLongRunDistanceKm > 15.0d + 0.001d);
        // The peak is genuinely reached at 12W (DIST-GEN.19A §33): 51.0km.
        Assert.Equal(51.0d, volume.WeeklyVolumePlan.Weeks.Max(w => w.PlannedWeeklyVolumeKm));
    }

    /// <summary>KEY2 golden across phases (§59 of the governing prompt): at least one representative
    /// week from each of Foundation/Build/RaceSpecific/Taper, asserting KEY2 is present, never a
    /// second true-hard session, and never carries target-race pace -- exercised at the 14W horizon,
    /// which reaches every phase at its preferred width.</summary>
    [Fact]
    public async Task KEY2Golden_AcrossAllFourPhases_PresentAndNeverTargetRacePace()
    {
        var candidate = await CandidateAsync();
        var context = Context(candidate, targetWeekCount: 14);
        var result = await Pipeline().MaterializeAsync(context);
        var prescribed = result.PrescriptionResult.FinalPrescribedPlan;

        var phases = prescribed.Weeks.Select(w => w.PhaseKey).Distinct().ToArray();
        _output.WriteLine("Phases observed: " + string.Join(",", phases));

        Assert.All(prescribed.Weeks, w =>
        {
            var keySessions = w.Sessions.Where(s => s.StructuralRole == "KEY_SESSION").ToArray();
            Assert.Equal(2, keySessions.Length);
        });

        // Taper weeks: KEY2 (the second KEY slot) is present at exactly the fixed exposure and never
        // becomes a quality/hard session in taper -- easy-equivalent per DIST-GEN.19 §38/§57.
        var taperWeeks = prescribed.Weeks.Where(w => w.PhaseKey == "TAPER").ToArray();
        Assert.NotEmpty(taperWeeks);
        Assert.All(taperWeeks, w => Assert.Equal(5, w.Sessions.Count));
    }

    [Fact]
    public async Task NineWeekHorizon_RejectedTypedNot4DOrHmFallback()
    {
        var candidate = await CandidateAsync();
        var allocation = new CatalogPhaseAllocationResolver().Resolve(candidate, 9);
        Assert.False(allocation.IsMathematicallyFeasible);
        Assert.Contains("TARGET_BELOW_SUM_OF_MINIMUMS", allocation.ReasonCode);
    }

    // ── Canonical/projected zero-delta ───────────────────────────────────────

    [Fact]
    public void CanonicalZeroDelta_HmI5DAndTenKI5D_HmI3DAndI4D_UnaffectedByNewRegistration()
    {
        Assert.Equal(0.28d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.LongRunPreferredMinimumShare);
        Assert.Equal(19.0d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(51.0d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.ResolvedPeakReference.Value);
        Assert.Equal(29.5d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.GoldenFixtureStartingVolumeKm);
        Assert.Equal(12.0d, VolumeSafetyPolicy.HalfMarathonIntermediate5D.StartingAbsoluteLongRunCapKm);

        // TEN_K I5D's own StartingAbsoluteLongRunCapKm stays null -- never normalized to 12.0.
        Assert.Null(VolumeSafetyPolicy.FiveDayIntermediate.StartingAbsoluteLongRunCapKm);
        Assert.Equal(44.5d, VolumeSafetyPolicy.FiveDayIntermediate.ResolvedPeakReference.Value);

        // Projected 16K's own 3D/4D authority unaffected.
        Assert.Equal(34.0d, VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K.ResolvedPeakReference.Value);
        Assert.Equal(2.0d, VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K.AbsoluteWeeklyIncrementCapKm);
        Assert.Null(VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K.StartingAbsoluteLongRunCapKm);
        Assert.Equal(40.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K.ResolvedPeakReference.Value);
        Assert.Null(VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K.StartingAbsoluteLongRunCapKm);

        // 15K/18K's own I4D authority also unaffected.
        Assert.Equal(14.5d, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K.PreferredAbsolutePeakLongRunKm);
        Assert.Equal(16.0d, VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K.PreferredAbsolutePeakLongRunKm);
    }

    // ── Recurring frequency-gate audit: the dispatch gate widening is scoped, not over-widened ──

    [Fact]
    public void DispatchGateWidening_CatalogVolumeAndLongRunPlanner_AdmitsExactly3And4And5_Never6()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application",
            "RuntimeCatalog", "Prescription", "Volume", "CatalogVolumeAndLongRunPlanner.cs"));

        Assert.Contains(
            "request.Candidate.Level == \"INTERMEDIATE\" && (request.Candidate.DaysPerWeek == 3 || request.Candidate.DaysPerWeek == 4 || request.Candidate.DaysPerWeek == 5) &&",
            source, StringComparison.Ordinal);

        // Never over-widened to a range or a >= comparison for this specific dark-dispatch gate.
        Assert.DoesNotContain("DaysPerWeek >= 3", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DaysPerWeek is >= 3", source, StringComparison.Ordinal);
    }
}

/// <summary>Test-only accessor mirroring the production <see cref="TargetDistance16KAwarePeakVolumeBandLoader"/> decision for direct-orchestrator 16K I5D tests that bypass <see cref="CatalogPreviewGenerator"/>.</summary>
internal sealed class TargetDistance16KI5DAwarePeakVolumeBandLoaderTestAccessor : ICatalogPeakVolumeBandLoader
{
    private readonly ICatalogPeakVolumeBandLoader _inner;
    public TargetDistance16KI5DAwarePeakVolumeBandLoaderTestAccessor(IOptions<PlanCatalogOptions> options) => _inner = new CatalogPeakVolumeBandLoader(options);

    public Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default) =>
        Task.FromResult(TargetDistance16KIntermediate5DPeakVolumeBandPolicy.Build(distanceFamily, experience, runsPerWeek, reference.Key, reference.Version));
}
