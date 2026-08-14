// Phase 4L.5B -- shared flow-test harness for the Long-Horizon Flutter
// surfaces, following the exact precedent established by
// `active_plan_test_harness.dart` for the static plan: a fresh, minimal
// `GoRouter` (no `redirect`, no `refreshListenable`) that never touches
// `FirebaseAuth`, plus scripted repository doubles overridden via
// `ProviderScope`. This keeps flow tests deterministic, fast, and free of
// any Firebase test-mocking infrastructure this repo doesn't have.
library;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/api_exception.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/calendar/data/calendar_provider.dart';
import 'package:antigravity_app/features/calendar/presentation/active_calendar_dispatcher_page.dart';
import 'package:antigravity_app/features/home/data/home_provider.dart';
import 'package:antigravity_app/features/home/presentation/active_home_dispatcher_page.dart';
import 'package:antigravity_app/features/onboarding/presentation/long_horizon_plan_preview_page.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/presentation/long_horizon_regenerate_plan_page.dart';
import 'package:antigravity_app/features/profile/data/profile_provider.dart';
import 'package:antigravity_app/features/training_day/presentation/rolling_session_detail_page.dart';
import 'package:antigravity_app/features/training_day/presentation/training_day_detail_page.dart';

/// Scripted [LongHorizonRepository]: every method is driven entirely by
/// fields set before the test pumps its widget, and every call is recorded
/// so tests can assert exact request shape / call counts (duplicate-tap
/// prevention, retry-never-calls-activation, exact-body assertions).
class ScriptedLongHorizonRepository extends LongHorizonRepository {
  ScriptedLongHorizonRepository() : super(ApiClient());

  // ── Scripted responses / errors (settable mid-test) ──────────────────
  LongHorizonPlanPreviewContract? previewResponse;
  Object? previewError;

  LongHorizonConfirmPlanResponse? confirmResponse;
  Object? confirmError;

  ActiveHomeResult? homeResult;
  final List<ActiveHomeResult> homeResults = [];
  Object? homeError;

  final Map<String, ActiveCalendarResult> calendarResultsByMonth = {};
  Object? calendarError;

  final Map<String, LongHorizonRollingSessionDetailResponse> detailById = {};
  final Map<String, List<LongHorizonRollingSessionDetailResponse>>
      detailSequencesById = {};
  Object? detailError;

  LongHorizonSessionMutationResponse? completeResponse;
  Object? completeError;
  LongHorizonSessionMutationResponse? notTodayResponse;
  Object? notTodayError;

  LongHorizonActivateNextWindowResponse? activateResponse;
  Object? activateError;

  LongHorizonRetryContinuationResponse? retryResponse;
  Object? retryError;

  // ── Call recording ─────────────────────────────────────────────────
  int previewCallCount = 0;
  int confirmCallCount = 0;
  int homeCallCount = 0;
  final List<String> calendarMonthsRequested = [];
  final List<String> detailIdsRequested = [];
  int completeCallCount = 0;
  final List<({double distance, int duration})> completeRequests = [];
  int notTodayCallCount = 0;
  final List<NotTodayReason> notTodayRequests = [];
  int activateCallCount = 0;
  int retryCallCount = 0;

  @override
  Future<LongHorizonPlanPreviewContract> generateLongHorizonRacePlanPreview(
      GenerateRacePlanPreviewRequestDto request) async {
    previewCallCount++;
    if (previewError != null) throw previewError!;
    return previewResponse!;
  }

  @override
  Future<LongHorizonConfirmPlanResponse> confirmLongHorizonPlan(
      String previewId) async {
    confirmCallCount++;
    if (confirmError != null) {
      final error = confirmError!;
      confirmError = null;
      throw error;
    }
    return confirmResponse!;
  }

  @override
  Future<ActiveHomeResult> fetchActiveHome() async {
    homeCallCount++;
    if (homeError != null) throw homeError!;
    if (homeResults.isNotEmpty) {
      return homeResults.length == 1
          ? homeResults.first
          : homeResults.removeAt(0);
    }
    return homeResult!;
  }

  @override
  Future<ActiveCalendarResult> fetchActiveCalendar(String month) async {
    calendarMonthsRequested.add(month);
    if (calendarError != null) throw calendarError!;
    return calendarResultsByMonth[month] ??
        ActiveCalendarResult.fromJson({
          'schedule_strategy': 'rolling_long_horizon',
          'plan_id': 'plan-1',
          'month': month,
          'sessions': <Map<String, dynamic>>[],
        });
  }

