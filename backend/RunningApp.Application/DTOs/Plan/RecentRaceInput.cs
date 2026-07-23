using RunningApp.Domain.Enums;

namespace RunningApp.Application.DTOs.Plan;

/// <summary>
/// A previously-completed race result reported at onboarding for
/// pace/readiness context. Deliberately distinct from the target race
/// (<see cref="GeneratePreviewRequest.RaceName"/>/<see cref="GeneratePreviewRequest.RaceDate"/>/
/// <see cref="GeneratePreviewRequest.TargetFinishTimeSeconds"/>) — mapping
/// this object must never write into those fields.
/// </summary>
public sealed class RecentRaceInput
{
    public GoalDistance Distance { get; set; }
    public int FinishTimeSeconds { get; set; }
    public DateOnly RaceDate { get; set; }
}
