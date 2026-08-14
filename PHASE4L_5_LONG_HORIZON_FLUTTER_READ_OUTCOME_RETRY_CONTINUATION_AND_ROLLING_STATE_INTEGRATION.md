# Phase 4L.5 — Long-Horizon Flutter Read, Workout Outcome, Retry, Continuation and Rolling-State Integration

**Date:** 2026-08-06
**Status:** PARTIAL — a real, working, tested vertical slice is complete and merged; several explicitly-scoped items are deferred (see §12). Governance record `TD-LONG-HORIZON-FLUTTER-READ-OUTCOME-RETRY-CONTINUATION-INTEGRATION-001` is recorded **OPEN**, per this phase's own instruction not to overclose.

---

## 1. Objective

Integrate the Flutter mobile app with the now-complete Long-Horizon (21-52 week RollingLongHorizon) backend lifecycle proven in Phases 4L.1-4L.4F, **without changing any backend planning, checkpoint, recovery, or activation authority**. The backend remains the sole source of truth for every schedule, phase, checkpoint-readiness, recovery-requirement, and activation/retry decision; the Flutter app only renders what the backend returns and submits explicit, user-initiated mutations/actions.

## 2. What "Long-Horizon" means to the mobile app

A race plan whose start-to-race span exceeds 20 weeks. The existing 8-20 week static route (`GeneratePreviewResponse`, `HomeResponse`, flat training-day list) is completely unmodified. A Long-Horizon plan is discriminated end-to-end by a `schedule_strategy` field (`static_complete` | `rolling_long_horizon`) returned by the shared `GET /plans/active/home` and `GET /plans/active/calendar` endpoints — the client never infers strategy from payload shape, only from this field.

## 3. Architecture decision

**Additive parallel vertical slice**, not in-place modification of the existing static screens. `HomePage` (2620 lines) and the rest of the static flow are byte-for-byte unchanged. Instead:

- A new `lib/core/network/long_horizon_dtos.dart` holds every Long-Horizon wire model, decoded with this codebase's existing manual-`fromJson`/snake_case/`fromWire`-enum-with-`unknown`-fallback convention (matching `core/models/preparation_runway.dart`).
- A new `LongHorizonRepository` (`features/plan/data/long_horizon_repository.dart`) is a thin, honest HTTP pass-through — no method computes a schedule, checkpoint, recovery decision, or activation decision.
- New Riverpod providers (`features/plan/data/long_horizon_provider.dart`) follow the existing per-feature `data/` provider file convention and the existing app-wide manual-invalidation-set pattern (`invalidateLongHorizonState` mirrors the `ref.invalidate(homeDataProvider); ref.invalidate(calendarDataProvider); ...` pattern already used in `home_page.dart`/`calendar_page.dart`/`settings_page.dart`).
- A thin **route-level dispatcher**, `ActiveHomeDispatcherPage`, is installed at `AppRoutes.home` in place of `HomePage`. It reads `schedule_strategy` from `GET /plans/active/home` and renders either the untouched `HomePage` or the new `LongHorizonHomePage` — the only change to the existing static path is this one indirection.

## 4. What was built (real, tested, working)

### 4.1 Models — `lib/core/network/long_horizon_dtos.dart`
Every backend contract from `LongHorizonPublicPlanContracts.cs`, `LongHorizonPublicPreviewContracts.cs`, and `LongHorizonActiveReadContracts.cs` is ported field-for-field, verified against the actual C# source (not from memory) immediately before writing: preview (`LongHorizonPlanPreviewContract` + structural roadmap + executable weeks/sessions), confirmation, Home, Calendar, session detail, completion/not-today mutation, activation, and retry. `PlanScheduleStrategy`, `WorkoutRole` (`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN`), `LongHorizonCheckpointReadiness`, and `LongHorizonRecoveryRequirement` all fail closed to an explicit `unknown` member rather than throwing on an unrecognized wire value.

