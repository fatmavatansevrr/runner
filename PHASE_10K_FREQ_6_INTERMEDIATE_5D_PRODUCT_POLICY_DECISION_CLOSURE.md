# PHASE 10K-FREQ.6 — Intermediate 5D Product Policy Decision Closure

## 1. Scope

Product/domain authority only for `TEN_K × INTERMEDIATE × 5D × CORE`. No production implementation, public activation, catalog authoring, workout enrichment, or test mutation was performed.

Primary input: `PHASE_10K_FREQ_5_INTERMEDIATE_5D_SEVERITY_AND_PAIRING_EVIDENCE.md`.

## 2. Binding previous state

Frozen: `RUN_LAYOUT_5D = 2 KEY + 2 EASY_SUPPORT + 1 LONG_RUN`; weekly partitioning; severity-first/role-aware ordering; B1 worst-week-wins; original-week phase lineage; recency blindness; one post-aggregation numeric anchor; safety OR; persistence/replay determinism; FREQ.4 N≥1 cardinality mechanisms; FREQ.4A real KEY↔KEY coverage.

The 4D matrix is structural, not percentage-based. FREQ.5 supplied directional evidence but no exact role weights or thresholds.

## 3. Five severity-model comparison

| Model | §7 compatibility | Monotonic / role-aware | Precision / phase risk | Explainability, determinism, testing | Migration / 6D+ |
|---|---|---|---|---|---|
| A1 extended severity-first branches | High | Yes / categorical | Low false precision; low phase coupling | High | Moderate; frequency policy still needed |
| A2 normalized ratio | Low: changes structural authority into fraction | Monotonic but role-blind unless patched | High boundary/arithmetic convenience risk | Simple but semantically misleading | Superficially high |
| A3 weighted scalar | Medium | Yes | Unacceptable false precision; weights unsourced | Score is deterministic but hard to justify | High only if valid weights existed |
| A4 count floor + minimum role gates | Highest | Yes / explicit | Low false precision; phase invariant possible | High | Good; gates remain explicit per cardinality |
| A5 finite vector table | Representation, not independent semantics | Exact | No implicit precision; table-size cost | Highest testability/determinism | State count grows, but generation can be systematic |

Decisions:

- `NORMALIZED_RATIO_MODEL_REJECTED` — `REJECTED_OPTION`.
- `ROLE_WEIGHTED_SCALAR_MODEL_REJECTED_FOR_V1` — `REJECTED_OPTION`.
- `SELECTED_SEVERITY_SEMANTICS_MODEL = A4`, semantically the disciplined A1 continuation: monotonic count floor plus role-aware high-adherence gate.
- `RECOMMENDED_POLICY_REPRESENTATION = A5 finite state table`.

Authority: `APPROVED_PRODUCT_DEFAULT`, constrained by `EXISTING_CANONICAL_RULE` severity-first ordering.

## 4. Selected 0–5 broad severity tiers

| Completed | Broad result before role refinement | Reason and 4D comparison | Authority |
|---:|---|---|---|
| 0 | Reduce | No effective training; identical qualitative meaning to 4D | EXISTING_CANONICAL_RULE |
| 1 | Reduce | Isolated completion is insufficient to establish sustainable weekly execution; same as 4D | APPROVED_PRODUCT_DEFAULT constrained by §7 |
| 2 | Maintain | Preserves 4D's explicit “2 completed never progresses”; avoids role-first paradox | EXISTING_CANONICAL_RULE |
| 3 | Maintain | Material work occurred, but two of five planned sessions are absent; no top-tier role override | APPROVED_PRODUCT_DEFAULT |
| 4 | Maintain ceiling, eligible for role refinement | High adherence but one structural role is missing | APPROVED_PRODUCT_DEFAULT |
| 5 | Progress | Full effective execution | EXISTING_CANONICAL_RULE generalized to full cardinality |

This is not `/4 → /5` scaling. The accepted product semantics are: near-zero execution reduces; partial execution maintains; full execution progresses; exactly-one-missed is the only incomplete high-adherence state eligible for role discrimination.

## 5. Role equivalence and categorical gates

