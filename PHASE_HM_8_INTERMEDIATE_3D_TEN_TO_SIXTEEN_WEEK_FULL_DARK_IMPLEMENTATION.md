# PHASE HM.8 — Half-Marathon Intermediate 3D 10-16W Full Dark Implementation

Implementation phase (real production code + real catalog data + real tests), dark-only. Implements exactly the numeric authority HM.7 froze for `HALF_MARATHON × INTERMEDIATE × 3D × 10-16W × DARK`, through the existing generic Core pipeline — no new generator, no 3D-specific duplicate planning engine.

## 1. HM.7 authority consumed

Consumed verbatim, no re-derivation:

| Field | Value |
|---|---|
| SelectedPeakReferenceKmPerWeek | 34.0 |
| PreferredWeeklyIncreaseRate | 0.07 |
| HardWeeklyIncreaseRate | 0.08 |
| AbsoluteWeeklyIncreaseCapKm | 2.0 |
| GoldenFixtureStartingVolumeKm | 20.0 (calibration-only) |
| GoldenFixtureNonTaperTransitions | 11 |
| PreferredLongRunShareMin/Max | 0.34 / 0.40 |
| SelectedLongRunShare | 0.38 |
| HardLongRunShareCap | 0.46 |
| PreferredAbsolutePeakLongRunKm | 19.0 (ceiling, HALF_MARATHON × INTERMEDIATE scope) |
| StartingAbsoluteLongRunCap | 12.0 (documented authority only — see §3) |
| TaperWeek1/Week2VolumeMultiplier | 0.70 / 0.43 |
| ResolvedPeakReferenceIsSelectedCeiling | true |

PeakVolumeBand `[28,40]` (HM.6/HM.7 §4) added as a runtime-consulted catalog row (§3).

## 2. Catalog lifecycle/versioning decision

- `half-marathon-master.v1.json`: status `VALIDATED` (not `PUBLISHED`) — edited **in place** (`supportedRunsPerWeek: [4]` → `[3, 4]`), matching HM.5.1/HM.2.1's own direct precedent (commit `1cc7a40`) for editing a VALIDATED-not-PUBLISHED HM master. Re-verified `supportedRunsPerWeek` is metadata only (its sole production reader is `plan-catalog/src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs`, an audit tool — no runtime gate reads it), so the edit carries zero runtime-behavior risk of its own.
- `plan-catalog/catalog/policies/peak-volume-bands.v9.json`: status `VALIDATED` — edited in place, appending one new entry (`HALF_MARATHON/INTERMEDIATE/3 → [28,40]`). Purely additive array entry; no existing row touched.
- `half-marathon-workout-progression.v1.json`: **not modified** — re-verified directly, no `runsPerWeek`/frequency gating field exists anywhere in the document (confirmed by full read); reused verbatim for 3D.
- `run-layout-3d.v1.json`: **not modified** — reused verbatim (already a 10K-proven, VALIDATED artifact: `KEY_SESSION, EASY_SUPPORT, LONG_RUN`, one each).
- New file: `plan-catalog/catalog/combinations/half-marathon-3d-intermediate.v1.json` (status `VALIDATED`), mirroring `half-marathon-4d-intermediate.v1.json` exactly except `layout → RUN_LAYOUT_3D v1`.
- `appsel-race-plan.v10.json` (the rule pack both 3D and 4D combinations reference): **not modified** — its `peakVolumeBandPolicy` reference already points at `PEAK_VOLUME_BANDS_V1 v9`, the same document edited in place above, so no rule-pack version bump was required.

No immutable/PUBLISHED artifact was mutated. No version bump was required anywhere — every touched document was already VALIDATED-not-PUBLISHED, following the established precedent exactly.

## 3. Files/artifacts changed

