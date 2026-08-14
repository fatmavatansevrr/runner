import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/features/calendar/data/calendar_provider.dart';
import 'package:antigravity_app/features/calendar/presentation/long_horizon_calendar_page.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';
import 'package:antigravity_app/features/training_day/presentation/rolling_session_detail_page.dart';
import 'package:antigravity_app/core/network/api_client.dart';

LongHorizonRollingSessionResponse _session({
  required String id,
  required String date,
  required String role,
  required RollingSessionOutcome outcome,
  double? actualDistanceKm,
}) {
  return LongHorizonRollingSessionResponse.fromJson({
    'session_id': id,
    'plan_id': 'plan-1',
    'global_week': 3,
    'phase': 'general_endurance',
    'stage': 'base',
    'assigned_date': date,
    'workout_role': role,
    'planned_distance_km': 8.0,
    'outcome': switch (outcome) {
      RollingSessionOutcome.completed => 'completed',
      RollingSessionOutcome.notToday => 'not_today',
      _ => 'planned',
    },
    'is_long_run': role == 'LONG_RUN',
    'mutation_allowed': outcome == RollingSessionOutcome.planned,
    'public_provenance': 'generated_from_initial_profile',
    if (actualDistanceKm != null) 'actual_distance_km': actualDistanceKm,
  });
}

class _FakeLongHorizonRepository extends LongHorizonRepository {
  _FakeLongHorizonRepository(this.sessions) : super(ApiClient());
  final List<LongHorizonRollingSessionResponse> sessions;

  @override
  Future<ActiveCalendarResult> fetchActiveCalendar(String month) async {
    return ActiveCalendarResult.fromJson({
      'schedule_strategy': 'rolling_long_horizon',
      'plan_id': 'plan-1',
      'month': month,
      'sessions': sessions
          .map((s) => {
                'session_id': s.sessionId,
                'plan_id': s.planId,
                'global_week': s.globalWeek,
                'phase': s.phase,
                'stage': s.stage,
                'assigned_date': s.assignedDate,
                'workout_role': switch (s.workoutRole) {
                  WorkoutRole.keySession => 'KEY_SESSION',
                  WorkoutRole.easySupport => 'EASY_SUPPORT',
                  WorkoutRole.longRun => 'LONG_RUN',
                  WorkoutRole.unknown => 'UNKNOWN',
                },
                'planned_distance_km': s.plannedDistanceKm,
                'outcome': switch (s.outcome) {
                  RollingSessionOutcome.completed => 'completed',
                  RollingSessionOutcome.notToday => 'not_today',
                  RollingSessionOutcome.planned => 'planned',
                  RollingSessionOutcome.unknown => 'unknown',
                },
                'is_long_run': s.isLongRun,
                'mutation_allowed': s.mutationAllowed,
                'public_provenance': s.publicProvenance,
                if (s.actualDistanceKm != null)
                  'actual_distance_km': s.actualDistanceKm,
              })
          .toList(),
    });
  }
}

Widget _wrap(Widget child,
    {required List<LongHorizonRollingSessionResponse> sessions}) {
  final router = GoRouter(routes: [
    GoRoute(path: '/', builder: (_, __) => child),
    GoRoute(
      path: '/training-day/rolling/:sessionId',
      builder: (_, state) => RollingSessionDetailPage(
          sessionId: state.pathParameters['sessionId'] ?? ''),
    ),
  ]);
  return ProviderScope(
    overrides: [
      longHorizonRepositoryProvider
          .overrideWithValue(_FakeLongHorizonRepository(sessions)),
      calendarMonthProvider.overrideWith((ref) => '2026-01'),
    ],
    child: MaterialApp.router(routerConfig: router),
  );
}

