# Phase 10K-FREQ.6D.4D.2 (Split B) — Dual-KEY Exact Prescription Profile Resolution, Projection Dependency & Bundle Execution-Prescription Materialization

**Implementation phase executing Split B only of the FREQ.6D.4D-approved D1 architecture. No product decision, no dosage change, no lane/stage re-derivation, no progression-stage algorithm change, no WorkoutDefinition change, no profile content change, no catalog lifecycle change, no RunningApp consumer change, no TrainingDay persistence change, no database migration, no Adaptation policy change, no public 5D activation.**

## 1. Preflight

`PHASE_LEDGER.md` row 73: `FREQ.6D.4D.1`, `IMPLEMENTATION`, `DONE`, `FREQ6D4D_SPLIT_A_IMPLEMENTED_ADAPTATION_ENGINEERING_GAP_REMAINS`, confirmed. Row 72 (`FREQ.6D.4D`, architecture) confirmed `VERIFIED`. Commits `35ab52f`, `bb715ed`, `e0a513b` confirmed reachable from HEAD. Starting HEAD `e0a513b49802118363f70341605c893c1ee88e0b`, branch `main`, `git rev-list --left-right --count origin/main...HEAD` → `0 15`. `git status --short` → only ` m baseline_tmp` and two pre-existing, unrelated `plan-catalog/artifacts/audits/*` modifications (untouched by this phase). `git diff --check` → clean. `FREQ.6D.4D.2` confirmed not already a ledger row.

The complete real `PHASE_10K_FREQ_6D_4D_DUAL_KEY_STAGE_PROFILE_PRODUCTION_INTEGRATION_ARCHITECTURE.md` (§10-§16, §29-§34, §41) was re-read in full, extracting the frozen design this phase implements exactly: stage-authored `PrescriptionProfileCandidateKeys[]` (§10, sibling to `WorkoutCandidateReferences`, same exact-pin discipline); no `DoseCategory` runtime search, only a publish-time `LaneOrdinal↔DoseCategory` invariant reusing `PrescriptionProfileLaneDoseValidator` (§11); `ExactPrescriptionProjectionDependency`/`CatalogBundleAssembler` reused verbatim, only their caller (new, narrow glue) needed to exist (§12, §38); dependency cardinality = distinct-profile-set, deduplicated by exact identity, deterministically ordered (§13); bundle = unique execution library indexed by profile identity, not per-slot (§14); Split B's own boundary per §41: "Exact profile dependency materialization + bundle — depends on A's `LaneOrdinal`/stage resolution existing," explicitly **excluding** RunningApp session lineage (Split C) and persistence/Adaptation (Split D) and the real `RUN_LAYOUT_5D`/combination catalog authoring (Split E).

The real `PHASE_10K_FREQ_6D_4D_1_SLOT_LANE_STAGE_BINDING_IMPLEMENTATION.md` was re-read in full. Confirmed Split B's own §27 Split-B input contract: every `StageControlled` `BoundCatalogSession` already deterministically carries `(WeekNumber, StructuralRole, structural ordinal, LaneOrdinal, ProgressionStageKey)` — this phase consumes `LaneOrdinal`/`ProgressionStageKey` exactly as materialized, never re-deriving them, never re-running `ProgressionStageAllocator`.

## 2. Parent Split-A contract

Confirmed honored throughout: `CatalogWorkoutBinder`'s existing `laneOrdinal`/`stageDefinition` resolution (Split A) is read, not recomputed; the new profile-candidate resolution is inserted as an **additive** step immediately after the existing workout-definition resolution, inside the same `StageControlled` branch, using the same `laneOrdinal`/`stageDefinition` locals Split A already produced.

## 3. Files inspected