  @override
  Future<LongHorizonRollingSessionDetailResponse> fetchRollingSessionDetail(
      String sessionId) async {
    detailIdsRequested.add(sessionId);
    if (detailError != null) throw detailError!;
    final sequence = detailSequencesById[sessionId];
    if (sequence != null && sequence.isNotEmpty) {
      return sequence.length == 1 ? sequence.first : sequence.removeAt(0);
    }
    final found = detailById[sessionId];
    if (found == null) {
      throw const LongHorizonRollingSessionNotFoundTestException();
    }
    return found;
  }

  @override
  Future<LongHorizonSessionMutationResponse> completeRollingSession(
    String sessionId, {
    required double actualDistanceKm,
    required int actualDurationMinutes,
  }) async {
    completeCallCount++;
    completeRequests
        .add((distance: actualDistanceKm, duration: actualDurationMinutes));
    if (completeError != null) {
      final error = completeError!;
      completeError = null;
      throw error;
    }
    return completeResponse!;
  }

  @override
  Future<LongHorizonSessionMutationResponse> markRollingSessionNotToday(
    String sessionId,
    NotTodayReason reason,
  ) async {
    notTodayCallCount++;
    notTodayRequests.add(reason);
    if (notTodayError != null) {
      final error = notTodayError!;
      notTodayError = null;
      throw error;
    }
    return notTodayResponse!;
  }

  @override
  Future<LongHorizonActivateNextWindowResponse> activateNextWindow() async {
    activateCallCount++;
    if (activateError != null) {
      final error = activateError!;
      activateError = null;
      throw error;
    }
    return activateResponse!;
  }

  @override
  Future<LongHorizonRetryContinuationResponse> retryContinuation() async {
    retryCallCount++;
    if (retryError != null) {
      final error = retryError!;
      retryError = null;
      throw error;
    }
    return retryResponse!;
  }
}

/// A minimal stand-in for the real `ApiException` thrown by a 404 --
/// avoids depending on the real Dio/HTTP stack in a widget test.
class LongHorizonRollingSessionNotFoundTestException extends ApiException {
  const LongHorizonRollingSessionNotFoundTestException()
      : super(
          message: 'Rolling session not found.',
          errorCode: 'LONG_HORIZON_ROLLING_SESSION_NOT_FOUND',
          statusCode: 404,
        );
}

/// Scripted [PlanRepository] -- only used by static-control flow tests in
/// this phase (the static preview/confirm methods), kept minimal.
class ScriptedStaticPlanRepository extends PlanRepository {
  ScriptedStaticPlanRepository() : super(ApiClient());
  int generateRaceCallCount = 0;
  int confirmCallCount = 0;
  GeneratePreviewResponse? previewResponse;

  int cancelCallCount = 0;
  final List<({String planId, String reason})> cancelRequests = [];
  Object? cancelError;
  CancelPlanResponse? cancelResponse;

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(
      GenerateRacePlanPreviewRequestDto request) async {
    generateRaceCallCount++;
    return previewResponse!;
  }

  @override
  Future<ConfirmPlanResponse> confirmPlan(String previewId) async {
    confirmCallCount++;
    return ConfirmPlanResponse(planId: 'static-plan-1', status: 'active');
  }

  @override
  Future<CancelPlanResponse> cancelPlan(String planId, String reason) async {
    cancelCallCount++;
    cancelRequests.add((planId: planId, reason: reason));
    if (cancelError != null) {
      final error = cancelError!;
      cancelError = null;
      throw error;
    }
    return cancelResponse ??
        CancelPlanResponse(planId: planId, status: 'cancelled');
  }
}

/// Feeds `activePlanDetailsProvider` a queue of scripted responses --
/// [next] is consumed once per read, so an ambiguity test can express
/// "first read (initial page load) returns the still-active plan, second
/// read (post-cancellation verification) returns no active plan" as two
/// queued entries.
class ScriptedActivePlanDetailsSource {
  ScriptedActivePlanDetailsSource(this._queue);
  final List<PlanDetailsResponse> _queue;
  int readCount = 0;

