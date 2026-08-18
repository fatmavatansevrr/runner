# PHASE 10K-FREQ.6D.4B.3 — Intermediate 5D Full-Component Product Decision Closure

## 1. Preflight

- Repository authority read: `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`.
- Repository-backed prerequisites FREQ.6D.4B, FREQ.6D.4B.1, FREQ.6D.4B.2, FREQ.6D.4C.1 and FREQ.6D.4C.2 are `DONE / VERIFIED`.
- FREQ.6D.4B.2 classification is `FREQ6D4B2_ARCHITECTURE_APPROVED_WITH_PRODUCT_REF_AMENDMENT_REQUIRED`; its R1 architecture, FC7/FC8/FC9 disposition, FC10 invariant, exact-reference consequence, implementation manifest and lifecycle blocker were read from the real report.
- FREQ.6D.4B.1's support-component evidence, FC inventory, shared-default findings and accounting limitations were read from its real report.
- FREQ.6D.4C.3 remains unledgered/not started. FREQ.6D.4B.3 did not previously exist.
- Starting HEAD: `160e0aa568d9a625b43e114d2196a5c3a6e21b12`.
- Starting worktree: pre-existing `baseline_tmp` dirt and untracked `.claude/` only; neither is phase-owned.
- Starting `git diff --check`: PASS.
- Decision/documentation only; no production/catalog/runtime change.

## 2. Parent evidence and architecture

FREQ.6D.4B remains authoritative for every main-set value. FREQ.6D.4B.1 supports one phase/lane-invariant quality-session warm-up within 10–15 minutes easy and one cooldown within 5–10 minutes easy; exact points are product defaults, not scientific mandates. FREQ.6D.4B.2 freezes `R1_VERSIONED_FARTLEK_SKELETON_CORRECTION`: corrected executable FARTLEK is WARM_UP → repeated MAIN_SET with nested recovery → COOL_DOWN.

## 3. FC1 — shared warm-up policy

`DECIDED: ONE_SHARED_WARM_UP_POLICY`.

All eight Intermediate×5D profiles use the same warm-up. No phase or Primary/SecondaryControlled exception is supported. Classification: `PRODUCT_DEFAULT_WITHIN_EVIDENCE_SUPPORTED_ENVELOPE`.

## 4. FC2 — exact warm-up quantity

`DECIDED: Continuous, DurationSeconds = 600` (10 minutes).

Ten minutes is inside the evidenced 10–15-minute envelope and is the time-efficient endpoint supported by 4B.1. Duration is preferred to distance because it preserves the evidence unit and gives consistent preparation time across athlete speeds. This is an Appsel product default, not direct scientific authority.

## 5. FC3 — exact warm-up intensity

`DECIDED: IntensityMode = EffortBased; EffortDescriptorKey = "EASY"`.

`EASY` is existing canonical production vocabulary: every selected WorkoutDefinition already assigns WARM_UP the `EASY` intensity descriptor. The profile schema accepts a nonblank descriptor in the mode-matching field; no registry or new string is required. Classification: `APPSEL_CANONICAL_VOCABULARY_REUSE`.

## 6. FC4 — shared cooldown policy

`DECIDED: ONE_SHARED_COOL_DOWN_POLICY`.

All eight profiles use the same cooldown. No phase/lane exception is supported. Classification: `PRODUCT_DEFAULT_WITHIN_EVIDENCE_SUPPORTED_ENVELOPE`.

## 7. FC5 — exact cooldown quantity

`DECIDED: Continuous, DurationSeconds = 300` (5 minutes).

Five minutes is inside the frozen 5–10-minute envelope. The lower endpoint is selected because the evidence for additional active-cooldown benefit is weak, while it preserves the conventional easy transition without adding unsupported load. Duration is deterministic across athlete speeds. Product default, not scientific mandate.

## 8. FC6 — exact cooldown intensity

`DECIDED: IntensityMode = EffortBased; EffortDescriptorKey = "EASY"`.

Warm-up and cooldown share the same genuine easy-effort semantic; component type already distinguishes their role. No `WARMUP_EASY`, `COOLDOWN_EASY` or `PROBE_*` vocabulary is introduced. Classification: `APPSEL_CANONICAL_VOCABULARY_REUSE`.

## 9. FC7 inherited

`FC7 = R1_VERSIONED_FARTLEK_SKELETON_CORRECTION` (`CLOSED`, inherited from FREQ.6D.4B.2). Inter-repetition recovery belongs exclusively to the repeated MAIN_SET.

## 10. FC8 / FC9

- `FC8 = NOT_APPLICABLE`.
- `FC9 = NOT_APPLICABLE`.

The corrected skeleton contains no separate structural RECOVERY, so no quantity or intensity may be authored for one.

## 11. FC10 product-facing closure

`FC10_PRODUCT_POLICY_CLOSED`.

