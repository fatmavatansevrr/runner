# Appsel Backend Master Roadmap

This is a **living planning document**, not append-only history. For what actually exists/happened, see `PHASE_LEDGER.md` — this file is never a substitute parent authority. A roadmap label is not a Phase ID until its prompt/report is actually created and ledgered.

---

## 1. Backend V1 Scope

**Distances**: 10K · Half Marathon / 21.1K · Marathon / 42.2K

**Levels**: Beginner · Intermediate · Advanced

**Frequency**: 3D · 4D · 5D · 6D · 7D

- `2D = BACKLOG` unless later separately authorized.
- `Expert = OUT OF V1` unless later separately authorized.

A completed cell is not automatically `PUBLICLY_ACTIVE`. Per §12 (Support-State Vocabulary), a cell may end as `PUBLICLY_ACTIVE`, `GATED`, `PRODUCT_INELIGIBLE`, or `PROVEN_NON_SUPPORT` depending on real evidence/product authority. Not every matrix cell must become public.

---

## 2. Current Canonical State

Sourced from `PHASE_LEDGER.md` only (per this roadmap's own rule — never chat history).

### 10K support matrix (current, repository-verified)

|               | 3D | 4D | 5D | 6D | 7D |
|---|---|---|---|---|---|
| **Beginner** | `PROVEN_NON_SUPPORT` (Core; GEN.5C) | `PUBLICLY_ACTIVE` (Core; GEN.4E) | not yet opened | not yet opened | not yet opened |
| **Intermediate** | `PUBLICLY_ACTIVE` (Core; GEN.3B) | `PUBLICLY_ACTIVE` (pre-existing/Adaptation V1 baseline) | `BLOCKED` — product policy approved (FREQ.6), numeric authority approved (FREQ.6C), catalog architecture design verified (FREQ.6D.1/1A/1B), engineering machinery (schema/projector/RunningApp consumer) implemented through FREQ.6D.3D; **FREQ.6D.4 confirmed blocked** (`FREQ6D4_BLOCKED_ON_PRODUCTION_PROFILE_CONTENT_AUTHORITY`) — zero real WorkoutPrescriptionProfile content exists for any of the 8 required Foundation/Build/RaceSpecific/Taper KEY1/KEY2 slots; **not publicly active** | not yet opened | not yet opened |
| **Advanced** | not yet opened | not yet opened | not yet opened | not yet opened | not yet opened |

Beginner×3D Runway (15-20wk) is separately confirmed non-representable (FREQ.2) with zero live-cell exposure (FREQ.2A) — this is a Runway-horizon finding layered on top of the Core-level `PROVEN_NON_SUPPORT` result above, not a duplicate claim.

### Current active phase / next phase

**Latest verified completed phase**: `FREQ.6D.4B.1` — Execution Status `DONE`, Final Classification `FREQ6D4B1_FULL_PROFILE_MODEL_NON_REPRESENTABLE`. The evidence closure preserves every FREQ.6D.4B main-set decision, inventories all 26 structural components and 36 missing non-main athlete-facing fields, and supplies evidence-supported shared warm-up/cooldown envelopes. It also finds a narrower canonical conflict missed by the prior capability proof: FARTLEK v4/v5's separate `RECOVERY` skeleton row canonically means between-effort recovery, while the newer repeated MAIN_SET already owns and projects that exact recovery. The current exact skeleton/model combination would therefore require an unsupported additional executable component. FC1–FC10 enumerate the complete amendment scope.

**Next phase per repository-backed sequencing**: `FREQ.6D.4B.2` — a full-component product/architecture amendment that closes FC1–FC10, including the FARTLEK structural-versus-nested recovery ownership conflict. `FREQ.6D.4C.3` must not retry production authoring until that decision closes. The DRAFT→VALIDATED/legacy-resolver question remains a separate prerequisite before real publication.

---

## 3. Wave Sequence

```
WAVE A — 10K completion
        ↓
WAVE B — Half Marathon completion
        ↓
WAVE C — Marathon completion
        ↓
WAVE D — Cross-distance backend closure / release readiness
```

**Rule**: do NOT open Half Marathon implementation while 10K closure remains architecturally incomplete. Do NOT open Marathon while Half Marathon distance-generalization remains incomplete. Exceptions require an explicit roadmap/governance decision (recorded in `PHASE_LEDGER.md` as a `GOVERNANCE` phase).

---

## 4. Current Wave

**WAVE A — 10K completion.** Intermediate×5D is the active cell (FREQ chain, mid-implementation per §2). No Half Marathon or Marathon work may begin under this roadmap's own rule until 10K's architectural closure (§25/Wave A milestones) is reached.

---

## 5. Next Concrete Block

Populated from real repository state (§2), not assumption:

1. `FREQ.6D.3D` — RunningApp execution consumer (parent: `FREQ.6D.3C`, `VERIFIED`).
2. `FREQ.6D.4` — dual-KEY progression/runtime integration, 5D severity-table widening (per FREQ.6D.1B's confirmed-required scope), persistence lineage (parent: `FREQ.6D.3D` once it exists; architecturally previewed by FREQ.6D.CP1/6D.1A/6D.1B).
3. `FREQ.6D.5` — persistence/round-trip/full-regression closure (parent: `FREQ.6D.4`).
4. `FREQ.7` — first real Intermediate×5D candidate (parent: `FREQ.6D.5`).
5. `FREQ.8` — 5D activation decision (parent: `FREQ.7`).

None of these have a report yet — they are **not** Phase IDs until created; listed here only as the concrete next block, per §13's rule against pre-authoring fake IDs beyond the near-term horizon.

---

## 6. Future Capability Milestones

See §25 for the full milestone list (no speculative Phase IDs assigned).

---

## 7. Phase Type Taxonomy

| Type | Code | Purpose | Required |
|---|---|---|---|
| **A. EVIDENCE** | NO | Derive evidence envelope; no product selection | Sources/evidence attribution, uncertainty disclosed, no invented defaults |
| **B. DECISION** | NO | Select/freeze domain/product authority from an existing evidence envelope | Decision inventory, internal arithmetic/consistency, exact final classification |
| **C. ARCHITECTURE_DESIGN** | NO | Define contracts/ownership/data flow | Exact type/contract shape, DO NOT list, dependency boundaries, no product decisions hidden as engineering |
| **D. DESIGN_VERIFICATION** | NO | Challenge a design against its own frozen authorities and real code | Fidelity checks, real consumers/data-flow, open-decision audit |
| **E. IMPLEMENTATION** | YES | Implement one frozen contract/policy | Tight allowed scope, explicit DO NOT TOUCH list, tests, regression, file attribution, atomic commit |
| **F. VERIFICATION_CLOSURE** | NO production behavior change by default | Prove claimed implementation actually works | Real tests, failure-path evidence, regression, no invented evidence |
| **G. CHECKPOINT** | NO product/domain implementation | Consolidation/durability/governance | No new decision, repository attribution, commit state |

`GOVERNANCE` is retained as an operational subtype for repo/ledger gates (e.g. this roadmap's own bootstrap phase).

---

## 8. Prompt Construction Standard

Every future phase prompt must begin by declaring:

```
PHASE ID
PHASE TYPE
OBJECTIVE
AUTHORITATIVE PARENTS
ALLOWED SCOPE
FORBIDDEN SCOPE
```

Then include: repository baseline check · authority invariants · exact required work · stop conditions · tests/evidence standard · file attribution · documentation requirement · ledger update · commit boundary · final classifications. Exact content differs by phase type; the skeleton is mandatory.

---

## 9. Batching Rules

**Allowed** when: the structural architecture has already been proven; cells differ only along an already-modeled authority axis; evidence questions are materially the same; one matrix output can preserve per-cell distinctions.

Examples likely allowed later: Advanced 3D/5D/6D/7D evidence matrix after the Advanced anchor + frequency architecture are proven; Intermediate 6D/7D reuse/numeric matrix after the 5D dual-KEY architecture is proven.

**Forbidden** for: first new structural pattern; first second-KEY architecture; first new Distance; first new Level authority; first new persistence/boundary architecture; cells with materially different domain questions.

**Principle**: `FIRST STRUCTURAL INSTANCE = NARROW`. `REPEATED PROVEN PATTERN = MAY BATCH`.

---

## 10. Commit / Push Gates

**Commit hygiene**: every phase must end with an attributable atomic local commit, or an explicitly documented reason it is documentation-only/no-change. Implementation and documentation commits may be separate when useful; `PHASE_LEDGER.md` records both.

**Hard push gates** (mandatory remote-durability checkpoints):

- **A.** At the start of every new Wave.
- **B.** After every block of approximately 10 completed phase prompts.
- **C.** Before starting another Distance.
- **D.** After any major architecture checkpoint where losing local history would cause substantial recovery cost.

A push gate verifies: working-tree attribution · local commit graph · remote/upstream · ahead/behind · no unknown commits · push dry run · actual push · remote SHA == expected local gate SHA. **The next block cannot start until the gate PASSes.**

---

## 11. Parent Validation Rules

Before ANY future phase begins:

1. Read `PHASE_LEDGER.md`.
2. Verify the proposed parent Phase ID exists there.
3. Verify the report link exists.
4. Verify parent provenance is `VERIFIED`.
5. Verify the required parent commit is reachable from current HEAD.
6. Verify no duplicate Phase ID exists.
7. Determine phase type.
8. Read only the relevant authoritative reports.
9. Check the working tree.
10. Only then begin.

If a proposed parent exists only in `MASTER_ROADMAP.md` but not the ledger: **STOP**. Classification: `PARENT_PHASE_NOT_REPOSITORY_VERIFIED`.

---

## 12. Support-State Vocabulary

- **`UNSUPPORTED` / `PROVEN_NON_SUPPORT`** — identity/cell is not supported under the approved policy.
- **`PRODUCT_INELIGIBLE`** — identity is supported but this specific request does not qualify.
- **`GATED`** — internally supported/resolvable but public routing is closed.
- **`PUBLICLY_ACTIVE`** — normal public routing can reach it.

A `DONE` phase does not necessarily mean a `PUBLICLY_ACTIVE` cell (e.g. `FREQ.6D.3C` is `DONE`; Intermediate×5D remains not publicly active).

---

## 13. Roadmap Update Rules

At the end of every phase: `PHASE_LEDGER.md` appends the actual phase result; `MASTER_ROADMAP.md` updates only the planning/status fields affected. Never rewrite historical ledger truth merely to make the roadmap cleaner. If a phase discovers a blocker, the roadmap sequence changes — do not force a previously predicted next prompt merely because it was written earlier.

MASTER_ROADMAP must NOT pre-author speculative phase IDs beyond the near-term block (§5). Future work beyond that is represented as capability milestones (§6/§25), never as invented IDs like "GEN.17B.4" before a real prompt/report exists for it.

---

## 14. Near-term roadmap block (populated from repository audit, `APPSEL-BACKEND.GOV.0`)

Repository evidence (see `PHASE_LEDGER.md` rows 59-65) confirms the chain through the FREQ.6D.4B.1 full-component evidence closure. The real near-term sequence is now:

```
FREQ.6D.4C.2 (DONE)             → IMPLEMENTATION: narrowed WorkoutPrescriptionProfileValidator's
                                    intensity-mode check (M4); added the new capability-overlay
                                    artifact + GOAL_PACE_TEN_K v2 entry (M3); completed
                                    GOAL_PACE_TEN_K v3's DRAFT content. All 8 approved slots now
                                    proven representable and lossless-projecting.
FREQ.6D.4B.1 (DONE)             → EVIDENCE: all full-component fields inventoried; warm-up/cooldown
                                    envelopes established; FARTLEK structural RECOVERY conflicts
                                    with nested-recovery ownership in the current model.
FREQ.6D.4B.2 (next)             → DECISION/ARCHITECTURE AMENDMENT: close FC1-FC10 without reopening
                                    any frozen 4B main-set value.
FREQ.6D.4C.3 (blocked)          → author all 8 WorkoutPrescriptionProfile documents only after 4B.2
[not-yet-scheduled]             → resolve DRAFT->VALIDATED promotion safety against the legacy
                                    highest-non-retired resolver, before any real publication
FREQ.6D.4 (resumed)             → dual-KEY progression/runtime integration, 5D severity-table
                                    widening, persistence lineage, D18 KEY2-floor test
FREQ.6D.5                       → integrated regression closure
FREQ.7                          → first real Intermediate×5D candidate
FREQ.8                          → 5D activation decision
```

Then (capability milestones, no Phase IDs yet):

- 6D/7D evidence/reuse matrix
- 6D/7D numeric/product closure
- 6D/7D implementation/generalization
- Roadmap checkpoint / push gate (Gate B — approaching ~10 completed phase prompts since the last gate; this governance phase itself functions as an out-of-cycle gate per Gate D, see governance report)

---

## 15. Future Milestones — no fake IDs

### WAVE A — 10K

- Finish Intermediate 5D.
- Generalize Intermediate 6D/7D.
- Complete Advanced Level across proven frequencies.
- Complete Beginner remaining frequencies.
- Produce final 10K 15-cell support matrix.
- Full 10K backend regression.
- 10K release-readiness closure.

### WAVE B — HALF MARATHON

- 10K→HM reuse/gap audit.
- HM evidence synthesis.
- HM phase/horizon authority.
- HM numeric authority.
- HM workout capability.
- Intermediate 4D anchor.
- HM frequency matrix.
- Beginner matrix.
- Advanced matrix.
- HM full backend closure.

### WAVE C — MARATHON

- HM→Marathon reuse/gap audit.
- Marathon phase/horizon.
- Long-run/volume/pace/taper authority.
- Intermediate anchor.
- Frequency matrix.
- Beginner matrix.
- Advanced matrix.
- Marathon full backend closure.

### WAVE D — CROSS-DISTANCE

- Distance routing authority audit.
- Catalog graph audit.
- Persistence/replay audit.
- Public API integration.
- Cross-distance regression.
- Production release readiness.

No speculative phase IDs are assigned to any of the above until their own prompt/report is created.
