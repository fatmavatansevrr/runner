# PHASE HM.1 — HALF-MARATHON CORE-HORIZON AUTHORITY CLOSURE FOR INTERMEDIATE × 4D × 14W

**Type:** EVIDENCE / DECISION (decision/authority-closure only — no production code, no HM catalog artifacts, no public routing changes, no 10K changes of any kind)
**Parent:** `HM.0` / `HM.0.1` (`HM_DISTANCE_GENERALIZATION_READY_FOR_FIRST_DARK_VERTICAL_SLICE`, scoped to the Core horizon only)
**Scope discipline:** closes authority using ONLY the evidence already present in `HM_21K_Claude_Handoff_and_Build_Plan.md` (the sole permitted evidence base for this phase). No new external research performed, no book/literature search reopened, no number invented that is not traceable to that document. Where its evidence is genuinely insufficient to freeze an item, that item is escalated as `DOMAIN_DECISION_REQUIRED` with concrete options rather than resolved unilaterally, per the rigor precedent of `PHASE_10K_GEN_14_2D_PROGRESSION_STAGE_EXPOSURE_PACING_AUTHORITY_PROPOSAL.md`.

---

## 0. Startup confirmation

- `git fetch` clean; `git rev-list --left-right --count origin/main...HEAD` = `0  0` before starting. Worktree changes limited to the pre-existing tracked `bin`/`obj` rebuild-artifact noise called out by this engagement's own standing convention — left untouched, not staged.
- `git log -5` at start: `a5c625b` docs(hm-0.1) SHA backfill, `737c039` HM.0.1 corrective pass, `910b6d8` docs(hm-0) SHA backfill, `6f5fae5` HM.0, `a4ddca7` docs(gen-39) SHA backfill — consistent with `PHASE_LEDGER.md`'s own HM Program table (rows `HM.0`, `HM.0.1`).
- `PHASE_LEDGER.md`'s HM Program table (rows 1–2) confirms no `HM.1` row exists yet. **Next free phase ID confirmed: `HM.1`.**
- Both required source documents read in full before writing anything: `PHASE_HM_0_DISTANCE_GENERALIZATION_REUSE_AUDIT.md` (447 lines, as corrected by `HM.0.1`) and `HM_21K_Claude_Handoff_and_Build_Plan.md` (1184 lines, in full — not re-read from `HM.0`'s own excerpts). `PHASE_10K_GEN_14_2D_PROGRESSION_STAGE_EXPOSURE_PACING_AUTHORITY_PROPOSAL.md` read in full as the cited rigor precedent for Item 2's escalation shape (derive-if-evidence-supports-it, else escalate with concrete, evidence-scored options rather than pick one unilaterally).

---

## 1. Scope — exactly four items, per the governing instruction

This phase closes exactly: `OPEN-HM-01`'s 14-week row only (Item 1), `OPEN-HM-02` peak long-run authority for the Intermediate×4D cell only (Item 2), `OPEN-HM-03` exact HM workout progression for Intermediate×4D×14W only (Item 3), and `OPEN-HM-04` quality-long-run/hard-budget policy for Intermediate×4D only (Item 4). `OPEN-HM-01`'s remaining 10/11/12/13/15/16-week rows, `OPEN-HM-05` (2D), `OPEN-HM-06` (6D), and `OPEN-HM-07` (>16wk composition) are **not** touched here — confirmed correctly out of scope by `HM.0.1 §J`'s own first-slice-relevance analysis, re-confirmed by direct re-read of the handoff document's own text in this phase.

---

## 2. Item 1 — `OPEN-HM-01`, 14-week phase allocation row: **FROZEN**

**Source citation (exact):** `HM_21K_Claude_Handoff_and_Build_Plan.md` §4.1 (lines 139–158):

> ```
> Half Marathon core:
> minimum   = 10 weeks
> preferred = 14 weeks
> maximum   = 16 weeks
> ```
> Preferred 14-week phase shape:
> ```
> Foundation    3
> Build         4
> RaceSpecific  5
> Taper         2
> ```
> Do not invent the exact 10/11/12/13/15/16-week allocation table yet. That is an explicit remaining product-policy closure item.

And §6, `OPEN-HM-01` (lines 709–728):

> Known:
> ```
> 14 weeks = 3 Foundation / 4 Build / 5 RaceSpecific / 2 Taper
> ```
> Unknown/final decision required:
> ```
> 10 weeks → ?
> 11 weeks → ?
> 12 weeks → ?
> 13 weeks → ?
> 15 weeks → ?
> 16 weeks → ?
> ```

**Verification:** 3 + 4 + 5 + 2 = 14 — arithmetically self-consistent with the document's own stated preferred core length. The document states this row as `Known`, not as a candidate needing derivation — this is a mechanical formalization of an already-stated fact, not a new decision.

**FROZEN AUTHORITY — `HALF_MARATHON_PHASE_ALLOCATION` (Intermediate × 4D × 14W only):**

| Phase | Weeks |
|---|---|
| Foundation | 3 |
| Build | 4 |
| RaceSpecific | 5 |
| Taper | 2 |
| **Total** | **14** |

**Explicitly not decided here** (out of scope, per the governing instruction and confirmed still-open in the source document): the 10/11/12/13/15/16-week allocation table. This remains for the handoff's own `WAVE HM-D` ("Dynamic 10–16 week core," explicitly gated to run only after `WAVE HM-C` — this slice — is green).

---

## 3. Item 2 — `OPEN-HM-02`, peak long-run authority: **`DOMAIN_DECISION_REQUIRED`**

### 3.1 Why this escalates rather than freezes

**Source citation (exact):** `HM_21K_Claude_Handoff_and_Build_Plan.md` §6, `OPEN-HM-02` (lines 730–742):

> Legacy candidates include approximately 18–19 km, but this is not yet treated as a frozen canonical peak-long-run authority.
>
> Need to determine:
> - absolute distance vs duration vs weekly-share interaction;
> - level effects;
> - frequency effects;
> - peak placement;
> - whether any users can/should exceed 21.1 km in training;
> - how quality-long-run content affects the cap.
>
> Do not confuse starting long-run caps with peak-long-run caps.

I re-read the full document specifically searching for any additional peak-long-run evidence beyond this passage — including §4.2 (peak weekly-*volume* bands, a distinct quantity), §4.3 (explicitly *starting*-long-run caps, which the document itself warns not to confuse with peak), §3 ("long-run share/cap" listed only as a mechanism that *can* be generic while its numeric value "remains HM-specific" — no value supplied), and HM-LIT-15 through HM-LIT-27 (§5) — none of which supplies a peak-long-run distance, duration, or share percentage for HM at any level or frequency. The only concrete figure anywhere in the document for this quantity is the "~18–19 km" legacy figure, which the document's own text explicitly declines to treat as frozen.

This confirms `HM.0.1 §J`'s own assessment verbatim: *"Weak/unfrozen candidate only... This is real unresolved product/research territory, not merely awaiting sign-off."* Per this phase's explicit instruction not to force a freeze where the evidence doesn't support one, and not to invent a mechanism or number the document does not itself supply, **Item 2 is escalated.**

### 3.2 Concrete options (mirroring `GEN.14`'s evidence-scored-options rigor — none selected unilaterally)

| # | Candidate mechanism | Evidence basis in the handoff document | Confidence |
|---|---|---|---|
| **A** | **Absolute-km figure**: adopt ~18–19 km directly as Intermediate×4D's peak long run. | Explicitly named in §6 `OPEN-HM-02` as a "legacy candidate." No external citation is given for it anywhere in the document, no level/frequency differentiation is stated (unclear if it is a blanket figure or specific to any one cell), and the same paragraph immediately lists five open sub-questions (level effects, frequency effects, distance-vs-duration-vs-share interaction, peak placement, whether exceeding 21.1 km is ever appropriate) that this single figure does not resolve. | **LOW** — the source document itself explicitly disclaims this as not-frozen. |
| **B** | **Weekly-volume-share mechanism**: peak long run = a fixed percentage of that week's peak weekly-volume target, computed against the already-available Intermediate×4D peak-volume band (36–50 km/week, §4.2). | The document names "long-run share/cap" in §3 as a mechanism that *can* be generic while the *value* stays HM-specific — but supplies zero percentage figure anywhere for HM. Reusing 10K's own share percentage would violate the document's own repeated instruction not to copy 10K numeric values into HM (§0, and the explicit warnings in `OPEN-HM-05`/`OPEN-HM-06` against exactly this kind of cross-distance numeric borrowing). | **NONE** for any specific percentage — the mechanism shape is plausible, but zero HM-specific data exists to populate it. |
| **C** | **Duration-based cap**: a time-at-goal-pace ceiling instead of an absolute distance. | Named only as an open sub-question inside `OPEN-HM-02` itself ("absolute distance vs duration... interaction") — i.e. the document raises this as something to determine, not as something it has already determined. No duration figures for peak long run appear anywhere in the document. | **NONE** — pure mechanism naming, zero data. |
| **D** | **Level × Frequency combination table**: a distinct peak-long-run figure per level/frequency cell, mirroring the shape already used for peak weekly-volume bands (§4.2). | `OPEN-HM-02` flags "level effects; frequency effects" as things to determine, and §4.2 demonstrates this table shape is already in use for a related (but distinct) quantity — implying eventual differentiation is likely correct in shape, but no actual per-cell long-run values exist anywhere in the document for this quantity. | **NONE** for values — only a structural analogy to a table used for a different metric. |

**Honest conclusion:** none of the four is freezable today from the handoff document's evidence alone. Option A is the only one with a named figure at all, and the document that names it explicitly withholds authority from it. Options B–D are plausible mechanism shapes with zero populated HM data — adopting any of them now would mean inventing the actual number, which this phase's hard constraint forbids. **This is reported back as a real decision for the user to make** (either explicitly accept Option A as a provisional, LOW-confidence starting figure pending a dedicated future evidence phase, or direct that a dedicated `OPEN-HM-02`-focused research/evidence phase be opened before any HM long-run peak is frozen for any cell). No option is implemented in this phase.

---

## 4. Item 3 — `OPEN-HM-03`, exact HM workout progression stages: **PARTIALLY FROZEN — envelope frozen, exact stage identities/exposure minima remain a disclosed gap**

### 4.1 What the evidence supports freezing now

**Source citations:**

- **HM-LIT-17** (§5, lines 320–375) — dose-tier model and dose envelopes:
  > Preferred model: `workoutFamily × progressionStage × doseTier`. Main-set dose excludes warm-up; recovery jog; cool-down (those still count in total planned training volume).
  >
  > ```
  > Faster / 5K–10K work     LOW 8–12 min   STANDARD 12–18 min   HIGH 16–24 min
  > Threshold                LOW 15–20 min  STANDARD 20–30 min   HIGH 25–40 min
  > HM Pace                  LOW 15–25 min  STANDARD 25–45 min   HIGH 35–60 min
  > HM Pace inside Long Run  LOW 10–20 min  STANDARD 20–35 min   HIGH 30–45 min
  > ```
  > Candidate level mapping: `Beginner/New → LOW`, `Intermediate → STANDARD`, `Advanced → HIGH`.

- **HM-LIT-16** (§5, lines 292–318) — adjunct-vs-KEY distinction:
  > `STRIDES / SHORT_HILL_STRIDES` → adjunct, short, controlled, full recovery, does not consume a full hard-session slot. `STRUCTURED_HILL_REPETITIONS` → `KEY_SESSION`, hard stimulus. Candidate strides: 4–8 × 15–20 sec, controlled-fast, not sprinting, full easy recovery.

- **`OPEN-HM-03`** (§6, lines 744–764) — the phase-relative skeleton:
  > ```
  > Foundation      → aerobic / strides / basic quality
  > Build           → threshold development
  > RaceSpecific    → HM-specific introduction → HM-specific development → HM-specific rehearsal
  > Taper           → reduced-volume HM activation
  > ```
  > But exact stages, candidate workout identities and exposure minima/maxima must be adopted explicitly.

- **HM-LIT-21** (§5, lines 476–517) — the calendar-spacing constraints these stages must respect: KEY-vs-quality-LONG hard minimum ~48h, preferred ≥72h; two true hard stimuli minimum ~48h; `HARD+HARD` adjacency prohibited; "do not compress two hard sessions together to satisfy the calendar."

Since Intermediate maps to `STANDARD` dose tier (HM-LIT-17), this phase formalizes the following as a **frozen envelope** (mechanism/dose-range/tier-mapping level, not exact catalog-content level) for Intermediate × 4D × 14W:

**FROZEN AUTHORITY — `HALF_MARATHON_WORKOUT_PROGRESSION_ENVELOPE` (Intermediate × 4D × 14W only):**

| Phase (weeks, per Item 1) | Conceptual stage focus | Applicable dose family (STANDARD tier, Intermediate) |
|---|---|---|
| Foundation (3) | Aerobic base / strides / basic quality | Faster/5K–10K work — 12–18 min main-set |
| Build (4) | Threshold development | Threshold — 20–30 min main-set |
| RaceSpecific (5) | HM-specific introduction → development → rehearsal (3 sub-stages) | HM Pace — 25–45 min main-set; HM Pace inside Long Run — 20–35 min |
| Taper (2) | Reduced-volume HM activation | Reduced dose from whichever family was last active; no new family introduced |

Non-`KEY_SESSION` adjunct content (strides/short hill strides, 4–8×15–20sec) may appear in any phase without consuming a hard-session slot; `STRUCTURED_HILL_REPETITIONS` is `KEY_SESSION`-tier and does consume one. All stage sequencing must respect HM-LIT-21's spacing/adjacency rules (formalized jointly with Item 4, §5 below).

### 4.2 What remains a genuine, disclosed gap — not invented here

The handoff document's own text is explicit that the following are **not yet decided**, and this phase does not invent them:

1. **Exact catalog-shaped stage identities/names** — e.g. no catalog key equivalent to 10K's `TEN_K_SPECIFIC_INTRO`/`GOAL_PACE_REHEARSAL` exists anywhere in the document for HM's "HM-specific introduction/development/rehearsal" sub-stages. These are named as concepts, not as adoptable identities.
2. **Exposure minima/maxima per stage, per phase, for this exact level/frequency cell** — no number of times a given stage must repeat within the 3-week Foundation, 4-week Build, or 5-week RaceSpecific phase (for Intermediate×4D specifically) appears anywhere in the document. This is the exact `MinimumExposures`/`MaximumExposures`-shaped authority the 10K engagement's own `ProgressionStageAllocator` requires (`GEN.13`→`GEN.17`'s precedent), and here it is simply absent from the evidence base — not thin, absent.
3. **Exact workout identity for the goal-pace/HM-pace KEY content itself** — the dose *envelope* (minutes) is known; the specific session structure (e.g. "3×2km @ HM pace" vs. "1×20min continuous @ HM pace") is not specified anywhere.

