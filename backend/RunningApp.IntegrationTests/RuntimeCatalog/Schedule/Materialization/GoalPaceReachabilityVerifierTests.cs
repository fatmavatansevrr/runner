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

public sealed class GoalPaceReachabilityVerifierTests
{
    private readonly CatalogPhaseAllocationResolver _allocator = new();
    private static readonly PlanCatalogReference RegistryRef = new("RUNTIME_CONDITION_VALUES_V1", 2);

    [Fact]
    public async Task Verify_RealEightWeekCandidate_EnumeratesEveryRegisteredValuePlusNotEvaluated_WithExactStatusAndReasonCode()
    {
        var (candidate, progression, goalPaceStage, registeredValues) = await LoadRealAsync();
        var allocation = _allocator.Resolve(candidate, 8);

        var result = GoalPaceReachabilityVerifier.Verify(allocation, progression, candidate.SlotRoles, goalPaceStage, registeredValues);

        // Real registry today: REALISTIC, CHALLENGING, UNSUPPORTED, NOT_REQUESTED (4) + NOT_EVALUATED (1) = 5.
        Assert.Equal(5, result.OutcomeChecks.Count);

        var byValue = result.OutcomeChecks.ToDictionary(c => c.GoalFeasibilityValue);

        Assert.Equal(GoalPaceOutcomeStatus.Eligible, byValue["CHALLENGING"].Status);
        Assert.Contains("within 'GOAL_PACE_REHEARSAL''s own AllowedValues", byValue["CHALLENGING"].ReasonCode);
        Assert.Contains("confirmed scheduled directly (no fallback)", byValue["CHALLENGING"].ReasonCode);

        Assert.Equal(GoalPaceOutcomeStatus.Eligible, byValue["REALISTIC"].Status);
        Assert.Contains("confirmed scheduled directly (no fallback)", byValue["REALISTIC"].ReasonCode);

        Assert.Equal(GoalPaceOutcomeStatus.FallbackConfirmed, byValue["UNSUPPORTED"].Status);
        Assert.Contains("FallbackStageKey='CURRENT_FITNESS_SPECIFIC_REHEARSAL'", byValue["UNSUPPORTED"].ReasonCode);

        Assert.Equal(GoalPaceOutcomeStatus.FallbackConfirmed, byValue["NOT_REQUESTED"].Status);
        Assert.Contains("FallbackStageKey='CURRENT_FITNESS_SPECIFIC_REHEARSAL'", byValue["NOT_REQUESTED"].ReasonCode);

        Assert.Equal(GoalPaceOutcomeStatus.UncertainNotEvaluated, byValue["NOT_EVALUATED"].Status);
        Assert.Contains("TD-NOTEVALUATED-FALLBACK-001", byValue["NOT_EVALUATED"].ReasonCode);
        Assert.Contains("not yet product-approved", byValue["NOT_EVALUATED"].ReasonCode);
    }

