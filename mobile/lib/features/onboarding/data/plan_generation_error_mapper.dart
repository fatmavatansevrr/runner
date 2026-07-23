import '../../../core/network/api_exception.dart';

/// Backend error code for a race request whose available horizon
/// (StartDate to RaceDate) exceeds the currently supported standalone
/// race-cycle maximum. Long-horizon preparation + race-core composition is
/// not yet implemented — this is a temporary safety constraint, not a
/// request-shape validation error.
const String planHorizonCompositionRequiredErrorCode = 'PLAN_HORIZON_COMPOSITION_REQUIRED';

/// Backend error code for a race request whose horizon falls within the
/// nominal 8-14 week standalone core range but is not the one exact length
/// (12 weeks) currently proven to produce a race-date-aligned plan. Distinct
/// from [planHorizonCompositionRequiredErrorCode]: this request does not
/// need preparation-block composition, but this exact core length is not
/// yet implemented safely. Temporary safety constraint, not a request-shape
/// validation error.
const String planCoreHorizonUnsupportedErrorCode = 'PLAN_CORE_HORIZON_UNSUPPORTED';

/// Maps a plan-generation failure to a safe, non-technical message for
/// display. Never retries, never mutates onboarding state, never truncates
/// or resubmits the request — the caller decides what to do next (typically:
/// let the user go back and change StartDate or race date).
String planGenerationUserSafeMessage(Object error) {
  if (error is ApiException && error.errorCode == planHorizonCompositionRequiredErrorCode) {
    return 'This plan has more preparation time than the current race-training '
        'format supports. Long-term preparation plans are coming next.';
  }
  if (error is ApiException && error.errorCode == planCoreHorizonUnsupportedErrorCode) {
    return 'This race date creates a shorter or longer race-training block than '
        'the current plan format supports. For now, choose dates that allow a '
        '12-week plan.';
  }
  if (error is ApiException) {
    return error.message;
  }
  return 'Something went wrong. Please try again.';
}
