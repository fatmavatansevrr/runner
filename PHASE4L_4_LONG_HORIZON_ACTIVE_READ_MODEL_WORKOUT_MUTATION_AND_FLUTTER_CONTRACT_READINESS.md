# Phase 4L.4 — Long-Horizon Active Read Model, Workout Mutation Integration and Flutter Contract Readiness

## 1. Executive result

`LONG_HORIZON_ACTIVE_READ_MODEL_AND_WORKOUT_MUTATION_INTEGRATION_COMPLETED`. Confirmed rolling plans have authenticated Home, Calendar, active-details, session-detail, completion and direct not-today backend behavior. Pending future work remains non-executable and numerically hidden.

## 2. Inherited Phase 4L.3 public state

Phase 4L.3 supplies dedicated 21–52-week preview/confirmation, a real user-owned `TrainingPlan` with `RollingLongHorizon`, one linked rolling aggregate, all structural weeks, and only the initial executable window. It previously deferred Home and Calendar with `LONG_HORIZON_READ_SURFACE_NOT_YET_SUPPORTED`.

## 3. Scope and exclusions

This pass adds backend reads and explicit outcome mutation only. It adds no Flutter code, background work, automatic activation, fake `TrainingWeek`/`TrainingDay`, schedule conversion, numeric/calendar/direction/target/Runway/Core/checkpoint formula change, or downward interpolation. No commit is made.

## 4. Existing read/mutation inspection

Static Home, Calendar, detail, completion, not-today and pending confirmation use `TrainingDay.Id` and the `TrainingPlan`→`TrainingWeek`→`TrainingDay` graph. Static Home uses UTC `DateTime.UtcNow.Date`; Calendar accepts `YYYY-MM`. Static completion writes `TrainingDay`, `WorkoutLog`, week volume and `PlanEvent`; static not-today creates a `TrainingDay`-backed decision. Rolling sessions already had stable IDs, role, workout identity, distance, AssignedDate, context sequence and provenance, but only a string Planned status and no actual-result fields. The existing checkpoint aggregator accepts terminal `TrainingDay` evidence rows.

## 5. Read-model architecture

The architecture is unified public route dispatch with a strategy-specific internal rolling provider. `QueryAndMutationServices` retains the existing static mapping and delegates rolling plans to `ILongHorizonActiveReadModelProvider`. Static Home remains its existing object and static Calendar its existing list, so static JSON is unchanged. Rolling results use explicit dedicated V1 DTOs. No controller branches on entities.

## 6. Public rolling session identity

`LongHorizonRollingSessionState.Id` is `SessionId`. It is durable, restart-stable, globally unique and unchanged by outcomes. Authorization traverses the active user-owned `TrainingPlan.LongHorizonRollingPlanStateId`. Pending structural work has no session row or public ID. No date/ordinal-derived or fake `TrainingDay` ID exists.

## 7. Home contract

`GET /api/v1/plans/active/home` returns the existing `HomeResponse` for static plans or `LongHorizonHomeResponse` V1 for rolling plans. Rolling output includes explicit strategy, plan/goal/horizon/current week and stage, current window, next Pending boundary, activated/terminal counts, readiness, today, next activated session, and the activated window’s sessions.

## 8. Home semantics

UTC application date matches static Home. An activated session assigned today is returned, including its terminal outcome when applicable. Otherwise `today_workout` is null and the next future activated Planned session may be returned. Pending work is never synthesized. Blocked and terminal states map to public readiness/messages. Reading Home has no write or activation side effect.

## 9. Calendar contract

`GET /api/v1/plans/active/calendar?month=YYYY-MM` returns the unchanged static list for static plans or `LongHorizonCalendarResponse` V1 for rolling plans. Rolling entries contain only durable activated/completed sessions in the requested month with `SessionId`, strategy, immutable AssignedDate, role/workout identity, approved planned distance, public outcome/actuals and provenance.

## 10. Calendar historical behavior

Entries are ordered by AssignedDate then session ordinal. Completed/NotToday entries remain on their original dates. Context refresh can only create/change future Pending authority and cannot rewrite persisted session history. Pending weeks emit neither fake events nor dates. Reads never activate.

## 11. Session-detail contract

`GET /api/v1/training-days/rolling/{sessionId}` returns `LongHorizonRollingSessionDetailResponse` V1. It includes plan/session/strategy/global week/phase/stage/date/role/workout identity/planned distance, nullable unsupported planned duration/pace/intensity, actual outcome fields, timestamps, mutation eligibility, public description and provenance.

## 12. Session authorization

The session must belong through its rolling week/aggregate to the authenticated user’s active `RollingLongHorizon` plan. Cross-user, unknown and cancelled-plan lookups are indistinguishable 404 responses. Pending structural weeks cannot resolve because they have no session rows. User identity is never accepted from the request.

## 13. Rolling outcome model

`LongHorizonRollingSessionOutcomeStatus` has `Planned`, `Completed` and `NotToday`. The existing `CompletionStatus` column remains the storage column and safely defaults to Planned. Additive fields are `CompletedAtUtc`, `ActualDistanceKm`, `ActualDurationMinutes`, `NotTodayReason`, `NotTodayRecordedAtUtc`, and optimistic-concurrency `OutcomeVersion`. One status owns the outcome, so Completed and NotToday cannot coexist.

