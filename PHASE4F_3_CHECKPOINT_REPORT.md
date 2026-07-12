# Phase 4F.3 Checkpoint Report

## Repository state before checkpoint

Branch: `main`. HEAD before this checkpoint: `d4ebbf05d6853655a0272c5a7bd3fdaa3af78ec6` (Phase 4F.2 checkpoint). Prior checkpoints: `a0ca152e17e6f832a1c5b48c3b0f050643b93ac0` (Phase 4F.1), `d4ebbf05d6853655a0272c5a7bd3fdaa3af78ec6` (Phase 4F.2).

## Current branch and HEAD

`main`, HEAD = `d4ebbf05d6853655a0272c5a7bd3fdaa3af78ec6` prior to this commit.

## Modified and untracked file inventory

Modified (tracked):
- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogBundleLoader.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogCandidateSummary.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PlanCatalogBundleLoaderTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogCandidateEligibilityGateTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationServiceTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayloadSerializationTests.cs`

New (untracked, intended for this checkpoint):
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocation.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogRunLayoutSlots.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestrationExceptions.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestrator.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocationResolverTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/CatalogRunLayoutResolverTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/CatalogStageToWeekContextFactoryTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestratorFixtures.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestratorTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestrationTerminologyTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Phase4F3LiveBoundaryRegressionTests.cs`
- `PHASE4F_3_LIVE_CATALOG_STAGE_ALLOCATION_AND_SKELETON_INTEGRATION.md`
- `PHASE4F_3_CHECKPOINT_REPORT.md` (this file)

New (untracked, excluded from checkpoint):
- `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md`
- `baseline_tmp/`
- `backend/*/bin/Release/`, `backend/*/obj/Release/` (generated build output)

## File provenance classifications

| File | Classification |
|---|---|
| `PlanCatalogBundleLoader.cs` | PHASE4F_3_LOADER_SOURCE |
| `PlanCatalogCandidateSummary.cs` | PHASE4F_3_LOADER_SOURCE |
| `CatalogPhaseAllocation.cs` | PHASE4F_3_APPLICATION_SOURCE |
| `CatalogRunLayoutSlots.cs` | PHASE4F_3_APPLICATION_SOURCE |
| `CatalogPlanSkeletonOrchestrationExceptions.cs` | PHASE4F_3_APPLICATION_SOURCE |
| `CatalogPlanSkeletonOrchestrator.cs` | PHASE4F_3_APPLICATION_SOURCE |
| `PlanCatalogBundleLoaderTests.cs` (modified) | PHASE4F_3_TEST |
| `CatalogCandidateEligibilityGateTests.cs` (modified) | PHASE4F_3_TEST |
| `CatalogPlanConfirmationServiceTests.cs` (modified) | PHASE4F_3_TEST |
| `GeneratedCatalogPlanPayloadSerializationTests.cs` (modified) | PHASE4F_3_TEST |
| `CatalogPhaseAllocationResolverTests.cs` | PHASE4F_3_TEST |
| `CatalogRunLayoutResolverTests.cs` | PHASE4F_3_TEST |
| `CatalogStageToWeekContextFactoryTests.cs` | PHASE4F_3_TEST |
| `CatalogPlanSkeletonOrchestratorFixtures.cs` | PHASE4F_3_TEST |
| `CatalogPlanSkeletonOrchestratorTests.cs` | PHASE4F_3_TEST |
| `CatalogPlanSkeletonOrchestrationTerminologyTests.cs` | PHASE4F_3_TEST |
| `Phase4F3LiveBoundaryRegressionTests.cs` | PHASE4F_3_TEST |
| `PHASE4F_3_LIVE_CATALOG_STAGE_ALLOCATION_AND_SKELETON_INTEGRATION.md` | PHASE4F_3_DOCUMENTATION |
| `PHASE4F_3_CHECKPOINT_REPORT.md` | PHASE4F_3_DOCUMENTATION |
| `backend/*/bin/Release/`, `backend/*/obj/Release/` | GENERATED_ARTIFACT |
| `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md` | PRE_EXISTING_UNRELATED (excluded) |
| `baseline_tmp/` | PRE_EXISTING_UNRELATED (excluded) |