- EASY1 and EASY2 are symmetric.
- KEY1 and KEY2 are symmetric **for adherence severity**, even when Track B assigns primary/secondary prescription dose.
- LONG is a high-adherence-tier discriminator, not a numeric weight or independent safety gate.
- At 4/5, Progress requires both KEYs and LONG completed; therefore the sole missed session must be EASY. Missing either KEY or LONG yields Maintain.
- EASY remains valuable: missing two EASY sessions produces 3/5 Maintain, not Progress; EASY completions contribute normally to the count floor.
- `SEVERITY_POLICY_PHASE_INVARIANT` — workout purpose changes by phase; adherence semantics do not.

Classification: `APPROVED_PRODUCT_DEFAULT`, directionally evidence-informed but not literature-derived numeric truth.

## 6. Complete 5D weekly severity matrix

`E` is Easy completed count (0–2). KEY labels remain separate to make symmetry explicit. All 24 reachable symmetry-distinct vector rows are present.

| Count | K1 | K2 | LONG | E | Outcome | Rule | Authority | Notes |
|---:|:---:|:---:|:---:|---:|---|---|---|---|
| 0 | N | N | N | 0 | Reduce | count 0–1 | EXISTING_CANONICAL_RULE | none complete |
| 1 | N | N | N | 1 | Reduce | count 0–1 | APPROVED_PRODUCT_DEFAULT | one Easy only |
| 2 | N | N | N | 2 | Maintain | count 2–3 | EXISTING_CANONICAL_RULE | both Easy only |
| 1 | N | N | Y | 0 | Reduce | count 0–1 | APPROVED_PRODUCT_DEFAULT | Long only cannot override severity |
| 2 | N | N | Y | 1 | Maintain | count 2–3 | EXISTING_CANONICAL_RULE | — |
| 3 | N | N | Y | 2 | Maintain | count 2–3 | APPROVED_PRODUCT_DEFAULT | both KEYs absent |
| 1 | N | Y | N | 0 | Reduce | count 0–1 | APPROVED_PRODUCT_DEFAULT | KEY2 only |
| 2 | N | Y | N | 1 | Maintain | count 2–3 | EXISTING_CANONICAL_RULE | — |
| 3 | N | Y | N | 2 | Maintain | count 2–3 | APPROVED_PRODUCT_DEFAULT | KEY1 and Long absent |
| 2 | N | Y | Y | 0 | Maintain | count 2–3 | EXISTING_CANONICAL_RULE | KEY2+Long |
| 3 | N | Y | Y | 1 | Maintain | count 2–3 | APPROVED_PRODUCT_DEFAULT | one KEY and one Easy absent |
| 4 | N | Y | Y | 2 | Maintain | high-tier role gate | APPROVED_PRODUCT_DEFAULT | sole miss KEY1 |
| 1 | Y | N | N | 0 | Reduce | count 0–1 | APPROVED_PRODUCT_DEFAULT | KEY1 only; symmetric with KEY2 |
| 2 | Y | N | N | 1 | Maintain | count 2–3 | EXISTING_CANONICAL_RULE | — |
| 3 | Y | N | N | 2 | Maintain | count 2–3 | APPROVED_PRODUCT_DEFAULT | KEY2 and Long absent |
| 2 | Y | N | Y | 0 | Maintain | count 2–3 | EXISTING_CANONICAL_RULE | KEY1+Long |
| 3 | Y | N | Y | 1 | Maintain | count 2–3 | APPROVED_PRODUCT_DEFAULT | one KEY and one Easy absent |
| 4 | Y | N | Y | 2 | Maintain | high-tier role gate | APPROVED_PRODUCT_DEFAULT | sole miss KEY2 |
| 2 | Y | Y | N | 0 | Maintain | count 2–3 | EXISTING_CANONICAL_RULE | both KEYs only |
| 3 | Y | Y | N | 1 | Maintain | count 2–3 | APPROVED_PRODUCT_DEFAULT | Long + one Easy absent |
| 4 | Y | Y | N | 2 | Maintain | high-tier role gate | APPROVED_PRODUCT_DEFAULT | sole miss Long |
| 3 | Y | Y | Y | 0 | Maintain | count 2–3 | APPROVED_PRODUCT_DEFAULT | both Easy absent |
| 4 | Y | Y | Y | 1 | Progress | high-tier role gate | APPROVED_PRODUCT_DEFAULT | sole miss Easy |
| 5 | Y | Y | Y | 2 | Progress | full completion | EXISTING_CANONICAL_RULE | all complete |

