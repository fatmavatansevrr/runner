// [ignoring loop detection]
using Microsoft.EntityFrameworkCore;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public class PlanServices : IPlanPreviewService, IPlanConfirmationService, IPlanManagementService
{
    private readonly AppDbContext _context;

    public PlanServices(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GeneratePreviewResponse> GeneratePreviewAsync(string userId, GeneratePreviewRequest request, CancellationToken ct = default)
    {
        // 1. Try to find a template matching the user's goals
        var template = await _context.PlanTemplates
            .FirstOrDefaultAsync(t => t.GoalType == request.GoalType 
                                   && t.GoalDistance == request.GoalDistance 
                                   && t.Level == request.Level 
                                   && t.DaysPerWeek == request.DaysPerWeek, ct);

        // Fallback to first template if none matches
        if (template == null)
        {
            template = await _context.PlanTemplates.FirstOrDefaultAsync(ct);
            if (template == null)
            {
                throw new InvalidOperationException("No plan templates found in the database. Please seed templates first.");
            }
        }

        // 2. Parse week/day layout from the template JSON
        var templateData = JsonSerializer.Deserialize<TemplateJsonData>(template.DataJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (templateData == null || templateData.Weeks == null)
        {
            throw new InvalidOperationException("Plan template data is invalid or empty.");
        }

        // 3. Map slot indices to actual dates starting from the next upcoming Monday
        var startOfWeek1 = DateTime.UtcNow.Date.AddDays(((int)DayOfWeek.Monday - (int)DateTime.UtcNow.DayOfWeek + 7) % 7);
        if (startOfWeek1 == DateTime.UtcNow.Date && DateTime.UtcNow.Hour >= 22)
        {
            // If it's already late Monday, start next Monday
            startOfWeek1 = startOfWeek1.AddDays(7);
        }

        var previewWeeks = new List<PreviewWeekDto>();
        for (int i = 0; i < templateData.Weeks.Count; i++)
        {
            var tempWeek = templateData.Weeks[i];
            var weekStart = startOfWeek1.AddDays(i * 7);

            var previewDays = new List<PreviewDayDto>();
            foreach (var tempDay in tempWeek.Days)
            {
                // Simple day mapping: Slot 1 = Monday, Slot 2 = Wednesday, Slot 3 = Friday, Slot 4 = Sunday
                int dayOffset = tempDay.SlotIndex switch
                {
                    1 => 0, // Monday
                    2 => 2, // Wednesday
                    3 => 4, // Friday
                    4 => 6, // Sunday
                    _ => (tempDay.SlotIndex - 1) % 7
                };

                previewDays.Add(new PreviewDayDto
                {
                    SlotIndex = tempDay.SlotIndex,
                    DayType = tempDay.DayType,
                    DistanceKm = tempDay.DistanceKm,
                    DurationMin = tempDay.DurationMin,
                    Intensity = tempDay.Intensity ?? "z2",
                    Date = weekStart.AddDays(dayOffset)
                });
            }

            previewWeeks.Add(new PreviewWeekDto
            {
                WeekNumber = tempWeek.WeekNumber,
                WeekType = tempWeek.WeekType,
                Days = previewDays
            });
        }

        var previewResponse = new GeneratePreviewResponse
        {
            PreviewId = Guid.NewGuid(),
            TemplateId = template.TemplateId,
            GoalType = request.GoalType,
            GoalDistance = request.GoalDistance,
            Level = request.Level,
            DaysPerWeek = request.DaysPerWeek,
            Unit = request.Unit,
            Weeks = previewWeeks
        };

        // 4. Save to PlanPreviews table
        var previewEntity = new PlanPreview
        {
            Id = previewResponse.PreviewId,
            UserId = userId,
            TemplateId = template.TemplateId,
            RequestPayloadJson = JsonSerializer.Serialize(request),
            PreviewPayloadJson = JsonSerializer.Serialize(previewResponse),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        };

        _context.PlanPreviews.Add(previewEntity);
        await _context.SaveChangesAsync(ct);

        return previewResponse;
    }

    public async Task<ConfirmPlanResponse> ConfirmPlanAsync(string userId, ConfirmPlanRequest request, CancellationToken ct = default)
    {
        // 1. Fetch preview
        var preview = await _context.PlanPreviews
            .FirstOrDefaultAsync(p => p.Id == request.PreviewId && p.UserId == userId, ct);

        if (preview == null)
        {
            throw new ArgumentException("Plan preview not found.");
        }

        if (preview.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Plan preview has expired. Please generate a new preview.");
        }

        // 2. Deserialize preview payload
        var previewData = JsonSerializer.Deserialize<GeneratePreviewResponse>(preview.PreviewPayloadJson);
        var requestData = JsonSerializer.Deserialize<GeneratePreviewRequest>(preview.RequestPayloadJson);

        if (previewData == null || requestData == null)
        {
            throw new InvalidOperationException("Failed to load plan preview data.");
        }

        // 3. Guarantee UserProfile exists (self-healing onboarding)
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile == null)
        {
            profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Runner",
                Email = "runner@example.com",
                Unit = previewData.Unit,
                RunningBackground = previewData.Level,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserProfiles.Add(profile);
        }

        // 4. Enforce single active plan rule (cancel existing plans)
        var existingActivePlans = await _context.TrainingPlans
            .Where(p => p.UserId == userId && p.Status == TrainingPlanStatus.Active)
            .ToListAsync(ct);

        foreach (var existingPlan in existingActivePlans)
        {
            existingPlan.Status = TrainingPlanStatus.Cancelled;
            existingPlan.CancelledAt = DateTime.UtcNow;
        }

        // 5. Create active TrainingPlan
        var totalWeeks = previewData.Weeks.Count;
        var planStartDate = previewData.Weeks.First().Days.First().Date;
        var planEndDate = planStartDate.AddDays(totalWeeks * 7);

        var plan = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TemplateId = preview.TemplateId,
            Status = TrainingPlanStatus.Active,
            GoalType = previewData.GoalType,
            GoalDistance = previewData.GoalDistance,
            GoalDistanceKm = GetGoalDistanceInKm(previewData.GoalDistance),
            Level = previewData.Level,
            DaysPerWeek = previewData.DaysPerWeek,
            Unit = previewData.Unit,
            RaceName = requestData.RaceName,
            RaceDate = requestData.RaceDate,
            TargetFinishTimeSeconds = requestData.TargetFinishTimeSeconds,
            StartedAt = planStartDate,
            EstimatedEndDate = planEndDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.TrainingPlans.Add(plan);

        // 6. Map preview weeks and days to database entities
        foreach (var previewWeek in previewData.Weeks)
        {
            var plannedVol = previewWeek.Days.Sum(d => d.DistanceKm);
            var weekEntity = new TrainingWeek
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                WeekNumber = previewWeek.WeekNumber,
                WeekType = previewWeek.WeekType,
                PlannedVolumeKm = plannedVol,
                ActualVolumeKm = 0,
                IsRecoveryWeek = previewWeek.WeekType == TrainingWeekType.Recovery,
                StartDate = previewWeek.Days.First().Date,
                CreatedAt = DateTime.UtcNow,
                Plan = plan
            };

            _context.TrainingWeeks.Add(weekEntity);

            foreach (var previewDay in previewWeek.Days)
            {
                var dayEntity = new TrainingDay
                {
                    Id = Guid.NewGuid(),
                    PlanId = plan.Id,
                    WeekId = weekEntity.Id,
                    Date = previewDay.Date,
                    DayType = previewDay.DayType,
                    Status = TrainingDayStatus.Planned,
                    Title = GetTitleForDay(previewDay.DayType, previewDay.DistanceKm),
                    Description = GetDescriptionForDay(previewDay.DayType, previewDay.DistanceKm),
                    PlannedDistanceKm = previewDay.DistanceKm,
                    PlannedDurationMin = previewDay.DurationMin,
                    PlannedPaceMinKm = previewDay.DistanceKm > 0 ? (previewDay.DurationMin / previewDay.DistanceKm) : null,
                    Intensity = previewDay.Intensity,
                    IsLongRun = previewDay.DayType == TrainingDayType.LongRun,
                    OriginalDate = previewDay.Date,
                    OriginalType = previewDay.DayType,
                    CanMarkComplete = true,
                    CanMarkNotToday = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Plan = plan,
                    Week = weekEntity
                };

                _context.TrainingDays.Add(dayEntity);
            }
        }

        // Log PlanGenerated event
        var planEvent = new PlanEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = plan.Id,
            EventType = "PlanGenerated",
            PayloadJson = JsonSerializer.Serialize(new { templateId = plan.TemplateId, startDate = planStartDate }),
            CreatedAt = DateTime.UtcNow
        };
        _context.PlanEvents.Add(planEvent);

        await _context.SaveChangesAsync(ct);

        return new ConfirmPlanResponse
        {
            PlanId = plan.Id,
            Status = "active"
        };
    }

    public async Task<CancelPlanResponse> CancelPlanAsync(string userId, Guid planId, CancelPlanRequest request, CancellationToken ct = default)
    {
        var plan = await _context.TrainingPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId && p.Status == TrainingPlanStatus.Active, ct);

        if (plan == null)
        {
            throw new ArgumentException("Active training plan not found.");
        }

        plan.Status = TrainingPlanStatus.Cancelled;
        plan.CancelledAt = DateTime.UtcNow;

        // Log PlanCancelled event
        var planEvent = new PlanEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = plan.Id,
            EventType = "PlanCancelled",
            PayloadJson = JsonSerializer.Serialize(new { reason = request.Reason }),
            CreatedAt = DateTime.UtcNow
        };
        _context.PlanEvents.Add(planEvent);

        await _context.SaveChangesAsync(ct);

        return new CancelPlanResponse
        {
            PlanId = plan.Id,
            Status = "cancelled"
        };
    }

    public async Task<object> GetActivePlanDetailsAsync(string userId, CancellationToken ct = default)
    {
        var plan = await _context.TrainingPlans
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Days)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == TrainingPlanStatus.Active, ct);

        if (plan == null)
        {
            return new { message = "No active training plan found." };
        }

        var totalWeeks = plan.Weeks.Count;
        var completedWeeksCount = plan.Weeks.Count(w => w.Days.All(d => d.Status == TrainingDayStatus.Completed || d.Status == TrainingDayStatus.Missed || d.Status == TrainingDayStatus.Skipped));
        var totalPlannedDistance = plan.Weeks.Sum(w => w.PlannedVolumeKm);
        var totalCompletedDistance = plan.Weeks.Sum(w => w.ActualVolumeKm);

        var response = new PlanDetailsResponse
        {
            PlanId = plan.Id,
            TemplateId = plan.TemplateId,
            Status = plan.Status.ToString().ToLower(),
            GoalType = plan.GoalType,
            GoalDistance = plan.GoalDistance,
            Level = plan.Level,
            DaysPerWeek = plan.DaysPerWeek,
            Unit = plan.Unit,
            RaceName = plan.RaceName,
            RaceDate = plan.RaceDate,
            TargetFinishTimeSeconds = plan.TargetFinishTimeSeconds,
            StartedAt = plan.StartedAt,
            EstimatedEndDate = plan.EstimatedEndDate,
            TotalWeeks = totalWeeks,
            CompletedWeeksCount = completedWeeksCount,
            TotalPlannedDistance = totalPlannedDistance,
            TotalCompletedDistance = totalCompletedDistance,
            Weeks = plan.Weeks.OrderBy(w => w.WeekNumber).Select(w => new PlanWeekDetailDto
            {
                WeekId = w.Id,
                WeekNumber = w.WeekNumber,
                WeekType = w.WeekType,
                PlannedVolumeKm = w.PlannedVolumeKm,
                ActualVolumeKm = w.ActualVolumeKm,
                IsRecoveryWeek = w.IsRecoveryWeek,
                StartDate = w.StartDate,
                Days = w.Days.OrderBy(d => d.Date).Select(d => new PlanDayDetailDto
                {
                    DayId = d.Id,
                    Date = d.Date,
                    DayType = d.DayType,
                    Status = d.Status,
                    Title = d.Title,
                    Description = d.Description,
                    PlannedDistanceKm = d.PlannedDistanceKm,
                    PlannedDurationMin = d.PlannedDurationMin,
                    PlannedPaceMinKm = d.PlannedPaceMinKm,
                    Intensity = d.Intensity,
                    ActualDistanceKm = d.ActualDistanceKm,
                    ActualDurationMin = d.ActualDurationMin,
                    IsLongRun = d.IsLongRun,
                    CanMarkComplete = d.CanMarkComplete,
                    CanMarkNotToday = d.CanMarkNotToday
                }).ToList()
            }).ToList()
        };

        return response;
    }

    private static double GetGoalDistanceInKm(GoalDistance distance)
    {
        return distance switch
        {
            GoalDistance.FiveK => 5.0,
            GoalDistance.TenK => 10.0,
            GoalDistance.HalfMarathon => 21.0975,
            GoalDistance.Marathon => 42.195,
            _ => 5.0
        };
    }

    private static string GetTitleForDay(TrainingDayType dayType, double distance)
    {
        return dayType switch
        {
            TrainingDayType.Easy => $"Easy {distance:0.#}k Run",
            TrainingDayType.Interval => $"Interval Session",
            TrainingDayType.Tempo => $"Tempo Run",
            TrainingDayType.LongRun => $"Long Run {distance:0.#}k",
            TrainingDayType.Rest => "Rest Day",
            TrainingDayType.RecoveryEasy => "Recovery Run",
            _ => "Training Session"
        };
    }

    private static string GetDescriptionForDay(TrainingDayType dayType, double distance)
    {
        return dayType switch
        {
            TrainingDayType.Easy => $"Run at a conversational, easy pace for {distance:0.#} km.",
            TrainingDayType.Interval => "Warm up, then run intervals at a fast pace, followed by recovery jogs.",
            TrainingDayType.Tempo => "Run at a steady, comfortably hard tempo pace.",
            TrainingDayType.LongRun => $"Build endurance with a steady, relaxed {distance:0.#} km run.",
            TrainingDayType.Rest => "Give your body time to adapt and get stronger today. No running.",
            TrainingDayType.RecoveryEasy => "A very short, very slow run to help flush out legs.",
            _ => "Follow the target workout parameters."
        };
    }

    // JSON parsing helper classes
    private class TemplateJsonData
    {
        public string TemplateId { get; set; } = string.Empty;
        public List<TemplateWeek> Weeks { get; set; } = new();
    }

    private class TemplateWeek
    {
        public int WeekNumber { get; set; }
        public TrainingWeekType WeekType { get; set; }
        public List<TemplateDay> Days { get; set; } = new();
    }

    private class TemplateDay
    {
        public int SlotIndex { get; set; }
        public TrainingDayType DayType { get; set; }
        public double DistanceKm { get; set; }
        public int DurationMin { get; set; }
        public string? Intensity { get; set; }
    }
}
