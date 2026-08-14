# Phase 10K-GEN.4C.2 — Beginner 4D Peak-Reference Provenance & Full 8-14 Algorithm Revalidation

**Verification/decision-integrity follow-up only. No production code, no catalog mutation, no public rollout, no new literature review. GEN.4C and GEN.4C.1 remain the immutable historical record; this is a separate, superseding-for-eligibility-purposes addendum.**

## 1. Scope

Exactly two questions, per instruction, scope not broadened: (A) was `GoldenFixtureResolvedPeakKm=22.0` independently derived or selected via Intermediate precedent as the selecting authority; (B) were all 8-14 missing-readiness rows genuinely recomputed with the real algorithm, or only the 8-week row.

## 2. Binding GEN.4C/4C.1 state

`BEGINNER_4D_PRODUCT_POLICY_APPROVED_WITH_CATALOG_GAP` (GEN.4C); `BEGINNER_4D_MISSING_8W_ELIGIBILITY_APPROVED` (GEN.4C.1). This phase does not alter GEN.4C's other decisions (starting-volume defaults 12.0/9.5, peak *band* 18.0-24.0, taper multiplier 0.53, 9.0 km floor, session-allocation/long-run reuse, workout eligibility) — only the single `GoldenFixtureResolvedPeakKm`/`GoldenFixtureStartingVolumeKm` constant-pair decision and its downstream eligibility-matrix consequences are in scope.

## 3. Exact 22.0-km decision chronology (reconstructed honestly, not summarized after the fact)

The actual sequence, as it occurred in GEN.4C.1:

1. Already frozen *before* 22.0 was considered: missing start = 12.0 km (GEN.4C §6); explicit-zero = 9.5 km (§7); peak **band** = 18.0-24.0 km (§11, a clamp range, not a reference point); Core = 8-14 weeks; taper multiplier = 0.53; 4D floor = 9.0 km (re-derived exactly in GEN.4C §20-21); the *existence* of the linear-interpolation algorithm and its dependency on a `GoldenFixtureResolvedPeakKm`/`GoldenFixtureStartingVolumeKm`/`GoldenFixtureNonTaperTransitions` triple (discovered via direct code read at the start of GEN.4C.1, §3 of that document).
2. **No Beginner-specific deterministic equation existed, or was applied, to produce 22.0 from any of the above frozen values.** The frozen values (12.0, 9.5, 18.0-24.0, 0.53, 9.0) do not algebraically determine `GoldenFixtureResolvedPeakKm` — a band and a floor do not imply a single interpolation-reference point.
3. The actual method used: Intermediate's real, code-confirmed constants (`GoldenFixtureResolvedPeakKm=38`, within Intermediate's own peak band `[30,42]`) were read, a relative position was computed (`(38-30)/(42-30) = 66.7%`), and that percentage was then applied to Beginner's band (`18 + 0.667×(24-18) = 22.0`). **22.0 was obtained *from* Intermediate's numbers as a direct arithmetic input — it was not first obtained through any Beginner-specific rule and only afterward found to resemble Intermediate.**
4. Therefore, factually: **Intermediate's `GoldenFixtureResolvedPeakKm=38` (and its position within Intermediate's own band) was the selecting mechanism**, not a post-hoc sanity check performed after an independent Beginner-specific value had already been produced by some other means.
5. Classification of the analogy's role: **`LOAD_BEARING_SELECTION_RATIONALE`**, not `POST_HOC_VALIDATION`. There is no other rationale in the GEN.4C.1 text, and reconstruction here confirms none existed — the relative-position formula *is* the entire selection mechanism, and Intermediate's constants are its only real input beyond the already-approved Beginner band.

## 4. Peak-band vs. resolved-peak-reference semantics (verified against code and governance documentation, not assumed)

