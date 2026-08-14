# Phase 4L.2F-A — PostgreSQL Constraint-Exception and Rollback Completion

## 1. Executive result

`LONG_HORIZON_POSTGRESQL_CONSTRAINT_EXCEPTION_AND_ROLLBACK_COMPLETION_PROVED`.

Real configured PostgreSQL exceptions now prove that the complete EF unit of work rolls back when a later tracked mutation violates an existing database constraint. No earlier context, activation, week, session, block, retry or aggregate mutation survives. A fresh connection reconstructs the exact prior state, and a corrected explicit replay commits once.

## 2. Residual gap inherited from Phase 4L.2F

Phase 4L.2F proved application-injected pre-commit rollback and post-commit acknowledgement recovery but intentionally left real provider constraint exceptions untested. This phase closes that evidence gap without claiming the remaining Phase 4L.2G concurrency/idempotency matrix.

## 3. Scope and exclusions

Scope is the existing dark Long-Horizon persistence repository and real PostgreSQL. No public preview, confirmation, API, Home/Calendar, completion handler, Flutter, background job, transaction-boundary redesign, numeric/calendar formula, retry policy or activation policy changed. Phase 4L.2G and Phase 4L.3 were not implemented.

## 4. Constraint inventory

| Table | Database authority | Name | Expected SQLSTATE | Natural operation / reachability |
|---|---|---|---|---|
| `LongHorizonRollingPlanStates` | PK `Id`; required scalar columns; system `xmin` concurrency token | PK + `xmin` | 23505 / concurrency | Aggregate identity duplication is race/corruption only; stale `xmin` is naturally reachable under concurrency. |
| `LongHorizonRollingWeekStates` | FK `PlanStateId`; unique `(PlanStateId,GlobalWeek)` | `IX_LongHorizonRollingWeekStates_PlanStateId_GlobalWeek` | 23503 / 23505 | Structural initialization race/corruption. |
| `LongHorizonRollingSessionStates` | FK `WeekStateId`; unique `(WeekStateId,SessionOrdinal)`; required role/provenance/date | `IX_LongHorizonRollingSessionStates_WeekStateId_SessionOrdinal` | 23503 / 23505 / 23502 | Activation race/corruption; duplicate-session test uses a test-only staged entity. |
| `LongHorizonActivationWindowRecords` | FK `PlanStateId`; unique `IdempotencyKey`; required ownership/range/context fields | `IX_LongHorizonActivationWindowRecords_IdempotencyKey` | 23503 / 23505 / 23502 | Idempotency key is the database idempotency authority. Normal replay is pre-deduplicated; a race/corruption conflict is staged after validation. |
| `LongHorizonCheckpointRecords` | FK `PlanStateId`; unique `(PlanStateId,AsOfDate,SourceWindowStartWeek)` | `IX_LongHorizonCheckpointRecords_PlanStateId_AsOfDate_Window` | 23503 / 23505 | Checkpoint collision under race/corruption; committed atomically with activation. |
| `LongHorizonRunwayStates` | FK and unique `PlanStateId`; required JSONB snapshots | `IX_LongHorizonRunwayStates_PlanStateId` | 23503 / 23505 / 23502 | Duplicate immutable ownership is race/corruption only. |
| `LongHorizonCoreContextRecords` | FK `PlanStateId`; unique `(PlanStateId,ContextVersionSequence)`; required JSONB/authority fields | PostgreSQL physical `IX_LongHorizonCoreContextRecords_PlanStateId_ContextVersionSequ` | 23503 / 23505 / 23502 | Future refresh race/corruption; duplicate-context test stages the conflict after valid V2/supersession mutations. |
| `LongHorizonBlockRetryRecords` | FK `PlanStateId`; required event/reason/evidence fields; non-unique audit index | `FK_LongHorizonBlockRetryRecords_LongHorizonRollingPlanStates_P~` | 23503 / 23502 | Block/retry FK tests stage an invalid audit child after valid operation mutations. |

