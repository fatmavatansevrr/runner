# Phase 4A Corrigendum — Runtime Resolver Decision Set

Documentation-only corrigendum to `PHASE4A_RUNTIME_RESOLVER_DECISION_SET.md`. No resolver, generation, or
catalog-wiring code was written. This corrects the prior draft's canonical alignment and adds a permanent
evidence-log guardrail.

## 0. Critical scoping finding — read before the corrections below

The task that produced this corrigendum instructed correcting Phase 4A against "Appsel V1 Canonical
Decisions" (citing e.g. `doc13-section-12.4`) and described specific expected values (a 5-class
`GOAL_FEASIBILITY_IN` model with `CONSERVATIVE`/`STRETCH`/`CURRENTLY_UNSUPPORTED` bands, a 5–7-week
readiness-gated `TIME_ADEQUACY_IN` band, and a 5-level `PACE_SOURCE_IN` recency confidence ladder).

**A repository-wide search found no document titled or identifiable as "Appsel V1 Canonical Decisions,"
and no `doc13` file of any kind.** Searched: all `.md`/`.json` files repo-wide for `doc13`, `CONSERVATIVE`,
`STRETCH`, `CURRENTLY_UNSUPPORTED`, `recency`, `confidence ladder`; `plan-catalog/docs/` directory tree in
full (`README.md`, `archive/`, `canonical/`, `pending/`, `specifications/`).

The highest-precedence source that **does** exist and **is** canonical per `plan-catalog/docs/README.md`'s
own governance hierarchy (tier 1: "Approved Golden Fixture v3") is
`plan-catalog/docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json` (+
companion `.md`). This is treated below as the best-available canonical evidence. Where it does not
corroborate a value the task described as "expected," that value is **not** asserted as fact — per this
task's own "do not guess" instruction, such values are marked `DECISION_REQUIRED` or
`UNKNOWN_FROM_REPO_EVIDENCE`, not silently accepted from the task prompt's description.

This is itself flagged as a **REVISION_CANDIDATE**-class finding for product: either (a) an "Appsel V1
Canonical Decisions" document exists outside this repository and needs to be added to
`plan-catalog/docs/canonical/` before these three resolvers can be fully specified, or (b) the golden
fixture is the actual source of truth and the 5-class/week-band/confidence-ladder model described in the
corrigendum task needs to be reconciled with what the fixture actually contains (§§1–3 below).

## 1. Three simplifications acknowledged (from the prior Phase 4A draft)

The prior `PHASE4A_RUNTIME_RESOLVER_DECISION_SET.md` is confirmed to have used simplified models, now
corrected/reconciled against actual repo evidence below:

1. `GOAL_FEASIBILITY_IN` was described only as `REALISTIC`/`CHALLENGING`/`UNSUPPORTED` — this collapsed
   both the registry's actual 4-value set (§2) and any richer classification the golden fixture's rule
   file implies (§2).
2. `TIME_ADEQUACY_IN` was described with an invented `>=12/8-11/<8` week rule — this was **not** sourced
   from any repo evidence at all (the golden fixture only exercises the `ADEQUATE` / 12-week path; no
   `COMPRESSED`/`INSUFFICIENT` boundary appears anywhere in the repo). Corrected in §3.
3. `PACE_SOURCE_IN` recency was described with an invented binary `<=90/>90` day rule — also not sourced
   from any repo evidence. Corrected in §4.

---

## 2. Correction 1 — GOAL_FEASIBILITY_IN

**Registry values** (`plan-catalog/catalog/registries/runtime-condition-values.v2.json`, verbatim):
`REALISTIC`, `CHALLENGING`, `UNSUPPORTED`, `NOT_REQUESTED` (4 values).

**Task-described "Appsel canonical" values** (as given in the corrigendum request, not independently
located in any repo document): `CONSERVATIVE`, `REALISTIC`, `CHALLENGING`, `STRETCH`,
`CURRENTLY_UNSUPPORTED` (5 values).

