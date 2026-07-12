# Phase 4F.3 — Live Catalog Stage-Allocation Resolution and Internal Skeleton Integration

## 1. Files inspected

- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogCandidateSummary.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogBundleLoader.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogCandidateEligibilityGate.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogStageToWeekMaterializer.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/GeneratedCatalogPlanSkeleton.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/GeneratedCatalogPlanSkeletonValidator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogStageToWeekMaterializationExceptions.cs`
- `plan-catalog/catalog/templates/ten-k-master.v6.json`
- `plan-catalog/catalog/run-layouts/run-layout-4d.v2.json`
- `plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v5.json` (read only to confirm `stageKey` is a distinct, finer granularity — never consumed)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PlanCatalogBundleLoaderTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogCandidateEligibilityGateTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationServiceTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayloadSerializationTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Phase4F2LiveBoundaryRegressionTests.cs`

## 2. Repository state before implementation

HEAD = `d4ebbf05d6853655a0272c5a7bd3fdaa3af78ec6` (Phase 4F.2 checkpoint). Full suite: 488 passed / 0 failed / 0 skipped. `TEN_K__4D__INTERMEDIATE v10` = DRAFT. Public catalog activation blocked. `CatalogPreviewGenerator` produces no schedule payload. No stage-to-week materializer called by any live path. Two out-of-scope files (`PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md`, `baseline_tmp/`) were left untouched throughout.

## 3. Exact live candidate-selection boundary

The candidate is selected upstream by `ICatalogCandidateEligibilityGate` (production: `LoadForPublicPreviewAsync`, PUBLISHED-only; test-only dry-run: `LoadForInternalDryRunAsync`). Phase 4F.3's orchestrator never searches for or reselects a candidate — it receives an already-loaded `PlanCatalogCandidateSummary` via `CatalogPlanSkeletonOrchestrationContext.Candidate` and defensively verifies it (Step 1) against caller-supplied expected identity fields.

## 4. Exact artifact-loading boundary

All catalog JSON loading happens exclusively in `PlanCatalogBundleLoader` (unchanged in this phase except the additive `PhaseAllocations` extraction described in §7). No new file reads were introduced anywhere in the Phase 4F.3 orchestration boundary — `CatalogPhaseAllocationResolver`, `CatalogRunLayoutResolver`, `CatalogStageToWeekContextFactory`, and `CatalogPlanSkeletonOrchestrator` are all pure, dependency-free (or resolver-composition-only) classes operating solely on the already-loaded `PlanCatalogCandidateSummary`.

## 5. Master-template authority

`PlanCatalogCandidateSummary.MasterTemplate` (a `PlanCatalogReference(Key, Version)`) is the single authoritative pinned reference. The orchestrator's Step 2 compares it against the caller's `ExpectedMasterTemplate` and throws `CatalogMasterTemplateReferenceMismatchException` on any mismatch.

## 6. Planned-week-count authority

`PlanCatalogCandidateSummary.CoreCycle.DefaultWeeks` (sourced from `coreCycle.defaultWeeks` in `ten-k-master.v6.json`) is the sole authoritative planned-week-count. For the pilot this resolves to `12`. The orchestrator never independently computes a different total; Step 4 compares the phase allocation's own computed total against this value and throws `CatalogPhaseAllocationTotalMismatchException` on mismatch — never normalizing, truncating, or padding.

## 7. Catalog-derived phase allocation

**Loader gap found and fixed (additive):** `PlanCatalogBundleLoader` previously parsed `phases[].phaseKey` into `PhaseKeys` but silently discarded each phase's `preferredWeeks`. Fixed by adding a second pass over the same `phasesEl` JSON array, producing `PlanCatalogPhaseAllocation(string PhaseKey, int PreferredWeeks)` records, exposed as a new required field `PlanCatalogCandidateSummary.PhaseAllocations`. `PhaseKeys` was left unchanged for backward compatibility.

`CatalogPhaseAllocationResolver.Resolve` consumes this field directly and validates: non-empty; no blank phase key; no duplicate phase key; no non-positive `PreferredWeeks`. It deliberately does **not** enforce a closed phase-key vocabulary (keeping it generic across distance families, matching the established genericity pattern). It performs **no** total-vs-authority check itself — that comparison is the orchestrator's Step 4 responsibility, keeping the resolver a pure, honest reporter of whatever the catalog declares (proven by `Resolve_TotalMismatch_IsDetectableByCaller_NotSilentlyNormalized` and `Resolve_NoSilentRedistribution_ShortAllocationStaysShort`).

