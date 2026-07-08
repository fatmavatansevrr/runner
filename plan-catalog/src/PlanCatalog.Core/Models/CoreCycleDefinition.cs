namespace PlanCatalog.Core.Models;

public sealed record CoreCycleDefinition
{
    public required int MinimumWeeks { get; init; }
    public required int DefaultWeeks { get; init; }
    public required int MaximumWeeks { get; init; }
}
