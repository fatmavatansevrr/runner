# PHASE 10K-FREQ.6D.3C — Deterministic Execution Projection and Bundle Integration

## 1. Parent SHAs

- Current documented parent: `262641a6367a58fa8716b110083a57237fe0603f`
- 6D.3B contract: `36bb0250f8038a86f44e266ddfb335db9a9805d7`
- 6D.2A authoring: `491020605d6dcc1f166d5ed8072bfbe517c6864e`
- Recovery policy: `378e2eb`
- P1 + P3: `37a05178975382f029041e8de7fc67297c5de196`

## 2. Files inspected

Core profile/model/validator/cardinality and snapshot APIs; Contracts execution values/validator; artifact reference factory; canonical stamper, serializer and hasher; bundle assembler/publisher/interface; profile source availability; bundle/hash/architecture tests; and RunningApp coupling were inspected.

## 3. Files changed

- Infrastructure projector and exact-dependency seam
- bundle assembler additive projection overload
- canonical profile provenance stamping
- projector unit tests and synthetic bundle integration tests
- this report

## 4. Projection responsibility

`WorkoutPrescriptionExecutionProjector` is the single selection-free, side-effect-free Infrastructure compiler. It does not search, choose versions, evaluate eligibility, assign lanes/stages, or author data.

## 5. Exact projector input/output

Input is one exact `WorkoutPrescriptionProfile` plus its exact `WorkoutDefinition`. Provenance is constructed from their canonical metadata. Output is one validated `ExecutableWorkoutPrescription`. Exact key/version mismatch fails closed.

## 6. Explicit enum mappings

Structure, dose, recovery mode, placement, and intensity use exhaustive switch expressions. No casts, parsing, numeric coincidence, or name-based mapping exists. Shared Contracts component/accounting enums are copied directly. Unknown future Core values fail closed.

## 7. Continuous projection

Duration and distance map to explicit unit+value work quantities. Structure remains `Continuous`; repetition and recovery remain absent; intensity remains typed.

## 8. Repeated projection

Per-repetition work, repetition count, nested recovery quantity/unit/mode/placement/count, component identity, order, and typed intensity are preserved. Repeated `4×1000 m` cannot collapse into continuous `4000 m`.

## 9. Recovery derivation

The projector calls only `PrescriptionRecoveryCardinality.Derive`. Tests prove BETWEEN `4→3`, AFTER_EACH `4→4`, normal `6→5`, and reduced `4→3`. No duplicate N/N−1 production arithmetic was added.

## 10. Intensity mapping

Pace, effort, and heart-rate modes map explicitly with their exact validated descriptor. No calculation or semantic reinterpretation occurs.

## 11. Dose/accounting mapping

Primary and SecondaryControlled map one-to-one. `DistanceAccountingMode` is preserved without total calculation or unit conversion. No lane/KEY role is introduced.

## 12. Provenance generation

`CatalogArtifactReferences.ToRef` creates both exact references. `CatalogStamper` now canonically stamps profile hashes alongside other source documents. Profile, workout, and final bundle hashes remain three distinct authorities.

## 13. Boundary validation

Every projected output passes `ExecutableWorkoutPrescriptionValidator` before return. Invalid output aborts projection; it is never repaired.

## 14. Determinism

Identical exact inputs produce canonical-identical execution JSON. There are no timestamps, GUIDs, filesystem enumeration, or unordered collection dependencies.

## 15. Execution collection ordering

Bundle projections sort by `SourceProfile.DocumentType`, ordinal key, then exact version. Reversed dependency input produces identical serialized bundle and hash.

## 16. Exact profile dependency mechanism

`ExactPrescriptionProjectionDependency` carries only an exact `VersionedCatalogReference`. The concrete assembler overload resolves exactly that profile and its exact referenced workout from the supplied snapshot. It never scans all profiles or selects by family, dose, key-only, latest, lane, or 5D policy.

