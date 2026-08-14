# Phase 4G.5D — Dynamic Core Phase-to-Week Skeleton

## 1. Executive result

A new, dark, unwired generator — `DynamicCoreWeekSkeletonOrchestrator` — generalizes the existing 12-week stage/week skeleton generator to any mathematically feasible standalone-core length (8-14 weeks for `TEN_K_MASTER v6`). It consumes `ICatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)`'s `PhaseAllocationResult` exactly as given — no re-derivation, re-validation, or reinterpretation of phase ordering/priority. **No existing production file was modified.** The existing 12-week live path (`CatalogPlanSkeletonOrchestrator`, `CatalogStageToWeekMaterializer`, `CatalogPreviewGenerator`) is byte/value-level unchanged, proven by a concrete before/after comparison test, not asserted. Zero production call sites confirmed by both grep and a structural reachability test.

## 2. Key finding: the existing week-layout generator was already horizon-agnostic

Before writing any new code, `CatalogStageToWeekMaterializer.cs` (Phase 4F.2) was inspected in full. Its `Materialize(CatalogStageToWeekMaterializationContext)` method already takes `PlannedWeekCount` and `StageWeekAllocations` (an ordered list of `(StageKey, WeekCount)` pairs) as fully generic caller-supplied inputs — nothing in it hardcodes 12 weeks or a 3/4/4/1 split; it validates that the supplied allocation sums to `PlannedWeekCount` and rejects any mismatch, but never invents, rebalances, or assumes one. **This generator required zero changes.**

The actual gap was one level up: `CatalogPlanSkeletonOrchestrator.Build` (Phase 4F.3) always sources its phase allocation via `ICatalogPhaseAllocationResolver.Resolve(candidate)` — the fixed overload that always returns `candidate.CoreCycle.DefaultWeeks` (12) with the catalog's preferred 3/4/4/1 split — rather than the already-existing, already-dark, horizon-agnostic overload `Resolve(candidate, targetWeekCount)` (Phase 4G.3B.2), which returns a `PhaseAllocationResult` for any feasible target week count.

This pass therefore did not "generalize" `CatalogStageToWeekMaterializer` (it was already generic) — it added a new, separate, dark orchestrator (`DynamicCoreWeekSkeletonOrchestrator`) that plugs the already-generic materializer into the already-generic allocator overload, exactly the same terminology-adapter pattern `CatalogStageToWeekContextFactory` already established for the fixed-week path (Phase 4F.3), applied here to `PhaseAllocationResult.Phases` instead of `CatalogPhaseAllocation.Entries`. The existing fixed-week orchestrator, materializer, and every production call path are completely untouched.

## 3. Taper session-slot structure — verified, not assumed

Per this pass's explicit instruction, `CatalogStageToWeekMaterializer.BuildSessionSlots` was inspected directly rather than assumed. It builds session slots from `context.RunLayoutSlotRoles` **identically for every week regardless of `stageKey`** — the `stageKey` parameter is used only for provenance (`SourceStageKey`), never to branch the role sequence. There is **no Taper-specific reduction, volume differentiation, or session-count differentiation anywhere in this materializer**. Confirmed by test (`Build_RealCandidate_EveryWeekIncludingTaperHasFourSessionSlotsInLayoutOrder`, run for all of 8-14 weeks): every Taper week has exactly 4 session slots in the same `KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN` order as every other week. The 4-slot model is therefore preserved for Taper without being forced — it is what the existing code already does.

## 4. Inputs consumed

- **Core phase allocation**: `PhaseAllocationResult` from `ICatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` — consumed via its `Phases` list in the exact order returned; never reordered, re-derived, or re-validated. `AllocationOrderCorrectnessVerifier`'s own scope (order-dependence/approved-priority correctness) is untouched and not duplicated here.
- **Days per week**: `runLayout.StructuralRoles.Count`, from `ICatalogRunLayoutResolver.Resolve(candidate)` — same resolver the fixed-week path already uses.
- **Catalog template**: the already-loaded `PlanCatalogCandidateSummary` (never reloaded or re-searched).
- **Start date metadata**: `StartDate`/`AsOfDate`, threaded through to the materializer exactly as the fixed-week path already does.

## 5. Output shape

