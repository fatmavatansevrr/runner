# Phase 4H.2 — Preparation Runway Week-by-Week and Workout Preview UI

## 1. Executive result

`PREPARATION_RUNWAY_WEEK_AND_WORKOUT_PREVIEW_UI_IMPLEMENTED_WITH_TRUTHFUL_RUNWAY_CORE_SEGMENTATION_COMPLETE_WEEK_TYPE_SUPPORT_AND_FULL_FLUTTER_REGRESSION_CLOSURE`

`PlanPreviewPage` now renders the actual week-by-week, workout-by-workout schedule — the gap Phase 4H.1
explicitly disclosed and deferred. Every week in a response is rendered in backend order (no truncation, no
renumbering); Preparation Runway weeks show their real runway block; Core weeks show their real phase; the
Runway→Core boundary is rendered from typed week-type comparison, never week-number arithmetic; all seven
current `TrainingWeekType` values are explicitly modeled (no current backend value maps to `unknown`); Core
intensity tokens are preserved with real, meaningful labels rather than collapsed into one generic string;
Intro and Progressed AerobicStrength sessions remain visibly and structurally distinct; the confirm CTA's
lifecycle-gated behavior from 4H.1 is unchanged. The stale `widget_test.dart` smoke test was removed with an
explicit, investigated rationale (§5). Two real defects were found and fixed during this phase — one in
production code (duplicate widget keys crashing on a malformed duplicate-week-number response) and one in a
previous phase's test (a since-outdated "unknown" assertion) — both discovered by real, executing tests, not
assumed. Full Flutter suite: **119 passed, 0 failed, 0 skipped.**

## 2. Inherited Phase 4H.1 state

Unchanged and re-verified: safe parsing of `lifecycle`/`preparation_runway` week type/`runway_block`/runway
intensity tokens; unknown-value safety; legacy backward compatibility; lifecycle-only confirmability
derivation; fail-closed behavior for non-confirmable/unknown lifecycle; no confirm call for a non-confirmable
preview; response-driven 15/17/20-week summary counts; no 12-week hardcoding.

## 3. Existing preview-screen gap

Confirmed via re-reading `PlanPreviewPage` before this phase's edits: it rendered exactly 4 summary rows
(Goal/Duration/Weekly Structure/Level) and nothing else — no week, no workout, was ever visible. This phase
closes that gap directly.

## 4. Artifacts inspected

- `mobile/lib/core/network/dtos.dart`, `mobile/lib/core/models/preparation_runway.dart` (both full files,
  as left by 4H.1).
- `mobile/lib/features/onboarding/presentation/plan_preview_page.dart` (full file).
- `mobile/lib/core/widgets/app_button.dart` (`AppPrimaryButton`'s `onPressed: null` disable convention,
  reused unchanged).
- `mobile/lib/features/home/presentation/home_page.dart` — existing date (`DateFormat('EEE, d MMM')`) and
  distance (`toStringAsFixed(x == x.roundToDouble() ? 0 : 1)`) formatting conventions, reused verbatim rather
  than reinvented.
- Backend: `GeneratePreviewResponse.cs`/`PreviewWeekDto`/`PreviewDayDto` (re-confirmed field shapes),
  `TrainingWeekType.cs` (all 7 members), `TrainingDayType.cs` (confirmed `LongRun` is a first-class member —
  the actual source of long-run identity, since `PreviewDayDto` has no separate `is_long_run` flag),
  `CatalogSessionPrescriptionPlanner.cs` (`EffortFor` — the exact Core+runway intensity-token vocabulary),
  `V1TaperSharpenPrescriptionPolicy.cs` (the one additional taper-specific intensity token).
