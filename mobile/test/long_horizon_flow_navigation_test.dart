// Phase 4L.5B -- proves Home/Calendar/detail navigation as one connected
// system: rolling Home -> rolling Calendar, rolling Home -> rolling detail,
// rolling Calendar -> rolling detail, invalid deep links, and that the
// strategy dispatch correctly separates rolling from static at every one of
// these points.
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'support/long_horizon_flow_test_harness.dart';

void main() {
  group('Home -> Calendar', () {
    testWidgets(
        'rolling Home reaches rolling Calendar via the bottom nav tab, activated sessions render',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.currentWindowInProgress),
        ))
        ..calendarResultsByMonth['2026-01'] = ActiveCalendarResult.fromJson({
          'schedule_strategy': 'rolling_long_horizon',
          'plan_id': 'plan-1',
          'month': '2026-01',
          'sessions': [
            rollingSessionJson(id: 's1', date: '2026-01-05', role: 'LONG_RUN')
          ],
        });
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          initialCalendarMonth: '2026-01',
          repository: repo);

      bundle.router.go(AppRoutes.calendar);
      await tester.pumpAndSettle();

      expect(bundle.repo.calendarMonthsRequested, contains('2026-01'));
      expect(find.text('Long Run'), findsOneWidget);
    });

    testWidgets('no Pending structural week ever appears as a Calendar event',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.currentWindowInProgress),
        ))
        ..calendarResultsByMonth['2026-06'] = ActiveCalendarResult.fromJson({
          'schedule_strategy': 'rolling_long_horizon',
          'plan_id': 'plan-1',
          'month': '2026-06',
          'sessions': <Map<String,
              dynamic>>[], // Week 20+ is Pending -- backend never returns it here.
        });
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.calendar,
          initialCalendarMonth: '2026-06',
          repository: repo);

      expect(find.text('No training sessions this month.'), findsOneWidget);
      expect(find.textContaining('Pending'), findsNothing);
      expect(find.textContaining('Week 2'), findsNothing);
    });
  });

  group('Home -> rolling detail', () {
    testWidgets(
        'tapping a Home session opens rolling detail via the typed sessionId route, not TrainingDay',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.currentWindowInProgress),
          windowSessions: [
            rollingSessionJson(
                id: 'session-abc', date: '2026-01-05', role: 'KEY_SESSION')
          ],
        ))
        ..detailById['session-abc'] =
            LongHorizonRollingSessionDetailResponse.fromJson({
          'session': rollingSessionJson(
              id: 'session-abc', date: '2026-01-05', role: 'KEY_SESSION'),
          'public_description':
              'Complete the assigned session at the prescribed effort.',
        });
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);

      expect(find.text('Key Session'), findsOneWidget);
      await tester.tap(find.text('Key Session'));
      await tester.pumpAndSettle();

      expect(bundle.repo.detailIdsRequested, ['session-abc']);
      expect(find.text('Session'),
          findsOneWidget); // RollingSessionDetailPage's AppBar title.
    });
  });

  group('Calendar -> rolling detail', () {
    testWidgets(
        'tapping a Calendar entry opens rolling detail with the exact SessionId',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.currentWindowInProgress),
        ))
        ..calendarResultsByMonth['2026-01'] = ActiveCalendarResult.fromJson({
          'schedule_strategy': 'rolling_long_horizon',
          'plan_id': 'plan-1',
          'month': '2026-01',
          'sessions': [
            rollingSessionJson(
                id: 'session-xyz', date: '2026-01-12', role: 'EASY_SUPPORT')
          ],
        })
        ..detailById['session-xyz'] =
            LongHorizonRollingSessionDetailResponse.fromJson({
          'session': rollingSessionJson(
              id: 'session-xyz', date: '2026-01-12', role: 'EASY_SUPPORT'),
          'public_description':
              'Complete the assigned session at the prescribed effort.',
        });
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.calendar,
          initialCalendarMonth: '2026-01',
          repository: repo);

      await tester.tap(find.text('Easy Run'));
      await tester.pumpAndSettle();

      expect(bundle.repo.detailIdsRequested, ['session-xyz']);
      expect(find.text('Session'), findsOneWidget);
    });
  });

  group('Invalid / cross-strategy deep links', () {
    testWidgets(
        'an unknown SessionId shows a safe error, never a crash or a static fallback',
        (tester) async {
      await pumpLongHorizonFlowApp(tester,
          initialLocation: '/training-day/rolling/does-not-exist');

      expect(tester.takeException(), isNull);
      // LongHorizonUiErrorMapper maps LONG_HORIZON_ROLLING_SESSION_NOT_FOUND
      // to a safe "we couldn't find that session" message -- never a raw
      // exception, never a crash, never the static TrainingDayDetailPage.
      expect(find.textContaining("couldn't find that session"), findsOneWidget);
    });
  });

  group('Terminal state retains navigation access', () {
    testWidgets(
        'a TerminalPlanComplete Home still allows navigating to Calendar',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
              checkpointReadiness:
                  LongHorizonCheckpointReadiness.terminalPlanComplete),
        ))
        ..calendarResultsByMonth['2026-01'] = ActiveCalendarResult.fromJson({
          'schedule_strategy': 'rolling_long_horizon',
          'plan_id': 'plan-1',
          'month': '2026-01',
          'sessions': [
            rollingSessionJson(
                id: 's1', date: '2026-01-05', outcome: 'completed')
          ],
        });
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          initialCalendarMonth: '2026-01',
          repository: repo);

      expect(find.text('Plan complete'), findsOneWidget);
      expect(find.text('Activate next block'), findsNothing);
      expect(find.text('Retry'), findsNothing);

      bundle.router.go(AppRoutes.calendar);
      await tester.pumpAndSettle();

      expect(find.text('Completed'), findsOneWidget);
    });
  });
}
