# Phase 4F.4 — Dark Internal Skeleton Wiring into Catalog Preview

## Repository state before implementation

HEAD = `ac696756d9d195323c971d535cec954fecb973b4` (Phase 4F.3 checkpoint). Release build: 0 errors, 0 warnings. Full suite: 544 passed / 0 failed / 0 skipped. `RuntimeCatalog`-filtered: 501/501. `TEN_K__4D__INTERMEDIATE v10` remained DRAFT. `CatalogPlanSkeletonOrchestrator` existed but was not called by `CatalogPreviewGenerator`. Working tree clean except the two known excluded items (`PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md`, `baseline_tmp/`), which were left untouched throughout this phase.

## Files inspected

- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshot.cs` (contains `CatalogPreviewSnapshotBuilder`; no separate `CatalogPreviewSnapshotBuilder.cs` file exists)
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogCandidateEligibilityGate.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/PilotGenerationRouteDecider.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogBundleLoader.cs`, `PlanCatalogCandidateSummary.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Resolvers/RuntimeConditionResolutionService.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/StageEligibilityEvaluator.cs` (confirmed unreferenced by any live path — stage-to-week scheduling remains unimplemented since Phase 4E.1)
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestrator.cs`, `CatalogPhaseAllocation.cs`, `CatalogRunLayoutSlots.cs`, `CatalogPlanSkeletonOrchestrationExceptions.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogStageToWeekMaterializer.cs`, `GeneratedCatalogPlanSkeletonValidator.cs`
- `backend/RunningApp.Application/Services/PlanServices.cs`
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs`
- `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs`
- `backend/RunningApp.Api/Program.cs`
- `backend/RunningApp.Application/RunningApp.Application.csproj`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/TestPlanServicesFactory.cs`, `CatalogPreviewGeneratorTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Phase4F3LiveBoundaryRegressionTests.cs`, `CatalogPlanSkeletonOrchestratorFixtures.cs`

## Current live preview orchestration path

`CatalogPreviewGenerator.GenerateAsync` is the exact class/method owning the successful catalog preview path. Sequence: (1) `_gate.LoadForPublicPreviewAsync` loads and PUBLISHED-gates the candidate — candidate identity and all four directly-loaded pinned dependencies (`masterTemplate`, `layout`, `levelModifier`, `rulePack`) become authoritative here; (2) a `RuntimeResolverContext` is built from the request and the already-fixed `asOfDate` parameter; (3) `_orchestration.ResolveAllResults(context)` runs the full resolver pipeline, with `InvalidOperationException`/`ArgumentException` mapped to `PlanPreviewGenerationFailedException`; (4) `ApplyNotEvaluatedGovernancePolicy(results)` — the point at which the resolver outcome is authoritative and preview-blocking failures are thrown; (5) `CatalogPreviewSnapshotBuilder.Build(...)` freezes everything into the returned `CatalogPreviewSnapshot`. `StageEligibilityEvaluator` is never called anywhere in this live path (stage-to-week scheduling remains unimplemented) — the resolver pipeline plus its governance policy is the closest existing equivalent to a "stage/eligibility evaluation" gate in the current architecture.

## Selected dark-wiring insertion point

Immediately after `ApplyNotEvaluatedGovernancePolicy(results);` succeeds and before `BuildDecisionTrace(results)` (i.e., before snapshot construction begins), via a new private helper `BuildDarkInternalSkeleton(candidate, asOfDate)`.

## Why the insertion point is safe

At this exact point: the request is definitively routed to catalog preview (this method only runs for `GenerationSource.Catalog`); the exact candidate (`TEN_K__4D__INTERMEDIATE`) is selected and loaded; candidate status/lifecycle has passed the PUBLISHED-only gate (step 1); candidate eligibility (dependency status) has passed (same gate call); runtime-condition resolution has fully completed and passed governance policy (steps 3–4); `asOfDate` is fixed (the method's own parameter, never recomputed); the candidate's pinned `MasterTemplate` and `Layout` references are already loaded as part of `candidate` (a `PlanCatalogCandidateSummary`). No prior decision is rerun to reach this point, and nothing after it (snapshot construction) depends on the skeleton's existence.

## Inputs already authoritative at insertion

`candidate.CandidateKey`/`CandidateVersion`/`CandidateStatus`, `candidate.MasterTemplate`/`Layout` (pinned identity/version), `candidate.PhaseAllocations`/`SlotRoles` (catalog-derived), `asOfDate` (frozen). `candidate.Level`/`CanonicalDistanceFamily` are also already available on `candidate` though not separately threaded through the orchestration context (the Phase 4F.3 context type does not need them directly — `CanonicalDistanceFamily` and `DaysPerWeek` are read from `candidate`/the resolved run layout inside `CatalogStageToWeekContextFactory`, unchanged from Phase 4F.3).

## Context construction

`BuildDarkInternalSkeleton` builds a `CatalogPlanSkeletonOrchestrationContext` with `Candidate = candidate`, `ExpectedCandidateKey/Version = candidate.CandidateKey/CandidateVersion` (self-consistency defensive check, matching the Phase 4F.3 fixture convention), `ExpectedMasterTemplate = candidate.MasterTemplate`, `ExpectedRunLayout = candidate.Layout`, and `StartDate = AsOfDate = asOfDate`. `StartDate` mirrors `AsOfDate` exactly as `BuildInputSnapshot` already does elsewhere in this class (the pre-existing, documented Phase 4E.1 simplification) — no new date policy was introduced. No defaults are inserted; no wall clock, database, or HTTP context is read.

## DI changes

**No DI registration was added.** `ICatalogPlanSkeletonOrchestrator` and its Phase 4F.3 collaborators are deliberately `internal` to `RunningApp.Application` (that phase's own boundary decision). Registering them by name in `RunningApp.Api/Program.cs` (a separate assembly with no `InternalsVisibleTo` grant) would require either (a) elevating those types to `public` — reopening a public-surface decision this phase does not need to make — or (b) adding `InternalsVisibleTo("RunningApp.Api")`, which was tried first and abandoned: it produces CS0051 ("inconsistent accessibility") because `CatalogPreviewGenerator`'s public constructor cannot expose an internal parameter type regardless of `InternalsVisibleTo`, since that attribute only relaxes cross-assembly *compile-time* accessibility, not the public-member-signature-consistency rule.

Instead, `CatalogPreviewGenerator` keeps its original two-parameter public/DI-facing constructor unchanged and composes a default `ICatalogPlanSkeletonOrchestrator` internally via a `private static DefaultSkeletonOrchestrator()` factory method (pure `new` composition of five pure, stateless, dependency-free Phase 4F.3 types — no `DbContext`/`HttpContext`/clock/catalog-loader dependency among them). This is constructed once per `CatalogPreviewGenerator` instance, which is itself `Scoped` in DI (unchanged registration in `Program.cs`) — functionally equivalent to a `Scoped` DI registration without requiring one. A second, `internal` three-parameter constructor overload (visible to `RunningApp.IntegrationTests` via the pre-existing `InternalsVisibleTo`) lets tests substitute a fake/spy orchestrator without widening the public API.

`Program.cs` itself only gained a comment explaining this decision — no new `AddScoped`/`AddSingleton` line and no change to any existing registration.

## Orchestrator invocation

```csharp
BuildDarkInternalSkeleton(candidate, asOfDate);
```
called between governance-policy success and snapshot construction. The private helper builds the context (above), calls `_skeletonOrchestrator.Build(skeletonContext)`, and discards the returned `CatalogPlanSkeletonOrchestrationResult` entirely — no field, snapshot property, or log statement retains it.

## Validation authority

`Build()`'s own 9-step validation order (Phase 4F.3, unchanged) is authoritative: any invalid catalog data at any step throws before this call returns, and the call's only externally observable effect on success is "nothing changes" (preview construction proceeds as before). This gives the dark invocation genuine correctness weight — it is never a no-op stub.

## Failure behavior

All 8 typed Phase 4F.3 orchestration exceptions (`CatalogPhaseAllocationSourceMissingException`, `CatalogPhaseAllocationInvalidException`, `CatalogPhaseAllocationTotalMismatchException`, `CatalogMasterTemplateReferenceMismatchException`, `CatalogRunLayoutReferenceMismatchException`, `CatalogRunLayoutSlotInvalidException`, `CatalogSkeletonContextInvalidException`, `CatalogPlanSkeletonOrchestrationFailedException`) are caught by exact type and re-thrown wrapped as the pre-existing `PlanPreviewGenerationFailedException` (no new public error code was introduced — this taxonomy member already exists precisely for "the generation pipeline failed for a typed reason," and is already used by this same method for `InvalidOperationException`/`ArgumentException` from the resolver pipeline). The original exception is preserved as `InnerException`; the message is prefixed `CATALOG_INTERNAL_SKELETON_MATERIALIZATION_FAILED:` for log/diagnostic distinguishability. `GlobalExceptionHandler` maps `PlanPreviewGenerationFailedException` to HTTP 500 / error code `PLAN_PREVIEW_GENERATION_FAILED`, and — confirmed by reading the handler — never echoes `exception.Message` to the client for any 500-status exception (only `"An unexpected error occurred."`), so no internal phase/layout/provenance detail can leak publicly.

## No-rerun guarantees

The dark call reads only already-loaded/already-computed local variables (`candidate`, `asOfDate`) — it does not call `_gate` or `_orchestration` again. Proven by `Phase4F4DarkSkeletonWiringTests.NoReruns_EligibilityGateCalledExactlyOnce` (a counting gate double asserts exactly one load) and `DarkSkeleton_Failure_NoRetryRerunsResolverOrchestration`.

## No-fallback guarantee

No `try/catch` in `GenerateCatalogPreviewAsync`/`GenerateAsync` ever routes to the legacy SQL flow — every exception (pre-existing or Phase 4F.4's new wrapped one) propagates out of `PlanServices.GenerateCatalogPreviewAsync` unchanged (confirmed: that method has no `try/catch` around `_catalogPreviewGenerator.GenerateAsync`, by design, unchanged this phase).

## DRAFT lifecycle behavior

`TEN_K__4D__INTERMEDIATE v10`'s real catalog JSON status was never touched. The real, non-dry-run path still throws `CatalogCandidateNotPublishedException` at `_gate.LoadForPublicPreviewAsync`, before the dark skeleton call site is ever reached — proven by `RealDraftCandidate_StillFailsAtPublishedOnlyGate_NeverInvokesSkeletonOrchestration` (a counting orchestrator double asserts zero invocations).

## Controlled eligible test fixture strategy

`Phase4F4DarkSkeletonWiringTests.LoadControlledPublishedCandidateAsync()` loads the real v10 candidate's real catalog data via the pre-existing, documented `ICatalogCandidateEligibilityGate.LoadForInternalDryRunAsync` entry point, then constructs a new, purely in-memory `PlanCatalogCandidateSummary` copying every field but with `CandidateStatus` and every `DependencyStatuses` entry overridden to `"PUBLISHED"`. The real catalog JSON file's `metadata.status` field was never written to; only an in-memory clone's field is overridden, exactly matching the task's "cloned in-memory candidate fixture" option.

## Snapshot boundary

`CatalogPreviewSnapshot.cs` was not modified — no new property was added. `Phase4F4DarkSkeletonWiringTests.DarkSkeleton_Success_Produces12WeekSkeleton_NotSurfacedAnywhere` reflects over the snapshot's own properties and asserts none contains `"Skeleton"` in its name.

## Hash boundary

`CatalogPreviewSnapshotBuilder.Build`'s `hashableContent` anonymous object (unchanged) never referenced the skeleton before this phase and still does not — no source line in `CatalogPreviewSnapshot.cs` changed. `DarkSkeleton_Success_SnapshotAndHashStructurallyUnchanged_FromPre4F4Shape` proves a real dark-materializing preview still produces the same structural shape (4 resolver results, empty stage-key lists, null payload, non-blank hash, 8 referenced artifacts) as every pre-4F.4 snapshot.

## GeneratedPreviewPlanPayload boundary

Never set by this phase — `CatalogPreviewSnapshotBuilder.Build` is called with the same argument list as before (the optional `generatedPreviewPlanPayload` parameter is never supplied by `CatalogPreviewGenerator`). Proven directly by `DarkSkeleton_Success_Produces12WeekSkeleton_NotSurfacedAnywhere` (`Assert.Null(snapshot.GeneratedPreviewPlanPayload)`) and by the pre-existing, unmodified `RealCatalogPreviewGeneration_StillProducesNullGeneratedPreviewPlanPayload` (Phase 4F.3, still green).

## Public DTO boundary

No DTO file was touched. `GeneratePreviewResponse`, `PreviewWeekDto`, `PreviewDayDto`, `ConfirmPlanResponse` are unmodified (confirmed via `git diff --name-status`); the pre-existing Phase 4F.3 reflection test `PublicPreviewDTOs_ExposeNoPhaseAllocationOrSkeletonContent` still passes unchanged.

## Confirm boundary

`CatalogPlanConfirmationService.cs` was not modified. Its constructor remains exactly 3 parameters (`AppDbContext`, `ILogger`, `IGeneratedCatalogPlanPayloadValidator`) — proven by the new `CatalogPlanConfirmationService_ConstructorSurface_UnchangedByPhase4F4` test, and confirmed structurally that no instance field references `CatalogPlanSkeletonOrchestrator`. Confirm never invokes preview generation or skeleton orchestration at all (unchanged architecture: confirm reads only the already-stored `CatalogPreviewSnapshot` JSON).

## Persistence boundary

No new persistence code was added anywhere. `PlanServices.GenerateCatalogPreviewAsync` still persists only the (unchanged-shape) `CatalogPreviewSnapshot` JSON into `PlanPreview.PreviewPayloadJson`. No `TrainingPlan`/`TrainingWeek`/`TrainingDay`/`PlanEvent` is created by preview generation, dark skeleton materialization, or (still-gated) confirm — re-confirmed by the full, unmodified pre-existing `CatalogPlanConfirmationServiceTests` (25/25 green).

## Logging/observability decision

No logging was added for the dark skeleton result. `CatalogPreviewGenerator` has no `ILogger` dependency at all (unchanged), and adding one solely to log a minimal success signal was judged out of the phase's minimal scope ("logging is optional and must not expand scope") — introducing a new constructor dependency purely for an optional structured log line was not worth the added surface area for a phase whose real public path is unreachable anyway (v10 still DRAFT).

## Build results

`dotnet build RunningApp.sln -c Release`: 0 errors, 0 warnings.

## Focused test results

- `Phase4F4DarkSkeletonWiringTests` + `Phase4F4ConfirmAndLegacyRegressionTests`: **21/21 passed** (11 facts + 8 theory cases in the wiring file, 2 facts in the confirm/legacy file)
- `CatalogPreviewGeneratorTests` (Phase 4E.1, updated call sites only) + `CatalogPreviewSnapshotVerifierTests`: **3/3 passed**
- `CatalogPlanConfirmationServiceTests` (unmodified): **25/25 passed**

## RuntimeCatalog test results

`dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~RuntimeCatalog"` → **522 passed, 0 failed, 0 skipped, 522 total**.