### 4.2 Repository — `lib/features/plan/data/long_horizon_repository.dart`
Wraps every real endpoint: `POST /plans/generate-preview/race/long-horizon`, `POST /plans/confirm/long-horizon`, `GET /plans/active/home`, `GET /plans/active/calendar`, `GET /training-days/rolling/{id}`, `POST /training-days/rolling/{id}/complete`, `POST /training-days/rolling/{id}/not-today`, `POST /plans/active/long-horizon/activate-next-window`, `POST /plans/active/long-horizon/retry`. Routes verified directly against `PlansController.cs`/`TrainingDaysController.cs` route attributes, not assumed.

### 4.3 Preview generation routing — `lib/features/onboarding/data/onboarding_provider.dart`
`OnboardingNotifier.generatePreview()` now computes the start-to-race span client-side and routes to the Long-Horizon endpoint when it exceeds 20 weeks, otherwise to the existing static endpoint. This is a **routing decision only** (which endpoint to call) — the backend alone decides the resulting preview's shape and readiness. `OnboardingState` gained a parallel `longHorizonPreviewResponse` field; exactly one of `previewResponse`/`longHorizonPreviewResponse` is ever populated by a single `generatePreview()` call (verified by test, §6). Habit goals never route to Long-Horizon (no race date exists to measure a span against).

### 4.4 Preview screen — `lib/features/onboarding/presentation/long_horizon_plan_preview_page.dart`
New screen at `AppRoutes.longHorizonPlanPreview`. Renders only what the backend returned: the current executable window in full session-level detail, and the remaining structural roadmap as locked, summary-only rows (`publicSummary`, no fabricated sessions) for weeks where `numericDetailsAvailable` is false. The confirm CTA is gated exclusively on `LongHorizonPlanPreviewContract.isConfirmable` (from `confirmation_readiness`), matching the existing static preview page's own lifecycle-gating pattern.

### 4.5 Confirmation
`_onConfirm` calls `LongHorizonRepository.confirmLongHorizonPlan`, then invalidates every plan-state provider (bootstrap/home/calendar/profile/active-home/active-calendar) and resets onboarding state — mirroring the existing static `plan_preview_page.dart`'s own confirm handler exactly.

### 4.6 Home — `lib/features/home/presentation/long_horizon_home_page.dart` + `active_home_dispatcher_page.dart`
Renders the active plan summary, today's workout (if any), and the current window's session list, each tappable through to the rolling detail page. A `_ReadinessCard` surfaces an explicit **Activate next block** button only when `checkpointReadiness == nextWindowActivationReady`, and an explicit **Retry** button only when `recoveryRequirement == calendarWindowPending` — every other readiness/recovery state (in-progress, complete-but-not-ready, terminal, regenerate-required, operational-support-required) is informational only, with no button that could imply an action the backend hasn't actually allowed. Neither action is ever called automatically — both require a tap.

### 4.7 Rolling session detail — `lib/features/training_day/presentation/rolling_session_detail_page.dart`
New route `/training-day/rolling/:sessionId`. Shows the session and lets the user record a single, explicit completion (distance + duration) or not-today (one of the six backend-approved reason tokens — `fatigue`/`soreness`/`illness`/`schedule`/`weather`/`other`, verified against `LongHorizonRollingSessionMutationService.AllowedNotTodayReasons`). Neither mutation ever chains into activation; the readiness card that might now show "Activate next block" only appears after the user separately navigates back to Home and taps it themselves.

### 4.8 Calendar
Decoding support only: `ActiveCalendarResult`/`LongHorizonCalendarResponse` in the DTOs file, and `activeCalendarResultProvider` in the providers file. **No dedicated Long-Horizon calendar UI was built this phase** — see §12.

## 5. What was explicitly NOT done (by design, matching the prompt's own prohibitions)

- No client-side reproduction of any planning, checkpoint, recovery, or activation formula.
- No inference of future workouts beyond `currentExecutableWeeks`/`current_window_sessions`.
- No synthesized Pending sessions — Pending weeks render `publicSummary` text only.
- No automatic activation after a completion/not-today mutation.
- No automatic retry.
- No background activation of any kind.
- Zero backend files modified in this phase.
- Zero commits made by the assistant during this phase (per explicit instruction).
- Static/Habit flow: `HomePage`, `CalendarPage`, `PlanDetailsPage`, the static `plan_preview_page.dart`, and every static provider are byte-for-byte unchanged. Regression-verified by the full existing Flutter test suite (§6).

