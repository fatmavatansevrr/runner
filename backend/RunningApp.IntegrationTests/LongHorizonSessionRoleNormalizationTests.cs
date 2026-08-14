using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.Services;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.4E -- proves the session-role normalization fix. Root cause
/// (confirmed by direct repository inspection): LongHorizonRollingJitActivationRuntime
/// and LongHorizonRealCalendarProjectionAdapter derived Runway session roles
/// via the raw PreparationRunwaySlotRole.ToString() enum default ("LongRun")
/// instead of the canonical uppercase convention ("LONG_RUN") every other
/// GE/Core code path already used. Fixed by routing both call sites through
/// the new LongHorizonSessionRoleCodec.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonSessionRoleNormalizationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonSessionRoleNormalizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RunwaySessions_PersistCanonicalRole_NotPascalCaseEnumDefault()
    {
        var state = await ConfirmAndCrossIntoRunwayAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.SegmentType == LongHorizonPersistedSegmentType.PreparationRunway)
            .ToListAsync();
        Assert.NotEmpty(sessions);
        var longRunSessions = sessions.Where(s => s.SessionOrdinal == sessions.Where(x => x.Week.GlobalWeek == s.Week.GlobalWeek).Max(x => x.SessionOrdinal)).ToList();
        // Every persisted Runway role must be one of the canonical tokens -- never the raw PascalCase enum default.
        Assert.All(sessions, s => Assert.True(
            s.SessionRole is "KEY_SESSION" or "EASY_SUPPORT" or "LONG_RUN",
            $"Unexpected non-canonical role persisted: '{s.SessionRole}'"));
        Assert.DoesNotContain(sessions, s => s.SessionRole is "KeySession" or "EasySupport" or "LongRun");
        Assert.Contains(sessions, s => s.SessionRole == "LONG_RUN");
    }

    [Fact]
    public async Task CompletedRunwayLongRun_IsRecognizedAsEvidence_ValidatedLoadIsProducible()
    {
        // This is the actual defect regression: a genuinely completed Runway
        // long run must now contribute to completedLongRuns. Whether the
        // NEXT continuation can activate is a separate, independent question
        // (the real-calendar CheckpointWindowNotComplete gate, Phase 4L.4C/D
        // -- deliberately not bypassed here, per this phase's own scope).
        var state = await ConfirmAndCrossIntoRunwayAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        var windowSessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
            .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        Assert.Contains(windowSessions, s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));

        // Complete every session (including the real long run) via the real public endpoint.
        foreach (var session in windowSessions)
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();

        // Real evidence adapter output -- the actual regression assertion.
        var reloaded = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
            .ToListAsync();
        var evidence = LongHorizonRollingOutcomeEvidenceAdapter.ToCheckpointRows(reloaded);
        var completedLongRuns = evidence.Count(row => row.TrainingDay.IsLongRun && row.TrainingDay.Status == TrainingDayStatus.Completed && row.TrainingDay.ActualDistanceKm > 0);
        // The activated Runway window spans 4 weeks, one long run each --
        // each contributes exactly once (no double-counting, no misses).
        var expectedLongRunWeeks = windowSessions.Select(s => s.Week.GlobalWeek).Distinct().Count();
        Assert.Equal(expectedLongRunWeeks, completedLongRuns);

        // Now request continuation. Either it activates (role fix alone was
        // sufficient) or it is blocked by the separate, unrelated real-calendar
        // gate -- but it must NEVER be JitValidatedLoadUnavailable again for
        // this reason, since valid long-run evidence now exists.
        var activate = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        if (activate.StatusCode == HttpStatusCode.Conflict)
        {
            var body = await activate.Content.ReadAsStringAsync();
            Assert.Contains("LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS", body); // real-calendar gate, not evidence.
            var lastBlock = await db.LongHorizonBlockRetryRecords.AsNoTracking()
                .Where(b => b.PlanStateId == state.RollingId).OrderByDescending(b => b.CreatedAtUtc).FirstOrDefaultAsync();
            Assert.True(lastBlock is null || lastBlock.InternalReasonCode != "JitValidatedLoadUnavailable");
        }
    }

    [Fact]
    public async Task GenuineMissingLongRunEvidence_StillPersistsTypedBlock_NoRegressionToReadStateCorrupt()
    {
        // Preserves Phase 4L.4D's fix: a REAL evidence gap (long run left
        // NotToday) must still block with the existing typed, classified
        // Block -- role normalization must not weaken or bypass this.
        var state = await ConfirmAndCrossIntoRunwayAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
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

        var activate = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, activate.StatusCode);
        var body = await activate.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", body);
        Assert.DoesNotContain("LONG_HORIZON_READ_STATE_CORRUPT", body); // no regression to the old unclassified error.

        using var verify = _factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var block = await verifyDb.LongHorizonBlockRetryRecords.AsNoTracking()
            .Where(b => b.PlanStateId == state.RollingId).OrderByDescending(b => b.CreatedAtUtc).FirstAsync();
        Assert.Equal("JitValidatedLoadUnavailable", block.InternalReasonCode);
        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Equal("regenerate_preview_required", home["active_plan"]!["recovery_requirement"]!.GetValue<string>());
    }

    [Fact]
    public async Task PublicHomeAndCalendar_ExposeCanonicalRunwayRole_IsLongRunCorrect()
    {
        var state = await ConfirmAndCrossIntoRunwayAsync();

        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        var sessions = home["current_window_sessions"]!.AsArray();
        Assert.NotEmpty(sessions);
        var longRunSessions = sessions.Where(s => s!["is_long_run"]!.GetValue<bool>()).ToList();
        Assert.NotEmpty(longRunSessions); // one per activated Runway week.
        Assert.All(longRunSessions, s => Assert.Equal("LONG_RUN", s!["workout_role"]!.GetValue<string>()));
        Assert.All(sessions, s => Assert.True(
            s!["workout_role"]!.GetValue<string>() is "KEY_SESSION" or "EASY_SUPPORT" or "LONG_RUN",
            $"Unexpected public workout_role: {s!["workout_role"]}"));
    }

    [Fact]
    public void RoleCodec_RecognizesCanonicalAndLegacyTokens_RejectsUnknown()
    {
        Assert.True(LongHorizonSessionRoleCodec.IsLongRun("LONG_RUN"));
        Assert.True(LongHorizonSessionRoleCodec.IsLongRun("long_run"));
        Assert.True(LongHorizonSessionRoleCodec.IsLongRun("LongRun")); // approved legacy token.
        Assert.False(LongHorizonSessionRoleCodec.IsLongRun("longrun")); // not the exact legacy form.
        Assert.False(LongHorizonSessionRoleCodec.IsLongRun("KEY_SESSION"));
        Assert.False(LongHorizonSessionRoleCodec.IsLongRun("KeySession"));
        Assert.False(LongHorizonSessionRoleCodec.IsLongRun(null));
        Assert.False(LongHorizonSessionRoleCodec.IsLongRun("SOMETHING_UNKNOWN"));

        Assert.True(LongHorizonSessionRoleCodec.IsKeySession("KEY_SESSION"));
        Assert.True(LongHorizonSessionRoleCodec.IsKeySession("KeySession"));
        Assert.False(LongHorizonSessionRoleCodec.IsKeySession("LONG_RUN"));

        Assert.True(LongHorizonSessionRoleCodec.IsEasySupport("EASY_SUPPORT"));
        Assert.True(LongHorizonSessionRoleCodec.IsEasySupport("EASY_SUPPORT_1")); // GE's suffixed form.
        Assert.True(LongHorizonSessionRoleCodec.IsEasySupport("EASY_SUPPORT_2"));
        Assert.True(LongHorizonSessionRoleCodec.IsEasySupport("EasySupport"));
        Assert.False(LongHorizonSessionRoleCodec.IsEasySupport("LONG_RUN"));
    }

    [Fact]
    public void RoleCodec_ToCanonicalToken_NeverEqualsRawEnumDefault()
    {
        foreach (RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization.PreparationRunwaySlotRole role in
                 Enum.GetValues<RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization.PreparationRunwaySlotRole>())
        {
            var canonical = LongHorizonSessionRoleCodec.ToCanonicalToken(role);
            Assert.Equal(canonical.ToUpperInvariant(), canonical); // canonical tokens are always uppercase.
            Assert.NotEqual(role.ToString(), canonical); // never the raw PascalCase enum default.
        }
    }

    [Fact]
    public void RoleCodec_TryParseCanonicalOrLegacy_RoundTripsAndFailsClosedForUnknown()
    {
        Assert.True(LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy("LONG_RUN", out var r1));
        Assert.Equal(RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization.PreparationRunwaySlotRole.LongRun, r1);
        Assert.True(LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy("LongRun", out var r2));
        Assert.Equal(RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization.PreparationRunwaySlotRole.LongRun, r2);
        Assert.False(LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy("SOMETHING_UNKNOWN", out _));
        Assert.False(LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy(null, out _));
        Assert.False(LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy("EASY_SUPPORT_1", out _)); // GE-suffixed form is not a plain enum value.
    }

    private async Task<ConfirmedState> ConfirmAndCrossIntoRunwayAsync()
    {
        var state = await ConfirmAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
            foreach (var session in sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
        var activate = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
        return state;
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
            target_finish_time_source = "product_average", race_name = "Phase 4L.4E",
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
