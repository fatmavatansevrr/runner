# PHASE DIST-GEN.2 — 16K × INTERMEDIATE × 4D FULL DARK TARGET-DISTANCE PROJECTION IMPLEMENTATION

**Type:** DARK-ONLY IMPLEMENTATION (real production code; nothing publicly reachable; no public activation)

---

## 1. DIST-GEN.1 authority consumed

Every numeric value in DIST-GEN.1 §47's complete policy table was implemented exactly, with no re-derivation:

| Field | Value | Where implemented |
|---|---|---|
| MinimumCoreWeeks / PreferredCoreWeeks / MaximumCoreWeeks | 8 / 12 / 14 | `TargetDistance16KHorizonPolicy` |
| PeakVolumeBandMin / Max | 34 / 46 | `TargetDistance16KPeakVolumeBandPolicy` |
| SelectedPeakReference | 40.0 | `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K.ResolvedPeakReference` |
| PreferredWeeklyIncreaseRate / HardWeeklyIncreaseRate | 0.07 / 0.08 | same policy, reused byte-identical to every other named policy |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | same policy |
| CalibrationStartingVolumeKm | 23.5 | `GoldenFixtureStartingVolumeKm` field |
| LR PreferredShareMin/Max/Selected/HardCap | 0.30 / 0.36 / 0.33 / 0.40 | same policy, byte-identical to HM's own 4D quadruple |
| PreferredAbsolutePeakLongRunKm | 15.0 | same policy |
| StartingAbsoluteLongRunCapKm | N/A (not set) | left unset, mirrors HM 4D |
| TaperDurationWeeks / multipliers | 2 / 0.70 / 0.43, non-chained | `TargetDistance16KTaperVolumePolicy` |
| RunLayout | RUN_LAYOUT_4D (1 KEY/2 EASY/1 LONG) | reused unmodified — no new layout file |
| Workout progression | HM Intermediate 4D progression, unmodified | reused unmodified — no new progression file |
| Recent-race Riegel equivalence | unmodified, exponent 1.06 | reused unmodified (`GoalFeasibilityResolver.ResolveFromRecentRace`) |
| Goal-feasibility thresholds | unmodified (0.03/0.06) | reused unmodified |
| GoldenFixtureNonTaperTransitions | 9 (12W − 2W taper → 10 non-taper weeks → 9 transitions) | encoded directly in the new policy instance |

No field was left blank; no new number was invented beyond DIST-GEN.1's own frozen table.

## 2. DIST-GEN.0.1 safety contract preserved

**Before:** `GoalDistance.Custom` → HTTP 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`, proven by `GoalDistanceCustomFailClosedTests` (13 tests).

**After:** Zero lines of `GenerateRacePlanPreviewRequestValidator.cs`, `GenerateHabitPlanPreviewRequestValidator.cs`, or `GeneratePreviewRequestValidator.cs` were touched (confirmed via `git status`). The full monolithic regression run (§40) re-executed every one of those 13 tests unchanged and they all still pass (they are inside the same `RunningApp.IntegrationTests.dll` run that produced 4854/4858 passing, with the same 4 pre-existing unrelated failures — none in `GoalDistanceCustomFailClosedTests`). `GoalDistance` enum itself has zero new members (`git diff` on `GoalDistance.cs` is empty). The one new field added anywhere in the request surface, `GeneratePreviewRequest.TargetDistanceKmOverride`, lives on the **internal-only** `GeneratePreviewRequest` type (already documented in-repo as "no controller action binds this type from JSON anymore") and `GeneratePreviewCommandMapper.ToInternalRequest` — the only code that ever constructs this type from real HTTP input — never sets it, so it is `null` for every request that ever originates from `GenerateRacePlanPreviewRequest`/`GenerateHabitPlanPreviewRequest`. A "custom" typed distance still has no way to reach this field through any public surface whatsoever.

## 3. Files changed

**New files (10):**
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/TargetDistance16KHorizonPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KTaperVolumePolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KPeakVolumeBandPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KAwarePeakVolumeBandLoader.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/Dark16KTargetDistanceProjectionTests.cs` (40 tests)
- This report.

**Modified files (8, all additive/gated):**
- `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs` — added `TargetDistanceKmOverride` (nullable, never set by any public mapper).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` — added `HalfMarathonIntermediate4DTargetDistance16K` static instance (58 lines, purely additive).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` — added one new dispatch branch, gated by `Dark16KPilotEligibilityPolicy`, checked before the generic HM-Intermediate branch.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/CatalogPrescriptionContextBuilder.cs` — `CatalogGoalDistanceResolver.Resolve` widened with two optional params (`level`, `daysPerWeek`) used only to skip its mismatch-guard for the one eligible pilot case; `RequestedTargetDistanceKm` now populated on the built context.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/PrescriptionContracts.cs` — added optional `RequestedTargetDistanceKm` field to `CatalogPrescriptionInputSnapshot`.
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs` — `BuildInputSnapshot` now resolves `RequestedTargetDistanceKm` through the gated override; `BuildDarkInternalDatedSkeleton` substitutes the pilot's own horizon bounds and peak-volume-band loader when eligible, and always routes the eligible case through the dynamic-core path (see §9/§26).
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs` — one-line fix: `RequestedTargetDistanceKm` now sourced from `snapshot.NormalizedInput.RequestedTargetDistanceKm` (falling back to `GoalDistanceKm`) instead of unconditionally from `GoalDistanceKm` (see §30).
- `backend/RunningApp.Application/Services/PlanServices.cs` — the public horizon gate now consults the pilot's own explicit horizon authority when eligible, before falling through to the pre-existing `RaceHorizonPolicy.DecideForDistance` call (unchanged for every other request).

