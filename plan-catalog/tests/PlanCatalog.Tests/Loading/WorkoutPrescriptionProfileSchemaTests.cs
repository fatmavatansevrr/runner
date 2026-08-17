using PlanCatalog.Contracts;
using PlanCatalog.Infrastructure.Schema;
using Xunit;

namespace PlanCatalog.Tests.Loading;

public sealed class WorkoutPrescriptionProfileSchemaTests
{
    private static JsonSchemaNetValidator Validator() => new(Path.Combine(AppContext.BaseDirectory, "TestSchemas"));

    [Fact]
    public void ValidRepeatedProfile_PassesSchema()
    {
        var result = Validator().Validate(DocumentTypes.WorkoutPrescriptionProfile, ValidRepeatedJson());
        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(x => x.Message)));
    }

    [Theory]
    [InlineData("\"structureMode\":\"REPEATED\"", "\"structureMode\":\"UNKNOWN\"")]
    [InlineData("\"doseCategory\":\"PRIMARY\"", "\"doseCategory\":\"UNKNOWN\"")]
    [InlineData("\"durationSeconds\":60", "\"durationSeconds\":0")]
    [InlineData("\"mode\":\"JOG\"", "\"mode\":\"FLOAT\"")]
    [InlineData("\"schemaVersion\":1", "\"schemaVersion\":2")]
    public void InvalidSchemaInput_Fails(string oldValue, string newValue)
    {
        var result = Validator().Validate(DocumentTypes.WorkoutPrescriptionProfile, ValidRepeatedJson().Replace(oldValue, newValue));
        Assert.False(result.IsValid);
        Assert.All(result.Issues, x => Assert.Equal("SCHEMA_VALIDATION_ERROR", x.Code));
    }

    [Fact]
    public void ContradictoryWorkUnits_FailSchema()
    {
        var json = ValidRepeatedJson().Replace("\"durationSeconds\":60", "\"durationSeconds\":60,\"distanceMeters\":200");
        Assert.False(Validator().Validate(DocumentTypes.WorkoutPrescriptionProfile, json).IsValid);
    }

    [Fact]
    public void RepeatedWithoutCount_FailsSchema()
    {
        var json = ValidRepeatedJson().Replace("\"repetitionCount\":6,", "");
        Assert.False(Validator().Validate(DocumentTypes.WorkoutPrescriptionProfile, json).IsValid);
    }

    [Fact]
    public void RepeatedWithoutPlacement_FailsSchema() =>
        Assert.False(Validator().Validate(DocumentTypes.WorkoutPrescriptionProfile, ValidRepeatedJson().Replace(",\"recoveryPlacement\":\"BETWEEN_REPETITIONS\"", "")).IsValid);

    [Fact]
    public void ContinuousWithPlacement_FailsSchema()
    {
        var json = ValidRepeatedJson()
            .Replace("\"structureMode\":\"REPEATED\"", "\"structureMode\":\"CONTINUOUS\"")
            .Replace("\"repetitionCount\":6,", "")
            .Replace(",\"recoveryQuantity\":{\"durationSeconds\":60,\"mode\":\"JOG\"}", "");
        Assert.False(Validator().Validate(DocumentTypes.WorkoutPrescriptionProfile, json).IsValid);
    }

    [Fact]
    public void UnknownPlacement_FailsSchema() =>
        Assert.False(Validator().Validate(DocumentTypes.WorkoutPrescriptionProfile, ValidRepeatedJson().Replace("BETWEEN_REPETITIONS", "UNKNOWN")).IsValid);

    [Fact]
    public void RawRecoveryCount_FailsSchema() =>
        Assert.False(Validator().Validate(DocumentTypes.WorkoutPrescriptionProfile, ValidRepeatedJson().Replace("\"recoveryPlacement\"", "\"recoveryCount\":3,\"recoveryPlacement\"")).IsValid);

    private static string ValidRepeatedJson() => """
    {"metadata":{"documentType":"WORKOUT_PRESCRIPTION_PROFILE","schemaVersion":1,"key":"FARTLEK_CONTROLLED","version":1,"status":"DRAFT"},"workoutDefinitionRef":{"documentType":"WORKOUT_DEFINITION","key":"FARTLEK","version":4},"doseCategory":"PRIMARY","distanceAccountingMode":"ESTIMATED_SESSION_TOTAL","components":[{"sequenceOrder":1,"componentType":"MAIN_SET","structureMode":"REPEATED","workQuantity":{"repetitionCount":6,"durationSeconds":60},"recoveryQuantity":{"durationSeconds":60,"mode":"JOG"},"recoveryPlacement":"BETWEEN_REPETITIONS","intensityTarget":{"mode":"EFFORT_BASED","effortDescriptorKey":"CONTROLLED_SURGE"}}]}
    """;
}
