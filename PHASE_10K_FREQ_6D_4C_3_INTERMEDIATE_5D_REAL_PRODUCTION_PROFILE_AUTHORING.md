# PHASE 10K-FREQ.6D.4C.3 — Intermediate 5D Real Production Workout Prescription Profile Authoring

**Implementation phase materializing the frozen FREQ.6D.4B/4B.3 Intermediate×5D prescription policy as eight real production `WorkoutPrescriptionProfile` catalog artifacts. No product decision, no new evidence, no dosage selection, no WorkoutDefinition skeleton change, no status promotion, no validator/overlay/schema/projector architecture change, no RunningApp change, no progression/lane wiring, no persistence change, no public activation.**

## 1. Preflight

`PHASE_LEDGER.md` rows 60-68: `FREQ.6D.4B` (61), `FREQ.6D.4C.1` (63), `FREQ.6D.4C.2` (64), `FREQ.6D.4B.1` (65), `FREQ.6D.4B.2` (66), `FREQ.6D.4B.3` (67), `FREQ.6D.4B.4` (68) all `DONE`/`VERIFIED`. `FREQ.6D.4B.4` final classification confirmed exactly `FREQ6D4B4_IMPLEMENTED_CATALOG_LIFECYCLE_BLOCKER_REMAINS`. Implementation commit `1b63f83` and governance commit `d91cee5` both confirmed reachable from HEAD via `git merge-base --is-ancestor`. Starting HEAD `d91cee526c4c4eaebe52147a776460a54d10f872`, branch `main`, `git rev-list --left-right --count origin/main...HEAD` → `0 2` (2 ahead, 0 behind — no push gate reached, no push performed). `git status --short` → ` m baseline_tmp` only (pre-existing, unrelated, preserved). `git diff --check` → clean.

Real 4B.3 report (`PHASE_10K_FREQ_6D_4B_3_...md`) and real 4B.4 report (`PHASE_10K_FREQ_6D_4B_4_...md`) read in full — see §2 below for the extracted exact authority. Real repository inspection (not report prose) confirmed: `catalog/workouts/fartlek.v5.json` is exactly `WARM_UP, MAIN_SET, COOL_DOWN` (`DRAFT`); `fartlek.v4.json` is unchanged, exactly `WARM_UP, MAIN_SET, RECOVERY, COOL_DOWN` (`VALIDATED`). `catalog/prescription-profiles/` did not exist before this phase — `FileSystemCatalogSourceRepository.LoadAll` returns `[]` for a missing subfolder, confirming the true pre-phase baseline of zero real production profile documents (also directly confirmed via `snapshot.PrescriptionProfiles.Count == 0` before authoring).

## 2. Parent/gate baseline extracted from real reports

From FREQ.6D.4B.3 (`FREQ6D4B3_PRODUCT_POLICY_APPROVED_WITH_BLD_S_V5_REFERENCE_AMENDMENT`): FC1-FC6 closed — one shared `WARM_UP` (Continuous, 600s, `EffortBased`/`EASY`) and one shared `COOL_DOWN` (Continuous, 300s, `EffortBased`/`EASY`) across all eight profiles, all phases, both dose categories. FC7 = `R1_VERSIONED_FARTLEK_SKELETON_CORRECTION` (inherited, closed). FC8/FC9 = `NOT_APPLICABLE`. FC10 closed: `EstimatedSessionTotal` retained for all eight, nested recovery counted exactly once, arithmetic remains downstream authority. BLD-S amended `FARTLEK v4 → corrected v5`; TAP-S preserved at `v5`; all other six references unchanged. Full eight-profile product-authority matrix (§17 of that report) supplied every exact main-set field.

From FREQ.6D.4B.4 (`FREQ6D4B4_IMPLEMENTED_CATALOG_LIFECYCLE_BLOCKER_REMAINS`): corrected `FARTLEK v5` DRAFT is exactly three rows; `FARTLEK v4` remains `VALIDATED`, four-row, byte-unchanged (`FARTLEK_V4_HISTORICAL_IMMUTABILITY_PRESERVED`); all eight full fixtures (test-only, not production) validated, projected losslessly and passed the boundary validator with zero structural `RECOVERY`; 1,396/1,396 full regression at that phase's close.

## 3. Files inspected

