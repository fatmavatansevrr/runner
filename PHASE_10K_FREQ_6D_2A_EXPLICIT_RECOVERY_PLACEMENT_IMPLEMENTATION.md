# PHASE 10K-FREQ.6D.2A — Explicit Recovery Placement Implementation

## 1. Parent SHAs

- Domain policy: `378e2eb` (`docs(freq-6d): close recovery cardinality policy`)
- Prior prescription contract: `bb35747e08e5061c67e90f6bd31eed31c384be15`
- Frozen P1 + P3 architecture: `37a05178975382f029041e8de7fc67297c5de196`

## 2. Files inspected

The profile model, prescription enums, validator, JSON schema, canonical JSON options, schema tests, validator tests, catalog loader/snapshot, schema-version validators, repository profile sources, and relevant legacy catalogs were inspected.

## 3. Files changed

- `PlanCatalog.Core/Enums/PrescriptionRecoveryPlacement.cs`
- `PlanCatalog.Core/Models/WorkoutPrescriptionProfile.cs`
- `PlanCatalog.Core/Models/PrescriptionRecoveryCardinality.cs`
- `PlanCatalog.Core/Validation/WorkoutPrescriptionProfileValidator.cs`
- `schemas/workout-prescription-profile.schema.json`
- profile validator and schema tests
- this implementation report

## 4. RecoveryPlacement type

`PrescriptionRecoveryPlacement` is an authoring-domain enum with exactly `BetweenRepetitions` and `AfterEachRepetition`. Canonical JSON renders these as `BETWEEN_REPETITIONS` and `AFTER_EACH_REPETITION`. `PrescriptionProfileComponent.RecoveryPlacement` is nullable so the typed validator can distinguish omission from an authored value; valid repeated components require it.

## 5. Schema changes

The component schema adds `recoveryPlacement` with exactly the two approved tokens. REPEATED requires it together with `recoveryQuantity`; CONTINUOUS forbids both. No schema default exists. Closed `additionalProperties` rejects `recoveryCount` and speculative placement fields/values.

## 6. Repeated rules

A valid repeated component has exactly one positive work unit, `repetitionCount >= 2`, exactly one positive recovery unit, valid recovery mode, and explicit valid placement. Missing placement is invalid and never normalized to BETWEEN.

## 7. Continuous rules

CONTINUOUS forbids `repetitionCount`, `recoveryQuantity`, and `recoveryPlacement`. Placement without recovery is independently invalid in typed validation.

## 8. Derivation authority

`PrescriptionRecoveryCardinality.Derive` in Core is the single reusable semantic authority. BETWEEN returns `N-1`; AFTER_EACH returns `N`; repetition counts below two and undefined enum values fail closed. No arithmetic was added to RunningApp or an execution contract.

## 9. Validation errors

- `PROFILE_REPEATED_RECOVERY_PLACEMENT_REQUIRED`
- `PROFILE_CONTINUOUS_RECOVERY_PLACEMENT_FORBIDDEN`
- `PROFILE_RECOVERY_PLACEMENT_INVALID`
- `PROFILE_RECOVERY_PLACEMENT_WITHOUT_RECOVERY`

Existing recovery/count/unit error codes remain intact.

## 10. Fixture/content audit

All repeated FREQ.6D.2 fixtures are test-only representability fixtures, not production catalog authority. Intervalized THRESHOLD and structured FARTLEK explicitly choose BETWEEN as fixture intent. No inference from workout family was introduced. Repository search found no committed source/production `WorkoutPrescriptionProfile` documents: **`NO_PRODUCTION_PROFILE_MIGRATION_REQUIRED`**.

## 11. Schema-version decision

This is an additive-but-required completion of authoring schema v1. The prior v1 contract has no committed profile content, published artifacts, or `prescription-profiles` source directory to preserve. A schema v2 would manufacture a legacy shape that never became authored catalog data. The v1 validator remains the supported version and is now complete under the authoritative policy. There is no missing-field compatibility fallback.

