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

class StartDateSelectionPage extends ConsumerStatefulWidget {
  const StartDateSelectionPage({super.key});

  @override
  ConsumerState<StartDateSelectionPage> createState() => _StartDateSelectionPageState();
}

class _StartDateSelectionPageState extends ConsumerState<StartDateSelectionPage> {
  String? _selected; // "today" | "next_monday" | "custom"
  DateTime? _customDate;

  DateTime _getNextMonday() {
    final now = DateTime.now();
    int daysToAdd = (DateTime.monday - now.weekday + 7) % 7;
    if (daysToAdd == 0) daysToAdd = 7;
    return now.add(Duration(days: daysToAdd));
  }

  void _onGeneratePlan() {
    DateTime startDate;
    if (_selected == 'today') {
      startDate = DateTime.now();
    } else if (_selected == 'next_monday') {
      startDate = _getNextMonday();
    } else {
      startDate = _customDate ?? DateTime.now();
    }

    ref.read(onboardingProvider.notifier).updateStartDate(startDate);
    context.push(AppRoutes.planGeneration);
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
                value: 0.9,
                backgroundColor: AppColors.border,
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
              const SizedBox(height: AppSpacing.xl),

              Text('When would you\nlike to start?', style: AppTextStyles.h1),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Starting on a Monday is highly recommended to align with standard weekly training cycles.',
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              // Today
              SelectableCard(
                isSelected: _selected == 'today',
                onTap: () => setState(() => _selected = 'today'),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Today', style: AppTextStyles.h3),
                        Text('Start training immediately.', style: AppTextStyles.bodyMedium),
                      ],
                    ),
                    _CheckCircle(selected: _selected == 'today'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Next Monday
              SelectableCard(
                isSelected: _selected == 'next_monday',
                onTap: () => setState(() => _selected = 'next_monday'),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Next Monday', style: AppTextStyles.h3),
                        Text('Recommended start date.', style: AppTextStyles.bodyMedium),
                      ],
                    ),
                    _CheckCircle(selected: _selected == 'next_monday'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // Custom date
              SelectableCard(
                isSelected: _selected == 'custom',
                onTap: () async {
                  setState(() => _selected = 'custom');
                  final date = await showDatePicker(
                    context: context,
                    initialDate: DateTime.now(),
                    firstDate: DateTime.now(),
                    lastDate: DateTime.now().add(const Duration(days: 30)),
                  );
                  if (date != null) {
                    setState(() => _customDate = date);
                  }
                },
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          _customDate == null
                              ? 'Choose custom date'
                              : 'Start date: ${_customDate!.day}/${_customDate!.month}/${_customDate!.year}',
                          style: AppTextStyles.h3,
                        ),
                        Text('Select your preferred day.', style: AppTextStyles.bodyMedium),
                      ],
                    ),
                    _CheckCircle(selected: _selected == 'custom'),
                  ],
                ),
              ),

              const Spacer(),

              AppPrimaryButton(
                label: 'Generate Plan',
                onPressed: _selected == null || (_selected == 'custom' && _customDate == null)
                    ? null
                    : _onGeneratePlan,
              ),
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
