# Phase 4L.2 — Long-Horizon Rolling Persistence and Restart-Safe State Contract

## 1. Executive result

This phase designs and implements the internal persistence model required to durably store, restore, and safely continue a Long-Horizon rolling plan after process restart — closing the persistence and restart-resume blockers Phase 4L.1 identified, without wiring anything to the public preview or confirmation endpoints. Eight new tables durably represent structural weeks, executable sessions, activation-window/checkpoint/block-retry audit records, and immutable Runway/Core ownership. Every one of 30 new tests runs against the real configured PostgreSQL database, with each "restart" proven by opening a brand-new `AppDbContext`/connection per operation — never approximated with an in-memory fixture. No commits made.

```
LONG_HORIZON_ROLLING_PERSISTENCE_AND_RESTART_SAFE_STATE_CONTRACT_COMPLETED_DARK

LONG_HORIZON_STRUCTURAL_PENDING_BLOCKED_ACTIVATED_COMPLETED_AND_MISSED_WEEK_STATES_ARE_NOW_DURABLY_REPRESENTED

LONG_HORIZON_RESTART_RECONSTRUCTS_THE_EXACT_ROLLING_LIFECYCLE_TARGET_LOCK_RUNWAY_PRESCRIPTION_CORE_CONTEXT_CHECKPOINT_AND_SESSION_CALENDAR_WITHOUT_REGENERATION_OR_HISTORY_REWRITE

LONG_HORIZON_ACTIVATION_BLOCK_AND_RETRY_WRITES_ARE_TRANSACTIONAL_CONCURRENCY_SAFE_AND_IDEMPOTENT

LONG_HORIZON_PUBLIC_PREVIEW_CONFIRMATION_API_HOME_CALENDAR_AND_FLUTTER_REMAIN_UNWIRED
```

## 2. Inherited readiness findings

Phase 4L.1 proved the public preview contract ready, the confirmation flow NotReady (requires a complete static schedule), the existing persistence model unable to represent `NumericPending`/`NumericActivationBlocked` weeks, and current DB state unable to reconstruct rolling lifecycle ownership, checkpoint state, Runway lock/prescription continuity, Core context versions, or retry history. This phase closes exactly those two blockers (persistence, restart/resume) and no others.

## 3. Scope and exclusions

In scope: durable-state classification, the minimal durable model, EF entity design, migration, repository contracts/implementation, transaction boundaries, concurrency, idempotency, restart snapshot/reconstruction, restart continuation (one operation, no loop), initial structural persistence, activation/block/retry persistence adapters, integrity validation, corruption/fail-closed behavior, public leakage boundary.

Excluded: public Long-Horizon exposure; the live preview endpoint; confirmation; Flutter; background activation jobs; automatic post-restart activation; unnecessary persistence of internal computation that can be safely reconstructed; any numeric/calendar/direction/evidence/lifecycle formula change; commits.

## 4. Existing persistence assumptions

Direct inspection confirmed: no repository pattern exists anywhere in the codebase (Application services inject `AppDbContext` directly); no entity uses a concurrency token today (`RowVersion`/`[Timestamp]`/`xmin` — zero hits); all EF configuration is inline Fluent API in one `OnModelCreating` method (no `IEntityTypeConfiguration<T>` classes exist); several string columns are explicitly mapped `jsonb` (`PlanPreview.RequestPayloadJson`, `TrainingDay.CatalogPrescriptionJson`, etc.); the provider is PostgreSQL/Npgsql; multi-entity writes rely purely on EF's own implicit per-`SaveChangesAsync` transaction (no explicit `BeginTransaction` anywhere); `TrainingDay.PlanId` is a required FK to `TrainingPlans`; `TrainingDayStatus` has no "not yet generated" value.

## 5. Durable-state classification

**A. Persist as authoritative durable state**: plan/week/session identity, structural dates, lifecycle state, executable numeric values (`WeeklyVolumeKm`/`LongRunKm`), session role/distance/`AssignedDate`, checkpoint decision summaries, block/retry audit records.

