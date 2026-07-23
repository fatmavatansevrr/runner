# Phase 4F.6 Step A — Live v10 Workout Progression Catalog Audit

Read-only audit of the exact versioned catalog bundle resolved by `TEN_K__4D__INTERMEDIATE v10`. No production code, catalog JSON, schema, `evidence-log.json`, or canonical decision artifact was modified. Companion machine-readable file: `phase4f6-step-a-v10-catalog-audit.json`.

## Candidate identity

`TEMPLATE_COMBINATION TEN_K__4D__INTERMEDIATE v10`, `metadata.status = "DRAFT"` (`plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`). Confirmed reachable by tooling via `plan-catalog/tests/PlanCatalog.Tests/Validation/D13GoalPaceTenKResolutionTests.cs`'s `BuildBundle(10)`.

## Resolved dependency graph

```
combination:TEN_K__4D__INTERMEDIATE:v10 (DRAFT)
├── planTemplate:TEN_K_MASTER:v6 (DRAFT)
│   └── workoutProgression:TEN_K_WORKOUT_PROGRESSION_V1:v5 (DRAFT)
│       ├── workout:EASY_STANDARD:v4 (DRAFT)
│       ├── workout:FARTLEK:v4 (DRAFT)
│       ├── workout:THRESHOLD_TEMPO:v4 (DRAFT)
│       └── workout:GOAL_PACE_TEN_K:v2 (DRAFT)
├── runLayout:RUN_LAYOUT_4D:v2 (DRAFT)
├── levelModifier:INTERMEDIATE_MODIFIER:v6 (DRAFT)
│   ├── progressionModifier:INTERMEDIATE_PROGRESSION_MODIFIER_V1:v2 (DRAFT)
│   └── workout:LONG_RUN_STANDARD:v4 (DRAFT)  [eligible-list only, see below]
└── rulePack:APPSEL_RACE_PLAN_V1:v4 (DRAFT)
    ├── peakVolumeBandPolicy:PEAK_VOLUME_BANDS_V1:v3 (DRAFT)
    └── runtimeConditionValueRegistry:RUNTIME_CONDITION_VALUES_V1:v2 (DRAFT)
```

Every node's exact version was resolved by direct file reads, not inferred — no version was substituted for a newer one that exists elsewhere in the catalog (e.g. `TEN_K_MASTER` has v1–v6 on disk; v10 references exactly v6, not v5 or v1). No artifact's `metadata` object carries a `contentHash` (expected — content hashes are stamped only at publish time by tooling not yet run against this DRAFT candidate).

## Actual phase and stage table

All 7 stages, exactly as stored in `ten-k-workout-progression.v5.json`. JSON array order matches `relativeOrder` in every case.

| stageId (phase:stage) | order | candidates | min/max | compression/extension | requires | fallback |
|---|---|---|---|---|---|---|
| FOUNDATION:FOUNDATION_EASY_BASE | 1 | EASY_STANDARD v4 | 3/6 | COMPRESSIBLE/EXTENDABLE | — | — |
| BUILD:FARTLEK_INTRO | 1 | FARTLEK v4 | 1/2 | COMPRESSIBLE/EXTENDABLE | — | — |
| BUILD:THRESHOLD_INTRO | 2 | THRESHOLD_TEMPO v4 | 2/4 | COMPRESSIBLE/EXTENDABLE | — | — |
| RACE_SPECIFIC:TEN_K_SPECIFIC_INTRO | 1 | THRESHOLD_TEMPO v4 | 1/2 | COMPRESSIBLE/EXTENDABLE | — | — |
| RACE_SPECIFIC:GOAL_PACE_REHEARSAL | 2 | GOAL_PACE_TEN_K v2 | 1/2 | PROTECTED/FIXED_EXPOSURE | GOAL_FEASIBILITY_IN ∈ {REALISTIC,CHALLENGING} | CURRENT_FITNESS_SPECIFIC_REHEARSAL |
| RACE_SPECIFIC:CURRENT_FITNESS_SPECIFIC_REHEARSAL | 3 | THRESHOLD_TEMPO v4 | 1/1 | COMPRESSIBLE/EXTENDABLE | — | — |
| TAPER:TAPER_SHARPEN | 1 | EASY_STANDARD v4 | 1/2 | PROTECTED/FIXED_EXPOSURE | — | — |

