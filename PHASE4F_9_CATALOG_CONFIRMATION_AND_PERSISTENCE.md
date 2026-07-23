# Phase 4F.9 Catalog Confirmation And Persistence

> **Update (Phase 4F.9.2):** real-PostgreSQL relational validation is now
> complete (`PHASE4F_9_2_RELATIONAL_VALIDATION.md`), closing the "Development
> PostgreSQL schema verification and real relational concurrency/rollback
> tests remain blocked" note below. A local Postgres 17 environment was
> bootstrapped via Docker Compose; all migrations applied and verified
> idempotent; sequential idempotency, same-preview and active-plan
> concurrency (with real SQLSTATE 23505 recovery), transaction atomicity,
> and fresh-context round-trip (including TAPER_SHARPEN) are all now proven
> against a real database. Two severe defects intrinsic to this phase's own
> hash-verification and date-handling code were found and fixed only because
> real Postgres finally became available — see the Phase 4F.9.2 report for
> full detail. Phase 4F.9.1's "blocked" language below is historical.

## Confirmation Boundary

Catalog confirmation accepts an authenticated user, a stored `PlanPreview`, the stored `CatalogPreviewSnapshot`, a verified snapshot hash, `GenerationSource=CATALOG`, and a structurally valid `GeneratedCatalogPlanPayload`. The stored snapshot is the source of truth. Confirmation does not rerun routing, candidate resolution, runtime conditions, stage allocation, workout binding, date assignment, volume allocation, or prescription generation.

Validation runs before mutation where practical: preview existence, ownership, expiration, invalidation, snapshot presence/deserialization, required snapshot fields, generation source, hash verification, idempotency, payload schema version, payload validation, unsupported 8-week explicit-zero guard, and active-plan invariant.

## Schema Changes

The Phase 4F.9 migration is additive and nullable for legacy compatibility.

`TrainingPlans`: `GenerationSource`, `SourcePreviewId`, `CatalogPreviewContentHash`, `CatalogMaterializerVersion`, `CatalogDependencyVersionsJson` (`jsonb`), `CatalogConfirmedAtUtc`. `SourcePreviewId` has a unique filtered index for non-null values.

`TrainingDays`: `CatalogPhaseKey`, `CatalogProgressionStageKey`, `CatalogWorkoutDefinitionKey`, `CatalogWorkoutDefinitionVersion`, `CatalogStructuralRole`, `CatalogPrescriptionJson` (`jsonb`), `CatalogPrescriptionSchemaVersion`, `GenerationSource`.

Existing `TrainingDay.CatalogStageKey` is retained as legacy/deprecated stage provenance. It is not repurposed. New writes populate both `CatalogPhaseKey` and, when distinct, `CatalogProgressionStageKey`.

## TrainingPlan Mapping

The persisted plan stores user, status, goal type, goal distance, level, days per week, unit, race date, start/end dates, target finish time, `GenerationSource=CATALOG`, `SourcePreviewId`, snapshot content hash, candidate key/version/status, materializer version, dependency summary JSON, confirmation timestamp, catalog artifact keys/versions, canonical distance family, and requested target distance.

## TrainingWeek Mapping

One `TrainingWeek` is created for every generated week. Week number, start date, planned weekly volume, phase-derived week type, recovery/taper marker, parent plan, and `CatalogPhaseKey` are stored.

## TrainingDay Mapping

One `TrainingDay` is created for every generated session. Rest rows are not created. Date, workout type, structural role, workout definition key/version, legacy catalog stage key, phase key, progression stage key, planned distance, estimated or target duration value, target pace where exact, intensity/effort text, catalog prescription JSON, schema version, and `GenerationSource=CATALOG` are stored. Actual/completion/adaptation fields remain empty/default.

## Prescription JSON

`CatalogPrescriptionJson` uses schema key `CATALOG_SESSION_PRESCRIPTION_SNAPSHOT`, schema version `1`, snake-case deterministic `System.Text.Json` serialization, ordered segments, session distance/duration semantics, pace object, provenance, and no raw decision trace or sensitive readiness evidence.

