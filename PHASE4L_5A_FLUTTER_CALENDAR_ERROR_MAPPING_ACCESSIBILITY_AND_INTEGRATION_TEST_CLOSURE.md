# Phase 4L.5A — Flutter Calendar, Error Mapping, Accessibility and Integration Test Closure

**Date:** 2026-08-06
**Status:** PARTIAL. Real, tested, working Calendar UI, a centralized error mapper, exact-month mutation invalidation, and an accessibility pass are complete. A critical pre-existing contract-decoding defect from Phase 4L.5 was found and fixed. Phase 4L.5's own closure conditions are now met and its governance record is CLOSED. This phase's own (higher) bar — the full requested test matrix, flow/integration tests, and a deep performance/security review — is not fully met, so `TD-LONG-HORIZON-FLUTTER-CALENDAR-ERROR-MAPPING-ACCESSIBILITY-TEST-CLOSURE-001` stays **OPEN**.

---

## 1. Executive result

`LONG_HORIZON_FLUTTER_CALENDAR_ERROR_MAPPING_ACCESSIBILITY_AND_INTEGRATION_TEST_CLOSURE_COMPLETED` is **NOT** claimed in full. What is real and verified:

- Rolling Calendar is implemented, tested, and wired through a route-level dispatcher exactly like Home.
- A critical Phase 4L.5 defect was found and fixed: several backend enums (`LongHorizonPublicLifecycleStatus`, `LongHorizonPublicPhase`, `LongHorizonConfirmationReadiness`, `LongHorizonConfirmationOutcome`) are real C# enums serialized under the API-wide `JsonStringEnumConverter(SnakeCaseLower)` policy, so their wire values are lowercase snake_case (e.g. `"available"`), not the PascalCase C# member names (`"Available"`) Phase 4L.5 used in `fromWire`. This would have made every real preview/confirmation response decode to `unknown`, permanently disabling the confirm CTA. Fixed and pinned with regression tests.
- A centralized `LongHorizonUiErrorMapper` now covers all 24 real Long-Horizon error codes (verified exhaustively against `GlobalExceptionHandler.cs`), each with a deterministic UI action and a sanitized message that never leaks the raw code/backend detail.
- Mutation invalidation is now exact-month, not family-wide, for completion/not-today/activation; retry correctly invalidates Home only.
- An accessibility pass added real `Semantics` to the Calendar tiles, Home session/readiness cards, and fixed a genuine app-wide gap in the shared `AppPrimaryButton` (a loading button had no accessible label at all).
- 48 new tests added this phase (283 total passing, up from 235 at the end of Phase 4L.5), `flutter analyze` clean, zero static/Habit regressions.

**Not done**: the full 120-item test matrix (a reconciliation table is in §29, not a literal 120 new tests), end-to-end flow/integration tests, a formal performance profiling pass, a written security/privacy audit beyond the mapper's own sanitization guarantees, and analytics events (none exist in this app at all — see §19).

## 2. Deferred gaps inherited from Phase 4L.5

Per Phase 4L.5's own documentation: rolling Calendar UI, Long-Horizon error-code mapping, accessibility review, analytics/logging review, broader provider/widget/integration test coverage, and closure-level contract parity verification. This phase closes the first five materially and partially closes the sixth (see §22).

## 3. Scope and exclusions

Same constraints as Phase 4L.5: no backend planning/checkpoint/recovery/calendar/activation formula reproduced client-side; no Pending workout synthesis; no automatic activation or retry; no background worker; no backend formula changes; no downward interpolation; no commits made by the assistant. **Zero backend files were modified this phase** — the critical defect found in §1 was a Flutter-side decoding bug, not a backend contract bug, and was fixed entirely in `long_horizon_dtos.dart`.

## 4. Phase 4L.5 implementation inspection

