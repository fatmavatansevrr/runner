import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'calendar_repository.dart';
import '../../../core/network/dtos.dart';
import 'package:intl/intl.dart';

final calendarMonthProvider = StateProvider<String>((ref) {
  return DateFormat('yyyy-MM').format(DateTime.now());
});

final calendarDataProvider = FutureProvider<List<TrainingDayResponse>>((ref) async {
  // TODO: Remove mock fallback when backend integration is stable.
  final month = ref.watch(calendarMonthProvider);
  try {
    final repo = ref.watch(calendarRepositoryProvider);
    final data = await repo.fetchCalendarData(month);
    if (data.isEmpty) {
      return _getMockCalendarData(month);
    }
    return data;
  } catch (e) {
    return _getMockCalendarData(month);
  }
});

List<TrainingDayResponse> _getMockCalendarData(String monthStr) {
  final parts = monthStr.split('-');
  final year = int.parse(parts[0]);
  final month = int.parse(parts[1]);

  // DateTime(year, month + 1, 0) gives the last day of the current month.
  final daysInMonth = month == 12 ? 31 : DateTime(year, month + 1, 0).day;
  final now = DateTime.now();

  return List.generate(daysInMonth, (index) {
    final dayNumber = index + 1;
    final date = DateTime(year, month, dayNumber);
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
      if (date.isBefore(now)) {
        status = dayNumber % 2 == 0 ? 'completed' : 'missed';
      }
    }

    return TrainingDayResponse(
      dayId: 'mock-day-$month-$dayNumber',
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
}

