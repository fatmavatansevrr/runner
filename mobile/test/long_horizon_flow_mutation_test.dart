// Phase 4L.5B -- proves rolling completion/not-today are fully connected:
// detail -> mutation -> Home/Calendar refresh, duplicate-tap prevention,
// conflict handling through the central error mapper, and that neither
// mutation ever triggers automatic activation.
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/api_exception.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'support/long_horizon_flow_test_harness.dart';

LongHorizonRollingSessionDetailResponse _plannedDetail(
    {String id = 'session-1', String date = '2026-01-05'}) {
  return LongHorizonRollingSessionDetailResponse.fromJson({
    'session': rollingSessionJson(id: id, date: date, role: 'EASY_SUPPORT'),
    'public_description':
        'Complete the assigned session at the prescribed effort.',
  });
}

LongHorizonRollingSessionDetailResponse _outcomeDetail({
  required String outcome,
  double? actualDistanceKm,
  int? actualDurationMinutes,
  String? notTodayReason,
}) =>
    LongHorizonRollingSessionDetailResponse.fromJson({
      'session': {
        ...rollingSessionJson(
          id: 'session-1',
          date: '2026-01-05',
          role: 'EASY_SUPPORT',
          outcome: outcome,
          mutationAllowed: outcome == 'planned',
          actualDistanceKm: actualDistanceKm,
        ),
        if (actualDurationMinutes != null)
          'actual_duration_minutes': actualDurationMinutes,
        if (notTodayReason != null) 'not_today_reason_category': notTodayReason,
      },
      'public_description': 'Authoritative session state.',
    });

ScriptedLongHorizonRepository _ambiguityDetailRepo(
  LongHorizonRollingSessionDetailResponse verified,
) {
  final repo = ScriptedLongHorizonRepository()
    ..homeResult = rollingActiveHomeResult(homeResponse(
      plan: activePlanSummary(
        checkpointReadiness:
            LongHorizonCheckpointReadiness.currentWindowInProgress,
      ),
    ))
    ..calendarResultsByMonth['2026-01'] = ActiveCalendarResult.fromJson({
      'schedule_strategy': 'rolling_long_horizon',
      'plan_id': 'plan-1',
      'month': '2026-01',
      'sessions': [
        rollingSessionJson(
          id: 'session-1',
          date: '2026-01-05',
          role: 'EASY_SUPPORT',
        ),
      ],
    });
  repo.detailSequencesById['session-1'] = [_plannedDetail(), verified];
  return repo;
}

