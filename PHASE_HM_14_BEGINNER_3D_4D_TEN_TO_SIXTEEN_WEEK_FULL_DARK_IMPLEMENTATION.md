# PHASE HM.14 — HALF-MARATHON BEGINNER 3D/4D 10–16W FULL DARK IMPLEMENTATION

**Type:** PRODUCTION IMPLEMENTATION (not governance/audit)
**Scope:** HALF_MARATHON × BEGINNER × {3D, 4D} × CORE 10–16W × DARK. HALF_MARATHON × BEGINNER × 5D is explicitly out of scope (PRODUCT_INELIGIBLE per HM.13 §6).

---

## 1. HM.13 authority consumed

`PHASE_HM_13_BEGINNER_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md` was read in full before any code was written. §24's own complete Beginner 3D/4D policy tables were extracted and cross-checked against this prompt's own restated numbers — **no discrepancy was found**; every value (peak bands, selected peaks, growth rates/caps, calibration starts, LR-share quadruples, absolute peak LR, starting-LR-cap N/A, taper multipliers, hard-stimulus cap, HM_PACE eligibility, 5D ineligibility) matches exactly. This report implements §24/§28's tables and recommended shape verbatim.

## 2. Product matrix implemented

| Cell | Status |
|---|---|
| HALF_MARATHON × BEGINNER × 3D | Implemented, dark-only |
| HALF_MARATHON × BEGINNER × 4D | Implemented, dark-only |
| HALF_MARATHON × BEGINNER × 5D | Not implemented — `PRODUCT_INELIGIBLE` (HM.13 §6), proven by regression, not silently omitted |

## 3. Catalog lifecycle/versioning

