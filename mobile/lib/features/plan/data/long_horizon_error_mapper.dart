import '../../../core/network/api_exception.dart';

/// Deterministic UI action a Long-Horizon error resolves to. The mapper
/// below decides this from the backend's own `errorCode` alone -- nothing
/// here re-derives or approximates backend recovery/checkpoint/activation
/// classification; every action here is a direct, 1:1 read of what the
/// backend already decided by returning that specific error code.
enum LongHorizonErrorAction {
  /// Show the message; no state refresh implied.
  showMessage,

  /// Re-fetch Home (`activeHomeResultProvider`) -- the action the user
  /// attempted lost a race with newer server state.
  refreshHome,

  /// Re-fetch the rolling session detail the user was viewing.
  refreshDetail,

  /// Re-fetch the current Calendar month.
  refreshCalendar,

  /// Offer the user an explicit "regenerate preview" path (never automatic).
  regeneratePreview,

  /// Offer the user the explicit cancel-then-create-new-plan flow (never
  /// automatic; the plan is never cancelled without a separate confirming
  /// tap the user makes after seeing this).
  cancelAndCreateNewPlan,

  /// Surface the backend's own retry-eligibility state (a "Retry" button
  /// only if the backend already said retry is eligible elsewhere -- this
  /// action alone never implies eligibility).
  retryGuidance,

  /// Existing app-wide session-expired handling (matches
  /// `ApiClient`'s own 401 handling; this mapper never invents a new
  /// auth flow).
  signInAgain,

  /// A safe, generic operational failure -- no backend detail shown.
  operationalSupport,

  /// Generic safe fallback for anything not in the explicit table below,
  /// including error codes the client has never seen.
  genericFailure,
}

/// The result of mapping one [ApiException] through
/// [LongHorizonUiErrorMapper.map]: a user-safe message plus the
/// deterministic action a screen should take. Never carries the backend's
/// raw error code or message text into anything shown to the user beyond
/// [userMessage] itself (which is always this mapper's own copy, not
/// `ApiException.message` passed through) for known codes; unknown codes
/// fall back to a generic message, never the raw backend string.
class LongHorizonUiError {
  const LongHorizonUiError({required this.userMessage, required this.action});
  final String userMessage;
  final LongHorizonErrorAction action;
}

