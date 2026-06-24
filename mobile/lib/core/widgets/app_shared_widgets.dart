import 'package:flutter/material.dart';
import '../theme/app_colors.dart';
import '../theme/app_text_styles.dart';
import '../theme/app_spacing.dart';

/// Empty state component — used when there is no active plan, no calendar data, etc.
class EmptyState extends StatelessWidget {
  const EmptyState({
    super.key,
    required this.title,
    required this.message,
    this.action,
    this.actionLabel,
    this.illustration,
  });

  final String title;
  final String message;
  final VoidCallback? action;
  final String? actionLabel;
  final Widget? illustration;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (illustration != null) ...[
              illustration!,
              const SizedBox(height: AppSpacing.lg),
            ],
            Text(
              title,
              style: AppTextStyles.h2,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              message,
              style: AppTextStyles.bodyMedium,
              textAlign: TextAlign.center,
            ),
            if (action != null && actionLabel != null) ...[
              const SizedBox(height: AppSpacing.lg),
              FilledButton(
                onPressed: action,
                child: Text(actionLabel!),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Loading state — centered progress indicator with optional message.
class LoadingState extends StatelessWidget {
  const LoadingState({super.key, this.message});

  final String? message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const CircularProgressIndicator(
            color: AppColors.primary,
            strokeWidth: 2.5,
          ),
          if (message != null) ...[
            const SizedBox(height: AppSpacing.md),
            Text(message!, style: AppTextStyles.bodyMedium),
          ],
        ],
      ),
    );
  }
}

/// Progress dots used in the intro carousel (matching screenshot 02).
class AppProgressDots extends StatelessWidget {
  const AppProgressDots({
    super.key,
    required this.count,
    required this.currentIndex,
  });

  final int count;
  final int currentIndex;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: List.generate(count, (i) {
        final isActive = i == currentIndex;
        return AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          margin: const EdgeInsets.symmetric(horizontal: 4),
          width: isActive ? 20 : 8,
          height: 8,
          decoration: BoxDecoration(
            color: isActive ? AppColors.primary : AppColors.border,
            borderRadius: BorderRadius.circular(4),
          ),
        );
      }),
    );
  }
}

/// Workout type badge chip (Easy Run, Long Run, etc.) — color-coded.
class WorkoutTypeBadge extends StatelessWidget {
  const WorkoutTypeBadge({super.key, required this.type});

  final String type; // "easy" | "interval" | "long_run" | "rest" | "tempo"

  @override
  Widget build(BuildContext context) {
    final (label, color) = _resolve(type);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(100),
      ),
      child: Text(
        label,
        style: AppTextStyles.bodySmall.copyWith(
          fontWeight: FontWeight.w600,
          color: AppColors.textPrimary,
        ),
      ),
    );
  }

  static (String, Color) _resolve(String type) => switch (type) {
    'easy'           => ('Easy Run', AppColors.easyRunTint),
    'interval'       => ('Interval', AppColors.intervalTint),
    'long_run'       => ('Long Run', AppColors.longRunTint),
    'tempo'          => ('Tempo', AppColors.intervalTint),
    'rest'           => ('Rest Day', AppColors.restTint),
    'recovery_easy'  => ('Recovery', AppColors.easyRunTint),
    _                => (type, AppColors.border),
  };
}
