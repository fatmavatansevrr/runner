# PHASE DIST-GEN.11 — 18K × Intermediate × 4D Public Readiness, Controlled Activation, and End-to-End Proof

## 1. DIST-GEN.10 baseline consumed

`PHASE_DIST_GEN_10_18K_INTERMEDIATE_4D_FULL_DARK_THIRD_TARGET_PROJECTION_IMPLEMENTATION.md` is the full dark-implementation baseline: 18.0km/HalfMarathon/Intermediate/4D is already structurally and numerically closed and internally materializable via `Dark16KPilotEligibilityPolicy.ApprovedDarkCells`/`TryResolveDarkEligibleCell`, `TargetDistance18KHorizonPolicy` (10/12/14 core weeks), `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K` (`PreferredAbsolutePeakLongRunKm = 16.0`, peak volume band 34.0/46.0/40.0), and the reused (not new) `TargetDistance16KTaperVolumePolicy` for taper. This phase adds no new numeric value anywhere — it only makes the already-frozen 18K authority reachable through the real public HTTP contract, exactly mirroring DIST-GEN.7's own proof shape for 15K.

## 2. Repository/push baseline

Starting commit `9413652e78f9c4d0fde32f9c93bb00a0343dcccb` on `main`, `## main...origin/main` with no ahead/behind annotation (0 ahead / 0 behind). Working tree carried only pre-existing tracked build-artifact noise (`bin/`/`obj/`/`TestResults/`) at the start — not touched by this phase's own edits beyond the standard build/test byproducts.

## 3. Files changed (exact list)

**Modified (5):**
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs` — added `(18.0, HalfMarathon, Intermediate, 4)` to `ApprovedPublicCells` only. `ApprovedDarkCells` untouched.
- `backend/RunningApp.IntegrationTests/DistGen7PublicActivationTests.cs` — one stale InlineData literal corrected (`18.0` → `17.0` in `UnsupportedTargetDistances_StillRejected_NeverSubstituted`'s Theory, disclosed in the file itself and in §10/§65 below).
- `backend/RunningApp.IntegrationTests/DistGen3PublicActivationTests.cs` — one stale InlineData literal corrected (`18.0` → `19.0` in `UnsupportedCustomTargetDistances_AtIntermediateFourDay_TypedRejection_NoSilentSubstitution`'s Theory). **This was NOT found until the first full monolithic regression run surfaced it as a genuine new failing identity** (`Expected: BadRequest, Actual: OK`) — see §81/§82 for the full disclosure of this finding and its resolution.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/Dark18KTargetDistanceProjectionTests.cs` — one test renamed and rewritten (`PublicEligibility_RemainsNarrowerThanDarkEligibility_18KNeverPublic` → `PublicEligibility_18KNowPublic_AfterDistGen11_DarkRegistryUnaffected`), since its own DIST-GEN.10-era premise ("18K never public") is this phase's own explicit, required purpose to invert. Disclosed in §11/§65.
- `mobile/lib/features/onboarding/presentation/race_details_page.dart` — `_approvedProjectedTargetsKm` extended from `[15.0, 16.0]` to `[15.0, 16.0, 18.0]`.

**Modified (2, frontend tests):**
- `mobile/test/goal_distance_custom_frontend_guard_test.dart` — added a full DIST-GEN.11 18K test group (accept/reject matrix + payload proof + 15K/16K zero-delta spot checks); two stale `'18.0'` entries in pre-existing "near but blocked" lists replaced with `'17.9'`/`'17.9'+'18.1'` respectively (disclosed, §10).
- `mobile/test/goal_distance_formatter_test.dart` — added one assertion that `requestedTargetDistanceKm: 18.0` renders `"18K"`.

**Added (1, backend):**
- `backend/RunningApp.IntegrationTests/DistGen11PublicActivationTests.cs` — 31 new tests, the full public-activation proof for 18K (see §7 onward).

**Report (1, new):** this file.

