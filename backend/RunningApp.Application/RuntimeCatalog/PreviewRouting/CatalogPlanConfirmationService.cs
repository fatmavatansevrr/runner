using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using System.Text.Json;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4E.2 — performs immutable catalog preview
/// confirmation: validates and persists the exact stored
/// <see cref="CatalogPreviewSnapshot"/> previously shown to the user.
///
/// The confirm semantic is exactly:
/// <code>confirm = validate and persist the exact stored preview snapshot</code>
///
/// It never becomes:
/// <code>
/// confirm → rerun route selection
///         → reload current catalog
///         → rerun resolver orchestration
///         → recompute AsOfDate
///         → reselect stages or fallbacks
///         → regenerate a potentially different plan
/// </code>
///
/// This service has NO dependency on any of the following — they must never
/// be injected or called, directly or indirectly, by this service:
/// <list type="bullet">
/// <item><see cref="IGenerationRouteDecider"/></item>
/// <item><see cref="ICatalogCandidateEligibilityGate"/></item>
/// <item><see cref="ICatalogPreviewGenerator"/></item>
/// <item><c>IRuntimeConditionResolutionService</c> / <c>RuntimeConditionResolutionService</c></item>
/// <item>Any runtime condition resolver (TimeAdequacy, PaceSource, CoreEntryReadiness, GoalFeasibility)</item>
/// <item><see cref="StageEligibilityEvaluator"/></item>
/// <item><c>IPlanGenerationEngine</c></item>
/// </list>
///
/// AsOfDate policy (TD-PACESOURCE-002 explicit closure decision, Phase 4E.2):
/// Confirm REUSES the preview's frozen <c>AsOfDate</c> from the stored
/// snapshot. It does NOT recompute from wall clock at confirm time.
/// <c>ConfirmedAtUtc</c> (= plan.CreatedAt) is a separate technical timestamp
/// and must never be used for any domain decision.
/// </summary>
public interface ICatalogPlanConfirmationService
{
    /// <summary>
    /// Validates and atomically persists the catalog plan stored in
    /// <paramref name="previewId"/>. Returns the confirmed plan response.
    ///
    /// Idempotent: repeated calls with the same preview return the
    /// already-confirmed plan without creating duplicates.
    ///
    /// Never falls back to SQL generation on any failure path.
    /// </summary>
    Task<ConfirmPlanResponse> ConfirmAsync(Guid internalUserId, Guid previewId, CancellationToken ct = default);
}

