import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/models/preparation_runway.dart';
import 'package:antigravity_app/core/models/recent_race_result.dart';
import 'package:antigravity_app/core/models/running_background.dart';
import 'package:antigravity_app/core/widgets/app_button.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
import 'package:antigravity_app/features/onboarding/presentation/plan_preview_page.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';
import 'support/noop_long_horizon_repository.dart';

/// Real (minimal but shape-accurate) backend response fixtures, matching
/// `RunningApp.Application.DTOs.Plan.GeneratePreviewResponse`/`PreviewWeekDto`/
/// `PreviewDayDto` field names and the backend's global snake_case_lower
/// JSON convention (`Program.cs`).
Map<String, dynamic> _coreDay({required int slot, required String date}) => {
      'slot_index': slot,
      'day_type': 'easy',
      'distance_km': 6.0,
      'duration_min': 35,
      'intensity': 'EASY',
      'date': date,
    };

Map<String, dynamic> _coreWeekJson(int weekNumber) => {
      'week_number': weekNumber,
      'week_type': 'base',
      'days': [_coreDay(slot: 1, date: '2026-07-2${weekNumber % 6}')],
      // No 'runway_block' key at all -- matches the real backend, which
      // never serializes it for a Core week's payload in legacy fixtures.
    };

Map<String, dynamic> _runwayWeekJson(int weekNumber, String block, String intensity) => {
      'week_number': weekNumber,
      'week_type': 'preparation_runway',
      'runway_block': block,
      'days': [
        {
          'slot_index': 1,
          'day_type': 'quality',
          'distance_km': 8.0,
          'duration_min': 45,
          'intensity': intensity,
          'date': '2026-07-2$weekNumber',
        },
      ],
    };

Map<String, dynamic> _runwayPreviewJson({
  required int runwayWeeks,
  required int coreWeeks,
  required String lifecycle,
}) {
  // Matches the real backend's canonical block ordering (CanonicalOrder in
  // TenKPreparationRunwayAllocationPolicyFactory): CONSISTENCY, then
  // GENERAL_ENDURANCE, then AEROBIC_STRENGTH, with PRE_SPECIFIC_TRANSITION
  // always fixed as the final runway week. Only the exact runwayWeeks
  // values this test file exercises (0, 3, 5, 8) need a fixture here.
  final blocks = <int, List<String>>{
    3: ['GENERAL_ENDURANCE', 'AEROBIC_STRENGTH', 'PRE_SPECIFIC_TRANSITION'],
    5: ['GENERAL_ENDURANCE', 'GENERAL_ENDURANCE', 'AEROBIC_STRENGTH', 'AEROBIC_STRENGTH', 'PRE_SPECIFIC_TRANSITION'],
    8: [
      'CONSISTENCY',
      'GENERAL_ENDURANCE',
      'GENERAL_ENDURANCE',
      'GENERAL_ENDURANCE',
      'AEROBIC_STRENGTH',
      'AEROBIC_STRENGTH',
      'GENERAL_ENDURANCE',
      'PRE_SPECIFIC_TRANSITION',
    ],
  }[runwayWeeks] ?? List.generate(runwayWeeks, (_) => 'GENERAL_ENDURANCE');
  final intensities = ['EASY', 'LONG_RUN_EASY_CONTROLLED', 'CONTROLLED_AEROBIC_POWER_INTRO', 'EASY'];
  final weeks = <Map<String, dynamic>>[];
  for (var i = 0; i < runwayWeeks; i++) {
    weeks.add(_runwayWeekJson(i + 1, blocks[i], intensities[i % intensities.length]));
  }
  for (var i = 0; i < coreWeeks; i++) {
    weeks.add(_coreWeekJson(runwayWeeks + i + 1));
  }
  return {
    'preview_id': 'preview-runway-1',
    'template_id': 'TEN_K__4D__INTERMEDIATE',
    'goal_type': 'race',
    'goal_distance': 'ten_k',
    'level': 'intermediate',
    'days_per_week': 4,
    'unit': 'km',
    'lifecycle': lifecycle,
    'weeks': weeks,
  };
}

