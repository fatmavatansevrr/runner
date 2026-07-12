using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Metadata;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Level-specific complexity and dosage. The sole owner of MaximumHardSessionsPerWeek — see brief §7.7.
/// Does not contain distance-specific stage order.
///
/// MaximumHardSessionsPerWeek is a ceiling, not a target or minimum: valid weeks may contain zero hard
/// sessions (deload/taper/readiness), and no consumer may infer that exactly this many must be scheduled.
/// AllowSecondHardStimulus and MaximumHardSessionsPerWeek are approved (Wave 5 / D2) only for the
/// TEN_K / INTERMEDIATE / 4-runs-per-week pilot scope realized by this artifact's sole current referrer
/// (INTERMEDIATE_MODIFIER); they must not be silently assumed to generalize to other day-counts or
/// distances sharing the INTERMEDIATE experience label without independent evidence.
/// </summary>
public sealed record ProgressionModifierDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required RunningExperience Experience { get; init; }

    /// <summary>Legacy (schemaVersion 1) only. Removed in schemaVersion 2+ — see brief §Wave 5 D2; no replacement taxonomy was introduced.</summary>
    public int? MaximumComplexityTier { get; init; }
    public required int MaximumHardSessionsPerWeek { get; init; }

    public required decimal MainSetDoseMultiplier { get; init; }

    public required bool AllowGoalPaceRehearsal { get; init; }
    public required bool AllowSecondHardStimulus { get; init; }
}
