using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Models;

public sealed record PhaseWorkoutProgressionDefinition
{
    public required PhaseKey PhaseKey { get; init; }

    public required IReadOnlyList<WorkoutProgressionStageDefinition> Stages { get; init; }
}
