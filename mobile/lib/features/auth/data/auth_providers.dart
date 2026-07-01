import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'firebase_auth_repository.dart';
import 'auth_session.dart';

final firebaseAuthRepositoryProvider = Provider<FirebaseAuthRepository>((ref) {
  return FirebaseAuthRepository();
});

/// Emits the current auth state on every sign-in / sign-out event.
/// Yields null when no user is authenticated.
final authStateProvider = StreamProvider<AuthSession?>((ref) {
  return ref.watch(firebaseAuthRepositoryProvider).authStateChanges();
});

/// One-shot read of the current session. Returns null if not signed in.
final currentAuthSessionProvider = FutureProvider<AuthSession?>((ref) {
  return ref.watch(firebaseAuthRepositoryProvider).getCurrentSession();
});

/// The current Firebase ID token. Refreshes automatically on each new
/// auth state change. Used by ApiClient to attach Authorization headers.
final authIdTokenProvider = FutureProvider<String?>((ref) async {
  // Re-derive whenever auth state changes so the token stays fresh.
  ref.watch(authStateProvider);
  return ref.read(firebaseAuthRepositoryProvider).getIdToken();
});
