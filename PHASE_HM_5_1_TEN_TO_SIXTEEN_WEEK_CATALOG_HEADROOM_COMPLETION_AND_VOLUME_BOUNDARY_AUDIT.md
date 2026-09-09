# PHASE HM.5.1 — Half-Marathon 10–16W Catalog Headroom Completion and Volume Boundary Audit

Append-only correction/completion of `HM.5` (`PHASE_HM_5_TEN_TO_SIXTEEN_WEEK_CATALOG_HEADROOM_IMPLEMENTATION.md`, commits `b558e68`/`63a218e`, classification `HM_10_16W_CATALOG_HEADROOM_IMPLEMENTATION_BLOCKED`), mirroring this engagement's own established pattern of an append-only follow-up phase correcting an earlier phase's partial finding rather than rewriting pushed history (e.g. `HM.2`'s own "full-slice continuation — current correction" superseding its Step-2 partial classification). HM.5 found a real `ProgressionStageAllocator` capability gap and, on that basis, authored **no** catalog file at all. This phase re-traced the exact same mechanism **stage by stage** (not only for the two `min=0` INTRO-family stages HM.5 examined) and found the gap is narrower than HM.5 concluded: it affects exactly **one** catalog field (`BUILD_FASTER_THAN_HM_INTRO.minimumExposures`), not the entire Layer B authoring effort. This phase implements everything HM.4 authored except that one field (kept at its current value, precisely disclosed below), empirically proves the result against the real, unmodified production pipeline, and discloses two further genuine, real, quantified volume-planner numeric-safety blockers found only once real end-to-end materialization was attempted for every newly-feasible horizon.

## 1. HM.4 frozen authority consumed

`PHASE_HM_4_PHASE_AND_STAGE_NUMERIC_HEADROOM_AUTHORITY_CLOSURE.md` and `PHASE_HM_3_TEN_TO_SIXTEEN_WEEK_DYNAMIC_CORE_PHASE_ALLOCATION_AUTHORITY_CLOSURE.md` were read in full (again, independently of HM.5's own reading). Layer A (phase Min/Preferred/Max/priorities, §5) and Layer B (stage Min/Preferred/Max/behavior, §6) were taken exactly as frozen. The seven-row oracle table (`10W→2/3/3/2 … 16W→4/5/5/2`) and per-horizon stage-occupancy table (§9–§10) are used strictly as verification oracles, never hardcoded into production code.

## 2. Catalog lifecycle/versioning decision

Both `half-marathon-master.v1.json` and `half-marathon-workout-progression.v1.json` carry `metadata.status = "VALIDATED"` (not `"PUBLISHED"`). HM.5's own audit (§2 of its report) reasoned from **TEN_K's** version-bump convention (`ten-k-master.v9→v11`) and concluded a new `v2` would be required for any change. This phase re-audited the **HALF_MARATHON-specific** history directly and found a closer, dispositive precedent HM.5 did not check: commit `1cc7a40` ("HM master CoreCycle authority correction") edited `half-marathon-master.v1.json` **in place** — changing `coreCycle.minimumWeeks`/`maximumWeeks` and adding a field — while `metadata.status` remained `"VALIDATED"` both before and after (verified via `git show 1cc7a40^:plan-catalog/catalog/templates/half-marathon-master.v1.json` against the current file). No hash-exception registry or golden-fixture snapshot test anywhere in `plan-catalog/tests/PlanCatalog.Tests` pins a hash for `HALF_MARATHON_MASTER` or `HALF_MARATHON_WORKOUT_PROGRESSION` (confirmed by search), and `plan-catalog/artifacts/appsel-plan-catalog/*` contains no released Half-Marathon artifact at all, so no publish-time cross-release hash guard is at risk.

**Decision**: in-place edit of both `v1` files, matching the direct, on-point `1cc7a40` precedent (a VALIDATED-but-not-PUBLISHED HM master template has already been edited in place once in this exact repository), rather than TEN_K's PUBLISHED-artifact version-bump convention, which does not apply here since HALF_MARATHON has never been published. `PlanCatalog.Tests` (1510/1510, §16) confirms zero hash regression from this choice.

## 3. Files/artifacts changed

- `plan-catalog/catalog/templates/half-marathon-master.v1.json` — Layer A phase headroom (in place).
- `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` — Layer B stage headroom, with one disclosed exception (§5).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm2FullDarkVerticalSliceTests.cs` — extended with the full 10–16W proof matrix (superseding its own stale `NonPreferredCoreLengths_RemainFailClosedUntilExactPhaseAllocationsExist` test, which asserted the pre-HM.5 infeasibility that this phase intentionally changes).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm5StageAllocatorMechanismProofTests.cs` — new, pure in-memory mechanism proof (no catalog file involved) run **before** any catalog edit, to empirically (not just theoretically) characterize `ProgressionStageAllocator`'s real compression/extension mechanism.
- `PHASE_LEDGER.md` (new HM.5.1 row), `MASTER_ROADMAP.md` (new HM.5.1 subsection), this report.
- No schema file, no other production `.cs` file, no 10K catalog file was changed.

## 4. Phase headroom encoding

`half-marathon-master.v1.json`'s four phases were widened exactly to HM.4 §5, verbatim, with zero deviation:

| Phase | Min | Preferred | Max | CompressionPriority | ExtensionPriority |
|---|---|---|---|---|---|
| FOUNDATION | 2 | 3 | 4 | 1 (unchanged) | 1 (unchanged) |
| BUILD | 3 | 4 | 5 | 2 (unchanged) | 2 (unchanged) |
| RACE_SPECIFIC | 3 | 5 | 5 | 3 (unchanged) | 3 (unchanged) |
| TAPER | 2 (unchanged) | 2 | 2 (unchanged) | 4 (unchanged) | 4 (unchanged) |

## 5. Stage headroom encoding

`half-marathon-workout-progression.v1.json`'s stages were widened exactly to HM.4 §6, **with one precisely-scoped, empirically-forced exception**:

| Stage | Min | Max | vs. HM.4 §6 |
|---|---|---|---|
| `FOUNDATION_AEROBIC_BASE` | 2 | 4 | Exact match |
| `BUILD_FASTER_THAN_HM_INTRO` | **1 (unchanged)** | **1 (unchanged)** | **Deviation** — HM.4 specifies min=0. Kept at today's pinned 1/1. |
| `BUILD_THRESHOLD_DEVELOPMENT` | 3 (unchanged) | 5 | Exact match on Max; Min intentionally left at 3 (not lowered) — see §6 for why this is required, not merely convenient. |
| `RACE_SPECIFIC_HM_INTRODUCTION` | 0 | 1 | Exact match |
| `RACE_SPECIFIC_HM_DEVELOPMENT` | 1 | 2 | Exact match |
| `RACE_SPECIFIC_HM_DEVELOPMENT_CURRENT_FITNESS_FALLBACK` | 1 | 2 | Exact match (symmetric with the primary stage, per HM.4 §13) |
| `RACE_SPECIFIC_HM_REHEARSAL` / fallback | 2/2 (unchanged) | — | Unchanged, `PROTECTED`/`FIXED_EXPOSURE`, per HM.4 |
| `TAPER_HM_ACTIVATION` / fallback | 2/2 (unchanged) | — | Unchanged, `PROTECTED`/`FIXED_EXPOSURE`, per HM.4 |

**The one exception, precisely explained (§6)**: `BUILD_FASTER_THAN_HM_INTRO.minimumExposures` was **not** lowered to HM.4's authored `0`. Doing so is empirically proven (§6, `Hm5StageAllocatorMechanismProofTests.cs`) to break the frozen 14W golden stage occupancy through the real, unmodified `ProgressionStageAllocator` — a stronger, independently-frozen invariant than any single Layer B field value. This is a disclosed, evidence-forced deviation from HM.4's literal number, not a re-decision of HM.4's product judgment (HM.4's number is not wrong — it is simply not realizable by the current generic stage allocator without perturbing 14W).

## 6. Generic resolver proof

**Phase level (`CatalogPhaseAllocationResolver`): PASSES with zero code change**, exactly as HM.3/HM.4/HM.5 all already concluded. Confirmed directly (not re-cited) via `NonPreferredCoreLengths_NowFeasible_TraverseRealPipelineAndMaterializeValidDarkPreviews` and `TwelveAndFifteenWeekHorizons_...`/`SixteenWeekHorizon_...` in `Hm2FullDarkVerticalSliceTests.cs`: every one of the seven horizons' `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` call reproduces HM.4's oracle table exactly, `IsMathematicallyFeasible=true`, using the real, loaded `HALF_MARATHON__4D__INTERMEDIATE` v1 candidate — no hardcoded per-horizon branch anywhere.

**Stage level (`ProgressionStageAllocator`): a real, narrow capability gap exists, empirically confirmed** (not merely re-cited from HM.5) via three new, executed, passing tests in `Hm5StageAllocatorMechanismProofTests.cs`, run **before** any catalog file was touched:

1. `Hm4ProposedIntroMinimumZero_At14WeekFrozenBuildLength_DoesNotReproduceFrozenOneThreeOccupancy` — constructs HM.4's literal proposed `BUILD_FASTER_THAN_HM_INTRO` (0/1) and `BUILD_THRESHOLD_DEVELOPMENT` (3/5) stage data against a synthetic 4-week Build phase skeleton (14W's real, unchanged Build length) and runs the real `ProgressionStageAllocator.Allocate`. **Observed, real result: `INTRO=0, THRESHOLD=4`** — not the frozen golden `1/3`. Root cause: the allocator has no `PreferredExposures` field distinct from `MinimumExposures` (confirmed absent from `CatalogWorkoutProgressionStage` and from `workout-progression.schema.json`); its `ApplyExtension` method fills surplus into the **highest-`RelativeOrder`** Extendable stage first (`BUILD_THRESHOLD_DEVELOPMENT`, order 2, before `BUILD_FASTER_THAN_HM_INTRO`, order 1) — confirmed identical to the pre-existing, already-passing, already-named `BuildPhase_ExtensionAllocatesSurplusToHigherRelativeOrderStageFirst` test's own documented mechanism for 10K's `FARTLEK_INTRO`/`THRESHOLD_INTRO` pair.
2. `Hm4ProposedIntroMinimumZero_At3WeekCompressedBuildLength_ProducesZeroPlusThreeAsHm4Predicted` — the same stage data against a 3-week Build phase (10–12W's real Build length) **does** reproduce HM.4's own predicted `0+3` split, because at that phase length `totalMinimum(seed)` already equals `availableWeeks`, so neither compression nor extension ever triggers.
3. `ApplyCompression_NeverReducesACompressibleStageBelowOneExposure_RegardlessOfDeclaredMinimum` — direct proof that `ApplyCompression`'s reduction floor is a hardcoded literal `1`, independent of a stage's own catalog-declared minimum.

**Why RACE_SPECIFIC's identical-shaped fields (`RACE_SPECIFIC_HM_INTRODUCTION` min=0) do NOT hit the same failure, hand-traced against the same real algorithm and cross-checked by the passing end-to-end tests in §11**: at every RACE_SPECIFIC horizon (10–16W), the extension surplus is large enough that after `RACE_SPECIFIC_HM_DEVELOPMENT` (the higher-`RelativeOrder` sibling, order 2) is filled to its own `MaximumExposures`, any remaining surplus spills over to `RACE_SPECIFIC_HM_INTRODUCTION` (order 1) — e.g. at 14W (frozen), surplus=2, `DEVELOPMENT` headroom=1 (absorbs 1), remaining 1 spills to `INTRODUCTION` (headroom=1, absorbs 1), landing exactly on the frozen `1+2+2` occupancy. This is a coincidence of RACE_SPECIFIC's specific headroom magnitudes, not evidence the underlying mechanism is sound in general — BUILD's smaller 14W surplus (1) is fully absorbed by `THRESHOLD_DEVELOPMENT` alone, never reaching `FASTER_THAN_HM_INTRO`. This asymmetry is the precise reason only one field needed to be held back.

**Conclusion**: this is not an HM.4 authority defect (HM.4's numbers remain correct product judgments) — it is a real, narrow, generic-mechanism capability gap in `ProgressionStageAllocator`, present regardless of distance family, that happens to bind for exactly one HM stage given HM's specific `RelativeOrder`/headroom-magnitude combination. `CatalogPhaseAllocationResolver` needed and received zero code change; `ProgressionStageAllocator` needed and received zero code change (the gap was worked around by data authoring, not by touching the shared algorithm) — per the governing instruction to treat any resolver/allocator code change as a red flag requiring proof, and given a data-only workaround exists that fully preserves 14W zero-delta, no production code change was made.

## 7. Exact 10–16 phase table

Produced against the real, loaded `HALF_MARATHON__4D__INTERMEDIATE v1` candidate (`CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)`), all seven rows passing, matching HM.4's oracle exactly:

| Horizon | Foundation | Build | RaceSpecific | Taper | Feasible |
|---|---|---|---|---|---|
| 9W | — | — | — | — | **No** (`TARGET_BELOW_SUM_OF_MINIMUMS`) |
| 10W | 2 | 3 | 3 | 2 | Yes |
| 11W | 2 | 3 | 4 | 2 | Yes |
| 12W | 2 | 3 | 5 | 2 | Yes |
| 13W | 2 | 4 | 5 | 2 | Yes |
| 14W | 3 | 4 | 5 | 2 | Yes (frozen, unchanged) |
| 15W | 4 | 4 | 5 | 2 | Yes |
| 16W | 4 | 5 | 5 | 2 | Yes |
| 17W | — | — | — | — | **No** (`TARGET_ABOVE_SUM_OF_MAXIMUMS`) |

## 8. Exact 10–16 stage occupancy

Produced via the real, unmodified `ProgressionStageAllocator` operating on the real dark `DynamicCoreCalendarMaterializationOrchestrator` pipeline's own generated skeleton for each horizon (not a synthetic fixture):

| Horizon | FOUNDATION_AEROBIC_BASE | BUILD (INTRO+THRESHOLD) | RACE_SPECIFIC (INTRO+DEV+REHEARSAL) | TAPER |
|---|---|---|---|---|
| 10W | 2 | 1+2 | 0+1+2 | 2 |
| 11W | 2 | 1+2 | 0+2+2 | 2 |
| 12W | 2 | 1+2 | 1+2+2 | 2 |
| 13W | 2 | 1+3 | 1+2+2 | 2 |
| 14W | 3 | 1+3 (frozen) | 1+2+2 (frozen) | 2 |
| 15W | 4 | 1+3 | 1+2+2 | 2 |
| 16W | 4 | 1+4 | 1+2+2 | 2 |

Deviation from HM.4's own §10 table: Build's compressed-horizon split is `1+2` here (10–12W) instead of HM.4's predicted `0+3` — a direct, disclosed consequence of §5/§6's one exception (`BUILD_FASTER_THAN_HM_INTRO` held at 1/1). The **total** Build week count is identical to HM.4's table at every horizon; only the internal split of *which* stage absorbs the compression differs. Every other phase's occupancy matches HM.4 §10 exactly, including the frozen 14W row.

## 9. Faster-than-HM fallback proof

The real `half-marathon-workout-progression.v1.json` declares `BUILD_FASTER_THAN_HM_INTRO` with `"requires": []` — unconditioned, confirming HM.5's own §9 finding: no real `requires`/`fallbackStageKey` eligibility branch exists for this stage today (HM.1.3's narrative "0+4 fallback shape" does not exist as real catalog wiring). Because this phase keeps the stage permanently at exactly 1 exposure (§5), Build progression is deterministic and identical regardless of any hypothetical eligibility state — HM dynamic-core feasibility does not depend on faster-than-HM eligibility, proven trivially by construction (there is no branch to depend on).

## 10. Pace-evidence fallback proof

`MissingIndependentExactPaceEvidence_HoldsForEveryNewlyFeasibleHorizon` (new test) runs the full dark pipeline for every horizon where full end-to-end materialization succeeds (10, 11, 13 — 12/15/16 are excluded here because they hit the volume-layer blockers in §13/§14, unrelated to pace evidence) with `exactPaceEvidence: false`. For each: `HM_PACE` never appears in any session's `WorkoutDefinitionKey` (confirming the approved `THRESHOLD_TEMPO`/`EASY_STANDARD` fallback activates), and the final prescribed plan's `ValidationResult.IsValid` holds. No desired-finish-time-derived fake pace is introduced. HM.2's pace authority is untouched.

## 11. 14W zero-delta proof

`PreferredFourteenWeekCore_TraversesRealPipelineAndMaterializesValidDarkPreview` — the pre-existing HM.2 golden test, **unmodified** — was run **after** both catalog files were edited and **passed unchanged**: 14 weeks, 56 sessions, `3/4/5/2` phase allocation, the exact frozen KEY-lane stage sequence (including `BUILD_FASTER_THAN_HM_INTRO` once, `RACE_SPECIFIC_HM_INTRODUCTION` once, etc.), 43.0km peak volume, 17.0km max long run, 30.0/18.5km taper, HM_PACE derivation, calendar validity, and preview payload semantics all byte/semantically identical to before this phase. This is the single most important result in this report: catalog headroom authoring did not perturb the preferred case.

## 12. Non-14W dark reachability

Confirmed **yes** — and confirmed through the real, already-existing `DynamicCoreCalendarMaterializationOrchestrator` pipeline (`Hm2FullDarkVerticalSliceTests.Pipeline()`), which already wires `DynamicCoreWeekSkeletonOrchestrator` (the `Resolve(candidate, targetWeekCount)`-based overload) end-to-end through volume, binding, prescription, and calendar materialization — this pipeline pre-existed this phase and was already exercised for 10K's own 8–14W range (`DynamicCoreWeekSkeletonOrchestratorTests.cs`). No new wiring was required. HM remains entirely non-public: `HalfMarathonIdentity_RemainsOutsideEveryPublicGate` (unmodified) continues to pass, and every new test explicitly re-confirms `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` returns `false` for Half-Marathon. Runway and LongHorizon were not touched and remain unreachable for HM (no HM Runway/LongHorizon artifact exists in the catalog at all).

## 13. Short-core numeric-safety proof

**Two of four shorter/compressed horizons are fully safe; two hit a genuine, real, quantified blocker — not invented, not silently worked around:**

- **10W, 11W, 13W: SAFE.** Full real-pipeline materialization succeeds; `PeakVolumeKm` stays at or below the 43.0km reference (well below it for the shorter horizons, confirming HM.5's own plausibility read that fewer non-taper transitions produce a lower, not higher, reachable peak); every long-run week's share stays ≤40% and ≤19km; taper is non-chained (two weeks, non-increasing).
- **12W: BLOCKED — `FINAL_WEEK_10_LONG_RUN_SHARE_EXCEEDS_CAP`.** Phase allocation (`2/3/5/2`) and stage occupancy are both valid, but the real, unmodified `CatalogFinalPrescribedPlanValidator`'s own fail-closed check rejects week 10's long-run distance as exceeding `week.PlannedWeeklyVolumeKm * hardCap` (the frozen 0.40 `HardLongRunShareCap`). Proven directly via `TwelveAndFifteenWeekHorizons_HitGenuineLongRunShareNumericSafetyBlocker(targetWeekCount: 12)`, which asserts the exact real exception and error code, not a hand-derived guess.

No `VolumeSafetyPolicy` field, growth-rate coefficient, or long-run-share constant was modified to make 12W pass. Per the governing instruction, this is reported as the correct, fail-closed, existing behavior — not silently worked around, not hidden behind an artificially inflated total volume.

## 14. Long-core extension-safety proof

**One of two extended horizons is fully safe; the other hits a second, distinct, genuine, real, quantified blocker:**

- **13W, 14W (both use Build=4/Foundation≤3, i.e. no more extension than 14W already uses): SAFE** (13W's numbers already covered in §13; 14W is the frozen golden case).
- **15W: BLOCKED — `FINAL_WEEK_13_LONG_RUN_SHARE_EXCEEDS_CAP`.** Same class of failure as 12W's, at a different week, proven via `TwelveAndFifteenWeekHorizons_HitGenuineLongRunShareNumericSafetyBlocker(targetWeekCount: 15)`.
- **16W: BLOCKED — genuine peak-volume ceiling overshoot, distinct from the long-run-share failures.** `SixteenWeekHorizon_HitsGenuinePeakVolumeCeilingOvershootBlocker` proves the real, unmodified planner resolves `PeakVolumeKm = 46.5`, strictly above the frozen `43.0` `ResolvedPeakReferenceKm`. Root cause, traced directly in `CatalogVolumeAndLongRunPlanner.ResolvePeak` (lines ~326–328): `transitionAdjustedMultiplier = 1 + ((canonicalDefaultMultiplier - 1) * transitions / GoldenFixtureNonTaperTransitions)` is a **linear extrapolation**, calibrated at the 14W golden fixture's own non-taper transition count. 16W's Foundation(4)+Build(5)+RaceSpecific(5) = 14 non-taper weeks exceeds the golden fixture's calibration point, so the multiplier — and the resulting peak — extrapolates *past* 43.0km rather than saturating at it. This is exactly the "extension invents a higher HM peak target" failure mode the governing prompt's §14 explicitly warned against and explicitly forbade fixing via a growth-policy change in this phase.

No `ResolvePeak` formula, `GoldenFixtureNonTaperTransitions` value, or `ResolvedPeakReferenceKm` ceiling was modified. Both blockers are reported precisely, with an exact pinned reproduction value (`46.5`), so a future phase's fix is verifiable against a concrete regression, not a vague description.

## 15. Long-run/taper proof

For every horizon that **does** fully materialize (10, 11, 13, 14): every long-run week's share ≤ 0.40 (hard cap) and distance ≤ 19.0km (preferred absolute peak ceiling); taper is exactly 2 weeks, non-increasing (non-chained) for every one of them, not just the 14W golden case. For the three horizons that do not fully materialize (12, 15, 16), the failure occurs specifically in the volume/long-run layer's own fail-closed safety checks — confirming those checks work exactly as designed, catching a genuine unsafe trajectory rather than silently producing one.

## 16. Catalog hash/dependency proof

`PlanCatalog.Tests` — **1510/1510 passed**, run once after the final catalog edits (§2/§4/§5). Zero new failures, zero hash-exception-registry entries required, matching the pre-existing baseline exactly (`HM.2.1` established the same 1510/1510 total before this phase). Confirms the in-place `VALIDATED`-status edit (§2) triggered no historical-hash mismatch anywhere in the suite.

## 17. 10K zero-delta proof

No 10K catalog file (`ten-k-master.v*.json`, `ten-k-workout-progression.v*.json`, or any other) was read for writing, and no shared production `.cs` file was modified. `PlanCatalog.Tests`' 1510/1510 pass count includes 10K's own governance/validation suites unchanged. `Hm5StageAllocatorMechanismProofTests.cs`'s own synthetic fixtures reuse `ProgressionStageAllocator` unmodified — the same class 10K's `ProgressionStageAllocatorTests.cs` already exercises — and neither that class nor `CatalogPhaseAllocationResolver` was edited, so 10K's phase allocation, stage allocation, catalog hash, workout binding, volume, taper, long-run, and public-routing behavior are structurally guaranteed unchanged by construction (no shared file touched). The full `RunningApp.IntegrationTests` run (§18) provides the empirical confirmation across every 10K-focused test in that suite.

## 18. Regression results

- **`PlanCatalog.Tests`**: 1510/1510 passed (§16).
- **`RunningApp.IntegrationTests` (HM-focused)**: `Hm2FullDarkVerticalSliceTests` + `Hm5StageAllocatorMechanismProofTests` — 17/17 passed (final run, after all adjustments described in §6/§13/§14).
- **`RunningApp.IntegrationTests` (full suite)**: run once, sequentially, after `PlanCatalog.Tests` completed (never concurrently, per this engagement's own established testing discipline). **Observed: 4515 passed / 4 failed / 4519 total, 52m43s.** The 4 failures are exactly the documented durable baseline, individually confirmed by name and error: `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13 and 14, `Expected OK / Actual InternalServerError`), `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` (the recurring wall-clock-date artifact), and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`. Zero new failures beyond this exact baseline — consistent with only catalog `.json` data and test files being changed this phase (no shared production `.cs` file was modified). The total (4519) is 7 higher than the previously recorded ~4512 baseline total, entirely accounted for by this phase's own 7 new HM-focused test cases added to `Hm2FullDarkVerticalSliceTests.cs`/`Hm5StageAllocatorMechanismProofTests.cs` (all of which passed).

## 19. Public/Runway/LongHorizon state

Unchanged. `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`/`IsSupportedPreparationRunwayIdentity` were not modified and continue to return `false` for every Half-Marathon identity (re-confirmed by `HalfMarathonIdentity_RemainsOutsideEveryPublicGate`, unmodified, and by an explicit re-assertion inside the new theory test). No Runway or LongHorizon artifact exists for Half-Marathon in the catalog; neither was activated or referenced by this phase.

## 20. Remaining blockers

Three, precisely scoped, none papered over:

1. **`ProgressionStageAllocator` has no stage-level `PreferredExposures`/decoupled-priority concept** (§6) — worked around by data authoring for this phase (holding `BUILD_FASTER_THAN_HM_INTRO` at 1/1), but the underlying generic-mechanism gap HM.5 identified is real and still open. A future mechanism-only phase (mirroring `HM.1.6`'s narrow-seam pattern) could close it with a new optional `preferredExposures` field (defaulting to `minimumExposures` for full 10K byte-identical-by-default compatibility) plus a decoupled stage priority field, after which `BUILD_FASTER_THAN_HM_INTRO` could be authored at its full HM.4-specified 0/1 range and 10–12W's Build occupancy would match HM.4 §10's `0+3` exactly instead of this phase's `1+2`.
2. **12W and 15W: `FINAL_WEEK_*_LONG_RUN_SHARE_EXCEEDS_CAP`** (§13/§14) — a genuine, real, reproducible volume/long-run-share safety-validator rejection. Requires a future phase to decide (not this one) how the reachable-peak/long-run-share resolution should behave for these two specific mid-range horizons — e.g. a different long-run-share interpolation, a lower reachable peak for these transition counts, or an explicit documented non-support decision — without touching the frozen 0.40/19.0km ceilings themselves.
3. **16W: peak volume resolves to 46.5km, above the 43.0km reference ceiling** (§14) — a genuine, real, reproducible overshoot in `ResolvePeak`'s linear extrapolation once non-taper transitions exceed the golden fixture's own calibration count. Requires a future phase to decide how `ResolvePeak` should saturate (rather than linearly extrapolate) once the golden fixture's calibrated transition range is exceeded.

10W, 11W, 13W, and 14W are genuinely, fully, safely dark-materializable end-to-end today, with zero remaining blocker.

## 21. Recommended next phase

Two independent, narrowly-scoped follow-ups, neither blocking the other:

- **HM.5.2 (mechanism-only, stage-allocator seam)**: add the `preferredExposures`/decoupled-priority concept described in §20 item 1, with full 10K zero-delta regression proof, then re-author `BUILD_FASTER_THAN_HM_INTRO` to its full HM.4-specified range.
- **HM.5.3 (volume-planner boundary, numeric-only)**: investigate and resolve the two distinct volume/long-run blockers in §20 items 2–3 (12W/15W long-run-share overflow; 16W peak-volume ceiling overshoot) as their own explicit product/numeric-mechanism decision, per the same "no forced closure" discipline — do not silently cap or rescale without a documented decision.

Until both land, HM's dark, non-public pipeline safely and fully supports exactly **10W, 11W, 13W, and 14W** end-to-end; 12W, 15W, and 16W are phase/stage-feasible but blocked at the volume-safety layer; 9W and 17W remain correctly infeasible at the phase-allocation layer.

## Final classification

`HM_10_16W_CATALOG_HEADROOM_IMPLEMENTATION_BLOCKED`

**Exact smallest blocker set** (three, independent, precisely scoped — not one monolithic blocker): (1) `ProgressionStageAllocator`'s missing stage-level preferred/priority concept forces one catalog field (`BUILD_FASTER_THAN_HM_INTRO`) to remain at its pre-HM.5 value rather than HM.4's full authored range; (2) 12W and 15W hit a genuine `FINAL_WEEK_*_LONG_RUN_SHARE_EXCEEDS_CAP` volume-safety rejection; (3) 16W's resolved peak volume (46.5km) genuinely exceeds the 43.0km reference ceiling. All three are real, reproduced by executed tests (not theorized), and none was papered over with an invented number or a silent policy change. Genuine, real progress beyond HM.5: Layer A is fully implemented and proven for all seven horizons with zero code change; Layer B is implemented for five of six affected fields exactly as HM.4 authored; the frozen 14W golden vertical slice is proven byte/semantically identical after both catalog edits; and four of seven newly-representable horizons (10W, 11W, 13W, 14W) are proven fully, safely, dark-materializable end-to-end through the real, unmodified production pipeline.
