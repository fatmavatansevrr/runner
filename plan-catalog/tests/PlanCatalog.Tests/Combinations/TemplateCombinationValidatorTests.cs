using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Combinations;

public sealed class TemplateCombinationValidatorTests
{
    [Fact]
    public void ValidFixture_Passes()
    {
        var fixture = new CombinationFixture();
        var snapshot = fixture.BuildSnapshot();

        var result = TemplateCombinationValidator.Validate(fixture.Combination, snapshot);

        Assert.True(result.IsValid, string.Join("; ", result.Issues.Select(i => $"{i.Code}: {i.Message}")));
    }

    [Fact]
    public void MissingLevelModifierToProgressionModifierReference_InvalidatesCombination()
    {
        var fixture = new CombinationFixture();
        var brokenLevelModifier = fixture.LevelModifier with
        {
            ProgressionModifier = new VersionedCatalogReference { DocumentType = "PROGRESSION_MODIFIER", Key = "DOES_NOT_EXIST", Version = 1 }
        };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(brokenLevelModifier)
            .With(fixture.WorkoutProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .With(fixture.Combination)
            .Build();

        var result = TemplateCombinationValidator.Validate(fixture.Combination, snapshot);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "TC_PROGRESSION_MODIFIER_MISSING");
    }

    [Fact]
    public void KeySessionCountExceedingResolvedCap_Fails()
    {
        var fixture = new CombinationFixture();
        var restrictiveProgressionModifier = fixture.ProgressionModifier with { MaximumHardSessionsPerWeek = 0 };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(fixture.WorkoutProgression).With(restrictiveProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .With(fixture.Combination)
            .Build();

        var result = TemplateCombinationValidator.Validate(fixture.Combination, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "TC_KEY_SESSION_COUNT_EXCEEDS_CAP");
    }

    [Fact]
    public void MissingPeakVolumeTuple_Fails()
    {
        var fixture = new CombinationFixture();
        var emptyPeakPolicy = fixture.PeakVolumeBandPolicy with { Entries = [] };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(fixture.WorkoutProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(emptyPeakPolicy).With(fixture.RulePack)
            .With(fixture.Combination)
            .Build();

        var result = TemplateCombinationValidator.Validate(fixture.Combination, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "TC_PEAK_TUPLE_MISSING");
    }

    [Fact]
    public void EmptyEffectiveWorkoutSet_Fails()
    {
        var fixture = new CombinationFixture();
        var restrictedLevelModifier = fixture.LevelModifier with { EligibleWorkoutKeys = new HashSet<string>() };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(restrictedLevelModifier)
            .With(fixture.WorkoutProgression).With(fixture.ProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .With(fixture.Combination)
            .Build();

        var result = TemplateCombinationValidator.Validate(fixture.Combination, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "TC_EFFECTIVE_WORKOUT_SET_EMPTY");
        Assert.Contains(result.Issues, i => i.Code == "TC_STAGE_UNREACHABLE");
    }

    [Fact]
    public void ExperienceMismatchBetweenLevelModifierAndProgressionModifier_Fails()
    {
        var fixture = new CombinationFixture();
        var mismatchedProgressionModifier = fixture.ProgressionModifier with { Experience = PlanCatalog.Contracts.Enums.RunningExperience.Advanced };

        var snapshot = new CatalogSnapshotBuilder()
            .With(fixture.MasterTemplate).With(fixture.Layout).With(fixture.LevelModifier)
            .With(fixture.WorkoutProgression).With(mismatchedProgressionModifier)
            .With(fixture.EasyWorkout).With(fixture.LongRunWorkout).With(fixture.ThresholdWorkout)
            .With(fixture.Registry).With(fixture.PeakVolumeBandPolicy).With(fixture.RulePack)
            .With(fixture.Combination)
            .Build();

        var result = TemplateCombinationValidator.Validate(fixture.Combination, snapshot);

        Assert.Contains(result.Issues, i => i.Code == "TC_PROGRESSION_MODIFIER_EXPERIENCE_MISMATCH");
    }
}
