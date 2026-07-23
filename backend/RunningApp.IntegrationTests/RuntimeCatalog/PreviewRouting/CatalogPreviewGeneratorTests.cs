using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — exercises the full catalog preview
/// generation pipeline (eligibility gate → resolver orchestration → immutable
/// snapshot) against the REAL plan-catalog source tree, using the gate's
/// internal-dry-run entry point (bypassing the PUBLISHED-only check, exactly
/// as documented on ICatalogCandidateEligibilityGate/CatalogPreviewGenerator)
/// since the real TEN_K__4D__INTERMEDIATE v10 candidate is DRAFT. Proves the
/// success path is fully implemented and that every resolver in one preview
/// shares a single AsOfDate, and that a public (non-dry-run) request against
/// the same DRAFT candidate fails loud with a typed error instead of ever
/// reaching SQL.
/// </summary>
public sealed class CatalogPreviewGeneratorTests
{
    /// <summary>Delegates public-preview loads straight to the internal-dry-run entry point — test-only, mirrors this phase's own documented "tests or an explicitly internal dry-run path" carve-out.</summary>
    private sealed class DryRunEligibilityGate : ICatalogCandidateEligibilityGate
    {
        private readonly ICatalogCandidateEligibilityGate _inner;
        public DryRunEligibilityGate(ICatalogCandidateEligibilityGate inner) => _inner = inner;

        public Task<PlanCatalogCandidateSummary> LoadForPublicPreviewAsync(string candidateKey, int candidateVersion, CancellationToken ct = default) =>
            _inner.LoadForInternalDryRunAsync(candidateKey, candidateVersion, ct);

        public Task<PlanCatalogCandidateSummary> LoadForInternalDryRunAsync(string candidateKey, int candidateVersion, CancellationToken ct = default) =>
            _inner.LoadForInternalDryRunAsync(candidateKey, candidateVersion, ct);
    }