## 6. Testing performed

- `flutter analyze`: zero errors across the whole app; the only new-code finding (one unused import) was fixed. All remaining warnings/infos are pre-existing and unrelated to this phase.
- New test files:
  - `test/long_horizon_dtos_test.dart` (16 tests) — decodes real-shaped fixture JSON for the preview, confirm, Home/`ActiveHomeResult`, session-mutation, activation, and retry contracts; verifies enum fail-closed behavior (`PlanScheduleStrategy`, `WorkoutRole`); verifies every `NotTodayReason` wire value is one of the six backend-approved tokens.
  - `test/long_horizon_preview_routing_test.dart` (4 tests) — verifies a ≤20-week race calls the static repository and never the Long-Horizon one; a >20-week race calls Long-Horizon and never static; exactly one of `previewResponse`/`longHorizonPreviewResponse` is ever populated (including after switching a race from long back to short); habit goals never route to Long-Horizon.
- **Regression found and fixed during this phase**: `onboardingProvider` now also depends on the new `longHorizonRepositoryProvider`. Six existing test files that override `planRepositoryProvider` (but not `longHorizonRepositoryProvider`) started failing with `[core/no-app] No Firebase App '[DEFAULT]' has been created`, because the un-overridden `longHorizonRepositoryProvider` fell through to a real `ApiClient()` → `apiClientProvider` → `firebaseAuthRepositoryProvider` → `FirebaseAuth.instance` chain that widget tests never initialize. Fixed by adding a shared `test/support/noop_long_horizon_repository.dart` and overriding `longHorizonRepositoryProvider` alongside `planRepositoryProvider` in `generate_preview_request_contract_test.dart`, `plan_generation_horizon_error_test.dart`, `onboarding_confirm_cleanup_test.dart`, `preparation_runway_schedule_ui_test.dart`, and `preparation_runway_preview_test.dart`.
- **Full suite result after the fix**: `flutter test` — **235 passed, 0 failed** (218 pre-existing + 17 new). No regressions in any static/Habit/Preparation-Runway test.

## 7. Contract-parity spot checks (condensed, not the full 109-item matrix — see §12)

