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

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    _selectedDays.addAll(state.selectedRunningDays);
  }

  void _toggleDay(String key, int limit) {
    setState(() {
      if (_selectedDays.contains(key)) {
        _selectedDays.remove(key);
      } else {
        if (_selectedDays.length < limit) {
          _selectedDays.add(key);
        } else {
          // Replace the first selected day, or show warning. Let's just remove the first one to make it easy to swap.
          _selectedDays.removeAt(0);
          _selectedDays.add(key);
        }
      }
    });
  }

  void _onContinue() {
    ref.read(onboardingProvider.notifier).updateSelectedRunningDays(_selectedDays);
    final state = ref.read(onboardingProvider);
    if (state.goalType == 'race') {
      context.go(AppRoutes.longRunDay);
    } else {
      context.go(AppRoutes.startDate);
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(onboardingProvider);
    final limit = state.daysPerWeek;
    final isSelectionValid = _selectedDays.length == limit;

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top category label
              Padding(
                padding: const EdgeInsets.only(left: 16.0),
                child: Text(
                  'Schedule',
                  style: AppTextStyles.label.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
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
                    onPressed: () {
                      if (state.goalType == 'race') {
                        context.go(AppRoutes.weeklyFrequency);
                      } else {
                        context.go(AppRoutes.preferredDuration);
                      }
                    },
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(100),
                      child: const LinearProgressIndicator(
                        value: 0.8,
                        backgroundColor: AppColors.border,
                        color: AppColors.primary,
                        minHeight: 6,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.xl),

              Padding(
                padding: const EdgeInsets.only(left: 16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Which days do you\nprefer to run?',
                      style: AppTextStyles.h1.copyWith(
                        fontSize: 28,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      'Select exactly $limit days for your plan.',
                      style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.xl),

              Expanded(
                child: ListView.separated(
                  itemCount: _days.length,
                  physics: const NeverScrollableScrollPhysics(),
                  separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.sm),
                  itemBuilder: (context, index) {
                    final day = _days[index];
                    final isSelected = _selectedDays.contains(day.$1);
                    return SelectableCard(
                      isSelected: isSelected,
                      onTap: () => _toggleDay(day.$1, limit),
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
                icon: Icons.arrow_forward_rounded,
                onPressed: isSelectionValid ? _onContinue : null,
              ),
              const SizedBox(height: AppSpacing.xs),
            ],
          ),
        ),
      ),
    );
  }
}