**`PEAK_BAND_SEMANTICS`**: `bounds.MinimumKm`/`bounds.MaximumKm` (from the `PeakVolumeBand` catalog artifact, GEN.4C §11's 18.0-24.0 km) function purely as a **clamp range** applied *after* a reachable-peak value has already been computed (`CatalogVolumeAndLongRunPlanner.cs` lines 141-147: `selected = reachable < bounds.MinimumKm ? reachable : Round(Clamp(reachable, bounds.MinimumKm, bounds.MaximumKm))`). The band never *generates* a value; it only *constrains* one already produced elsewhere.

**`RESOLVED_PEAK_REFERENCE_SEMANTICS`**: `GoldenFixtureResolvedPeakKm` (together with `GoldenFixtureStartingVolumeKm` and `GoldenFixtureNonTaperTransitions`) is documented explicitly, per direct read of `PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md` (lines 46-48), as a **`GOLDEN_FIXTURE_DERIVED_CALIBRATION_CONSTANT`**: *"the fixture's own resolved peak week volume... **Not a general product default, readiness fallback, or catalog-authored phase constraint**; used only to calibrate or reproduce the current pilot volume progression."* This is not a "preferred point inside the band," not a "target," and not itself a clamp — it is a **rate-calibration constant pair, sourced from an actual real reference dataset/worked example ("Golden Fixture v3," with its own Git provenance at a specific commit)**, whose ratio (`GoldenFixtureResolvedPeakKm / GoldenFixtureStartingVolumeKm`) determines how much proportional growth headroom `transitionAdjustedMultiplier` grants any real plan's actual starting volume over its actual number of transitions. Confirmed decoupled from any real per-Level default: Intermediate's own `GoldenFixtureStartingVolumeKm=24` does **not** equal Intermediate's own missing-readiness fallback (16 km) or explicit-zero fallback (12 km) — it is a wholly separate calibration input, not a runtime default.

**Both values are required by runtime, but for genuinely different purposes**: the band gates final admissibility; the golden-fixture pair determines the *shape* of growth before that gate is ever applied. **Critical finding: no equivalent "Beginner golden fixture" reference dataset/worked example exists anywhere in the repository.** The governance note's own definition of this constant type (calibrated *from an actual fixture*, explicitly not a freely-selectable product default) means there is **no legitimate methodology by which analogy, scaling, or relative-band-positioning against a *different* Level's fixture-derived constant can produce a valid instance of this constant type for Beginner** — the constant's entire defined nature presupposes a real, specific Beginner reference case to calibrate against, which does not exist.

## 5. 22.0-km provenance classification

**`BEGINNER_22KM_REFERENCE_IMPROPERLY_DERIVED_FROM_INTERMEDIATE` (C).**

Per §3, the actual selection logic was exactly the prohibited pattern: Intermediate precedent → choose Beginner 22, via a relative-position formula that does not change the fundamental fact that Intermediate's numbers were the selecting mechanism, not a post-hoc check. Per §4, this is further and independently confirmed invalid: the constant type itself is documented as fixture-calibration-derived, not analogy-derived, and no Beginner fixture exists to calibrate against — meaning even a *more careful* analogy-based derivation would still not produce a legitimate instance of this constant. **`BEGINNER_22KM_REFERENCE_SELECTED_BY_APPROVED_PRODUCT_DEFAULT` (B) does not apply**: at no point in GEN.4C.1 was this explicitly framed and classified as an Appsel product-default choice using `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` (the classification GEN.4C.1 actually used elsewhere, correctly, for the starting-volume and band decisions) — it was presented as a "derivation... by direct analogy," which is a scientific/calibration-style claim, not a disclosed product-default choice.

**This reopens only the single reference-value decision (`GoldenFixtureResolvedPeakKm`, and by extension `GoldenFixtureStartingVolumeKm`/`GoldenFixtureNonTaperTransitions`, since all three were set together in GEN.4C.1 without independent justification for the latter two either — `GoldenFixtureStartingVolumeKm=12.0` was set to equal my own missing-readiness default with no verification that this mirrors Intermediate's own decoupled relationship, §4). No other GEN.4C decision is reopened.**

## 6. Actual production interpolation algorithm (re-confirmed by direct full read of `CatalogVolumeAndLongRunPlanner.cs`, not reconstructed from memory or prior prose)

```
nonTaperWeeks = count of weeks where PhaseKey != "TAPER"
transitions = max(0, nonTaperWeeks - 1)
canonicalDefaultMultiplier = GoldenFixtureResolvedPeakKm / GoldenFixtureStartingVolumeKm
transitionAdjustedMultiplier = 1 + (canonicalDefaultMultiplier - 1) * transitions / GoldenFixtureNonTaperTransitions
reachable = Round0.5(startingVolumeKm * transitionAdjustedMultiplier)
selected (= peak.SelectedPeakKm) = reachable < bounds.MinimumKm
    ? reachable                                          // NOT clamped up to the band minimum
    : Round0.5(Clamp(reachable, bounds.MinimumKm, bounds.MaximumKm))

For each non-taper week at zero-based position `index` among nonTaperWeeks (denominator = max(1, nonTaperWeeks-1)):
    unclamped = startingVolumeKm + (peak.SelectedPeakKm - startingVolumeKm) * index / denominator
    final = Round0.5(Clamp(unclamped, min(startingVolumeKm, peak.SelectedPeakKm), peak.SelectedPeakKm))

Taper week (always exactly 1, always last):
    unclamped = previousWeekFinal * 0.53
    final = min(Round0.5(unclamped), peak.SelectedPeakKm)

Round0.5(x) = Math.Round(x / 0.5, MidpointRounding.AwayFromZero) * 0.5
```

The final non-taper week's `index` always equals `denominator` exactly, so **`ProjectedPreTaperWeeklyVolume = peak.SelectedPeakKm` always, for every horizon.**

## 7-13. Exact missing-readiness paths, all seven horizons (start = 12.0 km — provisional pending §5's reopened reference decision, computed here for audit/mechanical purposes only, per instruction not to claim final eligibility without a valid reference)

Using the disputed `GoldenFixtureResolvedPeakKm=22.0`, `GoldenFixtureStartingVolumeKm=12.0`, `GoldenFixtureNonTaperTransitions=10` (carried forward *only* as a mechanical illustration of the correct algorithm's shape — **not re-endorsed as valid**, per §5):

| Weeks | Transitions | Reachable (raw) | vs. band[18,24] | peak.SelectedPeakKm | Pre-taper = peak | Raw taper (×0.53) | Rounded taper | vs. 9.0 floor | Provisional result |
|---|---|---|---|---|---|---|---|---|---|
| 8 | 6 | 18.0 | At lower bound | 18.0 | 18.0 | 9.54 | 9.5 | Pass | `ELIGIBLE` (provisional) |
| 9 | 7 | ≈19.0 | Inside | 19.0 | 19.0 | 10.07 | 10.0 | Pass | `ELIGIBLE` (provisional) |
| 10 | 8 | 20.0 | Inside | 20.0 | 20.0 | 10.6 | 10.5 | Pass | `ELIGIBLE` (provisional) |
| 11 | 9 | 21.0 | Inside | 21.0 | 21.0 | 11.13 | 11.0 | Pass | `ELIGIBLE` (provisional) |
| 12 | 10 | 22.0 | Inside (= reference exactly) | 22.0 | 22.0 | 11.66 | 11.5 | Pass | `ELIGIBLE` (provisional) |
| 13 | 11 | 23.0 | Inside | 23.0 | 23.0 | 12.19 | 12.0 | Pass | `ELIGIBLE` (provisional) |
| 14 | 12 | 24.0 | At upper bound | 24.0 | 24.0 | 12.72 | 12.5 | Pass | `ELIGIBLE` (provisional) |

Full week-by-week 8-week path (matching GEN.4C.1's own table exactly, re-verified): 12.0→13.0→14.0→15.0→16.0→17.0→18.0→taper 9.5. **No difference found when recomputed within this unified method** (§12 of the binding prompt) — the 8-week path itself was already computed correctly in GEN.4C.1; only the *input reference constant* feeding it is now under dispute.

## 14. Old-vs-corrected missing-readiness matrix

| Horizon | GEN.4C original result | GEN.4C original model | GEN.4C.1 stated result | Recomputed pre-taper (this phase, provisional constants) | Recomputed taper | Actual final result | Changed? | Reason |
|---|---|---|---|---|---|---|---|---|
| 8 | `DECISION_DEPENDENT` | 7%-compounding approximation | `ELIGIBLE` | 18.0 | 9.5 | `ELIGIBLE` (provisional — reference disputed) | Yes (from `DECISION_DEPENDENT`) | Reference constant now supplied, but not yet validly |
| 9 | `ELIGIBLE` (≈17.9 km pre-taper, approximated) | 7%-compounding approximation | Not restated | 19.0 | 10.0 | `ELIGIBLE` (provisional) | Calculation changed, outcome same | Approximation replaced by exact formula; both happened to agree |
| 10 | `ELIGIBLE` (≈18.7 km, approx.) | Same | Not restated | 20.0 | 10.5 | `ELIGIBLE` (provisional) | Calculation changed, outcome same | Same |
| 11 | `ELIGIBLE` (≈19.6 km, approx.) | Same | Not restated | 21.0 | 11.0 | `ELIGIBLE` (provisional) | Calculation changed, outcome same | Same |
| 12 | `ELIGIBLE` (≈20.4 km, approx.) | Same | Not restated | 22.0 | 11.5 | `ELIGIBLE` (provisional) | Calculation changed, outcome same | Same |
| 13 | `ELIGIBLE` (≈21.2 km, approx.) | Same | Not restated | 23.0 | 12.0 | `ELIGIBLE` (provisional) | Calculation changed, outcome same | Same |
| 14 | `ELIGIBLE` (≈22.1 km, approx.) | Same | Not restated | 24.0 | 12.5 | `ELIGIBLE` (provisional) | Calculation changed, outcome same | Same |

**Explicit correction, per instruction: every row's underlying calculation changed** (the exact reachable-peak/pre-taper/taper figures differ from GEN.4C's approximated figures at every horizon, not just row 8) — rows 9-14 are **not** "unchanged," they are "recalculated, with the same final ELIGIBLE label as a coincidental outcome," and are correctly documented as corrected calculations here rather than silently equated with the old ones.

## 15. Exact 12-week 22.0-km verification

Confirmed by direct arithmetic (§7-13 table, row 12): at `transitions=10` (exactly `GoldenFixtureNonTaperTransitions`), `transitionAdjustedMultiplier = canonicalDefaultMultiplier` exactly, so `reachable = 12.0 × (22.0/12.0) = 22.0` exactly — reached in the **final non-taper week** (week 11 of a 12-week plan), not any other week. **This is a tautological consequence of how `GoldenFixtureStartingVolumeKm` was set equal to my own chosen 12.0 km missing-readiness default in GEN.4C.1 — it confirms internal arithmetic self-consistency, not external validity of the 22.0 choice itself** (§5).

**Intermediate 38-km analogy, factual basis**: confirmed directly from `VolumeSafetyPolicy.cs` (read in full, both this phase and GEN.4C.1): `GoldenFixtureResolvedPeakKm: 38d` and `GoldenFixtureStartingVolumeKm: 24d` are the real, live constants in `VolumeSafetyPolicy.Default`. **`INTERMEDIATE_ANALOGY_FACTUALLY_VALID`** — the claim that Intermediate's real code uses these exact numbers is accurate, verified against the actual file, not assumed. This is reported strictly separately from §5's provenance classification, per instruction: the analogy's *factual basis* being accurate does not make its *use as a selecting mechanism for Beginner* legitimate — these are independent findings.

## 16. Explicit-zero model-error finding

**Confirmed: GEN.4C's original explicit-zero 8-14 matrix (§24) used the identical flawed 7%-compounding approximation as the missing-readiness matrix, and requires the identical correction.** Recomputed here (start = 9.5 km, same disputed provisional reference constants, mechanical/audit purposes only):

## 17. Corrected explicit-zero 8-14 matrix (provisional, pending §5)

| Weeks | Transitions | Reachable (raw) | vs. band[18,24] | peak.SelectedPeakKm | Raw taper (×0.53) | Rounded taper | vs. 9.0 floor | Provisional result |
|---|---|---|---|---|---|---|---|---|
| 8 | 6 | 14.25→14.5 | **Below band minimum** | 14.5 (unclamped — below-band values are NOT raised to the band floor) | 7.685 | 7.5 | Fail | `PRODUCT_INELIGIBLE` (provisional) |
| 9 | 7 | 15.04→15.0 | Below band | 15.0 | 7.95 | 8.0 | Fail | `PRODUCT_INELIGIBLE` (provisional) |
| 10 | 8 | 15.83→16.0 | Below band | 16.0 | 8.48 | 8.5 | Fail | `PRODUCT_INELIGIBLE` (provisional) |
| 11 | 9 | 16.625→16.5 | Below band | 16.5 | 8.745 | 8.5 | Fail | `PRODUCT_INELIGIBLE` (provisional) |
| 12 | 10 | 17.42→17.5 | Below band | 17.5 | 9.275 | 9.5 | **Pass** | `ELIGIBLE` (provisional) |
| 13 | 11 | 18.21→18.0 | At/above band minimum | 18.0 | 9.54 | 9.5 | Pass | `ELIGIBLE` (provisional) |
| 14 | 12 | 19.0 | Inside band | 19.0 | 10.07 | 10.0 | Pass | `ELIGIBLE` (provisional) |

**Material correction, explicitly flagged, per instruction not to leave "explicit-zero eligible only at 14 weeks" frozen if it came from the wrong model: the corrected calculation shows explicit-zero provisionally `ELIGIBLE` at 12-14 weeks (not only 14), and `PRODUCT_INELIGIBLE` at 8-11 weeks (not 8-13 as GEN.4C originally concluded).** This is a genuine, real change in the eligibility boundary location, not merely a recalculated path with the same final labels — GEN.4C's original conclusion is now known to be wrong in its specific horizon boundary (it said 8-13 ineligible/14-only-eligible; the corrected math says 8-11 ineligible/12-14-eligible), independent of and in addition to the §5 provenance problem. **This entire corrected matrix remains provisional and not final, since it depends on the same disputed reference constant.**

## 18. Positive-observed-case model-error finding

**Confirmed: GEN.4C's representative positive-observed case (§25, `RecentWeeklyVolume=18.0 km`) also used an approximated, non-formula justification** ("18.0 km alone, with zero growth, already exceeds the 17.0 km pre-taper threshold at any point... All 8-14 week horizons are ELIGIBLE") rather than running the actual interpolation/clamp algorithm.

**Recomputed** (start=18.0 km, same disputed provisional reference constants): at every horizon 8-14, `reachable = 18.0 × transitionAdjustedMultiplier ≥ 18×1.5=27.0`, which exceeds the band maximum (24.0) at every horizon (since the minimum multiplier, at H=8, is already 1.5) → `peak.SelectedPeakKm = 24.0` (upper-bound-constrained) for every horizon → pre-taper = 24.0 → taper = `Round0.5(24.0×0.53) = Round0.5(12.72) = 12.5` ≥ 9.0 → **`ELIGIBLE` (provisional) at every horizon 8-14**. **Same final labels as GEN.4C's original conclusion, but via a materially different, now-exact calculation (consistently peak-clamped to 24.0, not "zero growth already sufficient") — documented as a corrected calculation per instruction, not silently equated with the prior reasoning.**

## 19. Cross-policy consistency result

Re-verified session allocation, long-run policy, workout minimums, and TAPER_SHARPEN representability at every recomputed volume checkpoint in §7-13/§17/§18 using the exact formulas already confirmed in GEN.4C/GEN.4C.1 (unchanged in this phase) — **no new contradiction found in any recomputed scenario's role-allocation reconciliation.** However, per §5, **this entire consistency result is itself provisional**, since every scenario's weekly-volume inputs derive from the disputed `GoldenFixtureResolvedPeakKm`. `CORRECTED_VOLUME_PATH_MUST_REMAIN_CROSS_POLICY_CONSISTENT` holds for the arithmetic **as computed**, but the arithmetic's own input is not yet validly closed.

## 20. Original-error scope classification

**`CALCULATION_MODEL_ERROR_WITH_MATERIAL_POLICY_DELTA`.**

Not `_WITH_NO_FINAL_ELIGIBILITY_DELTA` (the explicit-zero horizon boundary materially moved, §17) and not merely `_WITH_BOUNDED_ELIGIBILITY_DELTA` (bounded would understate it, since a genuine reference-value provenance defect was also found, §5, independent of the pure arithmetic-approximation issue). **Two distinct, stacked problems were found and must both be named:** (1) GEN.4C's original approximation method was wrong in kind (fixed in GEN.4C.1 for the 8-week case, now confirmed also needing to be fixed for every other row and matrix, §14/§16/§18); (2) the specific reference constant GEN.4C.1 introduced to fix problem (1) was itself improperly derived (§5), meaning the "fix" produced numerically clean but provenance-invalid results. **The selected policy itself must be revisited for this one constant — GEN.4D remains blocked**, per §16 of the binding prompt's own instruction.

## 21. Corrected GEN.4C status

**Reopened, narrowly: `BEGINNER_4D_PEAK_REFERENCE_CONSTANT_DECISION_REOPENED`, layered onto (not replacing) `BEGINNER_4D_PRODUCT_POLICY_APPROVED_WITH_CATALOG_GAP`.** Every other GEN.4C decision (starting-volume defaults, peak *band*, taper multiplier, 9.0 km floor, session-allocation/long-run reuse, all workout-eligibility decisions, Foundation-KEY finding, taper-sharpen finding) remains approved and unaffected — confirmed above that none of those depended on the disputed `GoldenFixtureResolvedPeakKm` value. GEN.4C.1's specific 8-week conclusion is **not retracted** (18.0/9.5 remains the correct *mechanical* output of the stated algorithm given the stated inputs), but its status changes from "approved" to "correctly computed from a not-yet-validly-approved input" — the same caveat now applies uniformly to every horizon in both the missing-readiness and explicit-zero matrices, not to row 8 alone.

## 22. GEN.4D readiness

**Not ready. Blocked on exactly one narrow item: legitimate selection of `GoldenFixtureResolvedPeakKm`/`GoldenFixtureStartingVolumeKm`/`GoldenFixtureNonTaperTransitions` for the Beginner policy instance, using a method consistent with these constants' documented nature (fixture-calibration-derived, not analogy-derived) or an explicitly-disclosed, non-scientific-sounding `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` classification if no real Beginner fixture is to be constructed.** Two concrete paths forward exist for a future decision phase (not resolved here, per this phase's own no-new-numeric-policy restriction): (a) construct an actual Beginner-specific reference/golden-fixture worked example independent of Intermediate, and derive the constants from it; or (b) explicitly, transparently adopt a product-default value for this constant-triple with the honest `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` label (not framed as a "derivation"), disclosing that Intermediate's values are used only as a starting anchor for a deliberate product choice, not as scientific/calibration authority. Neither path is selected here — this phase's scope explicitly prohibits new numeric product policy except where an existing decision is proven invalid, and proving §5's finding invalid does not itself constitute making the replacement decision.

The Fartlek/Threshold catalog-gap item (GEN.4C §12) remains separately disclosed, non-blocking, and untouched by this finding.

## 23. Final classification

```
BEGINNER_4D_22KM_REFERENCE_DECISION_REOPENED
```

Per the binding prompt's own explicit rule (§4/§18 of the phase prompt): this classification requires `GEN.4D_BLOCKED_ON_GEN4C_POLICY_INTEGRITY`. **GEN.4D must not proceed** until the `GoldenFixtureResolvedPeakKm`/`GoldenFixtureStartingVolumeKm`/`GoldenFixtureNonTaperTransitions` constant-triple decision is legitimately re-made (via a future, explicitly-scoped decision phase) and the full 8-14 missing-readiness and explicit-zero matrices (§14/§17) are re-finalized against whatever constants that decision produces — the arithmetic method itself (§6) is now confirmed correct and does not need to be re-derived again, only re-applied once a valid reference constant exists. `BEGINNER_4D_GEN4C_POLICY_INTEGRITY_REVALIDATED` and `BEGINNER_4D_GEN4C_POLICY_INTEGRITY_REVALIDATED_WITH_MATRIX_CORRECTION` do not apply, since the reference-value provenance itself failed validation, not merely the arithmetic surrounding an otherwise-valid value. `BEGINNER_4D_GEN4C_POLICY_CONTRADICTION_FOUND` does not apply (no contradiction between two approved decisions was found — the problem is a single never-properly-approved decision). `BEGINNER_4D_GEN4C_REVALIDATION_INCOMPLETE` does not apply — this revalidation is complete; its conclusion is a block, not an unfinished analysis.
