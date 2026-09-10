# Phase HM.5.2A — Progression Stage Compression-Priority Semantic Closure

## 0. HEAD and working-tree attribution

This phase starts from `HEAD` at HM.5.2's own governance-backfill commit (`1ee680a`). It touches
exactly five source files (one production model, one loader, one allocator, one catalog document,
one schema document), adds two new test files, adds this report, and updates `PHASE_LEDGER.md`/
`MASTER_ROADMAP.md`. No unrelated file was modified. The real, feature/fix commit for this phase's
work is referenced below as `<HM52A_COMMIT_SHA>` until the docs-only backfill commit that
immediately follows it fills in the literal value (this report and `PHASE_LEDGER.md` reference that
commit's own SHA as a placeholder, per this engagement's established two-commit governance pattern).

## 1. Current RelativeOrder semantics (as found, before this phase's change)

Direct trace of every use site in `ProgressionStageAllocator.cs`, confirming HM.5.2's own Section 3
finding by re-reading the real, unmodified code:

- **Chronological / contiguous week-block layout** (`AllocatePhase`'s final assembly loop):
  stages are laid out in **ascending** `RelativeOrder`. Unambiguous, and untouched by this phase.
- **Extension priority** (`ApplyExtension`): candidates ordered `Extendable` before `FixedExposure`,
  then **descending** `RelativeOrder`, then `ProgressionStageKey` ordinal. The highest-order stage
  receives surplus first. Untouched by this phase.
- **Compression priority** (`ApplyCompression`, *before* this phase): candidates ordered
  **descending** `RelativeOrder`, then `ProgressionStageKey` ordinal — the highest-order
  Compressible stage is reduced first.

So, restating HM.5.2's own already-proven finding directly from the code (not re-derived): before
this phase, `RelativeOrder` served three purposes simultaneously — (A) chronological order
(ascending), (C) extension-fill priority (descending), and what HM.5.2 called the compression
"selection order" (descending) — with (A) and (C)/compression sharing the same literal sort
direction by coincidence of both being "descending", even though they answer different questions
("which stage grows" vs "which stage shrinks").

## 2. Proven semantic overload

Answering the governing prompt's explicit question: does `RelativeOrder` currently mean any
combination of (A) chronological order, (B) compression-removal priority, (C) preferred-fill
priority, (D) extension-fill priority?

- **(A) chronological order — YES**, and this meaning is never touched by any phase to date
  (HM.5.2 or this one).
- **(D) extension-fill priority — YES** (descending). This is a real, independent, and — per
  HM.4's own authored intent (`PHASE_HM_4...` Section 8: "extend `BUILD_THRESHOLD_DEVELOPMENT`...
  `FASTER_THAN_HM_INTRO` never extends past 1") — **correct** use of `RelativeOrder`.
- **(B) compression-removal priority — YES, before this phase**, and this use was **overloaded
  with (D)**: the exact same descending-`RelativeOrder` sort key was reused for the opposite
  semantic question (which stage shrinks vs. which stage grows), producing a direction that
  happens to agree with (D) numerically but conflicts with HM.4's explicit Build-specific
  compression intent (`PHASE_HM_4...` Section 7: "drop `FASTER_THAN_HM_INTRO` (1→0) **before**
  compressing `BUILD_THRESHOLD_DEVELOPMENT`" — i.e. the **lowest**-`RelativeOrder` stage must
  compress first, the opposite of what descending-`RelativeOrder` selection produces).
- **(C) preferred-fill priority — NO.** `PreferredExposures` (HM.5.2) is a per-stage baseline
  *value*, not a *selection order* — it does not use `RelativeOrder` for anything; Regime B
  (exact fit) simply reads every active stage's own `Baseline` with no selection/ordering
  question to answer at all (every active stage's own baseline is used, none is skipped).

**Classification: `OVERLOADED_STAGE_ORDER_AUTHORITY`, confirmed** — specifically the (B)/(D)
overload identified by HM.5.2, now traced to the exact code line
(`ApplyCompression`'s `.OrderByDescending(a => a.EffectiveStage.RelativeOrder)`) and proven to
produce `1+2` instead of HM.4's required `0+3` for a compressed 3-week HM Build phase:

At 10W/11W/12W, Build's phase-level allocation is exactly 3 weeks (HM.4 Layer A, unchanged by this
phase). `BUILD_FASTER_THAN_HM_INTRO`'s `Baseline` is 1 (`PreferredExposures=1`, HM.5.2), and
`BUILD_THRESHOLD_DEVELOPMENT`'s `Baseline` is 3 (`MinimumExposures=3`, no `PreferredExposures`
declared, so `Baseline == MinimumExposures`). `totalBaseline = 1 + 3 = 4 > availableWeeks = 3` —
Regime A (compression), deficit 1. Pre-HM.5.2A candidate order was descending `RelativeOrder`:
`BUILD_THRESHOLD_DEVELOPMENT` (order 2) is tried **first**. Its own compression floor (pre-this-
phase) is the historical hardcoded `1` (its `PreferredExposures` is null, so
`CompressionFloor` returns `1`, not its real `MinimumExposures` of 3 — a separate, disclosed, and
deliberately untouched HM.5.2 characteristic, see Section 4 below), giving it
`reducible = 3 - 1 = 2` headroom — more than enough to absorb the entire deficit (1) by itself,
so it reduces to `3 - 1 = 2` and `BUILD_FASTER_THAN_HM_INTRO` (order 1) is never even considered.
Result: `1 + 2 = 3`. This is exactly the `1+2` HM.5.2 Section 9 disclosed, confirmed still
reproducible on this phase's starting `HEAD` before any of this phase's own edits were applied
(re-run via the existing `Hm52StageOccupancyAllHorizonsTests`/`Hm5StageAllocatorMechanismProofTests`
fixtures, which document this same mechanism against synthetic and real data respectively).

## 3. HM.4 frozen compression authority (not reopened, restated for traceability)

From `PHASE_HM_4_PHASE_AND_STAGE_NUMERIC_HEADROOM_AUTHORITY_CLOSURE.md` Section 7/10 (verbatim,
not re-derived):

```
CompressionPriority (Build): drop FASTER_THAN_HM_INTRO (1 -> 0) BEFORE compressing
BUILD_THRESHOLD_DEVELOPMENT below its own preferred value.
```

| Horizon | Build phase weeks | Frozen Build occupancy (INTRO+DEVELOPMENT) |
|---|---|---|
| 10W | 3 | 0+3 |
| 11W | 3 | 0+3 |
| 12W | 3 | 0+3 |
| 13W | 4 | 1+3 |
| 14W | 4 | 1+3 (frozen golden) |
| 15W | 4 | 1+3 |
| 16W | 5 | 1+4 |

HM.4 also froze RaceSpecific compression intent (Section 7): drop `HM_INTRODUCTION` (1→0) before
compressing `HM_DEVELOPMENT` (2→1); `HM_REHEARSAL` is never touched in either direction. This
phase's own Section 9 (below) proves this authority is **already** reproduced exactly by the real,
unmodified extension mechanism — RaceSpecific never actually enters the compression regime at any
of the 7 real horizons (its Minimum-sum floor, `0+1+2=3`, never exceeds its own allocated week
count at any horizon from 10W to 16W), so no `CompressionPriority` catalog value was needed there.

## 4. Existing schema capability

Checked whether an existing scalar authority already expresses stage-level compression priority,
before adding a new field (per the governing prompt's explicit instruction):

- `RelativeOrder` — already shown (Section 1/2) to be unable to express both extension-fill and
  compression-removal priority simultaneously when they must run in opposite directions for the
  same phase (Build), without breaking the already-proven-correct extension order (10K's real,
  unmodified `BuildPhase_ExtensionAllocatesSurplusToHigherRelativeOrderStageFirst` test, and HM.4's
  own extension intent, both require descending `RelativeOrder` for extension). Cannot be reused.
- `PreferredExposures` (HM.5.2) — a per-stage baseline value, not an ordering/priority concept.
  Cannot answer "which stage compresses first" at all; it only sets *how much* a stage would like
  to keep before any compression/extension question is even asked.
- `MinimumExposures`/`MaximumExposures` — bounds, not priorities; already fully consumed by their
  own existing meaning (the true floor/ceiling).
- `CompressionBehavior` (`COMPRESSIBLE`/`PROTECTED`) — a binary gate (may this stage ever be
  reduced at all), not a priority among multiple Compressible stages.
- The sibling, already-approved **phase-level** `CatalogPhaseAllocationResolver.CompressionPriority`/
  `ExtensionPriority` fields (used for `FOUNDATION`/`BUILD`/`RACE_SPECIFIC`/`TAPER` ordering, per
  `PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md` and HM.4's own Layer A) are a **different
  class entirely** — they order which *phase* compresses/extends first, not which *stage within
  one phase's own stage list* compresses first. Confirmed by direct read: `CatalogPhaseAllocationResolver.cs`
  and `ProgressionStageAllocator.cs` are separate types operating at separate granularities, with no
  shared field between them. This precedent's *pattern* (a decoupled, explicit priority scalar,
  lower value = acted on first) is reused below — its *field* is not reachable from stage-level code.

**Conclusion: no existing scalar authority expresses stage-level compression-removal priority.**
A new, additive, optional field is required — matching HM.5.2's own explicit precedent instruction
("If no, identify the smallest additive generic seam").

## 5. Minimal generic seam

Added `CatalogWorkoutProgressionStage.CompressionPriority` (`int?`, default `null`), mirroring
`PreferredExposures`'s own HM.5.2 pattern exactly (same additive/degenerate-default shape already
established for `VolumeSafetyPolicy.TaperVolumeMultipliers`, HM.1.4B):

- **Semantics**: when two or more active Compressible stages in the same phase declare
  `CompressionPriority`, the stage with the **lowest** value is reduced **first** — mirroring the
  sibling, already-approved phase-level `CompressionPriority` convention (lower number = acted on
  earlier), so this engagement now has exactly one consistent "lower priority number = earlier"
  convention at both the phase and stage granularity, not two conflicting ones.
- **Default/fallback**: `null` for every stage that doesn't declare it (every 10K stage in every
  version/lane; every pre-HM.5.2A HM stage, including HM's own `RACE_SPECIFIC`/`FOUNDATION`/`TAPER`
  stages, which this phase does not touch — see Section 9). `ApplyCompression`'s candidate sort
  becomes:

  ```
  OrderBy(CompressionPriority ?? int.MaxValue)
      .ThenByDescending(RelativeOrder)
      .ThenBy(ProgressionStageKey, Ordinal)
  ```

  When **no** active Compressible stage in a phase declares `CompressionPriority`, every candidate
  computes the identical key (`int.MaxValue`), so the entire ordering degenerates to the *historical*
  `ThenByDescending(RelativeOrder).ThenBy(ProgressionStageKey)` tie-break — **byte-identical to the
  pre-HM.5.2A algorithm** for every one of those stages. This is a formal argument (by
  construction), independently re-confirmed empirically (Section 11).
