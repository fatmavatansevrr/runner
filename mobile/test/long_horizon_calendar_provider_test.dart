import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_provider.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';

class _RecordingRepository extends LongHorizonRepository {
  _RecordingRepository() : super(ApiClient());
  final List<String> requestedMonths = [];

  @override
  Future<ActiveCalendarResult> fetchActiveCalendar(String month) async {
    requestedMonths.add(month);
    return ActiveCalendarResult.fromJson({
      'schedule_strategy': 'rolling_long_horizon',
      'plan_id': 'plan-1',
      'month': month,
      'sessions': <Map<String, dynamic>>[],
    });
  }
}

void main() {
  group('activeCalendarResultProvider month isolation', () {
    test('two different months are requested and cached independently',
        () async {
      final repo = _RecordingRepository();
      final container = ProviderContainer(
          overrides: [longHorizonRepositoryProvider.overrideWithValue(repo)]);
      addTearDown(container.dispose);

      final january =
          await container.read(activeCalendarResultProvider('2026-01').future);
      final february =
          await container.read(activeCalendarResultProvider('2026-02').future);

      expect(january.rollingCalendar!.month, '2026-01');
      expect(february.rollingCalendar!.month, '2026-02');
      expect(repo.requestedMonths, ['2026-01', '2026-02']);
    });

    test('invalidating one month does not re-request an unrelated cached month',
        () async {
      final repo = _RecordingRepository();
      final container = ProviderContainer(
          overrides: [longHorizonRepositoryProvider.overrideWithValue(repo)]);
      addTearDown(container.dispose);

      await container.read(activeCalendarResultProvider('2026-01').future);
      await container.read(activeCalendarResultProvider('2026-02').future);
      expect(repo.requestedMonths, ['2026-01', '2026-02']);

      container.invalidate(activeCalendarResultProvider('2026-01'));
      await container.read(activeCalendarResultProvider('2026-01').future);

      // 2026-01 was re-requested (3rd call), but 2026-02 was never touched
      // again -- proves invalidating one family instance leaves the other
      // month's cache alone (Phase 4L.5A Part 5/29: "unrelated month
      // remains cached").
      expect(repo.requestedMonths, ['2026-01', '2026-02', '2026-01']);
    });
  });

  group('monthKeyForDate', () {
    test('derives yyyy-MM from a yyyy-MM-dd assigned date', () {
      expect(monthKeyForDate('2026-03-17'), '2026-03');
    });
  });

  group('rollingSessionDetailProvider isolation', () {
    test('is keyed by sessionId, not shared across different sessions',
        () async {
      final repo = _RecordingSessionDetailRepository();
      final container = ProviderContainer(
          overrides: [longHorizonRepositoryProvider.overrideWithValue(repo)]);
      addTearDown(container.dispose);

      final a = await container
          .read(rollingSessionDetailProvider('session-a').future);
      final b = await container
          .read(rollingSessionDetailProvider('session-b').future);

      expect(a.session.sessionId, 'session-a');
      expect(b.session.sessionId, 'session-b');
      expect(repo.requestedIds, ['session-a', 'session-b']);
    });
  });
}

class _RecordingSessionDetailRepository extends LongHorizonRepository {
  _RecordingSessionDetailRepository() : super(ApiClient());
  final List<String> requestedIds = [];

  @override
  Future<LongHorizonRollingSessionDetailResponse> fetchRollingSessionDetail(
      String sessionId) async {
    requestedIds.add(sessionId);
    return LongHorizonRollingSessionDetailResponse.fromJson({
      'session': {
        'session_id': sessionId,
        'plan_id': 'plan-1',
        'global_week': 1,
        'phase': 'general_endurance',
        'stage': 'base',
        'assigned_date': '2026-01-05',
        'workout_role': 'EASY_SUPPORT',
        'planned_distance_km': 6.0,
        'outcome': 'planned',
        'is_long_run': false,
        'mutation_allowed': true,
        'public_provenance': 'generated_from_initial_profile',
      },
      'public_description':
          'Complete the assigned session at the prescribed effort.',
    });
  }
}
