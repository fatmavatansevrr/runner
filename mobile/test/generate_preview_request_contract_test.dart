import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/models/average_finish_time_policy.dart';
import 'package:antigravity_app/core/models/recent_race_result.dart';
import 'package:antigravity_app/core/models/running_background.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';
import 'support/noop_long_horizon_repository.dart';

/// Captures the last race/habit request passed to the repository instead of
/// making a network call, so tests can assert on the exact serialized shape.
class _CapturingPlanRepository extends PlanRepository {
  _CapturingPlanRepository() : super(ApiClient());

  GenerateRacePlanPreviewRequestDto? lastRaceRequest;
  GenerateHabitPlanPreviewRequestDto? lastHabitRequest;

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(GenerateRacePlanPreviewRequestDto request) async {
    lastRaceRequest = request;
    return GeneratePreviewResponse(
      previewId: 'preview-1',
      templateId: 'TEN_K__4D__INTERMEDIATE',
      goalType: 'race',
      goalDistance: request.goalDistance,
      level: request.level,
      daysPerWeek: request.daysPerWeek,
      unit: request.unit,
      weeks: const [],
    );
  }

  @override
  Future<GeneratePreviewResponse> generateHabitPlanPreview(GenerateHabitPlanPreviewRequestDto request) async {
    lastHabitRequest = request;
    return GeneratePreviewResponse(
      previewId: 'preview-1',
      templateId: 'habit_5k_beginner_3day_km_v1',
      goalType: 'habit',
      goalDistance: request.goalDistance,
      level: request.level,
      daysPerWeek: request.daysPerWeek,
      unit: request.unit,
      weeks: const [],
    );
  }

  @override
  Future<ConfirmPlanResponse> confirmPlan(String previewId) async =>
      ConfirmPlanResponse(planId: 'plan-1', status: 'active');
}

