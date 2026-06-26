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

  final Map<String, String> _dayLabels = const {
    'monday': 'Monday',
    'tuesday': 'Tuesday',
    'wednesday': 'Wednesday',
    'thursday': 'Thursday',
    'friday': 'Friday',
    'saturday': 'Saturday',
    'sunday': 'Sunday',
  };

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    final savedDay = state.longRunDay.toLowerCase();
    final selectedDays = state.selectedRunningDays;
    
    if (selectedDays.contains(savedDay)) {
      _selectedDay = savedDay;
    } else if (selectedDays.isNotEmpty) {
      _selectedDay = selectedDays.first;
    } else {
      _selectedDay = 'sunday';
    }
  }

  void _onContinue() {
    if (_selectedDay == null) return;
    // Capitalize the first letter
    final capitalized = _selectedDay![0].toUpperCase() + _selectedDay!.substring(1);
    ref.read(onboardingProvider.notifier).updateLongRunDay(capitalized);
    context.go(AppRoutes.startDate);
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(onboardingProvider);
    final selectedDays = state.selectedRunningDays;
    final List<String> daysToShow = selectedDays.isNotEmpty ? selectedDays : ['saturday', 'sunday'];

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top category label
              Text(
                'Schedule',
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
                    onPressed: () => context.go(AppRoutes.runningDays),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(100),
                      child: const LinearProgressIndicator(
                        value: 0.85,
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
                'Choose your long run day',
                style: AppTextStyles.h1.copyWith(
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'A long run is the focal point of the week. Most runners prefer weekends.',
                style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              Expanded(
                child: ListView.separated(
                  itemCount: daysToShow.length,
                  physics: const NeverScrollableScrollPhysics(),
                  separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
                  itemBuilder: (context, index) {
                    final dayKey = daysToShow[index];
                    final isSelected = _selectedDay == dayKey;
                    final label = _dayLabels[dayKey] ?? dayKey;
                    return SelectableCard(
                      isSelected: isSelected,
                      onTap: () => setState(() => _selectedDay = dayKey),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(label, style: AppTextStyles.h3),
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
                icon: Icons.arrow_forward_rounded,
                onPressed: _selectedDay == null ? null : _onContinue,
              ),
              const SizedBox(height: AppSpacing.xs),
            ],
          ),
        ),
      ),
    );
  }
}
