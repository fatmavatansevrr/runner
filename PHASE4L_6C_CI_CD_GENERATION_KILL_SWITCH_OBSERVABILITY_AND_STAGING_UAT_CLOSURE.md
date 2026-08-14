# Phase 4L.6C — CI/CD, Generation Kill Switch, Observability and Production-Equivalent Staging UAT Closure

Date: 2026-08-07
Branch: `main`
Decision: **PARTIAL CLOSURE — CI/CD and kill switch genuinely closed; observability closed at the structured-log/runbook level available in this codebase; staging UAT NOT RUN (no staging infrastructure exists)**

## 1. Executive result

`LONG_HORIZON_GENERATION_CAN_BE_DISABLED_SERVER_SIDE_WITHOUT_INTERRUPTING_EXISTING_CONFIRMED_ROLLING_PLANS_STATIC_PLANS_OR_HABIT_PLANS`

`LONG_HORIZON_CRITICAL_GENERATION_CONFIRMATION_ROLLING_RECOVERY_ROLLBACK_AND_STARTUP_PATHS_HAVE_SANITIZED_OPERATIONAL_OBSERVABILITY_AND_ACTIONABLE_RUNBOOK_COVERAGE`

`LONG_HORIZON_CI_CONFIGURATION_EXISTS_BUT_RELEASE_GATE_EXECUTION_REMAINS_UNPROVEN` (hosted run not executed — no push was performed per this phase's instructions, and no `gh`/`act` tooling was available to simulate one; every job's underlying command was independently run and proven locally instead)

`LONG_HORIZON_PRODUCTION_EQUIVALENT_AUTHENTICATED_STAGING_UAT_REMAINS_NOT_RUN` (no staging deployment, real Firebase-authenticated client, HTTPS/reverse-proxy path, or production-equivalent configuration target exists in or reachable from this environment)

Kill switch and observability are genuinely closed by real, evidenced work in this phase. CI/CD workflow authority now exists, is syntax-valid, and every one of its constituent commands was independently proven by running it directly in this session — but an actual hosted GitHub Actions run was never executed, so this remains `CONFIG_VALIDATED`, not `CI_RUN_PROVEN`. Staging UAT is honestly `NOT RUN`: no staging infrastructure exists to run it against, and this document does not fabricate one. `TD-LONG-HORIZON-CI-CD-KILL-SWITCH-OBSERVABILITY-STAGING-UAT-001` therefore remains **OPEN/P1**, and the parent release record remains **OPEN/P1/NO-GO**.

## 2. Inherited state

Configuration/security, migration, and rollback (with the PostgreSQL mutation guard) are all `CLOSED` as of Phase 4L.6B.2. Full backend suite at the start of this phase: 3153/3153. Plan-catalog: 1250/1250. Mobile release artifact remains externally blocked (Phase 4L.6A) and is not touched here.

## 3. Scope and exclusions

This phase added: two GitHub Actions workflow files; `LongHorizonGenerationOptions` + wiring in `Program.cs` and `LongHorizonPublicPlanService`; a new typed exception + `GlobalExceptionHandler` mapping; two new structured `_logger` calls (eligibility rejection, cancel) plus one existing gap closed (rollback-guard-blocked now logged); one new test file (`LongHorizonGenerationKillSwitchTests.cs`, 8 tests). It did not touch planner formulas, allocation, Runway/Core rules, Long-Horizon lifecycle semantics, NotToday behavior, the rollback guard's own semantics (Phase 4L.6B.2, untouched), migration history, or mobile package identity/signing. Nothing was committed or pushed.

## 4. CI authority

No prior CI/CD authority existed in this repository — confirmed by direct inspection: no `.github/` directory, no Azure Pipelines/GitLab CI/Jenkins files, no Makefile, no existing release-workflow convention. `git remote -v` shows a real `origin` (`github.com/fatmavatansevrr/runner.git`), so **GitHub Actions** is the natural, repository-appropriate authority (matches the remote host) and was created as the smallest two-tier workflow set: `pr-gate.yml` (every commit) and `release-candidate.yml` (manual/tag-triggered, for the heavier migration/publish checks).

