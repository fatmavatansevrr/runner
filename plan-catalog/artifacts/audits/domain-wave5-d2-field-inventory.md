# Wave 5 / D2 — Field & Consumer Inventory

Read-only trace of all 5 fields on `INTERMEDIATE_PROGRESSION_MODIFIER_V1` before any implementation. Full detail in the JSON companion.

| Field | Type/requirement | v1 value | Written by | Read by | Runtime impact | Target of any consumer? |
|---|---|---|---|---|---|---|
| `maximumComplexityTier` | int, required on schemaVersion 1 only | 2 | authoring JSON | `ProgressionModifierValidator` | none (no downstream complexity-cap target exists since Wave 3 removed `WorkoutDefinition.complexityTier`) | No |
| `maximumHardSessionsPerWeek` | int ≥ 0, required | 1 | authoring JSON | `ProgressionModifierValidator`, `TemplateCombinationValidator` | structural **ceiling** check (`KEY_SESSION count > cap` fails); never a minimum/equality check | Yes — as a ceiling only |
| `mainSetDoseMultiplier` | decimal > 0, required | 1.0 | authoring JSON | `ProgressionModifierValidator` | positivity check only; not multiplied against anything in this repo | No (unused for computation here) |
| `allowGoalPaceRehearsal` | bool, required | true | authoring JSON | none | none; not already runtime-guarded anywhere in this repo | No |
| `allowSecondHardStimulus` | bool, required | false | authoring JSON | `ProgressionModifierValidator` | cross-field consistency check (forbids cap > 1 when false) | Yes — structural consistency only |

Key findings:
- No consumer treats `MaximumHardSessionsPerWeek` as a target/minimum — `TemplateCombinationValidator` uses strict `>`, so zero `KEY_SESSION` slots never fails.
- `AllowGoalPaceRehearsal` is not already runtime-guarded by anything in this repository; the real race-specific eligibility guard lives independently on `TEN_K_WORKOUT_PROGRESSION_V1`'s `GOAL_PACE_REHEARSAL` stage (`Requires: GOAL_FEASIBILITY_IN`).
- `AllowSecondHardStimulus` is schema-shaped as level-global (keyed only by `INTERMEDIATE`), but in actual current reuse it has exactly one referrer (`INTERMEDIATE_MODIFIER` → `TEN_K__4D__INTERMEDIATE`), so today it is applied combination-specifically in practice.
- `MainSetDoseMultiplier` is confirmed unused for computation anywhere in this repository (Process A); Process B (`runner/backend`) was not inspected or modified.

**Clarification pass note (see `domain-wave5-d2-clarification.md`):** the field-level trace for `allowGoalPaceRehearsal` above (`readBy: none`) was already accurate. The inconsistency found and fixed was the *ownership label* in `domain-wave5-d2-ownership.md` (`RUNTIME_GUARD_ONLY` → `TECHNICAL_METADATA`/`PRODUCT_CAPABILITY_DECLARATION (UNCONSUMED)`), not this trace.

No implementation was performed until this trace was complete.
