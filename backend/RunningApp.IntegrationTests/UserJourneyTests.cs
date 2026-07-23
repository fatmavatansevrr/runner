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
[Collection(ApiIntegrationTestCollection.Name)]
public class UserJourneyTests
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
        goal_distance = "five_k",
        level = "beginner",
        days_per_week = 3,
        unit = "km",
        start_date = "2026-07-20",
        preferred_days = new[] { "mon", "wed", "fri" },
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

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);

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

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
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

        var preview1 = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
        var confirm1 = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview1["preview_id"]!.GetValue<string>() });
        var firstPlanId = confirm1["plan_id"]!.GetValue<string>();
        Assert.False(confirm1["already_active"]!.GetValue<bool>());

        var preview2 = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
        var confirm2 = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview2["preview_id"]!.GetValue<string>() });

        Assert.True(confirm2["already_active"]!.GetValue<bool>());
        Assert.Equal(firstPlanId, confirm2["plan_id"]!.GetValue<string>());
    }

    // ─── Test 5: home after confirm + enum casing regression ───────────────
    [Fact]
    public async Task Home_AfterConfirm_ReturnsActivePlanAndWeekSummary_WithSnakeCaseLevel()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = preview["preview_id"]!.GetValue<string>() });

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");

        Assert.NotNull(home["active_plan"]);
        // Regression guard: the response echoes the canonical Running
        // Background V2.1 wire value "beginner" in snake_case. Legacy
        // aliases (new_to_running/used_to_run/running_regularly) are no
        // longer accepted at the public request boundary at all — see
        // RunningBackgroundV2_1Tests for that rejection behavior.
        Assert.Equal("beginner", home["active_plan"]!["level"]!.GetValue<string>());

        Assert.NotNull(home["today_workout"]); // always set, even as a synthetic rest day
        var weekSummary = home["week_summary"]!.AsArray();
        Assert.Equal(7, weekSummary.Count); // Monday..Sunday, always fully populated
    }

    // ─── Test 6: calendar returns typed days for the plan's month ───────────
    [Fact]
    public async Task Calendar_ReturnsTypedDaysForPlanMonth()
    {
        await ResetAsync();

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
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

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
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

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
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

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
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

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", ExactMatchPreviewRequest);
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

    // ─── Backend Integration Phase 0: unsupported goal combo => explicit 404, no silent fallback ────
    // Was: GeneratePreview_UnsupportedGoalCombo_UsesFallback_AndPersistsSnakeCaseEnumsThroughConfirm.
    // PlaceholderPlanGenerationEngine no longer falls back to an arbitrary seeded template for an
    // unsupported (GoalType, GoalDistance, Level, DaysPerWeek) combination — it now fails loudly with
    // PLAN_TEMPLATE_NOT_FOUND instead of silently substituting an unrelated plan.
    [Fact]
    public async Task GeneratePreview_UnsupportedGoalCombo_ReturnsPlanTemplateNotFound_NoSilentFallback()
    {
        await ResetAsync();

        var unsupportedRequest = new
        {
            goal_distance = "half_marathon",
            level = "intermediate",
            days_per_week = 5,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = new[] { "mon", "tue", "wed", "fri", "sun" },
            long_run_day = "sun",
            race_date = "2026-10-12",
            target_finish_time_seconds = 7200,
            target_finish_time_source = "user_defined",
        };

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", unsupportedRequest);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("PLAN_TEMPLATE_NOT_FOUND", error!["errorCode"]!.GetValue<string>());
    }

    // ─── Test 11: Confirm race plan => CustomGoalType null, HabitPlanType null ──
    [Fact]
    public async Task ConfirmRacePlan_NullsOutCustomGoalTypeAndHabitPlanType()
    {
        await ResetAsync();

        var raceRequest = new
        {
            goal_distance = "five_k",
            level = "beginner",
            days_per_week = 3,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = new[] { "mon", "wed", "fri" },
            long_run_day = "fri",
            race_name = "Integration Test Race",
            race_date = "2026-10-10",
            target_finish_time_seconds = 1500,
            target_finish_time_source = "user_defined",
            custom_goal_type = "comfort",      // should be ignored -- no such property on GenerateRacePlanPreviewRequest
            habit_plan_type = "five_k_comfort" // should be ignored -- no such property on GenerateRacePlanPreviewRequest
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/race", raceRequest);
        var previewId = preview["preview_id"]!.GetValue<string>();

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        Assert.False(string.IsNullOrWhiteSpace(confirm["plan_id"]!.GetValue<string>()));

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Null(details["custom_goal_type"]);
        Assert.Null(details["habit_plan_type"]);
        Assert.Equal("Integration Test Race", details["race_name"]!.GetValue<string>());
    }

    // ─── Test 12: habit_plan_type/custom_goal_type no longer exist on the ──────
    // ─── public habit contract -- sending them is silently ignored, confirm ────
    // ─── always nulls both out (superseded Tests 12-15, which exercised the ───
    // ─── now-removed custom-habit-goal branch; see plan doc: custom generation ─
    // ─── was already vestigial/unreachable for real Flutter users). ────────────
    [Fact]
    public async Task ConfirmHabitPlan_HabitPlanTypeAndCustomGoalTypeFields_AreIgnored_AndNulledOutOnConfirm()
    {
        await ResetAsync();

        var habitRequestWithStaleFields = new
        {
            goal_distance = "five_k",
            level = "beginner",
            days_per_week = 3,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = new[] { "mon", "wed", "fri" },
            habit_plan_type = "custom",      // no such property on GenerateHabitPlanPreviewRequest -- ignored
            custom_goal_type = "comfort",    // no such property on GenerateHabitPlanPreviewRequest -- ignored
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", habitRequestWithStaleFields);
        var previewId = preview["preview_id"]!.GetValue<string>();

        await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.True(details["has_active_plan"]!.GetValue<bool>());
        Assert.Null(details["habit_plan_type"]);
        Assert.Null(details["custom_goal_type"]);
    }

    // ─── Test 16: PreferredDays and LongRunDay day format normalization ────────
    // NOTE: PreferredDays/LongRunDay are now strictly-typed Weekday values at
    // the wire boundary (canonical lowercase 3-letter tokens only, e.g. "mon",
    // "sat" -- see GeneratePreviewRequest/Weekday), so the request itself can
    // no longer send loose formats like "mon,WEDNESDAY,sat" or "saturday".
    // This test's remaining, still-valid intent is the OUTPUT side: proving
    // the typed enum still normalizes to full-capitalized display/DB strings
    // ("Saturday", "Monday,Wednesday,Saturday") downstream of confirm.
    [Fact]
    public async Task ConfirmPlan_NormalizesDayFormats_ToFullCapitalizedNames()
    {
        await ResetAsync();

        var request = new
        {
            goal_distance = "five_k",
            level = "beginner",
            days_per_week = 3,
            unit = "km",
            start_date = "2026-07-20",
            preferred_days = new[] { "mon", "wed", "sat" },
            long_run_day = "sat"
        };

        var preview = await _client.PostJsonAsync("/api/v1/plans/generate-preview/habit", request);
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
