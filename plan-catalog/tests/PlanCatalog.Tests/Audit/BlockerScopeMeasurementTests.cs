using PlanCatalog.Core.Audit;
using Xunit;

namespace PlanCatalog.Tests.Audit;

/// <summary>
/// Milestone H: decision-level vs artifact-level blocker measurement. Tests 40-42 of the Part 2
/// required-tests list — see artifacts/audits/deterministic-graph-part2-migration.md.
/// </summary>
public sealed class BlockerScopeMeasurementTests
{
    [Fact]
    public void DecisionLevelAndArtifactLevelCounts_AreSeparate()
    {
        // Test 40: an artifact with more than one blocking field must contribute 1 to the artifact-level
        // count but more than 1 to the decision-level count — the two numbers are never equal for such an
        // artifact, proving they measure genuinely different things.
        var decisionCount = BlockerScopeMeasurement.TotalCatalogPlaceholderDecisionCount();
        var artifactCount = BlockerScopeMeasurement.TotalCatalogBlockingArtifactCount();

        Assert.True(decisionCount > 0);
        Assert.True(artifactCount > 0);
        Assert.True(decisionCount >= artifactCount, "Decision-level count can never be smaller than artifact-level count (each artifact contributes at least 1 decision).");
        Assert.NotEqual(decisionCount, artifactCount);
    }

    [Fact]
    public void CandidateClosureCounts_AreCalculatedFromCandidateDependenciesOnly()
    {
        // Test 41: scoping to an empty/unrelated identity set must yield zero, proving the scope
        // parameter — not the total-catalog list — determines the result.
        var emptyScope = Array.Empty<BlockerScopeMeasurement.ArtifactIdentity>();

        Assert.Equal(0, BlockerScopeMeasurement.ScopedDecisionCount(emptyScope));
        Assert.Equal(0, BlockerScopeMeasurement.ScopedArtifactCount(emptyScope));

        var knownBlockingIdentity = new BlockerScopeMeasurement.ArtifactIdentity("WORKOUT_DEFINITION", "GOAL_PACE_TEN_K", 1);
        var narrowScope = new[] { knownBlockingIdentity };

        var scopedDecisions = BlockerScopeMeasurement.ScopedDecisionCount(narrowScope);
        var scopedArtifacts = BlockerScopeMeasurement.ScopedArtifactCount(narrowScope);

        Assert.True(scopedArtifacts <= 1);
        Assert.True(scopedDecisions < BlockerScopeMeasurement.TotalCatalogPlaceholderDecisionCount());
    }

    [Fact]
    public void HistoricalOnlyDecisions_DoNotEnterCandidateClosureCounts()
    {
        // Test 42: an identity present in the eligible-release-union scope must never be reported as
        // historical-only, and vice versa — the two are a strict partition of the total.
        var knownBlockingIdentity = new BlockerScopeMeasurement.ArtifactIdentity("WORKOUT_DEFINITION", "GOAL_PACE_TEN_K", 1);
        var scopeIncludingIt = new[] { knownBlockingIdentity };

        var historicalOnlyWhenIncluded = BlockerScopeMeasurement.HistoricalOnlyDecisionCount(scopeIncludingIt);
        var totalDecisions = BlockerScopeMeasurement.TotalCatalogPlaceholderDecisionCount();
        var decisionsForIdentity = BlockerScopeMeasurement.ScopedDecisionCount(scopeIncludingIt);

        Assert.Equal(totalDecisions - decisionsForIdentity, historicalOnlyWhenIncluded);

        var historicalOnlyWhenExcluded = BlockerScopeMeasurement.HistoricalOnlyDecisionCount([]);
        Assert.Equal(totalDecisions, historicalOnlyWhenExcluded);
    }
}
