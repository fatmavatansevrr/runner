using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

/// <summary>Reads, but never recalculates, the current Core Week 1 prescribed pace output.</summary>
internal static class PreparationRunwayCoreWeekOnePaceAdapter
{
    public static PreparationRunwayCoreWeekOnePaceTarget FromAuthoritativeCoreBehavior(CatalogPrescribedPlan corePlan)
    {
        var first = corePlan.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        var keyCount = first?.Sessions.Count(s => s.StructuralRole == "KEY_SESSION") ?? 0;
        var easyCount = first?.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT") ?? 0;
        var longCount = first?.Sessions.Count(s => s.StructuralRole == "LONG_RUN") ?? 0;
        if (first is null || keyCount < 1 || longCount != 1 || easyCount != first.Sessions.Count - keyCount - longCount ||
            easyCount < 1 || first.PhaseKey != "FOUNDATION")
            throw new InvalidOperationException("Authoritative Core Foundation Week 1 pace target is unavailable.");

        var roleOrdinals = new Dictionary<PreparationRunwaySlotRole, int>();
        var slots = first.Sessions.OrderBy(s => s.Date).ThenBy(s => s.StructuralRole).Select(session =>
        {
            var role = session.StructuralRole switch
            {
                "KEY_SESSION" => PreparationRunwaySlotRole.KeySession,
                "EASY_SUPPORT" => PreparationRunwaySlotRole.EasySupport,
                "LONG_RUN" => PreparationRunwaySlotRole.LongRun,
                _ => throw new InvalidOperationException($"Unsupported Core Week 1 structural role '{session.StructuralRole}'."),
            };
            roleOrdinals.TryGetValue(role, out var prior);
            var ordinal = prior + 1;
            roleOrdinals[role] = ordinal;
            return new PreparationRunwayCoreWeekOnePaceSlotTarget(
                role, ordinal, session.WorkoutDefinitionKey, session.WorkoutDefinitionVersion,
                session.Prescription.PacePrescription, session.PaceSourceProvenance);
        }).ToArray();

        return new PreparationRunwayCoreWeekOnePaceTarget(
            corePlan.CandidateKey, corePlan.CandidateVersion, slots,
            "CatalogSessionPrescriptionPlanner authoritative Core Foundation Week 1 output");
    }
}
