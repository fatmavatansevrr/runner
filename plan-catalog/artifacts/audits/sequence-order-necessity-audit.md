# SequenceOrder Necessity Audit (D1) — SEQ-ORDER-001

Read-only. Scope: `RUN_LAYOUT_4D v1`'s `$.slots[*].sequenceOrder` field only.

## Task 1 (partial) — actual usage trace for `sequenceOrder`

| Field | Declared in | Written by | Read by | Validation use | Bundle use | Runtime behavior use | Audit-only use |
|---|---|---|---|---|---|---|---|
| `LayoutSlotDefinition.SequenceOrder` (`src/PlanCatalog.Core/Models/LayoutSlotDefinition.cs:7`) | `RunLayoutDefinition.Slots[*].sequenceOrder`, required `int`, schema `run-layout.schema.json` (`minimum: 1`) | Catalog authors, hand-written into `catalog/layouts/run-layout-4d.v1.json` | `RunLayoutValidator.Validate` (`src/PlanCatalog.Core/Validation/RunLayoutValidator.cs:26-32`) | **Yes, actively validated**: `RL_SEQUENCE_ORDER_NOT_CONTIGUOUS` requires the *set* of `SequenceOrder` values across all slots to be unique and exactly `{1..N}` (contiguous, starting at 1). No check that this matches JSON array position, and no check tying a specific ordinal to a specific `Role`. | Republished verbatim as part of the `RUN_LAYOUT` artifact in every release (`artifacts/appsel-plan-catalog/*/layouts/RUN_LAYOUT_4D.v1.json`); not special-cased by `CatalogBundleAssembler` (no bundle logic reads or transforms it) | **None found** — no Process A code path branches on a specific `SequenceOrder` value (only on its shape: uniqueness+contiguity). No Process B code exists in this repository to inspect (`runner/backend/` explicitly out of scope and not modified/read for logic). | `PilotDomainContentAudit.cs` (`AUD-017`) records it as `PLACEHOLDER_UNCONFIRMED` |

**Distinguishing actual vs. intended use**: the validator's contiguity check is real, implemented, and
tested (`RunLayoutValidatorTests.cs` — see below). But it validates *shape only* (a permutation of
`1..N`), never a specific role→ordinal mapping, and never anything that would distinguish
`KEY_SESSION=1,EASY=2,EASY=3,LONG_RUN=4` from, say, `LONG_RUN=1,EASY=2,EASY=3,KEY_SESSION=4`. Both would
pass `RL_SEQUENCE_ORDER_NOT_CONTIGUOUS` equally. There is no test, validator, or bundle-assembly logic
anywhere in the repository asserting that a *specific* role must hold a *specific* ordinal.

## Task 2 — D1 sequenceOrder necessity

**1. Is ordering already represented another way?**
Yes, redundantly, in two independent ways within the source JSON file itself:
- **Array/list order**: `catalog/layouts/run-layout-4d.v1.json`'s `slots` array is authored in the exact
  same order as the `sequenceOrder` values (index 0 → `sequenceOrder:1`, index 1 → `sequenceOrder:2`,
  etc.) — this is an authoring convention, not schema-enforced (nothing requires the array position to
  match the field value).
- **Slot index**: the JSON array's own positional index is a second, structurally-available ordering
  signal that the schema does not currently use for anything (no code reads `Slots[i]`'s index as the
  ordinal — every consumer reads the `SequenceOrder` field explicitly).
- **Day preference / explicit ordinal**: no separate "day preference" field exists; `SequenceOrder` **is**
  the only explicit ordinal field.
- No other field or mechanism represents slot ordering.

**2. Does `sequenceOrder` affect:**
- **Validation** — **Yes**: `RL_SEQUENCE_ORDER_NOT_CONTIGUOUS` (shape-only, as above).
- **Bundle hash** — **Yes, indirectly**: it is part of the serialized `RunLayoutDefinition` document, so
  it participates in `CatalogDocumentHasher`'s canonical-JSON content hash like every other field. This is
  a mechanical consequence of being a required schema field, not evidence the *value itself* carries
  domain meaning.
