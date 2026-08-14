using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.1 — the immutable, frozen record of
/// everything that went into (and came out of) a single catalog-routed
/// preview generation attempt. Built once, at preview-creation time, and
/// never recomputed afterward.
///
/// Keeps <see cref="AsOfDate"/> (the domain evaluation date, supplied once
/// to every resolver in the pipeline) explicitly distinct from
/// <see cref="CreatedAtUtc"/> (the technical wall-clock creation timestamp)
/// — the two must never be conflated, per this phase's own governance rule.
///
/// This is a Phase 4E.1 foundation type only: Phase 4E.2 is what will teach
/// <c>ConfirmPlanAsync</c> to read, validate, and persist a stored snapshot
/// like this one. Nothing in this phase wires this type into confirm.
/// </summary>
public sealed class CatalogPreviewSnapshot
{
    /// <summary>The normalized user/request evidence used for resolution (Phase 4C's ResolverInputSnapshot).</summary>
    public required ResolverInputSnapshot NormalizedInput { get; init; }

    /// <summary>Domain evaluation date, computed once and supplied to every resolver in the pipeline. Distinct from <see cref="CreatedAtUtc"/>.</summary>
    public required DateOnly AsOfDate { get; init; }

    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }

    /// <summary>The candidate's own metadata.status at the moment this snapshot was generated (a frozen snapshot, never re-read afterward — mirrors Phase 3's own CatalogCandidateStatusAtGenerationTime rationale).</summary>
    public required string CandidateStatusAtGenerationTime { get; init; }

    /// <summary>Every referenced artifact's exact (key, version) identity, keyed by role (masterTemplate, layout, levelModifier, workoutProgression, progressionModifier, rulePack, peakVolumeBandPolicy, runtimeConditionValueRegistry).</summary>
    public required IReadOnlyDictionary<string, PlanCatalogReference> ReferencedArtifacts { get; init; }

    /// <summary>Always <see cref="GenerationSource.Catalog"/> for a snapshot produced by this type — recorded explicitly rather than assumed.</summary>
    public required string GenerationSource { get; init; }

    public required string RouteReason { get; init; }

    /// <summary>Ordered resolver results (TIME_ADEQUACY_IN, PACE_SOURCE_IN, CORE_ENTRY_READINESS_IN, GOAL_FEASIBILITY_IN), exactly as produced by RuntimeConditionResolutionService.ResolveAllResults.</summary>
    public required IReadOnlyList<RuntimeConditionResolutionResult> ResolverResults { get; init; }

    /// <summary>Internal decision trace built from the same results (application-layer only — never serialized onto any public DTO).</summary>
    public required ResolverDecisionTrace DecisionTrace { get; init; }

    /// <summary>
    /// Stage keys selected as primary/eligible during this preview attempt.
    /// Populated by the current fixed or dynamic core orchestration and frozen
    /// for confirmation; never recomputed by a read model.
    /// </summary>
    public required IReadOnlyList<string> SelectedStageKeys { get; init; }

    /// <summary>Fallback stage keys used by the completed orchestration, if any (see <see cref="StageEligibilityEvaluator"/>).</summary>
    public required IReadOnlyList<string> FallbackStagesUsed { get; init; }

    /// <summary>
    /// The actual generated plan payload (weeks/days), populated for every
    /// successful current catalog preview and persisted exactly at confirm.
    ///
    /// Backend Integration Phase 4F.1: retyped from <c>object?</c> to the
    /// strongly typed <see cref="GeneratedCatalogPlanPayload"/> contract
    /// (Decision 2 — never <c>object</c>/<c>dynamic</c>/<c>JsonElement</c>/an
    /// untyped dictionary as the domain contract). This is a type-safety
    /// change that now provides the typed confirmation boundary.
    /// </summary>
    public GeneratedCatalogPlanPayload? GeneratedPreviewPlanPayload { get; init; }

    /// <summary>SHA-256 hex digest of this snapshot's own canonical JSON (excluding this field itself) — a content-integrity check, not a security signature.</summary>
    public required string ContentHash { get; init; }

    /// <summary>
    /// Phase 4F.9.2 — identifies which hash canonicalization algorithm
    /// produced <see cref="ContentHash"/>. Added after a real-PostgreSQL
    /// relational-validation pass discovered that <c>ReferencedArtifacts</c>,
    /// <c>GeneratedPreviewPlanPayload.DependencyVersions</c>,
    /// <c>GeneratedPreviewPlanPayload.Provenance.DependencyVersions</c>, and
    /// each resolver result's <c>Metadata</c> are all <c>Dictionary</c>-typed
    /// fields whose enumeration order is NOT preserved once
    /// <c>PlanPreviews.PreviewPayloadJson</c> is stored as PostgreSQL
    /// <c>jsonb</c> (jsonb canonicalizes/reorders object keys on write) — the
    /// original (version 1, pre-Phase-4F.9.2) hash was computed by directly
    /// serializing those dictionaries in whatever order .NET happened to
    /// enumerate them, making the hash order-dependent and therefore
    /// guaranteed to mismatch after any real jsonb round-trip. Version 2 (see
    /// <see cref="CatalogPreviewCanonicalHashSerializer"/>) sorts every
    /// dictionary-shaped field by key (<c>StringComparer.Ordinal</c>) before
    /// hashing, making the hash independent of storage-layer key reordering.
    /// No real (non-test) snapshot was ever produced with the version-1
    /// algorithm — this feature has never been committed, published, or
    /// activated — so only version 2 is a supported/verifiable value; any
    /// other value fails closed rather than silently falling back to the
    /// broken order-dependent algorithm.
    /// </summary>
    public required int HashAlgorithmVersion { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}

