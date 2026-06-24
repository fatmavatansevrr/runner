using System;

namespace RunningApp.Application.DTOs.TrainingDay;

public class ConfirmNotTodayDecisionResponse
{
    public Guid DecisionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Action { get; set; } = "no_change";
}
