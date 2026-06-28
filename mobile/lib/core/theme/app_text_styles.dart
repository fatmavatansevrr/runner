import 'package:flutter/material.dart';
import 'app_colors.dart';

/// Typography tokens using the Inter font family (matches design references).
/// Inter is declared in pubspec.yaml; falls back to system sans-serif.
abstract final class AppTextStyles {
  // ── Display ───────────────────────────────────────────────────────────────
  /// Large workout distance/type heading (e.g. "5 km" on Home card)
  static const TextStyle displayLarge = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 48,
    fontWeight: FontWeight.w700, // Bold
    color: AppColors.textPrimary,
    height: 1.1,
    letterSpacing: -1.0,
  );

  // ── Headings ──────────────────────────────────────────────────────────────
  static const TextStyle h1 = TextStyle(
    fontFamily: 'ClashGrotesk',
    fontSize: 28,
    fontWeight: FontWeight.w700, // Bold
    color: AppColors.textPrimary,
    height: 1.2,
    letterSpacing: -0.5,
  );

  static const TextStyle h2 = TextStyle(
    fontFamily: 'ClashGrotesk',
    fontSize: 22,
    fontWeight: FontWeight.w700, // Bold
    color: AppColors.textPrimary,
    height: 1.3,
  );

  static const TextStyle h3 = TextStyle(
    fontFamily: 'ClashGrotesk',
    fontSize: 18,
    fontWeight: FontWeight.w700, // Bold
    color: AppColors.textPrimary,
    height: 1.4,
  );

  // ── Body ──────────────────────────────────────────────────────────────────
  static const TextStyle bodyLarge = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 16,
    fontWeight: FontWeight.w400, // Regular
    color: AppColors.textPrimary,
    height: 1.5,
  );

  static const TextStyle bodyMedium = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 14,
    fontWeight: FontWeight.w400, // Regular
    color: AppColors.textSecondary,
    height: 1.5,
  );

  static const TextStyle bodySmall = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 12,
    fontWeight: FontWeight.w400, // Regular
    color: AppColors.textMuted,
    height: 1.4,
  );

  // ── Label / Caption ───────────────────────────────────────────────────────
  /// Uppercase micro-label (e.g. "TODAY'S PLAN", "WEEK 6", "THIS WEEK")
  static const TextStyle label = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 11,
    fontWeight: FontWeight.w500, // Medium
    color: AppColors.textSecondary,
    height: 1.2,
    letterSpacing: 0.8,
  );

  static const TextStyle labelPrimary = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 11,
    fontWeight: FontWeight.w500, // Medium
    color: AppColors.primary,
    height: 1.2,
    letterSpacing: 0.8,
  );

  // ── Button ────────────────────────────────────────────────────────────────
  static const TextStyle buttonPrimary = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 16,
    fontWeight: FontWeight.w500, // Medium
    color: AppColors.textOnDark,
    letterSpacing: 0.1,
  );

  static const TextStyle buttonSecondary = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 16,
    fontWeight: FontWeight.w500, // Medium
    color: AppColors.textPrimary,
    letterSpacing: 0.1,
  );

  // ── Calendar ──────────────────────────────────────────────────────────────
  static const TextStyle calendarDayNumber = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 14,
    fontWeight: FontWeight.w700, // Bold
    color: AppColors.textPrimary,
  );

  static const TextStyle calendarDayLabel = TextStyle(
    fontFamily: 'GeneralSans',
    fontSize: 11,
    fontWeight: FontWeight.w400, // Regular
    color: AppColors.textSecondary,
  );
}
