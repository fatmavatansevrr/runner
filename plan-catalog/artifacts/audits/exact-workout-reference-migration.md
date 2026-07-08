# Exact Workout Reference Migration — Milestone B — EXACTREF-001

## Scope

The only two confirmed key-only cross-artifact dependency fields (per Part 1's
`cross-artifact-reference-inventory.md`): `LevelModifierDefinition.EligibleWorkoutKeys` and
`WorkoutProgressionStageDefinition.WorkoutCandidateKeys`. No other field was touched — in particular
`PlanTemplateDefinition.WorkoutProgression`, `LevelModifierDefinition.ProgressionModifier`, and all
`RulePackDefinition` reference fields were confirmed already `VersionedCatalogReference` and left exactly
as-is.

## B1/B2 — New exact fields

| Field | Old shape | New shape |
|---|---|---|
| `WorkoutProgressionStageDefinition.WorkoutCandidateKeys` → `.WorkoutCandidates` | `IReadOnlyList<string>` | `IReadOnlyList<VersionedCatalogReference>` |
| `LevelModifierDefinition.EligibleWorkoutKeys` → `.EligibleWorkouts` | `IReadOnlySet<string>` | `IReadOnlyList<VersionedCatalogReference>` |

Both old and new fields are nullable on the same C# type (chosen mechanism — see B3 below); JSON Schema
requires exactly one of each pair via `oneOf`.

## B3 — Schema-version exclusivity: chosen mechanism

Considered: separate C# types per schema version, an upcaster, or a unified model with validator-enforced
exclusivity. **Chosen: unified model (nullable dual fields) + a dedicated source-integrity validator
(`SchemaVersionShapeValidator`) + JSON Schema `oneOf`.** Rationale: this codebase's dominant invariant
layer is already its C# `*Validator` classes (JSON Schema here does only loose structural checks — no
document type sets `additionalProperties: false`); introducing parallel C# record types per schema version
would require parallel `CatalogSourceSnapshot` collections and would fragment every consumer
(`CatalogBundleAssembler`, every `*Validator`) into type-switch branches, a much larger blast radius than
this migration's two-field scope justifies. The chosen mechanism is fully deterministic:

- `SchemaVersionShapeValidator.Validate` (new, `src/PlanCatalog.Core/Validation/`) enforces, per document:
  legacy field present + new field absent when `schemaVersion == 1`; new field present + legacy field
  absent when `schemaVersion >= 2`; both-present or neither-present always fails.
- JSON Schema `oneOf` on the same two fields provides a second, independent enforcement layer at the
  schema level (defense-in-depth, not the primary mechanism).
- **Historical JSON was never mutated** — `catalog/workout-progressions/ten-k-workout-progression.v1.json`
  and `catalog/level-modifiers/intermediate-modifier.v1.json` are byte-identical to before this migration
  (verified by hash, see below).

## B4 — Exact lookup APIs

Added to `CatalogSourceSnapshot`:
- `FindWorkout(string key, int version)` — exact match, `FirstOrDefault` by `(Key, Version)`, no auto-selection.
- `GetRequiredWorkout(string key, int version)` — same, throws if missing.

The pre-existing `FindWorkout(string key, IRetirementLedger?)` (highest-non-retired-version auto-select)
is retained, now explicitly documented as legacy-only, and is used **only** by:
- the legacy branch of `CatalogBundleAssembler.Assemble` (schemaVersion 1 progressions, unchanged behavior),
- `WorkoutProgressionValidator`/`LevelModifierValidator`'s legacy-field existence checks,
- historical-release reading/verification.

It is never called from the candidate (schemaVersion >= 2) assembly branch — confirmed by code inspection
of `CatalogBundleAssembler.cs`'s `progressionIsExact` branch, which calls only
`snapshot.FindWorkout(r.Key, r.Version)` (the exact overload).

## B5 — Determinism regression, proven

`ExactWorkoutReferenceSchemaTests.AddingHigherWorkoutVersion_DoesNotChangeAPinnedGraph` and
`BundleWorkoutClosureTests.BundleConstruction_RequiresNoLatestVersionLookup` prove the closure computed
from a schemaVersion >= 2 progression/level-modifier pair is a pure function of the documents' own exact
references — never of "what's the highest workout version currently in source." Live proof against the
real catalog: the candidate bundle (`TEN_K__4D__INTERMEDIATE` v4) was built twice in immediate succession
and produced byte-identical output both times (see `deterministic-graph-part2-migration.md`, "candidate
bundle build twice" command).

## Hash verification — nothing historical was mutated

| Artifact | Version | Hash | Status |
|---|---:|---|---|
| `TEN_K_WORKOUT_PROGRESSION_V1` | 1 | `a4856b47bf385ad29c148412480620b2584ddf7b0e0fa177664dc3455baf6281` | **unchanged** — identical to every prior audit's recorded value |
| `TEN_K_WORKOUT_PROGRESSION_V1` | 2 (candidate) | `a7bf02812e9931322be7625f3e26a8d3217100a9c7d7df190adedd4dd48f23f4` | new |
| `INTERMEDIATE_MODIFIER` | 1 | `c5e9d601f2756495c921676cb323872539eb65f901135b1240e27034861bff34` | **unchanged** |
| `INTERMEDIATE_MODIFIER` | 2 (candidate) | `1dda5d17b444710581afda274fae41534d8c619056235453455a76a3ed152770` | new |

## Domain-content impact: none

The candidate progression pins `EASY_STANDARD v2`, `FARTLEK v2`, `THRESHOLD_TEMPO v2`, `GOAL_PACE_TEN_K v1`
— the exact same versions the legacy auto-selection already resolves today. The candidate level-modifier
pins the same 5 keys at their current versions. **No workout dosage, exposure count, or domain
classification changed.** This is purely a reference-shape (provenance) change — classified
`TECHNICAL_ONLY`, not a domain-content decision, per the task's explicit instruction that versioning fixes
provenance without upgrading domain confidence.

## Tests (13 total — see `tests/PlanCatalog.Tests/Validation/ExactWorkoutReferenceSchemaTests.cs`, plus candidate-graph coverage in `BundleWorkoutClosureTests.cs` and `CandidateArtifactTests.cs`)

Tests 6-13 of the Part 2 required list, all passing — see `deterministic-graph-part2-migration.md` final
report for the full count.

## Final status: `EXACT_WORKOUT_REFERENCE_MODEL` = **IMPLEMENTED**
