// Core DTOs matching backend snake_case request/response structures.

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

class GeneratePreviewRequest {
  GeneratePreviewRequest({
    required this.goalType,
    required this.goalDistance,
    required this.level,
    required this.daysPerWeek,
    required this.unit,
    this.raceName,
    this.raceDate,
    this.targetFinishTimeSeconds,
    this.preferredDays,
    this.longRunDay,
    this.habitPlanType,
    this.customGoalType,
    this.customDurationWeeks,
    this.customTargetTimeSeconds,
  });

  final String goalType;
  final String goalDistance;
  final String level;
  final int daysPerWeek;
  final String unit;
  final String? raceName;
  final String? raceDate;
  final int? targetFinishTimeSeconds;
  final String? preferredDays;
  final String? longRunDay;
  final String? habitPlanType;
  final String? customGoalType;
  final int? customDurationWeeks;
  final int? customTargetTimeSeconds;

  Map<String, dynamic> toJson() {
    return {
      'goal_type': goalType,
      'goal_distance': goalDistance,
      'level': level,
      'days_per_week': daysPerWeek,
      'unit': unit,
      if (raceName != null) 'race_name': raceName,
      if (raceDate != null) 'race_date': raceDate,
      if (targetFinishTimeSeconds != null) 'target_finish_time_seconds': targetFinishTimeSeconds,
      if (preferredDays != null) 'preferred_days': preferredDays,
      if (longRunDay != null) 'long_run_day': longRunDay,
      if (habitPlanType != null) 'habit_plan_type': habitPlanType,
      if (customGoalType != null) 'custom_goal_type': customGoalType,
      if (customDurationWeeks != null) 'custom_duration_weeks': customDurationWeeks,
      if (customTargetTimeSeconds != null) 'custom_target_time_seconds': customTargetTimeSeconds,
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
  });

  final String previewId;
  final String templateId;
  final String goalType;
  final String goalDistance;
  final String level;
  final int daysPerWeek;
  final String unit;
  final List<PreviewWeekDto> weeks;

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
    );
  }
}

class PreviewWeekDto {
  PreviewWeekDto({
    required this.weekNumber,
    required this.weekType,
    required this.days,
  });

  final int weekNumber;
  final String weekType;
  final List<PreviewDayDto> days;

  factory PreviewWeekDto.fromJson(Map<String, dynamic> json) {
    return PreviewWeekDto(
      weekNumber: json['week_number'] ?? 0,
      weekType: json['week_type'] ?? '',
      days: (json['days'] as List? ?? [])
          .map((e) => PreviewDayDto.fromJson(e as Map<String, dynamic>))
          .toList(),
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
          ? ActivePlanSummaryDto.fromJson(json['active_plan'] as Map<String, dynamic>)
          : null,
      todayWorkout: json['today_workout'] != null
          ? TrainingDayResponse.fromJson(json['today_workout'] as Map<String, dynamic>)
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
  });

  final String planId;
  final String goalType;
  final String goalDistance;
  final String level;
  final String progressText;

  factory ActivePlanSummaryDto.fromJson(Map<String, dynamic> json) {
    return ActivePlanSummaryDto(
      planId: json['plan_id'] ?? '',
      goalType: json['goal_type'] ?? '',
      goalDistance: json['goal_distance'] ?? '',
      level: json['level'] ?? '',
      progressText: json['progress_text'] ?? '',
    );
  }
}

class TrainingDayResponse {
  TrainingDayResponse({
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

  factory TrainingDayResponse.fromJson(Map<String, dynamic> json) {
    return TrainingDayResponse(
      dayId: json['day_id'] ?? '',
      date: DateTime.parse(json['date']),
      dayType: json['day_type'] ?? '',
      status: json['status'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      plannedDistanceKm: (json['planned_distance_km'] as num? ?? 0.0).toDouble(),
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

  factory TrainingDayDetailResponse.fromJson(Map<String, dynamic> json) {
    return TrainingDayDetailResponse(
      dayId: json['day_id'] ?? '',
      date: DateTime.parse(json['date']),
      dayType: json['day_type'] ?? '',
      status: json['status'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      plannedDistanceKm: (json['planned_distance_km'] as num? ?? 0.0).toDouble(),
      plannedDurationMin: json['planned_duration_min'] ?? 0,
      plannedPaceMinKm: (json['planned_pace_min_km'] as num?)?.toDouble(),
      intensity: json['intensity'],
      actualDistanceKm: (json['actual_distance_km'] as num?)?.toDouble(),
      actualDurationMin: json['actual_duration_min'] as int?,
      isLongRun: json['is_long_run'] ?? false,
      canMarkComplete: json['can_mark_complete'] ?? false,
      canMarkNotToday: json['can_mark_not_today'] ?? false,
      completedAt: json['completed_at'] != null ? DateTime.parse(json['completed_at']) : null,
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
  CreateNotTodayDecisionResponse({required this.decisionId, required this.status});
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
      plannedDistanceKm: (json['planned_distance_km'] as num? ?? 0.0).toDouble(),
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
  ResolvePendingConfirmationResponse({required this.pendingConfirmationId, required this.status});
  final String pendingConfirmationId;
  final String status;

  factory ResolvePendingConfirmationResponse.fromJson(Map<String, dynamic> json) {
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
          ? ProfilePlanStatsDto.fromJson(json['active_plan_stats'] as Map<String, dynamic>)
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
      totalCompletedDistance: (json['total_completed_distance'] as num? ?? 0.0).toDouble(),
      adherenceRatePercent: (json['adherence_rate_percent'] as num? ?? 0.0).toDouble(),
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
      raceDate: json['race_date'] != null ? DateTime.parse(json['race_date']) : null,
      targetFinishTimeSeconds: json['target_finish_time_seconds'] as int?,
      startedAt: DateTime.parse(json['started_at']),
      estimatedEndDate: DateTime.parse(json['estimated_end_date']),
      totalWeeks: json['total_weeks'] ?? 0,
      completedWeeksCount: json['completed_weeks_count'] ?? 0,
      totalPlannedDistance: (json['total_planned_distance'] as num? ?? 0.0).toDouble(),
      totalCompletedDistance: (json['total_completed_distance'] as num? ?? 0.0).toDouble(),
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

  factory PlanDayDetailDto.fromJson(Map<String, dynamic> json) {
    return PlanDayDetailDto(
      dayId: json['day_id'] ?? '',
      date: DateTime.parse(json['date']),
      dayType: json['day_type'] ?? '',
      status: json['status'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      plannedDistanceKm: (json['planned_distance_km'] as num? ?? 0.0).toDouble(),
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
