using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DIST-GEN.13 — the ONE typed resolution boundary mapping an
/// already-resolved <see cref="Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell"/>
/// to its single <see cref="ProjectedTargetAuthority"/>. This retires the four
/// hand-written per-target dispatch cascades DIST-GEN.12 §11 identified and
/// which DIST-GEN.9 §62, DIST-GEN.10 §57 and DIST-GEN.11 §89 each deferred in
/// turn.
///
/// <b>Strictly downstream of eligibility, never a substitute for it.</b> This
/// registry answers only "given a cell the dark gate ALREADY approved, which
/// training authority does that cell own?" It never decides whether a cell is
/// eligible:
/// <see cref="Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>
/// remains the sole dark-eligibility authority, and the separately-curated
/// public gate (<see cref="Dark16KPilotEligibilityPolicy.ApprovedPublicCells"/>,
/// consumed only by <c>GeneratePreviewCommandMapper</c>) is untouched by, and
/// unreachable from, this type.
///
/// <b>Fail-closed by construction.</b>
/// <list type="bullet">
/// <item>Lookup is exact-cell dictionary equality over the full
/// <c>(TargetDistanceKm, ParentDistanceFamily, Level, RunsPerWeek)</c>
/// quadruple. There is no distance interval, no proximity match, no fitted or
/// derived value, no rounding and no partial key anywhere in this type.</item>
/// <item>An unregistered cell — or a null cell — resolves to <c>null</c>. It
/// never yields a defaulted or partial authority, never picks a numerically
/// close sibling, and never throws a <c>KeyNotFoundException</c>. Every
/// consumer treats a null authority exactly as it treats "not an approved
/// projected target" today, so the existing unsupported-target behaviour
/// (exception type, message text and reason code) is reached unchanged.</item>
/// <item>A duplicate registration for the same cell fails loudly at type
/// initialisation rather than silently resolving to whichever entry happened
/// to come first — see <see cref="BuildIndex"/>.</item>
/// </list>
///
/// <b>Exactly three registrations, one per approved dark cell</b> — 15.0, 16.0
/// and 18.0, all HALF_MARATHON × Intermediate × 4D, matching
/// <see cref="Dark16KPilotEligibilityPolicy.ApprovedDarkCells"/> one-for-one.
/// Adding a future fourth target is deliberately one registration here plus
/// that target's own authority files, never a fourth hand-edited branch at four
/// separate call sites. This phase adds no target and no registration.
/// </summary>
internal static class ProjectedTargetAuthorityRegistry
{
    private static readonly IReadOnlyDictionary<Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell, ProjectedTargetAuthority> ByCell =
        BuildIndex(Registrations());

    /// <summary>Every registered authority. Consumed by governance tests; carries no runtime dispatch meaning of its own.</summary>
    public static IReadOnlyCollection<ProjectedTargetAuthority> All => (IReadOnlyCollection<ProjectedTargetAuthority>)ByCell.Values;

    /// <summary>
    /// Exact-cell lookup. Returns <c>null</c> for a null cell and for any cell
    /// that does not have exactly one registration — never a partial, defaulted
    /// or numerically-close match.
    /// </summary>
    public static ProjectedTargetAuthority? TryResolve(Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell? cell) =>
        cell is not null && ByCell.TryGetValue(cell, out var authority) ? authority : null;

