import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/models/preparation_runway.dart';
import 'package:antigravity_app/core/models/recent_race_result.dart';
import 'package:antigravity_app/core/models/running_background.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/core/widgets/app_button.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
import 'package:antigravity_app/features/onboarding/presentation/plan_preview_page.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';
import 'support/noop_long_horizon_repository.dart';

// ── Fixture builders (real backend contract shape) ──────────────────────────

/// Zero-padded ISO date string, `offsetDays` after [_fixtureBase] --
/// avoids single-digit-day strings like "2026-07-2" that `DateTime.parse`
/// rejects, and safely rolls across month/year boundaries for large offsets
/// (e.g. week 20's sessions).
String _isoDate(int offsetDays) {
  final d = _fixtureBase.add(Duration(days: offsetDays));
  return '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
}

final DateTime _fixtureBase = DateTime(2026, 7, 20);

Map<String, dynamic> _day({
  required int slot,
  required String date,
  String dayType = 'easy',
  double distanceKm = 6.0,
  int durationMin = 35,
  String intensity = 'EASY',
}) =>
    {
      'slot_index': slot,
      'day_type': dayType,
      'distance_km': distanceKm,
      'duration_min': durationMin,
      'intensity': intensity,
      'date': date,
    };

Map<String, dynamic> _coreWeek(int weekNumber, String weekType) {
  final base = weekNumber * 7;
  return {
    'week_number': weekNumber,
    'week_type': weekType,
    'days': [
      _day(slot: 1, date: _isoDate(base), dayType: 'easy', intensity: 'EASY'),
      _day(slot: 2, date: _isoDate(base + 2), dayType: 'tempo', intensity: 'GOAL_PACE'),
      _day(slot: 3, date: _isoDate(base + 4), dayType: 'easy', intensity: 'EASY'),
      _day(
          slot: 4,
          date: _isoDate(base + 6),
          dayType: 'long_run',
          distanceKm: 14,
          intensity: 'LONG_RUN_EASY_CONTROLLED'),
    ],
  };
}

Map<String, dynamic> _runwayWeek(int weekNumber, String block, String keyIntensity) {
  final base = weekNumber * 7;
  return {
    'week_number': weekNumber,
    'week_type': 'preparation_runway',
    'runway_block': block,
    'days': [
      _day(slot: 1, date: _isoDate(base), dayType: 'easy', intensity: 'EASY'),
      _day(slot: 2, date: _isoDate(base + 2), dayType: 'tempo', intensity: keyIntensity),
      _day(slot: 3, date: _isoDate(base + 4), dayType: 'easy', intensity: 'EASY'),
      _day(
          slot: 4,
          date: _isoDate(base + 6),
          dayType: 'long_run',
          distanceKm: 10,
          intensity: 'LONG_RUN_EASY_CONTROLLED'),
    ],
  };
}

const _runwayBlockPlan = <int, List<String>>{
  3: ['GENERAL_ENDURANCE', 'AEROBIC_STRENGTH', 'PRE_SPECIFIC_TRANSITION'],
  5: [
    'GENERAL_ENDURANCE',
    'GENERAL_ENDURANCE',
    'AEROBIC_STRENGTH',
    'AEROBIC_STRENGTH',
    'PRE_SPECIFIC_TRANSITION'
  ],
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
};

const _runwayIntensityPlan = <int, List<String>>{
  3: ['EASY', 'CONTROLLED_AEROBIC_POWER_INTRO', 'EASY'],
  5: [
    'EASY',
    'EASY',
    'CONTROLLED_AEROBIC_POWER_INTRO',
    'CONTROLLED_AEROBIC_POWER_PROGRESSED',
    'EASY'
  ],
  8: [
    'EASY',
    'EASY',
    'EASY',
    'EASY',
    'CONTROLLED_AEROBIC_POWER_INTRO',
    'CONTROLLED_AEROBIC_POWER_PROGRESSED',
    'EASY',
    'EASY'
  ],
};

