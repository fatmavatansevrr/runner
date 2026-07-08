# Deterministic Dependency-Graph Hardening — Part 2 Migration — PART2-001

**No numbered release was published. No artifact was retired. No immutable release directory was
modified.** This report covers implementation only; Part 3 executes retirement and republication.

**Update (Part 3 — completed)**: retirement and republication have since been executed exactly as planned
here. `TEN_K__4D__INTERMEDIATE v1/v2/v3` are retired (ledger-only); `v4` is published as `0.6.0-pilot`'s
sole bundle for this key, with the exact hash predicted here
(`0a574a7abcefaed04b54844ba06d6ae047286f43562b7c540e3a30ad695f401b`). See
`deterministic-graph-part3-completion.md` for the full completion record.

## Purpose

> The same root combination version always resolves the same exact versioned dependencies and produces
> the same bundle, even after newer artifact versions are later added to the source catalog.

## Contradiction check against Part 1

Part 1's four reports were read before any change. No corrected fact was contradicted by the current
repository; all four "corrected facts" sections were followed exactly as specified:

1. Only `LevelModifierDefinition.EligibleWorkoutKeys` and `WorkoutProgressionStageDefinition.WorkoutCandidateKeys`
   were migrated. `PlanTemplateDefinition.WorkoutProgression`, `LevelModifierDefinition.ProgressionModifier`,
   and all `RulePackDefinition` reference fields were confirmed still `VersionedCatalogReference` and were
   **not** touched.
2. `PlanTemplateDefinition.RequiredRules` was converted to a semantic `RequiredRuleKeys` — it no longer
   selects a competing exact RulePack version.
3. `TEN_K__4D__INTERMEDIATE` v1/v2/v3 were confirmed still non-retired; the new candidate (v4) explicitly
   records its predecessor and rationale (see Milestone F1 below) rather than silently assuming v3.
4. `CatalogGraphValidator`/`TemplateCombinationValidator` no longer consult retirement at all;
   `CandidatePublishGraphValidator` (new) is the only place retirement is checked, scoped to one selected
   root.
5. `LONG_RUN_STANDARD` was **not** added to any `WorkoutCandidates`/`workoutCandidateKeys` list — it enters
   the candidate bundle exclusively via `LevelModifierDefinition.EligibleWorkouts` (Milestone E, union
   closure).

## Milestone summary

| Milestone | Status |
|---|---|
| A — Validation layer separation | DONE — `CatalogGraphValidator` (source-integrity) vs `CandidatePublishGraphValidator` (publish-graph) |
| B — Exact versioned workout references | DONE — new nullable dual-shape fields + `SchemaVersionShapeValidator` |
| C — RulePack ownership | DONE — `RequiredRuleKeys` semantic field; combination remains sole exact selector |
| D — Pinned runtime registry resolution | DONE — `FirstOrDefault()` removed from the active path |
| E — Self-contained workout closure | DONE — union closure via `WorkoutClosureResolver`; layout coverage validation |
| F — Immutable candidate version cascade | DONE — 4 new candidate artifacts created, 0 mutated |
| G — Active-version policy preparation | DONE — `ActiveVersionUniquenessValidator`, isolated only, not wired into `CatalogPublisher` |
| H — Candidate blocker measurement | DONE — `BlockerScopeMeasurement`, decision-level and artifact-level kept separate |

## Architecture decisions — how each was honored

- **Decision A (exact identity)**: the candidate shape (`WorkoutCandidates`, `EligibleWorkouts`) is always
  `VersionedCatalogReference`; resolution uses `CatalogSourceSnapshot.FindWorkout(key, version)` (exact) —
  never `FindWorkout(key)` (auto-select) — for any schemaVersion >= 2 document.
- **Decision B (self-contained bundle)**: `PublishedTemplateBundle.Workouts` already pinned
  key+version+hash before this migration (contract unchanged); the migration ensures the *set* of workouts
  populating that list is itself computed from exact references only, for the candidate shape.
- **Decision C (combination owns exact RulePack)**: `CatalogBundleAssembler` resolves the bundled RulePack
  exclusively from `combination.RulePack`; `master.RequiredRules`/`RequiredRuleKeys` is never read by
  bundle assembly (confirmed by grep — zero occurrences of `RequiredRule` in `CatalogBundleAssembler.cs`).
- **Decision D (RulePack owns exact registry)**: `CandidatePublishGraphValidator.ValidatePinnedRegistry`
  resolves the registry via `rulePack.RuntimeConditionValueRegistry` (exact), never
  `RuntimeConditionValueRegistries.FirstOrDefault()` — that call was deleted from
  `WorkoutProgressionValidator` entirely.
- **Decision E (historical artifacts remain readable)**: zero historical release directories, zero
  existing published artifacts (`v1` of everything) were modified — verified by hash in
  `exact-workout-reference-migration.md`/`rule-pack-ownership-audit.md`, and by all 6 releases continuing
  to `verify-release` PASSED.
- **Decision F (no retirement in Part 2)**: `artifacts/appsel-plan-catalog/retirements.json` does not
  exist before or after this task (confirmed by file-existence check, and by
  `ActiveVersionPreparationTests.RealCatalogRetirementPlan_IsGeneratedButNotExecuted`).

## Files changed

