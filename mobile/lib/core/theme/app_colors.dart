import 'package:flutter/material.dart';

/// Color tokens extracted from the Antigravity design references.
/// Do NOT invent new colors — all values are derived from the PNG screenshots.
abstract final class AppColors {
  // ── Brand / Accent ─────────────────────────────────────────────────────────
  /// Primary blue: CTAs, selected borders, active state (e.g. selected card border)
  static const Color primary = Color(0xFF2B5BFF);

  /// Primary blue light tint: progress bars, active day chip background
  static const Color primaryLight = Color(0xFFD6E0FF);

  // ── Semantic status ────────────────────────────────────────────────────────
  /// Completed workout indicator (green checkmark, Completed button)
  static const Color completed = Color(0xFF34C759);
  static const Color completedLight = Color(0xFFDDFAE6);

  /// Missed / Not Completed indicator — muted, not aggressive
  static const Color missed = Color(0xFFFF3B30);
  static const Color missedLight = Color(0xFFFFE5E3);

  // ── Workout type tints (from calendar cell backgrounds) ───────────────────
  /// Easy run — soft sky blue
  static const Color easyRunTint = Color(0xFFDCEEFD);

  /// Interval — soft pink/salmon
  static const Color intervalTint = Color(0xFFFFD6D6);

  /// Long run — soft lavender/purple
  static const Color longRunTint = Color(0xFFE8DFFD);

  /// Rest / recovery — warm cream/yellow
  static const Color restTint = Color(0xFFFFF8DC);

  // ── Neutral backgrounds ────────────────────────────────────────────────────
  /// App scaffold background
  static const Color background = Color(0xFFF5F6FA);

  /// Card surface
  static const Color surface = Color(0xFFFFFFFF);

  /// Today's plan card background (light blue tint from home screen)
  static const Color todayCardBackground = Color(0xFFEBF2FF);

  // ── Text ──────────────────────────────────────────────────────────────────
  static const Color textPrimary = Color(0xFF0D1B2A);
  static const Color textSecondary = Color(0xFF6B7280);
  static const Color textMuted = Color(0xFFADB5BD);
  static const Color textOnDark = Color(0xFFFFFFFF);

  // ── Surface / border ──────────────────────────────────────────────────────
  static const Color border = Color(0xFFE5E7EB);
  static const Color divider = Color(0xFFF3F4F6);

  // ── CTA / Button ──────────────────────────────────────────────────────────
  /// Dark pill CTA (seen on Plan Preview, Goal Selection, Auth — near-black)
  static const Color ctaDark = Color(0xFF0D1B2A);

  /// Secondary button background (white card with border)
  static const Color ctaSecondaryBackground = Color(0xFFFFFFFF);

  // ── Bottom navigation ─────────────────────────────────────────────────────
  static const Color navBackground = Color(0xFFFFFFFF);
  static const Color navActive = Color(0xFF0D1B2A);
  static const Color navInactive = Color(0xFFADB5BD);

  // ── Insight cards (home bottom widgets) ──────────────────────────────────
  static const Color weeklyCardBackground = Color(0xFFFFFBE6); // warm yellow
  static const Color tipCardBackground = Color(0xFFF0EBFF);    // light lavender

  // ── Today's plan card — workout-type backgrounds ──────────────────────────
  /// Easy run card: soft sky blue
  static const Color easyRunCard = Color(0xFFEBF2FF);
  /// Long run card: soft mint green
  static const Color longRunCard = Color(0xFFEDFBF0);
  /// Interval card: soft lavender
  static const Color intervalCard = Color(0xFFF3E8FF);
  /// Rest day card: warm cream
  static const Color restCard = Color(0xFFFFFBEA);
  /// Completed card: dark teal (white text)
  static const Color completedCard = Color(0xFF1A3A2A);
  /// Missed card: muted near-white
  static const Color missedCard = Color(0xFFF5F5F7);

  // ── Plan Generation screen ────────────────────────────────────────────────
  /// Dark navy background for plan generation loading screen
  static const Color navyBackground = Color(0xFF0A0F1C);
  /// Step icon for completed steps (green check)
  static const Color stepCompleted = Color(0xFF34C759);
}
