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

class RunningBackgroundPage extends ConsumerStatefulWidget {
  const RunningBackgroundPage({super.key});

  @override
  ConsumerState<RunningBackgroundPage> createState() => _RunningBackgroundPageState();
}

class _RunningBackgroundPageState extends ConsumerState<RunningBackgroundPage> {
  String? _selected; // "new_to_running" | "used_to_run" | "running_regularly"

  void _onContinue() {
    if (_selected == null) return;
    ref.read(onboardingProvider.notifier).updateLevel(_selected!);

    final goalType = ref.read(onboardingProvider).goalType;
    if (goalType == 'habit') {
      context.push(AppRoutes.habitGoal);
    } else {
      context.push(AppRoutes.weeklyFrequency);
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
                value: 0.45,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('What is your\nrunning experience?', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xl),

              // Option 1
              SelectableCard(
                isSelected: _selected == 'new_to_running',
                onTap: () => setState(() => _selected = 'new_to_running'),
                child: Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: const BoxDecoration(
                        color: AppColors.restTint,
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.sentiment_satisfied_alt_rounded, color: Colors.orange),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('New to running', style: AppTextStyles.h3),
                          Text('I am just starting my running journey.', style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 2
              SelectableCard(
                isSelected: _selected == 'used_to_run',
                onTap: () => setState(() => _selected = 'used_to_run'),
                child: Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: const BoxDecoration(
                        color: AppColors.easyRunTint,
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.directions_walk_rounded, color: AppColors.primary),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Used to run', style: AppTextStyles.h3),
                          Text('Returning to running after a break.', style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 3
              SelectableCard(
                isSelected: _selected == 'running_regularly',
                onTap: () => setState(() => _selected = 'running_regularly'),
                child: Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: const BoxDecoration(
                        color: AppColors.longRunTint,
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.directions_run_rounded, color: Colors.deepPurpleAccent),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Running regularly', style: AppTextStyles.h3),
                          Text('I currently run active weekly miles.', style: AppTextStyles.bodyMedium),
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
