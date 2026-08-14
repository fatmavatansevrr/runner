// Core DTOs matching backend snake_case request/response structures.

import '../models/preparation_runway.dart';

class BootstrapResponse {
  BootstrapResponse({
    required this.isAuthenticated,
    required this.hasProfile,
    required this.hasActivePlan,
    required this.hasPendingConfirmations,
    required this.nextScreen,
  });

  final bool isAuthenticated;
  final bool hasProfile;
  final bool hasActivePlan;
  final bool hasPendingConfirmations;
  final String nextScreen;

  factory BootstrapResponse.fromJson(Map<String, dynamic> json) {
    return BootstrapResponse(
      isAuthenticated: json['is_authenticated'] ?? false,
      hasProfile: json['has_profile'] ?? false,
      hasActivePlan: json['has_active_plan'] ?? false,
      hasPendingConfirmations: json['has_pending_confirmations'] ?? false,
      nextScreen: json['next_screen'] ?? 'Welcome',
    );
  }
}

/// Nested "recent race" contract — a previously-completed race result,
/// distinct from the target race (`GeneratePreviewRequest.raceName`/
/// `raceDate`/`targetFinishTimeSeconds`). Mapping this object must never
/// write into those target-race fields.
class RecentRaceRequest {
  const RecentRaceRequest({
    required this.distance,
    required this.finishTimeSeconds,
    required this.raceDate,
  });

  /// Canonical goal-distance wire token ("five_k"/"ten_k"/"half_marathon"/"marathon").
  final String distance;
  final int finishTimeSeconds;
  final String raceDate; // yyyy-MM-dd

  Map<String, dynamic> toJson() => {
        'distance': distance,
        'finish_time_seconds': finishTimeSeconds,
        'race_date': raceDate,
      };
}

/// Canonical public wire values for how a race request's target finish
/// time was derived — must be sent atomically with the target time itself
/// (see `OnboardingNotifier.setProductAverageTarget`/`setUserDefinedTarget`).
/// Not a Dart `enum` (this file has no other wire-value enum wrapper
/// convention — every other enum-like field here is a plain lowercase
/// string constant, e.g. `goalType`/`level`), kept consistent with that.
abstract final class TargetFinishTimeSourceWire {
  static const String productAverage = 'product_average';
  static const String userDefined = 'user_defined';
}

/// Public transport DTO for `POST /plans/generate-preview/race`. Contains
/// only race and shared fields — no habit/custom/legacy properties exist on
/// this type at all.
class GenerateRacePlanPreviewRequestDto {
  GenerateRacePlanPreviewRequestDto({
    required this.goalDistance,
    required this.level,
    required this.daysPerWeek,
    required this.unit,
    required this.startDate,
    required this.preferredDays,
    required this.longRunDay,
    required this.raceDate,
    required this.targetFinishTimeSeconds,
    required this.targetFinishTimeSource,
    this.raceName,
    this.recentWeeklyVolumeKm,
    this.recentLongestRunKm,
    this.recentRunsPerWeek,
    this.recentRace,
  });

  final String goalDistance;

  /// Running Background V2 wire value — one of "beginner", "intermediate",
  /// "advanced", "experienced" (see `RunningBackground.wireValue`). New
  /// code must never send a legacy alias.
  final String level;

  final int daysPerWeek;
  final String unit;

  /// Required. The first day of the plan's first 7-day window — it does
  /// not need to be a Monday.
  final String startDate; // yyyy-MM-dd

  /// Required. Canonical lowercase 3-letter weekday tokens (mon..sun),
  /// distinct, length must equal [daysPerWeek].
  final List<String> preferredDays;

  /// Required; must also be a member of [preferredDays].
  final String longRunDay;

  final String raceDate;
  final String? raceName;

  /// Required and positive. Resolved client-side (custom entry or "go with
  /// average") before this request is ever built — the backend never
  /// invents this value.
  final int targetFinishTimeSeconds;

  /// Required. One of [TargetFinishTimeSourceWire.productAverage]/
  /// [TargetFinishTimeSourceWire.userDefined] — always set atomically with
  /// [targetFinishTimeSeconds].
  final String targetFinishTimeSource;

  /// Average weekly distance over the last 4 weeks, in kilometers. `null`
  /// omits the field entirely (backend treats a missing field as
  /// NOT_PROVIDED, distinct from an explicit `0`).
  final double? recentWeeklyVolumeKm;

  /// Longest single run in the last 4 weeks, in kilometers.
  final double? recentLongestRunKm;

  /// Recent typical runs per week.
  final int? recentRunsPerWeek;

  /// Optional previously-completed race result. Always serialized as an
  /// explicit `null` key when absent (not omitted).
  final RecentRaceRequest? recentRace;

