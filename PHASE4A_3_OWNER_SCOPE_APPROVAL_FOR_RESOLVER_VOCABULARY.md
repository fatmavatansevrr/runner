# Phase 4A.3 — Owner Scope Approval for Resolver Vocabulary and Trace Metadata

Documentation-only. No registry value, golden fixture, resolver code, backend runtime behavior, or
generation logic was changed. This records a V1 scope decision building on the options Phase 4A.2
recommended — it converts "recommended option" into structured scope documentation.

## Provenance clarification (added by Phase 4A.3 Owner Approval Provenance Clarification pass)

**OWNER_APPROVAL_NOT_EVIDENCED.** A dedicated provenance-clarification pass searched this conversation and
the repository for evidence of an explicit, attributable product-owner sign-off on the V1 scope decisions
below (simple runtime registries for V1; richer Appsel V1 bands as trace metadata only; no registry
expansion for `GOAL_FEASIBILITY_IN`; no `READINESS_ONLY` value in `TIME_ADEQUACY_IN`; recency confidence as
trace metadata rather than a registry value). **No such evidence was found.** This document's original
title and framing ("Owner Scope Approval") were written by the assistant at the requesting task's
instruction to "document explicit owner approval for V1 scope" — that instruction was itself an imperative
to produce documentation, not a record of a distinct product-owner reviewing and signing off on the
content. There is no message in this conversation from an identified product-owner role approving these
specific points, and no repository file (commit, PR review, sign-off log, or similar) records such
approval either.

**Correct status label for everything below: `PROPOSED_SCOPE_DECISION`, not `OWNER_APPROVED`.** Every
"approved" statement in §A–§D and in the "Phase 4B readiness" section below should be read as **the
assistant's structured recommendation, carried forward from Phase 4A.2's analysis**, awaiting actual
product-owner review — not as a decision an owner has already ratified. The document's title is left
unchanged (renaming would break the cross-references already made to this exact filename from
`appsel-v1-canonical-decisions.md` and `evidence-log.json`/`.md`), but its status is downgraded here,
explicitly, per this clarification pass's own finding.

No new owner approval is being provided in this same pass either — this clarification pass is a provenance
check, not an approval-granting event. If and when an actual product owner reviews this content and
approves it, that approval should be recorded as a further, separate addendum, clearly attributed (who,
when, what exact scope), not folded silently into this document's existing "approved" language.

## Addendum — explicit owner approval recorded (2026-07-10)

Following the provenance clarification above (`OWNER_APPROVAL_NOT_EVIDENCED` as of that pass), the product
owner subsequently provided explicit approval, quoted verbatim as given:

