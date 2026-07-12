# Phase 4E.2 — Final Acceptance Report

Narrow acceptance-finalization pass following `PHASE4E_2_CLAUDE_SAFETY_AUDIT.md`'s classification of `PHASE4E_2_SAFE_TO_ACCEPT_AS_GATED_FOUNDATION`. No redesign, no stage-to-week scheduling, no public activation, no concurrency mechanism, no v10 publish, no resolver-behavior change, no catalog domain-decision change. Only the low-risk documentation/test-clarity/TD-note corrections listed below were made; the migration was inspected but **not applied**.

## 1. Exact files inspected

- `PHASE4E_2_CLAUDE_SAFETY_AUDIT.md` (full read, all 24 sections)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationServiceTests.cs`
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs`
- `plan-catalog/artifacts/audits/activation-readiness-risks.json` and `.md`
- `backend/RunningApp.Persistence/Migrations/20260712115640_Phase4E2_CatalogConfirmationState.cs` (and its `.Designer.cs`)
- `backend/RunningApp.Persistence/Migrations/AppDbContextModelSnapshot.cs` (spot-checked for `ConfirmedPlanId`/`IsInvalidated` consistency, already confirmed in the audit)
- Generated (not applied) idempotent SQL script for migrations `20260710072851_AddPlanCatalogProvenanceFields` → `20260712115640_Phase4E2_CatalogConfirmationState`, via `dotnet-ef migrations script --idempotent`, written to a scratch file outside the repository

## 2. Exact files changed in this pass

1. `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationServiceTests.cs` — renamed `SnapshotVerifier_WrongHash_ReturnsFalse` → `SnapshotVerifier_DifferentSnapshotContent_ProducesDifferentSelfConsistentHashes`, with an added header comment explaining the correction. **No assertion, no test body logic, no test behavior changed** — verified by an identical pass/fail test-suite total before and after (§9 below).
2. `backend/RunningApp.Application/Exceptions/AppExceptions.cs` — corrected two XML doc comments only, no code/behavior change:
   - `PlanPreviewAlreadyConfirmedException`: now states plainly that it is currently unused/dead code (a repo-wide search confirms no call site constructs or catches it), rather than falsely claiming the confirmation service "catches this internally."
   - `PlanPreviewSnapshotMalformedException`: removed the false claim that its message is "masked" (masking only applies to HTTP 500 in `GlobalExceptionHandler`; this exception maps to 422, so its message is exposed verbatim — which contains no sensitive detail).
3. `plan-catalog/artifacts/audits/activation-readiness-risks.json` and `.md` — appended (did not replace or remove) an addendum to `TD-PACESOURCE-002`'s `implementationNote`, documenting the Phase 4E.2 Resolved/Unresolved split (§6 below). **`status` remains `"OPEN"` in both files — not changed.**
4. This report.

No runtime source file's compiled behavior was altered. No migration was regenerated or applied. No architecture, resolver, or catalog-domain file was touched.

## 3. Six audit corrections and their disposition

| # | Correction (verbatim from audit §20) | Classification | Disposition this pass |
|---|---|---|---|
| 1 | *"rewrite or remove `SnapshotVerifier_WrongHash_ReturnsFalse` so it actually tests what its name claims (or rename it to accurately describe what it proves)"* | test-name/test-clarity-only | **Done** — took the rename-only branch; body/assertions untouched |
| 2 | *"remove the dead `PlanPreviewAlreadyConfirmedException`, or correct its doc comment, or wire it in"* | documentation-only | **Done** — took the doc-comment-correction branch |
| 3 | *"correct `PlanPreviewSnapshotMalformedException`'s doc comment"* | documentation-only | **Done** |
| 4 | *"append (never replace/close) a `TD-PACESOURCE-002` note reflecting Phase 4E.2's actual reuse-decision implementation"* | documentation-only / TD implementation-note update | **Done** — appended, status unchanged (§6) |
| 5 | *"apply migration `20260712115640_Phase4E2_CatalogConfirmationState` to `antigravity_dev`"* | environment/migration-operation | **Not performed** — explicitly out of scope for this pass; verified only (§7) |
| 6 | *"populate `CanonicalDistanceFamily` on `ResolverInputSnapshot`"* | runtime-code correction | **Deferred** — not performed; this pass makes no runtime-code changes per its own constraints |

