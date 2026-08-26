# PHASE 10K-FREQ.6D.11 — Intermediate×5D LongHorizon Dual-KEY Lineage, JIT Composition, Persistence & Execution-Context Architecture Design

**Type:** ARCHITECTURE / DESIGN + DESIGN VERIFICATION (no production code, no migration, no routing activation)
**Parent phase:** FREQ.6D.10
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. Every finding below is re-derived from the current repository state.

---

## 1. Preflight

- `git rev-parse HEAD` at start: `8a5c319` (FREQ.6D.10's own SHA-backfill commit).
- `git status --short`: only pre-existing, unrelated `m baseline_tmp` / `plan-catalog/artifacts/audits/*` entries (untouched, same as every prior phase's preflight).
- `git branch --show-current`: `main`.
- `git rev-list --left-right --count origin/main...HEAD`: `0  13`.
- `git diff --check`: clean.
- **Mandatory precondition verified**: `PHASE_LEDGER.md` row 90, `FREQ.6D.10`, Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_MISSING_ZERO_NUMERIC_AUTHORITY_IMPLEMENTED_AND_VERIFIED` — real HTTP + real PostgreSQL confirmed. The known missing/explicit-zero production correctness gap is closed; the 5D Core/Runway baseline is stable. Proceeding is authorized.

---

## 2. Previous numeric-production-gap status

`INTERMEDIATE_5D_MISSING_ZERO_NUMERIC_AUTHORITY_IMPLEMENTED_AND_VERIFIED` (§10 of `FREQ.6D.10`'s own report). Not blocked.

---

## 3. Current LongHorizon public routing state

Re-verified directly against current source (not memory), matching `FREQ.6D.5`'s own findings exactly:

- `LongHorizonPublicPlanService.cs:81` — `CandidateKey = "TEN_K__4D__INTERMEDIATE"` (class-level const).
- `LongHorizonPublicPlanService.cs:354` — `ValidatePilot` throws `LongHorizonPilotUnsupportedException` unless `command.DaysPerWeek == 4`.
- The public preview pipeline (`PlanServices.GeneratePreviewAsync`) never reaches LongHorizon composition for any candidate — 21+ week requests fail `PlanHorizonCompositionRequiredException` for everyone, exactly as `FREQ.6D.5` §19/§24 documented.
- No change since `FREQ.6D.5`; `FREQ.6D.6`-`.10` touched only Core/Runway (`FREQ.6D.10`'s own §3/§6 explicitly confirm both LongHorizon call sites to `TenKPreparationRunwayNumericPolicyFactory.Build()` are untouched).

---

## 4. Current LongHorizon data flow — `LONGHORIZON_CURRENT_DATAFLOW`

Re-traced directly against current source:

```
LongHorizonPublicPlanService (preview + confirm)                    [RollingActivation/PublicPreview/LongHorizonPublicPlanService.cs]
  → LongHorizonCompositionResolver.Resolve                          [LongHorizonCompositionResolver.cs:48-75]
      21-52wk → GE = AvailableFullWeeks-20, Runway FIXED 8wk, Core FIXED 12wk
  → LongHorizonRollingInitialActivationRuntime — first-window activation
      InputValidator hardcodes DaysPerWeek==4                       [LongHorizonRollingInitialActivationContracts.cs:105]
  → LongHorizonRollingJitActivationRuntime (≤4-week rolling cap)
      → LongHorizonRollingJitCompositionOrchestrator
          → LongHorizonRollingCoreGenerationInputAdapter — DaysPerWeek=4 hardcoded (x2)
          → (when Core needed) TenKPreparationRunwayDarkOrchestratorFactory — SAME orchestrator Runway itself uses
          → BuildBoundedCoreSelection — GroupBy(s => s.StructuralRole, ...) [LongHorizonRollingJitCompositionOrchestrator.cs:228-229]
              flattens real CatalogPrescribedSession (which DOES carry LaneOrdinal/ProgressionStageKey)
              into LongHorizonSessionPrescriptionReference (which does NOT)
  → LongHorizonRollingCheckpointRuntime — reassessment; DaysPerWeek==4 hard gate [line 368]
  → NextWindowLoadDecisionPolicy.Evaluate — already frequency-generic (§21 below)
  → ScheduleRepairRuntimeOrchestrator / ScheduleRepairSpacingValidator — role/date model only, no lane awareness
  → Persistence: LongHorizonRollingStateRepository → LongHorizonRollingSessionState
      no LaneOrdinal / ProgressionStageKey / PrescriptionProfileKey / PrescriptionProfileVersion columns
  → LongHorizonPublicPreviewMapper — public read surface
```

Every class/method cited above was re-confirmed against current source this phase (see §7/§8/§25 for line-level citations); this is not carried forward from `FREQ.6D.5` unverified.

---

## 5. Current rolling-state schema

`LongHorizonRollingSessionState` (`backend/RunningApp.Domain/Entities/LongHorizonRollingSessionState.cs:16-51`) — full field list:

`Id, WeekStateId, SessionOrdinal (int), SessionRole (string), WorkoutKey (string?), WorkoutVersion (int?), DistanceKm (double), AssignedDate (DateOnly), ActivationContextVersionSequence (int), Provenance (string), OutcomeStatus, CompletedAtUtc, ActualDistanceKm, ActualDurationMinutes, NotTodayReason, NotTodayRecordedAtUtc, OutcomeVersion, PlanningStatus, AdaptedFromSessionId (Guid?), Week (navigation)`.

**No `LaneOrdinal`, `ProgressionStageKey`, `PrescriptionProfileKey`, or `PrescriptionProfileVersion` field exists.** Confirmed by direct entity read this phase — `FREQ.6D.5`'s finding #3 (from the new phase's own preflight checklist item 3) holds unchanged.

---

## 6. `FREQ.6D.5` findings reconciliation (preflight items 1-8)

| # | Preflight item | Still true? |
|---|---|---|
| 1 | LongHorizon is 4D/pilot-scoped at multiple independent locations | **Confirmed, and undercounted** — see §7 (15 gates, not ~10) |
| 2 | ~10 `DaysPerWeek==4` assumptions previously identified | **Confirmed as undercounted**: full re-audit found 21 distinct lines across 14 files (15 substantive `DaysPerWeek`/candidate-literal occurrences plus test-only/doc-only/provenance-string occurrences) |
| 3 | `LongHorizonRollingSessionState` lacks LaneOrdinal/ProgressionStageKey/PrescriptionProfileKey/Version | **Confirmed**, all four absent (§5) |
| 4 | JIT composition groups by raw StructuralRole, cannot distinguish two KEY lanes | **Confirmed** — `LongHorizonRollingJitCompositionOrchestrator.cs:228-229`, `GroupBy(s => s.StructuralRole, StringComparer.Ordinal)` |
| 5 | No complete ProfileBacked ExecutionIndex propagation | **Confirmed** — zero references to `ExecutionPrescriptionIndex`/`IPublishedTemplateBundleLoader` anywhere under `RuntimeCatalog/Schedule/LongHorizon/` |
| 6 | 5-session Adaptation severity policy exists and is not itself the primary blocker | **Confirmed** — `NextWindowLoadDecisionPolicy` is real, generic, and KEY-lane-blind by design (§21) |
| 7 | Intermediate×5D 21+ remains non-public/gated | **Confirmed** (§3) |
| 8 | Core 8-14 / Runway 15-20 are separate, proven, must not be redesigned | **Honored** — this design changes nothing about Core or Runway's own structure or numeric authority; it only threads existing identity through LongHorizon |

No `FREQ.6D.5` finding is stale or superseded. `FREQ.6D.6`-`.10` did not touch LongHorizon.

---

## 7. Current 4D-only gates — `LONGHORIZON_CARDINALITY_HARDCODING_TABLE` / classification

Full re-audit (21 lines, 14 files, `RuntimeCatalog/Schedule/LongHorizon/` only):

| File:Line | Match | Classification |
|---|---|---|
| `LongHorizonDarkExecutionOrchestrator.cs:48` | `RUN_LAYOUT_4D` literal | `LEGACY_PILOT_GUARD` (dark, unwired) |
| `LongHorizonFullNumericOrchestrator.cs:269` | `DaysPerWeek = 4` | `LEGACY_PILOT_GUARD` (dark, unwired) |
| `LongHorizonStructuralMaterializer.cs:39` | `CandidateKey` const | `LEGACY_PILOT_GUARD` (dark) |
| `LongHorizonStructuralMaterializer.cs:52` | `RUN_LAYOUT_4D` | `LEGACY_PILOT_GUARD` (dark) |
| `LongHorizonStructuralMaterializer.cs:261` | `DaysPerWeek = 4` | `LEGACY_PILOT_GUARD` (dark) |
| `LongHorizonStructuralSkeletonContracts.cs:55` | doc comment only | `TEST_ONLY`-equivalent (no executable effect) |
| `LongHorizonFullDarkLifecycleHarness.cs:52,481` | `DaysPerWeek = 4` | `TEST_ONLY` |
| `LongHorizonRollingCheckpointRuntime.cs:368` | `!= 4` reject | `JIT_ALGORITHM_ASSUMPTION` (hard validation gate) |
| `LongHorizonRollingCoreGenerationInputAdapter.cs:43,68` | `DaysPerWeek = 4` ×2 | `JIT_ALGORITHM_ASSUMPTION` (feeds `GeneratePreviewRequest`/`ResolverInputSnapshot`) |
| `LongHorizonRollingInitialActivationContracts.cs:105` | `!= 4` reject | `SUPPORT_MATRIX_GATE` |
| `LongHorizonRollingJitActivationRuntime.cs:303` | provenance string literal | `LEGITIMATE_4D_SPECIFIC_BEHAVIOR` (label only, no branching) |
| `LongHorizonPublicPlanService.cs:81` | `CandidateKey` const | `SUPPORT_MATRIX_GATE` |
| `LongHorizonPublicPlanService.cs:162,203,319` | `DaysPerWeek = 4` ×3 | `PERSISTENCE_ASSUMPTION` / `JIT_ALGORITHM_ASSUMPTION` (request/mapper/persisted-plan builders) |
| `LongHorizonPublicPlanService.cs:354` | `!= 4` reject | `SUPPORT_MATRIX_GATE` |
| `LongHorizonFutureCoreRefreshOrchestrator.cs:97` | `DaysPerWeek = 4` | `JIT_ALGORITHM_ASSUMPTION` |
| `LongHorizonRollingStateRepository.cs:62` | `DaysPerWeek = 4` | `PERSISTENCE_ASSUMPTION` |
| `LongHorizonRollingRestartContinuationService.cs:65` | `DaysPerWeek = 4` | `JIT_ALGORITHM_ASSUMPTION` |

**Total: 21 lines / 14 files** (vs. `FREQ.6D.5`'s original ~10-site estimate). Breakdown by class: 3 hard `SUPPORT_MATRIX_GATE` validation rejects; 8 `JIT_ALGORITHM_ASSUMPTION`/`PERSISTENCE_ASSUMPTION` literal-value threading sites; 5 `LEGACY_PILOT_GUARD` (dark, never-live code); 2 `TEST_ONLY`; 1 `LEGITIMATE_4D_SPECIFIC_BEHAVIOR` (a label, not a branch); 1 doc-comment-only. **No mechanical delete-all-`==4`** is proposed anywhere in this design — every gate above is individually classified and the implementation-phase plan (§33) addresses each class differently (support-matrix gates dispatch through the generic identity policy per §60; JIT/persistence-assumption sites get layout-derived values per §28/§76).

No `RUN_LAYOUT_4D`-hardcoded site was found that also independently branches on `DaysPerWeek==5` anywhere (confirming §26's "not just ~10" instruction is satisfied without discovering a hidden 5D-specific shortcut already in place).

---

## 8. Current JIT collision trace — `CURRENT_5D_JIT_COLLISION_TRACE`

`LongHorizonRollingJitCompositionOrchestrator.BuildBoundedCoreSelection` (`.cs:219-269`):

```csharp
// line 228-229
var remainingByRole = week.Sessions.OrderBy(s => s.Date).ThenBy(s => s.StructuralRole)
    .GroupBy(s => s.StructuralRole, StringComparer.Ordinal)
```

**Reconstructing a real 5D Core week** (`KEY lane0`, `KEY lane1`, `EASY`, `EASY`, `LONG`) through this code:

1. Core generation produces 5 real `CatalogPrescribedSession` records for the week, each carrying `StructuralRole` ("KEY_SESSION" ×2, "EASY_SUPPORT" ×2, "LONG_RUN" ×1) **and** (per §9 below) `LaneOrdinal` (0 and 1 for the two KEY sessions, null for the others) and `ProgressionStageKey`.
2. `remainingByRole` groups by `s.StructuralRole` — **both** KEY_SESSION sessions land in the **same** dictionary bucket (`"KEY_SESSION" → [session(lane0), session(lane1)]`), ordered only by `Date` within that bucket.
3. Downstream (`.cs:234-244`), each expected structural slot (`datedSlot`) for the week dequeues the *next* candidate from that bucket (`remainingByRole.TryGetValue(datedSlot.StructuralRole, ...)`), FIFO by date — not by lane.
4. `LongHorizonSessionPrescriptionReference` (the type actually constructed at line ~240) carries only `SessionRole = prescribed.StructuralRole`, `WorkoutKey`, `WorkoutVersion`, `DistanceKm` — **`LaneOrdinal` and `ProgressionStageKey` are discarded at this exact boundary**, never copied onto the reference.

**Result:** the two KEY sessions are structurally indistinguishable the instant they leave `BuildBoundedCoreSelection`. If a repair, calendar shift, or the two lanes' `Date` values ever tie or invert (both are legitimate: lane assignment is prescription-time, not date-time — see §22's frozen invariant), the wrong lane's workout/profile could be dequeued into the wrong physical slot, and neither the resulting `LongHorizonRollingSessionState` row nor any downstream consumer could detect or correct it, because the lane identity was never captured to begin with. This is not a hypothetical — it is the literal, unconditional behavior of every 5D week that would ever reach this method today (blocked only by the upstream `DaysPerWeek==4` gates in §7, not by this method itself).

---

## 9. Core reference identity (verified this phase, not carried from memory)

`BoundCatalogSession` (`RuntimeCatalog/Schedule/Binding/BoundCatalogPlanContracts.cs:35-81`) — full field list: `WeekNumber, Date, PhaseKey, ProgressionStageKey (string?), LaneOrdinal (int?), PrescriptionProfileKey (string?), PrescriptionProfileVersion (int?), StructuralRole, WorkoutDefinitionKey, WorkoutDefinitionVersion, BindingMode, BindingPolicyKey/Version, SourceArtifactKey/Version, ConditionOutcome, FallbackOrigin, BindingReason`.

`LaneOrdinal` computation — `CatalogWorkoutBinder.cs:131-150`: zero-based rank over same-role slots ordered by `SlotOrderInWeek` (never date/weekday/workout-identity/dictionary order); for `KEY_SESSION` (the only `StageControlled` role today), lane 0 = PRIMARY, lane 1 = SECONDARY_CONTROLLED; `null` for `FixedDefault` roles (`EASY_SUPPORT`, `LONG_RUN`) — `CatalogWorkoutBinder.cs:284`. `CatalogSessionPrescriptionPlanner.cs:39-45` explicitly documents the binder as "the single source of truth for lane/ordinal identity end-to-end" — it does not recompute.

`ProgressionStageKey` — assigned at `CatalogWorkoutBinder.cs:224` (StageControlled) / `null` at line 283 (FixedDefault), sourced from `ProgressionStageAllocator.AllocatePhase` (`Schedule/Progression/ProgressionStageAllocator.cs:127-282`), which resolves per-stage eligibility against `RuntimeConditionResolutionResult`s and applies phase-level compression/extension — **not deterministically reconstructible from week number alone** (depends on the specific plan's runtime-condition outcomes and phase week-count allocation).

`TrainingDay.CatalogPrescriptionProfileKey`/`Version` (`RunningApp.Domain/Entities/TrainingDay.cs:92-93`) — both `nullable`, no DB-level NOT NULL/CHECK constraint (`AppDbContextModelSnapshot.cs:1125-1129`). The Legacy/ProfileBacked/partial-invalid invariant is enforced **upstream**, on `BoundCatalogSession`, in `CatalogSessionPrescriptionPlanner.ResolvePrescriptionSource` (`.cs:170-187`) — both-null → `Legacy`; both-present → `ProfileBacked`; one-null → throws `CatalogSessionPrescriptionInvalidProfileLineageException`. `CatalogPlanConfirmationService.cs:687-688` copies the already-validated values onto `TrainingDay` verbatim — there is no second, independent check at the persistence boundary.

`ExecutionPrescriptionIndex` (`RuntimeCatalog/Prescription/Execution/ExecutionPrescriptionIndex.cs:24-99`) — `ResolveExact(VersionedCatalogReference profileRef)` keyed on exact `(DocumentType, Key, Version)`; no nearest/latest/first-match — a legacy index throws `ExecutionPrescriptionLegacyIndexException`, a missing exact key throws `ExecutionPrescriptionNotFoundException`. Built via `ExecutionPrescriptionIndex.Build(PublishedTemplateBundle)` (lines 45-74), consumed in the Core pipeline via `CatalogPreviewGenerator.LoadExecutionIndex` (`.cs:959-963`), which is candidate-key/version-parameterized already (its own doc comment: "Exact candidate identity only — never 'latest,' never horizon-specific, never a 4D fallback").

`CatalogSessionPrescriptionSource` (`.cs:22-27`) — abstract-record + two sealed subtypes: `Legacy(CatalogWorkoutPrescription)`, `ProfileBacked(ExecutableWorkoutPrescription)`.

---

## 10. Identity Model A — StructuralRole + LaneOrdinal only

Persist `LaneOrdinal` directly on `LongHorizonRollingSessionState`; key JIT composition on `(WeekNumber, StructuralRole, LaneOrdinal)`. `LaneOrdinal` is `null` for `EASY_SUPPORT`/`LONG_RUN` (per Core's own convention, §9) — so this model does **not** solve repeated-EASY identity (§19) on its own; it only solves dual-KEY.

## 11. Identity Model B — StructuralRole + SlotOrderInWeek, LaneOrdinal derived for progression roles

Persist the catalog-authored `SlotOrderInWeek` (an already-real, already-immutable value the binder itself sorts by, per §9) instead of `LaneOrdinal` directly; derive `LaneOrdinal` for `StageControlled` roles the same way `CatalogWorkoutBinder` does today. Covers both dual-KEY and repeated-EASY with one field, since `SlotOrderInWeek` is defined for every slot, not only progression lanes.

## 12. Identity Model C — Stable `SessionSlotIdentity` value object (role + ordinal), persisted verbatim

A dedicated `(StructuralRole, SlotOrdinal)` pair, computed once at Core-generation/JIT-composition time and treated as immutable prescription/session lineage thereafter (never recomputed from date or dictionary order). Functionally similar to Model B but named as a first-class identity concept rather than reusing the catalog's own `SlotOrderInWeek` field name, making the LongHorizon-owned meaning ("this is the durable identity of this occurrence") explicit and decoupled from any future change to how the catalog authors slot ordering.

---

## 13. Selected identity model

**Model C, specialized as: persist `SlotOrdinal` (an `int`, catalog-`SlotOrderInWeek`-derived, immutable once assigned) alongside `LaneOrdinal` (nullable `int`, populated only for `StageControlled` roles, copied verbatim from `BoundCatalogSession.LaneOrdinal` — never recomputed).**

Rationale (scored against §54's seven criteria):

| Criterion | Model A | Model B | Model C (selected) |
|---|---|---|---|
| Semantic fidelity | Partial (KEY only) | Full | Full |
| Core consistency | Partial (reuses `LaneOrdinal` semantics only) | Full (reuses `SlotOrderInWeek` semantics) | Full — persists both fields Core already computes, under their own Core-established meanings, adding no new derivation rule |
| JIT determinism | Yes for KEY, no for EASY | Yes | Yes |
| Repair safety | Partial | Full | Full — an explicit invariant (§22/§56) that neither field is ever recomputed from date |
| Migration complexity | 1 nullable column | 1 nullable column | 2 nullable columns (marginally larger, still minimal/additive) |
| 4D compatibility | Nullable-safe | Nullable-safe | Nullable-safe (both fields null for every historical 4D row — §35/§36) |
| 6D/7D generality | No (assumes ≤2 lanes is the only repeated case) | Yes | Yes |
| Recommended | No | Close second | **Yes** |

Model C over Model B: keeping `LaneOrdinal` as its own field (rather than deriving it on read from `SlotOrdinal` every time) matches Core's own `BoundCatalogSession` shape exactly (§9) — LongHorizon then genuinely "does not create a second meaning for lane/stage/profile" (§3's own governing constraint), it reuses the identical two-field shape Core already validated, just persisted instead of transient. Model D (persist stage/profile only, infer slot later) is rejected: `ProgressionStageKey` is proven **not** deterministically reconstructible from week number alone (§9), so a model that relies on inferring slot/lane later without ever having stored it cannot guarantee determinism (§48) — it was evaluated and rejected, not silently dropped.

---

## 14. LaneOrdinal persistence decision

**L1 — persist `LaneOrdinal` directly**, copied verbatim from `BoundCatalogSession.LaneOrdinal` at the moment a `LongHorizonRollingSessionState` row is created from a real Core-generated session. Rejected L2 (persist `SlotOrderInWeek` and derive) as the *sole* field, because `LaneOrdinal`'s derivation rule (`StageControlled` roles only, zero-based rank by `SlotOrderInWeek`) is itself owned by `CatalogWorkoutBinder`, not by LongHorizon — re-deriving it a second time inside LongHorizon would violate §3's "do not create a second meaning" constraint. L3 (reconstruct from structural-role ordering each JIT generation) is explicitly rejected per the phase's own instruction: it is exactly the failure mode demonstrated in §8, and cannot be repair-safe (§21's repair audit found zero existing lane-preservation precedent to build on — this must be a persisted invariant, not a runtime recomputation).

---

## 15. Repeated EASY identity decision

**`SlotOrdinal`, a distinct field from `LaneOrdinal`** — not an overload of `LaneOrdinal` beyond its Core-established meaning (§53's explicit warning). `LaneOrdinal` remains `StageControlled`-only (null for `EASY_SUPPORT`/`LONG_RUN`, matching `BoundCatalogSession`, §9); `SlotOrdinal` is populated for every role, sourced from the same `SlotOrderInWeek` value `CatalogWorkoutBinder` already reads (§9), giving deterministic, non-invented identity to repeated `EASY_SUPPORT` occurrences without inventing `EASY_1`/`EASY_2`/`EASY_3` structural-role enum values (explicitly forbidden by §19/§53) and without repeating the existing GE segment's own anti-pattern (`LongHorizonGeStructuralContracts.cs:38-43` hardcodes `EasySupportA`/`EasySupportB` as **separate enum values** — confirmed by direct read this phase; flagged in §74 as an existing instance of exactly the pattern this design avoids going forward, not retroactively fixed here).

---

## 16. Stage lineage decision — `LONGHORIZON_STAGE_LINEAGE_DECISION`

**Persist `ProgressionStageKey` (nullable `string`) verbatim from `BoundCatalogSession.ProgressionStageKey` at row-creation time.** Not deterministically reconstructible later (§9) — stage assignment depends on the specific plan's own runtime-condition-resolver outcomes and phase-level compression/extension, both evaluated once at Core-generation time. Repair/substitution does not currently touch stage at all (§21) — the design in §21 below requires repair to **preserve**, not reassign, this persisted value. Exact profile candidate bindings depend on stage transitively (a `KEY_SESSION`'s profile selection is scoped by its progression phase/stage in Core's own pipeline) — persisting stage is therefore also a precondition for profile-binding determinism (§18).

---

## 17. Profile lineage decision

**P1 — persist the exact profile pair (`PrescriptionProfileKey string?`, `PrescriptionProfileVersion int?`), copied verbatim from `BoundCatalogSession` at the same row-creation boundary, under the identical both-null-or-both-present invariant `CatalogSessionPrescriptionPlanner.ResolvePrescriptionSource` already enforces (§9).** P3 (persist neither, search runtime catalog) is rejected per the phase's own explicit instruction — runtime profile-version search is forbidden, and would also violate determinism (§48, "no dependence on... current catalog latest version"). P2 (persist stage/lane only, rebind exact profile deterministically during JIT) is rejected because it re-introduces exactly the non-determinism P1 avoids: rebinding at JIT time means the *rebind* — not the original Core generation — becomes authoritative, and nothing guarantees the published bundle available at JIT time is the same one available at original binding time (§13/§42's version-drift concern) unless JIT is explicitly re-scoped to the plan's own frozen bundle version, which P1 already guarantees by storing the exact resolved pair once.

---

## 18. Profile binding lifecycle

**Frozen boundary: (B) when the future window becomes materialized/JIT-composed** — i.e., the moment a real `BoundCatalogSession`/`CatalogPrescribedSession` is produced for that specific future week (today's real Core-generation timing, unchanged), **not** (A) at initial rolling-session creation (which today only creates placeholder rolling state, before any real Core prescription exists for weeks beyond the near-term JIT window) and **not** (C) at `TrainingDay` materialization (too late — repair/adaptation decisions between JIT composition and eventual `TrainingDay` writing need the identity already fixed, per §45's "repair before materialization" requirement).

Effect on adaptation: none — adaptation (§21) operates on completed/outcome data, never on profile identity.
Effect on catalog version drift: this is exactly what makes §13's rule enforceable — the profile pair is fixed at the JIT-composition instant, using whatever published bundle is current *then*, and never revisited afterward for that specific session (see §13 for the frozen-vs-live-bundle distinction this implies for the plan-level bundle authority, §18-19 of the phase prompt).
Effect on bundle availability: requires the plan to retain enough identity to always resolve the *same* candidate/bundle at JIT time across the plan's full multi-month life — addressed in §19 below.

---

## 19. Version-drift rule / plan-level bundle authority

**Rule: a LongHorizon plan is created against one immutable `(CandidateKey, CandidateVersion)` pair — the exact combination `V1CatalogPilotIdentityPolicy.ResolveCandidate(level, daysPerWeek)` returns at plan-creation time — and every future JIT window resolves its `ExecutionPrescriptionIndex` from that exact, unchanging pair for the plan's entire lifetime.** Not-yet-materialized sessions do **not** bind against "whatever the catalog currently contains" — they bind against the plan's own frozen candidate identity, exactly mirroring how `CatalogPreviewGenerator.LoadExecutionIndex` already resolves an exact `(candidate.CandidateKey, candidate.CandidateVersion)` pair (§9) rather than "latest." No new mechanism is required to *express* this rule — `TrainingPlan` (or the LongHorizon-specific plan aggregate) already persists the confirmed candidate's key/version at confirmation time (existing, pre-LongHorizon behavior); the requirement is only that LongHorizon's own JIT composition **read that persisted pair back** to construct its `ExecutionPrescriptionIndex`, rather than re-resolving `V1CatalogPilotIdentityPolicy.ResolveCandidate` fresh at JIT time (which could theoretically observe a different result if the identity policy itself were ever changed — an explicit anti-pattern this rule forecloses).

This is the "plan-level bundle authority" required by §14 of the phase prompt: the plan's own persisted `(CandidateKey, CandidateVersion)` **is** the minimal required lineage — no additional plan-level field is needed beyond what already exists, since `PublishedTemplateBundleLoader` resolves a bundle from exactly that pair (§9).

---

## 20. ExecutionIndex design and lifecycle

**Target path** (mirrors §15/§16 of the phase prompt exactly): plan's persisted `(CandidateKey, CandidateVersion)` → `IPublishedTemplateBundleLoader.TryLoadAsync` → `PublishedTemplateBundle` → `ExecutionPrescriptionIndex.Build` → JIT materialization (`TenKPreparationRunwayDarkOrchestratorFactory`/Core generation, unchanged) → `CatalogSessionPrescriptionPlanner.ResolveExact` (unchanged). No runtime profile projection; no WorkoutDefinition-driven reconstruction — both already forbidden by the existing `ExecutionPrescriptionIndex.ResolveExact` contract (§9).

**When to build it — Option B selected: cached on an immutable runtime generation context, built once per LongHorizon request/window-materialization call.** Not (A) "once per request" as a separate concept from (B) — they collapse to the same thing for LongHorizon's actual call shape (one JIT window materialization = one request-scoped operation). Rejected (C) per-session as wasteful (`Build` validates every `ExecutableWorkoutPrescription` in the bundle up front — §9 — repeating that per session inside a window is pure duplicated work for an already-in-memory bundle). Rejected (D) persisted: no evidence anywhere in the repository suggests execution content belongs in the database — Core's own architecture treats it as bundle-derived, never persisted (§43's own governing principle, already established practice, not a new decision this phase invents).

**Concrete integration point**: `TenKPreparationRunwayDarkOrchestrator` — the shared orchestrator both Runway's Core-handoff and LongHorizon's Core-entry already call (§9's `TenKPreparationRunwayDarkOrchestratorFactory` reference, confirmed still the live LongHorizon Core-entry path) — needs the same `ExecutionPrescriptionIndex`/`IPublishedTemplateBundleLoader` wiring `CatalogPreviewGenerator` already has. This was `FREQ.6D.5`'s own L-D3/R-D-adjacent finding and remains open; this design confirms it as the correct, single integration point (rather than adding a second, LongHorizon-specific execution-index construction path), directly satisfying §18's "no LongHorizon-specific prescription engine" instruction.

---

## 21. Repair lineage

**Audit result: zero existing precedent.** Every repair/substitution/reschedule file under `RuntimeCatalog/Schedule/LongHorizon/Adaptation/` (`ScheduleRepairPolicy`, `ScheduleRepairRuntimeOrchestrator`, `ScheduleRepairCandidateProvider`, `ScheduleRepairPersistenceService`, `ScheduleRepairSpacingValidator`, `CandidateSelectionPolicy`, `ReasonClassificationPolicy`, `AdaptationSessionRoleResolver`, `RuntimeNotTodayReasonMapper`) was searched for `LaneOrdinal`/`ProgressionStageKey` this phase — **zero matches**. Today's repair flows are role-blind by construction (operating on `CatalogStructuralRole`/`CatalogSlotRole` only), consistent with adaptation's own KEY-lane severity-equivalence (§24). This means lane/stage preservation through repair must be designed net-new, not extended from an existing mechanism.

**Design rule** (new, narrow, additive): every repair/substitution operation that mutates a `LongHorizonRollingSessionState` row (move, reschedule, `SubstituteFutureEasy`, any future repair type) **must copy `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/`PrescriptionProfileKey`/`PrescriptionProfileVersion` unchanged from the row being repaired onto its replacement** — the repair changes `AssignedDate`/`WorkoutKey`/`DistanceKm`/`AdaptedFromSessionId` (exactly as today), never identity. This mirrors `FREQ.6D.1B`'s already-established Core-side principle (referenced by the phase prompt itself: "preserving TrainingDay lane/stage and using original role in severity aggregation") — extending that principle to LongHorizon's persisted rows is the correct, narrow generalization, not a new precedent. A repaired secondary-KEY (lane 1) session can never become primary (lane 0) through repair, because repair never touches `LaneOrdinal` at all.

---

## 22. Lane-vs-date invariant

**Frozen, unconditionally: `LaneOrdinal`/`SlotOrdinal` are prescription/session lineage, assigned once at JIT-composition/binding time (§18) and never recomputed from `AssignedDate` or any calendar-materialization outcome.** Calendar materialization (today's existing `CatalogWeekSkeletonCalendarMaterializer`, unchanged) can move a session's date without touching its lane identity; a future LongHorizon repair that reschedules a session's date must not, and under §21's design rule cannot, alter its lane. This directly resolves the `CURRENT_5D_JIT_COLLISION_TRACE` failure mode in §8 — the whole reason that collision is possible today is that `BuildBoundedCoreSelection`'s FIFO-by-date dequeue effectively (if unintentionally) *is* a date-based lane reconstruction; the fix (§28/§77) is to key composition on `(StructuralRole, LaneOrdinal, SlotOrdinal)` instead of `(StructuralRole, Date-order)`, eliminating date as an identity signal entirely.

---

## 23. Adaptation reuse

**Confirmed generic and reachable-once-upstream-produces-5-session-shape — no new policy needed.** `NextWindowLoadDecisionPolicy.DetermineFiveSessionLoadDecision` (`Adaptation/NextWindowLoadDecisionPolicy.cs:75-88`), quoted verbatim this phase:

```csharp
return summary.EffectiveCompletedCount switch
{
    0 or 1 => NextWindowLoadDecision.Reduce,
    2 or 3 => NextWindowLoadDecision.Maintain,
    4 => OnlyEasyMissing(summary) ? NextWindowLoadDecision.ProgressAsPlanned : NextWindowLoadDecision.Maintain,
    5 => NextWindowLoadDecision.ProgressAsPlanned,
    _ => throw new AdaptationLineageInvalidException(...),
};
```

Mapping: 5/5 → Progress; 4/5 with only EASY missing → Progress; 4/5 with a KEY or LONG missing → Maintain (both KEY lanes route through the same `KeySessionCompletedCount == KeySessionExpectedCount` aggregate check in `OnlyEasyMissing`, lines 131-144 — no per-lane branch exists); 2-3/5 → Maintain; 0-1/5 → Reduce. Exactly the table the phase prompt specifies. **A future 5-session window can consume this without new policy** — the only missing piece is that nothing upstream currently *produces* a real 5-session `WindowExecutionSummary` for LongHorizon (blocked by §7's gates, not by this policy).

---

## 24. Adherence severity vs. prescription identity — explicit separation

**ADHERENCE SEVERITY → role-level equivalence.** `NextWindowLoadDecisionPolicy`'s own doc comment (lines 62-73, quoted): "`FREQ.6` itself states both KEY lanes are severity-equivalent... the aggregate count is sufficient and keeps Adaptation lane-blind by construction." This is an existing, approved product decision — not reopened or reinterpreted here.

**PRESCRIPTION IDENTITY → lane-specific.** Everything in §13-§22 above is lane-specific by design. These are deliberately different axes operating at different layers: Adaptation reads `WindowExecutionSummary` (role-aggregated); JIT composition and persistence read/write `LaneOrdinal` (lane-specific). Neither axis needs to know about the other's granularity — Adaptation's `NextWindowLoadDecision` output (Progress/Maintain/Reduce) feeds forward into the *next* window's volume target, which then flows through the same lane-aware JIT composition this design specifies, without Adaptation itself ever inspecting a lane.

---

## 25. Pre-Core weekly-structure authority — Runway relationship and product-decision boundary

**Actual architecture (not assumed): LongHorizon = GE (variable weeks) + Preparation Runway (FIXED 8 weeks, reused unmodified) + Core (FIXED 12 weeks, reused unmodified)** — confirmed directly from `LongHorizonCompositionResolver.LongHorizon` (`.cs:125-147`, quoted): `GeneralEnduranceWeeks = AvailableFullWeeks - 20`, `PreparationRunwayWeeks = LongHorizonPreparationRunwayWeeks` (const `8`), `CoreWeeks = LongHorizonCoreWeeks` (const `12`) — for every 21-52 week plan, unconditionally. This directly answers §30/§31 of the phase prompt: **model (A)** — LongHorizon does hand into Preparation Runway then Core, and does **not** "directly create rolling preparation weeks" as a separate mechanism from Runway. The 21+ segment is **GE, then the real Runway pipeline (the same one `FREQ.6D.6`-`.10` already made 5D-capable), then the real Core pipeline** — not "prepend extra Runway weeks" as an approximation, but a literal architectural fact already encoded in the composition resolver.

**Split classification, per segment:**

- **Runway segment (8 fixed weeks) → `EXISTING_AUTHORITY_APPLIES`.** LongHorizon's Runway portion is the *same* `TenKPreparationRunwayDarkOrchestrator` pipeline Preparation Runway itself uses (§9's Runway-Core-handoff confirmation, plus `FREQ.6D.5` §18's own finding that LongHorizon's live Core-entry already invokes the same orchestrator factory). `FREQ.6D.6`'s approved 1 KEY + 3 EASY + 1 LONG weekly structure, and `FREQ.6D.10`'s now-wired 26.0km/19.5km starting-volume authority, apply to LongHorizon's Runway segment automatically once the gates in §7 are relaxed to admit a 5D candidate — **no new product decision is required for this segment.**
- **GE segment (variable weeks) → `NEW_PRODUCT_DECISION_REQUIRED`.** GE is a distinct, LongHorizon-only pre-Runway segment with its own dark, never-live catalog artifact (`plan-catalog/catalog/long-horizon-progressions/ten-k-long-horizon-ge-stage-families.v1.json`, `DRAFT`, explicitly not loaded at runtime — `FREQ.6D.5` §16, re-confirmed unchanged this phase) and its own hand-mirrored role model (`LongHorizonGeWeekRole` enum: `KeySession, EasySupportA, EasySupportB, LongRun` — confirmed this phase, `LongHorizonGeStructuralContracts.cs:38-43`) that is **structurally 4-session/4D-shaped by construction**, with no 5D variant authored anywhere. Per §72 of the phase prompt: **STOP architecture closure at this boundary for the GE segment specifically.** This design does not infer a 5-session GE structure from Runway's approved 1K+3E+1L shape, nor from Core's 2K+2E+1L shape — GE's own product semantics (its doc-comment self-description as covering "readiness buildup" phases: `ENTRY`/`BASE_DEVELOPMENT`/`AEROBIC_DURABILITY`/`CONSOLIDATION`/`PRE_RUNWAY_ALIGNMENT`) are plausibly frequency-invariant in *intent* but not proven so, and no evidence establishes whether a 5D GE week should run 5 sessions (mirroring Core/Runway), a reduced frequency, or GE's own existing 4-session shape unchanged. **Flagged as a narrow follow-up product-decision phase**, not resolved here.

---

## 26. LongHorizon numeric-authority result

| Value | Classification |
|---|---|
| Volume-ramp ratios/caps/long-run shares (`VolumeSafetyPolicy.Default`, reused by GE's numeric executor) | `4D_SPECIFIC` today (GE's numeric executor has no 5D-aware dispatch — it calls the same `LongHorizonGeNumericExecutor` regardless of candidate) — becomes `5D_EXISTING_AUTHORITY` automatically for the Runway/Core segments once §25's Runway-segment finding is wired (they already resolve through the same `VolumeSafetyPolicy.FiveDayIntermediate`/`V1FiveDayIntermediateMissingReadinessStartingVolumePolicy` `FREQ.6D.10` just implemented) |
| `RecoveryVolumeRatio=0.85`, `MinimumRecoveryReductionKm=0.5` (GE-specific) | `DECISION_REQUIRED` — documented rationale exists ("15% reduction") but was never evidenced for a 5D GE segment specifically; inherits the same GE-segment product-decision boundary as §25 |
| Final per-session split (`FourDaySessionDistanceAllocationPolicy.Allocate`, GE-specific) | `4D_SPECIFIC` — no 5-session/dual-KEY variant exists; blocked on the same GE product decision (§25) before a numeric question even arises |
| Core-segment numeric authority (once Core begins, via Runway→Core handoff) | `5D_EXISTING_AUTHORITY` — `FREQ.6C`/`FREQ.6D.9`/`FREQ.6D.10`, already wired, reachable once §7's gates admit a 5D candidate |
| Runway-segment numeric authority | `5D_EXISTING_AUTHORITY` — same `FREQ.6D.10` wiring, same reasoning as Core |

**No new number is proposed anywhere in this section.** GE's own numeric authority is entirely gated behind §25's GE product decision — this design does not extrapolate 4D's `0.85`/`0.5` GE-recovery values onto a hypothetical 5D GE segment.

---

## 27. Schema design — `LONGHORIZON_ROLLING_STATE_SCHEMA_DECISION`

| Field | Persist? | Nullable? | Authority | Assigned when? | Can derive? | Historical behavior | Validation |
|---|---|---|---|---|---|---|---|
| `LaneOrdinal` (`int?`) | Yes | Yes | `BoundCatalogSession.LaneOrdinal`, copied verbatim | JIT composition / binding time (§18) | No (must be captured at binding time — §14) | `null` for every historical 4D row (never populated retroactively — §36) | Non-null only for `StageControlled` roles (`KEY_SESSION` today); repair must not modify (§21/§22) |
| `SlotOrdinal` (`int?`) | Yes | Yes (see §40) | `SlotOrderInWeek` (the value `CatalogWorkoutBinder` already reads), copied verbatim | Same as `LaneOrdinal` | No | `null` for historical rows lacking it | Populated for every role including repeated `EASY_SUPPORT` (§15/§40); repair must not modify |
| `ProgressionStageKey` (`string?`) | Yes | Yes | `BoundCatalogSession.ProgressionStageKey` | Same | No (proven non-reconstructible, §9/§16) | `null` for historical rows and for any session type that legitimately has no stage (§39) | Repair must not modify (§21) |
| `PrescriptionProfileKey` (`string?`) | Yes | Yes | `BoundCatalogSession.PrescriptionProfileKey` | Same | No | `null` for historical (Legacy) rows | Both-null-or-both-present with `PrescriptionProfileVersion` (§38) |
| `PrescriptionProfileVersion` (`int?`) | Yes | Yes | `BoundCatalogSession.PrescriptionProfileVersion` | Same | No | `null` for historical rows | Same pair-invariant as above |

**Minimal set: exactly these five nullable columns.** No `ExecutionIndex`/execution-content field is added (§43) — execution content stays bundle-derived per §20.

---

## 28. JIT target algorithm — `LONGHORIZON_JIT_COMPOSITION_DECISION`

**Session key: `(StructuralRole, LaneOrdinal, SlotOrdinal)`** — not `StructuralRole` alone (§8's proven collision), and not over-keyed with additional dimensions: `ProgressionStageKey`/source-rolling-session-ID were evaluated (§18 of the phase prompt) and rejected as *composition keys* specifically (they remain persisted lineage per §16/§27, but are not needed to *distinguish* which slot a given real session fills within a week — `(StructuralRole, LaneOrdinal, SlotOrdinal)` alone is already unique per week by construction, since `SlotOrdinal` is a zero-based rank over every slot in the week, not just same-role slots).

Concretely, `BuildBoundedCoreSelection`'s `GroupBy(s => s.StructuralRole, ...)` (§8) generalizes to `GroupBy(s => (s.StructuralRole, s.LaneOrdinal, s.SlotOrdinal))`, with each resulting group now containing at most one real session (eliminating the FIFO-by-date dequeue entirely — no ambiguity remains to resolve by date order). `LongHorizonSessionPrescriptionReference` (§8) gains the same two nullable fields (`LaneOrdinal`, `SlotOrdinal`) alongside the existing `SessionRole`/`WorkoutKey`/`WorkoutVersion`/`DistanceKm`, carried through unchanged from the real `CatalogPrescribedSession` rather than discarded.

Repeated-EASY handling: identical mechanism — `SlotOrdinal` alone (since `LaneOrdinal` is null for `EASY_SUPPORT`) already disambiguates three future EASY slots deterministically.

Stage resolution / profile binding / bundle execution resolution: unchanged from Core's own existing mechanism (§9/§20) — JIT composition's only new responsibility is *carrying* these fields through, not computing them.

---

## 29. Core-entry reuse design

**Confirmed and reaffirmed as the correct target, not a new decision**: LongHorizon's Core-entry already invokes `TenKPreparationRunwayDarkOrchestratorFactory` — the same orchestrator Preparation Runway itself uses (§9, `FREQ.6D.5` §18). This design does not introduce a LongHorizon-specific duplicate Core engine; it threads the identity model (§13) and execution-context wiring (§20) through that *same* shared orchestrator, so both Runway's and LongHorizon's Core-entries benefit identically and simultaneously — resolving `FREQ.6D.5`'s L-D3 finding for both call sites at once, per its own recommended-sequence note ("Runway needs it too and LongHorizon will inherit it").

`FREQ.6D.5`'s separate L-D5 question (whether to eliminate the `TenKPreparationRunwayDarkOrchestrator` duplication entirely in favor of both callers invoking `CatalogPreviewGenerator`'s own Core-entry method directly) is **explicitly deferred, not decided here** — it is a real, independent architecture-debt question (recurrence risk: future Core-pipeline fixes need re-applying at each duplicate entry point) but resolving it is not a precondition for closing this phase's actual scope (dual-KEY lineage/JIT/persistence/execution-context). Recorded as a technical-debt item (§74), not blocking.

---

## 30. Runway relationship

Answered fully in §25 — **model (A)**: LongHorizon hands GE output into the real Preparation Runway pipeline, then into the real Core pipeline. Not a separate rolling-preparation mechanism.

---

## 31. Numeric authority (see §26)

---

## 32. Product decision check

**GE segment weekly structure and GE-specific numeric coefficients → `NEW_PRODUCT_DECISION_REQUIRED`** (§25/§26). **Runway and Core segments → `EXISTING_AUTHORITY_APPLIES`** (already approved by `FREQ.6D.6`/`FREQ.6C`/`FREQ.6D.9`/`FREQ.6D.10`, reachable automatically once §33's engineering splits land).

---

## 33. Implementation splits

Evaluated against the phase prompt's illustrative A-F decomposition; reordered/merged per actual dependencies found this phase:

- **SPLIT A — Persistent rolling-session identity schema + migration.** Adds the five nullable columns (§27) to `LongHorizonRollingSessionState`. Depends on: nothing (pure additive schema). Independently verifiable: yes (migration applies/rolls back cleanly against existing historical rows, all remain null — §35/§36 test plan, §66). Public effect: none (columns unused until Split B/C land). Rollback safety: additive-nullable, trivially reversible.
- **SPLIT B — JIT dual-KEY/repeated-role composition generalization.** Changes `BuildBoundedCoreSelection`'s grouping key (§28) and `LongHorizonSessionPrescriptionReference`'s shape to carry `LaneOrdinal`/`SlotOrdinal`. **Depends on Split A** (the fields must exist to persist through to `LongHorizonRollingSessionState`, even though the composition-key fix itself operates on in-memory `CatalogPrescribedSession`/`LongHorizonSessionPrescriptionReference` objects that predate persistence — see §65). Independently verifiable via dark tests (§67) before any real 5D candidate can reach it (still blocked by Split D's gates). No public effect on its own.
- **SPLIT C — Exact profile/bundle ExecutionIndex propagation.** Wires `IPublishedTemplateBundleLoader`/`ExecutionPrescriptionIndex` into `TenKPreparationRunwayDarkOrchestrator` (§20/§29). Independent of Split A/B (a pure execution-context threading change, benefits 4D too in principle — §29's L-D3 framing). No public effect on its own; also benefits Runway's existing 4D/5D public paths defensively (closes the same latent gap `FREQ.6D.4D.5G` fixed for Core's own dynamic branches, at the one call site that fix never reached — `FREQ.6D.5` §15).
- **SPLIT D — LongHorizon cardinality/support-gate generalization.** Relaxes the 3 hard `SUPPORT_MATRIX_GATE` rejects (§7) plus the `JIT_ALGORITHM_ASSUMPTION`/`PERSISTENCE_ASSUMPTION` literal-value sites, dispatching through `V1CatalogPilotIdentityPolicy`/a supported-combination manifest (§60) instead of raw `DaysPerWeek` literals, with `DaysPerWeek` itself derived from the resolved `RunLayout` (§9's `runLayout.StructuralRoles.Count` pattern) rather than hand-set. **Depends on Split A+B+C** (relaxing the gates without the identity/execution-context work already landing would let a 5D request reach the exact collision in §8, or a missing-execution-index failure). Also depends on §25's GE product decision being at least provisionally scoped — a 5D LongHorizon request cannot fully compose (GE segment) without it, even if Runway/Core segments are individually ready.
- **SPLIT E — Dark 5D rolling/adaptation verification.** No production activation; proves Splits A-D together produce correct dark output for representative 21/24/32/52-week plans (§67). Depends on A-D.
- **SPLIT F — Public activation + real PostgreSQL E2E.** Depends on E succeeding, and on §25's GE product decision being **fully resolved** (not merely scoped) — public activation cannot ship a plan type whose GE segment has no approved weekly structure.

No split was merged or reordered beyond making dependencies explicit; the prompt's illustrative A-F shape holds.

---

## 34. Dependency order

```
Split A (schema+migration)
    ↓
Split B (JIT composition) ──┐
Split C (execution-index)   ├─→ Split D (gate relaxation) ─→ Split E (dark verify) ─→ Split F (public + DB E2E)
                             │        ↑
                    GE product decision (narrow follow-up phase, §25) — required before D can
                    fully relax gates for the complete 21-52wk LongHorizon shape (Runway/Core
                    segments alone could theoretically dark-verify without it, but full public
                    activation cannot)
```

Split A is the only true "must land before JIT generalization" prerequisite (§65 — schema migration is a prerequisite because dual-KEY identity cannot survive persistence without the new columns, and critical lineage must never be kept memory-only in production). Splits B and C are mutually independent and could be implemented in either order or in parallel. Split D cannot begin meaningfully until B+C exist (relaxing gates first would expose the exact §8 collision). Split F requires both E's dark-verification success **and** §25's GE decision being resolved as a distinct, separately-tracked precondition — not a phase of this implementation sequence itself.

---

## 35. Backward-compatibility test plan (future, not run this phase — design only)

For existing 4D LongHorizon plans, post-Split-A/D:
- Historical DB row load: every existing `LongHorizonRollingSessionState` row loads with all five new columns `null`, no read-path exception.
- Rolling next-window generation for an existing 4D plan: byte-identical output to pre-change behavior (the new fields are populated but never consumed by 4D's own unchanged `FourDaySessionDistanceAllocationPolicy`/GE numeric path).
- Repair (move/reschedule/substitute) on an existing 4D row: unaffected — §21's copy-through rule is a no-op when all five fields are already null.
- Adaptation: unaffected (§23/§24 — role-aggregated, never lane-specific).
- Confirmation: unaffected.
- Home/calendar/detail reads: unaffected (public mapper never surfaces the new internal fields).

## 36. Backfill decision

**No backfill.** Per §36 of the phase prompt: historical 4D rows remain legitimately `null` for all five new columns — none of `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/`PrescriptionProfileKey`/`Version` is deterministically recoverable for a row that was never generated with that lineage captured (mirroring `TrainingDay`'s own established Legacy-vs-ProfileBacked precedent, §9). Runtime interprets a null-lineage row through existing Legacy semantics unchanged.

## 37. DB constraint decision

- **Application-level validation** (not a DB constraint): the both-null-or-both-present `PrescriptionProfileKey`/`Version` pair invariant — mirrors exactly where Core enforces it today (`CatalogSessionPrescriptionPlanner.ResolvePrescriptionSource`, an application-layer check, not a DB `CHECK` constraint — §9 confirmed no DB-level constraint exists for the equivalent `TrainingDay` columns either). Keeping the same enforcement layer avoids introducing an inconsistency between how `TrainingDay` and `LongHorizonRollingSessionState` express the identical invariant.
- **No new DB index** proposed — none of the five columns is used in a `WHERE`/`ORDER BY` hot path distinct from the existing `WeekStateId`/`SessionOrdinal` access pattern; adding one without an evidenced query-performance need would be premature.
- **No DB-level uniqueness constraint** on `(WeekStateId, StructuralRole/SessionRole, LaneOrdinal, SlotOrdinal)` is proposed at the schema level — the uniqueness invariant (§56) is enforced at JIT-composition/write time in application code, consistent with how the rest of this persisted-state model already validates shape (`ValidateFiveSessionSummary`-style fail-closed checks, §23, rather than DB constraints).

---

## 38. Partial-profile validation

Reuses the `TrainingDay`/`BoundCatalogSession` invariant verbatim (§9/§27): both `PrescriptionProfileKey`/`Version` null → Legacy; both non-null → ProfileBacked; one-null-one-present → invalid, fail-closed with a typed exception at the write boundary (mirroring `CatalogSessionPrescriptionInvalidProfileLineageException`'s existing message/failure shape — a new, LongHorizon-scoped exception type with the identical structure, not a generic 500, per §71).

---

## 39. Stage nullability

`ProgressionStageKey` is legitimately `null` for `EASY_SUPPORT`/`LONG_RUN` sessions (`FixedDefault` roles, per Core's own established convention — §9, `CatalogWorkoutBinder.cs:283`), and for every historical 4D row (§36). It is **not** globally required non-null — this design does not force a stage value onto fixed-default sessions where none exists in the domain model today.

## 40. Lane nullability

`LaneOrdinal` applies only to progression/`StageControlled` roles (`KEY_SESSION` today) — `null` for `LONG_RUN` and `EASY_SUPPORT`, exactly matching `BoundCatalogSession`'s own established meaning (§9, §15). `SlotOrdinal`, by contrast, is populated for **every** role including `LONG_RUN`/unique slots — it is not meaningless there, it simply always resolves to that role's unique position in the week (§15/§28); no lane value is ever forced onto a role for which lane has no meaning.

---

## 41. Plan version / replay

Minimum necessary provenance (per §41 of the phase prompt): the plan's own persisted `(CandidateKey, CandidateVersion)` pair (§19 — already exists, no new field) **plus** the per-session `(PrescriptionProfileKey, PrescriptionProfileVersion)` pair persisted at JIT-composition time (§17/§27, new). Together these are sufficient: candidate/version resolves the exact bundle (§19); the exact session profile refs resolve the exact execution prescription within that bundle (§9's `ResolveExact`, no nearest/latest). No published-bundle **hash** is required in addition to `(CandidateKey, CandidateVersion)` — that pair is already the bundle's own identity key in `IPublishedTemplateBundleLoader.TryLoadAsync`'s existing contract (§9); a hash would be redundant provenance, not additional determinism.

## 42. Bundle availability failure

**Fail-closed, no silent latest-resolution.** If a JIT window for an active plan references a `(CandidateKey, CandidateVersion)` no longer resolvable via `IPublishedTemplateBundleLoader.TryLoadAsync` (returns null), the design requires a distinct typed failure (§71 — e.g. `LongHorizonPublishedBundleUnavailableException`), not a fallback to whatever version is currently latest. This phase found no repository-wide bundle-retention guarantee documented (published bundles are versioned artifacts under `plan-catalog/artifacts/appsel-plan-catalog/<version>/`, retained by ordinary repository/release history, but no explicit "old versions are never deleted" policy statement was located) — so the fail-closed behavior is the safe default absent such a guarantee, not a redundant belt-and-suspenders addition.

---

## 43. Execution content persistence

**Confirmed preserved: `BUNDLE_ONLY`.** No execution-prescription content or hash is added to `LongHorizonRollingSessionState` (§27's schema table has no such field) — only the exact profile *reference* (`Key`/`Version`) is persisted; the actual `ExecutableWorkoutPrescription` content is re-resolved from the bundle via `ExecutionPrescriptionIndex.ResolveExact` (§9/§20) every time it's needed, exactly as Core already does. This matches existing architecture and was not weakened by this design.

---

## 44. JIT materialization boundary

At the exact boundary where a rolling-state row becomes a real, dated `TrainingDay`/bound session:

- **Must already exist**: `StructuralRole`, `LaneOrdinal`/`SlotOrdinal` (assigned at JIT-composition time, §18/§28), `ProgressionStageKey` (assigned at the same time, §16), the plan's own frozen `(CandidateKey, CandidateVersion)` (§19, exists from confirmation time).
- **Resolved at this boundary**: `PrescriptionProfileKey`/`Version` (already persisted from JIT composition, §17 — "resolved" here means *read back*, not re-derived), the exact `ExecutableWorkoutPrescription` content (via `ResolveExact` against the bundle, §9/§20 — read from bundle, never persisted).
- **Persisted at this boundary**: the `TrainingDay` row itself, with `CatalogPrescriptionProfileKey`/`Version`/`CatalogProgressionStageKey` copied verbatim from the already-resolved `LongHorizonRollingSessionState` row (mirroring `CatalogPlanConfirmationService.cs:687-688`'s existing "copied verbatim, never independently looked up" pattern, §9) — `LaneOrdinal`/`SlotOrdinal` are **not** persisted onto `TrainingDay` itself, matching `TrainingDay.cs:82-86`'s existing documented design (`LaneOrdinal` deliberately not persisted there because it's reconstructible from `(CatalogProgressionStageKey, progression key+version)` — confirmed by direct read this phase). This is a genuine, deliberate asymmetry between `LongHorizonRollingSessionState` (needs `LaneOrdinal`/`SlotOrdinal` because it is the only place identity survives *before* a `TrainingDay` exists) and `TrainingDay` (does not need them, per its own already-established design) — not an oversight.
- **Read from bundle**: execution prescription content (§43).

---

## 45. Repair before materialization

**Yes — future rolling sessions can be repaired before becoming `TrainingDay`s** (the whole point of the rolling/JIT model is that future weeks exist only as `LongHorizonRollingSessionState` until materialized). Per §21's design rule, the identity model (§13) must and does survive this: repair operates on `LongHorizonRollingSessionState` rows directly, and §21's copy-through rule applies identically whether the row being repaired is already `TrainingDay`-materialized or still purely rolling state. No lineage in this design is based solely on `TrainingDay` fields — every identity field lives on `LongHorizonRollingSessionState` first (§27), `TrainingDay` only receives a subset at materialization time (§44).

## 46. Repair after materialization

For concrete `TrainingDay`s, this design reuses the already-proven Core persistence/repair semantics unchanged — no separate LongHorizon-5D repair mechanism is introduced for the post-materialization case. (Pre-materialization repair, §45, is the only genuinely new surface, because `LongHorizonRollingSessionState` itself is LongHorizon-specific and has no Core equivalent to reuse.)

---

## 47. Week/window identity

**`WeekNumber` alone is not stable enough once adaptation can shift/compress/replace future weeks** (§47 of the phase prompt's own framing, confirmed applicable: `NextWindowLoadDecisionPolicy`'s `Reduce`/`Maintain`/`Progress` outcomes (§23) can change what a "future window" contains between when it was first rolling-materialized and when it is finally JIT-composed). This design does not introduce a new stable window/session ID scheme beyond what already exists — `LongHorizonRollingSessionState.Id` (a `Guid`, §5) and `LongHorizonRollingWeekState`'s own identity (the `WeekStateId` foreign key, §5) are already stable, logical (not calendar-week) identifiers, unaffected by `AssignedDate` changes. The requirement this section resolves is narrower: ensure the *new* identity fields (`LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/profile pair) are keyed to that same existing stable `Guid`-based row identity, never to `WeekNumber`/`AssignedDate` — which §13-§22 already guarantee by construction (the fields live on the same row as the existing stable `Id`).

---

## 48. Determinism

Same persisted rolling state + same adaptation inputs + same immutable bundle authority → same next generated window: guaranteed by (a) §19's frozen `(CandidateKey, CandidateVersion)` per plan (no "latest" dependency), (b) §28's `(StructuralRole, LaneOrdinal, SlotOrdinal)` composition key (no dictionary-iteration-order or date-ordering dependency — the exact two non-determinism sources §48 names explicitly), (c) §16's finding that `ProgressionStageKey` depends only on the plan's own already-recorded runtime-condition outcomes and phase allocation, not on wall-clock time or catalog state at JIT time, once persisted (§18 ensures it's captured once, not re-derived per JIT call).

---

## 49. 5D example — valid

**21-week Intermediate×5D LongHorizon plan, week 15 (a Runway week, since GE = 21-20 = 1 week, Runway = weeks 2-9, Core = weeks 10-21 — week 15 falls in Core, specifically real Core Week 6 given a 12-week Core block starting at week 10):**

- Real Core generation (via `TenKPreparationRunwayDarkOrchestrator`, reused unchanged) produces a `BoundCatalogSession` set for Core Week 6 against the real `TEN_K__5D__INTERMEDIATE` candidate: two `KEY_SESSION` sessions (`LaneOrdinal 0`/`SlotOrdinal` per `RUN_LAYOUT_5D`'s `sequenceOrder 1`; `LaneOrdinal 1`/`SlotOrdinal` per `sequenceOrder 3` — real layout from `plan-catalog/catalog/layouts/run-layout-5d.v1.json`, §9's research), two `EASY_SUPPORT` (`SlotOrdinal` per `sequenceOrder 2`/`4`, `LaneOrdinal null`), one `LONG_RUN` (`SlotOrdinal` per `sequenceOrder 5`, `LaneOrdinal null`).
- Each carries a real `ProgressionStageKey` (e.g. one of the real Build-phase stage keys the existing 5D Core progression catalog already authors — same mechanism proven public since `FREQ.6D.4D.5G`) and a real `PrescriptionProfileKey`/`Version` pair (one of the 8 real published `TEN_K__5D__INTERMEDIATE` v1.1.0 profiles, §9's research confirms these already exist and are published).
- JIT composition (§28) keys each session by `(StructuralRole, LaneOrdinal, SlotOrdinal)` — e.g. `("KEY_SESSION", 0, 1)` and `("KEY_SESSION", 1, 3)` — never colliding.
- Five `LongHorizonRollingSessionState` rows are written for the week, each carrying its own `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/`PrescriptionProfileKey`/`Version` (§27).
- Calendar materialization assigns real dates via the existing, unmodified `CatalogWeekSkeletonCalendarMaterializer` (already multi-KEY-generalized, §9's research).
- On confirmation, `TrainingDay` rows are written with `CatalogPrescriptionProfileKey`/`Version`/`CatalogProgressionStageKey` copied verbatim (§44) — `LaneOrdinal 0` and `LaneOrdinal 1` remain distinguishable throughout the entire chain, from generation through repair through confirmation.

## 50. 5D example — current failure

The identical week 15 scenario through **today's** unmodified architecture: `BuildBoundedCoreSelection` (§8) produces the same two `KEY_SESSION` `CatalogPrescribedSession` records with real `LaneOrdinal 0`/`1` — but `remainingByRole.GroupBy(s => s.StructuralRole, ...)` merges both into one `"KEY_SESSION"` bucket, and `LongHorizonSessionPrescriptionReference` construction (§8, item 4) never copies `LaneOrdinal` at all. The two lanes' `WorkoutKey`/`WorkoutVersion`/`DistanceKm` survive, dequeued in whatever order their `Date` values happen to sort — but nothing downstream (the persisted `LongHorizonRollingSessionState` row, any repair, any public read) can ever again determine *which* real lane produced *which* prescription. If the two lanes' distinct workouts (e.g. a threshold-tempo primary KEY vs. a controlled-progression secondary KEY, per Core's own real catalog) happen to require different treatment on repair or public rendering, the wrong one could be substituted for the wrong slot with no detectable error.

## 51. 4D legacy example

An existing, already-confirmed 4D LongHorizon plan (any currently-active row) after Split A's migration: its `LongHorizonRollingSessionState` rows gain five new columns, all populated `null` (§36 — no backfill). Every read path (rolling next-window generation, repair, adaptation, confirmation, home/calendar/detail) is unaffected — **zero semantic delta** — because `LaneOrdinal is null` for every 4D `KEY_SESSION` (4D's `RUN_LAYOUT_4D` has exactly one KEY per week, so lane disambiguation was never needed and the null value is simply never consumed by 4D's own unchanged single-KEY code paths), and `SlotOrdinal`/`ProgressionStageKey`/profile-pair are likewise present-but-unused unless/until a future phase explicitly makes 4D's own JIT path lane/stage-aware (out of scope — not proposed here). The plan does **not** need to "become ProfileBacked" (§51's own explicit non-requirement) — it remains exactly what it already is.

---

## 52. 6D/7D generalization check

The selected identity model (§13) supports both without modification:

- **2 KEY + 3 EASY + LONG (6D)**: `LaneOrdinal 0`/`1` for the two KEY lanes (unchanged from 5D — the model already handles N≥1 same-role `StageControlled` slots via zero-based rank, §9); `SlotOrdinal` disambiguates the three EASY occurrences the same way it disambiguates 5D's two (§15/§28 — the mechanism is not size-limited).
- **2 KEY + 4 EASY + LONG (7D)**: identical reasoning, four distinct `SlotOrdinal` values for the four EASY occurrences.

**No array/field is sized around 5** anywhere in this design (§52's explicit prohibition, honored) — `LaneOrdinal`/`SlotOrdinal` are unbounded `int?` columns, and the JIT composition key (§28) is a tuple, not a fixed-width structure. The one place a numeric literal `5` currently appears in the *existing* codebase relevant to this design is `NextWindowLoadDecisionPolicy`'s `ExpectedSessionCount == 5` dispatch condition (§23) — that condition is already generalized by construction to be *one* of potentially several session-count branches (a 6D/7D window would need its own `ExpectedSessionCount == 6/7` branch added to that same switch, an orthogonal, independently-scoped future change, not something this design's identity model blocks or needs to solve now).

---

## 53. Session-ordinal model for EASY (see §15)

`SlotOrdinal` — a distinct field, not an overload of `LaneOrdinal`. Resolved in §15.

## 54. Identity model options (see §10-§12)

## 55. Selected identity model (see §13)

---

## 56. Validation rules

- `(WeekStateId, StructuralRole, LaneOrdinal, SlotOrdinal)` must be unique per row — enforced at JIT-composition write time in application code (§37 — not a DB constraint).
- `PrescriptionProfileKey`/`Version` both-null-or-both-present (§38).
- A `ProfileBacked` progression-`KEY_SESSION` requires a successful `ExecutionPrescriptionIndex.ResolveExact` at JIT-composition time — a failure here is a distinct typed exception (§71), not a silent Legacy fallback (mirroring Core's own existing fail-closed `ResolveExact` contract, §9).
- `LaneOrdinal`/`SlotOrdinal` are immutable after assignment (§22) — enforced by §21's repair design rule (repair never writes these fields).
- Two `KEY_SESSION` rows in the same week can never share the same `LaneOrdinal` — guaranteed structurally by `CatalogWorkoutBinder`'s own zero-based-rank assignment (§9), which this design consumes verbatim rather than re-deriving, so no separate LongHorizon-side check can silently diverge from Core's own guarantee.

## 57. Database constraints (see §37)

---

## 58. LongHorizon execution-context contract (see §20)

## 59. Context propagation matrix — `LONGHORIZON_EXECUTION_CONTEXT_PROPAGATION_MATRIX`

| | Candidate/version identity | Bundle | ExecutionIndex | Profile ref | Stage | Lane/slot identity |
|---|---|---|---|---|---|---|
| Initial plan creation | AVAILABLE (persisted at confirmation, §19) | NOT_REQUIRED | NOT_REQUIRED | NOT_REQUIRED | NOT_REQUIRED | NOT_REQUIRED |
| Rolling window generation (JIT) | AVAILABLE (read back from plan, §19) | AVAILABLE (loaded via §20's wiring) | AVAILABLE (built once per window, §20) | DERIVED (via `ResolveExact` against bundle) | DERIVED (via `ProgressionStageAllocator`, unchanged) | DERIVED (via `CatalogWorkoutBinder`, unchanged) — then all four become AVAILABLE (persisted) on the resulting `LongHorizonRollingSessionState` row |
| Adaptation regeneration | AVAILABLE | NOT_REQUIRED (adaptation is role-aggregated only, §24) | NOT_REQUIRED | NOT_REQUIRED | NOT_REQUIRED | NOT_REQUIRED |
| Repair/regeneration | AVAILABLE | NOT_REQUIRED (repair copies existing values, §21 — never re-resolves) | NOT_REQUIRED | AVAILABLE (copied through, §21) | AVAILABLE (copied through) | AVAILABLE (copied through) |
| Core entry | AVAILABLE | AVAILABLE | AVAILABLE (same wiring as JIT generation, §29) | DERIVED then AVAILABLE | DERIVED then AVAILABLE | DERIVED then AVAILABLE |
| Confirmation/read | AVAILABLE | NOT_REQUIRED | NOT_REQUIRED | AVAILABLE (read back, written to `TrainingDay`, §44) | AVAILABLE | MISSING on `TrainingDay` itself by design (§44 — matches existing `TrainingDay.LaneOrdinal`-not-persisted precedent), AVAILABLE on the source `LongHorizonRollingSessionState` row |

---

## 60. Support-gate architecture

**Target: dispatch every remaining `SUPPORT_MATRIX_GATE` (§7) through `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`/`ResolveCandidate`** — the same generic identity policy Core and (as of `FREQ.6D.7`-`.10`) Runway already dispatch through, rather than `DaysPerWeek == 4 || DaysPerWeek == 5` literal widening (explicitly rejected by §60 of the phase prompt, and consistent with `FREQ.6D.5`'s own R1/R-D4 recommendation for Runway, generalized here to LongHorizon). `JIT_ALGORITHM_ASSUMPTION`/`PERSISTENCE_ASSUMPTION` literal-value sites (the 8 request/context/persisted-row builders in §7) are separately generalized to read `DaysPerWeek` from the resolved candidate's `RunLayout.StructuralRoles.Count` (§9's `CatalogStageToWeekContextFactory.Create` pattern — the exact abstraction already proven for Core) rather than a hand-set literal — this is the "resolved required session roles/slots" the phase prompt's §28 requires, not raw `DaysPerWeek` branching.

Support / eligibility / public-rollout are kept as three separate layers, matching the existing pattern this repository already uses for Core/Runway: *support* = `V1CatalogPilotIdentityPolicy` recognizes the combination (already true for `(Intermediate, 5)` today); *eligibility* = a specific request's readiness/horizon/candidate state is valid (existing `RaceHorizonPolicy`/`LongHorizonCompositionResolver` machinery, unchanged); *public rollout* = the routing gate itself (§62 — explicitly not touched by this phase).

## 61. LongHorizon capability flag

**Use the existing `V1CatalogPilotIdentityPolicy`/identity-policy mechanism directly — no new, independent capability-flag or feature-registry source is introduced.** This mirrors §61's own instruction ("use existing architecture... do not create another independent rollout source") and is consistent with how Core/Runway activation was gated in `FREQ.6D.7`/`.8` (an identity allow-list check, not a separate feature-flag system).

---

## 62. Public activation is later

**Explicitly not widened by this phase.** No routing, DI registration, or public-preview dispatch change is made or proposed as part of this design closing. The implementation splits (§33) explicitly sequence Split F (public activation + real HTTP/DB E2E) last, gated on Split E's dark verification succeeding **and** §25's GE product decision being fully resolved — this design does not collapse that sequence.

---

## 63-65. Implementation decomposition / dependency order / migration ordering (see §33-§34)

---

## 66. Backward-compatibility test plan (see §35)

## 67. 5D dark test plan

Representative 21/24/32/52-week Intermediate×5D plans, covering: initial rolling state (all fields populated correctly at JIT-composition time, §49's example generalized to each horizon); first rolling window (correct `(LaneOrdinal, SlotOrdinal)` assignment, no collision per §28); adaptation (5-session table exercised via §23's existing policy, now genuinely reachable); subsequent window (repair-copy-through invariant, §21, exercised across a real window boundary); Core entry (real dual-KEY Core Week 1 reached via the shared orchestrator, §29); dual KEY (both lanes independently traceable end-to-end, §49 vs. §50's contrast made concrete per horizon); Taper (real 5D Taper lane, already proven at the Core level since `FREQ.6D.4D.5D`, now reachable via LongHorizon's own Core-entry); persistence/reload (a fresh DB reload re-reads all five new columns correctly, mirroring `FREQ.6D.8`'s own "permanent regression: re-assert after a fresh DB reload" pattern).

## 68. PostgreSQL test plan

Real Postgres confirmation for at least one full 21-week LongHorizon plan reaching real Core Week 1: verify persisted `LongHorizonRollingSessionState` rows for the dual-KEY week carry distinct, correct `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/profile-pair values; verify the eventual `TrainingDay` rows (once confirmed) carry the correctly-copied-through profile/stage lineage per §44; verify a repair operation against a rolling (not-yet-materialized) session preserves identity per §21, re-confirmed after a fresh reload.

## 69. Adaptation test plan (see §68 of the phase prompt, mapped to §23's table)

5/5→Progress; 4/5-easy-missed→Progress; 4/5-KEY1-missed→Maintain; 4/5-KEY2-missed→Maintain (both KEY-missed cases must produce the *same* outcome, directly proving §24's role-level-equivalence invariant); 4/5-LONG-missed→Maintain; 2/5→Maintain; 1/5→Reduce. No new severity policy required — this test plan exercises `NextWindowLoadDecisionPolicy` exactly as it exists today (§23), the only new work is ensuring a real 5-session `WindowExecutionSummary` reaches it.

## 70. Repair test plan

Move primary KEY (lane 0); move secondary KEY (lane 1); substitute future EASY (any of the 2-4 repeated slots depending on frequency); calendar reorder (assert lane/slot identity survives a date-only change, §22); reload after repair (assert persisted lineage matches pre-repair values except the fields the specific repair type legitimately changes, per §21's copy-through rule).

## 71. Version-drift test plan

Simulate: plan created against bundle/candidate version A; catalog later publishes version B (a new `TEN_K__5D__INTERMEDIATE` release); next JIT window for the *existing* plan must still resolve via the plan's own frozen `(CandidateKey, CandidateVersion)=A` (§19) — never drift to B. A *new* plan created after B's publication resolves against B. No latest-version dependency exists in the JIT path itself (§48).

---

## 72. Failure taxonomy

Distinct typed exceptions required (new, LongHorizon-scoped, mirroring existing Core-side exception shapes rather than inventing an unrelated pattern):

- Invalid partial profile lineage on a `LongHorizonRollingSessionState` write — mirrors `CatalogSessionPrescriptionInvalidProfileLineageException` (§38).
- Missing exact execution prescription at JIT-composition time — mirrors `ExecutionPrescriptionNotFoundException` (§9/§56).
- Duplicate session-slot identity within a week — a new `LongHorizonSessionSlotIdentityConflictException`-shaped type (§56's uniqueness rule).
- Missing stage where the role requires one (a `StageControlled` role resolving with no `ProgressionStageKey`) — distinct from the legitimate-null case (§39).
- Unsupported LongHorizon combination — mirrors the existing `LongHorizonPilotUnsupportedException` shape, generalized per §60/§61 rather than replaced.
- Historical Legacy row encountered where new-lineage logic assumes ProfileBacked — must degrade to Legacy semantics, not throw (§36's explicit non-backfill decision requires this).

No collapse into a generic 500 anywhere in this list — each maps to an existing `GlobalExceptionHandler`-style typed-exception-to-status-code pattern this repository already uses pervasively (confirmed extensively during `FREQ.6D.10`'s own verification work).

---

## 73. No numeric invention

Confirmed: this design introduces zero new numeric values. Every number referenced (26.0/19.5/44.5/28%/36% for Runway/Core segments; the existing 0.85/0.5 GE-recovery values, explicitly flagged `DECISION_REQUIRED` rather than reused for a 5D GE segment) is either already-approved existing authority or explicitly deferred as `NUMERIC_DECISION_REQUIRED` pending §25's GE product decision.

---

## 74. Technical-debt disposition

- **`TD-RUNWAY-ARCHITECTURE-HARDCODED-SINGLE-CELL-001`** — unaffected by this phase (Runway itself was already closed by `FREQ.6D.7`-`.10`); no update needed here.
- **New debt item recommended (not created in this evidence/design phase's own governance metadata, per its no-code-changes constraint, but flagged for the implementation phase to formalize)**: `TD-LONGHORIZON-ARCHITECTURE-HARDCODED-SINGLE-KEY-001`, covering §7's 21-line gate inventory and §8's JIT collision — matches `FREQ.6D.5`'s own §22 suggestion, now with the exact decomposition (Splits A-D, §33) a future implementation phase needs.
- **New debt item recommended**: `TD-LONGHORIZON-GE-SEGMENT-4D-ONLY-001`, covering the GE segment's own 4-role (`KeySession`/`EasySupportA`/`EasySupportB`/`LongRun`) hardcoding and its `DRAFT`, never-loaded catalog artifact (§25) — distinct from the dual-KEY lineage debt above, since it is blocked on a product decision, not merely engineering.
- `FREQ.6D.5`'s L-D5 (Core-entry duplication) — recorded as open architecture debt, explicitly deferred (§29), not part of this phase's closure boundary.

---

## 75-77. Decision matrices (see §13 for the selected-model comparison table, §27 for the schema-decision table, §28 for the JIT-composition-decision table)

---

## 78. Implementation-readiness matrix

| Axis | Status |
|---|---|
| SESSION_IDENTITY | `APPROVED_DESIGN` (§13) |
| PERSISTENCE_SCHEMA | `APPROVED_DESIGN` (§27) |
| JIT_COMPOSITION | `APPROVED_DESIGN` (§28) |
| PROFILE_LINEAGE | `APPROVED_DESIGN` (§17) |
| EXECUTION_CONTEXT | `APPROVED_DESIGN` (§20) |
| ADAPTATION | `ALREADY_GENERIC` (§23) |
| REPAIR | `ENGINEERING_READY` (§21 — no existing precedent, but a fully specified, narrow, additive design rule) |
| CARDINALITY | `ENGINEERING_READY` (§7/§60 — every gate classified, target architecture specified) |
| SUPPORT_GATING | `ENGINEERING_READY` (§60/§61) |
| PRODUCT_WEEKLY_STRUCTURE | `PRODUCT_DECISION_REQUIRED` (§25 — GE segment only; Runway/Core segments are `ALREADY_GENERIC`) |
| NUMERIC_AUTHORITY | `NUMERIC_DECISION_REQUIRED` (§26 — GE segment only; Runway/Core segments are `5D_EXISTING_AUTHORITY`) |

---

## 79. Success boundary

A. ✅ Canonical session identity model selected (§13). B. ✅ Persistence schema decided (§27). C. ✅ JIT duplicate-role handling decided (§28). D. ✅ Profile binding lifecycle decided (§18). E. ✅ Exact ExecutionIndex path decided (§20). F. ✅ Historical 4D migration behavior decided (§35/§36). G. ✅ Adaptation reuse confirmed (§23). H. ✅ Repair lineage behavior decided (§21). I. ✅ 4D-only gate replacement strategy decided (§60). J. ✅ Implementation split order defined (§33/§34). **K. Hidden product/numeric authority remains for the GE segment specifically (§25/§26/§32) — not fully resolved.**

Per the phase's own rule ("if K fails: do not declare implementation-ready"), this phase does **not** declare full implementation-readiness. It declares the Runway/Core-segment portion of LongHorizon's dual-KEY architecture fully designed and implementation-ready, with the GE segment's weekly structure and numeric coefficients explicitly carved out as a distinct, narrow, not-yet-resolved product/numeric authority gap.

---

## 80. Final classification

**`INTERMEDIATE_5D_LONGHORIZON_ARCHITECTURE_APPROVED_PRODUCT_POLICY_REQUIRED`**

The complete dual-KEY lineage / JIT composition / persistence / execution-context architecture is selected and fully specified (§13, §20, §27, §28 — all of A-J in §79 are closed). The one remaining gap is narrow and precisely scoped: the LongHorizon-specific GE (General Endurance) pre-Runway segment has no approved 5D weekly session-role structure or numeric coefficients (§25/§26/§32) — its existing catalog artifact and role model are explicitly 4D-shaped and were never designed with a 5D question in mind. This is **not** a broader LongHorizon architecture gap (the Runway and Core segments — which together make up 20 of every 21-52 total weeks — are fully `EXISTING_AUTHORITY_APPLIES`/`ALREADY_GENERIC`, reachable via the engineering-only Splits A-D once implemented) and does not block the schema/JIT/persistence/execution-context implementation work from proceeding; it blocks only the eventual GE-segment implementation and, transitively, full public activation (Split F).

---

## 81. No code

Confirmed: no production code, EF migration, or routing change was made this phase. This document, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md` are the only files touched.

---

## 82-84. Phase report / ledger / roadmap / push gate

See the ledger row and roadmap update accompanying this report. Push-gate recalculated at phase end (§ below in the final report answers).
