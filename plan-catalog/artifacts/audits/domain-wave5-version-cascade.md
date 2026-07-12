# Wave 5 — Immutable Version Cascade

**Prior latest draft candidate root:** `TEN_K__4D__INTERMEDIATE v6` (Wave 3, unchanged, preserved)
**New draft candidate root:** `TEN_K__4D__INTERMEDIATE v7` (Wave 5, DRAFT, unpublished)
**Active root (unaffected):** `TEN_K__4D__INTERMEDIATE v4`

## New versions created (exact-reference cascade only)
1. `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v2` — schemaVersion 2; `maximumComplexityTier` removed; the other 4 fields (`maximumHardSessionsPerWeek`, `mainSetDoseMultiplier`, `allowGoalPaceRehearsal`, `allowSecondHardStimulus`) are byte-for-byte unchanged in value from v1.
2. `INTERMEDIATE_MODIFIER v5` — only the `progressionModifier` reference changed (→ v2); `eligibleWorkouts` copied unchanged from v4.
3. `TEN_K__4D__INTERMEDIATE v7` — only the `levelModifier` reference changed (→ v5); `masterTemplate`, `layout`, and `rulePack` copied unchanged from v6.

## Explicitly NOT bumped
`TEN_K_MASTER` (v5), `RUN_LAYOUT_4D` (v2), `APPSEL_RACE_PLAN_V1` (v2), `RUNTIME_CONDITION_VALUES_V1` (v1), `PEAK_VOLUME_BANDS_V1` (v2), `TEN_K_WORKOUT_PROGRESSION_V1` (v4), all 4 workout definitions (v4), `GOAL_PACE_TEN_K` (v1) — none of these are touched by D2.

## Candidate dependency graph (v7)
```
TEN_K__4D__INTERMEDIATE v7
├── masterTemplate:            TEN_K_MASTER v5
├── layout:                    RUN_LAYOUT_4D v2
├── levelModifier:              INTERMEDIATE_MODIFIER v5
│    ├── progressionModifier:   INTERMEDIATE_PROGRESSION_MODIFIER_V1 v2
│    └── eligibleWorkouts:      EASY_STANDARD v4, LONG_RUN_STANDARD v4, FARTLEK v4, THRESHOLD_TEMPO v4, GOAL_PACE_TEN_K v1
└── rulePack:                  APPSEL_RACE_PLAN_V1 v2
     ├── runtimeConditionValueRegistry: RUNTIME_CONDITION_VALUES_V1 v1
     └── peakVolumeBandPolicy:          PEAK_VOLUME_BANDS_V1 v2
```
(`workoutProgression` resolved via `masterTemplate`: `TEN_K_WORKOUT_PROGRESSION_V1 v4`.)

Prior draft candidates v5 and v6 are preserved byte-for-byte. All 7 historical/pilot releases (1.0.0 through 0.6.0-pilot) verify unchanged.

## Determinism
`build-bundle TEN_K__4D__INTERMEDIATE --version 7` was run 3 times: output was byte-identical each time, and `BundleContentHash` was stable at `e10b65f7aa82b9b59ac045efcd0df0335a157b3c65acbf280810885ebab393e4`.

No publish, retire, activate, or supersede action occurred.

## Clarification pass addendum (Issue 5): `TEN_K_MASTER v5` origin and cascade ownership

**Origin:** `TEN_K_MASTER v5` was created in **Wave 3** (`AUD-321`) to repoint the master's exact `WorkoutProgression` reference to `TEN_K_WORKOUT_PROGRESSION_V1 v4` after the `complexityTier` removal cascade. It is **unchanged by Wave 5** — reused as-is in the v7 candidate graph.

**Reference trace:**
- `TEN_K_MASTER` does **not** reference `INTERMEDIATE_MODIFIER` — `PlanTemplateDefinition` has no `LevelModifier`/`ProgressionModifier` field at all; its only cross-references are `WorkoutProgression` and `RequiredRules`/`RequiredRuleKeys` (RulePack key requirement, not exact selection).
- `TEN_K_MASTER` does **not** reference `INTERMEDIATE_PROGRESSION_MODIFIER_V1` (same reason, transitively).
- `TEN_K__4D__INTERMEDIATE` references `INTERMEDIATE_MODIFIER` **independently** of `TEN_K_MASTER` — `TemplateCombinationDefinition` owns `MasterTemplate`, `Layout`, `LevelModifier`, and `RulePack` as four separate sibling references; `LevelModifier` is never reached via `MasterTemplate`.
- The exact reference that changed (`INTERMEDIATE_MODIFIER v4 → v5`) is owned by **`TemplateCombinationDefinition.LevelModifier`** — i.e. the combination's own `levelModifier` field — not by `TEN_K_MASTER`.

**Conclusion:** `TEN_K_MASTER` does not own any changed exact reference in this task. No master bump was required, and none was performed. **No cascade defect found; the Wave 5 cascade is complete.**