**Classification:** this is closer to `HM.0`'s own Classification-D shape (`HM_NEW_DISTANCE_AUTHORITY_REQUIRED` — the architecture already has the slot, per `ProgressionStageAllocator`'s existing exposure-minima mechanism) than to a genuine multi-candidate `DOMAIN_DECISION_REQUIRED` — there is no conflicting evidence to adjudicate between, only a real absence of data. Per the governing instruction to "classify precisely what's missing rather than inventing values," this phase does **not** author placeholder exposure numbers. A dedicated future evidence/decision phase (mirroring 10K's `GEN.13`→`GEN.16` progression-stage-exposure sequence) is required before any HM workout-progression catalog content can be authored — catalog authoring itself remains barred regardless, per this phase's own standing constraint.

---

## 5. Item 4 — `OPEN-HM-04`, quality-long-run vs. weekly hard-budget policy: **FROZEN**

### 5.1 Source citation and an honest accuracy note on the governing framing

**Source citation (exact):** `HM_21K_Claude_Handoff_and_Build_Plan.md`, HM-LIT-17 (§5, lines 371–373):

> Quality long run:
> - threshold/HM-pace/structured-hard finish => consumes quality budget;
> - controlled aerobic steady finish does not automatically need to count as a hard stimulus.

Operationalized jointly with HM-LIT-21 (§5, lines 476–517):

> KEY vs quality LONG: hard minimum ~48h, preferred >=72h. Adjacency: HARD + HARD → prohibited; HARD + EASY → allowed; EASY + LONG → allowed when LONG is easy. ... Do not compress two hard sessions together to satisfy the calendar.

And `OPEN-HM-04` itself (§6, lines 766–777), which frames the exact-per-frequency-table question as still open but explicitly names HM-LIT-17/HM-LIT-21 as the informing evidence — matching `HM.0.1 §J`'s own classification of this item as "substantially pre-informed by already-closed research."

**Accuracy note (disclosed per this phase's own honesty requirement):** the governing prompt for this phase characterized this rule as "mirroring the 10K fixture's existing approach, per the handoff's own explicit reference." I searched the full handoff document (case-insensitive, for `fixture`, `mirror`, `containsQuality`, `hard-budget`, `golden`) and found **no literal sentence in the handoff document that references the 10K golden fixture's own `containsQualitySegment` mechanism by name or draws that comparison explicitly**. The document's only "mirror" references are unrelated (§8: "avoid inventing 2D/6D HM volume policy merely to mirror the 10K product matrix"). The substantive rule itself — a long run's hard/easy finish character determines whether it consumes the weekly quality budget — **is** genuinely and explicitly stated in HM-LIT-17, and is sufficient on its own to freeze this item; I am simply flagging that the specific "mirrors the 10K fixture, per the handoff's own reference" attribution does not have a literal citation I could locate in the source text, so it should not be repeated as a sourced claim.

### 5.2 Formalization for Intermediate × 4D

Intermediate×4D's structural template (`RUN_LAYOUT_4D`, already-generic and reused per `HM.0 §D`) carries 1 `KEY_SESSION` + 1 `LONG_RUN` + easy sessions per week. Applying HM-LIT-17 + HM-LIT-21 to this shape:

**FROZEN AUTHORITY — `HALF_MARATHON_QUALITY_LONG_RUN_POLICY` (Intermediate × 4D):**

1. `containsQualitySegment = true` for a given week's `LONG_RUN` when it contains a threshold, HM-pace, or structured-hard finish segment.
2. `containsQualitySegment = false` when the `LONG_RUN` finishes as controlled/steady/aerobic — such a run does not automatically count as a hard stimulus.
3. When `containsQualitySegment = true`, that week's `LONG_RUN` consumes the weekly quality/hard-session budget, i.e. it is counted alongside the week's `KEY_SESSION` for hard-adjacency purposes.
4. `HARD + HARD` adjacency (a hard `KEY_SESSION` and a quality `LONG_RUN` in the same week) is not prohibited outright, but is bound by HM-LIT-21's spacing floor: minimum ~48h between the two hard stimuli, preferred ≥72h. The calendar must not compress the two together merely to fit `PreferredDays`.
5. This rule is frequency-independent in its statement (HM-LIT-17/21 make no 3D/4D/5D/6D distinction), but its *practical effect* varies naturally by frequency because the number of weekly hard-eligible slots differs — for Intermediate×4D specifically there is exactly one `KEY_SESSION` and one `LONG_RUN` per week, so the rule reduces to the two-stimulus spacing case above; the fuller per-3D/4D/5D/6D table `OPEN-HM-04` asks for is not attempted here (out of scope — this phase closes only the Intermediate×4D cell).

---

## 6. Consolidated authority — `HALF_MARATHON × INTERMEDIATE × 4D × 14W`

This section is the single coherent authority record for this cell, combining Items 1–4:

```
HALF_MARATHON_CORE_AUTHORITY
  Level:            INTERMEDIATE
  Frequency:        4D
  CoreCycle:        14 weeks (preferred; min/max bounds around it remain OPEN, out of scope — Item 1)

  PhaseAllocation (FROZEN — §2):
    Foundation:      3 weeks
    Build:           4 weeks
    RaceSpecific:    5 weeks
    Taper:           2 weeks

  PeakLongRun (DOMAIN_DECISION_REQUIRED — §3):
    STATUS:          NOT FROZEN
    Options tabled:  A (absolute ~18-19km, LOW confidence, explicitly disclaimed by source)
                      B (weekly-volume-share mechanism, NONE confidence, no HM % value exists)
                      C (duration-based cap, NONE confidence, no HM data exists)
                      D (level x frequency table, NONE confidence, no HM values exist)
    ACTION:          escalated to user; no option selected

  WorkoutProgressionEnvelope (PARTIALLY FROZEN — §4):
    Dose-tier (Intermediate -> STANDARD):
      Faster/5K-10K:            12-18 min main-set
      Threshold:                20-30 min main-set
      HM Pace:                  25-45 min main-set
      HM Pace inside Long Run:  20-35 min main-set
    Phase-relative stage concept:
      Foundation -> aerobic/strides/basic quality
      Build -> threshold development
      RaceSpecific -> HM-specific intro -> development -> rehearsal
      Taper -> reduced-volume HM activation
    Adjunct (non-KEY): strides/short hill strides, 4-8 x 15-20sec, full recovery
    KEY-tier hill work: STRUCTURED_HILL_REPETITIONS
    GAP (not invented): exact stage catalog identities; exposure minima/maxima per
      stage per phase for this cell; exact KEY-session workout identity for HM-pace
      content. Requires a dedicated future evidence/decision phase before any HM
      workout-progression catalog content may be authored.

  QualityLongRunPolicy (FROZEN — §5):
    containsQualitySegment = true   when LONG_RUN finishes threshold/HM-pace/hard
    containsQualitySegment = false  when LONG_RUN finishes controlled/steady/aerobic
    true  => LONG_RUN consumes the weekly quality/hard-session budget
    Spacing vs KEY_SESSION: hard minimum ~48h, preferred >=72h
    Calendar must not compress two hard stimuli together to fit PreferredDays
```

**Net phase result:** 2 of 4 items fully frozen (Items 1, 4), 1 item partially frozen with a precisely-classified, disclosed remaining gap (Item 3), 1 item genuinely escalated as `DOMAIN_DECISION_REQUIRED` with four scored options and no unilateral selection (Item 2).

---

## 7. What this phase explicitly did NOT do

- Did not perform any new external research, literature search, or evidence gathering beyond re-reading `HM_21K_Claude_Handoff_and_Build_Plan.md` in full.
- Did not invent a peak-long-run number, workout-progression exposure count, or stage identity not already present in that document.
- Did not resolve `OPEN-HM-01`'s 10/11/12/13/15/16-week rows, `OPEN-HM-05` (2D), `OPEN-HM-06` (6D), or `OPEN-HM-07` (>16wk composition) — all confirmed out of scope for this slice, consistent with `HM.0.1 §J`.
- Did not write, modify, or delete any production code, test, or catalog artifact.
- Did not touch any 10K-reachable code path, authority, or catalog content.
- Did not widen any public routing surface.

---

## 8. Recommended next phase

1. **A human decision on Item 2** (§3.2) — accept Option A as a provisional LOW-confidence starting figure pending later refinement, or direct a dedicated `OPEN-HM-02` evidence/decision phase before any HM peak-long-run number is frozen for any cell.
2. **A dedicated HM workout-progression exposure-pacing phase** for Item 3's disclosed gap (§4.2), mirroring the 10K engagement's own `GEN.13`→`GEN.17` sequence, before any catalog authoring is attempted.
3. Only after both close: `HM.2` — implementation phase (mechanical fallback-defect closure in `CatalogVolumeAndLongRunPlanner`, additive widening of `V1CatalogPilotIdentityPolicy`, authoring the minimum HM catalog artifact, dark-verification of the full 14-week slice) — per `HM.0 §P`'s own recommended sequencing, still not scheduled as a Phase ID here.

---

## Final classification

```
HALF_MARATHON_INTERMEDIATE_4D_14W_CORE_AUTHORITY_PARTIALLY_FROZEN
PEAK_LONG_RUN_DOMAIN_DECISION_REQUIRED
```

Phase allocation (Item 1) and quality-long-run/hard-budget policy (Item 4) are frozen, final authority for this cell. Workout-progression envelope (Item 3) is frozen at the dose/tier/phase-concept level only, with a precisely-classified, disclosed gap (exact stage identities and exposure minima/maxima) requiring a dedicated future phase. Peak long-run authority (Item 2) is not frozen — genuinely escalated with four scored options, none selected. No production code, catalog artifact, or public routing change in this phase. No 10K authority, code, or catalog touched.
