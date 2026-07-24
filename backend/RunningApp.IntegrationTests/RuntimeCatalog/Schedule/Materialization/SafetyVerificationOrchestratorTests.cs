using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Phase 4G.3B.4b -- executable proof for
/// <see cref="SafetyVerificationOrchestrator"/>: composition/aggregation
/// correctness, no short-circuit, typed-result preservation, dark
/// reachability, and independence from
/// <see cref="AllocationOrderCorrectnessVerifier"/> and governance files.
/// Does not modify any of the nine verifier implementations or tests.
/// </summary>
public sealed class SafetyVerificationOrchestratorTests
{
    private static readonly CanonicalSafetyVerifier[] ExpectedOrder =
    {
        CanonicalSafetyVerifier.PhaseConstraint,
        CanonicalSafetyVerifier.RaceSpecificCapacity,
        CanonicalSafetyVerifier.StageReachability,
        CanonicalSafetyVerifier.WorkoutExposure,
        CanonicalSafetyVerifier.GoalPaceReachability,
        CanonicalSafetyVerifier.ReadinessEligibility,
        CanonicalSafetyVerifier.VolumeProgression,
        CanonicalSafetyVerifier.LongRunProgression,
        CanonicalSafetyVerifier.RaceDateAlignment,
    };

    // ── A. Canonical verifier set ──────────────────────────────────────────

    [Fact]
    public async Task Run_ProducesExactlyNineSummaries_InDocumentedOrder_EachVerifierOnce()
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var result = SafetyVerificationOrchestrator.Run(context);

