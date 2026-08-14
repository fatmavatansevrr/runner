# Phase 4G.3B.8 — TD-ALLOCATION-PRIORITY-001 Resolution Decision Audit

**Read-only decision audit. No catalog artifact, allocator, or verifier code
changed. No TD updated. Recommendation only — not applied.**

---

## 1. Exact affected target week counts (re-derived, not assumed)

Re-read directly from `plan-catalog/catalog/templates/ten-k-master.v6.json`
(confirmed unchanged since Phase 4G.3A/4G.3B.1 — same file, same values,
re-verified in this pass rather than assumed):

| Phase | minimumWeeks | preferredWeeks | maximumWeeks | compressionPriority | extensionPriority |
|---|---:|---:|---:|---:|---:|
| FOUNDATION | 2 | 3 | 4 | 1 | 1 |
| BUILD | 3 | 4 | 5 | 2 | 2 |
| RACE_SPECIFIC | 2 | 4 | 4 | 3 | 3 |
| TAPER | 1 | 1 | 1 | 4 | 4 |

Sums: minimum=8, preferred=12, maximum=14 — matching `coreCycle.minimumWeeks=8`,
`defaultWeeks=12`, `maximumWeeks=14` exactly.

**Compression headroom** (preferred − minimum) per phase: FOUNDATION=1,
BUILD=1, RACE_SPECIFIC=2, TAPER=0 (protected). Total=4, matching
preferred−minimum=12−8=4.

**Extension headroom** (maximum − preferred) per phase: FOUNDATION=1,
BUILD=1, **RACE_SPECIFIC=0**, TAPER=0. Total=2, matching maximum−preferred=14−12=2.

**Re-derivation result — this corrects the task prompt's own step-1 framing,
not merely confirms it:**

- **8 weeks**: every phase forced to its own minimum simultaneously
  (2+3+2+1=8) — no choice, order-irrelevant.
- **12 weeks**: exactly the preferred sum — the unique allocation, no
  compression or extension of any kind, order-irrelevant.
- **14 weeks**: every phase forced to its own maximum simultaneously
  (4+5+4+1=14) — no choice, order-irrelevant.
- **9, 10, 11 weeks**: compression-order-dependent — multiple phases have
  remaining compression headroom simultaneously, so which phase compresses
  first genuinely changes the resulting allocation (verified: at 10 weeks,
  reduction=2 could come entirely from RACE_SPECIFIC's headroom alone if it
  had top priority, producing 3+4+2+1=10, a different valid allocation than
  the current priority order's 2+3+4+1... i.e. distinct per-phase outcomes
  for the same total, confirming genuine order-dependence, not merely
  possible in principle).
- **13 weeks only** — not 13-14 as the task's own framing suggested —
  **is the sole extension-order-dependent target.** At 13 weeks, the +1
  week beyond preferred must go to either FOUNDATION (headroom 1) or BUILD
  (headroom 1) — RACE_SPECIFIC has **zero** extension headroom
  (`preferredWeeks == maximumWeeks == 4`) and TAPER is fixed — so the
  extension-priority order between FOUNDATION and BUILD specifically
  determines the 13-week allocation (current order gives FOUNDATION the
  extra week: 4+4+4+1=13; the reverse would give BUILD: 3+5+4+1=13). At 14
  weeks, **both** FOUNDATION and BUILD must be simultaneously maxed
  regardless of order (2 extra weeks needed, exactly matching their combined
  headroom of 1+1) — no genuine choice exists, matching
  `AllocationOrderCorrectnessVerifier`'s own real result (`Pass`, not
  `DecisionRequired`, at 14) and the TD's own statement text ("except the
  order-independent 8-week and 14-week fully-exhausted boundary
  allocations") — both independently corroborate this correction.

**Confirmed affected set: compression = {9, 10, 11}; extension = {13} only.**

---

## 2. Canonical-decisions search result — quoted verbatim, explicit NO

