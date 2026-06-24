import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/dtos.dart';

class PendingConfirmationRepository {
  PendingConfirmationRepository(this._client);
  final ApiClient _client;

  Future<List<PendingConfirmationResponse>> fetchPendingConfirmations() async {
    final response = await _client.get('/pending-confirmations');
    return (response.data as List? ?? [])
        .map((e) => PendingConfirmationResponse.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<ResolvePendingConfirmationResponse> resolvePendingConfirmation({
    required String pendingConfirmationId,
    required String resolution,
    double? actualDistanceKm,
    int? actualDurationMin,
    String? userNote,
  }) async {
    final response = await _client.post(
      '/pending-confirmations/resolve',
      data: ResolvePendingConfirmationRequest(
        pendingConfirmationId: pendingConfirmationId,
        resolution: resolution,
        actualDistanceKm: actualDistanceKm,
        actualDurationMin: actualDurationMin,
        userNote: userNote,
      ).toJson(),
    );
    return ResolvePendingConfirmationResponse.fromJson(response.data as Map<String, dynamic>);
  }
}

final pendingConfirmationRepositoryProvider = Provider<PendingConfirmationRepository>((ref) {
  return PendingConfirmationRepository(ref.watch(apiClientProvider));
});
