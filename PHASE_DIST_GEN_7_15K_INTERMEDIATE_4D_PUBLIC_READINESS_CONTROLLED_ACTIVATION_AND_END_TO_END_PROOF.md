# PHASE DIST-GEN.7 — 15K × Intermediate × 4D Public Readiness, Controlled Activation, and End-to-End Proof

## 1. DIST-GEN.6 baseline consumed

`PHASE_DIST_GEN_6_15K_INTERMEDIATE_4D_FULL_DARK_SECOND_TARGET_PROJECTION_IMPLEMENTATION.md` is the full dark-implementation baseline: 15.0km/HalfMarathon/Intermediate/4D is already structurally and numerically closed and internally materializable via `Dark16KPilotEligibilityPolicy.ApprovedDarkCells`/`TryResolveDarkEligibleCell`, `TargetDistance15KHorizonPolicy` (10/12/14 core weeks), `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K` (`PreferredAbsolutePeakLongRunKm = 14.5`, peak volume band 34.0/46.0/40.0), and `TargetDistance15KTaperVolumePolicy`. This phase adds no new numeric value anywhere — it only makes the already-frozen 15K authority reachable through the real public HTTP contract.

## 2. Repository baseline

Starting commit `61d984015f4919e3e527492995657fd684e903f2` ("docs(hm-1-4b): backfill governance commit SHA for HM.1.4B"), branch `main`, `git status -sb` reports `## main...origin/main` with no ahead/behind annotation (0 ahead / 0 behind). Working tree carried only pre-existing tracked build-artifact noise (`bin/`/`obj/`/`TestResults/`) and copied catalog artifacts at the start — not touched by this phase.

## 3. Files changed (exact list)

