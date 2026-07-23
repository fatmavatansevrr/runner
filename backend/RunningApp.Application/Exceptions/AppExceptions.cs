namespace RunningApp.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource (preview, plan, training day, decision, etc.)
/// does not exist for the current user. Mapped to HTTP 404 by the API's global
/// exception handler.
/// </summary>
public sealed class NotFoundAppException : Exception
{
    public NotFoundAppException(string message) : base(message) { }
}

/// <summary>
/// Thrown when an operation cannot proceed because of the current state of a
/// resource (e.g. an expired preview, an already-active plan). Mapped to
/// HTTP 409 by the API's global exception handler.
/// </summary>
public sealed class ConflictAppException : Exception
{
    public ConflictAppException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a protected endpoint is accessed without a valid Firebase ID
/// Token. Mapped to HTTP 401 by the API's global exception handler.
/// This is raised by <c>FirebaseIdentityProvider</c> when no verified identity
/// exists on the current request (missing Authorization header).
/// </summary>
public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}

/// <summary>
/// Thrown when no seeded plan template exactly matches a generate-preview
/// request's (GoalType, GoalDistance, Level, DaysPerWeek) combination.
/// Mapped to HTTP 404 by the API's global exception handler with error code
/// <c>PLAN_TEMPLATE_NOT_FOUND</c>. Deliberately replaces the previous silent
/// fallback-to-first-seeded-template behavior in
/// <c>PlaceholderPlanGenerationEngine</c> — an unsupported combination must
/// fail loudly rather than silently return an unrelated plan.
/// </summary>
public sealed class PlanTemplateNotAvailableException : Exception
{
    public PlanTemplateNotAvailableException(string message) : base(message) { }
}

/// <summary>
/// Thrown by <c>ICanonicalDistanceFamilyResolver</c> when a requested target
/// distance is invalid (zero/negative) or outside the supported marathon
/// range (0, 42.2] km. Mapped to HTTP 400 by the API's global exception
/// handler with error code <c>UNSUPPORTED_TARGET_DISTANCE</c>.
/// </summary>
public sealed class UnsupportedTargetDistanceException : Exception
{
    public UnsupportedTargetDistanceException(string message) : base(message) { }
}

/// <summary>
/// Thrown by <c>IPlanCatalogBundleLoader</c> when a Process A plan-catalog
/// candidate cannot be read-only loaded/parsed: missing file, invalid JSON,
/// a missing required field, or a candidate key/version that doesn't match
/// what the caller expected. Never thrown as a result of mutating anything —
/// the loader is strictly read-only.
/// </summary>
public sealed class PlanCatalogLoadException : Exception
{
    public PlanCatalogLoadException(string message) : base(message) { }
    public PlanCatalogLoadException(string message, Exception innerException) : base(message, innerException) { }
}

// ── Backend Integration Phase 4E.1: catalog preview routing errors ──────────
// None of these are ever caught and converted into a silent SQL fallback —
// once a request is routed to CATALOG, any failure below is the final
// outcome. See PHASE4E_1_CATALOG_PREVIEW_ROUTING_AND_IMMUTABLE_RESOLUTION_SNAPSHOT.md.

