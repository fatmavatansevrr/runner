using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

public sealed class RaceSpecificCapacityVerifierTests
{
    private readonly CatalogPhaseAllocationResolver _allocator = new();

    public static TheoryData<int, int, int, int> RealMatrix => new()
    {
        { 8, 2, 1, 0 },
        { 9, 3, 2, 1 },
        { 10, 4, 3, 2 },
        { 11, 4, 3, 2 },
        { 12, 4, 3, 2 },
        { 13, 4, 3, 2 },
        { 14, 4, 3, 2 },
    };

    [Theory]
    [MemberData(nameof(RealMatrix))]
    public async Task Verify_RealCatalogMatrix_UsesExposureSlots(
        int targetWeeks, int raceSpecificWeeks, int guaranteedSlack, int worstCaseSlack)
    {
        var (candidate, progression) = await LoadRealAsync();
        var allocation = _allocator.Resolve(candidate, targetWeeks);

        var result = RaceSpecificCapacityVerifier.Verify(allocation, progression, candidate.SlotRoles);

        Assert.Equal(targetWeeks, result.TargetWeeks);
        Assert.Equal(raceSpecificWeeks, result.AllocatedRaceSpecificWeeks);
        Assert.Equal(raceSpecificWeeks, result.AvailableSlots);
        Assert.Equal(1, result.UnconditionalRequiredExposureCount);
        Assert.Equal(1, result.ConditionalRequiredExposureCount);
        Assert.Equal(0, result.OptionalExposureCount);
        Assert.Equal(guaranteedSlack, result.GuaranteedSlack);
        Assert.Equal(worstCaseSlack, result.WorstCaseSlack);
        Assert.Equal(RaceSpecificCapacityOutcome.Pass, result.Outcome);

        if (targetWeeks == 8)
        {
            Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.ExactFitZeroWorstCaseSlack);
        }
        else
        {
            Assert.DoesNotContain(result.Findings, f => f.Code is RaceSpecificCapacityFindingCode.ExactFitZeroGuaranteedSlack
                or RaceSpecificCapacityFindingCode.ExactFitZeroWorstCaseSlack);
        }
    }

    [Fact]
    public void Verify_MinimumExposuresGreaterThanOne_CountsExposures_NotDefinitions()
    {
        var result = VerifySynthetic(1, [Stage("REPEATED", minimum: 2)]);

        Assert.Equal(2, result.UnconditionalRequiredExposureCount);
        Assert.Equal(RaceSpecificCapacityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.RaceSpecificGuaranteedCapacityShortfall
            && f.ExposureCount == 2 && f.Shortfall == 1);
    }

    [Fact]
    public void Verify_GuaranteedShortfall_ReportsExactContributingEvidence()
    {
        var result = VerifySynthetic(2, [Stage("A", minimum: 1), Stage("B", minimum: 2)]);

        var finding = Assert.Single(result.Findings);
        Assert.Equal(RaceSpecificCapacityFindingCode.RaceSpecificGuaranteedCapacityShortfall, finding.Code);
        Assert.Equal(3, finding.ExposureCount);
        Assert.Equal(1, finding.Shortfall);
        Assert.Contains("A:1", finding.Message);
        Assert.Contains("B:2", finding.Message);
    }

    [Fact]
    public void Verify_ExactGuaranteedFitWithoutConditionalDemand_PassesAndKeepsZeroSlackVisible()
    {
        var result = VerifySynthetic(2, [Stage("A"), Stage("B")]);

        Assert.Equal(RaceSpecificCapacityOutcome.Pass, result.Outcome);
        Assert.Equal(0, result.GuaranteedSlack);
        Assert.Equal(0, result.WorstCaseSlack);
        Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.ExactFitZeroGuaranteedSlack);
    }

    [Fact]
    public void Verify_PositiveSlack_PassesWithoutExactFitFinding()
    {
        var result = VerifySynthetic(3, [Stage("A")]);

        Assert.Equal(RaceSpecificCapacityOutcome.Pass, result.Outcome);
        Assert.Equal(2, result.GuaranteedSlack);
        Assert.Equal(2, result.WorstCaseSlack);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Verify_ZeroMinimumStage_IsOptionalAndNeverEstablishesCapacityFloor()
    {
        var result = VerifySynthetic(1, [Stage("REQUIRED"), Stage("OMITTABLE", minimum: 0, maximum: 2)]);

        Assert.Equal(1, result.UnconditionalRequiredExposureCount);
        Assert.Equal(2, result.OptionalExposureCount);
        Assert.Equal(RaceSpecificCapacityOutcome.Pass, result.Outcome);
        Assert.Equal(0, result.WorstCaseSlack);
    }

    [Fact]
    public void Verify_ConditionalPrimaryOrFallback_ConsumesExactlyOneAlternativeSlot()
    {
        var result = VerifySynthetic(1,
        [
            Stage("PRIMARY", requires: [Condition()], fallback: "FALLBACK"),
            Stage("FALLBACK"),
        ]);

        Assert.Equal(0, result.UnconditionalRequiredExposureCount);
        Assert.Equal(1, result.ConditionalRequiredExposureCount);
        Assert.Equal(0, result.OptionalExposureCount);
        Assert.Equal(0, result.WorstCaseSlack);
        Assert.Equal(RaceSpecificCapacityOutcome.Pass, result.Outcome);
    }

    [Fact]
    public void Verify_DeterminateSimultaneousConditionalDemandShortfall_Fails()
    {
        var result = VerifySynthetic(1,
        [
            Stage("A", requires: [Condition("A_IN")]),
            Stage("B", requires: [Condition("B_IN")]),
        ]);

        Assert.Equal(-1, result.WorstCaseSlack);
        Assert.Equal(RaceSpecificCapacityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.RaceSpecificConditionalCapacityShortfall
            && f.Shortfall == 1);
    }

    [Fact]
    public void Verify_SyntheticConditionalFallbackChainIsDecisionRequired_NotGuessed()
    {
        var result = VerifySynthetic(2,
        [
            Stage("PRIMARY", requires: [Condition()], fallback: "CONDITIONAL_FALLBACK"),
            Stage("CONDITIONAL_FALLBACK", requires: [Condition("PACE_SOURCE_IN")]),
        ]);

        Assert.Equal(RaceSpecificCapacityOutcome.DecisionRequired, result.Outcome);
        Assert.Null(result.WorstCaseSlack);
        Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.RaceSpecificConditionalCapacityUnresolved);
    }

    [Fact]
    public void Verify_MathematicallyInfeasibleAllocation_IsNotApplicable()
    {
        var allocation = Allocation(7, 0) with { IsMathematicallyFeasible = false };

        var result = RaceSpecificCapacityVerifier.Verify(allocation, Progression([Stage("A")]), Roles());

        Assert.Equal(RaceSpecificCapacityOutcome.NotApplicable, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.AllocationNotMathematicallyFeasible);
    }

    [Fact]
    public void Verify_MissingRaceSpecificAllocation_FailsClosed()
    {
        var allocation = Allocation(8, 2) with
        {
            Phases = [new AllocatedPhase("BUILD", 8, 1, 8)],
        };

        var result = RaceSpecificCapacityVerifier.Verify(allocation, Progression([Stage("A")]), Roles());

        Assert.Equal(RaceSpecificCapacityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.RaceSpecificPhaseMissing);
    }

    [Fact]
    public void Verify_DuplicateRaceSpecificProgression_FailsClosed()
    {
        var phase = new CatalogPhaseWorkoutProgression { PhaseKey = "RACE_SPECIFIC", Stages = [Stage("A")] };
        var progression = WithPhaseProgressions(Progression([Stage("A")]), [phase, phase]);

        var result = RaceSpecificCapacityVerifier.Verify(Allocation(8, 2), progression, Roles());

        Assert.Equal(RaceSpecificCapacityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.RaceSpecificProgressionDuplicate);
    }

    [Fact]
    public void Verify_WeeklyRoleContractMustProveExactlyOneKeySessionSlot()
    {
        var result = RaceSpecificCapacityVerifier.Verify(
            Allocation(8, 2), Progression([Stage("A")]), ["EASY_SUPPORT", "LONG_RUN"]);

        Assert.Equal(RaceSpecificCapacityOutcome.Fail, result.Outcome);
        Assert.Contains(result.Findings, f => f.Code == RaceSpecificCapacityFindingCode.WeeklyKeySessionSlotContractInvalid);
    }

    [Fact]
    public void Verify_IsDeterministicAndPureWithOwnTypes()
    {
        var allocation = Allocation(8, 2);
        var progression = Progression([Stage("A")]);
        var first = RaceSpecificCapacityVerifier.Verify(allocation, progression, Roles());
        var second = RaceSpecificCapacityVerifier.Verify(allocation, progression, Roles());

        Assert.Equal(first.TargetWeeks, second.TargetWeeks);
        Assert.Equal(first.AllocatedRaceSpecificWeeks, second.AllocatedRaceSpecificWeeks);
        Assert.Equal(first.AvailableSlots, second.AvailableSlots);
        Assert.Equal(first.UnconditionalRequiredExposureCount, second.UnconditionalRequiredExposureCount);
        Assert.Equal(first.ConditionalRequiredExposureCount, second.ConditionalRequiredExposureCount);
        Assert.Equal(first.OptionalExposureCount, second.OptionalExposureCount);
        Assert.Equal(first.GuaranteedSlack, second.GuaranteedSlack);
        Assert.Equal(first.WorstCaseSlack, second.WorstCaseSlack);
        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.Findings, second.Findings);
        var method = typeof(RaceSpecificCapacityVerifier).GetMethod(nameof(RaceSpecificCapacityVerifier.Verify), BindingFlags.Public | BindingFlags.Static)!;
        Assert.Equal(3, method.GetParameters().Length);
        Assert.Equal(typeof(RaceSpecificCapacityVerificationResult), method.ReturnType);
        Assert.Null(typeof(RaceSpecificCapacityVerifier).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault());
        Assert.DoesNotContain(typeof(AllocationOrderVerificationOutcome), method.ReturnType.GetProperties().Select(p => p.PropertyType));
        Assert.DoesNotContain(typeof(PhaseConstraintVerificationOutcome), method.ReturnType.GetProperties().Select(p => p.PropertyType));
    }

    [Fact]
    public void RaceSpecificCapacityVerifier_HasNoCallSiteInApplicationOrApiProductionCode()
    {
        DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator(nameof(RaceSpecificCapacityVerifier));
    }

    private static async Task<(PlanCatalogCandidateSummary Candidate, CatalogWorkoutProgressionDefinition Progression)> LoadRealAsync()
    {
        var options = Options.Create(new PlanCatalogOptions
        {
            CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog"),
        });
        var candidate = await new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance)
            .LoadCandidateAsync(V1LiveCatalogPilotRoutingPolicy.CandidateKey, V1LiveCatalogPilotRoutingPolicy.CandidateVersion);
        var progression = await new CatalogWorkoutProgressionLoader(options).LoadAsync(candidate.WorkoutProgression);
        return (candidate, progression);
    }

    private static RaceSpecificCapacityVerificationResult VerifySynthetic(
        int raceSpecificWeeks, IReadOnlyList<CatalogWorkoutProgressionStage> stages) =>
        RaceSpecificCapacityVerifier.Verify(Allocation(raceSpecificWeeks, raceSpecificWeeks), Progression(stages), Roles());

    private static PhaseAllocationResult Allocation(int targetWeeks, int raceSpecificWeeks) =>
        new(targetWeeks, 12, targetWeeks - 12, targetWeeks < 12 ? AllocationMode.Compression : AllocationMode.Preferred,
            [new AllocatedPhase("RACE_SPECIFIC", raceSpecificWeeks, 1, 20)], true, "MATHEMATICALLY_FEASIBLE");

    private static CatalogWorkoutProgressionDefinition Progression(IReadOnlyList<CatalogWorkoutProgressionStage> stages) =>
        new()
        {
            Key = "SYNTHETIC",
            Version = 1,
            DistanceFamily = "TEN_K",
            PhaseProgressions = [new CatalogPhaseWorkoutProgression { PhaseKey = "RACE_SPECIFIC", Stages = stages }],
        };

    private static CatalogWorkoutProgressionDefinition WithPhaseProgressions(
        CatalogWorkoutProgressionDefinition source, IReadOnlyList<CatalogPhaseWorkoutProgression> phases) =>
        new() { Key = source.Key, Version = source.Version, DistanceFamily = source.DistanceFamily, PhaseProgressions = phases };

    private static CatalogWorkoutProgressionStage Stage(
        string key, int minimum = 1, int? maximum = null,
        IReadOnlyList<CatalogRuntimeEligibilityCondition>? requires = null, string? fallback = null) =>
        new()
        {
            ProgressionStageKey = key,
            RelativeOrder = 1,
            MinimumExposures = minimum,
            MaximumExposures = maximum ?? Math.Max(minimum, 2),
            CompressionBehavior = CatalogStageCompressionBehavior.Compressible,
            ExtensionBehavior = CatalogStageExtensionBehavior.Extendable,
            Requires = requires ?? [],
            FallbackStageKey = fallback,
        };

    private static CatalogRuntimeEligibilityCondition Condition(string type = "GOAL_FEASIBILITY_IN") =>
        new() { ConditionType = type, AllowedValues = new HashSet<string> { "YES" } };

    private static string[] Roles() => ["KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN"];
}