    private static ICatalogCandidateEligibilityGate RealGate()
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return new CatalogCandidateEligibilityGate(bundleLoader);
    }

    private static RuntimeConditionResolutionService RealOrchestration() =>
        new(new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());

    /// <summary>Backend Integration Phase 4F.4 — the real, fully-composed Phase 4F.3 orchestrator, exactly as production DI wires it (see Program.cs).</summary>
    private static ICatalogPlanSkeletonOrchestrator RealSkeletonOrchestrator() => new CatalogPlanSkeletonOrchestrator(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    private static GeneratePreviewRequest PilotRequest(DateOnly raceDate) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = raceDate.AddDays(-84),
        RaceDate = raceDate,
        // TargetFinishTimeSeconds intentionally omitted: GoalFeasibilityResolver
        // short-circuits to Evaluated/NOT_REQUESTED immediately, so this request
        // reaches a fully Evaluated pipeline (no NotEvaluated results at all).
        // Backend Integration Phase 4F.5: a race-plan preview now also
        // dark-materializes a calendar-day assignment, which requires
        // PreferredDays/LongRunDay -- Mon/Wed/Fri/Sun with long run on Sunday
        // is a known-safe combination (see CatalogWeekSkeletonCalendarMaterializerTests).
        PreferredDays = new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = Weekday.Sun,
    };

    [Fact]
    public async Task GenerateAsync_ViaDryRunGate_ProducesFullyEvaluatedSnapshot_SharingOneAsOfDate()
    {
        var asOfDate = new DateOnly(2026, 1, 5);
        var raceDate = asOfDate.AddDays(84); // exactly 12 weeks -> meets ten-k-master.v6's defaultWeeks (ADEQUATE)
        var generator = new CatalogPreviewGenerator(new DryRunEligibilityGate(RealGate()), RealOrchestration(), RealSkeletonOrchestrator());

        var snapshot = await generator.GenerateAsync(PilotRequest(raceDate), asOfDate);

        // Item 10: a single shared AsOfDate reached every resolver in the pipeline.
        Assert.Equal(asOfDate, snapshot.AsOfDate);
        Assert.Equal(asOfDate, snapshot.NormalizedInput.StartDate);

        // Item 11: the snapshot freezes candidate identity, dependency identity,
        // resolver results, decision trace, generation source, and both timestamps.
        Assert.Equal("TEN_K__4D__INTERMEDIATE", snapshot.CandidateKey);
        Assert.Equal(10, snapshot.CandidateVersion);
        Assert.Equal("DRAFT", snapshot.CandidateStatusAtGenerationTime);
        Assert.Equal(GenerationSource.Catalog, snapshot.GenerationSource);
        Assert.Equal("PILOT_TEN_K_INTERMEDIATE_4D_MATCH", snapshot.RouteReason);
        Assert.Equal(4, snapshot.ResolverResults.Count);
        Assert.All(snapshot.ResolverResults, r => Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, r.Status));
        Assert.Equal("ADEQUATE", snapshot.ResolverResults.Single(r => r.ConditionType == "TIME_ADEQUACY_IN").OutputValue);
        Assert.Equal("NOT_REQUESTED", snapshot.ResolverResults.Single(r => r.ConditionType == "GOAL_FEASIBILITY_IN").OutputValue);
        Assert.Equal(4, snapshot.DecisionTrace.Steps.Count);
        Assert.Empty(snapshot.SelectedStageKeys);
        Assert.Empty(snapshot.FallbackStagesUsed);
        Assert.NotNull(snapshot.GeneratedPreviewPlanPayload);
        Assert.Equal(12, snapshot.GeneratedPreviewPlanPayload.PlannedWeekCount);
        Assert.Equal(48, snapshot.GeneratedPreviewPlanPayload.Weeks.Sum(w => w.Sessions.Count));
        Assert.True(new RunningApp.Application.RuntimeCatalog.Schedule.GeneratedCatalogPlanPayloadValidator()
            .Validate(snapshot.GeneratedPreviewPlanPayload).IsValid);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.ContentHash));
        Assert.True(snapshot.ExpiresAtUtc > snapshot.CreatedAtUtc);
        Assert.Equal(8, snapshot.ReferencedArtifacts.Count);
        Assert.Equal(6, snapshot.ReferencedArtifacts["masterTemplate"].Version);
    }

    [Fact]
    public async Task GenerateAsync_PublicPathAgainstRealDraftCandidate_ThrowsCatalogCandidateNotPublished_NeverProducesASnapshot()
    {
        // Item 3/4: the real gate (not the dry-run wrapper) enforces PUBLISHED-only
        // eligibility -- the resolver pipeline is never even reached.
        var generator = new CatalogPreviewGenerator(RealGate(), RealOrchestration(), RealSkeletonOrchestrator());

        await Assert.ThrowsAsync<CatalogCandidateNotPublishedException>(() =>
            generator.GenerateAsync(PilotRequest(new DateOnly(2026, 4, 1)), new DateOnly(2026, 1, 5)));
    }

    [Fact]
    public async Task GenerateAsync_RaceGoalMissingRaceDate_ThrowsPlanPreviewGenerationFailed_NotARawArgumentException()
    {
        // Item 9: TimeAdequacyResolver throws ArgumentException for a Race-goal
        // snapshot missing RaceDate -- CatalogPreviewGenerator must convert this
        // into an explicit typed failure, never let a raw framework exception
        // escape and never silently continue.
        var generator = new CatalogPreviewGenerator(new DryRunEligibilityGate(RealGate()), RealOrchestration(), RealSkeletonOrchestrator());
        var requestWithoutRaceDate = PilotRequest(new DateOnly(2026, 4, 1));
        requestWithoutRaceDate.RaceDate = null;

        await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(requestWithoutRaceDate, new DateOnly(2026, 1, 5)));
    }

    [Fact]
    public async Task Phase4F8_1_GenerateAsync_MaterializesPublicPayloadWithTaperSharpenSegments()
    {
        var asOfDate = new DateOnly(2026, 1, 5);
        var generator = new CatalogPreviewGenerator(new DryRunEligibilityGate(RealGate()), RealOrchestration(), RealSkeletonOrchestrator());

        var snapshot = await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);
        var payload = Assert.IsType<GeneratedCatalogPlanPayload>(snapshot.GeneratedPreviewPlanPayload);
        var taper = payload.Weeks.Single(w => w.Provenance.SourcePhaseKey == "TAPER")
            .Sessions.Single(s => s.Provenance.SourceLayoutSlotRole == "KEY_SESSION");

        Assert.Equal("TAPER_SHARPEN", taper.Provenance.SourceStageKey);
        Assert.Equal("EASY_STANDARD", taper.Provenance.SourceWorkoutKey);
        Assert.Equal(GeneratedCatalogWorkoutType.Easy, taper.WorkoutType);
        Assert.Equal(new[] { "EASY_BASELINE", "CONTROLLED_SHARPENING", "EASY_RECOVERY" }, taper.Segments.Select(s => s.DisplayText));
        Assert.Contains(taper.Segments, s => s.Intensity == "CONTROLLED_FAST_RELAXED");
        Assert.Equal(GeneratedCatalogPaceType.EffortOnly, taper.PacePrescription.PaceType);
        Assert.True(CatalogPreviewSnapshotVerifier.Verify(snapshot));
    }

    [Fact]
    public async Task Phase4F8_1_GenerateAsync_PublicPayloadIsDeterministicAndRoundTrips()
    {
        var asOfDate = new DateOnly(2026, 1, 5);
        var generator = new CatalogPreviewGenerator(new DryRunEligibilityGate(RealGate()), RealOrchestration(), RealSkeletonOrchestrator());

        var first = await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);
        var second = await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);
        var firstJson = System.Text.Json.JsonSerializer.Serialize(first.GeneratedPreviewPlanPayload);
        var secondJson = System.Text.Json.JsonSerializer.Serialize(second.GeneratedPreviewPlanPayload);
        var roundTrip = System.Text.Json.JsonSerializer.Deserialize<GeneratedCatalogPlanPayload>(firstJson);

        Assert.Equal(firstJson, secondJson);
        Assert.NotNull(roundTrip);
        Assert.True(new GeneratedCatalogPlanPayloadValidator().Validate(roundTrip).IsValid);
    }

    [Fact]
    public async Task Phase4F8_1_EightWeekExplicitZero_PropagatesTypedFailureAndProducesNoPayload()
    {
        var asOfDate = new DateOnly(2026, 1, 5);
        var request = PilotRequest(asOfDate.AddDays(55));
        request.RecentWeeklyVolumeKm = 0;
        var generator = new CatalogPreviewGenerator(new DryRunEligibilityGate(RealGate()), RealOrchestration(), RealSkeletonOrchestrator());

        var exception = await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(request, asOfDate));

        Assert.IsType<RunningApp.Application.RuntimeCatalog.Prescription.Session.CatalogSessionPrescriptionInfeasibleException>(exception.InnerException);
        Assert.Contains("8-week explicit-zero weekly-volume", exception.InnerException!.Message);
    }
}