## 14. Completion request

`POST /api/v1/training-days/rolling/{sessionId}/complete` accepts `LongHorizonCompleteSessionRequest`: `contract_version=1`, positive finite `actual_distance_km`, and positive `actual_duration_minutes`. It accepts no plan/user/planned/context/checkpoint/activation fields. Actual pace is derived for reads as minutes divided by kilometres.

## 15. Completion transaction

One PostgreSQL transaction loads the authenticated active rolling ownership, locks the `TrainingPlan` row against cancellation, verifies executable state, enforces replay/conflict rules, writes only actual/outcome fields, increments `OutcomeVersion`, atomically completes the week when every session is terminal, checks evidence availability, saves once and commits once. It never calls preview, confirmation, generation or continuation.

## 16. Completion idempotency

An exact distance/duration replay returns `IdempotentReplay` with the existing version. A materially different replay returns `LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT`. Planned distance, role, workout identity, provenance and AssignedDate are never overwritten.

## 17. Completion concurrency

`OutcomeVersion` is an EF concurrency token. Independent identical requests converge on one completion and one replay. Conflicting completion values or completion versus not-today produce one durable winner and a typed conflict/concurrency loser. Fresh reload is authoritative.

## 18. Not-today policy

Policy A is selected: direct durable NotToday on the rolling session. It is the closest existing product-level semantic that does not require a fake `TrainingDay` or a new strategy-aware pending-decision aggregate.

## 19. Not-today implementation

`POST /api/v1/training-days/rolling/{sessionId}/not-today` accepts V1 plus one of `fatigue`, `soreness`, `illness`, `schedule`, `weather`, or `other`. It uses the same owner/executable/transaction/concurrency rules as completion. Exact replay is idempotent; another reason or a completed outcome conflicts. No reschedule, deletion, replacement or activation occurs.

## 20. Missed policy

No Missed transition is introduced. The repository has no approved automatic/user-driven rolling Missed contract, and this pass adds no background/request-time scheduler. Explicit NotToday supplies the user-recorded non-completion outcome. A future Missed policy requires separate authority.

## 21. Week terminality

An activated week becomes `Completed` only when every persisted session is `Completed` or `NotToday`. A partial set remains `NumericActivated`; a session counts once because one row has one authoritative outcome. Pending future weeks are excluded. Completion timestamp and state update in the same transaction.

## 22. Checkpoint evidence integration

`LongHorizonRollingOutcomeEvidenceAdapter` maps durable rolling outcomes in memory to the existing `LongHorizonTrainingDayEvidenceRow` boundary: Completed maps to Completed with actual values; NotToday maps to Skipped without actual distance. This is an input adapter, not persisted fake `TrainingDay`. Existing aggregation, validated-load, adherence, long-run cap, evidence freshness and checkpoint formulas are unchanged.

## 23. Activation-trigger policy

Policy C is selected. Completion/Home expose `LongHorizonCheckpointReadiness`; they do not activate. No approved public continuation endpoint exists. Explicit server-owned next-window activation is deferred to the next backend activation phase or a separately authorized operation.

## 24. Home checkpoint readiness

The public enum is `CurrentWindowInProgress`, `CurrentWindowComplete`, `NextWindowActivationReady`, `ReassessmentRequired`, or `TerminalPlanComplete`. It is derived from durable status and contains no internal decision reason. Current implementation uses in-progress, next-ready, reassessment and terminal outputs; `CurrentWindowComplete` is reserved for a future explicit activation workflow boundary.

## 25. Calendar after outcome

Completed and NotToday states and actual values remain visible on the original AssignedDate. No replacement workout appears. Pending future sessions remain absent. Cancelled plans cease to be active Calendar results while their persisted history remains.

## 26. Detail after outcome

Detail preserves planned distance, AssignedDate, workout identity and public provenance, and adds the durable actual values/outcome/timestamp. Only actual/outcome fields mutate.

## 27. Active-plan details

`GET /api/v1/plans/active/details` branches by persisted strategy. Rolling details include explicit strategy, race/plan summary, total and completed weeks, current window, next Pending week, public readiness and all structural week summaries. Pending weeks set `NumericDetailsAvailable=false`; no session/numeric schedule is fabricated. Static-only added fields are omitted when null.

## 28. Cancellation compatibility

The existing cancel endpoint sets the real `TrainingPlan` to Cancelled and retains its linked rolling aggregate/history. Home/Calendar no longer find it active; detail/mutation become non-disclosing not-found. A row lock serializes cancellation with outcome mutation: either the outcome commits while active before cancellation, or cancellation wins and mutation is rejected.

## 29. Public error mapping

