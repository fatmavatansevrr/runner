import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/routing/app_router.dart';
import '../../../core/models/running_background.dart';
import '../data/onboarding_provider.dart';

/// Running Background V2 — "what-is-ur-background" screen (design asset
/// reference retained from the original page; the approved four-option
/// layout preserves the existing shell/progress/back-button/card/typography
/// conventions and only changes the option count from three to four).
class RunningBackgroundPage extends ConsumerStatefulWidget {
  const RunningBackgroundPage({super.key});

  @override
  ConsumerState<RunningBackgroundPage> createState() => _RunningBackgroundPageState();
}

class _RunningBackgroundPageState extends ConsumerState<RunningBackgroundPage> {
  RunningBackground? _selected;

  @override
  void initState() {
    super.initState();
    _selected = ref.read(onboardingProvider).runningBackground;
  }

  void _onContinue() {
    final selected = _selected;
    if (selected == null) return;
    ref.read(onboardingProvider.notifier).updateRunningBackground(selected);

    // Conditional navigation (§11): Beginner skips runner-background-details
    // entirely; every other level opens it first.
    if (!selected.skipsRunnerBackgroundDetails) {
      context.go(AppRoutes.runnerBackgroundDetails);
      return;
    }

    final goalType = ref.read(onboardingProvider).goalType;
    if (goalType == 'habit') {
      context.go(AppRoutes.habitGoal); // Habit flow goes to HabitGoalPage
    } else {
      context.go(AppRoutes.goalTime); // Race flow goes to GoalTimePage
    }
  }

  @override
  Widget build(BuildContext context) {
    final goalType = ref.watch(onboardingProvider).goalType;

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Experience',
                style: AppTextStyles.label.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: AppSpacing.xs),

              // Back button & Progress bar row
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_rounded, color: AppColors.textPrimary),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                    tooltip: 'Back',
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
                        value: 0.35,
                        backgroundColor: AppColors.border,
                        color: AppColors.primary,
                        minHeight: 6,
                        semanticsLabel: 'Onboarding progress',
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

              Expanded(
                child: SingleChildScrollView(
                  child: Column(
                    children: [
                      for (final level in RunningBackground.values) ...[
                        _RunningBackgroundOptionCard(
                          level: level,
                          isSelected: _selected == level,
                          onTap: () => setState(() => _selected = level),
                        ),
                        if (level != RunningBackground.values.last)
                          const SizedBox(height: AppSpacing.md),
                      ],
                    ],
                  ),
                ),
              ),

              const SizedBox(height: AppSpacing.md),
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

class _RunningBackgroundOptionCard extends StatelessWidget {
  const _RunningBackgroundOptionCard({
    required this.level,
    required this.isSelected,
    required this.onTap,
  });

  final RunningBackground level;
  final bool isSelected;
  final VoidCallback onTap;

  static const Map<RunningBackground, (IconData, Color, Color)> _visuals = {
    RunningBackground.beginner: (Icons.eco_rounded, AppColors.primary, Colors.white),
    RunningBackground.intermediate: (Icons.directions_run_rounded, AppColors.restTint, Colors.orange),
    RunningBackground.advanced: (Icons.speed_rounded, AppColors.longRunTint, Colors.purple),
    RunningBackground.experienced: (Icons.emoji_events_rounded, AppColors.primary, Colors.white),
  };

  @override
  Widget build(BuildContext context) {
    final (icon, bgColor, iconColor) = _visuals[level]!;

    return Semantics(
      // machine value is never derived from visible text — the semantic
      // label is built here purely for accessibility, from the same
      // label/description used visually, not the other way around.
      label: '${level.label}. ${level.description}',
      selected: isSelected,
      button: true,
      child: SelectableCard(
        isSelected: isSelected,
        onTap: onTap,
        child: Row(
          children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(color: bgColor, shape: BoxShape.circle),
              child: Icon(icon, color: iconColor),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(level.label, style: AppTextStyles.h3),
                  Text(level.description, style: AppTextStyles.bodyMedium),
                ],
              ),
            ),
            _CheckCircle(selected: isSelected),
          ],
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
