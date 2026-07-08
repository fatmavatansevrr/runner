# Domain Blocker Resolution Wave 1 — Schema Necessity Audit — WAVE1-SCHEMA-001

Read-only. Scope: the five blockers (D1, D6, D8, D10, D12) that may represent redundant, misplaced, or
incorrectly shaped fields. This is the top-level summary report; see `sequence-order-necessity-audit.md`
and `workout-components-ownership-audit.md` for the full per-decision detail this report synthesizes, and
`domain-wave1-migration-options.md` for options/recommendations/migration forecast.

## Core architectural question

A reusable Process A artifact must contain stable planning knowledge. It must not contain: user-specific
generated dosage, week-specific session construction, values derivable deterministically from another
canonical field, duplicate ordering already represented by collection order or slot index, or Process B
output masquerading as reusable catalog knowledge. Every field below is evaluated against this boundary.

## Task 1 — complete field usage trace (all five fields)

| Field | Declared in | Written by | Read by | Validation use | Bundle use | Runtime behavior use | Audit-only use |
|---|---|---|---|---|---|---|---|
| `RUN_LAYOUT_4D.sequenceOrder` | `LayoutSlotDefinition.SequenceOrder`, `run-layout.schema.json` | Catalog authors | `RunLayoutValidator.Validate` | **Yes** — `RL_SEQUENCE_ORDER_NOT_CONTIGUOUS` (shape only: unique + contiguous 1..N) | Republished verbatim, no bundle-assembly logic | None found | `AUD-017` |
| `EASY_STANDARD.components` | `WorkoutDefinition.Components`, `workout-definition.schema.json` | Catalog authors | *(none)* | **None** | Republished verbatim | None found | `AUD-207` |
| `FARTLEK.components` | same model/schema | Catalog authors | *(none)* | **None** | Republished verbatim | None found | `AUD-231` |
| `LONG_RUN_STANDARD.components` | same model/schema | Catalog authors | *(none)* | **None** | Republished verbatim | None found | `AUD-219` |
| `THRESHOLD_TEMPO.components` | same model/schema | Catalog authors | *(none)* | **None** | Republished verbatim | None found | `AUD-243` |

**Key asymmetry found**: `sequenceOrder` is actively validated (shape-only) while `components` has **zero**
validator coverage of any kind, despite both being schema-required fields on sibling artifact types
(`RUN_LAYOUT` and `WORKOUT_DEFINITION`). Neither field has any runtime-behavior consumer or Process-B-facing
logic implemented in this repository (`runner/backend/` was not modified or read for implementation logic,
per task constraints — only the published-boundary contract test `PublishedBoundaryTests.cs` was consulted
to determine which *types* are structurally exposed across the Process A/B boundary).

