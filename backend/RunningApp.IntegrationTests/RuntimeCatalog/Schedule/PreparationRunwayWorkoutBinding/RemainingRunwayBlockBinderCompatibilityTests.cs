using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

/// <summary>
/// Backend Integration Phase 4G.6A.4C — proves the existing, unmodified
/// production binder (Phase 4G.6A.4B) and catalog reader consume the three
/// new CONSISTENCY/GENERAL_ENDURANCE/PRE_SPECIFIC_TRANSITION Preparation
/// Runway progression documents (Phase 4G.6A.4C) generically -- no binder
/// or reader source change was required or made.
/// </summary>
public sealed class RemainingRunwayBlockBinderCompatibilityTests
{
    private static string RepoRoot() => RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string RealCatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");

    private static ICatalogWorkoutDefinitionLoader RealWorkoutLoader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = RealCatalogRoot() }));

    // ══════════════════════════════════════════════════════════════════
    // CONSISTENCY: 0/1/2 success, 3 fails capacity.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Consistency_BinderResults_MatchTheApprovedShapeForAllSupportedAllocations()
    {
        var definition = await PreparationRunwayBlockProgressionCatalogReader.LoadAsync(RealCatalogRoot(), "TEN_K_CONSISTENCY_PROGRESSION", 1);

        var zero = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("CONSISTENCY", 0, definition));
        var one = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("CONSISTENCY", 1, definition));
        var two = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("CONSISTENCY", 2, definition));
        var three = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("CONSISTENCY", 3, definition));

        Assert.True(zero.IsSuccess);
        Assert.Empty(zero.Binding!.OrderedWorkoutReferences);

        Assert.True(one.IsSuccess);
        Assert.Equal(new[] { "EASY_STANDARD" }, one.Binding!.OrderedWorkoutReferences.Select(r => r.WorkoutId));

        Assert.True(two.IsSuccess);
        Assert.Equal(new[] { "EASY_STANDARD", "LONG_RUN_STANDARD" }, two.Binding!.OrderedWorkoutReferences.Select(r => r.WorkoutId));

        Assert.False(three.IsSuccess);
        Assert.Equal(PreparationRunwayWorkoutBindingFailureCode.ProgressionCapacityExceeded, three.FailureCode);
        Assert.Null(three.Binding);
    }

    // ══════════════════════════════════════════════════════════════════
    // GENERAL_ENDURANCE: 0..5 success, 6 fails capacity.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GeneralEndurance_BinderResults_MatchTheApprovedShapeForAllSupportedAllocations()
    {
        var definition = await PreparationRunwayBlockProgressionCatalogReader.LoadAsync(RealCatalogRoot(), "TEN_K_GENERAL_ENDURANCE_PROGRESSION", 1);

        for (var allocated = 0; allocated <= 5; allocated++)
        {
            var result = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("GENERAL_ENDURANCE", allocated, definition));
            Assert.True(result.IsSuccess);
            Assert.Equal(allocated, result.Binding!.OrderedWorkoutReferences.Count);
            Assert.All(result.Binding.OrderedWorkoutReferences, r => Assert.Equal("LONG_RUN_STANDARD", r.WorkoutId));
        }

        var six = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("GENERAL_ENDURANCE", 6, definition));
        Assert.False(six.IsSuccess);
        Assert.Equal(PreparationRunwayWorkoutBindingFailureCode.ProgressionCapacityExceeded, six.FailureCode);
        Assert.Null(six.Binding);
    }

    // ══════════════════════════════════════════════════════════════════
    // PRE_SPECIFIC_TRANSITION: 0/1 success, 2 fails capacity.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PreSpecificTransition_BinderResults_MatchTheApprovedShapeForAllSupportedAllocations()
    {
        var definition = await PreparationRunwayBlockProgressionCatalogReader.LoadAsync(RealCatalogRoot(), "TEN_K_PRE_SPECIFIC_TRANSITION_PROGRESSION", 1);

        var zero = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("PRE_SPECIFIC_TRANSITION", 0, definition));
        var one = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("PRE_SPECIFIC_TRANSITION", 1, definition));
        var two = PreparationRunwayBlockWorkoutBindingEngine.Bind(new PreparationRunwayBlockWorkoutBindingRequest<string>("PRE_SPECIFIC_TRANSITION", 2, definition));

        Assert.True(zero.IsSuccess);
        Assert.Empty(zero.Binding!.OrderedWorkoutReferences);

        Assert.True(one.IsSuccess);
        Assert.Equal(new[] { "EASY_STANDARD" }, one.Binding!.OrderedWorkoutReferences.Select(r => r.WorkoutId));

        Assert.False(two.IsSuccess);
        Assert.Equal(PreparationRunwayWorkoutBindingFailureCode.ProgressionCapacityExceeded, two.FailureCode);
        Assert.Null(two.Binding);
    }

    // ══════════════════════════════════════════════════════════════════
    // Catalog-aware workout-reference validation, reusing the existing
    // Phase 4G.6A.4B validator unmodified, against the new v5 workouts.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EasyStandardV5AndLongRunStandardV5_PassTheExistingReferenceValidator()
    {
        var loader = RealWorkoutLoader();
        Assert.Null(await PreparationRunwayBlockWorkoutReferenceValidator.ValidateAsync(loader, new PreparationRunwayWorkoutReference("EASY_STANDARD", 5)));
        Assert.Null(await PreparationRunwayBlockWorkoutReferenceValidator.ValidateAsync(loader, new PreparationRunwayWorkoutReference("LONG_RUN_STANDARD", 5)));
    }

    [Fact]
    public async Task EasyStandardV4AndLongRunStandardV4_StillFailTheReferenceValidator_NotYetRunwayEligible()
    {
        // v4 (the live, Core-consumed version) deliberately does NOT declare
        // PREPARATION_RUNWAY eligibility -- only the new v5 files do. This
        // proves the eligibility bump is version-scoped, not a blanket change.
        var loader = RealWorkoutLoader();
        Assert.Equal(PreparationRunwayWorkoutBindingFailureCode.WorkoutNotRunwayEligible,
            await PreparationRunwayBlockWorkoutReferenceValidator.ValidateAsync(loader, new PreparationRunwayWorkoutReference("EASY_STANDARD", 4)));
        Assert.Equal(PreparationRunwayWorkoutBindingFailureCode.WorkoutNotRunwayEligible,
            await PreparationRunwayBlockWorkoutReferenceValidator.ValidateAsync(loader, new PreparationRunwayWorkoutReference("LONG_RUN_STANDARD", 4)));
    }

    // ══════════════════════════════════════════════════════════════════
    // Genericity re-confirmation: the binder required zero source changes
    // to support these three additional blocks.
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void BinderSourceFile_WasNotModifiedForThisPhase_NoBlockNameSpecialCasing()
    {
        var path = Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayWorkoutBinding", "PreparationRunwayBlockWorkoutBindingEngine.cs");
        var source = File.ReadAllText(path);
        foreach (var forbidden in new[] { "\"CONSISTENCY\"", "\"GENERAL_ENDURANCE\"", "\"PRE_SPECIFIC_TRANSITION\"", "\"AEROBIC_STRENGTH\"" })
            Assert.DoesNotContain(forbidden, source);
    }
}
