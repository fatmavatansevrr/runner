import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/routing/app_router.dart';
import '../data/onboarding_provider.dart';
import '../../plan/data/plan_repository.dart';
import '../../../core/network/bootstrap_provider.dart';
import '../../../core/widgets/app_button.dart';
import '../../home/data/home_provider.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';
import 'widgets/plan_preview_schedule.dart';

class PlanPreviewPage extends ConsumerStatefulWidget {
  const PlanPreviewPage({super.key});

  @override
  ConsumerState<PlanPreviewPage> createState() => _PlanPreviewPageState();
}

class _PlanPreviewPageState extends ConsumerState<PlanPreviewPage> {
  bool _isConfirming = false;

  // Phase 4H.2 (PART 13): interaction pattern B -- the first week starts
  // expanded (immediate real content, not an empty-looking list), every
  // other week starts collapsed. Deterministic (always week 1, regardless
  // of response), and keyed by week NUMBER (not list index) so toggling
  // survives whatever order the backend returns weeks in.
  final Set<int> _expandedWeekNumbers = {1};

  void _toggleWeek(int weekNumber) {
    setState(() {
      if (!_expandedWeekNumbers.remove(weekNumber)) {
        _expandedWeekNumbers.add(weekNumber);
      }
    });
  }

  void _onConfirm(String previewId) async {
    // Phase 4H.1 (PART 12): confirm is only ever invoked from the CTA's
    // onPressed, which is itself null (disabled) whenever
    // `!state.isPreviewConfirmable` -- this guard is defense-in-depth, not
    // the only enforcement point, so a non-confirmable lifecycle can never
    // reach the repository even if a caller is added later that forgets to
    // check the button's enabled state first.
    if (_isConfirming) return; // PART 12: block duplicate taps while in flight.
    setState(() => _isConfirming = true);
    try {
      final repo = ref.read(planRepositoryProvider);
      await repo.confirmPlan(previewId);
      // The plan is now active — refresh every screen that reads plan state.
      ref.invalidate(bootstrapDataProvider);
      ref.invalidate(homeDataProvider);
      ref.invalidate(calendarDataProvider);
      ref.invalidate(profileOverviewProvider);
      ref.invalidate(activePlanDetailsProvider);
      // Clear every onboarding answer (preview_id included) now that it has
      // been consumed by a successful confirm — cleanup must complete
      // before navigating away. Never done on the failure path below, so a
      // failed confirm always preserves the user's answers for retry.
      ref.read(onboardingProvider.notifier).reset();
      if (mounted) {
        context.go(AppRoutes.home);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to confirm plan: ${e.toString()}')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isConfirming = false);
      }
    }
  }

  /// Format goalDistance enum value into a human-readable label
  String _goalLabel(String goalType, String goalDistance) {
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

  // Running Background V2 canonical values, plus legacy aliases for any
  // preview response generated before the migration.
  // Running Background V2.1: the backend never emits a legacy alias in any
  // response (only these four canonical values), and the frontend model no
  // longer accepts them either — so this display-only mapping only ever
  // needs to cover the canonical contract.
  String _levelLabel(String level) => switch (level) {
        'beginner' => 'Beginner',
        'intermediate' => 'Intermediate',
        'advanced' => 'Advanced',
        'experienced' => 'Experienced',
        _ => level,
      };

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(onboardingProvider);
    final preview = state.previewResponse;

    // Defensive empty state: this page should only be reached after
    // generate-preview succeeds (see the previous onboarding step), but if
    // it's ever opened without a preview in local state, show a clear way
    // back instead of fabricating fake plan data.
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
                    onPressed: () {
                      if (context.canPop()) {
                        context.pop();
                      } else {
                        context.go(AppRoutes.startDate);
                      }
                    },
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    }

    // Phase 4H.1 (PART 9): response-driven duration text -- never a
    // hardcoded "12-week plan". For a Preparation Runway preview, the
    // runway/Core split is shown as a secondary line using the real
    // response-derived counts (never a recalculated or assumed 12).
    final durationValue = state.isPreparationRunwayPreview
        ? '${state.totalPreviewWeekCount}-week plan'
        : '${preview.weeks.length} weeks';
    final durationSubtitle = state.isPreparationRunwayPreview
        ? '${state.runwayWeekCount} weeks preparation • ${state.coreWeekCount} weeks race-specific core'
        : null;

    final rows = [
      _PreviewRow(
        icon: Icons.flag_rounded,
        iconColor: const Color(0xFF2B5BFF),
        iconBg: const Color(0xFFD6E0FF),
        label: 'GOAL',
        value: _goalLabel(preview.goalType, preview.goalDistance),
      ),
      _PreviewRow(
        icon: Icons.calendar_today_rounded,
        iconColor: const Color(0xFF00A97F),
        iconBg: const Color(0xFFD0F5EA),
        label: 'DURATION',
        value: durationValue,
        subtitle: durationSubtitle,
      ),
      _PreviewRow(
        icon: Icons.bolt_rounded,
        iconColor: const Color(0xFFF5A623),
        iconBg: const Color(0xFFFFF0D0),
        label: 'WEEKLY STRUCTURE',
        value: '${preview.daysPerWeek} days per week',
      ),
      _PreviewRow(
        icon: Icons.person_rounded,
        iconColor: const Color(0xFF8B5CF6),
        iconBg: const Color(0xFFEDE9FE),
        label: 'LEVEL',
        value: _levelLabel(preview.level),
      ),
    ];

    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Top progress bar ─────────────────────────────────────────────
            const _OnboardingProgressBar(),
            const SizedBox(height: 8),

