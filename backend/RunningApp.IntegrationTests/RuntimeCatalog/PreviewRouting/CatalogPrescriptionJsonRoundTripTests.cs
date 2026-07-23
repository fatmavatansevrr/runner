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
/// Backend Integration Phase 4F.9 follow-up — proves the JSON round-trip
/// behavior of <c>TrainingDay.CatalogPrescriptionJson</c> for a representative
/// spread of session shapes.
///
/// Deliberately does NOT invent a separate test-only serializer: every JSON
/// string asserted on here is produced by the REAL production path —
/// <see cref="CatalogPlanConfirmationService.ConfirmAsync"/> →
/// (private) <c>BuildCatalogTrainingDay</c> → (private) <c>BuildPrescriptionSnapshot</c>
/// → <c>JsonSerializer.Serialize(..., PersistenceJsonOptions)</c> — by actually
/// confirming a catalog preview end-to-end against an EF InMemory
/// <see cref="AppDbContext"/> and reading back the persisted
/// <see cref="TrainingDay.CatalogPrescriptionJson"/> string. Those two private
/// members are not made internal/testable for this — going through
/// <c>ConfirmAsync</c> is strictly more production-realistic than reaching in.
/// </summary>
public sealed class CatalogPrescriptionJsonRoundTripTests
{
    // ── Fixture helpers (mirrors CatalogPlanConfirmationServiceTests' own private helpers) ──

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static CatalogPlanConfirmationService NewService(AppDbContext ctx) =>
        new(ctx, NullLogger<CatalogPlanConfirmationService>.Instance, new GeneratedCatalogPlanPayloadValidator());