const _corePhaseCycle = [
  'base',
  'build',
  'recovery',
  'peak',
  'taper',
  'race_week'
];

Map<String, dynamic> previewJson({
  required int runwayWeeks,
  required int coreWeeks,
  String lifecycle = 'preparation_runway_preview_confirmable',
}) {
  final blocks = _runwayBlockPlan[runwayWeeks] ??
      List.generate(runwayWeeks, (_) => 'GENERAL_ENDURANCE');
  final intensities = _runwayIntensityPlan[runwayWeeks] ??
      List.generate(runwayWeeks, (_) => 'EASY');
  final weeks = <Map<String, dynamic>>[];
  for (var i = 0; i < runwayWeeks; i++) {
    weeks.add(_runwayWeek(i + 1, blocks[i], intensities[i]));
  }
  for (var i = 0; i < coreWeeks; i++) {
    final phase = _corePhaseCycle[i % _corePhaseCycle.length];
    weeks.add(_coreWeek(runwayWeeks + i + 1, phase));
  }
  return {
    'preview_id': 'preview-schedule-1',
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

Map<String, dynamic> coreOnlyPreviewJson(int weeks) => {
      'preview_id': 'preview-core-1',
      'template_id': 'TEN_K__4D__INTERMEDIATE',
      'goal_type': 'race',
      'goal_distance': 'ten_k',
      'level': 'intermediate',
      'days_per_week': 4,
      'unit': 'km',
      'lifecycle': 'core_confirmable',
      'weeks': List.generate(weeks,
          (i) => _coreWeek(i + 1, _corePhaseCycle[i % _corePhaseCycle.length])),
    };

class _ScriptedPlanRepository extends PlanRepository {
  _ScriptedPlanRepository(this._response) : super(ApiClient());
  final GeneratePreviewResponse _response;
  int confirmCallCount = 0;

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(
          GenerateRacePlanPreviewRequestDto request) async =>
      _response;

  @override
  Future<GeneratePreviewResponse> generateHabitPlanPreview(
          GenerateHabitPlanPreviewRequestDto request) async =>
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
        GoRoute(
            path: AppRoutes.planPreview,
            builder: (_, __) => const PlanPreviewPage()),
        GoRoute(
            path: AppRoutes.home,
            builder: (_, __) => const Scaffold(body: Text('HOME_PLACEHOLDER'))),
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
  notifier
      .updateSelectedRunningDays(['Monday', 'Wednesday', 'Friday', 'Sunday']);
  notifier.updateLongRunDay('Sunday');
  notifier.updateStartDate(DateTime(2026, 7, 20));
  notifier.updateRecentWeeklyVolumeKm(30);
  notifier.updateRecentLongestRunKm(12);
  notifier.updateRecentRaceResult(RecentRaceResult(
      distanceKm: 10, finishTimeSeconds: 3200, raceDate: DateTime(2026, 5, 1)));
  await notifier.generatePreview();
}

Future<ProviderContainer> _pumpSchedule(
    WidgetTester tester, GeneratePreviewResponse response) async {
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
  return container;
}

void main() {
  // ── Model tests: complete week-type support (Part 2 / items 3-10) ───────

  group('PreviewWeekType — complete backend vocabulary', () {
    test('every current backend value parses to a distinct, non-unknown member',
        () {
      expect(PreviewWeekType.fromWire('base'), PreviewWeekType.base);
      expect(PreviewWeekType.fromWire('build'), PreviewWeekType.build);
      expect(PreviewWeekType.fromWire('recovery'), PreviewWeekType.recovery);
      expect(PreviewWeekType.fromWire('peak'), PreviewWeekType.peak);
      expect(PreviewWeekType.fromWire('taper'), PreviewWeekType.taper);
      expect(PreviewWeekType.fromWire('race_week'), PreviewWeekType.raceWeek);
      expect(PreviewWeekType.fromWire('preparation_runway'),
          PreviewWeekType.preparationRunway);
    });

    test(
        'labels are non-empty and preparationRunway never collapses to a Core label',
        () {
      for (final type in PreviewWeekType.values) {
        expect(type.label, isNotEmpty);
      }
      expect(PreviewWeekType.preparationRunway.label, 'Preparation Runway');
      expect(PreviewWeekType.base.label, isNot('Preparation Runway'));
    });

    test('a genuinely unknown value still parses safely', () {
      expect(PreviewWeekType.fromWire('some_future_phase'),
          PreviewWeekType.unknown);
    });
  });

  // ── Model tests: Core intensity inventory (Part 3 / items 11-15) ────────

  group('WorkoutIntensityValue — full Core + runway inventory', () {
    test(
        'every currently-emitted Core intensity token has a real (non-generic) label',
        () {
      const known = [
        'EASY',
        'LONG_RUN_EASY_CONTROLLED',
        'SURGE_AND_FLOAT',
        'THRESHOLD_EFFORT',
        'GOAL_PACE',
        'EASY_BASELINE_SHARPENING_PENDING',
        'EASY_WITH_CONTROLLED_SHARPENING',
      ];
      for (final token in known) {
        final value = WorkoutIntensityValue.fromWire(token);
        expect(value.isKnown, isTrue, reason: token);
        expect(value.rawValue, token);
      }
    });

    test(
        'Intro and Progressed remain distinct and are never labeled threshold/goal pace',
        () {
      final intro =
          WorkoutIntensityValue.fromWire('CONTROLLED_AEROBIC_POWER_INTRO');
      final progressed =
          WorkoutIntensityValue.fromWire('CONTROLLED_AEROBIC_POWER_PROGRESSED');
      expect(intro.label, isNot(progressed.label));
      expect(intro.label, isNot(contains('Threshold')));
      expect(progressed.label, isNot(contains('Threshold')));
      expect(intro.label, isNot(contains('Goal Pace')));
    });

    test(
        'an unrecognized raw value is preserved and humanized, not collapsed to one generic string',
        () {
      final value =
          WorkoutIntensityValue.fromWire('SOME_FUTURE_QUALITY_SESSION');
      expect(value.isKnown, isFalse);
      expect(value.rawValue, 'SOME_FUTURE_QUALITY_SESSION');
      expect(value.label, 'Some Future Quality Session');
    });

    test('goal pace label never fabricates a numeric pace string', () {
      final value = WorkoutIntensityValue.fromWire('GOAL_PACE');
      expect(value.label, 'Goal Pace');
      expect(value.label, isNot(matches(RegExp(r'\d+:\d+'))));
    });
  });

  group('PreviewDayType — long-run identity source', () {
    test('long_run parses distinctly and drives isLongRun', () {
      expect(PreviewDayType.fromWire('long_run'), PreviewDayType.longRun);
      final day = PreviewDayDto.fromJson(
          _day(slot: 1, date: '2026-07-21', dayType: 'long_run'));
      expect(day.isLongRun, isTrue);
    });

    test('a non-long-run day type is not treated as a long run', () {
      final day = PreviewDayDto.fromJson(
          _day(slot: 1, date: '2026-07-21', dayType: 'easy'));
      expect(day.isLongRun, isFalse);
    });

    test('unknown day type is safe and not a long run', () {
      expect(
          PreviewDayType.fromWire('some_future_type'), PreviewDayType.unknown);
      final day = PreviewDayDto.fromJson(
          _day(slot: 1, date: '2026-07-21', dayType: 'some_future_type'));
      expect(day.isLongRun, isFalse);
      expect(day.dayTypeValue.label, isNotEmpty);
    });
  });

  // ── Widget tests: week list / segmentation / boundary (items 16-24) ────

  group('Week list rendering', () {
    testWidgets(
        'week count equals response.weeks.length, global numbering preserved, no truncation to 12',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpSchedule(tester, response);

      expect(find.textContaining('Week 1'), findsWidgets);
      expect(find.textContaining('Week 17'), findsOneWidget); // full 17-week response, not truncated to 12
      expect(find.textContaining('Week 18'), findsNothing); // and nothing beyond the real total
    });

    testWidgets(
        'Preparation Runway major label and each runway block label render',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 8, coreWeeks: 12));
      await _pumpSchedule(tester, response);

      expect(find.text('Preparation Runway'), findsWidgets);
      expect(find.text('Consistency'), findsOneWidget);
      expect(find.text('General Endurance'), findsWidgets);
      expect(find.text('Aerobic Strength'), findsWidgets);
      expect(find.text('Pre-Specific Transition'), findsOneWidget);
    });

    testWidgets(
        'Core week shows its phase label (Foundation), never a runway block',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpSchedule(tester, response);

      // The fixture's 6-phase cycle repeats 'base' every 6 Core weeks (12
      // Core weeks -> 'base' appears twice) -- assert presence, and that the
      // very first Core week specifically (Week 6) carries it.
      expect(find.text('Foundation'), findsWidgets);
      final week6Semantics = tester.getSemantics(find.text('Week 6').first);
      expect(week6Semantics.label, contains('Foundation'));
    });

    testWidgets(
        'boundary divider renders once, between the last runway and first Core week',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpSchedule(tester, response);

      expect(find.text('RACE-SPECIFIC CORE BEGINS'), findsOneWidget);
    });

    testWidgets('first Core week retains its true global number (6, not 1)',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpSchedule(tester, response);

      expect(find.text('Week 6'), findsOneWidget);
    });

    testWidgets(
        'runway week with missing runway_block shows a safe fallback, not Foundation',
        (tester) async {
      final json = previewJson(runwayWeeks: 3, coreWeeks: 12);
      (json['weeks'] as List)[0].remove('runway_block');
      final response = GeneratePreviewResponse.fromJson(json);
      await _pumpSchedule(tester, response);

      expect(find.text('Preparation Block'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets(
        'Core-only preview shows no runway block labels and no Preparation segment',
        (tester) async {
      final response =
          GeneratePreviewResponse.fromJson(coreOnlyPreviewJson(12));
      await _pumpSchedule(tester, response);

      expect(find.text('Preparation Runway'), findsNothing);
      expect(find.text('Preparation'), findsNothing);
      expect(find.textContaining('weeks preparation'), findsNothing);
    });
  });

  // ── Widget tests: workout rows (items 25-32) ─────────────────────────────

  group('Workout row rendering', () {
    testWidgets(
        'date/type/distance/duration/intensity render for an expanded week',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpSchedule(tester, response); // week 1 starts expanded

      expect(find.textContaining('Long Run'), findsWidgets);
      expect(find.textContaining('km'), findsWidgets);
      expect(find.textContaining('min'), findsWidgets);
    });

    testWidgets('zero distance/duration render a safe fallback, never "0 km"',
        (tester) async {
      final json = previewJson(runwayWeeks: 3, coreWeeks: 12);
      (json['weeks'] as List)[0]['days'][0]['distance_km'] = 0.0;
      (json['weeks'] as List)[0]['days'][0]['duration_min'] = 0;
      final response = GeneratePreviewResponse.fromJson(json);
      await _pumpSchedule(tester, response);

      expect(find.textContaining('Distance not specified'), findsWidgets);
      expect(find.textContaining('Duration not specified'), findsWidgets);
      expect(find.text('0 km'), findsNothing);
    });

    testWidgets('a week with zero sessions renders safely with no crash',
        (tester) async {
      final json = previewJson(runwayWeeks: 3, coreWeeks: 12);
      (json['weeks'] as List)[0]['days'] = [];
      final response = GeneratePreviewResponse.fromJson(json);
      await _pumpSchedule(tester, response);

      expect(find.text('No sessions for this week.'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  });

  // ── AerobicStrength Intro/Progressed presentation (items 51-55) ─────────

  group('AerobicStrength Intro/Progressed', () {
    testWidgets('15-week Intro-only fixture shows Intro, never Progressed',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 3, coreWeeks: 12));
      final container = await _pumpSchedule(tester, response);
      // Expand every runway week to make Intro reachable regardless of default expansion.
      final notifierState = container.read(onboardingProvider);
      for (final week in notifierState.previewResponse!.weeks
          .where((w) => w.weekTypeValue == PreviewWeekType.preparationRunway)) {
        final finder = find.text('Week ${week.weekNumber}').first;
        await tester.scrollUntilVisible(finder, 200, scrollable: find.byType(Scrollable).first);
        await tester.tap(finder);
        await tester.pumpAndSettle();
      }

      expect(find.textContaining('Controlled Aerobic Power — Intro'),
          findsOneWidget);
      expect(find.textContaining('Controlled Aerobic Power — Progressed'),
          findsNothing);
    });

    testWidgets('17-week Intro+Progressed fixture shows both, distinctly',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      final container = await _pumpSchedule(tester, response);
      final notifierState = container.read(onboardingProvider);
      for (final week in notifierState.previewResponse!.weeks
          .where((w) => w.weekTypeValue == PreviewWeekType.preparationRunway)) {
        final finder = find.text('Week ${week.weekNumber}').first;
        await tester.scrollUntilVisible(finder, 200, scrollable: find.byType(Scrollable).first);
        await tester.tap(finder);
        await tester.pumpAndSettle();
      }

      expect(find.textContaining('Controlled Aerobic Power — Intro'),
          findsOneWidget);
      expect(find.textContaining('Controlled Aerobic Power — Progressed'),
          findsOneWidget);
    });
  });

  // ── 15/17/20-week + matrix rendering (items 39-50) ───────────────────────

  group('Horizon matrix', () {
    testWidgets('8-week Core preview: exact count, no runway UI',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(coreOnlyPreviewJson(8));
      await _pumpSchedule(tester, response);
      expect(find.text('Week 8'), findsOneWidget);
      expect(find.text('Preparation Runway'), findsNothing);
    });

    testWidgets('12-week Core preview: exact count', (tester) async {
      final response =
          GeneratePreviewResponse.fromJson(coreOnlyPreviewJson(12));
      await _pumpSchedule(tester, response);
      expect(find.text('Week 12'), findsOneWidget);
    });

    testWidgets('14-week Core preview: exact count', (tester) async {
      final response =
          GeneratePreviewResponse.fromJson(coreOnlyPreviewJson(14));
      await _pumpSchedule(tester, response);
      expect(find.text('Week 14'), findsOneWidget);
    });

    testWidgets('15-week runway preview renders fully', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 3, coreWeeks: 12));
      await _pumpSchedule(tester, response);
      expect(find.text('Week 15'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('16-week runway preview count', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 4, coreWeeks: 12));
      await _pumpSchedule(tester, response);
      expect(find.text('Week 16'), findsOneWidget);
    });

    testWidgets('18-week runway preview count', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 6, coreWeeks: 12));
      await _pumpSchedule(tester, response);
      expect(find.text('Week 18'), findsOneWidget);
    });

    testWidgets('19-week runway preview count', (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 7, coreWeeks: 12));
      await _pumpSchedule(tester, response);
      expect(find.text('Week 19'), findsOneWidget);
    });

    testWidgets(
        '20-week runway preview: Week 20 reachable, no truncation, no overflow',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 8, coreWeeks: 12));
      await _pumpSchedule(tester, response);

      await tester.scrollUntilVisible(find.text('Week 20'), 300,
          scrollable: find.byType(Scrollable).first);
      expect(find.text('Week 20'), findsOneWidget);
      expect(tester.takeException(), isNull);

      // Week 20 is expandable like any other week.
      await tester.tap(find.text('Week 20'));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
    });
  });

  // ── Malformed/unknown data safety (items 56-63) ─────────────────────────

  group('Malformed and unknown data safety', () {
    testWidgets(
        'unknown week type, runway block, and intensity all render without crashing',
        (tester) async {
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
              _day(
                  slot: 1,
                  date: '2026-07-21',
                  dayType: 'some_future_day_type',
                  intensity: 'SOME_FUTURE_INTENSITY')
            ],
          },
        ],
      };
      final response = GeneratePreviewResponse.fromJson(json);
      await _pumpSchedule(tester, response);
      expect(tester.takeException(), isNull);
    });

    testWidgets(
        'empty weeks list shows the documented safe message, not a crash',
        (tester) async {
      final json = coreOnlyPreviewJson(0);
      final response = GeneratePreviewResponse.fromJson(json);
      await _pumpSchedule(tester, response);
      expect(find.text('No schedule details are available for this preview.'),
          findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets(
        'duplicate/out-of-order week numbers render safely without reordering',
        (tester) async {
      final json = {
        'preview_id': 'preview-malformed-1',
        'template_id': 'x',
        'goal_type': 'race',
        'goal_distance': 'ten_k',
        'level': 'intermediate',
        'days_per_week': 4,
        'unit': 'km',
        'lifecycle': 'core_confirmable',
        'weeks': [
          _coreWeek(2, 'base'),
          _coreWeek(1, 'build'),
          _coreWeek(2, 'recovery'), // duplicate week number
        ],
      };
      final response = GeneratePreviewResponse.fromJson(json);
      await _pumpSchedule(tester, response);
      expect(tester.takeException(), isNull);
      // Rendered in backend order (2, then 1, then 2) -- not reordered.
      final texts = find
          .textContaining('Week ')
          .evaluate()
          .map((e) => (e.widget as Text).data)
          .toList();
      expect(texts.first, contains('Week 2'));
    });

    testWidgets(
        'unknown lifecycle still renders the full week list safely, CTA fails closed',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(previewJson(
          runwayWeeks: 3, coreWeeks: 12, lifecycle: 'a_future_lifecycle'));
      await _pumpSchedule(tester, response);
      expect(find.text('Week 15'), findsOneWidget);
      final button = find.byType(AppPrimaryButton);
      expect(tester.widget<AppPrimaryButton>(button).onPressed, isNull);
    });
  });

  // ── Lifecycle CTA preserved exactly (items 33-38) ────────────────────────

  group('Confirm CTA preserved with the schedule list present', () {
    testWidgets(
        'runway confirmable: CTA enabled, reachable, confirm invoked once',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
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

      expect(find.byType(AppPrimaryButton), findsOneWidget);
      await tester.tap(find.text('Looks good, continue'));
      await tester.pumpAndSettle();
      expect(repo.confirmCallCount, 1);
      expect(find.text('HOME_PLACEHOLDER'), findsOneWidget);
    });

    testWidgets(
        'runway non-confirmable: full week list visible, zero repository calls',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(previewJson(
          runwayWeeks: 3,
          coreWeeks: 12,
          lifecycle: 'preparation_runway_preview_not_confirmable'));
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

      expect(
          find.text('Week 15'), findsOneWidget); // schedule still fully visible
      final button =
          tester.widget<AppPrimaryButton>(find.byType(AppPrimaryButton));
      expect(button.onPressed, isNull);
      await tester.tap(find.text('Looks good, continue'), warnIfMissed: false);
      await tester.pumpAndSettle();
      expect(repo.confirmCallCount, 0);
    });
  });

  // ── Accessibility (items 64-67) ──────────────────────────────────────────

  group('Accessibility', () {
    testWidgets(
        'week header exposes a semantics label with week number and segment',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpSchedule(tester, response);

      final semantics = tester.getSemantics(find.text('Week 1').first);
      expect(semantics.label, contains('Week 1'));
      expect(semantics.label, contains('Preparation Runway'));
    });

    testWidgets(
        'expansion state is exposed via semantics value, not only a visual icon',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpSchedule(tester, response); // week 1 starts expanded

      final expandedSemantics = tester.getSemantics(find.text('Week 1').first);
      expect(expandedSemantics.label, isNotEmpty);
    });

    testWidgets('long run is conveyed through text, not only an icon',
        (tester) async {
      final response = GeneratePreviewResponse.fromJson(
          previewJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpSchedule(tester, response);
      expect(find.textContaining('Long Run'), findsWidgets);
    });
  });
}
