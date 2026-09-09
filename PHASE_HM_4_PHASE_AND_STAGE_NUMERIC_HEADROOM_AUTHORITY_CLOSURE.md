# PHASE HM.4 — Half-Marathon 10–16W Phase and Stage Numeric Headroom Authority Closure

Governance / product-decision phase only. No production code, no catalog file, no test file, no schema field was modified by this phase — every value below is a proposed authoring decision for a future implementation phase, mechanically simulated against the real resolver but not yet written to any `.json`.

## 1. Source inventory

Read directly (not summarized) for this phase:

- `plan-catalog/catalog/templates/half-marathon-master.v1.json` — current phase Min/Preferred/Max/priority fields.
- `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` — current stage exposure bounds and behaviors.
- `plan-catalog/schemas/workout-progression.schema.json` — real `compressionBehavior`/`extensionBehavior` enum.
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocation.cs` — the real, unmodified `CatalogPhaseAllocationResolver` algorithm.
- `PHASE_HM_3_TEN_TO_SIXTEEN_WEEK_DYNAMIC_CORE_PHASE_ALLOCATION_AUTHORITY_CLOSURE.md` (this engagement's own immediately-prior phase).
- `PHASE_HM_2_1_HM_MASTER_AUTHORITY_AND_REGRESSION_CLOSURE.md`, `PHASE_HM_2_INTERMEDIATE_4D_14W_FULL_DARK_VERTICAL_SLICE.md` — for the frozen 14W anchor and `PhasesDefineDefaultCycleOnly` mechanism.
- `PHASE_HM_1_...` / `PHASE_HM_1_1_...` / `PHASE_HM_1_2_...` reports — checked for any pre-existing HM-specific compression/extension evidence.
- `HM_21K_Claude_Handoff_and_Build_Plan.md` §6 (`OPEN-HM-01`) — re-confirmed this open item explicitly states the 14W row as the only "Known" allocation and 10–13/15–16 as genuinely unresolved; no hidden evidence exists there for this phase to have missed.
- `PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md` (10K) — read strictly for **mechanism/governance pattern** (the generic resolver's round-robin, pass-based distribution behavior; the Foundation-first elasticity rationale class), never for numeric inheritance. No 10K number appears anywhere below.

## 2. Real schema terminology (re-confirmed, not re-derived)

`compressionBehavior`: `COMPRESSIBLE` | `PROTECTED`. `extensionBehavior`: `EXTENDABLE` | `FIXED_EXPOSURE`. This is the only vocabulary used throughout this report, per HM.3's own terminology correction.

## 3. Frozen authorities (not reopened)

- Canonical CoreCycle: `10 / 14 / 16` (unchanged).
- 14W preferred phase allocation: `Foundation 3 / Build 4 / RaceSpecific 5 / Taper 2` (unchanged, still the anchor every other row is computed relative to).
- Taper duration: `2 / 2 / 2`, permanently `PROTECTED`/`FIXED_EXPOSURE` (unchanged — no currently-approved authority contradicts this, so it is not reopened).
- Volume, long-run, taper-volume, pace, and calendar authorities: untouched; this phase concerns exposure-count/week-count structure only (§11 Dose Invariant).
- No prior HM candidate value was found anywhere for 10–13W or 15–16W phase splits — `OPEN-HM-01` is genuinely unresolved evidence, not a legacy number this phase silently promoted. Classification: `NOT_APPLICABLE` (no legacy candidate exists to classify).

## 4. Current point-value blocker (restated from HM.3, re-confirmed by direct read)

Every phase's `Min/Preferred/Max` and every stage's `minimumExposures/maximumExposures` equal an identical point value pinned to the 14W preferred row. This phase authors the missing headroom.

## 5. Layer A — Phase Min/Preferred/Max decisions

| Phase | Min | Preferred (frozen) | Max | CompressionPriority | ExtensionPriority |
|---|---|---|---|---|---|
| FOUNDATION | **2** | 3 | **4** | 1 (unchanged) | 1 (unchanged) |
| BUILD | **3** | 4 | **5** | 2 (unchanged) | 2 (unchanged) |
| RACE_SPECIFIC | **3** | 5 | **5** (unchanged — no extension) | 3 (unchanged) | 3 (unchanged) |
| TAPER | 2 (unchanged) | 2 | 2 (unchanged) | 4 (unchanged) | 4 (unchanged) |

Sum of minimums = 2+3+3+2 = **10**, exactly the canonical floor. Sum of maximums = 4+5+5+2 = **16**, exactly the canonical ceiling. No slack, no shortfall — this is a real constraint the proposed numbers must satisfy exactly, not an approximate fit.

No priority ordinal changes at all: the already-authored `1/2/3/4` compression and extension priorities (Foundation-first for both directions) are reused as-is. This ordering is independently justified for HM, not copied from 10K's specific numbers: Foundation (aerobic base) is the least race-specific and most fatigue-tolerant phase, Build's threshold-introduction progression is comparatively fixed once begun, RaceSpecific carries the protected rehearsal block and the highest fatigue/specificity load, and Taper is medically protected. This is the same *class* of reasoning already evidence-informed for 10K's Foundation elasticity (mechanism reuse, per §2's own instruction), applied fresh to HM's own phase objectives — not a numeric inheritance.

## 6. Layer B — Stage Min/Preferred/Max decisions

**FOUNDATION** — single stage, 1:1 with phase weeks:

| Stage | Min | Preferred | Max | compressionBehavior | extensionBehavior |
|---|---|---|---|---|---|
| `FOUNDATION_AEROBIC_BASE` | 2 | 3 | 4 | COMPRESSIBLE | EXTENDABLE |

**BUILD** — two stages, `FASTER_THAN_HM_INTRO` is the first-removable exposure (already implied by the existing "0+4 fallback shape" fact in the real catalog):

| Stage | Min | Preferred (eligible / fallback) | Max | compressionBehavior | extensionBehavior |
|---|---|---|---|---|---|
| `BUILD_FASTER_THAN_HM_INTRO` | 0 | 1 / 0 | 1 | COMPRESSIBLE | EXTENDABLE |
| `BUILD_THRESHOLD_DEVELOPMENT` | 3 | 3 / 4 | 5 | COMPRESSIBLE | EXTENDABLE |

Compression order within Build: drop `FASTER_THAN_HM_INTRO` (1→0) before compressing `BUILD_THRESHOLD_DEVELOPMENT` below its own preferred value. Extension order: extend `BUILD_THRESHOLD_DEVELOPMENT` (repeat one more threshold week) — `FASTER_THAN_HM_INTRO` never extends past 1.

**RACE_SPECIFIC** — three effective stages (`HM_DEVELOPMENT`/`HM_REHEARSAL` each collapse with their current-fitness fallback, which carries an identical numeric range):

| Stage | Min | Preferred | Max | compressionBehavior | extensionBehavior |
|---|---|---|---|---|---|
| `RACE_SPECIFIC_HM_INTRODUCTION` | 0 | 1 | 1 | COMPRESSIBLE | EXTENDABLE |
| `RACE_SPECIFIC_HM_DEVELOPMENT` (or fallback) | 1 | 2 | 2 | COMPRESSIBLE | EXTENDABLE |
| `RACE_SPECIFIC_HM_REHEARSAL` (or fallback) | 2 | 2 | 2 | **PROTECTED (unchanged)** | **FIXED_EXPOSURE (unchanged)** |

Compression order within RaceSpecific, mirroring Build's own already-established INTRO-first pattern: drop `HM_INTRODUCTION` (1→0) first, then compress `HM_DEVELOPMENT` (2→1). `HM_REHEARSAL` is never touched in either direction, in either eligibility branch — this answers §5's central question directly: **the 5-week floor HM.3 found is B, a consequence of current point-valued bounds, not a true canonical minimum** — the true canonical minimum under `HM_REHEARSAL`'s permanent protection is **3** (0+1+2), not 5.

**TAPER** — unchanged, `2/2/2`, `PROTECTED`/`FIXED_EXPOSURE`, both directions, always.

## 7. Compression priority (required output shape)

```
CompressionPriority:
1. FOUNDATION (FOUNDATION_AEROBIC_BASE: 3 -> 2)
2. BUILD (drop FASTER_THAN_HM_INTRO 1 -> 0, then BUILD_THRESHOLD_DEVELOPMENT 4 -> 3)
3. RACE_SPECIFIC (drop HM_INTRODUCTION 1 -> 0, then HM_DEVELOPMENT 2 -> 1; HM_REHEARSAL never touched)
4. TAPER (never — PROTECTED)
```

## 8. Extension priority (required output shape)

```
ExtensionPriority:
1. FOUNDATION (FOUNDATION_AEROBIC_BASE: 3 -> 4)
2. BUILD (BUILD_THRESHOLD_DEVELOPMENT: 3/4 -> 4/5)
3. RACE_SPECIFIC (no headroom authored -- structurally inert at this priority slot)
4. TAPER (never — PROTECTED/FIXED_EXPOSURE)
```

No `PRODUCT_DECISION_REQUIRED` tie exists: with Layer A/B frozen as above, the real resolver's round-robin, one-week-per-pass distribution (confirmed by direct read and mechanical simulation, §14) produces exactly one allocation per horizon — never two equally-defensible outcomes.

## 9. Exact 10–16 derived allocation table

Derived by mechanically simulating the real, unmodified `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` algorithm (its literal round-robin `DistributeByPriority` logic, re-implemented verbatim in a throwaway script, not a hand guess) against the Layer A bounds in §5:

| Horizon | Foundation | Build | RaceSpecific | Taper | Sum |
|---|---|---|---|---|---|
| 10W | 2 | 3 | 3 | 2 | 10 |
| 11W | 2 | 3 | 4 | 2 | 11 |
| 12W | 2 | 3 | 5 | 2 | 12 |
| 13W | 2 | 4 | 5 | 2 | 13 |
| 14W | 3 | 4 | 5 | 2 | 14 (frozen, unchanged) |
| 15W | 4 | 4 | 5 | 2 | 15 |
| 16W | 4 | 5 | 5 | 2 | 16 |

Every row was authored as a *consequence* of §5/§6/§7/§8's frozen headroom and priority policy — the table was not written first and reverse-fit.

## 10. Per-horizon stage occupancy

| Horizon | FOUNDATION_AEROBIC_BASE | BUILD (INTRO+DEVELOPMENT) | RACE_SPECIFIC (INTRO+DEVELOPMENT+REHEARSAL) | TAPER |
|---|---|---|---|---|
| 10W | 2 | 0+3 | 0+1+2 | 2 |
| 11W | 2 | 0+3 | 0+2+2 | 2 |
| 12W | 2 | 0+3 | 1+2+2 | 2 |
| 13W | 2 | 1+3 (or 0+4 fallback) | 1+2+2 | 2 |
| 14W | 3 | 1+3 (or 0+4 fallback) | 1+2+2 | 2 |
| 15W | 4 | 1+3 (or 0+4 fallback) | 1+2+2 | 2 |
| 16W | 4 | 1+4 (or 0+5 fallback) | 1+2+2 | 2 |

Every row respects every Min/Max in §6, never duplicates the `PROTECTED`/`FIXED_EXPOSURE` rehearsal or taper stage beyond their fixed 2, and never invents a new stage — extension only ever repeats `FOUNDATION_AEROBIC_BASE` or `BUILD_THRESHOLD_DEVELOPMENT`, both already-`EXTENDABLE` real stages.

## 11. 10W feasibility proof

`Foundation=2, Build=3, RaceSpecific=3, Taper=2`. The essential adaptation sequence — aerobic base, volume/threshold introduction, genuine race-pace-specific rehearsal, taper — remains structurally intact at every horizon down to 10W: `RACE_SPECIFIC_HM_REHEARSAL`'s permanent protection guarantees a real 2-week race-pace dress-rehearsal block regardless of compression, and only the lowest-information "introduction" exposures (Build's `FASTER_THAN_HM_INTRO`, RaceSpecific's `HM_INTRODUCTION`) are ever fully dropped — never a core-adaptation stage. **No `CANONICAL_CORE_MINIMUM_CONFLICT` found.**

## 12. 16W feasibility proof

`Foundation=4, Build=5, RaceSpecific=5, Taper=2`. Both added weeks are absorbed by repeating an already-`EXTENDABLE` stage at its existing dose tier (`FOUNDATION_AEROBIC_BASE` +1, `BUILD_THRESHOLD_DEVELOPMENT` +1) — no new stage invented, `RACE_SPECIFIC_HM_REHEARSAL` never duplicated, `TAPER` never extended, no dose escalation. **16W is fully representable from existing phase/stage semantics.**

## 13. Fallback compatibility

`BUILD_THRESHOLD_DEVELOPMENT`'s numeric range (`min 3, max 5`) is identical regardless of whether `FASTER_THAN_HM_INTRO` is eligible — only its *own* preferred baseline shifts (3 when intro is present, 4 when intro is absent, per the catalog's own pre-existing "0+4 fallback shape" fact), and both baselines fit inside the same authored range. `RACE_SPECIFIC_HM_DEVELOPMENT` and its `CURRENT_FITNESS_FALLBACK` already carry an identical numeric range in the real catalog today, so applying the same widened bounds to both symmetrically means no horizon's week-count depends on which pace-evidence branch is active. **Fallback compatibility confirmed for every horizon.**

## 14. Generic resolver simulation

Re-implemented `CatalogPhaseAllocationResolver`'s exact published algorithm (`ValidateStructure` bounds check, anchor-at-preferred, `DistributeByPriority`'s round-robin one-week-per-pass walk) verbatim in a standalone script and ran it against the Layer A data in §5 for all seven target weeks — see §9's table, which is this simulation's literal output, not a hand-authored guess. All seven horizons return `IsMathematicallyFeasible = true` under the real algorithm with zero code change. **No `GENERIC_RESOLVER_CAPABILITY_GAP` exists; the finding is purely `DATA_AUTHORITY_INSUFFICIENT`, now resolved by this phase's proposed data.**

## 15. Authority ownership check

- `HALF_MARATHON_MASTER`: owns every Layer A number above. All target fields (`minimumWeeks`/`preferredWeeks`/`maximumWeeks`/`compressionPriority`/`extensionPriority`) already exist in the schema and in the real template file today (currently pinned) — **no new schema field is required**, only widened values.
- `HALF_MARATHON_WORKOUT_PROGRESSION`: owns every Layer B number above. `minimumExposures`/`maximumExposures`/`compressionBehavior`/`extensionBehavior` likewise already exist as real schema fields — **no new schema field is required**.
- `CatalogPhaseAllocationResolver`: owns only the generic mechanical distribution algorithm, confirmed unmodified and untouched by this phase (§14). It holds and invents no HM-specific numeric preference — no duplicate authority found.

## 16. Evidence / product-default classification

| Value | Classification |
|---|---|
| FOUNDATION min=2 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (base-phase elasticity is a coaching-literature-informed *class* of reasoning already used for 10K; the specific number 2 for HM is a fresh product default) |
| FOUNDATION max=4 | `PRODUCT_DEFAULT` (sized to exactly close the 16W ceiling together with Build's headroom) |
| BUILD min=3 (via dropping INTRO) | `DIRECT_EXISTING_AUTHORITY` (3 is literally `BUILD_THRESHOLD_DEVELOPMENT`'s own already-authored preferred exposure count today, now also serving as Build's floor) |
| BUILD max=5 | `PRODUCT_DEFAULT` (one repetition of an existing stage, sized to exactly close the 16W ceiling) |
| RACE_SPECIFIC min=3 (via dropping INTRO, halving DEVELOPMENT) | `PRODUCT_DEFAULT` — explicitly **not** `EVIDENCE_BACKED`: no source establishes that `HM_DEVELOPMENT` can compress to exactly 1 exposure; this is a product judgment call, stated as such |
| RACE_SPECIFIC max=5 | `DIRECT_EXISTING_AUTHORITY` (unchanged from the already-frozen preferred value; zero new ceiling invented) |
| TAPER 2/2/2 | `DIRECT_EXISTING_AUTHORITY` (unchanged, already frozen, not reopened) |

## 17. Remaining conflicts

None found. Every horizon in `[10,16]` resolves deterministically; both hard tests (§11, §12) pass; no tie required a `PRODUCT_DECISION_REQUIRED` escalation.

## 18. Recommended implementation phase

A future `HM.5` would: (1) widen the seven now-frozen numeric fields in `half-marathon-master.v1.json` and `half-marathon-workout-progression.v1.json` exactly as authored in §5/§6 (pure catalog data edit, zero code change, per §15); (2) re-run `PlanCatalog.Tests` for hash/schema regression; (3) confirm the real `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` now returns `IsMathematicallyFeasible=true` for all seven horizons against the *real* loaded candidate (not the simulation); (4) still leave every non-14W horizon **fail-closed at the public/Runway/dynamic-Core-wiring layer**, exactly as HM.2.1 left it — this phase authors allocation *data*, not activation.

## Final classification

`HM_10_16W_PHASE_AND_STAGE_HEADROOM_AUTHORITY_CLOSED`