- **Scope of use**: `CompressionPriority` is read from `EffectiveStage` directly in
  `ApplyCompression`'s own candidate sort — exactly the same way `RelativeOrder` itself is already
  read directly from `EffectiveStage` in that same sort (Section 12's TRUE-convergence/fallback
  merge branches never needed to touch `RelativeOrder` either, since `RelativeOrder` lives on the
  effective stage, not the merged `ActiveStage` wrapper — `CompressionPriority` follows the exact
  same shape, so no new merge-combination rule was needed in `AllocatePhase`'s Section 12 branches).
- **Never used for**: chronological ordering (still exclusively ascending `RelativeOrder`,
  untouched) or extension-fill ordering (still exclusively descending `RelativeOrder`, untouched).
  This directly answers the governing prompt's Section 7 requirement: PREFERRED TARGET (HM.5.2's
  `PreferredExposures`/`Baseline`), COMPRESSION REMOVAL ORDER (this phase's `CompressionPriority`),
  and EXTENSION FILL ORDER (unchanged `RelativeOrder` descending) are now three independently
  addressable authorities, not one overloaded field.
- **No new exception/validation was added for `CompressionPriority` itself** — any `int?` value is
  structurally valid (unlike `PreferredExposures`, there is no `[Minimum,Maximum]` range a priority
  ordinal must fit inside); this keeps the seam to its smallest possible shape, per the governing
  prompt's own "smallest generic fix" instruction.

## 6. Catalog/versioning changes

- `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json`:
  `BUILD_FASTER_THAN_HM_INTRO` gains `"compressionPriority": 1`; `BUILD_THRESHOLD_DEVELOPMENT` gains
  `"compressionPriority": 2` — the only catalog-data change in this phase. Document `status` remains
  `"VALIDATED"` (not `"PUBLISHED"`), the same in-place-edit precedent HM.5.1/HM.5.2 already
  established for this exact document — **not** an immutable published artifact, so no version
  bump, no new file, and no hash pin is broken by this edit (there is no `contentHash` field
  populated on this document to begin with — confirmed by direct read).
- `plan-catalog/schemas/workout-progression.schema.json`: added `"compressionPriority": { "type":
  "integer" }` as a new, optional (not in the `required` array) sibling property to
  `minimumExposures`/`maximumExposures`. Also added `"preferredExposures": { "type": "integer",
  "minimum": 0 }` — an HM.5.2 omission discovered during this phase's schema audit (HM.5.2 added
  the field to the backend runtime model and loader but never declared it in this JSON Schema
  document) — corrected here as a documentation-completeness fix, not a behavior change: the schema
  has no `additionalProperties: false` anywhere in the stage object (confirmed by direct read), so
  neither the pre-existing omission nor this phase's addition changes what JSON was ever accepted or
  rejected by schema validation.