/// Single centralized mapper for every Long-Horizon backend public error
/// code. Every code below is verified against the actual, exhaustive
/// `errorCode` table in
/// `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs`
/// (Phase 4L.5A) -- no code here is invented, and no code that mapper
/// actually returns for a Long-Horizon exception is missing here.
abstract final class LongHorizonUiErrorMapper {
  static LongHorizonUiError map(Object error) {
    if (error is! ApiException) {
      return const LongHorizonUiError(
        userMessage: 'Something went wrong. Please try again.',
        action: LongHorizonErrorAction.genericFailure,
      );
    }
    if (error.statusCode == 401) {
      return const LongHorizonUiError(
        userMessage: 'Your session has expired. Please sign in again.',
        action: LongHorizonErrorAction.signInAgain,
      );
    }

    switch (error.errorCode) {
      // ── Preview / confirmation ──────────────────────────────────────
      case 'LONG_HORIZON_PREVIEW_NOT_FOUND':
        return const LongHorizonUiError(
          userMessage:
              "We couldn't find that plan preview. Please generate a new one.",
          action: LongHorizonErrorAction.regeneratePreview,
        );
      case 'LONG_HORIZON_PREVIEW_EXPIRED':
        return const LongHorizonUiError(
          userMessage:
              'This plan preview has expired. Please generate a new one.',
          action: LongHorizonErrorAction.regeneratePreview,
        );
      case 'LONG_HORIZON_PREVIEW_STALE':
        return const LongHorizonUiError(
          userMessage:
              'This plan preview is out of date. Please generate a new one.',
          action: LongHorizonErrorAction.regeneratePreview,
        );
      case 'LONG_HORIZON_ACTIVE_PLAN_CONFLICT':
        return const LongHorizonUiError(
          userMessage:
              'You already have an active plan. Cancel it before starting a new one.',
          action: LongHorizonErrorAction.refreshHome,
        );
      case 'LONG_HORIZON_PILOT_UNSUPPORTED':
        return const LongHorizonUiError(
          userMessage: 'This plan configuration is not yet supported.',
          action: LongHorizonErrorAction.showMessage,
        );
      case 'PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW':
        return const LongHorizonUiError(
          userMessage:
              'This race date is outside the range of plan lengths currently supported.',
          action: LongHorizonErrorAction.showMessage,
        );
      case 'LONG_HORIZON_INITIALIZATION_INFEASIBLE':
        return const LongHorizonUiError(
          userMessage:
              "We couldn't build a plan for this configuration. Please adjust your details and try again.",
          action: LongHorizonErrorAction.showMessage,
        );

      // ── Reads ────────────────────────────────────────────────────────
      case 'LONG_HORIZON_READ_SURFACE_NOT_YET_SUPPORTED':
        return const LongHorizonUiError(
          userMessage: 'This view is not yet available for your plan.',
          action: LongHorizonErrorAction.showMessage,
        );
      case 'LONG_HORIZON_ACTIVE_PLAN_NOT_FOUND':
        return const LongHorizonUiError(
          userMessage: "We couldn't find an active plan.",
          action: LongHorizonErrorAction.refreshHome,
        );
      case 'LONG_HORIZON_READ_STATE_CORRUPT':
        return const LongHorizonUiError(
          userMessage:
              'Something went wrong loading your plan. Please try again shortly.',
          action: LongHorizonErrorAction.operationalSupport,
        );
      case 'LONG_HORIZON_ROLLING_SESSION_NOT_FOUND':
        return const LongHorizonUiError(
          userMessage: "We couldn't find that session.",
          action: LongHorizonErrorAction.refreshCalendar,
        );

      // ── Mutations (complete / not-today) ────────────────────────────
      case 'LONG_HORIZON_ROLLING_SESSION_NOT_EXECUTABLE':
        return const LongHorizonUiError(
          userMessage: 'This session is no longer available to update.',
          action: LongHorizonErrorAction.refreshDetail,
        );
      case 'LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT':
        return const LongHorizonUiError(
          userMessage:
              'This session was already updated elsewhere. Refreshing.',
          action: LongHorizonErrorAction.refreshDetail,
        );
      case 'LONG_HORIZON_ROLLING_SESSION_OUTCOME_CONFLICT':
        return const LongHorizonUiError(
          userMessage:
              'This session was already updated elsewhere. Refreshing.',
          action: LongHorizonErrorAction.refreshDetail,
        );
      case 'LONG_HORIZON_ROLLING_MUTATION_CONCURRENCY_CONFLICT':
        return const LongHorizonUiError(
          userMessage: 'Something changed while saving. Refreshing.',
          action: LongHorizonErrorAction.refreshDetail,
        );
      case 'LONG_HORIZON_ROLLING_MUTATION_VERSION_UNSUPPORTED':
        return const LongHorizonUiError(
          userMessage: 'Please update the app to continue.',
          action: LongHorizonErrorAction.showMessage,
        );

      // ── Continuation / activation ───────────────────────────────────
      case 'LONG_HORIZON_CONTINUATION_VERSION_UNSUPPORTED':
        return const LongHorizonUiError(
          userMessage: 'Please update the app to continue.',
          action: LongHorizonErrorAction.showMessage,
        );
      case 'LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS':
        return const LongHorizonUiError(
          userMessage: "Your current training block isn't finished yet.",
          action: LongHorizonErrorAction.refreshHome,
        );
      case 'LONG_HORIZON_REASSESSMENT_REQUIRED':
        return const LongHorizonUiError(
          userMessage: 'Your plan needs a quick check-in before continuing.',
          action: LongHorizonErrorAction.refreshHome,
        );
      case 'LONG_HORIZON_CONTINUATION_BLOCKED':
        return const LongHorizonUiError(
          userMessage: 'Your plan is currently paused and needs attention.',
          action: LongHorizonErrorAction.refreshHome,
        );
      case 'LONG_HORIZON_RETRY_REQUIRED':
        return const LongHorizonUiError(
          userMessage: 'Please retry to continue your plan.',
          action: LongHorizonErrorAction.retryGuidance,
        );
      case 'LONG_HORIZON_CONTINUATION_CONCURRENCY_CONFLICT':
        return const LongHorizonUiError(
          userMessage: 'Something changed while activating. Refreshing.',
          action: LongHorizonErrorAction.refreshHome,
        );

      // ── Retry ────────────────────────────────────────────────────────
      case 'LONG_HORIZON_NO_BLOCKED_BOUNDARY':
        return const LongHorizonUiError(
          userMessage: 'There is nothing to retry right now.',
          action: LongHorizonErrorAction.refreshHome,
        );
      case 'LONG_HORIZON_RETRY_NOT_ELIGIBLE':
        return const LongHorizonUiError(
          userMessage:
              "This can't be retried automatically. Please regenerate your plan preview instead.",
          action: LongHorizonErrorAction.regeneratePreview,
        );

      // ── Recovery classification ─────────────────────────────────────
      case 'LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED':
        return const LongHorizonUiError(
          userMessage:
              'Please cancel this plan and create a new one to continue.',
          action: LongHorizonErrorAction.cancelAndCreateNewPlan,
        );
      case 'LONG_HORIZON_OPERATIONAL_SUPPORT_REQUIRED':
        return const LongHorizonUiError(
          userMessage:
              'We need to look into this for you. Please contact support.',
          action: LongHorizonErrorAction.operationalSupport,
        );

      default:
        return const LongHorizonUiError(
          userMessage: 'Something went wrong. Please try again.',
          action: LongHorizonErrorAction.genericFailure,
        );
    }
  }
}
