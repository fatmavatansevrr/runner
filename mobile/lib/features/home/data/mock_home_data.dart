import '../../../core/network/dtos.dart';
import '../../onboarding/data/onboarding_provider.dart';

/// TEST-ONLY mock data for the Home dashboard. Used by
/// [homeDataProvider]'s in-memory bypass (`useMockHomeDataProvider`) so the
/// generate-plan flow can jump straight to Home without a real confirmed
/// plan on the backend. Never used on the real/production data path.
HomeResponse buildMockHomeResponse(OnboardingState onboarding) {
  final today = DateTime.now();
  final startOfWeek = today.subtract(Duration(days: today.weekday - 1)); // Monday

  const dayPlan = [
    ('easy', 'Easy 4k Run', 'Run at a conversational, easy pace for 4 km.', 4.0, 24),
    ('rest', 'Rest Day', 'Give your body time to adapt and get stronger today.', 0.0, 0),
    ('tempo', 'Tempo Run', 'Run at a steady, comfortably hard tempo pace.', 6.0, 32),
    ('easy', 'Easy 4k Run', 'Run at a conversational, easy pace for 4 km.', 4.0, 24),
    ('interval', 'Interval Session', '6x400m at 5K pace with 90s recovery jogs.', 5.0, 35),
    ('rest', 'Rest Day', 'Give your body time to adapt and get stronger today.', 0.0, 0),
    ('long_run', 'Long Run 10k', 'Build endurance with a steady, relaxed 10 km run.', 10.0, 60),
  ];

  final weekSummary = List<TrainingDayResponse>.generate(7, (i) {
    final date = startOfWeek.add(Duration(days: i));
    final (dayType, title, description, distanceKm, durationMin) = dayPlan[i];
    final isPast = date.isBefore(DateTime(today.year, today.month, today.day));

    return TrainingDayResponse(
      dayId: 'mock-day-$i',
      date: date,
      dayType: dayType,
      status: dayType == 'rest'
          ? 'planned'
          : isPast
              ? 'completed'
              : 'planned',
      title: title,
      description: description,
      plannedDistanceKm: distanceKm,
      plannedDurationMin: durationMin,
      plannedPaceMinKm: distanceKm > 0 ? durationMin / distanceKm : null,
      intensity: dayType == 'rest' ? null : 'z2',
      actualDistanceKm: isPast && dayType != 'rest' ? distanceKm : null,
      actualDurationMin: isPast && dayType != 'rest' ? durationMin : null,
      isLongRun: dayType == 'long_run',
      canMarkComplete: !isPast && dayType != 'rest',
      canMarkNotToday: !isPast && dayType != 'rest',
    );
  });

  final todayWorkout = weekSummary.firstWhere(
    (d) => d.date.year == today.year && d.date.month == today.month && d.date.day == today.day,
    orElse: () => weekSummary.first,
  );

  return HomeResponse(
    activePlan: ActivePlanSummaryDto(
      planId: 'mock-plan-id',
      goalType: onboarding.goalType,
      goalDistance: onboarding.goalDistance,
      level: onboarding.runningBackground.wireValue,
      progressText: 'Week 1 of 12',
    ),
    todayWorkout: todayWorkout,
    dailyTip: DailyTipResponse(
      tipKey: 'mock_tip_01',
      title: 'Keep it comfortable',
      message: 'Today is about showing up, not pushing hard.',
      workoutType: 'easy',
    ),
    weekSummary: weekSummary,
    hasPendingConfirmations: false,
  );
}
