# Evidence Log

Append-only log of external/supporting evidence considered for Appsel runtime-resolver decisions
(`GOAL_FEASIBILITY_IN`, `PACE_SOURCE_IN`, `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN`, and any future
runtime condition resolver). Structured companion to `evidence-log.json`, which is the machine-readable
source of truth — this file is a human-readable summary and must stay consistent with it.

## Purpose

Runtime resolver thresholds are product decisions, not engineering decisions. Over time, external
evidence (scientific literature, coaching-system documentation, product-tool precedent) may be proposed
to support, challenge, or motivate revision of an approved Appsel threshold. Without a log, such evidence
could be cited informally in a phase document and silently shift a threshold without a recorded trail.
This log exists to make that impossible: **no external evidence may be cited in any phase document unless
it is logged here first**, with an explicit quality label and canonical-alignment status.

This log does **not** itself set or change thresholds. It only records evidence and its relationship to
already-approved canonical decisions. Changing an approved threshold still requires explicit product
approval, recorded separately (e.g. in a corrigendum or a future canonical decisions document).

## Source hierarchy

1. **Appsel Canonical Decisions** — product threshold source of truth. As of this log's creation, no
   repository document could be located that is explicitly titled or identifiable as "Appsel V1 Canonical
   Decisions" (searched for `doc13`, `canonical decisions`, and related terms — zero matches). The
   highest-precedence located source consistent with `plan-catalog/docs/README.md`'s own governance
   hierarchy (tier 1: "Approved Golden Fixture v3") is
   `plan-catalog/docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`
   and its companion `.md`. Where this document is silent or does not corroborate a claimed canonical
   value, that value is **not** treated as canonical fact by this log or by the Phase 4A corrigendum —
   see `PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md` for the full accounting.
2. **plan-catalog registry/artifacts** — runtime vocabulary and catalog contract source of truth (e.g.
   `catalog/registries/runtime-condition-values.v2.json`, `catalog/rule-packs/appsel-race-plan.v4.json`).
3. **Evidence log external sources** (this file / `evidence-log.json`) — supporting, challenging, or
   revision-candidate evidence only. Never authoritative on its own; always subordinate to tiers 1 and 2.

## Allowed quality labels

- `INTERNAL_CANONICAL_DECISION`
- `PRIMARY_SCIENTIFIC`
- `COACHING_SYSTEM_SOURCE`
- `REVIEW_OR_META_ANALYSIS`
- `PRODUCT_TOOL_SUPPORT`
- `PREPRINT_OR_WEAK_SUPPORT`
- `UNKNOWN_OR_UNVERIFIED`

## Allowed statuses

- `PROPOSED`
- `UNDER_REVIEW`
- `ACCEPTED_AS_SUPPORTING_EVIDENCE`
- `ACCEPTED_REVISES_CANONICAL`
- `REJECTED_KEPT_CANONICAL`
- `DECISION_CONFLICT`
- `REVISION_CANDIDATE`

## Rules

- External evidence may support, challenge, or motivate revision of a canonical decision.
- External evidence must never silently override an approved Appsel canonical decision.
- If external evidence conflicts with an approved canonical decision, it must be logged with status
  `DECISION_CONFLICT` or `REVISION_CANDIDATE` — never silently reconciled.
- Any canonical threshold change requires explicit, separately recorded product approval. This log is
  evidence bookkeeping, not an approval mechanism.
- Entries are append-only. Do not edit or delete a prior entry's substantive fields after it is committed;
  add a new entry (with a note referencing the prior `evidenceId`) if a correction is needed.

## Current evidence entries

