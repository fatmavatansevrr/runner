/// Running Background V2 — a previously-completed race result the user
/// reports at onboarding for pace/readiness context. Deliberately distinct
/// from the *target* race entered in "Enter Race Details" (a future goal
/// event) — the two objects must never be mixed or overwritten.
class RecentRaceResult {
  const RecentRaceResult({
    required this.distanceKm,
    required this.finishTimeSeconds,
    required this.raceDate,
    this.raceName,
  });

  /// Race distance in kilometers (application-unit-neutral canonical
  /// storage; the UI converts to/from the user's displayed unit at the
  /// input boundary only).
  final double distanceKm;

  final int finishTimeSeconds;

  final DateTime raceDate;

  /// Optional; only included if a name was entered.
  final String? raceName;

  /// Buckets [distanceKm] into the nearest canonical goal-distance wire
  /// token (matching `GoalDistance`'s snake_case values on the backend:
  /// five_k/ten_k/half_marathon/marathon). Uses the same preset thresholds
  /// as [_distancePresetLabel] below. Falls back to "custom" — the closest
  /// preset is NOT silently substituted for a genuinely custom distance.
  String get distanceToken {
    if ((distanceKm - 5).abs() < 0.5) return 'five_k';
    if ((distanceKm - 10).abs() < 0.5) return 'ten_k';
    if ((distanceKm - 21.1).abs() < 1.0) return 'half_marathon';
    if ((distanceKm - 42.2).abs() < 1.0) return 'marathon';
    return 'custom';
  }

  RecentRaceResult copyWith({
    double? distanceKm,
    int? finishTimeSeconds,
    DateTime? raceDate,
    String? raceName,
  }) {
    return RecentRaceResult(
      distanceKm: distanceKm ?? this.distanceKm,
      finishTimeSeconds: finishTimeSeconds ?? this.finishTimeSeconds,
      raceDate: raceDate ?? this.raceDate,
      raceName: raceName ?? this.raceName,
    );
  }

  /// Compact summary, e.g. "10K · 58:30 · 14 Jun 2026".
  String summary({required bool useKm}) {
    final displayDistance = useKm ? distanceKm : distanceKm / 1.60934;
    final distanceLabel = _distancePresetLabel(displayDistance, useKm);
    final minutes = finishTimeSeconds ~/ 60;
    final seconds = finishTimeSeconds % 60;
    final timeLabel =
        '${minutes.toString()}:${seconds.toString().padLeft(2, '0')}';
    final dateLabel = _formatDate(raceDate);
    return '$distanceLabel · $timeLabel · $dateLabel';
  }

  static String _distancePresetLabel(double displayDistance, bool useKm) {
    final unit = useKm ? 'km' : 'mi';
    // Recognize common presets with their conventional short label;
    // otherwise fall back to the raw numeric distance.
    if (useKm) {
      if ((displayDistance - 5).abs() < 0.05) return '5K';
      if ((displayDistance - 10).abs() < 0.05) return '10K';
      if ((displayDistance - 21.1).abs() < 0.15) return 'Half Marathon';
      if ((displayDistance - 42.2).abs() < 0.15) return 'Marathon';
    }
    return '${displayDistance.toStringAsFixed(1)}$unit';
  }

  static String _formatDate(DateTime date) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    return '${date.day} ${months[date.month - 1]} ${date.year}';
  }
}
