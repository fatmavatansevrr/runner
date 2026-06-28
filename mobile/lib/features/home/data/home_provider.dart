import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'home_repository.dart';
import '../../../core/network/dtos.dart';

final homeDataProvider = FutureProvider<HomeResponse>((ref) async {
  // TODO: Remove mock fallback when backend integration is stable.
  try {
    final repo = ref.watch(homeRepositoryProvider);
    final response = await repo.fetchHomeData();
    if (response.activePlan == null) {
      return _getMockHomeResponse();
    }
    return response;
  } catch (e) {
    return _getMockHomeResponse();
  }
});

HomeResponse _getMockHomeResponse() {
  final now = DateTime.now();
  // Monday-first week calculation
  final startOfWeek = DateTime(now.year, now.month, now.day).subtract(Duration(days: now.weekday - 1));
  
  final weekSummary = List.generate(7, (index) {
    final date = startOfWeek.add(Duration(days: index));
    final isToday = date.day == now.day && date.month == now.month && date.year == now.year;
    
    String dayType = 'rest';
    String status = 'planned';
    String title = 'Rest Day';
    String description = 'Take it easy and recover.';
    double plannedDistance = 0.0;
    int plannedDuration = 0;
    double? plannedPace;
    String? intensity;

    if (isToday) {
      // Force today to be the required Easy Run
      dayType = 'easy';
      title = 'Easy Run';
      description = 'Run at an easy, conversational pace.';
      plannedDistance = 5.0;
      plannedDuration = 35;
      plannedPace = 6.25; // 6.25 formats to Pace: 6:00-6:30
      intensity = 'z2';
      status = 'planned';
    } else {
      // Dynamic mock data based on weekday relative to today
      final weekday = date.weekday;
      if (weekday == 1) { // Monday
        dayType = 'easy';
        title = 'Easy Run';
        description = 'Easy conversational run.';
        plannedDistance = 5.0;
        plannedDuration = 30;
        plannedPace = 6.25;
        intensity = 'z2';
        status = date.isBefore(now) ? 'completed' : 'planned';
      } else if (weekday == 2) { // Tuesday
        dayType = 'rest';
        title = 'Rest Day';
        description = 'Recovery day.';
        status = date.isBefore(now) ? 'completed' : 'planned';
      } else if (weekday == 3) { // Wednesday
        dayType = 'interval';
        title = 'Interval Run';
        description = '4x400m speed repetitions.';
        plannedDistance = 6.0;
        plannedDuration = 35;
        plannedPace = 5.75;
        intensity = 'z4';
        status = date.isBefore(now) ? 'completed' : 'planned';
      } else if (weekday == 4) { // Thursday
        dayType = 'rest';
        title = 'Rest Day';
        description = 'Rest and stretch.';
        status = date.isBefore(now) ? 'completed' : 'planned';
      } else if (weekday == 5) { // Friday
        dayType = 'easy';
        title = 'Easy Run';
        description = 'Easy run.';
        plannedDistance = 5.0;
        plannedDuration = 30;
        plannedPace = 6.25;
        intensity = 'z2';
        status = date.isBefore(now) ? 'completed' : 'planned';
      } else if (weekday == 6) { // Saturday
        dayType = 'long_run';
        title = 'Long Run';
        description = 'Weekly aerobic distance builder.';
        plannedDistance = 10.0;
        plannedDuration = 60;
        plannedPace = 6.75;
        intensity = 'z2';
        status = date.isBefore(now) ? 'completed' : 'planned';
      } else if (weekday == 7) { // Sunday
        dayType = 'rest';
        title = 'Rest Day';
        description = 'Weekly recovery.';
        status = date.isBefore(now) ? 'completed' : 'planned';
      }
    }

    return TrainingDayResponse(
      dayId: 'mock-day-$index-${date.day}',
      date: date,
      dayType: dayType,
      status: status,
      title: title,
      description: description,
      plannedDistanceKm: plannedDistance,
      plannedDurationMin: plannedDuration,
      plannedPaceMinKm: plannedPace,
      intensity: intensity,
      actualDistanceKm: status == 'completed' ? plannedDistance : null,
      actualDurationMin: status == 'completed' ? plannedDuration : null,
      isLongRun: dayType == 'long_run',
      canMarkComplete: status == 'planned',
      canMarkNotToday: status == 'planned',
    );
  });

  // Find today's workout in weekSummary
  final todayWorkout = weekSummary.firstWhere(
    (d) => d.date.day == now.day && d.date.month == now.month && d.date.year == now.year,
    orElse: () => weekSummary[0],
  );

  return HomeResponse(
    activePlan: ActivePlanSummaryDto(
      planId: 'mock-plan-id-1234',
      goalType: 'race',
      goalDistance: 'five_k',
      level: 'new_to_running',
      progressText: 'Week 6 of 8',
    ),
    todayWorkout: todayWorkout,
    dailyTip: DailyTipResponse(
      tipKey: 'mock-tip',
      title: 'Hydration is key',
      message: 'Drink plenty of water before and after your runs to maintain peak performance and avoid cramping.',
    ),
    weekSummary: weekSummary,
    hasPendingConfirmations: false,
  );
}