Every ownership FK uses cascade delete from its parent; sessions cascade from weeks. There is no separate idempotency table and no Long-Horizon check constraint. Application integrity validation remains authoritative for range/lifecycle consistency not represented by a database constraint.

## 5. Real provider exception standard

All focused tests use `AppDbContext` configured with `UseNpgsql` against `antigravity_dev`, call the real repository `SaveChangesAsync`, assert the inner `PostgresException` SQLSTATE and constraint/column metadata, dispose the failed context, and inspect/reconstruct through a fresh context. No exception is manually thrown, and no Npgsql/SaveChanges mock, EF InMemory, SQLite or pre-failure raw SQL is used.

## 6. Test-only constraint mutation seam

`ILongHorizonPersistenceConstraintMutation.Stage` receives the already-tracking `AppDbContext` immediately before the repository's existing `SaveChangesAsync`. Tests add or alter one scenario-specific tracked entity; the database itself produces the failure. `NoOpLongHorizonPersistenceConstraintMutation` is the constructor default. The seam is internal, has no DI/configuration/API registration and performs no second save.

## 7. Durable snapshot authority

The focused tests capture a canonical ordered JSON fingerprint of aggregate authority fields, week rows, executable sessions, activation windows, Core contexts and supersession links, and block/retry records, plus scalar row counts and `xmin`. Comparison is structural/canonical; list-backed domain record equality is not used. Generated timestamps live inside the failed transaction and therefore disappear with it; no timestamp exclusion is necessary.

## 8. Duplicate activation ownership

A mixed Runway-to-Core activation tracks lifecycle, session, Core-context and aggregate changes, then the seam adds a second activation row with the same real `IdempotencyKey`. PostgreSQL returns 23505 for `IX_LongHorizonActivationWindowRecords_IdempotencyKey`. No new row or pointer survives; raw state and `xmin` are unchanged; corrected replay succeeds once.

## 9. Duplicate executable session

A Core-only activation tracks earlier week/aggregate/session changes, then a duplicate `(WeekStateId,SessionOrdinal)` row is staged. PostgreSQL returns 23505 for `IX_LongHorizonRollingSessionStates_WeekStateId_SessionOrdinal`. No partial Core window or orphan/duplicate session survives; corrected replay succeeds once.

## 10. Duplicate Core context

A future-only refresh tracks V2, V1 supersession, future ownership and activation mutations, then a duplicate `(PlanStateId,ContextVersionSequence)` is staged. PostgreSQL returns 23505 for physical constraint `IX_LongHorizonCoreContextRecords_PlanStateId_ContextVersionSequ`. V1 remains Active with null `SupersededByContextId`, no V2 survives, and corrected replay creates one V2. The existing plan-scoped identity regression continues to permit two plans with the same context-local identity.

## 11. Idempotency constraint status

Activation `IdempotencyKey` is unique at the database level and is the persisted idempotency ownership mechanism. No separate idempotency record/table exists for block or retry; those paths use application lookup semantics. The activation test deliberately stages the conflict after normal replay lookup so provider rollback, not application deduplication, is exercised.

## 12. Foreign-key violation

Both `SaveBlockAsync` and `SaveRetryRestorationAsync` track valid lifecycle/aggregate/audit changes, then stage a test-only `LongHorizonBlockRetryRecord` referencing a nonexistent plan. PostgreSQL returns 23503 for `FK_LongHorizonBlockRetryRecords_LongHorizonRollingPlanStates_P~`. No valid earlier mutation or orphan survives; corrected explicit replay succeeds once.

## 13. Not-null violation availability

An activation test nulls the newly tracked required `LongHorizonActivationWindowRecords.IdempotencyKey` immediately before save. PostgreSQL, rather than EF validation, returns 23502 and identifies column `IdempotencyKey`. The complete operation rolls back and corrected replay succeeds.