            // ── Back arrow ───────────────────────────────────────────────────
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: IconButton(
                icon: const Icon(Icons.arrow_back_rounded,
                    color: AppColors.textPrimary),
                onPressed: () => context.go(AppRoutes.startDate),
              ),
            ),

            // ── Scrollable body ──────────────────────────────────────────────
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Heading
                    const Text(
                      'Plan Preview',
                      style: TextStyle(
                        fontSize: 30,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                        height: 1.2,
                      ),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      "Here's your personalised running plan.\nYou can review and customize it next.",
                      style: TextStyle(
                        fontSize: 15,
                        color: AppColors.textSecondary,
                        height: 1.5,
                      ),
                    ),
                    const SizedBox(height: 36),

                    // Info rows
                    Container(
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: AppColors.border),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: 0.04),
                            blurRadius: 16,
                            offset: const Offset(0, 4),
                          ),
                        ],
                      ),
                      child: Column(
                        children: List.generate(rows.length, (i) {
                          return Column(
                            children: [
                              rows[i],
                              if (i < rows.length - 1)
                                const Divider(
                                  height: 1,
                                  indent: 68,
                                  endIndent: 16,
                                  color: AppColors.border,
                                ),
                            ],
                          );
                        }),
                      ),
                    ),

                    const SizedBox(height: 24),

                    // Plan name chip
                    Center(
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 8),
                        decoration: BoxDecoration(
                          color: AppColors.primaryLight,
                          borderRadius: BorderRadius.circular(100),
                        ),
                        child: Text(
                          '${preview.goalType == 'race' ? '🏁' : '🏃'} ${_goalLabel(preview.goalType, preview.goalDistance)} Plan',
                          style: const TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: AppColors.primary,
                          ),
                        ),
                      ),
                    ),

                    const SizedBox(height: 32),

                    // ── Plan Schedule (Phase 4H.2) ───────────────────────────
                    // Same SingleChildScrollView as the summary above -- not
                    // a second/nested scrollable (PART 4).
                    PlanScheduleSection(
                      response: preview,
                      expandedWeekNumbers: _expandedWeekNumbers,
                      onToggleWeek: _toggleWeek,
                    ),
                  ],
                ),
              ),
            ),

            // ── Non-confirmable lifecycle notice (PART 11) ─────────────────────
            // The preview itself remains fully visible above; only the
            // confirm action is affected. No raw backend error code/feature-
            // gate terminology is ever shown here.
            if (!state.isPreviewConfirmable)
              Padding(
                padding: const EdgeInsets.fromLTRB(24, 0, 24, 12),
                child: Text(
                  'This plan is available for preview, but activation is not '
                  'currently available.',
                  textAlign: TextAlign.center,
                  style:
                      TextStyle(fontSize: 13, color: AppColors.textSecondary),
                ),
              ),

            // ── Bottom CTA ───────────────────────────────────────────────────
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 0, 24, 24),
              child: AppPrimaryButton(
                label: 'Looks good, continue',
                isLoading: _isConfirming,
                icon: Icons.arrow_forward_rounded,
                // Phase 4H.1 (PART 11): gated exclusively on lifecycle --
                // never on week count. `unknown` fails closed (disabled),
                // matching PreviewLifecycle.isConfirmable's own semantics.
                onPressed: state.isPreviewConfirmable
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

// ─────────────────────────────────────────────────────────────────────────────
// Progress bar at the top of onboarding pages
// ─────────────────────────────────────────────────────────────────────────────

class _OnboardingProgressBar extends StatelessWidget {
  const _OnboardingProgressBar();

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(0),
      child: LinearProgressIndicator(
        value: 1.0,
        minHeight: 3,
        backgroundColor: AppColors.border,
        color: AppColors.primary,
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Single icon-row info item
// ─────────────────────────────────────────────────────────────────────────────

class _PreviewRow extends StatelessWidget {
  const _PreviewRow({
    required this.icon,
    required this.iconColor,
    required this.iconBg,
    required this.label,
    required this.value,
    this.subtitle,
  });

  final IconData icon;
  final Color iconColor;
  final Color iconBg;
  final String label;
  final String value;

  /// Optional second line (PART 9: runway/Core week split for a
  /// Preparation Runway preview). Null for every other row.
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
      child: Row(
        children: [
          // Icon box
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: iconBg,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: iconColor, size: 22),
          ),
          const SizedBox(width: 14),
          // Labels
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textMuted,
                    letterSpacing: 0.6,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textPrimary,
                  ),
                ),
                if (subtitle != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    subtitle!,
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ],
            ),
          ),
          const Icon(Icons.chevron_right_rounded,
              color: AppColors.textMuted, size: 20),
        ],
      ),
    );
  }
}
