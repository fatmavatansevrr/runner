# Phase 10K-GEN.5A.2 — Beginner 3D Peak Band — Narrow Evidence Synthesis

**The band is derived first, from real cited sources only, before any eligibility matrix is touched. No forbidden derivation pattern (ratio-of-Intermediate's-3D-band, or taper-break-even-math) was used anywhere below.**

## 1. Real sources found

1. **Hal Higdon Novice 10K** ([halhigdon.com](https://www.halhigdon.com/training-programs/10k-training/novice-10k/)) — real, directly-fetched, week-by-week table. 8 weeks, exactly 3 running days/week (Tue/Thu/Sun), the canonical beginner-3-day-10K program this framework's earlier phases (GEN.2B.1) already treated as a source-class precedent.

| Week | Tue | Thu | Sun | Weekly total (mi) | Weekly total (km) |
|---|---|---|---|---|---|
| 1 | 2.5 | 2 | 3 | 7.5 | 12.07 |
| 2 | 2.5 | 2 | 3.5 | 8.0 | 12.87 |
| 3 | 2.5 | 2 | 4 | 8.5 | 13.68 |
| 4 | 3 | 2 | 4 | 9.0 | 14.48 |
| 5 | 3 | 2 | 4.5 | 9.5 | 15.29 |
| 6 | 3 | 2 | 5 | 10.0 | 16.09 |
| 7 | 3 | 2 | 5.5 | 10.5 | **16.90** |
| 8 (race) | 3 | 2 | 10K | — | (race week, not a training peak) |

**Real peak training-week volume: 16.90 km** (week 7, the week immediately before race week — the genuine training peak, not the race week itself).

2. **McMillan Running, 10K Level 1 (Beginner), 3 runs/week** ([mcmillanrunning.com](https://www.mcmillanrunning.com/product/10k-level-1-beginner-20-week/)) — real, cited. Weekly volume range **10-20 miles/week = 16.1-32.2 km/week**. Higdon's real 16.9km peak sits almost exactly at the low end of this range — the two independent real sources corroborate each other closely at the low end, rather than converging on a single point deep inside the range.

3. **GRONORUN (Buist et al. 2008)** ([pubmed.ncbi.nlm.nih.gov/17940147](https://pubmed.ncbi.nlm.nih.gov/17940147/), [BMC Musculoskeletal Disorders design paper](https://link.springer.com/article/10.1186/1471-2474-8-24)) — real, cited, but **deliberately not used as a volume source**: it targets a 4-mile (6.4 km) event, not 10K, and its actual finding (RCT: no significant injury-incidence difference between an 8-week standard program and a 13-week graded program applying the 10% rule) is about **program horizon/progression rate**, not peak weekly volume. Consistent with the phase's explicit instruction not to overclaim — this study contributes zero numeric input to the band below; it is cited only to disclose that it was checked and correctly found non-applicable to this specific question.

## 2. Band derivation (evidence first, no eligibility check yet)

Both real, directly-relevant sources are 3-day/week, beginner-specific, 10K-specific programs — the closest available match to Beginner×3D's actual product shape. Anchoring on Higdon's real, exact peak-week value (16.9 km) with McMillan's independently-corroborating low-range floor (16.1 km), and allowing modest headroom consistent with McMillan's stated range without drifting toward its upper end (32 km, which is closer to *Intermediate*'s real 3D band [22-32] than to any beginner-specific source found):

```
Candidate band: 16.0 - 20.0 km/week
Recommended reference point: 17.0 km/week  (Round0.5 of Higdon's real 16.90 km peak)
```

**Cross-frequency consistency constraint (Section 4 of the phase prompt) — checked, satisfied without needing an inversion**: Beginner×4D's approved band is 18-24 km. This candidate 3D band (16-20) sits *below* 4D's band at both ends (16<18, 20<24) — directionally consistent with the real Intermediate pattern (3D lower than 4D for the same Level) with zero need to invoke that pattern as a derivation input. No inversion occurred; nothing to flag as surprising here.

**Classification: `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`** (not `GOLDEN_FIXTURE_DERIVED`) — real sources inform the range, but the exact band edges (16.0/20.0) and reference (17.0) involve product judgment about how much headroom above Higdon's single-program peak is reasonable, exactly mirroring GEN.4C.3's Path Z provenance model.

## 3. Only now: recompute matrices from the frozen band

### Missing-readiness (start = 12.0 km), band ceiling = 20.0 km

Re-ran GEN.5 §B's mechanism with `bounds.MaximumKm = 20.0` applied as the real per-step clamp (same clamping behavior confirmed load-bearing in GEN.5A §1 / GEN.5A.1).

| Weeks | Unclamped reachable | Clamped reachable | Taper (×0.53, Round0.5) | vs 12.0km floor |
|---|---|---|---|---|
| 8 | 17.5 | 17.5 | 9.5 | INELIGIBLE |
| 9 | 18.5 | 18.5 | 10.0 | INELIGIBLE |
| 10 | 19.5 | 19.5 | 10.5 | INELIGIBLE |
| 11 | 21.0 | **20.0** (clamped) | 10.5 | INELIGIBLE |
| 12 | 22.5 | 20.0 (plateaued) | 10.5 | INELIGIBLE |
| 13 | 24.0 | 20.0 | 10.5 | INELIGIBLE |
| 14 | 25.5 | 20.0 | 10.5 | INELIGIBLE |

**All seven horizons `PRODUCT_INELIGIBLE`.**

### Positive-observed (representative example, per GEN.4C.3's precedent: an observed value at the top of Beginner's habitual range)

Used 18.0 km observed recent weekly volume (used directly per the frozen `OBSERVED_RECENT_VOLUME_IS_USER_EVIDENCE_AUTHORITY` rule — no compounding needed for the starting point itself, only for subsequent weeks). Growth hits the 20.0km ceiling by week 4 (transitions=2) and plateaus there for the rest of the horizon range, so weeks 8-14 all reach an identical 20.0 km plateau.

`Round0.5(20.0 × 0.53) = Round0.5(10.6) = 10.5` km. **Below the 12.0km floor. All seven horizons `PRODUCT_INELIGIBLE`** — even for a runner already observed running at the upper edge of Beginner's own habitual volume.

### Explicit-zero — confirmed unaffected, not recomputed (per instruction)

GEN.5 §B already found explicit-zero universally ineligible with an unclamped ceiling; GEN.5A.1 §4's monotonicity argument (a lower ceiling can only ever reduce reachable growth further) holds identically here — 20.0 km is a real, even less restrictive ceiling than either GEN.5A.1 hypothesis, but the conclusion is unchanged: still universally ineligible.

## 4. The exact structural reason (a general, precise finding, not just three more failing matrices)

Solved directly: the minimum pre-taper volume required for `Round0.5(X × 0.53) ≥ 12.0` is `X ≥ 11.75 / 0.53 ≈ 22.17 km`, i.e. the first reachable 0.5km-rounded value that clears it is **22.5 km** (exactly the value GEN.5 §B found as the first eligible point, unclamped). **Any band ceiling below ≈22.2 km makes Beginner×3D structurally ineligible at every horizon and every readiness state, independent of starting volume** — because the algorithm can never even reach the pre-taper volume the taper floor requires, regardless of how many weeks are available. This evidence-grounded band (16-20 km) sits well under that threshold, so the "all-ineligible" result above is not a coincidence of these particular horizons — it is mathematically guaranteed for the entire Core/Runway/LongHorizon range under this band.

## 5. Finding, stated as a product-policy representability question — not a physiological claim (per explicit instruction)

**Beginner×3D Core (8-14wk) is not representable under the approved V1 load policy (frozen 3D progression/taper/session-minimum constraints, GEN.2B.1-2B.3) at this horizon length, given a beginner-specific, evidence-grounded peak-volume-band (16-20 km).** This is a statement about the current product policy's numeric compatibility, not a claim about what beginner runners are physiologically capable of — real beginner 3-day programs (Higdon, McMillan) exist and work at these volumes; the incompatibility is specifically between (a) this beginner-appropriate volume ceiling and (b) this system's existing 3D taper-floor mechanism (12.0km role-viability floor ÷ 0.53 taper multiplier ⇒ ≈22.2km required pre-taper), which was calibrated against Intermediate's much higher baseline and was never re-validated against a materially lower peak-volume ceiling.

## 6. Final classification

```
BEGINNER_3D_PEAK_BAND_FROZEN_AND_MATRICES_RECOMPUTED
```

Band frozen: **16.0-20.0 km, reference 17.0 km, `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`**. All missing-readiness, positive-observed, and explicit-zero 8-14wk matrices: **`PRODUCT_INELIGIBLE`** at every horizon. Not resolved further — this is a representability finding for the next decision phase, not an implementation.
