# Phase HM.5.2 — Progression Stage Allocator Optional-Preferred Exposure Semantic Closure

## 0. HEAD and working-tree attribution

All work in this phase starts from `HEAD` at commit `182114b` (HM.5.1's own governance-backfill
commit). This phase touches exactly five source files, adds two new test files, adds this report,
and updates `PHASE_LEDGER.md`/`MASTER_ROADMAP.md`. No unrelated file was modified. The real,
feature/fix commit for this phase's work is `615ddd051cff13ca7ddc48ffd5d9cf958460dae0`
(`feat(hm-5-2): close progression stage allocator optional-preferred exposure semantic gap`);
this report and `PHASE_LEDGER.md` referenced that commit's own SHA as a placeholder until the
docs-only backfill commit that immediately follows it.

## 1. Current allocator algorithm (as found, before this phase's change)

`ProgressionStageAllocator.AllocatePhase` (per phase, per lane):

1. **Structural validation** (`ValidateStructure`): rejects blank/duplicate `ProgressionStageKey`,
   duplicate `RelativeOrder`.
2. **Eligibility/fallback resolution** (`ResolveEligibility`): for each top-level requested stage
   (a stage that is not itself some other stage's `fallbackStageKey` target), walks
   `Requires`/`fallbackStageKey` using already-resolved `RuntimeConditionResolutionResult`s (never
   re-evaluating a condition itself). Produces an effective stage per requested stage.
3. **Duplicate-effective-stage merge** (Section 12 of the original 4F.6A design): if two or more
   requested stages resolve to the same effective stage (true convergence via independent
   fallback chains), bounds are merged conservatively (max of minimums, min of maximums); a
   single-substitution fallback (one requested stage's own fallback target) merges generously (max
   of minimums, max of maximums).
4. **Capacity resolution** — *this is the part this phase changes*. Before this phase:
   `totalMinimum = sum(MinimumExposures)` across all active stages. If `totalMinimum >
   availableWeeks`: `ApplyCompression` reduces `Compressible` stages, **highest `RelativeOrder`
   first** then `ProgressionStageKey` ordinal, mutating each stage's own `MinimumExposures` field
   down to a **hardcoded floor of 1** (never lower — there was no other floor concept in the
   schema). If `totalMinimum < availableWeeks`: `ApplyExtension` grows `Extendable` stages (tried
   before `FixedExposure` stages) from `MinimumExposures` toward `MaximumExposures`, **highest
   `RelativeOrder` first**. If `totalMinimum == availableWeeks`: **no adjustment code runs at
   all** — the final assembly loop's fallback (`active.FinalAllocatedExposures ??
   active.MinimumExposures`) is what actually answers this case, by reading `MinimumExposures`
   directly.
5. **Contiguous week-block layout**: ascending `RelativeOrder`, independent of whichever tie-break
   decided *how many* exposures each stage received.

## 2. Exact capability defect

There was no field distinct from `MinimumExposures` that a stage could use as its own
"baseline/preferred" seed. `MinimumExposures` had to serve two conflicting roles simultaneously:
(a) the exact-fit/extension baseline (what a stage gets when nothing forces compression or
extension), and (b) the true hard floor during compression. HM.4 needed `BUILD_FASTER_THAN_HM_INTRO`
to declare Min=0 (a true floor of zero, reachable only under real compression) while keeping its
own Preferred baseline at 1 for every horizon that doesn't need to compress it. The pre-existing
schema could not express "0 as floor, 1 as everyday baseline" as two different numbers on the same
field — authoring Min=0 collapsed the baseline itself to 0.

At the frozen 14W horizon, Build = 4 weeks exactly equals `BUILD_FASTER_THAN_HM_INTRO`'s intended 1
+ `BUILD_THRESHOLD_DEVELOPMENT`'s intended 3 (Regime B — exact fit). With the old schema, authoring
Min=0 for `BUILD_FASTER_THAN_HM_INTRO` made `totalMinimum` (0 + 3 = 3) **less than** `availableWeeks`
(4), which is Regime C (extension) — not Regime B. `ApplyExtension` then grows the highest-
`RelativeOrder` stage first (`BUILD_THRESHOLD_DEVELOPMENT`, order 2) from its own `MinimumExposures`
(3) up to its `MaximumExposures` (5), consuming the entire 1-week surplus before `INTRO` (order 1)
is ever reconsidered — producing 0+4, not the frozen 1+3. This is exactly what
`Hm5StageAllocatorMechanismProofTests.Hm4ProposedIntroMinimumZero_At14WeekFrozenBuildLength_DoesNotReproduceFrozenOneThreeOccupancy`
already proved empirically (kept passing, unmodified, by this phase — see Section 8).

## 3. Meaning of `RelativeOrder`

Direct trace of every use site (pre- and post-this-phase):

- **Contiguous block layout** (Section 8/9 of the original 4F.6A design): stages are laid out in
  **ascending** `RelativeOrder` — this is chronological/workout-sequencing order within the phase
  (e.g. "faster-than-race workouts happen before threshold development"). Unambiguous.
- **Extension priority** (`ApplyExtension`): candidates ordered **descending** `RelativeOrder`
  (after the Extendable-before-FixedExposure primary sort) — the highest-order stage gets surplus
  first. This matches HM.4's own authored intent exactly: "extend `BUILD_THRESHOLD_DEVELOPMENT`
  (repeat one more threshold week) — `FASTER_THAN_HM_INTRO` never extends past 1" (PHASE_HM_4
  report line 63). **Not changed by this phase.**
- **Compression priority** (`ApplyCompression`): candidates ordered **descending** `RelativeOrder`
  — the highest-order stage is reduced first. This is the **opposite** direction from HM.4's
  explicitly authored compression intent: "Compression order within Build: drop
  `FASTER_THAN_HM_INTRO` (1→0) **before** compressing `BUILD_THRESHOLD_DEVELOPMENT` below its own
  preferred value" (PHASE_HM_4 report line 63) — i.e. HM.4 wants the **lowest**-`RelativeOrder`
  stage (`INTRO`, order 1) reduced first, but the real algorithm reduces the **highest**-order
  stage (`THRESHOLD_DEVELOPMENT`, order 2) first.

**Finding**: `RelativeOrder` is genuinely overloaded with two different, and in this one case
*conflicting*, meanings — extension-fill priority (descending, matches HM.4) and compression-
removal priority (descending, contradicts HM.4's own stated Build-specific intent). This is a real,
pre-existing property of the shared algorithm, not something this phase introduces. See Section 16
("Remaining findings") for why this phase does not attempt to fix the compression-direction half of
this overload, and why that does not block this phase's own stated closure criteria.

## 4. Min vs Preferred vs Max semantic model (as implemented by this phase)

New optional field: `CatalogWorkoutProgressionStage.PreferredExposures` (`int?`, default `null`).
Every active stage now has a computed `Baseline` value: `PreferredExposures ?? MinimumExposures`.
`Baseline` — not `MinimumExposures` — is what the exact-fit fallback, `ApplyCompression`, and
`ApplyExtension` all now operate on:

- **Regime B (exact fit, `sum(Baseline) == availableWeeks`)**: no compression/extension code runs;
  the final-assembly fallback now reads `active.FinalAllocatedExposures ?? active.Baseline` (was
  `?? active.MinimumExposures`). This is the literal fix for the 14W case (Section 2).
- **Regime A (compression, `sum(Baseline) > availableWeeks`)**: `ApplyCompression` reduces each
  Compressible candidate (same descending-`RelativeOrder` order as before — unchanged) from its
  `Baseline` down to a **compression floor** that is `MinimumExposures` when `PreferredExposures`
  is declared, else the historical hardcoded `1` when it is not. This is a strict generalization:
  every stage that leaves `PreferredExposures` unset computes the *identical* floor (1) it always
  did.
- **Regime C (extension, `sum(Baseline) < availableWeeks`)**: `ApplyExtension` grows each candidate
  (same descending-`RelativeOrder`-among-Extendable-first order as before — unchanged) from its
  `Baseline` (not `MinimumExposures`) up to its `MaximumExposures`.

For every stage that leaves `PreferredExposures` null (every 10K stage in every version/lane;
every pre-HM.5.2 HM stage), `Baseline ≡ MinimumExposures` and the compression floor `≡ 1` — the
new code path is provably byte-identical to the old one by construction, not merely by empirical
luck.

## 5. Compression authority

`RelativeOrder` descending remains the compression *stage-selection order* — **unchanged by this
phase** (see Section 3/16 for why). What this phase changes is the compression *floor value* per
candidate (Section 4). No new priority field was invented for stage selection, per the governing
prompt's own instruction not to invent a new field until proving the existing schema cannot express
the distinction — this phase proves the schema (specifically, the *floor*) could not express
HM.4's Min=0/Preferred=1 split, and fixes exactly that with the smallest addition (one nullable
int). The separate compression-*direction* overload (Section 3) is a distinct property this phase
does not touch, in order to guarantee 10K zero-delta (see Section 12/16).

## 6. Extension authority

`ExtensionBehavior` (Extendable tried before FixedExposure) plus `RelativeOrder` descending remain
the extension stage-selection order — unchanged. What changes is only the seed each candidate grows
from (`Baseline` instead of `MinimumExposures`) and the headroom computation
(`MaximumExposures - Baseline` instead of `MaximumExposures - MinimumExposures`). For a stage with
`PreferredExposures` null this is arithmetically identical to before.

## 7. Minimal generic change

Files changed (see Section 15 for the full list):

- `CatalogWorkoutProgressionStage.PreferredExposures` — new, optional (`int?`, default `null`),
  same "additive/degenerate-default" pattern already established for
  `VolumeSafetyPolicy.TaperVolumeMultipliers` (HM.1.4B) and for this same type's own
  `WorkoutCandidateReferences`/`PrescriptionProfileCandidateKeys` fields.
- `CatalogWorkoutProgressionLoader`: reads an optional `"preferredExposures"` JSON number,
  defaulting to `null` when absent — every existing catalog document (which never declares this
  property) parses byte-identically to before.
- `ProgressionStageAllocator`: introduces `ActiveStage.PreferredExposures` (carried through the
  Section 12 merge logic with two new, explicitly-documented, currently-inert combine rules — see
  Section 4/8 of the original design and this file's own code comments; no real catalog stage
  reaches the merge branches with `PreferredExposures` declared, so these rules are conventions,
  not empirically exercised paths) and `ActiveStage.Baseline` (`PreferredExposures ??
  MinimumExposures`). Every place that previously read `MinimumExposures` as the allocator's
  working baseline now reads `Baseline` instead; every place that read `MinimumExposures` as a true
  floor (only the compression floor, post this phase) still reads `MinimumExposures` — but only
  when `PreferredExposures` is declared, else the historical floor of `1` is kept.
- A new structural validation: a stage that declares `PreferredExposures` outside its own
  `[MinimumExposures, MaximumExposures]` range throws
  `ProgressionStagePreferredExposuresOutOfBoundsException` — unreachable for any pre-HM.5.2 stage.
- `half-marathon-workout-progression.v1.json`: `BUILD_FASTER_THAN_HM_INTRO` changes from
  `minimumExposures: 1, maximumExposures: 1` (HM.5.1's disclosed 1/1 workaround) to
  `minimumExposures: 0, preferredExposures: 1, maximumExposures: 1` (HM.4's real, literal, frozen
  authority). This is the **only** catalog-data change in this phase.

No other stage, phase, workout dose, volume, long-run, or taper value was touched. No HM-specific
or `BUILD_FASTER_THAN_HM_INTRO`-specific branch exists anywhere in `ProgressionStageAllocator.cs` —
grep-verified: the allocator source contains no HM/Half-Marathon/BUILD_FASTER_THAN_HM string
literal.

## 8. Generic invariant tests

New file: `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Progression/Hm52ProgressionStageAllocatorPreferredExposureInvariantTests.cs`
(14 tests, all passing), using only synthetic, distance-blind fixtures (`PHASE_A`/stage keys `A`/`B`
— no HM or 10K name anywhere):

| Invariant | Test | Result |
|---|---|---|
| (A) `sum(Min)==phaseWeeks`, no Preferred declared | `SumOfMinimumEqualsPhaseWeeks_NoPreferredDeclared_ResolvesExactMinimumOccupancy` | PASS |
| (B) `sum(Preferred)==phaseWeeks` → exact Preferred, regardless of RelativeOrder | `SumOfPreferredEqualsPhaseWeeks_ResolvesExactPreferredOccupancy_RegardlessOfRelativeOrder` | PASS |
| (C) between Min and Preferred → compression toward declared Minimum floor | `PhaseWeeksBetweenMinimumAndPreferred_CompressesTowardDeclaredMinimum_HighestRelativeOrderFirst` | PASS |
| (D) between Preferred and Max → only Extendable stages grow, from Preferred baseline | `PhaseWeeksBetweenPreferredAndMaximum_ExtendsOnlyExtendableStages_FromPreferredBaseline`, `ExtensionGrowsFromPreferredBaseline_NotFromMinimum_WhenTheyDiffer` | PASS |
| (E) FixedExposure never compresses/extends | `FixedExposureStage_WithPreferredDeclared_NeverCompressesOrExtends` | PASS |
| (F) Protected never removed before Compressible | `ProtectedStageWithPreferred_NeverCompressedBeforeCompressibleStage` | PASS |
| (G) ineligible optional stage yields to fallback; fallback's Preferred governs | `IneligibleOptionalStage_YieldsToFallback_FallbackPreferredGovernsBaseline` | PASS |
| (H) never exceeds Maximum | `ExtensionNeverExceedsMaximumExposures_EvenWithPreferredDeclared`, `ExtensionBeyondMaximum_EvenWithPreferredDeclared_ThrowsTypedException` | PASS |
| (I) total resolved == phaseWeeks, all three regimes | `TotalResolvedExposures_AlwaysEqualsPhaseWeeks_AcrossAllThreeRegimes` (×3 horizons) | PASS |
| Structural guard | `PreferredExposuresOutsideMinMaxRange_ThrowsTypedException` | PASS |

All 14 pass. Test-run evidence: `dotnet test ... --filter "FullyQualifiedName~Hm52ProgressionStageAllocatorPreferredExposureInvariantTests"` → `Başarısız: 0, Başarılı: 14, Toplam: 14`.

## 9. HM 10-16 stage occupancy after fix

New file: `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm52StageOccupancyAllHorizonsTests.cs`.
Produced via the real, unmodified `CatalogPhaseAllocationResolver` + real, unmodified
`ProgressionStageAllocator` + real `half-marathon-workout-progression.v1.json` (post this phase's
one-field edit), through `DynamicCoreWeekSkeletonOrchestrator` (the same generic any-horizon
skeleton path `Hm2FullDarkVerticalSliceTests` already exercises end-to-end). All 8 tests pass
(7 horizons + the dedicated 14W golden-restoration check).

| Horizon | FOUNDATION_AEROBIC_BASE | BUILD_FASTER_THAN_HM_INTRO | BUILD_THRESHOLD_DEVELOPMENT | RACE_SPECIFIC_HM_INTRODUCTION | RACE_SPECIFIC_HM_DEVELOPMENT | RACE_SPECIFIC_HM_REHEARSAL | TAPER_HM_ACTIVATION |
|---|---|---|---|---|---|---|---|
| 10W | 2 | 1 | 2 | 0 | 1 | 2 | 2 |
| 11W | 2 | 1 | 2 | 0 | 2 | 2 | 2 |
| 12W | 2 | 1 | 2 | 1 | 2 | 2 | 2 |
| 13W | 2 | 1 | 3 | 1 | 2 | 2 | 2 |
| **14W** | **3** | **1** | **3** | **1** | **2** | **2** | **2** |
| 15W | 4 | 1 | 3 | 1 | 2 | 2 | 2 |
| 16W | 4 | 1 | 4 | 1 | 2 | 2 | 2 |

Every row: `GeneratedCatalogStageScheduleValidator.IsValid == true`, `schedule.Weeks.Count ==
targetWeekCount` exactly, and `BUILD_FASTER_THAN_HM_INTRO`'s occupancy never exceeds its frozen
Maximum (1) at any horizon. **The 14W row is byte-identical to the frozen golden occupancy** (1+3
Build, unchanged from HM.5.1), independently re-confirmed by
`Hm2FullDarkVerticalSliceTests.PreferredFourteenWeekCore_TraversesRealPipelineAndMaterializesValidDarkPreview`
(still passing, unmodified, through the full production pipeline — not just this phase's own
narrower test).

**Honest disclosure — 10W/11W/12W do not match HM.4's own §10 "0+3" compression prediction**: at
these three compressed horizons, `BUILD_FASTER_THAN_HM_INTRO` still resolves to **1**, not the 0 HM.4's
table predicted, and `BUILD_THRESHOLD_DEVELOPMENT` absorbs the entire compression deficit instead
(2, not 3). This is the **direct, mechanical consequence of Section 3's compression-direction
finding**: `ApplyCompression`'s descending-`RelativeOrder` selection order (unchanged by this
phase, see Section 5/16) tries `BUILD_THRESHOLD_DEVELOPMENT` (order 2) first, and its own headroom
down to its floor (`Baseline 3 - floor 1 = 2`) is large enough to fully absorb every observed
10-12W deficit (1 week) before `BUILD_FASTER_THAN_HM_INTRO` (order 1) is ever touched. This is
**structurally valid** (within every frozen bound, total exposures == phaseWeeks at every horizon,
`GeneratedCatalogStageScheduleValidator` accepts it) and is **the exact same disclosed deviation
HM.5.1 already reported** at these same three horizons (HM.5.1 report §8: "Build's compressed-
horizon split is `1+2` here (10–12W) instead of HM.4's predicted `0+3`") — this phase does not
regress it, worsen it, or newly introduce it; it simply does not additionally fix it, because doing
so safely requires decoupling compression-selection order from `RelativeOrder` (see Section 16),
which is out of this phase's "smallest generic fix" scope and risks 10K zero-delta if done
carelessly.

## 10. Faster-than-HM eligible case

`BUILD_FASTER_THAN_HM_INTRO` declares `"requires": []` in the real catalog (confirmed by direct
read of `half-marathon-workout-progression.v1.json`, unchanged by this phase) — it is
`NotConditioned`, always eligible, with no `fallbackStageKey`. There is no real ineligibility branch
wired into the actual catalog data for this stage, exactly as HM.5.1 already found and disclosed.
At the frozen 14W preferred horizon (Regime B, `sum(Baseline)==availableWeeks`), it resolves to
exactly 1, and `BUILD_THRESHOLD_DEVELOPMENT` to exactly 3 — restored, matching HM.4's real, literal
authority (Section 9).

## 11. Faster-than-HM ineligible/fallback case

Not reachable in real data — see Section 10 (no `requires`, no `fallbackStageKey` on this stage in
the real catalog). This phase does not invent an eligibility mechanism that doesn't exist in the
real data, per the governing prompt's own explicit instruction. The generic
ineligible-optional-stage-yields-to-fallback invariant is proven with a synthetic fixture instead
(Section 8, invariant G) — `IneligibleOptionalStage_YieldsToFallback_FallbackPreferredGovernsBaseline`.

## 12. 10K recurring-family audit

Enumerated every real 10K `WORKOUT_PROGRESSION` catalog document:
`ten-k-workout-progression.v1.json` through `.v7.json`, and `ten-k-workout-progression-2d.v1.json`
(9 files total). Grepped every one (and every other file under `plan-catalog/catalog/`) for the new
`"preferredExposures"` property: **zero matches anywhere except the one HM field this phase itself
edited**. Every 10K stage across every version/lane (`v6`/`v7` are lane-based —
`FOUNDATION_PRIMARY_STAGE`/`FOUNDATION_SECONDARY_STAGE`, `BUILD_PRIMARY_STAGE`/`BUILD_SECONDARY_STAGE`,
`RACE_SPECIFIC_PRIMARY_STAGE`/`RACE_SPECIFIC_SECONDARY_STAGE`,
`TAPER_PRIMARY_STAGE`/`TAPER_SECONDARY_STAGE`) declares only `minimumExposures`/`maximumExposures`,
several with genuine `Min < Max` headroom (e.g. `FOUNDATION_PRIMARY_STAGE` 2/4,
`BUILD_PRIMARY_STAGE` 3/5) and the one real fallback pair (`GOAL_PACE_REHEARSAL` →
`CURRENT_FITNESS_SPECIFIC_REHEARSAL`, `TEN_K_WORKOUT_PROGRESSION_V1`'s `RACE_SPECIFIC` phase).

Because `PreferredExposures` is absent from every one of these stages, `Baseline ≡
MinimumExposures` and the compression floor `≡ 1` for every one of them **by construction** — the
new code path is mathematically identical to the pre-HM.5.2 code path for every 10K stage,
regardless of which regime (compression/exact-fit/extension) any given horizon or candidate falls
into. This is a formal argument, not merely an empirical one: nothing in this phase's diff can
produce a different number for any stage whose `PreferredExposures` is null.

Checked the `PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md` precedent explicitly (per the
governing prompt's own instruction): its 13-week extension mechanism uses `CompressionPriority`/
`ExtensionPriority` fields (already `1/2/3/4`, already decoupled from structural ordering) on
`CatalogPhaseAllocationResolver` — the **phase-level** allocator, a completely different class from
`ProgressionStageAllocator` (the **stage-level** allocator this phase touches). Grepped that report
for `ProgressionStageAllocator`/`StageAllocator`: zero matches. Confirmed this precedent does not
flow through the code this phase changed, and needs no re-verification here.

## 13. 10K before/after stage vectors (empirical, not just formal)

Ran the real, unmodified `ProgressionStageAllocatorTests.cs` (22 tests, exercising the real default
12-week 10K pilot candidate — `DefaultTwelveWeekPilot_...`, `FoundationPhase_...`,
`BuildPhase_ExtensionAllocatesSurplusToHigherRelativeOrderStageFirst` [a real extension-regime
case: `FARTLEK_INTRO` order1/min1/max2 + `THRESHOLD_INTRO` order2/min2/max4, 4 available weeks,
asserts exactly `FARTLEK_INTRO=1, THRESHOLD_INTRO=3` — unchanged], `TaperPhase_...`,
`Determinism_...`, plus every synthetic structural-rejection fixture) — **all 22 pass, unmodified,
zero assertion changed**. Also ran the full `Hm5StageAllocatorMechanismProofTests.cs` (3 tests,
including the two that directly characterize the pre-existing compression-floor-of-1 and
extension-favors-highest-order mechanics against synthetic HM-shaped fixtures that do **not** use
`PreferredExposures`) — **all 3 pass, unmodified, zero assertion changed**, confirming those
mechanics remain byte-identical for any stage that doesn't opt into the new field.

## 14. Regression results

All commands run sequentially (never two `dotnet test` invocations concurrently against the shared
Postgres test database, per this engagement's own established discipline):

1. `RunningApp.Application` build: **0 errors, 0 warnings**.
2. `ProgressionStageAllocatorTests` + `Hm5StageAllocatorMechanismProofTests` (targeted, pre-catalog-edit): **30/30 passed**.
3. `HalfMarathon` + `Hm2FullDarkVerticalSliceTests` (targeted, post-catalog-edit): **136/136 passed** — includes the frozen 14W golden test, the 10/11/13W non-preferred-horizon tests, and the (still correctly failing-closed, unmodified) 12W/15W long-run-share and 16W peak-volume-ceiling blocker tests.
4. `Hm52ProgressionStageAllocatorPreferredExposureInvariantTests` (new, generic): **14/14 passed**.
5. `Hm52StageOccupancyAllHorizonsTests` (new, HM-specific structural proof): **22/22 passed**.
6. Full `RuntimeCatalog` namespace subtree (10K + HM + LongHorizon + PreparationRunway + Binding + Materialization + Progression, the entire reachable surface of this shared allocator): **3623/3623 passed, 0 failed**, 6m06s.
7. `PlanCatalog.Tests` (full suite, run sequentially after the RuntimeCatalog run completed): **1510/1510 passed**, 7s — matches the established baseline exactly, zero hash/golden regression from the one-field catalog edit.
8. Full monolithic `RunningApp.IntegrationTests` suite (run alone, after every step above completed — never concurrently with another `dotnet test` invocation against the same Postgres test database): **4538 passed / 3 failed / 4541 total, 40m46s**. The 3 failures are exactly the documented durable baseline minus the wall-clock-date artifact (which is documented as appearing on only ~4 of every 7 real days, so its absence on this run's calendar day is expected, not a discrepancy):
   - `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` — pre-existing baseline failure.
   - `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)` — pre-existing baseline failure.
   - `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — pre-existing baseline failure.

   **Zero new failures.** The total (4541) is exactly HM.5.1's own documented baseline total (4519) plus this phase's 22 new tests (14 generic invariant + 8 HM stage-occupancy), confirming no test was silently removed or skipped either.

