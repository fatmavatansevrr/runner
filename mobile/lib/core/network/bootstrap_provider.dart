import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'bootstrap_repository.dart';
import 'dtos.dart';
import '../../features/auth/data/auth_providers.dart';

final bootstrapDataProvider = FutureProvider<BootstrapResponse>((ref) async {
  // Gate on Firebase auth state: if no user is signed in, return 'Welcome'
  // without hitting the backend.
  final authAsync = ref.watch(authStateProvider);
  final session = authAsync.valueOrNull;

  if (session == null) {
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
    return await repo.getBootstrap();
  } catch (_) {
    // Backend unavailable — route to onboarding so the user can proceed.
    return BootstrapResponse(
      isAuthenticated: true,
      hasProfile: false,
      hasActivePlan: false,
      hasPendingConfirmations: false,
      nextScreen: 'Onboarding',
    );
  }
});
