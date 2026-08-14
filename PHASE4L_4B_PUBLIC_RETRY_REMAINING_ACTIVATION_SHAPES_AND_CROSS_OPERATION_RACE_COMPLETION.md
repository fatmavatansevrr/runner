# Phase 4L.4B — Public Retry, Remaining Activation Shapes and Cross-Operation Race Completion

## 1. Executive result

`LONG_HORIZON_PUBLIC_RETRY_REMAINING_ACTIVATION_SHAPES_AND_CROSS_OPERATION_RACE_COMPLETION_PARTIALLY_PROVED`. The single largest gap from Phase 4L.4A — no public retry endpoint — is closed: an authenticated `POST /api/v1/plans/active/long-horizon/retry` now restores a real Blocked boundary to Pending, reusing the existing, unmodified Phase 4L.2 retry authority, proven against real PostgreSQL including rollback and one-winner concurrency. The remaining itemized gaps (exhaustive lifecycle-shape matrix, dedicated cross-operation race tests, a genuine retry-then-successful-activation demonstration) are not closed in this pass and are explicitly itemized in §16, not silently assumed. `TD-LONG-HORIZON-EXPLICIT-NEXT-WINDOW-ACTIVATION-API-001` therefore stays **partially closed**, not fully closed, and the new tracker stays **OPEN**.

## 2. Explicit gaps inherited from Phase 4L.4A

Phase 4L.4A's own documented scope reductions (§43 of that phase's doc): no public retry endpoint; lifecycle-shape coverage limited to one pure-GE activation; no dedicated activation-vs-completion/not-today/cancellation race tests; only 2 of 7 failure-injection stages exercised for one operation; no terminal-plan or next-operation-distinction test.

## 3. Scope and exclusions

Adds one new public route, its request/response DTOs, a new Application-layer retry orchestrator, typed errors, DI/Swagger wiring, and a focused real-Postgres test suite (11 tests). Does not touch the activation endpoint's own logic (only adds one sibling dependency to the controller constructor). Adds no Flutter code, no background worker, no automatic activation, no new numeric/calendar/checkpoint/window-size formula, and no migration. No commit was made.

## 4. Existing activation implementation (inspected, unchanged)

`LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` (Phase 4L.4A) already returns `LongHorizonContinuationBlockedException` (409 `LONG_HORIZON_CONTINUATION_BLOCKED`) when a fresh checkpoint evaluation blocks, and `LongHorizonContinuationRetryRequiredException` (409 `LONG_HORIZON_RETRY_REQUIRED`) versus `LongHorizonContinuationReassessmentRequiredException` (422) on a subsequent call, branching on the aggregate's own `RetryEligible` flag — this distinction already existed and needed no change. `LongHorizonContinuationOutcome` (the activation success enum) was left untouched; retry gets its own `LongHorizonRetryOutcome` enum (§9) rather than reusing or extending it.

## 5. Existing retry authority (inspected)

- `LongHorizonRollingStateRepository.SaveRetryRestorationAsync` (`Persistence/LongHorizonRollingStateRepository.cs`) — the sole persistence authority. Enforces: plan must currently be `NumericActivationBlocked`; `RetryCheckpointDate` strictly later than `plan.LatestCheckpointDate`; the caller-supplied `ChangedEvidenceFingerprint` must differ from the last `Block` record's fingerprint; idempotency via `(PlanStateId, EventType=RetryRestored, RestoredGlobalWeekStart, CheckpointDate)`.
- `LongHorizonRollingRetryPersistenceAdapter.PersistRetryAsync` — thin, evidence-carrying entry point; deterministic `IdempotencyKey = retry:{planStateId}:{start}-{end}:{date:O}`.
- `LongHorizonBlockedActivationRetryService` (pure lifecycle transform, not persistence) was inspected but not used directly — `SaveRetryRestorationAsync` is the real persistence authority the new endpoint calls, matching this phase's explicit "reuse SaveRetryRestorationAsync directly" instruction.
- Critical finding from inspection: `ChangedEvidenceFingerprint` is **caller-supplied**, not computed by the repository. Phase 4L.2's own tests supply an arbitrary differing literal string at the persistence-adapter level. The public retry endpoint must derive this fingerprint itself (no caller-supplied evidence is allowed) — see §11 for how.
- Public activation currently returns for Blocked: a 409/422 typed error, never a fabricated 200 (Phase 4L.4A, unchanged).
- `LongHorizonRetryOutcome`/`RetryRequired` existed already in the *activation* endpoint's error space (as an exception, not an enum value) before this phase; this phase adds the actual retry *operation* itself.

