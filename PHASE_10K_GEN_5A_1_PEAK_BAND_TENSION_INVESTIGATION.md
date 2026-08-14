# Phase 10K-GEN.5A.1 — Peak-Band Tension Investigation

**Numeric exploration only. No decision made, no code touched, no value selected. Both hypothesis ceilings below are working hypotheses only, not proposals.**

## 1. Method

Re-ran GEN.5 §B's real missing-readiness compounding sequence (start=12.0, 3D's frozen 7%/8%/2.0km-capped ratio-compounding, `Round0.5`) with `bounds.MaximumKm` actually applied as a per-step clamp — exactly as `CatalogVolumeAndLongRunPlanner.cs` line 141 does it (`threeDayReachable = Math.Min(candidate, bounds.MaximumKm)`, applied every iteration, confirmed load-bearing in GEN.5A §1) — under each of GEN.5A §4's two sanity-check-region hypothesis ceilings: **18 km** (upper) and **13 km** (lower).

## 2. Hypothesis ceiling = 18 km

| Weeks (transitions) | Unclamped reachable | Clamped reachable | Ceiling hit? |
|---|---|---|---|
| 8 (6) | 17.5 | 17.5 | not yet |
| 9 (7) | 18.5 | **18.0** | **first hit** |
| 10 (8) | 19.5 | 18.0 | plateaued |
| 11 (9) | 21.0 | 18.0 | plateaued |
| 12 (10) | 22.5 | 18.0 | plateaued |
| 13 (11) | 24.0 | 18.0 | plateaued |
| 14 (12) | 25.5 | 18.0 | plateaued |

Growth first hits the ceiling at **week 9** and stays permanently flat at 18.0 km for every subsequent week in the 8-14 window — confirmed mechanically: once `threeDayReachable == ceiling`, the next step's `candidate` is always computed as `ceiling + positive increase`, which the same-iteration `Math.Min(candidate, ceiling)` clamps straight back down to `ceiling`. No week ever exceeds it again.

**Taper re-check (not assumed — recomputed at every horizon):**

| Weeks | Clamped pre-taper | Taper (×0.53, Round0.5) | vs 12.0km floor |
|---|---|---|---|
| 8 | 17.5 | 9.5 | INELIGIBLE (unchanged from GEN.5 §B — ceiling not yet reached) |
| 9 | 18.0 | 9.5 | INELIGIBLE (was already INELIGIBLE in §B, unaffected) |
| 10 | 18.0 | 9.5 | INELIGIBLE (unchanged) |
| 11 | 18.0 | 9.5 | INELIGIBLE (unchanged) |
| 12 | 18.0 | 9.5 | **INELIGIBLE — was ELIGIBLE (12.0) in GEN.5 §B** |
| 13 | 18.0 | 9.5 | **INELIGIBLE — was ELIGIBLE (12.5) in GEN.5 §B** |
| 14 | 18.0 | 9.5 | **INELIGIBLE — was ELIGIBLE (13.5) in GEN.5 §B** |

**Under the 18km hypothesis, all seven horizons (8-14) become `PRODUCT_INELIGIBLE`.** The three horizons GEN.5 §B found eligible (12-14) flip to ineligible once the ceiling is actually enforced.

## 3. Hypothesis ceiling = 13 km

| Weeks (transitions) | Unclamped reachable | Clamped reachable | Ceiling hit? |
|---|---|---|---|
| 4 (2) | 13.5 | **13.0** | **first hit** (well before the 8-14 window even starts) |
| 8-14 (6-12) | — | 13.0 | plateaued for the entire window |

Growth hits this lower ceiling at week 4 — before the Core window even begins — so every one of weeks 8-14 sees an identical, fully plateaued 13.0 km pre-taper value.

**Taper re-check:** `Round0.5(13.0 × 0.53) = Round0.5(6.89) = 7.0` km — **below the 12.0km floor at every single horizon, 8 through 14.** All seven `PRODUCT_INELIGIBLE`, uniformly.

## 4. Explicit-zero (not investigated further, per instruction)

GEN.5 §B already found explicit-zero universally `PRODUCT_INELIGIBLE` across 8-14 weeks with an *unclamped* ceiling. Clamping to either 18km or 13km can only ever reduce reachable growth further (clamps are a `Min`), so explicit-zero remains ineligible at every horizon under both hypotheses trivially — not recomputed, per instruction.

## 5. Headroom / margin comparison

**Neither hypothesis produces a single eligible horizon in 8-14 weeks** — so there is no "shortest surviving horizon" to measure margin at. Reporting the shortfall instead (how far *below* the 12.0km floor the least-bad horizon lands), compared against every other already-frozen cell's real margins:

| Cell | Representative horizon | Taper value | Floor | Margin |
|---|---|---|---|---|
| Intermediate×3D missing-readiness (frozen, `Gen3AThreeDayEligibilityMatrixTests`) | week 8 | 12.5 | 12.0 | **+0.5 km** |
| Intermediate×3D missing-readiness | week 10 | 14.5 | 12.0 | **+2.5 km** |
| Beginner×4D missing-readiness (frozen, GEN.4C.4) | week 8 | 9.5 | 9.0 | **+0.5 km** |
| Beginner×4D missing-readiness | week 14 | 12.0 | 9.0 | **+3.0 km** |
| **Beginner×3D, 18km hypothesis** | every horizon 8-14 | 9.5 | 12.0 | **−2.5 km** |
| **Beginner×3D, 13km hypothesis** | every horizon 8-14 | 7.0 | 12.0 | **−5.0 km** |

**Every other real, already-implemented cell in this entire engagement clears its taper floor with positive margin (+0.5 to +3.0 km) somewhere in its 8-14 week range.** Under either sanity-check-consistent ceiling hypothesis, Beginner×3D does not merely have a *tighter* margin — it has **no positive margin anywhere in the window at all**, missing the floor by 2.5-5.0 km uniformly across all seven horizons. This is a categorically different (worse) outcome than "tight but survivable," not a matter of degree.

## 6. What this means for the tension (reported, not resolved)

If the sanity-check region (~13-18km, physiologically motivated by Intermediate's real 3D<4D pattern) is the one the eventual product decision adopts, **Beginner×3D missing-readiness has zero eligible horizons anywhere in the 8-14 week Core window** — the same qualitative outcome GEN.5 §B already found for explicit-zero. If instead the data-grounded region (~22.5-29.5km, GEN.5A §2) is adopted, GEN.5 §B's original finding stands (8-11 ineligible, 12-14 eligible). The two hypotheses do not just disagree on numbers — they disagree on whether Beginner×3D has *any* viable Core-window offering at all for missing-readiness. This sharpens, rather than resolves, the tension GEN.5A disclosed; it is handed to the product-decision phase as-is.

## 7. Final classification

```
PEAK_BAND_TENSION_QUANTIFIED_READY_FOR_PRODUCT_DECISION
```