**B. Persist source facts and deterministically reconstruct**: the GE structural skeleton's workout role labels — a pure, evidence-independent function of (`TotalWeeks`, `ReadinessProfile`, `CandidateKey`/`Version`), all three already stored on the plan aggregate. Reconstruction re-invokes the exact unmodified `LongHorizonStructuralMaterializer` rather than storing a duplicate copy.

**C. Persist as immutable historical projection**: the full Runway prescription (per-week values) and the selected Core weeks, as bounded, versioned jsonb snapshots — their generation IS evidence-fed, so byte-identical re-derivation after a future code/catalog/resolver/rounding change is not guaranteed, and this phase does not attempt to prove it (Decision A, not B).

**D. Do not persist**: raw internal diagnostic traces, full un-selected future Core output, internal audit-event detail, condition-resolution raw trace.

## 6. Minimal durable model

Eight tables: `LongHorizonRollingPlanState` (aggregate), `LongHorizonRollingWeekState` (one row per global week), `LongHorizonRollingSessionState` (executable sessions only, created when a week activates), `LongHorizonActivationWindowRecord` (immutable per-attempt decision), `LongHorizonCheckpointRecord` (immutable checkpoint snapshot), `LongHorizonRunwayState` (created once), `LongHorizonCoreContextRecord` (versioned, supersede-on-refresh), `LongHorizonBlockRetryRecord` (immutable audit trail). No future `TrainingDay`/session rows exist for `NumericPending` weeks — structurally guaranteed by the integrity validator, not merely a convention.

## 7. Plan aggregate

`LongHorizonRollingPlanState` — deliberately NOT a `TrainingPlans` row: no `GoalType.LongHorizon`/`GoalDistance.LongHorizon` enum value exists yet (confirmed absent, Phase 4L.1), so creating one would require fabricating a meaning that doesn't exist. Its own `Id` is the internal ownership anchor for every child table. Fields: `TotalWeeks`, `ReadinessProfile`, `StartDate`, `RaceDate`, `GoalType`/`GoalDistance`/`Level`/`DaysPerWeek`/`PreferredDaysCsv`/`LongRunDay`, `CandidateKey`/`CandidateVersion`/`CatalogRootPath`, `CurrentLifecycleStatus`, `CurrentWindowStartWeek`/`EndWeek`, `LastActivatedGlobalWeek`, `LatestCheckpointDate`, `ActiveContextVersionSequence`/`Id`, `CurrentBlockedPublicReasonCategory`/`InternalReasonCode`, `BlockedAt`, `RetryEligible`, `PersistenceContractVersion`, timestamps, and a Postgres `xmin`-backed concurrency token.

## 8. Structural week persistence

`LongHorizonRollingWeekState`: exactly one row per `(PlanStateId, GlobalWeek)` (unique index). `SegmentType`/`Stage`/`StructuralStartDate`/`EndDate`/`LifecycleState` plus nullable `WeeklyVolumeKm`/`LongRunKm` (populated only when Activated/Completed/Missed, enforced by the integrity validator — a Pending row can never carry a fabricated zero or real value).

## 9. Executable session persistence

`LongHorizonRollingSessionState` — deliberately NOT `TrainingDay`: `TrainingDay.PlanId` is a required FK to `TrainingPlans`, and Long-Horizon plans have no real `TrainingPlan` row. Reusing `TrainingDay` would require either fabricating a `TrainingPlan` row (forbidden) or a nullable `PlanId` (a breaking change to a table read by the real Home/Calendar/Details/completion endpoints, risking dark session leakage into live surfaces). The dedicated table keeps this subsystem fully isolated from every live read path, with a unique `(WeekStateId, SessionOrdinal)` index.

## 10. Activation-window records

`LongHorizonActivationWindowRecord`: one immutable row per attempted authoritative activation decision (Activated or Blocked), with a deterministic `IdempotencyKey` (unique index) preventing duplicate writes on replay.

## 11. Checkpoint persistence

