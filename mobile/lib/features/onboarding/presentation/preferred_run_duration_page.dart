import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../data/onboarding_provider.dart';

class PreferredRunDurationPage extends ConsumerStatefulWidget {
  const PreferredRunDurationPage({super.key});

  @override
  ConsumerState<PreferredRunDurationPage> createState() => _PreferredRunDurationPageState();
}

class _PreferredRunDurationPageState extends ConsumerState<PreferredRunDurationPage> {
  int _selectedDuration = 30;
  late FixedExtentScrollController _scrollController;

  // List of possible durations (10 to 120 minutes)
  final List<int> _durations = List.generate(111, (index) => index + 10);

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    _selectedDuration = state.preferredRunDuration;
    if (!_durations.contains(_selectedDuration)) {
      _selectedDuration = 30;
    }
    final initialIndex = _durations.indexOf(_selectedDuration);
    _scrollController = FixedExtentScrollController(initialItem: initialIndex);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onContinue() {
    ref.read(onboardingProvider.notifier).updatePreferredRunDuration(_selectedDuration);
    context.go(AppRoutes.runningDays);
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
                    // Top Category label
                    Text(
                      'Duration',
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
                          onPressed: () => context.go(AppRoutes.weeklyFrequency),
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: ClipRRect(
                            borderRadius: BorderRadius.circular(100),
                            child: const LinearProgressIndicator(
                              value: 0.7,
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
                      "How much time to run?",
                      style: AppTextStyles.h1.copyWith(
                        fontSize: 28,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      'Select your preferred daily target duration.',
                      style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Spinner
                    Center(
                      child: Container(
                        height: 180,
                        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
                        decoration: BoxDecoration(
                          color: AppColors.surface,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: Stack(
                          alignment: Alignment.center,
                          children: [
                            // Selection overlay box
                            Container(
                              height: 48,
                              decoration: BoxDecoration(
                                color: AppColors.primaryLight.withOpacity(0.15),
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(color: AppColors.primary.withOpacity(0.2), width: 1),
                              ),
                            ),
                            ListWheelScrollView.useDelegate(
                              controller: _scrollController,
                              itemExtent: 36,
                              perspective: 0.005,
                              diameterRatio: 1.2,
                              physics: const FixedExtentScrollPhysics(),
                              onSelectedItemChanged: (index) {
                                setState(() {
                                  _selectedDuration = _durations[index];
                                });
                              },
                              childDelegate: ListWheelChildBuilderDelegate(
                                childCount: _durations.length,
                                builder: (context, index) {
                                  final duration = _durations[index];
                                  final isSelected = duration == _selectedDuration;
                                  return Center(
                                    child: Text(
                                      '$duration min',
                                      style: TextStyle(
                                        fontSize: isSelected ? 24 : 18,
                                        fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                                        color: isSelected ? AppColors.primary : AppColors.textSecondary.withOpacity(0.5),
                                      ),
                                    ),
                                  );
                                },
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Info box / Gear settings warning
                    Container(
                      padding: const EdgeInsets.all(AppSpacing.md),
                      decoration: BoxDecoration(
                        color: AppColors.surface,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: AppColors.border),
                      ),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(Icons.settings_suggest_rounded, color: AppColors.primary, size: 24),
                          const SizedBox(width: AppSpacing.md),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Adjust Anytime',
                                  style: AppTextStyles.h3.copyWith(
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  'Your target duration controls the volume of your training days. You can easily modify this in your profile settings later.',
                                  style: AppTextStyles.bodyMedium.copyWith(
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.lg),
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
}
