import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/routing/app_router.dart';
import '../../../core/widgets/app_bottom_sheet.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../../../core/widgets/status_indicator.dart';
import '../data/home_provider.dart';
import '../data/home_repository.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';

class HomePage extends ConsumerStatefulWidget {
  const HomePage({super.key});

  @override
  ConsumerState<HomePage> createState() => _HomePageState();
}

class _HomePageState extends ConsumerState<HomePage> {
  final _distanceController = TextEditingController();
  final _durationController = TextEditingController();
  bool _isSubmitting = false;

  @override
  void dispose() {
    _distanceController.dispose();
    _durationController.dispose();
    super.dispose();
  }

  void _showCompletionSheet(String dayId, double plannedDistance, int plannedDuration) {
    _distanceController.text = plannedDistance.toStringAsFixed(1);
    _durationController.text = plannedDuration.toString();

    AppBottomSheet.show(
      context: context,
      title: 'Log Workout',
      child: StatefulBuilder(
        builder: (context, setModalState) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Enter details for your completed run.',
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: _distanceController,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(
                  labelText: 'Actual Distance (km)',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: _durationController,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Actual Duration (minutes)',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              if (_isSubmitting)
                const Center(child: CircularProgressIndicator(color: AppColors.primary))
              else
                AppPrimaryButton(
                  label: 'Log Workout',
                  onPressed: () async {
                    final dist = double.tryParse(_distanceController.text.trim()) ?? plannedDistance;
                    final dur = int.tryParse(_durationController.text.trim()) ?? plannedDuration;

                    setModalState(() => _isSubmitting = true);
                    setState(() => _isSubmitting = true);
                    try {
                      final repo = ref.read(homeRepositoryProvider);
                      await repo.completeWorkout(dayId, dist, dur, 'Completed today!');
                      ref.invalidate(homeDataProvider);
                      ref.invalidate(calendarDataProvider);
                      ref.invalidate(profileOverviewProvider);
                      ref.invalidate(activePlanDetailsProvider);
                      if (context.mounted) Navigator.pop(context);
                    } catch (e) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text('Error: ${e.toString()}')),
                        );
                      }
                    } finally {
                      setModalState(() => _isSubmitting = false);
                      setState(() => _isSubmitting = false);
                    }
                  },
                ),
            ],
          );
        },
      ),
    );
  }

  void _showNotTodaySheet(String dayId) {
    String selectedReason = 'Too busy';
    final reasons = ['Too busy', 'Feeling tired', 'Bad weather', 'Injured / Sore', 'Other'];

    AppBottomSheet.show(
      context: context,
      title: 'Skip Workout today?',
      child: StatefulBuilder(
        builder: (context, setModalState) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Let us know why you can\'t run today. We will adjust the rest of your week.',
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.md),
               ...reasons.map((reason) {
                return RadioListTile<String>(
                  title: Text(reason, style: AppTextStyles.bodyMedium),
                  value: reason,
                  groupValue: selectedReason,
                  activeColor: AppColors.primary,
                  onChanged: (val) {
                    if (val != null) {
                      setModalState(() => selectedReason = val);
                    }
                  },
                );
              }),
              const SizedBox(height: AppSpacing.lg),
              if (_isSubmitting)
                const Center(child: CircularProgressIndicator(color: AppColors.primary))
              else
                AppPrimaryButton(
                  label: 'Skip Workout',
                  onPressed: () async {
                    setModalState(() => _isSubmitting = true);
                    setState(() => _isSubmitting = true);
                    try {
                      final repo = ref.read(homeRepositoryProvider);
                      final decision = await repo.createNotTodayDecision(dayId, selectedReason);
                      await repo.confirmNotTodayDecision(decision.decisionId);
                      ref.invalidate(homeDataProvider);
                      ref.invalidate(calendarDataProvider);
                      ref.invalidate(profileOverviewProvider);
                      ref.invalidate(activePlanDetailsProvider);
                      if (context.mounted) Navigator.pop(context);
                    } catch (e) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text('Error: ${e.toString()}')),
                        );
                      }
                    } finally {
                      setModalState(() => _isSubmitting = false);
                      setState(() => _isSubmitting = false);
                    }
                  },
                ),
            ],
          );
        },
      ),
    );
  }

  void _showTipDialog(String title, String message) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title, style: AppTextStyles.h2),
        content: Text(message, style: AppTextStyles.bodyMedium),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Got it'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final homeState = ref.watch(homeDataProvider);

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async => ref.refresh(homeDataProvider),
          color: AppColors.primary,
          child: homeState.when(
            loading: () => const LoadingState(message: 'Updating your schedule...'),
            error: (err, _) => Center(
              child: Padding(
                padding: const EdgeInsets.all(AppSpacing.lg),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(Icons.cloud_off_rounded, size: 64, color: AppColors.textMuted),
                    const SizedBox(height: AppSpacing.md),
                    Text('Could not load plan data', style: AppTextStyles.h2),
                    const SizedBox(height: AppSpacing.sm),
                    Text(err.toString(), style: AppTextStyles.bodyMedium, textAlign: TextAlign.center),
                    const SizedBox(height: AppSpacing.lg),
                    ElevatedButton(
                      onPressed: () => ref.refresh(homeDataProvider),
                      child: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            ),
            data: (homeData) {
              final activePlan = homeData.activePlan;
              if (activePlan == null) {
                return EmptyState(
                  title: 'Ready to start running?',
                  message: 'Create your adaptive plan and start tracking workouts.',
                  illustration: const Icon(Icons.directions_run_rounded, size: 90, color: AppColors.primary),
                  actionLabel: 'Create a Plan',
                  action: () => context.go(AppRoutes.goalSelection),
                );
              }

              final todayWorkout = homeData.todayWorkout;
              final dailyTip = homeData.dailyTip;
              final weekSummary = homeData.weekSummary;

              // Calculate weekly completed distance
              double weeklyCompleted = weekSummary.fold(0.0, (sum, day) => sum + (day.actualDistanceKm ?? 0.0));
              double weeklyPlanned = weekSummary.fold(0.0, (sum, day) => sum + day.plannedDistanceKm);

              return SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const SizedBox(height: AppSpacing.md),

                    // ── Header ───────────────────────────────────────────────────
                    Row(
                      children: [
                        CircleAvatar(
                          radius: 22,
                          backgroundColor: AppColors.primaryLight,
                          child: const Icon(Icons.directions_run_rounded, color: AppColors.primary),
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(activePlan.level == 'new_to_running' ? 'Hello, Runner!' : 'Hello, Champ!', style: AppTextStyles.h3),
                            Text(activePlan.progressText.toUpperCase(), style: AppTextStyles.label),
                          ],
                        ),
                        const Spacer(),
                        IconButton(
                          icon: const Icon(Icons.refresh_rounded, color: AppColors.textSecondary),
                          onPressed: () => ref.refresh(homeDataProvider),
                        ),
                      ],
                    ),
                    const SizedBox(height: AppSpacing.lg),

                    // ── Pending Confirmations Banner ────────────────────────────
                    if (homeData.hasPendingConfirmations) ...[
                      Container(
                        margin: const EdgeInsets.only(bottom: AppSpacing.md),
                        padding: const EdgeInsets.all(AppSpacing.md),
                        decoration: BoxDecoration(
                          color: AppColors.primaryLight,
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: Row(
                          children: [
                            const Icon(Icons.notification_important_rounded, color: AppColors.primary),
                            const SizedBox(width: AppSpacing.md),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('Unconfirmed Runs', style: AppTextStyles.h3.copyWith(color: AppColors.primary)),
                                  Text(
                                    'You have past runs waiting to be confirmed.',
                                    style: AppTextStyles.bodySmall.copyWith(color: AppColors.textSecondary),
                                  ),
                                ],
                              ),
                            ),
                            TextButton(
                              onPressed: () => context.go(AppRoutes.pendingConfirmation),
                              child: const Text('Resolve', style: TextStyle(fontWeight: FontWeight.bold)),
                            ),
                          ],
                        ),
                      ),
                    ],

                    // ── Today's Plan Card ───────────────────────────────────────
                    if (todayWorkout == null || todayWorkout.dayType == 'rest' || todayWorkout.status == 'skipped')
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(AppSpacing.lg),
                        decoration: BoxDecoration(
                          color: AppColors.surface,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: Column(
                          children: [
                            const Icon(Icons.hotel_rounded, size: 48, color: AppColors.textMuted),
                            const SizedBox(height: AppSpacing.md),
                            Text('Rest Day', style: AppTextStyles.h2),
                            const SizedBox(height: AppSpacing.xs),
                            Text(
                              'Enjoy your rest today! Recovery is essential for building strength.',
                              style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
                              textAlign: TextAlign.center,
                            ),
                          ],
                        ),
                      )
                    else
                      Container(
                        padding: const EdgeInsets.all(AppSpacing.md),
                        decoration: BoxDecoration(
                          color: AppColors.todayCardBackground,
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text("TODAY'S WORKOUT", style: AppTextStyles.labelPrimary),
                                if (dailyTip != null)
                                  OutlinedButton.icon(
                                    onPressed: () => _showTipDialog(dailyTip.title, dailyTip.message),
                                    icon: const Icon(Icons.lightbulb_outline, size: 16),
                                    label: const Text('Tip'),
                                    style: OutlinedButton.styleFrom(
                                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                      minimumSize: Size.zero,
                                      textStyle: AppTextStyles.bodySmall,
                                      side: const BorderSide(color: AppColors.primary),
                                      foregroundColor: AppColors.primary,
                                    ),
                                  ),
                              ],
                            ),
                            const SizedBox(height: AppSpacing.md),
                            Row(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        '${todayWorkout.plannedDistanceKm.toStringAsFixed(1)} km',
                                        style: AppTextStyles.displayLarge,
                                      ),
                                      Text(todayWorkout.title, style: AppTextStyles.h2),
                                      const SizedBox(height: AppSpacing.xs),
                                      Text(
                                        todayWorkout.description,
                                        style: AppTextStyles.bodySmall.copyWith(color: AppColors.textSecondary),
                                      ),
                                      if (todayWorkout.plannedPaceMinKm != null) ...[
                                        const SizedBox(height: AppSpacing.sm),
                                        Row(
                                          children: [
                                            const Icon(Icons.timer_outlined, size: 16, color: AppColors.textSecondary),
                                            const SizedBox(width: 4),
                                            Text('Pace: ${todayWorkout.plannedPaceMinKm!.toStringAsFixed(2)} min/km', style: AppTextStyles.bodyMedium),
                                          ],
                                        ),
                                      ],
                                    ],
                                  ),
                                ),
                                const SizedBox(width: AppSpacing.md),
                                WorkoutTypeBadge(type: todayWorkout.dayType),
                              ],
                            ),
                            const SizedBox(height: AppSpacing.md),
                            if (todayWorkout.status == 'completed')
                              Row(
                                children: [
                                  const Icon(Icons.check_circle_rounded, color: AppColors.completed, size: 20),
                                  const SizedBox(width: AppSpacing.sm),
                                  Text(
                                    'Logged: ${todayWorkout.actualDistanceKm?.toStringAsFixed(1) ?? todayWorkout.plannedDistanceKm.toStringAsFixed(1)} km in ${todayWorkout.actualDurationMin ?? todayWorkout.plannedDurationMin} mins',
                                    style: AppTextStyles.h3.copyWith(color: AppColors.completed),
                                  ),
                                ],
                              )
                            else ...[
                              Row(
                                children: [
                                  Expanded(
                                    child: ElevatedButton.icon(
                                      onPressed: () => _showCompletionSheet(
                                        todayWorkout.dayId,
                                        todayWorkout.plannedDistanceKm,
                                        todayWorkout.plannedDurationMin,
                                      ),
                                      icon: const Icon(Icons.check_rounded, size: 18),
                                      label: const Text('Completed'),
                                      style: ElevatedButton.styleFrom(
                                        backgroundColor: AppColors.completed,
                                        foregroundColor: AppColors.textOnDark,
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: AppSpacing.sm),
                                  Expanded(
                                    child: OutlinedButton.icon(
                                      onPressed: () => _showNotTodaySheet(todayWorkout.dayId),
                                      icon: const Icon(Icons.close_rounded, size: 18),
                                      label: const Text('Not Today'),
                                      style: OutlinedButton.styleFrom(
                                        side: const BorderSide(color: AppColors.border),
                                        foregroundColor: AppColors.textPrimary,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ],
                        ),
                      ),
                    const SizedBox(height: AppSpacing.lg),

                    // ── Weekly Mini Calendar ─────────────────────────────────────
                    Text('WEEK SUMMARY', style: AppTextStyles.label),
                    const SizedBox(height: AppSpacing.sm),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: weekSummary.map((day) {
                        final isToday = day.date.day == DateTime.now().day &&
                            day.date.month == DateTime.now().month;
                        final weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
                        final label = weekdays[day.date.weekday - 1];

                        return Container(
                          width: 44,
                          padding: const EdgeInsets.symmetric(vertical: 8),
                          decoration: BoxDecoration(
                            color: isToday ? AppColors.primary : AppColors.surface,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                              color: isToday ? AppColors.primary : AppColors.border,
                            ),
                          ),
                          child: Column(
                            children: [
                              Text(
                                label,
                                style: AppTextStyles.bodySmall.copyWith(
                                  color: isToday ? Colors.white : AppColors.textMuted,
                                  fontSize: 10,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                day.date.day.toString(),
                                style: AppTextStyles.calendarDayNumber.copyWith(
                                  color: isToday ? Colors.white : AppColors.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 4),
                              StatusIndicator(status: day.status, showText: false),
                            ],
                          ),
                        );
                      }).toList(),
                    ),
                    const SizedBox(height: AppSpacing.lg),

                    // ── Insight Widgets ───────────────────────────────────────────
                    Row(
                      children: [
                        Expanded(
                          child: Container(
                            padding: const EdgeInsets.all(AppSpacing.md),
                            decoration: BoxDecoration(
                              color: AppColors.weeklyCardBackground,
                              borderRadius: BorderRadius.circular(16),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('THIS WEEK', style: AppTextStyles.label),
                                const SizedBox(height: AppSpacing.sm),
                                Text(
                                  '${weeklyCompleted.toStringAsFixed(1)} / ${weeklyPlanned.toStringAsFixed(1)} km',
                                  style: AppTextStyles.h3,
                                ),
                                const SizedBox(height: AppSpacing.sm),
                                LinearProgressIndicator(
                                  value: weeklyPlanned > 0 ? (weeklyCompleted / weeklyPlanned).clamp(0.0, 1.0) : 0.0,
                                  backgroundColor: AppColors.border,
                                  color: AppColors.ctaDark,
                                  borderRadius: BorderRadius.circular(4),
                                ),
                              ],
                            ),
                          ),
                        ),
                        const SizedBox(width: AppSpacing.sm),
                        if (dailyTip != null)
                          Expanded(
                            child: Container(
                              padding: const EdgeInsets.all(AppSpacing.md),
                              decoration: BoxDecoration(
                                color: AppColors.tipCardBackground,
                                borderRadius: BorderRadius.circular(16),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('DAILY TIP', style: AppTextStyles.label),
                                  const SizedBox(height: AppSpacing.sm),
                                  Text(
                                    dailyTip.message,
                                    maxLines: 4,
                                    overflow: TextOverflow.ellipsis,
                                    style: AppTextStyles.bodyMedium.copyWith(height: 1.3),
                                  ),
                                ],
                              ),
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: AppSpacing.lg),
                  ],
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}
