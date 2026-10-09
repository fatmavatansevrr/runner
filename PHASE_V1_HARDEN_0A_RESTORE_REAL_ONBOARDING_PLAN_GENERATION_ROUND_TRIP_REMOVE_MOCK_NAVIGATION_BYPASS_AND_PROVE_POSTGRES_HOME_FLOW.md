# PHASE V1-HARDEN.0A — Restore real onboarding plan-generation round trip, remove mock navigation bypass, and prove preview→confirm→Postgres→Home flow

## 1. V1-HARDEN.0 binding finding

V1-HARDEN.0 (`PHASE_V1_HARDEN_0_END_TO_END_LAUNCH_READINESS_AUDIT_...md`, §4/§81) identified **`V1HARDEN0-001`**, P0, in `mobile/lib/features/onboarding/presentation/plan_generation_page.dart`. At the time of that audit, `_startGeneration()` (lines ~80–117) had the real flow fully written out as a comment directly above the active code, and the active code unconditionally did:

```dart
if (mounted) {
  setState(() => _completedSteps = 4);
  await Future.delayed(const Duration(milliseconds: 300));
  if (mounted) {
    ref.read(useMockHomeDataProvider.notifier).state = true;
    context.go(AppRoutes.home);
  }
}
```

— i.e. it never called `generatePreview()`, never reached the real preview/confirm screens, and sent every onboarding user straight to a hardcoded mock Home (`useMockHomeDataProvider` / `buildMockHomeResponse` in `features/home/data/home_provider.dart` and `mock_home_data.dart`). This was found to be present since at least commit `1b07cf8` (predating the entire DERIVED-DIST/FIVE-K distance-engine chain), with no debug gate and no environment check — it ran identically in every build configuration.

This phase independently re-read the file in full and confirmed the finding exactly as described, byte-for-byte, before making any change.

## 2. Severity classification

Per this phase's own classification discipline: no evidence of security exposure or irreversible data corruption was found beyond the bypass itself (no cross-user leak, no corrupted persisted state — the bypass simply never reaches the backend at all). Classified as:

**`P1_CRITICAL_ONBOARDING_BACKEND_BYPASS`**

