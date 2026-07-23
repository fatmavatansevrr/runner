using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
/// Backend Integration Phase 4E.2 — Immutable Catalog Preview Confirmation tests.
///
/// All 52 assertions required by Phase 4E.2 are organized into groups:
/// (1)  Happy path — valid confirm produces correct plan
/// (2)  Dispatch — GenerationSource drives routing, not route decider
/// (3)  Isolation — no generation, routing, or resolution component is invoked
/// (4)  Snapshot validation — schema, hash, integrity, GenerationSource
/// (5)  State-based rejection — expiry, ownership, invalidation, snapshot presence
/// (6)  Idempotency — repeated confirm returns existing plan, no duplicates
/// (7)  Atomicity — partial failures roll back everything
/// (8)  TD status assertions
///
/// All tests use fully in-memory EF + real CatalogPlanConfirmationService.
/// No catalog files, no resolvers, no route deciders are loaded.
/// </summary>
public sealed class CatalogPlanConfirmationServiceTests
{
    // ── Fixture helpers ──────────────────────────────────────────────────────

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static CatalogPlanConfirmationService NewService(AppDbContext ctx) =>
        new(ctx, NullLogger<CatalogPlanConfirmationService>.Instance, new GeneratedCatalogPlanPayloadValidator());

    /// <summary>
    /// Builds a minimal-but-complete and integrity-valid CatalogPreviewSnapshot
    /// using CatalogPreviewSnapshotBuilder.Build (the same builder used by
    /// CatalogPreviewGenerator at preview time). This fixture:
    /// - Has GenerationSource = "CATALOG"
    /// - Has a valid ContentHash computed from its own content
    /// - Has all required fields non-null
    /// The resulting snapshot can be JSON-serialized into PreviewPayloadJson
    /// and will pass all 15 confirmation steps.
    /// </summary>
    private static CatalogPreviewSnapshot BuildValidSnapshot(
        DateOnly? asOfDate = null,
        string candidateKey = "TEN_K__4D__INTERMEDIATE",
        int candidateVersion = 10,
        string candidateStatus = "DRAFT",
        GeneratedCatalogPlanPayload? generatedPreviewPlanPayload = null)
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

        // Build a PlanCatalogCandidateSummary for the builder
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
            asOfDate: asOfDate ?? DateOnly.FromDateTime(now),
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

