import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/dtos.dart';

class TrainingDayRepository {
  TrainingDayRepository(this._client);
  final ApiClient _client;

  Future<TrainingDayDetailResponse> fetchTrainingDayDetail(String trainingDayId) async {
    final response = await _client.get('/training-days/$trainingDayId');
    return TrainingDayDetailResponse.fromJson(response.data as Map<String, dynamic>);
  }
}

final trainingDayRepositoryProvider = Provider<TrainingDayRepository>((ref) {
  return TrainingDayRepository(ref.watch(apiClientProvider));
});