(V1-HARDEN.0's own taxonomy labeled it P0 because it independently blocks the entire launch; this phase does not dispute that launch-blocking severity, but per this phase's own narrower rubric — reserving P0 for security/irreversible-corruption findings — the correct label for the defect's *nature* is P1_CRITICAL. It remains launch-blocking regardless of label.)

## 3. Existing onboarding graph

Traced the full race/habit onboarding chain (Riverpod `StateNotifierProvider`-based, `onboardingProvider` / `OnboardingNotifier` in `mobile/lib/features/onboarding/data/onboarding_provider.dart`):

Goal/distance selection (`goal_selection_page.dart`, `custom_goal_page.dart`, `custom_goal_with_time_page.dart`, `habit_goal_page.dart`) → experience (`running_background_page.dart`, `runner_background_details_page.dart`, `recent_race_result_page.dart`) → weekly frequency (`weekly_frequency_page.dart`) → long-run-day preference, race-only (`long_run_day_preference_page.dart`) → preferred running days (`running_days_selection_page.dart`) → preferred run duration, habit-only (`preferred_run_duration_page.dart`) → start date (`start_date_selection_page.dart`, which is the single funnel point: both race and habit paths call `context.go(AppRoutes.planGeneration)` from here) → **`PlanGenerationPage`** (the defect site) → `PlanPreviewPage` (static) or `LongHorizonPlanPreviewPage` (long-horizon) → Confirm → Home.

Every input screen writes into `OnboardingState` via `OnboardingNotifier` setters (`updateGoalType`, `updateGoalDistance`, `updateTargetDistanceKm`, `updateRunningBackground`, `updateDaysPerWeek`, `updateRaceDetails`, `setProductAverageTarget`/`setUserDefinedTarget`, `updateLongRunDay`, `updateStartDate`, `updateSelectedRunningDays`, `updatePreferredRunDuration`, `updateRecentWeeklyVolumeKm`, `updateRecentLongestRunKm`, `updateRecentRaceResult`). `OnboardingNotifier.generatePreview()` (already fully implemented, untouched by this phase) builds the request from this state, branches race vs. habit and static vs. long-horizon, and calls the real repository. This infrastructure was already correct and complete before this phase started — the only broken link was `PlanGenerationPage` never calling it.

## 4. Mock bypass root cause

Confirmed root cause: `PlanGenerationPage._startGeneration()` had a "TEST SHORTCUT" code path substituted for the real call, with the real call preserved only as a comment. No debug/environment gate existed. `useMockHomeDataProvider` (declared in `home_provider.dart`) was flipped to `true` unconditionally, and `ActiveHomeDispatcherPage` (the route-level widget behind `AppRoutes.home`) reads that flag first and renders the static mock-populated `HomePage` if it is set — bypassing the real `activeHomeResultProvider`/`/plans/active/home` read entirely.

## 5. Sibling bypass search

Searched the full onboarding and home-presentation trees for the same defect family (`mock`, `TODO`, `hardcode`, `temporary`, `TEST SHORTCUT`, `bypass`) and for every other write-site of `useMockHomeDataProvider`. Result: **no second bypass found**. The only other reference to `useMockHomeDataProvider` is its declaration (`home_provider.dart`, default `false`) and its read in `active_home_dispatcher_page.dart` (a legitimate, narrowly-scoped dev/test seam — not reachable from onboarding, and never set to `true` by any production code path after this fix). Confirmed both race and habit onboarding funnel through the identical `start_date_selection_page.dart` → `AppRoutes.planGeneration` → `PlanGenerationPage` path — there is no separate habit-specific generation screen that could hide a second bypass.

## 6. Real preview API contract

`PlanRepository` (`mobile/lib/features/plan/data/plan_repository.dart`):
- `POST /plans/generate-preview/race` — `generateRacePlanPreview(GenerateRacePlanPreviewRequestDto)` → `GeneratePreviewResponse`
- `POST /plans/generate-preview/habit` — `generateHabitPlanPreview(GenerateHabitPlanPreviewRequestDto)` → `GeneratePreviewResponse`

`LongHorizonRepository` (`mobile/lib/features/plan/data/long_horizon_repository.dart`):
- `POST /plans/generate-preview/race/long-horizon` — `generateLongHorizonRacePlanPreview(GenerateRacePlanPreviewRequestDto)` → `LongHorizonPlanPreviewContract` (reuses the identical static request shape; the backend alone decides which schedule strategy the response uses)

Routing between static and long-horizon is a pure client-side **endpoint-selection** decision (`OnboardingNotifier._isLongHorizonSpan()`, threshold 20 weeks) — it never computes a schedule; the backend is the sole authority over the resulting content, exactly as the existing code's own doc comments state. This phase did not touch this logic.

## 7. Real confirm API contract

- `POST /plans/confirm` — `PlanRepository.confirmPlan(previewId)`, body `{ "previewId": <guid> }` → `ConfirmPlanResponse { planId, status }`
- `POST /plans/confirm/long-horizon` — `LongHorizonRepository.confirmLongHorizonPlan(previewId)`, body `{ "preview_id": ..., "contract_version": 1 }` → `LongHorizonConfirmPlanResponse`

Backend: `backend/RunningApp.Application/DTOs/Plan/ConfirmPlanRequest.cs` (`Guid PreviewId`), `backend/RunningApp.Api/Controllers/PlansController.cs` (`[Route("api/v1/plans")]`). Both confirm calls were already correctly wired in `plan_preview_page.dart`/`long_horizon_plan_preview_page.dart` before this phase — they were simply unreachable because `PlanGenerationPage` never navigated to either preview screen.

## 8. Onboarding input mapping

`OnboardingNotifier._buildRacePreviewRequest()`/`_generateHabitPreview()` (pre-existing, unchanged by this phase) map every collected field: `goalType`, `goalDistance`, `targetDistanceKm` (custom-only), `runningBackground.wireValue` → `level`, `daysPerWeek`, `unit`, `startDate` (formatted `yyyy-MM-dd`), `selectedRunningDays` → `preferredDays` (3-letter lowercase tokens), `longRunDay` (race-only, null for habit — never a stale default), `raceName`/`raceDate` (race-only), `targetFinishTimeSeconds`+`targetFinishTimeSource` (set atomically, race-only, required positive), `recentWeeklyVolumeKm`/`recentLongestRunKm`/`recentRaceResult` (all null for Beginner, explicit null-vs-zero semantics preserved for non-Beginner). No silent defaults are introduced anywhere in this mapping; this phase verified but did not modify it.

## 9. Exact 5K request mapping

`goalDistance = 'five_k'` is sent as the canonical enum value — never converted to a generic custom-distance request. `targetDistanceKm` stays `null` for every canonical preset, including exact 5K. Confirmed via `OnboardingState.targetDistanceKm`'s own doc comment and `race_details_page.dart`'s tight-exact-match gate (unmodified).

## 10. Custom-distance mapping

5K<target<10K and 10K<target<HM custom targets set `goalDistance = 'custom'` and a non-null `targetDistanceKm` carrying the exact typed value (no rounding/preset snapping) — `race_details_page.dart`'s existing admission logic (DERIVED-DIST.4A / FIVE-K.1, untouched) is the sole gate for which exact values are accepted; the onboarding provider only forwards whatever it is given.

## 11. Exact 10K mapping

`goalDistance = 'ten_k'`, `targetDistanceKm = null` — same canonical-identity treatment as exact 5K. Verified no code path coerces an exact-10K selection into the generic custom branch.

## 12. Exact HM mapping

`goalDistance = 'half_marathon'`, `targetDistanceKm = null` — same treatment. The repository-side exact-HM canonical distance constant (owned by `race_details_page.dart`/backend `GoalDistance.HalfMarathon`) is unchanged by this phase.

## 13. Public-matrix preservation

This phase changed zero backend code and zero catalog/eligibility logic. The public FIVE_K (Intermediate 4D/5D only), TEN_K, derived-interval (6-9K, 11-20K), and HM matrices established by FIVE-K.1/DERIVED-DIST.4A are verified unchanged — `git diff` confirms no file under `backend/`, `plan-catalog/`, or the catalog artifacts was touched (see §58).

## 14. Preview implementation

`PlanGenerationPage._startGeneration()` now calls `await ref.read(onboardingProvider.notifier).generatePreview()` inside a `try`, instead of the removed mock block. This is the exact code the file's own prior comment specified, restored verbatim (see diff in §4). No new request-shape logic, no new client-side schedule computation was added.

## 15. Preview loading state

Unchanged visual loading UI (animated step checklist / circular progress) now genuinely tracks a real in-flight request rather than a fixed-timer animation that always "succeeds." The generate action cannot run twice concurrently from the same page instance — `_startGeneration()` is invoked once per page build (`initState`) and only re-invoked by the user's own "Try Again" tap on the error screen, which fully replaces the error UI with the loading UI first (standard `setState`-based StatefulWidget re-render, no second overlapping call can be triggered from the UI while the loading screen has no interactive controls).

## 16. Preview success state

On success, `onboardingProvider`'s `previewResponse` or `longHorizonPreviewResponse` (populated by the real backend response — exactly one, never both, per `generatePreview()`'s own existing contract) determines the next route: `isLongHorizonPreview` picks `AppRoutes.longHorizonPlanPreview`, otherwise `AppRoutes.planPreview`. **Never Home.** Verified by the new discriminating test (`plan_generation_page_navigation_test.dart`, tests 1–2).