    private static CatalogPreviewSnapshot BuildValidSnapshot(GeneratedCatalogPlanPayload payload)
    {
        var input = new ResolverInputSnapshot
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10.0,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = payload.DaysPerWeek,
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
            CandidateKey = payload.CandidateKey,
            CandidateVersion = payload.CandidateVersion,
            CandidateStatus = "DRAFT",
            DependencyStatuses = new Dictionary<string, string>
            {
                ["masterTemplate"] = "DRAFT",
                ["layout"] = "DRAFT",
                ["levelModifier"] = "DRAFT",
                ["rulePack"] = "DRAFT",
            },
            CanonicalDistanceFamily = "TEN_K",
            Level = "INTERMEDIATE",
            DaysPerWeek = payload.DaysPerWeek,
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
            asOfDate: payload.StartDate,
            candidate: candidate,
            routeReason: "PILOT_TEN_K_INTERMEDIATE_4D_MATCH",
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

    private static PlanPreview BuildPreviewRow(Guid userId, CatalogPreviewSnapshot snapshot) => new()
    {
        Id = Guid.NewGuid(),
        InternalUserId = userId,
        TemplateId = snapshot.CandidateKey,
        RequestPayloadJson = "{}",
        PreviewPayloadJson = JsonSerializer.Serialize(snapshot, SnapshotSerializeOptions),
        ExpiresAt = DateTime.UtcNow.AddMinutes(30),
        CreatedAt = DateTime.UtcNow,
    };

    /// <summary>Confirms <paramref name="payload"/> against a fresh InMemory context and returns the persisted TrainingDay rows, ordered by SessionOrderInWeek/date.</summary>
    private static async Task<List<TrainingDay>> ConfirmAndGetDaysAsync(GeneratedCatalogPlanPayload payload)
    {
        await using var ctx = NewContext();
        var svc = NewService(ctx);
        var userId = Guid.NewGuid();
        var snapshot = BuildValidSnapshot(payload);
        var preview = BuildPreviewRow(userId, snapshot);
        ctx.PlanPreviews.Add(preview);
        await ctx.SaveChangesAsync();

        await svc.ConfirmAsync(userId, preview.Id);

        return await ctx.TrainingDays.OrderBy(d => d.Date).ToListAsync();
    }

    private const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    private const int CandidateVersion = 10;

    private static Dictionary<string, PlanCatalogReference> DependencyVersions() => new()
    {
        ["masterTemplate"] = new("TEN_K_MASTER", 6),
        ["layout"] = new("RUN_LAYOUT_4D", 2),
        ["levelModifier"] = new("INTERMEDIATE_MODIFIER", 6),
        ["rulePack"] = new("APPSEL_RACE_PLAN_V1", 4),
    };

    /// <summary>
    /// A single-week, 4-session plan covering, in session order:
    /// 1. Effort-only EASY session with an unresolved/null duration estimate.
    /// 2. An exact GOAL_PACE (Target pace type) session.
    /// 3. A pace-RANGE (duration-basis) session.
    /// 4. A FARTLEK/THRESHOLD/GOAL_PACE ordered-segments session.
    /// </summary>
    private static GeneratedCatalogPlanPayload BuildMixedSessionPlan(DateOnly start)
    {
        var sessions = new List<GeneratedCatalogTrainingDayPayload>
        {
            // 1. Effort-only EASY, unresolved/null duration (distance basis,
            // no TargetDurationMinutes — that's implied by the basis — and no
            // EstimatedDurationMinutes either, which the validator explicitly
            // permits as null).
            new()
            {
                Date = start,
                SessionOrderInWeek = 1,
                WorkoutType = GeneratedCatalogWorkoutType.Easy,
                PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
                TargetDistanceKm = 6.0,
                TargetDurationMinutes = null,
                EstimatedDistanceKm = null,
                EstimatedDurationMinutes = null,
                PlannedIntensity = "z2",
                PacePrescription = new GeneratedCatalogPacePrescription
                {
                    PaceType = GeneratedCatalogPaceType.EffortOnly,
                    EffortLabel = "conversational",
                },
                Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
                Provenance = new GeneratedCatalogDayProvenance
                {
                    SourceStageKey = "BUILD",
                    SourceWorkoutKey = "EASY_STANDARD",
                    SourceWorkoutVersion = 4,
                    SourceProgressionStepKey = "BUILD_WEEK",
                    SourceLayoutSlotRole = "EASY_SUPPORT",
                },
            },
            // 2. Exact GOAL_PACE session (Target pace type).
            new()
            {
                Date = start.AddDays(2),
                SessionOrderInWeek = 2,
                WorkoutType = GeneratedCatalogWorkoutType.Tempo,
                PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
                TargetDistanceKm = 8.0,
                TargetDurationMinutes = null,
                EstimatedDistanceKm = null,
                EstimatedDurationMinutes = 45,
                PlannedIntensity = "GOAL_PACE",
                PacePrescription = new GeneratedCatalogPacePrescription
                {
                    PaceType = GeneratedCatalogPaceType.Target,
                    TargetSecondsPerKm = 285,
                    DisplayText = "4:45/km",
                },
                Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
                Provenance = new GeneratedCatalogDayProvenance
                {
                    SourceStageKey = "GOAL_PACE_REHEARSAL",
                    SourceWorkoutKey = "GOAL_PACE_TEN_K",
                    SourceWorkoutVersion = 2,
                    SourceProgressionStepKey = "BUILD_WEEK",
                    SourceLayoutSlotRole = "KEY_SESSION",
                },
            },
            // 3. Pace-range (duration basis) session.
            new()
            {
                Date = start.AddDays(4),
                SessionOrderInWeek = 3,
                WorkoutType = GeneratedCatalogWorkoutType.Tempo,
                PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Duration,
                TargetDistanceKm = null,
                TargetDurationMinutes = 40,
                EstimatedDistanceKm = 7.0,
                EstimatedDurationMinutes = null,
                PlannedIntensity = "z3",
                PacePrescription = new GeneratedCatalogPacePrescription
                {
                    PaceType = GeneratedCatalogPaceType.Range,
                    MinSecondsPerKm = 300,
                    MaxSecondsPerKm = 330,
                    DisplayText = "5:00-5:30/km",
                },
                Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
                Provenance = new GeneratedCatalogDayProvenance
                {
                    SourceStageKey = "BUILD",
                    SourceWorkoutKey = "THRESHOLD_TEMPO",
                    SourceWorkoutVersion = 4,
                    SourceProgressionStepKey = "BUILD_WEEK",
                    SourceLayoutSlotRole = "KEY_SESSION",
                },
            },
            // 4. FARTLEK/THRESHOLD/GOAL_PACE ordered segments.
            new()
            {
                Date = start.AddDays(6),
                SessionOrderInWeek = 4,
                WorkoutType = GeneratedCatalogWorkoutType.Interval,
                PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
                TargetDistanceKm = 9.0,
                TargetDurationMinutes = null,
                EstimatedDistanceKm = null,
                EstimatedDurationMinutes = 54,
                PlannedIntensity = "z4",
                PacePrescription = new GeneratedCatalogPacePrescription
                {
                    PaceType = GeneratedCatalogPaceType.Target,
                    TargetSecondsPerKm = 270,
                },
                Segments = new List<GeneratedCatalogWorkoutSegmentPayload>
                {
                    new()
                    {
                        SegmentOrder = 1,
                        SegmentType = GeneratedCatalogSegmentType.WarmUp,
                        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
                        TargetDistanceKm = 2.0,
                        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "easy" },
                        Intensity = "z2",
                        DisplayText = "FARTLEK_WARMUP",
                    },
                    new()
                    {
                        SegmentOrder = 2,
                        SegmentType = GeneratedCatalogSegmentType.WorkInterval,
                        RepetitionCount = 5,
                        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Duration,
                        TargetDurationSeconds = 180,
                        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.Target, TargetSecondsPerKm = 250 },
                        Intensity = "THRESHOLD",
                        DisplayText = "THRESHOLD_INTERVAL",
                    },
                    new()
                    {
                        SegmentOrder = 3,
                        SegmentType = GeneratedCatalogSegmentType.Steady,
                        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
                        TargetDistanceKm = 3.0,
                        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.Target, TargetSecondsPerKm = 270 },
                        Intensity = "GOAL_PACE",
                        DisplayText = "GOAL_PACE_STEADY",
                    },
                },
                Provenance = new GeneratedCatalogDayProvenance
                {
                    SourceStageKey = "BUILD",
                    SourceWorkoutKey = "FARTLEK_THRESHOLD_GOALPACE",
                    SourceWorkoutVersion = 1,
                    SourceProgressionStepKey = "BUILD_WEEK",
                    SourceLayoutSlotRole = "KEY_SESSION",
                },
            },
        };

        var deps = DependencyVersions();
        return new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion,
            StartDate = start,
            EndDate = start.AddDays(6),
            PlannedWeekCount = 1,
            DaysPerWeek = 4,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            CandidateKey = CandidateKey,
            CandidateVersion = CandidateVersion,
            DependencyVersions = deps,
            Weeks = new[]
            {
                new GeneratedCatalogWeekPayload
                {
                    WeekNumber = 1,
                    StartDate = start,
                    EndDate = start.AddDays(6),
                    StageKey = "BUILD",
                    PlannedVolumeKm = sessions.Sum(s => s.TargetDistanceKm ?? s.EstimatedDistanceKm ?? 0),
                    Sessions = sessions,
                    Provenance = new GeneratedCatalogWeekProvenance
                    {
                        StageKey = "BUILD",
                        SourcePhaseKey = "BUILD",
                        VolumeRuleKey = "BUILD_STANDARD",
                        ProgressionReferenceKey = "TEN_K_INTERMEDIATE_PROGRESSION_V1",
                    },
                },
            },
            Provenance = new GeneratedCatalogPlanProvenance
            {
                CandidateKey = CandidateKey,
                CandidateVersion = CandidateVersion,
                DependencyVersions = deps,
                GenerationSource = "CATALOG",
                AsOfDate = start,
                MaterializerVersion = "TEST_JSON_ROUNDTRIP",
            },
        };
    }

    /// <summary>Same shape as CatalogPlanConfirmationServiceTests' own TAPER_SHARPEN fixture: a TAPER-phase week whose key session carries the fine-grained TAPER_SHARPEN progression stage.</summary>
    private static GeneratedCatalogPlanPayload BuildTaperSharpenPlan(DateOnly start)
    {
        GeneratedCatalogTrainingDayPayload BuildTaperSession(int dayOffset, int order, string stageKey, string role, double distanceKm, bool taperSharpen) => new()
        {
            Date = start.AddDays(dayOffset),
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
                    new GeneratedCatalogWorkoutSegmentPayload
                    {
                        SegmentOrder = 1, SegmentType = GeneratedCatalogSegmentType.WarmUp,
                        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance, TargetDistanceKm = 2.0,
                        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "easy_baseline" },
                        Intensity = "easy_baseline", DisplayText = "easy_baseline",
                    },
                    new GeneratedCatalogWorkoutSegmentPayload
                    {
                        SegmentOrder = 2, SegmentType = GeneratedCatalogSegmentType.Steady,
                        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance, TargetDistanceKm = 1.0,
                        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "controlled_sharpening" },
                        Intensity = "controlled_sharpening", DisplayText = "controlled_sharpening",
                    },
                    new GeneratedCatalogWorkoutSegmentPayload
                    {
                        SegmentOrder = 3, SegmentType = GeneratedCatalogSegmentType.CoolDown,
                        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance, TargetDistanceKm = 2.0,
                        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "easy_recovery" },
                        Intensity = "easy_recovery", DisplayText = "easy_recovery",
                    },
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

        var sessions = new List<GeneratedCatalogTrainingDayPayload>
        {
            BuildTaperSession(0, 1, "TAPER_SHARPEN", "KEY_SESSION", 5.0, true),
            BuildTaperSession(2, 2, "TAPER", "EASY_SUPPORT", 4.0, false),
            BuildTaperSession(4, 3, "TAPER", "EASY_SUPPORT", 3.0, false),
            BuildTaperSession(6, 4, "TAPER", "LONG_RUN", 6.0, false),
        };
        var deps = DependencyVersions();

        return new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion,
            StartDate = start,
            EndDate = start.AddDays(6),
            PlannedWeekCount = 1,
            DaysPerWeek = 4,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            CandidateKey = CandidateKey,
            CandidateVersion = CandidateVersion,
            DependencyVersions = deps,
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
                CandidateKey = CandidateKey,
                CandidateVersion = CandidateVersion,
                DependencyVersions = deps,
                GenerationSource = "CATALOG",
                AsOfDate = start,
                MaterializerVersion = "TEST_TAPER_SHARPEN_ROUNDTRIP",
            },
        };
    }

    private static readonly DateOnly PlanStart = new(2026, 8, 3);

    [Fact]
    public async Task PrescriptionJson_EveryMixedSession_HasCorrectSchemaKeyAndVersion()
    {
        var days = await ConfirmAndGetDaysAsync(BuildMixedSessionPlan(PlanStart));

        Assert.Equal(4, days.Count);
        foreach (var day in days)
        {
            using var doc = JsonDocument.Parse(day.CatalogPrescriptionJson!);
            Assert.Equal("CATALOG_SESSION_PRESCRIPTION_SNAPSHOT", doc.RootElement.GetProperty("schema_key").GetString());
            Assert.Equal(1, doc.RootElement.GetProperty("schema_version").GetInt32());
        }
    }

    [Fact]
    public async Task PrescriptionJson_EffortOnlyEasySession_HasNoZeroNumericPaceAndNullDuration()
    {
        var days = await ConfirmAndGetDaysAsync(BuildMixedSessionPlan(PlanStart));
        var easySession = days.Single(d => d.DayType == RunningApp.Domain.Enums.TrainingDayType.Easy && d.CatalogWorkoutKey == "EASY_STANDARD");

        using var doc = JsonDocument.Parse(easySession.CatalogPrescriptionJson!);
        var pace = doc.RootElement.GetProperty("pace");

        // No numeric pace field is present as a literal zero for an
        // effort-only session — they must be JSON null.
        Assert.Equal(JsonValueKind.Null, pace.GetProperty("target_seconds_per_km").ValueKind);
        Assert.Equal(JsonValueKind.Null, pace.GetProperty("min_seconds_per_km").ValueKind);
        Assert.Equal(JsonValueKind.Null, pace.GetProperty("max_seconds_per_km").ValueKind);
        Assert.Equal("effort_only", pace.GetProperty("pace_type").GetString());

        // Unresolved/null duration: both duration fields are null.
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("target_duration_minutes").ValueKind);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("estimated_duration_minutes").ValueKind);

        // Also confirmed via the persisted TrainingDay row itself.
        Assert.Null(easySession.PlannedPaceMinKm);
    }

    [Fact]
    public async Task PrescriptionJson_GoalPaceSession_HasExactTargetPace()
    {
        var days = await ConfirmAndGetDaysAsync(BuildMixedSessionPlan(PlanStart));
        var goalPaceSession = days.Single(d => d.CatalogWorkoutKey == "GOAL_PACE_TEN_K");

        using var doc = JsonDocument.Parse(goalPaceSession.CatalogPrescriptionJson!);
        var pace = doc.RootElement.GetProperty("pace");

        Assert.Equal("target", pace.GetProperty("pace_type").GetString());
        Assert.Equal(285, pace.GetProperty("target_seconds_per_km").GetInt32());
        Assert.Equal(JsonValueKind.Null, pace.GetProperty("min_seconds_per_km").ValueKind);
        Assert.Equal(JsonValueKind.Null, pace.GetProperty("max_seconds_per_km").ValueKind);

        // Semantic round-trip against the persisted TrainingDay's own scalar copy.
        Assert.Equal(285 / 60.0, goalPaceSession.PlannedPaceMinKm);
    }

    [Fact]
    public async Task PrescriptionJson_PaceRangeSession_HasMinAndMaxNoTarget()
    {
        var days = await ConfirmAndGetDaysAsync(BuildMixedSessionPlan(PlanStart));
        var rangeSession = days.Single(d => d.CatalogWorkoutKey == "THRESHOLD_TEMPO");

        using var doc = JsonDocument.Parse(rangeSession.CatalogPrescriptionJson!);
        var pace = doc.RootElement.GetProperty("pace");

        Assert.Equal("range", pace.GetProperty("pace_type").GetString());
        Assert.Equal(300, pace.GetProperty("min_seconds_per_km").GetInt32());
        Assert.Equal(330, pace.GetProperty("max_seconds_per_km").GetInt32());
        Assert.Equal(JsonValueKind.Null, pace.GetProperty("target_seconds_per_km").ValueKind);

        // Duration-basis session: PlannedPaceMinKm is only populated for
        // PaceType.Target sessions (see BuildCatalogTrainingDay) — a Range
        // session correctly leaves it null rather than guessing a midpoint.
        Assert.Null(rangeSession.PlannedPaceMinKm);
    }

    [Fact]
    public async Task PrescriptionJson_FartlekThresholdGoalPaceSession_SegmentsAppearInSegmentOrder()
    {
        var days = await ConfirmAndGetDaysAsync(BuildMixedSessionPlan(PlanStart));
        var segmentedSession = days.Single(d => d.CatalogWorkoutKey == "FARTLEK_THRESHOLD_GOALPACE");

        using var doc = JsonDocument.Parse(segmentedSession.CatalogPrescriptionJson!);
        var segments = doc.RootElement.GetProperty("segments").EnumerateArray().ToList();

        Assert.Equal(3, segments.Count);
        Assert.Equal(1, segments[0].GetProperty("segment_order").GetInt32());
        Assert.Equal(2, segments[1].GetProperty("segment_order").GetInt32());
        Assert.Equal(3, segments[2].GetProperty("segment_order").GetInt32());
        Assert.Equal("FARTLEK_WARMUP", segments[0].GetProperty("display_text").GetString());
        Assert.Equal("THRESHOLD_INTERVAL", segments[1].GetProperty("display_text").GetString());
        Assert.Equal("GOAL_PACE_STEADY", segments[2].GetProperty("display_text").GetString());
    }

    [Fact]
    public async Task PrescriptionJson_TaperSharpenSession_RoundTripsSegmentsAndProvenance()
    {
        var days = await ConfirmAndGetDaysAsync(BuildTaperSharpenPlan(PlanStart));
        var taperSharpen = days.Single(d => d.CatalogProgressionStageKey == "TAPER_SHARPEN");

        using var doc = JsonDocument.Parse(taperSharpen.CatalogPrescriptionJson!);
        Assert.Equal("CATALOG_SESSION_PRESCRIPTION_SNAPSHOT", doc.RootElement.GetProperty("schema_key").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("schema_version").GetInt32());

        var segments = doc.RootElement.GetProperty("segments").EnumerateArray().ToList();
        Assert.Equal(3, segments.Count);
        Assert.Equal("easy_baseline", segments[0].GetProperty("display_text").GetString());
        Assert.Equal("controlled_sharpening", segments[1].GetProperty("display_text").GetString());
        Assert.Equal("easy_recovery", segments[2].GetProperty("display_text").GetString());

        var provenance = doc.RootElement.GetProperty("provenance");
        Assert.Equal("TAPER_SHARPEN", provenance.GetProperty("source_stage_key").GetString());
    }

    [Fact]
    public async Task PrescriptionJson_SerializingTwice_ProducesByteIdenticalOutput()
    {
        // Two independent confirmations of the SAME session content (different
        // preview/plan identities) must produce byte-identical
        // CatalogPrescriptionJson for the corresponding session -- proving the
        // production serialization is deterministic, not just idempotent
        // within a single confirmation.
        var firstRunDays = await ConfirmAndGetDaysAsync(BuildMixedSessionPlan(PlanStart));
        var secondRunDays = await ConfirmAndGetDaysAsync(BuildMixedSessionPlan(PlanStart));

        var firstGoalPace = firstRunDays.Single(d => d.CatalogWorkoutKey == "GOAL_PACE_TEN_K").CatalogPrescriptionJson;
        var secondGoalPace = secondRunDays.Single(d => d.CatalogWorkoutKey == "GOAL_PACE_TEN_K").CatalogPrescriptionJson;

        Assert.Equal(firstGoalPace, secondGoalPace, StringComparer.Ordinal);

        var firstSegmented = firstRunDays.Single(d => d.CatalogWorkoutKey == "FARTLEK_THRESHOLD_GOALPACE").CatalogPrescriptionJson;
        var secondSegmented = secondRunDays.Single(d => d.CatalogWorkoutKey == "FARTLEK_THRESHOLD_GOALPACE").CatalogPrescriptionJson;

        Assert.Equal(firstSegmented, secondSegmented, StringComparer.Ordinal);
    }

    [Fact]
    public async Task PrescriptionJson_DeserializesBackIntoJsonDocument_PreservingSemanticFieldValues()
    {
        var days = await ConfirmAndGetDaysAsync(BuildMixedSessionPlan(PlanStart));
        var goalPaceSession = days.Single(d => d.CatalogWorkoutKey == "GOAL_PACE_TEN_K");

        var json = goalPaceSession.CatalogPrescriptionJson!;
        using var doc = JsonDocument.Parse(json);

        // Round-trip through Deserialize back into a loosely-typed JsonDocument
        // (re-serialize the parsed document and re-parse) must preserve every
        // field's semantic value.
        var reserialized = JsonSerializer.Serialize(doc.RootElement);
        using var reparsed = JsonDocument.Parse(reserialized);

        Assert.Equal(
            doc.RootElement.GetProperty("workout_type").GetString(),
            reparsed.RootElement.GetProperty("workout_type").GetString());
        Assert.Equal(
            doc.RootElement.GetProperty("target_distance_km").GetDouble(),
            reparsed.RootElement.GetProperty("target_distance_km").GetDouble());
        Assert.Equal(
            doc.RootElement.GetProperty("pace").GetProperty("target_seconds_per_km").GetInt32(),
            reparsed.RootElement.GetProperty("pace").GetProperty("target_seconds_per_km").GetInt32());
        Assert.Equal(
            doc.RootElement.GetProperty("provenance").GetProperty("source_workout_key").GetString(),
            reparsed.RootElement.GetProperty("provenance").GetProperty("source_workout_key").GetString());
    }
}
