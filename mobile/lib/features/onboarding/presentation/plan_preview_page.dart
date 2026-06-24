import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/routing/app_router.dart';
import '../data/onboarding_provider.dart';
import '../../plan/data/plan_repository.dart';
import '../../../core/network/bootstrap_provider.dart';

class PlanPreviewPage extends ConsumerStatefulWidget {
  const PlanPreviewPage({super.key});

  @override
  ConsumerState<PlanPreviewPage> createState() => _PlanPreviewPageState();
}

class _PlanPreviewPageState extends ConsumerState<PlanPreviewPage> {
  bool _isConfirming = false;

  void _onConfirm(String previewId) async {
    setState(() => _isConfirming = true);
    try {
      final repo = ref.read(planRepositoryProvider);
      await repo.confirmPlan(previewId);
      // Invalidate bootstrap to refresh routing state
      ref.invalidate(bootstrapDataProvider);
      if (mounted) {
        context.go(AppRoutes.home);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to confirm plan: ${e.toString()}')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isConfirming = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(onboardingProvider);
    final preview = state.previewResponse;

    if (preview == null) {
      return Scaffold(
        backgroundColor: AppColors.background,
        appBar: AppBar(title: const Text('Your Plan')),
        body: const Center(
          child: Text('No preview generated. Please go back and try again.'),
        ),
      );
    }

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Your Running Plan'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary),
          onPressed: () => context.go(AppRoutes.goalSelection),
        ),
      ),
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Here is your plan overview', style: AppTextStyles.h1),
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      'Review the weekly mileage progression before confirming.',
                      style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: AppSpacing.lg),

                    // Plan Info Card
                    AppCard(
                      child: Row(
                        children: [
                          Expanded(
                            child: _InfoCol(label: 'DURATION', val: '${preview.weeks.length} Weeks'),
                          ),
                          Container(width: 1, height: 40, color: AppColors.border),
                          Expanded(
                            child: _InfoCol(label: 'FREQUENCY', val: '${preview.daysPerWeek} Days/Wk'),
                          ),
                          Container(width: 1, height: 40, color: AppColors.border),
                          Expanded(
                            child: _InfoCol(label: 'UNIT', val: preview.unit.toUpperCase()),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppSpacing.lg),

                    // Weekly progression cards
                    Text('WEEKLY PROGRESSION', style: AppTextStyles.label),
                    const SizedBox(height: AppSpacing.sm),

                    ListView.separated(
                      shrinkWrap: true,
                      physics: const NeverScrollableScrollPhysics(),
                      itemCount: preview.weeks.length,
                      separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
                      itemBuilder: (context, index) {
                        final week = preview.weeks[index];
                        // Calculate total planned distance for this week
                        double weeklyDistance = week.days.fold(0.0, (sum, day) => sum + day.distanceKm);
                        return AppCard(
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('Week ${week.weekNumber}', style: AppTextStyles.h3),
                                  Text(
                                    week.weekType == 'recovery' || week.weekType == 'Recovery' ? 'Recovery Week' : 'Build Week',
                                    style: AppTextStyles.bodySmall.copyWith(color: AppColors.textSecondary),
                                  ),
                                ],
                              ),
                              Row(
                                children: [
                                  Text(
                                    '${weeklyDistance.toStringAsFixed(1)} ${preview.unit}',
                                    style: AppTextStyles.h2.copyWith(color: AppColors.primary),
                                  ),
                                  const SizedBox(width: AppSpacing.xs),
                                  const Icon(Icons.arrow_forward_ios_rounded, size: 14, color: AppColors.textMuted),
                                ],
                              ),
                            ],
                          ),
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Column(
                children: [
                  if (_isConfirming)
                    const Center(child: CircularProgressIndicator(color: AppColors.primary))
                  else
                    AppPrimaryButton(
                      label: 'Confirm and Start Plan',
                      onPressed: () => _onConfirm(preview.previewId),
                    ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    'You can cancel or change this plan at any time.',
                    style: AppTextStyles.bodySmall.copyWith(color: AppColors.textSecondary),
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

class _InfoCol extends StatelessWidget {
  const _InfoCol({required this.label, required this.val});
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
