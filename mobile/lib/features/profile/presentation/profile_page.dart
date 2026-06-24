import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../../../core/routing/app_router.dart';
import '../data/profile_provider.dart';
import '../../plan/data/plan_repository.dart';
import '../../../core/network/bootstrap_provider.dart';
import '../../home/data/home_provider.dart';
import '../../calendar/data/calendar_provider.dart';

class ProfilePage extends ConsumerStatefulWidget {
  const ProfilePage({super.key});

  @override
  ConsumerState<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends ConsumerState<ProfilePage> {
  bool _isStopping = false;

  void _showCancelPlanDialog(BuildContext context, String planName) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Stop Active Plan?'),
        content: Text(
          'Are you sure you want to stop your $planName? Your current progress will be archived, and you can start a new plan anytime.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Keep Training'),
          ),
          TextButton(
            onPressed: () async {
              Navigator.pop(ctx);
              setState(() => _isStopping = true);
              try {
                final repo = ref.read(planRepositoryProvider);
                final activePlanAsync = ref.read(activePlanDetailsProvider);
                final planId = activePlanAsync.valueOrNull?.planId;
                if (planId != null) {
                  await repo.cancelPlan(planId, 'User stopped training plan manually.');
                } else {
                  throw Exception('Active plan ID not found.');
                }
                // Refresh all states
                ref.invalidate(profileOverviewProvider);
                ref.invalidate(activePlanDetailsProvider);
                ref.invalidate(homeDataProvider);
                ref.invalidate(calendarDataProvider);
                ref.invalidate(bootstrapDataProvider);
                if (mounted) {
                  context.go(AppRoutes.goalSelection);
                }
              } catch (e) {
                if (mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Failed to cancel plan: ${e.toString()}')),
                  );
                }
              } finally {
                if (mounted) {
                  setState(() => _isStopping = false);
                }
              }
            },
            style: TextButton.styleFrom(foregroundColor: AppColors.missed),
            child: const Text('Stop Plan'),
          ),
        ],
      ),
    );
  }

  String _formatLevel(String level) {
    switch (level) {
      case 'new_to_running':
        return 'New to Running';
      case 'used_to_run':
        return 'Used to Run';
      case 'running_regularly':
        return 'Running Regularly';
      default:
        return level;
    }
  }

  @override
  Widget build(BuildContext context) {
    final profileState = ref.watch(profileOverviewProvider);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        title: Text('Profile', style: AppTextStyles.h2),
        actions: [
          IconButton(
            icon: const Icon(Icons.settings_outlined, color: AppColors.textPrimary),
            onPressed: () => context.push(AppRoutes.settings),
          ),
        ],
      ),
      body: profileState.when(
        loading: () => const LoadingState(message: 'Loading your profile...'),
        error: (err, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.cloud_off_rounded, size: 48, color: AppColors.textMuted),
                const SizedBox(height: AppSpacing.md),
                Text('Error loading profile', style: AppTextStyles.h3),
                const SizedBox(height: AppSpacing.xs),
                Text(err.toString(), style: AppTextStyles.bodySmall, textAlign: TextAlign.center),
                const SizedBox(height: AppSpacing.md),
                ElevatedButton(
                  onPressed: () {
                    ref.refresh(profileOverviewProvider);
                    ref.refresh(activePlanDetailsProvider);
                  },
                  child: const Text('Retry'),
                ),
              ],
            ),
          ),
        ),
        data: (profile) {
          final stats = profile.activePlanStats;
          final initials = profile.name.isNotEmpty
              ? profile.name.split(' ').map((e) => e[0]).take(2).join().toUpperCase()
              : 'JD';

          return SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
            child: Column(
              children: [
                const SizedBox(height: AppSpacing.md),

                // Profile Header
                CircleAvatar(
                  radius: 40,
                  backgroundColor: AppColors.primaryLight,
                  child: Text(
                    initials,
                    style: AppTextStyles.h1.copyWith(color: AppColors.primary, fontSize: 32),
                  ),
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(profile.name.isEmpty ? 'Jane Doe' : profile.name, style: AppTextStyles.h2),
                Text(
                  '${_formatLevel(profile.runningBackground)}  |  ${profile.unit.toUpperCase()}',
                  style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
                ),
                const SizedBox(height: AppSpacing.lg),

                // Stats row
                AppCard(
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      _StatColumn(
                        value: stats != null ? stats.completedRunsCount.toString() : '0',
                        label: 'COMPLETED RUNS',
                      ),
                      Container(width: 1, height: 32, color: AppColors.border),
                      _StatColumn(
                        value: stats != null ? '${stats.totalCompletedDistance.toStringAsFixed(1)} ${profile.unit}' : '0.0 ${profile.unit}',
                        label: 'TOTAL DISTANCE',
                      ),
                      Container(width: 1, height: 32, color: AppColors.border),
                      _StatColumn(
                        value: stats != null ? '1' : '0',
                        label: 'PLANS',
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: AppSpacing.lg),

                // Active Plan details card
                if (stats != null)
                  AppCard(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text('ACTIVE PLAN', style: AppTextStyles.label),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                              decoration: BoxDecoration(
                                color: AppColors.completedLight,
                                borderRadius: BorderRadius.circular(100),
                              ),
                              child: Text(
                                stats.adherenceRatePercent >= 70 ? 'ON TRACK' : 'NEEDS ATTENTION',
                                style: AppTextStyles.label.copyWith(
                                  color: stats.adherenceRatePercent >= 70 ? AppColors.completed : AppColors.primary,
                                  fontSize: 10,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: AppSpacing.sm),
                        Text(stats.planName, style: AppTextStyles.h2),
                        const SizedBox(height: AppSpacing.xs),
                        Text(
                          '${stats.completedRunsCount} of ${stats.totalPlannedRunsCount} runs completed (${stats.adherenceRatePercent.toStringAsFixed(0)}% adherence)',
                          style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
                        ),
                        const SizedBox(height: AppSpacing.md),

                        LinearProgressIndicator(
                          value: stats.totalPlannedRunsCount > 0 ? (stats.completedRunsCount / stats.totalPlannedRunsCount) : 0.0,
                          backgroundColor: AppColors.border,
                          color: AppColors.primary,
                          borderRadius: BorderRadius.circular(4),
                        ),
                        const SizedBox(height: AppSpacing.lg),

                        if (_isStopping)
                          const Center(child: CircularProgressIndicator(color: AppColors.primary))
                        else ...[
                          Row(
                            children: [
                              Expanded(
                                child: AppPrimaryButton(
                                  label: 'View Plan Details',
                                  onPressed: () => context.push(AppRoutes.planDetails),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: AppSpacing.sm),
                          Center(
                            child: TextButton(
                              onPressed: () => _showCancelPlanDialog(context, stats.planName),
                              child: Text(
                                'Stop Plan',
                                style: AppTextStyles.bodyMedium.copyWith(
                                  color: AppColors.missed,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ],
                    ),
                  )
                else
                  AppCard(
                    child: Column(
                      children: [
                        const Icon(Icons.info_outline_rounded, size: 40, color: AppColors.textMuted),
                        const SizedBox(height: AppSpacing.sm),
                        Text('No active plan', style: AppTextStyles.h3),
                        const SizedBox(height: AppSpacing.xs),
                        Text(
                          'Get started by setting up a custom running plan.',
                          style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: AppSpacing.md),
                        AppPrimaryButton(
                          label: 'Get Started',
                          onPressed: () => context.go(AppRoutes.goalSelection),
                        ),
                      ],
                    ),
                  ),
                const SizedBox(height: AppSpacing.lg),

                // Badges Section
                Align(
                  alignment: Alignment.centerLeft,
                  child: Padding(
                    padding: const EdgeInsets.only(left: 4),
                    child: Text('BADGES & ACHIEVEMENTS', style: AppTextStyles.label),
                  ),
                ),
                const SizedBox(height: AppSpacing.sm),

                AppCard(
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      _BadgeWidget(icon: Icons.flash_on_rounded, label: 'First Step', unlocked: stats != null),
                      _BadgeWidget(icon: Icons.done_all_rounded, label: 'Consistent', unlocked: stats != null && stats.completedRunsCount >= 3),
                      _BadgeWidget(icon: Icons.emoji_events_rounded, label: 'First 5K', unlocked: stats != null && stats.totalCompletedDistance >= 5.0),
                      _BadgeWidget(icon: Icons.timer_rounded, label: 'Pace Master', unlocked: stats != null && stats.completedRunsCount >= 10),
                    ],
                  ),
                ),
                const SizedBox(height: AppSpacing.xl),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _StatColumn extends StatelessWidget {
  const _StatColumn({required this.value, required this.label});
  final String value;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(value, style: AppTextStyles.h2.copyWith(color: AppColors.textPrimary)),
        const SizedBox(height: 2),
        Text(label, style: AppTextStyles.label.copyWith(fontSize: 10)),
      ],
    );
  }
}

class _BadgeWidget extends StatelessWidget {
  const _BadgeWidget({required this.icon, required this.label, required this.unlocked});
  final IconData icon;
  final String label;
  final bool unlocked;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Opacity(
          opacity: unlocked ? 1.0 : 0.35,
          child: Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: unlocked ? AppColors.primaryLight : AppColors.border,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: unlocked ? AppColors.primary : AppColors.textSecondary, size: 24),
          ),
        ),
        const SizedBox(height: 4),
        Text(
          label,
          style: AppTextStyles.bodySmall.copyWith(
            fontSize: 10,
            fontWeight: FontWeight.bold,
            color: unlocked ? AppColors.textPrimary : AppColors.textMuted,
          ),
        ),
      ],
    );
  }
}
