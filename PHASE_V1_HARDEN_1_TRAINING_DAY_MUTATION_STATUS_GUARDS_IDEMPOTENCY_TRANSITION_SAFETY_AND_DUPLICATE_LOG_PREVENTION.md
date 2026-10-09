# PHASE V1-HARDEN.1 — Training-Day Mutation Status Guards, Idempotency, Transition Safety, and Duplicate-Audit-Log Prevention

## 1. Binding V1-HARDEN.0 findings

Read `PHASE_V1_HARDEN_0_...md` in full (not the summary). Binding findings consumed verbatim:

- **`V1HARDEN0-002`** (§19, §82) — `CompleteWorkoutAsync` (`QueryAndMutationServices.cs:388-451` at that time) never checked `day.Status` before mutating. A repeated call re-set `Status/ActualDistanceKm/ActualDurationMin/CompletedAt` to the new call's values (silently overwriting the first call's recorded actuals) and unconditionally appended a **new** `WorkoutLog` row and a **new** `PlanEvent` row every call — unbounded duplicate audit-trail rows for one real workout. `week.ActualVolumeKm` recomputation was the one genuinely idempotent side effect (it explicitly excludes the current day before re-adding it).
- **`V1HARDEN0-004`** (§20-21, §82) — `CreateNotTodayDecisionAsync` had zero status checks. A `NotToday` decision could be created and later confirmed (`ConfirmNotTodayDecisionAsync`, which sets `TrainingDay.Status = Missed`) against a day whose `Status` was already `Completed`, silently reverting a genuinely completed workout to `Missed`.
- **`V1HARDEN0-003`** (§20, §83, P2-leaning but explicitly folded into this phase by V1-HARDEN.0 §87's own follow-up slicing: "fix V1HARDEN0-002 ... and V1HARDEN0-004/V1HARDEN0-003 ... together") — repeated `CreateNotTodayDecisionAsync` calls against a non-Completed day created unbounded duplicate `Pending` decision rows for one real tap.
- Also binding: §57 folded the concurrency half of this same defect into `V1HARDEN0-002` ("concurrent-session mutation is covered by the ordinary read-modify-write-via-`SaveChangesAsync` pattern, which... does not itself prevent two concurrent `Complete` calls from both succeeding") — i.e. this is not only a serial-replay defect but a genuine concurrency race, confirmed by that audit, not newly discovered here.
- Zero-delta constraints (must NOT touch): active-plan DB-level uniqueness (§11-12, §72), cross-user authorization (§42-43/§72 — ownership scoped via server-resolved `InternalUserId` everywhere), preview→confirm frozen-snapshot contract (§13-14, just restored to reachability by V1-HARDEN.0A), catalog/startup validation (§47-49).

V1-HARDEN.0A (read in full) confirmed: zero backend files changed in that phase; V1HARDEN0-002/003/004 were explicitly carried forward untouched to this phase (§66 of that report).

## 2. Scope/non-scope

In scope: `CompleteWorkoutAsync`, `CreateNotTodayDecisionAsync`, `ConfirmNotTodayDecisionAsync` (the method that actually performs the Completed→Missed mutation V1HARDEN0-004 names — fixing creation-time alone cannot close the race variant of that finding). Out of scope and untouched: training science, distance routing, FIVE_K/TEN_K/HM authority, generated plan content, calendar assignment, onboarding, preview/confirm, observability/metrics, `ResolvePendingConfirmationAsync`/`GetPendingConfirmationsAsync` (a structurally separate `PendingConfirmation` subsystem, not named in V1HARDEN0-002/003/004 and not touched), V1-HARDEN.0's P2/P3 items (exception-message leakage beyond what naturally falls out of the two new typed exceptions' own messages, cross-field volume validation, metrics pipeline).

## 3. Existing Complete path

`POST /api/v1/training-days/{trainingDayId}/complete` → `TrainingDaysController.Complete` → `IWorkoutCompletionService.CompleteWorkoutAsync` → `QueryAndMutationServices.CompleteWorkoutAsync`. Pre-phase: loaded the day tracked (`Include(Week)`), unconditionally set `Status=Completed` plus actuals/timestamps/flags, appended a `WorkoutLog` and a `PlanEvent`, recomputed `week.ActualVolumeKm`, one `SaveChangesAsync` call, no transaction object, no status check, no plan-state check.

## 4. Existing NotToday path

Two public endpoints: `POST /api/v1/training-days/{trainingDayId}/not-today-decisions` (`CreateNotTodayDecisionAsync` — creates a `Pending` `NotTodayDecision` row only; does **not** touch `TrainingDay.Status`) and `POST /api/v1/not-today-decisions/{decisionId}/confirm` (`ConfirmNotTodayDecisionAsync` — the method that actually sets `TrainingDay.Status = Missed`, runs the adaptation engine, and logs a `WorkoutMissed` `PlanEvent`). Pre-phase: neither method checked any status before mutating.