Every field above is `fieldSource: ACTUAL_CATALOG` (verbatim from the resolved JSON) except where noted. Full per-field provenance objects (decisionId, evidenceIds, notes) are in the JSON artifact.

## Phase-level derived summary (DERIVED_FROM_CATALOG)

| Phase | Weeks | Stages | ΣMin | ΣMax | Candidate union | Conditional stages | Fallback stages |
|---|---|---|---|---|---|---|---|
| FOUNDATION | 3 | 1 | 3 | 6 | {EASY_STANDARD} | 0 | 0 |
| BUILD | 4 | 2 | 3 | 6 | {FARTLEK, THRESHOLD_TEMPO} | 0 | 0 |
| RACE_SPECIFIC | 4 | 3 | 3 | 5 | {THRESHOLD_TEMPO, GOAL_PACE_TEN_K} | 1 | 1 |
| TAPER | 1 | 1 | 1 | 2 | {EASY_STANDARD} | 0 | 0 |

No duplicate candidate keys within any single stage. THRESHOLD_TEMPO is reused across two distinct RACE_SPECIFIC stages (cross-stage reuse, not a validation violation). The one fallback (`GOAL_PACE_REHEARSAL → CURRENT_FITNESS_SPECIFIC_REHEARSAL`) stays within the same phase, is not circular, and its target has no `requires` of its own.

## Workout-definition table

| Key | v | family | complexityTier | eligiblePhases | prescriptionModes | referenced by | public type mapping |
|---|---|---|---|---|---|---|---|
| EASY_STANDARD | 4 | EASY | absent (schema-forbidden ≥v3) | FOUNDATION,BUILD,RACE_SPECIFIC,TAPER | DISTANCE | FOUNDATION_EASY_BASE, TAPER_SHARPEN | **MISSING** |
| LONG_RUN_STANDARD | 4 | LONG_RUN | absent | FOUNDATION,BUILD,RACE_SPECIFIC,TAPER | DISTANCE | **none** (eligible-list only) | **MISSING** |
| FARTLEK | 4 | QUALITY | absent | BUILD | MIXED | FARTLEK_INTRO | **MISSING** |
| THRESHOLD_TEMPO | 4 | QUALITY | absent | BUILD,RACE_SPECIFIC | MIXED | THRESHOLD_INTRO, TEN_K_SPECIFIC_INTRO, CURRENT_FITNESS_SPECIFIC_REHEARSAL | **MISSING** |
| GOAL_PACE_TEN_K | 2 | QUALITY | 2 (EXPLICIT_PRODUCT_DEFAULT) | RACE_SPECIFIC | PACE_BASED | GOAL_PACE_REHEARSAL | **MISSING** |

