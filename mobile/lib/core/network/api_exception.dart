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
  });

  final String message;
  final String? errorCode;
  final String? correlationId;
  final int? statusCode;

  @override
  String toString() => message;
}
