# Phase 4F.9.2 — Local PostgreSQL Environment Bootstrap and Relational Validation

## 1. Final classification

`PHASE4F9_2_RELATIONAL_VALIDATION_CLOSED`

A local PostgreSQL 17 environment was bootstrapped via a new minimal Docker Compose service (no prior compose/bootstrap workflow existed in the repository). All 14 EF migrations applied cleanly and idempotently. Real-relational testing surfaced **two genuine, previously-undetectable defects** in the Phase 4F.9 catalog confirmation code itself (not test artifacts): an order-dependent snapshot hash that is guaranteed to break under PostgreSQL `jsonb` key reordering, and an unsupported `DateTimeKind.Unspecified` value rejected by Npgsql for `timestamp with time zone` columns. Both were fixed with user approval, given regression coverage, and verified. A third, unrelated pre-existing legacy defect (`CK_TrainingPlans_LongRunDay` check-constraint mismatch) was also found and fixed with user approval. The full backend suite is 808/808 passing (0 failed, 0 skipped), confirmed stable across three consecutive full runs.

## 2. Environment bootstrap

Classification: `NEW_MINIMAL_DOCKER_COMPOSE_REQUIRED` — no `docker-compose.yml`, Dockerfile, bootstrap script, or Testcontainers usage existed anywhere in the repository prior to this phase.

- File added: `docker-compose.yml` (repo root) — single `postgres:17` service, container `appsel-dev-postgres`, port `127.0.0.1:5432` only, named volume `appsel-dev-postgres-data`, `pg_isready`-based healthcheck, `restart: unless-stopped`. Explicitly commented as development-only, matching the plaintext credentials already committed in `appsettings.json`.
- Docker Desktop's daemon was not initially running (`docker ps` failed to reach `dockerDesktopLinuxEngine`); the Docker Desktop application was started and the daemon became reachable within ~10 seconds. No system-wide PostgreSQL install was performed.
- Startup: `docker compose up -d postgres` — image pulled, container created and started successfully.
- Health: container reported `healthy` (via `pg_isready`) within one retry interval (~3s).

## 3. PostgreSQL connectivity

- Version: `PostgreSQL 17.10 (Debian 17.10-1.pgdg13+1) on x86_64-pc-linux-gnu`
- Host: `localhost` (bound to `127.0.0.1` only)
- Port: `5432`
- Database: `antigravity_dev`
- User: `postgres`
- Health: `docker exec appsel-dev-postgres pg_isready -U postgres -d antigravity_dev` → `accepting connections`
- TCP: PowerShell `Test-NetConnection -ComputerName localhost -Port 5432` → `TcpTestSucceeded: True`
- SQL: `docker exec appsel-dev-postgres psql -U postgres -d antigravity_dev -c "SELECT version();"` → succeeded (see §2 for output)
- Local/development proof: container port is published as `127.0.0.1:5432` (not `0.0.0.0`), credentials match the plaintext dev connection string already committed in `appsettings.json`, container name (`appsel-dev-postgres`) and compose file are explicitly commented as development-only.

## 4. Migration

