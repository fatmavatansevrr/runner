import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/network/long_horizon_dtos.dart';
import '../data/calendar_provider.dart';
import '../../plan/data/long_horizon_provider.dart';
import '../../plan/data/long_horizon_error_mapper.dart';

/// Calendar for an active RollingLongHorizon plan. Renders ONLY the
/// backend's `GET /plans/active/calendar` session list for the selected
/// month ([LongHorizonCalendarResponse.sessions]) -- there is no code path
/// here that can render a structural/Pending roadmap week as an event: this
/// screen never receives [LongHorizonStructuralRoadmapWeekContract] at all
/// (`ActiveCalendarResult`/`LongHorizonCalendarResponse` carry only
/// `LongHorizonRollingSessionResponse` entries), so "Pending weeks leaking
/// into Calendar" is a type-level impossibility, not just a convention.
class LongHorizonCalendarPage extends ConsumerWidget {
  const LongHorizonCalendarPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final month = ref.watch(calendarMonthProvider);
    final asyncResult = ref.watch(activeCalendarResultProvider(month));

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            _MonthHeader(month: month),
            Expanded(
              child: asyncResult.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (err, _) => _ErrorState(
                  message: LongHorizonUiErrorMapper.map(err).userMessage,
                  onRetry: () =>
                      ref.invalidate(activeCalendarResultProvider(month)),
                ),
                data: (result) {
                  final rolling = result.rollingCalendar;
                  if (rolling == null) {
                    // Strategy resolved to static -- the dispatcher should
                    // prevent reaching this widget at all, but fail safe.
                    return const Center(
                        child:
                            Text('This plan uses the standard schedule view.'));
                  }
                  if (rolling.sessions.isEmpty) {
                    return const _EmptyMonthState();
                  }
                  return _SessionList(sessions: rolling.sessions);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MonthHeader extends ConsumerWidget {
  const _MonthHeader({required this.month});
  final String month;

  void _shift(WidgetRef ref, int delta) {
    final parts = month.split('-');
    var y = int.parse(parts[0]);
    var m = int.parse(parts[1]) + delta;
    if (m == 0) {
      m = 12;
      y -= 1;
    } else if (m == 13) {
      m = 1;
      y += 1;
    }
    ref.read(calendarMonthProvider.notifier).state =
        '$y-${m.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          IconButton(
            tooltip: 'Previous month',
            icon: const Icon(Icons.chevron_left_rounded),
            onPressed: () => _shift(ref, -1),
          ),
          Text(month,
              style:
                  const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
          IconButton(
            tooltip: 'Next month',
            icon: const Icon(Icons.chevron_right_rounded),
            onPressed: () => _shift(ref, 1),
          ),
        ],
      ),
    );
  }
}

class _EmptyMonthState extends StatelessWidget {
  const _EmptyMonthState();

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Text(
          'No training sessions this month.',
          textAlign: TextAlign.center,
          style: TextStyle(fontSize: 14, color: AppColors.textSecondary),
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline_rounded,
                size: 40, color: AppColors.textMuted),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            TextButton(onPressed: onRetry, child: const Text('Retry')),
          ],
        ),
      ),
    );
  }
}

class _SessionList extends StatelessWidget {
  const _SessionList({required this.sessions});
  final List<LongHorizonRollingSessionResponse> sessions;

  @override
  Widget build(BuildContext context) {
    // Deterministic date ordering -- the backend already orders by
    // AssignedDate/SessionOrdinal (see LongHorizonActiveReadModelProvider.
    // GetCalendarAsync), but sorting client-side too makes ordering
    // independent of transport reordering and safe to test in isolation.
    final sorted = [...sessions]
      ..sort((a, b) => a.assignedDate.compareTo(b.assignedDate));
    return ListView.builder(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      itemCount: sorted.length,
      itemBuilder: (context, index) => _CalendarSessionTile(
        // Keyed by SessionId -- stable identity across refreshes/mutations,
        // never by list index.
        key: ValueKey(sorted[index].sessionId),
        session: sorted[index],
      ),
    );
  }
}

class _CalendarSessionTile extends StatelessWidget {
  const _CalendarSessionTile({super.key, required this.session});
  final LongHorizonRollingSessionResponse session;

  @override
  Widget build(BuildContext context) {
    final (icon, color, outcomeLabel) = switch (session.outcome) {
      RollingSessionOutcome.completed => (
          Icons.check_circle_rounded,
          const Color(0xFF00A97F),
          'Completed'
        ),
      RollingSessionOutcome.notToday => (
          Icons.remove_circle_outline_rounded,
          AppColors.textMuted,
          'Not today'
        ),
      _ => (Icons.circle_outlined, AppColors.primary, 'Planned'),
    };

    return Semantics(
      label:
          '${session.workoutRole.label}, ${session.assignedDate}, ${session.plannedDistanceKm.toStringAsFixed(1)} kilometers, $outcomeLabel',
      button: true,
      child: InkWell(
        onTap: () => context.push('/training-day/rolling/${session.sessionId}'),
        borderRadius: BorderRadius.circular(14),
        child: Container(
          margin: const EdgeInsets.only(bottom: 10),
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: AppColors.border),
          ),
          child: Row(
            children: [
              ExcludeSemantics(child: Icon(icon, color: color, size: 22)),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(session.workoutRole.label,
                        style: const TextStyle(
                            fontWeight: FontWeight.w600, fontSize: 14)),
                    Text(
                      session.outcome == RollingSessionOutcome.completed &&
                              session.actualDistanceKm != null
                          ? '${session.assignedDate} • ${session.actualDistanceKm!.toStringAsFixed(1)} km actual'
                          : '${session.assignedDate} • ${session.plannedDistanceKm.toStringAsFixed(1)} km',
                      style: const TextStyle(
                          fontSize: 12, color: AppColors.textSecondary),
                    ),
                  ],
                ),
              ),
              Text(outcomeLabel,
                  style: TextStyle(
                      fontSize: 11, color: color, fontWeight: FontWeight.w600)),
              const SizedBox(width: 6),
              const ExcludeSemantics(
                  child: Icon(Icons.chevron_right_rounded,
                      color: AppColors.textMuted, size: 20)),
            ],
          ),
        ),
      ),
    );
  }
}
