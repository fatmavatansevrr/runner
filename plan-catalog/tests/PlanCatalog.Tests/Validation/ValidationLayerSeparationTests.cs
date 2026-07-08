using PlanCatalog.Contracts;
using PlanCatalog.Core.Ports;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Milestone A of artifacts/audits/deterministic-graph-part2-migration.md: source-integrity
/// (<see cref="CatalogGraphValidator"/>/<see cref="TemplateCombinationValidator"/>) vs publish-graph
/// (<see cref="CandidatePublishGraphValidator"/>) separation. Tests 1-5 of the Part 2 required-tests list.
/// </summary>
public sealed class ValidationLayerSeparationTests
{
    private sealed class FakeRetirementLedger(params (string DocumentType, string Key, int Version)[] retired) : IRetirementLedger
    {
        public bool IsRetired(string documentType, string key, int version) =>
            retired.Contains((documentType, key, version));
    }

    [Fact]
    public void ValidRetiredSource_RemainsReadable_SourceIntegrityStillPasses()
    {
        // Test 1: a well-formed document that happens to be retired must still pass source-integrity
        // validation — CatalogGraphValidator has no retirement concept at all, so retirement can never
        // cause a false structural failure.
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();

        var result = CatalogGraphValidator.Validate(snapshot);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void MalformedSource_FailsSourceIntegrityValidation_RegardlessOfRetirementStatus()
    {
        // Test 2: malformed content fails source-integrity validation unconditionally — retirement status
        // is never consulted by this layer, so it cannot rescue malformed content.
        var fixture = new CombinationFixture();
        var malformedProgression = fixture.WorkoutProgression with
        {
            PhaseProgressions = fixture.WorkoutProgression.PhaseProgressions.Select(p => p with
            {
                Stages = p.Stages.Select(s => s with { RelativeOrder = 99 }).ToList()
            }).ToList()
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(malformedProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack).With(fixture.Combination)
            .Build();

        var result = CatalogGraphValidator.Validate(snapshot);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "WP_RELATIVE_ORDER_NOT_CONTIGUOUS");
    }

    [Fact]
    public void RetiredRoot_DoesNotPoisonContextualValidationOfADifferentCandidateRoot()
    {
        // Test 3: retiring combination A must not affect publish-graph validation of an unrelated,
        // non-retired combination B that happens to share the same catalog snapshot.
        var fixture = new CombinationFixture();
        var combinationB = fixture.Combination with { Metadata = Meta.Of(DocumentTypes.TemplateCombination, "OTHER_COMBINATION", status: Core.Enums.CatalogStatus.Published) };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(fixture.WorkoutProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .With(fixture.Combination).With(combinationB)
            .Build();

        var ledger = new FakeRetirementLedger((DocumentTypes.TemplateCombination, fixture.Combination.Metadata.Key, fixture.Combination.Metadata.Version));

        var resultForRetiredRoot = CandidatePublishGraphValidator.Validate(snapshot, fixture.Combination, ledger);
        var resultForOtherRoot = CandidatePublishGraphValidator.Validate(snapshot, combinationB, ledger);

        Assert.False(resultForRetiredRoot.IsValid);
        Assert.Contains(resultForRetiredRoot.Issues, i => i.Code == "RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE");
        Assert.True(resultForOtherRoot.IsValid);
    }

    [Fact]
    public void ExplicitRetiredRootBuild_FailsWithSuggestedCode()
    {
        // Test 4.
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();
        var ledger = new FakeRetirementLedger((DocumentTypes.TemplateCombination, fixture.Combination.Metadata.Key, fixture.Combination.Metadata.Version));

        var result = CandidatePublishGraphValidator.Validate(snapshot, fixture.Combination, ledger);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE");
    }
}
