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

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    // Map backend enums or load existing level
    _selected = state.level;
    if (_selected != 'new_to_running' && _selected != 'used_to_run' && _selected != 'running_regularly') {
      _selected = null;
    }
  }

  void _onContinue() {
    if (_selected == null) return;
    ref.read(onboardingProvider.notifier).updateLevel(_selected!);

    final goalType = ref.read(onboardingProvider).goalType;
    if (goalType == 'habit') {
      context.go(AppRoutes.habitGoal); // Habit flow goes to HabitGoalPage
    } else {
      context.go(AppRoutes.goalTime); // Race flow goes to GoalTimePage (NEW)
    }
  }

  @override
  Widget build(BuildContext context) {
    final goalType = ref.watch(onboardingProvider).goalType;

    return Scaffold(
      backgroundColor: AppColors.background, // Matches the reference background
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top Category label
              Text(
                'Experience',
                style: AppTextStyles.label.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),

              // Back button & Progress bar row
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_rounded, color: AppColors.textPrimary),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                    onPressed: () {
                      if (goalType == 'race') {
                        context.go(AppRoutes.raceDetails);
                      } else {
                        context.go(AppRoutes.goalSelection);
                      }
                    },
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(100),
                      child: const LinearProgressIndicator(
                        value: 0.4,
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
                'What is your running\nbackground?',
                style: AppTextStyles.h1.copyWith(
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'This helps us tailor the plan to your current fitness level.',
                style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              // Option 1: New to running
              SelectableCard(
                isSelected: _selected == 'new_to_running',
                onTap: () => setState(() => _selected = 'new_to_running'),
                child: Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: const BoxDecoration(
                        color: AppColors.primary, // Solid blue matching what-is-ur-background.png
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.eco_rounded, color: Colors.white), // White leaf icon
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('New to running', style: AppTextStyles.h3),
                          Text("I'm just getting started", style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                    _CheckCircle(selected: _selected == 'new_to_running'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 2: Used to run
              SelectableCard(
                isSelected: _selected == 'used_to_run',
                onTap: () => setState(() => _selected = 'used_to_run'),
                child: Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: const BoxDecoration(
                        color: AppColors.longRunTint, // Light purple
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.history_rounded, color: Colors.purple), // Purple history icon
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Used to run', style: AppTextStyles.h3),
                          Text('Returning after a break', style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                    _CheckCircle(selected: _selected == 'used_to_run'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Option 3: Running regularly
              SelectableCard(
                isSelected: _selected == 'running_regularly',
                onTap: () => setState(() => _selected = 'running_regularly'),
                child: Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: const BoxDecoration(
                        color: AppColors.restTint, // Light orange
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.directions_run_rounded, color: Colors.orange), // Orange runner icon
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Running regularly', style: AppTextStyles.h3),
                          Text('I run consistent distances', style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                    _CheckCircle(selected: _selected == 'running_regularly'),
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