| ID | Resolver | Quality label | Status | Source verified in-repo? |
|---|---|---|---|---|
| EV-001 | GOAL_FEASIBILITY_IN | PRIMARY_SCIENTIFIC | ACCEPTED_AS_SUPPORTING_EVIDENCE | Technique (Riegel formula) corroborated in-repo via golden-fixture-v3's `RIEGEL_CONVERSION_5K_TO_10K` rule; the specific bibliographic citation itself was not independently verified by inspection of the original 1981 publication. |
| EV-002 | PACE_SOURCE_IN | UNKNOWN_OR_UNVERIFIED | PROPOSED | No — no VDOT/Daniels source exists anywhere in this repository. |
| EV-003 | CORE_ENTRY_READINESS_IN | UNKNOWN_OR_UNVERIFIED | PROPOSED | No — no training-load/recent-load-window literature exists anywhere in this repository. |
| EV-004 | CORE_ENTRY_READINESS_IN | UNKNOWN_OR_UNVERIFIED | PROPOSED | No — no ACSM or preparticipation-screening document exists anywhere in this repository. |
| EV-005 | MULTIPLE | INTERNAL_CANONICAL_DECISION | PROPOSED | Yes — the source is itself an in-repo artifact, `plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`, imported in Phase 4A.1. `conflictsWithCanonical=true`: its 5-class `GOAL_FEASIBILITY_IN` model conflicts with the registry's 4-value set, and it does not resolve the `CORE_ENTRY_READINESS_IN` STANDARD-vs-registry anomaly (tracked as risk `TD-REGISTRY-001`). Left `PROPOSED`, not accepted — no reconciliation has run yet. |
| EV-006 | MULTIPLE | INTERNAL_CANONICAL_DECISION | ACCEPTED_AS_SUPPORTING_EVIDENCE (re-upgraded) | Yes — source is `PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md`. `conflictsWithCanonical=false`. History: `PROPOSED` → `ACCEPTED_AS_SUPPORTING_EVIDENCE` → downgraded to `PROPOSED` (provenance clarification found `OWNER_APPROVAL_NOT_EVIDENCED`) → **re-upgraded to `ACCEPTED_AS_SUPPORTING_EVIDENCE`** following explicit product-owner approval recorded 2026-07-10 (quoted verbatim: "V1 için runtime registry simple kalacak. Onaylıyorum."). Does not close `TD-REGISTRY-001` (separate fixture-defect matter, remains `OPEN`) or change EV-005's status (remains `PROPOSED`, a broader/distinct question). |
| EV-007 | PACE_SOURCE_IN | INTERNAL_CANONICAL_DECISION | ACCEPTED_AS_SUPPORTING_EVIDENCE | Yes — source is `docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json` (`INPUT_SNAPSHOT` + `PACE_CONVERSION` steps). Direct fixture evidence: `confirmDate` and `planStartDate` are distinct fields, and `PACE_CONVERSION`'s `ageReferenceDate` matches `confirmDate`, not `planStartDate`. Basis for `RuntimeResolverContext.AsOfDate` (Phase 4D.2) — an internal, non-public field; `PaceSourceResolver` never reads the wall clock. Elevates a Phase 4D.2 in-document finding into the durable evidence log per the Phase 4D.2.5 alignment pass. |
| EV-008 | CORE_ENTRY_READINESS_IN | INTERNAL_CANONICAL_DECISION | ACCEPTED_AS_SUPPORTING_EVIDENCE | Yes — source is the explicit owner decision recorded in `PHASE4D_3_1_CORE_ENTRY_READINESS_THRESHOLD_DECISION_AND_RESOLVER_ACTIVATION.md`. Internal product decision, not external scientific sourcing: READY = weekly≥15 AND longest≥6; NOT_READY = weekly<8 OR longest<4 (checked before READY); CAUTION otherwise (both present) or when exactly one field is present; both missing → NOT_READY only in `GoalType=Race` context, else `NotEvaluated`. `RecentRunsPerWeek` is metadata-only, never a hard gate. Implemented in `CoreEntryReadinessResolver` (Phase 4D.3.1). Does not close `TD-REGISTRY-001` (separate `STANDARD` fixture defect) or change EV-005's status. |

Full structured detail (supports/conflictsWithCanonical/notes) for each entry lives in `evidence-log.json`
— this table is a summary index only; `evidence-log.json` is authoritative if the two ever disagree.

## How future phases must use this log

1. Before citing **any** external (non-repo) evidence in a phase document, add an entry here first with a
   quality label and a status. Do this even for evidence that seems obviously supportive — the log exists
   precisely so "obviously supportive" claims are still recorded and auditable.
2. Do not invent bibliographic details. If the exact source cannot be verified by actually inspecting it
   (in-repo or otherwise), use quality label `UNKNOWN_OR_UNVERIFIED` and status `PROPOSED`, and say
   explicitly what was and was not checked.
3. If new evidence conflicts with an approved Appsel canonical decision or an approved registry value, the
   entry must be marked `DECISION_CONFLICT` or `REVISION_CANDIDATE` — it must never be used to silently
   change a threshold, a registry value, or a phase document's stated rule.
4. Changing an approved canonical threshold on the basis of logged evidence still requires a separate,
   explicit product approval step, recorded outside this log (e.g. a corrigendum document referencing the
   relevant `evidenceId`(s)).
5. This log is append-only. Future phases add entries; they do not rewrite or remove prior ones.
