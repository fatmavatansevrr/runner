# Phase 4L.2D — GE-to-Runway Partial-Boundary Checkpoint Handoff Root-Cause Resolution

## 1. Executive result

This phase set out to resolve a reported production defect: 25/26/27-week horizons failing a second continuation call with `LongHorizonCheckpointDecisionInvalidException: Next GE week 5 must be NumericPending`, occurring before Runway composition is reached. Direct, non-speculative investigation — isolated calls to the checkpoint runtime, direct calls to the composition orchestrator, stage-by-stage tracing — proved this was **never a production defect**. The GE checkpoint runtime, the boundary-handoff routing, and the JIT composition orchestrator all behave correctly for partial terminal GE windows. The actual root cause: a shared PostgreSQL test fixture (`LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync`) hardcoded `RaceDate = StartDate + 21*7 days` regardless of the plan's real horizon, causing the real Core-generation pipeline to silently reject the first mixed-window composition for non-21-week horizons. Fixed with a one-line test-infrastructure change. Zero production code was modified.

```
LONG_HORIZON_GE_RUNWAY_PARTIAL_BOUNDARY_CHECKPOINT_HANDOFF_ROOT_CAUSE_RESOLVED
```

## 2. Defect discovered after Phase 4L.2C

Phase 4L.2C's own re-diagnosis of the Phase 4L.2B finding classified it as "IndependentDefectRemains" without further investigation (explicitly out of 4L.2C's scope). This phase's job was to close that gap.

## 3. Scope and exclusions

