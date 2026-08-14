import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/models/running_background.dart';
import 'package:antigravity_app/core/models/recent_race_result.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
import 'package:antigravity_app/features/onboarding/presentation/plan_preview_page.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';
import 'support/noop_long_horizon_repository.dart';

/// A [PlanRepository] double that never touches the network. `generatePreview`
/// always succeeds (so tests can legitimately populate
/// `OnboardingState.previewResponse` through the real code path rather than
/// reaching into notifier-internal state). `confirmPlan` either succeeds or
/// throws [confirmError], depending on what the test is exercising.
class _FakePlanRepository extends PlanRepository {
  _FakePlanRepository({this.confirmError}) : super(ApiClient());

  final Object? confirmError;
  int confirmCallCount = 0;

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(GenerateRacePlanPreviewRequestDto request) async {
    return GeneratePreviewResponse(
      previewId: 'preview-abc-123',
      templateId: 'TEN_K__4D__INTERMEDIATE',
      goalType: 'race',
      goalDistance: request.goalDistance,
      level: request.level,
      daysPerWeek: request.daysPerWeek,
      unit: request.unit,
      weeks: List.generate(
        12,
        (i) => PreviewWeekDto(weekNumber: i + 1, weekType: 'base', days: const []),
      ),
    );
  }

  @override
  Future<GeneratePreviewResponse> generateHabitPlanPreview(GenerateHabitPlanPreviewRequestDto request) async {
    return GeneratePreviewResponse(
      previewId: 'preview-abc-123',
      templateId: 'habit_5k_beginner_3day_km_v1',
      goalType: 'habit',
      goalDistance: request.goalDistance,
      level: request.level,
      daysPerWeek: request.daysPerWeek,
      unit: request.unit,
      weeks: List.generate(
        12,
        (i) => PreviewWeekDto(weekNumber: i + 1, weekType: 'base', days: const []),
      ),
    );
  }

  @override
  Future<ConfirmPlanResponse> confirmPlan(String previewId) async {
    confirmCallCount++;
    if (confirmError != null) {
      throw confirmError!;
    }
    return ConfirmPlanResponse(planId: 'plan-xyz-789', status: 'active');
  }
}

/// Populates a realistic, fully-answered onboarding state (goal, running
/// background, recent-running readiness, recent race result, selected days,
/// start date) and generates a preview through the real code path, using the
/// fake repository so no network call is made.
Future<void> _fillOutOnboarding(ProviderContainer container) async {
  final notifier = container.read(onboardingProvider.notifier);
  notifier.updateGoalType('race');
  notifier.updateGoalDistance('ten_k');
  notifier.updateRunningBackground(RunningBackground.intermediate);
  notifier.updateDaysPerWeek(4);
  notifier.updateRaceDetails('Fall 10K', '2026-10-08');
  notifier.setUserDefinedTarget(3600);
  notifier.updateSelectedRunningDays(['Monday', 'Wednesday', 'Friday', 'Sunday']);
  notifier.updateLongRunDay('Sunday');
  notifier.updateStartDate(DateTime(2026, 7, 20));
  notifier.updateRecentWeeklyVolumeKm(30);
  notifier.updateRecentLongestRunKm(12);
  notifier.updateRecentRaceResult(RecentRaceResult(
    distanceKm: 10,
    finishTimeSeconds: 3200,
    raceDate: DateTime(2026, 5, 1),
  ));
  await notifier.generatePreview();
}

/// Minimal router: PlanPreviewPage plus a placeholder Home so navigation can
/// be asserted without pulling in the real (provider-heavy) HomePage.
GoRouter _testRouter() => GoRouter(
      initialLocation: AppRoutes.planPreview,
      routes: [
        GoRoute(path: AppRoutes.planPreview, builder: (_, __) => const PlanPreviewPage()),
        GoRoute(path: AppRoutes.home, builder: (_, __) => const Scaffold(body: Text('HOME_PLACEHOLDER'))),
      ],
    );