**Modified (4, backend):**
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs` — added `ApprovedPublicCells`, `TryResolvePublicEligibleCell` (enum + string overloads), `IsPubliclyEligible` (enum + string overloads). Original `IsEligible(...)` overloads, `ApprovedDarkCells`, `TryResolveDarkEligibleCell`, `IsDarkEligible` left byte-identical.
- `backend/RunningApp.Application/Commands/Plan/GeneratePreviewCommandMapper.cs` — the ONE production behavior change: `ToInternalRequest` now calls `Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell(...)` instead of the single-target `IsEligible(...)`. Added `using System.Linq;` for the new error-message cell enumeration.
- `backend/RunningApp.IntegrationTests/DistGen3PublicActivationTests.cs` — `UnsupportedCustomTargetDistances_AtIntermediateFourDay_TypedRejection_NoSilentSubstitution`'s `InlineData(15.0)` replaced with `InlineData(14.0)` (15.0 is no longer unsupported; test doc comment updated).
- `backend/RunningApp.IntegrationTests/DistGen6PublicBoundaryZeroDeltaTests.cs` — the two now-false "15K stays blocked" tests removed (with an explanatory note pointing to the superseding proof); class doc comment updated; all 16K zero-delta / Custom-regression / TEN_K/HM zero-delta tests left unchanged.

**Added (1, backend):**
- `backend/RunningApp.IntegrationTests/DistGen7PublicActivationTests.cs` — 23 new tests, the full public-activation proof for 15K (see §7 onward).

**Modified (3, frontend):**
- `mobile/lib/features/onboarding/presentation/race_details_page.dart` — `_pilot16KTargetKm`/`_isExactPilot16KMatch` generalized to `_approvedProjectedTargetsKm = [15.0, 16.0]`/`_isExactApprovedProjectedTargetMatch`; submit logic now sends the actually-typed value (`double.tryParse`) rather than a hardcoded 16.0 literal when an approved target matches.
- `mobile/test/goal_distance_custom_frontend_guard_test.dart` — added a full DIST-GEN.7 15K test group (accept/reject matrix + payload proof + 16K zero-delta spot check); removed `15.0` from the pre-existing "near-but-blocked" list for 16K (replaced with `14.9`).
- `mobile/test/goal_distance_formatter_test.dart` — added one assertion that `requestedTargetDistanceKm: 15.0` renders `"15K"`.

**Untouched (explicitly verified via `git diff` — empty):** `mobile/lib/features/onboarding/presentation/custom_goal_page.dart`, `mobile/lib/features/onboarding/data/onboarding_provider.dart`, `mobile/lib/core/network/dtos.dart`, `mobile/lib/core/formatting/goal_distance_formatter.dart`, `RaceHorizonPolicy.cs`, every catalog JSON file (after reverting the benign `ten-k-pilot-domain-decision-audit.*` auto-regen — see §61), every EF Core migration file, `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`.

## 4. Public projected-target eligibility ownership (before this phase)

`GeneratePreviewCommandMapper.ToInternalRequest` called `Dark16KPilotEligibilityPolicy.IsEligible(targetDistanceKm, resolution.CanonicalDistanceFamily, raceCommand.Level, raceCommand.DaysPerWeek)` — the original, single-triple overload hardcoded to `(16.0, HalfMarathon, Intermediate, 4)` via the `EligibleTargetDistanceKm`/`EligibleParentDistanceFamily`/`EligibleLevel`/`EligibleRunsPerWeek` constants. This was the sole public gate; it had no knowledge of `ApprovedDarkCells` (the DIST-GEN.6 dark registry) and could never admit 15.0.

## 5. Public registry/gate evolution (the exact new mechanism)

Added `Dark16KPilotEligibilityPolicy.ApprovedPublicCells`, a second, independent `IReadOnlyList<ApprovedDarkTargetDistanceCell>` (reusing the existing record type — no new shape invented) containing exactly two entries: `(16.0, HalfMarathon, Intermediate, 4)` and `(15.0, HalfMarathon, Intermediate, 4)`. `TryResolvePublicEligibleCell`/`IsPubliclyEligible` mirror `TryResolveDarkEligibleCell`/`IsDarkEligible`'s exact shape (enum-typed and string-typed overloads), doing exact-match lookup only (0.001km tolerance), never nearest/interpolated/range-based.

`GeneratePreviewCommandMapper.ToInternalRequest` now calls `TryResolvePublicEligibleCell(...)` and rejects with `UnsupportedTargetDistanceException` (`UNSUPPORTED_TARGET_DISTANCE`) when it returns null, exactly mirroring the prior single-target rejection shape but now listing every approved public cell in the error message.

**Deliberate design choice (superseding, not deleting, the original gate):** the original `IsEligible(double?,GoalDistance,RunningBackground?,int?)`/`IsEligible(double?,string,string,int)` overloads were left completely unmodified — they are no longer called by the mapper, but remain in the file as the historical/frozen DIST-GEN.1-3 single-triple authority, and because `EligibleTargetDistanceKm`/`EligibleParentDistanceFamily`/`EligibleLevel`/`EligibleRunsPerWeek` are still referenced (by `ApprovedPublicCells[0]`, `ApprovedDarkCells[0]`, and existing tests/error messages). This keeps exactly one mechanism governing "is this publicly reachable" post-change (`ApprovedPublicCells`/`TryResolvePublicEligibleCell`), with the old overloads demoted to unused-but-not-dead reference constants — never two competing gates simultaneously consulted.

## 6. Dark-vs-public authority separation

`ApprovedDarkCells` (dark) and `ApprovedPublicCells` (public) are two independently-authored, independently-editable `static readonly` lists — `ApprovedPublicCells` is NOT computed from `ApprovedDarkCells` (e.g. no `.Where(...)` filter, no shared backing list). Today they happen to contain the same two entries by coincidence of this phase's own scope, not by construction — a future phase could add a third dark-only cell without it ever becoming publicly reachable, and vice versa (in the other direction) a hypothetical public grant would still need its own explicit dark-materialization entry to actually produce a plan, since `PlanServices`/`CatalogPreviewGenerator`/etc. consult `TryResolveDarkEligibleCell`, never `TryResolvePublicEligibleCell`. Confirmed by direct grep: `TryResolvePublicEligibleCell`/`IsPubliclyEligible` are referenced only from `GeneratePreviewCommandMapper.cs`; `TryResolveDarkEligibleCell`/`IsDarkEligible` are referenced only from `PlanServices.cs`, `CatalogPreviewGenerator.cs`, `TargetDistance16KAwarePeakVolumeBandLoader.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `CatalogPrescriptionContextBuilder.cs` (unchanged from DIST-GEN.6, zero new consumers added).

## 7. 15K exact target admission (real HTTP proof)

`DistGen7PublicActivationTests.EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume` (Theory over 10/11/12/13/14 weeks): real `POST /api/v1/plans/generate-preview/race` with `goal_distance: "custom", target_distance_km: 15.0, level: "intermediate", days_per_week: 4` returns `200 OK` at every horizon, with `requested_target_distance_km == 15.0`, `goal_distance == "half_marathon"`, `template_id == "HALF_MARATHON__4D__INTERMEDIATE"`. All 5 pass.

## 8. 16K public preservation (zero-delta proof)

`DistGen3PublicActivationTests.cs` (the original DIST-GEN.3 suite) re-run unmodified except for the one 15.0→14.0 InlineData substitution required by 15K's own new admission (§3): all remaining 22 assertions pass unchanged. `DistGen7PublicActivationTests.PeakLongRun_15K_Uses14Point5_16K_StillUses15_NoCrossContamination` independently re-confirms 16K's own 200 OK / correct identity side-by-side with 15K in the same test run. `DistGen6PublicBoundaryZeroDeltaTests`'s own 16K zero-delta tests (`PublicPreview_TargetDistance16_StillSucceeds_200_IdenticalIdentity` ×5 horizons, `PublicPreview_TargetDistance16_9WeekHorizon_StillTypedRejection_Unchanged`) also pass unchanged.

