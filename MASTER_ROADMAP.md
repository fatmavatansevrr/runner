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
| **Intermediate** | `PUBLICLY_ACTIVE` (Core; GEN.3B) | `PUBLICLY_ACTIVE` (pre-existing/Adaptation V1 baseline) | `BLOCKED` — product policy approved (FREQ.6), numeric authority approved (FREQ.6C), catalog architecture design verified (FREQ.6D.1/1A/1B), engineering machinery (schema/projector/RunningApp consumer) implemented through FREQ.6D.3D; recovery-ownership architecture resolved (FREQ.6D.4B.2/4B.4) and all 8 real production WorkoutPrescriptionProfile documents now authored (FREQ.6D.4C.3); catalog-lifecycle/exact-version-activation architecture approved and implemented (FREQ.6D.4C.4/4C.5); **dual-KEY stage→profile production-integration architecture approved (FREQ.6D.4D)** — slot/lane identity + per-lane stage binding (FREQ.6D.4D.1), exact profile resolution + bundle wiring (FREQ.6D.4D.2), RunningApp session-lineage consumption (FREQ.6D.4D.3), durable profile-lineage persistence + the complete 5-session Adaptation severity table (FREQ.6D.4D.4), and the real 5D catalog content + RunningApp published-bundle runtime discovery (FREQ.6D.4D.5, Split E, partial) now implemented and verified; **public activation attempted and reverted twice (FREQ.6D.4D.5, FREQ.6D.4D.5B)** — the first revert's blocker (`CatalogWeekSkeletonCalendarMaterializer` hardcoding one KEY_SESSION slot/week) is resolved: the product question was decided (FREQ.6D.4D.5A, `MinimumKeySessionToKeySessionSeparationDays = 2`) and the materializer itself was generalized and verified against the real 5D candidate for every supported horizon (FREQ.6D.4D.5B); retrying activation surfaced a second, independent, calendar-unrelated blocker — `CatalogPrescriptionContextValidator` hardcodes a `TAPER_SHARPEN` stage-key check incompatible with the real dual-lane Taper stage naming; **the semantic-authority question is now resolved (FREQ.6D.4D.5C)** — the real completeness authority for ProfileBacked Taper already exists downstream (Split-C's per-session execution-resolution guarantee); a narrow, additive Legacy/ProfileBacked-partitioned validator fix is scoped but not yet implemented; **not publicly active** | not yet opened | not yet opened |
| **Advanced** | not yet opened | not yet opened | not yet opened | not yet opened | not yet opened |

Beginner×3D Runway (15-20wk) is separately confirmed non-representable (FREQ.2) with zero live-cell exposure (FREQ.2A) — this is a Runway-horizon finding layered on top of the Core-level `PROVEN_NON_SUPPORT` result above, not a duplicate claim.

### Current active phase / next phase

**Latest verified completed phase**: `FREQ.6D.4D.5C` — Execution Status `DONE`, Final Classification `TAPER_COMPLETENESS_EXISTING_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`. Evidence + architecture-decision phase, no code touched. Traced `CatalogPrescriptionContextValidator`'s hardcoded `TAPER_SHARPEN` check to its real origin (`PHASE4F_7D`'s `V1_TAPER_SHARPEN_PRESCRIPTION_POLICY` — a deliberate, narrow, pilot-specific legacy runtime-injection content policy for the pre-catalog-architecture 3D/4D pilot, never canonical domain vocabulary). Found the real semantic authority for ProfileBacked Taper completeness already exists, independently, one layer downstream: `CatalogSessionPrescriptionPlanner`'s Split-C fail-closed per-session execution-resolution guarantee already covers every KEY lane (both real 5D lanes included), stronger than "at least one," with zero new mechanism needed. Evaluated and rejected weaker/wrong-axis models (phase-presence-only, dose-category-as-cardinality, new explicit metadata, terminal-stage semantics); selected completeness achieved by partitioning the existing check along the Legacy/ProfileBacked axis this architecture already uses everywhere (`BoundCatalogSession.PrescriptionProfileKey`, already real, threaded one struct further — additive, not new metadata). Constructed a real invalid-5D counterexample (malformed Legacy-classified stage) proving the fix does not collapse into blanket acceptance. Full implementation contract and 22-item test manifest produced for the next, still-unstarted phase. `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic; the validator change itself is not yet implemented.

