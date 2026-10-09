import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/models/running_background.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/api_exception.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/home/data/home_provider.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
import 'package:antigravity_app/features/onboarding/presentation/plan_generation_page.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';

/// V1-HARDEN.0A — discriminating navigation-gate tests for
/// `PlanGenerationPage`. V1-HARDEN.0 found this page unconditionally set
/// `useMockHomeDataProvider = true` and navigated straight to Home,
/// regardless of any real backend call. These tests assert the restored
/// real-flow invariant directly: Home is reached 0 times after a preview
/// failure, and the page never sets the mock-Home flag — it only ever
/// forwards to the real Preview/Long-Horizon-Preview screen, which is the
/// sole gate (tested separately in onboarding_confirm_cleanup_test.dart)
/// that may eventually reach Home, and only after a real successful confirm.
class _FakePlanRepository extends PlanRepository {
  _FakePlanRepository({this.raceError}) : super(ApiClient());

  final Object? raceError;
  int raceCallCount = 0;

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(
      GenerateRacePlanPreviewRequestDto request) async {
    raceCallCount++;
    if (raceError != null) throw raceError!;
    return GeneratePreviewResponse(
      previewId: 'preview-nav-test-1',
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
}

/// A [LongHorizonRepository] double whose preview call always succeeds with
/// a confirmable Long-Horizon contract — used to prove
/// `PlanGenerationPage` routes to the Long-Horizon preview screen (not
/// Home, and not the static preview screen) when the backend selects that
/// strategy.
class _FakeLongHorizonRepository extends LongHorizonRepository {
  _FakeLongHorizonRepository() : super(ApiClient());

  @override
  Future<LongHorizonPlanPreviewContract> generateLongHorizonRacePlanPreview(
      GenerateRacePlanPreviewRequestDto request) async {
    return LongHorizonPlanPreviewContract.fromJson({
      'preview_id': 'lh-preview-nav-test-1',
      'goal_type': 'race',
      'goal_distance': request.goalDistance,
      'total_weeks': 26,
      'start_date': request.startDate,
      'estimated_end_date': request.raceDate,
      'race_date': request.raceDate,
      'current_window_start_week': 1,
      'current_window_end_week': 4,
      'current_executable_week_count': 4,
      'structural_roadmap': <dynamic>[],
      'current_executable_weeks': <dynamic>[],
      'preview_readiness': 'ready_for_public_preview',
      'confirmation_readiness': 'ready_for_rolling_persistence',
      'public_warnings': <dynamic>[],
      'provenance_summary': 'generated_from_initial_profile',
    });
  }
}

void _fillRaceOnboarding(
  ProviderContainer container, {
  required DateTime startDate,
  required String raceDate,
}) {
  final notifier = container.read(onboardingProvider.notifier);
  notifier.updateGoalType('race');
  notifier.updateGoalDistance('ten_k');
  notifier.updateRunningBackground(RunningBackground.intermediate);
  notifier.updateDaysPerWeek(4);
  notifier.updateRaceDetails('Nav Test Race', raceDate);
  notifier.setUserDefinedTarget(3600);
  notifier.updateSelectedRunningDays(['Monday', 'Wednesday', 'Friday', 'Sunday']);
  notifier.updateLongRunDay('Sunday');
  notifier.updateStartDate(startDate);
  notifier.updateRecentWeeklyVolumeKm(30);
  notifier.updateRecentLongestRunKm(12);
}

/// Minimal router carrying all three destinations `PlanGenerationPage` can
/// reach, each rendering a distinguishable placeholder so a test can assert
/// exactly which one (if any) was actually reached.
GoRouter _testRouter() => GoRouter(
      initialLocation: AppRoutes.planGeneration,
      routes: [
        GoRoute(
            path: AppRoutes.planGeneration,
            builder: (_, __) => const PlanGenerationPage()),
        GoRoute(
            path: AppRoutes.planPreview,
            builder: (_, __) => const Scaffold(body: Text('PREVIEW_PLACEHOLDER'))),
        GoRoute(
            path: AppRoutes.longHorizonPlanPreview,
            builder: (_, __) => const Scaffold(body: Text('LONG_HORIZON_PREVIEW_PLACEHOLDER'))),
        GoRoute(
            path: AppRoutes.home,
            builder: (_, __) => const Scaffold(body: Text('HOME_PLACEHOLDER'))),
      ],
    );

/// Builds a [ProviderContainer] with onboarding state already fully
/// populated via [_fillRaceOnboarding] *before* `PlanGenerationPage` is ever
/// built — its `initState` fires `generatePreview()` immediately on first
/// build, so state must exist before `pumpWidget`, not after.
ProviderContainer _readyContainer({
  required DateTime startDate,
  required String raceDate,
  PlanRepository? planRepository,
  LongHorizonRepository? longHorizonRepository,
}) {
  final container = ProviderContainer(overrides: [
    planRepositoryProvider.overrideWithValue(planRepository ?? _FakePlanRepository()),
    longHorizonRepositoryProvider
        .overrideWithValue(longHorizonRepository ?? _FakeLongHorizonRepository()),
  ]);
  _fillRaceOnboarding(container, startDate: startDate, raceDate: raceDate);
  return container;
}

void main() {
  group('PlanGenerationPage — real backend round trip, no mock-Home bypass', () {
    testWidgets(
        'successful static-horizon preview navigates to the real Preview screen, never Home, and never arms the mock-Home flag',
        (tester) async {
      final fakeRepo = _FakePlanRepository();
      final container = _readyContainer(
        startDate: DateTime(2026, 7, 20),
        raceDate: '2026-10-08', // ~12 weeks: static horizon, not long-horizon
        planRepository: fakeRepo,
      );
      addTearDown(container.dispose);

      await tester.binding.setSurfaceSize(const Size(800, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp.router(routerConfig: _testRouter()),
        ),
      );

      await tester.pumpAndSettle(const Duration(seconds: 2));

      // The real preview endpoint was actually called exactly once.
      expect(fakeRepo.raceCallCount, 1);

      // Landed on the real Preview screen — never Home, directly or via the
      // mock flag.
      expect(find.text('PREVIEW_PLACEHOLDER'), findsOneWidget);
      expect(find.text('HOME_PLACEHOLDER'), findsNothing);
      expect(find.byType(PlanGenerationPage), findsNothing);
      expect(container.read(useMockHomeDataProvider), isFalse);

      // The preview actually came from the real backend response, not a
      // fabricated one — a stale/mock preview would not carry this exact id.
      expect(
        container.read(onboardingProvider).previewResponse?.previewId,
        'preview-nav-test-1',
      );
    });

    testWidgets(
        'successful long-horizon preview navigates to the Long-Horizon Preview screen, never Home',
        (tester) async {
      final container = _readyContainer(
        startDate: DateTime(2026, 1, 1),
        raceDate: '2026-10-01', // ~39 weeks: routes to Long-Horizon
      );
      addTearDown(container.dispose);

      await tester.binding.setSurfaceSize(const Size(800, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp.router(routerConfig: _testRouter()),
        ),
      );

      await tester.pumpAndSettle(const Duration(seconds: 2));

      expect(find.text('LONG_HORIZON_PREVIEW_PLACEHOLDER'), findsOneWidget);
      expect(find.text('PREVIEW_PLACEHOLDER'), findsNothing);
      expect(find.text('HOME_PLACEHOLDER'), findsNothing);
      expect(container.read(useMockHomeDataProvider), isFalse);
      expect(container.read(onboardingProvider).isLongHorizonPreview, isTrue);
    });

    testWidgets(
        'backend preview rejection shows the error state, stays on PlanGenerationPage, and never reaches Home or Preview',
        (tester) async {
      final fakeRepo = _FakePlanRepository(
        raceError: const ApiException(
          message: 'This exact combination is not supported yet.',
          errorCode: 'PRODUCT_INELIGIBLE',
          correlationId: 'corr-nav-test',
          statusCode: 422,
        ),
      );
      final container = _readyContainer(
        startDate: DateTime(2026, 7, 20),
        raceDate: '2026-10-08',
        planRepository: fakeRepo,
      );
      addTearDown(container.dispose);

      await tester.binding.setSurfaceSize(const Size(800, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp.router(routerConfig: _testRouter()),
        ),
      );

      await tester.pumpAndSettle(const Duration(seconds: 2));

      expect(fakeRepo.raceCallCount, 1);

      // No navigation anywhere — definitely not to mock/real Home, and not
      // to a Preview screen fabricated from a failed call.
      expect(find.text('HOME_PLACEHOLDER'), findsNothing);
      expect(find.text('PREVIEW_PLACEHOLDER'), findsNothing);
      expect(find.byType(PlanGenerationPage), findsOneWidget);

      // The safe, mapped error message is shown (never the raw exception
      // text/error code/correlation id).
      expect(find.text('Generation Failed'), findsOneWidget);
      expect(
        find.text('This exact combination of level, frequency, and running '
            'history is not supported yet. Try adjusting your level or '
            'weekly frequency.'),
        findsOneWidget,
      );
      expect(find.textContaining('PRODUCT_INELIGIBLE'), findsNothing);
      expect(find.textContaining('corr-nav-test'), findsNothing);

      // The mock-Home bypass this phase removed must never be armed on a
      // failure path either.
      expect(container.read(useMockHomeDataProvider), isFalse);

      // Onboarding answers are preserved exactly — the user can go back and
      // change inputs without re-entering anything (no state mutation on
      // the error path).
      expect(container.read(onboardingProvider).previewResponse, isNull);
      expect(container.read(onboardingProvider).startDate, DateTime(2026, 7, 20));
    });

    testWidgets('Try Again re-invokes the real preview call (no silent fallback, no retry loop)',
        (tester) async {
      final fakeRepo = _FakePlanRepository(
        raceError: const ApiException(
          message: 'transient',
          errorCode: 'PRODUCT_INELIGIBLE',
        ),
      );
      final container = _readyContainer(
        startDate: DateTime(2026, 7, 20),
        raceDate: '2026-10-08',
        planRepository: fakeRepo,
      );
      addTearDown(container.dispose);

      await tester.binding.setSurfaceSize(const Size(800, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp.router(routerConfig: _testRouter()),
        ),
      );

      await tester.pumpAndSettle(const Duration(seconds: 2));
      expect(fakeRepo.raceCallCount, 1);

      await tester.tap(find.text('Try Again'));
      await tester.pumpAndSettle(const Duration(seconds: 2));

      // Exactly one additional real call — not zero (no silent no-op) and
      // not an automatic unbounded retry loop.
      expect(fakeRepo.raceCallCount, 2);
      expect(find.text('HOME_PLACEHOLDER'), findsNothing);
      expect(container.read(useMockHomeDataProvider), isFalse);
    });
  });
}
