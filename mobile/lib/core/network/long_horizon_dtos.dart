// Long-Horizon (21-52 week RollingLongHorizon) public DTOs.
//
// Mirrors the backend's real, unmodified V1 wire contracts exactly (see
// backend/RunningApp.Application/DTOs/Plan/LongHorizonActiveReadContracts.cs
// and LongHorizonPublicPlanContracts.cs, plus
// RollingActivation/PublicPreview/LongHorizonPublicPreviewContracts.cs).
// Follows this app's established manual-fromJson/snake_case-key convention
// (see dtos.dart) and the existing fail-closed `fromWire`/`unknown` enum
// idiom (see core/models/preparation_runway.dart) -- never a freezed/
// json_serializable model, to stay consistent with the rest of the codebase.
//
// No field here reproduces backend planning/checkpoint/recovery authority --
// every enum is a direct decode of a server-computed value; this file only
// renders what the server already decided.

/// Discriminates which shape an `/active/home` or `/active/calendar`
/// response actually is. Backend field: `schedule_strategy`.
enum PlanScheduleStrategy {
  staticComplete,
  rollingLongHorizon,
  unknown;

  static PlanScheduleStrategy fromWire(String? wire) {
    switch (wire) {
      case 'static_complete':
        return PlanScheduleStrategy.staticComplete;
      case 'rolling_long_horizon':
        return PlanScheduleStrategy.rollingLongHorizon;
      default:
        return PlanScheduleStrategy.unknown;
    }
  }
}

// ── Preview ──────────────────────────────────────────────────────────────

enum LongHorizonPublicLifecycleStatus {
  available,
  pending,
  blocked,
  completed,
  missed,
  unknown;

  // Backend `LongHorizonPublicLifecycleStatus` is a real C# enum under the
  // API-wide `JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)`
  // (see Program.cs), so its wire values are lowercase snake_case, NOT the
  // PascalCase C# member names -- confirmed by direct source read in
  // Phase 4L.5A after this exact PascalCase/snake_case mismatch was found
  // to silently fail every preview/confirmation decode (fromWire always
  // fell through to `unknown`, so `isConfirmable` could never be true).
  static LongHorizonPublicLifecycleStatus fromWire(String? wire) {
    switch (wire) {
      case 'available':
        return LongHorizonPublicLifecycleStatus.available;
      case 'pending':
        return LongHorizonPublicLifecycleStatus.pending;
      case 'blocked':
        return LongHorizonPublicLifecycleStatus.blocked;
      case 'completed':
        return LongHorizonPublicLifecycleStatus.completed;
      case 'missed':
        return LongHorizonPublicLifecycleStatus.missed;
      default:
        return LongHorizonPublicLifecycleStatus.unknown;
    }
  }
}

enum LongHorizonPublicPhase {
  generalEndurance,
  preparationRunway,
  core,
  unknown;

  // See the casing note on LongHorizonPublicLifecycleStatus.fromWire above --
  // this is a real C# enum, wire values are snake_case (`general_endurance`
  // etc.), not the PascalCase C# member names.
  static LongHorizonPublicPhase fromWire(String? wire) {
    switch (wire) {
      case 'general_endurance':
        return LongHorizonPublicPhase.generalEndurance;
      case 'preparation_runway':
        return LongHorizonPublicPhase.preparationRunway;
      case 'core':
        return LongHorizonPublicPhase.core;
      default:
        return LongHorizonPublicPhase.unknown;
    }
  }

  /// User-facing label. Never exposes the raw enum/backend token.
  String get label {
    switch (this) {
      case LongHorizonPublicPhase.generalEndurance:
        return 'Base Building';
      case LongHorizonPublicPhase.preparationRunway:
        return 'Preparation';
      case LongHorizonPublicPhase.core:
        return 'Race Training';
      case LongHorizonPublicPhase.unknown:
        return 'Training';
    }
  }
}

enum LongHorizonConfirmationReadiness {
  notReadyForConfirmation,
  readyForRollingPersistence,
  readyForLegacyFullPersistence,
  unknown;

  // See the casing note on LongHorizonPublicLifecycleStatus.fromWire above.
  static LongHorizonConfirmationReadiness fromWire(String? wire) {
    switch (wire) {
      case 'not_ready_for_confirmation':
        return LongHorizonConfirmationReadiness.notReadyForConfirmation;
      case 'ready_for_rolling_persistence':
        return LongHorizonConfirmationReadiness.readyForRollingPersistence;
      case 'ready_for_legacy_full_persistence':
        return LongHorizonConfirmationReadiness.readyForLegacyFullPersistence;
      default:
        return LongHorizonConfirmationReadiness.unknown;
    }
  }

  bool get isConfirmable =>
      this == LongHorizonConfirmationReadiness.readyForRollingPersistence ||
      this == LongHorizonConfirmationReadiness.readyForLegacyFullPersistence;
}

enum LongHorizonPreviewReadiness {
  readyForPublicPreview,
  publicPreviewBlocked,
  unknown;

  static LongHorizonPreviewReadiness fromWire(String? wire) {
    switch (wire) {
      case 'ready_for_public_preview':
        return LongHorizonPreviewReadiness.readyForPublicPreview;
      case 'public_preview_blocked':
        return LongHorizonPreviewReadiness.publicPreviewBlocked;
      default:
        return LongHorizonPreviewReadiness.unknown;
    }
  }
}

