import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../data/pending_confirmation_provider.dart';
import '../data/pending_confirmation_repository.dart';
import '../../home/data/home_provider.dart';
import '../../../core/network/dtos.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';

class PendingConfirmationPage extends ConsumerStatefulWidget {
  const PendingConfirmationPage({super.key});

  @override
  ConsumerState<PendingConfirmationPage> createState() => _PendingConfirmationPageState();
}

class _PendingConfirmationPageState extends ConsumerState<PendingConfirmationPage> {
  final Map<String, String> _resolutions = {}; // id -> 'completed' | 'missed'
  bool _isSubmitting = false;

  void _markResolution(String id, String val) {
    setState(() {
      _resolutions[id] = val;
    });
  }

  void _onSave(List<PendingConfirmationResponse> items) async {
    setState(() => _isSubmitting = true);
    try {
      final repo = ref.read(pendingConfirmationRepositoryProvider);
      
      // Resolve all items that have a selection
      for (final item in items) {
        final res = _resolutions[item.pendingConfirmationId];
        if (res != null) {
          await repo.resolvePendingConfirmation(
            pendingConfirmationId: item.pendingConfirmationId,
            resolution: res,
            actualDistanceKm: res == 'completed' ? item.plannedDistanceKm : 0.0,
            actualDurationMin: res == 'completed' ? item.plannedDurationMin : 0,
            userNote: 'Resolved via pending confirmations',
          );
        }
      }

      // Invalidate providers
      ref.invalidate(pendingConfirmationsProvider);
      ref.invalidate(homeDataProvider);
      ref.invalidate(calendarDataProvider);
      ref.invalidate(profileOverviewProvider);
      ref.invalidate(activePlanDetailsProvider);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Successfully confirmed all runs!')),
        );
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to save resolutions: ${e.toString()}')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final pendingState = ref.watch(pendingConfirmationsProvider);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Pending Confirmations'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary),
          onPressed: () => context.pop(),
        ),
      ),
      body: SafeArea(
        child: pendingState.when(
          loading: () => const LoadingState(message: 'Fetching past workouts...'),
          error: (err, _) => Center(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.cloud_off_rounded, size: 48, color: AppColors.textMuted),
                  const SizedBox(height: AppSpacing.md),
                  Text('Error fetching confirmations', style: AppTextStyles.h3),
                  const SizedBox(height: AppSpacing.xs),
                  Text(err.toString(), style: AppTextStyles.bodySmall, textAlign: TextAlign.center),
                  const SizedBox(height: AppSpacing.md),
                  ElevatedButton(
                    onPressed: () => ref.refresh(pendingConfirmationsProvider),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            ),
          ),
          data: (items) {
            if (items.isEmpty) {
              return EmptyState(
                title: 'All caught up!',
                message: 'No pending workouts require confirmation.',
                illustration: const Icon(Icons.check_circle_outline_rounded, size: 80, color: AppColors.completed),
                actionLabel: 'Go Back',
                action: () => context.pop(),
              );
            }

            final allSelected = items.every((item) => _resolutions.containsKey(item.pendingConfirmationId));

            return Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Confirm your past runs', style: AppTextStyles.h1),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    'Tell us what happened with these workouts to keep your adaptation data accurate.',
                    style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
                  ),
                  const SizedBox(height: AppSpacing.xl),

                  Expanded(
                    child: ListView.separated(
                      itemCount: items.length,
                      separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.md),
                      itemBuilder: (context, index) {
                        final run = items[index];
                        final runId = run.pendingConfirmationId;
                        final resolution = _resolutions[runId];

                        final dateFormatted = '${run.date.day}/${run.date.month}/${run.date.year}';

                        return AppCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text(dateFormatted, style: AppTextStyles.label),
                                  if (resolution != null)
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: resolution == 'completed'
                                            ? AppColors.completedLight
                                            : AppColors.missedLight,
                                        borderRadius: BorderRadius.circular(100),
                                      ),
                                      child: Text(
                                        resolution.toUpperCase(),
                                        style: AppTextStyles.label.copyWith(
                                          color: resolution == 'completed'
                                              ? AppColors.completed
                                              : AppColors.missed,
                                          fontSize: 10,
                                        ),
                                      ),
                                    ),
                                ],
                              ),
                              const SizedBox(height: AppSpacing.sm),
                              Text('${run.plannedDistanceKm.toStringAsFixed(1)} km ${run.title}', style: AppTextStyles.h2),
                              const SizedBox(height: AppSpacing.md),

                              Row(
                                children: [
                                  Expanded(
                                    child: OutlinedButton.icon(
                                      onPressed: () => _markResolution(runId, 'completed'),
                                      icon: const Icon(Icons.check_rounded, size: 16),
                                      label: const Text('I Ran'),
                                      style: OutlinedButton.styleFrom(
                                        foregroundColor: AppColors.completed,
                                        side: BorderSide(
                                          color: resolution == 'completed'
                                              ? AppColors.completed
                                              : AppColors.border,
                                        ),
                                        backgroundColor: resolution == 'completed'
                                            ? AppColors.completedLight
                                            : Colors.transparent,
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: AppSpacing.sm),
                                  Expanded(
                                    child: OutlinedButton.icon(
                                      onPressed: () => _markResolution(runId, 'missed'),
                                      icon: const Icon(Icons.close_rounded, size: 16),
                                      label: const Text('Missed'),
                                      style: OutlinedButton.styleFrom(
                                        foregroundColor: AppColors.missed,
                                        side: BorderSide(
                                          color: resolution == 'missed'
                                              ? AppColors.missed
                                              : AppColors.border,
                                        ),
                                        backgroundColor: resolution == 'missed'
                                            ? AppColors.missedLight
                                            : Colors.transparent,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        );
                      },
                    ),
                  ),

                  if (_isSubmitting)
                    const Center(child: CircularProgressIndicator(color: AppColors.primary))
                  else
                    AppPrimaryButton(
                      label: 'Save & Continue',
                      onPressed: !allSelected ? null : () => _onSave(items),
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