/// <summary>
/// Thrown when the catalog candidate selected for a pilot request is not
/// runtime-eligible for PUBLIC preview because its own
/// <c>metadata.status</c> is not <c>PUBLISHED</c> (e.g. still DRAFT or
/// VALIDATED). Mapped to HTTP 409 — the resource exists but is not yet in a
/// usable lifecycle state, mirroring <see cref="ConflictAppException"/>'s
/// semantics for "correct request, wrong current state," not "bad request."
/// A DRAFT/VALIDATED candidate may still be loaded directly by tests or an
/// explicit internal dry-run path that does not go through this check.
/// </summary>
public sealed class CatalogCandidateNotPublishedException : Exception
{
    public CatalogCandidateNotPublishedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the catalog candidate itself is PUBLISHED but one of its
/// directly-loaded dependency artifacts (master template, layout, level
/// modifier, rule pack) is not. Distinct from
/// <see cref="CatalogCandidateNotPublishedException"/> so callers/logs can
/// tell "the candidate itself isn't ready" apart from "the candidate is
/// ready but something it depends on isn't."
/// </summary>
public sealed class CatalogDependencyNotRuntimeEligibleException : Exception
{
    public CatalogDependencyNotRuntimeEligibleException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a request is routed to the catalog pilot flow but no usable
/// catalog candidate can be loaded at all (e.g. the underlying
/// <see cref="PlanCatalogLoadException"/> indicates a missing/malformed
/// source file — a repository-configuration problem, not a lifecycle-status
/// problem). Never converted into a legacy-SQL fallback.
/// </summary>
public sealed class CatalogPilotNotAvailableException : Exception
{
    public CatalogPilotNotAvailableException(string message) : base(message) { }
    public CatalogPilotNotAvailableException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a runtime-condition resolver's <see cref="RunningApp.Application.RuntimeCatalog.Resolvers.RuntimeConditionResolutionStatus.NotEvaluated"/>
/// result classifies (per <c>NotEvaluatedReasonClassifier</c>) as
/// <c>RequiredInputNotProvided</c> — evidence the catalog flow cannot
/// proceed without was missing from the request.
/// </summary>
public sealed class RuntimeConditionRequiredInputMissingException : Exception
{
    public RuntimeConditionRequiredInputMissingException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a resolver's NotEvaluated result classifies as
/// <c>Unsupported</c> — the resolver recognized the input combination but
/// has no approved rule to classify it (e.g. TIME_ADEQUACY_IN=INSUFFICIENT
/// feeding GOAL_FEASIBILITY_IN, or a PACE_SOURCE_IN value with no approved
/// feasibility method). Never silently treated as any particular outcome.
/// </summary>
public sealed class RuntimeConditionUnsupportedException : Exception
{
    public RuntimeConditionUnsupportedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a resolver's NotEvaluated result classifies as
/// <c>DependencyUnresolved</c> — a prior resolver's result was missing from
/// the pipeline entirely (should not happen through
/// <c>RuntimeConditionResolutionService.ResolveAllResults</c>, which always
/// supplies all three dependencies; indicates a wiring defect if it fires).
/// </summary>
public sealed class RuntimeConditionDependencyUnresolvedException : Exception
{
    public RuntimeConditionDependencyUnresolvedException(string message) : base(message) { }
}

/// <summary>
/// Thrown for a catalog-flow failure that is technical/configuration in
/// nature (e.g. a resolver produced an output value that failed registry
/// validation, or an unrecognized resolver output reached the orchestration
/// layer) — a generic, explicit "generation failed" signal that must never
/// be converted into a partially-generated plan or a legacy-SQL fallback.
/// </summary>
public sealed class PlanPreviewGenerationFailedException : Exception
{
    public PlanPreviewGenerationFailedException(string message) : base(message) { }
    public PlanPreviewGenerationFailedException(string message, Exception innerException) : base(message, innerException) { }
}

// ── Backend Integration Phase 4E.2: catalog confirm boundary errors ──────────
// All of the following are thrown only by CatalogPlanConfirmationService.
// None is ever caught and silently rerouted to SQL or to a new generation run.
// Each has a stable typed error code registered in GlobalExceptionHandler.

/// <summary>
/// Thrown when <see cref="CatalogPlanConfirmationService"/> cannot find the
/// plan preview referenced by the confirmation request. Distinct from the
/// legacy <see cref="NotFoundAppException"/> so callers can tell
/// "preview-not-found" apart from other 404 conditions.
/// Mapped to HTTP 404, error code PLAN_PREVIEW_NOT_FOUND.
/// </summary>
public sealed class PlanPreviewNotFoundException : Exception
{
    public PlanPreviewNotFoundException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the authenticated user is not the owner of the plan preview
/// being confirmed. The preview exists but belongs to a different user.
/// Mapped to HTTP 403, error code PLAN_PREVIEW_FORBIDDEN.
/// </summary>
public sealed class PlanPreviewForbiddenException : Exception
{
    public PlanPreviewForbiddenException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the plan preview has passed its <c>ExpiresAt</c> timestamp.
/// A typed alternative to the legacy generic ConflictAppException used in the
/// SQL confirm path, so catalog consumers can distinguish "preview exists but
/// is expired" from other conflict conditions.
/// Mapped to HTTP 409, error code PLAN_PREVIEW_EXPIRED.
/// </summary>
public sealed class PlanPreviewExpiredException : Exception
{
    public PlanPreviewExpiredException(string message) : base(message) { }
}

/// <summary>
/// Defined for the idempotency signal concept but currently UNUSED/dead code
/// — corrected by the Phase 4E.2 acceptance-finalization pass (documentation-only;
/// no behavior change). <see cref="CatalogPlanConfirmationService"/>'s actual
/// idempotency short-circuit (step 10 of <c>ConfirmAsync</c>) checks
/// <c>preview.ConfirmedPlanId.HasValue</c> inline and returns the existing plan
/// directly — it never throws or catches this exception type. A repository-wide
/// search finds no call site that constructs or catches it. Not registered in
/// GlobalExceptionHandler. Retained only in case a future phase wires it in;
/// see PHASE4E_2_CLAUDE_SAFETY_AUDIT.md §11/§16 item 2.
/// </summary>
public sealed class PlanPreviewAlreadyConfirmedException : Exception
{
    public Guid ConfirmedPlanId { get; }
    public PlanPreviewAlreadyConfirmedException(Guid confirmedPlanId, string message)
        : base(message)
    {
        ConfirmedPlanId = confirmedPlanId;
    }
}

/// <summary>
/// Thrown when <c>PlanPreview.IsInvalidated == true</c>: the preview has
/// been explicitly revoked by an emergency operator action and must not be
/// confirmed regardless of its expiry or integrity state.
/// Mapped to HTTP 409, error code PLAN_PREVIEW_INVALIDATED.
/// </summary>
public sealed class PlanPreviewInvalidatedException : Exception
{
    public PlanPreviewInvalidatedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the catalog confirmation service finds no snapshot stored for
/// a catalog-routed preview (e.g. <c>PreviewPayloadJson</c> is empty, null,
/// or contains an obviously non-snapshot value). Indicates a persistence gap
/// rather than a user error — the system stored a catalog preview without its
/// mandatory snapshot, which is a configuration/wiring defect.
/// Mapped to HTTP 422, error code PLAN_PREVIEW_SNAPSHOT_MISSING.
/// </summary>
public sealed class PlanPreviewSnapshotMissingException : Exception
{
    public PlanPreviewSnapshotMissingException(string message) : base(message) { }
}

/// <summary>
/// Thrown when <c>PreviewPayloadJson</c> cannot be deserialized as a
/// <c>CatalogPreviewSnapshot</c> — the JSON is present but structurally
/// invalid. The original parse exception is preserved as the inner exception
/// for server-side logging.
/// Mapped to HTTP 422, error code PLAN_PREVIEW_SNAPSHOT_MALFORMED. Corrected by
/// the Phase 4E.2 acceptance-finalization pass (documentation-only; no behavior
/// change): <see cref="RunningApp.Api.ErrorHandling.GlobalExceptionHandler"/>
/// only masks the exposed message for HTTP 500 responses, not 422 — this
/// exception's message IS exposed to the caller verbatim (it contains no
/// sensitive detail, only the preview ID and a generic "structurally invalid"
/// statement). See PHASE4E_2_CLAUDE_SAFETY_AUDIT.md §5/§16 item 3.
/// </summary>
public sealed class PlanPreviewSnapshotMalformedException : Exception
{
    public PlanPreviewSnapshotMalformedException(string message) : base(message) { }
    public PlanPreviewSnapshotMalformedException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a deserialized <c>CatalogPreviewSnapshot</c> is missing a
/// required field or carries an unrecognized schema version, making it
/// impossible to safely validate or persist the plan. Does not attempt to
/// repair or regenerate the snapshot.
/// Mapped to HTTP 422, error code PLAN_PREVIEW_SNAPSHOT_UNSUPPORTED.
/// </summary>
public sealed class PlanPreviewSnapshotUnsupportedException : Exception
{
    public PlanPreviewSnapshotUnsupportedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the SHA-256 content hash stored in the snapshot does not match
/// the hash recomputed from the snapshot's own hashable content. The snapshot
/// has been corrupted or tampered with after creation and must not be used to
/// generate a plan. Does not attempt to repair, normalize, or regenerate.
/// Mapped to HTTP 422, error code PLAN_PREVIEW_INTEGRITY_FAILED.
/// </summary>
public sealed class PlanPreviewIntegrityFailedException : Exception
{
    public PlanPreviewIntegrityFailedException(string message) : base(message) { }
}

/// <summary>
/// Phase 4F.9.2 — thrown when a stored snapshot's
/// <c>HashAlgorithmVersion</c> is not the current supported value. Fails
/// closed rather than falling back to a known order-dependent (and
/// therefore, once stored as PostgreSQL jsonb, guaranteed-to-mismatch) hash
/// algorithm. No real (non-test) snapshot was ever produced with an older
/// version — this feature has never been committed, published, or
/// activated.
/// </summary>
public sealed class PlanPreviewHashAlgorithmVersionUnsupportedException : Exception
{
    public PlanPreviewHashAlgorithmVersionUnsupportedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the <c>GenerationSource</c> field in a stored
/// <c>CatalogPreviewSnapshot</c> is not <c>"CATALOG"</c> — the snapshot was
/// not produced by the catalog pilot flow. A catalog-confirm attempt against
/// a non-catalog snapshot is a dispatch or persistence defect; it must never
/// silently fall back to the SQL confirm path.
/// Mapped to HTTP 422, error code PLAN_PREVIEW_GENERATION_SOURCE_INVALID.
/// </summary>
public sealed class PlanPreviewGenerationSourceInvalidException : Exception
{
    public PlanPreviewGenerationSourceInvalidException(string message) : base(message) { }
}

/// <summary>
/// Generic typed catch-all for unexpected technical failures that occur
/// during catalog confirmation after all structured validation has passed
/// (e.g. an unexpected persistence exception that is not a concurrency
/// conflict). Never thrown for normal validation-level rejections — those
/// each have their own specific typed exception. The original exception is
/// preserved as the inner exception for server-side logging.
/// Mapped to HTTP 500, error code CATALOG_CONFIRMATION_FAILED.
/// </summary>
public sealed class CatalogConfirmationFailedException : Exception
{
    public CatalogConfirmationFailedException(string message) : base(message) { }
    public CatalogConfirmationFailedException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown by <see cref="CatalogPlanConfirmationService"/> when a catalog
/// preview's immutable snapshot does not contain the complete persistable
/// week/day schedule required by the existing active-plan read paths.
///
/// In Phase 4E.1/4E.2 this is always the case:
/// <c>GeneratedPreviewPlanPayload</c> is permanently <c>null</c> and
/// <c>SelectedStageKeys</c> is always empty because stage-to-week
/// scheduling is not yet implemented. A snapshot in this state must
/// never be confirmed into an active <c>TrainingPlan</c> — doing so
/// would create an empty-shell plan that breaks every downstream
/// read path (home, calendar, plan-details) while consuming the user's
/// active-plan uniqueness slot.
///
/// On this exception: no <c>TrainingPlan</c>, <c>TrainingWeek</c>,
/// <c>TrainingDay</c>, or <c>PlanEvent</c> is created. The preview's
/// <c>ConfirmedPlanId</c> is left <c>null</c>. The active-plan slot
/// is not consumed.
///
/// Mapped to HTTP 422, error code CATALOG_PREVIEW_NOT_PERSISTABLE.
/// </summary>
public sealed class CatalogPreviewNotPersistableException : Exception
{
    public CatalogPreviewNotPersistableException(string message) : base(message) { }
}

// ── Backend Integration Phase 4F.1: typed catalog schedule contract boundary errors ──
// All three are thrown only when CatalogPlanConfirmationService encounters a
// NON-NULL GeneratedPreviewPlanPayload (CatalogPreviewNotPersistableException,
// above, remains the exception for the null-payload case, unchanged from Phase
// 4E.2). None of these three ever results in a persisted TrainingPlan/Week/Day
// in this phase -- Phase 4F.1 defines the schedule contract and its structural
// validator only; it does not implement stage-to-week materialization or
// enable successful catalog confirmation. See
// PHASE4F_1_PERSISTABLE_CATALOG_SCHEDULE_CONTRACT.md.

/// <summary>
/// Thrown when a non-null <c>GeneratedPreviewPlanPayload</c>'s
/// <c>SchemaVersion</c> does not match <see cref="RunningApp.Application.RuntimeCatalog.Schedule.GeneratedCatalogPlanPayload.CurrentSchemaVersion"/>
/// (Phase 4F.1 Decision 10). Never silently migrated, reinterpreted, or
/// treated as the current version.
/// Mapped to HTTP 422, error code CATALOG_PREVIEW_SCHEDULE_SCHEMA_UNSUPPORTED.
/// </summary>
public sealed class CatalogPreviewScheduleSchemaUnsupportedException : Exception
{
    public CatalogPreviewScheduleSchemaUnsupportedException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a non-null, schema-version-supported <c>GeneratedPreviewPlanPayload</c>
/// fails <see cref="RunningApp.Application.RuntimeCatalog.Schedule.IGeneratedCatalogPlanPayloadValidator"/>'s
/// structural/persistability validation (Phase 4F.1 Decision 3). Never
/// repaired, never partially accepted.
/// Mapped to HTTP 422, error code CATALOG_PREVIEW_SCHEDULE_INVALID.
/// </summary>
public sealed class CatalogPreviewScheduleInvalidException : Exception
{
    public CatalogPreviewScheduleInvalidException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a non-null <c>GeneratedPreviewPlanPayload</c> passes ALL
/// structural validation but confirm still cannot proceed, because Phase
/// 4F.1 deliberately does not implement persisting a typed schedule payload
/// into <c>TrainingWeek</c>/<c>TrainingDay</c> rows -- that capability is
/// explicitly deferred to a later, separately-scoped phase. A structurally
/// valid payload is necessary but not (yet) sufficient for a successful
/// confirmation. This exception exists so a caller can distinguish "your
/// schedule is malformed" (<see cref="CatalogPreviewScheduleInvalidException"/>)
/// from "your schedule is well-formed, but this backend cannot act on it yet."
/// Mapped to HTTP 422, error code CATALOG_PREVIEW_MATERIALIZATION_NOT_IMPLEMENTED.
/// </summary>
public sealed class CatalogPreviewMaterializationNotImplementedException : Exception
{
    public CatalogPreviewMaterializationNotImplementedException(string message) : base(message) { }
}

public sealed class CatalogPreviewOwnershipMismatchException : Exception
{
    public CatalogPreviewOwnershipMismatchException(string message) : base(message) { }
}

public sealed class CatalogPreviewAlreadyConfirmedException : Exception
{
    public Guid ConfirmedPlanId { get; }
    public CatalogPreviewAlreadyConfirmedException(Guid confirmedPlanId, string message) : base(message) => ConfirmedPlanId = confirmedPlanId;
}

public sealed class CatalogPreviewConfirmationConcurrencyException : Exception
{
    public CatalogPreviewConfirmationConcurrencyException(string message) : base(message) { }
    public CatalogPreviewConfirmationConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class CatalogPreviewPersistenceContractException : Exception
{
    public CatalogPreviewPersistenceContractException(string message) : base(message) { }
}

public sealed class CatalogPlanPersistenceFailedException : Exception
{
    public CatalogPlanPersistenceFailedException(string message) : base(message) { }
    public CatalogPlanPersistenceFailedException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class CatalogPersistedPlanValidationException : Exception
{
    public CatalogPersistedPlanValidationException(string message) : base(message) { }
}

public sealed class CatalogActivePlanConflictException : Exception
{
    public CatalogActivePlanConflictException(string message) : base(message) { }
}

public sealed class CatalogPrescriptionPersistenceUnsupportedException : Exception
{
    public CatalogPrescriptionPersistenceUnsupportedException(string message) : base(message) { }
}

// ── Long-horizon race fail-closed safety constraint ──────────────────────────
// Temporary: long-horizon preparation + race-core composition is not yet
// implemented. See RunningApp.Application.Common.RaceHorizonPolicy and
// PHASE4G_1_LONG_HORIZON_FAIL_CLOSED_SAFETY_CONSTRAINT.md.

/// <summary>
/// Thrown when a race request's available horizon (StartDate to RaceDate)
/// exceeds <see cref="RunningApp.Application.Common.RaceHorizonPolicy.MaximumSupportedStandaloneWeeks"/>.
/// The system fails closed rather than silently selecting a shorter
/// supported core cycle anchored at StartDate and leaving RaceDate
/// unreached — the exact regression this exception exists to prevent. Never
/// caught and converted into a legacy-SQL fallback, a clamped/stretched
/// core, or a truncated plan. No preview or plan row is persisted when this
/// is thrown.
/// Mapped to HTTP 422, error code PLAN_HORIZON_COMPOSITION_REQUIRED.
/// </summary>
public sealed class PlanHorizonCompositionRequiredException : Exception
{
    public PlanHorizonCompositionRequiredException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a race request's available horizon (StartDate to RaceDate)
/// falls within the nominal standalone core-cycle range
/// (<see cref="RunningApp.Application.Common.RaceHorizonPolicy.MinimumSupportedStandaloneWeeks"/>..<see cref="RunningApp.Application.Common.RaceHorizonPolicy.MaximumSupportedStandaloneWeeks"/>)
/// but is not the one exact length
/// (<see cref="RunningApp.Application.Common.RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks"/>)
/// currently proven to produce a race-date-aligned plan. Distinct from
/// <see cref="PlanHorizonCompositionRequiredException"/>: the request is
/// within the nominally-supported range (does not need preparation-block
/// composition), but the current catalog phase allocator is not yet
/// horizon-aware and always emits its fixed default allocation regardless
/// of the accepted cycle length — generating here would silently misalign
/// with RaceDate (verified live for both 8-week overshoot and 20-week
/// undershoot). Never caught and converted into a legacy-SQL fallback, a
/// stretched/trimmed/repeated core, or a truncated plan. No preview or plan
/// row is persisted when this is thrown.
/// Mapped to HTTP 422, error code PLAN_CORE_HORIZON_UNSUPPORTED.
/// </summary>
public sealed class PlanCoreHorizonUnsupportedException : Exception
{
    public PlanCoreHorizonUnsupportedException(string message) : base(message) { }
}

/// <summary>
/// Defensive invariant thrown when a generated race-specific dated schedule's
/// final session/plan-end date is not aligned to the request's RaceDate
/// (ends materially before the race, or after it). A backstop against any
/// future regression re-introducing the class of bug
/// <see cref="PlanHorizonCompositionRequiredException"/> fails closed for
/// upstream, in case a schedule ever reaches materialization with a mismatch
/// that earlier horizon validation did not catch. Never caught/repaired —
/// the malformed schedule is never persisted.
/// Mapped to HTTP 422, error code CATALOG_RACE_DATE_ALIGNMENT_INVALID.
/// </summary>
public sealed class CatalogRaceDateAlignmentInvalidException : Exception
{
    public CatalogRaceDateAlignmentInvalidException(string message) : base(message) { }
}
