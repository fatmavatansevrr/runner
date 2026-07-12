using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — stable generation-source values, decided
/// once per request before any generation begins. Plain string constants
/// (not a C# enum), matching this codebase's established convention for
/// registry-style/audit-style values (see e.g. every resolver's OutputValue
/// constants) rather than relying on enum serialization casing.
/// </summary>
public static class GenerationSource
{
    public const string Catalog = "CATALOG";
    public const string LegacySql = "LEGACY_SQL";
}

/// <summary>
/// The outcome of routing a single generate-preview request to either the
/// catalog pilot flow or the existing legacy SQL-template flow. Computed
/// exactly once, before generation begins — there is no "try catalog, catch
/// failure, fall back to SQL" path anywhere in this codebase; once
/// <see cref="Source"/> is <see cref="GenerationSource.Catalog"/>, any
/// catalog-flow failure is final.
/// </summary>
public sealed record GenerationRouteDecision(string Source, string RouteReason);

/// <summary>
/// Decides <see cref="GenerationRouteDecision"/> for a single request. No
/// implementation may consult catalog files, resolvers, or the database —
/// routing is pure request-shape logic, decided before any of those are
/// touched.
/// </summary>
public interface IGenerationRouteDecider
{
    GenerationRouteDecision Decide(GeneratePreviewRequest request);
}

/// <summary>
/// Backend Integration Phase 4E.1 — routes exactly the
/// TEN_K / INTERMEDIATE / 4-days-per-week pilot combination to the catalog
/// flow; every other request continues through the existing legacy SQL
/// flow unchanged.
///
/// Pilot match criteria, each with its evidence basis:
/// <list type="bullet">
/// <item><c>GoalType == Race</c> — TEN_K is a race distance; every prior
/// phase's TEN_K/INTERMEDIATE/4D test scenario used GoalType.Race (no habit
/// scenario for this combination exists anywhere in this repository).</item>
/// <item><c>GoalDistance == TenK</c> — the pilot's own catalog candidate key
/// is literally <c>TEN_K__4D__INTERMEDIATE</c>.</item>
/// <item><c>Level == RunningBackground.RunningRegularly</c> — the backend's
/// <c>RunningBackground</c> enum (NewToRunning/UsedToRun/RunningRegularly)
/// has no exact "INTERMEDIATE" member (Phase 2 finding: NotSupported, a
/// different taxonomy axis). No formal owner-approved mapping from
/// RunningBackground to the catalog's INTERMEDIATE level exists as of this
/// phase. <c>RunningRegularly</c> is used here because it is the value
/// every prior phase's TEN_K/INTERMEDIATE/4D test scenario has used as its
/// informal stand-in since Phase 0 (e.g.
/// CatalogNotWiredToGenerationTests.TenKIntermediate4Day_StillReturnsPlanTemplateNotAvailable_CatalogNotYetWired)
/// — an established repository convention, not a newly invented mapping.
/// If a future phase formally approves a different mapping, this criterion
/// must be updated to match, not silently left inconsistent.</item>
/// <item><c>DaysPerWeek == 4</c> — the pilot candidate key names 4D.</item>
/// </list>
/// </summary>
public sealed class PilotGenerationRouteDecider : IGenerationRouteDecider
{
    public const string PilotCandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int PilotCandidateVersion = 10;

    public GenerationRouteDecision Decide(GeneratePreviewRequest request)
    {
        var isPilotCombination =
            request.GoalType == GoalType.Race &&
            request.GoalDistance == GoalDistance.TenK &&
            request.Level == RunningBackground.RunningRegularly &&
            request.DaysPerWeek == 4;

        return isPilotCombination
            ? new GenerationRouteDecision(GenerationSource.Catalog, "PILOT_TEN_K_INTERMEDIATE_4D_MATCH")
            : new GenerationRouteDecision(GenerationSource.LegacySql, "NOT_PILOT_COMBINATION");
    }
}
