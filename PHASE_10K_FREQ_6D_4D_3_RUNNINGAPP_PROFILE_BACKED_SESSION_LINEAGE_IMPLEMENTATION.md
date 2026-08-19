# Phase 10K-FREQ.6D.4D.3 (Split C) — RunningApp Profile-Backed Session Lineage & Live Execution-Prescription Consumption

**Implementation phase executing Split C only of the FREQ.6D.4D-approved D1 architecture. No product decision, no dosage change, no stage allocation change, no lane re-derivation, no profile selection change, no projection change, no bundle authoring change, no WorkoutDefinition change, no profile content change, no database migration, no durable TrainingDay lineage yet, no Adaptation policy implementation, no public 5D activation.**

## 1. Preflight

`PHASE_LEDGER.md` row 74: `FREQ.6D.4D.2`, `IMPLEMENTATION`, `DONE`, `FREQ6D4D_SPLIT_B_IMPLEMENTED_ADAPTATION_ENGINEERING_GAP_REMAINS`, confirmed. Row 73 (`FREQ.6D.4D.1`) and row 56/58 (`FREQ.6D.3C`/`3D`) confirmed `DONE`/`VERIFIED`. Commits `72c11ca`, `380eec6`, `220c1a6` confirmed reachable from HEAD. Starting HEAD `220c1a60631eb31d16dacfa85021f635b0c2662e`, branch `main`, `git rev-list --left-right --count origin/main...HEAD` → `0 18`. `git status --short` → only ` m baseline_tmp` and the two pre-existing, unrelated `plan-catalog/artifacts/audits/*` modifications. `git diff --check` → clean. `FREQ.6D.4D.3` confirmed not already a ledger row. `docker ps` re-confirmed Docker Desktop still not running in this environment (same limitation Split B disclosed).

The real `PHASE_10K_FREQ_6D_4D_2_EXACT_PROFILE_DEPENDENCY_BUNDLE_IMPLEMENTATION.md` was re-read in full, extracting exactly: `BoundCatalogSession.PrescriptionProfileKey`/`PrescriptionProfileVersion` (both nullable, populated together or not at all — §10); Legacy = both null, ProfileBacked = both exact/non-null (§6); stage/profile resolution authority terminates in Split B (§29 Split-C input contract: "may consume `PrescriptionProfileKey`/`Version` directly — must not select profiles, re-run stage allocation, or reproject authoring profiles"); `ExecutionPrescriptions` bundle semantics — a unique library indexed by exact profile identity (§14); projection dependency semantics — already-resolved, never re-derived (§11-13); no profile catalog search allowed downstream (consistent with `FREQ.6D.4D` architecture §15).

The real `PHASE_10K_FREQ_6D_3D_RUNNINGAPP_EXECUTION_PRESCRIPTION_CONSUMER_IMPLEMENTATION.md`'s referenced code was read directly (current state): `ExecutionPrescriptionIndex` (`RuntimeCatalog/Prescription/Execution/ExecutionPrescriptionIndex.cs`) — exact-reference-only lookup, `IsProfileBacked` discriminates a legacy (`ExecutionPrescriptions == null`) bundle from a profile-backed one, `ResolveExact` fails closed with no latest/nearest/first-match; `PublishedTemplateBundleJsonReader` — a pure JSON→`PublishedTemplateBundle` deserializer, no file-discovery logic; `CatalogSessionPrescriptionSource` (`Legacy`/`ProfileBacked` discriminated record) — confirmed, per its own doc comment, **not yet wired into `CatalogSessionPrescriptionPlanner`'s live session-building branch** ("It exists here, proven by tests, as the seam FREQ.6D.4 wires into") — this is exactly the seam this split closes.

## 2. Split-B input contract

Honored throughout, confirmed by direct code review: `CatalogSessionPrescriptionPlanner.ResolvePrescriptionSource` (new) reads `session.PrescriptionProfileKey`/`PrescriptionProfileVersion` verbatim off the already-bound `BoundCatalogSession` — it does not read `LaneOrdinal`, `ProgressionStageKey`, `PhaseKey`, or `DoseCategory` anywhere in its own logic (confirmed by direct grep of the new method body). No call to `ProgressionStageAllocator`, `CatalogWorkoutBinder`, or any PlanCatalog stage/profile-candidate type exists in this split's diff.

## 3. Files inspected

