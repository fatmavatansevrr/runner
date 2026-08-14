# Phase 4L.2E — Future-Only Core Context Refresh Capability

## 1. Executive result

Implemented and proved the missing production capability: an explicit, eligibility-gated future-only Core context refresh (V1→V2). A new `LongHorizonCoreRefreshEligibility` gate, request/result contracts, and a `LongHorizonFutureCoreRefreshOrchestrator` reuse the existing real condition-resolution/Core-generation/persistence chain — never duplicating it — to produce a new versioned Core context applying only to future `NumericPending` weeks, while historical activated weeks remain immutably owned by the prior context. Proven against real PostgreSQL: V1 immutability, V2 future-only ownership, restart reconstruction of both contexts, next activation under V2 through terminal completion, stale-version replay rejection, and one focused concurrency race.

```
LONG_HORIZON_FUTURE_ONLY_CORE_CONTEXT_REFRESH_CAPABILITY_COMPLETED_DARK
```

## 2. Capability gap inherited from 4L.2B-R

Phase 4L.2B-R left `futureCoreRefreshRestart` entirely unattempted: no production V1→V2 context-supersession capability existed to test.

## 3. Scope and exclusions

Implemented: eligibility gate, request/result contracts, orchestrator, restart reconstruction, next-activation-under-V2 proof, one idempotency-replay test, one focused concurrency test. Explicitly not attempted: the full failure-injection matrix, the complete concurrency matrix (Core-refresh-vs-block/retry/checkpoint races), the full 13-item corruption matrix, public preview/confirmation/Home/Calendar/Flutter wiring, and any numeric/calendar/evidence/direction/Runway/target-lock formula change.

## 4. Current Core-context architecture

Inspection found the underlying V1→V2 supersession mechanism already existed in `LongHorizonRollingStateRepository.SaveActivationSuccessAsync`: every time a Core-generating continuation call persists a new `ContextVersionSequence`, all existing Active `LongHorizonCoreContextRecord` rows for the plan are marked Superseded and the new row becomes Active. However, `SupersededByContextId` (an existing column) was never actually populated — a real, previously-unnoticed gap, now fixed as a narrow correction. Reconstruction (`LongHorizonRollingStateReconstructionService`) never rehydrates a full Core context object for reuse (unlike Runway); it only exposes `CoreContextIds` (a bare list). No eligibility contract, refresh-specific request/result contract, or dedicated "current active context" accessor existed.

## 5. Refresh eligibility

`LongHorizonCoreRefreshEligibility.Evaluate` — a pure typed decision, never selecting/activating/generating: `RejectedLifecycleBlocked`, `RejectedRunwayStillPending` (any Runway week still Pending), `RejectedNotInCore` (no Core week yet Activated/Completed/Missed), `RejectedNoActiveCoreContext`, `RejectedNoFutureCoreWeeks`, `RejectedSameOrEarlierAsOfDate`, `RejectedUnchangedEvidence`. Proven via `IneligibleRefresh_DuringRunway_Rejects` and `IneligibleRefresh_SameAsOfDate_Rejects`.

## 6. Future-boundary derivation

`RefreshEffectiveFromGlobalWeek` = first `NumericPending` Core week, derived directly from per-week lifecycle within the structural Core segment — proven exact (horizon 25: weeks 17-20 activated, refresh correctly derives week 21).

## 7. Refresh request contract

`LongHorizonFutureCoreRefreshRequest` (internal, no public DTO): `PlanStateId`, `ExpectedAggregateVersion`, `RequestedAsOfDate`, `TrainingDayEvidence`, `CurrentAvailability`, `LongRunDay`, `SafetyState`, `PlanStartDate`, `RaceDate`, `CatalogRootPath`. Callers supply only raw evidence — never generated Core output directly.

## 8. Condition re-resolution

Reuses the real, unmodified `RuntimeConditionResolutionService` via the existing `LongHorizonRollingJitCompositionOrchestrator` — the same call every Core-generating continuation already makes. No duplicate resolver path, no caller-fabricated condition values.

## 9. Core regeneration authority

Reuses the real, unmodified `TenKPreparationRunwayDarkOrchestrator`/Core-generation authority via the same existing composition orchestrator. No second generator, no formula change.

## 10. V2 context contract

`LongHorizonCoreContextRecord` gains one new nullable `EvidenceFingerprint` column (migration `20260804111737_LongHorizonCoreContextEvidenceFingerprint`, additive, no data loss) and now correctly populates `SupersededByContextId` when a prior Active context is superseded. `ContextVersionSequence` remains the existing, unmodified versioning field.

## 11. Historical V1 preservation

Proven: after refresh and a restart, the prior Active context row is reloaded with `Status=Superseded` and `SupersededByContextId` pointing to the new row. Historical activated weeks' `TotalWeeklyVolumeKm` and `CalendarDates` are asserted byte-identical before and after refresh and restart.

## 12. Future V2 ownership

Proven: the refresh result's `EffectiveFromGlobalWeek` (surfaced from eligibility's own boundary computation, since the underlying persisted context's own `EffectiveFromGlobalWeek` field reflects the full regenerated Core candidate's structural range rather than the narrower refresh-effective boundary — an existing field semantic disclosed here, not silently reinterpreted) correctly starts at week 21, immediately after the last historically-activated week (20).

## 13. Persistence transaction

