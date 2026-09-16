import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/onboarding/presentation/race_details_page.dart';
import 'package:antigravity_app/features/onboarding/presentation/custom_goal_page.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';
import 'support/noop_long_horizon_repository.dart';

/// Never actually invoked by these tests (the whole point of the guard is
/// that Continue is unreachable) -- present only so onboardingProvider's
/// construction doesn't fall through to a real ApiClient()/Firebase-backed
/// PlanRepository in this widget-test sandbox.
class _UnreachablePlanRepository extends PlanRepository {
  _UnreachablePlanRepository() : super(ApiClient());

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(GenerateRacePlanPreviewRequestDto request) =>
      throw StateError('must not be called -- Continue is guarded');

  @override
  Future<GeneratePreviewResponse> generateHabitPlanPreview(GenerateHabitPlanPreviewRequestDto request) =>
      throw StateError('must not be called -- Continue is guarded');
}

/// PHASE DIST-GEN.0.1 -- frontend proof for the smallest honest UX change:
/// neither onboarding screen that can express an arbitrary/unsupported
/// distance may let the user press Continue into a request that now
/// correctly fails closed server-side (GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED).
/// These are pure widget tests -- no network call is made or expected;
/// today's real defect is entirely a client-side "what gets sent" problem
/// (see race_details_page.dart's own _mapDistanceTextToEnum and
/// custom_goal_page.dart's unconditional updateGoalDistance('custom')).
void main() {
  GoRouter routerFor(Widget page, String path) => GoRouter(
        initialLocation: path,
        routes: [
          GoRoute(path: path, builder: (_, __) => page),
          GoRoute(path: AppRoutes.runningBackground, builder: (_, __) => const Scaffold(body: Text('NEXT_PLACEHOLDER'))),
          GoRoute(path: AppRoutes.weeklyFrequency, builder: (_, __) => const Scaffold(body: Text('WEEKLY_FREQ_PLACEHOLDER'))),
          GoRoute(path: AppRoutes.customGoalWithTime, builder: (_, __) => const Scaffold(body: Text('CUSTOM_TIME_PLACEHOLDER'))),
          GoRoute(path: AppRoutes.habitGoal, builder: (_, __) => const Scaffold(body: Text('HABIT_GOAL_PLACEHOLDER'))),
          GoRoute(path: AppRoutes.goalSelection, builder: (_, __) => const Scaffold(body: Text('GOAL_SELECTION_PLACEHOLDER'))),
        ],
      );

  Future<void> pumpPage(WidgetTester tester, Widget page, String path) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          planRepositoryProvider.overrideWithValue(_UnreachablePlanRepository()),
          longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
        ],
        child: MaterialApp.router(routerConfig: routerFor(page, path)),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('RaceDetailsPage — unsupported free-text distance', () {
    testWidgets('typing a non-preset distance (e.g. 16) disables Continue and shows a guard message', (tester) async {
      await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      // Sanity: the default preset value (10.0, mapped from the default
      // 'ten_k' state) leaves Continue enabled.
      var continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull);

      await tester.enterText(find.byType(TextField).at(1), '16');
      await tester.pumpAndSettle();

      continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNull, reason: 'Continue must be disabled for an unsupported distance.');
      expect(find.textContaining('isn\'t supported yet'), findsOneWidget);

      // Recovering to a real preset re-enables Continue -- zero regression
      // for supported distances.
      await tester.enterText(find.byType(TextField).at(1), '10.0');
      await tester.pumpAndSettle();
      continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull);
      expect(find.textContaining('isn\'t supported yet'), findsNothing);
    });

    for (final preset in ['5.0', '10.0', '21.1', '42.2']) {
      testWidgets('canonical preset $preset keeps Continue enabled with no guard message', (tester) async {
        await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);
        await tester.enterText(find.byType(TextField).at(1), preset);
        await tester.pumpAndSettle();

        final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
        expect(continueButton.onPressed, isNotNull);
        expect(find.textContaining('isn\'t supported yet'), findsNothing);
      });
    }
  });

  group('CustomGoalPage — habit "custom goal" always maps to goal_distance=custom', () {
    testWidgets('Continue is always disabled and a guard message is always shown', (tester) async {
      await pumpPage(tester, const CustomGoalPage(), AppRoutes.customGoal);

      final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNull,
          reason: 'custom_goal_page always sends goal_distance=custom regardless of the stepper value, '
              'which the backend now correctly rejects -- Continue must never be reachable here.');
      expect(find.textContaining('Custom distances aren\'t supported yet'), findsOneWidget);

      // Changing the stepper does not change the outcome -- the guard is
      // unconditional for this screen, not value-dependent.
      await tester.tap(find.byIcon(Icons.add));
      await tester.pumpAndSettle();
      final continueButtonAfter = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButtonAfter.onPressed, isNull);
    });
  });
}
