using PlanCatalog.Contracts.Enums;
using PlanCatalog.Infrastructure.Serialization;
using Xunit;

namespace PlanCatalog.Tests.Serialization;

/// <summary>
/// PrescriptionMode (how a workout is prescribed) and DistanceAccountingMode (how session distance
/// reconciles with components) are deliberately separate vocabularies — see Golden Fixture v3, where
/// every workout carries both fields independently. They must never overlap.
/// </summary>
public sealed class PrescriptionModeAndDistanceAccountingModeSeparationTests
{
    [Theory]
    [InlineData(PrescriptionMode.Distance, "DISTANCE")]
    [InlineData(PrescriptionMode.Mixed, "MIXED")]
    public void PrescriptionMode_ConfirmedValues_SerializeAsUpperSnakeCase(PrescriptionMode mode, string expected)
    {
        var json = new SystemTextJsonCanonicalSerializer().Serialize(mode);
        Assert.Equal($"\"{expected}\"", json);
    }

    [Theory]
    [InlineData(DistanceAccountingMode.ExactSessionTotal, "EXACT_SESSION_TOTAL")]
    [InlineData(DistanceAccountingMode.EstimatedSessionTotal, "ESTIMATED_SESSION_TOTAL")]
    [InlineData(DistanceAccountingMode.EmbeddedComponents, "EMBEDDED_COMPONENTS")]
    public void DistanceAccountingMode_Values_SerializeAsUpperSnakeCase(DistanceAccountingMode mode, string expected)
    {
        var json = new SystemTextJsonCanonicalSerializer().Serialize(mode);
        Assert.Equal($"\"{expected}\"", json);
    }

    [Fact]
    public void PrescriptionMode_DoesNotContainAnyDistanceAccountingModeValue()
    {
        var prescriptionModeNames = Enum.GetNames<PrescriptionMode>();
        var accountingModeNames = Enum.GetNames<DistanceAccountingMode>();

        Assert.Empty(prescriptionModeNames.Intersect(accountingModeNames, StringComparer.Ordinal));
    }

    [Fact]
    public void DistanceAccountingMode_DoesNotContainAnyPrescriptionModeValue()
    {
        var prescriptionModeNames = Enum.GetNames<PrescriptionMode>();
        var accountingModeNames = Enum.GetNames<DistanceAccountingMode>();

        Assert.Empty(accountingModeNames.Intersect(prescriptionModeNames, StringComparer.Ordinal));
    }

    [Fact]
    public void PrescriptionMode_ContainsConfirmedGoldenFixtureValues()
    {
        Assert.Contains(PrescriptionMode.Distance, Enum.GetValues<PrescriptionMode>());
        Assert.Contains(PrescriptionMode.Mixed, Enum.GetValues<PrescriptionMode>());
    }

    [Fact]
    public void DistanceAccountingMode_ContainsExactlyTheThreeConfirmedGoldenFixtureValues()
    {
        var values = Enum.GetNames<DistanceAccountingMode>();
        Assert.Equal(
            new[] { "ExactSessionTotal", "EstimatedSessionTotal", "EmbeddedComponents" },
            values);
    }
}
