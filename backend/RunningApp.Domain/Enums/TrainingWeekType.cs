namespace RunningApp.Domain.Enums;

public enum TrainingWeekType
{
    Base,
    Build,
    Recovery,
    Peak,
    Taper,
    RaceWeek,

    /// <summary>
    /// Backend Integration Phase 4G.6B — a 15-20 week TEN_K Preparation
    /// Runway week (the pre-Core segment). Never emitted for 8-14 week
    /// requests. Distinct from Base/Build/Peak/Taper on purpose: it is not
    /// a Core phase and must never be represented as one.
    /// </summary>
    PreparationRunway
}
