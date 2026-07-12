# Phase 4A.1 — Canonical Artifact Import + Registry Risk Capture

Documentation-only pass. No resolver, generation, registry, or golden-fixture change was made. This
imports a missing canonical source into the repository and records a persistent registry/fixture conflict
as a tracked risk — it does not resolve anything.

## What happened

`PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md` found that "Appsel V1 Canonical Decisions"
(referenced in conversation as `doc13-*`) **was not present anywhere in this repository** — a
repository-wide search for `doc13`, `CONSERVATIVE`, `STRETCH`, `CURRENTLY_UNSUPPORTED` found zero matches
outside the corrigendum's own text. Because it wasn't a repo artifact, no future agent could reliably treat
it as evidence — each pass would either re-derive it from conversation memory or ignore it entirely. This
phase fixes that specific gap: it imports the conversation-provided content as a durable, inspectable file,
and separately records the previously-known `CORE_ENTRY_READINESS_IN` "STANDARD" anomaly as a formal,
tracked activation risk (it existed as a documented observation in two prior passes but had no entry in the
living risk aggregator).

## Imported canonical decision artifact

**Path:** `plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`

Placed inside the existing `docs/canonical/` directory (already the repo's designated canonical location
per `plan-catalog/docs/README.md`), alongside the existing `golden-fixture-v3/` canonical import. No
better-fitting location was found — `docs/specifications/` holds process specs, and `docs/archive/` /
`docs/pending/` are both explicitly non-canonical.

The document explicitly states (per its own §A): it is a product/canonical decision artifact, not a
generated runtime catalog artifact; it does not by itself modify registry values, golden fixtures, or
runtime behavior; the registry remains runtime-vocabulary source of truth and golden fixtures remain test
evidence until explicitly revised; and any conflict must be resolved in a later explicit reconciliation
phase.

**Resolver decisions imported (§B of the artifact), transcribed from conversation content, not
independently verified against any external source:**
- `GOAL_FEASIBILITY_IN`: 5-class model (`CONSERVATIVE`/`REALISTIC`/`CHALLENGING`/`STRETCH`/`CURRENTLY_UNSUPPORTED`), with 2 of the 5 boundaries (`REALISTIC` ≤3%, `CHALLENGING` ≤6%) partially corroborated by the golden fixture's `GOAL_FEASIBILITY_V1` rule (`realisticMaxRatio=0.03`, `challengingMaxRatio=0.06`); the other 3 classes have no fixture corroboration.
- `TIME_ADEQUACY_IN` (10K): `>=12→ADEQUATE` (fixture-confirmed at exactly 12 weeks), `8-11→COMPRESSED`, `5-7→readiness-gated COMPRESSED`, `<=4→insufficient/readiness-only (vocabulary undecided)` — only the first row has fixture evidence.
- `PACE_SOURCE_IN` recency ladder: 5 bands (0-30/31-60/61-90/91-180/>180 days), with one weak corroborating data point (49 days → `HIGH` confidence, on an unrelated `PACE_CONVERSION` step) falling in the 31-60 band.
- `PACE_SOURCE_IN` evidence hierarchy: 5 unranked categories (certified race / time trial / structured test / user-reported pace / effort-only), imported as a list only — no mapping to registry values (`NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME`) is decided.
- `CORE_ENTRY_READINESS_IN`: scope note only (compressed-override-only vs. general vs. both — undecided); references the fixture's one concrete gate (`minimumWeeklyVolumeKm=20`, `minimumLongestRunKm=8`, `minimumRunsPerWeek=3` → `"STANDARD"`) without resolving the STANDARD/registry mismatch.

## Evidence-log update

`plan-catalog/docs/evidence-log.json` and `.md` both updated (append-only) with **EV-005**:
`relatedResolver=MULTIPLE`, `relatedCanonicalDecision=appsel-v1-canonical-decisions`,
`source=plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`,
`qualityLabel=INTERNAL_CANONICAL_DECISION`, `conflictsWithCanonical=true`, `status=PROPOSED` (not
`ACCEPTED_AS_INTERNAL_CANONICAL_SOURCE` — no reconciliation has run yet, so it is not treated as accepted).
EV-001 through EV-004 were left unmodified (append-only discipline preserved).

## STANDARD anomaly risk entry

Added **`TD-REGISTRY-001`** to `plan-catalog/artifacts/audits/activation-readiness-risks.json` and `.md`
(the existing living aggregator — confirmed to already exist and already track 3 prior risks:
`TD-D3-001`, `TD-WAVE5-001`, `TD-BACKEND-001`). Title: "CORE_ENTRY_READINESS registry/fixture vocabulary
mismatch." Classification `REGISTRY_FIXTURE_CONFLICT`, severity `BLOCKS_RESOLVER_IMPLEMENTATION`, status
`OPEN`. Cross-referenced to `docs/canonical/appsel-v1-canonical-decisions.md` §C.2 and to
`PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md` §6, where this anomaly was independently
rediscovered.

## Current unresolved conflicts (none resolved by this pass)

1. `GOAL_FEASIBILITY_IN`: imported 5-class model vs. registry's 4-value set — count and naming both
   disagree (`UNSUPPORTED` vs. `CURRENTLY_UNSUPPORTED`).
2. `CORE_ENTRY_READINESS_IN`: registry (`READY`/`CAUTION`/`NOT_READY`) vs. fixture (`"STANDARD"`) — tracked
   as `TD-REGISTRY-001`.
3. `TIME_ADEQUACY_IN`: only the `>=12→ADEQUATE` boundary is evidenced; the `8-11`/`5-7`/`<=4` rows and the
   readiness-override mechanism have no registry or fixture support.
4. `PACE_SOURCE_IN`: only one confidence data point is evidenced; the 5-level ladder and the
   evidence-hierarchy-to-registry mapping are both undecided.