`ExecutionPrescriptionIndex.cs`, `PublishedTemplateBundleJsonReader.cs`, `CatalogSessionPrescriptionSource.cs`, `CatalogSessionPrescriptionSourceTests.cs` (full reads); `CatalogSessionPrescriptionPlanner.cs`, `CatalogSessionPrescriptionContracts.cs`, `CatalogSessionPrescriptionExceptions.cs` (full reads, pre-Split-C state); `BoundCatalogPlanContracts.cs` (post-Split-B state, confirmed `PrescriptionProfileKey`/`Version` fields); `PlanCatalogBundleLoader.cs` (full read — confirmed it reads raw catalog *source* documents from `combinations/`/`templates/`/`layouts/`/`level-modifiers/`/`rule-packs/`, and does **not** load a published-bundle artifact at all; no real published-bundle file-discovery convention exists anywhere in this repository yet); `CatalogPreviewGenerator.cs`, `LivePlanPreviewRouting.cs`, `CatalogCandidateEligibilityGate.cs` (grepped for every `IPlanCatalogBundleLoader`/`CatalogSessionPrescriptionRequest` construction site — confirmed exactly two real callers, both via `PlanCatalogCandidateSummary`, neither sourcing a `PublishedTemplateBundle`); `Phase4F7BVolumeAndLongRunTests.cs` (full read — the established real-planner-invocation test-fixture convention, reused); `CatalogPrescribedSession`/`CatalogPrescribedPlan` consumers grepped repository-wide (`CatalogFinalPrescribedPlanValidator.cs`, `DynamicCoreSessionPrescriptionOrchestrator.cs`, `V1TaperSharpenPrescriptionPolicy.cs`, `PreparationRunwayPersistablePlanMapper.cs`, `CatalogPublicPreviewMaterializer.cs`, `LongHorizonRollingJitCompositionOrchestrator.cs`, `PreparationRunwayCoreWeekOnePaceAdapter.cs`) — confirmed real downstream consumers of the legacy `.Prescription`/`.PlannedDistanceKm` fields exist, directly informing §5's additive-field design decision.

## 4. Files changed

**Production (4 files, `backend/RunningApp.Application/RuntimeCatalog/Prescription/`)**:
- `Execution/CatalogSessionPrescriptionSource.cs` — doc comment updated to reflect this split's wiring (no logic change).
- `Session/CatalogSessionPrescriptionContracts.cs` — added `ExecutionPrescriptionIndex? ExecutionIndex = null` to `CatalogSessionPrescriptionRequest` (trailing, defaulted, additive); added `required CatalogSessionPrescriptionSource PrescriptionSource` to `CatalogPrescribedSession`.
- `Session/CatalogSessionPrescriptionExceptions.cs` — added `CatalogSessionPrescriptionInvalidProfileLineageException`, `CatalogSessionPrescriptionMissingExecutionPrescriptionException`, `CatalogSessionPrescriptionProfileWorkoutMismatchException`.
- `Session/CatalogSessionPrescriptionPlanner.cs` — new `ResolvePrescriptionSource` method (the classification/resolution boundary, §5-7 below); `BuildSession` now also constructs and attaches `PrescriptionSource`.

**Test (1 file modified for the new required field, 1 new file)**:
- `PreparationRunwayPaceMaterializerTests.cs` — one hand-constructed `CatalogPrescribedSession` fixture updated to populate the now-required `PrescriptionSource` (mechanical, matches its own existing `CatalogWorkoutPrescription`).
- `Freq6D4DSplitCProfileBackedSessionConsumptionTests.cs` (new, 16 tests) — exercises the **real** `CatalogSessionPrescriptionPlanner`/`CatalogPrescriptionContextBuilder`/`CatalogVolumeAndLongRunPlanner` pipeline end-to-end (not a synthetic binder-only fixture, unlike Split A/B), since the generalized N-KEY volume allocator (`Phase 10K-FREQ.4`) already supports a real 5-session (2×KEY + 2×EASY + 1×LONG) week.

Plus the mechanical rebuild of tracked `bin/`/`obj/` build artifacts.

**No** PlanCatalog file was touched (confirmed: `git status` shows zero `plan-catalog/src`/`plan-catalog/tests` changes this split) — matching §42's expected scope exactly. **No** `WorkoutPrescriptionExecutionProjector`, `CatalogBundleAssembler`, `ExecutionPrescriptionIndex`, `PublishedTemplateBundleJsonReader`, `TrainingDay`/database, or `NextWindowLoadDecisionPolicy`/Adaptation file was touched.

