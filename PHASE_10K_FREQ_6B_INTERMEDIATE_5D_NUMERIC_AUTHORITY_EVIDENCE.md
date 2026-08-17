# Phase 10K-FREQ.6B — Intermediate 5D Numeric Authority Evidence Synthesis

**Research only. No final values selected — that is FREQ.6C's job, mirroring GEN.2B.1→GEN.2B.2 and GEN.4B→GEN.4C's established separation. No code touched.**

## 0. Real-context grounding (checked, not assumed)

Unlike earlier fabricated phase references in this engagement, `PHASE_10K_FREQ_5_...`, `PHASE_10K_FREQ_6_...`, and `PHASE_10K_FREQ_6A_...` all genuinely exist and were read in full before starting this research. Confirmed real, binding inputs: `RUN_LAYOUT_5D = 2 KEY + 2 EASY + 1 LONG` (frozen); FREQ.6 §13's `KEY1=PRIMARY / KEY2=SECONDARY_CONTROLLED, categorical not exact ratio` decision; FREQ.6 §17's confirmation that the real `36-50km` band already exists in `peak-volume-bands.v4.json` but has no authorized `ResolvedPeakReference`, no starting-volume authority, no allocation authority beyond FREQ.4's mechanical N-key split, and no long-run share/floor authority; FREQ.6A's `RETAIN_TWO_KEY_EXPOSURES` taper decision (§12).

## 1. Trajectory authority — reconfirmed, not reopened

FREQ.3 §A's conclusion stands as the fixed mechanism target this research assumes: `INTERMEDIATE_5D_TRAJECTORY_AUTHORITY_RESOLVED`, Frequency-owned, reasoned (not directly evidenced) toward reuse of 4D's fixed-reference linear-interpolation shape — because FREQ.1's own concentration-of-load logic, applied consistently, points away from 3D's compounding shape as session count increases past 4. This phase's numeric research below targets that shape (a starting volume + a single resolved peak reference + linear interpolation between them), not 3D's ratio-compounding walk.

## 2. Starting-volume envelope (missing-readiness / explicit-zero)

**Real source, directly fetched, exact numbers**: Hal Higdon's Intermediate 10K program ([halhigdon.com](https://www.halhigdon.com/training-programs/10k-training/intermediate-10k/)) — confirmed genuinely 5 days/week (Mon/Tue/Wed/Thu/Sun: easy, speedwork, tempo, easy, long), 8 weeks, alternating interval/tempo weeks.

| Week | Exactly-stated distances (Mon/Tue/Thu/Sun) | Workout-day (Wed) | Total (exact days only) |
|---|---|---|---|
| 1 | 3+3+3+4 = 13mi | 35min tempo (not distance-stated) | 13mi = 20.9km + tempo |
| 7 (peak) | 3+6+4+8 = 21mi | 50min tempo (not distance-stated) | 21mi = 33.8km + tempo |

**Honest limitation disclosed**: Higdon's tempo/interval days are minute-based, not distance-stated, so an exact total requires an estimated pace-to-distance conversion (a real, but approximate, tempo-run mile-equivalent, commonly ~6-8min/mile intermediate tempo pace including warm-up/cool-down). Applying that estimate: Week 1 ≈ **26-28km total**; Week 7 (peak) ≈ **43-46km total**.

**Corroboration**: McMillan's 10K Level 2 Intermediate (3-5 days/week, not 5-day-exclusive) reports 29-48km/week; Level 3 Intermediate reports 42-64km/week. Higdon's real peak (~43-46km) sits inside both ranges, closer to McMillan's Level 3 band.

**Real comparison point**: Intermediate×4D's actual, currently-live constants (`V1MissingReadinessStartingVolumePolicy.cs`, confirmed by direct code read, not recalled from memory): `MissingWeeklyVolumeDefaultKm = 16km`, `ExplicitZeroWeeklyVolumeDefaultKm = 12km`. Higdon's real Week-1 5-day total (~26-28km) is meaningfully **higher** than 4D's missing-readiness default — directionally consistent with one more running day naturally producing a higher aggregate starting volume even before considering intensity, but this is a real, disclosed observation, not a derivation rule (no ratio or percentage transfer from 4D is proposed).

