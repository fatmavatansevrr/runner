using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.Adaptation;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.3 -- Rev3.1 §4.1 Runtime Reason Vocabulary Mapping. Pure logic,
/// no DB: exercises <see cref="RuntimeNotTodayReasonMapper"/> directly.
/// </summary>
public sealed class RuntimeNotTodayReasonMapperTests
{
    [Theory]
    [InlineData("schedule", "ScheduleConflict", "ScheduleConflict")]
    [InlineData("weather", "Weather", "Weather")]
    [InlineData("fatigue", "Tired", "Tired")]
    [InlineData("other", "Other", "Other")]
    public void OperationalRuntimeTokens_MapToExpectedMeaningAndReasonCode(string runtimeReason, string expectedMeaningName, string expectedCodeName)
    {
        var meaning = RuntimeNotTodayReasonMapper.Map(runtimeReason);
        Assert.Equal(expectedMeaningName, meaning.ToString());
        var code = RuntimeNotTodayReasonMapper.ToReasonCode(meaning);
        Assert.Equal(expectedCodeName, code.ToString());
        Assert.Equal(ReasonClass.Operational, ReasonClassificationPolicy.Classify(code));
    }

    [Fact]
    public void Illness_MapsToOperationalClassification_ButBlocksRepair_WithSafetyFlagFalse()
    {
        var meaning = RuntimeNotTodayReasonMapper.Map("illness");
        Assert.Equal(AdaptationReasonMeaning.Illness, meaning);
        var code = RuntimeNotTodayReasonMapper.ToReasonCode(meaning);
        Assert.Equal(NotTodayReasonCode.Illness, code);
        Assert.Equal(ReasonClass.Operational, ReasonClassificationPolicy.Classify(code));
        Assert.True(ReasonClassificationPolicy.BlocksReschedule(code));
        Assert.False(ReasonClassificationPolicy.TriggersSafetyFlag(code));
    }

    [Fact]
    public void Soreness_MapsToSafetyMeaning_BlocksRepair_WithSafetyFlagTrue()
    {
        var meaning = RuntimeNotTodayReasonMapper.Map("soreness");
        Assert.Equal(AdaptationReasonMeaning.Safety, meaning);
        var code = RuntimeNotTodayReasonMapper.ToReasonCode(meaning);
        Assert.Equal(NotTodayReasonCode.PainOrDiscomfort, code);
        Assert.Equal(ReasonClass.Safety, ReasonClassificationPolicy.Classify(code));
        Assert.True(ReasonClassificationPolicy.BlocksReschedule(code));
        Assert.True(ReasonClassificationPolicy.TriggersSafetyFlag(code));
    }

    [Fact]
    public void UnmappedRuntimeToken_IsRejected()
    {
        Assert.Throws<RuntimeNotTodayReasonUnmappedException>(() => RuntimeNotTodayReasonMapper.Map("travel"));
    }

    /// <summary>
    /// Architectural proof, not a string-content check: soreness's meaning
    /// (<see cref="AdaptationReasonMeaning.Safety"/>) is never the literal
    /// enum member name "PainOrDiscomfort" -- the mapping only ever resolves
    /// to that 4M.1 vocabulary member via the separate <see cref="RuntimeNotTodayReasonMapper.ToReasonCode"/>
    /// step, never inside <see cref="RuntimeNotTodayReasonMapper.Map"/> itself.
    /// </summary>
    [Fact]
    public void Soreness_IsNotImplementedAsDirectTokenAliasToPainOrDiscomfort()
    {
        var meaning = RuntimeNotTodayReasonMapper.Map("soreness");
        Assert.NotEqual("PainOrDiscomfort", meaning.ToString());
        Assert.Equal("Safety", meaning.ToString());
    }
}
