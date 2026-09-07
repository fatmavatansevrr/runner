using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayPacePrescription;

/// <summary>Reads, but never recalculates, the current Core Week 1 prescribed pace output.</summary>
internal static class PreparationRunwayCoreWeekOnePaceAdapter
{
    public static PreparationRunwayCoreWeekOnePaceTarget FromAuthoritativeCoreBehavior(CatalogPrescribedPlan corePlan)
    {
        var weeksOrdered = corePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
        var localWeekOne = weeksOrdered.Length > 0 ? weeksOrdered[0] : null;
        var localWeekOneKeyCount = localWeekOne?.Sessions.Count(s => s.StructuralRole == "KEY_SESSION") ?? 0;

        // Phase 10K-GEN.37 -- "DECISION ON GEN.36": anchor to the first Core
        // week (within local weeks 1-2) that carries a KEY_SESSION, rather
        // than unconditionally assuming local week 1 always does. Core's
        // Pattern-A/B alternation is strict (proven GEN.26-GEN.36): if local
        // week 1 is Pattern B (EASY_SUPPORT+LONG_RUN, zero KEY_SESSION --
        // only reachable once a continuation offset makes Core's true week 1
        // continue an odd GeneralEnduranceWeeks global parity), local week 2
        // is guaranteed Pattern A. Anchoring to local week 1 when it already
        // carries a KEY_SESSION (every case reachable before GEN.37, and
        // every case where the continuation offset happens to land Core's
        // week 1 on Pattern A) is byte-identical to pre-GEN.37 behavior --
        // zero delta. This does not add any new pace/numeric authority: it
        // only changes WHICH week supplies the existing goal-pace data, never
        // what that data means or how it is computed (deriving a target from
        // an EASY_SUPPORT session was explicitly considered and rejected --
        // not implemented here or anywhere).
        var first = localWeekOneKeyCount >= 1
            ? localWeekOne
            : (weeksOrdered.Length > 1 ? weeksOrdered[1] : null);

        var keyCount = first?.Sessions.Count(s => s.StructuralRole == "KEY_SESSION") ?? 0;
        var easyCount = first?.Sessions.Count(s => s.StructuralRole == "EASY_SUPPORT") ?? 0;
        var longCount = first?.Sessions.Count(s => s.StructuralRole == "LONG_RUN") ?? 0;
        // Phase 10K-GEN.29 -- this floor (`easyCount < 1`) was disclosed as
        // still open but unreachable for 2D by GEN.27 §1's own recurring-
        // defect-family search ("no 2D Runway->Core continuity path exists
        // yet at the numeric/pace layer"). GEN.29 makes that path real: 2D
        // Core Week 1 (RUN_LAYOUT_2D) is exactly 1 KEY_SESSION + 1 LONG_RUN,
        // zero EASY_SUPPORT -- the same "not every caller shape considered"
        // family GEN.10/GEN.20/GEN.27/GEN.28 already found repeated
        // instances of. Removed the unconditional easyCount>=1 requirement;
        // KEY_SESSION (>=1) and exactly one LONG_RUN remain required for
        // every frequency including 2D. Zero-delta for every pre-GEN.29
        // frequency, all of which always have easyCount>=1 already.
        if (first is null || keyCount < 1 || longCount != 1 || easyCount != first.Sessions.Count - keyCount - longCount ||
            first.PhaseKey != "FOUNDATION")
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
            $"CatalogSessionPrescriptionPlanner authoritative Core Foundation week {first.WeekNumber} output" +
            (first.WeekNumber == 1 ? string.Empty : " (GEN.37: local week 1 carried zero KEY_SESSION, anchored to the next KEY_SESSION-bearing week per DECISION ON GEN.36)"));
    }
}
