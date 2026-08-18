# Phase 10K-FREQ.6D.4C.2 — Typed Prescription Compatibility Validation & Exact-Version Capability Overlay Implementation

**Implementation phase executing the FREQ.6D.4C.1-approved M4+M3 architecture. No product decision, no new prescription dosage, no profile authoring, no workout identity change, no historical `WorkoutDefinition` mutation, no DRAFT→VALIDATED promotion, no legacy resolver refactor, no dual-lane wiring, no RunningApp change, no persistence change, no public activation.**

## 1. Preflight

`PHASE_LEDGER.md` row 63: `FREQ.6D.4C.1` `DONE`/`VERIFIED`, final classification `FREQ6D4C1_ARCHITECTURE_APPROVED_WITH_CATALOG_LIFECYCLE_BLOCKER`. Re-verified from the real repository (not report prose): the 4 approved `WorkoutDefinition` version files exist, all `status: "DRAFT"`; zero `WORKOUT_PRESCRIPTION_PROFILE` documents exist anywhere in `plan-catalog/catalog/`; `dotnet build`/`dotnet test` green (1382 passed pre-change, matching the ledgered baseline plus the 37 new tests already drafted). `FREQ.6D.4C.2` confirmed not already ledgered. `MASTER_ROADMAP.md` §14 lists `FREQ.6D.4C.2` as the next near-term item with the exact scope executed here.

## 2. Scope executed

- **M4**: removed the `PROFILE_INTENSITY_MODE_NOT_ALLOWED` cross-check from `WorkoutPrescriptionProfileValidator` entirely. `AllowedPrescriptionModes` (legacy, session-level dosage descriptor; real values only `Distance`/`Mixed`) is no longer cross-checked against `PrescriptionIntensityMode` (typed, per-component targeting: `PaceBased`/`EffortBased`/`HeartRateBased`) — the two axes were never the same concept (`SEMANTIC_AXIS_MISMATCH`, per FREQ.6D.4C.1 §6). The internal, profile-only checks (`PROFILE_INTENSITY_MODE_INVALID`, `PROFILE_INTENSITY_MODE_DESCRIPTOR_MISMATCH`) are untouched and remain the sufficient safety invariant.
- **M3**: added `WorkoutDefinitionCapabilityOverlay`, a new, narrow, additive-only catalog document type keyed by exact `(WorkoutDefinition key, version)`. It fills a genuine absence in `AllowedDistanceAccountingModes` on an immutable historical version; it can never override explicit metadata (`explicit + overlay` present together fails closed as `Conflict`, never resolved by precedence). One real entry authored: `GOAL_PACE_TEN_K` v2 → `[ESTIMATED_SESSION_TOTAL]`.
- **Direct completion**: `GOAL_PACE_TEN_K` v3 (still `DRAFT`, not historical) had `allowedDistanceAccountingModes: ["ESTIMATED_SESSION_TOTAL"]` added directly to its own JSON — not via overlay, since only immutable/historical versions need the overlay mechanism.
- New `WorkoutCapabilityResolver.ResolveDistanceAccountingCapability(workout, overlay)` is the single canonical resolution seam, returning a source tag (`ExplicitWorkoutMetadata` / `CapabilityOverlay` / `Conflict` / `Unresolved`) consumed by the validator.
- The overlay artifact is wired through every layer of the existing generic catalog pipeline exactly like every other document type: `CatalogSourceSnapshot` (list + `FindCapabilityOverlay` exact lookup + `FindStatus`), `FileSystemCatalogSourceRepository` (loader), `CatalogGraphValidator` (duplicate-key-version + orphan-target + conflict checks), `PublishReadinessValidator` (metadata inclusion), `CatalogStamper` (hash/status stamping), `CatalogPublisher.ExcludeDraftArtifacts` (draft stripping). No generic scan-by-folder mechanism exists in this codebase, so a dedicated `capability-overlays` subfolder + one hardcoded `LoadAll<T>("capability-overlays")` call was added, matching the established per-document-type wiring convention.

