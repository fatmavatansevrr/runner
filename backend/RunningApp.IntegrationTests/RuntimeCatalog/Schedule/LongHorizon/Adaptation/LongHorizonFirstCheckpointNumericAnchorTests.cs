using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4B.2 confirmation pass (§A/§B) -- real, HTTP-driven proof of the
/// exact anchor-selection behavior at a plan's very first-ever checkpoint,
/// where PriorValidatedCheckpointLoad has never been recorded yet. All three
/// reachable branches (Maintain-with-evidence, Reduce-with-evidence,
/// Reduce-zero-evidence) are exercised against the real TEN_K/Intermediate/4D
/// pilot, not the pure selector in isolation -- this proves the real
/// checkpoint runtime actually reaches the same code paths already verified
/// at the unit level.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonFirstCheckpointNumericAnchorTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonFirstCheckpointNumericAnchorTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// §A: first-ever checkpoint, EffectiveCompletedCount == 0 (no session in
    /// the window completed at all -- illness on everything). PriorValidatedCheckpointLoad
    /// is absent (nothing has ever been checkpointed) AND ValidatedSustainableLoad
    /// is absent (the evidence aggregator's own "needs >=1 completed long run"
    /// gate is never satisfied). Both selector inputs are null -&gt; the
    /// selector returns null -&gt; the existing, unmodified
    /// isJitEvidenceUnavailable check blocks activation with the same typed
    /// 409 this repository already used before Phase 4M.4B.2 for "no
    /// evidence, no prior" -- no numeric fallback, no percentage, no default
    /// value of any kind is invoked.
    /// </summary>
    [Fact]
    public async Task FirstCheckpoint_ZeroCompletion_NoPriorNoEvidence_BlocksWithExistingTypedConflict_NoNumericFallback()
    {
        var state = await ConfirmAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).ToListAsync();
            foreach (var session in sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                    new { reason = "illness" })).EnsureSuccessStatusCode();
        }

        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", body);
        // No numeric leakage of any kind in the sanitized error body.
        Assert.DoesNotContain("WeeklyVolumeKm", body);

        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        // Window did not advance -- no partial/fallback activation occurred.
        Assert.Equal(1, aggregate.CurrentWindowStartWeek);
    }

    /// <summary>
    /// §B Case 1 (revised in Phase 4M.4B.2A): first-ever checkpoint,
    /// LoadDecision = Maintain (EffectiveCompletedCount = 2, unconditional
    /// Maintain bracket), PriorValidatedCheckpointLoad absent. The
    /// selector's Maintain branch itself is unchanged and still correctly
    /// falls back to this window's own ValidatedSustainableLoad. But at
    /// this pilot's real roadmap length, the General Endurance phase is
    /// fully consumed by the plan's very first window (confirmed via direct
    /// TotalWeeks/GeneralEnduranceWeeks inspection during the 4M.4B.2A
    /// defect investigation), so the *next* checkpoint always routes
    /// through real Runway/Core JIT composition
    /// (<see cref="RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.Persistence.LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync"/>),
    /// not the GE materializer -- and that composition path independently,
    /// legitimately declines (Block) whenever the checkpointed window has
    /// any NotToday session, regardless of LoadDecision. This is real,
    /// pre-existing JIT/Runway composition behavior the 4M.4B.2A window-
    /// advancement fix now correctly surfaces as a typed 409 instead of a
    /// previously-masked false "activated" response (see
    /// PHASE4M_4B_2A_MULTIWINDOW_ACTIVATION_ADVANCEMENT_DEFECT.md). Not a
    /// Rev4 regression: the anchor-selection Maintain branch itself is
    /// separately proven correct in NextWindowNumericAnchorSelectorTests
    /// and in the real GE-materializer path once GE weeks remain (see
    /// LongHorizonNumericAnchorMaterializationE2ETests).
    /// </summary>
    [Fact]
    public async Task FirstCheckpoint_Maintain_WithEvidence_NoPrior_BlocksViaRealJitRunwayEvidenceCompletenessRequirement()
    {
        var state = await ConfirmAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).ToListAsync();
            // Exactly 2 completed (one of which is a long run, satisfying the
            // aggregator's gate) -> EffectiveCompletedCount = 2 -> Maintain,
            // unconditionally, regardless of which two roles.
            var toComplete = new[]
            {
                sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole)),
                sessions.First(s => LongHorizonSessionRoleCodec.IsEasySupport(s.SessionRole)),
            };
            foreach (var session in sessions)
            {
                if (toComplete.Any(t => t.Id == session.Id))
                    (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                        new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
                else
                    (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                        new { reason = "illness" })).EnsureSuccessStatusCode();
            }
        }

        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", body);

        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        // Critical invariant proven by the 4M.4B.2A fix: a genuine Block
        // must never masquerade as a real window advancement.
        Assert.Equal(1, aggregate.CurrentWindowStartWeek);
        Assert.Equal(1, aggregate.CurrentWindowEndWeek);
    }

    /// <summary>
    /// §B Case 2 (revised in Phase 4M.4B.2A): first-ever checkpoint,
    /// LoadDecision = Reduce with EffectiveCompletedCount &gt; 0,
    /// PriorValidatedCheckpointLoad absent. Same finding as the Maintain
    /// case above: the selector's Reduce branch (Rev4's own literal
    /// "min(undefined, X) = X" formula) is unchanged and correct, but this
    /// pilot's real roadmap routes the very next checkpoint through Runway/
    /// Core JIT composition, which legitimately declines on any NotToday
    /// evidence. See the Maintain test's doc comment above for the full
    /// explanation and PHASE4M_4B_2A_MULTIWINDOW_ACTIVATION_ADVANCEMENT_DEFECT.md.
    /// </summary>
    [Fact]
    public async Task FirstCheckpoint_Reduce_WithEvidence_NoPrior_BlocksViaRealJitRunwayEvidenceCompletenessRequirement()
    {
        var state = await ConfirmAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).ToListAsync();
            var longRun = sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));
            foreach (var session in sessions)
            {
                if (session.Id == longRun.Id)
                    (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                        new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
                else
                    (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                        new { reason = "illness" })).EnsureSuccessStatusCode();
            }
        }

        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", body);

        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(1, aggregate.CurrentWindowStartWeek);
        Assert.Equal(1, aggregate.CurrentWindowEndWeek);
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
            target_finish_time_source = "product_average", race_name = "Phase 4M.4B.2 Confirmation",
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