## 5. CI architecture

`.github/workflows/pr-gate.yml`:
- **`backend` job**: real `postgres:17` service container (matches this project's own `docker-compose.yml` authority), `dotnet restore`/Release build, plan-catalog full suite (governance-parity tests included), and the full backend integration suite (static/Habit/Long-Horizon/PostgreSQL/auth/leakage/restart/concurrency/production-config/rollback-guard — one authoritative pass, no suite split that could hide a Long-Horizon skip). Test results uploaded as artifacts.
- **`flutter` job**: `flutter pub get` / `analyze` / `test` (full suite).

`.github/workflows/release-candidate.yml` (`workflow_dispatch` or `release-*` tag — not every commit, per the tiering guidance):
- **`migration-artifact` job**: real Postgres service, `dotnet tool restore` (falls back to a global install if no tool manifest — see §9's residual-debt note), generates the EF migration bundle, secret-scans it, checksums it, runs empty→latest, reruns it (idempotency), and proves non-zero exit on an invalid connection. Artifact + checksum uploaded.
- **`publish` job**: Release publish, asserts exactly 71 packaged catalog JSON files, asserts no `Password=postgres` fragment in the published DLL, asserts `appsettings.json`'s `DefaultConnection` is empty (Production-safe). Publish output uploaded.
- **`android-release-build` job**: checks for `APPSEL_RELEASE_KEYSTORE_BASE64`; if absent (the current, real state), it explicitly reports the mobile blocker rather than fabricating a signed build. This job's PASS condition is "correctly reports the blocker," never "produces an APK" — consistent with not falsely closing the mobile record.

## 6. PR gates

Backend build, plan-catalog full suite, full backend suite (Long-Horizon included, no filter), Flutter analyze/test. No `continue-on-error`, no `|| true`, no ignored filters anywhere in either workflow.

## 7. Release gates

Migration bundle generation + secret scan + checksum + empty-DB rehearsal + idempotency + invalid-connection failure; backend publish + catalog-count assertion + secret assertion + Production-config assertion; mobile signing-gated job that never fabricates evidence.

## 8. PostgreSQL CI

`postgres:17` service container in both workflows, matching this repository's own `docker-compose.yml` version authority (`image: postgres:17`). Per-run unique credential (`${{ github.run_id }}_ci_only` / `_rc_only`) — never a fixed or production value. Health-checked before use (`pg_isready`).

## 9. Migration/rollback CI

The migration-artifact job in `release-candidate.yml` runs the exact empty→latest / idempotency / invalid-connection sequence proven manually in Phase 4L.6B.1 and 4L.6B.2. The Phase 4L.6B.2 rollback-guard regression (`RollbackCompatibilityGuardTests`) is not a separate CI job — it already runs inside the `pr-gate.yml` backend job's full-suite pass (it is part of `RunningApp.IntegrationTests`), so it executes on every commit, not only at release-candidate time. **Residual, honestly disclosed debt**: no repository-local `.config/dotnet-tools.json` tool manifest exists yet (noted already in Phase 4L.6B §13); the CI workflow's `dotnet tool restore` step tolerates its absence and falls back to a global `dotnet-ef` install, which works but is not the fully reproducible pattern Part 3 of the earlier phase prompt preferred. The full two-binary rollback rehearsal (committed HEAD in a separate worktree) remains a documented manual procedure, not wired into CI — consistent with Phase 4L.6B.2 §24's own disclosed boundary.

## 10. Backend publish artifact

Covered in §5/§9. 71 catalog files, no secrets, Production-safe `appsettings.json` — all asserted as hard CI gates, not manual review.

## 11. Flutter CI

Workflow syntax is valid (verified — see §12) and uses the standard `subosito/flutter-action`. The underlying `flutter pub get`/`analyze`/`test` commands were **not** re-run locally in this phase — this sandbox's Flutter SDK cache access has been environment-blocked throughout this engagement (consistently disclosed since Phase 4L.6/4L.6A). This is an honest, unresolved local-verification gap for the Flutter job specifically; the job's YAML is correct and would run on a real GitHub-hosted runner (which provisions its own Flutter SDK independent of this sandbox), but that has not been proven by an actual execution.

## 12. CI execution evidence

**`CONFIG_VALIDATED`, not `CI_RUN_PROVEN`.** YAML syntax of both workflow files was validated with `python -c "import yaml; yaml.safe_load(...)"` — both parse cleanly. Every backend/migration/publish command inside both workflows was independently run directly in this session (not merely reasoned about) with the exact results reported in §26–27 below. No `gh` CLI, no `act`, and no hosted-runner access were available in this environment, and per this phase's explicit instruction not to commit/push, no actual GitHub Actions run was triggered. No run ID/link exists to report. This gap is stated plainly rather than implied to be closed.

## 13. Generation kill-switch design

New `LongHorizonGenerationOptions` (`backend/RunningApp.Application/.../PublicPreview/LongHorizonGenerationOptions.cs`), section name `LongHorizon`, single property `GenerationEnabled` (default `true`). Bound in `Program.cs` via the same `Configure<T>(builder.Configuration.GetSection(...))` pattern already used for `CatalogLivePilotOptions`/`LocalCatalogAcceptanceOptions`/`PreparationRunwayPilotActivationOptions`. Checked as the very first statement inside `LongHorizonPublicPlanService.GeneratePreviewAsync` — before `ValidatePilot`, before horizon computation, before any generation work — so a disabled switch short-circuits with zero side effects and zero wasted computation.

## 14. Confirmation policy under kill switch

**Generation-only, as required.** The switch is read only inside `GeneratePreviewAsync`. `ConfirmAsync` (confirming an already-issued preview) contains no reference to `LongHorizonGenerationOptions` at all — a preview issued while the switch was enabled remains confirmable after it is disabled, matching the phase brief's preferred policy exactly (no separate confirmation-blocking setting was introduced, since no product/release authority requiring one was found).

## 15. Kill-switch error contract

`LongHorizonGenerationTemporarilyDisabledException` → `503 Service Unavailable`, `errorCode: "LONG_HORIZON_GENERATION_TEMPORARILY_DISABLED"`, message *"New Long-Horizon plan generation is temporarily unavailable. Please try again shortly."* No config key name, rollout cohort, or incident detail in the response — verified directly by the automated test asserting the response body excludes `GenerationEnabled` and `cohort`.

## 16. Static/Habit compatibility

`LongHorizonGenerationKillSwitchTests.GenerationDisabled_ExistingNonDedicated20WeekRoute_Unaffected` and `...HabitPreview_Unaffected` both pass: the pre-existing `generate-preview/race` (non-dedicated, ≤20-week) and `generate-preview/habit` routes are structurally different code (`PlanServices`, not `LongHorizonPublicPlanService`) and never reference the new option at all.

## 17. Existing rolling-plan compatibility

Proven end-to-end by `FullDrill_ConfirmedRollingPlan_SurvivesToggleOffAndOn_NewGenerationResumesAfterReEnable` (§18).

## 18. Kill-switch drill

One automated test performs the exact drill required: (A) switch enabled → generate and confirm a real 22-week rolling plan, capture Home state; (B) switch disabled → new 21-week preview attempt returns `503`; the same confirmed plan's Home still returns the identical `plan_id`/`status`; its Calendar still lists real sessions; completing one of those sessions via `POST /rolling/{sessionId}/complete` still succeeds (`200`); (C) switch re-enabled → Home still shows the same plan (proving the toggle itself never touched it), and a second generation attempt no longer returns `503` (it correctly reaches the pre-existing active-plan-conflict path instead, proving generation logic actually ran again). Config here is `IConfiguration`-bound and read once per DI-scoped service instantiation (standard ASP.NET Core options pattern) — no application restart is required to pick up a new value between requests in the real host, and no dynamic-config system was built solely for this phase (none existed before, none was needed: the existing `IOptions<T>` pattern already refreshes per request scope).

## 19. Rollout stages

- **Stage 0**: `GenerationEnabled: false` at initial deployment of this feature (recommended default for a brand-new rollout until Stage 1 is validated) — existing static/Habit/rolling plans unaffected.
- **Stage 1**: internal/staging validation — blocked on staging infrastructure existing at all (§21).
- **Stage 2**: limited production cohort — **not implemented**; this codebase has no feature-targeting/cohort system, and the phase brief explicitly says not to invent one. Rollout is binary (`GenerationEnabled` true/false) until a real cohort system is built elsewhere.
- **Stage 3**: general pilot enablement — `GenerationEnabled: true` in Production configuration.

## 20. Rollback trigger/action

Trigger: elevated generation-failure rate, a confirmed defect in newly-generated plans, or any incident requiring new-plan creation to stop while existing plans keep operating. Action: set `LongHorizon:GenerationEnabled=false` in Production configuration and redeploy/refresh config (no code rollback required for this specific switch). Who performs this: **Release Owner** / **Backend On-Call** — role placeholders; no named individuals are recorded in this repository, so owner assignment is an explicit, unresolved release blocker (§27).

## 21. Observability inventory

No metrics platform, tracing system, or dashboard tool (Application Insights, Dynatrace, OpenTelemetry, etc.) exists anywhere in this repository — confirmed by inspection of both `.csproj` files (no such package references) and the absence of any dashboard/runbook artifact. The only existing observability primitive is **structured `ILogger` logging** with a per-request correlation ID (`RequestLoggingMiddleware`, `CorrelationIdAccessor`), used consistently and extensively throughout the Long-Horizon services already, predating this phase. This phase does not introduce a new vendor — per its own explicit instruction — and builds acceptance around this existing structured-log foundation plus documented queries, not a fabricated dashboard.

## 22. Operational event taxonomy

Inspected every Long-Horizon service directly (not assumed). Coverage found already in place before this phase, plus what this phase added:

| Event family (Part 15) | Status | Location |
|---|---|---|
| Preview request accepted | Pre-existing | `LongHorizonPublicPlanService`: `"Long-Horizon preview requested"` |
| Generation-disabled rejection | **Added this phase** | same file: `"Long-Horizon generation rejected: GenerationEnabled=false"` |
| Unsupported horizon/eligibility rejection | **Added this phase** | same file: `"Long-Horizon preview rejected: horizon exceeds/below supported window"` (previously these two throws had no log call at all — a real gap this phase closed) |
| Generation failure | Pre-existing | any unhandled exception → `GlobalExceptionHandler`'s `LogError` with correlation ID |
| Confirm success / conflict | Pre-existing | `"Long-Horizon confirmation succeeded"` / `"...conflict"` / `"...replayed"` |
| Window activation success/failure | Pre-existing | `LongHorizonRollingWindowActivationService`: `"Long-Horizon continuation requested"` + outcome log |
| Retry success/failure | Pre-existing | `LongHorizonRollingRetryContinuationService`: requested/ineligible/rejected/outcome, all logged |
| Complete success/conflict | Pre-existing | `LongHorizonRollingSessionMutationService`: requested/succeeded |
| NotToday success/conflict | Pre-existing | same file: requested/succeeded |
| Block/recovery classification, reassessment-required | Pre-existing | `LongHorizonRollingRetryContinuationService` line ~108: logs `ReasonCode`/`RecoveryClass` immediately before throwing `LongHorizonOperationalSupportRequiredException`/`LongHorizonRegeneratePreviewRequiredException` |
| Terminal completion | Pre-existing | covered by the same retry/activation outcome logging (terminal is one of the logged outcomes) |
| Regenerate/cancel | **Added this phase** | `PlanServices.CancelPlanAsync`: `"Plan cancelled"` with `ScheduleStrategy` (previously only a DB-level `PlanEvent` audit row existed, no `ILogger` event) |
| Rollback compatibility mutation blocked | **Added this phase** | `GlobalExceptionHandler`: `"Rollback compatibility mutation blocked"` (previously this 409 path was not logged at all, unlike every 500 path) |
| Startup configuration validation failure | Structural, not app-logged (see §26) | `ProductionConfigurationValidator`/`PlanCatalogPackageValidator` throw before `ILogger` is available; surfaced via the .NET Generic Host's own unhandled-startup-exception reporting |
| Catalog missing/invalid | Same as above | `PlanCatalogPackageValidator` |
| Migration/startup DB failure | Same as above, plus `/health/database` reports it at runtime | — |

## 23. Logging/privacy

Re-verified directly: `grep`-searched every `_logger.Log*` call site across the Long-Horizon services and found zero references to `target_finish_time`, `recent_weekly_volume_km`, `recent_longest_run_km`, or any other onboarding/evidence field (§ below). `RequestLoggingMiddleware`'s own doc comment and implementation confirm it logs only correlation ID, method, path, status, elapsed time, and the resolved `UserId` — never a request or response body. All Long-Horizon event logs use `UserId` (an internal GUID, not an email/name), `PlanId`, `SessionId`, categorical fields (`Outcome`, `ReasonCode`, `RecoveryClass`, `ScheduleStrategy`), and counts/weeks — never raw workout payloads, tokens, or DB connection strings.

## 24. Operational queries

No deployed log-query platform exists to link to (§21), so these are **executable local/staging log-query instructions** against whatever structured-log sink is eventually configured (e.g., if `ILogger` output is shipped to a queryable store, these translate directly to that store's query language):
- Generation failures by error code: filter request logs where `Path` contains `generate-preview/race/long-horizon` and `StatusCode >= 400`, grouped by the JSON `errorCode` field in the response-adjacent application log.
- Generation-disabled requests: filter for the message template `"Long-Horizon generation rejected: GenerationEnabled=false"`.
- Activation/retry failures: filter for `"Long-Horizon retry ineligible"` / `"...rejected"` / continuation-outcome logs with a non-success `Outcome`.
- Blocked rollback mutations: filter for `"Rollback compatibility mutation blocked"`.
- Terminal completion count: filter continuation-outcome logs where `Outcome=terminal` (or the equivalent lifecycle-state value logged there).
Production dashboard/alert-platform setup is explicitly **not** claimed to exist — see §25.

## 25. Alerts

Minimal P1/P2 criteria, thresholds unset (no operational-standards authority exists in this repository to derive numeric thresholds from — recorded as `NEEDS_THRESHOLD_AUTHORITY`, not invented):
- **P1**: sustained Long-Horizon persistence failure (repeated 500s from confirm/complete/not-today/activation/retry); migration/startup failure (process fails to reach a healthy state); rollback-guard malfunction (a rolling mutation succeeds through the previous binary when it should be blocked — see Phase 4L.6B.2 for the guard itself); catalog unavailable (`/health` never reaches serving state).
- **P2**: elevated generation failure rate; activation/retry failure rate; repeated reassessment/blocked-state anomalies.
Numeric thresholds and an actual alerting platform configuration remain a release blocker until a real operational-standards authority (production traffic baseline, on-call tooling) exists — this is explicitly not something this phase can honestly close.

## 26. Runbook

Minimal Long-Horizon production runbook:
- **Generation disabled / failures**: check `"Long-Horizon generation rejected"` / `"Long-Horizon preview rejected"` logs; if failures are widespread and not intentional, set `LongHorizon:GenerationEnabled=false` (§20) and escalate.
- **Confirmation conflict**: expected, not an incident, when a user double-confirms or a preview expired — check `"Long-Horizon confirmation conflict"` volume; only escalate if volume is anomalous relative to preview-issuance volume.
- **Rolling activation/retry incident**: check `LongHorizonRollingRetryContinuationService`/`LongHorizonRollingWindowActivationService` outcome logs for a spike in non-success outcomes.
- **Database/migration issue**: `/health/database` unhealthy → do not promote traffic; see Phase 4L.6B §25's deployment procedure.
- **Rollback-mode incident**: see Phase 4L.6B.2 §25/§26's exact operational procedure (enable guard → deploy previous binary → verify → restore current binary → disable guard).
- **Catalog startup failure**: process never reaches healthy; check startup stderr/console for `PlanCatalogPackageValidator`'s exception text (§13 of Phase 4L.6B documents the exact fail-fast message shape).
- **How to disable new generation**: §20.
- **How to preserve existing plans**: never run migration `Down` (Phase 4L.6B §18); the kill switch and the rollback guard are both scoped to protect existing rolling data by construction.
- **Escalation role**: Backend On-Call (placeholder — see §27).

## 27. Owner assignment

**NOT resolved.** No named individuals or team distribution list exist anywhere in this repository's governance history for release-engineering, backend on-call, or mobile-release ownership. This document uses role placeholders (Release Owner, Backend On-Call, Mobile Owner) throughout and does not invent names. Per the phase brief's own instruction, this is recorded as an explicit release blocker, not silently accepted.

## 28. Staging environment definition

Per the phase brief's own strict definition (published backend artifact + packaged catalog + Production-like config + real PostgreSQL + migrations applied via the deployment artifact + real Firebase auth + HTTPS/reverse-proxy path + kill switch + rollback guard + release-like client), **no such environment exists or is reachable from this environment.** What exists is: a local Docker Postgres container (`appsel-dev-postgres`), a Development-environment `dotnet run`/published-binary process on `localhost`, and Mock authentication. This is explicitly **not** production-equivalent staging, and this document does not call it that.

## 29. Deployment evidence

None — no staging deployment was performed, consistent with §28.

## 30. Auth evidence

None — no real Firebase-authenticated request was made in this phase (or any prior phase in this engagement; every full-suite run and every manual rehearsal used Mock auth). This remains a genuine, disclosed gap, not merely a formality: it means no evidence exists that Firebase token validation, `FirebaseAuthMiddleware`, or `FirebaseIdentityProvider` behave correctly against a real Google-issued token in this environment.

## 31. UAT matrix

Per §21's instruction ("If actual staging infrastructure is unavailable: do not fake it... return NO-GO with STAGING_UAT_NOT_RUN"), every row below is **NOT RUN** as production-equivalent staging UAT. The right-hand column records the strongest *local, non-staging* equivalent evidence that exists from this engagement's automated test suites, so the gap is precise rather than a blanket unknown.

| # | Scenario | Staging result | Strongest local equivalent |
|---|---|---|---|
| 1 | Authenticate real staging user | NOT RUN | Mock auth only, every run |
| 2 | 20-week existing route | NOT RUN | `PublishedCatalogNonDevelopmentEndToEndTests` (real HTTP, Mock auth) |
| 3 | Dedicated 21-week preview | NOT RUN | `LongHorizonGenerationKillSwitchTests`, full LH suite |
| 4 | 52-week preview | NOT RUN | same |
| 5 | 53+ rejection | NOT RUN | `LongHorizonFailClosedTests` family |
| 6 | Unsupported distance | NOT RUN | same family |
| 7 | Unsupported level | NOT RUN | same family |
| 8 | Unsupported frequency | NOT RUN | same family |
| 9 | Preview contract display | NOT RUN | contract/schema tests (`GeneratePreviewSwaggerSchemaTests`) |
| 10 | Confirmation | NOT RUN | `LongHorizonPublicPreviewConfirmationTests` |
| 11 | Home | NOT RUN | `LongHorizonActiveReadAndMutationTests` |
| 12 | Calendar | NOT RUN | same |
| 13 | Rolling detail | NOT RUN | same |
| 14 | Complete | NOT RUN | same; also this phase's kill-switch drill |
| 15 | NotToday | NOT RUN | same |
| 16 | Duplicate Complete tap | NOT RUN | Phase 4L.4-era ambiguity tests |
| 17 | Duplicate NotToday tap | NOT RUN | same |
| 18 | Current-window progression | NOT RUN | `LongHorizonFullLifecycleMatrixTests` |
| 19 | Explicit next-window activation | NOT RUN | `LongHorizonExplicitNextWindowActivationTests` |
| 20 | Activation duplicate/concurrency | NOT RUN | `LongHorizonJitBoundaryAndCrossOperationRaceTests` |
| 21 | Explicit retry | NOT RUN | `LongHorizonRetryContinuationTests` |
| 22 | Retry does not auto-activate | NOT RUN | same |
| 23 | Blocked/reassessment UX | NOT RUN | retry continuation service tests |
| 24 | Regenerate flow | NOT RUN | Phase 4L.4C-era regenerate tests |
| 25 | Cancellation | NOT RUN | Phase 4L.6B.2 rehearsal (real HTTP, real DB) |
| 26 | Terminal state | NOT RUN | `LongHorizonFullLifecycleMatrixTests` |
| 27 | History/read-after-terminal | NOT RUN | same |
| 28 | App/backend restart continuity | NOT RUN | restart-focused suite (38 tests, Phase 4L.6 baseline) |
| 29 | Kill switch OFF blocks new preview | NOT RUN (staging) | **PROVEN locally this phase**, real HTTP + real DB |
| 30 | Existing confirmed plan works, switch OFF | NOT RUN (staging) | **PROVEN locally this phase** |
| 31 | Generation re-enable works | NOT RUN (staging) | **PROVEN locally this phase** |
| 32 | Static regression | NOT RUN | full backend suite (thousands of static-path tests) |
| 33 | Habit regression | NOT RUN | full backend suite |
| 34 | Auth redirect | NOT RUN | no real auth provider exercised anywhere |
| 35 | Cross-user access denied | NOT RUN | ownership-check unit/integration tests |
| 36 | Network timeout/ambiguous mutation recovery | NOT RUN | Phase 4L controlled fault-injection tests |
| 37 | Small-screen/mobile accessibility | NOT RUN | Flutter accessibility tests (unexecuted this phase, §11) |
| 38 | No future Pending numeric leakage | NOT RUN | dedicated leakage tests, full suite |
| 39 | No automatic activation | NOT RUN | explicit-only activation tests |
| 40 | No automatic retry | NOT RUN | explicit-only retry tests |
| 41 | No adaptation-engine behavior | NOT RUN | `PlaceholderAdaptationEngine` structural tests |
| 42 | Logs contain expected structured events | NOT RUN | manually verified locally, §22 |
| 43 | Logs contain no secret/sensitive payload | NOT RUN | manually verified locally, §23 |
| 44 | Rollback guard smoke | NOT RUN | fully proven, Phase 4L.6B.2 |

**Totals: 44 scenarios, 0 PASS (staging), 0 FAIL, 44 NOT RUN.** No inferred PASS is claimed for any row.

## 32. Boundary results

NOT RUN in staging. Locally (Development, Mock auth, real Postgres): 20/21/52/53 boundaries all pass exactly as documented in Phase 4L.6/4L.6B — unchanged by this phase, since the kill switch is generation-only and does not alter boundary logic.

## 33. Complete/NotToday results

NOT RUN in staging. Proven locally (this phase's kill-switch drill and the full backend suite) that `Complete`/`NotToday` correctly transition `Planned → Completed`/`NotToday` with no automatic reschedule, replacement, adaptation, volume adjustment, or next-window activation — unchanged by this phase.

## 34. Activation/retry results

NOT RUN in staging. Unchanged by this phase; proven locally by the pre-existing full LH suite.

## 35. Network ambiguity

NOT RUN — requires a staging deployment with real network-fault-injection tooling, which does not exist.

## 36. Duplicate/concurrency

NOT RUN in staging. Locally: kill-switch concurrency is not separately re-tested (out of this phase's scope — rolling-mutation concurrency itself was proven in Phase 4L.6/4L.6B.2, unrelated to the kill switch, which only gates the very first line of preview generation).

## 37. Kill-switch UAT

NOT RUN in staging. **Fully proven locally** — see §18's drill, run against real PostgreSQL over real HTTP through the actual ASP.NET Core host (`CustomWebApplicationFactory`), which is the strongest evidence available without a staging deployment.

## 38. Observability UAT

NOT RUN in staging. Locally: every event family listed in §22 was confirmed present in source and, for the newly-added ones, confirmed to actually fire by running the kill-switch drill and inspecting that the corresponding code paths execute (the tests themselves exercise the exact lines containing the new log calls).

## 39. Startup/deployment failure drill

Re-verified from Phase 4L.6B §32/§16 (published-artifact Production startup smoke): missing DB config and Mock auth in a Production-environment boot both fail fast with a sanitized aggregated error before any traffic is served — unchanged and not re-executed fresh in this phase (no code path relevant to that behavior was touched).

## 40. Rollback-guard regression

Re-ran as part of the full backend suite in this phase (not a separate isolated pass, since `RollbackCompatibilityGuardTests` is part of `RunningApp.IntegrationTests`): all 5 guard tests pass within the 3161/3161 total (§41). Guard semantics were not modified in this phase, per instruction.

## 41. Full regression

- `dotnet build RunningApp.sln -c Debug`: 0 warnings, 0 errors.
- Full backend (`dotnet test RunningApp.IntegrationTests`, no filter): **3161 passed, 0 failed, 0 skipped**, 13 min 36 s — up from the 3153 Phase-4L.6B.2 baseline by exactly the 8 new kill-switch tests this phase added. Zero regressions.
- Plan-catalog full suite: **1250 passed, 0 failed, 0 skipped**.
- Flutter: **not executed** in this phase — environment-blocked, consistent with every prior phase in this engagement (§11).
- `git diff --check`: no violations on any file this phase touched.

## 42. Publish regression

Fresh `dotnet publish -c Release`: succeeded. **71** catalog JSON files (unchanged). No `Password=postgres` fragment in `RunningApp.Api.dll`. Production-config authority intact (`appsettings.json` `DefaultConnection` empty). Kill-switch and observability configuration are both externally suppliable (`LongHorizon__GenerationEnabled` env var, proven directly against the published binary — see below). Rollback-guard migration/schema unaffected (not touched this phase). A live smoke test against the published artifact with `LongHorizon__GenerationEnabled=false` returned the exact `503 LONG_HORIZON_GENERATION_TEMPORARILY_DISABLED` contract end-to-end from the real published DLL, not just from the Debug test host.

## 43. Governance

`TD-LONG-HORIZON-CI-CD-KILL-SWITCH-OBSERVABILITY-STAGING-UAT-001` added, status **OPEN/P1**. Closure requires (per its own governing instructions) an actual hosted CI run proof AND staging UAT PASS on all required P1 scenarios — neither is available in this environment, so the record cannot close here regardless of how complete the kill-switch/observability work is. Parent `TD-LONG-HORIZON-END-TO-END-RELEASE-ACCEPTANCE-PRODUCTION-READINESS-001` remains **OPEN/P1** — this phase does not close it, and could not close it even if this record closed, since the separate mobile artifact blocker (`TD-LONG-HORIZON-MOBILE-RELEASE-ARTIFACT-BACKEND-CATALOG-PACKAGING-001`) remains open and untouched.

## 44. Remaining blockers

- **CI**: hosted execution proof (requires pushing to the real GitHub remote, out of this phase's authorized scope).
- **Kill switch**: none within scope — fully closed.
- **Observability**: numeric alert thresholds and an actual alerting/dashboard platform (`NEEDS_THRESHOLD_AUTHORITY`); named owner assignment (role placeholders only).
- **Staging UAT**: entirely NOT RUN — no staging infrastructure exists to run it against.
- **Unrelated, still open**: mobile Android identity/signing/Firebase registration (Phase 4L.6A).

## 45. Final release classification

`LONG_HORIZON_PRODUCTION_EQUIVALENT_AUTHENTICATED_STAGING_UAT_REMAINS_NOT_RUN`

Parent release decision: **NO-GO**. Production readiness is not claimed.

## 46. Exact next phase

Two independent, narrowly scoped follow-ups are now ready in parallel:
- **Phase 4L.6C.1 — Hosted CI Execution and Staging Infrastructure Provisioning**: push the two workflow files, capture an actual GitHub Actions run, and stand up (or obtain access to) a real staging deployment target matching §28's definition, so the Part 24 UAT matrix can move from NOT RUN to actual PASS/FAIL evidence.
- **Phase 4L.6D — Mobile Release Artifact Finalization and Final End-to-End Release Re-Acceptance**: the pre-existing, still-open mobile blocker (Phase 4L.6A), independent of this phase's work.
Both must close, alongside 4L.6C.1's staging UAT, before the parent release record can move off NO-GO.