- `mobile/lib/core/routing/app_router.dart` — confirmed its `redirect` callback reads
  `FirebaseAuth.instance.currentUser` directly (informs §5's smoke-test decision).
- `mobile/test/preparation_runway_preview_test.dart`, `onboarding_confirm_cleanup_test.dart`,
  `generate_preview_request_contract_test.dart`, `widget_test.dart` (all read in full before any edit).

## 5. Full-suite baseline repair

**Option B: removed** `mobile/test/widget_test.dart` (`"Onboarding smoke test"`). Investigated repair first:
`AppRouter.router`'s `redirect` callback calls `FirebaseAuth.instance.currentUser` directly — pumping the
real `AntigravityApp` in a widget test would require real Firebase Auth test-mocking infrastructure that
does not exist anywhere else in this test suite (every other test file overrides Riverpod providers instead
of touching Firebase directly). Building that infrastructure from scratch is a materially different,
riskier scope than this phase's UI-focused mandate, and the test provided no product-specific value beyond
"the app doesn't crash on boot" — which the existing `onboarding_confirm_cleanup_test.dart` and the new
`preparation_runway_schedule_ui_test.dart` already cover far more meaningfully (they pump real feature pages
through real `ProviderScope`/`GoRouter` harnesses and assert real navigation/confirm outcomes). This is
documented here rather than silently deleted.

## 6. Complete week-type model

`PreviewWeekType` (in `preparation_runway.dart`) extended from 4H.1's partial 5-member set
(`base`/`build`/`taper`/`preparationRunway`/`unknown`) to the full 8-member set matching every current
`TrainingWeekType` value: `base`, `build`, `recovery`, `peak`, `taper`, `raceWeek`, `preparationRunway`,
`unknown`. `fromWire` now exact-matches all seven real wire values (`base`/`build`/`recovery`/`peak`/
`taper`/`race_week`/`preparation_runway`); only a genuinely future value now falls to `unknown`. Centralized
`label` getter: `base`→"Foundation", `build`→"Build", `recovery`→"Recovery", `peak`→"Peak", `taper`→"Taper",
`raceWeek`→"Race Week", `preparationRunway`→"Preparation Runway" (never used directly — the week card shows
"Preparation Runway" + the runway block instead), `unknown`→"Training Week".

## 7. Core intensity inventory

Read directly from `CatalogSessionPrescriptionPlanner.EffortFor` and `V1TaperSharpenPrescriptionPolicy.cs`
(not inferred from field names). The complete current vocabulary: `EASY`, `LONG_RUN_EASY_CONTROLLED` (shared
verbatim between a runway Long Run and a Core `LONG_RUN_STANDARD` session), `SURGE_AND_FLOAT` (fartlek),
`THRESHOLD_EFFORT` (threshold tempo), `GOAL_PACE` (goal-pace session), `EASY_BASELINE_SHARPENING_PENDING`
(pre-final taper week), `EASY_WITH_CONTROLLED_SHARPENING` (final taper week), plus the two runway-only
values from 4H.1 (`CONTROLLED_AEROBIC_POWER_INTRO`/`_PROGRESSED`).

## 8. Intensity fallback design

New `WorkoutIntensityCategory` enum (9 known members + `unknown`) plus a `WorkoutIntensityValue` wrapper
class carrying `{category, rawValue, label, isKnown}` — the preferred design from the phase prompt. Every
known token gets a real, meaningful label (e.g. "Threshold Effort", "Goal Pace", "Fartlek — Surge & Float").
A genuinely unrecognized future token is **not** collapsed into one generic string — `humanizeRawToken`
converts it to Title Case (e.g. `"SOME_FUTURE_QUALITY_SESSION"` → `"Some Future Quality Session"`),
preserving real backend meaning rather than discarding it. 4H.1's original `WorkoutIntensity` enum (runway-
only, Core-collapsed-to-unknown) was left in place unmodified — still used by 4H.1's own passing tests —
rather than rewritten, to avoid destabilizing already-verified code; the new UI reads `intensityDetail`
(→ `WorkoutIntensityValue`), not the old `intensityValue` getter.

## 9. Preview-page architecture

New file `mobile/lib/features/onboarding/presentation/widgets/plan_preview_schedule.dart`
(`PlanScheduleSection` + private `_SegmentOverview`/`_WeekCard`/`_WorkoutRow`), inserted into
`PlanPreviewPage`'s **existing** `SingleChildScrollView` — not a second/nested scrollable, per the phase's
own explicit prohibition. A plain `Column` of up to 20 week cards (each expandable to at most 4 workout
rows — 80 rows total in the worst case) is a modest, bounded widget count; §26 verifies this stays
performant without needing slivers or lazy builders.

## 10. Segment overview

`_SegmentOverview`, shown only when `response.isPreparationRunwayPlan`: two chips, "Preparation" and
"Race-Specific Core", each showing `response.runwayWeekCount`/`response.coreWeekCount` — both computed from
the actual parsed `weeks` collection (`GeneratePreviewResponse.runwayWeekCount`/`coreWeekCount`, from 4H.1),
never inferred as "always 12" from total duration. For a Core-only preview this widget is never built at
all — verified by test (`findsNothing` for "Preparation Runway"/"weeks preparation" text).

