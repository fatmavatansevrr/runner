# PHASE DIST-GEN.8 — Post-15K/16K Multi-Target Authority Pattern, Distance-Banding Evidence, and Next-Expansion Decision

**Classification: GOVERNANCE / ARCHITECTURE / AUTHORITY-PATTERN AUDIT.** No production code, catalog artifact, database, API, or frontend file was touched in this phase. This phase determines what reusable authority pattern has actually emerged now that two distinct interior projected targets (15K, 16K) are both authority-closed, dark-proven, and publicly proven, and selects exactly one next expansion axis.

## 1. Current post-DIST-GEN.7 baseline

Public projected-distance cells, per `PHASE_DIST_GEN_7_...md` and `PHASE_LEDGER.md` row 52: `15.0km × Intermediate × 4D` (DIST-GEN.5's Standard Core range, 10-14W) and `16.0km × Intermediate × 4D` (10-14W) are both PUBLIC. Both reuse the identical, unmodified `HALF_MARATHON__4D__INTERMEDIATE` catalog identity. Public eligibility is governed by `Dark16KPilotEligibilityPolicy.ApprovedPublicCells` (exactly two entries), kept independently declared from the dark-only `ApprovedDarkCells` registry. Every other projected target/cell remains unsupported. This state is **not reopened** by this phase — it is the frozen input to the analysis below.

## 2. Four-point authority matrix

Read directly from `VolumeSafetyPolicy.cs` (all four instances: `Default` for TEN_K, `HalfMarathonIntermediate4DTargetDistance15K`, `HalfMarathonIntermediate4DTargetDistance16K`, `HalfMarathonIntermediate4D` for canonical HM), `TargetDistance15KHorizonPolicy.cs`/`TargetDistance16KHorizonPolicy.cs`, and `RaceHorizonPolicy.cs` — not reconstructed from memory of earlier prompts.

| Field | TEN_K | 15K | 16K | HALF_MARATHON |
|---|---|---|---|---|
| TargetDistanceKm | 10.0 (canonical) | 15.0 (projected) | 16.0 (projected) | 21.0975 (canonical) |
| Structural family | TEN_K (own) | HALF_MARATHON (reused) | HALF_MARATHON (reused) | HALF_MARATHON (own) |
| MinimumCoreWeeks | 8 | 10 | 10 | 10 |
| PreferredCoreWeeks | 12 | 12 | 12 | 14 |
| MaximumCoreWeeks | 14 | 14 | 14 | 16 |
| PeakVolumeBandMin/Max | not a distinct tuple on `Default` | 34.0 / 46.0 | 34.0 / 46.0 | not a distinct tuple on the canonical record (real historical HM band cited in DIST-GEN.1 as [36,50]) |
| SelectedPeakReference | 38.0 | 40.0 | 40.0 | 43.0 |
| PreferredWeeklyIncreaseRate | 0.07 | 0.07 | 0.07 | 0.07 |
| HardWeeklyIncreaseRate | 0.08 | 0.08 | 0.08 | 0.08 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | 2.5 | 2.5 | 2.5 |
| CalibrationStartingVolumeKm | 24.0 | 23.5 | 23.5 | 25.0 |
| PreferredLongRunShareMin | 0.30 | 0.30 | 0.30 | 0.30 |
| PreferredLongRunShareMax | 0.36 | 0.36 | 0.36 | 0.36 |
| SelectedLongRunShare | 0.33 | 0.33 | 0.33 | 0.33 |
| HardLongRunShareCap | 0.40 | 0.40 | 0.40 | 0.40 |
| PreferredAbsolutePeakLongRunKm | null (not populated on `Default`) | **14.5** | **15.0** | 19.0 |
| StartingAbsoluteLongRunCapKm | null | null | null | null (non-null only at HM's own 5D/6D) |
| TaperDurationWeeks | 1 | 2 | 2 | 2 |
| TaperWeek1Multiplier | 0.53 (single-week) | 0.70 | 0.70 | 0.70 |
| TaperWeek2Multiplier | n/a | 0.43 | 0.43 | 0.43 |
| TaperMechanism | single-week | non-chained, fixed-anchor | non-chained, fixed-anchor | non-chained, fixed-anchor |
| Progression family | TEN_K's own | HM Intermediate 4D (reused) | HM Intermediate 4D (reused) | HM Intermediate 4D (own) |
| Target-race-pace semantic | generic (target-distance-driven) | generic (same mechanism) | generic (same mechanism) | generic (same mechanism) |
| Riegel behavior | generic, unmodified | generic, unmodified | generic, unmodified | generic, unmodified |
| Readiness model | generic | generic | generic | generic |
| Calendar model | generic | generic | generic | generic |
| Adaptation model | generic | generic | generic | generic |

## 3. Current public/dark matrix

| Target | Public | Dark |
|---|---|---|
| TEN_K (canonical) | Live | N/A (not a projection) |
| HALF_MARATHON (canonical) | Live | N/A |
| 15.0 (Intermediate/4D) | Live | Live |
| 16.0 (Intermediate/4D) | Live | Live |
| Every other target/cell | Unsupported | Unsupported unless independently authorized |

## 4. Generic infrastructure proven twice

| Component | Classification |
|---|---|
| `Custom` + `target_distance_km` request contract | `GENERIC_PROVEN_MULTI_TARGET` |
| `CanonicalDistanceFamilyResolver` | `GENERIC_PROVEN_MULTI_TARGET` (now has two real production consumers routing through it with different values) |
| Projected-target authority registry pattern (`ApprovedDarkCells`/`ApprovedPublicCells`) | `GENERIC_PROVEN_MULTI_TARGET` at n=2; see §9/§64 for scalability beyond 2 |
| Dark/public registry separation | `GENERIC_PROVEN_MULTI_TARGET` — proven exactly once as a genuine architectural boundary (DIST-GEN.6 built dark-only, DIST-GEN.7 built public independently) |
| Catalog candidate reuse | `GENERIC_PROVEN_MULTI_TARGET` |
| Horizon authority dispatch | `GENERIC_BUT_ONLY_PARTIALLY_PROVEN` — works at n=2 via an explicit boolean/pattern-match cascade, not yet proven to scale past a handful of targets without becoming branching debt (§9/§65) |
| Numeric policy dispatch (volume/LR/taper) | `GENERIC_BUT_ONLY_PARTIALLY_PROVEN`, same reasoning |
| Target-race-pace resolution | `GENERIC_PROVEN_MULTI_TARGET` — zero code change was needed for either 15K or 16K |
| Riegel | `GENERIC_PROVEN_MULTI_TARGET` |
| Exact-target persistence | `GENERIC_PROVEN_MULTI_TARGET` — DIST-GEN.7 §40 explicitly proved this across two independently public targets |
| Response DTOs (`RequestedTargetDistanceKm`) | `GENERIC_PROVEN_MULTI_TARGET` |
| Display formatter | `GENERIC_PROVEN_MULTI_TARGET` |
| Home/Calendar/Detail/PlanDetails | `GENERIC_PROVEN_MULTI_TARGET` |
| Mutations (Complete/Not-Today/Cancel) | `GENERIC_PROVEN_MULTI_TARGET` |
| Rollback behavior (creation-eligibility independent of plan operability) | `GENERIC_PROVEN_MULTI_TARGET` |

## 5. Exact-target registry audit

The current implementation is a clean exact-target registry at the eligibility layer (`ApprovedDarkCells`/`ApprovedPublicCells`, each a small explicit list, exact-match lookup) but a **branching cascade at the dispatch layer** — `PlanServices.cs`'s horizon-gate logic, for example, is `isDark15KTarget ? TargetDistance15KHorizonPolicy... : isDark16KEligible ? TargetDistance16KHorizonPolicy... : RaceHorizonPolicy...`, and the equivalent pattern repeats at the volume/LR dispatch call sites. This is not yet a "growing collection of target-specific branches" in a harmful sense (it is still fully auditable and correct at n=2), but it is a ternary/if-else chain that grows linearly with each new target, not a lookup. **Classification: `MANAGEABLE_BUT_NEEDS_GENERALIZATION`** — healthy today, but a third target would make this pattern noticeably harder to read and would be the natural point to convert it into a keyed dictionary lookup (see §65/§81 handoff note).

## 6. Mechanism-vs-parameter distinction

Mechanisms (reused unmodified regardless of target): the generic phase allocator, `RaceHorizonPolicy`'s 5-argument `Decide` overload, the Riegel formula, target-race-pace resolution, the readiness model, calendar materialization, adaptation. Parameters (independently authored per target, even when values coincide): `PeakVolumeBandMin/Max`, `SelectedPeakReference`, `CalibrationStartingVolumeKm`, `PreferredAbsolutePeakLongRunKm`, `MinimumCoreWeeks`/`PreferredCoreWeeks`/`MaximumCoreWeeks` (as numbers, even though the horizon-decision *mechanism* that consumes them is shared). This distinction is maintained throughout §7 below and is the single most important discipline this phase applies.

## 7. Authority-provenance matrix

For every 15K/16K field where the numeric value is equal, per §5 of the governing prompt's own central rule (value equality ≠ authority equality):

| Field | 15K value | 16K value | Provenance |
|---|---|---|---|
| Structural family | HALF_MARATHON | HALF_MARATHON | `SHARED_PARENT_FAMILY_AUTHORITY` |
| MinimumCoreWeeks | 10 | 10 | `SHARED_PARENT_FAMILY_AUTHORITY` — both are the reused HM template's own real phase-minimum sum, a structural fact, not independently decided |
| PreferredCoreWeeks | 12 | 12 | `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE` — DIST-GEN.5 explicitly re-ran DIST-GEN.1's own evidence-weighing process independently and reached the same number; disclosed as convergence, not shared authority |
| MaximumCoreWeeks | 14 | 14 | `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE`, same reasoning |
| PeakVolumeBandMin/Max | [34,46] | [34,46] | `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE` — DIST-GEN.5 §28 re-ran the overlap-based derivation independently |
| SelectedPeakReference | 40.0 | 40.0 | `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE`, same reasoning |
| PreferredWeeklyIncreaseRate/HardWeeklyIncreaseRate | 0.07/0.08 | 0.07/0.08 | `SHARED_LEVEL_FREQUENCY_AUTHORITY` — identical across **all four** anchors including TEN_K, far too broad to be a 15-16K-specific pattern |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | 2.5 | `SHARED_LEVEL_FREQUENCY_AUTHORITY`, same reasoning (identical across all four anchors) |
| CalibrationStartingVolumeKm | 23.5 | 23.5 | `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE` for the *value*; the *derivation methodology* (HM's own calibration/peak ratio applied to each target's own independently-derived peak) is `SHARED_PARENT_FAMILY_AUTHORITY` (a mechanism, not a parameter — see §6) |
| LR share quadruple | 0.30/0.36/0.33/0.40 | 0.30/0.36/0.33/0.40 | `SHARED_LEVEL_FREQUENCY_AUTHORITY` — identical across all four anchors, the strongest evidence in the entire engagement for this classification |
| PreferredAbsolutePeakLongRunKm | 14.5 | 15.0 | `INDEPENDENT_TARGET_AUTHORITIES_DIFFERENT_VALUE` — the one field that actually differs, mechanically forced by the LR-below-target invariant |
| StartingAbsoluteLongRunCapKm | null | null | `SHARED_LEVEL_FREQUENCY_AUTHORITY` (frequency-driven: non-null only at 5D/6D, per DIST-GEN.4's own research; nothing to do with distance) |
| TaperDurationWeeks/multipliers/mechanism | 2/0.70/0.43/non-chained | 2/0.70/0.43/non-chained | `SHARED_PARENT_FAMILY_AUTHORITY` — TEN_K's own genuinely different taper (1-week, 0.53) proves this is HM-parent-inherited, not a "15-16K band"; both targets independently inherited the identical HM mechanism because they reuse the identical HM catalog identity |
| Target-race-pace/Riegel/readiness/calendar/adaptation | generic | generic | `SHARED_GENERIC_MECHANISM` |

## 8. Phase-topology pattern

Confirmed: both 15K and 16K reuse the identical, unmodified `half-marathon-master.v5.json` phase topology and generic allocator — the structural minimum (10) is `SHARED_PARENT_FAMILY_AUTHORITY`, not product horizon authority. This is explicitly distinguished from Preferred/Maximum (§7), which remain independently-derived target authority even though the structural floor beneath them is shared.

## 9. Horizon pattern

Comparing all four real triples — TEN_K 8/12/14, 15K 10/12/14, 16K 10/12/14, HM 10/14/16 — the correct model (per DIST-GEN.2A's own established correction, now doubly confirmed by 15K's independent closure) is: **(C) parent-family structural floor plus target-specific preferred/max**. The floor is a structural fact about which template is reused (10 for anything reusing HM's own template, 8 for TEN_K's own template); Preferred/Maximum are independently-evidenced, target-specific decisions that happen, for the two currently-authorized interior targets, to converge on the same numbers as each other (but not on HM's own 14/16). This is NOT evidence for a "15-16K horizon band" — it is evidence that the parent-family-floor model is correct and that two independent evidence-gathering exercises, both working from broadly similar external sources (10-mile and 15K plans), reached similar conclusions.

## 10. Peak-volume pattern

Four real points: TEN_K 38.0, 15K 40.0, 16K 40.0, HM 43.0. This shows a plausible directional trend (increasing with distance) but with only one genuinely independent interior data-point-pair (15K/16K, only 1km apart) between the two canonical anchors — **not enough to distinguish continuous distance dependence from discrete target-specific values that happen to trend the same direction the canonical anchors already do.** Classification: `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` (see §31/§69 for what a third point would need to resolve). No regression was run; no curve was fit (§43).

## 11. Selected-peak pattern

Selected peak tracks each target's own independently-selected reference, not a governed band midpoint — DIST-GEN.1/5 each independently used an overlap-based selection method (drawing on both canonical anchors' own bands plus external evidence), and both landed on 40.0. This is `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE`, per §7, not evidence of a governed band-midpoint rule.

## 12. Growth-rate pattern

0.07/0.08 is identical across all four anchors, including TEN_K (a different structural family entirely). This is decisive: growth-rate authority was never target-distance-dependent to begin with — it is `SHARED_LEVEL_FREQUENCY_AUTHORITY` (Intermediate × 4D), a classification DIST-GEN.0 already suggested and this four-point comparison now confirms with the strongest possible evidence (agreement across a structurally unrelated family).

## 13. Absolute-weekly-cap pattern

Identical reasoning and identical classification to §12 — 2.5km is `SHARED_LEVEL_FREQUENCY_AUTHORITY`, confirmed by TEN_K's own independent agreement.

## 14. Calibration pattern

15K/16K's calibration values (23.5 each) are `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE` as raw numbers, but both were reached via the identical, `SHARED_PARENT_FAMILY_AUTHORITY` ratio-reuse *methodology* (HM's own calibration÷peak ratio, 0.5814, applied to each target's own independently-selected peak). Calibration remains strictly test/reference authority in both cases — no code path anywhere treats it as a runtime readiness substitute (confirmed unchanged since DIST-GEN.1).

## 15. LR-share pattern

The 0.30/0.36/0.33/0.40 quadruple is identical across all four anchors — the single strongest piece of evidence anywhere in this engagement for `SHARED_LEVEL_FREQUENCY_AUTHORITY`. This field should be formally understood as Intermediate×4D authority, not target-distance authority, and was never a "15-16K band" candidate to begin with.

## 16. Absolute-peak-LR pattern

The most target-dependent field in the entire matrix: TEN_K null/not-applicable, 15K 14.5, 16K 15.0, HM 19.0. No two of the four values agree. This field shows **no** banding signal at all — it is mechanically constrained per-target by the binding invariant (§17) and independently evidenced per target. Classification: `EXACT_TARGET_SPECIFIC`.

## 17. LR-below-target invariant review

Cumulative evidence across DIST-GEN.0 (the original conceptual invariant), DIST-GEN.1 (the Hal Higdon 10-mile/15K combined source's own near-full-distance long run, deliberately not treated as sufficient to reopen), and DIST-GEN.5 (the same combined source re-examined, again found to be a non-generalizable dual-target outlier, while three dedicated 15K-only sources all showed long runs comfortably below the 15K distance) all point the same direction. **Output: `RULE_REAFFIRMED`.** No new evidence in this phase changes that conclusion; the rule remains binding production authority, unchanged here.

## 18. Starting-LR-cap pattern

Null at TEN_K/15K/16K/HM-4D; non-null only at HM's own 5D/6D cells (per DIST-GEN.4's own research). This is `SHARED_LEVEL_FREQUENCY_AUTHORITY` (frequency-driven — the field activates only at higher weekly frequencies, regardless of target distance), not target-specific and not distance-banded.

## 19. Taper pattern

TEN_K's own genuinely different taper (1-week, single 0.53 multiplier) versus 15K/16K/HM's shared 2-week/0.70/0.43/non-chained mechanism is decisive: this is `SHARED_PARENT_FAMILY_AUTHORITY` — both interior targets independently inherited HM's own real taper mechanism because they reuse HM's real catalog identity, not because "intermediate distances between 10 and 21.1km share a taper band." There is no such thing as an `INTERMEDIATE_DISTANCE_TAPER_FAMILY` distinct from "whichever structural parent a target reuses" — the correct, more precise model is parent-family inheritance, and this four-point comparison (with TEN_K as the crucial contrast case) proves it.

## 20. Target-race-pace pattern

Confirmed `CONTINUOUS_GENERIC_MECHANISM` — the same resolver, parameterized purely by whatever `RequestedTargetDistanceKm`/`TargetDistanceKmOverride` value is in play, with zero code branching on which target is active. No pace band is needed or would mean anything, since the mechanism was never discrete to begin with.

## 21. Riegel pattern

Same classification and reasoning as §20 — `CONTINUOUS_GENERIC_MECHANISM`. Explicitly, per this phase's own required discipline (§25 of the governing prompt), this is **not** treated as evidence that training-load fields (peak volume, LR, taper, calibration) could also safely be continuous/formula-driven — pace/Riegel's continuity is a property of race-performance-equivalence math, not of training-load prescription, and the two remain categorically distinct per DIST-GEN.0's own standing interpolation prohibition (§42/§43).

## 22. Goal-feasibility pattern

Unmodified, generic, target-distance-driven exactly like pace/Riegel — `SHARED_GENERIC_MECHANISM`.

## 23. Readiness pattern

Recent-weekly-volume/recent-longest-run/recent-runs-per-week semantics are identical and target-distance-independent across all four anchors — readiness itself is generic infrastructure, while any *threshold* that consumes readiness inputs (e.g., calibration as a reference point) remains target-specific per §14. No new readiness threshold was found or is recommended.

## 24. Calendar/adaptation pattern

Both entirely target-independent — `GENERIC_REUSABLE_INFRASTRUCTURE`. No banding concept applies to either; there is nothing to band.

## 25. 15K–16K shared-policy hypothesis

Tested explicitly, per field:

| Field | Hypothesis (one shared 15-16K policy bundle) |
|---|---|
| Horizon triple | `NOT_APPLICABLE` — the real ownership is parent-family (minimum) + independent-per-target (preferred/max), not a 15-16K bundle |
| Peak volume/selected peak/calibration | `INSUFFICIENT_EVIDENCE` — converged, but not enough points to call it a bundle vs. coincidence |
| Growth/cap/LR shares/starting LR cap | `CONTRADICTED` as a "15-16K" hypothesis specifically — these are broader (Intermediate×4D or frequency) invariants, not scoped to these two targets |
| Absolute peak LR | `CONTRADICTED` outright — the values differ |
| Taper | `CONTRADICTED` as a "15-16K" hypothesis — it is parent-family, proven by TEN_K's own contrast |
| Generic mechanisms | `NOT_APPLICABLE` — never target-scoped to begin with |

**Conclusion: a single monolithic 15-16K policy bundle is not supported by any field** — every field that matches either matches far more broadly (family/frequency invariant) or matches only by unconfirmed convergence, and the one field with real signal (absolute peak LR) actively contradicts the hypothesis. This directly satisfies the governing prompt's own §28 expectation.

## 26. Componentized-sharing hypothesis

The more nuanced hypothesis — some components shared, others exact-target — is well supported, but not exactly in the conceptual shape the governing prompt's own example (§29) speculatively proposed. The real division, derived from provenance (§7) rather than assumed: **shared** = growth rates, absolute weekly cap, LR share quadruple, starting LR cap, taper mechanism+multipliers (all via family/frequency invariance, not "15-16K sharing"); **independently-converged-but-not-yet-bandable** = horizon preferred/max, peak volume band, selected peak, calibration methodology; **genuinely exact-target-specific** = absolute peak LR (the only field with confirmed divergence).

## 27. Shared invariant vs distance band

Applying §30's own discipline: LR shares, growth rates, absolute weekly cap, starting LR cap, and taper are **not** a 15-16K distance band — they are Intermediate×4D-invariant or HALF_MARATHON-parent-invariant, confirmed by their agreement with TEN_K (a structurally unrelated family) or by TEN_K's own contrast (taper). Classifying them at the highest defensible abstraction (per §30's explicit instruction) means recording them as `LEVEL_FREQUENCY_INVARIANT` or `PARENT_FAMILY_INVARIANT` authority, never as a "band," which would understate how broadly they actually apply and would incorrectly imply they might stop applying outside some future band boundary.

## 28. Distance-banding standard

Applying §10 of the governing prompt's own required standard (a band needs more than two endpoints matching): no field in this matrix meets the bar. The candidates with real convergence (peak volume, selected peak, calibration) have exactly one adjacent pair of independently-derived points, no direct canonical rule, no strong domain evidence beyond the two closure phases' own convergent findings, and no third internal target authority to corroborate. **No field passes the band-definition standard.**

## 29. Field-specific bandability

See §67's table below — this phase deliberately does not answer bandability as one yes/no for the whole policy, per §11's own instruction to assess every field separately.

## 30. Band-evidence classifications

Bounded, non-boundary-defining evidence update per field (mirroring but sharpening DIST-GEN.5's own §57):

| Field | Classification |
|---|---|
| Horizon minimum | `PARENT_FAMILY_INVARIANT` (not banding — a structural fact) |
| Horizon preferred/max | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` |
| Peak volume band/selected peak | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` |
| Calibration | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` (value); `PARENT_FAMILY_INVARIANT` (methodology) |
| Growth rates/absolute weekly cap/LR shares/starting LR cap | `LEVEL_FREQUENCY_INVARIANT` — not a band, a stronger/different classification entirely |
| Absolute peak LR | `EXACT_TARGET_SPECIFIC` |
| Taper | `PARENT_FAMILY_INVARIANT` |
| Pace/Riegel/feasibility/readiness/calendar/adaptation | `CONTINUOUS_GENERIC_MECHANISM` |

## 31. Third-target information need

Per §69's own "third-point principle": a third interior authority point is needed specifically to distinguish, for the `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` fields (peak volume, selected peak, calibration, horizon preferred/max), whether the 15K/16K convergence (a) continues as a genuinely stable plateau across a meaningfully wider interior range, or (b) was itself just a product of 15K and 16K being only 1km apart and would diverge at a more distant interior point, or (c) begins shifting toward HM's own higher values before reaching HM itself. A point immediately adjacent to 16K (e.g. 17K) would mostly re-test (b) without much new information; a point positioned meaningfully further toward HM would test (a) vs (c) directly, which is the more valuable open question.

## 32. 12K revisit

DIST-GEN.4's original rejection (reopens the unresolved 10-12K structural-parent boundary) is **unchanged by having 15K/16K's own authority closed** — nothing about 15K/16K's own closure provides any new evidence about whether HALF_MARATHON is the correct structural parent near the 10K boundary specifically; that is an entirely separate question from interior-band behavior further from the boundary. 12K would still require its own architecture decision (parent-family confirmation) as a prerequisite to any numeric authority work, exactly as before. **Classification: still a poor next-target choice, for the same reason as DIST-GEN.4 found, honestly reconfirmed rather than silently assumed.**

## 33. 18K revisit

18K sits at a genuinely informative middle position: 2km from 16K, 3.1km from HM. It would directly test the §31 question (does the 15-16K convergence continue, or start shifting toward HM?) without simply re-confirming what's already known. Real external 18K training-plan evidence was not independently re-gathered in this phase (per §48's "reuse prior research first, supplement only for unresolved questions" — 18K's own plan-structure evidence was not previously gathered in DIST-GEN.0/1/5 and would need fresh, narrow research if selected, which is itself informative and appropriately scoped for a future authority-closure phase, not this governance phase).

## 34. 20K revisit

20K sits only 1.1km from HM's own 21.1km ceiling. It would most likely show near-total convergence toward HM's own numeric authority (peak ≈43.0, calibration ≈25.0, absolute peak LR approaching but still below 20.0) — a real, useful data point for eventually answering "at what point does HM's own authority become the right reuse," but it teaches comparatively less about the ambiguous *middle* of the interior range than 18K does, since HM's own values are already fully known and 20K is unlikely to surprise.

## 35. 10-mile exact analysis

Public/dark authority remains exactly `16.0`, not `16.0934`. DIST-GEN.5's own additional observation (the Higdon combined-program source's near-full-distance long run, at a ratio of ~1.0 relative to the 10-mile distance, versus 15K/16K's own real ratios of 0.967/0.9375) is weak evidence *against* a clean alias, not for one — it suggests the 10-mile-exact target's own real training authority, if ever closed, might genuinely need a different (probably higher) absolute-peak-LR value than 16K's own 15.0, since 16K's own ceiling sits meaningfully below even its own target while the one piece of available 10-mile-specific evidence sits close to the full distance. **No positive evidence exists for training-authority equivalence between 16.0934 and 16.0.** Classification: `INSUFFICIENT_EVIDENCE`, unchanged from DIST-GEN.4's own deferral, now with one additional weak data point pointing away from a simple alias rather than toward one.

## 36. Authority precision vs identity precision

The 15K/16K experience strongly confirms and sharpens the two-layer model DIST-GEN.0/6 already implied: `RequestedTargetDistanceKm` (exact user-facing identity, unrounded, already proven to coexist correctly with a completely independent `CanonicalDistanceFamily`) versus a coarser, explicitly-governed training-authority key (today, implicitly, "which named policy instance/registry entry was resolved"). The registries built in DIST-GEN.6/7 (`ApprovedDarkCells`/`ApprovedPublicCells`) are, in effect, an informal first implementation of a `TrainingAuthorityKey` concept, even though no such named type exists yet. This model is sound and validated; formalizing it explicitly (a distinct named key type, rather than an implicit `TargetDistanceKm` match) is a reasonable future refinement but not urgent at n=2 targets.

## 37. Alias model

Conceptually: if 10-mile-exact were ever aliased to 16K's own authority, `RequestedTargetDistanceKm` would remain exactly `16.0934` (preserving exact user identity and display), while a separate, explicit `TrainingAuthorityKey`-shaped field would resolve to the *same* registry entry 16.0 already uses. This would NOT be nearest-distance fallback (which the product invariant, §60, correctly continues to prohibit) — it would be a single, explicitly-governed alias grant, auditable exactly like today's exact-match registry entries. **This model is architecturally sound and NOT approved for use here** — §35 found insufficient, and mildly contrary, evidence to justify actually granting it for the 10-mile/16K pair specifically.

## 38. Exact-target registry model

At two targets, this model is unambiguously working well: clear provenance, fail-closed by construction, cheaply auditable. Extrapolating: at 3-5 targets it likely remains manageable with a modest amount of duplicated declaration (per-target sibling classes/instances), which is acceptable, intentional governance duplication (§40), not technical debt. Beyond roughly 5 targets, the current dispatch-layer branching cascade (§5/§9) would become a real readability/maintainability cost, though the registry/eligibility layer itself would likely remain fine even then.

## 39. Component-authority registry model

DIST-GEN.0's original distributed-typed-authority instinct is now more clearly justified by real evidence than it was at the time: §7's provenance matrix shows several fields (growth, cap, LR shares, starting LR cap, taper) are genuinely owned at a *different, broader* scope (level/frequency or parent-family) than the target-specific fields (peak volume, selected peak, calibration, absolute peak LR). A componentized model — where a target references a shared `LevelFrequencyAuthority` for the first group and a distinct `TargetSpecificVolumeAuthority`/`TargetSpecificLongRunAuthority` for the second — would make this real, already-true ownership structure explicit in code rather than implicit in each `VolumeSafetyPolicy` instance's own repeated field values. This is a genuinely worthwhile normalization **when and if** a third target is added (since a third instance would repeat the shared fields a third time), but is not urgent enough to justify its own standalone phase before a third target exists to prove the pattern needs it.

## 40. Monolithic target-policy bundle risk

The current duplication (each `VolumeSafetyPolicy` instance restating identical growth/cap/LR-share/starting-LR-cap values) is judged **intentional, acceptable governance duplication**, not technical-ownership debt — per §7, these ARE independently-applicable Intermediate×4D facts being correctly re-asserted per instance, and the repetition itself is what let this phase directly observe (via literal equality across four independent records) that they are in fact shared invariants. Collapsing them into one physically-shared constant now, before a third target exists to stress-test the abstraction, would be a premature optimization with no proven benefit yet.

## 41. Formula/interpolation model rejection or support

Reassessed with two real interior points: the answer remains **NO** — no field is justified as continuous/formula-based beyond the pace/Riegel mechanisms already correctly classified as such. Two points can suggest a direction but cannot themselves justify a formula (§42/§43's own explicit prohibition against curve-fitting four points is respected in full — no regression, interpolation, or ratio-scaling was performed anywhere in this analysis to produce any classification above).

## 42. Frequency-expansion revisit

DIST-GEN.4's own finding stands, now reinforced: phase topology and horizon mechanisms are confirmed broadly reusable (§8-9), but peak volume, calibration, LR policy, and (for 5D/6D) KEY2 dual-lane semantics would all require genuinely new, per-cell authority for either 15K or 16K at a new frequency — the same cost structure DIST-GEN.4 found for 16K alone. Having two targets does not reduce this cost; if anything it doubles the number of frequency-expansion decisions eventually needed (15K-at-3D and 16K-at-3D would each need their own authority, per §7's own finding that target-specific fields don't share across targets even when the frequency is held constant).

## 43. Level-expansion revisit

Similarly unchanged in kind, and now doubled in scope for the same reason as §42. Experienced remains excluded (EXP.0, product-wide, not reopened here). Two targets make level expansion neither more nor less architecturally attractive — the authority cost per (target × level) cell is independent of how many other targets already exist.

## 44. KEY2 orthogonality

Confirmed still orthogonal to distance-banding: KEY2/dual-lane semantics are a frequency×catalog-structure concern (DIST-GEN.4 §17), entirely unrelated to which target distance is projected. Resolving distance-banding first would not reduce KEY2's own complexity at all — the two dimensions remain genuinely independent, and this phase's own analysis does not change that.

## 45. Product-value evidence

No internal product telemetry or usage-demand source exists anywhere in this repository. **SOURCE_SILENCE** for demand around 15K, 16K, 10-mile, 18K, 20K, or any frequency. External race prevalence (from earlier research) is contextual evidence about race-calendar reality, not internal product demand, and is not used here to justify any option.

## 46. External research additions if any

None performed. Every question this phase needed to answer was resolvable from existing DIST-GEN.0/1/4/5/6/7 evidence plus direct re-reading of current policy source. No new external sources were fetched.

## 47. Reuse-strength matrix

| Option | Generic infrastructure reuse | New training-authority count | Architectural novelty | Implementation scope | Public-risk surface |
|---|---|---|---|---|---|
| Third exact target (18K) | High (same registries/mechanisms) | Medium (peak/selected-peak/calibration/absolute-LR/preferred-max, likely) | Low-medium (proves/refutes band-candidate fields) | Medium (mirrors DIST-GEN.5/6/7's own three-phase shape) | Low (additive, dark-first) |
| Frequency expansion | Medium-high | Medium-high per cell (doubled: 15K AND 16K) | Low (already understood pattern from HM's own matrix) | Medium-large (multiple cells) | Low-medium |
| Level expansion | Medium-high | High per cell (doubled) | Low | Large (safety/quality-structure surface) | Medium |
| 10-mile alias closure | High (reuses everything) | Low (a decision, not new numbers, if approved) | Medium (tests the identity/authority-precision model) | Small | Low |
| Shared-component normalization | N/A (a refactor, not an expansion) | None | Medium (formalizes existing implicit structure) | Medium (touches existing policy classes) | Low if done with strict zero-delta tests |

## 48. Training-authority cost matrix

| Option | Horizon | Peak volume | Calibration | LR | Taper | Pace | Readiness | Quality-session structure |
|---|---|---|---|---|---|---|---|---|
| Third target (18K) | LOW (reuse mechanism, new preferred/max evidence) | MEDIUM (new evidence-gathering) | LOW (methodology reuse) | LOW (likely shared) | LOW (parent-family reuse) | LOW (generic) | LOW (generic) | N/A (4D only) |
| Frequency expansion | LOW | HIGH (new band per cell) | MEDIUM | MEDIUM | LOW | LOW | LOW | HIGH (5D/6D KEY2) |
| Level expansion | LOW | HIGH | HIGH | MEDIUM | LOW | LOW | LOW | HIGH (Advanced dual-hard) |
| 10-mile alias | LOW (decision only) | LOW | LOW | LOW | LOW | LOW | LOW | N/A |
| Shared-component normalization | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A |

## 49. Engineering-cost matrix

| Option | Policy work | Routing | Tests | Frontend | Public activation | Catalog impact | Schema impact |
|---|---|---|---|---|---|---|---|
| Third target (18K) | MEDIUM | SMALL (extends existing registries) | MEDIUM | SMALL (mirrors 15K's own addition) | SMALL (mirrors DIST-GEN.7) | NONE | NONE |
| Frequency expansion | MEDIUM-LARGE | MEDIUM | LARGE | SMALL | SMALL | NONE | NONE |
| Level expansion | LARGE | MEDIUM | LARGE | SMALL | SMALL | NONE | NONE |
| 10-mile alias | SMALL | SMALL | SMALL | NONE (no new admission) | NONE (decision only) | NONE | NONE |
| Shared-component normalization | MEDIUM (refactor) | MEDIUM (refactor) | MEDIUM (zero-delta proofs) | NONE | NONE | NONE | NONE |

## 50. Risk matrix

| Option | Invented-number risk | Boundary risk | Catalog-explosion risk | Policy-cross-contamination risk | Public-regression risk | Long-term scalability | Explanatory clarity |
|---|---|---|---|---|---|---|---|
| Third target (18K) | Low (same evidence-gathering discipline as 15K/16K) | Low | None | Low (registry pattern proven twice already) | Low | Good, tests scalability directly | High — directly answers §31's open question |
| Frequency expansion | Low-medium | None | None | Medium (more cells to keep isolated) | Low | Good but doubles authority surface | Medium |
| Level expansion | Medium (largest new-evidence need) | None | None | Medium | Medium | Good but largest surface | Medium |
| 10-mile alias | Medium if forced without evidence; low if left `INSUFFICIENT_EVIDENCE` | Low | None | Low | None (no new public surface if deferred) | High if deferred correctly | High — clarifies the precision model either way |
| Shared-component normalization | None (pure refactor) | N/A | N/A | Low if zero-delta-tested | Low if zero-delta-tested | High | Medium |

## 51. Public-registry scalability

`ApprovedPublicCells`: 2 entries today, exact-match list scan. **Classification: `SCALABLE_AS_IS`** for a third or even fourth entry (list-scan cost is trivial at this scale); **`SCALABLE_WITH_TYPED_KEY_GENERALIZATION`** recommended once entries reach roughly 5+, at which point a dictionary keyed by a proper `(TargetDistanceKm, ParentFamily, Level, RunsPerWeek)` value-type would be more appropriate than a list scan — not urgent now.

## 52. Dark-registry scalability

`ApprovedDarkCells`: identical shape and identical classification to §51.

## 53. Authority-registry scalability

Horizon/volume/LR dispatch (the ternary/pattern-match cascades in `PlanServices.cs` and sibling call sites): **`MANAGEABLE_BUT_NEEDS_GENERALIZATION`** (matches §5/§9's own classification) — a third target would add a third branch to each cascade, which remains readable but is the natural trigger point to convert these into a keyed lookup (a `Dictionary<double, ITargetDistanceAuthority>`-shaped resolution, or similar) rather than continuing to grow the cascade by hand at each new target.

## 54. Test-architecture scalability

Per DIST-GEN.6/7's own findings: `Dark15K...`/`Dark16K...TargetDistanceProjectionTests.cs` are already `[Theory]`/`[InlineData]`-parameterized in a way that scales to new data rows cleanly; the public activation test files (`DistGen3.../DistGen6.../DistGen7...PublicActivationTests.cs`) have so far been additive new files per phase rather than a single growing parameterized suite. Recommendation for a future third-target phase: continue the existing pattern (own dark test file, own public test file) for auditability, but consider a shared, data-driven "cross-target isolation matrix" test (mirroring DIST-GEN.7's own §25 proof) parameterized over the full set of approved targets, so that proof scales automatically as targets are added rather than needing a new bespoke isolation test per pair.

## 55. Hard-coded-target audit

Direct grep for `15.0`/`16.0` across `backend/RunningApp.Application/*.cs` found exactly the expected production files (`Dark16KPilotEligibilityPolicy.cs`, `GeneratePreviewRequest.cs`, `PlanServices.cs`, `CatalogPreviewGenerator.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `TargetDistance16KAwarePeakVolumeBandLoader.cs`, `VolumeSafetyPolicy.cs`, `TargetDistance16KHorizonPolicy.cs`, `CatalogPlanConfirmationService.cs`) plus DTO doc-comment mentions (`ProfileOverviewResponse.cs`, `HomeResponse.cs`, `PlanDetailsResponse.cs`, `GeneratePreviewResponse.cs`) and unrelated incidental matches in files with no target-distance relationship (`V1BeginnerThreeDayTaperLongRunSharePolicy.cs`, `V1BeginnerThreeDayVolumeEligibilityPolicy.cs`, `CoreEntryReadinessResolver.cs` — these contain unrelated numeric literals, not target-distance authority). Every relevant hit classifies as `CORRECT_EXACT_TARGET_AUTHORITY`, `CORRECT_PUBLIC_GATE`, or `CORRECT_DARK_GATE` per DIST-GEN.6/7's own already-completed classification passes (§6 of each report) — no new `BUG` or unclassified `SCALABILITY_DEBT` hit was found beyond what those two phases already disclosed (the dispatch-cascade pattern, §5/§9/§53).

## 56. Option A analysis — distance bands

Resolves: whether shared training authority can be governed across a distance range. New training authority: none (a band, if approved, would consume existing evidence, not invent new numbers). Architecture learning: would be high **if** evidence supported it, but per §28 it does not yet. External evidence need: would require a third point at minimum. Implementation cost: N/A, not actionable. Public risk: N/A. **Deferred** — `BANDING_NOT_READY` for every field (§28/§30).

## 57. Option B analysis — third exact target

Resolves: whether the 15K/16K convergence on peak volume/selected peak/calibration/horizon-preferred-max is real interior stability or coincidental adjacency (§31). New training authority: a genuinely bounded, medium-cost evidence-gathering exercise, following the exact precedent DIST-GEN.5 already validated. Architecture learning: high — directly informs the still-open banding question with real data rather than continued speculation. External evidence need: some (18K-specific plan-structure sources, not yet gathered). Implementation cost: medium, well-precedented. Public risk: low. **Selected — see §63.**

## 58. Option C analysis — 10-mile alias/precision

Resolves: whether exact-identity vs training-authority-precision separation should be formally exercised for a real pending case. New training authority: none, if the answer remains "insufficient evidence" (the likely honest outcome per §35). Architecture learning: moderate — clarifies a real, previously-deferred model question, but the answer is likely to remain a deferral rather than a resolution, yielding less net progress than Option B. External evidence need: low (mostly synthesis of already-gathered evidence). Implementation cost: small. Public risk: low. **Deferred** — lower information-gain than Option B, since the likely honest outcome is another `INSUFFICIENT_EVIDENCE`, not a resolution.

## 59. Option D analysis — frequency expansion

Resolves: whether target-distance and frequency truly compose orthogonally, in production, for a second real target (not just theoretically per DIST-GEN.4's own structural audit). New training authority: high (doubled cost, §42/§48). Architecture learning: low-medium — the orthogonality question was already substantially answered by DIST-GEN.4's own structural audit (horizon/topology/KEY2 all confirmed target-distance-agnostic); actually building a frequency cell would mostly re-confirm known theory at high training-authority cost, not resolve new uncertainty. **Deferred** — lower information-gain per unit of authority cost than Option B.

## 60. Option E analysis — level expansion

Same reasoning as §59, intensified: highest training-authority cost of any option (§48), lowest architectural novelty (the level-expansion pattern is already fully understood from HM's own matrix), and the largest safety-sensitive surface (Beginner/Advanced). **Deferred**, for the same information-gain-per-cost reasoning, more strongly.

## 61. Option F analysis — shared-component authority normalization

Resolves: whether the real, now-confirmed ownership structure (§7/§39) should be formalized in code rather than left implicit in repeated field values. New training authority: none (pure refactor). Architecture learning: moderate — valuable governance hygiene, but doesn't resolve any live open question the way Option B does; the current duplication is explicitly judged acceptable, not harmful, at n=2 (§40). **Deferred as a standalone phase, but recommended as an explicit methodology to fold into whichever phase implements Option B** (see §64) — normalizing now, before a third data point exists to prove which fields the abstraction should actually cover, risks guessing the abstraction's shape prematurely.

## 62. Final option comparison

| | Uncertainty resolved | New authority | Learning | Evidence need | Cost | Risk | Scalability | Verdict |
|---|---|---|---|---|---|---|---|---|
| A. Bands now | None — not ready | N/A | N/A | N/A | N/A | N/A | N/A | Not actionable |
| **B. Third target (18K)** | **Band-candidate stability (§31)** | **Medium, bounded, precedented** | **High** | **Some (18K-specific)** | **Medium** | **Low** | **Directly tests dispatch-layer scalability at n=3** | **Selected** |
| C. 10-mile alias | Identity/authority-precision model | Low/none likely | Moderate | Low | Small | Low | High if deferred | Deferred |
| D. Frequency expansion | Orthogonality (already largely known) | High (doubled) | Low-medium | Some | Medium-large | Low-medium | Good but costly | Deferred |
| E. Level expansion | Cross-level extrapolation (already largely known) | Highest | Low | High | Large | Medium | Good but costly | Deferred |
| F. Shared-component normalization | Governance hygiene | None | Moderate | None | Medium | Low | High | Deferred as standalone; folded into B |

## 63. Next-axis decision

**Option B: a third exact target, held at Intermediate/4D, structural parent HALF_MARATHON — specifically 18K.**

Applying the governing standard (§82) in order: (1) *strongest available evidence* — 18K sits inside the already-approved HALF_MARATHON structural-parent interval, requiring no boundary re-litigation (unlike 12K, §32); (2) *greatest unresolved architectural uncertainty* — directly, and uniquely among the options, tests whether the peak-volume/selected-peak/calibration/horizon-preferred-max convergence observed at 15K/16K is real interior stability or coincidental adjacency (§31), the single most important open question this phase surfaced; (3) *highest information gain* — a genuinely new data point at a meaningfully different interior position teaches more than doubling down on frequency/level cells whose orthogonality is already substantially understood (§59/§60); (4) *smallest unsupported-authority surface* — bounded to one target, one cell, reusing every proven mechanism; (5) *product value* — SOURCE_SILENCE across all options, a wash; (6) *long-term scalability* — a third real target is also the natural, honest point to stress-test whether the current registry/dispatch pattern (§51-53) truly scales, rather than speculating about it; (7) *zero-delta safety* — fully additive, dark-first, matching the now-proven three-phase template exactly; (8) *no invented numbers* — 18K would require the same real-evidence-gathering discipline DIST-GEN.1/5 already modeled twice successfully.

## 64. Exact next-phase handoff

**DIST-GEN.9 — 18K INTERMEDIATE 4D THIRD-TARGET AUTHORITY CLOSURE.** Scope: gather real, on-point external 18K training-plan evidence (narrow supplemental research, since this specific distance's evidence was not previously gathered); freeze the complete 18K numeric authority table using the same discipline as DIST-GEN.1/5 (structural floor from the reused catalog identity, independently-derived preferred/max, independently-derived peak-volume/selected-peak/calibration, evidence-driven absolute-peak-LR respecting the reaffirmed §17 invariant); **explicitly compare the resulting 18K values against both 15K/16K's own converged values and HM's own canonical values to directly answer whether the interior convergence continues or begins shifting toward HM** — this comparison is the primary deliverable, not merely a number table; **as part of this same phase, apply this report's own provenance-classification discipline (§7) to every field from the start**, so the shared-component-authority structure (§39/§61) is correctly reflected in how 18K's own authority is documented, without requiring a separate normalization phase; explicitly reassess whether the horizon/volume/LR dispatch cascade (§53) should be generalized into a keyed lookup as part of 18K's own dark implementation, given a third branch is the point this phase itself identified as the natural trigger. This phase must not begin any implementation itself (per §76 of this phase's own governing prompt) — DIST-GEN.9 would be the authority-closure phase, followed by its own dark-implementation and public-activation phases per the now-established three-phase template (§62 of the original governing prompt), which this report explicitly recommends adopting as standard practice for any future target (recorded as governance, not code, per that section's own instruction).

## 65. Existing-product zero-delta

Confirmed by construction — this phase touched no production, catalog, database, API, or frontend file (`git status --porcelain -- backend plan-catalog mobile` verified empty). 5K, TEN_K, HALF_MARATHON, public 15K, public 16K, unsupported Custom targets, Habit Custom, Experienced exclusion, catalog hashes, and every current numeric policy remain completely untouched — this entire phase is analysis of already-existing, already-verified source values.

## 66. Remaining blockers

None. Every required matrix/classification (four-point authority matrix, authority-provenance matrix, bandability table) is complete; value equality was rigorously separated from authority equality throughout (§7); no unsupported band boundary was invented (§28/§68 satisfied); the 10-mile question is precisely bounded, not silently resolved (§35); registry and dispatch-layer scalability were both assessed with concrete, actionable classifications (§51-53); frequency/level expansion were both explicitly reconsidered and honestly deferred with reasoning, not by default; the third-target need was explicitly assessed and answered (§31); exactly one next phase was selected and named (§63-64).

---

## Final classification

**POST_15K_16K_MULTI_TARGET_AUTHORITY_PATTERN_AND_NEXT_EXPANSION_CLOSED**

The central finding: almost nothing in the current 15K/16K matrix is actually "15-16K-specific shared authority." Fields that match either match far more broadly (Intermediate×4D or HALF_MARATHON-parent invariants, confirmed by agreement with — or informative contrast against — TEN_K) or match only through unconfirmed, adjacent-point convergence not yet strong enough to call a band. The one field that genuinely differs (absolute peak long run) does so for a real, mechanically-understood reason. Next axis selected: **a third exact target, 18K Intermediate 4D.** Next phase: **DIST-GEN.9 — 18K Intermediate 4D Third-Target Authority Closure.** DIST-GEN.9 itself is not begun by this phase.