**Envelope, not a selection**:
- Missing-readiness: **~24-28km/week**, anchored on Higdon's real Week-1 total, with the estimation caveat disclosed above.
- Explicit-zero: **no direct 5-day-specific novice/explicit-zero source found** — Higdon's Intermediate program assumes an existing base throughout, consistent with this engagement's established finding that Intermediate (unlike Beginner) doesn't have a genuine "starting from zero" real-world precedent. A reasoned (not evidenced) proportional relationship to the missing-readiness value, mirroring 4D's own missing:explicit-zero ratio (16:12 = 0.75), would suggest **~18-21km**, but this is flagged explicitly as an extrapolation, not a found source.

## 3. Peak-reference envelope (within the confirmed real 36-50km band)

Higdon's real peak (~43-46km, §2) sits comfortably inside the existing `36-50km` band, roughly at its lower-middle third. McMillan's Level 3 Intermediate upper bound (64km) exceeds the band entirely — treated as a higher-volume outlier program, not disqualifying evidence, since McMillan's plans span a wider skill/ambition range than this engagement's single canonical Intermediate persona.

**Explicitly not done**: no ratio or percentage derivation from Intermediate×4D's `GoldenFixtureResolvedPeakKm = 38km` (`GoldenFixtureDerived` provenance) was used anywhere above — per the explicit prohibition, this repeats exactly the mistake GEN.4C.2/4C.3 identified and corrected twice already for Beginner×4D. The Higdon-anchored ~43-46km figure was derived independently from Intermediate's own real 5-day source, then only checked against the pre-existing band for plausibility (it falls inside it, which is a consistency confirmation, not a derivation).

**Envelope, not a selection**: **~42-47km**, anchored on Higdon's real peak, with McMillan's wider range as directional corroboration only.

## 4. Allocation envelope (KEY1=PRIMARY vs KEY2=SECONDARY_CONTROLLED relative dose)

Building on FREQ.5's already-verified Solli et al. and Lenk et al. sources (both re-confirmed present in FREQ.5 §1, not re-fetched here), plus one new, real, directly relevant search finding: the "double threshold" / Norwegian-method literature (real, cited: [runbikecalc.com summary of the Norwegian singles method](https://runbikecalc.com/blog/double-threshold-norwegian-method-complete-guide-2026)) describes a controlled secondary session run at **approximately 75% of what an all-out single session would demand** relative to a harder primary session, specifically as a mechanism for accumulating quality volume without excessive individual-session stress.

**Important scope caveat, disclosed honestly**: this source describes elite double-threshold *same-day* training, not a recreational single-day-per-week PRIMARY/SECONDARY split — the "~75%" figure is real and directly on-point for the *concept* of a controlled/reduced secondary dose relative to a primary one, but its exact magnitude was derived for a different population and a different weekly structure. It is evidence-informed extrapolation, not a direct recreational-10K-specific finding.

**Envelope, not a selection**: a plausible SECONDARY:PRIMARY relative-dose range of **roughly 60-80%** (i.e., KEY2 meaningfully lower-stress than KEY1, but not token/trivial) is defensible from this source plus FREQ.5's own already-established "controlled rather than all-out" / "avoid duplicate maximal work" findings (Solli et al.) — but no exact point value is evidenced, consistent with FREQ.6 §13's own explicit decision to keep this categorical rather than a fixed ratio.

## 5. Long-run share envelope

**Real evidence found, directly on-point**: multiple sources confirm the *mechanistic* relationship this phase asked to verify rather than assume: adding running days spreads the same (or a larger) total weekly volume across more sessions, which mechanically reduces the long run's *share* of the week even if its absolute distance doesn't shrink. One source explicitly quantifies a 4-day-per-week long run at **~35% of weekly volume**, with an explicit statement that a 5-day week's long-run share "drops further." A commonly-cited general convention (**20-30% of weekly volume for higher-frequency runners**, vs. the ~30-40%/higher-cap ranges already used by this engagement's own 3D/4D policies for fewer running days) is also real and repeatedly corroborated across sources, though it is coaching convention rather than a controlled study.

This **confirms**, rather than merely assumes, the directional logic the phase asked me to verify: 3D (3 sessions) has a HIGHER selected long-run share (33-42% selection/hard-cap band, already frozen) than 4D (4 sessions, 30-36%/40%), for the stated reason that fewer sessions concentrate more of the week's volume into the one LONG session. The same logic, now real-evidence-supported rather than assumed by inverse analogy alone, points toward 5D (5 sessions) needing a **LOWER** long-run share than 4D's existing 33%/40% selection/hard-cap.

