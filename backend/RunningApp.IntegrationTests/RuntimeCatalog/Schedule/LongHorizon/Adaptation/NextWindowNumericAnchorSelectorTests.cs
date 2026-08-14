using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4B.2 -- pure logic tests for
/// <see cref="NextWindowNumericAnchorSelector"/> (Rev4 §7 formula). No DB:
/// this component takes only in-memory <c>ValidatedSustainableLoad</c>
/// values and a decision/count, exactly like Phase 4M.1's own pure-policy
/// tests.
/// </summary>
public sealed class NextWindowNumericAnchorSelectorTests
{
    private static ValidatedSustainableLoad Load(double weeklyKm, double longRunKm, LongHorizonEvidenceSource source = LongHorizonEvidenceSource.CompletedTrainingHistory) => new()
    {
        WeeklyVolumeKm = weeklyKm,
        LongRunKm = longRunKm,
        EvidenceWindowStartWeek = 1,
        EvidenceWindowEndWeek = 1,
        CompletedEvidenceWeekNumbers = [1],
        ExcludedRecoveryWeekNumbers = [],
        WeeklyLoadSource = LongHorizonEvidenceAuthorityRecord.Create(source, LongHorizonEvidenceAuthorityStatus.Authoritative),
        LongRunSource = LongHorizonEvidenceAuthorityRecord.Create(source, LongHorizonEvidenceAuthorityStatus.Authoritative),
        RoundingPolicy = "nearest_tenth",
        LongRunCapPolicy = "share_of_weekly",
        ValidationStatus = LongHorizonValidationStatus.Valid,
    };