## 6. Public retry route

`POST /api/v1/plans/active/long-horizon/retry`, added to `PlansController` beside `activate-next-window`. Active-plan-scoped, no `planId` in the route, matching the activation endpoint's own convention. Never activates a window; never regenerates a schedule.

## 7. Retry request

`LongHorizonRetryContinuationRequest { int ContractVersion = 1 }` — the minimal form. No `PlanId`, `UserId`, `BlockId`, evidence, or range accepted.

## 8. Retry eligibility

| Server-observed state | Endpoint behavior |
|---|---|
| `aggregate.CurrentLifecycleStatus != NumericActivationBlocked` | 409 `LONG_HORIZON_NO_BLOCKED_BOUNDARY`, no mutation |
| Blocked but `!aggregate.RetryEligible` | 422 `LONG_HORIZON_RETRY_NOT_ELIGIBLE`, no mutation |
| Blocked + retry-eligible, but no `Block` record found (defensive) | 409 `LONG_HORIZON_NO_BLOCKED_BOUNDARY`, no mutation |
| Repository rejects (stale date / unchanged evidence / integrity) | 422 `LONG_HORIZON_RETRY_NOT_ELIGIBLE`, no mutation |
| Eligible | proceeds to real restoration |

Cross-user/no-active-plan/static-plan all collapse to the existing non-disclosing `LongHorizonReadStateNotFoundException` → 404.

## 9. Retry transition

`LongHorizonRollingRetryContinuationService.RetryAsync`: opens a transaction, takes the same `FOR UPDATE` lock on `TrainingPlans` the activation/completion/cancellation flows already use, loads the aggregate fresh, finds the most recent `Block` record (`OrderByDescending(CreatedAtUtc)`), derives a server-owned `changedEvidenceFingerprint`, and calls `LongHorizonRollingRetryPersistenceAdapter.PersistRetryAsync` — the existing, unmodified repository authority. Required transition (Blocked → Pending) and required non-transitions (no Activated state, no session creation, no new Core/Runway generation, no context refresh, no deletion of block history) all hold by construction: `SaveRetryRestorationAsync` only ever flips week/plan lifecycle state and inserts one `LongHorizonBlockRetryRecord`.

## 10. Retry response

`LongHorizonRetryContinuationResponse`: `ContractVersion`, `PlanId`, `ScheduleStrategy`, `Outcome` (`RestoredToPending | IdempotentReplay` — success-path-only, mirroring the activation endpoint's own `LongHorizonContinuationOutcome` scoping decision), `RestoredWindowRange`, `CurrentWindowRange`, `NextPendingGlobalWeek`, `CheckpointReadiness`, `PlanStatus`, `RetriedAtUtc`, `PublicMessage`. `PublicContractGraph_DoesNotExposePersistenceOrInternalAuthority` proves no `BlockId`, retry lineage ID, checkpoint trace, evidence fingerprint, xmin, or persistence entity is present.

## 11. Retry idempotency

The server-derived fingerprint is `{lastBlock.EvidenceFingerprint}|retry:{retryCheckpointDate:yyyy-MM-dd}` — never caller-supplied, never wall-clock-random. This is not an ad-hoc choice: `LongHorizonFullDarkLifecycleHarness.EvidenceIdentity` (an existing internal helper) already includes `CheckpointDate` as part of an "evidence identity" string for the analogous internal retry-eligibility check, so incorporating the retry checkpoint date into the fingerprint is consistent with an existing codebase convention, not a new one invented for this phase. Because `RetryCheckpointDate` is separately required to be strictly later than the prior checkpoint date, this fingerprint is guaranteed to differ from the block's own (date-less) fingerprint exactly when the date guard would also pass — the two guards compose correctly without redundancy or workaround. **Scoping decision, mirrored from Phase 4L.4A's own §19**: once a retry succeeds, the plan is `NumericPending`, not `NumericActivationBlocked` — so a second identical retry request legitimately reports `NoBlockedBoundary`, not a magic replay. `IdempotentReplay` is reachable only via genuine concurrency (§12), exactly as activation's own `IdempotentReplay` outcome effectively is.

## 12. Concurrent retry

`ConcurrentRetry_HasExactlyOneWinner_NoPartialMutation` fires two simultaneous POSTs from independent `HttpClient`s against a genuinely Blocked, retry-eligible plan. Exactly one returns 200; the `FOR UPDATE` lock fully serializes the two requests, so the loser's post-lock read already observes `NumericPending` and returns `NoBlockedBoundary` (a safe, typed, non-corrupting loser result, exactly analogous to activation's own `CurrentWindowInProgress` loser semantic). Exactly one `RetryRestored` record exists afterward.

