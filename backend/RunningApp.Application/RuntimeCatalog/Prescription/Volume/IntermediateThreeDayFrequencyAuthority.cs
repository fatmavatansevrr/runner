using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.16 — the single, explicit implementation owner of the two
/// <c>FREQUENCY_ONLY</c> fields DIST-GEN.15 §26-27/§53 classified as
/// frequency-owned authority at <b>Intermediate × 3 runs/week</b>:
/// <see cref="AbsoluteWeeklyIncreaseCapKm"/> (2.0, confirmed by direct,
/// exact agreement between canonical TEN_K's own <c>ThreeDayIntermediate</c>
/// and canonical HALF_MARATHON's own <c>HalfMarathonIntermediate3D</c>) and
/// <see cref="StartingAbsoluteLongRunCapKm"/> (null, consistent with the
/// already-established fact that this field is non-null only at 5D/6D).
///
/// <b>This component freezes no new number and creates no new authority.</b>
/// Both values are already-decided, already-shipped values, moved here from
/// what would otherwise be duplicated per-instance literals, mirroring
/// exactly how <see cref="IntermediateFourDayGrowthAuthority"/> owns the
/// analogous 4D fields.
///
/// <b>Deliberately narrow — this is NOT a 3D twin of
/// <see cref="IntermediateFourDayGrowthAuthority"/>.</b> It does NOT bundle:
/// <list type="bullet">
/// <item>The long-run share quadruple — DIST-GEN.15B's own governing
/// authority table classifies LR-share at Intermediate×3D as
/// <c>PARENT_FAMILY_ONLY</c> (HALF_MARATHON's own 0.34/0.40/0.38/0.46,
/// DIST-GEN.15 §30), never frequency-owned. Bundling it here would
/// misrepresent that ownership. No shared 3D LR-share component exists in
/// this codebase today (canonical <c>HalfMarathonIntermediate3D</c> declares
/// its own quadruple as a literal), so each 3D consumer keeps its own
/// literal declaration of the parent-family value.</item>
/// <item><c>PreferredWeeklyIncreaseRate</c>/<c>HardWeeklyIncreaseRate</c> —
/// <c>GENERIC</c>, universal, identical literals at every anchor and every
/// frequency; not frequency-scoped authority, so not owned here.</item>
/// <item>Any exact-cell peak-volume/selected-peak/calibration field — these
/// are <c>NEW_TARGET_FREQUENCY_CELL_AUTHORITY</c> (DIST-GEN.15B), owned
/// solely by each individual target×3D cell's own declared policy (see
/// <see cref="TargetDistance16KIntermediate3DPeakVolumeBandPolicy"/> and
/// 16K's own named <see cref="VolumeSafetyPolicy"/> instance for its own
/// 28/40/34/20 values).</item>
/// </list>
///
/// <b>Frequency-keyed, not merely level-keyed</b> — mirrors
/// <see cref="IntermediateFourDayGrowthAuthority"/>'s own established
/// discipline: HALF_MARATHON × Intermediate × 4D genuinely carries a
/// DIFFERENT absolute weekly cap (2.5) and IS NOT a consumer of this
/// component and never may become one without its own governance phase.
///
/// Today's only consumer is <c>16K × Intermediate × 3D</c>
/// (<see cref="VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K"/>)
/// — the first, and so far only, projected-distance target at this
/// frequency. Canonical HM I3D (<see cref="VolumeSafetyPolicy.HalfMarathonIntermediate3D"/>)
/// and canonical TEN_K I3D (<see cref="VolumeSafetyPolicy.ThreeDayIntermediate"/>)
/// keep their own independent literal declarations of these same values,
/// completely untouched by this component — exactly mirroring how
/// <see cref="IntermediateFourDayGrowthAuthority"/> never became a consumer
/// of canonical TEN_K's or HALF_MARATHON's own 4D instances.
/// </summary>
internal static class IntermediateThreeDayFrequencyAuthority
{
    /// <summary>The level half of this component's own scope key.</summary>
    public const RunningBackground ScopeLevel = RunningBackground.Intermediate;

    /// <summary>The frequency half of this component's own scope key — 3 runs/week, never 4D/5D/6D.</summary>
    public const int ScopeRunsPerWeek = 3;

    /// <summary>DIST-GEN.15 §26 <c>FREQUENCY_ONLY</c> — absolute per-week volume-increase cap in km. Confirmed by exact cross-family agreement (TEN_K and HALF_MARATHON both 2.0 at Intermediate×3D); genuinely 2.5 at Intermediate×4D, which is NOT in this component's scope.</summary>
    public const double AbsoluteWeeklyIncreaseCapKm = 2.0d;

    /// <summary>DIST-GEN.15 §27 <c>FREQUENCY_ONLY</c> — the first-Core-week absolute long-run ceiling is null at 3D for both canonical families, consistent with it being non-null only at 5D/6D. A cap is never a default and never a readiness substitute.</summary>
    public static readonly double? StartingAbsoluteLongRunCapKm = null;
}