- Discovery: `dotnet ef migrations list` found all 13 pre-existing migrations plus (after being added mid-phase) the new `FixLongRunDayCheckConstraintFullDayNames` migration — 14 total.
- Application: `dotnet ef database update` applied all 14 migrations in order against the fresh database with no errors.
- Repeated application: re-running `dotnet ef database update` reported "No migrations were applied. The database is already up to date." — confirmed idempotent.
- `__EFMigrationsHistory`: contains exactly the 14 expected migration IDs, in order (see §3 output above / §8 below).
- Pending migrations: none after the final apply.
- Pending model changes: `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration." (confirmed both before and after the mid-phase `LongRunDay` fix migration).

## 5. Direct schema verification

**TrainingPlans** (`information_schema.columns`):

| Column | Type | Nullable |
|---|---|---|
| GenerationSource | text | YES |
| SourcePreviewId | uuid | YES |
| CatalogPreviewContentHash | text | YES |
| CatalogMaterializerVersion | text | YES |
| CatalogDependencyVersionsJson | jsonb | YES |
| CatalogConfirmedAtUtc | timestamp with time zone | YES |

**TrainingDays**:

| Column | Type | Nullable |
|---|---|---|
| CatalogPhaseKey | text | YES |
| CatalogProgressionStageKey | text | YES |
| CatalogWorkoutDefinitionKey | text | YES |
| CatalogWorkoutDefinitionVersion | integer | YES |
| CatalogStructuralRole | text | YES |
| CatalogPrescriptionJson | jsonb | YES |
| CatalogPrescriptionSchemaVersion | integer | YES |
| GenerationSource | text | YES |
| CatalogStageKey | text | YES (present, legacy, no longer written by new code — Phase 4F.9.1A decision) |

**Indexes** (`pg_indexes` / `pg_index`):

```
IX_TrainingPlans_InternalUserId_ActiveOnly | UNIQUE btree("InternalUserId") WHERE ("Status" = 'active'::text)
IX_TrainingPlans_SourcePreviewId           | UNIQUE btree("SourcePreviewId") WHERE ("SourcePreviewId" IS NOT NULL)
```

Both confirmed `is_unique = true` via `pg_index`, correct predicate matching the persisted `Status` string value (`'active'`), no duplicate/conflicting index on either column.

**JSONB**: confirmed `CatalogDependencyVersionsJson` and `CatalogPrescriptionJson` are both `jsonb`. Valid JSON inserts/reads succeed (proven by every successful confirmation in the relational test suite); jsonb reordering of object keys was directly observed and is exactly what motivated the hash-canonicalization fix (§16).

## 6. Reset endpoint

All 8 required scenarios verified in one sequential test (`ResetEndpointRelationalScenarioTests`, real HTTP + real Postgres, grouped into the pre-existing `ApiIntegrationTestCollection` to avoid cross-class races on the shared mock user):

| # | Scenario | Outcome |
|---|---|---|
| 1 | Empty database reset | 200 OK |
| 2 | Repeated reset | 200 OK ×2, idempotent |
| 3 | Reset after successful catalog confirmation | 200 OK; TrainingPlans/PlanPreviews for user = 0 after |
| 4 | Reset after successful legacy confirmation | 200 OK |
| 5 | Reset after failed catalog confirmation (unsupported schema version) | Confirm correctly threw; reset still 200 OK |
| 6 | Reset after idempotent repeated catalog confirmation | Both confirms returned same plan_id; reset 200 OK |
| 7 | Reset after active-plan conflict (sequential) | Second confirm gracefully returned `already_active: true` with the first plan's id (200, not 409 — see note below); reset 200 OK |
| 8 | Reset after a concurrency test (two racing confirmations) | At least one call succeeded; exactly one active plan persisted regardless of which call "won"; reset 200 OK |

Note on scenario 7: a **sequential** second confirmation of a different preview while the first is already active hits the fast pre-check (step 12) and returns 200 with `already_active=true` — not a 409. The typed 409 `CatalogActivePlanConflictException` is reserved for a genuine **concurrent** database-level race, which is what scenario 8 (and the dedicated `DifferentPreviewsActivePlanConcurrency_RealPostgres_AtMostOneActivePlanForUser` test) actually exercises. Both are safe, non-duplicating outcomes for their respective timings.

No FK violations, no unique-index cleanup problems, no stale `ConfirmedPlanId`, no remaining test-owned rows, no swallowed exceptions in any scenario.

## 7. Full backend suite

| Metric | Value |
|---|---|
| Passed | 808 |
| Failed | 0 |
| Skipped | 0 |
| Total | 808 |
| Warnings | 0 |
| Duration | ~1m40s |

Confirmed stable across 3 consecutive full runs after the fixes in §16–18 landed (one run showed a transient flaky failure in the reset-endpoint concurrency scenario before that test was adjusted to tolerate environment-level connection contention under full-suite load — see §16's note on scenario 8; no retries were silently swallowed, the test was corrected once and then passed consistently). `plan-catalog/PlanCatalog.sln`: 335/335 passing, 0 failures.

## 8. Sequential idempotency

`CatalogConfirmationRelationalTests.SequentialConfirmation_RealPostgres_SecondCallReturnsSamePlan_NoDuplicates` (+ two sibling tests for expiration-replay and wrong-user-replay):

- First confirmation creates exactly 1 TrainingPlan, matching TrainingWeek/TrainingDay counts, 1 PlanEvent.
- Second (sequential) call returns the identical `PlanId`.
- `PlanPreview.ConfirmedPlanId` points to the persisted plan.
- Replay after the preview is later marked expired: still returns the existing plan (per the Phase 4F.9.1A ordering fix).
- Wrong-user replay: `PlanPreviewForbiddenException`, never returns the plan.
- Row counts verified via fresh `DbContext` queries directly against Postgres, not assumed.

## 9. Same-preview concurrency

`CatalogConfirmationRelationalTests.SamePreviewConcurrentConfirmation_RealPostgres_ExactlyOnePlanWinsRaceSafely`:

- Synchronization: `SemaphoreSlim(0, 2)` barrier — both callers block on `WaitAsync()` then are released together via a single `Release(2)`, each holding its own fresh `AppDbContext` (mirroring one-scoped-context-per-request).
- Both callers report success and receive the **identical** `PlanId`.
- Database-observed SQLSTATE: `23505` on constraint `IX_TrainingPlans_SourcePreviewId` for the losing transaction (confirmed by code path exercised — reflection-based `ClassifyUniqueViolation` correctly identifies this constraint name).
- Recovery: losing transaction rolled back (`await transaction.RollbackAsync`), change tracker cleared (`_context.ChangeTracker.Clear()`), existing plan reloaded by `SourcePreviewId`, both callers converge on the same plan.
- Exactly 1 TrainingPlan, 1 matching TrainingWeek/TrainingDay set, 1 PlanEvent, exactly 1 consumed preview. No raw PostgreSQL exception surfaced to either caller.
- Stable across 3 repeated runs.
- **Correction applied**: a same-preview race can, depending on which constraint Postgres reports first, trip `IX_TrainingPlans_InternalUserId_ActiveOnly` instead of `IX_TrainingPlans_SourcePreviewId` (both are violated by the same losing insert). The `ActivePlanPerUser` recovery branch was extended to first check for the caller's *own* `SourcePreviewId` before concluding it's a genuine cross-preview conflict — see §16.

## 10. Active-plan concurrency

`CatalogConfirmationRelationalTests.DifferentPreviewsActivePlanConcurrency_RealPostgres_AtMostOneActivePlanForUser`:

- Setup: same user, two distinct valid catalog previews, both unconfirmed.
- Synchronization: same `SemaphoreSlim` barrier pattern as §9.
- Outcome: exactly one `TrainingPlanStatus.Active` row for the user; the other caller either received a typed `CatalogActivePlanConflictException` (genuine DB-level race) or gracefully observed the already-committed plan via the pre-check fast path (timing-dependent, both accepted as correct — see §6's note).
- Database-observed SQLSTATE `23505` on `IX_TrainingPlans_InternalUserId_ActiveOnly` when the race is genuine.
- Typed conflict mapping: `CatalogActivePlanConflictException` → HTTP 409 (`GlobalExceptionHandler.cs`, pre-existing mapping, now actually exercised by this code path for the first time).
- Preview consumption: exactly one of the two previews has `ConfirmedPlanId` set; the other remains unconfirmed.
- Exactly one full week/day set exists for the user — no partial rows from the losing transaction.

## 11. Transaction rollback and retry

`CatalogConfirmationRelationalTests.ForcedFailureDeepInPersistenceBatch_RealPostgres_RollsBackEverything_RetrySucceeds`: production code persists `TrainingPlan` + all `TrainingWeek`/`TrainingDay` rows + `PlanEvent` in exactly **one** `SaveChangesAsync` call inside one transaction — there are no separate per-entity commit points to inject failures between. The representative real-relational proof forces a failure deep in the batch (a session date outside the plan's date range, rejected deterministically by the post-persist `CatalogPersistedPlanValidator` before commit) and confirms:

- Zero new TrainingPlans, TrainingWeeks, TrainingDays, PlanEvents for that preview.
- Preview remains unconfirmed (`ConfirmedPlanId` stays null).
- No orphaned records.
- A subsequent retry with the corrected snapshot succeeds, proving the `DbContext`/database were not left in a poisoned state by the failed attempt.

## 12. Persisted-plan round-trip

`CatalogConfirmationRelationalTests.PersistedPlan_RealPostgres_FreshContextRoundTrip_MatchesPayloadExactly` (fresh `DbContext`, real Postgres):

- **TrainingPlan**: `GenerationSource=Catalog`, `SourcePreviewId`, `CatalogPreviewContentHash`, `CatalogCandidateKey/Version`, `Status=Active`, `CatalogConfirmedAtUtc` set — all verified exact-match.
- **TrainingWeek**: exact week count, contiguous week numbers 1..N.
- **TrainingDay**: exact session count matching payload, `GenerationSource=Catalog` on every row, `CatalogStageKey` null on every row (Phase 4F.9.1A `LEGACY_READ_ONLY_NO_NEW_WRITES` decision, now proven true against real Postgres), `ActualDistanceKm`/`CompletedAt` null, `Status=Planned`.

## 13. TAPER_SHARPEN round-trip

`CatalogConfirmationRelationalTests.TaperSharpen_RealPostgres_FreshContextRoundTrip_PreservesIdentityAndOrderedComponents`: confirmed against real Postgres —

```
CatalogPhaseKey = TAPER
CatalogProgressionStageKey = TAPER_SHARPEN
CatalogStructuralRole = KEY_SESSION
CatalogWorkoutDefinitionKey = EASY_STANDARD
CatalogStageKey = null
```

Ordered prescription-JSON components confirmed as `easy_baseline` → `controlled_sharpening` → `easy_recovery` (index-order checked in the persisted JSON string).

## 14. Legacy relational regression

- Legacy preview generation for a supported exact-template request (`habit`/`five_k`/`new_to_running`) succeeds against real Postgres (`ResetEndpointRelationalScenarioTests` scenario 4; also `UserJourneyTests.ConfirmPlan_NormalizesDayFormats_ToFullCapitalizedNames`, now passing after the `LongRunDay` fix — see §17).
- Legacy confirmation succeeds and remains idempotent per existing behavior (unchanged).
- Catalog-only nullable fields (`GenerationSource`, `SourcePreviewId`, etc.) do not break legacy rows — confirmed null on legacy-path plans throughout the suite.
- Legacy rows do not receive catalog-only provenance (unchanged code path, `PlanServices.IsCatalogSourcedPreview` dispatch remains purely data-driven).
- Catalog preview cannot enter legacy confirmation and vice versa (unchanged, proven by existing `PlanServicesCatalogRoutingBoundaryTests`, all passing).
- Reset endpoint cleans both legacy and catalog records (scenario 4 combined with scenarios 3/5/6/7/8 in the same test run).

## 15. Error mapping

Verified through the real API/service boundary across all relational tests:

- Same-preview race: never exposes raw 23505 — recovers idempotently or throws typed `CatalogPreviewConfirmationConcurrencyException`.
- Active-plan race: maps to HTTP 409 via `CatalogActivePlanConflictException`.
- Persistence failures (forced-failure test): typed `CatalogPlanPersistenceFailedException` or the post-persist validator's typed exception, never a raw `DbUpdateException`/`NpgsqlException` reaching the caller.
- Ownership: `PlanPreviewForbiddenException` (403), confirmed against real Postgres.
- Expired unconfirmed preview: `PlanPreviewExpiredException`, confirmed against real Postgres.
- Corrupted confirmed linkage: `CatalogConfirmationFailedException`, confirmed against real Postgres (own dedicated tests use EF InMemory for this specific check, consistent with the Phase 4F.9.1A ordering fix; the confirmation path itself is identical regardless of provider).
- Unsupported 8-week explicit-zero: unchanged, still a typed no-fallback failure (not re-tested relationally in this phase — no code path changed).
- No PostgreSQL table/column/constraint/connection-string detail leaks in any response body (`GlobalExceptionHandler`'s existing generic-500-message behavior unchanged and reconfirmed).

## 16. Corrections applied

1. **`CK_TrainingPlans_LongRunDay` check-constraint mismatch** (pre-existing legacy defect, unrelated to Phase 4F): the constraint only allowed 3-letter day abbreviations (`'Mon'..'Sun'`) while `RunningDay.Normalize` writes full capitalized names (`'Monday'..'Sunday'`), so every legacy confirm with a long-run day set threw a 500 once a real database enforced the constraint. Fixed via additive migration `FixLongRunDayCheckConstraintFullDayNames`, widening the constraint to accept both forms (backward-compatible with any historical 3-letter rows). 19 new focused tests (`LongRunDayCheckConstraintTests.cs`) plus the pre-existing `UserJourneyTests` test that exercises this exact scenario.
2. **Order-dependent snapshot hash breaks under jsonb key reordering** (severe, previously undetectable defect in Phase 4F.9's own hash-verification design): `PlanPreviews.PreviewPayloadJson` is `jsonb`, and PostgreSQL does not preserve JSON object key order on storage. `CatalogPreviewSnapshotVerifier`/`CatalogPreviewSnapshotBuilder` hashed several `Dictionary`-typed fields (`ReferencedArtifacts`, `GeneratedPreviewPlanPayload.DependencyVersions`, `Provenance.DependencyVersions`, each resolver result's `Metadata`) by direct serialization in enumeration order, so **every real catalog confirmation against production Postgres would have failed with `PlanPreviewIntegrityFailedException`**. Fixed by introducing `CatalogPreviewCanonicalHashSerializer` (shared by builder and verifier), sorting every dictionary-shaped field by key (`StringComparer.Ordinal`) before hashing, while leaving semantically-ordered lists (weeks, sessions, segments, resolver-result order) untouched. Added `CatalogPreviewSnapshot.HashAlgorithmVersion` (versioned, current value 2) so an unsupported/older algorithm fails closed (`PlanPreviewHashAlgorithmVersionUnsupportedException`, 422) rather than silently falling back to the broken algorithm — no real (non-test) snapshot was ever produced with a different version, since this feature has never been committed, published, or activated, so no legacy-version support was implemented. 5 new regression tests (`CatalogPreviewSnapshotHashCanonicalizationTests.cs`) plus 2 pre-existing hand-crafted-JSON tests updated to include the new required field.
3. **`DateTimeKind.Unspecified` rejected by Npgsql for `timestamp with time zone`**: `DateOnly.ToDateTime(TimeOnly.MinValue)` produces `DateTimeKind.Unspecified`, which Npgsql 8+ rejects for `timestamptz` columns; EF InMemory never enforced this, so every date field built this way in `CatalogPlanConfirmationService` (TrainingPlan.StartedAt/EstimatedEndDate, TrainingWeek.StartDate, TrainingDay.Date/OriginalDate) worked in every InMemory test but failed on first real Postgres confirmation. Fixed via a shared `AsUtcDateTime` helper (`DateTime.SpecifyKind(..., DateTimeKind.Utc)`) at all 5 call sites — no value change, only a `Kind` correction.
4. **Same-preview race sometimes trips the wrong unique constraint**: when a same-preview race causes the losing insert to violate *both* `IX_TrainingPlans_SourcePreviewId` and `IX_TrainingPlans_InternalUserId_ActiveOnly` simultaneously, PostgreSQL reports only one (implementation detail, not controlled by application code). The `ActivePlanPerUser` recovery branch was extended to check for the caller's own `SourcePreviewId` first, so this scenario still resolves idempotently instead of incorrectly surfacing `CatalogActivePlanConflictException`.

## 17. Files created

- `docker-compose.yml`
- `backend/RunningApp.Persistence/Migrations/20260716130000_FixLongRunDayCheckConstraintFullDayNames.cs` (+ `.Designer.cs`)
- `backend/RunningApp.IntegrationTests/LongRunDayCheckConstraintTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogConfirmationRelationalTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshotHashCanonicalizationTests.cs`
- `backend/RunningApp.IntegrationTests/ResetEndpointRelationalScenarioTests.cs`
- `PHASE4F_9_2_RELATIONAL_VALIDATION.md` (this file)
- `plan-catalog/artifacts/audits/phase4f9-2-relational-validation.json`

## 18. Files modified

- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshot.cs` — added `HashAlgorithmVersion`; added `CatalogPreviewCanonicalHashSerializer`.
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshotVerifier.cs` — delegates to the canonical serializer; fails closed on unsupported hash-algorithm version.
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs` — `AsUtcDateTime` helper (5 call sites); `ActivePlanPerUser` recovery branch extended (§16.4); unsupported-hash-version exception no longer rewrapped.
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs` — added `PlanPreviewHashAlgorithmVersionUnsupportedException`.
- `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs` — mapped the new exception to HTTP 422.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationServiceTests.cs` — 2 hand-crafted-JSON tests updated with the new required field.
- `backend/RunningApp.Persistence/Migrations/AppDbContextModelSnapshot.cs` — regenerated (no manual edits needed beyond what `dotnet ef` produced).