**Golden-fixture-v3 rule-file evidence** (`GOAL_FEASIBILITY_V1`, step `GOAL_FEASIBILITY_RESOLVER`, lines
298–343 of the decisiontrace): only **two** named thresholds exist —
`classificationThresholds.realisticMaxRatio = 0.03` and `classificationThresholds.challengingMaxRatio =
0.06`. The single evaluation instance in the fixture produces `classification: "REALISTIC"` (from
`goalGapRatio = 0.007658`, i.e. within the `realisticMaxRatio` band). **No class boundary beyond
`challengingMaxRatio` is evidenced anywhere in the fixture** — there is no third or fourth ratio threshold,
and no occurrence of the strings `CONSERVATIVE`, `STRETCH`, or `CURRENTLY_UNSUPPORTED` anywhere in the
fixture, the `.md` narrative, or any other repository file.

**Do they match?** No — three-way disagreement:
- Registry (4 values, no ratio boundaries) vs. golden fixture (2 ratio boundaries, output vocabulary not
  spelled out beyond `REALISTIC`/implied `CHALLENGING`) vs. task-described canonical (5 values with named
  percentage bands) all disagree with each other.
- Naming conflict confirmed as flagged by the task: registry says `UNSUPPORTED`; task-described canonical
  says `CURRENTLY_UNSUPPORTED`. Neither name is corroborated by the golden fixture, which never reaches
  that classification in its one evaluated case.

**Classification: `BLOCKED_BY_REGISTRY_CONFLICT`.** Per instruction, the registry and the (task-described,
not repo-located) canonical model are not silently reconciled to either one.

**Corrected threshold table — evidence-only, gaps marked explicitly:**

| Class | Ratio band | Source | Status |
|---|---|---|---|
| (below/at 0 — target equal to or slower than evidence) | `ratio <= 0` (implied only, never evidenced) | Neither registry nor fixture names this class | `DECISION_REQUIRED` |
| `REALISTIC` | `0 < ratio <= 0.03` | Golden fixture `GOAL_FEASIBILITY_V1.realisticMaxRatio = 0.03`; registry has `REALISTIC` as an allowed value | Confirmed value name (registry) + confirmed boundary (fixture) — the only fully evidenced row |
| `CHALLENGING` | `0.03 < ratio <= 0.06` | Golden fixture `challengingMaxRatio = 0.06`; registry has `CHALLENGING` as an allowed value | Confirmed value name + confirmed boundary |
| (task-described `STRETCH`, `0.06 < ratio <= 0.10`) | not evidenced | No repo document names this class or boundary | `DECISION_REQUIRED` |
| `UNSUPPORTED` (registry) / `CURRENTLY_UNSUPPORTED` (task-described) | `ratio > 0.06` (registry-implied) or `ratio > 0.10` (task-described) | Registry names `UNSUPPORTED`; no fixture evidence for the exact boundary or for a `CURRENTLY_UNSUPPORTED` name | `BLOCKED_BY_REGISTRY_CONFLICT` — naming AND boundary both unresolved |
| `NOT_REQUESTED` | N/A (no target time given) | Registry only | Confirmed value name, trigger condition not specified anywhere |

**Required resolution:** product must either (a) supply/point to the actual "Appsel V1 Canonical
Decisions" document so its 5-class model can be verified against real content, or (b) confirm the registry
model (`REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED`) plus the two fixture-evidenced ratio
boundaries (0.03, 0.06) as the actual approved model, with an explicit upper boundary for `UNSUPPORTED`
and a decision on whether a `CONSERVATIVE`/`STRETCH` split is wanted at all — and, if so, a registry
version bump to add those values (registry changes are out of scope for this backend-only corrigendum;
noted for Process A).

---

## 3. Correction 2 — TIME_ADEQUACY_IN

**Registry values:** `ADEQUATE`, `CHALLENGING`... — no, registry has `ADEQUATE`, `COMPRESSED`,
`INSUFFICIENT` (verbatim from `runtime-condition-values.v2.json`).