## Full-suite results

`dotnet test RunningApp.sln -c Release --no-build` → **565 passed, 0 failed, 0 skipped, 565 total**, duration ~9s.

## Exact test-count reconciliation

544 (Phase 4F.3 baseline) + 21 (Phase 4F.4 new: 19 in `Phase4F4DarkSkeletonWiringTests.cs` + 2 in `Phase4F4ConfirmAndLegacyRegressionTests.cs`) = **565**, matching the observed full-suite result exactly. One pre-existing Phase 4F.3 test (`CatalogPreviewGenerator_ConstructorDependencies_DoNotIncludeOrchestratorType`) was renamed to `CatalogPreviewGenerator_PublicConstructorSurface_DoesNotTakeOrchestratorAsAParameter` with an updated doc comment explaining its narrowed (not weakened) meaning post-wiring — its assertion logic is byte-for-byte unchanged and it still passes, so it is not counted as a new test.

## Files changed

Modified:
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`
- `backend/RunningApp.Api/Program.cs` (comment only — no registration change)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/TestPlanServicesFactory.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPreviewGeneratorTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Phase4F3LiveBoundaryRegressionTests.cs` (one test renamed + doc comment; assertion unchanged)

New:
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F4DarkSkeletonWiringTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F4ConfirmAndLegacyRegressionTests.cs`
- `PHASE4F_4_DARK_INTERNAL_SKELETON_WIRING_INTO_CATALOG_PREVIEW.md`

## Deferred work

Calendar-day assignment, preferred-day/long-run-day-preference consumption, weekly volume/distance/duration/pace/intensity/segment calculation, final schedule payload construction, week/day persistence, catalog confirm enablement, concurrent-confirm protection, v10 publication, `StageKey`→`PhaseKey` rename migration, any structured logging of dark skeleton outcomes.

## Public activation blockers

`TEN_K__4D__INTERMEDIATE v10`'s real catalog JSON `metadata.status` remains `DRAFT`; the PUBLISHED-only eligibility gate (unchanged) continues to block every real, non-dry-run request before the dark skeleton call site is reachable.

## Final classification

**`BACKEND_DARK_MATERIALIZES_INTERNAL_CATALOG_SKELETON_DURING_ELIGIBLE_PREVIEW_WITHOUT_PUBLIC_SCHEDULE_OUTPUT`**
