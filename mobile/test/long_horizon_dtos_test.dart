import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';

void main() {
  group('PlanScheduleStrategy', () {
    test('decodes known wire values', () {
      expect(PlanScheduleStrategy.fromWire('static_complete'),
          PlanScheduleStrategy.staticComplete);
      expect(PlanScheduleStrategy.fromWire('rolling_long_horizon'),
          PlanScheduleStrategy.rollingLongHorizon);
    });

    test('fails closed to unknown for unrecognized/missing values', () {
      expect(PlanScheduleStrategy.fromWire('something_new'),
          PlanScheduleStrategy.unknown);
      expect(PlanScheduleStrategy.fromWire(null), PlanScheduleStrategy.unknown);
    });
  });

  // Phase 4L.5A: backend LongHorizonPublicLifecycleStatus/PublicPhase/
  // ConfirmationReadiness/PreviewReadiness/ConfirmationOutcome are real C#
  // enums under the API-wide JsonStringEnumConverter(SnakeCaseLower), so
  // their wire values are lowercase snake_case, e.g. "available", not the
  // PascalCase C# member name "Available". Phase 4L.5 got this wrong for
  // every one of these enums (used PascalCase in fromWire), which would
  // have made every real preview/confirmation response fail closed to
  // `unknown` and `isConfirmable` permanently false. These tests pin the
  // corrected, verified-against-source casing so it can't regress silently.
  group('Enum wire casing (snake_case, not PascalCase)', () {
    test('LongHorizonPublicLifecycleStatus', () {
      expect(LongHorizonPublicLifecycleStatus.fromWire('available'),
          LongHorizonPublicLifecycleStatus.available);
      expect(LongHorizonPublicLifecycleStatus.fromWire('pending'),
          LongHorizonPublicLifecycleStatus.pending);
      expect(LongHorizonPublicLifecycleStatus.fromWire('blocked'),
          LongHorizonPublicLifecycleStatus.blocked);
      expect(LongHorizonPublicLifecycleStatus.fromWire('completed'),
          LongHorizonPublicLifecycleStatus.completed);
      expect(LongHorizonPublicLifecycleStatus.fromWire('missed'),
          LongHorizonPublicLifecycleStatus.missed);
      expect(LongHorizonPublicLifecycleStatus.fromWire('Available'),
          LongHorizonPublicLifecycleStatus.unknown,
          reason:
              'PascalCase must NOT match -- it is not what the backend actually sends');
    });

    test('LongHorizonPublicPhase', () {
      expect(LongHorizonPublicPhase.fromWire('general_endurance'),
          LongHorizonPublicPhase.generalEndurance);
      expect(LongHorizonPublicPhase.fromWire('preparation_runway'),
          LongHorizonPublicPhase.preparationRunway);
      expect(
          LongHorizonPublicPhase.fromWire('core'), LongHorizonPublicPhase.core);
      expect(LongHorizonPublicPhase.fromWire('GeneralEndurance'),
          LongHorizonPublicPhase.unknown);
    });

    test('LongHorizonConfirmationReadiness', () {
      expect(
          LongHorizonConfirmationReadiness.fromWire(
              'not_ready_for_confirmation'),
          LongHorizonConfirmationReadiness.notReadyForConfirmation);
      expect(
          LongHorizonConfirmationReadiness.fromWire(
              'ready_for_rolling_persistence'),
          LongHorizonConfirmationReadiness.readyForRollingPersistence);
      expect(
          LongHorizonConfirmationReadiness.fromWire(
              'ready_for_legacy_full_persistence'),
          LongHorizonConfirmationReadiness.readyForLegacyFullPersistence);
      expect(
          LongHorizonConfirmationReadiness.fromWire(
              'ReadyForRollingPersistence'),
          LongHorizonConfirmationReadiness.unknown);
    });

    test('LongHorizonPreviewReadiness', () {
      expect(LongHorizonPreviewReadiness.fromWire('ready_for_public_preview'),
          LongHorizonPreviewReadiness.readyForPublicPreview);
      expect(LongHorizonPreviewReadiness.fromWire('public_preview_blocked'),
          LongHorizonPreviewReadiness.publicPreviewBlocked);
    });

    test('LongHorizonConfirmationOutcome', () {
      expect(LongHorizonConfirmationOutcome.fromWire('confirmed'),
          LongHorizonConfirmationOutcome.confirmed);
      expect(LongHorizonConfirmationOutcome.fromWire('already_confirmed'),
          LongHorizonConfirmationOutcome.alreadyConfirmed);
      expect(LongHorizonConfirmationOutcome.fromWire('Confirmed'),
          LongHorizonConfirmationOutcome.unknown);
    });

    test('LongHorizonPublicBlockedReasonCategory', () {
      expect(
          LongHorizonPublicBlockedReasonCategory.fromWire(
              'more_training_data_needed'),
          LongHorizonPublicBlockedReasonCategory.moreTrainingDataNeeded);
      expect(
          LongHorizonPublicBlockedReasonCategory.fromWire(
              'safety_review_required'),
          LongHorizonPublicBlockedReasonCategory.safetyReviewRequired);
    });

    test('LongHorizonPublicProvenance', () {
      expect(
          LongHorizonPublicProvenance.fromWire(
              'generated_from_recent_training'),
          LongHorizonPublicProvenance.generatedFromRecentTraining);
      expect(
          LongHorizonPublicProvenance.fromWire('awaiting_more_training_data'),
          LongHorizonPublicProvenance.awaitingMoreTrainingData);
    });

    test(
        'LongHorizonCheckpointReadiness (already-correct baseline, guarded against regression)',
        () {
      expect(
          LongHorizonCheckpointReadiness.fromWire('current_window_in_progress'),
          LongHorizonCheckpointReadiness.currentWindowInProgress);
      expect(LongHorizonCheckpointReadiness.fromWire('current_window_complete'),
          LongHorizonCheckpointReadiness.currentWindowComplete);
      expect(
          LongHorizonCheckpointReadiness.fromWire(
              'next_window_activation_ready'),
          LongHorizonCheckpointReadiness.nextWindowActivationReady);
      expect(LongHorizonCheckpointReadiness.fromWire('reassessment_required'),
          LongHorizonCheckpointReadiness.reassessmentRequired);
      expect(LongHorizonCheckpointReadiness.fromWire('terminal_plan_complete'),
          LongHorizonCheckpointReadiness.terminalPlanComplete);
    });
  });

  group('LongHorizonPlanPreviewContract', () {
    test('decodes full preview payload including nested roadmap/session shapes',
        () {
      final json = {
        'preview_id': 'preview-abc',
        'goal_type': 'race',
        'goal_distance': 'marathon',
        'total_weeks': 32,
        'start_date': '2026-01-05',
        'estimated_end_date': '2026-08-17',
        'race_date': '2026-08-16',
        'current_window_start_week': 1,
        'current_window_end_week': 8,
        'current_executable_week_count': 8,
        'preview_readiness': 'ready_for_public_preview',
        'confirmation_readiness': 'ready_for_rolling_persistence',
        'public_warnings': <String>[],
        'provenance_summary': 'generated_from_initial_profile',
        'expires_at_utc': '2026-01-06T00:00:00Z',
        'structural_roadmap': [
          {
            'global_week': 1,
            'phase': 'general_endurance',
            'stage': 'base',
            'lifecycle_status': 'available',
            'is_executable': true,
            'structural_start_date': '2026-01-05',
            'structural_end_date': '2026-01-11',
            'numeric_details_available': true,
            'public_summary': 'Base building',
          },
          {
            'global_week': 20,
            'phase': 'core',
            'lifecycle_status': 'pending',
            'is_executable': false,
            'structural_start_date': '2026-05-25',
            'structural_end_date': '2026-05-31',
            'numeric_details_available': false,
            'public_summary': 'Unlocks later',
          },
        ],
        'current_executable_weeks': [
          {
            'global_week': 1,
            'phase': 'general_endurance',
            'stage': 'base',
            'week_start_date': '2026-01-05',
            'week_end_date': '2026-01-11',
            'weekly_volume_km': 25.5,
            'long_run_volume_km': 10.0,
            'lifecycle_status': 'available',
            'public_provenance_summary': 'generated_from_initial_profile',
            'sessions': [
              {
                'session_date': '2026-01-05',
                'weekday': 'Monday',
                'session_role': 'EASY_SUPPORT',
                'distance_km': 5.0,
                'is_long_run': false,
                'executable_status': 'available',
              },
            ],
          },
        ],
      };

      final preview = LongHorizonPlanPreviewContract.fromJson(json);

      expect(preview.previewId, 'preview-abc');
      expect(preview.totalWeeks, 32);
      expect(preview.previewReadiness,
          LongHorizonPreviewReadiness.readyForPublicPreview);
      expect(preview.confirmationReadiness,
          LongHorizonConfirmationReadiness.readyForRollingPersistence);
      expect(preview.isConfirmable, isTrue);
      expect(preview.isBlocked, isFalse);
      expect(preview.provenanceSummary,
          LongHorizonPublicProvenance.generatedFromInitialProfile);
      expect(preview.blockedState, isNull);
      expect(preview.structuralRoadmap, hasLength(2));
      expect(preview.structuralRoadmap[1].isExecutable, isFalse);
      expect(preview.structuralRoadmap[1].numericDetailsAvailable, isFalse);
      expect(preview.currentExecutableWeeks, hasLength(1));
      expect(preview.currentExecutableWeeks.single.sessions.single.sessionRole,
          'EASY_SUPPORT');
      expect(preview.currentExecutableWeeks.single.phase,
          LongHorizonPublicPhase.generalEndurance);
      expect(preview.currentExecutableWeeks.single.publicProvenanceSummary,
          LongHorizonPublicProvenance.generatedFromInitialProfile);
    });

    test('not_ready_for_confirmation is never confirmable', () {
      final preview = LongHorizonPlanPreviewContract.fromJson({
        'preview_id': 'p',
        'goal_type': 'race',
        'goal_distance': 'marathon',
        'total_weeks': 30,
        'start_date': '2026-01-01',
        'estimated_end_date': '2026-08-01',
        'race_date': '2026-08-01',
        'current_window_start_week': 1,
        'current_window_end_week': 8,
        'current_executable_week_count': 8,
        'preview_readiness': 'ready_for_public_preview',
        'confirmation_readiness': 'not_ready_for_confirmation',
        'public_warnings': <String>[],
        'provenance_summary': 'generated_from_initial_profile',
        'structural_roadmap': [],
        'current_executable_weeks': [],
      });
      expect(preview.isConfirmable, isFalse);
    });

    test('a blocked preview decodes blocked_state with its reason category',
        () {
      final preview = LongHorizonPlanPreviewContract.fromJson({
        'preview_id': 'p',
        'goal_type': 'race',
        'goal_distance': 'marathon',
        'total_weeks': 30,
        'start_date': '2026-01-01',
        'estimated_end_date': '2026-08-01',
        'race_date': '2026-08-01',
        'current_window_start_week': 1,
        'current_window_end_week': 8,
        'current_executable_week_count': 0,
        'preview_readiness': 'public_preview_blocked',
        'confirmation_readiness': 'not_ready_for_confirmation',
        'public_warnings': <String>[],
        'provenance_summary': 'awaiting_more_training_data',
        'structural_roadmap': [],
        'current_executable_weeks': [],
        'blocked_state': {
          'reason_category': 'more_training_data_needed',
          'retry_eligible': false,
          'next_action_key': 'add_recent_training',
          'last_evaluated_date': '2026-01-01',
        },
      });
      expect(preview.isBlocked, isTrue);
      expect(preview.blockedState, isNotNull);
      expect(preview.blockedState!.reasonCategory,
          LongHorizonPublicBlockedReasonCategory.moreTrainingDataNeeded);
      expect(preview.blockedState!.retryEligible, isFalse);
    });
  });

  group('LongHorizonConfirmPlanResponse', () {
    test('decodes confirmation outcome', () {
      final response = LongHorizonConfirmPlanResponse.fromJson({
        'plan_id': 'plan-1',
        'preview_id': 'preview-1',
        'outcome': 'confirmed',
        'total_weeks': 32,
        'next_pending_global_week': 9,
        'plan_status': 'Active',
        'public_message': 'Plan confirmed',
      });
      expect(response.outcome, LongHorizonConfirmationOutcome.confirmed);
      expect(response.planId, 'plan-1');
      expect(response.nextPendingGlobalWeek, 9);
    });
  });

  group('WorkoutRole', () {
    test('decodes only the three canonical tokens, fails closed otherwise', () {
      expect(WorkoutRole.fromWire('KEY_SESSION'), WorkoutRole.keySession);
      expect(WorkoutRole.fromWire('EASY_SUPPORT'), WorkoutRole.easySupport);
      expect(WorkoutRole.fromWire('LONG_RUN'), WorkoutRole.longRun);
      expect(WorkoutRole.fromWire('some_alias'), WorkoutRole.unknown);
    });
  });

  group('LongHorizonRollingSessionResponse', () {
    test('decodes optional planned/actual detail fields when present', () {
      final session = LongHorizonRollingSessionResponse.fromJson({
        'session_id': 'session-1',
        'plan_id': 'plan-1',
        'global_week': 3,
        'phase': 'general_endurance',
        'stage': 'base',
        'assigned_date': '2026-01-19',
        'workout_role': 'LONG_RUN',
        'workout_key': 'LR_EASY',
        'workout_version': 2,
        'planned_distance_km': 12.0,
        'planned_duration_minutes': 70,
        'planned_pace_minutes_per_km': 5.8,
        'planned_intensity': 'easy',
        'outcome': 'completed',
        'is_long_run': true,
        'mutation_allowed': false,
        'public_provenance': 'generated_from_initial_profile',
        'actual_distance_km': 12.3,
        'actual_duration_minutes': 68,
        'actual_pace_minutes_per_km': 5.53,
        'completed_at_utc': '2026-01-19T09:00:00Z',
      });
      expect(session.workoutKey, 'LR_EASY');
      expect(session.workoutVersion, 2);
      expect(session.plannedDurationMinutes, 70);
      expect(session.plannedPaceMinutesPerKm, 5.8);
      expect(session.actualPaceMinutesPerKm, 5.53);
      expect(session.publicProvenance, 'generated_from_initial_profile');
    });

    test(
        'nullable fields absent from a Planned session decode as null, never a fabricated default',
        () {
      final session = LongHorizonRollingSessionResponse.fromJson({
        'session_id': 'session-2',
        'plan_id': 'plan-1',
        'global_week': 3,
        'phase': 'general_endurance',
        'stage': 'base',
        'assigned_date': '2026-01-21',
        'workout_role': 'EASY_SUPPORT',
        'planned_distance_km': 6.0,
        'outcome': 'planned',
        'is_long_run': false,
        'mutation_allowed': true,
        'public_provenance': 'generated_from_initial_profile',
      });
      expect(session.actualDistanceKm, isNull);
      expect(session.actualPaceMinutesPerKm, isNull);
      expect(session.workoutKey, isNull);
      expect(session.notTodayRecordedAtUtc, isNull);
    });
  });

  group('ActiveHomeResult', () {
    test(
        'decodes the rolling shape when schedule_strategy is rolling_long_horizon',
        () {
      final result = ActiveHomeResult.fromJson({
        'schedule_strategy': 'rolling_long_horizon',
        'active_plan': {
          'plan_id': 'plan-1',
          'goal_type': 'race',
          'goal_distance': 'marathon',
          'total_weeks': 32,
          'current_global_week': 3,
          'current_phase': 'general_endurance',
          'current_stage': 'base',
          'current_window_start_week': 1,
          'current_window_end_week': 8,
          'activated_session_count': 12,
          'terminal_session_count': 0,
          'checkpoint_readiness': 'current_window_in_progress',
          'status': 'Active',
          'public_message': 'Week 3 of 32',
        },
        'current_window_sessions': [],
        'has_pending_confirmations': false,
      });

      expect(result.strategy, PlanScheduleStrategy.rollingLongHorizon);
      expect(result.rollingHome, isNotNull);
      expect(result.rollingHome!.activePlan.currentGlobalWeek, 3);
      expect(result.rawStaticJson, isNull);
    });

    test('falls back to the static shape when schedule_strategy is absent', () {
      final json = {'some_static_field': 'value'};
      final result = ActiveHomeResult.fromJson(json);
      expect(result.strategy, PlanScheduleStrategy.staticComplete);
      expect(result.rollingHome, isNull);
      expect(result.rawStaticJson, same(json));
    });
  });

  group('LongHorizonSessionMutationResponse', () {
    test('decodes a completion outcome', () {
      final response = LongHorizonSessionMutationResponse.fromJson({
        'session_id': 'session-1',
        'plan_id': 'plan-1',
        'outcome': 'completed',
        'outcome_version': 1,
        'checkpoint_readiness': 'current_window_in_progress',
        'next_window_activated': false,
      });
      expect(response.outcome, LongHorizonSessionMutationOutcome.completed);
      expect(response.nextWindowActivated, isFalse);
    });
  });

  group('NotTodayReason', () {
    test('every reason maps to a backend-approved wire token', () {
      const approved = {
        'fatigue',
        'soreness',
        'illness',
        'schedule',
        'weather',
        'other'
      };
      for (final reason in NotTodayReason.values) {
        expect(approved.contains(reason.wireValue), isTrue,
            reason: '${reason.wireValue} must be one of $approved');
      }
    });
  });

  group('LongHorizonActivateNextWindowResponse', () {
    test('decodes an activation outcome with activated sessions', () {
      final response = LongHorizonActivateNextWindowResponse.fromJson({
        'plan_id': 'plan-1',
        'outcome': 'activated',
        'activated_global_weeks': [9, 10, 11],
        'activated_sessions': [],
        'checkpoint_readiness': 'current_window_in_progress',
        'plan_status': 'Active',
        'is_terminal': false,
        'activated_at_utc': '2026-03-01T08:00:00Z',
        'public_message': 'Next block activated',
      });
      expect(response.outcome, LongHorizonContinuationOutcome.activated);
      expect(response.activatedGlobalWeeks, [9, 10, 11]);
      expect(response.isTerminal, isFalse);
      expect(response.activatedAtUtc, isNotNull);
    });

    test('decodes a terminal outcome', () {
      final response = LongHorizonActivateNextWindowResponse.fromJson({
        'plan_id': 'plan-1',
        'outcome': 'terminal_plan_complete',
        'activated_global_weeks': [],
        'activated_sessions': [],
        'checkpoint_readiness': 'terminal_plan_complete',
        'plan_status': 'Completed',
        'is_terminal': true,
        'public_message': 'Plan complete',
      });
      expect(response.outcome,
          LongHorizonContinuationOutcome.terminalPlanComplete);
      expect(response.isTerminal, isTrue);
    });
  });

  group('LongHorizonRetryContinuationResponse', () {
    test('decodes a restored-to-pending outcome', () {
      final response = LongHorizonRetryContinuationResponse.fromJson({
        'plan_id': 'plan-1',
        'outcome': 'restored_to_pending',
        'restored_window_range': {'start_global_week': 5, 'end_global_week': 8},
        'current_window_range': {'start_global_week': 5, 'end_global_week': 8},
        'checkpoint_readiness': 'current_window_in_progress',
        'plan_status': 'Active',
        'retried_at_utc': '2026-03-01T08:00:00Z',
        'public_message': 'Restored',
      });
      expect(response.outcome, LongHorizonRetryOutcome.restoredToPending);
      expect(response.restoredWindowRange.startGlobalWeek, 5);
      expect(response.retriedAtUtc, isNotNull);
    });
  });
}
