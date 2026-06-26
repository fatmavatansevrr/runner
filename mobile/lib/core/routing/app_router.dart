import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../features/auth/presentation/auth_welcome_page.dart';
import '../../features/auth/presentation/sign_up_page.dart';
import '../../features/auth/presentation/sign_in_page.dart';
import '../../features/onboarding/presentation/intro_carousel_page.dart';
import '../../features/onboarding/presentation/goal_selection_page.dart';
import '../../features/onboarding/presentation/race_details_page.dart';
import '../../features/onboarding/presentation/running_background_page.dart';
import '../../features/onboarding/presentation/habit_goal_page.dart';
import '../../features/onboarding/presentation/custom_goal_page.dart';
import '../../features/onboarding/presentation/custom_goal_with_time_page.dart';
import '../../features/onboarding/presentation/weekly_frequency_page.dart';
import '../../features/onboarding/presentation/running_days_selection_page.dart';
import '../../features/onboarding/presentation/long_run_day_preference_page.dart';
import '../../features/onboarding/presentation/start_date_selection_page.dart';
import '../../features/onboarding/presentation/goal_time_page.dart';
import '../../features/onboarding/presentation/preferred_run_duration_page.dart';
import '../../features/onboarding/presentation/plan_generation_page.dart';
import '../../features/onboarding/presentation/plan_preview_page.dart';
import '../../features/home/presentation/home_page.dart';
import '../../features/calendar/presentation/calendar_page.dart';
import '../../features/profile/presentation/profile_page.dart';
import '../../features/plan/presentation/plan_details_page.dart';
import '../../features/settings/presentation/settings_page.dart';
import '../../features/pending_confirmation/presentation/pending_confirmation_page.dart';
import '../../features/training_day/presentation/training_day_detail_page.dart';
import '../theme/app_colors.dart';

/// Route names — use these constants everywhere to avoid typos.
abstract final class AppRoutes {
  static const String welcome           = '/auth';
  static const String signUp            = '/auth/signup';
  static const String signIn            = '/auth/signin';
  static const String introCarousel     = '/intro';
  static const String goalSelection     = '/onboarding/goal';
  static const String raceDetails       = '/onboarding/race-details';
  static const String runningBackground = '/onboarding/background';
  static const String habitGoal         = '/onboarding/habit-goal';
  static const String customGoal        = '/onboarding/custom-goal';
  static const String customGoalWithTime = '/onboarding/custom-goal-time';
  static const String weeklyFrequency   = '/onboarding/frequency';
  static const String runningDays       = '/onboarding/days';
  static const String longRunDay        = '/onboarding/long-run-day';
  static const String startDate         = '/onboarding/start-date';
  static const String goalTime          = '/onboarding/goal-time';
  static const String preferredDuration = '/onboarding/preferred-duration';
  static const String planGeneration    = '/onboarding/generating';
  static const String planPreview       = '/onboarding/plan-preview';
  static const String home              = '/home';
  static const String calendar          = '/calendar';
  static const String profile           = '/profile';
  static const String planDetails       = '/profile/plan-details';
  static const String settings          = '/settings';
  static const String pendingConfirmation = '/pending-confirmation';
  static const String trainingDayDetail  = '/training-day/:dayId';
}

