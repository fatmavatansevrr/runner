# Phase 10K-FREQ.1 — 3D/4D Volume Trajectory Authority Audit

**Audit only. No code touched. Resolves `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001`.**

## 1. Origin trace

`CatalogVolumeAndLongRunPlanner.cs` line 148 cites its own source directly in a code comment: `"TEN_K/INTERMEDIATE/3D GEN.2B.2 sequential preferred growth"`. Traced both documents directly:

- **`PHASE_10K_GEN_2B_1_3D_TRAINING_LOAD_EVIDENCE_SYNTHESIS.md` §10.2** ("Core trajectory lower-bound test") — already computes 3D volume progression using a week-by-week compounding percentage model, explicitly framed against real literature (the evidence table's own "10% rule" / weekly-increase framing, citing Frandsen et al. and the traditional weekly-progression-rule literature by name). This is the **evidence** stage, chronologically before any product decision was made.
- **`PHASE_10K_GEN_2B_2_3D_TRAINING_LOAD_PRODUCT_DECISION.md` §12** ("Core-horizon trajectory validation") — fully specifies the exact mechanism now in production: *"7% preferred growth, 2.0 km absolute cap, 8% hard cap, 0.5 km rounding with post-round cap enforcement, one final taper week at the existing 0.53 multiplier, and the 32 km peak ceiling."* The illustrative table's numbers (`12, 12.5, 13.5, 14.5, 15.5, 16.5, 17.5` for a 12-start/8-week case) are **byte-identical** to the real production algorithm's output, independently reconfirmed in `GEN.5 §B` of this engagement.

**Both documents predate any implementation.** The 3D taper-floor conflict itself (`12km` floor vs. `0.53` multiplier) was *discovered* in GEN.2B.2 §12 through this exact trajectory math — it is not a retrofit explanation for an already-built algorithm.

Chronologically, 4D's fixed-reference interpolation algorithm is the **older** mechanism — it is described in `PHASE_10K_GEN_0_CURRENT_STATE_BASELINE.md` as the original, pre-engagement pilot's existing implementation. So the direction of "which came first" is 4D, but the evidence trail shows 3D's shape was not derived *from* 4D's — GEN.2B.1/2B.2 never reference, compare against, or reuse 4D's mechanism anywhere in either document (checked: no mention of "interpolation," "linear," "ResolvedPeakReference," or 4D's algorithm by name in either document).

## 2. Did the original rationale argue for THIS shape specifically, or only for the rate values?

**Both, but not in a comparative way.** GEN.2B.1 §10.2 and GEN.2B.2 §12 do far more than justify the 7%/8%/2.0km numbers in isolation — they compute and validate the actual week-by-week compounding *trajectory*, explicitly checking real per-week outputs against the peak band and the taper floor, and *discovering a real defect* (the taper-floor conflict) that only exists because of the compounding shape's specific interaction with the 12km structural minimum. That is design-level engagement with the shape itself, not merely rate selection.

**What is genuinely absent**: any explicit comparison against 4D's fixed-reference interpolation shape as an alternative. Nothing in either document frames this as "we chose compounding over interpolation because X." The compounding shape appears to have been adopted directly from real-world training-progression convention (percentage-based weekly increase — the same "10% rule" framing GEN.2B.1's own literature review cites, e.g. Frandsen et al.) as the natural default for a training-load evidence question, not as a considered rejection of 4D's existing approach. **The 3D shape has real, independent, literature-grounded rationale for existing — it does not have rationale for being *different from 4D specifically*, because no one appears to have framed it as a choice between the two.**

## 3. Quantified week-by-week divergence for equivalent endpoints

Constructed a controlled comparison: same start (12.0 km), same natural endpoint (25.5 km), same step count (12 transitions — the real 14-week missing-readiness case from `GEN.5 §B`), computing both shapes over the identical range.

| Step (i) | 4D-style linear interpolation | 3D real compounding (from GEN.5 §B) | Divergence |
|---:|---:|---:|---:|
| 0 | 12.0 | 12.0 | 0 |
| 1 | 13.125 | 12.5 | −0.625 |
| 2 | 14.25 | 13.5 | −0.75 |
| 3 | 15.375 | 14.5 | −0.875 |
| 4 | 16.5 | 15.5 | −1.0 |
| 5 | 17.625 | 16.5 | −1.125 |
| 6 | 18.75 | 17.5 | −1.25 |
| 7 | 19.875 | 18.5 | −1.375 |
| **8** | **21.0** | **19.5** | **−1.5 (maximum)** |
| 9 | 22.125 | 21.0 | −1.125 |
| 10 | 23.25 | 22.5 | −0.75 |
| 11 | 24.375 | 24.0 | −0.375 |
| 12 | 25.5 | 25.5 | 0 |

**Maximum divergence: 1.5 km, at step 8 of 12 (≈7.7% of that week's compounding-path volume).** The two shapes are not cosmetically different — compounding produces a **convex** path (smaller absolute increases early while the base is small, larger absolute increases later as the base grows, until the 2.0km cap flattens it near the end), while interpolation produces a **straight-line** path. They only agree at the two endpoints by construction; every intermediate week differs, by a materially non-trivial margin at the midpoint.

## 4. Classification

```
INTENTIONAL_FREQUENCY_POLICY
```

**With an important, honestly-stated nuance**: this is not "intentional" in the sense of a documented A/B comparison against 4D's shape — no such comparison exists anywhere in the evidence trail. It is intentional in the sense that mattered for `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001`'s actual question (*"unified before 5D introduces a third shape,"* i.e., is this a real design choice or an accident to be cleaned up): the compounding shape was independently derived from real training-load literature convention at the evidence stage, explicitly validated week-by-week at the product-decision stage, and used to *discover* a real numeric conflict (the taper-floor issue) — none of which is consistent with "unexamined historical specialization." It was designed on its own merits, not copied, not defaulted-to, not an implementation shortcut.

This does **not** mean the divergence (§3) is irrelevant to future work — a genuine, disclosed 5D+ design question remains: should a third Frequency introduce a third shape, reuse one of these two, or does the real, literature-grounded rationale for 3D's compounding shape (which is about *how a low-frequency, few-session structure concentrates load week-to-week*, not about the number "3" specifically) generalize to other Frequencies too? **Not answered here** — explicitly out of scope per this phase's own instruction ("do NOT propose a 5D trajectory shape — that's FREQ.3's job").

`TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001` closed as: **real, deliberate, evidence-grounded divergence — not technical debt requiring unification.** No code changed.
