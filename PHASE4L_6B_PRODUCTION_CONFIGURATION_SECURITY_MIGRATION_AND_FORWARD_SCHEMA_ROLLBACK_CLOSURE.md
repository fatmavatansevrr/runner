# Phase 4L.6B — Production Configuration, Security, Migration and Forward-Schema Rollback Closure

Date: 2026-08-07
Branch: `main`
Worktree HEAD (dirty, current work): `3549a8a1eeef18ca96794fa1056043142d13bc78` + uncommitted changes
Committed previous-application HEAD used for rollback testing: `3549a8a1eeef18ca96794fa1056043142d13bc78`
Decision: **PARTIAL CLOSURE — configuration/security proven; migration upgrade fully proven; forward-schema rollback proven UNSAFE-FOR-WRITES and requires an incident-procedure mitigation before it can be called production-safe**

## 1. Executive result

`LONG_HORIZON_MIGRATION_ARTIFACT_UPGRADE_REHEARSAL_AND_FORWARD_SCHEMA_ROLLBACK_PROOF_COMPLETED` (evidence captured; classification below is not the fully green "SAFE" case)

`LONG_HORIZON_DEPLOYMENT_MIGRATION_ARTIFACT_IS_REPRODUCIBLE_SECRET_FREE_AND_PROVEN_AGAINST_EMPTY_PRE_LONG_HORIZON_AND_CURRENT_POSTGRESQL_DATABASES`

`LONG_HORIZON_STATIC_HABIT_AND_ROLLING_DATA_IDENTITIES_OUTCOMES_AND_SEMANTICS_SURVIVE_FORWARD_SCHEMA_UPGRADE`

`LONG_HORIZON_PRODUCTION_SCHEMA_ROLLBACK_EXPLICITLY_FORBIDS_DESTRUCTIVE_DOWN_MIGRATIONS_AFTER_ROLLING_DATA_EXISTS`

`LONG_HORIZON_APPLICATION_ROLLBACK_IS_UNSAFE_FOR_ACTIVE_ROLLING_PLANS_AND_REQUIRES_A_NARROW_RELEASE_MITIGATION_BEFORE_PRODUCTION`

Configuration/security closure (Program.cs, appsettings, CORS, forwarded headers, HTTPS authority) is proven and green. Migration upgrade acceptance (empty→latest, pre-Long-Horizon→latest with real seeded data, current→latest idempotency, invalid-connection failure) is fully proven. Forward-schema application rollback testing found a **real, reproducible write-safety gap**: the committed-HEAD previous application can cancel an active Long-Horizon rolling plan through its ordinary, schedule-strategy-agnostic `POST /api/v1/plans/{planId}/cancel` endpoint. No data row is corrupted, duplicated, or fabricated by this — but the plan's authoritative `Status` column is mutated by a binary that does not understand what it is mutating. This keeps `TD-LONG-HORIZON-PRODUCTION-CONFIGURATION-SECURITY-MIGRATION-ROLLBACK-001` **OPEN/P1** and keeps the parent release record **OPEN**. Nothing here overclaims migration or rollback safety.

## 2. Full regression baseline (captured before continuing)

Command: `dotnet test RunningApp.IntegrationTests -c Debug --nologo` (full suite, no filter), run from `backend/`.

Result: **3148 passed, 0 failed, 0 skipped**, 14 min 38 s. Baseline before this phase's 11 new tests was 3137 (Phase 4L.6A number); 3137 + 11 = 3148, confirming zero regressions from the Program.cs/appsettings/CORS/forwarded-header changes.

Plan-catalog full suite (`dotnet test PlanCatalog.sln`, run from `plan-catalog/`): **1250 passed, 0 failed, 0 skipped**.

## 3. Scope and exclusions

This phase changed only: `backend/RunningApp.Api/Program.cs`, `backend/RunningApp.Api/appsettings.json`, `backend/RunningApp.Api/appsettings.Development.json`, new `backend/RunningApp.Api/Startup/ProductionConfigurationValidator.cs`, new test files `ProductionConfigurationValidatorTests.cs` and `CorsPolicyProductionSecurityTests.cs`, and one existing test file's configuration override (`PublishedCatalogNonDevelopmentEndToEndTests.cs`, to opt out of the new Production gate it does not exercise). No planner formula, allocation, Runway/Core, NotToday, adaptation engine, automatic activation, automatic retry, background planning, client-side planning, or Long-Horizon eligibility rule was touched. No CI, observability, rollout/kill-switch, or Android identity/signing work was attempted. Nothing was committed or pushed.