## 14. Check-constraint availability

The design-time EF model and Long-Horizon migration contain no Long-Horizon check constraint. No 23514 test, constraint or migration was fabricated. Range ordering, positivity and lifecycle consistency remain enforced by existing application validators.

## 15. Mixed activation rollback

23505 duplicate activation ownership proves rollback of context ownership, week lifecycle, sessions, activation row and aggregate pointers in the real mixed operation. The fresh raw and reconstructed snapshots equal the pre-state.

## 16. Core-only rollback

23505 duplicate session and 23502 required-value failures independently prove that earlier Core-only lifecycle/session/aggregate mutations do not survive. Each failed context is discarded; corrected replay commits the first successful operation once.

## 17. Core-refresh rollback

23505 duplicate plan-scoped Core context proves V1 and its supersession link remain exact, no V2/future ownership survives, the active-context pointer stays V1 and `xmin` is unchanged. Replay creates exactly one V2.

## 18. Block rollback

23503 during `SaveBlockAsync` leaves no block record, no Blocked lifecycle transition and no aggregate block fields. Replay creates one block.

## 19. Retry rollback

23503 during `SaveRetryRestorationAsync` leaves the original Blocked state and block row intact, creates no retry row and performs no Pending transition. Replay creates one retry restoration; activation remains a separate operation.

## 20. xmin concurrency distinction

Existing real PostgreSQL stale-writer tests continue to produce `DbUpdateConcurrencyException`/typed `ConcurrencyConflict`, not `PostgresException` 23505. The loser writes nothing, discards its context and reloads the winner through a fresh context. This representative distinction does not claim Phase 4L.2G's full race matrix.

## 21. Failed DbContext lifecycle

Every provider-failure test scopes the failing `AppDbContext` in its own `await using` block. All raw inspection, reconstruction and replay occur only after disposal through newly constructed contexts/connections. Production guidance is: authoritative `DbUpdateException` fails the operation, ends the request scope, and requires fresh reload before explicit retry; no global retry middleware was added.

## 22. Provider execution strategy

Production and these direct integration contexts call `UseNpgsql` without `EnableRetryOnFailure`, so the configured strategy is non-retrying. Permanent 23505/23503/23502 failures are surfaced once. The mutation seam fires once per repository call, and corrected replay is visibly a new call rather than an execution-strategy retry. Deterministic operation identities remain unchanged.

## 23. Raw SQL post-failure inspection

Independent fresh-context relational queries read `xmin`, aggregate fields, ordered ownership rows and counts without modifying or repairing data. EF generates the read-only SQL; no repository filtering is used to hide orphan rows and no raw SQL repairs state.

## 24. Reconstruction and integrity validation

After raw equality, each test loads through `LongHorizonRollingStateRepository`, which delegates to `LongHorizonRollingStateReconstructionService`, and explicitly invokes `LongHorizonRollingPersistenceIntegrityValidator.ValidateReconstructedState`. Current window/first Pending boundary, active context, target lock/prescription, historical sessions and aggregate version remain unchanged. Reconstruction does not invoke a resolver, Core generator, Runway materializer or auto-repair path.

## 25. Corrected replay

The seam is absent from a new context and the exact semantic operation is called again. Since the failed save committed nothing, this is the first commit: one aggregate-version advancement, no duplicate activation/context/session, no stale failed row, and the next boundary advances correctly.

## 26. Mutation-category coverage

Ownership/context tracked, lifecycle changes tracked, sessions tracked, aggregate pointers tracked, immediately-before-save, real provider failure during save, and post-commit-before-acknowledgement are all covered across Phase 4L.2F plus this phase. Exhaustive per-line failpoints are not required because each pre-commit category converges on one EF transaction at the same single save; complete distinct mutation-category coverage is required and now met.

## 27. Schema-change decision