- All eight profiles remain `DistanceAccountingMode = EstimatedSessionTotal`.
- WARM_UP, MAIN_SET work, nested recovery where present, and COOL_DOWN may contribute under canonical downstream accounting semantics.
- Removed legacy structural RECOVERY contributes zero because it is absent.
- A physical recovery effort is visible exactly once through nested recovery and derived cardinality.
- `ACCOUNTING_ARITHMETIC_REMAINS_EXISTING_DOWNSTREAM_AUTHORITY`; no duration→distance, effort→pace or session-kilometer formula is selected here.

## 12. BLD-S exact-reference amendment

`DECIDED: BLD-S WorkoutDefinitionRef FARTLEK v4 → corrected FARTLEK v5`.

Authority: `PRODUCT_REFERENCE_AMENDMENT_REQUIRED_BY_APPROVED_ARCHITECTURE`. All BLD-S main fields remain exactly 10×60 s, 60 s Jog, BetweenRepetitions, EffortBased `SURGE_FASTER_THAN_5K_EFFORT`, SecondaryControlled and EstimatedSessionTotal.

## 13. TAP-S preservation

TAP-S remains pinned to corrected DRAFT FARTLEK v5. No v6 is created. Its frozen 6×20 s / 100 s Walk nested recovery remains unchanged. Removing the duplicate `RECOVERY / EASY_JOG` row eliminates the former Jog-versus-Walk contradiction.

## 14. Other exact references

`UNCHANGED`:

- FND-P: AEROBIC_STRENGTH_CONTROLLED_INTRO v3
- FND-S: THRESHOLD_TEMPO v5
- BLD-P: THRESHOLD_TEMPO v4
- RS-P: GOAL_PACE_TEN_K v2 plus its exact capability overlay
- RS-S: THRESHOLD_TEMPO v4
- TAP-P: GOAL_PACE_TEN_K v3

No automatic version upgrade is authorized.

## 15. Shared support policy

Every one of the eight distinct profile documents reuses these values:

| Component | Structure | Work | Intensity |
|---|---|---|---|
| WARM_UP | Continuous | 600 seconds | EffortBased / `EASY` |
| COOL_DOWN | Continuous | 300 seconds | EffortBased / `EASY` |

Value reuse does not imply document reuse; the eight profiles remain distinct by exact reference, main set, dose category and phase purpose.

## 16. Phase and lane invariance

The support policy applies unchanged across Foundation, Build, RaceSpecific and Taper and across Primary and SecondaryControlled. Taper main-set reduction does not reduce support components. Adherence severity and main-set dose remain separate authorities.

## 17. Final eight-profile product-authority matrix

All rows use `EstimatedSessionTotal`. `WU` always means Continuous 600 s, EffortBased `EASY`; `CD` always means Continuous 300 s, EffortBased `EASY`.

| Slot | Exact reference | Ordered executable components | Dose category |
|---|---|---|---|
| FND-P | AEROBIC_STRENGTH_CONTROLLED_INTRO v3 | WU; MAIN Repeated 30 s ×6, EffortBased `CONTROLLED_AEROBIC_POWER_INTRO`, recovery 90 s Jog BetweenRepetitions; CD | Primary |
| FND-S | THRESHOLD_TEMPO v5 | WU; MAIN Continuous 1200 s, EffortBased `CONTROLLED_THRESHOLD_INTRO`; CD | SecondaryControlled |
| BLD-P | THRESHOLD_TEMPO v4 | WU; MAIN Continuous 2400 s, PaceBased `THRESHOLD_PACE`; CD | Primary |
| BLD-S | **corrected FARTLEK v5** | WU; MAIN Repeated 60 s ×10, EffortBased `SURGE_FASTER_THAN_5K_EFFORT`, recovery 60 s Jog BetweenRepetitions; CD | SecondaryControlled |
| RS-P | GOAL_PACE_TEN_K v2 | WU; MAIN Continuous 1200 s, PaceBased `GOAL_PACE_TEN_K`; CD | Primary |
| RS-S | THRESHOLD_TEMPO v4 | WU; MAIN Continuous 1500 s, PaceBased `THRESHOLD_SUPPORT_PACE`; CD | SecondaryControlled |
| TAP-P | GOAL_PACE_TEN_K v3 | WU; MAIN Continuous 600 s, PaceBased `GOAL_PACE_TEN_K`; CD | Primary |
| TAP-S | corrected FARTLEK v5 | WU; MAIN Repeated 20 s ×6, EffortBased `CONTROLLED_STRIDES_SHARPENING`, recovery 100 s Walk BetweenRepetitions; CD | SecondaryControlled |

Every component has exact SequenceOrder by table order, ComponentType, StructureMode, work unit/value and typed intensity. Every repeated main also has exact repetition count, recovery unit/value, mode and placement. There is no structural RECOVERY row.