## 7. Completion, repair and invalid-state semantics

Canonical final-effective-execution remains authoritative:

- Calendar-repaired session: counts under its original logical structural role when the terminal lineage session completes.
- `SubstituteFutureEasy`: the recovered priority root counts under the original KEY/LONG role; the superseded Easy remains informational and is not a negative adherence signal.
- Safely substituted workout identity with structural role retained: counts under original structural role.
- Down-dosed session: if canonically planned and completed, counts as that structural role; dose importance is not a second adherence accounting system.
- Workout identity changes do not change adherence role without an explicit structural-lineage change.

Structurally corrupt input fails closed through the existing typed invariant family (`AdaptationLineageInvalidException` or a frequency-policy invariant exception in implementation): completed count must equal `K1 + K2 + LONG + E`; planned KEY count must be 2; `E ∈ [0,2]`; role lineage must be known. No silent normalization.

## 8. B1 ordinal compatibility

`CAN_4D_AND_5D_WEEKLY_OUTCOMES_BE_AGGREGATED_BY_THE_EXISTING_B1_ORDER = YES`.

Both policies give the same ordinal meaning:

- Reduce: weekly execution too weak to sustain the planned upward load response.
- Maintain: meaningful but insufficient/role-incomplete execution; hold rather than progress.
- Progress: full execution or the canonical high-adherence exception where only Easy is missed and all KEY/LONG roles completed.

Thus unchanged `Reduce < Maintain < Progress` is meaningful across 4D and 5D. If mixed-frequency weeks ever coexist in one persisted window, original-week policy dispatch occurs before B1; B1 sees only the shared ordinal enum. No recency or numeric-anchor change.

## 9. Severity policy identity

- `PolicyKey`: `TEN_K_INTERMEDIATE_5D_WEEKLY_ADAPTATION_SEVERITY_V1`
- `Version`: 1
- `Scope`: TEN_K × INTERMEDIATE × 5D × Core structural weeks
- Authority owner: Frequency-scoped Adaptation Severity Policy; RunLayout supplies counts, not outcomes
- Provenance: `APPROVED_PRODUCT_DEFAULT` constrained by canonical severity-first/role-aware semantics

It is not presented as a generic all-frequency rule.

## 10. KEY1/KEY2 prescription architecture

RunLayout retains two identical structural `KEY_SESSION` roles. `PRIMARY` and `SECONDARY_CONTROLLED` are phase-relative prescription/dosage labels, not permanent structural subroles. Slot identity/ordinal plus phase prescription policy can express different purposes without expanding role taxonomy.

Frozen invariant: `TWO_STRUCTURAL_KEY_SLOTS_DO_NOT_IMPLY_TWO_EQUAL_SEVERITY_STIMULI`.

Track B “primary” does not grant higher Track A adherence weight.

## 11. Phase product decisions

Every exact purpose selection below is an `EVIDENCE_INFORMED_PRODUCT_DEFAULT`; catalog identity and dosage readiness are audited separately.

| Phase | KEY1 / PRIMARY purpose | KEY2 / SECONDARY_CONTROLLED purpose | Differentiation rule |
|---|---|---|---|
| Foundation | Controlled aerobic-strength/economy stimulus: short hills/strides, non-exhaustive | Controlled threshold introduction at lower fatigue/dose | Complementary neuromuscular/economy + introductory aerobic-quality; neither maximal |
| Build | Controlled threshold/MIT development | Controlled fartlek/VO2-oriented support, lower accumulated stress than primary | Different purpose/fatigue profile; two threshold-family formats permitted only if prescription structures are materially distinct |
| RaceSpecific | 10K-specific rehearsal | Controlled threshold support | Primary specificity + secondary aerobic-quality support; never two automatic GOAL_PACE duplicates |
| Taper | Reduced-dose 10K-specific sharpening | Reduced-dose economy/strides sharpening, secondary controlled | Retain specificity/frequency while reducing total dose; no new taper multiplier |

## 12. Taper structural authority and product decision