enum LongHorizonPublicBlockedReasonCategory {
  moreTrainingDataNeeded,
  completeCurrentWeek,
  updateAvailability,
  safetyReviewRequired,
  paceInformationNeeded,
  planTransitionUnavailable,
  unknown;

  static LongHorizonPublicBlockedReasonCategory fromWire(String? wire) {
    switch (wire) {
      case 'more_training_data_needed':
        return LongHorizonPublicBlockedReasonCategory.moreTrainingDataNeeded;
      case 'complete_current_week':
        return LongHorizonPublicBlockedReasonCategory.completeCurrentWeek;
      case 'update_availability':
        return LongHorizonPublicBlockedReasonCategory.updateAvailability;
      case 'safety_review_required':
        return LongHorizonPublicBlockedReasonCategory.safetyReviewRequired;
      case 'pace_information_needed':
        return LongHorizonPublicBlockedReasonCategory.paceInformationNeeded;
      case 'plan_transition_unavailable':
        return LongHorizonPublicBlockedReasonCategory.planTransitionUnavailable;
      default:
        return LongHorizonPublicBlockedReasonCategory.unknown;
    }
  }
}

enum LongHorizonPublicProvenance {
  generatedFromRecentTraining,
  generatedFromInitialProfile,
  updatedAfterCompletedTraining,
  awaitingMoreTrainingData,
  unknown;

  static LongHorizonPublicProvenance fromWire(String? wire) {
    switch (wire) {
      case 'generated_from_recent_training':
        return LongHorizonPublicProvenance.generatedFromRecentTraining;
      case 'generated_from_initial_profile':
        return LongHorizonPublicProvenance.generatedFromInitialProfile;
      case 'updated_after_completed_training':
        return LongHorizonPublicProvenance.updatedAfterCompletedTraining;
      case 'awaiting_more_training_data':
        return LongHorizonPublicProvenance.awaitingMoreTrainingData;
      default:
        return LongHorizonPublicProvenance.unknown;
    }
  }
}

class LongHorizonBlockedStateContract {
  const LongHorizonBlockedStateContract({
    required this.reasonCategory,
    required this.retryEligible,
    required this.nextActionKey,
    required this.lastEvaluatedDate,
  });

  final LongHorizonPublicBlockedReasonCategory reasonCategory;
  final bool retryEligible;
  final String nextActionKey;
  final String lastEvaluatedDate;

  factory LongHorizonBlockedStateContract.fromJson(Map<String, dynamic> json) {
    return LongHorizonBlockedStateContract(
      reasonCategory: LongHorizonPublicBlockedReasonCategory.fromWire(
          json['reason_category'] as String?),
      retryEligible: json['retry_eligible'] as bool? ?? false,
      nextActionKey: json['next_action_key'] as String? ?? '',
      lastEvaluatedDate: json['last_evaluated_date'] as String? ?? '',
    );
  }
}

class LongHorizonExecutableSessionContract {
  const LongHorizonExecutableSessionContract({
    required this.sessionDate,
    required this.weekday,
    required this.sessionRole,
    required this.distanceKm,
    required this.isLongRun,
    required this.executableStatus,
  });

  final String sessionDate; // yyyy-MM-dd
  final String weekday;
  final String sessionRole; // canonical wire token, see WorkoutRole below
  final double distanceKm;
  final bool isLongRun;
  final LongHorizonPublicLifecycleStatus executableStatus;

  factory LongHorizonExecutableSessionContract.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonExecutableSessionContract(
      sessionDate: json['session_date'] as String? ?? '',
      weekday: json['weekday'] as String? ?? '',
      sessionRole: json['session_role'] as String? ?? '',
      distanceKm: (json['distance_km'] as num?)?.toDouble() ?? 0,
      isLongRun: json['is_long_run'] as bool? ?? false,
      executableStatus: LongHorizonPublicLifecycleStatus.fromWire(
          json['executable_status'] as String?),
    );
  }
}

class LongHorizonExecutableWeekContract {
  const LongHorizonExecutableWeekContract({
    required this.globalWeek,
    required this.phase,
    required this.stage,
    required this.weekStartDate,
    required this.weekEndDate,
    required this.weeklyVolumeKm,
    required this.longRunVolumeKm,
    required this.lifecycleStatus,
    required this.sessions,
    required this.publicProvenanceSummary,
  });

  final int globalWeek;
  final LongHorizonPublicPhase phase;
  final String stage;
  final String weekStartDate;
  final String weekEndDate;
  final double weeklyVolumeKm;
  final double longRunVolumeKm;
  final LongHorizonPublicLifecycleStatus lifecycleStatus;
  final List<LongHorizonExecutableSessionContract> sessions;
  final LongHorizonPublicProvenance publicProvenanceSummary;

