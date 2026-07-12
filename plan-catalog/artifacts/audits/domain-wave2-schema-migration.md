# Domain Wave 2 Schema Migration

`RUN_LAYOUT` schemaVersion 2 removes the independently-authored `sequenceOrder` field. Historical schemaVersion 1 layouts still require/read it; schemaVersion 2 layouts reject it and use `slots` array order.

`WORKOUT_DEFINITION` schemaVersion 2 makes `components` optional. If present, `components` must be non-empty, use only `WARM_UP`, `MAIN_SET`, `RECOVERY`, `COOL_DOWN`, and preserve authored array order.

Semantic validation is intentionally narrow by workout key because the catalog has no existing structural-family discriminator. No new taxonomy field was introduced for this wave.