### Schemas
- `schemas/workout-progression.schema.json` — added `workoutCandidates`, made `workoutCandidateKeys` optional, added `oneOf` mutual-exclusivity.
- `schemas/level-modifier.schema.json` — added `eligibleWorkouts`, made `eligibleWorkoutKeys` optional, added `oneOf`.
- `schemas/plan-template.schema.json` — added `requiredRuleKeys`, made `requiredRules` optional, added `oneOf`.

### Models (`src/PlanCatalog.Core/Models/`)
- `WorkoutProgressionStageDefinition.cs` — `WorkoutCandidateKeys` made nullable; `WorkoutCandidates` (new, nullable `VersionedCatalogReference[]`) added.
- `LevelModifierDefinition.cs` — `EligibleWorkoutKeys` made nullable; `EligibleWorkouts` (new) added.
- `PlanTemplateDefinition.cs` — `RequiredRules` made nullable; `RequiredRuleKeys` (new, nullable `string[]`) added.

### Validation (`src/PlanCatalog.Core/Validation/`)
- `SchemaVersionShapeValidator.cs` (new) — Milestone B3/C2 shape exclusivity, source-integrity layer.
- `CandidatePublishGraphValidator.cs` (new) — Milestone A2/C3/D2/E1 combined, publish-graph layer.
- `ActiveVersionUniquenessValidator.cs` (new) — Milestone G, isolated policy component.
- `CatalogGraphValidator.cs` — removed `IRetirementLedger` parameter entirely; wired in `SchemaVersionShapeValidator`.
- `TemplateCombinationValidator.cs` — removed retirement check (`TC_PROGRESSION_MODIFIER_RETIRED` deleted); `ValidateEffectiveWorkoutSet` now handles both legacy and exact shapes; fixed `GRAPH_DUPLICATE_LEVEL_MODIFIER_EXPERIENCE`/`GRAPH_DUPLICATE_PROGRESSION_MODIFIER_EXPERIENCE` to group by distinct key (a latent bug the migration's own v2 level-modifier exposed — see below).
- `WorkoutProgressionValidator.cs` — removed `RuntimeConditionValueRegistries.FirstOrDefault()` and all `Requires`-vs-registry checks; added local `Requires` shape check (`WP_CONDITION_ALLOWED_VALUES_EMPTY`); handles both candidate shapes.
- `PlanTemplateValidator.cs` — handles both `RequiredRules`/`RequiredRuleKeys`.
- `LevelModifierValidator.cs` — handles both `EligibleWorkoutKeys`/`EligibleWorkouts`.

### Catalog (`src/PlanCatalog.Core/Catalog/`)
- `CatalogSourceSnapshot.cs` — added exact `FindWorkout(key, version)` and `GetRequiredWorkout(key, version)`; documented the legacy `FindWorkout(key, ledger)` as legacy-only.
- `WorkoutClosureResolver.cs` (new) — shared union-closure computation (Milestone E), used by both `CandidatePublishGraphValidator` (coverage checking) and `CatalogBundleAssembler` (bundle content).

### Audit (`src/PlanCatalog.Core/Audit/`)
- `BlockerScopeMeasurement.cs` (new) — Milestone H decision-level/artifact-level measurement.

### Infrastructure (`src/PlanCatalog.Infrastructure/`)
- `Publishing/CatalogBundleAssembler.cs` — branches on exact-vs-legacy shape; exact branch uses `WorkoutClosureResolver` + exact lookups; legacy branch is byte-identical to before.
- `Publishing/CatalogPublisher.cs` — `CatalogGraphValidator.Validate` call no longer passes a ledger (source-integrity); new `CandidatePublishGraphValidator` pass added per eligible combination (publish-graph), fixing Finding 1 (retirement no longer consulted before filtering).

### CLI (`src/PlanCatalog.Cli/Commands/`)
- `ReleaseCommands.cs` (`build-bundle`) — added `CandidatePublishGraphValidator` check for the explicitly requested combination.
- `ValidateCommands.cs` (`validate`, `validate-combination`) — updated call signatures; `validate-combination` now runs both structural and publish-graph validation.

### Catalog source artifacts (new, immutable — see Milestone F below)
- `catalog/workout-progressions/ten-k-workout-progression.v2.json`
- `catalog/level-modifiers/intermediate-modifier.v2.json`
- `catalog/templates/ten-k-master.v3.json`
- `catalog/combinations/ten-k-4d-intermediate.v4.json`

### Tests
13 test files added/updated — see individual milestone reports and the final report's test-count summary.

## Unexpected discovery during Milestone F

Creating `INTERMEDIATE_MODIFIER v2` (same `Experience: INTERMEDIATE` as `v1`) tripped a latent bug in
`CatalogGraphValidator`'s `GRAPH_DUPLICATE_LEVEL_MODIFIER_EXPERIENCE` check: it grouped by `Experience`
across **all versions of all keys**, so two immutable versions of the *same* key legitimately sharing one
`Experience` were flagged as if they were two *different* keys in conflict. This had never fired before
because no `LevelModifierDefinition` key had ever had a second version. Fixed to group by distinct `Key`
within each `Experience` group (same fix applied symmetrically to the `ProgressionModifier` variant of the
check, which has the identical latent defect, not yet triggered). Documented here rather than silently
folded into "Milestone B" since it is a genuine, independently-discovered correctness fix.

## See also

`exact-workout-reference-migration.md`, `rule-pack-ownership-audit.md`,
`runtime-registry-resolution-audit.md`, `bundle-workout-closure-audit.md`,
`part3-retirement-and-release-plan.md` for milestone-specific detail.
