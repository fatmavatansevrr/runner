import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/models/preparation_runway.dart';
import '../../../core/network/dtos.dart';
import '../../../core/theme/app_colors.dart';
import '../../profile/data/profile_provider.dart';

/// Phase 4H.3 — previously entirely static/mock content (hardcoded "12
/// Weeks", "Week 6 of 12", 48 runs, etc. regardless of the real plan). Now
/// consumes the real, authoritative `activePlanDetailsProvider`
/// (`PlanDetailsResponse`) — the one active-plan endpoint that exposes a
/// real, per-week `TrainingWeekType` (including `PreparationRunway`) and the
/// real total week count (see PHASE4H_3_...md §5's contract audit for why
/// this is the only authoritative source for a truthful Preparation
/// Runway/Core composition anywhere in the active-plan UI — Home, Calendar,
/// and Training Day Detail do not expose week type or runway block at all).
class PlanDetailsPage extends ConsumerWidget {
  const PlanDetailsPage({super.key});

  static String _goalLabel(String goalType, String goalDistance) {
    final distLabel = switch (goalDistance) {
      'five_k' => '5 km',
      'ten_k' => '10 km',
      'half_marathon' => 'Half Marathon',
      'marathon' => 'Marathon',
      _ => goalDistance,
    };
    final verb = goalType == 'race' ? 'Race' : 'Run';
    return '$verb $distLabel';
  }

  static String _levelLabel(String level) => switch (level) {
        'beginner' => 'Beginner',
        'intermediate' => 'Intermediate',
        'advanced' => 'Advanced',
        'experienced' => 'Experienced',
        _ => level,
      };

  static String _targetTimeLabel(int? seconds) {
    if (seconds == null || seconds <= 0) return 'Not set';
    final h = seconds ~/ 3600;
    final m = (seconds % 3600) ~/ 60;
    final s = seconds % 60;
    final mm = m.toString().padLeft(2, '0');
    final ss = s.toString().padLeft(2, '0');
    return h > 0 ? '$h:$mm:$ss' : '$mm:$ss';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final planAsync = ref.watch(activePlanDetailsProvider);

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        centerTitle: true,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded,
              color: AppColors.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: const Text('Plan Summary',
            style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary)),
      ),
      body: planAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text('Unable to load plan details.',
                style: const TextStyle(color: AppColors.textSecondary)),
          ),
        ),
        data: (plan) {
          if (!plan.hasActivePlan) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Text('No active plan.',
                    style: TextStyle(color: AppColors.textSecondary)),
              ),
            );
          }
          return _PlanDetailsBody(plan: plan);
        },
      ),
    );
  }
}

class _PlanDetailsBody extends StatelessWidget {
  const _PlanDetailsBody({required this.plan});

  final PlanDetailsResponse plan;

