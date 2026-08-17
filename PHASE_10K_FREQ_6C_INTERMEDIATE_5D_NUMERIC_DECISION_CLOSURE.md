# Phase 10K-FREQ.6C — Intermediate 5D Numeric Authority Decision Closure

**Product decision. Final values selected from FREQ.6B's evidence envelopes only — nothing outside them, nothing imported from 4D by ratio. No code touched (5D has zero runtime implementation to touch — no RUN_LAYOUT_5D exists yet).**

## A. Final values selected

| Authority | Envelope (FREQ.6B) | Selected | Rationale |
|---|---|---|---|
| Missing-readiness starting volume | ~24-28 km | **26.0 km** | Direct evidence anchor — Hal Higdon's real Week-1 5-day total, not a midpoint-of-a-vague-range guess |
| Explicit-zero starting volume | ~18-21 km | **19.5 km** | `26.0 × 0.75`, reusing the exact ratio 4D's own real missing:explicit-zero pair (16:12) already establishes — the weaker of the two envelopes (FREQ.6B disclosed no direct 5-day explicit-zero source exists), so anchored to the one real, precedented ratio available rather than picked freehand |
| Resolved peak reference | ~42-47 km | **44.5 km** | Center of Higdon's real, directly-evidenced peak estimate (43-46km), inside the wider corroborating envelope. `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` — never `GoldenFixtureDerived`, per explicit prohibition |
| KEY2:KEY1 relative dose | ~60-80% | **70%** | Midpoint of the Norwegian-method-anchored envelope; this is a stored authority for the *future* prescription-profile capability FREQ.6A found missing, not a change to FREQ.4's current equal-distance-split mechanism (which remains unmodified) |
| Long-run selection share | ~25-32% | **28%** | Below-midpoint, reflecting the weaker (directional-only) evidence precision disclosed in FREQ.6B §5 |
| Long-run hard cap | ~35-38% | **36%** | Gives an 8-point selection-to-cap margin, comparable in style to 4D's own 7-point margin (33%→40%), confirmed `28% < 36%` with real margin |

## B. Taper-floor check with final numbers — real result, not the envelope-bound scenario FREQ.6B flagged

Used FREQ.3's confirmed mechanism exactly (4D-style: `canonicalDefaultMultiplier = ResolvedPeakReference / GoldenFixtureStartingVolumeKm`, `transitionAdjustedMultiplier = 1 + (canonicalDefaultMultiplier-1)×transitions/GoldenFixtureNonTaperTransitions`, `reachable = startingVolumeKm × transitionAdjustedMultiplier`), following the exact precedent GEN.4C.3/GEN.4C.4 established for Beginner×4D: `GoldenFixtureStartingVolumeKm` set to the missing-readiness value itself (26.0, self-referential, not borrowed from 4D), `GoldenFixtureNonTaperTransitions = 10` (the same Level/Frequency-independent 12-week canonical reference used everywhere else in this engagement).

`canonicalDefaultMultiplier = 44.5 / 26.0 = 1.71154`.

**Missing-readiness (start = 26.0km) — every horizon:**

| Weeks | Reachable | Taper (×0.53, Round0.5) | vs 9.0km partial floor |
|---|---|---|---|
| 8 | 37.0 | 19.5 | Clears by 10.5km |
| 9 | 39.0 | 20.5 | Clears by 11.5km |
| 10 | 41.0 | 21.5 | Clears by 12.5km |
| 11 | 42.5 | 22.5 | Clears by 13.5km |
| 12 | 44.5 (exactly the resolved peak reference, by construction — transitions=10=GoldenFixtureNonTaperTransitions) | 23.5 | Clears by 14.5km |
| 13 | 46.5 | 24.5 | Clears by 15.5km |
| 14 | 48.0 | 25.5 | Clears by 16.5km |

**Explicit-zero (start = 19.5km) — every horizon:**

| Weeks | Reachable | Taper (×0.53, Round0.5) | vs 9.0km partial floor |
|---|---|---|---|
| 8 | 28.0 | 15.0 | Clears by 6.0km |
| 9 | 29.0 | 15.5 | Clears by 6.5km |
| 10 | 30.5 | 16.0 | Clears by 7.0km |
| 11 | 32.0 | 17.0 | Clears by 8.0km |
| 12 | 33.5 | 18.0 | Clears by 9.0km |
| 13 | 35.0 | 18.5 | Clears by 9.5km |
| 14 | 36.0 | 19.0 | Clears by 10.0km |