## 17. Bundle population semantics

No dependencies or an empty exact set preserves `ExecutionPrescriptions = null`. One or more exact dependencies projects every entry. Any resolution/projection/validation failure aborts the entire assembly; no partial bundle or legacy fallback is returned.

## 18. Null/legacy semantics

The existing interface/publisher path remains unchanged and passes no projection set. Production/historical bundles remain null/absent and retain canonical serialization/hash compatibility.

## 19. Failure semantics

Tests cover profile/workout mismatch, missing profile, missing workout, invalid reference type, unsupported structure/intensity/placement, invalid authoring state, invalid boundary output, duplicate exact dependency, and whole-bundle failure. Exact profile versions remain distinct entries.

## 20. Hash behavior

Projection values are included before the existing bundle hash is calculated. Reversed input order retains the hash; a repetition-dose change changes profile provenance, derived count, execution payload, and final bundle hash. No hash algorithm was added.

## 21. FARTLEK proof

A test-only structured FARTLEK profile retains four repetitions, 1000 m per-repetition work, 400 m nested jog, BETWEEN placement, recovery count three, and typed intensity. No production content was authored.

## 22. THRESHOLD proof

An intervalized THRESHOLD fixture projects through the same generic code and retains the same lossless fields. The projector has no workout-family branch.

## 23. Taper proof

Generic inputs with six and four BETWEEN repetitions produce five and three recoveries. There is no taper branch.

## 24. Architecture containment

Infrastructure continues to reference Core + Contracts. Contracts gains no Core dependency. RunningApp, binders, persistence, APIs, progression, Adaptation, and public preview are unchanged. Final diff contains no lane ordinal, keyOrdinal, KEY1/KEY2, phase pairing, stage allocation, compression, or extension logic.

## 25. Tests/regression

- Focused projector/integration: **26 passed, 0 failed, 0 skipped** after the final hash test was added.
- Combined projection/source/Contracts/boundary regression: **85 passed, 0 failed, 0 skipped**.
- Full PlanCatalog: **1338 passed, 0 failed, 0 skipped**.
- Build: **succeeded, 0 warnings, 0 errors**.
- `git diff --check`: clean.

## 26. Production profile-wiring status

No production graph currently supplies exact profile dependencies. The generic exact-reference seam and synthetic end-to-end publication proof are complete; current production bundles deliberately remain legacy/null. **`PROJECTION_CAPABILITY_IMPLEMENTED_PRODUCTION_PROFILE_WIRING_DEFERRED_TO_6D4`**.

## 27. File attribution

| File/group | Attribution |
|---|---|
| `WorkoutPrescriptionExecutionProjector.cs` | PROJECTOR |
| `ExactPrescriptionProjectionDependency.cs` | PROJECTION_INPUT / SEAM |
| `CatalogBundleAssembler.cs` | BUNDLE_ASSEMBLER |
| `CatalogStamper.cs` | PROVENANCE/HASH_INTEGRATION |
| projection/integration tests | TEST |
| this report | DOCUMENTATION |

No unexpected file is present.

## 28. 6D.3D input contract

RunningApp may rely on lossless, boundary-validated Contracts values with exact provenance, explicit structure/unit/repetitions, nested recovery placement and derived count, typed intensity, exact dose/accounting semantics, and hash coverage. Profile-backed publish failures are fail-closed; legacy bundles remain null. RunningApp must consume Contracts directly and must not resolve profiles or recalculate recovery count.

## 29. Commit SHA

Authoritative implementation commit: `573b7ac58c5677f27f2e729569f1e5c21cc06d4e` (`feat(catalog): project prescription profiles into published bundles`). This report is committed separately to reference the immutable implementation SHA.

## 30. Final classification

**`FREQ6D3C_PROJECTION_IMPLEMENTED_PROFILE_WIRING_DEFERRED`**

The complete generic projection capability exists; production lane/profile selection remains intentionally deferred to 6D.4.
