/// Running Background V2 — canonical "Go with average" finish times.
///
/// Single source for the values `GoalTimePage` uses both to jump the time
/// wheel and to serialize `target_finish_time_seconds` when the user taps
/// "Go with average" instead of entering a custom time. These are the
/// product's only existing average-time values (previously inlined in
/// `goal_time_page.dart`) — centralized here so they're named, testable,
/// and can never drift between the button label and the value actually
/// sent to the backend.
class AverageFinishTimePolicy {
  const AverageFinishTimePolicy._();

  static const int fiveKSeconds = 28 * 60; // 28m
  static const int tenKSeconds = 58 * 60; // 58m
  static const int halfMarathonSeconds = (2 * 3600) + (5 * 60); // 2h 05m
  static const int marathonSeconds = (4 * 3600) + (21 * 60); // 4h 21m

  /// Buckets [distanceKm] into the nearest canonical race distance and
  /// returns its average finish time in seconds. Always positive.
  static int secondsForDistanceKm(double distanceKm) {
    if (distanceKm >= 42.0) return marathonSeconds;
    if (distanceKm >= 21.0) return halfMarathonSeconds;
    if (distanceKm >= 10.0) return tenKSeconds;
    return fiveKSeconds;
  }

  /// Human-readable label for the same bucketing, e.g. "4h 21m".
  static String labelForDistanceKm(double distanceKm) {
    final totalSeconds = secondsForDistanceKm(distanceKm);
    final hours = totalSeconds ~/ 3600;
    final minutes = (totalSeconds % 3600) ~/ 60;
    if (hours > 0) {
      return '${hours}h ${minutes.toString().padLeft(2, '0')}m';
    }
    return '${minutes}m';
  }
}