## 12. Profile-version decision

No real profile exists, so no document version is mutated or created. Future executable semantic changes to an authored/published profile require a new profile document version under existing immutability rules.

## 13. Backward compatibility

Legacy catalogs without a profile directory load unchanged. Intermediate×3D, Intermediate×4D, and Beginner×4D legacy repetition payloads are not reinterpreted as prescription profiles. The stricter rule applies only to `WorkoutPrescriptionProfile` authoring.

## 14. Distance example

For four repetitions, derivation tests prove recovery counts 3 (BETWEEN) and 4 (AFTER_EACH). Consequently `4×1000 + 3×400 = 5200 m` and `4×1000 + 4×400 = 5600 m`. No new distance-accounting helper was added because this phase owns cardinality, not session accounting.

## 15. Duration example

For six 60-second repetitions with 60-second recovery, derivation returns 5 for BETWEEN and 6 for AFTER_EACH. No duration-to-distance conversion is performed.

## 16. FARTLEK/THRESHOLD tests

Structured FARTLEK and intervalized THRESHOLD remain representable and now explicitly author placement in their test fixture builder.

## 17. Taper compatibility

The same Core authority derives six BETWEEN repetitions to five recoveries and a reduced four-repetition taper form to three. No taper-specific rule or progression was added.

## 18. Serialization round-trip

Both placements serialize to their exact upper-snake-case tokens and deserialize to the same enum values. Missing values are neither emitted nor inserted; repeated omission fails validation/schema.

## 19. Targeted tests

`dotnet test ... --filter FullyQualifiedName~WorkoutPrescriptionProfile --no-restore`: **43 passed, 0 failed, 0 skipped**.

## 20. Full catalog regression

`dotnet test plan-catalog/PlanCatalog.sln --no-restore`: **1293 passed, 0 failed, 0 skipped**.

## 21. Build

`dotnet build plan-catalog/PlanCatalog.sln --no-restore`: **succeeded, 0 warnings, 0 errors**.

## 22. File attribution

| File | Attribution |
|---|---|
| `PrescriptionRecoveryPlacement.cs` | ENUM |
| `WorkoutPrescriptionProfile.cs` | AUTHORING_MODEL |
| `PrescriptionRecoveryCardinality.cs` | AUTHORING_MODEL |
| `WorkoutPrescriptionProfileValidator.cs` | VALIDATOR |
| `workout-prescription-profile.schema.json` | SCHEMA |
| `WorkoutPrescriptionProfileValidatorTests.cs` | TEST / SERIALIZATION |
| `WorkoutPrescriptionProfileSchemaTests.cs` | TEST |
| this report | DOCUMENTATION |

No unexpected file is attributed to this phase.

## 23. 6D.3B contract

6D.3B may rely on a real typed placement, explicit placement on every valid repeated profile, exactly two values, no raw authoring count, exact author intent, frozen derivation, no placement on continuous profiles, and unchanged legacy non-profile catalogs. It may define immutable execution `RecoveryPlacement + RecoveryCount` without a product decision.

## 24. 6D.3C contract

6D.3C may call the single Core derivation authority: BETWEEN → `N-1`, AFTER_EACH → `N`. Missing or unsupported placement fails closed. No default, content selection, or legacy inference is permitted.

## 25. Commit SHA

Authoritative implementation commit: `491020605d6dcc1f166d5ed8072bfbe517c6864e` (`feat(catalog): add explicit recovery placement semantics`). This report is committed separately so the implementation commit can be referenced immutably.

## 26. Final classification

**`FREQ6D2A_EXPLICIT_RECOVERY_PLACEMENT_IMPLEMENTED`**

Containment check: no changes were made to `PlanCatalog.Contracts`, Infrastructure projector/bundle integration, RunningApp, `CatalogWorkoutBinder`, generated payloads, APIs, RunLayout, lane/keyOrdinal, progression, persistence, or Adaptation.
