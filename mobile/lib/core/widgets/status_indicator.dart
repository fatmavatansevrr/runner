import 'package:flutter/material.dart';
import '../theme/app_colors.dart';
import '../theme/app_text_styles.dart';

class StatusIndicator extends StatelessWidget {
  const StatusIndicator({
    super.key,
    required this.status,
    this.showText = true,
  });

  final String status; // "planned" | "completed" | "missed" | "pending" | "skipped"
  final bool showText;

  @override
  Widget build(BuildContext context) {
    final (label, icon, color, bgColor) = _resolve(status);

    if (!showText) {
      return Container(
        padding: const EdgeInsets.all(6),
        decoration: BoxDecoration(
          color: bgColor,
          shape: BoxShape.circle,
          border: Border.all(color: color, width: 1),
        ),
        child: Icon(icon, size: 16, color: color),
      );
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(100),
        border: Border.all(color: color.withValues(alpha: 0.3), width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: color),
          const SizedBox(width: 4),
          Text(
            label,
            style: AppTextStyles.bodySmall.copyWith(
              fontWeight: FontWeight.w600,
              color: color,
            ),
          ),
        ],
      ),
    );
  }

  static (String, IconData, Color, Color) _resolve(String status) => switch (status) {
    'completed' => ('Completed', Icons.check_circle_rounded, AppColors.completed, AppColors.completedLight),
    'missed'    => ('Missed', Icons.error_rounded, AppColors.missed, AppColors.missedLight),
    'skipped'   => ('Skipped', Icons.skip_next_rounded, AppColors.textSecondary, AppColors.divider),
    'pending'   => ('Unconfirmed', Icons.help_outline_rounded, AppColors.primary, AppColors.primaryLight),
    _           => ('Planned', Icons.radio_button_unchecked_rounded, AppColors.textSecondary, Colors.transparent),
  };
}
