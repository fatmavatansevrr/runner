using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class Phase4G5MPublicCalendarAdversarialTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Phase4G5MPublicCalendarAdversarialTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() =>
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();

    private static Dictionary<string, object?> Request(
        string startDate, int weeks, string[] preferredDays, string longRunDay) => new()
    {
        ["goal_distance"] = "ten_k",
        ["level"] = "intermediate",
        ["days_per_week"] = 4,
        ["unit"] = "km",
        ["start_date"] = startDate,
        ["preferred_days"] = preferredDays,
        ["long_run_day"] = longRunDay,
        ["race_name"] = "Phase 4G.5M calendar",
        ["race_date"] = DateOnly.Parse(startDate).AddDays(weeks * 7).ToString("yyyy-MM-dd"),
        ["target_finish_time_seconds"] = 3480,
        ["target_finish_time_source"] = "product_average",
        ["recent_weekly_volume_km"] = 20,
        ["recent_longest_run_km"] = 8,
        ["recent_runs_per_week"] = 3,
        ["recent_race"] = null,
    };

    [Theory]
    [InlineData("2026-07-20", 8, "mon,wed,fri,sun", "sun")]
    [InlineData("2026-07-20", 12, "mon,wed,fri,sun", "sun")]
    [InlineData("2026-07-20", 14, "mon,wed,fri,sun", "sun")]
    [InlineData("2026-07-22", 8, "wed,fri,sun,tue", "sun")]
    [InlineData("2026-07-22", 12, "wed,fri,sun,tue", "sun")]
    [InlineData("2026-07-22", 14, "wed,fri,sun,tue", "sun")]
    [InlineData("2026-08-30", 8, "sun,tue,thu,sat", "sat")]
    [InlineData("2026-08-30", 12, "sun,tue,thu,sat", "sat")]
    [InlineData("2026-08-30", 14, "sun,tue,thu,sat", "sat")]
    public async Task ValidStartDayAndPreferredDayMatrix_PreservesStartRelativeCalendarRules(
        string startText, int weeks, string preferredCsv, string longRunDay)
    {
        await ResetAsync();
        var start = DateOnly.Parse(startText);
        var preferred = preferredCsv.Split(',');
        var request = Request(startText, weeks, preferred, longRunDay);

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<JsonNode>())!;
        var publicWeeks = preview["weeks"]!.AsArray();
        Assert.Equal(weeks, publicWeeks.Count);
        Assert.Equal(start, DateOnly.Parse(publicWeeks[0]!["days"]!.AsArray().Min(d => d!["date"]!.GetValue<string>())![..10]));

        var preferredSet = preferred.Select(ParseDay).ToHashSet();
        foreach (var (week, index) in publicWeeks.Select((value, index) => (value!, index)))
        {
            var windowStart = start.AddDays(index * 7);
            var days = week["days"]!.AsArray();
            Assert.Equal(4, days.Count);
            Assert.Equal(4, days.Select(d => d!["date"]!.GetValue<string>()).Distinct().Count());
            Assert.All(days, day =>
            {
                var date = DateOnly.Parse(day!["date"]!.GetValue<string>()[..10]);
                Assert.InRange(date, windowStart, windowStart.AddDays(6));
                Assert.Contains(date.DayOfWeek, preferredSet);
            });
            var longRun = Assert.Single(days, d => d!["day_type"]!.GetValue<string>() == "long_run");
            Assert.Equal(ParseDay(longRunDay), DateOnly.Parse(longRun!["date"]!.GetValue<string>()[..10]).DayOfWeek);
        }

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new
        {
            preview_id = preview["preview_id"]!.GetValue<string>()
        });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);
        Assert.Equal(start.ToDateTime(TimeOnly.MinValue), plan.Weeks.Min(w => w.StartDate).Date);
        Assert.Equal(weeks, plan.Weeks.Count);
        Assert.All(plan.Weeks, week =>
        {
            var key = Assert.Single(week.Days, d => d.CatalogStructuralRole == "KEY_SESSION");
            var longRun = Assert.Single(week.Days, d => d.CatalogStructuralRole == "LONG_RUN");
            Assert.True(Math.Abs((key.Date - longRun.Date).TotalDays) >= 2);
        });
        Assert.Equal("TAPER", plan.Weeks.OrderBy(w => w.WeekNumber).Last().CatalogPhaseKey);
        Assert.True(plan.Weeks.SelectMany(w => w.Days).Max(d => d.Date).Date <= DateOnly.Parse((string)request["race_date"]!).ToDateTime(TimeOnly.MinValue));
        await ResetAsync();
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("wrong_count")]
    [InlineData("long_run_outside")]
    [InlineData("race_before_start")]
    public async Task InvalidCalendarRequest_FailsTypedAndPersistsNoSchedule(string kind)
    {
        await ResetAsync();
        var before = await CountsAsync();
        var request = Request("2026-07-20", 8, new[] { "mon", "wed", "fri", "sun" }, "sun");
        if (kind == "duplicate") request["preferred_days"] = new[] { "mon", "wed", "fri", "fri" };
        if (kind == "wrong_count") request["preferred_days"] = new[] { "mon", "wed", "fri" };
        if (kind == "long_run_outside") request["long_run_day"] = "sat";
        if (kind == "race_before_start") request["race_date"] = "2026-07-19";

        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity });
        var error = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Contains(error!["errorCode"]!.GetValue<string>(), new[] { "VALIDATION_ERROR", "CATALOG_LIVE_PILOT_REQUEST_UNSUPPORTED" });
        Assert.Null(error["weeks"]);
        Assert.Equal(before, await CountsAsync());
    }

    [Fact]
    public async Task InvalidWeekdayToken_IsRejectedByModelBinding_AndPersistsNoSchedule()
    {
        await ResetAsync();
        var before = await CountsAsync();
        var request = Request("2026-07-20", 8, new[] { "mon", "wed", "fri", "noday" }, "fri");
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await CountsAsync());
    }

    private async Task<(int previews, int plans, int weeks, int days)> CountsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.PlanPreviews.CountAsync(), await db.TrainingPlans.CountAsync(),
            await db.TrainingWeeks.CountAsync(), await db.TrainingDays.CountAsync());
    }

    private static DayOfWeek ParseDay(string value) => value switch
    {
        "mon" => DayOfWeek.Monday, "tue" => DayOfWeek.Tuesday,
        "wed" => DayOfWeek.Wednesday, "thu" => DayOfWeek.Thursday,
        "fri" => DayOfWeek.Friday, "sat" => DayOfWeek.Saturday,
        "sun" => DayOfWeek.Sunday, _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
}