void main() {
  group('Completion flow', () {
    testWidgets(
        'entering valid values and completing sends the exact request and pops back to Calendar',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.currentWindowInProgress),
        ))
        ..detailById['session-1'] = _plannedDetail()
        ..calendarResultsByMonth['2026-01'] = ActiveCalendarResult.fromJson({
          'schedule_strategy': 'rolling_long_horizon',
          'plan_id': 'plan-1',
          'month': '2026-01',
          'sessions': [
            rollingSessionJson(
                id: 'session-1', date: '2026-01-05', role: 'EASY_SUPPORT')
          ],
        })
        ..completeResponse = LongHorizonSessionMutationResponse.fromJson({
          'session_id': 'session-1',
          'plan_id': 'plan-1',
          'outcome': 'completed',
          'outcome_version': 2,
          'checkpoint_readiness': 'current_window_in_progress',
          'next_window_activated': false,
        });
      // Reached via a real push from Calendar (not as the initial route) so
      // there is a real back-stack entry for the detail screen's own
      // `context.pop()` on success to return to -- exactly how this screen
      // is reached in the real app.
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.calendar,
          initialCalendarMonth: '2026-01',
          repository: repo);
      await tester.tap(find.text('Easy Run'));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.widgetWithText(TextField, 'Actual distance (km)'), '8.2');
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual duration (minutes)'), '45');
      await tester.tap(find.text('Mark complete'));
      await tester.pumpAndSettle();

      expect(repo.completeCallCount, 1);
      expect(repo.completeRequests.single.distance, 8.2);
      expect(repo.completeRequests.single.duration, 45);
      // No automatic activation of any kind from a completion mutation.
      expect(repo.activateCallCount, 0);
      // Popped back to Calendar -- proven by the Calendar month header
      // reappearing (the detail screen's own AppBar title is gone).
      expect(find.text('2026-01'), findsOneWidget);
    });

    testWidgets(
        'an invalid (empty) distance/duration blocks the request entirely',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..detailById['session-1'] = _plannedDetail();
      await pumpLongHorizonFlowApp(tester,
          initialLocation: '/training-day/rolling/session-1', repository: repo);

      await tester.tap(find.text('Mark complete'));
      await tester.pumpAndSettle();

      expect(repo.completeCallCount, 0);
      expect(find.text('Enter a valid distance and duration.'), findsOneWidget);
    });

    testWidgets(
        'a completion conflict is mapped centrally and the detail is refreshed, not shown as a raw error',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..detailById['session-1'] = _plannedDetail()
        ..completeError =
            _apiError('LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT');
      await pumpLongHorizonFlowApp(tester,
          initialLocation: '/training-day/rolling/session-1', repository: repo);

      await tester.enterText(
          find.widgetWithText(TextField, 'Actual distance (km)'), '8.2');
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual duration (minutes)'), '45');
      await tester.tap(find.text('Mark complete'));
      await tester.pumpAndSettle();

      // Mapper's message shown, not the raw backend error code/detail.
      expect(
          find.textContaining(
              'LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT'),
          findsNothing);
      // refreshDetail action re-requested the detail (2nd call: initial +
      // post-conflict refresh).
      expect(repo.detailIdsRequested.length, greaterThanOrEqualTo(2));
    });

    testWidgets(
        'a rapid double tap on Mark complete only calls the repository once',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.currentWindowInProgress),
        ))
        ..detailById['session-1'] = _plannedDetail()
        ..calendarResultsByMonth['2026-01'] = ActiveCalendarResult.fromJson({
          'schedule_strategy': 'rolling_long_horizon',
          'plan_id': 'plan-1',
          'month': '2026-01',
          'sessions': [
            rollingSessionJson(
                id: 'session-1', date: '2026-01-05', role: 'EASY_SUPPORT')
          ],
        })
        ..completeResponse = LongHorizonSessionMutationResponse.fromJson({
          'session_id': 'session-1',
          'plan_id': 'plan-1',
          'outcome': 'completed',
          'outcome_version': 2,
          'checkpoint_readiness': 'current_window_in_progress',
          'next_window_activated': false,
        });
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.calendar,
          initialCalendarMonth: '2026-01',
          repository: repo);
      await tester.tap(find.text('Easy Run'));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.widgetWithText(TextField, 'Actual distance (km)'), '8.2');
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual duration (minutes)'), '45');

      final button = tester.widget<ElevatedButton>(
          find.widgetWithText(ElevatedButton, 'Mark complete'));
      button.onPressed?.call();
      button.onPressed?.call();
      await tester.pumpAndSettle();

      expect(repo.completeCallCount, 1);
    });
  });

  group('Not-today flow', () {
    testWidgets('choosing an approved reason sends the exact backend token',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.currentWindowInProgress),
        ))
        ..detailById['session-1'] = _plannedDetail()
        ..calendarResultsByMonth['2026-01'] = ActiveCalendarResult.fromJson({
          'schedule_strategy': 'rolling_long_horizon',
          'plan_id': 'plan-1',
          'month': '2026-01',
          'sessions': [
            rollingSessionJson(
                id: 'session-1', date: '2026-01-05', role: 'EASY_SUPPORT')
          ],
        })
        ..notTodayResponse = LongHorizonSessionMutationResponse.fromJson({
          'session_id': 'session-1',
          'plan_id': 'plan-1',
          'outcome': 'not_today',
          'outcome_version': 2,
          'checkpoint_readiness': 'current_window_in_progress',
          'next_window_activated': false,
        });
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.calendar,
          initialCalendarMonth: '2026-01',
          repository: repo);
      await tester.tap(find.text('Easy Run'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Not today'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Illness'));
      await tester.pumpAndSettle();

      expect(repo.notTodayCallCount, 1);
      expect(repo.notTodayRequests.single.wireValue, 'illness');
      expect(repo.activateCallCount, 0); // never automatic.
    });
  });

  group('Read-after-write ambiguity', () {
    testWidgets('completion timeout plus matching Completed is committed',
        (tester) async {
      final repo = _ambiguityDetailRepo(_outcomeDetail(
        outcome: 'completed',
        actualDistanceKm: 8.2,
        actualDurationMinutes: 45,
      ))
        ..completeError = const ApiException(message: 'timeout');
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.calendar,
        initialCalendarMonth: '2026-01',
        repository: repo,
      );
      await tester.tap(find.text('Easy Run'));
      await tester.pumpAndSettle();
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual distance (km)'), '8.2');
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual duration (minutes)'), '45');
      await tester.tap(find.text('Mark complete'));
      await tester.pumpAndSettle();

      expect(repo.completeCallCount, 1);
      expect(repo.detailIdsRequested.length, greaterThanOrEqualTo(2));
      expect(find.text('2026-01'), findsOneWidget);
    });

    testWidgets('completion timeout plus Planned preserves explicit retry',
        (tester) async {
      final repo = _ambiguityDetailRepo(_plannedDetail())
        ..completeError = const ApiException(message: 'timeout');
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: '/training-day/rolling/session-1',
        repository: repo,
      );
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual distance (km)'), '8.2');
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual duration (minutes)'), '45');
      await tester.tap(find.text('Mark complete'));
      await tester.pumpAndSettle();

      expect(repo.completeCallCount, 1);
      expect(find.textContaining('still planned'), findsOneWidget);
      expect(find.text('Mark complete'), findsOneWidget);
    });

    testWidgets('completion timeout plus different Completed shows conflict',
        (tester) async {
      final repo = _ambiguityDetailRepo(_outcomeDetail(
        outcome: 'completed',
        actualDistanceKm: 7.0,
        actualDurationMinutes: 42,
      ))
        ..completeError = const ApiException(message: 'timeout');
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: '/training-day/rolling/session-1',
        repository: repo,
      );
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual distance (km)'), '8.2');
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual duration (minutes)'), '45');
      await tester.tap(find.text('Mark complete'));
      await tester.pumpAndSettle();

      expect(find.textContaining('different values'), findsOneWidget);
      expect(repo.completeCallCount, 1);
    });

    testWidgets('completion timeout plus NotToday shows outcome conflict',
        (tester) async {
      final repo = _ambiguityDetailRepo(
        _outcomeDetail(outcome: 'not_today'),
      )..completeError = const ApiException(message: 'timeout');
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: '/training-day/rolling/session-1',
        repository: repo,
      );
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual distance (km)'), '8.2');
      await tester.enterText(
          find.widgetWithText(TextField, 'Actual duration (minutes)'), '45');
      await tester.tap(find.text('Mark complete'));
      await tester.pumpAndSettle();

      expect(find.textContaining('marked not today elsewhere'), findsOneWidget);
    });

    testWidgets('not-today timeout plus authoritative NotToday is committed',
        (tester) async {
      final repo = _ambiguityDetailRepo(_outcomeDetail(
        outcome: 'not_today',
        notTodayReason: 'illness',
      ))
        ..notTodayError = const ApiException(message: 'timeout');
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.calendar,
        initialCalendarMonth: '2026-01',
        repository: repo,
      );
      await tester.tap(find.text('Easy Run'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Not today'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Illness'));
      await tester.pumpAndSettle();

      expect(repo.notTodayCallCount, 1);
      expect(repo.detailIdsRequested.length, greaterThanOrEqualTo(2));
      expect(find.text('2026-01'), findsOneWidget);
    });

    testWidgets('not-today timeout plus Planned permits explicit retry',
        (tester) async {
      final repo = _ambiguityDetailRepo(_plannedDetail())
        ..notTodayError = const ApiException(message: 'timeout');
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: '/training-day/rolling/session-1',
        repository: repo,
      );
      await tester.tap(find.text('Not today'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Illness'));
      await tester.pumpAndSettle();

      expect(find.textContaining('still planned'), findsOneWidget);
      expect(find.text('Not today'), findsOneWidget);
    });

    testWidgets('not-today timeout plus Completed shows conflict',
        (tester) async {
      final repo = _ambiguityDetailRepo(_outcomeDetail(
        outcome: 'completed',
        actualDistanceKm: 8,
        actualDurationMinutes: 45,
      ))
        ..notTodayError = const ApiException(message: 'timeout');
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: '/training-day/rolling/session-1',
        repository: repo,
      );
      await tester.tap(find.text('Not today'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Illness'));
      await tester.pumpAndSettle();

      expect(find.textContaining('completed elsewhere'), findsOneWidget);
    });
  });

  group('Activation flow', () {
    testWidgets(
        'the Activate action only appears when NextWindowActivationReady, and calls activation exactly once',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.nextWindowActivationReady),
        ))
        ..activateResponse = LongHorizonActivateNextWindowResponse.fromJson({
          'plan_id': 'plan-1',
          'outcome': 'activated',
          'activated_global_weeks': [9, 10, 11, 12],
          'activated_sessions': [
            rollingSessionJson(
                id: 'new-1', date: '2026-03-02', role: 'LONG_RUN')
          ],
          'checkpoint_readiness': 'current_window_in_progress',
          'plan_status': 'Active',
          'is_terminal': false,
          'public_message': 'Next block activated',
        });
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);

      expect(find.text('Activate next block'), findsOneWidget);
      await tester.tap(find.text('Activate next block'));
      await tester.pumpAndSettle();

      expect(repo.activateCallCount, 1);
      expect(repo.retryCallCount, 0); // activation is never chained from retry.
    });

    testWidgets(
        'no Activate action appears for CurrentWindowInProgress, and activation is never called automatically',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.currentWindowInProgress),
        ));
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);

      expect(find.text('Activate next block'), findsNothing);
      expect(repo.activateCallCount, 0);
    });

    testWidgets('a rapid double tap on Activate only calls the repository once',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.nextWindowActivationReady),
        ))
        ..activateResponse = LongHorizonActivateNextWindowResponse.fromJson({
          'plan_id': 'plan-1',
          'outcome': 'activated',
          'activated_global_weeks': [9],
          'activated_sessions': [],
          'checkpoint_readiness': 'current_window_in_progress',
          'plan_status': 'Active',
          'is_terminal': false,
          'public_message': 'Next block activated',
        });
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);

      final button = tester.widget<ElevatedButton>(
          find.widgetWithText(ElevatedButton, 'Activate next block'));
      button.onPressed?.call();
      button.onPressed?.call();
      await tester.pumpAndSettle();

      expect(repo.activateCallCount, 1);
    });

    testWidgets(
        'activation timeout plus authoritative in-progress window is committed without a second mutation',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..activateError = const ApiException(message: 'timeout');
      repo.homeResults.addAll([
        rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.nextWindowActivationReady,
          ),
        )),
        rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.currentWindowInProgress,
            currentGlobalWeek: 9,
          ),
          windowSessions: [
            rollingSessionJson(id: 'new-1', date: '2026-03-02'),
          ],
        )),
      ]);
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        repository: repo,
      );

      await tester.tap(find.text('Activate next block'));
      await tester.pumpAndSettle();

      expect(repo.activateCallCount, 1);
      expect(
          find.textContaining('next training block is active'), findsOneWidget);
    });

    testWidgets(
        'activation timeout plus still-ready state permits explicit retry',
        (tester) async {
      final ready = rollingActiveHomeResult(homeResponse(
        plan: activePlanSummary(
          checkpointReadiness:
              LongHorizonCheckpointReadiness.nextWindowActivationReady,
        ),
      ));
      final repo = ScriptedLongHorizonRepository()
        ..activateError = const ApiException(message: 'timeout')
        ..homeResults.addAll([ready, ready]);
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        repository: repo,
      );

      await tester.tap(find.text('Activate next block'));
      await tester.pumpAndSettle();

      expect(repo.activateCallCount, 1);
      expect(find.textContaining('still ready'), findsOneWidget);
      expect(find.text('Activate next block'), findsOneWidget);
    });

    testWidgets('activation concurrency refresh renders the winner state once',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..activateError = _apiError(
          'LONG_HORIZON_CONTINUATION_CONCURRENCY_CONFLICT',
        );
      repo.homeResults.addAll([
        rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.nextWindowActivationReady,
          ),
        )),
        rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.currentWindowInProgress,
            currentGlobalWeek: 9,
          ),
          windowSessions: [
            rollingSessionJson(id: 'winner-1', date: '2026-03-02'),
          ],
        )),
      ]);
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        repository: repo,
      );

      await tester.tap(find.text('Activate next block'));
      await tester.pumpAndSettle();

      expect(repo.activateCallCount, 1);
      expect(find.text('Activate next block'), findsNothing);
      expect(find.text('Easy Run'), findsOneWidget);
    });
  });

  group('Retry flow -- separate from activation', () {
    testWidgets(
        'the Retry action only appears for a calendar-window-pending recovery state, and never calls activation',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.calendarWindowPending,
          ),
        ))
        ..retryResponse = LongHorizonRetryContinuationResponse.fromJson({
          'plan_id': 'plan-1',
          'outcome': 'restored_to_pending',
          'restored_window_range': {
            'start_global_week': 5,
            'end_global_week': 8
          },
          'current_window_range': {
            'start_global_week': 5,
            'end_global_week': 8
          },
          'checkpoint_readiness': 'current_window_in_progress',
          'plan_status': 'Active',
          'public_message': 'Restored',
        });
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);

      expect(find.text('Retry'), findsOneWidget);
      expect(find.text('Activate next block'), findsNothing);
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();

      expect(repo.retryCallCount, 1);
      // The defining separation requirement: retry NEVER also calls
      // activation in the same action.
      expect(repo.activateCallCount, 0);
    });

    testWidgets('RegeneratePreviewRequired hides the retry action entirely',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);

      expect(find.text('Retry'), findsNothing);
      expect(find.text('Plan update needed'), findsOneWidget);
    });

    testWidgets(
        'OperationalSupportRequired hides both retry and activate, shows a safe support message',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.operationalSupportRequired,
          ),
        ));
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);

      expect(find.text('Retry'), findsNothing);
      expect(find.text('Activate next block'), findsNothing);
      expect(find.text('Support needed'), findsOneWidget);
    });

    testWidgets(
        'retry timeout plus activation-ready state shows Activate without calling activation',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..retryError = const ApiException(message: 'timeout');
      repo.homeResults.addAll([
        rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.calendarWindowPending,
          ),
        )),
        rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.nextWindowActivationReady,
          ),
        )),
      ]);
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        repository: repo,
      );

      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();

      expect(repo.retryCallCount, 1);
      expect(repo.activateCallCount, 0);
      expect(find.text('Activate next block'), findsOneWidget);
    });

    testWidgets(
        'retry timeout plus unchanged blocked state keeps explicit retry',
        (tester) async {
      final blocked = rollingActiveHomeResult(homeResponse(
        plan: activePlanSummary(
          checkpointReadiness:
              LongHorizonCheckpointReadiness.reassessmentRequired,
          recoveryRequirement:
              LongHorizonRecoveryRequirement.calendarWindowPending,
        ),
      ));
      final repo = ScriptedLongHorizonRepository()
        ..retryError = const ApiException(message: 'timeout')
        ..homeResults.addAll([blocked, blocked]);
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        repository: repo,
      );

      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();

      expect(repo.retryCallCount, 1);
      expect(find.textContaining('retry explicitly'), findsOneWidget);
      expect(find.text('Retry'), findsOneWidget);
    });
  });
}

// Real ApiException, matching exactly what ApiClient._mapError produces
// from the backend's structured error envelope.
ApiException _apiError(String code) => ApiException(
      message: 'raw backend detail for $code',
      errorCode: code,
      statusCode: 409,
    );
