using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Models;

public sealed record RuntimeConditionValueSet
{
    public required RuntimeConditionType ConditionType { get; init; }

    public required IReadOnlySet<string> AllowedValues { get; init; }
}
