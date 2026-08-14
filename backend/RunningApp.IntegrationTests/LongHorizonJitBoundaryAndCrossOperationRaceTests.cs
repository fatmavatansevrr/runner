using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.4D -- closes the concrete, well-scoped gaps from Phase 4L.4A/B/C:
/// the JIT-boundary misclassification fix, and cross-operation races against
/// the one lifecycle window this repository can naturally reach through the
/// public activation endpoint today.
///
/// Empirical finding driving this file's scope (see the phase document's
/// "Lifecycle shape reachability" section for full detail): a 21-week
/// RollingLongHorizon plan's structural window 1 is GE-only (1 week);
/// window 2 immediately crosses into Preparation Runway. That GE-&gt;Runway
/// crossing (already proven in Phase 4L.4A) is the ONLY activation-success
/// shape reachable today. A THIRD continuation cycle (i.e. any pure-Runway,
/// mixed Runway/Core, pure-Core, or Core-refresh continuation) always blocks
/// -- not because of missing training data, but because
/// LongHorizonRollingJitActivationRuntime assigns Runway session roles via
/// `SlotRole.ToString()` (PascalCase, e.g. "LongRun"), while the evidence
/// adapter's long-run detection matches only "LONG_RUN" (GE's convention).
/// This is a genuine, pre-existing, separate defect this phase's
/// investigation surfaced but does NOT fix (fixing session-role
/// normalization is out of this phase's narrow JIT-boundary-classification
/// scope and risks touching protected prescription/composition code). This
/// gives this file a second, real (not seeded) benefit: it naturally
/// reproduces the exact JIT-boundary evidence-gap this phase's fix targets,
/// with no direct DB seeding required.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonJitBoundaryAndCrossOperationRaceTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonJitBoundaryAndCrossOperationRaceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task JitBoundaryEvidenceGap_ClassifiesAsRealDurableBlock_NotUnclassifiedCorruptState()
    {
        var state = await ConfirmAndCrossIntoRunwayAsync();

        // Cycle 2: terminalize the (now Runway) window with every real long
        // run left NotToday -- a genuine evidence gap (Phase 4L.4E fixed
        // role detection, so this is no longer reachable by simply
        // completing the window; it must be a real absence of long-run
        // evidence, exactly like Phase 4L.4C's GE-side equivalent).
        await LeaveRunwayLongRunsNotTodayAsync(state.RollingId);
        var activate = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, activate.StatusCode);
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", await activate.Content.ReadAsStringAsync());
        // Never the old, unclassified, non-recoverable-looking corrupt-state result.
        Assert.DoesNotContain("LONG_HORIZON_READ_STATE_CORRUPT", await activate.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var block = await db.LongHorizonBlockRetryRecords.AsNoTracking()
            .Where(b => b.PlanStateId == state.RollingId && b.EventType == LongHorizonPersistedBlockRetryEventType.Block)
            .OrderByDescending(b => b.CreatedAtUtc).FirstAsync();
        Assert.Equal("JitValidatedLoadUnavailable", block.InternalReasonCode);
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericActivationBlocked, aggregate.CurrentLifecycleStatus);
    }

    [Fact]
    public async Task JitBoundaryBlock_HomeAndRetryAgree_RegeneratePreviewRequired_ZeroMutation()
    {
        var state = await ConfirmAndCrossIntoRunwayAsync();
        await LeaveRunwayLongRunsNotTodayAsync(state.RollingId);
        (await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 })).Dispose();

        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        var plan = home["active_plan"]!;
        Assert.Equal("reassessment_required", plan["checkpoint_readiness"]!.GetValue<string>());
        Assert.Equal("regenerate_preview_required", plan["recovery_requirement"]!.GetValue<string>());
        Assert.DoesNotContain("JitValidatedLoadUnavailable", home.ToJsonString());

        using var beforeScope = _factory.Services.CreateScope();
        var beforeDb = beforeScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await beforeDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);

        var retry = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Contains("LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED", await retry.Content.ReadAsStringAsync());

        using var afterScope = _factory.Services.CreateScope();
        var afterDb = afterScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var after = await afterDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(before.CurrentLifecycleStatus, after.CurrentLifecycleStatus);
        Assert.Equal(before.RetryEligible, after.RetryEligible);
        Assert.Equal(0, await afterDb.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == state.RollingId && b.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored));
    }

    [Fact]
    public async Task RetryVsActivation_NonRecoverableBlock_NoBypassToActivated()
    {
        var state = await ConfirmAndCrossIntoRunwayAsync();
        await LeaveRunwayLongRunsNotTodayAsync(state.RollingId);
        (await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 })).Dispose();

        using var second = _factory.CreateClient();
        var responses = await Task.WhenAll(
            _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 }),
            second.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }));

        // Neither operation may ever succeed for a non-recoverable block --
        // no direct or indirect Blocked -> Activated transition.
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericActivationBlocked, aggregate.CurrentLifecycleStatus);
        Assert.Equal(0, await db.LongHorizonRollingSessionStates.CountAsync(s => s.Week.PlanStateId == state.RollingId
            && s.Week.GlobalWeek > aggregate.CurrentWindowEndWeek));
    }

    [Fact]
    public async Task RetryVsCancellation_IsCoherent_NoResurrectionOrPartialState()
    {
        var state = await ConfirmAndCrossIntoRunwayAsync();
        await LeaveRunwayLongRunsNotTodayAsync(state.RollingId);
        (await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 })).Dispose();

        using var second = _factory.CreateClient();
        var responses = await Task.WhenAll(
            _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 }),
            second.PostRawAsync($"/api/v1/plans/{state.PlanId}/cancel", new { reason = "race" }));

        Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode); // cancellation always a clean generic operation.
        // Retry never succeeds either way: it is rejected as non-recoverable
        // (Conflict, evaluated before/independent of cancellation) or the
        // plan-row lock serializes behind cancellation first, in which case
        // retry observes a non-disclosing missing-plan result (NotFound) --
        // both are safe, typed, non-mutating losers.
        Assert.Contains(responses[0].StatusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.NotFound });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(TrainingPlanStatus.Cancelled, (await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == state.PlanId)).Status);
        Assert.Equal(0, await db.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == state.RollingId && b.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored));
        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Null(home["active_plan"]);
    }

    [Fact]
    public async Task ActivationVsCompletion_StaleActivationCannotUseIncompleteEvidence_LaterFreshActivationSucceeds()
    {
        var state = await ConfirmAsync();
        var sessions = await GetWindowSessionsAsync(state.RollingId, 1, 1);
        var last = sessions[^1];
        foreach (var s in sessions[..^1])
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{s.Id}/complete",
                new { actual_distance_km = s.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();

        // Process B fires "activate" while the window is still genuinely
        // in-progress (final session Planned); Process A completes it. Real
        // ordering inside one PostgreSQL instance is not client-controlled,
        // so both plausible orderings are accepted, but corruption is not:
        // activation must never use an incomplete window, and the final
        // session's outcome must exist exactly once afterward.
        using var second = _factory.CreateClient();
        var responses = await Task.WhenAll(
            second.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }),
            _client.PostRawAsync($"/api/v1/training-days/rolling/{last.Id}/complete", new { actual_distance_km = last.DistanceKm, actual_duration_minutes = 30 }));

        Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode); // completion always succeeds.

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Completed,
                (await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == last.Id)).OutcomeStatus);
        }

        // Regardless of the race's outcome, a later fresh activation request succeeds exactly once.
        var fresh = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.True(fresh.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict);
        if (fresh.StatusCode == HttpStatusCode.Conflict)
            Assert.Contains("LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS", await fresh.Content.ReadAsStringAsync());

        using var verify = _factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await verifyDb.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.RollingId && a.StartGlobalWeek == 2));
    }

    [Fact]
    public async Task ActivationVsNotToday_StaleActivationCannotUseIncompleteEvidence()
    {
        var state = await ConfirmAsync();
        var sessions = await GetWindowSessionsAsync(state.RollingId, 1, 1);
        var last = sessions[^1];
        foreach (var s in sessions[..^1])
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{s.Id}/complete",
                new { actual_distance_km = s.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();

        using var second = _factory.CreateClient();
        var responses = await Task.WhenAll(
            second.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }),
            _client.PostRawAsync($"/api/v1/training-days/rolling/{last.Id}/not-today", new { reason = "fatigue" }));

        Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(LongHorizonRollingSessionOutcomeStatus.NotToday,
                (await db.LongHorizonRollingSessionStates.AsNoTracking().SingleAsync(s => s.Id == last.Id)).OutcomeStatus);
        }

        var fresh = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        var freshBody = await fresh.Content.ReadAsStringAsync();
        Assert.True(fresh.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict, freshBody);
        if (fresh.StatusCode == HttpStatusCode.OK)
        {
            using var verify = _factory.Services.CreateScope();
            var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await verifyDb.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.RollingId && a.StartGlobalWeek == 2));
        }
    }

    [Fact]
    public async Task ActivationVsCancellation_IsCoherent_NoSessionsAfterCancellationWins()
    {
        var state = await ConfirmAsync();
        await CompleteCurrentWindowAsync(state.RollingId);

        using var second = _factory.CreateClient();
        var responses = await Task.WhenAll(
            _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 }),
            second.PostRawAsync($"/api/v1/plans/{state.PlanId}/cancel", new { reason = "race" }));

        Assert.Equal(HttpStatusCode.OK, responses[1].StatusCode);
        Assert.Contains(responses[0].StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Conflict });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(TrainingPlanStatus.Cancelled, (await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == state.PlanId)).Status);
        // The initial confirm already created window 1's activation record;
        // either the race's activation also committed window 2 (count 2) or
        // cancellation won first (count stays 1) -- never a partial mix.
        var activationCount = await db.LongHorizonActivationWindowRecords.CountAsync(a => a.PlanStateId == state.RollingId);
        Assert.True(activationCount is 1 or 2, $"activationCount={activationCount}");
        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Null(home["active_plan"]);
    }

    private async Task<ConfirmedState> ConfirmAndCrossIntoRunwayAsync()
    {
        var state = await ConfirmAsync();
        await CompleteCurrentWindowAsync(state.RollingId);
        var activate = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode); // the one proven-reachable shape: GE(week1) -> Runway.
        return state;
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

    /// <summary>
    /// Phase 4L.4E: terminalizes the current (Runway) window with every real
    /// long-run session left NotToday and every other session Completed --
    /// a genuine evidence gap. Since the role-normalization fix now
    /// correctly recognizes Runway long runs, fully completing the window
    /// (as Phase 4L.4D's fixture did) no longer blocks; this is the real
    /// replacement construction for a naturally reachable JitValidatedLoadUnavailable.
    /// </summary>
    private async Task LeaveRunwayLongRunsNotTodayAsync(Guid rollingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == rollingId);
        var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek
                && s.OutcomeStatus == LongHorizonRollingSessionOutcomeStatus.Planned)
            .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        foreach (var session in sessions)
        {
            if (LongHorizonSessionRoleCodec.IsLongRun(session.SessionRole))
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today", new { reason = "illness" })).EnsureSuccessStatusCode();
            else
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
    }

    private async Task<List<(Guid Id, double DistanceKm)>> GetWindowSessionsAsync(Guid rollingId, int startWeek, int endWeek)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId && s.Week.GlobalWeek >= startWeek && s.Week.GlobalWeek <= endWeek)
            .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal)
            .Select(s => new ValueTuple<Guid, double>(s.Id, s.DistanceKm)).ToListAsync();
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
            target_finish_time_source = "product_average", race_name = "Phase 4L.4D",
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
