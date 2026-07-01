class AuthException implements Exception {
  const AuthException(this.message, {this.isCancelled = false});

  final String message;

  /// True when the user deliberately cancelled the sign-in flow (e.g. dismissed
  /// the Google account picker or Apple sheet). The UI should silently exit the
  /// loading state without showing an error SnackBar.
  final bool isCancelled;

  @override
  String toString() => message;
}
