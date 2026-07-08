using PlanCatalog.Contracts;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Publishing;

/// <summary>
/// TemplateCombinationDefinition is a versioned catalog artifact and must follow the same immutability
/// rules as PlanTemplateDefinition: a published (key, version) tuple can never be re-published with
/// different content. This guards against the exact defect discovered and corrected in the
/// TEN_K__4D__INTERMEDIATE combination (see artifacts/audits/combination-immutability-investigation.md).
/// </summary>
public sealed class CombinationImmutabilityTests
{
    [Fact]
    public void PublishReadinessValidator_RejectsSameKeyVersion_WithDifferingContentHash_ForCombinations()
    {
        var original = new TemplateCombinationDefinition
        {
            Metadata = Meta.Of(DocumentTypes.TemplateCombination, "TEN_K__4D__INTERMEDIATE", version: 1, status: CatalogStatus.Published)
                with { ContentHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
            MasterTemplate = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.PlanTemplate, Key = "TEN_K_MASTER", Version = 1 },
            Layout = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.RunLayout, Key = "RUN_LAYOUT_4D", Version = 1 },
            LevelModifier = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.LevelModifier, Key = "INTERMEDIATE_MODIFIER", Version = 1 },
            RulePack = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.RulePack, Key = "APPSEL_RACE_PLAN_V1", Version = 1 }
        };

        // Same (documentType, key, version) but a different masterTemplate reference => different
        // semantic content => a different hash under the same published version, exactly like the
        // TEN_K__4D__INTERMEDIATE v1-mutation defect.
        var mutated = original with
        {
            MasterTemplate = original.MasterTemplate with { Version = 2 },
            Metadata = original.Metadata with { ContentHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" }
        };

        var snapshot = new CatalogSnapshotBuilder().With(original).With(mutated).Build();

        var result = PublishReadinessValidator.Validate(snapshot, PlanCatalog.Core.Validation.ValidationResult.Empty, PlanCatalog.Core.Validation.ValidationResult.Empty);

        Assert.Contains(result.Issues, i => i.Code == "PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION");
    }

    [Fact]
    public void CatalogGraphValidator_DetectsDuplicateKeyVersion_ForCombinations()
    {
        var a = new TemplateCombinationDefinition
        {
            Metadata = Meta.Of(DocumentTypes.TemplateCombination, "TEN_K__4D__INTERMEDIATE", version: 1),
            MasterTemplate = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.PlanTemplate, Key = "TEN_K_MASTER", Version = 1 },
            Layout = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.RunLayout, Key = "RUN_LAYOUT_4D", Version = 1 },
            LevelModifier = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.LevelModifier, Key = "INTERMEDIATE_MODIFIER", Version = 1 },
            RulePack = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = DocumentTypes.RulePack, Key = "APPSEL_RACE_PLAN_V1", Version = 1 }
        };

        // A second document declaring the same (Key, Version) — regardless of differing content —
        // must be flagged as a duplicate before it ever reaches hash comparison.
        var b = a with { MasterTemplate = a.MasterTemplate with { Version = 2 } };

        var snapshot = new CatalogSnapshotBuilder().With(a).With(b).Build();

        var result = CatalogGraphValidator.Validate(snapshot);

        Assert.Contains(result.Issues, i => i.Code == "GRAPH_DUPLICATE_KEY_VERSION");
    }
}
