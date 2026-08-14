import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/models/preparation_runway.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/features/plan/presentation/plan_details_page.dart';
import 'package:antigravity_app/features/profile/data/profile_repository.dart';

// ── Phase 4H.3 — active-plan (Plan Details) typed-model and UI coverage. ──
//
// Home/Calendar/Training Day Detail were investigated (source-read, not
// rewritten -- see PHASE4H_3_...md §4/§5) and found to already handle
// unrecognized day_type/intensity/status values safely (switch statements
// with default fallbacks) and to carry no hardcoded week-count assumption.
// PlanDetailsPage was the one page found to be entirely disconnected static
// mock content (hardcoded "12 Weeks"/"Week 6 of 12" regardless of the real
// plan) -- this file covers its real rewrite plus the shared typed-model
// getters now available on every active-plan DTO.

Map<String, dynamic> _day({
  required String dayId,
  required String date,
  String dayType = 'easy',
  String status = 'planned',
  double distanceKm = 6.0,
  int durationMin = 35,
  String intensity = 'EASY',
  bool isLongRun = false,
}) =>
    {
      'day_id': dayId,
      'date': date,
      'day_type': dayType,
      'status': status,
      'title': 'Session',
      'description': '',
      'planned_distance_km': distanceKm,
      'planned_duration_min': durationMin,
      'intensity': intensity,
      'is_long_run': isLongRun,
      'can_mark_complete': true,
      'can_mark_not_today': true,
    };

Map<String, dynamic> _week(int weekNumber, String weekType,
        {String? completedStatus}) =>
    {
      'week_id': 'week-$weekNumber',
      'week_number': weekNumber,
      'week_type': weekType,
      'planned_volume_km': 24.0,
      'actual_volume_km': 0.0,
      'is_recovery_week': false,
      'start_date': '2026-07-20',
      'days': [
        _day(
            dayId: 'd$weekNumber-1',
            date: '2026-07-20',
            status: completedStatus ?? 'planned'),
        _day(
            dayId: 'd$weekNumber-2',
            date: '2026-07-22',
            status: completedStatus ?? 'planned'),
      ],
    };

Map<String, dynamic> _planJson(
    {required int runwayWeeks, required int coreWeeks, int? targetSeconds}) {
  // Note: PlanWeekDetailDto (the active-plan Plan Details contract) does not
  // expose a runway_block field at all -- confirmed by contract audit (see
  // PHASE4H_3_...md §5) -- so this fixture only varies week_type, matching
  // what the real backend response can actually contain.
  final corePhases = [
    'base',
    'build',
    'recovery',
    'peak',
    'taper',
    'race_week'
  ];
  final weeks = <Map<String, dynamic>>[];
  for (var i = 0; i < runwayWeeks; i++) {
    weeks.add(_week(i + 1, 'preparation_runway'));
  }
  for (var i = 0; i < coreWeeks; i++) {
    weeks.add(_week(runwayWeeks + i + 1, corePhases[i % corePhases.length]));
  }
  return {
    'has_active_plan': true,
    'plan_id': 'plan-1',
    'status': 'active',
    'goal_type': 'race',
    'goal_distance': 'ten_k',
    'level': 'intermediate',
    'days_per_week': 4,
    'unit': 'km',
    'target_finish_time_seconds': targetSeconds,
    'started_at': '2026-07-20T00:00:00Z',
    'estimated_end_date': '2026-11-20T00:00:00Z',
    'total_weeks': runwayWeeks + coreWeeks,
    'completed_weeks_count': 0,
    'total_planned_distance': 500.0,
    'total_completed_distance': 0.0,
    'weeks': weeks,
  };
}

Map<String, dynamic> _coreOnlyPlanJson(int weeks) =>
    _planJson(runwayWeeks: 0, coreWeeks: weeks);

class _ScriptedProfileRepository extends ProfileRepository {
  _ScriptedProfileRepository(this._plan) : super(ApiClient());
  final PlanDetailsResponse _plan;