> "V1 için runtime registry simple kalacak. Onaylıyorum."
>
> (Translation for context, not a substitute for the quoted original: "The runtime registry will stay
> simple for V1. I approve.")

**Product owner explicitly approved the V1 scope decision: runtime condition registries remain simple for
V1; richer Appsel V1 product bands are retained as decision trace / UX metadata; no registry expansion is
required before Phase 4B input-contract work.**

This approval covers exactly the scope documented in §A–§D above:
- Simple runtime registry values for V1 (`GOAL_FEASIBILITY_IN`: `REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED`;
  `TIME_ADEQUACY_IN`: `ADEQUATE`/`COMPRESSED`/`INSUFFICIENT`; `PACE_SOURCE_IN`: `NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME`).
- Richer Appsel V1 bands (`CONSERVATIVE`/`STRETCH`, the sub-8-week `TIME_ADEQUACY_IN` behavior, the
  `PACE_SOURCE_IN` recency ladder/evidence hierarchy) retained as decision-trace/UX metadata only, not
  registry values.
- No registry expansion (no v3) required before Phase 4B.
- No `READINESS_ONLY` value added to `TIME_ADEQUACY_IN` — the sub-4-week case continues to route through
  the existing `PLAN_MODE_IN.READINESS_ONLY` value.

**What remains outside this approval's scope, unchanged:** this approval covers the V1 *vocabulary
richness* scope decision only. It does **not** approve, resolve, or close the separate
`CORE_ENTRY_READINESS_IN`/`STANDARD` fixture-defect question (§D above, tracked as `TD-REGISTRY-001`),
which is a defect-correction matter, not a scope-richness matter, and remains `OPEN` pending a future
golden-fixture-v4 correction. It also does not itself implement Phase 4B, resolver logic, or generation —
those remain separate, future work.

With this addendum, the status of the V1 scope decision recorded in §A–§D is upgraded from
`PROPOSED_SCOPE_DECISION` (per the provenance clarification pass) to **owner-approved**, specifically and
only for the points listed above.

## A. GOAL_FEASIBILITY_IN V1 scope approval

**Approved V1 runtime registry output** (unchanged, matches `catalog/registries/runtime-condition-values.v2.json`
verbatim): `REALISTIC`, `CHALLENGING`, `UNSUPPORTED`, `NOT_REQUESTED`.

**The richer product model** from `docs/canonical/appsel-v1-canonical-decisions.md` §B.1 —
`CONSERVATIVE`, `REALISTIC`, `CHALLENGING`, `STRETCH`, `CURRENTLY_UNSUPPORTED` — is retained, but **as trace
metadata, not as a runtime registry value.**

**Explicit scope decision:**
- `CONSERVATIVE` and `STRETCH` will **not** be added to `runtime-condition-values` v2 or a future v3 for
  the current V1 implementation.
- They may be emitted later as decision-trace metadata (e.g. an `aggressivenessBand` fact), following the
  precedent already present in the golden fixture's own `GOAL_FEASIBILITY_RESOLVER` step, which already
  carries `facts.goalGapRatio`/`facts.goalGapPercentDisplay` alongside its coarse `classification` result.
- They may be used for UX explanation (e.g. showing a user "your goal is a stretch" language) without any
  registry change.
- They do **not** directly control stage eligibility in V1.
- `GOAL_PACE_REHEARSAL` eligibility continues to use the simple registry output exactly as it does today —
  `catalog/workout-progressions/ten-k-workout-progression.v5.json`'s `requires: [{conditionType:
  GOAL_FEASIBILITY_IN, allowedValues: [REALISTIC, CHALLENGING]}]` is unaffected by this scope decision and
  is not proposed to change.

**Naming:** the runtime registry keeps `UNSUPPORTED` (not `CURRENTLY_UNSUPPORTED`). Product copy /
UX text may describe this state to a user as "currently unsupported" in natural language, but the
underlying runtime condition value remains the registry's `UNSUPPORTED` string unless a future registry
version explicitly changes it through its own separate, recorded decision. This document does not rename
anything in the registry.

## B. TIME_ADEQUACY_IN V1 scope approval

**Approved V1 runtime registry** (unchanged): `ADEQUATE`, `COMPRESSED`, `INSUFFICIENT`.

**Documented intentionally, as approved V1 scope:**
- The 5–7 week 10K case is represented as `COMPRESSED` plus a `CORE_ENTRY_READINESS_IN` gate — not as a new
  `TIME_ADEQUACY_IN` value. (Note: this composition still requires the `CORE_ENTRY_READINESS_IN`/`STANDARD`
  fixture defect — §D below — to be resolved before it can be implemented; this scope approval does not
  itself unblock resolver implementation, only the vocabulary question.)
- The `<=4` week readiness-only route uses the **already-existing** `PLAN_MODE_IN.READINESS_ONLY` value
  (confirmed present in both `runtime-condition-values.v1.json` and `.v2.json`'s `PLAN_MODE_IN` allowed-value
  set) — not a new `TIME_ADEQUACY_IN` value.
- `READINESS_ONLY` is **not** added as a `TIME_ADEQUACY_IN` value, in this pass or as a planned future one
  under this scope decision.
- This is a conscious design choice to keep schedule adequacy (a pure calendar-math question: is there
  enough time) separate from plan mode (a broader plan-shape question that already has its own
  `PLAN_MODE_IN` condition type and its own `STANDARD`/`FOCUSED_CORE`/`COMPRESSED`/`READINESS_ONLY`/
  `COMPLETION_FOCUSED` vocabulary). Conflating the two was exactly the failure mode diagnosed as the root
  cause of the `CORE_ENTRY_READINESS_IN`/`STANDARD` defect (§D) — this scope decision deliberately avoids
  repeating that mistake in a second condition-type pairing.
- A future resolver implementation must evaluate `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN`, and
  `PLAN_MODE_IN` **together** where the 5–7 week and `<=4` week cases are concerned — no single resolver in
  isolation can represent that behavior. This is noted as an implementation-sequencing requirement for
  whichever future phase actually implements resolver logic; it is not implemented here.

## C. PACE_SOURCE_IN V1 scope approval

**Approved V1 runtime registry** (unchanged): `NONE`, `RECENT_RACE`, `ESTIMATED`, `TARGET_TIME`.

**Documented intentionally, as approved V1 scope:**
- The recency confidence ladder (0–30/31–60/61–90/91–180/>180 days) is stored as decision-trace metadata,
  not as a registry output — following the precedent already present in the golden fixture's own
  `PACE_CONVERSION` step, which already carries `"confidence": "HIGH"` and `"resultAgeDays": 49` as facts
  alongside its result.
- The evidence-hierarchy details (certified race / time trial / structured test / user-reported pace /
  effort-only) are likewise trace metadata for V1, not registry values.
- No `TIME_TRIAL` / `STRUCTURED_TEST` / `USER_REPORTED_PACE` registry values are added in V1.
- A future resolver **may** produce fields such as `paceEvidenceLayer`, `paceRecencyConfidence`,
  `paceEvidenceAgeDays`, `paceSourceReasonCode` — but these are decision-trace/metadata fields, not runtime
  condition values, and are not implemented by this document. Their exact shape remains
  `DECISION_REQUIRED` for whichever future phase actually implements the `PACE_SOURCE_IN` resolver.

## D. CORE_ENTRY_READINESS_IN fixture defect (restated, not re-decided)

- Registry values remain `READY` / `CAUTION` / `NOT_READY` — unchanged, no `STANDARD` value is added to any
  registry version.
- `STANDARD` in the golden fixture (`docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`,
  step `CORE_ENTRY_READINESS_RESOLVER`) is a **fixture defect**, caused by accidental reuse of the
  `PLAN_MODE_IN.STANDARD` value for an unrelated condition type — confirmed via Phase 4A.2's investigation
  (the fixture's own, separate, correctly-wired `PLAN_MODE_RESOLVER` step independently and correctly
  produces `STANDARD` for `PLAN_MODE_IN`, and the D3 follow-up audit had already flagged this exact mismatch
  as pre-dating any registry version change).
- A future golden-fixture-v4 should correct this to `READY`, unless later evidence proves otherwise. No
  such evidence has been found as of this pass.
- **`TD-REGISTRY-001` remains `OPEN`** until golden-fixture-v4 is produced and validated (or an explicit,
  owner-approved interim compatibility-mapping document is published with a mandatory fixture-correction
  follow-up, per Phase 4A.2 §3/§9). This pass does not close it, and does not itself produce fixture v4.

## Phase 4B readiness

Following this scope decision (per the provenance clarification above: assistant-recommended,
`PROPOSED_SCOPE_DECISION`, not owner-ratified), Phase 4B is classified:

**`READY_TO_PROCEED_WITH_INPUT_CONTRACT_ONLY`**

This specific classification does not depend on the vocabulary-scope question being owner-approved: the
approved-safe input fields below were independently justified in Phase 4A.2 by direct golden-fixture
evidence (matching `INPUT_SNAPSHOT` field shapes), are purely additive/nullable, and are read by no
resolver — so they carry no risk from the vocabulary scope question remaining `PROPOSED_SCOPE_DECISION`
rather than owner-approved. Resolver-vocabulary threshold work, by contrast, does still require actual
owner sign-off before proceeding, per the provenance finding above.

Approved safe input fields (unchanged from Phase 4A.2 §11, re-confirmed here as still evidence-backed and
unaffected by anything decided in this pass): `recentLongestRunKm`, `recentWeeklyVolumeKm`,
`recentRunsPerWeek`, `recentRaceDistanceKm`, `recentRaceFinishTimeSeconds`, `recentRaceDate`.

Still withheld, unchanged: `paceEvidenceType`, `paceEvidenceDate` — both remain tied to the
`PACE_SOURCE_IN` evidence-hierarchy mapping question, which is trace-metadata scope now (§C above) but
still has no approved field shape or allowed-value set. This scope-approval pass narrows *where* that
richness lives (trace metadata, not registry) but does not itself decide the metadata's shape — that
remains a separate, future decision, so these two fields stay withheld.

This classification applies strictly to **input-contract work** (optional, nullable fields carried through
preview/confirm payloads, read by no resolver). It does not apply to, and does not unblock, resolver
threshold implementation for any of the four condition types — those remain gated on the owner decisions
already listed in Phase 4A.2 §12, now partially satisfied by this document (items 1–3) but not items 4–6
(the `CORE_ENTRY_READINESS_IN`/`STANDARD` fix itself, golden-fixture-v4 commissioning, and
`paceEvidenceType`/`paceEvidenceDate` timing).

---

## Final report

**1. Files inspected:** `PHASE4A_2_RUNTIME_CONDITION_CONFLICT_CLASSIFICATION_AND_RECONCILIATION_PROPOSAL.md`;
`plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`; `plan-catalog/docs/evidence-log.json`/`.md`;
`plan-catalog/catalog/registries/runtime-condition-values.v1.json` and `.v2.json` (re-confirmed values, not
re-derived); `catalog/workout-progressions/ten-k-workout-progression.v5.json` (re-confirmed
`GOAL_PACE_REHEARSAL` requires clause unaffected).

**2. Files changed:** New — `PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md` (this document).
Updated (append-only, new section added, nothing removed) —
`plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`. Updated (append-only) —
`plan-catalog/docs/evidence-log.json` and `.md` (EV-006 added).

**3. GOAL_FEASIBILITY V1 scope decision:** Registry stays at 4 values
(`REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED`); the 5-class product model
(`CONSERVATIVE`/`REALISTIC`/`CHALLENGING`/`STRETCH`/`CURRENTLY_UNSUPPORTED`) is approved as trace metadata
only, not as registry values, and does not control `GOAL_PACE_REHEARSAL` eligibility. `UNSUPPORTED` naming
is retained in the registry regardless of product copy wording.

**4. TIME_ADEQUACY V1 scope decision:** Registry stays at 3 values
(`ADEQUATE`/`COMPRESSED`/`INSUFFICIENT`); `READINESS_ONLY` is explicitly **not** added to
`TIME_ADEQUACY_IN` — the `<=4` week case routes through the already-existing `PLAN_MODE_IN.READINESS_ONLY`
value instead. The 5–7 week case is approved as `COMPRESSED` + a `CORE_ENTRY_READINESS_IN` gate (still
blocked on the `STANDARD` fixture defect for actual implementation).

**5. PACE_SOURCE V1 scope decision:** Registry stays at 4 values
(`NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME`); the recency confidence ladder and evidence-hierarchy
details are approved as trace metadata only. No `TIME_TRIAL`/`STRUCTURED_TEST`/`USER_REPORTED_PACE`
registry values are added.

**6. CORE_ENTRY_READINESS / STANDARD decision:** Restated, not re-decided — confirmed fixture defect,
recommended fix is golden-fixture-v4 correcting `STANDARD`→`READY`; `TD-REGISTRY-001` **remains OPEN**, not
closed by this pass.

**7. Whether Appsel V1 canonical artifact was updated:** Yes — a new section, "V1 Runtime Scope and Trace
Metadata Resolution," was appended to `plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md` (see
next tool operation for its exact content); nothing in the document's existing §A–§D was removed or altered.

**8. Evidence-log update:** `EV-006` appended to both `plan-catalog/docs/evidence-log.json` and `.md`.
EV-001 through EV-005 left unmodified (append-only discipline preserved).

**9. EV-005 status after this pass:** Unchanged — still `PROPOSED`. This pass does not broaden or accept
EV-005; it adds a new, separately-scoped EV-006 instead, per the task's explicit preference.

**10. EV-006 entry:** `relatedResolver=MULTIPLE`, `relatedCanonicalDecision=phase4a3-owner-scope-approval`,
`source=PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md`,
`qualityLabel=INTERNAL_CANONICAL_DECISION`, `conflictsWithCanonical=false`,
`status=ACCEPTED_AS_SUPPORTING_EVIDENCE`. Note: `ACCEPTED_AS_INTERNAL_CANONICAL_SOURCE` is **not** one of
the allowed status values defined in `evidence-log.md`'s "Allowed statuses" list (`PROPOSED`,
`UNDER_REVIEW`, `ACCEPTED_AS_SUPPORTING_EVIDENCE`, `ACCEPTED_REVISES_CANONICAL`, `REJECTED_KEPT_CANONICAL`,
`DECISION_CONFLICT`, `REVISION_CANDIDATE`) — confirmed by direct re-read of that file. Per the task's own
fallback instruction ("If allowed statuses do not include ACCEPTED_AS_INTERNAL_CANONICAL_SOURCE, use the
closest existing accepted status and explain"), `ACCEPTED_AS_SUPPORTING_EVIDENCE` was used instead: this
scope-approval document is being cited as evidence supporting the V1 implementation-scope decision, and
`conflictsWithCanonical=false` because it does not contradict any existing registry/fixture value — it only
scopes where the richer product bands live (metadata vs. registry), which is consistent with, not
contradictory to, the current registry.

**11. Confirmation registry values were not changed:** Confirmed — `runtime-condition-values.v2.json` was
read-only re-confirmed, not edited; no v3 file was created.

**12. Confirmation golden fixtures were not changed:** Confirmed — no file under
`docs/canonical/golden-fixture-v3/` was edited; no v4 file was created.

**13. Confirmation resolver code was not implemented:** Confirmed — no `.cs` file was touched.

**14. Confirmation generation was not implemented:** Confirmed.

**15. Confirmation TD-REGISTRY-001 remains open:** Confirmed — `activation-readiness-risks.json`/`.md` were
not modified in this pass; the entry's `status` field remains `OPEN` as set in Phase 4A.1.

**16. Whether Phase 4B can proceed:** Yes — classified `READY_TO_PROCEED_WITH_INPUT_CONTRACT_ONLY`.

**17. Approved safe input fields for Phase 4B:** `recentLongestRunKm`, `recentWeeklyVolumeKm`,
`recentRunsPerWeek`, `recentRaceDistanceKm`, `recentRaceFinishTimeSeconds`, `recentRaceDate`. Still
withheld: `paceEvidenceType`, `paceEvidenceDate`.

**18. Anything not completed exactly as specified:** One judgment call, explained: the task offered
`ACCEPTED_AS_SUPPORTING_EVIDENCE` or `ACCEPTED_AS_INTERNAL_CANONICAL_SOURCE` for EV-006 "depending on
evidence-log allowed statuses." Since the latter is not an allowed status (confirmed by direct inspection
of `evidence-log.md`), `ACCEPTED_AS_SUPPORTING_EVIDENCE` was used, exactly as the task's own fallback
instruction directed. No other deviation.
