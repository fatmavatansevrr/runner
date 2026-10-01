using System.Collections.Generic;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DERIVED-DIST.0 — governing prompt §61's required field-by-field
/// audit, done explicitly and in code (so it is itself a checkable, versioned
/// artifact, not only prose in a report) BEFORE any blind rebinding was
/// implemented. Every field below is classified into exactly one of four
/// buckets. This type performs no rebinding itself — it documents and lets
/// tests assert against the classification that the two narrow production
/// widenings (<see cref="DerivedDistanceEligibilityPolicy"/> /
/// <see cref="DerivedDistanceHorizonAuthority"/> consumed by
/// <c>PlanServices.GeneratePreviewAsync</c> and
/// <c>CatalogPreviewGenerator</c>) actually implement.
///
/// <b>Result: no UNSAFE_TO_REBIND field was found for the V1 prototype scope</b>
/// (HALF_MARATHON Intermediate x {3,4,5}D, requested target strictly between
/// TEN_K and HALF_MARATHON). This is itself a significant, disclosed finding
/// — see PHASE_DERIVED_DIST_0_...md §10/§61 for the full narrative.
/// </summary>
public enum DerivedTargetFieldClassification
{
    /// <summary>Must carry the ACTUAL requested target distance, never HM's own family-representative distance.</summary>
    RebindToRequestedTarget,

    /// <summary>Stays exactly as HALF_MARATHON's own canonical value — structural/parent authority, never target-specific.</summary>
    KeepFromParent,

    /// <summary>Not touched directly by this prototype at all — already flows through an existing, generic, target-aware resolver with zero new code.</summary>
    RecomputeThroughExistingGenericResolver,

    /// <summary>No field in this prototype's actual scope was classified this way — see this type's own doc comment.</summary>
    UnsafeToRebind,
}

/// <summary>One audited field's classification and the concrete mechanism/evidence behind it.</summary>
public sealed record DerivedTargetFieldAuditEntry(string FieldName, DerivedTargetFieldClassification Classification, string Mechanism);

public static class DerivedDistanceTargetFieldAudit
{
    public static readonly IReadOnlyList<DerivedTargetFieldAuditEntry> Entries =
    [
        new("ResolverInputSnapshot.RequestedTargetDistanceKm", DerivedTargetFieldClassification.RebindToRequestedTarget,
            "CatalogPreviewGenerator.ResolveRequestedTargetDistanceKm widened (this phase) to also recognize a " +
            "DerivedDistanceEligibilityPolicy-eligible request, mirroring the existing Dark16KPilotEligibilityPolicy arm " +
            "byte-for-byte; resolves to request.TargetDistanceKmOverride.Value, never the family-representative constant."),

        new("ResolverInputSnapshot.GoalDistanceKm (family-representative, e.g. 21.0975)", DerivedTargetFieldClassification.KeepFromParent,
            "Unchanged: CatalogGoalDistanceResolver.Resolve(candidate.CanonicalDistanceFamily, request.GoalDistance) still " +
            "resolves the structural parent's own representative distance -- RequestedTargetDistanceKm and GoalDistanceKm " +
            "are, and remain, two distinct fields by design (pre-existing doc note in CatalogPrescriptionContextBuilder.cs)."),

        new("request.GoalDistance / Candidate.CanonicalDistanceFamily / RunLayout / PhaseTopology", DerivedTargetFieldClassification.KeepFromParent,
            "Stays HALF_MARATHON for every derived request -- candidate identity, phase allocation, run layout, and " +
            "workout-catalog selection are never target-distance-specific; this is the structural-parent reuse the whole " +
            "prototype depends on (governing prompt §24)."),

        new("GoalFeasibilityResolver target-distance for Riegel projection", DerivedTargetFieldClassification.RecomputeThroughExistingGenericResolver,
            "GoalFeasibilityResolver.ResolveFromRecentRace already reads 'input.RequestedTargetDistanceKm ?? input.GoalDistanceKm' " +
            "verbatim (pre-existing code, zero lines changed) -- once RequestedTargetDistanceKm carries the real requested " +
            "target (see the first row above), the Riegel projection, goal-gap-ratio classification, and aggressiveness " +
            "band all automatically use the requested target, with no new code."),

        new("VolumeSafetyPolicy (peak/taper/long-run-share authority)", DerivedTargetFieldClassification.KeepFromParent,
            "CatalogVolumeAndLongRunPlanner.Build's exact-cell dispatch (Dark16KPilotEligibilityPolicy/ProjectedTargetAuthorityRegistry) " +
            "resolves null for every derived (unregistered) cell by construction and falls through, completely unmodified, " +
            "to VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(daysPerWeek) -- canonical HALF_MARATHON's own peak " +
            "band, taper multipliers (via HalfMarathonParentTaperAuthority), and long-run shares. No new peak-volume policy " +
            "class is introduced (governing prompt §30/§31)."),

        new("Peak-volume band (catalog JSON)", DerivedTargetFieldClassification.KeepFromParent,
            "TargetDistance16KAwarePeakVolumeBandLoader resolves null for every derived cell and delegates unchanged to the " +
            "real catalog-backed loader -- HALF_MARATHON's own real catalog band, never a new band."),

        new("Core horizon (Minimum/Preferred/MaximumCoreWeeks, TargetWeekCount)", DerivedTargetFieldClassification.RebindToRequestedTarget,
            "DerivedDistanceHorizonAuthority's own 10/12/14 triple (reused convergent authority, not target-specific per " +
            "distance) drives RaceHorizonPolicy.Decide; the resulting AvailableFullWeeks becomes the DynamicCore pipeline's " +
            "own TargetWeekCount, which genuinely varies with the request's StartDate/RaceDate -- this is the one load-bearing " +
            "'rebind' the whole arc-selection mechanism depends on."),

        new("Phase/stage allocation (Foundation/Build/RaceSpecific/Taper weeks)", DerivedTargetFieldClassification.KeepFromParent,
            "The catalog's own generic two-argument phase-allocation resolver (candidate, targetWeekCount) is pre-existing, unmodified, catalog-authored " +
            "mechanical allocation -- never touched by this phase."),

        new("Taper mechanics (2 weeks, 0.70/0.43, non-chained)", DerivedTargetFieldClassification.KeepFromParent,
            "Flows from canonical HALF_MARATHON's own VolumeSafetyPolicy instance (HalfMarathonIntermediate3D/4D/5D), which " +
            "itself already sources its taper multipliers from HalfMarathonParentTaperAuthority -- reused directly, zero new " +
            "taper code (governing prompt §21/§22)."),

        new("KEY1/KEY2 workout identity and progression-stage content", DerivedTargetFieldClassification.KeepFromParent,
            "Sourced from the unmodified half-marathon workout-progression document via the unmodified ProgressionStageAllocator " +
            "/ CatalogWorkoutBinder pipeline -- no new workout definitions, no new progression document (governing prompt §25/§27)."),

        new("CatalogPlanPrescriptionContext.InputSnapshot.RequestedTargetDistanceKm (downstream prescription/volume context)", DerivedTargetFieldClassification.RebindToRequestedTarget,
            "CatalogPrescriptionContextBuilder.Build already carries request.ResolverInput.RequestedTargetDistanceKm verbatim " +
            "into CatalogPlanPrescriptionContext.InputSnapshot (pre-existing code, confirmed by direct read, zero lines " +
            "changed), and CatalogVolumeAndLongRunPlanner.Build already reads that same field for its exact-cell dispatch " +
            "check -- once the first row above carries the real requested target, every downstream reader of this same " +
            "field automatically sees the real requested target with no further code change."),
    ];
}