**Golden-fixture-v3 evidence** (step `TIME_ADEQUACY_RESOLVER`, lines 111–122): `facts.availableFullWeeks =
12`, `facts.defaultCoreWeeks = 12`, `facts.requiredRunwayWeeks = 0` → `result.timeAdequacy = "ADEQUATE"`.
This is the **only** case the fixture exercises. **No week-count boundary for `COMPRESSED` or
`INSUFFICIENT` appears anywhere in the repository** — not in the fixture, not in
`APPSEL_RACE_PLAN_V1 v4` (whose `policies`/`rules` arrays are both empty), not in any archived or pending
document.

**The prior Phase 4A draft's `>=12/8-11/<8` table was not sourced from any repo evidence — confirmed
invented, now removed.** The task's requested replacement (`>=12 ADEQUATE`, `8-11 COMPRESSED`, `5-7
COMPRESSED-only-if-readiness-override-passes`, `<=4 INSUFFICIENT/READINESS_ONLY`) is **also** not evidenced
by any repo document — it is the task prompt's own description, not something located and verified here.

**Corrected treatment:**

| Weeks | Task-described value | Repo evidence | Status |
|---|---|---|---|
| >= 12 | `ADEQUATE` | Fixture confirms `ADEQUATE` at exactly 12 weeks (the only tested point) | Weeks-12 case confirmed; the general `>=12` rule is not separately proven for e.g. 13+ weeks, but is the only directionally consistent reading available |
| 8–11 | `COMPRESSED` | No repo evidence | `DECISION_REQUIRED` |
| 5–7 | `COMPRESSED` only if readiness override passes | No repo evidence for the band, and no repo evidence of a "readiness override" mechanism feeding `TIME_ADEQUACY_IN` at all | `DECISION_REQUIRED` |
| <= 4 | `INSUFFICIENT` or a `READINESS_ONLY`-style value | Registry has no `READINESS_ONLY` value for `TIME_ADEQUACY_IN` — that string only exists in the registry under a **different** condition type, `PLAN_MODE_IN` (`STANDARD/FOCUSED_CORE/COMPRESSED/READINESS_ONLY/COMPLETION_FOCUSED`) | `DECISION_REQUIRED` — runtime vocabulary for `TIME_ADEQUACY_IN` cannot represent `READINESS_ONLY`; per instruction, this is marked `DECISION_REQUIRED` rather than invented. If the product intent is "route to `PLAN_MODE_IN = READINESS_ONLY` instead of failing `TIME_ADEQUACY_IN`," that is a cross-resolver design decision, not a `TIME_ADEQUACY_IN` value change. |

**Confirmed per instruction: this corrigendum does not automatically reject all plans below 8 weeks** — no
`INSUFFICIENT`-for-everything-under-8 rule is asserted; the entire sub-12-week space is `DECISION_REQUIRED`.

**Classification: `PARTIAL_DECISION_SET`** — the `>=12 → ADEQUATE` boundary has fixture evidence; every
other boundary and the readiness-override mechanism itself are undecided.

---

## 4. Correction 3 — PACE_SOURCE_IN recency confidence

**Registry values:** `NONE`, `RECENT_RACE`, `ESTIMATED`, `TARGET_TIME` (unchanged, confirmed present).

**Golden-fixture-v3 evidence:** there is no `PACE_SOURCE_IN`-named resolver step in the fixture at all. The
closest related step is `PACE_CONVERSION` (`RIEGEL_CONVERSION_5K_TO_10K`), which reports a single data
point: `"confidence": "HIGH"`, `"resultAgeDays": 49`. That is **one point on a possible ladder**, not a
ladder definition — the fixture never enumerates confidence bands, day-count boundaries, or a "not usable"
cutoff anywhere.

**The prior Phase 4A draft's `<=90 usable / >90 stale` binary model was not sourced from any repo evidence
— confirmed invented, now removed.** The task's requested 5-level ladder (`0-30 full`, `31-60 high`, `61-90
moderate`, `91-180 low/confirmation-needed`, `>180 not usable`) is **also not evidenced by any repo
document** — again, the task prompt's own description, not verified repo content. Note that the fixture's
one data point (49 days → `HIGH`) is at least *consistent* with the task-described ladder's `31-60 → high
confidence` band, which is a weak positive signal but not confirmation of the full ladder or its exact
boundaries.

**Corrected treatment — all bands DECISION_REQUIRED except the single evidenced data point:**

| Days since result | Task-described confidence | Repo evidence | Status |
|---|---|---|---|
| 0–30 | full confidence | none | `DECISION_REQUIRED` |
| 31–60 | high confidence | fixture's single data point (49 days → `HIGH`) falls in this band — weakly consistent, not confirmatory of the boundary itself | `DECISION_REQUIRED` (boundary unconfirmed; single point is not a boundary proof) |
| 61–90 | moderate confidence | none | `DECISION_REQUIRED` |
| 91–180 | low confidence / confirmation needed | none | `DECISION_REQUIRED` |
| >180 | not usable as pace anchor | none | `DECISION_REQUIRED` |

**What does the ladder affect?** Clarified per instruction — none of this is decided; captured explicitly
so Phase 4B does not have to re-derive the question:
- **`PACE_SOURCE_IN` output value:** DECISION_REQUIRED whether recency degrades `RECENT_RACE` down to
  `ESTIMATED` (or to `NONE`) at some age, or whether recency is orthogonal to the output value entirely.
- **Confidence metadata:** the fixture's own `PACE_CONVERSION` step already carries a `confidence` field
  (`HIGH`) alongside its result — precedent exists for confidence as separate metadata rather than folded
  into the `PACE_SOURCE_IN` value itself, but this is not confirmed as the approved pattern for the
  resolver layer specifically.
- **Decision trace:** the §7 trace shape proposed in the original Phase 4A document (`ReasonCode`,
  `Warnings`) is a plausible carrier for recency confidence; not yet implemented or approved.
- **Warnings:** DECISION_REQUIRED whether a low/moderate-confidence pace should surface a user-facing
  warning (mirrors the fixture's separate `WARNING_POLICY_EVALUATION` step, which exists as its own stage
  downstream of feasibility/readiness — a precedent for warnings being a distinct concern from the
  resolver's own output).
- **Fallback behavior:** DECISION_REQUIRED — no repo evidence of what `PACE_SOURCE_IN` falls back to when
  a race result is present but stale.

**Classification: `PARTIAL_DECISION_SET`** — registry values and one real data point exist; no boundary,
no effect-on-output-value decision, no fallback decision.

---

## 5. Correction 4 — PACE_SOURCE_IN evidence hierarchy mapping

Task-proposed evidence layers (certified race / time trial / structured test / user-reported pace /
effort-only) do not appear anywhere in the repository under those names. The golden fixture's
`INPUT_SNAPSHOT.recentRace` field (`distanceKm`, `timeSeconds`, `date`) is the only evidenced input shape,
and it does not distinguish "certified race" from "time trial" or "structured test" — it is a single
undifferentiated race-result shape.

| Evidence layer (task-proposed) | Recommended registry mapping (task-proposed) | Repo evidence for this specific mapping | Status |
|---|---|---|---|
| certified official race | `RECENT_RACE` | `INPUT_SNAPSHOT.recentRace` exists and feeds `PACE_CONVERSION`; nothing in the fixture distinguishes "certified" from any other race | `DECISION_REQUIRED` (mapping is plausible but the certified/uncertified distinction itself is not evidenced — the fixture has only one undifferentiated race-result shape) |
| time trial | `ESTIMATED` unless canonical doc says race-equivalent | No repo document defines "time trial" as a distinct input type at all | `DECISION_REQUIRED` |
| structured test | `ESTIMATED` | No repo document defines "structured test" as a distinct input type | `DECISION_REQUIRED` |
| user-reported pace | `ESTIMATED` | `GeneratePreviewRequest.PreferredPace` exists (Phase 4A §2) but is documented as a *comfort* pace, not a performance/race-pace report — mapping it to `ESTIMATED` is plausible, not confirmed | `DECISION_REQUIRED` |
| effort-only | `NONE` or `ESTIMATED` if canonical approves | No repo document defines an "effort-only" input at all; no backend field captures it | `DECISION_REQUIRED` |
| target finish time only | `TARGET_TIME` | `GeneratePreviewRequest.TargetFinishTimeSeconds` exists and is the only field that unambiguously maps to `TARGET_TIME` by name/semantics | Only fully evidenced mapping in this table |

**None of these mappings are asserted as approved.** Every row except the last is `DECISION_REQUIRED`, per
instruction ("If any mapping is not supported by canonical evidence, mark DECISION_REQUIRED").

---

## 6. Correction 5 — CORE_ENTRY_READINESS_IN scope

The prior Phase 4A draft did not propose specific km thresholds (it correctly marked the resolver
`BLOCKED_BY_MISSING_INPUT` outright). This corrigendum task's own proposed thresholds (`weekly>=15 &
longest>=6` / `weekly 8-14.9 or longest 4-5.9` / `weekly<8 or longest<4`) are **not** the same numbers found
in repo evidence:

**Golden-fixture-v3 evidence** (step `CORE_ENTRY_READINESS_RESOLVER`, `CORE_ENTRY_READINESS_V1`, lines
73–98): `thresholds.minimumWeeklyVolumeKm = 20`, `thresholds.minimumLongestRunKm = 8`,
`thresholds.minimumRunsPerWeek = 3` → `result.readiness = "STANDARD"`. This is a **single pass/fail-style
entry gate** (one threshold set, one outcome name, `"STANDARD"`), not a 3-tier
`READY`/`CAUTION`/`NOT_READY` model, and the numbers (20 km / 8 km minimum) do not match either this task's
proposed thresholds or the original Phase 4A draft's assumptions.

**Scope classification (A/B/C/D):** **D — not approved / DECISION_REQUIRED**, for three independent
reasons:
1. The task's proposed threshold numbers (15/6, 8-14.9/4-5.9, <8/<4) have no repo evidence at all —
   confirmed not present anywhere.
2. The repo's actual evidenced numbers (20 km / 8 km / 3 runs-per-week) use a completely different
   structure — one minimum-threshold gate producing `"STANDARD"`, not a 3-tier banded output.
3. The registry's `CORE_ENTRY_READINESS_IN` output vocabulary (`READY`/`CAUTION`/`NOT_READY`) does not
   contain `"STANDARD"` at all — this is a **fourth** naming conflict (in addition to the `UNSUPPORTED` vs
   `CURRENTLY_UNSUPPORTED` one in §2), between the fixture's actual output value and the registry's
   allowed-value set. Flagged as `BLOCKED_BY_REGISTRY_CONFLICT` in addition to `DECISION_REQUIRED` on the
   scope question itself.

**Whether this applies to (A) compressed-plan readiness override only, (B) general core-entry readiness, or
(C) both:** the fixture's single evaluation gives no signal either way — the plan in that fixture is a
standard 12-week `ADEQUATE`-timeline plan, not a compressed one, yet `CORE_ENTRY_READINESS_RESOLVER` still
ran and produced `"STANDARD"`. This is weak evidence that the resolver runs for **all** plans (leaning
toward B/C), but is not strong enough to confirm scope without product input — remains `DECISION_REQUIRED`.

---

## 7. Permanent guardrail (added)

> Any newly discovered external evidence must be logged in `plan-catalog/docs/evidence-log.json` with a
> quality label and canonical-alignment status before being cited in any phase document. It may not
> silently alter an approved Appsel threshold.

This guardrail is now recorded in this corrigendum (here) and is proposed as a standing entry in every
future Phase 4B+ guardrail list. See `plan-catalog/docs/evidence-log.md` §"How future phases must use this
log" for the operational detail.

---

## 8. Resolver readiness reclassification

| Resolver | Classification | Why |
|---|---|---|
| `GOAL_FEASIBILITY_IN` | `BLOCKED_BY_REGISTRY_CONFLICT` | Registry (4 values), golden fixture (2 ratio boundaries, no third/fourth class evidenced), and the task-described 5-class canonical model all disagree; `UNSUPPORTED` vs `CURRENTLY_UNSUPPORTED` naming unresolved |
| `TIME_ADEQUACY_IN` | `PARTIAL_DECISION_SET` | `>=12 weeks → ADEQUATE` has fixture evidence; every other band and the readiness-override mechanism are `DECISION_REQUIRED`; registry has no `READINESS_ONLY` value for this condition type |
| `PACE_SOURCE_IN` | `PARTIAL_DECISION_SET` | Registry values confirmed; one real confidence data point (49 days → HIGH) exists but no ladder boundaries, no output-value effect decision, and the full evidence-hierarchy mapping (§5) is `DECISION_REQUIRED` except the `TARGET_TIME` row |
| `CORE_ENTRY_READINESS_IN` | `BLOCKED_BY_PRODUCT_DECISION` + `BLOCKED_BY_REGISTRY_CONFLICT` | Threshold scope (A/B/C/D) is undecided (`BLOCKED_BY_PRODUCT_DECISION`), and the fixture's actual output value `"STANDARD"` does not exist in the registry's `READY`/`CAUTION`/`NOT_READY` vocabulary at all (`BLOCKED_BY_REGISTRY_CONFLICT`) |

This matches the task's own "expected likely result" for 3 of the 4 resolvers; `CORE_ENTRY_READINESS_IN`
came back with an *additional* registry conflict beyond the anticipated `BLOCKED_BY_PRODUCT_DECISION`,
because the fixture evidence itself was inspected (not just the registry) and revealed the `"STANDARD"`
naming mismatch.

---

## Final report

**1. Files inspected:** `PHASE4A_RUNTIME_RESOLVER_DECISION_SET.md`; repo-wide search for `doc13`,
`CONSERVATIVE`, `STRETCH`, `CURRENTLY_UNSUPPORTED`, `VDOT`, `Daniels`, `canonical decisions`;
`plan-catalog/docs/README.md`; `plan-catalog/docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`
(full); `plan-catalog/docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.md` (partial,
pipeline/volume sections); `plan-catalog/catalog/registries/runtime-condition-values.v2.json`;
`plan-catalog/catalog/rule-packs/appsel-race-plan.v4.json`;
`plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v5.json`; `plan-catalog/docs/`
directory tree (`archive/`, `pending/`, `specifications/` listing only, contents not all read);
`plan-catalog-antigravity-brief-v2 (1).md` (partial); `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs`
(referenced from Phase 4A, not re-read).

**2. Files changed:** Three new files created — `plan-catalog/docs/evidence-log.json`,
`plan-catalog/docs/evidence-log.md`, `PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md`. The original
`PHASE4A_RUNTIME_RESOLVER_DECISION_SET.md` was **not** edited — this corrigendum supersedes/corrects it as
a separate document per the task's instruction to "correct or create" the corrigendum file, not to rewrite
the original in place.

**3. evidence-log.json created or updated:** Created (new file), 4 entries (EV-001–EV-004).

**4. evidence-log.md created or updated:** Created (new file), full summary per required sections.

**5. EV-001–EV-004 entries added:**
- EV-001: `GOAL_FEASIBILITY_IN`, quality `PRIMARY_SCIENTIFIC`, status `ACCEPTED_AS_SUPPORTING_EVIDENCE` —
  technique corroborated in-repo (Riegel formula already used by golden-fixture-v3's own
  `RIEGEL_CONVERSION_5K_TO_10K` rule).
- EV-002: `PACE_SOURCE_IN`, quality `UNKNOWN_OR_UNVERIFIED`, status `PROPOSED` — no VDOT/Daniels source
  found anywhere in repo; no bibliographic detail invented.
- EV-003: `CORE_ENTRY_READINESS_IN`, quality `UNKNOWN_OR_UNVERIFIED`, status `PROPOSED` — no training-load
  literature found in repo; noted that repo's own evidence uses a single-value anchor, not a rolling
  4-week average, so this entry is a candidate for future refinement, not a current-behavior match.
- EV-004: `CORE_ENTRY_READINESS_IN`, quality `UNKNOWN_OR_UNVERIFIED`, status `PROPOSED` — no ACSM/
  preparticipation-screening document found in repo.

**6. GOAL_FEASIBILITY_IN registry values:** `REALISTIC`, `CHALLENGING`, `UNSUPPORTED`, `NOT_REQUESTED` (4
values, verbatim from `runtime-condition-values.v2.json`).

**7. GOAL_FEASIBILITY_IN Appsel canonical values:** **Not located in any repository document.** The
5-class model (`CONSERVATIVE`/`REALISTIC`/`CHALLENGING`/`STRETCH`/`CURRENTLY_UNSUPPORTED`) described in the
corrigendum task request could not be verified against any file in this repository. The closest available
tier-1 canonical evidence (golden-fixture-v3's `GOAL_FEASIBILITY_V1` rule) evidences only 2 ratio
boundaries (`realisticMaxRatio=0.03`, `challengingMaxRatio=0.06`) and one observed output (`REALISTIC`).

**8. Whether registry and canonical values match:** No — three-way disagreement between the registry (4
values), the golden fixture (2 boundaries, incomplete class set observed), and the task-described 5-class
model (not repo-located). Classified `BLOCKED_BY_REGISTRY_CONFLICT`.

**9. Corrected GOAL_FEASIBILITY_IN threshold table:** See §2 table — only `REALISTIC` (`<=0.03`) and
`CHALLENGING` (`0.03–0.06`) are evidenced; everything else is `DECISION_REQUIRED` or
`BLOCKED_BY_REGISTRY_CONFLICT`.

**10. Corrected TIME_ADEQUACY_IN threshold table:** See §3 table — only `>=12 weeks → ADEQUATE` is
evidenced (single data point); `8-11`, `5-7`, `<=4` bands and the readiness-override mechanism are all
`DECISION_REQUIRED`; registry has no `READINESS_ONLY` value for this condition type (that string exists
only under the unrelated `PLAN_MODE_IN` condition type).

**11. Corrected PACE_SOURCE_IN recency confidence ladder:** See §4 table — no boundary is evidenced; one
data point (49 days → `HIGH` confidence, on an unrelated `PACE_CONVERSION` step, not a `PACE_SOURCE_IN`
step) is weakly consistent with the task-described `31-60 → high` band but does not confirm the boundary.
All 5 bands marked `DECISION_REQUIRED`.

**12. PACE_SOURCE_IN evidence hierarchy mapping:** See §5 table — only `target finish time only →
TARGET_TIME` is fully evidenced by field semantics; all other rows (`certified race`, `time trial`,
`structured test`, `user-reported pace`, `effort-only`) are `DECISION_REQUIRED`.

**13. CORE_ENTRY_READINESS_IN scope clarification:** **D — not approved / DECISION_REQUIRED**, plus a newly
found `BLOCKED_BY_REGISTRY_CONFLICT`: the golden fixture's actual evidenced output value is `"STANDARD"`
(from a single pass/fail-style gate: `minimumWeeklyVolumeKm=20`, `minimumLongestRunKm=8`,
`minimumRunsPerWeek=3`), which does not exist in the registry's `READY`/`CAUTION`/`NOT_READY` vocabulary at
all. The task's proposed 15/6, 8-14.9/4-5.9, <8/<4 km bands have no repo evidence.

**14. Permanent evidence-log guardrail added:** Yes — recorded in this corrigendum (§7) and in
`plan-catalog/docs/evidence-log.md`'s "How future phases must use this log" section; proposed for the
standing Phase 4B+ guardrail list.

**15. Remaining DECISION_REQUIRED items:** All items in §§2–6 not marked "confirmed" — summarized: (a)
whether an actual "Appsel V1 Canonical Decisions" document exists outside this repo and needs to be added;
(b) `GOAL_FEASIBILITY_IN`'s upper boundary and whether a `CONSERVATIVE`/`STRETCH` split is wanted, plus the
`UNSUPPORTED`/`CURRENTLY_UNSUPPORTED` naming; (c) `TIME_ADEQUACY_IN`'s sub-12-week bands and the
readiness-override mechanism; (d) `PACE_SOURCE_IN`'s recency ladder boundaries and whether recency affects
the output value, confidence metadata, trace, warnings, or fallback; (e) the full evidence-hierarchy
mapping table (§5) except the `TARGET_TIME` row; (f) `CORE_ENTRY_READINESS_IN`'s threshold scope (A/B/C/D)
and reconciling the fixture's `"STANDARD"` output with the registry's 3-value vocabulary.

**16. Resolver readiness classification:** `GOAL_FEASIBILITY_IN` → `BLOCKED_BY_REGISTRY_CONFLICT`;
`TIME_ADEQUACY_IN` → `PARTIAL_DECISION_SET`; `PACE_SOURCE_IN` → `PARTIAL_DECISION_SET`;
`CORE_ENTRY_READINESS_IN` → `BLOCKED_BY_PRODUCT_DECISION` + `BLOCKED_BY_REGISTRY_CONFLICT`.

**17. Whether Phase 4B input-contract work can proceed:** Only the evidence-log mechanism and the
input-availability findings from the original Phase 4A (§2 of that document — which fields exist/don't
exist on `GeneratePreviewRequest`) remain solid ground to build on. No resolver's threshold work can
proceed until: (a) the "Appsel V1 Canonical Decisions" document question is resolved with product, and (b)
the two registry conflicts (`GOAL_FEASIBILITY_IN` naming, `CORE_ENTRY_READINESS_IN` `"STANDARD"` vs.
`READY`/`CAUTION`/`NOT_READY`) are resolved — these are Process A (registry/rule-pack) decisions, not
purely backend ones.

**18. Confirmation no resolver code was implemented:** Confirmed — no `.cs` file was created or modified.

**19. Confirmation no generation was implemented:** Confirmed.

**20. Confirmation no TrainingWeeks/TrainingDays were generated:** Confirmed — no runtime entity instances
were created.

**21. Confirmation no backend runtime behavior was changed:** Confirmed — zero files under `backend/` were
touched in this pass.

**22. Confirmation no plan-catalog artifacts were modified except docs/evidence-log files:** Confirmed —
only `plan-catalog/docs/evidence-log.json` and `plan-catalog/docs/evidence-log.md` were added under
`plan-catalog/`; no `catalog/`, `artifacts/`, `src/`, or `tests/` file was modified.

**23. Anything not completed exactly as specified:** Yes, one deliberate deviation, stated openly rather
than silently worked around: the task's Corrections 1–3 assumed a specific "Appsel V1 Canonical Decisions"
document and specific numeric models (5-class feasibility, week-banded time adequacy, 5-level recency
ladder) that could not be located anywhere in this repository. Rather than writing those task-described
numbers into the corrigendum as if they were verified canonical fact — which would violate this task's own
"do not guess" / "do not invent thresholds" instructions — this corrigendum reports what evidence actually
exists (the golden-fixture-v3 decisiontrace), marks every unconfirmed value `DECISION_REQUIRED` or
`BLOCKED_BY_REGISTRY_CONFLICT`, and flags the missing canonical-decisions document itself as the top
open item for product. EV-002/EV-003/EV-004 were also left at `PROPOSED`/`UNKNOWN_OR_UNVERIFIED` rather
than `ACCEPTED_AS_SUPPORTING_EVIDENCE`, since their specific bibliographic sources are not present in this
repository and were not independently inspected — only EV-001 had in-repo corroboration (the fixture's own
use of the Riegel formula).
