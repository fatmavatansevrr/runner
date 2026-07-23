# Phase 4F.9.1A — Pre-Relational Validation Corrections

> **Update (Phase 4F.9.2):** real-PostgreSQL relational validation is now
> complete — see `PHASE4F_9_2_RELATIONAL_VALIDATION.md`. It found and fixed
> two additional severe, previously-undetectable defects in the confirmation
> code (an order-dependent snapshot hash broken by jsonb key reordering, and
> a `DateTimeKind.Unspecified` rejection by Npgsql), plus one unrelated
> pre-existing legacy defect (`CK_TrainingPlans_LongRunDay`). All relational
> verification this document deferred is now closed; final classification
> `PHASE4F9_2_RELATIONAL_VALIDATION_CLOSED`.

## 1. Final classification

`PHASE4F9_1A_COMPLETED_WITH_NON_BLOCKING_GAPS`

All four verified blockers from `PHASE4F_PRE_PUBLICATION_IMPLEMENTATION_VALIDATION.md` were resolved with concrete, tested code changes: confirmation ordering, active-plan concurrency translation, pilot-identity centralization, and the 8-week explicit-zero claim (confirmed correct, comment fixed). The remaining items (stale docs, DI test, JSON round-trip test, CatalogStageKey, misfiled artifact) were also completed. No product decision is left unresolved; a small number of non-blocking gaps remain (noted in §19).

## 2. Confirmation ordering

Final order in `CatalogPlanConfirmationService.ConfirmAsync` (`backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs:110-160`):

1. Load preview
2. Ownership (always before idempotency — a non-owner never receives an existing plan)
3. **Idempotency** (already-confirmed replay) — returns the existing plan without re-checking expiry/invalidation/hash/schema
4. Expiration (unconfirmed preview only)
5. Invalidation
6. Snapshot presence
7. Parse snapshot
8. Schema completeness
9. GenerationSource check
10. Hash verification
11. Persistability guard (null payload / schema version / structural validation / 8-week-zero)
12. Active-plan invariant (fast pre-check + DB-level enforcement)
13. Transactional persistence

Exact idempotent-expiration behavior: an already-confirmed preview returns the existing plan **even if it has since expired or been invalidated** — expiration/invalidation now only block confirming a *new* plan, never a replay of an already-settled one. Two new defensive checks were added inside the idempotency branch: (a) if `ConfirmedPlanId` points to a missing `TrainingPlan` row, throws `CatalogConfirmationFailedException` (unchanged); (b) if `ConfirmedPlanId` points to a plan owned by a **different** user (corrupted/tampered linkage), throws `CatalogConfirmationFailedException` rather than ever returning another user's plan.

## 3. Active-plan invariant

- Product rule: at most one `TrainingPlanStatus.Active` plan per `InternalUserId` (pre-existing rule, unchanged).
- Database mechanism: **pre-existing** unique partial index `IX_TrainingPlans_InternalUserId_ActiveOnly` on `InternalUserId` filtered `WHERE "Status" = 'active'`, created by the `MigrateToInternalUserIdFKs` migration (2026-07-01), confirmed still present and correct in `AppDbContextModelSnapshot.cs:780-783`. No new migration was required — the gap was in exception *translation*, not schema.
- Exception translation: `ClassifyUniqueViolation` (renamed from `IsSourcePreviewUniqueViolation`) now distinguishes two distinct SQLSTATE 23505 constraint names: `IX_TrainingPlans_SourcePreviewId` → idempotent reload-and-return; `IX_TrainingPlans_InternalUserId_ActiveOnly` → `CatalogActivePlanConflictException` (pre-existing exception type and 409 HTTP mapping, previously unused by this code path). Any other 23505 or non-23505 `DbUpdateException` still falls through to `CatalogPlanPersistenceFailedException` (500).
- Migration impact: none. `dotnet-ef migrations has-pending-model-changes` confirms no pending model changes.
- Unverified relational assumptions: the actual PostgreSQL-level behavior of two concurrent transactions racing on this partial index (lock behavior, exact `ConstraintName` populated by Npgsql on 23505) is still unverified against a real database — this remains explicitly for Phase 4F.9.2.

## 4. Pilot identity

