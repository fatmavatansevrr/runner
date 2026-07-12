using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog;

/// <summary>
/// Backend Integration Phase 2 — catalog vocabulary mapping analysis.
/// PlanCatalogDomainMapper is analysis-only: these tests prove it classifies
/// concepts accurately from the real TEN_K__4D__INTERMEDIATE v10 summary,
/// and — just as importantly — that it never produces a TrainingWeek,
/// TrainingDay, or any other generation side effect.
/// </summary>
public sealed class PlanCatalogDomainMapperTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !(Directory.Exists(Path.Combine(dir.FullName, "backend")) && Directory.Exists(Path.Combine(dir.FullName, "plan-catalog"))))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root.");
    }

    private static async Task<PlanCatalogMappingResult> MapRealV10Async()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);

        var summary = await loader.LoadCandidateAsync("TEN_K__4D__INTERMEDIATE", 10);

        var mapper = new PlanCatalogDomainMapper(NullLogger<PlanCatalogDomainMapper>.Instance);
        return mapper.Map(summary);
    }

    [Fact]
    public async Task Map_ConsumesRealV10Summary_Succeeds()
    {
        var result = await MapRealV10Async();

        Assert.Equal("TEN_K__4D__INTERMEDIATE", result.CandidateKey);
        Assert.Equal(10, result.CandidateVersion);
        Assert.Equal(GoalDistance.TenK, result.CanonicalDistanceFamily);
    }

    [Fact]
    public async Task Map_ClassifiesGoalPaceTenK_AsRequiresNewField_AndNotCurrentlyRepresentable()
    {
        var result = await MapRealV10Async();

        Assert.False(result.CanRepresentGoalPaceTenK);
        var classification = Assert.Single(result.Classifications, c => c.Concept.StartsWith("GOAL_PACE_TEN_K"));
        Assert.Equal(BackendRepresentationSupport.RequiresNewField, classification.Support);
    }

    [Fact]
    public async Task Map_ClassifiesKeySession_AsNotSupported()
    {
        var result = await MapRealV10Async();

        Assert.False(result.CanRepresentKeySession);
        var classification = Assert.Single(result.Classifications, c => c.Concept == "KEY_SESSION");
        Assert.Equal(BackendRepresentationSupport.NotSupported, classification.Support);
    }

    [Fact]
    public async Task Map_ClassifiesEasySupport_AsNotSupported()
    {
        var result = await MapRealV10Async();

        Assert.False(result.CanRepresentEasySupport);
        var classification = Assert.Single(result.Classifications, c => c.Concept == "EASY_SUPPORT");
        Assert.Equal(BackendRepresentationSupport.NotSupported, classification.Support);
    }

    [Fact]
    public async Task Map_ClassifiesLongRun_AsNativeSupported()
    {
        var result = await MapRealV10Async();

        Assert.True(result.CanRepresentLongRun);
        var classification = Assert.Single(result.Classifications, c => c.Concept == "LONG_RUN");
        Assert.Equal(BackendRepresentationSupport.NativeSupported, classification.Support);
    }

    [Fact]
    public async Task Map_ClassifiesPhaseKeys_BuildAndTaperNative_FoundationAndRaceSpecificRequireExtension()
    {
        var result = await MapRealV10Async();

        // Not all 4 phase keys are natively representable today (FOUNDATION/RACE_SPECIFIC are not) —
        // so CanRepresentPhaseKeys must be false even though 2 of the 4 already match exactly.
        Assert.False(result.CanRepresentPhaseKeys);

        Assert.Equal(BackendRepresentationSupport.RequiresEnumExtension, Single(result, "FOUNDATION").Support);
        Assert.Equal(BackendRepresentationSupport.NativeSupported, Single(result, "BUILD").Support);
        Assert.Equal(BackendRepresentationSupport.RequiresEnumExtension, Single(result, "RACE_SPECIFIC").Support);
        Assert.Equal(BackendRepresentationSupport.NativeSupported, Single(result, "TAPER").Support);
    }

    [Fact]
    public async Task Map_ClassifiesRuntimeConditionGroups_AsNotSupported()
    {
        var result = await MapRealV10Async();

        Assert.False(result.CanRepresentRuntimeConditionValues);
        Assert.Contains(result.Classifications, c => c.Concept.StartsWith("PACE_SOURCE_IN") && c.Support == BackendRepresentationSupport.NotSupported);
        Assert.Contains(result.Classifications, c => c.Concept.StartsWith("TIME_ADEQUACY_IN") && c.Support == BackendRepresentationSupport.NotSupported);
        Assert.Contains(result.Classifications, c => c.Concept.StartsWith("CORE_ENTRY_READINESS_IN") && c.Support == BackendRepresentationSupport.NotSupported);
        Assert.Contains(result.Classifications, c => c.Concept.StartsWith("GOAL_FEASIBILITY_IN") && c.Support == BackendRepresentationSupport.NotSupported);
    }

    [Fact]
    public async Task Map_ReportsRequestedTargetDistanceKm_AsNotYetSupported()
    {
        var result = await MapRealV10Async();

        Assert.False(result.RequestedTargetDistanceKmPlaceholderSupported);
        var classification = Assert.Single(result.Classifications, c => c.Concept == "RequestedTargetDistanceKm");
        Assert.Equal(BackendRepresentationSupport.RequiresNewField, classification.Support);
    }

    [Fact]
    public async Task Map_ProducesNonEmptyKnownUnsupportedList()
    {
        var result = await MapRealV10Async();

        Assert.NotEmpty(result.KnownUnsupportedOrMissingRepresentations);
        Assert.Contains(result.KnownUnsupportedOrMissingRepresentations, s => s.Contains("KEY_SESSION"));
    }

    [Fact]
    public async Task Map_NeverReferencesTrainingWeekOrTrainingDayTypes_AndProducesNoSuchInstances()
    {
        // Structural guard: PlanCatalogMappingResult must not carry any TrainingWeek/TrainingDay
        // instances or collections thereof — this mapper is analysis-only.
        var resultType = typeof(PlanCatalogMappingResult);
        var propertyTypeNames = resultType.GetProperties().Select(p => p.PropertyType.FullName ?? p.PropertyType.Name);

        Assert.DoesNotContain(propertyTypeNames, n => n.Contains("TrainingWeek", StringComparison.Ordinal));
        Assert.DoesNotContain(propertyTypeNames, n => n.Contains("TrainingDay", StringComparison.Ordinal));

        var result = await MapRealV10Async();
        Assert.NotNull(result); // mapping succeeded without needing a database or generation engine at all
    }

    private static CatalogConceptClassification Single(PlanCatalogMappingResult result, string concept) =>
        Assert.Single(result.Classifications, c => c.Concept == concept);
}
