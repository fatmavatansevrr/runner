using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.3 — production-level tests for <see cref="LongHorizonCompositionResolver"/>.
/// Pure, deterministic, no clock/catalog/database dependency: every case
/// uses <see cref="RaceHorizonPolicy.Decide(DateOnly, DateOnly)"/> anchored
/// at a fixed date (mirroring <see cref="RaceHorizonPolicy.Classify(int)"/>'s
/// own technique), so the exact same <c>CoreHorizonDecision</c> production
/// code would compute for a real request is fed into the resolver under
/// test -- never a hand-constructed shortcut decision.
/// </summary>
public sealed class LongHorizonCompositionResolverTests
{
    private static readonly DateOnly Anchor = new(2000, 1, 1);

    private static LongHorizonCompositionDecision Resolve(int availableFullWeeks, ReadinessProfile? profile = ReadinessProfile.ConsistencyNeeded)
    {
        var raceDate = Anchor.AddDays(availableFullWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(Anchor, raceDate);
        return LongHorizonCompositionResolver.Resolve(coreHorizon, profile);
    }

    // ── Below-8 / 8-14 / 15-20 regression (unchanged existing behavior) ────

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void CoreOnlyRange_ReturnsCoreOnlyPath(int weeks)
    {
        var decision = Resolve(weeks);
        Assert.Equal(LongHorizonPath.CoreOnly, decision.HorizonPath);
        Assert.Equal(weeks, decision.CoreWeeks);
        Assert.Null(decision.GeneralEnduranceWeeks);
        Assert.Null(decision.PreparationRunwayWeeks);
        Assert.Equal(GeneralEnduranceDurationClassification.NotApplicable, decision.GeneralEnduranceClassification);
    }

    [Fact]
    public void BelowMinimum_PreservesExistingInsufficientPath()
    {
        var decision = Resolve(7);
        Assert.Equal(LongHorizonPath.InsufficientOrExistingUnsupportedPath, decision.HorizonPath);
        Assert.Null(decision.CoreWeeks);
        Assert.Null(decision.GeneralEnduranceWeeks);
    }

    [Theory]
    [InlineData(15, 3)]
    [InlineData(20, 8)]
    public void PreparationRunwayRange_ReturnsExpectedRunwayCount(int weeks, int expectedRunway)
    {
        var decision = Resolve(weeks);
        Assert.Equal(LongHorizonPath.PreparationRunwayAndCore, decision.HorizonPath);
        Assert.Equal(expectedRunway, decision.PreparationRunwayWeeks);
        Assert.Equal(12, decision.CoreWeeks);
        Assert.Null(decision.GeneralEnduranceWeeks);
        Assert.Equal(GeneralEnduranceDurationClassification.NotApplicable, decision.GeneralEnduranceClassification);
    }

    [Fact]
    public void TwentyWeeks_RemainsEightRunwayTwelveCore_NoLongHorizonGe()
    {
        var decision = Resolve(20);
        Assert.Equal(LongHorizonPath.PreparationRunwayAndCore, decision.HorizonPath);
        Assert.Equal(8, decision.PreparationRunwayWeeks);
        Assert.Equal(12, decision.CoreWeeks);
        Assert.Null(decision.GeneralEnduranceWeeks);
        Assert.Equal(LongHorizonEligibility.NotApplicableBelowLongHorizonBoundary, decision.Eligibility);
    }

    // ── 20 -> 21 boundary ────────────────────────────────────────────────────

    [Fact]
    public void TwentyOneWeeks_BecomesLongHorizon()
    {
        var decision = Resolve(21);
        Assert.Equal(LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore, decision.HorizonPath);
        Assert.Equal(LongHorizonEligibility.SupportedLongHorizon, decision.Eligibility);
    }

    [Fact]
    public void TwentyOneWeeks_Maps_1_8_12()
    {
        var decision = Resolve(21);
        Assert.Equal(1, decision.GeneralEnduranceWeeks);
        Assert.Equal(8, decision.PreparationRunwayWeeks);
        Assert.Equal(12, decision.CoreWeeks);
    }

    [Fact]
    public void TwentyOneWeeks_ClassifiesShortExtension() =>
        Assert.Equal(GeneralEnduranceDurationClassification.ShortExtension, Resolve(21).GeneralEnduranceClassification);

    // ── Fixture matrix ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(22, 2)]
    [InlineData(23, 3)]
    [InlineData(24, 4)]
    [InlineData(28, 8)]
    [InlineData(32, 12)]
    [InlineData(40, 20)]
    [InlineData(48, 28)]
    [InlineData(52, 32)]
    public void FixtureMatrix_MapsExpectedGeneralEndurance(int weeks, int expectedGe)
    {
        var decision = Resolve(weeks);
        Assert.Equal(expectedGe, decision.GeneralEnduranceWeeks);
        Assert.Equal(8, decision.PreparationRunwayWeeks);
        Assert.Equal(12, decision.CoreWeeks);
        Assert.Equal(LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore, decision.HorizonPath);
    }

    [Fact]
    public void TwentyFourWeeks_ClassifiesFullPhase() =>
        Assert.Equal(GeneralEnduranceDurationClassification.FullPhase, Resolve(24).GeneralEnduranceClassification);

    // ── 52 -> 53 boundary / unsupported ─────────────────────────────────────

    [Theory]
    [InlineData(53)]
    [InlineData(60)]
    [InlineData(1000)]
    public void AboveFiftyTwo_IsUnsupported(int weeks)
    {
        var decision = Resolve(weeks);
        Assert.Equal(LongHorizonPath.UnsupportedAboveSupportedWindow, decision.HorizonPath);
        Assert.Equal(LongHorizonEligibility.UnsupportedAboveAnnualWindow, decision.Eligibility);
        Assert.Equal("PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW", decision.ReasonCode);
    }

    [Theory]
    [InlineData(53)]
    [InlineData(60)]
    public void AboveFiftyTwo_EmitsNoAllocationValues(int weeks)
    {
        var decision = Resolve(weeks);
        Assert.Null(decision.GeneralEnduranceWeeks);
        Assert.Null(decision.PreparationRunwayWeeks);
        Assert.Null(decision.CoreWeeks);
    }

    // ── Readiness profile ────────────────────────────────────────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    [InlineData(52)]
    public void BothReadinessProfiles_ReceiveEqualDurations(int weeks)
    {
        var consistencyNeeded = Resolve(weeks, ReadinessProfile.ConsistencyNeeded);
        var coreEntryReady = Resolve(weeks, ReadinessProfile.CoreEntryReady);

        Assert.Equal(consistencyNeeded.GeneralEnduranceWeeks, coreEntryReady.GeneralEnduranceWeeks);
        Assert.Equal(consistencyNeeded.PreparationRunwayWeeks, coreEntryReady.PreparationRunwayWeeks);
        Assert.Equal(consistencyNeeded.CoreWeeks, coreEntryReady.CoreWeeks);
        Assert.Equal(consistencyNeeded.GeneralEnduranceClassification, coreEntryReady.GeneralEnduranceClassification);
    }

    [Fact]
    public void ProfileValueIsPreservedVerbatim()
    {
        Assert.Equal(ReadinessProfile.ConsistencyNeeded, Resolve(24, ReadinessProfile.ConsistencyNeeded).ReadinessProfile);
        Assert.Equal(ReadinessProfile.CoreEntryReady, Resolve(24, ReadinessProfile.CoreEntryReady).ReadinessProfile);
    }

    // ── Sum invariant + monotonicity (loop, not just fixture points) ───────

    [Fact]
    public void SumInvariantHolds_ForEveryValueInRange()
    {
        for (var weeks = 21; weeks <= 52; weeks++)
        {
            var decision = Resolve(weeks);
            Assert.Equal(weeks,
                decision.GeneralEnduranceWeeks!.Value + decision.PreparationRunwayWeeks!.Value + decision.CoreWeeks!.Value);
        }
    }

    [Fact]
    public void MonotonicityHolds_ForEveryTransitionInRange()
    {
        for (var weeks = 21; weeks < 52; weeks++)
        {
            var current = Resolve(weeks);
            var next = Resolve(weeks + 1);

            Assert.Equal(current.GeneralEnduranceWeeks!.Value + 1, next.GeneralEnduranceWeeks!.Value);
            Assert.Equal(8, current.PreparationRunwayWeeks);
            Assert.Equal(8, next.PreparationRunwayWeeks);
            Assert.Equal(12, current.CoreWeeks);
            Assert.Equal(12, next.CoreWeeks);
            Assert.Equal(LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore, next.HorizonPath);

            // Classification changes ONLY at the GE 3 -> 4 boundary.
            if (weeks == 23)
            {
                Assert.Equal(GeneralEnduranceDurationClassification.ShortExtension, current.GeneralEnduranceClassification);
                Assert.Equal(GeneralEnduranceDurationClassification.FullPhase, next.GeneralEnduranceClassification);
            }
            else
            {
                Assert.Equal(current.GeneralEnduranceClassification, next.GeneralEnduranceClassification);
            }
        }
    }

    [Fact]
    public void RunwayAndCoreRemainFixed_ForEveryValueInRange()
    {
        for (var weeks = 21; weeks <= 52; weeks++)
        {
            var decision = Resolve(weeks);
            Assert.Equal(8, decision.PreparationRunwayWeeks);
            Assert.Equal(12, decision.CoreWeeks);
        }
    }

    // ── Provenance / determinism ─────────────────────────────────────────────

    [Fact]
    public void PolicyProvenanceIsPresent()
    {
        var decision = Resolve(24);
        Assert.Equal("TD-LONG-HORIZON-COMPOSITION-001", decision.PolicyId);
        Assert.False(string.IsNullOrWhiteSpace(decision.PolicyVersion));
        Assert.Equal("TD-LONG-HORIZON-GE-SAFETY-001", decision.DeferredSafetyPolicyId);
        Assert.Equal("TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001", decision.RecoveryPolicyId);
    }

    [Fact]
    public void DecisionIsDeterministic_ForBothProfiles()
    {
        foreach (var profile in new[] { ReadinessProfile.ConsistencyNeeded, ReadinessProfile.CoreEntryReady })
        {
            var first = Resolve(24, profile);
            var second = Resolve(24, profile);
            // Compare every field except Rules explicitly: Rules is an
            // IReadOnlyList<string>, and the record-synthesized equality
            // compares that interface-typed field by reference, not
            // content -- two independently-resolved arrays with identical
            // logical content are legitimately different instances. This
            // does not indicate non-determinism (every other field, and
            // Rules' own content, are asserted equal below).
            Assert.Equal(first with { Rules = second.Rules }, second);
            Assert.Equal(first.Rules, second.Rules); // content equality
        }
    }

    // ── Validator ─────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_AcceptsEveryRealResolvedDecision()
    {
        for (var weeks = 7; weeks <= 60; weeks++)
        {
            var result = LongHorizonCompositionValidator.Validate(Resolve(weeks));
            Assert.True(result.IsValid, $"weeks={weeks}: {string.Join("; ", result.Findings)}");
        }
    }

    [Fact]
    public void Validator_RejectsAnImpossibleHandConstructedDecision()
    {
        var impossible = Resolve(24) with { PreparationRunwayWeeks = 5 }; // violates fixed-8 invariant
        var result = LongHorizonCompositionValidator.Validate(impossible);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public void Validator_RejectsUnsupportedDecisionMissingReasonCode()
    {
        var malformed = Resolve(53) with { ReasonCode = null };
        var result = LongHorizonCompositionValidator.Validate(malformed);
        Assert.False(result.IsValid);
    }

    // ── Generation containment (at the resolver level) ──────────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    [InlineData(52)]
    public void EligibleLongHorizonDecisions_AreNeverActivatedForGeneration(int weeks)
    {
        var decision = Resolve(weeks);
        Assert.Equal(LongHorizonGenerationActivationStatus.EligibleButGenerationNotActivated, decision.GenerationActivationStatus);
        Assert.Equal("LONG_HORIZON_GENERATION_NOT_ACTIVATED", decision.ReasonCode);
    }

    [Fact]
    public void GenerationNotActivatedReason_IsNeverUsedFor53Plus() =>
        Assert.NotEqual("LONG_HORIZON_GENERATION_NOT_ACTIVATED", Resolve(53).ReasonCode);
}