- Central policy: `V1CatalogPilotIdentityPolicy` (`backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`), `PolicyKey = "V1_CATALOG_PILOT_IDENTITY_POLICY"`, `PolicyVersion = 1`.
- Backend level: `RunningBackground.RunningRegularly`. Catalog level: `"INTERMEDIATE"`.
- Candidate identity: `CandidateKey = "TEN_K__4D__INTERMEDIATE"`, `CandidateVersion = 10`, `DaysPerWeek = 4`, `GoalType = Race`, `GoalDistance = TenK`.
- Consumers: `V1LiveCatalogPilotRoutingPolicy.Evaluate` (now calls `IsSupportedIdentity(...)` instead of an inline four-field comparison); `CatalogPreviewGenerator.GenerateAsync` (candidate key/version constants); `ProgressionStageAllocatorTests.cs`, `CatalogWorkoutBinderTests.cs`, `CatalogCandidateEligibilityGateTests.cs` (test fixtures repointed from the deleted `PilotGenerationRouteDecider` constants).
- Governance basis: evidence basis `NOT_AN_EVIDENCE_QUESTION`, decision status `EXPLICIT_PRODUCT_DEFAULT` (stated in the policy's own XML doc comment). The `RunningRegularly ↔ INTERMEDIATE` equivalence was confirmed to be **already documented** pre-existing in governance/audit artifacts (`plan-catalog/artifacts/audits/phase4f-pre-publication-implementation-validation.json`, `backend-process-b-activation-review.json`, `PHASE2_CATALOG_VOCABULARY_MAPPING.md`, `PHASE4E_1_CATALOG_PREVIEW_ROUTING_AND_IMMUTABLE_RESOLUTION_SNAPSHOT.md`) — no new AUD entry was added, per the instruction not to duplicate an already-recorded decision.

## 5. Legacy route decider

`REMOVED_AS_DEAD_CODE`

`PilotGenerationRouteDecider` (in `GenerationRouteDecision.cs`) was never registered in DI (`IGenerationRouteDecider` resolves only to `LivePlanPreviewRoutingService`, `Program.cs:189-190`). Its `Decide` method had no live caller. Its `PilotCandidateKey`/`PilotCandidateVersion` constants **were** referenced by `CatalogPreviewGenerator.cs` and three test files — those four call sites were repointed to `V1CatalogPilotIdentityPolicy.CandidateKey`/`CandidateVersion` before the class and its dedicated `PilotGenerationRouteDeciderTests.cs` were deleted. A stale reference to that deleted test class in `Phase4F4ConfirmAndLegacyRegressionTests.cs`'s comments was also corrected.

## 6. 8-week explicit-zero result

`CONFIRMED_INFEASIBLE`

Real production classes (`CatalogVolumeAndLongRunPlanner`, `V1FourDaySessionVolumeAllocationPolicy`) were exercised in-memory for TEN_K/4D/INTERMEDIATE, 8-week cycle, `RecentWeeklyVolumeKm=0`:

| Week | Phase | Volume (km) | Long run (km) | Residual (km) |
|---|---|---|---|---|
| 1 | FOUNDATION | 12 | 4 | 8 |
| 2 | FOUNDATION | 12.5 | 4 | 8.5 |
| 3 | BUILD | 13.5 | 4.5 | 9 |
| 4 | RACE_SPECIFIC | 14 | 4.5 | 9.5 |
| 5 | RACE_SPECIFIC | 14.5 | 5 | 9.5 |
| 6 | RACE_SPECIFIC | 15.5 | 5 | 10.5 |
| 7 | RACE_SPECIFIC | 16 | 5.5 | 10.5 |
| 8 (TAPER) | TAPER | 8.5 | 3 | **5.5** |

Weeks 1–7 all clear the 6.0km session-allocation floor (3.0km KEY_SESSION + 2×1.5km EASY_SUPPORT). **Week 8 (taper) does not**: 16km peak × 0.53 taper multiplier = 8.5km weekly volume, minus a 3km taper long run, leaves a 5.5km residual — below the 6.0km floor. The real `V1FourDaySessionVolumeAllocationPolicy.Allocate` throws `CatalogSessionPrescriptionInfeasibleException` at week 8. This confirms the guard in `CatalogPlanConfirmationService.cs` is correct, but the earlier static-arithmetic audit had the wrong week (it checked week 1, not the taper week). Code change: the guard's `if` condition and thrown-exception type are **unchanged**; only its comment and exception message were corrected to state the real cause (taper-week residual vs. floor) instead of the previous vague "known unsupported path" wording. No numeric defaults were changed. No existing test needed correcting since the guard's behavior was proven right, not wrong.

## 7. CatalogStageKey

`LEGACY_READ_ONLY_NO_NEW_WRITES`

A full-tree grep found no active production reader of `TrainingDay.CatalogStageKey` outside migrations, its own doc comments, and legacy-path tests — `CatalogPersistedPlanValidator` and all `Application/Services` query paths already read `CatalogPhaseKey`/`CatalogProgressionStageKey` for catalog-sourced rows. The write `CatalogStageKey = session.Provenance.SourceStageKey` was removed from `CatalogPlanConfirmationService.BuildCatalogTrainingDay`; the property's XML doc on `TrainingDay.cs` now states the decision explicitly (legacy SQL-path rows only; new catalog writes never populate it). A test assertion was added proving new catalog-sourced `TrainingDay` rows leave `CatalogStageKey` null, while legacy-path tests (`CatalogAwarePersistenceTests.cs`) are unaffected.

## 8. DI resolution

New file `backend/RunningApp.IntegrationTests/DependencyInjectionResolutionTests.cs`, built against the real `Program.cs` registration path via `CustomWebApplicationFactory` (Development environment enables ASP.NET Core's built-in `ValidateScopes`/`ValidateOnBuild`). Confirmed resolvable with scope validation on and no PostgreSQL connection opened merely by resolution: `IGenerationRouteDecider`, `ICatalogPreviewGenerator`, `IGeneratedCatalogPlanPayloadValidator`, `ICatalogPlanConfirmationService`, `ICatalogPeakVolumeBandLoader`, and `IOptions<CatalogLivePilotOptions>` (confirmed `Enabled == false` by default). 7/7 new tests pass.

## 9. Prescription JSON

New file `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPrescriptionJsonRoundTripTests.cs`, driving the real `ConfirmAsync` path end-to-end (not a duplicated serializer) and reading back the actual persisted `CatalogPrescriptionJson`. Covers effort-only EASY, exact GOAL_PACE, pace-range, unresolved duration, FARTLEK/THRESHOLD/GOAL_PACE ordered segments, and TAPER_SHARPEN. Confirms: `schema_key == "CATALOG_SESSION_PRESCRIPTION_SNAPSHOT"`, `schema_version == 1`, byte-identical determinism across repeated confirmations, no zero-valued numeric pace for effort-only sessions, correct segment ordering, and semantic field preservation on round-trip. 8/8 new tests pass.

## 10. Stale documentation

- `CatalogPreviewGenerator.cs` (class doc comment): removed "dark/discarded/never stored" claim; now states generation and public materialization are implemented, the payload is stored/hashed, and confirmation/persistence is implemented (Phase 4F.9) but not yet relationally verified.
- `CatalogPreviewSnapshot.cs` (`CatalogPreviewSnapshotBuilder.Build` doc comment): removed the claim that `generatedPreviewPlanPayload` is excluded from the hash — it is included in `hashableContent` (verified at line 153).
- `CatalogPlanConfirmationService.cs`: removed the trailing "steps 12-15 do not exist as executable code" block (they do — that block sat below code that runs them); persistability-guard and 8-week-guard comments updated to reflect current reality (see §6).
- `TrainingDay.cs` (`CatalogStageKey` doc): updated to state the LEGACY_READ_ONLY_NO_NEW_WRITES decision explicitly instead of a generic "legacy/deprecated" label.

No historical correction records were rewritten as though the corrected behavior always existed — each change describes current code, not a revised history.

## 11. Audit artifact move

Moved `phase4f9-catalog-confirmation-and-persistence-audit.json` from repo root to `plan-catalog/artifacts/audits/phase4f9-catalog-confirmation-and-persistence-audit.json`. No duplicate remains at the old path (confirmed via `ls`). No code, test, or script referenced the old path; two prior audit documents mention the filename only as a list entry (not a live path reference) and were left as historical text.

## 12. Governance actions

None added. The one candidate new-decision item (`RunningRegularly ↔ INTERMEDIATE` mapping) was confirmed already recorded in pre-existing governance/audit artifacts (see §4) — adding a duplicate entry was explicitly out of scope per this phase's instructions.

## 13. Migration static validation

No new migration was created — the active-plan invariant's database enforcement (`IX_TrainingPlans_InternalUserId_ActiveOnly`) already existed from the `MigrateToInternalUserIdFKs` migration (2026-07-01), confirmed present and correctly filtered in `AppDbContextModelSnapshot.cs:780-783`. `dotnet-ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration."

## 14. Tests

| Command | Result |
|---|---|
| `dotnet build plan-catalog/PlanCatalog.sln` | 0 errors, 0 warnings |
| `dotnet test plan-catalog/PlanCatalog.sln` | 335 passed / 0 failed / 335 total |
| `dotnet build backend/RunningApp.sln` | 0 errors, 0 warnings |
| `dotnet test backend/RunningApp.sln` (full suite) | 738 passed / **37 failed** (all PostgreSQL-connection environment-blocked, same root cause as the prior validation pass) / 775 total |
| `dotnet ef migrations has-pending-model-changes` | "No changes have been made to the model since the last migration." |

No test was weakened or converted to a skip. 41 new tests were added across confirmation-ordering, DI-resolution, and prescription-JSON round-trip coverage; all pass. Total test count rose from 759 (pre-4F.9.1A) to 775.

## 15. Files created

- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`
- `backend/RunningApp.IntegrationTests/DependencyInjectionResolutionTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPrescriptionJsonRoundTripTests.cs`
- `PHASE4F_9_1A_PRE_RELATIONAL_CORRECTIONS.md` (this file)
- `plan-catalog/artifacts/audits/phase4f9-1a-pre-relational-corrections.json`

## 16. Files modified

- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs` — confirmation ordering, active-plan-conflict exception classification, CatalogStageKey write removed, stale-comment corrections (functional + doc).
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/LivePlanPreviewRouting.cs` — consumes `V1CatalogPilotIdentityPolicy` instead of inline hardcoded identity check (no behavior change).
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/GenerationRouteDecision.cs` — dead-code removal note (see §5).
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs` — candidate-constant call sites repointed; stale doc comment corrected.
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshot.cs` — stale doc comment corrected.
- `backend/RunningApp.Domain/Entities/TrainingDay.cs` — `CatalogStageKey` doc comment corrected to state LEGACY_READ_ONLY_NO_NEW_WRITES.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationServiceTests.cs` — 7 new ordering/idempotency/conflict tests added; 1 assertion added proving CatalogStageKey stays null on new writes.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/ProgressionStageAllocatorTests.cs`, `CatalogWorkoutBinderTests.cs`, `CatalogCandidateEligibilityGateTests.cs` — candidate constants repointed to `V1CatalogPilotIdentityPolicy`.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F4ConfirmAndLegacyRegressionTests.cs` — stale comment reference corrected.

## 17. Files removed or moved

- Removed: `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/GenerationRouteDecision.cs`'s `PilotGenerationRouteDecider` class (dead code, unreachable in production — see §5) and its dedicated `PilotGenerationRouteDeciderTests.cs` file.
- Moved: `phase4f9-catalog-confirmation-and-persistence-audit.json` from repo root to `plan-catalog/artifacts/audits/phase4f9-catalog-confirmation-and-persistence-audit.json` — filing-hygiene correction, no content change.

## 18. Environment-blocked validation

- The same 37 `RunningApp.IntegrationTests` failures requiring a live PostgreSQL connection (`localhost:5432/antigravity_dev`), unchanged in root cause from the prior validation pass.
- Real relational proof of the `IX_TrainingPlans_InternalUserId_ActiveOnly` and `IX_TrainingPlans_SourcePreviewId` concurrency races (actual Npgsql `ConstraintName`/`SqlState` values under real concurrent load) remains for Phase 4F.9.2.

## 19. Remaining blockers

None blocking. Non-blocking gaps carried forward:

- Real PostgreSQL concurrency behavior for both unique-index races is still unverified (expected — reserved for 4F.9.2).
- `RuntimeConditionResolutionService` debt items (TD-PACESOURCE-001/002, TD-CORE-READINESS-001, TD-REGISTRY-001) were not re-derived in this pass (out of scope for 4F.9.1A).
- Phase 4F.5–4F.9(.1A) remain uncommitted working-tree changes, per instruction not to commit in this phase.

## 20. Relational-validation readiness

`READY_FOR_PHASE4F9_2_RELATIONAL_VALIDATION`

## 21. Publication boundary

Candidate `TEN_K__4D__INTERMEDIATE v10` remains `DRAFT` (unchanged, unverified by inspection during this pass — no publication/activation code was touched). `CatalogLivePilotOptions.Enabled` remains `false` by default (confirmed by the new DI-resolution test). No publication ledger entry exists. No activation was enabled.

## 22. Repository state

Branch `main`, HEAD unchanged at `0c67965` (no commits were made, per instructions). All Phase 4F.9.1A changes are additional uncommitted working-tree modifications layered on top of the already-uncommitted Phase 4F.5–4F.9 work; no staged changes; no destructive operations were performed; the only file removed (`GenerationRouteDecision.cs`'s dead class + its dedicated test file) was confirmed unreachable before deletion. Generated `bin`/`obj` drift is unchanged/incidental and was not touched.

## 23. Final conclusion

All four verified pre-relational blockers (confirmation ordering, active-plan concurrency translation, pilot-identity duplication, and the unverified 8-week explicit-zero claim) are now resolved with concrete code changes and passing tests, and the remaining housekeeping items (stale docs, DI coverage, JSON round-trip coverage, CatalogStageKey ambiguity, misfiled audit artifact) are also complete. Builds are clean across both solutions, the full backend suite shows only the expected PostgreSQL-environment-blocked failures (37, unchanged in cause), and `dotnet-ef migrations has-pending-model-changes` reports no pending changes. The repository is ready for Phase 4F.9.2 real-PostgreSQL relational validation. Stop after Phase 4F.9.1A.
