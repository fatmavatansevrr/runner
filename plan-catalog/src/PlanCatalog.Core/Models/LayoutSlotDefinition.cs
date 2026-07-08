using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Models;

public sealed record LayoutSlotDefinition
{
    public required int SequenceOrder { get; init; }
    public required SlotRole Role { get; init; }
}