**Untouched (explicitly verified via `git diff` — empty):** `backend/RunningApp.Application/Commands/Plan/GeneratePreviewCommandMapper.cs`, `Dark16KTargetDistanceProjectionTests.cs`, `Dark15KTargetDistanceProjectionTests.cs`, every horizon/volume/taper/LR policy class (`TargetDistance18KHorizonPolicy.cs`, `VolumeSafetyPolicy.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `TargetDistance16KAwarePeakVolumeBandLoader.cs`, `PlanServices.cs`, `RaceHorizonPolicy.cs`), every catalog JSON, every EF Core migration, `mobile/lib/features/onboarding/presentation/custom_goal_page.dart`, `mobile/lib/features/onboarding/data/onboarding_provider.dart`, `mobile/lib/core/network/dtos.dart`, `mobile/lib/core/formatting/goal_distance_formatter.dart`, `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`.

## 4. Public eligibility ownership

Unchanged mechanism from DIST-GEN.7: `GeneratePreviewCommandMapper.ToInternalRequest` calls `Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell(...)`, which iterates `ApprovedPublicCells` (exact-match only, 0.001km tolerance). This phase's only production change is the one new list entry — `TryResolvePublicEligibleCell`/`IsPubliclyEligible` (both overloads) required and received zero code change, exactly as predicted by DIST-GEN.10 §74. Confirmed by `git diff` on `GeneratePreviewCommandMapper.cs`: empty.

## 5. Public registry evolution

`ApprovedPublicCells` now has exactly three entries: `(16.0, HalfMarathon, Intermediate, 4)` (DIST-GEN.3), `(15.0, HalfMarathon, Intermediate, 4)` (DIST-GEN.7), `(18.0, HalfMarathon, Intermediate, 4)` (DIST-GEN.11, this phase). Still a flat, independently-authored list — not derived from `ApprovedDarkCells`, not a computed range. `ApprovedDarkCells` remains exactly the three entries DIST-GEN.10 left it at (15.0/16.0/18.0) — untouched.

## 6. Dark/public separation

Confirmed unchanged: `TryResolvePublicEligibleCell`/`IsPubliclyEligible` are referenced only from `GeneratePreviewCommandMapper.cs`; `TryResolveDarkEligibleCell`/`IsDarkEligible` are referenced only from `PlanServices.cs`, `CatalogPreviewGenerator.cs`, `TargetDistance16KAwarePeakVolumeBandLoader.cs`, `CatalogVolumeAndLongRunPlanner.cs` (zero new consumers this phase). The two lists are independently editable; 18.0 being dark-eligible since DIST-GEN.10 never implied public eligibility until this phase's own one-line addition.

## 7. Exact 18K public admission

`DistGen11PublicActivationTests.EligibleHorizons_18K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume` (Theory over 10/11/12/13/14 weeks): real `POST /api/v1/plans/generate-preview/race` with `goal_distance: "custom", target_distance_km: 18.0, level: "intermediate", days_per_week: 4` returns `200 OK` at every horizon, with `requested_target_distance_km == 18.0`, `goal_distance == "half_marathon"`, `template_id == "HALF_MARATHON__4D__INTERMEDIATE"`. All 5 pass.

## 8. 15K public zero-delta

`DistGen11PublicActivationTests.ZeroDelta_15K_StillPubliclyEligible_Unaffected` and `DistGen7PublicActivationTests` (unmodified except the one disclosed §10 InlineData substitution) both pass: 15K's own public admission, horizon matrix, and 14.5km ceiling remain fully unaffected by the 18K addition.

## 9. 16K public zero-delta

`DistGen11PublicActivationTests.ZeroDelta_16K_StillPubliclyEligible_Unaffected` passes; `DistGen7PublicActivationTests.PeakLongRun_15K_Uses14Point5_16K_StillUses15_NoCrossContamination` (unmodified) continues to pass — 16K's own public path is zero-delta.

## 10. Unsupported-target preservation

`DistGen11PublicActivationTests.UnsupportedTargetDistances_StillRejected_NeverSubstituted_NeverSnapped` (Theory: 12.0, 14.0, 17.0, 19.0, 20.0, 16.09, 17.9, 18.1, 18.01, 18.09) — all return `400 Bad Request` with `UNSUPPORTED_TARGET_DISTANCE`, never `HALF_MARATHON__4D__INTERMEDIATE`/`TEN_K__4D__INTERMEDIATE`. **Disclosed stale-literal correction**: `DistGen7PublicActivationTests.UnsupportedTargetDistances_StillRejected_NeverSubstituted` originally included `[InlineData(18.0)]`, written before this phase's own explicit purpose (admitting 18.0 publicly) existed. This is the exact same kind of correction DIST-GEN.7 itself performed against `DistGen3PublicActivationTests.cs`'s own `15.0` InlineData when 15K was newly admitted, and the kind DIST-GEN.10 performed against `Dark15KTargetDistanceProjectionTests.cs`'s own `18.0` InlineData. The literal was replaced with `17.0` (an adjacent unsupported value, preserving the theory's original coverage intent) rather than deleted or silently left red.

## 11. Wrong-cell preservation

`DistGen11PublicActivationTests.Target18K_WrongLevel_Rejected` (beginner/advanced × 4D) and `Target18K_WrongFrequency_Rejected` (3D/5D/6D × intermediate) — all non-200, non-500, containing `UNSUPPORTED_TARGET_DISTANCE`. 5 test cases, all pass. **Disclosed test amendment**: `Dark18KTargetDistanceProjectionTests.PublicEligibility_RemainsNarrowerThanDarkEligibility_18KNeverPublic` (DIST-GEN.10's own test, asserting `IsPubliclyEligible(18.0,...) == false` and `ApprovedPublicCells.Count == 2`) had a premise this phase's own explicit purpose directly inverts. Renamed to `PublicEligibility_18KNowPublic_AfterDistGen11_DarkRegistryUnaffected` and rewritten to assert the correct post-phase state (`IsPubliclyEligible(18.0,...) == true`, `ApprovedPublicCells.Count == 3`, `ApprovedDarkCells.Count` still 3) — no coverage lost, the negative "18K stays dark-only" claim is simply no longer true by this phase's own design.

## 12. Exact-target matching semantics

`TryResolvePublicEligibleCell` uses `Math.Abs(km - cell.TargetDistanceKm) < 0.001`, unchanged. Proven at the boundary: 16.09 (10-mile-exact), 17.9, 18.1, 18.01, 18.09 all rejected (§10); on the frontend, the same four near-18.0 values plus 17.0/19.0/20.0/16.09 all still blocked (§40/§43). No nearest-target fallback exists anywhere — a non-matching value returns `null`, never the closest cell.

## 13. Structural-family resolution

`CanonicalDistanceFamilyResolver().Resolve(18.0)` is unchanged and resolves to `HALF_MARATHON`, matching `ApprovedPublicCells`' new entry's `ParentDistanceFamily`. Confirmed via every 18K test's own `goal_distance == "half_marathon"` assertion.

## 14. Catalog candidate reuse

18K public plans reuse the identical `HALF_MARATHON__4D__INTERMEDIATE` catalog candidate/template identity 15K/16K/canonical HM already reuse — confirmed by every `DistGen11PublicActivationTests` test asserting the same `template_id` string. No new catalog JSON, no new candidate.

## 15. Generation-source proof

18K flows through the same `PlanServices.GeneratePreviewAsync` → `CatalogPreviewGenerator` pipeline as every other request; the only per-target divergence is the `TryResolveDarkEligibleCell`-resolved cell's `TargetDistanceKm` pattern-matched dispatch to the 18K-specific horizon/volume/taper policy instances (unchanged from DIST-GEN.10, zero further generalization performed or required by this public-activation phase).

## 16. Horizon public contract

`TargetDistance18KHorizonPolicy`: `MinimumCoreWeeks = 10`, `PreferredCoreWeeks = 12`, `MaximumCoreWeeks = 14` (unmodified since DIST-GEN.10).

## 17. Lower-bound rejection

`DistGen11PublicActivationTests.NineWeekHorizon_18K_TypedRejection_NotFiveHundred_NotSilentSubstitution`: real HTTP 9W request → `422 Unprocessable Entity`, body contains `DARK_18K_CORE_HORIZON`, does NOT contain `TARGET_BELOW_SUM_OF_MINIMUMS`, `DARK_15K`, `DARK_16K`, or `template_id`. Pass.

## 18. Minimum-horizon proof

`EligibleHorizons_18K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(10)`: 200 OK, 10 weeks returned, correct identity, peak volume 31.0 (±0.5), long run ≤ 16.0km. Pass.

## 19. Preferred-horizon proof

`EligibleHorizons_18K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(12)` plus the full E2E lifecycle test (§46-58) and the dark-vs-public golden comparison (§23) and three-way isolation test (§32) all use 12W as the primary golden horizon. All pass.

## 20. Maximum-horizon proof

`EligibleHorizons_18K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(14)`: 200 OK, 14 weeks, correct identity, peak volume 34.0 (±0.5). Pass.

## 21. Upper-bound rejection

`FifteenWeekHorizon_18K_TypedRejection_UnderOwnMaximumAuthority`: real HTTP 15W request → `422`, body contains `DARK_18K_CORE_HORIZON`, does not contain `template_id`. Pass.

## 22. Complete supported-horizon matrix

All 5 horizons (10/11/12/13/14) independently HTTP-proven in §18-20; all 5 pass with peak volumes 31.0/32.5/34.0/34.0/34.0 exactly matching DIST-GEN.10's own dark golden trace prediction.

## 23. Dark/public golden comparison

`DarkVsPublic_18K_12W_SemanticZeroDelta_OnlyReachabilityChanged`: the public 12W HTTP trace matches DIST-GEN.10's own real-pipeline golden trace (§39 of that report) exactly — peak volume 34.0 (±0.5), peak long run 13.5 (±0.5), 4 sessions/week at every week, 12-week length. Both traces exercise the identical `TargetDistance18KHorizonPolicy`/`VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K` instances — this is a structural guarantee (same code path from `PlanServices.GeneratePreviewAsync` onward), not an empirical coincidence, exactly as DIST-GEN.7 §22 established for 15K.

## 24. Peak-volume policy proof

Every horizon's observed peak weekly volume matches DIST-GEN.10's own predicted trace (31.0/32.5/34.0/34.0/34.0 for 10-14W), staying within `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K`'s frozen 34.0(band min)/46.0(band max)/40.0(selected peak) triple — never 15K's/16K's own distinguishable numbers (identical by shared/converged authority per DIST-GEN.9 §29, not a coincidence this phase introduces), never HM's canonical 43.0, never TEN_K's 38.0.

## 25. Selected-peak proof

No horizon (10-14W) forces convergence to the 40.0 selected-peak reference — all observed peaks (31.0-34.0) stay comfortably below it, consistent with the unmodified HM.5.3 "selected ceiling, never forced" invariant.

## 26. Calibration proof

Not independently re-derived (unmodified `GoldenFixtureStartingVolumeKm = 23.5` from DIST-GEN.9/10); confirmed present in every generated trace's own Week-1 volume via the horizon matrix (§22).

## 27. Shared growth proof

`PreferredWeeklyIncreaseRate=0.07`/`HardWeeklyIncreaseRate=0.08` — unmodified, confirmed via unchanged progression shape across all 5 horizons matching DIST-GEN.10's own predicted trace.

## 28. Shared absolute-cap proof

`AbsoluteWeeklyIncreaseCapKm=2.5` — unmodified, consistent with the smooth week-over-week volume progression observed in every horizon matrix test.

## 29. Shared LR-share proof

LR quadruple `0.30/0.36/0.33/0.40` — unmodified; long-run values observed at every horizon are consistent with this share applied to the observed weekly volumes (e.g. 12W peak 34.0 × ~0.33 ≈ 11.2-13.5 range observed).

## 30. Shared starting-LR-cap proof

`StartingAbsoluteLongRunCapKm = null` — unmodified; no anomalous Week-1 long-run cap observed in any trace.

## 31. 18K peak-LR proof

Every 18K trace (all 5 horizons) asserts `long_run` distances stay `< 18.0` and `<= 16.0 + 0.001` (§7's per-horizon assertion) — `PreferredAbsolutePeakLongRunKm = 16.0` is reachable and correctly enforced through the real public HTTP path, not only the internal DIST-GEN.10 harness.

## 32. 15K/16K/18K LR isolation

`DistGen11PublicActivationTests.ThreeWayIsolation_15K16K18K_PublicPeakLongRun_NoCrossContamination` — the single most important test in this phase. Generates all three public plans (15K/16K/18K) at 12W via real HTTP in one test, asserts `requested_target_distance_km` are pairwise distinct (15.0/16.0/18.0), and asserts each plan's own peak long run stays within its own ceiling (≤14.5/≤15.0/≤16.0) while never reaching its own respective ceiling-plus-one boundary (<15.0/<16.0/<18.0). Pass. This directly extends `DistGen7PublicActivationTests.PeakLongRun_15K_Uses14Point5_16K_StillUses15_NoCrossContamination`'s two-way proof into a three-way real-HTTP proof.

## 33. LR-below-target invariant

Asserted directly in the three-way isolation test: `peak15 < 15.0`, `peak16 < 16.0`, `peak18 < 18.0` — all hold. Pass.

## 34. Parent-family taper

Not independently re-derived this phase (unmodified, reused directly from `TargetDistance16KTaperVolumePolicy` per DIST-GEN.10 §33's own disclosed decision); confirmed present via the TAPER-phase volume drop observed at week 11/12 in the 12W golden comparison (§23), consistent with the non-chained 2-week/0.70/0.43 mechanism.

## 35. Target-race-pace proof

Unchanged — `TargetFinishTimeSeconds`/`TargetFinishTimeSource` flow through the same request/command/internal-request mapping for 18K Custom requests as for 15K/16K; not touched by this phase.

## 36. Riegel proof

Not touched by this phase. N/A — no changes to any Riegel/finish-time-projection code path.

## 37. Target-time fitness separation

Not touched by this phase. N/A.

## 38. Product-average behavior

Not touched by this phase — the 18K test requests use `user_defined` target-finish-time sources exactly as DIST-GEN.7/10 did for 15K/16K; no change to `TargetFinishTimeSource` handling.

## 39. Readiness safety

Not touched by this phase — no changes to readiness/explicit-zero handling. The full monolithic regression (§81) is the proof no readiness-adjacent behavior regressed.

## 40. Frontend 18K admission

`race_details_page.dart`'s `_approvedProjectedTargetsKm` extended from `[15.0, 16.0]` to `[15.0, 16.0, 18.0]` — a one-line generalization of the already-generic exact-match list mechanism, exactly as the governing prompt predicted. `goal_distance_custom_frontend_guard_test.dart`'s new `RaceDetailsPage — DIST-GEN.11 exact 18K projected target` group: typing `18` enables Continue with no guard message; typing 17.9/18.1/18.01/18.09/17.0/19.0/20.0/16.09 keeps Continue disabled with the guard message shown.

## 41. Frontend exact payload

`submitting exactly 18.0 sets goal_distance=custom and target_distance_km=18.0 in state` test: enters `"18"`, taps Continue, asserts `onboardingProvider` state has `goalDistance == 'custom'`, `targetDistanceKm == 18.0`, and the serialized `GenerateRacePlanPreviewRequestDto` JSON payload contains `target_distance_km: 18.0`. Pass.

## 42. Frontend 15K zero-delta

`typing exactly 15.0 still enables Continue unchanged (15K zero-delta)` (new, in the DIST-GEN.11 group) and the pre-existing DIST-GEN.7 group's own 15.0 tests all still pass unmodified.

## 43. Frontend 16K zero-delta

`typing exactly 16.0 still enables Continue unchanged (16K zero-delta)` (new, in the DIST-GEN.11 group) and the pre-existing DIST-GEN.3/7 group's own 16.0 tests all still pass unmodified.

## 44. Habit Custom zero-delta

`git diff --stat -- mobile/lib/features/onboarding/presentation/custom_goal_page.dart` produces no output (confirmed empty). The pre-existing `CustomGoalPage — habit "custom goal" always maps to goal_distance=custom` test group (untouched) still passes — Continue remains permanently disabled on that screen regardless of this phase.

## 45. User-visible 18K identity

`goal_distance_formatter_test.dart`'s new test: `formatGoalDistanceLabel(goalDistance: 'half_marathon', requestedTargetDistanceKm: 18.0) == '18K'`. The formatter source (`goal_distance_formatter.dart`) required zero changes — confirmed via `git diff` (empty) — it was already generic (`'${requestedTargetDistanceKm.round()}K'` for any non-null value). No per-widget `if target==18` check was added anywhere.

## 46. Preview response

Real 18K preview response body: `requested_target_distance_km: 18.0`, `goal_distance: "half_marathon"`, `template_id: "HALF_MARATHON__4D__INTERMEDIATE"`, `weeks: [...]` with `days[].day_type`/`distance_km` fields — same shape as every existing preview response.

## 47. Preview→confirm

`TwelveWeek_18K_FullLifecycle_IdentitySurvivesEveryStep`: `POST /api/v1/plans/generate-preview/race` → 200 with `preview_id`; `POST /api/v1/plans/confirm` with that `preview_id` → returns `plan_id`. Pass.

## 48. PostgreSQL persistence

Same test: direct EF Core read of `TrainingPlans` (`AsNoTracking`, `Include(Weeks).ThenInclude(Days)`) by `plan_id` confirms the row exists with `CanonicalDistanceFamily == "HALF_MARATHON"`, `RequestedTargetDistanceKm == 18.0`, `Weeks.Count == 12`.

## 49. Three-target exact-target persistence

Same test asserts `plan.RequestedTargetDistanceKm` is `18.0` and explicitly `!= 21.0975` (real HALF_MARATHON distance), `!= 16.0`, `!= 15.0` (sibling pilots), and `!= 5.0` (the historical silent-fallback defect DIST-GEN.0.1 closed). Pass.

## 50. Home

`GET /api/v1/plans/active/home` → `active_plan.requested_target_distance_km == 18.0`. Pass (both in the lifecycle test and the rollback-safety test).

## 51. Calendar

`GET /api/v1/plans/active/calendar?month=2026-07` → non-empty array. Pass.

## 52. Detail

Covered by §53 (PlanDetails is the "detail" read endpoint in this API surface, matching DIST-GEN.7's own precedent).

## 53. PlanDetails

`GET /api/v1/plans/active/details` → `requested_target_distance_km == 18.0`, `goal_distance == "half_marathon"`. Pass.

## 54. Profile/overview

N/A — no profile/overview endpoint exists in this API surface beyond Home/Details, matching DIST-GEN.3/7's own precedent.

## 55. Complete

`POST /api/v1/training-days/{id}/complete` on the first scheduled day (`actual_distance_km`/`actual_duration_min`) → success; DB read confirms `TrainingDayStatus.Completed`. Pass.

## 56. Not Today

`POST /api/v1/training-days/{id}/not-today-decisions` on the second scheduled day (`reason`) → success (2xx). Pass.

## 57. Pending confirmation

N/A, matching DIST-GEN.3/7's own precedent — no distinct "pending confirmation" state/endpoint exists; preview→confirm (§47) is the entire confirmation lifecycle.

## 58. Cancel

`POST /api/v1/plans/{planId}/cancel` (`reason`) → `200 OK`. Pass. §47-58 are all exercised in a single unbroken lifecycle test, `TwelveWeek_18K_FullLifecycle_IdentitySurvivesEveryStep`.

## 59. Three-target public isolation

Covered by §32 (`ThreeWayIsolation_15K16K18K_PublicPeakLongRun_NoCrossContamination`) — the real-HTTP three-way proof that 15K/16K/18K each resolve distinct target distances and distinct (never cross-contaminating) peak-long-run ceilings.

## 60. Unsupported target matrix

| target_distance_km | Result |
|---|---|
| 12.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 14.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 15.0 | 200 OK (unchanged since DIST-GEN.7) |
| 16.0 | 200 OK (unchanged since DIST-GEN.3) |
| 16.09 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 17.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 17.9 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 18.0 | **200 OK (newly admitted by this phase)** |
| 18.01 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 18.09 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 18.1 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 19.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 20.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |

All confirmed by direct test execution (§10, §22).

## 61. 18K cell matrix

18K at Beginner/Advanced × 4D and Intermediate × 3D/5D/6D — all rejected with `UNSUPPORTED_TARGET_DISTANCE`, never 500, never 200 (§11).

## 62. Custom-without-target safety

`CustomWithoutTargetDistanceKm_StillFailsClosed_18KAdditionDidNotWeaken` confirms `Custom` with no `target_distance_km` still returns `422` with `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`, and no preview/plan row is persisted (row-count before/after assertion). Pass.

## 63. Invalid-target behavior

No new invalid-input shape was introduced; existing DTO validation (rejecting non-numeric/negative/absent target values) is untouched — confirmed by the full monolithic regression's own unchanged coverage of these paths.

## 64. Error-contract matrix

| Scenario | Status | Error code |
|---|---|---|
| `custom`, no target | 422 | `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` |
| `custom`, target resolves out of all family bands | 400 | `UNSUPPORTED_TARGET_DISTANCE` |
| `custom`, target in-band but not an approved public cell (12/14/17/19/20/16.09/17.9/18.1/etc.) | 400 | `UNSUPPORTED_TARGET_DISTANCE` |
| `custom`, 15.0/16.0/18.0 but wrong level or wrong days_per_week | 400 | `UNSUPPORTED_TARGET_DISTANCE` |
| 18.0 approved cell, horizon < 10W | 422 | `DARK_18K_CORE_HORIZON_<N>_NOT_SUPPORTED` |
| 18.0 approved cell, horizon > 14W | 422 | `DARK_18K_CORE_HORIZON_<N>_NOT_SUPPORTED` |
| 18.0 approved cell, 10-14W | 200 | n/a |
| 15.0/16.0 approved cell, horizon out of range | 422 | `DARK_15K_CORE_HORIZON_<N>_NOT_SUPPORTED` / `DARK_16K_CORE_HORIZON_<N>_NOT_SUPPORTED` (unchanged) |

## 65. Public registry provenance

`ApprovedPublicCells` grew by exactly one line this phase (§5). **Three** pre-existing tests carried stale "18.0 is not yet public"/"18.0 is unsupported" premises. Two (`DistGen7PublicActivationTests.cs`'s InlineData, `Dark18KTargetDistanceProjectionTests.cs`'s dedicated test) were caught during initial diff review and disclosed/corrected before the first full regression run (§10, §11). The third (`DistGen3PublicActivationTests.cs`'s own separate `[InlineData(18.0)]`) was missed in that initial review and was caught only by the first full monolithic regression run surfacing it as a genuine new failing identity — diagnosed and corrected with full disclosure, not silently patched (§81). All three corrections mirror the identical, already-established precedent: DIST-GEN.7's own `DistGen3PublicActivationTests.cs` 15.0→14.0 substitution (documented in that same file's own pre-existing comment) and DIST-GEN.10's own §69 precedent (`Dark15KTargetDistanceProjectionTests.cs`'s stale 18.0 literals).

## 66. Rollback boundary

Remove the single entry `new(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4)` from `Dark16KPilotEligibilityPolicy.ApprovedPublicCells`. This alone restores 18.0 to `UNSUPPORTED_TARGET_DISTANCE` at the public HTTP boundary, with zero effect on 15.0/16.0 (separate, independent list entries) and zero effect on the dark registry (`ApprovedDarkCells`, a completely separate list, unaffected either way).

## 67. Confirmed-plan operability after rollout disable

`ConfirmedPlan_18K_RemainsFullyOperable_ReadsAndMutationsDoNotRecheckPublicEligibility`: confirms an 18K plan, then constructs an isolated in-memory `ApprovedPublicCells`-shaped list with the 18.0 entry removed (the exact seam mechanism the governing prompt names as acceptable), confirms that rolled-back list correctly excludes 18.0 while retaining 15.0/16.0, then reads Home/Details and cancels the already-confirmed plan — all succeed, confirming by direct inspection and execution that `PlanServices`'s read/mutation methods and `TrainingDaysController`/`PlansController`'s complete/not-today/cancel actions never consult `GeneratePreviewCommandMapper`/`Dark16KPilotEligibilityPolicy.ApprovedPublicCells` at all. Mirrors DIST-GEN.7 §55's identical finding for 15K. Pass.

## 68. TEN_K zero-delta

`DistGen11PublicActivationTests.ZeroDelta_TenKIntermediate4D_Unaffected` passes: canonical `ten_k` requests still return `TEN_K__4D__INTERMEDIATE` with `requested_target_distance_km == null`.

## 69. HALF_MARATHON zero-delta

`DistGen11PublicActivationTests.ZeroDelta_HalfMarathonIntermediate4D_Unaffected` passes: canonical `half_marathon` requests still return `HALF_MARATHON__4D__INTERMEDIATE` with `requested_target_distance_km == null`.

## 70. HM 6D zero-delta

Not directly re-exercised by a new test in this phase (no HM-6D-specific call site was touched); the pre-existing `DistGen3PublicActivationTests.ZeroDelta_HalfMarathonIntermediate6D_Unaffected` (untouched) and the full monolithic regression (§81) cover it unchanged.

## 71. 5K zero-delta

Not directly re-exercised by a new test in this phase (no 5K-specific call site was touched); the full monolithic regression (§81) covers every existing 5K test unchanged.

## 72. Original Custom→5K regression

The pre-existing `GoalDistanceCustomFailClosedTests.RacePreview_GoalDistanceCustom_FailsClosed_NeverFiveKPlan_NeverFiveHundred` test (untouched by this phase) continues to pass per the full monolithic regression (§81) — Custom with no target still fails closed at 422, never silently produces a 5K plan.

## 73. Catalog/hash integrity

Zero catalog JSON files touched by source changes. `PlanCatalog.Tests`: **1510/1510**, exact zero-delta from the DIST-GEN.10 baseline. The one recurring, benign auto-regenerated audit-file diff (`plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}`, produced by running `PlanCatalog.Tests`) was reverted via `git checkout --` before the final diff audit.

## 74. Database/schema zero-delta

`git status --porcelain` shows no new/modified file under any `Migrations/` directory. `TrainingPlan.RequestedTargetDistanceKm`/`CanonicalDistanceFamily` columns were already added by DIST-GEN.2; this phase only reads/writes them through the pre-existing pipeline.

## 75. Training-authority zero-delta

No horizon/volume/taper/LR policy class was touched (`TargetDistance18KHorizonPolicy.cs`, `VolumeSafetyPolicy.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `TargetDistance16KAwarePeakVolumeBandLoader.cs`, `PlanServices.cs`, `RaceHorizonPolicy.cs` all show zero diff via `git status`/`git diff`). Every numeric value observed in this phase's own tests traces to DIST-GEN.9's frozen authority table, verbatim.

