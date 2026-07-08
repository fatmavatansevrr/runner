using PlanCatalog.Contracts;
using PlanCatalog.Core.Audit;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class PilotDomainContentAuditTests
{
    [Fact]
    public void CoreCycleWeeks_IsClassifiedCanonicalConfirmed_NotBlocking()
    {
        var entries = PilotDomainContentAudit.Entries
            .Where(e => e.DocumentType == DocumentTypes.PlanTemplate && e.Key == "TEN_K_MASTER" && e.JsonPath.Contains("coreCycle"))
            .ToList();

        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.Equal(ContentDecisionStatus.CanonicalConfirmed, e.Classification));
        Assert.All(entries, e => Assert.False(e.IsBlocking));
    }

    [Fact]
    public void ProgressionModifierDosage_IsClassifiedPlaceholderUnconfirmed_Blocking()
    {
        var hasBlocking = PilotDomainContentAudit.HasBlockingUnconfirmedContent(
            DocumentTypes.ProgressionModifier, "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 1);

        Assert.True(hasBlocking);
    }

    [Fact]
    public void UnknownArtifact_HasNoBlockingContent()
    {
        var hasBlocking = PilotDomainContentAudit.HasBlockingUnconfirmedContent("PLAN_TEMPLATE", "DOES_NOT_EXIST", 1);

        Assert.False(hasBlocking);
    }

    [Fact]
    public void AllEntryIds_AreUnique()
    {
        var duplicates = PilotDomainContentAudit.Entries
            .GroupBy(e => e.EntryId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void ConfirmedWorkoutDefinitionFields_CiteGoldenFixtureV3AsSource()
    {
        // v1 was restored to its original, unconfirmed legacy content (WORKOUT-IMMUT-001); the
        // fixture-confirmed prescription mode now lives on v2.
        var entry = PilotDomainContentAudit.Entries.Single(e =>
            e.DocumentType == DocumentTypes.WorkoutDefinition && e.Key == "EASY_STANDARD" && e.Version == 2 && e.JsonPath == "$.allowedPrescriptionModes");

        Assert.Equal(ContentDecisionStatus.CanonicalConfirmed, entry.Classification);
        Assert.Contains("golden-10k-intermediate-4d-12w.v3.plandocument.json", entry.SourceFile);
    }

    [Fact]
    public void GoalPaceTenK_HasNoConfirmedPrescriptionOrAccountingModeEntry()
    {
        // No Golden Fixture v3 evidence exists for this key — it must remain grouped as unconfirmed,
        // not split into a separately-confirmed prescription/accounting entry like the other 4 workouts.
        var entries = PilotDomainContentAudit.Entries.Where(e => e.Key == "GOAL_PACE_TEN_K").ToList();

        Assert.DoesNotContain(entries, e => e.JsonPath == "$.allowedDistanceAccountingModes");
        Assert.DoesNotContain(entries, e => e.JsonPath == "$.allowedPrescriptionModes" && e.Classification == ContentDecisionStatus.CanonicalConfirmed);
    }

    [Fact]
    public void PassingStructuralTests_DoNotImplyCanonicalClassification()
    {
        // The 4-day layout's shape (1 KEY_SESSION, 2 EASY_SUPPORT, 1 LONG_RUN) is CANONICAL_CONFIRMED,
        // but sequenceOrder assignment is a separate, still-PLACEHOLDER_UNCONFIRMED decision — passing
        // RunLayoutValidator (structural) says nothing about whether that assignment is a sourced decision.
        var sequenceEntry = PilotDomainContentAudit.Entries.Single(e =>
            e.DocumentType == DocumentTypes.RunLayout && e.JsonPath.Contains("sequenceOrder"));

        Assert.Equal(ContentDecisionStatus.PlaceholderUnconfirmed, sequenceEntry.Classification);
    }
}