Full search terms from the task's scope (`sequenceOrder`, `components`, `workout components`, `session
components`, `component ordering`, `warmup`, `cooldown`, `repetitions`, `recovery`, `main set`) were run
across `plan-catalog/` in full. Beyond the model/schema/validator/test/audit locations captured above, all
remaining matches were catalog JSON data files (`catalog/`, every historical release directory under
`artifacts/appsel-plan-catalog/*/`) and Golden Fixture v3 documents — data, not additional code-level
consumers. No Process A CLI command reads or transforms either field beyond generic document
serialization/validation/publishing (`ValidateCommands.cs`/`ReleaseCommands.cs` operate on whole documents,
never on these fields specifically).

## Task 2 (D1) and Task 3 (D6/D8/D10/D12) summary

See the two dedicated reports for full detail. Headline findings:

- **D1** (`sequenceOrder`): redundant with array order under all currently-implemented usage; the one real
  consumer (`RL_SEQUENCE_ORDER_NOT_CONTIGUOUS`) checks only a value-set property fully recoverable from
  array position; no domain rule determines which ordinal a role holds; two independent representations
  (field value vs. array position) can silently disagree under the current schema.
- **D6/D10** (`EASY_STANDARD`/`LONG_RUN_STANDARD` `components`): the Golden Fixture v3 generated model
  **never** produces a `components` array for either key (23/23 and 10/10 occurrences respectively are
  flat) — the catalog's current single/two-element authored breakdown does not correspond to anything in
  the generated output shape for these specific keys.
- **D8** (`FARTLEK` `components`): structurally reusable 3-part shape, correctly avoids copying generated
  dosage, but its generic `MAIN_SET` label is less specific than the fixture's `FARTLEK_MAIN_SET` — an
  open vocabulary-granularity question, not a shape defect.
- **D12** (`THRESHOLD_TEMPO` `components`): the closest match to its fixture counterpart of the four
  workouts — continuous-only shape confirmed by both fixture occurrences, with the interval/cruise-repeat
  format structurally evidenced as belonging to a **different** workout key (`TEN_K_REPETITIONS`), not a
  variant shape of `THRESHOLD_TEMPO`.

## Task 4 — comparison across the four workout types

| Workout | Structurally continuous or composite (per fixture)? | Legitimate to have no explicit component structure? | Notes |
|---|---|---|---|
| `EASY_STANDARD` | **Continuous** (23/23 fixture instances flat) | **Yes** — the generated model itself never decomposes this key into components; a reusable definition asserting "this workout is a single continuous effort block, family/prescription-mode sufficient" is consistent with all available evidence | No marathon-specific or embedded-quality structure was inferred (none exists for this key in this 10K pilot fixture) |
| `FARTLEK` | **Composite** (2/2 fixture instances have components) | No — reusable definition needs *some* structural component capability, since the workout is inherently a changing effort/recovery pattern | The open question is granularity (generic `MAIN_SET` vs. a `FARTLEK`-specific main-set label), not whether structure exists at all |
| `LONG_RUN_STANDARD` | **Continuous** (10/10 fixture instances flat) | **Yes**, by the same evidence as `EASY_STANDARD` | Per explicit instruction, no marathon-specific long-run structure (e.g. embedded quality segments) was inferred for this 10K pilot; the fixture's `LONG_RUN_PROGRESSION` key (which *does* have components) is a distinct, non-substitutable workout key, not evidence for `LONG_RUN_STANDARD` itself |
| `THRESHOLD_TEMPO` | **Composite, continuous-format only** (2/2 fixture instances have components, zero repetitions field) | No — needs structural component capability, but specifically for a **continuous tempo** shape only | The fixture shows the cruise-repeat/interval format is realized as an entirely separate workout key (`TEN_K_REPETITIONS`), suggesting that distinction (continuous vs. interval) should live at the **WorkoutDefinition key level** (a new key, mirroring the fixture's own pattern) rather than inside a single `THRESHOLD_TEMPO` artifact's shape — no concrete repetition count or duration was assigned by this audit |

This audit did **not** apply one generic "components decision" without checking these differences — the
four workouts split into two structurally distinct groups (continuous: `EASY_STANDARD`/`LONG_RUN_STANDARD`;
composite: `FARTLEK`/`THRESHOLD_TEMPO`), a split independently corroborated by the pre-existing,
`CANONICAL_CONFIRMED` `AllowedDistanceAccountingModes` field (`EXACT_SESSION_TOTAL` for the continuous
pair, `ESTIMATED_SESSION_TOTAL` for the composite pair) — see `workout-components-ownership-audit.md`'s
architectural note.

## Task 5 — Golden Fixture v3 boundary findings (summary)

See `workout-components-ownership-audit.md` for the complete field-by-field classification. Summary:
generic `componentType` values (`WARM_UP`, `COOL_DOWN`, `STRIDES`) are **structural evidence, reusable
catalog knowledge**; workout-specific `componentType` values (`FARTLEK_MAIN_SET`, `TEMPO_MAIN_SET`,
`INTERVAL_MAIN_SET`) are **derived fixture output**, not promoted to catalog vocabulary; every numeric
field inside a component (`repetitions`, `durationSeconds`, `distanceKm`, pace bounds) is a **user-specific
generated value** and was not, and must not be, copied into any reusable definition. The
components-array-presence pattern itself (present for `FARTLEK`/`THRESHOLD_TEMPO`, absent for
`EASY_STANDARD`/`LONG_RUN_STANDARD`) is **structural evidence only** — it tells us *whether* a components
concept applies to a key, never *what values* it should contain.

## Validation confirmations (this task)

`dotnet build -c Release`: 0 errors. `dotnet test -c Release`: 260/260 passing (unchanged from before this
audit). Active-root blocker inventory re-confirmed: 13 decisions / 9 artifacts, unchanged. Active v4 bundle
hash re-confirmed unchanged: `0a574a7abcefaed04b54844ba06d6ae047286f43562b7c540e3a30ad695f401b`. Full
detail in `domain-wave1-migration-options.md`'s validation section.

## Final status: schema-necessity usage trace and cross-workout comparison complete for all five blockers. No field's classification was changed; no schema, model, validator, or catalog file was modified.
