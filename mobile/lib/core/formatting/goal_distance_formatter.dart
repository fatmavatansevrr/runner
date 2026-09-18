/// PHASE DIST-GEN.3 — the single, shared goal-distance label formatter.
///
/// Previously duplicated as hardcoded per-enum switch statements in
/// `plan_details_page.dart` (`_goalLabel`) and `home_page.dart`
/// (`_PlanCompletedState._fmtGoal`). Both now delegate here so a public
/// numeric target distance (currently only the approved 16K/HalfMarathon-
/// family pilot, `requestedTargetDistanceKm == 16.0`) is labeled correctly
/// as "16K" everywhere, without duplicating the disambiguation logic.
library;

/// Returns a short, human-readable label for a goal distance.
///
/// When [requestedTargetDistanceKm] is non-null (currently only true for the
/// approved 16K/HalfMarathon-family pilot), the label is derived from that
/// exact numeric value (e.g. `16.0` -> `'16K'`) rather than from
/// [goalDistance] — [goalDistance] alone cannot disambiguate "16K" from a
/// true canonical Half Marathon, since both carry the wire value
/// `'half_marathon'`. When null, falls back to the existing per-enum switch.
String formatGoalDistanceLabel({
  required String goalDistance,
  double? requestedTargetDistanceKm,
}) {
  if (requestedTargetDistanceKm != null) {
    return '${requestedTargetDistanceKm.round()}K';
  }
  return switch (goalDistance) {
    'five_k' => '5 km',
    'ten_k' => '10 km',
    'half_marathon' => 'Half Marathon',
    'marathon' => 'Marathon',
    _ => goalDistance,
  };
}