The JSON preserves effort-only pace without zero numeric pace, target pace as seconds/km, ranges as min/max seconds/km, unresolved numeric pace as null, estimated duration separately from prescribed duration, and ordered segment details.

## TAPER_SHARPEN

The taper sharpening key session persists with `CatalogPhaseKey=TAPER`, `CatalogProgressionStageKey=TAPER_SHARPEN`, `CatalogStructuralRole=KEY_SESSION`, and `CatalogWorkoutDefinitionKey=EASY_STANDARD`. Its prescription JSON preserves ordered component labels including `EASY_BASELINE`, `CONTROLLED_SHARPENING`, and `EASY_RECOVERY`, so it remains distinguishable from ordinary easy support.

## Transaction, Idempotency, Concurrency

For relational providers, confirmation uses one database transaction covering `TrainingPlan`, `TrainingWeek`, `TrainingDay`, `PlanEvent`, and preview `ConfirmedPlanId`. In-memory tests run without a transaction because the provider does not support it. Rollback leaves no persisted plan/week/day/event and does not consume the preview.

Repeated confirmation returns the existing confirmed plan when `PlanPreview.ConfirmedPlanId` is set. `TrainingPlans.SourcePreviewId` has a unique filtered index to prevent duplicate plans for the same preview at the database level.

## Preview Lifecycle

Successful confirmation sets `PlanPreview.ConfirmedPlanId` and preserves the snapshot payload. Failed confirmation does not consume the preview. Expiration blocks unconfirmed previews and does not invalidate an already confirmed idempotent response path once the preview is marked confirmed.

## Legacy Isolation

Legacy preview confirmation remains routed through the existing SQL path. Legacy rows do not receive catalog-only provenance unless existing legacy code already populated shared fields. Catalog confirmation does not call the legacy generation engine.

## Publication Boundary

Candidate publication and production catalog activation remain separate. This phase does not publish v10, does not write a publication ledger entry, and does not enable production activation.

## Tests

Focused integration coverage includes valid materialization, validation failures, idempotent repeat confirmation, TAPER_SHARPEN provenance/prescription preservation, and legacy-routing regression. Existing Phase 4F.8.2 routing, Phase 4F.8.1 preview, and Phase 4F.7D prescription tests remain part of the validation set.

## Phase 4F.9.1 Validation Update

The 37 full-backend-suite failures observed after Phase 4F.9 were traced to `APPLICATION_STARTUP_FAILURE`: the real PostgreSQL test/development database at `localhost:5432` rejected connections before the reset endpoint action body ran. The reset endpoint did not suppress the error; the HTTP 500 was produced by `MockAuthMiddleware` user synchronization failing to open the configured `antigravity_dev` connection.

The Phase 4F.9 migration metadata was completed by adding the migration designer and updating `AppDbContextModelSnapshot` with the catalog confirmation columns, JSONB mappings, and `IX_TrainingPlans_SourcePreviewId` filtered unique index.

The catalog confirmation service now translates a relational `IX_TrainingPlans_SourcePreviewId` uniqueness race into idempotent recovery: the losing transaction is rolled back, the change tracker is cleared, and the existing plan is reloaded by `SourcePreviewId` and returned. If the invariant fires but no plan can be reloaded, a typed `CatalogPreviewConfirmationConcurrencyException` is thrown.

Development PostgreSQL schema verification and real relational concurrency/rollback tests remain blocked until PostgreSQL is available on the configured connection string.

Phase 4F.9.1 continuation reattempted the relational work with elevated local access. `localhost:5432` still refused connections for the configured `antigravity_dev` database, no PostgreSQL service was found, and nothing was listening on port 5432. EF pending-model-change verification still passes, but migration application, direct schema checks, reset endpoint verification, full backend-suite closure, and real relational concurrency/rollback checks remain blocked by database availability.