`plan-catalog/schemas/workout-prescription-profile.schema.json`; `src/PlanCatalog.Core/Models/WorkoutPrescriptionProfile.cs`; `src/PlanCatalog.Core/Validation/WorkoutPrescriptionProfileValidator.cs`; `src/PlanCatalog.Core/Validation/PrescriptionProfileLaneDoseValidator.cs`; `src/PlanCatalog.Core/Validation/CatalogGraphValidator.cs`; `src/PlanCatalog.Infrastructure/Projection/WorkoutPrescriptionExecutionProjector.cs`; `src/PlanCatalog.Infrastructure/Publishing/CatalogBundleAssembler.cs`; `src/PlanCatalog.Infrastructure/Publishing/CatalogStamper.cs`; `src/PlanCatalog.Infrastructure/Serialization/CanonicalJsonOptions.cs`; `tests/PlanCatalog.Tests/Validation/PrescriptionCapabilityMetadataOverlayTests.cs` (exact frozen field values already proven there against the corrected v5); `tests/PlanCatalog.Tests/Projection/PrescriptionBundleProjectionIntegrationTests.cs`; `catalog/workouts/fartlek.v4.json`, `fartlek.v5.json`.

## 4. Files changed

- 8 new profile source documents: `catalog/prescription-profiles/intermediate-5d-{foundation,build,race-specific,taper}-{primary,secondary-controlled}.v1.json`.
- 1 new test file: `tests/PlanCatalog.Tests/Validation/Intermediate5DProductionPrescriptionProfileSourceTests.cs` (64 tests).
- 2 test files updated to retire now-stale zero-profile assertions: `Intermediate5DProductionPrescriptionCatalogTests.cs` (FREQ.6D.4C's own `RealCatalog_HasNoPrescriptionProfilesAuthoredYet`, renamed and updated to assert the count as of *that* phase's own commit boundary, `== 8` at today's real state — corrected to reflect this phase's real, intentional addition) and `FileSystemCatalogSourceRepositoryTests.cs` (removed an incidental, unrelated `Assert.Empty(PrescriptionProfiles)` from a registry-loading test).
- 2 auto-regenerated audit artifacts (`artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}`) — diff is timestamp-only, verified via `git diff` with the `generatedAtUtc`/`Generated:` lines excluded.
- **Zero infrastructure/production code changed** — schema, model, validator, overlay, projector, loader, graph validator, publisher, stamper are all byte-identical to the FREQ.6D.4B.4 baseline.

## 5. Real profile count

8 (0 → 8). `snapshot.PrescriptionProfiles.Count == 8` confirmed via `RealCatalog_HasExactlyEightProductionPrescriptionProfiles`.

## 6. Eight profile identities

| Slot | Profile key | File |
|---|---|---|
| FND-P | `INTERMEDIATE_5D_FOUNDATION_PRIMARY` v1 | `intermediate-5d-foundation-primary.v1.json` |
| FND-S | `INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED` v1 | `intermediate-5d-foundation-secondary-controlled.v1.json` |
| BLD-P | `INTERMEDIATE_5D_BUILD_PRIMARY` v1 | `intermediate-5d-build-primary.v1.json` |
| BLD-S | `INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED` v1 | `intermediate-5d-build-secondary-controlled.v1.json` |
| RS-P | `INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY` v1 | `intermediate-5d-race-specific-primary.v1.json` |
| RS-S | `INTERMEDIATE_5D_RACE_SPECIFIC_SECONDARY_CONTROLLED` v1 | `intermediate-5d-race-specific-secondary-controlled.v1.json` |
| TAP-P | `INTERMEDIATE_5D_TAPER_PRIMARY` v1 | `intermediate-5d-taper-primary.v1.json` |
| TAP-S | `INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED` v1 | `intermediate-5d-taper-secondary-controlled.v1.json` |

Keys are deterministic/semantic (distance implied by program, level+frequency+phase+dose), not `KEY1`/`KEY2`/`Lane0`/`Lane1` runtime vocabulary, per §14 of the phase prompt. All distinct; no reuse.

## 7. Exact WorkoutDefinition refs