The new orchestrator returns the existing `GeneratedCatalogPlanSkeleton`/`GeneratedCatalogWeekSkeleton`/`GeneratedCatalogSessionSlotSkeleton` types (Phase 4F.2), unmodified. Field-name mapping to this task's requested vocabulary (documented terminology debt, following the exact precedent `CatalogStageToWeekContextFactory.Create`'s own doc comment already establishes for "stage" vs. "phase" — not silently resolved, not renamed):

| Task vocabulary | Existing field |
|---|---|
| WeekIndex | `GeneratedCatalogWeekSkeleton.WeekNumber` |
| PhaseType | `GeneratedCatalogWeekSkeleton.StageKey` |
| PhaseWeekIndex | `GeneratedCatalogWeekSkeleton.StageWeekIndex` |
| PhaseWeekCount | `GeneratedCatalogWeekSkeleton.StageWeekCount` |
| Session slots | `GeneratedCatalogWeekSkeleton.SessionSlots` |
| Provenance | `GeneratedCatalogWeekSkeletonProvenance` / `GeneratedCatalogSessionSlotSkeletonProvenance` |

Reusing the existing types (rather than introducing a parallel, differently-named set) was a deliberate choice: it means the new generic path is provably identical in shape and behavior to the already-tested fixed-week path, and the 12-week regression proof (Section 9) is a direct, no-adapter-needed field-by-field comparison. No workout definitions are assigned — `StructuralRole` remains the catalog's own immutable role string (`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN`), never a workout-type prescription.

## 6. Date/index model

This pass **does touch real calendar dates**, not purely index-based: `GeneratedCatalogWeekSkeleton.StartDate`/`EndDate` are populated, because this pass delegates entirely to the existing, unmodified `CatalogStageToWeekMaterializer`, which has its own already-established Phase 4F.2 "Decision 1" date convention: `weekStart = StartDate + (WeekNumber-1)*7 days`, `weekEnd = weekStart + 6 days` (inclusive 7-day blocks, no calendar-week normalization, no Monday alignment, no partial weeks). This orchestrator performs **no date arithmetic of its own** — no new convention was derived. This is a separate, pre-existing convention from `RaceDateAlignmentVerifier`'s race-date trailing-gap tolerance (which governs plan-end-to-race-date alignment, not per-week boundaries) and from `PreparationRunwayAllocation`'s inclusive `CoreStartDate` calculation (which governs runway-to-core-start offset, not per-week boundaries) — neither of those two conventions applies to the question this pass's date usage actually answers (where does week N start/end), so reusing the materializer's own existing Decision 1 convention verbatim, rather than forcing an inapplicable convention onto a different question, was the correct choice.

## 7. Required invariants — how each is satisfied