/// <summary>
/// Phase 4F.9.2 — the single canonical hash-content builder shared by
/// <see cref="CatalogPreviewSnapshotBuilder"/> (at generation time) and
/// <see cref="CatalogPreviewSnapshotVerifier"/> (at confirm time), so the two
/// can never silently diverge. Sorts every dictionary-shaped field reachable
/// from the hashable content by key (never relying on Dictionary insertion
/// order, PostgreSQL jsonb key order, or System.Text.Json's incidental
/// preservation of input order) while leaving every semantically-ordered
/// list (resolver results, weeks, sessions, segments) untouched.
/// </summary>
public static class CatalogPreviewCanonicalHashSerializer
{
    /// <summary>
    /// Version 2: dictionary-shaped fields are canonicalized (sorted by key,
    /// <see cref="StringComparer.Ordinal"/>) before hashing. See
    /// <see cref="CatalogPreviewSnapshot.HashAlgorithmVersion"/> for why
    /// version 1 (order-dependent) is not implemented as a fallback.
    /// </summary>
    public const int CurrentHashAlgorithmVersion = 2;

    private static readonly JsonSerializerOptions HashSerializerOptions = new()
    {
        WriteIndented = false,
    };

    public static string ComputeContentHash(
        ResolverInputSnapshot normalizedInput,
        DateOnly asOfDate,
        string candidateKey,
        int candidateVersion,
        string candidateStatusAtGenerationTime,
        IReadOnlyDictionary<string, PlanCatalogReference> referencedArtifacts,
        string generationSource,
        string routeReason,
        IReadOnlyList<RuntimeConditionResolutionResult> resolverResults,
        GeneratedCatalogPlanPayload? generatedPreviewPlanPayload,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        var hashableContent = new
        {
            normalizedInput,
            asOfDate,
            CandidateKey = candidateKey,
            CandidateVersion = candidateVersion,
            CandidateStatusAtGenerationTime = candidateStatusAtGenerationTime,
            referencedArtifacts = Canonicalize(referencedArtifacts),
            GenerationSource = generationSource,
            routeReason,
            resolverResults = resolverResults.Select(r => new
            {
                r.ConditionType,
                Status = r.Status.ToString(),
                r.OutputValue,
                r.ReasonCode,
                Metadata = Canonicalize(r.Metadata)
            }),
            generatedPreviewPlanPayload = CanonicalizePayload(generatedPreviewPlanPayload),
            createdAtUtc,
            expiresAtUtc,
        };

        var json = JsonSerializer.Serialize(hashableContent, HashSerializerOptions);
        return ComputeSha256Hex(json);
    }

    private static SortedDictionary<string, TValue> Canonicalize<TValue>(IReadOnlyDictionary<string, TValue> source) =>
        new(source.ToDictionary(kv => kv.Key, kv => kv.Value), StringComparer.Ordinal);

    private static object? CanonicalizePayload(GeneratedCatalogPlanPayload? payload)
    {
        if (payload is null) return null;