  Map<String, dynamic> toJson() {
    return {
      'goal_distance': goalDistance,
      'level': level,
      'days_per_week': daysPerWeek,
      'unit': unit,
      'start_date': startDate,
      'preferred_days': preferredDays,
      'long_run_day': longRunDay,
      'race_date': raceDate,
      if (raceName != null) 'race_name': raceName,
      'target_finish_time_seconds': targetFinishTimeSeconds,
      'target_finish_time_source': targetFinishTimeSource,
      if (recentWeeklyVolumeKm != null)
        'recent_weekly_volume_km': recentWeeklyVolumeKm,
      if (recentLongestRunKm != null)
        'recent_longest_run_km': recentLongestRunKm,
      if (recentRunsPerWeek != null) 'recent_runs_per_week': recentRunsPerWeek,
      'recent_race': recentRace?.toJson(),
    };
  }
}

/// Public transport DTO for `POST /plans/generate-preview/habit`. Contains
/// only habit and shared fields — no race/target-time/recent-race/custom
/// properties exist on this type at all.
class GenerateHabitPlanPreviewRequestDto {
  GenerateHabitPlanPreviewRequestDto({
    required this.goalDistance,
    required this.level,
    required this.daysPerWeek,
    required this.unit,
    required this.startDate,
    required this.preferredDays,
    this.longRunDay,
  });

  final String goalDistance;
  final String level;
  final int daysPerWeek;
  final String unit;
  final String startDate; // yyyy-MM-dd
  final List<String> preferredDays;

  /// Optional for Habit. If provided, must be a member of [preferredDays].
  final String? longRunDay;

  Map<String, dynamic> toJson() {
    return {
      'goal_distance': goalDistance,
      'level': level,
      'days_per_week': daysPerWeek,
      'unit': unit,
      'start_date': startDate,
      'preferred_days': preferredDays,
      if (longRunDay != null) 'long_run_day': longRunDay,
    };
  }
}

class GeneratePreviewResponse {
  GeneratePreviewResponse({
    required this.previewId,
    required this.templateId,
    required this.goalType,
    required this.goalDistance,
    required this.level,
    required this.daysPerWeek,
    required this.unit,
    required this.weeks,
    // Phase 4H.1: optional, defaults to the approved backward-compatible
    // fallback (see PreviewLifecycle.fromWire) so every pre-existing
    // direct-constructor call site (tests included) keeps compiling and
    // behaving as "core confirmable" without being touched.
    this.lifecycle = 'core_confirmable',
  });

  final String previewId;
  final String templateId;
  final String goalType;
  final String goalDistance;
  final String level;
  final int daysPerWeek;
  final String unit;
  final List<PreviewWeekDto> weeks;

  /// Raw backend wire value (e.g. "core_confirmable"). Kept alongside the
  /// typed [lifecycleValue] accessor so no information is lost in transit —
  /// widgets should read [lifecycleValue], not this field, directly.
  final String lifecycle;

  /// The authoritative, safe-by-default typed lifecycle. Never derived from
  /// [weeks].length or any other field — see [PreviewLifecycle.fromWire].
  PreviewLifecycle get lifecycleValue => PreviewLifecycle.fromWire(lifecycle);

  /// True when at least one week is a Preparation Runway week (per its typed
  /// week type, not inferred from week count).
  bool get isPreparationRunwayPlan =>
      weeks.any((w) => w.weekTypeValue == PreviewWeekType.preparationRunway);

  int get runwayWeekCount => weeks
      .where((w) => w.weekTypeValue == PreviewWeekType.preparationRunway)
      .length;

  int get coreWeekCount => weeks.length - runwayWeekCount;

  int get totalWeekCount => weeks.length;

  factory GeneratePreviewResponse.fromJson(Map<String, dynamic> json) {
    return GeneratePreviewResponse(
      previewId: json['preview_id'] ?? '',
      templateId: json['template_id'] ?? '',
      goalType: json['goal_type'] ?? '',
      goalDistance: json['goal_distance'] ?? '',
      level: json['level'] ?? '',
      daysPerWeek: json['days_per_week'] ?? 0,
      unit: json['unit'] ?? 'km',
      weeks: (json['weeks'] as List? ?? [])
          .map((e) => PreviewWeekDto.fromJson(e as Map<String, dynamic>))
          .toList(),
      // Missing key (legacy fixture) -> null -> PreviewLifecycle.fromWire
      // maps null to coreConfirmable, the approved fallback (PART 1).
      lifecycle: json['lifecycle'] as String? ?? 'core_confirmable',
    );
  }
}

class PreviewWeekDto {
  PreviewWeekDto({
    required this.weekNumber,
    required this.weekType,
    required this.days,
    this.runwayBlock,
  });

