import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';
import 'api_provider.dart';
import 'dtos.dart';

class BootstrapRepository {
  BootstrapRepository(this._client);
  final ApiClient _client;

  Future<BootstrapResponse> getBootstrap() async {
    final response = await _client.get('/me/bootstrap');
    return BootstrapResponse.fromJson(response.data as Map<String, dynamic>);
  }
}

final bootstrapRepositoryProvider = Provider<BootstrapRepository>((ref) {
  return BootstrapRepository(ref.watch(apiClientProvider));
});
