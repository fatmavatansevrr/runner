// Phase 4H.1 — typed, safe wrappers around the new backend preview fields
// introduced by the TEN_K 15-20 week Preparation Runway backend phases
// (`lifecycle`, `week_type=preparation_runway`, `runway_block`, and the
// runway effort-only intensity tokens). Every enum here has an explicit
// `unknown` member and a `fromWire` parser that never throws — a future
// backend value must degrade safely, never crash JSON parsing or the UI.
//
// Centralizes wire-string <-> typed-value <-> display-label conversion in
// one place so no widget re-implements its own switch over the raw backend
// string (see PART 4 of PHASE4H_1_...md).

/// Mirrors backend `PreviewLifecycleClassification`
/// (`RunningApp.Application.DTOs.Plan.GeneratePreviewResponse.cs`),
/// serialized snake_case_lower by the backend's global JSON convention.
/// The backend field is authoritative — this is never derived from week
/// count, `runway_block` presence, preview ID, or goal distance.
enum PreviewLifecycle {
  coreConfirmable,
  preparationRunwayPreviewConfirmable,
  preparationRunwayPreviewNotConfirmable,

  /// Any value this client doesn't recognize (a future backend addition,
  /// or a truly missing/malformed field with no safe legacy fallback).
  /// Fails closed: never confirmable.
  unknown;

  /// `wire == null` maps to [coreConfirmable] — the approved backward-
  /// compatible fallback for legacy fixtures/responses recorded before this
  /// field existed (every real 8-14 week backend response already sets this
  /// explicitly today, so `null` only occurs for a hand-built legacy test
  /// fixture, never a real current backend call).
  static PreviewLifecycle fromWire(String? wire) {
    switch (wire) {
      case null:
        return PreviewLifecycle.coreConfirmable;
      case 'core_confirmable':
        return PreviewLifecycle.coreConfirmable;
      case 'preparation_runway_preview_confirmable':
        return PreviewLifecycle.preparationRunwayPreviewConfirmable;
      case 'preparation_runway_preview_not_confirmable':
        return PreviewLifecycle.preparationRunwayPreviewNotConfirmable;
      default:
        return PreviewLifecycle.unknown;
    }
  }

  /// The sole authority for whether the confirm CTA may be enabled and the
  /// confirm request may ever be sent. [unknown] is deliberately excluded
  /// (fails closed).
  bool get isConfirmable =>
      this == PreviewLifecycle.coreConfirmable ||
      this == PreviewLifecycle.preparationRunwayPreviewConfirmable;

  bool get isPreparationRunway =>
      this == PreviewLifecycle.preparationRunwayPreviewConfirmable ||
      this == PreviewLifecycle.preparationRunwayPreviewNotConfirmable;
}

/// Mirrors backend `TrainingWeekType` (`RunningApp.Domain.Enums`) in full,
/// serialized snake_case_lower. Phase 4H.2 extends 4H.1's partial modeling
/// (which only had `base`/`build`/`taper`/`preparationRunway`/`unknown`) to
/// cover every current backend member explicitly (`recovery`/`peak`/
/// `raceWeek`) so no currently-emitted Core week type falls through to
/// [unknown].
enum PreviewWeekType {
  base,
  build,
  recovery,
  peak,
  taper,
  raceWeek,
  preparationRunway,

  /// Any value this client doesn't recognize — a genuinely future backend
  /// addition, not any of the seven current `TrainingWeekType` members.
  unknown;

  static PreviewWeekType fromWire(String? wire) {
    switch (wire) {
      case 'base':
        return PreviewWeekType.base;
      case 'build':
        return PreviewWeekType.build;
      case 'recovery':
        return PreviewWeekType.recovery;
      case 'peak':
        return PreviewWeekType.peak;
      case 'taper':
        return PreviewWeekType.taper;
      case 'race_week':
        return PreviewWeekType.raceWeek;
      case 'preparation_runway':
        return PreviewWeekType.preparationRunway;
      default:
        return PreviewWeekType.unknown;
    }
  }

  /// Display label for a Core week type. Never used for
  /// [preparationRunway] itself (that week's card shows "Preparation
  /// Runway" + its [PreparationRunwayBlock] label instead — see
  /// `_WeekCard` in `plan_preview_schedule.dart`), and never used to relabel
  /// a runway week as a Core phase.
  String get label {
    switch (this) {
      case PreviewWeekType.base:
        return 'Foundation';
      case PreviewWeekType.build:
        return 'Build';
      case PreviewWeekType.recovery:
        return 'Recovery';
      case PreviewWeekType.peak:
        return 'Peak';
      case PreviewWeekType.taper:
        return 'Taper';
      case PreviewWeekType.raceWeek:
        return 'Race Week';
      case PreviewWeekType.preparationRunway:
        return 'Preparation Runway';
      case PreviewWeekType.unknown:
        return 'Training Week';
    }
  }
}

