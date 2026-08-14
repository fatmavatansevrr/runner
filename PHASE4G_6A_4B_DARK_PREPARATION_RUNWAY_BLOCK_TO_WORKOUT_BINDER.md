# Phase 4G.6A.4B — Dark Preparation Runway Block-to-Workout Progression Binder

Scope: `TEN_K__4D__INTERMEDIATE v10` only. Dark-only implementation and test pass. No allocator change, no dated-week generation, no runtime activation.

## 1. Executive Result

**`PRODUCTION_OWNED_DARK_PREPARATION_RUNWAY_BLOCK_TO_WORKOUT_PROGRESSION_BINDER_IMPLEMENTED_WITH_EXACT_PREFIX_SELECTION_AND_NO_RUNTIME_ACTIVATION`**

A production-owned, internal, generic, pure binder engine (`PreparationRunwayBlockWorkoutBindingEngine`), a dark catalog reader for the Phase 4G.6A.4A progression document (`PreparationRunwayBlockProgressionCatalogReader`), and a narrow catalog-aware workout-reference validator (`PreparationRunwayBlockWorkoutReferenceValidator`) were implemented in `RunningApp.Application`, exercised exclusively through direct tests, and never wired into any DI registration or live orchestrator.

## 2. Inherited Catalog Contract

`TEN_K_AEROBIC_STRENGTH_PROGRESSION v1`: Step 1 → `AEROBIC_STRENGTH_CONTROLLED_INTRO v1`; Step 2 → `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1`. No third step. Approved AerobicStrength allocations (unchanged, re-verified, not recalculated): `RunwayWeeks 3-4 → AS=1`, `RunwayWeeks 5-8 → AS=2`.

## 3. Binder Responsibility

Exactly: block allocation count + ordered progression definition → ordered workout references. No calendar dates, phase dates, weekly distance, workout duration, segment prescription, pace, long-run values, `TrainingWeek`, `TrainingDay`, or public schedule DTO is produced anywhere in the new code — confirmed by direct source inspection (no such symbol appears in either new production file).

## 4. Production Ownership

`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayWorkoutBinding/` (new sibling folder to `PreparationRunwayEngine`, matching the established one-folder-per-concern convention). All types `internal`. No DI registration. No test-owned duplicate: the engine's own class declaration does not exist anywhere in `RunningApp.IntegrationTests` (proven by a dedicated test).

## 5. Typed Contracts

`PreparationRunwayBlockProgressionStep(StepNumber, WorkoutId, WorkoutVersion)`, `PreparationRunwayBlockProgressionDefinition<TKey>(ProgressionId, Version, BlockKey, OrderedSteps)`, `PreparationRunwayBlockWorkoutBindingRequest<TKey>(BlockKey, AllocatedWeeks, ProgressionDefinition?)`, `PreparationRunwayWorkoutReference(WorkoutId, WorkoutVersion)`, `PreparationRunwayBlockWorkoutBinding<TKey>(BlockKey, AllocatedWeeks, OrderedWorkoutReferences)`, `PreparationRunwayBlockWorkoutBindingResult<TKey>` (success/failure, failure never carries a partial binding), `PreparationRunwayWorkoutBindingFailureCode` (all 12 requested codes). Generic over `TKey`, matching the Phase 4G.6A.3B allocator's own genericity convention — not coupled to any specific block-key type.

## 6. Progression Loading

**Decision: a narrow, dedicated, dark reader** (`PreparationRunwayBlockProgressionCatalogReader`), reusing the existing `CatalogArtifactFileResolver` (the same subfolder+documentType+key+version scan convention already used by `CatalogWorkoutProgressionLoader`/`CatalogWorkoutDefinitionLoader`) rather than extending `FileSystemCatalogSourceRepository` (a `PlanCatalog.Infrastructure` type that no production backend project references — confirmed by direct investigation: only `RunningApp.IntegrationTests` references `PlanCatalog.*` assemblies, explicitly documented in that csproj as intentional: *"Production projects do not reference PlanCatalog authoring/infrastructure assemblies"*) and rather than a parallel ad hoc architecture. Takes a plain `catalogRoot` string, not `IOptions<PlanCatalogOptions>`, so it cannot be constructor-injected by accident. The new `preparation-runway-progressions` folder is not added to any published-bundle path.

## 7. Canonical Ordering

Steps are always re-sorted by `StepNumber` ascending before selection or contiguity validation — the progression document's own array order is never trusted. Contiguity (`1..N`, no gaps, no missing Step 1) is validated after sorting.

## 8. Prefix-Selection Semantics

`OrderedSteps.OrderBy(StepNumber).Take(AllocatedWeeks)` — exact prefix, verbatim. No nearest-match, phase, weight, objective, alphabetical, or file-order selection exists anywhere in the engine.

## 9. Capacity Failure Behavior