void main() {
  group('Confirm success — onboarding state cleanup', () {
    testWidgets('successful confirm resets onboarding state and lands on Home', (tester) async {
      final fakeRepo = _FakePlanRepository();
      late ProviderContainer container;

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            planRepositoryProvider.overrideWithValue(fakeRepo),
            longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              container = ProviderScope.containerOf(context);
              return MaterialApp.router(routerConfig: _testRouter());
            },
          ),
        ),
      );
      await tester.pumpAndSettle();

      await _fillOutOnboarding(container);
      await tester.pumpAndSettle();

      // Sanity check: state really is populated before confirming.
      final beforeState = container.read(onboardingProvider);
      expect(beforeState.previewResponse, isNotNull);
      expect(beforeState.recentWeeklyVolumeKm, 30);
      expect(beforeState.recentRaceResult, isNotNull);

      await tester.tap(find.text('Looks good, continue'));
      await tester.pumpAndSettle();

      // Confirm was actually called.
      expect(fakeRepo.confirmCallCount, 1);

      // Onboarding state is fully reset — including recent-running/recent-race
      // fields, goal data, selected days, start date, and the preview draft.
      final afterState = container.read(onboardingProvider);
      expect(afterState.previewResponse, isNull);
      expect(afterState.recentWeeklyVolumeKm, isNull);
      expect(afterState.recentLongestRunKm, isNull);
      expect(afterState.recentRaceResult, isNull);
      expect(afterState.goalType, 'habit'); // fresh OnboardingState() default
      expect(afterState.goalDistance, 'five_k');
      expect(afterState.runningBackground, RunningBackground.beginner);
      expect(afterState.selectedRunningDays, isEmpty);
      expect(afterState.startDate, isNull);
      expect(afterState.raceName, isNull);
      expect(afterState.raceDate, isNull);

      // Navigation replaced the stack and landed on Home.
      expect(find.text('HOME_PLACEHOLDER'), findsOneWidget);
      expect(find.byType(PlanPreviewPage), findsNothing);
    });

    testWidgets('failed confirm preserves onboarding state and stays on preview', (tester) async {
      final fakeRepo = _FakePlanRepository(confirmError: Exception('network error'));
      late ProviderContainer container;

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            planRepositoryProvider.overrideWithValue(fakeRepo),
            longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              container = ProviderScope.containerOf(context);
              return MaterialApp.router(routerConfig: _testRouter());
            },
          ),
        ),
      );
      await tester.pumpAndSettle();

      await _fillOutOnboarding(container);
      await tester.pumpAndSettle();

      final beforeState = container.read(onboardingProvider);

      await tester.tap(find.text('Looks good, continue'));
      await tester.pumpAndSettle();

      expect(fakeRepo.confirmCallCount, 1);

      // Every answer is preserved exactly as it was before the failed
      // attempt, so the user can retry without re-entering anything.
      final afterState = container.read(onboardingProvider);
      expect(afterState.previewResponse, isNotNull);
      expect(afterState.previewResponse!.previewId, beforeState.previewResponse!.previewId);
      expect(afterState.recentWeeklyVolumeKm, beforeState.recentWeeklyVolumeKm);
      expect(afterState.recentLongestRunKm, beforeState.recentLongestRunKm);
      expect(afterState.recentRaceResult, beforeState.recentRaceResult);
      expect(afterState.goalType, beforeState.goalType);
      expect(afterState.selectedRunningDays, beforeState.selectedRunningDays);
      expect(afterState.startDate, beforeState.startDate);

      // Still on the preview screen — a safe error is shown, no navigation.
      expect(find.byType(PlanPreviewPage), findsOneWidget);
      expect(find.text('HOME_PLACEHOLDER'), findsNothing);
      expect(find.textContaining('Failed to confirm plan'), findsOneWidget);
    });
  });

  group('OnboardingNotifier.reset()', () {
    test('clears every onboarding answer back to fresh defaults', () {
      final container = ProviderContainer(
        overrides: [
          planRepositoryProvider.overrideWithValue(_FakePlanRepository()),
          longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
        ],
      );
      addTearDown(container.dispose);
      final notifier = container.read(onboardingProvider.notifier);

      notifier.updateGoalType('race');
      notifier.updateGoalDistance('ten_k');
      notifier.updateRunningBackground(RunningBackground.advanced);
      notifier.updateDaysPerWeek(5);
      notifier.updateRaceDetails('Spring Half', '2026-11-01');
      notifier.setUserDefinedTarget(9000);
      notifier.updateLongRunDay('Saturday');
      notifier.updateStartDate(DateTime(2026, 8, 1));
      notifier.updateSelectedRunningDays(['Tuesday', 'Thursday', 'Saturday']);
      notifier.updatePreferredRunDuration(45);
      notifier.updateHabitGoal('ten_k');
      notifier.updateRecentWeeklyVolumeKm(50);
      notifier.updateRecentLongestRunKm(20);
      notifier.updateRecentRaceResult(RecentRaceResult(
        distanceKm: 21.1,
        finishTimeSeconds: 6300,
        raceDate: DateTime(2026, 4, 1),
      ));

      final populated = container.read(onboardingProvider);
      expect(populated.goalType, 'race');
      expect(populated.recentRaceResult, isNotNull);

      notifier.reset();

      final reset = container.read(onboardingProvider);
      final fresh = OnboardingState();
      expect(reset.goalType, fresh.goalType);
      expect(reset.goalDistance, fresh.goalDistance);
      expect(reset.runningBackground, fresh.runningBackground);
      expect(reset.daysPerWeek, fresh.daysPerWeek);
      expect(reset.raceName, isNull);
      expect(reset.raceDate, isNull);
      expect(reset.targetFinishTimeSeconds, isNull);
      expect(reset.targetFinishTimeSource, isNull);
      expect(reset.longRunDay, fresh.longRunDay);
      expect(reset.startDate, isNull);
      expect(reset.selectedRunningDays, isEmpty);
      expect(reset.preferredRunDuration, fresh.preferredRunDuration);
      expect(reset.habitGoal, fresh.habitGoal);
      expect(reset.recentWeeklyVolumeKm, isNull);
      expect(reset.recentLongestRunKm, isNull);
      expect(reset.recentRaceResult, isNull);
      expect(reset.previewResponse, isNull);
    });

    test('post-cancellation "Create a plan" starts from an empty state', () {
      // Mirrors the exact call pattern used after a successful plan
      // cancellation (profile_page.dart): populate state as if from an
      // abandoned prior onboarding attempt, then reset() before the user
      // reaches goal-selection again.
      final container = ProviderContainer(
        overrides: [
          planRepositoryProvider.overrideWithValue(_FakePlanRepository()),
          longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
        ],
      );
      addTearDown(container.dispose);
      final notifier = container.read(onboardingProvider.notifier);

      notifier.updateGoalType('habit');
      notifier.updateRunningBackground(RunningBackground.experienced);
      notifier.updateRecentWeeklyVolumeKm(40);

      notifier.reset();

      final state = container.read(onboardingProvider);
      final fresh = OnboardingState();
      expect(state.goalType, fresh.goalType);
      expect(state.runningBackground, fresh.runningBackground);
      expect(state.recentWeeklyVolumeKm, isNull);
      expect(state.previewResponse, isNull);
    });
  });
}
