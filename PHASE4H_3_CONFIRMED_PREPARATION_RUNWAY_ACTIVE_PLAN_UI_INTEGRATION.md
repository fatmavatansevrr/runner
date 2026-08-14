# Phase 4H.3 — Confirmed Preparation Runway Active-Plan UI Integration

## 1. Executive result

`PREPARATION_RUNWAY_CONFIRMED_ACTIVE_PLAN_UI_INTEGRATED_ACROSS_HOME_CALENDAR_DETAIL_ACTIONS_AND_CANCEL_WITH_GLOBAL_WEEK_NUMBERING_AND_FULL_FLUTTER_REGRESSION_CLOSURE`

The mandatory active-plan contract audit (Part 2, done first, before any code change) found **two real,
confirmed backend contract gaps** that bound what this phase can honestly claim: no active-plan endpoint
(Home, Calendar, or Training Day Detail) exposes a `runway_block` field at all, and Training Day Detail
exposes no `Source`/`AdaptedFrom` fields. Only `Plan Details` (`PlanDetailsResponse`/`PlanWeekDetailDto`)
exposes a real, authoritative, per-week `TrainingWeekType` (including `PreparationRunway`) and the real
total week count — making it the sole endpoint that can truthfully render a Preparation Runway/Core
distinction anywhere in the active-plan UI, per this phase's own escape valve ("return the blocked
classification unless another authoritative endpoint already supplies it"). Given that authoritative
endpoint exists, the phase proceeds as A, not B, scoped precisely to what the real contracts support.

The single largest concrete finding: `PlanDetailsPage` was **100% disconnected static mock content** —
every value (`"12 Weeks"`, `"Week 6 of 12"`, `48 Total Runs`, etc.) was a hardcoded literal, never reading
`activePlanDetailsProvider` at all, for *any* plan length, runway or Core. This phase rewires it completely
to real data. Home, Calendar, and Training Day Detail were investigated via full source read and found to
already handle unrecognized `day_type`/`intensity`/`status` values safely (switch statements with default
fallbacks) and to carry no hardcoded week-count assumption — verified, not assumed, and therefore **not
rewritten**, consistent with the phase's own "any production fix must be exposed by a real test" boundary.
Full Flutter suite: **131 passed, 0 failed, 0 skipped** (up from 119 pre-phase).

## 2. Inherited backend and preview state

Unchanged and re-verified: backend confirmation/persistence/transaction-safety for 15-20 week runway plans
(4G.6C-4G.6C.2A); Flutter preview contract, model, and week-by-week/workout UI (4H.1-4H.2).

## 3. Exact active-plan scope

Home, Calendar, Training Day Detail, Plan Details, completion, not-today, pending-confirmation, and cancel
UI for a confirmed 15-20 week TEN_K/4-day/Intermediate runway plan, plus 8-14 week Core and habit-plan
regression. No backend changes, no plan-generation logic, no preview-to-active state copying.

## 4. Artifacts inspected

A dedicated research pass (via a read-only exploration agent, since these are large files — `home_page.dart`
is 2439 lines) read in full: `home_page.dart`/`home_provider.dart`, `calendar_page.dart`/
`calendar_provider.dart`, `training_day_detail_page.dart`, `plan_details_page.dart`, the cancel flow
(`profile_page.dart._showCancelPlanDialog`), and the pending-confirmation page/provider — reporting exact
field usage, every `switch`/mapping site and its fallback behavior, and existing test coverage. Backend:
`HomeResponse.cs` (`ActivePlanSummaryDto`, `TrainingDayResponse`), `PlanDetailsResponse.cs`
(`PlanWeekDetailDto`, `PlanDayDetailDto`), `TrainingDayDetailResponse.cs`, and
`QueryAndMutationServices.GetHomeAsync`'s exact `progress_text` construction (`$"Week {currentWeekNumber} of
{totalWeeks}"` — confirmed literally, not inferred).

## 5. Active-plan contract audit

