import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/models/recent_race_result.dart';

/// Running Background V2 — the "Add recent result" form, opened from
/// [RunnerBackgroundDetailsPage]. Deliberately a distinct, separate object
/// from the *target* race entered in "Enter Race Details": this page never
/// reads or writes that state. Fully optional; the caller receives `null`
/// via `Navigator.pop` if the user backs out without saving.
class RecentRaceResultPage extends StatefulWidget {
  const RecentRaceResultPage({super.key, this.initialResult});

  /// When editing an already-saved result, the existing value to prefill.
  final RecentRaceResult? initialResult;

  @override
  State<RecentRaceResultPage> createState() => _RecentRaceResultPageState();
}

enum _DistancePreset { fiveK, tenK, halfMarathon, marathon, other }

extension on _DistancePreset {
  String get label => switch (this) {
        _DistancePreset.fiveK => '5K',
        _DistancePreset.tenK => '10K',
        _DistancePreset.halfMarathon => 'Half Marathon',
        _DistancePreset.marathon => 'Marathon',
        _DistancePreset.other => 'Other',
      };

  double? get distanceKm => switch (this) {
        _DistancePreset.fiveK => 5.0,
        _DistancePreset.tenK => 10.0,
        _DistancePreset.halfMarathon => 21.0975,
        _DistancePreset.marathon => 42.195,
        _DistancePreset.other => null,
      };
}

class _RecentRaceResultPageState extends State<RecentRaceResultPage> {
  _DistancePreset _preset = _DistancePreset.fiveK;
  late final TextEditingController _otherDistanceController;
  late final TextEditingController _hoursController;
  late final TextEditingController _minutesController;
  late final TextEditingController _secondsController;
  DateTime? _raceDate;

  @override
  void initState() {
    super.initState();
    final initial = widget.initialResult;

    _preset = _presetFor(initial?.distanceKm);
    _otherDistanceController = TextEditingController(
      text: _preset == _DistancePreset.other && initial != null
          ? initial.distanceKm.toString()
          : '',
    );

    final totalSeconds = initial?.finishTimeSeconds ?? 0;
    _hoursController = TextEditingController(
      text: initial == null ? '' : (totalSeconds ~/ 3600).toString(),
    );
    _minutesController = TextEditingController(
      text: initial == null ? '' : ((totalSeconds % 3600) ~/ 60).toString(),
    );
    _secondsController = TextEditingController(
      text: initial == null ? '' : (totalSeconds % 60).toString(),
    );

    _raceDate = initial?.raceDate;
  }

  static _DistancePreset _presetFor(double? km) {
    if (km == null) return _DistancePreset.fiveK;
    if ((km - 5).abs() < 0.05) return _DistancePreset.fiveK;
    if ((km - 10).abs() < 0.05) return _DistancePreset.tenK;
    if ((km - 21.0975).abs() < 0.15) return _DistancePreset.halfMarathon;
    if ((km - 42.195).abs() < 0.15) return _DistancePreset.marathon;
    return _DistancePreset.other;
  }

  @override
  void dispose() {
    _otherDistanceController.dispose();
    _hoursController.dispose();
    _minutesController.dispose();
    _secondsController.dispose();
    super.dispose();
  }

  double? get _distanceKm {
    if (_preset != _DistancePreset.other) return _preset.distanceKm;
    final parsed = double.tryParse(_otherDistanceController.text.trim());
    if (parsed == null || parsed.isNaN || parsed.isInfinite || parsed <= 0) return null;
    return parsed;
  }

  int? get _finishTimeSeconds {
    final h = int.tryParse(_hoursController.text.trim()) ?? 0;
    final m = int.tryParse(_minutesController.text.trim()) ?? 0;
    final s = int.tryParse(_secondsController.text.trim()) ?? 0;
    if (h < 0 || m < 0 || s < 0) return null;
    final total = h * 3600 + m * 60 + s;
    return total > 0 ? total : null;
  }

