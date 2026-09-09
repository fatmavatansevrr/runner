# PHASE HM.3 — 10–16W Dynamic Core Phase-Allocation Authority Closure

Governance only. No production code, catalog data, or test file was modified by this phase.

## 1. Objective recap

Resolve the exact deterministic `FoundationWeeks/BuildWeeks/RaceSpecificWeeks/TaperWeeks` allocation for every Half-Marathon Intermediate×4D Core horizon in the frozen canonical range `[10,16]`, reusing (never reopening) HM's already-frozen core bounds (`10/14/16`), the frozen `14W = 3/4/5/2` preferred allocation, the workout-stage catalog, taper duration authority, peak-volume authority, and long-run authority.

## 2. Terminology premise correction

The governing prompt names four stage properties: `MANDATORY_EXPOSURE`, `COMPRESSIBLE`, `REPEATABLE_FOR_EXTENSION`, `NON_REPEATABLE`. I read the real schema (`plan-catalog/schemas/workout-progression.schema.json` lines 47–48) and the real HM catalog data directly. The actual, only vocabulary that exists is:

- `compressionBehavior`: `COMPRESSIBLE` | `PROTECTED`
- `extensionBehavior`: `EXTENDABLE` | `FIXED_EXPOSURE`

None of `MANDATORY_EXPOSURE`, `REPEATABLE_FOR_EXTENSION`, or `NON_REPEATABLE` exist anywhere in the schema or any catalog file (confirmed by direct read, not grep-miss). This is reported the same way HM.1.2's `HM-LIT-06`–`11` citation gap was reported: the prompt's premise doesn't match the real artifact, so this closure substitutes the real vocabulary rather than inventing a mapping. Practically: `PROTECTED` + `FIXED_EXPOSURE` together are the closest real-world analogue to the prompt's intended "non-repeatable/mandatory" concept, and `COMPRESSIBLE` + `EXTENDABLE` together are the closest analogue to "compressible/repeatable for extension" — but they are not the same axis (a stage can be `COMPRESSIBLE` and `FIXED_EXPOSURE` simultaneously in principle, though no such stage exists in HM's real data today).

## 3. Existing generic mechanism confirmed reusable

Read `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocation.cs` in full. `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` is already fully generic and distance-agnostic:

- Anchors at each phase's `PreferredWeeks`.
- For `targetWeekCount < sumPreferred`: walks phases in ascending `CompressionPriority` order, decrementing one week at a time, never below a phase's own `MinimumWeeks`.
- For `targetWeekCount > sumPreferred`: walks phases in ascending `ExtensionPriority` order, incrementing one week at a time, never above a phase's own `MaximumWeeks`.
- Returns `IsMathematicallyFeasible = false` with an explicit reason code if `targetWeekCount` falls outside `[sumMinimum, sumMaximum]`.

This is the exact same mechanism already proven for `TEN_K_MASTER v6`'s 8–14 week range (`PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md`, re-confirmed by direct read of that report). **No new allocator, no HM-specific allocation code, and no per-horizon master template is required** — confirmed per the prompt's own verification requirement. The only thing that can make HM's 10–16W range feasible is authoring wider `MinimumWeeks`/`MaximumWeeks` values on HM's four existing phase rows (and, if extension requires it, wider exposure bounds on the underlying workout-progression stages) — pure catalog data, zero new code.

## 4. Real current catalog state (read directly, not summarized)

`plan-catalog/catalog/templates/half-marathon-master.v1.json`:

| Phase | Min | Preferred | Max | CompressionPriority | ExtensionPriority | isCompressionProtected |
|---|---|---|---|---|---|---|
| FOUNDATION | 3 | 3 | 3 | 1 | 1 | false |
| BUILD | 4 | 4 | 4 | 2 | 2 | false |
| RACE_SPECIFIC | 5 | 5 | 5 | 3 | 3 | false |
| TAPER | 2 | 2 | 2 | 4 | 4 | true |

`plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` (only the active-path stage of each fallback pair listed):

| Phase | Stage | min/max exposures | compressionBehavior | extensionBehavior |
|---|---|---|---|---|
| FOUNDATION | `FOUNDATION_AEROBIC_BASE` | 3/3 | COMPRESSIBLE | EXTENDABLE |
| BUILD | `BUILD_FASTER_THAN_HM_INTRO` | 1/1 | COMPRESSIBLE | EXTENDABLE |
| BUILD | `BUILD_THRESHOLD_DEVELOPMENT` | 3/3 | COMPRESSIBLE | EXTENDABLE |
| RACE_SPECIFIC | `RACE_SPECIFIC_HM_INTRODUCTION` | 1/1 | COMPRESSIBLE | EXTENDABLE |
| RACE_SPECIFIC | `RACE_SPECIFIC_HM_DEVELOPMENT` (or fallback) | 2/2 | COMPRESSIBLE | EXTENDABLE |
| RACE_SPECIFIC | `RACE_SPECIFIC_HM_REHEARSAL` (or fallback) | 2/2 | **PROTECTED** | **FIXED_EXPOSURE** |
| TAPER | `TAPER_HM_ACTIVATION` (or fallback) | 2/2 | **PROTECTED** | **FIXED_EXPOSURE** |

## 5. Central finding: zero authored headroom exists anywhere today

Every phase's `Minimum/Preferred/Maximum` triple is `(N,N,N)` — identical to the value HM.2.1 already independently proved makes the resolver return `IsMathematicallyFeasible=false` for every horizon except 14. Every workout-progression stage's `minimumExposures/maximumExposures` pair is likewise `(N,N)` — including the stages carrying `COMPRESSIBLE`/`EXTENDABLE` labels. Those labels describe **capability class**, not an authored numeric range: nothing in the real catalog data today permits `FOUNDATION_AEROBIC_BASE` to run 2 or 4 weeks instead of exactly 3, despite being labeled `EXTENDABLE`.

This means the phase-week floor implied by the *current* stage catalog for every phase equals its own preferred value exactly:

- `RACE_SPECIFIC`'s floor is provably `5` regardless of any future compression-priority reasoning: `RACE_SPECIFIC_HM_REHEARSAL` (or its fallback) is `PROTECTED`/`FIXED_EXPOSURE` at exactly 2 — an absolute, already-frozen floor (this is real existing authority, not new) — plus `HM_DEVELOPMENT`'s pinned 2 plus `HM_INTRODUCTION`'s pinned 1 = 5, with the latter two currently carrying zero authored slack.
- `TAPER`'s floor and ceiling are both `2` permanently: `isCompressionProtected: true` at the phase level, and its sole active stage is `PROTECTED`/`FIXED_EXPOSURE`. This is already-frozen taper duration authority (out of scope to reopen) and independently confirms `TaperWeeks = 2` for every horizon in `[10,16]` — the one row of the required table answerable with zero new authority.
- `FOUNDATION`'s and `BUILD`'s floors and ceilings are likewise pinned at their preferred values today, purely because no one has yet authored a wider number — not because of any structural or evidence-based constraint discovered this phase.

## 6. Priority-order conclusion (real, derivable, not new)

`CompressionPriority` and `ExtensionPriority` are identically ordered today: `FOUNDATION(1) < BUILD(2) < RACE_SPECIFIC(3) < TAPER(4)`. This exactly mirrors the already-approved 10K Foundation-first policy (`PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md` §7–8: Foundation/aerobic-base duration is coaching-literature-elastic, Build is comparatively fixed, and Taper is protected). **If** Min/Max headroom is authored in a future phase using this same ordinal pattern, compression below 14 weeks would remove from `FOUNDATION` first, then `BUILD`, then `RACE_SPECIFIC` (never `TAPER`); extension above 14 weeks would add to `FOUNDATION` first, then `BUILD`, then `RACE_SPECIFIC` (never `TAPER`). This directional conclusion requires no new evidence — it is a direct, mechanical reading of already-frozen priority ordinals — but it cannot by itself produce week-counts without a floor/ceiling number for at least one phase.

## 7. Required table — result

| Horizon | Foundation | Build | RaceSpecific | Taper | Status |
|---|---|---|---|---|---|
| 10W | ? | ? | ? | 2 | **UNRESOLVED — no authored headroom** |
| 11W | ? | ? | ? | 2 | **UNRESOLVED — no authored headroom** |
| 12W | ? | ? | ? | 2 | **UNRESOLVED — no authored headroom** |
| 13W | ? | ? | ? | 2 | **UNRESOLVED — no authored headroom** |
| 14W | 3 | 4 | 5 | 2 | Frozen (HM.1) |
| 15W | ? | ? | ? | 2 | **UNRESOLVED — no authored headroom** |
| 16W | ? | ? | ? | 2 | **UNRESOLVED — no authored headroom** |

Only `TaperWeeks = 2` is answerable for every row, and only because Taper's protection is already-frozen authority. Every other cell requires a number that does not exist anywhere in currently-frozen HM authority, and authoring one is explicitly out of this phase's scope (see §8).

## 8. Why this cannot be resolved without reopening forbidden scope

To fill in a single non-Taper cell for any horizon other than 14, at least one of the following must happen, and every option is explicitly forbidden by this phase's own governing instructions:

1. **Widen a phase's `MinimumWeeks`/`MaximumWeeks` on the master template** below/above its current pinned value — this is a genuine new numeric authority decision (e.g., "Foundation may run as few as 2 weeks" or "as many as 4"), not derivable from anything already frozen. The instruction explicitly warns against inventing new numeric constants without evidence, and no HM evidence source (`HM_21K_Claude_Handoff_and_Build_Plan.md`) was re-consulted or re-opened for this phase per the prompt's own scope fence — and even if consulted, doing so would materially resemble "reopening the 14W allocation," which is explicitly barred.
2. **Widen a workout-progression stage's `minimumExposures`/`maximumExposures`** (e.g., let `FOUNDATION_AEROBIC_BASE` run 2–4 exposures instead of exactly 3) — explicitly barred: *"Do not reopen: … workout-stage catalog"* and *"no new workout progression is invented."*
3. **Treat a currently-pinned stage as compressible/extendable in practice despite its authored bounds being equal** — this would silently contradict the catalog's own authored data rather than compute from it, which every phase in this engagement has treated as a fail-closed violation, not a shortcut.

There is no fourth path: the generic resolver mechanism (§3) is already correct and sufficient, but a mechanism with no headroom to distribute produces no allocation, by construction and by design (`IsMathematicallyFeasible=false` is the resolver's own honest, tested output for exactly this situation — confirmed live via HM.2.1's `DynamicCoreWeekSkeletonInfeasibleException` proof for all six non-14W horizons).

## 9. What IS closed by this phase (real progress, not a null result)

- Confirmed **no new allocation code or per-horizon template is architecturally required** — the existing generic `CatalogPhaseAllocationResolver` is already sufficient once (and only once) real Min/Max headroom is authored. This was an explicit verification requirement and is now closed: `ALLOCATOR_REUSE_CONFIRMED`.
- Confirmed **Taper is fixed at exactly 2 weeks for every horizon in [10,16]** — the one genuinely resolvable row, derived from already-frozen, already-protected authority (not new).
- Confirmed the **compression/extension priority order** (Foundation-first both directions) is already established and, if headroom is authored later, needs no re-derivation — it already matches the evidence-informed 10K precedent's own rationale class.
- Confirmed **RaceSpecific's real floor under the current catalog is exactly 5** (driven by its own already-protected `HM_REHEARSAL` block) — meaning if/when Min/Max authority is later opened, RaceSpecific's `MinimumWeeks` cannot honestly be set below 5 without first reopening the (currently forbidden) workout-stage catalog.
- Corrected the governing prompt's terminology premise (§2) so a future phase does not search the schema for property names that were never real.

## 10. Recurring-assumption-family check

Searched whether any other consumer assumes HM's phase Min/Max already has headroom (i.e., whether some other code path silently expects a non-14 horizon to work): none found. `CatalogPhaseAllocationResolver`, `CatalogFinalPrescribedPlanValidator`, and the dynamic Core pipeline (per HM.2.1's own direct trace) all correctly and consistently fail closed for every non-14 horizon today — no `LEGACY_ASSUMPTION` or `REQUIRES_PARAMETERIZATION` instance found; classification: `SAFE_SIBLING` across the board (10K's own equivalent resolver usage is unaffected — nothing in this phase touched TEN_K_MASTER or any 10K catalog file).

## 11. Files changed by this phase

None besides this report. No `.json` catalog file, no `.cs` production file, no test file, no `PHASE_LEDGER.md`/`MASTER_ROADMAP.md` row beyond appending this phase's own entry (added after this report; see commit).

## 12. Remaining gaps / next steps

A future phase (tentatively `HM.4`) would need to, **as its own explicit product-authority decision** (not mechanically derivable from anything frozen today):

1. Decide and freeze real `MinimumWeeks`/`MaximumWeeks` bounds for `FOUNDATION`, `BUILD`, and `RACE_SPECIFIC` (Taper stays `2/2/2`, permanently protected).
2. Correspondingly widen the workout-progression stage exposure bounds those phases would need to actually represent a shorter/longer phase (e.g., `FOUNDATION_AEROBIC_BASE`'s `maximumExposures` would need to exceed 3 to support a 4+ week Foundation; `RACE_SPECIFIC_HM_INTRODUCTION`/`HM_DEVELOPMENT` would need compressible headroom below their pinned values to support RaceSpecific below 5, since `HM_REHEARSAL` itself can never shrink).
3. Re-run this same table derivation once those two are real — at that point `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` requires no code change at all to produce every row.

## 13. Final classification

`HM_10_16W_DYNAMIC_CORE_ALLOCATION_AUTHORITY_BLOCKED`

**Exact smallest blocker:** every HM phase's `MinimumWeeks`/`MaximumWeeks` and every relevant workout-progression stage's `minimumExposures`/`maximumExposures` are currently authored as an exact point value equal to the frozen 14-week preferred allocation (zero real headroom anywhere), and authoring the headroom needed to resolve even one non-14W cell requires either a new phase-level numeric authority decision or reopening the workout-stage catalog — both explicitly out of this phase's scope. Do not force a number to close this table; per the governing prompt's own instruction, an unresolved tie/gap of this kind is a product decision for a future phase, not something to paper over here.
