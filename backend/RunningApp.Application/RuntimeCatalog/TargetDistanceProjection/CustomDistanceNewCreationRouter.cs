using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;

/// <summary>
/// PHASE DERIVED-DIST.2 — the one new-plan-creation routing decision for a
/// custom target distance strictly between TEN_K and HALF_MARATHON: did this
/// request's generation pipeline consult the generic, continuous-range
/// derivation mechanism (<see cref="DerivedDistanceEligibilityPolicy"/> /
/// <see cref="DerivedDistanceArcSelector"/>), or the historical, exact-cell
/// registry (<see cref="Dark16KPilotEligibilityPolicy"/> /
/// <see cref="ProjectedTargetAuthorityRegistry"/>)?
///
/// Exactly two members — there is no third "neither" state here, because this
/// enum only ever gets set on a request <see cref="DTOs.Plan.GeneratePreviewRequest.RoutingDecision"/>
/// that <see cref="RunningApp.Application.Commands.Plan.GeneratePreviewCommandMapper"/>
/// has already determined IS an approved custom-target request (either route).
/// A request that is neither derived-eligible nor an approved legacy cell
/// never reaches generation at all — the mapper rejects it with
/// <c>UnsupportedTargetDistanceException</c> before this enum is ever
/// consulted.
/// </summary>
public enum CustomDistanceRoutingDecision
{
    /// <summary>Generic derived routing (DERIVED-DIST.0/.2) — the V1 production default for new public creation.</summary>
    GenericDerived,

    /// <summary>Historical exact projected-cell routing (DIST-GEN.1-20) — legacy/rollback only as of DERIVED-DIST.2.</summary>
    LegacyExactProjected,
}

/// <summary>
/// PHASE DERIVED-DIST.2 §16/§18 — the ONE narrow rollback control boundary
/// for custom 10K-to-HALF_MARATHON new-plan creation. This is a
/// CREATION-ROUTING switch only: it is consulted exclusively by
/// <see cref="RunningApp.Application.Commands.Plan.GeneratePreviewCommandMapper.ToInternalRequest(Commands.Plan.PlanPreviewCommand)"/>
/// at the moment a brand-new public preview request is mapped, and nowhere
/// else. It has no effect on: canonical TEN_K/HALF_MARATHON requests; any
/// already-confirmed plan's Home/Calendar/TrainingDay/PlanDetails/Complete/
/// Not-Today/Cancel read or mutation flow (those never re-run this mapper —
/// see <see cref="RunningApp.Application.Commands.Plan.GeneratePreviewCommandMapper.ToCommand(DTOs.Plan.GeneratePreviewRequest)"/>,
/// which only ever replays an already-resolved, already-persisted
/// <see cref="DTOs.Plan.GeneratePreviewRequest.RoutingDecision"/>, never
/// recomputes it); or any already-generated preview awaiting confirmation
/// (the preview's own persisted <c>RequestPayloadJson</c> already carries its
/// own frozen <see cref="CustomDistanceRoutingDecision"/>, so flipping this
/// switch between preview-generation and confirm can never silently change
/// which route a pending confirm uses — see <see cref="DTOs.Plan.GeneratePreviewRequest.RoutingDecision"/>'s
/// own doc comment).
///
/// Production default is <see cref="CustomDistanceRoutingMode.GenericDerived"/>
/// (DERIVED-DIST.2 §17). Flipping to <see cref="CustomDistanceRoutingMode.LegacyExactProjected"/>
/// restores the pre-DERIVED-DIST.2 behavior for brand-new requests only: a
/// Custom target is then only accepted when it exactly matches one of
/// <see cref="Dark16KPilotEligibilityPolicy.ApprovedPublicCells"/> (16K I5D —
/// never public before this phase — again becomes unreachable, exactly as it
/// was pre-migration). No DB rollback, no catalog rollback, no plan rewrite,
/// no migration, and no deletion of any derived code is required or performed
/// by this switch.
/// </summary>
public static class CustomDistanceRoutingMode
{
    public static CustomDistanceRoutingDecision ProductionDefault { get; set; } = CustomDistanceRoutingDecision.GenericDerived;
}

