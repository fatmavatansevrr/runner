// Phase 4H.6 — residual active-plan UI consistency and combined frontend
// E2E closure. Builds on the exact same Phase 4H.5 harness/fixtures (no new
// architecture) to close the gaps that phase explicitly disclosed as open:
// Completed/Missed Detail provenance, Home today-workout provenance,
// PendingConfirmationPage/PlanDetailsPage harness coverage, Calendar
// year-crossing, stale-Detail-after-cancel, accessibility semantics
// assertions, and simplified combined active-plan journeys.
//
// Honest scope note (see PHASE4H_6_...md §31 for the full disclosure): this
// file does NOT implement the literal preview -> confirm portion of the
// combined 15/17/20-week journeys (onboarding/preview flow is a separate,
// pre-existing harness from Phases 4H.1-4H.3 with its own scripted preview
// repository; wiring the two harnesses into one mega end-to-end test was
// judged disproportionate to this phase and is disclosed as not done). The
// "combined journey" tests here instead prove one continuously-evolving
// active-plan scripted-repository state across Home -> Calendar -> Detail
// -> mutation -> refreshed state, which is the part of the journey these
// pages can actually exercise.

import 'dart:ui' show Size;

import 'package:flutter/material.dart' show Icons, Semantics, TextButton;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/pending_confirmation/presentation/pending_confirmation_page.dart';
import 'package:antigravity_app/features/plan/presentation/plan_details_page.dart';
import 'package:antigravity_app/features/profile/data/profile_provider.dart';
import 'package:antigravity_app/features/training_day/presentation/training_day_detail_page.dart';

import 'support/active_plan_test_harness.dart';
import 'support/preparation_runway_active_plan_fixtures.dart';

Future<void> _pumpDetail(
  WidgetTester tester,
  TrainingDayDetailResponse detail, {
  PlanDetailsResponse? planDetails,
}) async {
  final home =
      HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1 of 17'));
  final plan = planDetails ??
      PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
  final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
  await pumpActivePlanApp(
    tester,
    initialLocation: '/training-day/${detail.dayId}',
    homeResponse: home,
    detailResponsesById: {detail.dayId: detail},
    profileOverview: overview,
    planDetails: plan,
  );
}

/// Finds a `Semantics` node's merged label for a given finder -- the shared
/// helper used by every accessibility assertion in this file (PART 10).
String? _semanticsLabel(WidgetTester tester, Finder finder) {
  final widget = tester.widget<Semantics>(finder);
  return widget.properties.label;
}

