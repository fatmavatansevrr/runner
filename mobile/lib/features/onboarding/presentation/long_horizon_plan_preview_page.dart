import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/routing/app_router.dart';
import '../../../core/network/long_horizon_dtos.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/network/bootstrap_provider.dart';
import '../../../core/widgets/app_button.dart';
import '../data/onboarding_provider.dart';
import '../../plan/data/long_horizon_repository.dart';
import '../../home/data/home_provider.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';
import '../../plan/data/long_horizon_provider.dart';
import '../../plan/data/long_horizon_error_mapper.dart';

/// Preview screen for a Long-Horizon (21-52 week) race plan. Renders only
/// what the backend actually returned: the structural roadmap (Pending
/// weeks show a summary only — never a fabricated schedule) and the fully
/// numeric current executable window. Never infers, interpolates, or
/// synthesizes any session beyond what
/// [LongHorizonPlanPreviewContract.currentExecutableWeeks] contains.
class LongHorizonPlanPreviewPage extends ConsumerStatefulWidget {
  const LongHorizonPlanPreviewPage({super.key});

  @override
  ConsumerState<LongHorizonPlanPreviewPage> createState() =>
      _LongHorizonPlanPreviewPageState();
}

class _LongHorizonPlanPreviewPageState
    extends ConsumerState<LongHorizonPlanPreviewPage> {
  bool _isConfirming = false;
  bool _confirmationCommitted = false;
  final Set<int> _expandedWeekNumbers = {1};

  void _toggleWeek(int weekNumber) {
    setState(() {
      if (!_expandedWeekNumbers.remove(weekNumber)) {
        _expandedWeekNumbers.add(weekNumber);
      }
    });
  }

  Future<void> _onConfirm(String previewId) async {
    if (_isConfirming) return;
    setState(() => _isConfirming = true);
    try {
      final repo = ref.read(longHorizonRepositoryProvider);
      await repo.confirmLongHorizonPlan(previewId);
      _finishCommittedConfirmation();
    } catch (e) {
      if (_isAmbiguousMutationError(e)) {
        await _verifyConfirmationAfterAmbiguity();
      } else {
        _showMessage(LongHorizonUiErrorMapper.map(e).userMessage);
      }
    } finally {
      if (mounted) setState(() => _isConfirming = false);
    }
  }

  bool _isAmbiguousMutationError(Object error) =>
      error is! ApiException ||
      error.statusCode == null ||
      error.statusCode == 408 ||
      error.statusCode == 504;

  Future<void> _verifyConfirmationAfterAmbiguity() async {
    try {
      final result =
          await ref.read(longHorizonRepositoryProvider).fetchActiveHome();
      if (result.rollingHome != null) {
        _finishCommittedConfirmation();
        return;
      }
      _showMessage(
        "We couldn't confirm that the plan was activated. Your preview is still here; please try again.",
      );
    } catch (_) {
      _showMessage(
        "We couldn't verify whether the plan was activated. Check your connection and try again.",
      );
    }
  }

  void _finishCommittedConfirmation() {
    if (_confirmationCommitted) return;
    _confirmationCommitted = true;
    ref.invalidate(bootstrapDataProvider);
    ref.invalidate(homeDataProvider);
    ref.invalidate(calendarDataProvider);
    ref.invalidate(profileOverviewProvider);
    ref.invalidate(activeHomeResultProvider);
    ref.invalidate(activeCalendarResultProvider);
    ref.read(onboardingProvider.notifier).reset();
    if (mounted) context.go(AppRoutes.home);
  }

  void _showMessage(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }

  String _goalLabel(String goalType, String goalDistance) {
    final distLabel = switch (goalDistance) {
      'five_k' => '5 km',
      'ten_k' => '10 km',
      'half_marathon' => 'Half Marathon',
      'marathon' => 'Marathon',
      _ => goalDistance,
    };
    return 'Race $distLabel';
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(onboardingProvider);
    final preview = state.longHorizonPreviewResponse;

    if (preview == null) {
      return Scaffold(
        backgroundColor: AppColors.background,
        body: SafeArea(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.error_outline_rounded,
                      size: 40, color: AppColors.textMuted),
                  const SizedBox(height: 16),
                  const Text(
                    "We couldn't find a plan preview.\nPlease go back and try again.",
                    textAlign: TextAlign.center,
                    style:
                        TextStyle(fontSize: 15, color: AppColors.textSecondary),
                  ),
                  const SizedBox(height: 20),
                  AppPrimaryButton(
                    label: 'Go Back',
                    onPressed: () => context.canPop()
                        ? context.pop()
                        : context.go(AppRoutes.startDate),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }

    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const LinearProgressIndicator(
              value: 1.0,
              minHeight: 3,
              backgroundColor: AppColors.border,
              color: AppColors.primary,
            ),
            const SizedBox(height: 8),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: IconButton(
                icon: const Icon(Icons.arrow_back_rounded,
                    color: AppColors.textPrimary),
                onPressed: () => context.go(AppRoutes.startDate),
              ),
            ),
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Plan Preview',
                      style: TextStyle(
                          fontSize: 30,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary,
                          height: 1.2),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      '${preview.totalWeeks}-week plan • your current training block '
                      'is shown in full detail; later blocks unlock as you progress.',
                      style: const TextStyle(
                          fontSize: 15,
                          color: AppColors.textSecondary,
                          height: 1.5),
                    ),
                    const SizedBox(height: 28),
                    _InfoCard(preview: preview, goalLabel: _goalLabel),
                    const SizedBox(height: 28),
                    const Text(
                      'Current training block',
                      style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary),
                    ),
                    const SizedBox(height: 12),
                    ...preview.currentExecutableWeeks.map(
                      (week) => _ExecutableWeekCard(
                        week: week,
                        expanded:
                            _expandedWeekNumbers.contains(week.globalWeek),
                        onToggle: () => _toggleWeek(week.globalWeek),
                      ),
                    ),
                    if (preview.structuralRoadmap.length >
                        preview.currentExecutableWeeks.length) ...[
                      const SizedBox(height: 24),
                      const Text(
                        'Full roadmap',
                        style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w700,
                            color: AppColors.textPrimary),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'Later weeks unlock automatically as you complete each block — '
                        "they aren't scheduled with specific workouts yet.",
                        style: TextStyle(
                            fontSize: 13, color: AppColors.textSecondary),
                      ),
                      const SizedBox(height: 12),
                      ...preview.structuralRoadmap
                          .where((w) => !w.isExecutable)
                          .map((w) => _RoadmapRow(week: w)),
                    ],
                  ],
                ),
              ),
            ),
            if (!preview.isConfirmable)
              const Padding(
                padding: EdgeInsets.fromLTRB(24, 0, 24, 12),
                child: Text(
                  'This plan is available for preview, but activation is not currently available.',
                  textAlign: TextAlign.center,
                  style:
                      TextStyle(fontSize: 13, color: AppColors.textSecondary),
                ),
              ),
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 0, 24, 24),
              child: AppPrimaryButton(
                label: 'Looks good, continue',
                isLoading: _isConfirming,
                icon: Icons.arrow_forward_rounded,
                onPressed: preview.isConfirmable
                    ? () => _onConfirm(preview.previewId)
                    : null,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoCard extends StatelessWidget {
  const _InfoCard({required this.preview, required this.goalLabel});
  final LongHorizonPlanPreviewContract preview;
  final String Function(String, String) goalLabel;

  @override
  Widget build(BuildContext context) {
    final rows = [
      ('GOAL', goalLabel('race', preview.goalDistance), Icons.flag_rounded),
      ('DURATION', '${preview.totalWeeks} weeks', Icons.calendar_today_rounded),
      (
        'CURRENT BLOCK',
        'Weeks ${preview.currentWindowStartWeek}-${preview.currentWindowEndWeek}',
        Icons.view_week_rounded
      ),
      ('RACE DATE', preview.raceDate, Icons.emoji_events_rounded),
    ];
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: List.generate(rows.length, (i) {
          final (label, value, icon) = rows[i];
          return Column(
            children: [
              Padding(
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                child: Row(
                  children: [
                    Icon(icon, color: AppColors.primary, size: 20),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(label,
                              style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                  color: AppColors.textMuted)),
                          Text(value,
                              style: const TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.w600,
                                  color: AppColors.textPrimary)),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              if (i < rows.length - 1)
                const Divider(
                    height: 1,
                    indent: 50,
                    endIndent: 16,
                    color: AppColors.border),
            ],
          );
        }),
      ),
    );
  }
}

