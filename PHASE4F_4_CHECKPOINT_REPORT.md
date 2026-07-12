# Phase 4F.4 Checkpoint Report

## Repository state before checkpoint

Branch: `main`. HEAD before this checkpoint: `ac696756d9d195323c971d535cec954fecb973b4` (Phase 4F.3 checkpoint). Prior checkpoints: `a0ca152e17e6f832a1c5b48c3b0f050643b93ac0` (Phase 4F.1), `d4ebbf05d6853655a0272c5a7bd3fdaa3af78ec6` (Phase 4F.2), `ac696756d9d195323c971d535cec954fecb973b4` (Phase 4F.3).

## Current branch and HEAD

`main`, HEAD = `ac696756d9d195323c971d535cec954fecb973b4` prior to this commit.

## Complete file inventory

Modified (tracked):
- `backend/RunningApp.Api/Program.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPreviewGeneratorTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/TestPlanServicesFactory.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Phase4F3LiveBoundaryRegressionTests.cs`

New (untracked, intended for this checkpoint):
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F4DarkSkeletonWiringTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F4ConfirmAndLegacyRegressionTests.cs`
- `PHASE4F_4_DARK_INTERNAL_SKELETON_WIRING_INTO_CATALOG_PREVIEW.md`
- `PHASE4F_4_CHECKPOINT_REPORT.md` (this file)

New (untracked, excluded from checkpoint):
- `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md`
- `baseline_tmp/`
- `backend/*/bin/Release/`, `backend/*/obj/Release/` (generated build output)

## File provenance classification

| File | Classification |
|---|---|
| `Program.cs` (comment only) | PHASE4F_4_SOURCE |
| `CatalogPreviewGenerator.cs` | PHASE4F_4_SOURCE |
| `CatalogPreviewGeneratorTests.cs` (updated call sites) | PHASE4F_4_TEST |
| `TestPlanServicesFactory.cs` (updated call site) | PHASE4F_4_TEST |
| `Phase4F3LiveBoundaryRegressionTests.cs` (one test renamed, assertion unchanged) | PHASE4F_4_TEST |
| `Phase4F4DarkSkeletonWiringTests.cs` | PHASE4F_4_TEST |
| `Phase4F4ConfirmAndLegacyRegressionTests.cs` | PHASE4F_4_TEST |
| `PHASE4F_4_DARK_INTERNAL_SKELETON_WIRING_INTO_CATALOG_PREVIEW.md` | PHASE4F_4_DOCUMENTATION |
| `PHASE4F_4_CHECKPOINT_REPORT.md` | PHASE4F_4_DOCUMENTATION |
| `backend/*/bin/Release/`, `backend/*/obj/Release/` | GENERATED_ARTIFACT |
| `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md` | PRE_EXISTING_UNRELATED (excluded) |
| `baseline_tmp/` | PRE_EXISTING_UNRELATED (excluded) |

No file was found with `UNCLEAR_PROVENANCE`. No file outside this set was touched — `CatalogPreviewSnapshot.cs`, `CatalogPlanConfirmationService.cs`, and every public DTO under `RunningApp.Application/DTOs/Plan/` are confirmed unmodified (`git diff --name-status` shows nothing for those paths).

## Intentionally excluded files

- `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md` — pre-existing, unrelated, never staged or touched.
- `baseline_tmp/` — pre-existing, unclear-provenance independent clone, never staged, touched, or deleted.

## Generated artifacts removed or restored

- Restored to committed state via `git checkout --` after the Release rebuild: `backend/RunningApp.IntegrationTests/obj/RunningApp.IntegrationTests.csproj.nuget.dgspec.json`, `backend/RunningApp.IntegrationTests/obj/project.assets.json`, `backend/RunningApp.IntegrationTests/obj/project.nuget.cache` (touched only by `dotnet build`/`restore`, no source content).
- `backend/*/bin/Release/`, `backend/*/obj/Release/` left as untracked generated output (never tracked by git; no destructive `git clean` used).
- No stray DLL, secret, connection string, or temporary JSON found in the diff.

## Live preview orchestration path before Phase 4F.4

`CatalogPreviewGenerator.GenerateAsync`: load+PUBLISHED-gate candidate → build resolver input/context → `RuntimeConditionResolutionService.ResolveAllResults` (with `InvalidOperationException`/`ArgumentException` wrapped as `PlanPreviewGenerationFailedException`) → `ApplyNotEvaluatedGovernancePolicy` → build decision trace → `CatalogPreviewSnapshotBuilder.Build`. No skeleton orchestration existed in this path prior to this phase.

## Selected dark-wiring insertion point

`CatalogPreviewGenerator.cs` line 147: `BuildDarkInternalSkeleton(candidate, asOfDate);`, placed immediately after `ApplyNotEvaluatedGovernancePolicy(results);` (line 135) and before `BuildDecisionTrace(results)` (line 149) / snapshot construction (lines 152-154).

## Authoritative inputs available at insertion

At this exact source location: the request is definitively routed to catalog preview (method only reachable via `GenerationSource.Catalog`); `candidate` is loaded and PUBLISHED-gated (line 99); the resolver pipeline has fully run and passed governance policy (lines 110-135); `asOfDate` is the method's own frozen parameter; `candidate.MasterTemplate`/`candidate.Layout` (pinned identity/version) and `candidate.PhaseAllocations`/`SlotRoles` are already loaded as part of `candidate`. Nothing is recomputed to reach this point.

## Constructor-accessibility constraint

Adding `ICatalogPlanSkeletonOrchestrator` as a fourth public constructor parameter produced compiler error CS0051 ("inconsistent accessibility"): `CatalogPreviewGenerator`'s constructor is `public`, but `ICatalogPlanSkeletonOrchestrator` (and every Phase 4F.3 orchestration type) is deliberately `internal` to `RunningApp.Application`. Adding `InternalsVisibleTo("RunningApp.Api")` to relax this was tried and found insufficient: `InternalsVisibleTo` only relaxes cross-assembly compile-time member access, not the public-member-signature-consistency rule that CS0051 enforces — a public constructor can never expose a less-accessible parameter type, in any assembly.

## Why public DI injection was not used

Registering the Phase 4F.3 types directly in `RunningApp.Api/Program.cs` (as the task's own illustrative approach suggested) would force one of two unwanted outcomes: (a) making `ICatalogPlanSkeletonOrchestrator` and its dependency closure `public`, reopening Phase 4F.3's own deliberate internal-only boundary decision, or (b) granting `RunningApp.Api` a same-assembly-only workaround that still could not satisfy CS0051 for the constructor itself. Both were rejected in favor of composition-root construction.

## Internal composition-root design

`CatalogPreviewGenerator`'s public constructor (`ICatalogCandidateEligibilityGate`, `RuntimeConditionResolutionService`) now delegates via `: this(gate, orchestration, DefaultSkeletonOrchestrator())` to an `internal` three-parameter constructor. `DefaultSkeletonOrchestrator()` is a `private static` factory building a `CatalogPlanSkeletonOrchestrator` from five pure `new`-constructed collaborators (`CatalogPhaseAllocationResolver`, `CatalogRunLayoutResolver`, `CatalogStageToWeekContextFactory`, `CatalogStageToWeekMaterializer`, `GeneratedCatalogPlanSkeletonValidator`) — identical composition to `TestPlanServicesFactory`/`CatalogPlanSkeletonOrchestratorFixtures.RealOrchestrator()` used throughout Phase 4F.3.

## Public constructor compatibility

`public CatalogPreviewGenerator(ICatalogCandidateEligibilityGate gate, RuntimeConditionResolutionService orchestration)` — byte-for-byte identical signature to Phase 4E.1. `Program.cs`'s existing `AddScoped<ICatalogPreviewGenerator, CatalogPreviewGenerator>()` registration required no change.

## Internal test seam

`internal CatalogPreviewGenerator(ICatalogCandidateEligibilityGate gate, RuntimeConditionResolutionService orchestration, ICatalogPlanSkeletonOrchestrator skeletonOrchestrator)` — accessible only from `RunningApp.IntegrationTests` (pre-existing `InternalsVisibleTo` grant, unchanged this phase) and from within `RunningApp.Application` itself (the public constructor's own delegation). Not reachable from `RunningApp.Api` or any other external consumer.

## Collaborator purity and statelessness

All five `DefaultSkeletonOrchestrator()` collaborators, and `CatalogPlanSkeletonOrchestrator` itself, were verified (Phase 4F.3, re-confirmed this phase) to have zero `DbContext`, HTTP-context, wall-clock, authenticated-user, or other mutable/request-scoped dependency — every constructor among them takes only pure, dependency-free or previously-verified-pure collaborators. No service-locator or `IServiceProvider` usage exists anywhere in `CatalogPreviewGenerator.cs` or `Program.cs`'s diff.

## No-rerun guarantees

`BuildDarkInternalSkeleton` reads only the already-computed `candidate` and `asOfDate` local variables — it never calls `_gate` or `_orchestration` again. Proven by `Phase4F4DarkSkeletonWiringTests.NoReruns_EligibilityGateCalledExactlyOnce` (a counting gate double asserts exactly one load per `GenerateAsync` call) and `DarkSkeleton_Failure_NoRetryRerunsResolverOrchestration`.

## No-duplicate-load guarantees

No new catalog-file read, master-template reload, or run-layout reload was introduced — `CatalogPlanSkeletonOrchestrationContext` is built purely from fields already present on the already-loaded `candidate` object; the orchestrator's own resolvers (`CatalogPhaseAllocationResolver`/`CatalogRunLayoutResolver`) read only `candidate.PhaseAllocations`/`SlotRoles`, never touching `IPlanCatalogBundleLoader` themselves (confirmed, Phase 4F.3, unchanged).

## Context-construction behavior

`CatalogPlanSkeletonOrchestrationContext { Candidate = candidate, ExpectedCandidateKey = candidate.CandidateKey, ExpectedCandidateVersion = candidate.CandidateVersion, ExpectedMasterTemplate = candidate.MasterTemplate, ExpectedRunLayout = candidate.Layout, StartDate = asOfDate, AsOfDate = asOfDate }` — every field sourced directly from already-authoritative live inputs; no default invented, no phase allocation or run-layout slot fabricated (those come from the orchestrator's own resolvers reading `candidate` data, per Phase 4F.3, unchanged).

## Dark skeleton invocation

`_skeletonOrchestrator.Build(skeletonContext)`, called inside a `try` block; the returned `CatalogPlanSkeletonOrchestrationResult` is not assigned to any field, return value, log statement, or snapshot — it is discarded entirely upon return.

## Dark-result lifetime

Request-scoped and ephemeral: the result exists only as an unused expression-statement return value within `BuildDarkInternalSkeleton`'s stack frame, garbage-collected immediately after the method returns. It is never retained beyond `GenerateAsync`'s own execution.

## Validation authority

`CatalogPlanSkeletonOrchestrator.Build`'s full 9-step validation order (Phase 4F.3, unchanged) governs whether the call throws; a successful return is the only path by which `BuildDarkInternalSkeleton` returns normally, so "the orchestrator ran successfully" is a real, load-bearing precondition for preview construction to continue — never a bypassed or ignored check.

## Failure wrapping and typed-cause preservation

All 8 Phase 4F.3 typed exceptions are caught by exact type (`CatalogPhaseAllocationSourceMissingException`, `CatalogPhaseAllocationInvalidException`, `CatalogPhaseAllocationTotalMismatchException`, `CatalogMasterTemplateReferenceMismatchException`, `CatalogRunLayoutReferenceMismatchException`, `CatalogRunLayoutSlotInvalidException`, `CatalogSkeletonContextInvalidException`, `CatalogPlanSkeletonOrchestrationFailedException`) and re-thrown as `PlanPreviewGenerationFailedException(message, ex)` — the pre-existing two-argument constructor already used elsewhere in this same method, preserving the original exception as `InnerException`. No new public error code was introduced (`PLAN_PREVIEW_GENERATION_FAILED` already existed and already covers "the generation pipeline failed for a typed reason").

## Global exception sanitization

Confirmed by reading `GlobalExceptionHandler.cs`: `PlanPreviewGenerationFailedException` maps to HTTP 500 / `PLAN_PREVIEW_GENERATION_FAILED`, and for every 500-status exception the response body always contains the fixed string `"An unexpected error occurred."` — `exception.Message` (which may contain phase/layout/candidate detail) is never echoed to the client.

## Real DRAFT candidate behavior

`TEN_K__4D__INTERMEDIATE v10`'s real catalog JSON was never modified. `RealDraftCandidate_StillFailsAtPublishedOnlyGate_NeverInvokesSkeletonOrchestration` uses a `CountingSkeletonOrchestrator` double and the real (non-dry-run) `CatalogCandidateEligibilityGate` against the real catalog source tree, asserting `CatalogCandidateNotPublishedException` is thrown and `InvocationCount == 0`.

## Snapshot boundary

`CatalogPreviewSnapshot.cs` is unmodified (confirmed via `git diff --name-status`) — no new property, no schema version change. `DarkSkeleton_Success_Produces12WeekSkeleton_NotSurfacedAnywhere` reflects over the snapshot's own properties and asserts none contains `"Skeleton"` in its name.

## Hash boundary

`CatalogPreviewSnapshotBuilder.Build`'s `hashableContent` anonymous object (in the unmodified `CatalogPreviewSnapshot.cs`) is unchanged — it never referenced the skeleton before this phase and still does not. `DarkSkeleton_Success_SnapshotAndHashStructurallyUnchanged_FromPre4F4Shape` proves a real dark-materializing preview still produces the same structural shape (4 resolver results, empty stage-key lists, null payload, non-blank hash, 8 referenced artifacts) as every pre-4F.4 snapshot.

## GeneratedPreviewPlanPayload boundary

Never set — `CatalogPreviewSnapshotBuilder.Build` is called with the same argument list as before (the optional `generatedPreviewPlanPayload` parameter is never supplied). Proven by `DarkSkeleton_Success_Produces12WeekSkeleton_NotSurfacedAnywhere` (`Assert.Null(snapshot.GeneratedPreviewPlanPayload)`) and the unmodified Phase 4F.3 `RealCatalogPreviewGeneration_StillProducesNullGeneratedPreviewPlanPayload`.

## Public DTO boundary

No DTO file was touched (`git diff --name-status` shows nothing under `RunningApp.Application/DTOs/Plan/`). The unmodified Phase 4F.3 reflection test `PublicPreviewDTOs_ExposeNoPhaseAllocationOrSkeletonContent` still passes.

## Confirm boundary

`CatalogPlanConfirmationService.cs` unmodified. `CatalogPlanConfirmationService_ConstructorSurface_UnchangedByPhase4F4` (new) proves its constructor remains exactly 3 parameters with no `CatalogPlanSkeletonOrchestrator`/`Materialization` reference; `CatalogPlanConfirmationService_NeverInvokesSkeletonOrchestration_NoTypeReferenceAnywhereInAssembly` proves no instance field of that type exists.

## Persistence boundary

No persistence code added anywhere. `PlanServices.GenerateCatalogPreviewAsync` (unmodified) still persists only the unchanged-shape `CatalogPreviewSnapshot` JSON. The full, unmodified `CatalogPlanConfirmationServiceTests` (25/25 green) re-confirms no `TrainingPlan`/`TrainingWeek`/`TrainingDay`/`PlanEvent` is created and `ConfirmedPlanId` remains null on rejection.

## Legacy SQL boundary

`PlanServices.cs`'s route dispatch (unmodified) still sends non-catalog requests down the entirely separate legacy code path before `CatalogPreviewGenerator` is ever constructed or called — the dark skeleton call site is unreachable from that path. `PilotGenerationRouteDeciderTests` (unmodified, still green) continues to cover legacy routing decisions.

## Candidate lifecycle status

`TEN_K__4D__INTERMEDIATE v10` remains DRAFT; no publish/activation action was taken this phase or this checkpoint.

## Public activation blockers

The existing PUBLISHED-only eligibility gate (unchanged) continues to reject the real, non-dry-run preview path for v10 before the dark skeleton call site is reachable.

## Focused test results

`Phase4F4DarkSkeletonWiringTests` + `Phase4F4ConfirmAndLegacyRegressionTests`: **21/21 passed**.

## RuntimeCatalog test results

`dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~RuntimeCatalog"` → **522 passed, 0 failed, 0 skipped, 522 total**.

