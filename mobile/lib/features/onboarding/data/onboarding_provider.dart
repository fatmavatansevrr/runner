import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../plan/data/plan_repository.dart';
import '../../../core/network/dtos.dart';

class OnboardingState {
  OnboardingState({
    this.goalType = 'habit',

    this.goalDistance = 'five_k',
    this.level = 'beginner',
    this.daysPerWeek = 3,
    this.unit = 'km',
    this.raceName,
    this.raceDate,
    this.targetFinishTimeSeconds,
    this.longRunDay = 'Sunday',
    this.startDate,
    this.previewResponse,
  });

  final String goalType;
  final String goalDistance;
  final String level;
  final int daysPerWeek;
  final String unit;
  final String? raceName;
  final String? raceDate;
  final int? targetFinishTimeSeconds;
  final String longRunDay;
  final DateTime? startDate;
  final GeneratePreviewResponse? previewResponse;

  OnboardingState copyWith({
    String? goalType,
    String? goalDistance,
    String? level,
    int? daysPerWeek,
    String? unit,
    String? raceName,
    String? raceDate,
    int? targetFinishTimeSeconds,
    String? longRunDay,
    DateTime? startDate,
    GeneratePreviewResponse? previewResponse,
  }) {
    return OnboardingState(
      goalType: goalType ?? this.goalType,
      goalDistance: goalDistance ?? this.goalDistance,
      level: level ?? this.level,
      daysPerWeek: daysPerWeek ?? this.daysPerWeek,
      unit: unit ?? this.unit,
      raceName: raceName ?? this.raceName,
      raceDate: raceDate ?? this.raceDate,
      targetFinishTimeSeconds: targetFinishTimeSeconds ?? this.targetFinishTimeSeconds,
      longRunDay: longRunDay ?? this.longRunDay,
      startDate: startDate ?? this.startDate,
      previewResponse: previewResponse ?? this.previewResponse,
    );
  }
}

class OnboardingNotifier extends StateNotifier<OnboardingState> {
  OnboardingNotifier(this._planRepository) : super(OnboardingState());

  final PlanRepository _planRepository;

  void updateGoalType(String type) => state = state.copyWith(goalType: type);
  void updateGoalDistance(String dist) => state = state.copyWith(goalDistance: dist);
  void updateLevel(String lvl) => state = state.copyWith(level: lvl);
  void updateDaysPerWeek(int days) => state = state.copyWith(daysPerWeek: days);
  void updateUnit(String unit) => state = state.copyWith(unit: unit);
  void updateRaceDetails(String name, String date) =>
      state = state.copyWith(raceName: name, raceDate: date);
  void updateTargetFinishTime(int? seconds) => state = state.copyWith(targetFinishTimeSeconds: seconds);
  void updateLongRunDay(String day) => state = state.copyWith(longRunDay: day);
  void updateStartDate(DateTime date) => state = state.copyWith(startDate: date);

  Future<GeneratePreviewResponse> generatePreview() async {
    final request = GeneratePreviewRequest(
      goalType: state.goalType,
      goalDistance: state.goalDistance,
      level: state.level,
      daysPerWeek: state.daysPerWeek,
      unit: state.unit,
      raceName: state.raceName,
      raceDate: state.raceDate,
      targetFinishTimeSeconds: state.targetFinishTimeSeconds,
    );
    final response = await _planRepository.generatePreview(request);
    state = state.copyWith(previewResponse: response);
    return response;
  }
}

final onboardingProvider = StateNotifierProvider<OnboardingNotifier, OnboardingState>((ref) {
  return OnboardingNotifier(ref.watch(planRepositoryProvider));
});