## 11. Week-card component

`_WeekCard` (private to `plan_preview_schedule.dart`): always-visible header (`Week {weekNumber}`, major
label, runway block secondary label when applicable, session count, expand/collapse chevron) plus, when
expanded, its workout rows. Keyed by `'${previewId}-week-$i-${weekNumber}'` — list **position**, not just
`weekNumber` (see §35 for why this exact key shape was chosen).

## 12. Workout-row component

`_WorkoutRow` (private to `plan_preview_schedule.dart`): date (`DateFormat('EEE, d MMM')`, matching
`home_page.dart`'s existing convention), workout type label, distance (`toStringAsFixed` with the existing
trailing-.0-removal convention, or "Distance not specified" for `<= 0`), duration (or "Duration not
specified" for `<= 0`), intensity label (`day.intensityDetail.label`), and a long-run indicator (icon +
bold text, never icon-only).

## 13. Runway block presentation

`_WeekCard._secondaryLabel`: for a runway week, `week.runwayBlockValue!.label` when `runwayBlock` is
present; `"Preparation Block"` (a safe, non-crashing fallback — never "Foundation") when `runwayBlock` is
`null` on a runway week (malformed/legacy data). Never shown at all for a Core week.

## 14. Core phase presentation

`_WeekCard._majorLabel` for a non-runway week is `week.weekTypeValue.label` — the real Core phase (e.g.
"Foundation" for `base`), read directly from the typed week type, never hardcoded and never confused with a
runway block.

## 15. Runway/Core boundary