Investigated: the exact failure at horizons 25/26/27. Explicitly not attempted (per this phase's own boundaries): the full Phase 4L.2B matrix, Core-only refresh, the full failure-injection matrix, the full concurrency matrix, any GE/Runway/Core numeric or calendar formula change.

## 4. Real PostgreSQL reproduction

Reproduced directly via `LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync` against the real configured PostgreSQL database, using the production repository, reconstruction service, checkpoint runtime, composition orchestrator, and activation persistence adapter — no EF InMemory, no mocked repository, no hand-built lifecycle snapshot.

## 5. Horizon 25 failure

Pre-fix: `call1=Outcome=Success window=[1,4]` (persisted window unchanged — this was actually a silent BLOCK, since `PersistBlockAsync` does not move the window pointer), plan row showed `CurrentBlockedInternalReasonCode=JitSegmentTransitionInfeasible`. `call2` then threw `LongHorizonCheckpointDecisionInvalidException: Next GE week 5 must be NumericPending`.

## 6. Horizon 26 failure

Identical failure pattern and identical root cause (GE=6 weeks; call1 silently blocked at the same RaceDate-driven Core-generation rejection).

## 7. Horizon 27 failure

Identical failure pattern and identical root cause (GE=7 weeks).

## 8. Boundary trace

A direct isolated call to `LongHorizonRollingCheckpointRuntime.EvaluateAndActivateNextGeWindowAsync` for horizon 25's exact partial-terminal-GE boundary `(5,5)` returned `Outcome=NextGeWindowActivated`, `decisionOutcome=GrowthEligible`, `boundary=[(5,5)]` — proving the checkpoint runtime itself was never the defect. A direct call to `LongHorizonRollingJitCompositionOrchestrator.ComposeAndActivateNextWindowAsync` with a correctly-computed RaceDate (matching the real horizon) returned `CompositionAndActivationSucceeded` with the full expected stage list (`ConditionResolution -> ... -> Phase4K8RuntimeInvocation -> ... -> FinalActivationResultValidation`).

## 9. Structural-boundary authority

Confirmed unchanged and correct: `LongHorizonStructuralRoadmap.GeneralEnduranceWeeks`/`Segments` remains the sole segment-boundary authority. GE weeks are contiguous 1..GeneralEnduranceWeeks; Runway begins immediately after. No second boundary-selection algorithm exists or was added.

## 10. Checkpoint-validator review

`LongHorizonRollingCheckpointRuntime.ValidatePendingBoundary` (the exact throw site of the reported message) was never defective. It correctly threw because week 5 had genuinely already been marked `NumericActivationBlocked` by an earlier, silently-failed call — not because of any GE/Runway coordinate confusion, boundary-clipping bug, or stale-window misread. `ValidatePendingBoundary`'s own boundary computation (`nextEnd = Math.Min(nextStart + 3, GeneralEnduranceWeeks)`) already correctly clips to the structural GE end before validation.

## 11. Terminal-window semantics

Confirmed correct via direct isolated proof (`CheckpointRuntime_AcceptsPartialTerminalGeWindowInIsolation`): a partial final GE remainder (here, week 5 alone) is, and always was, a valid terminal checkpoint window, correctly classified `GrowthEligible`.

## 12. Lifecycle source-of-truth hierarchy

Confirmed unchanged: structural roadmap determines segment ownership; per-week lifecycle rows determine Pending/Activated/Blocked state; checkpoint result determines Growth/Maintenance/Blocked; aggregate `CurrentWindow` fields are convenience metadata correctly populated by `SaveActivationSuccessAsync` and, by intentional Phase 4L.2 design, left unchanged by `PersistBlockAsync` (not a new defect — this design choice is what made the FIRST call's silent block manifest as a confusing SECOND-call symptom, but the design itself is unchanged and correct for its original purpose).

## 13. Exact root cause

`backend/RunningApp.IntegrationTests/.../LongHorizonRunwayCorePostgresRestartTests.cs`'s `LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync` hardcoded `RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(21 * 7)` regardless of the plan's actual `TotalWeeks`. For 25/26/27-week horizons this made the race appear 4-6 weeks sooner than the plan's real structural length. The real, unmodified Core-generation/condition-resolution pipeline (invoked for real inside `LongHorizonRollingJitCompositionOrchestrator`) then rejected the first GE+Runway mixed-window composition and returned `CompositionBlocked`. Since `PersistBlockAsync` does not update `plan.CurrentWindowStartWeek`/`EndWeek`, weeks 5-8 were marked `NumericActivationBlocked` while the durable pointer stayed at `[1,4]`. The second checkpoint call recomputed `nextStart=5` from the stale pointer and correctly rejected the now-Blocked week 5 — the exact reported message, as a downstream symptom of the first call's silent failure.

## 14. Independence from the 4L.2C tuple defect

Fully independent. The 4L.2C ValueTuple JSON round-trip defect manifested inside `ImmutablePreparationRunwayPrescriptionValidator.Validate`'s reuse-scope check, only reachable when `ExistingRunwayPrescription` is non-null (a Runway continuation). This phase's defect is a test-fixture RaceDate error causing a first-entry Core-generation rejection, occurring before any Runway prescription exists at all. Confirmed by direct evidence: the 4L.2C fix (already applied, `FullFidelityJsonOptions`) was in place throughout this phase's investigation and did not affect this defect's reproduction or resolution.

## 15. Minimal production fix

None required. The one-line fix is entirely in test infrastructure: `RaceDate` computed from `state.StructuralRoadmap.TotalWeeks * 7` days instead of a hardcoded `21 * 7`.

## 16. Explicit handoff contract

Not created. The existing `LongHorizonRollingCheckpointRuntimeOutcome.GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached` outcome, combined with the test harness's own `reachesGeBoundary` check (`checkpoint.ActivationWindow.EndGlobalWeek == GeneralEnduranceWeeks`) routing to real JIT composition, already correctly implements the required handoff semantics end-to-end. Building a new competing contract for an already-correct code path was rejected as unnecessary abstraction over a non-defect, per this session's standing "no premature abstraction" principle.

## 17. Handoff validator

Not created, for the same reason as §16 — no production validator gap was found to close.

## 18. Phase 4K.7 behavior after fix

Unchanged (no production code was modified). Re-confirmed correct: if remaining GE weeks exist, only bounded GE weeks are selected/materialized and validated as GE; if no remaining GE weeks exist and the next structural week is Runway, the checkpoint runtime returns the non-error handoff result without invoking the GE materializer; if blocked, the existing blocked result is returned without handoff.

## 19. Phase 4K.8C integration

Confirmed via real Postgres: after the test-harness fix, `LongHorizonRollingJitCompositionOrchestrator` is invoked exactly once per checkpoint call and succeeds, producing the natural GE+Runway mixed window for all three horizons, with real condition resolution, real Core generation, one target lock, one full Runway prescription, and exact session-date projection.

## 20. Natural mixed-window shapes

For all three horizons (25/26/27), the natural mixed shape is identical: initial activation covers weeks 1-4; the first continuation call selects the terminal GE remainder (weeks 5..GeneralEnduranceWeeks) plus Runway weeks up to the 4-week window cap, producing window `[5,8]` with `SegmentsCovered=[GeneralEndurance, PreparationRunway]`; the second continuation call is a pure Runway window `[9,12]`. (GE end differs per horizon — 5/6/7 respectively — but the greedy 4-week cap always produces the same `[5,8]`/`[9,12]` shape for these three specific horizons.)

## 21. Persistence/restart behavior

`PartialTerminalGeWindow_HandsOffToRunwayAndPersistsAcrossRestart` proves, against real Postgres with a brand-new `AppDbContext` after every operation: the mixed window persists and reconstructs exactly; the Runway continuation persists and reconstructs exactly; the target lock/prescription identity is unchanged across the restart (no regeneration); every week `1..window2.EndGlobalWeek` is `Activated` with no gaps.

## 22. Atomicity

Unaffected — the mixed-window activation was already atomic (`LongHorizonRollingActivationWindowValidator.ValidateAtomicity`, unchanged). No new atomicity gap was found or needed closing.

## 23. Replay/idempotency

Unaffected; the existing `IdempotencyKey` mechanism (already proven in Phase 4L.2C) governs this same activation-persistence path and was not touched by this phase.

## 24. Focused concurrency

Not separately re-tested this phase since no production code changed. Existing Phase 4L.2A concurrency coverage remains the current evidence.

## 25. Corruption/fail-closed behavior

Not separately tested this phase for the same reason. Existing Phase 4L.2A corruption coverage remains the current evidence.

## 26. Existing-test weakness

The pre-existing test fixture's hardcoded `21 * 7` RaceDate was written when the fixture was created (Phase 4L.2A) and only ever exercised at the 21-week horizon until Phase 4L.2B first drove it at 25/26/27 weeks — the first real-Postgres test to exercise the wrong-RaceDate path. Phase 4K.9's in-memory harness never shared this fixture and supplied its own scenario-correct race dates directly, which is why it never caught this.

## 27. No-formula-change proof

Zero changes to `backend/RunningApp.Application`. Confirmed via direct diff: this phase's only production-adjacent change is a one-line test-infrastructure fix plus new test files. No GE/Runway/Core numeric formula, calendar algorithm, direction policy, or retry policy was modified.

## 28. Remaining Phase 4L.2B blockers

Unaffected by this closure. `TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001` remains OPEN: Runway→Core mixed restart, Core-only/future-refresh restart, the full transaction failure-injection matrix, and the full concurrency/idempotency matrix are all still unresolved and were not attempted this phase.

## 29. Dark integration

Unchanged. No endpoint, DI registration, background job, confirmation, public-preview, Home/Calendar, completion-handler, API, or Flutter file was touched.

## 30. Governance artifacts

New TD `TD-LONG-HORIZON-GE-RUNWAY-PARTIAL-BOUNDARY-HANDOFF-001`, status **CLOSED** (root cause proven not speculated, exact test-infrastructure correction applied, horizons 25/26/27 pass through real PostgreSQL restart, partial terminal GE windows confirmed valid, no Runway week ever entered GE-only validation, explicit handoff proven to already work, mixed activation persists and reconstructs, no formula changes). Append-only updates added to `TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001` (remains OPEN), `TD-LONG-HORIZON-RUNWAY-CONTINUATION-WINDOW-ADVANCEMENT-001`, `TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001`, `TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001`, `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001`. Phase 4L.2B TD kept OPEN. Confirmation/public-wiring TDs not touched. Aggregate updated to 51 risks, 15 OPEN, 36 CLOSED.

## 31. Tests

2 new focused tests against real PostgreSQL: `PartialTerminalGeWindow_HandsOffToRunwayAndPersistsAcrossRestart` (Theory, horizons 25/26/27 — 3 cases) and `CheckpointRuntime_AcceptsPartialTerminalGeWindowInIsolation`. Full LongHorizon integration suite re-confirmed passing (757/757). Full backend suite and plan-catalog suite re-confirmed passing (see final report for exact counts).

## 32. Public/confirmation/API/Flutter status

Unchanged. All remain entirely unwired, exactly as before this phase.

## 33. Final classification

```
LONG_HORIZON_GE_RUNWAY_PARTIAL_BOUNDARY_CHECKPOINT_HANDOFF_ROOT_CAUSE_RESOLVED
```

```
LONG_HORIZON_PARTIAL_TERMINAL_GE_WINDOWS_NOW_HAND_OFF_AT_THE_STRUCTURAL_RUNWAY_BOUNDARY_WITHOUT_REQUIRING_A_NONEXISTENT_NEXT_GE_WEEK
```

```
LONG_HORIZON_PHASE4K_7_VALIDATES_ONLY_REMAINING_GE_WEEKS_AND_PHASE4K_8C_RETAINS_SOLE_AUTHORITY_FOR_ATOMIC_GE_RUNWAY_MIXED_ACTIVATION
```

```
LONG_HORIZON_HORIZONS_25_26_AND_27_NOW_PERSIST_RESTART_AND_CONTINUE_ACROSS_THE_REAL_GE_RUNWAY_BOUNDARY
```

```
LONG_HORIZON_PHASE4L_2B_REMAINS_OPEN_FOR_CORE_REFRESH_FAILURE_INJECTION_AND_FULL_CONCURRENCY_COMPLETION
```

All five outcomes hold true, though the first four hold true because production code was already correct, not because new production machinery was built to make them true.

## 34. Exact next phase

**Resume Phase 4L.2B — Mixed-Window, Core Refresh, Failure-Injection and Concurrency Completion Matrix.** Both root-cause investigations blocking it (Phase 4L.2C's Runway continuation defect, and this phase's GE→Runway boundary finding) are now resolved. Not Phase 4L.3.