- No change to `plan-catalog/src/PlanCatalog.Core/Models/WorkoutProgressionStageDefinition.cs` or
  `WorkoutProgressionValidator.cs` — confirmed by direct read that `PreferredExposures` was never
  added there either (HM.5.2's own established precedent: these two new fields are backend-runtime-
  only concepts, consumed exclusively by `RunningApp.Application`'s own
  `CatalogWorkoutProgressionLoader`/`ProgressionStageAllocator`, not by `PlanCatalog.Core`'s
  publish-time validation or hashing pipeline). `IContentHasher` computes its hash over the raw
  canonical JSON text of a document (confirmed by direct read of `IContentHasher`'s own contract
  comment), so this edit would only affect a hash if one were pinned for this specific document —
  none is (Section above).
- `PlanCatalog.Tests` full suite (1510/1510, Section 13) independently confirms zero schema/hash/
  combination-validation regression from both catalog and schema edits.

## 7. Generic invariant proof

New file: `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Progression/Hm52aProgressionStageAllocatorCompressionPriorityInvariantTests.cs`
(11 tests total, all passing), synthetic distance-blind fixtures only (`PHASE_A`, stage keys like
`A`/`B`/`OPTIONAL_INTRO`/`CORE_DEVELOPMENT` — no HM or 10K name):

