# TEN_K / 4D / INTERMEDIATE Pilot — Vocabulary Decisions

Reconciled against Golden Fixture v3 (`docs/canonical/golden-fixture-v3/`) per the source hierarchy in
`plan-catalog/docs/README.md`. Golden Fixture v3 references `TEN_K_MASTER v2` / `APPSEL_RACE_PLAN_V1 v3`;
per explicit instruction, field-level vocabulary facts are treated as `SOURCE_SEMANTICS_USABLE` even
though `ARTIFACT_VERSION_PARITY_UNRESOLVED` for the artifact revision itself.

## PrescriptionMode

| Current value | Final value | Ownership | Classification | Source | Affected artifacts | Migration impact |
|---|---|---|---|---|---|---|
| `DISTANCE` (new) | `DISTANCE` | Process A (WorkoutDefinition.AllowedPrescriptionModes) | CANONICAL_CONFIRMED | Golden Fixture v3 plandocument.json — every `workout.prescriptionMode` value across all 12 weeks | EASY_STANDARD, LONG_RUN_STANDARD | Catalog JSON migrated from invented `EFFORT_BASED` |
| `MIXED` (new) | `MIXED` | Process A | CANONICAL_CONFIRMED | Golden Fixture v3, same field | FARTLEK, THRESHOLD_TEMPO | Catalog JSON migrated from invented `EFFORT_BASED`/`PACE_BASED` |
| `PACE_BASED` (legacy) | `PACE_BASED` | Process A | PLACEHOLDER_UNCONFIRMED | none — invented, no source | GOAL_PACE_TEN_K | Retained only because GOAL_PACE_TEN_K (no fixture evidence) still uses it; not removed to avoid inventing a replacement |
| `EFFORT_BASED` (legacy) | `EFFORT_BASED` | Process A | PLACEHOLDER_UNCONFIRMED | none — invented, no source | none (all 4 formerly-EFFORT_BASED workouts migrated) | Retained in the enum only for schema/backward compatibility; unused by any current pilot workout |
| `HEART_RATE_BASED` (legacy) | `HEART_RATE_BASED` | Process A | PLACEHOLDER_UNCONFIRMED | none — invented, no source | none | Never used by any pilot workout; retained because removing an unused-but-still-declared enum member is not "safe" to assert without a dedicated deprecation decision |

**Not removed**: the three legacy values were not deleted from the enum. `GOAL_PACE_TEN_K` still
references `PACE_BASED`, and removing the enum member would force either an invented replacement value
(prohibited) or a breaking schema change to that specific workout with no source-backed substitute.

## DistanceAccountingMode (new vocabulary — restored/introduced this pass)

| Current serialized value | Ownership | Classification | Source | Affected artifacts |
|---|---|---|---|---|
| `EXACT_SESSION_TOTAL` | Process A (new optional `WorkoutDefinition.AllowedDistanceAccountingModes`) | CANONICAL_CONFIRMED | Golden Fixture v3 — `workout.distanceAccountingMode` | EASY_STANDARD, LONG_RUN_STANDARD |
| `ESTIMATED_SESSION_TOTAL` | Process A | CANONICAL_CONFIRMED | Golden Fixture v3 | FARTLEK, THRESHOLD_TEMPO |
| `EMBEDDED_COMPONENTS` | Process A (vocabulary confirmed; not yet assigned to any pilot workout) | CANONICAL_CONFIRMED | Golden Fixture v3 — observed on `EASY_WITH_STRIDES` (not a current pilot workout key) | none in this pilot |

**Migration impact**: `AllowedDistanceAccountingModes` is a new, optional (nullable) field — it does not
break existing artifacts that omit it (`GOAL_PACE_TEN_K` omits it deliberately; there is no fixture
evidence for that key). `WorkoutDefinitionValidator` now rejects an explicitly-present-but-empty list,
mirroring `AllowedPrescriptionModes`.

**No overlap**: `PrescriptionMode` and `DistanceAccountingMode` share zero serialized values (verified by
`PrescriptionModeAndDistanceAccountingModeSeparationTests` and `GoldenFixtureV3IntegrityTests.PrescriptionModeAndDistanceAccountingMode_AreDistinctInTheFixture`).

## PhaseIntent

No change. Golden Fixture v3, its DecisionTrace, and `progression_rules_v2.yaml` were searched in full for
`AEROBIC_BASE`, `VOLUME_BUILD`, `RACE_SPECIFIC_SHARPENING`, and any generic "intent" vocabulary — zero
matches beyond the literal phase key `"TAPER"` (which is a `PhaseKey`, not a `PhaseIntent`). Remains
**PLACEHOLDER_UNCONFIRMED**, ownership: Process A (`PhaseDefinition.Intents`), production-blocking.

## WorkoutComponentType

