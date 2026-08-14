import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../../core/models/preparation_runway.dart';
import '../../../../core/network/dtos.dart';
import '../../../../core/theme/app_colors.dart';

/// Phase 4H.2 — the response-driven week-by-week and workout-by-workout
/// preview schedule. Renders exactly `response.weeks` in backend order, with
/// no client-side generation, reordering, or renumbering. Placed inside the
/// existing `PlanPreviewPage`'s `SingleChildScrollView` (not a second
/// scrollable) — a plain `Column` of up to 20 week cards (each expandable to
/// up to 4 workout rows) is a modest, bounded widget count that stays
/// performant without slivers/lazy builders (see PHASE4H_2_...md §9/§18 for
/// the explicit performance verification).
class PlanScheduleSection extends StatelessWidget {
  const PlanScheduleSection({
    super.key,
    required this.response,
    required this.expandedWeekNumbers,
    required this.onToggleWeek,
  });

  final GeneratePreviewResponse response;
  final Set<int> expandedWeekNumbers;
  final void Function(int weekNumber) onToggleWeek;

  @override
  Widget build(BuildContext context) {
    if (response.weeks.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 24),
        child: Text(
          'No schedule details are available for this preview.',
          style: TextStyle(fontSize: 14, color: AppColors.textSecondary),
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Plan Schedule',
          style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary),
        ),
        const SizedBox(height: 12),
        if (response.isPreparationRunwayPlan)
          _SegmentOverview(response: response),
        const SizedBox(height: 16),
        for (var i = 0; i < response.weeks.length; i++) ...[
          _WeekCard(
            // PART 6/17: keyed by preview ID + list POSITION, not just
            // weekNumber -- a malformed backend response with a duplicate
            // week_number must still produce unique widget keys (Flutter
            // throws "Duplicate keys found" otherwise, discovered by a real
            // test with duplicate week numbers, not assumed).
            key: ValueKey('${response.previewId}-week-$i-${response.weeks[i].weekNumber}'),
            week: response.weeks[i],
            // PART 7: the Runway/Core boundary is detected from the typed
            // week type of this week vs. the previous one -- never from
            // week-number arithmetic.
            isFirstCoreWeekAfterRunway: i > 0 &&
                response.weeks[i].weekTypeValue !=
                    PreviewWeekType.preparationRunway &&
                response.weeks[i - 1].weekTypeValue ==
                    PreviewWeekType.preparationRunway,
            expanded:
                expandedWeekNumbers.contains(response.weeks[i].weekNumber),
            onToggle: () => onToggleWeek(response.weeks[i].weekNumber),
          ),
          const SizedBox(height: 10),
        ],
      ],
    );
  }
}

/// PART 5: response-derived runway/Core segment counts. Never shown for a
/// Core-only preview (the caller only builds this widget when
/// `response.isPreparationRunwayPlan`), and never infers "12 Core weeks"
/// from total duration -- `coreWeekCount` is `weeks.length - runwayWeekCount`
/// over the actual parsed collection.
class _SegmentOverview extends StatelessWidget {
  const _SegmentOverview({required this.response});

  final GeneratePreviewResponse response;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: 'Preparation ${response.runwayWeekCount} weeks, '
          'Race-Specific Core ${response.coreWeekCount} weeks',
      child: Row(
        children: [
          Expanded(
              child: _SegmentChip(
                  title: 'Preparation',
                  weeks: response.runwayWeekCount,
                  color: const Color(0xFFF5A623))),
          const SizedBox(width: 10),
          Expanded(
              child: _SegmentChip(
                  title: 'Race-Specific Core',
                  weeks: response.coreWeekCount,
                  color: AppColors.primary)),
        ],
      ),
    );
  }
}

class _SegmentChip extends StatelessWidget {
  const _SegmentChip(
      {required this.title, required this.weeks, required this.color});

  final String title;
  final int weeks;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: color.withValues(alpha: 0.30)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title,
              style: TextStyle(
                  fontSize: 11, fontWeight: FontWeight.w700, color: color)),
          const SizedBox(height: 2),
          Text('$weeks week${weeks == 1 ? '' : 's'}',
              style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary)),
        ],
      ),
    );
  }
}

/// PART 6/7/8/13: one week's header (always visible) plus its workouts
/// (visible only when [expanded]). Header shows the global week number
/// (never renumbered/reset), the major segment label (Preparation Runway or
/// the Core phase label), and — for a runway week — the runway block as a
/// secondary label. A boundary divider renders immediately above the first
/// Core week that follows a runway week.
class _WeekCard extends StatelessWidget {
  const _WeekCard({
    super.key,
    required this.week,
    required this.isFirstCoreWeekAfterRunway,
    required this.expanded,
    required this.onToggle,
  });

  final PreviewWeekDto week;
  final bool isFirstCoreWeekAfterRunway;
  final bool expanded;
  final VoidCallback onToggle;

  bool get _isRunway => week.weekTypeValue == PreviewWeekType.preparationRunway;

  String get _majorLabel =>
      _isRunway ? 'Preparation Runway' : week.weekTypeValue.label;

