# PHASE 10K-FREQ.6D.4B.2 — FARTLEK Recovery Ownership and Full-Component Profile Architecture

## 1. Preflight

- `PHASE_LEDGER.md` and `MASTER_ROADMAP.md` were read as repository authority.
- FREQ.6D.4B, FREQ.6D.4B.1, FREQ.6D.4C.1 and FREQ.6D.4C.2 are repository-backed `DONE / VERIFIED`.
- FREQ.6D.4B.1's real report records 26 components, 36 missing non-main authority fields, the three-part recovery taxonomy, FC1–FC10 and `FREQ6D4B1_FULL_PROFILE_MODEL_NON_REPRESENTABLE`.
- FREQ.6D.4C.3 remains absent from the ledger and report set. FREQ.6D.4B.2 did not previously exist.
- Starting HEAD: `126c4cf47cf3da67cf5cae7c0095e82ea84f038d`.
- Starting worktree contained only pre-existing `baseline_tmp` dirt and untracked `.claude/`; neither is phase-owned.
- Starting `git diff --check`: PASS.
- This phase changes documentation/governance only.

## 2. Parent evidence

FREQ.6D.4B main-set authorities remain frozen, including FND-P 6×30 s with its approved nested recovery, BLD-S 10×60 s with 60 s Jog `BetweenRepetitions`, and TAP-S 6×20 s with 100 s Walk `BetweenRepetitions`. Structure, intensity, dose category and `ESTIMATED_SESSION_TOTAL` remain binding.

FREQ.6D.4B.1 proved that the current exact FARTLEK v4/v5 skeleton forces an additional positive RECOVERY component even though the repeated MAIN_SET already owns the same inter-effort recovery. Warm-up/cooldown product defaults remain open.

## 3. Canonical recovery semantics

`domain-wave2-component-vocabulary.md`, `ten-k-pilot-domain-decision-audit.md/json` and `PilotDomainContentAudit` all agree: FARTLEK's structural `RECOVERY / EASY_JOG` represents recovery segments **between variable efforts**, not a post-set block. The decision audit classifies the skeleton as an explicit product default and explicitly assigns no separate duration, distance, pace or repetition values.

No later authority supersedes that meaning. FREQ.6D.3A.1 and the executable contract instead introduced the more precise representation: recovery quantity/mode/placement on a Repeated component, with derived cardinality.

Classification: `CANONICAL_BETWEEN_EFFORT_RECOVERY`.

## 4. STRUCTURAL_RECOVERY_USAGE_INVENTORY

A complete parse of real `catalog/workouts/*.json` found five structural RECOVERY occurrences:

| WorkoutDefinition | Status | Sequence | Finding |
|---|---|---|---|
| FARTLEK v3 | DRAFT | WARM_UP → MAIN_SET → RECOVERY/EASY_JOG → COOL_DOWN | legacy between-effort marker |
| FARTLEK v4 | VALIDATED | same | immutable historical artifact; BLD-S currently pins it |
| FARTLEK v5 | DRAFT | same | TAP-S pins it; can be corrected before validation |
| AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1 | DRAFT | same | same generic repeated-work modeling risk |
| AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v2 | DRAFT | same | same generic repeated-work modeling risk |

FARTLEK v1/v2 already use WARM_UP → MAIN_SET → COOL_DOWN. No other real WorkoutDefinition has structural RECOVERY. The issue is not unique to 5D or FARTLEK; it is a generic mismatch between legacy semantic-marker skeletons and the newer executable repeated-work ownership model.

## 5. Root model conflict

WorkoutDefinition components are presently a mixture:

- WARM_UP, MAIN_SET and COOL_DOWN act as executable structural blocks when a profile is attached;
- RECOVERY in the affected definitions was authored as a domain marker for recovery *inside* repeated work;
- the current profile validator assumes every skeleton item is an independently executable block with positive work and intensity;
- the projector preserves every profile component and cannot merge meanings.

For the canonical between-effort concept, the correct root representation is **B: nested recovery metadata on the Repeated MAIN_SET**. A separate true post-set recovery remains valid only when a future definition explicitly gives it an independent semantic purpose; it must not be inferred from the generic name `RECOVERY`.

## 6. R1 — versioned skeleton correction

