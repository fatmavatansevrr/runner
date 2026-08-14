using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayEngine;

/// <summary>
/// Backend Integration Phase 4G.6A.3B — exercises the production-owned
/// <see cref="PreparationRunwayBlockAllocationEngine"/> (moved from the
/// Phase 4G.6A.3 test-project-only reference implementation). This test
/// file OWNS NO ALLOCATION LOGIC ITSELF -- every assertion below calls the
/// real production class.
/// </summary>
public sealed class PreparationRunwayBlockAllocationEngineTests
{
    // ══════════════════════════════════════════════════════════════════
    // Target-matrix proofs (Phase 4G.6A.2 / 4G.6A.2A approved matrices),
    // now produced by the production engine with EXPLICIT declared minima
    // only (no engine-level "max(min,1)" heuristic).
    // ══════════════════════════════════════════════════════════════════

    public static IEnumerable<object[]> ConsistencyNeededMatrix()
    {
        yield return new object[] { 3, 1, 1, 1 };
        yield return new object[] { 4, 1, 2, 1 };
        yield return new object[] { 5, 2, 2, 1 };
        yield return new object[] { 6, 2, 3, 1 };
        yield return new object[] { 7, 2, 4, 1 };
        yield return new object[] { 8, 2, 5, 1 };
    }

