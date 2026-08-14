using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunway;

/// <summary>
/// Backend Integration Phase 4G.6A.2 — a LOCAL, TEST-ONLY analysis
/// simulation of the future generic constrained-proportional Preparation
/// Runway allocator's intended high-level mechanics (Step 6 of the phase's
/// own prompt: mandatory minima -> remaining capacity -> normalized weights
/// over eligible expandable blocks -> ideal proportional shares -> floor ->
/// largest-remainder distribution -> maxima enforcement -> deterministic
/// overflow redistribution). This class is entirely self-contained inside
/// the test project: it is not referenced by, and does not reference, any
/// production runtime or DI composition. It exists solely to produce
/// verifiable, deterministic evidence for the decision document
/// (PHASE4G_6A_2_TEN_K_RUNWAY_COEFFICIENT_AND_ELIGIBILITY_DECISION.md) --
/// it is not itself a production allocator, and this phase does not wire it
/// into any live or dark orchestrator.
/// </summary>
public sealed class Phase4G6A2CoefficientSimulationTests
{
    private sealed record BlockSpec(string Key, int MinimumWeeks, int MaximumWeeks, double Weight, int AllocationPriority, int CanonicalOrder, bool Expandable);

    private sealed record AllocationResult(int RunwayWeeks, IReadOnlyDictionary<string, int> Allocated, IReadOnlyList<string> Ties, bool CapRedistributionOccurred);

    /// <summary>
    /// The candidate allocator's 8-step mechanics, exactly as described in
    /// this phase's own prompt. Fixed (non-expandable) blocks receive their
    /// mandatory minimum (== their maximum, by construction) and never
    /// participate in proportional distribution. Eligible expandable blocks
    /// receive a baseline minimum (1, if eligible -- matching every
    /// candidate target-matrix row's own "present blocks always show >= 1"
    /// shape), then share the remaining capacity proportionally by
    /// normalized weight, floored, with the largest-fractional-remainder
    /// block(s) receiving the leftover integer(s) one at a time (tie-break:
    /// AllocationPriority descending, then CanonicalOrder ascending, then
    /// key ordinal). Any block whose total would exceed its own maximum is
    /// capped, and the overflow is redistributed to the remaining
    /// uncapped eligible expandable block(s) using the same tie-break.
    /// </summary>
    private static AllocationResult Allocate(int runwayWeeks, IReadOnlyList<BlockSpec> blocks)
    {
        var ties = new List<string>();
        var allocated = blocks.ToDictionary(b => b.Key, b => 0);

        // Step 1: mandatory minima (fixed blocks get Minimum == Maximum; eligible expandable blocks get a baseline of 1).
        foreach (var block in blocks)
        {
            allocated[block.Key] = block.Expandable ? Math.Min(1, block.MaximumWeeks) : block.MinimumWeeks;
        }

        // Step 2: remaining capacity.
        var remaining = runwayWeeks - allocated.Values.Sum();
        if (remaining < 0)
        {
            throw new InvalidOperationException($"RunwayWeeks={runwayWeeks} is insufficient for the supplied mandatory minima (sum={allocated.Values.Sum()}).");
        }

        var expandable = blocks.Where(b => b.Expandable).ToList();
        var capRedistributed = false;

        while (remaining > 0 && expandable.Any(b => allocated[b.Key] < b.MaximumWeeks))
        {
            var open = expandable.Where(b => allocated[b.Key] < b.MaximumWeeks).ToList();

            // Step 3: normalize weights over currently-open eligible expandable blocks.
            var totalWeight = open.Sum(b => b.Weight);

            // Step 4: ideal proportional shares of the currently-remaining capacity.
            var ideal = open.ToDictionary(b => b.Key, b => remaining * (b.Weight / totalWeight));

            // Step 5: floor.
            var floorShare = ideal.ToDictionary(kv => kv.Key, kv => (int)Math.Floor(kv.Value));
            var flooredTotal = floorShare.Values.Sum();
            var leftover = remaining - flooredTotal;

            // Step 6: distribute largest remainders, one at a time, honoring maxima as we go.
            var ordered = open
                .OrderByDescending(b => ideal[b.Key] - floorShare[b.Key])
                .ThenByDescending(b => b.AllocationPriority)
                .ThenBy(b => b.CanonicalOrder)
                .ThenBy(b => b.Key, StringComparer.Ordinal)
                .ToList();

            var remainderRecipients = ordered.Take(leftover).ToList();

            if (remainderRecipients.Count > 0)
            {
                var boundaryRemainder = ideal[remainderRecipients[^1].Key] - floorShare[remainderRecipients[^1].Key];
                if (ordered.Skip(leftover).Any(b => Math.Abs((ideal[b.Key] - floorShare[b.Key]) - boundaryRemainder) < 1e-9))
                {
                    ties.Add($"RunwayWeeks={runwayWeeks}: fractional-remainder tie at the leftover boundary among [{string.Join(",", ordered.Select(b => b.Key))}] resolved by AllocationPriority/CanonicalOrder/key.");
                }
            }

            foreach (var block in remainderRecipients)
            {
                floorShare[block.Key] += 1;
            }

            // Apply floor shares (before overflow distribution) additively to the running allocation, respecting maxima, then compute overflow.
            foreach (var block in open)
            {
                var proposed = allocated[block.Key] + floorShare[block.Key];
                var applied = Math.Min(proposed, block.MaximumWeeks);
                var overflow = proposed - applied;
                allocated[block.Key] = applied;
                if (overflow > 0)
                {
                    capRedistributed = true;
                    // Step 7/8: enforce maxima and redistribute overflow deterministically
                    // to the remaining uncapped open block(s), same tie-break.
                    var recipients = open.Where(b => b.Key != block.Key && allocated[b.Key] < b.MaximumWeeks)
                        .OrderByDescending(b => b.AllocationPriority).ThenBy(b => b.CanonicalOrder).ThenBy(b => b.Key, StringComparer.Ordinal)
                        .ToList();
                    var remainingOverflow = overflow;
                    while (remainingOverflow > 0 && recipients.Count > 0)
                    {
                        foreach (var recipient in recipients.ToList())
                        {
                            if (remainingOverflow == 0)
                            {
                                break;
                            }

                            if (allocated[recipient.Key] < recipient.MaximumWeeks)
                            {
                                allocated[recipient.Key]++;
                                remainingOverflow--;
                            }

                            if (allocated[recipient.Key] >= recipient.MaximumWeeks)
                            {
                                recipients.Remove(recipient);
                            }
                        }

                        if (recipients.Count == 0 && remainingOverflow > 0)
                        {
                            throw new InvalidOperationException($"RunwayWeeks={runwayWeeks}: overflow of {remainingOverflow} could not be redistributed -- combined maxima insufficient.");
                        }
                    }
                }
            }

            remaining = runwayWeeks - allocated.Values.Sum();
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException($"RunwayWeeks={runwayWeeks}: {remaining} week(s) could not be allocated -- combined maxima insufficient for this horizon.");
        }

        return new AllocationResult(runwayWeeks, allocated, ties, capRedistributed);
    }