  Future<PlanDetailsResponse> next() async {
    readCount++;
    if (_queue.isEmpty) {
      throw StateError(
          'ScriptedActivePlanDetailsSource: no more scripted responses.');
    }
    return _queue.length == 1 ? _queue.first : _queue.removeAt(0);
  }
}

PlanDetailsResponse activePlan(
    {required bool hasActivePlan, String planId = 'plan-1'}) {
  if (!hasActivePlan) {
    return PlanDetailsResponse(
      hasActivePlan: false,
      planId: '',
      status: 'none',
      goalType: '',
      goalDistance: '',
      level: '',
      daysPerWeek: 0,
      unit: 'km',
      startedAt: DateTime(2026, 1, 5),
      estimatedEndDate: DateTime(2026, 8, 17),
      totalWeeks: 0,
      completedWeeksCount: 0,
      totalPlannedDistance: 0,
      totalCompletedDistance: 0,
      weeks: const [],
    );
  }
  return PlanDetailsResponse(
    hasActivePlan: true,
    planId: planId,
    status: 'active',
    goalType: 'race',
    goalDistance: 'marathon',
    level: 'intermediate',
    daysPerWeek: 4,
    unit: 'km',
    startedAt: DateTime(2026, 1, 5),
    estimatedEndDate: DateTime(2026, 8, 17),
    totalWeeks: 32,
    completedWeeksCount: 2,
    totalPlannedDistance: 200,
    totalCompletedDistance: 40,
    weeks: const [],
  );
}

/// Real route paths, minimal set -- everything a Long-Horizon flow test
/// needs to navigate through (onboarding preview, main shell Home/Calendar,
/// rolling detail, static detail). No `redirect`/`refreshListenable`, so no
/// FirebaseAuth dependency at all, matching `active_plan_test_harness.dart`'s
/// own established precedent.
GoRouter buildLongHorizonFlowTestRouter({required String initialLocation}) =>
    GoRouter(
      initialLocation: initialLocation,
      routes: [
        GoRoute(
          path: AppRoutes.longHorizonPlanPreview,
          builder: (_, __) => const LongHorizonPlanPreviewPage(),
        ),
        ShellRoute(
          builder: (context, state, child) => Scaffold(body: child),
          routes: [
            GoRoute(
                path: AppRoutes.home,
                builder: (_, __) => const ActiveHomeDispatcherPage()),
            GoRoute(
                path: AppRoutes.calendar,
                builder: (_, __) => const ActiveCalendarDispatcherPage()),
          ],
        ),
        GoRoute(
          path: AppRoutes.rollingSessionDetail,
          builder: (_, state) => RollingSessionDetailPage(
              sessionId: state.pathParameters['sessionId'] ?? ''),
        ),
        GoRoute(
          path: AppRoutes.trainingDayDetail,
          builder: (_, state) =>
              TrainingDayDetailPage(dayId: state.pathParameters['dayId'] ?? ''),
        ),
        GoRoute(
          path: AppRoutes.longHorizonRegeneratePlan,
          builder: (_, __) => const LongHorizonRegeneratePlanPage(),
        ),
        GoRoute(
          path: AppRoutes.goalSelection,
          builder: (_, __) =>
              const Scaffold(body: Text('GOAL_SELECTION_PLACEHOLDER')),
        ),
      ],
    );

class LongHorizonFlowHarnessBundle {
  LongHorizonFlowHarnessBundle(
      {required this.repo, required this.staticPlanRepo, required this.router});
  final ScriptedLongHorizonRepository repo;
  final ScriptedStaticPlanRepository staticPlanRepo;
  final GoRouter router;
}