**Untouched (confirmed via `git status`):** every catalog JSON file, `GoalDistance.cs`, all three public request validators, `V1CatalogPilotIdentityPolicy`'s public gate, all database migrations, `mobile/**`.

## 4. Production-owned dark seam

The exact, sole entry point: `GeneratePreviewRequest.TargetDistanceKmOverride` (nullable `double`) on the internal-only request type that both `PlanServices.GeneratePreviewAsync` and `CatalogPreviewGenerator` consume. No public wire DTO carries this field. It is populated only by test/internal-harness code constructing `GeneratePreviewRequest` directly — mirroring the exact precedent `V1CatalogPilotIdentityPolicy`'s internal 3-arg `ResolveCandidate(GoalDistance, RunningBackground, int)` overload established for HM's own dark-only 5D/6D candidates (PHASE_HM_X1_3). Every consumer of this field is gated through the single `Dark16KPilotEligibilityPolicy.IsEligible(...)` check (§6), so the override has zero effect unless the exact frozen triple is also present.

## 5. Distance resolution — CanonicalDistanceFamilyResolver wiring proof

`CanonicalDistanceFamilyResolver` itself was **not modified** (confirmed via `git status` — zero diff). It was already correct: `Resolve(16.0)` returns `CanonicalDistanceFamily = HalfMarathon`, `RequestedTargetDistanceKm = 16.0` (preserved exactly, unrounded). Proven directly by `CanonicalDistanceFamilyResolver_ClassifiesButNeverGrantsEligibilityAlone` for 16.0/12.0/15.0/18.0/20.0 (all → HalfMarathon, exact value preserved) and 9.5 (→ TenK). The resolver remains otherwise dark/unwired into any production call path other than this dark seam's own use of the value it would produce (the seam does not literally call the resolver at runtime — the override value is supplied directly and validated by the narrower `Dark16KPilotEligibilityPolicy` gate, which is the correct division of labor per §6 below).

## 6. 16K pilot eligibility — the narrow gate

`Dark16KPilotEligibilityPolicy.IsEligible(...)` (two overloads: request-shaped and candidate-shaped) is the **single** place the exact triple `(TargetDistanceKm=16.0, Level=Intermediate, RunsPerWeek=4, ParentDistanceFamily=HalfMarathon)` is checked. Every consumer (`CatalogPreviewGenerator`, `CatalogVolumeAndLongRunPlanner`, `CatalogGoalDistanceResolver`, `PlanServices`, `TargetDistance16KAwarePeakVolumeBandLoader`) calls this one class — no scattered `if distance == 16.0` checks exist anywhere else. Proven ineligible, explicitly, by `EligibilityGate_OnlyExactFrozenTripleIsEligible`: 12.0, 15.0, 18.0, 20.0 km (same level/frequency) are all `false`; wrong level (Beginner/Advanced) and wrong frequency (3/5/6) are `false`; `GoalDistance.TenK` with 16.0 is `false`; a `null` override is `false`. `CanonicalDistanceFamilyResolver` continues to **classify** 12K/15K/18K/20K into `HALF_MARATHON` (a resolver concern) — `CanonicalDistanceFamilyResolver_ClassifiesButNeverGrantsEligibilityAlone` proves classification and eligibility are two independent questions with two independent answers for every one of those distances.

## 7. Catalog identity reuse

Zero catalog file changes. `git status --porcelain -- plan-catalog` is clean after this phase (one transient timestamp regeneration in `ten-k-pilot-domain-decision-audit.json`/`.md`, produced automatically by running `PlanCatalog.Tests` itself, was reverted via `git checkout` before finalizing). The dark 16K pilot resolves to the exact same `HALF_MARATHON__4D__INTERMEDIATE` v1 candidate every canonical HM 4D Intermediate request uses — same master template, same layout, same workout progression, same rule pack.

## 8. Projection-policy ownership

Distributed, not monolithic, per DIST-GEN.1 §44's own recommendation:
- **Eligibility:** `Dark16KPilotEligibilityPolicy` (§6).
- **Horizon:** `TargetDistance16KHorizonPolicy` (§9).
- **Peak volume / growth / taper / long-run share:** `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K` (a new named instance of the existing typed record — no new record shape).
- **Peak-volume band (catalog-external companion to VolumeSafetyPolicy):** `TargetDistance16KPeakVolumeBandPolicy` + `TargetDistance16KAwarePeakVolumeBandLoader` (a decorator over the real `ICatalogPeakVolumeBandLoader`).
- **Target-race-pace / Riegel / goal-feasibility:** no new component — reused entirely unchanged (§12/§13).

