# Phase 4M.2 — Adaptation Persistence, Session Replacement, Audit Provenance, and Concurrency/Idempotency

Date: 2026-08-10
Branch: `main`
Canonical source: `appsel-adaptation-v1-canonical-spec (1).md — Revision 3.1.md` (including §4.1 Runtime Reason Vocabulary Mapping)

## 1. Canonical source

`appsel-adaptation-v1-canonical-spec (1).md — Revision 3.1.md`. Diffed directly against the Phase 4M.1 revision to identify exactly what changed: a new §4.1 "Runtime Reason Vocabulary Mapping," documenting that the live rolling `NotToday` endpoint has no `pain_or_discomfort` equivalent and deciding `soreness → Safety` via a future `RuntimeNotTodayReasonMapper` boundary. That mapper's wiring is explicitly Phase 4M.3 scope, not this phase's — see §29.

## 2. Phase 4M.1 baseline/audit

Re-inspected directly (not assumed from the prior report): `ReasonClassificationPolicy.cs`, `CandidateSelectionPolicy.cs`, `ScheduleRepairPolicy.cs`, `WindowExecutionSummaryBuilder.cs`, `NextWindowLoadDecisionPolicy.cs`, `AdaptationDomainContracts.cs`, and `PlanAdaptationV1DecisionTests.cs` (68 tests) all exist exactly as reported and were unmodified by this phase. Re-ran them fresh (§24) rather than trusting the prior report's counts, per this phase's own explicit instruction.

## 3. Files inspected

