# Phase 4L.5C — Regenerate/Cancellation, Authentication Redirect and Mutation Ambiguity Closure

## 1. Executive result

`LONG_HORIZON_FLUTTER_REGENERATE_CANCELLATION_AUTH_REDIRECT_AND_MUTATION_AMBIGUITY_CLOSURE_COMPLETED`.

The final material Flutter gaps inherited from Phases 4L.5A/4L.5B are closed. The full Flutter suite passes 339/339. No backend source changed and Phase 4L.6 was not implemented.

## 2. Inherited gaps

The inherited gaps were: no complete `RegeneratePreviewRequired` cancellation flow, Firebase-coupled router authentication that could not be tested without initialization, and missing read-after-write proof for confirmation, completion, NotToday, activation, retry, and cancellation.

## 3. Scope and exclusions

This pass changed only Flutter integration, tests, this phase record, and the append-only readiness ledger. It did not change backend planning/recovery/continuation authority, add an endpoint, auto-run a mutation, synthesize a session, add background work, redesign Riverpod/go_router, or implement Phase 4L.6.

## 4. Recovery/cancellation inspection

The existing static Stop Plan dialog in `ProfilePage` uses `PlanRepository.cancelPlan`. Its repository contract is reusable for RollingLongHorizon; its static navigation/reset UI is not. A narrow Rolling regeneration page therefore reuses the repository/endpoint while owning only its distinct explanation, authoritative verification, onboarding reset, and back-stack behavior.

## 5. Regenerate product flow

Rolling Home recovery card → `LongHorizonRegeneratePlanPage` explanation → user selects Stop current plan and continue → destructive confirmation dialog → one cancellation request → authoritative active-plan read → only when no active plan remains, onboarding reset and `go(/onboarding/goal)`.

## 6. Regenerate UI

The page explains that the current plan cannot safely continue, stopping is explicit and irreversible in this flow, workout history follows existing behavior, and no replacement is automatic. It avoids internal checkpoint/JIT/evidence terminology. Cancel returns to Home.

## 7. Cancellation request authority

The real contract is `POST /api/v1/plans/{planId}/cancel` with `{ reason }`. `UserId` is never sent; backend authentication/ownership remains authoritative. The plan ID is required by this actual endpoint, so the client uses the active-plan-details read rather than inventing a new active-plan cancellation route. One in-flight guard prevents duplicate requests.

## 8. Cancellation success/replay

A 200 response, replay-like response, 404/already-gone response, timeout, and lost response all converge on the same active-plan verification. HTTP status alone never triggers success. Navigation occurs once only after the read proves no active plan remains.

## 9. Cancellation ambiguity

If no active plan remains, cancellation is committed. If the same plan remains, the screen preserves state and offers explicit retry. If another active plan appears, the user is directed to review Home. If verification fails, local active-plan state is retained and safe network guidance is shown.

## 10. Cancellation conflicts

Not-found/already-cancelled requires verification; concurrency/change refreshes authority rather than deleting local state; authentication remains governed by the existing auth flow; persistence/network failures preserve the plan; unknown errors are not rendered raw.

## 11. Authentication architecture

The production router previously read `FirebaseAuth.instance.currentUser` and constructed a Firebase stream notifier internally. `AuthNavigationState` is now the smallest app-owned navigation boundary: only `loading`, `unauthenticated`, and `authenticated`. It contains no token, user payload, profile, or storage behavior.

## 12. Auth test-seam decision

Option B was selected. `FirebaseAuthNavigationState` delegates to the same Firebase current user and `authStateChanges()` stream in production. `AppRouter.createRouter` accepts an `AuthNavigationState`, so tests exercise the real route graph and redirect callback without initializing Firebase.

## 13. Protected routes

The existing public set remains splash, auth entry/sign-up/sign-in, and intro. All other routes remain protected, including rolling/static Home, Calendar, detail, plan details, regeneration, and Habit onboarding routes. No ownership decision moved client-side.

## 14. Auth redirect tests

Real-router tests prove unauthenticated rolling Home, Calendar, rolling detail, regenerate, static detail, and Habit routes redirect to `/auth` before protected widgets render. Authenticated rolling Home/Calendar/detail proceed. Loading stays on splash. An auth-state refresh does not loop or invent destination restoration; existing policy leaves the user on auth after sign-in unless existing login/bootstrap navigation moves them.

## 15. Firebase-init regression

Router tests override only the app-owned navigation state and repository providers. They do not initialize Firebase. Production startup still creates `FirebaseAuthNavigationState` and observes the same Firebase stream. The full 339-test suite, including the six previously affected bootstrap tests, is green.

## 16. Mutation ambiguity authority

A timeout/lost response is not proof of failure. Each mutation uses its approved read surface: confirmation/activation/retry use active Home, completion/NotToday use rolling detail, and cancellation uses active-plan details. Definitive typed errors continue through `LongHorizonUiErrorMapper`.

## 17. Confirmation ambiguity

After an ambiguous confirm, a rolling active Home proves commit and navigates once. No rolling active plan preserves the preview and offers explicit retry. No new preview or second confirmation is issued, and committed confirmation clears stale onboarding preview state once.

## 18. Completion ambiguity

Detail `Completed` with matching distance/duration commits. `Planned` permits retry. `Completed` with different values shows a conflict and authoritative values after refresh. `NotToday` shows an outcome conflict. Success refreshes detail, Home, and the assigned-date Calendar month.

## 19. Not-today ambiguity

Authoritative `NotToday` commits when the public reason matches or the contract omits it. `Planned` permits retry; `Completed` is a conflict. No reason authority is invented when the backend does not expose one.

## 20. Activation ambiguity/concurrency