  bool get _isValid => _distanceKm != null && _finishTimeSeconds != null && _raceDate != null;

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _raceDate ?? now,
      firstDate: DateTime(now.year - 10),
      lastDate: now,
      helpText: 'Race date',
    );
    if (picked != null) {
      setState(() => _raceDate = picked);
    }
  }

  void _onSave() {
    final distanceKm = _distanceKm;
    final finishTimeSeconds = _finishTimeSeconds;
    final raceDate = _raceDate;
    if (distanceKm == null || finishTimeSeconds == null || raceDate == null) return;

    context.pop(RecentRaceResult(
      distanceKm: distanceKm,
      finishTimeSeconds: finishTimeSeconds,
      raceDate: raceDate,
    ));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        backgroundColor: AppColors.background,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.close_rounded, color: AppColors.textPrimary),
          tooltip: 'Cancel',
          onPressed: () => context.pop(),
        ),
        title: const Text('Recent race result', style: AppTextStyles.h3),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: SingleChildScrollView(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Distance', style: AppTextStyles.label.copyWith(fontWeight: FontWeight.bold)),
                      const SizedBox(height: AppSpacing.xs),
                      Wrap(
                        spacing: AppSpacing.sm,
                        runSpacing: AppSpacing.sm,
                        children: [
                          for (final preset in _DistancePreset.values)
                            ChoiceChip(
                              label: Text(preset.label),
                              selected: _preset == preset,
                              onSelected: (_) => setState(() => _preset = preset),
                            ),
                        ],
                      ),
                      if (_preset == _DistancePreset.other) ...[
                        const SizedBox(height: AppSpacing.sm),
                        TextField(
                          controller: _otherDistanceController,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          decoration: const InputDecoration(
                            hintText: 'e.g. 15',
                            suffixText: 'km',
                            prefixIcon: Icon(Icons.square_foot_rounded, color: AppColors.textMuted),
                          ),
                        ),
                      ],
                      const SizedBox(height: AppSpacing.lg),

                      Text('Finish time', style: AppTextStyles.label.copyWith(fontWeight: FontWeight.bold)),
                      const SizedBox(height: AppSpacing.xs),
                      Row(
                        children: [
                          Expanded(
                            child: _DurationField(controller: _hoursController, label: 'hh'),
                          ),
                          const SizedBox(width: AppSpacing.sm),
                          Expanded(
                            child: _DurationField(controller: _minutesController, label: 'mm'),
                          ),
                          const SizedBox(width: AppSpacing.sm),
                          Expanded(
                            child: _DurationField(controller: _secondsController, label: 'ss'),
                          ),
                        ],
                      ),
                      const SizedBox(height: AppSpacing.lg),

                      Text('Race date', style: AppTextStyles.label.copyWith(fontWeight: FontWeight.bold)),
                      const SizedBox(height: AppSpacing.xs),
                      SelectableCard(
                        isSelected: false,
                        onTap: _pickDate,
                        child: Row(
                          children: [
                            const Icon(Icons.calendar_today_rounded, color: AppColors.textMuted, size: 20),
                            const SizedBox(width: AppSpacing.sm),
                            Text(
                              _raceDate == null
                                  ? 'Select a date'
                                  : '${_raceDate!.day}/${_raceDate!.month}/${_raceDate!.year}',
                              style: AppTextStyles.bodyMedium,
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
                label: 'Save',
                icon: Icons.check_rounded,
                onPressed: _isValid ? _onSave : null,
              ),
              const SizedBox(height: AppSpacing.xs),
            ],
          ),
        ),
      ),
    );
  }
}

class _DurationField extends StatelessWidget {
  const _DurationField({required this.controller, required this.label});

  final TextEditingController controller;
  final String label;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      keyboardType: TextInputType.number,
      textAlign: TextAlign.center,
      decoration: InputDecoration(hintText: label),
    );
  }
}