  /// PART 8: a runway week with no `runway_block` (malformed/legacy) still
  /// renders "Preparation Runway" as the major label and a safe secondary
  /// label here -- never silently reclassified as Core.
  String? get _secondaryLabel {
    if (!_isRunway) return null;
    if (week.runwayBlock == null) return 'Preparation Block';
    return week.runwayBlockValue!.label;
  }

  String get _semanticsLabel {
    final parts = <String>['Week ${week.weekNumber}', _majorLabel];
    if (_secondaryLabel != null) parts.add(_secondaryLabel!);
    return parts.join(', ');
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (isFirstCoreWeekAfterRunway) const _SegmentBoundaryDivider(),
        Semantics(
          button: true,
          label: _semanticsLabel,
          value: expanded ? 'Expanded' : 'Collapsed',
          child: Material(
            color: Colors.white,
            borderRadius: BorderRadius.circular(16),
            child: InkWell(
              borderRadius: BorderRadius.circular(16),
              onTap: onToggle,
              child: Container(
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(
                      color: _isRunway
                          ? const Color(0xFFF5A623).withValues(alpha: 0.35)
                          : AppColors.border),
                ),
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('Week ${week.weekNumber}',
                                  style: const TextStyle(
                                      fontSize: 13,
                                      fontWeight: FontWeight.w700,
                                      color: AppColors.textMuted)),
                              const SizedBox(height: 2),
                              Text(_majorLabel,
                                  style: const TextStyle(
                                      fontSize: 16,
                                      fontWeight: FontWeight.w700,
                                      color: AppColors.textPrimary)),
                              if (_secondaryLabel != null)
                                Padding(
                                  padding: const EdgeInsets.only(top: 2),
                                  child: Text(_secondaryLabel!,
                                      style: const TextStyle(
                                          fontSize: 13,
                                          color: AppColors.textSecondary)),
                                ),
                            ],
                          ),
                        ),
                        Text(
                            '${week.days.length} session${week.days.length == 1 ? '' : 's'}',
                            style: const TextStyle(
                                fontSize: 12, color: AppColors.textMuted)),
                        const SizedBox(width: 6),
                        Icon(
                            expanded
                                ? Icons.expand_less_rounded
                                : Icons.expand_more_rounded,
                            color: AppColors.textMuted),
                      ],
                    ),
                    if (expanded) ...[
                      const SizedBox(height: 10),
                      if (week.days.isEmpty)
                        const Text('No sessions for this week.',
                            style: TextStyle(
                                fontSize: 13, color: AppColors.textSecondary))
                      else
                        for (final day in week.days) _WorkoutRow(day: day),
                    ],
                  ],
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _SegmentBoundaryDivider extends StatelessWidget {
  const _SegmentBoundaryDivider();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          const Expanded(child: Divider(color: AppColors.border)),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 10),
            child: Text('RACE-SPECIFIC CORE BEGINS',
                style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.6,
                    color: AppColors.textMuted)),
          ),
          const Expanded(child: Divider(color: AppColors.border)),
        ],
      ),
    );
  }
}

/// PART 9/10/12: one workout row. Long-run identity comes solely from
/// `day.isLongRun` (backed by the formal `day_type == "long_run"` field —
/// see `PreviewDayDto.isLongRun`), never inferred from slot position.
/// Distance/duration/date use this app's existing formatting conventions
/// (see `home_page.dart`'s `toStringAsFixed`/`DateFormat` usage) — no
/// fabricated numeric pace is ever shown for an effort-only session.
class _WorkoutRow extends StatelessWidget {
  const _WorkoutRow({required this.day});

  final PreviewDayDto day;

  static String _formatDistance(double km) {
    if (km <= 0) return 'Distance not specified';
    final rounded = km.toStringAsFixed(km == km.roundToDouble() ? 0 : 1);
    return '$rounded km';
  }

  static String _formatDuration(int minutes) {
    if (minutes <= 0) return 'Duration not specified';
    return '$minutes min';
  }

  @override
  Widget build(BuildContext context) {
    final dateLabel = DateFormat('EEE, d MMM').format(day.date);
    final typeLabel = day.isLongRun ? 'Long Run' : day.dayTypeValue.label;
    final intensityLabel = day.intensityDetail.label;
    final semanticsLabel = [
      dateLabel,
      typeLabel,
      _formatDistance(day.distanceKm),
      _formatDuration(day.durationMin),
      intensityLabel,
      if (day.isLongRun) 'Long run',
    ].join(', ');

    return Semantics(
      label: semanticsLabel,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              width: 64,
              child: Text(dateLabel,
                  style: const TextStyle(
                      fontSize: 12, color: AppColors.textMuted)),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      // PART 19: long-run is also conveyed via text, not
                      // only the icon.
                      if (day.isLongRun) ...[
                        const Icon(Icons.trending_up_rounded,
                            size: 14, color: Color(0xFF00A97F)),
                        const SizedBox(width: 4),
                      ],
                      Flexible(
                        child: Text(
                          typeLabel,
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: day.isLongRun
                                ? FontWeight.w700
                                : FontWeight.w600,
                            color: AppColors.textPrimary,
                          ),
                        ),
                      ),
                    ],
                  ),
                  Text(
                    '${_formatDistance(day.distanceKm)} • ${_formatDuration(day.durationMin)} • $intensityLabel',
                    style: const TextStyle(
                        fontSize: 12, color: AppColors.textSecondary),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