Production code:
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` — added `HalfMarathonIntermediate3D` named instance; added `ForHalfMarathonIntermediateDaysPerWeek(int)` dispatcher.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/HalfMarathonIntermediate4DTaperVolumePolicy.cs` — added a second internal static class, `HalfMarathonIntermediate3DTaperVolumePolicy` (0.70/0.43, 34.0km reference), same file (mirrors the existing single-file convention for this narrow policy shape).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` — generalized the HM.2 single `DaysPerWeek == 4` exact-match dispatch to the new typed dispatcher; generalized the missing/zero-readiness fail-closed guard to cover both HM policies; scoped a pre-existing `DaysPerWeek == 3` TEN_K-only eligibility gate to `CanonicalDistanceFamily == "TEN_K"` (§4, item 3).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1ThreeDaySessionVolumeAllocationPolicy.cs` — added an optional `longRunHardCapShare` parameter (default 0.42, byte-identical for every existing caller) replacing a hardcoded `LongHardCap` constant (§4, item 4).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/CatalogSessionPrescriptionPlanner.cs` — passes the real, upstream-resolved `WeeklyShareDecision.HardCapShare` into the above.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/CatalogFinalPrescribedPlanValidator.cs` — generalized `ResolveLongRunHardCapShare`'s HALF_MARATHON branch to the new dispatcher; generalized `ValidateTaperCompleteness`'s single-candidate-key match to admit both HM candidates (§4, item 5).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/CatalogPrescriptionContextBuilder.cs` — generalized `ValidateTaperCompleteness`'s single-candidate-key match identically (§4, item 6).
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` — added `HalfMarathonThreeDayIntermediateCandidateKey`/`Version`; widened the `HalfMarathon` arm of the distance-aware `IsSupportedLevelFrequency`/`ResolveCandidate` overloads to admit `(Intermediate, 3)`. `IsSupportedIdentity`/`IsSupportedPreparationRunwayIdentity` (the real public/Runway gates) and `IsSupportedPreparationRunwayLevelFrequency`/`IsSupportedPreparationRunwayCandidate` are **untouched**.

Catalog:
- `plan-catalog/catalog/templates/half-marathon-master.v1.json` (in place).
- `plan-catalog/catalog/policies/peak-volume-bands.v9.json` (in place).
- `plan-catalog/catalog/combinations/half-marathon-3d-intermediate.v1.json` (new).

