import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../plan/data/plan_repository.dart';
import '../../plan/data/long_horizon_repository.dart';
import '../../../core/models/running_background.dart';
import '../../../core/models/recent_race_result.dart';
import '../../../core/models/preparation_runway.dart';
import '../../../core/network/dtos.dart';
import '../../../core/network/long_horizon_dtos.dart';

/// The existing static race-plan route supports 8-20 week plans. Any race
/// whose start-to-race span exceeds this is routed to the Long-Horizon
/// (21-52 week) endpoint instead. This is a client-side ROUTING decision
/// only (which endpoint to call) — the backend alone decides the resulting
/// schedule, phases, and checkpoint structure; nothing here computes a plan.
const int _staticRaceMaxWeeks = 20;

class OnboardingState {
  OnboardingState({
    this.goalType = 'habit',
    this.goalDistance = 'five_k',
    this.runningBackground = RunningBackground.beginner,
    this.daysPerWeek = 3,
    this.unit = 'km',
    this.raceName,
    this.raceDate,
    this.targetFinishTimeSeconds,
    this.targetFinishTimeSource,
    this.longRunDay = 'Sunday',
    this.startDate,
    this.previewResponse,
    this.longHorizonPreviewResponse,
    this.selectedRunningDays = const [],
    this.preferredRunDuration = 30,
    this.customGoalDistance = 5,
    this.customGoalType = 'finish',
    this.customGoalTime = 50,
    this.habitGoal = 'five_k',
    this.recentWeeklyVolumeKm,
    this.recentLongestRunKm,
    this.recentRaceResult,
  });

  final String goalType;
  final String goalDistance;

  /// Running Background V2 — replaces the prior raw-string `level` field.
  final RunningBackground runningBackground;

  final int daysPerWeek;
  final String unit;
  final String? raceName;
  final String? raceDate;
  final int? targetFinishTimeSeconds;

  /// How [targetFinishTimeSeconds] was derived — one of
  /// [TargetFinishTimeSourceWire.productAverage]/[TargetFinishTimeSourceWire.userDefined].
  /// Always set atomically with [targetFinishTimeSeconds] (see
  /// [OnboardingNotifier.setProductAverageTarget]/[OnboardingNotifier.setUserDefinedTarget])
  /// — never left stale/mismatched with the value.
  final String? targetFinishTimeSource;

  final String longRunDay;
  final DateTime? startDate;
  final GeneratePreviewResponse? previewResponse;

  /// Set instead of [previewResponse] when the race span routes to the
  /// Long-Horizon (21-52 week) endpoint. Exactly one of the two is
  /// populated after a successful [OnboardingNotifier.generatePreview] call
  /// for a race goal — never both, never neither.
  final LongHorizonPlanPreviewContract? longHorizonPreviewResponse;
  final List<String> selectedRunningDays;
  final int preferredRunDuration;
  final int customGoalDistance;
  final String customGoalType; // "finish" | "steady" | "time"
  final int customGoalTime;
  final String habitGoal;

  /// Average weekly distance over the last 4 weeks, in kilometers (canonical
  /// storage unit — the details screen converts to/from the user's
  /// displayed unit at the input boundary). `null` = "I'm not sure" /
  /// not answered. `0` is a distinct, explicit value, never conflated with
  /// `null`.
  final double? recentWeeklyVolumeKm;

  /// Longest single run in the last 4 weeks, in kilometers. Same
  /// null-vs-zero semantics as [recentWeeklyVolumeKm].
  final double? recentLongestRunKm;

  /// Optional previously-completed race result, distinct from the target
  /// race entered in Enter Race Details.
  final RecentRaceResult? recentRaceResult;

  // ── Phase 4H.1 — derived Preparation Runway preview state ──────────────
  // All derived from [previewResponse]; never separately stored/mutated
  // (PART 7: "do not store duplicate mutable versions of the same values
  // when they can be derived safely").

  /// [PreviewLifecycle.unknown] (fails closed) when no preview has been
  /// generated yet — distinct from the backend's own `unknown` case, but
  /// with the identical safe effect: the confirm CTA never enables itself
  /// from an absent preview.
  PreviewLifecycle get previewLifecycle =>
      previewResponse == null ? PreviewLifecycle.unknown : previewResponse!.lifecycleValue;

  /// The sole gate for whether the confirm CTA may be enabled/tapped
  /// (PART 11) — never derived from [totalPreviewWeekCount] or any other
  /// field.
  bool get isPreviewConfirmable => previewLifecycle.isConfirmable;

  bool get isPreparationRunwayPreview => previewResponse?.isPreparationRunwayPlan ?? false;

