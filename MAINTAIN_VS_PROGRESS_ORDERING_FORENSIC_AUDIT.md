# Maintain vs. ProgressAsPlanned Ordering — Forensic Audit

**Status: AUDIT EVIDENCE.** No runtime math, rounding behavior, or tolerance value was changed to produce this document.

## Methodology (C1/C3)

Same deterministic generation as the permanent `MaintainNotExceedingProgressAsPlannedInvariantTests` (seed `Random(42)`, 200 iterations, identical input ranges) — extended, in a temporary audit-only fixture (deleted after this document was written), to record per-case metadata and export the full result set to `maintain-vs-progress-catalog-distribution.json`.

**Pre-materialization comparison basis:** Rev4/Rev4.1 define `Maintain = PriorValidatedCheckpointLoad` (held verbatim, never processed by `CatalogProgressionStep`) and `ProgressAsPlanned = CatalogProgressionStep(ValidatedSustainableLoad(window))`. For a fair single-step comparison, this audit feeds the **same** starting evidence value as both "the anchor Maintain holds" and "the anchor ProgressAsPlanned's `ValidatedSustainableLoad` input is." Under this construction, the two anchors are **identical by definition before materialization runs** — this is not an empirical measurement but a direct, structural consequence of the two formulas (one is raw, one is a function of the same raw value). The audit fixture computes and reports this pre-materialization comparison explicitly rather than assuming it, and it confirms the expected result (§ "Pre- vs. post-materialization," below).

**Post-materialization comparison:** the real, unmodified `LongHorizonGeNumericExecutor.Execute` (the actual General Endurance catalog progression step — the same code the real GE materializer path uses) is run against each case's starting evidence, and week 1's output (`numeric[0].TotalVolumeKm`) is compared against the held Maintain value.

**Catalog metadata captured per case:** `LongHorizonGeStructuralSelector.Select`'s week-1 descriptor's `IsRecoveryWeek`, `StageFamily`, `MesocycleIndex`, and `MesocyclePosition` — the real, unmodified structural classification the catalog itself assigns to that week, not an inference from the numeric output alone.

## C1. Export

`maintain-vs-progress-catalog-distribution.json` (183 rows, one per valid case) — created in the repo root. Each row includes: case id, all four input parameters, feasibility, the week-1 catalog classification fields above, both anchors, pre- and post-materialization absolute/relative deviation, the strict-violation flag, and a per-case root-cause classification.

## C2/C3. Root-cause correlation and pre-/post-materialization counts

| Metric | Count |
|---|---|
| Valid cases | 183 |
| Strict violations (`Maintain > ProgressAsPlanned`, post-materialization) | 94 |
| **Pre-materialization strict violations** | **0** |
| **Post-materialization strict violations** | **94** |
| **Violations created only by rounding/allocation** | **94 (100%)** |
| **Violations already present before rounding** | **0 (0%)** |
| Violations occurring on a catalog-classified recovery week (`Week1IsRecoveryWeek=true`) | **0** |
| Violations occurring on an ordinary (non-recovery) forward-growth week | **94 (100%)** |
| Unexplained violations (not attributable to rounding, recovery, or phase transition) | **0** |

**Direct answers to C2's five questions:**
1. **How many of the 94 occur on known cutback/step-back weeks?** **Zero.** Every one of the 94 violating cases' week-1 descriptor has `IsRecoveryWeek=false` — confirmed from the real catalog structural selector's own classification, not inferred from the numeric result.
2. **How many occur on phase transitions?** All 94 cases are week-1 of a checkpoint window, which is always the first week of that window's own `LongHorizonGeStructuralSelector.Select` output — the classification field (`StageFamily`/`MesocyclePosition`, e.g. `BaseDevelopment`/`Development1`) shows ordinary progression-stage weeks throughout the violating set, not a special transition marker. No case's classification indicates a phase-boundary week distinct from ordinary forward development.
3. **How many occur during ordinary forward-growth weeks?** **All 94 (100%).**
4. **How many are explained only by per-session rounding?** **All 94 (100%)** — see the analytical argument above (pre-materialization deviation is exactly 0 for every case; the entire deviation is introduced between the pre-materialization anchor and the post-materialization, session-allocated total).
5. **How many remain unexplained?** **Zero.**

