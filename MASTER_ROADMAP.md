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
| **Intermediate** | `PUBLICLY_ACTIVE` (Core; GEN.3B) | `PUBLICLY_ACTIVE` (pre-existing/Adaptation V1 baseline) | `PUBLICLY_ACTIVE` (Core, 8-14 weeks; FREQ.6D.4D.5G) and `PUBLICLY_ACTIVE` (Preparation Runway, 15-20 weeks; FREQ.6D.8) for all readiness states including missing/explicit-zero (`FREQ.6D.10`). **Long-Horizon (21-52w) 5D remains `BLOCKED`** — its complete dual-KEY lineage/JIT/persistence/execution-context architecture is now designed and approved (`FREQ.6D.11`: 5 new nullable `LongHorizonRollingSessionState` columns, `(StructuralRole, LaneOrdinal, SlotOrdinal)` JIT composition key, frozen-bundle profile binding), with the Runway/Core segments (20 of every 21-52 weeks) inheriting existing approved authority automatically once engineering splits land — but the LongHorizon-only GE (General Endurance) pre-Runway segment still has no approved 5D weekly structure or numeric coefficients, a narrow, distinct, not-yet-scheduled product-decision gap | not yet opened | not yet opened |
| **Advanced** | not yet opened | not yet opened | not yet opened | not yet opened | not yet opened |

Beginner×3D Runway (15-20wk) is separately confirmed non-representable (FREQ.2) with zero live-cell exposure (FREQ.2A) — this is a Runway-horizon finding layered on top of the Core-level `PROVEN_NON_SUPPORT` result above, not a duplicate claim.

### Current active phase / next phase

**Latest verified completed phase**: `FREQ.6D.11` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_ARCHITECTURE_APPROVED_PRODUCT_POLICY_REQUIRED`. Designed the complete Intermediate×5D LongHorizon dual-KEY lineage/JIT-composition/persistence/execution-context architecture: session identity (`LaneOrdinal`+`SlotOrdinal`, mirroring `BoundCatalogSession`'s own shape), 5 new nullable `LongHorizonRollingSessionState` columns (zero backfill, zero-delta for historical 4D rows), JIT composition keyed on `(StructuralRole, LaneOrdinal, SlotOrdinal)` (fixing a confirmed real dual-KEY collision in `BuildBoundedCoreSelection`'s raw-`StructuralRole` grouping), frozen-bundle profile-binding lifecycle, and `ExecutionPrescriptionIndex` propagation reusing the existing shared `TenKPreparationRunwayDarkOrchestrator` (no duplicate engine). Discovered LongHorizon's real Runway relationship directly from `LongHorizonCompositionResolver`: 21-52wk = GE (variable) + the real Preparation Runway (fixed 8wk, already 5D-capable since `FREQ.6D.10`) + the real Core (fixed 12wk) — so Runway/Core segments (20 of every 21-52 weeks) inherit existing approved authority automatically. Only the LongHorizon-only GE segment has no approved 5D weekly structure/numeric coefficients (its catalog is `DRAFT`/never-loaded, 4D-shaped by construction) — correctly flagged `PRODUCT_DECISION_REQUIRED`/`NUMERIC_DECISION_REQUIRED` rather than inferred. Re-audited 4D-only gates exhaustively: 21 lines/14 files (vs. `FREQ.6D.5`'s ~10 estimate), each classified. Confirmed 5-session Adaptation already generic/lane-blind, no new policy needed. Defined a 6-split implementation decomposition with explicit dependency order. No production code/migration/routing touched. Next: a narrow GE-segment product/numeric-decision phase.

Prior phase: `FREQ.6D.10` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_MISSING_ZERO_NUMERIC_AUTHORITY_IMPLEMENTED_AND_VERIFIED`. Wired the `FREQ.6D.9`-confirmed existing `FREQ.6C` Intermediate×5D numeric authority into Core and Preparation Runway; real HTTP + real PostgreSQL verified across all previously-failing horizons.

Prior phase: `FREQ.6D.9` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_MISSING_ZERO_EXISTING_NUMERIC_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`. Reconciled the `FREQ.6D.8`-disclosed Intermediate×5D missing/explicit-zero Core starting-volume failure against the complete `FREQ.6`/`FREQ.6B`/`FREQ.6C` authority chain; found `FREQ.6C` already approved exact 5D-specific values, all `ELIGIBLE`, but runtime never wired to them. No production code touched — evidence/decision only.

Prior phase: `FREQ.6D.8` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED`. Real HTTP + real PostgreSQL verification for Intermediate×5D Preparation Runway (15-20 weeks); found and disclosed (not fixed) the pre-existing Core starting-volume defect `FREQ.6D.9` reconciled.