  factory LongHorizonExecutableWeekContract.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonExecutableWeekContract(
      globalWeek: json['global_week'] as int? ?? 0,
      phase: LongHorizonPublicPhase.fromWire(json['phase'] as String?),
      stage: json['stage'] as String? ?? '',
      weekStartDate: json['week_start_date'] as String? ?? '',
      weekEndDate: json['week_end_date'] as String? ?? '',
      weeklyVolumeKm: (json['weekly_volume_km'] as num?)?.toDouble() ?? 0,
      longRunVolumeKm: (json['long_run_volume_km'] as num?)?.toDouble() ?? 0,
      lifecycleStatus: LongHorizonPublicLifecycleStatus.fromWire(
          json['lifecycle_status'] as String?),
      sessions: (json['sessions'] as List<dynamic>? ?? [])
          .map((e) => LongHorizonExecutableSessionContract.fromJson(
              e as Map<String, dynamic>))
          .toList(),
      publicProvenanceSummary: LongHorizonPublicProvenance.fromWire(
          json['public_provenance_summary'] as String?),
    );
  }
}

/// One row of the structural roadmap. Deliberately carries NO session list
/// and NO numeric detail for Pending weeks -- [numericDetailsAvailable]
/// is false for those, and the UI must never fabricate one.
class LongHorizonStructuralRoadmapWeekContract {
  const LongHorizonStructuralRoadmapWeekContract({
    required this.globalWeek,
    required this.phase,
    required this.lifecycleStatus,
    required this.isExecutable,
    required this.structuralStartDate,
    required this.structuralEndDate,
    required this.numericDetailsAvailable,
    required this.publicSummary,
    this.stage,
  });

  final int globalWeek;
  final LongHorizonPublicPhase phase;
  final String? stage;
  final LongHorizonPublicLifecycleStatus lifecycleStatus;
  final bool isExecutable;
  final String structuralStartDate;
  final String structuralEndDate;
  final bool numericDetailsAvailable;
  final String publicSummary;

  factory LongHorizonStructuralRoadmapWeekContract.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonStructuralRoadmapWeekContract(
      globalWeek: json['global_week'] as int? ?? 0,
      phase: LongHorizonPublicPhase.fromWire(json['phase'] as String?),
      stage: json['stage'] as String?,
      lifecycleStatus: LongHorizonPublicLifecycleStatus.fromWire(
          json['lifecycle_status'] as String?),
      isExecutable: json['is_executable'] as bool? ?? false,
      structuralStartDate: json['structural_start_date'] as String? ?? '',
      structuralEndDate: json['structural_end_date'] as String? ?? '',
      numericDetailsAvailable:
          json['numeric_details_available'] as bool? ?? false,
      publicSummary: json['public_summary'] as String? ?? '',
    );
  }
}

class LongHorizonPlanPreviewContract {
  const LongHorizonPlanPreviewContract({
    required this.previewId,
    required this.goalType,
    required this.goalDistance,
    required this.totalWeeks,
    required this.startDate,
    required this.estimatedEndDate,
    required this.raceDate,
    required this.currentWindowStartWeek,
    required this.currentWindowEndWeek,
    required this.currentExecutableWeekCount,
    required this.structuralRoadmap,
    required this.currentExecutableWeeks,
    required this.previewReadiness,
    required this.confirmationReadiness,
    required this.publicWarnings,
    required this.provenanceSummary,
    required this.expiresAtUtc,
    this.blockedState,
  });

  final String previewId;
  final String goalType;
  final String goalDistance;
  final int totalWeeks;
  final String startDate;
  final String estimatedEndDate;
  final String raceDate;
  final int currentWindowStartWeek;
  final int currentWindowEndWeek;
  final int currentExecutableWeekCount;
  final List<LongHorizonStructuralRoadmapWeekContract> structuralRoadmap;
  final List<LongHorizonExecutableWeekContract> currentExecutableWeeks;
  final LongHorizonPreviewReadiness previewReadiness;
  final LongHorizonConfirmationReadiness confirmationReadiness;
  final List<String> publicWarnings;
  final LongHorizonPublicProvenance provenanceSummary;
  final LongHorizonBlockedStateContract? blockedState;
  final DateTime? expiresAtUtc;

  bool get isConfirmable => confirmationReadiness.isConfirmable;
  bool get isBlocked =>
      previewReadiness == LongHorizonPreviewReadiness.publicPreviewBlocked;

  factory LongHorizonPlanPreviewContract.fromJson(Map<String, dynamic> json) {
    return LongHorizonPlanPreviewContract(
      previewId: json['preview_id'] as String? ?? '',
      goalType: json['goal_type'] as String? ?? '',
      goalDistance: json['goal_distance'] as String? ?? '',
      totalWeeks: json['total_weeks'] as int? ?? 0,
      startDate: json['start_date'] as String? ?? '',
      estimatedEndDate: json['estimated_end_date'] as String? ?? '',
      raceDate: json['race_date'] as String? ?? '',
      currentWindowStartWeek: json['current_window_start_week'] as int? ?? 0,
      currentWindowEndWeek: json['current_window_end_week'] as int? ?? 0,
      currentExecutableWeekCount:
          json['current_executable_week_count'] as int? ?? 0,
      previewReadiness: LongHorizonPreviewReadiness.fromWire(
          json['preview_readiness'] as String?),
      publicWarnings:
          (json['public_warnings'] as List<dynamic>? ?? []).cast<String>(),
      provenanceSummary: LongHorizonPublicProvenance.fromWire(
          json['provenance_summary'] as String?),
      blockedState: json['blocked_state'] != null
          ? LongHorizonBlockedStateContract.fromJson(
              json['blocked_state'] as Map<String, dynamic>)
          : null,
      structuralRoadmap: (json['structural_roadmap'] as List<dynamic>? ?? [])
          .map((e) => LongHorizonStructuralRoadmapWeekContract.fromJson(
              e as Map<String, dynamic>))
          .toList(),
      currentExecutableWeeks:
          (json['current_executable_weeks'] as List<dynamic>? ?? [])
              .map((e) => LongHorizonExecutableWeekContract.fromJson(
                  e as Map<String, dynamic>))
              .toList(),
      confirmationReadiness: LongHorizonConfirmationReadiness.fromWire(
          json['confirmation_readiness'] as String?),
      expiresAtUtc: json['expires_at_utc'] != null
          ? DateTime.tryParse(json['expires_at_utc'] as String)
          : null,
    );
  }
}