`AllocatedWeeks > OrderedSteps.Count` → `ProgressionCapacityExceeded`, no partial binding returned. The last step is never repeated and progression never cycles back to Step 1 — confirmed by the `ThreeAllocations_FailsCapacity` test (2-step progression, 3 requested, explicit failure, `result.Binding == null`).

## 10. AerobicStrength 0/1/2/3 Proof

Against the real catalog-loaded progression (`RealCatalogReader_LoadsTheAerobicStrengthProgressionDefinition_MatchingTheHandBuiltFixture` + `EndToEnd_RealCatalogReaderPlusBinder_ProducesTheExactApprovedAerobicStrengthSequence`): `0 → []`; `1 → [AEROBIC_STRENGTH_CONTROLLED_INTRO v1]`; `2 → [AEROBIC_STRENGTH_CONTROLLED_INTRO v1, AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1]`; `3 → ProgressionCapacityExceeded`. A dedicated test also proves Step 2 can never be selected alone, and a structural source-scan test confirms no `RunwayWeeks` literal and no reference to the allocator's own types exist anywhere in the binder engine's file.

## 11. Catalog-Reference Validation

`PreparationRunwayBlockWorkoutReferenceValidator` (separate from the pure engine, reuses the existing `ICatalogWorkoutDefinitionLoader`/`CatalogWorkoutDefinitionSummary`, Phase 4F.6B) checks, against the real catalog: existence (`WorkoutReferenceNotFound`), version match (`WorkoutVersionNotFound`), `PREPARATION_RUNWAY` eligibility and non-`RACE_SPECIFIC` (`WorkoutNotRunwayEligible`), `QUALITY` family and absence of forbidden intensity tokens (`THRESHOLD`/`MAXIMAL`/`ALL_OUT`/`SPRINT`/`VO2MAX`/`GOAL_PACE`/`TARGET_TIME`) in any component's `intensityDescriptor` (`WorkoutSemanticMismatch`). Both real AerobicStrength workout references pass validation; `EASY_STANDARD`, `GOAL_PACE_TEN_K`, and `THRESHOLD_TEMPO` are all confirmed rejected using real catalog content (not synthetic fixtures).

## 12. Genericity Proof

`Genericity_ArbitrarySyntheticBlockKeyAndProgression_BehavesIdenticallyToTheAerobicStrengthCase` uses a private, TEN_K-unrelated `SyntheticBlock.Alpha` enum and synthetic workout IDs, proving the 0/1/2/3 behavior is a property of the generic algorithm, not TEN_K-specific code.

## 13. Dark/Public/Persistence Classification

**`PRODUCTION_OWNED`**, **`DARK_UNWIRED`** (zero DI registration or live invocation anywhere outside the new folder — proven by a dedicated reachability scan plus an adversarial-detection counter-proof), **`PUBLICLY_UNREACHABLE`** (no symbol from the new folder appears in `RunningApp.Api` or `RunningApp.Application/DTOs`), **`PERSISTENCE_UNREACHABLE`** (no symbol appears in `RunningApp.Persistence`).

## 14. Unsupported Block Behavior

`ProgressionDefinition = null` with `AllocatedWeeks = 0` → success, empty result (no progression needed). `ProgressionDefinition = null` with `AllocatedWeeks > 0` → typed `MissingProgressionDefinition` failure, no placeholder/fallback workout selected. No mapping was invented for `CONSISTENCY`, `GENERAL_ENDURANCE`, or `PRE_SPECIFIC_TRANSITION` in this phase — only `AEROBIC_STRENGTH`'s real, catalog-backed progression was exercised end-to-end.

## 15. Explicit Non-Implementation Statement

This phase implemented **no** allocator change, **no** allocation-profile or target-matrix change, **no** new workout definition, **no** workout schema/enum change, **no** volume/long-run/pace calculation, **no** calendar assignment, **no** `StartDate`/`RaceDate` handling, **no** `TrainingWeek`/`TrainingDay` generation, **no** public preview/confirm behavior, **no** persistence or migration, **no** 15-20 week activation, **no** General Endurance staged-plan work, **no** 21-52 or 53+ behavior, and **no** intermediate-distance projection. `TD-AEROBIC-STRENGTH-SEMANTICS-001` was neither reopened nor re-closed (no catalog regression was found).

## 16. Exact Follow-up Phase

A future phase may design the CONSISTENCY/GENERAL_ENDURANCE/PRE_SPECIFIC_TRANSITION progression mappings (each currently has no canonical catalog progression — `EASY_STANDARD`/`LONG_RUN_STANDARD` reuse across those three blocks was explicitly identified in Phase 4G.6A.2A as content-supported but never formalized into a versioned progression document the way AerobicStrength now is), and/or make an explicit, separately-recorded architecture decision on whether/how any of these binder components are ever wired into a live or dark orchestrator seam — this phase deliberately makes no such decision.