## 17. Preview failure state

On any thrown error, `_error` is set from `planGenerationUserSafeMessage(e)` (pre-existing, unchanged mapper) and the error UI (`_buildError()`, pre-existing) is shown — "Generation Failed" plus the safe mapped copy and a "Try Again" button. No navigation occurs anywhere on this path (verified — test 3). Onboarding state is untouched on failure (confirmed via the existing `generatePreview()` error-path semantics, which never calls `copyWith`/`state=` on a thrown error).

## 18. Confirm implementation

Unchanged — `plan_preview_page.dart`'s `_onConfirm()` and `long_horizon_plan_preview_page.dart`'s `_onConfirm()` were already correctly implemented (real `confirmPlan`/`confirmLongHorizonPlan` calls, `_isConfirming` guard against duplicate taps, provider invalidation of `bootstrapDataProvider`/`homeDataProvider`/`calendarDataProvider`/`profileOverviewProvider`/`activePlanDetailsProvider`, `onboardingProvider.notifier.reset()` only on success). This phase did not modify either file. It was simply unreachable before this phase's fix because `PlanGenerationPage` never got the user there.

## 19. Confirm success state

Unchanged: `context.go(AppRoutes.home)` only after `await repo.confirmPlan(previewId)` / `confirmLongHorizonPlan(previewId)` completes without throwing, and only after invalidating every plan-derived provider and resetting onboarding state. Already covered by `onboarding_confirm_cleanup_test.dart` (pre-existing, still passing — see §55).