| Backend field (verified from C# source) | Dart field | Verified |
|---|---|---|
| `LongHorizonConfirmPlanResponse.Outcome` (`Confirmed`/`AlreadyConfirmed`) | `LongHorizonConfirmationOutcome` | ✅ |
| `LongHorizonPlanPreviewContract.ConfirmationReadiness` | `LongHorizonConfirmationReadiness.isConfirmable` | ✅ |
| `LongHorizonExecutableSessionContract.SessionRole` (`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN`) | `WorkoutRole.fromWire` | ✅ |
| `LongHorizonNotTodayRequest.Reason` allowed set (`AllowedNotTodayReasons`) | `NotTodayReason.wireValue` | ✅ (test-verified) |
| `LongHorizonActivateNextWindowRequest`/`RetryContinuationRequest` — `ContractVersion` only, no plan ID (single active plan inferred server-side from the authenticated user) | Repository sends `{'contract_version': 1}` with no plan ID | ✅ |
| Route: `POST /plans/generate-preview/race/long-horizon` accepts the **same** `GenerateRacePlanPreviewRequest` shape as the static race route | Reuses the existing `GenerateRacePlanPreviewRequestDto` | ✅ |

## 8. Governance

`TD-LONG-HORIZON-FLUTTER-READ-OUTCOME-RETRY-CONTINUATION-INTEGRATION-001` appended to `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`, **status OPEN**. Not closed, because Calendar UI, exhaustive error-code mapping, accessibility pass, analytics, and the full 109-item test matrix remain outstanding (§12) — closure requires all of those per the phase's own closure conditions. No existing CLOSED backend governance record's conclusion was altered; this is a new, additive, append-only entry.

## 9. Files added

- `mobile/lib/core/network/long_horizon_dtos.dart`
- `mobile/lib/features/plan/data/long_horizon_repository.dart`
- `mobile/lib/features/plan/data/long_horizon_provider.dart`
- `mobile/lib/features/onboarding/presentation/long_horizon_plan_preview_page.dart`
- `mobile/lib/features/home/presentation/long_horizon_home_page.dart`
- `mobile/lib/features/home/presentation/active_home_dispatcher_page.dart`
- `mobile/lib/features/training_day/presentation/rolling_session_detail_page.dart`
- `mobile/test/long_horizon_dtos_test.dart`
- `mobile/test/long_horizon_preview_routing_test.dart`
- `mobile/test/support/noop_long_horizon_repository.dart`

## 10. Files modified

- `mobile/lib/features/onboarding/data/onboarding_provider.dart` — added Long-Horizon routing (`_isLongHorizonSpan`, parallel `longHorizonPreviewResponse` field, `LongHorizonRepository` dependency).
- `mobile/lib/features/onboarding/presentation/plan_generation_page.dart` — updated the (currently dead, test-shortcut-bypassed) commented real-flow block to route to the correct preview screen based on `isLongHorizonPreview`. **No behavior change** — the mock-data test shortcut this file actually executes today is untouched.
- `mobile/lib/core/routing/app_router.dart` — added `AppRoutes.longHorizonPlanPreview` and `AppRoutes.rollingSessionDetail` routes; `AppRoutes.home` now points at `ActiveHomeDispatcherPage` instead of `HomePage` directly.
- Six existing test files — added `longHorizonRepositoryProvider` override (see §6).

## 11. Verified backend readiness records (append-only note, no conclusions changed)

`activation-readiness-risks.json`'s existing CLOSED Long-Horizon backend entries (`TD-LONG-HORIZON-EXPLICIT-NEXT-WINDOW-ACTIVATION-API-001`, `TD-LONG-HORIZON-PUBLIC-RETRY-ACTIVATION-SHAPE-RACE-COMPLETION-001`, `TD-LONG-HORIZON-PUBLIC-ACTIVATION-SHAPE-JIT-RACE-COMPLETION-001`, `TD-LONG-HORIZON-PUBLIC-LIFECYCLE-SHAPE-REPLAY-TERMINAL-CONCURRENCY-MATRIX-001`) are now confirmed **consumed by a real Flutter client** for the reachable lifecycle shapes exercised by this phase's tests (preview generation, confirmation, Home read, session completion/not-today, explicit activation, explicit retry). Their CLOSED status and original conclusions are unchanged.

## 12. Explicitly deferred (disclosed, not silently omitted)

- **Calendar UI**: decoding models/provider exist; no dedicated Long-Horizon calendar screen was built.
- **Error-code mapping**: `plan_generation_error_mapper.dart`'s two stale precursor codes (`PLAN_HORIZON_COMPOSITION_REQUIRED`, `PLAN_CORE_HORIZON_UNSUPPORTED`) were not revisited; no new Long-Horizon-specific error codes were added to the mapper (the generic `ApiException.message` fallback is shown instead).
- **Accessibility pass**: no semantics-label audit was performed on the new screens.
- **Analytics/logging events**: none added.
- **Full 109-item test matrix**: 20 targeted tests were written (16 decoder + 4 routing) covering the highest-risk surfaces (contract shape fidelity, fail-closed enum decoding, endpoint routing correctness, exactly-one-preview-field invariant). No widget-level tests were written for the new screens themselves.
- **JSON fixtures file**: fixtures are inlined in the test files rather than extracted to a shared fixtures module.
- **Performance optimization pass**: not performed.
- **Recommended next step**: a follow-up phase (4L.5B or the originally-recommended 4L.6) should build the Calendar UI and widget-level tests for `LongHorizonHomePage`/`RollingSessionDetailPage`/`LongHorizonPlanPreviewPage` before this governance record can close.
