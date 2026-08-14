# Phase 10K-GEN.5 — Beginner 3D Composition Resolution

**Verification/resolution phase only. No production code touched. No new evidence synthesized — every input number below is a direct, unmodified reuse of an already-frozen value.**

## 1. Scope and outcome

Section D was triggered. This is **not** a clean composition. Final classification: **`BEGINNER_3D_COMPOSITION_CONFLICT_FOUND`**.

## A. Structural floor — confirmed Level-agnostic (real re-derivation, not asserted)

Read `V1ThreeDaySessionVolumeAllocationPolicy.cs` and `PHASE_10K_GEN_2B_2_3D_TRAINING_LOAD_PRODUCT_DECISION.md` §7 directly. The 4.0/3.0/5.0 km minimums are classified `3D_SESSION_MINIMUM_VIABILITY_POLICY_APPROVED` and documented explicitly as *"a mathematical floor for this layout, not physiological readiness"* — a structural, layout-owned constant. The policy class itself (despite its historically Intermediate-named `PolicyKey` string) contains zero Level-conditional logic anywhere — pure percentage/rounding/reconciliation math driven only by `weekly.PlannedWeeklyVolumeKm`. **Confirmed Level-agnostic.** Floor = 4+3+5 = **12.0 km/week**, identical for Beginner×3D and Intermediate×3D.

Real code confirmation this floor is already reused, not newly invented: `CatalogVolumeAndLongRunPlanner.Build`'s existing 3D taper-floor check —
```csharp
if (request.Candidate.DaysPerWeek == 3) { ... if (projectedTaper < 12d) throw new ThreeDayCoreProductIneligibleException(...); }
```
— is keyed on `DaysPerWeek == 3` alone, with **no Level condition**. It already applies identically to any Level, Beginner included, with zero new code required. This mechanism reuse is real, not assumed.

## B. Taper-floor recomputation — real, by-hand application of the frozen 3D ratio-compounding algorithm

Read `CatalogVolumeAndLongRunPlanner.ResolvePeak`'s 3D branch (ratio-compounding: 7% preferred / 8% hard / 2.0 km absolute weekly cap, `Round0.5`, all frozen GEN.2B.1-2B.3 values, reused verbatim) and applied it by hand to Beginner's two frozen starting volumes (12.0 missing-readiness, 9.5 explicit-zero — both GEN.4C.4, unmodified). `transitions = W - 2` for horizon `W` (nonTaperWeeks = W-1, transitions = nonTaperWeeks-1).

**Missing-readiness (start = 12.0 km):**

| Weeks | Reachable pre-taper | Taper (×0.53, Round0.5) | vs 12.0km floor |
|---|---|---|---|
| 8 | 17.5 | 9.5 | **INELIGIBLE** |
| 9 | 18.5 | 10.0 | **INELIGIBLE** |
| 10 | 19.5 | 10.5 | **INELIGIBLE** |
| 11 | 21.0 | 11.0 | **INELIGIBLE** |
| 12 | 22.5 | 12.0 | **ELIGIBLE** (exact floor) |
| 13 | 24.0 | 12.5 | **ELIGIBLE** |
| 14 | 25.5 | 13.5 | **ELIGIBLE** |

**Explicit-zero (start = 9.5 km):**

| Weeks | Reachable pre-taper | Taper (×0.53, Round0.5) | vs 12.0km floor |
|---|---|---|---|
| 8 | 12.5 | 6.5 | **INELIGIBLE** |
| 9 | 13.5 | 7.0 | **INELIGIBLE** |
| 10 | 14.5 | 7.5 | **INELIGIBLE** |
| 11 | 15.5 | 8.0 | **INELIGIBLE** |
| 12 | 16.5 | 8.5 | **INELIGIBLE** |
| 13 | 17.5 | 9.5 | **INELIGIBLE** |
| 14 | 18.5 | 10.0 | **INELIGIBLE** |

**Explicit-zero is `PRODUCT_INELIGIBLE` at every single horizon from 8 to 14 weeks.** The mechanism does not fail to apply — it applies cleanly and produces a definitive, unambiguous answer: within the entire Core window, Beginner's 9.5 km explicit-zero starting point never grows fast enough under 3D's ratio-compounding algorithm (7%/8%/2.0km-capped growth, only ~6-12 compounding steps available at these horizons) to clear the 12.0 km structural taper floor. This is exactly the scenario Section D named as a possible trigger, and it is real, not a hypothetical.

