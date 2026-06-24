import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/dtos.dart';

class CalendarRepository {
  CalendarRepository(this._client);
  final ApiClient _client;

  Future<List<TrainingDayResponse>> fetchCalendarData(String month) async {
    final response = await _client.get(
      '/plans/active/calendar',
      queryParameters: {'month': month},
    );
    return (response.data as List? ?? [])
        .map((e) => TrainingDayResponse.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}

final calendarRepositoryProvider = Provider<CalendarRepository>((ref) {
  return CalendarRepository(ref.watch(apiClientProvider));
});