Each is consulted at exactly one seam: `CatalogVolumeAndLongRunPlanner.Build`'s dispatcher (volume/taper/LR), `PlanServices.GeneratePreviewAsync` and `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton` (horizon), `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton`/`CatalogGoalDistanceResolver` (peak band / requested-target-distance), `CatalogPlanConfirmationService.BuildCatalogTrainingPlan` (persistence). The generic algorithm itself (`CatalogVolumeAndLongRunPlanner`'s actual arithmetic, `DynamicCoreWeekSkeletonOrchestrator`'s phase allocator, the session allocator, the calendar materializer) is **completely unmodified** — only policy *selection* was touched, exactly as instructed.

## 9. Horizon implementation

`TargetDistance16KHorizonPolicy` freezes `MinimumCoreWeeks=8`, `PreferredCoreWeeks=12`, `MaximumCoreWeeks=14` as named constants and calls `RaceHorizonPolicy`'s own generic, explicit-bounds 5-argument `Decide(startDate, raceDate, min, preferred, max)` overload directly — never the distance-blind parameterless `Decide(startDate, raceDate)` two-arg overload, and never `DecideForDistance`'s `_ => Decide(...)` fallback arm (the one DIST-GEN.0.1 flagged as the TEN_K-borrowing anti-pattern). Proof, via real production code, not mocks:
- `HorizonPolicy_16K_ClassifiesExactlyAtItsOwnFrozenBoundaries`: 7 weeks → `BelowMinimum`; 8/14 weeks → `StandaloneCoreSupported`; 12 weeks → `ExactStandaloneCoreSupported`; 15 weeks → `CompositionRequired`.
- `HorizonPolicy_16K_ReasonCode_IsDistinctFromTenKAndHalfMarathonOwnCodes`: the pilot's own reason code (`DARK_16K_CORE_HORIZON_7_NOT_SUPPORTED`) is textually distinct from TEN_K's own `RaceHorizonPolicy.GetCoreHorizonUnsupportedReasonCode` shape (`CORE_HORIZON_7_NOT_IMPLEMENTED`).
- `RealPipeline_BelowMinimumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds` (7W) and `RealPipeline_AboveMaximumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds` (15W): real `PlanServices.GeneratePreviewAsync` calls reject with `PlanCoreHorizonTooShortException`/`PlanHorizonCompositionRequiredException` carrying `DARK_16K_CORE_HORIZON_*` in the message, and explicitly assert the message does **not** contain HALF_MARATHON's own "(10" minimum-weeks text.
- `RealPipeline_ZeroDelta_CanonicalHalfMarathon_TooShortForItsOwnBounds_StillUsesHmOwnMinimum`: the identical 8-week horizon, with `TargetDistanceKmOverride=null` (canonical request), is instead rejected by HM's own real 10-week minimum message — proving the two authorities are genuinely distinct call paths, not merely differently-worded output of the same one.

A second, necessary horizon touchpoint was found and fixed during implementation: `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton`'s own internal `RaceHorizonPolicy.Decide(...)` call (used to decide dynamic-core vs. static-preferred-core routing) was **also** using the candidate's own catalog-declared core-cycle bounds (10/14/16) — the same wrong authority at a different layer. This is fixed the same way (bounds resolved to locals first, exactly one `RaceHorizonPolicy.Decide` call site retained — see `PreparationRunwayHorizonAuthorityTests.CatalogPreviewGeneratorSource_HasExactlyOneRaceHorizonPolicyDecideCallSite_OutsideRunwayMethod`, which now passes again after this fix).

## 10. Phase allocation

The real, unmodified `DynamicCoreWeekSkeletonOrchestrator`/`CatalogPhaseAllocationResolver` (the same generic allocator TEN_K's own 8-14W and HM's own 10-16W policies already use) is reused with zero new code. Real per-horizon splits, read from the passing `GoldenTrace_12W` test's own trace: **12W → FOUNDATION(2) / BUILD(3) / RACE_SPECIFIC(5) / TAPER(2)** — matching DIST-GEN.1 §38's own derivation (12 − 2 taper = 10 non-taper weeks, and 2+3+5=10). See §27 for the full week-by-week table.

## 11. Progression reuse

Confirmed zero new stage keys: no new workout-progression JSON artifact was authored or referenced; the dark 16K pilot binds against the exact same `half-marathon-workout-progression` artifact HM's canonical 4D Intermediate candidate already uses (candidate identity is unmodified — §7).

## 12. Target-race-pace implementation

Investigated directly in `CatalogSessionPrescriptionPlanner.PaceFor`: `HM_PACE`'s resolved numeric pace is computed exclusively from **independent recent-race evidence** (`recentRace.FinishTimeSeconds / recentRace.DistanceKm`) — it never references the target/goal distance (21.0975 or 16.0) at all. This is a genuine finding, not assumed: DIST-GEN.1 §14 anticipated "the resolved pace value varies by target distance," but the actual current mechanism does not consume target distance for this workout family at all — it is already fully target-distance-independent. **No code change was needed or made** for this item; it is reused completely unchanged, correctly, for the dark 16K pilot exactly as for canonical HM.

## 13. Riegel wiring