    private static readonly BlockSpec Transition = new("PRE_SPECIFIC_TRANSITION", 1, 1, 0d, 1, 4, Expandable: false);

    // ── CONSISTENCY_NEEDED profile: Consistency + GeneralEndurance expandable, AerobicStrength ineligible (excluded from the block list entirely) ──

    public static IEnumerable<object[]> ConsistencyNeededCoefficientCandidates()
    {
        yield return new object[] { "A", 0.35, 0.65 };
        yield return new object[] { "B", 0.40, 0.60 };
        yield return new object[] { "C", 0.45, 0.55 };
    }

    [Theory]
    [MemberData(nameof(ConsistencyNeededCoefficientCandidates))]
    public void ConsistencyNeeded_AllThreeCandidates_ReproduceTheTargetMatrixAfterCapRedistribution(string label, double consistencyWeight, double generalEnduranceWeight)
    {
        var expected = new Dictionary<int, (int Consistency, int GeneralEndurance)>
        {
            [3] = (1, 1), [4] = (1, 2), [5] = (2, 2), [6] = (2, 3), [7] = (2, 4), [8] = (2, 5),
        };

        foreach (var (runwayWeeks, target) in expected)
        {
            var blocks = new[]
            {
                new BlockSpec("CONSISTENCY", 0, 2, consistencyWeight, 3, 1, Expandable: true),
                new BlockSpec("GENERAL_ENDURANCE", 1, 5, generalEnduranceWeight, 4, 2, Expandable: true),
                Transition,
            };

            var result = Allocate(runwayWeeks, blocks);

            Assert.Equal(runwayWeeks, result.Allocated.Values.Sum());
            Assert.Equal(target.Consistency, result.Allocated["CONSISTENCY"]);
            Assert.Equal(target.GeneralEndurance, result.Allocated["GENERAL_ENDURANCE"]);
            Assert.Equal(1, result.Allocated["PRE_SPECIFIC_TRANSITION"]);
            Assert.True(result.Allocated["CONSISTENCY"] <= 2, $"[{label}] Consistency exceeded its MaxWeeks=2 at RunwayWeeks={runwayWeeks}.");
        }
    }

