using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

/// <summary>
/// PHASE DIST-GEN.13 — the single, explicit implementation owner of the
/// <c>SHARED_LEVEL_FREQUENCY_AUTHORITY</c> fields DIST-GEN.8 §7 already closed
/// and DIST-GEN.12 §53 listed as <c>NORMALIZATION_READY</c>, scoped narrowly and
/// deliberately to <b>INTERMEDIATE × 4 runs/week</b> — never to a level, never to
/// a frequency, and never to a distance this component does not name.
///
/// <b>This component freezes no new number and creates no new authority.</b>
/// Every value below is an already-decided, already-shipped value moved from a
/// duplicated per-instance literal to the one place that actually owns it. The
/// governance source is DIST-GEN.8 §7 (consumed unchanged by DIST-GEN.12 §4/§7/§53):
/// these fields are identical across every Intermediate × 4D anchor in this
/// engagement — canonical TEN_K (<see cref="VolumeSafetyPolicy.Default"/>),
/// canonical HALF_MARATHON (<see cref="VolumeSafetyPolicy.HalfMarathonIntermediate4D"/>),
/// and the three projected target distances (15K/16K/18K) — which is precisely
/// why they cannot be distance-scoped: TEN_K's inclusion could not otherwise be
/// explained (DIST-GEN.12 §56).
///
/// <b>Frequency-keyed, not merely level-keyed.</b> The frequency half of the key
/// is load-bearing and must never be dropped: HALF_MARATHON × Intermediate × 6D
/// genuinely carries a DIFFERENT long-run share quadruple (0.28/0.36/0.28/0.36)
/// and a genuinely non-null <see cref="StartingAbsoluteLongRunCapKm"/> (12.0),
/// and HALF_MARATHON × Intermediate × 3D genuinely carries both a different
/// quadruple (0.34/0.40/0.38/0.46) and a different absolute weekly cap (2.0).
/// Neither is a consumer of this component and neither may ever become one
/// without its own governance phase. Level is equally load-bearing: Beginner and
/// Advanced cells carry their own independently-frozen values.
///
/// Consumers are named explicitly, one by one — there is no "apply to everything
/// that looks like 4D" mechanism here, and this type deliberately exposes only
/// constants, never a resolver, a lookup, a band, or a fallback.
/// </summary>
internal static class IntermediateFourDayGrowthAuthority
{
    /// <summary>The level half of this component's own scope key. Declared so the scope is readable in code, not only in a doc comment.</summary>
    public const RunningBackground ScopeLevel = RunningBackground.Intermediate;

    /// <summary>The frequency half of this component's own scope key — 4 runs/week, never 3D/5D/6D (each of which genuinely differs, see this class's own doc comment).</summary>
    public const int ScopeRunsPerWeek = 4;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_LEVEL_FREQUENCY_AUTHORITY</c> — preferred weekly volume-increase ratio. Identical at every Intermediate×4D anchor including canonical TEN_K.</summary>
    public const double PreferredWeeklyIncreaseRate = 0.07d;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_LEVEL_FREQUENCY_AUTHORITY</c> — hard weekly volume-increase ratio ceiling. Identical at every Intermediate×4D anchor including canonical TEN_K.</summary>
    public const double HardWeeklyIncreaseRate = 0.08d;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_LEVEL_FREQUENCY_AUTHORITY</c> — absolute per-week volume-increase cap in km. Identical at every Intermediate×4D anchor; genuinely 2.0 at Intermediate×3D, which is NOT in this component's scope.</summary>
    public const double AbsoluteWeeklyIncreaseCapKm = 2.5d;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_LEVEL_FREQUENCY_AUTHORITY</c> — long-run share quadruple, member 1 of 4 (preferred minimum).</summary>
    public const double LongRunPreferredMinimumShare = 0.30d;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_LEVEL_FREQUENCY_AUTHORITY</c> — long-run share quadruple, member 2 of 4 (preferred maximum).</summary>
    public const double LongRunPreferredMaximumShare = 0.36d;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_LEVEL_FREQUENCY_AUTHORITY</c> — long-run share quadruple, member 3 of 4 (selection share).</summary>
    public const double LongRunSelectionShare = 0.33d;

    /// <summary>DIST-GEN.8 §7 <c>SHARED_LEVEL_FREQUENCY_AUTHORITY</c> — long-run share quadruple, member 4 of 4 (hard-cap share).</summary>
    public const double LongRunHardCapShare = 0.40d;

    /// <summary>
    /// DIST-GEN.8 §7 / DIST-GEN.12 §17 — the first-Core-week absolute long-run
    /// ceiling is <c>null</c> (absent) at 4D for every anchor, and non-null only
    /// at 5D/6D. This is a FREQUENCY fact, not a distance fact. A cap is never a
    /// default and never a readiness substitute — that binding semantic
    /// distinction is unchanged here.
    /// </summary>
    public static readonly double? StartingAbsoluteLongRunCapKm = null;
}
