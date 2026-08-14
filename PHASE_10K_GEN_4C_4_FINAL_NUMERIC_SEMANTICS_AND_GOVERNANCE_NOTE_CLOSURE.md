# Phase 10K-GEN.4C.4 — Final Numeric Semantics & Governance Note Closure

**Verification/documentation follow-up only. No production code, no catalog mutation, no product policy change, no public rollout, no new literature review.**

## 1. Scope

Close exactly two items before GEN.4D: (A) disambiguate the GEN.4C.3 "12.0/10" phrase; (B) record the 3D/4D progression-shape observation as governance debt, without investigating or resolving it.

## 2. Meaning of "12.0/10"

**Exact source location**: GEN.4C.3 §14 ("Final Beginner resolved reference"): *"`ResolvedPeakReference = { ValueKm: 21.0, StartingReferenceKm: 12.0 (...), TransitionsReference: 10 (...), Provenance: PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE }`"*, restated in §20/§23 as "values 12.0/21.0/10," and abbreviated in the final chat report as "12.0/10 as the paired reference-start/transitions."

Traced precisely against the actual calculation (GEN.4C.3 §12-14, and the underlying `CatalogVolumeAndLongRunPlanner.ResolvePeak` formula it mirrors):

- **`12.0`** = `StartingReferenceKm` — the Beginner-instance equivalent of `GoldenFixtureStartingVolumeKm`, one of the two operands in `canonicalDefaultMultiplier = ResolvedPeakKm / StartingReferenceKm`. Set equal to Beginner's already-approved **missing-readiness** starting default (GEN.4C §6), by deliberate, disclosed choice (GEN.4C.3 §14) — **not** related to, and not a stand-in for, the 9.5 km explicit-zero value.
- **`10`** = `TransitionsReference` — the Beginner-instance equivalent of `GoldenFixtureNonTaperTransitions`. **This is a pure, dimensionless integer count of week-to-week growth transitions**, reused unchanged from the existing canonical reference: a 12-week Core plan has 1 taper week and 11 non-taper weeks, giving `transitions = 11 - 1 = 10` (matching the planner's own `transitions = max(0, nonTaperWeeks - 1)` formula evaluated at the canonical 12-week horizon). It is **not** a kilometer value, not a policy value in the sense of a training-load number, not a planner input distinct from what it plainly is, not a rounded display of `9.5`, and has **no relationship whatsoever** to the explicit-zero starting volume or to any taper-floor arithmetic (which uses `9.0`/`17.0`/`0.53`, entirely separate constants).

**Direct answers to the required checklist**: not an actual km policy value (it has no km unit at all); it is the actual planner-consumed `GoldenFixtureNonTaperTransitions`-equivalent input (a real, used field, not a decorative one); it *is* the transition-count denominator itself, precisely; it is not a rounded display of 9.5 (no arithmetic relationship exists between 10 and 9.5 anywhere in this computation); it is not a shorthand/example — it is the literal integer used in every §15/§16 row of GEN.4C.3's tables (visible as `transitions=6,7,8,9,10,11,12` for horizons 8-14, with `10` appearing specifically at the 12-week row as the transitions count, not as any other kind of reference); it is unrelated to explicit-zero starting volume and unrelated to taper-floor arithmetic, confirmed by direct inspection of every formula it appears in.

## 3. Frozen numeric-policy reconfirmation

| Value | Frozen figure | Status this phase |
|---|---|---|
| Missing starting volume | 12.0 km | Unchanged |
| Explicit-zero starting volume | 9.5 km | Unchanged |
| General Beginner 4D floor | 9.0 km | Unchanged |
| Taper floor | 9.0 km | Unchanged |
| Taper multiplier | 0.53 | Unchanged |
| Pre-taper break-even | 17.0 km | Unchanged |
| Beginner `ResolvedPeakReference` | 21.0 km | Unchanged |
| `PeakVolumeBand` | 18.0-24.0 km | Unchanged |

**No 10.0-km runtime/product value exists anywhere in the approved Beginner policy.** The only "10" in the entire GEN.4C.3 decision is the dimensionless transitions-count integer (§2), categorically different in kind — not merely in magnitude — from any of the km-denominated figures above. No value silently drifted from 9.5 to 10.0 or from any other approved figure to 10.0 at any point in this phase's re-inspection.

**Classification: `GEN4C3_NUMERIC_SEMANTICS_UNCHANGED`.** `GEN4C3_NUMERIC_POLICY_DRIFT_FOUND` does not apply.

## 4. Break-even reconfirmation

Re-verified against the actual `Round0.5(x) = Math.Round(x/0.5, MidpointRounding.AwayFromZero) × 0.5` implementation (unchanged, re-confirmed present in `CatalogVolumeAndLongRunPlanner.cs`):

| Pre-taper | Raw (× 0.53) | Rounded (production-equivalent) | vs. 9.0 km floor |
|---|---|---|---|
| 16.0 | 8.48 | 8.5 | **FAIL** |
| 16.5 | 8.745 | 8.5 | **FAIL** |
| 17.0 | 9.01 | 9.0 | **PASS** |
| 17.5 | 9.275 | 9.5 | **PASS** |

Exact match to the values stated in the binding prompt and to every prior computation across GEN.4C.1/4C.2/4C.3 — no discrepancy found. **Classification: `TAPER_BREAK_EVEN_17KM_RECONFIRMED`.**

## 5. Final missing-readiness matrix

No runtime input touched by the §2 clarification (`10` was never a km value feeding this matrix's eligibility comparisons in the first place — it only ever fed the dimensionless `transitionAdjustedMultiplier` ratio, exactly as intended). Matrix not recomputed, per instruction, since no ambiguity actually reached runtime inputs:

```
8  ELIGIBLE
9  ELIGIBLE
10 ELIGIBLE
11 ELIGIBLE
12 ELIGIBLE
13 ELIGIBLE
14 ELIGIBLE
```

**Confirmed authoritative, unchanged from GEN.4C.3 §15.**

## 6. Final explicit-zero matrix

Same confirmation, no recomputation required:

```
8  PRODUCT_INELIGIBLE
9  PRODUCT_INELIGIBLE
10 PRODUCT_INELIGIBLE
11 PRODUCT_INELIGIBLE
12 PRODUCT_INELIGIBLE
13 ELIGIBLE
14 ELIGIBLE
```

**Confirmed authoritative, unchanged from GEN.4C.3 §16.**

**Classification for both: `GEN4C3_FINAL_MATRICES_REMAIN_AUTHORITATIVE`.**

## 7. 3D/4D progression-shape governance observation

**`POSSIBLE_CROSS_FREQUENCY_VOLUME_PROGRESSION_SHAPE_DIVERGENCE`** — recorded as an observed architectural fact only, not investigated or resolved further in this phase:

- The 3D policy branch (`ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayIntermediate)`) uses ratio-compounding week-to-week growth (driven by `PreferredMaxWeeklyIncreaseRatio`/`HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm`), confirmed by direct code read in GEN.4C.3 §8.
- The 4D policy path (both `.Default`/Intermediate and the new Beginner instance) uses resolved-reference linear interpolation, confirmed throughout GEN.4C.1-4C.3.
- **This divergence is not caused by Beginner** — it exists between Intermediate×3D and Intermediate×4D, predating any Beginner×4D work, discovered incidentally during GEN.4C.3's Path Y evaluation.
- **It predates Beginner×4D implementation** entirely; Beginner×4D was never routed through the 3D branch and continues to use the approved, unmodified 4D generic interpolation path (Path Z, GEN.4C.3).
- **GEN.4C.3 intentionally did not resolve** whether this divergence is a deliberate Frequency-specific domain distinction or an unexamined historical specialization — that question was explicitly out of scope for the peak-reference decision and remains open.
- **Intermediate×4D remains fully unchanged** by any decision in this phase or GEN.4C.3.

**Technical debt recorded: `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001`** — *"Audit whether 3D and 4D volume-progression algorithm-shape divergence is an intentional Frequency policy distinction or historical specialization before future 5D+ generalization."* **This debt does not block GEN.4D** and its audit is not scheduled here, per explicit instruction.

## 8. Explicit-zero matrix revision history (preserved, not rewritten)

| Phase | Result | Reason |
|---|---|---|
| GEN.4C | 8-13 `PRODUCT_INELIGIBLE`, 14 `ELIGIBLE` | Matrix used an incorrect 7%-compounding approximation instead of the real 4D planner algorithm |
| GEN.4C.2 (provisional) | 8-11 `PRODUCT_INELIGIBLE`, 12-14 `ELIGIBLE` | Real linear-interpolation algorithm applied, but with the disputed 22.0 km reference improperly derived from Intermediate |
| GEN.4C.3 (final) | 8-12 `PRODUCT_INELIGIBLE`, 13-14 `ELIGIBLE` | Path Z resolved truthful peak-reference authority; replaced the invalid 22.0 km value with the independently-selected 21.0 km `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` reference |

**Required statement, affirmed: the final matrix changed because upstream model/provenance inputs were corrected across three successive phases, never because eligibility rules themselves were relaxed.** At no point was the taper multiplier (0.53, unchanged throughout), any session minimum (KEY 3.0/EASY 1.5, unchanged throughout), role count (1 KEY + 2 EASY + 1 LONG, unchanged throughout), the structural floor (9.0 km, unchanged throughout), or the eligibility architecture itself (typed `PRODUCT_INELIGIBLE` routing, unchanged throughout) ever weakened to produce a more favorable result — every change in the table above traces to a fix of a genuine calculation-model or provenance defect, verified and disclosed at each step, never to a loosened constraint.

## 9. GEN.4D governance carry-forward notes

A. Authoritative Beginner peak reference: **21.0 km**, provenance **`PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`** — never `GOLDEN_FIXTURE_DERIVED`.
B. Intermediate's 38.0 km remains **`GOLDEN_FIXTURE_DERIVED`**, zero behavior/provenance change.
C. Planner algorithm (`CatalogVolumeAndLongRunPlanner`) remains unchanged.
D. Path Z requires only a small provenance/authority generalization (§23 of GEN.4C.3) — not a new progression engine.
E. Final explicit-zero eligibility: 8-12 blocked, 13-14 eligible.
F. Final missing-readiness eligibility: 8-14 eligible, all seven horizons.
G. FARTLEK/THRESHOLD_TEMPO interval/repetition capability gap remains unrelated and non-Beginner-specific (GEN.4C §12).
H. `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001` is recorded (§7) but does not block Beginner×4D implementation.

## 10. GEN.4D readiness

All required conditions hold: `GEN4C3_NUMERIC_SEMANTICS_UNCHANGED` (§3), `TAPER_BREAK_EVEN_17KM_RECONFIRMED` (§4), `GEN4C3_FINAL_MATRICES_REMAIN_AUTHORITATIVE` (§5-6), and no hidden 10.0 km policy drift exists anywhere (§2-3). **`GEN4D_READY`.**

## 11. Final classification

```
BEGINNER_4D_GEN4C_FINAL_NUMERIC_SEMANTICS_VERIFIED
GEN4D_READY
```
