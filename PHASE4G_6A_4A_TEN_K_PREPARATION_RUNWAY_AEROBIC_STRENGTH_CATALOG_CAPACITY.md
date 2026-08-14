# Phase 4G.6A.4A — 10K Preparation Runway AerobicStrength Workout Definition, Schema Eligibility and Two-Week Catalog Progression

Scope: `TEN_K__4D__INTERMEDIATE v10` only. Catalog/schema capacity implementation. No allocator binding, no runway-week generation, no 15-20 week activation.

## 1. Executive Result

**`TEN_K_PREPARATION_RUNWAY_AEROBIC_STRENGTH_TWO_WEEK_CATALOG_CAPACITY_IMPLEMENTED_AND_VALIDATED`**

Two new versioned workout definitions, a workout-definition schema/enum extension for Preparation Runway eligibility, and a canonical two-step progression mapping were authored and validated. `TD-AEROBIC-STRENGTH-SEMANTICS-001` is **CLOSED**.

## 2. Inherited Blocker

`TD-AEROBIC-STRENGTH-SEMANTICS-001`, semantic portion resolved (Phase 4G.6A.2/4G.6A.2A), remaining reason `OPEN`: no catalog-executable workout, no runway-representing `eligiblePhases` value, no approved two-week progression mapping.

## 3. Pilot Scope

`TEN_K__4D__INTERMEDIATE v10`, `DaysPerWeek=4`, `RunningBackground=Intermediate`, `PreferredCoreWeeks=12`, Preparation Runway 3-8 full weeks, `AEROBIC_STRENGTH` eligible only in `CORE_ENTRY_READY`. No numeric workout content generalized to other distances/candidates.

## 4. Artifacts Inspected

