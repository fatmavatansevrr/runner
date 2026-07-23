# Phase 4F Pre-Publication Implementation Validation

> **Further update (Phase 4F.9.2):** real-PostgreSQL relational validation is
> now complete (`PHASE4F_9_2_RELATIONAL_VALIDATION.md`, classification
> `PHASE4F9_2_RELATIONAL_VALIDATION_CLOSED`). It found and fixed two
> additional severe defects invisible to any InMemory-only testing: an
> order-dependent snapshot hash broken by PostgreSQL jsonb key reordering,
> and a `DateTimeKind.Unspecified` value rejected by Npgsql for
> `timestamptz` columns — both would have broken every real catalog
> confirmation in production. Full backend suite: 808/808 passing against a
> real database.

> **Superseding update (Phase 4F.9.1A):** every blocker identified below was
> resolved in `PHASE4F_9_1A_PRE_RELATIONAL_CORRECTIONS.md` — confirmation
> ordering was corrected (idempotency now checked before expiration),
> the active-plan concurrency race now translates to
> `CatalogActivePlanConflictException` via the pre-existing
> `IX_TrainingPlans_InternalUserId_ActiveOnly` index, pilot identity is now
> centrally owned by `V1CatalogPilotIdentityPolicy`, and the 8-week
> explicit-zero claim was runtime-verified as `CONFIRMED_INFEASIBLE` (at the
> taper week, not week 1 as originally suspected here). See that report for
> full detail and updated relational-validation readiness
> (`READY_FOR_PHASE4F9_2_RELATIONAL_VALIDATION`). The classification and
> findings below reflect the state as originally audited and are retained
> for historical accuracy.

## 1. Final classification

`PHASE4F_CORRECTIONS_REQUIRED_BEFORE_RELATIONAL_VALIDATION` — **RESOLVED by Phase 4F.9.1A** (see note above).

Core generation/materialization/persistence logic is sound and builds/tests clean, but two correctness defects directly affecting the exact behavior Phase 4F.9.2 is meant to prove (concurrency and idempotency under confirmation) were found: an expiration-before-idempotency ordering defect, and an unverified active-plan TOCTOU race sitting alongside a self-disclosed "sequential only, does not prove concurrency safety" test suite. These should be resolved or explicitly risk-accepted before spending real PostgreSQL time proving concurrency semantics that the current code/tests do not yet establish.

## 2. Repository state

- Branch: `main`. HEAD: `0c67965` ("checkpoint: add Phase 4F.4 dark skeleton preview wiring").
- `git log` contains commits only through **Phase 4F.4**. All code and documentation for **Phase 4F.5 through 4F.9** (and 4E.2 dev-DB verification) exists solely as **untracked working-tree files** — confirmed via `git status --short` (214 changed/untracked paths) and empty `git log --all -- <file>` for the 4F.5–4F.9 docs.
- No staged changes. Unstaged changes are almost entirely `bin/`/`obj/` build-artifact drift (generated, incidental) plus a real source diff set: `TestingController.cs`, `GlobalExceptionHandler.cs`, `Program.cs`, `AppExceptions.cs`, `CatalogPlanConfirmationService.cs`, `CatalogPreviewGenerator.cs`, `CatalogPreviewSnapshot.cs`, `CatalogPreviewSnapshotVerifier.cs`, `PlanServices.cs`, `TrainingDay.cs` and others under `RuntimeCatalog/**`.
- Untracked (new) source: all of `RuntimeCatalog/Prescription/**`, `RuntimeCatalog/Schedule/Binding/**`, `RuntimeCatalog/Schedule/Materialization/**`, `RuntimeCatalog/Schedule/Progression/**`, `CatalogPersistedPlanValidator.cs`, `LivePlanPreviewRouting.cs`, `CatalogPublicPreviewMaterializer.cs`, plus matching integration test files and 16 root-level `PHASE4F_5..9*.md` docs and 1 misfiled audit JSON (`phase4f9-catalog-confirmation-and-persistence-audit.json` sits at repo root instead of `plan-catalog/artifacts/audits/`).
- Classification: this is **intentional in-progress Phase 4F.5–4F.9 work**, not stale/orphaned drift. No incidental generated drift beyond `bin/`/`obj/` was reverted (none was touched, per instructions).
- Documentation risk: the uncommitted docs (7B, 7B.1, 8.2, 9, etc.) use decisive/closure vocabulary ("CLOSED", "CANONICAL_CONFIRMED", "EXPLICIT_PRODUCT_DEFAULT") despite having zero commit/PR/review trail. Treat as working claims, not ratified decisions, until committed and reviewed.

