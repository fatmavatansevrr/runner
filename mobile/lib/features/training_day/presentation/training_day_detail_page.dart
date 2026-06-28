import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_button.dart';
import '../../../core/widgets/app_card.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../../../core/widgets/status_indicator.dart';
import '../../../core/network/dtos.dart';
import '../data/training_day_provider.dart';
import '../../home/data/home_repository.dart';
import '../../home/data/home_provider.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';

/// Full-page detail view for a single training day.
/// Opened from Calendar day taps and Home workout card taps.
///
/// Displays all 4 states:
///   - rest        → Rest Day panel (no actions)
///   - planned     → Workout info + Complete / Not Today buttons
///   - completed   → Logged stats summary
///   - missed      → Supportive messaging
class TrainingDayDetailPage extends ConsumerStatefulWidget {
  const TrainingDayDetailPage({super.key, required this.dayId});

  final String dayId;

  @override
  ConsumerState<TrainingDayDetailPage> createState() =>
      _TrainingDayDetailPageState();
}

class _TrainingDayDetailPageState
    extends ConsumerState<TrainingDayDetailPage> {
  final _distanceController = TextEditingController();
  final _durationController = TextEditingController();
  bool _isSubmitting = false;

  @override
  void dispose() {
    _distanceController.dispose();
    _durationController.dispose();
    super.dispose();
  }

  // ── Helpers ──────────────────────────────────────────────────────────────

  String _formatDate(DateTime d) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    const weekdays = [
      'Monday', 'Tuesday', 'Wednesday', 'Thursday',
      'Friday', 'Saturday', 'Sunday'
    ];
    return '${weekdays[d.weekday - 1]}, ${months[d.month - 1]} ${d.day}';
  }

  String _formatWorkoutType(String dayType) => switch (dayType) {
        'easy'           => 'Easy Run',
        'long_run'       => 'Long Run',
        'interval'       => 'Interval',
        'tempo'          => 'Tempo Run',
        'recovery_easy'  => 'Recovery Run',
        'rest'           => 'Rest Day',
        _                => dayType,
      };

  // ── Completion sheet ─────────────────────────────────────────────────────

  void _showCompletionSheet(TrainingDayDetailResponse day) {
    _distanceController.text = day.plannedDistanceKm.toStringAsFixed(1);
    _durationController.text = day.plannedDurationMin.toString();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: AppColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => Padding(
        padding: EdgeInsets.only(
          left: AppSpacing.md,
          right: AppSpacing.md,
          top: AppSpacing.lg,
          bottom: MediaQuery.of(ctx).viewInsets.bottom + AppSpacing.lg,
        ),
        child: StatefulBuilder(
          builder: (context, setModalState) => Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.check_circle_outline_rounded,
                      color: AppColors.completed),
                  const SizedBox(width: AppSpacing.sm),
                  Text('Log Workout', style: AppTextStyles.h2),
                ],
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Enter your actual distance and duration.',
                style: AppTextStyles.bodyMedium
                    .copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: _distanceController,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(
                  labelText: 'Actual Distance (km)',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: _durationController,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Actual Duration (minutes)',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              if (_isSubmitting)
                const Center(
                    child: CircularProgressIndicator(
                        color: AppColors.completed))
              else
                AppPrimaryButton(
                  label: 'Save Workout',
                  onPressed: () async {
                    final dist = double.tryParse(
                            _distanceController.text.trim()) ??
                        day.plannedDistanceKm;
                    final dur = int.tryParse(
                            _durationController.text.trim()) ??
                        day.plannedDurationMin;

                    setModalState(() => _isSubmitting = true);
                    setState(() => _isSubmitting = true);
                    try {
                      final repo = ref.read(homeRepositoryProvider);
                      await repo.completeWorkout(
                          day.dayId, dist, dur, 'Logged from detail view.');
                      _invalidateAll();
                      if (context.mounted) Navigator.pop(context);
                      if (mounted) context.pop();
                    } catch (e) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text('Error: ${e.toString()}')),
                        );
                      }
                    } finally {
                      setModalState(() => _isSubmitting = false);
                      setState(() => _isSubmitting = false);
                    }
                  },
                ),
            ],
          ),
        ),
      ),
    );
  }

  // ── Not Today sheet ──────────────────────────────────────────────────────

  void _showNotTodaySheet(TrainingDayDetailResponse day) {
    String selectedReason = 'Too busy';
    final reasons = [
      'Too busy',
      'Feeling tired',
      'Bad weather',
      'Injured / Sore',
      'Other'
    ];

    showModalBottomSheet(
      context: context,
      backgroundColor: AppColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: StatefulBuilder(
          builder: (context, setModalState) => Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.directions_run_outlined,
                      color: AppColors.textSecondary),
                  const SizedBox(width: AppSpacing.sm),
                  Text('Skip today?', style: AppTextStyles.h2),
                ],
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                "Let us know why you can't run today.",
                style: AppTextStyles.bodyMedium
                    .copyWith(color: AppColors.textSecondary),
              ),
              const SizedBox(height: AppSpacing.sm),
              ...reasons.map((reason) => RadioListTile<String>(
                    title: Text(reason, style: AppTextStyles.bodyMedium),
                    value: reason,
                    groupValue: selectedReason,
                    activeColor: AppColors.primary,
                    contentPadding: EdgeInsets.zero,
                    onChanged: (val) {
                      if (val != null) {
                        setModalState(() => selectedReason = val);
                      }
                    },
                  )),
              const SizedBox(height: AppSpacing.md),
              if (_isSubmitting)
                const Center(
                    child: CircularProgressIndicator(
                        color: AppColors.primary))
              else
                AppPrimaryButton(
                  label: 'Skip Workout',
                  onPressed: () async {
                    setModalState(() => _isSubmitting = true);
                    setState(() => _isSubmitting = true);
                    try {
                      final repo = ref.read(homeRepositoryProvider);
                      final decision = await repo
                          .createNotTodayDecision(day.dayId, selectedReason);
                      await repo
                          .confirmNotTodayDecision(decision.decisionId);
                      _invalidateAll();
                      if (context.mounted) Navigator.pop(context);
                      if (mounted) context.pop();
                    } catch (e) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text('Error: ${e.toString()}')),
                        );
                      }
                    } finally {
                      setModalState(() => _isSubmitting = false);
                      setState(() => _isSubmitting = false);
                    }
                  },
                ),
            ],
          ),
        ),
      ),
    );
  }

  void _invalidateAll() {
    ref.invalidate(homeDataProvider);
    ref.invalidate(calendarDataProvider);
    ref.invalidate(profileOverviewProvider);
    ref.invalidate(activePlanDetailsProvider);
    ref.invalidate(trainingDayDetailProvider(widget.dayId));
  }

  // ── Build ─────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final detailState = ref.watch(trainingDayDetailProvider(widget.dayId));

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Workout Detail'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        foregroundColor: AppColors.textPrimary,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded,
              color: AppColors.textPrimary),
          onPressed: () => context.pop(),
        ),
      ),
      body: SafeArea(
        child: detailState.when(
          loading: () => const LoadingState(message: 'Loading workout...'),
          error: (err, _) => Center(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.cloud_off_rounded,
                      size: 64, color: AppColors.textMuted),
                  const SizedBox(height: AppSpacing.md),
                  Text('Could not load workout',
                      style: AppTextStyles.h2),
                  const SizedBox(height: AppSpacing.sm),
                  Text(err.toString(),
                      style: AppTextStyles.bodyMedium,
                      textAlign: TextAlign.center),
                  const SizedBox(height: AppSpacing.lg),
                  AppPrimaryButton(
                    label: 'Retry',
                    onPressed: () =>
                        ref.invalidate(trainingDayDetailProvider(widget.dayId)),
                  ),
                ],
              ),
            ),
          ),
          data: (day) => _DayDetailContent(
            day: day,
            formatDate: _formatDate,
            formatType: _formatWorkoutType,
            onComplete: () => _showCompletionSheet(day),
            onNotToday: () => _showNotTodaySheet(day),
          ),
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Content widget — handles all 4 states
// ─────────────────────────────────────────────────────────────────────────────

