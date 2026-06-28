import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_text_styles.dart';

class PlanDetailsPage extends ConsumerWidget {
  const PlanDetailsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
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
          onPressed: () => context.pop(),
        ),
        title: const Text(
          'Plan Summary',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.w700,
            color: AppColors.textPrimary,
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(
              Icons.more_vert_rounded,
              color: AppColors.textPrimary,
              size: 22,
            ),
            onPressed: () {},
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24),
        child: Column(
          children: [
            const SizedBox(height: 24),

            // Plan Hero Card
            const _PlanHeroCard(
              title: '21K Half Marathon Challenge',
              subtitle: 'Week 6 of 12',
              badges: ['Intermediate', 'Sub 1:59:00'],
            ),
            const SizedBox(height: 20),

            // Goal & Duration metrics row
            Row(
              children: const [
                Expanded(
                  child: _PlanMetricCard(
                    icon: Icons.emoji_events_outlined,
                    iconColor: Color(0xFFD97706),
                    iconBgColor: Color(0xFFFEF3C7),
                    label: 'GOAL TIME',
                    value: '1:59:00',
                  ),
                ),
                SizedBox(width: 12),
                Expanded(
                  child: _PlanMetricCard(
                    icon: Icons.calendar_today_outlined,
                    iconColor: Color(0xFF2563EB),
                    iconBgColor: Color(0xFFDBEAFE),
                    label: 'DURATION',
                    value: '12 Weeks',
                  ),
                ),
              ],
            ),
            const SizedBox(height: 20),

            // Active Progress Card
            const _ActiveProgressCard(
              runsCompleted: 24,
              runsTotal: 48,
              distanceKm: 155.5,
              activeWeeks: 6,
              progressPercent: 0.5,
              note: "You're showing great consistency! You've officially reached the halfway mark. Keep up that momentum.",
            ),
            const SizedBox(height: 20),

            // Plan Summary Card
            const _PlanSummaryInfoCard(),
            const SizedBox(height: 40),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Reusable sub-widgets for Plan Details / Plan Summary
// ─────────────────────────────────────────────────────────────────────────────

class _PlanHeroCard extends StatelessWidget {
  const _PlanHeroCard({
    required this.title,
    required this.subtitle,
    required this.badges,
  });
  final String title;
  final String subtitle;
  final List<String> badges;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(vertical: 28, horizontal: 24),
      decoration: BoxDecoration(
        color: const Color(0xFFF3E8FF), // Lavender/purple background
        borderRadius: BorderRadius.circular(28),
      ),
      child: Column(
        children: [
          // Center running icon badge
          Container(
            width: 48,
            height: 48,
            decoration: const BoxDecoration(
              color: Colors.white,
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.directions_run_rounded,
              color: Color(0xFF8B5CF6), // Purple accent
              size: 24,
            ),
          ),
          const SizedBox(height: 16),

          // Title
          Text(
            title,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 6),

          // Subtitle
          Text(
            subtitle,
            style: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w500,
              color: AppColors.textSecondary,
            ),
          ),
          const SizedBox(height: 16),

          // Badges Row
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: badges.map((b) => Container(
              margin: const EdgeInsets.symmetric(horizontal: 4),
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                color: Colors.white.withOpacity(0.8),
                borderRadius: BorderRadius.circular(100),
              ),
              child: Text(
                b,
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
            )).toList(),
          ),
        ],
      ),
    );
  }
}

class _PlanMetricCard extends StatelessWidget {
  const _PlanMetricCard({
    required this.icon,
    required this.iconColor,
    required this.iconBgColor,
    required this.label,
    required this.value,
  });
  final IconData icon;
  final Color iconColor;
  final Color iconBgColor;
  final String label;
  final String value;

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
          // Icon circle
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: iconBgColor,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: iconColor, size: 20),
          ),
          const SizedBox(height: 12),
          Text(
            label,
            style: const TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.w700,
              color: AppColors.textSecondary,
              letterSpacing: 0.5,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            value,
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
            ),
          ),
        ],
      ),
    );
  }
}