An advanced/current in-progress window commits after Home read and invalidates only months represented by server-returned sessions. Still-ready permits explicit retry. Reassessment and terminal states re-render as authoritative. Typed concurrency refreshes the winner state once; no second activation occurs.

## 21. Retry ambiguity

Retry re-reads Home. Activation-ready shows an explicit Activate action without calling activation. Unchanged recovery preserves Retry. Regenerate/support states render their existing actions. Retry never fabricates Blocked→Pending or invalidates Calendar without server state that warrants it.

## 22. Verification result handling

No persisted generic verification model was added. Small endpoint-local branches are clearer and preserve the actual authoritative payload. Unknown/indeterminate verification never becomes generic success.

## 23. Duplicate navigation protection

In-flight booleans prevent double taps. Confirmation/cancellation use one-shot committed guards. Mounted checks precede UI transitions. No arbitrary delay is used. Tests assert one mutation and one route transition.

## 24. Regenerate navigation/back stack

Back before confirmation returns to Home. `PopScope` disables back during cancellation. Committed cancellation uses `go()` rather than `push()`, removing cancelled Home/regenerate routes. Onboarding state is reset and no preview is pre-created.

## 25. Static/Habit cancellation compatibility

Static `ProfilePage` cancellation and repository behavior were not rewritten. Habit behavior was not intercepted. The shared repository method remains unchanged; strategy-specific behavior is limited to the new Rolling page. Full Flutter regressions pass.

## 26. Error mapping

Existing real backend Long-Horizon codes continue through `LongHorizonUiErrorMapper`. Verification-read failures have distinct safe messages from definitive mutation conflicts. No new backend code was invented and no raw exception is displayed.

## 27. Accessibility

The destructive confirmation has an explicit semantic label; loading remains announced; errors are live regions; the page scrolls; mutation-time back is controlled. A 320×568 test found a shared long-label horizontal overflow, fixed by making `AppPrimaryButton` text flexible with one-line safe ellipsis. Accessibility tests pass 7/7.

## 28. Logging/privacy

No analytics SDK or new logging was added. Plan/user IDs, auth tokens, Firebase payloads, actual metrics, NotToday reasons, full responses, and raw exceptions are not logged by this work.

## 29. Test harness

The Phase 4L.5B harness was extended with scripted response sequences, one-shot ambiguous errors, an optional real router, controllable auth state, deterministic active-plan/detail/Home reads, call counts, and no arbitrary delays or global mutable state.

## 30. Connected-flow tests

The focused regenerate/auth/preview-confirmation/mutation/accessibility files contain 56 passing tests. They cover explicit cancellation, replay/ambiguity, no automatic replacement, auth redirects, confirmation ambiguity, all requested completion/NotToday shapes, activation timeout/concurrency, retry separation, duplicate guards, and accessibility. Full Flutter total is 339 passing.

## 31. Formatting/analyze

Every touched Dart file was formatted. Analyzer run against all 12 touched Dart files has zero findings. Project-wide analyze reports 165 inherited warnings/info in unrelated legacy files; project-wide formatting debt also remains inherited. No unrelated broad formatting was performed and no new touched-file format failure exists.

## 32. Backend visibility

No backend file changed. Fresh Release `--no-build` results: Long-Horizon-filtered backend 847 passed, 0 failed, 0 skipped; full backend 3,063 passed, 0 failed, 0 skipped. Plan-catalog: 1,206 passed, 43 failed, 0 skipped—the identical documented pre-existing governance snapshot/parity debt (hard-coded 36/57 aggregate assumptions), with no runtime/catalog regression.

## 33. Governance

`TD-LONG-HORIZON-FLUTTER-REGENERATE-CANCELLATION-AUTH-AMBIGUITY-CLOSURE-001` is added CLOSED. Phase 4L.5A Calendar/accessibility and Phase 4L.5B end-to-end/navigation records are append-only CLOSED. Phase 4L.5 read/outcome remains CLOSED. Aggregate: 67 total, 14 OPEN, 53 CLOSED.

## 34. Remaining gaps

No material Phase 4L.5C Flutter blocker remains. Existing release-readiness, plan-catalog governance-test debt, and project-wide analyzer/format debt remain outside this closure. They are not silently closed.

## 35. Final classification

`LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED_NOW_HAS_AN_EXPLICIT_USER_CONFIRMED_SERVER_VERIFIED_CANCELLATION_AND_RETURN_TO_PLAN_CREATION_FLOW_WITH_NO_AUTOMATIC_REPLACEMENT`.

`LONG_HORIZON_PROTECTED_HOME_CALENDAR_DETAIL_AND_REGENERATE_ROUTES_NOW_HAVE_AUTOMATED_AUTHENTICATION_REDIRECT_PROOF_WITH_NO_PROTECTED_CONTENT_FLASH_OR_REDIRECT_LOOP`.

`LONG_HORIZON_CONFIRMATION_COMPLETION_NOT_TODAY_ACTIVATION_RETRY_AND_CANCELLATION_TIMEOUTS_NOW_RESOLVE_THROUGH_AUTHORITATIVE_READ_AFTER_WRITE_VERIFICATION_WITHOUT_BLIND_DUPLICATE_MUTATION`.

`LONG_HORIZON_MUTATION_REPLAY_CONCURRENCY_AND_AMBIGUITY_HANDLING_PRODUCES_ONE_ROUTE_ONE_NOTIFICATION_ONE_SERVER_AUTHORED_STATE_AND_NO_FABRICATED_SESSION`.

`LONG_HORIZON_PHASE4L_5A_AND_PHASE4L_5B_REMAINING_MATERIAL_FLUTTER_GAPS_ARE_CLOSED`.

## 36. Exact next phase

Recommended next phase: Phase 4L.6 — End-to-End Release Acceptance, Governance Test-Debt Closure and Production Readiness. It was not started here.
