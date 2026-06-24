namespace RunningApp.Application.DTOs.PendingConfirmation;

public class PendingConfirmationItemDto
{
    public Guid TrainingDayId { get; init; }
    public DateTime Date { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public double PlannedDistanceKm { get; init; }
    public int PlannedDurationMin { get; init; }
    public string Status { get; init; } = "pending";
}

public class PendingConfirmationsResponse
{
    public List<PendingConfirmationItemDto> Items { get; init; } = [];
    public int MaxVisibleItems { get; init; } = 3;
}

public class ResolveItemRequest
{
    public Guid TrainingDayId { get; init; }
    public string Answer { get; init; } = string.Empty; // "completed" | "missed"
}

public class ResolvePendingConfirmationsRequest
{
    public List<ResolveItemRequest> Items { get; init; } = [];
}

public class ResolvedItemDto
{
    public Guid TrainingDayId { get; init; }
    public string Status { get; init; } = string.Empty;
}

public class ResolvePendingConfirmationsResponse
{
    public List<ResolvedItemDto> ResolvedItems { get; init; } = [];
    public string TriggerSource { get; init; } = "pending_confirmation";
    public bool PlanAdapted { get; init; } = false;
    public object? AdaptationSummary { get; init; } = null;
}
