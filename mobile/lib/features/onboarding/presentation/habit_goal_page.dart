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

class HabitGoalPage extends ConsumerStatefulWidget {
  const HabitGoalPage({super.key});

  @override
  ConsumerState<HabitGoalPage> createState() => _HabitGoalPageState();
}

class _HabitGoalPageState extends ConsumerState<HabitGoalPage> {
  String? _selected; // "five_k" | "ten_k" | "duration_30" | "custom"

  void _onContinue() {
    if (_selected == null) return;

    if (_selected == 'five_k') {
      ref.read(onboardingProvider.notifier).updateGoalDistance('five_k');
      context.push(AppRoutes.weeklyFrequency);
    } else if (_selected == 'ten_k') {
      ref.read(onboardingProvider.notifier).updateGoalDistance('ten_k');
      context.push(AppRoutes.weeklyFrequency);
    } else if (_selected == 'duration_30') {
      // Map 30 min run to five_k template
      ref.read(onboardingProvider.notifier).updateGoalDistance('five_k');
      context.push(AppRoutes.weeklyFrequency);
    } else if (_selected == 'custom') {
      ref.read(onboardingProvider.notifier).updateGoalDistance('custom');
      context.push(AppRoutes.customGoal);
    }
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
                value: 0.5,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('Choose a habit goal', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Select a target distance or duration that feels achievable.',
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              // Option 1: 5k
              SelectableCard(
                isSelected: _selected == 'five_k',
                onTap: () => setState(() => _selected = 'five_k'),
                child: Row(
                  children: [
                    const Icon(Icons.directions_run_rounded, color: AppColors.primary),
                    const SizedBox(width: AppSpacing.md),
                    Text('Run 5 km', style: AppTextStyles.h3),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 2: 10k
              SelectableCard(
                isSelected: _selected == 'ten_k',
                onTap: () => setState(() => _selected = 'ten_k'),
                child: Row(
                  children: [
                    const Icon(Icons.directions_run_rounded, color: Colors.purple),
                    const SizedBox(width: AppSpacing.md),
                    Text('Run 10 km', style: AppTextStyles.h3),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 3: 30 minutes
              SelectableCard(
                isSelected: _selected == 'duration_30',
                onTap: () => setState(() => _selected = 'duration_30'),
                child: Row(
                  children: [
                    const Icon(Icons.timer_outlined, color: Colors.teal),
                    const SizedBox(width: AppSpacing.md),
                    Text('Run for 30 minutes', style: AppTextStyles.h3),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 4: Custom goal
              SelectableCard(
                isSelected: _selected == 'custom',
                onTap: () => setState(() => _selected = 'custom'),
                child: Row(
                  children: [
                    const Icon(Icons.tune_rounded, color: Colors.orange),
                    const SizedBox(width: AppSpacing.md),
                    Text('Custom goal', style: AppTextStyles.h3),
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
