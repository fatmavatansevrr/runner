using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Phase 4F.9.2 — regression coverage for the canonical (order-independent)
/// snapshot hash introduced after real-PostgreSQL relational validation
/// discovered that <c>PlanPreviews.PreviewPayloadJson</c> being stored as
/// <c>jsonb</c> reorders JSON object keys, which broke the previous
/// order-dependent hash (see <see cref="CatalogPreviewSnapshot.HashAlgorithmVersion"/>
/// and <see cref="CatalogPreviewCanonicalHashSerializer"/>). Fully
/// provider-independent — no database involved; the actual jsonb round-trip
/// proof lives in <see cref="CatalogConfirmationRelationalTests"/>.
/// </summary>
public sealed class CatalogPreviewSnapshotHashCanonicalizationTests
{
    private static readonly ResolverInputSnapshot Input = new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        GoalDistanceKm = 10.0,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        RaceDate = new DateOnly(2026, 12, 1),
        CanonicalDistanceFamily = "TEN_K",
    };

    private static readonly List<RuntimeConditionResolutionResult> ResolverResults = new()
    {
        RuntimeConditionResolutionResult.NotEvaluated("TIME_ADEQUACY_IN", "NO_RACE_DATE"),
        RuntimeConditionResolutionResult.NotEvaluated("PACE_SOURCE_IN", "NO_PACE_EVIDENCE"),
    };

    // Same four entries, deliberately inserted in two different orders.
    private static Dictionary<string, PlanCatalogReference> ReferencedArtifactsInOrder() => new()
    {
        ["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 6),
        ["layout"] = new PlanCatalogReference("RUN_LAYOUT_4D", 2),
        ["levelModifier"] = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
        ["rulePack"] = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
    };

    private static Dictionary<string, PlanCatalogReference> ReferencedArtifactsReordered() => new()
    {
        ["rulePack"] = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
        ["layout"] = new PlanCatalogReference("RUN_LAYOUT_4D", 2),
        ["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 6),
        ["levelModifier"] = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
    };

    [Fact]
    public void ComputeContentHash_SameReferencedArtifacts_DifferentInsertionOrder_ProducesIdenticalHash()
    {
        var createdAt = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = createdAt.AddMinutes(30);
        var asOfDate = DateOnly.FromDateTime(createdAt);

        var hashA = CatalogPreviewCanonicalHashSerializer.ComputeContentHash(
            Input, asOfDate, "TEN_K__4D__INTERMEDIATE", 10, "DRAFT",
            ReferencedArtifactsInOrder(), GenerationSource.Catalog, "PILOT_MATCH",
            ResolverResults, null, createdAt, expiresAt);

        var hashB = CatalogPreviewCanonicalHashSerializer.ComputeContentHash(
            Input, asOfDate, "TEN_K__4D__INTERMEDIATE", 10, "DRAFT",
            ReferencedArtifactsReordered(), GenerationSource.Catalog, "PILOT_MATCH",
            ResolverResults, null, createdAt, expiresAt);

        Assert.Equal(hashA, hashB);
    }

    [Fact]
    public void ComputeContentHash_MaterialValueChange_StillChangesHash()
    {
        var createdAt = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = createdAt.AddMinutes(30);
        var asOfDate = DateOnly.FromDateTime(createdAt);

        var hashOriginal = CatalogPreviewCanonicalHashSerializer.ComputeContentHash(
            Input, asOfDate, "TEN_K__4D__INTERMEDIATE", 10, "DRAFT",
            ReferencedArtifactsInOrder(), GenerationSource.Catalog, "PILOT_MATCH",
            ResolverResults, null, createdAt, expiresAt);

        var tamperedArtifacts = ReferencedArtifactsInOrder();
        tamperedArtifacts["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 99); // version changed

        var hashTampered = CatalogPreviewCanonicalHashSerializer.ComputeContentHash(
            Input, asOfDate, "TEN_K__4D__INTERMEDIATE", 10, "DRAFT",
            tamperedArtifacts, GenerationSource.Catalog, "PILOT_MATCH",
            ResolverResults, null, createdAt, expiresAt);

        Assert.NotEqual(hashOriginal, hashTampered);
    }

    [Fact]
    public void ComputeContentHash_ResolverResultOrderChange_StillChangesHash()
    {
        // Resolver result order IS semantically meaningful (unlike dictionary
        // key order) and must NOT be canonicalized away.
        var createdAt = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = createdAt.AddMinutes(30);
        var asOfDate = DateOnly.FromDateTime(createdAt);

        var hashOriginalOrder = CatalogPreviewCanonicalHashSerializer.ComputeContentHash(
            Input, asOfDate, "TEN_K__4D__INTERMEDIATE", 10, "DRAFT",
            ReferencedArtifactsInOrder(), GenerationSource.Catalog, "PILOT_MATCH",
            ResolverResults, null, createdAt, expiresAt);

        var reversedResults = ResolverResults.AsEnumerable().Reverse().ToList();
        var hashReversedOrder = CatalogPreviewCanonicalHashSerializer.ComputeContentHash(
            Input, asOfDate, "TEN_K__4D__INTERMEDIATE", 10, "DRAFT",
            ReferencedArtifactsInOrder(), GenerationSource.Catalog, "PILOT_MATCH",
            reversedResults, null, createdAt, expiresAt);

        Assert.NotEqual(hashOriginalOrder, hashReversedOrder);
    }

    [Fact]
    public void Verify_RoundTrippedSnapshotWithReorderedDictionary_StillVerifiesTrue()
    {
        // Simulates exactly what real PostgreSQL jsonb storage does to
        // ReferencedArtifacts (reorders keys) without needing a database:
        // build a snapshot, then construct an equivalent snapshot whose
        // ReferencedArtifacts dictionary was populated in a different order,
        // and confirm both verify successfully against the SAME stored hash.
        var candidate = new PlanCatalogCandidateSummary
        {
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            CandidateStatus = "DRAFT",
            DependencyStatuses = new Dictionary<string, string> { ["masterTemplate"] = "DRAFT" },
            CanonicalDistanceFamily = "TEN_K",
            Level = "INTERMEDIATE",
            DaysPerWeek = 4,
            MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 6),
            Layout = new PlanCatalogReference("RUN_LAYOUT_4D", 2),
            LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
            WorkoutProgression = new PlanCatalogReference("TEN_K_INTERMEDIATE_PROGRESSION", 2),
            ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER", 1),
            RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
            PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BAND_POLICY", 1),
            RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
            ReferencedWorkouts = new List<PlanCatalogReference>(),
            PhaseKeys = new List<string> { "FOUNDATION" },
            PhaseAllocations = new List<PlanCatalogPhaseAllocation> { new("FOUNDATION", 4) },
            SlotRoles = new List<string> { "EASY" },
            CoreCycle = new PlanCatalogCoreCycle(8, 12, 16),
        };

        var now = DateTime.UtcNow;
        var snapshot = CatalogPreviewSnapshotBuilder.Build(
            normalizedInput: Input,
            asOfDate: DateOnly.FromDateTime(now),
            candidate: candidate,
            routeReason: "PILOT_MATCH",
            resolverResults: ResolverResults,
            decisionTrace: new ResolverDecisionTrace { Steps = Array.Empty<ResolverDecisionTraceStep>() },
            createdAtUtc: now,
            expiresAtUtc: now.AddMinutes(30));

        Assert.True(CatalogPreviewSnapshotVerifier.Verify(snapshot));

        // Rebuild ReferencedArtifacts in a different insertion order but with
        // identical content — this is exactly what a jsonb round-trip does.
        var reorderedArtifacts = new Dictionary<string, PlanCatalogReference>();
        foreach (var kv in snapshot.ReferencedArtifacts.OrderByDescending(kv => kv.Key, StringComparer.Ordinal))
        {
            reorderedArtifacts[kv.Key] = kv.Value;
        }

        var reorderedSnapshot = new CatalogPreviewSnapshot
        {
            NormalizedInput = snapshot.NormalizedInput,
            AsOfDate = snapshot.AsOfDate,
            CandidateKey = snapshot.CandidateKey,
            CandidateVersion = snapshot.CandidateVersion,
            CandidateStatusAtGenerationTime = snapshot.CandidateStatusAtGenerationTime,
            ReferencedArtifacts = reorderedArtifacts,
            GenerationSource = snapshot.GenerationSource,
            RouteReason = snapshot.RouteReason,
            ResolverResults = snapshot.ResolverResults,
            DecisionTrace = snapshot.DecisionTrace,
            SelectedStageKeys = snapshot.SelectedStageKeys,
            FallbackStagesUsed = snapshot.FallbackStagesUsed,
            GeneratedPreviewPlanPayload = snapshot.GeneratedPreviewPlanPayload,
            ContentHash = snapshot.ContentHash,
            HashAlgorithmVersion = snapshot.HashAlgorithmVersion,
            CreatedAtUtc = snapshot.CreatedAtUtc,
            ExpiresAtUtc = snapshot.ExpiresAtUtc,
        };

        Assert.True(CatalogPreviewSnapshotVerifier.Verify(reorderedSnapshot));
    }

    [Fact]
    public void Verify_UnsupportedHashAlgorithmVersion_ThrowsTypedException_FailsClosed()
    {
        var candidate = new PlanCatalogCandidateSummary
        {
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            CandidateStatus = "DRAFT",
            DependencyStatuses = new Dictionary<string, string> { ["masterTemplate"] = "DRAFT" },
            CanonicalDistanceFamily = "TEN_K",
            Level = "INTERMEDIATE",
            DaysPerWeek = 4,
            MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 6),
            Layout = new PlanCatalogReference("RUN_LAYOUT_4D", 2),
            LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
            WorkoutProgression = new PlanCatalogReference("TEN_K_INTERMEDIATE_PROGRESSION", 2),
            ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER", 1),
            RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
            PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BAND_POLICY", 1),
            RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
            ReferencedWorkouts = new List<PlanCatalogReference>(),
            PhaseKeys = new List<string> { "FOUNDATION" },
            PhaseAllocations = new List<PlanCatalogPhaseAllocation> { new("FOUNDATION", 4) },
            SlotRoles = new List<string> { "EASY" },
            CoreCycle = new PlanCatalogCoreCycle(8, 12, 16),
        };

        var now = DateTime.UtcNow;
        var snapshot = CatalogPreviewSnapshotBuilder.Build(
            normalizedInput: Input,
            asOfDate: DateOnly.FromDateTime(now),
            candidate: candidate,
            routeReason: "PILOT_MATCH",
            resolverResults: ResolverResults,
            decisionTrace: new ResolverDecisionTrace { Steps = Array.Empty<ResolverDecisionTraceStep>() },
            createdAtUtc: now,
            expiresAtUtc: now.AddMinutes(30));

        var downgraded = new CatalogPreviewSnapshot
        {
            NormalizedInput = snapshot.NormalizedInput,
            AsOfDate = snapshot.AsOfDate,
            CandidateKey = snapshot.CandidateKey,
            CandidateVersion = snapshot.CandidateVersion,
            CandidateStatusAtGenerationTime = snapshot.CandidateStatusAtGenerationTime,
            ReferencedArtifacts = snapshot.ReferencedArtifacts,
            GenerationSource = snapshot.GenerationSource,
            RouteReason = snapshot.RouteReason,
            ResolverResults = snapshot.ResolverResults,
            DecisionTrace = snapshot.DecisionTrace,
            SelectedStageKeys = snapshot.SelectedStageKeys,
            FallbackStagesUsed = snapshot.FallbackStagesUsed,
            GeneratedPreviewPlanPayload = snapshot.GeneratedPreviewPlanPayload,
            ContentHash = snapshot.ContentHash,
            HashAlgorithmVersion = 1, // simulate a pre-Phase-4F.9.2 (never actually shipped) snapshot
            CreatedAtUtc = snapshot.CreatedAtUtc,
            ExpiresAtUtc = snapshot.ExpiresAtUtc,
        };

        Assert.Throws<PlanPreviewHashAlgorithmVersionUnsupportedException>(
            () => CatalogPreviewSnapshotVerifier.Verify(downgraded));
    }
}