`PlanScheduleSection` computes `isFirstCoreWeekAfterRunway` for each week by comparing its typed week type to
the **previous list element's** typed week type (`response.weeks[i].weekTypeValue != preparationRunway &&
response.weeks[i-1].weekTypeValue == preparationRunway`) — never week-number arithmetic. When true,
`_SegmentBoundaryDivider` ("RACE-SPECIFIC CORE BEGINS") renders immediately above that week's card. Verified
by test for a 17-week fixture: the divider renders exactly once, and the first Core week retains its true
global number (`"Week 6"`, not `"Week 1"`).

## 16. AerobicStrength Intro presentation

Week block label: "Aerobic Strength". Workout intensity label: "Controlled Aerobic Power — Intro"
(`WorkoutIntensityCategory.controlledAerobicPowerIntro`). Verified with a real 15-week (3 runway weeks)
Intro-only fixture: the Intro label appears exactly once, and "Progressed" never appears anywhere.

## 17. AerobicStrength Progressed presentation

Same block label; workout intensity label "Controlled Aerobic Power — Progressed". Verified with a real
17-week (5 runway weeks) Intro+Progressed fixture: both labels appear, each exactly once, and are textually
distinct (`intro.label != progressed.label`, verified at both the model and widget layer).

## 18. Date/distance/duration formatting

Reused the app's existing conventions exactly (§4) rather than introducing new formatting logic. No
fabricated numeric pace is ever shown — `PreviewDayDto` carries no pace field for a preview response at all
(pace only appears elsewhere, in confirmed-plan responses out of this phase's scope), so this requirement is
satisfied by construction, not by a special case.

## 19. Long-run presentation

**Source of truth, explicitly investigated rather than assumed**: `PreviewDayDto` has no `is_long_run`
field. `TrainingDayType` (backend enum) has a first-class `LongRun` member, serialized `"long_run"`. New
`PreviewDayType` enum (`dtos.dart`/`preparation_runway.dart`) mirrors this in full, and `PreviewDayDto.
isLongRun` reads `dayTypeValue == PreviewDayType.longRun` — the formal backend-guaranteed signal, never
inferred from slot position (slot index 4 is never assumed to be the long run). Rendered via both a
trending-up icon **and** bold "Long Run" text (never icon-only, per PART 19's accessibility requirement).

## 20. Expansion behavior

**Chosen: Option B** — the first week starts expanded, every other week starts collapsed
(`_expandedWeekNumbers = {1}` in `_PlanPreviewPageState`, keyed by week **number** so toggling survives
whatever backend order the weeks arrive in). Rationale: gives the user real, immediate visible content
(never an all-collapsed wall of headers) without eagerly laying out and rendering all 80 workout rows on
first frame. Deterministic (always week 1, never response-dependent). Toggling is a plain `Set<int>` mutation
via `setState` — no new animation/state-management architecture.

## 21. Lifecycle/confirm CTA preservation

Unchanged from 4H.1: `onPressed: state.isPreviewConfirmable ? () => _onConfirm(...) : null`. The CTA sits
in a fixed `Padding` below the `Expanded(SingleChildScrollView(...))`, not inside the scrollable region
itself, so adding the schedule list cannot push it out of reach — it was always reachable regardless of
scroll content length, verified by test for both a Core-confirmable and a runway-confirmable 17-week preview
(CTA tapped successfully, confirm invoked exactly once, navigation to Home occurred).

## 22. 8-14 Core regression

Three dedicated tests (8/12/14-week Core-only fixtures): exact final week number renders, no "Preparation
Runway" text anywhere, existing summary values unaffected. All pass.

## 23. 15-week rendering

3 runway weeks (Intro only, per the deterministic allocation table established in Phase 4G.6C.2A), 12 Core
weeks, total 15 — renders fully, `find.text('Week 15')` present, no exception.

## 24. 17-week rendering

5 runway weeks (Intro + Progressed), boundary divider present once, first Core week is "Week 6" — all
verified by test (§15/§17).

## 25. 20-week rendering

8 runway weeks, 12 Core weeks, total 20. `tester.scrollUntilVisible` reaches "Week 20"; it is tappable
(expandable) with zero exceptions afterward (`tester.takeException()` asserted `null`).

## 26. 80-session performance/scroll result

The 20-week test's `tester.takeException()` assertion (§25) is the direct proof this renders without a
`RenderFlex` overflow or unbounded-height exception at the maximum fixture size (20 weeks × 4 sessions).
Given the modest widget count (≤20 week cards, ≤80 conditionally-built workout rows only for expanded
weeks — collapsed weeks build zero workout-row widgets), no sliver/lazy-builder architecture was needed; this
was verified empirically, not assumed.

## 27. Unknown/malformed data handling

Verified by dedicated tests: unknown week type + unknown runway block + unknown day type + unknown intensity
combined in one fixture (renders with zero exceptions); empty `weeks` list (shows the documented
`"No schedule details are available for this preview."` message); a week with an empty `days` list (shows
`"No sessions for this week."`); zero/negative distance and duration (show "Distance not specified"/
"Duration not specified", never "0 km"); a runway week missing `runway_block` (shows "Preparation Block",
never "Foundation"); unknown lifecycle (full schedule still renders, CTA fails closed — verified via direct
`AppPrimaryButton.onPressed == null` assertion).

## 28. Accessibility

Each week header is wrapped in `Semantics(button: true, label: "Week N, <segment>[, <block>]", value:
"Expanded"/"Collapsed")` — verified by test that the semantics label contains both the week number and the
segment name, and that expansion state is exposed as a semantics value (not only the visual chevron icon).
Each workout row is wrapped in `Semantics(label: "<date>, <type>, <distance>, <duration>, <intensity>[,
Long run]")`. Long-run identity is conveyed via bold text ("Long Run") in addition to the icon — verified by
test that the text is present, not relying on icon-only detection.

## 29. Error/empty states

