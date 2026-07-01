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
import '../../home/data/home_repository.dart';
import '../../home/data/home_provider.dart';
import '../../../core/widgets/app_button.dart';


class CalendarPage extends ConsumerStatefulWidget {
  const CalendarPage({super.key});

  @override
  ConsumerState<CalendarPage> createState() => _CalendarPageState();
}

class _CalendarPageState extends ConsumerState<CalendarPage> {
  bool _isSubmitting = false;
  DateTime? _selectedDate = DateTime.now();

  @override
  void initState() {
    super.initState();
    print('CALENDAR_PAGE_LOG: CalendarPage Initialized at ${DateTime.now().toIso8601String()}');
  }

  int _getWeekNumber(DateTime date) {
    final firstDayOfYear = DateTime(date.year, 1, 1);
    final daysOffset = firstDayOfYear.weekday - 1; // days before first Monday
    final dayOfYear = date.difference(firstDayOfYear).inDays;
    return ((dayOfYear + daysOffset) / 7).floor() + 1;
  }

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
    final buildStart = DateTime.now();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final renderEnd = DateTime.now();
      print('CALENDAR_PAGE_LOG: Render completed at ${renderEnd.toIso8601String()}. Render duration from build start: ${renderEnd.difference(buildStart).inMilliseconds}ms');
    });

    final monthStr = ref.watch(calendarMonthProvider);
    final calendarState = ref.watch(calendarDataProvider);
    final profileAsync = ref.watch(profileOverviewProvider);
    final userName = profileAsync.valueOrNull?.name ?? 'Runner';
    final planDetailsAsync = ref.watch(activePlanDetailsProvider);
    final planDetails = planDetailsAsync.valueOrNull;

    final parts = monthStr.split('-');
    final year = int.parse(parts[0]);
    final month = int.parse(parts[1]);

    final now = DateTime.now();

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 16),

              // Calendar Title and Month Navigator Row (Figma Style)
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Calendar',
                    style: TextStyle(
                      fontFamily: 'ClashGrotesk',
                      fontSize: 28,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(100),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        GestureDetector(
                          onTap: () => _prevMonth(monthStr),
                          child: const Icon(Icons.chevron_left_rounded, size: 20, color: AppColors.textPrimary),
                        ),
                        const SizedBox(width: 8),
                        Text(
                          DateFormat('MMMM yyyy').format(DateTime(year, month)),
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(width: 8),
                        GestureDetector(
                          onTap: () => _nextMonth(monthStr),
                          child: const Icon(Icons.chevron_right_rounded, size: 20, color: AppColors.textPrimary),
                        ),
                      ],
                    ),
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
                  // Filter workouts that fall in the current week (from Monday to Sunday of the current week).
                  final startOfWeek = now.subtract(Duration(days: now.weekday - 1));
                  final endOfWeek = startOfWeek.add(const Duration(days: 6));
                  final thisWeekWorkouts = workouts.where((w) =>
                      w.date.isAfter(startOfWeek.subtract(const Duration(days: 1))) &&
                      w.date.isBefore(endOfWeek.add(const Duration(days: 1)))).toList();
                  final completedThisWeek = thisWeekWorkouts.where((w) => w.status == 'completed').toList();
                  final totalThisWeek = thisWeekWorkouts.where((w) => w.dayType != 'rest').toList();
                  
                  final runsCompleted = totalThisWeek.isNotEmpty ? completedThisWeek.length : 3;
                  final runsTotal = totalThisWeek.isNotEmpty ? totalThisWeek.length : 4;
                  
                  final completed = workouts.where((w) => w.status == 'completed').toList();
                  final distanceCompleted = completedThisWeek.fold(0.0, (sum, w) => sum + (w.actualDistanceKm ?? 0.0));
                  final distLogged = distanceCompleted > 0 ? distanceCompleted : 15.5;

                  // 1. Generate gridWeeks (Monday-first, 5 or 6 weeks)
                  final firstDayOfMonth = DateTime(year, month, 1);
                  final firstDayWeekday = firstDayOfMonth.weekday; // 1 = Mon ... 7 = Sun
                  final paddingOffset = firstDayWeekday - 1;
                  final gridStartDate = firstDayOfMonth.subtract(Duration(days: paddingOffset));
                  
                  final List<List<DateTime>> gridWeeks = [];
                  var currentDay = gridStartDate;
                  for (int w = 0; w < 6; w++) {
                    final List<DateTime> weekDays = [];
                    for (int d = 0; d < 7; d++) {
                      weekDays.add(currentDay);
                      currentDay = currentDay.add(const Duration(days: 1));
                    }
                    gridWeeks.add(weekDays);
                  }

                  // Let's filter out the 6th week if it contains only days from the next month
                  if (gridWeeks.length == 6) {
                    final sixthWeek = gridWeeks[5];
                    final allNextMonth = sixthWeek.every((d) => d.month != month);
                    if (allNextMonth) {
                      gridWeeks.removeAt(5);
                    }
                  }

                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Large rounded white card
                      Container(
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(24),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withOpacity(0.03),
                              blurRadius: 16,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
                        child: Column(
                          children: [
                            // Table showing weeks and days (M T W T F S S)
                            Table(
                              columnWidths: const {
                                0: FixedColumnWidth(42), // Wk column
                                1: FlexColumnWidth(),
                                2: FlexColumnWidth(),
                                3: FlexColumnWidth(),
                                4: FlexColumnWidth(),
                                5: FlexColumnWidth(),
                                6: FlexColumnWidth(),
                                7: FlexColumnWidth(),
                              },
                              defaultVerticalAlignment: TableCellVerticalAlignment.middle,
                              children: [
                                // Weekday headers: empty, M, T, W, T, F, S, S
                                TableRow(
                                  children: [
                                    const Center(child: Text('')),
                                    ...['M', 'T', 'W', 'T', 'F', 'S', 'S'].map((d) => Padding(
                                      padding: const EdgeInsets.only(bottom: 12),
                                      child: Center(
                                        child: Text(
                                          d,
                                          style: const TextStyle(
                                            fontSize: 12,
                                            fontWeight: FontWeight.w700,
                                            color: AppColors.textSecondary,
                                          ),
                                        ),
                                      ),
                                    )).toList(),
                                  ],
                                ),
                                // Week rows
                                ...gridWeeks.map((weekDays) {
                                  final firstDayOfWeek = weekDays.first;
                                  final weekNum = _getWeekNumber(firstDayOfWeek);
                                  final wkLabel = 'Wk ${weekNum.toString().padLeft(2, '0')}';
                                  
                                  return TableRow(
                                    children: [
                                      // Week Label Cell
                                      Padding(
                                        padding: const EdgeInsets.symmetric(vertical: 4),
                                        child: Center(
                                          child: Text(
                                            wkLabel,
                                            style: const TextStyle(
                                              fontSize: 11,
                                              fontWeight: FontWeight.w600,
                                              color: AppColors.textMuted,
                                            ),
                                          ),
                                        ),
                                      ),
                                      // 7 Day Cells
                                      ...weekDays.map((day) {
                                        final isOverflow = day.month != month;
                                        final isToday = day.day == now.day &&
                                            day.month == now.month &&
                                            day.year == now.year;
                                        
                                        // Selected day check
                                        final isSelected = _selectedDate != null &&
                                            day.day == _selectedDate!.day &&
                                            day.month == _selectedDate!.month &&
                                            day.year == _selectedDate!.year;
                                        
                                        // Find workout
                                        final matches = workouts.where((w) =>
                                            w.date.day == day.day &&
                                            w.date.month == day.month &&
                                            w.date.year == day.year);
                                        final workout = matches.isEmpty ? null : matches.first;
                                        
                                        // Rest days (and days with no scheduled workout) render as
                                        // ordinary calendar cells — no fill, no badge, no markers.
                                        // Only actual running workouts (easy/interval/long run/tempo)
                                        // get a colored highlight.
                                        final isRestDay = isOverflow
                                            ? false
                                            : (workout == null || workout.dayType == 'rest');

                                        // Resolve background color based on training type (dayType)
                                        Color bg = Colors.transparent;
                                        if (!isOverflow && !isRestDay && workout != null) {
                                          bg = switch (workout.dayType) {
                                            'easy' || 'easy_run' => AppColors.easyRunTint,
                                            'interval'           => AppColors.intervalTint,
                                            'long_run'           => AppColors.longRunTint,
                                            'tempo'              => const Color(0xFFFFEDD5),
                                            _                    => Colors.transparent,
                                          };
                                        }

                                        return GestureDetector(
                                          onTap: () {
                                            setState(() {
                                              _selectedDate = day;
                                            });
                                            _showDayDetailModal(context, workout, day, isToday);
                                          },
                                          child: Container(
                                            height: 52,
                                            margin: const EdgeInsets.symmetric(horizontal: 2, vertical: 6),
                                            decoration: BoxDecoration(
                                              color: isSelected
                                                  ? AppColors.ctaDark
                                                  : (isOverflow
                                                      ? Colors.transparent
                                                      : (isRestDay ? Colors.white : bg)),
                                              borderRadius: BorderRadius.circular(8),
                                              border: Border.all(
                                                color: isSelected
                                                    ? AppColors.ctaDark
                                                    : (isToday
                                                        ? AppColors.ctaDark.withOpacity(0.3)
                                                        : (isRestDay ? AppColors.border : Colors.transparent)),
                                                width: 1.5,
                                              ),
                                            ),
                                            child: Column(
                                              mainAxisAlignment: MainAxisAlignment.center,
                                              children: [
                                                // Small weekday label
                                                Text(
                                                  DateFormat('E').format(day).substring(0, 1).toUpperCase(),
                                                  style: TextStyle(
                                                    fontSize: 8,
                                                    fontWeight: FontWeight.w700,
                                                    color: isSelected
                                                        ? Colors.white70
                                                        : (isOverflow
                                                            ? AppColors.textMuted.withOpacity(0.4)
                                                            : AppColors.textMuted),
                                                  ),
                                                ),
                                                const SizedBox(height: 2),
                                                // Date number
                                                Text(
                                                  day.day.toString(),
                                                  style: TextStyle(
                                                    fontSize: 13,
                                                    fontWeight: FontWeight.bold,
                                                    color: isSelected
                                                        ? Colors.white
                                                        : (isOverflow
                                                            ? AppColors.textSecondary.withOpacity(0.3)
                                                            : AppColors.textPrimary),
                                                  ),
                                                ),
                                                const SizedBox(height: 2),
                                                // Missed or completed marker (rest days never show one)
                                                SizedBox(
                                                  height: 10,
                                                  child: isOverflow || isRestDay
                                                      ? null
                                                      : (workout?.status == 'missed'
                                                          ? Icon(
                                                              Icons.close_rounded,
                                                              size: 10,
                                                              color: isSelected ? Colors.white70 : AppColors.ctaDark,
                                                            )
                                                          : (workout?.status == 'completed'
                                                              ? Icon(
                                                                  Icons.check_rounded,
                                                                  size: 10,
                                                                  color: isSelected ? Colors.greenAccent : AppColors.completed,
                                                                )
                                                              : null)),
                                                ),
                                              ],
                                            ),
                                          ),
                                        );
                                      }).toList(),
                                    ],
                                  );
                                }).toList(),
                              ],
                            ),
                            
                            const SizedBox(height: 16),
                            const Divider(color: AppColors.divider),
                            const SizedBox(height: 12),

                            // Legend at the bottom of the calendar card
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                _LegendItem(color: AppColors.easyRunTint, label: 'Easy Run'),
                                _LegendItem(color: AppColors.intervalTint, label: 'Interval'),
                                _LegendItem(color: AppColors.longRunTint, label: 'Long Run'),
                                _LegendItem(
                                  isIcon: true,
                                  icon: Icons.close_rounded,
                                  label: 'Missed',
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                      
                      const SizedBox(height: 16),
                      
                      // Progress cards below the calendar card
                      _ProgressCard(
                        title: 'THIS WEEK',
                        valueText: '$runsCompleted / $runsTotal Runs',
                        subValueText: '${distLogged.toStringAsFixed(1)} km logged',
                        progress: runsTotal > 0 ? (runsCompleted / runsTotal) : 0.75,
                        icon: const Icon(
                          Icons.directions_run_rounded,
                          color: AppColors.primary,
                          size: 24,
                        ),
                      ),
                      const SizedBox(height: 12),
                      _ProgressCard(
                        title: 'OVERALL PROGRESS',
                        valueText: planDetails != null 
                            ? '${planDetails.completedWeeksCount} of ${planDetails.totalWeeks} Weeks'
                            : '8 of 12 Weeks',
                        subValueText: '${planDetails != null ? (planDetails.totalWeeks > 0 ? (planDetails.completedWeeksCount / planDetails.totalWeeks * 100).round() : 65) : 65}% Complete',
                        progress: planDetails != null && planDetails.totalWeeks > 0
                            ? (planDetails.completedWeeksCount / planDetails.totalWeeks)
                            : 0.65,
                      ),
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
      final dotColor = switch (workout?.status) {
        'completed' => AppColors.completed,
        'missed'    => AppColors.missed,
        'planned'   => AppColors.primary,
        _           => null,
      };
      return (AppColors.ctaDark, Colors.white, dotColor);
    }

    if (workout == null || workout.dayType == 'rest') {
      return (Colors.transparent, AppColors.textSecondary, null);
    }

    final bg = switch (workout.dayType) {
      'easy' || 'easy_run' => AppColors.easyRunTint,
      'interval'           => AppColors.intervalTint,
      'long_run'           => AppColors.longRunTint,
      'tempo'              => const Color(0xFFFFEDD5),
      _                    => Colors.transparent,
    };

    final dotColor = switch (workout.status) {
      'completed' => AppColors.completed,
      'missed'    => AppColors.missed,
      'planned'   => AppColors.primary,
      _           => null,
    };

    // Text color is textPrimary since background is a soft tint
    return (bg, AppColors.textPrimary, dotColor);
  }

  // Helper methods for Calendar Selection Modal Flows
  // ================================================

  void _showDayDetailModal(BuildContext context, TrainingDayResponse? workout, DateTime date, bool isToday) {
    final now = DateTime.now();
    final todayDate = DateTime(now.year, now.month, now.day);
    final cellDate = DateTime(date.year, date.month, date.day);
    final isFuture = cellDate.isAfter(todayDate);

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) {
        return Container(
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
          ),
          padding: EdgeInsets.fromLTRB(24, 16, 24, 24 + MediaQuery.of(ctx).viewInsets.bottom),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  margin: const EdgeInsets.only(bottom: 20),
                  decoration: BoxDecoration(
                    color: AppColors.border,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    (isToday ? "TODAY" : DateFormat('EEEE, MMMM d').format(date)).toUpperCase(),
                    style: const TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textSecondary,
                      letterSpacing: 0.8,
                    ),
                  ),
                  GestureDetector(
                    onTap: () => Navigator.pop(ctx),
                    child: Container(
                      width: 28,
                      height: 28,
                      decoration: const BoxDecoration(
                        color: Color(0xFFF3F4F6),
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(
                        Icons.close_rounded,
                        color: AppColors.textSecondary,
                        size: 16,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              if (workout == null || workout.dayType == 'rest') ...[
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Rest Day',
                      style: TextStyle(fontSize: 24, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
                    ),
                    const Text('🏖️', style: TextStyle(fontSize: 28)),
                  ],
                ),
                const SizedBox(height: 16),
                const Text(
                  'Recovery is part of training. Rest, hydrate, and come back stronger.',
                  style: TextStyle(fontSize: 15, color: AppColors.textSecondary, height: 1.4),
                ),
              ] else ...[
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        workout.title,
                        style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w800, color: AppColors.textPrimary),
                      ),
                    ),
                    _resolveBadge(workout.status, isFuture, isToday),
                  ],
                ),
                const SizedBox(height: 8),
                Text(
                  workout.description,
                  style: const TextStyle(fontSize: 14, color: AppColors.textSecondary),
                ),
                const SizedBox(height: 20),
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF8F9FC),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: AppColors.border),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      _buildMetricItem(
                        Icons.straighten_rounded,
                        'DISTANCE',
                        '${workout.plannedDistanceKm.toStringAsFixed(1)} km',
                      ),
                      _buildMetricItem(
                        Icons.timer_outlined,
                        'DURATION',
                        '${workout.plannedDurationMin} min',
                      ),
                      if (workout.plannedPaceMinKm != null)
                        _buildMetricItem(
                          Icons.speed_rounded,
                          'PACE',
                          _fmtPaceLabel(workout.plannedPaceMinKm!),
                        ),
                    ],
                  ),
                ),
                const SizedBox(height: 20),
                if (isFuture) ...[
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.divider,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.lock_clock_outlined, color: AppColors.textSecondary, size: 20),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            "You can't log future runs. Focus on today's goal!",
                            style: TextStyle(fontSize: 13, color: AppColors.textSecondary, height: 1.4),
                          ),
                        ),
                      ],
                    ),
                  ),
                ] else if (isToday && workout.status == 'planned') ...[
                  Row(
                    children: [
                      Expanded(
                        child: _ActionButton(
                          label: 'Completed',
                          icon: Icons.check_rounded,
                          isPositive: true,
                          onTap: () {
                            Navigator.pop(ctx);
                            _showCompletionSheet(context, workout);
                          },
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: _ActionButton(
                          label: 'Not Today',
                          icon: Icons.close_rounded,
                          isPositive: false,
                          onTap: () {
                            Navigator.pop(ctx);
                            _showNotTodaySheet(context, workout);
                          },
                        ),
                      ),
                    ],
                  ),
                ] else if (workout.status == 'completed') ...[
                  Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: AppColors.completedLight,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Row(
                      children: [
                        Icon(Icons.emoji_events_rounded, color: Color(0xFFFFD700), size: 18),
                        SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            "Nice work! Every run brings you closer to your best.",
                            style: TextStyle(fontSize: 13, color: AppColors.completed, height: 1.4, fontWeight: FontWeight.w500),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  _buildComparisonSection(workout),
                ] else if (workout.status == 'missed') ...[
                  Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: AppColors.missedLight,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.info_outline_rounded, color: AppColors.missed, size: 18),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            "One missed run doesn't define your progress. Your plan can continue from here.",
                            style: TextStyle(fontSize: 13, color: AppColors.missed, height: 1.4, fontWeight: FontWeight.w500),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ],
            ],
          ),
        );
      },
    );
  }

  String _fmtPaceLabel(double pace) {
    final low = pace - 0.3;
    final high = pace + 0.3;
    String fmt(double v) {
      final min = v.floor();
      final sec = ((v - min) * 60).round();
      return '$min:${sec.toString().padLeft(2, '0')}';
    }
    return '${fmt(low)}–${fmt(high)}';
  }

  Widget _resolveBadge(String status, bool isFuture, bool isToday) {
    if (isFuture) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        decoration: BoxDecoration(
          color: const Color(0xFFEBF2FF),
          borderRadius: BorderRadius.circular(100),
        ),
        child: const Text(
          'Planned',
          style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: AppColors.primary),
        ),
      );
    }
    if (isToday && status == 'planned') {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        decoration: BoxDecoration(
          color: AppColors.primaryLight,
          borderRadius: BorderRadius.circular(100),
        ),
        child: const Text(
          'Today',
          style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: AppColors.primary),
        ),
      );
    }
    if (status == 'completed') {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        decoration: BoxDecoration(
          color: AppColors.completedLight,
          borderRadius: BorderRadius.circular(100),
        ),
        child: const Text(
          'Completed',
          style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: AppColors.completed),
        ),
      );
    }
    if (status == 'missed') {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        decoration: BoxDecoration(
          color: AppColors.missedLight,
          borderRadius: BorderRadius.circular(100),
        ),
        child: const Text(
          'Not Completed',
          style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: AppColors.missed),
        ),
      );
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: AppColors.divider,
        borderRadius: BorderRadius.circular(100),
      ),
      child: Text(
        status.toUpperCase(),
        style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: AppColors.textSecondary),
      ),
    );
  }

  Widget _buildMetricItem(IconData icon, String label, String value) {
    return Column(
      children: [
        Icon(icon, size: 20, color: AppColors.textSecondary),
        const SizedBox(height: 4),
        Text(
          label,
          style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w600, color: AppColors.textMuted),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
        ),
      ],
    );
  }

  Widget _buildComparisonSection(TrainingDayResponse workout) {
    final actDist = workout.actualDistanceKm ?? workout.plannedDistanceKm;
    final actDur = workout.actualDurationMin ?? workout.plannedDurationMin;
    final avgPace = actDist > 0 ? actDur / actDist : 0.0;
    
    String fmtPace(double v) {
      final min = v.floor();
      final sec = ((v - min) * 60).round();
      return '$min:${sec.toString().padLeft(2, '0')}';
    }

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text('Logged Distance', style: TextStyle(fontSize: 13, color: AppColors.textSecondary)),
              Text('${actDist.toStringAsFixed(1)} km', style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.completed)),
            ],
          ),
          const Divider(height: 20),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text('Logged Duration', style: TextStyle(fontSize: 13, color: AppColors.textSecondary)),
              Text('$actDur min', style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.completed)),
            ],
          ),
          const Divider(height: 20),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text('Average Pace', style: TextStyle(fontSize: 13, color: AppColors.textSecondary)),
              Text('${fmtPace(avgPace)} /km', style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.completed)),
            ],
          ),
        ],
      ),
    );
  }

  void _showCompletionSheet(BuildContext context, TrainingDayResponse workout) {
    final distanceController = TextEditingController(text: workout.plannedDistanceKm.toStringAsFixed(1));
    final durationController = TextEditingController(text: workout.plannedDurationMin.toString());
    String selectedResult = 'as_planned';

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setModal) => Padding(
          padding: EdgeInsets.only(
            left: 24,
            right: 24,
            top: 24,
            bottom: MediaQuery.of(ctx).viewInsets.bottom + 24,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: AppColors.border,
                    borderRadius: BorderRadius.circular(4),
                  ),
                ),
              ),
              const SizedBox(height: 20),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Log Workout',
                      style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                  GestureDetector(
                    onTap: () => Navigator.pop(ctx),
                    child: Container(
                      width: 28,
                      height: 28,
                      decoration: const BoxDecoration(
                        color: Color(0xFFF3F4F6),
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(
                        Icons.close_rounded,
                        color: AppColors.textSecondary,
                        size: 16,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 4),
              const Text('How did it go?',
                  style: TextStyle(fontSize: 14, color: AppColors.textSecondary)),
              const SizedBox(height: 20),

              Row(
                children: [
                  _ResultChip(
                    label: 'As Planned',
                    selected: selectedResult == 'as_planned',
                    onTap: () => setModal(() => selectedResult = 'as_planned'),
                  ),
                  const SizedBox(width: 8),
                  _ResultChip(
                    label: 'Shorter',
                    selected: selectedResult == 'shorter',
                    onTap: () => setModal(() => selectedResult = 'shorter'),
                  ),
                  const SizedBox(width: 8),
                  _ResultChip(
                    label: 'Exceeded',
                    selected: selectedResult == 'exceeded',
                    onTap: () => setModal(() => selectedResult = 'exceeded'),
                  ),
                ],
              ),
              const SizedBox(height: 20),

              Row(
                children: [
                  Expanded(
                    child: _CompactTextField(
                      controller: distanceController,
                      label: 'Distance (km)',
                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: _CompactTextField(
                      controller: durationController,
                      label: 'Duration (min)',
                      keyboardType: TextInputType.number,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),

              if (_isSubmitting)
                const Center(child: CircularProgressIndicator(color: AppColors.primary))
              else
              AppPrimaryButton(
                label: 'Log it',
                isLoading: _isSubmitting,
                onPressed: () async {
                  final dist = double.tryParse(distanceController.text.trim()) ?? workout.plannedDistanceKm;
                  final dur = int.tryParse(durationController.text.trim()) ?? workout.plannedDurationMin;
                  
                  setModal(() => _isSubmitting = true);
                  setState(() => _isSubmitting = true);
                  try {
                    final repo = ref.read(homeRepositoryProvider);
                    await repo.completeWorkout(workout.dayId, dist, dur, 'Completed from calendar selection!');
                    
                    ref.invalidate(calendarDataProvider);
                    ref.invalidate(homeDataProvider);
                    ref.invalidate(profileOverviewProvider);
                    ref.invalidate(activePlanDetailsProvider);
                    
                    if (context.mounted) Navigator.pop(context);
                  } catch (e) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
                    }
                  } finally {
                    setModal(() => _isSubmitting = false);
                    setState(() => _isSubmitting = false);
                  }
                },
              ),
            ],
          ),
        ),
      ),
    ).whenComplete(() {
      distanceController.dispose();
      durationController.dispose();
    });
  }

  void _showNotTodaySheet(BuildContext context, TrainingDayResponse workout) {
    String? selectedReason;
    final reasons = [
      ('need_rest', 'Need rest'),
      ('no_time', 'No time'),
      ('feeling_tired', 'Not feeling it'),
      ('other', 'Other'),
    ];

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setModal) => Padding(
          padding: EdgeInsets.only(
            left: 24,
            right: 24,
            top: 24,
            bottom: MediaQuery.of(ctx).viewInsets.bottom + 24,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: AppColors.border,
                    borderRadius: BorderRadius.circular(4),
                  ),
                ),
              ),
              const SizedBox(height: 20),

              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Skip today\'s workout?',
                      style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800, color: AppColors.textPrimary)),
                  GestureDetector(
                    onTap: () => Navigator.pop(ctx),
                    child: Container(
                      width: 28,
                      height: 28,
                      decoration: const BoxDecoration(
                        color: Color(0xFFF3F4F6),
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(
                        Icons.close_rounded,
                        color: AppColors.textSecondary,
                        size: 16,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              const Text(
                "Rest days are part of progress.\nWe'll adjust your plan to keep you on track.",
                style: TextStyle(fontSize: 14, color: AppColors.textSecondary, height: 1.4),
              ),
              const SizedBox(height: 20),

              const Text('Reason (Optional)',
                  style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.textSecondary, letterSpacing: 0.4)),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: reasons.map((r) {
                  final isSelected = selectedReason == r.$1;
                  return GestureDetector(
                    onTap: () => setModal(() => selectedReason = isSelected ? null : r.$1),
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                      decoration: BoxDecoration(
                        color: isSelected ? AppColors.ctaDark : const Color(0xFFF5F6FA),
                        borderRadius: BorderRadius.circular(100),
                        border: Border.all(color: isSelected ? AppColors.ctaDark : AppColors.border),
                      ),
                      child: Text(
                        r.$2,
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w500,
                          color: isSelected ? Colors.white : AppColors.textPrimary,
                        ),
                      ),
                    ),
                  );
                }).toList(),
              ),
              const SizedBox(height: 20),

              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: AppColors.tipCardBackground,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Icon(Icons.lightbulb_outline_rounded, color: Color(0xFF8B5CF6), size: 18),
                    const SizedBox(width: 10),
                    const Expanded(
                      child: Text(
                        'Consistency is key — Taking a rest today won\'t affect your progress. Your schedule will update automatically.',
                        style: TextStyle(fontSize: 13, color: AppColors.textSecondary, height: 1.4),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              AppPrimaryButton(
                label: 'Got it!',
                isLoading: _isSubmitting,
                onPressed: () async {
                  setModal(() => _isSubmitting = true);
                  setState(() => _isSubmitting = true);
                  try {
                    final repo = ref.read(homeRepositoryProvider);
                    final decision = await repo.createNotTodayDecision(workout.dayId, selectedReason ?? 'other');
                    await repo.confirmNotTodayDecision(decision.decisionId);
                    
                    ref.invalidate(calendarDataProvider);
                    ref.invalidate(homeDataProvider);
                    ref.invalidate(profileOverviewProvider);
                    ref.invalidate(activePlanDetailsProvider);
                    
                    if (context.mounted) Navigator.pop(context);
                  } catch (e) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
                    }
                  } finally {
                    setModal(() => _isSubmitting = false);
                    setState(() => _isSubmitting = false);
                  }
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}


// ─────────────────────────────────────────────────────────────────────────────
// Header
// ─────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────
// Legend Item (Figma Style)
// ─────────────────────────────────────────────────────────────────────────────

class _LegendItem extends StatelessWidget {
  const _LegendItem({
    this.color,
    this.isIcon = false,
    this.icon,
    required this.label,
  });

  final Color? color;
  final bool isIcon;
  final IconData? icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (isIcon && icon != null)
          Icon(icon, size: 12, color: AppColors.textPrimary)
        else
          Container(
            width: 10,
            height: 10,
            decoration: BoxDecoration(
              color: color ?? Colors.transparent,
              borderRadius: BorderRadius.circular(3),
              border: color == Colors.transparent || color == null
                  ? Border.all(color: AppColors.border, width: 1.5)
                  : null,
            ),
          ),
        const SizedBox(width: 4),
        Text(
          label,
          style: const TextStyle(
            fontSize: 10,
            fontWeight: FontWeight.w500,
            color: AppColors.textSecondary,
          ),
        ),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Reusable Progress Card (Figma Style)
// ─────────────────────────────────────────────────────────────────────────────

class _ProgressCard extends StatelessWidget {
  const _ProgressCard({
    required this.title,
    required this.valueText,
    required this.subValueText,
    required this.progress,
    this.icon,
  });

  final String title;
  final String valueText;
  final String subValueText;
  final double progress;
  final Widget? icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textSecondary,
                  letterSpacing: 0.5,
                ),
              ),
              if (icon != null) icon!,
            ],
          ),
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            crossAxisAlignment: CrossAxisAlignment.baseline,
            textBaseline: TextBaseline.alphabetic,
            children: [
              Text(
                valueText,
                style: const TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w500,
                  color: AppColors.textPrimary,
                ),
              ),
              Text(
                subValueText,
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w400,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ClipRRect(
            borderRadius: BorderRadius.circular(100),
            child: LinearProgressIndicator(
              value: progress,
              minHeight: 6,
              backgroundColor: AppColors.divider,
              color: AppColors.primary,
            ),
          ),
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

// ─────────────────────────────────────────────────────────────────────────────
// Result chip for completion sheet in calendar
// ─────────────────────────────────────────────────────────────────────────────

class _ResultChip extends StatelessWidget {
  const _ResultChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });
  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          color: selected ? AppColors.ctaDark : const Color(0xFFF5F6FA),
          borderRadius: BorderRadius.circular(100),
          border: Border.all(
              color: selected ? AppColors.ctaDark : AppColors.border),
        ),
        child: Text(label,
            style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w500,
                color: selected ? Colors.white : AppColors.textPrimary)),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Compact text field for completion sheet in calendar