class _ActiveProgressCard extends StatelessWidget {
  const _ActiveProgressCard({
    required this.runsCompleted,
    required this.runsTotal,
    required this.distanceKm,
    required this.activeWeeks,
    required this.progressPercent,
    required this.note,
  });
  final int runsCompleted;
  final int runsTotal;
  final double distanceKm;
  final int activeWeeks;
  final double progressPercent;
  final String note;

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
          // Header Row
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Active Progress',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),
              Text(
                '${(progressPercent * 100).toStringAsFixed(0)}% Complete',
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),

          // Horizontal Progress Bar (Navy fill, gray track)
          ClipRRect(
            borderRadius: BorderRadius.circular(100),
            child: LinearProgressIndicator(
              value: progressPercent,
              minHeight: 8,
              backgroundColor: const Color(0xFFF3F4F6),
              color: AppColors.ctaDark,
            ),
          ),
          const SizedBox(height: 20),

          // Stats Row
          Row(
            children: [
              Expanded(
                child: Column(
                  children: [
                    Text(
                      '$runsCompleted / $runsTotal',
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 4),
                    const Text(
                      'RUNS',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textSecondary,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
              ),
              Container(width: 1, height: 32, color: AppColors.border),
              Expanded(
                child: Column(
                  children: [
                    Text(
                      '${distanceKm.toStringAsFixed(1)} km',
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 4),
                    const Text(
                      'DISTANCE',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textSecondary,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
              ),
              Container(width: 1, height: 32, color: AppColors.border),
              Expanded(
                child: Column(
                  children: [
                    Text(
                      '$activeWeeks Weeks',
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 4),
                    const Text(
                      'ACTIVE',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textSecondary,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),

          // Motivational Note Box
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: const Color(0xFFF9FAFB),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppColors.border, width: 1),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(
                  Icons.favorite_rounded,
                  color: Color(0xFFEF4444),
                  size: 16,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    note,
                    style: const TextStyle(
                      fontSize: 13,
                      height: 1.4,
                      fontWeight: FontWeight.w500,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _PlanSummaryInfoCard extends StatelessWidget {
  const _PlanSummaryInfoCard();

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
          const Text(
            'Plan Summary',
            style: TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 16),
          const _PlanSummaryRow(
            icon: Icons.repeat_rounded,
            iconColor: Color(0xFF6D28D9),
            iconBgColor: Color(0xFFF3E8FF),
            label: 'Training Frequency',
            value: '4 Days / Week',
          ),
          const Divider(height: 24, thickness: 1),
          const _PlanSummaryRow(
            icon: Icons.directions_run_rounded,
            iconColor: Color(0xFF1D4ED8),
            iconBgColor: Color(0xFFDBEAFE),
            label: 'Total Plan Volume',
            value: '48 Total Runs',
          ),
          const Divider(height: 24, thickness: 1),
          const _PlanSummaryRow(
            icon: Icons.calendar_today_rounded,
            iconColor: Color(0xFF047857),
            iconBgColor: Color(0xFFD1FAE5),
            label: 'Duration',
            value: '12 Weeks',
          ),
        ],
      ),
    );
  }
}

class _PlanSummaryRow extends StatelessWidget {
  const _PlanSummaryRow({
    required this.icon,
    required this.iconColor,
    required this.iconBgColor,
    required this.label,
    required this.value,
  });
  final IconData icon;
  final Color iconColor;
  final Color iconBgColor;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        // Small pastel icon circle
        Container(
          width: 32,
          height: 32,
          decoration: BoxDecoration(
            color: iconBgColor,
            shape: BoxShape.circle,
          ),
          child: Icon(icon, color: iconColor, size: 16),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Text(
            label,
            style: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w500,
              color: AppColors.textSecondary,
            ),
          ),
        ),
        Text(
          value,
          style: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w700,
            color: AppColors.textPrimary,
          ),
        ),
      ],
    );
  }
}
