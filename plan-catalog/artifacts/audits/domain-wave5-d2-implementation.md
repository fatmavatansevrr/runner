# Wave 5 / D2 — Implementation

## Schema evolution
`schemas/progression-modifier.schema.json`: `maximumComplexityTier` removed from the top-level `required` list; a conditional `allOf` now requires it only when `metadata.schemaVersion == 1` and forbids it (`not: required`) when `schemaVersion >= 2`. Failure code: `LEGACY_MAXIMUM_COMPLEXITY_TIER_NOT_ALLOWED_IN_NEW_SCHEMA`. Legacy (schemaVersion 1) documents remain readable and still require the field. No replacement complexity field was introduced.

## Model / validator
`ProgressionModifierDefinition.MaximumComplexityTier` is now `int?` (was required `int`). `ProgressionModifierValidator` rejects a present value when `schemaVersion >= 2` and keeps the pre-existing `PM_COMPLEXITY_TIER_TOO_LOW` (`>= 1`) check for `schemaVersion == 1`. This exactly mirrors the Wave 3 pattern used for `WorkoutDefinition.complexityTier`.

## Approved values applied
On the new artifact `catalog/progression-modifiers/intermediate-progression-modifier.v2.json` (schemaVersion 2, version 2, status DRAFT):

- `maximumHardSessionsPerWeek = 1`
- `mainSetDoseMultiplier = 1.00`
- `allowGoalPaceRehearsal = true`
- `allowSecondHardStimulus = false`

These four values are **unchanged from v1** — the only content change across the v1→v2 boundary is the removal of `maximumComplexityTier`. No values were created for `NEW`, `ADVANCED`, `EXPERIENCED`, or any 5/6-day or other-distance combination.

## Immutable cascade created
- `catalog/progression-modifiers/intermediate-progression-modifier.v2.json` (new)
- `catalog/level-modifiers/intermediate-modifier.v5.json` (new — only `progressionModifier` reference repointed to v2; `eligibleWorkouts` unchanged from v4)
- `catalog/combinations/ten-k-4d-intermediate.v7.json` (new — only `levelModifier` reference repointed to v5; `masterTemplate`/`layout`/`rulePack` unchanged from v6)

`v1` of the progression modifier remains untouched (immutable, PUBLISHED across all 7 historical releases 1.0.0–0.6.0-pilot). Candidates v5 (Wave 2) and v6 (Wave 3) are preserved unchanged.

## Activation readiness (concise, per this wave's scope)
- No publish, retire, activate, or supersede action was taken; the new candidate root `TEN_K__4D__INTERMEDIATE v7` is DRAFT/unpublished.
- Resolving D2 removes exactly one of the four remaining blocking decisions in the Wave 3 candidate closure (D2, D3, D4, D13 → D3, D4, D13).
- A Production release remains blocked on D3/D4/D13 regardless of this task; a Pilot release was not cut in this task per instruction (would require an explicit follow-up product decision to batch this with other Wave 5+ work, mirroring the Wave 2/3 pattern of one release per completed batch rather than per decision).