class _ExecutableWeekCard extends StatelessWidget {
  const _ExecutableWeekCard(
      {required this.week, required this.expanded, required this.onToggle});
  final LongHorizonExecutableWeekContract week;
  final bool expanded;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          InkWell(
            onTap: onToggle,
            borderRadius: BorderRadius.circular(16),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Expanded(
                    child: Text('Week ${week.globalWeek} • ${week.phase.label}',
                        style: const TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                            color: AppColors.textPrimary)),
                  ),
                  Text('${week.weeklyVolumeKm.toStringAsFixed(1)} km',
                      style: const TextStyle(
                          fontSize: 13, color: AppColors.textSecondary)),
                  Icon(
                      expanded
                          ? Icons.expand_less_rounded
                          : Icons.expand_more_rounded,
                      color: AppColors.textMuted),
                ],
              ),
            ),
          ),
          if (expanded)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              child: Column(
                children: week.sessions
                    .map((s) => Padding(
                          padding: const EdgeInsets.symmetric(vertical: 4),
                          child: Row(
                            children: [
                              Icon(
                                  s.isLongRun
                                      ? Icons.directions_run_rounded
                                      : Icons.circle,
                                  size: s.isLongRun ? 18 : 8,
                                  color: AppColors.primary),
                              const SizedBox(width: 10),
                              Expanded(
                                  child: Text(
                                      '${s.weekday} — ${s.distanceKm.toStringAsFixed(1)} km')),
                            ],
                          ),
                        ))
                    .toList(),
              ),
            ),
        ],
      ),
    );
  }
}

class _RoadmapRow extends StatelessWidget {
  const _RoadmapRow({required this.week});
  final LongHorizonStructuralRoadmapWeekContract week;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          const Icon(Icons.lock_outline_rounded,
              size: 16, color: AppColors.textMuted),
          const SizedBox(width: 10),
          Expanded(
            child: Text('Week ${week.globalWeek} • ${week.publicSummary}',
                style: const TextStyle(
                    fontSize: 13, color: AppColors.textSecondary)),
          ),
        ],
      ),
    );
  }
}