The frozen RunLayout is phase-invariant: stage/materialization repeats all resolved layout slots in every phase, including Taper. No phase-specific role-cardinality override exists. Therefore one-KEY taper would require a separate structural authority decision and cannot be selected here.

Selected: `RETAIN_TWO_KEY_EXPOSURES`.

- Both remain structural KEY roles.
- KEY1 is reduced-dose race-specific sharpening.
- KEY2 is lower-dose economy/strides sharpening.
- Existing total taper multiplier/authority remains unchanged.
- “Reduced dose” must be achieved inside the eventual 5D allocation/prescription authority, not by deleting/reclassifying a role or lowering floors.

This product selection is approved, but numeric taper feasibility is not proven because the required 5D load authorities are incomplete (§19).

## 13. Relative dose and identity differentiation

- KEY1 = `PRIMARY`; KEY2 = `SECONDARY_CONTROLLED` in every phase, but purposes are phase-specific.
- No exact percentage/share is selected.
- Product invariant: the two sessions must have materially different purpose, intensity-duration profile, or prescription structure; catalog key inequality alone is neither necessary nor sufficient.
- Duplicate maximal work, automatic duplicate identity, and identical prescription structures are forbidden.
- Two threshold-family sessions may coexist only when one is primary and the other demonstrably lower-dose/different-format.

Current equal KEY distance splitting from FREQ.4 cannot by itself represent asymmetric prescription dose. Distance equality could coexist with different intensity structures in principle, but the current artifacts do not encode the selected structures fully; this is a prescription/catalog gap.

## 14. Spacing

- `KEY_KEY_SPACING_REUSED`: FREQ.4's 2-day product default and FREQ.4A real DB coverage.
- `KEY_LONG_SPACING_REUSED`: existing 2-day authority.

No new number or spacing authority was created.

## 15. Fallback and week-to-week rotation

Fallback order:

1. Existing phase/stage fallback, only if it remains inside the selected purpose/dose envelope.
2. A versioned deterministic approved alternative inside the same envelope.
3. Otherwise typed `PRODUCT_INELIGIBLE`.

Forbidden: nearest identity, arbitrary Easy substitution, automatic duplication of the other KEY, silently dropping a KEY, or Level/Frequency coercion.

Rotation: progression-stage-driven and phase-relative across 8–14-week compression/extension. No absolute week numbers. KEY1 follows the primary stage progression; KEY2 follows a separate secondary-purpose progression synchronized by phase, never by copying KEY1.

## 16. Current catalog capacity audit

Direct artifact result:

- `FARTLEK v4`: validated BUILD identity with component labels but no repetitions, work duration/distance or recovery dose.
- `THRESHOLD_TEMPO v4`: validated BUILD/RACE_SPECIFIC identity with no main-set duration/distance/repetition dose.
- `GOAL_PACE_TEN_K v2`: validated RACE_SPECIFIC identity and PACE_BASED mode, but generic components alone do not provide the full selected 5D relative-dose/rotation policy.
- No eligible KEY identity represents Foundation aerobic-strength/economy or Taper economy/strides sharpening.
- Existing progression has one stage-controlled candidate per week, not two coordinated phase-relative progressions.

`FARTLEK/THRESHOLD result = GAP_LOAD_BEARING_AND_BLOCKS_5D_IMPLEMENTATION`.

### INTERMEDIATE_5D_POLICY_CAPACITY_MATRIX

| Slot | Selected purpose | Selected catalog identity | Structure sufficient? | Dose representable? | Fallback? | Ready? | Blocking gap |
|---|---|---|:---:|:---:|---|:---:|---|
| Foundation KEY1 | aerobic-strength/economy | none truthful | No | No | none approved | No | identity + structure absent |
| Foundation KEY2 | controlled threshold intro | none Foundation-eligible | No | No | none approved | No | eligibility + dose absent |
| Build KEY1 | threshold/MIT | THRESHOLD_TEMPO candidate | No | No | typed ineligible only | No | main-set dose absent |
| Build KEY2 | controlled fartlek/VO2 support | FARTLEK candidate | No | No | typed ineligible only | No | interval/recovery dose absent |
| RaceSpecific KEY1 | 10K rehearsal | GOAL_PACE_TEN_K candidate | Partial | No | typed ineligible only | No | 5D primary-dose progression absent |
| RaceSpecific KEY2 | threshold support | THRESHOLD_TEMPO candidate | No | No | typed ineligible only | No | main-set dose absent |
| Taper KEY1 | reduced 10K sharpening | none Taper-eligible | No | No | none approved | No | taper identity/stage/dose absent |
| Taper KEY2 | economy/strides sharpening | none Taper-eligible | No | No | none approved | No | identity/stage/dose absent |

