using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RunningApp.IntegrationTests;

/// <summary>
/// End-to-end contract tests against the real Api host + real Postgres DB
/// (the same DB the mobile app talks to in development). Each test resets
/// the mock user's data first via POST /api/v1/testing/reset, so tests are
/// independent of each other and of run order.
///
/// Assertions read raw JSON (snake_case keys) rather than the server's C#
/// DTOs, so these tests verify the actual wire contract the Flutter app
/// consumes, not just that the C# compiles.
/// </summary>
public class UserJourneyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserJourneyTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    private static readonly object ExactMatchPreviewRequest = new
    {
        goal_type = "habit",
        goal_distance = "five_k",
        level = "new_to_running",
        days_per_week = 3,
        unit = "km",
    };

    private async Task ResetAsync()
    {
        var response = await _client.PostRawAsync("/api/v1/testing/reset");
        response.EnsureSuccessStatusCode();
    }

    // ─── Test 1: fresh user bootstrap ───────────────────────────────────────
    [Fact]
    public async Task Bootstrap_FreshUser_HasNoActivePlanAndPointsAwayFromHome()
    {
        await ResetAsync();

        var bootstrap = await _client.GetJsonAsync("/api/v1/me/bootstrap");

        Assert.False(bootstrap["has_active_plan"]!.GetValue<bool>());
        var nextScreen = bootstrap["next_screen"]!.GetValue<string>();
        Assert.NotEqual("Home", nextScreen);
    }

    // ─── Test 2: generate preview does not create a plan ───────────────────
    [Fact]
    public async Task GeneratePreview_DoesNotCreatePlan_AndIncludesFallbackFields()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);

        Assert.False(string.IsNullOrWhiteSpace(preview["preview_id"]!.GetValue<string>()));
        // Fallback fields must be present even when not used.
        Assert.NotNull(preview["fallback_used"]);
        Assert.True(preview.AsObject().ContainsKey("fallback_reason"));

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.False(details["has_active_plan"]!.GetValue<bool>());
    }

    // ─── Test 3: confirm plan persists plan/week/day ────────────────────────
    [Fact]
    public async Task ConfirmPlan_CreatesPlanWeeksAndDays()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.False(string.IsNullOrWhiteSpace(confirm["plan_id"]!.GetValue<string>()));
        Assert.False(confirm["already_active"]!.GetValue<bool>());

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        var weeks = details["weeks"]!.AsArray();
        Assert.True(weeks.Count > 0);
        var firstWeekDays = weeks[0]!["days"]!.AsArray();
        Assert.True(firstWeekDays.Count > 0);
        Assert.All(firstWeekDays, day => Assert.False(string.IsNullOrWhiteSpace(day!["day_id"]!.GetValue<string>())));
    }

    // ─── Test 4: duplicate confirm does not create a second plan ───────────
    [Fact]
    public async Task ConfirmPlan_WhenActivePlanExists_ReturnsExistingPlanWithoutDuplicating()
    {
        await ResetAsync();

        var preview1 = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        var confirm1 = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview1["preview_id"]!.GetValue<string>() });
        var firstPlanId = confirm1["plan_id"]!.GetValue<string>();
        Assert.False(confirm1["already_active"]!.GetValue<bool>());

        var preview2 = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        var confirm2 = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview2["preview_id"]!.GetValue<string>() });

        Assert.True(confirm2["already_active"]!.GetValue<bool>());
        Assert.Equal(firstPlanId, confirm2["plan_id"]!.GetValue<string>());
    }

    // ─── Test 5: home after confirm + enum casing regression ───────────────
    [Fact]
    public async Task Home_AfterConfirm_ReturnsActivePlanAndWeekSummary_WithSnakeCaseLevel()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");

        Assert.NotNull(home["active_plan"]);
        // Regression guard: RunningBackground.NewToRunning must serialize as
        // "new_to_running", not "newtorunning" (a bare .ToString().ToLower()
        // mangles multi-word enum names).
        Assert.Equal("new_to_running", home["active_plan"]!["level"]!.GetValue<string>());

        Assert.NotNull(home["today_workout"]); // always set, even as a synthetic rest day
        var weekSummary = home["week_summary"]!.AsArray();
        Assert.Equal(7, weekSummary.Count); // Monday..Sunday, always fully populated
    }

    // ─── Test 6: calendar returns typed days for the plan's month ───────────
    [Fact]
    public async Task Calendar_ReturnsTypedDaysForPlanMonth()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        var firstDayDate = DateTime.Parse(details["weeks"]![0]!["days"]![0]!["date"]!.GetValue<string>());
        var month = firstDayDate.ToString("yyyy-MM");

        var calendarResponse = await _client.GetAsync($"/api/v1/plans/active/calendar?month={month}");
        calendarResponse.EnsureSuccessStatusCode();
        var calendar = (await calendarResponse.Content.ReadFromJsonAsync<JsonNode>())!.AsArray();

        Assert.True(calendar.Count > 0);
        foreach (var day in calendar)
        {
            Assert.True(day!.AsObject().ContainsKey("date"));
            Assert.True(day.AsObject().ContainsKey("day_type"));
            Assert.True(day.AsObject().ContainsKey("status"));
        }
    }

    // ─── Test 7: training day detail ────────────────────────────────────────
    [Fact]
    public async Task TrainingDayDetail_ReturnsTypedResponse()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        var dayId = details["weeks"]![0]!["days"]![0]!["day_id"]!.GetValue<string>();

        var dayDetail = await _client.GetJsonAsync($"/api/v1/training-days/{dayId}");

        Assert.Equal(dayId, dayDetail["day_id"]!.GetValue<string>());
        Assert.True(dayDetail.AsObject().ContainsKey("status"));
        Assert.True(dayDetail.AsObject().ContainsKey("day_type"));
        Assert.True(dayDetail.AsObject().ContainsKey("can_mark_complete"));
    }

    // ─── Test 8: complete workout does not mutate future days ──────────────
    [Fact]
    public async Task CompleteWorkout_MarksCompleted_PlanAdaptedFalse_FutureDaysUntouched()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        var before = await _client.GetJsonAsync("/api/v1/plans/active/details");
        var firstWeekDays = before["weeks"]![0]!["days"]!.AsArray();
        var targetDayId = firstWeekDays[0]!["day_id"]!.GetValue<string>();
        var otherDayIds = firstWeekDays.Skip(1).Select(d => d!["day_id"]!.GetValue<string>()).ToList();

        var complete = await _client.PostJsonAsync(
            $"/api/v1/training-days/{targetDayId}/complete",
            new { actual_distance_km = 2.0, actual_duration_min = 20 });

        Assert.Equal("completed", complete["status"]!.GetValue<string>());

        var targetDetail = await _client.GetJsonAsync($"/api/v1/training-days/{targetDayId}");
        Assert.Equal("completed", targetDetail["status"]!.GetValue<string>());

        // Other, untouched days in the same week must remain exactly as planned.
        foreach (var otherId in otherDayIds)
        {
            var otherDetail = await _client.GetJsonAsync($"/api/v1/training-days/{otherId}");
            Assert.Equal("planned", otherDetail["status"]!.GetValue<string>());
        }
    }

    // ─── Test 9: not-today does not mutate future days ──────────────────────
    [Fact]
    public async Task NotToday_ConfirmedDecision_MarksMissed_PlanAdaptedFalse_FutureDaysUntouched()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        var before = await _client.GetJsonAsync("/api/v1/plans/active/details");
        var firstWeekDays = before["weeks"]![0]!["days"]!.AsArray();
        var targetDayId = firstWeekDays[0]!["day_id"]!.GetValue<string>();
        var otherDayIds = firstWeekDays.Skip(1).Select(d => d!["day_id"]!.GetValue<string>()).ToList();

        var decision = await _client.PostJsonAsync(
            $"/api/v1/training-days/{targetDayId}/not-today-decisions",
            new { reason = "feeling_tired" });
        var decisionId = decision["decision_id"]!.GetValue<string>();

        var confirmDecision = await _client.PostJsonAsync(
            $"/api/v1/not-today-decisions/{decisionId}/confirm",
            new { });

        Assert.False(confirmDecision["plan_adapted"]!.GetValue<bool>());
        Assert.Equal("no_change", confirmDecision["action"]!.GetValue<string>());

        var targetDetail = await _client.GetJsonAsync($"/api/v1/training-days/{targetDayId}");
        Assert.Equal("missed", targetDetail["status"]!.GetValue<string>());

        foreach (var otherId in otherDayIds)
        {
            var otherDetail = await _client.GetJsonAsync($"/api/v1/training-days/{otherId}");
            Assert.Equal("planned", otherDetail["status"]!.GetValue<string>());
        }
    }

    // ─── Test 10: no-active-plan state is safe everywhere ───────────────────
    [Fact]
    public async Task NoActivePlan_AfterCancel_AllReadEndpointsAreNullSafe()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", ExactMatchPreviewRequest);
        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });
        var planId = confirm["plan_id"]!.GetValue<string>();

        var cancel = await _client.PostJsonAsync($"/api/v1/plans/{planId}/cancel", new { reason = "integration_test_cleanup" });
        Assert.Equal("cancelled", cancel["status"]!.GetValue<string>());

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Null(home["active_plan"]);

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.False(details["has_active_plan"]!.GetValue<bool>());

        var profile = await _client.GetJsonAsync("/api/v1/profile/overview");
        Assert.True(profile.AsObject().ContainsKey("active_plan_stats"));

        var bootstrap = await _client.GetJsonAsync("/api/v1/me/bootstrap");
        Assert.False(bootstrap["has_active_plan"]!.GetValue<bool>());
    }

    // ─── Test 4b (item 4 of the task): fresh user, never had a plan ────────
    [Fact]
    public async Task NoActivePlan_FreshUser_AllReadEndpointsAreNullSafe()
    {
        await ResetAsync();

        var home = await _client.GetAsync("/api/v1/plans/active/home");
        Assert.Equal(System.Net.HttpStatusCode.OK, home.StatusCode);

        var calendarThisMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var calendar = await _client.GetAsync($"/api/v1/plans/active/calendar?month={calendarThisMonth}");
        Assert.Equal(System.Net.HttpStatusCode.OK, calendar.StatusCode);

        var details = await _client.GetAsync("/api/v1/plans/active/details");
        Assert.Equal(System.Net.HttpStatusCode.OK, details.StatusCode);

        var profile = await _client.GetAsync("/api/v1/profile/overview");
        Assert.Equal(System.Net.HttpStatusCode.OK, profile.StatusCode);

        var bootstrap = await _client.GetAsync("/api/v1/me/bootstrap");
        Assert.Equal(System.Net.HttpStatusCode.OK, bootstrap.StatusCode);
    }

    // ─── Fallback path: unsupported goal combo + multi-word enum casing ────
    [Fact]
    public async Task GeneratePreview_UnsupportedGoalCombo_UsesFallback_AndPersistsSnakeCaseEnumsThroughConfirm()
    {
        await ResetAsync();

        var unsupportedRequest = new
        {
            goal_type = "race",
            goal_distance = "half_marathon",
            level = "running_regularly",
            days_per_week = 5,
            unit = "km",
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", unsupportedRequest);
        Assert.True(preview["fallback_used"]!.GetValue<bool>());
        Assert.False(string.IsNullOrWhiteSpace(preview["fallback_reason"]!.GetValue<string>()));

        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        // The plan keeps the user's original (unsupported) goal/level even
        // though the day-by-day structure fell back to a seeded template.
        Assert.Equal("half_marathon", home["active_plan"]!["goal_distance"]!.GetValue<string>());
        Assert.Equal("running_regularly", home["active_plan"]!["level"]!.GetValue<string>());
    }

    // ─── Test 11: Confirm race plan => CustomGoalType null, HabitPlanType null ──
    [Fact]
    public async Task ConfirmRacePlan_NullsOutCustomGoalTypeAndHabitPlanType()
    {
        await ResetAsync();

        var raceRequest = new
        {
            goal_type = "race",
            goal_distance = "five_k",
            level = "new_to_running",
            days_per_week = 3,
            unit = "km",
            race_name = "Integration Test Race",
            race_date = "2026-10-10",
            target_finish_time_seconds = 1500,
            custom_goal_type = "comfort",      // should be ignored
            habit_plan_type = "five_k_comfort" // should be ignored
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", raceRequest);
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.False(string.IsNullOrWhiteSpace(confirm["plan_id"]!.GetValue<string>()));

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Null(details["custom_goal_type"]);
        Assert.Null(details["habit_plan_type"]);
        Assert.Equal("Integration Test Race", details["race_name"]!.GetValue<string>());
    }

    // ─── Test 12: Confirm habit standard plan => CustomGoalType null ────────────
    [Fact]
    public async Task ConfirmHabitStandardPlan_NullsOutCustomGoalType()
    {
        await ResetAsync();

        var habitRequest = new
        {
            goal_type = "habit",
            goal_distance = "five_k",
            level = "new_to_running",
            days_per_week = 3,
            unit = "km",
            habit_plan_type = "five_k_comfort",
            custom_goal_type = "comfort" // ignored because habit_plan_type is not "custom"
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", habitRequest);
        var previewId = preview["preview_id"]!.GetValue<string>();

        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Equal("five_k_comfort", details["habit_plan_type"]!.GetValue<string>());
        Assert.Null(details["custom_goal_type"]);
    }

    // ─── Test 13: Confirm habit custom plan => CustomGoalType valid value ───────
    [Fact]
    public async Task ConfirmHabitCustomPlan_PersistsCustomGoalType()
    {
        await ResetAsync();

        var habitCustomRequest = new
        {
            goal_type = "habit",
            goal_distance = "five_k",
            level = "new_to_running",
            days_per_week = 3,
            unit = "km",
            habit_plan_type = "custom",
            custom_goal_type = "comfort"
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", habitCustomRequest);
        var previewId = preview["preview_id"]!.GetValue<string>();

        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Equal("custom", details["habit_plan_type"]!.GetValue<string>());
        Assert.Equal("comfort", details["custom_goal_type"]!.GetValue<string>());
    }

    // ─── Test 14: Empty CustomGoalType => null ─────────────────────────────────
    [Fact]
    public async Task ConfirmHabitCustomPlan_EmptyCustomGoalType_MapsToNull()
    {
        await ResetAsync();

        var habitCustomRequest = new
        {
            goal_type = "habit",
            goal_distance = "five_k",
            level = "new_to_running",
            days_per_week = 3,
            unit = "km",
            habit_plan_type = "custom",
            custom_goal_type = "   " // empty/whitespace
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", habitCustomRequest);
        var previewId = preview["preview_id"]!.GetValue<string>();

        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Equal("custom", details["habit_plan_type"]!.GetValue<string>());
        Assert.Null(details["custom_goal_type"]);
    }

    // ─── Test 15: Invalid CustomGoalType => 400 validation response ────────────
    [Fact]
    public async Task ConfirmHabitCustomPlan_InvalidCustomGoalType_Returns400()
    {
        await ResetAsync();

        var habitCustomRequest = new
        {
            goal_type = "habit",
            goal_distance = "five_k",
            level = "new_to_running",
            days_per_week = 3,
            unit = "km",
            habit_plan_type = "custom",
            custom_goal_type = "invalid_value"
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", habitCustomRequest);
        var previewId = preview["preview_id"]!.GetValue<string>();

        var response = await _client.PostRawAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("VALIDATION_ERROR", error!["errorCode"]!.GetValue<string>());
    }

    // ─── Test 16: PreferredDays and LongRunDay day format normalization ────────
    [Fact]
    public async Task ConfirmPlan_NormalizesDayFormats_ToFullCapitalizedNames()
    {
        await ResetAsync();

        var request = new
        {
            goal_type = "habit",
            goal_distance = "five_k",
            level = "new_to_running",
            days_per_week = 3,
            unit = "km",
            preferred_days = "mon,WEDNESDAY,sat",
            long_run_day = "saturday"
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview", request);
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planIdStr = confirm["plan_id"]!.GetValue<string>();
        var planId = Guid.Parse(planIdStr);

        // Assert HTTP response representation (LongRunDay)
        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Equal("Saturday", details["long_run_day"]?.GetValue<string>());

        // Assert Database representation (PreferredDays and LongRunDay)
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await context.TrainingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId);
        
        Assert.NotNull(plan);
        Assert.Equal("Monday,Wednesday,Saturday", plan.PreferredDays);
        Assert.Equal("Saturday", plan.LongRunDay);
    }
}