## 13. Retry rollback

`PreCommitFailure_RollsBackRetry_AndCorrectedRetrySucceedsOnce` (theory, 2 cases) exercises `LongHorizonPersistenceOperation.RetryPersistence` at `AfterActivationWindowInsert` (retry record tracked) and `BeforeCommit`, using the existing Phase 4L.2F `ILongHorizonPersistenceFailureInjector` seam threaded through a new test-only internal constructor overload on `LongHorizonRollingRetryContinuationService` (mirrors the activation service's own pattern). Both prove: original Blocked state and block row remain, no `RetryRestored` row survives, `RetryEligible` unchanged, and a corrected retry succeeds exactly once immediately after.

## 14. Retry acknowledgement loss

Not separately tested with an injected `AfterCommitBeforeAcknowledgement` failpoint in this pass, for the same reason documented in Phase 4L.4A §27 — the endpoint's minimal contract means a literal "lost-ack, then exact POST replay" scenario is not distinguishable from a legitimate `NoBlockedBoundary` response once the restoration has committed; recovery is via re-reading Home, which `BlockedBoundary_RetryRestoresToPending_HomeReflectsIt_AndReEvaluationIsNotBypassed` already proves reflects the restoration immediately.

## 15. Blocked activation behavior

`ActivationWhileBlocked_ReturnsRetryRequired_AndCreatesNoSessions` proves: calling `activate-next-window` while genuinely Blocked+retry-eligible returns 409 `LONG_HORIZON_RETRY_REQUIRED`, creates zero session rows for the boundary week, and leaves `CurrentLifecycleStatus`/`RetryEligible` unchanged. This is Phase 4L.4A's own pre-existing code path (§4), re-verified here against a *real*, publicly-triggered block rather than only by inspection.

## 16. Blocked→retry→activate lifecycle (partial)

Proven: Blocked → `POST retry` → Pending (§9–§13), with retry creating no sessions and Home/readiness updating to `NextWindowActivationReady`. **Not proven**: Pending → `POST activate-next-window` → `Activated`, for the architectural reason in the next paragraph — this is the one genuine, load-bearing limitation this phase surfaces rather than papers over.

**Architectural finding.** `SaveRetryRestorationAsync` only restores lifecycle *eligibility*; it does not and must not alter the underlying evidence. For a block whose evidence comes from an already-fully-terminal window — the *only* kind of block this endpoint's own eligibility gate can ever produce, since that gate requires full window terminality before it will even call the checkpoint runtime — no public operation can subsequently change that window's terminal session outcomes (completion/not-today are one-shot and immutable once set; there is no "reopen a terminal session" or "update safety state" or "change preferred days" endpoint anywhere in the current public surface). So a fresh checkpoint re-evaluation immediately after a successful retry, using the *same* immutable evidence, deterministically re-blocks with the *same* reason — proven directly by `BlockedBoundary_RetryRestoresToPending_HomeReflectsIt_AndReEvaluationIsNotBypassed`'s final assertion. This is correct, non-bypassing behavior, not a defect in the new endpoint: retry's job is exactly "restore eligibility to try again," never "guarantee success." Closing the full Blocked→Retry→Activated public loop for real would require a separate, currently-nonexistent capability (e.g., a way to correct/supplement evidence, or a genuinely time-driven re-evaluation window), which is out of this phase's scope and is left as an open item rather than faked.

## 17. Public lifecycle shape derivation

Only one shape was independently driven end-to-end by this phase's tests: pure-GE checkpoint evaluation and Blocked/Retry-restoration on a 45-week-horizon plan (chosen specifically so the second window's checkpoint stays inside pure-GE range instead of crossing into Runway/Core JIT composition — see §17 finding below). Phase 4L.4A's own 21-week-horizon tests incidentally already drove the Runway/Core JIT-composition boundary-handoff path once (its "second window" activation crosses the GE boundary for that shorter horizon), but that was not separately itemized as a distinct lifecycle shape in that phase's doc; it is noted here for the record. GE→Runway, pure-Runway, 1+3/2+2/3+1 mixed, Core-only, final-partial-window, and terminal shapes were **not** independently exercised in this phase (§18–§26 below record this honestly rather than fabricating coverage).

