using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.DTOs.Plan;

public sealed class LongHorizonConfirmPlanRequest
{
    public required Guid PreviewId { get; init; }
    public int ContractVersion { get; init; } = 1;
}

public enum LongHorizonConfirmationOutcome
{
    Confirmed,
    AlreadyConfirmed,
}

public sealed record LongHorizonConfirmPlanResponse
{
    public int ContractVersion { get; init; } = 1;
    public required Guid PlanId { get; init; }
    public required Guid PreviewId { get; init; }
    public required LongHorizonConfirmationOutcome Outcome { get; init; }
    public PlanScheduleStrategy ScheduleStrategy { get; init; } = PlanScheduleStrategy.RollingLongHorizon;
    public required int TotalWeeks { get; init; }
    public required IReadOnlyList<LongHorizonExecutableWeekContract> CurrentExecutableWeeks { get; init; }
    public required int? NextPendingGlobalWeek { get; init; }
    public required string PlanStatus { get; init; }
    public required string PublicMessage { get; init; }
    public required DateTime ConfirmedAtUtc { get; init; }
}
