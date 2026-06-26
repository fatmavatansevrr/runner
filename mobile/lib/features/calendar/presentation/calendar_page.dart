import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../data/calendar_provider.dart';
import '../../../core/network/dtos.dart';
import '../../profile/data/profile_provider.dart';

class CalendarPage extends ConsumerStatefulWidget {
  const CalendarPage({super.key});

  @override
  ConsumerState<CalendarPage> createState() => _CalendarPageState();
}

class _CalendarPageState extends ConsumerState<CalendarPage> {
  int _getDaysInMonth(int year, int month) {
    if (month == 2) {
      final isLeap =
          (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
      return isLeap ? 29 : 28;
    }
    const days = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    return days[month - 1];
  }

  void _prevMonth(String currentMonthStr) {
    final parts = currentMonthStr.split('-');
    int y = int.parse(parts[0]);
    int m = int.parse(parts[1]);
    if (m == 1) {
      m = 12;
      y -= 1;
    } else {
      m -= 1;
    }
    ref.read(calendarMonthProvider.notifier).state =
        '$y-${m.toString().padLeft(2, '0')}';
  }

  void _nextMonth(String currentMonthStr) {
    final parts = currentMonthStr.split('-');
    int y = int.parse(parts[0]);
    int m = int.parse(parts[1]);
    if (m == 12) {
      m = 1;
      y += 1;
    } else {
      m += 1;
    }
    ref.read(calendarMonthProvider.notifier).state =
        '$y-${m.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    final monthStr = ref.watch(calendarMonthProvider);
    final calendarState = ref.watch(calendarDataProvider);
    final profileAsync = ref.watch(profileOverviewProvider);
    final userName = profileAsync.valueOrNull?.name ?? 'Runner';

    final parts = monthStr.split('-');
    final year = int.parse(parts[0]);
    final month = int.parse(parts[1]);

    // Calendar grid layout
    final firstWeekday = DateTime(year, month, 1).weekday; // Mon=1 … Sun=7
    final paddingOffset = firstWeekday - 1;
    final daysInMonth = _getDaysInMonth(year, month);
    final totalCells = paddingOffset + daysInMonth;

    final now = DateTime.now();
    final monthLabel = DateFormat('MMMM yyyy').format(DateTime(year, month));

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 16),

              // ── Header ──────────────────────────────────────────────────────
              _CalendarHeader(userName: userName),
              const SizedBox(height: 24),

              // ── Month navigator ─────────────────────────────────────────────
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(monthLabel,
                      style: const TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary)),
                  Row(
                    children: [
                      _NavChevron(
                        icon: Icons.chevron_left_rounded,
                        onTap: () => _prevMonth(monthStr),
                      ),
                      const SizedBox(width: 6),
                      _NavChevron(
                        icon: Icons.chevron_right_rounded,
                        onTap: () => _nextMonth(monthStr),
                      ),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 16),

              // ── Calendar grid ───────────────────────────────────────────────
              calendarState.when(
                loading: () => const SizedBox(
                  height: 260,
                  child: LoadingState(message: 'Loading calendar...'),
                ),
                error: (err, _) => _CalendarError(
                  message: err.toString(),
                  onRetry: () => ref.invalidate(calendarDataProvider),
                ),
                data: (workouts) {
                  // Month-level metrics
                  final completed =
                      workouts.where((w) => w.status == 'completed').toList();
                  final missed =
                      workouts.where((w) => w.status == 'missed').toList();
                  final totalRuns =
                      workouts.where((w) => w.dayType != 'rest').length;
                  final completedDist = completed.fold(
                      0.0, (s, w) => s + (w.actualDistanceKm ?? 0.0));
                  final adherence = totalRuns > 0
                      ? (completed.length / totalRuns * 100).round()
                      : 0;

                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Calendar card
                      Container(
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(20),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.04),
                              blurRadius: 12,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          children: [
                            // Weekday headers
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceAround,
                              children: ['S', 'M', 'T', 'W', 'T', 'F', 'S']
                                  .map((d) => SizedBox(
                                        width: 36,
                                        child: Text(d,
                                            textAlign: TextAlign.center,
                                            style: const TextStyle(
                                                fontSize: 12,
                                                fontWeight: FontWeight.w600,
                                                color: AppColors.textMuted)),
                                      ))
                                  .toList(),
                            ),
                            const SizedBox(height: 8),

                            // Day grid
                            GridView.builder(
                              shrinkWrap: true,
                              physics: const NeverScrollableScrollPhysics(),
                              gridDelegate:
                                  const SliverGridDelegateWithFixedCrossAxisCount(
                                crossAxisCount: 7,
                                mainAxisSpacing: 6,
                                crossAxisSpacing: 4,
                                childAspectRatio: 1.0,
                              ),
                              itemCount: totalCells,
                              itemBuilder: (context, index) {
                                if (index < paddingOffset) {
                                  return const SizedBox.shrink();
                                }
                                final dayNumber = index - paddingOffset + 1;
                                final cellDate = DateTime(year, month, dayNumber);
                                final isToday = cellDate.day == now.day &&
                                    cellDate.month == now.month &&
                                    cellDate.year == now.year;

                                final matches = workouts.where((w) =>
                                    w.date.day == dayNumber &&
                                    w.date.month == month &&
                                    w.date.year == year);
                                final workout =
                                    matches.isEmpty ? null : matches.first;

                                final hasDetail = workout != null &&
                                    workout.dayId.isNotEmpty &&
                                    workout.dayId !=
                                        '00000000-0000-0000-0000-000000000000';

                                final (bg, textClr, dotColor) =
                                    _resolveCell(workout, isToday);

                                final dayId = workout?.dayId ?? '';
                                return GestureDetector(
                                  onTap: hasDetail
                                      ? () => context.push(
                                          '/training-day/$dayId')
                                      : null,
                                  child: Column(
                                    children: [
                                      Expanded(
                                        child: Container(
                                          decoration: BoxDecoration(
                                            color: bg,
                                            shape: BoxShape.circle,
                                          ),
                                          child: Center(
                                            child: Text(
                                              dayNumber.toString(),
                                              style: TextStyle(
                                                fontSize: 13,
                                                fontWeight: FontWeight.w600,
                                                color: textClr,
                                              ),
                                            ),
                                          ),
                                        ),
                                      ),
                                      const SizedBox(height: 2),
                                      if (dotColor != null)
                                        Container(
                                          width: 5,
                                          height: 5,
                                          decoration: BoxDecoration(
                                            color: dotColor,
                                            shape: BoxShape.circle,
                                          ),
                                        )
                                      else
                                        const SizedBox(height: 5),
                                    ],
                                  ),
                                );
                              },
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // Legend
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          _LegendDot(color: AppColors.completed, label: 'Completed'),
                          const SizedBox(width: 16),
                          _LegendDot(color: AppColors.missed, label: 'Missed'),
                          const SizedBox(width: 16),
                          _LegendDot(color: AppColors.primary, label: 'Planned'),
                          const SizedBox(width: 16),
                          _LegendDot(color: AppColors.textMuted, label: 'Rest'),
                        ],
                      ),
                      const SizedBox(height: 24),

                      // Month stats cards
                      Row(
                        children: [
                          Expanded(child: _StatCard(label: 'Completed', value: '${completed.length} / $totalRuns', sub: 'runs')),
                          const SizedBox(width: 10),
                          Expanded(child: _StatCard(label: 'Distance', value: '${completedDist.toStringAsFixed(1)}', sub: 'km logged')),
                          const SizedBox(width: 10),
                          Expanded(child: _StatCard(label: 'Adherence', value: '$adherence%', sub: 'completion')),
                        ],
                      ),

                      if (missed.isNotEmpty) ...[
                        const SizedBox(height: 20),
                        Container(
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            color: AppColors.missedLight,
                            borderRadius: BorderRadius.circular(14),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.info_outline_rounded,
                                  color: AppColors.missed, size: 18),
                              const SizedBox(width: 10),
                              Expanded(
                                child: Text(
                                  '${missed.length} run${missed.length > 1 ? 's' : ''} missed this month — that\'s okay, keep going! 🏃',
                                  style: const TextStyle(
                                      fontSize: 13,
                                      color: AppColors.missed,
                                      height: 1.4),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                      const SizedBox(height: 24),
                    ],
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  // Returns (background, textColor, dotColor)
  (Color, Color, Color?) _resolveCell(
      TrainingDayResponse? workout, bool isToday) {
    if (isToday) {
      return (AppColors.ctaDark, Colors.white,
          workout?.status == 'completed' ? AppColors.completed : null);
    }
    if (workout == null || workout.dayType == 'rest') {
      return (Colors.transparent, AppColors.textSecondary, null);
    }
    return switch (workout.status) {
      'completed' => (AppColors.completedLight, AppColors.completed, AppColors.completed),
      'missed'    => (AppColors.missedLight, AppColors.missed, AppColors.missed),
      'skipped'   => (AppColors.border, AppColors.textMuted, null),
      _           => (
          // Planned — type-tinted background
          switch (workout.dayType) {
            'long_run'  => AppColors.longRunCard,
            'interval'  => AppColors.intervalCard,
            'tempo'     => const Color(0xFFFFEDD5),
            _           => AppColors.easyRunCard,
          },
          AppColors.primary,
          AppColors.primary,
        ),
    };
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Header
// ─────────────────────────────────────────────────────────────────────────────

class _CalendarHeader extends StatelessWidget {
  const _CalendarHeader({required this.userName});
  final String userName;

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final dateLabel = DateFormat('EEE, d MMM').format(now).toUpperCase();
    return Row(
      children: [
        CircleAvatar(
          radius: 22,
          backgroundColor: AppColors.primaryLight,
          child: Text(
            userName.isNotEmpty ? userName[0].toUpperCase() : 'R',
            style: const TextStyle(
              fontWeight: FontWeight.w700,
              color: AppColors.primary,
              fontSize: 17,
            ),
          ),
        ),
        const SizedBox(width: 12),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Hello, $userName',
                style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary)),
            Text(dateLabel,
                style: const TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w500,
                    color: AppColors.textSecondary,
                    letterSpacing: 0.5)),
          ],
        ),
        const Spacer(),
        IconButton(
          icon: const Icon(Icons.search_rounded, color: AppColors.textPrimary),
          onPressed: null,
        ),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Chevron nav button
// ─────────────────────────────────────────────────────────────────────────────

class _NavChevron extends StatelessWidget {
  const _NavChevron({required this.icon, required this.onTap});
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: AppColors.border),
        ),
        child: Icon(icon, size: 20, color: AppColors.textPrimary),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Legend dot item
// ─────────────────────────────────────────────────────────────────────────────

class _LegendDot extends StatelessWidget {
  const _LegendDot({required this.color, required this.label});
  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 8,
          height: 8,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 4),
        Text(label,
            style: const TextStyle(fontSize: 11, color: AppColors.textSecondary)),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Stat card
// ─────────────────────────────────────────────────────────────────────────────

class _StatCard extends StatelessWidget {
  const _StatCard({required this.label, required this.value, required this.sub});
  final String label;
  final String value;
  final String sub;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          Text(label,
              style: const TextStyle(
                  fontSize: 10,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textMuted,
                  letterSpacing: 0.4)),
          const SizedBox(height: 4),
          Text(value,
              style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary)),
          Text(sub,
              style: const TextStyle(
                  fontSize: 10, color: AppColors.textMuted)),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Error state
// ─────────────────────────────────────────────────────────────────────────────

class _CalendarError extends StatelessWidget {
  const _CalendarError({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 280,
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.cloud_off_rounded,
                size: 48, color: AppColors.textMuted),
            const SizedBox(height: AppSpacing.md),
            Text('Error loading calendar', style: AppTextStyles.h3),
            const SizedBox(height: AppSpacing.xs),
            Text(message,
                style: const TextStyle(
                    fontSize: 13, color: AppColors.textSecondary),
                textAlign: TextAlign.center),
            const SizedBox(height: AppSpacing.md),
            ElevatedButton(
              onPressed: onRetry,
              child: const Text('Retry'),
            ),
          ],
        ),
      ),
    );
  }
}