## 5. TrainingDay status model

`TrainingDayStatus` enum (source: `RunningApp.Domain/Enums/TrainingDayStatus.cs`): `Planned, Completed, Missed, Skipped, PendingConfirmation, Rescheduled, SoftMissed`. Grepped every assignment site across `RunningApp.Application`: in the **Core** (non-rolling) `TrainingDay` path exercised by this phase's two endpoints, only three values are ever actually assigned — `Planned` (creation), `Completed` (`CompleteWorkoutAsync`, `ResolvePendingConfirmationAsync`), `Missed` (`ConfirmNotTodayDecisionAsync`, `ResolvePendingConfirmationAsync`). `Skipped`/`SoftMissed` are used only inside the structurally separate Long-Horizon rolling-session evidence mapper (`LongHorizonRollingOutcomeEvidenceAdapter`), never on a real persisted Core `TrainingDay` row. `PendingConfirmation`/`Rescheduled` have zero assignment sites anywhere in the read code. The effective Core status model is therefore exactly three live states: `Planned → {Completed, Missed}`, both terminal.

## 6. Stored vs derived statuses

`TrainingDay.Status` is a persisted column — not derived at read time. The pre-existing `CanMarkComplete`/`CanMarkNotToday` boolean columns (also persisted, defaulting `true` at creation in both `CatalogPlanConfirmationService.BuildCatalogTrainingDay` and the legacy `PlanServices` path) are the domain's own pre-existing, already-wired "is this action currently allowed" signal — both are explicitly flipped to `false` the moment a day becomes `Completed` (`CompleteWorkoutAsync`) or `Missed` (`ConfirmNotTodayDecisionAsync`, `ResolvePendingConfirmationAsync`), confirmed by direct read of all four call sites. This is the single most important piece of **existing, pre-established product authority** this phase found: the domain already encodes "both mutations become permanently invalid once a day reaches either terminal state" — it was simply never *enforced* at mutation time, only *recorded* after the fact and left for the frontend to read defensively. Calendar/Home `Status` display is the same persisted column, never re-derived.

## 7. Current transition matrix (pre-fix, from source)

| CurrentStatus | Mutation | Pre-fix behavior | Existing authority signal |
|---|---|---|---|
| Planned | Complete | Succeeds, sets Completed | `CanMarkComplete=true` pre-mutation |
| Planned | CreateNotToday | Succeeds, creates Pending decision | `CanMarkNotToday=true` pre-mutation |
| Completed | Complete (retry) | Overwrites actuals, duplicates log/event | `CanMarkComplete` already `false` — **violated** |
| Completed | CreateNotToday | Succeeds (creates decision); confirming it later overwrites Status to Missed | `CanMarkNotToday` already `false` — **violated** |
| Missed | Complete | Succeeds, overwrites to Completed | `CanMarkComplete` already `false` — **violated** |
| Missed | CreateNotToday (retry) | Succeeds, creates a second Pending decision | `CanMarkNotToday` already `false` — **violated** |
| NotToday Pending, same day Planned | CreateNotToday (retry) | Succeeds, creates a second Pending decision | no signal exists for this case at all |

Every "violated" row is a real, reproducible instance of the governing prompt's "frontend is defense-in-depth only" principle being the *only* line of defense pre-phase — the backend recorded `CanMarkComplete=false`/`CanMarkNotToday=false` but never read it back.

## 8. Product-authority review

Per §6, the existing, pre-established, already-wired `CanMarkComplete`/`CanMarkNotToday` flag semantics are treated as binding product authority — not invented here. This directly resolves §16 of the governing prompt (NotToday→Complete) **from source**, not as a product-decision punt: the domain's own prior convention (both flags false after `Missed` is set) establishes that `Missed` is a terminal state from which `Complete` is not permitted, exactly symmetric with `Completed` being terminal against `NotToday`. No new product semantics were invented; the existing flag-write convention was simply finally *enforced* at the two mutation entry points instead of only recorded.

## 9. Guard architecture

One authoritative loader, `LoadOwnedActiveDayForMutationAsync` (`QueryAndMutationServices.cs`), used by all three mutation methods (`CompleteWorkoutAsync`, `CreateNotTodayDecisionAsync`, `ConfirmNotTodayDecisionAsync`). It performs, in order: (a) ownership scoping (`d.Plan.InternalUserId == internalUserId` — zero delta from the pre-existing convention), (b) the plan-state guard (§15-16), (c) a Postgres row lock (`SELECT ... FOR UPDATE` on the owning `TrainingPlan`) that serializes all three mutation methods against the same plan (§20), (d) loading the `TrainingDay` fresh, tracked, with its `Week` included. Each caller then applies its own status-specific branch (idempotent-replay / terminal-conflict / proceed) inline — kept as three small, independently readable `if` blocks per method rather than a separate abstract "transition policy" class, per the governing prompt's own "don't over-abstract" instruction and this repository's existing anemic-entity/service-layer style (no prior `TrainingDay.Complete()`/`MarkNotToday()` domain methods existed to strengthen).

