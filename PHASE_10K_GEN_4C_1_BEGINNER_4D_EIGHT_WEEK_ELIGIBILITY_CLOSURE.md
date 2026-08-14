# Phase 10K-GEN.4C.1 — Beginner 4D Eight-Week Eligibility Decision Closure

**Decision/verification follow-up only. No production code, no catalog mutation, no public rollout. GEN.4C remains the immutable historical record; this is a separate addendum.**

## 1. C5 audit-trail text

Verbatim from GEN.4B's claim ledger, reproduced here for the permanent record: *"A claim retrieved during search referencing a 'Foster's prospective beginner cohort' finding lower injury rates in walk-run vs. continuous-only progressions."* No identifiable primary source (no author/year/journal/clickable reference) existed — it appeared only inside a search-tool-generated summary, never as a retrievable result. It was quarantined in the claim ledger and never used in any starting-volume, progression, peak-volume, frequency-eligibility, session-allocation, long-run, taper, taper-floor, or 8-14-feasibility calculation, nor in GEN.4B's or GEN.4C's final classifications. **Binding: `C5_EXCLUSION_HAS_NO_MATERIAL_EFFECT`.** No replacement source was sought in this phase, consistent with instruction.

## 2. Existing GEN.4C binding policy (unchanged, restated for traceability)

Missing starting volume = 12.0 km/week; explicit-zero = 9.5 km/week; progression policy = existing shared Intermediate values reused (`PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`, `AbsoluteWeeklyIncrementCapKm=2.5`); taper multiplier = 0.53; Beginner 4D general/taper minimum representable week = 9.0 km (exact, re-derived in GEN.4C §20-21); peak band = 18.0-24.0 km (not mandatory attainment); RUN_LAYOUT_4D structural cardinality unchanged (1 KEY + 2 EASY + 1 LONG); session allocation and long-run policy = shared 4D authority, unchanged.

## 3. Original `DECISION_DEPENDENT` cause — traced and reclassified

**Root cause identified by direct, full read of `CatalogVolumeAndLongRunPlanner.cs` (not re-approximated).** GEN.4C's own §23 matrix computed the 8-14 week trajectories using a **linear-approximation growth model** (`starting × (1 + 0.07×(nonTaperWeeks−1))`), explicitly disclosed at the time as an approximation of the real interpolation algorithm. That approximation was **not merely imprecise — it modeled the wrong mechanism entirely** for the 4D (non-3D) code path:

- The `PreferredMaxWeeklyIncreaseRatio`/`HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm` week-to-week ratio-driven growth walk (`CatalogVolumeAndLongRunPlanner.cs` lines 116-134, 234-238, 254-261) is used **exclusively inside the `ThreeDayIntermediate`-policy branch** (`ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayIntermediate)`). It is never reached for the 4D (`Default`-shaped) policy path.
- For the 4D path, the reachable peak for a given horizon is computed **once, up front**, by `ResolvePeak` (lines 136-147): `transitions = nonTaperWeeks - 1`; `canonicalDefaultMultiplier = GoldenFixtureResolvedPeakKm / GoldenFixtureStartingVolumeKm`; `transitionAdjustedMultiplier = 1 + (canonicalDefaultMultiplier - 1) × transitions / GoldenFixtureNonTaperTransitions`; `reachable = startingVolumeKm × transitionAdjustedMultiplier`, then clamped to the peak band. Every non-taper week's volume is then **pure linear interpolation** from the starting volume to this single `reachable`/`selected` peak value (line 243: `starting + (peak - starting) × index / denominator`) — never a repeated 7%-per-week compounding or additive walk.
- **GEN.4C's §11 peak-volume decision selected a band (18.0-24.0 km, the clamp bounds) but never selected `GoldenFixtureResolvedPeakKm`** (the policy-level reference constant that actually determines `transitionAdjustedMultiplier`, and therefore how much growth headroom any given horizon receives). Without this constant, the exact reachable-peak-per-horizon computation — and therefore the exact pre-taper volume and exact taper-floor outcome — could not be performed at all; GEN.4C's own approximation was a stand-in for a genuinely missing input, not a rounding-precision shortcut.

**Classification: `REAL_UNRESOLVED_PRODUCT_DECISION`** (not `CALCULATION_AMBIGUITY`, not `IMPLEMENTATION_DETAIL_MISTAKEN_FOR_PRODUCT_DECISION`, not `DOCUMENTATION_ERROR`, and not `ALREADY_RESOLVED_BY_FROZEN_POLICY`). This phase closes it narrowly below (§4), rather than blocking, since a defensible, precedent-consistent value can be derived without new evidence synthesis.

