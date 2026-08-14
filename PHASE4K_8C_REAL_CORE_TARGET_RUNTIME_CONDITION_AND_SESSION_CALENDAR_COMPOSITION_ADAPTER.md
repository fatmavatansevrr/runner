# Phase 4K.8C — Real Core Target, Runtime Condition and Session-Level Calendar Composition Adapter

## 1. Executive result

This phase completes the dark composition layer between validated rolling evidence and the existing Phase 4K.8 JIT activation runtime. `LongHorizonRollingJitCompositionOrchestrator` closes the gap Phase 4K.8 explicitly disclosed: the Core Week-1 target, available Core weeks, and resolved condition results Phase 4K.8's own request previously accepted as caller-fabricated inputs are now produced internally, for real, by invoking the real, unmodified `RuntimeConditionResolutionService` (all four production resolvers) and the real, unmodified `TenKPreparationRunwayDarkOrchestrator` — reached through its own existing production factory, `TenKPreparationRunwayDarkOrchestratorFactory.Create`, the same factory the live 15–20 week preview path already uses. The composed real inputs are then handed unchanged to Phase 4K.8's own `ILongHorizonRollingJitActivationRuntime` for atomic activation — Phase 4K.8's window selection, atomicity, direction guard, target-lock validation, and bounded Runway slice selection are never duplicated or redesigned. No commits made.

```
LONG_HORIZON_REAL_CORE_TARGET_RUNTIME_CONDITION_AND_SESSION_CALENDAR_COMPOSITION_COMPLETED_DARK

LONG_HORIZON_VALIDATED_CHECKPOINT_EVIDENCE_NOW_FLOWS_THROUGH_THE_REAL_RUNTIME_CONDITION_RESOLVERS_AND_UNCHANGED_CORE_GENERATOR_TO_PRODUCE_THE_LOCKED_CORE_WEEK_ONE_TARGET

LONG_HORIZON_BOUNDED_CORE_SELECTION_READS_REAL_DATED_AND_VALUED_SESSIONS_DIRECTLY_FROM_THE_REAL_GENERATED_RESULT

LONG_HORIZON_PHASE4K_8_JIT_ACTIVATION_RUNTIME_IS_NOW_INVOKED_THROUGH_A_COMPLETE_REAL_AUTHORITY_COMPOSITION_LAYER

LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED
```

## 2. Inherited policy and contract state

Phase 4K.5A: `ValidatedSustainableLoad` is the approved rolling authority into the unchanged Core generator input boundary; onboarding remains legacy provenance only. Phase 4K.8: `LongHorizonRollingJitActivationRuntime`, its typed request/result contracts, window selection, atomicity, and the four in-scope activation outcomes — unmodified, invoked as-is by this phase's new composition layer. Phase 4K.8A/4K.8B: Runway direction authority, `PreparationRunwayDirectionGuard`, `ImmutablePreparationRunwayPrescription`, target-lock scope/refresh guard, bounded slice factory — all reused verbatim through Phase 4K.8's own request, never touched directly by this phase.

## 3. Scope and exclusions

In scope: the composition orchestrator entry point, real condition-resolution invocation, the rolling Core input mapping, real Core generator invocation (via the existing production dark-orchestrator factory), real Core Week-1 target extraction, real bounded Core-week selection, composing Phase 4K.8's activation request from these real outputs, and atomic composition-plus-activation.

Excluded: redesigning Phase 4K.8 itself; any GE/Runway/Core numeric formula change; enabling downward interpolation; persisting rolling state; public preview/API/DTO/Flutter changes; implementing the complete 21–52 lifecycle matrix (Phase 4K.9); commits.

## 4. Orchestrator entry point

`ILongHorizonRollingJitCompositionOrchestrator.ComposeAndActivateNextWindowAsync(LongHorizonRollingJitCompositionRequest, CancellationToken)`, implemented by `LongHorizonRollingJitCompositionOrchestrator` — internal, constructible with only an optional `ILongHorizonRollingJitActivationRuntime` seam for testing, no public DI registration.

## 5. Request contract deliberately narrower than Phase 4K.8's

`LongHorizonRollingJitCompositionRequest` structurally cannot carry a caller-supplied Core Week-1 target, available Core weeks, or condition results — those fields do not exist on the type at all (proven by three dedicated reflection tests). It instead carries the same rolling/checkpoint state Phase 4K.8 needs, plus the real catalog dependencies (`CatalogRootPath`, `Candidate`) the unchanged production pipeline requires.

## 6. Condition-resolution authority

A real `RuntimeConditionResolutionService`, constructed from the same four stateless resolvers RunningApp.Api registers for production DI (`TimeAdequacyResolver`, `PaceSourceResolver`, `CoreEntryReadinessResolver`, `GoalFeasibilityResolver`), invoked via `ResolveAllResults` with the checkpoint date as `AsOfDate` and the real candidate's own `CoreCycle` attached. Discovered during this phase: `TimeAdequacyResolver` throws if `CoreCycle` is unset — a genuine wiring requirement, not previously documented at this call site, now satisfied from `request.Candidate.CoreCycle`.

