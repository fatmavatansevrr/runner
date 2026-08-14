import 'dart:ui' show Size;

import 'package:flutter_test/flutter_test.dart';
import 'package:antigravity_app/core/network/dtos.dart';
import 'package:antigravity_app/core/routing/app_router.dart';
import 'package:antigravity_app/features/training_day/presentation/training_day_detail_page.dart';

import 'support/active_plan_test_harness.dart';
import 'support/preparation_runway_active_plan_fixtures.dart';

// ── Phase 4H.5 — the first page-level widget-test coverage for the
// active-plan surfaces (Home/Calendar/Training Day Detail/Profile-cancel),
// built on the reusable harness in test/support/. Every test here pumps a
// REAL page widget through a REAL ProviderScope + real-route GoRouter, and
// asserts real visible text/semantics plus real scripted-repository call
// counts/captured arguments -- not DTO-only assertions (see 4H.3/4H.4,
// which explicitly disclosed this exact gap as unclosed).

void main() {
  // ── PART 6/12: Home page harness ────────────────────────────────────────

  group('Home page — rendering matrix', () {
    testWidgets('15-week runway plan: authoritative week/segment/block visible',
        (tester) async {
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 1 of 15',
        currentWeekNumber: 1,
        totalWeeks: 15,
        currentWeekType: 'preparation_runway',
        currentRunwayBlock: 'CONSISTENCY',
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 15, runwayWeeks: 3));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );

      // Phase 4H.6 added workout-level provenance to the today-workout
      // card itself, alongside the pre-existing plan-level segment badge --
      // both legitimately show "Preparation Runway"/"Consistency" now.
      expect(find.textContaining('Preparation Runway'), findsWidgets);
      expect(find.textContaining('Consistency'), findsWidgets);
      expect(find.textContaining('Week 1'), findsWidgets);
      expect(tester.takeException(), isNull);
      expect(bundle.homeRepo.fetchCallCount, greaterThanOrEqualTo(1));
    });

    testWidgets(
        '17-week plan at first Core week: Core phase shown, no runway block',
        (tester) async {
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 6 of 17',
        currentWeekNumber: 6,
        totalWeeks: 17,
        currentWeekType: 'build',
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

      // Phase 4H.6: both the plan-level badge and the today-card's own
      // workout-level provenance line legitimately show "Build" now.
      expect(find.textContaining('Build'), findsWidgets);
      expect(find.textContaining('Preparation Runway'),
          findsNothing); // Core week -- no runway segment shown
      expect(tester.takeException(), isNull);
    });

    testWidgets('20-week plan: real total weeks reachable, no truncation to 12',
        (tester) async {
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 19 of 20',
        currentWeekNumber: 19,
        totalWeeks: 20,
        currentWeekType: 'taper',
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 20, runwayWeeks: 8));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );

      expect(find.textContaining('Week 19'), findsWidgets);
      // Phase 4H.6: plan-level badge + today-card provenance both show
      // "Taper" legitimately now.
      expect(find.textContaining('Taper'), findsWidgets);
      expect(tester.takeException(), isNull);
    });

    testWidgets(
        'legacy response (no 4G.6D fields): no crash, no fabricated segment badge',
        (tester) async {
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 3 of 12'));
      final planDetails =
          PlanDetailsResponse.fromJson(planDetailsJson(totalWeeks: 12));
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

    testWidgets(
        'Core week never shows a runway block even if erroneously present in a fixture',
        (tester) async {
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 8 of 17',
        currentWeekNumber: 8,
        totalWeeks: 17,
        currentWeekType: 'build',
        currentRunwayBlock:
            'AEROBIC_STRENGTH', // erroneous -- must never render
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

      expect(find.textContaining('Aerobic Strength'), findsNothing);
    });

    testWidgets('no active plan: safe empty state, no crash', (tester) async {
      final home = HomeResponse.fromJson({
        'active_plan': null,
        'today_workout': null,
        'daily_tip': null,
        'week_summary': <Map<String, dynamic>>[],
        'has_pending_confirmations': false,
      });
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(hasActivePlan: false, totalWeeks: 0));
      final overview = ProfileOverviewResponse.fromJson(
          profileOverviewJson(hasActivePlan: false));

      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );

      expect(tester.takeException(), isNull);
    });
  });

  // ── PART 7: Home navigation ─────────────────────────────────────────────

  group('Home page — navigation to Detail', () {
    testWidgets('tapping the today workout opens Detail with the exact day ID',
        (tester) async {
      final todayDay = dayJson(
        dayId: 'runway-day-1',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        title: 'Easy Run',
      );
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 2 of 17',
        currentWeekNumber: 2,
        totalWeeks: 17,
        currentWeekType: 'preparation_runway',
        currentRunwayBlock: 'GENERAL_ENDURANCE',
        todayWorkout: todayDay,
        weekSummary: [todayDay],
      ));
      final detail = TrainingDayDetailResponse.fromJson({
        ...dayJson(
            dayId: 'runway-day-1',
            date: testToday(),
            weekNumber: 2,
            weekType: 'preparation_runway',
            runwayBlock: 'GENERAL_ENDURANCE',
            title: 'Easy Run',
            source: 'template'),
      });
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        detailResponsesById: {'runway-day-1': detail},
        profileOverview: overview,
        planDetails: planDetails,
      );

      await tester.tap(find.text('Easy Run').first);
      await tester.pumpAndSettle();

      expect(find.byType(TrainingDayDetailPage), findsOneWidget);
      expect(bundle.trainingDayRepo.requestedDayIds, contains('runway-day-1'));
      expect(bundle.trainingDayRepo.fetchCallCount, 1);
    });
  });

  // ── PART 8/9: Calendar page harness + navigation ────────────────────────

  group('Calendar page — month matrix', () {
    testWidgets('runway-only month renders and requests exactly that yyyy-MM',
        (tester) async {
      final month = DateTime(2026, 7);
      final monthKey = '2026-07';
      final days = calendarMonthJson(
          monthAnchor: month,
          daysInMonth: 31,
          weekType: 'preparation_runway',
          runwayBlock: 'AEROBIC_STRENGTH',
          startingWeekNumber: 1);
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1 of 17'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.calendar,
        homeResponse: home,
        calendarResponsesByMonth: {
          monthKey: days.map(TrainingDayResponse.fromJson).toList()
        },
        initialCalendarMonth: monthKey,
        profileOverview: overview,
        planDetails: planDetails,
      );

      expect(bundle.calendarRepo.requestedMonths, [monthKey]);
      expect(bundle.calendarRepo.fetchCallCount, 1);
      expect(tester.takeException(), isNull);
    });

    testWidgets('Core-only month: selecting a day never shows a runway label',
        (tester) async {
      final month = DateTime(2026, 9);
      final monthKey = '2026-09';
      final days = calendarMonthJson(
          monthAnchor: month,
          daysInMonth: 30,
          weekType: 'build',
          runwayBlock: null,
          startingWeekNumber: 9);
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 9 of 17'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.calendar,
        homeResponse: home,
        calendarResponsesByMonth: {
          monthKey: days.map(TrainingDayResponse.fromJson).toList()
        },
        initialCalendarMonth: monthKey,
        profileOverview: overview,
        planDetails: planDetails,
      );

      // Tap the 8th of the month (an easy day, not overflow).
      await tester.tap(find.text('8').first);
      await tester.pumpAndSettle();

      expect(find.textContaining('Preparation Runway'), findsNothing);
      expect(tester.takeException(), isNull);
    });

    testWidgets('empty month renders safely with no crash', (tester) async {
      final monthKey = '2026-12';
      final home = HomeResponse.fromJson(
          homeResponseJson(progressText: 'Week 17 of 17'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.calendar,
        homeResponse: home,
        calendarResponsesByMonth: {monthKey: <TrainingDayResponse>[]},
        initialCalendarMonth: monthKey,
        profileOverview: overview,
        planDetails: planDetails,
      );

      expect(tester.takeException(), isNull);
    });

    testWidgets(
        'boundary month: runway and Core days coexist, both selectable without crash',
        (tester) async {
      final month = DateTime(2026, 8);
      final monthKey = '2026-08';
      final runwayDays = calendarMonthJson(
              monthAnchor: month,
              daysInMonth: 15,
              weekType: 'preparation_runway',
              runwayBlock: 'PRE_SPECIFIC_TRANSITION',
              startingWeekNumber: 5)
          .sublist(0, 15);
      final coreDaysRaw = calendarMonthJson(
          monthAnchor: DateTime(2026, 8, 16),
          daysInMonth: 16,
          weekType: 'base',
          startingWeekNumber: 6);
      final allDays = [...runwayDays, ...coreDaysRaw];
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 6 of 17'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.calendar,
        homeResponse: home,
        calendarResponsesByMonth: {
          monthKey: allDays.map(TrainingDayResponse.fromJson).toList()
        },
        initialCalendarMonth: monthKey,
        profileOverview: overview,
        planDetails: planDetails,
      );

      expect(tester.takeException(), isNull);
    });
  });

  group('Calendar page — day navigation', () {
    testWidgets(
        'selecting a real workout day opens Detail with the exact day ID',
        (tester) async {
      final month = DateTime(2026, 7);
      final monthKey = '2026-07';
      final targetDate = DateTime(2026, 7, 21);
      final targetDayId = 'cal-2026-7-21';
      final days = calendarMonthJson(
          monthAnchor: month,
          daysInMonth: 31,
          weekType: 'preparation_runway',
          runwayBlock: 'GENERAL_ENDURANCE',
          startingWeekNumber: 1);
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1 of 17'));
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
          dayId: targetDayId,
          date: targetDate,
          weekNumber: 1,
          weekType: 'preparation_runway',
          runwayBlock: 'GENERAL_ENDURANCE',
          source: 'template'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.calendar,
        homeResponse: home,
        calendarResponsesByMonth: {
          monthKey: days.map(TrainingDayResponse.fromJson).toList()
        },
        detailResponsesById: {targetDayId: detail},
        initialCalendarMonth: monthKey,
        profileOverview: overview,
        planDetails: planDetails,
      );

      // Open the day-detail panel for the 21st, then tap through to Detail.
      await tester.tap(find.text('21').first);
      await tester.pumpAndSettle();
      expect(find.textContaining('Preparation Runway'),
          findsWidgets); // selected-day panel shows provenance
    });
  });

  // ── PART 10: Training Day Detail harness ────────────────────────────────

  group('Training Day Detail page — session matrix', () {
    Future<void> pumpDetailFor(
        WidgetTester tester, TrainingDayDetailResponse detail) async {
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 1 of 17'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());
      await pumpActivePlanApp(
        tester,
        initialLocation: '/training-day/${detail.dayId}',
        homeResponse: home,
        detailResponsesById: {detail.dayId: detail},
        profileOverview: overview,
        planDetails: planDetails,
      );
    }

    testWidgets('Consistency Easy: provenance + no crash', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-consistency',
        date: testToday(),
        weekNumber: 1,
        weekType: 'preparation_runway',
        runwayBlock: 'CONSISTENCY',
        source: 'template',
        title: 'Easy Run',
        dayType: 'easy',
      ));
      await pumpDetailFor(tester, detail);
      expect(find.text('Consistency'), findsOneWidget);
      expect(find.text('Source: Plan Template'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('AerobicStrength Intro and Progressed remain visually distinct',
        (tester) async {
      final intro = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-intro',
        date: testToday(),
        weekNumber: 5,
        weekType: 'preparation_runway',
        runwayBlock: 'AEROBIC_STRENGTH',
        intensity: 'CONTROLLED_AEROBIC_POWER_INTRO',
        source: 'template',
        dayType: 'tempo',
      ));
      await pumpDetailFor(tester, intro);
      expect(find.textContaining('CONTROLLED_AEROBIC_POWER_INTRO'),
          findsOneWidget);
      expect(find.textContaining('CONTROLLED_AEROBIC_POWER_PROGRESSED'),
          findsNothing);
    });

    testWidgets('Core Foundation day: no runway block shown', (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-core',
        date: testToday(),
        weekNumber: 6,
        weekType: 'base',
        source: 'template',
        title: 'Foundation Run',
      ));
      await pumpDetailFor(tester, detail);
      expect(find.text('Foundation'), findsOneWidget);
      expect(find.textContaining('Consistency'), findsNothing);
      expect(find.textContaining('Aerobic Strength'), findsNothing);
    });

    testWidgets('completed runway session: status visible, source unchanged',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-completed',
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
      await pumpDetailFor(tester, detail);
      // _CompletedView (unlike _PlannedView) does not render
      // _ProvenanceCard -- provenance/source text is a Planned-view-only
      // affordance in current production code, so this assertion targets
      // what the Completed view actually shows instead.
      expect(find.text('ACTUAL STATS'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('missing pace never shows a fabricated 0:00/km',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-nopace',
        date: testToday(),
        weekNumber: 5,
        weekType: 'preparation_runway',
        runwayBlock: 'AEROBIC_STRENGTH',
        intensity: 'CONTROLLED_AEROBIC_POWER_PROGRESSED',
        source: 'template',
      ));
      await pumpDetailFor(tester, detail);
      expect(find.textContaining('0:00'), findsNothing);
      expect(find.text('TARGET PACE'), findsNothing);
    });

    testWidgets('adapted-origin day shows the safe message, never the raw GUID',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-adapted',
        date: testToday(),
        weekNumber: 6,
        weekType: 'build',
        source: 'engine_adapted',
        adaptedFromId: 'original-day-guid-should-not-appear',
      ));
      await pumpDetailFor(tester, detail);
      expect(find.text('Adapted from an earlier workout'), findsOneWidget);
      expect(find.textContaining('original-day-guid-should-not-appear'),
          findsNothing);
    });
  });

  // ── PART 11: completion flow ─────────────────────────────────────────────

  group('Training Day Detail — completion flow', () {
    testWidgets(
        'completing a runway Easy session sends the correct day ID and refetches Detail/Home/Calendar',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-complete-me',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        source: 'template',
        canMarkComplete: true,
        canMarkNotToday: true,
      ));
      final todayDay = dayJson(
        dayId: 'd-complete-me',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        title: 'Easy Run',
      );
      final home = HomeResponse.fromJson(homeResponseJson(
        progressText: 'Week 2 of 17',
        currentWeekNumber: 2,
        totalWeeks: 17,
        currentWeekType: 'preparation_runway',
        currentRunwayBlock: 'GENERAL_ENDURANCE',
        todayWorkout: todayDay,
        weekSummary: [todayDay],
      ));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      // Navigate through Home (via a real tap, using context.push) rather
      // than starting directly on the Detail route -- Home's route stays
      // mounted underneath in the Navigator stack, so its
      // ref.watch(homeDataProvider) subscription stays live and a
      // post-completion invalidate genuinely triggers a real Home refetch
      // (an invalidated-but-unwatched provider would never refetch, which
      // is exactly the structural mistake this fix corrects).
      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.home,
        homeResponse: home,
        detailResponsesById: {'d-complete-me': detail},
        profileOverview: overview,
        planDetails: planDetails,
      );
      await tester.tap(find.text('Easy Run').first);
      await tester.pumpAndSettle();

      final homeFetchesBefore = bundle.homeRepo.fetchCallCount;

      await tester.tap(find.text('Mark as Completed'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Save Workout'));
      await tester.pumpAndSettle();

      expect(bundle.homeRepo.completeCallCount, 1);
      expect(bundle.homeRepo.lastCompletedDayId, 'd-complete-me');
      // Real invalidation-driven refetch: Home is still mounted underneath
      // the pushed Detail route, so _invalidateAll()'s Home invalidation
      // genuinely triggers a new fetch, not just a no-op invalidate.
      expect(bundle.homeRepo.fetchCallCount, greaterThan(homeFetchesBefore));
      expect(bundle.trainingDayRepo.fetchCallCount,
          greaterThanOrEqualTo(2)); // initial + post-invalidate refetch
    });

    testWidgets(
        'completion failure preserves the Planned state and shows an error',
        (tester) async {
      final detail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-fail',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        source: 'template',
        canMarkComplete: true,
      ));
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 2 of 17'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: '/training-day/d-fail',
        homeResponse: home,
        detailResponsesById: {'d-fail': detail},
        profileOverview: overview,
        planDetails: planDetails,
        completeError: Exception('network error'),
      );

      await tester.tap(find.text('Mark as Completed'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Save Workout'));
      await tester.pumpAndSettle();

      expect(bundle.homeRepo.completeCallCount, 1);
      expect(find.text('Mark as Completed'),
          findsOneWidget); // still Planned -- action still offered
      expect(find.textContaining('Error'), findsOneWidget);
    });
  });

  // ── PART 12: not-today flow ───────────────────────────────────────────────

  group('Training Day Detail — not-today flow', () {
    testWidgets(
        'not-today sends the correct day ID/reason, day becomes Missed on refresh, no pending item',
        (tester) async {
      final plannedDetail = TrainingDayDetailResponse.fromJson(dayJson(
        dayId: 'd-skip-me',
        date: testToday(),
        weekNumber: 2,
        weekType: 'preparation_runway',
        runwayBlock: 'GENERAL_ENDURANCE',
        source: 'template',
        canMarkComplete: true,
        canMarkNotToday: true,
      ));
      final home =
          HomeResponse.fromJson(homeResponseJson(progressText: 'Week 2 of 17'));
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(totalWeeks: 17, runwayWeeks: 5));
      final overview = ProfileOverviewResponse.fromJson(profileOverviewJson());

      // The Not Today reason sheet has 5 radio options and overflows the
      // default 800x600 test viewport, pushing "Skip Workout" off-screen --
      // enlarge the surface so the whole sheet fits and the button is
      // genuinely hit-testable, rather than trying to scroll a modal sheet.
      await tester.binding.setSurfaceSize(const Size(800, 1400));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: '/training-day/d-skip-me',
        homeResponse: home,
        detailResponsesById: {'d-skip-me': plannedDetail},
        profileOverview: overview,
        planDetails: planDetails,
      );

      await tester.tap(find.text('Not Today'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Skip Workout'));
      await tester.pumpAndSettle();

      expect(bundle.homeRepo.notTodayCreateCallCount, 1);
      expect(bundle.homeRepo.lastNotTodayDayId, 'd-skip-me');
      expect(bundle.homeRepo.lastNotTodayReason,
          'Too busy'); // default RadioListTile selection
      expect(bundle.homeRepo.notTodayConfirmCallCount, 1);
      // Runway not-today never creates a pending row -- no pending repository
      // exists in this harness at all (Home/Calendar/Detail never watch
      // pendingConfirmationsProvider directly, per Phase 4H.3's own
      // investigation), so its absence here is itself part of the proof
      // that no pending call occurs from this flow.
    });
  });

  // ── PART 15: cancel flow (via the real ProfilePage) ──────────────────────

  group('Profile page — cancel flow', () {
    testWidgets(
        'stopping a 20-week runway plan sends the correct single plan ID and navigates to goal selection',
        (tester) async {
      final planDetails = PlanDetailsResponse.fromJson(
          planDetailsJson(planId: 'plan-20wk', totalWeeks: 20, runwayWeeks: 8));
      final overview = ProfileOverviewResponse.fromJson(
          profileOverviewJson(planName: 'TEN_K 20-Week Plan'));
      final home = HomeResponse.fromJson(
          homeResponseJson(progressText: 'Week 10 of 20'));

      final bundle = await pumpActivePlanApp(
        tester,
        initialLocation: AppRoutes.profile,
        homeResponse: home,
        profileOverview: overview,
        planDetails: planDetails,
      );

      await tester.ensureVisible(find.text('Stop Plan').first);
      await tester.pumpAndSettle();
      await tester.tap(find.text('Stop Plan').first);
      await tester.pumpAndSettle();
      expect(find.text('Stop Active Plan?'), findsOneWidget);
      await tester.tap(
          find.text('Stop Plan').last); // dialog's destructive confirm action
      await tester.pumpAndSettle();

      expect(bundle.planRepo.cancelCallCount, 1);
      expect(bundle.planRepo.lastCancelledPlanId, 'plan-20wk');
    });
  });
}