**Approve.** Correct affected authoring targets to WARM_UP → MAIN_SET → COOL_DOWN and keep inter-repetition recovery exclusively on repeated MAIN_SET.

- Semantic fidelity: exact.
- Historical immutability: FARTLEK v4 remains unchanged; DRAFT artifacts may be corrected before validation under existing repository precedent.
- Exact references: TAP-S can retain corrected-in-place DRAFT FARTLEK v5; BLD-S cannot remain on immutable v4 and requires a narrow v4→v5 product-reference amendment.
- Schema/projector/runtime/persistence: no contract change. Existing nested recovery projects losslessly.
- Generality: the same rule applies to intervals, hill repetitions and race-specific repeated sets.
- Complexity: smallest durable change; it fixes source semantics instead of teaching every downstream layer a legacy alias.

## 7. R2 — non-executable structural marker

**Reject.** It preserves exact references but makes `WorkoutComponentType` polymorphic without explicit metadata, relaxes mandatory work/intensity, breaks the current one-row/one-output invariant and requires validator/projector/contract changes. Inferring marker status from the name `RECOVERY` would be heuristic and would prevent a genuine post-set RECOVERY block.

## 8. R3 — explicit profile binding/link

**Reject.** An explicit link could preserve lineage and single execution authority, but adds reference topology, target identity, cycle/ambiguity checks, schema versioning and projection elision solely to accommodate legacy skeletons. It aliases one semantic across two source objects and is unjustified when a corrected exact WorkoutDefinition is available.

## 9. R4 — dual executable recovery

**Reject.** No independent canonical purpose exists. It produces nine nested recoveries plus one extra block for BLD-S and five plus one for TAP-S; `AfterEachRepetition` would be even more duplicative. It also conflicts with TAP-S's frozen Walk versus skeleton `EASY_JOG`.

## 10. R5 — remove nested recovery

**Reject.** It reopens FREQ.6D.3A.1/FREQ.6D.2A, loses placement and derived cardinality, cannot express repeated recovery with one unique structural component row and conflicts directly with frozen 4B prescriptions and the execution contract.

## 11. FARTLEK_RECOVERY_OWNERSHIP_OPTION_MATRIX

| Option | Canonical fidelity | Single authority | Historical immutability | 4B refs | Profile schema | Projection | Runtime/persistence | Future generality | Failure semantics | Complexity | Recommended |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1 corrected version/skeleton | full | yes, MAIN_SET nested | preserves validated v4 | BLD amendment; TAP preserved | no change | no change | no behavior contract change | strong | existing strict validation plus corrected skeleton | low | **yes** |
| R2 marker | full if explicitly modeled | potentially | preserves all | preserved | major optional/marker shape | elision/binding required | contract lineage changes | ambiguous for true blocks | many new marker failures | high | no |
| R3 explicit link | full | yes | preserves all | preserved | new link/reference union | linked projection required | provenance/serialization changes | coherent but overbuilt | target/cycle/cardinality failures | very high | no |
| R4 dual executable | false | no | preserves all | preserved | none | duplicate output | duplicate execution/accounting | harmful precedent | cannot detect semantic duplicate | low code/high domain risk | no |
| R5 structural only | false against frozen model | weak | preserves all | main-set amendment | major repeated-work change | cardinality redesign | broad breaking impact | poor | missing placement/count | very high | no |

No alternative outperforms R1.

## 12. Selected architecture

`R1_VERSIONED_FARTLEK_SKELETON_CORRECTION` is selected.

Architectural rule:

> A recovery effort occurring between repetitions is owned only by the Repeated executable component through RecoveryQuantity, RecoveryMode, RecoveryPlacement and derived RecoveryCount. A WorkoutDefinition intended for that representation must not include a second structural RECOVERY row for the same semantic. A genuinely independent recovery block may exist only when the definition explicitly establishes a distinct executable purpose.

## 13. Single-authority map

| Datum | Sole authority |
|---|---|
| inter-repetition work/recovery quantity | `MAIN_SET.RecoveryQuantity` |
| mode | `MAIN_SET.RecoveryQuantity.Mode` |
| placement | `MAIN_SET.RecoveryPlacement` |
| count | `PrescriptionRecoveryCardinality.Derive(repetitionCount, placement)` |
| support-component work/intensity | each genuine WARM_UP/COOL_DOWN component |
| true future post-set block | its own explicitly distinct component, never an alias of nested recovery |

