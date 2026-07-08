# Workout Components Ownership Audit (D6, D8, D10, D12) — COMPONENTS-OWNERSHIP-001

Read-only. Scope: `$.components` on `EASY_STANDARD v2`, `FARTLEK v2`, `LONG_RUN_STANDARD v2`,
`THRESHOLD_TEMPO v2`.

## Task 1 (partial) — actual usage trace for `components`

| Field | Declared in | Written by | Read by | Validation use | Bundle use | Runtime behavior use | Audit-only use |
|---|---|---|---|---|---|---|---|
| `WorkoutDefinition.Components` (`src/PlanCatalog.Core/Models/WorkoutDefinition.cs:24`) | Required `IReadOnlyList<WorkoutComponentDefinition>`; schema `workout-definition.schema.json` `$.components` (required, array, no `minItems`) | Catalog authors, hand-written per workout JSON file | **No validator reads it** — `WorkoutDefinitionValidator.Validate` (`src/PlanCatalog.Core/Validation/WorkoutDefinitionValidator.cs`) checks `ComplexityTier`, `EligiblePhases`, `AllowedPrescriptionModes`, `AllowedDistanceAccountingModes` but has **zero** checks on `Components` (no count check, no `SequenceOrder` contiguity check unlike the analogous `RunLayoutValidator`, no content check) | **None** — confirmed by direct inspection of `WorkoutDefinitionValidator.cs`; this is asymmetric with `RUN_LAYOUT_4D`'s `sequenceOrder`, which *is* actively validated | Republished verbatim as part of each `WORKOUT_DEFINITION` artifact in every release; not special-cased by `CatalogBundleAssembler` | **None found** in this repository | `PilotDomainContentAudit.cs` (`AUD-207`/`AUD-231`/`AUD-219`/`AUD-243`), classified `PLACEHOLDER_UNCONFIRMED` for all 4 workouts' v2 |
| `WorkoutComponentDefinition.SequenceOrder` (`src/PlanCatalog.Core/Models/WorkoutComponentDefinition.cs:11`) | Required `int` per component, schema `minimum: 1` | Catalog authors | No validator reads it (confirmed same file as above) | None | Republished verbatim | None found | Not separately audited (bundled inside the parent `components` field's audit entry) |
| `WorkoutComponentDefinition.ComponentType` (`WorkoutComponentDefinition.cs:12`) | Required `WorkoutComponentType` enum (`WarmUp, MainSet, CoolDown, Recovery, Strides` — 5 values); part of the stable `PlanCatalog.Contracts` Process A/Process B boundary contract (confirmed via `tests/PlanCatalog.Tests/Architecture/PublishedBoundaryTests.cs`'s `Contracts_PublishedTypes_AreLimitedToKnownBoundaryShapes` allow-list, which explicitly includes `WorkoutComponentType`) | Catalog authors | No validator reads it | None | Republished verbatim | Structurally part of the published boundary shape, but no runtime logic in this repository reads or branches on it | Bundled inside `components`'s audit entry |
| `WorkoutComponentDefinition.IntensityDescriptor` (`WorkoutComponentDefinition.cs:13`) | Required `string` — **unconstrained free text**, no schema `enum` (unlike `ComponentType`) | Catalog authors | No validator reads it | None | Republished verbatim | None found | Bundled inside `components`'s audit entry |

**Search terms not found anywhere in `plan-catalog/src/`**: `warmup` (as a distinct field/type name beyond
the `WarmUp` enum member), `cooldown` (beyond `CoolDown` enum member), `repetitions`, `recovery` (as a
Process A field — it exists only as a `WorkoutComponentType.Recovery` enum member, never as a structured
sub-object), `main set` (beyond the `MainSet` enum member). This confirms Process A's `components` model
is a flat list of `{sequenceOrder, componentType, intensityDescriptor}` triples with **no** nested
repetition-count, duration, or recovery-detail structure of any kind.

## Task 5 — Golden Fixture v3 boundary findings

Direct structural tally of every realized `workout.components` occurrence across all weeks/days in
`docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.plandocument.json` (33+ workout
instances total for the 4 keys in scope):

| WorkoutKey (fixture) | Occurrences | Ever has `components` array? |
|---|---:|---|
| `EASY_STANDARD` | 23 | **Never** — every instance is a flat `{family, workoutKey, prescriptionMode, plannedDistanceKm, intensity, loadClassification, ...}` shape with no `components` key at all |
| `LONG_RUN_STANDARD` | 10 | **Never** — same flat shape as `EASY_STANDARD` |
| `FARTLEK` | 2 | **Always** — `components: [WARM_UP, FARTLEK_MAIN_SET, COOL_DOWN]` |
| `THRESHOLD_TEMPO` | 2 | **Always** — `components: [WARM_UP, TEMPO_MAIN_SET, COOL_DOWN]` |

(Other workout keys present in the fixture — `EASY_SHAKEOUT`, `EASY_WITH_STRIDES`, `LONG_RUN_PROGRESSION`,
`RACE_DAY`, `RACE_PACE_REPEATS`, `TEN_K_REPETITIONS` — are **not** among the 4 in scope for this audit and
are noted only as structural context: `EASY_WITH_STRIDES` and `LONG_RUN_PROGRESSION` show that a
components-bearing *variant* of easy/long-run training exists in the domain, but as a **different,
non-substitutable workout key**, not as an alternate shape of `EASY_STANDARD`/`LONG_RUN_STANDARD`.)

For each of the four in-scope workouts, one representative generated instance and its field classification:

**`EASY_STANDARD`** (`docs/canonical/golden-fixture-v3/...plandocument.json:196-211`):
```json
{ "family": "EASY", "workoutKey": "EASY_STANDARD", "prescriptionMode": "DISTANCE",
  "plannedDistanceKm": 5.5, "distanceIsEstimate": false,
  "intensity": { "mode": "EASY_PACE_RANGE", "paceMinSecPerKm": 350, "paceMaxSecPerKm": 375 },
  "loadClassification": "LOW", "distanceAccountingMode": "EXACT_SESSION_TOTAL" }
```
No `components` array. `plannedDistanceKm` (5.5) → **user-specific generated value** (this athlete's
actual planned distance for this week — different across the 23 occurrences). `intensity.paceMinSecPerKm`/
`paceMaxSecPerKm` → **derived fixture output**, computed from this athlete's goal pace. Neither is reusable
catalog knowledge; neither corresponds to anything in the catalog's `components` field, because there is
no `components` field in the generated output at all for this key.

**`FARTLEK`** (`...plandocument.json:520-559`):
```json
{ "componentType": "WARM_UP", "distanceKm": 2.0, "intensity": {...} },
{ "componentType": "FARTLEK_MAIN_SET", "repetitions": 3,
  "work": { "durationSeconds": 180, "intensity": {...} },
  "recovery": { "durationSeconds": 120, "mode": "EASY_JOG" } },
{ "componentType": "COOL_DOWN", "distanceKm": 2.0, "intensity": {...} }
```
`componentType: "WARM_UP"`/`"COOL_DOWN"` → **structural evidence**, corroborating that the catalog's
generic `WarmUp`/`CoolDown` vocabulary is real. `componentType: "FARTLEK_MAIN_SET"`,
`repetitions: 3`, `work.durationSeconds: 180`, `recovery.durationSeconds: 120` → **user-specific generated
value** (a concrete prescription — this athlete's plan happens to call for 3 reps of 180s at this pace) —
**must not** be copied into the reusable catalog definition. `distanceKm: 2.0` on `WARM_UP`/`COOL_DOWN` is
likewise a generated concrete value, not reusable knowledge.

**`LONG_RUN_STANDARD`** (`...plandocument.json:238-253`): structurally identical (flat, no `components`)
to `EASY_STANDARD`'s example above — no distinguishing evidence beyond confirming the same "continuous,
no component breakdown" pattern.

**`THRESHOLD_TEMPO`** (`...plandocument.json:648-679`):
```json
{ "componentType": "WARM_UP", "distanceKm": 2.0, "intensity": {...} },
{ "componentType": "TEMPO_MAIN_SET", "durationSeconds": 1200,
  "intensity": { "mode": "ESTIMATED_THRESHOLD_EFFORT", "paceMinSecPerKm": 315, "paceMaxSecPerKm": 320 } },
{ "componentType": "COOL_DOWN", "distanceKm": 2.0, "intensity": {...} }
```
`componentType: "TEMPO_MAIN_SET"` (no `repetitions` field present, unlike `FARTLEK_MAIN_SET`) →
**structural evidence** that `THRESHOLD_TEMPO`, as this specific catalog key, is realized **only** as a
single continuous tempo block in both fixture occurrences — never as an interval/cruise-repeat format.
`durationSeconds: 1200`, `paceMinSecPerKm`/`paceMaxSecPerKm` → **user-specific generated value**. A
separate, differently-keyed workout (`TEN_K_REPETITIONS`, `...plandocument.json:1020-1059`) realizes the
interval/cruise-repeat format (`componentType: "INTERVAL_MAIN_SET"`, `repetitions: 4`,
`work.durationSeconds: 240`, `recovery.durationSeconds: 90`) — this is **structural evidence that the
interval format belongs to a different workout key, not to `THRESHOLD_TEMPO`**.

### Fixture field classification summary

| Fixture field/pattern | Classification |
|---|---|
| `workoutKey` presence/absence pattern of `components` (per key, across all instances) | **Structural evidence only** — reveals which keys are continuous vs. composite in the *generated model's own shape*, not a value to copy |
| `componentType` vocabulary (`WARM_UP`, `COOL_DOWN`, `FARTLEK_MAIN_SET`, `TEMPO_MAIN_SET`, `INTERVAL_MAIN_SET`, `STRIDES`) | Generic members (`WARM_UP`, `COOL_DOWN`, `STRIDES`) → **structural evidence, reusable catalog knowledge**, already present in `WorkoutComponentType`. Workout-specific members (`FARTLEK_MAIN_SET`, `TEMPO_MAIN_SET`, `INTERVAL_MAIN_SET`) → **derived fixture output** naming, not yet promoted to catalog vocabulary, per `ten-k-pilot-vocabulary-decisions.md`'s recorded open ownership question |
| `repetitions`, `work.durationSeconds`, `recovery.durationSeconds`, `distanceKm`, `intensity.paceMinSecPerKm`/`paceMaxSecPerKm` | **User-specific generated value** — concrete, athlete-specific prescription; must never be promoted into a reusable `WorkoutDefinition` |
| `plannedDistanceKm`, `distanceIsEstimate`, `loadClassification`, `containsQualitySegment`, `trainingHardStimulusCount` | **User-specific generated value** (session-level) — plan-instance output, not catalog policy |
| `distanceAccountingMode` on the generated workout | **Derived fixture output** that happens to mirror the catalog's own `AllowedDistanceAccountingModes` vocabulary — corroborates that vocabulary (already `CANONICAL_CONFIRMED` per `ten-k-pilot-domain-review-summary.md`), not the `components` question |

### Evidence level per decision (D6/D8/D10/D12)

| Decision | Fixture evidence type |
|---|---|
| D6 (`EASY_STANDARD.components`) | **Structural evidence only** — and the structural evidence points toward "no components array in the generated model for this key," not toward confirming any specific catalog-authored breakdown |
| D8 (`FARTLEK.components`) | **Structural evidence only** for the generic component *types* (`WARM_UP`/`COOL_DOWN` presence); **no evidence** (in fact contrary evidence) for the catalog's current single generic `MAIN_SET`/`SURGE_AND_FLOAT` middle component, since the fixture always shows a more specific `FARTLEK_MAIN_SET` with embedded dosage |
| D10 (`LONG_RUN_STANDARD.components`) | **Structural evidence only**, same conclusion as D6 |
| D12 (`THRESHOLD_TEMPO.components`) | **Structural evidence only** for `WARM_UP`/`COOL_DOWN`; additionally **structural evidence that the format is continuous-only** (no repetition/interval structure) for this specific key |

No fixture value was promoted into any catalog default by this audit. No decision above was resolved from
a single generated instance treated as a general rule.

## Task 3 — components ownership, per workout

### EASY_STANDARD v2
Current catalog value: `[{sequenceOrder:1, componentType:"MAIN_SET", intensityDescriptor:"EASY"}]`
(`catalog/workouts/easy-standard.v2.json`).
1. **Consumed anywhere?** No (see usage trace above — zero validator/runtime consumers).
2. **Current shape reusable or generated?** Neither cleanly — it is an authored, single-element
   placeholder that does not correspond to the fixture's actual generated shape (which has no
   `components` array for this key at all).
3. **Duplicates `PrescriptionMode`/`DistanceAccountingMode`?** No direct field duplication, but see the
   architectural note below on `DistanceAccountingMode`'s correlation with component presence.
4. **Duplicates `WorkoutProgression` dosage?** No — dosage fields (`MinimumExposures`/`MaximumExposures` on
   `WorkoutProgressionStageDefinition`, `MainSetDoseMultiplier` on `ProgressionModifierDefinition`) live on
   entirely separate models; `components` carries no dosage/count data of its own kind to duplicate them
   with.
5. **Belongs in `WorkoutDefinition`?** Only if a components concept is kept at all for this key — see Task 4.
6. **Should it instead be:** given the fixture never structures this key with components, the leading
   candidate is that `EASY_STANDARD` should have an **optional structural descriptor** (or no components
   requirement at all), not a mandatory single-element placeholder.

### FARTLEK v2
Current catalog value: `[{WARM_UP,EASY}, {MAIN_SET,SURGE_AND_FLOAT}, {COOL_DOWN,EASY}]`
(`catalog/workouts/fartlek.v2.json`).
1. **Consumed anywhere?** No.
2. **Current shape reusable or generated?** The 3-part `WARM_UP`/`MAIN_SET`/`COOL_DOWN` shape is
   structurally reusable (matches the fixture's own 3-part shape), but the middle element's
   `intensityDescriptor: "SURGE_AND_FLOAT"` is a stable structural label, not a copied generated value
   (correctly, no `repetitions`/duration was copied in) — the catalog author did **not** violate the
   "Category B must not be copied" rule here.
3. **Duplicates `PrescriptionMode`/`DistanceAccountingMode`?** No.
4. **Duplicates `WorkoutProgression` dosage?** No.
5. **Belongs in `WorkoutDefinition`?** Yes, structurally — the fixture confirms FARTLEK always has a
   composite warm-up/main-set/cool-down shape, matching a reusable "structural capability" concept.
6. **Should it instead be:** the existing generic breakdown is defensible as a **component-capability
   taxonomy** (this workout *has* a warm-up, a variable-effort main set, and a cool-down), but the fixture's
   `FARTLEK_MAIN_SET` label being more specific than the catalog's generic `MAIN_SET` is an unresolved
   vocabulary-granularity question (per `ten-k-pilot-vocabulary-decisions.md`) — not evidence the current
   shape is wrong, but evidence it may be under-specified.

### LONG_RUN_STANDARD v2
Current catalog value: `[{WARM_UP,EASY}, {MAIN_SET,STEADY}]` (`catalog/workouts/long-run-standard.v2.json`).
1. **Consumed anywhere?** No.
2. **Current shape reusable or generated?** Authored placeholder, same character as `EASY_STANDARD` — the
   fixture never structures this key with components at all (10/10 occurrences flat), so this 2-element
   breakdown does not correspond to anything in the generated model.
3. **Duplicates `PrescriptionMode`/`DistanceAccountingMode`?** No.
4. **Duplicates `WorkoutProgression` dosage?** No.
5. **Belongs in `WorkoutDefinition`?** Only if a components concept is kept for this key at all.
6. **Should it instead be:** same conclusion as `EASY_STANDARD` — leading candidate is an optional
   structural descriptor or no components requirement, not a mandatory fixed breakdown.

### THRESHOLD_TEMPO v2
Current catalog value: `[{WARM_UP,EASY}, {MAIN_SET,THRESHOLD}, {COOL_DOWN,EASY}]`
(`catalog/workouts/threshold-tempo.v2.json`).
1. **Consumed anywhere?** No.
2. **Current shape reusable or generated?** Structurally reusable and consistent with both fixture
   occurrences (continuous single main-set, no repetitions) — the catalog's shape is the *closer* match of
   the four workouts to its fixture counterpart.
3. **Duplicates `PrescriptionMode`/`DistanceAccountingMode`?** No.
4. **Duplicates `WorkoutProgression` dosage?** No.
5. **Belongs in `WorkoutDefinition`?** Yes, structurally — matches fixture shape closely.
6. **Should it instead be:** the existing generic breakdown is reasonable as-is; the only open question
   (per Task 4) is whether a future interval/cruise-repeat variant should be modeled as a *different*
   `WorkoutDefinition` key (matching the fixture's own `TEN_K_REPETITIONS` pattern) rather than as a shape
   change to `THRESHOLD_TEMPO` itself.

## Architectural note: `DistanceAccountingMode` correlation

`AllowedDistanceAccountingModes` (already `CANONICAL_CONFIRMED`, not in scope to change) already encodes
a related signal: `EASY_STANDARD`/`LONG_RUN_STANDARD` use `EXACT_SESSION_TOTAL` (flat, single-total
distance accounting), while `FARTLEK`/`THRESHOLD_TEMPO` use `ESTIMATED_SESSION_TOTAL`. The fixture's
`EMBEDDED_COMPONENTS` mode (seen on the out-of-scope `EASY_WITH_STRIDES` key) is reserved for workouts
whose distance is actually computed by summing component-level distances. **No catalog workout in this
4-key scope currently uses `EMBEDDED_COMPONENTS`.** This is not a duplication of `components` by
`DistanceAccountingMode` (they are different concerns — one describes *how distance is totaled*, the other
describes *session structure*), but it is a structural correlation worth recording: the same two-workout
split (`EASY_STANDARD`/`LONG_RUN_STANDARD` vs. `FARTLEK`/`THRESHOLD_TEMPO`) that the fixture shows for
"has components array or not" already exists, independently, in `AllowedDistanceAccountingModes`'s
`EXACT_SESSION_TOTAL` vs. `ESTIMATED_SESSION_TOTAL` split — suggesting (not proving) that whether a
workout needs a `components` breakdown may be *derivable* from its `DistanceAccountingMode`, rather than
needing to be independently authored and independently unconfirmed for every workout.

## Final status: components ownership and Golden Fixture boundary analysis complete for all 4 workouts. No schema, code, or classification was changed.
