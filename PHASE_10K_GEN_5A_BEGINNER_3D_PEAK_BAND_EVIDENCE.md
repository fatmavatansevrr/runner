# Phase 10K-GEN.5A — Beginner 3D Peak-Volume-Band Evidence Synthesis

**Envelope only. No point value selected. No new external literature search — reuses Beginner's own already-approved evidence (GEN.4B) and 3D's own already-frozen structural constraints (GEN.2B.1-2B.3) exclusively. No Intermediate or Beginner×4D value used as a derivation input anywhere below — only as an explicitly-labeled post-hoc sanity check, per instruction.**

## 1. First: is a peak band even horizon-independent? (real code finding, not assumed)

Read `CatalogVolumeAndLongRunPlanner.cs` lines 141/143-144/158-161 directly. Two real, load-bearing facts:

- **`bounds.MaximumKm` is a hard clamp** in both the 3D ratio-compounding loop (`threeDayReachable = Math.Min(candidate, bounds.MaximumKm)`, applied every single step) and the general formula (`Round(Clamp(reachable, MinimumKm, MaximumKm))`).
- **`bounds.MinimumKm` is not a clamp at all** — it only drives a classification label (`BelowTypicalPeakButValid`); a `reachable` value below it is left un-raised, per the already-frozen `PEAK_VOLUME_BAND_IS_NOT_A_MANDATORY_ATTAINMENT_TARGET` invariant. **The maximum is the only number that actually does safety work here; the minimum is comparatively low-stakes.**

Extended the GEN.5 §B compounding sequence (start=12.0, unclamped, no `bounds.MaximumKm` applied) well past the 8-14 week Core window to see whether it naturally plateaus at longer horizons (Runway 15-20wk, LongHorizon 21+wk):

| Weeks (transitions) | Reachable (unclamped) |
|---|---|
| 14 (12) | 25.5 |
| 15 (13) | 27.5 |
| 16 (14) | 29.5 |
| 17 (15) | 31.5 (7% increase now exceeds the 2.0km cap; capped growth from here) |
| 18 (16) | 33.5 |
| 19 (17) | 35.5 |

**Growth never plateaus on its own.** The compounding formula has no self-limiting term — it is mechanically incapable of selecting its own ceiling. **A peak-volume-band therefore must be horizon-independent by construction**: it is the only thing that stops this formula from producing an unbounded, clearly-unsafe weekly volume at longer horizons. This directly answers the phase's "check this assumption explicitly" instruction: not merely confirmed, but shown to be a structural necessity, not a design preference.

## 2. Independently-derived candidate LOWER region (Beginner's own inputs + 3D's own structure only)

From GEN.5 §B's real missing-readiness computation (start=12.0, 3D's own frozen 7%/8%/2.0km-capped compounding, 3D's own 12.0km structural floor):

- Week 12: reachable = 22.5, taper = 12.0 — clears the floor with **zero margin** (exact boundary).
- Week 13: reachable = 24.0, taper = 12.5 — first horizon with durable (non-boundary-exact) clearance.
- Week 14: reachable = 25.5, taper = 13.5.

**Candidate lower region: approximately 22.5-24.0 km** — the zone where Beginner's own starting-volume growth, under 3D's own mechanism, first produces a durably taper-viable weekly volume. This is derived entirely from Beginner's own frozen starting volume and 3D's own frozen growth/taper mechanism — no borrowed number from any other cell.

## 3. No independently-derivable UPPER bound exists from Beginner's own inputs alone

Beginner's own evidence envelope (GEN.4B) characterized peak volume as *"below Intermediate, imprecise"* — a qualitative, not quantitative, constraint. 3D's own structural math (session minimums, 40%/42% long-run share) does not introduce any new quantitative ceiling either: the session-allocation reconciliation in `V1ThreeDaySessionVolumeAllocationPolicy` is self-consistent at any weekly volume ≥ 12.0 km, with no upper limit emerging from the math itself. **Stated honestly: nothing in Beginner's own approved inputs or 3D's own structural constraints independently produces an upper bound.** Inventing one here would not be a derivation — it would be an unstated product judgment call, exactly the kind of move this phase is scoped to avoid (see DO NOT list, "do NOT select a final deterministic value").

## 4. Sanity check against Beginner×4D (explicitly NOT a derivation input) — a real, disclosed tension, not resolved here

Per instruction, checked only for directional plausibility, using the same proportional pattern the real Intermediate data shows (GEN.5 §C: 3D's band sits at roughly 0.73× (min) / 0.76× (max) of 4D's band for the identical Level):

```
Beginner×4D approved band: 18-24 km
Naive proportional scaling: 18 × 0.733 ≈ 13.2 km   |   24 × 0.762 ≈ 18.3 km
```

**This sanity check produces ~13-18 km — below both 3D's own 12.0km structural floor-clearance margin AND below the §2 lower region (22.5-24 km) that Beginner's own real starting-volume growth naturally reaches by week 12-13.** The two approaches disagree, not narrowly but substantially. Two possible readings, neither adjudicated here:

1. The Intermediate-derived proportional-scaling ratio doesn't transfer linearly to Beginner's much lower baseline (plausible — Intermediate's 3D/4D bands both sit well above their respective structural floors with room to spare; Beginner's 4D band sits much closer to its own structural constraints already, so scaling it down further may not be physiologically meaningful).
2. Beginner's real, evidence-derived growth trajectory under 3D's mechanism is itself already inconsistent with a "beginner should train less on 3D than on 4D" expectation, given 3D's fewer sessions concentrate the same total volume into fewer, larger runs.

**This tension is reported plainly, not silently resolved.** It is exactly the kind of finding a subsequent product-decision phase (a GEN.4C-style closure) needs to weigh — this phase does not pick a winner.

## 5. Envelope (not a point value)

```
ResolvedPeakBand (Beginner × 3D) — ENVELOPE, NOT FINAL:
  Data-grounded candidate region (from Beginner's own growth + 3D's own structure): ~22.5 - ~29.5 km
    (22.5 = first floor-clearing value, week 12; 29.5 = week 16, chosen only as an
     illustrative stopping point before growth becomes clearly implausible for a
     beginner runner — NOT itself evidence-derived, and not proposed as the ceiling)
  Sanity-check-only alternative region (proportional scaling from Intermediate's
  real 3D/4D ratio applied to Beginner's 4D band): ~13 - ~18 km
    (explicitly NOT used as a derivation input; disclosed as contradicting §2's
     data-grounded region, unresolved)
Provenance: PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE
```

No single value is selected. Both the data-grounded region and the sanity-check tension are handed to the product-decision phase as-is.

## 6. Final classification

```
BEGINNER_3D_PEAK_BAND_EVIDENCE_ENVELOPE_READY_FOR_PRODUCT_DECISION
```