class _DayDetailContent extends StatelessWidget {
  const _DayDetailContent({
    required this.day,
    required this.formatDate,
    required this.formatType,
    required this.onComplete,
    required this.onNotToday,
  });

  final TrainingDayDetailResponse day;
  final String Function(DateTime) formatDate;
  final String Function(String) formatType;
  final VoidCallback onComplete;
  final VoidCallback onNotToday;

  @override
  Widget build(BuildContext context) {
    if (day.dayType == 'rest') return _RestDayView(day: day, formatDate: formatDate);

    return switch (day.status) {
      'completed' => _CompletedView(day: day, formatDate: formatDate, formatType: formatType),
      'missed'    => _MissedView(day: day, formatDate: formatDate, formatType: formatType),
      'skipped'   => _MissedView(day: day, formatDate: formatDate, formatType: formatType),
      _           => _PlannedView(
          day: day,
          formatDate: formatDate,
          formatType: formatType,
          onComplete: onComplete,
          onNotToday: onNotToday,
        ),
    };
  }
}

// ── Rest Day ──────────────────────────────────────────────────────────────────

class _RestDayView extends StatelessWidget {
  const _RestDayView({required this.day, required this.formatDate});
  final TrainingDayDetailResponse day;
  final String Function(DateTime) formatDate;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        children: [
          const SizedBox(height: AppSpacing.xl),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AppSpacing.xl),
            decoration: BoxDecoration(
              color: AppColors.restTint,
              borderRadius: BorderRadius.circular(24),
            ),
            child: Column(
              children: [
                const Icon(Icons.hotel_rounded,
                    size: 64, color: AppColors.textSecondary),
                const SizedBox(height: AppSpacing.md),
                Text('Rest Day', style: AppTextStyles.h1),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  formatDate(day.date),
                  style: AppTextStyles.bodyMedium
                      .copyWith(color: AppColors.textSecondary),
                ),
                const SizedBox(height: AppSpacing.md),
                Text(
                  'Recovery is part of training. Rest, hydrate, and come back stronger.',
                  style: AppTextStyles.bodyMedium,
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Planned ───────────────────────────────────────────────────────────────────

class _PlannedView extends StatelessWidget {
  const _PlannedView({
    required this.day,
    required this.formatDate,
    required this.formatType,
    required this.onComplete,
    required this.onNotToday,
  });
  final TrainingDayDetailResponse day;
  final String Function(DateTime) formatDate;
  final String Function(String) formatType;
  final VoidCallback onComplete;
  final VoidCallback onNotToday;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Date + status ────────────────────────────────────────────────
          Text(formatDate(day.date).toUpperCase(),
              style: AppTextStyles.label),
          const SizedBox(height: AppSpacing.sm),

          // ── Hero card ────────────────────────────────────────────────────
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AppSpacing.md),
            decoration: BoxDecoration(
              color: AppColors.todayCardBackground,
              borderRadius: BorderRadius.circular(20),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(formatType(day.dayType).toUpperCase(),
                        style: AppTextStyles.labelPrimary),
                    StatusIndicator(status: day.status),
                  ],
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  '${day.plannedDistanceKm.toStringAsFixed(1)} km',
                  style: AppTextStyles.displayLarge,
                ),
                Text(day.title, style: AppTextStyles.h2),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  day.description,
                  style: AppTextStyles.bodyMedium
                      .copyWith(color: AppColors.textSecondary),
                ),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Metrics row ──────────────────────────────────────────────────
          AppCard(
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _MetricCell(
                  icon: Icons.timer_outlined,
                  label: 'DURATION',
                  value: '${day.plannedDurationMin} min',
                ),
                if (day.plannedPaceMinKm != null)
                  _MetricCell(
                    icon: Icons.speed_rounded,
                    label: 'TARGET PACE',
                    value:
                        '${day.plannedPaceMinKm!.toStringAsFixed(2)} /km',
                  ),
                if (day.intensity != null)
                  _MetricCell(
                    icon: Icons.bolt_rounded,
                    label: 'INTENSITY',
                    value: day.intensity!.toUpperCase(),
                  ),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.lg),

