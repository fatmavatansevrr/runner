# PHASE 10K-FREQ.6D.2 — Prescription Profile Schema and Validation Implementation

## 1. Baseline SHA

`da7e82b132582ef7cfdd67b9837f9c43b02623bd` — exact expected parent at phase start.

Pre-existing `baseline_tmp` dirty gitlink remained unrelated and untouched.

## 2. Files inspected

- FREQ.6D.1, FREQ.6D.1A, FREQ.6D.1B and CP1 design authorities.
- Current workout/profile-adjacent schemas and `WorkoutDefinition` component model.
- `CatalogSourceSnapshot`, `FileSystemCatalogSourceRepository`, `SchemaFileMap` and canonical JSON options.
- `CatalogGraphValidator`, `PublishReadinessValidator`, existing workout/progression validators and validation error conventions.
- Plan Catalog schema, loading, serialization, graph, publishing and boundary tests.

## 3. Files changed

| Area | Files |
|---|---|
| NEW_SCHEMA | `plan-catalog/schemas/workout-prescription-profile.schema.json` |
| NEW_TYPED_CONTRACT | `PlanCatalog.Core/Models/WorkoutPrescriptionProfile.cs`; four `PlanCatalog.Core/Enums/Prescription*.cs` files |
| VALIDATOR | `WorkoutPrescriptionProfileValidator.cs`; `PrescriptionProfileLaneDoseValidator.cs`; `CatalogGraphValidator.cs` |
| DESERIALIZATION / REGISTRY | `DocumentTypes.cs`; `SchemaFileMap.cs`; `FileSystemCatalogSourceRepository.cs` |
| COMPATIBILITY / VERSIONING | `CatalogSourceSnapshot.cs`; `PublishReadinessValidator.cs` |
| TEST | `WorkoutPrescriptionProfileValidatorTests.cs`; `WorkoutPrescriptionProfileSchemaTests.cs`; `CatalogSnapshotBuilder.cs` |
| DOCUMENTATION | This file |

No runtime binder, progression allocator, persistence, adaptation, calendar repair, routing, combination or catalog data artifact changed.

## 4. Exact typed contract

`WorkoutPrescriptionProfile` contains:

- catalog `Metadata` identity and version;
- exact `VersionedCatalogReference WorkoutDefinitionRef`;
- `PrescriptionDoseCategory DoseCategory`;
- `DistanceAccountingMode DistanceAccountingMode`;
- ordered `PrescriptionProfileComponent` values.

Each component contains `SequenceOrder`, existing `WorkoutComponentType`, `PrescriptionStructureMode`, typed `PrescriptionWorkQuantity`, optional typed `PrescriptionRecoveryQuantity`, and `PrescriptionIntensityTarget`.

Work quantity supports explicit `DurationSeconds` or `DistanceMeters`, plus `RepetitionCount` only for repeated work. Recovery supports explicit seconds or meters and `Jog`, `Walk`, or `Stationary`. Intensity uses one mode-matched descriptor: pace, effort, or heart-rate zone.

The profile and its enums live in Core because they are authoring-time catalog contracts, not Process A→B published boundary DTOs.

## 5. Structure-mode implementation

- `Continuous`: exactly one positive work unit; no repetition count; no recovery quantity.
- `Repeated`: exactly one positive per-repetition work unit; `RepetitionCount >= 2`; exactly one positive recovery unit and a valid recovery mode.
- Unknown modes and contradictory units fail at JSON Schema/deserialization and are also defended by typed source validation.

## 6. Component-model relationship

`WorkoutDefinition.components` remains the sole workout identity/skeleton authority. A profile must match its referenced definition one-to-one by ordered `(SequenceOrder, ComponentType)`.

The profile is the sole executable dose authority for quantities, repetition, recovery and intensity. Existing `WorkoutDefinition.intensityDescriptor` remains non-executable descriptive identity metadata. A definition without a component skeleton cannot acquire a profile without a new WorkoutDefinition version.

Legacy and profile components are therefore not competing executable sources.

## 7. Dose-category implementation

The frozen Core enum contains only:

- `Primary`
- `SecondaryControlled`

No structural role was added. `KEY_SESSION` remains unchanged and RunLayout was not modified.

## 8. LaneOrdinal ↔ DoseCategory validator

`PrescriptionProfileLaneDoseValidator` is the single publish-time authority:

- lane 0 + `Primary`: pass;
- lane 1 + `SecondaryControlled`: pass;
- lane 0 + secondary: `PROFILE_LANE_DOSE_CATEGORY_MISMATCH`;
- lane 1 + primary: `PROFILE_LANE_DOSE_CATEGORY_MISMATCH`;
- any other ordinal: `PROFILE_LANE_ORDINAL_UNSUPPORTED`.

Runtime consumers may rely on successfully source-validated data; no runtime duplicate authority was introduced.

## 9. Version identity