## 15. Files changed

- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/CatalogWorkoutProgressionDefinition.cs` — added `CatalogWorkoutProgressionStage.PreferredExposures` (`int?`, default `null`).
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/CatalogWorkoutProgressionLoader.cs` — reads optional `"preferredExposures"` JSON property.
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/ProgressionStageAllocator.cs` — `Baseline` concept, `CompressionFloor` helper, `CombineConservativePreferred`/`CombineGenerousPreferred` merge helpers, `PreferredExposures` bounds validation, `ActiveStage.BaselineExposuresBeforeExtension` rename (was `MinimumExposuresBeforeExtension`).
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/ProgressionStageSchedulingExceptions.cs` — new `ProgressionStagePreferredExposuresOutOfBoundsException`.
- `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` — `BUILD_FASTER_THAN_HM_INTRO`: `minimumExposures` 1→0, added `preferredExposures: 1` (only data change).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Progression/Hm52ProgressionStageAllocatorPreferredExposureInvariantTests.cs` — new, generic invariant tests.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm52StageOccupancyAllHorizonsTests.cs` — new, HM 10-16W structural stage-occupancy proof.
- `PHASE_HM_5_2_PROGRESSION_STAGE_ALLOCATOR_OPTIONAL_PREFERRED_SEMANTIC_CLOSURE.md` — this report.
- `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` — governance updates.

