# 0.6.0-pilot Release Audit — ZERO-SIX-001

## Release identity

- Version: `0.6.0-pilot`
- Channel: `Pilot`
- Path: `artifacts/appsel-plan-catalog/0.6.0-pilot/`
- Manifest: `artifacts/appsel-plan-catalog/0.6.0-pilot/release-manifest.json`
- Bundle: `artifacts/appsel-plan-catalog/0.6.0-pilot/bundles/TEN_K__4D__INTERMEDIATE.v4.json`
- `verify-release --version 0.6.0-pilot`: **PASSED**

## Bundle list — exactly one TEN_K__4D__INTERMEDIATE bundle

```json
"bundles": [
  { "documentType": "PUBLISHED_TEMPLATE_BUNDLE", "key": "TEN_K__4D__INTERMEDIATE", "version": 4,
    "contentHash": "0a574a7abcefaed04b54844ba06d6ae047286f43562b7c540e3a30ad695f401b" }
]
```

No `v1`, `v2`, or `v3` bundle present. Matches the exact Part 2 candidate hash.

## Combination artifact list — exactly one TEMPLATE_COMBINATION entry

```json
{ "documentType": "TEMPLATE_COMBINATION", "key": "TEN_K__4D__INTERMEDIATE", "version": 4,
  "contentHash": "5c6c701b783ee04794a3d2b19aa0d6b640ea3263f89d415bff396d01a6f3daac" }
```

## v4 bundle dependency graph (as published)

```
TEN_K__4D__INTERMEDIATE v4
├── masterTemplate:      TEN_K_MASTER v3
├── layout:               RUN_LAYOUT_4D v1
├── levelModifier:        INTERMEDIATE_MODIFIER v2
├── workoutProgression:   TEN_K_WORKOUT_PROGRESSION_V1 v2
├── progressionModifier:  INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1
├── rulePack:             APPSEL_RACE_PLAN_V1 v2
├── runtimeConditionValueRegistry: RUNTIME_CONDITION_VALUES_V1 v1
├── peakVolumeBandPolicy: PEAK_VOLUME_BANDS_V1 v2
└── workouts: EASY_STANDARD v2, FARTLEK v2, GOAL_PACE_TEN_K v1, LONG_RUN_STANDARD v2, THRESHOLD_TEMPO v2
```

Exact match to the Part 2 acceptance-baseline graph stated in this task's instructions.

## LONG_RUN_STANDARD confirmation

`LONG_RUN_STANDARD v2` is present in the published bundle's `workouts` array. Confirmed (again, against
the published output, not just the pre-publish preview) that `TEN_K_WORKOUT_PROGRESSION_V1 v2`'s
`workoutCandidates` arrays contain **zero** references to `LONG_RUN_STANDARD` — it is present exclusively
through `INTERMEDIATE_MODIFIER v2.eligibleWorkouts`.

## Other artifacts republished

All other stamped catalog document types (`PLAN_TEMPLATE` v1/v2/v3, `RUN_LAYOUT`, `LEVEL_MODIFIER` v1/v2,
`WORKOUT_PROGRESSION` v1/v2, `PROGRESSION_MODIFIER`, `WORKOUT_DEFINITION` all versions,
`RUNTIME_CONDITION_VALUE_REGISTRY`, `PEAK_VOLUME_BAND_POLICY` v1/v2, `RULE_PACK` v1/v2) are republished
individually per brief §15's full-catalog-packaging design — only the combination/bundle list narrows to
the single eligible root.

## Production negative test

`publish --version 0.6.0-pilot-prod-negative-test --channel Production --allow-unconfirmed-content`
correctly **FAILED** with 9 `PUBLISH_PRODUCTION_CONTAINS_UNCONFIRMED_CONTENT` errors (matching
`activeRootClosureBlockingArtifactCount = 9` exactly — see `placeholder-scope-audit.md`). No
`0.6.0-pilot-prod-negative-test` directory was created under `artifacts/appsel-plan-catalog/` — confirmed
by directory listing immediately after the failed attempt.

## Production readiness error contract clarification

A follow-up audit (`artifacts/audits/production-readiness-error-contract-audit.md`) traced why the
Production negative test above reported exactly 9 errors: one Production-readiness error is emitted per
**blocking artifact identity**, not per field-level decision — `errorCount (9) == blockingArtifactCount
(9)`, a distinct number from `blockingDecisionCount (13)`. That follow-up task also fixed a defect where
the 13 nested decisions were only reachable by parsing a message string, by adding a structured
`ContentDecisionGuardResult` (`BlockingArtifactCount`, `BlockingDecisionCount`, `Errors[].BlockingDecisions[]`)
surfaced through both the CLI and `CatalogValidationException.ContentDecisionDetail`. This release
(`0.6.0-pilot`) itself was not modified by that follow-up task — no new release was published; only
validator diagnostics, the CLI, and documentation changed.

## Final status: 0.6.0-pilot published, verified, and matches the Part 2 candidate graph exactly.