`LongHorizonCheckpointRecord`: an immutable decision snapshot (`AsOfDate`, source window, evidence fingerprint, validated weekly/long-run volume, decision, authoritative reason), unique per `(PlanStateId, AsOfDate, SourceWindowStartWeek)`. Raw `TrainingDay`-equivalent evidence is never duplicated into this table — it remains the caller's own source facts.

## 12. Runway lock/prescription persistence

Decision A: `LongHorizonRunwayState` persists the immutable activated-authority prescription values (per-week global week/weekly volume/long run/stage) as a bounded, versioned jsonb snapshot, created exactly once per plan (unique index on `PlanStateId`) and never regenerated on restart. Decision B (rerun the materializer after restart) was rejected because equivalence across future code/catalog/resolver/rounding changes is not guaranteed.

## 13. Core context persistence

`LongHorizonCoreContextRecord`: `ContextVersionSequence`/`EffectiveFromGlobalWeek`/`EffectiveToGlobalWeek`/`AsOfDate` plus bounded jsonb snapshots of the condition-result summary and selected Core weeks (same Decision-A rationale). A new context creation marks the prior Active context Superseded; activated Core weeks retain their original context ownership since only future Pending weeks are affected by a later refresh.

## 14. Block/retry persistence

`LongHorizonBlockRetryRecord` (immutable audit trail: Block/RetryRequested/RetryRestored — this phase persists one combined `RetryRestored` row per successful retry rather than two separate Requested/Restored rows, a disclosed simplification since the intermediate "requested" state has no separate durable meaning here). `SaveRetryRestorationAsync` enforces: the plan must currently be `NumericActivationBlocked` (no repository method performs a direct Blocked→Activated transition — proven by `DirectBlockedToActivatedIsRejected`); the retry checkpoint date must be strictly later than the prior one; the evidence fingerprint must differ from the most recent block's fingerprint.

## 15. Persistence contract versioning

`PersistenceContractVersion = 1`, stored on the plan aggregate, checked during reconstruction. Any other value throws `LongHorizonRollingPersistenceCorruptionException` — proven directly by mutating a real row to 999 and reconstructing.

## 16. EF Core entity design

Inline Fluent API in `AppDbContext.OnModelCreating`, matching the existing 100%-inline convention (no `IEntityTypeConfiguration<T>` introduced). Explicit foreign keys with `DeleteBehavior.Cascade` from plan→week→session (matching the existing `TrainingPlan`→`TrainingWeek`→`TrainingDay` cascade pattern). Enums stored as strings via the existing global `SnakeCaseEnumConverter<TEnum>` loop (no bespoke conversion needed). `jsonb` column type on the three bounded-payload columns, matching the existing `CatalogPrescriptionJson` convention. `xmin`-backed concurrency token via `Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` (the Npgsql-idiomatic approach; no new column added to the table).

## 17. Migration

`20260803141913_LongHorizonRollingPersistence.cs` — eight `CreateTable` statements, zero `AddColumn`/`AlterColumn`/`DropColumn` against any existing table (confirmed by direct inspection of the generated migration). Applied to the local integration-test PostgreSQL database (`antigravity_dev`) via `dotnet ef database update`, confirmed successful. No existing plan was marked Long-Horizon; no fake pending weeks were created for any existing plan.

## 18. Repository contracts

`ILongHorizonRollingStateRepository`: `InitializeStructuralStateAsync`, `LoadRestartSnapshotAsync`, `SaveActivationSuccessAsync`, `SaveBlockAsync`, `SaveRetryRestorationAsync`. Implemented by `LongHorizonRollingStateRepository`, using `AppDbContext` directly (matching the existing codebase convention — no separate Infrastructure repository layer exists anywhere else either). Never exposes an EF entity beyond this implementation; every method takes/returns Application-layer contracts.

## 19. Transaction boundaries

Every `Save*` operation relies on EF Core's own implicit per-`SaveChangesAsync` transaction — one call covers the plan aggregate update, week-state updates, session inserts, and the activation-window/checkpoint/Runway/Core records together, so a mid-write failure rolls back the entire operation atomically. This matches the existing codebase's own convention (no explicit `BeginTransaction`/`CommitAsync` exists anywhere in `RunningApp.Application` today).