Exactly as required and verified via `MixedExactWorkoutVersions_AreSimultaneouslyReferencedWithoutAutoUpgrade`: FND-P→`AEROBIC_STRENGTH_CONTROLLED_INTRO v3`; FND-S→`THRESHOLD_TEMPO v5`; BLD-P→`THRESHOLD_TEMPO v4`; BLD-S→`FARTLEK v5`; RS-P→`GOAL_PACE_TEN_K v2`; RS-S→`THRESHOLD_TEMPO v4`; TAP-P→`GOAL_PACE_TEN_K v3`; TAP-S→`FARTLEK v5`. `THRESHOLD_TEMPO v4` and `v5` are simultaneously referenced by distinct profiles; `GOAL_PACE_TEN_K v2` and `v3` are simultaneously referenced; `FARTLEK v5` is referenced twice (BLD-S, TAP-S) while `FARTLEK v4` remains unreferenced by any of the eight and unchanged. No profile resolver auto-upgrades exact versions — all lookups are exact `(key, version)`.

## 8. Warm-up implementation

All eight: `ComponentType=WARM_UP`, `StructureMode=Continuous`, `WorkQuantity.DurationSeconds=600`, `IntensityTarget={Mode=EffortBased, EffortDescriptorKey="EASY"}`, no repetition/recovery fields. Verified for all eight by `EachSlot_HasExactSharedWarmUpAndCoolDown_AndNoStructuralRecovery`.

## 9. Cooldown implementation

All eight: `ComponentType=COOL_DOWN`, `StructureMode=Continuous`, `WorkQuantity.DurationSeconds=300`, `IntensityTarget={Mode=EffortBased, EffortDescriptorKey="EASY"}`, no repetition/recovery fields. Same test as §8.

## 10-17. Per-slot content

| Slot | Main structure | Main work | Recovery | Intensity | Descriptor | Dose |
|---|---|---|---|---|---|---|
| FND-P | Repeated ×6 | 30s | 90s Jog, BetweenReps | EffortBased | `CONTROLLED_AEROBIC_POWER_INTRO` | Primary |
| FND-S | Continuous | 1200s | — | EffortBased | `CONTROLLED_THRESHOLD_INTRO` | SecondaryControlled |
| BLD-P | Continuous | 2400s | — | PaceBased | `THRESHOLD_PACE` | Primary |
| BLD-S | Repeated ×10 | 60s | 60s Jog, BetweenReps | EffortBased | `SURGE_FASTER_THAN_5K_EFFORT` | SecondaryControlled |
| RS-P | Continuous | 1200s | — | PaceBased | `GOAL_PACE_TEN_K` | Primary |
| RS-S | Continuous | 1500s | — | PaceBased | `THRESHOLD_SUPPORT_PACE` | SecondaryControlled |
| TAP-P | Continuous | 600s | — | PaceBased | `GOAL_PACE_TEN_K` | Primary |
| TAP-S | Repeated ×6 | 20s | 100s Walk, BetweenReps | EffortBased | `CONTROLLED_STRIDES_SHARPENING` | SecondaryControlled |

Every value is copied exactly from the frozen FREQ.6D.4B/4B.3 matrix — zero implementation discretion. Verified per-slot by `EachSlot_MainSetMatchesExactFrozenAuthority`.

## 18. D1-D18 fidelity

All D1-D18 (FREQ.6D.4B) athlete-facing values are materialized exactly as decided — main-set structure, work, recovery, intensity, descriptor and dose category for all eight slots (§10-17), the TAP-S FARTLEK-reuse decision (no new WorkoutDefinition identity), and the KEY2-floor/D18 engineering deferral (not applicable to source content). Classification: `IMPLEMENTED_EXACTLY` for every applicable D-item.

## 19. FC1-FC10 fidelity

| ID | Result |
|---|---|
| FC1 | Implemented — one shared WARM_UP reused across all 8 |
| FC2 | Implemented — 600s |
| FC3 | Implemented — EffortBased/EASY |
| FC4 | Implemented — one shared COOL_DOWN reused across all 8 |
| FC5 | Implemented — 300s |
| FC6 | Implemented — EffortBased/EASY |
| FC7 | Represented — all BLD-S/TAP-S content authored against the corrected 3-component FARTLEK v5, zero structural RECOVERY |
| FC8 | N/A — no structural recovery component authored |
| FC9 | N/A — no structural recovery component authored |
| FC10 | Preserved — nested recovery is the sole recovery representation; recovery contributes to accounting exactly once |

## 20. Recovery/no-double-count

