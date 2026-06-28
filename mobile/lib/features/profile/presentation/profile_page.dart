import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/app_shared_widgets.dart';
import '../../../core/routing/app_router.dart';
import '../data/profile_provider.dart';
import '../../plan/data/plan_repository.dart';
import '../../../core/network/bootstrap_provider.dart';
import '../../home/data/home_provider.dart';
import '../../calendar/data/calendar_provider.dart';

class ProfilePage extends ConsumerStatefulWidget {
  const ProfilePage({super.key});

  @override
  ConsumerState<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends ConsumerState<ProfilePage> {
  bool _isStopping = false;

  void _showCancelPlanDialog(BuildContext context, String planName) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Stop Active Plan?'),
        content: Text(
          'Are you sure you want to stop your $planName? Your current progress will be archived, and you can start a new plan anytime.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Keep Training'),
          ),
          TextButton(
            onPressed: () async {
              Navigator.pop(ctx);
              setState(() => _isStopping = true);
              try {
                final repo = ref.read(planRepositoryProvider);
                final activePlanAsync = ref.read(activePlanDetailsProvider);
                final planId = activePlanAsync.valueOrNull?.planId;
                if (planId != null) {
                  await repo.cancelPlan(planId, 'User stopped training plan manually.');
                } else {
                  throw Exception('Active plan ID not found.');
                }
                // Refresh all states
                ref.invalidate(profileOverviewProvider);
                ref.invalidate(activePlanDetailsProvider);
                ref.invalidate(homeDataProvider);
                ref.invalidate(calendarDataProvider);
                ref.invalidate(bootstrapDataProvider);
                if (mounted) {
                  context.go(AppRoutes.goalSelection);
                }
              } catch (e) {
                if (mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Failed to cancel plan: ${e.toString()}')),
                  );
                }
              } finally {
                if (mounted) {
                  setState(() => _isStopping = false);
                }
              }
            },
            style: TextButton.styleFrom(foregroundColor: AppColors.missed),
            child: const Text('Stop Plan'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white, // White background for the entire page
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0,
        centerTitle: true,
        leading: IconButton(
          icon: const Icon(
            Icons.arrow_back_ios_new_rounded,
            color: AppColors.textPrimary,
            size: 20,
          ),
          onPressed: () {
            if (context.canPop()) {
              context.pop();
            }
          },
        ),
        title: const Text(
          'Profile',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.w700,
            color: AppColors.textPrimary,
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(
              Icons.settings_outlined,
              color: AppColors.textPrimary,
              size: 22,
            ),
            onPressed: () => context.push(AppRoutes.settings),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24),
        child: Column(
          children: [
            const SizedBox(height: 24),

            // Profile Identity Section
            const _ProfileAvatar(initials: 'JD'),
            const SizedBox(height: 16),
            const Text(
              'John Doe',
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.w800,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              'Professional Member',
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w500,
                color: AppColors.textSecondary,
              ),
            ),
            const SizedBox(height: 24),

            // Stats row
            const _ProfileStatsRow(runs: '45', distanceKm: '187'),
            const SizedBox(height: 32),

            // Active Plan Section Title
            Align(
              alignment: Alignment.centerLeft,
              child: Text(
                'Active Plan',
                style: AppTextStyles.h3.copyWith(
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),
            ),
            const SizedBox(height: 12),

            // Active Plan Card
            _ActivePlanCard(
              planName: '21K Half Marathon',
              weekText: 'Week 6 of 12',
              progressPercent: 0.5,
              onViewPlan: () => context.push(AppRoutes.planDetails),
              onStopPlan: () => _showCancelPlanDialog(context, '21K Half Marathon'),
            ),
            const SizedBox(height: 32),

            // Recent Badges Title Row
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Recent Badges',
                  style: AppTextStyles.h3.copyWith(
                    fontWeight: FontWeight.w800,
                    color: AppColors.textPrimary,
                  ),
                ),
                GestureDetector(
                  onTap: () {}, // View All
                  child: const Text(
                    'View All',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),

            // Badge cards side by side
            Row(
              children: const [
                Expanded(
                  child: _BadgeCard(
                    icon: Icons.bolt_rounded,
                    iconColor: Color(0xFFD97706),
                    iconBgColor: Color(0xFFFEF3C7),
                    title: 'Fast Paced',
                    subtitle: 'Avg pace < 5:00',
                  ),
                ),
                SizedBox(width: 12),
                Expanded(
                  child: _BadgeCard(
                    icon: Icons.water_drop_rounded,
                    iconColor: Color(0xFF2563EB),
                    iconBgColor: Color(0xFFDBEAFE),
                    title: 'Rain Runner',
                    subtitle: 'Wet weather run',
                  ),
                ),
              ],
            ),
            const SizedBox(height: 40),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Reusable Sub-Widgets for Profile
// ─────────────────────────────────────────────────────────────────────────────

class _ProfileAvatar extends StatelessWidget {
  const _ProfileAvatar({required this.initials});
  final String initials;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        CircleAvatar(
          radius: 50,
          backgroundColor: const Color(0xFFEFF6FF), // light blue tint
          child: Text(
            initials,
            style: const TextStyle(
              fontSize: 32,
              fontWeight: FontWeight.w700,
              color: Color(0xFF1D4ED8), // blue accent
            ),
          ),
        ),
        Positioned(
          bottom: 0,
          right: 0,
          child: Container(
            width: 32,
            height: 32,
            decoration: BoxDecoration(
              color: Colors.white,
              shape: BoxShape.circle,
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.1),
                  blurRadius: 4,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            child: const Icon(
              Icons.edit_outlined,
              size: 16,
              color: AppColors.textPrimary,
            ),
          ),
        ),
      ],
    );
  }
}

class _ProfileStatsRow extends StatelessWidget {
  const _ProfileStatsRow({required this.runs, required this.distanceKm});
  final String runs;
  final String distanceKm;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Column(
          children: [
            Text(
              runs,
              style: const TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.w800,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              'RUNS',
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: AppColors.textSecondary,
                letterSpacing: 0.8,
              ),
            ),
          ],
        ),
        const SizedBox(width: 40),
        Container(
          width: 1,
          height: 36,
          color: AppColors.border,
        ),
        const SizedBox(width: 40),
        Column(
          children: [
            Text(
              distanceKm,
              style: const TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.w800,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              'KM',
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: AppColors.textSecondary,
                letterSpacing: 0.8,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _ActivePlanCard extends StatelessWidget {
  const _ActivePlanCard({
    required this.planName,
    required this.weekText,
    required this.progressPercent,
    required this.onViewPlan,
    required this.onStopPlan,
  });
  final String planName;
  final String weekText;
  final double progressPercent;
  final VoidCallback onViewPlan;
  final VoidCallback onStopPlan;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppColors.border, width: 1),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.03),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ON TRACK Badge
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: const Color(0xFFDCFCE7), // light green
              borderRadius: BorderRadius.circular(100),
            ),
            child: const Text(
              'ON TRACK',
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w700,
                color: Color(0xFF15803D), // dark green
                letterSpacing: 0.5,
              ),
            ),
          ),
          const SizedBox(height: 12),

          // Title, Subtitle and Flag Row
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      planName,
                      style: const TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      weekText,
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w500,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              // Flag icon container
              Container(
                width: 40,
                height: 40,
                decoration: const BoxDecoration(
                  color: Color(0xFFF3F4F6),
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.flag_rounded,
                  color: AppColors.textPrimary,
                  size: 20,
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),

          // Progress Header
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Progress',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textSecondary,
                ),
              ),
              Text(
                '${(progressPercent * 100).toStringAsFixed(0)}%',
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),

          // Progress Indicator
          ClipRRect(
            borderRadius: BorderRadius.circular(100),
            child: LinearProgressIndicator(
              value: progressPercent,
              minHeight: 8,
              backgroundColor: AppColors.border,
              color: AppColors.ctaDark,
            ),
          ),
          const SizedBox(height: 24),

          // Action Buttons
          SizedBox(
            width: double.infinity,
            height: 48,
            child: ElevatedButton(
              onPressed: onViewPlan,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.ctaDark,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(24),
                ),
                elevation: 0,
              ),
              child: const Text(
                'View Plan',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Center(
            child: TextButton(
              onPressed: onStopPlan,
              child: const Text(
                'Stop Plan',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: AppColors.missed,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _BadgeCard extends StatelessWidget {
  const _BadgeCard({
    required this.icon,
    required this.iconColor,
    required this.iconBgColor,
    required this.title,
    required this.subtitle,
  });
  final IconData icon;
  final Color iconColor;
  final Color iconBgColor;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.border, width: 1),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.02),
            blurRadius: 8,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Icon Container
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: iconBgColor,
              shape: BoxShape.circle,
            ),
            child: Icon(
              icon,
              color: iconColor,
              size: 20,
            ),
          ),
          const SizedBox(height: 16),
          Text(
            title,
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            subtitle,
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w500,
              color: AppColors.textSecondary,
            ),
          ),
        ],
      ),
    );
  }
}