## 5. Legacy/ProfileBacked classification

`ResolvePrescriptionSource(request, session, legacyPrescription)` is the sole classification boundary (§3 of the prompt honored exactly):

- `PrescriptionProfileKey == null && PrescriptionProfileVersion == null` → `Legacy`, wrapping the same, still-always-computed `CatalogWorkoutPrescription` instance.
- Both non-null → `ProfileBacked`, resolved per §6 below.
- Exactly one non-null → `CatalogSessionPrescriptionInvalidProfileLineageException` (never interpreted as Legacy — `PartialLineage_KeyOnly_ThrowsInvalidLineage`/`_VersionOnly_ThrowsInvalidLineage`).

**Design decision, disclosed**: the legacy distance/pace/duration/segment computation (`DistanceFor`/`PaceFor`/`SegmentsFor`/`DurationFor`) still runs unconditionally for **every** session, Legacy or ProfileBacked — `CatalogPrescribedSession.Prescription`/`PlannedDistanceKm`/etc. remain always-populated, byte-identical to pre-Split-C behavior. `PrescriptionSource` is a purely **additive** field carrying the classification and, for ProfileBacked sessions, the exact resolved `ExecutableWorkoutPrescription` alongside it. This was a deliberate, narrow choice: making the legacy fields optional would have required touching 5 real, unrelated downstream consumers (`CatalogPublicPreviewMaterializer`, `PreparationRunwayPersistablePlanMapper`, etc.) that unconditionally read them today — completely disproportionate to this split's mandate and a direct risk to the "Legacy zero-delta" invariant (§8/§16/§22/§23/§28-30 of the prompt). Split D (persistence) is the natural point to decide whether/how `TrainingDay` materialization prefers `PrescriptionSource.ProfileBacked` over the legacy fields once durable columns exist.

## 6. Exact lookup path

For a classified-ProfileBacked session: if `request.ExecutionIndex` is absent, `CatalogSessionPrescriptionMissingExecutionPrescriptionException` is thrown immediately — **never** a silent Legacy degradation (§4 of the prompt, proven directly by `ProfileBacked_MissingExecutionIndex_NeverFallsBackToLegacy_ThrowsTypedException`). When present, `index.ResolveExact(new VersionedCatalogReference { DocumentType = WORKOUT_PRESCRIPTION_PROFILE, Key, Version })` is called — the exact same, unmodified `ExecutionPrescriptionIndex.ResolveExact` from `FREQ.6D.3D`, which itself already fails closed for missing-entry (`ExecutionPrescriptionNotFoundException`) and wrong-version (no latest/nearest/first-match) — both exceptions propagate verbatim, not re-wrapped, so `FREQ.6D.3D`'s typed exceptions remain the single authority for those specific failure modes.

## 7. CatalogSessionPrescriptionSource wiring

Reused verbatim, zero logic duplication (§6 of the prompt honored): the planner constructs exactly `new CatalogSessionPrescriptionSource.Legacy(legacyPrescription)` or `new CatalogSessionPrescriptionSource.ProfileBacked(execution)` — both cases wrap the pre-existing/already-resolved value without copying fields, using the exact discriminated-union shape `FREQ.6D.3D` already built and tested (`CatalogSessionPrescriptionSourceTests.cs`, still passing unchanged, §22).

## 8. CatalogSessionPrescriptionPlanner wiring

