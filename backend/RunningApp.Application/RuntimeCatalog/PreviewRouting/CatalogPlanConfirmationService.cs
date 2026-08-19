using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.Common;
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
    public const string CatalogConfirmationMaterializerVersion = "CATALOG_CONFIRMATION_MATERIALIZER_V1";
    private const int PrescriptionSchemaVersion = 1;

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
    private readonly CatalogPersistedPlanValidator _persistedPlanValidator = new();

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
    /// Mandatory confirmation flow. Backend Integration Phase 4F.9.1A
    /// corrected the step order so that idempotent replay of an
    /// already-confirmed preview is evaluated BEFORE expiration/invalidation
    /// checks that only make sense for a not-yet-confirmed preview:
    ///
    /// 1.  Load preview by stable identifier.
    /// 2.  Verify authenticated-user ownership (always first — a non-owner
    ///     must never receive an existing plan, confirmed or not).
    /// 3.  Idempotency: if already confirmed, return the existing plan
    ///     without re-validating expiry/invalidation/hash/schema. A preview
    ///     that was confirmed while valid and has since expired (or been
    ///     invalidated by an unrelated operator action) still returns the
    ///     existing plan — expiration/invalidation are concerns for
    ///     confirming a NEW plan, not for replaying an already-settled one.
    /// 4.  Verify preview has not expired (unconfirmed preview only).
    /// 5.  Verify preview has not been explicitly invalidated.
    /// 6.  Verify the immutable snapshot exists (PreviewPayloadJson non-empty).
    /// 7.  Parse the CatalogPreviewSnapshot from JSON.
    /// 8.  Verify snapshot schema/field completeness.
    /// 9.  Verify GenerationSource == "CATALOG".
    /// 10. Verify snapshot content integrity (SHA-256 hash match).
    /// 11. Persistability guard (extended by Backend Integration Phase 4F.1):
    ///     <list type="bullet">
    ///     <item>GeneratedPreviewPlanPayload is null: throws
    ///     <see cref="CatalogPreviewNotPersistableException"/>.</item>
    ///     <item>Non-null but SchemaVersion unsupported: throws
    ///     <see cref="CatalogPreviewScheduleSchemaUnsupportedException"/>.</item>
    ///     <item>Non-null, schema supported, but fails structural validation
    ///     (<see cref="IGeneratedCatalogPlanPayloadValidator"/>): throws
    ///     <see cref="CatalogPreviewScheduleInvalidException"/>.</item>
    ///     </list>
    ///     In every rejection case: no TrainingPlan, no TrainingWeek, no
    ///     TrainingDay, no PlanEvent is created; ConfirmedPlanId stays null;
    ///     the active-plan slot is not consumed.
    /// 12. Active-plan invariant pre-check (fast, friendly-error path) plus a
    ///     database-level unique partial index
    ///     (<c>IX_TrainingPlans_InternalUserId_ActiveOnly</c>, pre-existing
    ///     since the <c>MigrateToInternalUserIdFKs</c> migration) as the
    ///     actual concurrency-safe enforcement: a losing concurrent
    ///     confirmation is translated to <see cref="CatalogActivePlanConflictException"/>,
    ///     never a raw database error.
    /// 13. Persist TrainingPlan/TrainingWeek/TrainingDay/PlanEvent inside one
    ///     transaction and set ConfirmedPlanId atomically.
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
        // Checked before idempotency: a non-owner must never receive an
        // existing plan, confirmed or not.
        if (preview.InternalUserId != internalUserId)
        {
            // Return the same 403 whether the preview doesn't exist for the
            // user or belongs to another — don't leak existence to non-owners.
            throw new PlanPreviewForbiddenException(
                $"Plan preview '{previewId}' does not belong to the authenticated user.");
        }

        // ── Step 3: Idempotency (moved ahead of expiration/invalidation) ─────
        // If this preview was already confirmed by a prior request, return the
        // existing plan without creating anything and WITHOUT re-checking
        // expiry/invalidation/hash — an idempotent replay of a settled
        // confirmation must succeed even if the preview has since expired or
        // been invalidated for future (new) confirmation attempts.
        if (preview.ConfirmedPlanId.HasValue)
        {
            var alreadyConfirmedPlan = await _context.TrainingPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == preview.ConfirmedPlanId.Value, ct);

            if (alreadyConfirmedPlan == null)
            {
                // ConfirmedPlanId is set but the plan row is gone — data
                // integrity issue. Fail safely rather than re-creating.
                throw new CatalogConfirmationFailedException(
                    $"Plan preview '{previewId}' has a ConfirmedPlanId set but the associated TrainingPlan " +
                    $"({preview.ConfirmedPlanId.Value}) no longer exists. Data integrity error.");
            }

            if (alreadyConfirmedPlan.InternalUserId != internalUserId)
            {
                // Tampered or corrupted confirmed-preview linkage: the stored
                // ConfirmedPlanId points at a plan owned by a different user.
                // Never return another user's plan — fail safely instead.
                throw new CatalogConfirmationFailedException(
                    $"Plan preview '{previewId}' has a ConfirmedPlanId ({preview.ConfirmedPlanId.Value}) " +
                    "that does not belong to the authenticated user. Data integrity error.");
            }

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

        // ── Step 4: Expiry (unconfirmed preview only) ────────────────────────
        if (preview.ExpiresAt < DateTime.UtcNow)
        {
            throw new PlanPreviewExpiredException(
                $"Plan preview '{previewId}' expired at {preview.ExpiresAt:O}. Generate a new preview.");
        }

        // ── Step 5: Invalidation ──────────────────────────────────────────────
        if (preview.IsInvalidated == true)
        {
            throw new PlanPreviewInvalidatedException(
                $"Plan preview '{previewId}' has been explicitly invalidated and cannot be confirmed.");
        }

        // ── Step 6: Snapshot presence ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(preview.PreviewPayloadJson))
        {
            throw new PlanPreviewSnapshotMissingException(
                $"Plan preview '{previewId}' has no stored snapshot. " +
                "This indicates a persistence defect — the catalog preview was created without its mandatory snapshot.");
        }

        // ── Step 7: Parse snapshot ────────────────────────────────────────────
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

        // ── Step 8: Schema completeness ───────────────────────────────────────
        ValidateSnapshotSchema(previewId, snapshot);

        // ── Step 9: GenerationSource ──────────────────────────────────────────
        if (snapshot.GenerationSource != GenerationSource.Catalog)
        {
            throw new PlanPreviewGenerationSourceInvalidException(
                $"Plan preview '{previewId}' snapshot has GenerationSource='{snapshot.GenerationSource}', " +
                $"not '{GenerationSource.Catalog}'. Catalog confirmation requires a catalog-sourced snapshot. " +
                "This is a dispatch or persistence defect — it must not fall back to SQL confirmation.");
        }

        // ── Step 10: Integrity (hash verification) ───────────────────────────
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
        catch (PlanPreviewHashAlgorithmVersionUnsupportedException)
        {
            // Fail closed with its own distinct, typed result — must not be
            // rewrapped as a generic integrity failure (see Phase 4F.9.2:
            // there is no legacy hash algorithm to fall back to).
            throw;
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

        // ── Step 11: Persistability guard (extended by Phase 4F.1) ────────────
        // A catalog preview must not be confirmed into an active TrainingPlan
        // unless its immutable snapshot contains the complete persistable
        // week/day schedule required by the existing active-plan read paths
        // (GetHomeAsync, GetCalendarAsync, GetActivePlanDetailsAsync).
        //
        // As of Phase 4F.5-4F.8, CatalogPreviewGenerator populates
        // GeneratedPreviewPlanPayload for every real production catalog
        // snapshot, so this branch is a defensive guard against a malformed
        // or corrupted stored snapshot rather than the normal path. Confirming
        // a null-payload snapshot would create an empty-shell active plan that:
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
                "Confirmation blocked — no TrainingPlan created, active-plan slot not consumed.",
                previewId);
            throw new CatalogPreviewNotPersistableException(
                $"Plan preview '{previewId}' cannot be confirmed: the snapshot does not contain " +
                "a complete persistable week/day schedule. GeneratedPreviewPlanPayload is null.");
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

        // Backend Integration Phase 4F.9.1A: this guard was re-verified against
        // the real production pipeline (CatalogVolumeAndLongRunPlanner ->
        // V1FourDaySessionVolumeAllocationPolicy) for TEN_K/4D/INTERMEDIATE,
        // 8-week cycle, RecentWeeklyVolumeKm=0 (explicit-zero -> 12km V1
        // starting-volume default). Weeks 1-7 are feasible (residual volumes
        // 8.0-10.5km, all above the 6.0km KEY_SESSION(3.0km)+2x
        // EASY_SUPPORT(1.5km) allocation floor). The taper week (week 8)
        // resolves to 8.5km planned weekly volume (12km week-7 volume x the
        // canonical 0.53 taper multiplier) with a 3.0km long run, leaving a
        // 5.5km residual — below the 6.0km floor — so
        // V1FourDaySessionVolumeAllocationPolicy.Allocate throws
        // CatalogSessionPrescriptionInfeasibleException("Week 8 residual
        // volume 5.5km cannot support V1 key/easy minimums.") every time.
        // This is a genuine pipeline infeasibility (not a false special-case
        // rejection), so the guard is kept as a fast, typed pre-check ahead
        // of full plan generation. See
        // Phase4F7D_ExplicitZeroEightWeekPath_RemainsBlockedByExisting4F7CAllocationMinimum
        // in Phase4F7BVolumeAndLongRunTests.cs for the exact numbers asserted
        // against the real planner/allocator classes.
        if (snapshot.GeneratedPreviewPlanPayload.PlannedWeekCount == 8 &&
            snapshot.NormalizedInput.RecentWeeklyVolumeKm == 0)
        {
            throw new CatalogPreviewPersistenceContractException(
                $"Plan preview '{previewId}' represents the known unsupported 8-week explicit-zero weekly-volume path " +
                "(taper-week residual volume falls below the V1 key/easy session-allocation floor) and cannot be confirmed.");
        }

        // ── Step 12: Active-plan invariant ────────────────────────────────────
        // Friendly-error fast path only. This read is NOT the concurrency
        // guarantee — two concurrent confirmations for the same user (from
        // two different previews) can both pass this check. The actual
        // concurrency-safe enforcement is the pre-existing database-level
        // unique partial index IX_TrainingPlans_InternalUserId_ActiveOnly
        // (added by the MigrateToInternalUserIdFKs migration), whose
        // violation is translated below into CatalogActivePlanConflictException.
        var existingActivePlan = await _context.TrainingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InternalUserId == internalUserId && p.Status == TrainingPlanStatus.Active, ct);

        if (existingActivePlan != null)
        {
            return new ConfirmPlanResponse
            {
                PlanId = existingActivePlan.Id,
                Status = "active",
                AlreadyActive = true
            };
        }

        // ── Step 13: Transactional persistence ───────────────────────────────
        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        try
        {
            var plan = BuildCatalogTrainingPlan(internalUserId, previewId, snapshot);
            _context.TrainingPlans.Add(plan);

            foreach (var weekPayload in snapshot.GeneratedPreviewPlanPayload.Weeks.OrderBy(w => w.WeekNumber))
            {
                var week = BuildCatalogTrainingWeek(plan, weekPayload);
                _context.TrainingWeeks.Add(week);

                foreach (var session in weekPayload.Sessions.OrderBy(s => s.SessionOrderInWeek))
                {
                    _context.TrainingDays.Add(BuildCatalogTrainingDay(plan, week, weekPayload, session));
                }
            }

            _context.PlanEvents.Add(new PlanEvent
            {
                Id = Guid.NewGuid(),
                InternalUserId = internalUserId,
                PlanId = plan.Id,
                EventType = "CatalogPlanConfirmed",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    preview_id = previewId,
                    candidate_key = snapshot.CandidateKey,
                    candidate_version = snapshot.CandidateVersion,
                    content_hash = snapshot.ContentHash,
                    materializer_version = CatalogConfirmationMaterializerVersion
                }, PersistenceJsonOptions),
                CreatedAt = plan.CreatedAt
            });

            preview.ConfirmedPlanId = plan.Id;
            await _context.SaveChangesAsync(ct);

            var validation = await _persistedPlanValidator.ValidateAsync(_context, previewId, snapshot, ct);
            if (!validation.IsValid)
            {
                throw new CatalogPersistedPlanValidationException(
                    $"Persisted catalog plan failed validation: {string.Join(", ", validation.Errors)}");
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(ct);
            }

            return new ConfirmPlanResponse
            {
                PlanId = plan.Id,
                Status = "active",
                AlreadyActive = false
            };
        }
        catch (DbUpdateException ex)
        {
            if (transaction != null) await transaction.RollbackAsync(ct);

            var violatedConstraint = ClassifyUniqueViolation(ex);
            switch (violatedConstraint)
            {
                case UniqueConstraintKind.SourcePreviewId:
                {
                    // This preview was concurrently confirmed by another
                    // request racing to persist for the exact same
                    // SourcePreviewId. Recover idempotently: reload and
                    // return the plan the winning transaction created.
                    _context.ChangeTracker.Clear();
                    var existingPlan = await _context.TrainingPlans
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.SourcePreviewId == previewId, ct);

                    if (existingPlan != null)
                    {
                        return new ConfirmPlanResponse
                        {
                            PlanId = existingPlan.Id,
                            Status = "active",
                            AlreadyActive = false
                        };
                    }

                    throw new CatalogPreviewConfirmationConcurrencyException(
                        $"Plan preview '{previewId}' hit the catalog confirmation uniqueness invariant, but no existing confirmed plan could be reloaded.",
                        ex);
                }
                case UniqueConstraintKind.ActivePlanPerUser:
                {
                    // Two distinct races can surface this exact same
                    // constraint violation, and they require two different
                    // recoveries:
                    //   (a) SAME preview raced against itself, and Postgres
                    //       happened to report the ActiveOnly violation
                    //       instead of the SourcePreviewId violation (which
                    //       constraint is reported first is a database
                    //       implementation detail, not something application
                    //       code controls, when a single losing insert
                    //       violates both indexes at once). This is still
                    //       fundamentally an idempotent replay of the SAME
                    //       previewId — reload by SourcePreviewId first and
                    //       return that plan, exactly like the
                    //       SourcePreviewId branch above.
                    //   (b) a DIFFERENT preview for the same user won the
                    //       one-active-plan-per-user slot. The loser here has
                    //       no plan of its own to reload — surface a typed
                    //       conflict rather than silently returning someone
                    //       else's plan.
                    _context.ChangeTracker.Clear();
                    var ownPlan = await _context.TrainingPlans
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.SourcePreviewId == previewId, ct);

                    if (ownPlan != null)
                    {
                        return new ConfirmPlanResponse
                        {
                            PlanId = ownPlan.Id,
                            Status = "active",
                            AlreadyActive = false
                        };
                    }

                    throw new CatalogActivePlanConflictException(
                        $"Plan preview '{previewId}' lost a concurrent race to confirm an active plan for this user: " +
                        "another confirmation already created the user's active plan.");
                }
                default:
                    throw new CatalogPlanPersistenceFailedException(
                        $"Plan preview '{previewId}' could not be persisted atomically.", ex);
            }
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken ct)
    {
        if (string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            return null;

        return await _context.Database.BeginTransactionAsync(ct);
    }

    private static TrainingPlan BuildCatalogTrainingPlan(Guid internalUserId, Guid previewId, CatalogPreviewSnapshot snapshot)
    {
        var payload = snapshot.GeneratedPreviewPlanPayload!;
        var now = DateTime.UtcNow;
        return new TrainingPlan
        {
            Id = Guid.NewGuid(),
            InternalUserId = internalUserId,
            TemplateId = snapshot.CandidateKey,
            Status = TrainingPlanStatus.Active,
            GoalType = payload.GoalType,
            GoalDistance = snapshot.NormalizedInput.GoalDistance ?? GoalDistance.TenK,
            GoalDistanceKm = snapshot.NormalizedInput.GoalDistanceKm,
            Level = snapshot.NormalizedInput.Level ?? RunningBackground.Intermediate,
            DaysPerWeek = payload.DaysPerWeek,
            Unit = DistanceUnit.Km,
            RaceDate = snapshot.NormalizedInput.RaceDate,
            RaceName = snapshot.NormalizedInput.RaceName,
            TargetFinishTimeSeconds = snapshot.NormalizedInput.TargetFinishTimeSeconds,
            PreferredDays = RunningDay.NormalizeList(WeekdayCsv.ToCsv(snapshot.NormalizedInput.PreferredDays)),
            WeeklyAvailability = snapshot.NormalizedInput.WeeklyAvailability,
            PreferredPace = snapshot.NormalizedInput.PreferredPace,
            LongRunDay = WeekdayCsv.ToCsv(snapshot.NormalizedInput.LongRunDay),
            StartedAt = AsUtcDateTime(payload.StartDate),
            EstimatedEndDate = AsUtcDateTime(payload.EndDate),
            CreatedAt = now,
            UpdatedAt = now,
            GenerationSource = GenerationSource.Catalog,
            SourcePreviewId = previewId,
            CatalogPreviewContentHash = snapshot.ContentHash,
            CatalogMaterializerVersion = payload.Provenance.MaterializerVersion ?? CatalogConfirmationMaterializerVersion,
            CatalogDependencyVersionsJson = JsonSerializer.Serialize(payload.DependencyVersions, PersistenceJsonOptions),
            CatalogConfirmedAtUtc = now,
            CatalogCandidateKey = snapshot.CandidateKey,
            CatalogCandidateVersion = snapshot.CandidateVersion,
            CatalogCandidateStatusAtGenerationTime = snapshot.CandidateStatusAtGenerationTime,
            CatalogTemplateKey = GetRef(snapshot, "masterTemplate")?.Key,
            CatalogTemplateVersion = GetRef(snapshot, "masterTemplate")?.Version,
            CatalogLayoutKey = GetRef(snapshot, "layout")?.Key,
            CatalogLayoutVersion = GetRef(snapshot, "layout")?.Version,
            CatalogLevelModifierKey = GetRef(snapshot, "levelModifier")?.Key,
            CatalogLevelModifierVersion = GetRef(snapshot, "levelModifier")?.Version,
            CatalogWorkoutProgressionKey = GetRef(snapshot, "workoutProgression")?.Key,
            CatalogWorkoutProgressionVersion = GetRef(snapshot, "workoutProgression")?.Version,
            CatalogProgressionModifierKey = GetRef(snapshot, "progressionModifier")?.Key,
            CatalogProgressionModifierVersion = GetRef(snapshot, "progressionModifier")?.Version,
            CatalogRulePackKey = GetRef(snapshot, "rulePack")?.Key,
            CatalogRulePackVersion = GetRef(snapshot, "rulePack")?.Version,
            CatalogPeakVolumeBandPolicyKey = GetRef(snapshot, "peakVolumeBandPolicy")?.Key,
            CatalogPeakVolumeBandPolicyVersion = GetRef(snapshot, "peakVolumeBandPolicy")?.Version,
            CatalogRuntimeConditionRegistryKey = GetRef(snapshot, "runtimeConditionValueRegistry")?.Key,
            CatalogRuntimeConditionRegistryVersion = GetRef(snapshot, "runtimeConditionValueRegistry")?.Version,
            CanonicalDistanceFamily = payload.CanonicalDistanceFamily,
            RequestedTargetDistanceKm = snapshot.NormalizedInput.GoalDistanceKm,
        };
    }

    private static TrainingWeek BuildCatalogTrainingWeek(TrainingPlan plan, GeneratedCatalogWeekPayload week) => new()
    {
        Id = Guid.NewGuid(),
        PlanId = plan.Id,
        Plan = plan,
        WeekNumber = week.WeekNumber,
        WeekType = MapWeekType(week.Provenance.SourcePhaseKey),
        PlannedVolumeKm = week.PlannedVolumeKm,
        ActualVolumeKm = 0,
        IsRecoveryWeek = string.Equals(week.Provenance.SourcePhaseKey, "TAPER", StringComparison.Ordinal),
        StartDate = AsUtcDateTime(week.StartDate),
        CreatedAt = plan.CreatedAt,
        CatalogPhaseKey = week.Provenance.SourcePhaseKey,
    };

    private static TrainingDay BuildCatalogTrainingDay(TrainingPlan plan, TrainingWeek week, GeneratedCatalogWeekPayload weekPayload, GeneratedCatalogTrainingDayPayload session)
    {
        var distance = session.TargetDistanceKm ?? session.EstimatedDistanceKm ?? 0;
        var duration = session.TargetDurationMinutes ?? session.EstimatedDurationMinutes ?? 0;
        var dayType = MapDayType(session.WorkoutType);
        var progressionStageKey = IsFineGrainedProgressionStage(session.Provenance.SourceStageKey, weekPayload.Provenance.SourcePhaseKey)
            ? session.Provenance.SourceStageKey
            : null;

        return new TrainingDay
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            WeekId = week.Id,
            Plan = plan,
            Week = week,
            Date = AsUtcDateTime(session.Date),
            DayType = dayType,
            Status = TrainingDayStatus.Planned,
            Title = $"{session.WorkoutType} {distance:0.#}k Run",
            Description = session.PacePrescription.EffortLabel ?? session.PacePrescription.DisplayText ?? "Follow the catalog prescription.",
            PlannedDistanceKm = distance,
            PlannedDurationMin = duration,
            PlannedPaceMinKm = session.PacePrescription.PaceType == GeneratedCatalogPaceType.Target
                ? session.PacePrescription.TargetSecondsPerKm / 60.0
                : null,
            Intensity = session.PlannedIntensity ?? session.PacePrescription.EffortLabel,
            IsLongRun = session.WorkoutType == GeneratedCatalogWorkoutType.LongRun,
            OriginalDate = AsUtcDateTime(session.Date),
            OriginalType = dayType,
            CanMarkComplete = true,
            CanMarkNotToday = true,
            Source = TrainingDaySource.Template,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.CreatedAt,
            CatalogWorkoutKey = session.Provenance.SourceWorkoutKey,
            CatalogWorkoutVersion = session.Provenance.SourceWorkoutVersion,
            // CatalogStageKey is intentionally NOT written here. It is a
            // legacy field from the SQL-generation path (Phase 3) with no
            // active reader in the catalog pipeline (CatalogPersistedPlanValidator
            // and all home/calendar/plan-details query paths use CatalogPhaseKey
            // and CatalogProgressionStageKey instead) — see the
            // LEGACY_READ_ONLY_NO_NEW_WRITES decision on TrainingDay.CatalogStageKey.
            CatalogPhaseKey = weekPayload.Provenance.SourcePhaseKey,
            CatalogProgressionStageKey = progressionStageKey,
            CatalogWorkoutDefinitionKey = session.Provenance.SourceWorkoutKey,
            CatalogWorkoutDefinitionVersion = session.Provenance.SourceWorkoutVersion,
            // Backend Integration Phase 10K-FREQ.6D.4D Split D: exact prescription-profile
            // lineage, copied verbatim from the already-resolved Split-C/mapper provenance --
            // never independently looked up here. Both null for Legacy sessions (every real
            // session today); both non-null together for a genuinely ProfileBacked session.
            CatalogPrescriptionProfileKey = session.Provenance.SourcePrescriptionProfileKey,
            CatalogPrescriptionProfileVersion = session.Provenance.SourcePrescriptionProfileVersion,
            CatalogSlotRole = session.Provenance.SourceLayoutSlotRole,
            CatalogStructuralRole = session.Provenance.SourceLayoutSlotRole,
            CatalogPrescriptionSchemaVersion = PrescriptionSchemaVersion,
            CatalogPrescriptionJson = JsonSerializer.Serialize(BuildPrescriptionSnapshot(session), PersistenceJsonOptions),
            GenerationSource = GenerationSource.Catalog,
        };
    }

    private static object BuildPrescriptionSnapshot(GeneratedCatalogTrainingDayPayload session) => new
    {
        schema_key = "CATALOG_SESSION_PRESCRIPTION_SNAPSHOT",
        schema_version = PrescriptionSchemaVersion,
        workout_type = session.WorkoutType.ToString(),
        prescription_basis = session.PrescriptionBasis.ToString(),
        target_distance_km = session.TargetDistanceKm,
        target_duration_minutes = session.TargetDurationMinutes,
        estimated_distance_km = session.EstimatedDistanceKm,
        estimated_duration_minutes = session.EstimatedDurationMinutes,
        planned_intensity = session.PlannedIntensity,
        pace = session.PacePrescription,
        segments = session.Segments.OrderBy(s => s.SegmentOrder).ToArray(),
        provenance = session.Provenance
    };

    private static readonly JsonSerializerOptions PersistenceJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    /// <summary>
    /// Phase 4F.9.2 — real-PostgreSQL relational validation discovered that
    /// <c>DateOnly.ToDateTime(TimeOnly.MinValue)</c> produces
    /// <see cref="DateTimeKind.Unspecified"/>, which Npgsql rejects for
    /// <c>timestamp with time zone</c> columns ("only UTC is supported").
    /// EF InMemory never enforces this, so every date-bearing column built
    /// from a <see cref="DateOnly"/> here (StartedAt, EstimatedEndDate,
    /// TrainingWeek.StartDate, TrainingDay.Date/OriginalDate) previously
    /// worked in every InMemory test but would fail on first real Postgres
    /// confirmation. Catalog dates carry no time-of-day or timezone meaning
    /// (they are calendar dates), so stamping them as UTC midnight is a safe,
    /// lossless representation — never a data change, only a Kind fix.
    /// </summary>
    private static DateTime AsUtcDateTime(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static PlanCatalogReference? GetRef(CatalogPreviewSnapshot snapshot, string key) =>
        snapshot.ReferencedArtifacts.TryGetValue(key, out var reference) ? reference : null;

    private static TrainingWeekType MapWeekType(string? phaseKey) => phaseKey switch
    {
        "FOUNDATION" => TrainingWeekType.Base,
        "BUILD" => TrainingWeekType.Build,
        "RACE_SPECIFIC" => TrainingWeekType.Peak,
        "TAPER" => TrainingWeekType.Taper,
        // Backend Integration Phase 4G.6C: the four Preparation Runway block
        // names (see PreparationRunwayPublicPreviewMapper.RunwayBlockPublicName)
        // carried through PreparationRunwayPersistablePlanMapper's
        // GeneratedCatalogWeekProvenance.SourcePhaseKey -- never mislabeled
        // as Foundation/Build/Taper.
        "CONSISTENCY" or "GENERAL_ENDURANCE" or "AEROBIC_STRENGTH" or "PRE_SPECIFIC_TRANSITION" => TrainingWeekType.PreparationRunway,
        _ => TrainingWeekType.Build
    };

    private static TrainingDayType MapDayType(GeneratedCatalogWorkoutType workoutType) => workoutType switch
    {
        GeneratedCatalogWorkoutType.Easy => TrainingDayType.Easy,
        GeneratedCatalogWorkoutType.Interval => TrainingDayType.Interval,
        GeneratedCatalogWorkoutType.Tempo => TrainingDayType.Tempo,
        GeneratedCatalogWorkoutType.LongRun => TrainingDayType.LongRun,
        GeneratedCatalogWorkoutType.RecoveryEasy => TrainingDayType.RecoveryEasy,
        _ => TrainingDayType.Easy
    };

    private static bool IsFineGrainedProgressionStage(string stageKey, string? phaseKey) =>
        !string.IsNullOrWhiteSpace(stageKey) &&
        !string.Equals(stageKey, phaseKey, StringComparison.Ordinal);

    private enum UniqueConstraintKind
    {
        None,
        SourcePreviewId,
        ActivePlanPerUser
    }

    /// <summary>
    /// Distinguishes which unique-index violation caused a DbUpdateException,
    /// by SQLSTATE 23505 + constraint name (reflection-based, so this does not
    /// take a hard Npgsql dependency). Two distinct constraints are
    /// recognized because they represent two distinct concurrency races with
    /// two distinct correct recoveries:
    /// <list type="bullet">
    /// <item><c>IX_TrainingPlans_SourcePreviewId</c> — the SAME preview was
    /// confirmed twice concurrently. Correct recovery: reload and return the
    /// plan the winner created (idempotent).</item>
    /// <item><c>IX_TrainingPlans_InternalUserId_ActiveOnly</c> — DIFFERENT
    /// previews for the SAME user raced to create the one-active-plan slot.
    /// Correct recovery: the loser has no plan of its own; surface a typed
    /// <see cref="CatalogActivePlanConflictException"/>, never a raw 23505.</item>
    /// </list>
    /// Not every 23505 is treated as idempotent confirmation — only the
    /// SourcePreviewId constraint name qualifies for that recovery path.
    /// </summary>
    private static UniqueConstraintKind ClassifyUniqueViolation(DbUpdateException ex)
    {
        var current = ex.InnerException;
        while (current != null)
        {
            var type = current.GetType();
            var sqlState = type.GetProperty("SqlState")?.GetValue(current)?.ToString();
            var constraintName = type.GetProperty("ConstraintName")?.GetValue(current)?.ToString();
            if (sqlState == "23505")
            {
                if (string.Equals(constraintName, "IX_TrainingPlans_SourcePreviewId", StringComparison.Ordinal))
                {
                    return UniqueConstraintKind.SourcePreviewId;
                }

                if (string.Equals(constraintName, "IX_TrainingPlans_InternalUserId_ActiveOnly", StringComparison.Ordinal))
                {
                    return UniqueConstraintKind.ActivePlanPerUser;
                }
            }

            current = current.InnerException;
        }

        return UniqueConstraintKind.None;
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
