import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/api_client.dart';
import '../../../core/network/api_provider.dart';
import '../../../core/network/dtos.dart';

class ProfileRepository {
  ProfileRepository(this._client);
  final ApiClient _client;

  Future<ProfileOverviewResponse> fetchProfileOverview() async {
    final response = await _client.get('/profile/overview');
    return ProfileOverviewResponse.fromJson(response.data as Map<String, dynamic>);
  }

  Future<PlanDetailsResponse> fetchActivePlanDetails() async {
    final response = await _client.get('/plans/active/details');
    return PlanDetailsResponse.fromJson(response.data as Map<String, dynamic>);
  }
}

final profileRepositoryProvider = Provider<ProfileRepository>((ref) {
  return ProfileRepository(ref.watch(apiClientProvider));
});
