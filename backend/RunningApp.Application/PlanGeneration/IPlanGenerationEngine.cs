using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Entities;

namespace RunningApp.Application.PlanGeneration;

/// <summary>
/// Result of selecting a plan template for a generate-preview request.
/// Surfaces fallback behavior explicitly instead of letting it happen silently.
/// </summary>
public sealed class TemplateSelectionResult
{
    public required PlanTemplate Template { get; init; }
    public bool FallbackUsed { get; init; }
    public string? FallbackReason { get; init; }
}

/// <summary>
/// Interface for the plan generation engine.
/// Phase 1: only <see cref="PlaceholderPlanGenerationEngine"/> implements this,
/// and it does nothing more than pick a seeded template (with fallback).
/// A real generation algorithm (load progression, taper, race-specific
/// periodization, etc.) is out of scope for this phase.
/// </summary>
public interface IPlanGenerationEngine
{
    Task<TemplateSelectionResult> SelectTemplateAsync(GeneratePreviewRequest request, CancellationToken ct = default);
}
