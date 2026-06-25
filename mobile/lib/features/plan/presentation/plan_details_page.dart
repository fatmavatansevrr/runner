import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../../profile/data/profile_provider.dart';

class PlanDetailsPage extends ConsumerWidget {
  const PlanDetailsPage({super.key});

  String _getWeekdayName(DateTime date) {
    const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    return days[date.weekday - 1];
  }

  String _formatGoalType(String gt) {
    return gt == 'habit' ? 'Build a Habit' : 'Train for a Race';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final planDetailsState = ref.watch(activePlanDetailsProvider);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Plan Details'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary),
          onPressed: () => context.pop(),
        ),
      ),
      body: SafeArea(
        child: planDetailsState.when(
          loading: () => const LoadingState(message: 'Loading active plan details...'),
          error: (err, _) => Center(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.cloud_off_rounded, size: 48, color: AppColors.textMuted),
                  const SizedBox(height: AppSpacing.md),
                  Text('Error loading plan details', style: AppTextStyles.h3),
                  const SizedBox(height: AppSpacing.xs),
                  Text(err.toString(), style: AppTextStyles.bodySmall, textAlign: TextAlign.center),
                  const SizedBox(height: AppSpacing.md),
                  ElevatedButton(
                    onPressed: () => ref.invalidate(activePlanDetailsProvider),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            ),
          ),
          data: (plan) {
            return SingleChildScrollView(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${plan.goalDistance.toUpperCase().replaceAll('_', ' ')} ${_formatGoalType(plan.goalType)} Plan', style: AppTextStyles.h1),
                  Text(
                    'Goal: ${_formatGoalType(plan.goalType)}  |  Duration: ${plan.totalWeeks} Weeks  |  Unit: ${plan.unit.toUpperCase()}',
                    style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
                  ),
                  const SizedBox(height: AppSpacing.xl),

                  ListView.separated(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    itemCount: plan.weeks.length,
                    separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.lg),
                    itemBuilder: (context, weekIndex) {
                      final week = plan.weeks[weekIndex];
                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Padding(
                            padding: const EdgeInsets.only(left: 4, bottom: AppSpacing.xs),
                            child: Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text('WEEK ${week.weekNumber}', style: AppTextStyles.label),
                                Text(
                                  '${week.actualVolumeKm.toStringAsFixed(1)} / ${week.plannedVolumeKm.toStringAsFixed(1)} ${plan.unit}',
                                  style: AppTextStyles.bodySmall.copyWith(color: AppColors.textSecondary, fontWeight: FontWeight.bold),
                                ),
                              ],
                            ),
                          ),
                          AppCard(
                            padding: EdgeInsets.zero,
                            child: Column(
                              children: List.generate(week.days.length, (dayIndex) {
                                final day = week.days[dayIndex];
                                final isLast = dayIndex == week.days.length - 1;

                                return Column(
                                  children: [
                                    _RunItem(
                                      dayName: _getWeekdayName(day.date),
                                      type: day.dayType,
                                      distance: '${day.plannedDistanceKm.toStringAsFixed(1)} ${plan.unit}',
                                      duration: '${day.plannedDurationMin} min',
                                      status: day.status,
                                    ),
                                    if (!isLast) const Divider(height: 1, indent: 16, endIndent: 16),
                                  ],
                                );
                              }),
                            ),
                          ),
                        ],
                      );
                    },
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

class _RunItem extends StatelessWidget {
  const _RunItem({
    required this.dayName,
    required this.type,
    required this.distance,
    required this.duration,
    required this.status,
  });

  final String dayName;
  final String type;
  final String distance;
  final String duration;
  final String status;

  @override
  Widget build(BuildContext context) {
    final isCompleted = status == 'completed';

    return Padding(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Text(dayName, style: AppTextStyles.h3),
                  if (isCompleted) const SizedBox(width: 6),
                  if (isCompleted) const Icon(Icons.check_circle_rounded, color: AppColors.completed, size: 16),
                ],
              ),
              const SizedBox(height: 2),
              WorkoutTypeBadge(type: type),
            ],
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                distance,
                style: AppTextStyles.h3.copyWith(
                  color: isCompleted ? AppColors.completed : AppColors.primary,
                ),
              ),
              Text(duration, style: AppTextStyles.bodySmall.copyWith(color: AppColors.textSecondary)),
            ],
          ),
        ],
      ),
    );
  }
}