## Why Phase 4B remains blocked

Phase 4B (input-contract work for the four resolvers) requires a settled target vocabulary and threshold
set to build an input contract against. As of this pass, that target is unsettled in two independent ways:
(a) the registry itself doesn't agree with the newly-imported canonical document for
`GOAL_FEASIBILITY_IN`, and (b) the registry doesn't agree with the repo's own tier-1 golden-fixture
evidence for `CORE_ENTRY_READINESS_IN`. Building an input contract before either conflict is resolved risks
designing inputs for a vocabulary that will change under it. Per this phase's own explicit constraint
("Phase 4B input-contract work must not proceed until canonical decision sources are reconciled"), Phase 4B
is not started here.

## Recommended next phase

**Phase 4A.2 — Runtime Condition Registry and Fixture Reconciliation Proposal.** Scope: for each of the 4
open conflicts above, propose (not silently apply) a reconciliation — e.g. a `runtime-condition-values.v3`
registry candidate, and/or a corrected golden-fixture-v4 candidate, and/or an explicit product decision
recorded back into `appsel-v1-canonical-decisions.md` — each with the same "propose, don't apply" discipline
used in this pass, leaving actual registry/fixture edits and version bumps to a still-later, explicitly
approved implementation phase.

## Final report

**1. Files inspected:** `PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md`;
`plan-catalog/docs/evidence-log.json`/`.md`; `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`;
`plan-catalog/docs/README.md`; `plan-catalog/catalog/registries/runtime-condition-values.v2.json`;
`plan-catalog/docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`
(re-referenced, not re-read in full — content already captured in the corrigendum this pass builds on).

**2. Files changed:** 2 new files — `plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`,
`PHASE4A_1_CANONICAL_ARTIFACT_IMPORT_AND_RISK_CAPTURE.md` (this document). 3 append-only edits —
`plan-catalog/docs/evidence-log.json` (added EV-005), `plan-catalog/docs/evidence-log.md` (added EV-005 row),
`plan-catalog/artifacts/audits/activation-readiness-risks.json` and `.md` (added `TD-REGISTRY-001`, updated
`finalStatus`/risk count).

**3. Whether Appsel V1 canonical decision artifact was created:** Yes.

**4. Exact path:** `plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`.

**5. Summary of canonical resolver decisions imported:** See "Imported canonical decision artifact" above
— 5-class `GOAL_FEASIBILITY_IN` model, 10K `TIME_ADEQUACY_IN` week bands, `PACE_SOURCE_IN` 5-level recency
ladder, `PACE_SOURCE_IN` 5-category evidence hierarchy (unmapped), `CORE_ENTRY_READINESS_IN` scope note.
All imported as proposed/unreconciled content, not binding thresholds.

**6. Evidence-log entries added/updated:** `EV-005` added to both `evidence-log.json` and `evidence-log.md`
(`INTERNAL_CANONICAL_DECISION`, `conflictsWithCanonical=true`, `status=PROPOSED`). EV-001–EV-004 unchanged.

**7. Whether activation-readiness-risks artifact was found:** Yes —
`plan-catalog/artifacts/audits/activation-readiness-risks.json` and `.md`, an existing living aggregator
already containing 3 risks (`TD-D3-001`, `TD-WAVE5-001`, `TD-BACKEND-001`).

**8. STANDARD anomaly risk ID added:** `TD-REGISTRY-001`.

**9. Exact registry values for CORE_ENTRY_READINESS_IN:** `READY`, `CAUTION`, `NOT_READY` (verbatim from
`catalog/registries/runtime-condition-values.v2.json`).

**10. Exact fixture value(s) found for readiness:** `"STANDARD"` (from
`golden-10k-intermediate-4d-12w.v3.decisiontrace.json`, step `CORE_ENTRY_READINESS_RESOLVER`,
`result.readiness`). No other readiness value appears anywhere else in the fixture.

**11. Confirmation registry/fixture conflict remains OPEN:** Confirmed — `TD-REGISTRY-001` status is
`OPEN`; not marked resolved by this pass.

**12. Confirmation GOAL_FEASIBILITY_IN registry/product conflict remains OPEN:** Confirmed — documented as
unresolved in `appsel-v1-canonical-decisions.md` §C.1; no registry change was made.

**13. Confirmation no registry values were changed:** Confirmed —
`catalog/registries/runtime-condition-values.v2.json` was read-only inspected, not edited; no v3 registry
was created.

**14. Confirmation no golden fixtures were changed:** Confirmed — `docs/canonical/golden-fixture-v3/*` was
read-only inspected (in the prior corrigendum pass; not re-read here), not edited.

**15. Confirmation no resolver code was implemented:** Confirmed — no `.cs` file was touched.

**16. Confirmation no generation was implemented:** Confirmed.

**17. Whether Phase 4B can proceed:** No — blocked, per "Why Phase 4B remains blocked" above, until the 4
open conflicts are reconciled.

**18. Recommended next phase:** Phase 4A.2 — Runtime Condition Registry and Fixture Reconciliation
Proposal.

**19. Anything not completed exactly as specified:** All requested artifacts were created/updated at the
exact suggested paths and with the exact suggested IDs (`EV-005`, `TD-REGISTRY-001`). One clarification:
the evidence-log entry's `status` was set to `PROPOSED` rather than `ACCEPTED_AS_INTERNAL_CANONICAL_SOURCE`
— per the task's own phrasing ("PROPOSED or ACCEPTED_AS_INTERNAL_CANONICAL_SOURCE, depending on repo
convention") this was a judgment call, made conservatively: since the artifact itself documents unresolved
conflicts with the registry and fixtures, marking it "accepted" as a canonical source before any
reconciliation has run would overstate its current standing.
