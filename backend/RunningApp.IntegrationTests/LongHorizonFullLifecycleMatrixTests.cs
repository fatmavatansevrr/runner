using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Services;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.4F -- proves the public continuation lifecycle matrix through
/// the real HTTP activation endpoint, now that Phase 4L.4E's role-evidence
/// fix has unblocked continuation beyond the first GE-&gt;Runway crossing.
///
/// Empirical finding driving this file's scope (see the phase document's
/// "Final lifecycle-shape inventory" section): for a 21-week TEN_K plan
/// under the existing, unmodified composition formula (GE = TotalWeeks-20,
/// Runway = 8 weeks exactly, Core = 12 weeks exactly), the full public
/// continuation chain -- GE(window 1, 1 week) -&gt; Runway(2-5) -&gt;
/// Runway(6-9) -&gt; Core(10-13) -&gt; Core(14-17) -&gt; Core(18-21, final) -&gt;
/// TerminalPlanComplete -- is now real-time reachable end-to-end through the
/// real public activation endpoint, with NO real-calendar time gate beyond
/// the very first GE checkpoint (confirmed empirically, not assumed).
///
/// Also empirically confirmed: because Runway is always exactly 8 weeks
/// (2x4-week windows) and Core is always exactly 12 weeks (3x4-week
/// windows), and window 1 always consumes the entire GE segment in one
/// shot, NO window boundary ever straddles a segment boundary for any valid
/// 21-52 week horizon under the current, unmodified composition formula.
/// Mixed Runway/Core windows (1+3/2+2/3+1) and a partial final window are
/// therefore structurally unreachable -- not a testing gap, an architectural
/// consequence of the approved TD-LONG-HORIZON-COMPOSITION-001 formula.
/// Fabricating them would violate this phase's own "do not fabricate
/// unsupported horizons only to force a shape" instruction, so they are
/// documented, not built.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonFullLifecycleMatrixTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonFullLifecycleMatrixTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullLifecycleWalk_ProducesEveryReachableShape_ThroughRealPublicEndpoint()
    {
        var state = await ConfirmAsync();

        // Cycle 1: GE (window 1) -> Runway (2-5). Already the Phase 4L.4A/4L.4D baseline shape.
        var w1 = await CompleteAndActivateAsync(state.RollingId);
        AssertActivated(w1, expectedStart: 2, expectedEnd: 5);
        await AssertOnlySegmentAsync(state.RollingId, 2, 5, LongHorizonPersistedSegmentType.PreparationRunway);
        await AssertCanonicalRolesAsync(state.RollingId, 2, 5);

        // Cycle 2: pure Runway continuation (Runway -> Runway). NEW shape,
        // unreachable before Phase 4L.4E's fix.
        var w2 = await CompleteAndActivateAsync(state.RollingId);
        AssertActivated(w2, expectedStart: 6, expectedEnd: 9);
        await AssertOnlySegmentAsync(state.RollingId, 6, 9, LongHorizonPersistedSegmentType.PreparationRunway);
        await AssertCanonicalRolesAsync(state.RollingId, 6, 9);

        // Cycle 3: Runway -> Core boundary. Historical Runway output must remain immutable.
        var priorRunwaySessionIds = await SessionIdsAsync(state.RollingId, 2, 9);
        var w3 = await CompleteAndActivateAsync(state.RollingId);
        AssertActivated(w3, expectedStart: 10, expectedEnd: 13);
        await AssertOnlySegmentAsync(state.RollingId, 10, 13, LongHorizonPersistedSegmentType.Core);
        await AssertCanonicalRolesAsync(state.RollingId, 10, 13);
        Assert.Equal(priorRunwaySessionIds, await SessionIdsAsync(state.RollingId, 2, 9)); // historical Runway sessions immutable.

        // Cycle 4: pure Core continuation. NEW shape.
        var w4 = await CompleteAndActivateAsync(state.RollingId);
        AssertActivated(w4, expectedStart: 14, expectedEnd: 17);
        await AssertOnlySegmentAsync(state.RollingId, 14, 17, LongHorizonPersistedSegmentType.Core);

        // Cycle 5: pure Core continuation, final window (18-21 = TotalWeeks). NEW shape.
        var w5 = await CompleteAndActivateAsync(state.RollingId);
        AssertActivated(w5, expectedStart: 18, expectedEnd: 21);
        Assert.Null(w5["next_pending_global_week"]);
        await AssertOnlySegmentAsync(state.RollingId, 18, 21, LongHorizonPersistedSegmentType.Core);

        // Cycle 6: final window terminal -> TerminalPlanComplete. NEW shape.
        await CompleteCurrentWindowAsync(state.RollingId);
        var terminal = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("terminal_plan_complete", terminal["outcome"]!.GetValue<string>());
        Assert.True(terminal["is_terminal"]!.GetValue<bool>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Next-operation distinction: every activation window is exactly once, distinct, never swallowed by a prior one's replay.
        var windows = await db.LongHorizonActivationWindowRecords.AsNoTracking()
            .Where(a => a.PlanStateId == state.RollingId).Select(a => new { a.StartGlobalWeek, a.EndGlobalWeek }).ToListAsync();
        Assert.Equal(new[] { (1, 1), (2, 5), (6, 9), (10, 13), (14, 17), (18, 21) },
            windows.Select(w => (w.StartGlobalWeek, w.EndGlobalWeek)).OrderBy(w => w.StartGlobalWeek));

        // Terminal idempotency/replay: no write occurs on repeated terminal calls.
        var aggregateBefore = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        var terminalReplay = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("terminal_plan_complete", terminalReplay["outcome"]!.GetValue<string>());
        var aggregateAfter = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(aggregateBefore.CurrentWindowStartWeek, aggregateAfter.CurrentWindowStartWeek);
        Assert.Equal(aggregateBefore.CurrentWindowEndWeek, aggregateAfter.CurrentWindowEndWeek);
        Assert.Equal(6, await db.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.RollingId)); // unchanged: still exactly 6.

        // Home/active-details remain terminal and readable after the plan is fully complete.
        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Equal("terminal_plan_complete", home["active_plan"]!["checkpoint_readiness"]!.GetValue<string>());
        var calendar = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-09"));
        Assert.NotEmpty(calendar["sessions"]!.AsArray()); // historical September sessions remain readable.
    }

    [Fact]
    public async Task ConcurrentActivation_AtCoreOnlyBoundary_HasExactlyOneDurableWinner()
    {
        var state = await ConfirmAsync();
        // Reach the Runway->Core boundary (3 real cycles), then race the Core continuation.
        await CompleteAndActivateAsync(state.RollingId); // -> Runway 2-5
        await CompleteAndActivateAsync(state.RollingId); // -> Runway 6-9
        await CompleteAndActivateAsync(state.RollingId); // -> Core 10-13
        await CompleteCurrentWindowAsync(state.RollingId); // terminalize Core 10-13

        using var second = _factory.CreateClient();
        var responses = await Task.WhenAll(
            _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }),
            second.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode != HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.RollingId && a.StartGlobalWeek == 14));
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(14, aggregate.CurrentWindowStartWeek);
        Assert.Equal(17, aggregate.CurrentWindowEndWeek);
    }

    [Theory]
    [InlineData(0)] // AfterVersionValidation
    [InlineData(5)] // BeforeCommit
    public async Task PreCommitFailure_AtCoreOnlyActivation_RollsBackExactPriorState_AndCorrectedRetrySucceedsOnce(int failpointValue)
    {
        var failpoint = (RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence.LongHorizonPersistenceFailpoint)failpointValue;
        var state = await ConfirmAsync();
        await CompleteAndActivateAsync(state.RollingId); // -> Runway 2-5
        await CompleteAndActivateAsync(state.RollingId); // -> Runway 6-9
        await CompleteAndActivateAsync(state.RollingId); // -> Core 10-13
        await CompleteCurrentWindowAsync(state.RollingId); // terminalize Core 10-13

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var injector = new RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence.LongHorizonTestPersistenceFailureInjector(
                RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence.LongHorizonPersistenceOperation.CoreOnlyActivation, failpoint);
            var service = new LongHorizonRollingWindowActivationService(
                db, scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LongHorizonRollingWindowActivationService>>(), injector);
            await Assert.ThrowsAnyAsync<Exception>(() => service.ActivateNextWindowAsync(state.OwnerId, new LongHorizonActivateNextWindowRequest()));
        }

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            Assert.Equal(10, aggregate.CurrentWindowStartWeek); // exact prior state -- no partial advancement.
            Assert.Equal(13, aggregate.CurrentWindowEndWeek);
            Assert.Equal(0, await db.LongHorizonRollingSessionStates.CountAsync(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek == 14));
        }

        var retry = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("activated", retry["outcome"]!.GetValue<string>());
        Assert.Equal(14, retry["activated_window_range"]!["start_global_week"]!.GetValue<int>());
    }

    [Fact]
    public async Task HomeCalendarDetail_ReflectLaterCoreShape_WithCanonicalRolesAndNoPendingLeakage()
    {
        var state = await ConfirmAsync();
        await CompleteAndActivateAsync(state.RollingId); // -> Runway 2-5
        await CompleteAndActivateAsync(state.RollingId); // -> Runway 6-9
        await CompleteAndActivateAsync(state.RollingId); // -> Core 10-13

        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Equal(10, home["active_plan"]!["current_window_start_week"]!.GetValue<int>());
        Assert.Equal(13, home["active_plan"]!["current_window_end_week"]!.GetValue<int>());
        // CurrentPhase reflects the plan's real-calendar "today" position
        // (unchanged, unrelated Home semantics -- not the activated window),
        // so it stays general_endurance here since real "today" precedes
        // every activated week's real calendar date. current_window_sessions
        // below is the field that must reflect the newly activated Core window.
        var sessions = home["current_window_sessions"]!.AsArray();
        Assert.NotEmpty(sessions);
        Assert.All(sessions, s => Assert.True(s!["workout_role"]!.GetValue<string>() is "KEY_SESSION" or "EASY_SUPPORT" or "LONG_RUN"));
        Assert.DoesNotContain("numeric_pending", home.ToJsonString(), StringComparison.OrdinalIgnoreCase);

        var firstSessionId = sessions[0]!["session_id"]!.GetValue<Guid>();
        var detail = await JsonAsync(await _client.GetAsync($"/api/v1/training-days/rolling/{firstSessionId}"));
        Assert.Equal(firstSessionId, detail["session"]!["session_id"]!.GetValue<Guid>());
        Assert.DoesNotContain("context", detail.ToJsonString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("target_lock", detail.ToJsonString(), StringComparison.OrdinalIgnoreCase);

        var calendar = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/calendar?month=2026-11"));
        Assert.Equal("rolling_long_horizon", calendar["schedule_strategy"]!.GetValue<string>());
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
            target_finish_time_source = "product_average", race_name = "Phase 4L.4F",
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

    private async Task CompleteCurrentWindowAsync(Guid rollingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == rollingId);
        var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek
                && s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Planned)
            .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        foreach (var session in sessions)
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
    }

    private async Task<JsonNode> CompleteAndActivateAsync(Guid rollingId)
    {
        await CompleteCurrentWindowAsync(rollingId);
        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        return await JsonAsync(response);
    }

    private static void AssertActivated(JsonNode activationResponse, int expectedStart, int expectedEnd)
    {
        Assert.Equal("activated", activationResponse["outcome"]!.GetValue<string>());
        Assert.Equal(expectedStart, activationResponse["activated_window_range"]!["start_global_week"]!.GetValue<int>());
        Assert.Equal(expectedEnd, activationResponse["activated_window_range"]!["end_global_week"]!.GetValue<int>());
    }

    private async Task AssertOnlySegmentAsync(Guid rollingId, int startWeek, int endWeek, LongHorizonPersistedSegmentType expected)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var segments = await db.LongHorizonRollingWeekStates.AsNoTracking()
            .Where(w => w.PlanStateId == rollingId && w.GlobalWeek >= startWeek && w.GlobalWeek <= endWeek)
            .Select(w => w.SegmentType).Distinct().ToListAsync();
        Assert.Equal(new[] { expected }, segments);
    }

    private async Task AssertCanonicalRolesAsync(Guid rollingId, int startWeek, int endWeek)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roles = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek >= startWeek && s.Week.GlobalWeek <= endWeek)
            .Select(s => s.SessionRole).ToListAsync();
        Assert.NotEmpty(roles);
        Assert.All(roles, r => Assert.True(r is "KEY_SESSION" or "EASY_SUPPORT" or "LONG_RUN", $"Non-canonical role: {r}"));
    }

    private async Task<List<Guid>> SessionIdsAsync(Guid rollingId, int startWeek, int endWeek)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek >= startWeek && s.Week.GlobalWeek <= endWeek)
            .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).Select(s => s.Id).ToListAsync();
    }

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonNode.Parse(body)!;
    }

    private sealed record ConfirmedState(Guid PlanId, Guid RollingId, Guid OwnerId);
}
