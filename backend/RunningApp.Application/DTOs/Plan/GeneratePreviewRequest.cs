using RunningApp.Domain.Enums;
using System;

namespace RunningApp.Application.DTOs.Plan;

public class GeneratePreviewRequest
{
    public GoalType GoalType { get; set; }
    public GoalDistance GoalDistance { get; set; }
    public RunningBackground Level { get; set; }
    public int DaysPerWeek { get; set; }
    public DistanceUnit Unit { get; set; }
    public string? RaceName { get; set; }
    public DateOnly? RaceDate { get; set; }
    public int? TargetFinishTimeSeconds { get; set; }

    // Onboarding snapshot fields — captured once and frozen onto TrainingPlan.
    // Nullable so existing clients that don't send these continue to work.
    public string? PreferredDays { get; set; }       // JSON array e.g. "[1,3,5]"
    public int? WeeklyAvailability { get; set; }     // hours per week available
    public double? PreferredPace { get; set; }       // min/km comfortable pace

    public string? LongRunDay { get; set; }
    public string? HabitPlanType { get; set; }
    public string? CustomGoalType { get; set; }
    public int? CustomDurationWeeks { get; set; }
    public int? CustomTargetTimeSeconds { get; set; }

    // ── Backend Integration Phase 4B: runtime fitness-evidence input contract ──
    // All nullable/optional. Not read by any resolver or generation code today
    // (PlaceholderPlanGenerationEngine ignores them entirely) — they exist so a
    // future Phase 4C resolver implementation has real user evidence available
    // once it is wired up. See PHASE4B_RUNTIME_INPUT_CONTRACT_FOR_FITNESS_EVIDENCE.md.
    // paceEvidenceType/paceEvidenceDate are explicitly withheld (Phase 4A.3 scope
    // decision) pending the PACE_SOURCE_IN evidence-hierarchy mapping — do not add them here.

    /// <summary>User-reported longest run in the last ~30 days, in km.</summary>
    public double? RecentLongestRunKm { get; set; }

    /// <summary>User-reported recent typical weekly running volume, in km.</summary>
    public double? RecentWeeklyVolumeKm { get; set; }

    /// <summary>User-reported recent typical runs per week.</summary>
    public int? RecentRunsPerWeek { get; set; }

    /// <summary>Distance of the user's most recent race result, in km.</summary>
    public double? RecentRaceDistanceKm { get; set; }

    /// <summary>Finish time of the user's most recent race result, in seconds.</summary>
    public int? RecentRaceFinishTimeSeconds { get; set; }

    /// <summary>Date of the user's most recent race result.</summary>
    public DateOnly? RecentRaceDate { get; set; }
}
