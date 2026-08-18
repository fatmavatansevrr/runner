# PHASE 10K-FREQ.6D.4B.1 — Intermediate 5D Full-Component Prescription Evidence

## 1. Preflight

- Repository authority read: `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`.
- Repository-backed prerequisites: FREQ.6D.4A, FREQ.6D.4B, FREQ.6D.4C.1 and FREQ.6D.4C.2 are `DONE / VERIFIED` (ledger rows 60, 61, 63 and 64).
- FREQ.6D.4C.3 is not ledgered and has no phase report or commit; it is not retroactively treated as complete.
- Starting HEAD: `ef3dcf7c725a41935783ecaa97ec778bbbbb11bf`.
- Starting worktree: pre-existing `baseline_tmp` submodule dirt and untracked `.claude/` only. Neither belongs to this phase and neither was modified.
- Starting `git diff --check`: PASS.
- Scope: evidence and governance only. No production profile, WorkoutDefinition, validator, runtime, persistence or activation change.

## 2. Parent state

FREQ.6D.4B froze the athlete-facing main-set prescriptions for eight Intermediate×5D slots. The failed FREQ.6D.4C.3 preflight exposed a narrower parent gap: the profile contract requires an executable prescription for every structural component, while 4B froze only the main component.

There are 26 structural component rows across the eight exact definitions: 8 main components and 18 non-main components. Each non-main component needs both a work quantity and a typed intensity target. Therefore **36 component-level athlete-facing authority fields remain open**: 16 warm-up fields, 16 cool-down fields and 4 FARTLEK structural-RECOVERY fields. Recovery semantics and reuse rules are additional decisions, not extra schema fields.

## 3. 4B scope correction

Classification: `FREQ6D4B_MAIN_SET_POLICY_REMAINS_VALID`.

Nothing in this phase reopens the exact 4B matrix. In particular FND-P remains 6×30 s, BLD-S remains 10×60 s with 60 s jog between repetitions, and TAP-S remains 6×20 s with 100 s walk between repetitions. All other 4B main-set quantity, recovery, intensity, dose-category and distance-accounting selections remain binding.

## 4. Exact skeleton inventory

| Slot | Exact WorkoutDefinition | Status | Ordered skeleton |
|---|---|---|---|
| FND-P | `AEROBIC_STRENGTH_CONTROLLED_INTRO v3` | DRAFT | 1 WARM_UP (`EASY`), 2 MAIN_SET (`CONTROLLED_AEROBIC_POWER_INTRO`), 3 COOL_DOWN (`EASY`) |
| FND-S | `THRESHOLD_TEMPO v5` | DRAFT | 1 WARM_UP (`EASY`), 2 MAIN_SET (`THRESHOLD`), 3 COOL_DOWN (`EASY`) |
| BLD-P | `THRESHOLD_TEMPO v4` | VALIDATED | 1 WARM_UP (`EASY`), 2 MAIN_SET (`THRESHOLD`), 3 COOL_DOWN (`EASY`) |
| BLD-S | `FARTLEK v4` | VALIDATED | 1 WARM_UP (`EASY`), 2 MAIN_SET (`SURGE_AND_FLOAT`), 3 RECOVERY (`EASY_JOG`), 4 COOL_DOWN (`EASY`) |
| RS-P | `GOAL_PACE_TEN_K v2` | VALIDATED | 1 WARM_UP (`EASY`), 2 MAIN_SET (`GOAL_PACE`), 3 COOL_DOWN (`EASY`) |
| RS-S | `THRESHOLD_TEMPO v4` | VALIDATED | 1 WARM_UP (`EASY`), 2 MAIN_SET (`THRESHOLD`), 3 COOL_DOWN (`EASY`) |
| TAP-P | `GOAL_PACE_TEN_K v3` | DRAFT | 1 WARM_UP (`EASY`), 2 MAIN_SET (`GOAL_PACE`), 3 COOL_DOWN (`EASY`) |
| TAP-S | `FARTLEK v5` | DRAFT | 1 WARM_UP (`EASY`), 2 MAIN_SET (`SURGE_AND_FLOAT`), 3 RECOVERY (`EASY_JOG`), 4 COOL_DOWN (`EASY`) |

The four DRAFT statuses are the already-disclosed catalog-lifecycle blocker and are not changed here.

## 5. FULL_COMPONENT_REQUIREMENT_MATRIX