`GoalFeasibilityResolver.ResolveFromRecentRace` was **not modified**. It already reads `input.RequestedTargetDistanceKm ?? input.GoalDistanceKm` as its projection target — since `ResolverInputSnapshot.RequestedTargetDistanceKm` is now correctly set to 16.0 for the dark pilot (§5), this formula automatically projects a recent-race performance to 16K-equivalent fitness without any further change. Proven live: `RealPipeline_EligibleHorizons_MaterializeSuccessfully`/`GoldenTrace_12W_...` both supply a recent 10K race (27:00) and materialize successfully through the real feasibility-resolution step, confirming the Riegel projection runs against 16.0km, not 21.0975km, for this request.

## 14. Product-average handling

Not exercised in this phase's own tests (the dark harness uses `TargetFinishTimeSource.UserDefined` with independent recent-race evidence, per §12/§13's proof requirement, rather than `ProductAverage`). `CanonicalTargetFinishTimePolicy` was **not modified** — DIST-GEN.1 §18's own method (project HM's own canonical HalfMarathon seconds down to 16.0 via the same Riegel formula) remains implementation-ready but unwired; this is a genuinely bounded, disclosed gap (no code path in this phase computes a 16K product-average default), carried forward to DIST-GEN.3 per §42 below.

## 15. Goal-feasibility behavior

`GoalFeasibilityResolver`'s REALISTIC/CHALLENGING/UNSUPPORTED/NOT_REQUESTED thresholds (0.03/0.06) are completely unmodified and were exercised live by every `RealPipeline_*` test that reaches real generation (all of them supply recent-race evidence and a target time, so `GOAL_FEASIBILITY_IN` is evaluated, not skipped).

## 16. Peak-volume policy (exact encoded values)

`VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K`: `ResolvedPeakReference=40.0`, `PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`, `AbsoluteWeeklyIncrementCapKm=2.5`, `GoldenFixtureStartingVolumeKm=23.5`, `GoldenFixtureNonTaperTransitions=9`, `TaperVolumeMultipliers=[0.70,0.43]`, `LongRunPreferredMinimumShare=0.30`, `LongRunPreferredMaximumShare=0.36`, `LongRunSelectionShare=0.33`, `LongRunHardCapShare=0.40`, `PreferredAbsolutePeakLongRunKm=15.0`, `StartingAbsoluteLongRunCapKm=null`, `ResolvedPeakReferenceIsSelectedCeiling=true`. All asserted directly in `VolumeSafetyPolicy_16K_EncodesExactFrozenValues`, alongside an explicit zero-delta assertion that `VolumeSafetyPolicy.HalfMarathonIntermediate4D` (canonical) still reads 43.0/25.0/19.0.

Separately, `TargetDistance16KPeakVolumeBandPolicy` freezes the [34,46] band (DIST-GEN.1 §22) — this is NOT a `VolumeSafetyPolicy` field (that record only carries the resolved reference, matching every other named policy in the file); it is a companion policy substituted only for the eligible request via `TargetDistance16KAwarePeakVolumeBandLoader`, since the real catalog-loaded band for the reused `HALF_MARATHON`/`INTERMEDIATE`/`4` identity is HM's own real [36,50] band (a genuine, disclosed cross-authority tension found during implementation — see §43).

## 17. Weekly-growth guards

0.07 preferred / 0.08 hard / 2.5km absolute cap — all reused byte-identical, both anchors already agreed exactly at 4D (DIST-GEN.1 §24-25), so no new value was introduced. The 12W golden trace (§27) shows every non-taper week-to-week delta at exactly 1.5km (well under the 2.5km cap and the ~7% preferred rate for volumes in the 20-34km range).

## 18. Absolute weekly cap

2.5km, identical to `HalfMarathonIntermediate4D` and `Default` (TEN_K) at 4D — `DIRECT_EXISTING_AUTHORITY`, reused with zero new derivation.

## 19. Calibration safety

`GoldenFixtureStartingVolumeKm=23.5` is a reference/test-fixture constant, never consulted at runtime unless the generic, already-existing, already-fail-closed `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` guard is reached with missing/zero readiness — a mechanism this phase adds no new code to (it is the same guard every other named policy already relies on). This phase's own tests always supply real, positive readiness (`RecentWeeklyVolumeKm=20`, `RecentLongestRunKm=10`), so the calibration constant was never exercised as an actual starting point in any test — exactly as intended (missing/zero readiness must still fail closed, not silently substitute 23.5). No new test explicitly forces missing/zero readiness for the 16K pilot in this phase; this is a real, disclosed, bounded gap (the underlying guard is generic and mechanically applies once wired, per DIST-GEN.1 §34, but this phase did not add a dedicated regression test proving it for this exact policy instance).

## 20. Long-run policy

`LongRunPreferredMinimumShare=0.30`/`Maximum=0.36`/`Selection=0.33`/`HardCap=0.40` — byte-identical to `HalfMarathonIntermediate4D`, the strongest possible internal evidence for reuse (DIST-GEN.1 §27). Proven live in `GoldenTrace_12W`: every week's `LongRunShareOfWeeklyVolume` sits between 32.1% and 39.7%, all within the reused envelope.