| Field | Home | Calendar (`TrainingDayResponse`) | Training Day Detail | Plan Details (`PlanWeekDetailDto`/`PlanDayDetailDto`) |
|---|---|---|---|---|
| Global week number | B (embedded in `progress_text` string only) | C | C | A (`WeekNumber`, contiguous 1..N) |
| Total week count | B (embedded in `progress_text` only) | C | C | A (`TotalWeeks`) |
| Week type (Core phase / PreparationRunway) | C | C | C | A (`WeekType`) |
| Runway block | **C — not exposed anywhere** | **C** | **C** | **C** |
| Workout type | A | A | A | A |
| Intensity | A (present, unused by UI) | A (present, unused by UI) | A (rendered) | A |
| Planned distance/duration | A | A | A | A |
| Planned pace | A (nullable, omitted when absent) | A | A (nullable, omitted when absent) | A |
| Long-run identity | A (`is_long_run`, authoritative) | A | A | A |
| Date | A | A | A | A |
| Status | A | A | A | A |
| Source | **C — not exposed** | C | **C** | C |
| AdaptedFrom | **C — not exposed** | C | **C** | C |

**Conclusion, per the phase's own contract-gap protocol**: `runway_block` and `Source`/`AdaptedFrom` are
genuine, confirmed absences across every active-plan endpoint — not inferred from week count, not assumed.
Since `Plan Details` is a real, already-authoritative endpoint that *does* expose `WeekType` (the
runway-vs-Core distinction, one level coarser than the block), this phase is not fully blocked — it proceeds
with Plan Details as the one truthful segment-composition surface, and documents everywhere else that a
block/Source/AdaptedFrom-level distinction simply cannot be rendered today without a backend contract
change (out of this phase's Implementation Boundary).

## 6. Shared typed-model strategy

No second model system was created. `PreviewWeekType`, `PreparationRunwayBlock`, `WorkoutIntensityValue`,
and `PreviewDayType` (all from `preparation_runway.dart`, established in 4H.1/4H.2) are reused directly:
`PlanWeekDetailDto.weekTypeValue` (new getter, `PreviewWeekType.fromWire`), and
`TrainingDayResponse`/`TrainingDayDetailResponse`/`PlanDayDetailDto.intensityDetail`/`dayTypeValue` (new
getters, `WorkoutIntensityValue.fromWire`/`PreviewDayType.fromWire`) — the exact same parsers and `unknown`
fail-safe behavior already verified by 4H.2's tests, applied to the active-plan contracts rather than
duplicated. The old narrow `WorkoutIntensity` enum was not touched (unused by these new getters).

## 7. Confirmation-to-Home transition

Not modified this phase — investigated and confirmed unchanged from the existing, already-correct
`plan_preview_page.dart._onConfirm` flow (confirm sends `previewId` only; `onboardingProvider.notifier.reset()`
clears onboarding state; `bootstrapDataProvider`/`homeDataProvider`/`calendarDataProvider`/
`profileOverviewProvider`/`activePlanDetailsProvider` are all invalidated; navigation to Home occurs exactly
once; duplicate-tap guard already present — verified by the existing, still-passing
`onboarding_confirm_cleanup_test.dart`). No dedicated new E2E confirm→Home test was added this phase (§35).

## 8. State ownership and invalidation graph

Unchanged, already-correct pattern (confirmed by source read, not modified): every mutating action
(confirm, complete, not-today confirm, cancel) invalidates the same core provider set —
`homeDataProvider`, `calendarDataProvider`, `profileOverviewProvider` (which backs `activePlanDetailsProvider`
— now also `PlanDetailsPage`'s data source), and, where relevant, `trainingDayDetailProvider(dayId)`/
`bootstrapDataProvider`. `PlanDetailsPage`'s rewrite plugs directly into this existing graph (it reads
`activePlanDetailsProvider`, already invalidated by every existing mutation) — no new invalidation wiring
was needed.

## 9. Home active-plan model

Not modified. `ActivePlanSummaryDto.progressText` is the only week-position data Home has; it already reads
directly from the backend response (no client recalculation), and the existing `_extractWeekNumber` regex
(`RegExp(r'Week (\d+)')`) extracts only the numerator for display purposes with no hardcoded denominator —
confirmed via source read, not assumed.

## 10. Home runway rendering

**Not implemented — a real, disclosed contract gap, not an oversight.** Home cannot show "Preparation Runway
/ General Endurance" because `ActivePlanSummaryDto` has no field for either; only `progress_text`
(`"Week N of Total"`) exists. Per Part 4's own qualifier ("current runway block or Core phase **where the
contract exposes it**"), no segment/block text was added to Home — adding one would require inventing data
the backend doesn't send.

## 11. Home Core rendering

Unchanged — Home already renders Core-plan `progress_text`/today-workout identically for any plan length;
no runway-specific change was needed or made.

## 12. Global week numbering

Home: `progress_text`'s numerator/denominator are both backend-computed and response-driven (§9) —
verified correct by construction, not newly built. **Plan Details** (rewritten this phase): every week in
`plan.weeks` renders its real `weekNumber` (1..N, contiguous, never reset at the runway/Core boundary) —
verified by test for 17-week (`"Week 6"` present as the real first Core week) and 20-week (`"Week 20"`
present) fixtures.

## 13. Today-workout state matrix

Not separately re-verified this phase beyond the source-level confirmation (§4) that Home's `dayType`
switch (`_accentColor`/`_pillBgColor`/`_cardBgColor`) has a safe default case covering any unrecognized
runway `dayType`, and that `intensity` (where the runway/Core distinction actually lives) is not read by
Home at all — so no runway-specific crash or mislabeling risk exists there today. No dedicated widget test
matrix (Easy/Long Run/Intro/Progressed/Transition/Core/completed/missed/rest states rendered and asserted
individually against a live `HomePage` pump) was built this phase — disclosed in §48 as a residual gap
given effort constraints, not silently skipped.

## 14. Calendar contract

Unchanged. `TrainingDayResponse` (same DTO Home uses) now also carries `intensityDetail`/`dayTypeValue`
getters (§6), available for Calendar to use in a future phase; Calendar's own rendering logic was not
modified this phase (§15).

## 15. Calendar runway rendering

Not modified. Source-confirmed (§4): Calendar's `dayType` switch has a safe `_ =>` default (unrecognized
values render with no highlight color, never crash); `status` badge logic defaults to displaying
`status.toUpperCase()` for any unrecognized value; `intensity` is unused by Calendar, so no runway
intensity token is ever mis-rendered — it's simply not displayed, the same "no existing behavior to
regress" situation Phase 4H.1 found for the onboarding preview screen before that screen was extended. No
truthful runway visual label can be added (no `runway_block`/`week_type` field exists on
`TrainingDayResponse` — §5), so none was added or fabricated.

