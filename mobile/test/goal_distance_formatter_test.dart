import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/formatting/goal_distance_formatter.dart';

/// PHASE DIST-GEN.3 — the shared goal-distance label formatter used by both
/// `plan_details_page.dart` and `home_page.dart` (previously two duplicated
/// hardcoded switch statements).
void main() {
  group('formatGoalDistanceLabel', () {
    test('renders "16K" when requestedTargetDistanceKm is present (the 16K pilot)', () {
      expect(
        formatGoalDistanceLabel(goalDistance: 'half_marathon', requestedTargetDistanceKm: 16.0),
        '16K',
      );
    });

    test('renders "15K" when requestedTargetDistanceKm is present (DIST-GEN.7 15K projected target)', () {
      expect(
        formatGoalDistanceLabel(goalDistance: 'half_marathon', requestedTargetDistanceKm: 15.0),
        '15K',
      );
    });

    test('rounds a non-exact requestedTargetDistanceKm before labeling', () {
      expect(
        formatGoalDistanceLabel(goalDistance: 'half_marathon', requestedTargetDistanceKm: 15.6),
        '16K',
      );
    });

    test('falls back to the canonical per-enum label when requestedTargetDistanceKm is null', () {
      expect(formatGoalDistanceLabel(goalDistance: 'five_k'), '5 km');
      expect(formatGoalDistanceLabel(goalDistance: 'ten_k'), '10 km');
      expect(formatGoalDistanceLabel(goalDistance: 'half_marathon'), 'Half Marathon');
      expect(formatGoalDistanceLabel(goalDistance: 'marathon'), 'Marathon');
    });

    test('a true canonical half_marathon (requestedTargetDistanceKm null) is never mislabeled "16K"', () {
      expect(
        formatGoalDistanceLabel(goalDistance: 'half_marathon', requestedTargetDistanceKm: null),
        'Half Marathon',
      );
    });

    test('unknown goalDistance falls back to the raw value', () {
      expect(formatGoalDistanceLabel(goalDistance: 'unknown_value'), 'unknown_value');
    });
  });
}
