# Domain Wave 2 Activation Plan

Do not execute in this task.

Candidate root: `TEN_K__4D__INTERMEDIATE v5`, bundle hash `cc894e866be081fb3b44171a27f2bee94375240c1fd6c6042255e3c393014732`. Predecessor: `TEN_K__4D__INTERMEDIATE v4`.

Dependency graph: `TEN_K_MASTER v4`, `RUN_LAYOUT_4D v2`, `INTERMEDIATE_MODIFIER v3`, `TEN_K_WORKOUT_PROGRESSION_V1 v3`, `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1`, `APPSEL_RACE_PLAN_V1 v2`, `RUNTIME_CONDITION_VALUES_V1 v1`, `PEAK_VOLUME_BANDS_V1 v2`, workouts `EASY_STANDARD v3`, `FARTLEK v3`, `GOAL_PACE_TEN_K v1`, `LONG_RUN_STANDARD v3`, `THRESHOLD_TEMPO v3`.

Later activation should retire `TEN_K__4D__INTERMEDIATE v4`, publish `0.7.0-pilot`, and supersede `0.6.0-pilot` only after verification. Production must still fail after activation because 8 blockers remain. Rollback keeps all historical artifacts and restores v4 eligibility.