## 18. FC1–FC10 final table

| ID | Status | Final authority | Evidence class | Implementation consequence |
|---|---|---|---|---|
| FC1 | DECIDED | one shared warm-up | supported shared default | reuse across 8 |
| FC2 | DECIDED | Continuous 600 s | product default within evidence envelope | exact work quantity |
| FC3 | DECIDED | EffortBased / `EASY` | canonical vocabulary reuse | exact typed intensity |
| FC4 | DECIDED | one shared cooldown | supported shared default | reuse across 8 |
| FC5 | DECIDED | Continuous 300 s | product default within evidence envelope | exact work quantity |
| FC6 | DECIDED | EffortBased / `EASY` | canonical vocabulary reuse | exact typed intensity |
| FC7 | CLOSED | R1 skeleton correction | approved architecture | correct DRAFT source skeletons |
| FC8 | NOT_APPLICABLE | no structural recovery quantity | R1 consequence | author none |
| FC9 | NOT_APPLICABLE | no structural recovery intensity | R1 consequence | author none |
| FC10 | CLOSED | EstimatedSessionTotal retained; nested recovery counted once; arithmetic downstream | frozen architecture/product policy | no duplicate component/arithmetic invention |

## 19. D1–D18 + FC1–FC10 completeness

D1–D18 and FC1–FC10 together supply every athlete-facing value needed for all eight full profiles. D18 remains its previously classified engineering regression item, not an athlete-facing choice. The implementation agent has no discretion over quantity, unit, descriptor, intensity mode, exact WorkoutDefinition reference, recovery representation or accounting mode.

Classification: `NO_IMPLEMENTATION_PRODUCT_DECISION_REMAINS`.

## 20. Descriptor-vocabulary result

Option A: existing descriptor reused. `EASY` appears in the exact selected WorkoutDefinition WARM_UP/COOL_DOWN components and is a production catalog semantic, not a test fixture. `EffortBased` selects `EffortDescriptorKey`; the current schema requires only one nonblank mode-matching descriptor and can encode `EASY` losslessly. No new vocabulary authority or schema change is required.

## 21. Numeric compatibility

Classification: `COMPATIBLE`.

The selected 10-minute warm-up and 5-minute cooldown lie inside FREQ.6D.4B.1's already-checked envelopes and do not alter FREQ.6C's upstream weekly trajectory/allocation. Duration-based components require no invented kilometer conversion. Exact conversion remains downstream; no numeric contradiction was found.

## 22. Downstream implementation manifest

Required order:

1. Correct DRAFT FARTLEK v5 to WARM_UP → MAIN_SET → COOL_DOWN.
2. Contain/correct or supersede conflicting DRAFT FARTLEK v3 and AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1/v2 before executable profile use, per 4B.2.
3. Update WorkoutDefinition validation so corrected new/DRAFT FARTLEK uses the three-row skeleton without invalidating historical validated v4; add deterministic duplicated-ownership failures.
4. Apply BLD-S exact-reference amendment to corrected v5 in the appropriate source/decision manifest; preserve all other refs.
5. Add no-double-count, nested recovery, immutable-history, exact-reference and all-eight representability tests.
6. Re-run full catalog validation/regression.
7. Only then run FREQ.6D.4C.3 production profile authoring with WU=600 s EASY and CD=300 s EASY.

No production implementation is performed in this phase.

## 23. Lifecycle blocker

`CATALOG_LIFECYCLE_BLOCKER_REMAINS_OPEN_BEFORE_6D4D`.

Corrected FARTLEK v5 remains DRAFT and subject to the existing DRAFT→VALIDATED versus highest-non-retired resolver risk. This does not block the narrow skeleton/reference implementation or DRAFT profile content authoring, but it blocks publication/6D.4D until separately resolved.

## 24. Next phase and final classification

Next phase type: **IMPLEMENTATION** — narrow corrected-DRAFT-skeleton, validator containment, BLD-S reference amendment and no-double-count regression implementation. FREQ.6D.4C.3 remains blocked until it passes.

Final classification: **`FREQ6D4B3_PRODUCT_POLICY_APPROVED_WITH_BLD_S_V5_REFERENCE_AMENDMENT`**.

Decision gate: FC1–FC10 complete; all eight profiles have full athlete-facing authority; no support number/descriptor/structural recovery value remains open.

### Gate B durability result

Completion reached the roadmap's approximately-ten threshold. After explicit user approval, the phase/governance chain was pushed without force from prior remote `244a154` through local gate SHA `0bc70c5`. A fresh fetch/prune then proved local HEAD = `origin/main` = remote `refs/heads/main` at `0bc70c5f181bfd6888c4c3d62c8391441c92923c`, with ahead/behind `0/0`. Pre-existing `baseline_tmp` dirt remained attributed and uncommitted. Classification: `PUSH_GATE_PASS`.