The minimum change identified and made (§7 of the prompt): one new private method (`ResolvePrescriptionSource`) and one new local variable/field assignment in `BuildSession`. `CatalogSessionPrescriptionPlanner` does not re-derive `LaneOrdinal` (confirmed: the pre-existing `currentKeyOrdinal` local, from Split A, is still used only for `DistanceFor`'s legacy volume-allocation indexing — untouched), does not re-select a profile (no candidate list, no dose-category branch anywhere in the new code), and does not re-project an authoring profile (`WorkoutPrescriptionExecutionProjector` is never referenced in `RunningApp.Application` at all — confirmed by repository-wide grep, zero hits outside `plan-catalog/`).

## 9. Internal generated-session lineage

`CatalogPrescribedSession` (the real, existing "internal generated-session object that carries the actual workout/session prescription before persistence," per §12 of the prompt) is the narrowest existing contract, and it required exactly one additive field (`PrescriptionSource`) — no new duplicate authoring-profile type was invented.

## 10-11. Lane / Stage preservation

`BoundCatalogSession.LaneOrdinal` and `.ProgressionStageKey` are read directly off `session` throughout `BuildSession` exactly as before Split C (unchanged code paths — `LaneOrdinal` still feeds `currentKeyOrdinal` for legacy volume indexing; `ProgressionStageKey` is copied verbatim onto `CatalogPrescribedSession.ProgressionStageKey`, unchanged). Neither is lost or recomputed by the new `ResolvePrescriptionSource` method — proven directly by `LaneOrdinal_ProgressionStageKey_ProfileKeyVersion_AllPreservedThroughInternalMaterialization`.

## 12. Profile lineage preservation

`PrescriptionProfileKey`/`Version` are consumed, never re-derived, and the resolved `ExecutableWorkoutPrescription`'s own `SourceProfile.Key`/`Version` are asserted to match what was requested (`ResolveExact`'s own exact-match contract) — proven by `ProfileBacked_BothExactFieldsPresent_ResolvesExactExecutionPrescription`.

## 13. Calendar materialization

`BuildSession` computes `distance`/`pace`/`duration` from `session.StructuralRole`/`Date`/allocation exactly as before — none of these computations reads `PrescriptionProfileKey`/`Version`/`LaneOrdinal`/`ProgressionStageKey` as an input, so calendar placement cannot influence profile identity by construction. Directly proven, not merely asserted by absence: `CalendarOrderIndependence_LaneOrderByDateDiffersFromStructuralOrder_ProfileIdentityUnchanged` constructs a week where Lane 1 (SecondaryControlled/`FARTLEK`/`PROFILE_B`) is dated *before* Lane 0 (Primary/`THRESHOLD_TEMPO`/`PROFILE_A`) and confirms each session still resolves its own bound profile, never swapped by date order.

## 14. Dual-lane consumer proof

`DualLane_TwoExactProfiles_ConsumedIndependently_NoDoseCategoryInspectionAtConsumerTime` — a real, hand-constructed dual-lane `BoundCatalogPlan` (2×KEY_SESSION + 2×EASY_SUPPORT + 1×LONG_RUN per week, matching the real generalized N-KEY volume allocator from `Phase 10K-FREQ.4`) flows through the actual `CatalogSessionPrescriptionPlanner.Build` with a real `ExecutionPrescriptionIndex` containing both lanes' executions; lane 0 resolves `PROFILE_A`, lane 1 resolves `PROFILE_B`, independently. `SameStage_DifferentLane_ResolvesDistinctExactProfileExecutions` additionally confirms both lanes share the identical `ProgressionStageKey` string (same week/phase) yet still resolve distinct exact profiles — proving lane, not stage alone, is the consumer-side discriminator.

## 15. Fail-closed behavior

| Case | Typed result | Test |
|---|---|---|
| Partial lineage (key only) | `CatalogSessionPrescriptionInvalidProfileLineageException` | `PartialLineage_KeyOnly_ThrowsInvalidLineage` |
| Partial lineage (version only) | `CatalogSessionPrescriptionInvalidProfileLineageException` | `PartialLineage_VersionOnly_ThrowsInvalidLineage` |
| ProfileBacked, no index supplied | `CatalogSessionPrescriptionMissingExecutionPrescriptionException` | `ProfileBacked_MissingExecutionIndex_NeverFallsBackToLegacy_ThrowsTypedException` |
| Exact profile missing from bundle | `ExecutionPrescriptionNotFoundException` (FREQ.6D.3D, reused) | `ProfileBacked_ExactProfileMissingFromBundle_FailsClosed` |
| Wrong exact version | `ExecutionPrescriptionNotFoundException` (FREQ.6D.3D, reused) | `ProfileBacked_WrongExactVersion_FailsClosed_DoesNotUseOtherVersion` |
| Resolved execution's `SourceWorkout` diverges from bound session's `WorkoutDefinitionKey`/`Version` | `CatalogSessionPrescriptionProfileWorkoutMismatchException` (new, this split) | `ProfileBacked_WorkoutProvenanceMismatch_FailsClosed` |

No `Lane1 missing → fall back to Legacy` or `unresolvable → null result` path exists anywhere in the diff (confirmed by direct review).

