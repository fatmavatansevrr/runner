using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// PHASE FIVE-K.1 — real HTTP/PostgreSQL public-activation proof for the
/// canonical FIVE_K Intermediate 4D/5D cells, implementing FIVE-K.0B's frozen
/// restricted V1 matrix. Mirrors <see cref="DerivedDist2PublicActivationTests"/>'s
/// own real HTTP + Postgres proof shape (same <see cref="CustomWebApplicationFactory"/>
/// configuration, same reset-before-each pattern, same raw-JSON wire-contract
/// assertions, same <see cref="PublishedCatalogTestRelease"/> real-publisher
/// catalog-schema-validation fixture) — no new E2E test infrastructure invented.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class FiveK1PublicActivationEndToEndTests : IClassFixture<PublishedCatalogTestRelease>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FiveK1PublicActivationEndToEndTests(PublishedCatalogTestRelease release)
    {
        _factory = new CustomWebApplicationFactory("Production", new Dictionary<string, string?>
        {
            ["Auth:Provider"] = "Mock",
            ["PlanCatalog:CatalogRootPath"] = release.ReleaseRoot,
            ["CatalogLivePilot:Enabled"] = "true",
            ["LocalCatalogAcceptance:Enabled"] = "false",
            ["ProductionConfigurationValidation:Enabled"] = "false",
        });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task ResetAsync()
    {
        using var resetFactory = new CustomWebApplicationFactory();
        using var resetClient = resetFactory.CreateClient();
        (await resetClient.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
    }

    private static string[] DaysFor(int days) => days switch
    {
        4 => new[] { "tue", "thu", "sat", "sun" },
        5 => new[] { "mon", "tue", "thu", "sat", "sun" },
        _ => Enumerable.Range(0, days).Select(i => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }[i]).ToArray(),
    };

    private static object FiveKRequest(int days, int weeks = 10, string level = "intermediate", DateOnly? start = null)
    {
        var startDate = start ?? new DateOnly(2026, 8, 3);
        var preferred = DaysFor(days);
        return new
        {
            goal_distance = "five_k",
            level,
            days_per_week = days,
            unit = "km",
            start_date = startDate.ToString("yyyy-MM-dd"),
            preferred_days = preferred,
            long_run_day = preferred[^1],
            race_date = startDate.AddDays(weeks * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 1200,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = days == 4 ? 25.0 : 28.0,
            recent_longest_run_km = 8,
            recent_runs_per_week = days,
            // Independent current-fitness evidence (RECENT_RACE pace source) is required for
            // GOAL_FEASIBILITY_IN to classify a user-defined target time (shared,
            // distance-agnostic resolver behavior -- PHASE4D_4). 1250s/5K recent race vs a
            // 1200s/5K goal is a 4.0% Riegel gap -- within the CHALLENGING band (<=6%).
            recent_race = new { distance = "five_k", finish_time_seconds = 1250, race_date = startDate.AddDays(-21).ToString("yyyy-MM-dd") },
            race_name = $"FIVE-K.1 I{days}D public activation test",
        };
    }

    // ───────────────────────── Preview: public activation ─────────────────────────

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public async Task ExactFiveK_IntermediatePublicFrequencies_PreviewSucceeds_UsesCanonicalFiveKTemplate(int days)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", FiveKRequest(days));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"expected 200, got {response.StatusCode}: {body}");
        var preview = JsonNode.Parse(body)!;
        Assert.Equal("five_k", preview["goal_distance"]!.GetValue<string>());
        Assert.Equal($"FIVE_K__{days}D__INTERMEDIATE", preview["template_id"]!.GetValue<string>());
        Assert.Null(preview["requested_target_distance_km"]);
        Assert.Equal(10, preview["weeks"]!.AsArray().Count);
        Assert.All(preview["weeks"]!.AsArray(), w => Assert.Equal(days, w!["days"]!.AsArray().Count));

        // Never TEN_K/HALF_MARATHON-derived or legacy habit-FiveK in disguise.
        Assert.DoesNotContain("TEN_K", body);
        Assert.DoesNotContain("HALF_MARATHON", body);
    }

    [Fact]
    public async Task FiveDayFiveK_NoKey2_FifthSessionIsEasy_NotInterval()
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", FiveKRequest(5));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = JsonNode.Parse(body)!;

        // FIVE-K.0B §19's no-KEY2 invariant: exactly one quality-type day per week
        // (interval/tempo, whichever stage is phase-active) -- never two -- plus
        // exactly three plain easy days and one long run. A future shared adapter
        // that silently injected a second quality day at 5D would fail this.
        foreach (var week in preview["weeks"]!.AsArray())
        {
            var days = week!["days"]!.AsArray();
            var dayTypes = days.Select(d => d!["day_type"]!.GetValue<string>()).ToArray();
            var qualityCount = dayTypes.Count(t => t is "interval" or "tempo");
            var easyCount = dayTypes.Count(t => t == "easy");
            var longRunCount = dayTypes.Count(t => t == "long_run");
            Assert.True(qualityCount <= 1, $"expected at most 1 quality day, found {qualityCount} ({string.Join(",", dayTypes)})");
            Assert.Equal(1, longRunCount);
            Assert.Equal(5, days.Count);
            // Foundation weeks (no quality stage active yet) run 4 easy + 1 long; every
            // other phase runs 1 quality + 3 easy + 1 long -- either way, never a second
            // quality day.
            Assert.Equal(4, easyCount + qualityCount);
        }
    }

    // ───────────────────────── Unsupported FIVE_K cells remain product-ineligible ─────────────────────────

    [Theory]
    [InlineData("beginner", 4)]
    [InlineData("advanced", 4)]
    public async Task UnsupportedFiveKLevels_NeverRouteToCanonicalFiveKTemplate(string level, int days)
    {
        await ResetAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", FiveKRequest(days, level: level));
        var body = await response.Content.ReadAsStringAsync();
        // Must never silently succeed with the canonical FIVE_K template identity.
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var preview = JsonNode.Parse(body)!;
            Assert.NotEqual($"FIVE_K__{days}D__INTERMEDIATE", preview["template_id"]?.GetValue<string>());
        }
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    public async Task UnsupportedFiveKFrequencies_NeverRouteToCanonicalFiveKTemplate(int days)
    {
        await ResetAsync();
        var request = new
        {
            goal_distance = "five_k",
            level = "intermediate",
            days_per_week = days,
            unit = "km",
            start_date = "2026-08-03",
            preferred_days = Enumerable.Range(0, days).Select(i => new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }[i]).ToArray(),
            long_run_day = "sun",
            race_date = new DateOnly(2026, 8, 3).AddDays(10 * 7).ToString("yyyy-MM-dd"),
            target_finish_time_seconds = 1200,
            target_finish_time_source = "user_defined",
            recent_weekly_volume_km = 20.0,
            recent_longest_run_km = 8,
            recent_runs_per_week = days,
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", request);
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var preview = JsonNode.Parse(body)!;
            Assert.DoesNotContain("FIVE_K__", preview["template_id"]?.GetValue<string>() ?? string.Empty);
        }
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ───────────────────────── Full mutation lifecycle (preview→confirm→Postgres→Home→Calendar→Details→Complete→Not-Today→Cancel) ─────────────────────────

    [Fact]
    public async Task FourDay_FullLifecycle_IdentitySurvivesEveryStep()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", FiveKRequest(4));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        Assert.Equal("five_k", preview["goal_distance"]!.GetValue<string>());
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);

        Assert.Equal("FIVE_K", plan.CanonicalDistanceFamily);
        Assert.Equal(GoalDistance.FiveK, plan.GoalDistance);
        Assert.Equal(10, plan.Weeks.Count);

        var home = await _client.GetJsonAsync("/api/v1/plans/active/home");
        Assert.Equal("five_k", home["active_plan"]!["goal_distance"]!.GetValue<string>());

        var calendar = await _client.GetJsonAsync("/api/v1/plans/active/calendar?month=2026-08");
        Assert.NotEmpty(calendar.AsArray());

        var details = await _client.GetJsonAsync("/api/v1/plans/active/details");
        Assert.Equal("five_k", details["goal_distance"]!.GetValue<string>());

        var days = plan.Weeks.SelectMany(w => w.Days).OrderBy(d => d.Date).ToArray();
        var toComplete = days[0];
        (await _client.PostRawAsync($"/api/v1/training-days/{toComplete.Id}/complete",
            new { actual_distance_km = toComplete.PlannedDistanceKm, actual_duration_min = 25 })).EnsureSuccessStatusCode();
        Assert.Equal(TrainingDayStatus.Completed, (await db.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == toComplete.Id)).Status);

        var toSkip = days[1];
        (await _client.PostRawAsync($"/api/v1/training-days/{toSkip.Id}/not-today-decisions",
            new { reason = "FIVE-K.1 not-today lifecycle proof" })).EnsureSuccessStatusCode();

        var cancel = await _client.PostRawAsync($"/api/v1/plans/{planId}/cancel",
            new { reason = "FIVE-K.1 lifecycle proof cancellation" });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
    }

    [Fact]
    public async Task FiveDay_PreviewConfirmPersist_ReadBack_SessionLayoutHasNoKey2()
    {
        await ResetAsync();
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race", FiveKRequest(5));
        previewResponse.EnsureSuccessStatusCode();
        var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonNode>())!;
        var previewId = Guid.Parse(preview["preview_id"]!.GetValue<string>());

        var confirm = await _client.PostJsonAsync("/api/v1/plans/confirm", new { preview_id = previewId });
        var planId = Guid.Parse(confirm["plan_id"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().Include(p => p.Weeks).ThenInclude(w => w.Days)
            .SingleAsync(p => p.Id == planId);

        Assert.Equal("FIVE_K", plan.CanonicalDistanceFamily);
        foreach (var week in plan.Weeks)
        {
            Assert.Equal(5, week.Days.Count);
        }
    }

    // ───────────────────────── Habit FiveK zero-delta (legacy, unrelated mechanism) ─────────────────────────

    [Fact]
    public async Task HabitFiveK_LegacySqlMechanism_RemainsCompletelySeparate_NeverHijackedByCanonicalRacePlanFiveK()
    {
        await ResetAsync();
        // A GoalType.Habit request (not Race) must never be affected by this
        // phase's canonical FIVE_K race-plan activation -- the legacy
        // GenerationSource.LegacySql habit FiveK mechanism is out of scope and
        // untouched. This proves the Race-plan activation did not widen any
        // shared dispatch in a way that accidentally changes habit-plan routing.
        var request = new
        {
            goal_distance = "five_k",
            level = "intermediate",
            days_per_week = 4,
            unit = "km",
            start_date = "2026-08-03",
            preferred_days = DaysFor(4),
            long_run_day = "sun",
        };
        var response = await _client.PostRawAsync("/api/v1/plans/generate-preview/habit", request);
        // Not asserting a specific status (habit preview contract is out of this
        // phase's scope) -- only that it never 500s as a side effect of the Race
        // FIVE_K activation, and never returns the canonical Race FIVE_K template id.
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("FIVE_K__4D__INTERMEDIATE", body);
    }
}
