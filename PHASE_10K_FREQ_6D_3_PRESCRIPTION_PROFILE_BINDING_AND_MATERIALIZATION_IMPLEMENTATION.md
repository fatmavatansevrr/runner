# PHASE 10K-FREQ.6D.3 — Prescription Profile Binding and Materialization Assessment

## 1. Parent SHA

`bb35747e08e5061c67e90f6bd31eed31c384be15` — exact authoritative parent confirmed before inspection.

The pre-existing dirty `baseline_tmp` gitlink remained unrelated and untouched.

## 2. Files inspected

- FREQ.6D.1/1A/1B design authorities and the FREQ.6D.2 implementation report.
- `plan-catalog/src/PlanCatalog.Core/Models/WorkoutPrescriptionProfile.cs` and its enums/validators.
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Binding/CatalogWorkoutBinder.cs` and binding contracts.
- `CatalogSessionPrescriptionPlanner.cs` and `CatalogSessionPrescriptionContracts.cs`.
- `GeneratedCatalogPlanPayload.cs` and `CatalogPublicPreviewMaterializer.cs`.
- PlanCatalog and backend project references plus the PlanCatalog published-boundary architecture tests.

## 3. Files changed

Only this assessment/governance document. No binder, materializer, runtime model, shared contract, test, persistence, API or routing file changed.

## 4. Binding responsibility boundary

The intended generic boundary remains valid conceptually: selection supplies one exact WorkoutDefinition plus one exact WorkoutPrescriptionProfile; a generic materializer maps those validated inputs without selecting a lane, slot, week or candidate.

The current repository cannot implement that boundary without first resolving a Process A→B contract conflict. The phase therefore stops under the explicit instructions rather than creating an unapproved adapter or dependency.

## 5. Exact input/output contracts audited

Intended inputs:

- `PlanCatalog.Core.Models.WorkoutPrescriptionProfile` and its real FREQ.6D.2 component/quantity/intensity types;
- the exact referenced `WorkoutDefinition`.

Current candidate outputs:

- Internal `CatalogPrescriptionSegment` carries order, component string, free-text intensity, one distance, optional duration, pace object and accounting flag.
- Public/internal-generation `GeneratedCatalogWorkoutSegmentPayload` adds repetition count and one work distance/duration, but has only a string `RecoveryType`; it has no embedded recovery quantity/unit alongside repeated work and no typed pace/effort/heart-rate discriminator matching FREQ.6D.2.

Neither output faithfully represents the complete input.

## 6. WorkoutDefinition/Profile composition

FREQ.6D.2 correctly keeps WorkoutDefinition skeleton authority separate from Profile executable-dose authority. No implementation attempted to reinterpret `WorkoutDefinition.intensityDescriptor`, synthesize a profile, or make legacy data a fallback for an explicitly selected profile.

## 7. Continuous materialization

The existing internal segment could carry either a duration or distance for a continuous component, but it would reduce typed intensity modes to existing pace/free-text shapes. Lossless continuous materialization is therefore only partially supported and was not implemented in isolation because the common output contract must also support repeated components.

## 8. Repeated materialization

Blocked. `CatalogPrescriptionSegment` has no repetition count, per-repetition quantity contract, embedded recovery quantity or recovery mode. `GeneratedCatalogWorkoutSegmentPayload` retains repetition count but cannot preserve an embedded recovery duration/distance and mode on the same repeated component.

Consequently `4 × 1000m + 400m jog` cannot be represented losslessly and distinctly from continuous/flattened quantities by the canonical current runtime pipeline.

## 9. FARTLEK proof

Blocked by the repeated-output gap. The approved structured `6 × 60s + 60s jog/float` profile would lose embedded recovery quantity/mode or require an unapproved transformation into additional runtime segments that no design authority currently defines.

## 10. THRESHOLD proof

Continuous threshold is partially shape-compatible. Intervalized/cruise threshold is blocked by the same repeated/recovery gap. Implementing only continuous threshold would leave one generic profile contract with inconsistent runtime support and would not meet this phase’s objective.

## 11. Taper proof

A smaller continuous quantity could be carried, but typed intensity and generic repeated sharpening remain incomplete. No taper-specific branch was introduced; doing so would violate the genericity and no-selection requirements.

## 12. Intensity handling

FREQ.6D.2 defines explicit `PaceBased`, `EffortBased` and `HeartRateBased` modes with exactly one matching descriptor. Current `CatalogPrescriptionSegment` carries a `CatalogPacePrescription` plus a free-text `IntensityDescriptor`; it has no lossless generic representation of all three profile modes. Normalizing them to strings is explicitly forbidden.

## 13. Distance-accounting handling

The profile-level DistanceAccountingMode could be carried by `CatalogWorkoutPrescription`, but current segment output cannot preserve the work-versus-recovery quantity distinction required for later canonical total accounting. Computing or flattening totals here would duplicate numeric authority, so it was not attempted.

## 14. Legacy compatibility

No legacy path changed. Existing 3D, 4D and Beginner 4D binding/materialization remains byte-for-byte code-identical. No profile is attempted when no explicit profile reference exists, and no broken explicit profile can fall back because no new path was installed.

## 15. Failure semantics

FREQ.6D.2 already supplies exact profile lookup and source validation failures. Runtime failures requested by this phase cannot be added truthfully until a runtime boundary/output contract exists. Adding exceptions around a non-existent materializer would be dead scaffolding and was rejected.

## 16. Tests

No new materialization tests were fabricated because there is no approved lossless output contract to assert against. No production/test code changed, so focused/full suites were not rerun solely for this blocked documentation checkpoint.

Read-only evidence used real source contracts and project dependency declarations. `git diff --check` is the applicable hygiene gate.

## 17. Regression

No behavioral regression is possible from this phase document. The authoritative parent already records FREQ.6D.2 as 1278/1278 PlanCatalog tests and a zero-warning build. This phase does not claim a new backend or full-catalog regression run.

## 18. File attribution

| File | Classification |
|---|---|
| `PHASE_10K_FREQ_6D_3_PRESCRIPTION_PROFILE_BINDING_AND_MATERIALIZATION_IMPLEMENTATION.md` | GOVERNANCE / BLOCKER DOCUMENTATION |

No `UNEXPECTED` file is included.

## 19. Required architecture decision before 6D.4

The repository needs one approved Process A→B projection strategy that remains a one-way executable output rather than a second authoring authority. At minimum it must decide:

1. How `RunningApp.Application` consumes exact validated profiles without referencing the authoring-only `PlanCatalog.Core` assembly. Current project boundaries prohibit direct consumption of the “real FREQ.6D.2 types.”
2. Which canonical internal runtime type is extended/replaced to carry, losslessly: structure mode, repetition count, per-repetition duration/distance, recovery duration/distance/mode, typed intensity mode/descriptor and profile distance-accounting mode.
3. Whether that richer representation remains internal through 6D.4 or becomes a published/preview contract later. This phase’s strong default forbids casual public expansion.
4. How an embedded repeated-component recovery maps to downstream segments without changing its authored component skeleton or double-counting distance.

Rejected unilateral options:

- adding `RunningApp.Application → PlanCatalog.Core` dependency;
- moving authoring profile types into `PlanCatalog.Contracts` (the published-boundary architecture test explicitly rejects those types there);
- duplicating the profile DTO/enums in backend;
- flattening recovery/intensity into strings;
- creating a 5D/KEY2-specific binder;
- silently splitting embedded recovery into extra components without an approved mapping contract.

FREQ.6D.4 cannot rely on profile materialization yet. It may rely only on the unchanged FREQ.6D.2 source contract and this exact blocker inventory.

## 20. Commit SHA

The documentation-only blocker commit SHA is reported in the final response; a commit cannot embed its own final SHA.

## 21. Final classification

`FREQ6D3_BLOCKED_ON_ARCHITECTURE_DECISION`

Triggered stop conditions:

- runtime representation cannot express the profile losslessly without an approved boundary/output design;
- the real FREQ.6D.2 types cannot reach the current binder through existing project references;
- proceeding would require either a new cross-process dependency, duplicate DTO/model, or public/internal contract expansion;
- an explicit broken profile must never fall back to legacy behavior.
