using RunningApp.Domain.Enums;

namespace RunningApp.Application.Adaptation;

/// <summary>
/// Describes the outcome of an adaptation decision.
/// Phase 1: always NoChange. Real logic added later.
/// </summary>
public class AdaptationDecision
{
    public AdaptationAction Action { get; init; } = AdaptationAction.NoChange;
    public TriggerSource TriggerSource { get; init; }
    public bool PlanAdapted { get; init; } = false;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string SupportiveText { get; init; } = string.Empty;
    public List<Guid> AffectedDays { get; init; } = [];
}
