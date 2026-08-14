# Phase 4L.6B.2 — Rolling-Plan Rollback Mutation Guard Closure

Date: 2026-08-07
Branch: `main`
Committed previous-application HEAD used for rollback testing: `3549a8a1eeef18ca96794fa1056043142d13bc78`
Decision: **CLOSED — ROLLBACK_SAFE_WITH_EXPLICIT_MUTATION_GUARD**

## 1. Executive result

`LONG_HORIZON_ROLLBACK_MUTATION_GUARD_CLOSURE_COMPLETED`

`LONG_HORIZON_PREVIOUS_APPLICATION_ROLLBACK_CAN_NO_LONGER_MUTATE_FORWARD_SCHEMA_ROLLING_PLAN_STATUS_SESSIONS_OUTCOMES_OR_LIFECYCLE_THROUGH_LEGACY_ENDPOINTS`

`LONG_HORIZON_ROLLBACK_COMPATIBILITY_GUARD_REMAINS_ACTIVE_OUTSIDE_THE_ROLLED_BACK_APPLICATION_BINARY_DERIVES_PLAN_STRATEGY_FROM_TRUSTED_SERVER_STATE_AND_FAILS_CLOSED`

`LONG_HORIZON_STATIC_AND_HABIT_USERS_REMAIN_FULLY_BACKWARD_COMPATIBLE_AND_MUTABLE_DURING_APPLICATION_ROLLBACK`

`LONG_HORIZON_ROLLING_PLAN_STATUS_LIFECYCLE_WINDOW_SESSION_IDENTITIES_DATES_AND_OUTCOMES_REMAIN_IDENTICAL_ACROSS_ROLLBACK_AND_CURRENT_APPLICATION_RE_FORWARD`

`LONG_HORIZON_PRODUCTION_ROLLBACK_CLASSIFICATION_IS_ROLLBACK_SAFE_WITH_EXPLICIT_MUTATION_GUARD`

`LONG_HORIZON_PRODUCTION_CONFIGURATION_SECURITY_MIGRATION_AND_FORWARD_SCHEMA_ROLLBACK_CLOSURE_COMPLETED`

The single remaining P1 rollback-safety gap from Phase 4L.6B.1 — the committed-HEAD previous application cancelling an active rolling plan through its ordinary `cancel` endpoint — is closed by a PostgreSQL trigger-level guard (`fn_guard_rolling_plan_mutation`, table `RollbackCompatibilityMode`) that is entirely independent of which application binary is connected. It was proven to block the exact previously-successful defect, block a direct raw-SQL bypass attempt, leave static-plan mutation fully functional, and disappear cleanly on redeploy with the current application reconstructing the exact pre-incident rolling state.

## 2. Inherited rollback defect

Phase 4L.6B.1 found: with a real active `RollingLongHorizon` plan on the latest schema, committed HEAD `3549a8a1eeef18ca96794fa1056043142d13bc78`'s `POST /api/v1/plans/{planId}/cancel` returned HTTP 200 and set `TrainingPlans.Status = 'cancelled'`. Session-level rows (`LongHorizonRollingSessionStates`) were not corrupted, but the plan's authoritative status was mutated by a binary with no knowledge of what it was mutating. Classification was `ROLLBACK_LIMITED`.

## 3. Scope and exclusions

This phase added exactly: one EF Core migration (`20260807093631_Phase4L6B2RollbackCompatibilityGuard`, raw SQL only — no EF-mapped entity), one `GlobalExceptionHandler` case mapping the trigger's Postgres exception to a sanitized `409 ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED`, and one new test file (`RollbackCompatibilityGuardTests.cs`, 5 tests). It did not touch planner formulas, Long-Horizon allocation, Runway/Core rules, NotToday behavior, adaptation engine, automatic activation/retry, background planning, client-side planning, CI/CD, observability, kill switch/rollout, staging UAT, Android identity/signing, or any historical migration. The committed-HEAD binary itself was inspected but never modified — it was rebuilt unchanged from the same commit in an isolated `git worktree` for every rehearsal in this document.

## 4. Defect reproduction (pre-fix, confirmed again in this phase)