  @override
  Widget build(BuildContext context) {
    final allDays = plan.weeks.expand((w) => w.days).toList();
    final runsTotal = allDays.length;
    final runsCompleted = allDays.where((d) => d.status == 'completed').length;
    final progressPercent = plan.totalWeeks > 0
        ? (plan.completedWeeksCount / plan.totalWeeks).clamp(0.0, 1.0)
        : 0.0;

    // Response-derived, never inferred from total duration alone.
    final runwayWeeks = plan.weeks
        .where((w) => w.weekTypeValue == PreviewWeekType.preparationRunway)
        .length;
    final isPreparationRunwayPlan = runwayWeeks > 0;
    final coreWeeks = plan.weeks.length - runwayWeeks;

    return SingleChildScrollView(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      child: Column(
        children: [
          const SizedBox(height: 24),
          _PlanHeroCard(
            title: PlanDetailsPage._goalLabel(plan.goalType, plan.goalDistance),
            subtitle: '${plan.totalWeeks}-week plan',
            badges: [
              PlanDetailsPage._levelLabel(plan.level),
              if (plan.targetFinishTimeSeconds != null)
                'Goal ${PlanDetailsPage._targetTimeLabel(plan.targetFinishTimeSeconds)}',
            ],
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              Expanded(
                child: _PlanMetricCard(
                  icon: Icons.emoji_events_outlined,
                  iconColor: const Color(0xFFD97706),
                  iconBgColor: const Color(0xFFFEF3C7),
                  label: 'GOAL TIME',
                  value: PlanDetailsPage._targetTimeLabel(
                      plan.targetFinishTimeSeconds),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _PlanMetricCard(
                  icon: Icons.calendar_today_outlined,
                  iconColor: const Color(0xFF2563EB),
                  iconBgColor: const Color(0xFFDBEAFE),
                  label: 'DURATION',
                  value: '${plan.totalWeeks} Weeks',
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          _ActiveProgressCard(
            runsCompleted: runsCompleted,
            runsTotal: runsTotal,
            distanceKm: plan.totalCompletedDistance,
            activeWeeks: plan.completedWeeksCount,
            progressPercent: progressPercent,
            note: "You're showing great consistency! Keep up that momentum.",
          ),
          const SizedBox(height: 20),
          // Segment composition (Phase 4H.3 PART 15) -- only shown for a
          // real Preparation Runway plan, never fabricated for a Core-only
          // plan (matches the onboarding preview's own convention from
          // Phase 4H.2's PlanScheduleSection).
          if (isPreparationRunwayPlan) ...[
            _SegmentCompositionCard(
                runwayWeeks: runwayWeeks, coreWeeks: coreWeeks),
            const SizedBox(height: 20),
          ],
          _PlanSummaryInfoCard(
              daysPerWeek: plan.daysPerWeek,
              runsTotal: runsTotal,
              totalWeeks: plan.totalWeeks),
          const SizedBox(height: 20),
          _WeekListCard(weeks: plan.weeks),
          const SizedBox(height: 40),
        ],
      ),
    );
  }
}

class _SegmentCompositionCard extends StatelessWidget {
  const _SegmentCompositionCard(
      {required this.runwayWeeks, required this.coreWeeks});

  final int runwayWeeks;
  final int coreWeeks;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Plan Structure',
              style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary)),
          const SizedBox(height: 12),
          Semantics(
            label:
                'Preparation $runwayWeeks weeks, Race-Specific Core $coreWeeks weeks',
            child: Row(
              children: [
                Expanded(
                    child: _PlanSummaryRow(
                        icon: Icons.trending_up_rounded,
                        iconColor: const Color(0xFFF5A623),
                        iconBgColor: const Color(0xFFFFF0D0),
                        label: 'Preparation',
                        value: '$runwayWeeks weeks')),
              ],
            ),
          ),
          const Divider(height: 24, thickness: 1),
          _PlanSummaryRow(
              icon: Icons.flag_rounded,
              iconColor: AppColors.primary,
              iconBgColor: AppColors.primaryLight,
              label: 'Race-Specific Core',
              value: '$coreWeeks weeks'),
        ],
      ),
    );
  }
}

class _WeekListCard extends StatelessWidget {
  const _WeekListCard({required this.weeks});

  final List<PlanWeekDetailDto> weeks;

  @override
  Widget build(BuildContext context) {
    if (weeks.isEmpty) return const SizedBox.shrink();
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Weeks',
              style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary)),
          const SizedBox(height: 12),
          for (final week in weeks)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Semantics(
                label: 'Week ${week.weekNumber}, ${week.weekTypeValue.label}',
                child: Row(
                  children: [
                    SizedBox(
                      width: 56,
                      // Global week number, never reset/renumbered per segment.
                      child: Text('Week ${week.weekNumber}',
                          style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textMuted)),
                    ),
                    Expanded(
                      child: Text(week.weekTypeValue.label,
                          style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textPrimary)),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}

// ── Reusable sub-widgets (unchanged visual style from the prior static page) ──

class _PlanHeroCard extends StatelessWidget {
  const _PlanHeroCard(
      {required this.title, required this.subtitle, required this.badges});
  final String title;
  final String subtitle;
  final List<String> badges;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 24),
      decoration: BoxDecoration(
          color: const Color(0xFFF3E8FF),
          borderRadius: BorderRadius.circular(28)),
      child: Column(
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: const BoxDecoration(
                color: Colors.white, shape: BoxShape.circle),
            child: const Icon(Icons.directions_run_rounded,
                color: Color(0xFF8B5CF6), size: 24),
          ),
          const SizedBox(height: 16),
          Text(title,
              textAlign: TextAlign.center,
              style: const TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary)),
          const SizedBox(height: 6),
          Text(subtitle,
              style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w500,
                  color: AppColors.textSecondary)),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: badges
                .map((b) => Container(
                      margin: const EdgeInsets.symmetric(horizontal: 4),
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 6),
                      decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.8),
                          borderRadius: BorderRadius.circular(100)),
                      child: Text(b,
                          style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textPrimary)),
                    ))
                .toList(),
          ),
        ],
      ),
    );
  }
}