// ── Confirmation ─────────────────────────────────────────────────────────

enum LongHorizonConfirmationOutcome {
  confirmed,
  alreadyConfirmed,
  unknown;

  // See the casing note on LongHorizonPublicLifecycleStatus.fromWire above.
  static LongHorizonConfirmationOutcome fromWire(String? wire) {
    switch (wire) {
      case 'confirmed':
        return LongHorizonConfirmationOutcome.confirmed;
      case 'already_confirmed':
        return LongHorizonConfirmationOutcome.alreadyConfirmed;
      default:
        return LongHorizonConfirmationOutcome.unknown;
    }
  }
}

class LongHorizonConfirmPlanResponse {
  const LongHorizonConfirmPlanResponse({
    required this.planId,
    required this.previewId,
    required this.outcome,
    required this.totalWeeks,
    required this.planStatus,
    required this.publicMessage,
    this.nextPendingGlobalWeek,
  });

  final String planId;
  final String previewId;
  final LongHorizonConfirmationOutcome outcome;
  final int totalWeeks;
  final int? nextPendingGlobalWeek;
  final String planStatus;
  final String publicMessage;

  factory LongHorizonConfirmPlanResponse.fromJson(Map<String, dynamic> json) {
    return LongHorizonConfirmPlanResponse(
      planId: json['plan_id'] as String? ?? '',
      previewId: json['preview_id'] as String? ?? '',
      outcome:
          LongHorizonConfirmationOutcome.fromWire(json['outcome'] as String?),
      totalWeeks: json['total_weeks'] as int? ?? 0,
      nextPendingGlobalWeek: json['next_pending_global_week'] as int?,
      planStatus: json['plan_status'] as String? ?? '',
      publicMessage: json['public_message'] as String? ?? '',
    );
  }
}

// ── Session role / outcome ──────────────────────────────────────────────

/// Canonical rolling workout-role tokens (Phase 4L.4E). Never accept
/// arbitrary aliases — only the three server-owned canonical tokens the
/// public contract defines.
enum WorkoutRole {
  keySession,
  easySupport,
  longRun,
  unknown;

  static WorkoutRole fromWire(String? wire) {
    switch (wire) {
      case 'KEY_SESSION':
        return WorkoutRole.keySession;
      case 'EASY_SUPPORT':
        return WorkoutRole.easySupport;
      case 'LONG_RUN':
        return WorkoutRole.longRun;
      default:
        return WorkoutRole.unknown;
    }
  }

  String get label {
    switch (this) {
      case WorkoutRole.keySession:
        return 'Key Session';
      case WorkoutRole.easySupport:
        return 'Easy Run';
      case WorkoutRole.longRun:
        return 'Long Run';
      case WorkoutRole.unknown:
        return 'Training Session';
    }
  }
}

enum RollingSessionOutcome {
  planned,
  completed,
  notToday,
  unknown;

  static RollingSessionOutcome fromWire(String? wire) {
    switch (wire) {
      case 'planned':
        return RollingSessionOutcome.planned;
      case 'completed':
        return RollingSessionOutcome.completed;
      case 'not_today':
        return RollingSessionOutcome.notToday;
      default:
        return RollingSessionOutcome.unknown;
    }
  }
}

class LongHorizonRollingSessionResponse {
  const LongHorizonRollingSessionResponse({
    required this.sessionId,
    required this.planId,
    required this.globalWeek,
    required this.phase,
    required this.stage,
    required this.assignedDate,
    required this.workoutRole,
    required this.plannedDistanceKm,
    required this.outcome,
    required this.isLongRun,
    required this.mutationAllowed,
    required this.publicProvenance,
    this.workoutKey,
    this.workoutVersion,
    this.plannedDurationMinutes,
    this.plannedPaceMinutesPerKm,
    this.plannedIntensity,
    this.actualDistanceKm,
    this.actualDurationMinutes,
    this.actualPaceMinutesPerKm,
    this.completedAtUtc,
    this.notTodayReasonCategory,
    this.notTodayRecordedAtUtc,
  });

  final String sessionId;
  final String planId;
  final int globalWeek;
  final String phase; // "general_endurance" | "preparation_runway" | "core"
  final String stage;
  final String assignedDate; // yyyy-MM-dd
  final WorkoutRole workoutRole;
  final String? workoutKey;
  final int? workoutVersion;
  final double plannedDistanceKm;
  final int? plannedDurationMinutes;
  final double? plannedPaceMinutesPerKm;
  final String? plannedIntensity;
  final RollingSessionOutcome outcome;
  final bool isLongRun;
  final bool mutationAllowed;