For the pilot (`TEN_K__4D__INTERMEDIATE v10` / `ten-k-master.v6.json`), this resolves to exactly `FOUNDATION=3, BUILD=4, RACE_SPECIFIC=4, TAPER=1`, total `12` — confirmed by a real, catalog-file-backed test (§24).

## 8. Catalog-derived run layout

`CatalogRunLayoutResolver.Resolve` consumes `PlanCatalogCandidateSummary.SlotRoles` (already loaded verbatim from `run-layout-4d.v2.json`'s `slots[].role` array by the existing loader — no loader change was needed here) and validates: non-empty; no blank role; slot count exactly equals `DaysPerWeek`; no role containing `REST`/`OPTIONAL`/`RECOVERY` (case-insensitive substring match). For the pilot this resolves to exactly `["KEY_SESSION","EASY_SUPPORT","EASY_SUPPORT","LONG_RUN"]`.

## 9. phaseKey vs stageKey

`phaseKey` (`FOUNDATION`/`BUILD`/`RACE_SPECIFIC`/`TAPER` in `ten-k-master.v6.json`'s `phases[]`) is the week-allocation granularity — each with its own `preferredWeeks`. `stageKey` (e.g. `GOAL_PACE_REHEARSAL`, nested in `ten-k-workout-progression.v5.json`'s `phaseProgressions[].stages[]`) is the finer workout-selection granularity with `minimumExposures`/`maximumExposures` and no week-count of its own. Phase 4F.3's new types (`CatalogPhaseAllocationEntry.PhaseKey`/`PhaseWeekCount`) use precise phase terminology throughout and never read any workout-progression file or type (proven in §24's terminology tests). Phase 4F.2's pre-existing `CatalogStageWeekAllocation.StageKey`/`WeekCount` field names were **not** renamed — see §10.

## 10. New resolver/adapter boundaries

- `ICatalogPhaseAllocationResolver` / `CatalogPhaseAllocationResolver` — `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocation.cs`
- `ICatalogRunLayoutResolver` / `CatalogRunLayoutResolver` — `.../CatalogRunLayoutSlots.cs`
- `ICatalogStageToWeekContextFactory` / `CatalogStageToWeekContextFactory` — `.../CatalogPlanSkeletonOrchestrator.cs`
- `ICatalogPlanSkeletonOrchestrator` / `CatalogPlanSkeletonOrchestrator` — same file

All are `internal` (not `public`), consistent with the task's own illustrative signatures. `InternalsVisibleTo("RunningApp.IntegrationTests")` (present since Phase 4E.1.1) makes them directly testable.

## 11. Materialization-context factory

`CatalogStageToWeekContextFactory.Create` is the explicit, documented terminology adapter: it translates `CatalogPhaseAllocation`'s phase-terminology entries 1:1, in order, into Phase 4F.2's pre-existing `CatalogStageWeekAllocation` shape (`StageKey`/`WeekCount`) purely because that is the literal field name Phase 4F.2 already established. The values themselves remain phase-granularity identities; only the C# property name differs. This is documented terminology debt (see the type's own XML doc comment), not a silent semantic change, and no broad rename was performed. It preserves `StartDate`/`AsOfDate` exactly, candidate key/version, the four directly-loaded dependency identities/versions (`masterTemplate`/`layout`/`levelModifier`/`rulePack`, matching `PlanCatalogCandidateSummary.DependencyStatuses`' own established scope), and creates no default values anywhere.

## 12. Skeleton orchestrator

`CatalogPlanSkeletonOrchestrator.Build` has a 5-dependency constructor (`ICatalogPhaseAllocationResolver`, `ICatalogRunLayoutResolver`, `ICatalogStageToWeekContextFactory`, `ICatalogStageToWeekMaterializer`, `IGeneratedCatalogPlanSkeletonValidator`) and no database, HTTP, clock, or route-decider dependency (proven in §24). It never mutates the loaded candidate and persists nothing.

## 13. Validation order