No profile may declare `RECOVERY.WorkQuantity` for the same effort.

## 14. Structural-lineage semantics

Execution continues to require exactly one output component per component in the **corrected exact WorkoutDefinition**. It does not need to retain a removed legacy marker because a new/corrected source version is itself the structural lineage authority; immutable v4 remains available as historical evidence.

Allowed projection relationship remains identity-preserving: WorkoutDefinition skeleton row ↔ profile component row ↔ executable component row. Nested recovery is subordinate data of its repeated component, not a separate row. The projector must never infer this from a component name.

## 15. Generalization and future interval impact

The rule applies identically to fartlek, intervals, repetitions, hill repetitions and race-specific repeated sets. Between-rep recovery belongs to the repeated component. A future workout may contain a genuinely separate recovery block after a set only if its WorkoutDefinition uses explicit, independently evidenced semantics and its profile supplies independent executable work; such a block must not duplicate nested recovery.

The two AEROBIC_STRENGTH_CONTROLLED_PROGRESSED DRAFT definitions must be brought under this rule before any profile authoring/validation that treats their recovery as between-repetition recovery. This is implementation containment, not a new athlete-facing decision.

## 16. Profile-schema consequence

No WorkoutPrescriptionProfile schema/model change is required. The current required `workQuantity`, typed intensity, repeated recovery fields, exact skeleton matching and deterministic projector are correct once the source skeleton represents executable blocks rather than a legacy marker.

## 17. WorkoutDefinition-version consequence

- FARTLEK v4: immutable and unchanged.
- FARTLEK v5: DRAFT; correct its skeleton in place to WARM_UP → MAIN_SET → COOL_DOWN before validation. This follows the existing rule that DRAFT content may be completed/corrected before publication.
- FARTLEK v3: historical DRAFT candidate not used by these profiles; do not silently rewrite without an explicit implementation-scope inventory. It must never be used for executable profile authoring in its conflicting shape.
- AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1/v2: DRAFT; correct or supersede before profile use.
- `WorkoutDefinitionValidator`: its schemaVersion≥2 FARTLEK exact-sequence rule must change to the corrected three-component sequence.

No new FARTLEK v6 is required for the selected minimal path; v5 already has both BUILD and TAPER eligibility and is still DRAFT.

## 18. 4B exact-reference consequence

- **BLD-S → FARTLEK v4 cannot remain.** Required product amendment: BLD-S → corrected FARTLEK v5. No main-set field changes.
- **TAP-S → FARTLEK v5 remains.** Its exact reference is preserved; only the still-DRAFT referenced content is corrected before validation.

This architecture phase approves the need but does not perform the product-reference amendment.

## 19. Lifecycle interaction

Correcting DRAFT v5 does not solve its DRAFT→VALIDATED interaction with the legacy highest-non-retired resolver. That independent blocker remains load-bearing before FREQ.6D.4D/publication. Repointing BLD-S to v5 increases the importance of resolving that lifecycle boundary but does not change its nature. No resolver change is selected here.

## 20. FC7 closure

`FC7 = R1_VERSIONED_FARTLEK_SKELETON_CORRECTION`.

- Authority: nested recovery on repeated MAIN_SET only.
- Versioning: validated v4 immutable; correct DRAFT v5; address other conflicting DRAFT definitions before use.
- Schema: profile schema unchanged; WorkoutDefinition validation rule corrected.
- 4B refs: BLD-S v4→v5 amendment required; TAP-S v5 preserved.
- Downstream: implement catalog/validator/test changes before authoring profiles.

## 21. FC8 / FC9 disposition

- `FC8 = NOT_APPLICABLE`.
- `FC9 = NOT_APPLICABLE`.

No separate executable structural recovery remains, so no unsupported quantity or intensity may be selected.

## 22. FC10 no-double-count invariant

Architectural portion frozen:

> The same physical recovery effort contributes to executable output and distance accounting exactly once. For between-repetition recovery, only the nested recovery on the Repeated component is visible to execution/accounting, with count derived from placement. No structural RECOVERY work row or linked duplicate is emitted.

This phase selects no duration-to-distance arithmetic; the canonical downstream accounting owner remains unchanged. FC10 is closed for ownership/no-double-count structure and remains open only insofar as the subsequent product/implementation phase must verify canonical total-accounting behavior.

