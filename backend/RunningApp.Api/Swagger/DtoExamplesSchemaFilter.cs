using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using RunningApp.Application.DTOs.Home;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.DTOs.Profile;
using RunningApp.Application.DTOs.TrainingDay;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;

namespace RunningApp.Api.Swagger;

/// <summary>
/// Attaches a realistic example payload to select RESPONSE DTOs the mobile
/// app actually consumes, so Swagger shows the real wire shape instead of
/// an empty/zeroed schema. Every field below matches a property that
/// exists on the corresponding DTO — nothing here is aspirational.
///
/// Deliberately does NOT attach an example to <see cref="GeneratePreviewRequest"/>
/// (or any other request DTO): its OpenAPI schema — required properties,
/// nullability, enum value lists, the nested RecentRace object, the
/// PreferredDays array — is generated purely from the DTO's own C# types
/// (<c>required</c> members, nullable reference/value types,
/// <see cref="System.Text.Json.Serialization.JsonConverterAttribute"/>
/// converters) and MUST stay that way. A hard-coded request example here
/// previously made Swagger look like a pre-filled acceptance-test payload
/// rather than a designed contract; a valid sample request now lives only
/// in <c>backend/RunningApp.Api/generate-preview.http</c> and in this
/// project's automated tests, neither of which can influence model
/// binding, defaults, or validation.
/// </summary>
public sealed class DtoExamplesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.Example = context.Type switch
        {
            Type t when t == typeof(GeneratePreviewResponse) => GeneratePreviewResponseExample(),
            Type t when t == typeof(ConfirmPlanRequest) => ConfirmPlanRequestExample(),
            Type t when t == typeof(ConfirmPlanResponse) => ConfirmPlanResponseExample(),
            Type t when t == typeof(LongHorizonPlanPreviewContract) => LongHorizonPreviewExample(),
            Type t when t == typeof(LongHorizonConfirmPlanRequest) => LongHorizonConfirmRequestExample(),
            Type t when t == typeof(LongHorizonConfirmPlanResponse) => LongHorizonConfirmResponseExample(),
            Type t when t == typeof(LongHorizonHomeResponse) => LongHorizonHomeExample(),
            Type t when t == typeof(LongHorizonCalendarResponse) => LongHorizonCalendarExample(),
            Type t when t == typeof(LongHorizonRollingSessionDetailResponse) => LongHorizonSessionDetailExample(),
            Type t when t == typeof(LongHorizonSessionMutationResponse) => LongHorizonMutationExample(),
            Type t when t == typeof(LongHorizonActivateNextWindowResponse) => LongHorizonActivateNextWindowExample(),
            Type t when t == typeof(LongHorizonRetryContinuationResponse) => LongHorizonRetryContinuationExample(),
            Type t when t == typeof(HomeResponse) => HomeResponseExample(),
            Type t when t == typeof(TrainingDayResponse) => TrainingDayResponseExample(),
            Type t when t == typeof(PlanDetailsResponse) => PlanDetailsResponseExample(),
            Type t when t == typeof(TrainingDayDetailResponse) => TrainingDayDetailResponseExample(),
            Type t when t == typeof(ProfileOverviewResponse) => ProfileOverviewResponseExample(),
            _ => schema.Example,
        };
    }

    private static IOpenApiAny GeneratePreviewResponseExample() => new OpenApiObject
    {
        ["preview_id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
        ["template_id"] = new OpenApiString("habit_5k_beginner_3day_km_v1"),
        ["goal_type"] = new OpenApiString("habit"),
        ["goal_distance"] = new OpenApiString("five_k"),
        ["level"] = new OpenApiString("beginner"),
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
                ["runway_block"] = new OpenApiNull(),
            },
        },
        ["fallback_used"] = new OpenApiBoolean(false),
        ["fallback_reason"] = new OpenApiNull(),
        ["lifecycle"] = new OpenApiString("core_confirmable"),
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

    private static OpenApiObject LongHorizonSessionExample() => new()
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["session_id"] = new OpenApiString("6c54a864-992d-42a0-8a7a-70bc9b9c0d41"),
        ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
        ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
        ["global_week"] = new OpenApiInteger(1),
        ["phase"] = new OpenApiString("general_endurance"),
        ["stage"] = new OpenApiString("GeneralEndurance"),
        ["assigned_date"] = new OpenApiString("2026-09-07"),
        ["workout_role"] = new OpenApiString("EASY_SUPPORT_1"),
        ["planned_distance_km"] = new OpenApiDouble(4.5),
        ["outcome"] = new OpenApiString("planned"),
        ["is_long_run"] = new OpenApiBoolean(false),
        ["mutation_allowed"] = new OpenApiBoolean(true),
        ["public_provenance"] = new OpenApiString("GeneratedFromInitialProfile")
    };

    private static IOpenApiAny LongHorizonHomeExample() => new OpenApiObject
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
        ["active_plan"] = new OpenApiObject
        {
            ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
            ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
            ["total_weeks"] = new OpenApiInteger(21),
            ["current_global_week"] = new OpenApiInteger(1),
            ["current_window_start_week"] = new OpenApiInteger(1),
            ["current_window_end_week"] = new OpenApiInteger(1),
            ["next_pending_global_week"] = new OpenApiInteger(2),
            ["checkpoint_readiness"] = new OpenApiString("current_window_in_progress"),
            ["public_message"] = new OpenApiString("long_horizon.current_window_in_progress")
        },
        ["today_workout"] = LongHorizonSessionExample(),
        ["next_executable_workout"] = new OpenApiNull(),
        ["current_window_sessions"] = new OpenApiArray { LongHorizonSessionExample() },
        ["has_pending_confirmations"] = new OpenApiBoolean(false)
    };

    private static IOpenApiAny LongHorizonCalendarExample() => new OpenApiObject
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
        ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
        ["month"] = new OpenApiString("2026-09"),
        ["sessions"] = new OpenApiArray { LongHorizonSessionExample() }
    };

    private static IOpenApiAny LongHorizonSessionDetailExample() => new OpenApiObject
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["session"] = LongHorizonSessionExample(),
        ["public_description"] = new OpenApiString("Complete the assigned session at the prescribed effort.")
    };

    private static IOpenApiAny LongHorizonMutationExample() => new OpenApiObject
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["session_id"] = new OpenApiString("6c54a864-992d-42a0-8a7a-70bc9b9c0d41"),
        ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
        ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
        ["outcome"] = new OpenApiString("completed"),
        ["outcome_version"] = new OpenApiInteger(1),
        ["checkpoint_readiness"] = new OpenApiString("current_window_in_progress"),
        ["next_window_activated"] = new OpenApiBoolean(false)
    };

    private static IOpenApiAny LongHorizonActivateNextWindowExample() => new OpenApiObject
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
        ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
        ["outcome"] = new OpenApiString("activated"),
        ["previous_window_range"] = new OpenApiObject { ["start_global_week"] = new OpenApiInteger(1), ["end_global_week"] = new OpenApiInteger(4) },
        ["activated_window_range"] = new OpenApiObject { ["start_global_week"] = new OpenApiInteger(5), ["end_global_week"] = new OpenApiInteger(8) },
        ["activated_global_weeks"] = new OpenApiArray { new OpenApiInteger(5), new OpenApiInteger(6), new OpenApiInteger(7), new OpenApiInteger(8) },
        ["next_pending_global_week"] = new OpenApiInteger(9),
        ["checkpoint_readiness"] = new OpenApiString("current_window_in_progress"),
        ["plan_status"] = new OpenApiString("Active"),
        ["is_terminal"] = new OpenApiBoolean(false),
        ["activated_at_utc"] = new OpenApiString("2026-09-05T12:00:00Z"),
        ["public_message"] = new OpenApiString("long_horizon.continuation_activated")
    };

    private static IOpenApiAny LongHorizonRetryContinuationExample() => new OpenApiObject
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
        ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
        ["outcome"] = new OpenApiString("restored_to_pending"),
        ["restored_window_range"] = new OpenApiObject { ["start_global_week"] = new OpenApiInteger(5), ["end_global_week"] = new OpenApiInteger(8) },
        ["current_window_range"] = new OpenApiObject { ["start_global_week"] = new OpenApiInteger(1), ["end_global_week"] = new OpenApiInteger(4) },
        ["next_pending_global_week"] = new OpenApiInteger(5),
        ["checkpoint_readiness"] = new OpenApiString("next_window_activation_ready"),
        ["plan_status"] = new OpenApiString("Active"),
        ["retried_at_utc"] = new OpenApiString("2026-09-06T09:00:00Z"),
        ["public_message"] = new OpenApiString("long_horizon.retry_restored_to_pending")
    };

    private static IOpenApiAny LongHorizonPreviewExample() => new OpenApiObject
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["preview_id"] = new OpenApiString("6f4c27ea-faf0-48a2-9dca-342a036dde52"),
        ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
        ["generated_at_utc"] = new OpenApiString("2026-08-04T12:00:00Z"),
        ["expires_at_utc"] = new OpenApiString("2026-08-04T12:30:00Z"),
        ["goal_type"] = new OpenApiString("Race"),
        ["goal_distance"] = new OpenApiString("TenK"),
        ["total_weeks"] = new OpenApiInteger(21),
        ["start_date"] = new OpenApiString("2026-09-07"),
        ["race_date"] = new OpenApiString("2027-02-01"),
        ["days_per_week"] = new OpenApiInteger(4),
        ["current_window_start_week"] = new OpenApiInteger(1),
        ["current_window_end_week"] = new OpenApiInteger(3),
        ["preview_readiness"] = new OpenApiString("ready_for_public_preview"),
        ["confirmation_readiness"] = new OpenApiString("ready_for_rolling_persistence"),
        ["structural_roadmap"] = new OpenApiArray
        {
            new OpenApiObject
            {
                ["global_week"] = new OpenApiInteger(1),
                ["phase"] = new OpenApiString("general_endurance"),
                ["lifecycle_status"] = new OpenApiString("available"),
                ["is_executable"] = new OpenApiBoolean(true),
                ["numeric_details_available"] = new OpenApiBoolean(true),
            },
            new OpenApiObject
            {
                ["global_week"] = new OpenApiInteger(4),
                ["phase"] = new OpenApiString("preparation_runway"),
                ["lifecycle_status"] = new OpenApiString("pending"),
                ["is_executable"] = new OpenApiBoolean(false),
                ["numeric_details_available"] = new OpenApiBoolean(false),
            },
        },
        ["current_executable_weeks"] = new OpenApiArray(),
    };

    private static IOpenApiAny LongHorizonConfirmRequestExample() => new OpenApiObject
    {
        ["preview_id"] = new OpenApiString("6f4c27ea-faf0-48a2-9dca-342a036dde52"),
        ["contract_version"] = new OpenApiInteger(1),
    };

    private static IOpenApiAny LongHorizonConfirmResponseExample() => new OpenApiObject
    {
        ["contract_version"] = new OpenApiInteger(1),
        ["plan_id"] = new OpenApiString("172637f1-f319-486e-a542-89c9d12ad3ac"),
        ["preview_id"] = new OpenApiString("6f4c27ea-faf0-48a2-9dca-342a036dde52"),
        ["outcome"] = new OpenApiString("confirmed"),
        ["schedule_strategy"] = new OpenApiString("rolling_long_horizon"),
        ["total_weeks"] = new OpenApiInteger(21),
        ["plan_status"] = new OpenApiString("active"),
        ["public_message"] = new OpenApiString("long_horizon.confirmed"),
    };

    private static IOpenApiAny HomeResponseExample() => new OpenApiObject
    {
        ["active_plan"] = new OpenApiObject
        {
            ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
            ["goal_type"] = new OpenApiString("habit"),
            ["goal_distance"] = new OpenApiString("five_k"),
            ["level"] = new OpenApiString("beginner"),
            ["progress_text"] = new OpenApiString("Week 1 of 1"),
            ["current_week_number"] = new OpenApiInteger(1),
            ["total_weeks"] = new OpenApiInteger(1),
            ["current_week_type"] = new OpenApiString("build"),
            ["current_runway_block"] = new OpenApiNull(),
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
        // Phase 4G.6D — a Core day: week_type is the actual persisted Core
        // phase, runway_block is always null. A Preparation Runway day would
        // instead show week_type="preparation_runway" and runway_block set
        // to the exact persisted block (e.g. "AEROBIC_STRENGTH").
        ["week_number"] = new OpenApiInteger(1),
        ["week_type"] = new OpenApiString("build"),
        ["runway_block"] = new OpenApiNull(),
    };

    private static IOpenApiAny PlanDetailsResponseExample() => new OpenApiObject
    {
        ["has_active_plan"] = new OpenApiBoolean(true),
        ["plan_id"] = new OpenApiString("8f14e45f-ceea-4abc-a743-8b1e3f6c1a2b"),
        ["template_id"] = new OpenApiString("habit_5k_beginner_3day_km_v1"),
        ["status"] = new OpenApiString("active"),
        ["goal_type"] = new OpenApiString("habit"),
        ["goal_distance"] = new OpenApiString("five_k"),
        ["level"] = new OpenApiString("beginner"),
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
        // Phase 4G.6D — real persisted enum value, never assumed to always be "template".
        obj["source"] = new OpenApiString("template");
        obj["adapted_from_id"] = new OpenApiNull();
        return obj;
    }

    private static IOpenApiAny ProfileOverviewResponseExample() => new OpenApiObject
    {
        ["name"] = new OpenApiString("Runner"),
        ["email"] = new OpenApiString("runner@example.com"),
        ["unit"] = new OpenApiString("km"),
        ["running_background"] = new OpenApiString("beginner"),
        ["active_plan_stats"] = new OpenApiObject
        {
            ["plan_name"] = new OpenApiString("Beginner FiveK Habit Plan"),
            ["goal_type"] = new OpenApiString("habit"),
            ["goal_distance"] = new OpenApiString("five_k"),
            ["completed_runs_count"] = new OpenApiInteger(0),
            ["total_planned_runs_count"] = new OpenApiInteger(2),
            ["total_completed_distance"] = new OpenApiDouble(0.0),
            ["adherence_rate_percent"] = new OpenApiDouble(0.0),
        },
    };
}