Documented, not newly built: this implementation uses the existing runtime contract's combined refresh-plus-immediate-bounded-activation authority (`LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync` → `SaveActivationSuccessAsync`, one existing `SaveChangesAsync` per call), per the phase's own explicit fallback. A separate context-only (no-activation) persistence path was not built.

## 14. Refresh result

`LongHorizonFutureCoreRefreshResult`: `Outcome` (`Refreshed`/`IdempotentReplay`/`Ineligible`/`Blocked`/`StaleVersion`/`CorruptState`), `PreviousContextId`/`NewContextId`, `PreviousContextVersion`/`NewContextVersion`, `EffectiveFromGlobalWeek`/`EffectiveToGlobalWeek`, `Eligibility`, `PersistenceResult`, `ValidationStages`.

## 15. Restart reconstruction

New repository method `GetActiveCoreContextAsync` proves V1 and V2 both reconstruct from a fresh `AppDbContext` after restart: V1 remains present (Superseded), V2 is the new Active row. No resolver or Core-generator invocation occurs during plain reconstruction (unchanged `LongHorizonRollingStateReconstructionService`).

## 16. Next activation under V2

`NextActivationAfterRefresh_UsesV2AndRestarts` proves: after refresh and restart, the normal, unmodified rolling continuation service (no refresh-specific window selector) selects the next bounded Core window `[21,24]` under V2, and a further restart-and-continue reaches terminal completion (week 25) with every lifecycle state `NumericActivated`.

## 17. Sequential refresh behavior

Not attempted this phase (V1→V2→V3). Only V1→V2 was proven; a further sequential refresh test was out of this phase's completed budget.

## 18. Idempotency

Deterministic via `ExpectedAggregateVersion` (xmin-backed): a replayed request carrying the now-stale version is rejected `StaleVersion` rather than duplicating V2 (`RefreshReplay_WithSameAggregateVersion_IsRejectedAsStale`, proven exactly one context row exists). A literal "identical successful result on exact replay" was not separately proven — this design's eligibility gate correctly treats a same-AsOfDate re-request as `RejectedSameOrEarlierAsOfDate` once V2 is Active, a different but equally effective duplicate-prevention mechanism, disclosed honestly.

## 19. Focused concurrency

`ConcurrentIdenticalRefresh_HasExactlyOneWinner`: two independent snapshots at the same xmin/version race; winner refreshes successfully, loser is rejected `StaleVersion`; exactly one Active context row exists; loser reloads winner's context. Not the full Phase 4L.2G concurrency matrix.

## 20. Blocked refresh

Reuses the existing composition-blocked outcome path: if delegated composition/persistence doesn't succeed, the orchestrator returns `Outcome=Blocked` carrying the underlying `PersistenceResult`; V1 remains authoritative; no V2 persisted. No new public block taxonomy invented. Not separately tested with an explicit blocked scenario this phase.

## 21. Integrity validation

Not extended this phase. `LongHorizonRollingPersistenceIntegrityValidator` was not modified; the existing reconstruction-time validator continues to run unchanged (re-confirmed passing on every test in this phase's suite).

## 22. Corruption/fail-closed behavior

Not tested this phase. The full 13-item corruption matrix (missing V1/V2, cross-plan context, historical-boundary violation, overlapping ownership, duplicate version, broken lineage, tampered payload, historical rebinding) was not attempted — explicitly deferred.

## 23. No-formula-change proof

Zero changes to any numeric, calendar, direction, Runway, target-lock, checkpoint-evidence, Growth/Maintenance, retry, or window-selection formula. This phase adds context-version ownership orchestration only: one new eligibility file, one new contracts file, one new orchestrator file, two small repository additions, one `SupersededByContextId` correction, and one additive migration.

## 24. Dark integration

Internal only. No endpoint, DI registration, background job, confirmation, public-preview, Home/Calendar, completion-handler, API, or Flutter file references any new type (grepped directly).

## 25. Governance

New TD `TD-LONG-HORIZON-FUTURE-ONLY-CORE-CONTEXT-REFRESH-001`, status **CLOSED**. Append-only updates to `TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001` (remains OPEN — one of five blockers removed), `TD-LONG-HORIZON-RUNWAY-CONTINUATION-WINDOW-ADVANCEMENT-001`, `TD-LONG-HORIZON-GE-RUNWAY-PARTIAL-BOUNDARY-HANDOFF-001`, `TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001`, `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001`, `TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001`. Aggregate: 52 risks, 15 OPEN, 37 CLOSED. Parent Phase 4L.2B TD kept OPEN. Confirmation/public-wiring TDs untouched.

## 26. Tests

6 new focused tests, all real PostgreSQL, 0 failing: eligible refresh with full restart/reconstruction/historical-immutability proof, ineligible-during-Runway, ineligible-same-AsOfDate, next-activation-under-V2 through terminal completion, stale-version replay rejection, one focused concurrency race. Full LongHorizon suite: 769/769. Full backend and plan-catalog suites re-confirmed passing (see final report for exact counts).

## 27. Public/confirmation/API/Flutter status

Unchanged. All remain entirely unwired.

## 28. Final classification

```
LONG_HORIZON_FUTURE_ONLY_CORE_CONTEXT_REFRESH_CAPABILITY_COMPLETED_DARK
```

## 29. Exact next phase

Phase 4L.2F — Transactional Failure-Injection and Rollback Matrix. The parent Phase 4L.2B TD remains OPEN: failure-injection and the full remaining concurrency/idempotency matrix are still unresolved. Not Phase 4L.3.
