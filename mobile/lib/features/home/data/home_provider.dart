import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'home_repository.dart';
import 'mock_home_data.dart';
import '../../../core/network/dtos.dart';
import '../../onboarding/data/onboarding_provider.dart';

/// TEST-ONLY bypass: when true, [homeDataProvider] returns mock data for
/// the current week instead of calling the real `/plans/active/home`
/// endpoint. Set by the generate-plan test shortcut
/// (`PlanGenerationPage`) so Home can be reached without a confirmed plan
/// on the backend. Defaults to false — the real network path is untouched
/// otherwise.
final useMockHomeDataProvider = StateProvider<bool>((ref) => false);

final homeDataProvider = FutureProvider<HomeResponse>((ref) async {
  if (ref.watch(useMockHomeDataProvider)) {
    return buildMockHomeResponse(ref.watch(onboardingProvider));
  }

  final repo = ref.watch(homeRepositoryProvider);
  return repo.fetchHomeData();
});
