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
/// structurally, not merely empirically.
///
/// PHASE DIST-GEN.6 — generalized (name kept, per DIST-GEN.6's own
/// lower-risk-preferred option) from a single hardcoded 16K boolean check to
/// a registry-resolved lookup via <see cref="Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>:
/// for the approved dark 16K pilot request this still returns the pilot's
/// own frozen [34,46] band (<see cref="TargetDistance16KPeakVolumeBandPolicy"/>);
/// for the approved dark 15K second-target request it returns THAT target's
/// own, independently-declared [34,46] band
/// (<see cref="TargetDistance15KPeakVolumeBandPolicy"/> — numerically
/// identical, a disclosed evidence convergence per DIST-GEN.5 §28/§57, never
/// a shared instance). For every other request (including a canonical
/// HALF_MARATHON request, which loads HM's own real [36,50] catalog band —
/// DIST-GEN.1 §20) this delegates unchanged to the real loader.
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
        var darkCell = Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(_targetDistanceKmOverride, _requestGoalDistance, _level, _daysPerWeek);
        // PHASE DIST-GEN.13 -- the ONE typed resolution boundary replacing the
        // former three sequential per-target return branches. Each target's own
        // independently-declared TargetDistanceNNKPeakVolumeBandPolicy still
        // owns and builds its own band; only the SELECTION is now an exact-cell
        // lookup. Fail-closed: a cell without exactly one registered authority
        // resolves to null and delegates unchanged to the real catalog-backed
        // loader below -- never a defaulted or numerically-close band.
        var projectedTargetAuthority = ProjectedTargetAuthorityRegistry.TryResolve(darkCell);
        if (projectedTargetAuthority is { } projected)
        {
            return Task.FromResult(projected.BuildPeakVolumeBand(distanceFamily, experience, runsPerWeek, reference.Key, reference.Version));
        }

        return _inner.LoadAsync(reference, distanceFamily, experience, runsPerWeek, ct);
    }
}