| Invariant | Test | Result |
|---|---|---|
| Explicit `CompressionPriority` removes the low-priority optional stage first, even with the lower `RelativeOrder` | `ExplicitCompressionPriority_RemovesLowPriorityOptionalStageFirst_EvenWithLowerRelativeOrder` | PASS |
| Preferred-size capacity resolves the exact Preferred vector, `CompressionPriority` declared but unused (Regime B never invokes `ApplyCompression`) | `PreferredSizeCapacity_ResolvesExactPreferredVector_CompressionPriorityDeclaredButUnused` | PASS |
| Extension still follows the existing, untouched `RelativeOrder`-descending authority even when `CompressionPriority` is declared | `ExtensionCapacity_StillFollowsRelativeOrderDescending_EvenWithCompressionPriorityDeclared` | PASS |
| `CompressionPriority` does not alter chronological week-block order | `CompressionPriority_DoesNotAlterChronologicalWeekBlockOrder` | PASS |
| `CompressionPriority` does not alter extension priority, even when priorities are authored to invert `RelativeOrder` in the opposite direction from the compression case | `CompressionPriority_DoesNotAlterExtensionPriority_EvenWhenPrioritiesInvertRelativeOrder` | PASS |
| A `Protected` stage is never compressed ahead of a `Compressible` stage, even when it would sort first by `CompressionPriority` | `ProtectedStage_NeverCompressedAheadOfCompressibleStage_EvenWithLowerCompressionPriority` | PASS |
| Total resolved occupancy == phase capacity across compression/exact-fit/extension, with `CompressionPriority` declared | `TotalResolvedExposures_AlwaysEqualsPhaseWeeks_WithCompressionPriorityDeclared` (×3 regimes) | PASS |
| A legacy catalog with no `CompressionPriority` declared anywhere follows the exact historical `RelativeOrder`-descending compression order | `LegacyCatalog_WithNoCompressionPriorityDeclared_FollowsHistoricalRelativeOrderDescendingCompression` | PASS |
| Mixed declaration: an undeclared-priority sibling sorts after every declared priority (`int.MaxValue`) | `MixedDeclaration_UndeclaredCompressionPriorityStage_SortsAfterEveryDeclaredPriority` | PASS |

All 11 pass. Test-run evidence:
`dotnet test ... --filter "FullyQualifiedName~Hm52aProgressionStageAllocatorCompressionPriorityInvariantTests"`
→ `Başarısız: 0, Başarılı: 11, Toplam: 11`. Every pre-existing HM.5.2 invariant test
(`Hm52ProgressionStageAllocatorPreferredExposureInvariantTests`, 14 tests) also re-run and still
passes unmodified (Section 11) — the two invariant families are independent and do not interact
(no fixture in either file declares both `PreferredExposures` and `CompressionPriority` together,
though nothing in the algorithm would prevent a real catalog from doing so — `Baseline` and the
compression selection order are computed independently).

## 8. Exact HM 10-16 stage occupancy

