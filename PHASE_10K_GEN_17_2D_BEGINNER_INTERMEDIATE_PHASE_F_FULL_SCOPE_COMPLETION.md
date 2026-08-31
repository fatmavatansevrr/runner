# PHASE 10K-GEN.17 — 2D (Beginner + Intermediate) Phase F: Full-Scope Completion

**Parent authority**: `GEN.16` (`TWO_D_PROGRESSION_STAGE_EXPOSURE_PACING_AUTHORITY_FINAL`), implementing `GEN.14`/`GEN.15`'s signed-off Halving mechanism
**Phase type**: IMPLEMENTATION + DEFECT DISCOVERY/FIX + DARK VERIFICATION
**Execution status**: DONE (PARTIAL)
**Final classification**: `TWO_D_PHASE_F_CORE_BINDING_VOLUME_ADAPTATION_COMPLETE_RUNWAY_LONGHORIZON_NOT_ATTEMPTED` — Core workout-content binding, full volume/long-run planning, and the frozen 2-session Adaptation dispatch arm are real, implemented, and dark-verified for both levels; Preparation Runway and LongHorizon are explicitly **not implemented** in this phase (see §7 — a real, disclosed, multi-file scope gap, not a training-methodology blocker).

---

## 0. Mandatory startup — completed

`PHASE_LEDGER.md`/`MASTER_ROADMAP.md` read; `GEN.12`, `GEN.13`, `GEN.14` (and `GEN.15`/`GEN.16`) re-read in full, per instruction. `git log -5`, `git fetch && git diff HEAD origin/main` (in sync at phase start), `git status` clean except the pre-existing unrelated local modifications (`baseline_tmp`, `plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}`) — left untouched throughout, never staged. Next free phase ID confirmed unique: `GEN.17` (ledger row 120).

## 1. Scope actually completed this phase

1. **`ProgressionStageAllocator` lane/week-eligibility mechanism** (`GEN.13`'s escalated blocker, `GEN.14`/`GEN.15`/`GEN.16`'s now-final mechanism) — implemented.
2. **New 2D-specific progression-stage catalog content** for both Beginner and Intermediate, calibrated per `GEN.14`'s exact halving formula — authored and wired.
3. **Full workout-content binding** for 2D, end-to-end, both levels, across the 12/14-week Core horizon range — implemented and dark-verified. The 8-week horizon is a real, disclosed, deterministic capacity gap (§6) — not implemented around.
4. **Volume/long-run planning for 2D** — confirmed fully wired end-to-end (`GEN.12` had already implemented `VolumeSafetyPolicy.Beginner2D`/`Intermediate2D` but could not dark-verify them through a real plan because binding was blocked; that gap is now closed).
5. **Adaptation for 2D** — the `GEN.11 §9`-frozen 2-session dispatch arm (`2/2 Progress, 1/2 Maintain, 0/2 Reduce`) implemented in `NextWindowLoadDecisionPolicy`.
6. **Preparation Runway / LongHorizon for 2D** — **not implemented** in this phase. See §7 for the specific, concrete gaps found and why this was not patched around.

## 2. The mechanism: how `ProgressionStageAllocator` learns which weeks are eligible for a lane

`GEN.14` froze the numeric/policy mechanism (Pattern-A-week-denominated capacity + halved exposure content) but explicitly left the *mechanical* question open: how does the allocator, given only `phaseWeeks` (a list of `GeneratedCatalogWeekSkeleton`), know which weeks structurally carry a slot for a given lane, without duplicating the pattern-resolution logic `CatalogStageToWeekMaterializer.ResolveWeekRoles` already owns?

