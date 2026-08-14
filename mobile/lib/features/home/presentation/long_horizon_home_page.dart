import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/routing/app_router.dart';
import '../../../core/network/long_horizon_dtos.dart';
import '../../../core/network/api_exception.dart';
import '../../plan/data/long_horizon_repository.dart';
import '../../plan/data/long_horizon_provider.dart';
import '../../plan/data/long_horizon_error_mapper.dart';

/// Home screen for an active RollingLongHorizon plan. Renders exactly what
/// `GET /plans/active/home` returned for the current window — never
/// fabricates a future workout, never auto-activates the next window, and
/// never auto-retries. Activation/retry are separate, explicit, user-tapped
/// actions surfaced only when the backend's own readiness fields allow it.
class LongHorizonHomePage extends ConsumerStatefulWidget {
  const LongHorizonHomePage({super.key});

  @override
  ConsumerState<LongHorizonHomePage> createState() =>
      _LongHorizonHomePageState();
}

class _LongHorizonHomePageState extends ConsumerState<LongHorizonHomePage> {
  bool _actionInFlight = false;

  Future<void> _activateNextWindow() async {
    if (_actionInFlight) return;
    final before = ref.read(activeHomeResultProvider).value?.rollingHome;
    setState(() => _actionInFlight = true);
    try {
      final response =
          await ref.read(longHorizonRepositoryProvider).activateNextWindow();
      invalidateLongHorizonHomeState(ref);
      // Invalidate exactly the Calendar month(s) the newly activated
      // sessions actually fall in -- derived from their real AssignedDate,
      // never a guessed "current month" (Phase 4L.5A Part 5/Part 9). A
      // window can span a month boundary, so this may be more than one.
      final affectedMonths = response.activatedSessions
          .map((s) => monthKeyForDate(s.assignedDate))
          .toSet();
      for (final month in affectedMonths) {
        invalidateLongHorizonCalendarMonth(ref, month);
      }
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(response.publicMessage)));
      }
    } catch (e) {
      if (_isAmbiguousMutationError(e)) {
        await _verifyActivation(before);
      } else {
        _handleHomeMutationError(e);
      }
    } finally {
      if (mounted) setState(() => _actionInFlight = false);
    }
  }

  Future<void> _retry() async {
    if (_actionInFlight) return;
    final before = ref.read(activeHomeResultProvider).value?.rollingHome;
    setState(() => _actionInFlight = true);
    try {
      final response =
          await ref.read(longHorizonRepositoryProvider).retryContinuation();
      // Retry only restores a Blocked boundary back to Pending -- it never
      // creates or moves a session, so unlike activation it must NOT
      // invalidate any Calendar month (Phase 4L.5A Part 5: "do not
      // invalidate unrelated Calendar months unless backend state changed").
      invalidateLongHorizonHomeState(ref);
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(response.publicMessage)));
      }
    } catch (e) {
      if (_isAmbiguousMutationError(e)) {
        await _verifyRetry(before);
      } else {
        _handleHomeMutationError(e);
      }
    } finally {
      if (mounted) setState(() => _actionInFlight = false);
    }
  }

  bool _isAmbiguousMutationError(Object error) =>
      error is! ApiException ||
      error.statusCode == null ||
      error.statusCode == 408 ||
      error.statusCode == 504;

  Future<void> _verifyActivation(LongHorizonHomeResponse? before) async {
    try {
      final result =
          await ref.read(longHorizonRepositoryProvider).fetchActiveHome();
      final home = result.rollingHome;
      if (home == null) {
        _showMessage('Your active plan changed. Home has been refreshed.');
        invalidateLongHorizonHomeState(ref);
        return;
      }
      final plan = home.activePlan;
      final advanced = before != null &&
          plan.currentWindowEndWeek > before.activePlan.currentWindowEndWeek;
      if (advanced ||
          plan.checkpointReadiness ==
              LongHorizonCheckpointReadiness.currentWindowInProgress) {
        for (final month in home.currentWindowSessions
            .map((session) => monthKeyForDate(session.assignedDate))
            .toSet()) {
          invalidateLongHorizonCalendarMonth(ref, month);
        }
        invalidateLongHorizonHomeState(ref);
        _showMessage('Your next training block is active.');
        return;
      }
      invalidateLongHorizonHomeState(ref);
      if (plan.checkpointReadiness ==
          LongHorizonCheckpointReadiness.nextWindowActivationReady) {
        _showMessage(
          'The next block is still ready. Activation was not confirmed; you can try again.',
        );
      } else if (plan.checkpointReadiness ==
          LongHorizonCheckpointReadiness.reassessmentRequired) {
        _showMessage(
            'Your plan needs attention before another block can be activated.');
      } else if (plan.checkpointReadiness ==
          LongHorizonCheckpointReadiness.terminalPlanComplete) {
        _showMessage('Your plan is complete.');
      }
    } catch (_) {
      _showMessage(
        "We couldn't verify whether the next block was activated. Check your connection and try again.",
      );
    }
  }

  Future<void> _verifyRetry(LongHorizonHomeResponse? before) async {
    try {
      final result =
          await ref.read(longHorizonRepositoryProvider).fetchActiveHome();
      final home = result.rollingHome;
      invalidateLongHorizonHomeState(ref);
      if (home == null) {
        _showMessage('Your active plan changed. Home has been refreshed.');
        return;
      }
      final plan = home.activePlan;
      final stillSameRecovery = before != null &&
          plan.checkpointReadiness == before.activePlan.checkpointReadiness &&
          plan.recoveryRequirement == before.activePlan.recoveryRequirement;
      if (!stillSameRecovery) {
        if (plan.checkpointReadiness ==
            LongHorizonCheckpointReadiness.nextWindowActivationReady) {
          _showMessage(
              'Your plan is ready. Activate the next block when you choose.');
        } else if (plan.recoveryRequirement ==
            LongHorizonRecoveryRequirement.regeneratePreviewRequired) {
          _showMessage('Create a new plan to continue.');
        } else if (plan.recoveryRequirement ==
            LongHorizonRecoveryRequirement.operationalSupportRequired) {
          _showMessage('Please contact support to continue your plan.');
        } else {
          _showMessage('Your plan was refreshed with the latest state.');
        }
      } else {
        _showMessage(
            'The plan still needs attention. You can retry explicitly.');
      }
    } catch (_) {
      _showMessage(
        "We couldn't verify whether retry succeeded. Check your connection and try again.",
      );
    }
  }

  void _handleHomeMutationError(Object error) {
    final mapped = LongHorizonUiErrorMapper.map(error);
    if (mapped.action == LongHorizonErrorAction.refreshHome) {
      invalidateLongHorizonHomeState(ref);
    }
    _showMessage(mapped.userMessage);
  }

  void _showMessage(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    final asyncResult = ref.watch(activeHomeResultProvider);

    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: asyncResult.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (err, _) => Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.error_outline_rounded,
                      size: 40, color: AppColors.textMuted),
                  const SizedBox(height: 12),
                  Text(LongHorizonUiErrorMapper.map(err).userMessage,
                      textAlign: TextAlign.center),
                  const SizedBox(height: 16),
                  TextButton(
                    onPressed: () => ref.invalidate(activeHomeResultProvider),
                    child: const Text('Retry'),
                  ),
                ],
              ),
            ),
          ),
          data: (result) {
            if (result.rollingHome == null) {
              // Strategy resolved to static — should not normally reach
              // this widget (the route dispatcher decides), but fail safe
              // with a clear message instead of a blank/broken screen.
              return const Center(
                  child: Text('This plan uses the standard schedule view.'));
            }
            return _buildRollingHome(result.rollingHome!);
          },
        ),
      ),
    );
  }

  Widget _buildRollingHome(LongHorizonHomeResponse home) {
    final plan = home.activePlan;
    return RefreshIndicator(
      onRefresh: () async => ref.invalidate(activeHomeResultProvider),
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            plan.publicMessage,
            style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary),
          ),
          const SizedBox(height: 4),
          Text(
            'Week ${plan.currentGlobalWeek} of ${plan.totalWeeks} • ${plan.currentPhase}',
            style:
                const TextStyle(fontSize: 14, color: AppColors.textSecondary),
          ),
          const SizedBox(height: 20),
          _ReadinessCard(
            checkpointReadiness: plan.checkpointReadiness,
            recoveryRequirement: plan.recoveryRequirement,
            actionInFlight: _actionInFlight,
            onActivate: _activateNextWindow,
            onRetry: _retry,
            onRegenerate: () =>
                context.push(AppRoutes.longHorizonRegeneratePlan),
          ),
          const SizedBox(height: 20),
          if (home.todayWorkout != null) ...[
            const Text('Today',
                style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary)),
            const SizedBox(height: 8),
            _SessionCard(session: home.todayWorkout!),
            const SizedBox(height: 20),
          ],
          const Text('This block',
              style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary)),
          const SizedBox(height: 8),
          ...home.currentWindowSessions.map((s) => _SessionCard(session: s)),
        ],
      ),
    );
  }
}