Zero-weeks and zero-sessions-in-a-week cases are both handled with the documented safe messages (§27); no
schedule row is ever invented. Existing confirm-error handling (`_onConfirm`'s `catch`/`SnackBar` path) is
unchanged and unaffected by this phase's additions.

## 30. Test fixtures

New `mobile/test/preparation_runway_schedule_ui_test.dart` builds real-shape fixtures via helpers
(`_coreWeek`, `_runwayWeek`, `previewJson`, `coreOnlyPreviewJson`) covering 8/12/14/15/16/17/18/19/20-week
horizons, all 4 runway blocks, all 7 Core week types (cycled), the full Core+runway intensity vocabulary,
and malformed/unknown variants (unknown enums, missing `runway_block`, zero distance/duration, empty weeks,
empty `days`, duplicate/out-of-order week numbers, unknown lifecycle).

## 31. Model tests

10 tests: `PreviewWeekType` — all 7 real values parse distinctly, labels non-empty, `preparationRunway`
never collapses to a Core label, unknown-safety; `WorkoutIntensityValue` — every currently-emitted Core
token has a real label, Intro/Progressed distinctness, unrecognized-value humanization, no fabricated
numeric pace for goal-pace; `PreviewDayType` — `long_run` drives `isLongRun`, non-long-run days don't,
unknown day types are safe.

## 32. Widget tests

29 widget tests across 8 groups: week-list rendering (6), workout-row rendering (3), AerobicStrength
Intro/Progressed (2), horizon matrix (9), malformed/unknown-data safety (4), CTA preservation (2),
accessibility (3) — all passing (see §33/§34).

## 33. Flutter analyze result

`flutter analyze`: **0 errors**, 152 total info/warning-level issues (up from the 146 pre-4H.1 baseline by
+6, all `prefer_const_constructors` info-level suggestions in this phase's own new code — the same lint
category already present 30+ times elsewhere in this codebase, not a new class of issue).

## 34. Full Flutter test result

`flutter test test/preparation_runway_schedule_ui_test.dart`: **39 passed, 0 failed, 0 skipped** (focused).
`flutter test` (full suite): **119 passed, 0 failed, 0 skipped.** Zero newly skipped tests were used to hide
any failure.

## 35. Production defects found

**One real production defect**, found by a real test (duplicate/out-of-order week numbers, §27) and fixed:
`_WeekCard`'s widget `Key` was originally `'${previewId}-week-${weekNumber}'`. A malformed backend response
with two weeks sharing the same `week_number` (explicitly required to be handled safely per PART 17) produced
two widgets with an identical key, which Flutter throws a hard `FlutterError` for ("Duplicate keys found").
Fixed by including the list **position** in the key (`'${previewId}-week-$i-${weekNumber}'`), which is
always unique regardless of any duplicate/out-of-order `week_number` value the backend might ever send.

## 36. Production fixes made

Exactly the one fix in §35, in `plan_preview_schedule.dart`. No other production code (backend or Flutter)
was changed to satisfy an assumption — every other requirement was met by construction using real, already-
investigated backend contract data.

## 37. Explicit non-implementation statement

Not implemented this phase, consistent with the phase's own Implementation Boundary: confirmed-plan
Home/Calendar/Training-Day UI (explicitly out of scope); 21-52 week UI; any new candidate/distance/level
activation; any localization/`.arb` infrastructure (still none exists in this project, per 4H.1's finding,
unchanged); any new state-management framework (plain `StatefulWidget`/`setState` + the existing Riverpod
`OnboardingState`, matching every other page in this app). The old `WorkoutIntensity` enum from 4H.1 was
deliberately left in place unmodified rather than merged into the new `WorkoutIntensityCategory` — a
disclosed, minimal-blast-radius choice (§8), not an oversight.

## 38. Residual frontend gaps

- No confirmed-plan (active) week/workout UI exists yet — this phase covers preview only, per its own scope.
- The old, narrower `WorkoutIntensity` enum (4H.1) and the new, complete `WorkoutIntensityCategory`/
  `WorkoutIntensityValue` (4H.2) now coexist in the same file; a future cleanup phase could retire the former
  once nothing depends on it, but doing so here risked destabilizing already-green 4H.1 tests for no
  behavioral gain.
- No performance profiling beyond exception-free rendering + successful scroll-to-Week-20 was performed
  (no frame-timing/jank measurement tooling was introduced, consistent with "do not perform speculative
  micro-optimization").

## 39. Final phase classification

`PREPARATION_RUNWAY_WEEK_AND_WORKOUT_PREVIEW_UI_IMPLEMENTED_WITH_TRUTHFUL_RUNWAY_CORE_SEGMENTATION_COMPLETE_WEEK_TYPE_SUPPORT_AND_FULL_FLUTTER_REGRESSION_CLOSURE`

Every week and every workout in a real backend response is now visible and accessible from `PlanPreviewPage`,
with no truncation and Week 20 reachable; the Runway/Core boundary and every runway block/Core phase label
is truthful and typed-detection-driven; all seven current backend week types are explicitly modeled (none
falls through to `unknown`); the full Core+runway intensity vocabulary is preserved with real labels, never
collapsed into one generic string; Intro and Progressed remain visibly and structurally distinct;
non-confirmable previews render their full schedule with zero confirm calls; the stale smoke test is
resolved (removed, with investigated rationale); `flutter analyze` reports zero errors; and the full Flutter
suite is 119/119 green with no skips used to hide anything.

## 40. Exact next phase

Confirmed-plan Home/Calendar/Training-Day UI for an active Preparation Runway plan — rendering the real
runway/Core week structure once a plan is actually confirmed and active, not merely previewed — is the
natural next phase, now that both the backend (through Phase 4G.6C.2A) and the preview-side Flutter
contract/model/UI layer (through this phase) are complete and verified.
