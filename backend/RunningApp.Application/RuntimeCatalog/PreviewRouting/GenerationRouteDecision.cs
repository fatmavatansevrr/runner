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

// Phase 4F.9.1A: the previous PilotGenerationRouteDecider implementation of
// IGenerationRouteDecider lived here. It was never registered in DI (see
// RunningApp.Api/Program.cs, which registers only LivePlanPreviewRoutingService
// for IGenerationRouteDecider) and was exercised solely by its own now-removed
// unit test file. Its pilot-identity check duplicated
// V1LiveCatalogPilotRoutingPolicy's inline check verbatim; both now consume the
// single centrally-owned V1CatalogPilotIdentityPolicy. Removed as dead code.
