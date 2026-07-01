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

class WeeklyFrequencyPage extends ConsumerStatefulWidget {
  const WeeklyFrequencyPage({super.key});

  @override
  ConsumerState<WeeklyFrequencyPage> createState() => _WeeklyFrequencyPageState();
}

class _WeeklyFrequencyPageState extends ConsumerState<WeeklyFrequencyPage> {
  int? _selected;

  void _onContinue() {
    if (_selected == null) return;
    ref.read(onboardingProvider.notifier).updateDaysPerWeek(_selected!);
    context.push(AppRoutes.runningDays);
  }

  Color _resolveTint(int days) {
    if (days <= 3) return AppColors.easyRunTint;
    if (days <= 5) return AppColors.longRunTint;
    return AppColors.intervalTint;
  }

  Color _resolveTextColor(int days) {
    if (days <= 3) return AppColors.primary;
    if (days <= 5) return Colors.deepPurple;
    return Colors.red;
  }

  String _resolveSubtitle(int days) {
    switch (days) {
      case 1:
        return 'Best for very busy schedules.';
      case 2:
        return 'Good for maintaining baseline fitness.';
      case 3:
        return 'Best for beginners or building habits.';
      case 4:
        return 'Recommended if you already run regularly.';
      case 5:
        return 'For active runners aiming for higher volume.';
      case 6:
        return 'Advanced training frequency.';
      case 7:
        return 'Daily running routine.';
      default:
        return '';
    }
  }

  @override
  Widget build(BuildContext context) {
    final onboardingState = ref.watch(onboardingProvider);
    final isRace = onboardingState.goalType == 'race';
    final options = isRace ? [2, 3, 4, 5, 6] : [1, 2, 3, 4, 5, 6, 7];

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
                value: 0.6,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('How many days\nper week?', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xs),
              const SizedBox(height: AppSpacing.xl),

              Expanded(
                child: SingleChildScrollView(
                  child: Column(
                    children: options.map((days) {
                      return Padding(
                        padding: const EdgeInsets.only(bottom: AppSpacing.md),
                        child: SelectableCard(
                          isSelected: _selected == days,
                          onTap: () => setState(() => _selected = days),
                          child: Row(
                            children: [
                              Container(
                                width: 40,
                                height: 40,
                                decoration: BoxDecoration(
                                  color: _resolveTint(days),
                                  shape: BoxShape.circle,
                                ),
                                child: Center(
                                  child: Text(
                                    '$days',
                                    style: TextStyle(
                                      fontWeight: FontWeight.bold,
                                      color: _resolveTextColor(days),
                                    ),
                                  ),
                                ),
                              ),
                              const SizedBox(width: AppSpacing.md),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text('$days days per week', style: AppTextStyles.h3),
                                    Text(_resolveSubtitle(days), style: AppTextStyles.bodyMedium),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    }).toList(),
                  ),
                ),
              ),

              const SizedBox(height: AppSpacing.md),

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
