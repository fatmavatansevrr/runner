import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
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

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    if (state.startDate != null) {
      final now = DateTime.now();
      final nextMonday = _getNextMonday();
      if (DateUtils.isSameDay(state.startDate!, now)) {
        _selected = 'today';
      } else if (DateUtils.isSameDay(state.startDate!, nextMonday)) {
        _selected = 'next_monday';
      } else {
        _selected = 'custom';
        _customDate = state.startDate;
      }
    }
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
    context.go(AppRoutes.planGeneration);
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(onboardingProvider);
    final nextMonday = _getNextMonday();
    final formattedNextMonday = DateFormat('EEEE, MMM d').format(nextMonday);
    final formattedToday = DateFormat('EEEE, MMM d').format(DateTime.now());

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
                'Start Date',
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
                    onPressed: () {
                      if (state.goalType == 'race') {
                        context.go(AppRoutes.longRunDay);
                      } else {
                        context.go(AppRoutes.runningDays);
                      }
                    },
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(100),
                      child: const LinearProgressIndicator(
                        value: 0.95,
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
                'When would you\nlike to start?',
                style: AppTextStyles.h1.copyWith(
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Starting on a Monday is highly recommended to align with standard weekly training cycles.',
                style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              // 1. Next Monday (Recommended)
              SelectableCard(
                isSelected: _selected == 'next_monday',
                onTap: () => setState(() => _selected = 'next_monday'),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Text('Next Monday', style: AppTextStyles.h3),
                              const SizedBox(width: AppSpacing.xs),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                decoration: BoxDecoration(
                                  color: AppColors.primaryLight.withOpacity(0.2),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                                child: const Text(
                                  'REC',
                                  style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: AppColors.primary),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 2),
                          Text('Starts on $formattedNextMonday', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                        ],
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    _CheckCircle(selected: _selected == 'next_monday'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // 2. Today
              SelectableCard(
                isSelected: _selected == 'today',
                onTap: () => setState(() => _selected = 'today'),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Today', style: AppTextStyles.h3),
                          const SizedBox(height: 2),
                          Text('Starts today, $formattedToday', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                        ],
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    _CheckCircle(selected: _selected == 'today'),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),

              // 3. Custom date
              SelectableCard(
                isSelected: _selected == 'custom',
                onTap: () async {
                  setState(() => _selected = 'custom');
                  final date = await showDatePicker(
                    context: context,
                    initialDate: _customDate ?? DateTime.now(),
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
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            _customDate == null
                                ? 'Choose custom date'
                                : 'Start date: ${DateFormat('EEEE, MMM d').format(_customDate!)}',
                            style: AppTextStyles.h3,
                          ),
                          const SizedBox(height: 2),
                          Text('Pick a custom day to start training.', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
                        ],
                      ),
                    ),
                    const SizedBox(width: AppSpacing.md),
                    _CheckCircle(selected: _selected == 'custom'),
                  ],
                ),
              ),

              const Spacer(),

              AppPrimaryButton(
                label: 'Generate Plan',
                icon: Icons.arrow_forward_rounded,
                onPressed: _selected == null || (_selected == 'custom' && _customDate == null)
                    ? null
                    : _onGeneratePlan,
              ),
              const SizedBox(height: AppSpacing.xs),
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