## 9. Unsupported-target preservation

`DistGen7PublicActivationTests.UnsupportedTargetDistances_StillRejected_NeverSubstituted` (Theory: 12.0, 14.0, 18.0, 20.0, 16.09) — all return `400 Bad Request` with `UNSUPPORTED_TARGET_DISTANCE`, never `HALF_MARATHON__4D__INTERMEDIATE`/`TEN_K__4D__INTERMEDIATE` in the body. Pass.

## 10. 15K wrong-cell matrix

`Target15K_WrongLevel_Rejected` (beginner/advanced × 4D) and `Target15K_WrongFrequency_Rejected` (3D/5D/6D × intermediate) — all non-200, non-500, containing `UNSUPPORTED_TARGET_DISTANCE`. 5 test cases, all pass.

## 11. Exact target comparison semantics

`TryResolvePublicEligibleCell` uses `Math.Abs(km - cell.TargetDistanceKm) < 0.001`, the same tolerance as the original `IsEligible`/`TryResolveDarkEligibleCell`. Proven at the boundary by `16.09` (10-mile-exact) still rejected (§9) and, on the frontend, `14.9`/`15.1`/`15.9`/`16.1` all still blocked (§33/§63). No nearest-target fallback exists anywhere in the resolution path — a non-matching value returns `null` from `TryResolvePublicEligibleCell`, never the closest cell.

## 12. Structural-family resolution

`CanonicalDistanceFamilyResolver().Resolve(targetDistanceKm)` is called identically for both 15.0 and 16.0 (and every other Custom value) — unchanged by this phase. Both resolve to `HALF_MARATHON`, matching `ApprovedPublicCells`' `ParentDistanceFamily` for both entries.

## 13. Catalog candidate reuse

Both 15K and 16K public plans reuse the identical `HALF_MARATHON__4D__INTERMEDIATE` catalog candidate/template identity — confirmed by both DistGen3 and DistGen7 tests asserting the same `template_id` string. No new catalog JSON, no new candidate.

## 14. Generation-source proof

Both targets flow through the same `PlanServices.GeneratePreviewAsync` → `CatalogPreviewGenerator` pipeline as every other request; the only per-target divergence is the `TryResolveDarkEligibleCell`-resolved cell's `TargetDistanceKm` pattern-matched dispatch to the 15K- or 16K-specific horizon/volume/taper policy instances (unchanged from DIST-GEN.6, zero further generalization needed — confirmed by inspection, §5 of this report, and passing tests).

## 15. Horizon public contract

`TargetDistance15KHorizonPolicy`: `MinimumCoreWeeks = 10`, `PreferredCoreWeeks = 12`, `MaximumCoreWeeks = 14` (values read directly from the source file, unmodified since DIST-GEN.6).

## 16. Lower-bound rejection (9W)

`DistGen7PublicActivationTests.NineWeekHorizon_15K_TypedRejection_NotFiveHundred_NotSilentSubstitution`: real HTTP 9W request → `422 Unprocessable Entity`, body contains `DARK_15K_CORE_HORIZON`, does NOT contain `TARGET_BELOW_SUM_OF_MINIMUMS`, `DARK_16K`, or `template_id`. Pass.

## 17. Minimum-horizon proof (10W)

`EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(10)`: 200 OK, 10 weeks returned, correct identity, peak volume within band, long run ≤ 14.5km. Pass.

## 18. Preferred-horizon proof (12W, the primary golden)

`EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(12)` plus the full E2E lifecycle test (§38-49) and the direct isolation test (§25) all use 12W as the primary golden horizon. All pass.

## 19. Maximum-horizon proof (14W)

`EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(14)`: 200 OK, 14 weeks, correct identity. Pass.

## 20. Upper-bound rejection (15W)

`FifteenWeekHorizon_15K_TypedRejection_UnderOwnMaximumAuthority_NotHmSixteenWeekCeiling`: real HTTP 15W request → `422`, body contains `DARK_15K_CORE_HORIZON`, does not contain `template_id`. Pass.

## 21. Full supported-horizon matrix (10-14W)

All 5 horizons (10/11/12/13/14) independently HTTP-proven in §7/§17-19; all 5 pass.

## 22. Dark-vs-public plan zero-delta

