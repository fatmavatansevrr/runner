import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'long_horizon_repository.dart';
import '../../../core/network/long_horizon_dtos.dart';
import '../../home/data/home_provider.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';

/// GET /plans/active/home, strategy-discriminated. This is the single
/// source of truth for whether the signed-in user's active plan is
/// `static_complete` or `rolling_long_horizon` — nothing in the Flutter
/// app infers strategy any other way.
final activeHomeResultProvider = FutureProvider<ActiveHomeResult>((ref) async {
  final repo = ref.watch(longHorizonRepositoryProvider);
  return repo.fetchActiveHome();
});

/// GET /plans/active/calendar?month=YYYY-MM, strategy-discriminated.
/// [month] must be `yyyy-MM`.
final activeCalendarResultProvider =
    FutureProvider.family<ActiveCalendarResult, String>((ref, month) async {
  final repo = ref.watch(longHorizonRepositoryProvider);
  return repo.fetchActiveCalendar(month);
});

/// GET /training-days/rolling/{sessionId}
final rollingSessionDetailProvider =
    FutureProvider.family<LongHorizonRollingSessionDetailResponse, String>(
        (ref, sessionId) async {
  final repo = ref.watch(longHorizonRepositoryProvider);
  return repo.fetchRollingSessionDetail(sessionId);
});

/// `yyyy-MM-dd` -> `yyyy-MM`, so a mutation's real `AssignedDate` (not a
/// guessed "current" month) decides exactly which Calendar month provider
/// instance to invalidate. See Phase 4L.5A Part 9/Part 5.
String monthKeyForDate(String isoDate) =>
    isoDate.length >= 7 ? isoDate.substring(0, 7) : isoDate;

/// Invalidates Home, the static-equivalent providers, and the profile
/// overview -- the parts of app state every Long-Horizon mutation can
/// affect regardless of which specific action triggered it. Does **not**
/// touch any Calendar month provider; callers that know which date(s) were
/// affected must additionally call [invalidateLongHorizonCalendarMonth] for
/// each exact affected month. Never triggers a re-fetch on its own, and
/// never calls activate-next-window or retry automatically.
void invalidateLongHorizonHomeState(WidgetRef ref) {
  ref.invalidate(activeHomeResultProvider);
  ref.invalidate(homeDataProvider);
  ref.invalidate(profileOverviewProvider);
}

/// Invalidates exactly one Calendar month (`yyyy-MM`) — never the whole
/// family — so an unrelated month's already-cached data is left alone
/// (Phase 4L.5A Part 5/Part 29: "unrelated month remains cached").
void invalidateLongHorizonCalendarMonth(WidgetRef ref, String month) {
  ref.invalidate(activeCalendarResultProvider(month));
  ref.invalidate(calendarDataProvider);
}