  @override
  Future<PlanDetailsResponse> fetchActivePlanDetails() async => _plan;
}

Future<void> _pumpPlanDetails(
    WidgetTester tester, PlanDetailsResponse plan) async {
  await tester.pumpWidget(ProviderScope(
    overrides: [
      profileRepositoryProvider
          .overrideWithValue(_ScriptedProfileRepository(plan))
    ],
    child: const MaterialApp(home: PlanDetailsPage()),
  ));
  await tester.pumpAndSettle();
}

void main() {
  // ── Shared typed-model reuse on active-plan DTOs (PART 3) ──────────────

  group('Active-plan DTO typed getters reuse the shared preview vocabulary',
      () {
    test(
        'PlanWeekDetailDto.weekTypeValue parses preparation_runway and every Core value',
        () {
      expect(
          PlanWeekDetailDto.fromJson(_week(1, 'preparation_runway'))
              .weekTypeValue,
          PreviewWeekType.preparationRunway);
      expect(PlanWeekDetailDto.fromJson(_week(1, 'base')).weekTypeValue,
          PreviewWeekType.base);
      expect(PlanWeekDetailDto.fromJson(_week(1, 'taper')).weekTypeValue,
          PreviewWeekType.taper);
      expect(
          PlanWeekDetailDto.fromJson(_week(1, 'some_future_type'))
              .weekTypeValue,
          PreviewWeekType.unknown);
    });

    test(
        'TrainingDayResponse.intensityDetail/dayTypeValue are safe for runway values',
        () {
      final day = TrainingDayResponse.fromJson(_day(
          dayId: 'd1',
          date: '2026-07-20',
          dayType: 'long_run',
          intensity: 'CONTROLLED_AEROBIC_POWER_PROGRESSED',
          isLongRun: true));
      expect(day.dayTypeValue, PreviewDayType.longRun);
      expect(day.intensityDetail.category,
          WorkoutIntensityCategory.controlledAerobicPowerProgressed);
      expect(
          day.intensityDetail.label, 'Controlled Aerobic Power — Progressed');
    });

    test(
        'TrainingDayDetailResponse and PlanDayDetailDto expose the same typed getters, never a fabricated pace',
        () {
      final detailJson = _day(
          dayId: 'd1',
          date: '2026-07-20',
          intensity: 'CONTROLLED_AEROBIC_POWER_INTRO')
        ..['completed_at'] = null;
      final detail = TrainingDayDetailResponse.fromJson(detailJson);
      expect(detail.intensityDetail.label, 'Controlled Aerobic Power — Intro');
      expect(detail.plannedPaceMinKm,
          isNull); // effort-only session -- no numeric pace

      final planDay = PlanDayDetailDto.fromJson(
          _day(dayId: 'd1', date: '2026-07-20', intensity: 'SURGE_AND_FLOAT'));
      expect(planDay.intensityDetail.label, 'Fartlek — Surge & Float');
    });

    test(
        'unknown active-plan intensity/day-type values parse safely, never crash',
        () {
      final day = TrainingDayResponse.fromJson(_day(
          dayId: 'd1',
          date: '2026-07-20',
          dayType: 'some_future_day_type',
          intensity: 'SOME_FUTURE_INTENSITY'));
      expect(day.dayTypeValue, PreviewDayType.unknown);
      expect(day.intensityDetail.isKnown, isFalse);
      expect(day.intensityDetail.label, isNotEmpty);
    });
  });

  // ── Plan Details page: real data, no hardcoded 12-week mock content ────

  group('PlanDetailsPage — real active-plan data (PART 15)', () {
    testWidgets('15-week runway plan: real total weeks, no 12-week hardcoding',
        (tester) async {
      final plan = PlanDetailsResponse.fromJson(
          _planJson(runwayWeeks: 3, coreWeeks: 12, targetSeconds: 3480));
      await _pumpPlanDetails(tester, plan);

      expect(find.text('15-week plan'), findsOneWidget);
      expect(find.text('15 Weeks'), findsWidgets);
      expect(find.text('12 Weeks'), findsNothing);
      expect(find.textContaining('Week 6 of 12'), findsNothing);
      expect(tester.takeException(), isNull);
    });

    testWidgets(
        '17-week runway plan shows real Preparation/Core composition, week list labeled truthfully',
        (tester) async {
      final plan = PlanDetailsResponse.fromJson(
          _planJson(runwayWeeks: 5, coreWeeks: 12));
      await _pumpPlanDetails(tester, plan);

      expect(find.text('17-week plan'), findsOneWidget);
      expect(find.text('Plan Structure'), findsOneWidget);
      expect(find.text('5 weeks'), findsOneWidget); // Preparation
      expect(find.text('12 weeks'), findsOneWidget); // Race-Specific Core
      expect(find.text('Preparation Runway'), findsWidgets); // week list rows
      expect(find.text('Foundation'), findsWidgets); // first Core week ('base')
      expect(find.text('Week 6'),
          findsOneWidget); // first Core week keeps its true global number
    });

    testWidgets(
        '20-week runway plan: all 20 weeks listed, global numbering intact',
        (tester) async {
      final plan = PlanDetailsResponse.fromJson(
          _planJson(runwayWeeks: 8, coreWeeks: 12));
      await _pumpPlanDetails(tester, plan);

      expect(find.text('20-week plan'), findsOneWidget);
      expect(find.text('20 Weeks'), findsWidgets);
      expect(find.text('Week 20'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets(
        '8-week Core-only plan: no Preparation Runway section, no runway labels',
        (tester) async {
      final plan = PlanDetailsResponse.fromJson(_coreOnlyPlanJson(8));
      await _pumpPlanDetails(tester, plan);

      expect(find.text('8-week plan'), findsOneWidget);
      expect(find.text('Plan Structure'),
          findsNothing); // segment card only shown for a real runway plan
      expect(find.text('Preparation Runway'), findsNothing);
    });

    testWidgets('12-week Core-only plan regression: real data, no mock content',
        (tester) async {
      final plan = PlanDetailsResponse.fromJson(_coreOnlyPlanJson(12));
      await _pumpPlanDetails(tester, plan);

      expect(find.text('12-week plan'), findsOneWidget);
      expect(find.text('12 Weeks'), findsWidgets);
      expect(find.text('Preparation Runway'), findsNothing);
    });

    testWidgets('14-week Core-only plan regression', (tester) async {
      final plan = PlanDetailsResponse.fromJson(_coreOnlyPlanJson(14));
      await _pumpPlanDetails(tester, plan);

      expect(find.text('14-week plan'), findsOneWidget);
      expect(find.text('14 Weeks'), findsWidgets);
    });

    testWidgets(
        'goal time renders "Not set" rather than a fabricated value when absent',
        (tester) async {
      final plan = PlanDetailsResponse.fromJson(
          _planJson(runwayWeeks: 3, coreWeeks: 12, targetSeconds: null));
      await _pumpPlanDetails(tester, plan);

      expect(find.text('Not set'), findsOneWidget);
    });

    testWidgets('no active plan renders a safe empty state, not a crash',
        (tester) async {
      // Per PlanDetailsResponse's own contract, the backend always sends
      // deterministic non-null defaults for every field even when
      // has_active_plan is false -- this fixture matches that shape.
      final plan = PlanDetailsResponse.fromJson({
        'has_active_plan': false,
        'plan_id': '',
        'status': '',
        'goal_type': '',
        'goal_distance': '',
        'level': '',
        'days_per_week': 0,
        'unit': 'km',
        'started_at': '2026-01-01T00:00:00Z',
        'estimated_end_date': '2026-01-01T00:00:00Z',
        'total_weeks': 0,
        'completed_weeks_count': 0,
        'total_planned_distance': 0.0,
        'total_completed_distance': 0.0,
        'weeks': <Map<String, dynamic>>[],
      });
      await _pumpPlanDetails(tester, plan);

      expect(find.text('No active plan.'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  });
}
