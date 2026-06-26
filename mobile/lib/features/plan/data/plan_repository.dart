import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/dtos.dart';

class PlanRepository {
  PlanRepository(this._client);
  final ApiClient _client;

  Future<GeneratePreviewResponse> generatePreview(GeneratePreviewRequest request) async {
    const path = '/plans/generate-preview';
    print('Calling generatePreview: ${ApiClient.resolveBaseUrl()}$path');
    final response = await _client.post(
      path,
      data: request.toJson(),
    );
    return GeneratePreviewResponse.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ConfirmPlanResponse> confirmPlan(String previewId) async {
    final response = await _client.post(
      '/plans/confirm',
      data: ConfirmPlanRequest(previewId: previewId).toJson(),
    );
    return ConfirmPlanResponse.fromJson(response.data as Map<String, dynamic>);
  }

  Future<CancelPlanResponse> cancelPlan(String planId, String reason) async {
    final response = await _client.post(
      '/plans/$planId/cancel',
      data: CancelPlanRequest(reason: reason).toJson(),
    );
    return CancelPlanResponse.fromJson(response.data as Map<String, dynamic>);
  }
}

final planRepositoryProvider = Provider<PlanRepository>((ref) {
  return PlanRepository(ref.watch(apiClientProvider));
});
