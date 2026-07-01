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

class PlanPreviewPage extends ConsumerStatefulWidget {
  const PlanPreviewPage({super.key});

  @override
  ConsumerState<PlanPreviewPage> createState() => _PlanPreviewPageState();
}

class _PlanPreviewPageState extends ConsumerState<PlanPreviewPage> {
  bool _isConfirming = false;

  void _onConfirm(String previewId) async {
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
      'five_k'       => '5 km',
      'ten_k'        => '10 km',
      'half_marathon' => 'Half Marathon',
      'marathon'     => 'Marathon',
      _              => goalDistance,
    };
    final verb = goalType == 'race' ? 'Race' : 'Run';
    return '$verb $distLabel';
  }

  String _levelLabel(String level) => switch (level) {
        'new_to_running'    => 'Beginner',
        'used_to_run'       => 'Returning',
        'running_regularly' => 'Intermediate',
        _                   => level,
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
                  const Icon(Icons.error_outline_rounded, size: 40, color: AppColors.textMuted),
                  const SizedBox(height: 16),
                  const Text(
                    "We couldn't find a plan preview.\nPlease go back and try again.",
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 15, color: AppColors.textSecondary),
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
        value: '${preview.weeks.length} weeks',
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
                icon: const Icon(Icons.arrow_back_rounded, color: AppColors.textPrimary),
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
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
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
                  ],
                ),
              ),
            ),

            // ── Bottom CTA ───────────────────────────────────────────────────
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 0, 24, 24),
              child: AppPrimaryButton(
                label: 'Looks good, continue',
                isLoading: _isConfirming,
                icon: Icons.arrow_forward_rounded,
                onPressed: () => _onConfirm(preview.previewId),
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
  });

  final IconData icon;
  final Color iconColor;
  final Color iconBg;
  final String label;
  final String value;

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
              ],
            ),
          ),
          const Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 20),
        ],
      ),
    );
  }
}
