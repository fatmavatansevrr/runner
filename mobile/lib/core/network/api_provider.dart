import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';
import '../../features/auth/data/auth_providers.dart';

final apiClientProvider = Provider<ApiClient>((ref) {
  final authRepo = ref.watch(firebaseAuthRepositoryProvider);

  return ApiClient(
    // Regular calls: returns the cached Firebase token (no network hit).
    tokenProvider: () => authRepo.getIdToken(),

    // Called once on 401: forces Firebase to fetch a fresh token from Google.
    // If this also fails, onAuthInvalidated is triggered.
    tokenRefresher: () => authRepo.getIdToken(forceRefresh: true),

    // Called when the 401 retry still fails — the session is truly revoked.
    // Firebase sign-out clears local state; authStateProvider emits null,
    // which causes bootstrapDataProvider to return 'Welcome' and the router
    // to redirect the user back to the auth screen.
    onAuthInvalidated: () => authRepo.signOut(),
  );
});