  /// Plain manually-formatted backend string (not a real enum on the wire) --
  /// only ever `"updated_after_completed_training"` or
  /// `"generated_from_initial_profile"` in practice, but decoded as an
  /// opaque string rather than assumed exhaustive.
  final String publicProvenance;
  final double? actualDistanceKm;
  final int? actualDurationMinutes;
  final double? actualPaceMinutesPerKm;
  final DateTime? completedAtUtc;
  final String? notTodayReasonCategory;
  final DateTime? notTodayRecordedAtUtc;

  factory LongHorizonRollingSessionResponse.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonRollingSessionResponse(
      sessionId: json['session_id'] as String? ?? '',
      planId: json['plan_id'] as String? ?? '',
      globalWeek: json['global_week'] as int? ?? 0,
      phase: json['phase'] as String? ?? '',
      stage: json['stage'] as String? ?? '',
      assignedDate: json['assigned_date'] as String? ?? '',
      workoutRole: WorkoutRole.fromWire(json['workout_role'] as String?),
      workoutKey: json['workout_key'] as String?,
      workoutVersion: json['workout_version'] as int?,
      plannedDistanceKm: (json['planned_distance_km'] as num?)?.toDouble() ?? 0,
      plannedDurationMinutes: json['planned_duration_minutes'] as int?,
      plannedPaceMinutesPerKm:
          (json['planned_pace_minutes_per_km'] as num?)?.toDouble(),
      plannedIntensity: json['planned_intensity'] as String?,
      outcome: RollingSessionOutcome.fromWire(json['outcome'] as String?),
      isLongRun: json['is_long_run'] as bool? ?? false,
      mutationAllowed: json['mutation_allowed'] as bool? ?? false,
      publicProvenance: json['public_provenance'] as String? ?? '',
      actualDistanceKm: (json['actual_distance_km'] as num?)?.toDouble(),
      actualDurationMinutes: json['actual_duration_minutes'] as int?,
      actualPaceMinutesPerKm:
          (json['actual_pace_minutes_per_km'] as num?)?.toDouble(),
      completedAtUtc: json['completed_at_utc'] != null
          ? DateTime.tryParse(json['completed_at_utc'] as String)
          : null,
      notTodayReasonCategory: json['not_today_reason_category'] as String?,
      notTodayRecordedAtUtc: json['not_today_recorded_at_utc'] != null
          ? DateTime.tryParse(json['not_today_recorded_at_utc'] as String)
          : null,
    );
  }
}

// ── Recovery / readiness ────────────────────────────────────────────────

enum LongHorizonCheckpointReadiness {
  currentWindowInProgress,
  currentWindowComplete,
  nextWindowActivationReady,
  reassessmentRequired,
  terminalPlanComplete,
  unknown;

  static LongHorizonCheckpointReadiness fromWire(String? wire) {
    switch (wire) {
      case 'current_window_in_progress':
        return LongHorizonCheckpointReadiness.currentWindowInProgress;
      case 'current_window_complete':
        return LongHorizonCheckpointReadiness.currentWindowComplete;
      case 'next_window_activation_ready':
        return LongHorizonCheckpointReadiness.nextWindowActivationReady;
      case 'reassessment_required':
        return LongHorizonCheckpointReadiness.reassessmentRequired;
      case 'terminal_plan_complete':
        return LongHorizonCheckpointReadiness.terminalPlanComplete;
      default:
        return LongHorizonCheckpointReadiness.unknown;
    }
  }
}

enum LongHorizonRecoveryRequirement {
  none,
  calendarWindowPending,
  regeneratePreviewRequired,
  operationalSupportRequired,
  unknown;

  static LongHorizonRecoveryRequirement fromWire(String? wire) {
    switch (wire) {
      case 'none':
        return LongHorizonRecoveryRequirement.none;
      case 'calendar_window_pending':
        return LongHorizonRecoveryRequirement.calendarWindowPending;
      case 'regenerate_preview_required':
        return LongHorizonRecoveryRequirement.regeneratePreviewRequired;
      case 'operational_support_required':
        return LongHorizonRecoveryRequirement.operationalSupportRequired;
      default:
        return LongHorizonRecoveryRequirement.unknown;
    }
  }
}

class LongHorizonActivePlanSummaryResponse {
  const LongHorizonActivePlanSummaryResponse({
    required this.planId,
    required this.goalType,
    required this.goalDistance,
    required this.totalWeeks,
    required this.currentGlobalWeek,
    required this.currentPhase,
    required this.currentStage,
    required this.currentWindowStartWeek,
    required this.currentWindowEndWeek,
    required this.activatedSessionCount,
    required this.terminalSessionCount,
    required this.checkpointReadiness,
    required this.status,
    required this.publicMessage,
    this.nextPendingGlobalWeek,
    this.recoveryRequirement,
    this.blockedPublicReasonCategory,
  });

