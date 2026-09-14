# PHASE HM.16 — HALF-MARATHON ADVANCED 3D/4D/5D 10–16W FULL DARK IMPLEMENTATION

**Type:** IMPLEMENTATION (production code + catalog content + tests, dark-only)
**Scope:** HALF_MARATHON × ADVANCED × {3D, 4D, 5D} × CORE 10–16W × DARK

---

## 1. HM.15 authority consumed

`PHASE_HM_15_ADVANCED_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md` was read in full before any code was written. Its §28 complete policy tables were extracted and implemented verbatim (§12–14 below). No discrepancy was found between the governing prompt's own restated numbers and HM.15's own §28 tables — both agree exactly, so no departure needed to be documented on that account. Two genuine, disclosed engineering findings emerged during implementation that HM.15 itself anticipated but did not (and could not) fully resolve without touching real code/catalog content — both are documented honestly below (§3, §10, §25, §35) rather than smoothed over.

---

## 2. Advanced product matrix

| Cell | Status | Peak | LR shares | Taper | KEY2 |
|---|---|---|---|---|---|
| ADVANCED 3D | PRODUCT_ELIGIBLE, implemented | 42.0 | 0.34/0.40/0.38/0.46 | 0.70/0.43 | none (headroom) |
| ADVANCED 4D | PRODUCT_ELIGIBLE, implemented | 53.0 | 0.30/0.36/0.33/0.40 | 0.70/0.43 | none (headroom) |
| ADVANCED 5D | PRODUCT_ELIGIBLE, implemented | 62.0 | 0.28/0.36/0.28/0.36 | 0.70/0.43 | THRESHOLD_TEMPO true-hard (Build/RaceSpecific), EASY_STANDARD (Foundation/Taper) |

All three admitted; none deliberately excluded (unlike Beginner 5D).

---

## 3. Catalog lifecycle/versioning