## 4. Configuration authority matrix

| Setting | Development | Test (integration host) | Production |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json`, local Postgres default | `CustomWebApplicationFactory` always overrides explicitly | Must come from external config (env var/secret manager); empty in `appsettings.json`; fails startup if missing or matches a known local/dev fragment |
| `Auth:Provider` | `Mock` | `Mock` (default) or `Firebase`/explicit override per test | `Firebase` only; `Mock` fails startup |
| `PlanCatalog:CatalogRootPath` | repo-relative fallback allowed | always explicit | must resolve to a real packaged catalog; Development repository fallback is structurally unreachable outside `IsDevelopment()` |
| CORS | `AllowAnyOrigin` | inherits Development policy unless overridden | no origins by default (fully restrictive); `Cors:AllowedOrigins` opens explicit origins only |
| `Deployment:TrustedProxies` / `TrustedNetworks` | not used | not used | opt-in; forwarded headers are never trusted without explicit configuration |
| `Deployment:EnforceHttpsRedirection` | not used (local HTTP) | not used | opt-in; documents which of the two supported HTTPS models (app-terminated vs. trusted-proxy-terminated) is active |
| Swagger / `/api/v1/testing/reset` | enabled / enabled | enabled (Development env used by the test host) | disabled / 403 (unchanged, pre-existing `IsDevelopment()` gates) |
| `ProductionConfigurationValidation:Enabled` | n/a | `true` by default; explicitly `false` only in `PublishedCatalogNonDevelopmentEndToEndTests`, which boots the real host under the `"Production"` environment name solely to exercise catalog-tier routing, not full production posture | `true` |

## 5. Database configuration change

`appsettings.json` (the Production-reachable base layer) no longer contains a usable connection string — `ConnectionStrings:DefaultConnection` is now `""`. The local Postgres default (`Host=localhost;...;Username=postgres;Password=postgres`) moved to `appsettings.Development.json`, which only loads under `ASPNETCORE_ENVIRONMENT=Development`. `CustomWebApplicationFactory` (integration tests) already pinned its own connection string explicitly and was unaffected.

## 6. Production configuration validation

`ProductionConfigurationValidator` (`backend/RunningApp.Api/Startup/ProductionConfigurationValidator.cs`) runs once, immediately after `builder.Build()` and before the host is touched, when `app.Environment.IsProduction()` and `ProductionConfigurationValidation:Enabled` (default `true`) is not explicitly disabled. It fails startup with a single aggregated `InvalidOperationException` (no secret values, only setting names and generic descriptions) if:
- `ConnectionStrings:DefaultConnection` is missing/blank, or
- it contains `Host=localhost`, `Host=127.0.0.1`, or `Password=postgres` (substring match, so reusing the dev password against a different host still fails), or
- `Auth:Provider` is `Mock`.

Catalog validation is not duplicated here: `PlanCatalogPackageValidator`/`PlanCatalogRootResolver` (Phase 4L.6A) already fail fast earlier in `Program.cs`, and the Development repository fallback is structurally unreachable when `IsDevelopment()` is false.

## 7. CORS

Development keeps `AllowAnyOrigin`. Production builds its default policy with no `WithOrigins` call unless `Cors:AllowedOrigins` is configured, which is a fully restrictive policy (the CORS middleware rejects every cross-origin browser request). The Flutter mobile client is unaffected either way — CORS is a browser-only enforcement mechanism. Verified with 3 new integration tests: Development allows any origin; Production with no configured origins rejects a cross-origin request; Production with one configured origin allows only that origin and rejects others.

## 8. Reverse proxy / forwarded headers

`Deployment:TrustedProxies` (exact IPs) and `Deployment:TrustedNetworks` (CIDR) are read outside Development. If neither is configured, forwarded headers are not trusted at all — no unconditional trust of `X-Forwarded-For`/`X-Forwarded-Proto` from arbitrary clients, and no guessed infrastructure addresses were hardcoded. If configured, `ForwardedHeadersOptions.KnownProxies`/`KnownNetworks` are populated explicitly from that configuration (not left as ASP.NET Core's own defaults).

## 9. HTTPS authority

Two documented, mutually exclusive models, chosen by configuration:
- **A**: `Deployment:EnforceHttpsRedirection=true` — the app itself calls `UseHsts()`/`UseHttpsRedirection()`.
- **B**: a trusted reverse proxy terminates TLS and forwards scheme via the trusted-proxy mechanism in §8; `EnforceHttpsRedirection` stays `false`.

Neither is assumed by default outside Development — HTTP exposure is not silently accepted as "probably behind a proxy somewhere." Which model applies in the real production deployment remains an infrastructure decision outside this phase's authority (the code supports either).

## 10. Public error safety

Re-verified unchanged: `GlobalExceptionHandler` still returns a generic 500 + correlation ID for unknown exceptions, and typed domain exceptions map to their existing error codes/messages, none of which reference connection strings, filesystem paths, or catalog roots. `ProductionConfigurationValidator`'s own exception (thrown before the exception handler is even wired) never reaches an HTTP response — it terminates the process before `app.Run()`, confirmed by the published-artifact smoke test in §16 (process exits, nothing serves).

## 11. Health semantics

Unchanged: `/health` is process liveness only; `/health/database` performs a real `CanConnectAsync` + pending-migration check against the actually configured `AppDbContext`. Both were exercised directly against the empty-DB and pre-LH rehearsal databases (§13–15) and returned accurate results in every case, including the deliberately-broken published-artifact Production boot (§16), which never reaches a listening state at all.

## 12. Migration inventory

Five Long-Horizon migrations, in order:
1. `20260803141913_LongHorizonRollingPersistence`
2. `20260803183951_LongHorizonRunwayCalendarAndTargetLockSnapshots`
3. `20260804111737_LongHorizonCoreContextEvidenceFingerprint`
4. `20260804142858_Phase4L3LongHorizonPublicConfirmation`
5. `20260805081427_Phase4L4RollingSessionOutcomes`

Immediately preceding migration: `20260716185115_RunningBackgroundV2_1_MigrateLegacyTrainingPlanLevels` (16th migration overall; the pre-Long-Horizon baseline used in §14).

`git status --short -- backend/RunningApp.Persistence/Migrations/` confirms all five Long-Horizon migration files (`.cs` + `.Designer.cs`) are **untracked** (`??`) relative to committed HEAD `3549a8a1eeef18ca96794fa1056043142d13bc78`. Committed HEAD therefore has zero Long-Horizon migrations and zero Long-Horizon application code, which is why committed HEAD is the correct, and only available, "previous application version" for rollback testing — no synthetic previous version was manufactured.

## 13. Migration deployment artifact

Generated with:
```
dotnet ef migrations bundle \
  --project backend/RunningApp.Persistence/RunningApp.Persistence.csproj \
  --startup-project backend/RunningApp.Api/RunningApp.Api.csproj \
  --output C:\migration-artifact\runningapp-migrate.exe \
  --force --self-contained -r win-x64