| Value | In current enum? | Ownership | Classification | Note |
|---|---|---|---|---|
| `WARM_UP` | yes | Process A (shared, general structural) | CANONICAL_CONFIRMED | Used generically for FARTLEK/THRESHOLD_TEMPO in Golden Fixture v3 |
| `MAIN_SET` | yes | Process A (shared, general structural) | CANONICAL_CONFIRMED | Used generically (e.g. `EASY_WITH_STRIDES`) in Golden Fixture v3 |
| `COOL_DOWN` | yes | Process A (shared, general structural) | CANONICAL_CONFIRMED | Used generically for FARTLEK/THRESHOLD_TEMPO in Golden Fixture v3 |
| `STRIDES` | yes | Process A (shared, general structural) | CANONICAL_CONFIRMED | Appears verbatim as a componentType in Golden Fixture v3 |
| `RECOVERY` | yes | Process A | PLACEHOLDER_UNCONFIRMED | Fixture never uses a top-level `RECOVERY` componentType — its recovery data is a nested property of `ACTIVATION_REPEATS` (`recovery: {durationSeconds, mode}`), a different shape from this catalog's flat component list. Not contradicted, but not confirmed either; not removed since it is not proven unsafe to keep. |
| `FUELING_PRACTICE` | no | undetermined | PLACEHOLDER_UNCONFIRMED | Not present anywhere in Golden Fixture v3. No source. Not added. |
| `FARTLEK_MAIN_SET`, `TEMPO_MAIN_SET`, `INTERVAL_MAIN_SET`, `STEADY_FINISH`, `ACTIVATION_REPEATS`, `EASY_RUNNING_TO_SESSION_TOTAL` | no | **UNRESOLVED — Process B generated-output discriminator vs. Process A reusable catalog component** | not added, ownership unresolved | These appear only inside the *generated* PlanDocument's per-day workout components, never as an authoring-time catalog concept. Per explicit instruction, not automatically promoted into the shared `WorkoutComponentType` enum. Recorded here as an open ownership question for a future dedicated decision — this pass does not decide it. |

## StageCompressionBehavior / StageExtensionBehavior

No change. Both the vocabulary (`COMPRESSIBLE`/`PROTECTED`, `EXTENDABLE`/`FIXED_EXPOSURE`) and its
per-stage assignment were invented; Golden Fixture v3 realizes a plan with `runwayWeeks=0` (no phase
compression/extension needed), so it never exercises this logic and cannot confirm or deny either
vocabulary. Remain **PLACEHOLDER_UNCONFIRMED**, ownership: Process A
(`WorkoutProgressionStageDefinition`), production-blocking.

## RuntimeConditionType (registry allowed values)

| Condition type | Values | Classification | Note |
|---|---|---|---|
| `GOAL_FEASIBILITY_IN` | REALISTIC, CHALLENGING, UNSUPPORTED, NOT_REQUESTED | CANONICAL_CONFIRMED | brief §7.6, corroborated by fixture `goalFeasibility.classification=REALISTIC` |
| `PLAN_MODE_IN` | STANDARD, FOCUSED_CORE, COMPRESSED, READINESS_ONLY, COMPLETION_FOCUSED | CANONICAL_CONFIRMED | brief §7.6, corroborated by fixture `planMode=STANDARD` |
| `PACE_SOURCE_IN` | RACE_RESULT, TIME_TRIAL, ESTIMATED, NOT_PROVIDED | PLACEHOLDER_UNCONFIRMED | Fixture's `capacitySnapshot.paceSource=RECENT_RACE` does not match any of these; ownership between this Process A registry and the Process B DecisionTrace field is not explicitly established. Not rewritten — see runtime-condition-registry review below. |
| `TIME_ADEQUACY_IN` | ADEQUATE, TIGHT, INSUFFICIENT | PLACEHOLDER_UNCONFIRMED | Fixture's `TIME_ADEQUACY_RESOLVER.result.timeAdequacy=ADEQUATE` happens to match one of ours, but ownership is unestablished; whole field left unconfirmed rather than partially upgraded. |
| `CORE_ENTRY_READINESS_IN` | READY, NOT_READY, UNKNOWN | PLACEHOLDER_UNCONFIRMED | Fixture's `CORE_ENTRY_READINESS_RESOLVER.result.readiness=STANDARD` does not match any of these. Not rewritten — ownership unresolved. |

## Generated component labels — explicit ownership decisions

Per the brief's instruction not to automatically expand the shared enum, this pass records these
generated-output-specific labels as **unresolved ownership**, not as new catalog vocabulary:

- `FARTLEK_MAIN_SET`, `TEMPO_MAIN_SET`, `INTERVAL_MAIN_SET` — workout-specific quality-session subtypes; plausibly Process-B-only discriminators describing *which* main-set variant was generated for a given workout family, not a reusable Process A structural component.
- `STEADY_FINISH` — appears once (`LONG_RUN_PROGRESSION`); same ownership ambiguity.
- `ACTIVATION_REPEATS` — a repeats-based component with nested `work`/`recovery` sub-objects; structurally different from this catalog's flat `WorkoutComponentDefinition` shape. Adopting it would require a schema shape change beyond a simple enum addition.
- `EASY_RUNNING_TO_SESSION_TOTAL` — a distance-accounting-driven fill component (its `targetSessionDistanceKm` ties directly to `ESTIMATED_SESSION_TOTAL` accounting); likely a generated-output artifact of the accounting mode itself rather than an independent structural component.

None of these were added to `WorkoutComponentType`. This is intentionally left as an open decision for a
future, explicitly-scoped follow-up.