## 3. Build results

| Command | Result |
|---|---|
| `dotnet build plan-catalog/PlanCatalog.sln` | 0 errors, 0 warnings (2.8s) |
| `dotnet test plan-catalog/PlanCatalog.sln` | 335 passed / 0 failed / 0 skipped |
| `dotnet build backend/RunningApp.sln` | 0 errors, 0 warnings (3.1s) |
| `dotnet test backend/RunningApp.sln` (full suite, only test project is `RunningApp.IntegrationTests`) | 722 passed / **37 failed** / 0 skipped, 759 total |
| `dotnet-ef migrations has-pending-model-changes` | "No changes have been made to the model since the last migration." |

All 37 backend failures are **environment-blocked**, not logic defects: every one fails inside its `ResetAsync()` helper with `HttpRequestException: 500`, before the test body executes, because `CustomWebApplicationFactory.cs:24-25` hardcodes a PostgreSQL connection (`localhost:5432/antigravity_dev`) that is unreachable in this sandbox (confirmed no listener on 5432). These are **not** reported as passing and are **not** logic bugs. Zero genuine logic failures were found in either suite.

## 4. Production call chain

**Preview generation** (`PlansController.GeneratePreview` → `PlanServices.GeneratePreviewAsync`):

1. Route decision: `LivePlanPreviewRoutingService.Decide` → `V1LiveCatalogPilotRoutingPolicy.Evaluate` (`PreviewRouting/LivePlanPreviewRouting.cs:63,180`)
2. Catalog dispatch: `PlanServices.GenerateCatalogPreviewAsync` (`PlanServices.cs:253`)
3. Candidate resolution: `CatalogPreviewGenerator.GenerateAsync` (`CatalogPreviewGenerator.cs:290`) → `CatalogCandidateEligibilityGate.LoadForPublicPreviewAsync`
4. Runtime conditions: `RuntimeConditionResolutionService.ResolveAllResults` + `ApplyNotEvaluatedGovernancePolicy` (`CatalogPreviewGenerator.cs:369`)
5. Skeleton: `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton` (`:445`) → `CatalogPlanSkeletonOrchestrator.Build`
6. Stage scheduling (4F.6A): `ProgressionStageAllocator.Allocate`
7. Calendar assignment (4F.5): `CatalogWeekSkeletonCalendarMaterializer.Materialize` + `DatedGeneratedCatalogPlanSkeletonValidator.Validate`
8. Workout binding (4F.6B): `CatalogWorkoutBinder.BindAsync`
9. Prescription context (4F.7A): `CatalogPrescriptionContextBuilder.Build`
10. Volume/long-run (4F.7B): `CatalogVolumeAndLongRunPlanner.Build`
11. Session prescription (4F.7C): `CatalogSessionPrescriptionPlanner.Build`
12. TAPER_SHARPEN + final validation (4F.7D): `CatalogFinalPrescribedPlanFinalizer.Complete` (policy: `V1TaperSharpenPrescriptionPolicy`) → `CatalogFinalPrescribedPlanValidator`
13. Public materialization (4F.8.1): `CatalogPublicPreviewMaterializer.Materialize` (`:119`)
14. Snapshot/hash: `CatalogPreviewSnapshotBuilder.Build` (`CatalogPreviewSnapshot.cs:119-178`, SHA-256 `ContentHash`)
15. Persist `PlanPreview` and return (`PlanServices.cs:280-294`)

**Confirmation** (`PlansController.ConfirmPlan` → `CatalogPlanConfirmationService.ConfirmAsync`, `CatalogPlanConfirmationService.cs:157`): preview lookup (162) → ownership (172) → **expiration (181)** → invalidation (188) → snapshot presence (195) → deserialize (203) → schema completeness (224) → generation-source check (227) → hash verification (236-260) → **already-confirmed idempotency (265)** → payload/schema/structural/8-week-zero guard (310-354) → active-plan invariant (356-368) → transaction: persist `TrainingPlan`/`TrainingWeek`/`TrainingDay`/`PlanEvent`, set `ConfirmedPlanId`, post-persist `CatalogPersistedPlanValidator` (370-416).