Real phase allocator (`CatalogPhaseAllocationResolver`, unmodified) + real stage allocator
(`ProgressionStageAllocator`, this phase's `ApplyCompression` change) + real catalog artifacts
(post this phase's two-field edit), through `DynamicCoreWeekSkeletonOrchestrator` — the exact same
production path `Hm52StageOccupancyAllHorizonsTests` already exercises. New file:
`backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm52aStageCompressionPriorityOccupancyTests.cs`.

| Horizon | Phase allocation (Foundation/Build/RaceSpecific/Taper) | BUILD_FASTER_THAN_HM_INTRO | BUILD_THRESHOLD_DEVELOPMENT | Build total | RACE_SPECIFIC_HM_INTRODUCTION | RACE_SPECIFIC_HM_DEVELOPMENT | RACE_SPECIFIC_HM_REHEARSAL | TAPER_HM_ACTIVATION |
|---|---|---|---|---|---|---|---|---|
| 10W | 2/3/3/2 | **0** | **3** | 3 | 0 | 1 | 2 | 2 |
| 11W | 2/3/4/2 | **0** | **3** | 3 | 0 | 2 | 2 | 2 |
| 12W | 2/3/5/2 | **0** | **3** | 3 | 1 | 2 | 2 | 2 |
| 13W | 2/4/5/2 | 1 | 3 | 4 | 1 | 2 | 2 | 2 |
| **14W** | **3/4/5/2** | **1** | **3** | **4** | **1** | **2** | **2** | **2** |
| 15W | 4/4/5/2 | 1 | 3 | 4 | 1 | 2 | 2 | 2 |
| 16W | 4/5/5/2 | 1 | 4 | 5 | 1 | 2 | 2 | 2 |

Every value in this table is copied verbatim from
`PHASE_HM_4_PHASE_AND_STAGE_NUMERIC_HEADROOM_AUTHORITY_CLOSURE.md` Section 9/10's frozen table —
not re-derived here — and **every one is now reproduced exactly by the real, unmodified-except-for-
this-phase's-own-additive-field production pipeline**, confirmed by
`EveryHorizon_BuildStageOccupancyMatchesHm4FrozenCompressionAndExtensionAuthority` (7 theory cases)
and `EveryHorizon_RaceSpecificStageOccupancyMatchesHm4FrozenAuthority` (7 theory cases), both
passing. `GeneratedCatalogStageScheduleValidator.IsValid == true` at every horizon; total resolved
exposures == `targetWeekCount` exactly (re-confirmed by `Hm52StageOccupancyAllHorizonsTests`,
still passing unmodified). The **10W/11W/12W discrepancy HM.5.2 disclosed (`1+2` instead of `0+3`)
is now closed**: `BUILD_FASTER_THAN_HM_INTRO` correctly compresses to `0` first at every one of
these three horizons, and `BUILD_THRESHOLD_DEVELOPMENT` correctly stays at its own real `3`.

The **14W frozen golden Build occupancy (`1+3`) is unchanged** — re-confirmed both narrowly
(`Hm52StageOccupancyAllHorizonsTests.FourteenWeekHorizon_BuildPhaseResolvesFrozenOneAndThreeOccupancy_UnderHm4RealMinimumZeroAuthority`,
still passing unmodified) and end-to-end
(`Hm2FullDarkVerticalSliceTests.PreferredFourteenWeekCore_TraversesRealPipelineAndMaterializesValidDarkPreview`,
still passing unmodified) — this phase's `ApplyCompression` change is never even invoked at 14W
(Regime B, exact fit; `ApplyCompression` only runs when `totalBaseline > availableWeeks`).

## 9. RaceSpecific recurring-family result

Mandatory audit per the governing prompt's Section 9, even though the discovered symptom was
Build-specific. Traced `RACE_SPECIFIC_HM_INTRODUCTION` (Min=0/Max=1, no `PreferredExposures`, so
`Baseline=0`), `RACE_SPECIFIC_HM_DEVELOPMENT` (Min=1/Max=2, `Baseline=1`, `Extendable`), and
`RACE_SPECIFIC_HM_REHEARSAL` (Min=2/Max=2, `Protected`/`FixedExposure`, permanently untouched in
either direction) across all 7 horizons' real RaceSpecific phase-level allocation (3, 4, 5, 5, 5, 5,
5 weeks respectively, per HM.4 Layer A, unchanged):

- `totalBaseline = 0 + 1 + 2 = 3` for every horizon (RaceSpecific stages' `Baseline` values are
  fixed; only the phase-level allocated week count varies).
- At **10W** (RaceSpecific=3): `totalBaseline (3) == availableWeeks (3)` — **Regime B, exact fit**.
  No compression, no extension. Result: `0+1+2`. Matches HM.4.
- At **11W-16W** (RaceSpecific=4 or 5): `totalBaseline (3) < availableWeeks` — **Regime C,
  extension**, surplus 1-2. `ApplyExtension`'s existing, unchanged `Extendable`-first /
  descending-`RelativeOrder` order tries `RACE_SPECIFIC_HM_DEVELOPMENT` (order 2, Extendable,
  headroom `2-1=1`) before `RACE_SPECIFIC_HM_INTRODUCTION` (order 1, Extendable, headroom
  `1-0=1`) — filling DEVELOPMENT to its own Maximum (2) first, then INTRODUCTION. This produces
  exactly `0+2+2` at 11W (surplus 1, fully absorbed by DEVELOPMENT) and `1+2+2` at 12W-16W
  (surplus 2, DEVELOPMENT then INTRODUCTION each absorb 1) — **matching HM.4's frozen table
  exactly at every horizon** (Section 8 above).

**Conclusion: RaceSpecific's real compression/extension mechanism already reproduces HM.4's frozen
authority exactly, with zero code or catalog change from this phase.** Compression (`ApplyCompression`)
is **never actually invoked** for RaceSpecific at any of the 7 real horizons — RaceSpecific's own
Minimum-sum floor (3) never exceeds its own phase-level allocation at any horizon from 10W to 16W —
so there was no live `RelativeOrder`-overload defect to close there in the first place, and no
`CompressionPriority` value was authored onto `RACE_SPECIFIC_HM_INTRODUCTION`/`RACE_SPECIFIC_HM_DEVELOPMENT`
(nothing to close; adding an inert field there would be unjustified scope creep). `RACE_SPECIFIC_HM_REHEARSAL`'s
permanent protection is re-confirmed intact at every horizon (`expectedRehearsal == 2` asserted
directly, Section 8's test data) — never compressed, never extended, in either eligibility branch.

## 10. Eligibility/fallback result

`BUILD_FASTER_THAN_HM_INTRO` declares `"requires": []` in the real catalog (`NotConditioned`,
always eligible, no `fallbackStageKey`) — unchanged by this phase, confirmed by direct read; there
is no real ineligibility branch for this stage in the current catalog (same finding HM.5.1/HM.5.2
already made and did not need to invent). `CompressionPriority` is read directly from
`EffectiveStage` in exactly the same way `RelativeOrder` already was — it requires no new
eligibility/fallback-merge logic, and none was added. The generic ineligible-optional-stage-yields-
to-fallback invariant (already proven generically by HM.5.2's own `IneligibleOptionalStage_YieldsToFallback_FallbackPreferredGovernsBaseline`)
is unaffected: that test declares no `CompressionPriority` on either fixture stage, still passes
unmodified (Section 11), and the fallback-resolution code path this phase did not touch (`ResolveEligibility`,
the Section 12 merge branches in `AllocatePhase`) is provably untouched by this phase's diff (only
`ApplyCompression`'s own candidate sort changed). Binding (which concrete workout a resolved stage
occupancy maps to) is a separate, later concern (`CatalogWorkoutBinder`) not exercised or altered by
this phase — stage occupancy and workout binding remain cleanly separated, as before.

## 11. 10K before/after vectors

Ran the real, unmodified `ProgressionStageAllocatorTests.cs` (22 tests, exercising the real default
12-week 10K pilot candidate, including `BuildPhase_ExtensionAllocatesSurplusToHigherRelativeOrderStageFirst`
[real extension-regime case, unaffected — extension order is untouched] and
`CompressionAllocation_ReducesHighestRelativeOrderCompressibleStageFirst` [synthetic compression
case with exactly ONE Compressible stage — neither fixture declares `CompressionPriority`, so both
compute `int.MaxValue` and the tie-break falls through to the identical historical
`RelativeOrder`-descending order]) — **all 22 pass, unmodified, zero assertion changed**. Ran the
full `Hm5StageAllocatorMechanismProofTests.cs` (3 tests, HM-shaped synthetic fixtures that
predate `PreferredExposures`/`CompressionPriority` and declare neither) — **all 3 pass, unmodified,
zero assertion changed**. Ran the full `Hm52ProgressionStageAllocatorPreferredExposureInvariantTests.cs`
(14 tests, none declare `CompressionPriority`) — **all 14 pass, unmodified, zero assertion changed**.

**Formal argument** (not just empirical): grepped every real 10K `WORKOUT_PROGRESSION` catalog
document (`ten-k-workout-progression.v1.json` through `.v7.json`, `ten-k-workout-progression-2d.v1.json`
— 9 files) for `"compressionPriority"`: **zero matches anywhere**. Because `ApplyCompression`'s new
sort key computes `CompressionPriority ?? int.MaxValue`, and every 10K stage across every
version/lane leaves this field null (by construction — no 10K catalog document was edited by this
phase), every 10K Compressible candidate's sort key is identically `int.MaxValue` in every
compression scenario, and the `.ThenByDescending(RelativeOrder).ThenBy(ProgressionStageKey)`
tie-break — the *entire* pre-HM.5.2A sort — is exactly what executes. This is mathematically
identical to the pre-HM.5.2A code path for every 10K stage, in every phase, at every horizon,
regardless of which regime (compression/exact-fit/extension) is entered — not merely empirically
observed to be so.

Re-checked `PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md` explicitly (per the governing prompt's
own instruction): its 13-week extension mechanism uses the **phase-level**
`CatalogPhaseAllocationResolver.CompressionPriority`/`ExtensionPriority` fields (already `1/2/3/4`,
already decoupled from structural ordering) — a completely different class and a completely
different field from this phase's new **stage-level** `CatalogWorkoutProgressionStage.CompressionPriority`.
Grepped that report and `CatalogPhaseAllocationResolver.cs` for `ProgressionStageAllocator`/
`StageAllocator`/the new field name: zero matches. Confirmed this precedent's own mechanism does
not flow through the code this phase changed, and does not need re-verification here — it was
already independently proven safe by HM.4/HM.5.1/HM.5.2's own prior audits and remains untouched.

**Target achieved: ZERO 10K BEHAVIORAL DELTA BY CONSTRUCTION**, confirmed both formally (this
section) and empirically (Section 13's full `RuntimeCatalog` subtree run — 3657/3657, zero
failures, the only new tests being this phase's own 26 additions).

## 12. Hash/catalog validation

- `PlanCatalog.Tests` full suite: **1510/1510 passed** (Section 13) — matches the documented
  1510/1510 baseline exactly, confirming zero schema/hash/combination-validation regression from
  both the `half-marathon-workout-progression.v1.json` two-field edit and the
  `workout-progression.schema.json` two-property addition.
- The edited catalog document's `metadata.status` is `"VALIDATED"`, not `"PUBLISHED"` — confirmed
  by direct read both before and after this phase's edit — so it is not an immutable published
  artifact under this repository's own lifecycle rules (the same precedent HM.5.1/HM.5.2 already
  relied on for their own in-place edits to this same document).
- No `contentHash` value is populated on this document (confirmed by direct read) — there is
  nothing to silently update or hide a mutation behind.
- The schema addition (`compressionPriority`, `preferredExposures`) is purely additive: the schema
  has no `additionalProperties: false` restriction anywhere in the stage-object definition
  (confirmed by direct read of the full schema file, both properties list and `required` array),
  so this change could not have caused any prior document to newly fail or newly pass validation —
  it only makes two already-silently-accepted properties explicit and documented.
- No other 10K or HM catalog document, workout definition, prescription profile, or combination
  document was read from or written to by this phase.

## 13. Regression results

All commands run sequentially (never two `dotnet test` invocations concurrently against the shared
Postgres test database, per this engagement's own established discipline):

1. `RunningApp.Application` build: **0 errors, 0 warnings**.
2. `RunningApp.IntegrationTests` build: **0 errors** (pre-existing, unrelated nullable-reference
   warnings only, none touching any file this phase changed).
3. Targeted allocator suite (`ProgressionStageAllocatorTests` + `Hm52ProgressionStageAllocatorPreferredExposureInvariantTests`
   + `Hm52aProgressionStageAllocatorCompressionPriorityInvariantTests` + `Hm5StageAllocatorMechanismProofTests`
   + `Hm52StageOccupancyAllHorizonsTests`): **63/63 passed**.
4. `Hm52aStageCompressionPriorityOccupancyTests` (new, HM 10-16W Build+RaceSpecific structural
   proof): **15/15 passed**.
5. Full `RuntimeCatalog.HalfMarathon` namespace + `Hm2FullDarkVerticalSliceTests` (includes the
   still-correctly-failing-closed, unmodified 12W/15W long-run-share and 16W peak-volume-ceiling
   blocker tests — see Section 14): **155/155 passed**.
6. Full `RuntimeCatalog` namespace subtree (10K + HM + LongHorizon + PreparationRunway + Binding +
   Materialization + Progression — the entire reachable surface of the shared allocator):
   **3657/3657 passed, 0 failed**, 5m46s. (3657 = the count observed on this phase's own starting
   `HEAD`'s subtree plus this phase's own 26 new tests — 11 generic invariant +
   15 HM structural — confirmed by direct `--list-tests` enumeration; no test was silently removed
   or skipped.)
7. `PlanCatalog.Tests` (full suite, run sequentially after the `RuntimeCatalog` run completed):
   **1510/1510 passed**, ~5s — matches the established baseline exactly.
8. Full monolithic `RunningApp.IntegrationTests` suite (run alone, after every step above completed
   — never concurrently with another `dotnet test` invocation against the same Postgres test
   database): **`3 failed / 4564 passed / 4567 total`, 40m9s** — see Section 13a below for the
   literal observed named-failure comparison against the documented durable baseline (Gen4E weeks
   13/14, `Sw09ExplicitZeroReadinessEndToEndTests`, and the wall-clock-date LongHorizon artifact
   appearing on roughly 4 of every 7 real calendar days).

### 13a. Full monolithic suite — literal result

`Başarısız: 3, Başarılı: 4564, Atlanan: 0, Toplam: 4567`, 40m9s. The 3 failures, compared by exact
test NAME against the documented durable baseline (not merely by count):

- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
  — `Assert.Equal() Failure: Expected OK, Actual InternalServerError` — **matches the documented
  baseline exactly by name.**
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
  — same failure shape — **matches the documented baseline exactly by name.**
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`
  — same failure shape — **matches the documented baseline exactly by name.**

The recurring wall-clock-date LongHorizon test artifact (documented as appearing on roughly 4 of
every 7 real calendar days) **did not trigger on this run's calendar day** — its absence is
expected, not a discrepancy, exactly as HM.5.2's own baseline run also did not trigger it.

**Zero new failures, by name, anywhere in the suite** — in particular, no new failure appeared in
any 10K-namespaced test despite this phase modifying the shared `ProgressionStageAllocator.ApplyCompression`
method that both HM and every 10K candidate route through. Total (`4567`) is exactly HM.5.2's own
documented baseline total (`4541`) plus this phase's own `26` new tests (`11` generic invariant +
`15` HM structural), confirming by direct arithmetic that no existing test was silently removed,
renamed out of the count, or skipped.

## 14. Known HM.5.3 blockers reproduced unchanged

Re-ran `Hm2FullDarkVerticalSliceTests.TwelveAndFifteenWeekHorizons_HitGenuineLongRunShareNumericSafetyBlocker`
(both `12W`→`FINAL_WEEK_10_LONG_RUN_SHARE_EXCEEDS_CAP` and `15W`→`FINAL_WEEK_13_LONG_RUN_SHARE_EXCEEDS_CAP`
cases) and `SixteenWeekHorizon_HitsGenuinePeakVolumeCeilingOvershootBlocker` — **all still fail
closed with the exact same error codes and the exact same pinned `46.5d` km overshoot value**,
unmodified, in Section 13 step 5's `155/155` run. This confirms directly (not merely by inference)
that shifting Build's compressed-horizon split from `1+2` to `0+3` at 10W/11W/12W does **not** alter
the downstream volume/long-run planner's resolved values for these three horizons — the planner's
`ResolvePeak`/long-run-share mechanism operates on phase-level week counts and the weekly-volume
curve, not on which of Build's two stages a given week's `KEY_SESSION` is labeled with. HM.5.3
(the volume-planner boundary decision) remains fully open and untouched by this phase, exactly as
before — nothing in this phase attempts or claims to resolve it.

## 15. Files changed

- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/CatalogWorkoutProgressionDefinition.cs`
  — added `CatalogWorkoutProgressionStage.CompressionPriority` (`int?`, default `null`).
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/CatalogWorkoutProgressionLoader.cs`
  — reads optional `"compressionPriority"` JSON property, mirroring `"preferredExposures"`'s own
  HM.5.2 pattern exactly.
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/ProgressionStageAllocator.cs`
  — `ApplyCompression`'s candidate sort now orders by `CompressionPriority ?? int.MaxValue`
  ascending first, then the historical `RelativeOrder` descending / `ProgressionStageKey` ordinal
  tie-break. Doc comments updated (class-level summary, `ApplyCompression`'s own remarks) to
  describe the new, decoupled authority. No other method, and no other line of executable logic,
  changed.
- `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` —
  `BUILD_FASTER_THAN_HM_INTRO`: added `"compressionPriority": 1`. `BUILD_THRESHOLD_DEVELOPMENT`:
  added `"compressionPriority": 2`. Only data change.
- `plan-catalog/schemas/workout-progression.schema.json` — added `"compressionPriority"` (new) and
  `"preferredExposures"` (HM.5.2 omission, corrected) as optional integer properties on the stage
  object; neither added to the `required` array.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Progression/Hm52aProgressionStageAllocatorCompressionPriorityInvariantTests.cs`
  — new, generic (distance-blind) invariant tests (11 tests).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm52aStageCompressionPriorityOccupancyTests.cs`
  — new, HM-specific 10-16W Build + RaceSpecific structural stage-occupancy proof against HM.4's
  frozen table, plus a direct catalog-data assertion (15 tests).
- `PHASE_HM_5_2A_STAGE_COMPRESSION_PRIORITY_SEMANTIC_CLOSURE.md` — this report.
- `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` — governance updates.

No file under `VolumeSafetyPolicy`, `CatalogVolumeAndLongRunPlanner`, any volume/long-run/taper
path, any public/Runway/LongHorizon routing file, `CatalogPhaseAllocationResolver.cs` (the separate
phase-level allocator), `PlanCatalog.Core`'s `WorkoutProgressionStageDefinition.cs`/
`WorkoutProgressionValidator.cs`, any 10K catalog document, or any other HM catalog document
(`half-marathon-master.v1.json` was read for context only, never edited) was touched.

## 16. Remaining blocker ledger

- **HM.5.3 volume-planner blockers (unchanged, out of scope)**: 12W/15W
  `FINAL_WEEK_*_LONG_RUN_SHARE_EXCEEDS_CAP` and 16W's 46.5km peak-volume overshoot vs the 43.0km
  reference ceiling remain exactly as HM.5.1/HM.5.2 disclosed and this phase re-confirmed unchanged
  (Section 14). Not touched, per this phase's explicit hard boundary.
- **9W/17W remain correctly infeasible** — unchanged, not re-verified in this phase's own new
  tests (no code path this phase touches affects phase-level feasibility); still covered by the
  existing, unmodified `OutOfCanonicalRangeHorizons_RemainInfeasibleAfterHm5Headroom` test.
- **HM remains entirely dark** — `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` for Half
  Marathon is unchanged; no public, Runway, or LongHorizon route was activated by this phase.
- **The `CompressionFloor` hardcoded-`1`-for-undeclared-`PreferredExposures` characteristic
  (HM.5.2's own documented behavior, Section 2's `BUILD_THRESHOLD_DEVELOPMENT` trace above) is
  unchanged and not revisited** — this phase's fix works entirely by removing
  `BUILD_FASTER_THAN_HM_INTRO`'s own headroom (`1 - 0 = 1`) before `BUILD_THRESHOLD_DEVELOPMENT` is
  ever considered, so `BUILD_THRESHOLD_DEVELOPMENT`'s own compression floor value never becomes
  numerically relevant at any of the 7 real HM horizons (the deficit is always fully absorbed by
  `BUILD_FASTER_THAN_HM_INTRO` alone, since 1 week is the largest deficit reached down to the
  canonical 10W floor). A future phase that ever needed a *third* Build stage or a larger
  compression deficit would need to separately examine whether `BUILD_THRESHOLD_DEVELOPMENT`
  should also declare `PreferredExposures` to gain a real (non-hardcoded) compression floor — out
  of this phase's scope, and not currently reachable by any real catalog data.
- **No other `RelativeOrder`-overload defect family was found** anywhere else in the real HM or 10K
  catalog data (Section 9's RaceSpecific audit; Section 11's 10K audit) — the Build-stage defect
  HM.5.2 disclosed was the only live instance.

## 17. HM.5.3 gating conclusion

HM.5.3 (the volume-planner boundary decision for 12W/15W long-run-share and 16W peak-volume
overshoot) remains fully open and untouched, exactly as before this phase, and is independently
re-confirmed byte-identical (Section 14). This phase's own literal scope — the stage-allocator
compression-removal-priority semantic — is closed independently of HM.5.3; nothing in HM.5.3 blocks
on anything in this phase, and nothing in this phase attempts or claims to resolve HM.5.3. This
phase does not begin HM.5.3's own numeric trajectory work, per the governing prompt's explicit
instruction.

## Final classification

`HM_STAGE_COMPRESSION_PRIORITY_SEMANTIC_CLOSED_ZERO_DELTA`

- Runtime reproduces HM.4's compression authority: 10W/11W/12W Build resolves `0+3` exactly
  (Section 8), 14W remains `1+3` unchanged (Section 8), RaceSpecific compression/extension
  matches HM.4 at every horizon (Section 9), protected rehearsal remains protected at every
  horizon (Section 9), extension behavior is unchanged and independently re-confirmed correct
  (Section 7 invariant C/E, Section 11), chronological ordering is unchanged (Section 7 invariant
  D), eligibility/fallback remains deterministic (Section 10), no HM-specific allocator branch
  exists (grep-verified, Section 15/16 of this report and this phase's own code diff), all existing
  10K behavior remains zero-delta both formally and empirically (Section 11), no catalog/hash
  regression exists (Section 12; `PlanCatalog.Tests` `1510/1510`), and no new integration regression
  exists (Section 13a: the full monolithic suite's only 3 failures match the documented durable
  baseline exactly by name, with zero new failures anywhere, including the 10K namespace).
- The two disclosed HM.5.3 volume-planner blockers (12W/15W long-run-share; 16W 46.5km peak-volume
  overshoot) reproduce byte-identically after this phase's Build-split change (Section 14),
  confirming the volume/long-run planner is insensitive to which Build stage a given week's
  `KEY_SESSION` is labeled with.
- HM.5.3's own numeric trajectory work was not begun in this phase, per the governing prompt's
  explicit instruction.
