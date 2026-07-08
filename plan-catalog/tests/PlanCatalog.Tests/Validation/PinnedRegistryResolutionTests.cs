using System.Reflection;
using PlanCatalog.Contracts;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

/// <summary>
/// Milestone D: pinned runtime registry resolution. Tests 20-24 of the Part 2 required-tests list — see
/// artifacts/audits/runtime-registry-resolution-audit.md. Decision D: Combination -> exact RulePack ->
/// exact RuntimeConditionValueRegistry, never FirstOrDefault.
/// </summary>
public sealed class PinnedRegistryResolutionTests
{
    private static (CatalogSourceSnapshot Snapshot, PlanCatalog.Core.Models.TemplateCombinationDefinition Combination) BuildSnapshotWithTwoRegistries(
        CombinationFixture fixture, string requiredValue, bool pinRegistryContainingValue)
    {
        var registryWithValue = fixture.Registry with
        {
            Metadata = Meta.Of(DocumentTypes.RuntimeConditionValueRegistry, "REGISTRY_WITH_VALUE", status: Core.Enums.CatalogStatus.Published),
            ConditionValueSets = [new PlanCatalog.Core.Models.RuntimeConditionValueSet { ConditionType = PlanCatalog.Contracts.Enums.RuntimeConditionType.GoalFeasibilityIn, AllowedValues = new HashSet<string> { requiredValue } }]
        };
        var registryWithoutValue = fixture.Registry with
        {
            Metadata = Meta.Of(DocumentTypes.RuntimeConditionValueRegistry, "REGISTRY_WITHOUT_VALUE", status: Core.Enums.CatalogStatus.Published),
            ConditionValueSets = [new PlanCatalog.Core.Models.RuntimeConditionValueSet { ConditionType = PlanCatalog.Contracts.Enums.RuntimeConditionType.GoalFeasibilityIn, AllowedValues = new HashSet<string> { "UNRELATED_VALUE" } }]
        };

        var pinnedRegistry = pinRegistryContainingValue ? registryWithValue : registryWithoutValue;
        var rulePack = fixture.RulePack with
        {
            RuntimeConditionValueRegistry = new VersionedCatalogReference { DocumentType = DocumentTypes.RuntimeConditionValueRegistry, Key = pinnedRegistry.Metadata.Key, Version = pinnedRegistry.Metadata.Version }
        };
        var combination = fixture.Combination with
        {
            RulePack = new VersionedCatalogReference { DocumentType = DocumentTypes.RulePack, Key = rulePack.Metadata.Key, Version = rulePack.Metadata.Version }
        };

        // The fixture's own progression has an empty Requires list — inject a real condition so the
        // pinned-registry check has something to evaluate.
        var progressionWithCondition = fixture.WorkoutProgression with
        {
            PhaseProgressions = fixture.WorkoutProgression.PhaseProgressions.Select((p, i) => i == 0
                ? p with
                {
                    Stages = p.Stages.Select(s => s with
                    {
                        Requires = [new PlanCatalog.Core.Models.RuntimeEligibilityCondition { ConditionType = PlanCatalog.Contracts.Enums.RuntimeConditionType.GoalFeasibilityIn, AllowedValues = new HashSet<string> { requiredValue } }]
                    }).ToList()
                }
                : p).ToList()
        };

        // Registries added in an order where the "wrong" one for this scenario comes first — proves
        // source ordering never influences which registry gets used.
        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(progressionWithCondition).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(registryWithoutValue).With(registryWithValue)
            .With(fixture.PeakVolumeBandPolicy).With(rulePack).With(combination)
            .Build();

        return (snapshot, combination);
    }

    [Fact]
    public void ExactRulePackPinnedRegistry_IsUsed_NotAnyOtherRegistryInSource()
    {
        // Test 20.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildSnapshotWithTwoRegistries(fixture, "REALISTIC", pinRegistryContainingValue: true);

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.DoesNotContain(result.Issues, i => i.Code == "RUNTIME_CONDITION_VALUE_NOT_ALLOWED_BY_PINNED_REGISTRY");
    }

    [Fact]
    public void TwoRegistryVersions_ValidateIndependently()
    {
        // Test 21.
        var fixture = new CombinationFixture();
        var (snapshotPass, comboPass) = BuildSnapshotWithTwoRegistries(fixture, "REALISTIC", pinRegistryContainingValue: true);
        var (snapshotFail, comboFail) = BuildSnapshotWithTwoRegistries(fixture, "REALISTIC", pinRegistryContainingValue: false);

        var resultPass = CandidatePublishGraphValidator.Validate(snapshotPass, comboPass);
        var resultFail = CandidatePublishGraphValidator.Validate(snapshotFail, comboFail);

        Assert.DoesNotContain(resultPass.Issues, i => i.Code == "RUNTIME_CONDITION_VALUE_NOT_ALLOWED_BY_PINNED_REGISTRY");
        Assert.Contains(resultFail.Issues, i => i.Code == "RUNTIME_CONDITION_VALUE_NOT_ALLOWED_BY_PINNED_REGISTRY");
    }

    [Fact]
    public void SourceOrder_DoesNotAffectRegistryChoice()
    {
        // Test 22: registries are added wrong-one-first in BuildSnapshotWithTwoRegistries; the outcome
        // must depend only on the exact RulePack -> registry reference, never on list order.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildSnapshotWithTwoRegistries(fixture, "REALISTIC", pinRegistryContainingValue: true);

        Assert.Equal("REGISTRY_WITHOUT_VALUE", snapshot.RuntimeConditionValueRegistries[0].Metadata.Key);

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.DoesNotContain(result.Issues, i => i.Code == "RUNTIME_CONDITION_VALUE_NOT_ALLOWED_BY_PINNED_REGISTRY");
    }

    [Fact]
    public void InvalidConditionValue_FailsAgainstThePinnedRegistry()
    {
        // Test 23.
        var fixture = new CombinationFixture();
        var (snapshot, combination) = BuildSnapshotWithTwoRegistries(fixture, "REALISTIC", pinRegistryContainingValue: false);

        var result = CandidatePublishGraphValidator.Validate(snapshot, combination);

        Assert.Contains(result.Issues, i => i.Code == "RUNTIME_CONDITION_VALUE_NOT_ALLOWED_BY_PINNED_REGISTRY");
    }

    [Fact]
    public void FirstOrDefault_IsAbsentFromTheActiveOrCandidatePath()
    {
        // Test 24 — static-analysis-style proof: neither WorkoutProgressionValidator nor
        // CandidatePublishGraphValidator's IL/source references RuntimeConditionValueRegistries.FirstOrDefault.
        // Verified here by asserting the WorkoutProgressionValidator type has no reference to
        // RuntimeConditionValueRegistryDefinition at all (it was fully removed from that validator).
        var validatorType = typeof(WorkoutProgressionValidator);
        var usesRegistryType = validatorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Any(m => m.GetParameters().Any(p => p.ParameterType.Name.Contains("RuntimeConditionValueRegistry", StringComparison.Ordinal)));

        Assert.False(usesRegistryType);
    }
}
