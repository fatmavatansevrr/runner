namespace RunningApp.Application.RuntimeCatalog;

/// <summary>
/// Backend-safe summary of how well the current domain/DTO model can
/// represent a Process A plan-catalog candidate's vocabulary. Produced by
/// <see cref="IPlanCatalogDomainMapper"/> from a
/// <see cref="PlanCatalogCandidateSummary"/> (Phase 1 output). Contains no
/// weeks, no days, no dates — it is an analysis artifact for Phase 3
/// planning, not a generation result.
/// </summary>
public sealed class PlanCatalogMappingResult
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }

    /// <summary>Canonical distance family as backend's own <c>GoalDistance</c> enum (already native-supported).</summary>
    public required RunningApp.Domain.Enums.GoalDistance CanonicalDistanceFamily { get; init; }

    /// <summary>
    /// Whether backend currently has a field that can carry a user's exact
    /// requested target distance (km) separately from the fixed,
    /// family-representative distance. False today — see classification for
    /// "RequestedTargetDistanceKm" in <see cref="Classifications"/>.
    /// </summary>
    public required bool RequestedTargetDistanceKmPlaceholderSupported { get; init; }

    public required IReadOnlyList<string> SlotRoles { get; init; }
    public required IReadOnlyList<string> PhaseKeys { get; init; }
    public required IReadOnlyList<PlanCatalogReference> ReferencedWorkouts { get; init; }

    public required bool CanRepresentGoalPaceTenK { get; init; }
    public required bool CanRepresentGoalPaceRehearsalStage { get; init; }
    public required bool CanRepresentKeySession { get; init; }
    public required bool CanRepresentEasySupport { get; init; }
    public required bool CanRepresentLongRun { get; init; }
    public required bool CanRepresentPhaseKeys { get; init; }
    public required bool CanRepresentRuntimeConditionValues { get; init; }

    /// <summary>Every classified concept, evidence and reasoning included.</summary>
    public required IReadOnlyList<CatalogConceptClassification> Classifications { get; init; }

    /// <summary>Concise list of concepts backend currently cannot represent (NotSupported/RequiresNewField/RequiresNewTableOrJson/RequiresEnumExtension), for quick Phase 3 planning reference.</summary>
    public required IReadOnlyList<string> KnownUnsupportedOrMissingRepresentations { get; init; }
}
