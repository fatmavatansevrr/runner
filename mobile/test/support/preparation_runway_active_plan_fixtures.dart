// Phase 4H.5 — reusable backend-shaped fixture builders for active-plan
// page-level tests. Deliberately NOT derived from any preview DTO (Part 5:
// "Do not reuse preview DTOs as active-plan fixtures") -- these build the
// real Home/Calendar/Detail/PlanDetails JSON shapes directly, matching the
// backend contracts confirmed in Phase 4G.6D/4H.3/4H.4.
library;

/// Fixed anchor date used to compute fixture dates. `DateTime.now()` is
/// called directly (un-injectably) inside Home/Calendar production code
/// (confirmed by repository inspection -- no clock abstraction exists
/// anywhere in this codebase), so full time-freezing via DI is not
/// possible. The documented workaround (same technique used throughout the
/// backend phases of this session): fixtures are built relative to the
/// REAL `DateTime.now()` at test-run time, so "today"-dependent branches
/// (Home's today-workout match, Calendar's isToday/isFuture) always land on
/// a deterministically chosen fixture day -- never a hardcoded historical
/// date that would silently stop matching "today" once the wall clock moves
/// past it.
DateTime testToday() {
  final now = DateTime.now();
  return DateTime(now.year, now.month, now.day);
}