- **Layout compatibility** — No dedicated compatibility check was found beyond the contiguity/uniqueness
  rule and the existing `RL_LONG_RUN_COUNT_INVALID`/`RL_KEY_SESSION_COUNT_OUT_OF_RANGE`/`RL_SLOT_COUNT_MISMATCH`
  checks, none of which reference `SequenceOrder`.
- **Process B scheduling** — **Confirmed not used for this** (per the existing audit entry `AUD-017` and
  the brief itself): Golden Fixture v3 assigns real `scheduledDate`/weekday values per day
  (`docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.plandocument.json`, e.g.
  `"scheduledDate": "2026-08-04"`), independently of this catalog-level field — the brief explicitly
  forbids assigning weekdays at the catalog level, and no code path connects `SequenceOrder` to a
  `scheduledDate`.
- **Documentation only** — for everything beyond the shape check above, yes: the value functions as an
  authoring convenience/convention with no further consumer.

**3. Can two representations disagree?**
Yes — the field value and the JSON array position **could** disagree (nothing enforces they match), and
if `catalog/layouts/run-layout-4d.v1.json`'s slots were reordered in the array without also updating
`sequenceOrder`, or vice versa, `RunLayoutValidator` would only catch it if the resulting `sequenceOrder`
set stopped being `{1..N}` — a reordering that keeps the *set* of values `{1,2,3,4}` but assigns them to
different array positions (or different roles) would pass validation silently. This is a real, currently
unguarded representation-disagreement risk, though not one that has manifested in the current single
artifact (`run-layout-4d.v1.json`'s array order and `sequenceOrder` values are currently consistent).

**4. If removed, what would be lost?**
- The `RL_SEQUENCE_ORDER_NOT_CONTIGUOUS` shape check itself would need to be removed or re-based on array
  index instead (a mechanical schema change, not a loss of domain information).
- No domain/training information would be lost — no consumer reads the *value* for any behavioral
  purpose; only its *shape* (contiguity) is checked, and array order could carry that same shape
  information without a separate field.
- The one thing genuinely lost would be the *authoring convenience* of a self-describing field independent
  of array position — a minor ergonomic property, not a domain fact.

**5. Is the field redundant, ambiguous, technically required, or a true domain rule?**
**Redundant with array order**, given current usage. It is not ambiguous in its *current* single
artifact (array order and field value agree), but the schema does not *guarantee* they agree, which is a
latent ambiguity risk. It is not "technically required" by any downstream consumer beyond the shape check
that exists solely because the field exists (a self-referential requirement, not an external necessity).
It is not a true domain rule: no coaching/scheduling authority determines which numeric slot holds which
role — per `AUD-017`'s own recorded reasoning, "brief only mandates the shape, not the order," and no
canonical source contradicts that.

## Candidate outcomes considered

| Outcome | Fit |
|---|---|
| `KEEP_REQUIRED` | Possible but not well-supported — nothing requires an *explicit, independently-authored* ordinal field once array order is available |
| `KEEP_OPTIONAL` | Possible middle ground — retains the field for consumers not yet identified, without forcing every layout to author it |
| **`DERIVE_FROM_COLLECTION_ORDER`** | **Best fit given current evidence** — the field's only actual behavior (contiguity/uniqueness) is fully recoverable from array position, and removing the independently-authored field eliminates the two-representations-can-disagree risk identified in Q3 |
| `REPLACE_WITH_EXPLICIT_SLOT_ORDINAL` | Not clearly different in substance from the current field; would not resolve the redundancy, only rename it |
| `MOVE_TO_PROCESS_B` | Not supported — this is explicitly a Process A authoring-shape concern (slot role composition), and Process B already owns real scheduling independently, as confirmed by the fixture's own `scheduledDate` mechanism |
| `REMOVE` | Would also work if array order is deemed sufficiently reliable/enforced without a companion validator change — a stricter version of `DERIVE_FROM_COLLECTION_ORDER` |

No outcome is chosen as final in this report — see `domain-wave1-migration-options.md` for the full
option analysis with advantages/risks/approval requirements per outcome.

## Final status: D1 usage trace and necessity analysis complete. No schema, code, or classification was changed.
