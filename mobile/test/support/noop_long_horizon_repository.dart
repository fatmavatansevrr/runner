import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';

/// Shared no-op stand-in for [longHorizonRepositoryProvider] used by tests
/// that override [planRepositoryProvider] but never exercise the
/// Long-Horizon path. `onboardingProvider` reads both repositories, so
/// leaving this one unoverridden would otherwise fall through to a real
/// `ApiClient()` -> Firebase.instance and crash in a widget-test sandbox
/// that never initializes Firebase.
class NoopLongHorizonRepository extends LongHorizonRepository {
  NoopLongHorizonRepository() : super(ApiClient());
}
