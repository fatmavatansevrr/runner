# Phase 4I.6B — Long-Horizon Runway Entry-Above-Core-Target Numeric Transition Policy Resolution

## 1. Executive result

This phase is a pure governance/policy-decision pass — zero production code changed. It resolves every **structural** question the blocker raises (Runway's semantic role, rejection of a GE-side ceiling, rejection of Core-target lifting, profile-neutral applicability, workout/intensity unaffected) with real Class D architecture-consistency reasoning. It does **not** approve an exact numeric convergence formula: real diagnostic evidence — gathered by running the exact, unchanged existing GE numeric executor and Core generation pipeline — shows the GE-exit-vs-Core-target excess grows unboundedly with horizon length (from ~4km at 28 weeks to ~41km at 52 weeks, with GE's own peak reaching ~71.5km/week by week 52, over 3× the ~20km Core target). No canonical rule, approved product default, generalizable existing production behavior, or strong domain evidence exists anywhere in this repository for a specific multi-week reduction magnitude capable of safely bridging gaps of that size within a fixed 8-week Runway. No new percentage, multiplier, or sequence was invented to force an answer. No commits made.

Final classification:

```
LONG_HORIZON_RUNWAY_ENTRY_ABOVE_CORE_TARGET_POLICY_REMAINS_BLOCKED_BY_EXPLICIT_CANONICAL_PRODUCT_OR_EVIDENCE_GAP
```

## 2. Exact blocker