  final int weekNumber;
  final String weekType;
  final List<PreviewDayDto> days;

  /// Raw backend wire value (e.g. "CONSISTENCY"). Always null for a Core
  /// week — never omitted vs. explicit-null distinction needed here since
  /// the backend itself always serializes it as a JSON `null` for Core
  /// weeks (System.Text.Json default nullable-property behavior).
  final String? runwayBlock;

  PreviewWeekType get weekTypeValue => PreviewWeekType.fromWire(weekType);

  /// `null` for a Core week (matches [runwayBlock] being null). For a
  /// runway week, [PreparationRunwayBlock.unknown] if the raw value doesn't
  /// match one of the four known blocks (future backend addition) rather
  /// than treating it as "no block" — a runway week always has *some*
  /// block, so `null` here would misrepresent it as a Core week.
  PreparationRunwayBlock? get runwayBlockValue =>
      runwayBlock == null ? null : PreparationRunwayBlock.fromWire(runwayBlock);

  factory PreviewWeekDto.fromJson(Map<String, dynamic> json) {
    return PreviewWeekDto(
      weekNumber: json['week_number'] ?? 0,
      weekType: json['week_type'] ?? '',
      days: (json['days'] as List? ?? [])
          .map((e) => PreviewDayDto.fromJson(e as Map<String, dynamic>))
          .toList(),
      runwayBlock: json['runway_block'] as String?,
    );
  }
}

class PreviewDayDto {
  PreviewDayDto({
    required this.slotIndex,
    required this.dayType,
    required this.distanceKm,
    required this.durationMin,
    required this.intensity,
    required this.date,
  });

  final int slotIndex;
  final String dayType;
  final double distanceKm;
  final int durationMin;
  final String intensity;
  final DateTime date;

  WorkoutIntensity get intensityValue => WorkoutIntensity.fromWire(intensity);

  /// Phase 4H.2 — the richer typed+raw wrapper (see [WorkoutIntensityValue])
  /// used by the new schedule UI; preserves the original backend token for
  /// display even when [WorkoutIntensityCategory] doesn't yet have a
  /// dedicated member for it.
  WorkoutIntensityValue get intensityDetail =>
      WorkoutIntensityValue.fromWire(intensity);

  PreviewDayType get dayTypeValue => PreviewDayType.fromWire(dayType);

  /// Authoritative long-run identity, per PART 9: `PreviewDayDto` carries no
  /// separate `is_long_run` flag, so this reads the formal `day_type`
  /// member (`TrainingDayType.LongRun` -> `"long_run"`) rather than
  /// inferring it from slot position.
  bool get isLongRun => dayTypeValue == PreviewDayType.longRun;

  factory PreviewDayDto.fromJson(Map<String, dynamic> json) {
    return PreviewDayDto(
      slotIndex: json['slot_index'] ?? 0,
      dayType: json['day_type'] ?? '',
      distanceKm: (json['distance_km'] as num? ?? 0.0).toDouble(),
      durationMin: json['duration_min'] ?? 0,
      intensity: json['intensity'] ?? '',
      date: DateTime.parse(json['date']),
    );
  }
}

class ConfirmPlanRequest {
  ConfirmPlanRequest({required this.previewId});
  final String previewId;

  Map<String, dynamic> toJson() => {'preview_id': previewId};
}

class ConfirmPlanResponse {
  ConfirmPlanResponse({required this.planId, required this.status});
  final String planId;
  final String status;

  factory ConfirmPlanResponse.fromJson(Map<String, dynamic> json) {
    return ConfirmPlanResponse(
      planId: json['plan_id'] ?? '',
      status: json['status'] ?? '',
    );
  }
}

class CancelPlanRequest {
  CancelPlanRequest({required this.reason});
  final String reason;

  Map<String, dynamic> toJson() => {'reason': reason};
}

class CancelPlanResponse {
  CancelPlanResponse({required this.planId, required this.status});
  final String planId;
  final String status;

  factory CancelPlanResponse.fromJson(Map<String, dynamic> json) {
    return CancelPlanResponse(
      planId: json['plan_id'] ?? '',
      status: json['status'] ?? '',
    );
  }
}

class HomeResponse {
  HomeResponse({
    this.activePlan,
    this.todayWorkout,
    this.dailyTip,
    required this.weekSummary,
    required this.hasPendingConfirmations,
  });

  final ActivePlanSummaryDto? activePlan;
  final TrainingDayResponse? todayWorkout;
  final DailyTipResponse? dailyTip;
  final List<TrainingDayResponse> weekSummary;
  final bool hasPendingConfirmations;

