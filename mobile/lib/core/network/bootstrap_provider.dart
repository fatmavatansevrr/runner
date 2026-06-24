import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'bootstrap_repository.dart';
import 'dtos.dart';
import '../../features/auth/data/auth_provider.dart';

final bootstrapDataProvider = FutureProvider<BootstrapResponse>((ref) async {
  final auth = ref.watch(authProvider);
  if (!auth.isAuthenticated) {
    return BootstrapResponse(
      isAuthenticated: false,
      hasProfile: false,
      hasActivePlan: false,
      hasPendingConfirmations: false,
      nextScreen: 'Welcome',
    );
  }

  try {
    final repo = ref.watch(bootstrapRepositoryProvider);
    final response = await repo.getBootstrap();
    return response;
  } catch (e) {
    // If backend fails or not setup, return default fallback authenticated nextScreen
    return BootstrapResponse(
      isAuthenticated: true,
      hasProfile: false,
      hasActivePlan: false,
      hasPendingConfirmations: false,
      nextScreen: 'Onboarding',
    );
  }
});
