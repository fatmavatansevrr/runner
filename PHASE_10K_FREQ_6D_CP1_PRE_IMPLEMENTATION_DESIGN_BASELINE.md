# PHASE 10K-FREQ.6D.CP1 — Pre-Implementation Design Baseline

## Checkpoint meaning

The Intermediate × 5D generic prescription / dual-KEY design contract is frozen and verified. Implementation has not begun.

Accepted design state:

- `SEVERITY_TABLE_FIDELITY_CONFIRMED`
- `OPEN_ITEMS_ENGINEERING_ONLY_CONFIRMED`
- `FREQ6D_DESIGN_CAN_PROCEED_TO_6D2_6D3`
- The six-entry severity dispatch is outer count dispatch only; count 4 remains role-aware.
- Model A2 remains rejected.
- Repair/substitution contains no open product decision.
- Structured/controlled FARTLEK is already required by FREQ.6.
- `LaneOrdinal 0 → PRIMARY`; `LaneOrdinal 1 → SECONDARY_CONTROLLED`.
- Primary/secondary remain prescription semantics, not structural roles.
- `LaneOrdinal ↔ DoseCategory` requires publish-time validation.
- FREQ.6D.2 and FREQ.6D.3 are product/domain-unblocked.
- FREQ.6D.4 waits only for implementation prerequisites, not product decisions.

## Repository inventory and attribution

Before checkpoint, the relevant top-level status consisted of three untracked FREQ.6D documents and a dirty `baseline_tmp` gitlink. A sandbox-only status read also exposed the ignored local `.claude/` directory; it is unrelated local configuration and is excluded.

| Path | Attribution | Checkpoint action |
|---|---|---|
| `PHASE_10K_FREQ_6D_1_PRESCRIPTION_PROFILE_DESIGN.md` | `DESIGN_DOCUMENTATION` | Include |
| `PHASE_10K_FREQ_6D_1A_GENERIC_PRESCRIPTION_DUAL_KEY_DESIGN_EVIDENCE_CLOSURE.md` | `EVIDENCE_DOCUMENTATION` | Include |
| `PHASE_10K_FREQ_6D_1B_SEVERITY_TABLE_FIDELITY_AND_OPEN_DECISION_CHECK.md` | `EVIDENCE_DOCUMENTATION` / `GOVERNANCE_ARTIFACT` | Include |
| `baseline_tmp` | `UNRELATED` pre-existing dirty sub-repository; recorded gitlink remains unchanged | Exclude |
| `.claude/` | `UNRELATED` ignored local tooling configuration | Exclude |

No `UNKNOWN` file is included.

## Implementation-leak audit

No FREQ.6D.1/1A/1B-attributable production or test implementation is present or included. In particular, the checkpoint contains no:

- `PrescriptionProfile` production types;
- schema or validator implementation;
- binder/runtime implementation;
- dual-KEY progression implementation;
- `TrainingDay` persistence columns or migration;
- adaptation severity runtime change;
- public routing change.

The only non-document working-tree state is the unrelated dirty `baseline_tmp` gitlink, whose recorded parent-repository commit pointer is unchanged. Classification: `PRE_EXISTING_UNRELATED_CHANGE`, not an implementation leak.

## Binding implementation gates

This CP1 table is the authoritative phase-boundary interpretation. Any earlier design discussion that grouped work differently is superseded by this table.

| Phase | Included | Explicitly excluded |
|---|---|---|
| FREQ.6D.2 | Schema, typed contracts, deserialization, source validation, publish-time invariants | Binder, dual-KEY runtime progression, adaptation integration, public routing |
| FREQ.6D.3 | `PrescriptionProfile` binding and materialization | Dual-lane progression scheduling, adaptation severity, public routing |
| FREQ.6D.4 | Dual-KEY progression/runtime integration, 5D severity-table widening, required persistence lineage, runtime integration of frozen lane semantics | Public activation unless separately authorized |
| FREQ.6D.5 | Persistence/round-trip/full-regression closure | New product-policy decisions |

FREQ.6D.2 and FREQ.6D.3 are design-parallel safe. For code integration, FREQ.6D.3 consumes or rebases onto the real FREQ.6D.2 contracts. It must not create temporary duplicate `PrescriptionProfile` DTOs or stubs.

## Hygiene evidence

- Pre-stage `git diff --check`: PASS.
- Staged `git diff --check`: PASS.
- No focused backend test was required because no production or test code is part of this checkpoint.
- Unrelated working-tree state was left unstaged.

## Classification

`FREQ6D_PRE_IMPLEMENTATION_BASELINE_COMMITTED`