## 10. Complete retry behavior

Exact replay (same `ActualDistanceKm`/`ActualDurationMin`, compared via `IsSameCompletion`) against an already-`Completed` day returns the existing `CompleteWorkoutResponse` with no new `WorkoutLog`/`PlanEvent` row and no re-mutated `CompletedAt`/`UpdatedAt`. A *different*-valued retry throws `TrainingDayCompletionConflictException` (409, `TRAINING_DAY_COMPLETION_CONFLICT`) — the original recorded result is left untouched. Proven with real Postgres: `CompleteRetry_SameValues_IsIdempotent_NoDuplicateLogOrEvent`, `CompleteRetry_DifferentValues_Returns409_DoesNotOverwriteOriginal`.

## 11. NotToday retry behavior

Two layers, matching the two public endpoints: (a) `CreateNotTodayDecisionAsync` retried against a day still `Planned` with an existing `Pending` decision returns that same decision (`decisionId`, `status=pending`) instead of creating a duplicate row (closes `V1HARDEN0-003`) — proven by `NotTodayRetry_Pending_DoesNotCreateDuplicateDecision`. (b) `ConfirmNotTodayDecisionAsync` retried against an already-`Confirmed` decision returns the same result (`status=confirmed`, the original `Action`, `PlanAdapted=false`) with no re-run adaptation and no duplicate `WorkoutMissed` `PlanEvent` — proven by `NotTodayConfirmRetry_IsIdempotent_NoDuplicateMissedEvent`.

## 12. Completed→NotToday protection (V1HARDEN0-004, mandatory)

Two-layer fix, because the real mutation happens at *confirm* time, not creation time: (a) `CreateNotTodayDecisionAsync` rejects outright (409 `TRAINING_DAY_TRANSITION_CONFLICT`) if `day.Status == Completed` at creation time — proven by `CompletedDay_CannotBeRevertedByNotToday_CreateIsRejected`. (b) `ConfirmNotTodayDecisionAsync` — the actual authoritative guard point — re-checks `day.Status` under the plan lock immediately before applying `Missed`; if the day became `Completed` in the window between decision-creation and confirm (the exact race V1-HARDEN.0 described), it cancels the stale decision (`NotTodayDecisionStatus.Cancelled`) and rejects with 409, leaving the `Completed` status, its `WorkoutLog`, and its `PlanEvent` completely untouched — proven with real Postgres by `CompletedDay_CannotBeRevertedByNotToday_RaceBetweenCreateAndComplete_ConfirmIsRejected`, which deliberately creates the decision, then completes the day, then confirms, reproducing V1-HARDEN.0's exact scenario end-to-end.

## 13. NotToday→Complete behavior

Resolved from source (§8), not punted: `day.Status == Missed` → `CompleteWorkoutAsync` throws `TrainingDayTransitionConflictException` (409 `TRAINING_DAY_TRANSITION_CONFLICT`). Result: **`NOTTODAY_TO_COMPLETE_REJECTED_BY_PRODUCT_STATE_GUARD`**. Proven by `MissedDay_CannotBeCompleted_NotTodayToCompleteRejected`.

## 14. Missed behavior

`Missed` is a persisted status, set only by `ConfirmNotTodayDecisionAsync`/`ResolvePendingConfirmationAsync` (the latter untouched, out of scope). It is not derived by date and not distinct from "not-today" in the Core model — `NotTodayDecision.ResultingStatus` is always `Missed`, confirming the domain's own existing equivalence (not introduced by this phase). Treated as terminal, symmetric with `Completed`.

## 15. Plan-state guard

`LoadOwnedActiveDayForMutationAsync` throws `NotFoundAppException` ("Training day not found.") if the owning `TrainingPlan.Status != Active` — mirroring the exact "not found" treatment `LongHorizonRollingSessionMutationService.LoadOwnedAsync` already uses for a non-Active owning plan, so a cancelled plan's historical `TrainingDay` rows remain readable (unchanged) but are never mutable through Complete/CreateNotToday/ConfirmNotToday.

## 16. Cancelled/inactive plan behavior

Proven with real Postgres: `CancelledPlan_CompleteIsBlocked` and `CancelledPlan_NotTodayIsBlocked` — confirm a plan, cancel it, then call Complete/CreateNotToday on one of its days: both return 404, the day's `Status` stays `Planned` (unchanged), the plan stays `Cancelled` (no reactivation), zero new `WorkoutLog`/`NotTodayDecision` rows. Result: **`INACTIVE_PLAN_TRAINING_DAY_MUTATIONS_ARE_GUARDED`**.

## 17. Duplicate-log root cause

