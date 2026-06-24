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
  return repo.fetchCalendarData(month);
});
