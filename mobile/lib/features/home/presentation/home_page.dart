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

class HomePage extends ConsumerStatefulWidget {
  const HomePage({super.key});

  @override
  ConsumerState<HomePage> createState() => _HomePageState();
}

class _HomePageState extends ConsumerState<HomePage> {
  bool _isSubmitting = false;

  // ── Completion sheet ────────────────────────────────────────────────────

  void _showCompletionSheet(
      String dayId, double plannedDistance, int plannedDuration) {
    String selectedResult = 'as_planned';
    final distanceController =
        TextEditingController(text: plannedDistance.toStringAsFixed(1));
    final durationController =
        TextEditingController(text: plannedDuration.toString());

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
              // Handle
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
              const Text('Log Workout',
                  style: TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary)),
              const SizedBox(height: 4),
              const Text('How did it go?',
                  style: TextStyle(fontSize: 14, color: AppColors.textSecondary)),
              const SizedBox(height: 20),

              // Result chips
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

              // Distance / duration fields
              Row(children: [
                Expanded(
                  child: _CompactTextField(
                    controller: distanceController,
                    label: 'Distance (km)',
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true),
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
              ]),
              const SizedBox(height: 24),

              // CTA
              if (_isSubmitting)
                const Center(
                    child: CircularProgressIndicator(color: AppColors.primary))
              else
                SizedBox(
                  width: double.infinity,
                  height: 52,
                  child: ElevatedButton(
                    onPressed: () async {
                      final dist =
                          double.tryParse(distanceController.text.trim()) ??
                              plannedDistance;
                      final dur =
                          int.tryParse(durationController.text.trim()) ??
                              plannedDuration;
                      setModal(() => _isSubmitting = true);
                      setState(() => _isSubmitting = true);
                      try {
                        final repo = ref.read(homeRepositoryProvider);
                        await repo.completeWorkout(
                            dayId, dist, dur, 'Completed!');
                        ref.invalidate(homeDataProvider);
                        ref.invalidate(calendarDataProvider);
                        ref.invalidate(profileOverviewProvider);
                        ref.invalidate(activePlanDetailsProvider);
                        if (context.mounted) Navigator.pop(context);
                      } catch (e) {
                        if (context.mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(content: Text(e.toString())));
                        }
                      } finally {
                        setModal(() => _isSubmitting = false);
                        setState(() => _isSubmitting = false);
                      }
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.ctaDark,
                      foregroundColor: Colors.white,
                      shape: const StadiumBorder(),
                      elevation: 0,
                    ),
                    child: const Text('Log it',
                        style: TextStyle(
                            fontSize: 16, fontWeight: FontWeight.w600)),
                  ),
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

  // ── Not Today sheet ─────────────────────────────────────────────────────

  void _showNotTodaySheet(String dayId) {
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
              // Handle
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

              // Header row
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Skip today\'s workout?',
                      style: TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary)),
                  GestureDetector(
                    onTap: () => Navigator.pop(ctx),
                    child: const Icon(Icons.close_rounded,
                        color: AppColors.textMuted, size: 22),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              const Text(
                "Rest days are part of progress.\nWe'll adjust your plan to keep you on track.",
                style: TextStyle(fontSize: 14, color: AppColors.textSecondary, height: 1.4),
              ),
              const SizedBox(height: 20),

              // Reason chips
              const Text('Reason (Optional)',
                  style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textSecondary,
                      letterSpacing: 0.4)),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: reasons.map((r) {
                  final isSelected = selectedReason == r.$1;
                  return GestureDetector(
                    onTap: () => setModal(() => selectedReason =
                        isSelected ? null : r.$1),
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 150),
                      padding: const EdgeInsets.symmetric(
                          horizontal: 14, vertical: 8),
                      decoration: BoxDecoration(
                        color: isSelected
                            ? AppColors.ctaDark
                            : const Color(0xFFF5F6FA),
                        borderRadius: BorderRadius.circular(100),
                        border: Border.all(
                          color: isSelected
                              ? AppColors.ctaDark
                              : AppColors.border,
                        ),
                      ),
                      child: Text(
                        r.$2,
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w500,
                          color: isSelected
                              ? Colors.white
                              : AppColors.textPrimary,
                        ),
                      ),
                    ),
                  );
                }).toList(),
              ),
              const SizedBox(height: 20),

              // Supportive note
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
                    const Icon(Icons.lightbulb_outline_rounded,
                        color: Color(0xFF8B5CF6), size: 18),
                    const SizedBox(width: 10),
                    const Expanded(
                      child: Text(
                        'Consistency is key — Taking a rest today won\'t affect your progress. Your schedule will update automatically.',
                        style: TextStyle(
                            fontSize: 13,
                            color: AppColors.textSecondary,
                            height: 1.4),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // CTA
              if (_isSubmitting)
                const Center(
                    child: CircularProgressIndicator(color: AppColors.primary))
              else
                SizedBox(
                  width: double.infinity,
                  height: 52,
                  child: ElevatedButton(
                    onPressed: () async {
                      setModal(() => _isSubmitting = true);
                      setState(() => _isSubmitting = true);
                      try {
                        final repo = ref.read(homeRepositoryProvider);
                        final decision = await repo.createNotTodayDecision(
                            dayId, selectedReason ?? 'other');
                        await repo.confirmNotTodayDecision(decision.decisionId);
                        ref.invalidate(homeDataProvider);
                        ref.invalidate(calendarDataProvider);
                        ref.invalidate(profileOverviewProvider);
                        ref.invalidate(activePlanDetailsProvider);
                        if (context.mounted) Navigator.pop(context);
                      } catch (e) {
                        if (context.mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(content: Text(e.toString())));
                        }
                      } finally {
                        setModal(() => _isSubmitting = false);
                        setState(() => _isSubmitting = false);
                      }
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.ctaDark,
                      foregroundColor: Colors.white,
                      shape: const StadiumBorder(),
                      elevation: 0,
                    ),
                    child: const Text('Got it!',
                        style: TextStyle(
                            fontSize: 16, fontWeight: FontWeight.w600)),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  // ── Build ────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final homeState = ref.watch(homeDataProvider);
    // Read profile name with graceful fallback
    final profileAsync = ref.watch(profileOverviewProvider);
    final userName = profileAsync.valueOrNull?.name ?? 'Runner';

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: RefreshIndicator(
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

                    // ── Today's Plan label ───────────────────────────────────
                    const Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text("TODAY'S PLAN",
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textSecondary,
                              letterSpacing: 0.8,
                            )),
                        Icon(Icons.more_horiz_rounded,
                            color: AppColors.textMuted),
                      ],
                    ),
                    const SizedBox(height: 10),

                    // ── Workout card (state-driven) ──────────────────────────
                    if (todayWorkout == null ||
                        todayWorkout.dayType == 'rest')
                      _RestDayCard(tip: dailyTip?.message)
                    else if (todayWorkout.status == 'completed')
                      _CompletedCard(
                        workout: todayWorkout,
                        onTap: () => context.push(
                            '/training-day/${todayWorkout.dayId}'),
                      )
                    else if (todayWorkout.status == 'missed')
                      _MissedCard(
                        workout: todayWorkout,
                        onTap: () => context.push(
                            '/training-day/${todayWorkout.dayId}'),
                      )
                    else
                      _PlannedCard(
                        workout: todayWorkout,
                        onTap: () => context.push(
                            '/training-day/${todayWorkout.dayId}'),
                        onComplete: () => _showCompletionSheet(
                          todayWorkout.dayId,
                          todayWorkout.plannedDistanceKm,
                          todayWorkout.plannedDurationMin,
                        ),
                        onNotToday: () =>
                            _showNotTodaySheet(todayWorkout.dayId),
                      ),

                    const SizedBox(height: 24),

                    // ── Week mini calendar ──────────────────────────────────
                    _WeekCalendar(
                        days: weekSummary, weekLabel: 'WEEK $weekNum'),
                    const SizedBox(height: 20),

                    // ── Insight cards ───────────────────────────────────────
                    Row(children: [
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
                    ]),
                    const SizedBox(height: 24),
                  ],
                ),
              );
            },
          ),
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
    final dateLabel =
        DateFormat('EEE, d MMM').format(now).toUpperCase();

    return Row(
      children: [
        // Avatar
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
          onPressed: null, // placeholder
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
// Planned card — Easy / Long / Interval differentiated by dayType
// ─────────────────────────────────────────────────────────────────────────────

class _PlannedCard extends StatelessWidget {
  const _PlannedCard({
    required this.workout,
    required this.onTap,
    required this.onComplete,
    required this.onNotToday,
  });
  final TrainingDayResponse workout;
  final VoidCallback onTap;
  final VoidCallback onComplete;
  final VoidCallback onNotToday;

  (Color, Color, IconData, String) _cardStyle(String type) {
    return switch (type) {
      'long_run'  => (AppColors.longRunCard, const Color(0xFF1A6B3A), Icons.landscape_rounded, '🌲'),
      'interval'  => (AppColors.intervalCard, const Color(0xFF6B21A8), Icons.bar_chart_rounded, '⚡'),
      _           => (AppColors.easyRunCard, AppColors.primary, Icons.directions_run_rounded, '👟'),
    };
  }

  String _paceLabel() {
    if (workout.plannedPaceMinKm == null) return '';
    final pace = workout.plannedPaceMinKm!;
    final low = pace - 0.3;
    final high = pace + 0.3;
    String fmt(double v) {
      final min = v.floor();
      final sec = ((v - min) * 60).round();
      return '$min:${sec.toString().padLeft(2, '0')}';
    }
    return 'Pace: ${fmt(low)}–${fmt(high)}';
  }

  @override
  Widget build(BuildContext context) {
    final (bg, accentColor, _, emoji) = _cardStyle(workout.dayType);

    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(24),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Distance + emoji row
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${workout.plannedDistanceKm.toStringAsFixed(workout.plannedDistanceKm == workout.plannedDistanceKm.roundToDouble() ? 0 : 1)} km',
                        style: AppTextStyles.displayLarge,
                      ),
                      Text(workout.title,
                          style: const TextStyle(
                              fontSize: 22,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary)),
                    ],
                  ),
                ),
                Container(
                  width: 56,
                  height: 56,
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.5),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Center(
                    child: Text(emoji, style: const TextStyle(fontSize: 30)),
                  ),
                ),
              ],
            ),

            // Pace
            if (workout.plannedPaceMinKm != null) ...[
              const SizedBox(height: 10),
              Row(children: [
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
                      color: accentColor),
                ),
              ]),
            ],

            const SizedBox(height: 16),

            // Buttons
            Row(
              children: [
                Expanded(
                  child: _ActionButton(
                    label: 'Completed',
                    icon: Icons.check_rounded,
                    isPositive: true,
                    onTap: onComplete,
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: _ActionButton(
                    label: 'Not Today',
                    icon: Icons.close_rounded,
                    isPositive: false,
                    onTap: onNotToday,
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

  @override
  Widget build(BuildContext context) {
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
                        '${workout.plannedDistanceKm.toStringAsFixed(workout.plannedDistanceKm == workout.plannedDistanceKm.roundToDouble() ? 0 : 1)} km',
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
                    color: AppColors.completed.withValues(alpha: 0.2),
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: const Icon(Icons.check_circle_rounded,
                      color: AppColors.completed, size: 30),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Row(
                children: [
                  const Icon(Icons.emoji_events_rounded,
                      color: Color(0xFFFFD700), size: 18),
                  const SizedBox(width: 8),
                  const Expanded(
                    child: Text(
                      "Nice work! 🎉 Every run brings you closer to your best.",
                      style: TextStyle(
                          fontSize: 13,
                          color: Colors.white70,
                          height: 1.4),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            Container(
              padding:
                  const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
              decoration: BoxDecoration(
                color: AppColors.completed.withValues(alpha: 0.2),
                borderRadius: BorderRadius.circular(100),
              ),
              child: const Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.check_circle_rounded,
                      color: AppColors.completed, size: 16),
                  SizedBox(width: 6),
                  Text('Completed',
                      style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.completed)),
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
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.restCard,
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
              const Text('🏖️', style: TextStyle(fontSize: 52)),
            ],
          ),
          if (tip != null) ...[
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.7),
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
                          color: AppColors.textSecondary,
                          letterSpacing: 0.5)),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(tip!,
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
// Week mini calendar strip
// ─────────────────────────────────────────────────────────────────────────────

class _WeekCalendar extends StatelessWidget {
  const _WeekCalendar({required this.days, required this.weekLabel});
  final List<TrainingDayResponse> days;
  final String weekLabel;

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final weekdays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(weekLabel,
            style: const TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w600,
                color: AppColors.textSecondary,
                letterSpacing: 0.8)),
        const SizedBox(height: 10),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: days.map((day) {
            final isToday = day.date.day == now.day &&
                day.date.month == now.month &&
                day.date.year == now.year;
            final label = weekdays[day.date.weekday % 7]; // 0=Sun … 6=Sat

            Color bg;
            Color textClr;
            Widget? statusDot;

            if (isToday) {
              bg = AppColors.ctaDark;
              textClr = Colors.white;
            } else {
              bg = Colors.white;
              textClr = AppColors.textPrimary;
            }

            // Status dot below the day number
            if (day.status == 'completed') {
              statusDot = Container(
                width: 6,
                height: 6,
                decoration: const BoxDecoration(
                    color: AppColors.completed, shape: BoxShape.circle),
              );
            } else if (day.status == 'missed') {
              statusDot = Container(
                width: 6,
                height: 6,
                decoration: const BoxDecoration(
                    color: AppColors.missed, shape: BoxShape.circle),
              );
            } else if (day.dayType != 'rest' && !isToday) {
              statusDot = Container(
                width: 6,
                height: 6,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(color: AppColors.primary, width: 1.5),
                ),
              );
            }

            return Column(
              children: [
                Text(label,
                    style: const TextStyle(
                        fontSize: 11,
                        color: AppColors.textMuted,
                        fontWeight: FontWeight.w500)),
                const SizedBox(height: 4),
                Container(
                  width: 36,
                  height: 36,
                  decoration: BoxDecoration(
                    color: bg,
                    shape: BoxShape.circle,
                    border: isToday
                        ? null
                        : Border.all(color: AppColors.border, width: 1),
                  ),
                  child: Center(
                    child: Text(day.date.day.toString(),
                        style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: textClr)),
                  ),
                ),
                const SizedBox(height: 4),
                statusDot ?? const SizedBox(width: 6, height: 6),
              ],
            );
          }).toList(),
        ),
      ],
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
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.weeklyCardBackground,
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('THIS WEEK',
              style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textSecondary,
                  letterSpacing: 0.6)),
          const SizedBox(height: 6),
          Text('${completed.toStringAsFixed(1)} / ${planned.toStringAsFixed(1)} km',
              style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary)),
          const SizedBox(height: 8),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: progress,
              minHeight: 6,
              backgroundColor: AppColors.border,
              color: AppColors.ctaDark,
            ),
          ),
          const SizedBox(height: 4),
          Text('${(progress * 100).toStringAsFixed(0)}% of weekly goal',
              style: const TextStyle(
                  fontSize: 11,
                  color: AppColors.textMuted)),
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
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.tipCardBackground,
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('DAILY TIP',
              style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textSecondary,
                  letterSpacing: 0.6)),
          const SizedBox(height: 6),
          Text(tip.message,
              maxLines: 4,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                  fontSize: 13,
                  color: AppColors.textSecondary,
                  height: 1.4)),
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