## 76. Frontend tests

- `mobile/test/goal_distance_custom_frontend_guard_test.dart`: **41/41 pass** (includes the new DIST-GEN.11 group: 18.0 accepted; 17.9/18.1/18.01/18.09/17.0/19.0/20.0/16.09 blocked; 18.0 payload proof; 15K/16K zero-delta spot checks; plus all pre-existing 15K/16K/preset/habit-custom tests unchanged and passing).
- `mobile/test/goal_distance_formatter_test.dart`: **7/7 pass** (includes the new "18K" rendering assertion).
- Full `flutter test` run: **383/383 pass, exit code 0** — no pre-existing test broken.

## 77. Backend targeted tests

- `DistGen11PublicActivationTests.cs` (new): **31/31 pass**.
- `Dark18KTargetDistanceProjectionTests.cs` + `Dark15KTargetDistanceProjectionTests.cs` + `Dark16KTargetDistanceProjectionTests.cs` + `DistGen7PublicActivationTests.cs` (combined targeted run, post-amendment): **189/189 pass**.

## 78. PlanCatalog regression

`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1510 total / 1510 passed / 0 failed** — exactly matches the DIST-GEN.10 baseline.

## 79. RuntimeCatalog regression

`dotnet test ... --filter "FullyQualifiedName~RuntimeCatalog"`: **4008 total / 4008 passed / 0 failed** — exactly matches the DIST-GEN.10 baseline (net zero: one test renamed/rewritten in place, not added or removed).

## 80. PlanGeneration regression

N/A — no dedicated `PlanGeneration`-named test project/subtree distinct from the monolithic `RunningApp.IntegrationTests` suite exists in this repo; covered by §81.

## 81. Full monolithic regression

**Two full runs occurred, both fresh-build-confirmed, per this repo's own documented stale-build caution:**

**Run 1 (surfaced a genuine new failing identity):** `dotnet build backend/RunningApp.sln` confirmed 0 errors immediately before the run. `dotnet test backend/RunningApp.IntegrationTests --no-build` → **5069 total / 5064 passed / 5 failed**, 46m44s, results in `distgen11_full_monolithic.trx`. Parsed directly from the raw xUnit console output and cross-checked against the `.trx`'s own `<Counters>` element. The 5 failing identities:

1. `RunningApp.IntegrationTests.DistGen3PublicActivationTests.UnsupportedCustomTargetDistances_AtIntermediateFourDay_TypedRejection_NoSilentSubstitution(km: 18)` — **NEW, not in the DIST-GEN.10 baseline.** `Assert.Equal() Failure: Expected BadRequest, Actual OK`.
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` — known pre-existing.
3. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)` — known pre-existing.
4. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` — known pre-existing.
5. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — known pre-existing.