  factory HomeResponse.fromJson(Map<String, dynamic> json) {
    return HomeResponse(
      activePlan: json['active_plan'] != null
          ? ActivePlanSummaryDto.fromJson(
              json['active_plan'] as Map<String, dynamic>)
          : null,
      todayWorkout: json['today_workout'] != null
          ? TrainingDayResponse.fromJson(
              json['today_workout'] as Map<String, dynamic>)
          : null,
      dailyTip: json['daily_tip'] != null
          ? DailyTipResponse.fromJson(json['daily_tip'] as Map<String, dynamic>)
          : null,
      weekSummary: (json['week_summary'] as List? ?? [])
          .map((e) => TrainingDayResponse.fromJson(e as Map<String, dynamic>))
          .toList(),
      hasPendingConfirmations: json['has_pending_confirmations'] ?? false,
    );
  }
}

class ActivePlanSummaryDto {
  ActivePlanSummaryDto({
    required this.planId,
    required this.goalType,
    required this.goalDistance,
    required this.level,
    required this.progressText,
    // Phase 4H.4: additive, optional, default null -- every pre-existing
    // direct-constructor call site (tests included) keeps compiling
    // unchanged. See PART 6/24: a null currentWeekNumber/currentWeekType is
    // the documented legacy-fallback signal -- callers fall back to parsing
    // progressText only in that case, never when typed fields are present.
    this.currentWeekNumber,
    this.totalWeeks,
    this.currentWeekType,
    this.currentRunwayBlock,
  });

  final String planId;
  final String goalType;
  final String goalDistance;
  final String level;
  final String progressText;

  /// Backend Phase 4G.6D — authoritative, entity-derived. Null only for a
  /// legacy response (predating 4G.6D) or a plan with no persisted
  /// TrainingWeek rows.
  final int? currentWeekNumber;
  final int? totalWeeks;
  final String? currentWeekType;
  final String? currentRunwayBlock;

  PreviewWeekType? get currentWeekTypeValue => currentWeekType == null
      ? null
      : PreviewWeekType.fromWire(currentWeekType);

  /// See [WeekProvenance] — null for a legacy response; for a real 4G.6D
  /// response, never trusts [currentRunwayBlock] unless [currentWeekTypeValue]
  /// itself says this is a runway week.
  WeekProvenance? get currentWeekProvenance => currentWeekTypeValue == null
      ? null
      : WeekProvenance(
          weekType: currentWeekTypeValue!, runwayBlockRaw: currentRunwayBlock);

  factory ActivePlanSummaryDto.fromJson(Map<String, dynamic> json) {
    return ActivePlanSummaryDto(
      planId: json['plan_id'] ?? '',
      goalType: json['goal_type'] ?? '',
      goalDistance: json['goal_distance'] ?? '',
      level: json['level'] ?? '',
      progressText: json['progress_text'] ?? '',
      currentWeekNumber: json['current_week_number'] as int?,
      totalWeeks: json['total_weeks'] as int?,
      currentWeekType: json['current_week_type'] as String?,
      currentRunwayBlock: json['current_runway_block'] as String?,
    );
  }
}

class TrainingDayResponse {
  TrainingDayResponse({
    this.dayId,
    required this.date,
    required this.dayType,
    required this.status,
    required this.title,
    required this.description,
    required this.plannedDistanceKm,
    required this.plannedDurationMin,
    this.plannedPaceMinKm,
    this.intensity,
    this.actualDistanceKm,
    this.actualDurationMin,
    required this.isLongRun,
    required this.canMarkComplete,
    required this.canMarkNotToday,
    // Phase 4H.4: additive, optional, default null.
    this.weekNumber,
    this.weekType,
    this.runwayBlock,
  });

  /// Null for a synthetic rest/no-session day not backed by a persisted
  /// TrainingDay row (e.g. a calendar gap). Never a placeholder GUID.
  final String? dayId;
  final DateTime date;
  final String dayType;
  final String status;
  final String title;
  final String description;
  final double plannedDistanceKm;
  final int plannedDurationMin;
  final double? plannedPaceMinKm;
  final String? intensity;
  final double? actualDistanceKm;
  final int? actualDurationMin;
  final bool isLongRun;
  final bool canMarkComplete;
  final bool canMarkNotToday;

  /// Backend Phase 4G.6D — null for a synthetic (non-persisted) rest day or
  /// a legacy response predating this field.
  final int? weekNumber;
  final String? weekType;
  final String? runwayBlock;

  /// Phase 4H.3 — reuses the shared typed vocabulary from
  /// `preparation_runway.dart` rather than a second parallel label system.
  WorkoutIntensityValue get intensityDetail =>
      WorkoutIntensityValue.fromWire(intensity);