/// <inheritdoc cref="ICatalogPlanConfirmationService"/>
public sealed class CatalogPlanConfirmationService : ICatalogPlanConfirmationService
{
    private static readonly JsonSerializerOptions SnapshotDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
            // Phase 4E.2: RuntimeConditionResolutionResult has a private constructor
            // and cannot be deserialized by System.Text.Json's default converter.
            // This custom converter reconstructs instances via the public factory
            // methods, preserving the same invariants enforced at generation time.
            new RuntimeConditionResolutionResultConverter()
        }
    };

    private readonly AppDbContext _context;
    private readonly ILogger<CatalogPlanConfirmationService> _logger;
    private readonly IGeneratedCatalogPlanPayloadValidator _scheduleValidator;

    /// <summary>
    /// Backend Integration Phase 4F.1: adds <see cref="IGeneratedCatalogPlanPayloadValidator"/>
    /// as a required constructor dependency, used only by the (extended)
    /// step-11 persistability guard below. This does NOT add any generation,
    /// routing, or resolution dependency -- the validator itself has zero
    /// dependencies of its own (no database, clock, catalog loader, resolver,
    /// or route decider), preserving this service's existing isolation
    /// guarantee (see <see cref="CatalogPlanConfirmationService_HasNoGenerationOrResolutionDependencies"/>-style
    /// structural tests).
    /// </summary>
    public CatalogPlanConfirmationService(
        AppDbContext context,
        ILogger<CatalogPlanConfirmationService> logger,
        IGeneratedCatalogPlanPayloadValidator scheduleValidator)
    {
        _context = context;
        _logger = logger;
        _scheduleValidator = scheduleValidator;
    }

    /// <summary>
    /// Mandatory 15-step confirmation flow, executed in the deterministic
    /// order specified by Phase 4E.2:
    ///
    /// 1.  Load preview by stable identifier.
    /// 2.  Verify authenticated-user ownership.
    /// 3.  Verify preview has not expired.
    /// 4.  Verify preview has not been explicitly invalidated.
    /// 5.  Verify the immutable snapshot exists (PreviewPayloadJson non-empty).
    /// 6.  Parse the CatalogPreviewSnapshot from JSON.
    /// 7.  Verify snapshot schema/field completeness.
    /// 8.  Verify GenerationSource == "CATALOG".
    /// 9.  Verify snapshot content integrity (SHA-256 hash match).
    /// 10. Idempotency: if already confirmed, return existing plan.
    /// 11. Persistability guard (extended by Backend Integration Phase 4F.1):
    ///     <list type="bullet">
    ///     <item>GeneratedPreviewPlanPayload is null (still the case for
    ///     every real production snapshot, unchanged since Phase 4E.1/4E.2):
    ///     throws <see cref="CatalogPreviewNotPersistableException"/>.</item>
    ///     <item>Non-null but SchemaVersion unsupported: throws
    ///     <see cref="CatalogPreviewScheduleSchemaUnsupportedException"/>.</item>
    ///     <item>Non-null, schema supported, but fails structural validation
    ///     (<see cref="IGeneratedCatalogPlanPayloadValidator"/>): throws
    ///     <see cref="CatalogPreviewScheduleInvalidException"/>.</item>
    ///     <item>Non-null, schema supported, structurally valid: STILL
    ///     throws <see cref="CatalogPreviewMaterializationNotImplementedException"/>
    ///     — Phase 4F.1 defines the contract and validator only; persisting a
    ///     typed schedule into TrainingWeek/TrainingDay rows is explicitly
    ///     deferred to a later phase. A structurally valid payload is
    ///     necessary but not sufficient for a successful confirmation today.</item>
    ///     </list>
    ///     In every one of these four cases: no TrainingPlan, no TrainingWeek,
    ///     no TrainingDay, no PlanEvent is created; ConfirmedPlanId stays
    ///     null; the active-plan slot is not consumed.
    /// 12. Persist TrainingPlan with full catalog provenance from snapshot.
    ///     (Only reached when GeneratedPreviewPlanPayload is non-null and
    ///     contains the complete persistable schedule. Not reachable in
    ///     Phase 4E.1/4E.2.)
    /// 13. Persist TrainingWeeks and TrainingDays from snapshot payload.
    ///     (Not reachable in Phase 4E.1/4E.2.)
    /// 14. Persist catalog-generation audit PlanEvent.
    /// 15. Associate preview as confirmed (set ConfirmedPlanId) atomically
    ///     in one SaveChangesAsync call.
    ///
    /// No candidate selector, route decider, resolver, preview generator,
    /// stage evaluator, or generation engine is invoked.
    /// </summary>
    public async Task<ConfirmPlanResponse> ConfirmAsync(Guid internalUserId, Guid previewId, CancellationToken ct = default)
    {
        // ── Step 1: Load preview ──────────────────────────────────────────────
        // Do NOT use AsNoTracking here: we need to track the preview entity so
        // we can set ConfirmedPlanId atomically in the same SaveChangesAsync.
        var preview = await _context.PlanPreviews
            .FirstOrDefaultAsync(p => p.Id == previewId, ct);

        if (preview == null)
        {
            throw new PlanPreviewNotFoundException(
                $"Plan preview '{previewId}' was not found.");
        }

        // ── Step 2: Ownership ─────────────────────────────────────────────────
        if (preview.InternalUserId != internalUserId)
        {
            // Return the same 403 whether the preview doesn't exist for the
            // user or belongs to another — don't leak existence to non-owners.
            throw new PlanPreviewForbiddenException(
                $"Plan preview '{previewId}' does not belong to the authenticated user.");
        }

        // ── Step 3: Expiry ────────────────────────────────────────────────────
        if (preview.ExpiresAt < DateTime.UtcNow)
        {
            throw new PlanPreviewExpiredException(
                $"Plan preview '{previewId}' expired at {preview.ExpiresAt:O}. Generate a new preview.");
        }

        // ── Step 4: Invalidation ──────────────────────────────────────────────
        if (preview.IsInvalidated == true)
        {
            throw new PlanPreviewInvalidatedException(
                $"Plan preview '{previewId}' has been explicitly invalidated and cannot be confirmed.");
        }

        // ── Step 5: Snapshot presence ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(preview.PreviewPayloadJson))
        {
            throw new PlanPreviewSnapshotMissingException(
                $"Plan preview '{previewId}' has no stored snapshot. " +
                "This indicates a persistence defect — the catalog preview was created without its mandatory snapshot.");
        }

        // ── Step 6: Parse snapshot ────────────────────────────────────────────
        CatalogPreviewSnapshot snapshot;
        try
        {
            var parsed = JsonSerializer.Deserialize<CatalogPreviewSnapshot>(
                preview.PreviewPayloadJson, SnapshotDeserializeOptions);
            if (parsed == null)
            {
                throw new PlanPreviewSnapshotMalformedException(
                    $"Plan preview '{previewId}' snapshot deserialized as null. The stored JSON is not a valid CatalogPreviewSnapshot.");
            }
            snapshot = parsed;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[{PreviewId}] Snapshot JSON could not be parsed as CatalogPreviewSnapshot.", previewId);
            throw new PlanPreviewSnapshotMalformedException(
                $"Plan preview '{previewId}' snapshot is malformed and cannot be parsed. " +
                "The stored JSON is structurally invalid.", ex);
        }

        // ── Step 7: Schema completeness ───────────────────────────────────────
        ValidateSnapshotSchema(previewId, snapshot);

        // ── Step 8: GenerationSource ──────────────────────────────────────────
        if (snapshot.GenerationSource != GenerationSource.Catalog)
        {
            throw new PlanPreviewGenerationSourceInvalidException(
                $"Plan preview '{previewId}' snapshot has GenerationSource='{snapshot.GenerationSource}', " +
                $"not '{GenerationSource.Catalog}'. Catalog confirmation requires a catalog-sourced snapshot. " +
                "This is a dispatch or persistence defect — it must not fall back to SQL confirmation.");
        }

        // ── Step 9: Integrity (hash verification) ────────────────────────────
        if (string.IsNullOrWhiteSpace(snapshot.ContentHash))
        {
            throw new PlanPreviewIntegrityFailedException(
                $"Plan preview '{previewId}' snapshot is missing its ContentHash. Integrity cannot be verified.");
        }

        bool hashValid;
        try
        {
            hashValid = CatalogPreviewSnapshotVerifier.Verify(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{PreviewId}] Unexpected exception during snapshot hash verification.", previewId);
            throw new PlanPreviewIntegrityFailedException(
                $"Plan preview '{previewId}' snapshot integrity verification encountered an unexpected error.");
        }

        if (!hashValid)
        {
            throw new PlanPreviewIntegrityFailedException(
                $"Plan preview '{previewId}' snapshot content hash does not match the stored hash. " +
                "The snapshot has been corrupted or tampered with after creation. " +
                "This preview cannot be confirmed.");
        }

        // ── Step 10: Idempotency ──────────────────────────────────────────────
        // If this preview was already confirmed by a prior request, return the
        // existing plan without creating anything. This is the idempotency anchor.
        if (preview.ConfirmedPlanId.HasValue)
        {
            var alreadyConfirmedPlan = await _context.TrainingPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == preview.ConfirmedPlanId.Value, ct);

            if (alreadyConfirmedPlan != null)
            {
                _logger.LogInformation(
                    "[{PreviewId}] Preview already confirmed — returning existing plan {PlanId} (idempotent).",
                    previewId, alreadyConfirmedPlan.Id);
                return new ConfirmPlanResponse
                {
                    PlanId = alreadyConfirmedPlan.Id,
                    Status = "active",
                    AlreadyActive = false  // the plan is new-to-this-user in the idempotent path
                };
            }

            // ConfirmedPlanId is set but the plan row is gone — data integrity issue.
            // Fall through and re-create? No. Throw a typed error.
            throw new CatalogConfirmationFailedException(
                $"Plan preview '{previewId}' has a ConfirmedPlanId set but the associated TrainingPlan " +
                $"({preview.ConfirmedPlanId.Value}) no longer exists. Data integrity error.");
        }

        // ── Step 11: Persistability guard (extended by Phase 4F.1) ────────────
        // A catalog preview must not be confirmed into an active TrainingPlan
        // unless its immutable snapshot contains the complete persistable
        // week/day schedule required by the existing active-plan read paths
        // (GetHomeAsync, GetCalendarAsync, GetActivePlanDetailsAsync).
        //
        // GeneratedPreviewPlanPayload is still always null for every real
        // production snapshot as of Phase 4F.1 (CatalogPreviewGenerator never
        // supplies one). Confirming a null-payload snapshot would create an
        // empty-shell active plan that:
        //   - shows "Week 1 of 0" on the home screen
        //   - returns an empty calendar
        //   - shows 0 weeks, 0 planned distance, EstimatedEndDate=MinValue on plan-details
        //   - consumes IX_TrainingPlans_InternalUserId_ActiveOnly, blocking future
        //     plan generation for the user until the empty plan is manually cancelled
        //
        // On any rejection below: no TrainingPlan, TrainingWeek, TrainingDay, or
        // PlanEvent is created; ConfirmedPlanId is left null; no SaveChangesAsync
        // is called.
        if (snapshot.GeneratedPreviewPlanPayload is null)
        {
            _logger.LogWarning(
                "[{PreviewId}] Catalog preview rejected: GeneratedPreviewPlanPayload is null. " +
                "Stage-to-week scheduling is not implemented. " +
                "Confirmation blocked — no TrainingPlan created, active-plan slot not consumed.",
                previewId);
            throw new CatalogPreviewNotPersistableException(
                $"Plan preview '{previewId}' cannot be confirmed: the snapshot does not contain " +
                "a complete persistable week/day schedule. Stage-to-week scheduling is not yet " +
                "implemented. GeneratedPreviewPlanPayload is null. " +
                "Confirm will be available once a future phase populates the schedule in the snapshot.");
        }

        // Backend Integration Phase 4F.1: a non-null payload exists. Reject
        // unsupported schedule schema versions before attempting structural
        // validation (Decision 10 — never silently migrate/reinterpret).
        if (snapshot.GeneratedPreviewPlanPayload.SchemaVersion != GeneratedCatalogPlanPayload.CurrentSchemaVersion)
        {
            throw new CatalogPreviewScheduleSchemaUnsupportedException(
                $"Plan preview '{previewId}' snapshot's schedule payload has SchemaVersion=" +
                $"{snapshot.GeneratedPreviewPlanPayload.SchemaVersion}, but this backend only supports " +
                $"SchemaVersion={GeneratedCatalogPlanPayload.CurrentSchemaVersion}. The schedule is not " +
                "silently migrated or reinterpreted.");
        }

        var scheduleValidation = _scheduleValidator.Validate(snapshot.GeneratedPreviewPlanPayload);
        if (!scheduleValidation.IsValid)
        {
            throw new CatalogPreviewScheduleInvalidException(
                $"Plan preview '{previewId}' snapshot's schedule payload failed structural validation: " +
                string.Join(", ", scheduleValidation.Errors) + ". The schedule is not repaired or partially accepted.");
        }

        // Backend Integration Phase 4F.1: the payload is structurally valid —
        // necessary, but not yet sufficient. Persisting a typed schedule into
        // TrainingWeek/TrainingDay rows is explicitly deferred to a later,
        // separately-scoped phase; this phase defines the contract and
        // validator only. Never falls through to a persist path below.
        throw new CatalogPreviewMaterializationNotImplementedException(
            $"Plan preview '{previewId}' snapshot's schedule payload is structurally valid, but this " +
            "backend does not yet implement persisting a generated catalog schedule into an active " +
            "TrainingPlan. This capability is deferred to a future phase.");

        // Steps 12–15 (atomic persist: TrainingPlan/TrainingWeek/TrainingDay/
        // PlanEvent creation + ConfirmedPlanId association) do not exist as
        // executable code in this phase. Backend Integration Phase 4F.1
        // removed the previous Phase 4E.2 placeholder implementation (and its
        // now-orphaned BuildPlan helper) because every code path above now
        // unconditionally throws before reaching them — keeping unreachable
        // code here would only produce a compiler warning and imply a
        // materialization capability that does not exist. A future phase that
        // implements real stage-to-week persistence will need to design this
        // block against the actual GeneratedCatalogPlanPayload → TrainingPlan/
        // TrainingWeek/TrainingDay mapping (see
        // PHASE4F_1_PERSISTABLE_CATALOG_SCHEDULE_CONTRACT.md's mapping
        // specification), not merely restore what was removed here.
    }

    /// <summary>
    /// Validates that all required snapshot fields are present and non-default.
    /// Throws <see cref="PlanPreviewSnapshotUnsupportedException"/> for any
    /// missing or clearly invalid required field.
    ///
    /// Required fields:
    /// <c>CandidateKey</c>, <c>CandidateVersion</c>,
    /// <c>CandidateStatusAtGenerationTime</c>, <c>GenerationSource</c>,
    /// <c>RouteReason</c>, <c>ContentHash</c>, <c>NormalizedInput</c>,
    /// <c>ResolverResults</c>, <c>ReferencedArtifacts</c>.
    ///
    /// Note: Does NOT re-validate resolver result semantics (no
    /// ApplyNotEvaluatedGovernancePolicy re-run — the stored results are
    /// persisted exactly as frozen during preview creation).
    /// </summary>
    private static void ValidateSnapshotSchema(Guid previewId, CatalogPreviewSnapshot snapshot)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(snapshot.CandidateKey))
            missing.Add(nameof(snapshot.CandidateKey));
        if (snapshot.CandidateVersion <= 0)
            missing.Add(nameof(snapshot.CandidateVersion));
        if (string.IsNullOrWhiteSpace(snapshot.CandidateStatusAtGenerationTime))
            missing.Add(nameof(snapshot.CandidateStatusAtGenerationTime));
        if (string.IsNullOrWhiteSpace(snapshot.GenerationSource))
            missing.Add(nameof(snapshot.GenerationSource));
        if (string.IsNullOrWhiteSpace(snapshot.RouteReason))
            missing.Add(nameof(snapshot.RouteReason));
        if (string.IsNullOrWhiteSpace(snapshot.ContentHash))
            missing.Add(nameof(snapshot.ContentHash));
        if (snapshot.NormalizedInput == null)
            missing.Add(nameof(snapshot.NormalizedInput));
        if (snapshot.ResolverResults == null)
            missing.Add(nameof(snapshot.ResolverResults));
        if (snapshot.ReferencedArtifacts == null)
            missing.Add(nameof(snapshot.ReferencedArtifacts));

        if (missing.Count > 0)
        {
            throw new PlanPreviewSnapshotUnsupportedException(
                $"Plan preview '{previewId}' snapshot is missing required fields: " +
                string.Join(", ", missing) + ". " +
                "The snapshot schema is incomplete or from an unsupported version.");
        }
    }

}