Profile metadata supplies exact `(documentType, key, version)`. `WorkoutDefinitionRef` is an exact positive-version reference and is resolved only by `FindWorkout(key, version)`. `CatalogSourceSnapshot.FindPrescriptionProfile(key, version)` is exact-only; no latest/highest/nearest profile API exists.

## 10. Versioning behavior

- Workout identity/skeleton/eligibility change → new WorkoutDefinition version.
- Executable quantities, repetitions, recovery, intensity or dose category change → new PrescriptionProfile version/key.
- Future lane selection/rotation change → progression/pairing version.
- Published historical artifacts are not mutated.

## 11. Deserialization

`WORKOUT_PRESCRIPTION_PROFILE` is registered in `DocumentTypes`, `SchemaFileMap` and the source repository’s `prescription-profiles` folder. Canonical enum conversion uses existing UPPER_SNAKE_CASE JSON behavior. Schema version 1 is the only accepted version.

Unknown enums produce typed JSON deserialization failure. JSON Schema rejects missing/forbidden fields, unsupported schema versions, invalid units, non-positive quantities, invalid recovery modes and contradictory mode shapes.

## 12. Backward compatibility

Approved model A is implemented: historical artifacts have no PrescriptionProfile reference and retain legacy behavior. A missing `prescription-profiles` folder loads as an empty collection. No migration, normalization into synthetic profiles or retroactive reinterpretation occurs.

Intermediate × 3D, Intermediate × 4D and Beginner × 4D catalog paths therefore remain pinned to their existing artifacts.

## 13. Validation and failure semantics

Source graph validation adds exact, testable codes including:

- `PROFILE_SCHEMA_VERSION_UNSUPPORTED`
- `PROFILE_WORKOUT_REFERENCE_INVALID` / `PROFILE_WORKOUT_REFERENCE_NOT_FOUND`
- `PROFILE_COMPONENT_SKELETON_MISMATCH`
- `PROFILE_WORK_UNIT_AMBIGUOUS` / `PROFILE_WORK_QUANTITY_NON_POSITIVE`
- `PROFILE_REPEATED_COUNT_INVALID` / `PROFILE_REPEATED_RECOVERY_REQUIRED`
- `PROFILE_RECOVERY_UNIT_AMBIGUOUS` / `PROFILE_RECOVERY_MODE_INVALID`
- `PROFILE_INTENSITY_MODE_DESCRIPTOR_MISMATCH`
- `PROFILE_LANE_DOSE_CATEGORY_MISMATCH` / `PROFILE_LANE_ORDINAL_UNSUPPORTED`
- existing `GRAPH_DUPLICATE_KEY_VERSION` with the profile document type.

The profile’s distance-accounting and intensity modes must be allowed by the exact referenced WorkoutDefinition.

## 14. Tests

- New targeted tests: **28 passed**.
- They cover continuous threshold, intervalized threshold, structured controlled FARTLEK, reduced-dose Taper, exact version reference, duplicate identity/version, schema and enum failures, work/recovery unit failures, and all lane-dose cases.
- Existing focused schema/loader/workout/progression regression: **21 passed**.

## 15. Full catalog regression

- Full `PlanCatalog.Tests`: **1278 passed, 0 failed, 0 skipped**.
- `dotnet build plan-catalog/PlanCatalog.sln --no-restore`: **PASS, 0 warnings, 0 errors**.
- The full suite regenerated two tracked audit reports; they were identified as test side effects and restored to the baseline rather than attributed to this phase.
- Full backend regression was not claimed or run; no backend/shared runtime contract changed.

## 16. File attribution

All retained changes are categorized in section 3. There is no unexplained `UNEXPECTED` file. `baseline_tmp` and ignored `.claude/` remain outside the phase.

## 17. FREQ.6D.3 input contract

FREQ.6D.3 must consume these real types and must not create DTOs/stubs:

- `PlanCatalog.Core.Models.WorkoutPrescriptionProfile`
- `PrescriptionProfileComponent`
- `PrescriptionWorkQuantity`
- `PrescriptionRecoveryQuantity`
- `PrescriptionIntensityTarget`
- `PlanCatalog.Core.Enums.PrescriptionStructureMode`
- `PrescriptionDoseCategory`
- `PrescriptionRecoveryMode`
- `PrescriptionIntensityMode`
- `WorkoutPrescriptionProfileValidator`
- `PrescriptionProfileLaneDoseValidator`
- exact lookup `CatalogSourceSnapshot.FindPrescriptionProfile(string key, int version)`.

It may rely on exact WorkoutDefinition references, validated skeleton/mode compatibility, explicit units, schemaVersion 1, profile identity/version uniqueness, the lane-dose guarantee, and profile-optional legacy loading.

## 18. Commit SHA

The exact commit SHA is reported in the final phase report. A Git commit cannot embed its own final SHA without changing that SHA.

## 19. Final classification

`FREQ6D2_PRESCRIPTION_PROFILE_SCHEMA_AND_VALIDATION_IMPLEMENTED`