Implemented exactly as required, in `CatalogPlanSkeletonOrchestrator.Build`:
1. Candidate identity integrity (`CatalogSkeletonContextInvalidException`)
2. Master-template reference match (`CatalogMasterTemplateReferenceMismatchException`)
3. Planned-week-count authority read (`candidate.CoreCycle.DefaultWeeks`)
4. Phase-allocation validity incl. total-vs-authority mismatch (`CatalogPhaseAllocationSourceMissingException`/`CatalogPhaseAllocationInvalidException`/`CatalogPhaseAllocationTotalMismatchException`)
5. Run-layout reference match (`CatalogRunLayoutReferenceMismatchException`)
6. Run-layout slot validity (`CatalogRunLayoutSlotInvalidException`)
7. Materialization-context construction
8. Phase 4F.2 materialization, with materializer exceptions wrapped as `CatalogPlanSkeletonOrchestrationFailedException`
9. Skeleton output validation, with a failed validation also wrapped as `CatalogPlanSkeletonOrchestrationFailedException`

Any failure stops immediately and throws — `Build` never returns a partial `CatalogPlanSkeletonOrchestrationResult`.

## 14. Error taxonomy

All 8 required typed exceptions exist in `CatalogPlanSkeletonOrchestrationExceptions.cs`: `CatalogPhaseAllocationSourceMissingException`, `CatalogPhaseAllocationInvalidException`, `CatalogPhaseAllocationTotalMismatchException`, `CatalogMasterTemplateReferenceMismatchException`, `CatalogRunLayoutReferenceMismatchException`, `CatalogRunLayoutSlotInvalidException`, `CatalogSkeletonContextInvalidException`, `CatalogPlanSkeletonOrchestrationFailedException`. None are registered in `GlobalExceptionHandler` — there is no live HTTP caller as of this phase. None are ever caught-and-downgraded into a default skeleton.

## 15. Dependency isolation

`CatalogPlanSkeletonOrchestrator`, its 5 dependencies, and the two new resolvers have zero database, HTTP-context, authenticated-user, or wall-clock access — confirmed both by direct reading of every constructor and by reflection-based regression tests (§24) asserting no constructor parameter type name contains `DbContext` or `HttpContext`.

## 16. Determinism

`Build` is a pure function of its `CatalogPlanSkeletonOrchestrationContext` argument (which itself carries a frozen `AsOfDate`, never the wall clock). Proven via `Build_RealPilotCandidate_IsDeterministic_SameInputProducesStructurallyEquivalentOutput`, which invokes `Build` twice with the identical context and asserts structurally identical week/stage/date output.

## 17. Artifact-version provenance

`CatalogPlanSkeletonOrchestrationResult.DependencyVersions` and the underlying `GeneratedCatalogPlanSkeleton.DependencyVersions`/`Provenance` carry the exact loaded `PlanCatalogReference(Key, Version)` for `masterTemplate`, `layout`, `levelModifier`, and `rulePack` — proven against the real v10 candidate's actual loaded versions in `Build_RealPilotCandidate_ProvenanceReferencesLoadedArtifactVersions`.

## 18. Selected integration option + reason

**Option A** (internal orchestration service only, not yet called by live preview) was chosen. Reason: Option B (dark invocation from `CatalogPreviewGenerator`) would require adding a new constructor dependency to `CatalogPreviewGenerator`, cascading to `Program.cs` DI registration and `TestPlanServicesFactory.cs`. Since `TEN_K__4D__INTERMEDIATE v10` is DRAFT, `CatalogPreviewGenerator.GenerateAsync` already throws `CatalogCandidateNotPublishedException` at the eligibility gate before any orchestration call site could ever execute in production — so Option B would add real dependency-injection risk for zero additional live-behavior coverage. This directly matches the task's own "if dark invocation creates behavioral or dependency risk, use Option A" guidance. `CatalogPreviewGenerator` and `CatalogPlanConfirmationService` were not modified in this phase (confirmed unchanged constructor signatures, §24).

## 19. Proof public preview unchanged

`RealCatalogPreviewGeneration_StillProducesNullGeneratedPreviewPlanPayload` (Phase4F3LiveBoundaryRegressionTests) re-exercises the real, non-dry-run pilot preview path end-to-end and confirms it still throws `CatalogCandidateNotPublishedException` and creates no `PlanPreview` rows — byte-for-byte the same outcome as before this phase.