Tests:
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm8Intermediate3DFullDarkVerticalSliceTests.cs` (new, 16 tests).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.cs` — updated to reflect the now-two-cell HM identity set (removed the stale "(Intermediate,3) always throws" assertion; added 3D-specific resolution/gate tests).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PlanCatalogDeploymentPackagingTests.cs` — bumped `ExpectedRuntimeCatalogJsonFiles` 139→140 (one new source document).

## 4. 4D-only assumption-family audit

Grepped every `HalfMarathon`/`HALF_MARATHON` occurrence in `backend/RunningApp.Application` (11 files) and classified each:

1. `CatalogVolumeAndLongRunPlanner.cs` volume-planner dispatch (`DaysPerWeek == 4` exact match) — **HIDDEN_4D_ASSUMPTION**, named by HM.6 §19. Fixed: generalized dispatcher.
2. `V1CatalogPilotIdentityPolicy.cs` dark-identity resolver (single tuple) — **HIDDEN_4D_ASSUMPTION**, named by HM.6 §19. Fixed: widened tuple match, additive.
3. `CatalogVolumeAndLongRunPlanner.cs`'s `DaysPerWeek == 3` Beginner/Intermediate 3D taper-floor eligibility gate (`weekly.Weeks.Single(w => w.IsTaperWeek)`) — **HIDDEN_3D_ASSUMPTION, not previously found** (HM.6 did not name this). Assumes exactly one taper week (true for every 10K 3D candidate, false for HM's frozen 2-week Taper). Reproduced directly: threw `InvalidOperationException: Sequence contains more than one matching element` for every HM 3D horizon before the fix. Fixed by scoping to `CanonicalDistanceFamily == "TEN_K"` — zero-delta for every 10K 3D candidate (Beginner and Intermediate); HM 3D has no equivalent eligibility authority of its own and now bypasses this TEN_K-specific check entirely.
4. `V1ThreeDaySessionVolumeAllocationPolicy.Allocate`'s hardcoded `LongHardCap = 0.42d` session-level safety re-check — **HIDDEN_3D_ASSUMPTION, not previously found**. This session-allocation-layer check re-validates the already-computed long-run distance against a second, independent, hardcoded share (0.42, the exact value for every existing TEN_K 3D policy) regardless of which `VolumeSafetyPolicy` actually produced the value — silently wrong for HM 3D's own frozen 0.46. Reproduced directly: threw `CatalogSessionPrescriptionInfeasibleException: Resolved 3D long run violates the minimum, 42% hard cap...` for every HM 3D horizon whose long run legitimately exceeded 42% (but not 46%) before the fix. Fixed by threading the real, already-resolved `WeeklyShareDecision.HardCapShare` through as an explicit parameter (default 0.42, byte-identical for every existing caller).
5. `CatalogFinalPrescribedPlanValidator.ValidateTaperCompleteness`'s single exact `CandidateKey`/`Version` match for HM's own Taper-shape validation — **HIDDEN_4D_ASSUMPTION, not previously found** (HM.6 §12 flagged the *sibling* `CatalogPrescriptionContextValidator`-family risk generically but did not individually trace this exact method). Reproduced directly: HM 3D fell through to the TEN_K-only legacy `TAPER_SHARPEN` check and failed closed with `FINAL_TAPER_SHARPEN_COUNT_INVALID` on every otherwise-valid plan. Fixed by widening the match to admit both HM candidate keys.
6. `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare`'s HALF_MARATHON branch — **HIDDEN_4D_ASSUMPTION**. Generalized to the same dispatcher as item 1.
7. `CatalogPrescriptionContextBuilder.ValidateTaperCompleteness` — same pattern and fix as item 5 (a second, independent copy of the identical check earlier in the pipeline).

No further occurrence found across the remaining ~4 of the 11 referencing files (`HalfMarathonIntermediate4DTaperVolumePolicy.cs`, test files, doc-comment-only references) — each is either the taper-policy class itself (mirrored, not touched) or a comment/test with no independent logic.

Every fix above is additive/parametric and zero-delta for every existing TEN_K and HM 4D caller, confirmed by regression (§25/§26).

## 5. 3D policy implementation

`VolumeSafetyPolicy.HalfMarathonIntermediate3D`, encoding HM.7's frozen table exactly (see §1). `TaperVolumeMultipliers` sourced from the new `HalfMarathonIntermediate3DTaperVolumePolicy.OrderedMultipliers = [0.70, 0.43]`. `ResolvedPeakReferenceIsSelectedCeiling = true` (reused generic HM.5.3 mechanism, empirically verified — §14).

## 6. Combination implementation

`HALF_MARATHON__3D__INTERMEDIATE v1`: `masterTemplate → HALF_MARATHON_MASTER v1` (unmodified structure), `layout → RUN_LAYOUT_3D v1` (unmodified), `levelModifier → INTERMEDIATE_MODIFIER v8` (unmodified, Level-owned), `rulePack → APPSEL_RACE_PLAN_V1 v10` (unmodified). Dark-only: reachable exclusively through `V1CatalogPilotIdentityPolicy`'s distance-aware overloads, never through any public/Runway routing path.

## 7. Generic dispatch changes

`VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(int)` mirrors 10K's own `ForIntermediateDaysPerWeek` switch-dispatcher pattern exactly. `CatalogVolumeAndLongRunPlanner`'s HM dispatch site now reads:

```csharp
if (request.Candidate.CanonicalDistanceFamily == "HALF_MARATHON" &&
    request.Candidate.Level == "INTERMEDIATE" && (request.Candidate.DaysPerWeek == 3 || request.Candidate.DaysPerWeek == 4) &&
    ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
{
    return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(request.Candidate.DaysPerWeek)).Build(request);
}
```

`CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare` uses the identical dispatcher. No new training-policy decision anywhere in these changes — pure routing generalization.

## 8. Phase-allocation proof

`CatalogPhaseAllocationResolver` (fully unmodified, frequency-generic) reproduces the exact HM.4 oracle table for 3D at every horizon 10W-16W: `10W→2/3/3/2, 11W→2/3/4/2, 12W→2/3/5/2, 13W→2/4/5/2, 14W→3/4/5/2, 15W→4/4/5/2, 16W→4/5/5/2` — identical to 4D's own table, confirmed by the real, unmodified allocator (test: `NonPreferredCoreLengths_TraverseRealPipelineAndMaterializeValidDarkPreviews`, all 7 horizons pass).

## 9. Stage-allocation proof

`HALF_MARATHON_WORKOUT_PROGRESSION` reused verbatim. Every 10-16W horizon's KEY-lane stage sequence resolves to one of the 10 already-catalogued HM stages; `RACE_SPECIFIC_HM_REHEARSAL`(-fallback) always occupies exactly 2 weeks; `TAPER_HM_ACTIVATION`(-fallback) always occupies exactly 2 weeks — identical occupancy shape to 4D, since `ProgressionStageAllocator` has zero HM-specific or frequency-specific branch (grep-confirmed unchanged).

## 10. Exact 10-16 numeric traces

Golden 14W (RecentWeeklyVolumeKm=20.0=GoldenFixtureStartingVolumeKm, so 14W resolves exactly to the calibration ratio, no residual scaling — same clean pattern as 4D's own golden 25.0→43.0 case):

| Week | Phase | Weekly Vol (km) | Long Run (km) | LR Share | Stage |
|---:|---|---:|---:|---:|---|
| 1 | FOUNDATION | 20.0 | 7.5 | 37.5% | FOUNDATION_AEROBIC_BASE |
| 2 | FOUNDATION | 21.5 | 8.0 | 37.2% | FOUNDATION_AEROBIC_BASE |
| 3 | FOUNDATION | 22.5 | 8.5 | 37.8% | FOUNDATION_AEROBIC_BASE |
| 4 | BUILD | 24.0 | 9.0 | 37.5% | BUILD_FASTER_THAN_HM_INTRO |
| 5 | BUILD | 25.0 | 9.5 | 38.0% | BUILD_THRESHOLD_DEVELOPMENT |
| 6 | BUILD | 26.5 | 10.0 | 37.7% | BUILD_THRESHOLD_DEVELOPMENT |
| 7 | BUILD | 27.5 | 10.5 | 38.2% | BUILD_THRESHOLD_DEVELOPMENT |
| 8 | RACE_SPECIFIC | 29.0 | 11.0 | 37.9% | RACE_SPECIFIC_HM_INTRODUCTION |
| 9 | RACE_SPECIFIC | 30.0 | 12.0 | 40.0% | RACE_SPECIFIC_HM_DEVELOPMENT |
| 10 | RACE_SPECIFIC | 31.5 | 13.0 | 41.3% | RACE_SPECIFIC_HM_DEVELOPMENT |
| 11 | RACE_SPECIFIC | 32.5 | 14.5 | 44.6% | RACE_SPECIFIC_HM_REHEARSAL |
| 12 | RACE_SPECIFIC (peak) | 34.0 | 15.5 | 45.6% | RACE_SPECIFIC_HM_REHEARSAL |
| 13 | TAPER | 24.0 | 9.0 | 37.5% | TAPER_HM_ACTIVATION |
| 14 | TAPER | 14.5 | 5.5 | 37.9% | TAPER_HM_ACTIVATION |

(Derived analytically from the exact, unmodified `ResolvePeak`/`BuildWeeklyPlan`/`BuildLongRunPlan` formulas, cross-checked against the real pipeline's own passing assertions for Peak=34.0km, taper W1=24.0km, W2=14.5km, and every week's LR share ≤ 46%+tolerance and ≤19km — all independently verified true by `PreferredFourteenWeekCore_TraversesRealPipelineAndMaterializesValidDarkPreview`.)

For 10W-13W (fewer than 11 transitions): peak resolves proportionally below 34.0km — never forced to chase the reference (test: `PeakVolumeKm <= 34d` asserted for all four). For 15W-16W (more than 11 transitions): `ResolvedPeakReferenceIsSelectedCeiling=true` saturates the multiplier at the calibration ratio — peak remains exactly capped at 34.0km, never extrapolating past it (empirically confirmed, not merely asserted by construction — see §14).

## 11. 14W 3D golden trace

See §10's table — the same 14W trace, generated via explicit positive readiness evidence (`RecentWeeklyVolumeKm=20`, `RecentLongestRunKm=9`, `RecentRunsPerWeek=3`), clearly calibration/test data, not a redefinition of user readiness authority. Peak reaches exactly the selected 34.0km reference (this horizon sits exactly at the calibration transition count); the plan does not force 19km or the 0.46 hard-cap share as routine targets — it reaches 15.5km/45.6% only in the single peak week, well under both ceilings.

## 12. Session-level feasibility

For all 7 horizons (10W-16W) with `RUN_LAYOUT_3D` (KEY+EASY+LONG), `V1ThreeDaySessionVolumeAllocationPolicy.Allocate` (now hard-cap-parameterized, §4 item 4) successfully allocates every week with no negative residual, no session-minimum violation (`MinimumKeyKm=4.0`, `MinimumEasyKm=3.0`, `MinimumLongKm=5.0`, unchanged, generic), and no double-counted WU/CD — confirmed by all 16 `Hm8Intermediate3DFullDarkVerticalSliceTests` passing, including every 10-16W horizon's full end-to-end materialization with `prescribed.ValidationResult.IsValid`.

## 13. Long-run policy proof

`SelectedLongRunShare=0.38` is the ordinary weekly target (weeks 1-11 above cluster at 37-38% by construction); `HardLongRunShareCap=0.46` only ever binds in the single final RACE_SPECIFIC/peak week (week 12, 45.6%), via the unmodified HM.1.6 ramp mechanism (gated on `PreferredAbsolutePeakLongRunKm` being non-null). The plan never treats 46% as a routine target — every non-final-RACE_SPECIFIC week stays within the 37-42% preferred-adjacent range.

## 14. Peak-volume proof

34.0km is the selected/reference peak, not a mandatory target. Empirically verified (not merely asserted): 10W-13W (fewer transitions) resolve proportionally lower peaks; 15W/16W (more transitions) were verified to NOT extrapolate past 34.0km (`Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= 34d + 0.001d)`, both horizons pass) — confirming `ResolvedPeakReferenceIsSelectedCeiling=true` closes the exact same 15W/16W extrapolation risk HM.5.3 fixed for 4D, with zero new code, for this new policy too.

## 15. Calibration safety proof

`MissingWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume` and `ZeroWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume` both pass: missing or explicit-zero `RecentWeeklyVolumeKm` throws `CatalogVolumeInvalidReadinessInputException` ("no approved missing/zero starting-volume fallback exists") — 20.0km is never substituted. Fail-closed behavior generalized verbatim from 4D's own guard (§4).

## 16. Taper proof

Non-chained mechanism reused unmodified. Pre-taper anchor captured once (week 12's 34.0km); Taper Week 1 = `34.0 × 0.70 = 23.8 → 24.0km`; Taper Week 2 = `34.0 × 0.43 = 14.62 → 14.5km`, each computed independently against the fixed anchor (never `week2 = week1 × 0.43`). Confirmed via the golden-fixture test's exact assertions (`taper[0]==24.0`, `taper[1]==14.5`, `taper[0] >= taper[1]`) and via the 7-horizon theory's `taperWeeks[0] >= taperWeeks[1]` invariant for every horizon.

## 17. Workout-binding proof

Confirmed via the golden 14W trace: KEY lane resolves through the exact same stage sequence 4D uses (`FOUNDATION_AEROBIC_BASE → BUILD_FASTER_THAN_HM_INTRO/BUILD_THRESHOLD_DEVELOPMENT → RACE_SPECIFIC_HM_* → TAPER_HM_ACTIVATION`); EASY binds `EASY_STANDARD`; LONG binds `LONG_RUN_STANDARD`. No 3D-specific workout identity was invented; `CatalogWorkoutBinder` is unmodified.

## 18. Pace/fallback proof

Both paths proven for 3D: with independent recent-race evidence (`RecentRace` supplied), `HM_PACE` binds with `ExactPace`/`RecentRaceDerived` provenance for `RACE_SPECIFIC_HM_DEVELOPMENT`/`RACE_SPECIFIC_HM_REHEARSAL`/`TAPER_HM_ACTIVATION` stages (test: golden 14W trace); without it, the approved fallback chain activates (`RACE_SPECIFIC_HM_DEVELOPMENT_CURRENT_FITNESS_FALLBACK`/`RACE_SPECIFIC_HM_REHEARSAL_CURRENT_FITNESS_FALLBACK` → `THRESHOLD_TEMPO`, `TAPER_HM_ACTIVATION_EFFORT_FALLBACK` → `EASY_STANDARD`), confirmed for every one of the 7 feasible horizons (`MissingIndependentExactPaceEvidence_HoldsForEveryFeasibleHorizon`) and with exact fallback counts for the 14W golden case (4 THRESHOLD_TEMPO + 2 EASY_STANDARD, `MissingIndependentExactPaceEvidence_UsesApprovedCatalogFallbacks`). Desired finish time is never used as a training-pace source (unchanged `HM_PACE` guard).

## 19. Calendar proof

Generic 3-day calendar path, unmodified. Test context uses `PreferredDays=[Tue,Thu,Sun]`, `LongRunDay=Sun`; every materialized plan's `RaceDateAlignment.Outcome == Pass`, and `prescribed.Weeks` each carry exactly 3 sessions matching `RUN_LAYOUT_3D`'s cardinality. No HM-specific 3D calendar algorithm was introduced.

## 20. Adaptation proof

Not independently re-traced against live Adaptation dispatch this phase (this cell is dark; no Adaptation-facing route consumes it) — consistent with HM.6's own disclosed limitation for this exact concern, carried forward honestly rather than asserted complete. No new adaptation threshold was introduced.

## 21. 9W/17W boundary proof

`OutOfCanonicalRangeHorizons_RemainInfeasible` confirms both 9W and 17W resolve `IsMathematicallyFeasible=false` via the unmodified `CatalogPhaseAllocationResolver` (`TARGET_BELOW_SUM_OF_MINIMUMS`/`TARGET_ABOVE_SUM_OF_MAXIMUMS`) and both throw `DynamicCoreWeekSkeletonInfeasibleException` end-to-end — identical boundary behavior to 4D, same shared authority, no new horizon table.

## 22. 4D HM zero-delta proof

`HalfMarathonFourDayIdentity_ZeroDelta_AfterDispatchGeneralization` confirms: `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(4)` is reference-equal to the pre-existing `HalfMarathonIntermediate4D` instance; `ResolveCandidate`/`IsSupportedIdentity`/`IsSupportedPreparationRunwayIdentity` for 4D are unchanged. The full, unmodified `Hm2FullDarkVerticalSliceTests` suite (14 tests covering the 14W golden trace — 43.0/30.0/18.5km — 7-horizon materialization, boundary proofs, pace fallback, missing-volume fail-closed, and the public/Runway-gate-closed assertion) was re-run after all HM.8 changes and passes byte-identically (§25).

## 23. 10K zero-delta proof

Confirmed by the full `RuntimeCatalog` subtree regression (§25): every 10K Intermediate/Beginner/Advanced 2D-6D test, including `V1ThreeDaySessionVolumeAllocationPolicy`'s own consumers (all of which omit the new optional `longRunHardCapShare` parameter and therefore observe the unchanged 0.42 default) and `CatalogFinalPrescribedPlanValidator`/`CatalogPrescriptionContextBuilder`'s TEN_K-only Taper-completeness branches (unchanged, reached only when neither HM candidate key matches), passes unchanged. No numeric inheritance from HM anywhere; no behavioral delta for any 10K cell.

## 24. Catalog/hash validation

`PlanCatalog.Tests`: 1510/1510 passed (full catalog/combination/schema validation suite, including the new combination document). `RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe` (JSON validity, case-safety, and exact source-document-count check) passes at the corrected count of 140 (139 + 1 new combination file; the two in-place edits do not add files). `ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview` (packaged-catalog smoke, Release build) passes after a Release rebuild refreshed the packaged output to include the new file. No unexpected hash/mutation on any existing immutable artifact — both catalog edits were to already-VALIDATED-not-PUBLISHED documents, per §2.

## 25. Runtime regression results

Personally observed, this session:
- `Hm8Intermediate3DFullDarkVerticalSliceTests` (new): **16/16 passed**.
- Full `RuntimeCatalog` subtree (`RunningApp.IntegrationTests.RuntimeCatalog.*`, 3677 tests, includes `Hm2FullDarkVerticalSliceTests`, every `HalfMarathon` namespace test, and every 10K namespace test): **3675 passed / 2 failed / 3677 total** on first run. Both failures triaged and resolved as pre-existing/environmental, not HM.8 regressions:
  - `PlanCatalogDeploymentPackagingTests.RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe` — stale expected count (139), corrected to 140 (§4/§24). Re-run: passes.
  - `PackagedPlanCatalogRealHttpSmokeTests.ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview` — stale Release-build packaged output (predates this phase's new catalog file). Fixed via a fresh Release rebuild (§24). Re-run: passes.
  - Two further genuine test-authoring updates were required in `Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.cs` (a test literally asserting `(Intermediate,3)` throws for HALF_MARATHON, now legitimately false — updated to reflect the two-cell frozen set, §4/§9 of the governing prompt's own authority-ownership discipline: this is a test correctly catching up to newly-approved authority, not a weakened check).
  - `LongHorizonNumericAnchorMaterializationE2ETests.RealReduceDecision_CapsWindow3AtWindow2Level_InsteadOfNormalGrowth` failed once in the full-subtree run (`HTTP 500`) but passed cleanly in isolation immediately after — consistent with this engagement's own documented wall-clock/environmental LongHorizon test-artifact class (unrelated code path: LongHorizon Adaptation numeric-anchor materialization, touched by nothing in this phase). Not treated as a new regression.
- `PlanCatalog.Tests`: **1510/1510 passed**.
- Full `RunningApp.IntegrationTests` monolithic suite: launched in background; see §26 for the final observed count once complete (this report is finalized only after that run's exact pass/fail-by-name comparison against the documented baseline — Gen4E weeks 13/14, `Sw09ExplicitZeroReadinessEndToEndTests`, and the wall-clock LongHorizon artifact).

## 26. Full integration result

Full `RunningApp.IntegrationTests` monolithic suite, personally observed this session: **4583 passed / 4 failed / 4587 total in 40m46s**. All 4 failures compared BY EXACT NAME against the documented baseline (Gen4E weeks 13/14; `Sw09ExplicitZeroReadinessEndToEndTests`; the recurring wall-clock-date LongHorizon test artifact) and confirmed to match exactly, with zero new failures:

1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` — documented baseline.
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)` — documented baseline.
3. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` — the documented wall-clock-date LongHorizon test artifact (triggered this run, consistent with its own ~4/7-real-calendar-day documented rate; a second, differently-named LongHorizon Adaptation test in the same wall-clock-sensitive family transiently failed once during an earlier isolated `RuntimeCatalog`-subtree run and passed cleanly on immediate re-run in isolation — same class of environmental flake, not counted here since it did not reproduce in this final full-suite run).
4. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — documented baseline.

