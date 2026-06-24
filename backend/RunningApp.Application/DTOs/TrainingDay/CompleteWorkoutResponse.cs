using System;

namespace RunningApp.Application.DTOs.TrainingDay;

public class CompleteWorkoutResponse
{
    public Guid DayId { get; set; }
    public string Status { get; set; } = string.Empty;
}
