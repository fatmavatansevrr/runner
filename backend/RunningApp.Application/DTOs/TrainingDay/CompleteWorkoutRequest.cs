namespace RunningApp.Application.DTOs.TrainingDay;

public class CompleteWorkoutRequest
{
    public double ActualDistanceKm { get; set; }
    public int ActualDurationMin { get; set; }
    public string? UserNote { get; set; }
}