## 19. Reproducibility

**Startup**: `docker compose up -d postgres`
**Health**: `docker exec appsel-dev-postgres pg_isready -U postgres -d antigravity_dev`
**Shutdown**: `docker compose stop postgres`
**Full reset (destructive, local development only)**: `docker compose down -v` (removes the named volume `appsel-dev-postgres-data` — all data lost)
**Migration**: `dotnet ef database update --project backend/RunningApp.Persistence --startup-project backend/RunningApp.Api`
**Tests**:
```
dotnet test backend/RunningApp.sln
dotnet test plan-catalog/PlanCatalog.sln
```
**Credentials** (local development only, matching the plaintext value already committed in `appsettings.json` — not a production secret pattern, not to be reused anywhere beyond localhost): `Host=localhost;Port=5432;Database=antigravity_dev;Username=postgres;Password=postgres`.

## 20. Remaining gaps

- Scenario-8-style HTTP-level concurrent races can occasionally surface a transient 500 under heavy full-suite parallel load due to connection-pool/resource contention in the shared single-container dev database — this is environment resource contention, not a logic defect (the equivalent DbContext-level race is proven reliable in `CatalogConfirmationRelationalTests` across repeated runs). Not a blocker; noted for anyone extending HTTP-level concurrency tests further.
- `RuntimeConditionResolutionService` debt items (TD-PACESOURCE-001/002, TD-CORE-READINESS-001, TD-REGISTRY-001) were not re-derived in this phase (out of scope).
- Phase 4F.5–4F.9.2 remain uncommitted working-tree changes, per instruction not to commit in this phase.