## 4. Current confirm behavior

Unchanged from the audit's findings (re-verified against the same source files, none of which were modified in this pass except the two doc comments in §2, which have no runtime effect):

- `ConfirmPlanAsync` (`PlanServices.cs`) reads the stored preview's `PreviewPayloadJson`, checks for a top-level `generation_source == "CATALOG"` field, and dispatches unconditionally to `CatalogPlanConfirmationService.ConfirmAsync` for catalog-sourced previews — **never** re-running `IGenerationRouteDecider`, the eligibility gate, any resolver, `StageEligibilityEvaluator`, or a generation engine.
- `CatalogPlanConfirmationService.ConfirmAsync` runs its 15-step validation/persistence flow: ownership → expiry → invalidation → snapshot presence → parse → schema completeness → `GenerationSource` check → hash integrity → idempotency short-circuit → **persistability guard**.

## 5. Proof that real catalog previews are rejected before mutation, and no empty active plan is created

Re-confirmed by direct reading (no code changed here): `CatalogPreviewGenerator.GenerateAsync` — the only production path that builds a real snapshot — calls `CatalogPreviewSnapshotBuilder.Build(...)` without ever supplying a `generatedPreviewPlanPayload`, so **every real snapshot has `GeneratedPreviewPlanPayload == null`**. `CatalogPlanConfirmationService.ConfirmAsync`'s step-11 guard (`if (snapshot.GeneratedPreviewPlanPayload is null) throw new CatalogPreviewNotPersistableException(...)`) executes and throws **before** the method's first `_context.Add(...)` call. This is directly proven by a passing test, `ConfirmAsync_NonPersistableSnapshot_ThrowsCatalogPreviewNotPersistableException_AndLeavesDatabaseUnchanged`, which asserts `Assert.Empty` across `TrainingPlans`, `PlanEvents`, `TrainingWeeks`, and `TrainingDays`, and `Assert.Null` on the reloaded preview's `ConfirmedPlanId` — i.e. **no empty active plan, and no partial row of any kind, is ever created.** This test passed in both the audit's run and this pass's re-run (§9).

## 6. Proof that catalog failure never falls back to SQL

Re-confirmed by direct reading: `PlanServices.ConfirmPlanAsync`'s catalog-dispatch branch is `return await _catalogConfirmationService.ConfirmAsync(...)` with **no surrounding `try`/`catch`** — any exception thrown by the catalog confirmation service (including `CatalogPreviewNotPersistableException`) propagates directly out of `ConfirmPlanAsync` to `GlobalExceptionHandler`. The legacy SQL confirm logic beneath the dispatch branch is structurally unreachable once dispatch determines a preview is catalog-sourced — there is no code path that "falls through" from a caught catalog exception into the SQL branch.

## 7. Migration deployment status

```text
CREATED_BUT_NOT_APPLIED
```

Migration `20260712115640_Phase4E2_CatalogConfirmationState` exists, is internally consistent (`Up`/`Down` symmetric), and is reflected in `AppDbContextModelSnapshot.cs`. Verified this pass, without applying it to any database, via `dotnet-ef migrations script --idempotent` (a pure offline SQL-generation command — it reads only the compiled migration classes and never opens a database connection). The generated SQL for this migration is strictly additive and non-destructive:

```sql
ALTER TABLE "PlanPreviews" ADD "ConfirmedPlanId" uuid;
ALTER TABLE "PlanPreviews" ADD "IsInvalidated" boolean;
CREATE INDEX "IX_PlanPreviews_ConfirmedPlanId" ON "PlanPreviews" ("ConfirmedPlanId");
ALTER TABLE "PlanPreviews" ADD CONSTRAINT "FK_PlanPreviews_TrainingPlans_ConfirmedPlanId"
    FOREIGN KEY ("ConfirmedPlanId") REFERENCES "TrainingPlans" ("Id") ON DELETE SET NULL;
```