/// Mirrors the backend's honest Preparation Runway block name
/// (`PreviewWeekDto.RunwayBlock`, always null for Core weeks). Never mapped
/// to a Core phase label (Foundation/Build/Race Specific/Taper) — a runway
/// week is structurally distinct and must never be presented as one.
enum PreparationRunwayBlock {
  consistency,
  generalEndurance,
  aerobicStrength,
  preSpecificTransition,
  unknown;

  static PreparationRunwayBlock fromWire(String? wire) {
    switch (wire) {
      case 'CONSISTENCY':
        return PreparationRunwayBlock.consistency;
      case 'GENERAL_ENDURANCE':
        return PreparationRunwayBlock.generalEndurance;
      case 'AEROBIC_STRENGTH':
        return PreparationRunwayBlock.aerobicStrength;
      case 'PRE_SPECIFIC_TRANSITION':
        return PreparationRunwayBlock.preSpecificTransition;
      default:
        return PreparationRunwayBlock.unknown;
    }
  }

  /// English display label. No app-wide localization infrastructure exists
  /// yet in this project (no `.arb`/`flutter_localizations` setup — only the
  /// `intl` package is used, for date/number formatting) — this mirrors the
  /// existing display-label convention already used elsewhere in this
  /// feature (`PlanPreviewPage._goalLabel`/`_levelLabel`: a static switch,
  /// not a full l10n pipeline). Disclosed explicitly in the phase document
  /// rather than inventing a new localization system for this one feature.
  String get label {
    switch (this) {
      case PreparationRunwayBlock.consistency:
        return 'Consistency';
      case PreparationRunwayBlock.generalEndurance:
        return 'General Endurance';
      case PreparationRunwayBlock.aerobicStrength:
        return 'Aerobic Strength';
      case PreparationRunwayBlock.preSpecificTransition:
        return 'Pre-Specific Transition';
      case PreparationRunwayBlock.unknown:
        return 'Preparation';
    }
  }
}

/// Mirrors the runway effort-only intensity tokens
/// (`PreviewDayDto.Intensity`/`TrainingDay.Intensity` for a runway-sourced
/// session). Core sessions' intensity tokens are not individually modeled
/// here — they are not currently displayed anywhere in this app (the
/// preview screen shows only the plan summary, not per-day intensity), so
/// every real Core value safely falls back to [unknown] with a generic
/// label rather than crashing; this does not change any existing Core
/// rendering because none exists yet to change.
enum WorkoutIntensity {
  easy,
  longRunEasyControlled,
  controlledAerobicPowerIntro,
  controlledAerobicPowerProgressed,
  unknown;

  static WorkoutIntensity fromWire(String? wire) {
    switch (wire) {
      case 'EASY':
        return WorkoutIntensity.easy;
      case 'LONG_RUN_EASY_CONTROLLED':
        return WorkoutIntensity.longRunEasyControlled;
      case 'CONTROLLED_AEROBIC_POWER_INTRO':
        return WorkoutIntensity.controlledAerobicPowerIntro;
      case 'CONTROLLED_AEROBIC_POWER_PROGRESSED':
        return WorkoutIntensity.controlledAerobicPowerProgressed;
      default:
        return WorkoutIntensity.unknown;
    }
  }

  String get label {
    switch (this) {
      case WorkoutIntensity.easy:
        return 'Easy';
      case WorkoutIntensity.longRunEasyControlled:
        return 'Controlled Easy Long Run';
      case WorkoutIntensity.controlledAerobicPowerIntro:
        return 'Controlled Aerobic Power — Intro';
      case WorkoutIntensity.controlledAerobicPowerProgressed:
        return 'Controlled Aerobic Power — Progressed';
      case WorkoutIntensity.unknown:
        return 'Training Effort';
    }
  }
}

/// Phase 4H.2 — the full, current effort/intensity vocabulary this app can
/// actually receive, built from a direct read of the backend's session
/// prescription planner (`CatalogSessionPrescriptionPlanner.EffortFor`,
/// `V1TaperSharpenPrescriptionPolicy.cs`) rather than inferred from field
/// names. Unlike [WorkoutIntensity] (4H.1, runway-only, Core values all
/// collapsed to a shared `unknown`), this covers every value a Core OR
/// runway session's `intensity` field can currently carry.
enum WorkoutIntensityCategory {
  easy,

  /// Shared verbatim between a runway Long Run and a Core `LONG_RUN_STANDARD`
  /// session — the backend deliberately reuses the same effort-only token
  /// for both (`CatalogSessionPrescriptionPlanner.EffortFor`).
  longRunEasyControlled,

  /// Fartlek ("surge and float" -- `FARTLEK` workout).
  surgeAndFloat,