**Public workout-type mapping — verified, not assumed.** `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogDomainMapper.cs::ClassifyWorkoutKeys` already classifies all 5 keys as `BackendRepresentationSupport.RequiresNewField`, explicitly stating each existing enum member (`TrainingDayType.Interval` for FARTLEK, `.Tempo` for THRESHOLD_TEMPO, etc.) is only a **"plausible loose family match"**, never an exact/confirmed identity mapping. `GeneratedCatalogWorkoutType` (Phase 4F.1's newer public schema enum: `Easy, Interval, Tempo, LongRun, RecoveryEasy`) has no explicit mapping from any catalog key either. **Confirmed: FARTLEK does NOT confirmedly map to public INTERVAL; THRESHOLD_TEMPO does NOT confirmedly map to public TEMPO; GOAL_PACE_TEN_K has no distinct public type; LONG_RUN_STANDARD/EASY_STANDARD define no pace category.** This is an already-documented gap (Phase 3 `PlanCatalogDomainMapper`), not newly discovered, and not invented here.

`eligibleLevels` is `MISSING` as a workout-definition field for all 5 keys — level eligibility is governed inversely, by which `LEVEL_MODIFIER` documents list the workout in their own `eligibleWorkouts` array.

## Structural-role compatibility

1. **Explicitly encoded in catalog JSON data:** none. No schema or JSON field states a role→family or role→workout mapping.
2. **Mechanically implied by validated family constraints (Process A tooling code, not raw JSON):** `plan-catalog/src/PlanCatalog.Core/Validation/CandidatePublishGraphValidator.cs`'s `RoleCompatibleFamilies()` — `LONG_RUN → [LongRun]`, `EASY_SUPPORT → [Easy]`, `KEY_SESSION → [Quality, Race]`. This is a real, executed validator rule (`LAYOUT_SLOT_HAS_NO_ELIGIBLE_WORKOUT`), but it checks only **closure membership** (progression-stage candidates ∪ level-modifier eligible list), not stage-level schedulability.
3. **Assumed only by tests/docs:** none found beyond #2.
4. **Missing, requires later decision:** exact workout-per-role-per-week selection (entirely unassigned at any granularity finer than family); and specifically **how the LONG_RUN role is ever populated at all** — zero workout-progression stages target the `LongRun` family in v10; `LONG_RUN_STANDARD` is reachable only through the level modifier's `eligibleWorkouts` list, never through any stage's `workoutCandidates`. The audit explicitly does **not** conclude `EASY_SUPPORT → EASY_STANDARD` / `LONG_RUN → LONG_RUN_STANDARD` as settled facts — no deterministic catalog basis maps a structural role to one specific workout key.

Backend runtime code (`CatalogRunLayoutSlots`, `CatalogWeekSkeletonCalendarMaterializer`) treats `StructuralRole` as an opaque string end-to-end — confirmed by source inspection — so even the family-level validator rule above is not currently consumed by Process B at all.

## Runtime-eligibility table

| Stage | Condition | Allowed (stage) | Registry | Allowed (registry) | Resolver | Outcome domain | Fallback | Valid |
|---|---|---|---|---|---|---|---|---|
| GOAL_PACE_REHEARSAL | GOAL_FEASIBILITY_IN | REALISTIC, CHALLENGING | RUNTIME_CONDITION_VALUES_V1 v2 | REALISTIC, CHALLENGING, UNSUPPORTED, NOT_REQUESTED | `GoalFeasibilityResolver` (implemented, backend Phase 4C/4D) | REALISTIC/CHALLENGING/UNSUPPORTED/NOT_REQUESTED | CURRENT_FITNESS_SPECIFIC_REHEARSAL | ✓ |

`GOAL_FEASIBILITY_IN` is the **only** runtime-condition type referenced anywhere in v10's workout-progression (confirmed by direct inspection: exactly one `requires` block exists in the whole document). `PLAN_MODE_IN`, `PACE_SOURCE_IN`, `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN` all exist in the registry but are **not v10 dependencies** for stage eligibility — confirmed independently and matching `PilotDomainContentAudit.cs`'s own `AUD-406` finding ("repository-wide search... found zero references... outside the registry artifact itself").

## Fallback-chain audit

One chain: `GOAL_PACE_REHEARSAL → CURRENT_FITNESS_SPECIFIC_REHEARSAL` (same phase, target exists, not circular — confirmed both by direct traversal and by `WorkoutProgressionValidator.DetectCircularFallback`/`TemplateCombinationValidator`'s `TC_STAGE_UNREACHABLE` check). The fallback target has `requires: []`, so it is unconditionally eligible — **the chain always terminates successfully in v10's actual data; no unresolved-terminal scenario is reachable today.** General terminal behavior for a chain whose last stage is *also* conditionally ineligible is not represented anywhere in the current catalog/validator contract — classified `MISSING`/`OPEN_DECISION` for any future stage shape, not applicable to v10 itself.

## Exposure-feasibility table

| Phase | ΣMin | ΣMax | Resolved weeks | minimumFit | maximumFit |
|---|---|---|---|---|---|
| FOUNDATION | 3 | 6 | 3 | EXACT | HAS_EXTENSION_CAPACITY |
| BUILD | 3 | 6 | 4 | UNDER_CAPACITY | HAS_EXTENSION_CAPACITY |
| RACE_SPECIFIC | 3 | 5 | 4 | UNDER_CAPACITY | HAS_EXTENSION_CAPACITY |
| TAPER | 1 | 2 | 1 | EXACT | HAS_EXTENSION_CAPACITY |

No phase is `OVER_CAPACITY` or `INSUFFICIENT_MAXIMUM_CAPACITY`. BUILD and RACE_SPECIFIC have minimum-exposure slack (extension/compression allocation logic will need to decide how the gap between minimum and resolved week count is filled) — this audit does not resolve that allocation.

## Field-source summary

Exact counts of every `fieldSource`-tagged object in `phase4f6-step-a-v10-catalog-audit.json`:

| fieldSource | Count |
|---|---|
| ACTUAL_CATALOG | 96 |
| DERIVED_FROM_CATALOG | 35 |
| PROPOSED | 0 |
| MISSING | 15 |

Every audited field carries exactly one `fieldSource` tag. `PROPOSED` is intentionally zero — Step A does not populate proposed values.

## Existing evidence and decision links

The authoritative existing decision source is `plan-catalog/src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs` (720 lines, ~190 `AUD-xxx` entries — the single source of truth PublishReadinessValidator itself consults), not `docs/evidence-log.json` (which is scoped narrowly to the 4 runtime-resolver *threshold* decisions, not workout-progression stage authoring).

| decisionId | Existing artifact | Link quality |
|---|---|---|
| GOAL_PACE_REHEARSAL.requires | AUD-011; `activation-readiness-risks.md#TD-WAVE5-001` (explicitly about v10) | DIRECT |
| GOAL_PACE_REHEARSAL.fallbackStageKey | AUD-012, AUD-424 | DIRECT |
| TEN_K_SPECIFIC_INTRO / GOAL_PACE_REHEARSAL relativeOrder | AUD-010 | DIRECT |
| Every stage's minimumExposures/maximumExposures | AUD-014 | INDIRECT (PLACEHOLDER_UNCONFIRMED, carried forward unchanged from v1) |
| Every stage's compressionBehavior/extensionBehavior | AUD-015 | INDIRECT (PLACEHOLDER_UNCONFIRMED, carried forward unchanged) |
| Phase-level compressionPriority/extensionPriority | AUD-008 | INDIRECT (PLACEHOLDER_UNCONFIRMED) |
| INTERMEDIATE_MODIFIER eligible-workout set | AUD-045 | INDIRECT (PLACEHOLDER_UNCONFIRMED, carried forward through v3–v6 cascades) |
| FOUNDATION_EASY_BASE/other invented stage keys | AUD-013 | NONE beyond the general "invented for pilot completeness" note |

**Important governance finding, not newly decided here:** `AUD-014`/`AUD-015` (exposure counts and compression/extension behavior — the exact fields Phase 4F.6 will most need) are `PLACEHOLDER_UNCONFIRMED`, i.e. explicitly **invented, not canonically sourced**. `PilotDomainContentAudit.HasBlockingUnconfirmedContent` would flag them as blocking *for their originating version*, but the D2/D3/D4/D13 resolution waves never re-litigated these two specific fields — they were carried forward unchanged and remain formally unconfirmed. Separately, `D13GoalPaceTenKResolutionTests.cs` asserts **zero blocking entries remain** for the *closure* of bundle version 10 as a whole (`v10Remaining` is asserted empty) — meaning every field that *was* in scope for D2/D3/D4/D13 resolution is resolved; exposure/compression/extension were simply never in scope for those waves.

## Undocumented assumptions

| Assumption | Location | Encoded in catalog? | Current effect | Requires later decision? |
|---|---|---|---|---|
| Backend consumes zero stage-level progression fields today | `PlanCatalogBundleLoader.cs` (confirmed via source search) | No | Phase 4F.2 treats "stage" as phase-granularity only; entire per-stage authoring surface is inert in the running backend | Yes |
| LONG_RUN_STANDARD usable for LONG_RUN role despite no stage targeting it | `CandidatePublishGraphValidator.cs` (union closure, not stage-reachability) | No | Family-closure validator passes without proving any stage can ever produce a LONG_RUN session | Yes |
| "Golden" structural test suite targets stale artifact versions, not v10's actual graph | `PilotCatalogStructuralTests.cs` | No | No test directly re-asserts the golden structural claims (layout shape, core-cycle sum) against v10's actual v6/v2/v6/v4 dependency versions; only generic assembly/version checks exist for the literal v10 bundle | No |
| Single-candidate-per-stage means candidate ranking is untested | `ten-k-workout-progression.v5.json` (every stage has exactly 1 candidate) | Yes (fact, not assumption) | No ranking logic is exercised for v10; latent question for future multi-candidate stages | Yes |
| Complexity-tier gating cannot be mechanically checked for v10 | `progression-modifier.schema.json` + `intermediate-progression-modifier.v2.json` | Yes | The audit's own "are candidate complexity tiers allowed by the progression modifier" question is N/A, not true/false, because schemaVersion 2 forbids the gating field entirely | No |

## Validation results

```
cd plan-catalog
dotnet build PlanCatalog.sln -c Release
  → 0 errors, 0 warnings

dotnet test PlanCatalog.sln -c Release --no-build
  → 335 passed, 0 failed, 0 skipped, 335 total

dotnet test PlanCatalog.sln -c Release --no-build --filter "FullyQualifiedName~PilotCatalogStructuralTests|FullyQualifiedName~PilotDomainContentAuditTests|FullyQualifiedName~PilotWorkoutFixtureConfirmationTests|FullyQualifiedName~TaperPhaseFamilyEligibilityTests|FullyQualifiedName~D13GoalPaceTenKResolutionTests|FullyQualifiedName~D3RuntimeConditionRegistryResolutionTests|FullyQualifiedName~D4PeakVolumeBandResolutionTests"
  → 66 passed, 0 failed, 0 skipped, 66 total
```

No documented-but-unimplemented validation rule was found. One implemented rule appears to have thinner-than-ideal direct coverage: `CandidatePublishGraphValidator.ValidateLayoutCoverage`'s `LAYOUT_SLOT_HAS_NO_ELIGIBLE_WORKOUT` is exercised generically but not by a test that names v10's literal LONG_RUN-via-eligible-list-only situation explicitly — its passing status for v10 was confirmed by this audit's manual trace, not by a dedicated existing test assertion.

## Files inspected

Catalog JSON: `combinations/ten-k-4d-intermediate.v10.json`, `templates/ten-k-master.v{1..6}.json` (v6 primary), `layouts/run-layout-4d.v{1,2}.json`, `level-modifiers/intermediate-modifier.v6.json`, `progression-modifiers/intermediate-progression-modifier.v2.json`, `rule-packs/appsel-race-plan.v4.json`, `workout-progressions/ten-k-workout-progression.v5.json`, `registries/runtime-condition-values.v2.json`, `policies/peak-volume-bands.v3.json`, `workouts/{easy-standard,long-run-standard,fartlek,threshold-tempo}.v4.json`, `workouts/goal-pace-ten-k.v2.json`.

Schemas: all 12 files in `plan-catalog/schemas/`.

Code: `PlanCatalog.Core/Validation/{WorkoutProgressionValidator,RunLayoutValidator,TemplateCombinationValidator,CandidatePublishGraphValidator,CatalogGraphValidator}.cs`, `PlanCatalog.Core/Catalog/WorkoutClosureResolver.cs`, `PlanCatalog.Core/Audit/PilotDomainContentAudit.cs` (full, 720 lines).

Tests: `PlanCatalog.Tests/Golden/PilotCatalogStructuralTests.cs`, `PlanCatalog.Tests/Validation/{PilotDomainContentAuditTests,D13GoalPaceTenKResolutionTests,D3RuntimeConditionRegistryResolutionTests,D4PeakVolumeBandResolutionTests}.cs`.

Docs: `docs/evidence-log.{json,md}`, `docs/README.md`, `artifacts/audits/activation-readiness-risks.md`.

Backend: `RunningApp.Application/RuntimeCatalog/PlanCatalogDomainMapper.cs`, `PlanCatalogBundleLoader.cs`, `PlanCatalogCandidateSummary.cs`, `RunningApp.Domain/Enums/TrainingDayType.cs`, `RunningApp.Application/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayload.cs`.

## Files created

- `plan-catalog/artifacts/audits/phase4f6-step-a-v10-catalog-audit.json`
- `plan-catalog/artifacts/audits/phase4f6-step-a-v10-catalog-audit.md` (this file)

No temporary verification scripts were left in place.

## Files modified

None. No production code, catalog artifact, schema, `evidence-log.json`, or canonical decision document was changed.

## Repository state

Branch `main`, HEAD `0c6796578f08bc1d76d96f1944a80c9075455206` (unchanged). No commit was made.

## Pre-decision list for Step B/C

1. **Scientific/coaching evidence needed** for: per-phase `minimumWeeks`/`maximumWeeks` split (AUD-004/005), phase `intents` vocabulary (AUD-006), phase `compressionPriority`/`extensionPriority` (AUD-008), `isCompressionProtected` (AUD-009), every stage's `minimumExposures`/`maximumExposures` (AUD-014), every stage's `compressionBehavior`/`extensionBehavior` vocabulary and per-stage assignment (AUD-015), invented stage keys outside the RACE_SPECIFIC brief example (AUD-013).
2. **Product decision needed** for: which workouts an intermediate athlete may access as a *policy* (AUD-045, currently only a result-observation, not a policy), how the LONG_RUN structural role is ever populated (no stage targets it today), whether/how `StageDistributionBehavior`/`ExtensionPriority`/candidate-ranking/conditional-stage-placement/deterministic tie-breaks should be represented at all.
3. **Governance classification needed** for: whether `TD-WAVE5-001`'s "no automated cross-check between `AllowGoalPaceRehearsal` and stage reachability" risk should be closed by adding a validator rule or left as documented risk; whether the stale `PilotCatalogStructuralTests.cs` golden test should be updated to target v10's actual dependency versions.
4. **Schema/contract decision needed** for: whether structural-role→workout-family (already validator-encoded) should be promoted into an actual catalog schema field; whether a public workout-type mapping belongs in the catalog or purely in backend domain code; whether complexity-tier gating should be reintroduced in some form now that it's schema-forbidden on the currently-referenced progression modifier version.

## Final conclusion

The repository contains enough explicit, source-traceable catalog information to begin **Step B — Evidence Mapping**. Every stage, workout definition, runtime condition, and fallback in v10's actual resolved dependency graph was located, read, and recorded with exact provenance. The catalog is internally structurally valid (335/335 tests pass, including the exact v10 bundle assembly and its zero-remaining-blocking-entries assertion). The gaps found (exposure/compression/extension fields unconfirmed; no public workout-type mapping; LONG_RUN role unreachable via any stage) are exactly the kind of pre-existing, already-partially-documented gaps Step B exists to map against evidence — none of them block starting that work, and none were resolved or guessed at in this audit.