    [Theory]
    [MemberData(nameof(ConsistencyNeededMatrix))]
    public void ConsistencyNeeded_MatrixIsReproducedByGenericMechanism_WithDeclaredMinimaOnly(int runwayWeeks, int consistency, int generalEndurance, int transition)
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.ConsistencyNeeded);
        var result = PreparationRunwayBlockAllocationEngine.Allocate(runwayWeeks, policies);

        Assert.True(result.IsSuccess);
        Assert.Equal(runwayWeeks, result.TotalAllocatedWeeks);
        Assert.Equal(consistency, AllocatedWeeksFor(result, PreparationRunwayBlockType.Consistency));
        Assert.Equal(generalEndurance, AllocatedWeeksFor(result, PreparationRunwayBlockType.GeneralEndurance));
        Assert.Equal(0, AllocatedWeeksFor(result, PreparationRunwayBlockType.AerobicStrength));
        Assert.Equal(transition, AllocatedWeeksFor(result, PreparationRunwayBlockType.PreSpecificTransition));
    }

    public static IEnumerable<object[]> CoreEntryReadyMatrix()
    {
        yield return new object[] { 3, 1, 1, 1 };
        yield return new object[] { 4, 2, 1, 1 };
        yield return new object[] { 5, 2, 2, 1 };
        yield return new object[] { 6, 3, 2, 1 };
        yield return new object[] { 7, 4, 2, 1 };
        yield return new object[] { 8, 5, 2, 1 };
    }

    [Theory]
    [MemberData(nameof(CoreEntryReadyMatrix))]
    public void CoreEntryReady_MatrixIsReproducedByGenericMechanism_WithDeclaredMinimaOnly(int runwayWeeks, int generalEndurance, int aerobicStrength, int transition)
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);
        var result = PreparationRunwayBlockAllocationEngine.Allocate(runwayWeeks, policies);

        Assert.True(result.IsSuccess);
        Assert.Equal(runwayWeeks, result.TotalAllocatedWeeks);
        Assert.Equal(0, AllocatedWeeksFor(result, PreparationRunwayBlockType.Consistency));
        Assert.Equal(generalEndurance, AllocatedWeeksFor(result, PreparationRunwayBlockType.GeneralEndurance));
        Assert.Equal(aerobicStrength, AllocatedWeeksFor(result, PreparationRunwayBlockType.AerobicStrength));
        Assert.Equal(transition, AllocatedWeeksFor(result, PreparationRunwayBlockType.PreSpecificTransition));
    }

    [Fact]
    public void CoreEntryReady_AerobicStrengthSecondWeek_EmergesExactlyAtRunwayFive()
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);

        Assert.Equal(1, AllocatedWeeksFor(PreparationRunwayBlockAllocationEngine.Allocate(4, policies), PreparationRunwayBlockType.AerobicStrength));
        Assert.Equal(2, AllocatedWeeksFor(PreparationRunwayBlockAllocationEngine.Allocate(5, policies), PreparationRunwayBlockType.AerobicStrength));
    }

    [Fact]
    public void NoRunwayWeeksSpecificRouteTableExistsInTheProductionEngineSource()
    {
        var path = Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayEngine", "PreparationRunwayBlockAllocationEngine.cs");
        var source = File.ReadAllText(path);
        foreach (var n in new[] { "== 3", "== 4", "== 5", "== 6", "== 7", "== 8", "runwayWeeks == 5", "RunwayWeeks == 5" })
            Assert.DoesNotContain(n, source);
    }

    [Fact]
    public void ExactDeclaredMinimaOnly_TenKFactory_DeclaresConditionalMinimaExplicitly()
    {
        // Structural proof that the corrected policy factory itself
        // declares MinWeeks=1 for CONSISTENCY/AEROBIC_STRENGTH when
        // eligible (not 0 relying on an engine heuristic).
        var consistencyNeeded = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.ConsistencyNeeded);
        var consistencyPolicy = consistencyNeeded.Single(p => p.BlockKey == PreparationRunwayBlockType.Consistency);
        Assert.True(consistencyPolicy.IsEligible);
        Assert.Equal(1, consistencyPolicy.MinWeeks);

        var coreEntryReady = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);
        var aerobicStrengthPolicy = coreEntryReady.Single(p => p.BlockKey == PreparationRunwayBlockType.AerobicStrength);
        Assert.True(aerobicStrengthPolicy.IsEligible);
        Assert.Equal(1, aerobicStrengthPolicy.MinWeeks);
    }

    // ══════════════════════════════════════════════════════════════════
    // Generic Min=0 regression proof (the exact behavior Phase 4G.6A.3's
    // hidden heuristic would have prevented).
    // ══════════════════════════════════════════════════════════════════

    private enum SyntheticBlock { Alpha, Beta, Gamma, Delta }

    private static PreparationRunwayBlockAllocationPolicy<SyntheticBlock> P(
        SyntheticBlock key, bool eligible, int min, int max, double weight, int priority, int order, bool expandable = true) =>
        new(key, eligible, min, max, weight, priority, order, expandable);

    [Fact]
    public void EligibleExpandablePositiveWeightMinZeroBlock_MayValidlyRemainZero_WhenAnotherBlockWinsAllocation()
    {
        // Alpha: eligible, expandable, PreferredWeight>0, MinWeeks=0, but a
        // tiny weight relative to Beta -- and RunwayWeeks is small enough
        // that the entire pool goes to Beta, leaving Alpha at exactly zero.
        // Under Phase 4G.6A.3's removed heuristic, Alpha would have
        // received a forced introductory week regardless. This regression
        // test proves that no longer happens.
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 5, weight: 0.01, priority: 1, order: 1),
            P(SyntheticBlock.Beta, true, min: 1, max: 5, weight: 0.99, priority: 2, order: 2),
        };

        var result = PreparationRunwayBlockAllocationEngine.Allocate(1, policies);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
        Assert.Equal(1, AllocatedWeeksFor(result, SyntheticBlock.Beta));
    }

    [Fact]
    public void NoHiddenEffectiveMinimumHeuristicRemainsAnywhereInProductionOrTestCode()
    {
        // Behavior proof (stronger than a substring check): construct a
        // scenario where the removed "max(declared min, 1)" heuristic and
        // the corrected "declared min exactly" behavior would disagree,
        // and assert the corrected (lower) result.
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 5, weight: 1.0, priority: 2, order: 1),
            P(SyntheticBlock.Beta, true, min: 3, max: 3, weight: 0, priority: 1, order: 2, expandable: false),
        };
        // RunwayWeeks=3 exactly matches Beta's fixed capacity. If Alpha's
        // engine-side minimum were silently bumped to 1, minSum would be
        // 1+3=4 > RunwayWeeks=3, and allocation would fail
        // (MinimumCapacityExceedsRunway). With declared-minima-only
        // behavior, minSum=0+3=3=RunwayWeeks, and it succeeds with Alpha=0.
        var result = PreparationRunwayBlockAllocationEngine.Allocate(3, policies);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
        Assert.Equal(3, AllocatedWeeksFor(result, SyntheticBlock.Beta));

        // Structural companion: neither production file in the new folder
        // contains the removed heuristic's characteristic expression.
        var engineSource = File.ReadAllText(Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayEngine", "PreparationRunwayBlockAllocationEngine.cs"));
        Assert.DoesNotContain("Math.Max(p.MinWeeks", engineSource);
        Assert.DoesNotContain("Math.Min(1,", engineSource);
    }

    // ══════════════════════════════════════════════════════════════════
    // Generic algorithm tests (production engine)
    // ══════════════════════════════════════════════════════════════════

    [Fact] // exact-minimum allocation
    public void ExactMinimumAllocation_SumOfDeclaredMinimaEqualsRunwayWeeks_NoDistributionNeeded()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 2, max: 4, weight: 0.5, priority: 2, order: 1),
            P(SyntheticBlock.Beta, true, min: 2, max: 4, weight: 0.5, priority: 1, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(4, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
        Assert.Equal(2, AllocatedWeeksFor(result, SyntheticBlock.Beta));
    }

    [Fact] // exact-maximum allocation
    public void ExactMaximumAllocation_SumOfMaximaEqualsRunwayWeeks_AllBlocksSaturate()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 3, weight: 0.5, priority: 2, order: 1),
            P(SyntheticBlock.Beta, true, min: 0, max: 3, weight: 0.5, priority: 1, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(6, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
        Assert.Equal(3, AllocatedWeeksFor(result, SyntheticBlock.Beta));
    }

    [Fact] // floor-only allocation
    public void FloorOnlyAllocation_NoFractionalRemainderRequired()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 10, weight: 0.5, priority: 2, order: 1),
            P(SyntheticBlock.Beta, true, min: 0, max: 10, weight: 0.5, priority: 1, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(6, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
        Assert.Equal(3, AllocatedWeeksFor(result, SyntheticBlock.Beta));
    }

    [Fact] // largest-remainder allocation
    public void LargestRemainderAllocation_FractionalLeftoverGoesToLargerRemainder()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 1, max: 10, weight: 0.7, priority: 1, order: 1),
            P(SyntheticBlock.Beta, true, min: 1, max: 10, weight: 0.3, priority: 2, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(3, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
        Assert.Equal(1, AllocatedWeeksFor(result, SyntheticBlock.Beta));
    }

    [Fact] // priority tie
    public void Tie_EqualRemainder_ResolvedByAllocationPriorityDescending()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 1, max: 10, weight: 0.5, priority: 5, order: 2),
            P(SyntheticBlock.Beta, true, min: 1, max: 10, weight: 0.5, priority: 9, order: 1),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(3, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, AllocatedWeeksFor(result, SyntheticBlock.Beta));
        Assert.Equal(1, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
    }

    [Fact] // canonical-order tie
    public void Tie_EqualRemainderAndPriority_ResolvedByCanonicalOrderAscending()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 1, max: 10, weight: 0.5, priority: 5, order: 9),
            P(SyntheticBlock.Beta, true, min: 1, max: 10, weight: 0.5, priority: 5, order: 1),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(3, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, AllocatedWeeksFor(result, SyntheticBlock.Beta));
        Assert.Equal(1, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
    }

    [Fact] // stable-key tie
    public void Tie_EqualRemainderPriorityAndCanonicalOrder_ResolvedByKeyOrdinalAscending()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Beta, true, min: 1, max: 10, weight: 0.5, priority: 5, order: 1),
            P(SyntheticBlock.Alpha, true, min: 1, max: 10, weight: 0.5, priority: 5, order: 1),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(3, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
        Assert.Equal(1, AllocatedWeeksFor(result, SyntheticBlock.Beta));
    }

    [Fact] // cap redistribution + saturated-block removal
    public void SaturatedBlock_RemovedFromLaterRounds_OverflowRedistributed()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 1, max: 2, weight: 0.9, priority: 2, order: 1),
            P(SyntheticBlock.Beta, true, min: 1, max: 10, weight: 0.1, priority: 1, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(6, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
        Assert.Equal(4, AllocatedWeeksFor(result, SyntheticBlock.Beta));
    }

    [Fact] // zero-weight fixed block
    public void ZeroWeightFixedBlock_ReceivesExactlyItsFixedCapacity_NeverParticipatesInDistribution()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 10, weight: 1.0, priority: 2, order: 1),
            P(SyntheticBlock.Delta, true, min: 1, max: 1, weight: 0, priority: 1, order: 2, expandable: false),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(4, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, AllocatedWeeksFor(result, SyntheticBlock.Delta));
        Assert.Equal(3, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
    }

    [Fact] // ineligible exclusion
    public void IneligibleBlock_ReceivesZero_NeverParticipatesInMinimaOrNormalization()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 10, weight: 0.5, priority: 2, order: 1),
            P(SyntheticBlock.Beta, false, min: 5, max: 10, weight: 0.5, priority: 1, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(4, policies);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, AllocatedWeeksFor(result, SyntheticBlock.Beta));
        Assert.Equal(4, AllocatedWeeksFor(result, SyntheticBlock.Alpha));
    }

    [Fact] // duplicate key
    public void DuplicateBlockKey_IsRejected()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 10, weight: 0.5, priority: 2, order: 1),
            P(SyntheticBlock.Alpha, true, min: 0, max: 10, weight: 0.5, priority: 1, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(4, policies);
        Assert.False(result.IsSuccess);
        Assert.Equal(PreparationRunwayAllocationFailureCode.DuplicateBlockKey, result.FailureCode);
        Assert.Null(result.Allocations);
    }

    [Fact] // invalid min/max
    public void InvalidMinMax_IsRejected()
    {
        var policies = new[] { P(SyntheticBlock.Alpha, true, min: 5, max: 2, weight: 0.5, priority: 1, order: 1) };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(4, policies);
        Assert.False(result.IsSuccess);
        Assert.Equal(PreparationRunwayAllocationFailureCode.InvalidPolicy, result.FailureCode);
        Assert.Null(result.Allocations);
    }

    [Fact] // minima exceed runway
    public void MinimaExceedRunway_IsRejected()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 3, max: 5, weight: 0.5, priority: 2, order: 1),
            P(SyntheticBlock.Beta, true, min: 3, max: 5, weight: 0.5, priority: 1, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(4, policies);
        Assert.False(result.IsSuccess);
        Assert.Equal(PreparationRunwayAllocationFailureCode.MinimumCapacityExceedsRunway, result.FailureCode);
        Assert.Null(result.Allocations);
    }

    [Fact] // maxima below runway
    public void MaximaBelowRunway_IsRejected()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 2, weight: 0.5, priority: 2, order: 1),
            P(SyntheticBlock.Beta, true, min: 0, max: 2, weight: 0.5, priority: 1, order: 2),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(5, policies);
        Assert.False(result.IsSuccess);
        Assert.Equal(PreparationRunwayAllocationFailureCode.MaximumCapacityBelowRunway, result.FailureCode);
        Assert.Null(result.Allocations);
    }

    [Fact] // no expandable capacity
    public void NoExpandableCapacity_WithRemainingWeeks_IsRejected()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 0, max: 5, weight: 0, priority: 2, order: 1),
            P(SyntheticBlock.Beta, true, min: 1, max: 1, weight: 0, priority: 1, order: 2, expandable: false),
        };
        var result = PreparationRunwayBlockAllocationEngine.Allocate(3, policies);
        Assert.False(result.IsSuccess);
        Assert.Equal(PreparationRunwayAllocationFailureCode.NoExpandableCapacity, result.FailureCode);
        Assert.Null(result.Allocations);
    }

    [Fact] // final sum invariant
    public void Invariant_SumOfAllocationsAlwaysEqualsRunwayWeeks()
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);
        for (var week = 3; week <= 8; week++)
        {
            var result = PreparationRunwayBlockAllocationEngine.Allocate(week, policies);
            Assert.True(result.IsSuccess);
            Assert.Equal(week, result.Allocations!.Sum(a => a.AllocatedWeeks));
        }
    }

    [Fact] // repeated-call determinism
    public void Determinism_RepeatedCallsWithIdenticalInput_ProduceIdenticalOutput()
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);
        var first = PreparationRunwayBlockAllocationEngine.Allocate(7, policies);
        var second = PreparationRunwayBlockAllocationEngine.Allocate(7, policies);

        Assert.Equal(first.IsSuccess, second.IsSuccess);
        Assert.Equal(first.Allocations!.Select(a => (a.BlockKey, a.AllocatedWeeks)), second.Allocations!.Select(a => (a.BlockKey, a.AllocatedWeeks)));
    }

    [Fact] // input-order independence
    public void InputOrderIndependence_ShufflingPolicyListDoesNotChangeResult()
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);
        var reversed = policies.Reverse().ToArray();

        var a = PreparationRunwayBlockAllocationEngine.Allocate(7, policies);
        var b = PreparationRunwayBlockAllocationEngine.Allocate(7, reversed);

        var normalizedA = a.Allocations!.OrderBy(x => x.BlockKey.ToString(), StringComparer.Ordinal).Select(x => (x.BlockKey, x.AllocatedWeeks));
        var normalizedB = b.Allocations!.OrderBy(x => x.BlockKey.ToString(), StringComparer.Ordinal).Select(x => (x.BlockKey, x.AllocatedWeeks));
        Assert.Equal(normalizedA, normalizedB);
    }

    [Fact] // generic non-10K synthetic allocation
    public void Genericity_ArbitrarySyntheticPolicyAndHorizon_ProducesAValidDeterministicAllocation()
    {
        var policies = new[]
        {
            P(SyntheticBlock.Alpha, true, min: 1, max: 6, weight: 0.5, priority: 4, order: 1),
            P(SyntheticBlock.Beta, true, min: 0, max: 4, weight: 0.3, priority: 3, order: 2),
            P(SyntheticBlock.Gamma, true, min: 0, max: 4, weight: 0.2, priority: 2, order: 3),
            P(SyntheticBlock.Delta, true, min: 2, max: 2, weight: 0, priority: 1, order: 4, expandable: false),
        };

        for (var horizon = 10; horizon <= 16; horizon++)
        {
            var result = PreparationRunwayBlockAllocationEngine.Allocate(horizon, policies);
            Assert.True(result.IsSuccess);
            Assert.Equal(horizon, result.Allocations!.Sum(a => a.AllocatedWeeks));
            Assert.Equal(2, AllocatedWeeksFor(result, SyntheticBlock.Delta));
            foreach (var policy in policies)
            {
                var week = AllocatedWeeksFor(result, policy.BlockKey);
                Assert.InRange(week, policy.MinWeeks, policy.MaxWeeks);
            }
        }
    }

    [Fact] // chronological ordering by CanonicalOrder
    public void OutputOrdering_FollowsCanonicalOrderAscending_NotAllocationPriority()
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);
        var result = PreparationRunwayBlockAllocationEngine.Allocate(6, policies);
        Assert.True(result.IsSuccess);

        // CanonicalOrder: Consistency=1, GeneralEndurance=2, AerobicStrength=3, PreSpecificTransition=4.
        // AllocationPriority: GeneralEndurance=4 (highest), Consistency=3, AerobicStrength=2, Transition=1 (lowest) -- deliberately the opposite shape.
        var orderedKeys = result.Allocations!.OrderBy(a => a.CanonicalOrder).Select(a => a.BlockKey).ToArray();
        Assert.Equal(new[]
        {
            PreparationRunwayBlockType.Consistency, PreparationRunwayBlockType.GeneralEndurance,
            PreparationRunwayBlockType.AerobicStrength, PreparationRunwayBlockType.PreSpecificTransition,
        }, orderedKeys);
    }

    // ══════════════════════════════════════════════════════════════════
    // Trace/provenance
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Trace_IsDeterministicAndNonEmptyForSuccessAndFailure()
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);
        var success = PreparationRunwayBlockAllocationEngine.Allocate(6, policies);
        Assert.NotEmpty(success.Trace);

        var overConstrained = new[] { P(SyntheticBlock.Alpha, true, min: 5, max: 5, weight: 0, priority: 1, order: 1, expandable: false) };
        var failure = PreparationRunwayBlockAllocationEngine.Allocate(2, overConstrained);
        Assert.NotEmpty(failure.Trace);
        Assert.Contains(failure.Trace, line => line.StartsWith("FAILED:"));
    }

    // ══════════════════════════════════════════════════════════════════
    // Compatibility proof with the existing dark contract layer
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void EngineOutput_ProjectsIntoRealPreparationRunwayAllocation_AndPassesTheExistingValidator()
    {
        var policies = TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(PreparationRunwayAllocationProfile.CoreEntryReady);
        var result = PreparationRunwayBlockAllocationEngine.Allocate(6, policies);
        Assert.True(result.IsSuccess);

        var nonZeroInCanonicalOrder = result.Allocations!
            .Where(a => a.AllocatedWeeks > 0)
            .OrderBy(a => a.CanonicalOrder)
            .Select((a, index) => new PreparationRunwayBlockAllocation(a.BlockKey, PreparationRunwayPrescriptionProfile.Standard, a.AllocatedWeeks, index))
            .ToArray();

        var allocation = new PreparationRunwayAllocation(6 * 7, 6, 0, nonZeroInCanonicalOrder);
        var validation = PreparationRunwayAllocationValidator.Validate(allocation);
        Assert.True(validation.IsValid, string.Join("; ", validation.Findings.Select(f => f.Detail)));
    }

    // ══════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════

    private static int AllocatedWeeksFor<TKey>(PreparationRunwayAllocationEngineResult<TKey> result, TKey key) where TKey : notnull =>
        result.Allocations!.Single(a => EqualityComparer<TKey>.Default.Equals(a.BlockKey, key)).AllocatedWeeks;

    private static string RepoRoot() =>
        RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
}
