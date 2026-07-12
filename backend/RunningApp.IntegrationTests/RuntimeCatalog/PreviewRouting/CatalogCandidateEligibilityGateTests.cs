using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — proves the candidate/dependency lifecycle
/// eligibility gate: a DRAFT candidate cannot be used for public preview
/// (against the REAL, currently-DRAFT plan-catalog pilot candidate), and a
/// PUBLISHED candidate with an ineligible (non-PUBLISHED) dependency also
/// cannot be used (against a synthetic fixture, since no PUBLISHED-with-a-
/// draft-dependency candidate exists in the real catalog tree today).
/// </summary>
public sealed class CatalogCandidateEligibilityGateTests
{
    private static ICatalogCandidateEligibilityGate RealGate()
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = System.IO.Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return new CatalogCandidateEligibilityGate(bundleLoader);
    }

    [Fact]
    public async Task LoadForPublicPreviewAsync_RealDraftPilotCandidate_ThrowsCatalogCandidateNotPublished()
    {
        var gate = RealGate();

        var ex = await Assert.ThrowsAsync<CatalogCandidateNotPublishedException>(() =>
            gate.LoadForPublicPreviewAsync(PilotGenerationRouteDecider.PilotCandidateKey, PilotGenerationRouteDecider.PilotCandidateVersion));

        Assert.Contains("DRAFT", ex.Message);
    }

    [Fact]
    public async Task LoadForInternalDryRunAsync_RealDraftPilotCandidate_BypassesStatusCheck_LoadsSuccessfully()
    {
        var gate = RealGate();

        var summary = await gate.LoadForInternalDryRunAsync(PilotGenerationRouteDecider.PilotCandidateKey, PilotGenerationRouteDecider.PilotCandidateVersion);

        Assert.Equal("DRAFT", summary.CandidateStatus);
    }

    private sealed class FakeBundleLoader : IPlanCatalogBundleLoader
    {
        private readonly PlanCatalogCandidateSummary _summary;
        public FakeBundleLoader(PlanCatalogCandidateSummary summary) => _summary = summary;

        public Task<PlanCatalogCandidateSummary> LoadCandidateAsync(string candidateKey, int candidateVersion, CancellationToken ct = default) =>
            Task.FromResult(_summary);
    }

    private static PlanCatalogCandidateSummary PublishedCandidateWithDependencyStatus(string levelModifierStatus) => new()
    {
        CandidateKey = "TEN_K__4D__INTERMEDIATE",
        CandidateVersion = 10,
        CandidateStatus = "PUBLISHED",
        CanonicalDistanceFamily = "TEN_K",
        Level = "INTERMEDIATE",
        DaysPerWeek = 4,
        CoreCycle = new PlanCatalogCoreCycle(8, 12, 14),
        MasterTemplate = new PlanCatalogReference("ten-k-master", 6),
        Layout = new PlanCatalogReference("run-layout-4d", 2),
        LevelModifier = new PlanCatalogReference("intermediate-modifier", 6),
        WorkoutProgression = new PlanCatalogReference("ten-k-workout-progression", 5),
        ProgressionModifier = new PlanCatalogReference("intermediate-progression-modifier", 1),
        RulePack = new PlanCatalogReference("appsel-race-plan", 4),
        PeakVolumeBandPolicy = new PlanCatalogReference("peak-volume-band-policy", 1),
        RuntimeConditionValueRegistry = new PlanCatalogReference("runtime-condition-values", 2),
        DependencyStatuses = new Dictionary<string, string>
        {
            ["masterTemplate"] = "PUBLISHED",
            ["layout"] = "PUBLISHED",
            ["levelModifier"] = levelModifierStatus,
            ["rulePack"] = "PUBLISHED",
        },
        ReferencedWorkouts = System.Array.Empty<PlanCatalogReference>(),
        PhaseKeys = System.Array.Empty<string>(),
        PhaseAllocations = System.Array.Empty<PlanCatalogPhaseAllocation>(),
        SlotRoles = System.Array.Empty<string>(),
    };

    [Fact]
    public async Task LoadForPublicPreviewAsync_PublishedCandidateWithIneligibleDependency_ThrowsCatalogDependencyNotRuntimeEligible()
    {
        var loader = new FakeBundleLoader(PublishedCandidateWithDependencyStatus("DRAFT"));
        var gate = new CatalogCandidateEligibilityGate(loader);

        var ex = await Assert.ThrowsAsync<CatalogDependencyNotRuntimeEligibleException>(() =>
            gate.LoadForPublicPreviewAsync("TEN_K__4D__INTERMEDIATE", 10));

        Assert.Contains("levelModifier", ex.Message);
    }

    [Fact]
    public async Task LoadForPublicPreviewAsync_PublishedCandidateWithAllPublishedDependencies_Succeeds()
    {
        var loader = new FakeBundleLoader(PublishedCandidateWithDependencyStatus("PUBLISHED"));
        var gate = new CatalogCandidateEligibilityGate(loader);

        var summary = await gate.LoadForPublicPreviewAsync("TEN_K__4D__INTERMEDIATE", 10);

        Assert.Equal("PUBLISHED", summary.CandidateStatus);
    }
}
