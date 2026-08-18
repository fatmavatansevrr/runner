# PHASE 10K-FREQ.6D.4B.4 — Repeated-Workout Recovery Ownership Implementation

## 1. Preflight and Gate B baseline

- Repository authorities read: `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, FREQ.6D.4B.2 and FREQ.6D.4B.3 reports.
- Required parents FREQ.6D.4B.1, 4B.2, 4B.3, 4C.1 and 4C.2 were repository-backed `DONE / VERIFIED`.
- FREQ.6D.4B.3 classification: `FREQ6D4B3_PRODUCT_POLICY_APPROVED_WITH_BLD_S_V5_REFERENCE_AMENDMENT`.
- Gate B durable baseline: local HEAD, `origin/main` and remote `refs/heads/main` all contained `dc0f4a03fd403607599781d88e9ee3c154429321`; starting divergence was 0 ahead / 0 behind.
- Parent/starting HEAD: `dc0f4a03fd403607599781d88e9ee3c154429321`, branch `main`.
- Pre-existing worktree only: modified `baseline_tmp`, untracked `.claude/`; both preserved and excluded.
- Initial and final `git diff --check`: PASS.

## 2. Authority implemented

R1 was implemented without changing profile schema, execution contracts, projector, runtime, persistence, adaptation or public routing. A repeated MAIN_SET exclusively owns `RecoveryQuantity`, `RecoveryMode`, `RecoveryPlacement` and derived `RecoveryCount`. The same between-repetition effort is no longer duplicated by a structural RECOVERY row in newly authored affected definitions.

FC7 is implemented; FC8 and FC9 remain `NOT_APPLICABLE`; FC10's no-double-count invariant is structurally enforced. No new product or dosage decision was introduced.

## 3. WorkoutDefinition corrections and containment

- FARTLEK v5 DRAFT is now exactly WARM_UP, MAIN_SET, COOL_DOWN.
- FARTLEK v3 DRAFT was corrected to the same three-row ownership shape (`CORRECTED`).
- AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1 and v2 DRAFT were corrected to the same three-row ownership shape (`CORRECTED`).
- FARTLEK v4 remains VALIDATED, four-row, byte/content unchanged, with original metadata, eligibility and component order. `FARTLEK_V4_HISTORICAL_IMMUTABILITY_PRESERVED`.
- No FARTLEK v6 was created.

Generated pilot-domain audit artifacts were refreshed only to reflect the corrected DRAFT inventory; the v4 historical entry remains four-row.

## 4. Validator architecture and historical boundary

`WorkoutDefinitionValidator` now uses lifecycle status as the generic authoring/replay boundary:

- DRAFT FARTLEK and DRAFT AEROBIC_STRENGTH_CONTROLLED_PROGRESSED require WARM_UP, MAIN_SET, COOL_DOWN.
- A DRAFT affected definition carrying the legacy structural marker emits deterministic error `WD_RECOVERY_OWNERSHIP_DUPLICATED`.
- Non-DRAFT historical FARTLEK accepts the recognized current three-row or historical four-row exact skeleton, preserving replay without a key/version magic bypass.
- Arbitrary future shapes remain rejected; compatibility does not weaken DRAFT authoring.
- RECOVERY remains in the shared vocabulary and a genuine independent recovery block is not globally prohibited.

Existing exact-sequence validation continues to fail closed for invalid order, missing MAIN_SET, duplicate component types and ambiguous structures; there is no first-match recovery selection.

## 5. Exact-reference amendment and representability

The authoritative eight-slot test input now points BLD-S from FARTLEK v4 to corrected v5. TAP-S remains FARTLEK v5. BLD-S dosage remains 10 × 60 seconds with 60-second Jog between repetitions; TAP-S remains 6 × 20 seconds with 100-second Walk between repetitions.

All eight full fixtures use WARM_UP 600 seconds EffortBased/EASY and COOL_DOWN 300 seconds EffortBased/EASY. No `PROBE_*` value occurs in these eight fixtures. Each validates, projects losslessly and passes the executable boundary validator.

BLD-S and TAP-S each project exactly three components. MAIN_SET retains nested recovery and derived `RecoveryCount = repetitions - 1`; no structural RECOVERY is projected. This proves single authority and no double counting. Capability-overlay behavior remains functional.

## 6. Regression evidence

- Targeted validator/profile/capability tests: 52 passed, 0 failed, 0 skipped.
- Full PlanCatalog suite: 1,396 passed, 0 failed, 0 skipped.
- Release build: PASS, 0 warnings, 0 errors.
- Historical FARTLEK, catalog consistency, bundle/hash, highest-version resolution and existing 3D/4D behavior are covered by the full green suite.
- Profile schema/projector and legacy resolver have zero production delta.

## 7. File attribution

- `WORKOUT_DEFINITION_DRAFT_CONTENT`: four corrected DRAFT workout JSON files.
- `WORKOUT_DEFINITION_VALIDATOR`: narrow lifecycle-aware ownership validation.
- `REFERENCE_AMENDMENT`: BLD-S v5 exact test authority; TAP-S v5 preserved.
- `TEST`: validator, catalog, exact-fixture, projection and historical compatibility regressions.
- `DOCUMENTATION/AUDIT`: refreshed pilot-domain audit outputs and this report.
- `LEDGER/ROADMAP`: append-only phase truth and next-step update.
- `UNEXPECTED`: none. User-owned `baseline_tmp` and `.claude/` remain untouched.

## 8. Commits, lifecycle and readiness

- Atomic implementation commit: `1b63f83` (`feat(catalog): correct repeated-workout recovery ownership`).
- Report/ledger/roadmap are committed separately as governance.
- First completed phase in the new post-Gate-B block; no push gate is reached and no push is performed.
- `CATALOG_LIFECYCLE_BLOCKER_REMAINS_OPEN_BEFORE_6D4D`.
- FREQ.6D.4C.3 is ready to author the eight production profiles. FREQ.6D.4D must still wait for lifecycle/resolver closure.

## 9. Final classification

`FREQ6D4B4_IMPLEMENTED_CATALOG_LIFECYCLE_BLOCKER_REMAINS`