Both new columns are **nullable**; the index is a plain (non-unique) index; the foreign key uses `ON DELETE SET NULL` (matches `AppDbContext.cs`'s `.OnDelete(DeleteBehavior.SetNull)` configuration exactly). A search of the generated script for `DROP`, `TRUNCATE`, `DELETE FROM`, or narrowing `ALTER COLUMN` statements from this migration onward returned **zero matches**. **The migration was not applied to any database in this pass.**

## 8. Reason for the current 37 integration-test failures

All 37 failures (`UserJourneyTests.cs` and `FitnessEvidenceInputContractTests.cs`, both real-Postgres HTTP tests) share one identical, verified root cause: their shared `ResetAsync()` helper calls `POST /api/v1/testing/reset`, which queries `PlanPreviews` including the new `ConfirmedPlanId` column — a column that exists in the compiled C# model and the migration file, but **does not exist in the physical `antigravity_dev` database**, because the migration in §7 has not been applied there (`CREATED_BUT_NOT_APPLIED`). The resulting server-side exception is `Npgsql.PostgresException: 42703: column p.ConfirmedPlanId does not exist`. This is a database-schema-drift/deployment-order issue, not a defect in catalog-confirmation logic — confirmed by the fact that **zero** failures occur in any of the 21 new `CatalogPlanConfirmationServiceTests` or any other EF-InMemory-based test class, all of which exercise the actual confirm logic directly.

## 9. Explicit statement on baseline

**No committed git baseline exists for a "pre-Phase-4E.2, post-Phase-4E.1.1" state.** `git log` shows the latest actual commit is `fe85044` ("plan-catalog-added"), and that commit contains zero files under `RuntimeCatalog` — it predates the entire backend catalog-integration feature (Phases 1 through 4E.2), not just Phase 4E.2. Comparing against it would not isolate Phase 4E.2's risk and would very likely fail to compile. **Therefore "pre-existing" was never formally proven via a git-diff against a committed baseline** — the only available comparison is this same session's own directly-recorded prior measurement (367 passed / 0 failed / 367 total, immediately before Phase 4E.2 began), combined with direct stack-trace evidence (§8) distinguishing the current 37 failures' root cause from a code regression. **This report does not claim the full solution test suite is passing.** It explicitly is not: **351 passed, 37 failed, 388 total**, and it will remain in that state until the development database migration is applied (an infrastructure action explicitly not performed in this pass).

## 10. Exact test totals (this pass, re-run after the documentation/rename-only corrections)

```
Toplam test sayısı: 388
     Geçti: 351
     Başarısız: 37
```

Identical to the audit's own measurement (§9 of this report's numbering; §12 of the audit) — confirming the corrections made in this pass (a test rename and two doc-comment edits) introduced **zero change** in pass/fail outcome, as expected for documentation/naming-only edits. `dotnet build RunningApp.sln -c Release` → 0 errors, 0 warnings, both before and after this pass's edits.

## 11. Complete seven-TD status (post-this-pass)

| # | ID | Status | Changed this pass? |
|---|---|---|---|
| 1 | `TD-D3-001` | OPEN | No |
| 2 | `TD-WAVE5-001` | OPEN (revisited D13, not closed) | No |
| 3 | `TD-BACKEND-001` | OPEN | No |
| 4 | `TD-REGISTRY-001` | OPEN | No |
| 5 | `TD-PACESOURCE-001` | OPEN, has Phase 4D.5.1 `implementationNote` | No |
| 6 | `TD-PACESOURCE-002` | **OPEN — status unchanged**; Phase 4E.2 addendum appended to its `implementationNote` (see below) | **Yes — note only, not status** |
| 7 | `TD-CORE-READINESS-001` | OPEN, has Phase 4D.3.1 `resolutionNote` | No |

