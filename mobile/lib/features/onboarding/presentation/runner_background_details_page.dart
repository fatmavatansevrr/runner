import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/routing/app_router.dart';
import '../../../core/models/recent_race_result.dart';
import '../data/onboarding_provider.dart';

/// Running Background V2 — the "runner-background-details.png" screen.
/// Reached only for Intermediate/Advanced/Experienced (Beginner skips this
/// entirely — see `RunningBackgroundPage._onContinue`). Preserves the same
/// shell/progress/back-button/typography conventions as the rest of
/// onboarding; introduces no unit selector (the active application unit is
/// displayed as a field suffix only).
class RunnerBackgroundDetailsPage extends ConsumerStatefulWidget {
  const RunnerBackgroundDetailsPage({super.key});

  @override
  ConsumerState<RunnerBackgroundDetailsPage> createState() => _RunnerBackgroundDetailsPageState();
}

class _RunnerBackgroundDetailsPageState extends ConsumerState<RunnerBackgroundDetailsPage> {
  late final TextEditingController _weeklyVolumeController;
  late final TextEditingController _longestRunController;
  bool _weeklyVolumeNotSure = false;
  bool _longestRunNotSure = false;

  bool get _isMiles => ref.read(onboardingProvider).unit == 'mile';

  double _toDisplayUnit(double km) => _isMiles ? km / 1.60934 : km;
  double _toKm(double displayValue) => _isMiles ? displayValue * 1.60934 : displayValue;

  @override
  void initState() {
    super.initState();
    final state = ref.read(onboardingProvider);

    final weeklyKm = state.recentWeeklyVolumeKm;
    _weeklyVolumeNotSure = weeklyKm == null;
    _weeklyVolumeController = TextEditingController(
      text: weeklyKm == null ? '' : _formatNumber(_toDisplayUnit(weeklyKm)),
    );

    final longestKm = state.recentLongestRunKm;
    _longestRunNotSure = longestKm == null;
    _longestRunController = TextEditingController(
      text: longestKm == null ? '' : _formatNumber(_toDisplayUnit(longestKm)),
    );
  }

  @override
  void dispose() {
    _weeklyVolumeController.dispose();
    _longestRunController.dispose();
    super.dispose();
  }

  static String _formatNumber(double value) {
    if (value == value.roundToDouble()) return value.toStringAsFixed(0);
    return value.toStringAsFixed(1);
  }

  double? _parseField(TextEditingController controller, bool notSure) {
    if (notSure) return null;
    final text = controller.text.trim();
    if (text.isEmpty) return null;
    final parsed = double.tryParse(text);
    if (parsed == null || parsed.isNaN || parsed.isInfinite || parsed < 0) return null;
    return _toKm(parsed);
  }

  Future<void> _onAddRecentResult() async {
    final existing = ref.read(onboardingProvider).recentRaceResult;
    final result = await context.push<RecentRaceResult>(
      AppRoutes.recentRaceResult,
      extra: existing,
    );
    if (result != null) {
      ref.read(onboardingProvider.notifier).updateRecentRaceResult(result);
    }
  }

  void _onContinue() {
    ref.read(onboardingProvider.notifier)
      ..updateRecentWeeklyVolumeKm(_parseField(_weeklyVolumeController, _weeklyVolumeNotSure))
      ..updateRecentLongestRunKm(_parseField(_longestRunController, _longestRunNotSure));

    final goalType = ref.read(onboardingProvider).goalType;
    if (goalType == 'habit') {
      context.go(AppRoutes.habitGoal);
    } else {
      context.go(AppRoutes.goalTime);
    }
  }

  @override
  Widget build(BuildContext context) {
    final unit = ref.watch(onboardingProvider).unit == 'mile' ? 'mi' : 'km';
    final recentRaceResult = ref.watch(onboardingProvider).recentRaceResult;

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_rounded, color: AppColors.textPrimary),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                    tooltip: 'Back',
                    onPressed: () => context.go(AppRoutes.runningBackground),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(100),
                      child: const LinearProgressIndicator(
                        value: 0.42,
                        backgroundColor: AppColors.border,
                        color: AppColors.primary,
                        minHeight: 6,
                        semanticsLabel: 'Onboarding progress',
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.xl),