    /// <summary>
    /// The complete, explicit registration set — one entry per approved dark
    /// cell, each composed only of references to values that remain declared by
    /// their own owners.
    /// </summary>
    private static IReadOnlyList<ProjectedTargetAuthority> Registrations() =>
    [
        // DIST-GEN.5/6 -- 15.0km. Its own exact-target peak long run is 14.5km.
        new()
        {
            Cell = new(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4),
            PilotLabel = "dark 15K",
            MinimumCoreWeeks = TargetDistance15KHorizonPolicy.MinimumCoreWeeks,
            PreferredCoreWeeks = TargetDistance15KHorizonPolicy.PreferredCoreWeeks,
            MaximumCoreWeeks = TargetDistance15KHorizonPolicy.MaximumCoreWeeks,
            DecideHorizon = TargetDistance15KHorizonPolicy.Decide,
            ClassifyHorizon = TargetDistance15KHorizonPolicy.Classify,
            GetUnsupportedReasonCode = TargetDistance15KHorizonPolicy.GetUnsupportedReasonCode,
            VolumeSafetyPolicy = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K,
            BuildPeakVolumeBand = TargetDistance15KPeakVolumeBandPolicy.Build,
            PeakVolumeBandMinimumKm = TargetDistance15KPeakVolumeBandPolicy.MinimumKm,
            PeakVolumeBandMaximumKm = TargetDistance15KPeakVolumeBandPolicy.MaximumKm,
        },
        // DIST-GEN.1/2/2A/2B -- 16.0km. Its own exact-target peak long run is 15.0km.
        new()
        {
            Cell = new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4),
            PilotLabel = "dark 16K",
            MinimumCoreWeeks = TargetDistance16KHorizonPolicy.MinimumCoreWeeks,
            PreferredCoreWeeks = TargetDistance16KHorizonPolicy.PreferredCoreWeeks,
            MaximumCoreWeeks = TargetDistance16KHorizonPolicy.MaximumCoreWeeks,
            DecideHorizon = TargetDistance16KHorizonPolicy.Decide,
            ClassifyHorizon = TargetDistance16KHorizonPolicy.Classify,
            GetUnsupportedReasonCode = TargetDistance16KHorizonPolicy.GetUnsupportedReasonCode,
            VolumeSafetyPolicy = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K,
            BuildPeakVolumeBand = TargetDistance16KPeakVolumeBandPolicy.Build,
            PeakVolumeBandMinimumKm = TargetDistance16KPeakVolumeBandPolicy.MinimumKm,
            PeakVolumeBandMaximumKm = TargetDistance16KPeakVolumeBandPolicy.MaximumKm,
        },
        // DIST-GEN.9/10 -- 18.0km. Its own exact-target peak long run is 16.0km.
        new()
        {
            Cell = new(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4),
            PilotLabel = "dark 18K",
            MinimumCoreWeeks = TargetDistance18KHorizonPolicy.MinimumCoreWeeks,
            PreferredCoreWeeks = TargetDistance18KHorizonPolicy.PreferredCoreWeeks,
            MaximumCoreWeeks = TargetDistance18KHorizonPolicy.MaximumCoreWeeks,
            DecideHorizon = TargetDistance18KHorizonPolicy.Decide,
            ClassifyHorizon = TargetDistance18KHorizonPolicy.Classify,
            GetUnsupportedReasonCode = TargetDistance18KHorizonPolicy.GetUnsupportedReasonCode,
            VolumeSafetyPolicy = VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K,
            BuildPeakVolumeBand = TargetDistance18KPeakVolumeBandPolicy.Build,
            PeakVolumeBandMinimumKm = TargetDistance18KPeakVolumeBandPolicy.MinimumKm,
            PeakVolumeBandMaximumKm = TargetDistance18KPeakVolumeBandPolicy.MaximumKm,
        },
    ];

    /// <summary>
    /// Builds the exact-cell index, refusing a duplicate registration loudly
    /// rather than silently keeping one of them. Internal (not private) solely
    /// so the DIST-GEN.13 governance tests can exercise the duplicate-refusal
    /// path directly — see InternalsVisibleTo in RunningApp.Application.csproj.
    /// No production behaviour change.
    /// </summary>
    internal static IReadOnlyDictionary<Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell, ProjectedTargetAuthority> BuildIndex(
        IReadOnlyList<ProjectedTargetAuthority> registrations)
    {
        var byCell = new Dictionary<Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell, ProjectedTargetAuthority>();
        foreach (var registration in registrations)
        {
            if (!byCell.TryAdd(registration.Cell, registration))
            {
                throw new InvalidOperationException(
                    $"DIST-GEN.13 projected-target authority registry is misconfigured: cell {registration.Cell} is registered more than once. " +
                    "Exactly one authority per approved dark cell is required; a duplicate must never be silently resolved to whichever entry happens to come first.");
            }
        }

        return byCell;
    }
}