**One real, disclosed nuance surfaced while wiring this**: Split B kept `BoundCatalogSession.WorkoutDefinitionKey`/`Version` sourced from the stage's own pre-existing `WorkoutCandidateReferences` (the `Phase 4F.6B` mechanism, unchanged) rather than from the profile's own embedded `WorkoutDefinitionRef` — two independent authorities that a correctly-authored real catalog would keep consistent, but that no validator structurally guarantees equal. This split adds a narrow, fail-closed consistency check (`CatalogSessionPrescriptionProfileWorkoutMismatchException`) rather than silently trusting either authority — a safety net, not a redesign of either split's own scope.

## 16. Legacy zero-delta

`Legacy_WithNullExecutionLibrary_ContinuesWorkingExactly_RealFourDayCompatibilityCase` (the real 3D/4D single-KEY 4-session shape, no profile lineage, no index) produces a fully valid plan with every session classified `Legacy`. `Legacy_WithNonNullUnrelatedLibrary_StillUsesLegacyPath_LibraryExistenceDoesNotUpgrade` proves a legacy-classified session stays Legacy even when a real, populated `ExecutionPrescriptionIndex` is supplied — the existence of a library never upgrades session identity (§9/§23 of the prompt). No real production caller (`CatalogPreviewGenerator`, `DynamicCoreSessionPrescriptionOrchestrator`) was modified to pass an `ExecutionIndex` in this split (§ below), so every current real Intermediate×3D/4D/Beginner×4D candidate resolves exactly as before Split C — zero behavioral delta, confirmed by the 500/500 and 1,962/1,972 in-memory regression runs (§22).

## 17. Content fidelity

`ContentFidelity_ProfileBackedPrescriptionMatchesBundlePayloadExactly_NoRebuild` asserts, on the resolved `ProfileBacked` session, the exact fixture-authored values survive verbatim: `RepetitionCount=10`, `Work.Value=60` (seconds), `Recovery.Value=60`/`Mode=Jog`/`Placement=BetweenRepetitions`/`RecoveryCount=9` (the real, established BLD-S recovery shape from `FREQ.6D.3C`), `DoseCategory=SecondaryControlled`, `SourceWorkout.Key="FARTLEK"`. `NoRuntimeProjection_ResolvedExecutionIsTheExactBundleInstance` additionally asserts `Assert.Same` (reference equality) between the index's own resolved instance and what the planner attaches — proving no rebuild/recomputation occurs anywhere in the path (§10/§30 of the prompt).

## 18. DB/persistence zero-delta

No `TrainingDay` field, EF mapping, or migration was added (confirmed: zero files under `backend/RunningApp.Persistence`/`backend/RunningApp.Domain/Entities` changed). `STOP: FREQ6D4D3_BLOCKED_ON_SESSION_MATERIALIZATION_BOUNDARY` was not triggered — the existing `CatalogPrescribedSession` internal contract was sufficient to carry the resolved prescription without any persistence-layer change.

## 19. Adaptation zero-delta

`NextWindowLoadDecisionPolicy`, `WindowExecutionSummaryBuilder`, `ScheduleRepairRuntimeOrchestrator` were not touched (confirmed by diff review). The known `APPROVED_POLICY_NOT_YET_IMPLEMENTED` gap remains exactly as `FREQ.6D.4D.1` disclosed it.

## 20. Runtime environment status