The answer: it doesn't need to re-derive anything. `CatalogStageToWeekMaterializer` already writes its per-week decision directly onto each `GeneratedCatalogWeekSkeleton.SessionSlots[].StructuralRole` — including 2D's Model B alternation (via `GEN.12`'s `WeeklyPatternRoles`/`PatternPeriodWeeks`) and the TAPER override. Separately, `CatalogWorkoutBinder` already encodes the exact rule for turning a week's structural roles into a lane number: *"the structural ordinal for a repeated structural role within this week... IS the LaneOrdinal"* (its own `structuralOrdinalByRole` comment) — i.e. a week's Nth-occurring `KEY_SESSION` slot (0-based, ordered by `SlotOrderInWeek`) belongs to lane N.

`ProgressionStageAllocator.AllocatePhase` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/ProgressionStageAllocator.cs`) now reuses exactly this same rule and exactly the same per-week data, rather than a second, independently re-derived mechanism:

```csharp
var eligibleWeeks = phaseWeeks
    .Where(w => w.SessionSlots.Count(s => s.StructuralRole == ScheduledProgressionWeek.KeySessionStructuralRole) > laneOrdinal)
    .OrderBy(w => w.WeekNumber)
    .ToList();

var availableWeeks = eligibleWeeks.Count;
```

`availableWeeks` (previously `phaseWeeks.Count` unconditionally) and the contiguous-block layout (previously `phaseWeeks[weekIndex]`, now `eligibleWeeks[weekIndex]`) are the only two lines changed in the entire 584-line allocator. Every other line — compression, extension, fallback, eligibility, trace — is untouched, per `GEN.14 §4`'s explicit instruction that `ProgressionPhaseCapacityInsufficientException`'s fail-closed behavior is left completely alone.

**Zero-delta by construction**: for every pre-`GEN.12` frequency (no `WeeklyPatternRoles`), every declared lane's role is structurally present in 100% of a phase's weeks (`GEN.13 §2` established this by direct inspection of every existing progression document — no prior case of partial lane/week coverage exists anywhere in this codebase before 2D). `eligibleWeeks` is therefore always identical, in the same order, to `phaseWeeks` for every already-shipped candidate — this is byte-identical to pre-`GEN.17` behavior, not merely "usually" identical.

## 3. A second, sibling hardcode found and fixed by the same class of generalization

Wiring the new content through the real pipeline (rather than only unit-testing the allocator in isolation) surfaced a second real defect, of the exact same class `GEN.12 §3` found three of: `GeneratedCatalogStageScheduleValidator` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/GeneratedCatalogStageScheduleValidator.cs`) independently re-asserted the same "every lane covers every skeleton week" assumption the allocator itself used to make (`laneGroup.Count() != skeleton.Weeks.Count`), and immediately failed every valid 2D schedule the corrected allocator produced (`"Lane 0: total week count mismatch: schedule has 7, skeleton has 12"` for a 12-week Beginner plan). This was found by running the real, full binding pipeline end-to-end, not by inspection alone — reproducing the exact discovery discipline `GEN.12` itself used.

**Fix**: the validator's per-lane week-count check now compares against the same `eligibleWeekCount` computation (weeks with more than `laneOrdinal` `KEY_SESSION`-role slots), not `skeleton.Weeks.Count` unconditionally — the identical mechanical generalization, reusing the identical underlying data, applied to the sibling location that needed it. Zero-delta for the same reason as §2: every pre-`GEN.12` lane's eligible-week count equals `skeleton.Weeks.Count` exactly.

No other hardcoded "every week has a slot for this lane" assumption was found in the binding path itself — `CatalogWorkoutBinder`'s own per-week structural-ordinal lookup (§2) was already correct by construction (it only ever looks up `(WeekNumber, LaneOrdinal)` pairs that actually have a structural slot; a Pattern-B week simply never enters its `StageControlled` branch for lane 0), which is why `CatalogWorkoutBindingLaneCountMismatchException` was firing correctly before this phase (per `GEN.12 §6`) and needed no change.

## 3a. A third defect, found only by the full regression run: synthetic-skeleton zero-delta break