**No genuinely applicable existing principle was found — for either
compression order or extension order.** This is not merely "no direct
extension-specific reasoning found" (the weaker claim the task anticipated
as one possible outcome) — it is stronger: **this session's own prior work
(Phase 4G.3A) already proved the compression-order citation itself is
fictional**, which this audit independently re-confirms and extends to
cover extension and the "runway" concept specifically.

**Section headings actually present in `appsel-v1-canonical-decisions.md`**
(confirmed by direct grep of the file, not assumed from memory): `## Status
of this document`, `## Location rationale`, `## A. Purpose and authority`,
`## B. Resolver-related canonical decisions...` (`B.1`–`B.5`), `## C. Known
conflicts requiring reconciliation` (`C.1`–`C.4`), `## V1 Runtime Scope and
Trace Metadata Resolution`, `## D. Evidence-log relationship`. **There is no
§9, no §6, no §5, and no section discussing preparation runway, phase
extension, or "longer than preferred" plans of any kind.**

Direct case-insensitive search of the full document text for `runway`:
**zero matches.** For `extend|longer|exceeds|extension`: **zero matches.**
The `PREPARATION_RUNWAY_PLUS_CORE` resolver concept named in the task's own
investigation prompt: **zero matches anywhere in the entire repository**
(searched all `.md`/`.json` files) — this concept does not exist in this
codebase under that name or, as far as this search could determine, under
any equivalent name.

**Prior, already-established finding this audit re-confirms rather than
re-derives** — `PHASE4G_3A_EIGHT_WEEK_CORE_ALLOCATION_AUDIT.md`, quoted
verbatim:

> "The task instruction asked this audit to treat three inputs (C-01
> compression order, C-02 taper minimum, C-03 conditional Foundation=1) as
> `ALREADY_CANONICAL`, citing `appsel-v1-canonical-decisions.md §5`/`§6`.
> **That document has no §5 or §6**... | C-01 compression order
> (Foundation→Build→RaceSpecific→Taper) | `ALREADY_CANONICAL`, source §6 |
> **§6 does not exist.** However, the real catalog artifact
> (`ten-k-master.v6.json`) independently encodes this exact order via each
> phase's `compressionPriority`... | `EVIDENCE_BACKED` (catalog-artifact-sourced,
> not doc-sourced as claimed) |"

**This means even the compression order this TD's own `requiredResolution`
asks to "confirm or replace" was never actually a product/coaching decision
in the first place** — its only real basis is the catalog artifact's own
authored `compressionPriority` values, which is exactly the circular,
self-referential situation AUD-008 already flagged as `PLACEHOLDER_UNCONFIRMED`.
There is no existing document anywhere in this repository — for compression
*or* extension — that constitutes independent product/coaching reasoning
distinct from the catalog data itself.

**Explicit answer: NO, no genuinely applicable existing principle exists.**
Section 3 of the task template ("if found: compatibility analysis") is
therefore not applicable; proceeding to section 4 below (Option A/B/C
evaluation).

---

## 3. Option A/B/C evaluation

Since section 2 found no existing principle, Option C's premise ("state
this if the evidence in steps 1-3 does not clearly favor A or B") is the
starting condition, not a fallback of last resort — but Options A and B are
still evaluated on their own independent merits below, using what evidence
does exist in the repository (the training-science citations already used
elsewhere), before reaching the final recommendation in section 6.

**Relevant existing repository evidence** (from
`plan-catalog/artifacts/audits/phase4f6-step-b-training-science-evidence-mapping.md`,
the only place `Casado`/`Kenneally` are cited in this repository), quoted
for what it actually says, not overclaimed beyond it:

> "TDE-001: Kenneally, Casado, Santos-Concejero (2018)... TDE-002: Casado et
> al. (2022)... TDE-001/TDE-002 found pyramidal/polarized TID models
> outperform a threshold-*dominant* model as a whole-plan strategy... 
> Race-specific pace exposure nearer competition is evidence-backed
> generally (TDE-002); its exact runtime-eligibility thresholds... are
> explicitly `NOT_AN_EVIDENCE_QUESTION`... Whether intermediate runners
> should receive the identical stage form as advanced runners is
> `INSUFFICIENT_EVIDENCE`... **POPULATION**: every claim drawn from
> TDE-001/TDE-002 requires elite→intermediate extrapolation. **DISTANCE**:
> TDE-003 is not distance-running-specific at all; TDE-001/002 are
> middle/long-distance broadly, not 10K-isolated."