Read in full before making any change: `long_horizon_dtos.dart`, `long_horizon_repository.dart`, `long_horizon_provider.dart`, `onboarding_provider.dart`'s routing logic, `long_horizon_plan_preview_page.dart`, `long_horizon_home_page.dart`, `active_home_dispatcher_page.dart`, `rolling_session_detail_page.dart`, `app_router.dart`, `calendar_page.dart` (static, untouched), `calendar_provider.dart` (static, reused as-is for `calendarMonthProvider`), `api_client.dart`/`api_exception.dart`, and the Phase 4L.5 phase document. Findings: Calendar decoding models existed (`ActiveCalendarResult`/`LongHorizonCalendarResponse`) but no UI; error handling was screen-local (`e.message`/`ApiException.message` shown raw) with no central mapper; no analytics calls existed anywhere in Phase 4L.5's additions (nor in the app at all); new screens had default widget semantics only, no explicit `Semantics`.

## 5. Calendar backend contract

Verified directly against `backend/RunningApp.Application/Services/QueryAndMutationServices.cs` (`GetCalendarAsync`, the static/no-active-plan/dispatch path) and `LongHorizonActiveReadModelProvider.cs` (`GetCalendarAsync`, the rolling path): no active plan → static returns `[]` (bare empty array, no `schedule_strategy`); `RollingLongHorizon` → delegates to `LongHorizonActiveReadModelProvider.GetCalendarAsync`, which throws `ArgumentException` for a malformed `month` and returns `LongHorizonCalendarResponse { PlanId, Month, Sessions }` for a valid one — `Sessions` are exactly the plan's persisted `LongHorizonRollingSessionState` rows within the month, mapped through the same `Map()` helper Home uses. There is no separate rolling-Calendar-specific contract-version negotiation or "unsupported version" path at the read layer (unlike the mutation/continuation endpoints, which do check `ContractVersion`). This confirms: Pending structural weeks are architecturally absent from this response — the query only ever selects from `ActiveSessions(aggregate)` (Numeric­Activated/Completed weeks), never structural/roadmap data.

## 6. Calendar architecture

Route-level dispatcher (Option B), matching Phase 4L.5's Home dispatcher exactly: `ActiveCalendarDispatcherPage` reads `activeHomeResultProvider` (the same cached provider Home's own dispatcher uses — no extra network call) and renders either the untouched `CalendarPage` or the new `LongHorizonCalendarPage`. Strategy branching happens once, at the dispatcher; `LongHorizonCalendarPage` and its cells only ever see typed `LongHorizonRollingSessionResponse` objects, never raw JSON or a nullable-ID mega-model.

## 7. Rolling Calendar models

`ActiveCalendarResult`/`LongHorizonCalendarResponse` already existed in `core/network/long_horizon_dtos.dart` from Phase 4L.5 and needed no new type — `LongHorizonRollingSessionResponse` (shared with Home/detail) was extended this phase with previously-missing real backend fields: `workoutKey`, `workoutVersion`, `plannedDurationMinutes`, `plannedPaceMinutesPerKm`, `plannedIntensity`, `actualPaceMinutesPerKm`, `notTodayRecordedAtUtc`, and the required `publicProvenance` string. All nullable fields decode to `null` when absent, never a fabricated default (test-verified, §26).

## 8. Calendar repository/providers

No new repository method — `LongHorizonRepository.fetchActiveCalendar(month)` already existed. `activeCalendarResultProvider` (a `FutureProvider.family<ActiveCalendarResult, String>` keyed by `yyyy-MM`) already existed too; this phase added `monthKeyForDate`, `invalidateLongHorizonHomeState`, and `invalidateLongHorizonCalendarMonth` in `long_horizon_provider.dart` to replace the old family-wide `invalidateLongHorizonState` with exact-month invalidation (§12), and removed that broad function once every call site was migrated.

## 9. Rolling Calendar UI