## 20. Confirm failure state

Unchanged: on a thrown exception, a `SnackBar` shows a message, `_isConfirming` resets, no navigation, onboarding state fully preserved for retry. Already covered by the same pre-existing test file.

## 21. Home navigation gate

The sole gate to `AppRoutes.home` from the onboarding graph is now the two Confirm pages' own success paths. `PlanGenerationPage` itself can only reach `planPreview`/`longHorizonPlanPreview` (success) or stay on itself (failure) — it has no code path to Home at all after this fix (the import of `home_provider.dart` and the `useMockHomeDataProvider` write were both removed).

## 22. Real Home data source

`ActiveHomeDispatcherPage` (route target of `AppRoutes.home`) reads `activeHomeResultProvider`, which calls `GET /plans/active/home` through `LongHorizonRepository.fetchActiveHome()`/the static home provider — real backend-sourced data. `useMockHomeDataProvider` defaults to `false` and, after this fix, nothing in production ever sets it `true`, so the dispatcher always takes the real-data branch in production.

## 23. Mock-path removal

The exact bypass block (unconditional `useMockHomeDataProvider.notifier.state = true; context.go(AppRoutes.home);`) was deleted from `plan_generation_page.dart`. The mock infrastructure itself (`useMockHomeDataProvider`, `mock_home_data.dart`, the dispatcher's test-shortcut branch) was deliberately left in place — it is dev/test-only, defaults to `false`, and after this fix has no production writer anywhere in the tree (confirmed by `grep -rn "useMockHomeDataProvider"` across `mobile/lib`: only the declaration and the dispatcher's read remain).

## 24. No mock fallback

Confirmed no `try { generatePreview() } catch { show mock }` or `try { confirm() } catch { go to mock Home }` pattern exists anywhere — the only `catch` blocks in the restored path set `_error`/show a `SnackBar`, never fabricate success.

## 25. State invalidation

`generatePreview()` always calls `state.copyWith(previewResponse: ..., clearLongHorizonPreviewResponse: true)` or the reverse — exactly one of the two preview fields is ever populated per call, so a second `generatePreview()` call (e.g. after "Try Again" or after going back and changing inputs) fully replaces the prior preview; there is no code path that could confirm a stale preview object once a new one has been generated, because the Preview page reads `previewResponse`/`longHorizonPreviewResponse` live from `onboardingProvider`, not a locally cached copy.

## 26. Back-navigation behavior

`PlanPreviewPage`'s back arrow calls `context.go(AppRoutes.startDate)`, which re-enters the onboarding input chain. Any subsequent "Continue" press re-runs `start_date_selection_page.dart`'s `context.go(AppRoutes.planGeneration)` → a fresh `PlanGenerationPage` instance → a fresh `generatePreview()` call with whatever the (possibly-changed) `onboardingProvider` state now holds. Confirmed by this phase's own test design (the generated preview always reflects the *current* state at `generatePreview()` call time).

## 27. Stale preview prevention

No separate caching layer exists that could preserve an old preview after a new one is generated — `previewResponse`/`longHorizonPreviewResponse` are the only two fields, always both reassigned atomically relative to each other on every `generatePreview()` call (§25). `PreviewLifecycle.unknown` fails closed (confirm CTA disabled) whenever no preview exists. **`ONBOARDING_STALE_PREVIEW_CONFIRMATION_PREVENTED`** (see §65).

## 28. Preview double-tap

`PlanGenerationPage` has no user-interactive control during the loading state (no button) — there is nothing for a user to double-tap to trigger a second concurrent `generatePreview()` call from the same screen instance. The only re-trigger is the "Try Again" button on the *error* screen, which is a deliberate single retry, not a concurrency hazard (not gated by a separate in-flight boolean because the error state is exclusive with the loading state by construction — `_error` is only set once a prior call has already completed/failed).

## 29. Confirm double-tap frontend behavior

Unchanged, pre-existing: `_isConfirming` (both preview-confirmation pages) blocks a second tap while a confirm call is in flight; the CTA also shows a loading spinner (`isLoading: _isConfirming`).

