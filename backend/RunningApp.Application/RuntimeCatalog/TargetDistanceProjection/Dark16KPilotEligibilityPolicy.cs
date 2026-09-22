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

    /// <summary>
    /// PHASE DIST-GEN.6 — the small, explicit, in-code approved-cell registry
    /// DIST-GEN.4 §38 recommended in place of scattered per-call-site
    /// hardcoded quadruples. Every entry here is a DARK-only materialization
    /// eligibility grant, consulted by the internal projection pipeline
    /// (<see cref="TryResolveDarkEligibleCell(double?,RunningApp.Domain.Enums.GoalDistance,RunningApp.Domain.Enums.RunningBackground?,int?)"/>
    /// and its string-typed sibling) — this is a DISTINCT concept from PUBLIC
    /// activation, which remains governed exclusively by the narrower,
    /// unmodified <see cref="IsEligible(double?,RunningApp.Domain.Enums.GoalDistance,RunningApp.Domain.Enums.RunningBackground?,int?)"/>
    /// overloads above (still true only for the exact frozen 16.0 triple, per
    /// DIST-GEN.1/DIST-GEN.3 — untouched by this phase) consumed by
    /// <see cref="RunningApp.Application.Commands.Plan.GeneratePreviewCommandMapper"/>'s
    /// own canonicalization gate. Adding an entry here NEVER widens what a
    /// real public HTTP request can reach; it only widens what the internal,
    /// non-public <see cref="RunningApp.Application.DTOs.Plan.GeneratePreviewRequest.TargetDistanceKmOverride"/>
    /// seam can materialize.
    ///
    /// Three entries today: the original 16.0km pilot (DIST-GEN.1/2), the
    /// second, 15.0km target (DIST-GEN.5/6), and the third, 18.0km target
    /// (DIST-GEN.9/10) — all three reusing the identical
    /// HALF_MARATHON×Intermediate×4D catalog identity. Kept as three explicit
    /// records rather than a single deduplicated constant even though several
    /// of their downstream numeric fields coincide, per DIST-GEN.6's own
    /// governing instruction (re-confirmed by DIST-GEN.10 for the third
    /// entry): collapsing independently-authorized cells into one shared
    /// literal would misrepresent the authority structure a future,
    /// materially different fourth target might need.
    /// </summary>
    public sealed record ApprovedDarkTargetDistanceCell(
        double TargetDistanceKm,
        GoalDistance ParentDistanceFamily,
        RunningBackground Level,
        int RunsPerWeek);

    /// <summary>The complete, explicit dark-eligible registry. Order is not significant; lookup is by exact-match, never nearest/interpolated.</summary>
    public static readonly IReadOnlyList<ApprovedDarkTargetDistanceCell> ApprovedDarkCells =
    [
        new(EligibleTargetDistanceKm, EligibleParentDistanceFamily, EligibleLevel, EligibleRunsPerWeek), // DIST-GEN.1/2 -- 16.0km
        new(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4), // DIST-GEN.5/6 -- 15.0km
        new(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4), // DIST-GEN.9/10 -- 18.0km (DARK ONLY -- never added to ApprovedPublicCells)
    ];

    /// <summary>
    /// Enum-typed registry lookup, mirroring the shape of the primary
    /// <see cref="IsEligible(double?,GoalDistance,RunningBackground?,int?)"/>
    /// overload's parameters exactly, but checked against every approved
    /// dark cell rather than only the single 16.0km triple. Returns the
    /// matched cell (so a caller can select the correct target-specific
    /// downstream policy/instance), or null for any exact-non-match,
    /// including a numerically-close-but-not-exact target distance. Never
    /// used by any public-request-reachable call site.
    /// </summary>
    public static ApprovedDarkTargetDistanceCell? TryResolveDarkEligibleCell(
        double? targetDistanceKmOverride,
        GoalDistance requestGoalDistance,
        RunningBackground? level,
        int? runsPerWeek)
    {
        if (targetDistanceKmOverride is not { } km)
        {
            return null;
        }

        foreach (var cell in ApprovedDarkCells)
        {
            if (Math.Abs(km - cell.TargetDistanceKm) < ToleranceKm &&
                requestGoalDistance == cell.ParentDistanceFamily &&
                level == cell.Level &&
                runsPerWeek == cell.RunsPerWeek)
            {
                return cell;
            }
        }

        return null;
    }

    /// <summary>String/candidate-identity-typed sibling of <see cref="TryResolveDarkEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>, mirroring the shape of the secondary <see cref="IsEligible(double?,string,string,int)"/> overload.</summary>
    public static ApprovedDarkTargetDistanceCell? TryResolveDarkEligibleCell(
        double? targetDistanceKmOverride,
        string canonicalDistanceFamily,
        string level,
        int runsPerWeek)
    {
        if (targetDistanceKmOverride is not { } km || canonicalDistanceFamily != "HALF_MARATHON" || level != "INTERMEDIATE")
        {
            return null;
        }

        foreach (var cell in ApprovedDarkCells)
        {
            if (Math.Abs(km - cell.TargetDistanceKm) < ToleranceKm && runsPerWeek == cell.RunsPerWeek)
            {
                return cell;
            }
        }

        return null;
    }

    /// <summary>Convenience boolean form of <see cref="TryResolveDarkEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>, for call sites that only need the yes/no answer.</summary>
    public static bool IsDarkEligible(double? targetDistanceKmOverride, GoalDistance requestGoalDistance, RunningBackground? level, int? runsPerWeek) =>
        TryResolveDarkEligibleCell(targetDistanceKmOverride, requestGoalDistance, level, runsPerWeek) is not null;

    /// <summary>Convenience boolean form of <see cref="TryResolveDarkEligibleCell(double?,string,string,int)"/>, for call sites that only need the yes/no answer.</summary>
    public static bool IsDarkEligible(double? targetDistanceKmOverride, string canonicalDistanceFamily, string level, int runsPerWeek) =>
        TryResolveDarkEligibleCell(targetDistanceKmOverride, canonicalDistanceFamily, level, runsPerWeek) is not null;

    /// <summary>
    /// PHASE DIST-GEN.7 — the small, explicit, PUBLIC projected-target-distance
    /// registry. Deliberately declared as its OWN list, not derived from
    /// <see cref="ApprovedDarkCells"/> — a cell being materializable
    /// internally (dark) never implies it is publicly reachable, and vice
    /// versa; the two lists are independently authored and independently
    /// auditable, even though today they happen to contain the same two
    /// entries. Consulted exclusively by
    /// <see cref="RunningApp.Application.Commands.Plan.GeneratePreviewCommandMapper"/>'s
    /// canonicalization gate — the ONE place public target-distance
    /// eligibility is decided. As of DIST-GEN.7 this supersedes the original,
    /// single-target <see cref="IsEligible(double?,GoalDistance,RunningBackground?,int?)"/>
    /// overloads as the mapper's own gate (those overloads are left in place,
    /// unmodified, as the historical/frozen DIST-GEN.1-3 single-triple
    /// authority and because <see cref="EligibleTargetDistanceKm"/> et al. are
    /// still referenced by this class and by existing tests/messages — they
    /// are no longer called by the mapper and are effectively superseded,
    /// not dead in the sense of being unreferenced).
    ///
    /// Exactly three entries: 16.0 (DIST-GEN.3's own original public grant),
    /// 15.0 (DIST-GEN.7's own new public grant), and 18.0 (DIST-GEN.11's own
    /// new public grant, reusing the dark authority already frozen and
    /// implemented in DIST-GEN.9/10). Never a computed range —
    /// lookup is by exact match only, per this class's established pattern.
    /// </summary>
    public static readonly IReadOnlyList<ApprovedDarkTargetDistanceCell> ApprovedPublicCells =
    [
        new(EligibleTargetDistanceKm, EligibleParentDistanceFamily, EligibleLevel, EligibleRunsPerWeek), // DIST-GEN.3 -- 16.0km
        new(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4), // DIST-GEN.7 -- 15.0km
        new(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4), // DIST-GEN.11 -- 18.0km
    ];

    /// <summary>
    /// Enum-typed public-registry lookup, mirroring
    /// <see cref="TryResolveDarkEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>'s
    /// exact shape but checked against <see cref="ApprovedPublicCells"/>. This
    /// is the overload <see cref="RunningApp.Application.Commands.Plan.GeneratePreviewCommandMapper"/>
    /// calls.
    /// </summary>
    public static ApprovedDarkTargetDistanceCell? TryResolvePublicEligibleCell(
        double? targetDistanceKmOverride,
        GoalDistance requestGoalDistance,
        RunningBackground? level,
        int? runsPerWeek)
    {
        if (targetDistanceKmOverride is not { } km)
        {
            return null;
        }

        foreach (var cell in ApprovedPublicCells)
        {
            if (Math.Abs(km - cell.TargetDistanceKm) < ToleranceKm &&
                requestGoalDistance == cell.ParentDistanceFamily &&
                level == cell.Level &&
                runsPerWeek == cell.RunsPerWeek)
            {
                return cell;
            }
        }

        return null;
    }

    /// <summary>String/candidate-identity-typed sibling of <see cref="TryResolvePublicEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>, mirroring the dark registry's own string-typed sibling shape. Not currently called by any production call site (the mapper has only the typed enum available), kept for pattern-parity with the dark registry.</summary>
    public static ApprovedDarkTargetDistanceCell? TryResolvePublicEligibleCell(
        double? targetDistanceKmOverride,
        string canonicalDistanceFamily,
        string level,
        int runsPerWeek)
    {
        if (targetDistanceKmOverride is not { } km || canonicalDistanceFamily != "HALF_MARATHON" || level != "INTERMEDIATE")
        {
            return null;
        }

        foreach (var cell in ApprovedPublicCells)
        {
            if (Math.Abs(km - cell.TargetDistanceKm) < ToleranceKm && runsPerWeek == cell.RunsPerWeek)
            {
                return cell;
            }
        }

        return null;
    }

    /// <summary>Convenience boolean form of <see cref="TryResolvePublicEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>, for call sites that only need the yes/no answer.</summary>
    public static bool IsPubliclyEligible(double? targetDistanceKmOverride, GoalDistance requestGoalDistance, RunningBackground? level, int? runsPerWeek) =>
        TryResolvePublicEligibleCell(targetDistanceKmOverride, requestGoalDistance, level, runsPerWeek) is not null;

    /// <summary>Convenience boolean form of <see cref="TryResolvePublicEligibleCell(double?,string,string,int)"/>, for call sites that only need the yes/no answer.</summary>
    public static bool IsPubliclyEligible(double? targetDistanceKmOverride, string canonicalDistanceFamily, string level, int runsPerWeek) =>
        TryResolvePublicEligibleCell(targetDistanceKmOverride, canonicalDistanceFamily, level, runsPerWeek) is not null;
}