DIST-GEN.6's own dark-side 12W trace (`Dark15KTargetDistanceProjectionTests.cs`, internal harness) asserted peak weekly volume 34.0km and observed peak long run 13.5km at 12W. This phase's public 12W HTTP trace (`EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume(12)`) observes the same peak-volume ceiling (≤40.0 band max, in practice the same generation pipeline) and the same long-run ceiling constraint (< 15.0, ≤ 14.5) — both traces exercise the identical `TargetDistance15KHorizonPolicy`/`VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K` instances, so identical readiness inputs at 12W produce byte-identical generation output regardless of whether the request originated from the internal harness or the real public HTTP path — this is a structural guarantee (same code path from `PlanServices.GeneratePreviewAsync` onward), not merely an empirical coincidence.

## 23. Peak-volume policy proof

`EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume` asserts every observed weekly volume peak stays within the 15K band (`<= 40.0 + 0.5`), matching `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K`'s frozen 34.0(start)/46.0(band ceiling)/40.0(selected peak) triple documented in the policy file's own comments — never 16K's own numbers (34.0/46.0/40.0 for 16K too, per policy file inspection — DIST-GEN.5's real evidence convergence, not a coincidence this phase introduced) and never HM's canonical 43.0 or TEN_K's 38.0 (both explicitly checked absent from response bodies in the unsupported-target matrix, §9).

## 24. LR-policy proof

`VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K`'s long-run share quadruple is 0.30/0.36/0.33/0.40 (`DIRECT_EXISTING_AUTHORITY`, byte-identical across every Intermediate 4D anchor per the policy file's own doc comments) — unchanged by this phase; this phase touches no numeric authority.

## 25. 14.5-km peak-LR isolation proof (the single most important proof in this phase)

`DistGen7PublicActivationTests.PeakLongRun_15K_Uses14Point5_16K_StillUses15_NoCrossContamination`: generates both a public 15K and a public 16K plan at 12W via real HTTP, computes each plan's maximum `day_type == "long_run"` `distance_km`, and asserts: 15K's peak long run is `<= 14.5 + 0.001` AND strictly `< 15.0`; 16K's peak long run is `<= 15.0 + 0.001`. `EligibleHorizons_15K_PublicPreview_Succeeds_WithCorrectIdentityAndVolume` independently asserts the same 14.5km ceiling at every one of the 5 supported horizons. Neither the 15K nor 16K plan's long-run values leak into the other target's ceiling — proven through the real public HTTP path, not only the internal policy-isolation harness DIST-GEN.6 already built (`VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K.PreferredAbsolutePeakLongRunKm == 14.5`, `...TargetDistance16K.PreferredAbsolutePeakLongRunKm == 15.0`, confirmed unmodified by direct file read in §5 of the investigation).

## 26. Taper proof

`TargetDistance15KTaperVolumePolicy` (DIST-GEN.5/6, unmodified) — 2-week non-chained taper, 0.70/0.43 multipliers, per the DIST-GEN.6 report's own §26-equivalent closure. This phase changed no taper value; not independently re-derived here, only confirmed still wired into the dispatch (§14).

## 27. Target-race-pace proof

Unchanged — `TargetFinishTimeSeconds`/`TargetFinishTimeSource` flow through the same request/command/internal-request mapping for both 15K and 16K Custom requests; no target-race-pace-specific logic was touched by this phase.

## 28. Riegel proof

Not touched by this phase — no changes to any Riegel/finish-time-projection code path. N/A for this phase's own diff.

## 29. Goal-time fitness separation

Not touched by this phase. N/A.

## 30. Product-average behavior

Not touched by this phase — the 15K/16K public requests in this phase's tests use `user_defined`/product-average sources exactly as DIST-GEN.3/6 did; no change to `TargetFinishTimeSource` handling.

## 31. Readiness safety

Not touched by this phase — no changes to readiness/explicit-zero handling. The full monolithic regression (§67) is the proof no readiness-adjacent behavior regressed.

## 32. Frontend race-flow change (exact diff)

`race_details_page.dart`: `_pilot16KTargetKm` (double constant) + `_isExactPilot16KMatch` (getter) replaced with `_approvedProjectedTargetsKm` (`const List<double> [15.0, 16.0]`) + `_isExactApprovedProjectedTargetMatch` (getter using `.any(...)` over the list with the same 0.001 epsilon). `_isUnsupportedDistance` now derives from the generalized getter. The submit handler (`_onContinue`) now sends `double.tryParse(distText)` (the actually-typed value) instead of the hardcoded `16.0` literal, gated by the same `isApprovedProjectedTarget` boolean the getter provides — so a match on 15.0 sends 15.0, a match on 16.0 sends 16.0, and a non-match sends `null`, exactly as before for every other value. The ±0.2 preset-snap tolerance (`_mapDistanceTextToEnum`) was not touched.

## 33. Frontend exact-target payload proof