/// Pumps a real page reached via [initialLocation] inside a scripted
/// `ProviderScope` + minimal real-route `GoRouter`. [useMockHome] mirrors
/// the existing app-wide test shortcut flag so Home-reaching flows don't
/// need a full static-plan fixture when only the Long-Horizon path matters.
Future<LongHorizonFlowHarnessBundle> pumpLongHorizonFlowApp(
  WidgetTester tester, {
  required String initialLocation,
  String? initialCalendarMonth,
  List<Override> extraOverrides = const [],
  ScriptedLongHorizonRepository? repository,
  ScriptedActivePlanDetailsSource? activePlanDetailsSource,
  GoRouter? routerOverride,
  bool settle = true,
}) async {
  // Accepts a pre-configured repository so callers can script every
  // response the FIRST build will eagerly request (e.g. Home/Calendar
  // dispatchers watch activeHomeResultProvider immediately on mount) --
  // configuring the repo only after this function's own initial
  // pumpAndSettle would be too late and would surface as a null-check
  // error that the dispatcher's error branch silently swallows into a
  // static-Home fallback.
  final repo = repository ?? ScriptedLongHorizonRepository();
  final staticPlanRepo = ScriptedStaticPlanRepository();
  final router = routerOverride ??
      buildLongHorizonFlowTestRouter(initialLocation: initialLocation);

  final overrides = <Override>[
    longHorizonRepositoryProvider.overrideWithValue(repo),
    // onboardingProvider depends on BOTH repositories regardless of which
    // path a given flow exercises -- must always be overridden here too, or
    // it falls through to a real ApiClient -> FirebaseAuth.instance and
    // crashes in this Firebase-free test sandbox (the exact class of
    // regression fixed for six other test files in Phase 4L.5).
    planRepositoryProvider.overrideWithValue(staticPlanRepo),
    useMockHomeDataProvider.overrideWith((ref) => false),
    if (activePlanDetailsSource != null)
      activePlanDetailsProvider
          .overrideWith((ref) => activePlanDetailsSource.next()),
    ...extraOverrides,
  ];
  if (initialCalendarMonth != null) {
    overrides
        .add(calendarMonthProvider.overrideWith((ref) => initialCalendarMonth));
  }

  await tester.pumpWidget(ProviderScope(
    overrides: overrides,
    child: MaterialApp.router(routerConfig: router),
  ));
  if (settle) {
    await tester.pumpAndSettle();
  } else {
    await tester.pump();
  }

  return LongHorizonFlowHarnessBundle(
      repo: repo, staticPlanRepo: staticPlanRepo, router: router);
}

// ── Shared real-shaped fixture builders ───────────────────────────────

LongHorizonActivePlanSummaryResponse activePlanSummary({
  required LongHorizonCheckpointReadiness checkpointReadiness,
  LongHorizonRecoveryRequirement? recoveryRequirement,
  int currentGlobalWeek = 3,
  int totalWeeks = 32,
}) {
  return LongHorizonActivePlanSummaryResponse.fromJson({
    'plan_id': 'plan-1',
    'goal_type': 'race',
    'goal_distance': 'marathon',
    'total_weeks': totalWeeks,
    'current_global_week': currentGlobalWeek,
    'current_phase': 'general_endurance',
    'current_stage': 'base',
    'current_window_start_week': 1,
    'current_window_end_week': 8,
    'activated_session_count': 12,
    'terminal_session_count': 4,
    'checkpoint_readiness': switch (checkpointReadiness) {
      LongHorizonCheckpointReadiness.currentWindowInProgress =>
        'current_window_in_progress',
      LongHorizonCheckpointReadiness.currentWindowComplete =>
        'current_window_complete',
      LongHorizonCheckpointReadiness.nextWindowActivationReady =>
        'next_window_activation_ready',
      LongHorizonCheckpointReadiness.reassessmentRequired =>
        'reassessment_required',
      LongHorizonCheckpointReadiness.terminalPlanComplete =>
        'terminal_plan_complete',
      LongHorizonCheckpointReadiness.unknown => 'unknown',
    },
    if (recoveryRequirement != null)
      'recovery_requirement': switch (recoveryRequirement) {
        LongHorizonRecoveryRequirement.none => 'none',
        LongHorizonRecoveryRequirement.calendarWindowPending =>
          'calendar_window_pending',
        LongHorizonRecoveryRequirement.regeneratePreviewRequired =>
          'regenerate_preview_required',
        LongHorizonRecoveryRequirement.operationalSupportRequired =>
          'operational_support_required',
        LongHorizonRecoveryRequirement.unknown => 'unknown',
      },
    'status': 'Active',
    'public_message': 'Week $currentGlobalWeek of $totalWeeks',
  });
}

Map<String, dynamic> rollingSessionJson({
  required String id,
  required String date,
  String role = 'EASY_SUPPORT',
  String outcome = 'planned',
  double plannedDistanceKm = 8.0,
  bool mutationAllowed = true,
  double? actualDistanceKm,
}) {
  return {
    'session_id': id,
    'plan_id': 'plan-1',
    'global_week': 3,
    'phase': 'general_endurance',
    'stage': 'base',
    'assigned_date': date,
    'workout_role': role,
    'planned_distance_km': plannedDistanceKm,
    'outcome': outcome,
    'is_long_run': role == 'LONG_RUN',
    'mutation_allowed': mutationAllowed,
    'public_provenance': 'generated_from_initial_profile',
    if (actualDistanceKm != null) 'actual_distance_km': actualDistanceKm,
  };
}

