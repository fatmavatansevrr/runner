using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DIST-GEN.2 — the single, canonical, narrow eligibility gate for the
/// one approved dark target-distance projection pilot:
/// <c>(TargetDistanceKm = 16.0, ParentDistanceFamily = HALF_MARATHON,
/// Level = Intermediate, RunsPerWeek = 4)</c>, per
/// PHASE_DIST_GEN_1_16K_INTERMEDIATE_4D_TARGET_DISTANCE_PROJECTION_STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md
/// §3/§47.
///
/// This is deliberately the ONLY place this exact-triple check is expressed —
/// every production call site that needs to decide "does this request get the
/// 16K projection, or the canonical family policy" consults this class,
/// rather than repeating an inline <c>== 16.0</c> comparison. This is
/// authority/eligibility, distinct from CLASSIFICATION: the dormant
/// <see cref="CanonicalDistanceFamilyResolver"/> can classify many other
/// distances (12K, 15K, 18K, 20K, ...) into HALF_MARATHON or TEN_K — that
/// classification answers "which catalog family would structurally serve
/// this distance", never "is this distance approved for dark materialization
/// today". Only the exact frozen triple below is eligible; every other
/// distance (including other values a real customer might one day request
/// inside the open (10.0, 21.1) interval DIST-GEN.0 described) remains
/// entirely unreachable through this gate, by design, until its own future
/// governance/authority-closure phase exists.
///
/// Never reachable from any public request DTO: no public wire contract
/// (<c>GenerateRacePlanPreviewRequest</c>/<c>GenerateHabitPlanPreviewRequest</c>)
/// carries an arbitrary target-distance field, so the override this gate
/// checks (<see cref="RunningApp.Application.DTOs.Plan.GeneratePreviewRequest.TargetDistanceKmOverride"/>)
/// is always null for every real HTTP-originated request and can only be set
/// by an internal/test-harness caller constructing the internal
/// <see cref="RunningApp.Application.DTOs.Plan.GeneratePreviewRequest"/>
/// directly — mirroring the same "production-owned but not publicly
/// reachable" seam <see cref="RunningApp.Application.RuntimeCatalog.PreviewRouting.V1CatalogPilotIdentityPolicy"/>'s
/// own internal 3-arg <c>ResolveCandidate(GoalDistance,RunningBackground,int)</c>
/// overload established for HM's own dark-only 5D/6D candidates.
/// </summary>
public static class Dark16KPilotEligibilityPolicy
{
    /// <summary>The one approved dark-eligible target distance, per DIST-GEN.1 §3/§47.</summary>
    public const double EligibleTargetDistanceKm = 16.0;

    /// <summary>The structural parent this pilot projects from, per DIST-GEN.1 §4.</summary>
    public const GoalDistance EligibleParentDistanceFamily = GoalDistance.HalfMarathon;

    /// <summary>The one approved dark-eligible level, per DIST-GEN.1 §3.</summary>
    public const RunningBackground EligibleLevel = RunningBackground.Intermediate;

    /// <summary>The one approved dark-eligible frequency, per DIST-GEN.1 §3.</summary>
    public const int EligibleRunsPerWeek = 4;

    private const double ToleranceKm = 0.001;

    /// <summary>
    /// True only for the exact frozen triple, and only when
    /// <paramref name="requestGoalDistance"/> resolves to HALF_MARATHON (the
    /// structural parent this projection reuses — see DIST-GEN.1 §4/§6). Any
    /// null input, or any divergence from the frozen triple (including a
    /// numerically-close-but-not-exact target distance, or any other
    /// level/frequency), returns false.
    /// </summary>
    public static bool IsEligible(
        double? targetDistanceKmOverride,
        GoalDistance requestGoalDistance,
        RunningBackground? level,
        int? runsPerWeek) =>
        targetDistanceKmOverride is { } km &&
        Math.Abs(km - EligibleTargetDistanceKm) < ToleranceKm &&
        requestGoalDistance == EligibleParentDistanceFamily &&
        level == EligibleLevel &&
        runsPerWeek == EligibleRunsPerWeek;

    /// <summary>
    /// Candidate-identity-shaped overload for call sites that only have the
    /// catalog candidate's own string-typed <c>Level</c>/<c>CanonicalDistanceFamily</c>
    /// fields (e.g. <see cref="RunningApp.Application.RuntimeCatalog.Prescription.Volume.CatalogVolumeAndLongRunPlanner"/>'s
    /// dispatcher) rather than the request's typed enum. Semantically
    /// identical to the primary overload.
    /// </summary>
    public static bool IsEligible(
        double? targetDistanceKmOverride,
        string canonicalDistanceFamily,
        string level,
        int runsPerWeek) =>
        targetDistanceKmOverride is { } km &&
        Math.Abs(km - EligibleTargetDistanceKm) < ToleranceKm &&
        canonicalDistanceFamily == "HALF_MARATHON" &&
        level == "INTERMEDIATE" &&
        runsPerWeek == EligibleRunsPerWeek;
}
