import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/models/preparation_runway.dart';
import 'package:antigravity_app/core/network/dtos.dart';

// ── Phase 4H.4 — active-plan provenance DTO/model coverage. ────────────────
//
// The rendering widgets this phase added (_PlanSegmentBadge in home_page.dart,
// the provenance line in calendar_page.dart, _ProvenanceCard in
// training_day_detail_page.dart) are private to their pages and there is no
// pre-existing page-level widget-test harness for Home/Calendar/Detail
// (confirmed in Phase 4H.3 -- still true here, not rebuilt this phase; see
// PHASE4H_4_...md §48). This file instead exhaustively covers the underlying
// model layer those widgets render from -- WeekProvenance/TrainingDaySourceValue
// and every new DTO field's parsing/legacy-fallback/unknown-safety behavior --
// which is where the actual presentation logic (authoritative precedence,
// Core-block-hidden-even-if-erroneously-present, unknown fallbacks) lives.

Map<String, dynamic> _activePlanJson({
  int? currentWeekNumber,
  int? totalWeeks,
  String? currentWeekType,
  String? currentRunwayBlock,
}) =>
    {
      'plan_id': 'plan-1',
      'goal_type': 'race',
      'goal_distance': 'ten_k',
      'level': 'intermediate',
      'progress_text': 'Week 2 of 17',
      if (currentWeekNumber != null) 'current_week_number': currentWeekNumber,
      if (totalWeeks != null) 'total_weeks': totalWeeks,
      if (currentWeekType != null) 'current_week_type': currentWeekType,
      if (currentRunwayBlock != null)
        'current_runway_block': currentRunwayBlock,
    };

Map<String, dynamic> _dayJson({
  String? weekType,
  String? runwayBlock,
  int? weekNumber,
  String dayType = 'easy',
}) =>
    {
      'day_id': 'day-1',
      'date': '2026-07-21',
      'day_type': dayType,
      'status': 'planned',
      'title': 'Easy Run',
      'description': '',
      'planned_distance_km': 6.0,
      'planned_duration_min': 35,
      'is_long_run': false,
      'can_mark_complete': true,
      'can_mark_not_today': true,
      if (weekNumber != null) 'week_number': weekNumber,
      if (weekType != null) 'week_type': weekType,
      if (runwayBlock != null) 'runway_block': runwayBlock,
    };

Map<String, dynamic> _detailJson({
  String? weekType,
  String? runwayBlock,
  int? weekNumber,
  String? source,
  String? adaptedFromId,
}) =>
    {
      'day_id': 'day-1',
      'date': '2026-07-21',
      'day_type': 'easy',
      'status': 'planned',
      'title': 'Easy Run',
      'description': '',
      'planned_distance_km': 6.0,
      'planned_duration_min': 35,
      'is_long_run': false,
      'can_mark_complete': true,
      'can_mark_not_today': true,
      if (weekNumber != null) 'week_number': weekNumber,
      if (weekType != null) 'week_type': weekType,
      if (runwayBlock != null) 'runway_block': runwayBlock,
      if (source != null) 'source': source,
      if (adaptedFromId != null) 'adapted_from_id': adaptedFromId,
    };

