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
    /// Always empty in Phase 4E.1 — stage-to-week scheduling remains
    /// unimplemented (consistent with every prior Phase 4D resolver phase's
    /// own explicit boundary). Present in the schema for forward
    /// compatibility with the future phase that populates it.
    /// </summary>
    public required IReadOnlyList<string> SelectedStageKeys { get; init; }

    /// <summary>Fallback stage keys used (see <see cref="StageEligibilityEvaluator"/>). Always empty in Phase 4E.1, for the same reason as <see cref="SelectedStageKeys"/>.</summary>
    public required IReadOnlyList<string> FallbackStagesUsed { get; init; }

    /// <summary>
    /// The actual generated plan payload (weeks/days), once stage-to-week
    /// scheduling exists. Always null in Phase 4E.1/4E.2 — there is no plan
    /// content to freeze yet. Present in the schema for forward
    /// compatibility, explicitly documented as unpopulated rather than
    /// silently omitted.
    ///
    /// Backend Integration Phase 4F.1: retyped from <c>object?</c> to the
    /// strongly typed <see cref="GeneratedCatalogPlanPayload"/> contract
    /// (Decision 2 — never <c>object</c>/<c>dynamic</c>/<c>JsonElement</c>/an
    /// untyped dictionary as the domain contract). This is a type-safety
    /// change only: no production code path passes a non-null value here
    /// (verified — <c>CatalogPreviewGenerator.GenerateAsync</c> never
    /// supplies this parameter), so every real snapshot's value is
    /// unaffected: still <c>null</c>.
    /// </summary>
    public GeneratedCatalogPlanPayload? GeneratedPreviewPlanPayload { get; init; }

    /// <summary>SHA-256 hex digest of this snapshot's own canonical JSON (excluding this field itself) — a content-integrity check, not a security signature.</summary>
    public required string ContentHash { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}

/// <summary>
/// Builds a <see cref="CatalogPreviewSnapshot"/> and computes its
/// <see cref="CatalogPreviewSnapshot.ContentHash"/> deterministically.
/// </summary>
public static class CatalogPreviewSnapshotBuilder
{
    private static readonly JsonSerializerOptions HashSerializerOptions = new()
    {
        WriteIndented = false,
    };

    /// <summary>
    /// Builds the snapshot and stamps <see cref="CatalogPreviewSnapshot.ContentHash"/>
    /// with the SHA-256 of the canonical JSON serialization of every OTHER
    /// field (the hash cannot include itself).
    ///
    /// Backend Integration Phase 4F.1 compatibility note: <paramref name="generatedPreviewPlanPayload"/>
    /// (now strongly typed — see <see cref="Schedule.GeneratedCatalogPlanPayload"/>)
    /// is deliberately NOT included in <c>hashableContent</c> below, exactly as
    /// in Phase 4E.1/4E.2. This is an explicit compatibility decision, not an
    /// oversight: every real production snapshot still has this parameter
    /// null, so excluding it keeps the hash format byte-for-byte unchanged —
    /// zero risk to any already-stored snapshot's <c>ContentHash</c>, and zero
    /// need for a schema-version bump on the OUTER snapshot. Whether a future
    /// materialization phase's real (non-null) schedule content should become
    /// part of the integrity hash is a real product decision (tamper-evidence
    /// for the schedule itself vs. keeping the hash scoped to
    /// resolution-inputs-only) deliberately left open for that phase, not
    /// silently decided here.
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

        var hashableContent = new
        {
            normalizedInput,
            asOfDate,
            candidate.CandidateKey,
            candidate.CandidateVersion,
            CandidateStatusAtGenerationTime = candidate.CandidateStatus,
            referencedArtifacts,
            GenerationSource = PreviewRouting.GenerationSource.Catalog,
            routeReason,
            resolverResults = resolverResults.Select(r => new { r.ConditionType, Status = r.Status.ToString(), r.OutputValue, r.ReasonCode, r.Metadata }),
            createdAtUtc,
            expiresAtUtc,
        };

        var contentHash = ComputeSha256Hex(JsonSerializer.Serialize(hashableContent, HashSerializerOptions));

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
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };
    }

    private static string ComputeSha256Hex(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
