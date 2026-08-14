import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/routing/app_router.dart';
import '../../../core/network/bootstrap_provider.dart';
import '../../../core/widgets/app_button.dart';
import '../data/plan_repository.dart';
import '../data/long_horizon_provider.dart';
import '../../home/data/home_provider.dart';
import '../../calendar/data/calendar_provider.dart';
import '../../profile/data/profile_provider.dart';
import '../../onboarding/data/onboarding_provider.dart';

/// Shown when the active plan's readiness card reports
/// `LongHorizonRecoveryRequirement.regeneratePreviewRequired` -- the plan
/// cannot safely continue and the only approved recovery path is: stop the
/// current plan, then start a new one. This screen reuses the existing,
/// already-shared `PlanRepository.cancelPlan` endpoint (the same one
/// `ProfilePage`'s "Stop Plan" dialog already uses, and which the backend
/// confirms supports RollingLongHorizon plans identically to static ones —
/// no new endpoint was needed).
///
/// Cancellation here is never automatic: opening this screen cancels
/// nothing, and no replacement preview/confirmation is ever created by this
/// screen -- the user always lands back on race-plan creation to build a
/// new one themselves.
class LongHorizonRegeneratePlanPage extends ConsumerStatefulWidget {
  const LongHorizonRegeneratePlanPage({super.key});

  @override
  ConsumerState<LongHorizonRegeneratePlanPage> createState() =>
      _LongHorizonRegeneratePlanPageState();
}

class _LongHorizonRegeneratePlanPageState
    extends ConsumerState<LongHorizonRegeneratePlanPage> {
  bool _cancelling = false;
  bool _transitioned = false;
  String? _errorMessage;

  Future<void> _requestStopAndContinue() async {
    final confirmed = await showDialog<bool>(
      context: context,
      barrierDismissible: !_cancelling,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Stop your current plan?'),
        content: const Text(
          'This stops the current plan and cannot be undone through this flow. '
          'No new plan will be created automatically.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Keep current plan'),
          ),
          Semantics(
            button: true,
            label: 'Stop current plan and continue, destructive action',
            child: TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(true),
              child: const Text('Stop plan'),
            ),
          ),
        ],
      ),
    );
    if (confirmed == true && mounted) await _confirmStopAndContinue();
  }

  Future<void> _confirmStopAndContinue() async {
    if (_cancelling) return; // duplicate-tap guard
    final planId = ref.read(activePlanDetailsProvider).valueOrNull?.planId;
    if (planId == null || planId.isEmpty) {
      setState(() => _errorMessage =
          "We couldn't find your current plan. Please try again.");
      return;
    }
    setState(() {
      _cancelling = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(planRepositoryProvider)
          .cancelPlan(planId, 'Regenerate: plan could not safely continue.');
      await _verifyCancellation(planId);
    } catch (e) {
      await _verifyCancellation(planId, mutationError: e);
    } finally {
      if (mounted) setState(() => _cancelling = false);
    }
  }

  /// A failed or ambiguous (timed out / lost) cancellation response is
  /// never treated as proof of failure -- the authoritative active-plan
  /// read decides. A 404 from the cancel call itself already means "no
  /// longer an active plan owned by this user" (the backend's own
  /// not-found-if-not-Active semantics), which is also committed; any
  /// other error re-reads active-plan details to check.
  Future<void> _verifyCancellation(String originalPlanId,
      {Object? mutationError}) async {
    try {
      ref.invalidate(activePlanDetailsProvider);
      final details = await ref.read(activePlanDetailsProvider.future);
      if (!details.hasActivePlan) {
        _onCancellationCommitted();
        return;
      }
      if (mounted) {
        final changedPlan = details.planId != originalPlanId;
        setState(() => _errorMessage = changedPlan
            ? 'Your active plan changed. Return to Home and review the current plan before trying again.'
            : 'We could not confirm that your plan stopped. Please try again.');
      }
    } catch (_) {
      // The verification read itself failed -- preserve current UI and let
      // the user retry explicitly; never delete local active-plan state on
      // an unverified failure.
      if (mounted) {
        setState(() => _errorMessage =
            'A network problem stopped us from confirming your plan status. Please try again.');
      }
    }
  }

  void _onCancellationCommitted() {
    if (_transitioned) return;
    _transitioned = true;
    ref.invalidate(bootstrapDataProvider);
    ref.invalidate(homeDataProvider);
    ref.invalidate(calendarDataProvider);
    ref.invalidate(profileOverviewProvider);
    ref.invalidate(activePlanDetailsProvider);
    ref.invalidate(activeHomeResultProvider);
    ref.invalidate(activeCalendarResultProvider);
    // Guarantees "Create a plan" starts genuinely empty -- no stale answers
    // and, critically, no auto-generated replacement preview or
    // confirmation of any kind.
    ref.read(onboardingProvider.notifier).reset();
    if (mounted) {
      // go() replaces the whole stack -- system/app back can never return
      // to the now-cancelled, no-longer-executable Home screen.
      context.go(AppRoutes.goalSelection);
    }
  }

  @override
  Widget build(BuildContext context) {
    // Eagerly watched (not just read inside the button handler) so the
    // provider has already resolved by the time the user can tap the
    // confirm action -- mirrors the same eager-watch requirement the
    // Home/Calendar dispatchers have (see the harness's own doc comment on
    // this exact trap, Phase 4L.5B §5).
    ref.watch(activePlanDetailsProvider);
    return PopScope(
      canPop: !_cancelling,
      child: Scaffold(
        backgroundColor: AppColors.background,
        appBar: AppBar(
          title: const Text('Create a New Plan'),
          backgroundColor: Colors.white,
          elevation: 0,
        ),
        body: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  "Your current plan can't safely continue",
                  style: TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary),
                ),
                const SizedBox(height: 16),
                const Text(
                  'To keep training toward your goal, you\'ll need to create a new plan using your '
                  'current information. Your existing plan must be stopped first before a new one can '
                  'be created.',
                  style: TextStyle(
                      fontSize: 15,
                      color: AppColors.textSecondary,
                      height: 1.5),
                ),
                const SizedBox(height: 12),
                const Text(
                  'Stopping your plan is final and can\'t be undone from this screen. Your completed '
                  'workout history stays exactly as it is today.',
                  style: TextStyle(
                      fontSize: 15,
                      color: AppColors.textSecondary,
                      height: 1.5),
                ),
                const SizedBox(height: 12),
                const Text(
                  'A new plan is never created automatically -- after stopping this one, you\'ll build '
                  'your new plan yourself, the same way you created this one.',
                  style: TextStyle(
                      fontSize: 15,
                      color: AppColors.textSecondary,
                      height: 1.5),
                ),
                if (_errorMessage != null) ...[
                  const SizedBox(height: 20),
                  Semantics(
                    liveRegion: true,
                    child: Text(_errorMessage!,
                        style: const TextStyle(
                            color: Colors.redAccent, fontSize: 13)),
                  ),
                ],
                const SizedBox(height: 32),
                AppPrimaryButton(
                  label: 'Stop current plan and continue',
                  isLoading: _cancelling,
                  onPressed: _cancelling ? null : _requestStopAndContinue,
                ),
                const SizedBox(height: 12),
                TextButton(
                  onPressed: _cancelling ? null : () => context.pop(),
                  child: const Text('Cancel'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