No migration was added. Existing unique, FK and required-value constraints provide honest provider evidence. Absence of a Long-Horizon check constraint is documented rather than treated as a test convenience or automatically declared a production defect.

## 28. Production-inert safety

The NoOp mutator remains the default, all seam types are internal, tests are the only callers, and architecture checks confirm no `Program.cs` or `PlansController` reference. No invalid mutation is runtime-input reachable and no provider detail is exposed publicly.

## 29. No-formula-change proof

Structural horizon, GE/Runway/Core progression, refresh eligibility, target lock, direction, calendar, checkpoints, retry semantics, greedy selection and downward-interpolation status are unchanged. Only the test seam, focused tests and governance/documentation changed.

## 30. Dark integration

The feature remains internal and PostgreSQL-backed. It is not wired to preview, confirmation, Home/Calendar, completion endpoints, public API, Flutter or background scheduling. No public DTO changed.

## 31. Governance

`TD-LONG-HORIZON-POSTGRESQL-CONSTRAINT-EXCEPTION-ROLLBACK-001` is added `CLOSED`. `TD-LONG-HORIZON-TRANSACTIONAL-FAILURE-INJECTION-ROLLBACK-MATRIX-001` closes at complete mutation-category coverage. Phase 4L.2B's parent completion TD remains `OPEN` until Phase 4L.2G. Confirmation/public-wiring TDs remain unchanged.

## 32. Tests

Validation completed with zero failures/skips:

- focused Phase 4L.2F-A PostgreSQL constraint suite: 8 passed;
- combined Phase 4L.2F-A / 4L.2F / 4L.2E / xmin regression: 29 passed;
- full Long-Horizon backend filter: 791 passed;
- full backend solution: 3007 passed;
- focused governance/parity selection: 17 passed;
- full plan-catalog solution: 1245 passed.

The first full plan-catalog attempt correctly failed 18 stale documentation-integrity assertions that hard-coded the prior `53 / 16 OPEN / 37 CLOSED` aggregate and Phase 4L.2F `OPEN` status. Only those documentation-integrity assertions were updated; the final full rerun passed 1245/1245.

## 33. Public/confirmation/API/Flutter status

Public preview: unwired. Confirmation: unwired. API: unchanged/unwired. Home/Calendar: unchanged/unwired. Completion handlers: unchanged/unwired. Flutter: unchanged. Background jobs: none added.

## 34. Final classification

- `LONG_HORIZON_REAL_POSTGRESQL_UNIQUE_FOREIGN_KEY_AND_AVAILABLE_REQUIRED_VALUE_OR_CHECK_CONSTRAINT_FAILURES_ROLL_BACK_THE_COMPLETE_SAVECHANGES_UNIT_OF_WORK`
- `LONG_HORIZON_NO_EARLIER_TRACKED_CONTEXT_ACTIVATION_WEEK_SESSION_BLOCK_RETRY_OR_AGGREGATE_MUTATION_SURVIVES_A_PROVIDER_CONSTRAINT_EXCEPTION`
- `LONG_HORIZON_FRESH_RECONSTRUCTION_AFTER_PROVIDER_FAILURE_EQUALS_THE_EXACT_PRIOR_STATE_AND_CORRECTED_REPLAY_COMMITS_EXACTLY_ONCE`
- `LONG_HORIZON_TRANSACTIONAL_FAILURE_INJECTION_AND_ROLLBACK_MATRIX_IS_NOW_CLOSED_AT_COMPLETE_MUTATION_CATEGORY_COVERAGE`
- `LONG_HORIZON_PUBLIC_PREVIEW_CONFIRMATION_API_HOME_CALENDAR_COMPLETION_AND_FLUTTER_REMAIN_UNWIRED`

## 35. Exact next phase

Recommended next phase: **Phase 4L.2G — Remaining Concurrency, Idempotency and Commit-Ambiguity Matrix**. This phase does not begin it.
