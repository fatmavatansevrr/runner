using PlanCatalog.Contracts.Enums;

namespace PlanCatalog.Core.Models;

public sealed record PeakVolumeBandEntry
{
    public required DistanceFamily DistanceFamily { get; init; }
    public required RunningExperience Experience { get; init; }
    public required int RunsPerWeek { get; init; }

    public required decimal MinimumKm { get; init; }
    public required decimal MaximumKm { get; init; }
}