Decision docs (4G.6A.2, 4G.6A.2A, 4G.6A.3, 4G.6A.3B); `plan-catalog/catalog/workouts/*.json` (all 18, re-confirmed zero hill/stride/interval/economy/controlled content via fresh grep); `workout-definition.schema.json`, `workout-progression.schema.json`, `plan-template.schema.json` (all re-read in full); `ten-k-workout-progression.v5.json`, `ten-k-master.v6.json`, `ten-k-4d-intermediate.v10.json`; `WorkoutDefinition.cs`/`PhaseKey.cs`/`WorkoutFamily.cs`/`WorkoutComponentType.cs` (C# models, via a dedicated investigation confirming strict enum deserialization); `WorkoutDefinitionValidator.cs` (confirming the no-duplicate-component-type rule); `FileSystemCatalogSourceRepository.cs`/`CatalogBundleAssembler.cs` (via a dedicated investigation confirming loader/bundle mechanics); `BundleWorkoutClosureTests.cs`, `PilotCatalogStructuralTests.cs`, `JsonSchemaValidatorTests.cs`, `FileSystemCatalogSourceRepositoryTests.cs` (test-convention precedent); `activation-readiness-risks.json`/`.md`.

## 5. Existing-Workout Reuse Audit

Re-confirmed, not assumed: `EASY_STANDARD`/`LONG_RUN_STANDARD` (no components, no stimulus content), `FARTLEK` (closest directional precedent, `eligiblePhases=[BUILD]` only, `SURGE_AND_FLOAT` content, not hill/stride-specific), `THRESHOLD_TEMPO` (forbidden intensity per the AerobicStrength semantic contract), `GOAL_PACE_TEN_K` (race-pace-gated, forbidden). **No existing workout is reusable as-is or by mapping alone** — Phase 4G.6A.2A's conclusion stands, confirmed by direct re-inspection, not reversed.

## 6. One-Definition versus Two-Definition Decision

**Decision: two distinct workout definitions** (`AEROBIC_STRENGTH_CONTROLLED_INTRO`, `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED`), not one definition with two progression steps. Reason: direct inspection of `WorkoutDefinitionValidator.cs` found `components` may not contain duplicate `componentType` values (`COMPONENT_SEQUENCE_INVALID`), and no numeric field (repetition count, duration, distance) exists anywhere in the workout-definition schema for any of the 18 existing workouts — every existing workout defers all quantitative prescription to runtime. A single workout definition therefore cannot structurally represent two different weekly doses; the schema's only mechanism for expressing distinct weekly content is two distinct documents. This is not a preference for "more files" — it is the schema's own structural requirement, confirmed by inspection, not assumption.

## 7. Week 1 Semantic Contract

`AEROBIC_STRENGTH_CONTROLLED_INTRO v1`: `family=QUALITY`, `eligiblePhases=[PREPARATION_RUNWAY]`, `allowedPrescriptionModes=[MIXED]`, `allowedDistanceAccountingModes=[ESTIMATED_SESSION_TOTAL]`, components `WARM_UP("EASY") → MAIN_SET("CONTROLLED_AEROBIC_POWER_INTRO") → COOL_DOWN("EASY")` — a single continuous controlled exposure, no interior recovery segment, matching the "introductory, lower quality dose" requirement.

## 8. Week 2 Semantic Contract

`AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1`: identical metadata shape, components `WARM_UP("EASY") → MAIN_SET("CONTROLLED_AEROBIC_POWER_PROGRESSED") → RECOVERY("EASY_JOG") → COOL_DOWN("EASY")` — adds an explicit `RECOVERY` segment, directly precedented by the existing `FARTLEK` (has `RECOVERY`) vs. `THRESHOLD_TEMPO` (explicitly forbids `RECOVERY`) distinction already present in the catalog for exactly this kind of format reason. This represents a genuine, schema-validated structural difference (not label-only), while deferring the exact numeric realization of "modest quality-volume increase" to runtime prescription, consistent with how every other workout in this catalog already defers all numeric content.

## 9. Numeric Prescription Provenance

No numeric interval distance, duration, repetition count, or intensity value is embedded in either new workout definition — direct inspection confirmed the workout-definition schema has **no field capable of holding such a value** for any workout, existing or new (`components` items carry only `sequenceOrder`, `componentType`, `intensityDescriptor`; none is numeric). The one genuine "numeric" decision made in this phase is structural, not data: the presence/absence of a `RECOVERY` component between Week 1 and Week 2. Provenance: `REUSED_CANONICAL_PATTERN` (directly modeling the existing `FARTLEK`-has-`RECOVERY` vs. `THRESHOLD_TEMPO`-forbids-`RECOVERY` distinction). No numeric field anywhere in either new file is classified `EVIDENCE_INFORMED_PRODUCT_DEFAULT` or otherwise, because none exists to classify. The exact quantitative prescription (actual reps/duration/distance) is explicitly deferred to Phase 4G.6A.4B's runtime binding — not invented here.

## 10. Intensity Boundary

`intensityDescriptor` values used: `EASY` (warm-up/cool-down, reused verbatim from `FARTLEK`), `CONTROLLED_AEROBIC_POWER_INTRO`/`CONTROLLED_AEROBIC_POWER_PROGRESSED` (new, free-text — the schema's `intensityDescriptor` field is unconstrained string, confirmed by direct schema inspection, so no enum extension was needed for this field), `EASY_JOG` (recovery, reused verbatim from `FARTLEK`). No new intensity **enum** value was added anywhere (none was needed — `componentType` remains the closed enum, `intensityDescriptor` was already free text). Both new workouts sit structurally above continuous `EASY` (an explicit `MAIN_SET` with controlled-power content exists, `EASY_STANDARD`/`LONG_RUN_STANDARD` have none) and below `THRESHOLD_TEMPO`/`GOAL_PACE_TEN_K` (their forbidden-content grep, section 14, found zero matches for threshold/maximal/all-out/goal-pace/target-time language in either new file).

## 11. Schema Eligibility Change

`plan-catalog/schemas/workout-definition.schema.json`: `eligiblePhases.items.enum` extended from `["FOUNDATION","BUILD","RACE_SPECIFIC","TAPER"]` to `[..., "PREPARATION_RUNWAY"]`. **Mechanism chosen: Option A** (a generic `PREPARATION_RUNWAY` phase value), not a candidate-specific `TEN_K_PREPARATION_RUNWAY` value, not `CORE_ENTRY_READY` (a runtime profile name, not a catalog eligibility concept), and not any RunwayWeeks/candidate-ID coupling — matching the task's own explicit preference and prohibition list. Because the real production loader deserializes `EligiblePhases` via a strict `JsonStringEnumConverter` with no lenient fallback (confirmed by a dedicated investigation of `CanonicalJsonOptions.cs`), the corresponding C# `PhaseKey` enum (`plan-catalog/src/PlanCatalog.Contracts/Enums/PhaseKey.cs`) was extended identically with `PreparationRunway` — required, not optional, since the loader eagerly deserializes every file in `catalog/workouts/` regardless of reference status. `plan-template.schema.json`'s own, separate `phases[].phaseKey` enum was **not** touched — it remains closed to the four Core phases, so no `PlanTemplate` can ever declare a Preparation Runway phase.

## 12. Workout Definition Details

| Field | INTRO | PROGRESSED |
|---|---|---|
| Key | `AEROBIC_STRENGTH_CONTROLLED_INTRO` | `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` |
| Version | 1 | 1 |
| family | QUALITY | QUALITY |
| eligiblePhases | [PREPARATION_RUNWAY] | [PREPARATION_RUNWAY] |
| allowedPrescriptionModes | [MIXED] | [MIXED] |
| allowedDistanceAccountingModes | [ESTIMATED_SESSION_TOTAL] | [ESTIMATED_SESSION_TOTAL] |
| components | WARM_UP, MAIN_SET, COOL_DOWN | WARM_UP, MAIN_SET, RECOVERY, COOL_DOWN |

Neither file was produced by copying `FARTLEK` and changing the ID — every field was reviewed and set independently (confirmed by the differing `eligiblePhases`, differing `intensityDescriptor` values, and INTRO's differing component count from `FARTLEK`'s own fixed 4-component shape).

## 13. Two-Week Progression Mapping

New, separate, dark document type `PREPARATION_RUNWAY_BLOCK_PROGRESSION` — schema `plan-catalog/schemas/preparation-runway-block-progression.schema.json`, instance `plan-catalog/catalog/preparation-runway-progressions/ten-k-aerobic-strength-progression.v1.json` (`TEN_K_AEROBIC_STRENGTH_PROGRESSION v1`). Two steps: `stepOrder=1 → AEROBIC_STRENGTH_STEP_1_INTRO → AEROBIC_STRENGTH_CONTROLLED_INTRO v1`; `stepOrder=2 → AEROBIC_STRENGTH_STEP_2_PROGRESSED → AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1`. Not built on `workout-progression.schema.json` (whose own `phaseKey` enum is also closed to the four Core phases and is the document type the real, live `CatalogWorkoutProgressionLoader` consumes) — a deliberately separate, new schema/document type, confirmed by direct source inspection to be outside the 10 fixed subfolders `FileSystemCatalogSourceRepository` reads, so it is fully inert to the real loader with zero registration required.

## 14. GeneralEndurance Comparison

Confirmed by direct test assertion: both new workouts are `family=QUALITY` with an explicit controlled-power `MAIN_SET`; GeneralEndurance-family content (`EASY_STANDARD`/`LONG_RUN_STANDARD`) is `family=EASY`/`LONG_RUN` with zero components. Structurally distinct, not merely another easy-volume week.

## 15. PreSpecificTransition Comparison

Confirmed by direct test assertion: both new workouts declare `eligiblePhases=[PREPARATION_RUNWAY]` only — never any of the four Core phases that `EASY_STANDARD`/`LONG_RUN_STANDARD` (Transition's own content, per Phase 4G.6A.2A) are eligible in. No eligibility-based mechanism could ever conflate the two.

## 16. Core Week 1 Comparison

Confirmed by direct inspection of the live `ten-k-workout-progression.v5.json`: `FOUNDATION_EASY_BASE` (Core Week 1's real stage) candidates only `EASY_STANDARD`. Neither new AerobicStrength key appears there, and neither new workout's `eligiblePhases` includes `FOUNDATION`. No duplication, no premature race-specific progression (neither new workout is race-specific at all).

## 17. Fartlek/Threshold/Goal-Pace Comparison

`FARTLEK`: closest directional precedent (both `QUALITY`, both feature a `MAIN_SET`+`RECOVERY` pattern in the PROGRESSED variant) but rejected for direct reuse (`BUILD`-only eligibility, `SURGE_AND_FLOAT` content mismatch) — not reused, its `RECOVERY`-component precedent was reused instead (section 9). `THRESHOLD_TEMPO`: rejected outright (forbidden threshold intensity); confirmed absent from both new files by direct grep. `GOAL_PACE_TEN_K`: rejected outright (race-pace-gated); confirmed absent by direct grep for goal-pace/target-finish-time language.

## 18. Version and Catalog-Reference Changes

New files only: 2 workout definitions (`catalog/workouts/`), 1 new schema (`schemas/`), 1 new progression-mapping document (`catalog/preparation-runway-progressions/`, new folder). `TEN_K_MASTER`, `TEN_K__4D__INTERMEDIATE`, and `TEN_K_WORKOUT_PROGRESSION_V1` were **not** modified or version-bumped — no catalog reference integrity requirement forced a bump, since the new content is deliberately unreferenced by any live document (confirmed by direct test: the new workout keys are absent from `ten-k-workout-progression.v5.json` and from the real, constructed `TEN_K__4D__INTERMEDIATE v10` published bundle).

## 19. TD Status

`TD-AEROBIC-STRENGTH-SEMANTICS-001`: **CLOSED**, all eight closure conditions satisfied (registry closureNote, full detail). No duplicate TD created.

## 20. Catalog Readiness Result

Catalog capacity for `AEROBIC_STRENGTH` is now complete: a valid, schema-conformant, semantically-correct two-week workout progression exists and is provably distinct from every adjacent block/phase. Catalog readiness does **not** imply runtime readiness — see section 21.

## 21. Runtime/Binding Classification

**No allocation-to-workout binding was implemented.** The new catalog content is fully dark: unreferenced by any live progression, absent from the real published bundle, and the new progression-mapping document is unreachable by the production loader. `PreparationRunwayBlockAllocationEngine` (Phase 4G.6A.3B) was not modified, not touched, and was not made aware of this new content in any way.

## 22. Deferred Phase 4G.6A.4B Work

Implement the runtime binder that consumes `PreparationRunwayBlockAllocationEngine`'s `AllocatedWeeks` for `AerobicStrength` (1 or 2) and selects `AEROBIC_STRENGTH_STEP_1_INTRO`-only or `AEROBIC_STRENGTH_STEP_1_INTRO` then `AEROBIC_STRENGTH_STEP_2_PROGRESSED` from `ten-k-aerobic-strength-progression.v1.json`, remaining dark/unwired to any live path per the same convention established by every prior phase in this series.

## 23. Explicit Non-Implementation Statement

This phase implemented **no** allocator modification, **no** allocator DI wiring, **no** allocation-to-workout binding, **no** generated Preparation Runway weeks, **no** generated workout instances, **no** date assignment, **no** volume/long-run/pace/race-pace calculation, **no** CoreEntryReadiness change, **no** preview/confirm behavior change, **no** persistence or migration, **no** 15-20 week activation, **no** General Endurance staged-plan work, **no** 21-52 or 53+ behavior, and **no** intermediate-distance projection. The approved allocation matrices, `MinWeeks`, `MaxWeeks`, threshold 5, weights, priorities, and canonical order are all unchanged.
