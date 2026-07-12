# Phase 4A.2 — Runtime Condition Conflict Classification and Reconciliation Proposal

Documentation-only. No registry value, golden fixture, resolver code, backend runtime behavior, or
generation logic was changed. This proposes reconciliation options; it does not apply any of them.

## 1. Executive summary

Four open items from Phase 4A.1 are not the same kind of problem. Investigation (§3) found strong,
multi-source evidence that the `CORE_ENTRY_READINESS_IN` "STANDARD" anomaly is a **genuine legacy defect**
— `STANDARD` is a real, valid value, but of a *different* condition type (`PLAN_MODE_IN`), and the fixture
appears to have mistakenly reused it for the unrelated `CORE_ENTRY_READINESS_IN` output across two fixture
generations (v2 and v3). The other three items (`GOAL_FEASIBILITY_IN`, `TIME_ADEQUACY_IN`,
`PACE_SOURCE_IN`) are genuine **product-scope decisions** — the current registry/fixture is not "wrong,"
it is simply less rich than the newly-imported Appsel V1 canonical proposal, and richness is a product
choice, not a bug fix. Recommended: fix the defect on its own track (golden-fixture-v4, later, low risk);
route the three scope items to an explicit product-owner decision before any registry v3 work begins.
Phase 4B (input-contract only, no resolver logic) can proceed **now**, under a constrained, evidence-backed
list of stable input fields — see §10/§11.

## 2. Conflict classification matrix

| Issue | Classification | Basis |
|---|---|---|
| A. `CORE_ENTRY_READINESS_IN` STANDARD mismatch | `DEFECT_OR_LEGACY_ARTIFACT` | `STANDARD` is a real value — but of `PLAN_MODE_IN`, not `CORE_ENTRY_READINESS_IN`. Confirmed present verbatim in both `runtime-condition-values.v1.json` and `.v2.json`'s `PLAN_MODE_IN` set. Confirmed absent from `CORE_ENTRY_READINESS_IN`'s set in both versions. §3 for full trace. |
| B. `GOAL_FEASIBILITY_IN` 4-value registry vs. 5-class proposed canonical | `PRODUCT_SCOPE_EXPANSION` | Registry's 4 values are internally consistent and fully cover the one live catalog dependency (`GOAL_PACE_REHEARSAL` needs only `REALISTIC`/`CHALLENGING`). The proposed 5th/6th nuance (`CONSERVATIVE`, `STRETCH`) adds explainability richness, not a bug fix. |
| C. `TIME_ADEQUACY_IN` 5–7 week readiness-gated compressed band | `PRODUCT_SCOPE_EXPANSION` + `DECISION_REQUIRED` | No registry or fixture evidence contradicts the current 3-value model; the proposed band is new scope, and whether it needs a new registry value or can be represented with existing values + a cross-resolver gate is undecided. |
| D. `PACE_SOURCE_IN` 5-level recency confidence ladder | `PRODUCT_SCOPE_EXPANSION` + `DECISION_REQUIRED` | Registry's 4 source-type values are not contradicted by the ladder — the ladder is a *confidence* dimension, orthogonal to *source type*. Whether it belongs in the registry or in decision-trace metadata is undecided. |

`REGISTRY_VERSIONING_DECISION` and `FIXTURE_UPDATE_REQUIRED` are downstream consequences of B/C/D's
resolution, not separate open issues — see §7/§8. None of the four issues are
`DOCUMENTATION_ONLY_CONFLICT` (all have a real behavioral/vocabulary dimension) or purely
`UNKNOWN_FROM_REPO_EVIDENCE` (each has at least partial, specific repo evidence, cited above).

---

## 3. CORE_ENTRY_READINESS_IN / STANDARD anomaly investigation

**Where exactly does STANDARD appear?**
- `docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json` line 94:
  step `CORE_ENTRY_READINESS_RESOLVER` → `result.readiness = "STANDARD"`.
- Same file, line 395: a later step's `facts` snapshot repeats `"readiness": "STANDARD"` (carried-forward
  reference to the same resolver output, not a second occurrence).
- Same file, line 417 area: a **separate, later** step, `PLAN_MODE_RESOLVER`, independently produces
  `"result": "STANDARD"` (line 423), with `messageKey: "PLAN_MODE_STANDARD"` and `"planMode": "STANDARD"`
  (line 441) — this second occurrence of the string `"STANDARD"` is **correct and expected**: it is the
  `PLAN_MODE_IN` condition type's own legitimate value.
- `docs/archive/golden-fixture-v2/golden-10k-intermediate-4d-12w_v2_decisiontrace.json` line 34: the
  **prior fixture generation** already has `"readiness": "STANDARD"` at its own Core Entry Readiness step
  — confirming this is not new to v3.
