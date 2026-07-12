# Appsel V1 Canonical Decisions — Imported Product Decision Artifact

## Status of this document

**This is a product/canonical decision artifact, not a generated runtime catalog artifact.** It does not
by itself modify `catalog/registries/runtime-condition-values.v2.json`, any golden fixture under
`docs/canonical/golden-fixture-v3/`, or any backend runtime behavior. Nothing in this document is active,
published, or authoritative over the runtime vocabulary until an explicit, separately recorded
reconciliation phase resolves the conflicts listed in §C below.

**Why this document exists:** `PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md` found that a document
referred to in conversation as "Appsel V1 Canonical Decisions" (with citations like `doc13-section-12.4`)
was **not present anywhere in this repository** — confirmed by a repository-wide search for `doc13`,
`CONSERVATIVE`, `STRETCH`, `CURRENTLY_UNSUPPORTED`, and related terms, all returning zero matches outside
that corrigendum's own text. Because the document did not exist as a repo artifact, no future agent could
treat it as reliable, inspectable canonical evidence — every future pass would either have to re-derive it
from conversation memory (unreliable) or ignore it. This file imports the conversation-provided content as
a durable, versioned repository artifact so it can be inspected, cited, and reconciled going forward,
**without** pretending it was already an approved, in-repo source before now.