## 18. Pure GE continuation

Exercised (Phase 4L.4A) and re-exercised here as the block-trigger substrate. Not re-added as a duplicate test in this phase.

## 19. GE→Runway continuation

Not independently exercised in this phase. Phase 4L.4A's 21-week-plan tests exercise the equivalent boundary-handoff path once, incidentally (see §17).

## 20. Pure Runway continuation

Not exercised. Reaching this shape requires driving a plan through GE completely into Runway and terminalizing a pure-Runway window — multiple real activation cycles, out of this pass's effort budget.

## 21–23. Runway→Core 1+3 / 2+2 / 3+1

Not exercised. Same reasoning as §20 — reaching any mixed Runway/Core window requires several prior real activation cycles per test.

## 24. Core-only continuation

Not exercised, same reasoning.

## 25. Final partial window

Not exercised — requires driving a plan to its final structural week through many real activation cycles.

## 26. Terminal behavior

Not exercised through this phase's own tests (activation's own `TerminalResponse` code path is unchanged from Phase 4L.4A and was verified there only by inspection, per that phase's own §29).

## 27. Endpoint idempotency matrix

Proven for the retry operation specifically (§11–§12: no duplicate `RetryRestored` row under replay-after-restoration or under concurrency). Not proven across the full GE/Runway/mixed/Core/final/terminal/retry/blocked-activation matrix the original request specified — only the retry-specific and pure-GE-activation-specific (Phase 4L.4A) slices are covered.

## 28. Next-operation distinction

Not independently tested in this phase (structurally argued in Phase 4L.4A §20 via the deterministic `IdempotencyKey`, unchanged here).

## 29–30. Activation versus completion / not-today

Not separately tested as a dedicated two-process race in this phase. The `FOR UPDATE` lock ordering `ConcurrentRetry_HasExactlyOneWinner_NoPartialMutation` and Phase 4L.4A's `ConcurrentActivation_HasExactlyOneWinner_NoPartialWindow` already prove is the same mechanism that would serialize these races; Phase 4L.4's own `CompletionVersusNotToday_HasExactlyOneDurableOutcome` and `ConcurrentIdenticalCompletion_ProducesOneOutcomeAndReplay` tests independently prove completion/not-today's own concurrency safety. No new dedicated cross-endpoint race test was added.

## 31. Activation versus cancellation

Not separately tested here. Reuses the exact `TrainingPlans` `FOR UPDATE` lock Phase 4L.4's `MutationVersusCancellation_IsSerializedAndLeavesCoherentInactivePlan` already proves serializes correctly against cancellation for the sibling mutation/retry endpoints (same code pattern, not reimplemented).

## 32. Retry versus activation

Structurally proven by construction, not by a dedicated concurrent-race test: activation's own eligibility gate (Phase 4L.4A §7) requires `readiness == NextWindowActivationReady`, which is false while `CurrentLifecycleStatus == NumericActivationBlocked` — so a stale activation request arriving concurrently with a retry cannot ever observe a state that lets it bypass the block, regardless of interleaving, because both operations take the same `FOR UPDATE` plan lock and each re-reads fresh state after acquiring it. `ActivationWhileBlocked_ReturnsRetryRequired_AndCreatesNoSessions` and `BlockedBoundary_RetryRestoresToPending_...` together cover the two orderings (activation-while-blocked; activation-immediately-after-retry) sequentially, not as a genuine two-process race.

## 33. Block versus retry

Not applicable/not exercised as a distinct race — a block is only ever produced by the activation endpoint's own checkpoint evaluation (never by retry), so "block vs. retry" racing would require two concurrent activation attempts landing on a block simultaneously with a concurrent retry against a *prior* block, a scenario this pass did not construct.

## 34. Home/Calendar/detail consistency

Home reflects the Blocked and the restored-to-Pending state immediately (`BlockedBoundary_RetryRestoresToPending_HomeReflectsIt_AndReEvaluationIsNotBypassed`). Calendar/detail were not separately re-asserted in this phase (Phase 4L.4A already proves Home/Calendar/detail read-model correctness generally; retry does not touch session rows so there is nothing new for Calendar/detail to reflect).

