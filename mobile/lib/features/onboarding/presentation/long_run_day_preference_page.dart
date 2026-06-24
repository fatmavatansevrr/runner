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

class LongRunDayPreferencePage extends ConsumerStatefulWidget {
  const LongRunDayPreferencePage({super.key});

  @override
  ConsumerState<LongRunDayPreferencePage> createState() => _LongRunDayPreferencePageState();
}

class _LongRunDayPreferencePageState extends ConsumerState<LongRunDayPreferencePage> {
  String? _selectedDay;

  final List<(String, String)> _options = const [
    ('saturday', 'Saturday'),
    ('sunday', 'Sunday'),
  ];

  void _onContinue() {
    if (_selectedDay == null) return;
    ref.read(onboardingProvider.notifier).updateLongRunDay(_selectedDay!);
    context.push(AppRoutes.startDate);
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
                value: 0.8,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('Choose your long run day', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'A long run is the focal point of the week. Most runners prefer weekends.',
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              ..._options.map((option) {
                final isSelected = _selectedDay == option.$1;
                return Padding(
                  padding: const EdgeInsets.only(bottom: AppSpacing.md),
                  child: SelectableCard(
                    isSelected: isSelected,
                    onTap: () => setState(() => _selectedDay = option.$1),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(option.$2, style: AppTextStyles.h3),
                        AnimatedContainer(
                          duration: const Duration(milliseconds: 150),
                          width: 24,
                          height: 24,
                          decoration: BoxDecoration(
                            color: isSelected ? AppColors.primary : Colors.transparent,
                            shape: BoxShape.circle,
                            border: Border.all(
                              color: isSelected ? AppColors.primary : AppColors.border,
                              width: 2,
                            ),
                          ),
                          child: isSelected
                              ? const Icon(Icons.check, color: Colors.white, size: 14)
                              : null,
                        ),
                      ],
                    ),
                  ),
                );
              }),

              const Spacer(),

              AppPrimaryButton(
                label: 'Continue',
                onPressed: _selectedDay == null ? null : _onContinue,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