`PlanCatalog.Core/Models/WorkoutProgressionStageDefinition.cs`, `WorkoutProgressionDefinition.cs`, `PhaseWorkoutProgressionDefinition.cs` (confirmed: PlanCatalog.Core had **no** `Lanes`/lane concept at all before this phase — Split A's Lane model lived only in the RunningApp-side mirror, per its own explicitly scoped diff); `PlanCatalog.Core/Validation/WorkoutProgressionValidator.cs` (full read, both before/after edit); `PlanCatalog.Core/Catalog/WorkoutClosureResolver.cs`; `PlanCatalog.Core/Validation/PrescriptionProfileLaneDoseValidator.cs` (confirmed real, already-implemented, exactly the reusable lane↔dose authority the architecture calls for — reused verbatim, not duplicated); `PlanCatalog.Core/Models/WorkoutPrescriptionProfile.cs`; `PlanCatalog.Infrastructure/Projection/ExactPrescriptionProjectionDependency.cs`, `WorkoutPrescriptionExecutionProjector.cs`; `PlanCatalog.Infrastructure/Publishing/CatalogBundleAssembler.cs` (full read — confirmed the exact-dependency overload from `FREQ.6D.3C` already exists and needed no signature change); `CatalogSourceSnapshot.cs` (`FindPrescriptionProfile` confirmed real); `RunningApp.Application` `CatalogWorkoutProgressionDefinition.cs`, `CatalogWorkoutProgressionLoader.cs`, `CatalogWorkoutBinder.cs`, `BoundCatalogPlanContracts.cs`, `CatalogWorkoutBindingExceptions.cs` (all full reads, post-Split-A state); `plan-catalog/catalog/workout-progressions/*.json` (confirmed `WORKOUT_PROGRESSION` real document shape, `workoutCandidates` with `documentType` per entry — the exact sibling shape `prescriptionProfileCandidates` now mirrors); `CombinationFixture.cs`, `CatalogSnapshotBuilder.cs`, `WorkoutPrescriptionProfileValidatorTests.cs`, `WorkoutProgressionValidatorTests.cs`, `CatalogBundleAssemblerRetirementTests.cs` (existing test-fixture conventions, reused).

## 4. Files changed

**PlanCatalog.Core** (4 files):
- `Models/WorkoutProgressionStageDefinition.cs` — added `PrescriptionProfileCandidates` (`IReadOnlyList<VersionedCatalogReference>?`), sibling to `WorkoutCandidates`.
- `Models/PhaseWorkoutProgressionDefinition.cs` — added `Lanes`/`EffectiveLanes`/new `WorkoutProgressionLaneDefinition` record, mirroring the RunningApp Split-A shape (this was a real, necessary catch-up gap: Split A never touched PlanCatalog.Core, so it had no lane concept at all before this phase). `EffectiveLanes` is `[JsonIgnore]` — load-bearing (§4a below).
- `Validation/WorkoutProgressionValidator.cs` — restructured to validate per-`EffectiveLanes` (fallback/stage-key scoping now per-lane, matching the RunningApp binder's own per-lane lookup exactly); added `ValidateLaneOrdinals` (duplicate `LaneOrdinal` detection) and `ValidatePrescriptionProfileCandidates` (existence, duplicate, ambiguity, and `PrescriptionProfileLaneDoseValidator` reuse for the `LaneOrdinal↔DoseCategory` invariant).
- `Catalog/PrescriptionProfileClosureResolver.cs` (new) — mirrors `WorkoutClosureResolver`'s exact union-dedupe-sort pattern, scoped to `PrescriptionProfileCandidates`.
- `Catalog/WorkoutClosureResolver.cs` — **necessary correction, not scope creep**: both `IsExactShape` and `ComputeExactClosureRefs` read `.Stages` directly, which silently ignores any lane-authored content. Fixed to `.EffectiveLanes.SelectMany(l => l.Stages)` — degenerates to the original behavior for every existing single-lane document (confirmed via the full, unchanged 1501/1501 PlanCatalog regression, §24).

**PlanCatalog.Infrastructure** (2 files):
- `Projection/PrescriptionProjectionDependencyResolver.cs` (new) — the exact "narrow, catalog-authoring-time glue" the architecture disclosed as unnamed (§34): `ResolveForProgression(WorkoutProgressionDefinition) → IReadOnlyList<ExactPrescriptionProjectionDependency>`, built entirely from `PrescriptionProfileClosureResolver`'s already-deterministic, already-deduplicated output.
- `Publishing/CatalogBundleAssembler.cs` — same necessary lane-awareness fix as `WorkoutClosureResolver`, applied to `progressionIsExact` and the legacy `candidateKeys` branch (both previously read `.Stages` directly). The exact-dependency overload itself (`Assemble(..., executionDependencies)`, `FREQ.6D.3C`) required **zero** signature or projection-logic change, confirmed by direct diff review — matching the architecture's own `NO_CHANGE` prediction for the assembler proper.

**RunningApp.Application** (5 files):
- `Schedule/Progression/CatalogWorkoutProgressionDefinition.cs` — added `PrescriptionProfileCandidateKeys` to `CatalogWorkoutProgressionStage` (sibling to `WorkoutCandidateReferences`).
- `Schedule/Progression/CatalogWorkoutProgressionLoader.cs` — optional `"prescriptionProfileCandidates"` JSON array parsing, additive.
- `Schedule/Binding/BoundCatalogPlanContracts.cs` — added `PrescriptionProfileKey`/`PrescriptionProfileVersion` (both nullable) to `BoundCatalogSession`.
- `Schedule/Binding/CatalogWorkoutBinder.cs` — new resolution step inside the existing `StageControlled` branch, reading `stageDefinition.PrescriptionProfileCandidateKeys` (0 → Legacy, null fields; 1 → ProfileBacked, exact key/version copied; >1 → typed ambiguous exception). `FixedDefault` branch explicitly sets both new fields `null`.
- `Schedule/Binding/CatalogWorkoutBindingExceptions.cs` — added `CatalogWorkoutBindingAmbiguousPrescriptionProfileCandidateException`.

**Tests** (2 files modified, 4 new):
- `Freq6D4DSplitADualKeyLaneStageBindingTests.cs` — one test updated (§4a below, expected consequence).
- `Freq6D4DSplitBExactPrescriptionProfileBindingTests.cs` (new, RunningApp, 5 tests).
- `Catalog/PrescriptionProfileClosureResolverTests.cs` (new, PlanCatalog, 4 tests).
- `Validation/PrescriptionProfileLaneCandidateValidationTests.cs` (new, PlanCatalog, 7 tests).
- `Publishing/PrescriptionProjectionDependencyBundleAssemblyTests.cs` (new, PlanCatalog, 5 tests).

Plus the mechanical rebuild of tracked `bin/`/`obj/` build artifacts (established convention).

**No** `WorkoutPrescriptionProfile` source, `WorkoutDefinition`, catalog-lifecycle, `WorkoutPrescriptionExecutionProjector`, `ExecutionPrescriptionIndex`/`PublishedTemplateBundleJsonReader`/`CatalogSessionPrescriptionSource`, `TrainingDay`/database, `NextWindowLoadDecisionPolicy`, or public-routing file was touched — confirmed by direct review of the staged diff.

### 4a. Two disclosed, necessary corrections found while wiring this split

1. **`EffectiveLanes` needed `[JsonIgnore]`.** Adding it to `PhaseWorkoutProgressionDefinition` without `[JsonIgnore]` initially broke 3 real, pre-existing hash-stability tests (`CandidateArtifactTests.ExistingPublishedArtifacts_RemainByteForByteAndHashUnchanged`, `DependencyVersionCascadeTests.LegacyWorkoutProgressionAndLevelModifierV1_RemainByteUnchanged`, and the release-preview cross-hash guard) — a computed property is serialized by `System.Text.Json` by default, so every existing published document's content hash would have silently changed the moment this property was added. Caught via the full regression run (§24), fixed immediately, re-verified green. This is exactly the kind of historical-stability regression this engagement's own conventions exist to catch.
2. **`WorkoutClosureResolver`/`CatalogBundleAssembler` were not lane-aware.** Both read `.Stages` directly; a lane-authored `WorkoutProgressionDefinition` (which places its real content under `.Lanes`, leaving `.Stages` empty per the established degenerate-default convention) would silently produce an empty workout closure. Fixed to `.EffectiveLanes.SelectMany(l => l.Stages)` in both call sites — confirmed inert for every existing single-lane document (full regression unchanged).
3. **One Split-A test updated as an expected consequence** (`BoundCatalogSession_CarriesNoPrescriptionProfileField_ProfileSelectionRemainsSplitBScope`): this test proved, at Split A time, that `BoundCatalogSession` carried no profile field — a true statement about Split A's own scope. Split B's entire purpose is to close exactly that boundary. Renamed and flipped to assert the new, correct invariant (`RunningApp.IntegrationTests/.../Freq6D4DSplitADualKeyLaneStageBindingTests.cs:548-563`), following this engagement's established practice (`FREQ.6D.4C.2`) of updating an old boundary-proof test once the boundary it proved is deliberately, subsequently closed — not treating it as a regression.

## 5. Stage profile-candidate authoring

Additive `PrescriptionProfileCandidates` (PlanCatalog) / `PrescriptionProfileCandidateKeys` (RunningApp mirror), both `IReadOnlyList<VersionedCatalogReference>`/`IReadOnlyList<PlanCatalogReference>`, exact `(key, version)` pairs, JSON shape `"prescriptionProfileCandidates": [{ "documentType": "WORKOUT_PRESCRIPTION_PROFILE", "key": "...", "version": N }]` — the exact sibling shape to `workoutCandidates`, per architecture §10's own instruction.

## 6. Exact profile-selection rule

Cardinality-only, no search: 0 candidates → Legacy (both `PrescriptionProfileKey`/`Version` stay `null`, no error — the additive/degenerate-default legacy boundary); exactly 1 → ProfileBacked, bound verbatim; >1 → `CatalogWorkoutBindingAmbiguousPrescriptionProfileCandidateException` (RunningApp bind-time) and `WP_PRESCRIPTION_PROFILE_CANDIDATE_AMBIGUOUS` (PlanCatalog publish-time, defense-in-depth, same fail-fast-preferred pattern as every other static check in this validator).

## 7. Lane-dose resolution

Reused `PrescriptionProfileLaneDoseValidator.Validate(laneOrdinal, profile)` **verbatim** — zero second copy of the `LaneOrdinal 0 → Primary / LaneOrdinal 1 → SecondaryControlled` mapping, per the phase's own explicit instruction (§10 of the implementation prompt). This is a publish-time-only check (`WorkoutProgressionValidator`) — the RunningApp binder never loads a `WorkoutPrescriptionProfile` document or inspects `DoseCategory` at bind time; it only carries the already-validated exact reference through.

## 8. Phase/stage compatibility

Unchanged, reused: `CatalogWorkoutBinder`'s existing `ValidateInClosureAndPhase` continues to gate the `WorkoutDefinition` a profile ultimately points to (via the profile's own `WorkoutDefinitionRef`, per architecture §11 — "the profile already carries its own exact `WorkoutDefinitionRef`... the 5D binding layer must not independently resolve a `WorkoutDefinition` reference a second time"). No new phase-compatibility mechanism was added; none was needed.

## 9. Exact-version authority

No fallback path exists anywhere in the diff (confirmed by review): missing exact profile version is a `WP_PRESCRIPTION_PROFILE_CANDIDATE_MISSING` publish-time validation failure (`PrescriptionProfileLaneCandidateValidationTests.MissingExactProfileVersion_Fails`), never a "nearest version" substitution.

## 10. Bound profile lineage

`BoundCatalogSession.PrescriptionProfileKey`/`PrescriptionProfileVersion` (both nullable `string?`/`int?`), populated together or not at all — confirmed by direct review of every construction site in `CatalogWorkoutBinder.cs`. Sufficient for Split C to consume without profile search, per its own input contract (§13 below).

## 11. Projection dependency materialization

`PrescriptionProjectionDependencyResolver.ResolveForProgression(WorkoutProgressionDefinition)` — the disclosed, unnamed glue from architecture §34, now real: computes the progression's exact, deduplicated profile closure (`PrescriptionProfileClosureResolver`) and wraps each into `ExactPrescriptionProjectionDependency { Profile = ref }`, ready for `CatalogBundleAssembler.Assemble(..., executionDependencies)`.

## 12. Dependency cardinality/deduplication

Deduplicated by exact `(Key, Version)` identity via `Distinct()` on `VersionedCatalogReference` (a record, structural equality) — `PrescriptionProfileClosureResolverTests.SharedProfileAcrossLanes_Deduplicated` proves two stages in different phases/lanes referencing the identical profile collapse to one dependency. Deterministic ordering: `OrderBy(Key, Ordinal).ThenBy(Version)` — `PrescriptionProfileClosureResolverTests.DistinctRefsAcrossLanes_AllIncluded_DeterministicKeyThenVersionOrder` proves the order is content-derived, not declaration/lane/dictionary order.

## 13. Bundle assembly

`PrescriptionProjectionDependencyBundleAssemblyTests.DualLaneProfileBackedCombination_ProducesNonNullExecutionPrescriptions_BothLanesReachable` — a real, dual-lane, profile-backed `WorkoutProgressionDefinition` (substituted into the existing 4D `CombinationFixture` shell, since no real `RUN_LAYOUT_5D`/combination exists yet — Split E's own disclosed job, per architecture §41/§24) flows through `PrescriptionProjectionDependencyResolver` → `CatalogBundleAssembler.Assemble(..., executionDependencies)` (the unmodified `FREQ.6D.3C` overload) and produces a `PublishedTemplateBundle` with exactly 2 `ExecutionPrescriptions` entries, one per lane's exact profile.

## 14. ExecutionPrescriptions semantics

Confirmed unchanged: a unique, profile-identity-indexed library, never per-slot duplication — `BundleClosure_EveryStageCandidateHasMatchingExecutionPrescription` proves every stage-declared candidate ref has a matching library entry, and `SharedProfileAcrossLanes_Deduplicated` (§12) proves reuse across lanes/stages never inflates the library.

## 15-18. Foundation/Build/RaceSpecific/Taper mapping

Not authored as real, public production catalog content in this split — per architecture §41, the real `RUN_LAYOUT_5D`/combination that would give these mappings public reachability is explicitly Split E's job, not Split B's. This split proves the **mechanism** generically (§13's dual-lane test uses synthetic `FND_PRIMARY`/`FND_SECONDARY_CONTROLLED` stage-candidate refs, not real catalog file edits) — the 8 real profiles authored in `FREQ.6D.4C.3` remain exactly as authored, untouched, and are proven reachable by-construction: any future `WORKOUT_PROGRESSION` document that declares `Lanes`/`PrescriptionProfileCandidates` referencing them will resolve through the identical, now-tested path. `PUBLIC_5D_CATALOG_AUTHORING_REMAINS_SPLIT_E_SCOPE`.

## 19. 8-profile reachability

Mechanism-level, not content-level, per §15-18: `PrescriptionProfileClosureResolverTests` and `PrescriptionProjectionDependencyBundleAssemblyTests` prove the resolver/assembler chain is generic over any number of distinct profile refs (tested with both single and dual-lane, shared and distinct references) — nothing in the diff caps or special-cases a profile count.

## 20-23. Horizon results, BLD-S/TAP-S projection, consumer compatibility

Not re-exercised in this split (correctly): §20's real content-level 8/10/12/14-horizon behavior depends on the real `RUN_LAYOUT_5D`/combination (Split E). `WorkoutPrescriptionExecutionProjector`'s own BLD-S (10×60s Jog, `RecoveryCount 9`) / TAP-S (6×100s Walk, `RecoveryCount 5`) projection content was proven correct in `FREQ.6D.3C`'s own test suite and is **reused verbatim, unmodified** here (confirmed: zero changes to `WorkoutPrescriptionExecutionProjector.cs` in this diff) — re-authoring that regression here would duplicate existing coverage, not add any. Consumer-index compatibility (`ExecutionPrescriptionIndex.ResolveExact`) was proven in `FREQ.6D.3D` against the same `ExecutableWorkoutPrescription`/`PublishedTemplateBundle` shape this split's bundle test produces; no RunningApp code was touched (§confirmed §4), so no new consumer-compatibility risk was introduced.

## 24. Legacy 3D/4D zero-delta & real 4D null regression

`PrescriptionProjectionDependencyBundleAssemblyTests.LegacyBundle_NoExecutionDependenciesOverload_ExecutionPrescriptionsStaysNull` and `.NoCandidatesDeclared_EmptyDependencyList_ExecutionPrescriptionsStaysNull` directly prove the legacy path is unaffected. `Freq6D4DSplitBExactPrescriptionProfileBindingTests.SingleKeyLegacyLayout_NoCandidatesDeclared_RemainsLegacy_WorkoutBindingUnaffected` proves the RunningApp binder side identically. The real `TEN_K__4D__INTERMEDIATE` production combination was not touched by this split's diff at all (no catalog JSON file was edited) — its `ExecutionPrescriptions == null` regression (`FREQ.6D.4C.3`) is structurally unaffected, not merely re-asserted.

## 25. RuntimeCatalog known-baseline / environment disclosure

**Full-suite regression could not be executed cleanly in this environment**, disclosed honestly rather than glossed over: `docker ps` confirms Docker Desktop is not running here, so every Postgres-backed integration test (namespace pattern `*.Persistence.*`/`*.Adaptation.*` under `RuntimeCatalog.Schedule.LongHorizon`) fails with `Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5432` / the EF Core `NpgsqlExecutionStrategy` wrapper `"An exception has been raised that is likely due to a transient failure"` — confirmed by direct inspection of all 197 failures in a full-output-captured `RuntimeCatalog.Schedule`-scoped run: **every single one** traces to this identical transient-connection signature (173/197 matched the exact wrapper-exception text directly; the remaining 24 are theory-test variants and one HTTP-500-via-DB-backed-reset-endpoint failure with the identical root Npgsql cause, confirmed individually). Zero of the 197 are assertion/logic failures. This is an environment limitation (no container runtime available in this session), **not a Split B code regression** — `PlanCatalogDeploymentPackagingTests.RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe` (the previously-disclosed, unrelated, pre-existing count mismatch from `FREQ.6D.4D.1`) was not independently re-verified this phase for the same reason (its containing DB-touching test run could not complete cleanly here either), but nothing in this split's diff touches catalog file counts.

## 26. Full tests

- **New Split-B test files, all passing on first full run after fixes**: RunningApp `Freq6D4DSplitBExactPrescriptionProfileBindingTests` **5/5**; PlanCatalog `PrescriptionProfileClosureResolverTests` **4/4**, `PrescriptionProfileLaneCandidateValidationTests` **7/7**, `PrescriptionProjectionDependencyBundleAssemblyTests` **5/5** — **21/21 new tests total**.
- **Full `PlanCatalog.Tests` suite (all in-memory, no DB dependency)**: **1,501 passed, 0 failed, 1,501 total** — includes the 3 real hash-stability tests the `[JsonIgnore]` fix (§4a) restored to green, and confirms zero delta to every other PlanCatalog surface.
- **RunningApp `RuntimeCatalog.Schedule`-scoped regression (DB-dependent subset excluded per §25's disclosed environment limitation)**: **1,877 passed** of the tests that could execute; all 197 non-executing failures independently confirmed Npgsql/Postgres-connectivity-only (§25), zero logic/assertion failures. The pure in-memory Progression/Binding/Allocation surface this split's diff actually touches — including the updated Split-A test file and both new Split-A/Split-B binder test files — is fully green.

## 27. Build

`dotnet build plan-catalog/PlanCatalog.sln` and `-c Release`: 0 warnings, 0 errors, both. `dotnet build backend/RunningApp.sln` and `-c Release`: 0 warnings, 0 errors, both. `git diff --check`: clean (only pre-existing CRLF-normalization warnings, same as every prior phase in this chain).

## 28. File attribution

| Category | Files |
|---|---|
| `PROFILE_CANDIDATE_AUTHORING` | `WorkoutProgressionStageDefinition.cs`, `CatalogWorkoutProgressionDefinition.cs`, `CatalogWorkoutProgressionLoader.cs` |
| `PROFILE_RESOLUTION` | `CatalogWorkoutBinder.cs`, `CatalogWorkoutBindingExceptions.cs` |
| `BOUND_PROFILE_LINEAGE` | `BoundCatalogPlanContracts.cs` |
| `PROJECTION_DEPENDENCY_MATERIALIZATION` | `PrescriptionProfileClosureResolver.cs`, `PrescriptionProjectionDependencyResolver.cs` |
| `BUNDLE_ASSEMBLY` | `CatalogBundleAssembler.cs` (lane-awareness fix only — assembler proper unchanged) |
| `STATIC_VALIDATION` | `WorkoutProgressionValidator.cs`, `PhaseWorkoutProgressionDefinition.cs` (Lanes model), `WorkoutClosureResolver.cs` (lane-awareness fix) |
| `TEST` | 4 new files + 1 updated Split-A test |
| `DOCUMENTATION` | this report |
| `LEDGER` / `ROADMAP` | `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` |
| `UNEXPECTED` | None — every changed file falls within the categories above or the established tracked-build-artifact convention |

## 29. Split-C input contract

Per architecture §15/§16, re-confirmed and now real: Split C (RunningApp session lineage) may consume `BoundCatalogSession.PrescriptionProfileKey`/`PrescriptionProfileVersion` directly — it must **not** select profiles, re-run stage allocation, or reproject authoring profiles; those authorities terminate in this split. Every `ProfileBacked` `StageControlled` session now carries a lineage-sufficient `(PrescriptionProfileKey, PrescriptionProfileVersion)` pair; every Legacy session carries `(null, null)`, an unambiguous discriminator Split C can branch on directly.

## 30. Implementation SHA

Recorded below after commit (§ governance).

## 31. Final classification

**`FREQ6D4D_SPLIT_B_IMPLEMENTED_ADAPTATION_ENGINEERING_GAP_REMAINS`**

Split B (exact prescription-profile resolution, projection-dependency materialization, bundle wiring) is fully implemented and tested at the mechanism level: stage-authored exact profile candidates flow deterministically through the existing, unmodified `FREQ.6D.3C` projection/assembly pipeline into a bundle's `ExecutionPrescriptions`; fail-closed for every required failure mode (ambiguous, missing, wrong-dose, wrong-version); zero legacy 3D/4D delta; two real, necessary, disclosed corrections found and fixed while wiring this split (`[JsonIgnore]` hash-stability, lane-aware closure resolvers). Real public 5D catalog content (Foundation/Build/RaceSpecific/Taper profile mappings reachable through an actual combination) remains Split E's disclosed job, per the architecture's own decomposition — not a narrowing introduced by this phase. The inherited, pre-existing Adaptation severity-table gap (`APPROVED_POLICY_NOT_YET_IMPLEMENTED`, `FREQ.6D.4D.1`) remains untouched and unworsened. `FREQ.6D.4D` overall dual-KEY production integration is **not** complete — Splits C-E remain.