## 21. Absolute peak LR

`PreferredAbsolutePeakLongRunKm=15.0` — encoded exactly. `GoldenTrace_12W_MaterializesWithFrozen16KNumericsNotHmNumerics` asserts every week's long run is `< 16.0` (the target distance) **and** `<= 15.0` (the ceiling); the real trace's own maximum (13.5km at W10) sits comfortably below both.

## 22. Starting LR cap

`StartingAbsoluteLongRunCapKm = null` on the new policy instance — matches HM 4D's own null, asserted directly in `VolumeSafetyPolicy_16K_EncodesExactFrozenValues`.

## 23. Taper implementation (non-chained proof)

`TargetDistance16KTaperVolumePolicy` freezes `TaperWeek1VolumeMultiplier=0.70`, `TaperWeek2VolumeMultiplier=0.43`, both applied independently against the fixed pre-taper anchor (never chained). Real proof, from the passing 12W golden trace: pre-taper peak reached was 34.0km (the plan never needed to force the full 40.0 reference — correct HM.5.3 under-peaking behavior at this horizon); Taper W1 = 24.0 (34.0×0.70=23.8→24.0 rounded), Taper W2 = 14.5 (34.0×0.43=14.62→14.5 rounded) — **not** `24.0×0.43=10.3`, which would be the chained (wrong) result. This is the exact non-chained mechanism check the phase required.

## 24. Session allocation

`RUN_LAYOUT_4D` (1 KEY_SESSION + 2 EASY_SUPPORT + 1 LONG_RUN) reused unmodified — every materialized week in every passing test has exactly 4 sessions per week (implicitly verified by the real, unmodified `BoundCatalogPlanValidator`/`GeneratedCatalogPlanSkeletonValidator` that every successful materialization in this phase's tests passed through).

## 25. Workout binding

Reused unmodified — the same `CatalogWorkoutBinder`/`half-marathon-workout-progression` artifact HM's canonical candidate already uses; no new workout definition or binding rule was authored.

## 26. 8-14W traces

- **8W:** the horizon *gate* correctly admits it (`RealPipeline_EightWeekHorizon_PassesHorizonGate_ButFailsPhaseAllocation_ForCatalogReuseReasons` proves the rejection is NOT `PlanCoreHorizonTooShortException`/`PlanHorizonCompositionRequiredException`), but the reused, unmodified `HALF_MARATHON__4D__INTERMEDIATE` catalog identity's own phase-allocation minimum-week sum (Foundation+Build+RaceSpecific+Taper minimums, as authored in the master template's phase-allocation policy) is **10 weeks**, inherited unchanged from HALF_MARATHON's own real minimum. An 8-week plan cannot be phase-allocated against this reused identity: it fails with `DynamicCoreWeekSkeletonInfeasibleException: TARGET_BELOW_SUM_OF_MINIMUMS: targetWeekCount=8, sumOfMinimums=10`, proven directly by `EightWeekHorizon_CannotPhaseAllocate_BecauseReusedCatalogIdentityOwnMinimumSumIsTen`. This is a genuine, disclosed cross-authority conflict — see §43.
- **12W:** materializes fully and correctly — see the complete golden trace in §27.
- **14W:** materializes successfully (`RealPipeline_EligibleHorizons_MaterializeSuccessfully`/`TwoOfThreeHorizons_MaterializeWithinFrozenPeakAndNeverForcePeak`), peak volume bounded at ≤40.0km, weekly deltas ≤2.5km, all validated.

## 27. 12W golden trace (primary pilot debugging artifact)

Real output from `GoldenTrace_12W_MaterializesWithFrozen16KNumericsNotHmNumerics` (passing), with `CanonicalDistanceFamily=HALF_MARATHON` and `RequestedTargetDistanceKm=16.0` both confirmed set simultaneously on the same request/context:

| Week | Phase | Volume (km) | Long Run (km) | LR Share | Taper |
|---|---|---|---|---|---|
| W01 | FOUNDATION | 20.0 | 6.5 | 32.5% | No |
| W02 | FOUNDATION | 21.5 | 7.0 | 32.6% | No |
| W03 | BUILD | 23.0 | 7.5 | 32.6% | No |
| W04 | BUILD | 24.5 | 8.0 | 32.7% | No |
| W05 | BUILD | 26.0 | 8.5 | 32.7% | No |
| W06 | RACE_SPECIFIC | 28.0 | 9.0 | 32.1% | No |
| W07 | RACE_SPECIFIC | 29.5 | 10.5 | 35.6% | No |
| W08 | RACE_SPECIFIC | 31.0 | 11.5 | 37.1% | No |
| W09 | RACE_SPECIFIC | 32.5 | 12.5 | 38.5% | No |
| W10 | RACE_SPECIFIC | 34.0 | 13.5 | 39.7% | No |
| W11 | TAPER | 24.0 | 8.0 | 33.3% | **Yes (W1 = 34.0×0.70)** |
| W12 | TAPER | 14.5 | 5.0 | 34.5% | **Yes (W2 = 34.0×0.43, non-chained)** |