class _PlanMetricCard extends StatelessWidget {
  const _PlanMetricCard(
      {required this.icon,
      required this.iconColor,
      required this.iconBgColor,
      required this.label,
      required this.value});
  final IconData icon;
  final Color iconColor;
  final Color iconBgColor;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.border, width: 1),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 36,
            height: 36,
            decoration:
                BoxDecoration(color: iconBgColor, shape: BoxShape.circle),
            child: Icon(icon, color: iconColor, size: 20),
          ),
          const SizedBox(height: 12),
          Text(label,
              style: const TextStyle(
                  fontSize: 10,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textSecondary,
                  letterSpacing: 0.5)),
          const SizedBox(height: 4),
          Text(value,
              style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary)),
        ],
      ),
    );
  }
}

class _ActiveProgressCard extends StatelessWidget {
  const _ActiveProgressCard({
    required this.runsCompleted,
    required this.runsTotal,
    required this.distanceKm,
    required this.activeWeeks,
    required this.progressPercent,
    required this.note,
  });
  final int runsCompleted;
  final int runsTotal;
  final double distanceKm;
  final int activeWeeks;
  final double progressPercent;
  final String note;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppColors.border, width: 1),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text('Active Progress',
                  style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary)),
              Text('${(progressPercent * 100).toStringAsFixed(0)}% Complete',
                  style: const TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary)),
            ],
          ),
          const SizedBox(height: 12),
          ClipRRect(
            borderRadius: BorderRadius.circular(100),
            child: LinearProgressIndicator(
                value: progressPercent,
                minHeight: 8,
                backgroundColor: const Color(0xFFF3F4F6),
                color: AppColors.ctaDark),
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              Expanded(
                  child: _StatColumn(
                      value: '$runsCompleted / $runsTotal', label: 'RUNS')),
              Container(width: 1, height: 32, color: AppColors.border),
              Expanded(
                  child: _StatColumn(
                      value: '${distanceKm.toStringAsFixed(1)} km',
                      label: 'DISTANCE')),
              Container(width: 1, height: 32, color: AppColors.border),
              Expanded(
                  child: _StatColumn(
                      value: '$activeWeeks Weeks', label: 'ACTIVE')),
            ],
          ),
          const SizedBox(height: 20),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
                color: const Color(0xFFF9FAFB),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppColors.border, width: 1)),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.favorite_rounded,
                    color: Color(0xFFEF4444), size: 16),
                const SizedBox(width: 8),
                Expanded(
                    child: Text(note,
                        style: const TextStyle(
                            fontSize: 13,
                            height: 1.4,
                            fontWeight: FontWeight.w500,
                            color: AppColors.textSecondary))),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _StatColumn extends StatelessWidget {
  const _StatColumn({required this.value, required this.label});
  final String value;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(value,
            style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: AppColors.textPrimary)),
        const SizedBox(height: 4),
        Text(label,
            style: const TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w700,
                color: AppColors.textSecondary,
                letterSpacing: 0.5)),
      ],
    );
  }
}

class _PlanSummaryInfoCard extends StatelessWidget {
  const _PlanSummaryInfoCard(
      {required this.daysPerWeek,
      required this.runsTotal,
      required this.totalWeeks});

  final int daysPerWeek;
  final int runsTotal;
  final int totalWeeks;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppColors.border, width: 1),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Plan Summary',
              style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary)),
          const SizedBox(height: 16),
          _PlanSummaryRow(
              icon: Icons.repeat_rounded,
              iconColor: const Color(0xFF6D28D9),
              iconBgColor: const Color(0xFFF3E8FF),
              label: 'Training Frequency',
              value: '$daysPerWeek Days / Week'),
          const Divider(height: 24, thickness: 1),
          _PlanSummaryRow(
              icon: Icons.directions_run_rounded,
              iconColor: const Color(0xFF1D4ED8),
              iconBgColor: const Color(0xFFDBEAFE),
              label: 'Total Plan Volume',
              value: '$runsTotal Total Runs'),
          const Divider(height: 24, thickness: 1),
          _PlanSummaryRow(
              icon: Icons.calendar_today_rounded,
              iconColor: const Color(0xFF047857),
              iconBgColor: const Color(0xFFD1FAE5),
              label: 'Duration',
              value: '$totalWeeks Weeks'),
        ],
      ),
    );
  }
}

class _PlanSummaryRow extends StatelessWidget {
  const _PlanSummaryRow(
      {required this.icon,
      required this.iconColor,
      required this.iconBgColor,
      required this.label,
      required this.value});
  final IconData icon;
  final Color iconColor;
  final Color iconBgColor;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 32,
          height: 32,
          decoration: BoxDecoration(color: iconBgColor, shape: BoxShape.circle),
          child: Icon(icon, color: iconColor, size: 16),
        ),
        const SizedBox(width: 12),
        Expanded(
            child: Text(label,
                style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                    color: AppColors.textSecondary))),
        Text(value,
            style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary)),
      ],
    );
  }
}