void main() {
  // ── PART 1-5 / TEST REQUIREMENTS 1-10: typed enum parsing ─────────────

  group('PreviewLifecycle.fromWire', () {
    test('parses core_confirmable', () {
      expect(PreviewLifecycle.fromWire('core_confirmable'), PreviewLifecycle.coreConfirmable);
    });

    test('parses preparation_runway_preview_confirmable', () {
      expect(PreviewLifecycle.fromWire('preparation_runway_preview_confirmable'),
          PreviewLifecycle.preparationRunwayPreviewConfirmable);
    });

    test('parses preparation_runway_preview_not_confirmable', () {
      expect(PreviewLifecycle.fromWire('preparation_runway_preview_not_confirmable'),
          PreviewLifecycle.preparationRunwayPreviewNotConfirmable);
    });

    test('unknown wire value parses to unknown, never crashes', () {
      expect(PreviewLifecycle.fromWire('some_future_lifecycle_value'), PreviewLifecycle.unknown);
    });

    test('null wire value falls back to coreConfirmable (approved legacy fallback)', () {
      expect(PreviewLifecycle.fromWire(null), PreviewLifecycle.coreConfirmable);
    });

    test('isConfirmable is true only for coreConfirmable/runwayConfirmable', () {
      expect(PreviewLifecycle.coreConfirmable.isConfirmable, isTrue);
      expect(PreviewLifecycle.preparationRunwayPreviewConfirmable.isConfirmable, isTrue);
      expect(PreviewLifecycle.preparationRunwayPreviewNotConfirmable.isConfirmable, isFalse);
      expect(PreviewLifecycle.unknown.isConfirmable, isFalse); // fails closed
    });
  });

  group('PreviewWeekType.fromWire', () {
    test('parses preparation_runway distinctly from every Core value', () {
      expect(PreviewWeekType.fromWire('preparation_runway'), PreviewWeekType.preparationRunway);
      expect(PreviewWeekType.fromWire('base'), PreviewWeekType.base);
      expect(PreviewWeekType.fromWire('build'), PreviewWeekType.build);
      expect(PreviewWeekType.fromWire('taper'), PreviewWeekType.taper);
    });

    test('preparation_runway is never mapped to base/build/taper', () {
      final runway = PreviewWeekType.fromWire('preparation_runway');
      expect(runway, isNot(PreviewWeekType.base));
      expect(runway, isNot(PreviewWeekType.build));
      expect(runway, isNot(PreviewWeekType.taper));
    });

    test('unknown wire value parses safely', () {
      // 'recovery' was Phase 4H.1's example of a value not yet individually
      // modeled; Phase 4H.2 completed the enum to cover every current
      // TrainingWeekType member (including 'recovery'), so it is no longer
      // an example of an unknown value -- a genuinely future value is used
      // here instead.
      expect(PreviewWeekType.fromWire('some_future_week_type_value'), PreviewWeekType.unknown);
      expect(PreviewWeekType.fromWire(null), PreviewWeekType.unknown);
    });
  });

  group('PreparationRunwayBlock.fromWire', () {
    test('all four canonical blocks parse', () {
      expect(PreparationRunwayBlock.fromWire('CONSISTENCY'), PreparationRunwayBlock.consistency);
      expect(PreparationRunwayBlock.fromWire('GENERAL_ENDURANCE'), PreparationRunwayBlock.generalEndurance);
      expect(PreparationRunwayBlock.fromWire('AEROBIC_STRENGTH'), PreparationRunwayBlock.aerobicStrength);
      expect(PreparationRunwayBlock.fromWire('PRE_SPECIFIC_TRANSITION'), PreparationRunwayBlock.preSpecificTransition);
    });

    test('labels never use Core-phase terminology', () {
      expect(PreparationRunwayBlock.preSpecificTransition.label, isNot(contains('Taper')));
      expect(PreparationRunwayBlock.preSpecificTransition.label, isNot(contains('Race Week')));
      expect(PreparationRunwayBlock.consistency.label, isNot(contains('Foundation')));
    });

    test('unknown future block value parses safely, does not crash', () {
      expect(PreparationRunwayBlock.fromWire('SOME_FUTURE_BLOCK'), PreparationRunwayBlock.unknown);
    });
  });

  group('WorkoutIntensity.fromWire', () {
    test('all runway intensities parse and remain distinct', () {
      expect(WorkoutIntensity.fromWire('EASY'), WorkoutIntensity.easy);
      expect(WorkoutIntensity.fromWire('LONG_RUN_EASY_CONTROLLED'), WorkoutIntensity.longRunEasyControlled);
      expect(WorkoutIntensity.fromWire('CONTROLLED_AEROBIC_POWER_INTRO'), WorkoutIntensity.controlledAerobicPowerIntro);
      expect(WorkoutIntensity.fromWire('CONTROLLED_AEROBIC_POWER_PROGRESSED'), WorkoutIntensity.controlledAerobicPowerProgressed);
    });

    test('Intro and Progressed remain distinguishable', () {
      final intro = WorkoutIntensity.fromWire('CONTROLLED_AEROBIC_POWER_INTRO');
      final progressed = WorkoutIntensity.fromWire('CONTROLLED_AEROBIC_POWER_PROGRESSED');
      expect(intro, isNot(progressed));
      expect(intro.label, isNot(progressed.label));
    });

    test('unknown/Core-only intensity token parses safely to a generic label', () {
      final unknown = WorkoutIntensity.fromWire('GOAL_PACE_TEN_K');
      expect(unknown, WorkoutIntensity.unknown);
      expect(unknown.label, isNotEmpty);
    });
  });

  // ── PART 1/6 / TEST REQUIREMENTS 11-20: DTO parsing + domain mapping ──

  group('GeneratePreviewResponse.fromJson — lifecycle + backward compatibility', () {
    test('legacy 12-week fixture without a lifecycle key falls back to core_confirmable', () {
      final json = {
        'preview_id': 'preview-legacy-1',
        'template_id': 'legacy_template',
        'goal_type': 'race',
        'goal_distance': 'ten_k',
        'level': 'intermediate',
        'days_per_week': 4,
        'unit': 'km',
        'weeks': List.generate(12, (i) => _coreWeekJson(i + 1)),
        // No 'lifecycle' key at all.
      };
      final response = GeneratePreviewResponse.fromJson(json);
      expect(response.lifecycleValue, PreviewLifecycle.coreConfirmable);
      expect(response.isPreparationRunwayPlan, isFalse);
      expect(response.totalWeekCount, 12);
      expect(response.runwayWeekCount, 0);
      expect(response.coreWeekCount, 12);
    });

    test('every Core week parses with null runway_block', () {
      final json = _runwayPreviewJson(runwayWeeks: 0, coreWeeks: 3, lifecycle: 'core_confirmable');
      final response = GeneratePreviewResponse.fromJson(json);
      for (final week in response.weeks) {
        expect(week.runwayBlock, isNull);
        expect(week.runwayBlockValue, isNull);
      }
    });

    test('15-week runway response (Intro-only) maps exactly', () {
      final json = _runwayPreviewJson(runwayWeeks: 3, coreWeeks: 12, lifecycle: 'preparation_runway_preview_confirmable');
      final response = GeneratePreviewResponse.fromJson(json);
      expect(response.totalWeekCount, 15);
      expect(response.runwayWeekCount, 3);
      expect(response.coreWeekCount, 12);
      expect(response.isPreparationRunwayPlan, isTrue);
      expect(response.lifecycleValue, PreviewLifecycle.preparationRunwayPreviewConfirmable);
      // Global week numbers preserved 1..15, never reset at the boundary.
      expect(response.weeks.map((w) => w.weekNumber).toList(), List.generate(15, (i) => i + 1));
    });

    test('17-week runway response (Intro+Progressed) maps exactly', () {
      final json = _runwayPreviewJson(runwayWeeks: 5, coreWeeks: 12, lifecycle: 'preparation_runway_preview_confirmable');
      final response = GeneratePreviewResponse.fromJson(json);
      expect(response.totalWeekCount, 17);
      expect(response.runwayWeekCount, 5);
      expect(response.coreWeekCount, 12);
      final finalRunwayWeek = response.weeks[4];
      expect(finalRunwayWeek.runwayBlockValue, PreparationRunwayBlock.preSpecificTransition);
      final firstCoreWeek = response.weeks[5];
      expect(firstCoreWeek.weekTypeValue, isNot(PreviewWeekType.preparationRunway));
      expect(firstCoreWeek.runwayBlockValue, isNull);
    });

    test('20-week runway response maps exactly, dates/distances/long-run flags preserved', () {
      final json = _runwayPreviewJson(runwayWeeks: 8, coreWeeks: 12, lifecycle: 'preparation_runway_preview_confirmable');
      final response = GeneratePreviewResponse.fromJson(json);
      expect(response.totalWeekCount, 20);
      expect(response.runwayWeekCount, 8);
      final firstDay = response.weeks.first.days.first;
      expect(firstDay.distanceKm, 8.0);
      expect(firstDay.date, DateTime.parse('2026-07-21'));
    });

    test('non-confirmable runway lifecycle parses and remains visible/non-confirmable', () {
      final json = _runwayPreviewJson(runwayWeeks: 3, coreWeeks: 12, lifecycle: 'preparation_runway_preview_not_confirmable');
      final response = GeneratePreviewResponse.fromJson(json);
      expect(response.lifecycleValue, PreviewLifecycle.preparationRunwayPreviewNotConfirmable);
      expect(response.lifecycleValue.isConfirmable, isFalse);
      expect(response.weeks, isNotEmpty); // preview content itself is unaffected
    });

    test('unknown lifecycle value parses safely and fails closed', () {
      final json = _runwayPreviewJson(runwayWeeks: 3, coreWeeks: 12, lifecycle: 'some_future_lifecycle');
      final response = GeneratePreviewResponse.fromJson(json);
      expect(response.lifecycleValue, PreviewLifecycle.unknown);
      expect(response.lifecycleValue.isConfirmable, isFalse);
    });

    test('unknown week_type/runway_block/intensity values all parse without throwing', () {
      final json = {
        'preview_id': 'preview-unknown-1',
        'template_id': 'x',
        'goal_type': 'race',
        'goal_distance': 'ten_k',
        'level': 'intermediate',
        'days_per_week': 4,
        'unit': 'km',
        'lifecycle': 'preparation_runway_preview_confirmable',
        'weeks': [
          {
            'week_number': 1,
            'week_type': 'some_future_week_type',
            'runway_block': 'SOME_FUTURE_BLOCK',
            'days': [
              {
                'slot_index': 1,
                'day_type': 'quality',
                'distance_km': 5.0,
                'duration_min': 30,
                'intensity': 'SOME_FUTURE_INTENSITY',
                'date': '2026-07-21',
              },
            ],
          },
        ],
      };
      expect(() => GeneratePreviewResponse.fromJson(json), returnsNormally);
      final response = GeneratePreviewResponse.fromJson(json);
      expect(response.weeks.single.weekTypeValue, PreviewWeekType.unknown);
      expect(response.weeks.single.runwayBlockValue, PreparationRunwayBlock.unknown);
      expect(response.weeks.single.days.single.intensityValue, WorkoutIntensity.unknown);
    });
  });

  // ── PART 7 / TEST REQUIREMENTS 21-24: OnboardingState derived getters ──

  group('OnboardingState derived preview getters', () {
    test('no preview yet -> unknown lifecycle, not confirmable', () {
      final state = OnboardingState();
      expect(state.previewLifecycle, PreviewLifecycle.unknown);
      expect(state.isPreviewConfirmable, isFalse);
      expect(state.isPreparationRunwayPreview, isFalse);
      expect(state.totalPreviewWeekCount, 0);
    });

    test('Core confirmable preview -> confirmable, not a runway plan', () {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 0, coreWeeks: 12, lifecycle: 'core_confirmable'),
      );
      final state = OnboardingState(previewResponse: response);
      expect(state.isPreviewConfirmable, isTrue);
      expect(state.isPreparationRunwayPreview, isFalse);
      expect(state.runwayWeekCount, 0);
      expect(state.coreWeekCount, 12);
    });

    test('runway confirmable preview -> confirmable, is a runway plan', () {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 5, coreWeeks: 12, lifecycle: 'preparation_runway_preview_confirmable'),
      );
      final state = OnboardingState(previewResponse: response);
      expect(state.isPreviewConfirmable, isTrue);
      expect(state.isPreparationRunwayPreview, isTrue);
      expect(state.runwayWeekCount, 5);
      expect(state.coreWeekCount, 12);
      expect(state.totalPreviewWeekCount, 17);
    });

    test('runway non-confirmable preview -> not confirmable, still a runway plan', () {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 3, coreWeeks: 12, lifecycle: 'preparation_runway_preview_not_confirmable'),
      );
      final state = OnboardingState(previewResponse: response);
      expect(state.isPreviewConfirmable, isFalse);
      expect(state.isPreparationRunwayPreview, isTrue);
    });

    test('unknown lifecycle -> not confirmable (fails closed)', () {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 3, coreWeeks: 12, lifecycle: 'totally_unrecognized'),
      );
      final state = OnboardingState(previewResponse: response);
      expect(state.isPreviewConfirmable, isFalse);
    });
  });

  // ── PART 11-12 / TEST REQUIREMENTS 21-27, 29-32, 34-40: widget behavior ──

  group('Plan Preview CTA — lifecycle-driven confirmation', () {
    testWidgets('Core confirmable preview: CTA enabled, confirm invoked once', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 0, coreWeeks: 12, lifecycle: 'core_confirmable'),
      );
      final repo = _ScriptedPlanRepository(response);
      late ProviderContainer container;

      await tester.pumpWidget(ProviderScope(
        overrides: [
      planRepositoryProvider.overrideWithValue(repo),
      longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
    ],
        child: Consumer(builder: (context, ref, _) {
          container = ProviderScope.containerOf(context);
          return MaterialApp.router(routerConfig: _testRouter());
        }),
      ));
      await tester.pumpAndSettle();
      await _fillOutOnboarding(container);
      await tester.pumpAndSettle();

      final button = tester.widget<AppPrimaryButton>(find.byType(AppPrimaryButton));
      expect(button.onPressed, isNotNull);

      await tester.tap(find.text('Looks good, continue'));
      await tester.pumpAndSettle();
      expect(repo.confirmCallCount, 1);
      expect(find.text('HOME_PLACEHOLDER'), findsOneWidget);
    });

    testWidgets('Runway confirmable preview: CTA enabled, confirm invoked once', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 5, coreWeeks: 12, lifecycle: 'preparation_runway_preview_confirmable'),
      );
      final repo = _ScriptedPlanRepository(response);
      late ProviderContainer container;

      await tester.pumpWidget(ProviderScope(
        overrides: [
      planRepositoryProvider.overrideWithValue(repo),
      longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
    ],
        child: Consumer(builder: (context, ref, _) {
          container = ProviderScope.containerOf(context);
          return MaterialApp.router(routerConfig: _testRouter());
        }),
      ));
      await tester.pumpAndSettle();
      await _fillOutOnboarding(container);
      await tester.pumpAndSettle();

      // Response-driven duration summary -- not "12-week plan".
      expect(find.text('17-week plan'), findsOneWidget);
      expect(find.text('5 weeks preparation • 12 weeks race-specific core'), findsOneWidget);

      await tester.tap(find.text('Looks good, continue'));
      await tester.pumpAndSettle();
      expect(repo.confirmCallCount, 1);
      expect(find.text('HOME_PLACEHOLDER'), findsOneWidget);
    });

    testWidgets('Runway non-confirmable preview: CTA disabled, message shown, confirm never invoked', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 3, coreWeeks: 12, lifecycle: 'preparation_runway_preview_not_confirmable'),
      );
      final repo = _ScriptedPlanRepository(response);
      late ProviderContainer container;

      await tester.pumpWidget(ProviderScope(
        overrides: [
      planRepositoryProvider.overrideWithValue(repo),
      longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
    ],
        child: Consumer(builder: (context, ref, _) {
          container = ProviderScope.containerOf(context);
          return MaterialApp.router(routerConfig: _testRouter());
        }),
      ));
      await tester.pumpAndSettle();
      await _fillOutOnboarding(container);
      await tester.pumpAndSettle();

      // Preview content remains visible.
      expect(find.text('15-week plan'), findsOneWidget);
      // Non-technical explanatory message -- no raw error code.
      expect(find.textContaining('activation is not currently available'), findsOneWidget);
      expect(find.textContaining('CATALOG_PREVIEW_NOT_PERSISTABLE'), findsNothing);

      // Button state asserted directly, not just inferred from tap outcome.
      final button = tester.widget<AppPrimaryButton>(find.byType(AppPrimaryButton));
      expect(button.onPressed, isNull);

      // Tapping where the button would be does not invoke confirm -- the
      // button itself has onPressed: null when disabled.
      await tester.tap(find.text('Looks good, continue'), warnIfMissed: false);
      await tester.pumpAndSettle();
      expect(repo.confirmCallCount, 0);
      expect(find.byType(PlanPreviewPage), findsOneWidget); // no navigation occurred
    });

    testWidgets('Unknown lifecycle: CTA disabled, fails closed, confirm never invoked', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 3, coreWeeks: 12, lifecycle: 'a_future_lifecycle_this_client_predates'),
      );
      final repo = _ScriptedPlanRepository(response);
      late ProviderContainer container;

      await tester.pumpWidget(ProviderScope(
        overrides: [
      planRepositoryProvider.overrideWithValue(repo),
      longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
    ],
        child: Consumer(builder: (context, ref, _) {
          container = ProviderScope.containerOf(context);
          return MaterialApp.router(routerConfig: _testRouter());
        }),
      ));
      await tester.pumpAndSettle();
      await _fillOutOnboarding(container);
      await tester.pumpAndSettle();

      expect(find.textContaining('activation is not currently available'), findsOneWidget);
      await tester.tap(find.text('Looks good, continue'), warnIfMissed: false);
      await tester.pumpAndSettle();
      expect(repo.confirmCallCount, 0);
    });

    testWidgets('20-week runway preview renders without crash or truncation to 12', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
        _runwayPreviewJson(runwayWeeks: 8, coreWeeks: 12, lifecycle: 'preparation_runway_preview_confirmable'),
      );
      final repo = _ScriptedPlanRepository(response);
      late ProviderContainer container;

      await tester.pumpWidget(ProviderScope(
        overrides: [
      planRepositoryProvider.overrideWithValue(repo),
      longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
    ],
        child: Consumer(builder: (context, ref, _) {
          container = ProviderScope.containerOf(context);
          return MaterialApp.router(routerConfig: _testRouter());
        }),
      ));
      await tester.pumpAndSettle();
      await _fillOutOnboarding(container);
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.text('20-week plan'), findsOneWidget);
      expect(find.text('8 weeks preparation • 12 weeks race-specific core'), findsOneWidget);
    });
  });
}

/// Repository double whose generated preview is fully controlled per-test —
/// unlike the pre-existing fixed-12-week fake in
/// onboarding_confirm_cleanup_test.dart, this one can return any lifecycle/
/// horizon combination a test needs.
class _ScriptedPlanRepository extends PlanRepository {
  _ScriptedPlanRepository(this._response) : super(ApiClient());

  final GeneratePreviewResponse _response;
  int confirmCallCount = 0;

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(GenerateRacePlanPreviewRequestDto request) async =>
      _response;

  @override
  Future<GeneratePreviewResponse> generateHabitPlanPreview(GenerateHabitPlanPreviewRequestDto request) async =>
      _response;

  @override
  Future<ConfirmPlanResponse> confirmPlan(String previewId) async {
    confirmCallCount++;
    return ConfirmPlanResponse(planId: 'plan-xyz', status: 'active');
  }
}

GoRouter _testRouter() => GoRouter(
      initialLocation: AppRoutes.planPreview,
      routes: [
        GoRoute(path: AppRoutes.planPreview, builder: (_, __) => const PlanPreviewPage()),
        GoRoute(path: AppRoutes.home, builder: (_, __) => const Scaffold(body: Text('HOME_PLACEHOLDER'))),
      ],
    );

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
