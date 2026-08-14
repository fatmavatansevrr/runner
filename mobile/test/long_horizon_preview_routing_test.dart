import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/models/running_background.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/network/long_horizon_dtos.dart';
import 'package:antigravity_app/features/onboarding/data/onboarding_provider.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/data/long_horizon_repository.dart';

class _CapturingPlanRepository extends PlanRepository {
  _CapturingPlanRepository() : super(ApiClient());
  int callCount = 0;

  @override
  Future<GeneratePreviewResponse> generateRacePlanPreview(
      GenerateRacePlanPreviewRequestDto request) async {
    callCount++;
    return GeneratePreviewResponse(
      previewId: 'static-preview',
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
  Future<GeneratePreviewResponse> generateHabitPlanPreview(
      GenerateHabitPlanPreviewRequestDto request) async {
    callCount++;
    return GeneratePreviewResponse(
      previewId: 'static-habit-preview',
      templateId: 'habit_5k_beginner_3day_km_v1',
      goalType: 'habit',
      goalDistance: request.goalDistance,
      level: request.level,
      daysPerWeek: request.daysPerWeek,
      unit: request.unit,
      weeks: const [],
    );
  }
}

class _CapturingLongHorizonRepository extends LongHorizonRepository {
  _CapturingLongHorizonRepository() : super(ApiClient());
  int callCount = 0;
  GenerateRacePlanPreviewRequestDto? lastRequest;

  @override
  Future<LongHorizonPlanPreviewContract> generateLongHorizonRacePlanPreview(
      GenerateRacePlanPreviewRequestDto request) async {
    callCount++;
    lastRequest = request;
    return LongHorizonPlanPreviewContract.fromJson({
      'preview_id': 'lh-preview',
      'goal_type': 'race',
      'goal_distance': request.goalDistance,
      'total_weeks': 30,
      'start_date': request.startDate,
      'estimated_end_date': request.raceDate,
      'race_date': request.raceDate,
      'current_window_start_week': 1,
      'current_window_end_week': 8,
      'current_executable_week_count': 8,
      'preview_readiness': 'ready_for_public_preview',
      'confirmation_readiness': 'ready_for_rolling_persistence',
      'public_warnings': <String>[],
      'provenance_summary': 'generated_from_initial_profile',
      'structural_roadmap': [],
      'current_executable_weeks': [],
    });
  }
}

void main() {
  late _CapturingPlanRepository staticRepo;
  late _CapturingLongHorizonRepository longHorizonRepo;
  late OnboardingNotifier notifier;

  setUp(() {
    staticRepo = _CapturingPlanRepository();
    longHorizonRepo = _CapturingLongHorizonRepository();
    notifier = OnboardingNotifier(staticRepo, longHorizonRepo);
  });

  void primeRaceState(
      {required DateTime startDate, required DateTime raceDate}) {
    notifier.updateGoalType('race');
    notifier.updateSelectedRunningDays(['Monday', 'Wednesday', 'Friday']);
    notifier.updateStartDate(startDate);
    notifier.updateRaceDetails(
        'Test Race',
        '${raceDate.year.toString().padLeft(4, '0')}-'
            '${raceDate.month.toString().padLeft(2, '0')}-${raceDate.day.toString().padLeft(2, '0')}');
    notifier.setProductAverageTarget(3600);
    notifier.updateRunningBackground(RunningBackground.beginner);
  }

  test(
      'a race spanning <= 20 weeks calls the static repository, not Long-Horizon',
      () async {
    primeRaceState(
        startDate: DateTime(2026, 1, 5),
        raceDate: DateTime(2026, 4, 27)); // ~16 weeks

    await notifier.generatePreview();

    expect(staticRepo.callCount, 1);
    expect(longHorizonRepo.callCount, 0);
    expect(notifier.state.previewResponse, isNotNull);
    expect(notifier.state.longHorizonPreviewResponse, isNull);
    expect(notifier.state.isLongHorizonPreview, isFalse);
  });

  test(
      'a race spanning > 20 weeks calls the Long-Horizon repository, not static',
      () async {
    primeRaceState(
        startDate: DateTime(2026, 1, 5),
        raceDate: DateTime(2026, 8, 16)); // ~32 weeks

    await notifier.generatePreview();

    expect(longHorizonRepo.callCount, 1);
    expect(staticRepo.callCount, 0);
    expect(notifier.state.longHorizonPreviewResponse, isNotNull);
    expect(notifier.state.previewResponse, isNull);
    expect(notifier.state.isLongHorizonPreview, isTrue);
  });

  test(
      'exactly one of previewResponse / longHorizonPreviewResponse is ever set, never both',
      () async {
    primeRaceState(
        startDate: DateTime(2026, 1, 5), raceDate: DateTime(2026, 8, 16));
    await notifier.generatePreview();
    expect(notifier.state.previewResponse == null, isTrue);
    expect(notifier.state.longHorizonPreviewResponse == null, isFalse);

    // Regenerating with a short span flips to the static shape and clears
    // any stale Long-Horizon preview from the prior attempt.
    notifier.updateRaceDetails('Test Race', '2026-04-27');
    await notifier.generatePreview();
    expect(notifier.state.previewResponse == null, isFalse);
    expect(notifier.state.longHorizonPreviewResponse == null, isTrue);
  });

  test('habit goals never route to Long-Horizon regardless of dates', () async {
    notifier.updateGoalType('habit');
    notifier.updateSelectedRunningDays(['Monday', 'Wednesday', 'Friday']);
    notifier.updateStartDate(DateTime(2026, 1, 5));

    await notifier.generatePreview();

    expect(staticRepo.callCount, 1);
    expect(longHorizonRepo.callCount, 0);
  });
}