/// Central router configuration using go_router.
abstract final class AppRouter {
  static final GoRouter router = GoRouter(
    initialLocation: AppRoutes.welcome,
    debugLogDiagnostics: true,
    routes: [
      // ── Auth & Entry ──────────────────────────────────────────────────────
      GoRoute(
        path: AppRoutes.welcome,
        builder: (_, __) => const AuthWelcomePage(),
      ),
      GoRoute(
        path: AppRoutes.signUp,
        builder: (_, __) => const SignUpPage(),
      ),
      GoRoute(
        path: AppRoutes.signIn,
        builder: (_, __) => const SignInPage(),
      ),

      // ── Onboarding ───────────────────────────────────────────────────────
      GoRoute(
        path: AppRoutes.introCarousel,
        builder: (_, __) => const IntroCarouselPage(),
      ),
      GoRoute(
        path: AppRoutes.goalSelection,
        builder: (_, __) => const GoalSelectionPage(),
      ),
      GoRoute(
        path: AppRoutes.raceDetails,
        builder: (_, __) => const RaceDetailsPage(),
      ),
      GoRoute(
        path: AppRoutes.runningBackground,
        builder: (_, __) => const RunningBackgroundPage(),
      ),
      GoRoute(
        path: AppRoutes.habitGoal,
        builder: (_, __) => const HabitGoalPage(),
      ),
      GoRoute(
        path: AppRoutes.customGoal,
        builder: (_, __) => const CustomGoalPage(),
      ),
      GoRoute(
        path: AppRoutes.customGoalWithTime,
        builder: (_, __) => const CustomGoalWithTimePage(),
      ),
      GoRoute(
        path: AppRoutes.weeklyFrequency,
        builder: (_, __) => const WeeklyFrequencyPage(),
      ),
      GoRoute(
        path: AppRoutes.runningDays,
        builder: (_, __) => const RunningDaysSelectionPage(),
      ),
      GoRoute(
        path: AppRoutes.longRunDay,
        builder: (_, __) => const LongRunDayPreferencePage(),
      ),
      GoRoute(
        path: AppRoutes.startDate,
        builder: (_, __) => const StartDateSelectionPage(),
      ),
      GoRoute(
        path: AppRoutes.goalTime,
        builder: (_, __) => const GoalTimePage(),
      ),
      GoRoute(
        path: AppRoutes.preferredDuration,
        builder: (_, __) => const PreferredRunDurationPage(),
      ),
      GoRoute(
        path: AppRoutes.planGeneration,
        builder: (_, __) => const PlanGenerationPage(),
      ),
      GoRoute(
        path: AppRoutes.planPreview,
        builder: (_, __) => const PlanPreviewPage(),
      ),

      // ── Main app (shell with bottom nav) ─────────────────────────────────
      ShellRoute(
        builder: (context, state, child) => _MainShell(child: child),
        routes: [
          GoRoute(
            path: AppRoutes.home,
            builder: (_, __) => const HomePage(),
          ),
          GoRoute(
            path: AppRoutes.calendar,
            builder: (_, __) => const CalendarPage(),
          ),
          GoRoute(
            path: AppRoutes.profile,
            builder: (_, __) => const ProfilePage(),
          ),
        ],
      ),

      // ── Settings ─────────────────────────────────────────────────────────
      GoRoute(
        path: AppRoutes.settings,
        builder: (_, __) => const SettingsPage(),
      ),

      // ── Plan Details (Profile sub-route) ──────────────────────────────────
      GoRoute(
        path: AppRoutes.planDetails,
        builder: (_, __) => const PlanDetailsPage(),
      ),

      // ── Pending Confirmations ────────────────────────────────────────────
      GoRoute(
        path: AppRoutes.pendingConfirmation,
        builder: (_, __) => const PendingConfirmationPage(),
      ),

      // ── Training Day Detail ──────────────────────────────────────────────
      GoRoute(
        path: AppRoutes.trainingDayDetail,
        builder: (_, state) {
          final dayId = state.pathParameters['dayId'] ?? '';
          return TrainingDayDetailPage(dayId: dayId);
        },
      ),
    ],
  );
}

/// Shell widget providing the bottom navigation bar (Calendar | Home | Profile).
/// Matches the design reference: three tabs, home center with dark filled circle.
class _MainShell extends StatelessWidget {
  const _MainShell({required this.child});

  final Widget child;

  static final _tabs = [
    AppRoutes.calendar,
    AppRoutes.home,
    AppRoutes.profile,
  ];

  @override
  Widget build(BuildContext context) {
    final location = GoRouterState.of(context).matchedLocation;
    final currentIndex = _tabs.indexWhere((t) => location.startsWith(t));
    final index = currentIndex < 0 ? 1 : currentIndex;

    return Scaffold(
      body: child,
      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          boxShadow: [
            BoxShadow(color: Colors.black12, blurRadius: 10, offset: Offset(0, -2)),
          ],
        ),
        child: BottomNavigationBar(
          currentIndex: index,
          onTap: (i) => context.go(_tabs[i]),
          backgroundColor: AppColors.navBackground,
          selectedItemColor: AppColors.primary,
          unselectedItemColor: AppColors.navInactive,
          showSelectedLabels: true,
          showUnselectedLabels: true,
          type: BottomNavigationBarType.fixed,
          items: [
            const BottomNavigationBarItem(
              icon: Icon(Icons.calendar_month_outlined),
              activeIcon: Icon(Icons.calendar_month),
              label: 'Calendar',
            ),
            BottomNavigationBarItem(
              icon: Container(
                padding: const EdgeInsets.all(8),
                decoration: const BoxDecoration(
                  color: AppColors.ctaDark,
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.home_outlined, color: Colors.white, size: 28),
              ),
              activeIcon: Container(
                padding: const EdgeInsets.all(8),
                decoration: const BoxDecoration(
                  color: AppColors.primary,
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.home, color: Colors.white, size: 28),
              ),
              label: 'Home',
            ),
            const BottomNavigationBarItem(
              icon: Icon(Icons.person_outline),
              activeIcon: Icon(Icons.person),
              label: 'Profile',
            ),
          ],
        ),
      ),
    );
  }
}
