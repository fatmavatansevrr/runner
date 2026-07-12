# Wave 5 / D2 — Evidence Classification

`AUD-044` (the original bundled v1 entry covering all 5 fields) is left untouched as a historical record — it stays `PLACEHOLDER_UNCONFIRMED` for the immutable, already-PUBLISHED v1 artifact. D2 is resolved via 5 new, field-level entries on v2, because the approved decision set assigns each field a **different** classification and a single bundled row would misrepresent at least one of them.

| Field | Value | Classification | Notes |
|---|---|---|---|
| `maximumComplexityTier` | removed | `TECHNICAL_ONLY` | Field removal; no remaining domain claim. |
| `maximumHardSessionsPerWeek` | 1 | `CANONICAL_CONFIRMED` (task label: EVIDENCE_BACKED) | Fixture directly corroborates this exact combination; documented as a ceiling, not a target. |
| `mainSetDoseMultiplier` | 1.00 | `EXPLICIT_PRODUCT_DEFAULT` | Identity baseline, not an independently sourced universal ratio. |
| `allowGoalPaceRehearsal` | true | `EXPLICIT_PRODUCT_DEFAULT` | Principle flag; PRINCIPLE_FLAG/RUNTIME_GUARDED metadata recorded; guards remain separate and unimplemented here. |
| `allowSecondHardStimulus` | false | `CANONICAL_CONFIRMED` (task label: EVIDENCE_BACKED) | Consistent with fixture + existing cross-check rule; explicitly scoped, not generalized. |

The repository's `ContentDecisionStatus` enum is applied at field/JsonPath granularity (per `DomainContentDecision`'s own doc comment), not at the artifact level — so D2's 5 fields correctly carry 3 distinct classifications across 5 entries rather than one aggregate label. This avoids overstating evidence for the two `EXPLICIT_PRODUCT_DEFAULT` fields by never rolling the whole cluster up to `CANONICAL_CONFIRMED`. No blocker was recreated: none of the 5 new v2 entries are `PLACEHOLDER_UNCONFIRMED`.
