using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Phase 4F.9.2 — real-PostgreSQL relational validation for catalog
/// confirmation. Unlike <see cref="CatalogPlanConfirmationServiceTests"/>
/// (EF InMemory, provider-independent), every test here opens its own fresh
/// <see cref="AppDbContext"/> against the actual configured PostgreSQL
/// database (matching production's one-scoped-DbContext-per-request model),
/// so unique-index violations, check constraints, and real transaction
/// rollback are exercised for real rather than approximated.
/// </summary>
[Collection(RunningApp.IntegrationTests.ApiIntegrationTestCollection.Name)]
public sealed class CatalogConfirmationRelationalTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres";

    private static AppDbContext NewPostgresContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    private static CatalogPlanConfirmationService NewService(AppDbContext ctx) =>
        new(ctx, NullLogger<CatalogPlanConfirmationService>.Instance, new GeneratedCatalogPlanPayloadValidator());

    private static CatalogPreviewSnapshot BuildValidSnapshot(
        GeneratedCatalogPlanPayload? generatedPreviewPlanPayload,
        string candidateKey = "TEN_K__4D__INTERMEDIATE",
        int candidateVersion = 10,
        string candidateStatus = "DRAFT")
    {
        var input = new ResolverInputSnapshot
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10.0,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            RaceDate = new DateOnly(2026, 12, 1),
            CanonicalDistanceFamily = "TEN_K",
        };

        var refs = new Dictionary<string, PlanCatalogReference>
        {
            ["masterTemplate"] = new PlanCatalogReference("TEN_K_INTERMEDIATE_4D_MASTER", 3),
            ["layout"] = new PlanCatalogReference("FOUR_DAY_LAYOUT", 1),
            ["levelModifier"] = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 2),
            ["workoutProgression"] = new PlanCatalogReference("TEN_K_INTERMEDIATE_PROGRESSION", 2),
            ["progressionModifier"] = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER", 1),
            ["rulePack"] = new PlanCatalogReference("TEN_K_RULE_PACK", 1),
            ["peakVolumeBandPolicy"] = new PlanCatalogReference("PEAK_VOLUME_BAND_POLICY", 1),
            ["runtimeConditionValueRegistry"] = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
        };

        var candidate = new PlanCatalogCandidateSummary
        {
            CandidateKey = candidateKey,
            CandidateVersion = candidateVersion,
            CandidateStatus = candidateStatus,
            DependencyStatuses = new Dictionary<string, string>
            {
                ["masterTemplate"] = "DRAFT",
                ["layout"] = "DRAFT",
                ["levelModifier"] = "DRAFT",
                ["rulePack"] = "DRAFT",
            },
            CanonicalDistanceFamily = "TEN_K",
            Level = "INTERMEDIATE",
            DaysPerWeek = 4,
            MasterTemplate = refs["masterTemplate"],
            Layout = refs["layout"],
            LevelModifier = refs["levelModifier"],
            WorkoutProgression = refs["workoutProgression"],
            ProgressionModifier = refs["progressionModifier"],
            RulePack = refs["rulePack"],
            PeakVolumeBandPolicy = refs["peakVolumeBandPolicy"],
            RuntimeConditionValueRegistry = refs["runtimeConditionValueRegistry"],
            ReferencedWorkouts = new List<PlanCatalogReference>(),
            PhaseKeys = new List<string> { "FOUNDATION", "BUILD", "PEAK" },
            PhaseAllocations = new List<PlanCatalogPhaseAllocation>
            {
                new("FOUNDATION", 4), new("BUILD", 6), new("PEAK", 2),
            },
            SlotRoles = new List<string> { "EASY", "INTERVAL", "TEMPO", "LONG_RUN" },
            CoreCycle = new PlanCatalogCoreCycle(8, 12, 16),
        };

        var resolverResults = new List<RuntimeConditionResolutionResult>
        {
            RuntimeConditionResolutionResult.NotEvaluated("TIME_ADEQUACY_IN", "NO_RACE_DATE"),
            RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "NO_PACE_EVIDENCE"),
            RuntimeConditionResolutionResult.NotEvaluated("CORE_ENTRY_READINESS_IN", "NO_FITNESS_EVIDENCE"),
            RuntimeConditionResolutionResult.NotEvaluated("GOAL_FEASIBILITY_IN", "DEPENDENCY_NOT_EVALUATED"),
        };

        var trace = new ResolverDecisionTrace
        {
            Steps = resolverResults.Select((r, i) => ResolverDecisionTraceStep.FromResult(i, r.ConditionType + "_RESOLVER", r)).ToArray()
        };
        var now = DateTime.UtcNow;

        return CatalogPreviewSnapshotBuilder.Build(
            normalizedInput: input,
            asOfDate: DateOnly.FromDateTime(now),
            candidate: candidate,
            routeReason: "PILOT_TEN_K_INTERMEDIATE_4D_MATCH",
            resolverResults: resolverResults,
            decisionTrace: trace,
            createdAtUtc: now,
            expiresAtUtc: now.AddMinutes(30),
            generatedPreviewPlanPayload: generatedPreviewPlanPayload);
    }

    private static readonly JsonSerializerOptions SnapshotSerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    private static string SerializeSnapshot(CatalogPreviewSnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, SnapshotSerializeOptions);

    private static PlanPreview BuildPreviewRow(Guid userId, CatalogPreviewSnapshot snapshot, DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        InternalUserId = userId,
        TemplateId = snapshot.CandidateKey,
        RequestPayloadJson = "{}",
        PreviewPayloadJson = SerializeSnapshot(snapshot),
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(30),
        CreatedAt = DateTime.UtcNow,
    };

    // PlanPreviews.InternalUserId has an FK to Users, so every test needs a
    // real Users row (TrainingPlans.InternalUserId has no such FK, but we
    // create one consistently for both to keep helpers simple).
    private static async Task<Guid> NewIsolatedUserIdAsync()
    {
        var id = Guid.NewGuid();
        await using var ctx = NewPostgresContext();
        ctx.Users.Add(new User
        {
            Id = id,
            ExternalAuthProvider = "test",
            ExternalUserId = $"relational-test-{id}",
            Email = $"{id}@relational-test.local",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
        return id;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Section 11 — sequential idempotency (real PostgreSQL)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SequentialConfirmation_RealPostgres_SecondCallReturnsSamePlan_NoDuplicates()
    {
        var userId = await NewIsolatedUserIdAsync();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(payload);
        var previewId = Guid.NewGuid();

        await using (var seedCtx = NewPostgresContext())
        {
            var preview = BuildPreviewRow(userId, snapshot);
            seedCtx.PlanPreviews.Add(preview);
            await seedCtx.SaveChangesAsync();
            previewId = preview.Id;
        }

        Guid firstPlanId;
        await using (var ctx1 = NewPostgresContext())
        {
            var first = await NewService(ctx1).ConfirmAsync(userId, previewId);
            firstPlanId = first.PlanId;
        }

        await using (var ctx2 = NewPostgresContext())
        {
            var second = await NewService(ctx2).ConfirmAsync(userId, previewId);
            Assert.Equal(firstPlanId, second.PlanId);
        }

        await using var verifyCtx = NewPostgresContext();
        Assert.Equal(1, await verifyCtx.TrainingPlans.CountAsync(p => p.SourcePreviewId == previewId));
        Assert.Equal(payload.PlannedWeekCount, await verifyCtx.TrainingWeeks.CountAsync(w => w.PlanId == firstPlanId));
        Assert.Equal(payload.Weeks.Sum(w => w.Sessions.Count), await verifyCtx.TrainingDays.CountAsync(d => d.PlanId == firstPlanId));
        Assert.Equal(1, await verifyCtx.PlanEvents.CountAsync(e => e.PlanId == firstPlanId));

        var preview1 = await verifyCtx.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewId);
        Assert.Equal(firstPlanId, preview1.ConfirmedPlanId);
    }

    [Fact]
    public async Task SequentialConfirmation_RealPostgres_ReplayAfterExpiration_StillReturnsExistingPlan()
    {
        var userId = await NewIsolatedUserIdAsync();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(payload);
        Guid previewId;

        await using (var seedCtx = NewPostgresContext())
        {
            var preview = BuildPreviewRow(userId, snapshot);
            seedCtx.PlanPreviews.Add(preview);
            await seedCtx.SaveChangesAsync();
            previewId = preview.Id;
        }

        Guid firstPlanId;
        await using (var ctx1 = NewPostgresContext())
        {
            firstPlanId = (await NewService(ctx1).ConfirmAsync(userId, previewId)).PlanId;
        }

        // Simulate the preview expiring after it was already confirmed.
        await using (var expireCtx = NewPostgresContext())
        {
            var preview = await expireCtx.PlanPreviews.SingleAsync(p => p.Id == previewId);
            preview.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
            await expireCtx.SaveChangesAsync();
        }

        await using var replayCtx = NewPostgresContext();
        var replay = await NewService(replayCtx).ConfirmAsync(userId, previewId);
        Assert.Equal(firstPlanId, replay.PlanId);
    }

    [Fact]
    public async Task SequentialConfirmation_RealPostgres_WrongUserReplay_ThrowsForbidden()
    {
        var ownerId = await NewIsolatedUserIdAsync();
        var intruderId = await NewIsolatedUserIdAsync();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(payload);
        Guid previewId;

        await using (var seedCtx = NewPostgresContext())
        {
            var preview = BuildPreviewRow(ownerId, snapshot);
            seedCtx.PlanPreviews.Add(preview);
            await seedCtx.SaveChangesAsync();
            previewId = preview.Id;
        }

        await using (var ctx1 = NewPostgresContext())
        {
            await NewService(ctx1).ConfirmAsync(ownerId, previewId);
        }

        await using var ctx2 = NewPostgresContext();
        await Assert.ThrowsAsync<PlanPreviewForbiddenException>(
            () => NewService(ctx2).ConfirmAsync(intruderId, previewId));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Section 12 — same-preview concurrent confirmation (real PostgreSQL)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SamePreviewConcurrentConfirmation_RealPostgres_ExactlyOnePlanWinsRaceSafely()
    {
        var userId = await NewIsolatedUserIdAsync();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(payload);
        Guid previewId;

        await using (var seedCtx = NewPostgresContext())
        {
            var preview = BuildPreviewRow(userId, snapshot);
            seedCtx.PlanPreviews.Add(preview);
            await seedCtx.SaveChangesAsync();
            previewId = preview.Id;
        }

        // Synchronization barrier: both callers block until released together,
        // so both genuinely race for the same SourcePreviewId insert, not a
        // sequential ordering that merely looks concurrent.
        var barrier = new SemaphoreSlim(0, 2);
        async Task<(bool Success, Guid? PlanId, Exception? Error)> RaceAsync()
        {
            await using var ctx = NewPostgresContext();
            var svc = NewService(ctx);
            await barrier.WaitAsync();
            try
            {
                var result = await svc.ConfirmAsync(userId, previewId);
                return (true, result.PlanId, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex);
            }
        }

        var task1 = RaceAsync();
        var task2 = RaceAsync();
        barrier.Release(2);
        var results = await Task.WhenAll(task1, task2);

        // Required invariant: exactly one TrainingPlan, no partial rows, no
        // raw PostgreSQL exception exposed to either caller.
        await using (var verifyCtx = NewPostgresContext())
        {
            var plans = await verifyCtx.TrainingPlans.Where(p => p.SourcePreviewId == previewId).ToListAsync();
            Assert.Single(plans);
            var winningPlanId = plans[0].Id;

            Assert.Equal(payload.PlannedWeekCount, await verifyCtx.TrainingWeeks.CountAsync(w => w.PlanId == winningPlanId));
            Assert.Equal(payload.Weeks.Sum(w => w.Sessions.Count), await verifyCtx.TrainingDays.CountAsync(d => d.PlanId == winningPlanId));
            Assert.Equal(1, await verifyCtx.PlanEvents.CountAsync(e => e.PlanId == winningPlanId));

            var preview = await verifyCtx.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewId);
            Assert.Equal(winningPlanId, preview.ConfirmedPlanId);

            // Both callers must report success (current semantics: the losing
            // transaction rolls back, clears its tracker, reloads by
            // SourcePreviewId, and returns the SAME plan the winner created)
            // and must agree on the plan id — no raw exception leaked to
            // either caller.
            Assert.All(results, r => Assert.True(r.Success, r.Error?.ToString()));
            Assert.All(results, r => Assert.Equal(winningPlanId, r.PlanId));
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // Section 13 — different-preview active-plan concurrency (real PostgreSQL)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DifferentPreviewsActivePlanConcurrency_RealPostgres_AtMostOneActivePlanForUser()
    {
        var userId = await NewIsolatedUserIdAsync();
        var payloadA = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var payloadB = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan(startDate: new DateOnly(2027, 1, 4));
        var snapshotA = BuildValidSnapshot(payloadA);
        var snapshotB = BuildValidSnapshot(payloadB);
        Guid previewIdA, previewIdB;

        await using (var seedCtx = NewPostgresContext())
        {
            var previewA = BuildPreviewRow(userId, snapshotA);
            var previewB = BuildPreviewRow(userId, snapshotB);
            seedCtx.PlanPreviews.Add(previewA);
            seedCtx.PlanPreviews.Add(previewB);
            await seedCtx.SaveChangesAsync();
            previewIdA = previewA.Id;
            previewIdB = previewB.Id;
        }

        var barrier = new SemaphoreSlim(0, 2);
        async Task<(bool Success, Guid? PlanId, Type? ExceptionType)> RaceAsync(Guid previewId)
        {
            await using var ctx = NewPostgresContext();
            var svc = NewService(ctx);
            await barrier.WaitAsync();
            try
            {
                var result = await svc.ConfirmAsync(userId, previewId);
                return (true, result.PlanId, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.GetType());
            }
        }

        var taskA = RaceAsync(previewIdA);
        var taskB = RaceAsync(previewIdB);
        barrier.Release(2);
        var results = await Task.WhenAll(taskA, taskB);

        await using var verifyCtx = NewPostgresContext();
        var activePlans = await verifyCtx.TrainingPlans
            .Where(p => p.InternalUserId == userId && p.Status == TrainingPlanStatus.Active)
            .ToListAsync();
        Assert.Single(activePlans);
        var activePlanId = activePlans[0].Id;

        // Two legitimate outcomes exist depending on exact timing, and both
        // are safe: (a) the two calls genuinely collide inside the database,
        // and the loser gets a typed CatalogActivePlanConflictException; or
        // (b) the barrier releases both callers close together but not
        // perfectly simultaneously, so the second caller's fast pre-check
        // (step 12) observes the first caller's already-committed plan and
        // gracefully returns AlreadyActive=true for the SAME plan id — never
        // a distinct second plan, and never a raw exception either way.
        Assert.All(results, r => Assert.True(
            r.Success || r.ExceptionType == typeof(CatalogActivePlanConflictException),
            $"Expected success or CatalogActivePlanConflictException, got success={r.Success} exception={r.ExceptionType}"));
        Assert.All(results, r => Assert.True(!r.Success || r.PlanId == activePlanId));
        Assert.Contains(results, r => r.Success); // at least one caller must have actually created/observed the plan

        // Exactly one of the two previews is consumed; the loser remains unconfirmed.
        var finalPreviewA = await verifyCtx.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewIdA);
        var finalPreviewB = await verifyCtx.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewIdB);
        var confirmedCount = new[] { finalPreviewA.ConfirmedPlanId, finalPreviewB.ConfirmedPlanId }.Count(id => id.HasValue);
        Assert.Equal(1, confirmedCount);

        // Exactly one full week/day set exists for this user (no partial rows
        // from the losing transaction).
        var totalWeeksForUser = await verifyCtx.TrainingWeeks.CountAsync(w => w.Plan!.InternalUserId == userId);
        var totalDaysForUser = await verifyCtx.TrainingDays.CountAsync(d => d.Plan!.InternalUserId == userId);
        Assert.Equal(payloadA.PlannedWeekCount, totalWeeksForUser);
        Assert.Equal(payloadA.Weeks.Sum(w => w.Sessions.Count), totalDaysForUser);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Section 14 — atomicity: a forced failure mid-persistence leaves zero
    // orphaned rows, and a subsequent retry succeeds. Production persists
    // TrainingPlan+TrainingWeeks+TrainingDays+PlanEvent in exactly ONE
    // SaveChangesAsync call inside one transaction (see
    // CatalogPlanConfirmationService.ConfirmAsync) — there are no separate
    // per-entity commit points to inject failures between, so the
    // representative real-relational proof is: corrupt one row deep in the
    // batch (a TrainingDay violating a NOT NULL/format invariant the model
    // doesn't catch client-side) and confirm the ENTIRE batch (plan + weeks +
    // other days + event) rolls back, not just the corrupted row.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ForcedFailureDeepInPersistenceBatch_RealPostgres_RollsBackEverything_RetrySucceeds()
    {
        var userId = await NewIsolatedUserIdAsync();
        // A payload whose single session has an empty (invalid) WorkoutType
        // string is impossible to construct through the typed
        // GeneratedCatalogWorkoutType enum, so instead we force the failure by
        // giving the FIRST week a session with a date OUTSIDE the plan's
        // StartDate/EndDate range — CatalogPersistedPlanValidator (the
        // post-persist validator called inside the same try-block, before
        // commit) rejects this deterministically, causing the transaction to
        // roll back via the same code path a real constraint violation would.
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var corrupted = CorruptFirstSessionDate(payload);
        var snapshot = BuildValidSnapshot(corrupted);
        Guid previewId;

        await using (var seedCtx = NewPostgresContext())
        {
            var preview = BuildPreviewRow(userId, snapshot);
            seedCtx.PlanPreviews.Add(preview);
            await seedCtx.SaveChangesAsync();
            previewId = preview.Id;
        }

        await using (var ctx = NewPostgresContext())
        {
            await Assert.ThrowsAnyAsync<Exception>(() => NewService(ctx).ConfirmAsync(userId, previewId));
        }

        await using (var verifyCtx = NewPostgresContext())
        {
            Assert.Equal(0, await verifyCtx.TrainingPlans.CountAsync(p => p.SourcePreviewId == previewId));
            // No plan was created for this previewId, so by construction no
            // PlanEvent (always inserted with a real, just-created PlanId in
            // the same transaction) can reference it either. Scoped to this
            // test's own user rather than a global count, which the shared
            // dev database may hold rows for from other tests.
            var planIdsForUser = await verifyCtx.TrainingPlans
                .Where(p => p.InternalUserId == userId)
                .Select(p => p.Id)
                .ToListAsync();
            Assert.Equal(0, await verifyCtx.PlanEvents.CountAsync(e => planIdsForUser.Contains(e.PlanId)));
            var preview = await verifyCtx.PlanPreviews.AsNoTracking().SingleAsync(p => p.Id == previewId);
            Assert.Null(preview.ConfirmedPlanId);
        }

        // Retry with a corrected (valid) snapshot succeeds afterward, proving
        // the DbContext/database were not left poisoned by the failed attempt.
        var validSnapshot = BuildValidSnapshot(payload);
        await using (var fixCtx = NewPostgresContext())
        {
            var preview = await fixCtx.PlanPreviews.SingleAsync(p => p.Id == previewId);
            preview.PreviewPayloadJson = SerializeSnapshot(validSnapshot);
            await fixCtx.SaveChangesAsync();
        }

        await using var retryCtx = NewPostgresContext();
        var retryResult = await NewService(retryCtx).ConfirmAsync(userId, previewId);
        Assert.NotEqual(Guid.Empty, retryResult.PlanId);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Section 15 — fresh-context persisted-plan round-trip, incl. TAPER_SHARPEN
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PersistedPlan_RealPostgres_FreshContextRoundTrip_MatchesPayloadExactly()
    {
        var userId = await NewIsolatedUserIdAsync();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(payload);
        Guid previewId;

        await using (var seedCtx = NewPostgresContext())
        {
            var preview = BuildPreviewRow(userId, snapshot);
            seedCtx.PlanPreviews.Add(preview);
            await seedCtx.SaveChangesAsync();
            previewId = preview.Id;
        }

        Guid planId;
        await using (var ctx = NewPostgresContext())
        {
            planId = (await NewService(ctx).ConfirmAsync(userId, previewId)).PlanId;
        }

        await using var freshCtx = NewPostgresContext();
        var plan = await freshCtx.TrainingPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        Assert.Equal(GenerationSource.Catalog, plan.GenerationSource);
        Assert.Equal(previewId, plan.SourcePreviewId);
        Assert.Equal(snapshot.ContentHash, plan.CatalogPreviewContentHash);
        Assert.Equal(snapshot.CandidateKey, plan.CatalogCandidateKey);
        Assert.Equal(snapshot.CandidateVersion, plan.CatalogCandidateVersion);
        Assert.Equal(TrainingPlanStatus.Active, plan.Status);
        Assert.NotNull(plan.CatalogConfirmedAtUtc);

        var weeks = await freshCtx.TrainingWeeks.AsNoTracking().Where(w => w.PlanId == planId).OrderBy(w => w.WeekNumber).ToListAsync();
        Assert.Equal(payload.PlannedWeekCount, weeks.Count);
        Assert.Equal(Enumerable.Range(1, payload.PlannedWeekCount), weeks.Select(w => w.WeekNumber));

        var days = await freshCtx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).ToListAsync();
        Assert.Equal(payload.Weeks.Sum(w => w.Sessions.Count), days.Count);
        Assert.All(days, d => Assert.Equal(GenerationSource.Catalog, d.GenerationSource));
        Assert.All(days, d => Assert.Null(d.CatalogStageKey)); // LEGACY_READ_ONLY_NO_NEW_WRITES (Phase 4F.9.1A)
        Assert.All(days, d => Assert.Null(d.ActualDistanceKm));
        Assert.All(days, d => Assert.Null(d.CompletedAt));
        Assert.All(days, d => Assert.Equal(TrainingDayStatus.Planned, d.Status));
    }

    [Fact]
    public async Task TaperSharpen_RealPostgres_FreshContextRoundTrip_PreservesIdentityAndOrderedComponents()
    {
        var userId = await NewIsolatedUserIdAsync();
        var payload = BuildTaperSharpenPayload();
        var snapshot = BuildValidSnapshot(payload);
        Guid previewId;

        await using (var seedCtx = NewPostgresContext())
        {
            var preview = BuildPreviewRow(userId, snapshot);
            seedCtx.PlanPreviews.Add(preview);
            await seedCtx.SaveChangesAsync();
            previewId = preview.Id;
        }

        Guid planId;
        await using (var ctx = NewPostgresContext())
        {
            planId = (await NewService(ctx).ConfirmAsync(userId, previewId)).PlanId;
        }

        await using var freshCtx = NewPostgresContext();
        var taperSharpen = await freshCtx.TrainingDays.AsNoTracking()
            .SingleAsync(d => d.PlanId == planId && d.CatalogProgressionStageKey == "TAPER_SHARPEN");

        Assert.Equal("TAPER", taperSharpen.CatalogPhaseKey);
        Assert.Equal("KEY_SESSION", taperSharpen.CatalogStructuralRole);
        Assert.Equal("EASY_STANDARD", taperSharpen.CatalogWorkoutDefinitionKey);
        Assert.Null(taperSharpen.CatalogStageKey);

        var prescriptionJson = taperSharpen.CatalogPrescriptionJson!;
        Assert.Contains("\"catalog_session_prescription_snapshot\"", prescriptionJson, StringComparison.OrdinalIgnoreCase);
        var easyBaselineIdx = prescriptionJson.IndexOf("easy_baseline", StringComparison.OrdinalIgnoreCase);
        var sharpeningIdx = prescriptionJson.IndexOf("controlled_sharpening", StringComparison.OrdinalIgnoreCase);
        var recoveryIdx = prescriptionJson.IndexOf("easy_recovery", StringComparison.OrdinalIgnoreCase);
        Assert.True(easyBaselineIdx >= 0 && sharpeningIdx > easyBaselineIdx && recoveryIdx > sharpeningIdx,
            "TAPER_SHARPEN components must be ordered EASY_BASELINE, CONTROLLED_SHARPENING, EASY_RECOVERY.");
    }

    /// <summary>
    /// Clones a valid payload but shifts the first session's date 5 years
    /// into the past (outside the plan's Start/EndDate range), which
    /// <see cref="CatalogPersistedPlanValidator"/> deterministically rejects
    /// post-persist, forcing the whole confirm transaction to fail and roll
    /// back. All types here are plain sealed classes (not records), so this
    /// clones field-by-field rather than using a `with` expression.
    /// </summary>
    private static GeneratedCatalogPlanPayload CorruptFirstSessionDate(GeneratedCatalogPlanPayload payload)
    {
        var firstWeek = payload.Weeks[0];
        var firstSession = firstWeek.Sessions[0];

        var corruptedFirstSession = new GeneratedCatalogTrainingDayPayload
        {
            Date = firstSession.Date.AddYears(-5),
            SessionOrderInWeek = firstSession.SessionOrderInWeek,
            WorkoutType = firstSession.WorkoutType,
            PrescriptionBasis = firstSession.PrescriptionBasis,
            TargetDistanceKm = firstSession.TargetDistanceKm,
            TargetDurationMinutes = firstSession.TargetDurationMinutes,
            EstimatedDistanceKm = firstSession.EstimatedDistanceKm,
            EstimatedDurationMinutes = firstSession.EstimatedDurationMinutes,
            PlannedIntensity = firstSession.PlannedIntensity,
            PacePrescription = firstSession.PacePrescription,
            Segments = firstSession.Segments,
            Provenance = firstSession.Provenance,
        };

        var correctedSessions = new List<GeneratedCatalogTrainingDayPayload> { corruptedFirstSession };
        correctedSessions.AddRange(firstWeek.Sessions.Skip(1));

        var corruptedFirstWeek = new GeneratedCatalogWeekPayload
        {
            WeekNumber = firstWeek.WeekNumber,
            StartDate = firstWeek.StartDate,
            EndDate = firstWeek.EndDate,
            StageKey = firstWeek.StageKey,
            PlannedVolumeKm = firstWeek.PlannedVolumeKm,
            Sessions = correctedSessions,
            Provenance = firstWeek.Provenance,
        };

        var correctedWeeks = new List<GeneratedCatalogWeekPayload> { corruptedFirstWeek };
        correctedWeeks.AddRange(payload.Weeks.Skip(1));

        return new GeneratedCatalogPlanPayload
        {
            SchemaVersion = payload.SchemaVersion,
            StartDate = payload.StartDate,
            EndDate = payload.EndDate,
            PlannedWeekCount = payload.PlannedWeekCount,
            DaysPerWeek = payload.DaysPerWeek,
            CanonicalDistanceFamily = payload.CanonicalDistanceFamily,
            GoalType = payload.GoalType,
            CandidateKey = payload.CandidateKey,
            CandidateVersion = payload.CandidateVersion,
            DependencyVersions = payload.DependencyVersions,
            Weeks = correctedWeeks,
            Provenance = payload.Provenance,
        };
    }

    private static GeneratedCatalogPlanPayload BuildTaperSharpenPayload()
    {
        var start = new DateOnly(2026, 8, 3);
        var sessions = new List<GeneratedCatalogTrainingDayPayload>
        {
            BuildTaperSession(start, 0, 1, "TAPER_SHARPEN", "KEY_SESSION", 5.0, true),
            BuildTaperSession(start, 2, 2, "TAPER", "EASY_SUPPORT", 4.0, false),
            BuildTaperSession(start, 4, 3, "TAPER", "EASY_SUPPORT", 3.0, false),
            BuildTaperSession(start, 6, 4, "TAPER", "LONG_RUN", 6.0, false),
        };
        var dependencyVersions = new Dictionary<string, PlanCatalogReference>
        {
            ["masterTemplate"] = new("TEN_K_MASTER", 6),
            ["layout"] = new("RUN_LAYOUT_4D", 2),
            ["levelModifier"] = new("INTERMEDIATE_MODIFIER", 6),
            ["rulePack"] = new("APPSEL_RACE_PLAN_V1", 4),
        };

        return new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion,
            StartDate = start,
            EndDate = start.AddDays(6),
            PlannedWeekCount = 1,
            DaysPerWeek = 4,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            DependencyVersions = dependencyVersions,
            Weeks = new[]
            {
                new GeneratedCatalogWeekPayload
                {
                    WeekNumber = 1,
                    StartDate = start,
                    EndDate = start.AddDays(6),
                    StageKey = "TAPER",
                    PlannedVolumeKm = sessions.Sum(s => s.TargetDistanceKm ?? s.EstimatedDistanceKm ?? 0),
                    Sessions = sessions,
                    Provenance = new GeneratedCatalogWeekProvenance
                    {
                        StageKey = "TAPER",
                        SourcePhaseKey = "TAPER",
                        VolumeRuleKey = "TAPER_STANDARD",
                        ProgressionReferenceKey = "TEN_K_INTERMEDIATE_PROGRESSION_V1",
                    },
                },
            },
            Provenance = new GeneratedCatalogPlanProvenance
            {
                CandidateKey = "TEN_K__4D__INTERMEDIATE",
                CandidateVersion = 10,
                DependencyVersions = dependencyVersions,
                GenerationSource = "CATALOG",
                AsOfDate = start,
                MaterializerVersion = "TEST_TAPER_SHARPEN",
            },
        };
    }

    private static GeneratedCatalogTrainingDayPayload BuildTaperSession(
        DateOnly weekStart,
        int dayOffset,
        int order,
        string stageKey,
        string role,
        double distanceKm,
        bool taperSharpen) => new()
    {
        Date = weekStart.AddDays(dayOffset),
        SessionOrderInWeek = order,
        WorkoutType = role == "LONG_RUN" ? GeneratedCatalogWorkoutType.LongRun : GeneratedCatalogWorkoutType.Easy,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = distanceKm,
        EstimatedDurationMinutes = (int)(distanceKm * 6),
        PlannedIntensity = "z2",
        PacePrescription = new GeneratedCatalogPacePrescription
        {
            PaceType = GeneratedCatalogPaceType.EffortOnly,
            EffortLabel = taperSharpen ? "easy with controlled sharpening" : "conversational",
        },
        Segments = taperSharpen
            ? new[]
            {
                BuildSegment(1, "EASY_BASELINE", 2.0),
                BuildSegment(2, "CONTROLLED_SHARPENING", 1.0),
                BuildSegment(3, "EASY_RECOVERY", 2.0),
            }
            : Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
        Provenance = new GeneratedCatalogDayProvenance
        {
            SourceStageKey = stageKey,
            SourceWorkoutKey = "EASY_STANDARD",
            SourceWorkoutVersion = 4,
            SourceProgressionStepKey = stageKey,
            SourceLayoutSlotRole = role,
        },
    };

    private static GeneratedCatalogWorkoutSegmentPayload BuildSegment(int order, string displayText, double distanceKm) => new()
    {
        SegmentOrder = order,
        SegmentType = order == 2 ? GeneratedCatalogSegmentType.Steady : order == 1 ? GeneratedCatalogSegmentType.WarmUp : GeneratedCatalogSegmentType.CoolDown,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = distanceKm,
        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = displayText },
        Intensity = displayText,
        DisplayText = displayText,
    };
}