No file was found with `UNCLEAR_PROVENANCE`.

## Intentionally excluded files

- `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md` — pre-existing, unrelated to Phase 4F.3, never staged or touched.
- `baseline_tmp/` — pre-existing, unclear-provenance independent clone (273MB), never staged, touched, or deleted.

## Generated artifacts removed or restored

- Restored to committed state via `git checkout --` after the Release rebuild: `backend/RunningApp.IntegrationTests/obj/RunningApp.IntegrationTests.csproj.nuget.dgspec.json`, `backend/RunningApp.IntegrationTests/obj/project.assets.json`, `backend/RunningApp.IntegrationTests/obj/project.nuget.cache` (touched only by `dotnet build`/`restore`, no source content).
- `backend/*/bin/Release/`, `backend/*/obj/Release/` left as untracked generated output (never tracked by git; no destructive `git clean` used).
- No stray DLL, secret, connection string, or temporary JSON found in the diff.

## Loader gap identified

`PlanCatalogBundleLoader` previously parsed `templates/ten-k-master.v6.json`'s `phases[].phaseKey` into `PhaseKeys` but discarded each phase's `preferredWeeks` entirely — no typed representation of per-phase week allocation existed anywhere in the loaded runtime model.

## Loader extension implemented

A second pass over the same `phasesEl` JSON array (immediately following the existing `phaseKeys` pass, same `masterTemplateRef`) now also extracts `preferredWeeks` per phase via the existing `RequireInt` helper, producing `PlanCatalogPhaseAllocation(string PhaseKey, int PreferredWeeks)` records assigned to a new required `PlanCatalogCandidateSummary.PhaseAllocations` field. The change is purely additive: `PhaseKeys` is untouched, no other artifact (layout, level-modifier, rule-pack) parsing was touched, no public DTO changed, no database model changed, no migration added, no legacy SQL behavior changed.

## preferredWeeks preservation

Proven end-to-end against the real, unmodified `ten-k-master.v6.json` by `LoadCandidateAsync_ExposesPhaseAllocations_MatchingRepositoryPreferredWeeks` (`PlanCatalogBundleLoaderTests.cs`), asserting the loaded candidate's `PhaseAllocations` equals exactly `[("FOUNDATION",3),("BUILD",4),("RACE_SPECIFIC",4),("TAPER",1)]`, total 12 == `CoreCycle.DefaultWeeks`.

## PhaseAllocations model

`PlanCatalogPhaseAllocation(string PhaseKey, int PreferredWeeks)` — a new public record on `PlanCatalogCandidateSummary`, XML-documented to distinguish it from the finer workout-progression `stageKey` granularity.

## Candidate and dependency reference integrity

`CatalogPlanSkeletonOrchestrator.Build` Step 1 verifies the loaded candidate's `CandidateKey`/`CandidateVersion` against the caller's expected values (`CatalogSkeletonContextInvalidException` on mismatch); Steps 2 and 5 verify `MasterTemplate`/`Layout` references similarly. All four directly-loaded dependency references (`masterTemplate`, `layout`, `levelModifier`, `rulePack`) are carried through unchanged into `CatalogPlanSkeletonOrchestrationResult.DependencyVersions`.

## Master-template authority

`PlanCatalogCandidateSummary.MasterTemplate` is the single authoritative pinned reference; orchestrator Step 2 enforces it matches the caller's expectation before any further processing.

## Planned-week-count authority