        return new
        {
            payload.SchemaVersion,
            payload.StartDate,
            payload.EndDate,
            payload.PlannedWeekCount,
            payload.DaysPerWeek,
            payload.CanonicalDistanceFamily,
            payload.GoalType,
            payload.CandidateKey,
            payload.CandidateVersion,
            DependencyVersions = Canonicalize(payload.DependencyVersions),
            payload.Weeks, // order is semantically meaningful (week sequence) — never sorted
            Provenance = new
            {
                payload.Provenance.CandidateKey,
                payload.Provenance.CandidateVersion,
                DependencyVersions = Canonicalize(payload.Provenance.DependencyVersions),
                payload.Provenance.GenerationSource,
                payload.Provenance.AsOfDate,
                payload.Provenance.MaterializerVersion,
            },
        };
    }

    private static string ComputeSha256Hex(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Builds a <see cref="CatalogPreviewSnapshot"/> and computes its
/// <see cref="CatalogPreviewSnapshot.ContentHash"/> deterministically.
/// </summary>
public static class CatalogPreviewSnapshotBuilder
{
    /// <summary>
    /// Builds the snapshot and stamps <see cref="CatalogPreviewSnapshot.ContentHash"/>
    /// with the SHA-256 of the canonical JSON serialization of every OTHER
    /// field (the hash cannot include itself), via the shared
    /// <see cref="CatalogPreviewCanonicalHashSerializer"/> (Phase 4F.9.2 —
    /// order-independent for every dictionary-shaped field; see
    /// <see cref="CatalogPreviewSnapshot.HashAlgorithmVersion"/>).
    ///
    /// Backend Integration Phase 4F.5+ update: <paramref name="generatedPreviewPlanPayload"/>
    /// (strongly typed — see <see cref="Schedule.GeneratedCatalogPlanPayload"/>)
    /// IS included in the hashed content and therefore IS part of the
    /// computed <see cref="CatalogPreviewSnapshot.ContentHash"/>. This
    /// supersedes the earlier Phase 4E.1/4E.2/4F.1 decision to exclude it
    /// while it was always null — now that real production snapshots
    /// populate a non-null schedule payload, including it in the hash gives
    /// tamper-evidence over the schedule content itself, not just the
    /// resolution inputs.
    /// </summary>
    public static CatalogPreviewSnapshot Build(
        ResolverInputSnapshot normalizedInput,
        DateOnly asOfDate,
        PlanCatalogCandidateSummary candidate,
        string routeReason,
        IReadOnlyList<RuntimeConditionResolutionResult> resolverResults,
        ResolverDecisionTrace decisionTrace,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        GeneratedCatalogPlanPayload? generatedPreviewPlanPayload = null)
    {
        var referencedArtifacts = new Dictionary<string, PlanCatalogReference>
        {
            ["masterTemplate"] = candidate.MasterTemplate,
            ["layout"] = candidate.Layout,
            ["levelModifier"] = candidate.LevelModifier,
            ["workoutProgression"] = candidate.WorkoutProgression,
            ["progressionModifier"] = candidate.ProgressionModifier,
            ["rulePack"] = candidate.RulePack,
            ["peakVolumeBandPolicy"] = candidate.PeakVolumeBandPolicy,
            ["runtimeConditionValueRegistry"] = candidate.RuntimeConditionValueRegistry,
        };

        var contentHash = CatalogPreviewCanonicalHashSerializer.ComputeContentHash(
            normalizedInput,
            asOfDate,
            candidate.CandidateKey,
            candidate.CandidateVersion,
            candidate.CandidateStatus,
            referencedArtifacts,
            PreviewRouting.GenerationSource.Catalog,
            routeReason,
            resolverResults,
            generatedPreviewPlanPayload,
            createdAtUtc,
            expiresAtUtc);

        return new CatalogPreviewSnapshot
        {
            NormalizedInput = normalizedInput,
            AsOfDate = asOfDate,
            CandidateKey = candidate.CandidateKey,
            CandidateVersion = candidate.CandidateVersion,
            CandidateStatusAtGenerationTime = candidate.CandidateStatus,
            ReferencedArtifacts = referencedArtifacts,
            GenerationSource = PreviewRouting.GenerationSource.Catalog,
            RouteReason = routeReason,
            ResolverResults = resolverResults,
            DecisionTrace = decisionTrace,
            SelectedStageKeys = Array.Empty<string>(),
            FallbackStagesUsed = Array.Empty<string>(),
            GeneratedPreviewPlanPayload = generatedPreviewPlanPayload,
            ContentHash = contentHash,
            HashAlgorithmVersion = CatalogPreviewCanonicalHashSerializer.CurrentHashAlgorithmVersion,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };
    }
}
