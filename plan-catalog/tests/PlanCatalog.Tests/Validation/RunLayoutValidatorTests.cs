using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;
using PlanCatalog.Core.Validation;
using PlanCatalog.Tests.TestSupport;
using Xunit;

namespace PlanCatalog.Tests.Validation;

public sealed class RunLayoutValidatorTests
{
    private static RunLayoutDefinition ValidFourDayLayout() => new()
    {
        Metadata = Meta.Of("RUN_LAYOUT", "RUN_LAYOUT_4D"),
        RunsPerWeek = 4,
        Slots =
        [
            new LayoutSlotDefinition { SequenceOrder = 1, Role = SlotRole.KeySession },
            new LayoutSlotDefinition { SequenceOrder = 2, Role = SlotRole.EasySupport },
            new LayoutSlotDefinition { SequenceOrder = 3, Role = SlotRole.EasySupport },
            new LayoutSlotDefinition { SequenceOrder = 4, Role = SlotRole.LongRun }
        ]
    };

    [Fact]
    public void ValidFourDayLayout_Passes()
    {
        var result = RunLayoutValidator.Validate(ValidFourDayLayout());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void MissingLongRun_Fails()
    {
        var layout = ValidFourDayLayout() with
        {
            Slots =
            [
                new LayoutSlotDefinition { SequenceOrder = 1, Role = SlotRole.KeySession },
                new LayoutSlotDefinition { SequenceOrder = 2, Role = SlotRole.EasySupport },
                new LayoutSlotDefinition { SequenceOrder = 3, Role = SlotRole.EasySupport },
                new LayoutSlotDefinition { SequenceOrder = 4, Role = SlotRole.EasySupport }
            ]
        };

        var result = RunLayoutValidator.Validate(layout);
        Assert.Contains(result.Issues, i => i.Code == "RL_LONG_RUN_COUNT_INVALID");
    }

    [Fact]
    public void SlotCountMismatch_Fails()
    {
        var layout = ValidFourDayLayout() with { RunsPerWeek = 5 };
        var result = RunLayoutValidator.Validate(layout);
        Assert.Contains(result.Issues, i => i.Code == "RL_SLOT_COUNT_MISMATCH");
    }

    [Fact]
    public void NonContiguousSequence_Fails()
    {
        var layout = ValidFourDayLayout() with
        {
            Slots =
            [
                new LayoutSlotDefinition { SequenceOrder = 1, Role = SlotRole.KeySession },
                new LayoutSlotDefinition { SequenceOrder = 2, Role = SlotRole.EasySupport },
                new LayoutSlotDefinition { SequenceOrder = 2, Role = SlotRole.EasySupport },
                new LayoutSlotDefinition { SequenceOrder = 4, Role = SlotRole.LongRun }
            ]
        };

        var result = RunLayoutValidator.Validate(layout);
        Assert.Contains(result.Issues, i => i.Code == "RL_SEQUENCE_ORDER_NOT_CONTIGUOUS");
    }

    [Fact]
    public void RunsPerWeekOutOfRange_Fails()
    {
        var layout = ValidFourDayLayout() with { RunsPerWeek = 7 };
        var result = RunLayoutValidator.Validate(layout);
        Assert.Contains(result.Issues, i => i.Code == "RL_RUNS_PER_WEEK_OUT_OF_RANGE");
    }
}