Phase split: FOUNDATION=2, BUILD=3, RACE_SPECIFIC=5, TAPER=2 (sums to 12, matching DIST-GEN.1 §38's derivation exactly). Peak reached (34.0) correctly stays below the 40.0 reference (HM.5.3 under-peaking behavior, real readiness of 20.0km/10.0km governed the actual starting point rather than the 23.5 calibration reference). Every long run stays `< 16.0` and `<= 15.0`; every LR share stays within [30%,40%]; weekly volume deltas are a constant +1.5km (well inside the 2.5km cap).

## 28. 7W/15W boundaries

Both rejected via `TargetDistance16KHorizonPolicy`'s own explicit authority, proven by real `PlanServices.GeneratePreviewAsync` calls (`RealPipeline_BelowMinimumHorizon_...`/`RealPipeline_AboveMaximumHorizon_...`) carrying `DARK_16K_CORE_HORIZON_*_NOT_SUPPORTED` reason codes, explicitly distinct from HALF_MARATHON's own boundary text and never touching TEN_K's fallback arm (§9).

## 29. Target-distance identity proof

`RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously` (and the generation-time payload it inspects) proves `CanonicalDistanceFamily="HALF_MARATHON"` and `RequestedTargetDistanceKm=16.0` coexist, both non-null, neither overwriting the other, at both generation time (`normalized_input.requested_target_distance_km=16` in the persisted preview payload) and persisted-plan time (§30).

## 30. Persistence proof

**Reachable, and proven, through the real production pipeline — not a test-only backdoor.** `PlanServices.GeneratePreviewAsync` → `PlanServices.ConfirmPlanAsync` → `CatalogPlanConfirmationService.ConfirmAsync` (entirely unmodified control flow) was exercised end-to-end with an in-memory EF Core `AppDbContext`. A genuine, disclosed bug was found and fixed during this proof: `CatalogPlanConfirmationService.BuildCatalogTrainingPlan` unconditionally set `RequestedTargetDistanceKm = snapshot.NormalizedInput.GoalDistanceKm` (the family-representative constant), silently collapsing the two fields for every plan ever confirmed, including the dark 16K pilot's own already-correct generation-time value. Fixed to `snapshot.NormalizedInput.RequestedTargetDistanceKm ?? snapshot.NormalizedInput.GoalDistanceKm` — zero-delta for every canonical request (whose own `RequestedTargetDistanceKm` always already equals `GoalDistanceKm`, enforced by `CatalogGoalDistanceResolver`'s own mismatch guard), and correct for the dark pilot. `RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously` proves the final persisted `TrainingPlan` row: `CanonicalDistanceFamily="HALF_MARATHON"`, `RequestedTargetDistanceKm=16.0`, `GoalDistanceKm=21.0975` — all three simultaneously correct, neither of the first two overwriting the other.

## 31. DTO/user-visible-distance readiness

Not implemented in this phase (correctly out of scope per DIST-GEN.1 §42/§50 — a bounded, disclosed DIST-GEN.3 item). `HomeResponse`/`ActivePlanSummaryDto` still carry no numeric-distance field; nothing in this phase touches them. The schema-level data needed (`RequestedTargetDistanceKm`, now correctly populated end-to-end) is ready for a future DTO extension.

## 32. Public Custom still fail-closed

Not re-tested with a new test in this phase (would be a pure duplicate of existing coverage), but proven unaffected: `GoalDistanceCustomFailClosedTests`'s full 13-test suite (including `RacePreview_GoalDistanceCustom_FailsClosed_NeverFiveKPlan_NeverFiveHundred`, the four zero-delta tests, and both horizon-unreachability proofs) ran unchanged as part of the full monolithic regression (§40) and all still pass — none of the 4 pre-existing failures in that run belong to this test class.

## 33. Unsupported arbitrary distances remain dark/unavailable

12K, 15K, 18K, 20K: `CanonicalDistanceFamilyResolver` classifies all four into `HALF_MARATHON` (a pre-existing, unmodified capability), but `Dark16KPilotEligibilityPolicy.IsEligible` returns `false` for every one of them (§6) — none can reach `CatalogVolumeAndLongRunPlanner`'s new dispatch branch, `CatalogPreviewGenerator`'s override resolution, or the horizon substitution, because every one of those call sites gates on the identical eligibility check. No wire format exists to even express them as a request in the first place (§2/§4).

## 34. Canonical TEN_K zero-delta

Every `CatalogVolumeAndLongRunPlanner` dispatch branch for `CanonicalDistanceFamily == "TEN_K"` is completely untouched (no new branch was added anywhere near them); the new 16K branch is scoped exclusively to `CanonicalDistanceFamily == "HALF_MARATHON" && Level == "INTERMEDIATE" && DaysPerWeek == 4`, structurally unreachable for TEN_K. Full regression (§40) shows zero TEN_K-related new failures anywhere in the 4858-test run.

## 35. Canonical HM zero-delta

