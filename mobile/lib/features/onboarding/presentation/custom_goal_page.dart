import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../data/onboarding_provider.dart';

class CustomGoalPage extends ConsumerStatefulWidget {
  const CustomGoalPage({super.key});

  @override
  ConsumerState<CustomGoalPage> createState() => _CustomGoalPageState();
}

class _CustomGoalPageState extends ConsumerState<CustomGoalPage> {
  double _distanceKm = 5.0;
  double _durationMin = 30.0;
  String _mode = 'distance'; // 'distance' | 'duration'

  void _onContinue() {
    ref.read(onboardingProvider.notifier).updateGoalDistance('custom');
    if (_mode == 'distance') {
      // Custom distance km
      ref.read(onboardingProvider.notifier).updateTargetFinishTime((_distanceKm * 400).toInt()); // mock target pace time
    } else {
      // Custom duration min
      ref.read(onboardingProvider.notifier).updateTargetFinishTime((_durationMin * 60).toInt());
    }
    context.push(AppRoutes.weeklyFrequency);
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
                value: 0.55,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('Custom Goal', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.md),

              Row(
                children: [
                  ChoiceChip(
                    label: const Text('By Distance'),
                    selected: _mode == 'distance',
                    onSelected: (selected) {
                      if (selected) setState(() => _mode = 'distance');
                    },
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  ChoiceChip(
                    label: const Text('By Duration'),
                    selected: _mode == 'duration',
                    onSelected: (selected) {
                      if (selected) setState(() => _mode = 'duration');
                    },
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.xl),

              if (_mode == 'distance') ...[
                Text(
                  '${_distanceKm.toStringAsFixed(1)} km',
                  style: AppTextStyles.displayLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                Slider(
                  value: _distanceKm,
                  min: 1.0,
                  max: 50.0,
                  divisions: 98,
                  label: '${_distanceKm.toStringAsFixed(1)} km',
                  onChanged: (val) => setState(() => _distanceKm = val),
                ),
              ] else ...[
                Text(
                  '${_durationMin.toInt()} minutes',
                  style: AppTextStyles.displayLarge,
                ),
                const SizedBox(height: AppSpacing.md),
                Slider(
                  value: _durationMin,
                  min: 10.0,
                  max: 180.0,
                  divisions: 34,
                  label: '${_durationMin.toInt()} min',
                  onChanged: (val) => setState(() => _durationMin = val),
                ),
              ],

              const Spacer(),

              AppPrimaryButton(
                label: 'Continue',
                onPressed: _onContinue,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
