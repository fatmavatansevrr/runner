# D3 Follow-Up — RUNTIME_CONDITION_VALUES_V1 v2 Consumer Trace

Focused follow-up pass. **The D3 vocabulary itself was not changed** — no repository evidence proved a direct inconsistency requiring one. **D3 remains CLOSED.**

## 1. Consumer trace

- **Declared:** `RuntimeConditionType` enum (`PaceSourceIn`, `TimeAdequacyIn`, `CoreEntryReadinessIn`) + `RUNTIME_CONDITION_VALUES_V1` v1/v2 registry artifacts.
- **Structurally validated:** `RuntimeConditionValueRegistryValidator` checks the registry document's *own* shape (no duplicate condition types, non-empty allowed values, UPPER_SNAKE_CASE format) — this runs regardless of whether anything downstream references a given condition type. It is registry self-validation, not consumption.
- **Cross-validated against candidate content:** `CandidatePublishGraphValidator.ValidatePinnedRegistry` is the *only* mechanism that ties a registry's values to actual candidate content — it walks every workout-progression stage's `Requires` list and checks each declared condition type/value against the pinned registry. This is a no-op for any condition type no stage ever declares.
- **Candidate content search** (`catalog/workout-progressions/*.json` v1–v4, `catalog/rule-packs/*.json` v1–v3, `catalog/templates/*.json`, `catalog/workouts/*.json`): **zero** references to `PACE_SOURCE_IN`, `TIME_ADEQUACY_IN`, or `CORE_ENTRY_READINESS_IN`. The *only* condition type any stage's `Requires` ever references, in the entire catalog, is `GOAL_FEASIBILITY_IN` (on `TEN_K_WORKOUT_PROGRESSION_V1`'s `RACE_SPECIFIC.GOAL_PACE_REHEARSAL` stage).
- **Bundle impact:** `PublishedTemplateBundle` carries only a `CatalogArtifactReference` (key/version/hash) for the registry — never inlines `conditionValueSets`. The D3 change updates the referenced hash (v1→v2) but no vocabulary string appears in generated bundle JSON.
- **Eligibility/rule-selection/phase-selection/dosage impact:** none found. `RuntimeConditionType.cs`'s own doc comment states "Process A never evaluates them," consistent with the absence of any consumer.

## 2. Classification

| Field | Classification |
|---|---|
| `PACE_SOURCE_IN` | `DECLARED_BUT_CURRENTLY_UNUSED` |
| `TIME_ADEQUACY_IN` | `DECLARED_BUT_CURRENTLY_UNUSED` |
| `CORE_ENTRY_READINESS_IN` | `DECLARED_BUT_CURRENTLY_UNUSED` |

## 3. Candidate usage evidence — `TEN_K__4D__INTERMEDIATE v8`

`v8` → `APPSEL_RACE_PLAN_V1 v3` → `RUNTIME_CONDITION_VALUES_V1 v2`, and → `TEN_K_WORKOUT_PROGRESSION_V1 v4` (via `TEN_K_MASTER v5`). **No concrete value usage found.** The vocabulary is declared and structurally validated, but **not behaviorally consumed** by the pilot candidate or any artifact it references.

## 4. Old-v1 → new-v2 semantic assessment

| Condition type | Assessment | Detail |
|---|---|---|
| `PACE_SOURCE_IN` | **Mixed rename/semantic change** | `ESTIMATED` unchanged; `RACE_RESULT→RECENT_RACE` and `NOT_PROVIDED→NONE` are plausible 1:1 renames; `TIME_TRIAL` was **dropped with no direct replacement**; `TARGET_TIME` is a **new concept** not present in v1 at all. |
| `TIME_ADEQUACY_IN` | **Pure rename** | Same 3-value structure preserved exactly (`ADEQUATE`, `INSUFFICIENT` unchanged; `TIGHT→COMPRESSED`). |
| `CORE_ENTRY_READINESS_IN` | **Semantic change** | `READY`/`NOT_READY` unchanged; `UNKNOWN` (an epistemic "couldn't determine" state) was replaced by `CAUTION` (a substantive "conditional entry allowed" state) — same cardinality, but not a rename of the same concept. |

**Overall: mixed rename/semantic change** — not a uniform pure rename across all three. Confidence caveat: v1's original authors never documented explicit meanings beyond the bare enum strings (`AUD-048`: "invented for schema completeness"), so this assessment is based on plain-reading of identifiers plus the approved v2 definitions supplied for this task, not a confirmed historical mapping.

## 5. Process B / backend contract risk

**Status: `UNKNOWN_FROM_REPO_EVIDENCE`.** `runner/backend` and Process B implementation were not inspected (out of scope). The only relevant evidence available from `plan-catalog/` is Golden Fixture v3's DecisionTrace (`docs/canonical/golden-fixture-v3/`), which is Process-B-*generated output*, not a documented contract:

- `capacitySnapshot.paceSource = "RECENT_RACE"` — now textually matches the **new v2** value exactly (it did *not* match v1's `RACE_RESULT`). Directional signal only, not proof.
- `TIME_ADEQUACY_RESOLVER.result.timeAdequacy = "ADEQUATE"` — matches both v1 and v2 (unchanged value); no differential signal.
- `CORE_ENTRY_READINESS_RESOLVER...result.readiness = "STANDARD"` — matches **neither** v1 (`READY`/`NOT_READY`/`UNKNOWN`) **nor** v2 (`READY`/`CAUTION`/`NOT_READY`). This mismatch pre-dates and is unaffected by the D3 change.

Per `AUD-048`'s pre-existing, unchanged caution, these are Process-B-internal resolver output labels with no documented mapping to this Process A registry — suggestive, not dispositive. **Cannot verify** whether Process B expects the old v1 strings, already aligns with v2, or neither.

## 6. Technical debt note — TD-D3-001 (recorded as `AUD-406`, `TECHNICAL_ONLY`, non-blocking)

> Before publishing or activating `TEN_K__4D__INTERMEDIATE v8` or any descendant candidate, Process B/runtime mapping must be explicitly verified against the v2 canonical values: `PACE_SOURCE_IN` (`NONE`, `RECENT_RACE`, `ESTIMATED`, `TARGET_TIME`), `TIME_ADEQUACY_IN` (`ADEQUATE`, `COMPRESSED`, `INSUFFICIENT`), `CORE_ENTRY_READINESS_IN` (`READY`, `CAUTION`, `NOT_READY`). Confirm Process B no longer emits or expects the old v1 strings (`RACE_RESULT`, `TIME_TRIAL`, `NOT_PROVIDED`, `TIGHT`, `UNKNOWN`).

This is an **activation-readiness risk note**, not a reopening of D3 — the Process A vocabulary itself is correctly normalized per the approved decision and is self-consistent; the risk is entirely about the *unverified downstream contract*, not the registry content.

## Confirmations

- D4 and D13: not touched.
- No publish, activate, retire, or supersede action occurred.
- No files outside `plan-catalog/` were touched or inspected.
