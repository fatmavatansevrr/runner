/// Running Background V2.1 — the canonical four-level running-experience
/// self-description collected at onboarding. Wire values (sent to and
/// received from the backend, see `GeneratePreviewRequest.toJson()`) are the
/// snake_case names below, matching the backend's
/// `RunningApp.Domain.Enums.RunningBackground` enum exactly.
///
/// These four values are the entire active contract. "new_to_running",
/// "used_to_run", and "running_regularly" are NOT current product options
/// and do not appear anywhere in this model — there is no legacy-alias
/// handling on the frontend at all (unlike the backend, which retains a
/// narrowly-scoped historical-compat reader for pre-existing persisted
/// server-side data; the client has no equivalent legacy local store to
/// remain compatible with).
///
/// Canonical machine values are explicit and stable — never derive one from
/// visible label/description text.
enum RunningBackground {
  beginner,
  intermediate,
  advanced,
  experienced;

  /// The exact wire value sent to / received from the backend.
  String get wireValue => switch (this) {
        RunningBackground.beginner => 'beginner',
        RunningBackground.intermediate => 'intermediate',
        RunningBackground.advanced => 'advanced',
        RunningBackground.experienced => 'experienced',
      };

  /// Approved user-facing label (§2 of the Running Background V2 spec).
  String get label => switch (this) {
        RunningBackground.beginner => 'Beginner',
        RunningBackground.intermediate => 'Intermediate',
        RunningBackground.advanced => 'Advanced',
        RunningBackground.experienced => 'Experienced',
      };

  /// Approved user-facing description (§2 of the Running Background V2 spec).
  String get description => switch (this) {
        RunningBackground.beginner =>
          "I'm new to running or getting back into it.",
        RunningBackground.intermediate =>
          'I run regularly and can comfortably complete 5–10 km.',
        RunningBackground.advanced =>
          'I train consistently and regularly run 10 km or more.',
        RunningBackground.experienced =>
          'I have a strong running base and regularly train for longer distances.',
      };

  /// Only [RunningBackground.beginner] skips the runner-background-details
  /// screen; every other level opens it.
  bool get skipsRunnerBackgroundDetails => this == RunningBackground.beginner;

  /// Parses a wire value. Accepts ONLY the four canonical values — throws
  /// [FormatException] for anything else, including the removed legacy
  /// aliases. Never silently reinterprets an unrelated value.
  static RunningBackground parse(String raw) {
    for (final value in RunningBackground.values) {
      if (value.wireValue == raw) return value;
    }
    throw FormatException('Unknown RunningBackground value: $raw');
  }

  /// Same as [parse], but returns null instead of throwing — for tolerant
  /// reads of local state that may be absent or corrupted.
  static RunningBackground? tryParse(String? raw) {
    if (raw == null) return null;
    try {
      return parse(raw);
    } on FormatException {
      return null;
    }
  }
}