`PlanCatalogCandidateSummary.CoreCycle.DefaultWeeks` (from `coreCycle.defaultWeeks`) is the sole authoritative planned-week-count; for the pilot this is `12`. Orchestrator Step 4 compares the resolved phase allocation's total against this value and throws `CatalogPhaseAllocationTotalMismatchException` on any mismatch — never normalized, truncated, or padded.

## Catalog-derived phase allocation

`CatalogPhaseAllocationResolver.Resolve` derives the phase sequence and week counts directly from `PlanCatalogCandidateSummary.PhaseAllocations` (itself sourced from `ten-k-master.v6.json`), validating non-empty, non-blank keys, no duplicates, no non-positive week counts. For the pilot this resolves to exactly `FOUNDATION=3, BUILD=4, RACE_SPECIFIC=4, TAPER=1`, total `12` — confirmed both by a resolver-level test against a hand-built pilot-shaped fixture and, independently, by a real catalog-file-backed end-to-end orchestrator test (`Build_RealPilotCandidate_MatchesExpectedPhaseSequence`). No 3/4/4/1 constant is hardcoded anywhere in production source (confirmed by `CatalogPhaseAllocationResolver_ContainsNoHardcodedPilotConstant`, which feeds a distinct BASE/PEAK/TAPER shape and observes it pass through unchanged).

## Catalog-derived run layout