  int get runwayWeekCount => previewResponse?.runwayWeekCount ?? 0;

  int get coreWeekCount => previewResponse?.coreWeekCount ?? 0;

  int get totalPreviewWeekCount => previewResponse?.totalWeekCount ?? 0;

  /// Whether the most recently generated preview is the Long-Horizon shape.
  bool get isLongHorizonPreview => longHorizonPreviewResponse != null;

  /// Long-Horizon's own confirmability gate — mirrors [isPreviewConfirmable]
  /// for the static shape but reads the backend's `confirmation_readiness`
  /// field instead of [PreviewLifecycle].
  bool get isLongHorizonPreviewConfirmable =>
      longHorizonPreviewResponse?.isConfirmable ?? false;

  OnboardingState copyWith({
    String? goalType,
    String? goalDistance,
    RunningBackground? runningBackground,
    int? daysPerWeek,
    String? unit,
    String? raceName,
    String? raceDate,
    int? targetFinishTimeSeconds,
    String? targetFinishTimeSource,
    String? longRunDay,
    DateTime? startDate,
    GeneratePreviewResponse? previewResponse,
    bool clearPreviewResponse = false,
    LongHorizonPlanPreviewContract? longHorizonPreviewResponse,
    bool clearLongHorizonPreviewResponse = false,
    List<String>? selectedRunningDays,
    int? preferredRunDuration,
    int? customGoalDistance,
    String? customGoalType,
    int? customGoalTime,
    String? habitGoal,
    double? recentWeeklyVolumeKm,
    bool clearRecentWeeklyVolumeKm = false,
    double? recentLongestRunKm,
    bool clearRecentLongestRunKm = false,
    RecentRaceResult? recentRaceResult,
    bool clearRecentRaceResult = false,
  }) {
    return OnboardingState(
      goalType: goalType ?? this.goalType,
      goalDistance: goalDistance ?? this.goalDistance,
      runningBackground: runningBackground ?? this.runningBackground,
      daysPerWeek: daysPerWeek ?? this.daysPerWeek,
      unit: unit ?? this.unit,
      raceName: raceName ?? this.raceName,
      raceDate: raceDate ?? this.raceDate,
      targetFinishTimeSeconds: targetFinishTimeSeconds ?? this.targetFinishTimeSeconds,
      targetFinishTimeSource: targetFinishTimeSource ?? this.targetFinishTimeSource,
      longRunDay: longRunDay ?? this.longRunDay,
      startDate: startDate ?? this.startDate,
      previewResponse: clearPreviewResponse ? null : (previewResponse ?? this.previewResponse),
      longHorizonPreviewResponse: clearLongHorizonPreviewResponse
          ? null
          : (longHorizonPreviewResponse ?? this.longHorizonPreviewResponse),
      selectedRunningDays: selectedRunningDays ?? this.selectedRunningDays,
      preferredRunDuration: preferredRunDuration ?? this.preferredRunDuration,
      customGoalDistance: customGoalDistance ?? this.customGoalDistance,
      customGoalType: customGoalType ?? this.customGoalType,
      customGoalTime: customGoalTime ?? this.customGoalTime,
      habitGoal: habitGoal ?? this.habitGoal,
      recentWeeklyVolumeKm: clearRecentWeeklyVolumeKm
          ? null
          : (recentWeeklyVolumeKm ?? this.recentWeeklyVolumeKm),
      recentLongestRunKm: clearRecentLongestRunKm
          ? null
          : (recentLongestRunKm ?? this.recentLongestRunKm),
      recentRaceResult: clearRecentRaceResult
          ? null
          : (recentRaceResult ?? this.recentRaceResult),
    );
  }
}

class OnboardingNotifier extends StateNotifier<OnboardingState> {
  OnboardingNotifier(this._planRepository, this._longHorizonRepository) : super(OnboardingState());

  final PlanRepository _planRepository;
  final LongHorizonRepository _longHorizonRepository;

  void updateGoalType(String type) => state = state.copyWith(goalType: type);
  void updateGoalDistance(String dist) => state = state.copyWith(goalDistance: dist);

  /// Sets the selected running background. When changing TO
  /// [RunningBackground.beginner] from any other level, any previously
  /// entered recent-running details are cleared immediately so a later
  /// preview request never carries stale hidden values for a Beginner
  /// selection (see RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md).
  void updateRunningBackground(RunningBackground level) {
    final becameBeginner = level == RunningBackground.beginner &&
        state.runningBackground != RunningBackground.beginner;
    state = state.copyWith(
      runningBackground: level,
      clearRecentWeeklyVolumeKm: becameBeginner,
      clearRecentLongestRunKm: becameBeginner,
      clearRecentRaceResult: becameBeginner,
    );
  }