    [Fact] // Q.1
    public void ProgressAsPlanned_SelectsCurrentWindowEvidence_Unmodified()
    {
        var current = Load(30, 10);
        var prior = Load(20, 7);
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.ProgressAsPlanned, current, prior, effectiveCompletedCount: 4);
        Assert.Same(current, result);
    }

    [Fact] // Q.2
    public void Maintain_SelectsExactlyPriorValidatedCheckpointLoad()
    {
        var current = Load(30, 10);
        var prior = Load(20, 7);
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Maintain, current, prior, effectiveCompletedCount: 2);
        Assert.Same(prior, result);
    }

    [Fact] // Q.3 -- current < prior -> current wins
    public void Reduce_WithEvidence_CurrentLowerThanPrior_SelectsCurrent()
    {
        var current = Load(15, 5);
        var prior = Load(25, 8);
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Reduce, current, prior, effectiveCompletedCount: 1);
        Assert.Same(current, result);
    }

    [Fact] // Q.4 -- equal -> current wins (current <= prior)
    public void Reduce_WithEvidence_CurrentEqualsPrior_SelectsCurrent()
    {
        var current = Load(20, 7);
        var prior = Load(20, 7);
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Reduce, current, prior, effectiveCompletedCount: 1);
        Assert.Same(current, result);
    }

    [Fact] // Q.5 -- current > prior -> capped at prior
    public void Reduce_WithEvidence_CurrentHigherThanPrior_SelectsPrior()
    {
        var current = Load(30, 10);
        var prior = Load(20, 7);
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Reduce, current, prior, effectiveCompletedCount: 1);
        Assert.Same(prior, result);
    }

    [Fact] // Q.6 -- zero completion -> prior fallback regardless of current
    public void Reduce_ZeroCompletion_FallsBackToPrior()
    {
        var current = Load(5, 2); // even if some stray value exists, EffectiveCompletedCount gates it
        var prior = Load(20, 7);
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Reduce, current, prior, effectiveCompletedCount: 0);
        Assert.Same(prior, result);
    }

    [Fact] // Q.6 variant -- zero completion, current genuinely absent (Unavailable evidence)
    public void Reduce_ZeroCompletion_NullCurrent_FallsBackToPrior()
    {
        var prior = Load(20, 7);
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Reduce, currentWindowValidatedLoad: null, prior, effectiveCompletedCount: 0);
        Assert.Same(prior, result);
    }

    [Fact] // Section I -- zero-completion edge case must not throw, and equals Maintain numerically
    public void Reduce_ZeroCompletion_NumericallyEqualsMaintain()
    {
        var current = Load(30, 10);
        var prior = Load(20, 7);
        var reduce = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Reduce, current, prior, effectiveCompletedCount: 0);
        var maintain = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Maintain, current, prior, effectiveCompletedCount: 0);
        Assert.Equal(maintain, reduce);
    }

    [Fact] // Genuine ambiguity closed during implementation: no prior AND no current -> null (existing Block semantics unaffected)
    public void Reduce_ZeroCompletion_BothAbsent_ReturnsNull()
    {
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Reduce, currentWindowValidatedLoad: null, priorValidatedCheckpointLoad: null, effectiveCompletedCount: 0);
        Assert.Null(result);
    }

    [Fact] // First-ever-checkpoint fallback: Maintain with no prior degrades to current evidence rather than null
    public void Maintain_NoPriorAnchorYet_FallsBackToCurrentWindowEvidence()
    {
        var current = Load(18, 6);
        var result = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Maintain, current, priorValidatedCheckpointLoad: null, effectiveCompletedCount: 2);
        Assert.Same(current, result);
    }

    [Fact] // Q.7 -- no percentage/constant anywhere: selector output is always exactly one of the two supplied inputs (or null), never a derived value
    public void EverySelectedResult_IsReferenceIdenticalToOneOfTheTwoInputs_NeverADerivedValue()
    {
        var current = Load(27, 9);
        var prior = Load(22, 8);
        foreach (var decision in new[] { NextWindowLoadDecision.ProgressAsPlanned, NextWindowLoadDecision.Maintain, NextWindowLoadDecision.Reduce })
        {
            foreach (var completedCount in new[] { 0, 1, 2, 3, 4 })
            {
                var result = NextWindowNumericAnchorSelector.Select(decision, current, prior, completedCount);
                Assert.True(ReferenceEquals(result, current) || ReferenceEquals(result, prior),
                    $"decision={decision} completedCount={completedCount} produced a value that is neither input verbatim.");
            }
        }
    }

    // ── Severity ordering (Q.8) ─────────────────────────────────────────

    [Fact]
    public void SeverityOrdering_ReduceNeverExceedsMaintain_AcrossRandomizedInputs()
    {
        var random = new Random(42);
        for (var i = 0; i < 200; i++)
        {
            var currentKm = random.NextDouble() * 60;
            var priorKm = random.NextDouble() * 60;
            var completed = random.Next(0, 5);
            var current = Load(currentKm, currentKm / 3);
            var prior = Load(priorKm, priorKm / 3);

            var reduceAnchor = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Reduce, current, prior, completed);
            var maintainAnchor = NextWindowNumericAnchorSelector.Select(NextWindowLoadDecision.Maintain, current, prior, completed);

            Assert.True((reduceAnchor?.WeeklyVolumeKm ?? 0) <= (maintainAnchor?.WeeklyVolumeKm ?? double.MaxValue),
                $"Reduce ({reduceAnchor?.WeeklyVolumeKm}) exceeded Maintain ({maintainAnchor?.WeeklyVolumeKm}) for current={currentKm} prior={priorKm} completed={completed}.");
        }
    }

    // ── Safety orthogonality (Q.31-33) -- the selector never even accepts a safety parameter ──

    [Fact]
    public void SelectorSignature_HasNoSafetyReviewRequiredParameter_SafetyCannotInfluenceAnchor()
    {
        var method = typeof(NextWindowNumericAnchorSelector).GetMethod("Select")!;
        var parameterNames = method.GetParameters().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(parameterNames, n => n!.Contains("Safety", StringComparison.OrdinalIgnoreCase));
    }
}