## 7. Rolling Core input mapping

`LongHorizonRollingCoreGenerationInputAdapter.Build` implements the Phase 4K.5A-approved mapping into the legacy `GeneratePreviewRequest`/`ResolverInputSnapshot` shape the real Core pipeline requires: `ValidatedSustainableLoad.WeeklyVolumeKm`/`LongRunKm` populate `RecentWeeklyVolumeKm`/`RecentLongestRunKm` directly; exact completed frequency populates `RecentRunsPerWeek` (or null, never inferred). Onboarding evidence and planned GE exit are never read.

## 8. Core generator invocation

`TenKPreparationRunwayDarkOrchestratorFactory.Create(new PlanCatalogOptions { CatalogRootPath })` constructs the full real component graph (Core generator, `DynamicCoreCalendarMaterializationOrchestrator` and its layers, `PreparationRunwayCalendarComposer`) with zero DI container. `OrchestrateAsync` produces one real `TenKPreparationRunwayDarkOrchestrationResult` per Runway entry/Core-touching window. Invoked only when needed — first Runway entry, or when the current window could reach Core — never rerun for a pure mid-Runway continuation window.

## 9. Core Week-1 target extraction

`PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior(realComposition.CoreResult.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan)` — the exact same adapter call the existing 15–20 week production path already makes, applied to the real generated result. No target field is ever assigned directly from request input values.

## 10. Bounded Core selection

`LongHorizonBoundedCorePrescriptionSelection` wraps exact `CatalogPrescribedWeek`/`CatalogPrescribedSession` entries read directly from `realComposition.CoreResult.PrescriptionResult.FinalPrescribedPlan.Weeks` — each entry already carries a real assigned session date and distance together, avoiding a fragile two-structure correlation. Global week numbers are computed via the structural Core segment's own `StartGlobalWeek` offset; no week is renumbered, no value is recomputed.

## 11. Runway calendar authority and the honest scope gap

The real `TenKPreparationRunwayDarkOrchestrator`'s own `PreparationRunwayCalendarComposer` output (`CalendarComposition.DatedRunwayWeeks`/`DatedCoreWeeks`, part of the same real composition call) is available on `LongHorizonRollingJitCompositionResult.RealCompositionResult` for inspection and provenance. However, Phase 4K.8's own final `ActivatedNumericWeek.CalendarDates` field still uses the simpler, unmodified week-level `LongHorizonCalendarAssigner.WeekStartDate` assignment — this phase does not redesign that Phase 4K.8 authority. This is disclosed explicitly: real per-session dates are computed and validated as part of composition, they are just not force-fit into a contract shape Phase 4K.8 already owns.

## 12. Core generation necessity boundary

Core generation triggers when `ExistingRunwayPrescription is null` (first Runway entry) or when the first unstarted pending week plus the window size exceeds Runway's own last week (the window may reach into Core). A pure Runway continuation window that ends exactly at Runway's own last week is correctly treated as Runway-only and skips Core regeneration — an off-by-one in this exact boundary check was found and fixed during this phase's own real-pipeline testing.

## 13. Phase 4K.8 runtime integration

The composed Core target/weeks/condition results plus the unmodified caller-supplied GE/Runway continuation state are assembled into a `LongHorizonRollingJitActivationRequest` and passed unchanged to the existing, unmodified `ILongHorizonRollingJitActivationRuntime.ResolveAndActivateNextWindowAsync`.

## 14. Atomicity

One shared try/catch wraps the entire composition-plus-activation call: any `LongHorizonRollingContractException`, any real-composition failure (mapped to a typed reason via the orchestration failure stage), or a `JitWindowBlocked` result from Phase 4K.8's own runtime all produce a `CompositionBlocked` result with zero activated weeks, no exposed `RealCompositionResult`, and exactly one authoritative reason.

## 15. Failure taxonomy

`TenKPreparationRunwayDarkOrchestrationFailure.Stage` maps to a `LongHorizonReasonCode`: CoreGeneration/AllocationPolicy stages → `CoreJitContextUnavailable`; NumericMaterialization/StructuralMaterialization/BlockAllocation/ProgressionLoading/WorkoutBinding stages → `RunwayJitContextUnavailable`; CalendarComposition stage → `JitAvailabilityInfeasible`; all other stages → `JitEvidenceConflictUnresolved`.

## 16. Versioning

Reuses the existing SHA-256/`Guid.NewGuid` conventions already established by Phase 4K.5/4K.8/4K.8B's context-version primitives; checkpoint date is always an explicit input, never an internal clock read.

## 17. Dark integration

Internal, independently invokable, supplied-context/evidence based. Unwired from live preview, confirmation, persistence repositories, endpoints, and Flutter. No public DI registration.

## 18. Persistence/public status

