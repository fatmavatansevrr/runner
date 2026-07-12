using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog;

/// <summary>
/// Backend Integration Phase 1 — Process A bundle reader. All tests read the
/// REAL plan-catalog source tree read-only (no plan-catalog file is ever
/// written to by these tests) and prove parsing/identity/reference exposure
/// only. None of these tests generate a schedule, a TrainingWeek/TrainingDay,
/// or claim TEN_K__4D__INTERMEDIATE is runtime-ready.
/// </summary>
public sealed class PlanCatalogBundleLoaderTests
{
    private const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    private const int CandidateVersion = 10;

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (expected a directory containing both 'backend' and 'plan-catalog').");
    }

    private static string RealCatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");

    private static PlanCatalogBundleLoader NewLoader(string catalogRootPath) =>
        new(Options.Create(new PlanCatalogOptions { CatalogRootPath = catalogRootPath }), NullLogger<PlanCatalogBundleLoader>.Instance);

    [Fact]
    public async Task LoadCandidateAsync_TenK4DIntermediateV10_ParsesSuccessfully()
    {
        var loader = NewLoader(RealCatalogRoot());

        var summary = await loader.LoadCandidateAsync(CandidateKey, CandidateVersion);

        Assert.NotNull(summary);
    }

    [Fact]
    public async Task LoadCandidateAsync_ExposesCandidateIdentity()
    {
        var loader = NewLoader(RealCatalogRoot());

        var summary = await loader.LoadCandidateAsync(CandidateKey, CandidateVersion);

        Assert.Equal(CandidateKey, summary.CandidateKey);
        Assert.Equal(CandidateVersion, summary.CandidateVersion);
        Assert.Equal("DRAFT", summary.CandidateStatus); // v10 has never been published/activated
        Assert.Equal("TEN_K", summary.CanonicalDistanceFamily);
        Assert.Equal("INTERMEDIATE", summary.Level);
        Assert.Equal(4, summary.DaysPerWeek);
    }

    [Fact]
    public async Task LoadCandidateAsync_ExposesMajorArtifactReferences()
    {
        var loader = NewLoader(RealCatalogRoot());

        var summary = await loader.LoadCandidateAsync(CandidateKey, CandidateVersion);

        Assert.Equal(new PlanCatalogReference("TEN_K_MASTER", 6), summary.MasterTemplate);
        Assert.Equal(new PlanCatalogReference("RUN_LAYOUT_4D", 2), summary.Layout);
        Assert.Equal(new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6), summary.LevelModifier);
        Assert.Equal(new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 5), summary.WorkoutProgression);
        Assert.Equal(new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2), summary.ProgressionModifier);
        Assert.Equal(new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4), summary.RulePack);
        Assert.Equal(new PlanCatalogReference("PEAK_VOLUME_BANDS_V1", 3), summary.PeakVolumeBandPolicy);
        Assert.Equal(new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2), summary.RuntimeConditionValueRegistry);
    }

    [Fact]
    public async Task LoadCandidateAsync_GoalPaceTenKV2IsReachableThroughReferencedWorkouts()
    {
        var loader = NewLoader(RealCatalogRoot());

        var summary = await loader.LoadCandidateAsync(CandidateKey, CandidateVersion);

        Assert.Contains(summary.ReferencedWorkouts, w => w.Key == "GOAL_PACE_TEN_K" && w.Version == 2);
    }

    [Fact]
    public async Task LoadCandidateAsync_ExposesPhaseKeysAndSlotRoles()
    {
        var loader = NewLoader(RealCatalogRoot());

        var summary = await loader.LoadCandidateAsync(CandidateKey, CandidateVersion);

        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, summary.PhaseKeys);
        Assert.Equal(new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" }, summary.SlotRoles);
    }

    // Backend Integration Phase 4F.3: PhaseAllocations (phaseKey + preferredWeeks,
    // previously discarded by the loader) must resolve directly from the real,
    // unmodified templates/ten-k-master.v6.json -- no hardcoded 3/4/4/1 anywhere
    // in the loader itself.
    [Fact]
    public async Task LoadCandidateAsync_ExposesPhaseAllocations_MatchingRepositoryPreferredWeeks()
    {
        var loader = NewLoader(RealCatalogRoot());

        var summary = await loader.LoadCandidateAsync(CandidateKey, CandidateVersion);

        Assert.Equal(
            new[]
            {
                new RunningApp.Application.RuntimeCatalog.PlanCatalogPhaseAllocation("FOUNDATION", 3),
                new RunningApp.Application.RuntimeCatalog.PlanCatalogPhaseAllocation("BUILD", 4),
                new RunningApp.Application.RuntimeCatalog.PlanCatalogPhaseAllocation("RACE_SPECIFIC", 4),
                new RunningApp.Application.RuntimeCatalog.PlanCatalogPhaseAllocation("TAPER", 1),
            },
            summary.PhaseAllocations);

        // The total must equal coreCycle.defaultWeeks, both read from the same file.
        Assert.Equal(12, summary.PhaseAllocations.Sum(p => p.PreferredWeeks));
        Assert.Equal(12, summary.CoreCycle.DefaultWeeks);
    }

    [Fact]
    public async Task LoadCandidateAsync_DoesNotMutateAnyPlanCatalogFile()
    {
        var catalogRoot = RealCatalogRoot();
        var combinationFile = Path.Combine(catalogRoot, "combinations", "ten-k-4d-intermediate.v10.json");
        var beforeHash = await File.ReadAllTextAsync(combinationFile);

        var loader = NewLoader(catalogRoot);
        await loader.LoadCandidateAsync(CandidateKey, CandidateVersion);

        var afterHash = await File.ReadAllTextAsync(combinationFile);
        Assert.Equal(beforeHash, afterHash);
    }

    [Fact]
    public async Task LoadCandidateAsync_MissingCatalogRoot_ThrowsPlanCatalogLoadException()
    {
        var loader = NewLoader(Path.Combine(Path.GetTempPath(), $"nonexistent-catalog-{Guid.NewGuid()}"));

        var ex = await Assert.ThrowsAsync<PlanCatalogLoadException>(
            () => loader.LoadCandidateAsync(CandidateKey, CandidateVersion));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadCandidateAsync_InvalidJson_ThrowsPlanCatalogLoadException()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"invalid-catalog-{Guid.NewGuid()}");
        var combinationsDir = Path.Combine(tempRoot, "combinations");
        Directory.CreateDirectory(combinationsDir);
        await File.WriteAllTextAsync(Path.Combine(combinationsDir, "broken.v1.json"), "{ this is not valid json ");

        try
        {
            var loader = NewLoader(tempRoot);

            var ex = await Assert.ThrowsAsync<PlanCatalogLoadException>(
                () => loader.LoadCandidateAsync(CandidateKey, CandidateVersion));

            Assert.Contains("invalid JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task LoadCandidateAsync_WrongExpectedVersion_ThrowsPlanCatalogLoadException()
    {
        var loader = NewLoader(RealCatalogRoot());

        var ex = await Assert.ThrowsAsync<PlanCatalogLoadException>(
            () => loader.LoadCandidateAsync(CandidateKey, 999));

        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public async Task LoadCandidateAsync_WrongExpectedKey_ThrowsPlanCatalogLoadException()
    {
        var loader = NewLoader(RealCatalogRoot());

        await Assert.ThrowsAsync<PlanCatalogLoadException>(
            () => loader.LoadCandidateAsync("NOT_A_REAL_CANDIDATE_KEY", 1));
    }
}