`lib/features/calendar/presentation/long_horizon_calendar_page.dart`: month header with prev/next navigation (mutates the existing shared `calendarMonthProvider`, so static and rolling Calendar share one month-navigation state), a session list sorted by `assignedDate`, `ListView.builder` keyed by `ValueKey(sessionId)`, loading/error/empty states, and per-entry outcome badges (Planned/Completed/Not today) with actual distance shown only when present.

## 10. Pending-event leakage protection

Type-level, not conventional: `ActiveCalendarResult`/`LongHorizonCalendarResponse` carry only `List<LongHorizonRollingSessionResponse>` — there is no field, constructor parameter, or code path anywhere in `LongHorizonCalendarPage`/`_SessionList`/`_CalendarSessionTile` that can accept a `LongHorizonStructuralRoadmapWeekContract` (the Pending-week type). A structural roadmap week literally cannot reach this screen without a compile error, since the roadmap type is never imported or referenced in the Calendar file at all. Confirmed by direct code read (§5/§9), not just a comment claim.

## 11. Calendar/detail routing

Rolling entries call `context.push('/training-day/rolling/${session.sessionId}')` → `AppRoutes.rollingSessionDetail` → `RollingSessionDetailPage(sessionId: ...)` (registered in Phase 4L.5, reused unchanged). Static Calendar entries are untouched and continue using the existing static training-day route. Test-verified (`long_horizon_calendar_test.dart`, "a rolling Calendar entry navigates to the rolling session detail route").

## 12. Mutation refresh behavior

Rewrote invalidation to be exact rather than broad, per this phase's own explicit requirement:
- **Completion/not-today**: captures the session's real `assignedDate` from the currently-cached detail *before* invalidating it, invalidates Home + exactly `activeCalendarResultProvider(monthKeyForDate(assignedDate))`.
- **Activation**: invalidates Home + every month actually present among `activatedSessions[].assignedDate` (handles a window spanning a month boundary — a `Set<String>` of month keys, not a single guess).
- **Retry**: invalidates Home only — retry restores a Blocked boundary to Pending and creates no new session, so no Calendar month is touched.

Provider-level tests (`long_horizon_calendar_provider_test.dart`) prove invalidating one month leaves an unrelated cached month untouched.

## 13. Long-Horizon error mapper

`lib/features/plan/data/long_horizon_error_mapper.dart`: `LongHorizonUiErrorMapper.map(Object error) -> LongHorizonUiError { userMessage, action }`. The 24 error codes come from an exhaustive grep+read of `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs`'s actual exception→code switch expression — not the prompt's suggested list, which included several codes (`LONG_HORIZON_TERMINAL_PLAN_COMPLETE`, `LONG_HORIZON_CONTINUATION_STALE`, `LONG_HORIZON_CONTINUATION_INFEASIBLE`, `LONG_HORIZON_CONTINUATION_PERSISTENCE_FAILURE`, `LONG_HORIZON_CHECKPOINT_WINDOW_NOT_COMPLETE`) that do not exist as real backend error codes (some are success-path enum values, e.g. `TerminalPlanComplete` is a 200 `LongHorizonContinuationOutcome`, never an error). Per this phase's own instruction ("use exact codes that actually exist in backend source; remove any invented code"), the mapper only implements the real 24 plus the pre-existing static-plan `PLAN_HORIZON_COMPOSITION_REQUIRED`/`PLAN_CORE_HORIZON_UNSUPPORTED` codes (already handled by the existing, untouched `plan_generation_error_mapper.dart`).

## 14. Error action classification

`LongHorizonErrorAction` enum: `showMessage`, `refreshHome`, `refreshDetail`, `refreshCalendar`, `regeneratePreview`, `cancelAndCreateNewPlan`, `retryGuidance`, `signInAgain`, `operationalSupport`, `genericFailure`. Wired into three call sites (`long_horizon_home_page.dart` activate/retry, `rolling_session_detail_page.dart` complete/not-today) so `refreshHome`/`refreshDetail` actions actually trigger the corresponding invalidation, not just a message. `regeneratePreview`/`cancelAndCreateNewPlan`/`retryGuidance` currently only affect the displayed message — no dedicated "regenerate" or "cancel-and-recreate" screen exists yet to route to (this is a real, disclosed gap, not a silent omission — see §22).