`CatalogRunLayoutResolver.Resolve` derives the ordered structural role sequence directly from `PlanCatalogCandidateSummary.SlotRoles` (sourced from `run-layout-4d.v2.json`'s `slots[].role`), validating non-empty, non-blank, slot count == `DaysPerWeek`, and rejecting any REST/OPTIONAL/RECOVERY role (case-insensitive substring). For the pilot this resolves to exactly `["KEY_SESSION","EASY_SUPPORT","EASY_SUPPORT","LONG_RUN"]` — confirmed against the real catalog file via `Build_RealPilotCandidate_ProducesFourStructuralSlotsPerWeek`. No hardcoded array exists in production source (confirmed by `CatalogRunLayoutResolver_ContainsNoHardcodedLayoutArray`, feeding a 5-role non-pilot shape and observing pass-through).

## Phase/stage terminology distinction

`phaseKey` (week-allocation granularity, e.g. `FOUNDATION`/`BUILD`/`RACE_SPECIFIC`/`TAPER`) is distinct from the finer, nested workout-selection `stageKey` (e.g. `GOAL_PACE_REHEARSAL` in `ten-k-workout-progression.v5.json`), which Phase 4F.3 never reads. All new Phase 4F.3 types use precise phase terminology (`CatalogPhaseAllocationEntry.PhaseKey`/`PhaseWeekCount`). Confirmed structurally by `CatalogPlanSkeletonOrchestrationTerminologyTests` (4 tests): no workout-progression type is referenced by the resolver; the adapter translates phase values 1:1 into Phase 4F.2's stage-named fields without reordering or renaming values.

## Deferred terminology debt

Phase 4F.2's pre-existing `CatalogStageWeekAllocation.StageKey`/`WeekCount` and `GeneratedCatalogWeekSkeleton.StageKey` field names were **not** renamed in this checkpoint (no broad rename was performed). `CatalogStageToWeekContextFactory.Create`'s XML doc comment documents this explicitly as intentional, non-semantic terminology debt for a possible future phase, not an unnoticed inconsistency.

## Resolver boundaries

`ICatalogPhaseAllocationResolver`/`CatalogPhaseAllocationResolver` and `ICatalogRunLayoutResolver`/`CatalogRunLayoutResolver` are pure, dependency-free, `internal` classes operating only on an already-loaded `PlanCatalogCandidateSummary` — no catalog-file I/O, database, or clock access.

## Context factory

`ICatalogStageToWeekContextFactory`/`CatalogStageToWeekContextFactory` is the explicit phase→stage terminology adapter (§ above), preserving `StartDate`/`AsOfDate` exactly, candidate identity, and the four dependency references, and creating no default values (proven by `CatalogStageToWeekContextFactoryTests`, 10/10).

## Orchestrator validation order

`CatalogPlanSkeletonOrchestrator.Build` runs, in exact order: (1) candidate identity integrity, (2) master-template reference match, (3) planned-week-count authority read, (4) phase-allocation validity incl. total-vs-authority mismatch, (5) run-layout reference match, (6) run-layout slot validity, (7) materialization-context construction, (8) Phase 4F.2 materialization, (9) skeleton output validation — stopping and throwing immediately on the first failure, never returning a partial result.

## Error taxonomy

All 8 required typed `internal` exceptions exist in `CatalogPlanSkeletonOrchestrationExceptions.cs`: `CatalogPhaseAllocationSourceMissingException`, `CatalogPhaseAllocationInvalidException`, `CatalogPhaseAllocationTotalMismatchException`, `CatalogMasterTemplateReferenceMismatchException`, `CatalogRunLayoutReferenceMismatchException`, `CatalogRunLayoutSlotInvalidException`, `CatalogSkeletonContextInvalidException`, `CatalogPlanSkeletonOrchestrationFailedException`. None are registered in `GlobalExceptionHandler` (no live caller). Materializer exceptions are caught by exact type and re-thrown wrapped as `CatalogPlanSkeletonOrchestrationFailedException` with the original exception preserved as `InnerException` — never swallowed, never downgraded to a default skeleton, no SQL fallback exists.

## Determinism and dependency isolation

`CatalogPlanSkeletonOrchestrator`'s constructor takes only its 5 typed dependencies — no `DbContext`, no `HttpContext` (confirmed via reflection in `Phase4F3LiveBoundaryRegressionTests.CatalogPlanSkeletonOrchestrator_HasNoDatabaseOrHttpDependency`). `Build` is a pure function of its context argument; determinism proven by invoking it twice with identical input against the real v10 candidate and asserting structurally identical output (`Build_RealPilotCandidate_IsDeterministic_SameInputProducesStructurallyEquivalentOutput`).

## Artifact-version provenance

`CatalogPlanSkeletonOrchestrationResult.DependencyVersions` and the underlying skeleton's `Provenance` carry the exact loaded `PlanCatalogReference(Key, Version)` for all four dependencies, proven against the real candidate's actual loaded versions.

## Option A verification

Confirmed: `CatalogPreviewGenerator.cs` contains zero references to `Materialization`/`Orchestrator`/`Skeleton`; its single constructor's parameter list is unchanged. `CatalogPlanConfirmationService.cs` contains no reference to the new orchestration namespace (the only match, `CatalogPreviewMaterializationNotImplementedException`, is a pre-existing, unrelated Phase 4E.2/4F.1 exception). No `CatalogPreviewSnapshotBuilder.cs` class exists as a separate file; `CatalogPreviewSnapshot.cs` and `CatalogPreviewSnapshotVerifier.cs` contain no orchestration/skeleton references. `GeneratedPreviewPlanPayload` remains typed exclusively as `GeneratedCatalogPlanPayload?` (Phase 4F.1 final-contract type).

## Proof that live preview is unwired

`Phase4F3LiveBoundaryRegressionTests.CatalogPreviewGenerator_ConstructorDependencies_DoNotIncludeOrchestratorType` and `CatalogPlanConfirmationService_ConstructorDependencies_DoNotIncludeOrchestratorType` reflect over each service's single constructor and assert no parameter type name contains `CatalogPlanSkeletonOrchestrator`.

## Proof that snapshot payload remains null

`RealCatalogPreviewGeneration_StillProducesNullGeneratedPreviewPlanPayload` re-exercises the real, non-dry-run pilot preview path end-to-end: throws `CatalogCandidateNotPublishedException` (v10 is DRAFT), creates zero `PlanPreview` rows.

## Proof that preview hashing remains unchanged

`CatalogPreviewSnapshotVerifier.cs` (unchanged file, confirmed by `git diff --name-status` showing no modification) contains no orchestration/skeleton reference; the pre-existing snapshot-hash regression tests (`CatalogPreviewSnapshotVerifierTests`, part of the 8 targeted regression tests run in Stage 12) remain green, unmodified.

## Proof that confirm remains gated

`CatalogPlanConfirmationService.cs` is unmodified (confirmed via `git diff --name-status`); its pre-existing null-payload-blocks-confirmation guard is untouched. `CatalogPlanConfirmationServiceTests` (25/25, including the one compile-fix-only modification) remain green.

## Proof that persistence remains absent

`Phase4F3LiveBoundaryRegressionTests.ConfirmAsync_RemainsGated_NoTrainingPlanArtifactsExistFromOrchestration` runs the orchestrator standalone against an in-memory `AppDbContext` and asserts zero `TrainingPlans`/`PlanPreviews` rows exist afterward.

## Proof that public DTOs remain unchanged

`PublicPreviewDTOs_ExposeNoPhaseAllocationOrSkeletonContent` reflects over `GeneratePreviewResponse`, `PreviewWeekDto`, `PreviewDayDto`, `ConfirmPlanResponse`, asserting no property name contains `Phase`/`Skeleton`/`StructuralRole` and no property type references `Materialization`. `CatalogPreviewSnapshot_HasNoSkeletonProperty` confirms the snapshot type itself. `git diff` confirms zero DTO source files were touched.

## Candidate lifecycle status

`TEN_K__4D__INTERMEDIATE v10` remains DRAFT; no publish/activation action was taken this phase or this checkpoint.

## Public activation blockers

The existing PUBLISHED-only eligibility gate (unchanged) continues to reject the real, non-dry-run preview path for v10, confirmed by the live-boundary regression test in this checkpoint's run.

## Focused test results

- Phase 4F.2 + Phase 4F.3 `Schedule.Materialization` tests: **104/104 passed**
- Loader-extension (`PlanCatalogBundleLoaderTests`): **11/11 passed**
- Confirm/persistence regression (`CatalogPlanConfirmationServiceTests`): **25/25 passed**
- Preview snapshot/payload serialization regression: **8/8 passed**

## RuntimeCatalog test results

`dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~RuntimeCatalog"` → **501 passed, 0 failed, 0 skipped, 501 total**.

## Full-suite test results

`dotnet test RunningApp.sln -c Release --no-build` → **544 passed, 0 failed, 0 skipped, 544 total**, duration ~8s.

## Exact test-count reconciliation

488 (Phase 4F.2 baseline) + 56 (Phase 4F.3 new tests: 12 phase-allocation resolver + 10 run-layout resolver + 10 context factory + 11 end-to-end orchestration + 4 terminology + 8 live-boundary regression + 1 loader-extension) = **544**, exactly matching the observed full-suite result.

## Files included in checkpoint

All items classified `PHASE4F_3_LOADER_SOURCE`, `PHASE4F_3_APPLICATION_SOURCE`, `PHASE4F_3_TEST`, and `PHASE4F_3_DOCUMENTATION` above (17 files total: 6 modified + 11 new).

## Files excluded from checkpoint

`PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md`, `baseline_tmp/`, all `bin/Release`/`obj/Release` generated output.

## Deferred work

Calendar-day assignment, preferred-day/long-run-day-preference consumption, weekly volume calculation, workout selection, distance/duration/pace/intensity/segment calculation, final schedule payload construction, week/day persistence, catalog confirm enablement, concurrent-confirm protection, v10 publication, wiring the orchestrator into any live request path, `StageKey`→`PhaseKey` rename migration.

## Final classification

**`PHASE4F_3_CHECKPOINT_VERIFIED_INTERNAL_SKELETON_ORCHESTRATION_NOT_WIRED_NOT_PUBLICLY_ACTIVATABLE`**
