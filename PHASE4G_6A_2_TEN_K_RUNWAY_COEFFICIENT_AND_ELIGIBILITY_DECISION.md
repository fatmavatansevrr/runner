# Phase 4G.6A.2 — 10K Preparation Runway Block Semantics, Eligibility, Target-Behavior Matrix, Capacity and Coefficient Decision Resolution

Scope: `TEN_K__4D__INTERMEDIATE v10` only. This is a documentation, registry, and local-analysis pass. No runtime allocator, workout binding, catalog change, schema change, persistence change, or public HTTP behavior change was made. `PreparationRunwayContracts`/`PreparationRunwayValidators` remain entirely dark and unwired, exactly as before this phase.

## 1. Purpose and Scope

This phase resolves, for the 10K pilot only, the four Preparation Runway block semantics (`CONSISTENCY`, `GENERAL_ENDURANCE`, `AEROBIC_STRENGTH`, `PRE_SPECIFIC_TRANSITION`), their eligibility-signal mapping, a two-profile runner model, target-behavior matrices for runway weeks 3-8, min/max capacity per block, a coefficient-family allocator simulation (local/test-only), final priority/canonical-order metadata, and a catalog-capacity audit — culminating in exactly one binary outcome: policy **APPROVED** or **BLOCKED**. Per the task's own instruction, a partial approval is not permitted; either every retained block's profile is resolved and catalog-supported, or the overall result is BLOCKED with the exact blocker(s) named.

## 2. Source Document Status

The referenced external source, `Appsel_4G_6A_2_Kaynak_Kullanim_Haritasi_ve_Karar_Dayanaklari.docx`, does **not exist** anywhere in this repository. A repository-wide search (file-name glob and content grep for "Kaynak Kullanim" / "Appsel_4G_6A_2") found zero matches. Since the task's own prompt already states the document's key conclusions directly — "Evidence-supported direction: General Endurance should remain dominant; Consistency should be conditional; Aerobic Strength requires semantic and readiness qualification; a short transition bridge is reasonable. Not externally proven: exact allocator weights; exact second-week thresholds; exact min/max capacities; Appsel catalog executability." — this phase proceeds using that summary as the evidence basis, per the task's own instruction to independently verify all repository claims from source code rather than trust the missing document's un-seen detail. This absence is recorded here as a finding, not silently worked around.

## 3. Evidence Classification Vocabulary Used

Per the task's mandated vocabulary: `EVIDENCE_INFORMED`, `PRODUCT_DEFAULT`, `PROVISIONAL_PRODUCT_DEFAULT`, `REPOSITORY_SUPPORTED`, `PARTIALLY_SUPPORTED`, `UNSUPPORTED`, `DESIGN_INTENT_NOT_YET_PROVEN`. No exact numeric weight, threshold, or capacity is labeled `EVIDENCE_BACKED` anywhere in this document — none has independent (e.g. peer-reviewed or coaching-literature-sourced) numeric evidence; where a value is repository-derived (an existing resolver threshold, an existing schema constraint), it is labeled `REPOSITORY_SUPPORTED`.

## 4. Block Semantics Resolution

- **CONSISTENCY**: a runner without sufficiently established recent running habit uses the runway to (re)build consistent, low-risk aerobic frequency before Core begins. Semantics: `EVIDENCE_INFORMED` (matches the source summary's "Consistency should be conditional" — conditional on the runner's own `CORE_ENTRY_READINESS_IN` signal, not always present).
- **GENERAL_ENDURANCE**: broad, non-specific aerobic-volume development — the dominant, near-always-present block across both profiles. Semantics: `EVIDENCE_INFORMED` (matches "General Endurance should remain dominant").
- **AEROBIC_STRENGTH**: resolved this phase — see section 5.
- **PRE_SPECIFIC_TRANSITION**: a short, fixed 1-week bridge immediately before Core begins, easing the runner into Foundation-phase-specific loading. Semantics: `EVIDENCE_INFORMED` (matches "a short transition bridge is reasonable"); fixed at exactly 1 week in both profiles (not expandable) since the source summary offers no evidence for a longer bridge and a longer bridge would encroach on the other blocks' already-uncertain capacity.

No fifth block was added; none of the four was renamed.

## 5. AEROBIC_STRENGTH Semantic Resolution

Two candidate interpretations were evaluated:

- **(A) Gym-based resistance/plyometric strength training.**
- **(B) Running-based controlled aerobic-power/economy stimulus** (hills, strides, controlled fartlek, short aerobic intervals).

**Resolution: (B).** Interpretation (A) is rejected. An exhaustive grep of all 18 workout-definition documents in `plan-catalog/catalog/workouts/` for `hill|stride|plyometric|resistance|gym|strength|core|economy|power` (case-insensitive) found **zero matches** anywhere in the catalog. No workout-definition schema field, no `objective`/`intent`/`tags`/`role` extension point exists to carry gym-based strength content even if authored (`plan-catalog/schemas/workout-definition.schema.json` has closed enums for `family` — `EASY, LONG_RUN, QUALITY, RACE` — and `eligiblePhases` — `FOUNDATION, BUILD, RACE_SPECIFIC, TAPER`). Approving a week-consuming runway block for content that does not exist and has no schema path to exist, purely by citing general strength-training literature, is exactly the unsupported inference this phase is required to reject (per the task's own explicit warning).

Interpretation (B) is the defensible direction per the source summary's own qualification language ("requires semantic and readiness qualification"), and is directionally supported by the one piece of running-based intensity-variation content that does exist: FARTLEK's `SURGE_AND_FLOAT` pattern (`family: QUALITY`). However — **even under (B), no catalog-executable workout exists today**: FARTLEK is restricted to `eligiblePhases: [BUILD]` only by the closed schema enum (confirmed directly in `ten-k-master.v6.json`: `BUILD.eligibleWorkoutFamilies` includes `QUALITY`, but `FOUNDATION.eligibleWorkoutFamilies` is `["EASY","LONG_RUN"]` only, and no phase value exists for a Preparation-Runway-eligible context at all). No hill-repeat, stride, or short-aerobic-interval workout definition exists anywhere in the catalog.

**Conclusion: AEROBIC_STRENGTH is catalog-`UNSUPPORTED` under either semantic interpretation, independent of which interpretation is chosen.** This is recorded as `TD-AEROBIC-STRENGTH-SEMANTICS-001` (see section 19).

## 6. Eligibility Signal Mapping

The Preparation Runway only ever applies ahead of a Race-goal Core plan. `CoreEntryReadinessResolver.Resolve` (`backend/RunningApp.Application/RuntimeCatalog/Resolvers/CoreEntryReadinessResolver.cs`) is reused verbatim, with its already owner-approved V1 thresholds (`TD-CORE-READINESS-001` resolutionNote, EV-008) — no new threshold is invented:

- `READY`: `RecentWeeklyVolumeKm >= 15` AND `RecentLongestRunKm >= 6`.
- `NOT_READY`: `RecentWeeklyVolumeKm < 8` OR `RecentLongestRunKm < 4` (checked first).
- `CAUTION`: everything else with both fields present, or exactly one field present ("partial core evidence").
- Both fields missing, `GoalType == Race`: `NOT_READY` (the resolver's own existing rule).

A direct read of `ResolveBothMissing` confirms that for `GoalType == Race` specifically, the resolver **never** returns `NotEvaluated` — the both-missing branch is unconditionally `Evaluated/NOT_READY` for Race. Since the Preparation Runway exists only in a Race context, `CORE_ENTRY_READINESS_IN` is therefore **always `Evaluated`** (never `NotEvaluated`/`PrescriptionInputState.NotProvided`-equivalent) at the point a runway profile decision would be made. This eliminates the need for a separate missing-evidence branch in the profile-selection logic itself (see section 7).

Classification: `REPOSITORY_SUPPORTED` (real, already-implemented, already owner-approved resolver and enum; reused unmodified).

`GoalFeasibilityResolver`'s `TIME_ADEQUACY_IN`/`PACE_SOURCE_IN`/`GOAL_FEASIBILITY_IN` outputs were reviewed and found **not relevant** to runway-profile selection: they classify pace/target-time evidence, not running-consistency/core-readiness evidence, and the task's own profile model (`CONSISTENCY_NEEDED` vs `CORE_ENTRY_READY`) maps directly and only onto `CORE_ENTRY_READINESS_IN`'s three-value output space.

## 7. Missing-Evidence Policy

No separate missing-evidence policy is required beyond what `CoreEntryReadinessResolver` already enforces (section 6): in a Race context, missing evidence is never silently treated as `READY`, never converted to a numeric zero, and never used to downgrade `RunningBackground` — it deterministically resolves to `NOT_READY`, which this phase's profile mapping (section 8) routes to the conservative `CONSISTENCY_NEEDED` profile. This satisfies the task's explicit constraints: no arbitrary km threshold was invented, no Missing-to-zero conversion occurs, and `RunningBackground` (`Beginner/Intermediate/Advanced/Experienced`) is never read or altered by this mapping.

## 8. Two-Profile Model and Mutual-Exclusivity Determination

- **CORE_ENTRY_READY**: `CORE_ENTRY_READINESS_IN == READY`.
- **CONSISTENCY_NEEDED**: `CORE_ENTRY_READINESS_IN ∈ { CAUTION, NOT_READY }`.

This is a complete, mutually exclusive partition of `CoreEntryReadinessResolver`'s entire three-value output space, combined with section 6's proof that a fourth ("no evidence") case cannot occur in this context. It is the smallest safe V1 policy the task allows: two profiles, one already-approved typed signal, no new resolver, no new registry value, no new eligibility heuristic.

**Determination: mutual exclusivity is confirmed and is the smallest safe V1 policy.**

## 9. Target-Behavior Matrix — CONSISTENCY_NEEDED (Final)

Confirmed by the local coefficient simulation (section 15) across all three tested coefficient candidates, identically:

| RunwayWeeks | CONSISTENCY | GENERAL_ENDURANCE | PRE_SPECIFIC_TRANSITION |
|---|---|---|---|
| 3 | 1 | 1 | 1 |
| 4 | 1 | 2 | 1 |
| 5 | 2 | 2 | 1 |
| 6 | 2 | 3 | 1 |
| 7 | 2 | 4 | 1 |
| 8 | 2 | 5 | 1 |

`ConsistencySecondWeekThreshold = RunwayWeeks >= 5` — confirmed, exact, and robust to coefficient choice.

**This matrix is APPROVED as final** for the `CONSISTENCY_NEEDED` profile, subject to the overall verdict in section 22 (which is blocked by the *other* profile, not this one — see section 22 for why a per-profile pass does not become a partial approval).

## 10. Target-Behavior Matrix — CORE_ENTRY_READY (Candidate vs. Simulation-Derived)

The candidate target matrix specified `AerobicStrengthSecondWeekThreshold = RunwayWeeks >= 6`. The local simulation (section 16) proves this specific matrix is **not** mechanically reproducible by the described constrained-proportional/floor/largest-remainder allocator under any of the three tested coefficient pairs (`0.70/0.30`, `0.65/0.35`, `0.60/0.40`), nor under a deliberately extreme `0.85/0.15` weighting. What the mechanism actually produces (identically across all three normal-range candidates) is:

| RunwayWeeks | GENERAL_ENDURANCE | AEROBIC_STRENGTH | PRE_SPECIFIC_TRANSITION |
|---|---|---|---|
| 3 | 1 | 1 | 1 |
| 4 | 2 | 1 | 1 |
| 5 | 2 | 2 | 1 |
| 6 | 3 | 2 | 1 |
| 7 | 4 | 2 | 1 |
| 8 | 5 | 2 | 1 |

I.e. the mechanically-produced threshold is `RunwayWeeks >= 5`, one week earlier than the candidate matrix specified. This is a structural property of proportional-weight allocation (`AerobicStrength`'s fractional remainder becomes competitive with `GeneralEndurance`'s at week 5 for every tested weight pair), not a tunable coefficient effect — reproducing the literal candidate matrix would require an explicit threshold-gated priority-insertion rule instead of, or in addition to, proportional weighting, which this phase is not authorized to design (no runtime allocator may be implemented here).

**This matrix is NOT approved as specified.** Even setting the mechanics question aside, `AEROBIC_STRENGTH` is catalog-`UNSUPPORTED` (section 5), which alone blocks this profile regardless of which threshold value is eventually chosen.

## 11. Final Second-Week Thresholds

- `CONSISTENCY_NEEDED.ConsistencySecondWeekThreshold`: **`RunwayWeeks >= 5`** — `REPOSITORY_SUPPORTED` in the sense that it is mechanically derived and verified from the allocator mechanics described in the task itself (not independently evidenced from external literature — no such evidence exists), robust across coefficient choice.
- `CORE_ENTRY_READY.AerobicStrengthSecondWeekThreshold`: **not finalized.** The candidate value (`>= 6`) is disproven as mechanically inconsistent with proportional weighting; the mechanically-consistent value under proportional weighting would be `>= 5`, but this is moot while the block itself is catalog-`UNSUPPORTED`. No production/dark contract value is set here — this remains an open design question for whichever future phase resolves the catalog-capacity blocker.

## 12. Final Min/Max Capacity Table

| Block | Profile | MinimumWeeks | MaximumWeeks | Justification |
|---|---|---|---|---|
| `PRE_SPECIFIC_TRANSITION` | both | 1 | 1 | Fixed bridge, `EVIDENCE_INFORMED` (section 4); not expandable in either profile. |
| `CONSISTENCY` | `CONSISTENCY_NEEDED` | 0 (absent when RunwayWeeks<3 is out of scope; effectively 1 within the supported 3-8 range) | 2 | Matches the simulation-confirmed matrix (section 9); capped at 2 to prevent over-consuming runway capacity that `GENERAL_ENDURANCE` should dominate, per the "General Endurance should remain dominant" evidence direction. |
| `GENERAL_ENDURANCE` | both | 1 | 5 | Dominant, always-eligible block in both profiles; capacity sized to absorb the full 3-8 week runway range once other blocks' minima/maxima are satisfied (matches simulation output in sections 9 and 10). |
| `AEROBIC_STRENGTH` | `CORE_ENTRY_READY` | 0 | 2 | Candidate values retained for documentation completeness only — **not approved for use**, since the block is catalog-`UNSUPPORTED` (section 5). Any future phase implementing this block must re-derive/re-approve these values alongside the catalog work, not simply reuse this table unexamined. |
| `CONSISTENCY` | `CORE_ENTRY_READY` | n/a | n/a | Not eligible in this profile (mutually exclusive per section 8). |
| `AEROBIC_STRENGTH` | `CONSISTENCY_NEEDED` | n/a | n/a | Not eligible in this profile (mutually exclusive per section 8). |

Classification of the numeric bounds themselves: `PROVISIONAL_PRODUCT_DEFAULT` (no external numeric evidence pins them; they are the task's own candidate values, retained where the simulation confirms they produce a defensible, robust outcome, and flagged unapproved where they don't).

## 13. Catalog-Capacity Audit Per Block

| Block | Catalog Support | Evidence |
|---|---|---|
| `CONSISTENCY` | `SUPPORTED` | Directly served by existing `EASY`/`LONG_RUN`-family content, matching `FOUNDATION` phase's own `eligibleWorkoutFamilies: ["EASY","LONG_RUN"]` and `intents: ["AEROBIC_BASE"]` (`ten-k-master.v6.json`) — the same content category the Core plan itself already uses for its own consistency-building Foundation phase. |
| `GENERAL_ENDURANCE` | `SUPPORTED` | Same `EASY`/`LONG_RUN`-family content; directly matches the block's own aerobic-endurance semantic. |
| `PRE_SPECIFIC_TRANSITION` | `PARTIALLY_SUPPORTED` | Base workout content (`EASY_STANDARD`/`LONG_RUN_STANDARD`) exists and could plausibly fill a bridge week, but no distinct "transition-specific" workout or stage-binding rule has been verified to exist or to correctly express the bridging semantic (easing toward Foundation-phase-specific loading) — flagged `NOT_VERIFIED` for the exact execution mechanism pending a future implementation phase, though base content is not blocking. |
| `AEROBIC_STRENGTH` | `UNSUPPORTED` | Section 5. Zero matching content under either semantic interpretation; the one directionally-relevant workout (FARTLEK `SURGE_AND_FLOAT`) is schema-restricted to `eligiblePhases: [BUILD]` only. |

## 14. Coefficient-Family Simulation — Methodology

A local, test-only xUnit file, `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/PreparationRunway/Phase4G6A2CoefficientSimulationTests.cs`, implements the task's own described 8-step mechanics generically: (1) mandatory minima, (2) remaining capacity, (3) normalized weights over currently-open eligible expandable blocks, (4) ideal proportional shares, (5) floor, (6) largest-remainder distribution (tie-break: fractional remainder desc → `AllocationPriority` desc → `CanonicalOrder` asc → key ordinal), (7) maxima enforcement, (8) deterministic overflow redistribution to remaining uncapped blocks using the same tie-break. This file is **not referenced by, and does not reference, any production runtime or DI composition** — it lives entirely inside the test project and exists solely to produce verifiable, deterministic evidence for this document. It is not itself a production allocator and is not wired into any live or dark orchestrator.

## 15. Coefficient-Family Simulation Results — CONSISTENCY_NEEDED

Three candidates tested: A (`Consistency=0.35/GeneralEndurance=0.65`), B (`0.40/0.60`), C (`0.45/0.55`), for RunwayWeeks 3-8. All three candidates produced **byte-identical** allocation output (section 9's table) — confirmed by `ConsistencyNeeded_AllThreeCandidates_ReproduceTheTargetMatrixAfterCapRedistribution` and `ConsistencyNeeded_SecondConsistencyWeek_AppearsExactlyAtRunwayFive_ForAllThreeCandidates` (both pass, 4 theory cases + 1 fact, 0 failures). The mechanism reason: `CONSISTENCY`'s low `MaximumWeeks=2` cap is reached quickly regardless of coefficient weighting, after which `GENERAL_ENDURANCE`'s generous `MaximumWeeks=5` absorbs all further capacity via overflow redistribution — making the exact coefficient choice immaterial to the final result for this profile. This is a genuine robustness finding: coefficient selection for `CONSISTENCY_NEEDED` may be made on secondary criteria (e.g. explainability) without affecting behavior.

## 16. Coefficient-Family Simulation Results — CORE_ENTRY_READY

Three candidates tested: A (`GeneralEndurance=0.70/AerobicStrength=0.30`), B (`0.65/0.35`), C (`0.60/0.40`), plus an extreme control (`0.85/0.15`). All three normal-range candidates produced identical output (section 10's table); `AEROBIC_STRENGTH` reaches its second week at `RunwayWeeks=5` in every case, not `RunwayWeeks=6` as the candidate matrix specified. The extreme `0.85/0.15` control (`CoreEntryReady_TargetMatrix_RequiresAnExplicitThresholdGateNotPureProportionalWeighting`) confirms this is not a coefficient-tuning problem: even at that extreme weighting, the exact candidate matrix (`GeneralEndurance=3/AerobicStrength=1` at week 5, `3/2` at week 6) is not reproduced. All 4 relevant test methods pass (3 theory cases + 1 fact).

Full RunwayWeeks 3-8 matrix for every candidate, both profiles, captured via `EvidenceCapture_FullMatrixForBothProfilesAcrossAllCandidates` (transcribed into sections 9-10 above).

## 17. Selected Coefficient Family and Rejected Alternatives

- **CONSISTENCY_NEEDED: no single coefficient pair is "selected" as materially better** — sections 9 and 15 show all three tested candidates (and, by the mechanism's own structural logic, the entire plausible range) are behaviorally equivalent for this profile. **Recorded default: candidate B (`0.40/0.60`)**, chosen only for symmetry/explainability (a round, easily-explained split), not because it produces different behavior — `PROVISIONAL_PRODUCT_DEFAULT`.
- **CORE_ENTRY_READY: no coefficient pair is selected.** Every tested candidate, including an extreme control, fails to reproduce the candidate target matrix (section 16), and the block itself is catalog-`UNSUPPORTED` regardless. No coefficient recommendation is made for this profile.

## 18. Final Allocation Priority and Canonical Order Metadata

| Block | CanonicalOrder | AllocationPriority (remainder tie-break) |
|---|---|---|
| `CONSISTENCY` | 1 | 3 |
| `GENERAL_ENDURANCE` | 2 | 4 (highest — matches "General Endurance should remain dominant") |
| `AEROBIC_STRENGTH` | 3 | 2 |
| `PRE_SPECIFIC_TRANSITION` | 4 | 1 (fixed, never participates in proportional distribution) |

`CanonicalOrder` reflects the blocks' narrative runway sequence (consistency/endurance development, then an aerobic-power qualifier if eligible, then a fixed pre-Core bridge). `AllocationPriority` governs largest-remainder/overflow tie-breaking only, and was exercised in the simulation's tie-break logic (section 14) without changing any of the confirmed results in sections 9-10, since no genuine ties occurred at the tested coefficient values for either profile.

## 19. TD / Risk Registry Updates

`TD-AEROBIC-STRENGTH-SEMANTICS-001` added `OPEN` to `plan-catalog/artifacts/audits/activation-readiness-risks.json` and its `.md` mirror, recording: the semantic resolution (interpretation B), the catalog-capacity blocker under either interpretation, and the independent allocator-mechanics non-reproducibility finding for `CORE_ENTRY_READY`'s target matrix. No existing TD was modified in a way that changes its status; this is a pure append. Aggregate updated from 18 total/10 OPEN/8 CLOSED to **19 total/11 OPEN/8 CLOSED**, recorded only in `activation-readiness-risks.json`'s `currentAppendOnlyStatus` field and mirrored once in the `.md` file's closing aggregate line (never restated per-entry), per this registry's established convention.

## 20. Repository Verification Performed

- New simulation test file: `Phase4G6A2CoefficientSimulationTests.cs` — 9 tests, 0 failures (`dotnet test ... --filter FullyQualifiedName~Phase4G6A2CoefficientSimulationTests`, Release configuration).
- Risk-registry parity/gate suite: `ActivationReadinessRiskParityTests` + `ActivationSafetyGateTests` — 15/15 passed (`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj --filter "FullyQualifiedName~ActivationReadinessRiskParityTests|FullyQualifiedName~ActivationSafetyGateTests"`, Release configuration).
- Full backend suite and full plan-catalog suite: run as part of this phase's closing verification pass (see the final report delivered alongside this document for exact pass/fail/skip counts).

## 21. Explicit Non-Goals / Prohibited Scope Confirmation

This phase did **not**: implement a production or dark runtime allocator; implement a largest-remainder runtime service; add any production allocation class; generate any runway week or workout; add any new workout definition; change catalog progression, workout-definition schema, volume/long-run progression, pace logic, or calendar assignment; touch persistence or author any database migration; change any preview/confirm response shape; activate 15-20 week behavior; implement the General Endurance staged plan or any part of 21-52 week support; implement 53+ rejection; perform intermediate-distance projection; or change any production configuration. `Phase4G6A2CoefficientSimulationTests.cs` is confirmed, by direct inspection of its own file (it lives in the test project, has no DI registration, and is referenced by no production `.csproj`), to be unreferenced by any production runtime or DI composition.

## 22. Final Verdict

**`TEN_K_PREPARATION_RUNWAY_POLICY_REMAINS_BLOCKED_BY_EXPLICIT_SEMANTIC_TYPED_SIGNAL_OR_CATALOG_CAPACITY_GAP`**

Exact blocker: **`AEROBIC_STRENGTH` is catalog-`UNSUPPORTED` under either semantic interpretation** (section 5, section 13), which alone blocks the `CORE_ENTRY_READY` profile regardless of coefficient or threshold choice, compounded by an independent finding that the profile's candidate target matrix is not mechanically reproducible by the described allocator mechanism even if the catalog gap were closed (section 10, section 16). Because `CORE_ENTRY_READY` is one of exactly two profiles in the smallest-safe-V1 two-profile model (section 8), and the task's own instruction is explicit that a partial approval must not be reported as complete, the overall 10K Preparation Runway allocation policy is **BLOCKED**, notwithstanding that the `CONSISTENCY_NEEDED` profile's own three blocks (`CONSISTENCY`, `GENERAL_ENDURANCE`, `PRE_SPECIFIC_TRANSITION`) are fully resolved, catalog-supported, and mechanically robust (sections 9, 13, 15).

## 23. Recommended Scope for Phase 4G.6A.3

1. A product/coaching decision on whether `AEROBIC_STRENGTH` is retained in the V1 Preparation Runway profile model at all. If retained: author a validated, versioned, catalog-executable workout definition for interpretation (B) (a hill-repeat, stride, or short aerobic-interval session), and extend the workout-definition schema's `eligiblePhases` enum (or an equivalent runway-eligibility mechanism) to permit its use outside `BUILD`. If not retained: formally revise the `CORE_ENTRY_READY` profile to a catalog-supportable alternative (e.g. `GENERAL_ENDURANCE`-only, or a different second block entirely) before any further allocator design work.
2. If `AEROBIC_STRENGTH` is retained and its catalog gap closed, revisit its second-week threshold: either accept the mechanically-consistent `RunwayWeeks >= 5` value this phase derived, or design an explicit threshold-gated priority-insertion rule (not proportional weighting) to preserve the originally-desired `RunwayWeeks >= 6` behavior — this phase does not choose between these two options, since doing so would begin allocator design, which is out of scope here.
3. Once both profiles are catalog-supported and mechanically resolved, a future phase may proceed to implement the actual dark generic allocator (still not wired to any live path) consuming this document's finalized capacity table, priority/canonical-order metadata, and eligibility mapping.