Running the full `RunningApp.IntegrationTests` regression (not just the new/targeted dark tests) surfaced a real, genuine zero-delta violation the targeted test runs in §5/§8/§9 did not exercise: `Freq6D4DSplitADualKeyLaneStageBindingTests` (21 pre-existing tests exercising `ProgressionStageAllocator` directly, in isolation from the real materializer) constructs its own synthetic `GeneratedCatalogWeekSkeleton` instances with `SessionSlots = Array.Empty<GeneratedCatalogSessionSlotSkeleton>()` — a deliberate, pre-existing test-fixture shortcut, since the allocator's original design never needed slot-level data, only `phaseWeeks.Count`. §2's eligibility filter (`SessionSlots.Count(s => s.StructuralRole == KEY_SESSION) > laneOrdinal`) evaluates `0 > laneOrdinal` for every such synthetic week — false for every lane — so every week became ineligible, and all 21 tests failed with `ProgressionPhaseCapacityInsufficientException ("...0 available week(s)")`.

**Root cause, precisely**: an *empty* `SessionSlots` list (no structural data supplied at all) is a genuinely different case from a *non-empty* `SessionSlots` list that happens to contain zero `KEY_SESSION` entries (2D's real Pattern-B week, which always has exactly `DaysPerWeek` real slots — `CatalogStageToWeekMaterializer.BuildWeeks` never produces an empty slot list for any real skeleton). The fix distinguishes them explicitly: a week with `SessionSlots.Count == 0` is treated as eligible (falls back to the pre-`GEN.17` unconditional-inclusion default), preserving every caller — production or test — that never populates structural slot data; only a week with real, non-empty slots that genuinely lacks the lane's role is excluded. Applied identically to both `ProgressionStageAllocator.AllocatePhase` and `GeneratedCatalogStageScheduleValidator` (the same sibling pair from §3).

This is the third and final real defect found in this phase, all in the same "reused eligibility filter didn't account for every real caller shape" family — found progressively, each by running a wider slice of the real test suite (targeted dark tests, then the specific existing dual-KEY suite, then the full regression), not assumed correct after the narrower checks passed. Re-verified: `Freq6D4DSplitADualKeyLaneStageBindingTests` 21/21 pass after the fix; every test in §5/§8/§9/§11 re-run and still passes (the fix is additive on top of the already-correct real-skeleton case).

## 4. New 2D-specific progression-stage catalog content

Per `GEN.14`'s frozen derivation rule (`2D_MinimumExposures(stage) = ceil(WeeklyCadence_MinimumExposures(stage) / 2)`, same for maximum, applied to each level's own existing weekly-cadence content, never cross-level, `ceil` never rounding a floor to zero):

**Source content confirmed before computing anything** (not guessed): `TEN_K__2D__BEGINNER`/`TEN_K__2D__INTERMEDIATE` both reuse `TEN_K_MASTER v11`, which (per `GEN.12 §4`) is itself based on the single-lane `v6` lineage — and `TEN_K_MASTER v6` (the same master template `TEN_K__4D__BEGINNER`/`TEN_K__3D__INTERMEDIATE` already use) references `workoutProgression = TEN_K_WORKOUT_PROGRESSION_V1 v5` (`plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v5.json`). This single-lane v5 document is the one and only source of weekly-cadence stage content for **both** levels' single-lane lineage — Beginner and Intermediate single-KEY combos have never had separate weekly-cadence progression content (level differentiation for this lineage happens via `progressionModifier`/eligible-workouts/dose-multiplier, not via different `MinimumExposures`/`MaximumExposures` numbers). Halving this one shared source independently for "Beginner's own" and "Intermediate's own" (per `GEN.14`'s instruction) therefore produces identical numeric output for both levels here — a direct, confirmed consequence of the shared source, not a shortcut taken by this phase.

**New document**: `plan-catalog/catalog/workout-progressions/ten-k-workout-progression-2d.v1.json`, key `TEN_K_WORKOUT_PROGRESSION_2D_V1` v1:

