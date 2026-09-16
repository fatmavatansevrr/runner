using RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.2 — decorates the real, unmodified <see cref="ICatalogPeakVolumeBandLoader"/>
/// (the catalog-JSON-backed loader every canonical TEN_K/HALF_MARATHON request
/// already uses) so both of <see cref="RunningApp.Application.RuntimeCatalog.PreviewRouting.CatalogPreviewGenerator"/>'s
/// generation paths (the exact-preferred-core static path AND the
/// CompressedCore/ExtendedCore dynamic-core path) consult the SAME single
/// substitution decision. For every canonical request (no
/// <see cref="RunningApp.Application.DTOs.Plan.GeneratePreviewRequest.TargetDistanceKmOverride"/>)
/// this delegates unchanged to the real loader — byte-identical,
/// structurally, not merely empirically. Only for the one approved dark 16K
/// pilot request does it return the pilot's own frozen [34,46] band
/// (<see cref="TargetDistance16KPeakVolumeBandPolicy"/>) instead of the real
/// catalog entry (which, for the reused HALF_MARATHON__4D__INTERMEDIATE
/// identity, is HM's own real [36,50] band — DIST-GEN.1 §20 — not this
/// pilot's own authority, DIST-GEN.1 §22).
/// </summary>
internal sealed class TargetDistance16KAwarePeakVolumeBandLoader : ICatalogPeakVolumeBandLoader
{
    private readonly ICatalogPeakVolumeBandLoader _inner;
    private readonly double? _targetDistanceKmOverride;
    private readonly GoalDistance _requestGoalDistance;
    private readonly RunningBackground _level;
    private readonly int _daysPerWeek;

    public TargetDistance16KAwarePeakVolumeBandLoader(
        ICatalogPeakVolumeBandLoader inner,
        double? targetDistanceKmOverride,
        GoalDistance requestGoalDistance,
        RunningBackground level,
        int daysPerWeek)
    {
        _inner = inner;
        _targetDistanceKmOverride = targetDistanceKmOverride;
        _requestGoalDistance = requestGoalDistance;
        _level = level;
        _daysPerWeek = daysPerWeek;
    }

    public Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default)
    {
        if (Dark16KPilotEligibilityPolicy.IsEligible(_targetDistanceKmOverride, _requestGoalDistance, _level, _daysPerWeek))
        {
            return Task.FromResult(TargetDistance16KPeakVolumeBandPolicy.Build(distanceFamily, experience, runsPerWeek, reference.Key, reference.Version));
        }

        return _inner.LoadAsync(reference, distanceFamily, experience, runsPerWeek, ct);
    }
}
