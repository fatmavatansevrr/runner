// Phase 4H.5 — the first reusable page-level widget-test harness for the
// active-plan surfaces (Home/Calendar/Training Day Detail/Profile-cancel).
// See PHASE4H_5_...md §6-9 for the full architecture rationale. Key
// decisions, confirmed by repository inspection before writing this file:
//
// - Auth/router: a fresh, minimal `GoRouter` (no `redirect`, no
//   `refreshListenable`) never touches `FirebaseAuth` at all -- confirmed by
//   reading `AppRouter.router`'s config (the Firebase dependency is
//   instance-level, not global) and by the identical pattern already used
//   in `onboarding_confirm_cleanup_test.dart`/`preparation_runway_schedule_ui_test.dart`.
//   This harness follows that same precedent, extended to the additional
//   real routes Home/Calendar/Detail navigate to.
// - Clock: no injectable clock abstraction exists anywhere in this codebase
//   (confirmed by search) -- Home/Calendar call `DateTime.now()` directly
//   and un-injectably in several places. True time-freezing via DI is not
//   possible without a production seam this phase does not add (out of
//   scope: "do not add new product behavior merely to make tests
//   possible"). The workaround, disclosed explicitly: fixtures are built
//   relative to the REAL `DateTime.now()` at test-run time (see
//   `preparation_runway_active_plan_fixtures.dart`'s `testToday()`), so
//   date-dependent branches always resolve deterministically relative to
//   whenever the test actually runs, never against a fixed historical date
//   that would silently stop matching "today".
library;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:antigravity_app/core/network/api_client.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/calendar/data/calendar_provider.dart';
import 'package:antigravity_app/features/calendar/data/calendar_repository.dart';
import 'package:antigravity_app/features/calendar/presentation/calendar_page.dart';
import 'package:antigravity_app/features/home/data/home_repository.dart';
import 'package:antigravity_app/features/home/presentation/home_page.dart';
import 'package:antigravity_app/features/pending_confirmation/data/pending_confirmation_repository.dart';
import 'package:antigravity_app/features/pending_confirmation/presentation/pending_confirmation_page.dart';
import 'package:antigravity_app/features/plan/data/plan_repository.dart';
import 'package:antigravity_app/features/plan/presentation/plan_details_page.dart';
import 'package:antigravity_app/features/profile/data/profile_repository.dart';
import 'package:antigravity_app/features/profile/presentation/profile_page.dart';
import 'package:antigravity_app/features/training_day/data/training_day_repository.dart';
import 'package:antigravity_app/features/training_day/presentation/training_day_detail_page.dart';

// ── Scripted repositories ───────────────────────────────────────────────
//
// Every scripted repository: (a) returns from a caller-supplied queue/map,
// never a hardcoded value; (b) records every call for assertion (count +
// captured arguments); (c) throws immediately on a call the test did not
// expect to happen, per PART 2 ("Unexpected calls should fail the test
// immediately").

class ScriptedHomeRepository extends HomeRepository {
  ScriptedHomeRepository(
      {List<HomeResponse>? homeResponses,
      Object? completeError,
      Object? notTodayError})
      : _homeResponses = List.of(homeResponses ?? const []),
        _completeError = completeError,
        _notTodayError = notTodayError,
        super(ApiClient());

  final List<HomeResponse> _homeResponses;
  final Object? _completeError;
  final Object? _notTodayError;

  int fetchCallCount = 0;
  int completeCallCount = 0;
  int notTodayCreateCallCount = 0;
  int notTodayConfirmCallCount = 0;
  String? lastCompletedDayId;
  double? lastCompletedDistance;
  int? lastCompletedDuration;
  String? lastNotTodayDayId;
  String? lastNotTodayReason;

  @override
  Future<HomeResponse> fetchHomeData() async {
    fetchCallCount++;
    if (_homeResponses.isEmpty) {
      throw StateError(
          'ScriptedHomeRepository.fetchHomeData called with an empty response queue (unexpected call).');
    }
    // Last response repeats once the queue is exhausted, so a page that
    // refetches more times than scripted still gets a deterministic (not
    // crashing) response -- callers assert fetchCallCount explicitly for
    // exact-count checks.
    return _homeResponses.length > 1
        ? _homeResponses.removeAt(0)
        : _homeResponses.first;
  }

  @override
  Future<CompleteWorkoutResponse> completeWorkout(String trainingDayId,
      double actualDistanceKm, int actualDurationMin, String? userNote) async {
    completeCallCount++;
    lastCompletedDayId = trainingDayId;
    lastCompletedDistance = actualDistanceKm;
    lastCompletedDuration = actualDurationMin;
    if (_completeError != null) throw _completeError;
    return CompleteWorkoutResponse(dayId: trainingDayId, status: 'completed');
  }