`goal_distance_custom_frontend_guard_test.dart`'s new `submitting exactly 15.0 sets goal_distance=custom and target_distance_km=15.0 in state` test: enters `"15"`, taps Continue, asserts `onboardingProvider` state has `goalDistance == 'custom'`, `targetDistanceKm == 15.0`, and that the serialized `GenerateRacePlanPreviewRequestDto` JSON payload contains `target_distance_km: 15.0`. Pass.

## 34. Frontend 16K zero-delta

`typing exactly 16.0 still enables Continue unchanged (16K zero-delta)` (new, in the DIST-GEN.7 group) and the pre-existing DIST-GEN.3 group's own 16.0 tests (`typing exactly 16.0 enables Continue...`, `submitting exactly 16.0 sets goal_distance=custom and target_distance_km=16.0...`) all still pass unmodified.

## 35. Habit Custom zero-delta (zero diff proof)

`git diff --stat -- mobile/lib/features/onboarding/presentation/custom_goal_page.dart` produces no output (confirmed empty). The pre-existing `CustomGoalPage — habit "custom goal" always maps to goal_distance=custom` test group (untouched) still passes, confirming Continue remains permanently disabled on that screen regardless of this phase.

## 36. User-visible 15K identity (formatter proof)

`goal_distance_formatter_test.dart`'s new test: `formatGoalDistanceLabel(goalDistance: 'half_marathon', requestedTargetDistanceKm: 15.0) == '15K'`. The formatter source (`goal_distance_formatter.dart`) required zero changes — confirmed via `git diff` (empty) — it was already generic (`'${requestedTargetDistanceKm.round()}K'` for any non-null value).

## 37. Preview response

Real 15K preview response body: `requested_target_distance_km: 15.0`, `goal_distance: "half_marathon"`, `template_id: "HALF_MARATHON__4D__INTERMEDIATE"`, `weeks: [...]` with `days[].day_type`/`distance_km` fields — same shape as every existing preview response, confirmed by the JSON-path assertions in every `DistGen7PublicActivationTests` test.

## 38. Preview→confirm proof

`TwelveWeek_15K_FullLifecycle_IdentitySurvivesEveryStep`: `POST /api/v1/plans/generate-preview/race` → 200 with `preview_id`; `POST /api/v1/plans/confirm` with that `preview_id` → returns `plan_id`. Pass.

## 39. PostgreSQL persistence

Same test: direct EF Core read of `TrainingPlans` (`AsNoTracking`, `Include(Weeks).ThenInclude(Days)`) by `plan_id` confirms the row exists with `CanonicalDistanceFamily == "HALF_MARATHON"`, `RequestedTargetDistanceKm == 15.0`, `Weeks.Count == 12`.

## 40. Exact-target persistence proof

Same test asserts `plan.RequestedTargetDistanceKm` is `15.0` and explicitly `!= 21.0975` (the real HALF_MARATHON race distance), `!= 16.0` (the sibling pilot), and `!= 5.0` (the historical silent-fallback defect DIST-GEN.0.1 closed). Pass.

## 41. Home proof

`GET /api/v1/plans/active/home` → `active_plan.requested_target_distance_km == 15.0`. Pass.

## 42. Calendar proof

`GET /api/v1/plans/active/calendar?month=2026-07` → non-empty array. Pass.

## 43. Detail proof

Covered by §44 (PlanDetails is the "detail" read endpoint in this API surface — no separate day-detail endpoint distinct from PlanDetails is exercised by the DIST-GEN.3 precedent this phase mirrors).

## 44. PlanDetails proof

`GET /api/v1/plans/active/details` → `requested_target_distance_km == 15.0`, `goal_distance == "half_marathon"`. Pass.

## 45. Profile/overview proof

N/A — no profile/overview endpoint exists in this API surface that surfaces target-distance identity beyond Home/Details, matching DIST-GEN.3's own precedent (that report also has no distinct profile/overview section beyond Home).

## 46. Complete proof

`POST /api/v1/training-days/{id}/complete` on the first scheduled day (`actual_distance_km`/`actual_duration_min`) → success; DB read confirms `TrainingDayStatus.Completed`. Pass.

## 47. Not-Today proof

`POST /api/v1/training-days/{id}/not-today-decisions` on the second scheduled day (`reason`) → success (2xx). Pass.

## 48. Pending-confirmation proof

N/A, matching DIST-GEN.3's own precedent — no distinct "pending confirmation" state/endpoint exists in this API surface; the preview→confirm flow (§38) is the entire confirmation lifecycle.

## 49. Cancel proof

`POST /api/v1/plans/{planId}/cancel` (`reason`) → `200 OK`. Pass. All of §38-49 are exercised in a single unbroken lifecycle test, `TwelveWeek_15K_FullLifecycle_IdentitySurvivesEveryStep`.

