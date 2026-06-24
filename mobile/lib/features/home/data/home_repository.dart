import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/dtos.dart';

class HomeRepository {
  HomeRepository(this._client);
  final ApiClient _client;

  Future<HomeResponse> fetchHomeData() async {
    final response = await _client.get('/plans/active/home');
    return HomeResponse.fromJson(response.data as Map<String, dynamic>);
  }

  Future<CompleteWorkoutResponse> completeWorkout(
    String trainingDayId,
    double actualDistanceKm,
    int actualDurationMin,
    String? userNote,
  ) async {
    final response = await _client.post(
      '/training-days/$trainingDayId/complete',
      data: CompleteWorkoutRequest(
        actualDistanceKm: actualDistanceKm,
        actualDurationMin: actualDurationMin,
        userNote: userNote,
      ).toJson(),
    );
    return CompleteWorkoutResponse.fromJson(response.data as Map<String, dynamic>);
  }

  Future<CreateNotTodayDecisionResponse> createNotTodayDecision(
    String trainingDayId,
    String reason,
  ) async {
    final response = await _client.post(
      '/training-days/$trainingDayId/not-today-decisions',
      data: CreateNotTodayDecisionRequest(reason: reason).toJson(),
    );
    return CreateNotTodayDecisionResponse.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ConfirmNotTodayDecisionResponse> confirmNotTodayDecision(
    String decisionId,
  ) async {
    final response = await _client.post(
      '/not-today-decisions/$decisionId/confirm',
      data: {},
    );
    return ConfirmNotTodayDecisionResponse.fromJson(response.data as Map<String, dynamic>);
  }
}

final homeRepositoryProvider = Provider<HomeRepository>((ref) {
  return HomeRepository(ref.watch(apiClientProvider));
});
