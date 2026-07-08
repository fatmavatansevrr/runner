using PlanCatalog.Contracts;
using PlanCatalog.Core.Ports;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Combinations;

/// <summary>
/// Retirement eligibility is a PUBLISH-GRAPH concern (<see cref="CandidatePublishGraphValidator"/>), not a
/// source-integrity concern (<see cref="TemplateCombinationValidator"/>/<see cref="CatalogGraphValidator"/>)
/// — see artifacts/audits/deterministic-graph-prechange-assessment.md Finding 1 and Milestone A of
/// artifacts/audits/deterministic-graph-part2-migration.md. New bundle assembly may only use eligible
/// PUBLISHED, non-retired artifacts. Historical bundles that already reference a since-retired artifact
/// are unaffected — retirement never mutates history.
/// </summary>
public sealed class RetirementResolutionTests
{
    private sealed class FakeRetirementLedger(params (string DocumentType, string Key, int Version)[] retired) : IRetirementLedger
    {
        public bool IsRetired(string documentType, string key, int version) =>
            retired.Contains((documentType, key, version));
    }

    [Fact]
    public void RetiredProgressionModifier_FailsCandidatePublishGraphValidation()
    {
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();
        var ledger = new FakeRetirementLedger((DocumentTypes.ProgressionModifier, fixture.ProgressionModifier.Metadata.Key, fixture.ProgressionModifier.Metadata.Version));

        var result = CandidatePublishGraphValidator.Validate(snapshot, fixture.Combination, ledger);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "PUBLISH_DEPENDENCY_RETIRED");
    }

    [Fact]
    public void NonRetiredProgressionModifier_DoesNotTriggerRetirementFailure()
    {
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();
        var ledger = new FakeRetirementLedger((DocumentTypes.ProgressionModifier, "SOME_OTHER_KEY", 1));

        var result = CandidatePublishGraphValidator.Validate(snapshot, fixture.Combination, ledger);

        Assert.DoesNotContain(result.Issues, i => i.Code == "PUBLISH_DEPENDENCY_RETIRED");
    }

    [Fact]
    public void CatalogGraphValidator_SourceIntegrityLayer_NeverConsultsRetirement()
    {
        // Confirms the Finding-1 fix: a retired dependency must not poison source-integrity validation of
        // the rest of the catalog — CatalogGraphValidator has no retirement-ledger parameter at all now.
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();

        var result = CatalogGraphValidator.Validate(snapshot);

        Assert.DoesNotContain(result.Issues, i => i.Code == "PUBLISH_DEPENDENCY_RETIRED" || i.Code == "TC_PROGRESSION_MODIFIER_RETIRED");
    }

    [Fact]
    public void NoRetirementLedgerSupplied_DefaultsToNothingRetired()
    {
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();

        var result = CandidatePublishGraphValidator.Validate(snapshot, fixture.Combination);

        Assert.True(result.IsValid);
    }
}