The target policy is intentionally not weakened to fit this capacity.

## 17. 5D peak/allocation/long-run authority and 8–14 representability

Repository confirmation:

- Peak band exists: TEN_K/INTERMEDIATE/5 runs, 36–50 km in `PEAK_VOLUME_BANDS_V1`.
- No approved 5D-specific `ResolvedPeakReference` or corresponding numeric trajectory input exists in `VolumeSafetyPolicy`.
- No approved 5D starting-volume missing/explicit-zero authority was found.
- FREQ.4 generalized mechanical N-key allocation, but selected primary/secondary dose asymmetry has no approved numeric allocation authority.
- No separately approved 5D long-run share/floor policy was found; importing 4D values is forbidden.

Therefore exact taper and full-week representability cannot be computed from approved authorities. This is an authority absence, not evidence of ineligibility.

| Horizon | Relevant Intermediate readiness categories | Outcome | Reason |
|---:|---|---|---|
| 8 | all current categories | DECISION_REQUIRED | missing 5D peak reference/start/allocation/long-run authority |
| 9 | all current categories | DECISION_REQUIRED | same |
| 10 | all current categories | DECISION_REQUIRED | same |
| 11 | all current categories | DECISION_REQUIRED | same |
| 12 | all current categories | DECISION_REQUIRED | same |
| 13 | all current categories | DECISION_REQUIRED | same |
| 14 | all current categories | DECISION_REQUIRED | same |

No `INTERMEDIATE_5D_TAPER_PRODUCT_INELIGIBILITY_REQUIRED` claim is made yet; no numeric matrix exists to support it. The missing cross-axis authorities are a non-catalog architecture/product-authority blocker.

## 18. Cross-policy consistency

- Severity KEY equivalence is independent of prescription primary/secondary dose: no coupling bug.
- Phase-invariant adherence remains compatible with phase-relative prescriptions.
- Two taper KEY roles preserve RunLayout ownership; dose reduction remains taper/prescription-owned.
- B1 remains ordinal and numeric anchor remains once per aggregated window.
- Spacing supports two KEYs but does not make the missing prescriptions feasible.
- Equal mechanical distance split does not silently claim equal physiological severity; asymmetric dose awaits explicit authority.
- No frozen progression, taper, spacing, volume or public-routing authority is mutated.

Product semantics contain no internal contradiction. Current implementation capacity does.

## 19. FREQ6_DECISION_INVENTORY — all 24 items

