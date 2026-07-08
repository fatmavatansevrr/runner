using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Infrastructure.Serialization;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Serialization;

public sealed class CanonicalSerializationTests
{
    private readonly SystemTextJsonCanonicalSerializer _serializer = new();

    [Fact]
    public void Enums_SerializeAsUpperSnakeCase()
    {
        var workout = new WorkoutDefinition
        {
            Metadata = Meta.Of("WORKOUT_DEFINITION", "EASY_STANDARD"),
            Family = WorkoutFamily.LongRun,
            ComplexityTier = 1,
            EligiblePhases = [PhaseKey.RaceSpecific],
            AllowedPrescriptionModes = [PrescriptionMode.HeartRateBased],
            Components = []
        };

        var json = _serializer.Serialize(workout);

        Assert.Contains("\"family\":\"LONG_RUN\"", json);
        Assert.Contains("\"RACE_SPECIFIC\"", json);
        Assert.Contains("\"HEART_RATE_BASED\"", json);
    }

    [Fact]
    public void NullOptionalFields_AreOmitted()
    {
        var stage = new WorkoutProgressionStageDefinition
        {
            StageKey = "A",
            RelativeOrder = 1,
            WorkoutCandidateKeys = ["EASY_STANDARD"],
            MinimumExposures = 1,
            MaximumExposures = 2,
            CompressionBehavior = StageCompressionBehavior.Compressible,
            ExtensionBehavior = StageExtensionBehavior.Extendable,
            Requires = [],
            FallbackStageKey = null
        };

        var json = _serializer.Serialize(stage);

        Assert.DoesNotContain("fallbackStageKey", json, StringComparison.Ordinal);
    }

    [Fact]
    public void NoClrTypeNamesAppearInOutput()
    {
        var workout = new WorkoutDefinition
        {
            Metadata = Meta.Of("WORKOUT_DEFINITION", "EASY_STANDARD"),
            Family = WorkoutFamily.Easy,
            ComplexityTier = 1,
            EligiblePhases = [PhaseKey.Foundation],
            AllowedPrescriptionModes = [PrescriptionMode.EffortBased],
            Components = []
        };

        var json = _serializer.Serialize(workout);

        Assert.DoesNotContain("PlanCatalog.Core", json, StringComparison.Ordinal);
        Assert.DoesNotContain("System.", json, StringComparison.Ordinal);
        Assert.DoesNotContain("$type", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SetOrder_DoesNotAffectSerializedOutput()
    {
        var a = new LevelModifierDefinition
        {
            Metadata = Meta.Of("LEVEL_MODIFIER", "INTERMEDIATE_MODIFIER"),
            Experience = RunningExperience.Intermediate,
            EligibleWorkoutKeys = new HashSet<string> { "ZEBRA", "ALPHA", "MIKE" },
            ProgressionModifier = new PlanCatalog.Contracts.References.VersionedCatalogReference { DocumentType = "PROGRESSION_MODIFIER", Key = "X", Version = 1 }
        };

        var b = a with { EligibleWorkoutKeys = new HashSet<string> { "MIKE", "ZEBRA", "ALPHA" } };

        Assert.Equal(_serializer.Serialize(a), _serializer.Serialize(b));
    }

    [Fact]
    public void RoundTrip_PreservesSemanticEquality()
    {
        var original = new ProgressionModifierDefinition
        {
            Metadata = Meta.Of("PROGRESSION_MODIFIER", "INTERMEDIATE_PROGRESSION_MODIFIER_V1"),
            Experience = RunningExperience.Intermediate,
            MaximumComplexityTier = 2,
            MaximumHardSessionsPerWeek = 1,
            MainSetDoseMultiplier = 1.15m,
            AllowGoalPaceRehearsal = true,
            AllowSecondHardStimulus = false
        };

        var json = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize<ProgressionModifierDefinition>(json);

        Assert.Equal(original, roundTripped);
    }
}
