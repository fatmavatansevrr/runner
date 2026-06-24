import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
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
  DateTime? _raceDate;
  String _selectedDistance = 'five_k';
  final _timeController = TextEditingController();

  @override
  void dispose() {
    _nameController.dispose();
    _timeController.dispose();
    super.dispose();
  }

  void _onContinue() {
    final name = _nameController.text.trim();
    final dateStr = _raceDate != null ? _raceDate!.toIso8601String().split('T')[0] : null;
    final durationMins = int.tryParse(_timeController.text.trim());
    final durationSeconds = durationMins != null ? durationMins * 60 : null;

    ref.read(onboardingProvider.notifier).updateGoalDistance(_selectedDistance);
    if (name.isNotEmpty && dateStr != null) {
      ref.read(onboardingProvider.notifier).updateRaceDetails(name, dateStr);
    }
    if (durationSeconds != null) {
      ref.read(onboardingProvider.notifier).updateTargetFinishTime(durationSeconds);
    }

    context.push(AppRoutes.runningBackground);
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
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    LinearProgressIndicator(
                      value: 0.3,
                      backgroundColor: AppColors.border,
                      color: AppColors.primary,
                      borderRadius: BorderRadius.circular(2),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    Text('Tell us about\nyour race', style: AppTextStyles.h1),
                    const SizedBox(height: AppSpacing.xl),

                    // Race Name
                    TextField(
                      controller: _nameController,
                      decoration: const InputDecoration(
                        labelText: 'Race Name',
                        hintText: 'e.g. London Marathon',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    // Race Date Picker
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
                          border: Border.all(color: AppColors.border),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              _raceDate == null
                                  ? 'Race Date'
                                  : '${_raceDate!.day}/${_raceDate!.month}/${_raceDate!.year}',
                              style: AppTextStyles.bodyMedium.copyWith(
                                color: _raceDate == null ? AppColors.textMuted : AppColors.textPrimary,
                              ),
                            ),
                            const Icon(Icons.calendar_today_rounded, size: 18, color: AppColors.textSecondary),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    // Race Distance
                    DropdownButtonFormField<String>(
                      value: _selectedDistance,
                      decoration: const InputDecoration(
                        labelText: 'Target Distance',
                        border: OutlineInputBorder(),
                      ),
                      items: const [
                        DropdownMenuItem(value: 'five_k', child: Text('5 km (5K)')),
                        DropdownMenuItem(value: 'ten_k', child: Text('10 km (10K)')),
                        DropdownMenuItem(value: 'half_marathon', child: Text('Half Marathon (21.1 km)')),
                        DropdownMenuItem(value: 'marathon', child: Text('Marathon (42.2 km)')),
                      ],
                      onChanged: (val) {
                        if (val != null) {
                          setState(() => _selectedDistance = val);
                        }
                      },
                    ),
                    const SizedBox(height: AppSpacing.md),

                    // Target finish time (optional)
                    TextField(
                      controller: _timeController,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Target Finish Time (Minutes, Optional)',
                        hintText: 'e.g. 120',
                        border: OutlineInputBorder(),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: AppPrimaryButton(
                label: 'Continue',
                onPressed: _onContinue,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
