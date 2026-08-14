using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4B.2 confirmation pass (§E), advanced across Phase 4M.4B.2A
/// (window-advancement routing defect, fixed) and Phase 4M.4B.2B (the
/// Core/Runway JIT "Maintain rejection" investigation).
///
/// Phase 4M.4B.2B's finding: there is no Maintain-specific defect.
/// `CoreJitContextUnavailable`/`DynamicCoreSessionPrescriptionFailedException`
/// ("Week N residual volume Xkm cannot support V1 key/easy minimums") is
/// the catalog's own, real, pre-existing minimum-session-volume validation,
/// symmetrically rejecting ANY numerically small carried anchor --
/// confirmed by directly reproducing the identical failure through a real
/// Reduce-selected anchor of the same magnitude at the same Runway->Core
/// boundary (temporary diagnostic, not part of permanent coverage). It has
/// nothing to do with which Rev4 branch (Maintain or Reduce) produced the
/// anchor, and nothing to do with an evidence-authority/plumbing gap:
/// Maintain's `PriorValidatedCheckpointLoad` is already an accepted,
/// correctly-plumbed authority into the same typed Core-generation input
/// Reduce and ProgressAsPlanned use (see `RealMaintainActivation_...` below,
/// a genuine, currently-passing real HTTP Maintain activation using that
/// exact carried value).
///
/// The one combination that legitimately still Blocks is Reduce
/// (necessarily producing a small anchor, since `EffectiveCompletedCount`
/// &lt;= 1 is what makes it Reduce in the first place) landing exactly on
/// this pilot's Runway->Core boundary and then being held forward via
/// Maintain (the only decision that propagates a value without
/// re-aggregating fresh evidence) -- this is disclosed below as a real,
/// understood, product-level DecisionRequired item, not forced to pass and
/// not hidden.
/// </summary>
[Collection(ApiIntegrationTestCollection.Name)]
public sealed class LongHorizonThreeWindowAnchorThreadingE2ETests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LongHorizonThreeWindowAnchorThreadingE2ETests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Phase 4M.4B.2B (§G) -- real Maintain E2E activation proof. Window 0
    /// fully completed (real ProgressAsPlanned, establishes a genuine, rich
    /// prior anchor). Window 1 evidenced to unconditionally reach Maintain
    /// (exactly 2 completed, one a LONG_RUN). Maintain's anchor is
    /// `PriorValidatedCheckpointLoad` verbatim -- Window 0's real,
    /// unreduced evidence -- which real Core/Runway JIT composition
    /// successfully consumes: the plan's first Runway/Core entry point has
    /// low enough minimums for this real, un-shrunk anchor to satisfy them.
    ///
    /// Phase 4M.5C update -- Rev5 §7a: Window 1 is a real 4-structural-week
    /// window, and NextWindowLoadDecisionPolicy is now correctly evaluated
    /// once per real structural week (never against the whole window
    /// directly), then aggregated via B1 worst-week-wins. Evidence is
    /// therefore evenly distributed -- exactly one LONG_RUN and one
    /// EASY_SUPPORT completed IN EVERY real structural week (not just the
    /// first) -- so every week's own weekly decision is independently
    /// Maintain (2/4 completed each), and B1's worst-of-four is therefore
    /// still Maintain. Concentrating both completions into only the first
    /// week (the pre-4M.5C fixture) would now correctly aggregate to Reduce
    /// (the other three weeks would each be 0/4), which is a different,
    /// still-real scenario, not what this test exists to prove.
    /// </summary>
    [Fact]
    public async Task RealMaintainActivation_UsesPriorValidatedCheckpointLoadVerbatim_GenuinelyAdvancesWindow()
    {
        var state = await ConfirmAsync();

        int window0Start, window0End;
        double window0Total;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            window0Start = aggregate.CurrentWindowStartWeek;
            window0End = aggregate.CurrentWindowEndWeek;
            var window0Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).ToListAsync();
            window0Total = window0Sessions.Sum(s => s.DistanceKm);
            foreach (var session in window0Sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
        var activate0 = await ActivateAsync(state.RollingId);
        Assert.Equal("activated", activate0["outcome"]!.GetValue<string>());
        Assert.Equal("progress_as_planned", activate0["next_window_load_decision"]!.GetValue<string>());

        int window1Start, window1End;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            window1Start = aggregate.CurrentWindowStartWeek;
            window1End = aggregate.CurrentWindowEndWeek;
            Assert.True(window1Start > window0End, $"Window 1 [{window1Start}-{window1End}] did not advance past Window 0 [{window0Start}-{window0End}].");
            var window1Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
            // No sessions from Window 0 leak into Window 1 -- fresh materialization.
            Assert.All(window1Sessions, s => Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, s.OutcomeStatus));

            // Phase 4M.5C: exactly one LONG_RUN and one EASY_SUPPORT
            // completed PER real structural week (not just the window's
            // first), so every week's own weekly decision is independently
            // Maintain and B1's worst-week-wins stays Maintain.
            var toComplete = window1Sessions
                .GroupBy(s => s.Week.GlobalWeek)
                .SelectMany(week => new[]
                {
                    week.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole)),
                    week.First(s => LongHorizonSessionRoleCodec.IsEasySupport(s.SessionRole)),
                })
                .ToList();
            foreach (var session in toComplete)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
            foreach (var session in window1Sessions.Where(s => toComplete.All(t => t.Id != s.Id)))
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                    new { reason = "illness" })).EnsureSuccessStatusCode();
        }

        var activate1 = await ActivateAsync(state.RollingId);
        Assert.Equal("activated", activate1["outcome"]!.GetValue<string>());
        Assert.Equal("maintain", activate1["next_window_load_decision"]!.GetValue<string>());

        // ── Fresh-DbContext proof (§G): new window identity/range, new
        // activation record, all Window 2 sessions Planned (no stale
        // Completed/NotToday reuse), materialized numeric target matches
        // Window 0's held (Maintain) level, not a freshly-grown one, no
        // duplicate activation. ──
        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var freshAggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.True(freshAggregate.CurrentWindowStartWeek > window1End,
            $"Window 2 [{freshAggregate.CurrentWindowStartWeek}-{freshAggregate.CurrentWindowEndWeek}] did not advance past Window 1 [{window1Start}-{window1End}].");
        var window2Sessions = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= freshAggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= freshAggregate.CurrentWindowEndWeek)
            .ToListAsync();
        Assert.NotEmpty(window2Sessions);
        Assert.All(window2Sessions, s => Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, s.OutcomeStatus));
        Assert.All(window2Sessions, s => Assert.True(s.DistanceKm > 0));
        Assert.All(window2Sessions, s => Assert.False(string.IsNullOrWhiteSpace(s.SessionRole)));
        var window2Total = window2Sessions.Where(s => s.Week.GlobalWeek == freshAggregate.CurrentWindowStartWeek).Sum(s => s.DistanceKm);
        // Held anchor, not freshly grown: Window 2's first materialized week
        // is numerically consistent with Window 0's own real total, not a
        // normal-progression-grown value.
        Assert.True(Math.Abs(window2Total - window0Total) < 0.5,
            $"Window 2's first week total ({window2Total}km) diverged from Window 0's held level ({window0Total}km) despite Maintain holding the prior anchor verbatim.");

        var activationRecords = await freshDb.LongHorizonActivationWindowRecords.AsNoTracking()
            .Where(a => a.PlanStateId == state.RollingId).ToListAsync();
        Assert.Equal(3, activationRecords.Count); // initial window0 + 2 real transitions
        Assert.Equal(activationRecords.Count, activationRecords.Select(a => a.IdempotencyKey).Distinct().Count());
    }

    /// <summary>
    /// Phase 4M.4B.2 (§E) / 4M.4B.2A -- real Reduce anchor threading,
    /// unaffected by the 4M.4B.2B finding (this transition never reaches
    /// Core generation at all -- it's a pure Runway continuation). Proven
    /// separately from the Maintain leg above because Reduce and Maintain
    /// have different selector branches and different real evidence shapes.
    /// </summary>
    [Fact]
    public async Task RealReduceActivation_ThreadsAnchorCorrectly_GenuinelyAdvancesWindow()
    {
        var state = await ConfirmAsync();

        int window0End;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            window0End = aggregate.CurrentWindowEndWeek;
            var window0Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).ToListAsync();
            foreach (var session in window0Sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
        await ActivateAsync(state.RollingId);

        int window1End;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            window1End = aggregate.CurrentWindowEndWeek;
            Assert.True(aggregate.CurrentWindowStartWeek > window0End);
            var window1Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
            var firstLongRun = window1Sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{firstLongRun.Id}/complete",
                new { actual_distance_km = firstLongRun.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
            foreach (var session in window1Sessions.Where(s => s.Id != firstLongRun.Id))
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                    new { reason = "illness" })).EnsureSuccessStatusCode();
        }

        var activate1 = await ActivateAsync(state.RollingId);
        Assert.Equal("activated", activate1["outcome"]!.GetValue<string>());
        Assert.Equal("reduce", activate1["next_window_load_decision"]!.GetValue<string>());

        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var freshAggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.True(freshAggregate.CurrentWindowStartWeek > window1End,
            $"Window 2 [{freshAggregate.CurrentWindowStartWeek}-{freshAggregate.CurrentWindowEndWeek}] did not advance past Window 1's end week {window1End}.");
        var window2Sessions = await freshDb.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
            .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= freshAggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= freshAggregate.CurrentWindowEndWeek)
            .ToListAsync();
        Assert.All(window2Sessions, s => Assert.Equal(LongHorizonRollingSessionOutcomeStatus.Planned, s.OutcomeStatus));
    }

    /// <summary>
    /// Phase 4M.4B.2B (§B/§C/§E) -- explicit, disclosed characterization of
    /// the real remaining DecisionRequired item: a Reduce decision that
    /// necessarily produces a small anchor (EffectiveCompletedCount &lt;= 1
    /// is what makes it Reduce), landing exactly on this pilot's real
    /// Runway->Core boundary, and then held forward unchanged by a
    /// subsequent Maintain (the only decision that does not re-aggregate
    /// fresh evidence). Root-caused via a real, unmodified
    /// `TenKPreparationRunwayDarkOrchestrator`/`DynamicCoreCalendarMaterializationOrchestrator`
    /// rejection: "Week 12 residual volume 5.5km cannot support V1
    /// key/easy minimums" -- the catalog's own real minimum-session-volume
    /// validation, confirmed (via a temporary diagnostic, not retained) to
    /// reject an equally-small real Reduce-selected anchor identically at
    /// the same boundary, proving this is not Maintain-specific. Per this
    /// investigation's explicit standard ("if the only way to make Maintain
    /// pass is to weaken a product/domain rule whose intent is unclear,
    /// STOP and report DecisionRequired"), this is asserted here rather
    /// than forced to pass or hidden.
    /// </summary>
    [Fact]
    public async Task RealChain_ReduceLandingOnRunwayCoreBoundary_ThenMaintain_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement()
    {
        var state = await ConfirmAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var window0Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).ToListAsync();
            foreach (var session in window0Sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
        await ActivateAsync(state.RollingId);

        int window1Start, window1End;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            window1Start = aggregate.CurrentWindowStartWeek;
            window1End = aggregate.CurrentWindowEndWeek;
            var window1Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
            var firstLongRun = window1Sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{firstLongRun.Id}/complete",
                new { actual_distance_km = firstLongRun.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
            foreach (var session in window1Sessions.Where(s => s.Id != firstLongRun.Id))
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                    new { reason = "illness" })).EnsureSuccessStatusCode();
        }

        var activate1 = await ActivateAsync(state.RollingId);
        Assert.Equal("activated", activate1["outcome"]!.GetValue<string>());
        Assert.Equal("reduce", activate1["next_window_load_decision"]!.GetValue<string>());

        int window2Start, window2End;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            window2Start = aggregate.CurrentWindowStartWeek;
            window2End = aggregate.CurrentWindowEndWeek;
            Assert.True(window2Start > window1End, $"Window 2 [{window2Start}-{window2End}] did not advance past Window 1 [{window1Start}-{window1End}].");
            var window2Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();

            var toComplete = new[]
            {
                window2Sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole)),
                window2Sessions.First(s => LongHorizonSessionRoleCodec.IsEasySupport(s.SessionRole)),
            };
            foreach (var session in toComplete)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
            foreach (var session in window2Sessions.Where(s => toComplete.All(t => t.Id != s.Id)))
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                    new { reason = "illness" })).EnsureSuccessStatusCode();
        }

        var activate2Response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, activate2Response.StatusCode);
        var activate2Body = await activate2Response.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", activate2Body);

        // Critical invariant, still proven even for this genuine Block: it
        // must never masquerade as a real window advancement (the exact
        // defect Phase 4M.4B.2A fixed).
        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var freshAggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(window2Start, freshAggregate.CurrentWindowStartWeek);
        Assert.Equal(window2End, freshAggregate.CurrentWindowEndWeek);
        Assert.Equal("CoreJitContextUnavailable", freshAggregate.CurrentBlockedInternalReasonCode);

        var activationRecords = await freshDb.LongHorizonActivationWindowRecords.AsNoTracking()
            .Where(a => a.PlanStateId == state.RollingId).ToListAsync();
        Assert.Equal(3, activationRecords.Count); // initial window0 + 2 real transitions (Blocks are not activation records)
        Assert.Equal(activationRecords.Count, activationRecords.Select(a => a.IdempotencyKey).Distinct().Count());
    }

    /// <summary>
    /// Phase 4M.4B.2C (§H item 2) -- the Reduce-only counterpart to
    /// <see cref="RealChain_ReduceLandingOnRunwayCoreBoundary_ThenMaintain_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement"/>,
    /// proving the Block is feasibility-based, not decision-enum-based:
    /// a Reduce decision (not Maintain) whose selected anchor is too small
    /// for the target Core/Runway week also Blocks, via the identical real
    /// `CoreJitContextUnavailable`/catalog-minimum-volume mechanism, with
    /// no window advancement.
    /// </summary>
    [Fact]
    public async Task RealReduceLandingOnRunwayCoreBoundary_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement()
    {
        var state = await ConfirmAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var window0Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).ToListAsync();
            foreach (var session in window0Sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
        await ActivateAsync(state.RollingId);

        // Window 1: sparse Reduce evidence (does not yet reach the Runway->Core
        // boundary -- confirmed in 4M.4B.2B this transition never invokes Core
        // generation at all).
        int window1End;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            window1End = aggregate.CurrentWindowEndWeek;
            var window1Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
            var firstLongRun = window1Sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{firstLongRun.Id}/complete",
                new { actual_distance_km = firstLongRun.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
            foreach (var session in window1Sessions.Where(s => s.Id != firstLongRun.Id))
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                    new { reason = "illness" })).EnsureSuccessStatusCode();
        }

        var activate1 = await ActivateAsync(state.RollingId);
        Assert.Equal("activated", activate1["outcome"]!.GetValue<string>());
        Assert.Equal("reduce", activate1["next_window_load_decision"]!.GetValue<string>());

        // Window 2: sparse Reduce evidence AGAIN, landing exactly on the
        // Runway->Core boundary -- LoadDecision stays Reduce (not Maintain),
        // and the selected anchor (min of this small current-window evidence
        // and Window 1's already-small carried anchor) is too small for the
        // real target Core week's catalog minimums.
        int window2Start, window2End;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            window2Start = aggregate.CurrentWindowStartWeek;
            window2End = aggregate.CurrentWindowEndWeek;
            Assert.True(window2Start > window1End, $"Window 2 [{window2Start}-{window2End}] did not advance past Window 1's end week {window1End}.");
            var window2Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();
            var firstLongRun = window2Sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));
            (await _client.PostRawAsync($"/api/v1/training-days/rolling/{firstLongRun.Id}/complete",
                new { actual_distance_km = firstLongRun.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
            foreach (var session in window2Sessions.Where(s => s.Id != firstLongRun.Id))
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                    new { reason = "illness" })).EnsureSuccessStatusCode();
        }

        var activate2Response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, activate2Response.StatusCode);
        var activate2Body = await activate2Response.Content.ReadAsStringAsync();
        Assert.Contains("LONG_HORIZON_CONTINUATION_BLOCKED", activate2Body);

        using var freshScope = _factory.Services.CreateScope();
        var freshDb = freshScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var freshAggregate = await freshDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
        Assert.Equal(window2Start, freshAggregate.CurrentWindowStartWeek);
        Assert.Equal(window2End, freshAggregate.CurrentWindowEndWeek);
        Assert.Equal("CoreJitContextUnavailable", freshAggregate.CurrentBlockedInternalReasonCode);

        var activationRecords = await freshDb.LongHorizonActivationWindowRecords.AsNoTracking()
            .Where(a => a.PlanStateId == state.RollingId).ToListAsync();
        // Real transitions that succeeded before the Block (initial window0
        // + window0->window1) -- the Block itself adds no activation record.
        // The exact count is a fact about this pilot's real roadmap/catalog
        // numerics, not asserted a priori; what matters is no double-commit.
        Assert.Equal(activationRecords.Count, activationRecords.Select(a => a.IdempotencyKey).Distinct().Count());
    }

    /// <summary>
    /// Phase 4M.5C §K -- the primary regression that motivated Revision 5,
    /// proven through the REAL activate-next-window HTTP endpoint (see
    /// WeeklyLoadDecisionAggregationTests.L8 for the equivalent proof
    /// against the pure aggregator directly). Every real structural week:
    /// both EASY_SUPPORT sessions completed, KEY_SESSION always NotToday.
    /// LONG_RUN is completed exactly once (week 1 only) rather than never --
    /// a real, pre-existing, unrelated JIT/Runway evidence-completeness
    /// requirement (confirmed separately by
    /// RealSorenessSubmission_BlocksViaRealJitRunwayEvidenceCompletenessRequirement
    /// in LongHorizonNextWindowDecisionActivationTests) needs at least one
    /// real completed Long Run anywhere in the window to compute a
    /// ValidatedSustainableLoad at all; zero completed Long Runs
    /// window-wide legitimately Blocks via JitValidatedLoadUnavailable
    /// regardless of this phase's change, so asserting the literal "0 Long"
    /// edge through the full HTTP/JIT pipeline is not a reachable production
    /// state (the pure-aggregator L8 test covers that exact edge instead).
    /// Window-wide completed count is still 9/16 (>= 4) -- the OLD
    /// direct-multi-week-summary bug would report ProgressAsPlanned; Rev5
    /// §7a's weekly-summary + B1 aggregation must report Maintain (week 1 is
    /// 3/4 with Key, not Easy, missing -> Maintain; weeks 2-4 are each 2/4
    /// -> Maintain; B1 worst-of-four -> Maintain).
    /// </summary>
    [Fact]
    public async Task RealActivation_EightEasyOneLongZeroKeyAcrossRealFourWeekWindow_ReportsMaintain_NotProgressAsPlanned()
    {
        var state = await ConfirmAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var window0Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId).ToListAsync();
            foreach (var session in window0Sessions)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
        }
        await ActivateAsync(state.RollingId); // window0 -> window1 (real 4-week window)

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aggregate = await db.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == state.RollingId);
            var window1Sessions = await db.LongHorizonRollingSessionStates.AsNoTracking().Include(s => s.Week)
                .Where(s => s.Week.PlanStateId == state.RollingId && s.Week.GlobalWeek >= aggregate.CurrentWindowStartWeek && s.Week.GlobalWeek <= aggregate.CurrentWindowEndWeek)
                .OrderBy(s => s.Week.GlobalWeek).ThenBy(s => s.SessionOrdinal).ToListAsync();

            // Every real structural week: both EASY_SUPPORT sessions
            // completed, KEY_SESSION always NotToday. LONG_RUN: completed
            // in the first real structural week only, NotToday in the rest.
            var firstWeekLongRun = window1Sessions.First(s => LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole));
            var toComplete = window1Sessions.Where(s => LongHorizonSessionRoleCodec.IsEasySupport(s.SessionRole)).Append(firstWeekLongRun).ToList();
            var toMiss = window1Sessions.Where(s => toComplete.All(t => t.Id != s.Id)).ToList();
            foreach (var session in toComplete)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/complete",
                    new { actual_distance_km = session.DistanceKm, actual_duration_minutes = 30 })).EnsureSuccessStatusCode();
            foreach (var session in toMiss)
                (await _client.PostRawAsync($"/api/v1/training-days/rolling/{session.Id}/not-today",
                    new { reason = "illness" })).EnsureSuccessStatusCode();
        }

        var activate1 = await ActivateAsync(state.RollingId);
        Assert.Equal("activated", activate1["outcome"]!.GetValue<string>());
        Assert.Equal("maintain", activate1["next_window_load_decision"]!.GetValue<string>());
    }

    private async Task<JsonNode> ActivateAsync(Guid rollingId)
    {
        var response = await _client.PostRawAsync("/api/v1/plans/active/long-horizon/activate-next-window", new { contract_version = 1 });
        if (!response.IsSuccessStatusCode)
        {
            using var diagScope = _factory.Services.CreateScope();
            var diagDb = diagScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var diagAggregate = await diagDb.LongHorizonRollingPlanStates.AsNoTracking().SingleAsync(p => p.Id == rollingId);
            var diagBody = await response.Content.ReadAsStringAsync();
            Assert.Fail(
                $"activate-next-window failed with {response.StatusCode}: {diagBody}\n" +
                $"CurrentBlockedInternalReasonCode={diagAggregate.CurrentBlockedInternalReasonCode}\n" +
                $"CurrentBlockedPublicReasonCategory={diagAggregate.CurrentBlockedPublicReasonCategory}\n" +
                $"CurrentWindow=[{diagAggregate.CurrentWindowStartWeek}-{diagAggregate.CurrentWindowEndWeek}]");
        }
        return await JsonAsync(response);
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
            target_finish_time_source = "product_average", race_name = "Phase 4M.4B.2B Confirmation",
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
