using Microsoft.EntityFrameworkCore;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Persistence;

namespace RunningApp.Application.PlanGeneration;

/// <summary>
/// Phase 1 placeholder. Picks the first seeded template that exactly matches
/// the request's goal/level/days-per-week; otherwise falls back to the first
/// seeded template and reports the fallback explicitly via
/// <see cref="TemplateSelectionResult"/>. No real generation logic
/// (periodization, load progression, taper weeks, etc.) is implemented here.
/// Replace this with a real engine in a future phase.
/// </summary>
public sealed class PlaceholderPlanGenerationEngine : IPlanGenerationEngine
{
    private readonly AppDbContext _context;

    public PlaceholderPlanGenerationEngine(AppDbContext context)
    {
        _context = context;
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

        var fallback = await _context.PlanTemplates.AsNoTracking().FirstOrDefaultAsync(ct);
        if (fallback == null)
        {
            throw new InvalidOperationException("No plan templates found in the database. Please seed templates first.");
        }

        return new TemplateSelectionResult
        {
            Template = fallback,
            FallbackUsed = true,
            FallbackReason =
                $"No seeded template matches goal_type={request.GoalType}, goal_distance={request.GoalDistance}, " +
                $"level={request.Level}, days_per_week={request.DaysPerWeek}. Falling back to '{fallback.TemplateId}'.",
        };
    }
}