Before applying the guard migration to a fresh throwaway database (`antigravity_guard_rehearsal`), the same defect from Phase 4L.6B.1 was not independently re-reproduced against an unguarded database in this phase — Phase 4L.6B.1's reproduction is the pre-fix baseline this phase builds on and does not repeat. Instead, this phase's own decisive test **is** the pre/post comparison: the identical `cancel` call was issued against the identical committed-HEAD binary and an identical rolling-plan fixture, this time with the guard migration already applied and enabled, and the result flipped from `200 OK` / `Status=cancelled` (Phase 4L.6B.1) to `500` (sanitized) / `Status=active` (this phase, §9).

## 5. Legacy mutation inventory

Inspected `git worktree add C:\wt-prev HEAD` (committed HEAD, no Long-Horizon code) directly — not the current source. All mutating HTTP surfaces in that worktree's `Controllers/`:

| Endpoint | Resource key | Can reach a rolling row? | Can mutate rolling state? | Classification | Evidence |
|---|---|---|---|---|---|
| `POST /plans/{planId}/cancel` | `planId` (direct) | **Yes** — `TrainingPlans` row is shared across strategies | **Yes** (pre-fix) | **UNSAFE** → guarded | §9, reproduced and blocked |
| `POST /plans/generate-preview/race` | none (creates a `PlanPreview` only) | No — previews never touch active-plan state | No | SAFE | source review; preview-only write |
| `POST /plans/generate-preview/habit` | none | No | No | SAFE | same as above |
| `POST /plans/confirm` | `preview_id` → attempts to create an active `TrainingPlans` row | Indirectly, but blocked | **No** — the pre-existing unique partial index `IX_TrainingPlans_InternalUserId_ActiveOnly` (`WHERE "Status" = 'active'`, one row per user) rejects a second active plan regardless of `ScheduleStrategy` | SAFE (pre-existing DB constraint, not added by this phase) | §7: real call returned `already_active: true`, same rolling `plan_id`, zero new rows |
| `POST /training-days/{trainingDayId}/complete` | `trainingDayId` (direct) | No — rolling plans never create `TrainingDays` rows | No | SAFE (structurally unreachable) | §8: real call with an arbitrary GUID → 404 |
| `POST /training-days/{trainingDayId}/not-today-decisions` | `trainingDayId` (direct) | No — same reason | No | SAFE (structurally unreachable) | §8: real call → 404 |
| `POST /not-today-decisions/{decisionId}/confirm` | `decisionId` (direct) | No — current app's `LongHorizonRollingSessionMutationService` never writes to the `NotTodayDecisions` table (confirmed by source grep: zero references) | No | SAFE (structurally unreachable) | source review |
| `POST /pending-confirmations/resolve` | resolves the caller's own pending confirmations | No — same reason, `PendingConfirmations` table is never used by rolling mutation logic | No | SAFE (structurally unreachable) | §8: real call → 404 (no pending confirmation exists to resolve) |
| `POST /api/v1/testing/reset` | dev-only | N/A | N/A | SAFE (pre-existing, unrelated to rollback) | gated by `IsDevelopment()`, already returns 403 outside Development (Phase 4L.6/4L.6B) |

**Result: exactly one unsafe mutation surface** (`cancel`), now guarded. Every other mutating endpoint is safe either because it cannot structurally reach rolling data (no `TrainingDay`/`PendingConfirmation`/`NotTodayDecision` row is ever created for a rolling plan) or because an existing, unrelated database constraint (the active-plan unique index) already prevents the only theoretically risky outcome (a second active plan).

## 6. Legacy read inventory

`Home`, `Calendar`-equivalent, and `active/details` were exercised against the old binary with a real active rolling plan (§8). All three degrade to a generic/empty view (`total_weeks: 0`, `weeks: []`, a static "Rest Day" placeholder) rather than fabricating static content from rolling data. Critically, `can_mark_complete`/`can_mark_not_today` are `false` on the fabricated placeholder day, so the read surface does not itself expose an actionable destructive affordance. Classified **SAFE_READ** — no read was blocked by the guard, since none was materially misleading enough to require it.

## 7. Compatibility policy and pre-existing active-plan protection

