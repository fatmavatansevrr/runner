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

// ── HM.18 -- new typed error codes for HALF_MARATHON's own Standard-Core V1 ──
// gate (backend `GlobalExceptionHandler`/`AppExceptions.cs`). Each is a
// stable, machine-readable code distinct from the others and from the
// TEN_K-era codes above -- never mapped to a generic message that would hide
// which specific condition occurred.

/// The request's horizon is shorter than HALF_MARATHON's own supported
/// standalone-core minimum (10 weeks).
const String planCoreHorizonTooShortErrorCode = 'PLAN_CORE_HORIZON_TOO_SHORT';

/// The request's horizon exceeds HALF_MARATHON's own supported standalone-core
/// maximum (16 weeks). May carry an advisory [ApiException.recommendedStartDate].
const String planHorizonStartTooEarlyErrorCode = 'PLAN_HORIZON_START_TOO_EARLY';

/// The requested HALF_MARATHON (Level, DaysPerWeek) combination is outside the
/// supported V1 frequency matrix (e.g. 2D/6D).
const String hmFrequencyNotSupportedErrorCode = 'HM_FREQUENCY_NOT_SUPPORTED_V1';

/// The requested product configuration is not eligible (e.g. HALF_MARATHON
/// Beginner x5D). Also used, with the same code, for other frozen
/// product-ineligibility decisions (readiness-insufficient variants included).
const String productIneligibleErrorCode = 'PRODUCT_INELIGIBLE';

/// A preferred-days/long-run-day combination could not be placed on the
/// generated calendar for this candidate.
const String preferredDayPlacementInfeasibleErrorCode = 'PREFERRED_DAY_PLACEMENT_INFEASIBLE';

/// Maps a plan-generation failure to a safe, non-technical message for
/// display. Never retries, never mutates onboarding state, never truncates
/// or resubmits the request — the caller decides what to do next (typically:
/// let the user go back and change StartDate, race date, level, or frequency).
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
  if (error is ApiException && error.errorCode == planCoreHorizonTooShortErrorCode) {
    return 'This race date is too close for a full training block. Choose a race '
        'date at least 10 weeks away, or an earlier start date.';
  }
  if (error is ApiException && error.errorCode == planHorizonStartTooEarlyErrorCode) {
    final recommended = error.recommendedStartDate;
    return recommended == null
        ? 'This race date is further away than the current plan format supports. '
            'Choose a later start date.'
        : 'This race date is further away than the current plan format supports. '
            'Try a start date on or after $recommended.';
  }
  if (error is ApiException && error.errorCode == hmFrequencyNotSupportedErrorCode) {
    return 'This training frequency is not yet available for Half Marathon at '
        'this level. Try a different number of days per week.';
  }
  if (error is ApiException && error.errorCode == productIneligibleErrorCode) {
    return 'This exact combination of level, frequency, and running history is '
        'not supported yet. Try adjusting your level or weekly frequency.';
  }
  if (error is ApiException && error.errorCode == preferredDayPlacementInfeasibleErrorCode) {
    return 'Your preferred training days can\'t be arranged for this plan. Try '
        'choosing different preferred days or a different long-run day.';
  }
  if (error is ApiException) {
    return error.message;
  }
  return 'Something went wrong. Please try again.';
}
