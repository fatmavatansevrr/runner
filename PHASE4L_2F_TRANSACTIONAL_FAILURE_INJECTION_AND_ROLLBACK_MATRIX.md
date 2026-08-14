# Phase 4L.2F — Transactional Failure-Injection and Rollback Matrix

## 1. Executive result

Built a production-inert, test-controlled failure-injection seam and proved representative pre-commit rollback and post-commit idempotent recovery against real PostgreSQL for all five required operation groups (mixed Runway→Core activation, Core-only activation, future-only Core refresh, block persistence, retry restoration). The transaction-boundary inventory confirmed every authoritative Long-Horizon persistence method already commits atomically via exactly one EF `SaveChangesAsync` call — no split-transaction defect existed to fix. However, the full suggested 95-item stage/operation matrix and real PostgreSQL constraint-exception rollback testing were not attempted, so this phase does not achieve full closure.

```
LONG_HORIZON_TRANSACTIONAL_ROLLBACK_MATRIX_REMAINS_BLOCKED_BY_EXPLICIT_TRANSACTION_FAILPOINT_SNAPSHOT_RECONSTRUCTION_CONSTRAINT_OR_REPLAY_GAP
```

Specifically blocked by: real PostgreSQL constraint-exception rollback testing (not attempted) and the full stage-coverage matrix (only representative coverage achieved).

## 2. Capability gap inherited from 4L.2B-R

Phase 4L.2B-R left the entire failure-injection/rollback capability group unattempted.

## 3. Scope and exclusions

Built: the failpoint seam, transaction-boundary inventory, representative rollback+replay tests per operation group, one post-commit acknowledgement test. Not attempted: the full concurrency matrix, idempotency-policy redesign, public/API/Flutter changes, background jobs, any formula change.

## 4. Transaction-boundary inventory

Confirmed via direct inspection: `SaveActivationSuccessAsync`, `SaveBlockAsync`, `SaveRetryRestorationAsync`, and `InitializeStructuralStateAsync` each call `_db.SaveChangesAsync()` exactly once, with no explicit `BeginTransaction` anywhere in the codebase. This means every authoritative operation is already atomic — EF Core's own Unit-of-Work pattern batches all staged entity changes (Adds, updates) into one PostgreSQL transaction at that single call. Classification: A/C-equivalent for every operation; zero Classification D (split-transaction) findings, so no atomicity fix was required before matrix testing could proceed.

## 5. Failure-injection design

`LongHorizonPersistenceOperation` (6 values) × `LongHorizonPersistenceFailpoint` (7 values). Because every operation commits via one `SaveChangesAsync` call, every stage before `BeforeCommit` shares the identical underlying atomicity guarantee — each stage marks a distinct point in the C# staging code (immediately after the corresponding entity is `Add()`'ed), not a distinct database round-trip. This is documented directly in the source code, not hidden. `ILongHorizonPersistenceFailureInjector` is wired via an optional constructor parameter on `LongHorizonRollingStateRepository`, defaulting to `NoOpLongHorizonPersistenceFailureInjector.Instance` — zero behavioral change to any of the dozens of existing production/test call sites that construct the repository with its original single-argument constructor.

## 6. Failure type and activation

`LongHorizonInjectedPersistenceFailureException` carries `Operation`, `Stage`, and optional `PlanId`. `LongHorizonTestPersistenceFailureInjector` throws exactly once for its configured `(Operation, Stage)` pair, then disarms — production code never catches or converts it into a business outcome; it propagates and the ambient `SaveChangesAsync` transaction (if reached) or the in-memory staged changes (if not yet reached) are simply never committed.

## 7. Production-inert safety

Proven: all failpoint types are `internal`, not public. `PlansController.cs` and `Program.cs` contain zero references to any failpoint type or injector (verified by a direct grep-based test). No environment variable or configuration flag exposes the injector.

## 8. Durable pre-state snapshot

Not built as a dedicated canonical `LongHorizonDurableStateSnapshot`/comparer contract. Each rollback test instead directly queries the relevant raw tables for row counts and key-field equality before/after the injected failure, plus reconstructs through the real repository and compares `CurrentWindow`/lifecycle state — real, PostgreSQL-verified evidence per test, narrower than a uniform structural comparer.

## 9. Reconstruction comparison

Every rollback test reconstructs via `LoadRestartSnapshotAsync` (which internally runs the existing integrity validator) after the injected failure — no validator exception occurred in any of the 14 passing tests, confirming reconstructed state remained valid throughout.

## 10. Mixed activation failpoints

`AfterVersionValidation`, `AfterContextInsert`, `AfterActivationWindowInsert`, `BeforeCommit` — 4 of the ~14 suggested stages, on the 2 Runway + 2 Core shape (horizon 26).

## 11. Mixed activation rollback results

For every tested stage: no new activation-window row, no orphan Core-context row, `CurrentWindow` unchanged, `ActiveContextVersionSequence` unchanged, no lifecycle state advanced past the pre-boundary window. Replay (injector disabled) succeeds exactly once with exactly one activation record for the new range.

## 12. Core-only failpoints

`BeforeCommit` only, on horizon 21's pure Core-only window `[10,13]`.

## 13. Core-only rollback results

Exact prior `CurrentWindow` and activation-record count after rollback; replay lands correctly on `[10,13]`.

## 14. Core-refresh failpoints

