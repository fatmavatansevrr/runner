# Backend Phase 0 — Release Impact Clarification

Focused clarification pass on the "safe template selection" change (removal of silent fallback in
`PlaceholderPlanGenerationEngine.SelectTemplateAsync`). Read-only investigation plus minimal, targeted
documentation updates. Does not revert the Phase 0 fix. Does not touch `plan-catalog/` or implement any
plan-catalog integration.

## 1. Scope / approval classification

**`BEHAVIOR_CHANGE_REQUIRES_PRODUCT_CONFIRMATION`** — not a clean `APPROVED_SAFETY_FIX`.

Evidence:
- `MVP_LIMITATIONS.md` §3 ("Limited Seed Templates") **explicitly documented the old fallback as the known, accepted MVP-phase effect**: *"If an onboarding user selects a Marathon or Half Marathon goal, or chooses an advanced running level, the backend plan generator defaults to picking `habit_5k_beginner_3day_km_v1` as a fallback."* This is not an undocumented bug — it was a **written, product-facing description of intended MVP behavior**, filed alongside other deliberate MVP simplifications (mock auth, placeholder adaptation engine).
- `FINAL_ACCEPTANCE_CHECKLIST.md` contains **no** row testing/certifying this fallback scenario (searched for "fallback"/"marathon"/"unsupported"/"advanced" — zero matches), so the QA-acceptance record does not depend on the old behavior.
- `API_DOCUMENTATION.md`'s `generate-preview` example never showed or described `fallback_used`/`fallback_reason` at all, and never described what happens for an unsupported combination — the public API doc was silent on this edge case, neither confirming nor denying the fallback as a supported contract.
- The DTO field comment (`GeneratePreviewResponse.FallbackUsed`, before this change) called it a *"Debug/development-only signal — production UI is not required to surface this,"* and the Flutter client's `GeneratePreviewResponse.fromJson` never reads `fallback_used`/`fallback_reason` at all (confirmed absent from the model).

**Conclusion:** the fallback was **intentional MVP-phase behavior, explicitly documented as such** — not merely an unreviewed dev placeholder — but it was never a *committed, tested, user-facing* contract (no acceptance-test coverage, no client consumption, explicitly marked debug-only in code). Changing it is a real, product-visible behavior change (200→404 for previously-"successful" unsupported requests) that a product owner should explicitly sign off on before this ships, even though the new behavior is safer. `MVP_LIMITATIONS.md` and `API_DOCUMENTATION.md` have been updated (see §3) to keep the docs accurate regardless of that sign-off.

## 2. API / frontend compatibility

A real Flutter mobile client exists (`mobile/lib`) and was inspected.

- `fallback_used` / `fallback_reason`: **never read** by the client. `GeneratePreviewResponse.fromJson` (`mobile/lib/core/network/dtos.dart`) has no fields for them — they were silently dropped on deserialization even before this change.
- Unsupported combinations expected to return 200 OK: **no evidence found** that any client code assumes this. The only "fallback" hits in `mobile/lib` are unrelated (a UI default-selection comment, a settings safety-net comment).
- `PLAN_TEMPLATE_NOT_FOUND` / HTTP 404 handling: `mobile/lib/core/network/api_client.dart`'s `_mapError` **generically** captures any backend `errorCode`/`message` from the standardized error envelope into `ApiException`, before falling back to a generic per-status-code message. Since the backend always sets a `message` for this exception, the client will surface the **exact backend message text** (e.g. `"No plan template is available for goal_type=Race, goal_distance=TenK, level=RunningRegularly, days_per_week=4."`) via `ApiException.toString()`. Call sites (e.g. `plan_generation_page.dart`) already do generic `catch (e) { setState(() => _error = e.toString()); }` — **no crash, no unhandled exception**.
- User-friendly unsupported-template message: **not present**. The surfaced text is the raw backend exception message (developer-oriented, contains enum names like `TenK`/`RunningRegularly`), not curated end-user copy.

**Classification: `CLIENT_READY_FOR_PLAN_TEMPLATE_NOT_FOUND`** (structurally — no crash, no missing handling path) **with a UX-polish follow-up recommended, not required**: add a specific `PLAN_TEMPLATE_NOT_FOUND` case to `_mapError`'s status-code switch (or a dedicated check before the generic envelope branch) so the client shows curated copy (e.g. "This combination isn't available yet") instead of the raw backend message. Not implemented in this pass — no client/mobile files were modified, per this task's backend-only scope.

## 3. API contract / documentation — updated

Found stale/incomplete in two places; both updated minimally:

- **`API_DOCUMENTATION.md`**: added a "Note on template coverage" line under `generate-preview`, and added the `PLAN_TEMPLATE_NOT_FOUND` row to the `errorCode` table (previously only listed generic `NOT_FOUND`/`CONFLICT`/`VALIDATION_ERROR`/`INTERNAL_ERROR`).
- **`MVP_LIMITATIONS.md`** §3: the "Effect" bullet describing the old silent-fallback behavior was rewritten to describe the new `404 PLAN_TEMPLATE_NOT_FOUND` behavior, explicitly noting it was changed by Backend Integration Phase 0.

