namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>Backend Integration Phase 4F.3 — a candidate's resolved, validated, ordered structural run-layout roles.</summary>
internal sealed class CatalogRunLayoutSlots
{
    /// <summary>Exact (key, version) identity of the source run-layout document.</summary>
    public required PlanCatalogReference Layout { get; init; }

    /// <summary>Ordered structural role sequence, e.g. <c>["KEY_SESSION","EASY_SUPPORT","EASY_SUPPORT","LONG_RUN"]</c>.</summary>
    public required IReadOnlyList<string> StructuralRoles { get; init; }
}

/// <summary>
/// Backend Integration Phase 4F.3 — derives a candidate's authoritative
/// structural session-slot roles from its already-loaded run-layout
/// dependency (<see cref="PlanCatalogCandidateSummary.SlotRoles"/>, itself
/// read verbatim from the layout document's own <c>slots[].role</c> array).
/// Never hardcodes the pilot's own <c>[KEY_SESSION, EASY_SUPPORT,
/// EASY_SUPPORT, LONG_RUN]</c> shape — every value comes directly from the
/// loaded candidate. Pure and dependency-free.
/// </summary>
internal interface ICatalogRunLayoutResolver
{
    CatalogRunLayoutSlots Resolve(PlanCatalogCandidateSummary candidate);
}

/// <inheritdoc cref="ICatalogRunLayoutResolver"/>
internal sealed class CatalogRunLayoutResolver : ICatalogRunLayoutResolver
{
    private static readonly string[] KnownUnsupportedRoleSubstrings = { "REST", "OPTIONAL", "RECOVERY" };

    public CatalogRunLayoutSlots Resolve(PlanCatalogCandidateSummary candidate)
    {
        var roles = candidate.SlotRoles;

        if (roles.Count == 0)
        {
            throw new CatalogRunLayoutSlotInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}''s loaded run layout declares no slots.");
        }

        if (roles.Any(string.IsNullOrWhiteSpace))
        {
            throw new CatalogRunLayoutSlotInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' has a blank/malformed slot role.");
        }

        if (roles.Count != candidate.DaysPerWeek)
        {
            throw new CatalogRunLayoutSlotInvalidException(
                $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' run layout declares " +
                $"{roles.Count} slots, but DaysPerWeek is {candidate.DaysPerWeek}. The layout's own slot count " +
                "is the source of truth and must match.");
        }

        foreach (var role in roles)
        {
            if (KnownUnsupportedRoleSubstrings.Any(unsupported => role.Contains(unsupported, StringComparison.OrdinalIgnoreCase)))
            {
                throw new CatalogRunLayoutSlotInvalidException(
                    $"Candidate '{candidate.CandidateKey}' v{candidate.CandidateVersion}' run layout declares an " +
                    $"unsupported structural role '{role}' (REST/OPTIONAL/RECOVERY roles are never accepted — rest " +
                    "days are implicit, and no optional/recovery slot classification exists in this contract).");
            }
        }

        return new CatalogRunLayoutSlots
        {
            Layout = candidate.Layout,
            StructuralRoles = roles,
        };
    }
}