String _isoDate(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

Map<String, dynamic> dayJson({
  required String dayId,
  required DateTime date,
  String dayType = 'easy',
  String status = 'planned',
  double distanceKm = 6.0,
  int durationMin = 35,
  String? intensity = 'EASY',
  bool isLongRun = false,
  bool canMarkComplete = true,
  bool canMarkNotToday = true,
  int? weekNumber,
  String? weekType,
  String? runwayBlock,
  String? source,
  String? adaptedFromId,
  double? actualDistanceKm,
  int? actualDurationMin,
  String title = 'Training Session',
}) {
  return {
    'day_id': dayId,
    'date': _isoDate(date),
    'day_type': dayType,
    'status': status,
    'title': title,
    'description': 'Session description.',
    'planned_distance_km': distanceKm,
    'planned_duration_min': durationMin,
    if (intensity != null) 'intensity': intensity,
    if (actualDistanceKm != null) 'actual_distance_km': actualDistanceKm,
    if (actualDurationMin != null) 'actual_duration_min': actualDurationMin,
    'is_long_run': isLongRun,
    'can_mark_complete': canMarkComplete,
    'can_mark_not_today': canMarkNotToday,
    if (weekNumber != null) 'week_number': weekNumber,
    if (weekType != null) 'week_type': weekType,
    if (runwayBlock != null) 'runway_block': runwayBlock,
    if (source != null) 'source': source,
    if (adaptedFromId != null) 'adapted_from_id': adaptedFromId,
  };
}

/// Real `HomeResponse` shape. `todayWorkout` defaults to a day dated
/// [testToday()] so Home's own `DateTime.now()`-based today-match succeeds.
Map<String, dynamic> homeResponseJson({
  String planId = 'plan-1',
  String goalType = 'race',
  String goalDistance = 'ten_k',
  String level = 'intermediate',
  required String progressText,
  int? currentWeekNumber,
  int? totalWeeks,
  String? currentWeekType,
  String? currentRunwayBlock,
  Map<String, dynamic>? todayWorkout,
  List<Map<String, dynamic>>? weekSummary,
  bool hasPendingConfirmations = false,
}) {
  return {
    'active_plan': {
      'plan_id': planId,
      'goal_type': goalType,
      'goal_distance': goalDistance,
      'level': level,
      'progress_text': progressText,
      if (currentWeekNumber != null) 'current_week_number': currentWeekNumber,
      if (totalWeeks != null) 'total_weeks': totalWeeks,
      if (currentWeekType != null) 'current_week_type': currentWeekType,
      if (currentRunwayBlock != null)
        'current_runway_block': currentRunwayBlock,
    },
    'today_workout': todayWorkout ??
        dayJson(
          dayId: 'today-day',
          date: testToday(),
          weekNumber: currentWeekNumber,
          weekType: currentWeekType,
          runwayBlock: currentRunwayBlock,
        ),
    'daily_tip': null,
    'week_summary': weekSummary ??
        [
          todayWorkout ??
              dayJson(
                  dayId: 'today-day',
                  date: testToday(),
                  weekNumber: currentWeekNumber,
                  weekType: currentWeekType,
                  runwayBlock: currentRunwayBlock)
        ],
    'has_pending_confirmations': hasPendingConfirmations,
  };
}

/// A month of Calendar days (real `List<TrainingDayResponse>` shape).
List<Map<String, dynamic>> calendarMonthJson({
  required DateTime monthAnchor,
  required int daysInMonth,
  required String weekType,
  String? runwayBlock,
  int startingWeekNumber = 1,
}) {
  return List.generate(daysInMonth, (i) {
    final date = DateTime(monthAnchor.year, monthAnchor.month, i + 1);
    return dayJson(
      dayId: 'cal-${monthAnchor.year}-${monthAnchor.month}-${i + 1}',
      date: date,
      dayType: i % 7 == 6 ? 'long_run' : 'easy',
      isLongRun: i % 7 == 6,
      weekNumber: startingWeekNumber + (i ~/ 7),
      weekType: weekType,
      runwayBlock: runwayBlock,
    );
  });
}

Map<String, dynamic> planDetailsJson({
  bool hasActivePlan = true,
  String planId = 'plan-1',
  String goalType = 'race',
  String goalDistance = 'ten_k',
  String level = 'intermediate',
  int daysPerWeek = 4,
  int totalWeeks = 17,
  int runwayWeeks = 0,
  int completedWeeksCount = 0,
}) {
  final weeks = <Map<String, dynamic>>[];
  const corePhases = [
    'base',
    'build',
    'recovery',
    'peak',
    'taper',
    'race_week'
  ];
  for (var i = 0; i < runwayWeeks; i++) {
    weeks.add({
      'week_id': 'week-${i + 1}',
      'week_number': i + 1,
      'week_type': 'preparation_runway',
      'planned_volume_km': 20.0,
      'actual_volume_km': 0.0,
      'is_recovery_week': false,
      'start_date': '2026-07-20',
      'days': <Map<String, dynamic>>[],
    });
  }
  for (var i = 0; i < totalWeeks - runwayWeeks; i++) {
    weeks.add({
      'week_id': 'week-${runwayWeeks + i + 1}',
      'week_number': runwayWeeks + i + 1,
      'week_type': corePhases[i % corePhases.length],
      'planned_volume_km': 30.0,
      'actual_volume_km': 0.0,
      'is_recovery_week': false,
      'start_date': '2026-08-20',
      'days': <Map<String, dynamic>>[],
    });
  }
  return {
    'has_active_plan': hasActivePlan,
    'plan_id': planId,
    'status': 'active',
    'goal_type': goalType,
    'goal_distance': goalDistance,
    'level': level,
    'days_per_week': daysPerWeek,
    'unit': 'km',
    'started_at': '2026-07-20T00:00:00Z',
    'estimated_end_date': '2026-11-20T00:00:00Z',
    'total_weeks': totalWeeks,
    'completed_weeks_count': completedWeeksCount,
    'total_planned_distance': 500.0,
    'total_completed_distance': 0.0,
    'weeks': weeks,
  };
}

Map<String, dynamic> pendingConfirmationJson({
  required String pendingConfirmationId,
  required String trainingDayId,
  required DateTime date,
  String dayType = 'easy',
  String title = 'Easy Run',
  double plannedDistanceKm = 6.0,
  int plannedDurationMin = 35,
}) =>
    {
      'pending_confirmation_id': pendingConfirmationId,
      'training_day_id': trainingDayId,
      'date': _isoDate(date),
      'day_type': dayType,
      'title': title,
      'planned_distance_km': plannedDistanceKm,
      'planned_duration_min': plannedDurationMin,
    };

Map<String, dynamic> profileOverviewJson({
  String name = 'Runner',
  bool hasActivePlan = true,
  String planName = 'TEN_K Preparation Plan',
  String goalType = 'race',
  String goalDistance = 'ten_k',
}) =>
    {
      'name': name,
      'email': 'runner@example.com',
      'unit': 'km',
      'running_background': 'intermediate',
      if (hasActivePlan)
        'active_plan_stats': {
          'plan_name': planName,
          'goal_type': goalType,
          'goal_distance': goalDistance,
          'completed_runs_count': 0,
          'total_planned_runs_count': 0,
          'total_completed_distance': 0.0,
          'adherence_rate_percent': 0.0,
        },
    };