  void updateDaysPerWeek(int days) => state = state.copyWith(daysPerWeek: days);
  void updateUnit(String unit) => state = state.copyWith(unit: unit);
  void updateRaceDetails(String name, String date) =>
      state = state.copyWith(raceName: name, raceDate: date);

  /// Sets the target finish time to the canonical product-average value for
  /// the current goal distance, tagging the source atomically. Callers
  /// resolve the canonical seconds themselves (see
  /// `AverageFinishTimePolicy.secondsForDistanceKm`) — this setter only
  /// records the (value, source) pair together, in one state write, so a
  /// stale custom value can never remain paired with `product_average`.
  void setProductAverageTarget(int seconds) {
    state = state.copyWith(
      targetFinishTimeSeconds: seconds,
      targetFinishTimeSource: TargetFinishTimeSourceWire.productAverage,
    );
  }

  /// Sets a user-entered target finish time, tagging the source atomically
  /// — a stale `product_average` source can never remain paired with a
  /// custom value.
  void setUserDefinedTarget(int seconds) {
    state = state.copyWith(
      targetFinishTimeSeconds: seconds,
      targetFinishTimeSource: TargetFinishTimeSourceWire.userDefined,
    );
  }

  void updateLongRunDay(String day) => state = state.copyWith(longRunDay: day);
  void updateStartDate(DateTime date) => state = state.copyWith(startDate: date);
  void updateSelectedRunningDays(List<String> days) => state = state.copyWith(selectedRunningDays: days);
  void updatePreferredRunDuration(int mins) => state = state.copyWith(preferredRunDuration: mins);
  void updateHabitGoal(String goal) => state = state.copyWith(habitGoal: goal);

  void updateCustomGoalDetails({int? distance, String? type, int? time}) {
    state = state.copyWith(
      customGoalDistance: distance ?? state.customGoalDistance,
      customGoalType: type ?? state.customGoalType,
      customGoalTime: time ?? state.customGoalTime,
    );
  }

  /// `null` means "I'm not sure" (not answered). A non-null value
  /// (including `0`) is an explicit, distinct value.
  void updateRecentWeeklyVolumeKm(double? km) {
    state = state.copyWith(
      recentWeeklyVolumeKm: km,
      clearRecentWeeklyVolumeKm: km == null,
    );
  }

  /// Same null/explicit-zero semantics as [updateRecentWeeklyVolumeKm].
  void updateRecentLongestRunKm(double? km) {
    state = state.copyWith(
      recentLongestRunKm: km,
      clearRecentLongestRunKm: km == null,
    );
  }

  void updateRecentRaceResult(RecentRaceResult? result) {
    state = state.copyWith(
      recentRaceResult: result,
      clearRecentRaceResult: result == null,
    );
  }

  /// Canonical lowercase 3-letter weekday token (mon..sun) the backend
  /// requires — converts from the full day name ("Monday") this state
  /// stores internally.
  static String _weekdayToken(String fullDayName) {
    final normalized = fullDayName.trim().toLowerCase();
    return normalized.length >= 3 ? normalized.substring(0, 3) : normalized;
  }