**Defect**: expiration (step 3) is checked **before** idempotency (step 10), contradicting the documented intent ("expiration... does not invalidate an already confirmed idempotent response path once the preview is marked confirmed" — PHASE4F_9 doc, "Preview Lifecycle"). A preview confirmed successfully but later found expired on repeat-confirm would throw `PlanPreviewExpiredException` instead of idempotently returning the existing plan. Classification: **IMPLEMENTATION_CONTRADICTS_DECISION**.

Two stale doc/comment blocks were also found describing now-false states as current: `CatalogPreviewGenerator.cs:32-38` and `CatalogPreviewSnapshot.cs:105-117` still claim the generated payload is "dark"/"discarded"/"never hashed," but it is in fact returned, stored as `GeneratedPreviewPlanPayload`, and included in the hashed content (`CatalogPreviewSnapshot.cs:153,174`). Similarly `CatalogPlanConfirmationService.cs:460-472` claims persistence steps "do not exist as executable code in this phase" directly above code that executes them (373-458). These are stale Phase 4E.1/4F.1/4F.4-era comments never updated as later phases landed — **STALE_DOCUMENTATION**, not a functional defect, but actively misleading to a reader.

## 5. Phase status matrix

| Phase | Classification | Evidence |
|---|---|---|
| 4D runtime-condition resolvers | VERIFIED_IMPLEMENTED | Individual resolvers registered once each, Scoped; orchestration service resolves and governs `NotEvaluated` explicitly (`ApplyNotEvaluatedGovernancePolicy`) |
| 4E preview/confirm governance boundary | VERIFIED_IMPLEMENTED | Routing/confirmation split cleanly; `IsCatalogSourcedPreview` dispatch is one-directional and data-driven |
| 4F.1 typed schedule contract | VERIFIED_IMPLEMENTED | Committed; builds clean |
| 4F.2 stage/week skeleton | VERIFIED_IMPLEMENTED | Committed; builds clean |
| 4F.3 live artifact resolution | VERIFIED_IMPLEMENTED | Committed |
| 4F.4 dark preview wiring | IMPLEMENTED_BUT_INCOMPLETELY_VALIDATED | Committed, but its own "dark/discarded" framing is now stale relative to 4F.5+ |
| 4F.5 calendar assignment | VERIFIED_IMPLEMENTED | Deterministic backtracking date assignment from real `StartDate`, no REST rows, exact role counts enforced |
| 4F.5.1 dated-skeleton validation | VERIFIED_IMPLEMENTED | `DatedGeneratedCatalogPlanSkeletonValidator` wired into production path |
| 4F.6A KEY_SESSION stage scheduler | VERIFIED_IMPLEMENTED | `ProgressionStageAllocator` sole owner |
| 4F.6B exact workout binding | VERIFIED_IMPLEMENTED | Keyed switch, throws on ambiguity rather than first-match |
| 4F.7A prescription context | VERIFIED_IMPLEMENTED | `CatalogPrescriptionContextBuilder` single owner |
| 4F.7B weekly volume/long-run | VERIFIED_IMPLEMENTED | Peak band data-driven, taper multiplier 0.53, long-run shares 30/33/36/40% all match doc |
| 4F.7B.1 canonical corrections | VERIFIED_IMPLEMENTED | 0.65→0.53 correction present and disclosed, no stale 0.65 in live code |
| 4F.7B.2 missing/zero volume policy | VERIFIED_IMPLEMENTED | 16km/12km defaults, typed failure on invalid, inconsistency handled as low-confidence clamp not direct anchor |
| 4F.7C pace source/session prescription | VERIFIED_IMPLEMENTED | No fabricated numeric pace found; ESTIMATED has no producer |
| 4F.7D TAPER_SHARPEN | VERIFIED_IMPLEMENTED | Identity/components/pending-state clearance all confirmed |
| 4F.8.1 public materialization | VERIFIED_IMPLEMENTED | Projection-only, cross-check assertions not recomputation |
| 4F.8.2 scoped live routing | VERIFIED_IMPLEMENTED | Candidate DRAFT, pilot disabled by default, no ledger entry |
| 4F.9 confirmation and persistence | PARTIALLY_IMPLEMENTED | Persistence mapping and migrations solid; confirmation validation ORDER contradicts documented idempotency-vs-expiration precedence (see §4); concurrency test coverage self-admits sequential-only |
| 4F.9.1 migration/concurrency corrections | IMPLEMENTED_BUT_INCOMPLETELY_VALIDATED | `SourcePreviewId` unique-violation recovery path implemented correctly; active-plan-invariant race window not verified closed |