BLD-S and TAP-S each project to exactly 3 components (`WARM_UP, MAIN_SET, COOL_DOWN`); neither contains a `Recovery`-typed component. Recovery exists solely as `MAIN_SET.RecoveryQuantity`/`RecoveryMode`/`RecoveryPlacement`, with `RecoveryCount` derived downstream by `PrescriptionRecoveryCardinality.Derive`, never authored in source. Verified by `BldS_ProjectsExactlyThreeComponents_WithNineRecoveries_NoStructuralRecovery`, `TapS_ProjectsExactlyThreeComponents_WithFiveRecoveries_NoStructuralRecovery`, and `SourceProfiles_NeverAuthorRawRecoveryCount`.

## 21. Typed intensity

All main-set descriptors are production vocabulary matching the frozen FREQ.6D.4B/4B.3 matrix; all support-component descriptors are `EASY`. No `PROBE_*` or placeholder string appears anywhere in the eight real source documents (verified by direct inspection and by every validator/projector pass succeeding with real descriptors).

## 22. Accounting

All eight: `DistanceAccountingMode = ESTIMATED_SESSION_TOTAL`. No component-level distance arithmetic or duration→km conversion was introduced; the projector preserves the enum and each component/derived-recovery independently, exactly as FREQ.6D.4B.1/4B.3 established.

## 23. Capability overlay

RS-P (`GOAL_PACE_TEN_K v2`) fails closed with `PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED` when validated without the overlay, and validates cleanly when the real `catalog/capability-overlays/goal-pace-ten-k-v2-distance-accounting-capability.v1.json` overlay (authored in FREQ.6D.4C.2) is supplied — verified by `RsP_ValidatesOnlyThroughTheRealCapabilityOverlay_NoOverlayFailsClosed`. No historical `WorkoutDefinition` mutation; no null-as-wildcard.

## 24. Graph validation

`CatalogGraphValidator.Validate(realSnapshot).IsValid == true` (`RealCatalog_PassesFullGraphValidation`), covering: unique profile key/version; exact `WorkoutDefinition` reference existence; component skeleton exactness; capability-metadata resolution; typed-intensity validity; accounting-mode allowance. A synthetic duplicate profile is confirmed rejected (`DuplicateProfileKeyVersion_IsRejectedByGraphValidation`, `..._ExactSameDocumentTwice`).

## 25. Lane-dose validation

Each of the 8 profiles satisfies `PrescriptionProfileLaneDoseValidator` for its own dose category's lane ordinal (0=Primary, 1=SecondaryControlled) and correctly fails `PROFILE_LANE_DOSE_CATEGORY_MISMATCH` for the wrong lane — verified by `EachSlot_SatisfiesItsFrozenLaneDoseMapping`. No lane-selection implementation was added.

## 26. 8-slot capacity

`RealCatalogCapacityMatrix_AllEightSlotsReady`: all 8 slots (FND-P, FND-S, BLD-P, BLD-S, RS-P, RS-S, TAP-P, TAP-S) resolve their real production source, real referenced `WorkoutDefinition`, real capability overlay where applicable, and validate cleanly. **8/8 READY**, using real production profile sources, not test fixtures.

## 27. Real execution projection

All 8 real profiles project via `WorkoutPrescriptionExecutionProjector` (against a stamped snapshot, real `ContentHash` on both profile and workout) with exact `ContractSchemaVersion=1`, correct `SourceProfile`/`SourceWorkout` key/version, correct `DoseCategory`, `DistanceAccountingMode`, 3-component sequence, `StructureMode`, `WorkQuantity`, `RepetitionCount` where applicable, nested recovery, `RecoveryPlacement`, derived `RecoveryCount`, `RecoveryMode`, and typed `IntensityTarget` — verified by `EachSlot_ProjectsLosslesslyAndPassesBoundaryValidation` (8 cases).

## 28. Boundary validation

`ExecutableWorkoutPrescriptionValidator.Validate(executable)` returns empty (zero issues) for all 8 real projected outputs. No post-projection normalization occurs anywhere in the pipeline.

## 29. Hash/provenance

All 8 real profiles receive a non-null, non-manual `ContentHash` from `CatalogStamper.StampAsPublished`. Mutating a load-bearing field (BLD-S main-set `RepetitionCount`) changes the computed hash — verified by `ProfileContentHashes_AreCanonicalAndChangeWithLoadBearingContent`. Historical `WorkoutDefinition` hashes are unaffected (no historical source file was touched).

## 30. Legacy containment

