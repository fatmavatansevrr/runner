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

class WeeklyFrequencyPage extends ConsumerStatefulWidget {
  const WeeklyFrequencyPage({super.key});

  @override
  ConsumerState<WeeklyFrequencyPage> createState() => _WeeklyFrequencyPageState();
}

class _WeeklyFrequencyPageState extends ConsumerState<WeeklyFrequencyPage> {
  int? _selected; // 3 | 4

  void _onContinue() {
    if (_selected == null) return;
    ref.read(onboardingProvider.notifier).updateDaysPerWeek(_selected!);
    context.push(AppRoutes.runningDays);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary),
          onPressed: () => context.pop(),
        ),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              LinearProgressIndicator(
                value: 0.6,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('How many days\nper week?', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Phase 1 supports 3 or 4 days per week plans. We suggest starting with 3 days.',
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              // Option 1: 3 days
              SelectableCard(
                isSelected: _selected == 3,
                onTap: () => setState(() => _selected = 3),
                child: Row(
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: const BoxDecoration(
                        color: AppColors.easyRunTint,
                        shape: BoxShape.circle,
                      ),
                      child: const Center(
                        child: Text(
                          '3',
                          style: TextStyle(fontWeight: FontWeight.bold, color: AppColors.primary),
                        ),
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('3 days per week', style: AppTextStyles.h3),
                          Text('Best for beginners or building habits.', style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 2: 4 days
              SelectableCard(
                isSelected: _selected == 4,
                onTap: () => setState(() => _selected = 4),
                child: Row(
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: const BoxDecoration(
                        color: AppColors.longRunTint,
                        shape: BoxShape.circle,
                      ),
                      child: const Center(
                        child: Text(
                          '4',
                          style: TextStyle(fontWeight: FontWeight.bold, color: Colors.deepPurple),
                        ),
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('4 days per week', style: AppTextStyles.h3),
                          Text('Recommended if you already run regularly.', style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              const Spacer(),

              AppPrimaryButton(
                label: 'Continue',
                onPressed: _selected == null ? null : _onContinue,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