- `peak-volume-bands.v9.json` (status `VALIDATED`, not `PUBLISHED`) edited **in place** — two new rows added (`HALF_MARATHON`/`NEW`/3→[22,32], 4→[28,38]), mirroring the exact in-place-edit precedent HM.7/HM.8/HM.10 already used for this same file.
- `level-modifiers/beginner-modifier.v2.json` — new, additive version. Adds `HM_PACE v1` to `eligibleWorkouts`, drops `GOAL_PACE_TEN_K` (mirrors `INTERMEDIATE_MODIFIER v8`'s own exact shape, the version already consumed by all three HM Intermediate combinations). 10K's own combinations remain on `BEGINNER_MODIFIER v1`, byte-unchanged.
- Two new `TEMPLATE_COMBINATION` files: `half-marathon-3d-beginner.v1.json`, `half-marathon-4d-beginner.v1.json`. Both reference the **existing, unmodified** `HALF_MARATHON_MASTER v1`, `RUN_LAYOUT_3D v1`/`RUN_LAYOUT_4D v1`, `APPSEL_RACE_PLAN_V1 v10` rule pack (same version already used by all three HM Intermediate combinations), and the new `BEGINNER_MODIFIER v2`. Zero new master/layout/workout-progression versions — matches HM.13 §28's own recommended shape (Beginner adds no second lane, unlike 5D).

## 4. Files/artifacts changed

**Catalog:**
- `plan-catalog/catalog/policies/peak-volume-bands.v9.json` (edited in place)
- `plan-catalog/catalog/level-modifiers/beginner-modifier.v2.json` (new)
- `plan-catalog/catalog/combinations/half-marathon-3d-beginner.v1.json` (new)
- `plan-catalog/catalog/combinations/half-marathon-4d-beginner.v1.json` (new)

**Production code:**
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` — added `HalfMarathonBeginner3D`, `HalfMarathonBeginner4D` named policies and `ForHalfMarathonBeginnerDaysPerWeek(int)` dispatcher (no 5-arm, fails closed via `ArgumentOutOfRangeException`).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/HalfMarathonIntermediate4DTaperVolumePolicy.cs` — added `HalfMarathonBeginner3DTaperVolumePolicy`, `HalfMarathonBeginner4DTaperVolumePolicy` internal static classes.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` — added a new, parallel, explicitly Beginner-scoped dispatch block (`Level == "NEW"`, separate from the Intermediate block); widened the missing/zero-readiness fail-closed guard to cover the two new policies; **fixed a real pre-existing bug** (see §31/§6 below) in the Beginner-4D taper-floor eligibility check that was unconditional on `CanonicalDistanceFamily`.
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` — added `HalfMarathonThreeDayBeginnerCandidateKey`/`Version`, `HalfMarathonFourDayBeginnerCandidateKey`/`Version`; widened the dark-only `IsSupportedLevelFrequency(GoalDistance,...)` HalfMarathon arm and `ResolveCandidate`/`TryResolveCandidate` to admit `(Beginner,3)`/`(Beginner,4)`, explicitly not `(Beginner,5)`. The TenK-only overloads and `IsSupportedPreparationRunwayLevelFrequency` are untouched.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/CatalogFinalPrescribedPlanValidator.cs` — widened `ResolveLongRunHardCapShare` and `ValidateTaperCompleteness` to admit the two new Beginner candidate keys (single-lane taper shape).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/CatalogPrescriptionContextBuilder.cs` — widened its own `ValidateTaperCompleteness` the same way (a third, independent occurrence of the same HM.8-established generalization pattern).

**Tests:**
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm14Beginner3D4DFullDarkVerticalSliceTests.cs` (new, 39 tests)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PlanCatalogDeploymentPackagingTests.cs` (updated `ExpectedRuntimeCatalogJsonFiles` 145→148, with a full HM.14 provenance comment mirroring the existing per-phase comment convention)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.cs` (moved `(Beginner,4)` out of the "not resolvable" theory into two new dedicated `HalfMarathon_Beginner{Three,Four}Day_ResolvesDarkOnly` tests, renamed the remaining theory to reflect five frozen cells)

No `PHASE_LEDGER.md`/`MASTER_ROADMAP.md` edits, no commits, no pushes — left for the orchestrating session per instruction.

## 5. Beginner modifier wiring

`BEGINNER_MODIFIER v2`'s `experience` field remains `"NEW"` (unchanged from v1). `PlanCatalogBundleLoader.cs:105` (`Level = experience`) means every HALF_MARATHON Beginner candidate's `PlanCatalogCandidateSummary.Level` resolves to the literal string `"NEW"`, confirmed by direct read and re-verified end-to-end via the new test suite (all 39 tests pass, including `BeginnerThreeDayTaperFloor_ActivatesGenericallyWithNoNewHmSpecificCode`, which depends on this exact mapping). `eligibleWorkouts` = `[EASY_STANDARD v4, LONG_RUN_STANDARD v4, FARTLEK v4, THRESHOLD_TEMPO v4, HM_PACE v1]`, mirroring `INTERMEDIATE_MODIFIER v8` (the version already consumed by all three HM Intermediate combinations) exactly. `progressionModifier` still points at `BEGINNER_PROGRESSION_MODIFIER_V1 v1`, unchanged.

## 6. Beginner progression compatibility

No new `HALF_MARATHON_WORKOUT_PROGRESSION` version was needed — the existing v1 (single-lane) document, already proven for Intermediate 3D/4D, is directly reusable. Confirmed live via the 14W golden test: `FOUNDATION_AEROBIC_BASE → BUILD_FASTER_THAN_HM_INTRO/BUILD_THRESHOLD_DEVELOPMENT → RACE_SPECIFIC_HM_INTRODUCTION/_DEVELOPMENT/_REHEARSAL → TAPER_HM_ACTIVATION` all bind correctly for a `Level=="NEW"` candidate with zero new stage-content authority.

## 7. Typed policy dispatch

`VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(int)`: `3 => HalfMarathonBeginner3D, 4 => HalfMarathonBeginner4D, _ => throw ArgumentOutOfRangeException` (deliberately no 5-arm). `CatalogVolumeAndLongRunPlanner.Build` dispatches to it via a **separate** condition block keyed on `CanonicalDistanceFamily == "HALF_MARATHON" && Level == "NEW" && DaysPerWeek is 3 or 4` — not folded into the Intermediate branch, per instruction. `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare` mirrors the same dispatch shape.

## 8. 5D product-ineligible proof

No `HALF_MARATHON__5D__BEGINNER` combination file exists. No `(RunningBackground.Beginner, 5)` tuple arm exists anywhere (`V1CatalogPilotIdentityPolicy`'s HalfMarathon switch, `ResolveCandidate`, `VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek`). Proven by the typed mechanism already used by every other unsupported HM cell — no new exception type was invented:
- `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency(HalfMarathon, Beginner, 5)` → `false`
- `TryResolveCandidate(HalfMarathon, Beginner, 5)` → `null`
- `ResolveCandidate(HalfMarathon, Beginner, 5)` → throws `ArgumentOutOfRangeException`
- `VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(5)` → throws `ArgumentOutOfRangeException`
- Intermediate x5D remains resolvable dark, unaffected

All four asserted directly by `HalfMarathonBeginnerFiveDay_RemainsProductIneligible` (passing).

## 9. Exact 3D policy encoding

`HalfMarathonBeginner3D`: PreferredRate 0.07, HardRate 0.08, AbsoluteCap 2.0km, CalibrationStart 15.5km, SelectedPeak 27.0km, NonTaperTransitions 11, LR shares 0.34/0.40/0.38/0.46, PreferredAbsolutePeakLR 16.0km, StartingAbsoluteLongRunCap null, Taper multipliers [0.70, 0.43], `ResolvedPeakReferenceIsSelectedCeiling=true`. Verified by `HalfMarathonBeginner3D_ExactPolicyEncoding` (passing).

## 10. Exact 4D policy encoding

`HalfMarathonBeginner4D`: PreferredRate 0.07, HardRate 0.08, AbsoluteCap 2.5km, CalibrationStart 19.0km, SelectedPeak 33.0km, NonTaperTransitions 11, LR shares 0.30/0.36/0.33/0.40, PreferredAbsolutePeakLR 16.0km, StartingAbsoluteLongRunCap null, Taper multipliers [0.70, 0.43], `ResolvedPeakReferenceIsSelectedCeiling=true`. Verified by `HalfMarathonBeginner4D_ExactPolicyEncoding` (passing).

## 11. 10–16 3D traces

`NonPreferredCoreLengths_ThreeDay_TraverseRealPipelineAndMaterializeValidDarkPreviews` (Theory, 10W–16W): phase allocation matches the frequency-generic 7-row oracle table exactly at every horizon; peak volume never exceeds 27.0km at any horizon (short horizons don't chase it, long horizons don't extrapolate past it); weekly absolute increase never exceeds 2.0km; LR share never exceeds 0.46, LR distance never exceeds 16.0km; taper stays non-chained at every horizon. All 7 horizons pass.

## 12. 10–16 4D traces

Same theory for 4D: peak never exceeds 33.0km; absolute increase never exceeds 2.5km; LR share never exceeds 0.40, LR distance never exceeds 16.0km; taper non-chained. All 7 horizons pass.

## 13. 14W 3D golden

`PreferredFourteenWeekCore_ThreeDay...`: RecentWeeklyVolumeKm=15.5 (=GoldenFixtureStartingVolumeKm) resolves cleanly to PeakVolumeKm=27.0 at the 11-transition calibration point. Phase split 3/4/5/2, 42 sessions, taper weeks 19.0/11.5km (non-chained: 11.5 ≠ round(19.0×0.43)=8.5). Full pipeline validation passes; public preview materializer accepts the plan.

## 14. 14W 4D golden

RecentWeeklyVolumeKm=19.0 resolves to PeakVolumeKm=33.0. Phase split 3/4/5/2, 56 sessions, taper weeks 23.0/14.0km. Full validation passes.

## 15. Dose/modifier proof

`beginner-progression-modifier.v1.json`'s `maximumHardSessionsPerWeek=1`/`allowSecondHardStimulus=false` holds structurally at every horizon for both frequencies — single-KEY-lane `RUN_LAYOUT_3D`/`RUN_LAYOUT_4D` admit exactly one `KEY_SESSION` per week by construction. Asserted explicitly (`Assert.Single(... "KEY_SESSION")`) in the golden tests and the non-preferred-horizon theory (14 horizon-runs total, all pass).

## 16. HM_PACE/fallback proof

`ExactPaceEvidence_BindsHmPace_ForBeginner` (both frequencies): with `PACE_SOURCE_IN:RECENT_RACE` + `GOAL_FEASIBILITY_IN:REALISTIC` evidence, `HM_PACE` binds at RACE_SPECIFIC/TAPER stages. `MissingIndependentExactPaceEvidence_FallsBackAndNeverFabricatesFromDesiredFinishTime` (both frequencies, all 7 horizons): with no evidence, `HM_PACE` never binds, and no non-`GOAL_PACE_TEN_K`/`HM_PACE` session ever carries an `ExactPace` prescription kind — proving the desired-finish-time is never fabricated into a pace for a fallback (`THRESHOLD_TEMPO`/`EASY_STANDARD`) session.

## 17. eligibleWorkouts runtime finding

Re-confirmed HM.13 §17's finding directly: `DynamicCoreWorkoutBindingOrchestrator.cs` never references `eligibleWorkouts`/`EligibleWorkouts`; the sole consumer is `PlanCatalogBundleLoader.ReadEligibleWorkouts`, a catalog-packaging dependency-bookkeeping read. The real runtime HM_PACE gate is the workout-progression stage's own level-blind `requires` conditions. `BEGINNER_MODIFIER v2` was created (adding `HM_PACE v1`) as a catalog-completeness declaration, not because the binder needed it — verified by running the Release packaging smoke test (`PackagedPlanCatalogRealHttpSmokeTests.ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview`), which passes after a full Release rebuild picked up the new catalog files.

## 18. Calibration safety

`MissingWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume` and `ZeroWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume` (both frequencies): both throw `DynamicCoreVolumeAndLongRunFailedException` wrapping `CatalogVolumeInvalidReadinessInputException` with message "no approved missing/zero starting-volume fallback exists" — 15.5/19.0 never substitute for missing or explicit-zero readiness, mirroring Intermediate 3D/4D/5D's own already-established fail-closed guard, mechanically extended.

## 19. LR/rounding proof

Asserted at every horizon for both frequencies: `LongRunShareOfWeeklyVolume <= hardCapShare + tolerance` and `PlannedLongRunDistanceKm <= 16.0 + tolerance`. Neither ceiling actually binds at either cell's own selected peak × selected share (3D: 27.0×0.38=10.26km; 4D: 33.0×0.33=10.89km — both far below 16.0km), matching HM.13 §22's own disclosure that the absolute ceiling is real but not the active constraint for Beginner.

## 20. Taper proof

Both cells: exactly 2 taper weeks, non-increasing (`taper[0] >= taper[1]`), and explicitly non-chained — 3D's taper[1] (11.5) is asserted `!= round(taper[0] × 0.43, 1)` (8.5), proving Week 2 is computed against the fixed pre-taper anchor (27.0), not chained from Week 1's own output.

## 21. Beginner taper-floor proof

`BeginnerThreeDayTaperFloor_ActivatesGenericallyWithNoNewHmSpecificCode`: confirms `CatalogSessionPrescriptionPlanner`'s existing `useBeginnerThreeDayTaperMinima = isThreeDay && weekly.IsTaperWeek && request.Candidate.Level == "NEW"` check fires correctly for the new HM Beginner 3D candidate — Week 2's long run (4.5km, below the normal 5.0km floor) is accepted because the lowered Beginner-specific 3.0km floor applies, with **zero new HM-specific code**. This is the exact HM.13 §19 finding, re-verified end-to-end rather than re-derived.

## 22. Session-allocation proof

Both frequencies route through the existing, unmodified `V1ThreeDaySessionVolumeAllocationPolicy`/`V1FourDaySessionVolumeAllocationPolicy`/`CatalogSessionPrescriptionPlanner` — no new allocator class. No negative residual or minimum-floor violation was observed at any of the 14 horizon-runs (7 per frequency) plus both golden 14W traces — all pass validation.

## 23. Calendar proof

`bound.Weeks.SelectMany(...).WorkoutDefinitionKey` non-empty at every session, every horizon, both frequencies. Full pipeline `BoundCatalogPlanValidator`/`GeneratedCatalogPlanSkeletonValidator`/`DatedGeneratedCatalogPlanSkeletonValidator` all pass (no test would pass otherwise, since they run inside the same real pipeline call).

## 24. Adaptation result

`AdaptationSmoke_AllCombinationsProduceConservativeDecisions` (both frequencies): generic `NextWindowLoadDecisionPolicy` (no HM/Beginner-specific code) applied honestly. A real, disclosed asymmetry was found and documented in the test, not hidden: a 3-session week's all-complete case resolves to `Maintain` (not `ProgressAsPlanned`) because the legacy decision table's `ProgressAsPlanned` threshold is `EffectiveCompletedCount>=4`, unreachable for a genuine 3-session week, and `OnlyEasyMissing` requires something to actually be missing. A 4-session week's all-complete case correctly resolves to `ProgressAsPlanned`. Both are pre-existing, level-blind, unmodified generic behavior — not a regression, and not something HM.14 introduced or should paper over.

## 25. 9W/17W boundary proof

`OutOfCanonicalRangeHorizons_RemainInfeasible` (both frequencies × {9,17}, 4 cases): phase allocation reports infeasible with the correct reason code (`TARGET_BELOW_SUM_OF_MINIMUMS`/`TARGET_ABOVE_SUM_OF_MAXIMUMS`), and the real pipeline throws `DynamicCoreWeekSkeletonInfeasibleException`.

## 26. Intermediate HM zero-delta

`IntermediateHalfMarathonCells_ZeroDelta_AfterBeginnerDispatchAddition`: `ForHalfMarathonIntermediateDaysPerWeek(3/4/5)` still resolve to the same named instances with the same `ResolvedPeakReference` values (34.0/43.0/51.0), and `ResolveCandidate` for all three Intermediate cells is unchanged. The full pre-existing HM Intermediate 3D/4D/5D dark test suites (`Hm2FullDarkVerticalSliceTests`, `Hm8Intermediate3DFullDarkVerticalSliceTests`, `Hm11Intermediate5DFullDarkVerticalSliceTests`) were run as part of the full RuntimeCatalog subtree and pass unchanged.

## 27. 10K zero-delta

`TenKBeginnerThreeAndFourDay_ZeroDelta_AfterHalfMarathonBeginnerAddition`: 10K's own `ForBeginnerDaysPerWeek(3/4)` still resolve to `ThreeDayBeginner`/`BeginnerFourDay` (peaks 17.0/21.0, unchanged), and both remain publicly resolvable exactly as before. `BEGINNER_MODIFIER v1` (10K's own version) is untouched; only the new, additive `v2` exists.

## 28. Catalog/hash proof

`PlanCatalogDeploymentPackagingTests.RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe` updated from 145→148 (the exact 3 new files: `beginner-modifier.v2.json`, `half-marathon-3d-beginner.v1.json`, `half-marathon-4d-beginner.v1.json`) with a full provenance comment matching every prior phase's own convention. `PackagedPlanCatalogRealHttpSmokeTests.ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview` passes after a full Release rebuild.

## 29. Regression results

1. `dotnet build RunningApp.sln -c Debug` — 0 errors (11 pre-existing warnings, none new).
2. `dotnet build RunningApp.sln -c Release` — 0 errors (14 pre-existing warnings, none new).
3. `PlanCatalog.Tests` (plan-catalog/tests/PlanCatalog.Tests) — **1510/1510 passing, 0 failures** (unchanged baseline; this suite uses its own isolated `TestCatalog` fixture, not the live catalog, so it required no new coverage from this phase).
4. `RunningApp.IntegrationTests --filter FullyQualifiedName~RuntimeCatalog` — **3737/3737 passing, 0 failures** (independently re-confirmed twice by the orchestrating session for stability; corrects this report's own earlier draft figure of 3767, a transcription inaccuracy with no functional significance — 0 failures either way) after fixes (initial run surfaced 2 real, expected breaks from this phase's own additive widening — see §31 — both fixed; a third failure, the Release packaging smoke test, was resolved by rebuilding Release, not by a code change).
5. Full monolithic `RunningApp.IntegrationTests` regression (isolated run, no concurrent `dotnet test` process, verified via process list before starting): **4643 passed, 4 failed, 0 skipped, 4647 total, duration 40m40s**. The exact 4 failures, by name, are precisely the documented durable baseline — zero new regressions anywhere, including 10K and every existing HM namespace:
   - `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
   - `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
   - `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
   - `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

## 30. Public/Runway/LongHorizon state

Untouched and re-verified closed: `IsSupportedIdentity`/`IsSupportedPreparationRunwayIdentity` both return `false` for every HALF_MARATHON Beginner combination (3D/4D/5D) in the new test suite. The TenK-only `IsSupportedLevelFrequency(RunningBackground,int)` overload and `IsSupportedPreparationRunwayLevelFrequency` were not modified.

## 31. Remaining blockers / disclosed findings

None blocking. Two genuine, pre-existing issues were found and are disclosed here (both fixed, not swept aside):

1. **Real bug found and fixed**: `CatalogVolumeAndLongRunPlanner.Build`'s Beginner×4D taper-floor eligibility check (`if (request.Candidate.Level == "NEW" && request.Candidate.DaysPerWeek == 4)`) was unconditional on `CanonicalDistanceFamily`, assuming TEN_K's own single-week Taper phase and TEN_K's own `V1BeginnerFourDayVolumeEligibilityPolicy` floor. Against HALF_MARATHON's frozen 2-week Taper phase, `.Single(w => w.IsTaperWeek)` would have thrown for the very first HM Beginner 4D request. Fixed by scoping the check to `CanonicalDistanceFamily == "TEN_K"` explicitly (zero delta for every existing TEN_K Beginner×4D candidate), mirroring the identical fix HM.8 already made for the neighboring 3-day check.
2. **Two pre-existing tests required updates, not fixes** (both are expected, additive-widening consequences, not defects): `PlanCatalogDeploymentPackagingTests`'s hardcoded file count (145→148) and `Hm2Step1IdentityAndGoalDistanceZeroDeltaTests`'s "only N frozen cells" theory (moved `(Beginner,4)` into its own dedicated resolvable-test, matching the exact precedent already set for `(Intermediate,3)`/`(Intermediate,5)` by HM.8/HM.11).

## 32. Beginner completion conclusion

HALF_MARATHON × BEGINNER × {3D,4D} × CORE 10–16W × DARK is now implemented through the real production pipeline using the same additive, zero-delta, dark-only pattern HM.8/HM.11 proved for Intermediate. HALF_MARATHON × BEGINNER × 5D remains correctly, deliberately, and verifiably absent (`PRODUCT_INELIGIBLE`). Combined with HM.1–HM.13, HALF_MARATHON's Core dark matrix now covers Intermediate {3D,4D,5D} and Beginner {3D,4D} — 5 of the 5 cells HM.13 froze authority for, 0 cells implemented without authority.

## 33. Recommended Advanced phase

The next natural phase is HALF_MARATHON × ADVANCED numeric/level authority closure (mirroring HM.12/HM.13's own governance-then-implementation split), following the same evidence discipline: 10K's own `Advanced3D/4D/5D/6D` policies and the `HM_21K_Claude_Handoff_and_Build_Plan.md`'s own named rollout matrix (which marks Advanced 3D/4D/5D all as `TARGET`) are the most on-point starting evidence.

---

## Final classification

```
HM_BEGINNER_3D_4D_10_16W_FULL_DARK_IMPLEMENTATION_COMPLETE
```
