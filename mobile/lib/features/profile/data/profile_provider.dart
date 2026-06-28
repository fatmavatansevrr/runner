import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'profile_repository.dart';
import '../../../core/network/dtos.dart';

final profileOverviewProvider = FutureProvider<ProfileOverviewResponse>((ref) async {
  try {
    final repo = ref.watch(profileRepositoryProvider);
    return await repo.fetchProfileOverview();
  } catch (e) {
    return _getMockProfileOverview();
  }
});

final activePlanDetailsProvider = FutureProvider<PlanDetailsResponse>((ref) async {
  try {
    final repo = ref.watch(profileRepositoryProvider);
    return await repo.fetchActivePlanDetails();
  } catch (e) {
    return _getMockPlanDetails();
  }
});

ProfileOverviewResponse _getMockProfileOverview() {
  return ProfileOverviewResponse(
    name: "Jane Doe",
    email: "jane.doe@example.com",
    unit: "km",
    runningBackground: "used_to_run",
    activePlanStats: ProfilePlanStatsDto(
      planName: "5K Adaptive Plan",
      goalType: "race",
      goalDistance: "five_k",
      completedRunsCount: 8,
      totalPlannedRunsCount: 24,
      totalCompletedDistance: 42.5,
      adherenceRatePercent: 88.0,
    ),
  );
}

PlanDetailsResponse _getMockPlanDetails() {
  final now = DateTime.now();
  final startDate = now.subtract(const Duration(days: 14));
  final endDate = now.add(const Duration(days: 42));

  // Generate 8 weeks
  final weeks = List.generate(8, (weekIdx) {
    final weekStart = startDate.add(Duration(days: weekIdx * 7));
    
    // Generate 7 days in each week
    final days = List.generate(7, (dayIdx) {
      final dayDate = weekStart.add(Duration(days: dayIdx));
      final isPast = dayDate.isBefore(now);
      
      String dayType = 'rest';
      String status = 'planned';
      String title = 'Rest Day';
      String description = 'Rest and recover.';
      double plannedDistance = 0.0;
      int plannedDuration = 0;
      double? plannedPace;
      String? intensity;

      if (dayIdx == 1) { // Tuesday
        dayType = 'easy';
        title = 'Easy Run';
        description = 'Run at an easy, conversational pace.';
        plannedDistance = 5.0;
        plannedDuration = 35;
        plannedPace = 7.0;
        intensity = 'z2';
        if (isPast) status = 'completed';
      } else if (dayIdx == 3) { // Thursday
        dayType = 'interval';
        title = 'Interval Run';
        description = '4x400m hard interval efforts.';
        plannedDistance = 6.0;
        plannedDuration = 40;
        plannedPace = 6.5;
        intensity = 'z4';
        if (isPast) status = 'completed';
      } else if (dayIdx == 5) { // Saturday
        dayType = 'easy';
        title = 'Easy Run';
        description = 'Run at an easy, conversational pace.';
        plannedDistance = 6.0;
        plannedDuration = 40;
        plannedPace = 7.0;
        intensity = 'z2';
        if (isPast) status = 'completed';
      } else if (dayIdx == 6) { // Sunday
        dayType = 'long_run';
        title = 'Long Run';
        description = 'Aerobic endurance building run.';
        plannedDistance = 12.0;
        plannedDuration = 80;
        plannedPace = 7.25;
        intensity = 'z2';
        if (isPast) {
          status = weekIdx % 2 == 0 ? 'completed' : 'missed';
        }
      }

      return PlanDayDetailDto(
        dayId: 'mock-plan-day-$weekIdx-$dayIdx',
        date: dayDate,
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

    return PlanWeekDetailDto(
      weekId: 'mock-plan-week-$weekIdx',
      weekNumber: weekIdx + 1,
      weekType: weekIdx == 3 || weekIdx == 7 ? 'recovery' : 'base',
      plannedVolumeKm: weekIdx == 3 || weekIdx == 7 ? 15.0 : 31.0,
      actualVolumeKm: weekIdx == 0 ? 31.0 : (weekIdx == 1 ? 19.0 : 0.0),
      isRecoveryWeek: weekIdx == 3 || weekIdx == 7,
      startDate: weekStart,
      days: days,
    );
  });

  return PlanDetailsResponse(
    planId: 'mock-plan-id-1234',
    status: 'active',
    goalType: 'race',
    goalDistance: 'five_k',
    level: 'new_to_running',
    daysPerWeek: 3,
    unit: 'km',
    raceName: 'City 5K',
    raceDate: endDate,
    targetFinishTimeSeconds: 1500,
    startedAt: startDate,
    estimatedEndDate: endDate,
    totalWeeks: 8,
    completedWeeksCount: 2,
    totalPlannedDistance: 240.0,
    totalCompletedDistance: 42.5,
    weeks: weeks,
  );
}