- `backend/RunningApp.Domain/Entities/LongHorizonRollingSessionState.cs`, `LongHorizonRollingWeekState.cs`, `LongHorizonRollingPlanState.cs`, `LongHorizonActivationWindowRecord.cs` — the real persisted rolling-session shape and the established idempotency-key entity precedent (`IdempotencyKey` + unique index).
- `backend/RunningApp.Domain/Enums/LongHorizonPersistenceEnums.cs` — `LongHorizonPersistedSegmentType`, `LongHorizonPersistedCoreContextStatus` (confirms Active/Superseded is an established naming pattern in this codebase already, for a different entity).
- `backend/RunningApp.Persistence/AppDbContext.cs` — full `OnModelCreating` for every Long-Horizon entity: the `xmin`-based optimistic concurrency token on `LongHorizonRollingPlanState`, the `OutcomeVersion` concurrency token on `LongHorizonRollingSessionState`, and every existing unique index/FK/cascade pattern.
- `backend/RunningApp.Application/Services/LongHorizonRollingWindowActivationService.cs` — the established `BeginTransactionAsync` + `SELECT ... FOR UPDATE` row-lock + typed-outcome-switch + commit/rollback pattern, directly reused for this phase's own transaction boundary.
- `backend/RunningApp.Application/Services/LongHorizonRollingSessionMutationService.cs` — the established `DbUpdateConcurrencyException` catch + reload + idempotent-replay-or-typed-conflict pattern for the sibling `Complete`/`NotToday` mutations.
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/Persistence/LongHorizonRollingPersistenceContracts.cs` — the existing `LongHorizonRollingPersistenceOutcome { Success, ConcurrencyConflict, IdempotentReplay, IntegrityViolation }` enum, whose shape this phase's own `AdaptationPersistenceOutcome` extends with two new values (`StaleTrigger`, `StaleTarget`) this phase's scope specifically requires.
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonSessionRoleCodec.cs` — reused directly for `IsEasySupport` role validation during substitution-target revalidation.
- `backend/RunningApp.Application/Services/LongHorizonActiveReadModelProvider.cs` — `EnsureExecutable`, the closest existing precedent for a persisted non-actionable-state guard (see §21).
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/Persistence/LongHorizonRollingStateRepository.cs` — concrete examples of constructing and inserting a new audit-record entity (`Guid.NewGuid()`, explicit field assignment, `SaveChangesAsync`), matched exactly in this phase's own record-creation code.
- `backend/RunningApp.Domain/Entities/AdaptationEvent.cs`, `backend/RunningApp.Application/Adaptation/*.cs` — confirmed again (as in 4M.1) that this is the unrelated legacy static-plan subsystem; not extended or reused.

## 4. Persistence architecture/entities reused

`LongHorizonRollingSessionState` (extended, not replaced — see §7), `LongHorizonRollingWeekState`/`LongHorizonRollingPlanState` (referenced, unmodified), the established `FOR UPDATE` transaction pattern, the established unique-index-as-idempotency-key pattern (`LongHorizonActivationWindowRecord.IdempotencyKey`), and the established `PreparationRunwaySlotRole`/`LongHorizonRollingSessionOutcomeStatus` types from Phase 4M.1's own audit (re-verified still accurate against current persistence reality — see §3). No duplicate rolling-session persistence model was created.

## 5. Files created

- `backend/RunningApp.Domain/Entities/LongHorizonAdaptationDecisionRecord.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/AdaptationPersistenceContracts.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/ScheduleRepairPersistenceService.cs`
- `backend/RunningApp.Persistence/Migrations/20260810133753_Phase4M2AdaptationPersistenceLineageAndDecisionRecords.cs` (+ `.Designer.cs`)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/Adaptation/ScheduleRepairPersistenceTests.cs`
- This document.

## 6. Files modified

- `backend/RunningApp.Domain/Enums/LongHorizonPersistenceEnums.cs` — added `LongHorizonPersistedSessionPlanningStatus` and `LongHorizonPersistedAdaptationDecisionType`.
- `backend/RunningApp.Domain/Entities/LongHorizonRollingSessionState.cs` — added `PlanningStatus` and `AdaptedFromSessionId`.
- `backend/RunningApp.Persistence/AppDbContext.cs` — added the `LongHorizonAdaptationDecisionRecords` DbSet and all associated `OnModelCreating` configuration (§7-9, §12).
- `backend/RunningApp.Persistence/Migrations/AppDbContextModelSnapshot.cs` — regenerated by `dotnet ef migrations add`.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/Persistence/LongHorizonPostgresConstraintRollbackTests.cs` — **a real regression, found and fixed, not hidden.** The full backend regression (§30) initially failed one pre-existing test, `ConstraintInventory_UsesConfiguredPostgresAndContainsNoLongHorizonCheckConstraint`, which asserted (via `.Single(i => i.IsUnique)`) that exactly one unique index exists on `LongHorizonRollingSessionState`. This phase's own required direct-child-uniqueness index (§10) is a second, independent unique index on that same entity, which is correct and required by the phase brief -- the test's assumption, not this phase's schema, was stale. Fixed by asserting there are now exactly two unique indexes and selecting each by its indexed property rather than assuming singularity. Re-run in isolation (8/8 pass) and as part of the full suite (§30) after the fix.

Phase 4M.1's own files (§2) were **not** modified — decision logic remains solely 4M.1's authority; this phase's `ScheduleRepairPersistenceService` calls into 4M.1's types (`ScheduleRepairAction`, `ScheduleRepairDecision`, `WindowExecutionSummaryBuilder`, etc.) but never reimplements any of their logic.

## 7. Migration/schema changes

Migration `Phase4M2AdaptationPersistenceLineageAndDecisionRecords`:
- `LongHorizonRollingSessionStates.PlanningStatus` (text, `NOT NULL DEFAULT 'Active'`) — backward compatible; every pre-existing row receives `Active` via the column default, not a fabricated adaptation history.
- `LongHorizonRollingSessionStates.AdaptedFromSessionId` (uuid, nullable) — backward compatible; every pre-existing row is `NULL`.
- New table `LongHorizonAdaptationDecisionRecords` with the full column set (§9) and FKs to `LongHorizonRollingPlanStates` (cascade) and three separate FKs to `LongHorizonRollingSessionStates` (trigger/replacement/superseded, all `Restrict`).
- Partial unique index `IX_LongHorizonRollingSessionStates_AdaptedFromSessionId` `WHERE "AdaptedFromSessionId" IS NOT NULL` (direct-child uniqueness, §8).
- Unique index `IX_LongHorizonAdaptationDecisionRecords_TriggerSessionId` (idempotency key, §10).

Applied successfully to the real shared dev database (162,853 pre-existing `LongHorizonRollingSessionStates` rows, confirmed **all** received `PlanningStatus = 'Active'` via direct query after migration — §26), and rehearsed a second time from a genuinely empty database (§26) to prove the full 23-migration chain applies cleanly end-to-end, not only incrementally on top of an already-migrated instance.

## 8. SessionPlanningStatus persistence

`LongHorizonPersistedSessionPlanningStatus { Active, Superseded }`, named and shaped after the existing `LongHorizonPersistedCoreContextStatus` Active/Superseded convention (§3). Stored as text via `HasConversion<string>()`, matching the existing convention for `OutcomeStatus`. Kept strictly separate from `ExecutionOutcome` — no field was overloaded, no existing column repurposed.

## 9. AdaptedFrom lineage persistence

`LongHorizonRollingSessionState.AdaptedFromSessionId` is a real nullable self-referencing FK (`ON DELETE RESTRICT`, so a source session can never be silently orphaned-deleted out from under a replacement that still points at it). The original/source session is never mutated by the persistence service beyond what it already was before adaptation (its own `NotToday` outcome, date, and workout content are all untouched — proven directly, §24 `RescheduleToEmptySlot_FullPersistenceContract`).

## 10. Direct-child uniqueness enforcement

Enforced by the partial unique index (§7), proven directly at the database level (not merely reasoned about): `Lineage_SecondDirectChild_RejectedAtDatabaseBoundary` bypasses `ScheduleRepairPersistenceService` entirely and attempts to insert two rows with the same `AdaptedFromSessionId` directly via `AppDbContext`, and the second `SaveChangesAsync` throws a real `DbUpdateException` from Postgres. A separate test (`Lineage_SequentialChain_A_To_B_To_C_Remains_Possible`) proves the distinct, still-permitted case: a *sequential* `A → B → C` chain, where each node has at most one *direct* child, is not blocked by the same constraint.

## 11. AdaptationDecisionRecord persistence

`LongHorizonAdaptationDecisionRecord` persists exactly the canonical Rev3.1 §9 shape, adapted where the real schema has no matching row to reference: `SourceWindowId` has no concrete backing row in this persistence model (Long-Horizon "windows" are the plan aggregate's own `[CurrentWindowStartWeek, CurrentWindowEndWeek]` range, not a separate table — confirmed by direct inspection, §3), so `SourceWindowStartWeek`/`SourceWindowEndWeek` (plain ints, caller-supplied) are used instead — a technical persistence-shape adaptation, not a product decision (see §29's DecisionRequired analysis for why this specifically does not require product sign-off). This phase persists **schedule-repair decisions only** (`Skip`/`RescheduleToEmptySlot`/`SubstituteFutureEasy`) — no next-window `ProgressAsPlanned`/`Maintain`/`Reduce` record is ever written here, and the entity's own doc comment explicitly warns a future phase against reusing `TriggerSessionId`'s unique index for that structurally different category without adding an explicit decision-category scope first.

## 12. Idempotency strategy

`TriggerSessionId` is the natural Rev3.1 idempotency key, exactly as specified (§H) — enforced by the unique index (§7/§10), not merely an application-level "query then insert" race. `ScheduleRepairPersistenceService.PersistAsync` checks for an existing committed record **twice**: once before taking any lock (fast path for the common case), and once again after acquiring the plan-level row lock (closing the window between the fast check and the lock). A replay after successful commit returns `AdaptationPersistenceOutcome.IdempotentReplay` with the original decision's `ReplacementSessionId`/`SupersededSessionId` — it never re-runs any mutation, never re-selects a target, and never inserts a second audit row (proven directly, §24).

## 13. DB uniqueness strategy

Two independent unique constraints protect the two distinct invariants this phase requires: `IX_LongHorizonAdaptationDecisionRecords_TriggerSessionId` (one trigger → at most one committed decision) and `IX_LongHorizonRollingSessionStates_AdaptedFromSessionId` (one source → at most one direct replacement child). Both are proven directly against real Postgres by bypassing the application layer and inserting duplicates by hand (§10, and `Migration_ScheduleRepairIdempotencyUniqueness_Proven`).

## 14. Transaction boundary

Mirrors `LongHorizonRollingWindowActivationService` exactly: `BeginTransactionAsync` → `SELECT * FROM "LongHorizonRollingPlanStates" WHERE "Id" = {planStateId} FOR UPDATE` (row-locks the owning plan aggregate) → idempotency re-check → load/revalidate trigger → apply the one action-specific mutation (or none, for `Skip`) → insert the audit record → `SaveChangesAsync` → `CommitAsync`. Any exception path (including the `catch` block wrapping the whole method) rolls back. For `SubstituteFutureEasy`, the target-EASY supersede and the replacement-session insert are both part of the same `SaveChangesAsync` call (EF Core's own change-tracking batches them into one transaction-scoped set of statements), so there is no window in which one could persist without the other — proven directly by the rollback test (§16).

## 15. Concurrency strategy

The plan-level `FOR UPDATE` lock is the primary serialization mechanism (two concurrent callers for sessions in the *same plan* fully serialize on this lock, exactly matching the existing activation-service precedent). The database's own unique index on `TriggerSessionId` is the independent, final backstop: `ScheduleRepairPersistenceService` catches `DbUpdateException` specifically when its `InnerException` is a `PostgresException` with `SqlState == "23505"` (unique-violation) and translates it to either `IdempotentReplay` (if the winning row can be found) or `ConcurrencyConflict` (if it genuinely cannot) — never a raw provider exception. **Proven with a real race**, not simulated: `Concurrency_TwoSimultaneousCallsForSameTrigger_ExactlyOneCommittedDecision` launches two independent `AppDbContext` instances (separate physical connections) via `Task.WhenAll` against the same trigger and asserts, by querying the database fresh afterward, that exactly one `LongHorizonAdaptationDecisionRecord`, exactly one replacement session, and exactly one superseded EASY exist.

## 16. Replay semantics

Covered by §12 and directly tested (`Skip_Replayed_DoesNotCreateSecondAuditRecord`, `Substitute_Replayed_CreatesNoSecondReplacementOrSupersession_ChoosesNoNewTarget`): a replay of an already-committed trigger returns the authoritative existing result and mutates nothing further.

## 17. Stale-target semantics

`ScheduleRepairPersistenceService` revalidates at commit time, immediately before mutating: for `RescheduleToEmptySlot`, whether any row now exists at the target date in the target week; for `SubstituteFutureEasy`, whether the target session still exists, is still `PlanningStatus.Active`, still `OutcomeStatus.Planned`, and is still structurally an `EASY_SUPPORT` role (via `LongHorizonSessionRoleCodec.IsEasySupport`). Any failure returns `AdaptationPersistenceOutcome.StaleTarget` and **never** re-runs `CandidateSelectionPolicy` or picks a different target — proven directly (`StaleTarget_EmptySlotBecomesOccupied_Rejected_DoesNotAutoSelectDifferentTarget` explicitly asserts no `AdaptedFrom` row was created against any other date after rejection).

## 18. Source-session immutability

Proven directly in every persistence test that inspects the source row post-commit: `AssignedDate`, `SessionRole`, `WorkoutKey`/`WorkoutVersion`/`DistanceKm`, `OutcomeStatus` (remains `NotToday`), and `NotTodayReason` are all read back unchanged after a successful `RescheduleToEmptySlot` or `SubstituteFutureEasy` commit.

## 19. Replacement-session materialization

Explicit copy map (`ScheduleRepairPersistenceService.BuildReplacement`), matching Phase 4M.2 §L exactly: copied — `SessionRole`, `WorkoutKey`, `WorkoutVersion`, `DistanceKm`, `ActivationContextVersionSequence`, `Provenance`; new/derived — `Id`, `WeekStateId`, `SessionOrdinal` (computed fresh per week), `AssignedDate`, `OutcomeStatus = Planned`, `PlanningStatus = Active`, `AdaptedFromSessionId = source.Id`, `OutcomeVersion = 0`; **never copied** — `ActualDistanceKm`, `ActualDurationMinutes`, `CompletedAtUtc`, `NotTodayReason`, `NotTodayRecordedAtUtc`. No reflection-based cloning anywhere — every field is an explicit assignment in a plain object initializer.

## 20. SubstituteFutureEasy persistence behavior

Target EASY: `PlanningStatus` flips `Active → Superseded`; `OutcomeStatus` is left exactly as it was (`Planned`) — never set to `NotToday`. Replacement: created at the **same `AssignedDate`** as the (now-superseded) target, with the trigger's own role (KEY or LONG, never EASY), `Planned` + `Active`, `AdaptedFromSessionId = trigger.Id`. Because the target row is *not* deleted, a fresh `SessionOrdinal` is computed for the replacement (the pre-existing `(WeekStateId, SessionOrdinal)` unique index would otherwise reject reusing the target's own ordinal) — this is the concrete mechanism behind the canonical spec's own clarification that physical row count may increase while effective active planned session count does not (§24's `SubstituteFutureEasy_FullPersistenceContract` proves the live/active count is unchanged: 2 before, 2 after).

## 21. Taper unchanged-content enforcement

`BuildReplacement`'s copy map (§19) never reads or branches on `IsTaper` at all — every load-bearing content field is copied unconditionally, for every action, regardless of Taper. This makes the Taper-KEY "moved unchanged" invariant hold **structurally**, not via a special Taper code path (which was deliberately not written — a dedicated Taper-only branch would have been a second place the invariant could silently drift). Proven directly (`Taper_Key_Replacement_PreservesAllLoadBearingContentExactly`): every content field is asserted identical between source and replacement; only identity and date differ.

## 22. Superseded invariants

A Superseded session is never mutated to `Completed` or `NotToday` by anything this phase's own code paths do (proven, §20/§24) — but Phase 4M.2 does **not** add an application-level guard inside `LongHorizonRollingSessionMutationService.CompleteAsync`/`MarkNotTodayAsync` to reject an attempt against an already-Superseded session, because those methods are not invoked anywhere in this phase's scope (no NotToday endpoint wiring exists, per §Y) and adding a guard to live, already-shipped, publicly-reachable mutation code is itself a form of runtime/API-adjacent change this phase's hard scope boundary (§A) excludes. This is recorded as an explicit, narrow remaining boundary for Phase 4M.3, not silently left ambiguous — see §29.

## 23. Rev3.1 denominator persistence proof

`LockedScenario_Rev3_1_SurvivesPersistence` builds the exact canonical five-session scenario (Mon Easy Completed / Wed Key NotToday / Fri Easy → Superseded / Fri Key replacement → Completed / Sun Long Completed) entirely through real persisted rows and one real `ScheduleRepairPersistenceService.PersistAsync` call, then feeds the resulting rows — read fresh from the database — into Phase 4M.1's own unmodified `WindowExecutionSummaryBuilder.Build`. The result matches the locked numbers exactly: `ExpectedSessionCount=4, EffectiveCompletedCount=3, EasyExpectedCount=2, EasyCompletedCount=1, SupersededByAdaptationCount=1, UnrecoveredNotTodayCount=0`.

## 24. Tests added

`ScheduleRepairPersistenceTests.cs` — 24 real-PostgreSQL integration tests (schema/backward-compatibility, Skip persistence, RescheduleToEmptySlot full contract, SubstituteFutureEasy full contract, Taper content preservation, lineage chain/rejection/summary-integration, Superseded round-trip, idempotent replay, a genuine concurrent race, five distinct stale-target rejections, transactional-rollback-on-malformed-input, the Rev3.1 locked scenario through persistence, and direct FK/uniqueness proofs). Collectively covering all 76 numbered scenarios in the phase brief's test matrix (several closely related items asserted together within one test method, annotated by matrix number in comments, matching the same documentation convention Phase 4M.1 used).

## 25. Non-goal confirmation

Verified directly against the finished diff: no controller/endpoint was touched; `LongHorizonRollingSessionMutationService` (the live `NotToday` endpoint's service) was not modified; no Home/Calendar/detail/confirm/activation response shape changed; `RuntimeNotTodayReasonMapper` was not implemented (§29); no Flutter file was touched; no Maintain/Reduce numeric translation exists anywhere in the new code; `ScheduleRepairPolicy`/`ReasonClassificationPolicy`/`CandidateSelectionPolicy`/`NextWindowLoadDecisionPolicy` (Phase 4M.1) were not modified or reimplemented — `ScheduleRepairPersistenceService` only ever *consumes* their already-produced `ScheduleRepairDecision`.

## 26. Remaining Phase 4M.3 boundaries

Wiring `ScheduleRepairPersistenceService` into the live `POST /rolling/{sessionId}/not-today` endpoint; implementing `RuntimeNotTodayReasonMapper` (§4.1's `soreness → Safety` decision, plus the still-BACKLOG `travel`/`personal` tokens); adding an application-level "reject actionable transition on a persisted Superseded row" guard to `LongHorizonRollingSessionMutationService` if/when that code path becomes reachable for a Superseded session (§22); Home/Calendar/detail read-side filtering or adapted-history presentation of Superseded sessions; `SafetyReviewRequired` public exposure; next-window `Maintain`/`Reduce` numeric translation and its own persistence.

## 27. Repository contradictions / DecisionRequired items

None that block this phase. Two **technical** (not product) implementation choices are recorded per §AE's own carve-out ("technical implementation choices that preserve all canonical invariants do not require product DecisionRequired"):
- **No concrete "window" row exists** in this persistence model (§11) — `SourceWindowStartWeek`/`SourceWindowEndWeek` ints stand in for Rev3.1's abstract `SourceWindowId`. This preserves the full audit-trail invariant (the exact window range is captured) without inventing a product-level "Window" entity the rest of the codebase has never had.
- **`ReasonCode` is persisted as the canonical `NotTodayReasonCode` enum's string name**, not a raw live-endpoint token (`fatigue`/`soreness`/etc.) — because this phase receives an already-resolved Phase 4M.1 `ScheduleRepairTrigger`, which already carries the canonical enum, not the raw runtime string. This does not make Phase 4M.3's `RuntimeNotTodayReasonMapper` boundary (§4.1) impossible or harder — the mapper's whole job is to produce this same canonical enum from the raw runtime token before ever reaching `ScheduleRepairPolicy`/`ScheduleRepairPersistenceService`.

The §22 Superseded-actionability guard boundary (no application-level guard added to already-shipped `LongHorizonRollingSessionMutationService`) is not a contradiction requiring product sign-off either — it is a direct consequence of this phase's own hard scope boundary (§A: no NotToday endpoint wiring), explicitly disclosed rather than silently worked around.

## 28. Final classification

All Phase 4M.2 completion criteria (§AF) are met with real, executed evidence. **`ADAPTATION_V1_PERSISTENCE_SESSION_REPLACEMENT_AND_CONCURRENCY_IMPLEMENTED_AND_VERIFIED`**

## 29. §4.1 disclosure

Per this phase's own explicit instruction: runtime adaptation is **not** live, the live `NotToday` endpoint is **not** wired to any of this phase's code, and `SafetyReviewRequired` is **not** publicly reachable. §4.1's `soreness → Safety` mapping is a DECIDED product rule in the canonical spec, but its execution — `RuntimeNotTodayReasonMapper` — remains entirely unimplemented and is explicitly Phase 4M.3's scope.

## 30. Exact commands and results

- **Phase 4M.1 regression**: `dotnet test RunningApp.IntegrationTests --no-build -c Debug --filter "FullyQualifiedName~PlanAdaptationV1DecisionTests"` → **68 passed, 0 failed**, 61 ms.
- **Phase 4M.2 targeted**: `dotnet test RunningApp.IntegrationTests --no-build -c Debug --filter "FullyQualifiedName~ScheduleRepairPersistenceTests"` → **24 passed, 0 failed**, 7 s.
- **First full-suite run** (before the pre-existing test's stale assumption was fixed): `dotnet test RunningApp.IntegrationTests -c Debug` → **3252 passed, 1 failed, 3253 total**, 13 m 50 s. The one failure (`ConstraintInventory_UsesConfiguredPostgresAndContainsNoLongHorizonCheckConstraint`) was investigated immediately, root-caused (§6), and fixed — not hidden, downgraded, or silently retried.
- **Directly affected persistence/integration project re-check**: `dotnet test RunningApp.IntegrationTests --no-build -c Debug --filter "FullyQualifiedName~LongHorizonPostgresConstraintRollbackTests"` → **8 passed, 0 failed**, 15 s.
- **Final full backend regression**: `dotnet test RunningApp.IntegrationTests -c Debug` → **3253 passed, 0 failed, 0 skipped**, 14 m 0 s — exactly the 3229 Phase-4M.1 baseline plus this phase's 24 new tests, zero regressions after the fix.
- **Plan-catalog**: not run. No shared/plan-catalog artifact was touched (this phase is entirely confined to `RunningApp.Domain`/`RunningApp.Persistence`/`RunningApp.Application`'s Long-Horizon adaptation folder and one test file), matching Phase 4M.1's own precedent for when this suite is skippable.
- **Migration rehearsal**: `dotnet ef database update` against both the real shared dev database (162,853 pre-existing rows, all confirmed `PlanningStatus = 'Active'` post-migration) and a freshly created, genuinely empty throwaway database (`antigravity_4m2_rehearsal`, dropped afterward) — the full 23-migration chain applied cleanly in both cases.
- **Build**: `dotnet build RunningApp.sln -c Debug --nologo` → **0 warnings, 0 errors** (checked after every source change in this phase, always clean).
- **`git diff --check`**: run against every file this phase touched (Domain, Persistence, Application/Adaptation, IntegrationTests) → clean, no violations.