class _ReadinessCard extends StatelessWidget {
  const _ReadinessCard({
    required this.checkpointReadiness,
    required this.recoveryRequirement,
    required this.actionInFlight,
    required this.onActivate,
    required this.onRetry,
    required this.onRegenerate,
  });

  final LongHorizonCheckpointReadiness checkpointReadiness;
  final LongHorizonRecoveryRequirement? recoveryRequirement;
  final bool actionInFlight;
  final VoidCallback onActivate;
  final VoidCallback onRetry;
  final VoidCallback onRegenerate;

  @override
  Widget build(BuildContext context) {
    // Only these two readiness states surface an explicit user action.
    // Every other state (in-progress, complete-but-not-yet-ready, terminal)
    // is informational only — no button is ever shown that could imply an
    // action the backend hasn't actually allowed.
    final showActivate = checkpointReadiness ==
        LongHorizonCheckpointReadiness.nextWindowActivationReady;
    final showRetry = recoveryRequirement ==
        LongHorizonRecoveryRequirement.calendarWindowPending;

    if (checkpointReadiness ==
        LongHorizonCheckpointReadiness.terminalPlanComplete) {
      return _card(
        icon: Icons.emoji_events_rounded,
        color: const Color(0xFFF5A623),
        title: 'Plan complete',
        subtitle: "You've completed every training block. Congratulations!",
      );
    }

    if (showActivate) {
      return _card(
        icon: Icons.arrow_circle_up_rounded,
        color: AppColors.primary,
        title: 'Next block ready',
        subtitle: 'Your next training block is ready to activate.',
        actionLabel: 'Activate next block',
        onAction: actionInFlight ? null : onActivate,
      );
    }

    if (showRetry) {
      return _card(
        icon: Icons.refresh_rounded,
        color: const Color(0xFFEF4444),
        title: 'Action needed',
        subtitle:
            'Your training block needs to be restored before you can continue.',
        actionLabel: 'Retry',
        onAction: actionInFlight ? null : onRetry,
      );
    }

    if (recoveryRequirement ==
        LongHorizonRecoveryRequirement.regeneratePreviewRequired) {
      return _card(
        icon: Icons.info_outline_rounded,
        color: AppColors.textSecondary,
        title: 'Plan update needed',
        subtitle:
            "Your current plan can't safely continue. You'll need to create a new one.",
        actionLabel: 'Create a new plan',
        // Never automatic -- navigating here cancels nothing by itself;
        // the regenerate screen requires its own separate explicit
        // confirmation before anything is cancelled.
        onAction: onRegenerate,
      );
    }

    if (recoveryRequirement ==
        LongHorizonRecoveryRequirement.operationalSupportRequired) {
      return _card(
        icon: Icons.support_agent_rounded,
        color: AppColors.textSecondary,
        title: 'Support needed',
        subtitle: 'Please contact support to continue your plan.',
      );
    }

    // currentWindowInProgress / currentWindowComplete — nothing to show.
    return const SizedBox.shrink();
  }