Prior phase: `FREQ.6D.7` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED`. Generalized the Intermediate×4D Preparation Runway implementation to also support Intermediate×5D, reusing the existing Runway engine and existing generic Core pipeline (no second implementation, `RUN_LAYOUT_5D` untouched).

Prior phase: `FREQ.6D.6` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_RUNWAY_PRODUCT_POLICY_APPROVED`. Resolved both open Preparation Runway product authorities for Intermediate×5D (weekly structure 1K+3E+1L, starting-volume reuse of the already-live Core numeric authority). No production code touched.

Prior phase: `FREQ.6D.5` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_RUNWAY_LONGHORIZON_SEPARATE_WAVES_REQUIRED`. Full pipeline trace of Preparation Runway and LongHorizon against Core 5D; found Runway hardcoded to 4D at three layers with no product authority for its 5D weekly shape (the gap `FREQ.6D.6` closed), and LongHorizon hardcoded even more pervasively with a deeper schema-level gap remaining fully open.

Prior phase: `FREQ.6D.4D.5G` — Execution Status `DONE`, Final Classification `FREQ6D4D5G_CORE_HORIZON_CONTEXT_AND_PUBLIC_5D_ACTIVATION_IMPLEMENTED`. Fixed the CompressedCore/ExtendedCore execution-context propagation gap and completed the fifth public-activation retry across all four Core horizons. **Parent `FREQ.6D.4D` closed as `FREQ6D4D_DUAL_KEY_PRODUCTION_INTEGRATION_IMPLEMENTED_AND_VERIFIED`.**

Prior phase: `FREQ.6D.4D.5F` — Execution Status `DONE (PARTIAL)`, Final Classification `FREQ6D4D5F_MAPPING_FIXED_PUBLIC_ACTIVATION_BLOCKED_ELSEWHERE`. Implemented the approved public workout-type mapping and found the CompressedCore/ExtendedCore execution-context gap `FREQ.6D.4D.5G` fixed.

Prior phase: `FREQ.6D.4D.5E` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_PUBLIC_WORKOUT_MAPPING_CLOSURE_APPROVED`. Derived the complete real Intermediate×5D workout closure fresh from the catalog and decided the `AEROBIC_STRENGTH_CONTROLLED_INTRO` → `Interval` mapping `FREQ.6D.4D.5F` implemented.

Prior phase: `FREQ.6D.4D.5D` — Execution Status `DONE (PARTIAL)`, Final Classification `FREQ6D4D5D_TAPER_FIXED_PUBLIC_ACTIVATION_BLOCKED_ELSEWHERE`. Implemented the approved `FREQ.6D.4D.5C` Legacy/ProfileBacked Taper-completeness partition in `CatalogPrescriptionContextValidator`, and found + fixed a second occurrence of the identical root cause in `CatalogFinalPrescribedPlanValidator`. Both real 5D Taper lanes proven valid through both validators without stage-name special-casing; zero legacy delta. Retried public activation a third time, progressed past calendar and both Taper checks for the first time, then found the `V1CatalogPublicWorkoutTypeMappingPolicy` gap `FREQ.6D.4D.5E` resolves.

Prior phase: `FREQ.6D.4D.5C` — Execution Status `DONE`, Final Classification `TAPER_COMPLETENESS_EXISTING_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`. Traced `CatalogPrescriptionContextValidator`'s hardcoded `TAPER_SHARPEN` check to its real origin and found the real completeness authority for ProfileBacked Taper already exists downstream (Split-C's execution-resolution guarantee); approved the Legacy/ProfileBacked partition `FREQ.6D.4D.5D` implements.

Prior phase: `FREQ.6D.4D.5` (Split E, partial) — Execution Status `DONE (PARTIAL)`, Final Classification `FREQ6D4D_SPLIT_E_PARTIAL_RUNTIME_DISCOVERY_IMPLEMENTED_PUBLIC_ACTIVATION_BLOCKED`. Authored the complete real Intermediate×5D PlanCatalog chain — `RUN_LAYOUT_5D`, `TEN_K__5D__INTERMEDIATE` combination, dual-lane progression, all 8 real production profiles promoted `VALIDATED`, a real CLI-published `1.1.0` release with `ExecutionPrescriptions` present for all 8 profiles — 1,510/1,510 PlanCatalog.Tests. Wired RunningApp's runtime published-bundle discovery end to end (`IPublishedTemplateBundleLoader`/`PublishedTemplateBundleLoader`, threaded through `CatalogPreviewGenerator`'s full constructor chain, DI, `PlanCatalog:PublishedBundleReleaseVersion` config, deployment packaging) — the exact gap Split C disclosed, now closed, though no candidate reaches it in production yet. Attempted public activation of `TEN_K__5D__INTERMEDIATE` for the 8-14 week Core route only (widest activation the user approved); real end-to-end HTTP testing (not static analysis) found `CatalogWeekSkeletonCalendarMaterializer` hardcodes exactly one `KEY_SESSION` slot per week in both its validation and its date-assignment backtracking algorithm — a real 5D week has two (LaneOrdinal 0/1), which would either be rejected outright or, if the count guard were removed, silently collide both slots onto the same calendar date. The routing widening was reverted rather than shipped with a confirmed live 500. Full regression: 3,612/3,613 RuntimeCatalog (1 pre-existing unrelated `Sw09` failure, unrelated to 5D), 1,510/1,510 PlanCatalog.

