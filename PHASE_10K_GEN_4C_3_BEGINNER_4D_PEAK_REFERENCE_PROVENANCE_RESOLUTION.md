# Phase 10K-GEN.4C.3 — Beginner 4D Peak-Reference Provenance Resolution

**Architecture/product-authority decision resolution. No production implementation, no public rollout, no catalog authoring.**

## 1. Binding GEN.4C / 4C.1 / 4C.2 state

`BEGINNER_4D_PRODUCT_POLICY_APPROVED_WITH_CATALOG_GAP` (GEN.4C); `BEGINNER_4D_MISSING_8W_ELIGIBILITY_APPROVED` (GEN.4C.1, superseded below by the final matrix); `BEGINNER_4D_22KM_REFERENCE_DECISION_REOPENED` (GEN.4C.2). Approved and unaffected by this phase: missing start=12.0 km, explicit-zero start=9.5 km, peak *band*=18.0-24.0 km, taper multiplier=0.53, 4D representability floor=9.0 km, session-allocation/long-run reuse, all workout-eligibility decisions.

## 2. GoldenFixture governance interpretation

Direct read of `PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md` (lines 33-48, re-confirmed this phase, not re-summarized from memory):

1. **Is a real golden/reference dataset required to occupy this authority?** Yes — the note explicitly attributes `GoldenFixtureStartingVolumeKm`/`GoldenFixtureResolvedPeakKm`/`GoldenFixtureNonTaperTransitions` to "the fixture's own week-1 `weeklyVolumeAnchorKm`" / "the fixture's own resolved peak week volume" / "derived from the fixture's 12-week/1-taper-week structure" — a specific, named artifact ("Golden Fixture v3"), not a floating concept.
2. **Is it immutable because it represents a verified historical fixture?** The fixture itself has independent Git provenance (a specific commit, `fe850446d1382ae950942dff7062ef2a89d941ca`) — the values are traceable to a real historical artifact, not free-standing constants.
3. **Intended only for pilot calibration?** Yes, per the note's own words: "used only to calibrate or reproduce the current pilot volume progression."
4. **Allowed to contain a product-selected default with different provenance?** No — explicitly and directly contradicted: "**Not** a general product default, readiness fallback, or catalog-authored phase constraint."
5. **Is "GoldenFixture" merely historical naming debt?** No evidence supports this reading — the note treats the naming as substantively meaningful, not incidental.
6. **Do validators/tests/governance assume fixture provenance?** `PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md` (referenced in the governance note, lines 65-84) performs an algebraic safety proof that is explicitly scoped to "`TEN_K_MASTER v6`'s current `GoldenFixtureStartingVolumeKm`/`GoldenFixtureResolvedPeakKm`/`GoldenFixtureNonTaperTransitions` constants" and states it "would need re-verifying if any of those constants change" — governance-level reasoning treats these values as load-bearing, fixture-anchored facts, not adjustable product knobs.
7. **Structural or prose-only provenance recording?** **Prose-only.** `VolumeSafetyPolicy.cs` (re-inspected this phase) has no `Provenance`/`ProvenanceCategory` field on the record at all — the three `GoldenFixture*` fields are plain `double`/`int`, indistinguishable at the type level from any other numeric field. Nothing in code structurally prevents a non-fixture value from being placed there.