// ─────────────────────────────────────────────────────────────────────────────
// Compact text field for workout log sheet
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
// Result chip for completion sheet
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
                SizedBox(
                  width: double.infinity,
                  height: 54,
                  child: ElevatedButton(
                    onPressed: onCreatePlan,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.ctaDark,
                      foregroundColor: Colors.white,
                      shape: const StadiumBorder(),
                      elevation: 0,
                    ),
                    child: const Text('Create a Plan',
                        style: TextStyle(
                            fontSize: 16, fontWeight: FontWeight.w600)),
                  ),
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
          SizedBox(
            width: double.infinity,
            height: 54,
            child: ElevatedButton(
              onPressed: onStartNew,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.ctaDark,
                foregroundColor: Colors.white,
                shape: const StadiumBorder(),
                elevation: 0,
              ),
              child: const Text('Start New Plan',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            height: 54,
            child: OutlinedButton(
              onPressed: () {
                ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
                    content:
                        Text('Plan summary view coming in a future update.')));
              },
              style: OutlinedButton.styleFrom(
                shape: const StadiumBorder(),
                side: const BorderSide(color: AppColors.border),
                foregroundColor: AppColors.textPrimary,
              ),
              child: const Text('View Plan Summary',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500)),
            ),
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
            ElevatedButton(
              onPressed: onRetry,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.ctaDark,
                foregroundColor: Colors.white,
                shape: const StadiumBorder(),
              ),
              child: const Text('Retry'),
            ),
          ],
        ),
      ),
    );
  }
}