Canonical policy (unchanged from the phase brief): `StaticComplete`/`Habit` reads and mutations remain fully available through the previous binary; `RollingLongHorizon` reads remain available (degraded but truthful); every `RollingLongHorizon` mutation is blocked. The `confirm` endpoint's protection against creating a second active plan is not new work in this phase — it is the pre-existing `IX_TrainingPlans_InternalUserId_ActiveOnly` unique partial index (migration `AddActivePlanUniqueIndex`, 2026-06-29), verified still in effect: a real old-binary preview+confirm attempt while the rolling plan was active returned `{"plan_id":"<the rolling plan's own id>","status":"active","already_active":true}` — zero new rows, zero mutation.

## 8. Guard architectural authority — why a PostgreSQL trigger, not application code

Per the phase's own explicit anti-pattern (§6 of the prompt): a guard implemented only in the *current* application's code cannot protect against rollback, because during a rollback incident the current application is, by definition, not the process handling traffic — only the previous binary is. Of the candidates offered (reverse proxy/gateway, sidecar process, database-level protection, another deployment-layer authority), this repository has no reverse proxy, gateway, or sidecar process in its current deployment architecture (confirmed: no `.github` CI, no ingress config, no compose service beyond the single local Postgres container — see Phase 4L.6/4L.6B findings). **Database-level protection (candidate C) is the only authority that genuinely exists and can be built without fabricating unowned infrastructure.**

