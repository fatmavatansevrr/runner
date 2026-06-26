import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/theme/app_radius.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../data/onboarding_provider.dart';

class RaceDetailsPage extends ConsumerStatefulWidget {
  const RaceDetailsPage({super.key});

  @override
  ConsumerState<RaceDetailsPage> createState() => _RaceDetailsPageState();
}

class _RaceDetailsPageState extends ConsumerState<RaceDetailsPage> {
  final _nameController = TextEditingController();
  final _distanceController = TextEditingController();
  DateTime? _raceDate;
  String _unit = 'km';

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    _nameController.text = state.raceName ?? '';
    _unit = state.unit;
    _distanceController.text = _mapEnumToDistanceText(state.goalDistance);
    if (state.raceDate != null) {
      _raceDate = DateTime.tryParse(state.raceDate!);
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _distanceController.dispose();
    super.dispose();
  }

  String _mapEnumToDistanceText(String val) {
    return switch (val) {
      'five_k' => '5.0',
      'ten_k' => '10.0',
      'half_marathon' => '21.1',
      'marathon' => '42.2',
      _ => '10.0',
    };
  }

  String _mapDistanceTextToEnum(String text) {
    final val = double.tryParse(text);
    if (val == null) return 'five_k';
    if ((val - 5.0).abs() < 0.2) return 'five_k';
    if ((val - 10.0).abs() < 0.2) return 'ten_k';
    if ((val - 21.1).abs() < 0.2) return 'half_marathon';
    if ((val - 42.2).abs() < 0.2) return 'marathon';
    return 'custom';
  }

  void _onContinue() {
    final name = _nameController.text.trim();
    final dateStr = _raceDate != null ? _raceDate!.toIso8601String().split('T')[0] : null;
    final distText = _distanceController.text.trim();
    final selectedEnum = _mapDistanceTextToEnum(distText);

    ref.read(onboardingProvider.notifier).updateGoalDistance(selectedEnum);
    ref.read(onboardingProvider.notifier).updateUnit(_unit);
    if (name.isNotEmpty && dateStr != null) {
      ref.read(onboardingProvider.notifier).updateRaceDetails(name, dateStr);
    }

    context.go(AppRoutes.runningBackground);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background, // Matches the reference background
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
                      'Race details',
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
                          onPressed: () => context.go(AppRoutes.goalSelection),
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: ClipRRect(
                            borderRadius: BorderRadius.circular(100),
                            child: const LinearProgressIndicator(
                              value: 0.3,
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
                      'Enter Race Details',
                      style: AppTextStyles.h1.copyWith(
                        fontSize: 28,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      'Tell us about your goal so we can tailor the plan.',
                      style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Race Name Field
                    Text(
                      'RACE NAME',
                      style: AppTextStyles.labelPrimary.copyWith(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    TextField(
                      controller: _nameController,
                      decoration: const InputDecoration(
                        hintText: 'e.g. New York Marathon',
                        prefixIcon: Icon(Icons.emoji_events_outlined, color: AppColors.textMuted),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    // Race Date Picker Field
                    Text(
                      'RACE DATE',
                      style: AppTextStyles.labelPrimary.copyWith(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    InkWell(
                      onTap: () async {
                        final date = await showDatePicker(
                          context: context,
                          initialDate: DateTime.now().add(const Duration(days: 30)),
                          firstDate: DateTime.now(),
                          lastDate: DateTime.now().add(const Duration(days: 365)),
                        );
                        if (date != null) {
                          setState(() => _raceDate = date);
                        }
                      },
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: 16),
                        decoration: BoxDecoration(
                          color: AppColors.surface,
                          border: Border.all(color: AppColors.border),
                          borderRadius: BorderRadius.circular(AppRadius.inputField),
                        ),
                        child: Row(
                          children: [
                            const Icon(Icons.calendar_today_rounded, size: 18, color: AppColors.textMuted),
                            const SizedBox(width: AppSpacing.md),
                            Expanded(
                              child: Text(
                                _raceDate == null
                                    ? 'e.g. 24 Jan 2026'
                                    : '${_raceDate!.day} ${_raceDate!.month == 1 ? "Jan" : _raceDate!.month == 2 ? "Feb" : _raceDate!.month == 3 ? "Mar" : _raceDate!.month == 4 ? "Apr" : _raceDate!.month == 5 ? "May" : _raceDate!.month == 6 ? "Jun" : _raceDate!.month == 7 ? "Jul" : _raceDate!.month == 8 ? "Aug" : _raceDate!.month == 9 ? "Sep" : _raceDate!.month == 10 ? "Oct" : _raceDate!.month == 11 ? "Nov" : "Dec"} ${_raceDate!.year}',
                                style: AppTextStyles.bodyMedium.copyWith(
                                  color: _raceDate == null ? AppColors.textMuted : AppColors.textPrimary,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    // Race Distance Field
                    Text(
                      'RACE DISTANCE',
                      style: AppTextStyles.labelPrimary.copyWith(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Row(
                      children: [
                        Expanded(
                          child: TextField(
                            controller: _distanceController,
                            keyboardType: const TextInputType.numberWithOptions(decimal: true),
                            decoration: const InputDecoration(
                              hintText: 'e.g. 42.2',
                              prefixIcon: Icon(Icons.square_foot_rounded, color: AppColors.textMuted),
                            ),
                          ),
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Container(
                          height: 48,
                          padding: const EdgeInsets.all(4),
                          decoration: BoxDecoration(
                            color: Colors.grey.shade100,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: AppColors.border),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              GestureDetector(
                                onTap: () => setState(() => _unit = 'km'),
                                child: Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                                  decoration: BoxDecoration(
                                    color: _unit == 'km' ? AppColors.primary : Colors.transparent,
                                    borderRadius: BorderRadius.circular(8),
                                  ),
                                  child: Text(
                                    'KM',
                                    style: TextStyle(
                                      color: _unit == 'km' ? Colors.white : AppColors.textSecondary,
                                      fontWeight: FontWeight.bold,
                                      fontSize: 12,
                                    ),
                                  ),
                                ),
                              ),
                              GestureDetector(
                                onTap: () => setState(() => _unit = 'mile'),
                                child: Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                                  decoration: BoxDecoration(
                                    color: _unit == 'mile' ? AppColors.primary : Colors.transparent,
                                    borderRadius: BorderRadius.circular(8),
                                  ),
                                  child: Text(
                                    'Mile',
                                    style: TextStyle(
                                      color: _unit == 'mile' ? Colors.white : AppColors.textSecondary,
                                      fontWeight: FontWeight.bold,
                                      fontSize: 12,
                                    ),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
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