  static String _formatDate(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

  /// Generates a preview and stores it in state. For race goals whose
  /// start-to-race span exceeds [_staticRaceMaxWeeks], this routes to the
  /// Long-Horizon endpoint and populates [OnboardingState.longHorizonPreviewResponse]
  /// instead of [OnboardingState.previewResponse] — exactly one of the two
  /// is ever set by a single call. The 20-week threshold only selects which
  /// backend endpoint is called; the backend alone decides the resulting
  /// preview's actual shape and readiness.
  Future<void> generatePreview() async {
    if (state.selectedRunningDays.isEmpty) {
      throw StateError('generatePreview: selectedRunningDays must be set before generating a preview.');
    }
    if (state.startDate == null) {
      throw StateError('generatePreview: startDate must be set before generating a preview.');
    }

    final preferredDays = state.selectedRunningDays.map(_weekdayToken).toList();
    final startDate = _formatDate(state.startDate!);

    if (state.goalType == 'race' && _isLongHorizonSpan()) {
      final request = _buildRacePreviewRequest(preferredDays, startDate);
      final response = await _longHorizonRepository.generateLongHorizonRacePlanPreview(request);
      state = state.copyWith(longHorizonPreviewResponse: response, clearPreviewResponse: true);
      return;
    }

    final response = state.goalType == 'race'
        ? await _generateRacePreview(preferredDays, startDate)
        : await _generateHabitPreview(preferredDays, startDate);

    state = state.copyWith(previewResponse: response, clearLongHorizonPreviewResponse: true);
  }

  /// True when the race's start-to-race span exceeds the static route's
  /// 20-week ceiling. Only meaningful for `goalType == 'race'` — habit
  /// plans never have a race date and are never routed to Long-Horizon.
  bool _isLongHorizonSpan() {
    if (state.raceDate == null || state.startDate == null) return false;
    final raceDate = DateTime.tryParse(state.raceDate!);
    if (raceDate == null) return false;
    final days = raceDate.difference(state.startDate!).inDays;
    final weeks = (days / 7).ceil();
    return weeks > _staticRaceMaxWeeks;
  }

  GenerateRacePlanPreviewRequestDto _buildRacePreviewRequest(List<String> preferredDays, String startDate) {
    if (state.raceDate == null) {
      throw StateError('generatePreview: race plans require raceDate before generating a preview.');
    }
    if (state.targetFinishTimeSeconds == null || state.targetFinishTimeSeconds! <= 0) {
      throw StateError(
          'generatePreview: race plans require a resolved positive targetFinishTimeSeconds '
          '(custom entry or "Go with average") before generating a preview.');
    }
    if (state.targetFinishTimeSource == null) {
      throw StateError(
          'generatePreview: race plans require target_finish_time_source to be set atomically '
          'with target_finish_time_seconds before generating a preview.');
    }

    // Beginner never sends recent-running readiness fields — the details
    // screen is skipped entirely for that level and no stale values are
    // ever accumulated in state (updateRunningBackground clears them on
    // transition to Beginner), so this is a defensive re-assertion, not the
    // only enforcement point.
    final isBeginner = state.runningBackground == RunningBackground.beginner;
    final recentRaceResult = isBeginner ? null : state.recentRaceResult;

    return GenerateRacePlanPreviewRequestDto(
      goalDistance: state.goalDistance,
      level: state.runningBackground.wireValue,
      daysPerWeek: state.daysPerWeek,
      unit: state.unit,
      startDate: startDate,
      preferredDays: preferredDays,
      longRunDay: _weekdayToken(state.longRunDay),
      raceName: state.raceName,
      raceDate: state.raceDate!,
      targetFinishTimeSeconds: state.targetFinishTimeSeconds!,
      targetFinishTimeSource: state.targetFinishTimeSource!,
      recentWeeklyVolumeKm: isBeginner ? null : state.recentWeeklyVolumeKm,
      recentLongestRunKm: isBeginner ? null : state.recentLongestRunKm,
      recentRace: recentRaceResult == null
          ? null
          : RecentRaceRequest(
              distance: recentRaceResult.distanceToken,
              finishTimeSeconds: recentRaceResult.finishTimeSeconds,
              raceDate: _formatDate(recentRaceResult.raceDate),
            ),
    );
  }

  Future<GeneratePreviewResponse> _generateRacePreview(List<String> preferredDays, String startDate) async {
    final request = _buildRacePreviewRequest(preferredDays, startDate);
    return _planRepository.generateRacePlanPreview(request);
  }

  Future<GeneratePreviewResponse> _generateHabitPreview(List<String> preferredDays, String startDate) async {
    final request = GenerateHabitPlanPreviewRequestDto(
      goalDistance: state.goalDistance,
      level: state.runningBackground.wireValue,
      daysPerWeek: state.daysPerWeek,
      unit: state.unit,
      startDate: startDate,
      preferredDays: preferredDays,
      // Habit's long-run-day preference isn't currently collected by any
      // onboarding screen (state.longRunDay defaults to 'Sunday' and is
      // only ever changed by the race-only long-run-day page) -- sending
      // that stale default here would misrepresent it as a real answer, so
      // it's left null for habit, matching the public contract's "optional"
      // semantics (unknown, not "Sunday").
      longRunDay: null,
    );
    return _planRepository.generateHabitPlanPreview(request);
  }

  /// Resets all onboarding answers (goal, running background, recent-running
  /// inputs, recent race result, selected days, start date, preview draft,
  /// etc.) back to fresh defaults. Called after a plan is successfully
  /// confirmed (so a later onboarding attempt never starts pre-filled with a
  /// previous plan's answers) and after an active plan is cancelled (so
  /// "Create a plan" starts empty). Must never be called on confirm
  /// *failure* — a failed confirm keeps every answer so the user can retry
  /// without re-entering anything.
  void reset() => state = OnboardingState();
}

final onboardingProvider = StateNotifierProvider<OnboardingNotifier, OnboardingState>((ref) {
  return OnboardingNotifier(ref.watch(planRepositoryProvider), ref.watch(longHorizonRepositoryProvider));
});
