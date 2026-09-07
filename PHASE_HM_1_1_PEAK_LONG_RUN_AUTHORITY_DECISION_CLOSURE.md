# PHASE HM.1.1 — HALF-MARATHON PEAK LONG-RUN AUTHORITY DECISION AND HM.1 FULL CLOSURE

**Phase type**: DECISION (decision-recording/governance-closure only — no production code, no HM catalog artifacts, no public routing changes, no 10K changes of any kind)
**Execution status**: DONE
**Final classification**: `HALF_MARATHON_INTERMEDIATE_4D_14W_PEAK_LONG_RUN_FROZEN_EVIDENCE_INFORMED_PRODUCT_DEFAULT_HM_1_FULLY_CLOSED`
**Governance commit**: `4d85703` (implementation + ledger row, backfilled self-referentially per this engagement's established two-commit pattern).
**Parent**: `HM.1` (`PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md`), which escalated Item 2 (peak long-run, `OPEN-HM-02`) as `DOMAIN_DECISION_REQUIRED` with four scored options, none selected.

---

## 0. What this phase is

A direct user decision message — "DECISION ON HM.1 ITEM 2" — resolving `HM.1`'s escalated peak-long-run item and closing `HM.1` out entirely. The message's exact text is the spec; this phase records it verbatim rather than reinterpreting or improving on it, matching this engagement's own established precedent for direct-decision phases (`GEN.16`, `GEN.24`, `GEN.31`, `GEN.36`, `GEN.37`) of recording a user's decision rationale as given rather than re-deriving it.

## 1. Startup confirmation

- `git fetch origin` clean; `git rev-list --left-right --count origin/main...HEAD` = `0  0` before starting.
- Worktree changes limited to the pre-existing tracked `bin`/`obj` rebuild-artifact noise this engagement has repeatedly called out as leftover verification-run byproduct — left untouched, not staged.
- Both required source documents read in full before writing anything:
  - `PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md` (the immediate predecessor, containing Item 2's four escalated options and the already-frozen/partially-frozen Items 1/3/4).
  - `HM_21K_Claude_Handoff_and_Build_Plan.md`, specifically re-confirming §6 `OPEN-HM-02`'s exact self-disclaimer language for the ~18–19 km legacy candidate: *"Legacy candidates include approximately 18–19 km, but this is not yet treated as a frozen canonical peak-long-run authority."* This decision's authority is explicitly grounded elsewhere (§3 below), not in that disclaimed candidate.
- `PHASE_LEDGER.md`'s HM Program table (rows 1–3: `HM.0`, `HM.0.1`, `HM.1`) confirms no `HM.1.1` row exists yet. Chosen phase ID: `HM.1.1` — mirroring the exact numbering discipline `HM.0.1` already established (a decimal sub-phase recording a decision/correction pass closing out the immediately-preceding integer phase's own remaining item), and deliberately *not* `HM.2`, since the governing decision message itself uses "HM.2" throughout to name the future implementation phase (master/core/numeric vertical slice or workout-catalog materialization) — reusing that name here would collide with an already-reserved identifier.

---

## 2. Item 2 — `OPEN-HM-02`, peak long-run authority: **FROZEN**

### 2.1 Frozen authority — recorded exactly as decided

```
HALF_MARATHON x INTERMEDIATE x 4D x 14-WEEK PREFERRED CORE

PreferredAbsolutePeakLongRunKm = 19.0 km

Authority classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT
(explicitly NOT EVIDENCE_BACKED -- no causal evidence establishes 19km
as uniquely optimal; this is a practice-convergence-informed ceiling)
```

### 2.2 Rationale — independent real-world convergence, not the legacy candidate's own authority

The handoff document's ~18–19 km legacy candidate (§6 `OPEN-HM-02`, quoted in full in §1 above) is explicitly self-disclaimed as non-authoritative by its own source text. **This decision does NOT rely on that candidate's own authority.** Instead, independently verified published Half-Marathon programs converge in the same range:

- Hal Higdon Intermediate 1: 12 mi (~19.3 km)
- Hal Higdon Intermediate 2: 12 mi (~19.3 km), explicitly a 5-day-running intermediate program peaking one week before race
- ASICS first-HM/beginner plan: 18 km
- B.A.A. Level Two (intermediate-equivalent): up to 11–12 mi (~17.7–19.3 km)

19.0 km (not 19.3) is selected deliberately: the `.3` in Higdon's figure is an artifact of mile-to-km conversion (12 mi), carrying no additional meaning for Appsel's own metric-based system. 19.0 sits at the upper end of the observed convergence band, conservative relative to the full 18–19.3 range.

These external citations are recorded as given, per the governing decision message's explicit instruction — not re-verified via web search in this phase, matching how this engagement's own decision-recording phases (e.g. `GEN.16`) recorded a user's decision rationale verbatim rather than re-deriving it.

### 2.3 Critical invariant — ceiling, not mandatory target

**19.0 km is an ABSOLUTE PREFERRED CEILING, not a required milestone.** Actual peak long run may legitimately land below 19 km when constrained by any of:

- starting-volume evidence;
- recent-longest-run evidence;
- reachable weekly progression;
- session-allocation feasibility;
- total weekly-volume feasibility;
- recovery/adaptation state.

The generator **MUST NOT** accelerate weekly volume or long-run progression merely to reach 19 km. If a real athlete's evidence-based trajectory tops out at, say, 16 km given their starting point and the available weeks, that is a **valid, correct outcome** — not a shortfall to be engineered around. This mirrors the Intermediate×4D peak-volume envelope (36–50 km/week, `HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2): a user who can only safely reach 34–36 km/week must not have their long run inflated to "reach" a 19 km target that assumes higher weekly volume than they can support.

### 2.4 Explicit scope constraint — do not generalize beyond this cell

This freezes **ONLY**: `HALF_MARATHON × INTERMEDIATE × 4D × PREFERRED-14W → 19.0 km`.

Explicitly **NOT** yet decided (do not infer or extrapolate from this value):

- Beginner×3D/4D
- Intermediate×3D/5D
- Advanced×3D/4D/5D
- 2D/6D at any level

B.A.A.'s own level-differentiated data (Level One ~9–10 mi, Level Three 13–14 mi) demonstrates real level-based variance exists — do **not** assume 19.0 km transfers to other levels/frequencies. Whether peak long-run needs a full Distance×Level×Frequency table (like the existing peak-volume band table, §4.2 of the handoff document) is a question for a **later phase**, once the first vertical slice's real behavior is observed. This phase does not build that table now — doing so would be premature generalization for a single-cell first slice.

### 2.5 Consolidated update to `HM.1`'s own authority record

`HM.1 §6`'s consolidated `HALF_MARATHON_CORE_AUTHORITY` block is updated in place (source report text otherwise left unmodified, per this engagement's established never-erase-only-append-a-correction discipline) to replace the `PeakLongRun (DOMAIN_DECISION_REQUIRED)` section with:

```
  PeakLongRun (FROZEN -- HM.1.1):
    PreferredAbsolutePeakLongRunKm: 19.0 km
    Authority classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT
      (NOT EVIDENCE_BACKED -- practice-convergence-informed ceiling, not
      causally proven optimal)
    Invariant: CEILING, NOT MANDATORY TARGET -- actual peak long run may
      legitimately land below 19km when constrained by starting-volume
      evidence, recent-longest-run evidence, reachable weekly progression,
      session-allocation feasibility, weekly-volume feasibility, or
      recovery/adaptation state. The generator MUST NOT accelerate
      progression merely to reach 19km.
    Scope: HM x INTERMEDIATE x 4D x PREFERRED-14W ONLY. Does not transfer
      to any other Level/Frequency cell. No Distance x Level x Frequency
      peak-long-run table built here -- premature generalization for a
      single-cell first slice.
```

---

## 3. HM.1 final status — all four items now closed or appropriately scoped

| Item | `OPEN-HM-*` | Status |
|---|---|---|
| 1 — 14W phase split | `OPEN-HM-01` | **FROZEN** (`HM.1`) — `3 Foundation / 4 Build / 5 RaceSpecific / 2 Taper` |
| 2 — Peak long-run (Intermediate×4D×14W) | `OPEN-HM-02` | **FROZEN** (this decision, `HM.1.1`) — `19.0 km`, `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, ceiling-not-target, single-cell scope only |
| 3 — Workout progression | `OPEN-HM-03` | **PARTIALLY FROZEN** (`HM.1`) — dose/stage semantics frozen; exact stage catalog (concrete workout identities per stage) remains an explicit, disclosed gap |
| 4 — Quality-long-run hard-budget policy | `OPEN-HM-04` | **FROZEN** (`HM.1`) |

`HM.1` is now fully closed: 3 of 4 items fully frozen (1, 2, 4), 1 item partially frozen with a precisely-classified, disclosed remaining gap (Item 3, unchanged by this phase).

---

## 4. Gating condition for HM.2 — recorded, not resolved here

This phase records the following condition for whoever drafts the `HM.2` prompt next; it does **not** decide which shape `HM.2` will actually need:

- **If `HM.2`'s scope is limited to master/core/numeric vertical slice** (no workout-catalog binding): it may proceed directly — Item 3's remaining gap (exact stage catalog identities, exposure minima/maxima) does **not** block it.
- **If `HM.2`'s scope includes materializing actual workouts** (binding real workout content to the plan): Item 3's exact stage catalog must be closed **FIRST**, as its own small closure phase, before `HM.2` proceeds. `HM.2` must **not** improvise workout identities to fill this gap.

---

## 5. What this phase explicitly did NOT do

- Did not re-verify the external citations (Hal Higdon, ASICS, B.A.A.) via new web search or literature review — recorded as given, per the governing decision message's explicit instruction.
- Did not build a Distance×Level×Frequency peak-long-run table for any cell beyond Intermediate×4D×14W.
- Did not resolve Item 3's disclosed workout-progression-stage-catalog gap — left exactly as `HM.1` disclosed it.
- Did not decide `HM.2`'s scope shape — only recorded the conditional gating rule for whoever scopes it next.
- Did not write, modify, or delete any production code, test, or catalog artifact.
- Did not touch any 10K-reachable code path, authority, or catalog content.
- Did not widen any public routing surface.

---

## 6. Final classification

```
HALF_MARATHON_INTERMEDIATE_4D_14W_PEAK_LONG_RUN_FROZEN_EVIDENCE_INFORMED_PRODUCT_DEFAULT
HM_1_FULLY_CLOSED
```

Item 2 (`OPEN-HM-02`) is now frozen at `19.0 km`, classified `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (explicitly not `EVIDENCE_BACKED`), recorded as an absolute preferred ceiling — never a mandatory target — and scoped strictly to `HALF_MARATHON × INTERMEDIATE × 4D × PREFERRED-14W`. This closes `HM.1` in full: Items 1, 2, and 4 are frozen; Item 3 remains partially frozen with its gap precisely disclosed, not invented around. The gating condition for `HM.2` is recorded, not resolved. No production code, catalog artifact, or public routing change in this phase. No 10K authority, code, or catalog touched.