`BeforeCommit` only (reusing the `CoreOnlyActivation` operation tag, since Phase 4L.2E's refresh capability shares the identical persistence code path).

## 15. Core-refresh rollback results

After injected failure: V1 remains Active (same `CoreContextId`/`ContextVersionSequence`), zero orphan V2 rows, V1's `SupersededByContextId` remains null (never partially written). Replay creates exactly one new V2 row.

## 16. Block failpoints

`BeforeCommit` only.

## 17. Block rollback results

Block-record count unchanged after rollback; plan lifecycle status is NOT Blocked (proving no partial application). Replay creates exactly one block record.

## 18. Retry failpoints

`BeforeCommit` only.

## 19. Retry rollback results

Retry-record count unchanged; plan remains in its original Blocked state (not silently transitioned). Replay creates exactly one retry-restoration record.

## 20. Initial persistence coverage

Not separately tested. Initial structural persistence (`InitializeStructuralStateAsync`) is a distinct method already covered by the existing Phase 4L.2 test suite; no new failpoint was added to it this phase given time constraints.

## 21. Checkpoint persistence coverage

Not separately tested. Checkpoint persistence is always committed atomically inside the same `SaveChangesAsync` call as activation (confirmed by the transaction-boundary inventory), so a standalone checkpoint rollback test would duplicate coverage already implied by the mixed/Core-only rollback tests, per the phase's own "do not duplicate coverage" instruction.

## 22. Terminal completion coverage

Not separately tested; terminal completion is not a standalone transaction distinct from the final Core-only activation already covered.

## 23. Post-commit acknowledgement ambiguity

`PostCommitAcknowledgementFailure_DoesNotRollBack_AndReplayDeduplicates` proves the required distinction for one operation (first Runway entry, `InitialPersistence`-classified): a failure injected after `SaveChangesAsync` returns successfully does NOT roll back — a fresh restart sees the already-committed window, and a direct replay using the actual persisted `IdempotencyKey` returns `IdempotentReplay` with no duplicate row. Only this one operation was tested, not all five groups.

## 24. Constraint-exception rollback

**Not attempted.** No test deliberately triggers a real PostgreSQL constraint violation (duplicate ownership, duplicate session, duplicate context, invalid foreign key, duplicate idempotency key) to prove EF's transaction rolls back completely on a genuine database-level exception. This is the single most significant gap relative to the phase's own closure criteria.

## 25. Transaction leak detection

Folded into each rollback test's own row-count/field-equality assertions rather than built as a dedicated cross-cutting leak-detection query set.

## 26. Provider execution strategy

Not separately inspected this phase (Part 17). No savepoint or Npgsql retry-strategy review was performed; the existing `DbContextOptionsBuilder<AppDbContext>().UseNpgsql(...)` configuration (no explicit retry policy) was used unchanged throughout, matching every other Long-Horizon persistence test in this session.

## 27. Replay after rollback

Proven for all five operation groups: after each injected pre-commit failure and fresh-context verification, disabling the injector and replaying the exact same authoritative request succeeds exactly once, with exactly one new row for the affected table.

## 28. No-formula-change proof

Zero changes to any numeric, calendar, direction, Runway, target-lock, checkpoint-evidence, refresh-eligibility, retry, or window-selection formula. Only transaction instrumentation (checkpoint calls immediately after existing entity-staging code) was added.

## 29. Dark integration

Internal only. No endpoint, DI registration, background job, confirmation, public-preview, Home/Calendar, completion-handler, API, or Flutter file references any new failpoint type.

## 30. Governance

New TD `TD-LONG-HORIZON-TRANSACTIONAL-FAILURE-INJECTION-ROLLBACK-MATRIX-001`, status **OPEN** (real constraint-exception testing and the full stage matrix remain unproven, per the phase's own explicit closure criteria). Append-only updates to `TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001` (remains OPEN — failureInjectionCoverage partially, not fully, addressed), `TD-LONG-HORIZON-FUTURE-ONLY-CORE-CONTEXT-REFRESH-001`, `TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001`, `TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001`, `TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001`, `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001`. Aggregate: 53 risks, 16 OPEN, 37 CLOSED. Parent Phase 4L.2B TD kept OPEN.

## 31. Tests

14 new focused tests, all real PostgreSQL, 0 failing: 5 infrastructure tests, 4 mixed-activation rollback+replay cases (one per tested stage), 1 Core-only rollback+replay, 1 Core-refresh rollback+replay with V1-authority proof, 1 block rollback+replay, 1 retry rollback+replay, 1 post-commit acknowledgement/idempotent-replay proof. Full LongHorizon suite: 783/783. Full backend and plan-catalog suites re-confirmed passing (see final report for exact counts).

## 32. Public/confirmation/API/Flutter status

Unchanged. All remain entirely unwired.

## 33. Final classification

```
LONG_HORIZON_TRANSACTIONAL_ROLLBACK_MATRIX_REMAINS_BLOCKED_BY_EXPLICIT_TRANSACTION_FAILPOINT_SNAPSHOT_RECONSTRUCTION_CONSTRAINT_OR_REPLAY_GAP
```

## 34. Exact next phase

A further-focused rollback phase targeting specifically: (1) real PostgreSQL constraint-exception rollback tests (duplicate ownership/session/context, foreign-key, duplicate idempotency key); (2) the remaining stage coverage per operation (AfterWeekUpdates, AfterSessionInserts as independent test cases, not just wired checkpoints); (3) the 1+3/3+1 mixed shapes in addition to 2+2. Per the phase's own explicit instruction, do not proceed to Phase 4L.2G (Remaining Concurrency, Idempotency and Commit-Ambiguity Matrix) until mixed/Core/refresh rollback is more completely proven. Not Phase 4L.3.