  /// Phase 4H.4 — see [ActivePlanSummaryDto.currentWeekProvenance]; null
  /// only when [weekType] itself is absent (legacy response or synthetic day).
  WeekProvenance? get weekProvenance => weekType == null
      ? null
      : WeekProvenance(
          weekType: PreviewWeekType.fromWire(weekType),
          runwayBlockRaw: runwayBlock);
  PreviewDayType get dayTypeValue => PreviewDayType.fromWire(dayType);

  factory TrainingDayResponse.fromJson(Map<String, dynamic> json) {
    return TrainingDayResponse(
      dayId: json['day_id'] as String?,
      date: DateTime.parse(json['date']),
      dayType: json['day_type'] ?? '',
      status: json['status'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      plannedDistanceKm:
          (json['planned_distance_km'] as num? ?? 0.0).toDouble(),
      plannedDurationMin: json['planned_duration_min'] ?? 0,
      plannedPaceMinKm: (json['planned_pace_min_km'] as num?)?.toDouble(),
      intensity: json['intensity'],
      actualDistanceKm: (json['actual_distance_km'] as num?)?.toDouble(),
      actualDurationMin: json['actual_duration_min'] as int?,
      isLongRun: json['is_long_run'] ?? false,
      canMarkComplete: json['can_mark_complete'] ?? false,
      canMarkNotToday: json['can_mark_not_today'] ?? false,
      weekNumber: json['week_number'] as int?,
      weekType: json['week_type'] as String?,
      runwayBlock: json['runway_block'] as String?,
    );
  }
}

class DailyTipResponse {
  DailyTipResponse({
    required this.tipKey,
    required this.title,
    required this.message,
    this.workoutType,
  });

  final String tipKey;
  final String title;
  final String message;
  final String? workoutType;

  factory DailyTipResponse.fromJson(Map<String, dynamic> json) {
    return DailyTipResponse(
      tipKey: json['tip_key'] ?? '',
      title: json['title'] ?? '',
      message: json['message'] ?? '',
      workoutType: json['workout_type'],
    );
  }
}

class TrainingDayDetailResponse {
  TrainingDayDetailResponse({
    required this.dayId,
    required this.date,
    required this.dayType,
    required this.status,
    required this.title,
    required this.description,
    required this.plannedDistanceKm,
    required this.plannedDurationMin,
    this.plannedPaceMinKm,
    this.intensity,
    this.actualDistanceKm,
    this.actualDurationMin,
    required this.isLongRun,
    required this.canMarkComplete,
    required this.canMarkNotToday,
    this.completedAt,
    // Phase 4H.4: additive, optional, default null.
    this.weekNumber,
    this.weekType,
    this.runwayBlock,
    this.source,
    this.adaptedFromId,
  });

  final String dayId;
  final DateTime date;
  final String dayType;
  final String status;
  final String title;
  final String description;
  final double plannedDistanceKm;
  final int plannedDurationMin;
  final double? plannedPaceMinKm;
  final String? intensity;
  final double? actualDistanceKm;
  final int? actualDurationMin;
  final bool isLongRun;
  final bool canMarkComplete;
  final bool canMarkNotToday;
  final DateTime? completedAt;

  /// Backend Phase 4G.6D — additive provenance/origin fields.
  final int? weekNumber;
  final String? weekType;
  final String? runwayBlock;
  final String? source;

  /// Kept as a raw String (matching this DTO's existing style — every other
  /// ID field in this file is a `String`, not a `Guid`/`Uuid` wrapper type),
  /// never rendered prominently in the UI (see PART 11 — represented only as
  /// "Adapted from an earlier workout" when non-null, not the raw ID).
  final String? adaptedFromId;

  /// Phase 4H.3 — see `TrainingDayResponse.intensityDetail`/`dayTypeValue`.
  WorkoutIntensityValue get intensityDetail =>
      WorkoutIntensityValue.fromWire(intensity);
  PreviewDayType get dayTypeValue => PreviewDayType.fromWire(dayType);

  /// Phase 4H.4 — see [ActivePlanSummaryDto.currentWeekProvenance].
  WeekProvenance? get weekProvenance => weekType == null
      ? null
      : WeekProvenance(
          weekType: PreviewWeekType.fromWire(weekType),
          runwayBlockRaw: runwayBlock);

  TrainingDaySourceValue get sourceValue =>
      TrainingDaySourceValue.fromWire(source);

  bool get hasAdaptedOrigin => adaptedFromId != null;

