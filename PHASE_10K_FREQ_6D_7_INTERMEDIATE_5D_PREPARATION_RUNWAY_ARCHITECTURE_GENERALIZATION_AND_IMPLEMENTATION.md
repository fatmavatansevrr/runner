# Phase 10K-FREQ.6D.7 — Intermediate×5D Preparation Runway Architecture Generalization & Implementation

**Type:** CHECKPOINT GATE → IMPLEMENTATION → INTEGRATED VERIFICATION
**Parent:** FREQ.6D.6 (`INTERMEDIATE_5D_RUNWAY_PRODUCT_POLICY_APPROVED`)
**Final classification:** `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED`

## 1. Scope

Generalize the Intermediate×4D Preparation Runway implementation to also support Intermediate×5D Preparation Runway + canonical Core (15-20 weeks), reusing the existing Runway engine, the existing generic Core pipeline, and the existing generic starting-volume authority — without creating a second 5D Runway implementation, without touching `RUN_LAYOUT_5D` (Core's own structural authority), without a new product decision, and without a new numeric authority. LongHorizon (21+ weeks) was explicitly out of scope and remains untouched.

## 2. Durability gate (pre-implementation)

Performed and closed before any production code changed, per `PHASE_10K_FREQ_6D_7_PREPARATION_RUNWAY_IMPLEMENTATION_DURABILITY_GATE.md`: 22 unpushed commits verified attributable to real, already-reported phases; no secrets found; pushed cleanly (no force); durable baseline recorded (`1a8d6ecbc18dde1957b70078ce9f2e1877144ce7`, backfilled). `DURABILITY_GATE_COMPLETE`.

## 3. Corrected understanding carried into this phase

Direct reading of `PreparationRunwayWeekMaterializer.cs`'s real anchor-selection loop showed that every Runway week always has exactly one `KEY_SESSION`-labeled slot (a hard invariant enforced by `ValidateWeekCardinality`) — the block-role override only decides which single slot receives the week's specific anchor workout versus a generic support default, it never reassigns `SlotRole` itself. An earlier research-subagent summary (inherited into FREQ.6D.5/FREQ.6D.6) had imprecisely characterized certain 4D weeks as having "0 KEY_SESSION" slots structurally; this is a refinement of the mechanism description, not a contradiction of the frozen FREQ.6D.6 decision (1 KEY + 3 EASY + 1 LONG every full 5D Runway week), which remains valid and unchanged.

## 4. Architecture generalization — what changed and why

The generalization follows one governing principle throughout: **role composition (how many KEY/EASY sessions) is a structural fact read from the real, resolved candidate — never a `DaysPerWeek == 4` literal.** Every fix below either (a) reads that fact from the real layout/candidate, or (b) reuses an already-existing "multi-key" generalization mechanism (`FourDaySessionDistanceAllocationPolicy`'s `keySessionCount` parameter, added in Phase 10K-FREQ.4) symmetrically for `EASY_SUPPORT`, rather than inventing a new algorithm.

### 4.1 Routing / candidate resolution
- **`V1CatalogPilotIdentityPolicy`** — added a Preparation-Runway-scoped allow-list (`IsSupportedPreparationRunwayLevelFrequency`/`IsSupportedPreparationRunwayIdentity`/`IsSupportedPreparationRunwayCandidate`), deliberately narrower than Core's own allow-list (which also includes Intermediate×3D and Beginner×4D — neither has an approved Runway product decision, so Core's broader identity set must never leak into Runway eligibility).
- **`PlanServices.IsPreparationRunwayPilotScope`** — now delegates to `IsSupportedPreparationRunwayIdentity` instead of a hardcoded `DaysPerWeek == 4` comparison.
- **`CatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync`** — candidate load now calls `V1CatalogPilotIdentityPolicy.ResolveCandidate(request.Level, request.DaysPerWeek)` instead of an unconditional 4D constant load; the snapshot provenance string is now built from the real `request.DaysPerWeek` instead of a hardcoded `"...4D_PREPARATION_RUNWAY_MATCH"` literal.
- **`TenKPreparationRunwayDarkOrchestrator.ValidateRequest`** — candidate check now uses `IsSupportedPreparationRunwayCandidate` instead of an exact `"TEN_K__4D__INTERMEDIATE"`/`DaysPerWeek != 4` literal (Level/CanonicalDistanceFamily/CoreCycle bounds checks are unchanged, still exact).

### 4.2 Structural layout
- **`TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout`** — parameterized by `daysPerWeek`: 4 → unchanged `RUN_LAYOUT_4D` reference and 4-slot sequence; 5 → new 5-slot sequence (`KeySession, EasySupport×3, LongRun`) under a new, purely-internal, non-catalog-loaded provenance reference (`PREPARATION_RUNWAY_LAYOUT_5D_V1`) — `SourceLayout` is pure provenance metadata, never dereferenced by the materializer, confirmed by reading `PreparationRunwayWeekMaterializationContracts.cs` in full. `RUN_LAYOUT_5D` (Core's own structural authority) is untouched. `BuildBlockRolePolicies`/`BuildSupportPolicy` needed no changes — they already operate identically regardless of layout width.
- **`PreparationRunwayWeekMaterializationContracts.cs`** — added `PreparationRunwayWeeklyShape.IsValid`, the single shared definition of a valid Runway week shape: exactly 1 `KeySession`, exactly 1 `LongRun`, and an *approved* `EasySupport` count (2 or 4D, 3 for 5D — an explicit allow-list, not an unbounded "N≥1" rule, since an arbitrary-width week is not itself approved). Every structural/numeric/pace validator that previously hardcoded an exact 4-slot count now consults this.
- **`PreparationRunwayWeekMaterializer.ValidateRequest`/`ValidateWeekCardinality`** — both now use `PreparationRunwayWeeklyShape.IsValid` instead of an exact 4-slot literal comparison.
- **`TenKPreparationRunwayFinalInvariantValidator`** — `structuralExact` now uses the shared shape check; `counts` now derives `sessionsPerWeek` from `request.Candidate.DaysPerWeek` instead of a literal `4`.

### 4.3 Numeric allocation (the "no new numeric authority" core)
- **`FourDaySessionDistanceAllocationPolicy`** — symmetrically generalized `EASY_SUPPORT` from a fixed count of 2 to an `easySupportCount` parameter (default 2, byte-identical for every existing caller), using the exact same equal-split mechanism (`SplitEvenly`) already established for `KEY_SESSION`'s `keySessionCount` generalization (Phase 10K-FREQ.4). This is not a new algorithm — it is the identical pattern applied to the other role that now also varies.
- **`PreparationRunwayNumericMaterializer`** — the per-week allocation call now passes `easySupportCount` read from the real materialized week's `EasySupport` slot count (Runway itself always has exactly 1 KEY, per FREQ.6D.6, so `keySessionCount` stays implicit/default there); `BuildSlots` now distributes `EasySupportDistancesKm[i]` positionally instead of a hardcoded first/second split; `ValidateRequest`'s two `!= 4` checks now use `PreparationRunwayWeeklyShape.IsValid` and a generic ≥1-KEY/exactly-1-LONG check.
- **`PreparationRunwayCoreWeekOneTargetAdapter`** — the single most load-bearing fix. Previously synthesized a fake, always-1-KEY 4-slot Core-boundary target via `FourDaySessionDistanceAllocationPolicy.Allocate(weekly, longRun)` regardless of the real candidate's shape. Now reads the real KEY_SESSION count directly from Core's own authoritative Week-1 prescribed sessions (`finalPrescribedPlan.Weeks[0].Sessions`) and calls the same allocator with `keySessionCount` equal to that real count — for 5D this is exactly the same shared "V1 multi-key" allocator already driving live 5D Core prescription itself (`V1FourDaySessionVolumeAllocationPolicy` calls the identical `FourDaySessionDistanceAllocationPolicy.Allocate`), not a new derivation. Byte-identical for every existing 4D caller (`keySessionCount` always resolves to 1 there).
- **Runway→Core boundary continuity (`AnalyzeContinuity` in `PreparationRunwayNumericMaterializer`, the parallel check in `PreparationRunwayCalendarComposer.ValidateRequest`, and the parallel check in `PreparationRunwayPaceMaterializer.BuildContinuityChecks`)** — per the frozen FREQ.6D.6 decision ("Core-entry compatibility means exact continuity of total weekly volume and long-run distance ... NOT per-slot role-count equality ... KEY count is explicitly NOT a Core-entry compatibility dimension"), all three now compute a `roleCompositionMatches` flag (real KEY/EASY counts on both sides of the boundary) and only perform per-slot KEY/EASY comparisons when it is true — every existing Intermediate 4D case (`roleCompositionMatches` always true there) is checked byte-for-byte exactly as before; the approved 5D redistribution (1 KEY + 3 EASY → 2 KEY + 2 EASY) is checked on total weekly volume and long-run distance only, which is the correct application of the approved product decision, not a weakened check.
- **`PreparationRunwayCalendarSkeletonAdapter`/`PreparationRunwayCalendarComposer.BuildCombinedUndatedSkeleton`** — both had a hardcoded `DaysPerWeek = 4` on the internal `GeneratedCatalogPlanSkeleton` used for calendar materialization (unrelated to `RUN_LAYOUT_4D`/`RUN_LAYOUT_5D` — this is the calendar day-assignment skeleton's own session-per-week expectation). Fixed to read the real slot count from the materialized week / real Core skeleton respectively. This was the root cause of a `"Week 1 has 5 session slots; expected 4 from resolved RunLayout"` failure surfaced by the new dark 5D tests.
- **`PreparationRunwayCoreWeekOnePaceAdapter`** — the Week-1 pace-target ordinal assignment previously gave every non-`EASY_SUPPORT` session (i.e. every `KEY_SESSION` and `LONG_RUN`) a fixed ordinal of 1. For 5D's real 2-`KEY_SESSION` Core Week 1 this produced two colliding `(KeySession, ordinal=1)` target entries, causing a `Sequence contains more than one matching element` failure downstream. Fixed to assign per-role ordinals generically (a small `Dictionary<PreparationRunwaySlotRole,int>` counter), byte-identical for every existing single-KEY/dual-EASY 4D case.
- **`TenKPreparationRunwayCoreGenerator`** — the Runway-owned Core-call site never threaded a real `ExecutionPrescriptionIndex` into `DynamicCoreCalendarMaterializationContext` (it was always implicitly `null`). Every existing 4D caller has Legacy (non-ProfileBacked) Core Week-1 prescription, so this was invisible until 5D's real, already-live ProfileBacked Week-1 `KEY_SESSION` (`INTERMEDIATE_5D_FOUNDATION_PRIMARY`) became reachable through this call site for the first time. This is exactly the same missing-context defect class FREQ.6D.4D.5G fixed for `CompressedCore`/`ExtendedCore`, at a call site FREQ.6D.4D.5G's own fix did not reach (Runway's own Core generator, a separate call site). Fixed by adding an optional `IPublishedTemplateBundleLoader` dependency and loading the real published-bundle execution index for the resolved candidate, mirroring `CatalogPreviewGenerator.LoadExecutionIndex` exactly; wired into `TenKPreparationRunwayDarkOrchestratorFactory.Create`.

### 4.4 What was confirmed to already be generic (no change needed)
- `TenKPreparationRunwayCoreGenerator`'s call into the shared `IDynamicCoreCalendarMaterializationOrchestrator`/`DynamicCoreSessionPrescriptionOrchestrator`/`CatalogVolumeAndLongRunPlanner` pipeline — the exact same Core pipeline Core itself uses, already fully candidate-generic.
- `V1MissingReadinessStartingVolumePolicy` (16km missing / 12km explicit-zero) — already reused unchanged, confirmed by FREQ.6D.6 to already be the live Intermediate×5D Core numeric authority, not a cross-frequency borrow; no new policy or constant introduced anywhere in this phase.
- `PreparationRunwayBlockAllocationEngine`/block-week role policies — unaffected by session-per-week width, confirmed by direct reading.
- Runway KEY prescription source (Legacy vs ProfileBacked) — Runway's own catalog content (`EASY_STANDARD`, `LONG_RUN_STANDARD`, `AEROBIC_STRENGTH_CONTROLLED_*`) is unchanged and unauthored-new; no Runway-specific ProfileBacked conversion was made.

## 5. Dark verification

A new test file, `TenKPreparationRunwayDarkOrchestrator5DTests.cs`, exercises the real `TenKPreparationRunwayDarkOrchestrator` (the exact same orchestrator the 4D pilot uses — no separate 5D orchestrator was created) end-to-end for the real `TEN_K__5D__INTERMEDIATE` candidate:

- All six 15-20 week horizons × READY/NOT_READY profiles (12 cases): every full Runway week is exactly 1 `KEY_SESSION` + 3 `EASY_SUPPORT` + 1 `LONG_RUN` (5 slots); real Core Week 1 is exactly 2 `KEY_SESSION` + 2 `EASY_SUPPORT` + 1 `LONG_RUN` (the second KEY appears only there, never inside Runway); total sessions equal `totalWeeks × 5`; final invariants (`IsValid`, `NumericContinuity`, `PaceContinuity`, `ProvenanceComplete`) all pass; every Runway week's slot distances sum exactly to its planned weekly total and are all positive; Core-entry continuity (weekly volume + long-run distance) holds within tolerance.
- Missing starting-weekly-volume evidence resolves to exactly 16km via the existing canonical `V1MissingReadinessStartingVolumePolicy`, never a Runway-local literal.
- Positive observed starting evidence (20km) is used directly, not overridden by a default.
- A dedicated regression test confirms the existing Intermediate×4D shape (1 KEY + 2 EASY + 1 LONG, 4 sessions/week) is still produced byte-for-byte through the same now-shared engine.

**Known, disclosed dark-test limitation:** an explicit-zero (0km/0km) recent-evidence combination, fed through the real `CatalogVolumeAndLongRunPlanner` Core also uses for its own Week 1 (pre-existing, unmodified by this phase), produces a residual too small to satisfy the real 5D Core's 2-KEY session minimum at very low evidence. This is a pre-existing Core-side numeric-feasibility edge, not a Runway-generalization defect — Core policy is explicitly out of scope for this phase to change (§ "NO CORE POLICY CHANGE") — and is disclosed here rather than masked; the explicit-zero → 12km resolution itself remains proven generically (candidate-agnostic) by the existing, unmodified `V1MissingReadinessStartingVolumePolicy` test suite. This edge was not independently confirmed to be reachable through the real, non-synthetic resolver pipeline (this dark harness hand-constructs its `CoreEntryReadiness`/evidence inputs rather than exercising the real resolvers), so it is recorded as a residual finding for the next phase to independently verify against real resolver output, not as a confirmed production defect.

## 6. Regression

- `PreparationRunwayWeekMaterializationTests` (28/28), full pre-existing Preparation Runway / `FourDaySessionDistanceAllocationPolicy` / LongHorizon / FREQ.6D.4D suite (1767/1767, zero failures across two independent full runs before and after the final set of fixes), and the new `TenKPreparationRunwayDarkOrchestrator5DTests` (15/15) all pass.
- One pre-existing test (`NonCanonicalFourRoleLayout_IsRejected`) initially regressed when `PreparationRunwayWeeklyShape.IsValid` was first written as an unbounded "≥1 EASY" rule; root-caused and fixed by tightening it to the explicit approved-count allow-list (`{2, 3}`) rather than an arbitrary width — the test now passes again for the correct reason (a 3-slot 1K+1E+1L layout is rejected as non-canonical, not merely "too few slots").
- Debug build: 0 warnings, 0 errors (`RunningApp.Application`, `RunningApp.IntegrationTests`). Release build: 0 warnings, 0 errors (`RunningApp.Application`). `git diff --check`: clean (line-ending-normalization warnings only, no whitespace-conflict errors).
- RuntimeCatalog/PlanCatalog full suites and DB-backed suite were not independently re-run beyond the filtered set above in this pass; the filtered set covers every file this phase touched plus its immediate dependents (LongHorizon, FREQ.6D.4D).

## 7. Not completed in this pass (honest disclosure)

- **No real HTTP E2E** was performed for the newly-reachable public 15-20 week Intermediate×5D route (would require running the live API against a real catalog bundle).
- **No real PostgreSQL confirmation** was performed for a persisted 5D Runway+Core plan or the Runway→Core structural-boundary regression assertion.
- **No global closing hardcoding-audit re-scan** was performed as a separate final pass beyond the systematic file-by-file trace in §4 above (every file touched was found via direct code reading and iterative dark-test failure diagnosis, not a single exhaustive grep sweep at the end).
- Unsupported-neighbor closure (Beginner×5D, Advanced×5D, Intermediate×6D/7D Runway) and the explicit 21+/LongHorizon-remains-closed proof were not independently re-verified in this pass beyond relying on `V1CatalogPilotIdentityPolicy`'s unchanged narrow allow-lists and the unchanged `PlanHorizonCompositionRequiredException` gate for 21+ weeks.

Because of the above, this phase is classified as **implemented and dark-verified**, not **publicly activated** — the routing code is wired to accept the approved Intermediate×5D 15-20 week combination (nothing else), but that has not been independently confirmed through a real HTTP/DB path in this session.

## 8. Technical debt disposition

No formal `TD-RUNWAY-ARCHITECTURE-HARDCODED-SINGLE-CELL-001` (or similarly-named) record exists in the repository's tracked debt ledgers (`plan-catalog/artifacts/audits/activation-readiness-risks.json` and the phase-report chain were both checked) — `PHASE_10K_FREQ_6D_5_...READINESS_AUDIT.md` §21 explicitly states no such record was created there, only flagged for a future phase. This phase resolves, by direct file/line correspondence, every Runway-specific hardcoding location that audit's §6/§7 enumerated (`PlanServices.IsPreparationRunwayPilotScope`; `CatalogPreviewGenerator.cs`'s candidate load; `TenKPreparationRunwayWeekMaterializationPolicyFactory`'s fixed `RUN_LAYOUT_4D`; `TenKPreparationRunwayFinalInvariantValidator`'s hardcoded 4D/4-slot invariants) plus several additional Runway-side hardcodings discovered only through this phase's own implementation (`PreparationRunwayWeekMaterializer`, `PreparationRunwayNumericMaterializer`, `PreparationRunwayCalendarComposer`/`PreparationRunwaySkeletonAdapter`, `PreparationRunwayCoreWeekOneTargetAdapter`/`PreparationRunwayCoreWeekOnePaceAdapter`, `TenKPreparationRunwayCoreGenerator`'s execution-index gap). That audit's suggestion of a separate `TD-LONGHORIZON-ARCHITECTURE-HARDCODED-SINGLE-KEY-001`-equivalent record for LongHorizon's own ~10 hardcoded gates remains correctly un-created — LongHorizon is untouched by this phase.

## 9. LongHorizon (explicitly out of scope, untouched)

No `FREQ.6D.5` LongHorizon finding (session-state lane/stage/profile lineage, JIT dual-KEY composition, `ExecutionIndex` propagation into the rolling-activation paths, the DB migration, the ~10 `DaysPerWeek==4` gates) was implemented here. `LongHorizonFullNumericOrchestrator.cs`, `LongHorizonStructuralMaterializer.cs`, and `LongHorizonRollingJitCompositionOrchestrator.cs` required three narrow, mechanical call-site fixes purely to keep them compiling against this phase's now-generalized shared-engine signatures (`PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior`'s new required parameter; `TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout`'s new required parameter) — each passes the real 4-literal/`candidate.DaysPerWeek` value LongHorizon has always used, zero behavior change, confirmed by the full LongHorizon regression suite passing unchanged.

## 10. Success-boundary checklist

| # | Item | Status |
|---|---|---|
| A | Durability gate performed before any code change | ✅ |
| B | Every FREQ.6D.5-identified Runway hardcoding generalized | ✅ |
| C | All 15-20 week dark plans succeed for both READY/NOT_READY profiles | ✅ |
| D | Exact 1 KEY + 3 EASY + 1 LONG every full Runway week | ✅ |
| E | Exactly 5 sessions/week throughout | ✅ |
| F | Second KEY appears only at Core Week 1 | ✅ |
| G | Missing (16km) resolves via existing canonical authority | ✅ |
| G′ | Explicit-zero (12km) resolves via existing canonical authority | ✅ (proven generically by existing unmodified policy suite; not independently re-proven end-to-end for 5D in this dark harness — see §5/§7) |
| H | Public 15-20 week activation succeeds | ⚠️ Routing wired; not independently confirmed via real HTTP |
| I | Real Postgres confirmation succeeds | ❌ Not performed this pass |
| J | 4D Runway unchanged | ✅ (28/28 + full regression) |
| K | 5D Core unchanged | ✅ (full regression, zero delta) |
| L | LongHorizon 21+ remains closed | ✅ (unchanged gate; not independently re-tested this pass) |
| M | No silent 4D coercion | ✅ (exact candidate resolution, no fallback, by construction) |

## 11. Support matrix after this phase

| Combination | Horizon | Status |
|---|---|---|
| Intermediate×5D Core | 8-14 weeks | `PUBLICLY_ACTIVE` (unchanged, FREQ.6D.4D.5G) |
| Intermediate×5D Preparation Runway + Core | 15-20 weeks | `IMPLEMENTED_DARK_VERIFIED` — routing code wired, not yet independently confirmed via real HTTP/DB |
| Intermediate×5D LongHorizon | 21+ weeks | `CLOSED` / gated (unchanged, `FREQ.6D.5` architecture gap remains) |
| Intermediate×4D Preparation Runway + Core | 15-20 weeks | `PUBLICLY_ACTIVE`, zero-delta (unchanged) |

## 12. Files changed (classification)

**RUNWAY_SUPPORT_GENERALIZATION / RUNWAY_CANDIDATE_RESOLUTION:** `V1CatalogPilotIdentityPolicy.cs`, `PlanServices.cs`, `CatalogPreviewGenerator.cs`, `TenKPreparationRunwayDarkOrchestrator.cs` (`ValidateRequest`/candidate load spots).

**RUNWAY_STRUCTURAL_POLICY:** `PreparationRunwayWeekMaterializationContracts.cs`, `TenKPreparationRunwayWeekMaterializationPolicyFactory.cs`, `PreparationRunwayWeekMaterializer.cs`, `TenKPreparationRunwayFinalInvariantValidator.cs`.

**RUNWAY_CORE_HANDOFF:** `FourDaySessionDistanceAllocationPolicy.cs`, `PreparationRunwayNumericMaterializer.cs`, `PreparationRunwayCoreWeekOneTargetAdapter.cs`, `PreparationRunwayCoreWeekOnePaceAdapter.cs`, `PreparationRunwayPaceMaterializer.cs`, `PreparationRunwayCalendarComposer.cs`, `PreparationRunwayCalendarSkeletonAdapter.cs`, `TenKPreparationRunwayComponentAdapters.cs` (execution-index wiring).

**TEST:** `TenKPreparationRunwayDarkOrchestrator5DTests.cs` (new), `PreparationRunwayWeekMaterializerTests.cs`, `PreparationRunwayNumericMaterializerTests.cs`, `LongHorizonCoreWeekOneEvidenceAuthorityDiagnosticTests.cs` (all three: mechanical call-site updates for new required parameters, zero behavior change).

**UNEXPECTED (mechanical, explained):** `LongHorizonFullNumericOrchestrator.cs`, `LongHorizonStructuralMaterializer.cs`, `LongHorizonRollingJitCompositionOrchestrator.cs` — required only to keep LongHorizon compiling against the two shared-engine signature changes above; each passes the same literal/`candidate.DaysPerWeek` value already in use, verified zero-delta by the full LongHorizon regression suite.

## 13. Next phase

Per this phase's own instruction not to solve LongHorizon here: the next roadmap capability is **Intermediate×5D LongHorizon architecture/design** (not implementation), resolving the exact `FREQ.6D.5` findings — persisted `LongHorizonRollingSessionState` schema gap (no `LaneOrdinal`/`ProgressionStageKey`/`PrescriptionProfileKey` columns), JIT dual-KEY lane-lineage loss in `BuildBoundedCoreSelection`, `ExecutionPrescriptionIndex` propagation into the rolling-activation paths, and the ~10 `DaysPerWeek==4` gates identified in `FREQ.6D.5` §12. Before that, if resumed, this phase's own §7 gaps (real HTTP E2E, real PostgreSQL confirmation, unsupported-neighbor/21+-closed re-verification) should be closed to reach `..._IMPLEMENTED_AND_PUBLICLY_ACTIVATED`.

## 14. Final classification

`INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED`
