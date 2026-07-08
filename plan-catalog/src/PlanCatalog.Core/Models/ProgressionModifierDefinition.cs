using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Level-specific complexity and dosage. The sole owner of MaximumHardSessionsPerWeek — see brief §7.7.
/// Does not contain distance-specific stage order.
/// </summary>
public sealed record ProgressionModifierDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required RunningExperience Experience { get; init; }

    public required int MaximumComplexityTier { get; init; }
    public required int MaximumHardSessionsPerWeek { get; init; }

    public required decimal MainSetDoseMultiplier { get; init; }

    public required bool AllowGoalPaceRehearsal { get; init; }
    public required bool AllowSecondHardStimulus { get; init; }
}