  factory TrainingDayDetailResponse.fromJson(Map<String, dynamic> json) {
    return TrainingDayDetailResponse(
      dayId: json['day_id'] ?? '',
      date: DateTime.parse(json['date']),
      dayType: json['day_type'] ?? '',
      status: json['status'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      plannedDistanceKm:
          (json['planned_distance_km'] as num? ?? 0.0).toDouble(),
      plannedDurationMin: json['planned_duration_min'] ?? 0,
      plannedPaceMinKm: (json['planned_pace_min_km'] as num?)?.toDouble(),
      intensity: json['intensity'],
      actualDistanceKm: (json['actual_distance_km'] as num?)?.toDouble(),
      actualDurationMin: json['actual_duration_min'] as int?,
      isLongRun: json['is_long_run'] ?? false,
      canMarkComplete: json['can_mark_complete'] ?? false,
      canMarkNotToday: json['can_mark_not_today'] ?? false,
      completedAt: json['completed_at'] != null
          ? DateTime.parse(json['completed_at'])
          : null,
      weekNumber: json['week_number'] as int?,
      weekType: json['week_type'] as String?,
      runwayBlock: json['runway_block'] as String?,
      source: json['source'] as String?,
      adaptedFromId: json['adapted_from_id'] as String?,
    );
  }
}

class CompleteWorkoutRequest {
  CompleteWorkoutRequest({
    required this.actualDistanceKm,
    required this.actualDurationMin,
    this.userNote,
  });

  final double actualDistanceKm;
  final int actualDurationMin;
  final String? userNote;

  Map<String, dynamic> toJson() {
    return {
      'actual_distance_km': actualDistanceKm,
      'actual_duration_min': actualDurationMin,
      if (userNote != null) 'user_note': userNote,
    };
  }
}

class CompleteWorkoutResponse {
  CompleteWorkoutResponse({required this.dayId, required this.status});
  final String dayId;
  final String status;

  factory CompleteWorkoutResponse.fromJson(Map<String, dynamic> json) {
    return CompleteWorkoutResponse(
      dayId: json['day_id'] ?? '',
      status: json['status'] ?? '',
    );
  }
}

class CreateNotTodayDecisionRequest {
  CreateNotTodayDecisionRequest({required this.reason});
  final String reason;

  Map<String, dynamic> toJson() => {'reason': reason};
}

class CreateNotTodayDecisionResponse {
  CreateNotTodayDecisionResponse(
      {required this.decisionId, required this.status});
  final String decisionId;
  final String status;

  factory CreateNotTodayDecisionResponse.fromJson(Map<String, dynamic> json) {
    return CreateNotTodayDecisionResponse(
      decisionId: json['decision_id'] ?? '',
      status: json['status'] ?? '',
    );
  }
}

class ConfirmNotTodayDecisionRequest {
  Map<String, dynamic> toJson() => {};
}

class ConfirmNotTodayDecisionResponse {
  ConfirmNotTodayDecisionResponse({
    required this.decisionId,
    required this.status,
    required this.action,
  });

  final String decisionId;
  final String status;
  final String action;

  factory ConfirmNotTodayDecisionResponse.fromJson(Map<String, dynamic> json) {
    return ConfirmNotTodayDecisionResponse(
      decisionId: json['decision_id'] ?? '',
      status: json['status'] ?? '',
      action: json['action'] ?? 'no_change',
    );
  }
}

class PendingConfirmationResponse {
  PendingConfirmationResponse({
    required this.pendingConfirmationId,
    required this.trainingDayId,
    required this.date,
    required this.dayType,
    required this.title,
    required this.plannedDistanceKm,
    required this.plannedDurationMin,
  });

  final String pendingConfirmationId;
  final String trainingDayId;
  final DateTime date;
  final String dayType;
  final String title;
  final double plannedDistanceKm;
  final int plannedDurationMin;

  factory PendingConfirmationResponse.fromJson(Map<String, dynamic> json) {
    return PendingConfirmationResponse(
      pendingConfirmationId: json['pending_confirmation_id'] ?? '',
      trainingDayId: json['training_day_id'] ?? '',
      date: DateTime.parse(json['date']),
      dayType: json['day_type'] ?? '',
      title: json['title'] ?? '',
      plannedDistanceKm:
          (json['planned_distance_km'] as num? ?? 0.0).toDouble(),
      plannedDurationMin: json['planned_duration_min'] ?? 0,
    );
  }
}

class ResolvePendingConfirmationRequest {
  ResolvePendingConfirmationRequest({
    required this.pendingConfirmationId,
    required this.resolution,
    this.actualDistanceKm,
    this.actualDurationMin,
    this.userNote,
  });

  final String pendingConfirmationId;
  final String resolution; // "completed" | "missed"
  final double? actualDistanceKm;
  final int? actualDurationMin;
  final String? userNote;

  Map<String, dynamic> toJson() {
    return {
      'pending_confirmation_id': pendingConfirmationId,
      'resolution': resolution,
      if (actualDistanceKm != null) 'actual_distance_km': actualDistanceKm,
      if (actualDurationMin != null) 'actual_duration_min': actualDurationMin,
      if (userNote != null) 'user_note': userNote,
    };
  }
}

class ResolvePendingConfirmationResponse {
  ResolvePendingConfirmationResponse(
      {required this.pendingConfirmationId, required this.status});
  final String pendingConfirmationId;
  final String status;

