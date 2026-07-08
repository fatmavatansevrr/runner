using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Models;

public sealed record PhaseDefinition
{
    public required PhaseKey PhaseKey { get; init; }
    public required int MinimumWeeks { get; init; }
    public required int PreferredWeeks { get; init; }
    public required int MaximumWeeks { get; init; }

    public required IReadOnlyList<PhaseIntent> Intents { get; init; }
    public required IReadOnlyList<WorkoutFamily> EligibleWorkoutFamilies { get; init; }

    public required int CompressionPriority { get; init; }
    public required int ExtensionPriority { get; init; }
    public required bool IsCompressionProtected { get; init; }
}
