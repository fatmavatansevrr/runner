import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/long_horizon_dtos.dart';
import '../../plan/data/long_horizon_provider.dart';
import 'calendar_page.dart';
import 'long_horizon_calendar_page.dart';

/// Route-level dispatcher for `AppRoutes.calendar`, mirroring
/// [ActiveHomeDispatcherPage]. Decides between the untouched static
/// [CalendarPage] and the new [LongHorizonCalendarPage] using the same
/// `schedule_strategy` read Home already uses (shared Riverpod cache via
/// [activeHomeResultProvider] -- no extra network call over what Home's own
/// dispatcher already triggers).
class ActiveCalendarDispatcherPage extends ConsumerWidget {
  const ActiveCalendarDispatcherPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final asyncResult = ref.watch(activeHomeResultProvider);
    return asyncResult.when(
      loading: () => const Scaffold(body: Center(child: CircularProgressIndicator())),
      // On error, fall through to the static CalendarPage -- it has its own
      // real error/retry UI wired to calendarDataProvider, so this avoids
      // duplicating an error screen here (same pattern as the Home dispatcher).
      error: (err, _) => const CalendarPage(),
      data: (result) {
        if (result.strategy == PlanScheduleStrategy.rollingLongHorizon) {
          return const LongHorizonCalendarPage();
        }
        return const CalendarPage();
      },
    );
  }
}