This is real, already-vetted evidence in the repository, but it answers a
**different** question (whole-plan training-intensity-distribution strategy,
and general value of race-specific pace exposure) than the one this TD
asks (which phase should receive *additional weeks* when a plan is
*longer than preferred*). It provides **indirect, non-specific** support
for Option B's rationale (race-specific exposure has *some* general
evidence value) but does not itself distinguish "extend Foundation" from
"extend Build" from "extend Race-Specific" as a phase-length-allocation
question — no source in this repository does.

### OPTION A — Reuse compression order for extension (current de facto state)

- **For:** Symmetry/simplicity — one shared ordering concept, easier to
  reason about and author. A longer Foundation does plausibly give more
  low-intensity base-building time, generically consistent with
  polarized/pyramidal-TID evidence (TDE-001/002) favoring low-intensity
  volume dominance — but that evidence is about *intensity distribution
  within* a plan, not about *which phase should grow* when total weeks
  increase; applying it to this specific question is an analogy, not a
  direct finding.
- **Against:** No source — in this repository or cited from it — actually
  reasons about whether *extending* Foundation specifically is
  coaching-optimal versus extending Build (more quality-development
  exposure) or Race-Specific (more race-pace exposure, which TDE-002 does
  at least generically support as valuable). The "symmetry" argument is a
  simplicity/authoring convenience, not a coaching argument.

### OPTION B — Reverse order for extension (Race-Specific/Build grow before Foundation)

- **For:** TDE-002's general, if non-specific, support for race-specific
  pace exposure value. Ties naturally to `TD-FOUNDATION-COMPRESSION-001`'s
  still-open `CORE_ENTRY_READINESS_IN`-gating question — a runner with
  adequate base (readiness=READY) arguably benefits more from extra
  quality/race-specific time than extra easy volume, if the base is
  already sufficient. **Critical structural problem**: RACE_SPECIFIC has
  **zero extension headroom** in the current catalog (`preferredWeeks ==
  maximumWeeks == 4`) — so "Race-Specific grows first" is not even
  *executable* under the current catalog data without first authoring a
  new, larger `RACE_SPECIFIC.maximumWeeks` value, which is a real catalog
  change beyond a priority-number swap. Only the Build-vs-Foundation
  ordering is actually executable within current bounds (the only real
  choice at the single affected target, 13 weeks).
- **Against:** No source directly supports this either — it is exactly as
  evidence-thin as Option A, just aimed a different direction. The
  `CORE_ENTRY_READINESS_IN`-gating tie-in is a plausible *future*
  architecture (conditional extension priority based on readiness), not
  something any current document or code establishes.

### OPTION C — Extension priority is genuinely independent, requires its own explicit product decision

Given section 2's explicit finding (no existing principle) and both A and
B being equally evidence-thin for the one real, executable question (which
of Foundation/Build gets the +1 week at 13 target weeks), **Option C is the
evidence-supported conclusion** — not a default fallback chosen for lack of
effort, but the honest result of the investigation actually performed.

---

## 4. Evidence-basis classification

Using this session's established two-axis taxonomy (Phase 4A.2/4A.3):

