import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../data/calendar_provider.dart';
import '../../../core/network/dtos.dart';

class CalendarPage extends ConsumerStatefulWidget {
  const CalendarPage({super.key});

  @override
  ConsumerState<CalendarPage> createState() => _CalendarPageState();
}

class _CalendarPageState extends ConsumerState<CalendarPage> {
  int _getDaysInMonth(int year, int month) {
    if (month == 2) {
      final isLeap = (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
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
    final newStr = '$y-${m.toString().padLeft(2, '0')}';
    ref.read(calendarMonthProvider.notifier).state = newStr;
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
    final newStr = '$y-${m.toString().padLeft(2, '0')}';
    ref.read(calendarMonthProvider.notifier).state = newStr;
  }

  String _getMonthName(int monthNum) {
    const names = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'
    ];
    return names[monthNum - 1];
  }

  @override
  Widget build(BuildContext context) {
    final monthStr = ref.watch(calendarMonthProvider);
    final calendarState = ref.watch(calendarDataProvider);

    final parts = monthStr.split('-');
    final year = int.parse(parts[0]);
    final month = int.parse(parts[1]);

    // Calendar logic
    final firstWeekday = DateTime(year, month, 1).weekday; // Monday = 1, Sunday = 7
    final paddingOffset = firstWeekday - 1; // offset for grid relative to Monday
    final daysInMonth = _getDaysInMonth(year, month);
    final totalCells = paddingOffset + daysInMonth;

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: AppSpacing.md),

              // Calendar Header
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('Calendar', style: AppTextStyles.h1),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xs, vertical: AppSpacing.xs),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Row(
                      children: [
                        IconButton(
                          icon: const Icon(Icons.chevron_left, size: 18, color: AppColors.textPrimary),
                          onPressed: () => _prevMonth(monthStr),
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${_getMonthName(month)} $year',
                          style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(width: 4),
                        IconButton(
                          icon: const Icon(Icons.chevron_right, size: 18, color: AppColors.textPrimary),
                          onPressed: () => _nextMonth(monthStr),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.lg),

              // Calendar Grid State handling
              calendarState.when(
                loading: () => const SizedBox(
                  height: 250,
                  child: LoadingState(message: 'Loading calendar...'),
                ),
                error: (err, _) => SizedBox(
                  height: 280,
                  child: Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.cloud_off_rounded, size: 48, color: AppColors.textMuted),
                        const SizedBox(height: AppSpacing.md),
                        Text('Error loading calendar', style: AppTextStyles.h3),
                        const SizedBox(height: AppSpacing.xs),
                        Text(err.toString(), style: AppTextStyles.bodySmall, textAlign: TextAlign.center),
                        const SizedBox(height: AppSpacing.md),
                        ElevatedButton(
                          onPressed: () => ref.refresh(calendarDataProvider),
                          child: const Text('Retry'),
                        ),
                      ],
                    ),
                  ),
                ),
                data: (workouts) {
                  // Pre-calculate weekly metrics
                  double completedDistance = workouts
                      .where((w) => w.status == 'completed')
                      .fold(0.0, (sum, day) => sum + (day.actualDistanceKm ?? 0.0));
                  int completedCount = workouts.where((w) => w.status == 'completed').length;
                  int totalRuns = workouts.where((w) => w.dayType != 'rest').length;
                  double adherence = totalRuns > 0 ? (completedCount / totalRuns) * 100 : 0.0;

                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      AppCard(
                        padding: const EdgeInsets.all(AppSpacing.md),
                        child: Column(
                          children: [
                            // Weekday headers
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceAround,
                              children: ['M', 'T', 'W', 'T', 'F', 'S', 'S'].map((day) {
                                return SizedBox(
                                  width: 36,
                                  child: Text(
                                    day,
                                    style: AppTextStyles.label.copyWith(fontSize: 12),
                                    textAlign: TextAlign.center,
                                  ),
                                );
                              }).toList(),
                            ),
                            const SizedBox(height: AppSpacing.sm),

                            GridView.builder(
                              shrinkWrap: true,
                              physics: const NeverScrollableScrollPhysics(),
                              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                                crossAxisCount: 7,
                                mainAxisSpacing: 6,
                                crossAxisSpacing: 6,
                                childAspectRatio: 1.0,
                              ),
                              itemCount: totalCells,
                              itemBuilder: (context, index) {
                                if (index < paddingOffset) {
                                  return const SizedBox.shrink();
                                }
                                final dayNumber = index - paddingOffset + 1;
                                
                                // Search workout
                                final matches = workouts.where((w) =>
                                    w.date.day == dayNumber &&
                                    w.date.month == month &&
                                    w.date.year == year);
                                final workout = matches.isEmpty ? null : matches.first;

                                final (bgColor, border, textColor, icon) = _resolveCellState(workout);

                                return Container(
                                  decoration: BoxDecoration(
                                    color: bgColor,
                                    borderRadius: BorderRadius.circular(10),
                                    border: border,
                                  ),
                                  child: Stack(
                                    alignment: Alignment.center,
                                    children: [
                                      Text(
                                        dayNumber.toString(),
                                        style: AppTextStyles.bodyMedium.copyWith(
                                          fontWeight: FontWeight.w600,
                                          color: textColor,
                                        ),
                                      ),
                                      if (icon != null)
                                        Positioned(
                                          bottom: 2,
                                          right: 2,
                                          child: Icon(icon, size: 10, color: textColor),
                                        ),
                                    ],
                                  ),
                                );
                              },
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: AppSpacing.lg),

                      // Legend
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          _LegendItem(color: AppColors.completed, label: 'Completed'),
                          const SizedBox(width: AppSpacing.md),
                          _LegendItem(color: AppColors.missed, label: 'Missed'),
                          const SizedBox(width: AppSpacing.md),
                          _LegendItem(color: AppColors.primary, label: 'Planned'),
                          const SizedBox(width: AppSpacing.md),
                          _LegendItem(color: AppColors.restTint, label: 'Rest Day', isRest: true),
                        ],
                      ),
                      const SizedBox(height: AppSpacing.xl),