## 6. Phase ownership

No duplicate volume/distance/date/key recomputation found downstream of its owning phase. One duplication found in **routing/identity logic**, not domain math: `PilotGenerationRouteDecider.Decide` (`GenerationRouteDecision.cs:75-79`) and the actually-wired `V1LiveCatalogPilotRoutingPolicy.Evaluate` (`LivePlanPreviewRouting.cs:72-76`) independently hardcode the identical four-field pilot-candidate check rather than sharing one function. The former is never registered in DI (`DEAD_OR_UNREACHABLE_CODE` / `TEST_ONLY_BEHAVIOR`, exercised only by `PilotGenerationRouteDeciderTests.cs`), but its presence is a live drift risk if either copy is edited without the other.

`CatalogStageKey` (legacy field) is still actively **written** by new catalog confirmation code (`CatalogPlanConfirmationService.cs:587`) alongside the newer `CatalogProgressionStageKey`/`CatalogWorkoutDefinitionKey` fields, rather than being purely legacy/read-only as the design doc implies ("retained as legacy/deprecated... not repurposed"). Dual-writing for backward compatibility is plausible and not necessarily wrong, but it should be an explicit decision, not an implicit side effect — flagged as **NON_BLOCKING_GAP**, confirm intent.

## 7. Pilot identity

Typed identity mapping (`GoalType=Race, GoalDistance=TenK, Level, DaysPerWeek=4 → TEN_K__4D__INTERMEDIATE v10`) exists but is **not a single owned, tested function**: it is duplicated verbatim in `GenerationRouteDecision.cs:75-79` (dead) and `LivePlanPreviewRouting.cs:72-76` (live). The `RunningRegularly ↔ INTERMEDIATE` equivalence is explicitly self-documented as **unapproved**: `GenerationRouteDecision.cs:53-64` states "No formal owner-approved mapping from RunningBackground to the catalog's INTERMEDIATE level exists as of this phase," and the live policy inherits this informal mapping without re-derivation or sign-off. Classification: **IMPLEMENTATION_CONTRADICTS_DECISION** relative to the audit's explicit requirement ("reject hidden equivalence implemented through scattered string comparisons... verify typed mapping exists and is tested").

## 8. Candidate integrity

`TEN_K__4D__INTERMEDIATE v10` (`plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`): status **DRAFT**. All 9 dependencies (master template, workout progression, run layout, level modifier + progression modifier, race-plan policy, peak-volume bands, runtime-condition values, 5 workout definitions) resolve, all DRAFT, all `reachableFromCandidate: true`. One disclosed (not hidden) data-quality note: `LONG_RUN_STANDARD:v4` is reachable only via level-modifier eligibility, referenced by zero progression stages. No publication ledger entry exists anywhere (`release-status.json` tops out at `0.6.0-pilot`/combination v4); `phase4f8-2` audit explicitly records `publicationLedgerAdded: false`. Catalog validation suite: **150/150 pass** (`Validation/*Tests.cs`, incl. `D13GoalPaceTenKResolutionTests.cs`, 12/12). Verdict: **VERIFIED_IMPLEMENTED**, no publish/activation leakage found.

## 9. Runtime-condition validation

