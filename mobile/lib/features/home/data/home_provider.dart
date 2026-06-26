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
  final startOfWeek = DateTime(now.year, now.month, now.day).subtract(Duration(days: now.weekday - 1));
  
  final weekSummary = List.generate(7, (index) {
    final date = startOfWeek.add(Duration(days: index));
    final weekday = date.weekday;
    String dayType = 'rest';
    String status = 'planned';
    String title = 'Rest Day';
    String description = 'Take it easy and recover.';
    double plannedDistance = 0.0;
    int plannedDuration = 0;
    double? plannedPace;
    String? intensity;

    if (weekday == 2) {
      dayType = 'easy';
      title = 'Easy Run';
      description = 'Run at an easy, conversational pace.';
      plannedDistance = 5.0;
      plannedDuration = 35;
      plannedPace = 7.0;
      intensity = 'z2';
      if (date.isBefore(now)) status = 'completed';
    } else if (weekday == 4) {
      dayType = 'interval';
      title = 'Interval Run';
      description = '4x400m hard interval efforts.';
      plannedDistance = 6.0;
      plannedDuration = 40;
      plannedPace = 6.5;
      intensity = 'z4';
      if (date.isBefore(now)) status = 'completed';
    } else if (weekday == 6) {
      dayType = 'tempo';
      title = 'Tempo Run';
      description = 'Sustained threshold run.';
      plannedDistance = 8.0;
      plannedDuration = 50;
      plannedPace = 6.25;
      intensity = 'z3';
      if (date.isBefore(now)) status = 'completed';
    } else if (weekday == 7) {
      dayType = 'long_run';
      title = 'Long Run';
      description = 'Aerobic endurance building run.';
      plannedDistance = 12.0;
      plannedDuration = 80;
      plannedPace = 7.25;
      intensity = 'z2';
      if (date.isBefore(now)) status = 'missed';
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
      progressText: 'Week 1 of 8',
    ),
    todayWorkout: todayWorkout.dayType == 'rest' ? null : todayWorkout,
    dailyTip: DailyTipResponse(
      tipKey: 'mock-tip',
      title: 'Hydration is key',
      message: 'Drink plenty of water before and after your runs to maintain peak performance and avoid cramping.',
    ),
    weekSummary: weekSummary,
    hasPendingConfirmations: false,
  );
}

