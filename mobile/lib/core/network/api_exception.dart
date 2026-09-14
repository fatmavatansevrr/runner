/// A clean, user-safe error surfaced from the backend.
///
/// Wraps the backend's standardized error envelope:
/// `{ "errorCode": "...", "message": "...", "correlationId": "..." }`.
/// [toString] returns just [message], so existing call sites that do
/// `SnackBar(content: Text(e.toString()))` automatically show a safe,
/// human-readable message instead of a raw stack trace.
class ApiException implements Exception {
  const ApiException({
    required this.message,
    this.errorCode,
    this.correlationId,
    this.statusCode,
    this.recommendedStartDate,
  });

  final String message;
  final String? errorCode;
  final String? correlationId;
  final int? statusCode;

  /// HM.18 -- advisory-only field, present only alongside
  /// `PLAN_HORIZON_START_TOO_EARLY` (an ISO `yyyy-MM-dd` date string, exactly
  /// as the backend's `ApiErrorResponse.RecommendedStartDate` serializes it).
  /// Never used to auto-resubmit or mutate onboarding state -- display only.
  final String? recommendedStartDate;

  @override
  String toString() => message;
}