    [Fact]
    public void ConsistencyNeeded_SecondConsistencyWeek_AppearsExactlyAtRunwayFive_ForAllThreeCandidates()
    {
        foreach (var (label, consistencyWeight, generalEnduranceWeight) in new[] { ("A", 0.35, 0.65), ("B", 0.40, 0.60), ("C", 0.45, 0.55) })
        {
            var blocks = new[]
            {
                new BlockSpec("CONSISTENCY", 0, 2, consistencyWeight, 3, 1, Expandable: true),
                new BlockSpec("GENERAL_ENDURANCE", 1, 5, generalEnduranceWeight, 4, 2, Expandable: true),
                Transition,
            };

            Assert.Equal(1, Allocate(4, blocks).Allocated["CONSISTENCY"]);
            Assert.Equal(2, Allocate(5, blocks).Allocated["CONSISTENCY"]);
        }
    }

    // ── CORE_ENTRY_READY profile: GeneralEndurance + AerobicStrength expandable ──
    // Illustrative/analysis only -- AerobicStrength remains catalog-UNSUPPORTED
    // (see the decision document); this simulation exists to determine
    // whether the CANDIDATE target matrix is even mechanically achievable by
    // the described allocator algorithm, independent of catalog capacity.

    public static IEnumerable<object[]> CoreEntryReadyCoefficientCandidates()
    {
        yield return new object[] { "A", 0.70, 0.30 };
        yield return new object[] { "B", 0.65, 0.35 };
        yield return new object[] { "C", 0.60, 0.40 };
    }

    [Theory]
    [MemberData(nameof(CoreEntryReadyCoefficientCandidates))]
    public void CoreEntryReady_NoTestedCandidateReproducesTheLiteralTargetMatrix_SecondAerobicStrengthWeekArrivesTooEarly(string label, double generalEnduranceWeight, double aerobicStrengthWeight)
    {
        // The candidate target matrix specifies AerobicStrength staying at
        // its minimum (1) through RunwayWeeks=5 and only reaching 2 at
        // RunwayWeeks=6 ("AerobicStrengthSecondWeekThreshold = RunwayWeeks >= 6").
        // This test proves that outcome does NOT emerge from the described
        // proportional/floor/largest-remainder mechanism under any of the
        // three candidate coefficient pairs -- AerobicStrength's fractional
        // remainder becomes competitive with GeneralEndurance's at
        // RunwayWeeks=5 already, one horizon earlier than the target matrix
        // specifies, for every tested pair.
        var blocks = new[]
        {
            new BlockSpec("GENERAL_ENDURANCE", 1, 5, generalEnduranceWeight, 4, 2, Expandable: true),
            new BlockSpec("AEROBIC_STRENGTH", 0, 2, aerobicStrengthWeight, 2, 3, Expandable: true),
            Transition,
        };

        var resultAtFive = Allocate(5, blocks);

        // The literal target matrix requires AerobicStrength == 1 at RunwayWeeks=5.
        Assert.True(resultAtFive.Allocated["AEROBIC_STRENGTH"] != 1, $"[Candidate {label}] expected the mismatch this test documents.");
        Assert.Equal(2, resultAtFive.Allocated["AEROBIC_STRENGTH"]);
    }

