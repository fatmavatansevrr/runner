import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/core/widgets/app_button.dart';
import 'package:antigravity_app/features/calendar/data/calendar_provider.dart';
import 'package:antigravity_app/features/calendar/presentation/long_horizon_calendar_page.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';
import 'support/long_horizon_flow_test_harness.dart';

class _FakeCalendarRepo extends LongHorizonRepository {
  _FakeCalendarRepo(this.json) : super(ApiClient());
  final Map<String, dynamic> json;

  @override
  Future<ActiveCalendarResult> fetchActiveCalendar(String month) async =>
      ActiveCalendarResult.fromJson(json);
}

Widget _wrapCalendar(Map<String, dynamic> json) {
  return ProviderScope(
    overrides: [
      longHorizonRepositoryProvider.overrideWithValue(_FakeCalendarRepo(json)),
      calendarMonthProvider.overrideWith((ref) => '2026-01'),
    ],
    child: MaterialApp.router(
      routerConfig: GoRouter(routes: [
        GoRoute(path: '/', builder: (_, __) => const LongHorizonCalendarPage()),
        GoRoute(
            path: '/training-day/rolling/:sessionId',
            builder: (_, __) => const SizedBox()),
      ]),
    ),
  );
}

Map<String, dynamic> _calendarJson(List<Map<String, dynamic>> sessions) => {
      'schedule_strategy': 'rolling_long_horizon',
      'plan_id': 'plan-1',
      'month': '2026-01',
      'sessions': sessions,
    };

Map<String, dynamic> _session({
  required String id,
  required String role,
  required String outcome,
}) =>
    {
      'session_id': id,
      'plan_id': 'plan-1',
      'global_week': 3,
      'phase': 'general_endurance',
      'stage': 'base',
      'assigned_date': '2026-01-05',
      'workout_role': role,
      'planned_distance_km': 8.0,
      'outcome': outcome,
      'is_long_run': role == 'LONG_RUN',
      'mutation_allowed': outcome == 'planned',
      'public_provenance': 'generated_from_initial_profile',
    };

void main() {
  group('AppPrimaryButton loading semantics', () {
    testWidgets(
        'a loading button announces the label plus a loading hint, not silence',
        (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(const MaterialApp(
        home: Scaffold(
            body: AppPrimaryButton(
                label: 'Mark complete', onPressed: null, isLoading: true)),
      ));
      expect(find.bySemanticsLabel('Mark complete, loading'), findsOneWidget);
      handle.dispose();
    });

    testWidgets(
        'a disabled (retry-ineligible) button has no actionable control',
        (tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: Scaffold(
            body: AppPrimaryButton(
                label: 'Retry', onPressed: null, isLoading: false)),
      ));
      final button = tester.widget<ElevatedButton>(find.byType(ElevatedButton));
      expect(button.onPressed, isNull);
    });
  });

  group('Calendar entry semantics', () {
    testWidgets('a Completed entry has a semantic label naming its outcome',
        (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(_wrapCalendar(_calendarJson(
          [_session(id: 's1', role: 'LONG_RUN', outcome: 'completed')])));
      await tester.pumpAndSettle();
      expect(
          find.bySemanticsLabel(RegExp('Long Run.*Completed')), findsOneWidget);
      handle.dispose();
    });

    testWidgets('a NotToday entry has a semantic label naming its outcome',
        (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(_wrapCalendar(_calendarJson(
          [_session(id: 's1', role: 'KEY_SESSION', outcome: 'not_today')])));
      await tester.pumpAndSettle();
      expect(find.bySemanticsLabel(RegExp('Key Session.*Not today')),
          findsOneWidget);
      handle.dispose();
    });

    testWidgets(
        'a long-run entry has a semantic label naming the LONG_RUN canonical role',
        (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(_wrapCalendar(_calendarJson(
          [_session(id: 's1', role: 'LONG_RUN', outcome: 'planned')])));
      await tester.pumpAndSettle();
      expect(find.bySemanticsLabel(RegExp('Long Run')), findsOneWidget);
      handle.dispose();
    });
  });

  group('Text scaling', () {
    testWidgets(
        'increased text scale does not clip the confirm/complete CTA off-screen',
        (tester) async {
      await tester.pumpWidget(
        const MediaQuery(
          data: MediaQueryData(textScaler: TextScaler.linear(2.0)),
          child: MaterialApp(
            home: Scaffold(
                body:
                    AppPrimaryButton(label: 'Mark complete', onPressed: null)),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      expect(find.text('Mark complete'), findsOneWidget);
    });
  });

  group('Regenerate cancellation semantics', () {
    testWidgets('destructive confirmation is announced and remains scroll safe',
        (tester) async {
      final handle = tester.ensureSemantics();
      tester.view.physicalSize = const Size(320, 568);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.home,
        repository: repo,
        activePlanDetailsSource: ScriptedActivePlanDetailsSource([
          activePlan(hasActivePlan: true),
        ]),
      );

      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Stop current plan and continue'));
      await tester.tap(find.text('Stop current plan and continue'));
      await tester.pumpAndSettle();

      expect(
        find.bySemanticsLabel(
          'Stop current plan and continue, destructive action',
        ),
        findsOneWidget,
      );
      expect(tester.takeException(), isNull);
      handle.dispose();
    });
  });
}
