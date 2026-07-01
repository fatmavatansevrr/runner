import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'calendar_repository.dart';
import '../../../core/network/dtos.dart';
import 'package:intl/intl.dart';

final calendarMonthProvider = StateProvider<String>((ref) {
  return DateFormat('yyyy-MM').format(DateTime.now());
});

final calendarDataProvider = FutureProvider<List<TrainingDayResponse>>((ref) async {
  final month = ref.watch(calendarMonthProvider);
  final repo = ref.watch(calendarRepositoryProvider);
  final startTime = DateTime.now();
  print('CALENDAR_PROVIDER_LOG: API request started for month $month at ${startTime.toIso8601String()}');
  
  final result = await repo.fetchCalendarData(month);
  
  final endTime = DateTime.now();
  print('CALENDAR_PROVIDER_LOG: API request completed at ${endTime.toIso8601String()}. Duration: ${endTime.difference(startTime).inMilliseconds}ms. Count: ${result.length} items.');
  return result;
});