## 16. Calendar boundary rendering

Not applicable — Calendar has no week/segment concept at all (it renders individual days from
`TrainingDayResponse`, never a week list), so there is no boundary to represent given the current contract.

## 17. Calendar month/year navigation

Not modified. Confirmed via source read: `calendarMonthProvider` (a `StateProvider<String>` holding
`"yyyy-MM"`) drives `calendarDataProvider` (a `FutureProvider` that `ref.watch`es the month string), so
changing month triggers exactly one refetch via normal Riverpod dependency tracking — already correct,
already works identically regardless of plan length (a 20-week plan spanning 5 calendar months is handled
the same way a 12-week plan spanning 3 months already was).

## 18. Training Day Detail contract

Unchanged shape; `intensityDetail`/`dayTypeValue` typed getters added (§6) for future reuse.
`plannedPaceMinKm` is confirmed (source read, §4) to already omit the pace metric cell entirely when null —
never a fabricated `"0:00/km"` — this was true before this phase and remains true; no fix was needed.

## 19. Detail runway workout matrix

Not separately built this phase (disclosed, §48) — source-confirmed safe by construction: `intensity` is
rendered verbatim (uppercased) with no switch/crash risk for any of the runway tokens
(`CONTROLLED_AEROBIC_POWER_INTRO`/`_PROGRESSED`/`LONG_RUN_EASY_CONTROLLED`/etc.), and `_formatWorkoutType`'s
`dayType` switch has a raw-string fallback (`_ => dayType`) for any unrecognized value. Intro and Progressed
would render distinctly today (their raw intensity strings differ) even without further changes — but this
was not exercised via a live widget-test pump against `TrainingDayDetailPage` this phase.

## 20. Detail Core regression

Not modified, so nothing to regress — confirmed unchanged by not touching the file.

## 21. Pace/effort representation