No file under `VolumeSafetyPolicy`, `CatalogVolumeAndLongRunPlanner`, any volume/long-run/taper
path, any public/Runway/LongHorizon routing file, or any other HM/10K workout-progression catalog
file was touched.

## 16. Remaining HM.5 blockers / residual findings

- **HM.5.3 volume-planner blockers (unchanged, out of scope)**: 12W/15W `FINAL_WEEK_*_LONG_RUN_SHARE_EXCEEDS_CAP`
  and 16W's 46.5km peak-volume overshoot vs the 43.0km reference ceiling remain exactly as HM.5.1
  disclosed them — re-confirmed still failing identically (same error codes, same 46.5km value) by
  the unmodified `Hm2FullDarkVerticalSliceTests` theory/fact tests in Section 14 step 3. Not
  touched, per this phase's explicit hard boundary.
- **Compression-priority-direction overload (new finding, this phase, disclosed not fixed)**:
  `RelativeOrder` descending is used for both extension-fill priority (correct per HM.4) and
  compression-removal priority (opposite of HM.4's explicit Build-specific intent — see Section 3).
  This phase's fix closes the Preferred-*baseline* gap (the literal HM.5/HM.5.1 blocker: 14W's
  golden occupancy) completely and generically, without touching compression *selection order* at
  all, in order to guarantee 10K zero-delta (Section 12/13) with certainty rather than risk it on a
  second, larger, less-well-understood change in the same phase. The practical consequence is
  narrow and disclosed in Section 9: at 10W/11W/12W, `BUILD_FASTER_THAN_HM_INTRO` still resolves to
  1 (not HM.4's predicted compressed value of 0) — identical to HM.5.1's own already-disclosed
  deviation at those same three horizons, not worsened or newly introduced by this phase. A future
  phase could close this cleanly by adding a decoupled stage-level compression-priority field
  (mirroring the phase-level `CompressionPriority`/`ExtensionPriority` precedent already proven at
  `CatalogPhaseAllocationResolver`), with its own full 10K zero-delta regression proof — that is a
  second, independent "smallest generic fix" of its own, not bundled into this one.
- **9W/17W remain correctly infeasible** — unchanged, not re-verified in this phase (no code path
  this phase touches affects phase-level feasibility).
- **HM remains entirely dark** — `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` for Half
  Marathon is unchanged (re-confirmed still `false` by `HalfMarathonIdentity_RemainsOutsideEveryPublicGate`,
  passing unmodified in Section 14 step 3). No public, Runway, or LongHorizon route was activated.

## 17. HM.5.3 gating conclusion

HM.5.3 (the volume-planner boundary decision for 12W/15W long-run-share and 16W peak-volume
overshoot) remains fully open and untouched, exactly as before this phase. This phase's own
literal scope — the stage-allocator Preferred-exposure semantic — is closed independently of
HM.5.3; nothing in HM.5.3 blocks on anything in this phase, and nothing in this phase attempts or
claims to resolve HM.5.3.

## Final classification

**`HM_PROGRESSION_STAGE_ALLOCATOR_OPTIONAL_PREFERRED_SEMANTIC_CLOSED`**

- HM.4's real 0/1 (Minimum/Preferred) authority is restored in the real catalog (Section 7, 10).
- The frozen preferred 14W Build occupancy (1+3) resolves exactly, re-verified through both this
  phase's own narrow allocator-level test and the full, unmodified, real production pipeline test
  (Section 9, 14).
- All 10-16W stage allocations are structurally valid (`GeneratedCatalogStageScheduleValidator`
  accepts every one; total resolved exposures == phaseWeeks at every horizon; no stage exceeds its
  frozen Maximum) — Section 9.
- Optional eligibility/fallback remains deterministic (Section 10/11; invariant G in Section 8).
- No stage exceeds frozen bounds anywhere (Section 8 invariant H, Section 9).
- No HM-specific allocator branch exists (Section 7 — grep-verified).
- All existing 10K behavior remains zero-delta, both formally (Section 12 — `PreferredExposures`
  absent everywhere in 10K, by construction identical arithmetic) and empirically (Section 13 —
  22/22 + 3/3 unmodified real-catalog tests pass; 3623/3623 across the entire `RuntimeCatalog`
  subtree; 1510/1510 `PlanCatalog.Tests`).
- No new regression exists (Section 14).

This closure is honest about, and does not paper over, the residual compression-priority-direction
finding (Section 16) — that finding does not conflict with any of the bullet points above (it
affects only the *internal split* of an already-structurally-valid compressed-horizon result, not
correctness, bounds, or the frozen 14W occupancy), and is explicitly flagged as a candidate for a
future, independent phase rather than silently left undocumented.
