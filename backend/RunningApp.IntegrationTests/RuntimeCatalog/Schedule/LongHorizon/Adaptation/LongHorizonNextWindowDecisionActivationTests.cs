using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4A -- real end-to-end HTTP tests proving the window-checkpoint
/// decision (NextWindowLoadDecision/NextWindowSafetyReviewRequired) is
/// carried through the real explicit activation endpoint, and that no
/// numeric mutation of any kind results from it (Phase 4M.4A is wiring
/// only -- see PHASE4M_4A doc for the confirmed absence of a numeric
/// progression seam).
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonNextWindowDecisionActivationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonNextWindowDecisionActivationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullyCompletedWindow_ActivationReportsProgressAsPlanned_SafetyFalse_NoNumericMutation()
    {
        var state = await ConfirmAsync();
        await CompleteAllCurrentWindowSessionsAsync(state);

        var activate = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));

        Assert.Equal("activated", activate["outcome"]!.GetValue<string>());
        Assert.Equal("progress_as_planned", activate["next_window_load_decision"]!.GetValue<string>());
        Assert.False(activate["next_window_safety_review_required"]!.GetValue<bool>());

        // No numeric mutation: activated sessions carry ordinary, non-zero,
        // non-special-cased distance/workout data -- ProgressAsPlanned/
        // Maintain/Reduce currently make zero difference to what gets
        // materialized (4M.4B scope), which this proves by absence of any
        // marker/override.
        Assert.All(activate["activated_sessions"]!.AsArray(), s =>
        {
            Assert.True(s!["planned_distance_km"]!.GetValue<double>() > 0);
            Assert.False(string.IsNullOrWhiteSpace(s["workout_role"]!.GetValue<string>()));
        });
    }

    /// <summary>
    /// Phase 4M.4A final closure (§A), revised in Phase 4M.4B.2A -- a real
    /// HTTP NotToday(reason: "soreness") submission (exercising the real
    /// 4M.3 RuntimeNotTodayReasonMapper + ReasonClassificationPolicy at
    /// submission time, not a hand-built LogicalSessionEvidence), the
    /// remaining window sessions completed normally, then the real explicit
    /// activate-next-window endpoint. The double-mapping audit (persisted
    /// reason is the raw runtime token, never pre-translated) still holds
    /// and is proven directly from the persisted row.
    ///
    /// The original version of this test additionally asserted a successful
    /// "activated" response with next_window_safety_review_required=true and
    /// an independently-computed LoadDecision. That is no longer reachable
    /// for this pilot: the 4M.4B.2A investigation found this pilot's real
    /// roadmap fully consumes its General Endurance phase in the plan's very
    /// first window, so this checkpoint always routes through real Runway/
    /// Core JIT composition, which legitimately declines (Block) whenever
    /// the checkpointed window has any NotToday session -- a real,
    /// pre-existing JIT/Runway evidence-completeness requirement, now
    /// correctly surfaced as a typed 409 instead of the previously-masked
    /// false "activated" response (see
    /// PHASE4M_4B_2A_MULTIWINDOW_ACTIVATION_ADVANCEMENT_DEFECT.md). The
    /// SafetyReviewRequired/LoadDecision-independence computation itself is
    /// unit-tested directly in WindowCheckpointSummaryAndDecisionTests/
    /// NextWindowLoadDecisionPolicy's own suite; observing it through a
    /// non-blocking real HTTP response requires a scenario with zero
    /// NotToday sessions, which this test cannot use by definition.
    /// </summary>
    [Fact]
    public async Task RealSorenessSubmission_BlocksViaRealJitRunwayEvidenceCompletenessRequirement_ReasonMappingStillCorrect()
    {
        var state = await ConfirmAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var windowSessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId).OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();

        var soreSession = windowSessions[0];
        var others = windowSessions.Skip(1).ToList();

        // Real endpoint, real runtime vocabulary token -- the exact live
        // client-facing string, never a canonical/internal enum value.
        (await _client.PostRawAsync($"/api/v1/training-days/rolling/{soreSession.Id}/not-today", new { reason = "soreness" })).EnsureSuccessStatusCode();
        foreach (var session in others)
        {
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }

        var activateResponse = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, activateResponse.StatusCode);
        var body = await activateResponse.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", body);

        // Critical invariant proven by the 4M.4B.2A fix: the Block must
        // never masquerade as a real window advancement.
        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(1, aggregate.CurrentWindowStartWeek);
        Assert.Equal(1, aggregate.CurrentWindowEndWeek);

        // Double-mapping audit (§B): the persisted reason is the raw runtime
        // token, verbatim -- never pre-translated to a canonical/internal
        // value. Unaffected by the later Block -- the NotToday call itself
        // already committed this before activation was attempted.
        var persisted = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == soreSession.Id);
        Assert.Equal("soreness", persisted.NotTodayReason);
        Assert.NotEqual("pain_or_discomfort", persisted.NotTodayReason);
    }

    /// <summary>
    /// Phase 4M.5 (§C2) -- real HTTP EASY_SUPPORT NotToday, everything else
    /// completed: EffectiveCompletedCount=3, only Easy missing -&gt;
    /// Rev4.1's own matrix says ProgressAsPlanned (not Maintain/Reduce).
    /// Primed via a fully-completed Window 0 first so the checkpoint under
    /// test is not the plan's first-ever one (avoiding the separately-proven,
    /// unrelated first-checkpoint/Core-JIT-evidence-completeness boundary
    /// from 4M.4B.2B) -- this isolates exactly the ScheduleRepairPolicy
    /// EasySupportRule (Skip, no replacement, no candidate query) and the
    /// resulting LoadDecision, both observed through the real response.
    /// </summary>
    [Fact]
    public async Task EasyNotToday_OtherThreeCompleted_SkipsNoReplacement_ProgressAsPlanned()
    {
        var state = await ConfirmAsync();
        await CompleteAllCurrentWindowSessionsAsync(state);
        var primeResponse = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        if (!primeResponse.IsSuccessStatusCode)
            Assert.Fail($"priming activation failed: {primeResponse.StatusCode} {await primeResponse.Content.ReadAsStringAsync()}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        var windowSessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
            .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        var easy = windowSessions.First(s => LongHorizonSessionRoleCodec.IsEasySupport(s.SessionRole));
        var others = windowSessions.Where(s => s.Id != easy.Id).ToList();

        (await _client.PostRawAsync($"/api/v1/training-days/rolling/{easy.Id}/not-today", new { reason = "schedule" })).EnsureSuccessStatusCode();
        foreach (var session in others)
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();

        var activate = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("activated", activate["outcome"]!.GetValue<string>());
        Assert.Equal("progress_as_planned", activate["next_window_load_decision"]!.GetValue<string>());
        Assert.False(activate["next_window_safety_review_required"]!.GetValue<bool>());

        // No replacement was created for the Easy -- EasySupportRule Skips
        // unconditionally, no candidate query, no new session lineage.
        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var noReplacement = await freshDb.LongHorizonRollingSessionStates.AsNoTracking()
            .Where(s => s.AdaptedFromSessionId == easy.Id).ToListAsync();
        Assert.Empty(noReplacement);
        var persistedEasy = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == easy.Id);
        Assert.Equal(RunningApp.Domain.Enums.LongHorizonRollingSessionOutcomeStatus.NotToday, persistedEasy.OutcomeStatus);
    }

    /// <summary>
    /// Phase 4M.5 (§C4) -- real HTTP illness on the KEY_SESSION, everything
    /// else completed: illness blocks repair (4M.1
    /// ReasonClassificationPolicy.BlocksReschedule) but is NOT a Safety
    /// reason -- proves illness-blocks-repair and SafetyReviewRequired=false
    /// are both correct simultaneously. Primed via Window 0 for the same
    /// reason as the Easy test above (avoids the separately-proven,
    /// unrelated first-checkpoint/Core-JIT-evidence-completeness boundary).
    ///
    /// Phase 4M.5 originally observed this pilot's post-priming window is a
    /// real 4-week/16-session block and, disclosed honestly at the time,
    /// found the OLD (pre-Rev5) direct multi-week-summary invocation
    /// resolved missing-1-of-16 to ProgressAsPlanned -- a documented
    /// consequence of the old bug 4M.5A/4M.5B/Rev5 §7a exist to fix (the
    /// old summary's KeySessionCompleted/LongRunCompleted booleans go
    /// permanently false the moment even one of the window's 4 real KEY
    /// occurrences is missing, which the old code never read; only the raw
    /// EffectiveCompletedCount>=4 threshold decided the branch). Phase
    /// 4M.5C's weekly-summary + B1 aggregation now correctly evaluates the
    /// KEY-missing structural week on its own -- exactly 3/4 completed for
    /// that week with KEY (not Easy) missing -- so that week alone resolves
    /// to Maintain (Rev3 §7 "3 completed, Key or Long missing -> Maintain"),
    /// and Maintain is the worst of the four weekly decisions (the other
    /// three weeks are all fully completed -> ProgressAsPlanned each). B1
    /// worst-week-wins therefore correctly yields Maintain for the whole
    /// window -- this is the fix taking effect, not a regression.
    /// </summary>
    [Fact]
    public async Task IllnessOnKey_OtherFifteenCompleted_NoRepair_Maintain_SafetyFalse()
    {
        var state = await ConfirmAsync();
        await CompleteAllCurrentWindowSessionsAsync(state);
        var primeResponse = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        if (!primeResponse.IsSuccessStatusCode)
            Assert.Fail($"priming activation failed: {primeResponse.StatusCode} {await primeResponse.Content.ReadAsStringAsync()}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        var windowSessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
            .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        var key = windowSessions.First(s => LongHorizonSessionRoleCodec.IsKeySession(s.SessionRole));
        var others = windowSessions.Where(s => s.Id != key.Id).ToList();

        (await _client.PostRawAsync($"/api/v1/training-days/rolling/{key.Id}/not-today", new { reason = "illness" })).EnsureSuccessStatusCode();
        foreach (var session in others)
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();

        var activate = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));
        Assert.Equal("activated", activate["outcome"]!.GetValue<string>());
        Assert.Equal("maintain", activate["next_window_load_decision"]!.GetValue<string>());
        Assert.False(activate["next_window_safety_review_required"]!.GetValue<bool>());

        // No repair attempted -- illness blocks reschedule, source KEY stays NotToday.
        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var noReplacement = await freshDb.LongHorizonRollingSessionStates.AsNoTracking()
            .Where(s => s.AdaptedFromSessionId == key.Id).ToListAsync();
        Assert.Empty(noReplacement);
        var persistedKey = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == key.Id);
        Assert.Equal(RunningApp.Domain.Enums.LongHorizonRollingSessionOutcomeStatus.NotToday, persistedKey.OutcomeStatus);
        Assert.Equal("illness", persistedKey.NotTodayReason);
    }

    [Fact]
    public async Task TerminalPlanComplete_DoesNotProduceNextWindowDecision()
    {
        var state = await ConfirmAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == state.RollingId);
        aggregate.CurrentLifecycleStatus = RunningApp.Domain.Enums.LongHorizonPersistedLifecycleState.Completed;
        await db.SaveChangesAsync();

        var activate = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));

        Assert.Equal("terminal_plan_complete", activate["outcome"]!.GetValue<string>());
        Assert.True(activate["is_terminal"]!.GetValue<bool>());
        Assert.Null(activate["next_window_load_decision"]);
        Assert.Null(activate["next_window_safety_review_required"]);
    }

    private async Task CompleteAllCurrentWindowSessionsAsync(ConfirmedState state)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId).OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
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
            target_finish_time_source = "product_average", race_name = "Phase 4M.4A",
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

    private static async Task<System.Text.Json.Nodes.JsonNode> JsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return System.Text.Json.Nodes.JsonNode.Parse(body)!;
    }

    private sealed record ConfirmedState(Guid PlanId, Guid RollingId, Guid OwnerId);
}