Prior phase: `FREQ.6D.4D.5B` — Execution Status `DONE (PARTIAL)`, Final Classification `FREQ6D4D5B_MULTI_KEY_CALENDAR_MATERIALIZER_IMPLEMENTED`. Generalized `CatalogWeekSkeletonCalendarMaterializer` to multi-KEY_SESSION weeks (keyCount≥1) enforcing the frozen `FREQ.6D.4D.5A` KEY↔KEY rule, proven against the real `TEN_K__5D__INTERMEDIATE` candidate for 8/10/12/14 weeks with zero legacy delta (3,639/3,640, 1,510/1,510 PlanCatalog). Public activation retried and reverted a second time on the `TAPER_SHARPEN` blocker `FREQ.6D.4D.5C` now resolves the authority question for.

Prior phase: `FREQ.6D.4D.5` (Split E, partial) — Execution Status `DONE (PARTIAL)`, Final Classification `FREQ6D4D_SPLIT_E_PARTIAL_RUNTIME_DISCOVERY_IMPLEMENTED_PUBLIC_ACTIVATION_BLOCKED`. Authored the complete real Intermediate×5D PlanCatalog chain — `RUN_LAYOUT_5D`, `TEN_K__5D__INTERMEDIATE` combination, dual-lane progression, all 8 real production profiles promoted `VALIDATED`, a real CLI-published `1.1.0` release with `ExecutionPrescriptions` present for all 8 profiles — 1,510/1,510 PlanCatalog.Tests. Wired RunningApp's runtime published-bundle discovery end to end (`IPublishedTemplateBundleLoader`/`PublishedTemplateBundleLoader`, threaded through `CatalogPreviewGenerator`'s full constructor chain, DI, `PlanCatalog:PublishedBundleReleaseVersion` config, deployment packaging) — the exact gap Split C disclosed, now closed, though no candidate reaches it in production yet. Attempted public activation of `TEN_K__5D__INTERMEDIATE` for the 8-14 week Core route only (widest activation the user approved); real end-to-end HTTP testing (not static analysis) found `CatalogWeekSkeletonCalendarMaterializer` hardcodes exactly one `KEY_SESSION` slot per week in both its validation and its date-assignment backtracking algorithm — a real 5D week has two (LaneOrdinal 0/1), which would either be rejected outright or, if the count guard were removed, silently collide both slots onto the same calendar date. The routing widening was reverted rather than shipped with a confirmed live 500. Full regression: 3,612/3,613 RuntimeCatalog (1 pre-existing unrelated `Sw09` failure, unrelated to 5D), 1,510/1,510 PlanCatalog.

**Next phase per repository-backed sequencing**: Split E — integrated end-to-end closure: real `RUN_LAYOUT_5D`/`TEN_K__5D__INTERMEDIATE` catalog authoring, public 5D support-matrix/routing activation, and `PublishedTemplateBundleJsonReader` wired into `PlanCatalogBundleLoader.LoadCandidateAsync` for a real profile-backed candidate — the one remaining disclosed gap since Split B/C (no production caller sources a real published bundle yet, since none has ever been authored). Dual-KEY production integration itself remains **not implemented** until Split E completes.

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
[Next, not yet scheduled]       → narrow CatalogPrescriptionContextValidator implementation per
                                    FREQ.6D.4D.5C §33 (thread PrescriptionProfileKey, partition the
                                    Taper-completeness check), then re-attempt 8-14w public
                                    activation a third time; Preparation Runway (15-20w) /
                                    Long-Horizon (21-52w) 5D activation remain a further,
                                    structurally harder, separate gap (hardcoded 4-slot weekly shape)
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