    [Fact]
    public async Task Verify_RealEightWeekCandidate_OverallOutcomeIsPassWithOpenRisk_NotPlainPass()
    {
        // Per the task's own rule: PassWithOpenRisk is the only honest result
        // today, since TD-NOTEVALUATED-FALLBACK-001 remains open. If this
        // assertion ever fails because the real result is Pass, that must be
        // treated as a bug in this verifier (a silently-swallowed
        // UncertainNotEvaluated case), not a genuine improvement -- see the
        // task's own explicit instruction.
        var (candidate, progression, goalPaceStage, registeredValues) = await LoadRealAsync();
        var allocation = _allocator.Resolve(candidate, 8);

        var result = GoalPaceReachabilityVerifier.Verify(allocation, progression, candidate.SlotRoles, goalPaceStage, registeredValues);

        Assert.Equal(GoalPaceReachabilityOutcome.PassWithOpenRisk, result.OverallOutcome);
        Assert.DoesNotContain(result.OutcomeChecks, c => c.Status == GoalPaceOutcomeStatus.StructurallyUnreachable);
        Assert.Contains(result.OutcomeChecks, c => c.Status == GoalPaceOutcomeStatus.UncertainNotEvaluated);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public async Task Verify_RealNineThroughFourteenWeekCandidates_AlsoPassWithOpenRisk(int targetWeeks)
    {
        var (candidate, progression, goalPaceStage, registeredValues) = await LoadRealAsync();
        var allocation = _allocator.Resolve(candidate, targetWeeks);

        var result = GoalPaceReachabilityVerifier.Verify(allocation, progression, candidate.SlotRoles, goalPaceStage, registeredValues);

        Assert.Equal(GoalPaceReachabilityOutcome.PassWithOpenRisk, result.OverallOutcome);
    }

    [Fact]
    public void Verify_SyntheticValueWithNoEligiblePathAndNoReachableFallback_OverallOutcomeIsFail()
    {
        // FallbackStageKey points to a stage that requires ANOTHER condition
        // type this synthetic progression never resolves -- StageReachabilityVerifier
        // will report a structural failure for the ineligible values, which
        // this verifier must surface as StructurallyUnreachable / Fail, not
        // silently treat as a confirmed fallback.
        var goalPaceStage = new CatalogWorkoutProgressionStage
        {
            ProgressionStageKey = "GOAL_PACE_REHEARSAL",
            RelativeOrder = 1,
            MinimumExposures = 1,
            MaximumExposures = 1,
            CompressionBehavior = CatalogStageCompressionBehavior.Protected,
            ExtensionBehavior = CatalogStageExtensionBehavior.FixedExposure,
            Requires = [new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } }],
            FallbackStageKey = "UNREACHABLE_FALLBACK",
        };
        var unreachableFallback = new CatalogWorkoutProgressionStage
        {
            ProgressionStageKey = "UNREACHABLE_FALLBACK",
            RelativeOrder = 2,
            MinimumExposures = 1,
            MaximumExposures = 1,
            CompressionBehavior = CatalogStageCompressionBehavior.Compressible,
            ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
            Requires = [new CatalogRuntimeEligibilityCondition { ConditionType = "SOME_OTHER_CONDITION_NEVER_SUPPLIED", AllowedValues = new HashSet<string> { "X" } }],
        };
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "SYNTHETIC", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = [new CatalogPhaseWorkoutProgression { PhaseKey = "RACE_SPECIFIC", Stages = [goalPaceStage, unreachableFallback] }],
        };
        var allocation = new PhaseAllocationResult(1, 1, 0, AllocationMode.Preferred,
            [new AllocatedPhase("RACE_SPECIFIC", 1, 1, 1)], true, "MATHEMATICALLY_FEASIBLE");
        var roles = new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" };
        var registeredValues = new HashSet<string> { "REALISTIC", "CHALLENGING", "UNSUPPORTED", "NOT_REQUESTED" };

        var result = GoalPaceReachabilityVerifier.Verify(allocation, progression, roles, goalPaceStage, registeredValues);

        Assert.Equal(GoalPaceReachabilityOutcome.Fail, result.OverallOutcome);
        Assert.Contains(result.OutcomeChecks, c => c.Status == GoalPaceOutcomeStatus.StructurallyUnreachable);
    }

    [Fact]
    public void Verify_UnexpectedStageShape_IsNotApplicable()
    {
        var wrongShapeStage = new CatalogWorkoutProgressionStage
        {
            ProgressionStageKey = "GOAL_PACE_REHEARSAL",
            RelativeOrder = 1,
            MinimumExposures = 1,
            MaximumExposures = 1,
            CompressionBehavior = CatalogStageCompressionBehavior.Compressible,
            ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
            Requires = [],
            FallbackStageKey = null,
        };
        var progression = new CatalogWorkoutProgressionDefinition
        {
            Key = "SYNTHETIC", Version = 1, DistanceFamily = "TEN_K",
            PhaseProgressions = [new CatalogPhaseWorkoutProgression { PhaseKey = "RACE_SPECIFIC", Stages = [wrongShapeStage] }],
        };
        var allocation = new PhaseAllocationResult(1, 1, 0, AllocationMode.Preferred,
            [new AllocatedPhase("RACE_SPECIFIC", 1, 1, 1)], true, "MATHEMATICALLY_FEASIBLE");

        var result = GoalPaceReachabilityVerifier.Verify(allocation, progression,
            ["KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN"], wrongShapeStage, new HashSet<string> { "REALISTIC" });

        Assert.Equal(GoalPaceReachabilityOutcome.NotApplicable, result.OverallOutcome);
        Assert.Empty(result.OutcomeChecks);
        Assert.Single(result.Findings);
    }

    [Fact]
    public void Verify_MathematicallyInfeasibleAllocation_IsNotApplicable()
    {
        var stage = new CatalogWorkoutProgressionStage
        {
            ProgressionStageKey = "GOAL_PACE_REHEARSAL", RelativeOrder = 1, MinimumExposures = 1, MaximumExposures = 1,
            CompressionBehavior = CatalogStageCompressionBehavior.Protected, ExtensionBehavior = CatalogStageExtensionBehavior.FixedExposure,
            Requires = [new CatalogRuntimeEligibilityCondition { ConditionType = "GOAL_FEASIBILITY_IN", AllowedValues = new HashSet<string> { "REALISTIC" } }],
            FallbackStageKey = "FALLBACK",
        };
        var allocation = new PhaseAllocationResult(7, 12, -5, AllocationMode.Compression, [], false, "TARGET_BELOW_SUM_OF_MINIMUMS");

        var result = GoalPaceReachabilityVerifier.Verify(allocation,
            new CatalogWorkoutProgressionDefinition { Key = "SYNTHETIC", Version = 1, DistanceFamily = "TEN_K", PhaseProgressions = [] },
            ["KEY_SESSION"], stage, new HashSet<string> { "REALISTIC" });

        Assert.Equal(GoalPaceReachabilityOutcome.NotApplicable, result.OverallOutcome);
    }

    [Fact]
    public async Task Verify_IsDeterministicAndPure()
    {
        var (candidate, progression, goalPaceStage, registeredValues) = await LoadRealAsync();
        var allocation = _allocator.Resolve(candidate, 8);

        var first = GoalPaceReachabilityVerifier.Verify(allocation, progression, candidate.SlotRoles, goalPaceStage, registeredValues);
        var second = GoalPaceReachabilityVerifier.Verify(allocation, progression, candidate.SlotRoles, goalPaceStage, registeredValues);

        Assert.Equal(first.OverallOutcome, second.OverallOutcome);
        Assert.Equal(first.OutcomeChecks, second.OutcomeChecks);
        Assert.Equal(first.Findings, second.Findings);
    }

    [Fact]
    public void GoalPaceReachabilityVerifier_ReusesRealProgressionStageAllocator_DoesNotDuplicateItsFallbackAlgorithm()
    {
        // Positive proof of reuse: the source genuinely calls the real
        // ProgressionStageAllocator.Allocate entry point -- the same one
        // StageReachabilityVerifier itself calls -- rather than
        // reimplementing fallback-chain-walking or cycle detection here.
        //
        // It deliberately does NOT call StageReachabilityVerifier.Verify(
        // itself: doing so would make this file a second production-code
        // call site of that method and break StageReachabilityVerifierTests'
        // own StageReachabilityVerifier_HasNoProductionCallSite regression
        // guard (confirmed by reproducing that exact failure during
        // development of this file, then redesigning around it rather than
        // touching that test, per this phase's explicit "do not modify
        // existing verifier tests" scope).
        var source = File.ReadAllText(Path.Combine(
            TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
            "Schedule", "Materialization", "GoalPaceReachabilityVerifier.cs"));

        Assert.Contains("new ProgressionStageAllocator().Allocate(", source);
        Assert.DoesNotContain("StageReachabilityVerifier.Verify(", source);

        // Negative proof: no fallback-chain-walking primitives (cycle
        // detection via a `visited` set, or a manual chain-walking loop)
        // exist in this file -- those belong only to ProgressionStageAllocator.
        Assert.DoesNotContain("visited", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("while (true)", source);
    }

    [Fact]
    public void GoalPaceReachabilityVerifier_HasNoCallSiteInApplicationOrApiProductionCode()
    {
        var repoRoot = TestPlanServicesFactory.RepoRoot();
        foreach (var root in new[] { Path.Combine(repoRoot, "backend", "RunningApp.Application"), Path.Combine(repoRoot, "backend", "RunningApp.Api") })
        {
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    && !f.EndsWith($"{Path.DirectorySeparatorChar}GoalPaceReachabilityVerifier.cs", StringComparison.OrdinalIgnoreCase));
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("GoalPaceReachabilityVerifier.Verify(", content, StringComparison.Ordinal);
            }
        }
    }

    private static async Task<(PlanCatalogCandidateSummary Candidate, CatalogWorkoutProgressionDefinition Progression, CatalogWorkoutProgressionStage GoalPaceStage, IReadOnlySet<string> RegisteredValues)> LoadRealAsync()
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") });
        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance)
            .LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);
        var goalPaceStage = progression.PhaseProgressions
            .Single(p => p.PhaseKey == "RACE_SPECIFIC").Stages
            .Single(s => s.ProgressionStageKey == "GOAL_PACE_REHEARSAL");

        var registrySnapshot = await new RuntimeConditionRegistryReader(options, NullLogger<RuntimeConditionRegistryReader>.Instance)
            .LoadAsync(RegistryRef);
        var registeredValues = registrySnapshot.AllowedValuesByConditionType[GoalFeasibilityResolver.ConditionTypeValue].ToHashSet();

        return (candidate, progression, goalPaceStage, registeredValues);
    }
}