## 23. Implementation manifest

| Area | Classification | Required future work |
|---|---|---|
| WORKOUT_DEFINITION_VERSION | change | correct DRAFT FARTLEK v5 skeleton; inventory/correct or supersede conflicting DRAFT FARTLEK v3 and aerobic-progressed v1/v2 before executable use; never mutate v4 |
| PROFILE_SCHEMA | `NO_CHANGE` | none |
| PROFILE_MODEL | `NO_CHANGE` | none |
| PROFILE_VALIDATOR | `NO_CHANGE` | exact skeleton match remains correct |
| WORKOUT_DEFINITION_VALIDATOR | change | require corrected three-row FARTLEK skeleton for applicable new/DRAFT content without invalidating immutable historical reads |
| PROJECTOR | `NO_CHANGE` | nested recovery remains sole executable recovery |
| CATALOG_GRAPH | reference change | after product approval, BLD-S profile pins v5; exact graph validation remains |
| TESTS | change | corrected skeleton, immutable v4, nested-only projection, no-double-count, conflicting legacy-definition rejection/containment, BLD/TAP exact refs |
| RUNTIME | `NO_CHANGE` | consumes the unchanged executable contract |
| PERSISTENCE | `NO_CHANGE` | no contract field change |

Implementation must account for historical validation: global validation must not reinterpret immutable v4 as if newly authored. The implementation phase must choose an explicit version/schema/status gate or historical-compatibility mechanism, never weaken validation silently.

## 24. Failure semantics

The eventual implementation must fail closed with deterministic codes for:

- a corrected/new repeated WorkoutDefinition that declares both a between-effort structural RECOVERY marker and a nested-recovery-capable MAIN_SET: `WD_RECOVERY_OWNERSHIP_DUPLICATED`;
- profile skeleton mismatch: existing `PROFILE_COMPONENT_SKELETON_MISMATCH`;
- repeated MAIN_SET missing nested recovery: existing `PROFILE_REPEATED_RECOVERY_REQUIRED` / placement-required error;
- multiple MAIN_SET candidates where a binding would be ambiguous: reject at WorkoutDefinition validation; no heuristic target selection;
- a claimed structural marker without a repeated target: reject; R2/R3 are not supported representations;
- a separate executable recovery with the same semantic owner: reject as duplicated ownership;
- a future true post-set recovery lacking explicit distinct semantic metadata/authority: reject rather than infer purpose.

Because R1 has no marker/link, target-reference, link-cycle and multi-target resolution logic must not be added.

## 25. FREQ.6D.4B.3 readiness

Ready. FC7 is closed, FC8/FC9 are not applicable, the no-double-count invariant is frozen and the architecture is deterministic. FREQ.6D.4B.3 may select only the ordinary product defaults FC1–FC6 and approve the narrow BLD-S v4→v5 exact-reference amendment. It must not invent structural recovery work.

## 26. FREQ.6D.4C.3 dependency chain

FREQ.6D.4C.3 remains not ready. Required sequence:

1. FREQ.6D.4B.3: select FC1–FC6; approve BLD-S v4→corrected v5; finish any product-facing portion of FC10.
2. Dedicated implementation: correct DRAFT WorkoutDefinition skeletons/validator/tests and apply the approved exact-reference consequence without mutating validated v4.
3. Verify all eight exact full profiles are representable and no recovery is counted twice.
4. Author the eight production profiles in FREQ.6D.4C.3.
5. Separately resolve DRAFT→VALIDATED/highest-non-retired resolver safety before FREQ.6D.4D/public activation.

## 27. Final classification

Primary: **`FREQ6D4B2_ARCHITECTURE_APPROVED_WITH_PRODUCT_REF_AMENDMENT_REQUIRED`**.

Also applicable: `FREQ6D4B2_ARCHITECTURE_APPROVED_IMPLEMENTATION_REQUIRED`.

The recovery architecture is fully resolved; it is not evidence-blocked and is representable after the selected source-skeleton correction. 4B main-set policy remains valid. Push-gate count reaches approximately nine completed phase prompts since the last durability gate, below the roadmap's approximately-ten threshold: `PUSH_GATE_NOT_REACHED`.