**No conflict materializes anywhere in 8-14wk, for either readiness state, at the actual selected values.** FREQ.6B's flagged risk was real but specifically about the *low end of the envelope range* and about 4D's own 16km value shown only as an out-of-envelope reference point — neither is what got selected. The actual selected explicit-zero value (19.5km) clears the partial floor with real margin (6.0km at its tightest, week 8) at every horizon. This is not a case of picking a number backward from the desired outcome — 19.5km was derived independently in §A from the one real precedented ratio available, and only then checked against the floor; the clean result is a consequence of Intermediate's real, much-higher baseline volumes compared to 3D/Beginner×3D, not an engineered avoidance.

## C. Real 8-14wk eligibility matrix — replacing FREQ.6 §17's all-`DECISION_REQUIRED` table

| Horizon | Missing-readiness | Explicit-zero |
|---:|---|---|
| 8 | ELIGIBLE | ELIGIBLE |
| 9 | ELIGIBLE | ELIGIBLE |
| 10 | ELIGIBLE | ELIGIBLE |
| 11 | ELIGIBLE | ELIGIBLE |
| 12 | ELIGIBLE | ELIGIBLE |
| 13 | ELIGIBLE | ELIGIBLE |
| 14 | ELIGIBLE | ELIGIBLE |

All 14 cells `ELIGIBLE`. No horizon routes to typed `PRODUCT_INELIGIBLE`.

## D. Cross-consistency check

**Peak reference reachability**: confirmed directly in §B's table — reachable volume never exceeds the band's real `50km` hard clamp (peaks at 48.0km, missing-readiness week 14) and never needs clamping in either direction; the resolved reference (44.5km) is reached exactly at week 12 by construction, consistent with every other cell's established pattern (GoldenFixtureNonTaperTransitions=10 ↔ the canonical 12-week horizon). Explicit-zero's early horizons (28.0-33.5km at weeks 8-12) sit below the band's `36km` minimum — non-binding per the already-frozen `PEAK_VOLUME_BAND_IS_NOT_A_MANDATORY_ATTAINMENT_TARGET` invariant, not a failure.

**KEY2 floor clearance at the low end of the dose-ratio envelope**: computed the actual smallest real `keyTotal` produced anywhere in the matrix above (explicit-zero, week 8, weekly=28.0km) using FREQ.4's real formula: long-run = `Round0.5(28.0×0.28) = 8.0km`; residual = `20.0km`; `keyTotal = Round0.5(Max(6.0, 20.0×0.5)) = 10.0km`. Splitting 10.0km at the selected 70% ratio gives KEY1≈5.9km / KEY2≈4.1km — both comfortably clear FREQ.4's real `3.0km` per-session minimum. **A real, disclosed, secondary finding**: at the theoretical structural floor (`keyTotal` at its absolute minimum of `6.0km`, which never actually occurs anywhere in the real computed matrix above) combined with the *low end* of the dose-ratio envelope (60% rather than the selected 70%), KEY2 would compute to ~2.25km — *below* its own 3.0km minimum. This is not realized anywhere in this closure's actual matrix (Intermediate's real volumes stay well clear of the structural floor throughout 8-14wk), but is flagged honestly for whoever implements the future asymmetric-allocation mechanism FREQ.6A deferred: that implementation will need its own floor-priority handling for KEY2 (mirroring how EASY's floor is already protected in `FourDaySessionDistanceAllocationPolicy`), not assumed safe by extrapolation from this closure's un-stressed real numbers alone.

No other frozen FREQ.6/FREQ.6A decision is contradicted: `RETAIN_TWO_KEY_EXPOSURES` remains taper-floor-safe at every horizon (§B); the categorical (not exact-ratio) framing of KEY1/KEY2 severity (FREQ.6 §5) is untouched (§A's 70% is a prescription-*dose* authority for a not-yet-built catalog capability, never an adherence-severity weight); FREQ.4's real, currently-running equal-distance-split mechanism is unmodified.

## E. Final classification

```
INTERMEDIATE_5D_NUMERIC_AUTHORITY_APPROVED
```

Not the partial-ineligibility variant — the real computation, with genuinely selected final values (not picked to avoid a conflict), produced zero `PRODUCT_INELIGIBLE` horizons across the entire 8-14wk range for both readiness states. FREQ.6 §17's numeric-authority blocker is closed. Remaining blockers before any implementation phase can start are exactly the ones FREQ.6/FREQ.6A already identified and this phase did not touch: the catalog/prescription-profile capability gap (FREQ.6A, `CATALOG_CAPACITY_BLOCKED`) and the FARTLEK/THRESHOLD structural gap it depends on.