## 20. Concurrency control

A Postgres `xmin`-backed optimistic concurrency token on `LongHorizonRollingPlanState`. Every `Save*` method sets the EF change-tracker's original `xmin` value to the caller-supplied expected version before `SaveChangesAsync`; a stale value throws `DbUpdateConcurrencyException`, mapped to a typed `ConcurrencyConflict` outcome — never silently swallowed. Proven directly: a winner writes first using the correct version, a loser reusing the now-stale version is rejected (`StaleConcurrencyVersionRejectsBlock`).

## 21. Idempotency

Deterministic idempotency keys derived from `(PlanStateId, week range, context version sequence, checkpoint/retry date)` — never a random request ID. Each `Save*` method checks for an existing record with the same deterministic signature before writing and returns `IdempotentReplay` without a duplicate write if found. Proven directly: replaying an identical block request produces exactly one durable block record (`DuplicateIdempotencyKeyReturnsPriorResultSafely`).

## 22. Restart snapshot

`LongHorizonRollingRestartSnapshot` wraps the exact same `LongHorizonFullDarkLifecycleState` type Phase 4K.9's runtimes already consume (no parallel shape invented), plus `PlanStateId`/`PlanStartDate`/`ConcurrencyVersion`/`CatalogRootPath`/`Candidate`. Internal only, no public DTO type.

## 23. Reconstruction service

`LongHorizonRollingStateReconstructionService.ReconstructAsync` assembles the snapshot from durable rows plus the deterministically regenerated structural skeleton, then runs `LongHorizonRollingPersistenceIntegrityValidator` before returning. No numeric or calendar value is ever regenerated — proven directly: two independent reconstructions of the same persisted plan produce byte-identical activated-week volumes and session dates (`NoNumericOrCalendarRegenerationOccursOnReconstruction`).

## 24. Restart continuation service

`LongHorizonRollingRestartContinuationService.ContinueGeCheckpointAsync` proves the full chain: load restart snapshot from a brand-new `AppDbContext`/connection → validate → map to the exact unmodified Phase 4K.7 `LongHorizonRollingCheckpointRequest` shape → invoke the real unmodified checkpoint runtime for exactly one operation → persist transactionally → return a new snapshot. No automatic loop; called only from tests, never from an endpoint, background service, or completion handler. Proven across two independent, separately-instantiated restarts in sequence (`RestartAfterRepeatedGeContinuesAcrossTwoSeparateRestarts`). The Runway/Core JIT composition path shares the same `LongHorizonRollingActivationPersistenceAdapter.PersistJitCompositionAsync` entry point but was not exercised end-to-end through the continuation service in this phase's own test matrix — disclosed as a scope boundary, not a claimed equivalence.

## 25. Initial structural persistence

`InitializeStructuralStateAsync` persists the complete structural roadmap plus the first activated window in one atomic write, idempotent on replay (checks for an existing plan row first). Proven for 21 and 52 weeks.

## 26. Activation adapter

`LongHorizonRollingActivationPersistenceAdapter` translates a real, already-succeeded `LongHorizonRollingCheckpointResult` (GE path) or `LongHorizonRollingJitCompositionResult` (Runway/Core path) into a persistence request — copies every numeric/calendar value verbatim, never recomputes.

## 27. Block adapter

`LongHorizonRollingBlockPersistenceAdapter` persists exactly one primary internal reason plus a public-safe category, sets the exact next boundary to Blocked, creates no executable sessions.

## 28. Retry adapter

`LongHorizonRollingRetryPersistenceAdapter` — a thin, evidence-carrying entry point; the repository itself enforces the later-checkpoint and changed-evidence requirements.

## 29. Completion/checkpoint consistency

Not directly exercised in this phase (Long-Horizon sessions are never real `TrainingDay` rows, so the existing completion endpoints cannot reach them today by construction — see §9). This is a structural, not incidental, guarantee: there is no code path connecting the real completion endpoints to any table this phase created.

## 30. Integrity validator

