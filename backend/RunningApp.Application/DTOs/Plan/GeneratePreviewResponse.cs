using RunningApp.Domain.Enums;
using System;
using System.Collections.Generic;

namespace RunningApp.Application.DTOs.Plan;

/// <summary>
/// Backend Integration Phase 4G.6B — explicit preview lifecycle
/// classification, carried from generation rather than inferred at confirm
/// time. <see cref="CoreConfirmable"/> is the existing, unchanged 8-14 week
/// behavior. <see cref="PreparationRunwayPreviewNotConfirmable"/> is new:
/// generation may succeed for a 15-20 week Preparation Runway preview, but
/// the result is explicitly not confirmable in this phase.
/// </summary>
public enum PreviewLifecycleClassification
{
    CoreConfirmable,
    PreparationRunwayPreviewNotConfirmable,

    /// <summary>
    /// Backend Integration Phase 4G.6C — an exact-pilot 15-20 week
    /// Preparation Runway preview whose snapshot carries a real, persistable
    /// <c>GeneratedPreviewPlanPayload</c> because the scoped confirmation
    /// gate (<c>PreparationRunwayPilotActivationOptions.ConfirmationEnabled</c>)
    /// was enabled at generation time. Confirming it follows the exact same
    /// <c>CatalogPlanConfirmationService.ConfirmAsync</c> path as any other
    /// catalog-sourced preview.
    /// </summary>
    PreparationRunwayPreviewConfirmable,
}

public class GeneratePreviewResponse
{
    public Guid PreviewId { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public GoalType GoalType { get; set; }
    public GoalDistance GoalDistance { get; set; }
    public RunningBackground Level { get; set; }
    public int DaysPerWeek { get; set; }
    public DistanceUnit Unit { get; set; }
    public List<PreviewWeekDto> Weeks { get; set; } = new();

    /// <summary>
    /// Always false as of Phase 0 (safe template selection): a request with
    /// no exact matching seeded template now fails with
    /// <c>PLAN_TEMPLATE_NOT_FOUND</c> instead of generating a preview from an
    /// unrelated fallback template. Field kept for API back-compatibility.
    /// </summary>
    public bool FallbackUsed { get; set; }

    /// <summary>Always null as of Phase 0 — see <see cref="FallbackUsed"/>.</summary>
    public string? FallbackReason { get; set; }

    /// <summary>
    /// Backend Integration Phase 4G.6B. Defaults to <see cref="PreviewLifecycleClassification.CoreConfirmable"/>
    /// — every existing 8-14 week response path sets this explicitly to the
    /// same value, so behavior is unchanged for existing clients that ignore
    /// this new field.
    /// </summary>
    public PreviewLifecycleClassification Lifecycle { get; set; } = PreviewLifecycleClassification.CoreConfirmable;
}

public class PreviewWeekDto
{
    public int WeekNumber { get; set; }
    public TrainingWeekType WeekType { get; set; }
    public List<PreviewDayDto> Days { get; set; } = new();

    /// <summary>
    /// Backend Integration Phase 4G.6B — the honest Preparation Runway
    /// block name (CONSISTENCY/GENERAL_ENDURANCE/AEROBIC_STRENGTH/
    /// PRE_SPECIFIC_TRANSITION) for a runway week. Always null for every
    /// existing Core week (8-14 week responses never set this field).
    /// </summary>
    public string? RunwayBlock { get; set; }
}

public class PreviewDayDto
{
    public int SlotIndex { get; set; }
    public TrainingDayType DayType { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMin { get; set; }
    public string Intensity { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
