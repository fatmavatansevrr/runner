using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 4G.3B.5 -- executable proof for
/// <see cref="RaceCoreSupportRegistryBuilder"/>: the MechanicallyPassed vs.
/// Supported distinction, the two-gate composition with
/// <see cref="AllocationOrderCorrectnessVerifier"/>, default-pending
/// semantics, purity, dark reachability, and zero governance-file I/O.
/// Does not modify SafetyVerificationOrchestrator, any of the nine
/// verifiers, or AllocationOrderCorrectnessVerifier.
/// </summary>
public sealed class RaceCoreSupportRegistryTests
{
    // ── Real data: reuse the exact real-context construction already ────────
    // ── established and proven in Phase 4G.3B.4b ─────────────────────────────

    private static async Task<SafetyVerificationPipelineResult> RealOrchestratorResultAsync(int weeks) =>
        SafetyVerificationOrchestrator.Run(await SafetyVerificationContextFixtures.RealAsync(weeks, "REALISTIC"));

    private static async Task<AllocationOrderVerificationResult> RealAllocationOrderResultAsync(int weeks)
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(weeks, "REALISTIC");
        return AllocationOrderCorrectnessVerifier.Verify(context.Allocation);
    }

    // ── 1. Real 12-week pilot -- honest result, not a forced assumption ─────

    [Fact]
    public async Task Real12WeekPilot_ReportsActualMeasuredStatus_NotForcedToMechanicallyPassed()
    {
        // IMPORTANT, HONESTLY REPORTED FINDING: the real 12-week orchestrator
        // result is DecisionRequired, not Pass -- GoalPaceReachabilityVerifier
        // structurally can never normalize to Pass for the real
        // GOAL_PACE_REHEARSAL stage (its own always-present synthetic
        // NotEvaluated check can only ever produce StructurallyUnreachable
        // or UncertainNotEvaluated, confirmed in Phase 4G.3B.4b -- see
        // SafetyVerificationOrchestratorTests.GoalPaceReachabilityVerifier_CanNeverNormalizeToPass_...).
        // The real registry entry for 12 weeks therefore genuinely shows
        // DecisionRequired today, not MechanicallyPassed -- this test
        // measures and asserts that real fact rather than forcing the
        // registry to agree with a prior assumption. The MechanicallyPassed
        // vs. Supported distinction is separately, genuinely exercised by
        // Constructed_AllNinePass_ReportsMechanicallyPassed_NeverSupported
        // below using a narrowly constructed synthetic orchestrator result,
        // exactly mirroring the same necessary technique the orchestrator's
        // own test suite already established for this same structural
        // limitation.
        var orchestratorResult = await RealOrchestratorResultAsync(12);
        var allocationOrderResult = await RealAllocationOrderResultAsync(12);

        var registry = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification(
            "TEN_K__4D__INTERMEDIATE",
            new Dictionary<int, SafetyVerificationPipelineResult> { [12] = orchestratorResult },
            new Dictionary<int, AllocationOrderVerificationResult> { [12] = allocationOrderResult });

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(SafetyVerificationOverallOutcome.DecisionRequired, orchestratorResult.OverallOutcome);
        Assert.Equal(RaceCoreSupportStatus.DecisionRequired, entry.Status);
        Assert.NotEqual(RaceCoreSupportStatus.Supported, entry.Status);
        Assert.NotEqual(RaceCoreSupportStatus.MechanicallyPassed, entry.Status);
        Assert.StartsWith("ORCHESTRATOR_DECISION_REQUIRED_AT_", entry.StatusReasonCode);
    }

    [Fact]
    public void Constructed_AllNinePass_ReportsMechanicallyPassed_NeverSupported()
    {
        // The genuine, structurally-only-achievable-via-construction proof
        // that MechanicallyPassed exists and is distinct from Supported.
        var orchestratorResult = SyntheticAllPassPipelineResult(12);
        var allocationOrderPass = new AllocationOrderVerificationResult(12, AllocationOrderVerificationOutcome.Pass, "PASS_SYNTHETIC");

        var registry = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification(
            "SYNTHETIC",
            new Dictionary<int, SafetyVerificationPipelineResult> { [12] = orchestratorResult },
            new Dictionary<int, AllocationOrderVerificationResult> { [12] = allocationOrderPass });

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(RaceCoreSupportStatus.MechanicallyPassed, entry.Status);
        Assert.NotEqual(RaceCoreSupportStatus.Supported, entry.Status);
        Assert.Equal("MECHANICALLY_PASSED_ORCHESTRATOR_AND_ALLOCATION_ORDER_PASS", entry.StatusReasonCode);
        Assert.Equal("Pass", entry.AllocationOrderCorrectnessOutcome);
        Assert.Empty(entry.BlockingFindings);
    }

    // ── 2. Real 8-14 week matrix -- measured, not assumed ────────────────────

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task Real8Through14_MeasuredRegistryStatus(int weeks)
    {
        var orchestratorResult = await RealOrchestratorResultAsync(weeks);
        var allocationOrderResult = await RealAllocationOrderResultAsync(weeks);

        var registry = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification(
            "TEN_K__4D__INTERMEDIATE",
            new Dictionary<int, SafetyVerificationPipelineResult> { [weeks] = orchestratorResult },
            new Dictionary<int, AllocationOrderVerificationResult> { [weeks] = allocationOrderResult });

        var entry = Assert.Single(registry.Entries);
        // Measured in Phase 4G.3B.4b: OverallOutcome is DecisionRequired for
        // every real target 8-14 (GoalPaceReachabilityVerifier's structural
        // finding). The registry must therefore report DecisionRequired for
        // every one of them too, driven by the orchestrator gate --
        // regardless of AllocationOrderCorrectnessVerifier's own result,
        // per build rule 4 (orchestrator DecisionRequired is unconditional).
        Assert.Equal(SafetyVerificationOverallOutcome.DecisionRequired, orchestratorResult.OverallOutcome);
        Assert.Equal(RaceCoreSupportStatus.DecisionRequired, entry.Status);
        Assert.NotEqual(RaceCoreSupportStatus.Supported, entry.Status);
        Assert.StartsWith("ORCHESTRATOR_DECISION_REQUIRED_AT_GoalPaceReachability", entry.StatusReasonCode);
    }

    // ── 3. No orchestrator result -> NotYetEvaluated ─────────────────────────

    [Fact]
    public void NoOrchestratorResultForWeek_IsNotYetEvaluated()
    {
        var registry = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification(
            "SYNTHETIC",
            new Dictionary<int, SafetyVerificationPipelineResult>(),
            new Dictionary<int, AllocationOrderVerificationResult> { [9] = new(9, AllocationOrderVerificationOutcome.Pass, "PASS_SYNTHETIC") });

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(9, entry.WeekCount);
        Assert.Equal(RaceCoreSupportStatus.NotYetEvaluated, entry.Status);
        Assert.Null(entry.OrchestratorResult);
        Assert.Equal("NOT_YET_EVALUATED_NO_ORCHESTRATOR_RESULT", entry.StatusReasonCode);
        Assert.Empty(entry.BlockingFindings);
    }

    // ── 4. OverallOutcome == Fail -> Fail (real, via mutated allocation) ────

    [Fact]
    public async Task RealMutatedAllocation_OrchestratorFail_RegistryReportsFail()
    {
        var mutated = await MutateFoundationBelowMinimumAsync();
        var orchestratorResult = SafetyVerificationOrchestrator.Run(mutated);
        Assert.Equal(SafetyVerificationOverallOutcome.Fail, orchestratorResult.OverallOutcome);

        var registry = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification(
            "SYNTHETIC_MUTATED",
            new Dictionary<int, SafetyVerificationPipelineResult> { [12] = orchestratorResult },
            allocationOrderResultsByWeekCount: null);

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(RaceCoreSupportStatus.Fail, entry.Status);
        Assert.StartsWith("ORCHESTRATOR_FAIL_AT_", entry.StatusReasonCode);
        Assert.NotEmpty(entry.BlockingFindings);
        Assert.Null(entry.AllocationOrderCorrectnessOutcome);
    }

    // ── 5. Pass + AllocationOrder DecisionRequired -> DecisionRequired ───────
    // ── (two-gate composition actually blocks) ───────────────────────────────

    [Fact]
    public void Constructed_OrchestratorPass_AllocationOrderDecisionRequired_RegistryBlocksAsDecisionRequired()
    {
        var orchestratorResult = SyntheticAllPassPipelineResult(9);
        var allocationOrderDecisionRequired = new AllocationOrderVerificationResult(
            9, AllocationOrderVerificationOutcome.DecisionRequired,
            "DECISION_REQUIRED_UNCONFIRMED_ALLOCATION_ORDER (TD-ALLOCATION-PRIORITY-001): synthetic test case.");

        var registry = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification(
            "SYNTHETIC",
            new Dictionary<int, SafetyVerificationPipelineResult> { [9] = orchestratorResult },
            new Dictionary<int, AllocationOrderVerificationResult> { [9] = allocationOrderDecisionRequired });

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(RaceCoreSupportStatus.DecisionRequired, entry.Status);
        Assert.Equal("ALLOCATION_ORDER_DECISION_REQUIRED", entry.StatusReasonCode);
        Assert.Contains(entry.BlockingFindings, f => f.Contains("TD-ALLOCATION-PRIORITY-001"));
        Assert.Equal("DecisionRequired", entry.AllocationOrderCorrectnessOutcome);
    }

    // ── 5b. StructurallyInfeasible (build-logic branch not otherwise ────────
    // ── exercised by the real 8-14 range, which is entirely feasible) ───────

    [Fact]
    public async Task RootAllocationInfeasible_RegistryReportsStructurallyInfeasible()
    {
        var infeasibleContext = await SafetyVerificationContextFixtures.RealMathematicallyInfeasibleAsync();
        var orchestratorResult = SafetyVerificationOrchestrator.Run(infeasibleContext);
        Assert.Equal(SafetyVerificationOverallOutcome.NotApplicable, orchestratorResult.OverallOutcome);

        var registry = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification(
            "SYNTHETIC_INFEASIBLE",
            new Dictionary<int, SafetyVerificationPipelineResult> { [12] = orchestratorResult },
            allocationOrderResultsByWeekCount: null);

        var entry = Assert.Single(registry.Entries);
        Assert.Equal(RaceCoreSupportStatus.StructurallyInfeasible, entry.Status);
        Assert.Equal("STRUCTURALLY_INFEASIBLE_ALLOCATION", entry.StatusReasonCode);
        Assert.NotEmpty(entry.BlockingFindings);
    }

    // ── 6. Supported is structurally never returned ─────────────────────────

    [Fact]
    public void Supported_IsNeverReturned_AcrossEveryConstructedInputCombination()
    {
        // Exhaustive-by-construction coverage of every reachable branch in
        // BuildFromMechanicalVerification's own logic: no orchestrator
        // result; NotApplicable; Fail; DecisionRequired (orchestrator);
        // Pass+no-allocation-order-dict; Pass+allocation-order-Pass;
        // Pass+allocation-order-DecisionRequired; Pass+allocation-order-dict-present-but-missing-this-week.
        var allocationOrderPass = new AllocationOrderVerificationResult(1, AllocationOrderVerificationOutcome.Pass, "PASS_SYNTHETIC");
        var allocationOrderDecisionRequired = new AllocationOrderVerificationResult(1, AllocationOrderVerificationOutcome.DecisionRequired, "DECISION_REQUIRED_SYNTHETIC");

        var scenarios = new List<RaceCoreSupportRegistry>
        {
            RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("S", new Dictionary<int, SafetyVerificationPipelineResult>(), null),
            RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("S",
                new Dictionary<int, SafetyVerificationPipelineResult> { [1] = SyntheticNotApplicablePipelineResult(1) }, null),
            RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("S",
                new Dictionary<int, SafetyVerificationPipelineResult> { [1] = SyntheticFailPipelineResult(1) }, null),
            RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("S",
                new Dictionary<int, SafetyVerificationPipelineResult> { [1] = SyntheticDecisionRequiredPipelineResult(1) }, null),
            RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("S",
                new Dictionary<int, SafetyVerificationPipelineResult> { [1] = SyntheticAllPassPipelineResult(1) }, null),
            RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("S",
                new Dictionary<int, SafetyVerificationPipelineResult> { [1] = SyntheticAllPassPipelineResult(1) },
                new Dictionary<int, AllocationOrderVerificationResult> { [1] = allocationOrderPass }),
            RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("S",
                new Dictionary<int, SafetyVerificationPipelineResult> { [1] = SyntheticAllPassPipelineResult(1) },
                new Dictionary<int, AllocationOrderVerificationResult> { [1] = allocationOrderDecisionRequired }),
            RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("S",
                new Dictionary<int, SafetyVerificationPipelineResult> { [1] = SyntheticAllPassPipelineResult(1) },
                new Dictionary<int, AllocationOrderVerificationResult> { [2] = allocationOrderPass }), // present dict, missing this week
        };

        foreach (var registry in scenarios)
        {
            foreach (var entry in registry.Entries)
            {
                Assert.NotEqual(RaceCoreSupportStatus.Supported, entry.Status);
            }
        }

        // Bounded source-text audit: the strongest practical proof alongside
        // exhaustive-by-construction coverage above -- confirms no code path
        // in the production file constructs RaceCoreSupportStatus.Supported
        // anywhere outside the enum's own declaration/doc comments.
        var source = File.ReadAllText(RegistrySourcePath());
        var codeOnlyLines = source.Split('\n')
            .Where(line => !line.TrimStart().StartsWith("///") && !line.TrimStart().StartsWith("//"))
            .ToList();
        var enumDeclarationLineIndex = codeOnlyLines.FindIndex(l => l.Contains("Supported,"));
        Assert.True(enumDeclarationLineIndex >= 0, "Could not locate the Supported enum member declaration.");
        var restOfFile = string.Join('\n', codeOnlyLines.Skip(enumDeclarationLineIndex + 1));
        Assert.DoesNotContain("RaceCoreSupportStatus.Supported", restOfFile);
    }

    // ── 7. Purity ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildFromMechanicalVerification_IsPure_IdenticalInputsProduceIdenticalResults()
    {
        var orchestratorResult = await RealOrchestratorResultAsync(12);
        var allocationOrderResult = await RealAllocationOrderResultAsync(12);
        var orchestratorDict = new Dictionary<int, SafetyVerificationPipelineResult> { [12] = orchestratorResult };
        var allocationOrderDict = new Dictionary<int, AllocationOrderVerificationResult> { [12] = allocationOrderResult };

        var first = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("X", orchestratorDict, allocationOrderDict);
        var second = RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification("X", orchestratorDict, allocationOrderDict);

        Assert.Equal(first.CandidateKey, second.CandidateKey);
        Assert.Equal(first.Entries.Count, second.Entries.Count);
        foreach (var (a, b) in first.Entries.Zip(second.Entries))
        {
            Assert.Equal(a.WeekCount, b.WeekCount);
            Assert.Equal(a.Status, b.Status);
            Assert.Equal(a.StatusReasonCode, b.StatusReasonCode);
            Assert.Equal(a.AllocationOrderCorrectnessOutcome, b.AllocationOrderCorrectnessOutcome);
            Assert.Equal(a.BlockingFindings, b.BlockingFindings);
        }
    }

    // ── 8. Dark reachability ─────────────────────────────────────────────────

    [Fact]
    public void RegistryBuilder_HasZeroProductionCallSites()
    {
        var repo = TestPlanServicesFactory.RepoRoot();
        foreach (var root in new[] { Path.Combine(repo, "backend", "RunningApp.Application"), Path.Combine(repo, "backend", "RunningApp.Api") })
        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.EndsWith("RaceCoreSupportRegistry.cs")))
        {
            Assert.DoesNotContain("RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification(", File.ReadAllText(file));
        }
    }

    // ── 9. Zero governance-file I/O ──────────────────────────────────────────

    [Fact]
    public void RegistryBuilder_PerformsNoGovernanceFileIO()
    {
        // Bounded to actual code lines -- the file's own doc comments
        // legitimately name these APIs in prose (e.g. "no File/Path/Stream/
        // JsonDocument/... access anywhere in this file") to document the
        // guarantee; what must be absent is any of them appearing as real
        // code, not as a textual mention of the guarantee itself.
        var codeOnlyLines = File.ReadAllLines(RegistrySourcePath())
            .Where(line => !line.TrimStart().StartsWith("///") && !line.TrimStart().StartsWith("//"))
            .ToList();
        var codeOnly = string.Join('\n', codeOnlyLines);
        foreach (var ioApi in new[] { "File.", "Path.Combine", "Stream", "JsonDocument", "JsonSerializer", "IConfiguration", "Directory." })
        {
            Assert.DoesNotContain(ioApi, codeOnly);
        }
    }

    // ── Synthetic pipeline-result builders (test-local only) ─────────────────

    private static string RegistrySourcePath() => Path.Combine(
        TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
        "Schedule", "Materialization", "RaceCoreSupportRegistry.cs");

    private static async Task<SafetyVerificationContext> MutateFoundationBelowMinimumAsync()
    {
        var real = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var foundation = real.Allocation.Phases.Single(p => p.PhaseKey == "FOUNDATION");
        var deficit = foundation.AllocatedWeeks - 1;
        var mutatedPhases = real.Allocation.Phases.Select(p =>
            p.PhaseKey == "FOUNDATION" ? p with { AllocatedWeeks = 1 } :
            p.PhaseKey == "BUILD" ? p with { AllocatedWeeks = p.AllocatedWeeks + deficit } :
            p).ToList();
        return real with { Allocation = real.Allocation with { Phases = mutatedPhases } };
    }

    private static SafetyVerificationPipelineResult SyntheticAllPassPipelineResult(int weeks) =>
        SyntheticPipelineResult(weeks, SafetyVerificationOverallOutcome.Pass);

    private static SafetyVerificationPipelineResult SyntheticFailPipelineResult(int weeks) =>
        SyntheticPipelineResult(weeks, SafetyVerificationOverallOutcome.Fail);

    private static SafetyVerificationPipelineResult SyntheticDecisionRequiredPipelineResult(int weeks) =>
        SyntheticPipelineResult(weeks, SafetyVerificationOverallOutcome.DecisionRequired);

    private static SafetyVerificationPipelineResult SyntheticNotApplicablePipelineResult(int weeks) =>
        SyntheticPipelineResult(weeks, SafetyVerificationOverallOutcome.NotApplicable);

    /// <summary>
    /// Test-local construction of a fully synthetic, self-consistent
    /// SafetyVerificationPipelineResult with every one of the nine typed
    /// results and every summary set to the same requested tier. Necessary
    /// because a genuine all-Pass real orchestrator result is structurally
    /// unreachable via SafetyVerificationOrchestrator.Run for the real
    /// GOAL_PACE_REHEARSAL stage (see Real12WeekPilot_* above) -- this
    /// mirrors the exact technique already established as necessary in
    /// SafetyVerificationOrchestratorTests for the same reason. Never calls
    /// SafetyVerificationOrchestrator.Run or any of the nine verifiers'
    /// Verify methods; only constructs already-defined typed records
    /// directly.
    /// </summary>
    private static SafetyVerificationPipelineResult SyntheticPipelineResult(int weeks, SafetyVerificationOverallOutcome tier)
    {
        var phaseConstraint = new PhaseConstraintVerificationResult(weeks, ToPhaseConstraintOutcome(tier), Array.Empty<string>());
        var raceSpecificCapacity = new RaceSpecificCapacityVerificationResult(weeks, weeks, weeks, 0, 0, 0, weeks, weeks,
            ToRaceSpecificCapacityOutcome(tier), Array.Empty<RaceSpecificCapacityFinding>());
        var stageReachability = new StageReachabilityVerificationResult(weeks, Array.Empty<PhaseStageReachabilityResult>(),
            ToStageReachabilityOutcome(tier), Array.Empty<StageReachabilityFinding>());
        var workoutExposure = new WorkoutExposureVerificationResult(weeks, 0, 0,
            new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(),
            Array.Empty<WeekWorkoutExposureResult>(), ToWorkoutExposureOutcome(tier), Array.Empty<WorkoutExposureFinding>());
        var goalPaceReachability = new GoalPaceReachabilityVerificationResult(Array.Empty<GoalPaceOutcomeCheck>(),
            ToGoalPaceReachabilityOutcome(tier), Array.Empty<string>());
        var readinessEligibility = new ReadinessEligibilityVerificationResult(weeks, Array.Empty<PhaseMinimumViolationCheck>(),
            ToReadinessEligibilityOutcome(tier), Array.Empty<string>());
        var volumeProgression = new VolumeProgressionVerificationResult(weeks, Array.Empty<WeeklyTransitionCheck>(),
            ToVolumeProgressionOutcome(tier), Array.Empty<string>());
        var longRunProgression = new LongRunProgressionVerificationResult(weeks, Array.Empty<LongRunWeekCheck>(),
            ToLongRunProgressionOutcome(tier), Array.Empty<string>());
        var startDate = new DateOnly(2026, 7, 20);
        var raceDateAlignment = new RaceDateAlignmentVerificationResult(weeks, startDate, startDate.AddDays(weeks * 7),
            startDate.AddDays(weeks * 7 - 1), Array.Empty<RaceDateAlignmentCheck>(), ToRaceDateAlignmentOutcome(tier), Array.Empty<string>());

        var summaries = new List<SafetyVerifierRunSummary>
        {
            new(CanonicalSafetyVerifier.PhaseConstraint, phaseConstraint.Outcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
            new(CanonicalSafetyVerifier.RaceSpecificCapacity, raceSpecificCapacity.Outcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
            new(CanonicalSafetyVerifier.StageReachability, stageReachability.Outcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
            new(CanonicalSafetyVerifier.WorkoutExposure, workoutExposure.Outcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
            new(CanonicalSafetyVerifier.GoalPaceReachability, goalPaceReachability.OverallOutcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
            new(CanonicalSafetyVerifier.ReadinessEligibility, readinessEligibility.Outcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
            new(CanonicalSafetyVerifier.VolumeProgression, volumeProgression.Outcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
            new(CanonicalSafetyVerifier.LongRunProgression, longRunProgression.Outcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
            new(CanonicalSafetyVerifier.RaceDateAlignment, raceDateAlignment.Outcome.ToString(), tier, Array.Empty<SafetyVerificationFinding>()),
        };

        return new SafetyVerificationPipelineResult(weeks, phaseConstraint, raceSpecificCapacity, stageReachability, workoutExposure,
            goalPaceReachability, readinessEligibility, volumeProgression, longRunProgression, raceDateAlignment,
            summaries, tier, Array.Empty<SafetyVerificationFinding>());
    }

    private static PhaseConstraintVerificationOutcome ToPhaseConstraintOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => PhaseConstraintVerificationOutcome.Pass,
        SafetyVerificationOverallOutcome.Fail => PhaseConstraintVerificationOutcome.Fail,
        SafetyVerificationOverallOutcome.NotApplicable => PhaseConstraintVerificationOutcome.NotApplicable,
        _ => PhaseConstraintVerificationOutcome.Pass, // DecisionRequired: this enum has no such value; PhaseConstraint itself stays Pass while another verifier below carries DecisionRequired.
    };

    private static RaceSpecificCapacityOutcome ToRaceSpecificCapacityOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => RaceSpecificCapacityOutcome.Pass,
        SafetyVerificationOverallOutcome.Fail => RaceSpecificCapacityOutcome.Fail,
        SafetyVerificationOverallOutcome.DecisionRequired => RaceSpecificCapacityOutcome.DecisionRequired,
        SafetyVerificationOverallOutcome.NotApplicable => RaceSpecificCapacityOutcome.NotApplicable,
        _ => RaceSpecificCapacityOutcome.Pass,
    };

    private static StageReachabilityOutcome ToStageReachabilityOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => StageReachabilityOutcome.Pass,
        SafetyVerificationOverallOutcome.Fail => StageReachabilityOutcome.Fail,
        SafetyVerificationOverallOutcome.DecisionRequired => StageReachabilityOutcome.DecisionRequired,
        SafetyVerificationOverallOutcome.NotApplicable => StageReachabilityOutcome.NotApplicable,
        _ => StageReachabilityOutcome.Pass,
    };

    private static WorkoutExposureOutcome ToWorkoutExposureOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => WorkoutExposureOutcome.Pass,
        SafetyVerificationOverallOutcome.Fail => WorkoutExposureOutcome.Fail,
        SafetyVerificationOverallOutcome.DecisionRequired => WorkoutExposureOutcome.DecisionRequired,
        SafetyVerificationOverallOutcome.NotApplicable => WorkoutExposureOutcome.NotApplicable,
        _ => WorkoutExposureOutcome.Pass,
    };

    private static GoalPaceReachabilityOutcome ToGoalPaceReachabilityOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => GoalPaceReachabilityOutcome.Pass,
        SafetyVerificationOverallOutcome.Fail => GoalPaceReachabilityOutcome.Fail,
        SafetyVerificationOverallOutcome.DecisionRequired => GoalPaceReachabilityOutcome.PassWithOpenRisk,
        SafetyVerificationOverallOutcome.NotApplicable => GoalPaceReachabilityOutcome.NotApplicable,
        _ => GoalPaceReachabilityOutcome.Pass,
    };

    private static ReadinessEligibilityOutcome ToReadinessEligibilityOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => ReadinessEligibilityOutcome.Pass,
        SafetyVerificationOverallOutcome.DecisionRequired => ReadinessEligibilityOutcome.DecisionRequired,
        SafetyVerificationOverallOutcome.NotApplicable => ReadinessEligibilityOutcome.NotApplicable,
        _ => ReadinessEligibilityOutcome.Pass, // this enum has no Fail value
    };

    private static VolumeProgressionOutcome ToVolumeProgressionOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => VolumeProgressionOutcome.Pass,
        SafetyVerificationOverallOutcome.Fail => VolumeProgressionOutcome.Fail,
        SafetyVerificationOverallOutcome.NotApplicable => VolumeProgressionOutcome.NotApplicable,
        _ => VolumeProgressionOutcome.Pass,
    };

    private static LongRunProgressionOutcome ToLongRunProgressionOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => LongRunProgressionOutcome.Pass,
        SafetyVerificationOverallOutcome.Fail => LongRunProgressionOutcome.Fail,
        SafetyVerificationOverallOutcome.NotApplicable => LongRunProgressionOutcome.NotApplicable,
        _ => LongRunProgressionOutcome.Pass,
    };

    private static RaceDateAlignmentOutcome ToRaceDateAlignmentOutcome(SafetyVerificationOverallOutcome tier) => tier switch
    {
        SafetyVerificationOverallOutcome.Pass => RaceDateAlignmentOutcome.Pass,
        SafetyVerificationOverallOutcome.Fail => RaceDateAlignmentOutcome.Fail,
        SafetyVerificationOverallOutcome.NotApplicable => RaceDateAlignmentOutcome.NotApplicable,
        _ => RaceDateAlignmentOutcome.Pass,
    };
}
