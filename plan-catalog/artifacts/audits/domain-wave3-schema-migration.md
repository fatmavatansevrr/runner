# Domain Wave 3 Schema Migration

`workout-definition.schema.json` now models the legacy and new shapes explicitly:

- `schemaVersion` 1 and 2 require `complexityTier`.
- `schemaVersion` 3 and later reject `complexityTier`.
- Wave 2 component vocabulary and optionality are unchanged.

`WorkoutDefinition.ComplexityTier` is nullable so old source and published artifacts remain readable. New v4 workout artifacts omit the field, and canonical serialization omits null legacy values.