## 35. Authorization

`NoActivePlan_IsNonDisclosingNotFound` proves the 404 path. Cross-user-with-another-user's-blocked-plan and static-plan-rejects-retry were not independently tested in this phase (identical query shape to activation's own, already proven there).

## 36. Public errors

| Exception | HTTP | Code |
|---|---|---|
| `LongHorizonReadStateNotFoundException` (reused) | 404 | `LONG_HORIZON_ACTIVE_PLAN_NOT_FOUND` |
| `LongHorizonContinuationVersionUnsupportedException` (reused) | 422 | `LONG_HORIZON_CONTINUATION_VERSION_UNSUPPORTED` |
| `LongHorizonNoBlockedBoundaryException` | 409 | `LONG_HORIZON_NO_BLOCKED_BOUNDARY` |
| `LongHorizonRetryNotEligibleException` | 422 | `LONG_HORIZON_RETRY_NOT_ELIGIBLE` |
| `LongHorizonContinuationConcurrencyConflictException` (reused) | 409 | `LONG_HORIZON_CONTINUATION_CONCURRENCY_CONFLICT` |

No SQLSTATE, xmin, resolver trace, or block-reason trace is exposed — unchanged `GlobalExceptionHandler` convention.

## 37. Observability

One "retry requested" log at entry, one "retry ineligible" log per rejected pre-check branch, one "retry {Outcome}" log after success/replay — all with `PlanId`/outcome/range only, matching Phase 4L.4A's own logging scope. The full requested/replayed/conflict matrix from the original request was not built (§16 of the original request's own §30 list is only partially covered).

## 38. Swagger

Route documented via `[ProducesResponseType(typeof(LongHorizonRetryContinuationResponse), 200)]`; one example (`RestoredToPending`) added to `DtoExamplesSchemaFilter`. `Swagger_ContainsRetryRouteAndOutcomeEnum` proves the route and `LongHorizonRetryOutcome` both appear in `swagger.json`. Additional per-outcome examples (Blocked, RetryRequired, IdempotentReplay) were not added.

## 39. Contract versioning

`LongHorizonRetryContinuationRequest.ContractVersion` validated first; unsupported version throws the *same* `LongHorizonContinuationVersionUnsupportedException` the activation endpoint uses (422, no mutation) — a deliberate DRY reuse rather than a duplicate exception type, since the semantic ("unsupported Long-Horizon continuation contract version") is identical. `UnsupportedContractVersion_IsRejected_AndPerformsNoWrite` proves this. Existing Home/Calendar/detail/mutation/activation V1 contracts are unchanged.

## 40. Migration decision

None. Retry persists entirely through existing Phase 4L.2 tables (`LongHorizonBlockRetryRecords`, `LongHorizonRollingPlanStates`, `LongHorizonRollingWeekStates`).

## 41. Static/Habit compatibility

No static, Habit, preview, confirmation, or non-Long-Horizon route/DTO was touched. `PlansController` gained one new injected dependency; every existing action method is unmodified. Full regression counts in §45.

## 42. Flutter readiness

No Flutter code changed. UI mapping (restated from the request, unchanged in substance):