/// <summary>
/// PHASE DERIVED-DIST.2 §16 — the ONE internal-dispatch routing boundary.
/// Every production call site inside the real catalog/legacy generation
/// pipeline (horizon selection in <c>PlanServices.GeneratePreviewAsync</c> and
/// <c>CatalogPreviewGenerator</c>, volume-policy selection in
/// <c>CatalogVolumeAndLongRunPlanner</c>, peak-volume-band selection in
/// <see cref="Prescription.Volume.TargetDistance16KAwarePeakVolumeBandLoader"/>)
/// that previously called <see cref="Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>
/// directly now calls <see cref="ResolveLegacyExactCellForDispatch"/> instead,
/// so all four dispatch seams can never silently disagree about which route a
/// given request takes.
///
/// <b>Back-compat fallback, not a live re-decision.</b> When
/// <paramref name="persistedRoutingDecision"/> is non-null (every request that
/// reached this point through <c>GeneratePreviewCommandMapper</c> — i.e. every
/// real public request, and every confirm replay of an already-persisted
/// preview) the persisted decision is authoritative and final: this method
/// never re-evaluates eligibility against the live <see cref="CustomDistanceRoutingMode"/>
/// switch for such a request. When it is null (every existing dark/legacy
/// internal test harness that constructs <see cref="DTOs.Plan.GeneratePreviewRequest"/>
/// directly, predating this phase, plus the DERIVED-DIST.0 prototype's own
/// internal callers) this resolves EXACTLY as it always has —
/// <see cref="Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(double?,GoalDistance,RunningBackground?,int?)"/>
/// unconditionally, so every pre-existing dark/legacy regression test keeps
/// its exact historical output (DERIVED-DIST.2 §58/§86 — these tests must
/// stay green, unmodified, not be rewritten to expect the new model).
/// </summary>
public static class CustomDistanceNewCreationRouter
{
    public static Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell? ResolveLegacyExactCellForDispatch(
        CustomDistanceRoutingDecision? persistedRoutingDecision,
        double? targetDistanceKmOverride,
        GoalDistance requestGoalDistance,
        RunningBackground? level,
        int? runsPerWeek)
    {
        if (persistedRoutingDecision == CustomDistanceRoutingDecision.GenericDerived)
        {
            return null;
        }

        return Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(
            targetDistanceKmOverride, requestGoalDistance, level, runsPerWeek);
    }

    /// <summary>String/candidate-identity-typed sibling, for <c>CatalogVolumeAndLongRunPlanner</c>'s own candidate-shaped call site.</summary>
    public static Dark16KPilotEligibilityPolicy.ApprovedDarkTargetDistanceCell? ResolveLegacyExactCellForDispatch(
        CustomDistanceRoutingDecision? persistedRoutingDecision,
        double? targetDistanceKmOverride,
        string canonicalDistanceFamily,
        string level,
        int runsPerWeek)
    {
        if (persistedRoutingDecision == CustomDistanceRoutingDecision.GenericDerived)
        {
            return null;
        }

        return Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(
            targetDistanceKmOverride, canonicalDistanceFamily, level, runsPerWeek);
    }

    /// <summary>
    /// The one place a brand-new public Custom-target request is classified
    /// into a <see cref="CustomDistanceRoutingDecision"/> (or rejected).
    /// Consulted ONLY by <c>GeneratePreviewCommandMapper.ToInternalRequest</c>.
    /// Order of evaluation is itself the governing-prompt-mandated precedence
    /// (§5/§13/§17): generic derived eligibility is checked FIRST, under the
    /// live <see cref="CustomDistanceRoutingMode"/> switch; the historical
    /// exact public-cell registry is consulted only as a fallback, so an
    /// existing exact cell (15K/16K/18K I4D, 16K I3D) is no longer the
    /// winning route for new creation merely because it exists (§59).
    /// </summary>
    public static CustomDistanceRoutingDecision? ClassifyNewPublicCustomRequest(
        double? targetDistanceKmOverride,
        GoalDistance requestGoalDistance,
        RunningBackground? level,
        int? runsPerWeek)
    {
        if (CustomDistanceRoutingMode.ProductionDefault == CustomDistanceRoutingDecision.GenericDerived &&
            DerivedDistanceEligibilityPolicy.IsEligible(targetDistanceKmOverride, requestGoalDistance, level, runsPerWeek))
        {
            return CustomDistanceRoutingDecision.GenericDerived;
        }

        if (Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell(targetDistanceKmOverride, requestGoalDistance, level, runsPerWeek) is not null)
        {
            return CustomDistanceRoutingDecision.LegacyExactProjected;
        }

        // Rollback mode (CustomDistanceRoutingMode.ProductionDefault == LegacyExactProjected):
        // generic derived is deliberately not consulted at all for NEW creation in this
        // mode (§18 — rollback disables new generic-derived creation), so a request that
        // only the derived policy would have approved is rejected, exactly like pre-DERIVED-DIST.2.
        return null;
    }
}