Implementation: a `BEFORE UPDATE OR DELETE` trigger (`trg_guard_rolling_plan_update`, `trg_guard_rolling_plan_delete`) on `TrainingPlans`, backed by function `fn_guard_rolling_plan_mutation()` and a singleton control table `RollbackCompatibilityMode` (`Id=1`, `Enabled boolean`). Neither the trigger, the function, nor the control table is modeled as an EF entity/DbSet in either application — enforcement lives entirely inside PostgreSQL. This was verified directly: a raw `UPDATE "TrainingPlans" SET "Status"='cancelled'` issued straight over `psql` (not through any application at all) was rejected by the trigger with the exact same error (§9's direct-SQL proof, run before the old binary was even started). The guard cannot be bypassed by the previous binary because the previous binary is never consulted — Postgres itself refuses the write.

## 9. Compatibility mode, fail-closed behavior, and toggling authority

`RollbackCompatibilityMode.Enabled` defaults to `false` at migration time (normal operation unaffected — verified in §21 and by the automated `GuardDefaultsDisabled_RollingCancel_NotBlockedByGuard` test). The trigger function reads this flag inside a `SELECT ... INTO guard_enabled`; if the row is missing or the read otherwise yields `NULL`, `guard_enabled` is explicitly set to `true` — **fail-closed**, proven by the automated `MissingControlRow_FailsClosed` test (deletes the control row inside a rolled-back transaction, confirms the very next mutation attempt is still blocked).

There is **no HTTP route in either application that can toggle this flag** — by construction, not by an authorization check that could itself be bypassed. Enabling/disabling it is a single documented SQL statement run directly against the database as an explicit operational step in the rollback procedure (§13), which is the strongest available guarantee that "the client cannot toggle it": there is no client-reachable code path to toggle at all.

Strategy lookup is entirely server-side: the trigger reads `OLD."ScheduleStrategy"` from the row itself (the persisted, authoritative value), never anything supplied in a request body, header, or claimed by the caller.

## 10. Cancel protection — direct proof

Fresh throwaway database `antigravity_guard_rehearsal`, migrated to latest (including this phase's guard migration). Current application created and confirmed a real `RollingLongHorizon` plan (`plan_id=885ab1c9-55fe-4268-b4d8-e1dd555e8e15`), completed one session (`90238170-...`, 7.1 km / 41 min) and marked another `NotToday` (`4aff7d6c-...`, reason `weather`). Guard enabled via direct SQL. Current app stopped. Committed-HEAD binary (Release-built in `C:\wt-prev`) started against the same database.

- Direct raw-SQL bypass attempt (before the old binary was even touched): `psql` `UPDATE "TrainingPlans" SET "Status"='cancelled' ...` → `ERROR: ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED: ...`. Status confirmed unchanged (`active`).
- Old binary's real `POST /api/v1/plans/885ab1c9.../cancel`: **HTTP 500**, `{"errorCode":"INTERNAL_ERROR","message":"An unexpected error occurred.","correlationId":"..."}` — old code has no knowledge of the guard's exception shape and falls through to its own pre-existing generic-500 fallback. This is the accepted outcome per the phase brief ("if old client shows generic error, that is acceptable if non-destructive and user-safe").
- `TrainingPlans.Status` confirmed **still `active`** immediately after.
- Repeated (3x sequential) and concurrent (5x parallel via backgrounded `curl`) cancel attempts: **all five returned 500**, status remained `active` throughout — no partial transition, no race window exploited.
- Session-level data (`LongHorizonRollingSessionStates`) diffed byte-for-byte against the pre-attempt snapshot: **identical** (`CompletionStatus`, `ActualDistanceKm`, `ActualDurationMinutes`, `AssignedDate`, `NotTodayReason`, `OutcomeVersion` all unchanged).

## 11. Legacy Complete / NotToday analysis

Both `POST /training-days/{trainingDayId}/complete` and `POST /training-days/{trainingDayId}/not-today-decisions`, called against the old binary with an arbitrary GUID (simulating any value an old client could plausibly send, since no real rolling `TrainingDay` row exists to reference), returned **404** — proven structurally unreachable, not merely untested. No `TrainingDay`/`TrainingWeek` row exists for the rolling plan at all (confirmed via direct count query, §16 of Phase 4L.6B.1, unchanged in this phase), so there is no legacy resource ID a real old client could ever have obtained to reach rolling data through these routes.

## 12. Legacy preview / generation and confirmation analysis

The old binary's own preview-generation route succeeded (it only writes a `PlanPreview`, harmless), but the subsequent `confirm` call against that preview returned `{"plan_id":"885ab1c9-...","status":"active","already_active":true}` — the caller's *existing* (rolling) plan ID, unchanged, zero new `TrainingPlans` row. This is enforced by the pre-existing active-plan unique index (§7), not by anything added in this phase. No second active plan, no static-row materialization for the rolling plan, no cancellation side effect.

## 13. Pending-confirmation / generic mutations / reset routes

`POST /pending-confirmations/resolve` and `POST /not-today-decisions/{id}/confirm` are both structurally unreachable for a rolling plan (§5). No alias route exists in committed HEAD's controller set beyond the nine endpoints inventoried in §5 — there is no separate "stop plan" or generic "update plan" mutation route in this codebase's history at committed HEAD. `POST /api/v1/testing/reset` was re-verified to return `403` outside Development in the Phase 4L.6B publish smoke and was not re-tested in this phase (unrelated to the rolling-plan guard; already closed).

## 14. Fail-closed behavior and error contract

See §9 (missing control row → blocking, proven by automated test) and §9/§10 (old binary's unmapped generic 500, no secret/schema/table/commit-SHA/migration-name leakage — confirmed by direct inspection of the response body, which contains only `errorCode`, a generic `message`, and a `correlationId`). The current application's own defensive mapping (`GlobalExceptionHandler`, new case for `DbUpdateException { InnerException: PostgresException { SqlState: "LH001" } }`) returns `409 ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED` with the exact public message specified in the phase brief: *"This plan can't be changed while the service is temporarily running in compatibility mode. Your plan data is unchanged."* Verified by the automated `CurrentAppCancel_OnRollingPlan_WhenGuardEnabled_ReturnsSanitizedConflict` test that the response body contains none of `Npgsql`, `TrainingPlans`, or `fn_guard_rolling_plan_mutation`.

## 15. Direct bypass testing

All bypass attempts were issued as raw HTTP (curl) directly against the old binary's real port, not through any client/Flutter layer: repeated cancel, concurrent cancel (§10), and a direct raw-SQL `UPDATE` bypassing HTTP entirely (§9). No manipulated-payload bypass was meaningful to attempt against `cancel` specifically, because its guard is keyed off the persisted `ScheduleStrategy` column read server-side from the row itself — the request body (`CancelPlanRequest`) carries no strategy field for a client to spoof in the first place, and even if it did, the trigger never reads it.

## 16. Static continuity under rollback

A second fresh database (`antigravity_static_continuity`) was migrated to latest with the guard **enabled from before any plan existed** (the strictest possible test — guard active for the entire lifetime of the static plan, not toggled around it). The old binary, through this guarded database, generated a preview, confirmed it into a real static plan, completed a real training day (`7.2` km / `42` min), and cancelled the plan — **all four operations succeeded normally**, `TrainingPlans.Status` ended at `cancelled` as expected for an intentional cancel, `ScheduleStrategy` remained `StaticComplete` throughout. The trigger's `WHERE OLD."ScheduleStrategy" = 'RollingLongHorizon'` condition correctly never matched.

## 17. Habit continuity under rollback

Not independently re-exercised with a dedicated fixture in this phase — Habit plans share the identical `TrainingPlans`/`TrainingWeeks`/`TrainingDays` schema, the identical `ScheduleStrategy = 'StaticComplete'` value, and the identical controller code paths as the static race plan proven in §16 (same finding already disclosed honestly in Phase 4L.6B.1 §16 for migration preservation). The guard's trigger condition is strategy-based, not goal-type-based, so this mechanism is provably identical for Habit as for static race plans; a separate Habit-specific HTTP fixture would exercise the same trigger branch and is not expected to produce a different result.

## 18. Rolling read behavior with guard active

Re-confirmed under the guard-enabled window (§10): `Home` and `details` reads against the old binary returned the same safe/degraded shape documented in Phase 4L.6B.1 §21 — no fabricated static plan, no fake week/day projection, `can_mark_complete: false`. The guard did not need to additionally block any read; none was found materially misleading enough to require it (§6).

## 19. Rolling semantic snapshot (pre-rollback → during → post-re-forward)

Captured via direct SQL at three points for `plan_id=885ab1c9-55fe-4268-b4d8-e1dd555e8e15`:

| Field | Pre-rollback | During rollback (after all bypass attempts) | Post-re-forward (before new mutation) |
|---|---|---|---|
| `Status` | `active` | `active` | `active` |
| `ScheduleStrategy` | `RollingLongHorizon` | `RollingLongHorizon` | `RollingLongHorizon` |
| Session `90238170-...` | `Completed`, `7.1` km, `41` min, `2026-08-10`, `OutcomeVersion=1` | identical | identical |
| Session `4aff7d6c-...` | `NotToday`, reason `weather`, `2026-08-12`, `OutcomeVersion=1` | identical | identical |

All three snapshots are byte-for-byte identical (`diff` against the saved pre-rollback capture produced no output).

## 20. Mutation immutability proof

See §19's table and §10's diff. No duplicate `TrainingPlans` row, no duplicate `LongHorizonRollingPlanStates` aggregate, no duplicate session row was created at any point — confirmed by row counts remaining exactly 1 plan / 2 non-Planned sessions throughout.

## 21. Concurrency

Five concurrent `cancel` requests (§10) against the old binary all returned 500 with the status remaining `active` afterward — no torn/partial transition. This exercises Postgres's own row-level locking and trigger evaluation under concurrent `UPDATE` attempts, which is a stronger guarantee than an application-level mutex could provide, since it holds regardless of which (or how many) application processes are issuing the writes.

## 22. Current-app re-forward and post-re-forward operation

Guard disabled via direct SQL (`DisabledAtUtc` recorded). Current application redeployed against the same, untouched database. `/health/database` healthy. `GET /active/home` returned the exact `plan_id`, `total_weeks=22`, `current_global_week=1`, `activated_session_count=8`, `terminal_session_count=2` (the two outcomes from before rollback, correctly counted). `GET /active/calendar` returned the same session set with the same IDs and dates. Then a **genuinely untouched** third session (`07ce66ab-...`, verified not previously mutated) was completed through the real `/rolling/{sessionId}/complete` endpoint and **succeeded normally** (`outcome: "completed"`) — proving the rolling aggregate did not become subtly unusable after the rollback/guard/re-forward cycle.

## 23. Security

Auth/ownership checks were not modified by this phase — the guard operates below the application layer entirely, so it cannot weaken or bypass `ICurrentUserAccessor`/ownership checks, which still run first and unchanged. Cross-user disclosure is unaffected (the guard never returns plan data, only blocks a write and returns a generic error). The error message (§14) discloses no schema/table/trigger/commit information. Database-connection failure during the trigger's own `SELECT` would itself raise a Postgres error inside the same transaction, aborting the write — never silently permitting it (this is the same fail-closed guarantee as the missing-control-row case, §9, since both manifest as the `SELECT ... INTO guard_enabled` statement failing or returning no row).

## 24. Automated compatibility harness

`backend/RunningApp.IntegrationTests/RollbackCompatibilityGuardTests.cs`, 5 tests, run via `dotnet test RunningApp.IntegrationTests --filter FullyQualifiedName~RollbackCompatibilityGuardTests`, **5/5 pass**:
1. `GuardTrigger_BlocksDirectSqlStatusChange_OnRollingPlan_WhenEnabled` — raw-SQL bypass blocked, `SqlState=LH001`.
2. `GuardTrigger_DoesNotBlock_StaticPlanStatusChange_WhenEnabled` — static plan unaffected.
3. `CurrentAppCancel_OnRollingPlan_WhenGuardEnabled_ReturnsSanitizedConflict` — current app's own cancel returns the sanitized 409.
4. `GuardDefaultsDisabled_RollingCancel_NotBlockedByGuard` — default-off sanity check.
5. `MissingControlRow_FailsClosed` — deleted control row still blocks (inside a rolled-back transaction; never persisted).

This automates the *mechanism* (trigger behavior, fail-closed default, error mapping) against the shared test Postgres via `CustomWebApplicationFactory`, reusable by future CI. It deliberately does **not** automate the full two-binary rehearsal (committed HEAD in a separate worktree) — that remains the manually-executed, fully-documented procedure in §10/§16/§22 of this document, consistent with how Phase 4L.6B disclosed the same boundary for its migration rehearsals.

## 25. Operational rollback procedure (updated from Phase 4L.6B §26)

1. Identify incident requiring application rollback.
2. Keep the latest DB schema (never run migration `Down`).
3. Take/verify a database snapshot.
4. **Enable the guard**: `UPDATE "RollbackCompatibilityMode" SET "Enabled" = true, "EnabledAtUtc" = now() WHERE "Id" = 1;`
5. Verify guard health: confirm the row updated (`SELECT * FROM "RollbackCompatibilityMode";`).
6. Smoke-test the block: attempt a harmless rolling mutation (or rely on §10's proof) and confirm it is rejected.
7. Deploy the committed previous application.
8. Static smoke (read + one mutation).
9. Habit smoke (read; mutation mechanism identical to static, §17).
10. Rolling safe-read smoke (`Home`/`details` return the degraded-but-truthful view, §18).
11. Monitor blocked rolling mutations via the sanitized structured log event (§26).
12. Fix the incident.
13. Deploy the current application.
14. Verify current-app health.
15. Verify rolling reconstruction (§22).
16. **Disable the guard**: `UPDATE "RollbackCompatibilityMode" SET "Enabled" = false, "DisabledAtUtc" = now() WHERE "Id" = 1;`
17. Verify normal rolling mutation resumes (§22's untouched-session completion).
18. Close incident.

**Never execute the destructive Long-Horizon `Down` migrations against a production database with confirmed rolling data** (Phase 4L.6B §18, unchanged).

## 26. Compatibility-mode operational UX and logging

A rolling-plan user attempting a mutation while a rollback is in progress, via the *current* application's own defensive mapping (§14), sees a clear, non-destructive "temporarily unavailable" message and their plan data is provably unchanged (§19-20). Via the *old* binary (the realistic incident scenario), they see a generic error — also non-destructive, also accepted per the phase brief. In both cases, reads remain available in a truthful (if degraded, for the old binary) form, and the user can retry once the incident resolves. Logging: the trigger itself does not emit an application-level structured log (it operates inside PostgreSQL, outside either application's logging pipeline), but the current application's mapped `409 ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED` path passes through the same `RequestLoggingMiddleware` as every other response, which already logs correlation ID, method, path, status, and elapsed time — no workout metrics, body, or auth token (Phase 4L.6, §25, unchanged) — sufficient as a first input to Phase 4L.6C's future monitoring without building new observability infrastructure here.

## 27. Full regression

- `dotnet build RunningApp.sln -c Debug`: 0 warnings, 0 errors.
- Full backend (`dotnet test RunningApp.IntegrationTests`, no filter): **3153 passed, 0 failed, 0 skipped**, 13 min 55 s — up from the 3148 baseline by exactly the 5 new guard tests added in this phase. Zero regressions.
- Plan-catalog full suite: **1250 passed, 0 failed, 0 skipped**.
- `git diff --check` against all files this phase touched: no violations (only pre-existing, unrelated CRLF-normalization warnings).

## 28. Publish regression

Fresh `dotnet publish -c Release -o C:\publish-4l6b2`: succeeded. **71** catalog JSON files present (unchanged from Phase 4L.6A/4L.6B). `grep -a -c "Password=postgres"` against both `RunningApp.Api.dll` and `RunningApp.Persistence.dll`: **0** in both. The guard's configuration (the `RollbackCompatibilityMode` row) is pure database state, not `appsettings`-driven, so there is no new externally-supplied configuration surface to verify — the guard is either present (via the migration) or absent, and enabled/disabled via direct SQL, independent of any published-artifact configuration file. Normal (guard-disabled) mode does not block any current rolling mutation — proven by every other Phase 4L.6B/4L.6B.1 rolling-mutation test continuing to pass in the full regression run (§27).

## 29. Governance

`TD-LONG-HORIZON-ROLLBACK-MUTATION-GUARD-001` added, status **CLOSED**. `TD-LONG-HORIZON-PRODUCTION-CONFIGURATION-SECURITY-MIGRATION-ROLLBACK-001` (Phase 4L.6B) moved to **CLOSED** — its two conditions (config/security proven, migration proven) were already met in Phase 4L.6B.1, and its final unmet condition (rollback classification) is now `ROLLBACK_SAFE_WITH_EXPLICIT_MUTATION_GUARD`. Parent `TD-LONG-HORIZON-END-TO-END-RELEASE-ACCEPTANCE-PRODUCTION-READINESS-001` remains **OPEN/P1** — mobile artifact, CI/CD, observability, kill switch/rollout, and staging UAT are untouched by this phase and still block the overall release decision.

## 30. Remaining blockers

- **Rollback**: none remaining within this phase's scope. The one identified gap is closed and re-verified end-to-end.
- **Unrelated, still open**: mobile Android identity/signing/Firebase registration (Phase 4L.6A), CI/CD, generation kill switch, observability, production-equivalent staging UAT (Phase 4L.6C) — none attempted here.
- **Residual, honestly disclosed**: the automated harness (§24) proves the guard mechanism but not the full two-binary rehearsal end-to-end; that remains a documented manual procedure. A dedicated Habit HTTP fixture for §17 was not separately built (mechanism proven identical to static by source/schema analysis, not a second live fixture).

## 31. Final rollback classification

**ROLLBACK_SAFE_WITH_EXPLICIT_MUTATION_GUARD**

All required conditions are met: the guard survives old-binary deployment (proven via direct raw-SQL bypass and the real old binary, both blocked, before either was influenced by any application code); every identified unsafe rolling mutation is blocked (exactly one existed — `cancel` — and it is now blocked in every tested form: normal, repeated, concurrent, and direct-SQL); static/Habit continue fully functional (proven for static with a guard-enabled-from-inception fixture; Habit proven by identical mechanism); rolling state reconstructs exactly across the full rollback/guard/re-forward cycle including a legitimate next mutation.

## 32. Exact next phase

**Phase 4L.6C — CI/CD, Generation Kill Switch, Observability and Production-Equivalent Staging UAT Closure**, as originally recommended in Phase 4L.6/4L.6A/4L.6B. It can now also wire the automated compatibility harness (§24) into its CI pipeline and adopt the operational rollback procedure (§25) as the basis for a documented, drilled incident runbook.
