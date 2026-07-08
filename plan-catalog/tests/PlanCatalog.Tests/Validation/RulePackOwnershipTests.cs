using PlanCatalog.Contracts;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Validation;
using PlanCatalog.Infrastructure.Hashing;
using PlanCatalog.Infrastructure.Publishing;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Milestone C: RulePack ownership. Tests 14-19 of the Part 2 required-tests list — see
/// artifacts/audits/rule-pack-ownership-audit.md. Decision C: TemplateCombinationDefinition.RulePack is
/// the sole exact RulePack selection; the master's new RequiredRuleKeys is semantic-only.
/// </summary>
public sealed class RulePackOwnershipTests
{
    private static CatalogSourceSnapshot BuildSnapshot(CombinationFixture fixture, PlanCatalog.Core.Models.PlanTemplateDefinition master, PlanCatalog.Core.Models.RulePackDefinition rulePack)
    {
        var combination = fixture.Combination with
        {
            MasterTemplate = new VersionedCatalogReference { DocumentType = DocumentTypes.PlanTemplate, Key = master.Metadata.Key, Version = master.Metadata.Version },
            RulePack = new VersionedCatalogReference { DocumentType = DocumentTypes.RulePack, Key = rulePack.Metadata.Key, Version = rulePack.Metadata.Version }
        };

        return new CatalogSnapshotBuilder()
            .With(master).With(fixture.Layout).With(fixture.LevelModifier)
            .With(fixture.WorkoutProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(rulePack)
            .With(combination)
            .Build();
    }

    [Fact]
    public void CombinationExactRulePack_IsTheOnlySelectedRulePack_BundleAssemblyIgnoresRequiredRules()
    {
        // Test 14.
        var fixture = new CombinationFixture();
        var secondRulePack = fixture.RulePack with { Metadata = Meta.Of(DocumentTypes.RulePack, "SECOND_RULE_PACK", status: Core.Enums.CatalogStatus.Published) };
        var masterRequiringFirst = fixture.MasterTemplate with
        {
            RequiredRules = [new VersionedCatalogReference { DocumentType = DocumentTypes.RulePack, Key = fixture.RulePack.Metadata.Key, Version = fixture.RulePack.Metadata.Version }]
        };

        var combination = fixture.Combination with
        {
            RulePack = new VersionedCatalogReference { DocumentType = DocumentTypes.RulePack, Key = secondRulePack.Metadata.Key, Version = secondRulePack.Metadata.Version }
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(masterRequiringFirst).With(fixture.Layout).With(fixture.LevelModifier)
            .With(fixture.WorkoutProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy)
            .With(fixture.RulePack).With(secondRulePack)
            .With(combination)
            .Build();

        var stamped = CatalogStamper.StampAsPublished(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher(), snapshot);
        var assembler = new CatalogBundleAssembler(new SystemTextJsonCanonicalSerializer(), new Sha256ContentHasher());
        var bundle = assembler.Assemble(stamped, combination.Metadata.Key, combination.Metadata.Version);

        Assert.Equal(secondRulePack.Metadata.Key, bundle.RulePack.Key);
    }

    [Fact]
    public void NewMasterSemanticKeyRequirement_IsEnforced_MatchingKeyPasses()
    {
        // Test 15.
        var fixture = new CombinationFixture();
        var masterWithSemanticRequirement = fixture.MasterTemplate with
        {
            Metadata = fixture.MasterTemplate.Metadata with { SchemaVersion = 2 },
            RequiredRules = null,
            RequiredRuleKeys = [fixture.RulePack.Metadata.Key]
        };

        var snapshot = BuildSnapshot(fixture, masterWithSemanticRequirement, fixture.RulePack);
        var combination = snapshot.Combinations.Single();

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.DoesNotContain(result.Issues, i => i.Code == "COMBINATION_RULE_PACK_DOES_NOT_SATISFY_MASTER_REQUIREMENTS");
    }

    [Fact]
    public void MismatchingRulePackKey_Fails()
    {
        // Test 16.
        var fixture = new CombinationFixture();
        var masterWithSemanticRequirement = fixture.MasterTemplate with
        {
            Metadata = fixture.MasterTemplate.Metadata with { SchemaVersion = 2 },
            RequiredRules = null,
            RequiredRuleKeys = ["SOME_OTHER_RULE_PACK_KEY"]
        };

        var snapshot = BuildSnapshot(fixture, masterWithSemanticRequirement, fixture.RulePack);
        var combination = snapshot.Combinations.Single();

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.Contains(result.Issues, i => i.Code == "COMBINATION_RULE_PACK_DOES_NOT_SATISFY_MASTER_REQUIREMENTS");
    }

    [Fact]
    public void ChangingOnlyRulePackVersion_DoesNotRequireChangingMaster_WhenKeyRemainsAcceptable()
    {
        // Test 17: master's semantic requirement is key-only, so a RulePack version bump (same key)
        // continues to satisfy it without any master change.
        var fixture = new CombinationFixture();
        var rulePackV2 = fixture.RulePack with { Metadata = fixture.RulePack.Metadata with { Version = 2 } };
        var masterWithSemanticRequirement = fixture.MasterTemplate with
        {
            Metadata = fixture.MasterTemplate.Metadata with { SchemaVersion = 2 },
            RequiredRules = null,
            RequiredRuleKeys = [fixture.RulePack.Metadata.Key]
        };

        var snapshot = BuildSnapshot(fixture, masterWithSemanticRequirement, rulePackV2);
        var combination = snapshot.Combinations.Single();

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.DoesNotContain(result.Issues, i => i.Code == "COMBINATION_RULE_PACK_DOES_NOT_SATISFY_MASTER_REQUIREMENTS");
    }

    [Fact]
    public void NewMasterSchema_RejectsSimultaneousRequiredRulesAndRequiredRuleKeys()
    {
        // Test 18.
        var fixture = new CombinationFixture();
        var invalidMaster = fixture.MasterTemplate with
        {
            Metadata = fixture.MasterTemplate.Metadata with { SchemaVersion = 2 },
            RequiredRuleKeys = [fixture.RulePack.Metadata.Key]
            // RequiredRules deliberately left populated too (fixture default).
        };

        var snapshot = BuildSnapshot(fixture, invalidMaster, fixture.RulePack);

        var result = SchemaVersionShapeValidator.Validate(snapshot);

        Assert.Contains(result.Issues, i => i.Code == "SCHEMA_SHAPE_BOTH_FORMS_PRESENT");
    }

    [Fact]
    public void HistoricalMasters_RemainReadable_LegacyRequiredRulesNotRetroactivelyChecked()
    {
        // Test 19: a legacy (schemaVersion 1) master using RequiredRules is not cross-checked against the
        // combination's RulePack — historical masters remain readable under their original rules.
        var fixture = new CombinationFixture();
        var mismatchedRulePack = fixture.RulePack with { Metadata = Meta.Of(DocumentTypes.RulePack, "DIFFERENT_RULE_PACK_KEY", status: Core.Enums.CatalogStatus.Published) };

        var snapshot = BuildSnapshot(fixture, fixture.MasterTemplate, mismatchedRulePack);
        var combination = snapshot.Combinations.Single();

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.DoesNotContain(result.Issues, i => i.Code == "COMBINATION_RULE_PACK_DOES_NOT_SATISFY_MASTER_REQUIREMENTS");
    }
}