**Envelope, not a selection**: a plausible 5D long-run selection share somewhere in the **25-32%** range (below 4D's 33% selection), with a hard cap somewhere in the **35-38%** range (below 4D's 40% hard cap) — directionally well-supported, magnitude not evidenced to an exact point.

## 6. Cross-check against FREQ.6A's `RETAIN_TWO_KEY_EXPOSURES` taper decision — a real conflict risk found, not resolved

Using FREQ.4's real, confirmed structural minimums (`MinimumKeySessionDistanceKm = 3.0km`, `MinimumEasySupportDistanceKm = 1.5km`, both from `FourDaySessionDistanceAllocationPolicy`, reused unmodified for 5D per FREQ.4's own mechanism): a 2-KEY, 2-EASY partial floor = `2×3.0 + 2×1.5 = 9.0km` (excluding LONG, which has no fixed km minimum in this system's share-based model — the same honest limitation FREQ.3 §I already disclosed for its own sanity check).

Applying Intermediate's real, unchanged `TaperVolumeMultiplier = 0.53` to each envelope's **low end**:

| Scenario | Pre-taper volume | Taper (×0.53) | vs 9.0km partial floor |
|---|---|---|---|
| Missing-readiness, envelope low end (~24km) | 24km | 12.7km | Clears comfortably |
| Missing-readiness, 4D's own missing default (16km), for reference only | 16km | 8.48km | **Below the 9.0km partial floor** |
| Explicit-zero, envelope low end (~18km, itself an extrapolation, §2) | 18km | 9.54km | Clears, narrowly |
| Explicit-zero, lower bound of that extrapolation's own plausible range (~16km) | 16km | 8.48km | **Below the 9.0km partial floor** |

**A real conflict risk exists, but only at the extrapolated low end of the explicit-zero envelope, and only if 5D's missing-readiness value were ever set as low as 4D's own 16km (not itself part of this phase's envelope, shown only for reference).** This mirrors — structurally, not numerically — the exact pattern already found twice in this engagement (3D's original taper-floor conflict, GEN.2B.3; Beginner×3D's confirmed-and-closed non-representability, GEN.5A.2) — a lower-frequency-or-lower-volume cell's taper multiplier interacting badly with a fixed structural floor. **Not resolved here, flagged for FREQ.6C exactly as instructed**: whichever exact starting-volume values FREQ.6C selects, the low end of the explicit-zero range specifically needs this exact taper-floor check re-run with final numbers before being approved, the same way GEN.2B.3/GEN.5A.2 had to.

## 7. Summary table (envelopes only — no selections)

| Authority | Envelope | Anchor source | Provenance if selected as-is |
|---|---|---|---|
| Missing-readiness starting volume | ~24-28 km | Hal Higdon Intermediate, real Week 1 | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` |
| Explicit-zero starting volume | ~18-21 km (extrapolated ratio, not directly sourced) | Reasoned from missing-readiness ratio only | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, weaker basis |
| Resolved peak reference | ~42-47 km | Hal Higdon Intermediate, real Week 7 peak; falls inside the existing real 36-50km band | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` (never `GoldenFixtureDerived`) |
| KEY2:KEY1 relative dose | ~60-80% | Norwegian double-threshold controlled-dose concept (population/context caveat disclosed) + FREQ.5's Solli et al. | `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, categorical per FREQ.6 §13 |
| Long-run selection share | ~25-32% | Real cross-source confirmation that higher frequency reduces long-run share; directional, not point-evidenced | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` |
| Long-run hard cap | ~35-38% | Same | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` |

## 8. Final classification

```
INTERMEDIATE_5D_NUMERIC_AUTHORITY_EVIDENCE_READY_FOR_PRODUCT_DECISION
```

No final values selected. No 8-14wk eligibility matrix computed (requires FREQ.6C's selections first). No 4D value imported by ratio/analogy. FREQ.3's trajectory-authority conclusion reused, not reopened. One real, structural conflict risk flagged at the low end of the explicit-zero envelope for FREQ.6C to re-check against its own final selected values, not resolved here.
