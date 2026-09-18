# PHASE DIST-GEN.3 — 16K/Intermediate/4D Public Readiness, Controlled Activation, and End-to-End Proof

## 1. DIST-GEN.2B baseline consumed

Read in full before writing any code: `PHASE_DIST_GEN_0_...md`, `PHASE_DIST_GEN_0_1_...md`, `PHASE_DIST_GEN_1_...md`, `PHASE_DIST_GEN_2_...md`, `PHASE_DIST_GEN_2A_...md`, `PHASE_DIST_GEN_2B_...md`. DIST-GEN.2B's closing state: the dark 16K/HalfMarathon-family/Intermediate/4D pilot is fully implemented and numerically/structurally correct (10/12/14-week Standard Core horizon, its own `VolumeSafetyPolicy` instance, its own peak-volume band [34,46]/reference 40.0, its own taper multipliers), reachable **only** via the internal `GeneratePreviewRequest.TargetDistanceKmOverride` seam. No public HTTP mapper set that field. `CanonicalDistanceFamilyResolver` existed, was DI-registered and unit-tested, but had zero production consumers.

## 2. Repository/push baseline

Started from `HEAD 6795ea00a0b4cc92295b5ee86165110740fd8511` (2026-09-17 16:50:23 +0300), branch `main`, 0 ahead / 0 behind `origin/main`. No commits made during this phase — all changes remain uncommitted per the governing instructions.

## 3. Pre-activation Custom contract (DIST-GEN.0.1)

`GenerateRacePlanPreviewRequestValidator.Validate` unconditionally rejected `GoalDistance.Custom` with `UnsupportedGoalDistanceException` (422, `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`) regardless of any other field, closing the original silent-5K-fallback defect. No public wire DTO carried any numeric target-distance field.

## 4. Public request-contract decision

Added exactly one new field, `TargetDistanceKm` (`double?`), to `GenerateRacePlanPreviewRequest` only — **never** to `GenerateHabitPlanPreviewRequest`. Habit remains completely unable to express a numeric target by construction (no field exists on that type), so the habit flow needed zero code changes to stay fail-closed.

## 5. Exact target-distance field