              Text(
                'Tell us about your recent running',
                style: AppTextStyles.h1.copyWith(
                  fontSize: 24,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'This helps us choose a safe and realistic starting point for your plan.',
                style: AppTextStyles.bodyLarge.copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.xl),

              Expanded(
                child: SingleChildScrollView(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _NumericReadinessField(
                        label: 'Average weekly distance',
                        helper: 'Average distance run per week over the last 4 weeks',
                        unit: unit,
                        controller: _weeklyVolumeController,
                        notSure: _weeklyVolumeNotSure,
                        onNotSureChanged: (v) => setState(() => _weeklyVolumeNotSure = v),
                        semanticsIdentifier: 'average_weekly_distance_field',
                      ),
                      const SizedBox(height: AppSpacing.lg),
                      _NumericReadinessField(
                        label: 'Longest run',
                        helper: 'The single longest run completed during the last 4 weeks',
                        unit: unit,
                        controller: _longestRunController,
                        notSure: _longestRunNotSure,
                        onNotSureChanged: (v) => setState(() => _longestRunNotSure = v),
                        semanticsIdentifier: 'longest_run_field',
                      ),
                      const SizedBox(height: AppSpacing.lg),

                      Text(
                        'Recent race result (optional)',
                        style: AppTextStyles.label.copyWith(fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: AppSpacing.xs),
                      if (recentRaceResult == null)
                        OutlinedButton.icon(
                          onPressed: _onAddRecentResult,
                          icon: const Icon(Icons.add_rounded),
                          label: const Text('Add recent result'),
                        )
                      else
                        Container(
                          padding: const EdgeInsets.all(AppSpacing.md),
                          decoration: BoxDecoration(
                            border: Border.all(color: AppColors.border),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Row(
                            children: [
                              Expanded(
                                child: Semantics(
                                  label: 'Saved recent race result',
                                  child: Text(
                                    recentRaceResult.summary(useKm: unit == 'km'),
                                    style: AppTextStyles.bodyMedium,
                                  ),
                                ),
                              ),
                              IconButton(
                                icon: const Icon(Icons.edit_outlined, size: 20),
                                tooltip: 'Edit recent result',
                                onPressed: _onAddRecentResult,
                              ),
                              IconButton(
                                icon: const Icon(Icons.close_rounded, size: 20),
                                tooltip: 'Remove recent result',
                                onPressed: () => ref
                                    .read(onboardingProvider.notifier)
                                    .updateRecentRaceResult(null),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: AppSpacing.md),
              AppPrimaryButton(
                label: 'Continue',
                icon: Icons.arrow_forward_rounded,
                onPressed: _onContinue,
              ),
              const SizedBox(height: AppSpacing.xs),
            ],
          ),
        ),
      ),
    );
  }
}

class _NumericReadinessField extends StatelessWidget {
  const _NumericReadinessField({
    required this.label,
    required this.helper,
    required this.unit,
    required this.controller,
    required this.notSure,
    required this.onNotSureChanged,
    required this.semanticsIdentifier,
  });

  final String label;
  final String helper;
  final String unit;
  final TextEditingController controller;
  final bool notSure;
  final ValueChanged<bool> onNotSureChanged;
  final String semanticsIdentifier;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: AppTextStyles.label.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 2),
        Text(helper, style: AppTextStyles.bodyMedium.copyWith(color: AppColors.textSecondary)),
        const SizedBox(height: AppSpacing.xs),
        Semantics(
          textField: true,
          enabled: !notSure,
          label: '$label, in $unit',
          child: TextField(
            controller: controller,
            enabled: !notSure,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: InputDecoration(
              hintText: 'e.g. 20',
              suffixText: unit,
              prefixIcon: const Icon(Icons.speed_outlined, color: AppColors.textMuted),
            ),
          ),
        ),
        const SizedBox(height: AppSpacing.xs),
        Semantics(
          label: "I'm not sure, for $label",
          checked: notSure,
          child: InkWell(
            onTap: () => onNotSureChanged(!notSure),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Checkbox(value: notSure, onChanged: (v) => onNotSureChanged(v ?? false)),
                const Text("I'm not sure", style: AppTextStyles.bodyMedium),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