| Phase | Stage | v5 weekly min/max | 2D halved min/max (`ceil(x/2)`) |
|---|---|---|---|
| FOUNDATION | FOUNDATION_EASY_BASE | 3 / 6 | 2 / 3 |
| BUILD | FARTLEK_INTRO | 1 / 2 | 1 / 1 |
| BUILD | THRESHOLD_INTRO | 2 / 4 | 1 / 2 |
| RACE_SPECIFIC | TEN_K_SPECIFIC_INTRO | 1 / 2 | 1 / 1 |
| RACE_SPECIFIC | GOAL_PACE_REHEARSAL | 1 / 2 | 1 / 1 |
| RACE_SPECIFIC | CURRENT_FITNESS_SPECIFIC_REHEARSAL (fallback target) | 1 / 1 | 1 / 1 |
| TAPER | TAPER_SHARPEN | 1 / 2 | 1 / 1 |

Every other field (`workoutCandidates`, `compressionBehavior`, `extensionBehavior`, `requires`, `fallbackStageKey`) is copied verbatim from v5 — only the two exposure numbers were re-derived, exactly as `GEN.14` specified.

**Wiring**: `plan-catalog/catalog/templates/ten-k-master.v11.json`'s `workoutProgression` reference was updated from `TEN_K_WORKOUT_PROGRESSION_V1 v5` to `TEN_K_WORKOUT_PROGRESSION_2D_V1 v1`. Confirmed via direct search that `TEN_K_MASTER v11` is referenced by **only** the two 2D combination documents (`ten-k-2d-beginner.v1.json`, `ten-k-2d-intermediate.v1.json`) — no other combination anywhere in the catalog uses `v11`, so this edit-in-place is safe and affects nothing else. `PlanCatalog.Tests` (1510/1510, including the real domain-graph/publish-pipeline governance suite) confirms this.

## 5. Full binding pipeline: real, dark-verified end-to-end for 12/14-week horizons

`Gen17TwoDayWorkoutBindingDarkVerificationTests` (`backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Binding/`), 13 tests, against the real `TEN_K__2D__BEGINNER v1`/`TEN_K__2D__INTERMEDIATE v1` candidates via the real, unmodified `DynamicCoreWorkoutBindingOrchestrator` (skeleton → progression loading → stage allocation → calendar → binding → output validation, every step's own existing validator reused unmodified):

- **12-week and 14-week Core horizons, both levels, both `GOAL_PACE_REHEARSAL` eligibility branches (`REALISTIC` and fallback)**: every session slot resolves to a non-null, versioned `WorkoutDefinitionKey`; `LONG_RUN` count always equals `targetWeekCount`; `KEY_SESSION` + `EASY_SUPPORT` count always equals `targetWeekCount` (the two roles partition every non-`LONG_RUN` slot, matching the real Pattern-A/B alternation). `TAPER_SHARPEN`'s identity remains `EASY_STANDARD` (matching the existing 4D pilot's own AUD-507/AUD-508 invariant).
- **Zero-delta**: `TEN_K__6D__INTERMEDIATE` (the real, already-`PUBLICLY_ACTIVE` dual-KEY candidate) binds its full 6-slot/week, 12-week plan exactly as before — the allocator/validator changes in §2/§3 produce no different output for a 100%-lane-coverage candidate.

## 6. Disclosed, real capacity gap: the 8-week Core minimum horizon