        Assert.Equal(9, result.OrderedSummaries.Count);
        Assert.Equal(ExpectedOrder, result.OrderedSummaries.Select(s => s.Verifier));
        Assert.Equal(ExpectedOrder.Distinct().Count(), result.OrderedSummaries.Select(s => s.Verifier).Distinct().Count());
    }

    // ── B. No short-circuit ────────────────────────────────────────────────

    [Fact]
    public async Task Run_WhenAnEarlyVerifierFails_LaterVerifiersStillExecute_AllNineResultsPopulated()
    {
        var mutated = await MutateFoundationBelowMinimumAsync();
        var result = SafetyVerificationOrchestrator.Run(mutated);

        // Verified below (Fail precedence, test F) that PhaseConstraint (index 1)
        // normalizes to Fail for this context. No-short-circuit proof: every
        // one of the later eight typed results is still present and
        // observable, not a default/placeholder value.
        Assert.Equal(9, result.OrderedSummaries.Count);
        Assert.NotNull(result.RaceSpecificCapacity);
        Assert.NotNull(result.StageReachability);
        Assert.NotNull(result.WorkoutExposure);
        Assert.NotNull(result.GoalPaceReachability);
        Assert.True(result.GoalPaceReachability.OutcomeChecks.Count > 0, "GoalPaceReachability (verifier 5, after the failing verifier 1) must still have actually executed its real per-value checks.");
        Assert.NotNull(result.ReadinessEligibility);
        Assert.NotNull(result.VolumeProgression);
        Assert.NotNull(result.LongRunProgression);
        Assert.NotNull(result.RaceDateAlignment);
    }

    // ── C. Real primary-path matrix (8-14) ─────────────────────────────────

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task Run_RealPrimaryPath_EightThroughFourteen_MeasuredOutcomeTable(int weeks)
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(weeks, "REALISTIC");
        var result = SafetyVerificationOrchestrator.Run(context);

        // Measured (not assumed) via a diagnostic run against the real
        // TEN_K__4D__INTERMEDIATE v10 candidate for every target in 8-14:
        // all eight verifiers other than GoalPaceReachability normalize to
        // Pass; GoalPaceReachability always normalizes to DecisionRequired
        // (PassWithOpenRisk, driven by its own always-present NotEvaluated
        // check -- see GoalPaceReachabilityVerifier's own Pass doc comment:
        // "Not achievable today (TD-NOTEVALUATED-FALLBACK-001 is still
        // open)"). Overall is therefore DecisionRequired for every target.
        AssertNormalized(result, CanonicalSafetyVerifier.PhaseConstraint, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.RaceSpecificCapacity, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.StageReachability, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.WorkoutExposure, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.GoalPaceReachability, SafetyVerificationOverallOutcome.DecisionRequired);
        AssertNormalized(result, CanonicalSafetyVerifier.ReadinessEligibility, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.VolumeProgression, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.LongRunProgression, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.RaceDateAlignment, SafetyVerificationOverallOutcome.Pass);
        Assert.Equal(SafetyVerificationOverallOutcome.DecisionRequired, result.OverallOutcome);

        // 8 weeks measured an additional RaceSpecificCapacity finding
        // (ExactFitZeroWorstCaseSlack) that does not occur at 9-14 -- a
        // real, measured difference, not assumed uniform across the range.
        var raceSpecificFindingCount = result.OrderedSummaries.Single(s => s.Verifier == CanonicalSafetyVerifier.RaceSpecificCapacity).Findings.Count;
        Assert.Equal(weeks == 8 ? 1 : 0, raceSpecificFindingCount);
    }

    // ── D. Real fallback-path matrix (8-14) ────────────────────────────────

    [Theory]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task Run_RealFallbackPath_EightThroughFourteen_MeasuredOutcomeTable(int weeks)
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(weeks, "UNSUPPORTED");
        var result = SafetyVerificationOrchestrator.Run(context);

        // Measured: identical tier pattern to the primary path (C) for every
        // target -- GoalPaceReachabilityVerifier enumerates every registered
        // GOAL_FEASIBILITY_IN value itself and does not consume
        // context.RuntimeConditions at all, so its result (and therefore the
        // overall outcome) is invariant to which single goal value the
        // fallback-path scheduling context used.
        AssertNormalized(result, CanonicalSafetyVerifier.PhaseConstraint, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.RaceSpecificCapacity, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.StageReachability, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.WorkoutExposure, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.GoalPaceReachability, SafetyVerificationOverallOutcome.DecisionRequired);
        AssertNormalized(result, CanonicalSafetyVerifier.ReadinessEligibility, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.VolumeProgression, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.LongRunProgression, SafetyVerificationOverallOutcome.Pass);
        AssertNormalized(result, CanonicalSafetyVerifier.RaceDateAlignment, SafetyVerificationOverallOutcome.Pass);
        Assert.Equal(SafetyVerificationOverallOutcome.DecisionRequired, result.OverallOutcome);

        var raceSpecificFindingCount = result.OrderedSummaries.Single(s => s.Verifier == CanonicalSafetyVerifier.RaceSpecificCapacity).Findings.Count;
        Assert.Equal(weeks == 8 ? 1 : 0, raceSpecificFindingCount);
    }

    // ── E. Real 12-week pilot ───────────────────────────────────────────────

    [Fact]
    public async Task Run_RealTwelveWeekPilot_MeasuredPerVerifierAndOverallOutcome_FindingsNotEmpty()
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var result = SafetyVerificationOrchestrator.Run(context);

        Assert.Equal(12, result.TargetWeeks);
        Assert.Equal(PhaseConstraintVerificationOutcome.Pass, result.PhaseConstraint.Outcome);
        Assert.Equal(RaceSpecificCapacityOutcome.Pass, result.RaceSpecificCapacity.Outcome);
        Assert.Equal(StageReachabilityOutcome.Pass, result.StageReachability.Outcome);
        Assert.Equal(WorkoutExposureOutcome.Pass, result.WorkoutExposure.Outcome);
        Assert.Equal(GoalPaceReachabilityOutcome.PassWithOpenRisk, result.GoalPaceReachability.OverallOutcome);
        Assert.Equal(ReadinessEligibilityOutcome.Pass, result.ReadinessEligibility.Outcome);
        Assert.Equal(VolumeProgressionOutcome.Pass, result.VolumeProgression.Outcome);
        Assert.Equal(LongRunProgressionOutcome.Pass, result.LongRunProgression.Outcome);
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
        Assert.Equal(SafetyVerificationOverallOutcome.DecisionRequired, result.OverallOutcome);

        // Measured: 10 real findings for the 12-week pilot (4 ExactStageReachabilityFit
        // + 1 ExactWorkoutExposureMatch + 5 GoalPaceReachability outcome checks).
        // Never assume Pass/DecisionRequired implies zero findings for this
        // codebase -- several verifiers attach an informational finding to a
        // Pass outcome (e.g. ExactWorkoutExposureMatch, ExactStageReachabilityFit).
        Assert.Equal(10, result.AggregatedFindings.Count);
        Assert.DoesNotContain(result.AggregatedFindings, f => f.SourceVerifier == CanonicalSafetyVerifier.RaceSpecificCapacity);
    }

    // ── F. Fail precedence ──────────────────────────────────────────────────

    [Fact]
    public async Task Run_ContextWithFailAndLaterDecisionRequired_OverallOutcomeIsFail_AllNineResultsPresent()
    {
        var mutated = await MutateFoundationBelowMinimumAsync();
        var result = SafetyVerificationOrchestrator.Run(mutated);

        var phaseConstraintSummary = result.OrderedSummaries.Single(s => s.Verifier == CanonicalSafetyVerifier.PhaseConstraint);
        var goalPaceSummary = result.OrderedSummaries.Single(s => s.Verifier == CanonicalSafetyVerifier.GoalPaceReachability);

        Assert.Equal(SafetyVerificationOverallOutcome.Fail, phaseConstraintSummary.NormalizedOutcome);
        Assert.Equal(SafetyVerificationOverallOutcome.DecisionRequired, goalPaceSummary.NormalizedOutcome);
        Assert.Equal(SafetyVerificationOverallOutcome.Fail, result.OverallOutcome);
        Assert.Equal(9, result.OrderedSummaries.Count);
    }

    // ── G. DecisionRequired precedence ──────────────────────────────────────

    [Fact]
    public async Task Run_ContextWithNoFail_AtLeastOneDecisionRequired_OverallOutcomeIsDecisionRequired()
    {
        // The real, unmutated 12-week pilot IS this scenario: measured (E)
        // to have zero Fail among its nine normalized tiers and exactly one
        // DecisionRequired (GoalPaceReachability) -- reused directly rather
        // than constructing a redundant synthetic case.
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var result = SafetyVerificationOrchestrator.Run(context);

        Assert.DoesNotContain(result.OrderedSummaries, s => s.NormalizedOutcome == SafetyVerificationOverallOutcome.Fail);
        Assert.Contains(result.OrderedSummaries, s => s.NormalizedOutcome == SafetyVerificationOverallOutcome.DecisionRequired);
        Assert.Equal(SafetyVerificationOverallOutcome.DecisionRequired, result.OverallOutcome);
    }

    // ── H. Clean Pass ────────────────────────────────────────────────────────

    [Fact]
    public async Task GoalPaceReachabilityVerifier_CanNeverNormalizeToPass_ForTheRealStage_SoNoGenuineCleanPassContextExists()
    {
        // Structural proof, not just an empirical 8-14 observation:
        // GoalPaceReachabilityVerifier.Verify ALWAYS appends exactly one
        // NotEvaluated check (CheckNotEvaluated is called unconditionally,
        // never behind a condition), and CheckNotEvaluated's own source can
        // only ever produce GoalPaceOutcomeStatus.StructurallyUnreachable or
        // UncertainNotEvaluated -- never Eligible/FallbackConfirmed. Since
        // GoalPaceReachabilityOutcome.Pass requires the absence of BOTH
        // StructurallyUnreachable and UncertainNotEvaluated among all
        // checks, and the always-present NotEvaluated check is guaranteed to
        // be one of exactly those two, GoalPaceReachabilityOutcome.Pass is
        // unreachable for any mathematically-feasible allocation using the
        // real GOAL_PACE_REHEARSAL stage shape -- confirming the verifier's
        // own doc comment ("Not achievable today"). Per this phase's own
        // instruction, this is documented rather than faked: no fabricated
        // all-nine-Pass context is constructed. The closest genuinely
        // achievable state -- all eight OTHER verifiers Pass, only
        // GoalPaceReachability contributing DecisionRequired -- is already
        // measured and asserted by tests C, D, E, and G above.
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var result = SafetyVerificationOrchestrator.Run(context);

        Assert.NotEqual(GoalPaceReachabilityOutcome.Pass, result.GoalPaceReachability.OverallOutcome);
        Assert.True(result.GoalPaceReachability.OutcomeChecks.Any(c => c.GoalFeasibilityValue == "NOT_EVALUATED"),
            "The always-present synthetic NotEvaluated check must be observable in the real result.");

        var eightOthersAllPass = result.OrderedSummaries
            .Where(s => s.Verifier != CanonicalSafetyVerifier.GoalPaceReachability)
            .All(s => s.NormalizedOutcome == SafetyVerificationOverallOutcome.Pass);
        Assert.True(eightOthersAllPass, "The closest achievable 'clean' state: all eight other verifiers Pass.");
    }

    // ── I. Root mathematical infeasibility ──────────────────────────────────

    [Fact]
    public async Task Run_RootAllocationMathematicallyInfeasible_OverallOutcomeIsNotApplicable_AllNineStillReturned()
    {
        var context = await SafetyVerificationContextFixtures.RealMathematicallyInfeasibleAsync();
        var result = SafetyVerificationOrchestrator.Run(context);

        Assert.Equal(SafetyVerificationOverallOutcome.NotApplicable, result.OverallOutcome);
        Assert.Equal(9, result.OrderedSummaries.Count);
        Assert.Equal(PhaseConstraintVerificationOutcome.NotApplicable, result.PhaseConstraint.Outcome);
        Assert.Equal(RaceSpecificCapacityOutcome.NotApplicable, result.RaceSpecificCapacity.Outcome);
        Assert.Equal(StageReachabilityOutcome.NotApplicable, result.StageReachability.Outcome);
        Assert.Equal(WorkoutExposureOutcome.NotApplicable, result.WorkoutExposure.Outcome);
        Assert.Equal(GoalPaceReachabilityOutcome.NotApplicable, result.GoalPaceReachability.OverallOutcome);
        Assert.Equal(ReadinessEligibilityOutcome.NotApplicable, result.ReadinessEligibility.Outcome);

        // VolumeProgression/LongRunProgression/RaceDateAlignment do not
        // themselves consume Allocation.IsMathematicallyFeasible at all (see
        // test J) -- their own NotApplicable trigger is independent, and
        // with an otherwise-real, non-empty VolumePlan/LongRunPlan/DatedSchedule
        // they legitimately still report Pass here. This is exactly the
        // documented, executable proof that OverallOutcome==NotApplicable is
        // driven solely by the root allocation flag, never by silently
        // requiring every individual verifier to also report NotApplicable.
        Assert.Equal(VolumeProgressionOutcome.Pass, result.VolumeProgression.Outcome);
        Assert.Equal(LongRunProgressionOutcome.Pass, result.LongRunProgression.Outcome);
        Assert.Equal(RaceDateAlignmentOutcome.Pass, result.RaceDateAlignment.Outcome);
    }

    // ── J. NotApplicable semantic distinction ──────────────────────────────

    [Fact]
    public async Task Run_NonRootNotApplicable_GoalPaceUnexpectedStageShape_DoesNotForceOverallNotApplicable()
    {
        var real = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var mismatchedStage = new CatalogWorkoutProgressionStage
        {
            ProgressionStageKey = "NOT_THE_REAL_GOAL_PACE_STAGE_KEY",
            RelativeOrder = real.GoalPaceStage.RelativeOrder,
            MinimumExposures = real.GoalPaceStage.MinimumExposures,
            MaximumExposures = real.GoalPaceStage.MaximumExposures,
            CompressionBehavior = real.GoalPaceStage.CompressionBehavior,
            ExtensionBehavior = real.GoalPaceStage.ExtensionBehavior,
            Requires = real.GoalPaceStage.Requires,
            FallbackStageKey = real.GoalPaceStage.FallbackStageKey,
        };
        var context = real with { GoalPaceStage = mismatchedStage };

        Assert.True(context.Allocation.IsMathematicallyFeasible, "This test's whole point requires a feasible root allocation.");

        var result = SafetyVerificationOrchestrator.Run(context);

        Assert.Equal(GoalPaceReachabilityOutcome.NotApplicable, result.GoalPaceReachability.OverallOutcome);
        var goalPaceSummary = result.OrderedSummaries.Single(s => s.Verifier == CanonicalSafetyVerifier.GoalPaceReachability);
        Assert.Equal(SafetyVerificationOverallOutcome.NotApplicable, goalPaceSummary.NormalizedOutcome);

        // The per-verifier summary honestly preserves NotApplicable, but
        // because the ROOT allocation is feasible, OverallOutcome must never
        // collapse to NotApplicable -- it is escalated to Fail for
        // aggregation purposes (see SafetyVerificationOrchestrator.AggregateOverallOutcome's
        // own doc comment for the reasoning).
        Assert.NotEqual(SafetyVerificationOverallOutcome.NotApplicable, result.OverallOutcome);
        Assert.Equal(SafetyVerificationOverallOutcome.Fail, result.OverallOutcome);
    }

    // ── K. Typed result preservation ────────────────────────────────────────

    [Fact]
    public async Task Run_TypedResultProperties_ContainExactCorrespondingVerifierOutput_NoCastingOrReflection()
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var result = SafetyVerificationOrchestrator.Run(context);

        var expectedPhaseConstraint = PhaseConstraintVerifier.Verify(context.Allocation);
        var expectedReadiness = ReadinessEligibilityVerifier.Verify(context.Allocation);
        var expectedVolume = VolumeProgressionVerifier.Verify(context.VolumePlan, context.Policy);
        var expectedRaceDate = RaceDateAlignmentVerifier.Verify(context.DatedSchedule, context.RaceDate);

        // Records whose properties include a mutable List<T> (Findings) do
        // not get free structural equality from the record-generated
        // Equals -- List<T> itself uses reference equality regardless of
        // element type, so two independently-constructed verifier results
        // with identical content are NOT `Assert.Equal` at the whole-record
        // level even though they are identical. Decomposing into scalar
        // properties plus an explicit top-level sequence comparison on
        // Findings (which DOES get xUnit's proper element-wise comparison)
        // is the correct, non-reflective way to prove preservation.
        Assert.Equal(expectedPhaseConstraint.TargetWeeks, result.PhaseConstraint.TargetWeeks);
        Assert.Equal(expectedPhaseConstraint.Outcome, result.PhaseConstraint.Outcome);
        Assert.Equal(expectedPhaseConstraint.Findings, result.PhaseConstraint.Findings);

        Assert.Equal(expectedReadiness.TargetWeeks, result.ReadinessEligibility.TargetWeeks);
        Assert.Equal(expectedReadiness.Outcome, result.ReadinessEligibility.Outcome);
        Assert.Equal(expectedReadiness.Findings, result.ReadinessEligibility.Findings);

        Assert.Equal(expectedVolume.TargetWeeks, result.VolumeProgression.TargetWeeks);
        Assert.Equal(expectedVolume.Outcome, result.VolumeProgression.Outcome);
        Assert.Equal(expectedVolume.Findings, result.VolumeProgression.Findings);

        Assert.Equal(expectedRaceDate.FinalSessionDate, result.RaceDateAlignment.FinalSessionDate);
        Assert.Equal(expectedRaceDate.Outcome, result.RaceDateAlignment.Outcome);
        Assert.Equal(expectedRaceDate.Findings, result.RaceDateAlignment.Findings);
    }

    // ── L. Finding aggregation ───────────────────────────────────────────────

    [Fact]
    public async Task Run_AggregatedFindings_PreserveSourceVerifierOrderAndAreNotDeduplicated()
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var result = SafetyVerificationOrchestrator.Run(context);

        var expectedOrderOfSources = result.OrderedSummaries.SelectMany(s => s.Findings.Select(_ => s.Verifier)).ToList();
        Assert.Equal(expectedOrderOfSources, result.AggregatedFindings.Select(f => f.SourceVerifier));

        // Four identical ExactStageReachabilityFit findings (one per phase)
        // are legitimately present and must NOT be silently deduplicated --
        // each is a distinct typed source finding (different phase), not an
        // exact duplicate projection.
        Assert.Equal(4, result.AggregatedFindings.Count(f => f.Code == "ExactStageReachabilityFit"));
        Assert.All(result.AggregatedFindings, f => Assert.False(string.IsNullOrWhiteSpace(f.Code)));
        Assert.All(result.AggregatedFindings, f => Assert.False(string.IsNullOrWhiteSpace(f.Message)));
    }

    // ── M. Determinism ──────────────────────────────────────────────────────

    [Fact]
    public async Task Run_TwoRunsWithIdenticalContext_ProduceEqualResults()
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var first = SafetyVerificationOrchestrator.Run(context);
        var second = SafetyVerificationOrchestrator.Run(context);

        Assert.Equal(first.OverallOutcome, second.OverallOutcome);
        Assert.Equal(first.AggregatedFindings, second.AggregatedFindings);

        // See Run_TypedResultProperties_* above for why a whole-record
        // comparison of a type containing a List<T> property does not work
        // -- decompose per-summary instead.
        Assert.Equal(first.OrderedSummaries.Count, second.OrderedSummaries.Count);
        foreach (var (a, b) in first.OrderedSummaries.Zip(second.OrderedSummaries))
        {
            Assert.Equal(a.Verifier, b.Verifier);
            Assert.Equal(a.OriginalOutcome, b.OriginalOutcome);
            Assert.Equal(a.NormalizedOutcome, b.NormalizedOutcome);
            Assert.Equal(a.Findings, b.Findings);
        }

        Assert.Equal(first.PhaseConstraint.Outcome, second.PhaseConstraint.Outcome);
        Assert.Equal(first.PhaseConstraint.Findings, second.PhaseConstraint.Findings);
        Assert.Equal(first.RaceDateAlignment.Outcome, second.RaceDateAlignment.Outcome);
        Assert.Equal(first.RaceDateAlignment.Findings, second.RaceDateAlignment.Findings);
    }

    // ── N. No materialization inside Run ────────────────────────────────────

    [Fact]
    public void Orchestrator_DoesNotDirectlyInvoke_AnyMaterializationAllocationOrBindingComponent()
    {
        var source = File.ReadAllText(OrchestratorSourcePath());

        // Bounded to constructor/invocation call sites of the forbidden
        // components -- "new X(" / "X.Something(" -- not a whole-word ban
        // that would also reject legitimate type-name usage in the typed
        // context record and adapter signatures (e.g. the orchestrator must
        // legitimately reference DatedGeneratedCatalogPlanSkeleton and
        // BoundCatalogPlan as CONTEXT FIELD TYPES, which is required and
        // expected -- only actually constructing/materializing them itself
        // would be a violation).
        var forbiddenConstructions = new[]
        {
            "new CatalogPhaseAllocationResolver(", "new GenericPhaseAllocator(", "new CatalogStageToWeekMaterializer(",
            "new ProgressionStageAllocator(", "new CatalogWeekSkeletonCalendarMaterializer(", "new CatalogWorkoutBinder(",
            "new CatalogVolumeAndLongRunPlanner(", "new PlanCatalogBundleLoader(", "new CatalogWorkoutProgressionLoader(",
            "new CatalogWorkoutDefinitionLoader(", "new CatalogPeakVolumeBandLoader(", "new RuntimeConditionRegistryReader(",
            "new TimeAdequacyResolver(", "new PaceSourceResolver(", "new CoreEntryReadinessResolver(", "new GoalFeasibilityResolver(",
            "new RuntimeConditionResolutionService(",
        };

        foreach (var forbidden in forbiddenConstructions)
        {
            Assert.DoesNotContain(forbidden, source);
        }
    }

    // ── O. No duplicated verifier logic ─────────────────────────────────────

    [Fact]
    public void Orchestrator_InvokesEachRealVerifyMethod_ContainsNoCopiedVerifierLogic()
    {
        var source = File.ReadAllText(OrchestratorSourcePath());

        foreach (var call in new[]
        {
            "PhaseConstraintVerifier.Verify(", "RaceSpecificCapacityVerifier.Verify(", "StageReachabilityVerifier.Verify(",
            "WorkoutExposureVerifier.Verify(", "GoalPaceReachabilityVerifier.Verify(", "ReadinessEligibilityVerifier.Verify(",
            "VolumeProgressionVerifier.Verify(", "LongRunProgressionVerifier.Verify(", "RaceDateAlignmentVerifier.Verify(",
        })
        {
            Assert.Contains(call, source);
        }

        // Bounded structural checks, not brittle whole-file keyword bans:
        // these specific numeric/string literals are the actual safety
        // thresholds/constants owned by individual verifiers (volume ratio
        // caps, the 7-day race-date tolerance, readiness classification
        // tokens) -- their presence in the orchestrator file would indicate
        // a genuinely copied threshold, not a legitimate adapter reference
        // (adapter code only ever references TYPE/METHOD/ENUM names, never
        // these literal values).
        Assert.DoesNotContain("HardMaxWeeklyIncreaseRatio =", source);
        Assert.DoesNotContain("LongRunHardCapShare =", source);
        Assert.DoesNotContain("maxAllowedTrailingGapDays", source);
        Assert.DoesNotContain("READY", source);
        Assert.DoesNotContain("CAUTION", source);
        Assert.DoesNotContain("NOT_READY", source);
        Assert.DoesNotContain("KEY_SESSION", source);
        Assert.DoesNotContain("EASY_SUPPORT", source);
        Assert.DoesNotContain("LONG_RUN_STANDARD", source);
    }

    // ── P. Independence from AllocationOrderCorrectnessVerifier ────────────

    [Fact]
    public void Orchestrator_NeverReferencesAllocationOrderCorrectnessVerifier()
    {
        var source = File.ReadAllText(OrchestratorSourcePath());

        // The orchestrator's own doc comment legitimately explains WHY
        // AllocationOrderCorrectnessVerifier is excluded (prose, not code) --
        // what must be absent is any actual invocation or instantiation.
        Assert.DoesNotContain("AllocationOrderCorrectnessVerifier.Verify(", source);
        Assert.DoesNotContain("new AllocationOrderCorrectnessVerifier(", source);
    }

    [Fact]
    public async Task Run_ResultIndependentOf_AllocationOrderCorrectnessVerifierOutcome()
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var before = AllocationOrderCorrectnessVerifier.Verify(context.Allocation);
        var result = SafetyVerificationOrchestrator.Run(context);
        var after = AllocationOrderCorrectnessVerifier.Verify(context.Allocation);

        Assert.Equal(before, after); // running the orchestrator has no observable side effect on it
        Assert.Equal(SafetyVerificationOverallOutcome.DecisionRequired, result.OverallOutcome); // unaffected by AllocationOrderCorrectnessVerifier's own result either way
    }

    // ── Q. Dark reachability ─────────────────────────────────────────────────

    [Fact]
    public void Orchestrator_HasZeroProductionCallSites_NoDIRegistration_TestsAreOnlyCaller()
    {
        DarkReachabilityAssertions.AssertOrchestratorHasNoLiveActivation();

        var repo = TestPlanServicesFactory.RepoRoot();
        foreach (var root in new[] { Path.Combine(repo, "backend", "RunningApp.Application"), Path.Combine(repo, "backend", "RunningApp.Api") })
        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.EndsWith("SafetyVerificationOrchestrator.cs")))
        {
            Assert.DoesNotContain("SafetyVerificationOrchestrator.Run(", File.ReadAllText(file));
        }

        var programSource = File.ReadAllText(Path.Combine(repo, "backend", "RunningApp.Api", "Program.cs"));
        Assert.DoesNotContain("SafetyVerificationOrchestrator", programSource);
    }

    // ── R. No governance consumption ────────────────────────────────────────

    [Fact]
    public void Orchestrator_NeverAccessesGovernanceInventoryFiles_ButMayPropagateFindingsContainingTheirNames()
    {
        var source = File.ReadAllText(OrchestratorSourcePath());

        foreach (var ioApi in new[] { "File.", "Path.Combine", "Stream", "JsonDocument", "JsonSerializer", "IConfiguration", "Directory." })
        {
            Assert.DoesNotContain(ioApi, source);
        }

        // Deliberately NOT asserting the orchestrator source never mentions
        // "activation-readiness-risks" or "TD-" -- it legitimately never
        // does today (it contains no literal TD/governance citation of its
        // own), but the real, unmodified ReadinessEligibilityVerifier and
        // GoalPaceReachabilityVerifier findings DO legitimately carry
        // TD-FOUNDATION-COMPRESSION-001/TD-NOTEVALUATED-FALLBACK-001 text
        // when propagated through aggregation -- asserting zero mentions in
        // the AGGREGATED FINDINGS would be false and would contradict this
        // phase's own instruction not to assert zero filename mentions when
        // they legitimately occur in diagnostic findings.
    }

    [Fact]
    public async Task Run_ReadinessEligibilityFinding_MayLegitimatelyPropagateATdCitation_WhenTheRealVerifierProducesOne()
    {
        var mutated = await MutateFoundationBelowMinimumAsync();
        var result = SafetyVerificationOrchestrator.Run(mutated);

        var readinessSummary = result.OrderedSummaries.Single(s => s.Verifier == CanonicalSafetyVerifier.ReadinessEligibility);
        Assert.Contains(readinessSummary.Findings, f => f.Message.Contains("TD-FOUNDATION-COMPRESSION-001"));
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static void AssertNormalized(SafetyVerificationPipelineResult result, CanonicalSafetyVerifier verifier, SafetyVerificationOverallOutcome expected)
    {
        var actual = result.OrderedSummaries.Single(s => s.Verifier == verifier).NormalizedOutcome;
        Assert.True(expected == actual, $"{verifier}: expected normalized outcome {expected}, got {actual}.");
    }

    private static string OrchestratorSourcePath() => Path.Combine(
        TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog",
        "Schedule", "Materialization", "SafetyVerificationOrchestrator.cs");

    /// <summary>Real 12-week context with FOUNDATION's AllocatedWeeks reduced to 1 (below its own catalog MinimumWeeks=2), redistributing the deficit into BUILD so TargetWeeks/sum stay consistent and downstream materialization (which only needs weeks to sum correctly, not individual bounds) keeps working. Triggers PhaseConstraintVerifier Fail (PHASE_BELOW_MINIMUM) without touching root mathematical feasibility.</summary>
    private static async Task<SafetyVerificationContext> MutateFoundationBelowMinimumAsync()
    {
        var real = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var foundation = real.Allocation.Phases.Single(p => p.PhaseKey == "FOUNDATION");
        var build = real.Allocation.Phases.Single(p => p.PhaseKey == "BUILD");
        var deficit = foundation.AllocatedWeeks - 1;

        var mutatedPhases = real.Allocation.Phases.Select(p =>
            p.PhaseKey == "FOUNDATION" ? p with { AllocatedWeeks = 1 } :
            p.PhaseKey == "BUILD" ? p with { AllocatedWeeks = p.AllocatedWeeks + deficit } :
            p).ToList();

        return real with { Allocation = real.Allocation with { Phases = mutatedPhases } };
    }
}
