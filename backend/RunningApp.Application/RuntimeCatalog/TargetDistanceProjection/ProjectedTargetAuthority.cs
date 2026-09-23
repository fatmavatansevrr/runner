using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DIST-GEN.13 — the complete training-authority composition for ONE
/// already-approved dark target-distance projection cell. Together with
/// <see cref="ProjectedTargetAuthorityRegistry"/> this is the single typed
/// resolution boundary that replaced the four hand-written per-target dispatch
/// cascades DIST-GEN.12 §11 identified (<c>PlanServices</c>,
/// <c>CatalogPreviewGenerator</c>, <c>CatalogVolumeAndLongRunPlanner</c>,
/// <c>TargetDistance16KAwarePeakVolumeBandLoader</c>).
///
/// <b>This record freezes no new number, decides nothing, and computes nothing.</b>
/// Every member below is a REFERENCE to a value that is already declared, and
/// still declared, by its own correct owner:
/// <list type="bullet">
/// <item><b>Exact-target fields</b> — <see cref="PreferredCoreWeeks"/>,
/// <see cref="MaximumCoreWeeks"/>, the peak-volume band, and — through
/// <see cref="VolumeSafetyPolicy"/> — <c>PreferredAbsolutePeakLongRunKm</c>,
/// <c>ResolvedPeakReference</c> and <c>GoldenFixtureStartingVolumeKm</c> —
/// stay declared in that target's OWN policy files
/// (<c>TargetDistanceNNKHorizonPolicy</c>,
/// <c>TargetDistanceNNKPeakVolumeBandPolicy</c>, and that target's own named
/// <see cref="Prescription.Volume.VolumeSafetyPolicy"/> instance).
/// DIST-GEN.12 §54 forbids normalizing any of them, most especially
/// <c>PreferredAbsolutePeakLongRunKm</c> (14.5 / 15.0 / 16.0 — three
/// genuinely different, independently-frozen values that must never move into
/// any shared component).</item>
/// <item><b>Shared level/frequency fields</b> (growth rates, absolute weekly
/// cap, long-run share quadruple, starting long-run cap) are referenced by the
/// target's own <see cref="Prescription.Volume.VolumeSafetyPolicy"/> instance
/// from <see cref="IntermediateFourDayGrowthAuthority"/>.</item>
/// <item><b>Shared parent-family taper fields</b> come from
/// <see cref="HalfMarathonParentTaperAuthority"/>.</item>
/// <item><see cref="MinimumCoreWeeks"/> is parent-family structural authority —
/// the reused HALF_MARATHON catalog identity's own phase-allocation
/// minimum-week sum (2+3+3+2=10), which each horizon policy now takes from the
/// <c>RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks</c>
/// constant that already carried that same fact.</item>
/// </list>
///
/// <b>Exact-cell keyed.</b> <see cref="Cell"/> is the very same
/// <see cref="Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell"/>
/// record the dark eligibility registry already uses — the full
/// <c>(TargetDistanceKm, ParentDistanceFamily, Level, RunsPerWeek)</c>
/// quadruple, never a distance alone. No distance interval, no proximity
/// match, no fitted value and no fallback exists in this type or in
/// <see cref="ProjectedTargetAuthorityRegistry"/>: an unregistered cell
/// resolves to nothing at all, and its caller then behaves exactly as it does
/// today for an unapproved target.
/// </summary>
internal sealed record ProjectedTargetAuthority
{
    /// <summary>
    /// The exact approved dark cell this authority belongs to — value-equal to
    /// the record the dark eligibility registry itself holds, so an authority
    /// can never be attached to a cell the dark gate never approved.
    /// </summary>
    public required Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell Cell { get; init; }

    /// <summary>
    /// The human-readable pilot label this target's own observable exception
    /// and log text uses ("dark 15K" / "dark 16K" / "dark 18K"). Carried here
    /// verbatim so each target's already-shipped message text stays
    /// byte-identical; deliberately never genericized into one shared string.
    /// </summary>
    public required string PilotLabel { get; init; }

    /// <summary>
    /// <c>SHARED_PARENT_FAMILY_AUTHORITY</c> (DIST-GEN.8 §7 / DIST-GEN.12 §53's
    /// own <c>MinimumCoreWeeks</c> row): the reused HALF_MARATHON catalog
    /// identity's own real phase-allocation minimum-week sum, a structural fact
    /// about the reused candidate rather than a per-target decision. Read from
    /// this target's own horizon policy, which now references the parent-family
    /// constant instead of re-declaring a bare literal.
    /// </summary>
    public required int MinimumCoreWeeks { get; init; }

    /// <summary>EXACT-TARGET (DIST-GEN.12 §54, <c>INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE</c>) — read from this target's own horizon policy. Never shared, never normalized.</summary>
    public required int PreferredCoreWeeks { get; init; }

    /// <summary>EXACT-TARGET (DIST-GEN.12 §54, <c>INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE</c>) — read from this target's own horizon policy. Never shared, never normalized.</summary>
    public required int MaximumCoreWeeks { get; init; }

    /// <summary>This target's own horizon-decision entry point, bound to its own declared policy class so the decision's provenance remains that policy, not this record.</summary>
    public required Func<DateOnly, DateOnly, CoreHorizonDecision> DecideHorizon { get; init; }

    /// <summary>This target's own horizon-classification entry point, bound to its own declared policy class.</summary>
    public required Func<CoreHorizonDecision, RaceHorizonClassification> ClassifyHorizon { get; init; }

    /// <summary>This target's own deterministic unsupported-horizon reason-code builder (e.g. <c>DARK_18K_CORE_HORIZON_7_NOT_SUPPORTED</c>), bound to its own declared policy class. Never a shared or genericized code.</summary>
    public required Func<int, string> GetUnsupportedReasonCode { get; init; }

    /// <summary>EXACT-TARGET — this target's own named <see cref="Prescription.Volume.VolumeSafetyPolicy"/> instance, referenced; never rebuilt here and never merged with a sibling's.</summary>
    public required VolumeSafetyPolicy VolumeSafetyPolicy { get; init; }

    /// <summary>EXACT-TARGET (DIST-GEN.12 §54) — this target's own peak-volume band builder, bound to its own independently-declared <c>TargetDistanceNNKPeakVolumeBandPolicy</c>.</summary>
    public required Func<string, string, int, string, int, CatalogPeakVolumeBand> BuildPeakVolumeBand { get; init; }

    /// <summary>EXACT-TARGET — this target's own declared peak-volume minimum, surfaced for provenance assertions only; the runtime value always comes from <see cref="BuildPeakVolumeBand"/>.</summary>
    public required double PeakVolumeBandMinimumKm { get; init; }

    /// <summary>EXACT-TARGET — this target's own declared peak-volume maximum, surfaced for provenance assertions only.</summary>
    public required double PeakVolumeBandMaximumKm { get; init; }
}