## 30. Active-plan conflict behavior

Unchanged: a `confirmPlan`/`confirmLongHorizonPlan` failure (including a conflict the backend reports, e.g. an already-active plan) surfaces through the existing failure path (`SnackBar`, no navigation) — the frontend never pretends a second plan was created. This phase did not add or alter any conflict-specific UI; backend DB-level uniqueness enforcement is unchanged (see V1-HARDEN.0 §11/§12, not reopened).

## 31. Auth/user context

Unchanged. Confirmed no `mock-user-001`/hardcoded identity is introduced anywhere in the restored path — `ApiClient` (used by every repository) attaches the real Firebase-resolved auth token; the backend resolves `InternalUserId` server-side exactly as before.

## 32. API configuration

Unchanged. `ApiClient.resolveBaseUrl()` (existing environment-driven configuration) is the sole source of the base URL — no hardcoded localhost/dev URL was added to `plan_generation_page.dart`.

## 33. Error propagation

Unchanged, pre-existing `planGenerationUserSafeMessage()` mapper — never shows raw `ApiException.message`/error code/correlation ID for the typed codes it maps, falls back to `error.message` only for untyped `ApiException`s (a pre-existing, out-of-scope-for-this-phase P2 per V1-HARDEN.0 §(error-leak finding) — not touched or expanded here). Verified via the new test's explicit assertion that `PRODUCT_INELIGIBLE`/`corr-nav-test` never appear in rendered text for a *typed* error.

## 34–39. Representative real-flow proofs (5K I4D/I5D, 7K, 10K, 16K, exact HM)

**Methodology used (see §46 for the full honesty statement):** this phase proves the restored *frontend gate* — that `PlanGenerationPage` genuinely calls the real repository, routes correctly on success, and never fabricates success on failure — via Flutter widget tests against fake repository doubles (`plan_generation_page_navigation_test.dart`), and relies on FIVE-K.1's own pre-existing, already-green `FiveK1PublicActivationEndToEndTests.cs`-style backend E2E tests for the real-PostgreSQL leg of 5K I4D/I5D, 7K-derived, canonical 10K, 16K-derived, and exact-HM, since this phase changed zero backend code and the backend side of that round trip was already independently proven by FIVE-K.1/DERIVED-DIST.4A and reconfirmed zero-delta by this phase's own backend regression run (§56). The genuinely *new* risk this phase closes is exclusively on the frontend side — whether the UI ever calls the backend at all — and that is what the new tests target directly, per distance class:

- **5K I4D / 5K I5D**: `OnboardingState.goalDistance = 'five_k'`, `targetDistanceKm = null` → `generateRacePlanPreview` → (per FIVE-K.1) canonical `FIVE_K` route, confirmable only for Intermediate {4D, 5D} — unchanged public matrix (§13).
- **7K** (5K<target<10K): `goalDistance = 'custom'`, `targetDistanceKm = 7.0` → TEN_K-derived route per DERIVED-DIST.4A (unchanged).
- **10K canonical**: `goalDistance = 'ten_k'`, `targetDistanceKm = null` → canonical TEN_K (verified no accidental custom-path conversion — §11).
- **16K** (10K<target<HM): `goalDistance = 'custom'`, `targetDistanceKm = 16.0` → HALF_MARATHON-derived (unchanged).
- **exact HM**: `goalDistance = 'half_marathon'`, `targetDistanceKm = null` → canonical HM (§12).

All five are now **UI-reachable** (the §0 defect is what made them unreachable, not anything about the five routes themselves, which were already correct). The new widget test (`plan_generation_page_navigation_test.dart`) directly proves, for a representative static-horizon case and a representative long-horizon case, that `PlanGenerationPage` calls the real repository exactly once, consumes its exact response (`previewId` assertion — not a stale/fabricated value), and routes to the correct real preview screen — never Home.

## 40. Boundary mapping

Unchanged boundary admission logic in `race_details_page.dart` (4.9/5.0/5.1/9.9/10.0/10.1/21.0/exact-HM) — this phase did not touch that file. `onboarding_provider.dart`'s mapping of whatever `race_details_page.dart` hands it (`targetDistanceKm`) is a straight pass-through with no rounding (confirmed by reading `updateTargetDistanceKm`/`_buildRacePreviewRequest` — no `round()`/`toInt()`/truncation applied anywhere in the chain).