`VolumeSafetyPolicy.HalfMarathonIntermediate4D` itself was not modified (only a new, separate, additively-named sibling instance was added). The new `CatalogVolumeAndLongRunPlanner` branch is checked, and its `Dark16KPilotEligibilityPolicy.IsEligible` guard requires `RequestedTargetDistanceKm` to differ from the family default (always false for canonical HM requests, whose own `RequestedTargetDistanceKm` is always resolved to 21.0975 by the unmodified `CatalogGoalDistanceResolver`/`ResolveRequestedTargetDistanceKm` logic) — so canonical HM 4D Intermediate requests always fall through unchanged to `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(4)` exactly as before. `RealPipeline_ZeroDelta_CanonicalHalfMarathon14W_StillSucceeds_NoOverride` and `RealPipeline_ZeroDelta_CanonicalHalfMarathon_TooShortForItsOwnBounds_StillUsesHmOwnMinimum` directly prove this at the full-pipeline level (the latter proving the horizon gate specifically still uses HM's real 10-week minimum, not the pilot's 8, for a canonical request).

## 36. HM 6D zero-delta

`VolumeSafetyPolicy.HalfMarathonIntermediate6D` and its dispatch branch are untouched; the new 16K branch's `DaysPerWeek == 4` condition makes it structurally unreachable for any 6D candidate. The full monolithic regression includes HM-X1.4's own 6D activation test suites (part of the unchanged 4854 passing tests).

## 37. Catalog/hash integrity

`PlanCatalog.Tests`: **1510/1510** passed, exactly matching the DIST-GEN.0.1 baseline — trivially confirming zero catalog artifact was touched (verified additionally via `git status --porcelain -- plan-catalog`, clean after reverting one auto-regenerated audit-timestamp file).

## 38. Recurring assumption-family findings

Three genuine, non-obvious findings surfaced only by real dark implementation and testing (not assumed, not glossed over):
1. **Peak-volume-band catalog-loader divergence** (§16, §43): the reused catalog identity's own real peak-volume-band entry (HM's [36,50]) is architecturally distinct from the pilot's own frozen [34,46] authority; required a dedicated decorator (`TargetDistance16KAwarePeakVolumeBandLoader`) at two independent call sites (`CatalogPreviewGenerator`'s static and dynamic-core paths).
2. **Candidate-core-cycle-driven horizon computation exists at TWO independent layers** (§9): `PlanServices`'s public gate and `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton`'s own internal routing decision both independently call `RaceHorizonPolicy.Decide` against the candidate's own catalog-declared bounds; both needed the same substitution.
3. **The "static" (exact-preferred-core) generation path is structurally tied to the reused candidate's own preferred week count**, never the pilot's own distinct preferred length, even when both classify as `PreferredCore` under their own respective bounds (§9/§26) — the dark 16K pilot must always route through the generic dynamic-core path, regardless of its own horizon Mode.
4. **`CatalogPlanConfirmationService.BuildCatalogTrainingPlan`'s own independent (and, until this phase, incorrect) re-derivation of `RequestedTargetDistanceKm`** (§30) — a real, pre-existing latent defect (silently collapsing the two fields for every plan, not just this pilot's) that only a genuine end-to-end persistence proof surfaced.
5. **HM_PACE's pace resolution never actually consumes target distance** (§12) — a correction to DIST-GEN.1's own stated expectation, discovered by direct code read rather than assumed.

## 39. Targeted tests

`Dark16KTargetDistanceProjectionTests.cs` — 40 tests, all passing, covering: eligibility gate (both overloads, 12 parameterized cases), `CanonicalDistanceFamilyResolver` classification-vs-eligibility distinction (6 cases), frozen `VolumeSafetyPolicy`/peak-band values, horizon-policy boundaries and reason-code distinctness, real full-pipeline materialization at 12W/14W, real full-pipeline rejection at 7W/15W with distinct reason codes, real zero-delta proofs for canonical HM at 14W and at HM's own too-short 8W, the 8W phase-allocation finding (both at the horizon-gate layer and the low-level orchestrator layer), the 12W golden trace, and the full generation→confirmation persistence proof.

## 40. Full regression (exact counts + exact failing test names)

- `PlanCatalog.Tests`: **1510 total / 1510 passed / 0 failed** (trx `<Counters>`).
- `RuntimeCatalog` subtree (`--filter FullyQualifiedName~RuntimeCatalog`): **3882 total / 3882 passed / 0 failed** (trx `<Counters>`; 3842 baseline + 40 new dark 16K tests).
- Full monolithic `RunningApp.IntegrationTests`: **4858 total / 4854 passed / 4 failed** (trx `<Counters total="4858" ... passed="4854" failed="4">`, cross-checked against a scoped `<UnitTestResult ... outcome="Failed" ...>` tag match — never a broad grep). The 4 failures, by exact full name:
  1. `RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
  2. `RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
  3. `RunningApp.IntegrationTests.LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
  4. `RunningApp.IntegrationTests.Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

One genuine regression was found and fixed during this phase's own implementation pass (not left in the final state): `PreparationRunwayHorizonAuthorityTests.CatalogPreviewGeneratorSource_HasExactlyOneRaceHorizonPolicyDecideCallSite_OutsideRunwayMethod` initially failed (2 call sites found instead of 1) after the first horizon-substitution edit; fixed by resolving bounds to locals before a single `RaceHorizonPolicy.Decide` call (§9), and confirmed passing in both the targeted rerun and the full RuntimeCatalog subtree rerun before the final monolithic run.

