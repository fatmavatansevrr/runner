# PHASE HM.1.3 — HALF-MARATHON WORKOUT-PROGRESSION EXPOSURE-COUNT AUTHORITY DECISION AND HM.1.2 FULL CLOSURE

**Phase type**: DECISION (decision-recording/governance-closure only — no production code, no HM catalog artifacts, no public routing changes, no 10K changes of any kind)
**Execution status**: DONE
**Final classification**: `HM_WORKOUT_PROGRESSION_STAGE_CATALOG_CLOSED`
**Governance commit**: PENDING (backfilled in the following commit, per this engagement's established two-commit self-referential-SHA pattern).
**Parent**: `HM.1.2` (`PHASE_HM_1_2_WORKOUT_PROGRESSION_STAGE_CATALOG_CLOSURE.md`), which derived and evidence-traced the full HM stage catalog for `HALF_MARATHON × INTERMEDIATE × 4D × 14W` but classified itself `HM_WORKOUT_PROGRESSION_STAGE_CATALOG_BLOCKED` on exactly one remaining numeric gap (§14 item 1: exact `MinimumExposures`/`MaximumExposures`, or a simpler fixed week-count, per `KEY_SESSION`-lane stage for this one cell), plus one disclosed non-blocking preference (§14 item 2: Taper KEY-lane family choice).

---

## 0. What this phase is

A direct user decision message — "DECISION ON HM.1.2" — closing `HM.1.2`'s remaining stage-catalog gap deterministically (fixed values / eligibility-driven rules, not open ranges), so no future author has to choose between multiple valid distributions when authoring the real `WORKOUT_PROGRESSION` catalog document. The message's exact text is the spec; this phase records it verbatim rather than reinterpreting or improving on it, matching this engagement's own established precedent for direct-decision phases (`HM.1.1`, and before it `GEN.16`, `GEN.24`, `GEN.31`, `GEN.36`, `GEN.37`) of recording a user's decision rationale as given rather than re-deriving it.

## 1. Startup confirmation

- `git fetch origin` clean; `git rev-list --left-right --count origin/main...HEAD` = `0  0` before starting.
- Worktree changes limited to the pre-existing tracked `bin`/`obj` rebuild-artifact noise this engagement has repeatedly called out as leftover verification-run byproduct, plus pre-existing unrelated `baseline_tmp` / `ten-k-pilot-domain-decision-audit.*` / `.trx` test-result noise — left untouched, not staged.
- Both required source documents read in full before writing anything:
  - `PHASE_HM_1_2_WORKOUT_PROGRESSION_STAGE_CATALOG_CLOSURE.md` (the immediate predecessor being closed out) — confirmed the stage names/properties this decision references (`FOUNDATION_AEROBIC_BASE`, `BUILD_FASTER_THAN_HM_INTRO`, `BUILD_THRESHOLD_DEVELOPMENT`, `RACE_SPECIFIC_HM_INTRODUCTION`/`_DEVELOPMENT`/`_REHEARSAL`, `TAPER_HM_ACTIVATION`) match exactly what `HM.1.2 §3` derived — this decision builds directly on that stage catalog, it does not redefine any stage identity, eligibility matrix, ordering rule, dose-envelope, taper semantic, or catalog-capability classification already frozen there.
  - `PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md` — the precedent for how a decision-recording phase in this HM engagement is structured: frozen authority block recorded verbatim, rationale section, explicit scope-constraint section, `EVIDENCE_INFORMED_PRODUCT_DEFAULT` classification kept distinct from `EVIDENCE_BACKED`, and a new decimal-sub-phase file (not an in-place rewrite of the parent phase's own report body) recording the decision and its supersession of the parent.
- `PHASE_LEDGER.md`'s HM Program table (rows 1–5: `HM.0`, `HM.0.1`, `HM.1`, `HM.1.1`, `HM.1.2`) confirms no `HM.1.3` row exists yet. **Chosen phase ID: `HM.1.3`** — mirroring `HM.1.1`'s own established discipline of a decimal sub-phase that closes out its immediately-preceding integer/decimal phase's own remaining item (`HM.0.1` closed `HM.0`'s search gap; `HM.1.1` closed `HM.1`'s escalated Item 2), and deliberately *not* `HM.2`, which both `HM.1.1 §4` and `HM.1.2 §15` already reserve as the name for the future workout-materializing implementation phase. Using `HM.2` here would collide with that already-reserved identifier.

---

## 2. Item — `HM.1.2 §14` items 1 and 2, exposure-count and Taper-family authority: **FROZEN**

### 2.1 Frozen authority — recorded exactly as decided

```
HALF_MARATHON x INTERMEDIATE x 4D x 14W PREFERRED CORE ONLY

Foundation (3 weeks) -- unchanged from HM.1.2:
    FOUNDATION_AEROBIC_BASE = 3 structural weeks
    (existing Foundation eligibility governs KEY binding -- not reopened here)

Build (4 weeks) -- deterministic, eligibility-driven:
    IF FasterThanHM eligible:
        BUILD_FASTER_THAN_HM_INTRO   = 1
        BUILD_THRESHOLD_DEVELOPMENT  = 3
    ELSE:
        BUILD_FASTER_THAN_HM_INTRO   = 0
        BUILD_THRESHOLD_DEVELOPMENT  = 4
    (sum is always exactly 4 -- no remaining choice; eligibility alone
    determines the split, not authorial discretion)

RaceSpecific (5 weeks) -- fixed:
    RACE_SPECIFIC_HM_INTRODUCTION = 1
    RACE_SPECIFIC_HM_DEVELOPMENT  = 2
    RACE_SPECIFIC_HM_REHEARSAL    = 2
    (1/2/2, not a range -- rehearsal gets 2 exposures, not 1, reflecting
    real-program evidence of repeated race-specific practice rather than
    a single late checkpoint; conservative relative to BAA's higher
    exposure count, appropriate given Appsel 4D's single KEY_SESSION lane
    and the current catalog's inability to carry quality-in-long-run)

Taper (2 weeks) -- fixed, now also closed (was flagged non-blocking, closing anyway):
    TAPER_HM_ACTIVATION = 2 (both weeks)
    Explicit constraint: the second taper exposure must NOT become another
    full-load STANDARD quality workout. Both apply taper-volume reduction;
    specificity/intensity may be retained per Taper's already-frozen
    semantics (no novel stimulus, lower volume, intensity/specificity may
    remain, short race-week activation). A future implementation phase
    must implement this via existing fallback/parameterization
    mechanisms -- not by inventing new taper-specific content.

Stage occupancy order (occupancy authority only, not final workout sequence):
    [FASTER_THAN_HM_INTRO?] -> THRESHOLD_DEVELOPMENT (x3 or x4)
    -> HM_INTRODUCTION -> HM_DEVELOPMENT -> HM_DEVELOPMENT
    -> HM_REHEARSAL -> HM_REHEARSAL
    -> TAPER_ACTIVATION -> TAPER_ACTIVATION
```

Cardinality check against `HM.1`'s own frozen `3 Foundation / 4 Build / 5 RaceSpecific / 2 Taper` allocation: `3 + 4 + 5 + 2 = 14`, exact, in both Build branches (`1+3=4` or `0+4=4`) — no slack, no shortfall, no invented filler week.

### 2.2 Rationale

Recorded as given, per the governing decision message's explicit instruction — not re-derived or re-verified via new research this phase, matching `HM.1.1 §2.2`'s own precedent for recording a user's decision rationale verbatim:

- **Build's eligibility-driven split** operationalizes `HM.1.2`'s own already-frozen classification of `BUILD_FASTER_THAN_HM_INTRO` as `OPTIONAL` (§3) without leaving the optionality's *numeric consequence* undecided — whichever branch fires, the two Build stages' combined count is always exactly 4, so no author ever has to invent a partial-week split.
- **RaceSpecific's 1/2/2** reflects real-program evidence of repeated race-specific practice (see §2.3 below) rather than a single late checkpoint, while remaining conservative relative to B.A.A.'s own higher exposure count — appropriate given Appsel's 4D single-`KEY_SESSION`-lane structural ceiling (`HM.1.2 §7`, `RUN_LAYOUT_4D`) and the still-unresolved catalog-structural inability to carry quality content inside `LONG_RUN` (`HM.1.2 §12`, `LONG_RUN_STANDARD`'s lack of an embedded quality/pace segment in any of its 6 versions).
- **Taper's 2/2, with the explicit non-full-load constraint on the second exposure**, closes `HM.1.2 §14` item 2's disclosed non-blocking Taper-family preference at the same time: by keeping `TAPER_HM_ACTIVATION` (the `HM_PACE`-carrying stage `HM.1.2 §3`/§9 already defined) as the occupant of both taper weeks rather than reverting to plain `EASY`, this decision confirms the `HM_PACE`-retained shape `HM.1.2 §9` already recommended without freezing — while adding a numeric guardrail (`second exposure ≠ full-load STANDARD`) that `HM.1.2` itself had left as "exact reduction not decided" (§8, §14 item 2). This satisfies all of `HM.1.2 §9`'s already-frozen Taper principles (no novel stimulus, lower volume, some specificity may remain, short activation) without inventing new taper-specific content — the reduction is implemented via `HM.1.2`'s own already-identified mechanism (existing fallback/parameterization, not a new workout family).

### 2.3 Classification — recorded prominently, not as a footnote

```
EVIDENCE_INFORMED_PRODUCT_DEFAULT
(explicitly NOT EVIDENCE_BACKED)
```

**Supporting evidence** for the general pattern of continuous, repeated specificity exposure (not the exact integers themselves):

- **Hal Higdon Intermediate 2** maintains continuous quality exposure from Week 1, with alternating tempo/interval work throughout the program rather than a single late-program checkpoint.
- **B.A.A. Level Two** repeats HM-pace/race-simulation exposure across multiple weeks in the main phase, rather than concentrating race-specific practice into one late checkpoint.

Both sources independently support "continuous, repeated specificity exposure" as a real, observed program-design pattern — this is the evidence basis for RaceSpecific carrying *multiple* HM-pace exposures (`_DEVELOPMENT`×2, `_REHEARSAL`×2) rather than a single terminal one.

**Why this is NOT `EVIDENCE_BACKED`**: the exact `1/3/1/2/2/2/2` distribution itself (Foundation×3, Build's `1+3` or `0+4`, RaceSpecific's `1/2/2`, Taper's `2/2`) is an Appsel product decision *informed by*, but not *derived from*, a causal study. No cited source specifies these exact counts — Hal Higdon and B.A.A. establish that repeated exposure is a real, valid pattern; they do not establish that exactly 2 development weeks and exactly 2 rehearsal weeks (as opposed to, say, 3 and 1, or 1 and 3) is uniquely correct. This mirrors `HM.1.1 §2.1`'s own explicit distinction for `PreferredAbsolutePeakLongRunKm = 19.0 km` (practice-convergence-informed, not causally proven optimal) — the same evidentiary honesty standard applies here, to a distribution rather than a single ceiling value.

These external citations are recorded as given, per the governing decision message's explicit instruction — not re-verified via new web search in this phase, matching how `HM.1.1` recorded its own citations verbatim.

### 2.4 Explicit scope constraint — recorded prominently, not as a footnote

This decision freezes **ONLY** the exact 14-week occupancy for `HALF_MARATHON × INTERMEDIATE × 4D × PREFERRED CORE`.

**Does NOT freeze**:

- "Every HM RaceSpecific phase is always 1/2/2" — this is a single-cell numeric decision, not a general RaceSpecific-phase rule.
- Any other frequency's occupancy (2D/3D/5D/6D at any level).
- Any other level's occupancy (Beginner/Advanced at any frequency).
- Any other horizon's occupancy (10–13 week or 15–16 week variants of this same Intermediate×4D cell).

The stage properties `HM.1.2 §11` already derived (`MANDATORY_EXPOSURE`/`COMPRESSIBLE`/`REPEATABLE_FOR_EXTENSION`/`NON_REPEATABLE`, and the parallel real-vocabulary `COMPRESSIBLE`/`PROTECTED`/`EXTENDABLE`/`FIXED_EXPOSURE` mapping) **remain the correct mechanism** for a **later phase** to resolve 10–13-week and 15–16-week occupancy (`HM.1.2 §11`'s own explicit statement that this classification "supplies only the qualitative semantics a future `WAVE HM-D` allocator will need," deliberately not frozen as a full dynamic-core allocation table). This phase does **not** attempt that resolution now, and this fixed 14W table must **not** be mistaken for that later work already being done — the later work remains entirely open, using the qualitative mechanism `HM.1.2` already built, not the fixed integers this phase freezes.

### 2.5 Consolidated update to `HM.1.2`'s own authority record

`HM.1.2 §14`'s "Remaining unsupported questions" section is superseded in place (source report text otherwise left unmodified, per this engagement's established never-erase-only-append-a-correction discipline — the `HM.1.2` file itself is not rewritten; this new decimal-sub-phase report, and the ledger/roadmap supersession notes below, are the append):

```
Item 1 -- exposure counts: FROZEN (HM.1.3).
  See HM.1.3 §2.1 for the exact fixed/eligibility-driven table.
  Classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT (not EVIDENCE_BACKED).
  Scope: HALF_MARATHON x INTERMEDIATE x 4D x PREFERRED-14W ONLY (HM.1.3 §2.4).

Item 2 -- Taper KEY-lane family choice: FROZEN (HM.1.3), closed anyway
  despite being flagged non-blocking. HM_PACE-retained (TAPER_HM_ACTIVATION)
  confirmed for both taper weeks, with an explicit non-full-load constraint
  on the second exposure. See HM.1.3 §2.1/§2.2.

Items resolved within HM.1.2's own evidence-respecting discretion
(quality-in-long-run not required; HM_PACE catalog-parameterization
classification) remain unchanged, not reopened by HM.1.3.
```

`HM.1.2 §15`'s "Exact gating conclusion for the future implementation phase" is likewise superseded in place — see §3 below for the updated gating status.

---

## 3. `HM.1.2` final status — the one remaining blocking gap and its one disclosed non-blocking preference are now both closed

| `HM.1.2 §14` item | Status |
|---|---|
| Item 1 — exposure counts (BLOCKING) | **FROZEN** (this decision, `HM.1.3`) — fixed/eligibility-driven table, §2.1 |
| Item 2 — Taper KEY-lane family choice (non-blocking) | **FROZEN** (this decision, `HM.1.3`) — `HM_PACE`-retained confirmed, with a non-full-load constraint on the second exposure, closed anyway per the governing decision's own explicit instruction |

`HM.1.2` is now **fully closed**: every one of its own required sections (stage inventory, evidence trace, phase×family eligibility, ordering, hard-budget interaction, `STANDARD` dose binding, taper semantics, pace-evidence fallback, compression/extension semantics, catalog-capability audit, and the 14-week structural reachability proof) was already genuinely closed, evidence-traced authority; the one remaining blocking numeric gap and the one disclosed non-blocking preference are both frozen by this phase. No item from `HM.1.2` remains open, unclassified, or invented-around.

---

## 4. Gating condition for `HM.2` — updated, still not executed here

`HM.1.1 §4` and `HM.1.2 §15` both recorded a conditional gating rule without resolving it. This phase updates that record to reflect the new closed state, without itself proceeding to `HM.2`:

- **A master/core/numeric-only `HM.2` scope** (no workout-catalog binding): unaffected either way, may proceed directly — unchanged from both prior phases' own gating condition.
- **A workout-materializing `HM.2` scope** (binding real workout content to the plan): was blocked on exactly `HM.1.2 §14` item 1 (exposure counts) and, as a non-blocking preference, item 2 (Taper family choice). **Both are now frozen by this phase.** A workout-materializing `HM.2` may now proceed to catalog authoring for `HALF_MARATHON × INTERMEDIATE × 4D × PREFERRED-14W`, subject to everything `HM.1`/`HM.1.1`/`HM.1.2`/`HM.1.3` have collectively frozen, and subject to the standing engagement discipline that catalog authoring itself (the actual `WORKOUT_PROGRESSION`/`PLAN_TEMPLATE`/new HM-specific `HM_PACE` workout-definition JSON documents) is barred from this phase and remains a distinct future implementation phase's own work — this phase performs no such authoring.
- **Not touched by this closure**: `HM.1.2 §11`'s qualitative stage-property mechanism remains the open path for a later phase to resolve the 10–13-week and 15–16-week occupancy rows (§2.4 above) — that generalization work is unaffected by this phase's fixed 14W numbers and is not accelerated or foreclosed by this closure.

---

## 5. What this phase explicitly did NOT do

- Did not re-derive, reinterpret, or improve on the governing decision message's exact text — recorded verbatim, per its own explicit instruction, matching `HM.1.1`'s own precedent.
- Did not re-verify the external citations (Hal Higdon Intermediate 2, B.A.A. Level Two) via new web search or literature review — recorded as given.
- Did not redefine any stage identity, eligibility matrix cell, ordering rule, dose envelope, pace-evidence fallback, or catalog-capability classification `HM.1.2` already froze — this decision adds exposure-count/Taper-family numbers on top of that unchanged foundation only.
- Did not resolve the 10–13-week or 15–16-week occupancy rows, or any other Level/Frequency cell's occupancy — explicitly out of scope, left to a later phase using `HM.1.2 §11`'s already-built qualitative mechanism.
- Did not author, modify, or publish any catalog artifact (`WORKOUT_PROGRESSION`, `PLAN_TEMPLATE`, or workout-definition JSON) — catalog authoring remains barred from this phase, deferred to `HM.2`.
- Did not decide `HM.2`'s scope shape — only recorded the updated (now largely unblocked, for the workout-materializing case) gating condition for whoever scopes it next.
- Did not write, modify, or delete any production code, test, or catalog artifact.
- Did not touch any 10K-reachable code path, authority, or catalog content.
- Did not widen any public routing surface.

---

## 6. Final classification

```
HM_WORKOUT_PROGRESSION_STAGE_CATALOG_CLOSED
```

`HM.1.2`'s one remaining blocking gap (exact per-`KEY_SESSION`-lane-stage exposure counts for `HALF_MARATHON × INTERMEDIATE × 4D × PREFERRED-14W`) is now frozen as a deterministic, fixed/eligibility-driven table — `FOUNDATION_AEROBIC_BASE=3`; Build's `1/3` or `0/4` split driven purely by `FasterThanHM` eligibility; `RACE_SPECIFIC_HM_INTRODUCTION=1`/`_DEVELOPMENT=2`/`_REHEARSAL=2`; `TAPER_HM_ACTIVATION=2` with an explicit non-full-load constraint on its second exposure — classified `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, explicitly not `EVIDENCE_BACKED`, and scoped strictly to this one cell (§2.4). `HM.1.2`'s disclosed non-blocking Taper-family preference is closed in the same pass. This closes `HM.1.2` in full — every required section is now genuinely closed, evidence-traced or decision-frozen authority, with the explicit scope constraint recorded prominently so this fixed 14W table is never mistaken for the still-open 10–13/15–16-week generalization work `HM.1.2 §11`'s qualitative mechanism remains correctly reserved for. No production code, catalog artifact, or public routing change in this phase. No 10K authority, code, or catalog touched.
