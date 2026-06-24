import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/dtos.dart';

class SettingsRepository {
  SettingsRepository(this._client);
  final ApiClient _client;

  Future<SettingsPreferencesResponse> fetchPreferences() async {
    final response = await _client.get('/settings/preferences');
    return SettingsPreferencesResponse.fromJson(response.data as Map<String, dynamic>);
  }
}

final settingsRepositoryProvider = Provider<SettingsRepository>((ref) {
  return SettingsRepository(ref.watch(apiClientProvider));
});
