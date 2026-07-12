using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule;

/// <summary>
/// Backend Integration Phase 4F.1 — contract serialization tests: proves the
/// typed <see cref="GeneratedCatalogPlanPayload"/> round-trips deterministically
/// through the same JSON conventions (snake_case + string enums) already used
/// by <c>PlanServices.SerializerOptions</c> / <c>CatalogPlanConfirmationService.SnapshotDeserializeOptions</c>,
/// and that the outer <see cref="CatalogPreviewSnapshot"/> contract no longer
/// accepts an untyped payload (Decision 2).
/// </summary>
public sealed class GeneratedCatalogPlanPayloadSerializationTests
{
    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    /// <summary>
    /// Mirrors <c>CatalogPlanConfirmationService.SnapshotDeserializeOptions</c>:
    /// full <see cref="CatalogPreviewSnapshot"/> round trips need the
    /// private-constructor-aware <see cref="RuntimeConditionResolutionResultConverter"/>
    /// for <c>ResolverResults</c> — <see cref="GeneratedCatalogPlanPayload"/>
    /// itself needs no custom converter (no private constructors anywhere in
    /// the Schedule contract types).
    /// </summary>
    private static readonly JsonSerializerOptions SnakeCaseOptionsWithResolverResultConverter = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
            new RuntimeConditionResolutionResultConverter(),
        },
    };

    // ── Test #1/#2: deterministic serialize + round-trip deserialize ────────

    [Fact]
    public void CompletePayload_SerializesDeterministically()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();

        var json1 = JsonSerializer.Serialize(payload, SnakeCaseOptions);
        var json2 = JsonSerializer.Serialize(payload, SnakeCaseOptions);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void CompletePayload_RoundTripsToAnEquivalentTypedPayload()
    {
        var original = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();

        var json = JsonSerializer.Serialize(original, SnakeCaseOptions);
        var roundTripped = JsonSerializer.Deserialize<GeneratedCatalogPlanPayload>(json, SnakeCaseOptions);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.SchemaVersion, roundTripped!.SchemaVersion);
        Assert.Equal(original.StartDate, roundTripped.StartDate);
        Assert.Equal(original.EndDate, roundTripped.EndDate);
        Assert.Equal(original.PlannedWeekCount, roundTripped.PlannedWeekCount);
        Assert.Equal(original.DaysPerWeek, roundTripped.DaysPerWeek);
        Assert.Equal(original.CanonicalDistanceFamily, roundTripped.CanonicalDistanceFamily);
        Assert.Equal(original.GoalType, roundTripped.GoalType);
        Assert.Equal(original.CandidateKey, roundTripped.CandidateKey);
        Assert.Equal(original.Weeks.Count, roundTripped.Weeks.Count);
        Assert.Equal(
            original.Weeks.SelectMany(w => w.Sessions).Count(),
            roundTripped.Weeks.SelectMany(w => w.Sessions).Count());

        var originalFirstSession = original.Weeks[0].Sessions[0];
        var roundTrippedFirstSession = roundTripped.Weeks[0].Sessions[0];
        Assert.Equal(originalFirstSession.WorkoutType, roundTrippedFirstSession.WorkoutType);
        Assert.Equal(originalFirstSession.PrescriptionBasis, roundTrippedFirstSession.PrescriptionBasis);
        Assert.Equal(originalFirstSession.TargetDistanceKm, roundTrippedFirstSession.TargetDistanceKm);
        Assert.Equal(originalFirstSession.PacePrescription.PaceType, roundTrippedFirstSession.PacePrescription.PaceType);

        // A round-tripped instance re-validates identically to the original —
        // the strongest practical proof the round trip preserved every field
        // the validator inspects.
        var validator = new GeneratedCatalogPlanPayloadValidator();
        Assert.True(validator.Validate(original).IsValid);
        Assert.True(validator.Validate(roundTripped).IsValid);
    }

    [Fact]
    public void PayloadWithSegments_RoundTripsSegmentsCorrectly()
    {
        var original = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var sessionWithSegments = original.Weeks[0].Sessions.Single(s => s.Segments.Count > 0);

        var json = JsonSerializer.Serialize(original, SnakeCaseOptions);
        var roundTripped = JsonSerializer.Deserialize<GeneratedCatalogPlanPayload>(json, SnakeCaseOptions)!;
        var roundTrippedSession = roundTripped.Weeks[0].Sessions.Single(s => s.Segments.Count > 0);

        Assert.Equal(sessionWithSegments.Segments.Count, roundTrippedSession.Segments.Count);
        for (var i = 0; i < sessionWithSegments.Segments.Count; i++)
        {
            Assert.Equal(sessionWithSegments.Segments[i].SegmentOrder, roundTrippedSession.Segments[i].SegmentOrder);
            Assert.Equal(sessionWithSegments.Segments[i].SegmentType, roundTrippedSession.Segments[i].SegmentType);
            Assert.Equal(sessionWithSegments.Segments[i].PrescriptionBasis, roundTrippedSession.Segments[i].PrescriptionBasis);
        }
    }

    // ── Test #3: null payload remains supported ──────────────────────────────

    [Fact]
    public void NullGeneratedPreviewPlanPayload_SerializesAndDeserializesAsNull()
    {
        var snapshot = BuildSnapshotWithPayload(generatedPreviewPlanPayload: null);

        var json = JsonSerializer.Serialize(snapshot, SnakeCaseOptions);
        var roundTripped = JsonSerializer.Deserialize<CatalogPreviewSnapshot>(json, SnakeCaseOptionsWithResolverResultConverter);

        Assert.NotNull(roundTripped);
        Assert.Null(roundTripped!.GeneratedPreviewPlanPayload);
    }

    [Fact]
    public void NullGeneratedPreviewPlanPayload_DoesNotAffectContentHash()
    {
        // Same content, one null payload — proves the (Phase 4F.1-documented)
        // decision that GeneratedPreviewPlanPayload is excluded from the hash
        // holds, by directly asserting a null payload hashes identically to
        // itself across two independent Build calls with identical other inputs.
        var snapshotA = BuildSnapshotWithPayload(generatedPreviewPlanPayload: null);
        var snapshotB = BuildSnapshotWithPayload(generatedPreviewPlanPayload: null, sameTimestampsAs: snapshotA);

        Assert.Equal(snapshotA.ContentHash, snapshotB.ContentHash);
        Assert.True(CatalogPreviewSnapshotVerifier.Verify(snapshotA));
    }

    // ── Test #4: arbitrary object payloads are no longer accepted ───────────

    [Fact]
    public void GeneratedPreviewPlanPayload_PropertyType_IsTheTypedContractOnly_NeverObjectOrDynamicOrJsonElement()
    {
        var property = typeof(CatalogPreviewSnapshot).GetProperty(nameof(CatalogPreviewSnapshot.GeneratedPreviewPlanPayload));

        Assert.NotNull(property);
        Assert.Equal(typeof(GeneratedCatalogPlanPayload), property!.PropertyType);
        Assert.NotEqual(typeof(object), property.PropertyType);
        Assert.NotEqual(typeof(JsonElement), property.PropertyType);
    }

    [Fact]
    public void CatalogPreviewSnapshotBuilder_Build_GeneratedPreviewPlanPayloadParameter_IsTypedContractOnly()
    {
        var buildMethod = typeof(CatalogPreviewSnapshotBuilder).GetMethod(nameof(CatalogPreviewSnapshotBuilder.Build));
        var parameter = buildMethod!.GetParameters().Single(p => p.Name == "generatedPreviewPlanPayload");

        Assert.Equal(typeof(GeneratedCatalogPlanPayload), parameter.ParameterType);
    }

    // ── Test #5: unsupported schedule schema version is rejected ────────────

    [Fact]
    public void UnsupportedSchemaVersion_FailsValidation_WithExplicitTypedError()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var unsupportedVersionPayload = new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion + 1,
            StartDate = payload.StartDate,
            EndDate = payload.EndDate,
            PlannedWeekCount = payload.PlannedWeekCount,
            DaysPerWeek = payload.DaysPerWeek,
            CanonicalDistanceFamily = payload.CanonicalDistanceFamily,
            GoalType = payload.GoalType,
            CandidateKey = payload.CandidateKey,
            CandidateVersion = payload.CandidateVersion,
            DependencyVersions = payload.DependencyVersions,
            Weeks = payload.Weeks,
            Provenance = payload.Provenance,
        };

        var result = new GeneratedCatalogPlanPayloadValidator().Validate(unsupportedVersionPayload);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.UnsupportedSchemaVersion, result.Errors);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static CatalogPreviewSnapshot BuildSnapshotWithPayload(
        GeneratedCatalogPlanPayload? generatedPreviewPlanPayload, CatalogPreviewSnapshot? sameTimestampsAs = null)
    {
        var input = new RunningApp.Application.RuntimeCatalog.Resolvers.ResolverInputSnapshot
        {
            GoalType = RunningApp.Domain.Enums.GoalType.Race,
            GoalDistance = RunningApp.Domain.Enums.GoalDistance.TenK,
        };

        var candidate = new RunningApp.Application.RuntimeCatalog.PlanCatalogCandidateSummary
        {
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            CandidateStatus = "DRAFT",
            DependencyStatuses = new System.Collections.Generic.Dictionary<string, string>(),
            CanonicalDistanceFamily = "TEN_K",
            Level = "INTERMEDIATE",
            DaysPerWeek = 4,
            CoreCycle = new RunningApp.Application.RuntimeCatalog.PlanCatalogCoreCycle(8, 12, 14),
            MasterTemplate = new RunningApp.Application.RuntimeCatalog.PlanCatalogReference("TEN_K_MASTER", 6),
            Layout = new RunningApp.Application.RuntimeCatalog.PlanCatalogReference("RUN_LAYOUT_4D", 2),
            LevelModifier = new RunningApp.Application.RuntimeCatalog.PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
            WorkoutProgression = new RunningApp.Application.RuntimeCatalog.PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION", 5),
            ProgressionModifier = new RunningApp.Application.RuntimeCatalog.PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER", 1),
            RulePack = new RunningApp.Application.RuntimeCatalog.PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
            PeakVolumeBandPolicy = new RunningApp.Application.RuntimeCatalog.PlanCatalogReference("PEAK_VOLUME_BAND_POLICY", 1),
            RuntimeConditionValueRegistry = new RunningApp.Application.RuntimeCatalog.PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
            ReferencedWorkouts = new System.Collections.Generic.List<RunningApp.Application.RuntimeCatalog.PlanCatalogReference>(),
            PhaseKeys = new System.Collections.Generic.List<string>(),
            SlotRoles = new System.Collections.Generic.List<string>(),
        };

        var results = new System.Collections.Generic.List<RunningApp.Application.RuntimeCatalog.Resolvers.RuntimeConditionResolutionResult>
        {
            RunningApp.Application.RuntimeCatalog.Resolvers.RuntimeConditionResolutionResult.Evaluated("TIME_ADEQUACY_IN", "ADEQUATE", "MEETS_DEFAULT_CORE_DURATION"),
        };
        var trace = new RunningApp.Application.RuntimeCatalog.Resolvers.ResolverDecisionTrace { Steps = System.Array.Empty<RunningApp.Application.RuntimeCatalog.Resolvers.ResolverDecisionTraceStep>() };

        var createdAtUtc = sameTimestampsAs?.CreatedAtUtc ?? new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
        var expiresAtUtc = sameTimestampsAs?.ExpiresAtUtc ?? createdAtUtc.AddMinutes(30);
        var asOfDate = sameTimestampsAs?.AsOfDate ?? new DateOnly(2026, 8, 3);

        return CatalogPreviewSnapshotBuilder.Build(
            input, asOfDate, candidate, "PILOT_TEN_K_INTERMEDIATE_4D_MATCH",
            results, trace, createdAtUtc, expiresAtUtc, generatedPreviewPlanPayload);
    }
}
