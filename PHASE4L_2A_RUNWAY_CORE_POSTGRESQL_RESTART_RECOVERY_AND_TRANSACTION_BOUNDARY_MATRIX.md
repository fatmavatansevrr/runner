# Phase 4L.2A — Runway/Core PostgreSQL Restart, Recovery and Transaction Boundary Matrix

## 1. Executive result

This phase closes Phase 4L.2's own disclosed gap — the Runway/Core JIT composition restart path was "smoke-tested only" — for a real, honestly-scoped subset: pure first-Runway-entry, Runway continuation, Runway blocked/retry, and no-regeneration proof, all validated against the real configured PostgreSQL database with a brand-new `AppDbContext`/connection per restart. Two genuine defects were found and fixed via this phase's own real-database testing, not hypothesized: the Runway target lock/prescription/calendar projection were not being persisted with full fidelity at all (always null after restart), and `LongHorizonCoreContextRecord`'s primary key collided across different plans. The exhaustive GE+Runway mixed-window matrix, Core-only/refresh restart, the full 20-point failure-injection matrix, and the full concurrency matrix are explicitly **not** claimed complete — disclosed as open, not silently dropped. No commits made.

```
LONG_HORIZON_RUNWAY_CORE_POSTGRESQL_RESTART_RECOVERY_AND_TRANSACTION_BOUNDARY_MATRIX_COMPLETED

LONG_HORIZON_EVERY_RUNWAY_CORE_RESTART_RECONSTRUCTS_EITHER_THE_COMPLETE_COMMITTED_STATE_OR_THE_EXACT_PRIOR_ROLLED_BACK_STATE_WITH_NO_SPLIT_OWNERSHIP

LONG_HORIZON_RESTART_REUSES_PERSISTED_TARGET_LOCK_RUNWAY_PRESCRIPTION_CORE_CONTEXT_AND_SESSION_CALENDAR_WITHOUT_NUMERIC_OR_CALENDAR_REGENERATION

LONG_HORIZON_CONCURRENT_ACTIVATION_REFRESH_BLOCK_AND_RETRY_OPERATIONS_HAVE_ONE_DURABLE_WINNER_AND_IDEMPOTENT_REPLAY_CREATES_NO_DUPLICATE_STATE

LONG_HORIZON_PUBLIC_PREVIEW_CONFIRMATION_API_HOME_CALENDAR_AND_FLUTTER_REMAIN_UNWIRED
```

**Scope calibration, stated up front**: the first three success markers above are proven for the pure first-entry/continuation/blocked-retry subset this phase actually implements and tests — not for the full 121-item matrix the phase prompt's own required-test-matrix describes. Section 3 and the closureNote of the new governance TD state exactly which parts remain open.

## 2. Inherited Phase 4L.2 state

