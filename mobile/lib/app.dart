import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'core/routing/app_router.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/data/auth_providers.dart';
import 'features/auth/data/auth_session.dart';
import 'core/network/bootstrap_provider.dart';
import 'features/home/data/home_provider.dart';
import 'features/calendar/data/calendar_provider.dart';
import 'features/profile/data/profile_provider.dart';
import 'features/pending_confirmation/data/pending_confirmation_provider.dart';

class AntigravityApp extends ConsumerStatefulWidget {
  const AntigravityApp({super.key});

  @override
  ConsumerState<AntigravityApp> createState() => _AntigravityAppState();
}

class _AntigravityAppState extends ConsumerState<AntigravityApp> {
  @override
  Widget build(BuildContext context) {
    // On every auth state change (sign-in, sign-out, different account),
    // invalidate all user-scoped data providers so stale data from a previous
    // session is never shown to a different user.
    ref.listen<AsyncValue<AuthSession?>>(
      authStateProvider,
      (previous, next) {
        final prevUid = previous?.valueOrNull?.uid;
        final nextUid = next.valueOrNull?.uid;
        if (prevUid != nextUid) {
          _clearUserDataCaches();
        }
      },
    );

    return MaterialApp.router(
      title: 'Antigravity',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      routerConfig: AppRouter.router,
    );
  }

  /// Clears all providers that hold user-scoped backend data.
  /// Called on sign-out AND when a different account signs in, so cached
  /// responses from user A are never served to user B.
  void _clearUserDataCaches() {
    ref.invalidate(bootstrapDataProvider);
    ref.invalidate(homeDataProvider);
    ref.invalidate(calendarDataProvider);
    ref.invalidate(profileOverviewProvider);
    ref.invalidate(activePlanDetailsProvider);
    ref.invalidate(pendingConfirmationsProvider);
  }
}