- `docs/archive/golden-fixture-v2/golden-10k-intermediate-4d-12w_v2.md` line 30: `| 3 | Core Entry
  Readiness | **STANDARD**, gaps=[] |` — the human-readable narrative table for v2 also names the Core
  Entry Readiness step's output as `STANDARD`, confirming this was an intentional (if mistaken) authored
  value, not a stray field.

**Does STANDARD appear outside the golden fixture?** No occurrence of `CORE_ENTRY_READINESS_IN` combined
with `STANDARD` was found in `catalog/`, `artifacts/appsel-plan-catalog/` (published snapshots), or any
backend file. `STANDARD` itself appears in many unrelated contexts (`EASY_STANDARD`, `LONG_RUN_STANDARD`
workout keys) which are irrelevant homonyms, not related to this condition-type question.

**Was STANDARD part of any older registry, audit note, source map, or D3 follow-up?**
- **Older registry:** `catalog/registries/runtime-condition-values.v1.json` — `CORE_ENTRY_READINESS_IN`
  allowed values were `READY`/`NOT_READY`/`UNKNOWN`. `STANDARD` was never a `CORE_ENTRY_READINESS_IN` value
  in v1 either.
- **Same v1 registry, different condition type:** `PLAN_MODE_IN` allowed values were
  `STANDARD`/`FOCUSED_CORE`/`COMPRESSED`/`READINESS_ONLY`/`COMPLETION_FOCUSED` — `STANDARD` **is** and
  always was a legitimate `PLAN_MODE_IN` value, unchanged from v1 to v2.
- **Audit note:** `artifacts/audits/domain-d3-followup.json` (`readinessObservation` field, and the
  matching prose in `domain-d3-followup.md` line 42) **already documented this exact mismatch**, explicitly
  stating: *"'STANDARD' matches NEITHER v1 (READY/NOT_READY/UNKNOWN) NOR v2 (READY/CAUTION/NOT_READY)
  CORE_ENTRY_READINESS_IN vocabulary. This mismatch pre-dates and is unaffected by the D3 change — it was
  already true under v1."* This confirms the anomaly was known before Phase 4A, just never converted into
  a tracked risk entry until `TD-REGISTRY-001` (Phase 4A.1).

