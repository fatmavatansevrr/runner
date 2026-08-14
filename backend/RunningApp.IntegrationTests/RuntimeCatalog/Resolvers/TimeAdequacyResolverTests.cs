using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4D.1 (amended 4D.1.5, 4D.1.6) —
/// TimeAdequacyResolver tests. Uses the REAL TEN_K__4D__INTERMEDIATE v10
/// catalog candidate's real coreCycle (minimumWeeks=8, defaultWeeks=12,
/// maximumWeeks=14, read from templates/ten-k-master.v6.json), proving the
/// resolver consumes catalog-provided thresholds rather than
/// distance-family-specific literals. No TrainingWeek/TrainingDay is
/// created; no resolver output is wired into generation.
/// </summary>
public sealed class TimeAdequacyResolverTests
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

    private static async Task<PlanCatalogCandidateSummary> LoadRealTenKV10Async()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);

        return await loader.LoadCandidateAsync("TEN_K__4D__INTERMEDIATE", 10);
    }

    private static IRuntimeConditionRegistryReader NewRegistryReader() =>
        new RuntimeConditionRegistryReader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<RuntimeConditionRegistryReader>.Instance);

    private static readonly PlanCatalogReference RegistryRef = new("RUNTIME_CONDITION_VALUES_V1", 2);

    private static RuntimeResolverContext Context(ResolverInputSnapshot input, PlanCatalogCoreCycle? coreCycle) =>
        new() { InputSnapshot = input, CoreCycle = coreCycle };

    // ─── Interface implementation ───────────────────────────────────────────

    [Fact]
    public void TimeAdequacyResolver_ImplementsITimeAdequacyResolver()
    {
        Assert.IsAssignableFrom<ITimeAdequacyResolver>(new TimeAdequacyResolver());
    }

    [Fact]
    public void TimeAdequacyResolver_ConditionTypeIsTimeAdequacyIn()
    {
        Assert.Equal("TIME_ADEQUACY_IN", new TimeAdequacyResolver().ConditionType);
    }

    [Fact]
    public void TimeAdequacyResolver_Resolve_TakesSingleRuntimeResolverContextArgument()
    {
        // Structural proof there is exactly one Resolve method, matching
        // IRuntimeConditionResolver.Resolve(RuntimeResolverContext) -- no
        // condition-specific overload remains (Phase 4D.1.6).
        var resolveMethods = typeof(TimeAdequacyResolver).GetMethods()
            .Where(m => m.Name == "Resolve")
            .ToList();

        var resolveMethod = Assert.Single(resolveMethods);
        var parameters = resolveMethod.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(RuntimeResolverContext), parameters[0].ParameterType);
    }

    // ─── Dynamic source: loader summary exposes real coreCycle ─────────────

    [Fact]
    public async Task LoadCandidateAsync_ExposesRealCoreCycle_FromTenKMasterTemplate()
    {
        var summary = await LoadRealTenKV10Async();

        Assert.Equal(8, summary.CoreCycle.MinimumWeeks);
        Assert.Equal(12, summary.CoreCycle.DefaultWeeks);
        Assert.Equal(14, summary.CoreCycle.MaximumWeeks);
    }

    // ─── Boundary behavior (using the REAL loaded coreCycle, not literals) ──
    // All boundary tests use GoalType.Race with both dates present, so the
    // race-context missing-date branch never triggers here.

    [Theory]
    [InlineData(84, "ADEQUATE")]  // exactly 12 weeks (defaultWeeks)
    [InlineData(91, "ADEQUATE")]  // 13 weeks, above default
    [InlineData(98, "ADEQUATE")]  // exactly 14 weeks (maximumWeeks)
    [InlineData(56, "COMPRESSED")] // exactly 8 weeks (minimumWeeks)
    [InlineData(70, "COMPRESSED")] // 10 weeks, between minimum and default
    [InlineData(83, "COMPRESSED")] // 11 weeks + 6 days, one day short of default
    [InlineData(49, "INSUFFICIENT")] // exactly 7 weeks, below minimum
    [InlineData(7, "INSUFFICIENT")]  // 1 week
    public async Task Resolve_UsesRealCoreCycle_ProducesExpectedOutputValue(int availableDays, string expectedOutputValue)
    {
        var summary = await LoadRealTenKV10Async();
        var resolver = new TimeAdequacyResolver();
        var startDate = new DateOnly(2026, 8, 3);
        var input = new ResolverInputSnapshot
        {
            GoalType = GoalType.Race,
            StartDate = startDate,
            RaceDate = startDate.AddDays(availableDays),
        };

        var result = resolver.Resolve(Context(input, summary.CoreCycle));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal(expectedOutputValue, result.OutputValue);
    }

    [Fact]
    public async Task Resolve_ExactlyDefaultCoreWeeks_ReasonCodeIsMeetsDefaultCoreDuration()
    {
        var summary = await LoadRealTenKV10Async();
        var resolver = new TimeAdequacyResolver();
        var startDate = new DateOnly(2026, 8, 3);
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race, StartDate = startDate, RaceDate = startDate.AddDays(84) };

        var result = resolver.Resolve(Context(input, summary.CoreCycle));

        Assert.Equal("MEETS_DEFAULT_CORE_DURATION", result.ReasonCode);
    }

    [Fact]
    public async Task Resolve_ExactlyMinimumCoreWeeks_ReasonCodeIsBelowDefaultButMeetsMinimum()
    {
        var summary = await LoadRealTenKV10Async();
        var resolver = new TimeAdequacyResolver();
        var startDate = new DateOnly(2026, 8, 3);
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race, StartDate = startDate, RaceDate = startDate.AddDays(56) };

        var result = resolver.Resolve(Context(input, summary.CoreCycle));

        Assert.Equal("BELOW_DEFAULT_BUT_MEETS_MINIMUM_CORE_DURATION", result.ReasonCode);
    }

    [Fact]
    public async Task Resolve_BelowMinimumCoreWeeks_ReasonCodeIsBelowMinimumCoreDuration()
    {
        var summary = await LoadRealTenKV10Async();
        var resolver = new TimeAdequacyResolver();
        var startDate = new DateOnly(2026, 8, 3);
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race, StartDate = startDate, RaceDate = startDate.AddDays(49) };

        var result = resolver.Resolve(Context(input, summary.CoreCycle));

        Assert.Equal("BELOW_MINIMUM_CORE_DURATION", result.ReasonCode);
    }

    // ─── Metadata ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_MetadataContainsAvailableWeeksMinimumDefaultMaximumAndRoundingRule()
    {
        var summary = await LoadRealTenKV10Async();
        var resolver = new TimeAdequacyResolver();
        var startDate = new DateOnly(2026, 8, 3);
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race, StartDate = startDate, RaceDate = startDate.AddDays(84) };

        var result = resolver.Resolve(Context(input, summary.CoreCycle));
        var metadata = result.Metadata;

        Assert.Equal("12", metadata["availableWeeks"]);
        Assert.Equal("84", metadata["availableDays"]);
        Assert.Equal("0", metadata["remainingDays"]);
        Assert.Equal("8", metadata["minimumCoreWeeks"]);
        Assert.Equal("12", metadata["defaultCoreWeeks"]);
        Assert.Equal("14", metadata["maximumCoreWeeks"]);
        Assert.Equal("CORE_HORIZON_DECISION_FULL_WEEKS_AND_REMAINDER", metadata["roundingRule"]);
    }

    [Fact]
    public async Task Resolve_ConditionTypeIsAlwaysTimeAdequacyIn()
    {
        var summary = await LoadRealTenKV10Async();
        var resolver = new TimeAdequacyResolver();
        var startDate = new DateOnly(2026, 8, 3);
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race, StartDate = startDate, RaceDate = startDate.AddDays(84) };

        var result = resolver.Resolve(Context(input, summary.CoreCycle));

        Assert.Equal("TIME_ADEQUACY_IN", result.ConditionType);
    }

    // ─── Missing CoreCycle: resolver configuration/context error ───────────

    [Fact]
    public void Resolve_MissingCoreCycle_ThrowsInvalidOperationException_ConfigErrorNotNotEvaluated()
    {
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot
        {
            GoalType = GoalType.Race,
            StartDate = new DateOnly(2026, 8, 3),
            RaceDate = new DateOnly(2026, 10, 25),
        };

        // No CoreCycle attached to the context at all.
        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(Context(input, coreCycle: null)));
        Assert.Contains("CoreCycle", ex.Message);
    }

    [Fact]
    public void Resolve_MissingCoreCycle_TakesPrecedenceOverMissingDateCheck()
    {
        // Even when dates are also missing, the CoreCycle check fires first
        // (it's a resolver wiring error, checked before any user-evidence
        // question is even considered).
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot { GoalType = GoalType.Habit, StartDate = null, RaceDate = null };

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(Context(input, coreCycle: null)));
    }

    // ─── Missing / invalid input — race-plan context ────────────────────────

    [Fact]
    public void Resolve_RacePlan_MissingRaceDate_ThrowsArgumentException_InvalidInputNotNotEvaluated()
    {
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race, StartDate = new DateOnly(2026, 8, 3), RaceDate = null };

        Assert.Throws<ArgumentException>(() => resolver.Resolve(Context(input, new PlanCatalogCoreCycle(8, 12, 14))));
    }

    [Fact]
    public void Resolve_RacePlan_MissingStartDate_ThrowsArgumentException_InvalidInputNotNotEvaluated()
    {
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race, StartDate = null, RaceDate = new DateOnly(2026, 10, 25) };

        Assert.Throws<ArgumentException>(() => resolver.Resolve(Context(input, new PlanCatalogCoreCycle(8, 12, 14))));
    }

    [Fact]
    public void Resolve_RacePlan_RaceDateBeforeStartDate_ThrowsArgumentException()
    {
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot
        {
            GoalType = GoalType.Race,
            StartDate = new DateOnly(2026, 10, 25),
            RaceDate = new DateOnly(2026, 8, 3),
        };

        Assert.Throws<ArgumentException>(() => resolver.Resolve(Context(input, new PlanCatalogCoreCycle(8, 12, 14))));
    }

    // ─── Missing input — non-race / unknown context ─────────────────────────

    [Fact]
    public void Resolve_HabitPlan_MissingRaceDate_ReturnsNotEvaluated_NotApplicableNonRacePlan()
    {
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot { GoalType = GoalType.Habit, StartDate = new DateOnly(2026, 8, 3), RaceDate = null };

        var result = resolver.Resolve(Context(input, new PlanCatalogCoreCycle(8, 12, 14)));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("NOT_APPLICABLE_NON_RACE_PLAN", result.ReasonCode);
        Assert.Null(result.OutputValue);
    }

    [Fact]
    public void Resolve_HabitPlan_MissingBothDates_ReturnsNotEvaluated_NotApplicableNonRacePlan()
    {
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot { GoalType = GoalType.Habit, StartDate = null, RaceDate = null };

        var result = resolver.Resolve(Context(input, new PlanCatalogCoreCycle(8, 12, 14)));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("NOT_APPLICABLE_NON_RACE_PLAN", result.ReasonCode);
    }

    [Fact]
    public void Resolve_UnknownGoalType_MissingDates_ReturnsNotEvaluated_MissingPlanTypeContext()
    {
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot { GoalType = null, StartDate = null, RaceDate = null };

        var result = resolver.Resolve(Context(input, new PlanCatalogCoreCycle(8, 12, 14)));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.Equal("MISSING_PLAN_TYPE_CONTEXT", result.ReasonCode);
        Assert.Null(result.OutputValue);
    }

    [Fact]
    public void Resolve_HabitPlan_BothDatesPresent_StillEvaluatesNormally()
    {
        var resolver = new TimeAdequacyResolver();
        var startDate = new DateOnly(2026, 8, 3);
        var input = new ResolverInputSnapshot { GoalType = GoalType.Habit, StartDate = startDate, RaceDate = startDate.AddDays(84) };

        var result = resolver.Resolve(Context(input, new PlanCatalogCoreCycle(8, 12, 14)));

        Assert.Equal(RuntimeConditionResolutionStatus.Evaluated, result.Status);
        Assert.Equal("ADEQUATE", result.OutputValue);
    }

    // ─── Registry validation ─────────────────────────────────────────────────

    [Theory]
    [InlineData("ADEQUATE")]
    [InlineData("COMPRESSED")]
    [InlineData("INSUFFICIENT")]
    public async Task RegistryValidation_TimeAdequacyOutputValues_AreValid(string value)
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.True(snapshot.IsValidValue("TIME_ADEQUACY_IN", value));
    }

    [Fact]
    public async Task RegistryValidation_ReadinessOnly_IsRejectedAsTimeAdequacyOutputValue()
    {
        var snapshot = await NewRegistryReader().LoadAsync(RegistryRef);

        Assert.False(snapshot.IsValidValue("TIME_ADEQUACY_IN", "READINESS_ONLY"));
        Assert.True(snapshot.IsValidValue("PLAN_MODE_IN", "READINESS_ONLY"));
    }

    [Fact]
    public async Task RegistryValidation_ResolverProducedOutputValues_AreAllRegistryValid()
    {
        var summary = await LoadRealTenKV10Async();
        var registrySnapshot = await NewRegistryReader().LoadAsync(RegistryRef);
        var resolver = new TimeAdequacyResolver();
        var startDate = new DateOnly(2026, 8, 3);

        foreach (var days in new[] { 84, 56, 49 })
        {
            var input = new ResolverInputSnapshot { GoalType = GoalType.Race, StartDate = startDate, RaceDate = startDate.AddDays(days) };
            var result = resolver.Resolve(Context(input, summary.CoreCycle));

            Assert.True(registrySnapshot.IsValid(result));
        }
    }

    [Fact]
    public async Task RegistryValidation_NotEvaluatedResult_IsContractValid_NotLookedUpInRegistry()
    {
        var registrySnapshot = await NewRegistryReader().LoadAsync(RegistryRef);
        var resolver = new TimeAdequacyResolver();
        var input = new ResolverInputSnapshot { GoalType = GoalType.Habit, StartDate = null, RaceDate = null };

        var result = resolver.Resolve(Context(input, new PlanCatalogCoreCycle(8, 12, 14)));

        Assert.Equal(RuntimeConditionResolutionStatus.NotEvaluated, result.Status);
        Assert.True(registrySnapshot.IsValid(result));
    }

    // ─── RuntimeResolverContext contract shape ───────────────────────────────

    [Fact]
    public void RuntimeResolverContext_CarriesInputSnapshot()
    {
        var input = new ResolverInputSnapshot { GoalType = GoalType.Race };
        var context = new RuntimeResolverContext { InputSnapshot = input };

        Assert.Same(input, context.InputSnapshot);
        Assert.Null(context.CoreCycle);
    }

    [Fact]
    public void RuntimeResolverContext_CanCarryCoreCycle()
    {
        var coreCycle = new PlanCatalogCoreCycle(8, 12, 14);
        var context = new RuntimeResolverContext { InputSnapshot = new ResolverInputSnapshot(), CoreCycle = coreCycle };

        Assert.Equal(coreCycle, context.CoreCycle);
    }

    [Fact]
    public void ResolverInputSnapshot_HasNoCoreCycleProperty_CatalogContextStaysOffTheUserSnapshot()
    {
        // Boundary regression guard: PlanCatalogCoreCycle (catalog/policy
        // context) must never be absorbed into ResolverInputSnapshot (user
        // evidence) -- it belongs only on RuntimeResolverContext.
        var snapshotProperties = typeof(ResolverInputSnapshot).GetProperties().Select(p => p.PropertyType).ToList();

        Assert.DoesNotContain(snapshotProperties, t => t == typeof(PlanCatalogCoreCycle));
    }
}