void main() {
  group('LongHorizonCalendarPage rendering', () {
    testWidgets('renders a Planned session with its canonical role label',
        (tester) async {
      await tester.pumpWidget(_wrap(
        const LongHorizonCalendarPage(),
        sessions: [
          _session(
              id: 's1',
              date: '2026-01-05',
              role: 'EASY_SUPPORT',
              outcome: RollingSessionOutcome.planned)
        ],
      ));
      await tester.pumpAndSettle();
      expect(find.text('Easy Run'), findsOneWidget);
      expect(find.text('Planned'), findsOneWidget);
    });

    testWidgets('renders a Completed session with actual distance and label',
        (tester) async {
      await tester.pumpWidget(_wrap(
        const LongHorizonCalendarPage(),
        sessions: [
          _session(
              id: 's1',
              date: '2026-01-05',
              role: 'LONG_RUN',
              outcome: RollingSessionOutcome.completed,
              actualDistanceKm: 15.2),
        ],
      ));
      await tester.pumpAndSettle();
      expect(find.text('Long Run'), findsOneWidget);
      expect(find.text('Completed'), findsOneWidget);
      expect(find.textContaining('15.2'), findsOneWidget);
    });

    testWidgets('renders a NotToday session with its label', (tester) async {
      await tester.pumpWidget(_wrap(
        const LongHorizonCalendarPage(),
        sessions: [
          _session(
              id: 's1',
              date: '2026-01-05',
              role: 'KEY_SESSION',
              outcome: RollingSessionOutcome.notToday)
        ],
      ));
      await tester.pumpAndSettle();
      expect(find.text('Key Session'), findsOneWidget);
      expect(find.text('Not today'), findsOneWidget);
    });

    testWidgets(
        'empty month renders the safe empty state, not a crash or placeholder event',
        (tester) async {
      await tester.pumpWidget(
          _wrap(const LongHorizonCalendarPage(), sessions: const []));
      await tester.pumpAndSettle();
      expect(find.text('No training sessions this month.'), findsOneWidget);
    });

    testWidgets(
        'entries are sorted deterministically by date regardless of input order',
        (tester) async {
      await tester.pumpWidget(_wrap(
        const LongHorizonCalendarPage(),
        sessions: [
          _session(
              id: 's-later',
              date: '2026-01-20',
              role: 'EASY_SUPPORT',
              outcome: RollingSessionOutcome.planned),
          _session(
              id: 's-earlier',
              date: '2026-01-05',
              role: 'LONG_RUN',
              outcome: RollingSessionOutcome.planned),
        ],
      ));
      await tester.pumpAndSettle();
      final dateTexts = tester
          .widgetList<Text>(find.textContaining('2026-01'))
          .map((t) => t.data)
          .toList();
      final earlierIndex =
          dateTexts.indexWhere((t) => t!.contains('2026-01-05'));
      final laterIndex = dateTexts.indexWhere((t) => t!.contains('2026-01-20'));
      expect(earlierIndex, lessThan(laterIndex));
    });

    testWidgets(
        'a rolling Calendar entry navigates to the rolling session detail route',
        (tester) async {
      await tester.pumpWidget(_wrap(
        const LongHorizonCalendarPage(),
        sessions: [
          _session(
              id: 'session-xyz',
              date: '2026-01-05',
              role: 'EASY_SUPPORT',
              outcome: RollingSessionOutcome.planned)
        ],
      ));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Easy Run'));
      await tester.pumpAndSettle();
      // The push navigated to the rolling detail route -- proven by the
      // AppBar title the RollingSessionDetailPage renders (a loading
      // spinner is shown first since the fake repo has no detail method
      // configured to return synchronously, but the AppBar itself proves
      // the correct route was reached).
      expect(find.text('Session'), findsOneWidget);
    });
  });

  group('Session identity / keys', () {
    testWidgets('entries are keyed by SessionId, not list index',
        (tester) async {
      await tester.pumpWidget(_wrap(
        const LongHorizonCalendarPage(),
        sessions: [
          _session(
              id: 'stable-key-1',
              date: '2026-01-05',
              role: 'EASY_SUPPORT',
              outcome: RollingSessionOutcome.planned)
        ],
      ));
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey('stable-key-1')), findsOneWidget);
    });
  });
}
