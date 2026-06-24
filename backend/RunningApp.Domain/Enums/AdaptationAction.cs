namespace RunningApp.Domain.Enums;

/// <summary>
/// Describes what the placeholder (and later real) Adaptive Engine decided to do.
/// Phase 1: always returns NoChange.
/// </summary>
public enum AdaptationAction
{
    NoChange,
    Skipped,
    Rescheduled,
    Shortened,
    RecoveryWeek
}