`LegacyCatalog_ExecutionPrescriptionsRemainNull_DespiteRealProfilesNowExisting` proves, against the real stamped catalog snapshot with all 8 new profiles present, that assembling the real `TEN_K__4D__INTERMEDIATE v10` combination bundle (both with no `executionDependencies` argument and with an explicit empty list) still yields `ExecutionPrescriptions == null`. `CatalogBundleAssembler.Assemble` only ever populates `ExecutionPrescriptions` from an explicitly-supplied dependency list — mere source-profile existence cannot auto-wire into any bundle. Intermediate×3D, ×4D and Beginner×4D are architecturally unaffected; no RunningApp, persistence, adaptation, calendar or TrainingDay file was touched.

## 31. Full PlanCatalog tests

`dotnet test tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1460 passed, 0 failed, 0 skipped, 1460 total** (1391 FREQ.6D.4C.2 baseline + 65 net new: 64 from the new production-profile source test file, +1 from splitting/renaming the retired FREQ.6D.4C zero-profile assertion, −0 net removed elsewhere).

## 32. Build

`dotnet build PlanCatalog.sln`: 0 warnings, 0 errors. `dotnet build PlanCatalog.sln -c Release`: 0 warnings, 0 errors.

## 33. File attribution

| Category | Files |
|---|---|
| `PRESCRIPTION_PROFILE_SOURCE` | 8 new files under `catalog/prescription-profiles/` |
| `CATALOG_GENERATED_AUDIT` | `artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` (timestamp-only regeneration, verified) |
| `TEST` | `Intermediate5DProductionPrescriptionProfileSourceTests.cs` (new); `Intermediate5DProductionPrescriptionCatalogTests.cs`, `FileSystemCatalogSourceRepositoryTests.cs` (retired now-stale zero-profile assertions) |
| `DOCUMENTATION` | this report |
| `LEDGER` / `ROADMAP` | `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` |
| `UNEXPECTED` | none |

No generic PlanCatalog implementation code changed — schema/loader/validator/projector/Contracts are all byte-identical to the FREQ.6D.4B.4 baseline, confirming the phase's own §26 expectation of zero infrastructure delta.

## 34. Lifecycle blocker

`CATALOG_LIFECYCLE_BLOCKER_REMAINS_OPEN_BEFORE_6D4D`. Four of the eight referenced `WorkoutDefinition` versions (`AEROBIC_STRENGTH_CONTROLLED_INTRO v3`, `THRESHOLD_TEMPO v5`, `FARTLEK v5`, `GOAL_PACE_TEN_K v3`) remain `DRAFT`; none was promoted. This phase's own real profile-source authoring did not require promotion — the loader, validator, capability overlay and projector all operate correctly against `DRAFT` `WorkoutDefinition` sources for authoring/graph-validation purposes; only real publication would require it, and no publication occurred.

## 35. Exact next required phase capability/type

The lifecycle/resolver closure: a phase resolving safe `DRAFT → VALIDATED` activation for the four affected `WorkoutDefinition` versions without the legacy highest-non-retired resolver (`CatalogSourceSnapshot.FindWorkout(string key, IRetirementLedger?)`) silently changing current Intermediate×3D/×4D/Beginner×4D behavior. Type: most likely `ARCHITECTURE_DESIGN` (evaluate options) followed by `IMPLEMENTATION`, per this engagement's established pattern — not fabricated as a Phase ID here; repository roadmap/governance will assign it when actually opened. `FREQ.6D.4D` (dual-lane engineering) remains blocked until that closure.

## 36. Implementation SHA

`e7a6c07`.

## 37. Final classification

**`FREQ6D4C3_PROFILES_AUTHORED_CATALOG_LIFECYCLE_BLOCKER_REMAINS`**

All 8 real production `WorkoutPrescriptionProfile` documents authored with complete, exact D1-D18 + FC1-FC10 athlete-facing fidelity; 8/8 real catalog capacity `READY`; all eight validate, project losslessly and pass boundary validation; zero structural `RECOVERY` duplication; zero infrastructure/engineering delta; legacy 3D/4D/Beginner bundles architecturally unaffected. The known, pre-existing catalog-lifecycle (`DRAFT→VALIDATED`) blocker was neither solved nor newly discovered by this phase — it remains exactly as disclosed by FREQ.6D.4B.4, and must close before `FREQ.6D.4D`.
