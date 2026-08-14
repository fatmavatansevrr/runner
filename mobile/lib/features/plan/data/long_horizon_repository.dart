import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/dtos.dart';
import '../../../core/network/long_horizon_dtos.dart';

/// Wraps every Long-Horizon (RollingLongHorizon) HTTP call. Every method
/// here is a thin, honest pass-through to a real backend endpoint — no
/// method here computes a schedule, a checkpoint, a recovery decision, or
/// an activation decision. The backend is the sole authority; this class
/// only transports its requests/responses.
class LongHorizonRepository {
  LongHorizonRepository(this._client);
  final ApiClient _client;

  /// POST /plans/generate-preview/race/long-horizon
  /// Reuses the same request shape as the existing static race preview —
  /// the backend controller accepts the identical `GenerateRacePlanPreviewRequest`
  /// for both routes; the horizon length alone (server-side) determines
  /// which schedule strategy the returned preview will use. Callers should
  /// still request the strategy-appropriate endpoint to receive the
  /// correctly-shaped preview response.
  Future<LongHorizonPlanPreviewContract> generateLongHorizonRacePlanPreview(
      GenerateRacePlanPreviewRequestDto request) async {
    final response = await _client.post(
      '/plans/generate-preview/race/long-horizon',
      data: request.toJson(),
    );
    return LongHorizonPlanPreviewContract.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// POST /plans/confirm/long-horizon
  Future<LongHorizonConfirmPlanResponse> confirmLongHorizonPlan(
      String previewId) async {
    final response = await _client.post(
      '/plans/confirm/long-horizon',
      data: {'preview_id': previewId, 'contract_version': 1},
    );
    return LongHorizonConfirmPlanResponse.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// GET /plans/active/home — strategy-discriminated; same endpoint the
  /// static Home flow already uses.
  Future<ActiveHomeResult> fetchActiveHome() async {
    final response = await _client.get('/plans/active/home');
    return ActiveHomeResult.fromJson(response.data as Map<String, dynamic>);
  }

  /// GET /plans/active/calendar?month=YYYY-MM — strategy-discriminated.
  Future<ActiveCalendarResult> fetchActiveCalendar(String month) async {
    final response = await _client
        .get('/plans/active/calendar', queryParameters: {'month': month});
    return ActiveCalendarResult.fromJson(response.data);
  }

  /// GET /training-days/rolling/{sessionId}
  Future<LongHorizonRollingSessionDetailResponse> fetchRollingSessionDetail(
      String sessionId) async {
    final response = await _client.get('/training-days/rolling/$sessionId');
    return LongHorizonRollingSessionDetailResponse.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// POST /training-days/rolling/{sessionId}/complete
  Future<LongHorizonSessionMutationResponse> completeRollingSession(
    String sessionId, {
    required double actualDistanceKm,
    required int actualDurationMinutes,
  }) async {
    final response = await _client.post(
      '/training-days/rolling/$sessionId/complete',
      data: {
        'contract_version': 1,
        'actual_distance_km': actualDistanceKm,
        'actual_duration_minutes': actualDurationMinutes,
      },
    );
    return LongHorizonSessionMutationResponse.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// POST /training-days/rolling/{sessionId}/not-today
  Future<LongHorizonSessionMutationResponse> markRollingSessionNotToday(
    String sessionId,
    NotTodayReason reason,
  ) async {
    final response = await _client.post(
      '/training-days/rolling/$sessionId/not-today',
      data: {'contract_version': 1, 'reason': reason.wireValue},
    );
    return LongHorizonSessionMutationResponse.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// POST /plans/active/long-horizon/activate-next-window
  /// Explicit, user-initiated only. Never call this automatically after a
  /// completion/not-today mutation or from a background timer.
  Future<LongHorizonActivateNextWindowResponse> activateNextWindow() async {
    final response = await _client.post(
      '/plans/active/long-horizon/activate-next-window',
      data: {'contract_version': 1},
    );
    return LongHorizonActivateNextWindowResponse.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// POST /plans/active/long-horizon/retry
  /// Explicit, user-initiated only. Never call this automatically.
  Future<LongHorizonRetryContinuationResponse> retryContinuation() async {
    final response = await _client.post(
      '/plans/active/long-horizon/retry',
      data: {'contract_version': 1},
    );
    return LongHorizonRetryContinuationResponse.fromJson(
        response.data as Map<String, dynamic>);
  }
}

final longHorizonRepositoryProvider = Provider<LongHorizonRepository>((ref) {
  return LongHorizonRepository(ref.watch(apiClientProvider));
});