**Diagnosis of the new failure (#1), disclosed in full, not silently patched:** `DistGen3PublicActivationTests.cs` (DIST-GEN.3-era file) has its own, separate `UnsupportedCustomTargetDistances_AtIntermediateFourDay_TypedRejection_NoSilentSubstitution` Theory with `[InlineData(18.0)]` — a second, independent stale literal this phase's own initial diff review missed (only the near-identical Theory in `DistGen7PublicActivationTests.cs`, §10, had been caught and fixed before Run 1). Its doc comment already documented one prior identical correction (`NOTE: 15.0 was removed from this matrix by PHASE DIST-GEN.7 ... 14.0 was added in its place`) — this phase's own 18.0 admission invalidates the same test's `18.0` case by the identical mechanism. This is precisely the class of "stale pre-existing literal test assertion analogous to what DIST-GEN.10 itself hit" the governing prompt named as a possibility and asked to be reported rather than hidden or silently worked around.

**Resolution:** `18.0` replaced with `19.0` (an unsupported value, preserving the theory's original 4-case coverage intent), with the doc comment extended to disclose exactly why, mirroring the immediately-preceding `15.0→14.0` comment already in the same file. Re-ran `DistGen3PublicActivationTests.cs` in isolation post-fix: **22/22 pass** (`distgen11_distgen3_recheck.trx`).

**Run 2 (fresh clean build, authoritative, post-fix):** `dotnet build backend/RunningApp.sln -v minimal` immediately before the run: **0 Hata (0 errors), 0 Uyarı** beyond the same 16 pre-existing warnings (§83). `dotnet test backend/RunningApp.IntegrationTests --no-build --logger "trx;LogFileName=distgen11_full_monolithic_v2.trx"` → **5069 total / 5065 passed / 4 failed**, 46m17s. Parsed from both the raw console output and directly from the `.trx`'s own `<Counters total="5069" executed="5069" passed="5065" failed="4" .../>` element (4 `outcome="Failed"` `<UnitTestResult>` entries confirmed via direct grep). The 4 failing identities, byte-identical to the DIST-GEN.10 baseline:

1. `RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
2. `RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
3. `RunningApp.IntegrationTests.LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
4. `RunningApp.IntegrationTests.Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

**No other failures.** `git status --porcelain -- plan-catalog backend/RunningApp.Persistence/Migrations` confirmed clean (no benign catalog-audit auto-regen this time, no migration diff) after Run 2.

## 82. Exact durable-baseline comparison

DIST-GEN.10's own documented durable baseline: **5038 total / 5034 passed / 4 failed**, with these exact 4 known pre-existing failing identities (identical list to §81 Run 2's own 4):
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

This phase's own authoritative Run 2: **5069 total / 5065 passed / 4 failed** — a **+31** delta in both total and passed, **0** delta in failed, and the exact same 4 failing identities, byte-for-byte, confirmed by identity comparison (not just count comparison).

**+31 reconciles exactly**: this phase added 31 new test cases in `DistGen11PublicActivationTests.cs` (5 horizon-matrix Theory cases + 2 wrong-level + 3 wrong-frequency + 10 unsupported-target Theory cases + 1 three-way isolation + 1 dark/public golden comparison + 1 full lifecycle + 1 rollback-safety + 1 custom-without-target + 2 sibling zero-delta (15K/16K) + 2 canonical zero-delta (TEN_K/HM) + 1 nine-week + 1 fifteen-week = 31, confirmed by the isolated targeted run in §77 reporting exactly 31/31). No other file's test count changed — `Dark18KTargetDistanceProjectionTests.cs`'s one amendment renamed/rewrote an existing test in place (net 0), and both `DistGen7PublicActivationTests.cs`'s and `DistGen3PublicActivationTests.cs`'s own amendments substituted an InlineData value (net 0 each). 31 + 0 + 0 + 0 = **+31**, matching the observed delta exactly.

## 83. Build-freshness verification

`dotnet build backend/RunningApp.sln -v minimal` run fresh immediately before the targeted test runs: **0 Hata (0 errors), 0 Uyarı (0 warnings)** beyond the same 16 pre-existing warnings present before this phase's edits (in files this phase never touched — `PreparationRunwayFiveDayPublicActivationEndToEndTests.cs`, `Gen9AdvancedCombinedDarkVerificationTests.cs`, and similar pre-existing nullable/analyzer warnings, byte-identical set to DIST-GEN.7's own §69 finding). A subsequent incremental rebuild immediately before the full monolithic run also showed 0 errors / 0 warnings, confirming the suite reflects the final source tree exactly.