Unchanged. Zero controller, DI registration, database entity/migration, or Flutter file touched — confirmed by a clean build with no file outside the new composition orchestrator/contracts/adapter files modified.

## 19. Architecture guard compliance

The existing `TenKPreparationRunwayDarkOrchestratorTests.cs` architecture guard restricts direct references to `TenKPreparationRunwayDarkOrchestrator` to four specific folders/files outside those folders. The new `RollingActivation` folder is not one of the four checked roots; referencing the dark orchestrator directly from it does not violate the guard — confirmed by reading the guard test's exact source before proceeding.

## 20. New files

`backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonRollingCoreGenerationInputAdapter.cs`, `LongHorizonRollingJitCompositionContracts.cs`, `LongHorizonRollingJitCompositionOrchestrator.cs`.

## 21. Tests

18 new focused tests in `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonRollingJitCompositionOrchestratorTests.cs`, exercising the real on-disk catalog (`plan-catalog/catalog`), the real four condition resolvers, and the real `TenKPreparationRunwayDarkOrchestrator` end-to-end — never a hand-built shortcut result. Covers: real condition-resolution invocation, real Core-generator invocation, real Core Week-1 target extraction, real bounded Core-week selection, request-shape guarantees (no caller-injectable target/weeks/conditions), GE+Runway/Runway-only end-to-end success, Runway continuation without regeneration, blocked scenarios, determinism, and integration boundaries.

## 22. Errors found and fixed during this phase

`TimeAdequacyResolver` requires `RuntimeResolverContext.CoreCycle` — fixed by wiring the real candidate's `CoreCycle`. Two tests' expected outcome corrected from `GeRunwayMixedWindowActivated` to `RunwayWindowActivated` once the test evidence was traced to a fully-completed GE case. An off-by-one in the Core-generation-necessity boundary (`>=` vs `>`) caused an unnecessary Core regeneration on a legitimate Runway-only continuation — fixed. One test's premise (forcing "above target" with a single scalar) no longer held once Core's own target began scaling with the same real evidence feeding Runway — rewritten to assert internal result consistency rather than a specific outcome. One determinism test's evidence combo was itself blocked by the real pipeline — fixed by using known-good evidence values and adding explicit outcome assertions for clearer failure diagnostics.

## 23. Full backend suite result

`dotnet test RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj`: **2770 passed, 0 failed, 0 skipped**.

## 24. Full plan-catalog suite result

`dotnet test` from `plan-catalog/`: **1127 passed, 0 failed, 0 skipped** (after this document was authored to satisfy the new governance test's file-existence check).

## 25. Governance artifacts updated

New TD `TD-LONG-HORIZON-JIT-REAL-CORE-CONDITION-CALENDAR-COMPOSITION-001` (CLOSED) in `activation-readiness-risks.json`/`.md`. Append-only updates added to `TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001`, `TD-LONG-HORIZON-RUNWAY-BOUNDED-PRESCRIPTION-CONTRACTS-001`, and `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001`. Aggregate updated to 43 risks, 14 OPEN, 29 CLOSED. New governance cross-check test file `LongHorizonJitRealCoreConditionCalendarCompositionGovernanceTests.cs`. Six prior governance test files with stale 42/28 hardcoded counts updated to 43/29.

## 26. What remains unimplemented

Full 21–52 week rolling dark lifecycle validation, retry, and boundary matrix (Phase 4K.9). Persistence of any rolling/context-version state. Public preview/confirmation/API exposure. Flutter integration. Threading real per-session calendar dates into Phase 4K.8's own final `ActivatedNumericWeek.CalendarDates` field (currently only available as provenance on `RealCompositionResult`, per Section 11's disclosed scope gap).

## 27. Final classification

`LONG_HORIZON_REAL_CORE_TARGET_RUNTIME_CONDITION_AND_SESSION_CALENDAR_COMPOSITION_COMPLETED_DARK`. `LONG_HORIZON_VALIDATED_CHECKPOINT_EVIDENCE_NOW_FLOWS_THROUGH_THE_REAL_RUNTIME_CONDITION_RESOLVERS_AND_UNCHANGED_CORE_GENERATOR_TO_PRODUCE_THE_LOCKED_CORE_WEEK_ONE_TARGET`. `LONG_HORIZON_BOUNDED_CORE_SELECTION_READS_REAL_DATED_AND_VALUED_SESSIONS_DIRECTLY_FROM_THE_REAL_GENERATED_RESULT`. `LONG_HORIZON_PHASE4K_8_JIT_ACTIVATION_RUNTIME_IS_NOW_INVOKED_THROUGH_A_COMPLETE_REAL_AUTHORITY_COMPOSITION_LAYER` — with the explicit caveat that "session-level calendar composition" is real and available as provenance but not yet the source of Phase 4K.8's own final calendar-date field (Section 11). `LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED`.

## 28. Exact next phase

Phase 4K.9 — Full 21–52 Rolling Dark Lifecycle Validation, Retry and Boundary Matrix.