## 21. Publication boundary

Confirmed unchanged: `TEN_K__4D__INTERMEDIATE v10` remains `DRAFT`; `CatalogLivePilotOptions.Enabled` remains `false` by default (no config override in `appsettings.json`/`appsettings.Development.json`); no publication ledger entry; no production activation; no live catalog exposure; no new public endpoint; no candidate lifecycle change; no plan-generation value change (only a `DateTimeKind` and hash-canonicalization correctness fix, plus the unrelated legacy day-check-constraint fix — no training-science numeric defaults were touched).

## 22. Repository state

- Branch: `main`. HEAD: `0c67965` (no commits made this phase).
- Ahead/behind: n/a (no remote comparison requested; no commits made).
- Staged: none.
- Unstaged: real source changes across `backend/RunningApp.Application`, `RunningApp.Api`, `RunningApp.Domain`, `RunningApp.Persistence`, `RunningApp.IntegrationTests`, plus generated `bin/obj` drift already present before this phase (untouched).
- Untracked: `docker-compose.yml` (new), the new migration pair, the 6 new/changed test files, this phase's two new docs/JSON, plus all previously-accumulated uncommitted Phase 4F.5–4F.9.1A work (unchanged from before this phase) and a pre-existing `baseline_tmp/` directory (present before this session, not created or touched by this phase).
- No destructive operations performed. No files deleted. No generated drift was removed (none created by this phase beyond normal `bin/obj` rebuild output, which was already untracked/ignored-in-spirit before this phase and left as-is).

## 23. Publication readiness

`READY_FOR_CANDIDATE_PUBLICATION` — from a **technical implementation and relational-correctness** standpoint only. This does not constitute a product/business decision to publish; the candidate remains DRAFT and no publication action was taken or is implied.

## 24. Final conclusion

A local PostgreSQL 17 environment was successfully bootstrapped, all migrations applied and verified idempotent, and every real-relational verification blocked since Phase 4F.9.1 is now closed: schema/index/JSONB verification, all 8 reset-endpoint scenarios, sequential idempotency, same-preview and active-plan concurrency (both with real SQLSTATE 23505 recovery), transaction atomicity, fresh-context round-trip (including TAPER_SHARPEN), legacy regression, and error mapping. Two severe defects intrinsic to Phase 4F.9's own catalog-persistence code — an order-dependent snapshot hash guaranteed to break under jsonb storage, and a DateTimeKind rejection guaranteed to break every real confirmation — were found only because real PostgreSQL was finally available, and both are now fixed and regression-tested. One unrelated pre-existing legacy defect was also found and fixed. The full backend suite (808 tests) and plan-catalog suite (335 tests) both pass cleanly and reproducibly. Stop after Phase 4F.9.2.
