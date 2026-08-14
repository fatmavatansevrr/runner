namespace RunningApp.Domain.Enums;

/// <summary>How a confirmed plan's executable schedule is materialized.</summary>
public enum PlanScheduleStrategy
{
    StaticComplete,
    RollingLongHorizon,
}
