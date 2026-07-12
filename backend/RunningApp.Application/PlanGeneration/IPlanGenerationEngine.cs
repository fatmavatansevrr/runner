using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Entities;

namespace RunningApp.Application.PlanGeneration;

/// <summary>
/// Result of selecting a plan template for a generate-preview request.
///
/// As of Phase 0 (safe template selection), <see cref="FallbackUsed"/> is
/// always false and <see cref="FallbackReason"/> is always null: selection
/// either finds an exact match or throws <see cref="RunningApp.Application.Exceptions.PlanTemplateNotAvailableException"/>.
/// The fields are kept on this type (and on <c>GeneratePreviewResponse</c>)
/// for API/serialization back-compatibility rather than removed.
/// </summary>
public sealed class TemplateSelectionResult
{
    public required PlanTemplate Template { get; init; }
    public bool FallbackUsed { get; init; }
    public string? FallbackReason { get; init; }
}

/// <summary>
/// Interface for the plan generation engine.
/// Phase 0: only <see cref="PlaceholderPlanGenerationEngine"/> implements this,
/// and it does nothing more than pick a seeded template that exactly matches
/// the request, throwing <see cref="RunningApp.Application.Exceptions.PlanTemplateNotAvailableException"/>
/// when none exists. A real generation algorithm (load progression, taper,
/// race-specific periodization, plan-catalog bundle consumption, etc.) is out
/// of scope for this phase.
/// </summary>
public interface IPlanGenerationEngine
{
    Task<TemplateSelectionResult> SelectTemplateAsync(GeneratePreviewRequest request, CancellationToken ct = default);
}