## Pre- vs. post-materialization

Confirmed both analytically (from the formulas themselves) and empirically (every one of the 183 exported rows shows `PreMaterializationAbsoluteDeviation = 0` exactly): the anchor-selection layer (`NextWindowNumericAnchorSelector`, Rev4 §7) never introduces any ordering violation on its own. The entire 94-case violation set is introduced strictly inside `CatalogProgressionStep`'s own real session-distance allocation/rounding (`LongHorizonGeNumericExecutor` → `FourDaySessionDistanceAllocationPolicy`), which snaps individual session distances to its own discrete grid, and whose summed output can land fractionally below the raw un-rounded baseline input even on an otherwise-ordinary growth week.

**This answers the audit's central question (C):** the 1.5% tolerance is covering **(A) numeric presentation/allocation noise**, not **(B) a deeper progression semantic difference**. There is no periodization interaction being masked — Maintain and ProgressAsPlanned's own pre-materialization anchor are the same number; only the catalog's own rounding-to-discrete-session-distances step introduces the gap.

## C4. Distribution

Relative-deviation statistics across the 94 violating cases:

| Statistic | Value |
|---|---|
| Median | 0.391% |
| P75 | 0.589% |
| P90 | 0.849% |
| P95 | 1.016% |
| P99 | 1.361% |
| **Max** | **1.361%** (matches the earlier-reported 1.36%, rounding difference only) |
| Median absolute | 0.129 km |
| Max absolute | 0.247 km |

Bucket counts (relative deviation, 94 total):

| Bucket | Count | Share |
|---|---|---|
| 0.00–0.25% | 35 | 37.2% |
| 0.25–0.50% | 25 | 26.6% |
| 0.50–0.75% | 17 | 18.1% |
| 0.75–1.00% | 12 | 12.8% |
| 1.00–1.25% | 4 | 4.3% |
| 1.25–1.50% | 1 | 1.1% |

The distribution is **monotonically decreasing and right-tailed**: 63.8% of all violations fall under 0.5%, and only a single case (1.1%) falls in the final 1.25–1.50% bucket, immediately below the proposed tolerance ceiling. This is not a distribution artificially clustered just under an arbitrary cutoff — it tapers naturally, and the 1.5% ceiling sits comfortably above the entire observed tail with a small margin (1.5% vs. the observed max of 1.361%), not exactly at it.

Because every violation shares the same root cause (§C2/C3: 100% rounding-only, 0% recovery-week, 0% unexplained), no further grouping by phase or cutback status is meaningful — there is only one group.

## C5. Governance verdict

### **ROUNDING_TOLERANCE_WELL_SUPPORTED_AS_PRODUCT_DEFAULT**

All four requirements are met:
- Strict anchor ordering holds exactly before materialization (0/183 pre-materialization violations — proven both structurally and empirically).
- Violations arise exclusively from deterministic allocation/rounding (94/94 classified `SESSION_ALLOCATION_ROUNDING_ONLY`; 0/94 correlate with a catalog-classified recovery/cutback week).
- The distribution is tightly bounded and naturally tapering (median 0.391%, 90th percentile 0.849%, max 1.361% — comfortably within the proposed 1.5% ceiling with margin, not clustered at the boundary).
- No unexplained structural violations exist (0/94).

This verdict is evidence for the user's own review of the `PROPOSED_FOR_USER_FREEZE` clause in `REV4_TO_REV4_1_CANONICAL_RECONCILIATION.md` — it is not itself a freeze of that clause.