  final String planId;
  final String goalType;
  final String goalDistance;
  final int totalWeeks;
  final int currentGlobalWeek;
  final String currentPhase;
  final String currentStage;
  final int currentWindowStartWeek;
  final int currentWindowEndWeek;
  final int? nextPendingGlobalWeek;
  final int activatedSessionCount;
  final int terminalSessionCount;
  final LongHorizonCheckpointReadiness checkpointReadiness;
  final LongHorizonRecoveryRequirement? recoveryRequirement;
  final String? blockedPublicReasonCategory;
  final String status;
  final String publicMessage;

  factory LongHorizonActivePlanSummaryResponse.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonActivePlanSummaryResponse(
      planId: json['plan_id'] as String? ?? '',
      goalType: json['goal_type'] as String? ?? '',
      goalDistance: json['goal_distance'] as String? ?? '',
      totalWeeks: json['total_weeks'] as int? ?? 0,
      currentGlobalWeek: json['current_global_week'] as int? ?? 0,
      currentPhase: json['current_phase'] as String? ?? '',
      currentStage: json['current_stage'] as String? ?? '',
      currentWindowStartWeek: json['current_window_start_week'] as int? ?? 0,
      currentWindowEndWeek: json['current_window_end_week'] as int? ?? 0,
      nextPendingGlobalWeek: json['next_pending_global_week'] as int?,
      activatedSessionCount: json['activated_session_count'] as int? ?? 0,
      terminalSessionCount: json['terminal_session_count'] as int? ?? 0,
      checkpointReadiness: LongHorizonCheckpointReadiness.fromWire(
          json['checkpoint_readiness'] as String?),
      recoveryRequirement: json['recovery_requirement'] != null
          ? LongHorizonRecoveryRequirement.fromWire(
              json['recovery_requirement'] as String?)
          : null,
      blockedPublicReasonCategory:
          json['blocked_public_reason_category'] as String?,
      status: json['status'] as String? ?? '',
      publicMessage: json['public_message'] as String? ?? '',
    );
  }
}

// ── Home ─────────────────────────────────────────────────────────────────

class LongHorizonHomeResponse {
  const LongHorizonHomeResponse({
    required this.activePlan,
    required this.currentWindowSessions,
    required this.hasPendingConfirmations,
    this.todayWorkout,
    this.nextExecutableWorkout,
  });

  final LongHorizonActivePlanSummaryResponse activePlan;
  final LongHorizonRollingSessionResponse? todayWorkout;
  final LongHorizonRollingSessionResponse? nextExecutableWorkout;
  final List<LongHorizonRollingSessionResponse> currentWindowSessions;
  final bool hasPendingConfirmations;

  factory LongHorizonHomeResponse.fromJson(Map<String, dynamic> json) {
    return LongHorizonHomeResponse(
      activePlan: LongHorizonActivePlanSummaryResponse.fromJson(
          json['active_plan'] as Map<String, dynamic>? ?? const {}),
      todayWorkout: json['today_workout'] != null
          ? LongHorizonRollingSessionResponse.fromJson(
              json['today_workout'] as Map<String, dynamic>)
          : null,
      nextExecutableWorkout: json['next_executable_workout'] != null
          ? LongHorizonRollingSessionResponse.fromJson(
              json['next_executable_workout'] as Map<String, dynamic>)
          : null,
      currentWindowSessions:
          (json['current_window_sessions'] as List<dynamic>? ?? [])
              .map((e) => LongHorizonRollingSessionResponse.fromJson(
                  e as Map<String, dynamic>))
              .toList(),
      hasPendingConfirmations:
          json['has_pending_confirmations'] as bool? ?? false,
    );
  }
}

/// Discriminated result of decoding `GET /plans/active/home` — the backend
/// may return either the existing static shape or the rolling shape from
/// the SAME endpoint. Decoding branches on `schedule_strategy`; nothing here
/// infers strategy from payload shape.
class ActiveHomeResult {
  const ActiveHomeResult._(this.strategy, this.rollingHome, this.rawStaticJson);

  final PlanScheduleStrategy strategy;
  final LongHorizonHomeResponse? rollingHome;

  /// The raw JSON for a static response — decoded by the existing,
  /// unmodified `HomeResponse.fromJson` at the call site, not here, to
  /// avoid this file taking a dependency on the static DTO file.
  final Map<String, dynamic>? rawStaticJson;

  factory ActiveHomeResult.fromJson(Map<String, dynamic> json) {
    final strategy =
        PlanScheduleStrategy.fromWire(json['schedule_strategy'] as String?);
    if (strategy == PlanScheduleStrategy.rollingLongHorizon) {
      return ActiveHomeResult._(
          strategy, LongHorizonHomeResponse.fromJson(json), null);
    }
    // Absent schedule_strategy or 'static_complete' both mean: decode as
    // the existing static shape (matches backend's own convention where
    // static responses never included this field before Long-Horizon).
    return ActiveHomeResult._(PlanScheduleStrategy.staticComplete, null, json);
  }
}

// ── Calendar ─────────────────────────────────────────────────────────────

class LongHorizonCalendarResponse {
  const LongHorizonCalendarResponse({
    required this.planId,
    required this.month,
    required this.sessions,
  });

  final String planId;
  final String month;
  final List<LongHorizonRollingSessionResponse> sessions;

