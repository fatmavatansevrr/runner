using PlanCatalog.Contracts.Enums;
using PlanCatalog.Contracts.References;

namespace PlanCatalog.Core.Models;

/// <summary>
/// Phase-relative stage order only. Deliberately excludes week numbers, calendar dates, actual phase
/// duration, user-specific values, and runtime resolver results — see brief §7.4/§9.
///
/// Schema-version-exclusive candidate shape (see artifacts/audits/exact-workout-reference-migration.md):
/// schemaVersion 1 documents populate <see cref="WorkoutCandidateKeys"/> (legacy, unversioned) and leave
/// <see cref="WorkoutCandidates"/> null; schemaVersion 2+ documents populate <see cref="WorkoutCandidates"/>
/// (exact key+version) and leave <see cref="WorkoutCandidateKeys"/> null. Never both — enforced by
/// <see cref="Validation.SchemaVersionShapeValidator"/> at source-integrity time, not by this type.
/// </summary>
public sealed record WorkoutProgressionStageDefinition
{
    public required string StageKey { get; init; }
    public required int RelativeOrder { get; init; }

    /// <summary>Legacy (schemaVersion 1) unversioned candidate keys. Null for schemaVersion >= 2.</summary>
    public IReadOnlyList<string>? WorkoutCandidateKeys { get; init; }

    /// <summary>Exact versioned candidates (schemaVersion >= 2). Null for schemaVersion 1.</summary>
    public IReadOnlyList<VersionedCatalogReference>? WorkoutCandidates { get; init; }

    public required int MinimumExposures { get; init; }
    public required int MaximumExposures { get; init; }

    public required StageCompressionBehavior CompressionBehavior { get; init; }
    public required StageExtensionBehavior ExtensionBehavior { get; init; }

    public required IReadOnlyList<RuntimeEligibilityCondition> Requires { get; init; }

    public string? FallbackStageKey { get; init; }
}