DTO doc comments (`GeneratePreviewResponse.FallbackUsed`/`FallbackReason`, `TemplateSelectionResult`) were already updated in the Phase 0 change itself (prior pass) to state the fields are now always `false`/`null`, kept for back-compat.

Integration test names already reflect the new behavior: `GeneratePreview_UnsupportedGoalCombo_ReturnsPlanTemplateNotFound_NoSilentFallback` (renamed from `..._UsesFallback_...` in the Phase 0 pass).

## 4. Test database isolation

- **New unit-style tests** (`SafeTemplateSelectionTests.cs`, added in Phase 0): use the **EF Core InMemory provider**, with a **fresh `Guid.NewGuid()`-named database per test method** (`NewSeededContext()` creates a new options instance per call). Fully isolated — no shared state between tests, no persistence beyond the test process, nothing to clean up. Proven by construction, not by convention.
- **Existing `UserJourneyTests`** (real HTTP host + real Postgres, `CustomWebApplicationFactory`): connection string is `Host=localhost;Port=5432;Database=antigravity_dev` — **this is the development database, not a dedicated test database**. Cleanup is via `ResetAsync()` → `POST /api/v1/testing/reset`, called at the **start** of every test (confirmed: every `[Fact]` in `UserJourneyTests.cs` begins with `await ResetAsync();`). `TestingController.ResetDatabase` deletes all rows scoped to the single hardcoded mock user (`mock-user-001` — per `MVP_LIMITATIONS.md` §2, there is only one user in this whole MVP system) across `WorkoutLogs`, `NotTodayDecisions`, `PendingConfirmations`, `AdaptationEvents`, `PlanEvents`, `PlanPreviews`, `TrainingDays`, `TrainingWeeks`, `TrainingPlans`, `UserProfiles`, in FK-safe order.
- **Did Phase 0 tests leave persistent state?** The one `UserJourneyTests` test I modified now creates **less** persisted state than before (it returns 404 immediately; no preview/plan is created), a net improvement. It does not clean up after itself, but neither did any pre-existing test — cleanup is pre-test-reset only, not post-test, which is **pre-existing behavior, not a Phase 0 regression**.
- **Follow-up note (test-environment hygiene, pre-existing, not introduced by Phase 0):** the integration suite runs against `antigravity_dev` rather than a dedicated test database. Whatever the last test run left behind persists in that database (scoped to `mock-user-001`) until the next `ResetAsync()` call. This is a real, unresolved hygiene risk for anyone using the same dev DB for manual/local testing in parallel with the test suite — flagged here, not fixed (out of scope for this clarification pass).

## 5. Final shipping classification

**`PHASE0_REQUIRES_PRODUCT_CONFIRMATION`**

The code change itself is sound (build green, 24/24 tests passing, no client crash risk, docs now accurate) and should **not be reverted** absent a specific product reason to keep the old fallback — but it changes a behavior that was **explicitly documented as intended MVP behavior** (`MVP_LIMITATIONS.md` §3), not merely a bug. A product owner should confirm that returning `404 PLAN_TEMPLATE_NOT_FOUND` for unsupported onboarding combinations (rather than silently substituting a 5K/Beginner plan) is the desired MVP-phase UX before this ships to users, since it changes what happens on a path real onboarding users can currently reach (selecting Marathon/Half-Marathon/Advanced).

## Files inspected

`MVP_LIMITATIONS.md`, `DEVELOPER_HANDOFF.md`, `API_DOCUMENTATION.md`, `FINAL_ACCEPTANCE_CHECKLIST.md`, `DATABASE_DOCUMENTATION.md`, `FRONTEND_DOCUMENTATION.md`; `backend/RunningApp.Application/PlanGeneration/*.cs`, `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewResponse.cs`, `backend/RunningApp.Api/Controllers/TestingController.cs`, `backend/RunningApp.IntegrationTests/{UserJourneyTests.cs,CustomWebApplicationFactory.cs,PlanGeneration/SafeTemplateSelectionTests.cs}`; `mobile/lib/core/network/{dtos.dart,api_client.dart,api_exception.dart}`, `mobile/lib/features/onboarding/{presentation/plan_generation_page.dart,presentation/goal_selection_page.dart,data/onboarding_provider.dart}`, `mobile/lib/features/settings/presentation/settings_page.dart`.

## Files changed (this pass)

- `API_DOCUMENTATION.md` (doc update)
- `MVP_LIMITATIONS.md` (doc update)
- `PHASE0_RELEASE_IMPACT_CLARIFICATION.md` (this report, new)

No backend source/test code was modified in this pass (Phase 0's own code/test changes are unchanged from the prior pass). No `plan-catalog/` artifact was touched. No `mobile/` client code was modified (follow-up noted, not implemented, per backend-only scope).
