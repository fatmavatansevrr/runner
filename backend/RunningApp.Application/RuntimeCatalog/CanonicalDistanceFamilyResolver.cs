using RunningApp.Application.Exceptions;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog;

/// <summary>
/// Result of resolving a user's requested target distance to an internal
/// canonical distance family. The two values are deliberately kept separate:
/// <see cref="CanonicalDistanceFamily"/> selects which Process A catalog
/// family (FIVE_K/TEN_K/HALF_MARATHON/MARATHON) to consult, while
/// <see cref="RequestedTargetDistanceKm"/> preserves exactly what the user
/// asked for, unrounded and unmodified, for user-facing display.
/// </summary>
public sealed class DistanceFamilyResolution
{
    public required GoalDistance CanonicalDistanceFamily { get; init; }
    public required double RequestedTargetDistanceKm { get; init; }
}

/// <summary>
/// Process B domain service: maps a requested custom target distance (km) to
/// the internal canonical distance family Process A catalogs are organized
/// around, without ever rewriting or rounding the user-facing requested
/// distance. See <see cref="CanonicalDistanceFamilyResolver"/> for the
/// mapping thresholds. This is NOT a fallback/substitution mechanism — the
/// user's requested goal is never changed; only which catalog family backs
/// it internally is selected.
/// </summary>
public interface ICanonicalDistanceFamilyResolver
{
    DistanceFamilyResolution Resolve(double requestedDistanceKm);
}

/// <inheritdoc cref="ICanonicalDistanceFamilyResolver"/>
public sealed class CanonicalDistanceFamilyResolver : ICanonicalDistanceFamilyResolver
{
    private const double FiveKUpperBoundKm = 5.0;
    private const double TenKUpperBoundKm = 10.0;
    private const double HalfMarathonUpperBoundKm = 21.1;
    private const double MarathonUpperBoundKm = 42.2;

    public DistanceFamilyResolution Resolve(double requestedDistanceKm)
    {
        if (requestedDistanceKm <= 0)
        {
            throw new UnsupportedTargetDistanceException(
                $"requestedDistanceKm must be a positive number, but was {requestedDistanceKm}.");
        }

        if (requestedDistanceKm > MarathonUpperBoundKm)
        {
            throw new UnsupportedTargetDistanceException(
                $"requestedDistanceKm {requestedDistanceKm} km exceeds the supported range " +
                $"(0, {MarathonUpperBoundKm}] km. No canonical distance family covers this request.");
        }

        // Deliberately does NOT round or substitute requestedDistanceKm anywhere below —
        // only the internal family selection depends on these thresholds.
        var family = requestedDistanceKm switch
        {
            <= FiveKUpperBoundKm => GoalDistance.FiveK,
            <= TenKUpperBoundKm => GoalDistance.TenK,
            <= HalfMarathonUpperBoundKm => GoalDistance.HalfMarathon,
            <= MarathonUpperBoundKm => GoalDistance.Marathon,
            _ => throw new UnsupportedTargetDistanceException(
                $"requestedDistanceKm {requestedDistanceKm} km did not match any canonical distance family band."),
        };

        return new DistanceFamilyResolution
        {
            CanonicalDistanceFamily = family,
            RequestedTargetDistanceKm = requestedDistanceKm,
        };
    }
}
