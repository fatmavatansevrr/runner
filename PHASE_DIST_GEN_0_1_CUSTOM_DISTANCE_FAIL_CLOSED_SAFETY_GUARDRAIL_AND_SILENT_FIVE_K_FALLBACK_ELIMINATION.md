# PHASE DIST-GEN.0.1 — CUSTOM-DISTANCE FAIL-CLOSED SAFETY GUARDRAIL AND SILENT 5K FALLBACK ELIMINATION

**Type:** SAFETY / DEFECT-CORRECTION (no custom-distance authority, no 16K numeric authority, no dormant-infrastructure activation)

---

## 1. DIST-GEN.0 findings consumed

`PHASE_DIST_GEN_0_10K_TO_HALF_MARATHON_INTERMEDIATE_DISTANCE_ARCHITECTURE_PARENT_FAMILY_DECOMPOSITION_AND_PROJECTION_AUTHORITY_AUDIT.md` was read in full. Its final classification `INTERMEDIATE_DISTANCE_10K_TO_HM_ARCHITECTURE_AND_PROJECTION_MODEL_CLOSED` is **not reopened**. This phase consumes exactly:

- §2/§46: `GoalDistance.Custom` exists and is vestigial; `GoalDistanceKm.Resolve` had no arm for it and fell through to `5.0`; both onboarding screens (`race_details_page.dart`, `custom_goal_page.dart`) already let a user express an arbitrary distance but discard it, sending only `'custom'`.
- §20 (RaceHorizonPolicy.DecideForDistance): non-HalfMarathon distances fall through to TEN_K's 8/12/14-week bounds via the parameterless `Decide` overload — confirmed real, but this phase proves it is unreachable for `Custom` through the public path rather than modifying it.
- §19/§22: `V1CatalogPilotIdentityPolicy` and `GoalFeasibilityResolver`'s Riegel formula were both confirmed to already correctly reject/never touch `Custom` in the live path.
- §34/§49: `CanonicalDistanceFamilyResolver` and dormant `RequestedTargetDistanceKm`/`CanonicalDistanceFamily` scaffolding must remain untouched and usable by a future DIST-GEN.1 — verified preserved (§13/§14 below).

No 16K numeric authority, target-distance projection, or dormant-infrastructure live-wiring was introduced by this phase.

## 2. Exact defect reproduction

Before this phase's fix, the following chain was live:

1. `GoalDistanceKm.Resolve(GoalDistance.Custom)` → `_ => 5.0` (unconditional fallback).
2. `GenerateRacePlanPreviewRequestValidator` only rejected `Custom` when `TargetFinishTimeSource == ProductAverage` (via `CanonicalTargetFinishTimePolicy.GetCanonicalSeconds` returning `null`). For `TargetFinishTimeSource == UserDefined`, `Custom` passed validation with **no rejection at all**.
3. `PlanServices.GeneratePreviewAsync`'s internal `GeneratePreviewRequestValidator.Validate` had **no `GoalDistance` check whatsoever**.
4. `_routeDecider.Decide(request)` → `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` returns `false` for `Custom` (correctly) → routes to `GenerationSource.LegacySql`.
5. The legacy path (`PlanServices.GeneratePreviewAsync` steps 1–4) proceeds to select a template and, at confirmation time, calls `GetGoalDistanceInKm(previewData.GoalDistance)` → `GoalDistanceKm.Resolve(Custom)` → **5.0**, persisted onto `TrainingPlan.GoalDistanceKm`.