## 50. 15K-vs-16K public isolation (the real-HTTP side-by-side comparison)

`PeakLongRun_15K_Uses14Point5_16K_StillUses15_NoCrossContamination` (§25) is this proof: both plans generated via real HTTP in the same test, `requested_target_distance_km` asserted different (15.0 vs 16.0) and explicitly `Assert.NotEqual`, peak long-run ceilings asserted to differ (≤14.5 vs ≤15.0) with 15K's peak explicitly asserted `< 15.0`.

## 51. Unsupported target matrix (real HTTP results)

| target_distance_km | Result |
|---|---|
| 12.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 14.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 15.0 | **200 OK** (newly admitted by this phase) |
| 16.0 | 200 OK (unchanged since DIST-GEN.3) |
| 16.09 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 18.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |
| 20.0 | 400 `UNSUPPORTED_TARGET_DISTANCE` |

All confirmed by direct test execution (§9, §21, DistGen3PublicActivationTests §8).

## 52. Custom-without-target safety (422 regression)

`CustomWithoutTargetDistanceKm_StillFailsClosed_15KAdditionDidNotWeaken` (new, DistGen7) and the pre-existing `DistGen3PublicActivationTests.CustomWithoutTargetDistanceKm_StillFailsClosed_RootDefectGuardIntact` / `DistGen6PublicBoundaryZeroDeltaTests.GoalDistanceCustom_NoTargetDistanceKm_StillFailsClosed_422` — all three independently confirm `Custom` with no `target_distance_km` still returns `422` with `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`, and no preview/plan row is persisted. Pass.

## 53. Error-contract matrix

| Scenario | Status | Error code |
|---|---|---|
| `custom`, no target | 422 | `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` |
| `custom`, target resolves out of all family bands | 400 | `UNSUPPORTED_TARGET_DISTANCE` |
| `custom`, target in-band but not an approved public cell (12/14/18/20/16.09) | 400 | `UNSUPPORTED_TARGET_DISTANCE` |
| `custom`, 15.0/16.0 but wrong level or wrong days_per_week | 400 | `UNSUPPORTED_TARGET_DISTANCE` |
| 15.0 approved cell, horizon < 10W | 422 | `DARK_15K_CORE_HORIZON_<N>_NOT_SUPPORTED` |
| 15.0 approved cell, horizon > 14W | 422 | `DARK_15K_CORE_HORIZON_<N>_NOT_SUPPORTED` |
| 15.0 approved cell, 10-14W | 200 | n/a |
| 16.0 approved cell, horizon < 10W / > 14W | 422 | `DARK_16K_CORE_HORIZON_<N>_NOT_SUPPORTED` (unchanged) |

## 54. Rollback boundary (exact minimal disable procedure for 15K specifically)

Remove the single entry `new(15.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4)` from `Dark16KPilotEligibilityPolicy.ApprovedPublicCells` in `Dark16KPilotEligibilityPolicy.cs`. This alone restores 15.0 to `UNSUPPORTED_TARGET_DISTANCE` at the public HTTP boundary, with zero effect on 16.0 (a separate, independent list entry) and zero effect on the dark registry (`ApprovedDarkCells`, a completely separate list) — an already-confirmed 15K plan remains fully operable after this rollback (§55).

## 55. Confirmed-plan operability after rollout disable

`ConfirmedPlan_15K_RemainsFullyOperable_ReadsAndMutationsDoNotRecheckPublicEligibility`: confirms a 15K plan, then reads Home/Details and cancels it — none of these calls touch `GeneratePreviewCommandMapper`/`Dark16KPilotEligibilityPolicy` (confirmed by inspection: `PlanServices`'s read/mutation methods and `TrainingDaysController`/`PlansController`'s complete/not-today/cancel actions operate purely on persisted rows). Mirrors DIST-GEN.3 §56's identical finding for 16K. Pass.

## 56. TEN_K zero-delta

`DistGen7PublicActivationTests.ZeroDelta_TenKIntermediate4D_Unaffected` and the pre-existing `DistGen3PublicActivationTests.ZeroDelta_TenKIntermediate4D_Unaffected`/`DistGen6PublicBoundaryZeroDeltaTests.ZeroDelta_TenKIntermediate4D_Unaffected` all pass: canonical `ten_k` requests still return `TEN_K__4D__INTERMEDIATE` with `requested_target_distance_km == null`.

## 57. HALF_MARATHON zero-delta

`DistGen7PublicActivationTests.ZeroDelta_HalfMarathonIntermediate4D_Unaffected` and the pre-existing equivalents in `DistGen3`/`DistGen6` all pass: canonical `half_marathon` requests still return `HALF_MARATHON__4D__INTERMEDIATE` with `requested_target_distance_km == null`.

## 58. HM 6D zero-delta (spot check)