  @override
  Future<CreateNotTodayDecisionResponse> createNotTodayDecision(
      String trainingDayId, String reason) async {
    notTodayCreateCallCount++;
    lastNotTodayDayId = trainingDayId;
    lastNotTodayReason = reason;
    if (_notTodayError != null) throw _notTodayError;
    return CreateNotTodayDecisionResponse(
        decisionId: 'decision-$trainingDayId', status: 'pending');
  }

  @override
  Future<ConfirmNotTodayDecisionResponse> confirmNotTodayDecision(
      String decisionId) async {
    notTodayConfirmCallCount++;
    return ConfirmNotTodayDecisionResponse(
        decisionId: decisionId, status: 'confirmed', action: 'no_change');
  }
}

class ScriptedCalendarRepository extends CalendarRepository {
  ScriptedCalendarRepository(this._responsesByMonth) : super(ApiClient());

  final Map<String, List<TrainingDayResponse>> _responsesByMonth;
  final List<String> requestedMonths = [];
  int get fetchCallCount => requestedMonths.length;

  @override
  Future<List<TrainingDayResponse>> fetchCalendarData(String month) async {
    requestedMonths.add(month);
    final response = _responsesByMonth[month];
    if (response == null) {
      throw StateError(
          'ScriptedCalendarRepository.fetchCalendarData called for unscripted month "$month" (unexpected call).');
    }
    return response;
  }
}

class ScriptedTrainingDayRepository extends TrainingDayRepository {
  ScriptedTrainingDayRepository(this._responsesById) : super(ApiClient());

  final Map<String, TrainingDayDetailResponse> _responsesById;
  final List<String> requestedDayIds = [];
  int get fetchCallCount => requestedDayIds.length;

  @override
  Future<TrainingDayDetailResponse> fetchTrainingDayDetail(
      String trainingDayId) async {
    requestedDayIds.add(trainingDayId);
    final response = _responsesById[trainingDayId];
    if (response == null) {
      // Real backend 404 behavior for a stale/cancelled/unknown day.
      throw Exception('404: Training day not found.');
    }
    return response;
  }
}

class ScriptedProfileRepository extends ProfileRepository {
  ScriptedProfileRepository(
      {required ProfileOverviewResponse overview,
      required PlanDetailsResponse planDetails})
      : _overview = overview,
        _planDetails = planDetails,
        super(ApiClient());

  ProfileOverviewResponse _overview;
  PlanDetailsResponse _planDetails;
  int overviewFetchCallCount = 0;
  int planDetailsFetchCallCount = 0;

  void setPlanDetails(PlanDetailsResponse planDetails) =>
      _planDetails = planDetails;

  @override
  Future<ProfileOverviewResponse> fetchProfileOverview() async {
    overviewFetchCallCount++;
    return _overview;
  }

  @override
  Future<PlanDetailsResponse> fetchActivePlanDetails() async {
    planDetailsFetchCallCount++;
    return _planDetails;
  }
}

class ScriptedPlanRepository extends PlanRepository {
  ScriptedPlanRepository({Object? cancelError})
      : _cancelError = cancelError,
        super(ApiClient());

  final Object? _cancelError;
  int cancelCallCount = 0;
  String? lastCancelledPlanId;
  String? lastCancelReason;

  @override
  Future<CancelPlanResponse> cancelPlan(String planId, String reason) async {
    cancelCallCount++;
    lastCancelledPlanId = planId;
    lastCancelReason = reason;
    if (_cancelError != null) throw _cancelError;
    return CancelPlanResponse(planId: planId, status: 'cancelled');
  }
}

class ScriptedPendingConfirmationRepository
    extends PendingConfirmationRepository {
  ScriptedPendingConfirmationRepository(
      {List<PendingConfirmationResponse> items = const [],
      Object? resolveError})
      : _items = items,
        _resolveError = resolveError,
        super(ApiClient());

  final List<PendingConfirmationResponse> _items;
  final Object? _resolveError;
  int fetchCallCount = 0;
  int resolveCallCount = 0;

  @override
  Future<List<PendingConfirmationResponse>> fetchPendingConfirmations() async {
    fetchCallCount++;
    return _items;
  }

  @override
  Future<ResolvePendingConfirmationResponse> resolvePendingConfirmation({
    required String pendingConfirmationId,
    required String resolution,
    double? actualDistanceKm,
    int? actualDurationMin,
    String? userNote,
  }) async {
    resolveCallCount++;
    if (_resolveError != null) throw _resolveError;
    return ResolvePendingConfirmationResponse(
        pendingConfirmationId: pendingConfirmationId, status: 'resolved');
  }
}

// ── Test router ──────────────────────────────────────────────────────────
//
// Real route paths (from `AppRoutes`), no `redirect`/`refreshListenable` --
// never touches FirebaseAuth. Extends the 2-route precedent already
// established by `onboarding_confirm_cleanup_test.dart` with every
// destination Home/Calendar/Detail/Profile can navigate to.

