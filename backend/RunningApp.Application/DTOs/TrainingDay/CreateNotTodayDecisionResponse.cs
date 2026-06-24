using System;

namespace RunningApp.Application.DTOs.TrainingDay;

public class CreateNotTodayDecisionResponse
{
    public Guid DecisionId { get; set; }
    public string Status { get; set; } = string.Empty;
}