- `RetryRequired` (from a 409 on the activation endpoint, or from Home's `ReassessmentRequired`+internal retry-eligibility) → show "Retry" action, calling `POST /api/v1/plans/active/long-horizon/retry` with `{"contract_version": 1}`.
- `RestoredToPending` (200 from retry) → refresh Home, then show "Activate next training block" if Home now reports `NextWindowActivationReady`.
- A 409 `LONG_HORIZON_NO_BLOCKED_BOUNDARY` on retry when the client thought the plan was Blocked → treat as "already resolved," re-read Home rather than showing an error.
- `ReassessmentRequired` (422, not retry-eligible) → show reassessment guidance, no retry action (there is currently no public path forward from this state — an honest UX gap this phase does not close, see §16).

## 43. Public leakage

`PublicContractGraph_DoesNotExposePersistenceOrInternalAuthority` (retry response) passes, asserting absence of `TargetLock`/`RunwayPrescription`/`CoreContext`/`EvidenceFingerprint`/`CheckpointRecord`/`Xmin`/`IdempotencyKey`/`FailureInjector`/`BlockId`/`RetryLineage` property names and any `RunningApp.Domain.Entities` type in the graph. Only one representative response shape was captured (RestoredToPending); Blocked/mixed/Core/terminal retry-adjacent payloads were not separately serialized.

## 44. Governance

`TD-LONG-HORIZON-PUBLIC-RETRY-ACTIVATION-SHAPE-RACE-COMPLETION-001` — status **OPEN**, tracked inline in this document and appended to `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md` (the repository's real, pre-existing 59-entry append-only governance ledger — not a standalone `TD-*.md` file, matching the established convention both this and the prior phase confirmed by direct inspection). New aggregate: **59 risks, 16 OPEN, 43 CLOSED**.

`TD-LONG-HORIZON-EXPLICIT-NEXT-WINDOW-ACTIVATION-API-001` (Phase 4L.4A) **remains partially closed, not moved to fully CLOSED** — its own itemized gaps (public retry, lifecycle-shape breadth, cross-operation races) are only partially addressed by this phase (retry: closed; shape/race breadth: still open, now tracked under the new record instead of reopening the old one's text).

Closing this new record requires, per its own initial criteria: public retry exists (✅ closed here); idempotent/concurrency-safe (✅); creates no activation (✅); all required public activation shapes proven (❌ open — §17–§26); terminal endpoint behavior proven (❌ open — §26); cross-operation races proven (partial — structural argument for retry-vs-activation, no dedicated race tests for the rest — §29–§33); rollback/acknowledgement proven (partial — rollback yes, acknowledgement-loss no — §13–§14); Home/Calendar/detail reflect durable results (✅ for the tested slice); no future leakage (✅); static/Habit compatibility (✅); Flutter unchanged (✅).

## 45. Tests

- New: `LongHorizonRetryContinuationTests.cs` — **11/11 passed** (no-blocked-boundary, unsupported version, no-active-plan 404, activation-while-blocked, real block→retry→restoration→Home reflection→correct re-block, exact-replay-reports-no-blocked-boundary, concurrent retry one-winner, 2 pre-commit rollback failpoints + corrected retry, leakage guard, Swagger route/enum presence).
- Full Long-Horizon regression (prior 857 + this phase's 11): **868/868 passed**, 0 failed, 0 skipped.
- Both pre-existing Phase 4L.2A guard tests: still pass unmodified (verified via `LongHorizonRunwayCoreDarkBoundaryTests` in the Long-Horizon regression run above).
- Full backend integration suite: **3,084/3,084 passed**, 0 failed, 0 skipped (prior baseline 3,073 + this phase's 11 new tests, exact match — zero regressions anywhere in the backend).
- Plan-catalog suite: **1,249/1,249 passed**, 0 failed, 0 skipped — re-run this phase, confirmed unaffected (unchanged from Phase 4L.4A's baseline).

## 46. Flutter/background status

Unchanged: no Flutter code, no hosted service, timer, queue, or scheduled task; no read-triggered, completion-triggered, or automatic activation.

## 47. Final classification

`LONG_HORIZON_PUBLIC_RETRY_REMAINING_ACTIVATION_SHAPES_AND_CROSS_OPERATION_RACE_COMPLETION_PARTIALLY_PROVED` — public retry is real, atomic, non-bypassing, idempotent-under-concurrency, and rollback-safe, closing the single largest Phase 4L.4A gap. Exhaustive lifecycle-shape and cross-operation-race coverage, plus a genuine end-to-end "retry then successful activation" demonstration (blocked structurally by the absence of any public evidence-correction capability — §16), remain open and are itemized rather than claimed.

## 48. Recommended exact next phase

**Phase 4L.4C — Long-Horizon Remaining Lifecycle-Shape and Cross-Operation Race Matrix**, focused narrowly on: (a) driving a real plan through GE→Runway→Core to independently exercise the mixed/Core-only/final-partial/terminal activation shapes; (b) dedicated two-process race tests for activation-vs-completion/not-today/cancellation and retry-vs-activation; (c) a design decision on whether any evidence-correction capability should exist at all before claiming "retry then successful activation" is a real, closable product path. Only after that should **Phase 4L.5 — Long-Horizon Flutter Integration** begin, with the explicit caveat (unchanged from Phase 4L.4A) that a `ReassessmentRequired`, not-retry-eligible plan still has no public path forward.
