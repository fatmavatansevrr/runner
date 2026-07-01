using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using RunningApp.Application.DTOs.Home;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.DTOs.Profile;
using RunningApp.Application.DTOs.TrainingDay;

namespace RunningApp.Api.Swagger;

/// <summary>
/// Attaches a realistic example payload to the response/request DTOs the
/// mobile app actually consumes, so Swagger shows the real wire shape
/// instead of an empty/zeroed schema. Every field below matches a property
/// that exists on the corresponding DTO — nothing here is aspirational.
/// </summary>
public sealed class DtoExamplesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.Example = context.Type switch
        {
            Type t when t == typeof(GeneratePreviewRequest) => GeneratePreviewRequestExample(),
            Type t when t == typeof(GeneratePreviewResponse) => GeneratePreviewResponseExample(),
            Type t when t == typeof(ConfirmPlanRequest) => ConfirmPlanRequestExample(),
            Type t when t == typeof(ConfirmPlanResponse) => ConfirmPlanResponseExample(),
            Type t when t == typeof(HomeResponse) => HomeResponseExample(),
            Type t when t == typeof(TrainingDayResponse) => TrainingDayResponseExample(),
            Type t when t == typeof(PlanDetailsResponse) => PlanDetailsResponseExample(),
            Type t when t == typeof(TrainingDayDetailResponse) => TrainingDayDetailResponseExample(),
            Type t when t == typeof(ProfileOverviewResponse) => ProfileOverviewResponseExample(),
            _ => schema.Example,
        };
    }

    private static IOpenApiAny GeneratePreviewRequestExample() => new OpenApiObject
    {
        ["goal_type"] = new OpenApiString("habit"),
        ["goal_distance"] = new OpenApiString("five_k"),
        ["level"] = new OpenApiString("new_to_running"),
        ["days_per_week"] = new OpenApiInteger(3),
        ["unit"] = new OpenApiString("km"),
        ["race_name"] = new OpenApiNull(),
        ["race_date"] = new OpenApiNull(),
        ["target_finish_time_seconds"] = new OpenApiNull(),
    };

    private static IOpenApiAny GeneratePreviewResponseExample() => new OpenApiObject
    {
        ["preview_id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
        ["template_id"] = new OpenApiString("habit_5k_beginner_3day_km_v1"),
        ["goal_type"] = new OpenApiString("habit"),
        ["goal_distance"] = new OpenApiString("five_k"),
        ["level"] = new OpenApiString("new_to_running"),
        ["days_per_week"] = new OpenApiInteger(3),
        ["unit"] = new OpenApiString("km"),
        ["weeks"] = new OpenApiArray
        {
            new OpenApiObject
            {
                ["week_number"] = new OpenApiInteger(1),
                ["week_type"] = new OpenApiString("build"),
                ["days"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["slot_index"] = new OpenApiInteger(1),
                        ["day_type"] = new OpenApiString("easy"),
                        ["distance_km"] = new OpenApiDouble(2.0),
                        ["duration_min"] = new OpenApiInteger(20),
                        ["intensity"] = new OpenApiString("z2"),
                        ["date"] = new OpenApiString("2026-07-06T00:00:00Z"),
                    },
                },
            },
        },
        ["fallback_used"] = new OpenApiBoolean(false),
        ["fallback_reason"] = new OpenApiNull(),
    };

    private static IOpenApiAny ConfirmPlanRequestExample() => new OpenApiObject
    {
        ["preview_id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    };

    private static IOpenApiAny ConfirmPlanResponseExample() => new OpenApiObject
    {
        ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
        ["status"] = new OpenApiString("active"),
        ["already_active"] = new OpenApiBoolean(false),
    };

    private static IOpenApiAny HomeResponseExample() => new OpenApiObject
    {
        ["active_plan"] = new OpenApiObject
        {
            ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
            ["goal_type"] = new OpenApiString("habit"),
            ["goal_distance"] = new OpenApiString("five_k"),
            ["level"] = new OpenApiString("new_to_running"),
            ["progress_text"] = new OpenApiString("Week 1 of 1"),
        },
        ["today_workout"] = (OpenApiObject)TrainingDayResponseExample(),
        ["daily_tip"] = new OpenApiObject
        {
            ["tip_key"] = new OpenApiString("easy_run_tip_01"),
            ["title"] = new OpenApiString("Keep it comfortable"),
            ["message"] = new OpenApiString("Today is about showing up, not pushing hard."),
            ["workout_type"] = new OpenApiString("easy"),
        },
        ["week_summary"] = new OpenApiArray { (OpenApiObject)TrainingDayResponseExample() },
        ["has_pending_confirmations"] = new OpenApiBoolean(false),
    };

    private static IOpenApiAny TrainingDayResponseExample() => new OpenApiObject
    {
        ["day_id"] = new OpenApiString("c1a2b3c4-d5e6-4f70-8a9b-0c1d2e3f4a5b"),
        ["date"] = new OpenApiString("2026-07-06T00:00:00Z"),
        ["day_type"] = new OpenApiString("easy"),
        ["status"] = new OpenApiString("planned"),
        ["title"] = new OpenApiString("Easy 2k Run"),
        ["description"] = new OpenApiString("Run at a conversational, easy pace for 2 km."),
        ["planned_distance_km"] = new OpenApiDouble(2.0),
        ["planned_duration_min"] = new OpenApiInteger(20),
        ["planned_pace_min_km"] = new OpenApiDouble(10.0),
        ["intensity"] = new OpenApiString("z2"),
        ["actual_distance_km"] = new OpenApiNull(),
        ["actual_duration_min"] = new OpenApiNull(),
        ["is_long_run"] = new OpenApiBoolean(false),
        ["can_mark_complete"] = new OpenApiBoolean(true),
        ["can_mark_not_today"] = new OpenApiBoolean(true),
    };

    private static IOpenApiAny PlanDetailsResponseExample() => new OpenApiObject
    {
        ["has_active_plan"] = new OpenApiBoolean(true),
        ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
        ["template_id"] = new OpenApiString("habit_5k_beginner_3day_km_v1"),
        ["status"] = new OpenApiString("active"),
        ["goal_type"] = new OpenApiString("habit"),
        ["goal_distance"] = new OpenApiString("five_k"),
        ["level"] = new OpenApiString("new_to_running"),
        ["days_per_week"] = new OpenApiInteger(3),
        ["unit"] = new OpenApiString("km"),
        ["race_name"] = new OpenApiNull(),
        ["race_date"] = new OpenApiNull(),
        ["target_finish_time_seconds"] = new OpenApiNull(),
        ["started_at"] = new OpenApiString("2026-07-06T00:00:00Z"),
        ["estimated_end_date"] = new OpenApiString("2026-07-13T00:00:00Z"),
        ["total_weeks"] = new OpenApiInteger(1),
        ["completed_weeks_count"] = new OpenApiInteger(0),
        ["total_planned_distance"] = new OpenApiDouble(7.5),
        ["total_completed_distance"] = new OpenApiDouble(0.0),
        ["weeks"] = new OpenApiArray
        {
            new OpenApiObject
            {
                ["week_id"] = new OpenApiString("a1b2c3d4-e5f6-4789-9abc-def012345678"),
                ["week_number"] = new OpenApiInteger(1),
                ["week_type"] = new OpenApiString("build"),
                ["planned_volume_km"] = new OpenApiDouble(7.5),
                ["actual_volume_km"] = new OpenApiDouble(0.0),
                ["is_recovery_week"] = new OpenApiBoolean(false),
                ["start_date"] = new OpenApiString("2026-07-06T00:00:00Z"),
                ["days"] = new OpenApiArray { (OpenApiObject)TrainingDayResponseExample() },
            },
        },
    };

    private static IOpenApiAny TrainingDayDetailResponseExample()
    {
        var obj = (OpenApiObject)TrainingDayResponseExample();
        obj["completed_at"] = new OpenApiNull();
        return obj;
    }

    private static IOpenApiAny ProfileOverviewResponseExample() => new OpenApiObject
    {
        ["name"] = new OpenApiString("Runner"),
        ["email"] = new OpenApiString("runner@example.com"),
        ["unit"] = new OpenApiString("km"),
        ["running_background"] = new OpenApiString("new_to_running"),
        ["active_plan_stats"] = new OpenApiObject
        {
            ["plan_name"] = new OpenApiString("NewToRunning FiveK Habit Plan"),
            ["goal_type"] = new OpenApiString("habit"),
            ["goal_distance"] = new OpenApiString("five_k"),
            ["completed_runs_count"] = new OpenApiInteger(0),
            ["total_planned_runs_count"] = new OpenApiInteger(2),
            ["total_completed_distance"] = new OpenApiDouble(0.0),
            ["adherence_rate_percent"] = new OpenApiDouble(0.0),
        },
    };
}
