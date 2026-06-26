import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../data/onboarding_provider.dart';

class GoalTimePage extends ConsumerStatefulWidget {
  const GoalTimePage({super.key});

  @override
  ConsumerState<GoalTimePage> createState() => _GoalTimePageState();
}

class _GoalTimePageState extends ConsumerState<GoalTimePage> {
  int _hours = 3;
  int _minutes = 45;
  int _seconds = 0;

  late FixedExtentScrollController _hoursController;
  late FixedExtentScrollController _minutesController;
  late FixedExtentScrollController _secondsController;

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);
    if (state.targetFinishTimeSeconds != null) {
      final totalSec = state.targetFinishTimeSeconds!;
      _hours = totalSec ~/ 3600;
      _minutes = (totalSec % 3600) ~/ 60;
      _seconds = totalSec % 60;
    }
    _hoursController = FixedExtentScrollController(initialItem: _hours);
    _minutesController = FixedExtentScrollController(initialItem: _minutes);
    _secondsController = FixedExtentScrollController(initialItem: _seconds);
  }

  @override
  void dispose() {
    _hoursController.dispose();
    _minutesController.dispose();
    _secondsController.dispose();
    super.dispose();
  }

  double _getDistanceKm(OnboardingState state) {
    return switch (state.goalDistance) {
      'five_k' => 5.0,
      'ten_k' => 10.0,
      'half_marathon' => 21.1,
      'marathon' => 42.2,
      'custom' => state.customGoalDistance.toDouble(),
      _ => 10.0,
    };
  }

  String _calculatePace(double distanceKm) {
    final totalSeconds = (_hours * 3600) + (_minutes * 60) + _seconds;
    if (totalSeconds <= 0 || distanceKm <= 0) return '0:00';
    final paceSecondsPerKm = totalSeconds / distanceKm;
    final paceMin = (paceSecondsPerKm / 60).floor();
    final paceSec = (paceSecondsPerKm % 60).round();
    return "$paceMin:${paceSec.toString().padLeft(2, '0')}";
  }

  void _onContinue() {
    final totalSeconds = (_hours * 3600) + (_minutes * 60) + _seconds;
    ref.read(onboardingProvider.notifier).updateTargetFinishTime(totalSeconds);
    context.go(AppRoutes.weeklyFrequency);
  }

  void _onGoWithAverage(double distanceKm) {
    // Average finish times: Marathon = 4h 21m, Half Marathon = 2h 05m, 10K = 58m, 5K = 28m
    int targetSec = 0;
    if (distanceKm >= 42.0) {
      _hours = 4; _minutes = 21; _seconds = 0;
      targetSec = (4 * 3600) + (21 * 60);
    } else if (distanceKm >= 21.0) {
      _hours = 2; _minutes = 5; _seconds = 0;
      targetSec = (2 * 3600) + (5 * 60);
    } else if (distanceKm >= 10.0) {
      _hours = 0; _minutes = 58; _seconds = 0;
      targetSec = 58 * 60;
    } else {
      _hours = 0; _minutes = 28; _seconds = 0;
      targetSec = 28 * 60;
    }
    setState(() {
      _hoursController.jumpToItem(_hours);
      _minutesController.jumpToItem(_minutes);
      _secondsController.jumpToItem(_seconds);
    });
    ref.read(onboardingProvider.notifier).updateTargetFinishTime(targetSec);
  }

  @override
  Widget build(BuildContext context) {
    final onboarding = ref.watch(onboardingProvider);
    final distanceKm = _getDistanceKm(onboarding);
    final calculatedPace = _calculatePace(distanceKm);

    // Dynamic average label based on distance
    final String avgLabel = distanceKm >= 42.0
        ? 'Go with average pace (4h 21m)'
        : distanceKm >= 21.0
            ? 'Go with average pace (2h 05m)'
            : distanceKm >= 10.0
                ? 'Go with average pace (58m)'
                : 'Go with average pace (28m)';

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            // Top Back Button & Progress Indicator Row
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
              child: Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_rounded, color: AppColors.textPrimary),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                    onPressed: () => context.go(AppRoutes.runningBackground),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(100),
                      child: const LinearProgressIndicator(
                        value: 0.5,
                        backgroundColor: AppColors.border,
                        color: AppColors.primary,
                        minHeight: 6,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    const SizedBox(height: AppSpacing.md),
                    // Centered title and subtitle
                    Text(
                      "What's your goal time?",
                      textAlign: TextAlign.center,
                      style: AppTextStyles.h1.copyWith(
                        fontSize: 28,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      'A precise target helps us tailor your intensity.',
                      textAlign: TextAlign.center,
                      style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    // Compact time picker (no oversized outer card)
                    _buildTimePicker(),
                    const SizedBox(height: AppSpacing.xl),

                    // Compact yellow Pace Required card
                    _buildPaceRequiredCard(calculatedPace),
                    const SizedBox(height: AppSpacing.lg),

                    // Secondary average pace button below
                    _buildAveragePaceButton(distanceKm, avgLabel),
                    const SizedBox(height: AppSpacing.md),
                  ],
                ),
              ),
            ),

            // Bottom-aligned CTA
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

  Widget _buildTimePicker() {
    const labelStyle = TextStyle(
      fontSize: 11,
      fontWeight: FontWeight.w700,
      color: AppColors.textSecondary,
      letterSpacing: 0.5,
    );

    return Column(
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const SizedBox(
              width: 50,
              child: Text('HRS', style: labelStyle, textAlign: TextAlign.center),
            ),
            const SizedBox(width: 44),
            const SizedBox(
              width: 50,
              child: Text('MIN', style: labelStyle, textAlign: TextAlign.center),
            ),
            const SizedBox(width: 44),
            const SizedBox(
              width: 50,
              child: Text('SEC', style: labelStyle, textAlign: TextAlign.center),
            ),
          ],
        ),
        const SizedBox(height: 6),
        SizedBox(
          height: 110,
          child: Stack(
            alignment: Alignment.center,
            children: [
              // Two clean horizontal boundary lines for the selected item
              Container(
                height: 38,
                decoration: const BoxDecoration(
                  border: Border(
                    top: BorderSide(color: AppColors.border, width: 1.2),
                    bottom: BorderSide(color: AppColors.border, width: 1.2),
                  ),
                ),
              ),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  _buildWheel(
                    controller: _hoursController,
                    max: 12,
                    currentVal: _hours,
                    onChanged: (val) => setState(() => _hours = val),
                  ),
                  const SizedBox(
                    width: 44,
                    child: Center(
                      child: Text(
                        ':',
                        style: TextStyle(
                          fontSize: 22,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ),
                  ),
                  _buildWheel(
                    controller: _minutesController,
                    max: 59,
                    currentVal: _minutes,
                    onChanged: (val) => setState(() => _minutes = val),
                  ),
                  const SizedBox(
                    width: 44,
                    child: Center(
                      child: Text(
                        ':',
                        style: TextStyle(
                          fontSize: 22,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ),
                  ),
                  _buildWheel(
                    controller: _secondsController,
                    max: 59,
                    currentVal: _seconds,
                    onChanged: (val) => setState(() => _seconds = val),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildWheel({
    required FixedExtentScrollController controller,
    required int max,
    required int currentVal,
    required ValueChanged<int> onChanged,
  }) {
    return SizedBox(
      width: 50,
      height: 110,
      child: ListWheelScrollView.useDelegate(
        controller: controller,
        itemExtent: 32,
        perspective: 0.003,
        diameterRatio: 1.2,
        physics: const FixedExtentScrollPhysics(),
        onSelectedItemChanged: onChanged,
        childDelegate: ListWheelChildBuilderDelegate(
          childCount: max + 1,
          builder: (context, index) {
            final isSelected = index == currentVal;
            return Center(
              child: Text(
                index.toString().padLeft(2, '0'),
                style: TextStyle(
                  fontSize: isSelected ? 22 : 16,
                  fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                  color: isSelected ? AppColors.textPrimary : AppColors.textSecondary.withOpacity(0.4),
                ),
              ),
            );
          },
        ),
      ),
    );
  }

  Widget _buildPaceRequiredCard(String calculatedPace) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: const Color(0xFFFFFBE6), // Muted warm yellow
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: const Color(0xFFFFE58F)),
      ),
      child: Row(
        children: [
          const Icon(Icons.speed_rounded, color: Color(0xFFD46B08), size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: RichText(
              text: TextSpan(
                style: const TextStyle(
                  color: Color(0xFFD46B08),
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                ),
                children: [
                  const TextSpan(text: 'Pace Required: '),
                  TextSpan(
                    text: '$calculatedPace /km',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAveragePaceButton(double distanceKm, String avgLabel) {
    return OutlinedButton.icon(
      onPressed: () => _onGoWithAverage(distanceKm),
      icon: const Icon(Icons.history_toggle_off_rounded, size: 18),
      label: Text(avgLabel, style: const TextStyle(fontWeight: FontWeight.w600)),
      style: OutlinedButton.styleFrom(
        foregroundColor: AppColors.textPrimary,
        side: const BorderSide(color: AppColors.border, width: 1.5),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        padding: const EdgeInsets.symmetric(vertical: 12),
        minimumSize: const Size(double.infinity, 48),
      ),
    );
  }
}