**Closing decision (new, disclosed, narrow — an explicit extension of GEN.4C's peak-volume decision, not a contradiction of it):**
- `GoldenFixtureResolvedPeakKm = 22.0 km`, `GoldenFixtureStartingVolumeKm = 12.0 km` (reuses the already-approved §6/GEN.4C missing-readiness starting default), `GoldenFixtureNonTaperTransitions = 10` (reused unchanged from `.Default` — this is a horizon-canonical reference constant, not Level-specific; Beginner's Core horizon range, 8-14 weeks, is identical to Intermediate's, so no new value is warranted).
- **Derivation of 22.0**: by direct analogy to the one existing precedent. Intermediate's own `GoldenFixtureResolvedPeakKm=38` sits at relative position `(38-30)/(42-30) = 66.7%` within its own peak band (30-42 km). Applying the identical relative position to Beginner's approved band (18-24 km): `18 + 0.667×(24-18) = 22.0 km`.
- **Internal-consistency check (not assumed, verified by direct computation, §4)**: with these constants, a 12-week Beginner plan (the same reference horizon Intermediate's constants are calibrated against) reaches a reachable peak of **exactly 22.0 km** — i.e., exactly its own `GoldenFixtureResolvedPeakKm`, mirroring precisely how Intermediate's 12-week reference plan reaches exactly 38 km. This is a strong self-consistency signal that the derivation is well-formed, not arbitrary.
- This decision does **not** alter the already-approved 18.0-24.0 km peak band (the clamp bounds) — every computed horizon's selected peak in §4 falls exactly inside that band, confirmed below.

## 4. Preferred-vs-hard progression semantics — documented exactly

**Required invariant `ELIGIBILITY_MUST_NOT_BE_CREATED_BY_ACCELERATING_TO_A_HARD_SAFETY_LIMIT` is satisfied trivially and structurally, not by discipline or restraint** — for the 4D path, the hard ratio (0.08) is never read as a driver of week-to-week growth at all (confirmed by the code read, §3); it exists only as decision-trace metadata on `ReachablePeakDecision`. There was never an opportunity to "accelerate to 8%" to manufacture eligibility, because the actual mechanism (linear interpolation toward a fixed, pre-determined reachable peak) has no per-week ratio choice to make in the first place. §9's GEN.4C decision to reuse the existing progression ratios unchanged remains correct and unaltered — it simply means those ratios continue to be carried as descriptive/decision-trace values for the 4D path, exactly as they already are for Intermediate today, not that they were ever the literal growth driver this phase needed to reverse-engineer.

## 5. Exact 8-week weekly-volume path (production-equivalent arithmetic)

Missing-readiness start = 12.0 km. `nonTaperWeeks = 7` (8 total weeks − 1 taper week, taper is always exactly 1 week per the canonical phase allocation). `transitions = 7 − 1 = 6`.

`canonicalDefaultMultiplier = 22.0 / 12.0 = 1.8333...`
`transitionAdjustedMultiplier = 1 + (1.8333 − 1) × 6/10 = 1 + 0.5 = 1.5`
`reachable = 12.0 × 1.5 = 18.0` → `Round0.5(18.0) = 18.0`
`18.0 < bounds.MinimumKm(18.0)`? No → `selected = Round(Clamp(18.0, 18.0, 24.0)) = 18.0` — **peak.SelectedPeakKm = 18.0 km, exactly at the approved band's lower bound.**

Linear interpolation, `denominator = max(1, 7−1) = 6`, `week(index) = 12.0 + (18.0 − 12.0) × index/6`:

| Week | Phase | Index | Unclamped | Selected/Final |
|---|---|---|---|---|
| 1 | Foundation | 0 | 12.0 | **12.0** |
| 2 | Foundation | 1 | 13.0 | **13.0** |
| 3 | Foundation | 2 | 14.0 | **14.0** |
| 4 | Build | 3 | 15.0 | **15.0** |
| 5 | Build | 4 | 16.0 | **16.0** |
| 6 | RaceSpecific | 5 | 17.0 | **17.0** |
| 7 | RaceSpecific | 6 | 18.0 | **18.0** (= reachable peak exactly, by construction of linear interpolation at the final non-taper index) |
| 8 | Taper | — | 18.0 × 0.53 = 9.54 | **9.5** (Round0.5 away-from-zero) |

(Phase-week distribution above follows the canonical 8-week compressed allocation shape — exact per-phase week counts are a `CatalogPhaseAllocationResolver` output independent of this volume calculation and are shown for orientation only, not re-derived in this phase.)

**Week 7 is the pre-taper authority.** `ProjectedPreTaperWeeklyVolume = 18.0 km`. `Round0.5(18.0 × 0.53) = Round0.5(9.54) = 9.5 km`.

**Comparison: 9.5 km ≥ 9.0 km floor → PASSES, with a 0.5 km margin (exactly one rounding increment above the boundary).**

## 6. Exact taper break-even table (16.0 / 16.5 / 17.0 / 17.5, production-equivalent rounding)

`Round0.5(x) = Math.Round(x/0.5, MidpointRounding.AwayFromZero) × 0.5`

| Pre-taper | Raw taper (×0.53) | Rounded taper | Passes 9.0 floor? |
|---|---|---|---|
| 16.0 | 8.48 | 8.5 | **No** |
| 16.5 | 8.745 | 8.5 | **No** |
| 17.0 | 9.01 | 9.0 | **Yes** (exact boundary) |
| 17.5 | 9.275 | 9.5 | **Yes** |

Confirms GEN.4C's originally-stated 17.0 km break-even exactly — this specific threshold was correct in GEN.4C (it is a pure function of the fixed 0.53 multiplier and the fixed 9.0 km floor, independent of the interpolation-model error found in §3, which only affected *how pre-taper volume is reached*, not the *threshold itself*).

## 7. Full taper/final-prescription representability (8-week case, taper week = 9.5 km)

Using the exact, verified `FourDaySessionDistanceAllocationPolicy`/`BuildLongRunPlan` formulas (re-confirmed this phase against the live code):

- `LongRunDistanceKm = Round0.5(0.33 × 9.5) = Round0.5(3.135) = 3.0`. Share = 3.0/9.5 = 31.6%, within [30%,36%]; hard cap = 0.40×9.5=3.8, 3.0 < 3.8 — no violation.
- `residual = Round0.5(9.5 − 3.0) = 6.5`. `key = Round0.5(Max(3.0, 6.5×0.5)) = Round0.5(3.25) = 3.5` (round-half-away-from-zero). `easyResidual = Round0.5(6.5 − 3.5) = 3.0` (not < 3.0, no adjustment). `firstEasy = Round0.5(3.0/2) = 1.5`. `secondEasy = Round0.5(6.5 − 3.5 − 1.5) = 1.5`. **Total = 3.5 + 1.5 + 1.5 + 3.0 = 9.5 — exact reconciliation.**
- Every minimum satisfied: KEY 3.5 ≥ 3.0; EASY1 1.5 ≥ 1.5 (exact); EASY2 1.5 ≥ 1.5 (exact); LONG within share band.
- `TAPER_SHARPEN` representability: KEY session distance = 3.5 km ≥ `MinSessionDistanceKm(3.0)` — applicable. `sharpening = Clamp(Round0.5(3.5×0.20), 0.5, 1.5) = Clamp(Round0.5(0.7), 0.5, 1.5) = Clamp(0.5, 0.5, 1.5) = 0.5`. `recovery = 0.5`. `easy = Round0.5(3.5 − 0.5 − 0.5) = 2.5 ≥ 1.5` — representable, no fallback branch triggered.

**No approved hard constraint is violated anywhere in the 8-week prescription.** No constraint required lowering to achieve this result.

## 8. Final 8-week eligibility

**`BEGINNER_4D_MISSING_8W_ELIGIBLE`.**

The frozen deterministic policy (now fully specified per §3's closing decision) reaches a fully representable taper (9.5 km ≥ 9.0 km floor, §5-6) and a fully representable final prescription (§7) at 8 weeks, missing readiness, 12.0 km start. No hard constraint was relaxed, no structural role was altered, and eligibility was not manufactured by accelerating toward the hard progression ratio (§4) or by using the peak band's lower bound to rescue the plan (the plan's reachable peak, 18.0 km, happens to land exactly at the band's lower bound as a direct mathematical consequence of the chosen constants and this specific 8-week horizon — it was not engineered to do so, and `BELOW_BAND_BUT_VALID`/`PRODUCT_INELIGIBLE` were both live possible outcomes before the arithmetic was run).

## 9. Updated 8-14 missing-readiness matrix (all exact, no approximation)

Recomputed for every horizon using the now-fully-specified constants (`GoldenFixtureResolvedPeakKm=22.0`, `GoldenFixtureStartingVolumeKm=12.0`, `GoldenFixtureNonTaperTransitions=10`):

| Weeks | Transitions | Reachable peak (exact) | Within approved 18-24 band? | Pre-taper volume | Taper volume (exact) | vs. 9.0 floor | Eligibility |
|---|---|---|---|---|---|---|---|
| 8 | 6 | 18.0 | At lower bound | 18.0 | 9.5 | Pass | `ELIGIBLE` |
| 9 | 7 | 19.0 | Yes | 19.0 | 10.0 | Pass | `ELIGIBLE` |
| 10 | 8 | 20.0 | Yes | 20.0 | 10.5 | Pass | `ELIGIBLE` |
| 11 | 9 | 21.0 | Yes | 21.0 | 11.0 | Pass | `ELIGIBLE` |
| 12 | 10 | 22.0 | Yes (= exact golden-fixture reference) | 22.0 | 11.5 | Pass | `ELIGIBLE` |
| 13 | 11 | 23.0 | Yes | 23.0 | 12.0 | Pass | `ELIGIBLE` |
| 14 | 12 | 24.0 | At upper bound | 24.0 | 12.5 | Pass | `ELIGIBLE` |

**Every row is exactly `ELIGIBLE`, with no `PRODUCT_INELIGIBLE` and no remaining `DECISION_DEPENDENT`.** No horizon's outcome changed from GEN.4C's original conclusion (9-14 were already `ELIGIBLE`; they remain `ELIGIBLE`, now with exact rather than approximate figures) — **`GEN4C_EXISTING_ELIGIBILITY_MATRIX_CONTRADICTION_FOUND` does not apply.**

## 10. Cross-policy consistency recheck

Re-verified simultaneous consistency of: 12.0 km start, the now-fully-specified progression/interpolation mechanism, the 18-24 km peak band (respected exactly at both ends across the full 8-14 range, §9), shared session allocation (exact reconciliation at every checkpoint recomputed, §7), shared long-run policy (share within band at every checkpoint), workout dosage (TAPER_SHARPEN representable at the 8-week minimum case, §7), the 17.0 km exact break-even (§6, unchanged and reconfirmed), the 0.53 taper multiplier (unaltered), the 9.0 km floor (unaltered), and the 8-week horizon specifically. **No contradiction found anywhere.** `NO_SELECTED_PRODUCT_VALUE_MAY_INVALIDATE_ANOTHER_SELECTED_PRODUCT_VALUE` holds. No GEN.4C product decision required reopening beyond the narrow, disclosed §3 addition (which extends rather than conflicts with the approved peak-band decision).

## 11. GEN.4C updated closure status

`BEGINNER_4D_PRODUCT_POLICY_APPROVED_WITH_CATALOG_GAP` **remains the status**, now annotated: **`8_WEEK_ELIGIBILITY_AMBIGUITY_RESOLVED`**. The peak-volume decision (GEN.4C §11) is extended, not superseded, by this phase's addition of the `GoldenFixtureResolvedPeakKm`/`GoldenFixtureNonTaperTransitions` reference constants — the approved 18.0-24.0 km band itself is unchanged and is respected exactly by every recomputed horizon.

## 12. Fartlek/Threshold catalog-gap status

**Unchanged, not conflated with the 8-week resolution.** The absence of interval/repetition structure for `FARTLEK`/`THRESHOLD_TEMPO` in the live catalog schema (GEN.4C §12, GEN4C-INV-016) remains a separate, disclosed, non-blocking follow-up item — it is orthogonal to volume/taper representability and was not touched by this phase's arithmetic.

## 13. GEN.4D readiness

**Not blocked.** With 8-week eligibility now closed `ELIGIBLE` and the full 8-14 matrix exact and contradiction-free, `PHASE 10K-GEN.4D — BEGINNER 4D CORE IMPLEMENTATION` (recommended by GEN.4C, still not started here) has no outstanding decision-closure blocker from this line of work. The one item GEN.4D's own first step should still incorporate (per GEN.4C §36, reaffirmed here) is formalizing the now-fully-specified Beginner `VolumeSafetyPolicy` instance (starting=12.0, peak reference=22.0, transitions=10, all other fields copied verbatim from `.Default` per GEN.4C §9) as an actual implementation artifact — this is `NEW_POLICY_IMPLEMENTATION` (small, precedented), not a new decision-closure task.

## 14. Final classification

```
BEGINNER_4D_MISSING_8W_ELIGIBILITY_APPROVED
```

The 8-week missing-readiness ambiguity is resolved deterministically via a narrow, disclosed, precedent-consistent closure of a genuinely missing (not merely imprecisely-approximated) product constant. The full 8-14 missing-readiness matrix is now exact and uniformly `ELIGIBLE` with zero contradiction against GEN.4C's prior 9-14 conclusions. `BEGINNER_4D_MISSING_8W_PRODUCT_INELIGIBILITY_APPROVED`, `BEGINNER_4D_MISSING_8W_DECISION_BLOCKED`, and `BEGINNER_4D_GEN4C_ELIGIBILITY_MATRIX_CONTRADICTION_FOUND` do not apply.