**Content provenance:** the specific numeric bands and vocabulary in §B below are transcribed from the
conversation turn that requested this import ("Phase 4A.1 — Canonical Artifact Import + Registry Risk
Capture"), not independently derived from any other repository file. Where a value is also independently
evidenced elsewhere in the repo (e.g. golden-fixture-v3), that corroboration is noted; where it is not,
that is noted too. No numeric threshold here has been verified against an external published source — this
is a transcription-and-import step, not a verification step.

## Location rationale

Placed at `plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`, inside the existing
`docs/canonical/` directory, per the task's own suggested path. `plan-catalog/docs/README.md` already
designates `docs/canonical/` as "the" approved canonical directory (tier-1 hierarchy position), and already
contains one prior example of an imported canonical artifact (`golden-fixture-v3/`). No better-fitting
existing location was found: `docs/specifications/` holds process/authoring specs, not product-threshold
decisions; `docs/archive/` and `docs/pending/` are both explicitly non-canonical per `docs/README.md`. This
file does **not** modify `docs/README.md`'s existing precedence list in this pass — see §C and the
handoff note for why that update is deferred to reconciliation.

---

## A. Purpose and authority

- This document is the **product threshold source of truth** for the four runtime resolvers
  (`GOAL_FEASIBILITY_IN`, `PACE_SOURCE_IN`, `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN`), as imported
  from the conversation that supplied it.
- `catalog/registries/runtime-condition-values.v2.json` **remains the runtime vocabulary source of truth**
  until explicitly revised through a recorded reconciliation phase. Where this document's proposed values
  disagree with the registry, the registry still governs actual runtime behavior — nothing here overrides
  it.
- Golden fixtures under `docs/canonical/golden-fixture-v3/` **remain test evidence** until explicitly
  revised. Where this document's proposed values disagree with fixture-evidenced behavior, the fixture
  still governs what is considered tested/proven behavior — nothing here overrides it.
- This document is a **source for future reconciliation work**, not a completed reconciliation. Any
  conflict between this document and the registry or fixtures must be resolved in a later, explicit
  reconciliation phase (recommended: **Phase 4A.2 — Runtime Condition Registry and Fixture Reconciliation
  Proposal**) — not silently, and not by this document alone.

## B. Resolver-related canonical decisions (as imported, unverified against external sources)

### B.1 `GOAL_FEASIBILITY_IN` product model (5-class)

| Class | Band |
|---|---|
| `CONSERVATIVE` | target equal to or slower than evidence |
| `REALISTIC` | 0–3% faster than evidence |
| `CHALLENGING` | >3–6% faster |
| `STRETCH` | >6–10% faster |
| `CURRENTLY_UNSUPPORTED` | >10% faster |

Partial corroboration: `docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`,
step `GOAL_FEASIBILITY_RESOLVER`, rule file `GOAL_FEASIBILITY_V1`, evidences exactly two ratio boundaries
(`realisticMaxRatio = 0.03`, `challengingMaxRatio = 0.06`) consistent with the `REALISTIC` and `CHALLENGING`
rows above. **No fixture evidence exists for `CONSERVATIVE`, `STRETCH`, or `CURRENTLY_UNSUPPORTED`** — those
three rows are imported from conversation content only, unverified elsewhere in the repo.

### B.2 `TIME_ADEQUACY_IN` 10K product model

| Weeks until race | Value |
|---|---|
| >= 12 | `ADEQUATE` |
| 8–11 | `COMPRESSED` |
| 5–7 | readiness-gated compressed — `COMPRESSED` only if a readiness override passes |
| <= 4 | insufficient, or a readiness-only path — **exact runtime vocabulary requires reconciliation** |

Partial corroboration: the fixture's `TIME_ADEQUACY_RESOLVER` step evidences exactly the `>= 12 weeks →
ADEQUATE` case (`availableFullWeeks=12` → `timeAdequacy="ADEQUATE"`). No fixture evidence exists for the
`8–11`, `5–7`, or `<=4` rows, nor for any "readiness override" mechanism. The registry
(`runtime-condition-values.v2.json`) has no value resembling "readiness-only" under `TIME_ADEQUACY_IN`
specifically — a `READINESS_ONLY` string exists only under the unrelated `PLAN_MODE_IN` condition type.

### B.3 `PACE_SOURCE_IN` recency confidence ladder

| Age of pace evidence | Confidence |
|---|---|
| 0–30 days | full confidence |
| 31–60 days | high confidence |
| 61–90 days | moderate confidence |
| 91–180 days | low confidence / confirmation needed |
| >180 days | not usable as pace anchor |

Partial corroboration: the fixture's `PACE_CONVERSION` step (not a `PACE_SOURCE_IN` step — no such step
exists in the fixture) evidences one data point: `resultAgeDays=49` → `confidence="HIGH"`, which falls in
the 31–60 day band above and is weakly consistent with "high confidence" in that band, but does not confirm
the band boundary itself (one point cannot prove a boundary). No other band is evidenced anywhere in the
repo.

### B.4 `PACE_SOURCE_IN` evidence hierarchy (unranked list, no registry mapping decided)

- certified race
- time trial
- structured test
- user-reported pace
- effort-only

No repository document defines these five categories or maps them to the registry's actual
`PACE_SOURCE_IN` values (`NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME`). This list is imported verbatim
from conversation content as a starting point for future mapping work — **it is not itself a mapping**, and
no mapping decision is asserted by this document.

### B.5 `CORE_ENTRY_READINESS_IN` note

- Previously discussed thresholds may include weekly-volume, longest-run, and runs-per-week readiness
  gates (for reference, the golden fixture's `CORE_ENTRY_READINESS_V1` rule evidences one concrete gate:
  `minimumWeeklyVolumeKm=20`, `minimumLongestRunKm=8`, `minimumRunsPerWeek=3` → output `"STANDARD"` — see
  §C for why that output value itself is a registry conflict, tracked separately as `TD-REGISTRY-001`).
- **Scope is not decided** and must be clarified before implementation: does a readiness gate apply to (a)
  compressed-plan readiness override only, (b) general core-entry readiness for all plans, or (c) both?
  This document does not answer that question — it is recorded as open in §C and in
  `PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md` §6.
- Compressed-readiness thresholds must **not** be silently broadened into general core-entry thresholds, or
  vice versa, without an explicit scope decision.

## C. Known conflicts requiring reconciliation

None of the following are resolved by this document. All remain open pending
**Phase 4A.2 — Runtime Condition Registry and Fixture Reconciliation Proposal** (or a differently named but
equivalent explicit reconciliation phase).

### C.1 `GOAL_FEASIBILITY_IN` conflict

- Appsel V1 canonical product model (§B.1, as imported): 5 values
  (`CONSERVATIVE`/`REALISTIC`/`CHALLENGING`/`STRETCH`/`CURRENTLY_UNSUPPORTED`).
- `catalog/registries/runtime-condition-values.v2.json` currently defines 4 values:
  `REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED`.
- These sets neither match in count nor in every name (`UNSUPPORTED` vs. `CURRENTLY_UNSUPPORTED`;
  `CONSERVATIVE` and `NOT_REQUESTED` each appear in only one of the two lists).
- **Requires registry reconciliation before implementation.** Not performed in this pass.

### C.2 `CORE_ENTRY_READINESS_IN` conflict

- Registry values (`runtime-condition-values.v2.json`): `READY` / `CAUTION` / `NOT_READY`.
- Golden fixture (`golden-10k-intermediate-4d-12w.v3.decisiontrace.json`, step
  `CORE_ENTRY_READINESS_RESOLVER`) currently evidences `readiness = "STANDARD"` — a value absent from the
  registry's allowed set entirely.
- **Requires fixture/registry mapping or correction.** Tracked as risk `TD-REGISTRY-001` (§3 of this pass,
  in `activation-readiness-risks.json`/`.md`). Not resolved in this pass.

### C.3 `TIME_ADEQUACY_IN` incompleteness

- Existing fixture evidence covers only the `>= 12 weeks → ADEQUATE` case (confirmed, single data point).
- Sub-12-week readiness-gated compressed behavior (the `8–11`, `5–7`, `<=4` rows in §B.2) has **no** fixture
  or registry corroboration and requires reconciliation.

### C.4 `PACE_SOURCE_IN` incompleteness

- Existing fixture evidence covers only one recency/confidence data point (`49 days → HIGH`, on the
  `PACE_CONVERSION` step, not a `PACE_SOURCE_IN` step).
- The full 5-level confidence ladder (§B.3) and the evidence-hierarchy mapping (§B.4) both require
  reconciliation.

## V1 Runtime Scope and Trace Metadata Resolution

Added in Phase 4A.3 — Owner Scope Approval for Resolver Vocabulary and Trace Metadata; provenance
corrected by the subsequent Phase 4A.3 Owner Approval Provenance Clarification pass. **This section
records a proposed V1 scope decision (`PROPOSED_SCOPE_DECISION`), not a confirmed product-owner approval —
a dedicated provenance check found `OWNER_APPROVAL_NOT_EVIDENCED`; see
`PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md`'s "Provenance clarification" section for the
full finding.** It supersedes nothing in §A–§D above; it proposes *where* the richer bands described in §B
should live for the current implementation, without deleting or demoting them, pending actual owner
review.

**Some canonical product bands are richer than the current runtime registry values.** §B.1's 5-class
`GOAL_FEASIBILITY_IN` model, §B.2's sub-8-week `TIME_ADEQUACY_IN` behavior, and §B.3's 5-level
`PACE_SOURCE_IN` recency ladder are all richer than what
`catalog/registries/runtime-condition-values.v2.json` currently expresses as runtime-gating vocabulary.

**For V1, runtime eligibility registry stays simple.** The approved V1 runtime registry outputs are,
unchanged: `GOAL_FEASIBILITY_IN` = `REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED`;
`TIME_ADEQUACY_IN` = `ADEQUATE`/`COMPRESSED`/`INSUFFICIENT`; `PACE_SOURCE_IN` =
`NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME`. No registry v3 is introduced by this decision.
`GOAL_PACE_REHEARSAL`'s existing `requires` clause (the only live catalog consumer of any of these
condition types) is unaffected.

**Richer bands are preserved as trace metadata / UX explanation, not lost.** `CONSERVATIVE`/`STRETCH`
(goal feasibility), the sub-8-week readiness-gated compressed behavior (time adequacy, via
`PLAN_MODE_IN.READINESS_ONLY` rather than a new `TIME_ADEQUACY_IN` value), and the recency confidence
ladder / evidence hierarchy (pace source) all remain part of the approved product model — they are
recorded here as intended to surface via future decision-trace metadata fields (e.g.
`aggressivenessBand`, `paceRecencyConfidence`, `paceEvidenceLayer`, `paceEvidenceAgeDays`,
`paceSourceReasonCode`) and UX copy, not deleted or deprioritized. Full detail:
`PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md`.

**Registry expansion can be reconsidered in a future version if stage eligibility needs finer control.**
This V1 scope decision is not permanent — if a future catalog stage is authored that genuinely needs to
gate on, e.g., `CONSERVATIVE` vs. `REALISTIC` (not just display it), a registry v3 proposal can be raised
at that time, following the same "propose, don't silently apply" discipline used throughout this
reconciliation track. No such stage exists today (confirmed: `GOAL_PACE_REHEARSAL` remains the only
consumer, gating only on `REALISTIC`/`CHALLENGING`).

**Does not affect `TD-REGISTRY-001` or the `CORE_ENTRY_READINESS_IN`/`STANDARD` defect.** That item is a
fixture defect (§C.2 above), not a scope-richness question, and remains open pending golden-fixture-v4.

## D. Evidence-log relationship

- External sources must be logged in `plan-catalog/docs/evidence-log.json` (with a quality label and
  status) **before** being cited in any phase document. This document itself is logged there as `EV-005`
  (see below) — the import of this file is treated as citable evidence only from the point it was logged,
  not retroactively.
- External evidence may support or challenge the canonical decisions recorded here, but it may **not**
  silently revise an approved threshold. No threshold in this document is "approved" in the sense of being
  live/binding on runtime behavior — see §A. Approval, if it happens, is a separate, explicit, recorded
  step.