### TD-PACESOURCE-002 reconciliation (this pass's required detail)

**Resolved** (by Phase 4E.2's actual, verified code — `CatalogPlanConfirmationService.BuildPlan`):
- Preview freezes `AsOfDate` exactly once (`DateOnly.FromDateTime(DateTime.UtcNow)`, computed in `PlanServices.GenerateCatalogPreviewAsync`, never re-read per resolver — established in Phase 4E.1).
- Confirm does not recompute `AsOfDate` — `BuildPlan` reads it verbatim from `snapshot.AsOfDate`; `ConfirmedAtUtc` is a separate technical timestamp never used for any domain decision.
- Confirm does not rerun resolver orchestration (§4/§6 — no resolver, route decider, or eligibility gate dependency exists on `CatalogPlanConfirmationService` at all).
- An immutable preview/confirm snapshot-consistency boundary exists: the stored `CatalogPreviewSnapshot` (hash-verified at confirm time, §5 of the audit) is the sole source of truth `BuildPlan` reads from.

**Unresolved** (explicitly out of this TD's own tracked scope, but real gaps toward full activation):
- Persistable stage-to-week schedule generation — `GeneratedPreviewPlanPayload` is always `null`; `BuildPlan` (where the `AsOfDate` reuse decision actually lives) has never been exercised in production because the persistability guard rejects every real snapshot first.
- Calendar-day alignment — does not exist in any form yet.
- Production concurrent-confirmation safety — no database-level "one preview → at most one confirmed plan" invariant exists (tracked separately as Blocking Defect §15 item 1 in the audit, not part of `TD-PACESOURCE-002`'s own criteria).
- Public catalog activation — independently blocked by the `PUBLISHED`-only eligibility gate.

**This TD is NOT marked closed.** Its tracked `status` field remains `"OPEN"` in both `activation-readiness-risks.json` and `.md`. Consistent with this repository's own established convention (`TD-CORE-READINESS-001`'s precedent: implemented-but-not-yet-exercised-against-live-traffic risks are never mechanically closed), the appended note records that the decision is made and coded, but the code path has never actually run against a real confirm request — reflecting the unresolved items above, none of which this TD's own three closure criteria formally require, but all of which are real blockers to the broader goal the TD exists to protect.

## 12. Public activation blockers (current, complete list)

1. `TEN_K__4D__INTERMEDIATE v10` (and all four of its direct dependencies) remain `DRAFT` — the `PUBLISHED`-only eligibility gate rejects every real request (unchanged, not touched by this pass, and this pass did not publish v10).
2. Stage-to-week scheduling does not exist — `GeneratedPreviewPlanPayload` is always `null`, so even a published candidate could never produce a persistable plan today.
3. No database-level concurrency invariant protects "one preview → at most one confirmed plan" — currently non-exploitable only because blocker #2's persistability guard rejects every request first; this must be solved before #2 is ever lifted.
4. The target development database has not had the Phase 4E.2 migration applied (§7/§8) — an infrastructure/deployment gap, orthogonal to the above three but must also be resolved before any environment relying on `PlanPreviews.ConfirmedPlanId`/`IsInvalidated` can function correctly.

## 13. Next recommended phase

A dedicated phase addressing **database-level concurrent-confirmation safety** (e.g. a unique constraint or an atomic conditional-update pattern on the preview→plan association) should precede any phase that populates `GeneratedPreviewPlanPayload`, since that population is what would first make the currently-latent concurrency defect exploitable. Separately, and independently, the Phase 4E.2 migration should be applied to `antigravity_dev` (an infrastructure action, not a code phase) so the full test suite can run green and so A9-class legacy-path verification can be completed with real empirical evidence rather than being blocked by schema drift. Stage-to-week scheduling itself remains explicitly out of scope until both of the above are addressed.

## 14. Final classification

```text
PHASE4E_2_ACCEPTED_AS_GATED_FOUNDATION_NOT_PUBLICLY_ACTIVATABLE
```
