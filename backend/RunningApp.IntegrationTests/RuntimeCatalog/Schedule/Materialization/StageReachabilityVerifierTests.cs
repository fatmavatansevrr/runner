using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class StageReachabilityVerifierTests
{
    private readonly CatalogPhaseAllocationResolver _allocator = new();

    [Theory]
    [InlineData(8, 2)]
    [InlineData(9, 3)]
    [InlineData(10, 4)]
    [InlineData(11, 4)]
    [InlineData(12, 4)]
    [InlineData(13, 4)]
    [InlineData(14, 4)]
    public async Task Verify_RealEightThroughFourteenMatrix_PassesUsingActualScheduler(int targetWeeks, int rsWeeks)
    {
        var (candidate, progression) = await LoadRealAsync();
        var result = StageReachabilityVerifier.Verify(_allocator.Resolve(candidate, targetWeeks), progression,
            Conditions("REALISTIC"), candidate.SlotRoles);

        Assert.Equal(StageReachabilityOutcome.Pass, result.Outcome);
        Assert.Equal(4, result.PhaseResults.Count);
        var rs = Assert.Single(result.PhaseResults, p => p.PhaseKey == "RACE_SPECIFIC");
        Assert.Equal(rsWeeks, rs.AllocatedWeeks);
        Assert.Equal(rsWeeks, rs.ScheduledExposures.Count);
        Assert.All(result.PhaseResults, p => Assert.Contains(p.Findings, f => f.Code == StageReachabilityFindingCode.ExactStageReachabilityFit));
    }

    [Theory]
    [InlineData("REALISTIC")]
    [InlineData("CHALLENGING")]
    public async Task Verify_RealEightWeekEligiblePath_ProducesExactPrimarySequence(string value)
    {
        var (candidate, progression) = await LoadRealAsync();
        var result = StageReachabilityVerifier.Verify(_allocator.Resolve(candidate, 8), progression, Conditions(value), candidate.SlotRoles);
        var rs = Assert.Single(result.PhaseResults, p => p.PhaseKey == "RACE_SPECIFIC");

        Assert.Equal(["TEN_K_SPECIFIC_INTRO", "GOAL_PACE_REHEARSAL"], rs.ScheduledExposures.Select(e => e.EffectiveStageKey));
        Assert.All(rs.ScheduledExposures, e => Assert.False(e.UsedFallback));
    }

    [Theory]
    [InlineData("UNSUPPORTED")]
    [InlineData("NOT_REQUESTED")]
    public async Task Verify_RealEightWeekIneligiblePath_UsesOneFallbackExposureOnly(string value)
    {
        var (candidate, progression) = await LoadRealAsync();
        var result = StageReachabilityVerifier.Verify(_allocator.Resolve(candidate, 8), progression, Conditions(value), candidate.SlotRoles);
        var rs = Assert.Single(result.PhaseResults, p => p.PhaseKey == "RACE_SPECIFIC");

        Assert.Equal(StageReachabilityOutcome.Pass, result.Outcome);
        Assert.Equal(2, rs.ScheduledExposures.Count);
        Assert.Equal("TEN_K_SPECIFIC_INTRO", rs.ScheduledExposures[0].EffectiveStageKey);
        Assert.Equal("GOAL_PACE_REHEARSAL", rs.ScheduledExposures[1].RequestedStageKey);
        Assert.Equal("CURRENT_FITNESS_SPECIFIC_REHEARSAL", rs.ScheduledExposures[1].EffectiveStageKey);
        Assert.True(rs.ScheduledExposures[1].UsedFallback);
    }

    [Fact]
    public async Task Verify_NotEvaluatedCondition_UsesExistingAllocatorFallbackSemantics()
    {
        var (candidate, progression) = await LoadRealAsync();
        var condition = RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "MISSING_OPTIONAL_EVIDENCE");
        var result = StageReachabilityVerifier.Verify(_allocator.Resolve(candidate, 8), progression, [condition], candidate.SlotRoles);
        var rs = Assert.Single(result.PhaseResults, p => p.PhaseKey == "RACE_SPECIFIC");

        Assert.Equal(StageReachabilityOutcome.Pass, result.Outcome);
        Assert.Equal("CURRENT_FITNESS_SPECIFIC_REHEARSAL", rs.ScheduledExposures[1].EffectiveStageKey);
        Assert.True(rs.ScheduledExposures[1].UsedFallback);
    }

    [Fact]
    public async Task Verify_MissingConditionResult_IsDecisionRequired_NotGuessed()
    {
        var (candidate, progression) = await LoadRealAsync();
        var result = StageReachabilityVerifier.Verify(_allocator.Resolve(candidate, 8), progression, [], candidate.SlotRoles);

        Assert.Equal(StageReachabilityOutcome.DecisionRequired, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == StageReachabilityFindingCode.RuntimeConditionStageSelectionUnresolved);
    }

    [Fact]
    public void Verify_MinimumExposureGreaterThanOne_IsRepeated()
    {
        var result = VerifySynthetic(2, [Stage("REPEAT", min: 2, max: 2)]);
        var phase = Assert.Single(result.PhaseResults);

        Assert.Equal(StageReachabilityOutcome.Pass, result.Outcome);
        Assert.Equal(2, phase.ScheduledExposures.Count);
        Assert.Equal([1, 2], phase.ScheduledExposures.Select(e => e.ExposureOrdinal));
    }

    [Fact]
    public void Verify_PhaseBeyondCombinedMaximum_Fails()
    {
        var result = VerifySynthetic(3, [Stage("A", min: 1, max: 2)]);
        Assert.Equal(StageReachabilityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == StageReachabilityFindingCode.MaximumExposureExceeded);
    }

    [Fact]
    public void Inspect_MalformedSchedulerResultWithOrderInversion_Fails()
    {
        var progression = Progression([Stage("A", order: 1), Stage("B", order: 2)]);
        var schedule = Schedule(
            Scheduled(1, "B", order: 2),
            Scheduled(2, "A", order: 1));

        var result = InspectMalformed(Allocation(2), progression, schedule);

        Assert.Equal(StageReachabilityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == StageReachabilityFindingCode.RelativeOrderViolation);
    }

    [Fact]
    public void Inspect_MalformedSchedulerResultOmittingProtectedFixedStage_Fails()
    {
        var protectedStage = Stage("PROTECTED", min: 1, max: 1,
            compression: CatalogStageCompressionBehavior.Protected,
            extension: CatalogStageExtensionBehavior.FixedExposure);

        var result = InspectMalformed(Allocation(1), Progression([protectedStage]), Schedule());

        Assert.Equal(StageReachabilityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == StageReachabilityFindingCode.ProtectedOrFixedExposureOmitted
            && f.StageKey == "PROTECTED" && f.Required == 1 && f.Actual == 0);
    }

    [Fact]
    public void Verify_MissingFallbackTarget_FailsClosed()
    {
        var result = VerifySynthetic(1, [Stage("A", requires: [Condition()], fallback: "MISSING")]);
        Assert.Equal(StageReachabilityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == StageReachabilityFindingCode.FallbackTargetMissing);
    }

    [Fact]
    public void Verify_SelfReferencingFallback_FailsClosed()
    {
        var result = VerifySynthetic(1, [Stage("A", requires: [Condition()], fallback: "A")]);
        Assert.Equal(StageReachabilityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == StageReachabilityFindingCode.FallbackCycle);
    }

    [Fact]
    public void Verify_WeeklySlotContractWithTwoKeySessions_FailsClosed()
    {
        var result = StageReachabilityVerifier.Verify(Allocation(1), Progression([Stage("A")]), Conditions("REALISTIC"), ["KEY_SESSION", "KEY_SESSION"]);
        Assert.Equal(StageReachabilityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == StageReachabilityFindingCode.WeeklyStageSlotCapacityExceeded);
    }

    [Fact]
    public void Verify_MathematicallyInfeasible_IsNotApplicable()
    {
        var result = StageReachabilityVerifier.Verify(Allocation(0) with { IsMathematicallyFeasible = false }, Progression([Stage("A")]), [], Roles());
        Assert.Equal(StageReachabilityOutcome.NotApplicable, result.Outcome);
    }

    [Fact]
    public async Task Verify_RealTwelveWeekSequenceMatchesDirectExistingScheduler()
    {
        var (candidate, progression) = await LoadRealAsync();
        var allocation = _allocator.Resolve(candidate, 12);
        var verified = StageReachabilityVerifier.Verify(allocation, progression, Conditions("REALISTIC"), candidate.SlotRoles);

        var all = verified.PhaseResults.SelectMany(p => p.ScheduledExposures).Select(e => e.EffectiveStageKey).ToList();
        Assert.Equal(12, all.Count);
        Assert.Equal(["TEN_K_SPECIFIC_INTRO", "TEN_K_SPECIFIC_INTRO", "GOAL_PACE_REHEARSAL", "GOAL_PACE_REHEARSAL"],
            Assert.Single(verified.PhaseResults, p => p.PhaseKey == "RACE_SPECIFIC").ScheduledExposures.Select(e => e.EffectiveStageKey));
    }

    [Fact]
    public void Verify_IsDeterministicPureAndIndependent()
    {
        var allocation = Allocation(1); var progression = Progression([Stage("A")]); var conditions = Conditions("REALISTIC");
        var first = StageReachabilityVerifier.Verify(allocation, progression, conditions, Roles());
        var second = StageReachabilityVerifier.Verify(allocation, progression, conditions, Roles());
        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.PhaseResults.SelectMany(p => p.ScheduledExposures), second.PhaseResults.SelectMany(p => p.ScheduledExposures));

        var source = File.ReadAllText(Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "Materialization", "StageReachabilityVerifier.cs"));
        Assert.DoesNotContain("AllocationOrderCorrectnessVerifier", source);
        Assert.DoesNotContain("PhaseConstraintVerifier", source);
        Assert.DoesNotContain("RaceSpecificCapacityVerifier", source);
        Assert.DoesNotContain("activation-readiness-risks", source);
    }

    [Fact]
    public void StageReachabilityVerifier_HasNoProductionCallSite()
    {
        DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(nameof(StageReachabilityVerifier));
    }

    private static async Task<(PlanCatalogCandidateSummary, CatalogWorkoutProgressionDefinition)> LoadRealAsync()
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") });
        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance).LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        return (candidate, await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression));
    }

    private static StageReachabilityVerificationResult VerifySynthetic(int weeks, IReadOnlyList<CatalogWorkoutProgressionStage> stages) =>
        StageReachabilityVerifier.Verify(Allocation(weeks), Progression(stages), Conditions("UNSUPPORTED"), Roles());
    private static PhaseAllocationResult Allocation(int weeks) => new(weeks, weeks, 0, AllocationMode.Preferred,
        [new AllocatedPhase("RACE_SPECIFIC", weeks, 1, 20)], true, "MATHEMATICALLY_FEASIBLE");
    private static CatalogWorkoutProgressionDefinition Progression(IReadOnlyList<CatalogWorkoutProgressionStage> stages) => new()
    { Key = "SYNTHETIC", Version = 1, DistanceFamily = "TEN_K", PhaseProgressions = [new() { PhaseKey = "RACE_SPECIFIC", Stages = stages }] };
    private static CatalogWorkoutProgressionStage Stage(string key, int min = 1, int max = 2,
        IReadOnlyList<CatalogRuntimeEligibilityCondition>? requires = null, string? fallback = null, int order = 1,
        CatalogStageCompressionBehavior compression = CatalogStageCompressionBehavior.Compressible,
        CatalogStageExtensionBehavior extension = CatalogStageExtensionBehavior.Extendable) => new()
    { ProgressionStageKey = key, RelativeOrder = order, MinimumExposures = min, MaximumExposures = max,
      CompressionBehavior = compression, ExtensionBehavior = extension,
      Requires = requires ?? [], FallbackStageKey = fallback };
    private static CatalogRuntimeEligibilityCondition Condition() => new() { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } };
    private static IReadOnlyList<RuntimeConditionResolutionResult> Conditions(string value) => [RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", value, "TEST")];
    private static string[] Roles() => ["KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN"];

    private static StageReachabilityVerificationResult InspectMalformed(
        PhaseAllocationResult allocation, CatalogWorkoutProgressionDefinition progression, GeneratedCatalogStageSchedule schedule)
    {
        var method = typeof(StageReachabilityVerifier).GetMethod("Inspect", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (StageReachabilityVerificationResult)method.Invoke(null, [allocation, progression, schedule])!;
    }

    private static GeneratedCatalogStageSchedule Schedule(params ScheduledProgressionWeek[] weeks) => new()
    {
        CandidateKey = "SYNTHETIC", CandidateVersion = 1, ProgressionArtifactKey = "SYNTHETIC",
        ProgressionArtifactVersion = 1, AllocatorVersion = ProgressionStageAllocatorVersion.V1,
        Weeks = weeks, Trace = new StageAllocationDecisionTrace { Steps = [] },
    };

    private static ScheduledProgressionWeek Scheduled(int week, string key, int order) => new()
    {
        WeekNumber = week, PhaseKey = "RACE_SPECIFIC", ProgressionStageKey = key,
        StageRelativeOrder = order, ConditionOutcome = ProgressionStageEligibilityOutcome.NotConditioned,
        AllocationReason = "SYNTHETIC_MALFORMED_RESULT",
    };
}
