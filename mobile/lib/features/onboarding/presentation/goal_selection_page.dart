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

class GoalSelectionPage extends ConsumerStatefulWidget {
  const GoalSelectionPage({super.key});

  @override
  ConsumerState<GoalSelectionPage> createState() => _GoalSelectionPageState();
}

class _GoalSelectionPageState extends ConsumerState<GoalSelectionPage> {
  String? _selected; // "habit" | "race"

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: AppSpacing.sm),
              LinearProgressIndicator(
                value: 0.1,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('What are you\nplanning for?', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xl),

              // Habit card
              SelectableCard(
                isSelected: _selected == 'habit',
                onTap: () => setState(() => _selected = 'habit'),
                child: Row(
                  children: [
                    Container(
                      width: 48,
                      height: 48,
                      decoration: BoxDecoration(
                        color: AppColors.intervalTint,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Icon(Icons.favorite_rounded, color: Colors.pinkAccent),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Build a running habit', style: AppTextStyles.h3),
                          const SizedBox(height: 2),
                          Text('Run consistently without pressure', style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                    _CheckCircle(selected: _selected == 'habit'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Race card
              SelectableCard(
                isSelected: _selected == 'race',
                onTap: () => setState(() => _selected = 'race'),
                child: Row(
                  children: [
                    Container(
                      width: 48,
                      height: 48,
                      decoration: BoxDecoration(
                        color: AppColors.primaryLight,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Icon(Icons.emoji_events_rounded, color: AppColors.primary),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Train for a race', style: AppTextStyles.h3),
                          const SizedBox(height: 2),
                          Text('Prepare for a 5k, 10k or Marathon', style: AppTextStyles.bodyMedium),
                        ],
                      ),
                    ),
                    _CheckCircle(selected: _selected == 'race'),
                  ],
                ),
              ),

              const Spacer(),

              AppPrimaryButton(
                label: 'Continue',
                onPressed: _selected == null
                    ? null
                    : () {
                        if (_selected == 'habit') {
                          ref.read(onboardingProvider.notifier).updateGoalType('habit');
                          ref.read(onboardingProvider.notifier).updateGoalDistance('five_k'); // fallback
                          context.go(AppRoutes.habitGoal);
                        } else {
                          ref.read(onboardingProvider.notifier).updateGoalType('race');
                          context.go(AppRoutes.raceDetails);
                        }
                      },
              ),
              const SizedBox(height: AppSpacing.md),
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
