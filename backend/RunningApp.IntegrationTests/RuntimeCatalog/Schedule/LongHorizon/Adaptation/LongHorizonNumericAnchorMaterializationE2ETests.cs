using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4B.2 -- real end-to-end proof that a real Reduce decision
/// materially changes the actual persisted/generated next-window numeric
/// target, through the real HTTP NotToday/Complete/activate-next-window
/// endpoints, using real Long-Horizon generation. No synthetic
/// LogicalSessionEvidence, no direct ValidatedSustainableLoad injection.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonNumericAnchorMaterializationE2ETests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonNumericAnchorMaterializationE2ETests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Window 1: completed fully (ProgressAsPlanned) -- establishes a real
    /// prior anchor. Window 2: only the LONG_RUN is completed (satisfies the
    /// evidence aggregator's own completed-long-run requirement), the rest
    /// marked NotToday -- EffectiveCompletedCount = 1 -&gt; real Reduce
    /// decision. Window 3's real materialized weekly distance total must not
    /// exceed Window 2's own assigned total (held/capped at the prior
    /// anchor, never grown) -- proving the Reduce anchor genuinely reached
    /// composition, not just the response DTO.
    /// </summary>
    [Fact]
    public async Task RealReduceDecision_CapsWindow3AtWindow2Level_InsteadOfNormalGrowth()
    {
        var state = await ConfirmAsync();

        // Window 1 -> Window 2: full completion, real ProgressAsPlanned.
        await CompleteAllAsync(state.RollingId);
        var activate1 = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("activated", activate1["outcome"]!.GetValue<string>());
        Assert.Equal("progress_as_planned", activate1["next_window_load_decision"]!.GetValue<string>());

        double window2Total;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            var window2Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .ToListAsync();
            window2Total = window2Sessions.Sum(s => s.DistanceKm);

            // Window 2 spans multiple structural weeks (the real GE-phase
            // window is a multi-week block, not a single 4-session week) --
            // the frozen 4M.1 NextWindowLoadDecisionPolicy's absolute-count
            // thresholds (0-1 -> Reduce) are unaffected by window size, so
            // to land in the Reduce bracket only the FIRST long run is
            // completed (satisfying the evidence aggregator's own
            // "needs >=1 completed long run" gate) and every other session
            // in the window is marked NotToday(illness) -- illness blocks
            // schedule-repair (4M.3: Skip, no reschedule/substitute
            // attempt), so none of these NotToday calls Supersede another
            // still-unprocessed session in this same loop.
            var firstLongRun = window2Sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));
            foreach (var session in window2Sessions)
            {
                if (session.Id == firstLongRun.Id)
                {
                    (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                        new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
                }
                else
                {
                    (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                        new { reason = "illness" })).EnsureSuccessStatusCode();
                }
            }
        }

        var activate2 = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("activated", activate2["outcome"]!.GetValue<string>());
        Assert.Equal("reduce", activate2["next_window_load_decision"]!.GetValue<string>());

        // Fresh DbContext (item 20/30): read Window 3's real persisted rows.
        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var freshAggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        var window3Sessions = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= freshAggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= freshAggregate.CurrentWindowEndWeek)
            .ToListAsync();
        var window3Total = window3Sessions.Sum(s => s.DistanceKm);

        // The real, generated, persisted numeric target for Window 3 must not
        // exceed Window 2's own level -- normal (unconstrained) progression
        // would have grown it further.
        Assert.True(window3Total <= window2Total + 0.01,
            $"Window 3 total ({window3Total}km) exceeded Window 2's level ({window2Total}km) despite a real Reduce decision -- the anchor did not reach composition.");
    }

    /// <summary>
    /// Retry/idempotency (Q.21, highest-risk path per the phase instructions):
    /// two concurrent activation requests against the same real Reduce-eligible
    /// window must produce exactly one committed activation -- the anchor
    /// selection/override happens on an in-memory checkpoint copy before the
    /// existing, unmodified idempotent persistence call, so it cannot itself
    /// introduce a double-application; this proves that empirically against
    /// the real endpoint rather than merely by code inspection.
    /// </summary>
    [Fact]
    public async Task ConcurrentActivation_WithRealReduceDecision_HasExactlyOneWinner_NoDoubleReduction()
    {
        var state = await ConfirmAsync();
        await CompleteAllAsync(state.RollingId);
        (await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 })).EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            var window2Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .ToListAsync();
            var firstLongRun = window2Sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));
            foreach (var session in window2Sessions)
            {
                if (session.Id == firstLongRun.Id)
                    (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                        new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
                else
                    (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                        new { reason = "illness" })).EnsureSuccessStatusCode();
            }
        }

        using var second = _factory.CreateClient();
        var responses = await Task.WhenAll(
            _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }),
            second.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));

        Assert.Single(responses, r => r.StatusCode == System.Net.HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode != System.Net.HttpStatusCode.OK);

        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var freshAggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        // Exactly one activation window record for the new window range --
        // no double-commit, and therefore no possibility of a doubly-applied
        // Reduce anchor.
        Assert.Equal(1, await freshDb.LongHorizonActivationWindowRecords.CountAsync(
            a => a.PlanStateId == state.RollingId && a.StartGlobalWeek == freshAggregate.CurrentWindowStartWeek));
    }

    private async Task CompleteAllAsync(Guid rollingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId).OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        foreach (var session in sessions)
        {
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
    }

    private async Task<ConfirmedState> ConfirmAsync()
    {
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
        var start = new DateOnly(2026, 9, 7);
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", new
        {
            goal_distance = "ten_k", level = "intermediate", days_per_week = 4, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = new[] { "mon", "wed", "fri", "sun" }, long_run_day = "sun",
            race_date = start.AddDays(21 * 7).ToString("yyyy-MM-dd"), target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average", race_name = "Phase 4M.4B.2",
            recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = 4
        });
        var preview = await JsonAsync(previewResponse);
        var rollingId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = rollingId }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        return new ConfirmedState(planId, rollingId, plan.InternalUserId!.Value);
    }

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonNode.Parse(body)!;
    }

    private sealed record ConfirmedState(Guid PlanId, Guid RollingId, Guid OwnerId);
}
