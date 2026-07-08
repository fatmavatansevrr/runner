using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

public static class RunLayoutValidator
{
    private static readonly IReadOnlySet<int> SupportedProductRunsPerWeek = new HashSet<int> { 2, 3, 4, 5 };

    public static ValidationResult Validate(RunLayoutDefinition layout)
    {
        var issues = new List<ValidationIssue>();

        if (!SupportedProductRunsPerWeek.Contains(layout.RunsPerWeek))
        {
            issues.Add(new ValidationIssue("RL_RUNS_PER_WEEK_OUT_OF_RANGE", ValidationSeverity.Error,
                $"RunsPerWeek {layout.RunsPerWeek} is outside the supported product range (2-5).", "$.runsPerWeek"));
        }

        if (layout.Slots.Count != layout.RunsPerWeek)
        {
            issues.Add(new ValidationIssue("RL_SLOT_COUNT_MISMATCH", ValidationSeverity.Error,
                $"Slot count {layout.Slots.Count} does not equal RunsPerWeek {layout.RunsPerWeek}.", "$.slots"));
        }

        var sequenceOrders = layout.Slots.Select(s => s.SequenceOrder).OrderBy(x => x).ToList();
        var expected = Enumerable.Range(1, layout.Slots.Count).ToList();
        if (sequenceOrders.Distinct().Count() != sequenceOrders.Count || !sequenceOrders.SequenceEqual(expected))
        {
            issues.Add(new ValidationIssue("RL_SEQUENCE_ORDER_NOT_CONTIGUOUS", ValidationSeverity.Error,
                "SequenceOrder values must be unique and contiguous starting at 1.", "$.slots"));
        }

        var longRunCount = layout.Slots.Count(s => s.Role == SlotRole.LongRun);
        if (longRunCount != 1)
        {
            issues.Add(new ValidationIssue("RL_LONG_RUN_COUNT_INVALID", ValidationSeverity.Error,
                $"Exactly one LONG_RUN slot is required; found {longRunCount}.", "$.slots"));
        }

        var keySessionCount = layout.Slots.Count(s => s.Role == SlotRole.KeySession);
        if (keySessionCount is < 0 or > 2)
        {
            issues.Add(new ValidationIssue("RL_KEY_SESSION_COUNT_OUT_OF_RANGE", ValidationSeverity.Error,
                $"KEY_SESSION count must be between 0 and 2; found {keySessionCount}.", "$.slots"));
        }

        return new ValidationResult(issues);
    }
}