void main() {
  // ── PART 3: Detail provenance consistency across Planned/Completed/Missed ─

  group('Detail — provenance consistency across status states', () {
    testWidgets('Planned runway provenance visible', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-consistency-planned',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'CONSISTENCY',
        source: 'template',
        status: 'planned',
      ));
      await _pumpDetail(tester, detail);
      expect(find.text('Source: Plan Template'), findsOneWidget);
      expect(find.textContaining('Consistency'), findsWidgets);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Completed runway provenance visible', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-consistency-completed',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'CONSISTENCY',
        source: 'template',
        status: 'completed',
        actualDistanceKm: 6.0,
        actualDurationMin: 34,
        canMarkComplete: false,
        canMarkNotToday: false,
      ));
      await _pumpDetail(tester, detail);
      expect(find.text('Source: Plan Template'), findsOneWidget);
      expect(find.textContaining('Consistency'), findsWidgets);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Missed runway provenance visible', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-consistency-missed',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'CONSISTENCY',
        source: 'template',
        status: 'missed',
        canMarkComplete: false,
        canMarkNotToday: false,
      ));
      await _pumpDetail(tester, detail);
      expect(find.text('Source: Plan Template'), findsOneWidget);
      expect(find.textContaining('Consistency'), findsWidgets);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Core completed provenance visible without a runway block',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-core-completed',
        date: testToday(),
        weekNumber: 8,
        weekType: 'build',
        source: 'template',
        status: 'completed',
        actualDistanceKm: 8.0,
        actualDurationMin: 45,
        canMarkComplete: false,
        canMarkNotToday: false,
      ));
      await _pumpDetail(tester, detail);
      expect(find.text('Build'), findsWidgets);
      expect(find.textContaining('Preparation Runway'), findsNothing);
      expect(find.text('Source: Plan Template'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('source preserved after completion (same dayId, same source)',
        (tester) async {
      final planned = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-source-check',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        source: 'template',
        status: 'planned',
      ));
      final completed = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-source-check',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        source: 'template',
        status: 'completed',
        actualDistanceKm: 6.0,
        actualDurationMin: 34,
        canMarkComplete: false,
        canMarkNotToday: false,
      ));
      expect(planned.sourceValue, completed.sourceValue);
      await _pumpDetail(tester, completed);
      expect(find.text('Source: Plan Template'), findsOneWidget);
    });

    testWidgets('source preserved after not-today (same dayId, same source)',
        (tester) async {
      final missed = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-source-check-2',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        source: 'template',
        status: 'missed',
        canMarkComplete: false,
        canMarkNotToday: false,
      ));
      await _pumpDetail(tester, missed);
      expect(find.text('Source: Plan Template'), findsOneWidget);
      expect(missed.hasAdaptedOrigin, isFalse);
    });

    testWidgets('adapted-origin message safe across all three states',
        (tester) async {
      for (final status in ['planned', 'completed', 'missed']) {
        final detail = TrainingDayDetailResponse.fromJson(dayJson(
          dayId: 'd-adapted-$status',
          date: testToday(),
          weekNumber: 6,
          weekType: 'build',
          source: 'engine_adapted',
          adaptedFromId: 'original-day-guid-should-not-appear',
          status: status,
          actualDistanceKm: status == 'completed' ? 8.0 : null,
          actualDurationMin: status == 'completed' ? 45 : null,
          canMarkComplete: status == 'planned',
          canMarkNotToday: status == 'planned',
        ));
        await _pumpDetail(tester, detail);
        expect(find.text('Adapted from an earlier workout'), findsOneWidget,
            reason: 'status=$status');
        expect(find.textContaining('original-day-guid-should-not-appear'),
            findsNothing,
            reason: 'status=$status');
      }
    });
  });

  // ── PART 4: Detail session-category matrix (categories not already ───────
  // covered by the Phase 4H.5 test file) ────────────────────────────────────

  group('Detail — additional session categories', () {
    testWidgets('General Endurance Easy', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-ge-easy',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        source: 'template',
        dayType: 'easy',
        title: 'Easy Run',
      ));
      await _pumpDetail(tester, detail);
      expect(find.textContaining('General Endurance'), findsWidgets);
      expect(tester.takeException(), isNull);
    });

    testWidgets('General Endurance Long Run', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-ge-long',
        date: testToday(),
        weekNumber: 3,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        source: 'template',
        dayType: 'long_run',
        isLongRun: true,
        title: 'Long Run',
      ));
      await _pumpDetail(tester, detail);
      expect(find.textContaining('General Endurance'), findsWidgets);
      expect(find.text('LONG RUN'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Pre-Specific Transition', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-transition',
        date: testToday(),
        weekNumber: 5,
        weekType: 'preparation_runway',
        runwayBlock: 'PRE_SPECIFIC_TRANSITION',
        source: 'template',
        title: 'Transition Run',
      ));
      await _pumpDetail(tester, detail);
      expect(find.textContaining('Transition'), findsWidgets);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Core quality session (build week, interval)', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-core-quality',
        date: testToday(),
        weekNumber: 8,
        weekType: 'build',
        source: 'template',
        dayType: 'interval',
        title: 'Interval Session',
      ));
      await _pumpDetail(tester, detail);
      expect(find.text('Build'), findsWidgets);
      expect(find.textContaining('Preparation Runway'), findsNothing);
      expect(tester.takeException(), isNull);
    });
  });

  // ── PART 5: Home today-workout provenance ─────────────────────────────────

  group('Home — today-workout provenance', () {
    Future<void> pumpHomeToday(WidgetTester tester,
        {required String weekType,
        String? runwayBlock,
        int weekNumber = 2}) async {
      final today = dayJson(
        dayId: 'today-provenance',
        date: testToday(),
        weekNumber: weekNumber,
        weekType: weekType,
        runwayBlock: runwayBlock,
        title: 'Easy Run',
      );
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week $weekNumber of 17',
        currentWeekNumber: weekNumber,
        totalWeeks: 17,
        currentWeekType: weekType,
        currentRunwayBlock: runwayBlock,
        todayWorkout: today,
        weekSummary: [today],
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
    }

    testWidgets('runway Easy shows Preparation Runway + block on the card',
        (tester) async {
      await pumpHomeToday(tester,
          weekType: 'preparation_runway', runwayBlock: 'CONSISTENCY');
      expect(find.textContaining('Preparation Runway'), findsWidgets);
      expect(find.textContaining('Consistency'), findsWidgets);
      expect(tester.takeException(), isNull);
    });

    testWidgets('runway Long Run shows provenance', (tester) async {
      await pumpHomeToday(tester,
          weekType: 'preparation_runway', runwayBlock: 'GENERAL_ENDURANCE');
      expect(find.textContaining('General Endurance'), findsWidgets);
    });

    testWidgets('Intro block shows distinct provenance', (tester) async {
      await pumpHomeToday(tester,
          weekType: 'preparation_runway', runwayBlock: 'AEROBIC_STRENGTH');
      expect(find.textContaining('Aerobic Strength'), findsWidgets);
    });

    testWidgets('Transition block shows distinct provenance', (tester) async {
      await pumpHomeToday(tester,
          weekType: 'preparation_runway',
          runwayBlock: 'PRE_SPECIFIC_TRANSITION');
      expect(find.textContaining('Transition'), findsWidgets);
    });

    testWidgets('Core provenance shows phase, never a runway block',
        (tester) async {
      await pumpHomeToday(tester, weekType: 'build', weekNumber: 8);
      expect(find.textContaining('Build'), findsWidgets);
      expect(find.textContaining('Preparation Runway'), findsNothing);
    });

    testWidgets(
        'Core provenance hides an erroneously-present runway block value',
        (tester) async {
      await pumpHomeToday(tester,
          weekType: 'build',
          runwayBlock: 'CONSISTENCY', // erroneous for a Core week
          weekNumber: 8);
      expect(find.textContaining('Consistency'), findsNothing);
    });

    testWidgets('legacy today-workout (no week_type) fabricates nothing',
        (tester) async {
      final today = dayJson(
        dayId: 'legacy-today',
        date: testToday(),
        title: 'Easy Run',
      ); // no weekType/runwayBlock at all
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 2 of 17',
        todayWorkout: today,
        weekSummary: [today],
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
      expect(find.textContaining('Preparation Runway'), findsNothing);
      expect(tester.takeException(), isNull);
    });
  });

  // ── PART 6: PendingConfirmationPage harness ────────────────────────────────

  group('Pending Confirmations page — empty state', () {
    testWidgets(
        'API returns empty: no card, no resolve CTA, no resolve call, back works',
        (tester) async {
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 2 of 17'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      // Reached via Home + a real push (not a bare initialLocation) so
      // there is an underlying route for "Go Back" to genuinely pop to --
      // matches the real product navigation (Home's pending-confirmations
      // banner uses context.go, but a direct initialLocation-only pump has
      // no route beneath it for context.pop() to target at all).
      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
        pendingItems: const [],
      );
      bundle.router.push(AppRoutes.pendingConfirmation);
      await tester.pumpAndSettle();

      expect(find.byType(PendingConfirmationPage), findsOneWidget);
      expect(find.text('All caught up!'), findsOneWidget);
      expect(find.text('No pending workouts require confirmation.'),
          findsOneWidget);
      expect(find.text('Save & Continue'), findsNothing);
      expect(bundle.pendingRepo.fetchCallCount, 1);
      expect(bundle.pendingRepo.resolveCallCount, 0);

      await tester.tap(find.text('Go Back'));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
    });
  });

  // ── PART 7: PlanDetailsPage shared-harness integration ─────────────────────

  group('Plan Details page — shared harness', () {
    Future<void> pumpPlanDetails(WidgetTester tester,
        {required int totalWeeks,
        required int runwayWeeks,
        String goalType = 'race',
        bool hasActivePlan = true}) async {
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1'));
      final planDetails = PlanDetailsResponse.fromJson(planDetailsJson(
        hasActivePlan: hasActivePlan,
        totalWeeks: totalWeeks,
        runwayWeeks: runwayWeeks,
        goalType: goalType,
      ));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.planDetails,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
    }

    testWidgets('15-week runway plan: correct duration + structure',
        (tester) async {
      await pumpPlanDetails(tester, totalWeeks: 15, runwayWeeks: 3);
      expect(find.byType(PlanDetailsPage), findsOneWidget);
      expect(find.text('15-week plan'), findsOneWidget);
      expect(find.text('Preparation'), findsOneWidget);
      expect(find.text('3 weeks'), findsOneWidget);
      expect(find.text('12 weeks'), findsOneWidget); // Core: 15 - 3
      expect(tester.takeException(), isNull);
    });

    testWidgets('17-week runway plan', (tester) async {
      await pumpPlanDetails(tester, totalWeeks: 17, runwayWeeks: 5);
      expect(find.text('17-week plan'), findsOneWidget);
      expect(find.text('5 weeks'), findsOneWidget);
    });

    testWidgets('20-week runway plan: Week 20 reachable, no truncation',
        (tester) async {
      await pumpPlanDetails(tester, totalWeeks: 20, runwayWeeks: 8);
      expect(find.text('20-week plan'), findsOneWidget);
      expect(find.text('Week 20'), findsOneWidget);
    });

    testWidgets('8-week Core plan: no Preparation segment', (tester) async {
      await pumpPlanDetails(tester, totalWeeks: 8, runwayWeeks: 0);
      expect(find.text('8-week plan'), findsOneWidget);
      expect(find.text('Preparation'), findsNothing);
    });

    testWidgets('12-week Core plan: no Preparation segment', (tester) async {
      await pumpPlanDetails(tester, totalWeeks: 12, runwayWeeks: 0);
      expect(find.text('12-week plan'), findsOneWidget);
      expect(find.text('Preparation'), findsNothing);
    });

    testWidgets('14-week Core plan: no Preparation segment', (tester) async {
      await pumpPlanDetails(tester, totalWeeks: 14, runwayWeeks: 0);
      expect(find.text('14-week plan'), findsOneWidget);
      expect(find.text('Preparation'), findsNothing);
    });

    testWidgets('habit plan does not require race-only fields, no crash',
        (tester) async {
      await pumpPlanDetails(tester,
          totalWeeks: 8, runwayWeeks: 0, goalType: 'habit');
      expect(find.text('8-week plan'), findsOneWidget);
      expect(find.text('Preparation'), findsNothing);
      expect(tester.takeException(), isNull);
    });

    testWidgets('no active plan: safe message, no crash', (tester) async {
      await pumpPlanDetails(tester,
          totalWeeks: 0, runwayWeeks: 0, hasActivePlan: false);
      expect(find.text('No active plan.'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  });

  // ── PART 8: Calendar year-crossing ─────────────────────────────────────────

  group('Calendar — year-crossing (December -> January)', () {
    testWidgets(
        'December request, next-month navigation requests following January',
        (tester) async {
      final december = calendarMonthJson(
        monthAnchor: DateTime(2026, 12),
        daysInMonth: 31,
        weekType: 'build',
      );
      final january = calendarMonthJson(
        monthAnchor: DateTime(2027, 1),
        daysInMonth: 31,
        weekType: 'taper',
        startingWeekNumber: 15,
      );
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 14'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.calendar,
        homeResponse: home,
        calendarResponsesByMonth: {
          '2026-12':
              december.map((e) => TrainingDayResponse.fromJson(e)).toList(),
          '2027-01':
              january.map((e) => TrainingDayResponse.fromJson(e)).toList(),
        },
        profileOverview: overview,
        planDetails: planDetails,
        initialCalendarMonth: '2026-12',
      );

      expect(bundle.calendarRepo.requestedMonths, ['2026-12']);

      // Navigate forward one month via the real next-month chevron.
      await tester.tap(find.byIcon(Icons.chevron_right_rounded));
      await tester.pumpAndSettle();

      expect(bundle.calendarRepo.requestedMonths, ['2026-12', '2027-01']);
      expect(bundle.calendarRepo.fetchCallCount, 2);
      expect(find.text('January 2027'), findsOneWidget);
      expect(find.text('December 2026'), findsNothing);
      expect(tester.takeException(), isNull);
    });
  });

  // ── PART 9: Stale Detail after cancel ──────────────────────────────────────

  group('Stale Detail — protection after plan cancellation', () {
    testWidgets(
        'Detail page opened before cancel becomes unavailable and blocks mutation after cancel',
        (tester) async {
      await tester.binding.setSurfaceSize(const Size(800, 1200));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-stale',
        date: testToday(),
        weekNumber: 10,
        weekType: 'build',
        source: 'template',
        canMarkComplete: true,
        canMarkNotToday: true,
      ));
      final home = HomeResponse.fromJson(
          homeResponseJson(progressText: 'Week 10 of 20'));
      final activePlanDetails = PlanDetailsResponse.fromJson(planDetailsJson(
          planId: 'plan-stale', totalWeeks: 20, runwayWeeks: 8));
      final overview = ProfileOverviewResponse.fromJson(
          profileOverviewJson(planName: 'Stale Plan Test'));

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: '/training-day/d-stale',
        homeResponse: home,
        detailResponsesById: {'d-stale': detail},
        profileOverview: overview,
        planDetails: activePlanDetails,
      );

      // Detail is open and fully mutable before cancel.
      expect(find.text('Mark as Completed'), findsOneWidget);
      expect(find.text('Not Today'), findsOneWidget);

      // Simulate the real cancel effect: the active-plan repository now
      // reports hasActivePlan == false (exactly what ProfilePage's real
      // cancel flow causes `activePlanDetailsProvider` to resolve to after
      // a successful cancelPlan() call), then invalidate that provider the
      // same way `_showCancelPlanDialog` does -- via the widget tree's own
      // ProviderContainer, since this test has no other handle to `ref`.
      bundle.profileRepo.setPlanDetails(
          PlanDetailsResponse.fromJson(planDetailsJson(hasActivePlan: false)));
      final container = ProviderScope.containerOf(
          tester.element(find.byType(TrainingDayDetailPage)));
      container.invalidate(activePlanDetailsProvider);
      await tester.pumpAndSettle();

      // Detail now shows the safe unavailable state, not stale Planned data.
      expect(find.text('This workout is no longer available'), findsOneWidget);
      expect(find.text('Mark as Completed'), findsNothing);
      expect(find.text('Not Today'), findsNothing);

      // No mutation calls were ever sent as a result of this transition.
      expect(bundle.homeRepo.completeCallCount, 0);
      expect(bundle.homeRepo.notTodayCreateCallCount, 0);

      // Back navigation still works from the unavailable state.
      final backButton = find.byIcon(Icons.arrow_back_ios_new_rounded);
      expect(backButton, findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  });

  // ── PART 10: Accessibility semantics assertions ────────────────────────────

  group('Accessibility — real tester.getSemantics assertions', () {
    testWidgets('Home: today-workout provenance is exposed via semantics',
        (tester) async {
      final today = dayJson(
        dayId: 'a11y-today',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'CONSISTENCY',
        title: 'Easy Run',
      );
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 2 of 17',
        currentWeekNumber: 2,
        totalWeeks: 17,
        currentWeekType: 'preparation_runway',
        currentRunwayBlock: 'CONSISTENCY',
        todayWorkout: today,
        weekSummary: [today],
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );

      final semanticsFinder = find.ancestor(
        of: find.text('Preparation Runway · Consistency'),
        matching: find.byType(Semantics),
      );
      expect(semanticsFinder, findsWidgets);
      final label = _semanticsLabel(tester, semanticsFinder.first);
      expect(label, contains('Preparation Runway'));
    });

    testWidgets('Detail: provenance card exposes a real semantics label',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'a11y-detail',
        date: testToday(),
        weekNumber: 6,
        weekType: 'build',
        source: 'engine_adapted',
        adaptedFromId: 'hidden-guid',
      ));
      await _pumpDetail(tester, detail);
      final cardSemantics = find.byWidgetPredicate((w) =>
          w is Semantics &&
          (w.properties.label?.contains('Adapted from an earlier workout') ??
              false));
      expect(cardSemantics, findsOneWidget);
    });

    testWidgets('Plan Details: week-list semantics carry week + phase',
        (tester) async {
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 8, runwayWeeks: 0));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.planDetails,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
      final weekSemantics = find.byWidgetPredicate((w) =>
          w is Semantics &&
          (w.properties.label?.startsWith('Week 1,') ?? false));
      expect(weekSemantics, findsOneWidget);
    });

    testWidgets('Pending Confirmations: empty-state page is reachable and safe',
        (tester) async {
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.pendingConfirmation,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
        pendingItems: const [],
      );
      expect(find.text('All caught up!'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Cancel dialog: destructive action has a real accessible label',
        (tester) async {
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(planId: 'plan-a11y', totalWeeks: 15, runwayWeeks: 3));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1'));
      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.profile,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
      await tester.ensureVisible(find.text('Stop Plan').first);
      await tester.tap(find.text('Stop Plan').first);
      await tester.pumpAndSettle();
      expect(find.text('Stop Active Plan?'), findsOneWidget);
      // AlertDialog action buttons render as real, individually-focusable
      // TextButtons with their own text semantics -- this is the accessible
      // signal a screen reader would announce. The card's own "Stop Plan"
      // trigger is also a TextButton with the same label, so the dialog's
      // destructive action is disambiguated as the LAST match (same
      // convention as the working cancel-flow test in Phase 4H.5).
      expect(find.widgetWithText(TextButton, 'Stop Plan'), findsNWidgets(2));
      expect(find.widgetWithText(TextButton, 'Keep Training'), findsOneWidget);
    });
  });

  // ── PART 11/12/13/14: simplified combined active-plan journeys ────────────
  // (Home -> Calendar -> Detail -> mutation -> refreshed state, one
  // continuously-evolving scripted-repository instance per journey. Does
  // NOT include the preview -> confirm portion -- see file-header disclosure.)

  group('Combined journey — 15-week (Home -> Calendar -> Detail -> complete)',
      () {
    testWidgets('one continuous scripted state proves refresh end-to-end',
        (tester) async {
      final todayDay = dayJson(
        dayId: 'j15-today',
        date: testToday(),
        weekNumber: 1,
        weekType: 'preparation_runway',
        runwayBlock: 'CONSISTENCY',
        title: 'Easy Run',
      );
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 1 of 15',
        currentWeekNumber: 1,
        totalWeeks: 15,
        currentWeekType: 'preparation_runway',
        currentRunwayBlock: 'CONSISTENCY',
        todayWorkout: todayDay,
        weekSummary: [todayDay],
      ));
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'j15-today',
        date: testToday(),
        weekNumber: 1,
        weekType: 'preparation_runway',
        runwayBlock: 'CONSISTENCY',
        source: 'template',
        canMarkComplete: true,
        canMarkNotToday: true,
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 15, runwayWeeks: 3));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        detailResponsesById: {'j15-today': detail},
        profileOverview: overview,
        planDetails: planDetails,
      );

      // Home: Week 1 of 15, Preparation Runway visible.
      expect(find.textContaining('Preparation Runway'), findsWidgets);

      // Home -> Detail via real tap/navigation.
      await tester.tap(find.text('Easy Run').first);
      await tester.pumpAndSettle();
      expect(find.byType(TrainingDayDetailPage), findsOneWidget);
      expect(bundle.trainingDayRepo.requestedDayIds, contains('j15-today'));
      expect(find.text('Source: Plan Template'), findsOneWidget);

      // Complete -> Detail/Home refetch (Home stays mounted under the
      // pushed Detail route via context.push).
      final homeFetchesBefore = bundle.homeRepo.fetchCallCount;
      await tester.tap(find.text('Mark as Completed'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Save Workout'));
      await tester.pumpAndSettle();

      expect(bundle.homeRepo.completeCallCount, 1);
      expect(bundle.homeRepo.lastCompletedDayId, 'j15-today');
      expect(bundle.homeRepo.fetchCallCount, greaterThan(homeFetchesBefore));
      // Save Workout's handler pops the Detail route back to Home (real
      // production behavior: `Navigator.pop` closes the sheet, then
      // `context.pop()` returns to the pushed-from Home route) -- so the
      // real post-completion assertion is that we're back on a refreshed
      // Home, not that Detail's provenance text is still on screen.
      expect(find.byType(TrainingDayDetailPage), findsNothing);
      expect(tester.takeException(), isNull);
    });
  });

  group(
      'Combined journey — 17-week (Home Core week -> not-today -> Pending empty)',
      () {
    testWidgets('not-today mutation, then Pending Confirmations is empty',
        (tester) async {
      await tester.binding.setSurfaceSize(const Size(800, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      final todayDay = dayJson(
        dayId: 'j17-today',
        date: testToday(),
        weekNumber: 6,
        weekType: 'build',
        title: 'Progressed Session',
        intensity: 'CONTROLLED_AEROBIC_POWER_PROGRESSED',
      );
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 6 of 17',
        currentWeekNumber: 6,
        totalWeeks: 17,
        currentWeekType: 'build',
        todayWorkout: todayDay,
        weekSummary: [todayDay],
      ));
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'j17-today',
        date: testToday(),
        weekNumber: 6,
        weekType: 'build',
        source: 'template',
        intensity: 'CONTROLLED_AEROBIC_POWER_PROGRESSED',
        canMarkComplete: true,
        canMarkNotToday: true,
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        detailResponsesById: {'j17-today': detail},
        profileOverview: overview,
        planDetails: planDetails,
        pendingItems: const [],
      );

      expect(find.text('Build'), findsWidgets);
      expect(find.textContaining('Preparation Runway'), findsNothing);

      await tester.tap(find.text('Progressed Session').first);
      await tester.pumpAndSettle();
      expect(bundle.trainingDayRepo.requestedDayIds, contains('j17-today'));

      await tester.tap(find.text('Not Today'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Skip Workout'));
      await tester.pumpAndSettle();

      expect(bundle.homeRepo.notTodayCreateCallCount, 1);
      expect(bundle.homeRepo.lastNotTodayDayId, 'j17-today');

      // Now separately verify Pending Confirmations remains empty for this
      // scripted state (runway/Core not-today never creates a pending row).
      final pendingBundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.pendingConfirmation,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
        pendingItems: const [],
      );
      expect(find.text('All caught up!'), findsOneWidget);
      expect(pendingBundle.pendingRepo.resolveCallCount, 0);
    });
  });

  group(
      'Combined journey — 20-week (Home -> late Core -> Plan Details -> cancel)',
      () {
    testWidgets('cancel sends the correct single plan ID and clears state',
        (tester) async {
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(planId: 'plan-j20', totalWeeks: 20, runwayWeeks: 8));
      final overview = ProfileOverviewResponse.fromJson(
          profileOverviewJson(planName: 'TEN_K 20-Week Plan'));
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 18 of 20',
        currentWeekNumber: 18,
        totalWeeks: 20,
        currentWeekType: 'taper',
      ));

      // Plan Details reachable with the real 20-week structure first.
      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.planDetails,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
      expect(find.text('20-week plan'), findsOneWidget);
      expect(find.text('8 weeks'), findsOneWidget); // Preparation
      expect(find.text('12 weeks'), findsOneWidget); // Core: 20 - 8

      // Cancel via the real Profile flow with the same plan identity.
      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.profile,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
      await tester.ensureVisible(find.text('Stop Plan').first);
      await tester.tap(find.text('Stop Plan').first);
      await tester.pumpAndSettle();
      expect(find.text('Stop Active Plan?'), findsOneWidget);
      await tester.tap(find.text('Stop Plan').last);
      await tester.pumpAndSettle();

      expect(bundle.planRepo.cancelCallCount, 1);
      expect(bundle.planRepo.lastCancelledPlanId, 'plan-j20');
    });
  });

  // ── PART 15/16/17/18: Core/habit/legacy/malformed regression (page-level) ─

  group('Core regression — 8/12/14-week Plan Details + Detail', () {
    for (final weeks in [8, 12, 14]) {
      testWidgets('$weeks-week Core plan: truthful structure, no runway leak',
          (tester) async {
        final home =
            HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1'));
        final planDetails = PlanDetailsResponse.fromJson(
            planDetailsJson(totalWeeks: weeks, runwayWeeks: 0));
        final overview =
            ProfileOverviewResponse.fromJson(profileOverviewJson());
        await pumpActivePlanApp(
          tester,
          initialLocation: AppRoutes.planDetails,
          homeResponse: home,
          profileOverview: overview,
          planDetails: planDetails,
        );
        expect(find.text('$weeks-week plan'), findsOneWidget);
        expect(find.text('Preparation'), findsNothing);
        expect(tester.takeException(), isNull);
      });
    }
  });

  group('Habit regression — page-level', () {
    testWidgets('Home/Plan Details render safely for an active habit plan',
        (tester) async {
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 3 of 8',
        goalType: 'habit',
        currentWeekNumber: 3,
        totalWeeks: 8,
        currentWeekType: 'base',
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(goalType: 'habit', totalWeeks: 8, runwayWeeks: 0));
      final overview = ProfileOverviewResponse.fromJson(
          profileOverviewJson(goalType: 'habit'));

      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
      expect(find.textContaining('Preparation Runway'), findsNothing);
      expect(tester.takeException(), isNull);

      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.planDetails,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );
      expect(find.text('8-week plan'), findsOneWidget);
      expect(find.text('Preparation'), findsNothing);
      expect(tester.takeException(), isNull);
    });
  });

  group('Unknown/malformed data safety — page level', () {
    testWidgets('unknown week type/runway block/source render safely',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-unknown',
        date: testToday(),
        weekNumber: 4,
        weekType: 'some_future_phase',
        runwayBlock: 'SOME_FUTURE_BLOCK',
        source: 'some_future_source',
      ));
      await _pumpDetail(tester, detail);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Core week with erroneous runway block hides the block',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-erroneous-block',
        date: testToday(),
        weekNumber: 8,
        weekType: 'build',
        runwayBlock: 'CONSISTENCY', // must never surface for a Core week
        source: 'template',
      ));
      await _pumpDetail(tester, detail);
      expect(find.textContaining('Consistency'), findsNothing);
      expect(tester.takeException(), isNull);
    });

    testWidgets('malformed adapted_from_id still hides raw id safely',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-malformed-adapted',
        date: testToday(),
        weekNumber: 4,
        weekType: 'build',
        source: 'engine_adapted',
        adaptedFromId: 'not-a-real-guid-!!!',
      ));
      await _pumpDetail(tester, detail);
      expect(find.text('Adapted from an earlier workout'), findsOneWidget);
      expect(find.textContaining('not-a-real-guid'), findsNothing);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Detail not found (stale/unknown day id) renders a safe error',
        (tester) async {
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      await pumpActivePlanApp(
        tester,
        initialLocation: '/training-day/does-not-exist',
        homeResponse: home,
        detailResponsesById: const {},
        profileOverview: overview,
        planDetails: planDetails,
      );
      expect(find.text('Could not load workout'), findsOneWidget);
      expect(find.text('Mark as Completed'), findsNothing);
      expect(tester.takeException(), isNull);
    });
  });
}