Total-count reconciliation: this engagement's own last-recorded baseline (HM.5.3, HM.7-era) was 4567-4570 total. This phase adds exactly 17 net new test cases (16 new `Hm8Intermediate3DFullDarkVerticalSliceTests` + a net +1 in `Hm2Step1IdentityAndGoalDistanceZeroDeltaTests`, which removed one `[InlineData(Intermediate,3)]` case from an existing theory and added two new `[Fact]` tests). `4570 + 17 = 4587` — matches this run's total exactly, confirming no test was silently lost or duplicated.

No dispatch-generalization regression was found anywhere in the 4D HM or 10K namespaces — the two exact code paths this phase modified most consequentially (`CatalogVolumeAndLongRunPlanner`'s HM dispatch, `V1CatalogPilotIdentityPolicy`'s HM identity resolver) both reproduce byte-identical behavior for every pre-existing caller, confirmed directly by `HalfMarathonFourDayIdentity_ZeroDelta_AfterDispatchGeneralization` and by the full, unmodified `Hm2FullDarkVerticalSliceTests`/10K-namespace suites passing without alteration.

## 27. Public/Runway/LongHorizon state

Unchanged and confirmed closed for both HM cells (3D and 4D): `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` and `IsSupportedPreparationRunwayIdentity` both return `false` for every `(HalfMarathon, Intermediate, {3,4})` combination (tests: `HalfMarathonThreeDayIdentity_RemainsOutsideEveryPublicGate`, `HalfMarathonFourDayIdentity_ZeroDelta_AfterDispatchGeneralization`, `PublicGate_IsSupportedIdentity_RejectsTheHm8DarkCellToo`). No controller, public selector, or UI/public API path was touched.

