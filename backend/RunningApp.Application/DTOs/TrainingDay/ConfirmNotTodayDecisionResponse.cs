using System;

namespace RunningApp.Application.DTOs.TrainingDay;

public class ConfirmNotTodayDecisionResponse
{
    public Guid DecisionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Action { get; set; } = "no_change";

    /// <summary>
    /// Always false in Phase 1 — the placeholder adaptation engine never
    /// mutates future training days.
    /// </summary>
    public bool PlanAdapted { get; set; }
}
