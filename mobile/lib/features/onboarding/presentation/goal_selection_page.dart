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
      backgroundColor: AppColors.background, // Off-white background
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: AppSpacing.sm),
              // Linear progress indicator
              ClipRRect(
                borderRadius: BorderRadius.circular(100),
                child: const LinearProgressIndicator(
                  value: 0.2,
                  backgroundColor: AppColors.border,
                  color: AppColors.primary,
                  minHeight: 6,
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Back button below progress bar
              IconButton(
                icon: const Icon(Icons.arrow_back_rounded, color: AppColors.textPrimary),
                padding: EdgeInsets.zero,
                constraints: const BoxConstraints(),
                onPressed: () => context.go(AppRoutes.introCarousel),
              ),
              const SizedBox(height: AppSpacing.lg),

              Text('What are you\nplanning for?', style: AppTextStyles.h1.copyWith(fontSize: 28, fontWeight: FontWeight.w800, letterSpacing: -0.5)),
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
                        color: AppColors.primary, // Solid blue matching what-is-your-plan.png
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Icon(Icons.emoji_events_rounded, color: Colors.white), // White trophy icon
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
                          context.go(AppRoutes.runningBackground); // Habits flow now goes to RunningBackgroundPage
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