                      // Weekly Summary card
                      Text('MONTH PROGRESS SUMMARY', style: AppTextStyles.label),
                      const SizedBox(height: AppSpacing.sm),

                      AppCard(
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceAround,
                          children: [
                            _SummaryMetric(label: 'COMPLETED', val: '$completedCount / $totalRuns runs'),
                            _SummaryMetric(label: 'TOTAL DISTANCE', val: '${completedDistance.toStringAsFixed(1)} km'),
                            _SummaryMetric(label: 'ADHERENCE', val: '${adherence.toStringAsFixed(0)}%'),
                          ],
                        ),
                      ),
                    ],
                  );
                },
              ),
              const SizedBox(height: AppSpacing.lg),
            ],
          ),
        ),
      ),
    );
  }

  (Color, BoxBorder?, Color, IconData?) _resolveCellState(TrainingDayResponse? workout) {
    if (workout == null) {
      // Normal rest / blank day
      return (AppColors.restTint, null, AppColors.textPrimary, null);
    }
    if (workout.dayType == 'rest') {
      return (AppColors.restTint, null, AppColors.textPrimary, null);
    }

    return switch (workout.status) {
      'completed' => (AppColors.completedLight, null, AppColors.completed, Icons.check_circle_rounded),
      'missed'    => (AppColors.missedLight, null, AppColors.missed, Icons.error_rounded),
      'skipped'   => (AppColors.border, null, AppColors.textSecondary, Icons.skip_next_rounded),
      'pending'   => (AppColors.primaryLight, null, AppColors.primary, Icons.help_outline_rounded),
      _           => (Colors.white, Border.all(color: AppColors.primary, width: 1.5), AppColors.primary, null),
    };
  }
}

class _LegendItem extends StatelessWidget {
  const _LegendItem({required this.color, required this.label, this.isRest = false});
  final Color color;
  final String label;
  final bool isRest;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 12,
          height: 12,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(3),
            border: isRest ? Border.all(color: AppColors.border) : null,
          ),
        ),
        const SizedBox(width: 4),
        Text(label, style: AppTextStyles.bodySmall.copyWith(fontSize: 11)),
      ],
    );
  }
}

class _SummaryMetric extends StatelessWidget {
  const _SummaryMetric({required this.label, required this.val});
  final String label;
  final String val;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(label, style: AppTextStyles.label.copyWith(fontSize: 10)),
        const SizedBox(height: 4),
        Text(val, style: AppTextStyles.h3.copyWith(color: AppColors.textPrimary)),
      ],
    );
  }
}