`CompleteWorkoutAsync` unconditionally constructed and added a new `WorkoutLog`/`PlanEvent` on every call, with no read-before-write check against the day's current status. `CreateNotTodayDecisionAsync` unconditionally constructed and added a new `NotTodayDecision` on every call. Root cause in both cases: the mutation's side effects were applied before any guard existed, not an architectural duplication-detection gap.

## 18. Duplicate-log prevention

Fixed by preventing the *write*, not by deduplicating on read: `CompleteWorkoutAsync`'s side-effect code (`ApplyCompletionAsync`) is now reached at most once per logical completion (gated by the status check in §10); `CreateNotTodayDecisionAsync` looks up an existing `Pending` decision for the same `TrainingDayId` before constructing a new one (§11a); `ConfirmNotTodayDecisionAsync` only constructs its `WorkoutMissed` `PlanEvent` once per decision (gated by the status check in §11b). No existing natural unique key was available to enforce this at the DB level without a new index/migration, and the serialized-via-plan-lock application-level check (§9/§20) is sufficient given the governing prompt's own "smallest correct protection, no DB migration by default" instruction.

## 19. Transaction boundary

All three mutation methods now run inside an explicit `_context.Database.BeginTransactionAsync()`/`CommitAsync()` boundary (the pre-existing code relied on `SaveChangesAsync`'s own atomicity for a single call, with no transaction object) — status update, log/event insert, and (for `CompleteWorkoutAsync`) the week-volume recompute now commit together or not at all, matching the existing precedent in `LongHorizonRollingSessionMutationService`/`CatalogPlanConfirmationService`.

## 20. Concurrency mechanism

Two layers, both reused from existing architecture (no new concurrency framework invented): (1) a Postgres row lock (`SELECT * FROM "TrainingPlans" WHERE "Id" = {planId} FOR UPDATE`) taken on the owning plan at the start of every mutation, serializing Complete/CreateNotToday/ConfirmNotToday against the same plan — the exact pattern `LongHorizonRollingSessionMutationService.LoadOwnedAsync` already uses; (2) an EF optimistic-concurrency token mapped onto Postgres's own built-in `xmin` system column on `TrainingDay` (`AppDbContext.cs`), the exact pattern already used for `LongHorizonRollingPlanState`, caught via `DbUpdateConcurrencyException` as defense-in-depth. **No database migration was added** — `xmin` already exists on every Postgres table; this is a pure EF model-configuration change. One genuine side effect required a configuration change, not a migration (see §58).

## 21. Concurrent Complete+Complete

Real-Postgres proof: `ConcurrentIdenticalCompletes_ProduceOneLogicalTransition` — two concurrent identical Complete requests against the same day both return 200; final state has exactly 1 `WorkoutLog` row, exactly 1 `WorkoutCompleted` `PlanEvent`, `Status=Completed`. `ConcurrentConflictingCompletes_ExactlyOneWins_NoLastWriteWinsCorruption` — two concurrent Complete requests with *different* actual values: exactly one 200, exactly one 409, exactly 1 `WorkoutLog` row, final `ActualDistanceKm` is one of the two submitted values (never a corrupted/mixed state, never both silently "sort of" applied).

## 22. Concurrent NotToday+NotToday

Covered by the same plan-lock serialization proven in §11a/§21's mechanism (identical code path); the duplicate-Pending-decision race is closed by re-querying for an existing Pending row *after* acquiring the plan lock, so a second concurrent create cannot pass the "no existing pending" check until the first's transaction has committed.

## 23. Concurrent Complete+NotToday

Real-Postgres proof: `ConcurrentCompleteVsConfirmNotToday_ExactlyOneDeterministicFinalState` — a Pending decision is created first (day still Planned), then Complete and Confirm-NotToday are fired concurrently against the same day. Exactly one deterministic final state results every run: either (day=`Completed`, decision=`Cancelled`, Complete=200, Confirm=409, 1 `WorkoutLog`, 0 `WorkoutMissed` events) or (day=`Missed`, decision=`Confirmed`, Complete=409, Confirm=200, 0 `WorkoutLog`, 1 `WorkoutMissed` event). Neither a 500 nor a mixed/corrupted state was observed across repeated runs.

## 24. Expected concurrency responses

Winner: 200 with the applied result. Loser: either an idempotent-replay 200 (same-mutation case) or a 409 (`TRAINING_DAY_COMPLETION_CONFLICT`, `TRAINING_DAY_TRANSITION_CONFLICT`, or `TRAINING_DAY_MUTATION_CONCURRENCY_CONFLICT`) — never a 500, never a silent overwrite.

## 25. HTTP/application result mapping

Three new typed exceptions (`RunningApp.Application.Exceptions.AppExceptions.cs`), all mapped to HTTP 409 in `GlobalExceptionHandler.cs`: `TrainingDayCompletionConflictException` → `TRAINING_DAY_COMPLETION_CONFLICT`; `TrainingDayTransitionConflictException` → `TRAINING_DAY_TRANSITION_CONFLICT`; `TrainingDayMutationConcurrencyConflictException` → `TRAINING_DAY_MUTATION_CONCURRENCY_CONFLICT`. The plan-state guard reuses the pre-existing `NotFoundAppException` (404) rather than inventing a fourth code, matching the exact precedent `LongHorizonRollingSessionMutationService` already set for the identical "day belongs to a non-Active plan" case.

## 26. Exception handling

Only `DbUpdateConcurrencyException` is caught and translated; every other exception (unexpected DB failure, etc.) still falls through to the existing generic 500 path. No broad automatic retry was added — the loser of a concurrency race gets one deterministic 409/200 response and must decide to retry itself, exactly matching the governing prompt's "don't add broad automatic retry" instruction.

## 27. Cross-user authorization zero-delta

`LoadOwnedActiveDayForMutationAsync`'s ownership predicate (`d.Plan.InternalUserId == internalUserId`) is unchanged from the pre-existing convention, now simply reused in one place instead of duplicated three times. No change to how `InternalUserId` is resolved (`ICurrentUserAccessor`, server-side only, never client-supplied).

## 28. Valid Complete flow

Proven: `ScheduledDay_Complete_Succeeds` — a freshly-confirmed plan's first scheduled day completes successfully (200) on the very first call.

## 29. Valid NotToday flow

Proven: `ScheduledDay_NotToday_Succeeds` — a freshly-confirmed plan's first scheduled day accepts a Not-Today decision successfully (200) on the very first call.

## 30-32. Home / Calendar / TrainingDay detail consistency

`GetHomeAsync`/`GetCalendarAsync`/`GetTrainingDayDetailAsync` were not modified by this phase and read the same persisted `TrainingDay.Status`/`ActualDistanceKm`/`ActualDurationMin` columns this phase's mutations write — by construction, a successful Complete/NotToday mutation is immediately visible to all three reads with no cache layer in between (V1-HARDEN.0 §57's own "no cache exists" finding, unchanged). Not independently re-tested with a dedicated Home/Calendar HTTP assertion in this phase (the mutation integration tests assert directly against the database row, which is the same row every read path queries) — treated as `STRUCTURALLY_PROVEN`, not a fresh `AUDIT_GAP`, since no code path in any of the three read methods was touched.

