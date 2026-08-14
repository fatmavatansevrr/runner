using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

/// <summary>Reads, but never recalculates, the current Core Week 1 prescribed pace output.</summary>
internal static class PreparationRunwayCoreWeekOnePaceAdapter
{
    public static PreparationRunwayCoreWeekOnePaceTarget FromAuthoritativeCoreBehavior(CatalogPrescribedPlan corePlan)
    {
        var first = corePlan.Weeks.OrderBy(w => w.WeekNumber).FirstOrDefault();
        if (first is null || first.Sessions.Count != 4 || first.PhaseKey != "FOUNDATION")
            throw new InvalidOperationException("Authoritative four-session Core Foundation Week 1 pace target is unavailable.");

        var easyOrdinal = 0;
        var slots = first.Sessions.OrderBy(s => s.Date).ThenBy(s => s.StructuralRole).Select(session =>
        {
            var role = session.StructuralRole switch
            {
                "KEY_SESSION" => PreparationRunwaySlotRole.KeySession,
                "EASY_SUPPORT" => PreparationRunwaySlotRole.EasySupport,
                "LONG_RUN" => PreparationRunwaySlotRole.LongRun,
                _ => throw new InvalidOperationException($"Unsupported Core Week 1 structural role '{session.StructuralRole}'."),
            };
            var ordinal = role == PreparationRunwaySlotRole.EasySupport ? ++easyOrdinal : 1;
            return new PreparationRunwayCoreWeekOnePaceSlotTarget(
                role, ordinal, session.WorkoutDefinitionKey, session.WorkoutDefinitionVersion,
                session.Prescription.PacePrescription, session.PaceSourceProvenance);
        }).ToArray();

        return new PreparationRunwayCoreWeekOnePaceTarget(
            corePlan.CandidateKey, corePlan.CandidateVersion, slots,
            "CatalogSessionPrescriptionPlanner authoritative Core Foundation Week 1 output");
    }
}