| Claim | Evidence basis |
|---|---|
| Compression order (current catalog values) | `EVIDENCE_BACKED` in the narrow sense of "the catalog artifact says so" (per `PHASE4G_3A`'s own finding) — but `NO_DIRECT_EVIDENCE` in the sense of independent product/coaching reasoning; AUD-008's `PLACEHOLDER_UNCONFIRMED` classification stands. |
| Extension order = compression order (Option A) | `NO_DIRECT_EVIDENCE` — no document reasons about this specifically; the "symmetry" rationale is an authoring convenience, not evidence. |
| Extension order reversed (Option B) | `NO_DIRECT_EVIDENCE` for the ordering claim itself; the general value of race-specific pace exposure is `EVIDENCE_INFORMED` (TDE-002, with the population/distance caveats quoted in §3) but does not reach the specific ordering question. |
| "This requires a new, explicit product/coaching decision" (Option C) | This is the audit's own conclusion, not an evidence-basis-classified claim about training science — it is a statement about the *state of available evidence*, which section 2/3 above source-verify as genuinely absent. |

---

## 5. Recommendation — explicit statement that product/coaching input is required

**This audit does not recommend Option A or Option B.** Forcing a choice
between them would repeat exactly the pattern `AUD-008` already flagged as
the original problem (an invented, unreasoned ordering presented as if
settled) — this time in the extension direction instead of compression.
Consistent with this session's established practice in the Phase 4G.3B.6
series (refusing to force a conclusion the evidence does not support), the
honest conclusion is: **this specific question cannot be resolved from
existing repository documents alone and requires new product/coaching
input.**

**The smallest, most specific version of the question to ask, NOT YET
APPLIED:**

> For the `TEN_K__4D__INTERMEDIATE` 13-week case specifically (the only
> real, executable extension-ordering choice given current catalog bounds
> — Race-Specific has zero extension headroom today), when the plan grows
> one week beyond the 12-week preferred core, should that extra week be
> added to **FOUNDATION** (extending the aerobic-base-building phase from 3
> to 4 weeks) or to **BUILD** (extending the volume/quality-development
> phase from 4 to 5 weeks)? Please reason from actual 10K
> intermediate-runner training-periodization practice, not from the
> compression order's own (itself unconfirmed, per AUD-008) precedent.

This is deliberately scoped to the one real, currently-executable choice
(13 weeks, Foundation-vs-Build) rather than a vague "how should extension
work in general" — Race-Specific's own extension eligibility (raising
`maximumWeeks` above 4) is a separate, larger catalog-authoring question
this audit does not fold into the same ask, since it requires a new bound
value, not merely a priority reordering.

---

## 6. Implementation cost estimate

**Zero schema/loader change required, regardless of which resolution path
is eventually chosen.** Confirmed by direct source inspection:
`PlanCatalogPhaseAllocation` (the typed catalog-loader record,
`backend/RunningApp.Application/RuntimeCatalog/...`) already declares
`CompressionPriority` and `ExtensionPriority` as **two fully independent
`int` properties**, and `CatalogPhaseAllocationResolver` already sorts by
each independently (`source.OrderBy(p => p.CompressionPriority)` and
`source.OrderBy(p => p.ExtensionPriority)` are already separate calls,
confirmed by direct grep of the resolver source). **The model already
supports asymmetric compression-vs-extension ordering per phase — the
current identical values (1/2/3/4 for both) are a catalog-*data* choice,
not a code/schema limitation.**

Whichever resolution is eventually chosen (A, B, or a new value set from
future product input), the implementation is therefore: **a
versioned-catalog-authoring change to `ten-k-master.v6.json`'s
`extensionPriority` field values only** (per the repository's normal
catalog-authoring process, as the TD's own `requiredResolution` already
specifies) — no `.cs` file changes, no schema migration, no allocator
logic change. If Option B's Race-Specific-extension idea is ever pursued
beyond the 13-week Foundation/Build question, that would additionally
require raising `RACE_SPECIFIC.maximumWeeks` above 4, a materially larger
and separate catalog-authoring decision with its own downstream
consequences (peak-volume-band interaction, `AllocationOrderCorrectnessVerifier`
re-verification, etc.) — explicitly out of scope for the narrow question
posed in section 5.

**Choosing now does not foreclose future refinement** — because the field
is already independently typed and stored per phase, revising
`extensionPriority`'s specific values later (e.g., after real product
input answers section 5's question) is exactly as cheap as this closure
would be: a catalog-data edit, nothing structural.
