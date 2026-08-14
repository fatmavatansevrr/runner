import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/network/long_horizon_dtos.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/widgets/app_button.dart';
import '../../plan/data/long_horizon_repository.dart';
import '../../plan/data/long_horizon_provider.dart';
import '../../plan/data/long_horizon_error_mapper.dart';

/// Detail + outcome-recording screen for one rolling (Long-Horizon) session.
/// Completion and not-today are each a single, explicit, user-initiated
/// mutation — this screen never auto-submits, never chains into activation,
/// and never guesses an outcome.
class RollingSessionDetailPage extends ConsumerStatefulWidget {
  const RollingSessionDetailPage({super.key, required this.sessionId});
  final String sessionId;

  @override
  ConsumerState<RollingSessionDetailPage> createState() =>
      _RollingSessionDetailPageState();
}

class _RollingSessionDetailPageState
    extends ConsumerState<RollingSessionDetailPage> {
  bool _mutating = false;
  bool _outcomeCommitted = false;
  final _distanceController = TextEditingController();
  final _durationController = TextEditingController();

  @override
  void dispose() {
    _distanceController.dispose();
    _durationController.dispose();
    super.dispose();
  }

  Future<void> _complete() async {
    // Phase 4L.5B: blocks a rapid double tap from firing two completion
    // requests -- mirrors the equivalent guard already present in
    // LongHorizonPlanPreviewPage._onConfirm.
    if (_mutating) return;
    final distance = double.tryParse(_distanceController.text);
    final duration = int.tryParse(_durationController.text);
    if (distance == null ||
        distance <= 0 ||
        duration == null ||
        duration <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
          content: Text('Enter a valid distance and duration.')));
      return;
    }
    setState(() => _mutating = true);
    try {
      // Captured before invalidating -- the real AssignedDate this
      // completion belongs to, so the exact affected Calendar month can be
      // invalidated (never a guessed "current month"; Phase 4L.5A Part 9).
      final assignedDate = ref
          .read(rollingSessionDetailProvider(widget.sessionId))
          .value
          ?.session
          .assignedDate;
      final repo = ref.read(longHorizonRepositoryProvider);
      await repo.completeRollingSession(widget.sessionId,
          actualDistanceKm: distance, actualDurationMinutes: duration);
      _finishCommittedOutcome(assignedDate);
    } catch (e) {
      if (_isAmbiguousMutationError(e)) {
        await _verifyCompletion(distance, duration);
      } else {
        _handleDefinitiveMutationError(e);
      }
    } finally {
      if (mounted) setState(() => _mutating = false);
    }
  }

  Future<void> _notToday(NotTodayReason reason) async {
    if (_mutating) return;
    setState(() => _mutating = true);
    try {
      final assignedDate = ref
          .read(rollingSessionDetailProvider(widget.sessionId))
          .value
          ?.session
          .assignedDate;
      final repo = ref.read(longHorizonRepositoryProvider);
      await repo.markRollingSessionNotToday(widget.sessionId, reason);
      _finishCommittedOutcome(assignedDate);
    } catch (e) {
      if (_isAmbiguousMutationError(e)) {
        await _verifyNotToday(reason);
      } else {
        _handleDefinitiveMutationError(e);
      }
    } finally {
      if (mounted) setState(() => _mutating = false);
    }
  }

  bool _isAmbiguousMutationError(Object error) =>
      error is! ApiException ||
      error.statusCode == null ||
      error.statusCode == 408 ||
      error.statusCode == 504;

  Future<void> _verifyCompletion(double distance, int duration) async {
    try {
      final detail = await ref
          .read(longHorizonRepositoryProvider)
          .fetchRollingSessionDetail(widget.sessionId);
      final session = detail.session;
      if (session.outcome == RollingSessionOutcome.completed) {
        final distanceMatches = session.actualDistanceKm != null &&
            (session.actualDistanceKm! - distance).abs() < 0.001;
        final durationMatches = session.actualDurationMinutes == duration;
        if (distanceMatches && durationMatches) {
          _finishCommittedOutcome(session.assignedDate);
        } else {
          _refreshDetailAndShow(
            'This session was completed with different values. The saved values are shown below.',
          );
        }
      } else if (session.outcome == RollingSessionOutcome.planned) {
        _refreshDetailAndShow(
          "The session is still planned, so the update was not confirmed. You can try again.",
        );
      } else {
        _refreshDetailAndShow(
          'This session was marked not today elsewhere. The saved outcome is shown below.',
        );
      }
    } catch (_) {
      _showMessage(
        "We couldn't verify whether the session was completed. Check your connection and try again.",
      );
    }
  }

  Future<void> _verifyNotToday(NotTodayReason requestedReason) async {
    try {
      final detail = await ref
          .read(longHorizonRepositoryProvider)
          .fetchRollingSessionDetail(widget.sessionId);
      final session = detail.session;
      if (session.outcome == RollingSessionOutcome.notToday) {
        final publicReason = session.notTodayReasonCategory;
        if (publicReason == null ||
            publicReason.isEmpty ||
            publicReason == requestedReason.wireValue) {
          _finishCommittedOutcome(session.assignedDate);
        } else {
          _refreshDetailAndShow(
            'This session was marked not today with different saved information.',
          );
        }
      } else if (session.outcome == RollingSessionOutcome.planned) {
        _refreshDetailAndShow(
          "The session is still planned, so the update was not confirmed. You can try again.",
        );
      } else {
        _refreshDetailAndShow(
          'This session was completed elsewhere. The saved outcome is shown below.',
        );
      }
    } catch (_) {
      _showMessage(
        "We couldn't verify whether the session was updated. Check your connection and try again.",
      );
    }
  }

  void _finishCommittedOutcome(String? assignedDate) {
    if (_outcomeCommitted) return;
    _outcomeCommitted = true;
    invalidateLongHorizonHomeState(ref);
    if (assignedDate != null) {
      invalidateLongHorizonCalendarMonth(ref, monthKeyForDate(assignedDate));
    }
    ref.invalidate(rollingSessionDetailProvider(widget.sessionId));
    if (mounted) context.pop();
  }

  void _handleDefinitiveMutationError(Object error) {
    final mapped = LongHorizonUiErrorMapper.map(error);
    if (mapped.action == LongHorizonErrorAction.refreshDetail) {
      ref.invalidate(rollingSessionDetailProvider(widget.sessionId));
    }
    _showMessage(mapped.userMessage);
  }

  void _refreshDetailAndShow(String message) {
    ref.invalidate(rollingSessionDetailProvider(widget.sessionId));
    _showMessage(message);
  }

  void _showMessage(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }

  Future<void> _showNotTodaySheet() async {
    final reason = await showModalBottomSheet<NotTodayReason>(
      context: context,
      builder: (context) => SafeArea(
        // Scrollable, not a bare Column -- the default modal-bottom-sheet
        // max-height constraint can be shorter than the header + 6 reason
        // rows on a small viewport, which previously overflowed instead of
        // scrolling (Phase 4L.5B finding).
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text('Why not today?',
                    style:
                        TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
              ),
              ...NotTodayReason.values.map(
                (r) => ListTile(
                    title: Text(r.label),
                    onTap: () => Navigator.of(context).pop(r)),
              ),
            ],
          ),
        ),
      ),
    );
    if (reason != null) await _notToday(reason);
  }

  @override
  Widget build(BuildContext context) {
    final asyncDetail =
        ref.watch(rollingSessionDetailProvider(widget.sessionId));

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
          title: const Text('Session'),
          backgroundColor: Colors.white,
          elevation: 0),
      body: SafeArea(
        child: asyncDetail.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (err, _) => Center(
            child: Text(LongHorizonUiErrorMapper.map(err).userMessage),
          ),
          data: (detail) => _buildDetail(detail),
        ),
      ),
    );
  }

  Widget _buildDetail(LongHorizonRollingSessionDetailResponse detail) {
    final session = detail.session;
    final canMutate = session.mutationAllowed &&
        session.outcome == RollingSessionOutcome.planned;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(session.workoutRole.label,
              style: const TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary)),
          const SizedBox(height: 4),
          Text(
              '${session.assignedDate} • ${session.plannedDistanceKm.toStringAsFixed(1)} km',
              style: const TextStyle(
                  fontSize: 14, color: AppColors.textSecondary)),
          const SizedBox(height: 12),
          Text(detail.publicDescription,
              style: const TextStyle(
                  fontSize: 14, color: AppColors.textSecondary)),
          const SizedBox(height: 24),
          if (session.outcome == RollingSessionOutcome.completed)
            _outcomeBanner(
                'Completed',
                Icons.check_circle_rounded,
                const Color(0xFF00A97F),
                session.actualDistanceKm != null
                    ? '${session.actualDistanceKm!.toStringAsFixed(1)} km in ${session.actualDurationMinutes} min'
                    : null)
          else if (session.outcome == RollingSessionOutcome.notToday)
            _outcomeBanner('Marked not today',
                Icons.remove_circle_outline_rounded, AppColors.textMuted, null)
          else if (canMutate) ...[
            TextField(
              controller: _distanceController,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              decoration:
                  const InputDecoration(labelText: 'Actual distance (km)'),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _durationController,
              keyboardType: TextInputType.number,
              decoration:
                  const InputDecoration(labelText: 'Actual duration (minutes)'),
            ),
            const SizedBox(height: 20),
            AppPrimaryButton(
              label: 'Mark complete',
              isLoading: _mutating,
              onPressed: _mutating ? null : _complete,
            ),
            const SizedBox(height: 12),
            TextButton(
              onPressed: _mutating ? null : _showNotTodaySheet,
              child: const Text('Not today'),
            ),
          ] else
            const Text('This session can no longer be updated.',
                style: TextStyle(fontSize: 13, color: AppColors.textSecondary)),
        ],
      ),
    );
  }

  Widget _outcomeBanner(
      String label, IconData icon, Color color, String? subtitle) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        children: [
          Icon(icon, color: color),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label,
                  style: TextStyle(fontWeight: FontWeight.w700, color: color)),
              if (subtitle != null)
                Text(subtitle,
                    style: const TextStyle(
                        fontSize: 12, color: AppColors.textSecondary)),
            ],
          ),
        ],
      ),
    );
  }
}
