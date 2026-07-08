using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class WorkoutProgressionValidatorTests
{
    [Fact]
    public void ValidFixture_Passes()
    {
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();

        var result = WorkoutProgressionValidator.Validate(fixture.WorkoutProgression, snapshot);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => i.Code)));
    }

    [Fact]
    public void CircularFallback_Fails()
    {
        var fixture = new CombinationFixture();

        var progression = fixture.WorkoutProgression with
        {
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Build,
                    Stages =
                    [
                        new WorkoutProgressionStageDefinition
                        {
                            StageKey = "A", RelativeOrder = 1, WorkoutCandidateKeys = [fixture.ThresholdWorkout.Metadata.Key],
                            MinimumExposures = 1, MaximumExposures = 2,
                            CompressionBehavior = StageCompressionBehavior.Compressible, ExtensionBehavior = StageExtensionBehavior.Extendable,
                            Requires = [], FallbackStageKey = "B"
                        },
                        new WorkoutProgressionStageDefinition
                        {
                            StageKey = "B", RelativeOrder = 2, WorkoutCandidateKeys = [fixture.ThresholdWorkout.Metadata.Key],
                            MinimumExposures = 1, MaximumExposures = 2,
                            CompressionBehavior = StageCompressionBehavior.Compressible, ExtensionBehavior = StageExtensionBehavior.Extendable,
                            Requires = [], FallbackStageKey = "A"
                        }
                    ]
                }
            ]
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(progression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .Build();

        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_CIRCULAR_FALLBACK");
    }

    [Fact]
    public void SelfFallback_Fails()
    {
        var fixture = new CombinationFixture();

        var progression = fixture.WorkoutProgression with
        {
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Build,
                    Stages =
                    [
                        new WorkoutProgressionStageDefinition
                        {
                            StageKey = "A", RelativeOrder = 1, WorkoutCandidateKeys = [fixture.ThresholdWorkout.Metadata.Key],
                            MinimumExposures = 1, MaximumExposures = 2,
                            CompressionBehavior = StageCompressionBehavior.Compressible, ExtensionBehavior = StageExtensionBehavior.Extendable,
                            Requires = [], FallbackStageKey = "A"
                        }
                    ]
                }
            ]
        };

        var snapshot = fixture.BuildSnapshot();
        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_SELF_FALLBACK");
    }

    [Fact]
    public void ExposureBoundsInverted_Fails()
    {
        var fixture = new CombinationFixture();

        var progression = fixture.WorkoutProgression with
        {
            PhaseProgressions =
            [
                fixture.WorkoutProgression.PhaseProgressions[0] with
                {
                    Stages =
                    [
                        fixture.WorkoutProgression.PhaseProgressions[0].Stages[0] with { MinimumExposures = 5, MaximumExposures = 1 }
                    ]
                }
            ]
        };

        var snapshot = fixture.BuildSnapshot();
        var result = WorkoutProgressionValidator.Validate(progression, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "WP_EXPOSURE_BOUNDS_INVALID");
    }
}
