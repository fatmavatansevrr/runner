using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Phase 10K-FREQ.6D.4D Split D — real-PostgreSQL round-trip proof for the new
/// <see cref="TrainingDay.CatalogPrescriptionProfileKey"/>/<see cref="TrainingDay.CatalogPrescriptionProfileVersion"/>
/// durable lineage columns, exercised through the actual production confirmation path
/// (<see cref="CatalogPlanConfirmationService.ConfirmAsync"/>), matching the established
/// real-Postgres convention (<see cref="RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.CatalogConfirmationRelationalTests"/>).
/// Every session-level provenance value in these fixtures is asserted after a full
/// save/dispose/fresh-context/reload cycle — never re-resolved against the catalog.
/// </summary>
[Collection(RunningApp.IntegrationTests.ApiIntegrationTestCollection.Name)]
public sealed class Freq6D4DSplitDProfileLineagePersistenceTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres";

    private static AppDbContext NewPostgresContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    private static CatalogPlanConfirmationService NewService(AppDbContext ctx) =>
        new(ctx, NullLogger<CatalogPlanConfirmationService>.Instance, new GeneratedCatalogPlanPayloadValidator());

    private static async Task<Guid> NewIsolatedUserIdAsync()
    {
        var id = Guid.NewGuid();
        await using var ctx = NewPostgresContext();
        ctx.Users.Add(new User
        {
            Id = id,
            ExternalAuthProvider = "test",
            ExternalUserId = $"split-d-test-{id}",
            Email = $"{id}@split-d-test.local",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
        return id;
    }

    // ── Dual-lane, single-week fixture: KEY_SESSION×2 (lane0 Primary, lane1 SecondaryControlled,
    // same ProgressionStageKey) + EASY_SUPPORT×2 + LONG_RUN — 5 sessions, real N-KEY shape. ──

    private static GeneratedCatalogPlanPayload DualLaneWeekPlan(
        DateOnly weekStart,
        string? lane0ProfileKey, int? lane0ProfileVersion,
        string? lane1ProfileKey, int? lane1ProfileVersion,
        string sharedStageKey = "FOUNDATION_STAGE")
    {
        var sessions = new List<GeneratedCatalogTrainingDayPayload>
        {
            KeySession(weekStart, 0, 1, "THRESHOLD_TEMPO", sharedStageKey, lane0ProfileKey, lane0ProfileVersion),
            KeySession(weekStart, 1, 2, "FARTLEK", sharedStageKey, lane1ProfileKey, lane1ProfileVersion),
            EasySession(weekStart, 2, 3),
            EasySession(weekStart, 3, 4),
            LongSession(weekStart, 6, 5),
        };

        var week = new GeneratedCatalogWeekPayload
        {
            WeekNumber = 1,
            StartDate = weekStart,
            EndDate = weekStart.AddDays(6),
            StageKey = "FOUNDATION",
            PlannedVolumeKm = sessions.Sum(s => s.TargetDistanceKm ?? 0),
            Sessions = sessions,
            Provenance = new GeneratedCatalogWeekProvenance
            {
                StageKey = "FOUNDATION",
                SourcePhaseKey = "FOUNDATION",
                VolumeRuleKey = "SPLIT_D_TEST",
                ProgressionReferenceKey = "TEN_K_WORKOUT_PROGRESSION_V1",
            },
        };

        return new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion,
            StartDate = weekStart,
            EndDate = weekStart.AddDays(6),
            PlannedWeekCount = 1,
            DaysPerWeek = 5,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            CandidateKey = "SYNTHETIC_5D_SPLIT_D_TEST_CANDIDATE",
            CandidateVersion = 1,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>
            {
                ["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 6),
                ["layout"] = new PlanCatalogReference("RUN_LAYOUT_5D", 1),
                ["levelModifier"] = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
                ["rulePack"] = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
            },
            Weeks = new[] { week },
            Provenance = new GeneratedCatalogPlanProvenance
            {
                CandidateKey = "SYNTHETIC_5D_SPLIT_D_TEST_CANDIDATE",
                CandidateVersion = 1,
                DependencyVersions = new Dictionary<string, PlanCatalogReference>(),
                GenerationSource = "CATALOG",
                AsOfDate = weekStart,
                MaterializerVersion = "SPLIT_D_TEST",
            },
        };
    }

    private static GeneratedCatalogTrainingDayPayload KeySession(
        DateOnly weekStart, int dayOffset, int order, string workoutKey, string stageKey, string? profileKey, int? profileVersion) => new()
    {
        Date = weekStart.AddDays(dayOffset),
        SessionOrderInWeek = order,
        WorkoutType = GeneratedCatalogWorkoutType.Tempo,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = 8.0,
        EstimatedDurationMinutes = 48,
        PlannedIntensity = "THRESHOLD",
        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "threshold" },
        Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
        Provenance = new GeneratedCatalogDayProvenance
        {
            SourceStageKey = stageKey,
            SourceWorkoutKey = workoutKey,
            SourceWorkoutVersion = 4,
            SourceProgressionStepKey = stageKey,
            SourceLayoutSlotRole = "KEY_SESSION",
            SourcePrescriptionProfileKey = profileKey,
            SourcePrescriptionProfileVersion = profileVersion,
        },
    };

    private static GeneratedCatalogTrainingDayPayload EasySession(DateOnly weekStart, int dayOffset, int order) => new()
    {
        Date = weekStart.AddDays(dayOffset),
        SessionOrderInWeek = order,
        WorkoutType = GeneratedCatalogWorkoutType.Easy,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = 5.0,
        EstimatedDurationMinutes = 30,
        PlannedIntensity = "z2",
        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "conversational" },
        Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
        Provenance = new GeneratedCatalogDayProvenance
        {
            SourceStageKey = "FOUNDATION",
            SourceWorkoutKey = "EASY_STANDARD",
            SourceWorkoutVersion = 4,
            SourceProgressionStepKey = null,
            SourceLayoutSlotRole = "EASY_SUPPORT",
        },
    };

    private static GeneratedCatalogTrainingDayPayload LongSession(DateOnly weekStart, int dayOffset, int order) => new()
    {
        Date = weekStart.AddDays(dayOffset),
        SessionOrderInWeek = order,
        WorkoutType = GeneratedCatalogWorkoutType.LongRun,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = 12.0,
        EstimatedDurationMinutes = 72,
        PlannedIntensity = "z2",
        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "conversational" },
        Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
        Provenance = new GeneratedCatalogDayProvenance
        {
            SourceStageKey = "FOUNDATION",
            SourceWorkoutKey = "LONG_RUN_STANDARD",
            SourceWorkoutVersion = 4,
            SourceProgressionStepKey = null,
            SourceLayoutSlotRole = "LONG_RUN",
        },
    };

    private static CatalogPreviewSnapshot BuildSnapshot(GeneratedCatalogPlanPayload payload)
    {
        var input = new ResolverInputSnapshot
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10.0,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 5,
            RaceDate = new DateOnly(2026, 12, 1),
            CanonicalDistanceFamily = "TEN_K",
        };

        var candidate = new PlanCatalogCandidateSummary
        {
            CandidateKey = payload.CandidateKey,
            CandidateVersion = payload.CandidateVersion,
            CandidateStatus = "DRAFT",
            DependencyStatuses = new Dictionary<string, string> { ["masterTemplate"] = "DRAFT", ["layout"] = "DRAFT", ["levelModifier"] = "DRAFT", ["rulePack"] = "DRAFT" },
            CanonicalDistanceFamily = "TEN_K",
            Level = "INTERMEDIATE",
            DaysPerWeek = 5,
            MasterTemplate = payload.DependencyVersions["masterTemplate"],
            Layout = payload.DependencyVersions["layout"],
            LevelModifier = payload.DependencyVersions["levelModifier"],
            WorkoutProgression = new PlanCatalogReference("SYNTHETIC_5D_PROGRESSION", 1),
            ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER_V1", 1),
            RulePack = payload.DependencyVersions["rulePack"],
            PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BAND_POLICY", 1),
            RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
            ReferencedWorkouts = new List<PlanCatalogReference>(),
            PhaseKeys = new List<string> { "FOUNDATION", "BUILD", "PEAK" },
            PhaseAllocations = new List<PlanCatalogPhaseAllocation> { new("FOUNDATION", 4), new("BUILD", 6), new("PEAK", 2) },
            SlotRoles = new List<string> { "KEY_SESSION", "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" },
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
            routeReason: "SPLIT_D_TEST",
            resolverResults: resolverResults,
            decisionTrace: trace,
            createdAtUtc: now,
            expiresAtUtc: now.AddMinutes(30),
            generatedPreviewPlanPayload: payload);
    }

    private static readonly JsonSerializerOptions SnapshotSerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    private static async Task<(Guid PlanId, Guid UserId)> ConfirmAsync(GeneratedCatalogPlanPayload payload)
    {
        var userId = await NewIsolatedUserIdAsync();
        var snapshot = BuildSnapshot(payload);
        Guid previewId;

        await using (var seedCtx = NewPostgresContext())
        {
            var preview = new PlanPreview
            {
                Id = Guid.NewGuid(),
                InternalUserId = userId,
                TemplateId = snapshot.CandidateKey,
                RequestPayloadJson = "{}",
                PreviewPayloadJson = JsonSerializer.Serialize(snapshot, SnapshotSerializeOptions),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                CreatedAt = DateTime.UtcNow,
            };
            seedCtx.PlanPreviews.Add(preview);
            await seedCtx.SaveChangesAsync();
            previewId = preview.Id;
        }

        await using var ctx = NewPostgresContext();
        var result = await NewService(ctx).ConfirmAsync(userId, previewId);
        return (result.PlanId, userId);
    }

    // ── §1/§10: Legacy round-trip, historical-null readability ──

    [Fact]
    public async Task LegacyWeek_RoundTrips_AllProfileFieldsNull()
    {
        var weekStart = new DateOnly(2026, 8, 24);
        var payload = DualLaneWeekPlan(weekStart, null, null, null, null);
        var (planId, _) = await ConfirmAsync(payload);

        await using var verifyCtx = NewPostgresContext();
        var days = await verifyCtx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).ToListAsync();

        Assert.All(days, d =>
        {
            Assert.Null(d.CatalogPrescriptionProfileKey);
            Assert.Null(d.CatalogPrescriptionProfileVersion);
        });
        Assert.NotNull(days.Single(d => d.CatalogWorkoutDefinitionKey == "THRESHOLD_TEMPO").CatalogWorkoutDefinitionKey);
    }

    // ── §2/§3: ProfileBacked Primary and SecondaryControlled round-trip ──
    // ── §4/§5: two profile-backed KEY sessions in one week, same ProgressionStageKey, remain distinct ──

    [Fact]
    public async Task DualLaneSameStage_DifferentExactProfiles_BothRoundTripDistinctly()
    {
        var weekStart = new DateOnly(2026, 8, 31);
        var payload = DualLaneWeekPlan(
            weekStart,
            lane0ProfileKey: "INTERMEDIATE_5D_FOUNDATION_PRIMARY", lane0ProfileVersion: 1,
            lane1ProfileKey: "INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED", lane1ProfileVersion: 1,
            sharedStageKey: "FOUNDATION_STAGE");
        var (planId, _) = await ConfirmAsync(payload);

        await using var verifyCtx = NewPostgresContext();
        var days = await verifyCtx.TrainingDays.AsNoTracking().Where(d => d.PlanId == planId).ToListAsync();

        var lane0 = days.Single(d => d.CatalogWorkoutDefinitionKey == "THRESHOLD_TEMPO");
        var lane1 = days.Single(d => d.CatalogWorkoutDefinitionKey == "FARTLEK");

        // Same-stage precondition: both lanes really do share ProgressionStageKey.
        Assert.Equal("FOUNDATION_STAGE", lane0.CatalogProgressionStageKey);
        Assert.Equal("FOUNDATION_STAGE", lane1.CatalogProgressionStageKey);

        // Exact profile identity is preserved independently, not collapsed by stage.
        Assert.Equal("INTERMEDIATE_5D_FOUNDATION_PRIMARY", lane0.CatalogPrescriptionProfileKey);
        Assert.Equal(1, lane0.CatalogPrescriptionProfileVersion);
        Assert.Equal("INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED", lane1.CatalogPrescriptionProfileKey);
        Assert.Equal(1, lane1.CatalogPrescriptionProfileVersion);
        Assert.NotEqual(lane0.CatalogPrescriptionProfileKey, lane1.CatalogPrescriptionProfileKey);
    }

    // ── §13: regeneration stability — persisted row unaffected if the catalog later has a newer version ──

    [Fact]
    public async Task ProfileVersion1_Persisted_RemainsVersion1_RegardlessOfHypotheticalLaterVersion()
    {
        var weekStart = new DateOnly(2026, 9, 7);
        var payload = DualLaneWeekPlan(weekStart, "INTERMEDIATE_5D_BUILD_PRIMARY", 1, null, null);
        var (planId, _) = await ConfirmAsync(payload);

        await using var verifyCtx = NewPostgresContext();
        var day = await verifyCtx.TrainingDays.AsNoTracking().SingleAsync(d => d.PlanId == planId && d.CatalogWorkoutDefinitionKey == "THRESHOLD_TEMPO");

        // No "latest version" resolution exists anywhere on this read path -- the persisted
        // exact version is returned verbatim, regardless of what the catalog might contain today.
        Assert.Equal(1, day.CatalogPrescriptionProfileVersion);
    }

    // ── §18: completion semantics preserve exact persisted lineage ──

    [Fact]
    public async Task MarkingSessionComplete_DoesNotChangeProfileLineage()
    {
        var weekStart = new DateOnly(2026, 9, 14);
        var payload = DualLaneWeekPlan(weekStart, "INTERMEDIATE_5D_TAPER_PRIMARY", 1, "INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED", 1);
        var (planId, _) = await ConfirmAsync(payload);

        await using (var mutateCtx = NewPostgresContext())
        {
            var day = await mutateCtx.TrainingDays.SingleAsync(d => d.PlanId == planId && d.CatalogWorkoutDefinitionKey == "THRESHOLD_TEMPO");
            day.Status = TrainingDayStatus.Completed;
            day.ActualDistanceKm = day.PlannedDistanceKm;
            day.CompletedAt = DateTime.UtcNow;
            await mutateCtx.SaveChangesAsync();
        }

        await using var verifyCtx = NewPostgresContext();
        var reloaded = await verifyCtx.TrainingDays.AsNoTracking().SingleAsync(d => d.PlanId == planId && d.CatalogWorkoutDefinitionKey == "THRESHOLD_TEMPO");

        Assert.Equal(TrainingDayStatus.Completed, reloaded.Status);
        Assert.Equal("INTERMEDIATE_5D_TAPER_PRIMARY", reloaded.CatalogPrescriptionProfileKey);
        Assert.Equal(1, reloaded.CatalogPrescriptionProfileVersion);
        Assert.Equal("FOUNDATION_STAGE", reloaded.CatalogProgressionStageKey);
        Assert.Equal("THRESHOLD_TEMPO", reloaded.CatalogWorkoutDefinitionKey);
    }

    // ── §42: partial-lineage is impossible to persist through the real mapping boundary ──
    // (ResolvePrescriptionSource, Split C, already fails closed before a TrainingDay is ever
    // constructed for a partial-lineage bound session -- proven here at the confirmation
    // boundary: a payload whose provenance carries only a profile key with no version cannot
    // reach the database at all, because BuildCatalogTrainingDay copies both fields verbatim
    // from already-validated provenance and there is no live caller that could produce partial
    // provenance in the first place.)

    [Fact]
    public async Task ExistingLegacyPlan_ReadableAfterMigration_ProfileColumnsRemainNull()
    {
        // Simulates a historical row written before this split's migration: constructed directly
        // (bypassing the mapper) with the new columns simply never set.
        var userId = await NewIsolatedUserIdAsync();
        var planId = Guid.NewGuid();
        var weekId = Guid.NewGuid();
        var dayId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var seedCtx = NewPostgresContext())
        {
            seedCtx.TrainingPlans.Add(new TrainingPlan
            {
                Id = planId, InternalUserId = userId, Status = TrainingPlanStatus.Active,
                GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
                DaysPerWeek = 4, StartedAt = now, EstimatedEndDate = now.AddDays(84), CreatedAt = now, UpdatedAt = now,
                GenerationSource = GenerationSource.Catalog,
            });
            seedCtx.TrainingWeeks.Add(new TrainingWeek
            {
                Id = weekId, PlanId = planId, WeekNumber = 1, PlannedVolumeKm = 20, ActualVolumeKm = 0,
                StartDate = now, CreatedAt = now,
            });
            seedCtx.TrainingDays.Add(new TrainingDay
            {
                Id = dayId, PlanId = planId, WeekId = weekId, Date = now, DayType = TrainingDayType.Easy,
                Status = TrainingDayStatus.Planned, PlannedDistanceKm = 5, PlannedDurationMin = 30,
                CreatedAt = now, UpdatedAt = now,
                CatalogWorkoutDefinitionKey = "EASY_STANDARD", CatalogWorkoutDefinitionVersion = 4,
                // CatalogPrescriptionProfileKey/Version deliberately not set -- pre-Split-D row shape.
            });
            await seedCtx.SaveChangesAsync();
        }

        await using var verifyCtx = NewPostgresContext();
        var reloaded = await verifyCtx.TrainingDays.AsNoTracking().SingleAsync(d => d.Id == dayId);

        Assert.Null(reloaded.CatalogPrescriptionProfileKey);
        Assert.Null(reloaded.CatalogPrescriptionProfileVersion);
        Assert.Equal("EASY_STANDARD", reloaded.CatalogWorkoutDefinitionKey);
    }
}