- **Weeks are contiguous**: `CatalogStageToWeekMaterializer.BuildWeeks` increments `weekNumber` by exactly 1 per generated week with no skip; proven by test (`Build_RealCandidate_WeeksAreContiguousStartingAtOneWithNoGapOrOverlap`, 8-14 weeks) asserting the week-number sequence equals `Enumerable.Range(1, targetWeekCount)` exactly.
- **WeekIndex starts at 1**: same test — the range starts at 1 by construction (`var weekNumber = 1;`).
- **Total weeks equals allocation total**: `GeneratedCatalogPlanSkeletonValidator` (existing, unmodified) checks `ActualWeekCountMismatch`; additionally the materializer's own `ValidateStageAllocation` throws `CatalogStageWeekCountMismatchException` if the allocation sum doesn't exactly equal `PlannedWeekCount` — this orchestrator sets `PlannedWeekCount = context.TargetWeekCount` and passes the resolver's `Phases` through unmodified, so the two are guaranteed consistent by construction (the resolver itself only returns `IsMathematicallyFeasible=true` when its own phases sum to the target).
- **Each phase receives exact allocated count**: proven by test (`Build_RealCandidate_EachPhaseReceivesExactAllocatedCountAndOrderIsPreserved`) — each phase's week count in the skeleton equals `AllocatedPhase.AllocatedWeeks` exactly.
- **Phase order is preserved**: the same test asserts the skeleton's own distinct phase-key sequence equals `PhaseAllocationResult.Phases`'s own order exactly — this orchestrator never sorts, reverses, or reorders.
- **Taper occupies final phase weeks**: proven by test (`Build_RealCandidate_TaperOccupiesFinalPhaseWeeks`, 8-14 weeks) — the last `TAPER.AllocatedWeeks` weeks are all `TAPER`, and no non-`TAPER` week exists after that point. (This is a consequence of `TAPER` being catalog-declared last in `ten-k-master.v6.json`'s `phases[]` array and this orchestrator preserving that order — not something this orchestrator enforces itself, consistent with "never re-derive/re-validate phase ordering.")
- **No week belongs to two phases / no gap exists**: both are structural consequences of `BuildWeeks`'s single sequential loop (each `weekNumber` is assigned exactly once, in exactly one phase's inner loop) — proven by the same contiguity test (no duplicate week numbers) and the phase-order-preservation test (each phase's week range is a contiguous, non-overlapping slice).

## 8. Test matrix (8-14 weeks)

All 7 target week counts were generated and validated against the real `TEN_K__4D__INTERMEDIATE v10` candidate:

| Weeks | Foundation | Build | Race-Specific | Taper | Total sessions |
|---|---|---|---|---|---|
| 8 | 2 | 3 | 2 | 1 | 32 |
| 9 | 2 | 3 | 3 | 1 | 36 |
| 10 | 2 | 3 | 4 | 1 | 40 |
| 11 | 2 | 4 | 4 | 1 | 44 |
| 12 | 3 | 4 | 4 | 1 | 48 |
| 13 | 4 | 4 | 4 | 1 | 52 |
| 14 | 4 | 5 | 4 | 1 | 56 |

Every row confirmed by `Build_RealCandidate_ProducesExpectedTotalAndPhaseWeekCounts` (theory, 7 cases) and `Build_RealCandidate_EveryWeekIncludingTaperHasFourSessionSlotsInLayoutOrder` (theory, 7 cases: 4 slots/week, `weeks*4` total, verified Taper-inclusive since no Taper-specific slot count exists — see Section 3).

## 9. Required verification

**Zero production call sites**: confirmed two ways —
1. A repo-wide grep for `DynamicCoreWeekSkeleton(Orchestrator|OrchestrationContext|OrchestrationResult)` across all four production projects (`RunningApp.Application`, `.Api`, `.Infrastructure`, `.Persistence`), excluding the new file's own source and `bin`/`obj` build output, returned zero source-code matches.
2. A structural reachability test (`DarkReachability_NoProductionCallSite`, matching this session's established grep-based pattern for every prior dark component) performs the identical scan as an executable, always-run assertion — `Assert.Empty(hits)`. A companion test (`DarkReachability_NoDiRegistration`) separately confirms `IDynamicCoreWeekSkeletonOrchestrator` has no reference anywhere in `RunningApp.Api` (where all DI registration lives), so it cannot be resolved from any service scope. `CatalogPreviewGenerator.cs`'s `git diff` is empty — confirmed untouched.

**Byte/value-level 12-week regression**: `Build_TargetWeekCount12_MatchesExistingFixedWeekOrchestratorExactly` builds the 12-week skeleton via the **existing, completely unmodified** `CatalogPlanSkeletonOrchestrator` (the real live-12-week path) and, separately, via the new `DynamicCoreWeekSkeletonOrchestrator` requesting `targetWeekCount=12`, then asserts full field-by-field equality between every week (`WeekNumber`, `StartDate`, `EndDate`, `StageKey`, `StageWeekIndex`, `StageWeekCount`) and every session slot (`SlotOrderInWeek`, `LayoutSlotKey`, `StructuralRole`) — a concrete comparison, not a bare "structurally identical" claim. The existing path's own output is additionally pinned to literal hard-coded values captured from the real run (`PlannedWeekCount=12`, `StartDate=2026-08-05`, `EndDate=2026-10-27`, phase sequence `FOUNDATION×3, BUILD×4, RACE_SPECIFIC×4, TAPER×1`, `48` total session slots), so a future accidental change to either path is caught even if both changed identically. All assertions pass.

## 9a. Existing-test reconciliation (one pre-existing dark-reachability test required an honest update)

A full backend suite run after adding the new orchestrator surfaced exactly one failure: `Phase4G3B2GenericPhaseAllocatorTests.Resolve_TargetWeekCountOverload_HasNoCallSiteInApplicationProductionCode` (Phase 4G.3B.2's own dark-reachability test), which asserted the two-argument `Resolve(candidate, targetWeekCount)` overload had **zero** call sites anywhere in `RunningApp.Application`/`RunningApp.Api`. That invariant is now literally false: `DynamicCoreWeekSkeletonOrchestrator.cs` is a genuine new call site.

This is not a defect to work around — it is the expected, correct consequence of this pass wiring the previously-uncalled generic allocator overload into a new (still fully dark) consumer. The test was updated, not weakened: renamed to `Resolve_TargetWeekCountOverload_HasNoCallSiteOutsideTheOneApprovedDarkConsumer`, with one new file-path exclusion (`DynamicCoreWeekSkeletonOrchestrator.cs`, following the exact precedent already set for excluding the resolver's own definition file's XML doc comments) and an updated doc comment explaining why: the overload's real invariant — unreachable from any *live* request path — still holds, because the one new consumer is itself proven dark by two separate tests (Section 9). The regex scan itself, and every other exclusion, is unchanged. This is the only existing file modified by this pass.

## 10. Focused test results

```text
DynamicCoreWeekSkeletonOrchestratorTests: 40 passed, 0 failed, 0 skipped
CatalogPlanSkeletonOrchestratorTests + CatalogStageToWeekMaterializerTests + CatalogStageToWeekContextFactoryTests (existing, unmodified): 58 passed, 0 failed, 0 skipped
Phase4G3B2GenericPhaseAllocatorTests + DynamicCoreWeekSkeletonOrchestratorTests (combined re-run after Section 9a's reconciliation): 70 passed, 0 failed, 0 skipped
```

## 10a. Full backend Run 1 (transient failure observed, re-run, confirmed unrelated)

The first full-suite run after this pass's changes reported one failure: `FitnessEvidenceInputContractTests.GeneratePreview_AcceptsRecentRaceFinishTimeSeconds`, via an `HttpRequestException: 500 (Internal Server Error)` inside its own `ResetAsync()` helper (the shared reset-endpoint used before every relational test). This is not a file this pass touched, and matches this repository's own already-documented `TD-TESTFLAKE-001` pattern exactly (transient reset-endpoint HTTP 500s, previously observed once and not reproduced in two subsequent clean runs during Phase 4G.3B.1). The full suite was re-run immediately: it passed clean (`1462 passed, 0 failed, 1 skipped, 1463 total`), confirming the failure was transient and unrelated to this pass's changes. That clean run is recorded as Run 1 below; a second clean run was then performed as Run 2.

```text
Run 1: Failed: 0, Passed: 1462, Skipped: 1, Total: 1463
Run 2: Failed: 0, Passed: 1462, Skipped: 1, Total: 1463
```

Both runs match exactly (the passed count is one higher than earlier phases' `1422` because it now includes this pass's 40 new `DynamicCoreWeekSkeletonOrchestratorTests`, plus the pre-existing suite total).

## 11. Compatibility

The existing 12-week output is proven structurally and value-identical to before this pass — see Section 9. No public endpoint, controller, or `CatalogPreviewGenerator` code path was touched, so public preview/confirm behavior is unaffected by construction (confirmed also by the unchanged `git diff` on `CatalogPreviewGenerator.cs`).

## 12. Files changed

- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/DynamicCoreWeekSkeletonOrchestrator.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/DynamicCoreWeekSkeletonOrchestratorTests.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Phase4G3B2GenericPhaseAllocatorTests.cs` (existing file, one test updated — see Section 9a; no production file modified).
- `PHASE4G_5D_DYNAMIC_CORE_WEEK_SKELETON.md` (new, this document).

No production (`RunningApp.Application`/`.Api`/`.Infrastructure`/`.Persistence`) file was modified — only one existing *test* file required a reconciliation update, documented in Section 9a.

## 13. Stop conditions — none triggered

- Generator assumes fixed 3/4/4/1 internally: **not triggered** — `DynamicCoreWeekSkeletonOrchestrator` and the reused `CatalogStageToWeekMaterializer` are both driven entirely by the resolver's `PhaseAllocationResult`/`AllocatedPhase` values; no week-count literal appears in either.
- Phase boundary off by one: **not triggered** — contiguity/no-gap/no-overlap tests pass for all 7 week counts.
- 12-week output changes unexpectedly: **not triggered** — Section 9's regression test proves exact equality.
- Public output changes: **not triggered** — no public/live file touched.
- Taper slot structure silently forced without verification: **not triggered** — Section 3 documents the actual verification performed before any assumption was made.
- Zero-call-site proof cannot be established: **not triggered** — established via grep and executable test.

## 14. Commit/push status

No file was staged. No commit or push was performed.