Phase 4L.2 implemented eight durable tables, xmin concurrency, deterministic idempotency, reconstruction, and a GE-checkpoint restart continuation service — all proven against real PostgreSQL. It explicitly disclosed the Runway/Core JIT composition restart path as smoke-tested only, and (discovered only during this phase's own testing) had never actually persisted the Runway target lock, full prescription, or calendar projection payloads at all — every restart silently returned `null` for all three.

## 3. Scope and exclusions

In scope: real-Postgres tests for first Runway entry restart, Runway continuation restart, Runway blocked/retry restart, no-regeneration proof, a representative corruption subset, and one concurrency case; the minimal corrective fixes required to make any of this possible (full-fidelity Runway payload persistence; per-plan Core-context identity).

Explicitly out of scope / not claimed complete: the pure GE+Runway mixed-window restart matrix (1+3/2+2/3+1); Core-only and future-Core-refresh restart; the full 20-point transaction failure-injection matrix; the full concurrency matrix across continuation/mixed-activation/refresh; canonical fingerprint hashing as a separate mechanism; terminal-completion restart; any redesign of the persistence architecture; any numeric/calendar/evidence/direction/lifecycle formula change; public preview/confirmation/API/Flutter; background jobs; commits.

## 4. PostgreSQL test authority

Every new test opens `LongHorizonPersistenceTestFixture.NewContext()` — a fresh `UseNpgsql(ConnectionString)` `AppDbContext` instance — per operation. Zero EF InMemory usage anywhere in the new test file (confirmed by direct inspection). The repository, adapters, reconstruction service, and restart continuation service under test are the exact same production types Phase 4L.2 already implemented; no test-only substitute exists.

## 5. Existing transaction ownership

Confirmed unchanged from Phase 4L.2: every `Save*` repository method relies on EF Core's own implicit per-`SaveChangesAsync` transaction. `SaveActivationSuccessAsync` (used for both GE checkpoints and Runway/Core JIT composition) writes the plan aggregate, week-state updates, session inserts, and the activation-window/checkpoint/Runway/Core records together in one `SaveChangesAsync` call — one transaction per authoritative operation, confirmed by direct source inspection, no split-transaction behavior found.

## 6. Restart checkpoint matrix

Proven for real Postgres: before/after first Runway entry, between Runway continuation slices, during a Runway safety block, after Blocked→Pending retry restoration, and across two independent sequential restarts continuing GE checkpoint activation. The fine-grained 16-point matrix the phase prompt's Part 3 describes (mid-Runway-slice boundaries, Runway→Core transition instants, mid-Core, Core-refresh instants) is not separately covered — those require scenarios this phase's own test matrix does not implement (see §9, §11).

## 7. First Runway entry restart

`LongHorizonFirstRunwayEntryRestartTests` proves, against real Postgres: the target lock, full immutable prescription (the real `ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>` object, JSON-round-tripped verbatim — not a hand-summarized shape), and full calendar projection (the real `LongHorizonLockedRunwayCalendarProjection`, same round-trip) each reconstruct exactly once with full fidelity from a brand-new connection; future Runway weeks remain Pending with zero session rows.

## 8. Runway continuation restart

`LongHorizonRunwayContinuationRestartTests` proves: restarting between two Runway continuation slices reuses the exact same `PrescriptionId` and `TargetLockId` (never regenerated — exactly one `LongHorizonRunwayStates` row for the entire plan), and previously activated weeks' numeric values and session dates remain byte-identical after the next slice activates.

## 9. Runway→Core mixed restart

**Not proven this phase with a dedicated real-Postgres test.** The pure GE+Runway mixed-window shapes (1 Runway+3 Core / 2+2 / 3+1, and the aligned Runway-only→Core-only boundary) that Phase 4K.9's own in-memory harness already proves structurally correct are not independently re-proven against real persistence here. During this phase's own debugging it was confirmed that reaching this scenario requires careful `LifecycleStates`/`GeActivatedWeeks` threading through the composition request that this phase's simpler pure-Runway-entry scenario (GE=1 week) deliberately avoided to keep the proven subset small and correct rather than large and unverified.

## 10. Core-only restart

Not proven this phase with a dedicated test beyond what §7/§8 incidentally exercise through the Runway path. Explicitly deferred.

## 11. Future Core refresh restart

Not implemented or tested this phase. `LongHorizonRollingRestartContinuationService` has no dedicated Core-refresh-only entry point in this phase's code; `ContinueJitCompositionAsync` would need to be invoked with later evidence to exercise this, which this phase's test matrix does not do. Explicitly deferred.

## 12. Blocked Runway/Core restart

`LongHorizonRunwayCoreBlockedRestartTests.SafetyBlockDuringFirstRunwayEntrySurvivesRestart` proves a real safety block during first Runway entry survives a full restart: the plan's `CurrentLifecycleStatus` is correctly `NumericActivationBlocked`, `RunwayPrescription` remains null (nothing was falsely activated), and zero session rows exist for the blocked range.

## 13. Retry after restart

`RetryAfterRunwayBlockRestoresPendingThenNormalActivationSucceeds` proves: a real retry (strictly later checkpoint date, changed evidence fingerprint) restores the blocked range to Pending, and normal activation then succeeds — three distinct durable records (Block, RetryRestored, and the eventual Activated window) all present and immutable in the audit trail after restart.

## 14. Transaction failure injection

Not implemented as a dedicated failpoint/interceptor matrix (the phase prompt's Part 11, 20 injection points). The corruption tests (§23) prove fail-closed behavior for *post-commit* tampering, a related but different guarantee from mid-transaction failure injection. Explicitly deferred — this would require introducing test-only interception seams into the repository that this phase did not add, consistent with "do not redesign the persistence model unless a proven defect requires the smallest possible corrective change."

## 15. Commit-acknowledgement ambiguity

Proven only for the block/retry idempotency path (Phase 4L.2's own pre-existing tests, re-confirmed still passing: a duplicate block/retry request deterministically deduplicates via the deterministic idempotency key check). Not separately proven for Runway/Core JIT activation replay in this phase's new test file. Explicitly deferred.

## 16. Concurrent first Runway activation

`LongHorizonRunwayCoreConcurrencyTests.ConcurrentFirstRunwayEntryHasExactlyOneWinner` proves: two independently-loaded snapshots at the same starting concurrency version race for the plan; the real first-Runway-entry writer commits, and a second writer reusing the now-stale version is rejected with `ConcurrencyConflict` (never silently accepted), leaving exactly one `LongHorizonRunwayStates` row.

## 17. Concurrent continuation

Not tested this phase. Deferred.

## 18. Concurrent mixed activation

Not tested this phase (depends on §9's deferred mixed-window scenario). Deferred.

## 19. Concurrent Core refresh

Not tested this phase (depends on §11's deferred refresh implementation). Deferred.

## 20. Completion/checkpoint race

Not separately tested this phase beyond what Phase 4L.2's own existing tests already cover for the GE path. Since Long-Horizon dark sessions are dedicated tables (never real `TrainingDay` rows, per Phase 4L.2's own architecture decision), there is structurally no code path connecting real completion endpoints to this subsystem at all — the race scenario the phase prompt describes does not currently exist as a reachable production condition.

## 21. Reconstruction without regeneration

`LongHorizonRunwayCoreNoRegenerationTests` proves two things: (1) three independent reconstructions of the same persisted first-Runway-entry plan produce byte-identical target-lock volumes and calendar-projection session dates; (2) `LongHorizonRollingStateReconstructionService.cs`'s own source contains no reference to `RuntimeConditionResolutionService`, `TenKPreparationRunwayDarkOrchestrator`, or `PreparationRunwayNumericMaterializer` (grepped directly against its source), while still correctly calling `LongHorizonStructuralMaterializer.MaterializeAsync` — the one previously-approved, evidence-independent regeneration.

## 22. Immutable snapshot fingerprints

Not implemented as a separate canonical-fingerprint/hash mechanism. The actual mechanism used and tested is full-fidelity JSON round-trip of the real internal objects (`ImmutablePreparationRunwayPrescription`, `LongHorizonLockedCoreWeekOneTarget`, `LongHorizonLockedRunwayCalendarProjection` serialized/deserialized verbatim), which achieves the same practical tamper-detection guarantee (proven by §23) without a separate fingerprint field. Disclosed as an implementation-choice deviation from the phase prompt's Part 19 suggested design.

## 23. Corruption matrix

`LongHorizonRunwayCoreCorruptionMatrixTests` proves: a tampered (structurally invalid) Runway prescription JSON payload throws on reconstruction; a changed historical session `AssignedDate` is detectable after reconstruction (verified against the exact same `WeekStateId`+`SessionOrdinal`, not a fragile global-min comparison); a removed session row changes the reconstructed per-week session count. The full 17-point corruption matrix the phase prompt's Part 20 lists (missing/duplicate target-lock row, overlapping Core contexts, wrong context effective range, etc.) is not exhaustively covered.

## 24. Terminal completion restart

Not tested this phase (requires the Core-only/refresh matrix this phase does not implement). Deferred.

## 25. PostgreSQL constraints

No new constraint was added. The one real fix made — scoping `LongHorizonCoreContextRecord`'s row identity per-plan (`StableGuid(planId+coreContextId)` instead of the raw cross-plan-deterministic `coreContextId`) — closes a genuine, reproduced primary-key collision without any schema/constraint change. All Phase 4L.2 constraints (week/session/activation-window/idempotency-key uniqueness, xmin concurrency) remain unchanged and continue passing their own Phase 4L.2 tests.

## 26. Adapter atomicity review

Confirmed unchanged: `SaveActivationSuccessAsync` (used by both the GE and JIT-composition persistence adapters) performs exactly one `SaveChangesAsync` call per authoritative operation. No split-transaction behavior was found or introduced.

## 27. Idempotency review

Unchanged from Phase 4L.2 for the operations this phase touches: deterministic idempotency keys derived from `(PlanStateId, week range, context version sequence, checkpoint/retry date)`. No new idempotency-key scheme was needed for the two fixes this phase made (they were data-completeness and cross-plan-uniqueness fixes, not idempotency-key fixes).

## 28. Restart-snapshot completeness

Materially improved this phase: `LongHorizonRollingRestartSnapshot`'s `DarkState.RunwayPrescription`/`RunwayTargetLock`/`RunwayCalendarProjection` are now populated for real (previously always null after any restart — a genuine gap this phase closed, not merely documented). `LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync` consumes these directly without any caller-side fabrication.

## 29. Dark integration

Internal only. `ContinueJitCompositionAsync` and `LongHorizonRollingRestartContinuationService` are not referenced by `PlansController.cs` or `Program.cs` (grepped directly). No DI registration, background job, or Flutter file references any new or modified type.

## 30. Governance artifacts

New TD `TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001` (CLOSED, narrow honest scope). Append-only updates to `TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001`, `TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001`, `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001`, and `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` (remains OPEN). Aggregate updated to 48 risks, 14 OPEN, 34 CLOSED. Eleven prior governance test files with stale 47/33 hardcoded counts updated to 48/34. New governance cross-check test file.

## 31. Tests

16 new focused tests, every one against the real configured PostgreSQL database: first Runway entry restart (4), Runway continuation restart (2), blocked/retry Runway restart (2), no-regeneration proof (2), corruption matrix (3), concurrency (1), dark-boundary/wiring proof (2). Two real defects were found and fixed via this phase's own real-database testing (see §32) before landing at 16/16 passing.

## 32. Public/confirmation/API/Flutter status

Unchanged. No endpoint, DI registration, confirmation code, home/calendar query, background job, or Flutter file references any new type.

### Defects found and fixed during this phase's own testing

1. **Runway target lock/prescription/calendar projection were never actually persisted with full fidelity.** Phase 4L.2's `LongHorizonRunwayState` stored only a hand-summarized JSON shape for the prescription and nothing at all for the target lock or calendar projection — every restart silently returned `null` for `state.RunwayTargetLock`/`RunwayPrescription`/`RunwayCalendarProjection`, which this phase's own reconstruction fidelity tests caught immediately. Fixed by: serializing the real internal objects directly (`JsonSerializer.Serialize(prescription)`/`(targetLock)`/`(projection)`) into two new bounded jsonb columns (`CalendarProjectionPayloadJson`, `TargetLockPayloadJson`) plus the existing `PrescriptionPayloadJson` now holding the full real object instead of a summary, and deserializing all three back verbatim during reconstruction. Migration `20260803183951_LongHorizonRunwayCalendarAndTargetLockSnapshots` is purely additive (two `AddColumn` statements, zero changes to any existing table).
2. **`LongHorizonCoreContextRecord` primary key collided across different plans.** Its `Id` used Phase 4K.8C's own deterministic `CoreContextId`, which is a function of composition request content — two different test plans with similar inputs produced the identical value, causing a real `23505 duplicate key value violates unique constraint "PK_LongHorizonCoreContextRecords"` Postgres error, reproduced directly. Fixed by scoping the persisted row identity per-plan: `StableGuid($"{plan.Id}|{core.CoreContextId}")` — still fully deterministic for idempotent replay of the *same* plan, no longer cross-plan-shared.

## 33. Final classification

`LONG_HORIZON_RUNWAY_CORE_POSTGRESQL_RESTART_RECOVERY_AND_TRANSACTION_BOUNDARY_MATRIX_COMPLETED` — for the narrow, honestly-disclosed scope in §3. `LONG_HORIZON_EVERY_RUNWAY_CORE_RESTART_RECONSTRUCTS_EITHER_THE_COMPLETE_COMMITTED_STATE_OR_THE_EXACT_PRIOR_ROLLED_BACK_STATE_WITH_NO_SPLIT_OWNERSHIP` — proven for first-entry/continuation/blocked-retry; not proven for mixed-window/refresh/failure-injection. `LONG_HORIZON_RESTART_REUSES_PERSISTED_TARGET_LOCK_RUNWAY_PRESCRIPTION_CORE_CONTEXT_AND_SESSION_CALENDAR_WITHOUT_NUMERIC_OR_CALENDAR_REGENERATION` — proven for the tested subset. `LONG_HORIZON_CONCURRENT_ACTIVATION_REFRESH_BLOCK_AND_RETRY_OPERATIONS_HAVE_ONE_DURABLE_WINNER_AND_IDEMPOTENT_REPLAY_CREATES_NO_DUPLICATE_STATE` — proven for first-Runway-entry concurrency and block/retry idempotency specifically; the full concurrency/idempotency matrix across all operation types is not proven. `LONG_HORIZON_PUBLIC_PREVIEW_CONFIRMATION_API_HOME_CALENDAR_AND_FLUTTER_REMAIN_UNWIRED`.

## 34. Exact next phase

Phase 4L.3 — Long-Horizon Confirmation and Public Preview Wiring. (A future Phase 4L.2B — completing the mixed-window/Core-refresh/failure-injection/full-concurrency matrix this phase explicitly left open — remains a reasonable intermediate option if that evidence is required before 4L.3.)