  factory ResolvePendingConfirmationResponse.fromJson(
      Map<String, dynamic> json) {
    return ResolvePendingConfirmationResponse(
      pendingConfirmationId: json['pending_confirmation_id'] ?? '',
      status: json['status'] ?? '',
    );
  }
}

class ProfileOverviewResponse {
  ProfileOverviewResponse({
    required this.name,
    required this.email,
    required this.unit,
    required this.runningBackground,
    this.activePlanStats,
  });

  final String name;
  final String email;
  final String unit;
  final String runningBackground;
  final ProfilePlanStatsDto? activePlanStats;

  factory ProfileOverviewResponse.fromJson(Map<String, dynamic> json) {
    return ProfileOverviewResponse(
      name: json['name'] ?? '',
      email: json['email'] ?? '',
      unit: json['unit'] ?? 'km',
      runningBackground: json['running_background'] ?? 'none',
      activePlanStats: json['active_plan_stats'] != null
          ? ProfilePlanStatsDto.fromJson(
              json['active_plan_stats'] as Map<String, dynamic>)
          : null,
    );
  }
}

class ProfilePlanStatsDto {
  ProfilePlanStatsDto({
    required this.planName,
    required this.goalType,
    required this.goalDistance,
    required this.completedRunsCount,
    required this.totalPlannedRunsCount,
    required this.totalCompletedDistance,
    required this.adherenceRatePercent,
  });

  final String planName;
  final String goalType;
  final String goalDistance;
  final int completedRunsCount;
  final int totalPlannedRunsCount;
  final double totalCompletedDistance;
  final double adherenceRatePercent;

  factory ProfilePlanStatsDto.fromJson(Map<String, dynamic> json) {
    return ProfilePlanStatsDto(
      planName: json['plan_name'] ?? '',
      goalType: json['goal_type'] ?? '',
      goalDistance: json['goal_distance'] ?? '',
      completedRunsCount: json['completed_runs_count'] ?? 0,
      totalPlannedRunsCount: json['total_planned_runs_count'] ?? 0,
      totalCompletedDistance:
          (json['total_completed_distance'] as num? ?? 0.0).toDouble(),
      adherenceRatePercent:
          (json['adherence_rate_percent'] as num? ?? 0.0).toDouble(),
    );
  }
}

class PlanDetailsResponse {
  PlanDetailsResponse({
    this.hasActivePlan = true,
    required this.planId,
    this.templateId,
    required this.status,
    required this.goalType,
    required this.goalDistance,
    required this.level,
    required this.daysPerWeek,
    required this.unit,
    this.raceName,
    this.raceDate,
    this.targetFinishTimeSeconds,
    required this.startedAt,
    required this.estimatedEndDate,
    required this.totalWeeks,
    required this.completedWeeksCount,
    required this.totalPlannedDistance,
    required this.totalCompletedDistance,
    required this.weeks,
  });

  final bool hasActivePlan;
  final String planId;
  final String? templateId;
  final String status;
  final String goalType;
  final String goalDistance;
  final String level;
  final int daysPerWeek;
  final String unit;
  final String? raceName;
  final DateTime? raceDate;
  final int? targetFinishTimeSeconds;
  final DateTime startedAt;
  final DateTime estimatedEndDate;
  final int totalWeeks;
  final int completedWeeksCount;
  final double totalPlannedDistance;
  final double totalCompletedDistance;
  final List<PlanWeekDetailDto> weeks;