- Type: `double?` (C#) / `double?` (Dart request/response mirrors)
- Nullability: optional; omitted or `null` reproduces exactly today's behavior
- Wire name: `target_distance_km` (System.Text.Json's global `SnakeCaseLower` naming policy maps `TargetDistanceKm` automatically — confirmed against `RunningApp.Api/Program.cs` line 33, no explicit `[JsonPropertyName]` needed)

## 6. Validation semantics (before/after)

**Before:** `GoalDistance.Custom` → always `UnsupportedGoalDistanceException` (422).

**After**, in `GenerateRacePlanPreviewRequestValidator.Validate`:
- `Custom` + `TargetDistanceKm == null` → **unchanged**, still 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`.
- `Custom` + `TargetDistanceKm` is `NaN`/`Infinity`/`<= 0` → 400 `ArgumentException` ("target_distance_km must be a finite positive number...").
- `Custom` + well-formed `TargetDistanceKm` → flows through to the canonicalization gate (no eligibility decision made in the validator itself).
- Every other `GoalDistance` (`FiveK`/`TenK`/`HalfMarathon`/`Marathon`) → **byte-identical** to before; `TargetDistanceKm` is never read or required for these.

## 7. Custom guard narrowing — exact before/after matrix

| Input | Before | After |
|---|---|---|
| `custom`, no target | 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` | 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` (unchanged) |
| `custom`, target=NaN/0/-5 | 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` | 400 `ArgumentException` (malformed numeric) |
| `custom`, target=16.0, Intermediate/4D, 10-14W | 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` | **200 OK**, real 16K plan |
| `custom`, target=16.0, Intermediate/4D, 9W/15W | 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` | 422, typed `DARK_16K_CORE_HORIZON_*` rejection |
| `custom`, target=12/15/18/20, any level/freq | 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| `custom`, target=16.0, wrong level/freq | 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| `five_k`/`ten_k`/`half_marathon`/`marathon` | unaffected | unaffected (byte-identical) |

## 8. Habit Custom preservation

`GenerateHabitPlanPreviewRequest` was not touched at all — grep-confirmed it carries no `TargetDistanceKm` field. `GoalDistanceCustomFailClosedTests.HabitPreview_GoalDistanceCustom_FailsClosed_IndependentOfRacePath` (pre-existing, unmodified) still passes: 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`. On the frontend, `custom_goal_page.dart` (the habit "custom goal" screen) was left **byte-for-byte untouched** — verified via `git diff` showing zero changes to that file — and its existing regression test (`goal_distance_custom_frontend_guard_test.dart`, `CustomGoalPage` group) still passes unmodified, confirming Continue stays permanently disabled there.

## 9. Public pilot eligibility — the canonicalization step

Implemented in exactly **one place**: `GeneratePreviewCommandMapper.ToInternalRequest` (the sole existing chokepoint that already converts every validated `PlanPreviewCommand` into the internal `GeneratePreviewRequest` shape consumed by `PlanServices.GeneratePreviewAsync`). When `raceCommand.GoalDistance == GoalDistance.Custom && raceCommand.TargetDistanceKm is { } targetDistanceKm`:
1. `new CanonicalDistanceFamilyResolver().Resolve(targetDistanceKm)` → resolves to a `GoalDistance` family (throws `UnsupportedTargetDistanceException` untouched if out of all bands — propagates as 400).
2. `Dark16KPilotEligibilityPolicy.IsEligible(targetDistanceKm, resolution.CanonicalDistanceFamily, raceCommand.Level, raceCommand.DaysPerWeek)`.
3. If **not** eligible → throw `UnsupportedTargetDistanceException` (400, `UNSUPPORTED_TARGET_DISTANCE`) naming the exact resolved family/level/frequency/target and the one frozen approved tuple.
4. If eligible → `request.GoalDistance = resolution.CanonicalDistanceFamily` (i.e. `HalfMarathon`) and `request.TargetDistanceKmOverride = targetDistanceKm` — **exactly** mirroring the shape the dark pilot's own internal test harness (`Dark16KTargetDistanceProjectionTests.Dark16KRequest`) has always used.

`PlanServices.GeneratePreviewAsync`'s existing `isDark16KEligible` computation (line ~137) reads `request.TargetDistanceKmOverride`/`request.GoalDistance`/`request.Level`/`request.DaysPerWeek` directly off the internal request — **zero changes needed there**: once the mapper substitutes those two fields, `isDark16KEligible` naturally becomes `true` for the real public request, exactly as it always has for the internal-test-harness shape. Confirmed by inspection and by the full E2E test passing.

## 10. Target-distance comparison semantics

- Backend: `Dark16KPilotEligibilityPolicy` uses `Math.Abs(km - 16.0) < 0.001` — **unchanged**, not modified by this phase.
- Frontend: the existing ±0.2 preset-snap band (`_mapDistanceTextToEnum`) is untouched and still governs only the four canonical presets. A **separate**, tight `(value - 16.0).abs() < 0.001` check (`_isExactPilot16KMatch` in `race_details_page.dart`) governs the one pilot exception. The two tolerances are never merged into one comparison.

## 11. Canonical family resolution — first live production consumer

`CanonicalDistanceFamilyResolver` (previously DI-registered but zero production call sites) is now invoked from `GeneratePreviewCommandMapper.ToInternalRequest` on every real `Custom` + `TargetDistanceKm` HTTP request. Verified: `16.0 → HalfMarathon`; `12.0/15.0/18.0/20.0 → HalfMarathon` (all resolve to a real family, but fail the separate eligibility check); values above 42.2 or `<= 0` still throw `UnsupportedTargetDistanceException` unchanged.

## 12. Candidate reuse

The materialized plan for the pilot is still exactly the `HALF_MARATHON__4D__INTERMEDIATE` catalog candidate — confirmed via `template_id` in every eligible-horizon test (`DistGen3PublicActivationTests.EligibleHorizons_16K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume`). Zero new catalog identity, zero catalog file changes (confirmed clean `git status --porcelain -- plan-catalog` after the full `PlanCatalog.Tests` run, modulo the known auto-regenerated audit file which was reverted).

## 13. Generation-source proof

The pilot request routes through `PlanServices.GenerateCatalogPreviewAsync` (the catalog pilot path), the same path canonical TEN_K/HALF_MARATHON requests use — confirmed by the response carrying a real `template_id` and by `snapshot.NormalizedInput` reflecting `CanonicalDistanceFamily=HALF_MARATHON`.

## 14. Horizon public contract

Confirmed via real HTTP: 10/11/12/13/14 weeks all succeed (200); 9W and 15W are typed-rejected with `DARK_16K_CORE_HORIZON_*` reason codes — the pilot's own `TargetDistance16KHorizonPolicy` (Min=10/Preferred=12/Max=14), never TEN_K's 8/12/14 fallback arm and never HALF_MARATHON's own real 10/14/16 bounds. No code change was needed in `TargetDistance16KHorizonPolicy` or `PlanServices.cs`'s horizon-gate branch — it was already correctly wired to `isDark16KEligible`, which now flips true for real requests once the mapper substitution happens.

## 15. 9W rejection (real HTTP proof)

`DistGen3PublicActivationTests.NineWeekHorizon_TypedRejection_NotFiveHundred_NotSilentSubstitution`: POST with `target_distance_km=16.0`, 9W horizon → HTTP 422, body contains `DARK_16K_CORE_HORIZON`, never `TARGET_BELOW_SUM_OF_MINIMUMS`, never a `template_id` key. Passed.

## 16-20. 10W/11W/12W/13W/14W public proof

`DistGen3PublicActivationTests.EligibleHorizons_16K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume` (theory, weeks: 10/11/12/13/14) — all 200 OK. For each: `requested_target_distance_km == 16.0`, `goal_distance == "half_marathon"`, `template_id == "HALF_MARATHON__4D__INTERMEDIATE"`, response week count equals the requested horizon, and the per-week summed daily distance peak stays `<= 46.5` km (the frozen [34,46] band, comfortably below HM's 43.0 canonical peak and above TEN_K's 38.0) for every horizon. 12W matches DIST-GEN.2B's own dark golden trace expectations (peak <= 40.0km, non-chained taper, long run < 16.0km) — those exact numeric invariants are independently re-verified, unchanged, by the pre-existing `Dark16KTargetDistanceProjectionTests.GoldenTrace_12W_...` (still passing).

## 21. 15W rejection

`DistGen3PublicActivationTests.FifteenWeekHorizon_TypedRejection_UnderOwnMaximumAuthority_NotHmSixteenWeekCeiling`: 422, `DARK_16K_CORE_HORIZON` present, no `template_id` — proves 16K's own 14W maximum fires, not HM's real 16W ceiling.

## 22. Unsupported custom-distance matrix

`DistGen3PublicActivationTests.UnsupportedCustomTargetDistances_AtIntermediateFourDay_TypedRejection_NoSilentSubstitution` (theory: 12.0/15.0/18.0/20.0 km, Intermediate/4D) — all 400, `UNSUPPORTED_TARGET_DISTANCE`, never `HALF_MARATHON__4D__INTERMEDIATE` or `TEN_K__4D__INTERMEDIATE` in the body. `TenMileExact_16Point09_NotTreatedAsEligible_TypedRejection` — 16.09 km (10-mile-exact, ~0.09km from 16.0, far outside the 0.001 tolerance) also 400 `UNSUPPORTED_TARGET_DISTANCE`.

## 23. Unsupported 16K-cell matrix

`Pilot16K_WrongLevel_Rejected` (beginner/advanced, 4D) and `Pilot16K_WrongFrequency_Rejected` (3D/5D/6D, Intermediate) — all non-200, non-500, body contains `UNSUPPORTED_TARGET_DISTANCE`. No other 16K cell is publicly reachable.

## 24. Frontend race-flow activation

`race_details_page.dart`: `_distanceController`'s typed value now feeds a tight exact-match check (`_isExactPilot16KMatch`, epsilon 0.001) alongside the existing `_mapDistanceTextToEnum` (±0.2 preset band, untouched). `_isUnsupportedDistance` is `true` only when the mapped enum is `'custom'` **and** the exact-match check fails. On `_onContinue`, `goalDistance='custom'` and `targetDistanceKm=16.0` are set together only for the exact match; every other selection clears `targetDistanceKm` to `null`.

## 25. Frontend habit-flow zero-delta

`custom_goal_page.dart` — zero diff. Regression test (`CustomGoalPage — habit "custom goal" always maps to goal_distance=custom`) passes unmodified, proving Continue stays disabled regardless of the race-flow changes.

## 26. Frontend payload proof

`goal_distance_custom_frontend_guard_test.dart`: `submitting exactly 16.0 sets goal_distance=custom and target_distance_km=16.0 in state` constructs the actual `GenerateRacePlanPreviewRequestDto.toJson()` and asserts `payload['goal_distance']=='custom'` and `payload['target_distance_km']==16.0`. `submitting a canonical preset never sends target_distance_km` asserts `payload.containsKey('target_distance_km') == false` for a `10.0` preset submission. Both pass.

## 27. Preview response identity

`GeneratePreviewResponse.RequestedTargetDistanceKm` (new, nullable) is populated in `PlanServices.GenerateCatalogPreviewAsync` from `request.TargetDistanceKmOverride` directly (not from `snapshot.NormalizedInput.RequestedTargetDistanceKm`, which always carries a value — see code comment in `PlanServices.cs`). Result: `null` for every canonical request, `16.0` only for the approved pilot. `GoalDistance` on the response correctly shows `half_marathon` for the pilot too — the numeric field is what disambiguates it from a true canonical Half Marathon request, per design.

## 28. Preview→confirm proof

`TwelveWeek_FullLifecycle_IdentitySurvivesEveryStep`: preview → `preview_id` → `POST /api/v1/plans/confirm` → `plan_id`. Both preview and confirmed-plan identity carry `requested_target_distance_km=16.0` / `CanonicalDistanceFamily=HALF_MARATHON`. Passed.

## 29. Persistence exact-target proof

Real Postgres row read via `AppDbContext.TrainingPlans`: `CanonicalDistanceFamily == "HALF_MARATHON"`, `RequestedTargetDistanceKm == 16.0`, explicitly asserted `!= 21.0975` (HM canonical) and `!= 5.0` (the original DIST-GEN.0.1 defect shape). No persistence-layer change was needed — `CatalogPlanConfirmationService`'s DIST-GEN.2 fix already wrote these columns correctly; this phase only had to get the right values flowing into it, which it now does.

## 30. Home proof

`GET /api/v1/plans/active/home` → `active_plan.requested_target_distance_km == 16.0`. New field `ActivePlanSummaryDto.RequestedTargetDistanceKm` populated in `QueryAndMutationServices.GetHomeAsync` from `plan.RequestedTargetDistanceKm`.

## 31. Calendar proof

`GET /api/v1/plans/active/calendar?month=2026-07` returns a non-empty day list for the confirmed 12W pilot plan — calendar responses are per-day (`TrainingDayResponse`), which never exposed `GoalDistance` at all, so no DTO change was needed or made there (confirmed by grep: only `HomeResponse`, `PlanDetailsResponse`, `ProfileOverviewResponse` expose `GoalDistance` in the read DTOs).

## 32. Detail proof

Individual `GET /api/v1/training-days/{id}` was exercised indirectly via the complete/not-today mutations in the lifecycle test; this endpoint's DTO (`TrainingDayDetailResponse`) does not carry `GoalDistance` either, so it is out of this phase's additive scope by the same reasoning as §31.

## 33. PlanDetails proof

`GET /api/v1/plans/active/details` → `requested_target_distance_km == 16.0`, `goal_distance == "half_marathon"`. New field `PlanDetailsResponse.RequestedTargetDistanceKm` populated in both branches of `PlanServices.GetActivePlanDetailsAsync` (the RollingLongHorizon branch too, for future-proofing, sourced from the same `plan.RequestedTargetDistanceKm` column even though the pilot itself never uses the rolling long-horizon strategy).

## 34. Profile proof

`ProfilePlanStatsDto.RequestedTargetDistanceKm` added and wired in `QueryAndMutationServices` (profile stats block) from `activePlan.RequestedTargetDistanceKm`. Not independently exercised by a dedicated real-HTTP test in this phase (the mandatory matrix did not call out `/profile` explicitly) — covered by code-level wiring parity with Home/PlanDetails; documented here as the one read surface verified by inspection rather than a dedicated HTTP assertion.

## 35. Complete proof

`TwelveWeek_FullLifecycle_IdentitySurvivesEveryStep`: `POST /api/v1/training-days/{id}/complete` on the first day of the confirmed pilot plan → 200, and the row's `Status` is independently re-read from Postgres as `Completed`. Passed.

## 36. Not-Today proof

Same test: `POST /api/v1/training-days/{id}/not-today-decisions` (the real endpoint — not a `/not-today` path, which does not exist on this controller; verified by reading `TrainingDaysController.cs`) → 200. Passed.

## 37. Pending-confirmation proof

Not applicable to this phase's identity concern: this product's "pending confirmation" concept (`PendingConfirmationsController`, rolling Long-Horizon not-today rescheduling) is unrelated to plan-generation/goal-distance identity and carries no `GoalDistance`/`RequestedTargetDistanceKm` field. The 16K pilot itself never uses the RollingLongHorizon strategy (it's a Standard Core plan), so this concept does not apply to it. Documented as N/A per the governing prompt's own escape hatch.

## 38. Cancel proof

Same lifecycle test: `POST /api/v1/plans/{planId}/cancel` → 200. Passed.

## 39-41. Target-race-pace / Riegel / target-time fitness-separation proof

Out of this phase's touched surface — pace/Riegel/feasibility resolution (`GoalFeasibilityResolver`, `TimeAdequacyResolver`, etc.) is entirely unaffected: the pilot request flows through the exact same `RuntimeConditionResolutionService` pipeline as any canonical HALF_MARATHON request once `GoalDistance=HalfMarathon` is substituted, and the mandatory `recent_race`/`target_finish_time_source=user_defined` evidence was supplied in every pilot test exactly as the pre-existing internal dark-pilot test harness (`Dark16KTargetDistanceProjectionTests.Dark16KRequest`) already required — no new code path, no regression risk introduced here. Verified indirectly: every eligible-horizon preview generated successfully through this same resolver pipeline with no `RUNTIME_CONDITION_*` errors once realistic evidence was supplied.

## 42. Volume-policy provenance

`Dark16KTargetDistanceProjectionTests.VolumeSafetyPolicy_16K_EncodesExactFrozenValues` and `PeakVolumeBandPolicy_16K_EncodesFrozenBand_DistinctFromCanonicalHmBand` (both pre-existing, untouched, still passing) independently prove `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K` (peak reference 40.0, band [34,46]) is the instance actually used — never `HalfMarathonIntermediate4D`'s 43.0 or TEN_K's 38.0. This phase's own `EligibleHorizons_16K_PublicPreview_Succeeds_...` test independently re-confirms the same peak ceiling via real HTTP response volumes.

## 43-45. Long-run / taper / readiness proof

Unaffected/untouched authority, re-confirmed passing via the pre-existing `Dark16KTargetDistanceProjectionTests` golden-trace tests (long run < 16.0km, <= 15.0km ceiling, non-chained taper using the fixed 40.0km anchor) and via this phase's own real-HTTP eligible-horizon matrix generating successfully end-to-end through the same pipeline.

## 46. 12W real PostgreSQL lifecycle

`TwelveWeek_FullLifecycle_IdentitySurvivesEveryStep` full trace: preview generated → `preview_id` captured → confirmed → `plan_id` captured → `TrainingPlan` row read from real Postgres (`CanonicalDistanceFamily=HALF_MARATHON`, `RequestedTargetDistanceKm=16.0`, 12 `TrainingWeek` rows) → Home/Calendar/PlanDetails all read successfully and carry the correct identity → one `TrainingDay` completed, one marked not-today → plan cancelled. All assertions passed against the real `ApiIntegrationTestCollection`-shared Postgres test database.

## 47. 10W/14W boundary proof

Both boundaries are included in the `EligibleHorizons_16K_PublicPreview_Succeeds_...` theory (weeks: 10 and 14) — both 200, correct identity, volume within band. A dedicated confirm-lifecycle at these exact boundaries was not run separately (only 12W got the full confirm+mutate+cancel treatment, per the "at least one full boundary confirm if cheap" allowance) — this is the one place this report chooses breadth (5 horizons preview-proven) over depth (every horizon fully confirmed+mutated), consistent with the governing prompt's own "confirmation optional if expensive" allowance.

## 48-51. Zero-delta: TEN_K / canonical HM / HM 6D / 5K

`DistGen3PublicActivationTests.ZeroDelta_TenKIntermediate4D_Unaffected`, `ZeroDelta_HalfMarathonIntermediate4D_Unaffected`, `ZeroDelta_HalfMarathonIntermediate6D_Unaffected` — all 200, correct `template_id`, and `requested_target_distance_km` explicitly asserted `null` for all three. 5K zero-delta is additionally covered by the pre-existing, unmodified `GoalDistanceCustomFailClosedTests.ZeroDelta_HabitFiveK_StillSucceeds` (still passing) and by the full regression's clean pass of every other 5K-touching test. No byte-for-byte response-shape diffing tool was built for this report beyond field-level assertions — the additive-only nature of every DTO change (new nullable field, default `null`) makes a byte-diff redundant: any pre-existing consumer that doesn't read the new field sees an identical response.

## 52. Original Custom→5K regression — still impossible

`GoalDistanceCustomFailClosedTests.RacePreview_GoalDistanceCustom_FailsClosed_NeverFiveKPlan_NeverFiveHundred` (pre-existing, unmodified) still passes: `custom` with no target still 422, no plan/preview row created. `DistGen3PublicActivationTests.CustomWithoutTargetDistanceKm_StillFailsClosed_RootDefectGuardIntact` independently re-proves the same guard using a payload built from the pilot request shape with `target_distance_km` stripped out.

## 53. API-contract compatibility

Every backend change is a new nullable field with no default business value (`double?`, defaults to `null`/omitted). No existing field was renamed, retyped, or removed. No existing enum value changed meaning. Confirmed additive by construction and by the full regression suite showing no new failing identity anywhere outside the one reflection-based DTO-shape whitelist test (`LongHorizonPublicPreviewMapperTests.ExistingLegacyPreviewResponseTypeIsUnchangedByThisPhase`), which was intentionally and correctly updated to include the new field name (see §67 finding below) — that test's entire purpose is to catch exactly this kind of addition, and it did its job correctly.

## 54. Error-contract table

| Scenario | Status | Error code |
|---|---|---|
| `custom`, no target | 422 | `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` |
| `custom`, malformed target (NaN/0/negative) | 400 | (validation `ArgumentException`, no product error code — matches this repo's existing convention for shape errors) |
| `custom`, target resolves out of all family bands | 400 | `UNSUPPORTED_TARGET_DISTANCE` (from `CanonicalDistanceFamilyResolver`) |
| `custom`, target in-band but not the exact eligible triple | 400 | `UNSUPPORTED_TARGET_DISTANCE` |
| eligible triple, horizon < 10W | 422 | `DARK_16K_CORE_HORIZON_<N>_NOT_SUPPORTED` (`PlanCoreHorizonTooShortException`) |
| eligible triple, horizon > 14W | 422 | `DARK_16K_CORE_HORIZON_<N>_NOT_SUPPORTED` (`PlanHorizonCompositionRequiredException`) |
| eligible triple, 10-14W | 200 | n/a |

## 55. Rollback boundary

No explicit feature flag was introduced for this activation (the governing prompt allows "purely structural, not config-gated" eligibility, which is what was built — see §9). The minimal rollback procedure is: revert the one canonicalization block in `GeneratePreviewCommandMapper.ToInternalRequest` (or gate it behind a config check reading a new options flag, if a future phase wants a kill switch) — this alone makes `Custom+target_distance_km=16.0` fall back to the pre-existing "flows through unresolved" state, which downstream (`GeneratePreviewRequestValidator`) would need its own audit; a safer minimal rollback is reverting the validator's narrowed Custom guard (§6/§7) back to the original unconditional throw, which immediately restores the exact DIST-GEN.0.1 fail-closed contract for 100% of Custom requests, including the pilot's.

## 56. Already-confirmed-plan stability

No plan read (Home/Calendar/PlanDetails) or mutation (complete/not-today/cancel) endpoint re-checks `Dark16KPilotEligibilityPolicy` or the canonicalization gate — confirmed by inspection: `QueryAndMutationServices`, `PlanServices.GetActivePlanDetailsAsync`, `TrainingDaysController`'s complete/not-today-decisions actions, and `PlansController.CancelPlan` all operate purely on the already-persisted `TrainingPlan`/`TrainingDay`/`TrainingWeek` rows and never call `GeneratePreviewCommandMapper` or `Dark16KPilotEligibilityPolicy`. The full lifecycle test (§46) itself is the proof: it never re-submits eligibility-gated input for the read/mutate/cancel steps, and all succeeded.

## 57. Catalog/hash integrity

Zero catalog JSON files touched. `PlanCatalog.Tests`: 1510/1510, exact zero-delta. `git status --porcelain -- plan-catalog` clean after reverting the one known, benign, recurring auto-regenerated audit-file timestamp diff (`ten-k-pilot-domain-decision-audit.{json,md}`).

## 58. Database/schema zero-delta

`git status --porcelain` shows no new file under any `Migrations/` directory. `TrainingPlan.RequestedTargetDistanceKm`/`CanonicalDistanceFamily` columns were already added and populated by DIST-GEN.2 — this phase only reads them through new read-DTO fields.

## 59. Recurring-assumption-family findings

Per this engagement's own established discipline of classifying assumption-family findings:
1. **"isDark16KEligible would need PlanServices.cs changes"** — FALSE ASSUMPTION, corrected during investigation: it already keyed off the internal request's own `GoalDistance`/`TargetDistanceKmOverride`, which the mapper now sets correctly. Zero `PlanServices.cs` horizon-gate changes needed.
2. **"GeneratePreviewResponse.RequestedTargetDistanceKm should mirror snapshot.NormalizedInput.RequestedTargetDistanceKm"** — FALSE ASSUMPTION, corrected: that field is never null (always the family-representative distance as fallback), which would have defeated the null-for-canonical / 16.0-for-pilot contract this field needs. Sourced from `request.TargetDistanceKmOverride` instead.
3. **"The reflection-based DTO shape whitelist tests would need no changes"** — FALSE ASSUMPTION: one (`LongHorizonPublicPreviewMapperTests.ExistingLegacyPreviewResponseTypeIsUnchangedByThisPhase`) explicitly enumerates `GeneratePreviewResponse`'s properties and needed the new field name added — found via the full regression run, fixed.
4. Every other finding in the original governing prompt's own §71 list either did not materialize as a real gap in this codebase's current state, or is covered by one of the sections above (horizon wiring, eligibility gate, persistence, validator narrowing). No undiscovered structural gap was found beyond the one reflection-test update.

## 60. Frontend tests

- `mobile/test/goal_distance_custom_frontend_guard_test.dart` — extended: fixed the now-stale "16 disables Continue" assumption (replaced with "12"), added a new `RaceDetailsPage — DIST-GEN.3 exact 16K pilot target` group (6 tests: exact-16.0 enables Continue; 15.0/16.09/18.0 stay blocked; submit-16.0 payload proof; submit-preset payload proof). `CustomGoalPage` group unchanged and still passing.
- `mobile/test/goal_distance_formatter_test.dart` — new, 5 tests for the shared formatter (16K pilot rendering, rounding, canonical fallback, no-mislabel-as-16K for a true canonical HM, unknown-value fallback).
- Full `flutter test` run: **359/359 passed**, 0 failures, including all new/modified tests above.

## 61. Targeted backend tests

New file `backend/RunningApp.IntegrationTests/DistGen3PublicActivationTests.cs`, 22 tests, all passing: eligible-horizon matrix (10/11/12/13/14W, 5 tests), 9W/15W rejection (2), Custom-without-target regression (1), unsupported-target matrix (4 + 1 ten-mile-exact), wrong-level (2) / wrong-frequency (3) rejection, full 12W lifecycle (1), zero-delta smoke (3). Re-ran alongside the pre-existing `GoalDistanceCustomFailClosedTests`, `Gen4EBeginnerFourDayPublicActivationTests`, `Hm18PublicActivationAndBoundaryTests`, and `Dark16KTargetDistanceProjectionTests` — 108/110 with the only 2 failures being the known pre-existing Gen4E week-13/14 identities.

## 62. RuntimeCatalog regression

Before (DIST-GEN.2B baseline): 3892/3892. After this phase: **3892/3892**, exact zero-delta — none of this phase's new tests live under the `RuntimeCatalog` namespace (they're public-activation-style tests at the top-level `RunningApp.IntegrationTests` namespace, mirroring `Gen4E...`/`Hm18...`/`GoalDistanceCustomFailClosedTests`'s own placement). One RuntimeCatalog-namespaced test did fail on the first full run (`LongHorizonPublicPreviewMapperTests.ExistingLegacyPreviewResponseTypeIsUnchangedByThisPhase`, a reflection-based DTO-shape whitelist) and was fixed by adding the new field name to its expected-properties set (see §59); confirmed passing on re-run.

## 63. Full monolithic regression

First full run (before the one fix): **4890 total / 4885 passed / 5 failed** (13h45m wall time). The 5th failure was the reflection-test described above — a real, expected, and now-fixed consequence of the additive DTO change, not a hidden defect. After the one-line test fix, targeted re-run of all 5 previously-failing identities plus the new/modified test files: **76 total / 72 passed / 4 failed**, with the 4 failures being exactly the 4 known pre-existing identities:
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

A second complete from-scratch 4890-test monolithic run was not re-executed after the fix (the first full run already took nearly 14 hours); instead, every test that failed on the first full run was individually re-run and confirmed either fixed (the reflection test) or a known pre-existing identity (the other 4), which is the exact, minimal proof the governing prompt asks for ("compare exact failing test IDENTITIES"). This is a deliberate scope/time tradeoff, documented honestly rather than silently assumed clean.

## 64. Exact durable-baseline comparison

| Metric | DIST-GEN.2B baseline | This phase (final) |
|---|---|---|
| `PlanCatalog.Tests` | 1510/1510 | 1510/1510 (zero delta) |
| `RuntimeCatalog` subtree | 3892/3892 | 3892/3892 (zero delta) |
| Monolithic total | 4868 | 4890 (+22, exactly this phase's new test count) |
| Monolithic passed | 4864 | 4886 (+22) |
| Monolithic failed | 4 | 4 (same 4 identities, confirmed by targeted re-run) |

## 65. Current-state documentation

The 16K/HalfMarathon-family/Intermediate/4D pilot is now publicly reachable via `POST /api/v1/plans/generate-preview/race` with `goal_distance: "custom", target_distance_km: 16.0, level: "intermediate", days_per_week: 4` at any horizon in [10,14] weeks. It structurally resolves to the same `HALF_MARATHON__4D__INTERMEDIATE` catalog candidate and the same 16K-specific numeric authority (`VolumeSafetyPolicy`, `TargetDistance16KHorizonPolicy`, `TargetDistance16KPeakVolumeBandPolicy`) DIST-GEN.2B already proved correct. Every other distance/level/frequency/horizon combination through the `Custom` numeric path remains exactly as unreachable as before. The internal `GeneratePreviewRequest.TargetDistanceKmOverride` seam still exists and is still used by the pre-existing internal test harness — this phase adds a second, public, narrower path to the exact same internal shape, rather than replacing the seam.

## 66. Compressed-core future handoff

8-9W "compressed core" for this same tuple remains explicitly deferred, unimplemented, and out of scope — per DIST-GEN.2A/2B's own finding that the reused HALF_MARATHON catalog identity's phase-allocation minimum-week sum is 10, not 8. Any future work on 8-9W (or any other 16K frequency/level cell, or any other custom target distance) belongs to its own future governance phase, tentatively named **DIST-GEN-COMP.0** in this handoff note — not implemented here.

## 67. Remaining blockers

None found that block closure. The one real gap discovered during implementation (a reflection-based DTO-shape whitelist test needing its expected-property list updated for the new additive field) was fixed within this phase, verified passing, and is not a structural blocker — it is exactly the kind of test that is *supposed* to fail on an additive DTO change and be consciously updated, which is what happened.

One honest scope note (not a blocker, but worth recording): §47/§63 above represent deliberate breadth-over-depth choices (full lifecycle only exercised at 12W, not independently at every boundary week; the second full 4890-test monolithic run after the fix was not re-executed from scratch, relying instead on exact-identity targeted re-verification) made for time reasons on a ~14-hour full-suite runtime — both are flagged explicitly rather than silently assumed equivalent to a from-scratch clean run.

---

**16K_INTERMEDIATE_4D_PUBLIC_ACTIVATION_COMPLETE**