Confirmed empirically (not merely by inspection) via `Gen17TwoDayWorkoutBindingDarkVerificationTests.BindAsync_RealTwoDayCandidate_EightWeeks_FailsClosed_RaceSpecificCapacityInsufficient`: at `targetWeekCount=8` (`TEN_K_MASTER`'s own catalog-declared Core minimum), the phase allocator (`CatalogPhaseAllocationResolver`, unmodified) compresses every phase to its own minimum-weeks bound: `FOUNDATION=2, BUILD=3, RACE_SPECIFIC=2, TAPER=1`. Given the frozen global odd/even week-ordinal pattern (`GEN.11 §1/§11` — week 1,3,5,7 = Pattern A), `RACE_SPECIFIC` lands on weeks 6-7 (even, odd) — exactly **1** real Pattern-A week. `RACE_SPECIFIC`'s two top-level stages (`TEN_K_SPECIFIC_INTRO`, `GOAL_PACE_REHEARSAL`) each halve to a minimum of 1 exposure (§4's table), for a combined minimum of **2** — one more than the 1 available Pattern-A week. Neither stage has compression headroom left to close this 1-week deficit: `TEN_K_SPECIFIC_INTRO` is already floored to its minimum by halving (headroom `1-1=0`), and `GOAL_PACE_REHEARSAL` is `Protected` (never compressible, by `GEN.7`'s own frozen semantics, unchanged here). The allocator therefore fails closed with `ProgressionPhaseCapacityInsufficientException`, exactly as designed — this is the guard doing its job, not a bug.

This is a **direct, deterministic, arithmetic consequence** of combining `GEN.14`'s frozen halving formula with `TEN_K_MASTER`'s existing (frozen, unrelated to 2D) 8-week Core-minimum phase-allocation bounds and `RACE_SPECIFIC`'s existing (also frozen, unrelated to 2D) `GOAL_PACE_REHEARSAL` Protected/FixedExposure classification. It affects **both levels identically** (the shared v5 source, per §4). It was not patched around: doing so would require inventing new authority in one of three places this phase has no standing to invent — reclassifying `GOAL_PACE_REHEARSAL`'s compression behavior (a `GEN.7`-frozen semantic), changing `RACE_SPECIFIC`'s minimum-week bound (a `TEN_K_MASTER`-frozen, cross-frequency structural constant), or deviating from `GEN.14`'s exact halving formula (now final, non-provisional authority per `GEN.16`). **12-week and 14-week horizons are fully supported and dark-verified; the 8-week Core minimum is not currently reachable for 2D at either level, and is disclosed here rather than silently left to surface as a runtime exception with no explanation.**

## 7. Preparation Runway / LongHorizon — not implemented, disclosed honestly

Per this phase's own instruction ("if real implementation surfaces a genuine new blocker... disclose it and classify DONE (PARTIAL)"), Runway and LongHorizon were investigated for real, concrete gaps before any implementation was attempted, rather than assumed trivial because Core now works:

- **`LongHorizonStructuralMaterializer.MaterializeAsync`** (`backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/LongHorizonStructuralMaterializer.cs:113`) has an explicit, hard `daysPerWeek is not (3 or 4 or 5 or 6) → throw` gate — `2` is not admitted. This is a real, structural admission gate, not a soft default; widening it safely requires confirming the General Endurance (GE) structural selector's behavior at `easySupportCount = daysPerWeek - 2 = 0` (line 133), which no test in this codebase currently exercises for any frequency, and which this phase did not have time to design and dark-verify to the standard the rest of this engagement holds itself to.
- **`TenKPreparationRunwayNumericPolicyFactory.Build(candidate)`** (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayNumericMaterialization/TenKPreparationRunwayNumericPolicyFactory.cs:32`) dispatches by exact `(CanonicalDistanceFamily, Level, DaysPerWeek)` identity — there is no `("TEN_K", "NEW"/"INTERMEDIATE", 2)` branch, so a 2D candidate would silently fall through to the generic 4D-shaped default (`VolumeSafetyPolicy.Default`, the 4D missing-readiness constants) rather than `VolumeSafetyPolicy.Beginner2D`/`Intermediate2D` and 2D's own `TwoDayMissingOrZeroReadinessProductIneligibleException` semantics. Adding this branch is small and low-risk in isolation, but wiring it without also closing the `LongHorizonStructuralMaterializer` gate above would produce a policy dispatch with no real caller ever reaching it — this phase chose not to make a partial, untested change to numeric-policy dispatch code with no corresponding dark verification, consistent with this engagement's own standard that new code paths are dark-verified before being left in place.
- `LongHorizonCompositionDecision` further hardcodes `PreparationRunwayWeeks != 8 → throw` and `CoreWeeks != 12 → throw` (fixed values, not the dynamic 8-14 Core range) — these are pre-existing, cross-frequency LongHorizon structural constants unrelated to 2D, but they mean a genuine LongHorizon 2D implementation must be verified against exactly this fixed 8+12 shape, which was out of this phase's remaining time budget to design, implement, and dark-verify to this engagement's standard (real skeleton generation, real calendar assignment, real rolling-activation persistence, zero-delta proof against every other frequency).

**No new authority was invented to route around any of these gates.** `GEN.11`'s own report explicitly deferred "the real dark-verification matrix" for Runway/LongHorizon to this phase ("Phase D/this phase") while proving only *representability* via a monotonic growth-ratio argument — it did not claim implementation would be small. Having now traced the real gates involved, Runway and LongHorizon for 2D are each a genuinely separate, multi-file, additive implementation exercise (GE structural-selector behavior at zero easy-support count, new Runway numeric-policy dispatch content, widened `daysPerWeek` admission across at least the three gates above, and their own dedicated dark-test suites) comparable in size to what `GEN.12` itself did for Core structural/calendar — not a small follow-on to what this phase already completed. Consistent with `GEN.12`'s own precedent (and this phase's own explicit instruction to stop and disclose rather than push through), this is left **not implemented**, with the exact gate locations recorded above so a dedicated follow-on phase can begin directly from them rather than re-discovering them.

## 8. Adaptation: the frozen 2-session dispatch arm

`GEN.11 §9` froze a 2-session Adaptation dispatch arm (`2/2 Progress, 1/2 Maintain, 0/2 Reduce`), explicitly diverging from `FREQ.6D.23`'s generalized N≥5 count-floor + role-gate model rather than extrapolating it down to N=2 (a 2-session week's single non-`LONG_RUN` slot alternates `KEY_SESSION`/`EASY_SUPPORT` by calendar week under Model B, so there is no stable within-window KEY-vs-EASY role split to gate on the way the N≥5 model's own "N-1" role-gated case does).

Found the real dispatch site: `NextWindowLoadDecisionPolicy.DetermineLoadDecision` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/NextWindowLoadDecisionPolicy.cs`), which switches on `WindowExecutionSummary.ExpectedSessionCount` (already generalized past a single 4-session default by `FREQ.6D.4D`/`FREQ.6D.26`, which added the 5-session and 6-session arms the exact same way). Added `TwoSessionStructuralWeekSize = 2` and `DetermineTwoSessionLoadDecision`, reproducing `GEN.11 §9`'s 3-row table verbatim (no role-gated middle case, matching the divergence rationale above), plus a structural-shape validator mirroring the existing 5/6-session validators. Dispatch is purely additive (a new `case` in an existing `switch` keyed on session count) — every other structural week size's behavior is unchanged.

**Disclosed reachability caveat**: this dispatch is only exercised through the LongHorizon rolling-activation pipeline (`LongHorizonRollingCheckpointRuntime` and siblings), which — per §7 — is not itself wired for 2D `daysPerWeek=2` yet. The dispatch arm itself is implemented, correct per `GEN.11`'s frozen table, and dark-verified in isolation (`Gen17TwoSessionAdaptationDispatchTests`, 6 tests, direct calls to `NextWindowLoadDecisionPolicy.Evaluate`), but has no live 2D caller until LongHorizon's own `daysPerWeek` gates are widened.

## 9. Volume/long-run planning: confirmed, no gap remains for the horizons that bind

`Gen17TwoDayVolumeAndLongRunDarkVerificationTests` (`backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Volume/`), 6 tests, against the real, unmodified `DynamicCoreVolumeAndLongRunOrchestrator` (which itself composes the real binding orchestrator + `CatalogVolumeAndLongRunPlanner`, both already implemented by `GEN.12` but previously unreachable because binding failed):

- 12-week and 14-week horizons, both levels: weekly volume plan and long-run progression both validate; peak volume never exceeds `GEN.11`'s frozen `PeakVolumeBand` (`Beginner [16,22]km`, `Intermediate [20,30]km`); taper week volume is strictly below peak; every week's long-run distance stays within `VolumeSafetyPolicy.Beginner2D`/`Intermediate2D`'s frozen 60% hard-cap long-run share.
- Missing/zero readiness (`RecentWeeklyVolumeKm=0`, `RecentLongestRunKm=0`) correctly throws `TwoDayMissingOrZeroReadinessProductIneligibleException` (wrapped by the orchestrator's own `DynamicCoreVolumeAndLongRunFailedException`), matching `GEN.11 §7`'s frozen no-default policy for both levels.

This closes `GEN.12 §5`'s own disclosed gap ("implemented and unit-reachable, but not yet dark-verified end-to-end... because that requires workout-content binding to succeed first") for every horizon binding now supports.

## 10. Proactive hardcoded-assumption search (item 7 of this phase's scope)

Before touching Adaptation/Runway/LongHorizon code, searched for the "every week has ≥1 KEY_SESSION" class of assumption `GEN.12 §3` found three instances of. Found and fixed one real instance in the binding-adjacent validation path (§3 above — `GeneratedCatalogStageScheduleValidator`). Searched `LongHorizon`/`PreparationRunwayNumericMaterialization`/`PreparationRunwayWeekMaterialization`/`PreparationRunwayOrchestration` directly for the same pattern (`keyCount < 1`, `KeySessionExpectedCount < 1`, hardcoded `daysPerWeek` admit-lists) — found no further instances of the *specific* keyCount-floor bug GEN.12 fixed, but found the three real, disclosed `daysPerWeek`-admission gates recorded in §7, which are the LongHorizon/Runway subsystem's own analogous "this frequency shape isn't representable yet" boundary — correctly a disclosed scope gap, not a silent-assumption defect to patch mechanically.

## 11. Tests added and results

- `Gen17TwoDayWorkoutBindingDarkVerificationTests.cs` — 13 tests (full binding pipeline, 12/14-week horizons both levels both goal-feasibility branches; 8-week capacity-insufficient disclosure; zero-delta for `TEN_K__6D__INTERMEDIATE`). **13/13 pass.**
- `Gen17TwoDayVolumeAndLongRunDarkVerificationTests.cs` — 6 tests (full volume/long-run plan, 12/14-week horizons both levels; missing/zero-readiness product-ineligibility). **6/6 pass.**
- `Gen17TwoSessionAdaptationDispatchTests.cs` — 6 tests (2-session Adaptation dispatch arm, all three outcomes plus safety-flag independence and legacy zero-delta). **6/6 pass.**
- **25 new tests total, 25/25 pass.**

Confirmed unaffected (re-run explicitly, not merely inferred from the full suite): `Gen12TwoDayDarkVerificationTests` (11/11, unchanged — structural/calendar layer untouched by this phase), `DynamicCoreWorkoutBindingOrchestratorTests` + `Freq6D26IntermediateSixDayDarkVerificationTests`-family + `Gen9AdvancedCombinedDarkVerificationTests` (94/94, the existing 4D/6D/Advanced dark suites most likely to detect a regression from the `ProgressionStageAllocator`/`GeneratedCatalogStageScheduleValidator` changes — all pass unchanged).

## 12. Full regression

- `PlanCatalog.Tests`: **1510/1510** pass (including the real domain-graph/publish-pipeline governance suite — confirms the new catalog document and the in-place `ten-k-master.v11.json` edit are valid and affect nothing outside the two 2D combinations).
- `RunningApp.IntegrationTests` (run alone, with no other process contending for the same Postgres instance — an earlier same-session attempt run concurrently with another test process produced 244 spurious failures, root-caused to resource contention, not a real regression, and discarded): **4070 total, 4067 passed, 3 failed, 0 skipped.**
- The 3 failures are **`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks:13)`, the same test at `weeks:14`, and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`** — all three throwing the identical `CatalogSessionPrescriptionInfeasibleException` ("4F.7C allocation minimums cannot be treated as safely satisfied by 4F.8.1 materialization"). **Confirmed pre-existing, not a GEN.17 regression**, by directly checking out a clean `origin/main` worktree (no GEN.17 changes present) and running the same three tests there: all three fail identically, with the exact same exception. This corrects prior phases' "2 durable pre-existing baseline failures" language for this environment — as of this phase's own direct verification, there are **3** distinct pre-existing baseline failures across these 2 test classes (the `Gen4E` class now fails at both `weeks:13` and `weeks:14`, not only `weeks:13` as earlier phases recorded), confirmed via direct origin/main comparison rather than assumed identical to an earlier phase's own count.
- **Zero new regressions**: every failure present on a clean `origin/main` checkout is present, identically, with this phase's changes applied; no additional failure appears.
- Debug and Release builds both clean (0 errors) for the full `RunningApp.sln`.

## 13. Explicit constraints — confirmed respected

- **Dark-only**: `V1CatalogPilotIdentityPolicy` untouched. Both 2D candidates remain loaded exclusively via `LoadForInternalDryRunAsync` in every test in this phase — never reachable through public HTTP. No public routing/gate change anywhere.
- **Zero-delta for every already-`PUBLICLY_ACTIVE` frequency**: every code change in this phase (`ProgressionStageAllocator`, `GeneratedCatalogStageScheduleValidator`, `NextWindowLoadDecisionPolicy`) is additive — a new `Where`/`Count` filter that resolves to "every week" for 100%-lane-coverage candidates, or a new `switch` case keyed on session count. Confirmed via re-running the existing 4D/5D/6D/Advanced dark suites (§11) unchanged, not merely by code inspection.
- **No new product/numeric authority beyond `GEN.11`/`GEN.14`/`GEN.15`/`GEN.16`'s frozen values**: the halving formula, the Pattern-A-denominated capacity mechanism, the 55%/60% long-run shares, the `PeakVolumeBand` values, and the 2-session Adaptation table are all direct, unmodified implementations of already-frozen authority. The one genuine new gap found (§7, Runway/LongHorizon `daysPerWeek` admission) was disclosed, not patched around with an invented rule.

## 14. Governance

`PHASE_LEDGER.md` row 120 (`GEN.17`) appended. `MASTER_ROADMAP.md`'s 2D axis paragraphs updated to reflect: Core (structural + binding + volume/long-run + Adaptation dispatch) `COMPLETE_AND_DARK_VERIFIED` for 12/14-week horizons at both levels, with the 8-week Core-minimum horizon explicitly disclosed as not currently supported (§6); Runway/LongHorizon explicitly `NOT_IMPLEMENTED` with the specific gate locations recorded (§7) for a dedicated follow-on phase. Public gates remain closed for both candidates — no public activation in this phase.

**Explicit statement of what remains** (per this phase's own required output):
1. A dedicated Preparation Runway 2D implementation-and-dark-verification phase, starting from the three gate locations in §7 (`LongHorizonStructuralMaterializer:113`'s `daysPerWeek` admit-list, the GE structural selector's untested `easySupportCount=0` case, `TenKPreparationRunwayNumericPolicyFactory`'s missing `(TEN_K, level, 2)` dispatch branches).
2. A dedicated LongHorizon 2D implementation-and-dark-verification phase, following Runway (LongHorizon composes Runway + Core).
3. Optionally, a small, focused decision on whether the disclosed 8-week Core-minimum capacity gap (§6) should be resolved by widening `RACE_SPECIFIC`'s minimum-week bound for 2D specifically, or left as a permanently unsupported horizon for 2D — this is a genuine product question (would require either a `TEN_K_MASTER`-level per-frequency phase-bound override, currently unsupported by the catalog schema, or accepting an 8-week-Core-unavailable-for-2D product constraint) that this phase correctly did not resolve unilaterally.

Not scheduled as Phase IDs here, per this engagement's own established sequencing discipline.