Net effect (confirmed by direct code read, matching DIST-GEN.0 §46's own disclosure): a race-plan request with `goal_distance: "custom"` and `target_finish_time_source: "user_defined"` reached generation and would silently receive a plan built and persisted as if for a 5K — no error, no warning, `HTTP 200`.

## 3. Backend Custom fallback inventory

| Site | Classification |
|---|---|
| `GoalDistanceKm.Resolve` `_ => 5.0` | **SILENT_CANONICAL_FALLBACK** (the core defect — fixed) |
| `CanonicalTargetFinishTimePolicy.GetCanonicalSeconds` | **SAFE_EXPLICIT_REJECTION** (already returned `null` for `Custom`; verified unchanged) |
| `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`/`IsSupportedLevelFrequency` | **SAFE_EXPLICIT_REJECTION** (`Custom` never matches `TenK`/`HalfMarathon`; verified unchanged) |
| `RaceHorizonPolicy.DecideForDistance` | **DORMANT_ONLY for Custom** — its `_ => Decide(...)` (TEN_K bounds) arm is real but now provably unreachable for `Custom` via the public path (proved by test, §16) |
| `CanonicalDistanceFamilyResolver` | **FUTURE_PROJECTION_SCAFFOLDING** — dark, DI-registered, zero production consumers (`CatalogNotWiredToGenerationTests` still passes, unchanged) |
| `PlanCatalogDomainMapper` | **TEST_ONLY / analytical** — never touches DB, never calls generation; its `GoalDistance.Custom` string reference is a doc-comment describing the (now-fixed) defect, not live logic |
| `GoalFeasibilityResolver`'s Riegel formula | **DORMANT_ONLY** — distance-generic, unwired to live generation, untouched |
| `RecentRaceInput.Distance` reaching `GoalDistanceKm.Resolve` in `CatalogPreviewGenerator`/`CatalogPrescriptionContextBuilder` | Was **SILENT_CANONICAL_FALLBACK** (recent race reported as Custom would resolve to 5.0km); now **SAFE_EXPLICIT_REJECTION** via the new validator guard |

## 4. Frontend race-flow defect

`race_details_page.dart`'s `_mapDistanceTextToEnum` snaps to a preset within ±0.2km or returns `'custom'`; the typed number itself was never transmitted. Confirmed via direct read — unchanged in this phase (no numeric transmission was added, per the "no custom-distance support" constraint). Fixed by blocking submission instead (§12).

## 5. Frontend habit-flow defect

`custom_goal_page.dart`'s `_onContinue` calls `updateGoalDistance('custom')` **unconditionally**, regardless of the stepper's integer km value — this screen never had a path to a canonical distance at all. Fixed by disabling Continue unconditionally on this screen (§12), since any submission from it was always defective.

## 6. Recent-race Custom audit

`RecentRaceInput.Distance` is a `GoalDistance` enum field, independent of the primary `GoalDistance`. Before the fix, a request with a fully-supported primary distance (e.g. `ten_k`) but `recent_race.distance: "custom"` would reach `CatalogPreviewGenerator.cs:1063` / `CatalogPrescriptionContextBuilder.cs:410`, both calling `GoalDistanceKm.Resolve(request.RecentRace.Distance)` — silently 5.0km, corrupting pace/feasibility evidence. Fixed: `GeneratePreviewRequestValidator` now rejects any request where `RecentRace.Distance` doesn't resolve, before either downstream call site is ever reached. Proven by `GoalDistanceCustomFailClosedTests.RacePreview_RecentRaceDistanceCustom_WithOtherwiseValidPrimaryGoalDistance_FailsClosed`.

## 7. GoalDistanceKm fallback audit

Full-file read confirmed `GoalDistanceKm.Resolve` was the **sole** place with an unconditional silent-canonical fallback for `GoalDistance`. `PlanServices.GetGoalDistanceInKm` is a thin one-line delegate to it (no independent fallback). No other `GoalDistance`-keyed switch in the backend produces a canonical value with a `default`/`_` arm — `CanonicalTargetFinishTimePolicy` and `V1CatalogPilotIdentityPolicy` were both already fail-closed (§3).

## 8. RaceHorizonPolicy fallback audit

`RaceHorizonPolicy.DecideForDistance`'s `_ => Decide(startDate, raceDate)` arm (TEN_K's 8/12/14-week bounds) is real and, per DIST-GEN.0, intentionally shared by every non-HalfMarathon distance including a hypothetical future one. This phase does **not** modify it (per the explicit instruction not to touch training-authority horizon code). Instead, the new fail-closed gate in `GeneratePreviewRequestValidator.Validate` runs as the *first* statement inside `PlanServices.GeneratePreviewAsync` (before the horizon-decision block that calls `RaceHorizonPolicy.DecideForDistance`), making the TEN_K-fallback arm provably unreachable for `Custom` via the public path. Proven directly: `RacePreview_GoalDistanceCustom_NeverReachesHorizonClassification` sends `goal_distance=custom` with both a too-short (5-week) and too-long (30-week) horizon that would otherwise trigger `PLAN_CORE_HORIZON_TOO_SHORT`/`PLAN_HORIZON_COMPOSITION_REQUIRED`/`PLAN_HORIZON_START_TOO_EARLY`/`PLAN_CORE_HORIZON_UNSUPPORTED` — the response is always `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` and never any horizon-classification code.

## 9. Public fail-closed boundary

Three layers, all defense-in-depth of the same rule (`GoalDistanceKm.TryResolve` fails):

