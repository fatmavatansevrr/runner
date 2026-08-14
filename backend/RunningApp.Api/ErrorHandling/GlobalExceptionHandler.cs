using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RunningApp.Api.Logging;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
// UnauthorizedAppException, NotFoundAppException, ConflictAppException, PlanTemplateNotAvailableException are all in RunningApp.Application.Exceptions

namespace RunningApp.Api.ErrorHandling;

/// <summary>
/// Maps known application exceptions to a standardized JSON error envelope
/// (see <see cref="ApiErrorResponse"/>) instead of letting unhandled
/// exceptions surface as raw 500s with stack traces.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, errorCode) = exception switch
        {
            // Phase 4L.6B.2: raised by the PostgreSQL rollback-compatibility-guard
            // trigger (fn_guard_rolling_plan_mutation) when a mutation reaches a
            // RollingLongHorizon TrainingPlans row while
            // RollbackCompatibilityMode.Enabled is true. Reachable from the
            // CURRENT application only as defensive hygiene -- during a real
            // rollback incident the mutation is issued by committed HEAD, which
            // has no knowledge of this pattern and falls through to its own
            // generic 500 handling, which is an accepted, non-destructive outcome.
            DbUpdateException { InnerException: PostgresException { SqlState: "LH001" } }
                => (StatusCodes.Status409Conflict, "ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED"),
            UnauthorizedAppException          => (StatusCodes.Status401Unauthorized,  "UNAUTHORIZED"),
            PlanTemplateNotAvailableException => (StatusCodes.Status404NotFound,      "PLAN_TEMPLATE_NOT_FOUND"),
            NotFoundAppException              => (StatusCodes.Status404NotFound,      "NOT_FOUND"),
            ConflictAppException              => (StatusCodes.Status409Conflict,      "CONFLICT"),
            UnsupportedTargetDistanceException =>(StatusCodes.Status400BadRequest,   "UNSUPPORTED_TARGET_DISTANCE"),
            // Backend Integration Phase 4E.1: catalog preview routing errors.
            // None of these are caught upstream and converted into a legacy-SQL
            // fallback -- they are the final outcome once a request is routed
            // to the catalog pilot flow.
            CatalogCandidateNotPublishedException      => (StatusCodes.Status409Conflict,   "CATALOG_CANDIDATE_NOT_PUBLISHED"),
            CatalogDependencyNotRuntimeEligibleException => (StatusCodes.Status409Conflict, "CATALOG_DEPENDENCY_NOT_RUNTIME_ELIGIBLE"),
            CatalogPilotNotAvailableException           => (StatusCodes.Status404NotFound,  "CATALOG_PILOT_NOT_AVAILABLE"),
            RuntimeConditionRequiredInputMissingException => (StatusCodes.Status400BadRequest, "RUNTIME_CONDITION_REQUIRED_INPUT_MISSING"),
            RuntimeConditionUnsupportedException        => (StatusCodes.Status422UnprocessableEntity, "RUNTIME_CONDITION_UNSUPPORTED"),
            RuntimeConditionDependencyUnresolvedException => (StatusCodes.Status500InternalServerError, "RUNTIME_CONDITION_DEPENDENCY_UNRESOLVED"),
            PlanProductIneligibleException productIneligible => (StatusCodes.Status422UnprocessableEntity, productIneligible.Reason),
            PlanPreviewGenerationFailedException        => (StatusCodes.Status500InternalServerError, "PLAN_PREVIEW_GENERATION_FAILED"),
            // Backend Integration Phase 4E.2: catalog confirm boundary errors.
            // None of these are caught and rerouted to SQL, regeneration, or a
            // partial-plan response. The catalog confirm path is final on every
            // typed-exception path below.
            PlanPreviewNotFoundException                => (StatusCodes.Status404NotFound,              "PLAN_PREVIEW_NOT_FOUND"),
            PlanPreviewForbiddenException               => (StatusCodes.Status403Forbidden,             "PLAN_PREVIEW_FORBIDDEN"),
            PlanPreviewExpiredException                 => (StatusCodes.Status409Conflict,              "PLAN_PREVIEW_EXPIRED"),
            PlanPreviewInvalidatedException             => (StatusCodes.Status409Conflict,              "PLAN_PREVIEW_INVALIDATED"),
            PlanPreviewSnapshotMissingException         => (StatusCodes.Status422UnprocessableEntity,   "PLAN_PREVIEW_SNAPSHOT_MISSING"),
            PlanPreviewSnapshotMalformedException       => (StatusCodes.Status422UnprocessableEntity,   "PLAN_PREVIEW_SNAPSHOT_MALFORMED"),
            PlanPreviewSnapshotUnsupportedException     => (StatusCodes.Status422UnprocessableEntity,   "PLAN_PREVIEW_SNAPSHOT_UNSUPPORTED"),
            PlanPreviewIntegrityFailedException         => (StatusCodes.Status422UnprocessableEntity,   "PLAN_PREVIEW_INTEGRITY_FAILED"),
            PlanPreviewHashAlgorithmVersionUnsupportedException => (StatusCodes.Status422UnprocessableEntity, "PLAN_PREVIEW_HASH_ALGORITHM_VERSION_UNSUPPORTED"),
            PlanPreviewGenerationSourceInvalidException => (StatusCodes.Status422UnprocessableEntity,   "PLAN_PREVIEW_GENERATION_SOURCE_INVALID"),
            CatalogPreviewNotPersistableException       => (StatusCodes.Status422UnprocessableEntity,   "CATALOG_PREVIEW_NOT_PERSISTABLE"),
            CatalogConfirmationFailedException          => (StatusCodes.Status500InternalServerError,   "CATALOG_CONFIRMATION_FAILED"),
            // Backend Integration Phase 4F.1: typed catalog schedule contract
            // boundary errors. Only reachable for a NON-NULL GeneratedPreviewPlanPayload
            // -- none of these ever results in a persisted plan; see
            // PHASE4F_1_PERSISTABLE_CATALOG_SCHEDULE_CONTRACT.md.
            CatalogPreviewScheduleSchemaUnsupportedException => (StatusCodes.Status422UnprocessableEntity, "CATALOG_PREVIEW_SCHEDULE_SCHEMA_UNSUPPORTED"),
            CatalogPreviewScheduleInvalidException            => (StatusCodes.Status422UnprocessableEntity, "CATALOG_PREVIEW_SCHEDULE_INVALID"),
            CatalogPreviewMaterializationNotImplementedException => (StatusCodes.Status422UnprocessableEntity, "CATALOG_PREVIEW_MATERIALIZATION_NOT_IMPLEMENTED"),
            CatalogLivePilotNotPublishedException            => (StatusCodes.Status409Conflict, "CATALOG_LIVE_PILOT_NOT_PUBLISHED"),
            CatalogLivePilotActivationDisabledException      => (StatusCodes.Status409Conflict, "CATALOG_LIVE_PILOT_ACTIVATION_DISABLED"),
            CatalogLivePilotRequestUnsupportedException      => (StatusCodes.Status422UnprocessableEntity, "CATALOG_LIVE_PILOT_REQUEST_UNSUPPORTED"),
            CatalogLivePilotGenerationInfeasibleException    => (StatusCodes.Status422UnprocessableEntity, "CATALOG_LIVE_PILOT_GENERATION_INFEASIBLE"),
            CatalogLiveFallbackNotPermittedException         => (StatusCodes.Status422UnprocessableEntity, "CATALOG_LIVE_FALLBACK_NOT_PERMITTED"),
            CatalogLiveRouteDecisionInvalidException         => (StatusCodes.Status500InternalServerError, "CATALOG_LIVE_ROUTE_DECISION_INVALID"),
            CatalogPreviewOwnershipMismatchException         => (StatusCodes.Status403Forbidden, "CATALOG_PREVIEW_OWNERSHIP_MISMATCH"),
            CatalogPreviewAlreadyConfirmedException          => (StatusCodes.Status409Conflict, "CATALOG_PREVIEW_ALREADY_CONFIRMED"),
            CatalogPreviewConfirmationConcurrencyException   => (StatusCodes.Status409Conflict, "CATALOG_PREVIEW_CONFIRMATION_CONCURRENCY"),
            CatalogPreviewPersistenceContractException       => (StatusCodes.Status422UnprocessableEntity, "CATALOG_PREVIEW_PERSISTENCE_CONTRACT"),
            CatalogPlanPersistenceFailedException            => (StatusCodes.Status500InternalServerError, "CATALOG_PLAN_PERSISTENCE_FAILED"),
            CatalogPersistedPlanValidationException          => (StatusCodes.Status500InternalServerError, "CATALOG_PERSISTED_PLAN_VALIDATION_FAILED"),
            CatalogActivePlanConflictException               => (StatusCodes.Status409Conflict, "CATALOG_ACTIVE_PLAN_CONFLICT"),
            CatalogPrescriptionPersistenceUnsupportedException => (StatusCodes.Status422UnprocessableEntity, "CATALOG_PRESCRIPTION_PERSISTENCE_UNSUPPORTED"),
            // Long-horizon race fail-closed safety constraint (temporary —
            // see RaceHorizonPolicy). Never a 400: the request is
            // structurally valid, just not yet composable.
            PlanHorizonCompositionRequiredException      => (StatusCodes.Status422UnprocessableEntity, "PLAN_HORIZON_COMPOSITION_REQUIRED"),
            PlanCoreHorizonUnsupportedException           => (StatusCodes.Status422UnprocessableEntity, "PLAN_CORE_HORIZON_UNSUPPORTED"),
            CatalogRaceDateAlignmentInvalidException     => (StatusCodes.Status422UnprocessableEntity, "CATALOG_RACE_DATE_ALIGNMENT_INVALID"),
            // Backend Integration Phase 4G.6B: scoped 15-20 week Preparation
            // Runway public preview. None of these fall back to a Core-only
            // plan or partial preview -- every path is final.
            PreparationRunwayPreviewNotEnabledException      => (StatusCodes.Status422UnprocessableEntity, "PREPARATION_RUNWAY_PREVIEW_NOT_ENABLED"),
            PreparationRunwayPreviewGenerationFailedException => (StatusCodes.Status422UnprocessableEntity, "PREPARATION_RUNWAY_PREVIEW_GENERATION_FAILED"),
            // Phase 4L.3: dedicated Long-Horizon public preview and confirmation.
            // Ownership mismatch intentionally shares the not-found mapping so
            // a caller cannot probe another user's preview identifiers.
            LongHorizonPreviewNotFoundException       => (StatusCodes.Status404NotFound, "LONG_HORIZON_PREVIEW_NOT_FOUND"),
            LongHorizonPreviewExpiredException        => (StatusCodes.Status410Gone, "LONG_HORIZON_PREVIEW_EXPIRED"),
            LongHorizonPreviewStaleException          => (StatusCodes.Status409Conflict, "LONG_HORIZON_PREVIEW_STALE"),
            LongHorizonPreviewCorruptException        => (StatusCodes.Status409Conflict, "LONG_HORIZON_PREVIEW_STALE"),
            LongHorizonActivePlanConflictException    => (StatusCodes.Status409Conflict, "LONG_HORIZON_ACTIVE_PLAN_CONFLICT"),
            LongHorizonPilotUnsupportedException      => (StatusCodes.Status422UnprocessableEntity, "LONG_HORIZON_PILOT_UNSUPPORTED"),
            LongHorizonPlanHorizonExceededException   => (StatusCodes.Status422UnprocessableEntity, "PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW"),
            LongHorizonInitializationInfeasibleException => (StatusCodes.Status422UnprocessableEntity, "LONG_HORIZON_INITIALIZATION_INFEASIBLE"),
            // Phase 4L.6C: generation-only kill switch. Never thrown by any
            // read, activation, retry, completion, NotToday, or cancellation
            // path -- see LongHorizonGenerationOptions's own doc comment.
            LongHorizonGenerationTemporarilyDisabledException => (StatusCodes.Status503ServiceUnavailable, "LONG_HORIZON_GENERATION_TEMPORARILY_DISABLED"),
            LongHorizonRollingReadSurfaceNotAvailableException => (StatusCodes.Status409Conflict, "LONG_HORIZON_READ_SURFACE_NOT_YET_SUPPORTED"),
            LongHorizonReadStateNotFoundException => (StatusCodes.Status404NotFound, "LONG_HORIZON_ACTIVE_PLAN_NOT_FOUND"),
            LongHorizonReadStateCorruptException => (StatusCodes.Status409Conflict, "LONG_HORIZON_READ_STATE_CORRUPT"),
            LongHorizonRollingSessionNotFoundException => (StatusCodes.Status404NotFound, "LONG_HORIZON_ROLLING_SESSION_NOT_FOUND"),
            LongHorizonRollingSessionNotExecutableException => (StatusCodes.Status409Conflict, "LONG_HORIZON_ROLLING_SESSION_NOT_EXECUTABLE"),
            LongHorizonRollingSessionCompletionConflictException => (StatusCodes.Status409Conflict, "LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT"),
            LongHorizonRollingSessionOutcomeConflictException => (StatusCodes.Status409Conflict, "LONG_HORIZON_ROLLING_SESSION_OUTCOME_CONFLICT"),
            LongHorizonRollingMutationConcurrencyConflictException => (StatusCodes.Status409Conflict, "LONG_HORIZON_ROLLING_MUTATION_CONCURRENCY_CONFLICT"),
            LongHorizonRollingMutationVersionUnsupportedException => (StatusCodes.Status422UnprocessableEntity, "LONG_HORIZON_ROLLING_MUTATION_VERSION_UNSUPPORTED"),
            // Phase 4L.4A: explicit next-window activation and public continuation.
            LongHorizonContinuationVersionUnsupportedException => (StatusCodes.Status422UnprocessableEntity, "LONG_HORIZON_CONTINUATION_VERSION_UNSUPPORTED"),
            LongHorizonContinuationInProgressException        => (StatusCodes.Status409Conflict, "LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS"),
            LongHorizonContinuationReassessmentRequiredException => (StatusCodes.Status422UnprocessableEntity, "LONG_HORIZON_REASSESSMENT_REQUIRED"),
            LongHorizonContinuationBlockedException           => (StatusCodes.Status409Conflict, "LONG_HORIZON_CONTINUATION_BLOCKED"),
            LongHorizonContinuationRetryRequiredException     => (StatusCodes.Status409Conflict, "LONG_HORIZON_RETRY_REQUIRED"),
            LongHorizonContinuationConcurrencyConflictException => (StatusCodes.Status409Conflict, "LONG_HORIZON_CONTINUATION_CONCURRENCY_CONFLICT"),
            // Phase 4L.4B: public Blocked -> Pending retry restoration.
            LongHorizonNoBlockedBoundaryException              => (StatusCodes.Status409Conflict, "LONG_HORIZON_NO_BLOCKED_BOUNDARY"),
            LongHorizonRetryNotEligibleException                => (StatusCodes.Status422UnprocessableEntity, "LONG_HORIZON_RETRY_NOT_ELIGIBLE"),
            // Phase 4L.4C: meaningful blocked-recovery classification.
            LongHorizonRegeneratePreviewRequiredException      => (StatusCodes.Status409Conflict, "LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED"),
            LongHorizonOperationalSupportRequiredException     => (StatusCodes.Status409Conflict, "LONG_HORIZON_OPERATIONAL_SUPPORT_REQUIRED"),
            // Phase 4M.3: live NotToday -> schedule-repair adaptation orchestration.
            LongHorizonAdaptationStaleTargetException          => (StatusCodes.Status409Conflict, "LONG_HORIZON_ADAPTATION_STALE_TARGET"),
            LongHorizonAdaptationStaleTriggerException         => (StatusCodes.Status409Conflict, "LONG_HORIZON_ADAPTATION_STALE_TRIGGER"),
            LongHorizonAdaptationConcurrencyConflictException  => (StatusCodes.Status409Conflict, "LONG_HORIZON_ADAPTATION_CONCURRENCY_CONFLICT"),
            LongHorizonAdaptationIntegrityViolationException   => (StatusCodes.Status500InternalServerError, "LONG_HORIZON_ADAPTATION_INTEGRITY_VIOLATION"),
            LongHorizonRollingSessionSupersededException       => (StatusCodes.Status409Conflict, "LONG_HORIZON_ROLLING_SESSION_SUPERSEDED"),
            ArgumentException                 => (StatusCodes.Status400BadRequest,    "VALIDATION_ERROR"),
            _                                 => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR"),
        };

        var correlationId = CorrelationIdAccessor.GetOrCreate(httpContext);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "[{CorrelationId}] Unhandled exception while processing {Path}", correlationId, httpContext.Request.Path);
        }
        else if (errorCode == "ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED")
        {
            // Phase 4L.6C: structured operational event for Part 15
            // "Recovery: rollback compatibility mutation blocked" -- no
            // secret/payload, just the correlation ID and the block reason.
            _logger.LogWarning(
                "[{CorrelationId}] Rollback compatibility mutation blocked. Path={Path}",
                correlationId, httpContext.Request.Path);
        }

        var response = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = errorCode switch
            {
                "ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED" =>
                    "This plan can't be changed while the service is temporarily running in compatibility mode. Your plan data is unchanged.",
                _ when statusCode == StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
                _ => exception.Message,
            },
            CorrelationId = correlationId,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, ResponseJsonOptions, cancellationToken);

        return true;
    }
}