**Missing-readiness is materially worse than every other cell computed in this entire engagement**: 8-11 weeks `PRODUCT_INELIGIBLE`, only 12-14 `ELIGIBLE` — contrast with Beginner×4D's missing-readiness matrix (GEN.4C.4: **all** of 8-14 `ELIGIBLE`) and Intermediate×3D's missing-readiness matrix (Gen3AThreeDayEligibilityMatrixTests: eligible from week 8). The 3D ratio-compounding mechanism was evidence-validated starting from Intermediate's 24 km baseline; starting the same mechanism from Beginner's much lower 12 km baseline compounds far more slowly in absolute km terms (2.0 km/week cap, 7% of a small number is a small number) and simply cannot reach a taper-survivable pre-taper volume within short horizons.

## C. Cross-policy consistency check — surfaced a second real problem, not just a confirmation

Checked whether Beginner's peak-volume-band (18-24 km, GEN.4C.4, frozen for **4D**) could be assumed Frequency-agnostic for 3D, as the phase prompt itself suggested ("It shouldn't [change] — peak-band is a weekly total, Frequency-agnostic by design per GEN.4A — confirm this holds").

**This assumption is false**, disproven by the real data in `plan-catalog/catalog/policies/peak-volume-bands.v4.json` itself:

```
TEN_K / INTERMEDIATE / 3 runs-per-week: 22-32 km
TEN_K / INTERMEDIATE / 4 runs-per-week: 30-42 km
TEN_K / INTERMEDIATE / 5 runs-per-week: 36-50 km
TEN_K / NEW (Beginner) / 4 runs-per-week: 18-24 km
TEN_K / NEW (Beginner) / 3 runs-per-week: <no entry exists>
```

Intermediate's own three rows prove peak-volume-bands are genuinely **Frequency-variant**, not Frequency-agnostic — 3D's band (22-32) is materially lower than 4D's (30-42), which is lower than 5D's (36-50), for the identical Level. GEN.4A's own decision (`PEAK_VOLUME_BAND_IS_LEGITIMATE_CROSS_AXIS_POLICY_DATA`) keys the policy by Distance × Level × **RunsPerWeek** explicitly for exactly this reason — it was never claimed to be Frequency-invariant, and the real data confirms it isn't.

**Consequence**: there is no frozen, evidence-backed peak-volume-band for Beginner×3D. Reusing Beginner's 4D band (18-24) for 3D without derivation would repeat precisely the mistake GEN.4C.2 identified and GEN.4C.3 corrected for the peak-reference question (*"22 km is reasonable because Intermediate uses 38 km" was explicitly ruled an insufficient, borrowed-authority justification*) — here the analogous invalid move would be "Beginner×3D's band is 18-24 because Beginner×4D's band is 18-24," which the Intermediate data directly contradicts as a valid inference pattern.

**Materiality check**: for the missing-readiness matrix (§B), the reachable pre-taper values at weeks 13-14 (24.0, 25.5) would be affected by whichever band ceiling is eventually chosen (if lower than Beginner's 4D band, per the Intermediate pattern where 3D's ceiling is meaningfully below 4D's), which could shift the exact reported numbers — though for weeks 13-14 specifically, taper stays above the 12.0 floor under both a 24.0 and a hypothetically lower ceiling already tested by hand, so this data gap does not change the *eligibility* verdict for those two weeks, only their exact stated km values. It does not affect §B's explicit-zero conclusion at all (those reachable values never approach any plausible band ceiling).

## D. Section D — genuine new questions, explicitly not resolved here

Two independent real problems, neither bridged with an invented value:

1. **Explicit-zero is universally `PRODUCT_INELIGIBLE` across the entire 8-14 week Core window for Beginner×3D.** This is a product question, not a composition question: does Beginner×3D exist at all for explicit-zero readiness within Core, or does it require a different mechanism (a lower taper floor specific to this cell — explicitly NOT invented here), a different starting-volume treatment, or simple non-support (mirroring the containment already used for other closed cells)? **Not decided in this phase.**
2. **No evidence-backed peak-volume-band exists for Beginner×3D**, and the real Intermediate data disproves the "reuse 4D's band" shortcut. A genuine evidence-envelope decision (GEN.4B-style) or an explicit, disclosed derivation (GEN.4C.3-style, from Beginner's own inputs only) is required before any Beginner×3D peak-reachability number can be considered authoritative. **Not decided in this phase** — explicitly out of scope per the DO NOT list ("do NOT re-run literature/coaching evidence synthesis").

## E. Final classification

```
BEGINNER_3D_COMPOSITION_CONFLICT_FOUND
```

No code was written. No new numeric value was invented to force a clean result. Both real findings (§B's explicit-zero universal ineligibility, §C's missing/non-transferable peak-band) are reported plainly, as instructed, rather than silently bridged.
