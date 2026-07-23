import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/api_exception.dart';
import 'package:antigravity_app/features/onboarding/data/plan_generation_error_mapper.dart';

void main() {
  group('planGenerationUserSafeMessage', () {
    test('maps PLAN_HORIZON_COMPOSITION_REQUIRED to the approved safe, non-technical copy', () {
      const error = ApiException(
        message: 'The available race-plan horizon requires a preparation block before the supported race-training core.',
        errorCode: 'PLAN_HORIZON_COMPOSITION_REQUIRED',
        correlationId: 'corr-123',
        statusCode: 422,
      );

      final message = planGenerationUserSafeMessage(error);

      expect(
        message,
        'This plan has more preparation time than the current race-training '
        'format supports. Long-term preparation plans are coming next.',
      );
      // Never leaks the raw backend message or correlation ID to the user.
      expect(message, isNot(contains('preparation block')));
      expect(message, isNot(contains('corr-123')));
    });

    test('errorCode constant matches the backend contract', () {
      expect(planHorizonCompositionRequiredErrorCode, 'PLAN_HORIZON_COMPOSITION_REQUIRED');
      expect(planCoreHorizonUnsupportedErrorCode, 'PLAN_CORE_HORIZON_UNSUPPORTED');
    });

    test('maps PLAN_CORE_HORIZON_UNSUPPORTED to its own approved safe copy', () {
      const error = ApiException(
        message: 'The requested race-plan horizon is recognized, but this exact core length is not yet implemented safely.',
        errorCode: 'PLAN_CORE_HORIZON_UNSUPPORTED',
        correlationId: 'corr-456',
        statusCode: 422,
      );

      final message = planGenerationUserSafeMessage(error);

      expect(
        message,
        'This race date creates a shorter or longer race-training block than '
        'the current plan format supports. For now, choose dates that allow a '
        '12-week plan.',
      );
      expect(message, isNot(contains('corr-456')));
    });

    test('the two typed horizon errors map to distinct, non-identical messages', () {
      const compositionRequired = ApiException(message: 'x', errorCode: 'PLAN_HORIZON_COMPOSITION_REQUIRED');
      const coreHorizonUnsupported = ApiException(message: 'x', errorCode: 'PLAN_CORE_HORIZON_UNSUPPORTED');

      expect(
        planGenerationUserSafeMessage(compositionRequired),
        isNot(equals(planGenerationUserSafeMessage(coreHorizonUnsupported))),
      );
    });

    test('a different ApiException error code falls through to its own backend message', () {
      const error = ApiException(
        message: 'PreferredDays count (4) must equal DaysPerWeek (3).',
        errorCode: 'VALIDATION_ERROR',
        statusCode: 400,
      );

      expect(planGenerationUserSafeMessage(error), 'PreferredDays count (4) must equal DaysPerWeek (3).');
    });

    test('a non-ApiException error falls back to a generic safe message', () {
      expect(planGenerationUserSafeMessage(StateError('boom')), 'Something went wrong. Please try again.');
    });
  });
}
