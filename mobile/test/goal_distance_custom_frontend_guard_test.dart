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
///
/// PHASE DERIVED-DIST.4A §29/§30/§36 -- the DIST-GEN.3/.7/.11 enumerated
/// exact-match allowlist ([15.0, 16.0, 18.0], tight ~0.001 epsilon) has been
/// REPLACED by a RANGE-based check: any custom value strictly between
/// canonical FIVE_K (5.0) and canonical HALF_MARATHON (21.0975) is now an
/// ordinary accepted value, mirroring the backend's own actual V1 product
/// contract for the 5K-10K and 10K-HM derived intervals. Every "near but
/// blocked" case from the old exact-match regime (12, 14.9, 15.1, 15.9,
/// 16.09, 16.1, 17.9, 18.1, 19.0, 20.0, ...) is now correctly ACCEPTED --
/// this file's own groups below are rewritten accordingly, not merely
/// patched, since the old tests' entire premise (exact-match-only) no longer
/// holds. The enumerated values below exist ONLY in this test file, per the
/// governing prompt's explicit instruction not to enumerate supported
/// distances in production code.
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

  group('RaceDetailsPage — unsupported free-text distance (outside the 5K-HM range)', () {
    testWidgets('typing a below-minimum distance (e.g. 3) disables Continue and shows a guard message', (tester) async {
      await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      // Sanity: the default preset value (10.0, mapped from the default
      // 'ten_k' state) leaves Continue enabled.
      var continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull);

      await tester.enterText(find.byType(TextField).at(1), '3');
      await tester.pumpAndSettle();

      continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNull, reason: 'Continue must be disabled below the FIVE_K floor.');
      expect(find.textContaining('isn\'t supported yet'), findsOneWidget);

      // Recovering to a real preset re-enables Continue -- zero regression
      // for supported distances.
      await tester.enterText(find.byType(TextField).at(1), '10.0');
      await tester.pumpAndSettle();
      continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull);
      expect(find.textContaining('isn\'t supported yet'), findsNothing);
    });

    for (final blocked in ['0', '-5', '25.0', '30.0']) {
      testWidgets('typing $blocked (outside the V1 range) keeps Continue disabled', (tester) async {
        await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

        await tester.enterText(find.byType(TextField).at(1), blocked);
        await tester.pumpAndSettle();

        final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
        expect(continueButton.onPressed, isNull,
            reason: '$blocked is outside (0, below-minimum] or [above-the-half-marathon-ceiling) and must stay blocked.');
      });
    }

    testWidgets('typing non-numeric text keeps Continue disabled', (tester) async {
      await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      await tester.enterText(find.byType(TextField).at(1), 'abc');
      await tester.pumpAndSettle();

      final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNull);
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

  group('RaceDetailsPage — DERIVED-DIST.4A range-based V1 support (5K < target < Half Marathon)', () {
    // Whole-km values across the full approved range, including every value
    // the OLD exact-match allowlist used to block (12, 14, 17, 19, 20, ...)
    // -- all now ordinary accepted values, with zero enumeration of supported
    // distances in production code.
    for (final accepted in [6.0, 7.0, 8.0, 9.0, 11.0, 12.0, 13.0, 14.0, 17.0, 19.0, 20.0, 20.9]) {
      testWidgets('typing $accepted enables Continue with no guard message', (tester) async {
        await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

        await tester.enterText(find.byType(TextField).at(1), accepted.toString());
        await tester.pumpAndSettle();

        final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
        expect(continueButton.onPressed, isNotNull, reason: '$accepted is inside the 5K-HM range and must be accepted.');
        expect(find.textContaining('isn\'t supported yet'), findsNothing);
      });
    }

    // The historical DIST-GEN.3/.7/.11 pilot values (15.0/16.0/18.0) remain
    // accepted, but now receive NO special treatment -- they are ordinary
    // in-range values, proven here by exercising values immediately around
    // them that the OLD regime would have blocked and the NEW regime accepts.
    for (final formerlyNearMiss in [14.9, 15.1, 15.9, 16.09, 16.1, 17.9, 18.1, 18.01, 18.09]) {
      testWidgets('typing $formerlyNearMiss (blocked under the old exact-match allowlist) now enables Continue', (tester) async {
        await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

        await tester.enterText(find.byType(TextField).at(1), formerlyNearMiss.toString());
        await tester.pumpAndSettle();

        final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
        expect(continueButton.onPressed, isNotNull,
            reason: '$formerlyNearMiss is an ordinary in-range value under the new range-based contract.');
        expect(find.textContaining('isn\'t supported yet'), findsNothing);
      });
    }

    for (final legacyPilot in [15.0, 16.0, 18.0]) {
      testWidgets('historical pilot value $legacyPilot remains accepted with no special status', (tester) async {
        await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

        await tester.enterText(find.byType(TextField).at(1), legacyPilot.toString());
        await tester.pumpAndSettle();

        final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
        expect(continueButton.onPressed, isNotNull);
        expect(find.textContaining('isn\'t supported yet'), findsNothing);
      });
    }

    testWidgets('accepts decimal target 17.7', (tester) async {
      await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);
      await tester.enterText(find.byType(TextField).at(1), '17.7');
      await tester.pumpAndSettle();

      final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(continueButton.onPressed, isNotNull);
    });

    for (final decimalFiveToTenK in ['5.1', '7.5', '9.9']) {
      testWidgets('accepts decimal 5K-10K target $decimalFiveToTenK', (tester) async {
        await pumpPage(tester, const RaceDetailsPage(), AppRoutes.raceDetails);
        await tester.enterText(find.byType(TextField).at(1), decimalFiveToTenK);
        await tester.pumpAndSettle();

        final continueButton = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
        expect(continueButton.onPressed, isNotNull, reason: '$decimalFiveToTenK is inside the 5K-10K derived interval.');
      });
    }

    testWidgets('submitting a range-accepted custom target (e.g. 17.7) sets goal_distance=custom and target_distance_km accordingly', (tester) async {
      final container = await pumpPageWithContainer(tester, const RaceDetailsPage(), AppRoutes.raceDetails);

      await tester.enterText(find.byType(TextField).at(1), '17.7');
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(ElevatedButton, 'Continue'));
      await tester.pumpAndSettle();

      final state = container.read(onboardingProvider);
      expect(state.goalDistance, 'custom');
      expect(state.targetDistanceKm, 17.7);

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
        targetFinishTimeSeconds: 6300,
        targetFinishTimeSource: TargetFinishTimeSourceWire.userDefined,
      ).toJson();
      expect(payload['goal_distance'], 'custom');
      expect(payload['target_distance_km'], 17.7);
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