| Slot | Main-set dose location | Additional workQuantity authority | Additional typed intensity authority | Recovery semantics permitted/required |
|---|---|---|---|---|
| FND-P | component 2, Repeated; frozen by 4B | components 1 and 3 | components 1 and 3 | nested recovery required on component 2; structural recovery absent |
| FND-S | component 2, Continuous; frozen | components 1 and 3 | components 1 and 3 | recovery forbidden on all Continuous components |
| BLD-P | component 2, Continuous; frozen | components 1 and 3 | components 1 and 3 | recovery forbidden on all Continuous components |
| BLD-S | component 2, Repeated; frozen | components 1, 3 and 4 | components 1, 3 and 4 | nested recovery required on component 2; component 3 also structurally present |
| RS-P | component 2, Continuous; frozen | components 1 and 3 | components 1 and 3 | recovery forbidden on all Continuous components |
| RS-S | component 2, Continuous; frozen | components 1 and 3 | components 1 and 3 | recovery forbidden on all Continuous components |
| TAP-P | component 2, Continuous; frozen | components 1 and 3 | components 1 and 3 | recovery forbidden on all Continuous components |
| TAP-S | component 2, Repeated; frozen | components 1, 3 and 4 | components 1, 3 and 4 | nested recovery required on component 2; component 3 also structurally present |

Every non-main component needs exactly one positive duration or distance and exactly one descriptor matching its typed intensity mode.

## 6. Recovery taxonomy

1. `NESTED_INTER_REPETITION_RECOVERY` belongs to a Repeated component. The projector derives count as repetitions − 1 for `BetweenRepetitions`, or repetitions for `AfterEachRepetition`.
2. `STRUCTURAL_RECOVERY_COMPONENT` is a separate skeleton row and therefore projects as a separate executable component with its own work and intensity.
3. `COOL_DOWN` is a distinct session-ending component.

The schema and projector do not make these interchangeable. In particular a Continuous structural RECOVERY cannot carry nested recovery fields, while a Repeated structural RECOVERY would itself require repetitions and another nested recovery—neither shape expresses “the already-declared recoveries of MAIN_SET.”

## 7. FARTLEK recovery analysis

The exact v4/v5 order is WARM_UP → repeated MAIN_SET → RECOVERY → COOL_DOWN. However, repository canonical evidence explicitly says FARTLEK v3's RECOVERY “means recovery segments between variable efforts, not a separate post-workout recovery block” (`domain-wave2-component-vocabulary.md`, D8 `CANONICAL_CONFIRMED`). v4 and v5 inherit that unchanged skeleton.

Consequences:

- It is **not** evidenced as post-set recovery.
- It is **not** evidenced as an easy transition before cooldown.
- It is **not** evidenced as another workout block.
- Existing canonical evidence describes it as the between-effort recovery concept.
- 4B now represents that concept losslessly inside the repeated MAIN_SET. Authoring component 3 as another positive executable block would duplicate rather than encode the canonical concept.
- `BetweenRepetitions` prevents a final nested recovery (BLD-S count 9; TAP-S count 5). The separate component would still add a tenth/sixth recovery-like block after the set, so it changes the intended cardinality even without `AfterEachRepetition`.
- `AfterEachRepetition` would be worse: it would produce 10/6 nested recoveries and then add the structural component once more.

This is not a missing sports-science duration. It is a real WorkoutDefinition-skeleton versus newer profile-model ownership conflict.

## 8. Warm-up evidence

Scientific evidence supports active warm-up as a preparation mechanism but does not establish one exact recreational-10K workout dose. A trained-runner crossover study found a short specific warm-up as effective as a longer protocol for a three-minute run; a trained 5000 m study found benefit from a high-intensity protocol, but that is race-performance evidence and does not justify prescribing hard priming before every Appsel quality session. Static stretching can impair some explosive outcomes and does not improve running economy; this does not establish an executable running quantity.

The strongest complete-session coaching precedent already accepted in Appsel is Hal Higdon's Intermediate 10K plan: before speedwork, jog 1–2 miles, stretch, then easy strides. Its tempo construction uses 10–15 minutes easy at the start. These are `COMMON_COACHING_CONVENTION` / `EVIDENCE_SUPPORTED_RANGE`, not an exact product mandate.

Evidence envelope for this model:

- quantity: 10–15 minutes easy running is the clean shared duration envelope; 1–2 miles is a broader speedwork convention but would produce athlete-dependent duration;
- intensity: easy, optionally progressive/specific preparation outside this skeleton; the WorkoutDefinitions themselves bind WARM_UP to `EASY`;
- typed candidate: `EffortBased` with a real `EASY`-semantic descriptor (final vocabulary spelling remains a product decision);
- no evidence requires Primary versus SecondaryControlled or Foundation/Build/RaceSpecific/Taper quantity differences;
- reduced taper main-set volume does not itself support reducing warm-up.

Sources: [van den Tillaar et al., 2017](https://pubmed.ncbi.nlm.nih.gov/27191697/), [Silva et al., trained 5000 m runners](https://pubmed.ncbi.nlm.nih.gov/37293424/), [running warm-up systematic review](https://pubmed.ncbi.nlm.nih.gov/35096248/), [Hal Higdon Intermediate 10K](https://www.halhigdon.com/training-programs/10k-training/intermediate-10k/).

## 9. Cool-down evidence

Scientific evidence is substantially weaker than convention. The 2018 narrative review finds active cooldown largely ineffective for same/next-day performance, injury prevention, soreness and most recovery markers, though it can accelerate blood-lactate clearance and cardiovascular/respiratory normalization. A systematic review found 6–10 minute active recovery interventions most consistently positive, but judged overall evidence weak and intensity inconclusive.

Higdon's complete 10K-session convention says cooldown is half the warm-up; his tempo construction ends with 5–10 minutes easy. This supports a 5–10 minute easy candidate envelope, not a scientifically exact dose.

- scientific evidence: no exact compulsory dose and no phase/lane differentiation;
- coaching convention: 5–10 minutes easy or half the warm-up;
- product-default candidate: one shared easy 5–10 minute duration rule;
- typed candidate: `EffortBased`, `EASY`-semantic descriptor;
- taper does not independently justify a shorter cooldown.

Sources: [Van Hooren & Peake, 2018](https://pubmed.ncbi.nlm.nih.gov/29663142/), [Ortiz et al., 2019](https://pubmed.ncbi.nlm.nih.gov/29742750/), [Hal Higdon Intermediate 10K](https://www.halhigdon.com/training-programs/10k-training/intermediate-10k/).

## 10. Structural recovery evidence

McMillan's FARTLEK convention describes jog recoveries **between** surges; it supports 4B's nested recovery, not another block after the set. No internal or external source found describes FARTLEK v4/v5's component 3 as a distinct post-set/easy-transition block in addition to nested recovery. [McMillan's article](https://www.mcmillanrunning.com/the-lost-art-of-the-fartlek/) therefore corroborates the canonical internal meaning and supplies no independent structural quantity/intensity envelope.

Result: `FREQ6D4B1_BLOCKED_ON_STRUCTURAL_RECOVERY_EVIDENCE` at the product-evidence level and, more precisely for current implementation, `FREQ6D4B1_FULL_PROFILE_MODEL_NON_REPRESENTABLE`.

## 11. Intensity-target evidence

| Component | Evidence-supported typed envelope | Authority class |
|---|---|---|
| WARM_UP | `EffortBased`; easy/conversational descriptor consistent with WorkoutDefinition `EASY` | internal skeleton + coaching convention; exact profile descriptor requires decision |
| COOL_DOWN | `EffortBased`; easy descriptor consistent with WorkoutDefinition `EASY` | internal skeleton + weak scientific/coaching support; exact descriptor requires decision |
| structural RECOVERY | WorkoutDefinition says `EASY_JOG`, but repository semantics place this effort between surges already represented by nested recovery | descriptor meaning exists; separate executable target is unsupported/non-representable |

No `PROBE_*` string is a candidate. Pace-based or heart-rate-based support-component targets are technically representable but not supported by the present evidence envelope.

## 12. Profile-model mandatory-field audit

Profile-level mandatory fields are metadata, exact WorkoutDefinition reference, dose category, distance-accounting mode and a non-empty component list matching the exact skeleton.

Per component:

- `sequenceOrder`, `componentType`, `structureMode`, `workQuantity`, `intensityTarget` are mandatory;
- work has exactly one positive `durationSeconds` or `distanceMeters`;
- Continuous forbids `repetitionCount`, `recoveryQuantity` and `recoveryPlacement`;
- Repeated requires `repetitionCount >= 2`, one positive recovery duration/distance, valid recovery mode and placement;
- intensity requires exactly one descriptor in the field corresponding to `PaceBased`, `EffortBased` or `HeartRateBased`;
- component order/type must exactly match the WorkoutDefinition;
- projection preserves every component and derives nested recovery cardinality; it performs no semantic merge.

Classification: `FULL_PROFILE_AUTHORING_FIELD_INVENTORY_COMPLETE`.

## 13. Eight full-profile field maps

| Slot | WARM_UP | MAIN_SET | structural RECOVERY | COOL_DOWN |
|---|---|---|---|---|
| FND-P | quantity OPEN; intensity OPEN | quantity/intensity/nested recovery FROZEN_BY_4B | absent | quantity OPEN; intensity OPEN |
| FND-S | quantity OPEN; intensity OPEN | quantity/intensity FROZEN_BY_4B | absent | quantity OPEN; intensity OPEN |
| BLD-P | quantity OPEN; intensity OPEN | quantity/intensity FROZEN_BY_4B | absent | quantity OPEN; intensity OPEN |
| BLD-S | quantity OPEN; intensity OPEN | quantity/intensity/nested recovery FROZEN_BY_4B | quantity/intensity BLOCKED_BY_SEMANTIC_CONFLICT | quantity OPEN; intensity OPEN |
| RS-P | quantity OPEN; intensity OPEN | quantity/intensity FROZEN_BY_4B | absent | quantity OPEN; intensity OPEN |
| RS-S | quantity OPEN; intensity OPEN | quantity/intensity FROZEN_BY_4B | absent | quantity OPEN; intensity OPEN |
| TAP-P | quantity OPEN; intensity OPEN | quantity/intensity FROZEN_BY_4B | absent | quantity OPEN; intensity OPEN |
| TAP-S | quantity OPEN; intensity OPEN | quantity/intensity/nested recovery FROZEN_BY_4B | quantity/intensity BLOCKED_BY_SEMANTIC_CONFLICT | quantity OPEN; intensity OPEN |

## 14. Shared-default analysis

| Candidate | Classification | Reason |
|---|---|---|
| STANDARD_QUALITY_WARM_UP across all eight | `SUPPORTED_SHARED_DEFAULT` as a product-default candidate, not authority | same QUALITY family and `EASY` skeleton descriptor; no evidence for lane/phase differences |
| STANDARD_QUALITY_COOL_DOWN across all eight | `SUPPORTED_SHARED_DEFAULT` as a product-default candidate, not authority | same `EASY` skeleton descriptor; weak science but stable coaching convention |
| STANDARD_POST_SET_RECOVERY | `INSUFFICIENT_EVIDENCE` and semantically contradicted for these FARTLEKs | canonical component means between-effort recovery, not post-set recovery |

## 15. Primary/Secondary comparison

No reviewed evidence supports different WARM_UP or COOL_DOWN solely because dose category is Primary versus SecondaryControlled. The main-set distinction already carries the dose/stress difference. Classification: shared support-component policy is suitable unless 4B.2 deliberately supplies evidence for an exception.

## 16. Phase comparison

No evidence requires Foundation, Build, RaceSpecific and Taper support-component differences for these eight quality sessions. Specific/harder sessions can justify progressive drills or strides in coaching practice, but the current skeleton has no separate drill/stride component and WARM_UP remains `EASY`. Taper's reduced main dose does not imply reduced preparation or cooldown. Classification: no phase-specific difference evidenced; exact shared default still requires product selection.

## 17. Numeric compatibility

The 10–15 minute warm-up and 5–10 minute cooldown envelopes are plausible within the already-approved Intermediate×5D weekly regime and do not feed back into FREQ.6C's allocation formula. Exact kilometer compatibility cannot be asserted because duration-based easy components need athlete-specific speed for conversion and the repository does not define that conversion here. Classification: `ACCOUNTING_DEPENDS_ON_DISTANCE_ACCOUNTING_MODE` / `INSUFFICIENT_INFORMATION` for exact totals, with no evidence that the envelopes make FREQ.6C impossible.

The structural RECOVERY cannot be compatibility-tested without first resolving whether it should exist as an additional executable component.

## 18. Distance-accounting analysis

All eight profiles are frozen to `ESTIMATED_SESSION_TOTAL`; GOAL_PACE v2 obtains that capability via its exact overlay. The profile model and execution projector preserve the accounting enum but do **not** calculate distance. They preserve each component and nested recovery count independently.

Therefore:

- warm-up, cooldown and a structural RECOVERY remain separate executable work rows;
- nested recovery remains inside MAIN_SET and has derived cardinality;
- no canonical code inspected defines how duration/intensity become estimated kilometers or whether every component is summed;
- implementation must not invent arithmetic;
- a positive FARTLEK structural RECOVERY would be visible as extra work and is at material risk of double counting.

No full-component envelope proves the FREQ.6C weekly envelope impossible, but exact compatibility remains downstream-accounting dependent.

## 19. Fixture audit — FIXTURE_AUTHORITY_CLASSIFICATION

The FREQ.6D.4C.2 test file explicitly labels every profile a TEST fixture only.

| Fixture field | Classification |
|---|---|
| generic 300 s non-main work | `TEST_ONLY_PROBE`, `NOT_PRODUCTION_AUTHORITY` |
| `PROBE_PACE`, `PROBE_HR`, `PROBE_EFFORT` | `TEST_ONLY_PROBE`, `NOT_PRODUCTION_AUTHORITY` |
| generic repeated work 60 s | only BLD-S coincidentally matches; FND-P conflicts with frozen 30 s and TAP-S conflicts with frozen 20 s |
| generic Jog recovery | BLD-S and FND-P coincide with 4B; TAP-S conflicts with frozen Walk |
| BetweenRepetitions | `FROZEN_BY_4B`, but the fixture is not the authority |
| FND-P-specific 300 s EASY warm-up/cooldown | `TEST_ONLY_PROBE`; no 4B or external exact-value authority |

No validating fixture value is promoted.

## 20. QUALITY_SESSION_WARMUP_EVIDENCE_MATRIX

| Slot/workout type | Purpose | Quantity evidence | Intensity evidence | Shared default | Phase-specific reason | Source / class | Decision? |
|---|---|---|---|---|---|---|---|
| FND-P aerobic-power intro | prepare for short repetitions | 10–15 min easy candidate; 1–2 mi broader convention | easy; specific strides convention exists outside skeleton | suitable | none evidenced | Higdon + running studies; supported range/convention | yes |
| FND-S controlled threshold intro | prepare for continuous tempo | 10–15 min easy | gradual easy-to-work transition convention | suitable | none evidenced | Higdon tempo construction; convention | yes |
| BLD-P threshold tempo | prepare for longer tempo | 10–15 min easy | easy/progressive | suitable | none evidenced | Higdon direct convention | yes |
| BLD-S fartlek | prepare for surges | 10–15 min easy | easy; optional strides convention | suitable | none evidenced | Higdon/McMillan convention | yes |
| RS-P 10K goal pace | prepare for specific pace | 10–15 min easy | easy; high-intensity priming evidence is directional only | suitable | specificity may justify drills, not a different executable quantity | 5000 m study + Higdon | yes |
| RS-S threshold support | prepare for controlled tempo | 10–15 min easy | easy/progressive | suitable | none evidenced | Higdon convention | yes |
| TAP-P goal pace | retain readiness without fatigue | 10–15 min easy | easy; no basis to cut warm-up with main dose | suitable | no taper difference evidenced | taper principle + convention | yes |
| TAP-S sharpening fartlek | prepare for very short surges | 10–15 min easy | easy; strides convention directionally relevant | suitable | no taper difference evidenced | convention | yes |

## 21. QUALITY_SESSION_COOLDOWN_EVIDENCE_MATRIX

| Slot/workout type | Purpose | Quantity evidence | Intensity evidence | Shared default | Phase-specific reason | Source / class | Decision? |
|---|---|---|---|---|---|---|---|
| FND-P | gradual post-session transition | 5–10 min candidate | easy | suitable | none | systematic reviews + Higdon; weak evidence/convention | yes |
| FND-S | same | 5–10 min | easy | suitable | none | same | yes |
| BLD-P | same | 5–10 min | easy | suitable | none | same | yes |
| BLD-S | same; distinct from inter-rep recovery | 5–10 min | easy | suitable | none | same | yes |
| RS-P | same | 5–10 min | easy | suitable | none | same | yes |
| RS-S | same | 5–10 min | easy | suitable | none | same | yes |
| TAP-P | same | 5–10 min | easy | suitable | no evidence for taper reduction | same | yes |
| TAP-S | same; distinct from nested recovery | 5–10 min | easy | suitable | no evidence for taper reduction | same | yes |

## 22. STRUCTURAL_RECOVERY_EVIDENCE_MATRIX

| Slot | Exact definition / position | Evidenced purpose | Quantity envelope | Intensity envelope | Nested relationship | Accounting implication | Evidence class | Decision |
|---|---|---|---|---|---|---|---|---|
| BLD-S | FARTLEK v4 / component 3 after MAIN_SET | canonical: between-variable-effort recovery, not post-set | no separate envelope | `EASY_JOG` describes the between-effort concept | same semantic concept as frozen 60 s Jog BetweenRepetitions | extra row would add work beyond 9 nested recoveries | canonical internal evidence; model conflict | architecture/product correction required |
| TAP-S | FARTLEK v5 / component 3 after MAIN_SET | inherited canonical meaning; not separately re-decided | no separate envelope | inherited `EASY_JOG`, but frozen nested mode is Walk | semantic conflict is even stronger: skeleton says jog while 4B nested recovery says walk | extra row adds work beyond 5 nested recoveries and mixes modes | canonical internal evidence + direct 4B contradiction | architecture/product correction required |

## 23. Complete amendment decision inventory

The original D1–D18 remain unchanged. New deterministic amendment decisions:

| ID | Required decision |
|---|---|
| FC1 | Confirm one shared WARM_UP rule or enumerate evidence-backed exceptions. |
| FC2 | Select exact shared/exception WARM_UP quantity and unit within the evidence envelope. |
| FC3 | Select typed WARM_UP intensity mode and production descriptor vocabulary. |
| FC4 | Confirm one shared COOL_DOWN rule or enumerate evidence-backed exceptions. |
| FC5 | Select exact shared/exception COOL_DOWN quantity and unit within the evidence envelope. |
| FC6 | Select typed COOL_DOWN intensity mode and production descriptor vocabulary. |
| FC7 | Resolve FARTLEK skeleton ownership: remove/supersede the structurally duplicated RECOVERY in new exact versions, or change the profile model so the skeleton marker can bind to MAIN_SET nested recovery without projecting extra work. |
| FC8 | If FC7 deliberately retains a separate executable component, provide new canonical purpose evidence and exact quantity; current evidence does not support this branch. |
| FC9 | If FC7 retains it, select typed intensity and reconcile `EASY_JOG` with TAP-S's frozen nested Walk without reopening 4B. |
| FC10 | Freeze distance-accounting treatment for all component work and nested recoveries, including an enforceable no-double-count invariant. |

Count: **10 decisions (FC1–FC10)**. FC1/FC4 minimize redundant per-slot choices; FC7 prevents FC8/FC9 from becoming hidden implementation judgments.

## 24. Full representability

Six non-FARTLEK profiles have complete evidence-supported non-main envelopes, with exact defaults pending. BLD-S and TAP-S do not: the exact skeleton forces an additional executable component whose canonical meaning is already owned by nested MAIN_SET recovery.

Classification: `PROFILE_MODEL_REQUIRES_UNSUPPORTED_ATHLETE_FACING_VALUES` and `FREQ6D4B1_FULL_PROFILE_MODEL_NON_REPRESENTABLE` for the current exact FARTLEK references. This does not invalidate 4B main-set policy; it identifies the amendment/architecture boundary.

## 25. FREQ.6D.4B.2 readiness

The evidence phase has inventoried every field, supplied warm-up/cooldown envelopes, separated recovery concepts, excluded fixtures and enumerated every remaining decision. A decision phase can now select FC1–FC6 and FC10.

However FC7 is not an ordinary athlete-facing default: 4B.2 must explicitly choose an architecture-compatible correction path before FC8/FC9 can even be considered. It must not manufacture a post-set recovery value from absent evidence. FREQ.6D.4C.3 remains not ready until that correction and all applicable FC decisions close.

## 26. Final classification

Primary classification: **`FREQ6D4B1_FULL_PROFILE_MODEL_NON_REPRESENTABLE`**.

Supporting classifications:

- `FREQ6D4B_MAIN_SET_POLICY_REMAINS_VALID`
- `FULL_PROFILE_AUTHORING_FIELD_INVENTORY_COMPLETE`
- warm-up/cooldown evidence envelopes complete; exact product defaults required
- `FREQ6D4B1_BLOCKED_ON_STRUCTURAL_RECOVERY_EVIDENCE`
- six profiles have evidence-supported full-component envelopes; two FARTLEK profiles are blocked by a canonical semantic/model conflict
- next phase: FREQ.6D.4B.2 full-component product/architecture amendment; FREQ.6D.4C.3 remains blocked
- push gate: completion brings the repository-backed count since the last gate to approximately eight completed phase prompts, below the roadmap's approximately-ten Gate B threshold; `PUSH_GATE_NOT_REACHED`