// ─────────────────────────────────────────────────────────────────────────────

class _CompactTextField extends StatelessWidget {
  const _CompactTextField({
    required this.controller,
    required this.label,
    required this.keyboardType,
  });
  final TextEditingController controller;
  final String label;
  final TextInputType keyboardType;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w500,
                color: AppColors.textSecondary)),
        const SizedBox(height: 4),
        TextField(
          controller: controller,
          keyboardType: keyboardType,
          style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary),
          decoration: InputDecoration(
            contentPadding:
                const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: AppColors.border),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: AppColors.border),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: AppColors.primary),
            ),
          ),
        ),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Action button for today's workout modal in calendar
// ─────────────────────────────────────────────────────────────────────────────

class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.label,
    required this.icon,
    required this.isPositive,
    required this.onTap,
  });
  final String label;
  final IconData icon;
  final bool isPositive;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        height: 44,
        decoration: BoxDecoration(
          color: isPositive ? AppColors.completed : Colors.white,
          borderRadius: BorderRadius.circular(100),
          border: Border.all(
            color: isPositive ? AppColors.completed : AppColors.border,
          ),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon,
                size: 16,
                color: isPositive ? Colors.white : AppColors.textPrimary),
            const SizedBox(width: 6),
            Text(label,
                style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: isPositive ? Colors.white : AppColors.textPrimary)),
          ],
        ),
      ),
    );
  }
}


