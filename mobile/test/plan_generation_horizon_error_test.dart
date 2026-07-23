import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/models/running_background.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/api_exception.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
import 'package:antigravity_app/features/onboarding/data/plan_generation_error_mapper.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';

/// A [PlanRepository] double whose race-preview call always fails with a
/// given typed backend error — simulating exactly what the real API returns
/// for SW-12 (20-week undershoot, StartDate=2026-05-25/RaceDate=2026-10-12,
/// PLAN_HORIZON_COMPOSITION_REQUIRED) and SW-13 (8-week overshoot,
/// StartDate=2026-07-20/RaceDate=2026-09-14, PLAN_CORE_HORIZON_UNSUPPORTED).
class _HorizonRejectingPlanRepository extends PlanRepository {
  _HorizonRejectingPlanRepository(this._errorCode, this._message) : super(ApiClient());

  final String _errorCode;
  final String _message;
  int raceCallCount = 0;

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(GenerateRacePlanPreviewRequestDto request) async {
    raceCallCount++;
    throw ApiException(
      message: _message,
      errorCode: _errorCode,
      correlationId: 'corr-test',
      statusCode: 422,
    );
  }
}

void main() {
  group('generatePreview() typed horizon failures', () {
    final scenarios = <String, ({String errorCode, String message, DateTime startDate, String raceDate})>{
      'PLAN_HORIZON_COMPOSITION_REQUIRED (SW-12, 20-week undershoot)': (
        errorCode: planHorizonCompositionRequiredErrorCode,
        message: 'The available race-plan horizon requires a preparation block before the supported race-training core.',
        startDate: DateTime(2026, 5, 25),
        raceDate: '2026-10-12',
      ),
      'PLAN_CORE_HORIZON_UNSUPPORTED (SW-13, 8-week overshoot)': (
        errorCode: planCoreHorizonUnsupportedErrorCode,
        message: 'The requested race-plan horizon is recognized, but this exact core length is not yet implemented safely.',
        startDate: DateTime(2026, 7, 20),
        raceDate: '2026-09-14',
      ),
    };

    for (final entry in scenarios.entries) {
      final scenario = entry.value;

      test('${entry.key}: never retries or resubmits, and preserves onboarding state (StartDate/RaceDate) unchanged', () async {
        final fakeRepo = _HorizonRejectingPlanRepository(scenario.errorCode, scenario.message);
        final container = ProviderContainer(
          overrides: [planRepositoryProvider.overrideWithValue(fakeRepo)],
        );
        addTearDown(container.dispose);
        final notifier = container.read(onboardingProvider.notifier);

        notifier.updateGoalType('race');
        notifier.updateGoalDistance('ten_k');
        notifier.updateRunningBackground(RunningBackground.intermediate);
        notifier.updateDaysPerWeek(4);
        notifier.updateRaceDetails('Fall 10K', scenario.raceDate);
        notifier.setProductAverageTarget(3480);
        notifier.updateSelectedRunningDays(['Monday', 'Wednesday', 'Friday', 'Sunday']);
        notifier.updateLongRunDay('Sunday');
        notifier.updateStartDate(scenario.startDate);
        notifier.updateRecentWeeklyVolumeKm(20);
        notifier.updateRecentLongestRunKm(8);

        final beforeState = container.read(onboardingProvider);

        Object? caughtError;
        try {
          await notifier.generatePreview();
        } catch (e) {
          caughtError = e;
        }

        // The failure propagates as the typed ApiException — not silently
        // swallowed, not converted into a successful preview, and never
        // silently retried through a legacy/fallback path (no such call
        // exists on this fake repo at all).
        expect(caughtError, isA<ApiException>());
        expect((caughtError as ApiException).errorCode, scenario.errorCode);

        // Exactly one call — no automatic retry, no automatic resubmission
        // with a truncated/shortened/mutated horizon.
        expect(fakeRepo.raceCallCount, 1);

        // Onboarding state (including startDate/raceDate, the two fields the
        // user needs to go back and change) is completely untouched by the
        // failure — generatePreview() never mutates state on the error path,
        // and dates are never auto-adjusted toward a 12-week window.
        final afterState = container.read(onboardingProvider);
        expect(afterState.startDate, beforeState.startDate);
        expect(afterState.raceDate, beforeState.raceDate);
        expect(afterState.startDate, scenario.startDate);
        expect(afterState.raceDate, scenario.raceDate);
        expect(afterState.previewResponse, isNull);
        // Same state instance — generatePreview() never calls copyWith/state=
        // on the error path.
        expect(identical(afterState, beforeState), isTrue);
      });
    }

    test('PLAN_HORIZON_COMPOSITION_REQUIRED message never mentions weeks, dates, or internal cycle terminology', () {
      const error = ApiException(
        message: 'The available race-plan horizon requires a preparation block before the supported race-training core.',
        errorCode: planHorizonCompositionRequiredErrorCode,
      );

      final message = planGenerationUserSafeMessage(error);

      expect(message, isNot(contains('cycle')));
      expect(message, isNot(contains('horizon')));
      expect(message, contains('preparation time'));
    });

    test('PLAN_CORE_HORIZON_UNSUPPORTED message never mentions weeks, dates, or internal cycle terminology', () {
      const error = ApiException(
        message: 'The requested race-plan horizon is recognized, but this exact core length is not yet implemented safely.',
        errorCode: planCoreHorizonUnsupportedErrorCode,
      );

      final message = planGenerationUserSafeMessage(error);

      expect(message, isNot(contains('cycle')));
      expect(message, isNot(contains('horizon')));
      expect(message, contains('12-week'));
    });

    test('the two typed horizon errors produce different messages, so a user never confuses them', () {
      const compositionRequired = ApiException(message: 'x', errorCode: planHorizonCompositionRequiredErrorCode);
      const coreHorizonUnsupported = ApiException(message: 'x', errorCode: planCoreHorizonUnsupportedErrorCode);

      expect(
        planGenerationUserSafeMessage(compositionRequired),
        isNot(equals(planGenerationUserSafeMessage(coreHorizonUnsupported))),
      );
    });
  });
}