## Confirm/persistence regression results

`CatalogPlanConfirmationServiceTests`: **25/25 passed**.

## Snapshot/hash regression results

`CatalogPreviewGeneratorTests` + `CatalogPreviewSnapshotVerifierTests`: **3/3 passed**.

## Full-suite results

`dotnet test RunningApp.sln -c Release --no-build` → **565 passed, 0 failed, 0 skipped, 565 total**, duration ~8s.

## Exact test-count reconciliation

544 (Phase 4F.3 baseline) + 21 (Phase 4F.4 new: 19 in `Phase4F4DarkSkeletonWiringTests.cs` including 8 theory cases + 2 in `Phase4F4ConfirmAndLegacyRegressionTests.cs`) = **565**, matching the observed full-suite result exactly. One pre-existing test was renamed (`CatalogPreviewGenerator_ConstructorDependencies_DoNotIncludeOrchestratorType` → `CatalogPreviewGenerator_PublicConstructorSurface_DoesNotTakeOrchestratorAsAParameter`) with its assertion logic byte-for-byte unchanged; not counted as new.

## Files included in checkpoint

All items classified `PHASE4F_4_SOURCE`, `PHASE4F_4_TEST`, and `PHASE4F_4_DOCUMENTATION` above (9 files: 5 modified + 4 new).

## Files excluded from checkpoint

`PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md`, `baseline_tmp/`, all `bin/Release`/`obj/Release` generated output.

## Deferred work

Calendar-day assignment, preferred-day/long-run-day-preference consumption, weekly volume/distance/duration/pace/intensity/segment calculation, final schedule payload construction, week/day persistence, catalog confirm enablement, concurrent-confirm protection, v10 publication, `StageKey`→`PhaseKey` rename migration, structured logging of dark skeleton outcomes.

## Final classification

**`PHASE4F_4_CHECKPOINT_VERIFIED_DARK_INTERNAL_PREVIEW_WIRING_WITHOUT_PUBLIC_SCHEDULE_OUTPUT`**
