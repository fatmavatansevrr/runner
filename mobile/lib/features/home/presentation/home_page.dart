import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/routing/app_router.dart';
import '../data/home_provider.dart';
import '../data/home_repository.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';
import '../../../core/network/dtos.dart';
import '../../../core/widgets/app_button.dart';

enum DayStatus { planned, completed, notToday }

class HomePage extends ConsumerStatefulWidget {
  const HomePage({super.key});

  @override
  ConsumerState<HomePage> createState() => _HomePageState();
}

class _HomePageState extends ConsumerState<HomePage> {
  bool _isSubmitting = false;
  DateTime? _selectedDate;
  final Map<String, DayStatus> _localDayStates = {};
  bool _showCompletionBanner = false;

  // ── Completion sheet ────────────────────────────────────────────────────

  void _showCompletionSheet(String dayId) {
    String selectedOption = 'as_planned';

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
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
              // Header Row
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: 38,
                    height: 38,
                    decoration: const BoxDecoration(
                      color: AppColors.completedLight,
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.check_rounded,
                      color: AppColors.completed,
                      size: 22,
                    ),
                  ),
                  const SizedBox(width: 12),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Run Complete',
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        SizedBox(height: 2),
                        Text(
                          'Great job! How did your run feel?',
                          style: TextStyle(
                            fontSize: 14,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                  GestureDetector(
                    onTap: () => Navigator.pop(ctx),
                    child: const Icon(
                      Icons.close_rounded,
                      color: AppColors.textMuted,
                      size: 22,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),

              // Selection Rows
              _CompletionOptionRow(
                title: 'As planned',
                subtitle: 'Everything went as expected',
                value: 'as_planned',
                groupValue: selectedOption,
                onTap: () => setModal(() => selectedOption = 'as_planned'),
              ),
              const SizedBox(height: 12),
              _CompletionOptionRow(
                title: 'Shorter',
                subtitle: 'I couldn’t complete the whole run',
                value: 'shorter',
                groupValue: selectedOption,
                onTap: () => setModal(() => selectedOption = 'shorter'),
              ),
              const SizedBox(height: 12),
              _CompletionOptionRow(
                title: 'Exceeded',
                subtitle: 'I did more than planned',
                value: 'exceeded',
                groupValue: selectedOption,
                onTap: () => setModal(() => selectedOption = 'exceeded'),
              ),
              const SizedBox(height: 20),

              // Recovery Tip Card
              const _RecoveryTipCard(
                title: 'RECOVERY TIP',
                text: 'Hydrate within 30 minutes to speed up muscle repair.',
              ),
              const SizedBox(height: 24),

              // Save Activity Button
              SizedBox(
                width: double.infinity,
                height: 48,
                child: ElevatedButton(
                  onPressed: () {
                    // Mark as completed locally and show success banner
                    setState(() {
                      _localDayStates[dayId] = DayStatus.completed;
                      _showCompletionBanner = true;
                    });
                    Navigator.pop(ctx);
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.ctaDark,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(24.0),
                    ),
                    elevation: 0,
                  ),
                  child: const Text(
                    'Save Activity',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // ── Not Today sheet ─────────────────────────────────────────────────────

  void _showNotTodayReasonSheet(String dayId) {
    String selectedReason = 'Too tired';
    final reasons = [
      'Too tired',
      'Too busy',
      'Sick or injured',
      'Bad weather',
      'Other',
    ];

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setModal) => Padding(
          padding: EdgeInsets.only(
            left: 24,
            right: 24,
            top: 12,
            bottom: MediaQuery.of(ctx).viewInsets.bottom + 24,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Drag handle
              Center(
                child: Container(
                  width: 36,
                  height: 5,
                  decoration: BoxDecoration(
                    color: AppColors.border,
                    borderRadius: BorderRadius.circular(2.5),
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Title and Close Row
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Not today?',
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  GestureDetector(
                    onTap: () => Navigator.pop(ctx),
                    child: const Icon(
                      Icons.close_rounded,
                      color: AppColors.textMuted,
                      size: 22,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              const Text(
                'Tell us what happened so we can adapt your plan later.',
                style: TextStyle(
                  fontSize: 14,
                  color: AppColors.textSecondary,
                ),
              ),
              const SizedBox(height: 20),

              // Radio Options
              ...reasons.map((reason) {
                final isSelected = selectedReason == reason;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: GestureDetector(
                    onTap: () => setModal(() => selectedReason = reason),
                    behavior: HitTestBehavior.opaque,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(
                          color: isSelected ? AppColors.ctaDark : AppColors.border,
                          width: isSelected ? 1.5 : 1,
                        ),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            reason,
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          IgnorePointer(
                            child: Radio<String>(
                              value: reason,
                              groupValue: selectedReason,
                              activeColor: AppColors.ctaDark,
                              onChanged: (_) {},
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                );
              }).toList(),
              const SizedBox(height: 16),

              // Info card
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: AppColors.tipCardBackground,
                  borderRadius: BorderRadius.circular(16),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: const [
                    Text(
                      'Your plan will adapt',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: Color(0xFF8B5CF6),
                        letterSpacing: 0.5,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'For now this is only saved locally. Later, the adaptive engine can use this to adjust your schedule.',
                      style: TextStyle(
                        fontSize: 13,
                        color: AppColors.textSecondary,
                        height: 1.4,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),

              // Actions
              Row(
                children: [
                  Expanded(
                    child: SizedBox(
                      height: 48,
                      child: OutlinedButton(
                        onPressed: () => Navigator.pop(ctx),
                        style: OutlinedButton.styleFrom(
                          side: const BorderSide(color: AppColors.border),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(24.0),
                          ),
                          foregroundColor: AppColors.textPrimary,
                        ),
                        child: const Text(
                          'Cancel',
                          style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: SizedBox(
                      height: 48,
                      child: ElevatedButton(
                        onPressed: () {
                          setState(() {
                            _localDayStates[dayId] = DayStatus.notToday;
                          });
                          Navigator.pop(ctx);
                        },
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.ctaDark,
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(24.0),
                          ),
                          elevation: 0,
                        ),
                        child: const Text(
                          'Save',
                          style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showUndoNotTodayDialog(String dayId) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Undo Not Today?'),
        content: const Text('This will mark the run as planned again.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text(
              'Cancel',
              style: TextStyle(color: AppColors.textSecondary),
            ),
          ),
          TextButton(
            onPressed: () {
              setState(() {
                _localDayStates[dayId] = DayStatus.planned;
              });
              Navigator.pop(ctx);
            },
            child: const Text(
              'Undo',
              style: TextStyle(color: AppColors.missed, fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }

  // ── Build ────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final homeState = ref.watch(homeDataProvider);
    // Read profile name with Sandra fallback
    final profileAsync = ref.watch(profileOverviewProvider);
    final userName = profileAsync.valueOrNull?.name ?? 'Sandra';

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Stack(
          children: [
            RefreshIndicator(
              onRefresh: () => ref.refresh(homeDataProvider.future),
              color: AppColors.primary,
          child: homeState.when(
            loading: () => const Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            ),
            error: (err, _) => _ErrorState(onRetry: () => ref.invalidate(homeDataProvider), message: err.toString()),
            data: (homeData) {
              final activePlan = homeData.activePlan;

              // ── No active plan ────────────────────────────────────────────
              if (activePlan == null) {
                return _NoActivePlanState(
                  userName: userName,
                  onCreatePlan: () => context.go(AppRoutes.goalSelection),
                );
              }

              // ── Plan completed detection ──────────────────────────────────
              final planDetailsAsync = ref.watch(activePlanDetailsProvider);
              final planDetails = planDetailsAsync.valueOrNull;
              final isPlanCompleted = planDetails != null &&
                  planDetails.totalWeeks > 0 &&
                  planDetails.completedWeeksCount >= planDetails.totalWeeks;

              if (isPlanCompleted) {
                return _PlanCompletedState(
                  userName: userName,
                  goalDistance: activePlan.goalDistance,
                  totalDistance: planDetails.totalCompletedDistance,
                  onStartNew: () => context.go(AppRoutes.goalSelection),
                );
              }

              final todayWorkout = homeData.todayWorkout;
              final dailyTip = homeData.dailyTip;
              final weekSummary = homeData.weekSummary;

              // Determine current selected workout
              final now = DateTime.now();
              final effectiveSelectedDate = _selectedDate ?? now;
              final selectedWorkout = weekSummary.firstWhere(
                (d) => d.date.day == effectiveSelectedDate.day &&
                       d.date.month == effectiveSelectedDate.month &&
                       d.date.year == effectiveSelectedDate.year,
                orElse: () => todayWorkout ?? weekSummary[0],
              );

              final weeklyCompleted = weekSummary.fold(
                  0.0, (sum, d) => sum + (d.actualDistanceKm ?? 0.0));
              final weeklyPlanned =
                  weekSummary.fold(0.0, (sum, d) => sum + d.plannedDistanceKm);

              // Determine current week number (rough from plan text or default 1)
              final weekNum = _extractWeekNumber(activePlan.progressText);

              return SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.symmetric(horizontal: 20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const SizedBox(height: 16),

                    // ── Header ──────────────────────────────────────────────
                    _HomeHeader(userName: userName),
                    const SizedBox(height: 20),



                    // ── Pending confirmations banner ─────────────────────────
                    if (homeData.hasPendingConfirmations)
                      _PendingBanner(
                        onResolve: () =>
                            context.go(AppRoutes.pendingConfirmation),
                      ),

                    // ── Workout card (state-driven) ──────────────────────────
                    if (selectedWorkout.dayType == 'rest')
                      _RestDayCard(tip: dailyTip?.message)
                    else
                      Builder(builder: (context) {
                        final dayStatus = _localDayStates[selectedWorkout.dayId] ??
                            (selectedWorkout.status == 'completed'
                                ? DayStatus.completed
                                : (selectedWorkout.status == 'missed' ||
                                        selectedWorkout.status == 'not_today' ||
                                        selectedWorkout.status == 'skipped')
                                    ? DayStatus.notToday
                                    : DayStatus.planned);
                        return _PlannedCard(
                          workout: selectedWorkout,
                          dayStatus: dayStatus,
                          onTap: () => context.push(
                              '/training-day/${selectedWorkout.dayId}'),
                          onComplete: () => _showCompletionSheet(selectedWorkout.dayId),
                          onNotToday: () => _showNotTodayReasonSheet(selectedWorkout.dayId),
                          onUndoComplete: () {
                            setState(() {
                              _localDayStates[selectedWorkout.dayId] = DayStatus.planned;
                              _showCompletionBanner = false;
                            });
                          },
                          onUndoNotToday: () => _showUndoNotTodayDialog(selectedWorkout.dayId),
                        );
                      }),

                    const SizedBox(height: 24),

                    // ── Week mini calendar ──────────────────────────────────
                    _WeekCalendar(
                      days: weekSummary,
                      weekLabel: 'WEEK $weekNum',
                      selectedDate: effectiveSelectedDate,
                      localDayStates: _localDayStates,
                      onSelectDate: (date) {
                        setState(() {
                          _selectedDate = date;
                        });
                      },
                    ),
                    const SizedBox(height: 24),

                    // ── Insight cards ───────────────────────────────────────
                    Row(
                      children: [
                        Expanded(
                          child: _WeeklyCard(
                            completed: weeklyCompleted,
                            planned: weeklyPlanned,
                          ),
                        ),
                        if (dailyTip != null) ...[
                          const SizedBox(width: 12),
                          Expanded(child: _DailyTipCard(tip: dailyTip)),
                        ],
                      ],
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              );
            },
          ),
        ),
        if (_showCompletionBanner)
          _FloatingNotificationBanner(
            onClose: () => setState(() => _showCompletionBanner = false),
          ),
      ],
    ),
  ),
);
}

  int _extractWeekNumber(String progressText) {
    final match = RegExp(r'Week (\d+)').firstMatch(progressText);
    return match != null ? int.parse(match.group(1)!) : 1;
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Header
// ─────────────────────────────────────────────────────────────────────────────

class _HomeHeader extends StatelessWidget {
  const _HomeHeader({required this.userName});
  final String userName;

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final dateLabel = DateFormat('EEEE, d MMMM').format(now);

    return Row(
      children: [
        // Avatar
        CircleAvatar(
          radius: 22,
          backgroundColor: AppColors.primaryLight,
          child: Text(
            userName.isNotEmpty ? userName[0].toUpperCase() : 'S',
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
            Text(
              'Hello, $userName',
              style: const TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              dateLabel,
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w500,
                color: AppColors.textSecondary,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Pending confirmations banner
// ─────────────────────────────────────────────────────────────────────────────

class _PendingBanner extends StatelessWidget {
  const _PendingBanner({required this.onResolve});
  final VoidCallback onResolve;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.primaryLight,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        children: [
          const Icon(Icons.notification_important_rounded,
              color: AppColors.primary, size: 20),
          const SizedBox(width: 10),
          const Expanded(
            child: Text(
              'You have past runs waiting to be confirmed.',
              style: TextStyle(
                  fontSize: 13,
                  color: AppColors.primary,
                  fontWeight: FontWeight.w500),
            ),
          ),
          GestureDetector(
            onTap: onResolve,
            child: const Text('Resolve',
                style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                    color: AppColors.primary)),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Planned card — Easy / Long / Interval / Tempo
// ─────────────────────────────────────────────────────────────────────────────

class _PlannedCard extends StatelessWidget {
  const _PlannedCard({
    required this.workout,
    required this.onTap,
    required this.onComplete,
    required this.onNotToday,
    required this.dayStatus,
    this.onUndoComplete,
    this.onUndoNotToday,
  });
  final TrainingDayResponse workout;
  final VoidCallback onTap;
  final VoidCallback onComplete;
  final VoidCallback onNotToday;
  final DayStatus dayStatus;
  final VoidCallback? onUndoComplete;
  final VoidCallback? onUndoNotToday;

  Color _accentColor(String type) {
    return switch (type) {
      'easy' || 'easy_run' => const Color(0xFF1D4ED8), // Deep blue
      'interval'           => const Color(0xFFDC2626), // Deep red/pink
      'long_run'           => const Color(0xFF6D28D9), // Deep purple
      'tempo'              => const Color(0xFFC2410C), // Deep orange/brown
      _                    => AppColors.primary,
    };
  }

  String _paceLabel() {
    if (workout.plannedPaceMinKm == null) return '';
    final pace = workout.plannedPaceMinKm!;
    final low = pace - 0.25;
    final high = pace + 0.25;
    String fmt(double v) {
      final min = v.floor();
      final sec = ((v - min) * 60).round();
      return '$min:${sec.toString().padLeft(2, '0')}';
    }
    return 'Pace: ${fmt(low)}–${fmt(high)}';
  }

  @override
  Widget build(BuildContext context) {
    final accentColor = _accentColor(workout.dayType);
    final bg = switch (workout.dayType) {
      'easy' || 'easy_run' => AppColors.easyRunTint,
      'interval'           => AppColors.intervalTint,
      'long_run'           => AppColors.longRunTint,
      'tempo'              => const Color(0xFFFFEDD5),
      _                    => AppColors.easyRunTint,
    };

    final isCompleted = dayStatus == DayStatus.completed;

    // Check if editable (today or future)
    final now = DateTime.now();
    final todayStart = DateTime(now.year, now.month, now.day);
    final isEditable = !workout.date.isBefore(todayStart);

    // Determine button states
    final bool showCompletedButton;
    final bool showNotTodayButton;
    final VoidCallback? completedTap;
    final VoidCallback? notTodayTap;
    final String completedLabel;
    final String notTodayLabel;
    final bool completedActive;
    final bool notTodayActive;

    if (dayStatus == DayStatus.completed) {
      showCompletedButton = true;
      showNotTodayButton = true;
      completedTap = onUndoComplete;
      notTodayTap = null; // Disabled when completed
      completedLabel = '✓ Completed';
      notTodayLabel = 'Not Today';
      completedActive = true;
      notTodayActive = false;
    } else if (dayStatus == DayStatus.notToday) {
      if (isEditable) {
        showCompletedButton = true; // Disabled
        showNotTodayButton = true;
        completedTap = null; // Disabled
        notTodayTap = onUndoNotToday;
        completedLabel = 'Completed';
        notTodayLabel = '✓ Not Today';
        completedActive = false;
        notTodayActive = true;
      } else {
        // Non-editable past skipped day: hide Completed button, show disabled selected Not Today
        showCompletedButton = false;
        showNotTodayButton = true;
        completedTap = null;
        notTodayTap = null;
        completedLabel = 'Completed';
        notTodayLabel = 'Not Today';
        completedActive = false;
        notTodayActive = true;
      }
    } else {
      // DayStatus.planned
      showCompletedButton = true;
      showNotTodayButton = true;
      completedTap = onComplete;
      notTodayTap = onNotToday;
      completedLabel = 'Completed';
      notTodayLabel = 'Not Today';
      completedActive = false;
      notTodayActive = false;
    }

    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: double.infinity,
        height: 260,
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(24),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.04),
              blurRadius: 10,
              offset: const Offset(0, 4),
            )
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Top Row: label + ellipsis menu or check icon
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  "TODAY'S PLAN",
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                    color: accentColor,
                    letterSpacing: 0.8,
                  ),
                ),
                isCompleted
                    ? const Icon(
                        Icons.check_circle_rounded,
                        color: AppColors.completed,
                        size: 24,
                      )
                    : Icon(
                        Icons.more_horiz_rounded,
                        color: accentColor.withOpacity(0.6),
                        size: 20,
                      ),
              ],
            ),
            const SizedBox(height: 16),

            // Distance & Title & Pace
            Row(
              crossAxisAlignment: CrossAxisAlignment.baseline,
              textBaseline: TextBaseline.alphabetic,
              children: [
                Text(
                  '${workout.plannedDistanceKm.toStringAsFixed(workout.plannedDistanceKm == workout.plannedDistanceKm.roundToDouble() ? 0 : 1)}',
                  style: AppTextStyles.displayLarge.copyWith(
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(width: 4),
                const Text(
                  'km',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textPrimary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              workout.title,
              style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
            if (workout.plannedPaceMinKm != null) ...[
              const SizedBox(height: 8),
              Row(
                children: [
                  Container(
                    width: 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: accentColor,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 6),
                  Text(
                    _paceLabel(),
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w500,
                      color: accentColor,
                    ),
                  ),
                ],
              ),
            ],

            const Spacer(),

            // Action Buttons
            Row(
              children: [
                if (showCompletedButton)
                  Expanded(
                    child: _ActionButton(
                      label: completedLabel,
                      icon: completedActive ? null : Icons.check_rounded,
                      onTap: completedTap,
                      backgroundColor: completedActive ? AppColors.completed : Colors.white,
                      borderColor: AppColors.completed,
                      textColor: completedActive ? Colors.white : AppColors.completed,
                      iconColor: completedActive ? Colors.white : AppColors.completed,
                    ),
                  )
                else
                  const Spacer(),
                const SizedBox(width: 12),
                if (showNotTodayButton)
                  Expanded(
                    child: _ActionButton(
                      label: notTodayLabel,
                      icon: notTodayActive ? null : Icons.close_rounded,
                      onTap: notTodayTap,
                      backgroundColor: notTodayActive ? AppColors.ctaDark : Colors.white,
                      borderColor: notTodayActive ? AppColors.ctaDark : AppColors.border,
                      textColor: notTodayActive
                          ? Colors.white
                          : (dayStatus == DayStatus.completed ? AppColors.textSecondary : AppColors.textPrimary),
                      iconColor: notTodayActive
                          ? Colors.white
                          : (dayStatus == DayStatus.completed ? AppColors.textSecondary : AppColors.textPrimary),
                    ),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Completed card
// ─────────────────────────────────────────────────────────────────────────────

class _CompletedCard extends StatelessWidget {
  const _CompletedCard({required this.workout, required this.onTap});
  final TrainingDayResponse workout;
  final VoidCallback onTap;

  String _fmtAvgPace(TrainingDayResponse w) {
    final dist = w.actualDistanceKm ?? w.plannedDistanceKm;
    final dur = w.actualDurationMin ?? w.plannedDurationMin;
    if (dist <= 0) return '-:--';
    final paceDecimal = dur / dist;
    final min = paceDecimal.floor();
    final sec = ((paceDecimal - min) * 60).round();
    return '$min:${sec.toString().padLeft(2, '0')} /km';
  }

  Widget _buildCompletedMetric(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 10,
            fontWeight: FontWeight.w600,
            color: Colors.white70,
            letterSpacing: 0.5,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          value,
          style: const TextStyle(
            fontSize: 15,
            fontWeight: FontWeight.w800,
            color: Colors.white,
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final actualDistance = workout.actualDistanceKm ?? workout.plannedDistanceKm;
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: AppColors.completedCard,
          borderRadius: BorderRadius.circular(24),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${actualDistance.toStringAsFixed(actualDistance == actualDistance.roundToDouble() ? 0 : 1)} km',
                        style: AppTextStyles.displayLarge
                            .copyWith(color: Colors.white),
                      ),
                      Text(workout.title,
                          style: const TextStyle(
                              fontSize: 22,
                              fontWeight: FontWeight.w700,
                              color: Colors.white)),
                    ],
                  ),
                ),
                Container(
                  width: 52,
                  height: 52,
                  decoration: BoxDecoration(
                    color: AppColors.completed.withOpacity(0.2),
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: const Icon(Icons.check_circle_rounded,
                      color: AppColors.completed, size: 30),
                ),
              ],
            ),
            const SizedBox(height: 18),
            // Horizontal progress section
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                _buildCompletedMetric('DISTANCE', '${actualDistance.toStringAsFixed(1)} km'),
                _buildCompletedMetric('DURATION', '${workout.actualDurationMin ?? workout.plannedDurationMin} min'),
                _buildCompletedMetric('PACE', _fmtAvgPace(workout)),
              ],
            ),
            const SizedBox(height: 16),
            ClipRRect(
              borderRadius: BorderRadius.circular(100),
              child: const LinearProgressIndicator(
                value: 1.0,
                minHeight: 6,
                backgroundColor: Colors.white24,
                color: AppColors.completed,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Missed card
// ─────────────────────────────────────────────────────────────────────────────

class _MissedCard extends StatelessWidget {
  const _MissedCard({required this.workout, required this.onTap});
  final TrainingDayResponse workout;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: AppColors.missedCard,
          borderRadius: BorderRadius.circular(24),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.missedLight,
                  borderRadius: BorderRadius.circular(100),
                ),
                child: const Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.remove_circle_outline_rounded,
                        color: AppColors.missed, size: 14),
                    SizedBox(width: 4),
                    Text('Plan skipped',
                        style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.missed)),
                  ],
                ),
              ),
            ]),
            const SizedBox(height: 12),
            Text(workout.title,
                style: const TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary)),
            const SizedBox(height: 6),
            const Text(
              'No worries — feel and recover today.',
              style: TextStyle(
                  fontSize: 14,
                  color: AppColors.textSecondary,
                  height: 1.4),
            ),
            const SizedBox(height: 14),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppColors.border),
              ),
              child: const Row(
                children: [
                  Icon(Icons.directions_run_rounded,
                      color: AppColors.primary, size: 18),
                  SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'See you on your next run! Every step forward counts. 🏃',
                      style: TextStyle(
                          fontSize: 13,
                          color: AppColors.textSecondary,
                          height: 1.4),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Rest day card
// ─────────────────────────────────────────────────────────────────────────────

class _RestDayCard extends StatelessWidget {
  const _RestDayCard({this.tip});
  final String? tip;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      height: 260,
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: AppColors.restTint,
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.04),
            blurRadius: 10,
            offset: const Offset(0, 4),
          )
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: const [
                    Text('Rest Day',
                        style: TextStyle(
                            fontSize: 32,
                            fontWeight: FontWeight.w800,
                            color: AppColors.textPrimary)),
                    SizedBox(height: 6),
                    Text(
                      'Your body needs rest\nto come back stronger.',
                      style: TextStyle(
                          fontSize: 14,
                          color: AppColors.textSecondary,
                          height: 1.4),
                    ),
                  ],
                ),
              ),
              const Text('🏖️', style: TextStyle(fontSize: 48)),
            ],
          ),
          const Spacer(),
          if (tip != null) ...[
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                color: Colors.white.withOpacity(0.7),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Row(
                children: [
                  const Text('💤', style: TextStyle(fontSize: 16)),
                  const SizedBox(width: 8),
                  const Text('REST DAY TIP',
                      style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                          color: Color(0xFFB45309),
                          letterSpacing: 0.5)),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(tip!,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                            fontSize: 13, color: AppColors.textSecondary)),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Week mini calendar strip — Reworked to vertical date chips (pills)
// ─────────────────────────────────────────────────────────────────────────────

class _WeekCalendar extends StatelessWidget {
  const _WeekCalendar({
    required this.days,
    required this.weekLabel,
    required this.selectedDate,
    required this.localDayStates,
    required this.onSelectDate,
  });
  final List<TrainingDayResponse> days;
  final String weekLabel;
  final DateTime selectedDate;
  final Map<String, DayStatus> localDayStates;
  final ValueChanged<DateTime> onSelectDate;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          weekLabel,
          style: const TextStyle(
            fontSize: 11,
            fontWeight: FontWeight.w600,
            color: AppColors.textSecondary,
            letterSpacing: 0.8,
          ),
        ),
        const SizedBox(height: 12),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: days.map((day) {
            final isSelected = day.date.day == selectedDate.day &&
                day.date.month == selectedDate.month &&
                day.date.year == selectedDate.year;

            return Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 4),
                child: _WeekDayChip(
                  day: day,
                  isSelected: isSelected,
                  localDayStates: localDayStates,
                  onTap: () => onSelectDate(day.date),
                ),
              ),
            );
          }).toList(),
        ),
      ],
    );
  }
}

class _WeekDayChip extends StatelessWidget {
  const _WeekDayChip({
    required this.day,
    required this.isSelected,
    required this.localDayStates,
    required this.onTap,
  });
  final TrainingDayResponse day;
  final bool isSelected;
  final Map<String, DayStatus> localDayStates;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final weekdays = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];
    final weekdayLabel = weekdays[(day.date.weekday - 1) % 7];

    final bg = isSelected ? AppColors.ctaDark : Colors.white;
    final weekdayColor = isSelected ? Colors.white70 : AppColors.textSecondary;
    final dateColor = isSelected ? Colors.white : AppColors.textPrimary;
    final border = isSelected ? null : Border.all(color: AppColors.border, width: 1);

    final status = localDayStates[day.dayId] ??
        (day.status == 'completed'
            ? DayStatus.completed
            : (day.status == 'missed' ||
                    day.status == 'not_today' ||
                    day.status == 'skipped')
                ? DayStatus.notToday
                : DayStatus.planned);
    final isCompleted = status == DayStatus.completed;

    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 12),
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(20),
          border: border,
          boxShadow: isSelected
              ? [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.08),
                    blurRadius: 6,
                    offset: const Offset(0, 2),
                  )
                ]
              : null,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              weekdayLabel,
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w600,
                color: weekdayColor,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              day.date.day.toString(),
              style: TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.w700,
                color: dateColor,
              ),
            ),
            const SizedBox(height: 6),
            if (status == DayStatus.completed)
              Container(
                width: 14,
                height: 14,
                decoration: const BoxDecoration(
                  color: AppColors.completed,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.check_rounded,
                  size: 9,
                  color: Colors.white,
                ),
              )
            else if (status == DayStatus.notToday)
              Container(
                width: 14,
                height: 14,
                decoration: const BoxDecoration(
                  color: AppColors.ctaDark,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.close_rounded,
                  size: 9,
                  color: Colors.white,
                ),
              )
            else
              const SizedBox(width: 14, height: 14),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Weekly insight card
// ─────────────────────────────────────────────────────────────────────────────

class _WeeklyCard extends StatelessWidget {
  const _WeeklyCard({required this.completed, required this.planned});
  final double completed;
  final double planned;

  @override
  Widget build(BuildContext context) {
    final progress =
        planned > 0 ? (completed / planned).clamp(0.0, 1.0) : 0.0;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.weeklyCardBackground,
        borderRadius: BorderRadius.circular(24),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('THIS WEEK',
              style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textSecondary,
                  letterSpacing: 0.6)),
          const SizedBox(height: 8),
          Text('${completed.toStringAsFixed(1)} / ${planned.toStringAsFixed(1)} km',
              style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary)),
          const SizedBox(height: 12),
          ClipRRect(
            borderRadius: BorderRadius.circular(100),
            child: LinearProgressIndicator(
              value: progress,
              minHeight: 6,
              backgroundColor: AppColors.textSecondary.withOpacity(0.15),
              color: AppColors.ctaDark,
            ),
          ),
          const SizedBox(height: 8),
          Text('${(progress * 100).toStringAsFixed(0)}% of weekly goal',
              style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w500,
                  color: AppColors.textSecondary)),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Daily tip card
// ─────────────────────────────────────────────────────────────────────────────

class _DailyTipCard extends StatelessWidget {
  const _DailyTipCard({required this.tip});
  final DailyTipResponse tip;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.tipCardBackground,
        borderRadius: BorderRadius.circular(24),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('DAILY TIP',
              style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textSecondary,
                  letterSpacing: 0.6)),
          const SizedBox(height: 8),
          Text(tip.message,
              maxLines: 4,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                  fontSize: 13,
                  color: AppColors.textPrimary,
                  height: 1.4,
                  fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Action button (Completed / Not Today)
// ─────────────────────────────────────────────────────────────────────────────

class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.label,
    this.icon,
    required this.onTap,
    this.backgroundColor,
    this.borderColor,
    this.textColor,
    this.iconColor,
  });
  final String label;
  final IconData? icon;
  final VoidCallback? onTap;
  final Color? backgroundColor;
  final Color? borderColor;
  final Color? textColor;
  final Color? iconColor;

  @override
  Widget build(BuildContext context) {
    final isDisabled = onTap == null;
    return GestureDetector(
      onTap: onTap,
      child: Opacity(
        opacity: isDisabled ? 0.4 : 1.0,
        child: Container(
          height: 44,
          decoration: BoxDecoration(
            color: backgroundColor ?? Colors.white,
            borderRadius: BorderRadius.circular(100),
            border: Border.all(
              color: borderColor ?? AppColors.border,
              width: 1,
            ),
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (icon != null) ...[
                Icon(icon,
                    size: 16,
                    color: iconColor ?? AppColors.textPrimary),
                const SizedBox(width: 6),
              ],
              Text(label,
                  style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: textColor ?? AppColors.textPrimary)),
            ],
          ),
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Completion option row for Run Complete popup
// ─────────────────────────────────────────────────────────────────────────────

class _CompletionOptionRow extends StatelessWidget {
  const _CompletionOptionRow({
    required this.title,
    required this.subtitle,
    required this.value,
    required this.groupValue,
    required this.onTap,
  });
  final String title;
  final String subtitle;
  final String value;
  final String groupValue;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final isSelected = value == groupValue;

    return GestureDetector(
      onTap: onTap,
      behavior: HitTestBehavior.opaque,
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isSelected ? AppColors.ctaDark : AppColors.border,
            width: isSelected ? 1.5 : 1,
          ),
        ),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    subtitle,
                    style: const TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
            IgnorePointer(
              child: Radio<String>(
                value: value,
                groupValue: groupValue,
                activeColor: AppColors.ctaDark,
                onChanged: (_) {},
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Recovery tip card for Run Complete popup
// ─────────────────────────────────────────────────────────────────────────────

class _RecoveryTipCard extends StatelessWidget {
  const _RecoveryTipCard({
    required this.title,
    required this.text,
  });
  final String title;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.tipCardBackground,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w700,
              color: Color(0xFF8B5CF6),
              letterSpacing: 0.6,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            text,
            style: const TextStyle(
              fontSize: 13,
              color: AppColors.textPrimary,
              height: 1.4,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Completion success notification banner
// ─────────────────────────────────────────────────────────────────────────────

class _FloatingNotificationBanner extends StatefulWidget {
  const _FloatingNotificationBanner({
    super.key,
    required this.onClose,
  });
  final VoidCallback onClose;

  @override
  State<_FloatingNotificationBanner> createState() => _FloatingNotificationBannerState();
}

class _FloatingNotificationBannerState extends State<_FloatingNotificationBanner> {
  bool _visible = false;
  double _offsetY = -20.0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        setState(() {
          _visible = true;
          _offsetY = 16.0;
        });
      }
    });

    Future.delayed(const Duration(seconds: 4), () {
      if (mounted) {
        _dismiss();
      }
    });
  }

  void _dismiss() {
    setState(() {
      _visible = false;
      _offsetY = -20.0;
    });
    Future.delayed(const Duration(milliseconds: 300), () {
      if (mounted) {
        widget.onClose();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedPositioned(
      duration: const Duration(milliseconds: 300),
      curve: Curves.easeOut,
      top: _offsetY,
      left: 20,
      right: 20,
      child: AnimatedOpacity(
        duration: const Duration(milliseconds: 300),
        opacity: _visible ? 1.0 : 0.0,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          decoration: BoxDecoration(
            color: const Color(0xFFE8F8EE),
            borderRadius: BorderRadius.circular(16),
            border: Border.all(
              color: const Color(0xFFBFEFD3),
              width: 1,
            ),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.08),
                blurRadius: 10,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Row(
            children: [
              Container(
                width: 28,
                height: 28,
                decoration: const BoxDecoration(
                  color: Colors.white,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.check_circle_rounded,
                  color: AppColors.completed,
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      'Run recorded!',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    SizedBox(height: 2),
                    Text(
                      'Nice work! See you on your next run. 🏃‍♀️',
                      style: TextStyle(
                        fontSize: 13,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              const Text(
                '🎉',
                style: TextStyle(fontSize: 20),
              ),
              const SizedBox(width: 12),
              GestureDetector(
                onTap: _dismiss,
                child: const Icon(
                  Icons.close_rounded,
                  color: AppColors.textMuted,
                  size: 18,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// No active plan state
// ─────────────────────────────────────────────────────────────────────────────

class _NoActivePlanState extends StatelessWidget {
  const _NoActivePlanState({required this.userName, required this.onCreatePlan});
  final String userName;
  final VoidCallback onCreatePlan;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.symmetric(horizontal: 20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SizedBox(height: 16),
          _HomeHeader(userName: userName),
          const SizedBox(height: 40),
          Center(
            child: Column(
              children: [
                const Text('🏃', style: TextStyle(fontSize: 72)),
                const SizedBox(height: 20),
                const Text('Ready to start running?',
                    style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary),
                    textAlign: TextAlign.center),
                const SizedBox(height: 10),
                const Text(
                  'Create your adaptive plan and start tracking your workouts.',
                  style: TextStyle(
                      fontSize: 15,
                      color: AppColors.textSecondary,
                      height: 1.5),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 36),
                AppPrimaryButton(
                  label: 'Create a Plan',
                  onPressed: onCreatePlan,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Plan completed state
// ─────────────────────────────────────────────────────────────────────────────

class _PlanCompletedState extends StatelessWidget {
  const _PlanCompletedState({
    required this.userName,
    required this.goalDistance,
    required this.totalDistance,
    required this.onStartNew,
  });
  final String userName;
  final String goalDistance;
  final double totalDistance;
  final VoidCallback onStartNew;

  String _fmtGoal(String val) => switch (val) {
        'five_k'        => '5K',
        'ten_k'         => '10K',
        'half_marathon' => 'Half Marathon',
        'marathon'      => 'Marathon',
        _               => val,
      };

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.symmetric(horizontal: 20),
      child: Column(
        children: [
          const SizedBox(height: 16),
          _HomeHeader(userName: userName),
          const SizedBox(height: 32),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(28),
            decoration: BoxDecoration(
              color: AppColors.completedCard,
              borderRadius: BorderRadius.circular(28),
            ),
            child: Column(
              children: [
                const Text('🏆', style: TextStyle(fontSize: 64)),
                const SizedBox(height: 16),
                const Text('Plan Complete!',
                    style: TextStyle(
                        fontSize: 26,
                        fontWeight: FontWeight.w800,
                        color: Colors.white)),
                const SizedBox(height: 6),
                Text('You finished your ${_fmtGoal(goalDistance)} plan.',
                    style: const TextStyle(
                        fontSize: 15, color: Colors.white70),
                    textAlign: TextAlign.center),
                const SizedBox(height: 16),
                Text('${totalDistance.toStringAsFixed(1)} km',
                    style: const TextStyle(
                        fontSize: 48,
                        fontWeight: FontWeight.w800,
                        color: AppColors.completed)),
                const Text('total distance logged',
                    style:
                        TextStyle(fontSize: 13, color: Colors.white60)),
              ],
            ),
          ),
          const SizedBox(height: 24),
          AppPrimaryButton(
            label: 'Start New Plan',
            onPressed: onStartNew,
          ),
          const SizedBox(height: 12),
          AppSecondaryButton(
            label: 'View Plan Summary',
            onPressed: () {
              ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
                  content:
                      Text('Plan summary view coming in a future update.')));
            },
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Error state
// ─────────────────────────────────────────────────────────────────────────────

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.onRetry, required this.message});
  final VoidCallback onRetry;
  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.cloud_off_rounded,
                size: 64, color: AppColors.textMuted),
            const SizedBox(height: 16),
            const Text('Could not load plan data',
                style: AppTextStyles.h2, textAlign: TextAlign.center),
            const SizedBox(height: 8),
            Text(message,
                style: const TextStyle(
                    fontSize: 13, color: AppColors.textSecondary),
                textAlign: TextAlign.center),
            const SizedBox(height: 24),
            AppPrimaryButton(
              label: 'Retry',
              onPressed: onRetry,
            ),
          ],
        ),
      ),
    );
  }
}