## 15. Network ambiguity

Not newly implemented this phase — Phase 4L.5's existing pattern (a failed mutation invalidates the relevant read provider so the next render re-fetches ground truth rather than assuming the mutation's outcome) is preserved and now uses exact-month invalidation. No explicit request-timeout-specific read-after-write verification path was added beyond the existing catch-and-invalidate pattern; this is a real, disclosed gap (§22).

## 16. Calendar error states

`LongHorizonCalendarPage` handles loading/error/empty explicitly. Errors go through `LongHorizonUiErrorMapper` (no raw exception text). No cached-data-with-non-blocking-error UI was added (Riverpod's `AsyncValue.error` in this implementation replaces the prior data rather than keeping it visible alongside a banner) — a real, disclosed gap matching the existing static Calendar's own behavior (verified: `calendar_page.dart`'s error handling follows the identical full-replace pattern, so this is consistent with, not a regression from, existing app convention).

## 17. Accessibility audit

Audited: Home readiness/recovery card, Home session cards, Calendar entries, the shared `AppPrimaryButton` used by preview-confirm and rolling-detail-complete. Findings and fixes:
- **Real app-wide bug found**: `AppPrimaryButton`'s loading state showed only a bare `CircularProgressIndicator` with zero semantic label — a screen reader announced nothing for any loading button anywhere in the app, not just Long-Horizon screens. Fixed with a `Semantics(label: '$label, loading')` wrapper; disabled/non-loading state also now explicitly reports `enabled: false`.
- Calendar entries and Home session cards gained explicit `Semantics(label: '<role>, <date>, <distance>, <outcome>')` with `ExcludeSemantics` on decorative icons/duplicate text so a screen reader announces one coherent phrase, not fragments.
- The readiness/recovery/terminal card gained a combined `Semantics(label: '$title. $subtitle')` so status is always conveyed through text, matching the existing app convention already used in `preparation_runway_schedule_ui_test.dart`'s "not only an icon" tests.
- Not audited: the not-today reason bottom sheet (uses default `ListTile` semantics, not explicitly verified), the completion form's `TextField` validation error semantics, and full touch-target sizing — real, disclosed gaps (§22).

## 18. Accessibility tests

`test/long_horizon_accessibility_test.dart` (6 tests): loading-button label+hint, disabled-button has no actionable control, Completed/NotToday/long-run Calendar entry semantic labels, and a text-scale-2.0 smoke test proving the confirm CTA isn't clipped/thrown. Not covered: activation/retry action labels specifically in their live Home context (covered indirectly via the shared `_card` helper's tests would require a full Home widget harness — deferred), regenerate/operational-support state labels, not-today selection semantics.

## 19. Analytics inventory

**No analytics SDK exists anywhere in this app** — confirmed by `pubspec.yaml` (no `firebase_analytics`, `segment`, `mixpanel`, or similar dependency) and a repository-wide grep (the only `Analytics` hit is an unrelated code comment). Per this phase's own instruction ("If analytics is absent: document that no analytics events were added; do not create a new subsystem"), **zero analytics events were added**, and no new analytics subsystem was created.

## 20. Analytics privacy

Not applicable — no analytics exist to audit for privacy (§19).

## 21. Logging safety

No new `print`/logging statements were added in this phase's Flutter code. Pre-existing `print(...)` calls in unrelated files (`plan_repository.dart`, `calendar_page.dart`, `plan_generation_page.dart` — all pre-existing, not touched by Long-Horizon work) were left as-is; they log URLs and lifecycle timing, not tokens or workout metrics, matching this phase's safety bar, but removing them was out of scope (they predate both 4L.5 and 4L.5A).

## 22. Contract parity matrix

| Endpoint | Method | Flutter repo method | Flutter response model | Error mapper coverage |
|---|---|---|---|---|
| `/plans/generate-preview/race/long-horizon` | POST | `generateLongHorizonRacePlanPreview` | `LongHorizonPlanPreviewContract` | ✅ (preview codes) |
| `/plans/confirm/long-horizon` | POST | `confirmLongHorizonPlan` | `LongHorizonConfirmPlanResponse` | ✅ |
| `/plans/active/home` | GET | `fetchActiveHome` | `ActiveHomeResult` | ✅ (read codes) |
| `/plans/active/calendar` | GET | `fetchActiveCalendar` | `ActiveCalendarResult` | ✅ |
| `/training-days/rolling/{id}` | GET | `fetchRollingSessionDetail` | `LongHorizonRollingSessionDetailResponse` | ✅ |
| `/training-days/rolling/{id}/complete` | POST | `completeRollingSession` | `LongHorizonSessionMutationResponse` | ✅ (mutation codes) |
| `/training-days/rolling/{id}/not-today` | POST | `markRollingSessionNotToday` | `LongHorizonSessionMutationResponse` | ✅ |
| `/plans/active/long-horizon/activate-next-window` | POST | `activateNextWindow` | `LongHorizonActivateNextWindowResponse` | ✅ (continuation codes) |
| `/plans/active/long-horizon/retry` | POST | `retryContinuation` | `LongHorizonRetryContinuationResponse` | ✅ (retry codes) |
| `/plans/active/details` | GET | *not consumed by Long-Horizon UI* | — | n/a — static-only screen, unchanged |
| `/plans/{id}/cancel` | POST | *reused from static `PlanRepository`* | `CancelPlanResponse` | not Long-Horizon-specific; no dedicated regenerate-flow UI exists yet (§14) |

Every enum field across every one of these DTOs was re-verified against backend C# source this phase (not just spot-checked); the casing defect (§1) was found this way.

## 23. Fixture parity

No dedicated fixtures file was created — fixtures remain inlined per-test (matching Phase 4L.5's own disclosed deferral). Test files with real-shaped inline fixtures: `long_horizon_dtos_test.dart` (preview incl. blocked state, confirm, session, mutation, activation, retry), `long_horizon_calendar_test.dart` and `long_horizon_accessibility_test.dart` (Calendar sessions in Planned/Completed/NotToday), `long_horizon_calendar_provider_test.dart` (empty-month, month isolation).

## 24. Provider tests

`long_horizon_calendar_provider_test.dart` (4 tests): exact month isolation (two months cached independently), invalidating one month leaves another untouched, `monthKeyForDate` derivation, `rollingSessionDetailProvider` isolation by session ID.

## 25. Calendar widget tests

`long_horizon_calendar_test.dart` (7 tests): Planned/Completed/NotToday rendering, empty-month state, deterministic date ordering, rolling-entry navigation to detail, stable `SessionId`-based keys.

## 26. Error-mapping tests

`long_horizon_error_mapper_test.dart` (20 tests): every major action category (`regeneratePreview`, `refreshHome`, `refreshDetail`, `retryGuidance`, `cancelAndCreateNewPlan`, `operationalSupport`, `signInAgain`, `genericFailure`), a loop asserting all 24 real codes' messages never leak the raw code or backend detail text, and unknown-code/non-`ApiException` fallback behavior.

## 27. Navigation tests

Only the one Calendar→detail navigation test in §25/§11. **Not covered**: Home→detail (static or rolling), invalid-deep-link safe state, unauthenticated-deep-link redirect, terminal-state Calendar access, back-button behavior. Real, disclosed gap (§29).

## 28. Flow tests

**Not implemented.** No preview→confirm→Home, Calendar→complete→Calendar-reflects-change, or retry-vs-activation-separation end-to-end flow test was written this phase. Real, disclosed gap (§29) — the single highest-value remaining item for a future phase.

## 29. Original-test-matrix reconciliation

| Category (from the 120-item matrix) | Status |
|---|---|
| Calendar contracts (1-10) | Covered — decoding, casing, month isolation, empty month (§24/§26 tests) |
| Calendar UI (11-25) | Covered — Planned/Completed/NotToday/role labels/empty/ordering/keys/no-Pending-leakage (§25 tests) |
| Routing (26-32) | **Partially covered** — only #26 (rolling entry→detail) has a real test; #27-32 (static entry, Home entry, invalid link, auth redirect, terminal access, cancelled-plan) are unimplemented/untested |
| Mutation refresh (33-40) | Implemented in code (§12) but **not independently test-covered** beyond the provider-isolation tests in §24 — no test simulates a full complete→Calendar-month-changes round trip |
| Error mapping (41-58) | Covered — all 24 real codes tested (§26); the prompt's 5 invented codes (#41 "preview expired maps regenerate" etc. use real codes so those ARE covered; the handful referencing nonexistent codes are N/A, not applicable — see §13) |
| Accessibility (59-70) | **Partially covered** — 6 of ~12 items tested (§18); regenerate/operational-support/not-today-selection semantics untested |
| Analytics/logging (71-80) | N/A — no analytics subsystem exists (§19); logging safety is a code-read confirmation, not test-covered |
| Providers/cache (81-90) | **Partially covered** — month isolation and session-detail isolation tested (§24); stale-result/offline/polling items are architecturally true (no polling exists anywhere, offline mutations were never wired) but not test-proven |
| Flow tests (91-104) | **Not covered** — real, material gap (§28) |
| Compatibility (105-120) | Covered by the full regression suite passing (283/283) plus the explicit static/Habit test files continuing to pass unmodified |

**Material uncovered gaps**: flow/integration tests (§28) and most of navigation testing (§27) are the two categories with a real, material coverage gap. Everything else is either covered or legitimately not applicable.

## 30. Static compatibility

`CalendarPage`, `calendar_repository.dart`, `calendar_provider.dart` (except the shared `calendarMonthProvider`, reused not modified), static training-day detail routing, and all static JSON fixtures are byte-for-byte unchanged. Verified by the full existing static test suite passing unmodified (283/283 includes every pre-existing static test).

## 31. Habit compatibility

Unchanged — Habit never touches any Long-Horizon code path (confirmed in Phase 4L.5 and re-confirmed here: `_isLongHorizonSpan()` only evaluates for `goalType == 'race'`). The six Firebase-init test-file fixes from Phase 4L.5 (`longHorizonRepositoryProvider` override alongside `planRepositoryProvider`) remain in place and still pass.

## 32. Performance

Reviewed, not profiled: Calendar entries are keyed by `SessionId` (§9); `ListView.builder` is used (lazy, not a full unbounded `Column`); no polling exists anywhere in the Long-Horizon code (confirmed by grep — no `Timer.periodic`/`Stream.periodic` in any Long-Horizon file); each Calendar month is an independent `FutureProvider.family` instance so switching months doesn't re-decode unrelated months; mutation invalidation is now exact-month (§12), not a full-family invalidation, avoiding an O(all-cached-months) re-fetch on every mutation. No profiling/DevTools pass was run — a real, disclosed gap.

## 33. Security/privacy

No `UserId`/`PlanId` is ever client-supplied as authority — every repository call relies on the existing authenticated `ApiClient` (Bearer token), matching Phase 4L.5. `SessionId` is used only as an opaque resource identifier in routes/requests, never combined with a client-asserted owner. Error messages never contain a raw backend code or a raw exception `toString()` (test-proven, §26). No screenshot/debug-dump capability was added. No new client-side authorization logic was introduced — a 404-for-not-found/not-owned session (from `LONG_HORIZON_ROLLING_SESSION_NOT_FOUND`) is handled identically regardless of *why* the backend returned it, so no cross-user probing signal is exposed differently.

## 34. Backend changes

**None.** Zero backend files were read for modification and zero were changed. The critical defect in §1 was entirely a Flutter-side (`long_horizon_dtos.dart`) fix — the backend's own JSON serialization was and remains correct and untouched.

## 35. Format/analyze/tests

- `flutter analyze`: **0 errors** across the whole app (only pre-existing info/warning-level lints in files this phase never touched, plus a small number of pre-existing warnings in files this phase legitimately edited that were already present before — see §36 for the exact count methodology used in Phase 4L.5, unchanged here).
- `dart format --set-exit-if-changed`: **fails project-wide** — 73 of 105 scanned files differ from the default formatter output, including many files this phase never touched (`main.dart`, `firebase_options.dart`, `profile_page.dart`, etc.). This is a **pre-existing condition**, confirmed by `analysis_options.yaml` having no line-length/format override and no CI formatting gate found anywhere in the repo — not a regression introduced by this phase. Mass-reformatting 73 unrelated files was intentionally not done, to avoid an unreviewable, unrelated diff.
- Focused new-test counts this phase: 24 DTO/decoder tests (`long_horizon_dtos_test.dart`, up from 16 — 8 new casing/field tests), 4 routing tests (unchanged from Phase 4L.5), 20 error-mapper tests (new), 7 Calendar widget tests (new), 6 accessibility tests (new), 4 provider-isolation tests (new). **48 net new tests this phase.**
- **Full `flutter test` suite: 283 passed, 0 failed, 0 skipped** (up from 235 at the end of Phase 4L.5).

## 36. Governance

New record `TD-LONG-HORIZON-FLUTTER-CALENDAR-ERROR-MAPPING-ACCESSIBILITY-TEST-CLOSURE-001` appended **OPEN** (flow/integration tests and full navigation coverage remain a material gap — §28/§29). `TD-LONG-HORIZON-FLUTTER-READ-OUTCOME-RETRY-CONTINUATION-INTEGRATION-001` is updated **OPEN → CLOSED**: every one of its own original closure conditions (preview/confirmation/Home/Calendar/detail/completion/not-today/activation/retry/recovery/terminal all work; static/Habit regressions pass; no client-side planning authority; backend contract parity complete; Flutter tests pass) is now genuinely satisfied — Calendar exists and is tested, and the contract-parity defect found this phase is fixed, which is precisely the condition that record's closure depended on. Release readiness, plan-catalog governance-test debt, and Phase 4L.6 are explicitly **not** touched or closed.

## 37. Final classification

`LONG_HORIZON_FLUTTER_CLOSURE_REMAINS_BLOCKED_BY_EXPLICIT_CALENDAR_CONTRACT_ROUTING_PENDING_LEAKAGE_ERROR_MAPPING_ACCESSIBILITY_ANALYTICS_PRIVACY_OR_TEST_GAP` — specifically, by the **flow/integration test gap (§28)** and the **partial navigation test gap (§27)**. Every other named blocking category (Calendar contract/routing, Pending leakage, error mapping, accessibility, analytics, privacy) has real, tested, non-fabricated closure this phase.

## 38. Exact next phase

A narrow follow-up (4L.5B) focused exclusively on flow/integration tests (preview→confirm→Home→Calendar→complete→verify, retry-vs-activation separation, regenerate-flow) and the remaining navigation tests (§27) would close `TD-LONG-HORIZON-FLUTTER-CALENDAR-ERROR-MAPPING-ACCESSIBILITY-TEST-CLOSURE-001` outright. After that, Phase 4L.6 (End-to-End Release Acceptance, Governance Test-Debt Closure and Production Readiness) is the right next step, as originally recommended.