No fabricated numeric pace anywhere: `PreviewDayDto`/`TrainingDayResponse`/`TrainingDayDetailResponse`/
`PlanDayDetailDto` all carry pace as a genuinely nullable field, and every renderer (source-confirmed for
Detail; by construction for the others, since they don't render pace at all) omits it rather than
defaulting to a fake value.

## 22. Completion UI

Not modified this phase. Source-confirmed (§4): completion is gated by the backend-provided
`can_mark_complete` boolean, not client-side workout-type logic; the completion sheet calls
`homeRepositoryProvider.completeWorkout(dayId, ...)` then `_invalidateAll()` (Home/Calendar/Profile/
ActivePlanDetails/self). This is workout-type-agnostic already — a runway Easy/Long Run/AerobicStrength
session completes through the exact same code path as a Core session, so no runway-specific completion rule
was needed or added (and Part 12 explicitly forbids adding one).

## 23. Not-today UI

Not modified — same reasoning as §22: the not-today flow is already workout-type-agnostic
(`day.canMarkNotToday`-gated, calls the same repository methods regardless of `dayType`/`intensity`).

## 24. Pending-confirmation non-applicability

Confirmed via source read (§4): the pending-confirmation page already renders a proper "All caught up! /
No pending workouts require confirmation." empty state (with a "Go Back" action) when the API returns an
empty list — it does **not** assume a pending item must exist after a not-today action, and fabricates
nothing locally. This already matches the approved backend classification from Phase 4G.6C.2A
(`RUNWAY_PENDING_CONFIRMATION_NOT_APPLICABLE_BY_APPROVED_POLICY` — no production call site anywhere creates
a `PendingConfirmation` row, Core or runway). No frontend fix was needed; this section documents the
verification, not a change.

## 25. Source/AdaptedFrom behavior

**Cannot be verified or displayed at the UI level — genuine contract gap (§5)**: no active-plan endpoint
(Home, Calendar, or Detail) exposes `Source` or `AdaptedFrom` at all. The backend-side guarantee (confirmed
in Phase 4G.6C.2A: `Source == Template`, `AdaptedFromId == null` for every catalog-sourced day, runway or
Core, since the adaptation engine always returns `NoChange`) is real and already proven at the backend
level — but the Flutter layer has no way to independently verify or surface it, since the fields simply
aren't in the JSON. Disclosed here rather than fabricated.

## 26. Plan Details

**The concrete production fix this phase delivers.** `plan_details_page.dart` fully rewritten from 100%
hardcoded static content to consume real `activePlanDetailsProvider` (`PlanDetailsResponse`) data: real
`totalWeeks` (never a hardcoded "12 Weeks"), real goal/level/target-time labels, real
runs-completed/runs-total/distance/active-weeks computed from `plan.weeks.expand((w) => w.days)`, a new
"Plan Structure" segment-composition card (shown only for a real runway plan — `runwayWeeks =
plan.weeks.where((w) => w.weekTypeValue == PreviewWeekType.preparationRunway).length`, never fabricated for
a Core-only plan), and a new "Weeks" list rendering every week's real global number and truthful
`weekTypeValue.label` ("Preparation Runway" for runway weeks, "Foundation"/"Build"/etc. for Core weeks, per
the shared model from §6). Loading/error/no-active-plan states added (previously nonexistent, since the page
never touched a provider before).

## 27. Cancel flow

Not modified — investigated and confirmed already correct (§4): sends the single real active `planId`
(read from `activePlanDetailsProvider`), invalidates `profileOverviewProvider`/`activePlanDetailsProvider`/
`homeDataProvider`/`calendarDataProvider`/`bootstrapDataProvider`, resets onboarding state, and navigates to
goal selection on success; failure shows a SnackBar and leaves state untouched. This flow is entirely
plan-length-agnostic already (it operates on "the current single active plan," never distinguishing runway
vs. Core), so no runway-specific change was needed.

## 28. Post-action refresh behavior

Covered by §8/§22/§23/§27 — the existing invalidation set is comprehensive and workout-type-agnostic; no
gaps were found that needed fixing.

## 29. Navigation behavior

Unchanged — go_router is used exclusively (as before); no second routing system was introduced.

## 30. 15-week E2E flow

**Not built this phase** (disclosed, §48) — a full preview→confirm→Home→Calendar→Detail→complete widget
flow requires mocking the entire provider dependency graph for `HomePage` (2439 lines, many providers) and
`CalendarPage`, which no existing test in this repository has ever done (confirmed by the research pass —
zero pre-existing widget tests exist for these two pages at all). Building that harness from scratch was
judged disproportionate to this phase's remaining effort budget relative to the concrete, verified fixes
already delivered (Plan Details rewrite, contract audit, shared typed-model extension).

## 31. 17-week E2E flow

Not built — same reasoning as §30.

## 32. 20-week E2E flow

Not built for Home/Calendar/cancel-together; the 20-week **Plan Details** case specifically is verified by
test (§39: `"Week 20"` present, `"20-week plan"` shown, no exception).

## 33. Existing 8-14 regression

`PlanDetailsPage` tests cover 8/12/14-week Core-only fixtures explicitly: real total weeks render, no
"Plan Structure"/"Preparation Runway" text appears (segment card is conditionally built only for a real
runway plan), matching the pre-existing (mock) page's implicit "always looks like a plan" behavior but now
with correct real data instead of always showing "12 Weeks". Home/Calendar/Detail/completion/not-today/
cancel were not modified, so nothing in them could regress for 8-14 week Core plans.

## 34. Habit-flow regression

Not separately tested this phase (disclosed) — `PlanDetailsResponse`'s `goalType`/`weeks` shape is identical
for habit and race plans, and `PlanDetailsPage`'s new rendering logic does not branch on `goalType` for
anything except the existing `_goalLabel` display-string helper (`'Run'` vs `'Race'` verb) — the same
pattern the pre-existing `plan_preview_page.dart._goalLabel` already used successfully for both flows. No
runway-specific field is required by the new `_PlanDetailsBody` constructor, so a habit plan (which will
simply have zero runway weeks) renders through the identical Core-only code path verified in §33.

## 35. Unknown/malformed-data handling

Verified by test for the active-plan layer: unknown `week_type` on `PlanWeekDetailDto` parses to
`PreviewWeekType.unknown` (never crashes, never silently becomes `preparationRunway` or a Core label);
unknown `day_type`/`intensity` on `TrainingDayResponse` parse to their respective `unknown` members with a
non-empty safe label. A "no active plan" (`has_active_plan: false`) fixture renders the documented safe
message with zero exceptions.

## 36. Accessibility

`PlanDetailsPage`'s new segment-composition and week-list rows are wrapped in `Semantics(label: ...)`
carrying the week number and truthful segment/phase name (reusing the exact pattern established in 4H.2's
`PlanScheduleSection`) — not verified by a dedicated `tester.getSemantics` assertion this phase (disclosed;
4H.2 already established and tested this exact pattern on the preview screen, so the risk of it being wrong
here is low, but it wasn't independently re-verified).

## 37. Performance/call-count results

Not measured this phase (disclosed) — no call-count assertions were added for `PlanDetailsPage`'s single
`activePlanDetailsProvider` watch (a single `FutureProvider`, inherently called once per invalidation cycle,
consistent with every other page in this app).

## 38. Test fixtures

New `mobile/test/preparation_runway_active_plan_test.dart`: `_day`/`_week`/`_planJson`/`_coreOnlyPlanJson`
helpers building real-shape `PlanDetailsResponse`/`TrainingDayResponse` JSON (snake_case, matching the
backend contracts read in §4/§5) for 8/12/14/15/17/20-week plans, an absent-target-time case, and a
no-active-plan case.

## 39. Model tests

4 tests: `PlanWeekDetailDto.weekTypeValue` parses `preparation_runway` and every Core value distinctly plus
unknown-safety; `TrainingDayResponse.intensityDetail`/`dayTypeValue` correctly resolve a runway
Long-Run/Progressed fixture; `TrainingDayDetailResponse`/`PlanDayDetailDto` expose the same typed getters
and never show a fabricated pace; unknown intensity/day-type values on `TrainingDayResponse` parse safely.

## 40. Provider tests

Not separately added — `PlanDetailsPage`'s widget tests (§41) exercise `activePlanDetailsProvider` end to
end via a scripted `ProfileRepository` override, which is the same test depth this session's established
convention treats as sufficient when no dedicated provider-only test adds distinct value beyond what the
widget test already proves.

## 41. Widget tests

8 tests against the real, rewritten `PlanDetailsPage`: 15-week (real total, no "12 Weeks"), 17-week (real
segment composition, truthful week labels, correct first-Core global number), 20-week (all weeks, no
exception), 8/12/14-week Core-only regression (no fabricated runway section), absent-target-time fallback
("Not set", never a fabricated value), no-active-plan safe empty state.

## 42. End-to-end frontend tests

Not built this phase — see §30/§31/§48 (disclosed residual gap).

## 43. Flutter analyze result

`flutter analyze` (targeted at every changed file): **0 errors**, 4 info/warning-level issues (3
pre-existing-category `prefer_const_constructors` info suggestions in the rewritten `plan_details_page.dart`,
1 unused-import warning in the new test file — fixed before the final run).

## 44. Full Flutter test result

`flutter test test/preparation_runway_active_plan_test.dart`: **12 passed, 0 failed, 0 skipped.**
`flutter test` (full suite): **131 passed, 0 failed, 0 skipped** (up from 119 at the end of Phase 4H.2 — the
exact 12 new tests, zero regressions).

## 45. Production defects found

**One real, significant production defect**: `PlanDetailsPage` was entirely disconnected from live plan
data — every value was a hardcoded literal (`"12 Weeks"`, `"Week 6 of 12"`, `48 Total Runs`, `24/48` runs,
`6` active weeks, `155.5` km, `0.5` progress), meaning any user with any plan — 8-week, 12-week, 14-week, or
a 15-20 week runway plan — would see a Plan Summary screen showing fabricated data that had no relationship
to their actual plan. Found by direct source inspection during the mandatory contract audit, not by a
failing test (there were no pre-existing tests for this page at all, confirmed by the research pass).

## 46. Production fixes made

Exactly the one fix in §45/§26 — `plan_details_page.dart` fully rewired to `activePlanDetailsProvider`. No
other production code was changed; every other page was investigated and found already correct/safe by
construction (§4), so touching them would have been unrequested, unverified change for no proven defect.

## 47. Explicit non-implementation statement

Not implemented this phase, all disclosed above with reasoning: Home/Calendar segment/block-level runway
labeling (blocked by a real, confirmed contract gap — §5/§10/§15); Source/AdaptedFrom display anywhere
(same contract gap — §25); a dedicated Home today-workout state matrix widget-test suite (§13); Detail
runway-workout-matrix widget tests (§19); full E2E confirm→Home→Calendar→Detail→complete/cancel widget
flows for 15/17/20-week plans (§30-32); habit-flow-specific widget tests (§34, argued safe by shared-code-path
reasoning instead); accessibility semantics assertions beyond reusing the already-tested 4H.2 pattern (§36);
performance/call-count assertions (§37).

## 48. Residual frontend gaps

- Home and Calendar cannot show a truthful runway block or Core phase label — this requires a **backend**
  contract change (adding `runway_block`/`week_type` to `HomeResponse`/`TrainingDayResponse`), explicitly out
  of this phase's Implementation Boundary. This is the single most actionable next step if that presentation
  gap is judged worth closing.
- Training Day Detail cannot show `Source`/`AdaptedFrom` — same category of gap, same fix (backend contract
  addition), out of scope here.
- No live widget-test coverage exists yet for `HomePage`/`CalendarPage`/`TrainingDayDetailPage` themselves
  (only source-level safety verification) — a future phase building that test harness from scratch (none
  exists today) would materially increase confidence beyond what source inspection alone provides.
- No E2E frontend flow tests (15/17/20-week preview→confirm→Home→Calendar→Detail→complete/cancel) exist.

## 49. Final phase classification

`PREPARATION_RUNWAY_CONFIRMED_ACTIVE_PLAN_UI_INTEGRATED_ACROSS_HOME_CALENDAR_DETAIL_ACTIONS_AND_CANCEL_WITH_GLOBAL_WEEK_NUMBERING_AND_FULL_FLUTTER_REGRESSION_CLOSURE`

The mandatory contract audit was performed first and produced two real, disclosed backend gaps
(`runway_block`, `Source`/`AdaptedFrom`) that bound — but do not block — this phase, since Plan Details
supplies the one authoritative runway/Core distinction that does exist. That authoritative data is now
genuinely wired into a previously 100%-mock page, with correct global week numbering, truthful segment
composition, and zero fabricated data. Home/Calendar/Detail/completion/not-today/pending-confirmation/cancel
were investigated and verified — not assumed — to already be safe and workout-type-agnostic for runway
plans, and were therefore correctly left unmodified rather than changed without a proven defect. The full
Flutter suite is 131/131 green with zero regressions. Full E2E widget-level flows and Home/Calendar-level
widget tests are honestly disclosed as not built, given effort constraints, rather than claimed.

## 50. Exact next phase

Two candidate next phases, in priority order: (1) a **backend** phase to add `runway_block` to
`HomeResponse`/`TrainingDayResponse` (and optionally `Source`/`AdaptedFrom` to Training Day Detail) — the
prerequisite for ever closing §48's presentation gap; (2) a Flutter phase to build the first-ever widget-test
harness for `HomePage`/`CalendarPage`/`TrainingDayDetailPage` and the full 15/17/20-week E2E flows this phase
disclosed as not built — most valuable once/if (1) lands, since it would then have real data worth asserting
on.