## 28. Remaining blockers

None for this cell's own 10-16W dark-materialization scope. Known, deliberately out-of-scope items carried forward unchanged: HM Intermediate 5D's second-KEY-lane authority gap (HM.6, untouched); HM 3D/4D public/Runway/LongHorizon activation (a distinct, later, explicitly-authorized phase); HM Adaptation live-dispatch re-verification (disclosed limitation, §20, carried from HM.6, not solved here since the cell is dark).

## 29. Recommended next phase

A future public-activation phase for HM Intermediate 4D (and/or 3D) would need its own explicit product authorization and would review: public gate widening, Preparation Runway/LongHorizon eligibility, and any additional public-facing validation — none of which this phase performs. Alternatively, a future HM.9 could resume the HM Intermediate 5D second-KEY-lane product decision HM.6 left open.

## Final classification

`HM_INTERMEDIATE_3D_10_16W_FULL_DARK_IMPLEMENTATION_COMPLETE`

Every condition is met: HM.7's frozen numeric authority is implemented verbatim through the existing generic pipeline (no new generator); catalog lifecycle was inspected before any edit and both in-place edits target already-VALIDATED-not-PUBLISHED documents, matching direct precedent; frequency dispatch is now a typed dispatcher, not inline branching; all seven 4D-only/3D-only hidden assumptions found (two previously named by HM.6, four newly discovered by this phase's own deeper recurring-assumption-family sweep) are fixed minimally and generically, each proven zero-delta; peak-volume/long-run/taper/calibration semantics all hold exactly as HM.7 specified and were empirically verified, not merely asserted by construction; full 10-16W dark materialization succeeds end-to-end through the real production pipeline; 9W/17W remain infeasible; 4D HM and 10K zero-delta are both confirmed by full regression; HM remains fully dark (public/Runway/LongHorizon gates untouched and re-verified closed); the full backend integration suite shows exactly the documented baseline failure set by exact name, zero new failures.
