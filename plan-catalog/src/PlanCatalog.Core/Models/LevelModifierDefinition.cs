using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Metadata;
using PlanCatalog.Contracts.References;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Level-specific eligible-workout scope. Complexity/dosage/hard-session-cap live exclusively in the
/// referenced <see cref="ProgressionModifierDefinition"/> — see brief §7.3. Deliberately does NOT contain
/// ProgressionProfileKey, PeakVolumeBandKey, or MaximumHardSessionsPerWeek.
/// </summary>
public sealed record LevelModifierDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required RunningExperience Experience { get; init; }

    /// <summary>Legacy (schemaVersion 1) unversioned eligible-workout keys. Null for schemaVersion >= 2.</summary>
    public IReadOnlySet<string>? EligibleWorkoutKeys { get; init; }

    /// <summary>Exact versioned eligible workouts (schemaVersion >= 2). Null for schemaVersion 1.</summary>
    public IReadOnlyList<VersionedCatalogReference>? EligibleWorkouts { get; init; }

    public required VersionedCatalogReference ProgressionModifier { get; init; }
}