void main() {
  // ── WeekProvenance authoritative precedence (PART 3) ────────────────────

  group('WeekProvenance', () {
    test('runway week with a real block: full provenance label', () {
      final p = WeekProvenance(
          weekType: PreviewWeekType.preparationRunway,
          runwayBlockRaw: 'GENERAL_ENDURANCE');
      expect(p.isPreparationRunwayWeek, isTrue);
      expect(p.weekTypeLabel, 'Preparation Runway');
      expect(p.runwayBlockLabel, 'General Endurance');
      expect(p.provenanceLabel, 'Preparation Runway · General Endurance');
    });

    test(
        'Core week: runway block is never trusted even if erroneously present (PART 25)',
        () {
      final p = WeekProvenance(
          weekType: PreviewWeekType.base, runwayBlockRaw: 'AEROBIC_STRENGTH');
      expect(p.isPreparationRunwayWeek, isFalse);
      expect(p.runwayBlockLabel,
          isNull); // gated on weekType, never on runwayBlockRaw presence
      expect(p.weekTypeLabel, 'Foundation');
      expect(p.provenanceLabel, 'Foundation');
    });

    test('runway week with a missing block: safe fallback, never Foundation',
        () {
      final p = WeekProvenance(
          weekType: PreviewWeekType.preparationRunway, runwayBlockRaw: null);
      expect(p.runwayBlockLabel, 'Preparation Block');
      expect(p.runwayBlockLabel, isNot('Foundation'));
    });

    test('unknown week type: safe generic label', () {
      final p = WeekProvenance(
          weekType: PreviewWeekType.unknown, runwayBlockRaw: null);
      expect(p.weekTypeLabel, 'Training Week');
      expect(p.runwayBlockLabel, isNull);
    });
  });

  // ── TrainingDaySourceValue ───────────────────────────────────────────────

  group('TrainingDaySourceValue', () {
    test('all four known values parse distinctly', () {
      expect(TrainingDaySourceValue.fromWire('template'),
          TrainingDaySourceValue.template);
      expect(TrainingDaySourceValue.fromWire('user_override'),
          TrainingDaySourceValue.userOverride);
      expect(TrainingDaySourceValue.fromWire('engine_adapted'),
          TrainingDaySourceValue.engineAdapted);
      expect(TrainingDaySourceValue.fromWire('engine_recovered'),
          TrainingDaySourceValue.engineRecovered);
    });

    test('null and unrecognized values are both safely unknown', () {
      expect(TrainingDaySourceValue.fromWire(null),
          TrainingDaySourceValue.unknown);
      expect(TrainingDaySourceValue.fromWire('some_future_source'),
          TrainingDaySourceValue.unknown);
    });
  });

  // ── ActivePlanSummaryDto (PART 1/6) ──────────────────────────────────────

  group('ActivePlanSummaryDto — Home provenance fields', () {
    test('a real Phase 4G.6D response parses all four typed fields', () {
      final dto = ActivePlanSummaryDto.fromJson(_activePlanJson(
        currentWeekNumber: 2,
        totalWeeks: 17,
        currentWeekType: 'preparation_runway',
        currentRunwayBlock: 'GENERAL_ENDURANCE',
      ));
      expect(dto.currentWeekNumber, 2);
      expect(dto.totalWeeks, 17);
      expect(dto.currentWeekTypeValue, PreviewWeekType.preparationRunway);
      expect(dto.currentWeekProvenance, isNotNull);
      expect(dto.currentWeekProvenance!.provenanceLabel,
          'Preparation Runway · General Endurance');
    });

    test('a Core week response has a null runway block', () {
      final dto = ActivePlanSummaryDto.fromJson(_activePlanJson(
        currentWeekNumber: 8,
        totalWeeks: 17,
        currentWeekType: 'build',
      ));
      expect(dto.currentWeekProvenance!.isPreparationRunwayWeek, isFalse);
      expect(dto.currentWeekProvenance!.runwayBlockLabel, isNull);
      expect(dto.currentWeekProvenance!.weekTypeLabel, 'Build');
    });

    test(
        'legacy response (PART 6/24): missing typed fields parse safely, currentWeekProvenance is null',
        () {
      final dto = ActivePlanSummaryDto.fromJson(_activePlanJson());
      expect(dto.currentWeekNumber, isNull);
      expect(dto.totalWeeks, isNull);
      expect(dto.currentWeekTypeValue, isNull);
      expect(dto.currentWeekProvenance, isNull);
      // progress_text (the legacy fallback source) still parses.
      expect(dto.progressText, 'Week 2 of 17');
    });

    test(
        'direct constructor call sites without the new fields still compile and default to null',
        () {
      final dto = ActivePlanSummaryDto(
        planId: 'p1',
        goalType: 'race',
        goalDistance: 'ten_k',
        level: 'intermediate',
        progressText: 'Week 1 of 12',
      );
      expect(dto.currentWeekNumber, isNull);
      expect(dto.currentWeekProvenance, isNull);
    });
  });

  // ── TrainingDayResponse (Home/Calendar) ──────────────────────────────────

  group('TrainingDayResponse — day-level provenance (PART 1/7)', () {
    test('a real runway day parses week_number/week_type/runway_block', () {
      final day = TrainingDayResponse.fromJson(_dayJson(
          weekNumber: 1,
          weekType: 'preparation_runway',
          runwayBlock: 'AEROBIC_STRENGTH'));
      expect(day.weekNumber, 1);
      expect(day.weekProvenance!.isPreparationRunwayWeek, isTrue);
      expect(day.weekProvenance!.runwayBlockLabel, 'Aerobic Strength');
    });

    test('a Core day never trusts an erroneous runway_block (PART 25)', () {
      final day = TrainingDayResponse.fromJson(_dayJson(
          weekNumber: 8, weekType: 'build', runwayBlock: 'CONSISTENCY'));
      expect(day.weekProvenance!.isPreparationRunwayWeek, isFalse);
      expect(day.weekProvenance!.runwayBlockLabel, isNull);
    });

    test('a legacy/synthetic day (no week fields) has a null weekProvenance',
        () {
      final day = TrainingDayResponse.fromJson(_dayJson());
      expect(day.weekNumber, isNull);
      expect(day.weekProvenance, isNull);
    });

    test('unknown week type on a calendar day is safe', () {
      final day = TrainingDayResponse.fromJson(
          _dayJson(weekNumber: 1, weekType: 'some_future_type'));
      expect(day.weekProvenance!.weekTypeLabel, 'Training Week');
    });
  });

  // ── TrainingDayDetailResponse (Detail) ───────────────────────────────────

  group('TrainingDayDetailResponse — provenance/source (PART 1/10/21)', () {
    test('a real runway day parses provenance, source, and null adaptedFrom',
        () {
      final detail = TrainingDayDetailResponse.fromJson(_detailJson(
        weekNumber: 3,
        weekType: 'preparation_runway',
        runwayBlock: 'AEROBIC_STRENGTH',
        source: 'template',
      ));
      expect(detail.weekNumber, 3);
      expect(detail.weekProvenance!.provenanceLabel,
          'Preparation Runway · Aerobic Strength');
      expect(detail.sourceValue, TrainingDaySourceValue.template);
      expect(detail.hasAdaptedOrigin, isFalse);
    });

    test(
        'an adapted-origin fixture: hasAdaptedOrigin is true, raw ID retained but not the display source',
        () {
      final detail = TrainingDayDetailResponse.fromJson(_detailJson(
        weekNumber: 8,
        weekType: 'build',
        source: 'engine_adapted',
        adaptedFromId: 'original-day-guid',
      ));
      expect(detail.sourceValue, TrainingDaySourceValue.engineAdapted);
      expect(detail.hasAdaptedOrigin, isTrue);
      expect(detail.adaptedFromId,
          'original-day-guid'); // raw ID retained for diagnostics, not shown as UI text
    });

    test('missing pace stays null -- never a fabricated 0:00/km (PART 10)', () {
      final detail = TrainingDayDetailResponse.fromJson(
          _detailJson(weekType: 'preparation_runway', weekNumber: 1));
      expect(detail.plannedPaceMinKm, isNull);
    });

    test(
        'legacy Detail response (no 4G.6D fields): parses safely, weekProvenance/source are null',
        () {
      final detail = TrainingDayDetailResponse.fromJson(_detailJson());
      expect(detail.weekProvenance, isNull);
      expect(detail.sourceValue, TrainingDaySourceValue.unknown);
      expect(detail.hasAdaptedOrigin, isFalse);
    });

    test('unknown source value is safe and distinct from a real value', () {
      final detail = TrainingDayDetailResponse.fromJson(
          _detailJson(source: 'some_future_source'));
      expect(detail.sourceValue, TrainingDaySourceValue.unknown);
      expect(detail.sourceValue.label, isNotEmpty);
    });

    test('direct constructor call sites without the new fields still compile',
        () {
      final detail = TrainingDayDetailResponse(
        dayId: 'd1',
        date: DateTime(2026, 7, 21),
        dayType: 'easy',
        status: 'planned',
        title: 'Easy',
        description: '',
        plannedDistanceKm: 5.0,
        plannedDurationMin: 30,
        isLongRun: false,
        canMarkComplete: true,
        canMarkNotToday: true,
      );
      expect(detail.weekProvenance, isNull);
      expect(detail.sourceValue, TrainingDaySourceValue.unknown);
    });
  });
}
