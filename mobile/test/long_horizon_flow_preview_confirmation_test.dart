// Phase 4L.5B -- proves preview -> confirmation -> rolling Home as one
// connected system through the real widget tree and a real (minimal)
// GoRouter, not just isolated unit assertions.
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/core/network/api_exception.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
import 'support/long_horizon_flow_test_harness.dart';

LongHorizonPlanPreviewContract _confirmablePreview(
    {String previewId = 'preview-1'}) {
  return LongHorizonPlanPreviewContract.fromJson({
    'preview_id': previewId,
    'goal_type': 'race',
    'goal_distance': 'marathon',
    'total_weeks': 32,
    'start_date': '2026-01-05',
    'estimated_end_date': '2026-08-17',
    'race_date': '2026-08-16',
    'current_window_start_week': 1,
    'current_window_end_week': 8,
    'current_executable_week_count': 8,
    'preview_readiness': 'ready_for_public_preview',
    'confirmation_readiness': 'ready_for_rolling_persistence',
    'public_warnings': <String>[],
    'provenance_summary': 'generated_from_initial_profile',
    'structural_roadmap': [],
    'current_executable_weeks': [
      {
        'global_week': 1,
        'phase': 'general_endurance',
        'stage': 'base',
        'week_start_date': '2026-01-05',
        'week_end_date': '2026-01-11',
        'weekly_volume_km': 25.0,
        'long_run_volume_km': 10.0,
        'lifecycle_status': 'available',
        'public_provenance_summary': 'generated_from_initial_profile',
        'sessions': [],
      },
    ],
  });
}

