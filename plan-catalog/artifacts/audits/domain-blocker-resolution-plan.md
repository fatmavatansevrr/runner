# Domain Blocker Resolution Plan — RESOLUTION-PLAN-001

Read-only work plan for resolving the 13 active-root blockers. No value is chosen here — see
`active-v4-domain-blocker-inventory.md` for the exact inventory/classification and
`domain-blocker-source-map.md` for the atomic research questions and source targets. This report covers
batching, execution order, evidence readiness, and the acceptance standard later resolution work must meet.

## Task 5 — dependency and batch grouping

| Batch ID | Decision IDs | Semantic theme | Why grouped | Required source set | Expected difficulty | Depends on earlier batch | User/product approval required? |
|---|---|---|---|---|---|---|---|
| B1 | D1 | RUN_LAYOUT slot ordering — feasibility of removal vs. canonical order | Single decision, single artifact, self-contained; the research question ("is order even consumed") does not depend on any other blocker | Consumption check (code/Process B contract) + optional Tier 1 source on ordering conventions | Low | None | **Yes** — either a removal decision or an accepted ordering convention needs sign-off |
| B2 | D5, D7, D9, D11 | `complexityTier` assignment across the 4 fixture-evidenced workouts | Same semantic question repeated per workout ("what tier does this workout belong to"); leaf artifacts; must be researched together so the tier rubric is applied consistently across all 4 in one pass, avoiding 4 independently-drifting definitions of "tier 1" | Coaching-authored complexity-tier rubric (does not yet exist) + per-workout classification against it | Moderate — requires first defining the rubric itself, a prerequisite not among the 13 decisions | None (leaf artifacts, no upstream blocker dependency) | Likely — the rubric itself is a product/coaching judgment call |
| B3 | D2 (D2a–D2e treated as one inseparable audit row) | Intermediate-athlete training-load ceiling (complexity cap, hard-session frequency, dose multiplier, goal-pace rehearsal permission, second-hard-stimulus permission) | All 5 fields are bundled into a single pre-existing audit row (`AUD-044`) and form one coherent policy question ("how hard/how often may an intermediate athlete train"); splitting them into 5 separate batches would fragment one coaching judgment across 5 audit updates for no benefit | Coaching literature on intermediate hard-session frequency/dose scaling; `progression_rules_v2.yaml` already ruled out as directly answering this | Moderate-to-high — prior passes already searched the one available rule file and found it silent | **Weakly depends on B2**: `maximumComplexityTier` (part of D2) is the *consumer* of the tier scale that B2 establishes — recommend B2 before B3 so the ceiling value is chosen against a defined rubric, not before one exists | **Yes** — very likely to resolve as `EXPLICIT_PRODUCT_DEFAULT` given the documented source silence |
| B4 | D6, D8, D10, D12 | Workout structural component/vocabulary granularity | Same semantic question repeated per workout ("is the generic WARM_UP/MAIN_SET/COOL_DOWN breakdown the right granularity, or does it need fixture-specific subtypes"); tied to the same open ownership question recorded in `ten-k-pilot-vocabulary-decisions.md` | Coaching material on session structural composition + an explicit vocabulary-ownership decision (ties to D3's ownership theme) | Moderate — partly a sourcing question, partly a schema/vocabulary-ownership question | None directly, but shares its ownership question with D3 (recommend researching alongside, not merging batches) | Possibly — if the resolution requires expanding `WorkoutComponentType`, that is a schema change needing approval |
| B5 | D3 | Runtime-condition-registry vocabulary ownership (`PACE_SOURCE_IN`/`TIME_ADEQUACY_IN`/`CORE_ENTRY_READINESS_IN`) | Single decision, single artifact; fundamentally an ownership/API-contract question rather than a literature question, so distinct in *kind* from every other batch | Explicit Process A/Process B ownership conversation | Low-to-moderate (not a research-heavy task, but requires stakeholder engagement outside this catalog's own documents) | None | **Yes** — an explicit ownership decision is itself the required approval |
| B6 | D4 (D4a–D4c treated as one artifact, 9 rows total but only the non-INTERMEDIATE 3 experience-level groups are blocking) | Non-INTERMEDIATE peak weekly-volume bands | Single artifact, one coherent domain-content question ("what are the peak-volume bands for NEW/ADVANCED/EXPERIENCED"), independent of every other batch | Coach/book peak-volume tables segmented by experience level; exercise-science weekly-mileage literature | Moderate-to-high — no such source has been located in any prior pass | None | Likely — if no Tier 1/2 source is found, will resolve as `EXPLICIT_PRODUCT_DEFAULT` |
| B7 | D13 | `GOAL_PACE_TEN_K` total evidence gap | Single, pre-existing audit row bundling 4 fields for one workout with zero fixture evidence of any kind — the most evidence-starved decision in the set, warranting a dedicated, isolated batch rather than folding it into B2/B4 (which assume at least partial fixture corroboration the other 4 workouts have and this one does not) | Dedicated new source search for this specific workout concept, OR an explicit product decision on how to proceed without one | High — by far the hardest decision to source; may not be resolvable through research alone | None | **Yes** — almost certainly requires a product-owner call regardless of research outcome |

**Batching rules applied**: B2 and B4 each group 4 decisions sharing an identical semantic question across
workouts (not merely sharing a file) — both stay under the 5-decision cap. B3, B5, B6, B7 are each a single
pre-existing audit row / artifact and were not artificially split, since D2's 5 sub-fields and D4's 9 rows
are each already one inseparable unit in `PilotDomainContentAudit.cs`. No batch combines unrelated
decisions merely because they touch the same file (e.g. D5/D6 on `EASY_STANDARD` are correctly split
across B2/B4 because `complexityTier` and `components` are different semantic questions, despite sharing
an artifact).

## Recommended execution order

1. **B1** (D1) — trivial, no dependency, resolves quickly either as removal or a confirmed convention; clears the smallest blocker first.
2. **B5** (D3) — an ownership conversation can start immediately in parallel with research batches; does not block or get blocked by anything else.
3. **B6** (D4) — independent domain-evidence research; can run in parallel with B5/B2.
4. **B2** (D5, D7, D9, D11) — leaf workout artifacts; must precede B3 because B3's `maximumComplexityTier` is the consumer of the tier scale B2 establishes.
5. **B3** (D2) — depends on B2's tier rubric being defined first.
6. **B4** (D6, D8, D10, D12) — leaf workout artifacts; can run in parallel with B2/B3 but is sequenced after them here only to keep workout-family churn (complexity + components) reviewed together per workout rather than interleaved.
7. **B7** (D13) — last: the most evidence-starved decision, most likely to require a product-owner call rather than pure research; sequencing it last avoids blocking the other 6 batches on its resolution.

`B1 → B5/B6 (parallel) → B2 → B3 → B4 → B7`

## Task 7 — evidence readiness

| Decision ID | Readiness state | Minimum next action |
|---|---|---|
| D1 | `POSSIBLY_REMOVE_FIELD` | Determine whether any consumer (Process A validators, Process B contract) reads `sequenceOrder`'s numeric value for anything beyond slot-role shape/count; if unused, draft a removal proposal; if used, escalate to `NEEDS_PRODUCT_DECISION` for a canonical ordering convention |
| D2 | `NEEDS_SOURCE_EXTRACTION` | Search for a Tier 1/2 source addressing intermediate hard-session frequency and main-set dose scaling; if none found (likely, given `progression_rules_v2.yaml` is already ruled out), escalate to `NEEDS_PRODUCT_DECISION` |
| D3 | `NEEDS_PRODUCT_DECISION` | Convene an explicit Process A/Process B ownership conversation to determine whether any DecisionTrace resolver-output label maps onto these three registry condition types |
| D4 | `NEEDS_SOURCE_EXTRACTION` | Search for a Tier 1/2 experience-segmented peak-volume table for TEN_K training; if none found, escalate to `NEEDS_PRODUCT_DECISION` |
| D5, D7, D9, D11 | `NEEDS_PRODUCT_DECISION` | A complexity-tier rubric (what distinguishes tier 1 from tier 2) does not yet exist anywhere in the catalog or its canonical sources; this must be authored/approved before any of the 4 workouts can be classified against it — effectively `BLOCKED_BY_ANOTHER_DECISION` on that not-yet-existing rubric, tracked here as `NEEDS_PRODUCT_DECISION` since the rubric itself is a product/coaching call |
| D6, D8, D10, D12 | `NEEDS_SCHEMA_RECONSIDERATION` | Resolve the open vocabulary-ownership question recorded in `ten-k-pilot-vocabulary-decisions.md`: decide whether `WorkoutComponentType` needs fixture-specific subtypes, or whether the existing generic breakdown is intentionally final |
| D13 | `NEEDS_PRODUCT_DECISION` | Product owner must decide whether to commission new dedicated source research for this specific workout key, approve an explicit default, or remove it from the active closure — no further catalog-side research action is likely to surface evidence that has not already been searched for |

## Decisions requiring product-owner approval

D1 (removal-or-convention call), D2 (near-certain `EXPLICIT_PRODUCT_DEFAULT` given documented source
silence), D3 (the ownership decision **is** the approval), D4 (non-INTERMEDIATE rows, likely
`EXPLICIT_PRODUCT_DEFAULT` if no source is found), D5/D7/D9/D11 (the tier rubric itself is a product
judgment call), D6/D8/D10/D12 (only if resolution requires a schema/vocabulary change), D13 (requires an
explicit call regardless of research outcome — the most certain product-approval requirement of the 13).

## Decisions that may indicate a redundant field

D1 is the only decision in this set carrying a live hypothesis that the underlying field itself may be
unnecessary rather than merely unvalued — see `active-v4-domain-blocker-inventory.md`'s classification
(`POSSIBLY_REDUNDANT_FIELD`). D6/D8/D10/D12 carry a *related* but distinct open question (vocabulary
granularity, not field existence) and are not classified as redundant-field candidates.

## Task 8 — resolution acceptance standard (for later resolution tasks)

This task does not resolve any decision. The following standard governs how later tasks may transition a
decision out of `PLACEHOLDER_UNCONFIRMED`.

### To become `CANONICAL_CONFIRMED`

Require **all** of:
- An atomic claim (one specific field, one specific value, stated as a testable proposition).
- Direct or clearly and explicitly applicable evidence from an approved canonical source (Golden Fixture
  v3, an approved versioned rule file, an approved Plan Generation Decisions document, or the brief) — not
  an inference from a single generated fixture instance realizing one value among several possible ones.
- Source references (exact file path and, where applicable, JSON pointer / section).
- Stated conditions and exclusions (population, plan context, reusable-catalog scope — matching the
  atomic question's own exclusions from `domain-blocker-source-map.md`).
- Conflict analysis: if two approved sources disagree, the higher-precedence source wins per
  `docs/README.md` §1, and the conflict (both paths, both values, selected value, precedence reason,
  affected artifacts/tests) must be recorded.
- Explicit reusable-catalog applicability (does this generalize beyond the one golden-fixture-realized
  plan, or is it scoped narrower).
- No contradiction with Golden Fixture v3 (an approved value may extend beyond what the fixture shows, but
  may never contradict what the fixture explicitly demonstrates).
- No unsupported generalization (a single realized instance is evidence of that instance, never of a
  general policy, per the explicit precedent already applied to D2/D4 in prior passes).

### To become `EXPLICIT_PRODUCT_DEFAULT`

Require **all** of:
- Evidence that canonical sources were searched and do not uniquely determine the value (the negative
  search itself must be documented — which sources were checked, and why they were found insufficient).
- Documented product rationale (why this specific value was chosen absent canonical evidence).
- Bounded applicability (explicitly scoped to this population/plan context, not silently generalized).
- Explicit user/product-owner approval, dated and attributable — not merely "no objection raised."

### To become `TECHNICAL_ONLY`

Require **both** of:
- Proof the value does not encode a running-domain claim (it is structural/mechanical: ordering,
  identifiers, or similar).
- Proof it does not alter training behavior for any generated plan, directly or indirectly through any
  consuming validator or rule.

A decision must **not** be reclassified `TECHNICAL_ONLY` merely to remove it from the Production blocker
count — this is the exact anti-pattern the classification methodology in
`ten-k-pilot-domain-review-summary.md` and `placeholder-scope-audit.md` was built to prevent, and no
decision in this 13-item set (see `active-v4-domain-blocker-inventory.md`'s classification table) currently
qualifies for it.

### To remove a field entirely

Require **both** of:
- Proof the field is unused (no validator, no Process A rule, no Process B contract consumer reads it) or
  semantically redundant with another field already present.
- A schema and compatibility impact analysis: which schema file(s) would change, whether any already-
  published artifact's shape would become invalid under a tightened schema (it would not, if the field is
  simply made optional/removed going forward — but this must be explicitly checked, not assumed), and
  whether any test currently asserts the field's presence.

## Final status: resolution plan complete — 7 batches, recommended order established, no decision resolved.