`docker ps` confirms Docker Desktop remains unavailable in this environment (same limitation Split B disclosed — unchanged since). Per §38 of the prompt, pure in-memory/component tests exercising the real consumer chain were prioritized: all 16 new Split-C tests and the full `RuntimeCatalog.Prescription`/`.Schedule.Binding`/`.Schedule.Progression` in-memory surface (500 tests) execute cleanly with zero DB dependency. A broader `RuntimeCatalog`-scoped run (excluding `LongHorizon`, the DB-heaviest namespace) showed 1,962 passed / 10 failed — all 10 confirmed Npgsql-connection-refused failures (same signature as Split B's disclosed 197), zero logic/assertion failures. No load-bearing Split-C assertion depends on a database — `VERIFICATION_LIMITED_BY_ENVIRONMENT` does not apply to this split's own correctness claims, only to the pre-existing, unrelated DB-backed suites that remain unexecutable here.

## 21. Targeted tests

All 30 items from the prompt's §40 matrix are covered by the 16 new tests (several items collapse onto one test where the same assertion proves multiple numbered claims — e.g. one dual-lane test proves items 12/13 together): 1-2 (`Legacy_BothProfileFieldsNull...`/`ProfileBacked_BothExactFieldsPresent...`), 3-4 (`PartialLineage_*`), 5-8 (`ProfileBacked_*` resolution/missing/wrong-version/wrong-key via the two divergent-bundle tests), 9 (`ProfileBacked_MissingExecutionIndex_NeverFallsBackToLegacy...`), 10-11 (`Legacy_WithNull...`/`Legacy_WithNonNull...`), 12-13 (`DualLane_*`/`SameStage_DifferentLane_*`), 14 (`CalendarOrderIndependence_*`), 15-18 (`LaneOrdinal_ProgressionStageKey_ProfileKeyVersion_AllPreserved...`), 19-23 (`ContentFidelity_*`), 24-25 (`NoRuntimeProjection_*` — proves no projector call by reference-equality; no authoring-profile lookup is proven structurally, §11 of the prompt, by the fact `RunningApp.Application` has zero reference to `PlanCatalog.Core`), 26-27 (`ExecutionPrescriptionIndex`/`CatalogSessionPrescriptionSource` unchanged, confirmed by `CatalogSessionPrescriptionSourceTests.cs` passing unmodified), 28-30 (`Legacy_WithNullExecutionLibrary_...` directly exercises the real 3D/4D shape; the broader 500/500 and 1,962/1,972 regression runs independently confirm Intermediate×3D/4D/Beginner×4D zero-delta).

## 22. Full regressions

- **New Split-C tests**: `Freq6D4DSplitCProfileBackedSessionConsumptionTests` **16/16**.
- **Scoped in-memory regression** (`RuntimeCatalog.Prescription` + `.Schedule.Binding` + `.Schedule.Progression` + all `Freq6D4D*` tests): **500/500 passed**.
- **Broad `RuntimeCatalog` regression excluding `LongHorizon`** (the DB-heaviest namespace): **1,962 passed, 10 failed** — all 10 independently confirmed Npgsql-connection-refused (identical signature to Split B's disclosed 197), zero logic/assertion failures.
- **`CatalogSessionPrescriptionSourceTests`** (the pre-existing `FREQ.6D.3D` test file this split's production code now actually exercises live): unchanged, still passing.
- Debug build: 0 warnings/errors. Release build: 0 warnings/errors. `git diff --check`: clean (CRLF-normalization warnings only).
- **No PlanCatalog file changed this split** — the pre-existing 1,501/1,501 PlanCatalog baseline (`FREQ.6D.4D.2`) is unaffected by construction, not re-run.

## 23. File attribution

| Category | Files |
|---|---|
| `SESSION_PRESCRIPTION_CONSUMPTION` | `CatalogSessionPrescriptionPlanner.cs` |
| `PROFILEBACKED_CLASSIFICATION` | `CatalogSessionPrescriptionPlanner.cs` (`ResolvePrescriptionSource`) |
| `INTERNAL_SESSION_LINEAGE` | `CatalogSessionPrescriptionContracts.cs` |
| `CONSUMER_GLUE` | `CatalogSessionPrescriptionSource.cs` (doc comment only), `CatalogSessionPrescriptionExceptions.cs` |
| `TEST` | `Freq6D4DSplitCProfileBackedSessionConsumptionTests.cs` |
| `PRE_EXISTING_TEST_BASELINE_MAINTENANCE` | `PreparationRunwayPaceMaterializerTests.cs` (mechanical field addition, not a behavior change) |
| `DOCUMENTATION` | this report |
| `LEDGER` / `ROADMAP` | `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` |
| `UNEXPECTED` | None |

## 24. Split-D persistence-gap matrix

`SESSION_LINEAGE_PERSISTENCE_GAP_MATRIX`

| Datum | In memory now? | Persisted now? | Required to persist? | Derivable safely? | Must remain exact? |
|---|---|---|---|---|---|
| `StructuralRole` | Yes | Yes (`CatalogStructuralRole`) | Already satisfied | N/A | Yes |
| `LaneOrdinal` | Yes (`BoundCatalogSession`, survives to `CatalogPrescribedSession` indirectly via `currentKeyOrdinal`, not itself copied onto `CatalogPrescribedSession`) | No | Recommended new nullable column, or reconstructible via `(CatalogProgressionStageKey, progression key+version)` once progression artifacts are treated as immutable (per `FREQ.6D.4D §17`) | Yes, per architecture's own §17 finding | Yes |
| `ProgressionStageKey` | Yes (`CatalogPrescribedSession.ProgressionStageKey`) | Yes (`CatalogProgressionStageKey`) | Already satisfied | N/A | Yes |
| `PrescriptionProfileKey` | Yes (`BoundCatalogSession`; observable via `CatalogPrescribedSession.PrescriptionSource.ProfileBacked.Prescription.SourceProfile.Key`) | No | Yes — new nullable `TrainingDay` column, per `FREQ.6D.4D §17`/§39 | No (immutable-profile assumption aside, no safe reconstruction without persisting it) | Yes |
| `PrescriptionProfileVersion` | Yes (same path) | No | Yes — new nullable `TrainingDay` column | No | Yes |
| `WorkoutDefinitionKey`/`Version` | Yes | Yes (`CatalogWorkoutDefinitionKey`/`Version`) | Already satisfied | N/A | Yes |
| Execution-prescription identity (`SourceProfile`+`SourceWorkout`+hash) | Yes, transiently (only for the duration of this request — `PrescriptionSource.ProfileBacked` is never persisted itself) | No | `BUNDLE_ONLY` per `FREQ.6D.4D §17` — reconstructible forever from the immutable `(ProfileKey, ProfileVersion)` pair against any historical published bundle; no redundant hash column needed | Yes | Its identity (not its full content) must remain exact |

This split makes every datum above **observable in memory** for the first time at the consumer layer; it persists none of them, per §33's explicit prohibition.

## 25. Split-D readiness

Per §36 of the prompt, all ten conditions are met: (1) bound ProfileBacked sessions carry exact profile refs — yes, unchanged since Split B, now also consumed; (2) RunningApp consumes exact executable prescriptions from the bundle — yes, proven end-to-end this split; (3) no runtime profile search — confirmed, zero `DoseCategory`/candidate-list logic in the new code; (4) no runtime projection — confirmed, `NoRuntimeProjection_ResolvedExecutionIsTheExactBundleInstance`; (5) Legacy sessions unchanged — confirmed, §16; (6) mixed classification is per-session — confirmed, `Legacy_WithNonNullUnrelatedLibrary_...`; (7) `LaneOrdinal` survives internal materialization — confirmed, still read verbatim off `BoundCatalogSession` throughout; (8) `ProgressionStageKey` survives — confirmed, copied onto `CatalogPrescribedSession` unchanged; (9) exact profile lineage is now observable at the point persistence work would read from (`PrescriptionSource`) — confirmed, §24; (10) missing exact bundle entry fails closed — confirmed, §15. Split D's own remaining, disclosed work: add the two new nullable `TrainingDay` columns, wire `CatalogPlanConfirmationService.BuildCatalogTrainingDay` (or equivalent) to copy `PrescriptionSource.ProfileBacked` lineage onto them when present, one EF migration, and implement the already-`FREQ.6`-approved 5-session Adaptation severity table.

## 26. Implementation SHA

Recorded below after commit (§ governance).

## 27. Final classification

**`FREQ6D4D_SPLIT_C_IMPLEMENTED_ADAPTATION_ENGINEERING_GAP_REMAINS`**

Split C (RunningApp profile-backed session lineage consumption) is fully implemented and tested: the previously-dormant `CatalogSessionPrescriptionSource`/`ExecutionPrescriptionIndex` seam (`FREQ.6D.3D`) is now genuinely wired into `CatalogSessionPrescriptionPlanner`'s live session-building path; classification is exact and per-session; every required failure mode is fail-closed via typed exceptions, including one new, disclosed safety check (`CatalogSessionPrescriptionProfileWorkoutMismatchException`) guarding the two-independent-authority nuance surfaced while wiring this split; Legacy 3D/4D/Beginner×4D behavior is provably unchanged (zero real production caller passes an `ExecutionIndex` yet, since no real published-bundle file-discovery convention exists — a disclosed, narrow, non-blocking follow-up, not a defect); content fidelity (recovery cardinality, dose category, workout provenance) survives unmodified end-to-end. The inherited, pre-existing Adaptation severity-table gap remains untouched and unworsened. `FREQ.6D.4D` overall dual-KEY production integration is **not** complete — Split D (persistence + repair lineage + 5-session Adaptation) and Split E (real `RUN_LAYOUT_5D`/combination + published-bundle file-discovery wiring) remain.