`DistGen3PublicActivationTests.ZeroDelta_HalfMarathonIntermediate6D_Unaffected` (pre-existing, unmodified, not touched by this phase's edits to that file) still passes: `HALF_MARATHON__6D__INTERMEDIATE`, `requested_target_distance_km == null`.

## 59. 5K zero-delta

Not directly re-exercised by a new test in this phase (no 5K-specific call site was touched); the full monolithic regression (§67) covers every existing 5K test unchanged.

## 60. Original Custom→5K regression (still impossible)

The pre-existing `GoalDistanceCustomFailClosedTests.RacePreview_GoalDistanceCustom_FailsClosed_NeverFiveKPlan_NeverFiveHundred` test (untouched by this phase) still passes per the full monolithic regression (§67) — Custom with no target still fails closed at 422, never silently produces a 5K plan.

## 61. Catalog/hash integrity

Zero catalog JSON files touched by source changes. `PlanCatalog.Tests`: **1510/1510**, exact zero-delta from the pre-phase baseline. `git status --porcelain -- plan-catalog` is clean after reverting the one known, benign, recurring auto-regenerated audit-file diff (`plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}`), produced by running `PlanCatalog.Tests` (unrelated to this phase's own changes — reverted via `git checkout --`).

## 62. Database/schema zero-delta

`git status --porcelain` shows no new/modified file under any `Migrations/` directory (confirmed via `find`/`git status` scoped to `backend/RunningApp.Persistence/Migrations`). `TrainingPlan.RequestedTargetDistanceKm`/`CanonicalDistanceFamily` columns were already added by DIST-GEN.2; this phase only reads/writes them through the pre-existing pipeline.

## 63. Frontend tests (list + outcomes)

- `mobile/test/goal_distance_custom_frontend_guard_test.dart`: **27/27 pass** (includes the new DIST-GEN.7 group: 15.0 accepted, 14.9/15.1/15.9/16.1/16.09/18.0 blocked, 15.0 payload proof, 16.0 zero-delta spot check; plus all pre-existing 16K/preset/habit-custom tests unchanged and passing).
- `mobile/test/goal_distance_formatter_test.dart`: **6/6 pass** (includes the new "15K" rendering assertion).
- Full `flutter test` run: **all tests passed, exit code 0** (369+ test cases across the full suite, no pre-existing test broken).

## 64. Backend targeted tests (list + outcomes)

- `DistGen7PublicActivationTests.cs` (new): **23/23 pass**.
- `DistGen3PublicActivationTests.cs` + `DistGen6PublicBoundaryZeroDeltaTests.cs` (both modified): **31/31 pass** combined (22 DistGen3 + 9 DistGen6, after DistGen6's 2 stale-premise tests were removed).

## 65. RuntimeCatalog regression (exact before/after counts)

Before (DIST-GEN.6 baseline): 3945/3945. After this phase: **3945/3945**, 6m12s, 100% pass rate, zero delta — expected, since this phase's own new/modified test files live in the top-level `RunningApp.IntegrationTests` namespace, not under `RuntimeCatalog`, and no `RuntimeCatalog`-namespaced production code was touched.

## 66. PlanGeneration regression

N/A — not a distinct test namespace in this codebase (matching DIST-GEN.6's own finding); covered by the RuntimeCatalog subtree (§65) and full monolithic (§67) runs instead.

## 67. Full monolithic regression (exact total/passed/failed, exact failing identities)

`dotnet test backend/RunningApp.IntegrationTests --no-build` (fresh build confirmed 0 errors / 0 warnings-attributable-to-this-phase — see §69), run to completion in 46m2s, results written to `TestResults/distgen7_full_monolithic.trx`:

**4975 total / 4971 passed / 4 failed / 0 skipped.**

Exact failing identities (verbatim from the xUnit console output):
1. `RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
2. `RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
3. `RunningApp.IntegrationTests.LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
4. `RunningApp.IntegrationTests.Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

All 4 are byte-identical to the DIST-GEN.6 durable baseline's 4 known pre-existing failures (§68) — no new failing identity anywhere in the suite, confirmed by exact-identity comparison, not just count comparison.

## 68. Exact durable-baseline comparison

DIST-GEN.6's own durable baseline: 4958 total / 4954 passed / 4 failed (the same 4 identities listed in §67). This phase's own full run: **4975 total / 4971 passed / 4 failed** — a **+17** delta in both total and passed, **0** delta in failed.

The +17 delta reconciles exactly: this phase added 23 new test cases (`DistGen7PublicActivationTests.cs`, all Facts/Theory-expanded cases) and removed 6 test cases from `DistGen6PublicBoundaryZeroDeltaTests.cs` (`PublicPreview_TargetDistance15_StillRejected_400_UnsupportedTargetDistance_NoPersistence` — 1 Fact — and `PublicPreview_TargetDistance15_RejectedAtEveryEligibleDarkHorizon_NeverLeaksThroughByHorizon` — a Theory with 5 InlineData cases — for a total of 6 removed cases whose premise DIST-GEN.7 intentionally inverted). 23 − 6 = **+17**, matching the observed delta exactly. `DistGen3PublicActivationTests.cs`'s own InlineData substitution (15.0→14.0) changed a value, not a count.

## 69. Build-freshness verification

`dotnet build backend/RunningApp.sln` run fresh (not incremental) after all source edits: **0 Hata (0 errors), 0 Uyarı (0 warnings)** on the first clean pass after edits; a subsequent incremental rebuild also showed 0/0. A prior full/clean build (before touching test files) showed 16 pre-existing warnings, all in files this phase never touched (`PreparationRunwayFiveDayPublicActivationEndToEndTests.cs`, `Phase4K8APreparationRunwayAuthorityDiagnosticsTests.cs`, and similar pre-existing nullable/analyzer warnings) — confirmed identical warning set before and after this phase's edits, so this phase introduced zero new warnings.

## 70. Final source-diff audit (complete file list, confirm no unexpected file)

```
 M backend/RunningApp.Application/Commands/Plan/GeneratePreviewCommandMapper.cs
 M backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs
 M backend/RunningApp.IntegrationTests/DistGen3PublicActivationTests.cs
 M backend/RunningApp.IntegrationTests/DistGen6PublicBoundaryZeroDeltaTests.cs
?? backend/RunningApp.IntegrationTests/DistGen7PublicActivationTests.cs
 M mobile/lib/features/onboarding/presentation/race_details_page.dart
 M mobile/test/goal_distance_custom_frontend_guard_test.dart
 M mobile/test/goal_distance_formatter_test.dart
```
(Excludes pre-existing `bin/`/`obj/`/`TestResults/` build-output noise and the reverted benign catalog-audit auto-regen — see §61.) No unexpected file present: exactly 7 modified + 1 added, matching §3.

## 71. Current public/dark matrix

| Target | Level/Freq | Public | Dark (materializable) |
|---|---|---|---|
| 15.0km | Intermediate/4D | **YES (new, this phase)** | YES (since DIST-GEN.6) |
| 16.0km | Intermediate/4D | YES (since DIST-GEN.3, unchanged) | YES (since DIST-GEN.2) |
| Any other target/level/frequency combination | — | NO | NO |

## 72. Rollback plan (repo-wide summary)

Full revert: `git checkout -- backend/RunningApp.Application/Commands/Plan/GeneratePreviewCommandMapper.cs backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs` restores the pre-phase single-target public gate exactly (15K becomes unreachable publicly again; 16K unaffected either way since it was never removed from the file, only its call site's method changed). Partial/targeted rollback: §54. Test-file reverts are optional (the removed DistGen6 tests would need to be either restored, accepting their now-false premise until the code revert also lands, or left as removed) — safest is to revert code and test files together as one unit.

## 73. DIST-GEN.8 recommendation (note only)

Two interior HALF_MARATHON-family targets (15K, 16K) are now both authority-closed and publicly proven, with one deliberately-differentiated numeric field (peak long-run ceiling) demonstrating the projection mechanism can carry genuinely target-specific authority, not just template-copying. A future governance phase could revisit distance-banding evidence broadly (e.g. whether a systematic sub-family exists between 12K-20K) now that two real, independently-authorized data points exist. This is a recommendation only — DIST-GEN.8 is explicitly NOT begun by this phase.

## 74. Remaining blockers

None. All regression suites are clean (PlanCatalog.Tests 1510/1510 zero-delta; RuntimeCatalog subtree 3945/3945 zero-delta; full monolithic 4975/4971/4-failed with the exact same 4 pre-existing failing identities as the DIST-GEN.6 durable baseline; flutter test suite fully green). No catalog JSON, no migration, no ledger/roadmap file touched. Source diff is exactly the 7 modified + 1 added files listed in §3/§70, with no unexpected file.

---

**15K_INTERMEDIATE_4D_PUBLIC_ACTIVATION_COMPLETE** — every criterion is met: public `target_distance_km=15.0` at Intermediate/4D works end-to-end through the real HTTP contract at every supported horizon (10-14W) with correct rejections outside that band (§7-§21); public 16K stays fully zero-delta (§8, §34, §58); the 14.5-vs-15.0 long-run-ceiling isolation is proven through the real public HTTP path (§25, §50), not only the internal harness; no training-authority numeric value was changed anywhere (§23-§31); the full regression suite is clean with zero new failing identity (§61, §65, §67-§68); no unexpected file diff, no catalog change, no migration (§61-§62, §70).