## 33. History/log count proof

Every mutation test in `V1Harden1TrainingDayMutationTests.cs` asserts `WorkoutLogs`/`PlanEvents`/`NotTodayDecisions` row counts directly via `AppDbContext`, not merely the final `Status` field — satisfying the governing prompt's "don't infer no-duplicate solely from one final status" instruction.

## 34. Real Postgres proof

All 15 new tests in `backend/RunningApp.IntegrationTests/V1Harden1TrainingDayMutationTests.cs` run against the real local Postgres 17 instance (`appsel-dev-postgres`) through the real HTTP pipeline (`CustomWebApplicationFactory`, same infrastructure `FiveK1PublicActivationEndToEndTests` uses) — no mocks, no in-memory provider. This specifically includes the three concurrency scenarios (§21-23), which used two real, separate `HttpClient`s issuing genuinely concurrent requests via `Task.WhenAll` against the real database.

## 35. Unit/domain tests

None added as a separate project. This repository has no `RunningApp.UnitTests` project and no prior Core-mutation unit-test convention to extend — every mutation-safety claim in this engagement to date has been proven through `RunningApp.IntegrationTests` against real Postgres (consistent with this engagement's own stated discipline that in-memory/mock tests cannot prove write-race behavior). Adding a parallel, separate unit-test project for three small `if`-guards that are already exhaustively exercised against the real database was judged to be process overhead without proof value, not time-saving under-testing — the governing prompt's own instruction not to over-abstract applies equally to test architecture.

## 36. Application tests

Covered by the same integration suite — each HTTP call in `V1Harden1TrainingDayMutationTests.cs` exercises the full controller→service→DbContext chain for authorized-valid / same-mutation-repeated / conflicting-transition / cancelled-plan scenarios.

## 37. HTTP integration tests

All 15 new tests call real public endpoints (`/api/v1/training-days/{id}/complete`, `/api/v1/training-days/{id}/not-today-decisions`, `/api/v1/not-today-decisions/{id}/confirm`, `/api/v1/plans/{id}/cancel`) via `HttpClient`, asserting on `HttpStatusCode` and the raw JSON error envelope (`errorCode`), not by calling application services directly.

## 38. Frontend defense-in-depth

Not touched. No Flutter file was modified. The existing Complete/NotToday button widgets were not inspected for a tap-disable gap in this phase, since the backend guard now stands independently and correctly regardless of frontend behavior, and the governing prompt scopes any frontend change in this phase as optional, not required, when the backend alone already closes the finding.

## 39. Onboarding zero-delta

No onboarding file touched. `git diff --stat` (§58) confirms this.

## 40. Active-plan uniqueness zero-delta

`AppDbContext.cs`'s `IX_TrainingPlans_InternalUserId_ActiveOnly`/`IX_TrainingPlans_SourcePreviewId` partial unique indexes were not touched. `CancelledPlan_CompleteIsBlocked`/`CancelledPlan_NotTodayIsBlocked` incidentally re-exercise cancel-then-mutate without finding any regression in this invariant.

## 41. FIVE_K zero-delta

`PlanCatalog.Tests` reconfirmed fresh this phase at 1510/1510 (§54), zero semantic delta — no catalog file was touched. The mutation tests use a real FIVE_K Intermediate 4D plan purely as fixture data (per the governing prompt's own suggestion), proving nothing new about distance correctness.

## 42. Derived/canonical plan zero-delta

No `RuntimeCatalog`, `CatalogVolumeAndLongRunPlanner`, `V1CatalogPilotIdentityPolicy`, `RaceHorizonPolicy`, or any other distance/eligibility/volume/horizon file was read for modification purposes in this phase (only `QueryAndMutationServices.cs`, `AppDbContext.cs`, `AppExceptions.cs`, `GlobalExceptionHandler.cs`, `Program.cs` — all lifecycle/persistence/error-contract files, zero plan-generation files).

## 43. Habit-plan impact

`CompleteWorkoutAsync`/`CreateNotTodayDecisionAsync`/`ConfirmNotTodayDecisionAsync` are shared by both `GoalType.Race` and `GoalType.Habit` plans — `TrainingDaysController`/`NotTodayDecisionsController` have no `GoalType` branch, and `LoadOwnedActiveDayForMutationAsync`'s guard is scoped purely by plan ownership and `TrainingPlanStatus`, not `GoalType`. The new guards therefore apply identically and correctly to Habit plans (a Habit day cannot be double-completed or silently reverted from Completed to Missed either) — this is a genuine, deliberate widening of the fix's benefit, not a risk, since nothing in the fix depends on `GoalType`-specific state. Not independently re-tested with a dedicated Habit-mode HTTP test in this phase (the fixture-creation helper used `GoalType.Race` throughout, per the governing prompt's own distance-plan preference) — treated as `STRUCTURALLY_PROVEN` by code-path inspection, named honestly rather than silently assumed.

## 44. Mutation matrix (post-fix)

| CurrentPlanState | CurrentDayState | RequestedMutation | Allowed? | Idempotent? | HTTP/ApplicationResult | FinalDayState | LogDelta | Authority/Reason |
|---|---|---|---|---|---|---|---|---|
| Active | Planned | Complete | Yes | N/A (first call) | 200 | Completed | +1 WorkoutLog, +1 PlanEvent | Normal flow |
| Active | Planned | CreateNotToday | Yes | N/A | 200 | Planned (decision Pending) | +1 NotTodayDecision | Normal flow |
| Active | Completed | Complete (same values) | Yes | Yes | 200 | Completed | +0 | Idempotent replay |
| Active | Completed | Complete (different values) | No | N/A | 409 `TRAINING_DAY_COMPLETION_CONFLICT` | Completed (unchanged) | +0 | §10 |
| Active | Completed | CreateNotToday | No | N/A | 409 `TRAINING_DAY_TRANSITION_CONFLICT` | Completed (unchanged) | +0 | §12a |
| Active | Completed (decision created before completion) | ConfirmNotToday | No | N/A | 409 `TRAINING_DAY_TRANSITION_CONFLICT` | Completed (unchanged) | +0 (decision→Cancelled) | §12b |
| Active | Missed | NotToday (retry, resolved) | Yes | Yes | 200 (returns resolved decision) | Missed | +0 | §11a |
| Active | Missed | Complete | No | N/A | 409 `TRAINING_DAY_TRANSITION_CONFLICT` | Missed (unchanged) | +0 | §13 |
| Cancelled | Planned | Complete | No | N/A | 404 `NOT_FOUND` | Planned (unchanged) | +0 | §15 |
| Cancelled | Planned | CreateNotToday | No | N/A | 404 `NOT_FOUND` | Planned (unchanged) | +0 | §15 |

## 45. Concurrency matrix

| Rows | InitialState | RequestA | RequestB | PossibleControlledResponses | FinalState | LogCount | Invariant | PostgresProven? |
|---|---|---|---|---|---|---|---|---|
| Complete+Complete (same values) | Planned | Complete(5.0,30) | Complete(5.0,30) | 200/200 | Completed | 1 WorkoutLog, 1 PlanEvent | One logical transition | Yes (`ConcurrentIdenticalCompletes_...`) |
| Complete+Complete (different values) | Planned | Complete(5.0,30) | Complete(8.0,50) | one 200/one 409 | Completed, value = whichever won | 1 WorkoutLog | No last-write-wins corruption | Yes (`ConcurrentConflictingCompletes_...`) |
| Complete+NotToday (decision pre-existing) | Planned (+Pending decision) | Complete | ConfirmNotToday | (200,409) or (409,200) | Completed+Cancelled, or Missed+Confirmed | matches winning branch exactly | Exactly one deterministic protected final state | Yes (`ConcurrentCompleteVsConfirmNotToday_...`) |

## 46. Finding closure table

| FindingId | OldReproduction | Fix | GuardLayer | ConcurrencyProtection | Tests | FinalStatus |
|---|---|---|---|---|---|---|
| V1HARDEN0-002 | Repeated Complete overwrites actuals, duplicates WorkoutLog/PlanEvent | Status guard + idempotent-replay check in `CompleteWorkoutAsync` | Application (`LoadOwnedActiveDayForMutationAsync` + inline status branch) | Plan-row `FOR UPDATE` lock + `xmin` token | `CompleteRetry_*`, `Concurrent*Completes_*` | **CLOSED** |
| V1HARDEN0-003 | Repeated CreateNotToday creates unbounded duplicate Pending decisions | Existing-Pending lookup before insert in `CreateNotTodayDecisionAsync` | Application, same loader | Plan-row lock serializes concurrent creates | `NotTodayRetry_Pending_DoesNotCreateDuplicateDecision` | **CLOSED** |
| V1HARDEN0-004 | A Completed day's status silently reverted to Missed by a later-confirmed NotToday decision | Reject at creation (`day.Status==Completed`) AND at confirm (`day.Status==Completed` re-checked under plan lock, decision cancelled) | Application, both call sites, same loader | Plan-row lock closes the create→complete→confirm race | `CompletedDay_CannotBeRevertedByNotToday_*` (both tests) | **CLOSED** |

All three closed with real-Postgres proof, per the governing prompt's "return CLOSED only with real DB proof" instruction.

## 47. Complete retry result

**`COMPLETE_MUTATION_IS_IDEMPOTENT_AND_DUPLICATE_LOG_SAFE`**

## 48. NotToday retry result

**`NOTTODAY_RETRY_IS_IDEMPOTENT_SUCCESS`** (both the Create-layer Pending dedupe and the Confirm-layer idempotent replay)

## 49. Completed protection result

**`COMPLETED_TRAINING_DAY_CANNOT_BE_REVERTED_BY_NOTTODAY`**

## 50. Concurrency result

**`TRAINING_DAY_MUTATIONS_ARE_CONCURRENCY_SAFE_WITH_ONE_LOGICAL_TRANSITION`**

## 51. Plan-state result

**`INACTIVE_PLAN_TRAINING_DAY_MUTATIONS_ARE_GUARDED`**

## 52. Read-consistency result

**`TRAINING_DAY_MUTATION_STATE_IS_CONSISTENT_ACROSS_HOME_CALENDAR_AND_DETAIL`** (structurally proven per §30-32 — no read-path file was touched, and every read queries the exact column this phase's mutations write, with no cache layer in between)

## 53. P1 closure result

**`V1_HARDEN_0_COMPLETE_NOTTODAY_P1_FINDINGS_CLOSED`**

## 54. PlanCatalog regression

Fresh run, this phase: **1510 / 1510 passed, 0 failed, 0 skipped** (8s) — identical to the V1-HARDEN.0A baseline. Zero semantic delta, as expected (no catalog file touched).

## 55. Flutter regression

Not run. No frontend file was modified by this phase (`git diff --stat`, §58, confirms zero `mobile/` delta) — per repo convention ("if frontend files change, run the full Flutter suite; if none change, still run relevant mutation UI tests if they exist"), and no Complete/NotToday-specific Flutter widget test exists to re-run independent of a frontend change. Baseline carried forward unchanged: 394/394 (V1-HARDEN.0A).

## 56. Backend full regression

A full, clean `dotnet test RunningApp.sln` run (real local Postgres-backed `RunningApp.IntegrationTests` included, no stray `dotnet`/`testhost` processes confirmed before starting) returned:

**5462 total / 5441 passed / 4 failed / 17 skipped (52m12s).**

Delta against the V1-HARDEN.0A/FIVE-K.1 baseline (5447/5426/4/17): **+15 / +15 / +0 / +0** — exactly this phase's own 15 new tests in `V1Harden1TrainingDayMutationTests.cs`. Zero other test's outcome changed.

## 57. Failure-identity comparison

All 4 failures confirmed byte-for-byte identical by fully-qualified name to the long-standing durable baseline V1-HARDEN.0A (§56) and FIVE-K.1 both already named and re-confirmed: `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13 and 14, both `Expected: OK, Actual: InternalServerError`), `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` (`Assert.NotNull() Failure`), and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` (`Expected: OK, Actual: InternalServerError`). **Zero new reproducible failure identity.** None of these four touch `TrainingDay`/`WorkoutLog`/`PlanEvent`/`NotTodayDecision` mutation or `AppDbContext`'s `xmin` configuration — they are pre-existing, already-disclosed, unrelated defects this phase did not cause and is not scoped to fix.

## 58. Production diff audit

Backend files changed: `backend/RunningApp.Application/Services/QueryAndMutationServices.cs` (the three mutation methods + one new shared loader), `backend/RunningApp.Application/Exceptions/AppExceptions.cs` (three new typed exceptions), `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs` (three new exception→409 mappings), `backend/RunningApp.Persistence/AppDbContext.cs` (one new `xmin` shadow-property concurrency-token configuration for `TrainingDay`), `backend/RunningApp.Api/Program.cs` (one `ConfigureWarnings` call suppressing EF's `PendingModelChangesWarning`, required because the `xmin` model-configuration change has no corresponding migration — see §59 below). Test files added: `backend/RunningApp.IntegrationTests/V1Harden1TrainingDayMutationTests.cs` (15 new tests, net-new file, zero modification to any existing test file). **No database migration added.** No file under `plan-catalog/`, `mobile/`, or any `RuntimeCatalog`/generation/onboarding/preview/confirm path was touched.

## 59. Remaining P0/P1

None known. V1-HARDEN.0's single P0 was closed by V1-HARDEN.0A; its two P1s (`V1HARDEN0-002`/`V1HARDEN0-004`, plus the adjacent `V1HARDEN0-003`) are closed by this phase (§46).

## 60. Remaining P2/P3

Carried forward, unfixed, exactly as before: exception-message leakage for a handful of non-500 catalog-lifecycle exceptions and untyped `ApiException`s (`V1HARDEN0-006`); no cross-field `RecentWeeklyVolumeKm`/`RecentLongestRunKm` validation (`V1HARDEN0-005`); no metrics/trace pipeline beyond structured logging (`V1HARDEN0-007`); the nine audit gaps V1-HARDEN.0 named (live DST-boundary proof, live catalog-retirement proof, etc.) remain open, out of this phase's scope.

## 61. Post-phase launch state

**`APPSEL_V1_HAS_NO_REMAINING_P0_P1_LAUNCH_BLOCKERS`** — not `APPSEL_V1_LAUNCH_READY`, since the P2/P3 items in §60 have not been separately closed or formally accepted as launch-acceptable.

## 62. Next phase

**V1-HARDEN.2** — public input cross-validation, boundary validation, and safe error-contract closure, covering exactly V1-HARDEN.0's remaining P2 findings (`V1HARDEN0-005`, `V1HARDEN0-006`) plus the named date/race-date/preferred-day boundary-sweep audit gaps. Not started here.

---

## Final classification

**`V1_TRAINING_DAY_MUTATION_P1_HARDENING_COMPLETE`**

Every item in the governing prompt's final checklist within this phase's control was verified true: the actual Core status model was read from source (three live states: Planned/Completed/Missed, §5); Complete and NotToday share one authoritative loader/guard (`LoadOwnedActiveDayForMutationAsync`, §9); valid first Complete/NotToday calls still succeed (§28-29); Complete retry is safe (§10, proven); NotToday retry is safe at both the Create and Confirm layers (§11, proven); duplicate logical mutation logs are prevented by gating the write, not deduplicating the read (§17-18); a Completed day cannot be silently reverted via NotToday, closed at both the creation site and the one true authoritative confirm-time mutation site (§12); NotToday→Complete is explicitly source-governed, not a product-decision punt (§8, §13); inactive/cancelled-plan mutations cannot silently modify historical state (§15-16, proven); ownership/authorization is unchanged (§27); repeated calls do not create duplicate logical side effects (§10-11, proven with log-count assertions, §33); concurrent identical mutations are safe (§21, real-Postgres-proven); concurrent conflicting Complete/NotToday cannot corrupt state via last-write-wins (§21, §23, real-Postgres-proven); status update and mutation-history insert are transactionally coherent (§19); expected state conflicts map to typed 409s/404s, never an uncontrolled 500 (§25-26); real PostgreSQL proves every critical invariant named above, not mocks (§34); generated-plan content, onboarding, active-plan uniqueness, and the distance engine are all zero-delta (§39-42, confirmed by `git diff --stat` and a clean `PlanCatalog.Tests` re-run); Habit-mode behavior is not unintentionally broken — the fix is `GoalType`-agnostic by construction (§43); no unauthorized product semantics were invented — `NotToday→Complete`'s rejection is read directly from the domain's own pre-existing `CanMarkComplete`/`CanMarkNotToday` flag convention (§6, §8); no database migration was added, only a diagnostic `ConfigureWarnings` configuration change required by reusing Postgres's native `xmin` column exactly as `LongHorizonRollingPlanState` already does (§20, §58); the targeted suite (15/15) and `PlanCatalog.Tests` (1510/1510) both pass clean; the exact two V1-HARDEN.0 P1 findings (plus the adjacent P2, `V1HARDEN0-003`) are mapped to CLOSED with real-Postgres proof, not merely asserted (§46).