**Next phase per repository-backed sequencing**: `FREQ.6D.12` — **INTERMEDIATE×5D LONGHORIZON GE SEGMENT WEEKLY STRUCTURE & NUMERIC POLICY DECISION CLOSURE**. Phase type: **EVIDENCE + PRODUCT_DECISION + NUMERIC_DECISION**. This phase resolves the LongHorizon GE (General Endurance) pre-Runway segment's own 5D weekly session-role structure and numeric coefficients (`FREQ.6D.11`'s §25/§26/§32/§78) — the one remaining gap before the dual-KEY lineage/JIT/persistence/execution-context implementation splits (`FREQ.6D.11`'s §33 Splits A-D) can fully proceed to public activation. Splits A-D themselves (schema+migration, JIT generalization, execution-index propagation, gate relaxation) do not strictly require this decision to begin for the Runway/Core segments, but full 21-52 week composition and public activation (Split F) do. `FREQ.6D.12` is scheduled, not started.

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

**WAVE A — 10K completion.** Intermediate×5D Core (8-14 weeks) is `PUBLICLY_ACTIVE` (FREQ.6D.4D.5G, §2). Preparation Runway (15-20w) is `PUBLICLY_ACTIVE` for all readiness states including missing/explicit-zero, real HTTP/DB verified (FREQ.6D.8/FREQ.6D.10, §2). Long-Horizon (21-52w) 5D activation remains an open gap; its dual-KEY lineage/JIT/persistence/execution-context architecture is now fully designed (FREQ.6D.11), with a narrow GE-segment product/numeric decision still open before implementation can fully proceed to public activation. No Half Marathon or Marathon work may begin under this roadmap's own rule until 10K's full architectural closure (§25/Wave A milestones, including the Long-Horizon 5D gap) is reached.

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

Repository evidence (see `PHASE_LEDGER.md` rows 59-72) confirms the chain through `FREQ.6D.4D`'s dual-KEY production-integration architecture approval. The real near-term sequence is now:

```
FREQ.6D.4C.2 (DONE)             → IMPLEMENTATION: narrowed WorkoutPrescriptionProfileValidator's
                                    intensity-mode check (M4); added the new capability-overlay
                                    artifact + GOAL_PACE_TEN_K v2 entry (M3); completed
                                    GOAL_PACE_TEN_K v3's DRAFT content. All 8 approved slots now
                                    proven representable and lossless-projecting.
FREQ.6D.4B.1 (DONE)             → EVIDENCE: all full-component fields inventoried; warm-up/cooldown
                                    envelopes established; FARTLEK structural RECOVERY conflicts
                                    with nested-recovery ownership in the current model.
FREQ.6D.4B.2 (DONE)             → ARCHITECTURE: R1 selected; nested MAIN_SET recovery is sole owner;
                                    BLD-S v4→v5 product-reference amendment required.
FREQ.6D.4B.3 (DONE)             → PRODUCT DECISION: WU=600s EASY, CD=300s EASY; BLD-S→v5;
                                    FC1-FC10 complete, no athlete-facing implementation choice.
Gate B (PASS: 0bc70c5)          → remote SHA matched local gate SHA; ahead/behind 0/0.
FREQ.6D.4B.4 (DONE)             → IMPLEMENTATION: corrected DRAFT skeletons and lifecycle-aware
                                    validation; BLD-S now targets v5; all-eight/no-double-count
                                    tests pass; immutable FARTLEK v4 preserved.
FREQ.6D.4C.3 (DONE)             → IMPLEMENTATION: authored all 8 real production
                                    WorkoutPrescriptionProfile documents using the corrected exact
                                    references and frozen full-component policy; 8/8 catalog
                                    capacity READY; zero infrastructure delta; legacy bundles
                                    architecturally unaffected.
FREQ.6D.4C.4 (DONE)             → ARCHITECTURE: root-caused the legacy regression exactly (only
                                    the frozen historical combinations v1-v3 + golden/cascade tests
                                    are exposed; the real, live v4 combination already resolves via
                                    exact refs). Selected exact-reference/manifest activation
                                    authority (already realized) + a narrow, additive legacy-
                                    resolver-eligibility flag as the permanent containment
                                    instrument. No CatalogStatus change; no legacy-pin migration.
FREQ.6D.4C.5 (DONE)             → IMPLEMENTATION: added the narrow, nullable, hash-stable
                                    EligibleForLegacyDefaultResolution flag; extended
                                    FindWorkout(key, ledger)'s filter; promoted all four DRAFT
                                    versions to VALIDATED with the flag false in the same atomic
                                    commit. Live v4 combination, historical v1-v3 replay, and all 8
                                    profiles proven unchanged; golden/cascade regressions green.
                                    CATALOG_LIFECYCLE_BLOCKER now CLOSED.
FREQ.6D.4D (DONE)               → ARCHITECTURE: re-verified FREQ.6D.1A/1B's proposed Lane/Stage/
                                    Adaptation design against current code (never implemented, every
                                    gap still real); selected Option D1 (catalog-authored LaneOrdinal
                                    + bind-time structural ordinal, per-lane independent allocator
                                    invocation, exact profile refs at catalog/binder boundary); full
                                    authority map/dataflow/failure-semantics/A-E implementation split
                                    produced. Zero remaining product decision; zero legacy delta.
FREQ.6D.4D.1 (DONE)             → IMPLEMENTATION (Split A): catalog-authored LaneOrdinal + bind-time
                                    structural ordinal (from SlotOrderInWeek); (WeekNumber, LaneOrdinal)-
                                    keyed stage schedule replacing the defective WeekNumber-only
                                    dictionary; per-lane independent ProgressionStageAllocator
                                    invocation (math unchanged); BoundCatalogSession.LaneOrdinal now
                                    sole ordinal authority. 21 new tests; 2,898/2,899 RuntimeCatalog,
                                    1,485/1,485 PlanCatalog. Legacy 3D/4D/Beginner×4D unchanged.
FREQ.6D.4D.2 (DONE)              → IMPLEMENTATION (Split B): additive PrescriptionProfileCandidates
                                    stage-authoring field (PlanCatalog + RunningApp mirror); exact
                                    cardinality-only profile resolution in CatalogWorkoutBinder
                                    (fail-closed on ambiguity); PrescriptionProfileLaneDoseValidator
                                    reused verbatim; new PrescriptionProfileClosureResolver /
                                    PrescriptionProjectionDependencyResolver glue feeding the
                                    unmodified FREQ.6D.3C CatalogBundleAssembler exact-dependency
                                    overload — dual-lane bundle ExecutionPrescriptions proven
                                    non-null/deterministic. 21 new tests; 1,501/1,501 PlanCatalog.
                                    RunningApp DB-backed subset could not execute (Docker/Postgres
                                    unavailable this session — environment limitation, not a
                                    regression; all 197 non-executing failures independently
                                    Npgsql-confirmed). Real RUN_LAYOUT_5D authoring remains Split E.
FREQ.6D.4D.3 (DONE)              → IMPLEMENTATION (Split C): CatalogSessionPrescriptionSource /
                                    ExecutionPrescriptionIndex (FREQ.6D.3D, previously dormant)
                                    wired into CatalogSessionPrescriptionPlanner's live path;
                                    exact per-session Legacy/ProfileBacked classification off
                                    BoundCatalogSession profile lineage; fail-closed on partial
                                    lineage/missing index/missing exact profile/wrong version/
                                    workout-provenance mismatch; never falls back to Legacy. 16
                                    new tests via the real end-to-end planner. 500/500 scoped
                                    in-memory regression; 1,962/1,972 broader (10 environmental
                                    Npgsql failures, Docker unavailable). Legacy 3D/4D unchanged.
FREQ.6D.4D.4 (DONE)              → IMPLEMENTATION (Split D): 2 new nullable TrainingDay columns
                                    (CatalogPrescriptionProfileKey/Version) via a real, applied EF
                                    migration; both live confirmation mappers wired to thread exact
                                    profile lineage through; LaneOrdinal/execution-content
                                    deliberately not persisted (derivable/bundle-only, per
                                    architecture); repair/substitution subsystem audited, already
                                    correct; complete real FREQ.6 24-row 5-session Adaptation
                                    severity table implemented in NextWindowLoadDecisionPolicy,
                                    legacy 4-session behavior unchanged. 44 + 5 new tests, all
                                    DB-backed proof real (Docker/Postgres restored this phase).
                                    2,967/2,969 RuntimeCatalog (2 pre-existing unrelated), 1,501/1,501
                                    PlanCatalog, 192/192 LongHorizon.Adaptation.
FREQ.6D.4D.5 (DONE, PARTIAL)    → IMPLEMENTATION (Split E, partial): real RUN_LAYOUT_5D/
                                    TEN_K__5D__INTERMEDIATE combination/dual-lane progression/8
                                    profiles/published 1.1.0 release with ExecutionPrescriptions;
                                    RunningApp published-bundle runtime discovery wired end to end
                                    (IPublishedTemplateBundleLoader, DI, config, packaging) — the
                                    Split-C gap now closed. Public activation attempted for 8-14w
                                    Core only, reverted: CatalogWeekSkeletonCalendarMaterializer
                                    hardcodes one KEY_SESSION slot/week, a real algorithm gap plus
                                    an undecided product question (min. inter-KEY-session
                                    separation) confirmed by a real E2E 500, not static analysis.
                                    TEN_K__5D__INTERMEDIATE remains fully dark to public traffic.
                                    3,612/3,613 RuntimeCatalog (1 pre-existing unrelated), 1,510/1,510
                                    PlanCatalog.
Gate B (PASS: 13594ac)          → remote SHA matched local gate SHA; ahead/behind 0/0. ~10
                                    completed phase prompts since the prior Gate B (0bc70c5):
                                    FREQ.6D.4B.4 through FREQ.6D.4D.5.
FREQ.6D.4D.5A (DONE)            → EVIDENCE + PRODUCT_DECISION: reconstructed prior FREQ.3/FREQ.4/
                                    FREQ.4A authority (predating the FREQ.6D branch); found
                                    DatedGeneratedCatalogPlanSkeletonValidator already generalized
                                    to N>=1 KEY with an embedded, disclosed, not-yet-evidenced
                                    MinimumKeySessionToKeySessionSeparationDays placeholder;
                                    clarified CatalogWeekSkeletonCalendarMaterializer (the real
                                    blocker) is a distinct, never-generalized upstream component.
                                    Fresh external evidence (48-72h recovery convention; 5 real
                                    fetched intermediate 10K/5-day plans, none consecutive-day)
                                    converged with the placeholder; a real combinatorial
                                    counterexample rejected the stricter >=3 alternative. Approved
                                    MinimumKeySessionToKeySessionSeparationDays = 2 (calendar-date
                                    difference), phase-invariant, symmetric, reusing an existing
                                    tie-break and exception type. No code touched. Full multi-slot
                                    implementation contract + test manifest produced for the next
                                    phase.
FREQ.6D.4D.5B (DONE, PARTIAL)   → IMPLEMENTATION: generalized CatalogWeekSkeletonCalendarMaterializer
                                    to multi-KEY_SESSION weeks (keyCount>=1), enforcing the frozen
                                    FREQ.6D.4D.5A KEY<->KEY rule via a generalized backtracking
                                    search degenerating exactly to the pre-existing algorithm for
                                    keyCount==1; single numeric authority (removed the
                                    materializer's own duplicate constant). Proven against the real
                                    TEN_K__5D__INTERMEDIATE candidate for 8/10/12/14 weeks, incl.
                                    Taper, determinism, lane-identity preservation; zero legacy
                                    delta (3,639/3,640, 1,510/1,510 PlanCatalog). Public activation
                                    retried and reverted again: CatalogPrescriptionContextValidator
                                    hardcodes a TAPER_SHARPEN stage-key check incompatible with the
                                    real dual-lane Taper naming -- a second, independent,
                                    calendar-unrelated blocker, not worked around.
FREQ.6D.4D.5C (DONE)            → EVIDENCE + ARCHITECTURE_DECISION: traced TAPER_SHARPEN to its
                                    real origin (PHASE4F_7D's V1_TAPER_SHARPEN_PRESCRIPTION_POLICY,
                                    a pilot-specific legacy runtime-injection content policy, never
                                    canonical vocabulary). Found ProfileBacked Taper completeness
                                    already proven downstream by Split-C's per-session execution-
                                    resolution guarantee (covers both real 5D lanes, stronger than
                                    "at least one"). Rejected weaker/wrong-axis models. Selected:
                                    partition the existing check along the Legacy/ProfileBacked axis
                                    (thread BoundCatalogSession.PrescriptionProfileKey one struct
                                    further -- additive, not new metadata); every Legacy Taper KEY
                                    instance still must match TAPER_SHARPEN/EASY_STANDARD exactly as
                                    today (zero 3D/4D/Beginner4D delta); ProfileBacked instances
                                    exempted, covered downstream. Real invalid-5D counterexample
                                    constructed proving no collapse into blanket acceptance. No code
                                    touched. Full implementation contract + 22-item test manifest
                                    produced for the next phase.
FREQ.6D.4D.5D (DONE, PARTIAL)   → IMPLEMENTATION + INTEGRATED VERIFICATION: implemented the
                                    approved FREQ.6D.4D.5C Legacy/ProfileBacked Taper-completeness
                                    partition in CatalogPrescriptionContextValidator; found and fixed
                                    a second occurrence of the identical root cause in
                                    CatalogFinalPrescribedPlanValidator (via the already-existing
                                    CatalogPrescribedSession.PrescriptionSource classification). Both
                                    real 5D Taper lanes proven valid without stage-name special-
                                    casing; malformed-Legacy counterexample still rejected; zero
                                    legacy delta (3,649/3,650, 1,510/1,510 PlanCatalog). Public
                                    activation retried a third time -- progressed past calendar and
                                    both Taper checks for the first time -- then found a third,
                                    independent, out-of-scope blocker: V1CatalogPublicWorkoutTypeMappingPolicy
                                    has no public workout-type mapping for the real 5D
                                    AEROBIC_STRENGTH_CONTROLLED_INTRO workout. Reverted a third time.
FREQ.6D.4D.5E (DONE)            → EVIDENCE + PRODUCT_DECISION: derived the complete real
                                    Intermediate x5D workout closure fresh from the catalog (8
                                    stage/lane combinations, 6 distinct reachable pairs), cross-
                                    referenced exhaustively against V1CatalogPublicWorkoutTypeMappingPolicy
                                    -- exactly one gap found (AEROBIC_STRENGTH_CONTROLLED_INTRO), no
                                    others. Traced the policy's real intent (coarse athlete-facing
                                    display taxonomy) and the workout's real semantics from FREQ.6D.4B
                                    product authority, without name-based inference. Approved mapping
                                    to the existing Interval type on strong structural/family
                                    precedent (same shape/family as the already-Interval-mapped
                                    FARTLEK); no taxonomy extension needed; key-only mapping ownership
                                    confirmed correct; zero UI/API/analytics impact. One-arm
                                    implementation contract + 18-item test manifest produced. No code
                                    touched.
FREQ.6D.4D.5F (DONE, PARTIAL)   → IMPLEMENTATION + INTEGRATED VERIFICATION: implemented the
                                    approved FREQ.6D.4D.5E mapping (AEROBIC_STRENGTH_CONTROLLED_INTRO
                                    -> Interval, one key-only arm, zero taxonomy change) plus a real
                                    catalog-file-driven exhaustive completeness gate reproducing the
                                    real 8-combination/6-pair closure and proving every pair maps
                                    exactly once, deterministically. Retried public activation a
                                    fourth time via real HTTP E2E testing (real committed catalog +
                                    real published 1.1.0 bundle): 12-week 5D preview genuinely
                                    succeeds (furthest point yet); 8/10/14-week previews 500 --
                                    root-caused to a fourth, independent blocker:
                                    CatalogPreviewGenerator's CompressedCore/ExtendedCore dynamic-
                                    orchestration branch (every horizon except exactly 12 weeks) never
                                    threads the published-bundle execution index through, unlike the
                                    exact-12-week pipeline. Reverted the widening a fourth time;
                                    retained the independently-correct mapping fix. Zero legacy delta
                                    (3,671/3,672, 1,510/1,510 PlanCatalog).
FREQ.6D.4D.5G (DONE)            → IMPLEMENTATION + INTEGRATED VERIFICATION: root-caused the
                                    FREQ.6D.4D.5F blocker exactly -- DynamicCoreSessionPrescriptionOrchestrator
                                    constructed CatalogSessionPrescriptionRequest with ExecutionIndex
                                    omitted (defaulted null), the single narrow wiring gap affecting
                                    every CompressedCore/ExtendedCore horizon. Fixed by threading the
                                    same published-bundle ExecutionPrescriptionIndex through both
                                    dynamic-orchestration context types, computed once per request via
                                    a new shared CatalogPreviewGenerator.LoadExecutionIndex helper -- no
                                    new prescription logic, no horizon-number special-casing,
                                    ExecutionPrescriptionIndex.ResolveExact remains the sole ProfileBacked
                                    consumer authority, Legacy sessions unaffected. Proved dark first (18
                                    new tests: real 8/10/14-week CompressedCore/ExtendedCore generation,
                                    every ProfileBacked session resolves exact execution, omitted-context
                                    still fails closed, Taper/calendar/determinism zero-delta) before
                                    retrying public activation a fifth time. Real HTTP E2E: 8/10/12/14-
                                    week previews all succeed; confirmed 8-week (CompressedCore),
                                    14-week (ExtendedCore), 12-week (PreferredCore reference) plans
                                    against real PostgreSQL with correct persistence; unsupported
                                    neighbors remain closed; zero legacy regression (3,704/3,705,
                                    1,510/1,510 PlanCatalog). TEN_K__5D__INTERMEDIATE genuinely
                                    publicly active. Parent FREQ.6D.4D closed as
                                    FREQ6D4D_DUAL_KEY_PRODUCTION_INTEGRATION_IMPLEMENTED_AND_VERIFIED.
FREQ.6D.5 (DONE)                → ARCHITECTURE + READINESS AUDIT: full pipeline trace of Preparation
                                    Runway (15-20w) and LongHorizon (21-52w) against proven Core 5D. No
                                    code touched. Runway hardcoded to Intermediate x4D at three
                                    independent layers (routing gate, unconditional candidate load,
                                    orchestrator ValidateRequest) plus a fixed single-KEY 4-slot layout
                                    with no product authority for 5D's Runway weekly structure
                                    (PRODUCT_DECISION_REQUIRED); catalog content itself is candidate-
                                    agnostic and most numeric coefficients already generic. LongHorizon
                                    hardcoded even more pervasively (~10 independent DaysPerWeek==4
                                    gates); genuine architecture gap -- persisted session schema has no
                                    LaneOrdinal/ProgressionStageKey/PrescriptionProfileKey columns,
                                    real JIT Core-week composition discards dual-KEY lineage by
                                    grouping on raw structural-role string (DB-migration-carrying gap).
                                    5-session Adaptation policy already frequency-generic but
                                    unreachable for LongHorizon. Zero ExecutionPrescriptionIndex
                                    references anywhere in LongHorizon; both Runway's and LongHorizon's
                                    Core-entry share the same orchestrator rather than
                                    CatalogPreviewGenerator directly. Classification:
                                    INTERMEDIATE_5D_RUNWAY_LONGHORIZON_SEPARATE_WAVES_REQUIRED.
FREQ.6D.6 (DONE)                → EVIDENCE + PRODUCT_DECISION: approved both open Preparation Runway
                                    product authorities. Weekly structure: 1 KEY_SESSION + 3
                                    EASY_SUPPORT + 1 LONG_RUN every Runway week (session count
                                    invariant at 5, never ramps -- grounded in RunLayout's fixed-
                                    cardinality architecture, Core's own phase-invariant frequency, and
                                    external base-phase coaching evidence), generalizing the existing
                                    4D block-role override table by one additional EASY slot; second
                                    KEY introduced only at real Core Week 1. Starting-volume: direct
                                    repository-truth discovery that CatalogVolumeAndLongRunPlanner.Build
                                    only special-cases Intermediate x3D and Beginner x4D --
                                    Intermediate x5D already falls through to the same
                                    V1MissingReadinessStartingVolumePolicy (16km missing / 12km
                                    explicit-zero) Runway itself uses, confirming rather than borrowing
                                    the already-live 5D Core numeric authority. This also proves
                                    Runway's own Week-1 volume and its Core-entry target resolve
                                    through the identical policy, making feasibility structurally
                                    assured across all 15-20 week horizons and every readiness state --
                                    zero blocked cells in the representability matrix. No production
                                    code, catalog, or LongHorizon work touched. Classification:
                                    INTERMEDIATE_5D_RUNWAY_PRODUCT_POLICY_APPROVED.
FREQ.6D.7 (DONE)                → CHECKPOINT GATE + IMPLEMENTATION + INTEGRATED VERIFICATION:
                                    generalized the Intermediate x4D Preparation Runway implementation
                                    to also support Intermediate x5D (1 KEY + 3 EASY + 1 LONG every
                                    Runway week, second KEY only at real Core Week 1), reusing the
                                    existing Runway engine and existing generic Core pipeline -- no
                                    second implementation, RUN_LAYOUT_5D untouched. Generalized
                                    FourDaySessionDistanceAllocationPolicy's EASY_SUPPORT the same way
                                    KEY_SESSION was already generalized (FREQ.4); PreparationRunway-
                                    CoreWeekOneTargetAdapter now reads the real KEY count from Core's
                                    own Week 1; Runway/Core boundary continuity (numeric, calendar,
                                    pace) now applies per-slot KEY/EASY comparison only when role
                                    composition actually matches, per FREQ.6D.6's "KEY count is not a
                                    Core-entry compatibility dimension." Found and fixed two more
                                    hardcodings only reachable once 5D's real dual-KEY Core Week 1
                                    was exercised through this path: a KEY-session pace-target ordinal
                                    collision and two hardcoded DaysPerWeek=4 calendar-skeleton
                                    literals; threaded a real ExecutionPrescriptionIndex into Runway's
                                    own Core-generation call site (the missing-context class
                                    FREQ.6D.4D.5G fixed elsewhere, at a call site that fix didn't
                                    reach). Proved dark end-to-end: all six 15-20 week horizons x
                                    READY/NOT_READY produce the exact approved shape, real Core Week 1
                                    is exactly 2K+2E+1L, Intermediate x4D remains byte-for-byte
                                    unchanged (1767/1767 full regression). Public routing is code-
                                    wired to the exact approved combination; real HTTP E2E and real
                                    PostgreSQL confirmation were not performed this session --
                                    disclosed explicitly. Classification:
                                    INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED.
FREQ.6D.8 (DONE)                → VERIFICATION + PUBLIC ACTIVATION: closed FREQ.6D.7's own disclosed
                                    gap with real HTTP E2E (real Api host, real Postgres, no mocks) for
                                    all six 15-20 week Intermediate x5D Preparation Runway public
                                    previews -- every one 200 with exact TEN_K__5D__INTERMEDIATE
                                    identity, exact 1 KEY+3 EASY+1 LONG Runway shape, exact 2 KEY+2
                                    EASY+1 LONG Core Week 1. Real confirmation/persistence verified for
                                    15/17/20-week horizons: exact role cardinality, exact last-Runway/
                                    first-Core transition re-asserted after a fresh DB reload (permanent
                                    regression), real profile-key/version lineage on Core KEY sessions.
                                    Home/calendar/training-day-detail succeed; unsupported neighbors and
                                    21+/24-week LongHorizon remain closed; Intermediate x4D Runway and
                                    Intermediate x5D Core-only remain byte-for-byte unchanged. Found and
                                    disclosed (not fixed, per explicit scope) a real, pre-existing
                                    Core-side defect: missing/explicit-zero starting-volume evidence
                                    fails real Core generation for Intermediate x5D at the real 2-KEY
                                    minimum -- independently proven pre-existing by reproducing
                                    identically against the already-active Core-only route and against
                                    the pre-FREQ.6D.7 durable baseline commit. No routing change needed
                                    (already wired); no production code touched. Full regression
                                    3744/3746 (2 pre-existing failures, both re-verified against the
                                    durable baseline), 1510/1510 PlanCatalog. Classification:
                                    INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED.
FREQ.6D.9 (DONE)                → EVIDENCE + PRODUCT/NUMERIC AUTHORITY DECISION: reconciled the
                                    FREQ.6D.8-disclosed Intermediate x5D missing/explicit-zero Core
                                    starting-volume failure against the complete FREQ.6/FREQ.6B/FREQ.6C
                                    authority chain. FREQ.6C already approved exact 5D-specific values
                                    two phases earlier -- missing=26.0km (Hal Higdon Week-1 evidence
                                    anchor), explicit-zero=19.5km (26.0*0.75, reusing 4D's own ratio
                                    applied to 5D's own anchor), peak reference=44.5km, long-run
                                    share=28%/36% cap -- all 14 representability cells already ELIGIBLE.
                                    Runtime never wires to this: CatalogVolumeAndLongRunPlanner.Build
                                    special-cases only Intermediate x3D/Beginner x4D, so 5D falls
                                    through unconditionally to the generic, 4D-provenance-labeled
                                    V1MissingReadinessStartingVolumePolicy (16km/12km) -- below the real
                                    5D 2-KEY structural minimum (~12.5-13.4km, computed from real
                                    unmodified per-session minima). Corrected FREQ.6D.6's prior premise
                                    that this fallthrough was already-live 5D authority
                                    (SUPERSEDED_BY_EXISTING_FREQ6C_AUTHORITY); Runway's own weekly-
                                    structure decision is unaffected. Distinguished this real,
                                    already-realized failure from FREQ.6C's separate, still-theoretical
                                    KEY2-floor asymmetric-allocation risk (unreachable today).
                                    Re-reproduced all 8 Core-only missing/zero HTTP 500s (8/10/12/14wk x
                                    missing/zero) against the real host/DB. No production code touched.
                                    Classification:
                                    INTERMEDIATE_5D_MISSING_ZERO_EXISTING_NUMERIC_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT.
FREQ.6D.10 (DONE)               → IMPLEMENTATION + INTEGRATED VERIFICATION: wired the FREQ.6D.9-
                                    confirmed existing FREQ.6C authority into CatalogVolumeAndLongRunPlanner
                                    (Core) and TenKPreparationRunwayNumericPolicyFactory (Runway) via
                                    exact-identity-only dispatch (mirroring ThreeDayIntermediate/
                                    BeginnerFourDay, never a broad DaysPerWeek>=5 condition). Fixed a
                                    latent units bug in PreparationRunwayNumericMaterializer's long-run-
                                    share validation, isolated per-policy (Default/3D/Beginner4D byte-
                                    identical). Real HTTP + real PostgreSQL: all 8 previously-failing
                                    Core-only cases and all 6 representative Runway cases now return 200.
                                    Full regression 3787/3791 (all diagnosed), 1719/1719 targeted,
                                    1510/1510 PlanCatalog. Classification:
                                    INTERMEDIATE_5D_MISSING_ZERO_NUMERIC_AUTHORITY_IMPLEMENTED_AND_VERIFIED.
FREQ.6D.11 (DONE)               → ARCHITECTURE DESIGN: selected the complete dual-KEY session-identity
                                    model (LaneOrdinal+SlotOrdinal), 5-column LongHorizonRollingSessionState
                                    schema (zero backfill), (StructuralRole,LaneOrdinal,SlotOrdinal) JIT
                                    composition key (fixing a confirmed real collision in
                                    BuildBoundedCoreSelection), frozen-bundle profile-binding lifecycle,
                                    and ExecutionPrescriptionIndex propagation reusing the shared
                                    TenKPreparationRunwayDarkOrchestrator. Found LongHorizon 21-52wk =
                                    GE(variable) + real Runway(fixed 8wk) + real Core(fixed 12wk) -- Runway/
                                    Core segments inherit existing FREQ.6C/6D.6/6D.10 authority automatically;
                                    only the GE segment has no approved 5D weekly structure/numeric
                                    coefficients. Re-audited gates: 21 lines/14 files. Defined 6-split
                                    implementation decomposition. No code touched. Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_ARCHITECTURE_APPROVED_PRODUCT_POLICY_REQUIRED.
FREQ.6D.12 (SCHEDULED)          → EVIDENCE + PRODUCT_DECISION + NUMERIC_DECISION: INTERMEDIATE×5D
                                    LONGHORIZON GE SEGMENT WEEKLY STRUCTURE & NUMERIC POLICY DECISION
                                    CLOSURE. Scheduled only; not started. Then the
                                    FREQ.6D.11 Splits A-D implementation (schema+migration, JIT
                                    generalization, execution-index propagation, gate relaxation), then
                                    dark verification, then public activation + real PostgreSQL E2E.
                                    FREQ.7 / FREQ.8 (legacy placeholder IDs) remain further out
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