## 20. Proof final schedule payload null

Covered by the same test above (§19) plus the pre-existing Phase 4F.1/4F.2 regression tests (unchanged, still green) — `GeneratedPreviewPlanPayload` remains null for every real (non-fixture) request path in this phase.

## 21. Proof confirm gated

`CatalogPlanConfirmationService`'s constructor is unchanged (proven via reflection, §24) and its existing null-payload-blocks-confirmation guard (already present since Phase 4F.1, re-verified by the pre-existing `CatalogPlanConfirmationServiceTests`) was not touched.

## 22. Public DTO boundary

`PublicPreviewDTOs_ExposeNoPhaseAllocationOrSkeletonContent` reflects over `GeneratePreviewResponse`, `PreviewWeekDto`, `PreviewDayDto`, and `ConfirmPlanResponse`, asserting no property name contains `Phase`/`Skeleton`/`StructuralRole` and no property type's full name contains `Materialization`. `CatalogPreviewSnapshot_HasNoSkeletonProperty` confirms the snapshot type itself carries no skeleton-named property (it retains only the unchanged Phase 4F.1 `GeneratedPreviewPlanPayload` typed exclusively as the final-contract type).

## 23. Candidate lifecycle status

`TEN_K__4D__INTERMEDIATE v10` remains DRAFT; no publish or activation action was taken; eligibility was never weakened. All real-catalog end-to-end tests use the documented, test-only `LoadForInternalDryRunAsync` entry point (present since Phase 4E.1), which bypasses only the PUBLISHED-only check and is never reachable from any live public request path.

## 24. Test results

All new and pre-existing tests pass:

| Category | File | Count |
|---|---|---|
| Phase-allocation resolution | `CatalogPhaseAllocationResolverTests.cs` | 12/12 |
| Run-layout resolution | `CatalogRunLayoutResolverTests.cs` | 10/10 |
| Context factory | `CatalogStageToWeekContextFactoryTests.cs` | 10/10 |
| End-to-end orchestration | `CatalogPlanSkeletonOrchestratorTests.cs` | 11/11 |
| Terminology | `CatalogPlanSkeletonOrchestrationTerminologyTests.cs` | 4/4 |
| Live-boundary regression | `Phase4F3LiveBoundaryRegressionTests.cs` | 8/8 |
| Loader extension | `PlanCatalogBundleLoaderTests.cs` (1 new test) | 1/1 |

`RuntimeCatalog`-filtered suite: 501/501 passed. Full solution suite (Release, `--no-build`): **544/544 passed, 0 failed, 0 skipped**. `dotnet build -c Release`: 0 errors, 0 warnings.

## 25. Exact test-count reconciliation

Baseline (Phase 4F.2 checkpoint): 488 passed. New tests added this phase: 12 + 10 + 10 + 11 + 4 + 8 + 1 = **56**. 488 + 56 = **544**, matching the observed full-suite result exactly.

## 26. Deferred terminology cleanup

Phase 4F.2's `CatalogStageWeekAllocation.StageKey`/`WeekCount` and `GeneratedCatalogWeekSkeleton.StageKey` field names remain as originally named — they describe phase-granularity data using stage-terminology field names, a pre-existing naming choice from Phase 4F.2 not revisited here. A future phase could rename these to `PhaseKey`/`PhaseWeekCount` for full terminology consistency; until then, `CatalogStageToWeekContextFactory.Create`'s XML doc comment documents this explicitly as known terminology debt, not an unnoticed inconsistency.

## 27. Remaining work / Final classification

Not started, and intentionally out of scope for this phase: calendar-day assignment, preferred-day/long-run-day-preference consumption, weekly volume calculation, workout selection, distance/duration/pace/intensity/segment calculation, final schedule payload construction, week/day persistence, catalog confirm enablement, concurrent-confirm protection, v10 publication, and wiring the orchestrator into any live request path (Option B or later explicit adoption).

**Final classification: `BACKEND_CAN_RESOLVE_LIVE_CATALOG_PHASE_ALLOCATION_AND_BUILD_INTERNAL_SKELETON_NOT_YET_WIRED_TO_PREVIEW`**
