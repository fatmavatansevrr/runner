using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Structural component of a workout (e.g. warm-up, main set). Carries a stable intensity descriptor
/// key, never a user's actual pace, distance, or repetition count — see brief §7.8.
/// </summary>
public sealed record WorkoutComponentDefinition
{
    public required int SequenceOrder { get; init; }
    public required WorkoutComponentType ComponentType { get; init; }
    public required string IntensityDescriptor { get; init; }
}