LongHorizonHomeResponse homeResponse({
  required LongHorizonActivePlanSummaryResponse plan,
  List<Map<String, dynamic>> windowSessions = const [],
  Map<String, dynamic>? todayWorkout,
}) {
  return LongHorizonHomeResponse.fromJson({
    'active_plan': plan.toJsonForTest(),
    if (todayWorkout != null) 'today_workout': todayWorkout,
    'current_window_sessions': windowSessions,
    'has_pending_confirmations': false,
  });
}

extension _RoundTrip on LongHorizonActivePlanSummaryResponse {
  /// Re-serializes this already-decoded summary back to JSON for embedding
  /// in a parent fixture -- avoids hand-duplicating every field twice.
  Map<String, dynamic> toJsonForTest() => {
        'plan_id': planId,
        'goal_type': goalType,
        'goal_distance': goalDistance,
        'total_weeks': totalWeeks,
        'current_global_week': currentGlobalWeek,
        'current_phase': currentPhase,
        'current_stage': currentStage,
        'current_window_start_week': currentWindowStartWeek,
        'current_window_end_week': currentWindowEndWeek,
        'activated_session_count': activatedSessionCount,
        'terminal_session_count': terminalSessionCount,
        'checkpoint_readiness': switch (checkpointReadiness) {
          LongHorizonCheckpointReadiness.currentWindowInProgress =>
            'current_window_in_progress',
          LongHorizonCheckpointReadiness.currentWindowComplete =>
            'current_window_complete',
          LongHorizonCheckpointReadiness.nextWindowActivationReady =>
            'next_window_activation_ready',
          LongHorizonCheckpointReadiness.reassessmentRequired =>
            'reassessment_required',
          LongHorizonCheckpointReadiness.terminalPlanComplete =>
            'terminal_plan_complete',
          LongHorizonCheckpointReadiness.unknown => 'unknown',
        },
        if (recoveryRequirement != null)
          'recovery_requirement': switch (recoveryRequirement!) {
            LongHorizonRecoveryRequirement.none => 'none',
            LongHorizonRecoveryRequirement.calendarWindowPending =>
              'calendar_window_pending',
            LongHorizonRecoveryRequirement.regeneratePreviewRequired =>
              'regenerate_preview_required',
            LongHorizonRecoveryRequirement.operationalSupportRequired =>
              'operational_support_required',
            LongHorizonRecoveryRequirement.unknown => 'unknown',
          },
        'status': status,
        'public_message': publicMessage,
      };
}

ActiveHomeResult rollingActiveHomeResult(LongHorizonHomeResponse home) {
  return ActiveHomeResult.fromJson({
    'schedule_strategy': 'rolling_long_horizon',
    'active_plan': home.activePlan.toJsonForTest(),
    if (home.todayWorkout != null)
      'today_workout': _sessionToJson(home.todayWorkout!),
    'current_window_sessions':
        home.currentWindowSessions.map(_sessionToJson).toList(),
    'has_pending_confirmations': home.hasPendingConfirmations,
  });
}

Map<String, dynamic> _sessionToJson(LongHorizonRollingSessionResponse s) =>
    rollingSessionJson(
      id: s.sessionId,
      date: s.assignedDate,
      role: switch (s.workoutRole) {
        WorkoutRole.keySession => 'KEY_SESSION',
        WorkoutRole.easySupport => 'EASY_SUPPORT',
        WorkoutRole.longRun => 'LONG_RUN',
        WorkoutRole.unknown => 'UNKNOWN',
      },
      outcome: switch (s.outcome) {
        RollingSessionOutcome.completed => 'completed',
        RollingSessionOutcome.notToday => 'not_today',
        RollingSessionOutcome.planned => 'planned',
        RollingSessionOutcome.unknown => 'unknown',
      },
      plannedDistanceKm: s.plannedDistanceKm,
      mutationAllowed: s.mutationAllowed,
      actualDistanceKm: s.actualDistanceKm,
    );
