import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'support/long_horizon_flow_test_harness.dart';

void main() {
  group('real AppRouter protected-route policy', () {
    for (final path in <String>[
      AppRoutes.home,
      AppRoutes.calendar,
      '/training-day/rolling/session-1',
      AppRoutes.longHorizonRegeneratePlan,
      '/training-day/static-day-1',
      AppRoutes.habitGoal,
    ]) {
      testWidgets('unauthenticated $path redirects before protected content',
          (tester) async {
        final auth = AuthNavigationState(
          AuthNavigationStatus.unauthenticated,
        );
        final router = AppRouter.createRouter(
          authState: auth,
          initialLocation: path,
        );
        await pumpLongHorizonFlowApp(
          tester,
          initialLocation: path,
          routerOverride: router,
        );

        expect(
            router.routeInformationProvider.value.uri.path, AppRoutes.welcome);
        expect(find.text('Create a new plan'), findsNothing);
        expect(find.text('Session'), findsNothing);
        router.dispose();
        auth.dispose();
      });
    }

    testWidgets('authenticated rolling Home proceeds through the real router',
        (tester) async {
      final auth = AuthNavigationState(AuthNavigationStatus.authenticated);
      final router = AppRouter.createRouter(
        authState: auth,
        initialLocation: AppRoutes.home,
      );
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.currentWindowInProgress,
          ),
        ));
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        routerOverride: router,
        repository: repo,
      );

      expect(router.routeInformationProvider.value.uri.path, AppRoutes.home);
      expect(find.textContaining('Week 3 of 32'), findsWidgets);
      router.dispose();
      auth.dispose();
    });

    testWidgets(
        'authenticated rolling Calendar proceeds through the real router',
        (tester) async {
      final auth = AuthNavigationState(AuthNavigationStatus.authenticated);
      final router = AppRouter.createRouter(
        authState: auth,
        initialLocation: AppRoutes.calendar,
      );
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.currentWindowInProgress,
          ),
        ));
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.calendar,
        initialCalendarMonth: '2026-08',
        routerOverride: router,
        repository: repo,
      );

      expect(
          router.routeInformationProvider.value.uri.path, AppRoutes.calendar);
      expect(find.text('2026-08'), findsOneWidget);
      router.dispose();
      auth.dispose();
    });

    testWidgets('authenticated rolling detail deep link proceeds',
        (tester) async {
      final auth = AuthNavigationState(AuthNavigationStatus.authenticated);
      const path = '/training-day/rolling/session-1';
      final router = AppRouter.createRouter(
        authState: auth,
        initialLocation: path,
      );
      final repo = ScriptedLongHorizonRepository()
        ..detailById['session-1'] =
            LongHorizonRollingSessionDetailResponse.fromJson(
          {
            'session': rollingSessionJson(
              id: 'session-1',
              date: '2026-08-06',
            ),
            'public_description': 'Easy run',
          },
        );
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: path,
        routerOverride: router,
        repository: repo,
      );

      expect(router.routeInformationProvider.value.uri.path, path);
      expect(find.text('Session'), findsOneWidget);
      router.dispose();
      auth.dispose();
    });

    testWidgets(
        'loading auth state stays on bootstrap and does not loop to auth',
        (tester) async {
      final auth = AuthNavigationState(AuthNavigationStatus.loading);
      final router = AppRouter.createRouter(
        authState: auth,
        initialLocation: AppRoutes.home,
      );
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        routerOverride: router,
        settle: false,
      );

      expect(router.routeInformationProvider.value.uri.path, AppRoutes.splash);
      expect(find.text('Create a new plan'), findsNothing);
      await tester.pumpWidget(const SizedBox.shrink());
      await tester.pump(const Duration(seconds: 2));
      router.dispose();
      auth.dispose();
    });

    testWidgets('auth change re-evaluates once without a redirect loop',
        (tester) async {
      final auth = AuthNavigationState(
        AuthNavigationStatus.unauthenticated,
      );
      final router = AppRouter.createRouter(
        authState: auth,
        initialLocation: AppRoutes.home,
      );
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        routerOverride: router,
      );
      expect(router.routeInformationProvider.value.uri.path, AppRoutes.welcome);

      auth.update(AuthNavigationStatus.authenticated);
      await tester.pumpAndSettle();
      expect(router.routeInformationProvider.value.uri.path, AppRoutes.welcome);
      router.dispose();
      auth.dispose();
    });
  });
}