GoRouter buildActivePlanTestRouter({required String initialLocation}) =>
    GoRouter(
      initialLocation: initialLocation,
      routes: [
        GoRoute(path: AppRoutes.home, builder: (_, __) => const HomePage()),
        GoRoute(
            path: AppRoutes.calendar, builder: (_, __) => const CalendarPage()),
        GoRoute(
            path: AppRoutes.profile, builder: (_, __) => const ProfilePage()),
        GoRoute(
            path: AppRoutes.planDetails,
            builder: (_, __) => const PlanDetailsPage()),
        GoRoute(
          path: AppRoutes.trainingDayDetail,
          builder: (context, state) =>
              TrainingDayDetailPage(dayId: state.pathParameters['dayId']!),
        ),
        GoRoute(
            path: AppRoutes.pendingConfirmation,
            builder: (_, __) => const PendingConfirmationPage()),
        GoRoute(
            path: AppRoutes.goalSelection,
            builder: (_, __) =>
                const Scaffold(body: Text('GOAL_SELECTION_PLACEHOLDER'))),
      ],
    );

// ── Harness bundle ───────────────────────────────────────────────────────

class ActivePlanTestHarnessBundle {
  ActivePlanTestHarnessBundle({
    required this.homeRepo,
    required this.calendarRepo,
    required this.trainingDayRepo,
    required this.profileRepo,
    required this.planRepo,
    required this.pendingRepo,
    required this.router,
  });

  final ScriptedHomeRepository homeRepo;
  final ScriptedCalendarRepository calendarRepo;
  final ScriptedTrainingDayRepository trainingDayRepo;
  final ScriptedProfileRepository profileRepo;
  final ScriptedPlanRepository planRepo;
  final ScriptedPendingConfirmationRepository pendingRepo;
  final GoRouter router;
}

/// Pumps a real page (Home/Calendar/Detail/Profile, reached via [initialLocation])
/// inside a fully-scripted `ProviderScope` + minimal real-route `GoRouter`.
/// Returns the scripted repositories/router so the test can assert call
/// counts, captured arguments, and (via `router.go`) navigation state.
Future<ActivePlanTestHarnessBundle> pumpActivePlanApp(
  WidgetTester tester, {
  required String initialLocation,
  required HomeResponse homeResponse,
  List<HomeResponse>? additionalHomeResponses,
  Map<String, List<TrainingDayResponse>> calendarResponsesByMonth = const {},
  Map<String, TrainingDayDetailResponse> detailResponsesById = const {},
  required ProfileOverviewResponse profileOverview,
  required PlanDetailsResponse planDetails,
  String initialCalendarMonth = '',
  Object? completeError,
  Object? notTodayError,
  Object? cancelError,
  List<PendingConfirmationResponse> pendingItems = const [],
  Object? resolveError,
}) async {
  final homeRepo = ScriptedHomeRepository(
    homeResponses: [homeResponse, ...?additionalHomeResponses],
    completeError: completeError,
    notTodayError: notTodayError,
  );
  final calendarRepo = ScriptedCalendarRepository(calendarResponsesByMonth);
  final trainingDayRepo = ScriptedTrainingDayRepository(detailResponsesById);
  final profileRepo = ScriptedProfileRepository(
      overview: profileOverview, planDetails: planDetails);
  final planRepo = ScriptedPlanRepository(cancelError: cancelError);
  final pendingRepo = ScriptedPendingConfirmationRepository(
      items: pendingItems, resolveError: resolveError);
  final router = buildActivePlanTestRouter(initialLocation: initialLocation);

  final overrides = <Override>[
    homeRepositoryProvider.overrideWithValue(homeRepo),
    calendarRepositoryProvider.overrideWithValue(calendarRepo),
    trainingDayRepositoryProvider.overrideWithValue(trainingDayRepo),
    profileRepositoryProvider.overrideWithValue(profileRepo),
    planRepositoryProvider.overrideWithValue(planRepo),
    pendingConfirmationRepositoryProvider.overrideWithValue(pendingRepo),
  ];
  if (initialCalendarMonth.isNotEmpty) {
    overrides
        .add(calendarMonthProvider.overrideWith((ref) => initialCalendarMonth));
  }

  await tester.pumpWidget(ProviderScope(
    overrides: overrides,
    child: MaterialApp.router(routerConfig: router),
  ));
  await tester.pumpAndSettle();

  return ActivePlanTestHarnessBundle(
    homeRepo: homeRepo,
    calendarRepo: calendarRepo,
    trainingDayRepo: trainingDayRepo,
    profileRepo: profileRepo,
    planRepo: planRepo,
    pendingRepo: pendingRepo,
    router: router,
  );
}