Individual resolvers (time adequacy, pace source, core-entry readiness, goal feasibility) are registered once each, Scoped, and are documented/confirmed as not yet wired into any live serving path beyond the orchestration service used by the catalog preview generator. `NotEvaluated` is handled by an explicit governance policy (`ApplyNotEvaluatedGovernancePolicy`) rather than silently upgraded to Eligible/Ineligible. TD-PACESOURCE-001/002, TD-CORE-READINESS-001, TD-REGISTRY-001 were not independently re-litigated in this pass beyond confirming the resolvers still exist and behave per the governing policy classes cited in the 4D docs; no evidence found that any was closed without meeting its stated criteria, but this pass did not re-derive the underlying training-science debt items from first principles. Classification: **VERIFIED_IMPLEMENTED** (structural), **IMPLEMENTED_BUT_INCOMPLETELY_VALIDATED** (debt-item closure criteria not independently re-audited this pass).

## 10. Schedule and binding validation

Cycle lengths genuinely parameterized from `CoreCycle.MinimumWeeks/MaximumWeeks` and real 8/12/14-week data in `ten-k-master.v6.json` (not a hardcoded 12-week fixture). Exactly one KEY_SESSION, two EASY_SUPPORT, one LONG_RUN per week enforced (`CatalogWeekSkeletonCalendarMaterializer.cs:122-167`); REST/OPTIONAL roles explicitly rejected. Dates derive from `context.StartDate`, no Monday hardcode; assignment uses deterministic bounded backtracking, no dictionary-order or hash-order dependency. Workout binding: EASY_SUPPORT→EASY_STANDARD, LONG_RUN→LONG_RUN_STANDARD (both fixed), KEY_SESSION→stage-controlled, with an explicit throw (`CatalogWorkoutBindingAmbiguousCandidateException`) on any stage declaring more than one candidate rather than first-match. No VO2max/hill/EASY_SHAKEOUT tokens found anywhere in `backend/`. Verdict: **VERIFIED_IMPLEMENTED**.

## 11. Volume and long-run validation