          // ── Action buttons ───────────────────────────────────────────────
          if (day.canMarkComplete)
            AppPrimaryButton(
              label: 'Mark as Completed',
              icon: Icons.check_rounded,
              onPressed: onComplete,
            ),
          if (day.canMarkComplete) const SizedBox(height: AppSpacing.sm),
          if (day.canMarkNotToday)
            AppSecondaryButton(
              label: 'Not Today',
              onPressed: onNotToday,
            ),
        ],
      ),
    );
  }
}

// ── Completed ─────────────────────────────────────────────────────────────────

class _CompletedView extends StatelessWidget {
  const _CompletedView({
    required this.day,
    required this.formatDate,
    required this.formatType,
  });
  final TrainingDayDetailResponse day;
  final String Function(DateTime) formatDate;
  final String Function(String) formatType;

  @override
  Widget build(BuildContext context) {
    final actualDist = day.actualDistanceKm ?? day.plannedDistanceKm;
    final actualDur = day.actualDurationMin ?? day.plannedDurationMin;
    // Compute pace from actual values
    final pace = actualDist > 0 ? actualDur / actualDist : 0.0;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(formatDate(day.date).toUpperCase(), style: AppTextStyles.label),
          const SizedBox(height: AppSpacing.sm),

          // ── Celebration card ─────────────────────────────────────────────
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AppSpacing.md),
            decoration: BoxDecoration(
              color: AppColors.completedLight,
              borderRadius: BorderRadius.circular(20),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(formatType(day.dayType).toUpperCase(),
                        style: AppTextStyles.label),
                    StatusIndicator(status: 'completed'),
                  ],
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  '${actualDist.toStringAsFixed(1)} km',
                  style: AppTextStyles.displayLarge
                      .copyWith(color: AppColors.completed),
                ),
                Text(day.title, style: AppTextStyles.h2),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  'Great job completing this workout!',
                  style: AppTextStyles.bodyMedium
                      .copyWith(color: AppColors.textSecondary),
                ),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Stats grid ───────────────────────────────────────────────────
          Text('ACTUAL STATS', style: AppTextStyles.label),
          const SizedBox(height: AppSpacing.sm),
          AppCard(
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _MetricCell(
                  icon: Icons.straighten_rounded,
                  label: 'DISTANCE',
                  value: '${actualDist.toStringAsFixed(2)} km',
                  color: AppColors.completed,
                ),
                _MetricCell(
                  icon: Icons.timer_outlined,
                  label: 'DURATION',
                  value: '$actualDur min',
                  color: AppColors.completed,
                ),
                if (actualDist > 0)
                  _MetricCell(
                    icon: Icons.speed_rounded,
                    label: 'AVG PACE',
                    value: '${pace.toStringAsFixed(2)} /km',
                    color: AppColors.completed,
                  ),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.sm),

          // ── Planned vs actual ────────────────────────────────────────────
          Text('PLANNED vs ACTUAL', style: AppTextStyles.label),
          const SizedBox(height: AppSpacing.sm),
          AppCard(
            child: Column(
              children: [
                _ComparisonRow(
                  label: 'Distance',
                  planned: '${day.plannedDistanceKm.toStringAsFixed(1)} km',
                  actual: '${actualDist.toStringAsFixed(1)} km',
                ),
                const Divider(height: AppSpacing.lg, color: AppColors.divider),
                _ComparisonRow(
                  label: 'Duration',
                  planned: '${day.plannedDurationMin} min',
                  actual: '$actualDur min',
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Missed ────────────────────────────────────────────────────────────────────

class _MissedView extends StatelessWidget {
  const _MissedView({
    required this.day,
    required this.formatDate,
    required this.formatType,
  });
  final TrainingDayDetailResponse day;
  final String Function(DateTime) formatDate;
  final String Function(String) formatType;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(formatDate(day.date).toUpperCase(), style: AppTextStyles.label),
          const SizedBox(height: AppSpacing.sm),

          // ── Supportive card ──────────────────────────────────────────────
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AppSpacing.lg),
            decoration: BoxDecoration(
              color: AppColors.missedLight,
              borderRadius: BorderRadius.circular(20),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                const Icon(Icons.directions_run_rounded,
                    size: 56, color: AppColors.textSecondary),
                const SizedBox(height: AppSpacing.md),
                Text(
                  'You missed this one.',
                  style: AppTextStyles.h2,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  "That's okay — every runner has off days. What matters is showing up again.",
                  style: AppTextStyles.bodyMedium.copyWith(
                    color: AppColors.textSecondary,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.md),
                StatusIndicator(status: day.status),
              ],
            ),
          ),
          const SizedBox(height: AppSpacing.md),

          // ── What was planned ─────────────────────────────────────────────
          Text('WHAT WAS PLANNED', style: AppTextStyles.label),
          const SizedBox(height: AppSpacing.sm),
          AppCard(
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _MetricCell(
                  icon: Icons.straighten_rounded,
                  label: 'DISTANCE',
                  value: '${day.plannedDistanceKm.toStringAsFixed(1)} km',
                ),
                _MetricCell(
                  icon: Icons.timer_outlined,
                  label: 'DURATION',
                  value: '${day.plannedDurationMin} min',
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Shared sub-widgets ────────────────────────────────────────────────────────

class _MetricCell extends StatelessWidget {
  const _MetricCell({
    required this.icon,
    required this.label,
    required this.value,
    this.color,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final resolvedColor = color ?? AppColors.textSecondary;
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 20, color: resolvedColor),
        const SizedBox(height: 4),
        Text(label,
            style: AppTextStyles.label.copyWith(fontSize: 10)),
        const SizedBox(height: 2),
        Text(
          value,
          style: AppTextStyles.h3.copyWith(color: AppColors.textPrimary, fontSize: 14),
        ),
      ],
    );
  }
}

class _ComparisonRow extends StatelessWidget {
  const _ComparisonRow({
    required this.label,
    required this.planned,
    required this.actual,
  });

  final String label;
  final String planned;
  final String actual;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(label, style: AppTextStyles.bodyMedium),
        ),
        Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'Planned: $planned',
              style:
                  AppTextStyles.bodySmall.copyWith(color: AppColors.textMuted),
            ),
            Text(
              'Actual: $actual',
              style: AppTextStyles.bodySmall
                  .copyWith(color: AppColors.completed, fontWeight: FontWeight.w600),
            ),
          ],
        ),
      ],
    );
  }
}