1. `GenerateRacePlanPreviewRequestValidator.Validate` (public controller boundary for race requests) — earliest possible rejection.
2. `GenerateHabitPlanPreviewRequestValidator.Validate` (public controller boundary for habit requests) — independent of the race path.
3. `GeneratePreviewRequestValidator.Validate` (shared internal validator, first statement inside `PlanServices.GeneratePreviewAsync`, reached by **both** race and habit flows after mapping) — also covers `RecentRaceInput.Distance`, which the two public-boundary validators don't see in the same shape.

All three run strictly before route decision, horizon selection, candidate resolution, volume planning, pace resolution, or calendar materialization.

## 10. Error-contract decision

Considered reusing `UnsupportedTargetDistanceException` (HTTP 400, `UNSUPPORTED_TARGET_DISTANCE`) — rejected: that exception is thrown by the dormant `ICanonicalDistanceFamilyResolver` for an out-of-range **numeric** km value; `GoalDistance.Custom` carries no numeric payload at all on the current live surface, a different failure shape. No existing exception honestly fit, so a new one was created:

- **New type:** `UnsupportedGoalDistanceException` (`RunningApp.Application.Exceptions`).
- **HTTP status:** 422 (this repo's established convention for domain-validation/unsupported-identity failures — matches `HmFrequencyNotSupportedException`, `CatalogLivePilotRequestUnsupportedException`).
- **Error code:** `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`.
- **Message:** plain, non-internal text naming the supported distances; never leaks stack traces or internal exception detail (`GlobalExceptionHandler`'s existing convention already masks 500s only — 422 messages are exposed verbatim by design, matching e.g. `PlanPreviewSnapshotMalformedException`).
- Mapped in `GlobalExceptionHandler` immediately after `UnsupportedTargetDistanceException` for locality.

## 11. Backend fix (exact diff description)

- `backend/RunningApp.Application/Common/GoalDistanceKm.cs`: `Resolve`'s `_ => 5.0` arm now `throw new UnsupportedGoalDistanceException(...)`. Added `TryResolve(GoalDistance, out double)` non-throwing counterpart for validators.
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs`: added `UnsupportedGoalDistanceException`.
- `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs`: added the `UnsupportedGoalDistanceException => (422, "GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED")` mapping.
- `backend/RunningApp.Application/Validation/GenerateRacePlanPreviewRequestValidator.cs`: added an early `GoalDistanceKm.TryResolve` guard, before the existing canonical-finish-time consistency check.
- `backend/RunningApp.Application/Validation/GenerateHabitPlanPreviewRequestValidator.cs`: added the same guard (habit requests previously had **no** `GoalDistance` check at all).
- `backend/RunningApp.Application/Validation/GeneratePreviewRequestValidator.cs`: added the same guard for `request.GoalDistance`, plus an independent guard for `request.RecentRace?.Distance`.
- `backend/RunningApp.IntegrationTests/PlanGeneration/GenerateRacePlanPreviewRequestValidatorTests.cs`: updated `ProductAverage_Custom_NoCanonicalValue_Throws` — this is a genuine, intentional contract change (was `ArgumentException`/400 "no canonical value"; is now `UnsupportedGoalDistanceException`/422, since the new gate fires first) — documented inline, not a silent behavior drift.

## 12. Frontend guard (exact diff description)

- `mobile/lib/features/onboarding/presentation/race_details_page.dart`: added `_isUnsupportedDistance` (reuses the existing `_mapDistanceTextToEnum` tolerance logic, checking for `'custom'`), a listener that rebuilds on distance-field edits, an inline red (`AppColors.missed`) message below the distance row when unsupported, and `onPressed: _isUnsupportedDistance ? null : _onContinue` on the Continue button (mirrors the existing `_isValid ? _onSave : null` pattern already used in `recent_race_result_page.dart`). No copy implies future support.
- `mobile/lib/features/onboarding/presentation/custom_goal_page.dart`: `_isUnsupportedDistance` is unconditionally `true` (this screen never produces anything but `goal_distance=custom`), Continue is disabled unconditionally, and an inline message directs the user back to the three supported habit-goal presets.

## 13. Dormant projection infrastructure preserved

`git diff` confirms **zero** changes to `CanonicalDistanceFamilyResolver.cs`, `TrainingPlan.cs` (`RequestedTargetDistanceKm`/`CanonicalDistanceFamily` fields untouched), and no database migration was added. `GoalDistance.Custom` itself is untouched (still exists, still a valid enum member) — only its silent-resolution behavior changed to fail closed.

## 14. CanonicalDistanceFamilyResolver remains dark

`CatalogNotWiredToGenerationTests` (its own explicit regression guard asserting plan generation does not consume it) passed unchanged as part of the `RuntimeCatalog` subtree run (3842/3842). `CanonicalDistanceFamilyResolverTests` likewise passed unchanged. No production call site was added.

## 15. Riegel infrastructure remains dark

`GoalFeasibilityResolverTests` and `PaceSourceResolverTests` (both exercise the Riegel-based `ResolveFromRecentRace` path) passed unchanged as part of the `RuntimeCatalog` subtree run. No live-wiring was introduced; `RecentRaceInput.Distance == Custom` is now rejected at validation time, before any Riegel computation would occur — this is a request-shape gate, not new race-equivalency logic.

## 16. 16K wrong-plan regression proof (core test)

`GoalDistanceCustomFailClosedTests.RacePreview_GoalDistanceCustom_FailsClosed_NeverFiveKPlan_NeverFiveHundred`: a real HTTP POST to `/api/v1/plans/generate-preview/race` with `goal_distance: "custom"` (standing in for "user typed 16") now returns exactly HTTP 422 with `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`, never 200, never 500; the response body contains no `template_id`/`preview_id`; and `PlanPreviews`/`TrainingPlans` row counts are unchanged before/after (no plan generated, no candidate resolved, no silent 5.0km persistence).

## 17. Additional arbitrary-distance proofs

`RacePreview_GoalDistanceCustom_AcrossLevelFrequencyAndTimeSource_AlwaysFailsClosedIdentically` — 4 additional scenarios (Intermediate/Beginner/Advanced across 3D/4D/5D, both `product_average` and `user_defined` target-time sources) all fail identically with the same typed 422, proving this is a distance-identity gate, not a 16K-pilot-cell coincidence.

## 18. Canonical 5K zero-delta

`ZeroDelta_HabitFiveK_StillSucceeds`: a real `five_k` habit-preview request returns HTTP 200, unaffected by the new validator (since `TryResolve` succeeds for all 4 canonical enum values).

## 19. 10K zero-delta

`ZeroDelta_TenKIntermediate4D_StillSucceeds`: `TEN_K__4D__INTERMEDIATE` at 12 weeks still returns 200 with the identical `template_id`.

## 20. HM zero-delta

`ZeroDelta_HalfMarathonIntermediate4D_StillSucceeds`: `HALF_MARATHON__4D__INTERMEDIATE` at 14 weeks still returns 200 with the identical `template_id`.

## 21. HM 6D zero-delta

`ZeroDelta_HalfMarathonIntermediate6D_StillSucceeds`: the HM-X1.4-activated `HALF_MARATHON__6D__INTERMEDIATE` cell still returns 200 with the identical `template_id`.

## 22. Catalog zero-delta

No catalog JSON file was read for modification or modified. `PlanCatalog.Tests` (1510/1510) is trivially unchanged, confirming this.

## 23. Schema zero-delta

No database migration was added. `TrainingPlan.RequestedTargetDistanceKm`/`CanonicalDistanceFamily` already existed in schema (per DIST-GEN.0 §2) and remain untouched — this phase is entirely request/routing-semantics.

## 24. Recurring defect-family search results

Searched the whole backend for every `GoalDistance`-keyed switch with a `default`/`_` arm producing a canonical value. Found exactly one live offender (`GoalDistanceKm.Resolve`, now fixed). `CatalogGoalDistanceResolver`/`CatalogPrescriptionContextBuilder`'s distance mapping delegates to `GoalDistanceKm.Resolve` for `TenK`/`HalfMarathon` literals only (never called with an arbitrary/unresolved value from a live request after the new gates) and has no independent fallback. `PlanCatalogDomainMapper` is analytical-only. No other recurring instance of this defect family was found.

## 25. Frontend tests

`mobile/test/goal_distance_custom_frontend_guard_test.dart` — 6 widget tests, all passing:
- Typing "16" in `RaceDetailsPage` disables Continue and shows the guard message; recovering to "10.0" re-enables it.
- All four canonical presets (5.0/10.0/21.1/42.2) keep Continue enabled with no guard message (zero UX regression).
- `CustomGoalPage`'s Continue is always disabled with the guard message shown, regardless of stepper interaction.

## 26. Backend tests

`backend/RunningApp.IntegrationTests/GoalDistanceCustomFailClosedTests.cs` — 13 real HTTP+PostgreSQL tests, all passing (race fail-closed + no-plan-created, 4 additional arbitrary-distance scenarios, habit fail-closed, recent-race fail-closed, 2 horizon-unreachability proofs, 3 zero-delta regression proofs, 1 habit zero-delta proof). Plus one updated pre-existing unit test (`GenerateRacePlanPreviewRequestValidatorTests.ProductAverage_Custom_NoCanonicalValue_Throws`, now asserting the new, more precise exception type).

## 27. Full regression results (exact counts)

- `PlanCatalog.Tests`: **1510/1510** passed (trivially unchanged — no catalog file touched).
- `RuntimeCatalog` subtree (`RunningApp.IntegrationTests --filter FullyQualifiedName~RuntimeCatalog`): **3842/3842** passed.
- `PlanGeneration` namespace: **192/192** passed.
- Full monolithic `RunningApp.IntegrationTests`: **4818 total / 4814 passed / 4 failed**.

## 28. Exact durable-baseline comparison

Baseline: 4805 total / 4801 passed / 4 failed. This run: 4818 total (4805 + 13 new tests) / 4814 passed / 4 failed — the delta is exactly the 13 new tests, all passing. The 4 failures are byte-identical by full name to the frozen baseline (verified via a scoped `<UnitTestResult ... outcome="Failed" ...>` tag match cross-checked against the trx's own `<Counters ... failed="4">` element, not a broad grep):

- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

**Zero new regression failures.**

## 29. Temporary custom-distance product contract

As of this phase: `GoalDistance.Custom` is a recognized-but-unsupported enum value. Any request (race or habit) identifying its goal distance — or a reported recent-race distance — as `Custom` is rejected with HTTP 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` before any generation work begins. The only currently supported distances are `five_k`, `ten_k`, `half_marathon`, and `marathon`. Both onboarding screens that can express an arbitrary distance now block Continue with an inline message rather than allowing submission into a request that fails server-side (or, before this phase, silently misfired). No UI copy implies future/"coming soon" support for arbitrary distances — this phase makes no product commitment about DIST-GEN.1's timeline or scope.

## 30. DIST-GEN.1 readiness

`GoalDistance.Custom`, `TrainingPlan.RequestedTargetDistanceKm`, `TrainingPlan.CanonicalDistanceFamily`, and `CanonicalDistanceFamilyResolver` are all untouched and remain exactly as DIST-GEN.0 found them — confirmed via `git diff` showing zero changes to any of these. A future DIST-GEN.1 can freeze 16K numeric authority and wire this scaffolding live without needing to first undo anything from this phase. The new `GeneratePreviewRequestValidator`/`GenerateRacePlanPreviewRequestValidator`/`GenerateHabitPlanPreviewRequestValidator` guards check `GoalDistanceKm.TryResolve`, so the moment a future phase adds a real arm for a new distance (or a distinct mechanism for genuine custom distances), these guards stop rejecting it automatically — no guard needs to be found and manually loosened.

## 31. Remaining blockers

None. The phase's own scope is fully closed: both onboarding UIs block submission for unsupported distances, both backend flows fail closed with a typed, non-500 HTTP 422, canonical 5K/10K/HM/HM-6D behavior is zero-delta, catalog/schema are unchanged, dormant DIST-GEN.1 scaffolding is preserved and untouched, no 16K or custom-distance numeric authority was invented, and the full regression suite shows zero new failures beyond the pre-existing, unrelated frozen baseline of 4.

---

## FINAL CLASSIFICATION

```
CUSTOM_DISTANCE_SILENT_CANONICAL_FALLBACK_ELIMINATED
```

Arbitrary user intent (e.g. typing "16") can no longer silently become a 5K plan or inherit TEN_K's horizon bounds — both the race and habit plan-generation flows fail closed with a typed, non-500 HTTP 422 (`GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`) before any route decision, horizon selection, candidate resolution, volume planning, pace resolution, or calendar materialization runs. `RecentRaceInput.Distance == Custom` is independently guarded. Canonical 5K/10K/HALF_MARATHON (including the HM-X1.4 6D cell) behavior is byte-identical/zero-delta. No catalog, schema, `VolumeSafetyPolicy`, workout-progression, or `RunLayout` file was touched. `CanonicalDistanceFamilyResolver` and the Riegel formula remain dark, tested, and unwired. No 16K or custom-distance numeric authority was invented anywhere in this phase. The full regression suite (4818 tests) shows zero new failures beyond the pre-existing, unrelated, frozen 4-test baseline.