  factory LongHorizonCalendarResponse.fromJson(Map<String, dynamic> json) {
    return LongHorizonCalendarResponse(
      planId: json['plan_id'] as String? ?? '',
      month: json['month'] as String? ?? '',
      sessions: (json['sessions'] as List<dynamic>? ?? [])
          .map((e) => LongHorizonRollingSessionResponse.fromJson(
              e as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// Discriminated result of decoding `GET /plans/active/calendar` — may be
/// the existing static list or the rolling object, from the same endpoint.
class ActiveCalendarResult {
  const ActiveCalendarResult._(
      this.strategy, this.rollingCalendar, this.rawStaticJson);

  final PlanScheduleStrategy strategy;
  final LongHorizonCalendarResponse? rollingCalendar;
  final dynamic rawStaticJson; // a List<dynamic> for the static shape.

  factory ActiveCalendarResult.fromJson(dynamic json) {
    if (json is Map<String, dynamic>) {
      final strategy =
          PlanScheduleStrategy.fromWire(json['schedule_strategy'] as String?);
      if (strategy == PlanScheduleStrategy.rollingLongHorizon) {
        return ActiveCalendarResult._(
            strategy, LongHorizonCalendarResponse.fromJson(json), null);
      }
    }
    // The static Calendar contract is a bare JSON array, not an object.
    return ActiveCalendarResult._(
        PlanScheduleStrategy.staticComplete, null, json);
  }
}

// ── Session detail ───────────────────────────────────────────────────────

class LongHorizonRollingSessionDetailResponse {
  const LongHorizonRollingSessionDetailResponse({
    required this.session,
    required this.publicDescription,
  });

  final LongHorizonRollingSessionResponse session;
  final String publicDescription;

  factory LongHorizonRollingSessionDetailResponse.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonRollingSessionDetailResponse(
      session: LongHorizonRollingSessionResponse.fromJson(
          json['session'] as Map<String, dynamic>? ?? const {}),
      publicDescription: json['public_description'] as String? ?? '',
    );
  }
}

// ── Mutations (complete / not-today) ────────────────────────────────────

enum LongHorizonSessionMutationOutcome {
  completed,
  notToday,
  idempotentReplay,
  unknown;

  static LongHorizonSessionMutationOutcome fromWire(String? wire) {
    switch (wire) {
      case 'completed':
        return LongHorizonSessionMutationOutcome.completed;
      case 'not_today':
        return LongHorizonSessionMutationOutcome.notToday;
      case 'idempotent_replay':
        return LongHorizonSessionMutationOutcome.idempotentReplay;
      default:
        return LongHorizonSessionMutationOutcome.unknown;
    }
  }
}

class LongHorizonSessionMutationResponse {
  const LongHorizonSessionMutationResponse({
    required this.sessionId,
    required this.planId,
    required this.outcome,
    required this.outcomeVersion,
    required this.checkpointReadiness,
    required this.nextWindowActivated,
  });

  final String sessionId;
  final String planId;
  final LongHorizonSessionMutationOutcome outcome;
  final int outcomeVersion;
  final LongHorizonCheckpointReadiness checkpointReadiness;
  final bool nextWindowActivated;

  factory LongHorizonSessionMutationResponse.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonSessionMutationResponse(
      sessionId: json['session_id'] as String? ?? '',
      planId: json['plan_id'] as String? ?? '',
      outcome: LongHorizonSessionMutationOutcome.fromWire(
          json['outcome'] as String?),
      outcomeVersion: json['outcome_version'] as int? ?? 0,
      checkpointReadiness: LongHorizonCheckpointReadiness.fromWire(
          json['checkpoint_readiness'] as String?),
      nextWindowActivated: json['next_window_activated'] as bool? ?? false,
    );
  }
}

/// Approved not-today reason tokens (backend-validated; the client must not
/// invent or accept arbitrary free text).
enum NotTodayReason {
  fatigue,
  soreness,
  illness,
  schedule,
  weather,
  other;

  String get wireValue => switch (this) {
        NotTodayReason.fatigue => 'fatigue',
        NotTodayReason.soreness => 'soreness',
        NotTodayReason.illness => 'illness',
        NotTodayReason.schedule => 'schedule',
        NotTodayReason.weather => 'weather',
        NotTodayReason.other => 'other',
      };

  String get label => switch (this) {
        NotTodayReason.fatigue => 'Fatigue',
        NotTodayReason.soreness => 'Soreness',
        NotTodayReason.illness => 'Illness',
        NotTodayReason.schedule => 'Schedule conflict',
        NotTodayReason.weather => 'Weather',
        NotTodayReason.other => 'Other',
      };
}

// ── Activation ───────────────────────────────────────────────────────────

enum LongHorizonContinuationOutcome {
  activated,
  idempotentReplay,
  terminalPlanComplete,
  unknown;

  static LongHorizonContinuationOutcome fromWire(String? wire) {
    switch (wire) {
      case 'activated':
        return LongHorizonContinuationOutcome.activated;
      case 'idempotent_replay':
        return LongHorizonContinuationOutcome.idempotentReplay;
      case 'terminal_plan_complete':
        return LongHorizonContinuationOutcome.terminalPlanComplete;
      default:
        return LongHorizonContinuationOutcome.unknown;
    }
  }
}

class LongHorizonWindowRange {
  const LongHorizonWindowRange(
      {required this.startGlobalWeek, required this.endGlobalWeek});
  final int startGlobalWeek;
  final int endGlobalWeek;

  factory LongHorizonWindowRange.fromJson(Map<String, dynamic> json) =>
      LongHorizonWindowRange(
        startGlobalWeek: json['start_global_week'] as int? ?? 0,
        endGlobalWeek: json['end_global_week'] as int? ?? 0,
      );
}

class LongHorizonActivateNextWindowResponse {
  const LongHorizonActivateNextWindowResponse({
    required this.planId,
    required this.outcome,
    required this.activatedGlobalWeeks,
    required this.activatedSessions,
    required this.checkpointReadiness,
    required this.planStatus,
    required this.isTerminal,
    required this.publicMessage,
    this.previousWindowRange,
    this.activatedWindowRange,
    this.nextPendingGlobalWeek,
    this.activatedAtUtc,
  });

  final String planId;
  final LongHorizonContinuationOutcome outcome;
  final LongHorizonWindowRange? previousWindowRange;
  final LongHorizonWindowRange? activatedWindowRange;
  final List<int> activatedGlobalWeeks;
  final List<LongHorizonRollingSessionResponse> activatedSessions;
  final int? nextPendingGlobalWeek;
  final LongHorizonCheckpointReadiness checkpointReadiness;
  final String planStatus;
  final bool isTerminal;
  final DateTime? activatedAtUtc;
  final String publicMessage;

  factory LongHorizonActivateNextWindowResponse.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonActivateNextWindowResponse(
      planId: json['plan_id'] as String? ?? '',
      outcome:
          LongHorizonContinuationOutcome.fromWire(json['outcome'] as String?),
      previousWindowRange: json['previous_window_range'] != null
          ? LongHorizonWindowRange.fromJson(
              json['previous_window_range'] as Map<String, dynamic>)
          : null,
      activatedWindowRange: json['activated_window_range'] != null
          ? LongHorizonWindowRange.fromJson(
              json['activated_window_range'] as Map<String, dynamic>)
          : null,
      activatedGlobalWeeks:
          (json['activated_global_weeks'] as List<dynamic>? ?? []).cast<int>(),
      activatedSessions: (json['activated_sessions'] as List<dynamic>? ?? [])
          .map((e) => LongHorizonRollingSessionResponse.fromJson(
              e as Map<String, dynamic>))
          .toList(),
      nextPendingGlobalWeek: json['next_pending_global_week'] as int?,
      checkpointReadiness: LongHorizonCheckpointReadiness.fromWire(
          json['checkpoint_readiness'] as String?),
      planStatus: json['plan_status'] as String? ?? '',
      isTerminal: json['is_terminal'] as bool? ?? false,
      activatedAtUtc: json['activated_at_utc'] != null
          ? DateTime.tryParse(json['activated_at_utc'] as String)
          : null,
      publicMessage: json['public_message'] as String? ?? '',
    );
  }
}

// ── Retry ────────────────────────────────────────────────────────────────

enum LongHorizonRetryOutcome {
  restoredToPending,
  idempotentReplay,
  unknown;

  static LongHorizonRetryOutcome fromWire(String? wire) {
    switch (wire) {
      case 'restored_to_pending':
        return LongHorizonRetryOutcome.restoredToPending;
      case 'idempotent_replay':
        return LongHorizonRetryOutcome.idempotentReplay;
      default:
        return LongHorizonRetryOutcome.unknown;
    }
  }
}

class LongHorizonRetryContinuationResponse {
  const LongHorizonRetryContinuationResponse({
    required this.planId,
    required this.outcome,
    required this.restoredWindowRange,
    required this.currentWindowRange,
    required this.checkpointReadiness,
    required this.planStatus,
    required this.publicMessage,
    this.nextPendingGlobalWeek,
    this.retriedAtUtc,
  });

  final String planId;
  final LongHorizonRetryOutcome outcome;
  final LongHorizonWindowRange restoredWindowRange;
  final LongHorizonWindowRange currentWindowRange;
  final int? nextPendingGlobalWeek;
  final LongHorizonCheckpointReadiness checkpointReadiness;
  final String planStatus;
  final DateTime? retriedAtUtc;
  final String publicMessage;

  factory LongHorizonRetryContinuationResponse.fromJson(
      Map<String, dynamic> json) {
    return LongHorizonRetryContinuationResponse(
      planId: json['plan_id'] as String? ?? '',
      outcome: LongHorizonRetryOutcome.fromWire(json['outcome'] as String?),
      restoredWindowRange: LongHorizonWindowRange.fromJson(
          json['restored_window_range'] as Map<String, dynamic>? ?? const {}),
      currentWindowRange: LongHorizonWindowRange.fromJson(
          json['current_window_range'] as Map<String, dynamic>? ?? const {}),
      nextPendingGlobalWeek: json['next_pending_global_week'] as int?,
      checkpointReadiness: LongHorizonCheckpointReadiness.fromWire(
          json['checkpoint_readiness'] as String?),
      planStatus: json['plan_status'] as String? ?? '',
      retriedAtUtc: json['retried_at_utc'] != null
          ? DateTime.tryParse(json['retried_at_utc'] as String)
          : null,
      publicMessage: json['public_message'] as String? ?? '',
    );
  }
}