  /// Threshold tempo (`THRESHOLD_TEMPO` workout).
  thresholdEffort,

  /// Goal-pace session (`GOAL_PACE_TEN_K` workout) — a real numeric pace may
  /// also be present on the day (not fabricated here; see
  /// `PreviewDayDto` — this category only labels the effort string itself).
  goalPace,

  /// Taper-phase easy running before the final sharpening polish
  /// (`ProgressionStageKey == "TAPER_SHARPEN"`, non-final week).
  easyBaselineSharpeningPending,

  /// The final taper-week sharpening session
  /// (`V1TaperSharpenPrescriptionPolicy`).
  easyWithControlledSharpening,

  controlledAerobicPowerIntro,
  controlledAerobicPowerProgressed,

  /// A raw value this client doesn't recognize (a genuinely future backend
  /// addition). The raw wire value is still preserved and safely humanized
  /// for display — see [WorkoutIntensityValue.label] — never collapsed to a
  /// single generic string that would discard real backend meaning.
  unknown;

  static WorkoutIntensityCategory _fromWire(String wire) {
    switch (wire) {
      case 'EASY':
        return WorkoutIntensityCategory.easy;
      case 'LONG_RUN_EASY_CONTROLLED':
        return WorkoutIntensityCategory.longRunEasyControlled;
      case 'SURGE_AND_FLOAT':
        return WorkoutIntensityCategory.surgeAndFloat;
      case 'THRESHOLD_EFFORT':
        return WorkoutIntensityCategory.thresholdEffort;
      case 'GOAL_PACE':
        return WorkoutIntensityCategory.goalPace;
      case 'EASY_BASELINE_SHARPENING_PENDING':
        return WorkoutIntensityCategory.easyBaselineSharpeningPending;
      case 'EASY_WITH_CONTROLLED_SHARPENING':
        return WorkoutIntensityCategory.easyWithControlledSharpening;
      case 'CONTROLLED_AEROBIC_POWER_INTRO':
        return WorkoutIntensityCategory.controlledAerobicPowerIntro;
      case 'CONTROLLED_AEROBIC_POWER_PROGRESSED':
        return WorkoutIntensityCategory.controlledAerobicPowerProgressed;
      default:
        return WorkoutIntensityCategory.unknown;
    }
  }
}

/// Wraps a parsed [WorkoutIntensityCategory] together with the original raw
/// wire value, per PART 3's preferred design — so a genuinely unknown future
/// intensity token still surfaces its real (humanized) text instead of a
/// single generic label that would silently discard backend meaning that
/// was actually supplied.
class WorkoutIntensityValue {
  const WorkoutIntensityValue({
    required this.category,
    required this.rawValue,
  });

  final WorkoutIntensityCategory category;
  final String rawValue;

  bool get isKnown => category != WorkoutIntensityCategory.unknown;

  factory WorkoutIntensityValue.fromWire(String? wire) {
    final raw = wire ?? '';
    return WorkoutIntensityValue(
        category: WorkoutIntensityCategory._fromWire(raw), rawValue: raw);
  }

  /// Never a fabricated numeric pace — always a textual effort label, even
  /// for [WorkoutIntensityCategory.goalPace] (the numeric value, when
  /// present, lives on `PreviewDayDto` itself, read directly, never derived
  /// from this label).
  String get label {
    switch (category) {
      case WorkoutIntensityCategory.easy:
        return 'Easy';
      case WorkoutIntensityCategory.longRunEasyControlled:
        return 'Controlled Easy Long Run';
      case WorkoutIntensityCategory.surgeAndFloat:
        return 'Fartlek — Surge & Float';
      case WorkoutIntensityCategory.thresholdEffort:
        return 'Threshold Effort';
      case WorkoutIntensityCategory.goalPace:
        return 'Goal Pace';
      case WorkoutIntensityCategory.easyBaselineSharpeningPending:
        return 'Easy';
      case WorkoutIntensityCategory.easyWithControlledSharpening:
        return 'Easy with Controlled Sharpening';
      case WorkoutIntensityCategory.controlledAerobicPowerIntro:
        return 'Controlled Aerobic Power — Intro';
      case WorkoutIntensityCategory.controlledAerobicPowerProgressed:
        return 'Controlled Aerobic Power — Progressed';
      case WorkoutIntensityCategory.unknown:
        return rawValue.isEmpty
            ? 'Training Effort'
            : humanizeRawToken(rawValue);
    }
  }
}

/// Mirrors backend `TrainingDayType` (`RunningApp.Domain.Enums`), serialized
/// snake_case_lower. `longRun` is the authoritative, backend-formal source
/// of long-run identity for a preview day — `PreviewDayDto` carries no
/// separate `is_long_run` flag, so this (not slot position) is what
/// `_WorkoutRow` reads (see PART 9 of PHASE4H_2_...md).
enum PreviewDayType {
  easy,
  interval,
  tempo,
  longRun,
  rest,
  recoveryEasy,
  unknown;

