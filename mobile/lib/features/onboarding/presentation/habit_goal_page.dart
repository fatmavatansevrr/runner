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

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    _selected = state.habitGoal;
  }

  void _onContinue() {
    if (_selected == null) return;

    ref.read(onboardingProvider.notifier).updateHabitGoal(_selected!);

    if (_selected == 'five_k') {
      ref.read(onboardingProvider.notifier).updateGoalDistance('five_k');
      context.go(AppRoutes.weeklyFrequency);
    } else if (_selected == 'ten_k') {
      ref.read(onboardingProvider.notifier).updateGoalDistance('ten_k');
      context.go(AppRoutes.weeklyFrequency);
    } else if (_selected == 'duration_30') {
      ref.read(onboardingProvider.notifier).updateGoalDistance('five_k');
      ref.read(onboardingProvider.notifier).updatePreferredRunDuration(30);
      context.go(AppRoutes.weeklyFrequency);
    } else if (_selected == 'custom') {
      ref.read(onboardingProvider.notifier).updateGoalDistance('custom');
      context.go(AppRoutes.customGoal);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top category label
              Text(
                'Plan Goal',
                style: AppTextStyles.label.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),

              // Back button and progress bar
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_rounded, color: AppColors.textPrimary),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                    onPressed: () => context.go(AppRoutes.runningBackground),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(100),
                      child: const LinearProgressIndicator(
                        value: 0.5,
                        backgroundColor: AppColors.border,
                        color: AppColors.primary,
                        minHeight: 6,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.xl),

              Text(
                'What kind of plan\nare we building?',
                style: AppTextStyles.h1.copyWith(
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Choose the option that best describes you.',
                style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              // Option 1: Run 5 km
              SelectableCard(
                isSelected: _selected == 'five_k',
                onTap: () => setState(() => _selected = 'five_k'),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Run 5 km', style: AppTextStyles.h3),
                          const SizedBox(height: 2),
                          Text('Perfect for beginners looking to build consistency.', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                        ],
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    _CheckCircle(selected: _selected == 'five_k'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 2: Run 10 km
              SelectableCard(
                isSelected: _selected == 'ten_k',
                onTap: () => setState(() => _selected = 'ten_k'),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Run 10 km', style: AppTextStyles.h3),
                          const SizedBox(height: 2),
                          Text('For runners looking to step up their distance.', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                        ],
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    _CheckCircle(selected: _selected == 'ten_k'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 3: Run for 30 minutes
              SelectableCard(
                isSelected: _selected == 'duration_30',
                onTap: () => setState(() => _selected = 'duration_30'),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Run for 30 minutes', style: AppTextStyles.h3),
                          const SizedBox(height: 2),
                          Text('Focus on time on your feet rather than distance.', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                        ],
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    _CheckCircle(selected: _selected == 'duration_30'),
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
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Custom goal', style: AppTextStyles.h3),
                          const SizedBox(height: 2),
                          Text('Set your own distance and time target.', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                        ],
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    _CheckCircle(selected: _selected == 'custom'),
                  ],
                ),
              ),

              const Spacer(),

              AppPrimaryButton(
                label: 'Continue',
                icon: Icons.arrow_forward_rounded,
                onPressed: _selected == null ? null : _onContinue,
              ),
              const SizedBox(height: AppSpacing.xs),
            ],
          ),
        ),
      ),
    );
  }
}

class _CheckCircle extends StatelessWidget {
  const _CheckCircle({required this.selected});
  final bool selected;

  @override
  Widget build(BuildContext context) {
    return AnimatedContainer(
      duration: const Duration(milliseconds: 150),
      width: 24,
      height: 24,
      decoration: BoxDecoration(
        color: selected ? AppColors.primary : Colors.transparent,
        shape: BoxShape.circle,
        border: Border.all(
          color: selected ? AppColors.primary : AppColors.border,
          width: 2,
        ),
      ),
      child: selected
          ? const Icon(Icons.check, color: Colors.white, size: 14)
          : null,
    );
  }
}