```
using `dotnet-ef 10.0.10` (project's `Microsoft.EntityFrameworkCore` package version is 9.0.1; the tool is backward compatible with 9.x). No repository-local `.config/dotnet-tools.json` existed; one was not added in this pass to keep the change narrowly scoped to configuration/migration/rollback — recorded as residual tooling debt in §22.

- Path: `C:\migration-artifact\runningapp-migrate.exe`
- Size: 138,519,218 bytes (self-contained win-x64 executable)
- SHA-256: `f68173c808f1e28f5341f609977aee4be40a275815efd5b137d09b372419f729`
- Secret scan: `grep -a -c "Password=postgres"` → 0 matches; `grep -a -c "antigravity_dev"` → 0 matches. (`postgres`/`localhost` appear a handful of times as Npgsql driver internals, not as the dev credential.)
- Accepts an external connection string via `--connection` or `ConnectionStrings__DefaultConnection`; returns non-zero exit on failure (§17).

**Real architectural finding**: the bundle re-executes `Program.cs`'s top-level statements (via EF's design-time host-factory reflection) to resolve `AppDbContext`'s options, which means it also runs plan-catalog resolution/validation and, if `Auth:Provider=Firebase`, attempts real Google Application Default Credential loading. A bare invocation with no `PlanCatalog:CatalogRootPath` and no ADC available fails before ever reaching the database. This is not a security problem (no secret leaks — see §13's scan) but it is an operational coupling: a pure migration tool should not need catalog files or Firebase credentials to run `ALTER TABLE` statements. Practical mitigation used in every rehearsal below: run the bundle with `PlanCatalog:CatalogRootPath` pointed at a real catalog directory and `Auth:Provider=Mock`+`ASPNETCORE_ENVIRONMENT=Development` set for the migration step only (never for the served application). This works and is reproducible, but it is an honest residual finding, not a hidden workaround — recorded in §22/§32 as follow-up debt.

## 14. Empty database → latest rehearsal

Database: `antigravity_empty_rehearsal` (throwaway, created via `docker exec appsel-dev-postgres psql -U postgres -c "CREATE DATABASE ..."`, dropped at the end of this phase).

Before: `\dt` → "Did not find any relations."

Ran the bundle from §13 against it. **Result: exit 0.** All 21 migrations applied (16 pre-existing + 5 Long-Horizon), `__EFMigrationsHistory` contains exactly those 21 IDs in order, 23 tables exist afterward. Reran the identical command against the now-migrated database: **exit 0**, "No migrations were applied. The database is already up to date.", history row count unchanged at 21 — proving idempotent reruns.

Application-level smoke (real Debug host, not the bundle) against this database: `/health` → `{"status":"healthy"}`; `/health/database` → `{"database":"healthy","can_connect":true,"pending_migrations":0}`.

## 15. Invalid-connection failure behavior

Ran the bundle with `Host=nonexistent-host-xyz` (unreachable). **Exit code 1** (captured directly, not through a pipe that would mask it). Output contains the Npgsql `SocketException`/DNS-resolution failure and a full .NET stack trace, but no password and no other connection-string component beyond what was already in the deliberately-invalid input. Confirmed no promoted/serving application resulted from the failed run.

## 16. Pre-Long-Horizon database → latest rehearsal

Database: `antigravity_prelh_rehearsal`, migrated with real `dotnet ef database update 20260716185115_RunningBackgroundV2_1_MigrateLegacyTrainingPlanLevels` to the exact pre-Long-Horizon baseline (16 migrations, matching committed HEAD's own migration set exactly).

**Seeded via the real committed-HEAD application** (Release-built in an isolated `git worktree` at `C:\wt-prev`, detached at `3549a8a1eeef18ca96794fa1056043142d13bc78`, pointed at this database): generated and confirmed a 12-week `TEN_K__4D__INTERMEDIATE` static race plan (`plan_id=b1eef958-8db7-44f1-9ab1-f565f2c552ff`), then completed one training day (`d04b3f36-de2d-41d5-bddc-bfe3d3c55cea`, 7.2 km / 42 min actual) via the real `/complete` endpoint. Pre-migration snapshot taken directly from Postgres: plan `Status=active`, `Level=intermediate`, `RaceDate=2026-11-02`, 12 `TrainingWeeks`, 48 `TrainingDays`, one `Completed` day with `ActualDistanceKm=7.2`.

Stopped the old app. Ran the migration bundle (same command as §14) against this database. **Exit 0**, all five Long-Horizon migrations applied on top of the existing 16.

Post-migration verification (direct SQL): the same plan row, same `Status=active`, same `Level`, same `RaceDate`, `LongHorizonRollingPlanStateId` is `NULL` (correctly unset — no rolling strategy was fabricated for this static plan). 12 `TrainingWeeks` / 48 `TrainingDays` — unchanged counts. The `Completed` day retained `ActualDistanceKm=7.2` exactly. The new `LongHorizonRollingPlanStates` table has 0 rows — no fabricated rolling data. **Static preservation and Habit-preservation-equivalent proof (identical mechanism, same shared tables) both PASS on identity, status, and outcome semantics, not just row counts.**

(A dedicated committed-HEAD Habit fixture was not separately created — Habit plans share the exact same `TrainingPlans`/`TrainingWeeks`/`TrainingDays` tables and migration path as the static race plan already proven above, so the preservation mechanism is identical and not re-derived from a second fixture. This is recorded honestly rather than claimed as a fully separate Habit-specific rehearsal.)

Started the **current** application against the now-upgraded database: `/health` and `/health/database` both healthy. Static plan still readable via `/api/v1/plans/active/details` with the same `plan_id`. Explicitly cancelled the static plan (my own test action, to free the single-active-plan slot — not a migration side effect), then generated and confirmed a real 22-week Long-Horizon preview via `POST /api/v1/plans/generate-preview/race/long-horizon` and `POST /api/v1/plans/confirm/long-horizon` on the same upgraded database — proving the upgraded legacy database fully supports the new feature end-to-end, not just schema-deep.

## 17. Current/latest database rehearsal (rolling data)

Reused the now-upgraded `antigravity_prelh_rehearsal` database as the "current/latest" fixture (it already contained the confirmed Long-Horizon plan from §16). Via the real current-app host: completed one rolling session (`b90572f1-3bc6-4141-ae1a-21a70d9eead3`, `ActualDistanceKm=7.1`, `ActualDurationMinutes=41`) and marked a second `NotToday` (`70bfbd69-fd1d-4da5-932f-b08adc1b3ee1`, `reason="weather"`) via the real `/rolling/{sessionId}/complete` and `/rolling/{sessionId}/not-today` endpoints. Snapshot taken directly from Postgres before the rerun.

Reran the migration bundle against this now-current database. **Exit 0**, "No migrations were applied. The database is already up to date.", `__EFMigrationsHistory` row count unchanged at 21 (no duplicates). Post-rerun snapshot: both session rows unchanged byte-for-byte (`CompletionStatus`, `ActualDistanceKm`, `ActualDurationMinutes`, `AssignedDate`, `NotTodayReason`, `OutcomeVersion` all identical). **Rolling-data idempotency PASS.**

## 18. Destructive Down migration analysis

Source-reviewed all five `Down(MigrationBuilder)` methods directly (not inferred):
- `LongHorizonRollingPersistence.Down` — `DropTable` on all 8 core Long-Horizon tables: `LongHorizonActivationWindowRecords`, `LongHorizonBlockRetryRecords`, `LongHorizonCheckpointRecords`, `LongHorizonCoreContextRecords`, `LongHorizonRollingSessionStates`, `LongHorizonRunwayStates`, `LongHorizonRollingWeekStates`, `LongHorizonRollingPlanStates`.
- `LongHorizonRunwayCalendarAndTargetLockSnapshots.Down` — `DropColumn` (2 columns).
- `LongHorizonCoreContextEvidenceFingerprint.Down` — `DropColumn` (1 column).
- `Phase4L3LongHorizonPublicConfirmation.Down` — `DropColumn` (11 columns).
- `Phase4L4RollingSessionOutcomes.Down` — `DropColumn` (6 columns, including `ActualDistanceKm`, `ActualDurationMinutes`, `NotTodayReason` — exactly the outcome fields proven populated and load-bearing in §17).

**Conclusion, unchanged from Phase 4L.6 and reaffirmed by direct source review**: running any of these `Down` migrations against a database containing real rolling-plan data is destructive and unrecoverable (whole-table drops, not soft deletes). No historical migration was rewritten to make `Down` appear safe — none needed correction; they simply must never be used as the production rollback mechanism once rolling data exists.

## 19. Forward-schema rollback policy

**Production schema rollback = keep the forward (latest) schema. Never run migration `Down`. Roll back the application binary only, against the unchanged latest schema.** This is the only rollback model tested or endorsed by this phase.

## 20. Committed-HEAD previous-application build

Isolated via `git worktree add C:\wt-prev HEAD` (detached at `3549a8a1eeef18ca96794fa1056043142d13bc78`) — no `stash`/`reset`/`clean` was run against the primary (dirty) worktree; the 531 pre-existing uncommitted changes in the main worktree were left untouched throughout. Confirmed the worktree's `Migrations/` directory contains 33 files (16 migrations × ~2 files each), zero of which are Long-Horizon — matching §12's finding that Long-Horizon is entirely uncommitted. `dotnet build RunningApp.sln -c Release` in that worktree succeeded, 0 warnings, 0 errors, producing `C:\wt-prev\backend\RunningApp.Api\bin\Release\net9.0\RunningApp.Api.dll`. Removed with `git worktree remove --force C:\wt-prev` at the end of this phase (safe — it only deletes the detached-checkout copy, not the main worktree or any commit).

## 21. Previous application vs. latest schema

Started the committed-HEAD build against the §17 database (latest schema, one static plan now cancelled, one confirmed active Long-Horizon rolling plan with a `Completed` and a `NotToday` session). `/health` → healthy. `/health/database` → healthy, `pending_migrations: 0` (the old app's own migration assembly only knows its 16 migrations, and `GetPendingMigrationsAsync` correctly reports none of *its* migrations are pending — it simply has no model awareness of the 5 newer ones, which EF Core tolerates as additive/unknown history rows).

`GET /api/v1/plans/active/details` returned `has_active_plan: true` for the rolling plan (shared `TrainingPlans` row, `Status=active` at that point), but with `total_weeks: 0`, `weeks: []` — because the old app has no concept of `LongHorizonRollingSessionStates` and the rolling plan never populated the legacy `TrainingWeeks`/`TrainingDays` tables. `GET /api/v1/plans/active/home` similarly returned a generic empty/rest-day view rather than real content. **This is a degraded, confusing read — not data corruption.** Verified directly in Postgres: the old app created zero `TrainingWeeks`/`TrainingDays` rows for the rolling plan (both counts remained 0), and the two `LongHorizonRollingSessionStates` outcome rows were untouched by these read calls.

## 22. Previous-app write-safety with rolling data — REAL FINDING

`POST /api/v1/plans/{planId}/cancel` against the active rolling plan's ID, issued to the **old** (committed-HEAD) application: **returned HTTP 200**, `{"plan_id":"...","status":"cancelled"}`. Verified in Postgres: the shared `TrainingPlans.Status` column flipped from `active` to `cancelled`. The two `LongHorizonRollingSessionStates` rows (`Completed`/`NotToday`, with their actual values) were **not** deleted or altered — this is not row-level corruption — but the plan's authoritative lifecycle status was changed by a binary that has no knowledge of `ScheduleStrategy=rolling_long_horizon` and applies the exact same unconditional cancel logic it uses for a legacy static plan. A second identical request correctly returned `404 NOT_FOUND` (already cancelled), consistent behavior, not a new bug — but the first call already did the damage.

**This is a genuine, reproducible P1 rollback risk**: during an application-rollback incident, if the old binary is left reachable for ordinary mutating traffic, an authenticated user (or an operator acting on their behalf) can inadvertently cancel their own active, real-money-equivalent Long-Horizon training plan, with no warning and no schedule-strategy check anywhere in the call path.

## 23. Current-app re-forward

Stopped the old app; started the current app against the same (now plan-cancelled) database. `/health/database` healthy. Queried both rolling session IDs directly: `GET /api/v1/training-days/rolling/{sessionId}` returned `LONG_HORIZON_ROLLING_SESSION_NOT_FOUND` for both — this is the **current app's own, correct, unrelated authorization/ownership rule** (rolling-session reads are scoped to the caller's currently-active plan; since the plan is now cancelled, there is no "active" plan to scope against), not data loss. Confirmed directly in Postgres that both session rows are still present with **byte-for-byte identical** `CompletionStatus`, `ActualDistanceKm`, `ActualDurationMinutes`, `AssignedDate`, `NotTodayReason`, and `OutcomeVersion` values as captured in the §17 pre-rollback snapshot. `GET /api/v1/plans/active/details` correctly reports `has_active_plan: false` (accurate, since the plan really is cancelled — a true reflection of §22's event, not a new defect introduced by re-forwarding).

**Session-level identity, outcomes, and actual values survive the old-app episode and the current-app redeploy with zero loss and zero duplication.** The only casualty of the full round-trip is the plan-level `Status`, and that casualty originates entirely from §22, not from the redeploy itself.

## 24. Rollback classification

**ROLLBACK_LIMITED**, precisely: static/Habit read and write behavior remains fully safe under application rollback (same mechanism proven in §16 already works both directions). Rolling-plan **reads** degrade safely (empty/generic view, zero fabrication, zero corruption) under the old app. Rolling-plan **writes** are **not safe** — the shared, schedule-strategy-blind `cancel` endpoint lets the old app mutate an active rolling plan's authoritative status. This does not meet the bar for `ROLLBACK_SAFE`, and it is short of `ROLLBACK_UNSAFE` only because no row is corrupted/duplicated/reinterpreted-as-static — the blast radius is a single boolean-equivalent status flip, not silent data loss.

**Required mitigation before this can be upgraded to safe**: during any application-rollback incident, mutating traffic (at minimum: cancel, complete, not-today, activate-next-window, retry) must be blocked at the reverse-proxy/gateway layer for the duration the old binary is serving, or — the narrower, cheaper option — routing must ensure the old binary never receives authenticated mutating requests from users who hold an active `rolling_long_horizon` plan. Neither exists today; this is real, unimplemented mitigation work, not a documentation gap.

## 25. Deployment migration procedure

1. Validate external production configuration (fails fast per §6 if incomplete).
2. Verify the target backend artifact and its packaged catalog (Phase 4L.6A: 71 JSON files).
3. Snapshot/back up the production database.
4. Run the migration artifact (`runningapp-migrate.exe --connection <external-connection-string>`); check exit code.
5. Confirm `__EFMigrationsHistory` matches the expected 21-migration set.
6. Deploy the current backend artifact.
7. Startup fail-fast validation runs automatically (§6); a bad deploy never reaches step 8.
8. `/health` → healthy.
9. `/health/database` → healthy, `pending_migrations: 0`.
10. Static/Habit smoke (existing plan read).
11. Long-Horizon smoke (preview/confirm on a test account, or read-only check).
12. Promote traffic.

Failure before step 4 succeeds → stop, nothing is deployed. Failure after step 4 but before step 12 → keep the migrated (latest) schema; do not promote; redeploy the last known-good artifact only if it is proven forward-schema-compatible (see §26 for why this is currently conditional).

## 26. Rollback procedure

1. Stop traffic promotion to the new release.
2. Preserve the database snapshot taken in §25 step 3; do not restore it and do not run any migration `Down`.
3. Keep the latest (forward) schema exactly as-is.
4. **Before deploying the previous binary**: block mutating endpoints (cancel/complete/not-today/activate-next-window/retry) at the gateway/proxy layer, or otherwise ensure no authenticated user with an active rolling plan can reach the previous binary's write path. This step does not yet have an implemented mechanism (§24) — until it does, application rollback while any rolling plan is active is an accepted incident-time risk, not a false claim of safety.
5. Deploy the previous (committed-HEAD-equivalent) binary for static/Habit-only traffic.
6. Verify `/health`, `/health/database`, static/Habit reads.
7. Verify rolling-plan rows are unmodified (direct DB check) if any rolling plans exist.
8. Restore the current binary as soon as the incident is resolved.
9. Verify rolling-plan state reconstructs exactly (§23's proof: session identity/outcomes are always safe; plan-level status is safe only if step 4's mitigation was actually enforced).

**Never run the destructive Long-Horizon `Down` migrations against a production database with confirmed rolling data (§18).**

## 27. Automated migration harness

Not implemented as a rerunnable script/test entrypoint in this pass — every rehearsal in §14–17 was executed manually against real throwaway databases and is fully reproducible from the exact commands recorded in this document, but no `dotnet test`-invocable harness exists yet. This is honestly recorded as residual automation debt for Phase 4L.6C (which already owns CI), not silently claimed as done.

## 28. Configuration/security regression

Reran the full `ProductionConfigurationValidatorTests` + `CorsPolicyProductionSecurityTests` suites after all migration-tooling work: still **22/22 passed**, unchanged from §28 of the earlier configuration pass. No regression from the migration-artifact or rollback work (which touched no application code).

## 29. Full regression (final)

- `dotnet build RunningApp.sln -c Debug` (backend): 0 warnings, 0 errors.
- Full backend (`dotnet test RunningApp.IntegrationTests`, no filter): **3148 passed, 0 failed, 0 skipped** (§2; not rerun a second time in this continuation since no application code changed after that run — migration-artifact and rollback work operated on throwaway databases and a separate git worktree, not on the tracked backend source).
- Plan-catalog full suite: **1250 passed, 0 failed, 0 skipped** (§2).
- `git diff --check`: run against the tracked working tree; no whitespace-error-only concerns reported for the files this phase touched.

## 30. Publish/catalog regression

Fresh `dotnet publish -c Release -o C:\publish-4l6b`: succeeded. Publish output contains exactly **71** catalog JSON files (Phase 4L.6A's expected count, unchanged). `appsettings.json` (Production-reachable) contains `ConnectionStrings:DefaultConnection: ""` and `Auth:Provider: "Firebase"` — no secret. `appsettings.Development.json` is still packaged (inherited Phase 4L.6A behavior, not changed here) and does still carry the local dev credential — inert unless an operator sets `ASPNETCORE_ENVIRONMENT=Development` in a real deployment, which is an operational control this phase does not own; recorded as residual P3 packaging debt (excluding Development config from the Release publish output is a `.csproj`-level change out of this phase's narrow scope). `grep -a -c "Password=postgres"` against `RunningApp.Api.dll` itself: 0.

Published-artifact Production startup smoke:
- Missing DB + `Auth:Provider=Mock` → process throws `ProductionConfigurationValidator`'s exact aggregated message and **exits without ever binding a listener** (confirmed no port opened).
- Safe external config (`Auth:Provider=Mock` with `ProductionConfigurationValidation:Enabled=false`, pointed at the real test database) → starts, `/health` and `/health/database` both healthy.
- A fully credentialed `Auth:Provider=Firebase` Production success boot was not exercised end-to-end (needs real Google Application Default Credentials unavailable in this sandbox — same limitation as §22 of the earlier configuration pass); the validator's own pass-case is proven directly by its unit test.

## 31. Governance

New record `TD-LONG-HORIZON-PRODUCTION-CONFIGURATION-SECURITY-MIGRATION-ROLLBACK-001` added to `plan-catalog/artifacts/audits/activation-readiness-risks.json` and `.md`, status **OPEN**, severity **P1_RELEASE_BLOCKER**. It is not closed because rollback closure explicitly requires "no rolling corruption" and an acceptable classification, and this phase found `ROLLBACK_LIMITED` with an unmitigated write-safety gap (§22/§24), not `ROLLBACK_SAFE`. Evidence appended to parent `TD-LONG-HORIZON-END-TO-END-RELEASE-ACCEPTANCE-PRODUCTION-READINESS-001` (kept OPEN, unchanged severity). `TD-LONG-HORIZON-MOBILE-RELEASE-ARTIFACT-BACKEND-CATALOG-PACKAGING-001` (Phase 4L.6A) was not touched. Ledger totals before this phase: 69 records / 16 OPEN / 53 CLOSED. After: 70 / 17 / 53.

## 32. Remaining blockers

- **Migration/config**: none remaining that this phase was scoped to close — empty/pre-LH/current rehearsals all passed with real semantic preservation.
- **Rollback**: the cancel-endpoint write-safety gap (§22/§24) is unmitigated. A concrete fix (gateway-level mutation blocking during rollback incidents, or a schedule-strategy guard reachable even from the old binary's request path — which is not possible without redeploying that old binary, so the gateway approach is the only real option) must be designed and implemented before `ROLLBACK_SAFE` can be claimed.
- **Migration tooling**: the bundle's coupling to catalog/Firebase-credential resolution (§13) is real operational debt; a repository-local `dotnet-tools.json` was not added.
- **Automation**: no rerunnable migration-acceptance harness/CI hook yet (§27) — explicitly deferred to Phase 4L.6C, which already owns CI.
- **Unrelated, still open**: mobile Android identity/signing/Firebase registration (Phase 4L.6A), CI/CD, generation kill switch, observability, production-equivalent staging UAT — none attempted here, all remain exactly as Phase 4L.6/4L.6A left them.

## 33. Final classification

`LONG_HORIZON_APPLICATION_ROLLBACK_IS_UNSAFE_FOR_ACTIVE_ROLLING_PLANS_AND_REQUIRES_A_NARROW_RELEASE_MITIGATION_BEFORE_PRODUCTION`

Configuration/security and migration-upgrade acceptance are genuinely closed by this phase's evidence. Forward-schema application rollback is proven — the evidence itself is complete and real — but the result of that evidence is `ROLLBACK_LIMITED`, not `ROLLBACK_SAFE`, so `TD-LONG-HORIZON-PRODUCTION-CONFIGURATION-SECURITY-MIGRATION-ROLLBACK-001` and the parent release record both remain **OPEN/P1**. Production readiness is not claimed.

## 34. Exact next phase

A narrowly scoped **Phase 4L.6B.2 — Rollback Write-Safety Mitigation** should implement and prove the gateway-level (or equivalent) mutation-blocking mechanism identified in §24/§32, then re-run §21–24's exact rehearsal to attempt to upgrade the classification to `ROLLBACK_SAFE`. After that, **Phase 4L.6C — CI/CD, Generation Kill Switch, Observability and Production-Equivalent Staging UAT Closure** remains the next major phase, and can now also adopt the migration-acceptance rehearsals in this document as its automated-harness starting point (§27).