void main() {
  group('Preview -> confirmation -> rolling Home', () {
    testWidgets(
        'confirming a ready preview reaches rolling Home and calls confirm exactly once',
        (tester) async {
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.longHorizonPlanPreview);
      final container = ProviderScope.containerOf(
        tester.element(find.byType(MaterialApp).first),
        listen: false,
      );
      container.read(onboardingProvider.notifier).state = container
          .read(onboardingProvider)
          .copyWith(longHorizonPreviewResponse: _confirmablePreview());
      await tester.pumpAndSettle();

      bundle.repo.homeResult = rollingActiveHomeResult(homeResponse(
        plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.currentWindowInProgress),
      ));
      bundle.repo.confirmResponse = LongHorizonConfirmPlanResponse.fromJson({
        'plan_id': 'plan-1',
        'preview_id': 'preview-1',
        'outcome': 'confirmed',
        'total_weeks': 32,
        'plan_status': 'Active',
        'public_message': 'Plan confirmed',
      });

      expect(find.text('Looks good, continue'), findsOneWidget);
      await tester.tap(find.text('Looks good, continue'));
      await tester.pumpAndSettle();

      expect(bundle.repo.confirmCallCount, 1);
      // Reached rolling Home -- the readiness card renders nothing for
      // currentWindowInProgress, but the plan's public_message (rendered
      // unconditionally) proves the Home screen, not the preview, is showing.
      expect(find.text('Week 3 of 32'), findsOneWidget);
      expect(find.byType(Scaffold).evaluate().isNotEmpty, isTrue);
    });

    testWidgets(
        'the confirm button is disabled for a NotReadyForConfirmation preview, and confirm is never called',
        (tester) async {
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.longHorizonPlanPreview);
      final container = ProviderScope.containerOf(
          tester.element(find.byType(MaterialApp).first),
          listen: false);
      final notReady = LongHorizonPlanPreviewContract.fromJson({
        'preview_id': 'preview-2',
        'goal_type': 'race',
        'goal_distance': 'marathon',
        'total_weeks': 32,
        'start_date': '2026-01-05',
        'estimated_end_date': '2026-08-17',
        'race_date': '2026-08-16',
        'current_window_start_week': 1,
        'current_window_end_week': 8,
        'current_executable_week_count': 0,
        'preview_readiness': 'public_preview_blocked',
        'confirmation_readiness': 'not_ready_for_confirmation',
        'public_warnings': <String>[],
        'provenance_summary': 'awaiting_more_training_data',
        'structural_roadmap': [],
        'current_executable_weeks': [],
      });
      container.read(onboardingProvider.notifier).state = container
          .read(onboardingProvider)
          .copyWith(longHorizonPreviewResponse: notReady);
      await tester.pumpAndSettle();

      final button = tester.widget<ElevatedButton>(
          find.widgetWithText(ElevatedButton, 'Looks good, continue'));
      expect(button.onPressed, isNull);
      expect(bundle.repo.confirmCallCount, 0);
    });

    testWidgets(
        'a rapid double tap on confirm only calls the repository once (in-flight guard)',
        (tester) async {
      final bundle = await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.longHorizonPlanPreview);
      final container = ProviderScope.containerOf(
          tester.element(find.byType(MaterialApp).first),
          listen: false);
      container.read(onboardingProvider.notifier).state = container
          .read(onboardingProvider)
          .copyWith(longHorizonPreviewResponse: _confirmablePreview());
      await tester.pumpAndSettle();

      bundle.repo.homeResult = rollingActiveHomeResult(homeResponse(
        plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.currentWindowInProgress),
      ));
      bundle.repo.confirmResponse = LongHorizonConfirmPlanResponse.fromJson({
        'plan_id': 'plan-1',
        'preview_id': 'preview-1',
        'outcome': 'confirmed',
        'total_weeks': 32,
        'plan_status': 'Active',
        'public_message': 'Plan confirmed',
      });

      // Invoke onPressed directly, twice, synchronously back-to-back --
      // proves the in-flight `_isConfirming` guard itself (both calls
      // happen before either yields at its first await), which
      // `tester.tap()` cannot reproduce without violating flutter_test's
      // "no overlapping unawaited gesture" guard.
      final button = tester.widget<ElevatedButton>(
          find.widgetWithText(ElevatedButton, 'Looks good, continue'));
      button.onPressed!();
      button.onPressed!();
      await tester.pumpAndSettle();

      expect(bundle.repo.confirmCallCount, 1);
    });

    testWidgets(
        'confirmation timeout plus authoritative rolling Home is committed and navigates once',
        (tester) async {
      final bundle = await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.longHorizonPlanPreview,
      );
      final container = ProviderScope.containerOf(
        tester.element(find.byType(MaterialApp).first),
        listen: false,
      );
      container.read(onboardingProvider.notifier).state = container
          .read(onboardingProvider)
          .copyWith(longHorizonPreviewResponse: _confirmablePreview());
      bundle.repo
        ..confirmError = const ApiException(message: 'timeout')
        ..homeResult = rollingActiveHomeResult(homeResponse(
          plan: activePlanSummary(
            checkpointReadiness:
                LongHorizonCheckpointReadiness.currentWindowInProgress,
          ),
        ));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Looks good, continue'));
      await tester.pumpAndSettle();

      expect(bundle.repo.confirmCallCount, 1);
      expect(bundle.repo.homeCallCount, greaterThanOrEqualTo(1));
      expect(bundle.router.routeInformationProvider.value.uri.path,
          AppRoutes.home);
    });

    testWidgets(
        'confirmation timeout without a rolling active plan preserves the preview for explicit retry',
        (tester) async {
      final bundle = await pumpLongHorizonFlowApp(
        tester,
        initialLocation: AppRoutes.longHorizonPlanPreview,
      );
      final container = ProviderScope.containerOf(
        tester.element(find.byType(MaterialApp).first),
        listen: false,
      );
      container.read(onboardingProvider.notifier).state = container
          .read(onboardingProvider)
          .copyWith(longHorizonPreviewResponse: _confirmablePreview());
      bundle.repo
        ..confirmError = const ApiException(message: 'timeout')
        ..homeResult = ActiveHomeResult.fromJson(<String, dynamic>{});
      await tester.pumpAndSettle();

      await tester.tap(find.text('Looks good, continue'));
      await tester.pumpAndSettle();

      expect(bundle.repo.confirmCallCount, 1);
      expect(find.text('Looks good, continue'), findsOneWidget);
      expect(find.textContaining('preview is still here'), findsOneWidget);
    });
  });

  group(
      'Lowercase-enum wire regression (Phase 4L.5A defect, re-proven in a live flow)',
      () {
    testWidgets(
        'a preview whose confirmation_readiness uses the old incorrect PascalCase value never enables confirm',
        (tester) async {
      await pumpLongHorizonFlowApp(tester,
          initialLocation: AppRoutes.longHorizonPlanPreview);
      final container = ProviderScope.containerOf(
          tester.element(find.byType(MaterialApp).first),
          listen: false);
      // The exact defect Phase 4L.5A found and fixed: PascalCase is what the
      // backend NEVER sends, so it must fail closed here too, proving the
      // fix holds through the real preview screen, not only the decoder
      // unit test.
      final pascalCaseBug = LongHorizonPlanPreviewContract.fromJson({
        'preview_id': 'preview-3',
        'goal_type': 'race',
        'goal_distance': 'marathon',
        'total_weeks': 32,
        'start_date': '2026-01-05',
        'estimated_end_date': '2026-08-17',
        'race_date': '2026-08-16',
        'current_window_start_week': 1,
        'current_window_end_week': 8,
        'current_executable_week_count': 8,
        'preview_readiness': 'ReadyForPublicPreview',
        'confirmation_readiness': 'ReadyForRollingPersistence',
        'public_warnings': <String>[],
        'provenance_summary': 'GeneratedFromInitialProfile',
        'structural_roadmap': [],
        'current_executable_weeks': [],
      });
      container.read(onboardingProvider.notifier).state = container
          .read(onboardingProvider)
          .copyWith(longHorizonPreviewResponse: pascalCaseBug);
      await tester.pumpAndSettle();

      expect(pascalCaseBug.isConfirmable, isFalse);
      final button = tester.widget<ElevatedButton>(
          find.widgetWithText(ElevatedButton, 'Looks good, continue'));
      expect(button.onPressed, isNull);
    });
  });
}
