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
  final todayDate = DateTime(now.year, now.month, now.day);

  return List.generate(daysInMonth, (index) {
    final dayNumber = index + 1;
    final date = DateTime(year, month, dayNumber);
    final cellDate = DateTime(date.year, date.month, date.day);
    final weekday = date.weekday;
    
    String dayType = 'rest';
    String status = 'planned';
    String title = 'Rest Day';
    String description = 'Take it easy and recover.';
    double plannedDistance = 0.0;
    int plannedDuration = 0;
    double? plannedPace;
    String? intensity;

    final isPast = cellDate.isBefore(todayDate);
    final isToday = cellDate.isAtSameMomentAs(todayDate);

    if (weekday == 2) {
      dayType = 'easy';
      title = 'Easy Run';
      description = 'Run at an easy, conversational pace.';
      plannedDistance = 5.0;
      plannedDuration = 35;
      plannedPace = 7.0;
      intensity = 'z2';
      if (isPast) {
        status = dayNumber % 9 == 0 ? 'missed' : 'completed';
      } else if (isToday) {
        status = 'planned';
      }
    } else if (weekday == 4) {
      dayType = 'interval';
      title = 'Interval Run';
      description = '4x400m hard interval efforts.';
      plannedDistance = 6.0;
      plannedDuration = 40;
      plannedPace = 6.5;
      intensity = 'z4';
      if (isPast) {
        status = dayNumber % 12 == 0 ? 'missed' : 'completed';
      } else if (isToday) {
        status = 'planned';
      }
    } else if (weekday == 6) {
      dayType = 'easy';
      title = 'Easy Run';
      description = 'Run at an easy, conversational pace.';
      plannedDistance = 6.0;
      plannedDuration = 40;
      plannedPace = 7.0;
      intensity = 'z2';
      if (isPast) {
        status = 'completed';
      } else if (isToday) {
        status = 'planned';
      }
    } else if (weekday == 7) {
      dayType = 'long_run';
      title = 'Long Run';
      description = 'Aerobic endurance building run.';
      plannedDistance = 12.0;
      plannedDuration = 80;
      plannedPace = 7.25;
      intensity = 'z2';
      if (isPast) {
        status = dayNumber % 2 == 0 ? 'completed' : 'missed';
      } else if (isToday) {
        status = 'planned';
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

