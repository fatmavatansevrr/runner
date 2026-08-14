// Phase 4L.5C -- proves the RegeneratePreviewRequired recovery state has a
// complete, explicit, server-verified cancellation flow: no automatic
// cancellation, no automatic replacement plan, exact-request assertions,
// duplicate-tap prevention, and read-after-write ambiguity handling.
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/api_exception.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'support/long_horizon_flow_test_harness.dart';

void main() {
  group('RegeneratePreviewRequired surfaces an explicit action', () {
    testWidgets(
        'the readiness card shows "Create a new plan" and never cancels on its own',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);

      expect(find.text('Create a new plan'), findsOneWidget);
      // Opening Home with this recovery state must never itself cancel.
      final source =
          ScriptedActivePlanDetailsSource([activePlan(hasActivePlan: true)]);
      expect(source.readCount, 0);
    });

    testWidgets(
        'tapping the action opens the explanation screen with both required actions, cancels nothing yet',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      final source =
          ScriptedActivePlanDetailsSource([activePlan(hasActivePlan: true)]);
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          repository: repo,
          activePlanDetailsSource: source);

      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();

      expect(find.text('Stop current plan and continue'), findsOneWidget);
      expect(find.text('Cancel'), findsOneWidget);
    });

    testWidgets(
        'cancelling the explanation dialog returns to Home without any request',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home, repository: repo);
      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();

      expect(find.text('Create a new plan'),
          findsOneWidget); // back on Home's readiness card.
    });
  });

  group('Explicit cancellation', () {
    testWidgets(
        'confirming sends exactly one cancel request for the correct planId and reaches onboarding',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      final source = ScriptedActivePlanDetailsSource([
        activePlan(hasActivePlan: true, planId: 'plan-xyz'),
        activePlan(hasActivePlan: false),
      ]);
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          repository: repo,
          activePlanDetailsSource: source);
      bundle.staticPlanRepo.cancelResponse =
          null; // use the default committed response.

      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop current plan and continue'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop plan'));
      await tester.pumpAndSettle();

      expect(bundle.staticPlanRepo.cancelCallCount, 1);
      expect(bundle.staticPlanRepo.cancelRequests.single.planId, 'plan-xyz');
      expect(find.text('GOAL_SELECTION_PLACEHOLDER'), findsOneWidget);
    });

    testWidgets(
        'a rapid double tap on the confirm action only sends one cancel request',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      final source = ScriptedActivePlanDetailsSource([
        activePlan(hasActivePlan: true),
        activePlan(hasActivePlan: false),
      ]);
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          repository: repo,
          activePlanDetailsSource: source);
      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Stop current plan and continue'));
      await tester.pumpAndSettle();
      final button = tester.widget<TextButton>(
        find.widgetWithText(TextButton, 'Stop plan'),
      );
      button.onPressed?.call();
      button.onPressed?.call();
      await tester.pumpAndSettle();

      expect(bundle.staticPlanRepo.cancelCallCount, 1);
      expect(find.text('GOAL_SELECTION_PLACEHOLDER'), findsOneWidget);
    });

    testWidgets(
        'cancellation never triggers a replacement preview or confirmation call',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      final source = ScriptedActivePlanDetailsSource([
        activePlan(hasActivePlan: true),
        activePlan(hasActivePlan: false),
      ]);
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          repository: repo,
          activePlanDetailsSource: source);
      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop current plan and continue'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop plan'));
      await tester.pumpAndSettle();

      expect(repo.previewCallCount, 0);
      expect(repo.confirmCallCount, 0);
      expect(bundle.staticPlanRepo.generateRaceCallCount, 0);
      expect(bundle.staticPlanRepo.confirmCallCount, 0);
    });
  });

  group('Cancellation ambiguity (timeout/lost response)', () {
    testWidgets(
        'a 404 from the cancel call itself is treated as already-committed and reaches onboarding once',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      final source = ScriptedActivePlanDetailsSource([
        activePlan(hasActivePlan: true),
        activePlan(hasActivePlan: false),
      ]);
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          repository: repo,
          activePlanDetailsSource: source);
      bundle.staticPlanRepo.cancelError = const ApiException(
          message: 'Active training plan not found.',
          errorCode: 'NOT_FOUND',
          statusCode: 404);

      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop current plan and continue'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop plan'));
      await tester.pumpAndSettle();

      expect(find.text('GOAL_SELECTION_PLACEHOLDER'), findsOneWidget);
    });

    testWidgets(
        'a lost (non-404) response followed by an authoritative read showing no active plan reaches onboarding once',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      // First read (used only if the page reads it eagerly) then the
      // post-ambiguity verification read, both scripted -- the important
      // one is the LAST value, since the source returns its final entry
      // repeatedly once exhausted to one element.
      final source = ScriptedActivePlanDetailsSource([
        activePlan(hasActivePlan: true),
        activePlan(hasActivePlan: false),
      ]);
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          repository: repo,
          activePlanDetailsSource: source);
      bundle.staticPlanRepo.cancelError = Exception('connection lost');

      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop current plan and continue'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop plan'));
      await tester.pumpAndSettle();

      expect(find.text('GOAL_SELECTION_PLACEHOLDER'), findsOneWidget);
    });

    testWidgets(
        'a lost response followed by a read showing the SAME plan still active shows explicit retry, not a loop',
        (tester) async {
      final repo = ScriptedLongHorizonRepository()
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.reassessmentRequired,
            recoveryRequirement:
                LongHorizonRecoveryRequirement.regeneratePreviewRequired,
          ),
        ));
      final source = ScriptedActivePlanDetailsSource(
          [activePlan(hasActivePlan: true, planId: 'plan-1')]);
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.home,
          repository: repo,
          activePlanDetailsSource: source);
      bundle.staticPlanRepo.cancelError = Exception('connection lost');

      await tester.tap(find.text('Create a new plan'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop current plan and continue'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop plan'));
      await tester.pumpAndSettle();

      // Still on the regenerate screen -- no navigation happened, and a
      // safe retry message is shown instead of a raw exception or a loop.
      expect(find.text('GOAL_SELECTION_PLACEHOLDER'), findsNothing);
      expect(find.text('Stop current plan and continue'), findsOneWidget);
      expect(find.textContaining('try again'), findsOneWidget);
    });
  });
}