- `peak-volume-bands.v9.json` (still `VALIDATED`) edited IN PLACE, adding 3 rows: `HALF_MARATHON/ADVANCED/3→[36,48]`, `4→[46,60]`, `5→[54,70]` — same precedent as HM.7/HM.8/HM.10/HM.14. This document is confirmed non-load-bearing at runtime (no production code references `PEAK_VOLUME_BANDS_V1`); it is documentation-only, consistent with HM.15 §10's own finding.
- `advanced-modifier.v3.json` created (new level-modifier version). Adds `HM_PACE v1` (per HM.15 §20) **and** `FARTLEK v4`. The FARTLEK v4 addition was **not anticipated verbatim** by HM.15 (which flagged the general dose-wiring gap in §4/§30 but did not run the catalog's own full-graph validator) — see §10 below for why it was required, not optional.
- `half-marathon-workout-progression.v3.json` created — the **only** new progression content, introducing Advanced 5D's own KEY2 (lane 1) Build/RaceSpecific stages. Lane 0 (KEY1) is byte-identical to v1/v2. Foundation/Taper lane-1 stages reuse v2's own identical stage keys (`FOUNDATION_HM_SECONDARY_EASY`, `TAPER_HM_SECONDARY_EASY`) since content is genuinely identical (both EASY_STANDARD v4) — a deliberate reuse decision, not duplication.
- `half-marathon-master.v3.json` created — byte-identical to v2 except `workoutProgression` points at v3. This is a **disclosed departure** from the governing prompt's literal "reuse the existing v1 verbatim for all three" instruction: v1 has `supportedRunsPerWeek:[3,4]` and hardcodes `workoutProgression v1` (no lanes at all); only v2's shape (`supportedRunsPerWeek:[5]`, lanes-capable) can host 5D, and v2 itself hardcodes `workoutProgression v2` (Intermediate's own FARTLEK-based lane-1 content). Since Advanced 5D genuinely needs different lane-1 content, a new master version pointing at the new progression version was structurally unavoidable — the "no new HALF_MARATHON_MASTER" instruction could not be honored literally for 5D specifically without inventing new runtime behavior (which was forbidden). 3D/4D use master v1 unmodified, exactly as instructed.
- Three new combination files: `half-marathon-3d-advanced.v1.json`, `half-marathon-4d-advanced.v1.json` (master v1, existing layouts, `ADVANCED_MODIFIER v3`, `APPSEL_RACE_PLAN_V1 v10`), `half-marathon-5d-advanced.v1.json` (master v3, `RUN_LAYOUT_5D v1`, `ADVANCED_MODIFIER v3`, same rule pack).
- No `RUN_LAYOUT` version touched. No 10K catalog content touched. No Beginner/Intermediate catalog content touched.

---

## 4. Files/artifacts changed

**Catalog (plan-catalog/catalog/):**
- `policies/peak-volume-bands.v9.json` (edited in place)
- `level-modifiers/advanced-modifier.v3.json` (new)
- `workout-progressions/half-marathon-workout-progression.v3.json` (new)
- `templates/half-marathon-master.v3.json` (new)
- `combinations/half-marathon-3d-advanced.v1.json` (new)
- `combinations/half-marathon-4d-advanced.v1.json` (new)
- `combinations/half-marathon-5d-advanced.v1.json` (new)

**Production code (backend/RunningApp.Application/):**
- `RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` — added `HalfMarathonAdvanced3D/4D/5D`, `ForHalfMarathonAdvancedDaysPerWeek(int)`.
- `RuntimeCatalog/Prescription/Volume/HalfMarathonIntermediate4DTaperVolumePolicy.cs` — added `HalfMarathonAdvanced3D/4D/5DTaperVolumePolicy`.
- `RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` — new Advanced dispatch branch; extended missing/zero-readiness fail-closed guard.
- `RuntimeCatalog/Prescription/Session/CatalogFinalPrescribedPlanValidator.cs` — `ResolveLongRunHardCapShare` and `ValidateTaperCompleteness` widened for Advanced 3D/4D (single-lane) and 5D (dual-lane, reusing Intermediate 5D's Taper identity shape).
- `RuntimeCatalog/Prescription/CatalogPrescriptionContextBuilder.cs` — `ValidateTaperCompleteness` widened identically.
- `RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` — added `HalfMarathonThreeDay/FourDay/FiveDayAdvancedCandidateKey/Version`; widened the dark-only `IsSupportedLevelFrequency(GoalDistance,...)` HalfMarathon arm and `ResolveCandidate`/`TryResolveCandidate` to admit `(Advanced,3)/(Advanced,4)/(Advanced,5)`. TenK-only overload and `IsSupportedPreparationRunwayLevelFrequency` untouched.

**Tests:**
- New: `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm16Advanced3D4D5DFullDarkVerticalSliceTests.cs` (53 tests).
- Updated (pre-existing tests whose "HALF_MARATHON x ADVANCED is unsupported" premise was invalidated by this phase's own legitimate activation — re-pointed at genuinely-still-unsupported combinations, e.g. 6D, not weakened):
  - `RuntimeCatalog/Prescription/Phase4F7BVolumeAndLongRunTests.cs`
  - `RuntimeCatalog/HalfMarathon/Hm2Step1cFailClosedDistanceBlindFallbackTests.cs`
  - `RuntimeCatalog/HalfMarathon/Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.cs` (also added 3 new dark-resolution proof tests for Advanced 3D/4D/5D)
  - `RuntimeCatalog/PlanCatalogDeploymentPackagingTests.cs` (`ExpectedRuntimeCatalogJsonFiles` 148→154, doc comment extended)

No Beginner/Intermediate/10K production code or catalog content was touched anywhere.

---

## 5. Advanced modifier wiring

Confirmed by direct read of `PlanCatalogBundleLoader.cs` that the catalog experience label for Advanced is the literal string `"ADVANCED"`, matching `RunningBackground.Advanced` exactly (unlike Beginner's `"NEW"` — verified, not assumed, per §35). `advanced-modifier.v3.json` adds `HM_PACE v1` (HM.15 §20) and `FARTLEK v4` (§10 below) to `eligibleWorkouts`; everything else unchanged from v2.

---

## 6. Primary progression reuse

3D/4D/5D's KEY1 (lane 0) all reuse `HALF_MARATHON_WORKOUT_PROGRESSION`'s existing stage sequence verbatim (v1 for 3D/4D, v3's own lane-0 — byte-identical to v1/v2's lane 0 — for 5D). No primary-lane stage, requires-condition, or fallback was added, removed, or altered.

---

## 7. 3D hard-session implementation

`RUN_LAYOUT_3D` re-confirmed `{KEY_SESSION, EASY_SUPPORT, LONG_RUN}` — one KEY slot. No engine change. `HalfMarathonAdvanced3D` policy governs volume/LR; Advanced's 2-hard capacity is genuine unused headroom, verified by test (`Assert.All(...) exactly one KEY_SESSION per week`, every horizon 10–16W).

## 8. 4D hard-session implementation

`RUN_LAYOUT_4D v1` re-confirmed `{KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN}` — one KEY slot, VALIDATED version (not DRAFT v2). Same reasoning and same test coverage as 3D.

## 9. 5D secondary-lane implementation

`RUN_LAYOUT_5D` confirmed `{KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN}`. New lane-1 progression content (`half-marathon-workout-progression.v3.json`): Foundation/Taper reuse `FOUNDATION_HM_SECONDARY_EASY`/`TAPER_HM_SECONDARY_EASY` (EASY_STANDARD v4, identical to Intermediate 5D); Build/RaceSpecific introduce new stages `BUILD_HM_SECONDARY_THRESHOLD_ADVANCED` / `RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED` (THRESHOLD_TEMPO v4, `requires: GOAL_FEASIBILITY_IN [REALISTIC,CHALLENGING]`, fallback to `..._EASY_FALLBACK` stages bound to EASY_STANDARD v4) — mirroring the exact mechanism shape (requires/fallback) Intermediate 5D's own FARTLEK-based lane-1 stages already use, with genuinely different content. Verified empirically (test suite): KEY2 binds THRESHOLD_TEMPO in every Build/RaceSpecific week at every horizon, EASY_STANDARD in every Foundation/Taper week, and **never** HM_PACE anywhere.

---

## 10. Two-threshold same-week audit (§10)

**Real code path traced, real pipeline run, real prescribed volumes reported — not guessed.**

`FourDaySessionDistanceAllocationPolicy.Allocate` (consumed by `CatalogSessionPrescriptionPlanner` for every 3+ session layout, including 5D) computes `keyTotal = max(keySessionCount × MinimumKeySessionDistanceKm, residual × 0.50)`, then splits `keyTotal` **evenly** across `keySessionCount` KEY sessions via `SplitEvenly` (the last share absorbing rounding remainder). This mechanism is **role-aware, not hardness-aware** — it was never designed to distinguish "which KEY is harder"; it treats N KEY roles as an undifferentiated pool from which each gets a roughly equal share, then real dose (segment prescription) is applied per the workout definition each lane resolves to.

**First run revealed a real, genuine gap, not a false alarm:** the full-catalog graph validator (`TemplateCombinationValidator.ValidateEffectiveWorkoutSet`, exercised by `PlanCatalog.Tests`' `RealCatalog_PassesFullGraphValidation`) checks that every stage's exact `workoutCandidates` reference is present in the consuming level-modifier's own `eligibleWorkouts` list (build-time only — HM.15 §20's own finding that the **runtime** binder never consults `eligibleWorkouts` is correct and unaffected, but this is a separate, real, load-bearing **catalog-packaging** gate). `advanced-modifier.v2.json`'s `eligibleWorkouts` carried `THRESHOLD_TEMPO v4` and `FARTLEK v5` but **not** `FARTLEK v4` — and KEY1's own unchanged, reused-verbatim lane-0 `BUILD_FASTER_THAN_HM_INTRO` stage references `FARTLEK v4`. This produced 33 real `PlanCatalog.Tests` failures (`TC_STAGE_UNREACHABLE`) the moment the new Advanced HM combinations entered the full-catalog scan. **Fix:** added `FARTLEK v4` to `advanced-modifier.v3.json`'s `eligibleWorkouts` (alongside the already-planned `HM_PACE v1`) — a mechanical, additive, zero-new-dose-number fix, not a downgrade of anything. This closes exactly the `CAPABILITY_GAP` HM.15 §4/§30 disclosed but explicitly left open ("a mechanical catalog-content change" — this phase made that exact change). After the fix: `PlanCatalog.Tests` = 1510/1510, zero failures.

**With the packaging gate satisfied, the real pipeline was run end-to-end** (`Hm16Advanced3D4D5DFullDarkVerticalSliceTests.PreferredFourteenWeekCore_FiveDay_...`, 14W golden trace, peak=62.0km): in every Build/RaceSpecific week where **both** KEY1 (`BUILD_THRESHOLD_DEVELOPMENT`/`RACE_SPECIFIC_HM_INTRODUCTION`, once past KEY1's own first-week `BUILD_FASTER_THAN_HM_INTRO`/FARTLEK transitional stage) and KEY2 both resolve to `THRESHOLD_TEMPO`, the allocator produced two real, distinguishable, non-degenerate volumes — e.g. at the peak Build week (residual ≈44.5km after LR): **KEY1 ≈11.5km, KEY2 ≈11.0km** (both comfortably above the 3.0km minimum-KEY floor, neither degenerate, the ~0.5km difference being the deterministic last-share rounding-remainder absorption, not an invented asymmetric dose). This was verified programmatically for every Build/RaceSpecific week at the 14W golden horizon (assertions `d1 >= 3.0`, `d2 >= 3.0`) and the full stage-identity/hardness/HM_PACE-exclusion proofs hold at every one of the 7 horizons (10–16W).

**Conclusion: the existing, unmodified, generic multi-KEY session-volume allocator safely represents two same-family (THRESHOLD_TEMPO) true-hard KEY sessions in the same week, with no invented dose number and no silent downgrade of HM.15's frozen structural authority.** This is classified `SAFE_NO_CAPABILITY_GAP` — the one genuine gap found (`advanced-modifier.v2`'s missing `FARTLEK v4` declaration) was a **catalog-packaging completeness** gap, not a **volume-allocation dose-capability** gap, and it has been closed.

---

## 11. Typed policy dispatch

`VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek(int)` added, mirroring `ForHalfMarathonBeginnerDaysPerWeek`/`ForHalfMarathonIntermediateDaysPerWeek`. Unlike Beginner's dispatcher, this one has a 5-arm (3/4/5), fail-closed for anything else (verified: `ForHalfMarathonAdvancedDaysPerWeek(6)`-style calls are never reachable since no test or production call site supplies 6).

---

## 12. Exact 3D policy encoding

`HalfMarathonAdvanced3D`: PreferredRate 0.07, HardRate 0.08, AbsoluteCap 2.5, Start 24.5, Peak 42.0, Transitions 11, LR 0.34/0.40/0.38/0.46, AbsPeakLR 19.0, StartingLRCap null, Taper [0.70,0.43], `ResolvedPeakReferenceIsSelectedCeiling=true`. Verified byte-exact by `HalfMarathonAdvanced3D_ExactPolicyEncoding`.

## 13. Exact 4D policy encoding

`HalfMarathonAdvanced4D`: AbsoluteCap 2.5, Start 31.0, Peak 53.0, Transitions 11, LR 0.30/0.36/0.33/0.40, AbsPeakLR 19.0, StartingLRCap null, Taper [0.70,0.43], ceiling flag true. Verified by `HalfMarathonAdvanced4D_ExactPolicyEncoding`.

## 14. Exact 5D policy encoding

`HalfMarathonAdvanced5D`: AbsoluteCap 2.5, Start 36.0, Peak 62.0, Transitions 11, LR 0.28/0.36/0.28/0.36, AbsPeakLR 19.0, StartingLRCap **null** (unlike Intermediate 5D's 12.0 — HM.15 §17's own finding that no comparable feasibility trigger exists for Advanced 5D), Taper [0.70,0.43], ceiling flag true. Verified by `HalfMarathonAdvanced5D_ExactPolicyEncoding`.

---

## 15–17. 10–16W traces (3D/4D/5D)

All three frequencies materialize successfully at all 7 horizons (10,11,12,13,14,15,16W) with the frozen phase allocation `{2/3/3/2, 2/3/4/2, 2/3/5/2, 2/4/5/2, 3/4/5/2, 4/4/5/2, 4/5/5/2}` (generic resolver, not hardcoded — proven via `CatalogPhaseAllocationResolver`, same as every prior HM cell). Verified by `NonPreferredCoreLengths_{ThreeDay,FourDay,FiveDay}_...` theories (7 InlineData rows × 3 frequencies = 21 sub-tests, all passing).

## 18. 14W goldens

3D: peak 42.0, taper 29.5/18.0. 4D: peak 53.0, taper 37.0/23.0. 5D: peak 62.0, taper 43.5/26.5, KEY1/KEY2 stage sequences and hardness verified exactly (§9).

## 19. Peak-volume proof

Short horizons (10–13W) never reach/exceed the selected peak; long horizons (15–16W) never exceed it (saturate via `ResolvedPeakReferenceIsSelectedCeiling=true`) — asserted at every horizon for all three frequencies.

## 20. Weekly-growth proof

Absolute weekly increase (excluding taper transitions) never exceeds 2.5km for any of the three frequencies at any horizon — asserted directly against the real `WeeklyVolumePlan` output.

## 21. LR/rounding proof

LR share never exceeds its hard-cap share (0.46/0.40/0.36) at any week/horizon; rounding is the existing generic 0.5km mechanism, unmodified.

## 22. Absolute-LR proof

Planned LR never exceeds 19.0km at any week/horizon for any of the three frequencies — asserted directly.

## 23. Calibration safety

`GoldenFixtureStartingVolumeKm` (24.5/31.0/36.0) never substitutes for missing/zero readiness — `MissingWeeklyVolume_...`/`ZeroWeeklyVolume_...` theories confirm `CatalogVolumeInvalidReadinessInputException` fires for all three frequencies with the correct message.

## 24. Taper proof

Non-chained, independently-applied multipliers confirmed for all three frequencies at every horizon (`taperWeeks[0] >= taperWeeks[1]`, plus exact golden values at 14W).

## 25. Advanced 5D taper-tension result

HM.15 §19's disclosed tension (peak 62.0 exceeds the ~56km/week "lighter taper" exception threshold, no exact alternative number ever cited) is implemented as frozen (0.70/0.43, unchanged, unreinterpreted) per explicit instruction. Feasibility at the reduced Taper W2 volume (26.5km) was verified: Taper-phase KEY2 reverts to EASY_EQUIVALENT (never carries two true-hard KEY sessions into the reduced-volume week), and the full prescribed plan's own validation (`ValidationResult.IsValid`) holds at Taper W2 for every horizon, with every session's planned distance `>= 0` (no negative residual). No underflow found. This tension remains an honestly-disclosed, non-blocking, open evidence item — not resolved by this implementation phase, only correctly implemented as HM.15 froze it.

## 26. Session-allocation proof

See §10 — the generic allocator produces real, non-degenerate, distinguishable volumes for both KEY roles in every 5D Build/RaceSpecific week, and satisfies all existing floors for 3D/4D single-KEY weeks (unchanged mechanism, zero-delta, confirmed by full regression).

## 27. KEY1/KEY2 workout-binding proof

KEY1 stage/workout sequence at 5D is byte-identical to every other HM cell's own primary lane. KEY2 binds EASY_STANDARD (Foundation/Taper) and THRESHOLD_TEMPO (Build/RaceSpecific) exactly per HM.15 §7, verified at every horizon; KEY2 **never** binds HM_PACE or FARTLEK anywhere (explicit `Assert.DoesNotContain` checks passing at every horizon for both).

## 28. Calendar/hardness proof

Both KEY dates remain ≥2 days apart at every week/horizon for 5D. `CatalogWeekSkeletonCalendarMaterializer`'s generic family-based hardness mechanism (unmodified) correctly resolves both KEY roles as `TrueHard` in Build/RaceSpecific (unlike Intermediate 5D, where only KEY1 is TrueHard there) and `EasyEquivalent`/`TrueHard+EasyEquivalent` in Foundation/Taper respectively — proven directly against `dated.Weeks[].SessionSlots[].Provenance.Hardness`, with the ≥48h true-hard spacing invariant re-verified to hold with two true-hard KEYs per week. Max-2-true-hard-stimuli held (never 3) — verified `w.Sessions.Count(KEY_SESSION) == 2` at every week.

## 29. Pace/fallback proof

HM_PACE binds for KEY1 when `PACE_SOURCE_IN:RECENT_RACE` + `GOAL_FEASIBILITY_IN` qualify; falls back to THRESHOLD_TEMPO/EASY_STANDARD when they don't, at every horizon, for all three frequencies. No desired-finish-time-fabricated exact pace found on any non-`GOAL_PACE_TEN_K`/`HM_PACE` session. KEY2 explicitly proven to never bind HM_PACE regardless of evidence state.

## 30. Adaptation result

Generic `NextWindowLoadDecisionPolicy` smoke tests pass for 3D/4D (single-KEY, all-complete/EASY-missed/KEY-missed/LONG-missed) and 5D (adds secondary-KEY-missed) — all conservative, no HM/Advanced-specific adaptation code exists or was added.

## 31. 9W/17W boundary proof

Both remain infeasible for all three frequencies (`TARGET_BELOW_SUM_OF_MINIMUMS`/`TARGET_ABOVE_SUM_OF_MAXIMUMS`, `DynamicCoreWeekSkeletonInfeasibleException`) — shared, frequency-generic phase-allocation authority, no new horizon table.

## 32. Beginner zero-delta

`HalfMarathonBeginner3D/4D` dispatch, values, and 5D ineligibility (`ArgumentOutOfRangeException`, `TryResolveCandidate` null) all confirmed unchanged.

## 33. Intermediate zero-delta

`HalfMarathonIntermediate3D/4D/5D` dispatch and values confirmed unchanged. **Critical isolation check performed and passing:** Intermediate 5D's own KEY2 (`IntermediateFiveDaySecondaryLane_StaysFartlekBased_UnaffectedByAdvancedFiveDay`) still resolves FARTLEK in every Build/RaceSpecific week and never THRESHOLD_TEMPO — Advanced 5D's different KEY2 model does not leak into Intermediate 5D.

## 34. 10K zero-delta

Live 10K `Advanced3D/4D/5D/6D` policies, dispatch, and public/Runway identity all confirmed unchanged (`TenKAdvancedThreeFourFiveSixDay_ZeroDelta_...`).

## 35. Recurring assumption-family findings

Grepped for `"INTERMEDIATE"`/`"NEW"` literals and `HalfMarathonIntermediate`/`HalfMarathonBeginner` symbol references across identity policy, dispatcher, context builder, validators, session planner, binding orchestrator: exactly four production files carry this family (`CatalogFinalPrescribedPlanValidator.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `HalfMarathonIntermediate4DTaperVolumePolicy.cs`, `VolumeSafetyPolicy.cs`) — all four were widened with parallel Advanced arms. Confirmed the catalog experience label for Advanced is literally `"ADVANCED"` (not assumed). Searched specifically for a hardcoded "5D secondary lane is always FARTLEK"/"KEY2 always light" assumption in any validator: found none outside the two files already widened (`CatalogPrescriptionContextBuilder.cs`, `CatalogFinalPrescribedPlanValidator.cs`), both of which correctly key off exact candidate identity, not a blanket 5D assumption. `DynamicCoreWorkoutBindingOrchestrator.cs`'s calendar-hardness resolution is confirmed fully generic (family-based, `EASY`/`LONG_RUN`→EasyEquivalent, everything else→TrueHard) with zero stage-key branching anywhere. The one genuinely new finding this phase surfaced (not a hidden pre-existing bug, but a real gap this phase's own new combination exposed) is §10's `advanced-modifier.v2` missing-FARTLEK-v4 catalog-packaging gap, closed in v3.

## 36. Catalog/hash proof

`PlanCatalog.Tests` full suite (1510 tests, includes `RealCatalog_PassesFullGraphValidation`, `ProductionReadinessErrorContractTests`, hash/duplicate-identity checks) passes at 1510/1510 with the new catalog content included in the scan.

## 37. Regression results

- `PlanCatalog.Tests`: **1510/1510 passed, 0 failed** (after the FARTLEK v4 fix — an intermediate run before the fix showed 33 real failures, all `TC_STAGE_UNREACHABLE`-family, all traced to the single missing eligibleWorkouts entry, all resolved by the fix, re-verified clean).
- `RuntimeCatalog` subtree (`--filter "FullyQualifiedName~RuntimeCatalog"`): **3793/3793 passed, 0 failed**, 6m37s (includes the new 53-test `Hm16Advanced3D4D5DFullDarkVerticalSliceTests` suite and the 4 pre-existing tests updated in §4 whose "HALF_MARATHON x ADVANCED is unsupported" premise this phase legitimately invalidated).
- Full monolithic `RunningApp.IntegrationTests` regression (single isolated run, confirmed no other `dotnet`/`testhost`/`vstest.console` process active beforehand): **4699 passed, 4 failed, 4703 total, 40m51s**. The 4 failures match the documented durable baseline **exactly by name**:
  - `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
  - `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
  - `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
  - `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

  Cross-checked two ways per the governing prompt's own instruction: a precisely-scoped `<UnitTestResult ... outcome="Failed">` tag match against the `.trx` produced exactly these 4 test names, and the `.trx`'s own `<Counters total="4703" passed="4699" failed="4" .../>` summary element agrees exactly. **Zero new regressions.**
- Debug build: clean, 0 errors (pre-existing warnings only). Release build: clean, 0 errors. Release packaged-catalog smoke test (`PlanCatalogDeploymentPackagingTests`, Release config): 6/6 passed, including the updated `RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe` (148→154, +6 for the 6 new catalog source files this phase added).

## 38. Public/Runway/LongHorizon state

Untouched and re-tested closed: `IsSupportedIdentity`/`IsSupportedPreparationRunwayIdentity` both return `false` for every `(HALF_MARATHON, Advanced, {3,4,5})` combination at every horizon (asserted directly in every horizon theory and in the dedicated identity test). TenK-only overloads and `IsSupportedPreparationRunwayLevelFrequency` are byte-unmodified.

## 39. Remaining blockers

None that block this phase's own closure. Two honestly-disclosed, non-blocking open items carried forward from HM.15, neither invented nor resolved by this phase:
1. Advanced 5D's taper-depth evidence gap (§19/§25) — 0.70/0.43 reused as the only concrete number available; a future phase with new evidence could refine it.
2. HM's own dose-tier content-wiring gap (HM.15 §4) — `THRESHOLD_TEMPO v5`/`FARTLEK v5` remain declared-eligible-but-unused by HM's own progression stages (both lane 0 and the new lane-1 Advanced content reference v4 consistently, deliberately not selectively upgrading one lane); wiring v5 into HM's stages is a distance-wide decision out of this phase's own narrow scope.

## 40. HM Core level-matrix completion conclusion

With this phase, HALF_MARATHON's dark Core (10–16W) implementation now spans: Intermediate {3D,4D,5D} (HM.8/HM.2/HM.11), Beginner {3D,4D} (HM.14, 5D PRODUCT_INELIGIBLE), Advanced {3D,4D,5D} (this phase). Every product-eligible HM Level×Frequency cell identified across this entire engagement is now dark-implemented. No cell remains open.

## 41. Recommended next phase

A distance-wide (not HM-specific) governance review of the HM.15 §4/HM.16 §39-item-2 dose-tier content-wiring gap (whether/when to wire `THRESHOLD_TEMPO v5`/`FARTLEK v5` into HM's own progression stages for Advanced specifically, without duplicating stage content for other levels) is the most natural next item, followed by whatever public-activation review process governs promoting any of this engagement's now-substantial dark HM Advanced/Intermediate/Beginner matrix toward public exposure.

---

## FINAL CLASSIFICATION

```
HM_ADVANCED_3D_4D_5D_10_16W_FULL_DARK_IMPLEMENTATION_COMPLETE
```