  Widget _card({
    required IconData icon,
    required Color color,
    required String title,
    required String subtitle,
    String? actionLabel,
    VoidCallback? onAction,
  }) {
    return Semantics(
      // Combines title+subtitle into one announced block -- readiness/
      // recovery/terminal state is always conveyed through this text, never
      // through color/icon alone (Phase 4L.5A accessibility audit).
      label: '$title. $subtitle',
      container: true,
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppColors.border),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ExcludeSemantics(child: Icon(icon, color: color, size: 24)),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  ExcludeSemantics(
                      child: Text(title,
                          style: const TextStyle(
                              fontWeight: FontWeight.w700, fontSize: 15))),
                  const SizedBox(height: 4),
                  ExcludeSemantics(
                      child: Text(subtitle,
                          style: const TextStyle(
                              fontSize: 13, color: AppColors.textSecondary))),
                  if (actionLabel != null) ...[
                    const SizedBox(height: 10),
                    Semantics(
                      label: onAction == null
                          ? '$actionLabel, loading'
                          : actionLabel,
                      button: true,
                      enabled: onAction != null,
                      child: ElevatedButton(
                        onPressed: onAction,
                        child: Text(actionLabel),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SessionCard extends StatelessWidget {
  const _SessionCard({required this.session});
  final LongHorizonRollingSessionResponse session;

  @override
  Widget build(BuildContext context) {
    final outcomeIcon = switch (session.outcome) {
      RollingSessionOutcome.completed => Icons.check_circle_rounded,
      RollingSessionOutcome.notToday => Icons.remove_circle_outline_rounded,
      _ => Icons.circle_outlined,
    };
    final outcomeColor = switch (session.outcome) {
      RollingSessionOutcome.completed => const Color(0xFF00A97F),
      RollingSessionOutcome.notToday => AppColors.textMuted,
      _ => AppColors.primary,
    };

    return Builder(builder: (context) {
      return Semantics(
        label:
            '${session.workoutRole.label}, ${session.assignedDate}, ${session.plannedDistanceKm.toStringAsFixed(1)} kilometers',
        button: true,
        child: InkWell(
          onTap: () =>
              context.push('/training-day/rolling/${session.sessionId}'),
          borderRadius: BorderRadius.circular(14),
          child: Container(
            margin: const EdgeInsets.only(bottom: 10),
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.border),
            ),
            child: Row(
              children: [
                ExcludeSemantics(
                    child: Icon(outcomeIcon, color: outcomeColor, size: 22)),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(session.workoutRole.label,
                          style: const TextStyle(
                              fontWeight: FontWeight.w600, fontSize: 14)),
                      Text(
                          '${session.assignedDate} • ${session.plannedDistanceKm.toStringAsFixed(1)} km',
                          style: const TextStyle(
                              fontSize: 12, color: AppColors.textSecondary)),
                    ],
                  ),
                ),
                const ExcludeSemantics(
                    child: Icon(Icons.chevron_right_rounded,
                        color: AppColors.textMuted, size: 20)),
              ],
            ),
          ),
        ),
      );
    });
  }
}
