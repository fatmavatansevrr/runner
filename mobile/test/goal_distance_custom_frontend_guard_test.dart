import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/onboarding/presentation/race_details_page.dart';
import 'package:antigravity_app/features/onboarding/presentation/custom_goal_page.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
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

  /// Like [pumpPage], but returns the [ProviderContainer] so a test can
  /// inspect [onboardingProvider] state after interacting with the page
  /// (e.g. to prove _onContinue set target_distance_km correctly).
  Future<ProviderContainer> pumpPageWithContainer(WidgetTester tester, Widget page, String path) async {
    final container = ProviderContainer(overrides: [
      planRepositoryProvider.overrideWithValue(_UnreachablePlanRepository()),
      longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
    ]);
    addTearDown(container.dispose);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MaterialApp.router(routerConfig: routerFor(page, path)),
      ),
    );
    await tester.pumpAndSettle();
    return container;
  }

  group('RaceDetailsPage — unsupported free-text distance', () {
    testWidgets('typing a non-preset, non-pilot distance (e.g. 12) disables Continue and shows a guard message', (tester) async {
      await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      // Sanity: the default preset value (10.0, mapped from the default
      // 'ten_k' state) leaves Continue enabled.
      var continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull);

      // PHASE DIST-GEN.3 note: this must NOT be '16' -- exactly 16.0 is now
      // the one approved pilot exact-match value and correctly ENABLES
      // Continue (see the dedicated pilot group below). '12' remains a
      // genuinely unsupported, non-preset, non-pilot value.
      await tester.enterText(find.byType(TextField).at(1), '12');
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

  group('RaceDetailsPage — DIST-GEN.3 exact 16K pilot target', () {
    testWidgets('typing exactly 16.0 enables Continue with no guard message', (tester) async {
      await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      await tester.enterText(find.byType(TextField).at(1), '16');
      await tester.pumpAndSettle();

      final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull,
          reason: 'Exactly 16.0 is the one approved public pilot target and must enable Continue.');
      expect(find.textContaining('isn\'t supported yet'), findsNothing);
    });

    // NOTE (DIST-GEN.7): 15.0 was removed from this "near but blocked" list --
    // it is now its own approved projected target (see the dedicated 15K
    // group below) and would make this test's premise false.
    for (final blocked in ['14.9', '16.09', '18.0']) {
      testWidgets('typing $blocked (near but not exactly an approved target) keeps Continue disabled', (tester) async {
        await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

        await tester.enterText(find.byType(TextField).at(1), blocked);
        await tester.pumpAndSettle();

        final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
        expect(continueButton.onPressed, isNull,
            reason: '$blocked must NOT be treated as an exact approved-target match (tight ~0.001 epsilon, '
                'not the ±0.2 preset-snap band).');
        expect(find.textContaining('isn\'t supported yet'), findsOneWidget);
      });
    }

    testWidgets('submitting exactly 16.0 sets goal_distance=custom and target_distance_km=16.0 in state', (tester) async {
      final container = await pumpPageWithContainer(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      await tester.enterText(find.byType(TextField).at(1), '16');
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ElevatedButton, 'Continue'));
      await tester.pumpAndSettle();

      final state = container.read(onboardingProvider);
      expect(state.goalDistance, 'custom');
      expect(state.targetDistanceKm, 16.0);

      final payload = GenerateRacePlanPreviewRequestDto(
        goalDistance: state.goalDistance,
        targetDistanceKm: state.targetDistanceKm,
        level: 'intermediate',
        daysPerWeek: 4,
        unit: 'km',
        startDate: '2026-07-20',
        preferredDays: const ['mon', 'wed', 'fri', 'sun'],
        longRunDay: 'sun',
        raceDate: '2026-10-12',
        targetFinishTimeSeconds: 5400,
        targetFinishTimeSource: TargetFinishTimeSourceWire.userDefined,
      ).toJson();
      expect(payload['goal_distance'], 'custom');
      expect(payload['target_distance_km'], 16.0);
    });

    testWidgets('submitting a canonical preset never sends target_distance_km', (tester) async {
      final container = await pumpPageWithContainer(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      await tester.enterText(find.byType(TextField).at(1), '10.0');
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ElevatedButton, 'Continue'));
      await tester.pumpAndSettle();

      final state = container.read(onboardingProvider);
      expect(state.goalDistance, 'ten_k');
      expect(state.targetDistanceKm, isNull);

      final payload = GenerateRacePlanPreviewRequestDto(
        goalDistance: state.goalDistance,
        targetDistanceKm: state.targetDistanceKm,
        level: 'intermediate',
        daysPerWeek: 4,
        unit: 'km',
        startDate: '2026-07-20',
        preferredDays: const ['mon', 'wed', 'fri', 'sun'],
        longRunDay: 'sun',
        raceDate: '2026-10-12',
        targetFinishTimeSeconds: 3480,
        targetFinishTimeSource: TargetFinishTimeSourceWire.productAverage,
      ).toJson();
      expect(payload.containsKey('target_distance_km'), isFalse);
    });
  });

  group('RaceDetailsPage — DIST-GEN.7 exact 15K projected target', () {
    testWidgets('typing exactly 15.0 enables Continue with no guard message', (tester) async {
      await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      await tester.enterText(find.byType(TextField).at(1), '15');
      await tester.pumpAndSettle();

      final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull,
          reason: 'Exactly 15.0 is DIST-GEN.7\'s newly approved public projected target and must enable Continue.');
      expect(find.textContaining('isn\'t supported yet'), findsNothing);
    });

    for (final blocked in ['14.9', '15.1', '15.9', '16.1', '16.09', '18.0']) {
      testWidgets('typing $blocked (near but not exactly an approved target) keeps Continue disabled', (tester) async {
        await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

        await tester.enterText(find.byType(TextField).at(1), blocked);
        await tester.pumpAndSettle();

        final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
        expect(continueButton.onPressed, isNull,
            reason: '$blocked must NOT be treated as an exact approved-target match (15.0 or 16.0 only, tight '
                '~0.001 epsilon, not the ±0.2 preset-snap band).');
        expect(find.textContaining('isn\'t supported yet'), findsOneWidget);
      });
    }

    testWidgets('submitting exactly 15.0 sets goal_distance=custom and target_distance_km=15.0 in state', (tester) async {
      final container = await pumpPageWithContainer(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      await tester.enterText(find.byType(TextField).at(1), '15');
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ElevatedButton, 'Continue'));
      await tester.pumpAndSettle();

      final state = container.read(onboardingProvider);
      expect(state.goalDistance, 'custom');
      expect(state.targetDistanceKm, 15.0);

      final payload = GenerateRacePlanPreviewRequestDto(
        goalDistance: state.goalDistance,
        targetDistanceKm: state.targetDistanceKm,
        level: 'intermediate',
        daysPerWeek: 4,
        unit: 'km',
        startDate: '2026-07-20',
        preferredDays: const ['mon', 'wed', 'fri', 'sun'],
        longRunDay: 'sun',
        raceDate: '2026-10-12',
        targetFinishTimeSeconds: 5100,
        targetFinishTimeSource: TargetFinishTimeSourceWire.userDefined,
      ).toJson();
      expect(payload['goal_distance'], 'custom');
      expect(payload['target_distance_km'], 15.0);
    });

    testWidgets('typing exactly 16.0 still enables Continue unchanged (16K zero-delta)', (tester) async {
      await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      await tester.enterText(find.byType(TextField).at(1), '16');
      await tester.pumpAndSettle();

      final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull);
      expect(find.textContaining('isn\'t supported yet'), findsNothing);
    });
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