## 3. Consumer audit of `AllowedPrescriptionModes` (required before touching it)

Every real consumer was classified before any change:

| Consumer | Classification | Action |
|---|---|---|
| `WorkoutPrescriptionProfileValidator`'s intensity-mode cross-check | `PROFILE_COMPAT_INVALID` (semantic-axis mismatch, per FREQ.6D.4C.1 §6) | Removed (M4) |
| `WorkoutDefinitionValidator`'s own internal check that `AllowedPrescriptionModes` is non-empty when present | `LEGACY_LEGITIMATE` | Untouched |
| `CatalogSessionPrescriptionPlanner.ModeFor` (legacy backend runtime, `RunningApp.Application`) | `LEGACY_LEGITIMATE` (first-class real consumer, confirms `MIXED` is not a wildcard) | Untouched — out of `plan-catalog` scope entirely |
| Schema (`workout-definition.schema.json`) enum declaration | `LEGACY_LEGITIMATE` | Untouched |

No other real call site references `AllowedPrescriptionModes`. Only the one invalid cross-check existed; it has been removed, nothing else.

## 4. Root-cause resolution mapping

| Root cause (FREQ.6D.4C.1) | Resolution this phase |
|---|---|
| Root cause 1 — missing `AllowedDistanceAccountingModes` on `GOAL_PACE_TEN_K` v2 (immutable) | `WorkoutDefinitionCapabilityOverlay` entry, M3 |
| Root cause 1 — same gap on `GOAL_PACE_TEN_K` v3 (still DRAFT) | Direct field completion on v3's own JSON |
| Root cause 2 — `SEMANTIC_AXIS_MISMATCH` blocking all 6 non-`GOAL_PACE_TEN_K` slots | Removal of the invalid cross-check, M4 |

## 5. Files touched — attribution taxonomy

