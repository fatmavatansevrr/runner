using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Core.Validation;

/// <summary>Which source produced an <see cref="EffectiveDistanceAccountingCapability"/> result.</summary>
public enum EffectiveDistanceAccountingCapabilitySource
{
    /// <summary>The referenced WorkoutDefinition explicitly declares AllowedDistanceAccountingModes.</summary>
    ExplicitWorkoutMetadata,

    /// <summary>The WorkoutDefinition declares nothing; a matching exact-version capability overlay supplied it.</summary>
    CapabilityOverlay,

    /// <summary>Both the WorkoutDefinition and an overlay declare a value for the same exact reference — never resolved by precedence.</summary>
    Conflict,

    /// <summary>Neither the WorkoutDefinition nor any overlay declares a value.</summary>
    Unresolved
}

/// <summary>One canonical, single-seam resolution result — never scattered across validators/tests/projector.</summary>
public sealed record EffectiveDistanceAccountingCapability
{
    public required EffectiveDistanceAccountingCapabilitySource Source { get; init; }
    public IReadOnlyList<DistanceAccountingMode>? AllowedModes { get; init; }

    public static EffectiveDistanceAccountingCapability FromExplicit(IReadOnlyList<DistanceAccountingMode> modes) =>
        new() { Source = EffectiveDistanceAccountingCapabilitySource.ExplicitWorkoutMetadata, AllowedModes = modes };

    public static EffectiveDistanceAccountingCapability FromOverlay(IReadOnlyList<DistanceAccountingMode> modes) =>
        new() { Source = EffectiveDistanceAccountingCapabilitySource.CapabilityOverlay, AllowedModes = modes };

    public static readonly EffectiveDistanceAccountingCapability Conflict =
        new() { Source = EffectiveDistanceAccountingCapabilitySource.Conflict };

    public static readonly EffectiveDistanceAccountingCapability Unresolved =
        new() { Source = EffectiveDistanceAccountingCapabilitySource.Unresolved };
}

/// <summary>
/// Phase 10K-FREQ.6D.4C.2 — the single canonical seam (FREQ.6D.4C.1 §17) resolving effective
/// distance-accounting capability for a WorkoutDefinition, given an optional exact-version overlay
/// (FREQ.6D.4C.1's M3). An explicitly-declared WorkoutDefinition value always wins when present alone;
/// an overlay only ever fills a genuine absence; both present simultaneously is a fail-closed conflict,
/// never resolved by an implicit precedence rule (EXPLICIT_DEFINITION_METADATA_CANNOT_BE_OVERRIDDEN).
/// </summary>
public static class WorkoutCapabilityResolver
{
    public static EffectiveDistanceAccountingCapability ResolveDistanceAccountingCapability(
        WorkoutDefinition workout,
        WorkoutDefinitionCapabilityOverlay? overlay)
    {
        var hasExplicit = workout.AllowedDistanceAccountingModes is not null;
        var hasOverlay = overlay is not null;

        if (hasExplicit && hasOverlay)
        {
            return EffectiveDistanceAccountingCapability.Conflict;
        }

        if (hasExplicit)
        {
            return EffectiveDistanceAccountingCapability.FromExplicit(workout.AllowedDistanceAccountingModes!);
        }

        if (hasOverlay)
        {
            return EffectiveDistanceAccountingCapability.FromOverlay(overlay!.AllowedDistanceAccountingModes);
        }

        return EffectiveDistanceAccountingCapability.Unresolved;
    }
}