## 41. Exact durable-baseline comparison

Baseline (DIST-GEN.0.1): 4818 total / 4814 passed / 4 failed. This run: 4858 total (4818 + 40 new tests) / 4854 passed (4814 + 40) / 4 failed. **The delta is exactly the 40 new tests, all passing. Zero new regression failures**, and the 4 failures are byte-identical by full name to the frozen baseline.

## 42. DIST-GEN.3 readiness

Genuinely bounded, disclosed, non-authority implementation items remain for a future phase:
- `CanonicalTargetFinishTimePolicy`'s 16K product-average method (§14) — the method is frozen (DIST-GEN.1 §18) but unwired; no code path in this phase computes it.
- `HomeResponse`/`ActivePlanSummaryDto` numeric-distance display fields (§31).
- A dedicated missing/zero-readiness fail-closed regression test for this exact new policy instance (§19) — the underlying guard is generic and already proven for every other named policy; this phase did not add a policy-specific test.
- Public request-contract wiring (a real `target_distance_km` field on a public DTO) — explicitly out of scope for this dark-only phase.
- The peak-volume-band catalog-loader divergence (§16/§38) is worked around correctly for this pilot via a decorator, but a future phase adding more distance-projected cells should generalize this pattern rather than adding a decorator per pilot.

## 43. Remaining blockers

**One genuine, disclosed, non-workaroundable blocker within this pilot's own frozen authority:** the 8-week horizon, though correctly accepted by `TargetDistance16KHorizonPolicy` (DIST-GEN.1's own frozen minimum), cannot actually be phase-allocated against the reused, unmodified `HALF_MARATHON__4D__INTERMEDIATE` catalog identity — that identity's own real phase-allocation minimum-week sum is 10 weeks (inherited unchanged from HALF_MARATHON's own real minimum), a full 2 weeks above this pilot's own frozen 8-week floor. This is a genuine cross-authority conflict between DIST-GEN.1's §10 evidence-derived horizon freeze (based on external 10-mile-plan arithmetic that did not check the reused catalog's own phase-allocation minimums) and this phase's own "zero catalog file changes" diff-safety constraint (§7) — the two cannot both be fully satisfied for the 8-week cell without either (a) modifying HALF_MARATHON_MASTER's own phase-allocation minimums (which would affect every HM candidate, violating catalog-identity-reuse), or (b) authoring a new catalog artifact for this pilot (explicitly disallowed, §7/DIST-GEN.1 §6). This is recorded honestly, proven by real production code (`DynamicCoreWeekSkeletonInfeasibleException: TARGET_BELOW_SUM_OF_MINIMUMS: targetWeekCount=8, sumOfMinimums=10`), not silently narrowed or hidden. 12W and 14W are fully feasible and fully proven (§26-27).

No other blockers. Every other required proof item materialized successfully through the real, unmodified production pipeline.

---

## FINAL CLASSIFICATION

```
16K_INTERMEDIATE_4D_FULL_DARK_TARGET_DISTANCE_PROJECTION_IMPLEMENTATION_BLOCKED
```

Not `_COMPLETE`, for exactly one reason, stated precisely: **the 8-week horizon, while correctly admitted by the new, explicit `TargetDistance16KHorizonPolicy` (8/12/14) authority, cannot be phase-allocated against the reused, unmodified HALF_MARATHON catalog identity, whose own real phase-allocation minimum-week sum is 10 weeks** (§43). This is not a training-authority defect, not a code defect, and not something this phase silently worked around — it is a genuine, disclosed structural tension between two of DIST-GEN.1's own frozen constraints (the 8-week minimum vs. full catalog-identity reuse) that this implementation phase surfaced by real testing rather than reasoning about in the abstract. Every other requirement holds: HALF_MARATHON remains the sole structural parent and TEN_K remains structurally uninvolved (§7, §34); no new distance family, enum value, or catalog combination was created (§3, §7); 12W and 14W of the three frozen horizons materialize completely and correctly, including a full golden trace (§26-27); 7W and 15W reject via the new, explicit, distinctly-reasoned 16K horizon authority, never TEN_K's fallback arm or HM's own bounds (§9, §28); every frozen numeric value from DIST-GEN.1 §47 is encoded exactly, with no training-load interpolation anywhere (§1, §16-23); distance-family/target-distance separation is proven to persist correctly end-to-end through real confirmation (§29-30); the public `GoalDistance.Custom` fail-closed contract is completely unaffected (§2, §32); 12K/15K/18K/20K and every other arbitrary distance remain dark/ineligible (§33); canonical TEN_K, HALF_MARATHON (including the HM-X1.4 6D cell) are zero-delta, proven both structurally and by a clean 4858-test regression run with the identical frozen 4-failure baseline (§34-36, §40-41). The single 8-week blocker is scoped, well-understood, and does not implicate 12W/14W or any canonical behavior — it is the smallest exact blocker, not a broader one, and is recommended as the first item for DIST-GEN.3 or a dedicated follow-up to resolve (most plausibly by re-examining whether 8W genuinely belongs in this pilot's own frozen minimum given the reused catalog's real structural floor, rather than by modifying catalog identity).