void main() {
  group('RecentRaceRequest — nested serialization', () {
    test('serializes as a nested object with the canonical distance token', () {
      final json = const RecentRaceRequest(
        distance: 'ten_k',
        finishTimeSeconds: 3510,
        raceDate: '2026-06-01',
      ).toJson();

      expect(json, {
        'distance': 'ten_k',
        'finish_time_seconds': 3510,
        'race_date': '2026-06-01',
      });
    });
  });

  group('GenerateRacePlanPreviewRequestDto — exact JSON shape', () {
    test('preferred_days is a JSON array, recent_race is nested, target source included', () {
      final json = GenerateRacePlanPreviewRequestDto(
        goalDistance: 'ten_k',
        level: 'intermediate',
        daysPerWeek: 4,
        unit: 'km',
        startDate: '2026-07-20',
        preferredDays: const ['mon', 'wed', 'fri', 'sun'],
        longRunDay: 'sun',
        raceName: 'Local 10K',
        raceDate: '2026-10-12',
        targetFinishTimeSeconds: 3480,
        targetFinishTimeSource: TargetFinishTimeSourceWire.productAverage,
        recentWeeklyVolumeKm: 20,
        recentLongestRunKm: 8,
        recentRunsPerWeek: 3,
        recentRace: const RecentRaceRequest(
          distance: 'ten_k',
          finishTimeSeconds: 3510,
          raceDate: '2026-06-01',
        ),
      ).toJson();

      expect(json, {
        'goal_distance': 'ten_k',
        'level': 'intermediate',
        'days_per_week': 4,
        'unit': 'km',
        'start_date': '2026-07-20',
        'preferred_days': ['mon', 'wed', 'fri', 'sun'],
        'long_run_day': 'sun',
        'race_date': '2026-10-12',
        'race_name': 'Local 10K',
        'target_finish_time_seconds': 3480,
        'target_finish_time_source': 'product_average',
        'recent_weekly_volume_km': 20.0,
        'recent_longest_run_km': 8.0,
        'recent_runs_per_week': 3,
        'recent_race': {
          'distance': 'ten_k',
          'finish_time_seconds': 3510,
          'race_date': '2026-06-01',
        },
      });
    });

    test('no goal_type discriminator key -- the endpoint determines the flow', () {
      final json = GenerateRacePlanPreviewRequestDto(
        goalDistance: 'ten_k',
        level: 'intermediate',
        daysPerWeek: 4,
        unit: 'km',
        startDate: '2026-07-20',
        preferredDays: const ['mon', 'wed', 'fri', 'sun'],
        longRunDay: 'sun',
        raceDate: '2026-10-12',
        targetFinishTimeSeconds: 3600,
        targetFinishTimeSource: TargetFinishTimeSourceWire.userDefined,
      ).toJson();

      expect(json.containsKey('goal_type'), isFalse);
    });
  });

  group('GenerateHabitPlanPreviewRequestDto — exact JSON shape', () {
    test('matches the target habit shape exactly, no race/target/recent-race fields at all', () {
      final json = GenerateHabitPlanPreviewRequestDto(
        goalDistance: 'five_k',
        level: 'beginner',
        daysPerWeek: 3,
        unit: 'km',
        startDate: '2026-07-20',
        preferredDays: const ['mon', 'wed', 'sat'],
      ).toJson();

      expect(json, {
        'goal_distance': 'five_k',
        'level': 'beginner',
        'days_per_week': 3,
        'unit': 'km',
        'start_date': '2026-07-20',
        'preferred_days': ['mon', 'wed', 'sat'],
      });
      expect(json.containsKey('goal_type'), isFalse);
      expect(json.containsKey('race_date'), isFalse);
      expect(json.containsKey('target_finish_time_seconds'), isFalse);
      expect(json.containsKey('target_finish_time_source'), isFalse);
      expect(json.containsKey('recent_race'), isFalse);
    });

    test('preferred_days is always a List<String>, never a comma-joined string', () {
      final json = GenerateHabitPlanPreviewRequestDto(
        goalDistance: 'five_k',
        level: 'beginner',
        daysPerWeek: 3,
        unit: 'km',
        startDate: '2026-07-20',
        preferredDays: const ['mon', 'wed', 'sat'],
      ).toJson();

      expect(json['preferred_days'], isA<List<String>>());
    });
  });

  group('AverageFinishTimePolicy — "Go with average"', () {
    test('every distance bucket produces a positive, non-null value', () {
      expect(AverageFinishTimePolicy.secondsForDistanceKm(5.0), greaterThan(0));
      expect(AverageFinishTimePolicy.secondsForDistanceKm(10.0), greaterThan(0));
      expect(AverageFinishTimePolicy.secondsForDistanceKm(21.1), greaterThan(0));
      expect(AverageFinishTimePolicy.secondsForDistanceKm(42.2), greaterThan(0));
    });

    test('matches the product\'s existing shipped average values exactly', () {
      expect(AverageFinishTimePolicy.secondsForDistanceKm(5.0), 28 * 60);
      expect(AverageFinishTimePolicy.secondsForDistanceKm(10.0), 58 * 60);
      expect(AverageFinishTimePolicy.secondsForDistanceKm(21.1), (2 * 3600) + (5 * 60));
      expect(AverageFinishTimePolicy.secondsForDistanceKm(42.2), (4 * 3600) + (21 * 60));
    });
  });

  group('OnboardingNotifier.generatePreview — full contract wiring', () {
    late ProviderContainer container;
    late _CapturingPlanRepository repo;

    setUp(() {
      repo = _CapturingPlanRepository();
      container = ProviderContainer(overrides: [
        planRepositoryProvider.overrideWithValue(repo),
        longHorizonRepositoryProvider.overrideWithValue(NoopLongHorizonRepository()),
      ]);
      addTearDown(container.dispose);
    });

    Future<void> fillRaceState({int? targetFinishTimeSeconds = 3600, bool setTarget = true}) async {
      final notifier = container.read(onboardingProvider.notifier);
      notifier.updateGoalType('race');
      notifier.updateGoalDistance('ten_k');
      notifier.updateRunningBackground(RunningBackground.intermediate);
      notifier.updateDaysPerWeek(4);
      notifier.updateRaceDetails('Local 10K', '2026-10-12');
      if (setTarget && targetFinishTimeSeconds != null) {
        notifier.setUserDefinedTarget(targetFinishTimeSeconds);
      }
      notifier.updateSelectedRunningDays(['monday', 'wednesday', 'friday', 'sunday']);
      notifier.updateLongRunDay('Sunday');
      notifier.updateStartDate(DateTime(2026, 7, 20));
    }

    test('custom target finish time is preserved verbatim, tagged user_defined', () async {
      await fillRaceState(targetFinishTimeSeconds: 3123);
      await container.read(onboardingProvider.notifier).generatePreview();
      expect(repo.lastRaceRequest!.targetFinishTimeSeconds, 3123);
      expect(repo.lastRaceRequest!.targetFinishTimeSource, TargetFinishTimeSourceWire.userDefined);
    });

    test('go-with-average sends canonical seconds tagged product_average', () async {
      await fillRaceState(setTarget: false);
      final notifier = container.read(onboardingProvider.notifier);
      notifier.setProductAverageTarget(AverageFinishTimePolicy.secondsForDistanceKm(10.0));

      await notifier.generatePreview();

      expect(repo.lastRaceRequest!.targetFinishTimeSeconds, AverageFinishTimePolicy.tenKSeconds);
      expect(repo.lastRaceRequest!.targetFinishTimeSource, TargetFinishTimeSourceWire.productAverage);
    });

    test('switching from go-with-average to a custom target atomically updates both value and source', () async {
      await fillRaceState(setTarget: false);
      final notifier = container.read(onboardingProvider.notifier);
      notifier.setProductAverageTarget(AverageFinishTimePolicy.tenKSeconds);
      expect(container.read(onboardingProvider).targetFinishTimeSource, TargetFinishTimeSourceWire.productAverage);

      notifier.setUserDefinedTarget(4000);

      final state = container.read(onboardingProvider);
      expect(state.targetFinishTimeSeconds, 4000);
      expect(state.targetFinishTimeSource, TargetFinishTimeSourceWire.userDefined);
    });

    test('switching from a custom target back to go-with-average atomically updates both value and source', () async {
      await fillRaceState(setTarget: false);
      final notifier = container.read(onboardingProvider.notifier);
      notifier.setUserDefinedTarget(4000);
      expect(container.read(onboardingProvider).targetFinishTimeSource, TargetFinishTimeSourceWire.userDefined);

      notifier.setProductAverageTarget(AverageFinishTimePolicy.tenKSeconds);

      final state = container.read(onboardingProvider);
      expect(state.targetFinishTimeSeconds, AverageFinishTimePolicy.tenKSeconds);
      expect(state.targetFinishTimeSource, TargetFinishTimeSourceWire.productAverage);
    });

    test('race request cannot be generated without a resolved positive target time', () async {
      await fillRaceState(setTarget: false);
      expect(
        () => container.read(onboardingProvider.notifier).generatePreview(),
        throwsA(isA<StateError>()),
      );
    });

    test('preferred_days serializes as lowercase 3-letter tokens, long_run_day too', () async {
      await fillRaceState();
      await container.read(onboardingProvider.notifier).generatePreview();
      expect(repo.lastRaceRequest!.preferredDays, ['mon', 'wed', 'fri', 'sun']);
      expect(repo.lastRaceRequest!.longRunDay, 'sun');
    });

    test('start_date serializes as yyyy-MM-dd', () async {
      await fillRaceState();
      await container.read(onboardingProvider.notifier).generatePreview();
      expect(repo.lastRaceRequest!.startDate, '2026-07-20');
    });

    test('recent_race serializes as a nested RecentRaceRequest with canonical distance token', () async {
      await fillRaceState();
      container.read(onboardingProvider.notifier).updateRunningBackground(RunningBackground.intermediate);
      container.read(onboardingProvider.notifier).updateRecentRaceResult(RecentRaceResult(
            distanceKm: 10.0,
            finishTimeSeconds: 3510,
            raceDate: DateTime(2026, 6, 1),
          ));

      await container.read(onboardingProvider.notifier).generatePreview();

      final recentRace = repo.lastRaceRequest!.recentRace;
      expect(recentRace, isNotNull);
      expect(recentRace!.distance, 'ten_k');
      expect(recentRace.finishTimeSeconds, 3510);
      expect(recentRace.raceDate, '2026-06-01');
    });

    test('"I\'m not sure" (null) readiness fields are omitted, never sent as 0', () async {
      await fillRaceState();
      final notifier = container.read(onboardingProvider.notifier);
      notifier.updateRecentWeeklyVolumeKm(null);
      notifier.updateRecentLongestRunKm(null);

      await notifier.generatePreview();

      expect(repo.lastRaceRequest!.recentWeeklyVolumeKm, isNull);
      expect(repo.lastRaceRequest!.recentLongestRunKm, isNull);
    });

    test('an explicit 0 for a readiness field is sent as 0, never coerced to null', () async {
      await fillRaceState();
      final notifier = container.read(onboardingProvider.notifier);
      notifier.updateRecentWeeklyVolumeKm(0);

      await notifier.generatePreview();

      expect(repo.lastRaceRequest!.recentWeeklyVolumeKm, 0);
    });

    test('switching Advanced/Intermediate back to Beginner removes stale readiness before sending', () async {
      await fillRaceState();
      notifierFillDetails(container);

      container.read(onboardingProvider.notifier).updateRunningBackground(RunningBackground.beginner);

      await container.read(onboardingProvider.notifier).generatePreview();

      expect(repo.lastRaceRequest!.recentWeeklyVolumeKm, isNull);
      expect(repo.lastRaceRequest!.recentLongestRunKm, isNull);
      expect(repo.lastRaceRequest!.recentRace, isNull);
    });

    test('habit flow sends GenerateHabitPlanPreviewRequestDto, not the race shape', () async {
      final notifier = container.read(onboardingProvider.notifier);
      notifier.updateGoalType('habit');
      notifier.updateGoalDistance('five_k');
      notifier.updateRunningBackground(RunningBackground.beginner);
      notifier.updateDaysPerWeek(3);
      notifier.updateSelectedRunningDays(['monday', 'wednesday', 'saturday']);
      notifier.updateStartDate(DateTime(2026, 7, 20));

      await notifier.generatePreview();

      expect(repo.lastHabitRequest, isNotNull);
      expect(repo.lastRaceRequest, isNull);
      expect(repo.lastHabitRequest!.preferredDays, ['mon', 'wed', 'sat']);
    });
  });
}

void notifierFillDetails(ProviderContainer container) {
  final notifier = container.read(onboardingProvider.notifier);
  notifier.updateRunningBackground(RunningBackground.intermediate);
  notifier.updateRecentWeeklyVolumeKm(30);
  notifier.updateRecentLongestRunKm(12);
  notifier.updateRecentRaceResult(RecentRaceResult(
    distanceKm: 10,
    finishTimeSeconds: 3200,
    raceDate: DateTime(2026, 5, 1),
  ));
}