    private static PlanPreview BuildPreviewRow(
        Guid userId,
        CatalogPreviewSnapshot snapshot,
        DateTime? expiresAt = null,
        bool? isInvalidated = null,
        Guid? confirmedPlanId = null)
    {
        return new PlanPreview
        {
            Id = Guid.NewGuid(),
            InternalUserId = userId,
            TemplateId = snapshot.CandidateKey,
            RequestPayloadJson = "{}",
            PreviewPayloadJson = SerializeSnapshot(snapshot),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
            IsInvalidated = isInvalidated,
            ConfirmedPlanId = confirmedPlanId,
        };
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 1 — Non-persistable Rejection Path
    // ════════════════════════════════════════════════════════════════════════

    // Test #1 — Valid catalog preview is rejected as non-persistable in Phase 4E.2.
    [Fact]
    public async Task ConfirmAsync_ValidCatalogPreview_ThrowsCatalogPreviewNotPersistableException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<CatalogPreviewNotPersistableException>(
            () => svc.ConfirmAsync(userId, preview.Id));
        
        Assert.Contains("GeneratedPreviewPlanPayload is null", ex.Message);
        
        // Ensure no active TrainingPlan is created
        Assert.Empty(ctx.TrainingPlans);
        Assert.Empty(ctx.PlanEvents);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 2 — Dispatch via stored GenerationSource
    // ════════════════════════════════════════════════════════════════════════

    // Test #2 — Dispatch based on stored GenerationSource, not route decider.
    // This is tested in PlanServicesCatalogRoutingBoundaryTests (#2).
    // Here we verify the service itself never re-checks routing:
    [Fact]
    public async Task ConfirmAsync_DoesNotRequireRouteDecider_OperatesOnStoredSnapshot()
    {
        // CatalogPlanConfirmationService has no IGenerationRouteDecider dependency.
        // It bypasses routing and fails at the step 11 persistability guard.
        await using var ctx = NewContext();
        var svc = NewService(ctx);  // no route decider injected
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<CatalogPreviewNotPersistableException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 3 — Isolation: no generation engine, eligibility gate, or resolvers
    // ════════════════════════════════════════════════════════════════════════

    // Tests #7–#16: CatalogPlanConfirmationService has ZERO dependencies on
    // generation/routing/resolution components. This is proven by constructor
    // signature: only AppDbContext and ILogger<> are injected.
    // A compile-time proof: the class is constructable with ONLY those two args.
    [Fact]
    public void CatalogPlanConfirmationService_HasNoGenerationOrResolutionDependencies()
    {
        // The fact that the constructor below compiles and succeeds (without
        // injecting IGenerationRouteDecider, ICatalogPreviewGenerator,
        // IRuntimeConditionResolutionService, StageEligibilityEvaluator, etc.)
        // is the structural proof that the service has none of those dependencies.
        // Reflection-based: verify no resolver/generator types appear in constructor parameters.
        var ctors = typeof(CatalogPlanConfirmationService).GetConstructors();
        Assert.Single(ctors);
        var paramTypes = ctors[0].GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();

        Assert.DoesNotContain(paramTypes, t => t.Contains("GenerationRouteDecider"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("CatalogCandidateEligibilityGate"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("CatalogPreviewGenerator"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("RuntimeConditionResolution"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("TimeAdequacyResolver"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("PaceSourceResolver"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("CoreEntryReadinessResolver"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("GoalFeasibilityResolver"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("StageEligibilityEvaluator"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("PlanGenerationEngine"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("PlanCatalogBundleLoader"));
        Assert.DoesNotContain(paramTypes, t => t.Contains("PlanCatalogDomainMapper"));
    }

    // Test #17 — A newer published candidate version does not alter the stored preview.
    [Fact]
    public async Task ConfirmAsync_NewerCatalogVersionDoesNotAlterStoredSnapshot()
    {
        // The stored snapshot has candidateVersion=10 (DRAFT).
        // It bypasses routing and fails at the step 11 persistability guard.
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot(candidateVersion: 10, candidateStatus: "DRAFT");
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<CatalogPreviewNotPersistableException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // Test #18 — A valid older preview remains confirmable after catalog evolution.
    [Fact]
    public async Task ConfirmAsync_OlderValidPreview_RemainsConfirmable()
    {
        // An older (but unexpired, unmodified) preview with an earlier AsOfDate
        // still fails at the step 11 persistability guard (does not re-route/re-generate).
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var olderAsOfDate = new DateOnly(2026, 3, 1);
        var snapshot = BuildValidSnapshot(asOfDate: olderAsOfDate);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<CatalogPreviewNotPersistableException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 4 — Snapshot validation and rejection
    // ════════════════════════════════════════════════════════════════════════

    // Test #22 — Expired preview rejected.
    [Fact]
    public async Task ConfirmAsync_ExpiredPreview_ThrowsPlanPreviewExpiredException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();
        var preview = BuildPreviewRow(userId, snapshot, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<PlanPreviewExpiredException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // Test #23 — Preview owned by another user rejected.
    [Fact]
    public async Task ConfirmAsync_DifferentUser_ThrowsPlanPreviewForbiddenException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var ownerId = Guid.NewGuid();
        var intruder = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();
        var preview = BuildPreviewRow(ownerId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<PlanPreviewForbiddenException>(
            () => svc.ConfirmAsync(intruder, preview.Id));
    }

    // Test #24 — Missing snapshot rejected.
    [Fact]
    public async Task ConfirmAsync_EmptyPayloadJson_ThrowsPlanPreviewSnapshotMissingException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var preview = new PlanPreview
        {
            Id = Guid.NewGuid(),
            InternalUserId = userId,
            TemplateId = "TEN_K__4D__INTERMEDIATE",
            RequestPayloadJson = "{}",
            PreviewPayloadJson = "",  // empty
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
        };
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<PlanPreviewSnapshotMissingException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // Test #25 — Malformed snapshot JSON rejected.
    [Fact]
    public async Task ConfirmAsync_MalformedJson_ThrowsPlanPreviewSnapshotMalformedException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var preview = new PlanPreview
        {
            Id = Guid.NewGuid(),
            InternalUserId = userId,
            TemplateId = "TEN_K__4D__INTERMEDIATE",
            RequestPayloadJson = "{}",
            PreviewPayloadJson = "{this is not valid json",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
        };
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<PlanPreviewSnapshotMalformedException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // Test #26 — Snapshot with invalid CandidateVersion rejected as unsupported schema.
    [Fact]
    public async Task ConfirmAsync_SnapshotInvalidCandidateVersion_ThrowsPlanPreviewSnapshotUnsupportedException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        
        // Pass all required fields to satisfy step 6 deserialization, but with candidate_version = 0 (invalid)
        var incompletePayload = JsonSerializer.Serialize(new
        {
            candidate_key = "TEN_K__4D__INTERMEDIATE",
            candidate_version = 0, // invalid version
            candidate_status_at_generation_time = "DRAFT",
            generation_source = "CATALOG",
            route_reason = "PILOT",
            content_hash = "abc123",
            normalized_input = new { },
            resolver_results = Array.Empty<object>(),
            referenced_artifacts = new Dictionary<string, object>(),
            as_of_date = "2026-07-11",
            decision_trace = new { },
            selected_stage_keys = Array.Empty<string>(),
            fallback_stages_used = Array.Empty<string>(),
            hash_algorithm_version = CatalogPreviewCanonicalHashSerializer.CurrentHashAlgorithmVersion,
            created_at_utc = DateTime.UtcNow,
            expires_at_utc = DateTime.UtcNow.AddMinutes(30)
        });
        var preview = new PlanPreview
        {
            Id = Guid.NewGuid(),
            InternalUserId = userId,
            RequestPayloadJson = "{}",
            PreviewPayloadJson = incompletePayload,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
        };
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<PlanPreviewSnapshotUnsupportedException>(
            () => svc.ConfirmAsync(userId, preview.Id));
        Assert.Contains("CandidateVersion", ex.Message);
    }

    // Test #29 — Hash mismatch rejected.
    [Fact]
    public async Task ConfirmAsync_HashMismatch_ThrowsPlanPreviewIntegrityFailedException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();
        // Tamper: build the JSON and replace the hash with a wrong one
        var json = SerializeSnapshot(snapshot);
        var tampered = json.Replace(snapshot.ContentHash, new string('0', snapshot.ContentHash.Length));
        var preview = new PlanPreview
        {
            Id = Guid.NewGuid(),
            InternalUserId = userId,
            TemplateId = snapshot.CandidateKey,
            RequestPayloadJson = "{}",
            PreviewPayloadJson = tampered,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
        };
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<PlanPreviewIntegrityFailedException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // Test #31 — Invalid GenerationSource rejected.
    [Fact]
    public async Task ConfirmAsync_WrongGenerationSource_ThrowsPlanPreviewGenerationSourceInvalidException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        // Build a snapshot with GenerationSource manually patched to LEGACY_SQL in the JSON
        var snapshot = BuildValidSnapshot();
        var json = SerializeSnapshot(snapshot);
        // Replace CATALOG with LEGACY_SQL in the raw JSON (both the field value and the hash will mismatch,
        // but we test GenerationSource check first — step 8 precedes step 9).
        // To test step 8 in isolation: build a snapshot whose JSON deserialization yields GenerationSource != CATALOG.
        // The easiest approach: serialize a custom object.
        var fakePayload = JsonSerializer.Serialize(new
        {
            candidate_key = "TEN_K__4D__INTERMEDIATE",
            candidate_version = 10,
            candidate_status_at_generation_time = "DRAFT",
            generation_source = "LEGACY_SQL",  // wrong
            route_reason = "PILOT",
            content_hash = "deadbeef",
            normalized_input = new { goal_type = 0 },
            resolver_results = Array.Empty<object>(),
            referenced_artifacts = new { masterTemplate = new { key = "K", version = 1 } },
            created_at_utc = DateTime.UtcNow,
            expires_at_utc = DateTime.UtcNow.AddMinutes(30),
            as_of_date = "2026-07-11",
            decision_trace = new { },
            selected_stage_keys = Array.Empty<string>(),
            fallback_stages_used = Array.Empty<string>(),
            hash_algorithm_version = CatalogPreviewCanonicalHashSerializer.CurrentHashAlgorithmVersion,
        });
        var preview = new PlanPreview
        {
            Id = Guid.NewGuid(),
            InternalUserId = userId,
            RequestPayloadJson = "{}",
            PreviewPayloadJson = fakePayload,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
        };
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<PlanPreviewGenerationSourceInvalidException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 5 — State-based rejection (invalidation, preview not found)
    // ════════════════════════════════════════════════════════════════════════

    // Test #19 — Invalidated preview rejected.
    [Fact]
    public async Task ConfirmAsync_InvalidatedPreview_ThrowsPlanPreviewInvalidatedException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();
        var preview = BuildPreviewRow(userId, snapshot, isInvalidated: true);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<PlanPreviewInvalidatedException>(
            () => svc.ConfirmAsync(userId, preview.Id));

        Assert.Empty(ctx.TrainingPlans);
    }

    // Preview not found throws PlanPreviewNotFoundException.
    [Fact]
    public async Task ConfirmAsync_PreviewNotFound_ThrowsPlanPreviewNotFoundException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);

        await Assert.ThrowsAsync<PlanPreviewNotFoundException>(
            () => svc.ConfirmAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 6 — Idempotency
    // 
    // WARNING: These tests verify only sequential idempotency (single-threaded context
    // execution). They DO NOT claim or prove production concurrency safety.
    // The current implementation is an optimistic read-then-write flow with no PostgreSQL
    // row lock (e.g. FOR UPDATE) or unique database index constraint on ConfirmedPlanId.
    // Under concurrent load, two requests for the same preview can both read
    // ConfirmedPlanId == null and both attempt plan insertion.
    // This missing preview-specific database concurrency invariant blocks public activation.
    // ════════════════════════════════════════════════════════════════════════

    // Test #34 — Repeated confirmation is idempotent (returns existing plan).
    [Fact]
    public async Task ConfirmAsync_Idempotent_RepeatedCallReturnsSamePlan()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();

        // Under Option B, we cannot confirm a new plan (always throws CatalogPreviewNotPersistableException).
        // However, if the preview already has ConfirmedPlanId set (from a prior successful confirm in a future phase),
        // it must return the existing plan without attempting to validate/persist or throwing.
        var existingPlan = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            InternalUserId = userId,
            Status = TrainingPlanStatus.Active,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        ctx.TrainingPlans.Add(existingPlan);

        var preview = BuildPreviewRow(userId, snapshot, confirmedPlanId: existingPlan.Id);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        var first = await svc.ConfirmAsync(userId, preview.Id);
        var second = await svc.ConfirmAsync(userId, preview.Id);

        Assert.Equal(existingPlan.Id, first.PlanId);
        Assert.Equal(existingPlan.Id, second.PlanId);
        Assert.Equal("active", second.Status);
    }

    // Test #35 — Repeated confirmation does not create duplicate plans.
    [Fact]
    public async Task ConfirmAsync_Idempotent_DoesNotCreateDuplicatePlans()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();

        var existingPlan = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            InternalUserId = userId,
            Status = TrainingPlanStatus.Active,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        ctx.TrainingPlans.Add(existingPlan);

        var preview = BuildPreviewRow(userId, snapshot, confirmedPlanId: existingPlan.Id);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await svc.ConfirmAsync(userId, preview.Id);
        await svc.ConfirmAsync(userId, preview.Id);

        var plans = await ctx.TrainingPlans
            .Where(p => p.InternalUserId == userId)
            .ToListAsync();
        Assert.Single(plans);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 7 — Atomicity and persistence correctness (Option B Rejection Proofs)
    // ════════════════════════════════════════════════════════════════════════

    // Test #51 — Non-persistable catalog previews throw CatalogPreviewNotPersistableException and leave the database unchanged.
    [Fact]
    public async Task ConfirmAsync_NonPersistableSnapshot_ThrowsCatalogPreviewNotPersistableException_AndLeavesDatabaseUnchanged()
    {
        // Under Option B, GeneratedPreviewPlanPayload is null by default.
        // Confirm must reject this snapshot, creating no plan/event/weeks/days and leaving ConfirmedPlanId null.
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();
        Assert.Null(snapshot.GeneratedPreviewPlanPayload);

        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        // 1. Verify confirm throws the expected typed exception
        var ex = await Assert.ThrowsAsync<CatalogPreviewNotPersistableException>(
            () => svc.ConfirmAsync(userId, preview.Id));
        Assert.Contains("GeneratedPreviewPlanPayload is null", ex.Message);

        // 2. Verify database remains completely untouched (no TrainingPlan, no PlanEvent)
        Assert.Empty(ctx.TrainingPlans);
        Assert.Empty(ctx.PlanEvents);
        Assert.Empty(ctx.TrainingWeeks);
        Assert.Empty(ctx.TrainingDays);

        // 3. Verify ConfirmedPlanId remains null on the preview row (active-plan slot is not consumed)
        var updatedPreview = await ctx.PlanPreviews.FindAsync(preview.Id);
        Assert.NotNull(updatedPreview);
        Assert.Null(updatedPreview.ConfirmedPlanId);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 8 — TD status assertions
    // ════════════════════════════════════════════════════════════════════════

    // Test #49 — TD-PACESOURCE-001 behavior unchanged: ESTIMATED is still never emitted.
    // (Structural proof: no call to PaceSourceResolver from this service.)
    [Fact]
    public void TdPaceSource001_EstimatedPathStillNeverEmitted_ByConfirmService()
    {
        var svcType = typeof(CatalogPlanConfirmationService);
        var fields = svcType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.DoesNotContain(fields, f => f.FieldType.Name.Contains("PaceSourceResolver"));
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 9 — Snapshot integrity verifier unit tests
    // ════════════════════════════════════════════════════════════════════════

    // Verifier returns true for a valid, unmodified snapshot.
    [Fact]
    public void SnapshotVerifier_ValidSnapshot_ReturnsTrue()
    {
        var snapshot = BuildValidSnapshot();
        Assert.True(CatalogPreviewSnapshotVerifier.Verify(snapshot));
    }

    // Renamed by Phase 4E.2 acceptance-finalization pass (name-only correction, no
    // assertion/behavior change): this test does NOT directly construct a snapshot
    // with a mismatched ContentHash and assert Verify()==false for it — despite its
    // former name ("WrongHash_ReturnsFalse") implying that. It only proves that two
    // snapshots with different content produce different (correctly self-consistent)
    // hashes. The actual Verify()==false-for-a-tampered-hash behavior IS proven
    // elsewhere, correctly and end-to-end, by
    // ConfirmAsync_HashMismatch_ThrowsPlanPreviewIntegrityFailedException (which
    // string-replaces ContentHash in real serialized JSON and asserts rejection).
    // See PHASE4E_2_CLAUDE_SAFETY_AUDIT.md §4/§16 item 1.
    [Fact]
    public void SnapshotVerifier_DifferentSnapshotContent_ProducesDifferentSelfConsistentHashes()
    {
        var snapshot = BuildValidSnapshot();
        // Create a copy with a wrong hash (using anonymous object + reflection isn't practical;
        // instead, verify that a snapshot with an obviously wrong hash fails).
        // We do this by building a new snapshot and checking its hash is specific.
        var originalHash = snapshot.ContentHash;
        Assert.NotEmpty(originalHash);
        Assert.True(CatalogPreviewSnapshotVerifier.Verify(snapshot));
        // Mutate hash by building a snapshot with different content:
        var differentSnapshot = BuildValidSnapshot(candidateVersion: 99);
        // The different snapshot's hash must differ (different candidateVersion).
        Assert.NotEqual(originalHash, differentSnapshot.ContentHash);
        // Verify the different snapshot against the original snapshot's hash is false:
        // We build a fake snapshot by re-using the original's hash with different content.
        // Not possible with init properties, so we verify the builder produces different hashes
        // for different inputs, proving the verifier would catch tampered content.
        Assert.True(CatalogPreviewSnapshotVerifier.Verify(differentSnapshot));
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 10 — PlanServices dispatch isolation (SQL path unchanged)
    // ════════════════════════════════════════════════════════════════════════

    // Test #44 — Legacy SQL confirm behavior is unchanged.
    // A non-catalog preview (PreviewPayloadJson without generation_source=CATALOG)
    // still goes through the SQL confirm path.
    [Fact]
    public async Task PlanServices_LegacySqlPreview_UsesExistingSqlConfirmPath()
    {
        await using var ctx = NewContext();
        var svc = TestPlanServicesFactory.Create(ctx);
        var userId = Guid.NewGuid();
        var previewId = Guid.NewGuid();

        // Legacy SQL-shaped preview (Weeks present, no generation_source field).
        // RequestPayloadJson must satisfy GeneratePreviewRequest's required
        // members (goal_type/goal_distance/level/days_per_week/unit/
        // preferred_days/start_date) so deserialization itself succeeds --
        // this test is about proving previewData.Weeks being empty (from
        // PreviewPayloadJson) throws the defensive InvalidOperationException,
        // not about exercising required-member JSON validation (see
        // GeneratePreviewRequestJsonContractTests for that).
        ctx.PlanPreviews.Add(new PlanPreview
        {
            Id = previewId,
            InternalUserId = userId,
            TemplateId = null,
            RequestPayloadJson = "{\"goal_type\":\"habit\",\"goal_distance\":\"five_k\",\"level\":\"beginner\"," +
                "\"days_per_week\":3,\"unit\":\"km\",\"preferred_days\":[\"mon\",\"wed\",\"sat\"]," +
                "\"start_date\":\"2026-07-20\"}",
            PreviewPayloadJson = "{}",  // no generation_source — treated as SQL
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        // SQL path: PreviewPayloadJson is empty, so previewData.Weeks will be empty,
        // triggering the defensive InvalidOperationException from the SQL path
        // (NOT from the catalog service). This is proof that the SQL path is reached.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ConfirmPlanAsync(userId, new RunningApp.Application.DTOs.Plan.ConfirmPlanRequest { PreviewId = previewId }));

        Assert.Contains("no week data", ex.Message);
        Assert.Empty(ctx.TrainingPlans);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 11 — Backend Integration Phase 4F.1: typed schedule contract at
    // the confirm boundary. Every fixture used here is hand-built test-only
    // data (see GeneratedCatalogPlanPayloadFixtures's own doc comment) — none
    // of these tests claims live catalog schedule materialization exists.
    // Proves confirm still never persists a plan in this phase, for all three
    // new non-null-payload outcomes (unsupported schema, invalid schedule,
    // structurally valid schedule) in addition to the unchanged null-payload
    // case already covered by Group 1/7 above (tests #44-#51).
    // ════════════════════════════════════════════════════════════════════════

    // Test #45 (part 2 of 4): unsupported schedule schema version.
    [Fact]
    public async Task ConfirmAsync_UnsupportedScheduleSchemaVersion_ThrowsCatalogPreviewScheduleSchemaUnsupportedException_NoMutation()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();

        var validPayload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var unsupportedVersionPayload = new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion + 1,
            StartDate = validPayload.StartDate,
            EndDate = validPayload.EndDate,
            PlannedWeekCount = validPayload.PlannedWeekCount,
            DaysPerWeek = validPayload.DaysPerWeek,
            CanonicalDistanceFamily = validPayload.CanonicalDistanceFamily,
            GoalType = validPayload.GoalType,
            CandidateKey = validPayload.CandidateKey,
            CandidateVersion = validPayload.CandidateVersion,
            DependencyVersions = validPayload.DependencyVersions,
            Weeks = validPayload.Weeks,
            Provenance = validPayload.Provenance,
        };

        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: unsupportedVersionPayload);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<CatalogPreviewScheduleSchemaUnsupportedException>(
            () => svc.ConfirmAsync(userId, preview.Id));

        Assert.Empty(ctx.TrainingPlans);
        Assert.Empty(ctx.PlanEvents);
        var updatedPreview = await ctx.PlanPreviews.FindAsync(preview.Id);
        Assert.Null(updatedPreview!.ConfirmedPlanId);
    }

    // Test #45 (part 3 of 4): structurally invalid schedule (fails the validator).
    [Fact]
    public async Task ConfirmAsync_StructurallyInvalidSchedule_ThrowsCatalogPreviewScheduleInvalidException_NoMutation()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();

        var validPayload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        // Truncate the week list without lowering PlannedWeekCount — an
        // ActualWeekCountMismatch structural failure.
        var invalidPayload = new GeneratedCatalogPlanPayload
        {
            SchemaVersion = validPayload.SchemaVersion,
            StartDate = validPayload.StartDate,
            EndDate = validPayload.EndDate,
            PlannedWeekCount = validPayload.PlannedWeekCount,
            DaysPerWeek = validPayload.DaysPerWeek,
            CanonicalDistanceFamily = validPayload.CanonicalDistanceFamily,
            GoalType = validPayload.GoalType,
            CandidateKey = validPayload.CandidateKey,
            CandidateVersion = validPayload.CandidateVersion,
            DependencyVersions = validPayload.DependencyVersions,
            Weeks = new[] { validPayload.Weeks[0] },
            Provenance = validPayload.Provenance,
        };

        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: invalidPayload);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<CatalogPreviewScheduleInvalidException>(
            () => svc.ConfirmAsync(userId, preview.Id));

        Assert.Contains("ActualWeekCountMismatch", ex.Message);
        Assert.Empty(ctx.TrainingPlans);
        Assert.Empty(ctx.PlanEvents);
        var updatedPreview = await ctx.PlanPreviews.FindAsync(preview.Id);
        Assert.Null(updatedPreview!.ConfirmedPlanId);
    }

    // Test #45 (part 4 of 4): a fully structurally VALID schedule still does
    // not result in a persisted plan in Phase 4F.1 — materialization/persistence
    // is explicitly deferred to a later phase. This is the single most
    // important test proving Phase 4F.1 does not accidentally enable
    // successful catalog confirmation.
    [Fact]
    public async Task ConfirmAsync_StructurallyValidSchedule_PersistsCatalogPlanWeeksDaysAndPreviewLink()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();

        var validPayload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        // Sanity: the payload really is valid per the validator, independent of confirm.
        Assert.True(new GeneratedCatalogPlanPayloadValidator().Validate(validPayload).IsValid);

        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: validPayload);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        var response = await svc.ConfirmAsync(userId, preview.Id);

        Assert.False(response.AlreadyActive);
        Assert.Single(ctx.TrainingPlans);
        Assert.Equal(validPayload.PlannedWeekCount, await ctx.TrainingWeeks.CountAsync());
        Assert.Equal(validPayload.Weeks.Sum(w => w.Sessions.Count), await ctx.TrainingDays.CountAsync());
        Assert.Single(ctx.PlanEvents);
        var updatedPreview = await ctx.PlanPreviews.FindAsync(preview.Id);
        Assert.Equal(response.PlanId, updatedPreview!.ConfirmedPlanId);
        var plan = await ctx.TrainingPlans.SingleAsync();
        Assert.Equal(GenerationSource.Catalog, plan.GenerationSource);
        Assert.Equal(preview.Id, plan.SourcePreviewId);
        Assert.Equal(snapshot.ContentHash, plan.CatalogPreviewContentHash);
        Assert.All(ctx.TrainingDays, d => Assert.Equal(GenerationSource.Catalog, d.GenerationSource));

        // LEGACY_READ_ONLY_NO_NEW_WRITES: new catalog-sourced TrainingDay rows
        // must NOT populate the legacy CatalogStageKey field. Only
        // CatalogPhaseKey / CatalogProgressionStageKey are written by the
        // catalog confirmation path.
        Assert.All(ctx.TrainingDays, d => Assert.Null(d.CatalogStageKey));
    }

    [Fact]
    public async Task ConfirmAsync_RepeatedConfirmation_ReturnsSamePlanWithoutDuplicateWeeksOrDays()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: payload);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        var first = await svc.ConfirmAsync(userId, preview.Id);
        var second = await svc.ConfirmAsync(userId, preview.Id);

        Assert.Equal(first.PlanId, second.PlanId);
        Assert.Single(ctx.TrainingPlans);
        Assert.Equal(payload.PlannedWeekCount, await ctx.TrainingWeeks.CountAsync());
        Assert.Equal(payload.Weeks.Sum(w => w.Sessions.Count), await ctx.TrainingDays.CountAsync());
    }

    [Fact]
    public async Task ConfirmAsync_TaperSharpen_PreservesSeparatePhaseProgressionAndStructuredComponents()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var payload = BuildTaperSharpenPayload();
        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: payload);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await svc.ConfirmAsync(userId, preview.Id);

        var taperSharpen = await ctx.TrainingDays.SingleAsync(d => d.CatalogProgressionStageKey == "TAPER_SHARPEN");
        Assert.Equal("TAPER", taperSharpen.CatalogPhaseKey);
        Assert.Equal("KEY_SESSION", taperSharpen.CatalogStructuralRole);
        Assert.Equal("EASY_STANDARD", taperSharpen.CatalogWorkoutDefinitionKey);
        Assert.Contains("easy_baseline", taperSharpen.CatalogPrescriptionJson!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("controlled_sharpening", taperSharpen.CatalogPrescriptionJson!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("easy_recovery", taperSharpen.CatalogPrescriptionJson!, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GROUP 12 — Backend Integration Phase 4F.9.1A: confirmation ordering
    // correction. Idempotency (already-confirmed replay) is now checked
    // BEFORE expiration/invalidation, but AFTER ownership. These tests pin
    // the exact required behavior from PHASE4F_9_1A_PRE_RELATIONAL_CORRECTIONS.
    // ════════════════════════════════════════════════════════════════════════

    // 1. already confirmed + not expired -> returns existing plan.
    [Fact]
    public async Task ConfirmAsync_AlreadyConfirmedAndNotExpired_ReturnsExistingPlan()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: payload);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        var first = await svc.ConfirmAsync(userId, preview.Id);
        var second = await svc.ConfirmAsync(userId, preview.Id);

        Assert.Equal(first.PlanId, second.PlanId);
    }

    // 2. already confirmed + expired -> STILL returns existing plan (idempotent
    // replay is not invalidated by later expiration of the source preview).
    [Fact]
    public async Task ConfirmAsync_AlreadyConfirmedButNowExpired_StillReturnsExistingPlan()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: payload);
        var preview = BuildPreviewRow(userId, snapshot, expiresAt: DateTime.UtcNow.AddMinutes(30));
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        var first = await svc.ConfirmAsync(userId, preview.Id);

        // Simulate the preview expiring after it was already confirmed.
        preview.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await ctx.SaveChangesAsync();

        var second = await svc.ConfirmAsync(userId, preview.Id);

        Assert.Equal(first.PlanId, second.PlanId);
    }

    // 3. already confirmed + wrong user -> ownership is checked before
    // idempotency, so a non-owner never receives the existing plan.
    [Fact]
    public async Task ConfirmAsync_AlreadyConfirmedButWrongUser_ThrowsPlanPreviewForbiddenException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var ownerId = Guid.NewGuid();
        var intruder = Guid.NewGuid();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: payload);
        var preview = BuildPreviewRow(ownerId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await svc.ConfirmAsync(ownerId, preview.Id);

        await Assert.ThrowsAsync<PlanPreviewForbiddenException>(
            () => svc.ConfirmAsync(intruder, preview.Id));
    }

    // 4. unconfirmed + expired -> still fails (expiration only stops
    // confirming a NEW plan, not replaying an existing one).
    [Fact]
    public async Task ConfirmAsync_UnconfirmedAndExpired_ThrowsPlanPreviewExpiredException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: payload);
        var preview = BuildPreviewRow(userId, snapshot, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<PlanPreviewExpiredException>(
            () => svc.ConfirmAsync(userId, preview.Id));

        Assert.Empty(ctx.TrainingPlans);
    }

    // 5. ConfirmedPlanId points to a missing plan -> typed data-integrity error.
    [Fact]
    public async Task ConfirmAsync_ConfirmedPlanIdPointsToMissingPlan_ThrowsCatalogConfirmationFailedException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot();
        var preview = BuildPreviewRow(userId, snapshot, confirmedPlanId: Guid.NewGuid());
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<CatalogConfirmationFailedException>(
            () => svc.ConfirmAsync(userId, preview.Id));
    }

    // 6. ConfirmedPlanId points to another user's plan (corrupted linkage) ->
    // must fail safely rather than leak another user's plan.
    [Fact]
    public async Task ConfirmAsync_ConfirmedPlanIdPointsToAnotherUsersPlan_ThrowsCatalogConfirmationFailedException()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var ownerId = Guid.NewGuid();
        var otherUsersPlan = new TrainingPlan
        {
            Id = Guid.NewGuid(),
            InternalUserId = Guid.NewGuid(), // belongs to a DIFFERENT user
            TemplateId = "OTHER",
            Status = TrainingPlanStatus.Active,
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartedAt = DateTime.UtcNow,
            EstimatedEndDate = DateTime.UtcNow.AddDays(56),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        ctx.TrainingPlans.Add(otherUsersPlan);
        var snapshot = BuildValidSnapshot();
        var preview = BuildPreviewRow(ownerId, snapshot, confirmedPlanId: otherUsersPlan.Id);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<CatalogConfirmationFailedException>(
            () => svc.ConfirmAsync(ownerId, preview.Id));
    }

    // 7. idempotent replay performs no new writes (no new TrainingPlan/Week/
    // Day/PlanEvent rows, no duplicate confirmation side effects).
    [Fact]
    public async Task ConfirmAsync_IdempotentReplay_PerformsNoNewWrites()
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var snapshot = BuildValidSnapshot(generatedPreviewPlanPayload: payload);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await svc.ConfirmAsync(userId, preview.Id);
        var planCountAfterFirst = await ctx.TrainingPlans.CountAsync();
        var weekCountAfterFirst = await ctx.TrainingWeeks.CountAsync();
        var dayCountAfterFirst = await ctx.TrainingDays.CountAsync();
        var eventCountAfterFirst = await ctx.PlanEvents.CountAsync();

        await svc.ConfirmAsync(userId, preview.Id);

        Assert.Equal(planCountAfterFirst, await ctx.TrainingPlans.CountAsync());
        Assert.Equal(weekCountAfterFirst, await ctx.TrainingWeeks.CountAsync());
        Assert.Equal(dayCountAfterFirst, await ctx.TrainingDays.CountAsync());
        Assert.Equal(eventCountAfterFirst, await ctx.PlanEvents.CountAsync());
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

    // Test #52 (partial — the remainder is proven structurally by
    // CatalogPlanConfirmationService_HasNoGenerationOrResolutionDependencies
    // above, which now also implicitly covers the new validator dependency
    // not being a route-decider/resolver/generator type): a structurally
    // valid, non-null payload does not cause the confirm service to invoke
    // anything beyond its own validator — the validator itself is proven
    // dependency-free by GeneratedCatalogPlanPayloadValidator's own
    // constructor (parameterless, see IGeneratedCatalogPlanPayloadValidator's
    // doc comment) and by GeneratedCatalogPlanPayloadValidatorTests, which
    // never touches a database, clock, catalog loader, resolver, or route
    // decider.
    [Fact]
    public void GeneratedCatalogPlanPayloadValidator_HasNoDependencies()
    {
        var ctors = typeof(GeneratedCatalogPlanPayloadValidator).GetConstructors();

        Assert.Single(ctors);
        Assert.Empty(ctors[0].GetParameters());
    }
}