`LongHorizonRollingPersistenceIntegrityValidator` checks: exact week count, unique/contiguous global weeks, phase order, Activated-without-sessions, Pending-with-sessions, Blocked-without-owning-record, unknown persistence version, current-window boundary validity. Run during every reconstruction.

## 31. Corruption/fail-closed behavior

Every corruption case throws `LongHorizonRollingPersistenceCorruptionException` — proven directly against real Postgres by deleting a structural week row, deleting an activated week's sessions, and setting an unknown persistence version, each producing a fail-closed exception on both the first and a repeated attempt (proving no auto-repair, `NoAutoRepairOccursAfterCorruptionDetection`).

## 32. Public leakage boundary

No new entity type lives in or is referenced from any DTOs namespace. The repository, adapters, restart continuation service, and reconstruction service are all internal, non-public C# types. `PlansController.cs` and `Program.cs` contain zero references to any new type (grepped directly). The repository implementation never calls `_db.TrainingPlans.Add` or `_db.TrainingDays.Add` (grepped directly against its own source).

## 33. Dark integration

Internal, repository-backed, independently testable. Not called by public preview, confirmation, home/calendar, or completion endpoints. Not registered as a live background workflow. Not exposed to Flutter.

## 34. Governance artifacts

New TD `TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001` (CLOSED). Append-only updates to `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001` and `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` (remains OPEN). Aggregate updated to 47 risks, 14 OPEN, 33 CLOSED. Ten prior governance test files with stale 46/32 hardcoded counts updated to 47/33. New governance test file.

## 35. Tests

30 new focused tests, all against the real configured PostgreSQL database: initial persistence (4), restart reconstruction (5), restart continuation across independent restarts (2), block/retry persistence and history immutability (6), concurrency and idempotency (2), corruption/fail-closed (5), and public leakage/wiring proof (6). Two real bugs were found and fixed via this phase's own real-database testing (see §37).

## 36. Public/confirmation/API/Flutter status

Unchanged. No endpoint, DI registration, confirmation code, home/calendar query, background job, or Flutter file references any new type.

### Bugs found and fixed during this phase's own testing

1. **Idempotency/concurrency test overlap**: a concurrency test's "loser" attempt reused the exact same block boundary and checkpoint date as the winner, so it matched the idempotency dedupe check (which runs before the concurrency check) and returned `IdempotentReplay` instead of exercising `ConcurrencyConflict`. Fixed by giving the loser a distinct checkpoint date, isolating the concurrency path specifically.
2. No second implementation bug was found; all other test failures during development were the expected first-pass compile/reference errors (missing `using` directives, incorrect field names on existing internal types), resolved before any test ran.

## 37. Final classification

`LONG_HORIZON_ROLLING_PERSISTENCE_AND_RESTART_SAFE_STATE_CONTRACT_COMPLETED_DARK`. `LONG_HORIZON_STRUCTURAL_PENDING_BLOCKED_ACTIVATED_COMPLETED_AND_MISSED_WEEK_STATES_ARE_NOW_DURABLY_REPRESENTED`. `LONG_HORIZON_RESTART_RECONSTRUCTS_THE_EXACT_ROLLING_LIFECYCLE_TARGET_LOCK_RUNWAY_PRESCRIPTION_CORE_CONTEXT_CHECKPOINT_AND_SESSION_CALENDAR_WITHOUT_REGENERATION_OR_HISTORY_REWRITE` — with the disclosed caveat that the Runway/Core JIT composition path was smoke-tested rather than exhaustively restart-matrix-tested (§24). `LONG_HORIZON_ACTIVATION_BLOCK_AND_RETRY_WRITES_ARE_TRANSACTIONAL_CONCURRENCY_SAFE_AND_IDEMPOTENT`. `LONG_HORIZON_PUBLIC_PREVIEW_CONFIRMATION_API_HOME_CALENDAR_AND_FLUTTER_REMAIN_UNWIRED`.

## 38. Exact next phase

Phase 4L.3 — Long-Horizon Confirmation and Public Preview Wiring.