  static PreviewDayType fromWire(String? wire) {
    switch (wire) {
      case 'easy':
        return PreviewDayType.easy;
      case 'interval':
        return PreviewDayType.interval;
      case 'tempo':
        return PreviewDayType.tempo;
      case 'long_run':
        return PreviewDayType.longRun;
      case 'rest':
        return PreviewDayType.rest;
      case 'recovery_easy':
        return PreviewDayType.recoveryEasy;
      default:
        return PreviewDayType.unknown;
    }
  }

  String get label {
    switch (this) {
      case PreviewDayType.easy:
        return 'Easy Run';
      case PreviewDayType.interval:
        return 'Interval Session';
      case PreviewDayType.tempo:
        return 'Tempo Session';
      case PreviewDayType.longRun:
        return 'Long Run';
      case PreviewDayType.rest:
        return 'Rest Day';
      case PreviewDayType.recoveryEasy:
        return 'Recovery Run';
      case PreviewDayType.unknown:
        return 'Training Session';
    }
  }
}

/// Humanizes a raw SCREAMING_SNAKE_CASE (or snake_case) backend token that
/// this client doesn't otherwise recognize into Title Case for safe display
/// (e.g. `"SOME_FUTURE_TOKEN"` -> `"Some Future Token"`) — used only as the
/// last-resort fallback for a genuinely unknown enum-like wire value, never
/// for a value this file already has a real label for.
String humanizeRawToken(String raw) {
  final words = raw.split(RegExp(r'[_\s]+')).where((w) => w.isNotEmpty);
  return words
      .map((w) => w.length <= 1
          ? w.toUpperCase()
          : '${w[0].toUpperCase()}${w.substring(1).toLowerCase()}')
      .join(' ');
}

/// Phase 4H.4 — mirrors backend `TrainingDaySource` (`RunningApp.Domain.Enums`),
/// serialized snake_case_lower. `null` on the wire (the entity's `Source` is
/// unset) is distinct from an unrecognized non-null value — both are
/// represented as [unknown] here (this app has no UI distinction between
/// "not set" and "not recognized" for this field), but `fromWire(null)`
/// never throws either way.
enum TrainingDaySourceValue {
  template,
  userOverride,
  engineAdapted,
  engineRecovered,
  unknown;

  static TrainingDaySourceValue fromWire(String? wire) {
    switch (wire) {
      case 'template':
        return TrainingDaySourceValue.template;
      case 'user_override':
        return TrainingDaySourceValue.userOverride;
      case 'engine_adapted':
        return TrainingDaySourceValue.engineAdapted;
      case 'engine_recovered':
        return TrainingDaySourceValue.engineRecovered;
      default:
        return TrainingDaySourceValue.unknown;
    }
  }

  String get label {
    switch (this) {
      case TrainingDaySourceValue.template:
        return 'Plan Template';
      case TrainingDaySourceValue.userOverride:
        return 'User Adjusted';
      case TrainingDaySourceValue.engineAdapted:
        return 'Adapted';
      case TrainingDaySourceValue.engineRecovered:
        return 'Recovered';
      case TrainingDaySourceValue.unknown:
        return 'Unknown Source';
    }
  }
}

/// Phase 4H.4 — shared active-plan/preview provenance presentation helpers
/// (PART 3). Authoritative precedence: [weekType] alone determines the
/// segment; [runwayBlockRaw] is read as a secondary label ONLY when
/// [weekType] is [PreviewWeekType.preparationRunway] — a Core response that
/// erroneously carries a non-null runway_block (PART 25) is deliberately
/// never trusted or displayed, precisely because this helper gates on
/// weekType first, not on runwayBlock's mere presence.
class WeekProvenance {
  const WeekProvenance({required this.weekType, required this.runwayBlockRaw});

  final PreviewWeekType weekType;
  final String? runwayBlockRaw;

  bool get isPreparationRunwayWeek =>
      weekType == PreviewWeekType.preparationRunway;

  String get weekTypeLabel => weekType.label;

  /// `null` for a Core week even if [runwayBlockRaw] is (erroneously) set —
  /// never trusted unless [weekType] itself says this is a runway week.
  String? get runwayBlockLabel {
    if (!isPreparationRunwayWeek) return null;
    if (runwayBlockRaw == null) return 'Preparation Block';
    return PreparationRunwayBlock.fromWire(runwayBlockRaw).label;
  }

  /// Combined "Preparation Runway / General Endurance" or plain "Foundation"
  /// depending on segment.
  String get provenanceLabel => runwayBlockLabel == null
      ? weekTypeLabel
      : '$weekTypeLabel · $runwayBlockLabel';
}
