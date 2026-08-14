namespace PlanCatalog.Contracts.Enums;

public enum PhaseKey
{
    Foundation,
    Build,
    RaceSpecific,
    Taper,

    /// <summary>
    /// Backend Integration Phase 4G.6A.4A — a generic, distance-family-agnostic
    /// workout-eligibility value for the pre-Core Preparation Runway period.
    /// It is NOT a Core plan-template phase: plan-template.schema.json's own
    /// phases[].phaseKey enum deliberately remains closed to
    /// FOUNDATION/BUILD/RACE_SPECIFIC/TAPER only (unchanged by this value),
    /// so no PlanTemplate can ever declare a PreparationRunway phase. This
    /// value exists solely so a WorkoutDefinition's EligiblePhases can
    /// truthfully declare "usable in the Preparation Runway period,"
    /// without implying eligibility for any Core phase.
    /// </summary>
    PreparationRunway,

    /// <summary>
    /// Backend Integration Phase 4I.4 — a generic, distance-family-agnostic
    /// workout-eligibility value for the 1-32-week Long-Horizon General
    /// Endurance segment approved by TD-LONG-HORIZON-COMPOSITION-001/
    /// TD-LONG-HORIZON-GE-SAFETY-001. Deliberately a textually and
    /// semantically distinct identifier from the Preparation Runway's own
    /// <c>PreparationRunwayBlockType.GeneralEndurance</c> block value and
    /// from <c>preparation-runway-block-progression.schema.json</c>'s
    /// <c>blockType: "GENERAL_ENDURANCE"</c> — the two concepts are
    /// governance-recognized as different (see
    /// TerminologyDoesNotCollideWithTheExistingRunwayGeneralEnduranceBlock
    /// in the Phase 4I.1/4I.2 governance test suites) and this value must
    /// never be merged with them. Like <see cref="PreparationRunway"/>,
    /// this is NOT a Core plan-template phase: plan-template.schema.json's
    /// own phases[].phaseKey enum remains closed to
    /// FOUNDATION/BUILD/RACE_SPECIFIC/TAPER only, unchanged by this value.
    /// This value exists solely so a WorkoutDefinition's EligiblePhases can
    /// truthfully declare "usable in the Long-Horizon General Endurance
    /// segment," without implying eligibility for Core or Preparation
    /// Runway. As of Phase 4I.4, no live orchestrator queries this value —
    /// it is reachable only by dark catalog capacity code and tests (see
    /// TD-LONG-HORIZON-GE-CATALOG-CAPACITY-001).
    /// </summary>
    LongHorizonGeneralEndurance
}