## 84. Final source-diff audit

```
 M backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs
 M backend/RunningApp.IntegrationTests/DistGen3PublicActivationTests.cs
 M backend/RunningApp.IntegrationTests/DistGen7PublicActivationTests.cs
 M backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/Dark18KTargetDistanceProjectionTests.cs
?? backend/RunningApp.IntegrationTests/DistGen11PublicActivationTests.cs
 M mobile/lib/features/onboarding/presentation/race_details_page.dart
 M mobile/test/goal_distance_custom_frontend_guard_test.dart
 M mobile/test/goal_distance_formatter_test.dart
?? PHASE_DIST_GEN_11_18K_INTERMEDIATE_4D_PUBLIC_READINESS_CONTROLLED_ACTIVATION_AND_END_TO_END_PROOF.md
```
(Excludes pre-existing `bin/`/`obj/`/`TestResults/` build-output noise and the reverted benign catalog-audit auto-regen — see §73.) No unexpected file present: exactly 7 modified + 2 added, matching the updated §3 (the `DistGen3PublicActivationTests.cs` amendment was added after the initial diff review, per §65/§81's own full disclosure). `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` confirmed zero diff via the same `git status` invocation — governance/commit activity is explicitly out of scope for this phase, per instruction. No commit was created; the working tree carries only these source/test changes plus this report, uncommitted.

## 85. Public-registry scalability observation

Adding the third public target required exactly one line in `ApprovedPublicCells` and zero changes to `TryResolvePublicEligibleCell`/`IsPubliclyEligible` or to `GeneratePreviewCommandMapper.cs` — confirming DIST-GEN.7 §5's own design ("a flat, independently-authored exact-match list") scales cleanly to a third entry with no mechanism change, unlike the dark-side dispatch cascades DIST-GEN.10 §56-57 disclosed as needing a third branch. The public gate is a single list-lookup call site, not a multi-site cascade, so it carries none of that disclosed architectural debt.

## 86. No-band proof

`git diff` for this phase's changed/new files, grepped for `Band(?!VolumeBand)|Range|nearest|minTarget|maxTarget|Interpolat|lerp` returns no new distance-band abstraction: `ApprovedPublicCells` is still a flat list of exact triples, `TryResolvePublicEligibleCell` is still exact-match only, and no `minTargetKm`/`maxTargetKm`/`InteriorDistancePolicy`-shaped type was added. Consistent with DIST-GEN.8's `BANDING_STILL_NOT_GOVERNANCE_READY` finding — this phase performs no banding work of any kind.

## 87. Current public/dark matrix

| Target | Dark | Public |
|---|---|---|
| 15K (HALF_MARATHON, Intermediate, 4D) | Yes | Yes (since DIST-GEN.7) |
| 16K (HALF_MARATHON, Intermediate, 4D) | Yes | Yes (since DIST-GEN.3) |
| 18K (HALF_MARATHON, Intermediate, 4D) | Yes | **Yes (new, this phase)** |
| Everything else (12K/17K/19K/20K/10-mile-exact/other levels/frequencies) | No | No |

## 88. Rollback plan

Full revert: `git checkout -- backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs mobile/lib/features/onboarding/presentation/race_details_page.dart` restores the pre-phase two-target public gate exactly (18K becomes unreachable publicly again; 15K/16K unaffected either way since they were never removed, only the file's own third entry added). Partial/targeted rollback: §66. Test-file reverts are optional but recommended together as one unit (reverting the code without reverting the two disclosed stale-literal test corrections would reintroduce two genuinely-failing tests, since their premise would then be true again).

## 89. DIST-GEN.12 recommendation

Three interior HALF_MARATHON-family targets (15K, 16K, 18K) are now all authority-closed and publicly proven, with the dark-side dispatch-generalization debt DIST-GEN.9/10 explicitly disclosed (§57 of DIST-GEN.10) still outstanding and now carried forward a second time. A future governance phase could: (a) perform the keyed-lookup dispatch generalization DIST-GEN.10 deferred, now with three concrete branches to generalize from; (b) revisit distance-banding evidence broadly, now that three real, independently-authorized public data points exist (though DIST-GEN.9 §60 already found `BANDING_STILL_NOT_GOVERNANCE_READY` even with all three dark data points in hand — a fourth target's public activation alone would not change that evidentiary conclusion). This is a recommendation only — DIST-GEN.12 is explicitly NOT begun by this phase.

## 90. Remaining blockers

**None.** The one genuine item surfaced during this phase's own verification — a third, previously-missed stale `[InlineData(18.0)]` literal in `DistGen3PublicActivationTests.cs`'s own unsupported-target Theory, which the first full monolithic regression run correctly surfaced as a new failing identity — was diagnosed precisely and disclosed in full (§65, §81) before being resolved with a minimal, one-line, test-data-only correction (`18.0` → `19.0`, mirroring the file's own pre-existing, identical-shaped `15.0→14.0` precedent from DIST-GEN.7). No coverage was lost (the equivalent and more complete real-HTTP admission proof for 18.0 lives in `DistGen11PublicActivationTests.cs`), no production authority was touched to make it pass, and the fix was verified in isolation (22/22) before being re-confirmed clean in a second, authoritative full-suite run (§81 Run 2: 5069/5065/4, exactly the 4 known durable identities). The one non-blocking structural item carried forward from DIST-GEN.9/10 — the dark-side dispatch-generalization debt (§57 of DIST-GEN.10) — remains outstanding and unaddressed by this phase, as expected: this phase's own scope was public-registry activation only, and the public gate itself required no such generalization (§85).

---

## Final classification

**18K_INTERMEDIATE_4D_PUBLIC_ACTIVATION_COMPLETE**

Every criterion holds: exact `target_distance_km=18.0` is admitted publicly at every one of the 5 supported horizons (§7, §18-22) with `RequestedTargetDistanceKm` staying exactly 18.0 and `CanonicalDistanceFamily=HALF_MARATHON` throughout (§13, §46-49); both horizon boundaries reject correctly and typedly, never 500, never a wrong-target fallback (§17, §21); generation is fully catalog-backed via the unmodified, already-proven `HALF_MARATHON__4D__INTERMEDIATE` candidate (§14-15); public output is semantically zero-delta from DIST-GEN.10's own dark golden trace (§23); every shared and target-specific authority field is correct with zero cross-contamination across 15K/16K/18K, proven through the real public HTTP path in the single most important test of this phase (§32-33); the full real HTTP+PostgreSQL lifecycle (preview→confirm→persist→Home→Calendar→Details→Complete→Not-Today→Cancel) works end-to-end (§47-58); user-visible "18K" identity renders correctly through the pre-existing generic formatter with zero per-widget special-casing (§45); the rollback proof holds via the exact seam mechanism the governing prompt named, and an already-confirmed plan's own operability is proven independent of creation-time eligibility (§66-67); 15K/16K/TEN_K/HALF_MARATHON/Custom-without-target/Habit-Custom are all zero-delta by direct real-HTTP re-proof or unmodified-file confirmation (§8-9, §44, §62, §68-69); fresh regression across all three suites shows no new failing identity after the one disclosed, diagnosed, and minimally-corrected stale test literal was resolved (§78-82: PlanCatalog 1510/1510, RuntimeCatalog 4008/4008, monolithic 5069/5065/4-failed with exactly the 4 known durable pre-existing identities and no others); no band, range, nearest-match, or interpolation logic exists anywhere in the diff (§86); and the final diff is scoped to exactly the intended files plus the three disclosed, precedent-matching stale-literal test corrections, with zero ledger/roadmap/commit activity (§84).