  factory PlanDetailsResponse.fromJson(Map<String, dynamic> json) {
    return PlanDetailsResponse(
      hasActivePlan: json['has_active_plan'] ?? true,
      planId: json['plan_id'] ?? '',
      templateId: json['template_id'],
      status: json['status'] ?? '',
      goalType: json['goal_type'] ?? '',
      goalDistance: json['goal_distance'] ?? '',
      level: json['level'] ?? '',
      daysPerWeek: json['days_per_week'] ?? 0,
      unit: json['unit'] ?? 'km',
      raceName: json['race_name'],
      raceDate:
          json['race_date'] != null ? DateTime.parse(json['race_date']) : null,
      targetFinishTimeSeconds: json['target_finish_time_seconds'] as int?,
      startedAt: DateTime.parse(json['started_at']),
      estimatedEndDate: DateTime.parse(json['estimated_end_date']),
      totalWeeks: json['total_weeks'] ?? 0,
      completedWeeksCount: json['completed_weeks_count'] ?? 0,
      totalPlannedDistance:
          (json['total_planned_distance'] as num? ?? 0.0).toDouble(),
      totalCompletedDistance:
          (json['total_completed_distance'] as num? ?? 0.0).toDouble(),
      weeks: (json['weeks'] as List? ?? [])
          .map((e) => PlanWeekDetailDto.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

class PlanWeekDetailDto {
  PlanWeekDetailDto({
    required this.weekId,
    required this.weekNumber,
    required this.weekType,
    required this.plannedVolumeKm,
    required this.actualVolumeKm,
    required this.isRecoveryWeek,
    required this.startDate,
    required this.days,
  });

  final String weekId;
  final int weekNumber;
  final String weekType;
  final double plannedVolumeKm;
  final double actualVolumeKm;
  final bool isRecoveryWeek;
  final DateTime startDate;
  final List<PlanDayDetailDto> days;

  /// Phase 4H.3 — reuses the shared typed vocabulary from Phase 4H.1/4H.2
  /// (`preparation_runway.dart`) rather than a second parallel label
  /// system; safe for any current or future `TrainingWeekType` value.
  PreviewWeekType get weekTypeValue => PreviewWeekType.fromWire(weekType);

  factory PlanWeekDetailDto.fromJson(Map<String, dynamic> json) {
    return PlanWeekDetailDto(
      weekId: json['week_id'] ?? '',
      weekNumber: json['week_number'] ?? 0,
      weekType: json['week_type'] ?? '',
      plannedVolumeKm: (json['planned_volume_km'] as num? ?? 0.0).toDouble(),
      actualVolumeKm: (json['actual_volume_km'] as num? ?? 0.0).toDouble(),
      isRecoveryWeek: json['is_recovery_week'] ?? false,
      startDate: DateTime.parse(json['start_date']),
      days: (json['days'] as List? ?? [])
          .map((e) => PlanDayDetailDto.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

class PlanDayDetailDto {
  PlanDayDetailDto({
    required this.dayId,
    required this.date,
    required this.dayType,
    required this.status,
    required this.title,
    required this.description,
    required this.plannedDistanceKm,
    required this.plannedDurationMin,
    this.plannedPaceMinKm,
    this.intensity,
    this.actualDistanceKm,
    this.actualDurationMin,
    required this.isLongRun,
    required this.canMarkComplete,
    required this.canMarkNotToday,
  });

  final String dayId;
  final DateTime date;
  final String dayType;
  final String status;
  final String title;
  final String description;
  final double plannedDistanceKm;
  final int plannedDurationMin;
  final double? plannedPaceMinKm;
  final String? intensity;
  final double? actualDistanceKm;
  final int? actualDurationMin;
  final bool isLongRun;
  final bool canMarkComplete;
  final bool canMarkNotToday;

  /// Phase 4H.3 — see `TrainingDayResponse.intensityDetail`/`dayTypeValue`.
  WorkoutIntensityValue get intensityDetail =>
      WorkoutIntensityValue.fromWire(intensity);
  PreviewDayType get dayTypeValue => PreviewDayType.fromWire(dayType);

  factory PlanDayDetailDto.fromJson(Map<String, dynamic> json) {
    return PlanDayDetailDto(
      dayId: json['day_id'] ?? '',
      date: DateTime.parse(json['date']),
      dayType: json['day_type'] ?? '',
      status: json['status'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      plannedDistanceKm:
          (json['planned_distance_km'] as num? ?? 0.0).toDouble(),
      plannedDurationMin: json['planned_duration_min'] ?? 0,
      plannedPaceMinKm: (json['planned_pace_min_km'] as num?)?.toDouble(),
      intensity: json['intensity'],
      actualDistanceKm: (json['actual_distance_km'] as num?)?.toDouble(),
      actualDurationMin: json['actual_duration_min'] as int?,
      isLongRun: json['is_long_run'] ?? false,
      canMarkComplete: json['can_mark_complete'] ?? false,
      canMarkNotToday: json['can_mark_not_today'] ?? false,
    );
  }
}

class SettingsPreferencesResponse {
  SettingsPreferencesResponse({
    required this.reminderStyle,
    required this.workoutRemindersEnabled,
    required this.eveningReminderEnabled,
    required this.reminderTime,
  });

  final String reminderStyle;
  final bool workoutRemindersEnabled;
  final bool eveningReminderEnabled;
  final String reminderTime;

  factory SettingsPreferencesResponse.fromJson(Map<String, dynamic> json) {
    return SettingsPreferencesResponse(
      reminderStyle: json['reminder_style'] ?? 'balanced',
      workoutRemindersEnabled: json['workout_reminders_enabled'] ?? true,
      eveningReminderEnabled: json['evening_reminder_enabled'] ?? true,
      reminderTime: json['reminder_time'] ?? '08:00',
    );
  }
}