| ID | Question | FREQ.5 status | Options | Selected outcome | Authority | Reason | Dependency | Blocking? |
|---:|---|---|---|---|---|---|---|:---:|
| 1 | severity model | evidence envelope | A1–A5 | A4 semantics/A5 representation | APPROVED_PRODUCT_DEFAULT | canonical ordering without weights | new 5D policy | No |
| 2 | exact 0–5 tiers | unsourced exacts | alternative bands | 0–1 R; 2–4 M ceiling; 5 P | APPROVED_PRODUCT_DEFAULT | monotonic structural semantics | policy/table | No |
| 3 | role branches | directional | various gates | only 4/5 role-refined | APPROVED_PRODUCT_DEFAULT | high-adherence analogue | table | No |
| 4 | KEY severity symmetry | no direct evidence | symmetric/asymmetric | symmetric | APPROVED_PRODUCT_DEFAULT | avoids prescription coupling | counted KEY evidence | No |
| 5 | LONG vs KEY/EASY | no weights | categorical choices | 4/5 discriminator | APPROVED_PRODUCT_DEFAULT | preserves §7 role gate | table | No |
| 6 | phase dependence | no requirement | invariant/dependent | invariant | APPROVED_PRODUCT_DEFAULT | separates adherence/prescription | dispatch | No |
| 7 | repaired/substituted/down-dose | canonical lineage | new accounting/reuse | final effective original role | EXISTING_CANONICAL_RULE | one evidence authority | existing builder | No |
| 8 | ties/invalid states | fail-closed precedent | normalize/fail | typed fail closed | EXISTING_CANONICAL_RULE | determinism/integrity | invariant exception | No |
| 9 | B1/4D ordinal compatibility | to confirm | yes/no | yes | DERIVED_FROM_FROZEN_MECHANISM | same enum meaning | pre-B1 dispatch | No |
| 10 | policy identity/tests | required | generic/scoped | scoped V1 + 24-state contract | APPROVED_PRODUCT_DEFAULT | no false genericity | registry/tests | No |
| 11 | Foundation pair | broad envelope | several | economy/aerobic-strength + threshold intro | EVIDENCE_INFORMED_PRODUCT_DEFAULT | controlled complement | catalog enrichment | Yes |
| 12 | Build pair | broad envelope | threshold/VO2/etc. | threshold + controlled fartlek/VO2 | EVIDENCE_INFORMED_PRODUCT_DEFAULT | complementary stress | catalog enrichment | Yes |
| 13 | RaceSpecific pair | broad envelope | rehearsal/support | 10K rehearsal + threshold | EVIDENCE_INFORMED_PRODUCT_DEFAULT | specificity + support | catalog enrichment | Yes |
| 14 | taper cardinality | two envelopes | retain 2/retain 1 | retain 2 | DERIVED_FROM_FROZEN_MECHANISM | phase-invariant RunLayout | taper prescriptions | Yes |
| 15 | taper dose relation | directional only | equal/asymmetric | primary reduced + lower secondary | EVIDENCE_INFORMED_PRODUCT_DEFAULT | retained exposure, reduced load | numeric authority | Yes |
| 16 | must identities differ | unresolved | inequality/stimulus rule | nonduplicate-stimulus rule | APPROVED_PRODUCT_DEFAULT | names do not prove stress | validator/schema | Yes |
| 17 | relative dose | no exact ratio | fixed/phase-specific/none | categorical PRIMARY/SECONDARY | EVIDENCE_INFORMED_PRODUCT_DEFAULT | avoids fake precision | prescription policy | Yes |
| 18 | spacing | frozen FREQ.4/4A | reuse/reopen | reuse both 2-day rules | EXISTING_CANONICAL_RULE | no contradiction | calendar/repair | No |
| 19 | implementable identities | capacity question | bind/enrich/block | enrich then bind; currently block | DERIVED_FROM_FROZEN_MECHANISM | names lack capability | workout catalog | Yes |
| 20 | F/T structure gap | known | load-bearing/not | load-bearing blocker | DERIVED_FROM_FROZEN_MECHANISM | selected Build/Race purposes require it | enrichment | Yes |
| 21 | runtime fallback | product choice | alternatives/ineligible | approved same-envelope fallback else typed ineligible | APPROVED_PRODUCT_DEFAULT | no coercion | fallback registry | Yes |
| 22 | week rotation | unresolved | fixed/deterministic/stage | dual stage-driven phase-relative | APPROVED_PRODUCT_DEFAULT | 8–14 compatible | progression artifact | Yes |
| 23 | two KEY hardness | directional evidence | equal/not implied | not equal by implication | EVIDENCE_INFORMED_PRODUCT_DEFAULT | structural≠physiological | prescription metadata | No |
| 24 | provenance per pairing | required | evidence/default | evidence-informed defaults | EXISTING_CANONICAL_RULE | honest evidence boundary | governance metadata | No |

## 20. 24-item closure status

| IDs | Status |
|---|---|
| 1–10 | RESOLVED_AS_PRODUCT_DEFAULT (7 and 9 additionally reuse/derive frozen authority) |
| 11–17 | RESOLVED_WITH_IMPLEMENTATION_BLOCKER |
| 18 | RESOLVED |
| 19–22 | RESOLVED_WITH_IMPLEMENTATION_BLOCKER |
| 23–24 | RESOLVED_AS_PRODUCT_DEFAULT |

No FREQ.5 product item remains `DECISION_REQUIRED`. The separate 8–14 numeric authority matrix remains DecisionRequired and blocks implementation readiness.

## 21. Technical-debt updates

