# Phase 4L.5B — Flutter End-to-End Flow, Navigation and Production-Wiring Test Closure

**Date:** 2026-08-06
**Status:** PARTIAL. Real, passing, connected-system flow and navigation tests now exist for the highest-value paths (preview→confirm→Home, Home↔Calendar↔detail navigation, completion, not-today, activation, retry-vs-activation separation, terminal-state access, invalid deep links), and two real production bugs were found and fixed along the way. Regenerate/cancellation flow, authentication-redirect behavior, and most timeout/concurrency-ambiguity paths remain untested, so `TD-LONG-HORIZON-FLUTTER-CALENDAR-ERROR-MAPPING-ACCESSIBILITY-TEST-CLOSURE-001` stays **OPEN**, and the new record `TD-LONG-HORIZON-FLUTTER-END-TO-END-FLOW-NAVIGATION-TEST-CLOSURE-001` is recorded **OPEN**.

---

## 1. Executive result

`LONG_HORIZON_FLUTTER_END_TO_END_FLOW_AND_NAVIGATION_TEST_CLOSURE_COMPLETED` is **NOT** claimed. What is real:

- 21 new flow/navigation tests, all passing, using the real widget tree, a real (minimal, Firebase-free) `GoRouter`, and real screens (`LongHorizonPlanPreviewPage`, `ActiveHomeDispatcherPage`, `ActiveCalendarDispatcherPage`, `LongHorizonHomePage`, `LongHorizonCalendarPage`, `RollingSessionDetailPage`) wired together exactly as they are in production — not isolated unit assertions.
- Two real production bugs found and fixed while writing these tests: (1) `RollingSessionDetailPage._complete()`/`_notToday()` had no duplicate-tap guard at all (unlike the preview page's `_onConfirm`, which did) — a rapid double tap fired two real mutation requests; (2) the not-today reason bottom sheet overflowed its `RenderFlex` on any viewport shorter than its content, because it used a bare `Column` inside a height-constrained modal sheet instead of a scrollable one.
- Full suite: **304 passed, 0 failed** (up from 283 at the end of Phase 4L.5A). `flutter analyze`: 0 errors. Touched files are formatted with `dart format`.

**Not done**: regenerate/cancellation flow proof (no such UI screen exists yet — a pre-existing, disclosed gap from Phase 4L.5A, §14 there), authentication-redirect proof (the test harness deliberately has no `redirect`/Firebase dependency, following the established `active_plan_test_harness.dart` precedent — real auth-redirect logic was not independently re-tested), most timeout/concurrency-specific ambiguity paths (only the completion-conflict error-mapper wiring was proven end-to-end; activation-concurrency, retry-ambiguity, and confirmation-replay-via-timeout were not), accessibility-in-flow tests, and a formal back-stack semantics audit beyond "pop returns to the calling screen."

## 2. Inherited open gaps

From Phase 4L.5A: end-to-end flow coverage, most navigation coverage, and full connected-system proof. This phase materially advances all three without fully closing them.

## 3. Scope and exclusions

Same constraints as 4L.5/4L.5A: no backend changes, no Riverpod rewrite, no new endpoints, no client-side planning/recovery duplication, no Pending synthesis, no automatic activation/retry, no background work, no broad reformat, no Phase 4L.6 work, no commits. Two backend-*adjacent* fixes were made, but **both are Flutter-only** (a missing guard and a layout overflow) — zero backend files were touched.

## 4. Routing/test infrastructure inspection

Inspected: `app_router.dart`'s real `GoRouter` (confirmed its `redirect` callback calls `FirebaseAuth.instance.currentUser` directly and unconditionally on every navigation — meaning the literal production router singleton cannot be exercised in a widget test without Firebase test-mocking infrastructure, which this repo does not have); `active_plan_test_harness.dart` (confirmed and reused its established precedent: a fresh, minimal, `redirect`-free `GoRouter` per test); every Phase 4L.5/4L.5A screen and provider; `ProviderScope` override patterns already used throughout the existing test suite. No `integration_test/` directory exists in this project.

## 5. Test-harness decision

**High-level widget tests with a real router and scripted repository overrides** (option 1 of the preferred hierarchy) — the same category of harness already used successfully in `active_plan_test_harness.dart` and Phase 4L.5A's Calendar/accessibility tests. A new shared harness, `test/support/long_horizon_flow_test_harness.dart`, provides `ScriptedLongHorizonRepository` (records every call, throws/returns exactly what a test configures), `buildLongHorizonFlowTestRouter` (a minimal, `redirect`-free router covering preview/Home/Calendar/rolling-detail/static-detail), and `pumpLongHorizonFlowApp` (pumps a real `MaterialApp.router` with scripted `ProviderScope` overrides). `integration_test`-level coverage was judged unnecessary — every flow this phase covers is fully provable at the widget level with a real router, and adding a second, heavier test tier would contradict the "choose the narrowest reliable test approach" instruction.

**A real subtlety found and documented**: `ActiveHomeDispatcherPage`/`ActiveCalendarDispatcherPage` eagerly `watch` `activeHomeResultProvider` on first build. A repository must be pre-configured with its full script *before* the harness's own initial `pumpAndSettle()` — configuring it afterward is too late, and the resulting `null` field read throws, which the dispatcher's `error` branch silently swallows into a static-Home fallback. This was hit twice while writing these tests, is one of the reasons some early test drafts silently exercised the wrong screen, and is now documented directly in the harness's own doc comment as a trap for future test authors.

## 6. Route graph

| Route | Path | Strategy | Auth (this test harness) | Notes |
|---|---|---|---|---|
| Long-Horizon preview | `/onboarding/plan-preview/long-horizon` | n/a (pre-plan) | none (harness has no redirect) | `LongHorizonPlanPreviewPage` |
| Home | `/home` | dispatched | none | `ActiveHomeDispatcherPage` → `HomePage` or `LongHorizonHomePage` |
| Calendar | `/calendar` | dispatched | none | `ActiveCalendarDispatcherPage` → `CalendarPage` or `LongHorizonCalendarPage` |
| Rolling detail | `/training-day/rolling/:sessionId` | rolling only | none | `RollingSessionDetailPage` |
| Static detail | `/training-day/:dayId` | static only | none | `TrainingDayDetailPage` (untouched, included for parity) |

Real production auth (`redirect` + `FirebaseAuth.instance`) is not part of this table because it was not re-tested this phase (§21 below explains why).

## 7. Preview→confirmation→Home

Proven (`long_horizon_flow_preview_confirmation_test.dart`, 4 tests): a `ReadyForRollingPersistence` preview enables the confirm button and reaching Home after confirm; a `NotReadyForConfirmation` preview disables it and confirm is never called; a rapid double tap (invoking `onPressed` twice synchronously, since `tester.tap()` cannot reproduce a true race without violating `flutter_test`'s own overlapping-gesture guard) only calls confirm once; and a dedicated regression proves a preview using the OLD, incorrect PascalCase wire values (the exact Phase 4L.5A defect) still fails closed through the live screen, not just the decoder unit test.

## 8. Confirmation replay/ambiguity

**Not covered.** No test simulates an `AlreadyConfirmed` replay response or a lost/timed-out confirmation response requiring an active-plan read-after-write check. Real, disclosed gap.

## 9. Home→Calendar

Proven: navigating from Home to Calendar via `router.go` renders the rolling Calendar with the exact requested month, and a real test proves an empty rolling Calendar response never renders a Pending/future-week placeholder (`long_horizon_flow_navigation_test.dart`).

## 10. Home→rolling detail

Proven: tapping a Home session card requests exactly that `SessionId` and lands on `RollingSessionDetailPage` (never `TrainingDayDetailPage`).

## 11. Calendar→rolling detail

Proven: tapping a Calendar entry requests exactly that `SessionId` and lands on the same rolling detail screen.

## 12. Completion flow

Proven (`long_horizon_flow_mutation_test.dart`): entering valid values and tapping Mark complete sends the exact `{distance, duration}` request, never calls activation automatically, and pops back to the calling screen; empty/invalid input blocks the request entirely with no repository call; a rapid double tap only calls the repository once — **only after this phase found and fixed the missing guard** (§1).

## 13. Completion conflicts/ambiguity

Partially covered: a `LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT` response is proven to route through the central mapper (no raw code shown, detail is re-requested). Timeout-specific ambiguity (a lost response, not an explicit conflict) is **not** covered.

## 14. Not-today flow

Proven: choosing an approved reason (Illness) sends the exact backend token `illness`, and never calls activation.

## 15. Not-today conflicts/ambiguity

**Not covered** beyond what completion conflict already proves about the shared mapper wiring (both mutations use the identical `refreshDetail` mapping path). No dedicated not-today-specific conflict test was written.

## 16. Activation-ready flow

Proven: the Activate action appears only when `checkpointReadiness == nextWindowActivationReady`, calling it hits the repository exactly once, and a double tap is prevented (the guard already existed here from Phase 4L.5, verified still correct).

## 17. Activation stale/concurrency/ambiguity

**Not covered.** No test drives a `CurrentWindowInProgress`/concurrency-conflict/timeout response through the live Activate button.

## 18. Retry flow

Proven, and this is the phase's second-most important result after the duplicate-tap fix: the Retry action appears only for `calendarWindowPending`, tapping it calls retry exactly once, and — critically — **retry never also calls activation in the same action** (`repo.activateCallCount == 0` asserted directly after a successful retry). `RegeneratePreviewRequired` and `OperationalSupportRequired` are proven to hide the Retry button entirely and show their own safe messages instead.

## 19. Retry rejection

Covered for the two "hide the button" states (§18). `RetryNotEligible` (a runtime error response rather than a readiness state) is not separately driven through a live retry tap — the mapper's own unit test (Phase 4L.5A) already proves its classification; the live-flow wiring specifically was not re-proven.

## 20. Regenerate/cancellation flow

**Not covered — cannot be, yet.** As disclosed in Phase 4L.5A §14/§22, no regenerate-preview or cancel-and-create-new-plan screen exists in the app. The error mapper correctly classifies the relevant codes, but there is no UI to navigate through. This remains the single largest structural gap blocking full closure.

## 21. Terminal flow

Proven: a `TerminalPlanComplete` Home shows "Plan complete" with neither an Activate nor a Retry button, and Calendar remains fully reachable and renders a Completed entry from that same terminal state (`long_horizon_flow_navigation_test.dart`).

## 22. Deep links

Proven: an unknown `SessionId` deep link (`/training-day/rolling/does-not-exist`) never crashes and never falls back to the static detail screen — it shows the mapper's safe "couldn't find that session" message. Cross-strategy ID mismatch, unauthenticated deep-link redirect, and "no protected-content flash" are **not covered** (§23 explains why for auth specifically).

## 23. Authentication bootstrap

**Deliberately not re-tested this phase.** The real `AppRouter.router`'s `redirect` callback calls `FirebaseAuth.instance.currentUser` directly and unconditionally; exercising it in a widget test would require Firebase test-mocking infrastructure (e.g. `firebase_auth_mocks`) that does not exist anywhere in this repository. Following the exact precedent already established (and explicitly documented) in `active_plan_test_harness.dart`, every flow test in this phase uses a fresh, `redirect`-free router instead. This keeps 283 pre-4L.5B tests plus these 21 new ones fast, deterministic, and Firebase-independent, but means real auth-redirect behavior remains proven only by manual/production testing, not automated tests. The six Firebase-init regression fixes from Phase 4L.5 remain in place and green (part of the 304-test full suite).

## 24. Back-stack semantics

Proven only narrowly: completion/not-today mutations correctly pop back to the screen that pushed the detail route (proven by asserting the Calendar month header reappears). Broader back-stack claims (confirmation replacing the onboarding stack so system-back can't return to a stale confirmable preview, repeated-router-refresh not duplicating Home, etc.) are **not independently tested** this phase.

## 25. Provider invalidation proof

Indirectly proven through the flow tests themselves (e.g. `repo.activateCallCount`/`repo.retryCallCount` assertions demonstrate the correct repository call graph), but no test directly asserts which specific Riverpod providers were invalidated after a flow-level mutation (Phase 4L.5A's `long_horizon_calendar_provider_test.dart` already covers this at the provider-unit level; this phase did not add a flow-level equivalent).

## 26. Duplicate-state protection

The two duplicate-tap fixes (§1) are exactly this requirement, proven for completion and confirm; activation's pre-existing guard was also verified. Not extended to not-today (no dedicated double-tap test for it, though it shares the same `_mutating` guard as completion) or to replay-after-navigation-refresh scenarios.

## 27. Error-mapper wiring

Proven for exactly one flow (completion conflict → `refreshDetail`) end-to-end through a live screen. Phase 4L.5A already proved the mapper's classification logic in isolation for all 24 codes; this phase proves the *wiring itself* is real for one representative case, not for all nine flows the prompt requested.

## 28. Accessibility in flows

**Not covered this phase.** Phase 4L.5A's accessibility tests remain valid and passing (unmodified), but no new flow-level accessibility test was added.

## 29. Analytics/logging flow verification

N/A — no analytics subsystem exists (unchanged from Phase 4L.5A §19/§20).

## 30. Static controls

Not newly added this phase — Phase 4L.5's `long_horizon_preview_routing_test.dart` already proves a ≤20-week race never calls the Long-Horizon repository, and the dispatcher tests in this phase implicitly prove the static fallback path renders `CalendarPage`/`HomePage` when strategy resolves to static (exercised as the dispatcher's error-fallback branch during harness debugging, §5).

## 31. Habit controls

Not newly added — `long_horizon_preview_routing_test.dart` (Phase 4L.5) already proves Habit goals never route to the Long-Horizon repository.

## 32. Test-matrix reconciliation

| Category | Status |
|---|---|
| Preview/confirmation (items 1-12) | Covered except confirmation replay/timeout (item 10-11) |
| Home/Calendar/detail navigation (13-22) | Covered for rolling; static/terminal partially; cross-strategy-ID-mismatch and static-route items rely on pre-existing static tests, not newly re-verified |
| Completion (23-34) | Covered except exact-replay-as-success and pure-timeout-verification (30, 32) |
| Not-today (35-45) | Covered except replay/timeout (40, 42) |
| Activation (46-59) | Covered except replay, concurrency, timeout, terminal-result-mapping (53-57) |
| Retry (60-69) | Covered except exact-request-body assertion and timeout (61, 68) |
| Regenerate/cancel (70-76) | **Not covered — no UI exists** |
| Terminal (77-83) | Covered except explicit replay-stability re-tap |
| Deep links/auth (84-92) | Partially covered (84-85, 88-89); auth-redirect items (86-87, 90-92) not covered (§23) |
| Provider/state quality (93-102) | Covered at the provider-unit level in Phase 4L.5A; not re-proven at flow level this phase |
| Error mapper wiring (103-113) | One of nine flows proven end-to-end; the rest proven only at the mapper-unit level (Phase 4L.5A) |
| Accessibility/analytics (114-123) | Not extended this phase (Phase 4L.5A's own tests remain valid) |
| Static/Habit controls (124-130) | Covered by pre-existing Phase 4L.5 tests, not duplicated |
| Constraints (131-140) | Held throughout — no client-side planning, no automatic activation/retry, no background work, touched files formatted, Phase 4L.6 not started |

**Material uncovered gaps, in priority order**: (1) regenerate/cancellation flow — blocked on a missing screen, not a missing test; (2) authentication-redirect proof — blocked on missing Firebase test-mocking infrastructure; (3) timeout/concurrency-specific ambiguity across activation and retry; (4) the remaining 8 of 9 error-mapper wiring flows.

## 33. Test quality

All 21 new tests use behavior-focused names, deterministic scripted fixtures (no `Future.delayed` timing dependencies except the necessary `pumpAndSettle`), independent `ProviderScope`/router instances per test, and explicit repository-call-count/request-body assertions rather than snapshot comparisons.

## 34. Formatting

`dart format` applied to exactly the 5 files touched this phase (the new harness, three new test files, and the one production file with the duplicate-tap/overflow fixes). Project-wide `dart format --set-exit-if-changed` remains red at the same pre-existing 73/105-file count as Phase 4L.5A reported — this phase introduced zero new unformatted files and did not attempt to close the pre-existing debt.

## 35. Analyze/tests

`flutter analyze`: 0 errors, 167 info/warning-level issues (all pre-existing except one unused import this phase introduced and then removed). Full `flutter test`: **304 passed, 0 failed, 0 skipped** (283 baseline + 21 new). Backend suites were **not re-run** this phase — zero backend files were touched, and the two fixes made were purely Flutter-side (a missing guard, a layout overflow), so there is no backend surface for a regression to hide in.

## 36. Backend changes

**None.**

## 37. Governance

New record `TD-LONG-HORIZON-FLUTTER-END-TO-END-FLOW-NAVIGATION-TEST-CLOSURE-001` appended **OPEN** — regenerate/cancellation flow, authentication-redirect proof, and most ambiguity/concurrency paths remain uncovered. `TD-LONG-HORIZON-FLUTTER-CALENDAR-ERROR-MAPPING-ACCESSIBILITY-TEST-CLOSURE-001` **remains OPEN** — its own closure bar ("no material flow gap, no material navigation gap") is not yet met by this phase's real but partial progress. `TD-LONG-HORIZON-FLUTTER-READ-OUTCOME-RETRY-CONTINUATION-INTEGRATION-001` remains **CLOSED** — no regression was found against it. Release readiness, plan-catalog governance-test debt, project-wide format debt, and Phase 4L.6 are untouched.

## 38. Final classification

`LONG_HORIZON_FLUTTER_FLOW_NAVIGATION_CLOSURE_REMAINS_BLOCKED_BY_EXPLICIT_ROUTING_MUTATION_REFRESH_AMBIGUITY_DEEP_LINK_AUTH_TERMINAL_REGENERATE_STATIC_HABIT_OR_TEST_GAP` — specifically the regenerate/cancellation UI gap (structural, not a test gap) and the authentication-redirect test gap (tooling, not implementation).

## 39. Exact next phase

A phase that (a) builds the regenerate-preview/cancel-and-create-new-plan screen the error mapper already classifies for but has no UI to route to, and (b) either adds minimal Firebase test-mocking infrastructure or accepts auth-redirect as a manually-verified-only path, would close both open governance records outright. After that, Phase 4L.6 (End-to-End Release Acceptance, Governance Test-Debt Closure and Production Readiness) is the right next step.