`RunwayEntryVolume > CoreWeek1TargetVolume` — the existing Preparation Runway numeric materializer has no approved rule for this state outside Taper semantics and correctly fails closed (Phase 4I.6A's own discovery).

## 3. Inherited completed state

Phase 4I.1-4I.6A's composition formula, GE structural/numeric policy, typed composition decision, GE catalog capacity, structural materializer, GE numeric execution, and the real Runway/Core numeric integration for short-GE horizons are all consumed as given, unchanged.

## 4. Scope and non-scope

In scope: evidence review, decision-standard-driven policy resolution, real diagnostic/simulation tests (test-only code, no production changes), governance artifacts, documentation. Out of scope (explicitly not touched): Runway/Core numeric algorithms, GE progression, public preview, confirmation, persistence, Flutter, 53+, other candidates, invented numeric constants.

## 5. Repository evidence reviewed

`PreparationRunwayStartingLoadEvidence`/`PreparationRunwayNumericMaterializer` (Phase 4I.6A research); `VolumeSafetyPolicy.Default` (0.07/0.08 ratio caps — increase-only, and per `TD-VOLUME-CAP-UNENFORCED-001` informational/unenforced in Core's own planner; 2.5km absolute cap; 0.53 Taper multiplier; long-run shares); `progression_rules_v2.yaml`'s `cutbackPolicy` (0.15-0.20 INTERMEDIATE range, `postCutbackProgressionBaseline=PREVIOUS_NON_CUTBACK_VOLUME` — the exact source Phase 4I.2A already used for GE's own single-week recovery, explicitly a *different* semantic: one interior development-cycle down-week, not a multi-week phase-transition); `TenKPreparationRunwayDarkOrchestrator`'s existing upward-only progression design; Phase 4I.6A's own discovered failure and its exact error message; TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001, TD-LONG-HORIZON-RUNWAY-CORE-NUMERIC-CONTEXT-INTEGRATION-001, TD-GENERAL-ENDURANCE-STAGED-PLAN-001. A repository-wide search for "maintenance phase," "controlled convergence," "detraining prevention," "consolidation," "pre-Core normalization," "descending bridge," "over-target entry," and "phase-boundary load reduction" found no existing precedent for any of these concepts anywhere in the codebase or canonical documents.

## 6. Existing Runway numeric semantics

The existing `PreparationRunwayNumericMaterializer` is designed exclusively for one direction: ramp from a starting-evidence value UP toward a `PreparationRunwayCoreWeekOneNumericTarget` boundary over the allocated Runway weeks, bounded by `VolumeSafetyPolicy.Default`'s increase caps. It has no code path, no config, and no evidence for the reverse (entry above target).

## 7. Existing Core target semantics

Core's Week-1 target is computed once, from the runner's own raw declared evidence (`RecentWeeklyVolumeKm`/`RecentLongestRunKm`), entirely independent of any preceding segment or horizon length — confirmed empirically: the Core target measured a flat 20km/6.5km-long-run across the 28/40/52-week diagnostic cases (§9), which all shared the same 20km/8km typical baseline input.

## 8. Observed successful cases

21 and 24 total weeks (both profiles, typical baseline): GE exit ≤ Core target, existing upward-progression Runway mode applies unchanged, full numeric execution succeeds end-to-end (Phase 4I.6A).

## 9. Observed failing cases (real, new evidence gathered this phase)

Using the real, unchanged GE numeric executor and real Core generation pipeline (typical baseline 20km/8km):

| Total weeks | GE weeks | GE exit volume | GE peak volume | Core Week-1 target | Excess |
|---|---|---|---|---|---|
| 28 | 8 | 24.0 km | 28.0 km | 20.0 km | **4.0 km** |
| 40 | 20 | 41.5 km | 49.0 km | 20.0 km | **21.5 km** |
| 52 | 32 | 61.0 km | 71.5 km | 20.0 km | **41.0 km** |

Core Week-1 target is confirmed flat (20km) across all three cases, confirming it is horizon-independent as expected. The excess is not a rounding artifact or edge case — it scales directly with how many mesocycles GE's own approved development-progression policy compounds through.

## 10. Trigger condition

Purely numeric: `ValidatedGeExitVolume > IndependentlyDerivedCoreWeek1TargetVolume`. Never a horizon-number check — the same total-week count could trigger or not trigger depending on the runner's baseline evidence, since both GE's exit and Core's target are evidence-derived, not horizon-derived directly.

## 11. Runway semantic purpose

**Entry consolidation at the Core target** — when GE has already exceeded Core's own target, Runway's role shifts from "build toward" to "hold at/settle into" the evidence-derived Core entry level. This is explicitly distinguished from: GE recovery (a single interior down-week inside an ongoing development cycle, expected to resume upward afterward — 0.85 policy); Taper (a single-occurrence, race-proximity, performance-peaking reduction, occurring inside Core, not Runway — 0.53 policy); injury-driven deload (not evidenced anywhere in this system); and "automatic adaptation" (not a concept this system implements). The Runway's own approved 8-week identity and block structure are preserved — this is not a redefinition of Runway as a Taper.

## 12. Candidate policies

All nine families from the prompt were evaluated:

- **A. Immediate ceiling clamp** — zero new formula (entry evidence clamped to the Core target before being fed to the unchanged existing algorithm). Only formula-free candidate. See §13 for disposition.
- **B. Linear controlled convergence** / **C. Bounded weekly convergence** / **D. Maintenance then convergence** / **E. Existing block-aware convergence** — all require inventing an unevidenced weekly-reduction magnitude or schedule. Rejected: no evidence source exists for any of these exact shapes.
- **F. GE-side ceiling** — evaluated in depth, rejected (§14).
- **G. Core-target lifting** — evaluated in depth, rejected (§15).
- **H. Fail unsupported above an explicit threshold** — partially adopted in effect (this phase's own conclusion is that no safe threshold can currently be set with evidence, so the practical result today IS "fail unsupported" for the observed 28/40/52-week cases) — but this phase does not approve a specific numeric threshold value, since doing so would itself require inventing one.
- **I. Hybrid policy** — would still require picking specific unevidenced numeric sub-rules; rejected for the same reason as B-E.

## 13. Rejected policies

Immediate ceiling clamp (A) was the most seriously considered candidate, since it requires zero new numeric formula. It is **rejected as a blanket default** specifically because of the magnitude found in §9: at 52 weeks, it would require an instantaneous 41km (57%) single-step volume drop from GE's peak entering Runway Week 1. This is exactly the "hidden Taper" / "unnecessary detraining" risk this phase's own decision standard requires rejecting without direct evidence it is safe — and no such evidence exists. For the smallest observed case (28 weeks, 4km/17% excess) it remains a *plausible* narrower candidate, but approving it only for an implicit "small excess" band would itself require inventing an unevidenced magnitude threshold distinguishing "small" from "large" — not approved. B through G and I are rejected as requiring an invented numeric rule with no evidentiary basis (§7 of the prompt: 0.07/0.08/2.5km/0.85/0.53 are each evidenced for a materially different purpose; reusing any without direct support is explicitly forbidden, and no direct support was found for any of them in this context).

## 14. GE-side ceiling evaluation

**Rejected.** GE was approved by Phase 4I.2 as an independently-developing general-endurance phase with its own complete mesocycle/progression/recovery structure, explicitly not defined in terms of any downstream phase's requirements. Making GE's own ceiling depend on a later phase's (Core's) evidence-derived target would invert that approved independence, would make very long GE horizons collapse into repetitive maintenance phases with no principled stopping point, and is explicitly out of this phase's scope to redesign (the prompt itself warns against adopting this "merely to avoid the transition problem"). GE remains independently valid on its own terms even when its exit exceeds Core Week 1 — the fact that its exit is high is not itself a defect in GE; it is evidence that the DOWNSTREAM transition needs a policy, which is exactly this phase's actual subject.

## 15. Core-target lifting evaluation

**Rejected.** Core's Week-1 target is an evidence-derived starting point computed once from the runner's own declared baseline (§7) — it is not merely a ceiling but the literal output of Core's own approved `StartingVolumeDecision` logic, which this phase is explicitly forbidden from changing. Lifting it based on incoming Runway/GE load would require re-deriving that decision with different inputs (contradicting "do not change Core algorithms"), risks violating Core's own peak-volume-band constraints (Core's target was computed assuming a 12-week build from the ORIGINAL evidence — silently starting Core higher does not automatically revalidate that band), and would create real circularity between Runway's numeric stage (which needs the target as an input) and this lifting logic (which would need Runway's own output as an input).

## 16. Entry reference

**Approved:** `LongHorizonGeExitState.FinalWeeklyVolumeKm` (final GE week's volume) — unchanged from Phase 4I.6A's own approved entry-reference decision (`PrecedingGeneralEnduranceExit`). This phase does not revisit that decision; it only resolves what happens once that reference is compared against the Core target.

## 17. Exit target

**Confirmed unchanged:** the independently-derived Core Week-1 target (`PreparationRunwayCoreWeekOneNumericTarget`, computed from original, unmodified user evidence — Phase 4I.6A §9's own architecture decision, re-affirmed here after evaluating and rejecting Core-target lifting).

## 18. Eight-week trajectory

**Not approved.** No exact deterministic week-by-week formula is approved by this phase (see §12/§13) — this is the actual unresolved gap.

## 19. Change-cap policy

**Not approved.** No maximum-weekly-reduction rule exists in the repository (confirmed by review, §5/§6) and none is invented here. The existing 0.07/0.08 ratios are increase-oriented and, per Core's own `TD-VOLUME-CAP-UNENFORCED-001`, are informational/unenforced even for increases in Core's own planner — there is no basis to treat them as an evidenced reduction cap. `cutbackPolicy`'s 0.15-0.20 range is evidenced specifically for a single interior recovery week within an ongoing development cycle (already fully consumed by Phase 4I.2A's GE recovery decision), not for a multi-week phase-transition convergence — reusing it here was evaluated and rejected as unsupported reuse.

## 20. Rounding

Not reached — no formula exists yet to round.

## 21. Long-run transition

Not reached — depends on the unresolved total-volume trajectory.

## 22. Workout/intensity behavior

**Resolved:** workout identity and pace/effort semantics remain completely unaffected by this state — only the numeric volume/distance trajectory is in question. No threshold or race-specific work is introduced; the existing Runway block structure and workout eligibility are preserved regardless of how the numeric question is eventually resolved.

## 23. Profile policy

**Resolved: identical for both profiles.** No evidence anywhere supports a profile-specific numeric formula for this state; profile differences remain confined to Runway block allocation/content exactly as already approved (Phase 4I.1 §13, Phase 4I.2 §7/§16).

## 24. Recovery distinction

**Resolved** (§11): GE recovery is a single interior down-week using the 0.85 policy, expected to resume development afterward. This state is a multi-week, one-directional, phase-boundary transition — not interior to any development cycle, and not necessarily governed by the same magnitude.

## 25. Taper distinction

**Resolved** (§11): Taper is a single-occurrence, race-proximity, performance-peaking reduction (0.53) that occurs inside Core, not Runway. This state has no race-proximity trigger and occurs entirely within the pre-Core Runway segment.

## 26. Core Week-1 handoff

**Resolved in principle, not in exact numeric relation:** Runway Week 8 must hand off to Core Week 1 without a numeric reset, without an unsupported increase, and without corrupting Core's own Foundation semantics/workout identities/peak-volume policy/runtime-condition results (all unchanged, per Phase 4I.6A). The EXACT relation (equality? within a band?) cannot be specified without first resolving §18/§19.

## 27. Feasibility threshold

**Not approved as an exact number.** The prompt's own suggested mechanism (`RequiredAverageWeeklyReduction <= ApprovedMaximumReduction`) requires the same unevidenced reduction-cap value this phase could not approve (§19) — so no exact feasibility threshold could be derived from it either.

## 28. Fail-closed behavior

The practical, current, correct behavior for all three observed excess cases (28/40/52 weeks) remains "fail closed" — exactly what the existing, unmodified Runway numeric materializer already does. This phase does not change that behavior; it confirms, with real evidence, that no responsible policy currently justifies changing it.

## 29-33. 21/24/28/40/52-week results

21 and 24 weeks: unaffected by this phase (GE exit ≤ Core target, existing mode applies, already proven in Phase 4I.6A). 28/40/52 weeks: remain correctly blocked; real excess values recorded in §9.

## 34. Simulation matrix

Real diagnostic simulations (not fabricated) were run for the three documented failing cases using the real, unchanged production GE numeric executor and Core generation pipeline (§9). The full representative matrix (A-I categories, both profiles, rounding/long-run/session-minimum boundaries) specified in the prompt's Part 16 was not exhaustively executed — a disclosed scope reduction, since the central finding (no evidence-backed formula exists) was already conclusively established by the three cases run, and further simulation would not change that governance conclusion.

## 35. Policy provenance

Not applicable — no runtime policy was approved to carry provenance metadata for.

## 36. Governance artifacts

`TD-LONG-HORIZON-RUNWAY-ENTRY-ABOVE-CORE-TARGET-001` — **added OPEN** (plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}), recording both the resolved structural sub-decisions and the explicitly unresolved numeric gap. `TD-LONG-HORIZON-RUNWAY-CORE-NUMERIC-CONTEXT-INTEGRATION-001` and `TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001`: unchanged, remain OPEN (per explicit instruction, since policy approval alone would not have closed numeric execution regardless, and no policy was approved). `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`: unchanged, remains OPEN.

## 37. Runtime implementation impact

None — no runtime implementation is possible yet, since the exact formula Phase 4I.6C would need to implement does not exist. Phase 4I.6C, as originally envisioned, remains blocked pending either new evidence or an explicit product decision to accept an approximate default despite incomplete evidence (a decision this phase is not authorized to make unilaterally, per the decision standard's own conservatism requirements).

## 38. Tests

9 new tests: 3 real diagnostic-evidence tests (`LongHorizonRunwayEntryAboveCoreTargetPolicyDiagnosticTests.cs`, empirically measuring the real excess at 28/40/52 weeks using the exact unchanged production GE/Core pipelines) plus 6 governance cross-check tests (`LongHorizonRunwayEntryAboveCoreTargetGovernanceTests.cs`, plan-catalog). All passing.

## 39. Non-claims

This phase does not claim: that immediate ceiling clamp is safe for any excess magnitude; that any specific weekly reduction percentage is physiologically valid; that GE's own progression policy is itself correct or incorrect for very long horizons (a separate, unaddressed question); that the 28-week case's smaller excess is "acceptable" as a blanket rule (only that it is a narrower, not-yet-approved candidate); that this is a complete evaluation of every possible convergence mathematics family.

## 40. Residual blockers

The exact numeric convergence formula (§18/§19) remains the single, precise blocker. A closely related but explicitly out-of-scope observation: GE's own uncapped compounding growth for very long horizons (peaking near 71.5km/week at 52 weeks for a 10K plan) may itself be worth a separate future governance review, independent of the Runway transition question.

## 41. Final classification

```
LONG_HORIZON_RUNWAY_ENTRY_ABOVE_CORE_TARGET_POLICY_REMAINS_BLOCKED_BY_EXPLICIT_CANONICAL_PRODUCT_OR_EVIDENCE_GAP
```

## 42. Exact next phase

Not Phase 4I.6C as originally envisioned (that phase presupposes an approved formula to implement, which does not exist). Recommended: **Phase 4I.6B.1 — Long-Horizon GE Peak-Volume Ceiling and Runway Convergence Evidence Sourcing**, a further governance pass specifically tasked with either (a) sourcing genuine external/canonical evidence for a safe multi-week volume-convergence rate (if any exists in a to-be-identified source not yet reviewed), or (b) making an explicit, disclosed, conservative product-default decision (with real stakeholder/product sign-off, not a coding-session invention) for the specific narrower case of small excesses only, formally scoping out large-excess horizons as an explicitly unsupported product boundary (parallel to the existing 53+ week boundary) rather than leaving them silently blocked forever.