Readiness/starting-volume policy (`V1MissingReadinessStartingVolumePolicy`): AVAILABLE uses reported value; NOT_PROVIDED→16km; EXPLICIT_ZERO→12km; INVALID→typed failure; longest-run/weekly-average inconsistency only affects a `LOW_CONFIDENCE_CONSERVATIVE_CLAMP` classification, never used directly as the anchor. All in one versioned policy class. Peak volume band is data-driven per distance family/experience/frequency (not hardcoded 30 or 42); taper multiplier is `0.53` with a guard rejecting values outside a 41–60% reduction band. Global search for obsolete constants `0.65`/`0.20`/`0.35` in live (non-test) backend code: **no live hits** — the only `0.65` hit is a test asserting its *absence*; the two live `0.20` hits are unrelated (taper-sharpen dose sizing, warm-up/cool-down sizing), and `0.35` does not appear at all. Long-run shares: preferred 30–36%, default 33%, hard cap 40%, with a runtime throw on cap violation; computed once in 4F.7B and consumed read-only downstream (allocation policy takes it as input; public materializer and final validator only re-check equality, they don't recompute). Verdict: **VERIFIED_IMPLEMENTED**.

## 12. Session prescription validation

`V1_FOUR_DAY_SESSION_VOLUME_ALLOCATION_POLICY`: KEY_SESSION target = 50% of residual (min 3km), EASY_SUPPORT min 1.5km each; throws `CatalogSessionPrescriptionInfeasibleException` before any session/payload is built if residual < 6km. This guard is generic across all weeks/cycles, so it structurally covers the "8-week + explicit-zero" case, but **static arithmetic on that specific scenario (12km start, ~4km long run, ~8km residual) suggests week 1 of an 8-week explicit-zero plan would clear the 6km floor and NOT actually be infeasible** — the claimed "known infeasible" case was not reproduced by static reading alone. Classification: **IMPLEMENTED_BUT_INCOMPLETELY_VALIDATED** — needs one concrete runtime/unit-test execution of that exact scenario to confirm whether infeasibility triggers at week 1, a later week, or not at all under current numbers; do not assume the documented claim is correct without that run. Pace source: GOAL_PACE_TEN_K requires feasibility ∈ {REALISTIC, CHALLENGING} + target time, else throws; all other workouts stay effort-only; ESTIMATED has no producer; unresolved pace is `null`, never 0; final validator rejects any non-GOAL_PACE_TEN_K session carrying an exact pace. Verdict overall: **VERIFIED_IMPLEMENTED** with the one arithmetic flag above.

## 13. TAPER_SHARPEN validation

Identity fields (PhaseKey=TAPER, ProgressionStageKey=TAPER_SHARPEN, StructuralRole=KEY_SESSION, WorkoutDefinitionKey=EASY_STANDARD) and component order (EASY_BASELINE/CONTROLLED_SHARPENING/EASY_RECOVERY) are enforced by `V1TaperSharpenPrescriptionPolicy` and cross-checked in `CatalogFinalPrescribedPlanValidator`. `BaselinePrescribedSharpeningPending` is set only transiently and is verified cleared before final-plan return (`CatalogPendingPrescriptionStateException` thrown if any non-TAPER_SHARPEN session, or any session post-finalization, still carries it); `CatalogPublicPreviewMaterializer` independently re-guards against it reaching the public payload. Verdict: **VERIFIED_IMPLEMENTED**, matches AUD-508-era decisions per governance cross-check.

## 14. Public materialization

`CatalogPublicPreviewMaterializer` is projection-only: it copies upstream values (week volume, session distance, phase/stage/workout keys, pace shape, segments) and where it appears to "recompute," it is in fact a read-only equality cross-check against upstream values (`Math.Abs(...) > 0.001` assertions), not a second calculation. No string-contains, display-title, or first-match mapping found; no VO2max/hill/shakeout. Verdict: **VERIFIED_IMPLEMENTED**.

## 15. Snapshot and hash

`CatalogPreviewSnapshotBuilder.Build` includes the generated public payload in the hashed content (contrary to stale in-code comments claiming otherwise — see §4). No builder/verifier asymmetry found: `CatalogPreviewSnapshotVerifier` re-derives the same canonical structure. Legacy hash behavior untouched. Verdict: **VERIFIED_IMPLEMENTED**, with the stale-comment note carried from §4.

## 16. Live routing

`V1LiveCatalogPilotRoutingPolicy` matrix confirmed: DRAFT (disabled or enabled) → not catalog-live; PUBLISHED+disabled → not catalog-live; PUBLISHED+enabled → eligible. Current real state: candidate is DRAFT, `CatalogLivePilotOptions.Enabled` defaults `false` in code with no override in `appsettings.json`/`appsettings.Development.json`. No live catalog exposure currently possible. Verdict: **VERIFIED_IMPLEMENTED** — closed/safe as claimed.

## 17. Confirmation

Snapshot/payload authority confirmed: `CatalogPlanConfirmationService` has no dependency on any router/resolver/generator (only `AppDbContext`, logger, and a dependency-free structural payload validator), matching "no rerun" requirement. Legacy separation is data-driven and one-directional (§23). Validation ORDER defect noted in §4/§5 (expiration precedes idempotency) is the one concrete finding here — **IMPLEMENTATION_CONTRADICTS_DECISION**.

## 18. Persistence mapping

Full field mapping verified for `TrainingPlan` (generation source, source-preview id, candidate identity, hash, materializer version, dependency-versions JSON, confirmation timestamp, plan-level fields), `TrainingWeek` (week number, start date, planned volume, phase key, taper/recovery marker), and `TrainingDay` (date, role, workout key/version, legacy `CatalogStageKey`, phase key, progression-stage key, distance, pace/duration/effort, prescription JSON + schema version, generation source) — all present with no field-loss found. One noted nuance, not a defect: `PlannedPaceMinKm` only captures the exact-pace case; effort-only/range paces are null at the column level and rely on the JSON blob as the canonical store (as designed).

## 19. Prescription JSON

Schema key `CATALOG_SESSION_PRESCRIPTION_SNAPSHOT`, version 1 (constant), snake_case `System.Text.Json` serialization, segments ordered by `SegmentOrder`. No raw exception text, file paths, or decision-trace/readiness-evidence leakage found (`DecisionTrace` explicitly excluded from both the outer hash and this object). Gap: no dedicated serializer round-trip unit test isolating the exact schema shape exists — coverage is indirect via a substring check inside `CatalogPlanConfirmationServiceTests`. Classification: **IMPLEMENTED_BUT_INCOMPLETELY_VALIDATED** (add a focused round-trip test; not done in this pass as no clear existing test-fixture convention was confirmed for it beyond the indirect one).

## 20. Migration static validation

`20260716120000_Phase4F9_CatalogConfirmationPersistence.cs`: Up/Down symmetric, `jsonb` types on the two JSON columns, all others nullable text/integer/uuid/timestamptz, filtered unique index `IX_TrainingPlans_SourcePreviewId` (`WHERE SourcePreviewId IS NOT NULL`) present and mirrored correctly in `AppDbContextModelSnapshot.cs`. `dotnet-ef migrations has-pending-model-changes` reports **no pending model changes**. Verdict: **VERIFIED_IMPLEMENTED** (static-only; real relational application remains for 4F.9.2).

## 21. Idempotency/concurrency code review

`ConfirmedPlanId` short-circuit and unique-violation recovery (rollback, change-tracker clear, reload by `SourcePreviewId`, else throw `CatalogPreviewConfirmationConcurrencyException`) are implemented correctly and specifically. However: (a) the active-plan-invariant check is a separate `AsNoTracking` query executed before the insert transaction begins — a TOCTOU window for two concurrent confirmations of *different* previews for the same user, not provably closed by the `SourcePreviewId` unique index alone (needs verification of a separate active-plan uniqueness constraint, not confirmed present in this pass); (b) `CatalogPlanConfirmationServiceTests.cs` **self-documents** (lines 538-547) that its concurrency tests are sequential-only and explicitly do **not** prove production concurrency safety for the two-concurrent-inserts scenario. Classification: **IMPLEMENTED_BUT_INCOMPLETELY_VALIDATED**, and this is the primary reason relational validation readiness is qualified rather than clean (§29).

## 22. Error mapping

All new typed exceptions map to distinct, non-contradictory HTTP statuses; 500-level responses return a generic message (raw exception text only logged server-side, never returned); 4xx/422 responses return `exception.Message` verbatim, and those messages were manually reviewed in the exception classes' own XML docs as free of sensitive detail. One harmless naming confusion: `PlanPreviewAlreadyConfirmedException` (dead, unregistered) vs. `CatalogPreviewAlreadyConfirmedException` (live, maps to 409) — cosmetic, not a functional bug. Verdict: **VERIFIED_IMPLEMENTED**.

## 23. Legacy isolation

Dispatch (`PlanServices.IsCatalogSourcedPreview`) is purely data-driven off stored `generation_source`, one-directional both ways, with a defensive guard in the legacy path against a catalog preview slipping through. New nullable catalog-only columns are not required by legacy rows. No regression risk found; nothing in legacy behavior was modified. Verdict: **VERIFIED_IMPLEMENTED**.

## 24. Governance-to-code validation

`AUD-500` through `AUD-561`: fully contiguous, no gaps, proper append-only/superseding pattern (corrections reference what they replace rather than silently overwriting). Three sampled claims (taper multiplier 0.53, TAPER_SHARPEN component order, `CatalogLivePilot` default false) all **match** current code exactly. No new AUD entries were added by this validation pass (none were needed — no new product/architecture decision arose). One filing-hygiene issue: `phase4f9-catalog-confirmation-and-persistence-audit.json` is at repo root instead of `plan-catalog/artifacts/audits/` alongside its ~70 siblings — content is internally consistent, just misfiled; recommend relocating on next commit (not moved in this pass to avoid unrequested repository restructuring).

## 25. Test-quality findings

Sampled files span the full spectrum: `Phase4F5_1ProductionValidatorWiringTests.cs` and part of `Phase4F8_2LivePilotRoutingTests.cs` are `PRODUCTION_PATH_COVERAGE`; `Phase4F7BVolumeAndLongRunTests.cs` is `UNIT_CONTRACT_COVERAGE` (real algorithm, synthetic fixtures); `Phase4F5DarkCalendarWiringTests.cs` is `IN_MEMORY_APPROXIMATION` (hand-built spies, not DI); `CatalogPlanConfirmationServiceTests.cs` is real-service + EF-InMemory but explicitly self-labeled as **not** proving concurrency (`FALSE_CONFIDENCE_RISK` avoided only because it's self-disclosed — the underlying gap in §21 remains real). No DI-resolution test exists anywhere in the backend test suite (`ServiceCollection`/`BuildServiceProvider` grep returns empty) — a real coverage gap for catching broken registrations before they reach `WebApplicationFactory` (which is itself currently fully environment-blocked).

## 26. Corrections applied

None. This pass was read-only investigation across all five parallel workstreams; no source, test, or documentation files were modified. All findings above are reported for the maintainer to correct, per the task's constraint to apply only concrete, narrowly-scoped fixes — given the volume and cross-cutting nature of the findings (ordering defect, identity-mapping governance gap, stale comments, missing tests), a single blind pass was judged too risky to edit without owner confirmation of intended behavior, particularly for the confirmation-ordering defect where "correct" behavior depends on a product decision (should an already-confirmed-but-now-expired preview return idempotently or error?) not unambiguously fixed by the existing docs alone.

## 27. Environment-blocked validation

- All 37 `RunningApp.IntegrationTests` failures (require live PostgreSQL at `localhost:5432/antigravity_dev`).
- Real relational uniqueness-constraint concurrency/rollback behavior (`IX_TrainingPlans_SourcePreviewId`, active-plan invariant) under genuine concurrent load.
- Reset-endpoint (`TestingController`) verification end-to-end.
- Actual migration application and post-migration schema diff against a live database.

## 28. Remaining blockers

1. **Confirmation validation ordering** — expiration check precedes idempotency check, contradicting documented intent; needs a product decision + code fix before relying on idempotent-replay semantics.
2. **Active-plan TOCTOU race** — not proven closed; concurrency tests are self-admittedly sequential-only.
3. **Pilot identity mapping** — `RunningRegularly↔INTERMEDIATE` remains an unapproved, duplicated informal mapping; needs owner sign-off and consolidation into one typed/tested function.
4. **8-week + explicit-zero infeasibility claim** — not reproduced by static arithmetic; needs one concrete runtime/unit test run to confirm actual trigger point (or absence).
5. **Stale "dark/discarded" documentation** in `CatalogPreviewGenerator.cs` and `CatalogPreviewSnapshot.cs` — non-functional but actively misleading; should be corrected before Phase 4F.5–4F.9 docs are committed.
6. Missing DI-resolution unit test and missing isolated prescription-JSON round-trip test — non-blocking but recommended before broader activation work.
7. `CatalogStageKey` dual-write by new code — confirm this is an intentional backward-compatibility decision, not an oversight.
8. Uncommitted state of Phase 4F.5–4F.9 — recommend committing and reviewing before treating any of its "CLOSED"/"CANONICAL_CONFIRMED" governance language as final.

## 29. Relational-validation readiness

`CORRECTIONS_REQUIRED_BEFORE_PHASE4F9_2` — **superseded, see update below.**

Rationale (as originally audited): items 1 and 2 above are exactly the concurrency/idempotency semantics that real PostgreSQL relational testing (Phase 4F.9.2) is meant to prove. Running that verification now, before the ordering defect is resolved and before an active-plan-race test exists even at the unit level, risks either validating incorrect behavior as correct or wasting relational-test time rediscovering what static review already found for free.

**Updated status (Phase 4F.9.1A): `READY_FOR_PHASE4F9_2_RELATIONAL_VALIDATION`.** Confirmation ordering was corrected, the active-plan race now translates to a typed `CatalogActivePlanConflictException` via the pre-existing `IX_TrainingPlans_InternalUserId_ActiveOnly` index, and provider-independent tests now cover both. See `PHASE4F_9_1A_PRE_RELATIONAL_CORRECTIONS.md` for full detail.

## 30. Final conclusion

Phase 4F's generation pipeline (4D resolvers through 4F.8.2 routing) is implemented consistently, deterministically, and fail-closed everywhere checked, with clean builds and no genuine (non-environment) test failures across 1,057 total tests run. Phase 4F.9 persistence mapping and migrations are structurally sound. The two concrete defects blocking relational validation are both narrowly scoped (confirmation validation ordering; unproven active-plan concurrency safety) and should be resolved or explicitly risk-accepted by the product/eng owner before PostgreSQL becomes available and Phase 4F.9.2 begins. No publication, activation, or PostgreSQL provisioning was performed or attempted, per instructions. Stop here.