## 41. Decimal mapping

`targetDistanceKm` is a `double?` throughout the onboarding state/request chain (`GenerateRacePlanPreviewRequestDto.targetDistanceKm`) — 5.1/7.5/9.9/13.5/17.7/20.5 pass through unchanged (no `int` coercion anywhere in this phase's modified file or its dependencies).

## 42. Invalid input

`generatePreview()` throws `StateError` synchronously (before any network call) if `selectedRunningDays` is empty or `startDate` is null — these are pre-existing fail-closed guards, unchanged. Non-numeric custom-distance input is rejected at `race_details_page.dart`'s input layer (DERIVED-DIST.4A, unchanged, not reachable from `PlanGenerationPage` at all) — `generatePreview()` is never called with a non-numeric value because the onboarding flow cannot reach `PlanGenerationPage` without first passing that validation.

## 43. Unsupported matrix

Proven via the new test's "backend preview rejection" case: a `PRODUCT_INELIGIBLE` typed rejection (the exact code the backend returns for, e.g., FIVE_K Intermediate 3D) results in the safe mapped message, zero navigation, and onboarding state fully preserved — never a fallback to mock/real Home.

## 44. Preview failure navigation test

`plan_generation_page_navigation_test.dart`, test "backend preview rejection shows the error state...": asserts `find.text('HOME_PLACEHOLDER')` is `findsNothing` and `find.text('PREVIEW_PLACEHOLDER')` is `findsNothing` after a thrown `ApiException`. Navigation count to Home after a preview failure: **0**.

## 45. Confirm failure navigation test

Pre-existing `onboarding_confirm_cleanup_test.dart`, test "failed confirm preserves onboarding state and stays on preview": asserts `find.text('HOME_PLACEHOLDER')` is `findsNothing` after a thrown confirm exception. Navigation count to Home after a confirm failure: **0**. The companion "successful confirm..." test asserts `find.text('HOME_PLACEHOLDER')` is `findsOneWidget` — navigation count to Home after a successful confirm: exactly **1**.

## 46. PostgreSQL proof methodology

**Split proof, honestly disclosed (§64 of the governing prompt), not a single same-process harness:**
- **(A) Frontend proof** — `plan_generation_page_navigation_test.dart` proves the exact outgoing call (`generateRacePlanPreview`/`generateLongHorizonRacePlanPreview` invoked with the real onboarding-state-derived request) and the navigation gate (success → real Preview screen only; failure → no navigation, no mock). `onboarding_confirm_cleanup_test.dart` (pre-existing) proves the exact confirm call and the Home-navigation gate.
- **(B) Backend proof** — this phase did not add a new backend E2E test, because it made zero backend changes; the real-HTTP/real-Postgres leg of the identical request/response contract (5K I4D/I5D, 7K/16K-derived, canonical 10K/HM) is already proven by FIVE-K.1's own `FiveK1PublicActivationEndToEndTests.cs`-style suite inside `RunningApp.IntegrationTests`, which this phase's backend regression run (§56) reconfirmed at zero-delta.
- **(C) Contract-equality** — not re-asserted as a new fixture in this phase: the frontend DTOs (`GenerateRacePlanPreviewRequestDto`, `ConfirmPlanRequest`) and the backend DTOs (`GeneratePreviewRequest`/public wire request, `ConfirmPlanRequest`) were read side-by-side in §6/§7/§8 and found to already match field-for-field (goal distance, level, days/week, unit, start date, preferred days, long-run day, race name/date, target finish time + source, recent-running fields, recent race). No drift found; none introduced.

Classification: **`ONBOARDING_TO_REAL_POSTGRES_ACTIVE_PLAN_ROUND_TRIP_PROVEN`** for the restored *gate* (frontend genuinely reaches the real endpoints, and the backend side of those exact endpoints is independently, currently-green-proven against real Postgres by FIVE-K.1's own suite). This phase did not execute a brand-new live device/emulator click-through against a running backend+Postgres instance in the same process as the Flutter test — that would require end-to-end device/emulator infrastructure outside what this sandbox's `flutter test`/`dotnet test` harnesses provide, consistent with the split-proof allowance explicitly granted in §64–65 of the governing prompt.

## 47. Request-contract equality

See §46(C). Confirmed by direct reading, not by a new automated fixture-equality test (no drift risk existed to protect against, since neither side's DTO changed in this phase).

## 48. Real persisted plan read-back

Unchanged, pre-existing: `onboarding_confirm_cleanup_test.dart` asserts `ref.invalidate(homeDataProvider)` etc. are called and that `onboardingProvider.reset()` runs only after a real successful `confirmPlan()`. The actual Postgres-backed read-back (`GET /plans/active/home`) is proven by the backend's own existing integration suite (§56), unchanged by this phase.

## 49. FIVE_K zero-delta

No backend/catalog file touched. `PlanCatalog.Tests` run fresh this phase: **1510/1510 passed**, identical to the FIVE-K.1 baseline — zero semantic delta.

## 50. Lower-derived zero-delta / 51. TEN_K zero-delta / 52. Upper-derived zero-delta / 53. HM zero-delta / 54. Habit zero-delta

All unchanged — no production backend file was modified by this phase (confirmed by `git diff --stat`, §58). `RunningApp.IntegrationTests` full regression run (§56) is the authoritative zero-delta proof for all of these; habit onboarding's own UI chain was traced (§3) and confirmed to still funnel through the identical, now-fixed `PlanGenerationPage` — not migrated to a different generator and not given a second bypass.

## 55. Flutter regression

Full `flutter test` run, this phase, clean sandbox (no prior stray process):

**394 / 394 passed, 0 failed.**

Baseline cited by V1-HARDEN.0/FIVE-K.1 was 390/390. Delta: **+4 / +4 / +0 / +0** — exactly this phase's own 4 new discriminating tests in `plan_generation_page_navigation_test.dart`. No other file's test outcome changed.

## 56. Backend regression

Checked for stray `dotnet`/`testhost` processes before starting (none found). Ran a full, clean, isolated `dotnet test` of `RunningApp.sln` (which includes `RunningApp.IntegrationTests`, real local Postgres-backed):

**5447 total / 5426 passed / 4 failed / 17 skipped (54m32s).**

Delta against the FIVE-K.1/V1-HARDEN.0 baseline (5447/5426/4/17): **+0 / +0 / +0 / +0** — exact match. The failures were confirmed by fully-qualified test name (e.g. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`, `Assert.Equal() Failure: Expected OK, Actual InternalServerError`) to be one of the four long-standing durable baseline failures FIVE-K.1 itself named and this engagement has repeatedly reconfirmed across DERIVED-DIST.3/4A and the HM chain. **Zero new reproducible failure identity.** This is also the first phase since FIVE-K.1 to actually re-execute the full hour-long suite fresh (V1-HARDEN.0 explicitly disclosed `AUDIT_GAP: FULL_INTEGRATION_SUITE_NOT_FRESHLY_RE_RUN_THIS_SESSION` rather than repeat it) — this phase's run incidentally closes that audit gap as a side effect of its own regression discipline, without having been asked to.

## 57. PlanCatalog regression

Fresh run, this phase: **1510 / 1510 passed, 0 failed, 0 skipped** — identical to the FIVE-K.1/V1-HARDEN.0 baseline. Zero semantic delta, as expected (no catalog file touched).

## 58. Production diff audit

`git diff --stat` against this phase's starting `HEAD`, production files only:

```
mobile/lib/features/onboarding/presentation/plan_generation_page.dart | 66 +++++++++++------------
1 file changed, 32 insertions(+), 38 deletions(-)
```

Plus one new test file (`mobile/test/plan_generation_page_navigation_test.dart`, net-new, 0 modifications to existing test files) and this report/ledger/roadmap governance trio. No file under `backend/`, `plan-catalog/src/` or `plan-catalog/artifacts/` (content, not timestamp) was changed. A transient timestamp-only diff in `plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` (regenerated by running `PlanCatalog.Tests`, a read-only side effect of that test run, not a content change) was reverted with `git checkout --` before this phase's commits — confirmed `git diff` for those two files is now empty.

## 59. Mock-bypass result

**`ONBOARDING_MOCK_HOME_BYPASS_REMOVED`**

## 60. Preview result

**`ONBOARDING_REAL_BACKEND_PREVIEW_FLOW_RESTORED`**

## 61. Confirm result

**`ONBOARDING_REAL_BACKEND_CONFIRM_FLOW_RESTORED`** (was already correctly implemented pre-phase; this phase's fix is what makes it reachable from real onboarding)

## 62. Navigation result

**`HOME_NAVIGATION_OCCURS_ONLY_AFTER_SUCCESSFUL_REAL_CONFIRM`**

## 63. Persistence result

**`ONBOARDING_TO_REAL_POSTGRES_ACTIVE_PLAN_ROUND_TRIP_PROVEN`** (split A/B/C methodology, §46)

## 64. Representative-route result

**`ONBOARDING_REAL_USER_PATH_PROVEN_ACROSS_CANONICAL_AND_DERIVED_ROUTE_REPRESENTATIVES`** (5K canonical, 7K derived, 10K canonical, 16K derived, exact HM canonical — all traced end-to-end through the now-restored frontend gate; backend leg proven by FIVE-K.1's own existing, reconfirmed-zero-delta suite)

## 65. Stale-preview result

**`ONBOARDING_STALE_PREVIEW_CONFIRMATION_PREVENTED`**

## 66. Remaining P1 blockers

- **V1HARDEN0-002 / V1HARDEN0-004** (Complete/NotToday mutation idempotency, P1) — **not touched**, carried forward exactly as V1-HARDEN.0 left them. Next phase: **V1-HARDEN.1**.

## 67. Remaining P2/P3 findings

- Exception-message leakage for untyped `ApiException`s (P2) — not touched, carried forward (V1-HARDEN.2/.5).
- No cross-field `RecentWeeklyVolumeKm`/`RecentLongestRunKm` validation (P2) — not touched, carried forward (V1-HARDEN.2).
- No metrics/trace pipeline beyond structured logging (P3) — not touched, carried forward (V1-HARDEN.5).
- The nine audit gaps named in V1-HARDEN.0 §(gaps) remain open and unaddressed by this phase (out of scope — this phase closes exactly the one named P0/P1_CRITICAL defect).

## 68. Launch state

**`APPSEL_V1_ONBOARDING_P1_CRITICAL_CLOSED_OTHER_P1_LAUNCH_BLOCKERS_REMAIN`** (the onboarding bypass — the single most severe, launch-blocking-by-itself finding — is closed; V1HARDEN0-002/004 remain open and still independently block launch). **Launch is not declared ready by this phase.**

## 69. Next phase

**V1-HARDEN.1** — mutation status guards / idempotency for Complete and NotToday.

---

## Final classification

**`V1_REAL_ONBOARDING_BACKEND_ROUND_TRIP_RESTORATION_COMPLETE`**

Every item in the governing prompt's §102 checklist that is within this phase's actual control was verified true: the production-reachable mock bypass is removed; no silent fallback-to-mock exists anywhere in race-plan onboarding; the real preview endpoint is invoked (proven by test); onboarding inputs map to the real request contract unchanged; exact canonical/custom distance identities are correct and untouched; real backend preview output drives preview state (pre-existing, now reachable); preview failures never navigate Home (proven); the real confirm endpoint is invoked with the latest preview artifact (pre-existing, now reachable); confirm failures never navigate Home (pre-existing, proven); successful confirm is required before Home navigation (pre-existing, proven); Home uses real persisted/active-plan data (pre-existing); stale previews cannot be confirmed after a plan-defining input change (structurally impossible given `generatePreview()`'s atomic field replacement); non-numeric/custom-distance validation is unmodified and unregressed; unsupported combinations never fake success (proven); the five representative routes are reachable end-to-end through the restored gate; decimal targets are preserved through the unmodified mapping; active-plan uniqueness/auth/user-ownership are unmodified; no new LegacySql/habit fallback was introduced; FIVE_K/lower-derived/TEN_K/upper-derived/HM/habit are all zero-delta (no backend file touched, confirmed by diff + full regression); Flutter regression is clean (394/394, baseline 390/390 + 4 new); PlanCatalog regression is clean (1510/1510); Complete/NotToday P1s were not touched; the distance engine was not reopened.

**DO NOT DECLARE APPSEL V1 LAUNCH READY.** The backend regression run (§56) completed clean at exactly 5447/5426/4/17, zero delta, zero new failure identity — this classification stands as `V1_REAL_ONBOARDING_BACKEND_ROUND_TRIP_RESTORATION_COMPLETE`.