**Does repository evidence suggest STANDARD means READY, ADEQUATE, STANDARD_PLAN, or something else?**
Strong evidence for **READY**: the `CORE_ENTRY_READINESS_RESOLVER` step's full evaluation (`ruleId:
TEN_K_STANDARD_ENTRY`, thresholds `minimumWeeklyVolumeKm=20`/`minimumLongestRunKm=8`/`minimumRunsPerWeek=3`,
all satisfied, `gaps: []`) describes exactly the semantics of "user meets baseline entry criteria with no
flagged gaps" — which is the plain-language meaning of `READY` in the registry's 3-value model
(`READY`/`CAUTION`/`NOT_READY`). No repo evidence supports `ADEQUATE` (that string belongs to
`TIME_ADEQUACY_IN`, a different condition type entirely, and mixing them would be a second, worse
conflation) or any `STANDARD_PLAN`-style value (not present anywhere in any registry version).

**Is the golden fixture outdated?** For this one field, yes — evidenced by the fact that the *separate*,
correctly-named `PLAN_MODE_RESOLVER` step in the **same fixture** independently and correctly produces
`STANDARD` for `PLAN_MODE_IN`. This is the strongest single piece of evidence: the fixture's authors had
`STANDARD` correctly wired to `PLAN_MODE_IN` in one step, and (most plausibly) copy/reused or conflated the
label when authoring the earlier `CORE_ENTRY_READINESS_RESOLVER` step — most likely because, in v1's now-
superseded `PLAN_MODE_IN` vocabulary, `STANDARD` was already the "default/no special mode" value, and an
author generalized that label to the readiness step by mistake or by an early, since-abandoned modeling
choice (e.g., early drafts might have used a shared "STANDARD vs special-case" vocabulary across multiple
condition types before the registry differentiated them per-type — this specific causal history is
`UNKNOWN_FROM_REPO_EVIDENCE`, only the resulting mismatch is proven).

**Is the registry missing a value?** No evidence supports this. The registry's `CORE_ENTRY_READINESS_IN`
set (`READY`/`CAUTION`/`NOT_READY`) is a complete, sensible 3-tier readiness model on its own; nothing
about the fixture's actual evaluated facts (a single pass/fail-style gate) requires a 4th value.

**Is a compatibility mapping needed?** Only as a temporary, explicitly-labeled bridge if fixture correction
is deferred — see Option C below. Not needed permanently if the fixture itself is corrected.

### Recommended option

**Option A — treat STANDARD as a legacy fixture bug; correct it to `READY` in a future golden-fixture-v4.**
Rationale: this is the only option consistent with all evidence — the registry is not missing anything, the
fixture's *other* correctly-modeled `PLAN_MODE_RESOLVER` step proves `STANDARD`'s legitimate home is
`PLAN_MODE_IN` not `CORE_ENTRY_READINESS_IN`, and the D3 follow-up already flagged this as unaffected by any
registry version change (i.e., not a registry-versioning problem). Option B (add `STANDARD` to a future
registry v3) is not recommended — it would permanently enshrine what the evidence indicates is a labeling
mistake, and would create a **second** condition type that also accepts `STANDARD`, worsening ambiguity
between `PLAN_MODE_IN` and `CORE_ENTRY_READINESS_IN` outputs rather than resolving it.

**Interim step (does not require touching the fixture now):** Option C — record a documented, temporary
interpretation mapping (`STANDARD → READY`, fixture-scope only) in the reconciliation tracking, so any
future consumer reading the v3 fixture knows how to interpret the field without waiting for v4. This
mapping is **documentation only** in this pass — no fixture byte is changed here.

### TD-REGISTRY-001 closure condition

`TD-REGISTRY-001` closes when **either**: (a) a golden-fixture-v4 is generated with
`CORE_ENTRY_READINESS_RESOLVER.result.readiness` corrected to `READY` (Option A, preferred), or (b) an
explicit, owner-approved compatibility-mapping document is published stating `STANDARD` is fixture-legacy
and equals `READY` for all consumption purposes, with a mandatory follow-up ticket to still eventually
correct the fixture (Option C, interim-only — does not permanently close without a follow-up). It does
**not** close via Option B (adding `STANDARD` to the registry), per the reasoning above. It remains `OPEN`
after this pass — no closure action was taken here.

---

## 4. GOAL_FEASIBILITY_IN product-scope decision options

**Which values are currently consumed by live catalog stages?** Only `REALISTIC` and `CHALLENGING` —
confirmed via `catalog/workout-progressions/ten-k-workout-progression.v5.json`, stage
`GOAL_PACE_REHEARSAL`: `"requires": [{"conditionType": "GOAL_FEASIBILITY_IN", "allowedValues":
["REALISTIC", "CHALLENGING"]}]`, with `fallbackStageKey: CURRENT_FITNESS_SPECIFIC_REHEARSAL`. This is the
**only** `requires` clause referencing `GOAL_FEASIBILITY_IN` anywhere in the catalog (confirmed by grep in
Phase 4A, re-confirmed here — `appsel-race-plan.v4.json`'s `rules`/`policies` arrays are both empty, so no
rule-pack-level consumption exists either).

**Does `GOAL_PACE_REHEARSAL` currently require only `REALISTIC`/`CHALLENGING`?** Yes, confirmed exactly as
above — `UNSUPPORTED` and `NOT_REQUESTED` both fall through to the fallback stage; `CONSERVATIVE` and
`STRETCH` are not referenced at all (they don't exist in the registry to be referenced).

**Is `NOT_REQUESTED` needed as no-target-time output?** Plausible by naming but **not confirmed** by any
repo document — no rule anywhere states what triggers `NOT_REQUESTED`. Registry declares the value exists;
no consumer or resolver-facing rule defines its trigger condition. `DECISION_REQUIRED` (unchanged from
Phase 4A's finding).

**Are `CONSERVATIVE`/`STRETCH` required for current v10 runtime behavior, or product expansion?** Product
expansion — confirmed not required: `v10`'s only consumer (`GOAL_PACE_REHEARSAL`) has a binary
`in-band/out-of-band` gate (`REALISTIC`/`CHALLENGING` vs. everything else → fallback). A 5-class model would
not change v10's actual stage-selection behavior at all unless a *future* catalog stage is authored to
specifically consume `CONSERVATIVE` or `STRETCH` — none exists today.

**Should `CURRENTLY_UNSUPPORTED` be renamed to `UNSUPPORTED` for registry consistency?** Recommended yes,
*if* the 5-class model is adopted at all — `UNSUPPORTED` is the already-registered, already-consumable-by-
convention name; introducing a differently-spelled synonym (`CURRENTLY_UNSUPPORTED`) for the same concept
serves no evidenced purpose and would only fragment the vocabulary further. This is a naming
recommendation conditional on Option B below being chosen — it is not itself a recommendation to adopt
Option B.

### Recommended option

**Option C — keep registry runtime values simple (`REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED`
unchanged), and store the richer aggressiveness band (`CONSERVATIVE`/`STRETCH` distinction, and the exact
ratio) as decision-trace metadata instead of a registry value.** Rationale: the *only* live catalog
consumer needs a coarse in-band/out-of-band signal, which the current registry already provides losslessly.
The 5-class model's value is explainability/UX (showing a user *how* challenging their goal is), which is a
presentation/reporting concern, not a stage-gating concern — the golden fixture already demonstrates this
pattern (`GOAL_FEASIBILITY_RESOLVER`'s `facts.goalGapRatio`/`goalGapPercentDisplay` already carry the
continuous, precise number *alongside* the coarse `classification` value). Extending that existing pattern
(richer facts/metadata next to a simple registry-gated classification) is lower-migration-cost than a
registry v3 bump, and does not put `GOAL_PACE_REHEARSAL`'s existing `requires` clause at any compatibility
risk.

**Product tradeoff, stated explicitly for the owner decision:**
- *Richer UX/explainability (Option B, registry v3)* — lets the product surface "your goal is a stretch"
  vs. "your goal is realistic" distinctly to users, and lets future catalog stages gate on finer bands if
  ever authored. Cost: a registry version bump, a migration/compat question for `TEN_K__4D__INTERMEDIATE`
  v10 (§6), and touches `GOAL_PACE_REHEARSAL`'s `requires` clause surface even if its actual allowed-list
  content doesn't change (the condition type it references would now have more possible values it must
  correctly ignore).
- *Simpler runtime vocabulary (Option C, metadata-only)* — zero registry/fixture/catalog migration cost,
  zero risk to `GOAL_PACE_REHEARSAL`'s existing behavior, and the ratio/band richness is still fully
  available for UX via decision-trace metadata (which the fixture already models for exactly this purpose).
  Cost: if a *future* catalog stage genuinely needs to gate on `CONSERVATIVE` vs. `REALISTIC` at the catalog
  level (not just for display), metadata alone won't suffice and a registry value will eventually be needed
  anyway — this is a "defer, don't foreclose" recommendation, not a permanent rejection of Option B.
- Option A (revise the canonical doc to drop `CONSERVATIVE`/`STRETCH` entirely) is not recommended — it
  would discard real product input without evidence that it's wrong, rather than finding a lower-risk way
  to honor it (Option C).

---

## 5. TIME_ADEQUACY_IN product-scope decision options

**Can `ADEQUATE`/`COMPRESSED`/`INSUFFICIENT` represent the 5–7 week readiness-gated compressed behavior?**
Partially. `COMPRESSED` can represent "compressed but proceeding," and `INSUFFICIENT` can represent "not
proceeding" — but the *conditional* nature of the 5–7 week band ("compressed only if a readiness check
passes") is not representable by `TIME_ADEQUACY_IN` alone; it requires a second signal
(`CORE_ENTRY_READINESS_IN`, per the task's own framing) to decide *which* of `COMPRESSED`/`INSUFFICIENT`
applies in that week range. No repo evidence shows the two resolvers were ever designed to compose this way
— `DECISION_REQUIRED`.

**Should 5–7 weeks be represented as `COMPRESSED` + readiness override?** This is the most evidence-
consistent option among those offered: it reuses the existing 3-value registry unchanged, and it gives the
"readiness override" concept a natural home in the *already-existing* `CORE_ENTRY_READINESS_IN` resolver
rather than inventing a new cross-cutting mechanism. Not confirmed by any repo document as the approved
design — recommended, not asserted as fact.

**Should `READINESS_ONLY` be a plan mode/warning rather than a `TIME_ADEQUACY_IN` value?** Strong evidence
yes: `READINESS_ONLY` **already exists** as a valid value — but of `PLAN_MODE_IN`, not `TIME_ADEQUACY_IN`
(confirmed in both registry v1 and v2: `PLAN_MODE_IN` allowed values include `READINESS_ONLY`). Inventing a
*second*, differently-typed `READINESS_ONLY`-equivalent under `TIME_ADEQUACY_IN` would repeat exactly the
kind of cross-condition-type confusion diagnosed as the root cause of the `STANDARD` defect in §3. The
extremely low-week case (`<=4` weeks) most plausibly should route to `PLAN_MODE_IN = READINESS_ONLY`
(already representable) rather than needing any new `TIME_ADEQUACY_IN` value at all.

**Is a registry change actually needed?** Likely **no** for `TIME_ADEQUACY_IN` itself, based on the above —
the existing 3 values plus routing the sub-4-week case through the existing `PLAN_MODE_IN.READINESS_ONLY`
value appear sufficient. This is a recommendation for further validation, not a closed decision — no repo
evidence proves the cross-resolver composition (`TIME_ADEQUACY_IN` + `CORE_ENTRY_READINESS_IN` +
`PLAN_MODE_IN` acting together) was ever actually designed this way; it is the most evidence-consistent
reading, not a confirmed one.

### Recommended option

**Option A — keep `TIME_ADEQUACY_IN` unchanged (`ADEQUATE`/`COMPRESSED`/`INSUFFICIENT`); represent the 5–7
week case as `COMPRESSED` plus a `CORE_ENTRY_READINESS_IN` gate, and represent the `<=4` week case via the
already-existing `PLAN_MODE_IN = READINESS_ONLY` value (once `CORE_ENTRY_READINESS_IN`'s own conflict is
separately resolved per §3) rather than inventing a new `TIME_ADEQUACY_IN` value.** This is `DECISION_REQUIRED`
for final approval (the composition itself is not repo-proven), but it is the option best supported by
what the registry already models, and it avoids repeating the `STANDARD` mistake by keeping
`READINESS_ONLY` where it already, correctly lives.

---

## 6. PACE_SOURCE_IN product-scope decision options

**Should `PACE_SOURCE_IN` remain source-type only?** Yes, recommended — its 4 existing values
(`NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME`) answer "where did this pace come from," a categorically
different question from "how much do we trust it right now," which is what the recency ladder answers.
Conflating the two into one enum would require either a combinatorial explosion of values (source ×
confidence) or silently dropping one dimension — neither is evidenced as intended anywhere in the repo.

**Should recency confidence be stored as decision-trace metadata instead of a registry value?** Recommended
yes — directly precedented by the fixture's own `PACE_CONVERSION` step, which already carries `"confidence":
"HIGH"` and `"resultAgeDays": 49` as **facts/metadata alongside**, not instead of, its categorical result.
This is the same pattern recommended for `GOAL_FEASIBILITY_IN` in §4 — precise/continuous data as metadata,
coarse category as the registry-gated value.

**Should time trial / structured test get their own registry values, or map to `ESTIMATED`?** No repo
evidence resolves this — genuinely `DECISION_REQUIRED`. The fixture's only input shape
(`INPUT_SNAPSHOT.recentRace`) does not distinguish a certified race from a time trial or structured test at
all; there is no field anywhere that would let a resolver tell them apart today, independent of which
registry values eventually get created.

**Is a registry change needed for Phase 4B/4C?** Not for Phase 4B — Phase 4B is input-contract-only (§9/§10)
and does not touch `PACE_SOURCE_IN`'s registry values at all. For a hypothetical Phase 4C (resolver
implementation), a registry change is not clearly needed either, if Option A below is adopted, since the
existing 4 values plus metadata can represent everything currently evidenced.

### Recommended option

**Option A — keep `PACE_SOURCE_IN` unchanged (`NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME`); store
recency confidence separately in decision-trace metadata**, following the fixture's own existing
`confidence`/`resultAgeDays` pattern. Option B (add `TIME_TRIAL`/`STRUCTURED_TEST`/`USER_REPORTED_PACE` as
new registry values) is not recommended at this time — no repo evidence distinguishes these input shapes
today, so new registry values would have no backing data to populate them; this should be revisited only
once/if a backend input field actually captures which evidence layer a user provided (a Phase 4B/4C
question, not resolvable now).

---

## 7. Registry v3 recommendation

**Is v3 required only for `GOAL_FEASIBILITY_IN`?** No registry v3 is recommended as *required* for any of
the four items at this time, per §3–§6's individually recommended options (all lean toward metadata/
existing-value solutions over new registry values). If a v3 is eventually pursued for `GOAL_FEASIBILITY_IN`
specifically (Option B in §4, held as a deferred/possible future path, not the current recommendation), it
would be the only one of the four with a plausible registry-versioning driver — `TIME_ADEQUACY_IN` and
`PACE_SOURCE_IN`'s recommended options (§5/§6) do not need new registry values, and `CORE_ENTRY_READINESS_IN`
(§3) is a same-version defect fix, not a versioning question.

**Is v3 required for `CORE_ENTRY_READINESS_IN`?** No — §3's recommended fix (Option A, correct the fixture)
requires zero registry change; the existing `READY`/`CAUTION`/`NOT_READY` set is not implicated as
insufficient by any evidence found.

**Is v3 not required if metadata/trace is used for richer details?** Correct, per the recommended options
across §4–§6 — using decision-trace metadata for `GOAL_FEASIBILITY_IN`'s aggressiveness band and
`PACE_SOURCE_IN`'s recency confidence avoids any registry version bump for those two, and `TIME_ADEQUACY_IN`'s
recommended composition (§5) reuses only already-existing values across two condition types.

**Should `TEN_K__4D__INTERMEDIATE v10` remain on registry v2 until a future candidate v11?** Yes,
recommended — v10 is the only currently-evidenced candidate, its one live dependency
(`GOAL_PACE_REHEARSAL` → `GOAL_FEASIBILITY_IN`) is fully satisfied by registry v2's existing values, and no
evidence requires touching v10 at all as part of this reconciliation. If a registry v3 is later approved
(e.g. for a future richer `GOAL_FEASIBILITY_IN`), it should apply to a new candidate version, not retrofit
v10 — consistent with plan-catalog's own immutable version-cascade discipline (never mutate a
prior/published artifact).

---

## 8. Golden fixture v4 recommendation

- **Should golden fixture v3 remain untouched?** Yes, in this pass and until an explicit v4 authoring pass
  is separately approved and executed — no fixture byte was changed here, consistent with the task's
  constraint.
- **Should a new golden fixture v4 be generated later?** Yes, recommended, primarily to carry the
  `STANDARD → READY` correction from §3 (Option A) plus whatever additional example cases the product
  decisions in §4–§6 end up requiring.
- **Should v4 replace `STANDARD` with `READY`?** Yes — this is the core recommended content of v4, per §3.
- **Should v4 include 5-class goal-feasibility examples?** Only if Option B from §4 is explicitly approved
  by the product owner; if Option C (metadata-only) is adopted instead, v4 should instead demonstrate the
  existing 4-value classification with richer `facts.goalGapRatio`/similar metadata alongside it — no new
  registry values needed for that demonstration.
- **Should v4 include sub-12-week time-adequacy examples?** Yes, recommended regardless of which
  `TIME_ADEQUACY_IN` option is chosen — the fixture currently only proves the `>=12 → ADEQUATE` case; at
  least one `COMPRESSED` and one `INSUFFICIENT` (or `PLAN_MODE_IN.READINESS_ONLY`-routed) example would
  close a real evidence gap regardless of the scope decision's outcome.
- **Should v4 include multiple pace-recency examples?** Yes, recommended for the same reason — one data
  point (49 days → `HIGH`) cannot evidence a ladder; several examples spanning the proposed bands (e.g. one
  under 30 days, one over 180 days) would let a future resolver implementation be tested against real
  fixture facts instead of invented ones.

None of this is started in this pass — it is a recommendation for a future, separately-scoped fixture-
authoring pass (outside Process A's normal immutable-version-cascade discipline would not apply here since
`golden-fixture-v3` is test evidence, not a published catalog artifact — but it should still be treated as
a deliberate, reviewed authoring step, not an ad hoc edit).

---

## 9. TD-REGISTRY-001 closure condition

Restated from §3 for the required-output section: closes on **golden-fixture-v4 correcting `STANDARD` →
`READY`** (preferred), or an **explicit, owner-approved interim compatibility-mapping document** with a
mandatory fixture-correction follow-up (acceptable interim only). Remains `OPEN` after this pass.

## 10. Phase 4B readiness decision

**Can Phase 4B proceed while registry reconciliation remains open, if input names are stable?** Yes,
recommended — Phase 4B as scoped (optional inputs carried through preview/confirm payloads, no resolver
logic) does not read or write any `RUNTIME_CONDITION_VALUES_V1` value, does not touch
`GOAL_PACE_REHEARSAL`'s `requires` clause, and does not depend on how `CORE_ENTRY_READINESS_IN`'s `STANDARD`
anomaly or any of the three scope decisions (§4–§6) resolve. The blocking condition stated in Phase 4A.1
("Phase 4B input-contract work must not proceed until canonical decision sources are reconciled") is here
narrowed: full **resolver threshold** work remains blocked (nothing in §4–§6 is decided), but **input-field
addition**, being purely additive/nullable and never consumed by any resolver yet, carries no risk of being
built against a vocabulary that changes underneath it — an input field name doesn't depend on which
registry values eventually gate it.

**Which input fields are safe regardless of reconciliation outcome?** See §11.

## 11. Safe input-field recommendation

| Field | Safe now? | Why |
|---|---|---|
| `recentLongestRunKm` | Yes | Matches the golden fixture's own evidenced input shape (`INPUT_SNAPSHOT.longestRunLast30DaysKm`) and feeds `CORE_ENTRY_READINESS_IN`/`LONG_RUN_COMPATIBILITY_RESOLVER` inputs in the fixture — a real, evidenced field name/shape, independent of the `STANDARD` output defect (§3) or any scope decision (§4–§6). |
| `recentWeeklyVolumeKm` | Yes | Matches the fixture's `CURRENT_CAPACITY_RESOLVER`/`weeklyVolumeKm` fact — evidenced, and independent of all four open items. |
| `recentRunsPerWeek` | Yes | Matches the fixture's `CORE_ENTRY_READINESS_RESOLVER.facts.runsPerWeek` — evidenced. Distinct from `GeneratePreviewRequest.DaysPerWeek` (planned future days, not historical actual days), so it doesn't collide with an existing field's meaning. |
| `recentRaceDistanceKm` | Yes | Matches `INPUT_SNAPSHOT.recentRace.distanceKm` — evidenced, and feeds the Riegel-based `PACE_CONVERSION` step (EV-001) independent of the `PACE_SOURCE_IN` scope decision (§6). |
| `recentRaceFinishTimeSeconds` | Yes | Matches `INPUT_SNAPSHOT.recentRace.timeSeconds` — evidenced, same reasoning as above. |
| `recentRaceDate` | Yes | Matches `INPUT_SNAPSHOT.recentRace.date` — evidenced; this is also the field the recency-confidence ladder (§6, still `DECISION_REQUIRED` on its *bands*) would eventually read from — capturing the raw date now is safe and useful regardless of which bands get approved later. |
| `targetFinishTimeSeconds` | Yes (already exists) | Already present on `GeneratePreviewRequest` per Phase 4A §2 — not a new field, listed here only for completeness of the resolver-input picture. |
| `paceEvidenceType` | **No — not approved** | §6's evidence-hierarchy mapping (certified race / time trial / structured test / user-reported pace / effort-only → registry value) is entirely `DECISION_REQUIRED`; adding this field now would require guessing its allowed-value set, which no repo evidence supports. Per the task's own instruction ("only if approved") and "do not approve unstable fields unless evidence supports them," this is withheld. |
| `paceEvidenceDate` | **No — not approved** | Same reasoning: this field's purpose is specifically to anchor the recency-confidence ladder (§6), whose bands are entirely undecided (only one data point exists). Note `recentRaceDate` (above) already captures the one evidenced date shape (`INPUT_SNAPSHOT.recentRace.date`) — a separate, more general `paceEvidenceDate` would be redundant with it until the evidence-hierarchy question (§6) is resolved and shows a genuine need for a source-agnostic date field. |

**Recommendation:** Phase 4B may add the 6 new fields marked "Yes" above (plus rely on the pre-existing
`targetFinishTimeSeconds`), all as nullable/optional, additive, carried through preview/confirm payloads
only — no resolver reads them yet. `paceEvidenceType`/`paceEvidenceDate` should wait for §6's resolution.

## 12. Required owner decisions

1. `GOAL_FEASIBILITY_IN`: approve Option C (metadata-only, recommended) vs. Option B (registry v3) vs.
   other — §4.
2. `TIME_ADEQUACY_IN`: approve the recommended `COMPRESSED` + `CORE_ENTRY_READINESS_IN` gate + existing
   `PLAN_MODE_IN.READINESS_ONLY` composition (§5), or specify an alternative.
3. `PACE_SOURCE_IN`: approve Option A (metadata-only, recommended) vs. Option B (new source-type registry
   values) — §6.
4. `CORE_ENTRY_READINESS_IN` / `STANDARD`: approve Option A (fixture correction in v4, recommended) vs.
   Option C (interim documented mapping) — §3.
5. Whether/when a golden-fixture-v4 authoring pass is commissioned, and its exact scope (§8).
6. Whether `paceEvidenceType`/`paceEvidenceDate` should be added to Phase 4B once the evidence-hierarchy
   mapping (§6) is resolved, or deferred further to a dedicated Phase 4C.

## 13. Explicit non-actions (confirmed not performed in this pass)

- `catalog/registries/runtime-condition-values.v2.json` was not modified; no v3 registry file was created.
- `docs/canonical/golden-fixture-v3/*` was not modified; no v4 fixture file was created.
- No resolver `.cs` file was created or modified.
- No `TrainingWeek`/`TrainingDay` was generated.
- No backend runtime behavior was changed (no file under `backend/` was touched).
- `EV-005` was not marked accepted (still `PROPOSED` in `evidence-log.json`/`.md` — unchanged by this pass).
- `TD-REGISTRY-001` was not marked closed (still `OPEN` — unchanged by this pass).
- No new input field was actually added to any backend DTO — §11 is a recommendation for Phase 4B to
  execute, not an implementation performed here.

---

## Final report

**1. Files inspected:** `plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`;
`plan-catalog/docs/evidence-log.json`/`.md`; `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`;
`catalog/registries/runtime-condition-values.v1.json` and `.v2.json`;
`docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json` (full);
`docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.md`; `catalog/rule-packs/appsel-race-plan.v4.json`;
`catalog/workout-progressions/ten-k-workout-progression.v5.json`;
`PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md`; `PHASE4A_1_CANONICAL_ARTIFACT_IMPORT_AND_RISK_CAPTURE.md`;
`artifacts/audits/domain-d3-followup.json`/`.md`; `docs/archive/golden-fixture-v2/golden-10k-intermediate-4d-12w_v2_decisiontrace.json`
and `.md` (non-canonical, inspected only for historical corroboration, not cited as canonical evidence);
`artifacts/audits/domain-blocker-source-map.json`/`.md` (checked, no relevant content beyond unrelated
`EASY_STANDARD`/`LONG_RUN_STANDARD` workout-key homonyms). Note: a file named
`golden-10k-intermediate-4d-12w.v3.decisiontrace.md` (listed in the task's inspection list) does not exist
in the repository — only `.decisiontrace.json` and a separate `.md` narrative file exist; both were
inspected.

**2. Files changed:** One new file —
`PHASE4A_2_RUNTIME_CONDITION_CONFLICT_CLASSIFICATION_AND_RECONCILIATION_PROPOSAL.md` (this document). No
other file was created or modified.

**3. Conflict classification matrix:** A → `DEFECT_OR_LEGACY_ARTIFACT`; B → `PRODUCT_SCOPE_EXPANSION`; C →
`PRODUCT_SCOPE_EXPANSION` + `DECISION_REQUIRED`; D → `PRODUCT_SCOPE_EXPANSION` + `DECISION_REQUIRED`. Full
basis in §2.

**4. CORE_ENTRY_READINESS recommended option:** Option A — correct the fixture (`STANDARD` → `READY`) in a
future golden-fixture-v4; Option C (interim documented mapping) acceptable only as a temporary bridge with
a mandatory follow-up.

**5. GOAL_FEASIBILITY recommended option:** Option C — keep registry unchanged; store the 5-class
aggressiveness band as decision-trace metadata rather than new registry values.

**6. TIME_ADEQUACY recommended option:** Option A — keep registry unchanged; represent the 5–7 week band as
`COMPRESSED` + a `CORE_ENTRY_READINESS_IN` gate, and route the `<=4` week case through the already-existing
`PLAN_MODE_IN.READINESS_ONLY` value.

**7. PACE_SOURCE recommended option:** Option A — keep registry unchanged; store recency confidence as
decision-trace metadata, following the fixture's own existing `confidence`/`resultAgeDays` pattern.

**8. Registry v3 recommendation:** Not required for any of the four items under the recommended options
above. `TEN_K__4D__INTERMEDIATE v10` should remain on registry v2; any future v3 (if the owner instead
chooses `GOAL_FEASIBILITY_IN` Option B) should apply only to a future candidate version, never retrofitted
onto v10.

**9. Golden fixture v4 recommendation:** v3 remains untouched now; a future v4 pass is recommended to (a)
correct `STANDARD` → `READY`, (b) add sub-12-week `TIME_ADEQUACY_IN` examples, and (c) add multiple
pace-recency examples. 5-class `GOAL_FEASIBILITY_IN` examples only if Option B is chosen instead of the
recommended Option C.

**10. TD-REGISTRY-001 closure condition:** Golden-fixture-v4 correction (preferred) or an explicit
owner-approved interim mapping document with a mandatory fixture-correction follow-up. Remains `OPEN`.

**11. Whether new risk entries are recommended:** No new `TD-*` risk entry is recommended in this pass —
the four open items are already fully captured by `TD-REGISTRY-001` (item A) and this document's own §4–§6
(items B/C/D, tracked as product-scope decisions rather than activation risks, since they are not defects).

**12. Whether EV-005 should remain PROPOSED:** Yes — unchanged, still `PROPOSED`, not modified in this pass.

**13. Whether Phase 4B can proceed:** Yes, under the constrained scope in §10/§11 (input-contract fields
only, no resolver logic) — full resolver-threshold work remains blocked pending the owner decisions in §12.

**14. Safe input fields for Phase 4B:** `recentLongestRunKm`, `recentWeeklyVolumeKm`, `recentRunsPerWeek`,
`recentRaceDistanceKm`, `recentRaceFinishTimeSeconds`, `recentRaceDate` (all new, evidenced, nullable) plus
the pre-existing `targetFinishTimeSeconds`. `paceEvidenceType`/`paceEvidenceDate` are **not** approved —
withheld pending §6's evidence-hierarchy resolution.

**15. Required owner decisions:** The 6 items listed in §12 — `GOAL_FEASIBILITY_IN` scope option,
`TIME_ADEQUACY_IN` composition approval, `PACE_SOURCE_IN` scope option, `CORE_ENTRY_READINESS_IN`/`STANDARD`
fix option, golden-fixture-v4 commissioning, and `paceEvidenceType`/`paceEvidenceDate` timing.

**16. Confirmation no registry values were changed:** Confirmed — both `runtime-condition-values.v1.json`
and `.v2.json` were read-only inspected.

**17. Confirmation no golden fixtures were changed:** Confirmed — `golden-fixture-v3/*` and the archived
`golden-fixture-v2/*` files were read-only inspected.

**18. Confirmation no resolver code was implemented:** Confirmed — no `.cs` file was touched.

**19. Confirmation no generation was implemented:** Confirmed.

**20. Confirmation no backend runtime behavior was changed:** Confirmed — no file under `backend/` was
touched in this pass.

**21. Anything not completed exactly as specified:** One clarification: the task's file-inspection list
named `golden-10k-intermediate-4d-12w.v3.decisiontrace.md`, which does not exist as a separate file in the
repository (only `.decisiontrace.json` and a differently-named `.v3.md` narrative file exist). Both actual
files were inspected in full; this is noted as a naming discrepancy in the task's own list, not a gap in
this pass's evidence coverage.