    [Fact]
    public void CoreEntryReady_TargetMatrix_RequiresAnExplicitThresholdGateNotPureProportionalWeighting()
    {
        // Confirms the negative finding above holds even for a
        // deliberately extreme, far-outside-the-candidate-range weight pair
        // heavily favoring GeneralEndurance -- proving the target matrix's
        // sharp "flat until week 6, then jump to 2" shape is not a
        // coefficient-tuning problem; it requires a fundamentally different
        // (threshold-gated) allocator rule, which this phase does not design.
        var blocks = new[]
        {
            new BlockSpec("GENERAL_ENDURANCE", 1, 5, 0.85, 4, 2, Expandable: true),
            new BlockSpec("AEROBIC_STRENGTH", 0, 2, 0.15, 2, 3, Expandable: true),
            Transition,
        };

        var atFive = Allocate(5, blocks);
        var atSix = Allocate(6, blocks);

        // Even at this extreme ratio, AerobicStrength still grows before
        // week 6, or GeneralEndurance itself no longer matches the target's
        // own week-6 value (3) -- either way, the literal matrix is not
        // reproduced by constant-weight proportional allocation.
        var atFiveMatches = atFive.Allocated["GENERAL_ENDURANCE"] == 3 && atFive.Allocated["AEROBIC_STRENGTH"] == 1;
        var atSixMatches = atSix.Allocated["GENERAL_ENDURANCE"] == 3 && atSix.Allocated["AEROBIC_STRENGTH"] == 2;
        Assert.False(atFiveMatches && atSixMatches);
    }

    private readonly ITestOutputHelper _output;

    public Phase4G6A2CoefficientSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Not an assertion -- a documentation/evidence-capture pass. Prints the
    /// full RunwayWeeks=3..8 allocation table for every candidate
    /// coefficient pair in both profiles, for direct transcription into
    /// PHASE4G_6A_2_TEN_K_RUNWAY_COEFFICIENT_AND_ELIGIBILITY_DECISION.md.
    /// </summary>
    [Fact]
    public void EvidenceCapture_FullMatrixForBothProfilesAcrossAllCandidates()
    {
        _output.WriteLine("=== CONSISTENCY_NEEDED (Consistency, GeneralEndurance, Transition) ===");
        foreach (var (label, consistencyWeight, generalEnduranceWeight) in new[] { ("A", 0.35, 0.65), ("B", 0.40, 0.60), ("C", 0.45, 0.55) })
        {
            var blocks = new[]
            {
                new BlockSpec("CONSISTENCY", 0, 2, consistencyWeight, 3, 1, Expandable: true),
                new BlockSpec("GENERAL_ENDURANCE", 1, 5, generalEnduranceWeight, 4, 2, Expandable: true),
                Transition,
            };

            for (var week = 3; week <= 8; week++)
            {
                var r = Allocate(week, blocks);
                _output.WriteLine($"Candidate {label} ({consistencyWeight:0.00}/{generalEnduranceWeight:0.00}) RunwayWeeks={week}: CONSISTENCY={r.Allocated["CONSISTENCY"]}, GENERAL_ENDURANCE={r.Allocated["GENERAL_ENDURANCE"]}, PRE_SPECIFIC_TRANSITION={r.Allocated["PRE_SPECIFIC_TRANSITION"]}, capRedistributed={r.CapRedistributionOccurred}");
            }
        }

        _output.WriteLine("");
        _output.WriteLine("=== CORE_ENTRY_READY (GeneralEndurance, AerobicStrength, Transition) ===");
        foreach (var (label, generalEnduranceWeight, aerobicStrengthWeight) in new[] { ("A", 0.70, 0.30), ("B", 0.65, 0.35), ("C", 0.60, 0.40) })
        {
            var blocks = new[]
            {
                new BlockSpec("GENERAL_ENDURANCE", 1, 5, generalEnduranceWeight, 4, 2, Expandable: true),
                new BlockSpec("AEROBIC_STRENGTH", 0, 2, aerobicStrengthWeight, 2, 3, Expandable: true),
                Transition,
            };

            for (var week = 3; week <= 8; week++)
            {
                var r = Allocate(week, blocks);
                _output.WriteLine($"Candidate {label} ({generalEnduranceWeight:0.00}/{aerobicStrengthWeight:0.00}) RunwayWeeks={week}: GENERAL_ENDURANCE={r.Allocated["GENERAL_ENDURANCE"]}, AEROBIC_STRENGTH={r.Allocated["AEROBIC_STRENGTH"]}, PRE_SPECIFIC_TRANSITION={r.Allocated["PRE_SPECIFIC_TRANSITION"]}, capRedistributed={r.CapRedistributionOccurred}");
            }
        }
    }
}
