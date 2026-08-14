using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests;

/// <summary>
/// Phase 4L.4B/4L.4C -- the public Blocked-recovery surface.
///
/// Phase 4L.4C empirical correction: direct investigation (see
/// LongHorizonBlockRecoveryClassificationTests for the pure taxonomy proof)
/// found that the naturally reachable block for a pure-GE continuation in
/// this environment is <c>CheckpointWindowNotComplete</c> -- a real,
/// currently-unelapsed real-calendar-time condition
/// (LongHorizonCheckpointEvidenceAggregator's own <c>periodEnded</c> check,
/// which fires before any evidence-completeness check regardless of session
/// outcomes) -- not <c>ValidatedLongRunEvidenceUnavailable</c> as Phase
/// 4L.4B assumed without directly verifying the persisted reason code. This
/// is legitimately RecoverableWithElapsedCalendarTime: retry is correctly
/// ALLOWED for it. The RequiresRegeneratePreview path (e.g. a genuine
/// ValidatedLongRunEvidenceUnavailable block) is proven here by directly
/// seeding the exact durable fields <c>SaveBlockAsync</c> itself would set
/// for that reason code -- the same technique Phase 4L.2's own tests already
/// use to construct specific block scenarios, not a new hack.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonRetryContinuationTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonRetryContinuationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task NoBlockedBoundary_ReturnsTypedConflict_AndPerformsNoWrite()
    {
        await ConfirmAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("LONG_HORIZON_NO_BLOCKED_BOUNDARY", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnsupportedContractVersion_IsRejected_AndPerformsNoWrite()
    {
        await ConfirmAsync();
        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 99 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("LONG_HORIZON_CONTINUATION_VERSION_UNSUPPORTED", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NoActivePlan_IsNonDisclosingNotFound()
    {
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("LONG_HORIZON_ACTIVE_PLAN_NOT_FOUND", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ActivationWhileBlocked_ReturnsRetryRequired_AndCreatesNoSessions()
    {
        var state = await ConfirmAsync();
        await BlockCurrentWindowViaRealCheckpointAsync(state);

        var second = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("LONG_HORIZON_RETRY_REQUIRED", await second.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.LongHorizonRollingSessionStates.CountAsync(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek == state.ActivatedWeekCount + 1));
    }

    [Fact]
    public async Task Home_ExposesRecoveryRequirementAndBlockedReasonCategory_WhenBlocked()
    {
        var state = await ConfirmAsync();
        await BlockCurrentWindowViaRealCheckpointAsync(state);

        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        var plan = home["active_plan"]!;
        Assert.Equal("reassessment_required", plan["checkpoint_readiness"]!.GetValue<string>());
        Assert.Equal("calendar_window_pending", plan["recovery_requirement"]!.GetValue<string>());
        Assert.Equal("MoreTrainingDataNeeded", plan["blocked_public_reason_category"]!.GetValue<string>());
        Assert.DoesNotContain("CheckpointWindowNotComplete", home.ToJsonString());
    }

    [Fact]
    public async Task RecoverableWithElapsedCalendarTime_RetrySucceedsOnceCheckpointDateAdvances()
    {
        // The real, naturally reachable case: CheckpointWindowNotComplete.
        var state = await ConfirmAsync();
        await BlockCurrentWindowViaRealCheckpointAsync(state);
        AssertReasonCode(state.RollingId, "CheckpointWindowNotComplete");

        // Retry immediately (same real-world day as the block) still fails
        // the repository's own strictly-later-checkpoint-date guard --
        // proving this phase's new eligibility gate doesn't bypass that
        // existing invariant.
        var tooSoon = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooSoon.StatusCode);
        Assert.Contains("LONG_HORIZON_RETRY_NOT_ELIGIBLE", await tooSoon.Content.ReadAsStringAsync());

        await BackdateLatestCheckpointDateAsync(state.RollingId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
        var retry = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 }));
        Assert.Equal("restored_to_pending", retry["outcome"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericPending, aggregate.CurrentLifecycleStatus);
    }

    [Fact]
    public async Task RequiresRegeneratePreview_RetryRejectsWithNoMutation_ForImmutableEvidenceBlock()
    {
        var state = await ConfirmAsync();
        // Directly seeds the exact durable fields SaveBlockAsync itself
        // would persist for a genuine ValidatedLongRunEvidenceUnavailable
        // block -- the same construction technique Phase 4L.2's own tests
        // already use, not a new fixture-fabrication pattern.
        await SeedImmutableEvidenceBlockAsync(state.RollingId, "ValidatedLongRunEvidenceUnavailable");
        var before = await SnapshotAsync(state.RollingId);

        var retry = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Contains("LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED", await retry.Content.ReadAsStringAsync());

        var after = await SnapshotAsync(state.RollingId);
        Assert.Equal(before, after); // no meaningless state churn.

        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Equal("regenerate_preview_required", home["active_plan"]!["recovery_requirement"]!.GetValue<string>());
    }

    [Fact]
    public async Task OperationalSupportRequired_RetryRejectsWithNoMutation_ForSafetyBlock()
    {
        var state = await ConfirmAsync();
        await SeedImmutableEvidenceBlockAsync(state.RollingId, "SafetyReassessmentRequired");

        var retry = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Contains("LONG_HORIZON_OPERATIONAL_SUPPORT_REQUIRED", await retry.Content.ReadAsStringAsync());

        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Equal("operational_support_required", home["active_plan"]!["recovery_requirement"]!.GetValue<string>());
    }

    [Fact]
    public async Task RepeatedRetryAgainstImmutableEvidence_ConsistentlyRejected_NoStateChurn()
    {
        var state = await ConfirmAsync();
        await SeedImmutableEvidenceBlockAsync(state.RollingId, "NumericWindowInfeasible");

        var first = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 });
        var second = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == state.RollingId && b.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored));
    }

    [Fact]
    public async Task ConcurrentRetryAgainstImmutableEvidence_BothRejected_NoPartialMutation()
    {
        var state = await ConfirmAsync();
        await SeedImmutableEvidenceBlockAsync(state.RollingId, "ValidatedLoadUnavailable");
        using var second = _factory.CreateClient();

        var responses = await Task.WhenAll(
            _client.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 }),
            second.PostRawAsync("/api/v1/plans/active/long-horizon/retry", new { contract_version = 1 }));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == state.RollingId && b.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored));
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericActivationBlocked, aggregate.CurrentLifecycleStatus);
    }

    [Fact]
    public async Task RegeneratePreview_ViaExistingCancelAndNewConfirm_IsTheApprovedRecoveryPath()
    {
        var state = await ConfirmAsync();
        await SeedImmutableEvidenceBlockAsync(state.RollingId, "ValidatedLongRunEvidenceUnavailable");

        // The declared recovery path for an evidence-immutable block reuses
        // 100% existing, unmodified capability: cancel the blocked plan (one
        // active plan is enforced), then generate and confirm a new preview.
        (await _client.PostRawAsync($"/api/v1/plans/{state.PlanId}/cancel", new { reason = "blocked_regenerate" })).EnsureSuccessStatusCode();

        var start = new DateOnly(2026, 9, 7);
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", new
        {
            goal_distance = "ten_k", level = "intermediate", days_per_week = 4, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = new[] { "mon", "wed", "fri", "sun" }, long_run_day = "sun",
            race_date = start.AddDays(45 * 7).ToString("yyyy-MM-dd"), target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average", race_name = "Phase 4L.4C regenerate",
            recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = 4
        });
        var preview = await JsonAsync(previewResponse);
        var newRollingId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = newRollingId }));
        var newPlanId = confirm["plan_id"]!.GetValue<Guid>();
        Assert.NotEqual(state.PlanId, newPlanId);

        var home = await JsonAsync(await _client.GetAsync("/api/v1/plans/active/home"));
        Assert.Equal(newPlanId, home["active_plan"]!["plan_id"]!.GetValue<Guid>());
        Assert.Equal("current_window_in_progress", home["active_plan"]!["checkpoint_readiness"]!.GetValue<string>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(TrainingPlanStatus.Cancelled, (await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == state.PlanId)).Status);
        Assert.True(await db.LongHorizonRollingPlanStates.AnyAsync(p => p.Id == state.RollingId));
        Assert.Equal(LongHorizonPersistedLifecycleState.NumericActivationBlocked,
            (await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId)).CurrentLifecycleStatus);
    }

    [Fact]
    public void PublicContractGraph_DoesNotExposePersistenceOrInternalAuthority()
    {
        var types = new HashSet<Type>();
        Walk(typeof(LongHorizonRetryContinuationResponse), types);
        Assert.DoesNotContain(types, t => t.Namespace == "RunningApp.Domain.Entities");
        var names = types.SelectMany(t => t.GetProperties()).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var forbidden in new[] { "TargetLock", "RunwayPrescription", "CoreContext", "EvidenceFingerprint", "CheckpointRecord", "Xmin", "IdempotencyKey", "FailureInjector", "BlockId", "RetryLineage" })
            Assert.DoesNotContain(forbidden, names);
    }

    [Fact]
    public async Task Swagger_ContainsRetryRouteAndOutcomeEnum()
    {
        var swagger = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/v1/plans/active/long-horizon/retry", swagger);
        Assert.Contains("LongHorizonRetryOutcome", swagger);
        Assert.Contains("LongHorizonRecoveryRequirement", swagger);
    }

    /// <summary>Real block via the actual checkpoint runtime: activating a
    /// pure-GE second window before its real calendar days have elapsed.</summary>
    private async Task BlockCurrentWindowViaRealCheckpointAsync(ConfirmedState state)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking()
                .Where(s => s.Week.PlanStateId == state.RollingId).OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
            foreach (var session in sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }

        var activate = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, activate.StatusCode);
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", await activate.Content.ReadAsStringAsync());
    }

    /// <summary>Directly persists the exact durable fields the real
    /// SaveBlockAsync sets for the given reason code, so this phase's
    /// classification-gated retry logic can be proven for reason codes not
    /// naturally reachable through pure-GE continuation in this test
    /// environment (see the file-level doc comment).</summary>
    private async Task SeedImmutableEvidenceBlockAsync(Guid rollingId, string internalReasonCode)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == rollingId);
        var boundaryStart = plan.CurrentWindowEndWeek + 1;
        var boundaryEnd = Math.Min(boundaryStart + 3, plan.TotalWeeks);
        var now = DateTime.UtcNow;
        plan.CurrentLifecycleStatus = LongHorizonPersistedLifecycleState.NumericActivationBlocked;
        plan.CurrentBlockedPublicReasonCategory = "MoreTrainingDataNeeded";
        plan.CurrentBlockedInternalReasonCode = internalReasonCode;
        plan.BlockedAt = DateOnly.FromDateTime(now);
        plan.RetryEligible = true;
        plan.LatestCheckpointDate = DateOnly.FromDateTime(now);
        plan.UpdatedAtUtc = now;
        db.LongHorizonBlockRetryRecords.Add(new LongHorizonBlockRetryRecord
        {
            Id = Guid.NewGuid(), PlanStateId = rollingId, EventType = LongHorizonPersistedBlockRetryEventType.Block,
            BlockedGlobalWeekStart = boundaryStart, BlockedGlobalWeekEnd = boundaryEnd,
            PublicReasonCategory = "MoreTrainingDataNeeded", InternalReasonCode = internalReasonCode,
            EvidenceFingerprint = "seeded-evidence-fingerprint", CheckpointDate = DateOnly.FromDateTime(now),
            RetryEligible = true, CreatedAtUtc = now,
        });
        await db.SaveChangesAsync();
    }

    private async Task BackdateLatestCheckpointDateAsync(Guid rollingId, DateOnly date)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.LongHorizonRollingPlanStates.SingleAsync(p => p.Id == rollingId);
        plan.LatestCheckpointDate = date;
        await db.SaveChangesAsync();
    }

    private void AssertReasonCode(Guid rollingId, string expected)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var block = db.LongHorizonBlockRetryRecords.AsNoTracking()
            .Where(b => b.PlanStateId == rollingId && b.EventType == LongHorizonPersistedBlockRetryEventType.Block)
            .OrderByDescending(b => b.CreatedAtUtc).First();
        Assert.Equal(expected, block.InternalReasonCode);
    }

    private async Task<(LongHorizonPersistedLifecycleState Status, bool RetryEligible, DateOnly? LatestCheckpointDate, int BlockCount, int RetryCount)> SnapshotAsync(Guid rollingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == rollingId);
        return (aggregate.CurrentLifecycleStatus, aggregate.RetryEligible, aggregate.LatestCheckpointDate,
            await db.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == rollingId && b.EventType == LongHorizonPersistedBlockRetryEventType.Block),
            await db.LongHorizonBlockRetryRecords.CountAsync(b => b.PlanStateId == rollingId && b.EventType == LongHorizonPersistedBlockRetryEventType.RetryRestored));
    }

    private async Task<ConfirmedState> ConfirmAsync()
    {
        (await _client.PostRawAsync("/api/v1/testing/reset")).EnsureSuccessStatusCode();
        var start = new DateOnly(2026, 9, 7);
        var previewResponse = await _client.PostRawAsync("/api/v1/plans/generate-preview/race/long-horizon", new
        {
            goal_distance = "ten_k", level = "intermediate", days_per_week = 4, unit = "km",
            start_date = start.ToString("yyyy-MM-dd"), preferred_days = new[] { "mon", "wed", "fri", "sun" }, long_run_day = "sun",
            race_date = start.AddDays(45 * 7).ToString("yyyy-MM-dd"), target_finish_time_seconds = 3480,
            target_finish_time_source = "product_average", race_name = "Phase 4L.4C",
            recent_weekly_volume_km = 20.0, recent_longest_run_km = 8.0, recent_runs_per_week = 4
        });
        var preview = await JsonAsync(previewResponse);
        var rollingId = preview["preview_id"]!.GetValue<Guid>();
        var confirm = await JsonAsync(await _client.PostRawAsync("/api/v1/plans/confirm/long-horizon", new { preview_id = rollingId }));
        var planId = confirm["plan_id"]!.GetValue<Guid>();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = await db.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        var sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == rollingId).OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
        return new ConfirmedState(planId, rollingId, plan.InternalUserId!.Value, sessions.Select(s => s.Week.GlobalWeek).Distinct().Count());
    }

    private static async Task<JsonNode> JsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonNode.Parse(body)!;
    }

    private static void Walk(Type type, ISet<Type> seen)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(DateTime) || !seen.Add(type)) return;
        if (type.IsArray) { Walk(type.GetElementType()!, seen); return; }
        if (type.IsGenericType) foreach (var argument in type.GetGenericArguments()) Walk(argument, seen);
        foreach (var property in type.GetProperties()) Walk(property.PropertyType, seen);
    }

    private sealed record ConfirmedState(Guid PlanId, Guid RollingId, Guid OwnerId, int ActivatedWeekCount);
}