| Category | Files |
|---|---|
| `CAPABILITY_MODEL` | `PlanCatalog.Core/Models/WorkoutDefinitionCapabilityOverlay.cs` (new) |
| `CAPABILITY_SCHEMA` | `schemas/workout-definition-capability-overlay.schema.json` (new) |
| `CAPABILITY_SOURCE` | `catalog/capability-overlays/goal-pace-ten-k-v2-distance-accounting-capability.v1.json` (new); `catalog/workouts/goal-pace-ten-k.v3.json` (edited — direct completion) |
| `CAPABILITY_LOADER` | `PlanCatalog.Infrastructure/Repositories/FileSystemCatalogSourceRepository.cs`; `PlanCatalog.Core/Catalog/CatalogSourceSnapshot.cs`; `PlanCatalog.Contracts/DocumentTypes.cs`; `PlanCatalog.Infrastructure/Schema/SchemaFileMap.cs`; `PlanCatalog.Infrastructure/Publishing/CatalogStamper.cs`; `PlanCatalog.Infrastructure/Publishing/CatalogPublisher.cs` |
| `CAPABILITY_RESOLVER` | `PlanCatalog.Core/Validation/WorkoutCapabilityResolver.cs` (new) |
| `PROFILE_VALIDATOR` | `PlanCatalog.Core/Validation/WorkoutPrescriptionProfileValidator.cs` |
| `GRAPH_VALIDATION` | `PlanCatalog.Core/Validation/CatalogGraphValidator.cs`; `PlanCatalog.Core/Validation/PublishReadinessValidator.cs` |
| `TEST` | `PlanCatalog.Tests/Validation/PrescriptionCapabilityMetadataOverlayTests.cs` (new, 37 tests); `PlanCatalog.Tests/Validation/Intermediate5DProductionPrescriptionCatalogTests.cs` (edited — 8 assertions updated to reflect the resolved blockers, see §8) |
| `DOCUMENTATION` | `artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` (auto-regenerated by the audit tool during the test run — real, mechanical reflection of the two new source files, not hand-edited) |
| `LEDGER` / `ROADMAP` | `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` (this phase's own governance commit) |
| `UNEXPECTED` | None. No `backend/RunningApp.*` file, no persistence/migration file, and no file outside `plan-catalog/` or the two root governance files appears in the diff. |

No STOP condition triggered.

## 6. Fail-closed conflict semantics — real proof

`WorkoutCapabilityResolver` never resolves an explicit-metadata-plus-overlay collision by precedence. If a future `WorkoutDefinition` version ever declared `AllowedDistanceAccountingModes` explicitly **and** a capability overlay targeted the same exact `(key, version)`, resolution returns `Conflict`, and the validator raises `PROFILE_CAPABILITY_OVERLAY_CONFLICTS_WITH_EXPLICIT_METADATA` regardless of profile content — proven by `PrescriptionCapabilityMetadataOverlayTests.ExplicitMetadataPlusOverlay_IsConflict_NeverSilentlyResolvedByPrecedence`. Overlay targeting a non-existent `(key, version)` (`GRAPH_CAPABILITY_OVERLAY_TARGET_NOT_FOUND`) and duplicate overlays targeting the same exact version (`GRAPH_CAPABILITY_OVERLAY_DUPLICATE_TARGET`) both fail closed at the graph-validation layer.

## 7. 8-slot representability + lossless-projection proof

`PrescriptionCapabilityMetadataOverlayTests.EightSlots()` reconstructs the exact FREQ.6D.4B frozen matrix (structure/work/recovery/intensity-mode/descriptor/dose exactly as decided) for all 8 slots, referencing the exact `(WorkoutDefinition key, version)` FREQ.6D.4B approved — including the 3 slots pinned to immutable `THRESHOLD_TEMPO`/`FARTLEK` v4 and the 1 pinned to immutable `GOAL_PACE_TEN_K` v2. Two theories run against all 8: (1) `WorkoutPrescriptionProfileValidator.Validate` returns `IsValid == true` with the real capability overlay supplied where applicable; (2) `WorkoutPrescriptionExecutionProjector.Project` succeeds and produces the exact expected `ExecutableWorkoutPrescription` (work/recovery quantities, derived `RecoveryCount`) with stamped content hashes. All 16 pass. This is a direct, real proof that the approved policy is now fully representable under the current architecture, with zero `WorkoutDefinition` reference changes.

## 8. Reconciling FREQ.6D.4C's blocker-proof tests

`Intermediate5DProductionPrescriptionCatalogTests.cs` (authored by FREQ.6D.4C specifically to *prove* the two blockers) contained assertions that now correctly fail once the blockers are resolved — this is the intended, expected consequence of the fix, not a regression. Updated in place, preserving all still-relevant coverage (family/skeleton/eligibility-diff invariants) and only changing assertions whose premise was the now-resolved defect:

- `MixedOnlyAllowedPrescriptionModes_BlocksEveryTypedIntensityMode` → renamed `..._NoLongerBlocksTypedIntensityModes`; asserts `DoesNotContain PROFILE_INTENSITY_MODE_NOT_ALLOWED` instead of `Contains`.
- `GoalPaceTenK_V3_OnlyAddsTaperEligibility` → renamed `..._AddsTaperEligibilityAndDistanceAccountingCapability`; the `AllowedDistanceAccountingModes` equality assertion (previously "both null") is replaced with an explicit assertion that v2 remains null and v3 is now `[ESTIMATED_SESSION_TOTAL]`, matching §2's direct-completion decision. All other eligibility-only-diff assertions (family, complexity tier, prescription modes, component skeleton, eligible-phases delta) are preserved unchanged.
- `GoalPaceTenK_MissingAllowedDistanceAccountingModes_BlocksAnyProfile_RS_P_TAP_P` → split into two tests: `GoalPaceTenK_V2_MissingAllowedDistanceAccountingModes_StillBlocksWithoutOverlay_ButResolvedByCapabilityOverlay` (proves v2 still fails closed with no overlay supplied, and is resolved for the approved mode only when the real overlay is supplied) and `GoalPaceTenK_V3_NoLongerNeedsOverlay_DirectlyCompletedSinceStillDraft` (proves v3 needs no overlay at all).
- `ButForAllowedPrescriptionModes_TheFrozenFndPPrescriptionWouldValidateAndProjectLosslessly` → renamed `TheFrozenFndPPrescription_ValidatesAndProjectsLosslesslyAgainstTheRealWorkout`; the hypothetical `with`-isolated single-field-fix workout is removed entirely — the real, unmodified v3 `WorkoutDefinition` is used directly for both the validation and projection assertions, since the real fix (M4) now makes the hypothetical unnecessary.

## 9. Historical immutability & DRAFT-regression re-verification

Re-ran the exact tests that caught FREQ.6D.4C's real regression (`WorkoutArtifactImmutabilityTests`, `DependencyVersionCascadeTests`) — both still pass; no historical (`v1`-`v4`/`v2`) `WorkoutDefinition` content was mutated this phase. `PrescriptionCapabilityMetadataOverlayTests.FourNewWorkoutDefinitionVersions_RemainDraft` (new) directly re-confirms all 4 FREQ.6D.4C versioned extensions are still `status: "DRAFT"` — no promotion occurred.

## 10. Build & full regression

`dotnet build PlanCatalog.sln`: 0 warnings, 0 errors. `dotnet test tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1391 passed, 0 failed, 0 skipped, 1391 total** (1353 ledgered baseline + 37 new capability/overlay tests + 1 net test-count delta from splitting one old test into two, per §8). `git diff --check`: no conflict markers, only pre-existing CRLF-normalization warnings (no content issue).

## 11. Catalog-lifecycle blocker — status unchanged

The DRAFT→VALIDATED promotion-safety question against the legacy highest-non-retired `FindWorkout(key)` resolver remains **explicitly, deliberately open** — this phase implements capability *validation*, not catalog *publication* readiness, and does not touch or refactor that resolver. `CATALOG_LIFECYCLE_BLOCKER_REMAINS_OPEN_BEFORE_6D4D` still applies; it is a pre-existing, disclosed, not-newly-encountered condition (per FREQ.6D.4C.1's own explicit deferral), not a new discovery of this phase.

## 12. FREQ.6D.4C.3 input contract — satisfied

FREQ.6D.4C.3 (profile authoring, out of this phase's scope) can now proceed against a catalog where all 8 approved `WorkoutDefinition` references validate and project the exact FREQ.6D.4B content losslessly, proven in §7. No profile documents were authored into the real catalog this phase — `RealCatalog_HasNoPrescriptionProfilesAuthoredYet` (inherited from FREQ.6D.4C, still passing) confirms this remains true.

## 13. Final classification

`FREQ6D4C2_TYPED_COMPATIBILITY_AND_CAPABILITY_OVERLAY_IMPLEMENTED` — both blockers resolved with real, tested code; all 8 FREQ.6D.4B slots proven representable; catalog-lifecycle blocker remains explicitly open and undisturbed, as required.

## 14. Commits

- Implementation: `eb4896c` — `feat(catalog): add exact prescription capability metadata`
- Governance: recorded in `PHASE_LEDGER.md`/`MASTER_ROADMAP.md`, this phase's governance commit SHA backfilled in a small follow-up commit per established convention.

## 15. Push-gate check

Phases completed since the last push gate (`APPSEL-BACKEND.GOV.0`, pushed at `28b1097`): `FREQ.6D.3D`, `FREQ.6D.4`, `FREQ.6D.4A`, `FREQ.6D.4B`, `FREQ.6D.4C`, `FREQ.6D.4C.1`, `FREQ.6D.4C.2` = 7. `MASTER_ROADMAP.md` §10 Gate B threshold is "approximately 10 completed phase prompts." 7 < 10 — threshold not yet reached. Classification: `PUSH_GATE_NOT_REACHED`.
