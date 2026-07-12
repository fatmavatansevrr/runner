# Phase 4E.1 — Catalog Preview Routing and Immutable Resolution Snapshot Foundation

## 1. Files inspected

- `backend/RunningApp.Application/Services/PlanServices.cs` (existing `GeneratePreviewAsync`/`ConfirmPlanAsync` flow, Phase 4B validation block, `SerializerOptions`, `PlanPreview` persistence pattern)
- `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs`, `GeneratePreviewResponse.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogBundleLoader.cs`, `PlanCatalogCandidateSummary.cs`, `PlanCatalogOptions.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Resolvers/*` (all four concrete resolvers, `RuntimeConditionResolutionService`, `RuntimeResolverContext`, `RuntimeConditionResolutionResult`, `ResolverDecisionTrace`)
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs`, `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs`
- `backend/RunningApp.Api/Program.cs` (DI registrations)
- `plan-catalog/catalog/{combinations,templates,layouts,level-modifiers,rule-packs}/*` (confirmed every directly-referenced Phase 4E.1 pilot document's `metadata.status`)
- `plan-catalog/artifacts/audits/activation-readiness-risks.json`
- Every prior Phase 4C–4D.5.1 documentation file and their corresponding `*NotWiredToGenerationTests.cs` / `SafeTemplateSelectionTests.cs` files

## 2. Files changed

New (`backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/`):
- `GenerationRouteDecision.cs` — `GenerationSource`, `GenerationRouteDecision`, `IGenerationRouteDecider`, `PilotGenerationRouteDecider`.
- `CatalogCandidateEligibilityGate.cs` — `ICatalogCandidateEligibilityGate`, `CatalogCandidateEligibilityGate`.
- `NotEvaluatedReasonClassifier.cs` — `NotEvaluatedReasonCategory`, `NotEvaluatedReasonClassifier`.
- `StageEligibilityEvaluator.cs` — `StageEligibilityRequirement`, `StageEligibilityOutcome(Kind)`, `StageEligibilityEvaluator`.
- `CatalogPreviewSnapshot.cs` — `CatalogPreviewSnapshot`, `CatalogPreviewSnapshotBuilder`.
- `CatalogPreviewGenerator.cs` — `ICatalogPreviewGenerator`, `CatalogPreviewGenerator`.

Modified:
- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogCandidateSummary.cs` — added `DependencyStatuses`.
- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogBundleLoader.cs` — populates `DependencyStatuses` for the four directly-loaded dependency documents.
- `backend/RunningApp.Application/Services/PlanServices.cs` — constructor takes `IGenerationRouteDecider`, `ICatalogPreviewGenerator`; `GeneratePreviewAsync` now decides the route once, before generation, and dispatches to the new `GenerateCatalogPreviewAsync` for `GenerationSource.Catalog`; `ConfirmPlanAsync` gained a minimal, explicitly-justified compatibility guard for a catalog-shaped (empty-`Weeks`) preview.
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs` — 7 new typed exceptions (`CatalogCandidateNotPublishedException`, `CatalogDependencyNotRuntimeEligibleException`, `CatalogPilotNotAvailableException`, `RuntimeConditionRequiredInputMissingException`, `RuntimeConditionUnsupportedException`, `RuntimeConditionDependencyUnresolvedException`, `PlanPreviewGenerationFailedException`).
- `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs` — 7 matching mappings.
- `backend/RunningApp.Api/Program.cs` — registers `IGenerationRouteDecider`, `ICatalogCandidateEligibilityGate`, `ICatalogPreviewGenerator`, and the concrete `RuntimeConditionResolutionService` (needed because `ResolveAllResults` is concrete-only).
- `backend/RunningApp.Application/RunningApp.Application.csproj` — added `InternalsVisibleTo` for `RunningApp.IntegrationTests` (test-only visibility for `CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy`; no production behavior change).
- 7 existing `*NotWiredToGenerationTests.cs` files + `SafeTemplateSelectionTests.cs` — updated pilot-combination assertions from `PlanTemplateNotAvailableException` to `CatalogCandidateNotPublishedException` (a deliberate, required behavior change — see §9).

New tests (`backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/`):
- `TestPlanServicesFactory.cs` (shared fully-wired `PlanServices` factory for all tests)
- `PilotGenerationRouteDeciderTests.cs`
- `CatalogCandidateEligibilityGateTests.cs`
- `StageEligibilityEvaluatorTests.cs`
- `NotEvaluatedReasonClassifierTests.cs`
- `CatalogPreviewGeneratorTests.cs`
- `PlanServicesCatalogRoutingBoundaryTests.cs`

## 3. Route-selection rule

`PilotGenerationRouteDecider.Decide` routes to `CATALOG` (reason `PILOT_TEN_K_INTERMEDIATE_4D_MATCH`) iff `GoalType == Race && GoalDistance == TenK && Level == RunningBackground.RunningRegularly && DaysPerWeek == 4`; every other request routes to `LEGACY_SQL` (reason `NOT_PILOT_COMBINATION`). `RunningRegularly` is the established, informal TEN_K/INTERMEDIATE/4D stand-in used by every prior phase's own test scenarios — no formally approved `RunningBackground → INTERMEDIATE` mapping exists (Phase 2 finding, unchanged). Decided exactly once, in `PlanServices.GeneratePreviewAsync`, before any generation-engine or catalog call.

## 4. Candidate and dependency eligibility gate

`CatalogCandidateEligibilityGate.LoadForPublicPreviewAsync` loads the candidate, then requires `CandidateStatus == "PUBLISHED"` (else `CatalogCandidateNotPublishedException`), then requires every entry in `DependencyStatuses` (masterTemplate, layout, levelModifier, rulePack) to be `"PUBLISHED"` (else `CatalogDependencyNotRuntimeEligibleException`). A `PlanCatalogLoadException` (missing/malformed source) becomes `CatalogPilotNotAvailableException`. `LoadForInternalDryRunAsync` bypasses all status checks — used only by tests, never by the public request path.

## 5. NotEvaluated policy

`NotEvaluatedReasonClassifier` maps every reasonCode currently producible by the four resolvers to one of 7 categories (unknown code → `TechnicalOrConfigurationFailure`, fail loud). `CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy` applies: `NotApplicable`/`UpstreamShortCircuit` → continue; `RequiredInputNotProvided` → `RuntimeConditionRequiredInputMissingException`; `Unsupported` → `RuntimeConditionUnsupportedException`; `DependencyUnresolved` → `RuntimeConditionDependencyUnresolvedException`; `OptionalInputNotProvided` (currently unreachable) / `TechnicalOrConfigurationFailure` → `PlanPreviewGenerationFailedException`. `StageEligibilityEvaluator.Evaluate` enforces the fallback rule exactly: `NotEvaluated` always returns `BlockedByNotEvaluated` (never auto-selects a fallback); only an `Evaluated` result with an ineligible value may select `fallbackStageKey`.

## 6. Preview snapshot schema

`CatalogPreviewSnapshot`: `NormalizedInput`, `AsOfDate`, `CandidateKey`/`CandidateVersion`/`CandidateStatusAtGenerationTime`, `ReferencedArtifacts` (8 roles), `GenerationSource`, `RouteReason`, `ResolverResults`, `DecisionTrace` (internal-only), `SelectedStageKeys`/`FallbackStagesUsed` (always empty — stage-to-week scheduling unimplemented), `GeneratedPreviewPlanPayload` (always null), `ContentHash` (SHA-256 of canonical JSON), `CreatedAtUtc`, `ExpiresAtUtc` (30 min, matching the legacy convention). Persisted as `PlanPreview.PreviewPayloadJson` — a deliberate shape divergence from the legacy `GeneratePreviewResponse` JSON, guarded at the confirm boundary (§8).

## 7. AsOfDate policy

Computed once, in `PlanServices.GenerateCatalogPreviewAsync`, as `DateOnly.FromDateTime(DateTime.UtcNow)` (matching the only evidenced backend convention — no per-user timezone handling exists anywhere in this backend). Passed unchanged through `ResolverInputSnapshot.StartDate` and `RuntimeResolverContext.AsOfDate` to every resolver in the pipeline; `RuntimeConditionResolutionService` preserves it verbatim across every threaded context copy. Kept explicitly distinct from `CreatedAtUtc`/`ExpiresAtUtc` on the snapshot.

## 8. Error taxonomy

All 7 exception types from the task's suggested taxonomy were added, each with a distinct HTTP status/error code in `GlobalExceptionHandler`: `CatalogCandidateNotPublishedException` (409), `CatalogDependencyNotRuntimeEligibleException` (409), `CatalogPilotNotAvailableException` (404), `RuntimeConditionRequiredInputMissingException` (400), `RuntimeConditionUnsupportedException` (422), `RuntimeConditionDependencyUnresolvedException` (500), `PlanPreviewGenerationFailedException` (500, also catches a resolver-level `ArgumentException` for invalid input reaching a resolver, e.g. a Race-goal request missing `RaceDate`). None is ever caught and converted into a fallback.

## 9. Confirmation checklist

- **Catalog failure never falls back to SQL**: `CatalogPreviewGenerator.GenerateAsync` has no catch-and-continue path; every exception propagates. `PlanServices.GenerateCatalogPreviewAsync` has no try/catch at all. Proven by `PlanServicesCatalogRoutingBoundaryTests.GeneratePreviewAsync_PilotCombination_NeverInvokesSqlGenerationEngine` (spy engine that fails the test if called).
- **Non-pilot combinations remain on SQL**: unchanged in every existing passing test (326 pre-existing + all new); `PilotGenerationRouteDeciderTests` directly proves every single-criterion mismatch routes to `LEGACY_SQL`.
- **Confirm is not yet catalog-enabled**: `ConfirmPlanAsync` never calls `ICatalogPreviewGenerator`, the eligibility gate, or any resolver. The only change is the minimal empty-`Weeks` guard, which throws `ConflictAppException` rather than crashing on `.First()` — proven not to affect any legacy preview (which always has ≥1 week) by the full existing `ConfirmPlanAsync_SupportedCombination_*` test and the new `PlanServicesCatalogRoutingBoundaryTests.ConfirmPlanAsync_CatalogShapedPreviewWithNoWeeks_*` test. **Phase 4E.2 will make confirm read the stored `CatalogPreviewSnapshot`, validate ownership/expiry/integrity/activation status, and persist the exact preview — never rerunning candidate selection, resolution, or generation.**
- **Every referenced TD's status**: `TD-D3-001`, `TD-WAVE5-001`, `TD-BACKEND-001`, `TD-REGISTRY-001`, `TD-PACESOURCE-001`, `TD-PACESOURCE-002`, `TD-CORE-READINESS-001` all remain `OPEN` — none closed or modified this phase.
- **`TD-PACESOURCE-001` remains OPEN**: confirmed; not touched.
- **No pace-source or goal-feasibility behavior changed**: `PaceSourceResolver`/`GoalFeasibilityResolver` source files were not modified in this phase; only newly *invoked* (for the pilot route) with the same logic as Phase 4D.2/4D.4.
- **No public DTO exposes the resolver trace**: `GeneratePreviewResponse` gained no new fields; `ResolverDecisionTrace`/`RuntimeConditionResolutionResult` never appear on it (existing reflection-based regression guards, still passing).

## 10. Test results

`dotnet test RunningApp.sln -c Debug`: **367 passed, 0 failed** (326 pre-existing + 41 new Phase 4E.1 tests). All 15 required-behavior items are covered:

1–2 (route identification) → `PilotGenerationRouteDeciderTests`
3 (catalog failure never calls SQL) → `PlanServicesCatalogRoutingBoundaryTests.GeneratePreviewAsync_PilotCombination_NeverInvokesSqlGenerationEngine`
4–5 (draft/ineligible-dependency rejection) → `CatalogCandidateEligibilityGateTests`
6–7 (fallback governance) → `StageEligibilityEvaluatorTests`
8–9 (required-input/technical failure) → `NotEvaluatedReasonClassifierTests`, `CatalogPreviewGeneratorTests.GenerateAsync_RaceGoalMissingRaceDate_*`
10–11 (shared AsOfDate, full snapshot) → `CatalogPreviewGeneratorTests.GenerateAsync_ViaDryRunGate_ProducesFullyEvaluatedSnapshot_SharingOneAsOfDate`
12–13 (existing tests unaffected) → full 326-test pre-existing suite, all still passing
14 (no public DTO trace exposure) → existing reflection guards, still passing
15 (confirm unchanged) → `PlanServicesCatalogRoutingBoundaryTests.ConfirmPlanAsync_CatalogShapedPreviewWithNoWeeks_*`

## 11. Final classification

```
BACKEND_HAS_SCOPED_CATALOG_PREVIEW_ROUTING_AND_IMMUTABLE_RESOLUTION_SNAPSHOT_NOT_YET_WIRED_TO_CONFIRM
```
