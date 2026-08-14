import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/api_exception.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_error_mapper.dart';

void main() {
  LongHorizonUiError mapCode(String code, {int? statusCode}) =>
      LongHorizonUiErrorMapper.map(
        ApiException(
            message: 'raw backend detail: internal reason XYZ',
            errorCode: code,
            statusCode: statusCode),
      );

  group('preview/confirmation error codes', () {
    test('LONG_HORIZON_PREVIEW_EXPIRED maps to regeneratePreview', () {
      expect(mapCode('LONG_HORIZON_PREVIEW_EXPIRED').action,
          LongHorizonErrorAction.regeneratePreview);
    });

    test('LONG_HORIZON_PREVIEW_STALE maps to regeneratePreview', () {
      expect(mapCode('LONG_HORIZON_PREVIEW_STALE').action,
          LongHorizonErrorAction.regeneratePreview);
    });

    test('LONG_HORIZON_PREVIEW_NOT_FOUND maps to regeneratePreview', () {
      expect(mapCode('LONG_HORIZON_PREVIEW_NOT_FOUND').action,
          LongHorizonErrorAction.regeneratePreview);
    });

    test('PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW maps to a safe message', () {
      final result = mapCode('PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW');
      expect(result.action, LongHorizonErrorAction.showMessage);
      expect(result.userMessage, isNot(contains('PLAN_HORIZON')));
    });

    test('LONG_HORIZON_ACTIVE_PLAN_CONFLICT maps to refreshHome', () {
      expect(mapCode('LONG_HORIZON_ACTIVE_PLAN_CONFLICT').action,
          LongHorizonErrorAction.refreshHome);
    });
  });

  group('mutation error codes', () {
    test('LONG_HORIZON_ROLLING_SESSION_NOT_EXECUTABLE maps to refreshDetail',
        () {
      expect(mapCode('LONG_HORIZON_ROLLING_SESSION_NOT_EXECUTABLE').action,
          LongHorizonErrorAction.refreshDetail);
    });

    test(
        'LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT maps to refreshDetail',
        () {
      expect(mapCode('LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT').action,
          LongHorizonErrorAction.refreshDetail);
    });

    test('LONG_HORIZON_ROLLING_SESSION_OUTCOME_CONFLICT maps to refreshDetail',
        () {
      expect(mapCode('LONG_HORIZON_ROLLING_SESSION_OUTCOME_CONFLICT').action,
          LongHorizonErrorAction.refreshDetail);
    });

    test(
        'LONG_HORIZON_ROLLING_MUTATION_CONCURRENCY_CONFLICT maps to refreshDetail',
        () {
      expect(
          mapCode('LONG_HORIZON_ROLLING_MUTATION_CONCURRENCY_CONFLICT').action,
          LongHorizonErrorAction.refreshDetail);
    });
  });

  group('continuation/recovery error codes', () {
    test(
        'LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS maps to refreshHome, no success framing',
        () {
      final result = mapCode('LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS');
      expect(result.action, LongHorizonErrorAction.refreshHome);
      expect(result.userMessage.toLowerCase(), isNot(contains('success')));
    });

    test('LONG_HORIZON_RETRY_REQUIRED maps to retryGuidance', () {
      expect(mapCode('LONG_HORIZON_RETRY_REQUIRED').action,
          LongHorizonErrorAction.retryGuidance);
    });

    test(
        'LONG_HORIZON_RETRY_NOT_ELIGIBLE maps to regeneratePreview (backend-approved recovery guidance)',
        () {
      expect(mapCode('LONG_HORIZON_RETRY_NOT_ELIGIBLE').action,
          LongHorizonErrorAction.regeneratePreview);
    });

    test(
        'LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED maps to cancelAndCreateNewPlan',
        () {
      expect(mapCode('LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED').action,
          LongHorizonErrorAction.cancelAndCreateNewPlan);
    });

    test('LONG_HORIZON_OPERATIONAL_SUPPORT_REQUIRED maps to operationalSupport',
        () {
      expect(mapCode('LONG_HORIZON_OPERATIONAL_SUPPORT_REQUIRED').action,
          LongHorizonErrorAction.operationalSupport);
    });

    test('LONG_HORIZON_CONTINUATION_CONCURRENCY_CONFLICT maps to refreshHome',
        () {
      expect(mapCode('LONG_HORIZON_CONTINUATION_CONCURRENCY_CONFLICT').action,
          LongHorizonErrorAction.refreshHome);
    });

    test(
        'LONG_HORIZON_READ_STATE_CORRUPT maps to a safe operational state, not a raw error',
        () {
      final result = mapCode('LONG_HORIZON_READ_STATE_CORRUPT');
      expect(result.action, LongHorizonErrorAction.operationalSupport);
      expect(result.userMessage, isNot(contains('CORRUPT')));
    });
  });

  group('unknown/generic handling', () {
    test(
        'an unrecognized error code maps to a safe generic failure, never the raw code/message',
        () {
      final result = mapCode('LONG_HORIZON_SOME_FUTURE_CODE_NOT_YET_MAPPED');
      expect(result.action, LongHorizonErrorAction.genericFailure);
      expect(
          result.userMessage, isNot(contains('LONG_HORIZON_SOME_FUTURE_CODE')));
      expect(result.userMessage, isNot(contains('raw backend detail')));
    });

    test('a 401 always maps to signInAgain regardless of errorCode', () {
      final result =
          mapCode('LONG_HORIZON_READ_STATE_CORRUPT', statusCode: 401);
      expect(result.action, LongHorizonErrorAction.signInAgain);
    });

    test('a non-ApiException error maps to a safe generic failure', () {
      final result =
          LongHorizonUiErrorMapper.map(Exception('some raw dart exception'));
      expect(result.action, LongHorizonErrorAction.genericFailure);
      expect(result.userMessage, isNot(contains('raw dart exception')));
    });
  });

  group('raw backend detail never leaks', () {
    test(
        'none of the mapped user messages ever contain a raw LONG_HORIZON_ error code',
        () {
      const codes = [
        'LONG_HORIZON_PREVIEW_NOT_FOUND',
        'LONG_HORIZON_PREVIEW_EXPIRED',
        'LONG_HORIZON_PREVIEW_STALE',
        'LONG_HORIZON_ACTIVE_PLAN_CONFLICT',
        'LONG_HORIZON_PILOT_UNSUPPORTED',
        'PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW',
        'LONG_HORIZON_INITIALIZATION_INFEASIBLE',
        'LONG_HORIZON_READ_SURFACE_NOT_YET_SUPPORTED',
        'LONG_HORIZON_ACTIVE_PLAN_NOT_FOUND',
        'LONG_HORIZON_READ_STATE_CORRUPT',
        'LONG_HORIZON_ROLLING_SESSION_NOT_FOUND',
        'LONG_HORIZON_ROLLING_SESSION_NOT_EXECUTABLE',
        'LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT',
        'LONG_HORIZON_ROLLING_SESSION_OUTCOME_CONFLICT',
        'LONG_HORIZON_ROLLING_MUTATION_CONCURRENCY_CONFLICT',
        'LONG_HORIZON_ROLLING_MUTATION_VERSION_UNSUPPORTED',
        'LONG_HORIZON_CONTINUATION_VERSION_UNSUPPORTED',
        'LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS',
        'LONG_HORIZON_REASSESSMENT_REQUIRED',
        'LONG_HORIZON_CONTINUATION_BLOCKED',
        'LONG_HORIZON_RETRY_REQUIRED',
        'LONG_HORIZON_CONTINUATION_CONCURRENCY_CONFLICT',
        'LONG_HORIZON_NO_BLOCKED_BOUNDARY',
        'LONG_HORIZON_RETRY_NOT_ELIGIBLE',
        'LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED',
        'LONG_HORIZON_OPERATIONAL_SUPPORT_REQUIRED',
      ];
      for (final code in codes) {
        final result = mapCode(code);
        expect(result.userMessage.contains('LONG_HORIZON_'), isFalse,
            reason: '$code leaked into its own message');
        expect(result.userMessage.contains('raw backend detail'), isFalse,
            reason: '$code did not sanitize the backend message');
      }
    });
  });
}