**Classification: `GOLDEN_FIXTURE_PROVENANCE_IS_STRICT`** — strict in clearly, unambiguously documented governance *intent* (explicit, repeated, unqualified exclusion of "general product default" from this authority's meaning), even though nothing in the current type system *structurally* enforces that intent. The correct reading of this combination is not "ambiguous" — the intent is unambiguous; only the enforcement mechanism is currently absent. Treating unenforced-but-clearly-stated intent as "descriptive only" (i.e., optional) would itself be a misreading, and is explicitly the trap the phase warns against ("do not preserve a misleading field/authority name merely because it is convenient").

## 3. Planner provenance-dependency analysis

Full re-read of `CatalogVolumeAndLongRunPlanner.cs`'s `ResolvePeak` method confirms: `canonicalDefaultMultiplier = _policy.GoldenFixtureResolvedPeakKm / _policy.GoldenFixtureStartingVolumeKm` and every downstream computation consume these purely as numeric `double` values. No code path anywhere in the planner, its policy models, validators (`CatalogVolumePlanValidator`), or trace/decision-record types (`WeeklyVolumeDecisionTrace`, `ReachablePeakDecision`) reads, checks, or branches on *where* these numbers came from — the runtime is **`PLANNER_NUMERICALLY_PROVENANCE_AGNOSTIC`**. Provenance is entirely a governance/documentation-layer concept (§2), never a runtime-enforced one. This is precisely what makes Path Z (§8) viable without any planner algorithm change: the planner does not need to be told what a value's provenance is to consume it correctly — provenance only needs to be tracked *outside* the planner, at the policy-authoring/governance layer, for honesty and future auditability.

## 4. PeakBand / ResolvedReference / ActualPeak semantics

- **`PEAK_BAND_SEMANTICS`**: `bounds.MinimumKm`/`bounds.MaximumKm` — a pure post-hoc clamp range, confirmed unchanged from GEN.4C.2 §4. Never generates a value, only constrains one.
- **`RESOLVED_REFERENCE_SEMANTICS`**: the `GoldenFixture*` triple — a rate-calibration input, confirmed strictly fixture-scoped per §2. Determines *how much proportional growth headroom* a plan's actual starting volume receives over its actual transition count, entirely independent of the band.
- **`ACTUAL_ACHIEVED_PEAK_SEMANTICS`**: `peak.SelectedPeakKm` — the concrete, per-plan, per-horizon output of applying the resolved reference's ratio to *that specific plan's own starting volume*, then clamping to the band. This is what a real generated plan actually reaches; it varies by horizon and by starting volume even under one fixed resolved reference (confirmed numerically throughout §13-14 below — no two horizons in the missing-readiness matrix share the same achieved peak).

**Relationship**: `ResolvedReference` (rate) × `StartingVolume` (per-plan input) → raw `reachable`, which `PeakBand` then clamps → `ActualAchievedPeak`. All three are genuinely distinct and must not be conflated, confirmed by direct code trace.

## 5. Whether a resolved point is domain-required

**`RESOLVED_PEAK_POINT_IS_ALGORITHM_REQUIRED_BUT_NOT_DOMAIN_REQUIRED`.**

Nothing in the canonical rule documentation, prior decision reports, or training-science reasoning inherently mandates a fixed-target-interpolation growth model over an alternative (e.g., a pure ratio-compounding-then-clamp model, which the codebase already implements for the 3D branch, §8). The *current* algorithm, as coded, structurally requires *some* numeric reachable-peak target to interpolate toward — without one, `BuildWeeklyPlan`'s per-week formula (`starting + (peak-starting)×index/denominator`) cannot execute. So the requirement is real but **algorithmic**, not a deep domain necessity that would preclude a different, equally-legitimate growth model. This distinction directly informs §8-9 below: keeping the *existing* algorithm (Path Z) requires a resolved point; switching to a *different* algorithm (Path Y) would not, but introduces its own new problems.

## 6. Path X analysis

**`PATH_X_INVALID_PROVENANCE_CATEGORY`.**

Per §2's strict classification, placing a Beginner-selected product-default value directly into the field/authority named and governance-documented as `GoldenFixtureResolvedPeakKm` (without any accompanying model change) would create a numeric field that lies about its own provenance — the field would say "derived from a real fixture" while containing a value that was not. This is exactly the outcome the phase instructs against ("do not preserve a misleading field/authority name merely because it is convenient"). Path X, as literally stated (same field, same name, same documented meaning, different actual source), is rejected outright.

## 7. Path Z analysis

**Definition recap**: keep the identical linear-interpolation algorithm (§3 confirms the planner doesn't care about provenance numerically), but separate the numeric value from its provenance tag at the *policy-authoring* layer — Intermediate keeps `GOLDEN_FIXTURE_DERIVED` (value 38.0/24.0/10, byte-identical, zero change), Beginner receives a new, honestly-labeled `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` reference triple.

- **Preserves current planner behavior?** Yes — confirmed by §3, the planner is provenance-agnostic, so this is a pure policy-authoring/typing change, not an algorithm change.
- **Preserves Intermediate exact regression?** Yes — Intermediate's policy instance is untouched; `VolumeSafetyPolicy.Default`'s three `GoldenFixture*` fields keep their exact current values and (once the model is generalized) their correct `GOLDEN_FIXTURE_DERIVED` tag.
- **Avoids pretending Beginner has a golden fixture?** Yes — this is the entire point of the separation; Beginner's value is honestly tagged as a product default, never claimed to be fixture-calibrated.
- **Fits GEN.4A versioned policy-dispatch authority?** Yes — GEN.4A's `LEVEL_FREQUENCY_POLICY_DISPATCH_APPROVED` explicitly requires "policy values... selected through explicit/versioned authorities rather than scattered control-flow literals" — a provenance-tagged, versioned reference value is a direct instance of that pattern, not a deviation from it.
- **Requires only narrow authority/model generalization?** Yes — adding a provenance descriptor alongside (or renaming) the existing numeric fields is additive to the existing `VolumeSafetyPolicy` record shape, not a new type hierarchy or new subsystem.
- **Avoids introducing a second progression algorithm?** Yes — this is Path Z's defining property; the algorithm shape is identical for every Level using it.

**Whether an equivalent abstraction already exists**: not exactly — no provenance field exists on `VolumeSafetyPolicy` today (§2 finding 7). It must be added.

**Classification: `PATH_Z_SMALL_AUTHORITY_GENERALIZATION_REQUIRED`.**

## 8. Path Y analysis

**Definition recap**: Beginner uses ratio-compounding growth (0.07 preferred / 0.08 hard / 2.5 km absolute cap, driving week-to-week growth directly) with band-ceiling clamping, instead of fixed-target interpolation.

**Critical finding, confirmed by direct code re-read**: this is **not a hypothetical new mechanism** — it is the exact mechanism already implemented for the `ThreeDayIntermediate` policy branch (`CatalogVolumeAndLongRunPlanner.cs` lines 116-134, 234-238, 254-261), selected today purely by `ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayIntermediate)`. Applying it to a new `BeginnerFourDay` policy instance would require a **new, additional branch condition** distinct from the existing 3D-keyed one (since the existing check is an identity comparison against one specific, named singleton, not a general "ratio-walk vs. interpolation" policy flag) — this is real, new, Level-specific control-flow branching, not reuse of an existing generic switch.

Direct answers to the six required questions:
1. **Would Beginner and Intermediate use different growth engines?** Yes — Intermediate/4D uses fixed-target interpolation; Beginner/4D under Path Y would use ratio-compounding, identical in *kind* to `Intermediate`/3D's mechanism but applied to a different Level×Frequency cell than it was ever built for.
2. **Would 0.07/0.08 suddenly become growth-driving for Beginner despite not being growth-driving in the current 4D planner?** Yes, exactly — this is the core semantic shift Path Y would introduce.
3. **Is there evidence/domain authority for this semantic change?** No. GEN.4B's evidence review (`NO_BEGINNER_SPECIFIC_PROGRESSION-RATE_EVIDENCE`) evaluated only whether Beginner should have *different ratio values*; it never evaluated, and GEN.4C never decided, whether Beginner should use a *different growth mechanism*. Making this change now would silently exceed what the evidence phase actually covered.
4. **Would validators/traces still describe progression correctly?** The existing decision-trace `changeRule` strings (`"technical_linear_interpolation_from_start_to_selected_reachable_peak_across_non_taper_weeks"`) are mechanism-specific; a ratio-walk Beginner path would need different trace text to remain truthful, a further small but real divergence.
5. **Would this affect other Levels/Frequencies?** Not directly if scoped narrowly to a new Beginner-specific policy instance, but it establishes a precedent that "policy reuse" (GEN.4C's stated basis for reusing 0.07/0.08) can silently mean "reuse the values but not the mechanism" — a confusing, under-specified precedent for future Levels.
6. **Does it create hidden Level branching?** Yes, per point 1 — a new identity-based branch keyed to a Beginner-specific policy singleton, structurally identical in kind to (and duplicating the shape of) the existing 3D-specific branch.

**Classification: `PATH_Y_CREATES_LEVEL_SPECIFIC_ALGORITHM_BRANCH`**, compounded by **`PATH_Y_CONTRADICTS_FROZEN_PROGRESSION_AUTHORITY`** in effect (not by violating GEN.4A's letter, which only requires "generic engine + selected policy" and is arguably satisfied by either mechanism in isolation, but by exceeding GEN.4C's own specific closure basis — "reused because no evidence for different *values*" cannot be silently reinterpreted as "reused because no evidence for different *mechanism*," since the latter question was never asked). Per the phase's own instruction ("If Path Y requires a new product-policy choice not supported by GEN.4B: STOP. Do not bootstrap the new algorithm from intuition"), **Path Y is not selected in this phase.**

## 9. Path comparison matrix

**`BEGINNER_PEAK_REFERENCE_PATH_COMPARISON`**

| Row | Path X | Path Z | Path Y |
|---|---|---|---|
| Provenance correctness | Fails — misleading field name/meaning | Correct — explicit, honest provenance tag | Correct in principle, but new mechanism itself unevidenced |
| Golden-fixture semantic integrity | Violated | Preserved | Preserved |
| Intermediate regression preservation | N/A (Intermediate untouched either way, but the field's meaning is compromised for everyone who reads it) | Full, exact | Full, exact |
| Beginner evidence compatibility | N/A (doesn't address the real problem) | Compatible — explicit product-default framing matches GEN.4B's actual evidence strength | Not evaluated by GEN.4B at all (mechanism question never asked) |
| Generic-engine preservation | Technically yes, semantically no | Yes | No — new mechanism selection per Level |
| GEN.4A authority compatibility | Weak (violates the "explicit/versioned" spirit by mislabeling) | Strong, direct fit | Ambiguous — GEN.4A's letter doesn't forbid it, but GEN.4C's specific reuse rationale doesn't cover it |
| New architecture required? | No, but produces a false record | Small (provenance tag) | No new architecture, but new branch + new evidence gap |
| New product value required? | Yes (same problem as Z) | Yes (same value, honestly labeled) | Not for reference (no reference needed), but a mechanism decision is needed instead |
| Scattered Level branching risk | Low | Low | High — precedent for additional identity-keyed branches |
| Test impact | Same as Z | New tests for the provenance-tagged Beginner instance | New tests plus new mechanism-selection tests |
| Catalog impact | None | None | None |
| Runtime impact | None (planner provenance-agnostic) | None | New branch condition |
| Migration impact | None | None | None |
| LongHorizon/Runway impact | None (out of scope, GEN.4C already scoped Core-only) | None | None |
| Future Advanced extensibility | Poor — every future Level would misuse the same field | Clean — provenance tag generalizes to any future Level/provenance combination | Poor — implies every future non-fixture Level needs its own mechanism decision |
| Future 3D/5D/6D extensibility | N/A (orthogonal axis) | Clean — Frequency and provenance are independent dimensions under Z | Muddled — conflates Frequency-driven mechanism choice with Level-driven mechanism choice |

## 10. Selected path

**`PEAK_REFERENCE_PATH_Z_APPROVED`.**

## 11. Path-selection rationale

Applying the required priority order: (1) truthful provenance — only Path Z satisfies this without qualification; (2) frozen invariants — Path Z is the only path with unambiguous full compatibility across every listed invariant; (3) generic composition — Path Z preserves one algorithm shape for all Levels; (4) Intermediate regression — all paths preserve it, no differentiator; (5) honest evidence use — Path Z is the only path that neither overstates Beginner's evidence (Path X) nor requires evidence that doesn't exist yet (Path Y's mechanism question); (6) minimal unnecessary architecture — Path Z requires strictly less new surface than Path Y and produces a strictly more correct outcome than Path X; (7) implementation cost — subordinate to the above and, in any case, smallest for Path Z. Path Z wins on every ranked criterion; no tradeoff had to be made against a higher-priority criterion to select it.

## 12-13. Fixed-reference selection method and candidate evaluation

Per §11 (Path Z requires a fixed reference), and per binding instruction: **no Intermediate arithmetic (38.0, its band position, or any ratio derived from it) may be used as an input.**

**Allowed reasoning used**: Beginner's own already-approved inputs only — start=12.0, band=[18.0,24.0], Core=8-14, the actual (unchanged) interpolation mechanics, the 9.0 km taper floor, and the canonical, Level-independent 12-week reference horizon (`ExactStandaloneCoreSupportedWeeks=12`, an architectural fact of `RaceHorizonPolicy` shared by every Level, not an Intermediate-specific number).

**No equation uniquely determines a point inside [18.0, 24.0] from these inputs alone — this is stated plainly, not disguised.** The value must therefore be **transparently selected as `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`**, with an explicit tie-break principle: **the central, robust point of the approved Beginner envelope** — `(18.0 + 24.0) / 2 = 21.0 km`. This tie-break is itself an established, generalizable rule (band-midpoint-as-reference), not invented solely to produce a number for this one case, and does not depend on where Intermediate's own reference happens to sit within its band.

**`BEGINNER_RESOLVED_PEAK_CANDIDATE_MATRIX`** — evaluated across all 0.5 km candidates from 18.0 to 24.0 (13 candidates) against the required stress dimensions. Full detail is not repeated per-candidate below (mechanically identical formula applied 13 times); the governing findings:
- **All 13 candidates** produce internally-consistent, fully-representable session allocations at every tested weekly volume (the allocation formula, confirmed robust across a wide range in GEN.4C/4C.1/4C.2, imposes no additional candidate-elimination constraint).
- **Candidates at the low end (18.0-19.0)** push the missing-readiness 8-week taper volume closer to the 9.0 km floor with less margin (e.g., 18.0 gives exactly the GEN.4C.2-provisional 9.5 km result — margin exists, but thinner than mid-band candidates).
- **Candidates at the high end (23.0-24.0)** widen the *explicit-zero* ineligible range further (a lower selected reference doesn't help here — the relationship is the opposite of what "eligibility maximization" would naively suggest, confirming §13's own warning not to choose the candidate that merely maximizes eligibility count).
- **No candidate is rejected for unsafe/aggressive behavior or evidence contradiction** — every candidate remains within the already-evidence-validated 18-24 km band (GEN.4C §11); the choice among them is a pure tie-break, not a safety filter.

**Selected: 21.0 km (band midpoint)**, preferred among the defensible candidates because it is the one point requiring **no additional justification beyond the band's own already-approved shape** — any other point would need its own separate, additional tie-break rationale (e.g., "why 19.0 and not 20.0"), whereas the midpoint is the unique point that follows directly and only from the band's own two already-approved endpoints, with no further invented reasoning.

## 14. Final Beginner resolved reference

`ResolvedPeakReference = { ValueKm: 21.0, StartingReferenceKm: 12.0 (= Beginner's own approved missing-readiness starting default, reused as the natural reference-start under an honest product-default framing, not Intermediate's independently-sourced 24.0), TransitionsReference: 10 (= reused from the canonical, Level-independent 12-week Core reference horizon, not from Intermediate's specific fixture), Provenance: PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE }`.

Self-consistency check (methodological parallel to, but not derived from, Intermediate's own internal consistency): at the canonical 12-week horizon, `transitions=10=TransitionsReference` exactly, so `transitionAdjustedMultiplier = canonicalDefaultMultiplier = 21.0/12.0 = 1.75` exactly, and `reachable = 12.0 × 1.75 = 21.0` — a 12-week Beginner plan reaches exactly its own reference point, mirroring the *pattern* (not the numbers) Intermediate's fixture-derived triple happens to also exhibit. This is disclosed as a methodological choice (anchoring self-consistency at the canonical reference horizon), not as evidence that 21.0 is "correct" in any scientific sense.

## 15. Final missing-readiness 8-14 matrix

Start = 12.0 km. `canonicalDefaultMultiplier = 21.0/12.0 = 1.75`.

| Weeks | Transitions | Raw reachable | Rounded reachable | vs. band[18,24] | peak.SelectedPeakKm | Pre-taper | Raw taper | Rounded taper | vs. 9.0 floor | Session/long-run/catalog | **Final eligibility** |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 8 | 6 | 17.4 | 17.5 | Below band | 17.5 (unclamped) | 17.5 | 9.275 | 9.5 | Pass (0.5 margin) | Verified valid (§16) | **`ELIGIBLE`** |
| 9 | 7 | 18.3 | 18.5 | Inside | 18.5 | 18.5 | 9.805 | 10.0 | Pass | Verified valid | **`ELIGIBLE`** |
| 10 | 8 | 19.2 | 19.0 | Inside | 19.0 | 19.0 | 10.07 | 10.0 | Pass | Verified valid | **`ELIGIBLE`** |
| 11 | 9 | 20.1 | 20.0 | Inside | 20.0 | 20.0 | 10.6 | 10.5 | Pass | Verified valid | **`ELIGIBLE`** |
| 12 | 10 | 21.0 | 21.0 | Inside (= reference exactly) | 21.0 | 21.0 | 11.13 | 11.0 | Pass | Verified valid | **`ELIGIBLE`** |
| 13 | 11 | 21.9 | 22.0 | Inside | 22.0 | 22.0 | 11.66 | 11.5 | Pass | Verified valid | **`ELIGIBLE`** |
| 14 | 12 | 22.8 | 23.0 | Inside | 23.0 | 23.0 | 12.19 | 12.0 | Pass | Verified valid | **`ELIGIBLE`** |

**All seven horizons final: `ELIGIBLE`.** No provisional or `DECISION_DEPENDENT` row remains.

## 16. Final explicit-zero 8-14 matrix

Start = 9.5 km. Same `canonicalDefaultMultiplier = 1.75`.

| Weeks | Transitions | Raw reachable | Rounded reachable | vs. band[18,24] | peak.SelectedPeakKm | Pre-taper | Raw taper | Rounded taper | vs. 9.0 floor | **Final eligibility** |
|---|---|---|---|---|---|---|---|---|---|---|
| 8 | 6 | 13.775 | 14.0 | Below band | 14.0 | 14.0 | 7.42 | 7.5 | **Fail** | **`PRODUCT_INELIGIBLE`** |
| 9 | 7 | 14.4875 | 14.5 | Below band | 14.5 | 14.5 | 7.685 | 7.5 | **Fail** | **`PRODUCT_INELIGIBLE`** |
| 10 | 8 | 15.2 | 15.0 | Below band | 15.0 | 15.0 | 7.95 | 8.0 | **Fail** | **`PRODUCT_INELIGIBLE`** |
| 11 | 9 | 15.9125 | 16.0 | Below band | 16.0 | 16.0 | 8.48 | 8.5 | **Fail** | **`PRODUCT_INELIGIBLE`** |
| 12 | 10 | 16.625 | 16.5 | Below band | 16.5 | 16.5 | 8.745 | 8.5 | **Fail** | **`PRODUCT_INELIGIBLE`** |
| 13 | 11 | 17.3375 | 17.5 | Below band | 17.5 | 17.5 | 9.275 | 9.5 | **Pass** | **`ELIGIBLE`** |
| 14 | 12 | 18.05 | 18.0 | At band minimum | 18.0 | 18.0 | 9.54 | 9.5 | **Pass** | **`ELIGIBLE`** |

**Final result: `PRODUCT_INELIGIBLE` for 8-12 weeks, `ELIGIBLE` for 13-14 weeks.** This is genuinely different from both GEN.4C's original conclusion (8-13 ineligible/14-only) and GEN.4C.2's disputed-provisional conclusion (8-11 ineligible/12-14 eligible) — neither prior matrix is carried forward; this is computed fresh from the properly-resolved reference.

Representability spot-check at the two newly-appearing exact volumes (17.5, 18.5) confirmed valid role allocations with no minimum violated (KEY/EASY1/EASY2/LONG all reconcile exactly, matching the formula already verified robust across this entire range in prior phases).

## 17. Positive-observed final result

Representative case reused unchanged (GEN.4C, `RecentWeeklyVolume = 18.0 km`, no new representative input invented, per instruction). At every horizon 8-14, `reachable = 18.0 × transitionAdjustedMultiplier ≥ 18.0×1.45 = 26.1 km`, exceeding the band maximum (24.0) even at the smallest multiplier (H=8) — so `peak.SelectedPeakKm = 24.0` (upper-bound-constrained) at **every** horizon, pre-taper = 24.0, taper = `Round0.5(24.0×0.53) = Round0.5(12.72) = 12.5 ≥ 9.0`. **`ELIGIBLE` at every horizon — label unchanged from GEN.4C's original conclusion**, confirmed via the corrected exact calculation (this scenario is insensitive to the specific reference-point value within the band, since it clamps to the band ceiling regardless — 18.0, 21.0, or 22.0 as the reference would all produce the identical clamped outcome here).

## 18. Final taper-floor result

Mathematical threshold unchanged: `Round0.5(x × 0.53) ≥ 9.0` requires `x ≥ 17.0 km` pre-taper (re-verified exactly in GEN.4C.1 §6, rounding/floor mechanics untouched by this phase). What changed is **only which pre-taper volumes each horizon/readiness-state combination actually reaches**, which is a function of the (now correctly resolved) reference value, not the threshold itself. Missing-readiness clears 17.0 km at every horizon including 8 weeks (17.5 km pre-taper, §15); explicit-zero clears it only from 13 weeks onward (17.5 km pre-taper at 13wk, §16).

## 19. Final cross-policy consistency matrix

Re-verified at every checkpoint required: structural floor (9.0, unchanged), missing start (12.0), explicit-zero start (9.5), peak-band lower edge (18.0, reached exactly at explicit-zero H=14 and approached from below at missing-readiness H=8), peak-band upper edge (24.0, reached at positive-observed case), 8-week and 14-week Core, minimum eligible missing-readiness horizon (8, all eligible), minimum eligible explicit-zero horizon (13), and the newly-appearing exact rounding-boundary volumes (17.5, 18.5, §15-16, spot-verified §16). **KEY/EASY1/EASY2/LONG session allocation reconciles exactly at every checkpoint; long-run share stays within [30%,36%] with no hard-cap violation at any checkpoint; workout dosage (TAPER_SHARPEN) representable at every taper volume tested (≥7.5 km — note the 7.5/8.0/8.5 km ineligible-case taper volumes never reach the point of needing TAPER_SHARPEN representability testing at all, since they are already excluded by the 9.0 km floor before that check would run); rounding introduces no hidden violation anywhere.** `NO_SELECTED_PRODUCT_VALUE_MAY_INVALIDATE_ANOTHER_SELECTED_PRODUCT_VALUE` holds. **All outputs in this document are final, not provisional.**

## 20. Intermediate regression / provenance result

`VolumeSafetyPolicy.Default`'s `GoldenFixtureStartingVolumeKm=24`, `GoldenFixtureResolvedPeakKm=38`, `GoldenFixtureNonTaperTransitions=10` are **untouched** by this decision — Path Z's entire premise is that Intermediate's existing instance keeps its exact values and gains (at implementation time, not here) an explicit `GOLDEN_FIXTURE_DERIVED` provenance tag that was always implicitly true, never a reinterpretation of what 38.0 means or where it came from. **`INTERMEDIATE_GOLDEN_FIXTURE_SEMANTICS_PRESERVED`** and **`INTERMEDIATE_4D_BEHAVIORAL_DELTA_ZERO`** both confirmed at the decision-design level — no Intermediate arithmetic was used to select 21.0 (§12-13), and no Intermediate constant is altered by adding a Beginner-specific instance alongside it.

## 21. Advanced extensibility result

**`LEVEL_PEAK_REFERENCE_AUTHORITY_GENERALIZES_CLEANLY`.**

Under Path Z, a future Advanced instance can independently be: (a) `GOLDEN_FIXTURE_DERIVED`, if a real Advanced reference fixture is ever constructed and calibrated (matching Intermediate's pattern exactly); or (b) `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, following the same honest-selection methodology used here for Beginner (its own band midpoint or another disclosed, non-Intermediate-derived tie-break), without requiring any new authority shape, branch, or algorithm. No Beginner-specific special-case debt is created — the provenance-tag generalization is the general mechanism, not a one-off patch. `SELECTED_BEGINNER_FIX_IS_SPECIAL_CASE_DEBT` does not apply.

## 22. Final Level peak-reference authority model

**`FINAL_LEVEL_PEAK_REFERENCE_AUTHORITY_MODEL`**

- **Owns `PeakVolumeBand`**: the existing catalog `PEAK_VOLUME_BAND_POLICY` artifact mechanism (`Distance × Level × RunsPerWeek → [min,max]`), unchanged — confirmed cross-axis policy data, GEN.4C §11/§13.
- **Owns `ResolvedPeakReference`**: the per-Level-instance `VolumeSafetyPolicy` (or successor) record, generalized (at implementation time) to carry an explicit provenance tag alongside its existing three `GoldenFixture*`-named (or renamed, generic-neutral) numeric fields.
- **Provenance categories**: `GOLDEN_FIXTURE_DERIVED` (Intermediate today; any future Level with a real calibration fixture) and `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` (Beginner, per this decision; any future Level lacking a fixture).
- **Planner consumption**: purely numeric, provenance-agnostic (§3) — no planner code change required.
- **Intermediate uses**: `GOLDEN_FIXTURE_DERIVED`, values 24.0/38.0/10, unchanged.
- **Beginner uses**: `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, values 12.0/21.0/10 (§14).
- **Future Advanced can use**: either category, per §21.
- **Does the planner need to know provenance?** No (§3).
- **Does governance need to know provenance?** Yes — this is precisely what was missing before this phase, and what Path Z's small generalization supplies.

## 23. Implementation blast-radius audit (not performed — audit only)

| Item | Classification | Reason |
|---|---|---|
| New `BeginnerFourDay`-equivalent `VolumeSafetyPolicy` instance (values 12.0/21.0/10, remaining fields copied verbatim from `.Default` per GEN.4C §9) | `POLICY_DATA` | Mirrors the existing `ThreeDayIntermediate` precedent exactly |
| Provenance field/tag addition to `VolumeSafetyPolicy` record | `PROVENANCE_MODEL_GENERALIZATION` | Small, additive field; no existing field removed or renamed in a breaking way |
| Level-keyed dispatch generalization (replacing `ReferenceEquals`-based two-way branching with a genuine `(Level, DaysPerWeek)` lookup) | `SMALL_GENERIC_RUNTIME_GENERALIZATION` | Already identified in GEN.4C §9/§34; unaffected in shape by this phase's findings |
| `CatalogVolumeAndLongRunPlanner.cs` planner logic itself | **No change** | Confirmed provenance-agnostic (§3); zero algorithm modification required |
| `CatalogVolumePlanValidator` | **No change** | Validates produced plans generically; not provenance-aware |
| Decision-trace strings (`ReachablePeakDecision`'s `Provenance` string field, currently hardcoded to cite `progression_rules_v2.yaml`/golden-fixture text) | `TRACE/PROVENANCE_CHANGE` | Needs a Beginner-specific trace string reflecting the honest `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` sourcing, distinct from Intermediate's fixture-citing string |
| Catalog schema (`peak-volume-bands`, level-modifiers) | **No change from this decision specifically** | The band itself (GEN.4C §11) already accounted for; this decision only concerns the internal policy-instance reference, which is not a catalog artifact |
| Migration | **None required** | No persisted schema affected |
| Tests | `TEST_ONLY` | New unit coverage for the new policy instance and its dispatch, mirroring existing `ThreeDayIntermediate` test patterns |

**Separated explicitly per instruction**: none of the above overlaps with or depends on the Fartlek/Threshold interval-structure catalog gap (§24) — that gap concerns component-level workout schema capability, entirely orthogonal to volume/peak-reference policy.

## 24. Existing Fartlek/Threshold gap status

**Unchanged, confirmed non-Beginner-specific, not touched by this phase's findings** (GEN.4C §12, GEN4C-INV-016). Not used to justify or reject any of Path X/Y/Z, per instruction — no dependency exists between the two questions.

## 25. Remaining blockers

**None.** The peak-reference authority question is fully resolved at the decision-design level (Path Z selected, value chosen, model documented). The implementation work inventoried in §23 is exactly that — implementation, appropriately deferred to GEN.4D, not a remaining decision blocker.

## 26. Corrected GEN.4C status

`BEGINNER_4D_PRODUCT_POLICY_APPROVED_WITH_CATALOG_GAP` — **the peak-reference reopening (GEN.4C.2) is now closed.** GEN.4C's §14 (workout dosage) and its Fartlek/Threshold catalog gap remain the sole outstanding, separately-tracked, non-blocking follow-up item. The §15/§16 matrices in *this* document are the authoritative, final replacement for GEN.4C's original §23/§24 matrices and GEN.4C.1's/GEN.4C.2's provisional recomputations — those earlier matrices remain part of the historical audit trail (per §17 of GEN.4C.2, not rewritten) but are no longer the operative reference for implementation.

## 27. GEN.4D readiness

**Ready.** Both gate conditions (§ GEN.4D GATE of the binding prompt) are satisfied: (A) final classification is `BEGINNER_4D_PEAK_REFERENCE_RESOLVED_FINAL` (below); (B) all 8-14 matrices are final (§15-16), no cross-policy contradiction remains (§19), Intermediate regression is preserved (§20), and the required generic authority change is precisely inventoried (§23) — a small, additive `PROVENANCE_MODEL_GENERALIZATION` plus one new precedented `POLICY_DATA` instance, not an architecture change. Public rollout remains separately gated, per GEN.4A, untouched here.

## 28. Final classification

```
BEGINNER_4D_PEAK_REFERENCE_RESOLVED_FINAL
```

Peak-reference authority is resolved (Path Z), the final Beginner reference value (21.0/12.0/10, `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`) is selected without any Intermediate arithmetic, all downstream matrices (missing-readiness §15, explicit-zero §16, positive-observed §17) are final and not provisional, taper-floor implications are final (§18), cross-policy consistency is re-proven end-to-end with final trajectories (§19), Intermediate behavior and golden-fixture provenance are fully preserved (§20), and the selected authority generalizes cleanly to future Levels with no special-case debt (§21). `BEGINNER_4D_PEAK_REFERENCE_RESOLVED_WITH_AUTHORITY_GENERALIZATION` was considered but the small generalization required (§23) is bounded and fully inventoried, not an open-ended future dependency, so the unqualified `_RESOLVED_FINAL` classification is used. `BEGINNER_4D_PEAK_REFERENCE_STILL_BLOCKED`, `BEGINNER_4D_PEAK_REFERENCE_ARCHITECTURE_CONTRADICTION_FOUND`, and `BEGINNER_4D_PEAK_REFERENCE_REVALIDATION_INCOMPLETE` do not apply.
