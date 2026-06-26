import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../data/onboarding_provider.dart';

class CustomGoalWithTimePage extends ConsumerStatefulWidget {
  const CustomGoalWithTimePage({super.key});

  @override
  ConsumerState<CustomGoalWithTimePage> createState() => _CustomGoalWithTimePageState();
}

class _CustomGoalWithTimePageState extends ConsumerState<CustomGoalWithTimePage> {
  int _timeMins = 50;

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    _timeMins = state.customGoalTime;
  }

  void _onContinue() {
    ref.read(onboardingProvider.notifier).updateCustomGoalDetails(
      time: _timeMins,
    );
    context.go(AppRoutes.weeklyFrequency);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Top category label
                    Text(
                      'Custom Goal',
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
                          onPressed: () => context.go(AppRoutes.customGoal),
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: ClipRRect(
                            borderRadius: BorderRadius.circular(100),
                            child: const LinearProgressIndicator(
                              value: 0.58,
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
                      'Choose a target time',
                      style: AppTextStyles.h1.copyWith(
                        fontSize: 28,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      'Select how many minutes you want to finish the distance in.',
                      style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: AppSpacing.xxl),

                    // Target time stepper
                    _buildStepper(
                      value: _timeMins,
                      unit: 'min',
                      onDecrement: () {
                        if (_timeMins > 5) {
                          setState(() => _timeMins -= 5);
                        }
                      },
                      onIncrement: () => setState(() => _timeMins += 5),
                      disableDecrement: _timeMins <= 5,
                    ),
                  ],
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: AppPrimaryButton(
                label: 'Continue',
                icon: Icons.arrow_forward_rounded,
                onPressed: _onContinue,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStepper({
    required int value,
    required String unit,
    required VoidCallback onDecrement,
    required VoidCallback onIncrement,
    bool disableDecrement = false,
  }) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        GestureDetector(
          onTap: disableDecrement ? null : onDecrement,
          child: Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: disableDecrement ? AppColors.border.withOpacity(0.5) : AppColors.surface,
              border: Border.all(color: AppColors.border),
            ),
            child: Icon(Icons.remove, color: disableDecrement ? AppColors.textSecondary.withOpacity(0.5) : AppColors.textPrimary),
          ),
        ),
        const SizedBox(width: AppSpacing.xl),
        Container(
          constraints: const BoxConstraints(minWidth: 100),
          alignment: Alignment.center,
          child: RichText(
            text: TextSpan(
              style: AppTextStyles.h1.copyWith(fontSize: 32, fontWeight: FontWeight.w800),
              children: [
                TextSpan(text: '$value'),
                TextSpan(
                  text: ' $unit',
                  style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary, fontWeight: FontWeight.normal),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(width: AppSpacing.xl),
        GestureDetector(
          onTap: onIncrement,
          child: Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: AppColors.surface,
              border: Border.all(color: AppColors.border),
            ),
            child: const Icon(Icons.add, color: AppColors.textPrimary),
          ),
        ),
      ],
    );
  }
}
