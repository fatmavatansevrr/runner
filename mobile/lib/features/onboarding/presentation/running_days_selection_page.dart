import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/routing/app_router.dart';

class RunningDaysSelectionPage extends ConsumerStatefulWidget {
  const RunningDaysSelectionPage({super.key});

  @override
  ConsumerState<RunningDaysSelectionPage> createState() => _RunningDaysSelectionPageState();
}

class _RunningDaysSelectionPageState extends ConsumerState<RunningDaysSelectionPage> {
  final List<String> _selectedDays = [];

  final List<(String, String)> _days = const [
    ('monday', 'Monday'),
    ('tuesday', 'Tuesday'),
    ('wednesday', 'Wednesday'),
    ('thursday', 'Thursday'),
    ('friday', 'Friday'),
    ('saturday', 'Saturday'),
    ('sunday', 'Sunday'),
  ];

  void _toggleDay(String key) {
    setState(() {
      if (_selectedDays.contains(key)) {
        _selectedDays.remove(key);
      } else {
        _selectedDays.add(key);
      }
    });
  }

  void _onContinue() {
    // If user selects running days, make sure the number matches or is close to daysPerWeek.
    // For simplicity, we proceed to Long Run Day Preference.
    context.push(AppRoutes.longRunDay);
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
                value: 0.7,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('Which days do you\nprefer to run?', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Select your preferred days. We will generate runs on these days.',
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              Expanded(
                child: ListView.separated(
                  itemCount: _days.length,
                  separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
                  itemBuilder: (context, index) {
                    final day = _days[index];
                    final isSelected = _selectedDays.contains(day.$1);
                    return SelectableCard(
                      isSelected: isSelected,
                      onTap: () => _toggleDay(day.$1),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(day.$2, style: AppTextStyles.h3),
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
                    );
                  },
                ),
              ),

              const SizedBox(height: AppSpacing.md),

              AppPrimaryButton(
                label: 'Continue',
                onPressed: _selectedDays.isEmpty ? null : _onContinue,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