Mappings include `LONG_HORIZON_ACTIVE_PLAN_NOT_FOUND` (404), `LONG_HORIZON_ROLLING_SESSION_NOT_FOUND` (404), `LONG_HORIZON_ROLLING_SESSION_NOT_EXECUTABLE` (409), `LONG_HORIZON_ROLLING_SESSION_COMPLETION_CONFLICT` (409), `LONG_HORIZON_ROLLING_SESSION_OUTCOME_CONFLICT` (409), `LONG_HORIZON_ROLLING_MUTATION_CONCURRENCY_CONFLICT` (409), `LONG_HORIZON_ROLLING_MUTATION_VERSION_UNSUPPORTED` (422), and `LONG_HORIZON_READ_STATE_CORRUPT` (409). Cross-user uses not-found. Provider details remain sanitized.

## 30. Contract versioning

Rolling Home, Calendar, detail, completion and not-today contracts explicitly use version 1. Rolling active details carry explicit strategy. Unsupported mutation versions fail closed. Existing static contract types and versions remain unchanged.

## 31. Flutter contract readiness

Flutter will need a strategy-discriminated decoder for `static_complete` versus `rolling_long_horizon`; rolling Home and Calendar are objects rather than the static Calendar list. IDs are UUID strings, dates are ISO `YYYY-MM-DD`, timestamps ISO UTC, distance kilometres and duration minutes. Nullable planned duration/pace/intensity mean the current rolling persistence authority does not own those values. Loading, empty today, in-progress, reassessment, next-ready and terminal states are explicit. Completion/not-today retry is safe only for exact replay; conflicting replay must refresh and display the typed conflict. Existing static models remain valid. No Flutter code changed.

## 32. Swagger

Home and Calendar document both static and rolling success responses. Dedicated rolling detail/completion/not-today routes and V1 examples are present. Examples cover in-progress, session detail and mutation/readiness without internal persistence entities. Blocked/terminal states are enum-defined and documented; Pending numeric output is absent.

## 33. Observability

Structured logs cover rolling Home, Calendar, detail, completion requested/succeeded/replayed/conflict, not-today requested/succeeded, and evidence-ready week terminality. They contain scoped user/plan/session/week/outcome metadata only—not raw evidence, JSONB snapshots, contexts, tokens or provider details.

## 34. Migration

`20260805081427_Phase4L4RollingSessionOutcomes` is additive. It preserves `CompletionStatus`, adds safe Planned default plus actual/not-today/version fields, and creates `IX_LongHorizonRollingSessionStates_AssignedDate`. Down removes only these additions/default. Existing static rows/tables and rolling prescription rows are not rewritten. It was applied successfully to local PostgreSQL.

## 35. Authorization

Owner Home, Calendar, detail, completion and not-today traverse authenticated active ownership. Another user cannot infer session or plan existence. Request models contain no UserId or PlanId authority.

## 36. Concurrency and rollback

Optimistic session versioning provides one outcome. The TrainingPlan row lock orders cancellation. Test-only pre-commit failpoints before save and before commit prove no partial outcome/week state and successful corrected retry. A post-commit-before-ack failpoint proves exact replay recovers the committed outcome. Existing Phase 4L.2F/4L.2G infrastructure remains passing.

## 37. Static backward compatibility

Static Home object, Calendar list, TrainingDay detail/completion/not-today, pending confirmations, active details, race/core/runway confirmation and Habit flows remain on their existing code. Strategy dispatch is invisible to static clients. The full backend regression is authoritative.

## 38. Public leakage guards

Rolling DTOs contain no rolling entities, target lock, Runway prescription, Core context, evidence fingerprint, checkpoint record, idempotency key, xmin, failure seam, internal reason, or provider detail. Home/Calendar select only session rows belonging to activated/completed weeks. Pending weeks have no public session ID, AssignedDate or numeric output.

## 39. Governance

`TD-LONG-HORIZON-ACTIVE-READ-MODEL-WORKOUT-MUTATION-001` is `CLOSED`. Append-only updates were added to confirmation, public-preview, rolling-persistence, mixed-completion, full-lifecycle and volume-redesign records. Registry aggregate is 57 total, 14 OPEN, 43 CLOSED. Flutter and background/automatic activation are not closed.

## 40. Tests

The focused Phase 4L.4 plus Phase 4L.3 regression slice passed 37/37. The complete Long-Horizon slice passed 847/847. The full backend suite passed 3,063/3,063, and the full plan-catalog suite passed 1,249/1,249. Every run had zero failed and zero skipped tests. Coverage includes read routing, Home/Calendar/detail, no Pending leakage, outcomes, replay/conflict, concurrent writers, terminality/evidence, active details, cancellation, authorization, failpoint rollback, acknowledgement loss, Swagger and static regression.

## 41. Flutter/background status

Flutter is unchanged. No hosted service, timer, queue, scheduled task, request-time missed derivation, read-triggered mutation, or automatic activation was added.

## 42. Final classification

`LONG_HORIZON_ACTIVE_READ_MODEL_AND_WORKOUT_MUTATION_INTEGRATION_COMPLETED`.

## 43. Exact next phase

Recommended: **Phase 4L.5 — Long-Horizon Flutter Read, Workout Outcome and Rolling-State Integration**. It should consume these V1 contracts and must not silently introduce automatic next-window activation; any explicit activation endpoint remains separately authorized backend work.
