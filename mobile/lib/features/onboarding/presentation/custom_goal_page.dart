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

class CustomGoalPage extends ConsumerStatefulWidget {
  const CustomGoalPage({super.key});

  @override
  ConsumerState<CustomGoalPage> createState() => _CustomGoalPageState();
}

class _CustomGoalPageState extends ConsumerState<CustomGoalPage> {
  int _distanceKm = 5;
  String _focusType = 'finish'; // 'finish' | 'steady' | 'time'
  int _timeMins = 50;

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    _distanceKm = state.customGoalDistance;
    _focusType = state.customGoalType;
    _timeMins = state.customGoalTime;
  }

  void _onContinue() {
    ref.read(onboardingProvider.notifier).updateCustomGoalDetails(
      distance: _distanceKm,
      type: _focusType,
      time: _timeMins,
    );
    ref.read(onboardingProvider.notifier).updateGoalDistance('custom');
    if (_focusType == 'time') {
      context.go(AppRoutes.customGoalWithTime);
    } else {
      context.go(AppRoutes.weeklyFrequency);
    }
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
                          onPressed: () => context.go(AppRoutes.habitGoal),
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: ClipRRect(
                            borderRadius: BorderRadius.circular(100),
                            child: const LinearProgressIndicator(
                              value: 0.55,
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
                      'Customize your plan',
                      style: AppTextStyles.h1.copyWith(
                        fontSize: 28,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      'Set your specific distance and training target.',
                      style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Step 1: Set distance
                    Text(
                      '1. Choose a target distance',
                      style: AppTextStyles.h3.copyWith(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    _buildStepper(
                      value: _distanceKm,
                      unit: 'km',
                      onDecrement: () {
                        if (_distanceKm > 1) {
                          setState(() => _distanceKm--);
                        }
                      },
                      onIncrement: () => setState(() => _distanceKm++),
                      disableDecrement: _distanceKm <= 1,
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Step 2: Choose focus
                    Text(
                      '2. Select your focus',
                      style: AppTextStyles.h3.copyWith(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    SelectableCard(
                      isSelected: _focusType == 'finish',
                      onTap: () => setState(() => _focusType = 'finish'),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Finish the distance', style: AppTextStyles.h3),
                                const SizedBox(height: 2),
                                Text('Focus on completing the distance safely.', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                              ],
                            ),
                          ),
                          const SizedBox(width: AppSpacing.md),
                          _CheckCircle(selected: _focusType == 'finish'),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    SelectableCard(
                      isSelected: _focusType == 'steady',
                      onTap: () => setState(() => _focusType = 'steady'),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Run steady pace', style: AppTextStyles.h3),
                                const SizedBox(height: 2),
                                Text('Focus on maintaining a comfortable, steady pace.', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                              ],
                            ),
                          ),
                          const SizedBox(width: AppSpacing.md),
                          _CheckCircle(selected: _focusType == 'steady'),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    SelectableCard(
                      isSelected: _focusType == 'time',
                      onTap: () => setState(() => _focusType = 'time'),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Finish under a time', style: AppTextStyles.h3),
                                const SizedBox(height: 2),
                                Text('Try to run the distance within a specific time limit.', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                              ],
                            ),
                          ),
                          const SizedBox(width: AppSpacing.md),
                          _CheckCircle(selected: _focusType == 'time'),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xl),


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