- `TD-5D-SEVERITY-THRESHOLD-GENERALIZATION-001`: `RESOLVED_BY_FREQ6`; preserve history until FREQ.7 implements the policy.
- `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001`: unchanged under its current resolved/non-blocking status; FREQ.6 does not silently select missing 5D numeric inputs.
- `TD-KEY-LONG-SPACING-TEST-NEVER-TESTS-REAL-VIOLATION-001`: preserved; FREQ.4A closed KEY↔KEY real coverage, not this distinct history.
- `TD-RUNWAY-ARCHITECTURE-HARDCODED-SINGLE-CELL-001`: preserved.
- `TD-LEGACY-FALLBACK-NO-SILENT-COERCION-001`: preserved if present; fallback rule reinforces it.
- Existing GEN.4C FARTLEK/THRESHOLD prescription-structure gap is reused, not duplicated. Status becomes load-bearing for 5D.

## 22. Final authority map

| Authority | Owns |
|---|---|
| RunLayout | 2 KEY + 2 EASY + 1 LONG structural cardinality in every phase |
| 5D Adaptation Severity Policy | one structural week's effective execution → Reduce/Maintain/Progress |
| B1 | ordinal worst-week aggregation after per-week policy dispatch |
| Distance workout progression | phase-relative primary candidate progression |
| 5D prescription policy | complementary KEY purposes, ordinal identity, relative categorical dose and secondary progression |
| Level | Intermediate eligibility/capability; never structural count |
| Calendar policy | KEY↔KEY and KEY↔LONG placement |
| Taper policy | total taper load and reduced-dose exposure semantics |
| Volume/trajectory policy | starting volume, resolved peak, progression, allocation and long-run numeric authority — currently incomplete for 5D |
| Catalog | actual implementable identities, components, modes, dosage and fallbacks |

No authority is duplicated.

## 23. Implementation blast-radius preview

### REQUIRED after blockers close

- New scoped 5D severity policy and finite-state tests.
- `NextWindowLoadDecisionPolicy` frequency/cardinality policy dispatch and registry/versioning.
- Expected-count/vector invariant validation.
- Internal 5D combination/layout manifest and two-KEY binding/progression policy.
- Dual phase-relative KEY progression/prescription selection.
- Primary/secondary categorical dose representation and session allocation integration.
- Taper two-KEY prescription selection.
- Typed product-ineligible fallback path.
- Complete 8–14, mixed 4D/5D B1, replay, persistence, calendar and numeric tests.

### CATALOG-GAP-DEPENDENT

- Foundation economy/aerobic-strength and threshold-intro definitions.
- Real FARTLEK/THRESHOLD interval/duration/recovery structures.
- Taper 10K sharpening and economy/strides capability.
- Progression artifact capable of two coordinated KEY stages.
- 5D peak-reference/start/allocation/long-run authority artifacts/policies.

### OPTIONAL

- Rename legacy `FourDay*Allocation` types after behavior is stable; naming alone is not authority.

### NO CHANGE EXPECTED

- B1 aggregation, weekly partitioning, safety aggregation, numeric-anchor invocation timing.
- Existing 3D/4D policies.
- Public identity routing; it must remain unchanged.

## 24. Decision-quality check

Severity tiers and pairing purposes were selected as explicit, honestly classified product defaults constrained by canonical rules/evidence direction. No decision was selected because current code made it convenient. Indeed, the selected product policy is stronger than current catalog capacity, and implementation is blocked rather than weakened.

## 25. FREQ.7 gate

FREQ.7 **must not start**. Before internal implementation:

1. Narrow catalog-capability closure must enrich the selected phase purposes and dual-stage prescriptions.
2. A numeric authority closure must select approved 5D starting-volume, resolved-peak/trajectory input, primary/secondary allocation and long-run policy, then compute 8–14 taper eligibility.

Public activation remains out of scope after those closures as well.

## 26. Final classification

Product policy is fully resolved, but a material non-catalog numeric authority architecture is absent, alongside catalog blockers.

`INTERMEDIATE_5D_PRODUCT_POLICY_APPROVED_WITH_ARCHITECTURE_BLOCKER`

Secondary implementation condition: `CATALOG_CAPACITY_BLOCKED`.
