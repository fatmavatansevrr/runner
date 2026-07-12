using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Persistence;

namespace RunningApp.Application.PlanGeneration;

/// <summary>
/// Phase 0 (safe template selection). Picks the seeded template that exactly
/// matches the request's (GoalType, GoalDistance, Level, DaysPerWeek)
/// combination. No real generation logic (periodization, load progression,
/// taper weeks, etc.) is implemented here — that remains a future phase.
///
/// Deliberately does NOT fall back to an arbitrary seeded template when no
/// exact match exists: an unsupported combination (e.g. a TEN_K / Intermediate
/// / 4-day request, which has no seeded template today) must fail loudly via
/// <see cref="PlanTemplateNotAvailableException"/> rather than silently
/// return an unrelated plan (e.g. a seeded 5K / Beginner template). See
/// artifacts/audits/backend-process-b-activation-review.md (plan-catalog) for
/// the review that identified the previous silent-fallback behavior.
/// </summary>
public sealed class PlaceholderPlanGenerationEngine : IPlanGenerationEngine
{
    private readonly AppDbContext _context;
    private readonly ILogger<PlaceholderPlanGenerationEngine> _logger;

    public PlaceholderPlanGenerationEngine(AppDbContext context, ILogger<PlaceholderPlanGenerationEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TemplateSelectionResult> SelectTemplateAsync(GeneratePreviewRequest request, CancellationToken ct = default)
    {
        var exactMatch = await _context.PlanTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.GoalType == request.GoalType
                                    && t.GoalDistance == request.GoalDistance
                                    && t.Level == request.Level
                                    && t.DaysPerWeek == request.DaysPerWeek, ct);

        if (exactMatch != null)
        {
            return new TemplateSelectionResult
            {
                Template = exactMatch,
                FallbackUsed = false,
                FallbackReason = null,
            };
        }

        // No silent fallback: log diagnostic context (safe to be verbose here —
        // this is a log, not the API response) and fail loudly.
        _logger.LogWarning(
            "No seeded plan template matches goal_type={GoalType}, goal_distance={GoalDistance}, " +
            "level={Level}, days_per_week={DaysPerWeek}. Refusing to fall back to an unrelated template.",
            request.GoalType, request.GoalDistance, request.Level, request.DaysPerWeek);

        throw new PlanTemplateNotAvailableException(
            $"No plan template is available for goal_type={request.GoalType}, goal_distance={request.GoalDistance}, " +
            $"level={request.Level}, days_per_week={request.DaysPerWeek}.");
    }
}
