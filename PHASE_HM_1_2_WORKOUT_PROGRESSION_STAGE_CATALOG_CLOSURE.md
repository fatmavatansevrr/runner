# PHASE HM.1.2 — HALF-MARATHON WORKOUT-PROGRESSION STAGE-CATALOG CLOSURE (INTERMEDIATE × 4D × 14W)

**Type:** EVIDENCE / DECISION (governance/decision only — no production code, no catalog artifacts, no public routing changes, no 10K changes of any kind)
**Parent:** `HM.1` (`PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md`, Item 3 partially frozen) / `HM.1.1` (`PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md`, Item 2 frozen, HM.1 fully closed, Item 3's gap unchanged and explicitly carried forward)
**Governing prompt:** user-authored "PHASE HM PROMPT 1.2" — verbatim text defines scope; followed precisely.

---

## 0. Startup confirmation

- `git fetch origin` clean; repo confirmed `0` behind/ahead of `origin/main` before starting.
- `git log -5`: `4a52944` docs(hm-1-1) SHA backfill, `4d85703` HM.1.1, `02a077e` docs(hm-1) SHA backfill, `e39b612` HM.1, `a5c625b` docs(hm-0.1) SHA backfill — consistent with `PHASE_LEDGER.md`'s HM Program table (rows 1–4: `HM.0`, `HM.0.1`, `HM.1`, `HM.1.1`).
- `PHASE_LEDGER.md`'s HM Program table confirms no `HM.1.2` row exists yet. **Next free phase ID confirmed: `HM.1.2`** (distinct from the reserved implementation-phase name `HM.2`).
- Worktree `bin`/`obj` rebuild-artifact noise present, per this engagement's standing convention — left untouched, not staged.

---

## 1. Source inventory actually used

**Read in full before writing anything:**
- `PHASE_LEDGER.md` (HM Program table, rows 1–4) and `MASTER_ROADMAP.md` §16 (HM Program current state).
- `HM_21K_Claude_Handoff_and_Build_Plan.md` — **read in its entirety, 1183 lines, not excerpted**.
- `PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md` (full).
- `PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md` (full).
- Real catalog JSON: `plan-catalog/catalog/workouts/*.json` (all versions of `EASY_STANDARD`, `LONG_RUN_STANDARD` v1 and v6, `THRESHOLD_TEMPO`, `FARTLEK` v4/v5, `GOAL_PACE_TEN_K`, `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`PROGRESSED`), `plan-catalog/catalog/layouts/run-layout-4d.v2.json`, `plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v4.json` and `.v6.json`, `plan-catalog/catalog/templates/ten-k-master.v6.json`, `plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`, `plan-catalog/catalog/registries/runtime-condition-values.v2.json`.

### 1.1 Material, disclosed discrepancy: the governing prompt's named source sections do not all exist

The governing prompt names the source boundary as `HM-LIT-06` (Threshold/Tempo), `07` (HM Race-Pace Training), `08` (Faster-than-HM/VO2 Work), `09` (Intensity Distribution/Hard Days), `10` (Progression/Cutback/Peak Placement), `11` (Taper), plus `15/16/17/18/20/21`.

A full, case-sensitive and case-insensitive search of `HM_21K_Claude_Handoff_and_Build_Plan.md` (the entire 1183-line file, confirmed read in full, not sampled) finds **exactly thirteen `HM-LIT-*` sections in the whole document: `HM-LIT-15` through `HM-LIT-27`**. There is no section, heading, or reference anywhere in the file numbered `HM-LIT-01` through `HM-LIT-14`. `§5`'s own heading is explicit about this: *"# 5. HM-LIT-15 → HM-LIT-27 research closure"* — the document's own numbering starts at 15.

This is reported as a genuine source-boundary discrepancy, not silently patched over by inventing what sections 06–11 "should" have said (which would violate this phase's own explicit ban on importing content not already in the approved evidence set). The topics the governing prompt associates with 06–11 (threshold/tempo dose, HM race-pace dose, faster-than-HM/VO2 dose, intensity distribution, progression/peak placement, taper) are **not absent from the document** — they exist, but only as data folded into the sections that do exist:
- Threshold, HM-pace, and faster-than-HM dose envelopes: **`HM-LIT-17`** (Workout Dose & Session Load) — a single unified dose-tier table, not four separate sections.
- Taper week-count and race-week schedule: **§4.1** (already consumed/frozen by `HM.1` Item 1) and **`HM-LIT-20`** (Race Week / Pre-Race Scheduling).
- Intensity-distribution philosophy (`LOW_INTENSITY_DOMINANCE`): **`HM-LIT-15`** (Easy/Recovery).
- Peak placement: named only as an *open sub-question* inside `OPEN-HM-02`, never answered anywhere in the document (already established by `HM.1 §3`).
- No dedicated "cutback week" or "progression/deload" research section exists anywhere in the document at all, under any number.

**Net effect on scope:** this phase's usable evidence base is the six sections that do exist and were named (`HM-LIT-15/16/17/18/20/21`), plus `HM.1`/`HM.1.1`'s frozen authority, plus the real catalog files — exactly as the prompt's source-boundary rule permits ("existing canonical Appsel workout/catalog vocabulary"). No content is fabricated to fill the 06–11 numbering gap.

### 1.2 Frozen inputs treated as already decided (not reopened)

`HM.1`'s `HALF_MARATHON_PHASE_ALLOCATION` (Foundation 3 / Build 4 / RaceSpecific 5 / Taper 2), `HM.1`'s `HALF_MARATHON_QUALITY_LONG_RUN_POLICY`, `HM.1.1`'s `PreferredAbsolutePeakLongRunKm = 19.0km` (ceiling, not target, single-cell scope), `HM.1`'s dose-tier/dose-envelope table (Intermediate → `STANDARD`), and `HM.1`'s phase-relative stage concept — reproduced in §2 below, not re-derived.

### 1.3 Real catalog evidence gathered (used throughout §5–§12)

Three concrete, previously-undocumented-for-HM findings materially shaped this phase's conclusions:

1. **Two independent eligibility gates exist in the real schema**, not one: a `WORKOUT_DEFINITION`-level `eligiblePhases` list (per workout) *and* a `PLAN_TEMPLATE`-level `phases[].eligibleWorkoutFamilies` allow-list (per phase, at the master-template level). `ten-k-master.v6.json` — the live, `VALIDATED`, currently-bound-to-4D-Intermediate master template (confirmed via `ten-k-4d-intermediate.v10.json`'s own reference chain) — sets `FOUNDATION.eligibleWorkoutFamilies = ["EASY","LONG_RUN"]` only, **excluding `QUALITY` entirely**, even though `AEROBIC_STRENGTH_CONTROLLED_INTRO` (a `QUALITY`-family workout) separately declares itself `FOUNDATION`-eligible at the workout-definition level. Both gates must agree; the phase-level gate is the more restrictive, live-production one.
2. **`LONG_RUN_STANDARD` has never, in any of its 6 versions, supported an embedded quality/pace segment inside a single session.** v1 (`VALIDATED`) had a flat `WARM_UP`/`MAIN_SET(STEADY)` structure with `EFFORT_BASED` prescription; v6 (`DRAFT`, current) dropped components entirely — pure `DISTANCE`/`EXACT_SESSION_TOTAL`, no `MIXED` mode, no `MAIN_SET` intensity descriptor at all. **No second `LONG_RUN`-family workout key exists anywhere in the catalog.** This is a real, structural, catalog-wide capability ceiling, not an HM-specific gap.
3. **`GOAL_PACE_TEN_K`'s schema is already 100% distance-generic** (`WARM_UP`/`MAIN_SET(GOAL_PACE)`/`COOL_DOWN`, `PACE_BASED` prescription, `complexityTier 2`) — only its catalog *key* ties it to 10K. The real `TEN_K_WORKOUT_PROGRESSION_V1 v4` document (single-KEY-lane, matching HM's own Intermediate×4D one-`KEY_SESSION` template) shows the exact reusable fallback mechanism this phase needed for HM Pace: `GOAL_PACE_REHEARSAL` requires `{conditionType: GOAL_FEASIBILITY_IN, allowedValues: [REALISTIC, CHALLENGING]}` with `fallbackStageKey: CURRENT_FITNESS_SPECIFIC_REHEARSAL` (which reuses `THRESHOLD_TEMPO`). The real `RUNTIME_CONDITION_VALUES_V1 v2` registry confirms `GOAL_FEASIBILITY_IN`'s actual engine-consumed value set is `[REALISTIC, CHALLENGING, UNSUPPORTED, NOT_REQUESTED]`.

---

## 2. Frozen prior authorities (reproduced, not re-derived)

```
HALF_MARATHON_PHASE_ALLOCATION (HM.1, FROZEN)
  Foundation: 3 weeks | Build: 4 weeks | RaceSpecific: 5 weeks | Taper: 2 weeks | Total: 14

PreferredAbsolutePeakLongRunKm = 19.0 km (HM.1.1, FROZEN)
  Classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT (not EVIDENCE_BACKED)
  Ceiling, not mandatory target. Scope: Intermediate×4D×14W ONLY.

HALF_MARATHON_QUALITY_LONG_RUN_POLICY (HM.1, FROZEN)
  containsQualitySegment=true  when LONG_RUN finishes threshold/HM-pace/hard
  containsQualitySegment=false when LONG_RUN finishes controlled/steady/aerobic
  true => LONG_RUN consumes the weekly quality/hard-session budget
  Spacing vs KEY_SESSION: hard minimum ~48h, preferred >=72h
  Calendar must not compress two hard stimuli together to fit PreferredDays

Dose-tier (Intermediate -> STANDARD), STANDARD envelopes (HM.1, FROZEN):
  Faster/5K-10K work:        12-18 min main-set
  Threshold:                 20-30 min main-set
  HM Pace:                   25-45 min main-set
  HM Pace inside Long Run:   20-35 min main-set

Phase-relative stage concept (HM.1, FROZEN at concept level only):
  Foundation   -> aerobic / strides / basic quality
  Build        -> threshold development
  RaceSpecific -> HM-specific intro -> development -> rehearsal
  Taper        -> reduced-volume HM activation

Adjunct (non-KEY): strides/short hill strides, 4-8 x 15-20sec, full recovery (HM-LIT-16)
KEY-tier hill work: STRUCTURED_HILL_REPETITIONS (HM-LIT-16), optional, not mandatory

RUN_LAYOUT_4D (real catalog, distance-generic, reused verbatim):
  KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN — exactly 1 KEY + 1 LONG/week
```

This phase's job is exactly the disclosed remainder: exact stage identities, workout-family eligibility per phase, ordering, hard-budget interaction, dose binding to real catalog workouts, taper semantics, fallback, and compression/extension semantics — for this one cell.

---

## 3. Proposed stage catalog

Governed entirely by `RUN_LAYOUT_4D`'s single `KEY_SESSION` slot per week — `LONG_RUN` content is governed separately by the already-frozen `HALF_MARATHON_QUALITY_LONG_RUN_POLICY` (§7), mirroring the real 10K precedent: neither `TEN_K_WORKOUT_PROGRESSION_V1 v4` nor `v6` ever references a `LONG_RUN`-family workout — the `WORKOUT_PROGRESSION` document type governs the `KEY_SESSION` lane only.

| StageKey | Phase | Objective | Allowed families (KEY lane) | Disallowed families | Dose tier | Optional/Required |
|---|---|---|---|---|---|---|
| `FOUNDATION_AEROBIC_BASE` | FOUNDATION | Aerobic development, controlled neuromuscular exposure | EASY (adjunct STRIDES permitted, non-KEY) | THRESHOLD, HM_PACE, FASTER_THAN_HM, STRUCTURED_HILL_REPETITIONS (all KEY-tier) | n/a (no quality dose) | **REQUIRED** |
| `BUILD_FASTER_THAN_HM_INTRO` | BUILD | Controlled speed introduction bridging aerobic base into threshold work | FASTER_THAN_HM/5K–10K-style (FARTLEK) | HM_PACE, STRUCTURED_HILL_REPETITIONS | Faster/5K-10K STANDARD 12-18min | **OPTIONAL** (HM.1's envelope explicitly names this "optional faster-than-HM support") |
| `BUILD_THRESHOLD_DEVELOPMENT` | BUILD | Threshold introduction and development | THRESHOLD | HM_PACE, FASTER_THAN_HM (as primary), STRUCTURED_HILL_REPETITIONS | Threshold STANDARD 20-30min | **REQUIRED** |
| `RACE_SPECIFIC_HM_INTRODUCTION` | RACE_SPECIFIC | Bridge threshold fitness into HM-specific intensity | THRESHOLD (bridging) and/or HM_PACE (low-STANDARD end) | FASTER_THAN_HM (catalog-prohibited, §5), STRUCTURED_HILL_REPETITIONS | Threshold STANDARD 20-30min, or HM Pace low-STANDARD ~25min | **REQUIRED** |
| `RACE_SPECIFIC_HM_DEVELOPMENT` | RACE_SPECIFIC | HM-pace specific development, full dose | HM_PACE | FASTER_THAN_HM, STRUCTURED_HILL_REPETITIONS, THRESHOLD (as primary) | HM Pace STANDARD 25-45min (standalone) | **REQUIRED** |
| `RACE_SPECIFIC_HM_REHEARSAL` | RACE_SPECIFIC | Final full-dress HM-pace/effort rehearsal, coincides with peak-long-run week (19.0km ceiling) | HM_PACE | Same as above | HM Pace STANDARD 25-45min | **REQUIRED**, spacing-protected (§6, §7) |
| `TAPER_HM_ACTIVATION` | TAPER | Reduced-volume HM-specific activation, no new stimulus | HM_PACE (reduced dose, reused from RaceSpecific) — see §9 for the open EASY-vs-HM_PACE preference item | THRESHOLD (catalog-excludes TAPER, §5), any family not already exercised earlier | Reduced from HM Pace STANDARD envelope; exact reduction not decided (§14) | **REQUIRED** |

Non-KEY adjuncts (STRIDES, optionally STRUCTURED_HILL_REPETITIONS as a KEY-tier alternative where used) may appear in Foundation/Build/RaceSpecific/Taper per HM-LIT-16 without consuming the single `KEY_SESSION` slot — never mandatory, per the source document's own explicit statement ("Hill work is not mandatory for every HM user").

---

## 4. Evidence/support trace per stage

| Stage | Citation |
|---|---|
| `FOUNDATION_AEROBIC_BASE` | HM.1 phase-relative concept ("Foundation → aerobic/strides/basic quality"); real precedent `ten-k-workout-progression.v4.json` `FOUNDATION_EASY_BASE` (single stage, `EASY_STANDARD`, `COMPRESSIBLE`/`EXTENDABLE`); real precedent `ten-k-master.v6.json` `phases[FOUNDATION].eligibleWorkoutFamilies=["EASY","LONG_RUN"]` (no `QUALITY` at the live master-template gate) |
| `BUILD_FASTER_THAN_HM_INTRO` | HM.1 envelope table ("Build" row lists "optional faster-than-HM support" — reproduced from HM-LIT-17's dose table plus the phase-relative concept); real precedent `ten-k-workout-progression.v4.json` `FARTLEK_INTRO` (1-2 exposures, `COMPRESSIBLE`/`EXTENDABLE`, ordered before threshold) |
| `BUILD_THRESHOLD_DEVELOPMENT` | HM.1 phase-relative concept ("Build → threshold development"); HM-LIT-17 dose table (Threshold STANDARD 20-30min); real precedent `ten-k-workout-progression.v4.json` `THRESHOLD_INTRO` |
| `RACE_SPECIFIC_HM_INTRODUCTION` | `OPEN-HM-03`'s own explicit three-sub-stage concept ("HM-specific introduction → development → rehearsal"); real precedent `ten-k-workout-progression.v4.json` `TEN_K_SPECIFIC_INTRO` (reuses `THRESHOLD_TEMPO` as the bridging workout before goal-pace content) |
| `RACE_SPECIFIC_HM_DEVELOPMENT` | Same `OPEN-HM-03` concept; HM-LIT-17 dose table (HM Pace STANDARD 25-45min); real precedent `GOAL_PACE_REHEARSAL`'s condition+fallback mechanism (§8) |
| `RACE_SPECIFIC_HM_REHEARSAL` | Same `OPEN-HM-03` concept ("rehearsal"); `HM.1.1`'s peak-long-run week placement (19.0km ceiling occurs within RaceSpecific per the phase-relative concept, since Taper is explicitly reduced-volume); HM-LIT-21 spacing rules (§6) |
| `TAPER_HM_ACTIVATION` | HM.1 phase-relative concept ("Taper → reduced-volume HM activation"); governing prompt's supplied Taper principles (no novel stimulus, lower volume, some specificity may remain, short activation); real, divergent 10K precedents for taper KEY content — `ten-k-workout-progression.v4.json` `TAPER_SHARPEN` (`EASY_STANDARD` only, 4D single-KEY) vs. `ten-k-workout-progression.v6.json` `TAPER_PRIMARY_STAGE` (`GOAL_PACE_TEN_K`, 5D dual-KEY) — both real, valid, already-approved (§9) |

---

## 5. Phase × workout-family eligibility matrix

`PRIMARY` = the phase's default/dominant source of that family's content · `SECONDARY` = present but not dominant · `OPTIONAL` = may appear, never required · `PROHIBITED` = must not appear (either by evidence or by real catalog structural constraint, noted per cell).

| Family | FOUNDATION | BUILD | RACE_SPECIFIC | TAPER |
|---|---|---|---|---|
| EASY | PRIMARY | SECONDARY (non-KEY days) | SECONDARY | PRIMARY |
| STRIDES (adjunct, non-KEY) | OPTIONAL | OPTIONAL | OPTIONAL | OPTIONAL (short/pre-race activation, HM-LIT-20-consistent) |
| STRUCTURED_HILL_REPETITIONS (KEY-tier) | OPTIONAL | OPTIONAL | **PROHIBITED** (no evidence for late-introduced hill-KEY content; single KEY slot already committed to HM-specific work) | **PROHIBITED** (novel-stimulus risk; not evidenced as taper-appropriate) |
| FARTLEK (faster-than-HM/5K-10K proxy) | **PROHIBITED** (catalog: `FARTLEK.eligiblePhases` never includes `FOUNDATION` in any version) | OPTIONAL | **PROHIBITED** (catalog: `FARTLEK.eligiblePhases` never includes `RACE_SPECIFIC` in any version) | OPTIONAL, reuse-only (catalog-eligible, but fresh introduction here would violate "no novel stimulus" unless already exercised in Build) |
| THRESHOLD | **PROHIBITED** (live `ten-k-master.v6.json` phase-gate excludes `QUALITY` from `FOUNDATION` entirely, overriding `THRESHOLD_TEMPO`'s own more permissive workout-level `eligiblePhases`) | PRIMARY | SECONDARY (bridging stage 1 + fallback for stages 2/3) | **PROHIBITED** (catalog: `THRESHOLD_TEMPO.eligiblePhases` excludes `TAPER` in every version read) |
| HM_PACE (new HM-parameterized `GOAL_PACE`-shaped workout) | **PROHIBITED** | **PROHIBITED** | PRIMARY | OPTIONAL (reduced dose, reuse-only — §9) |
| LONG_RUN_EASY (`LONG_RUN_STANDARD`) | PRIMARY | PRIMARY | SECONDARY (default most weeks) | PRIMARY |
| LONG_RUN_PROGRESSIVE | **PROHIBITED, all phases — catalog-structural** (§12; no version of the only `LONG_RUN`-family workout has ever supported an internal pace-build segment) | | | |
| LONG_RUN_WITH_HM_PACE | **PROHIBITED for the first slice, all phases — catalog-structural** (§12); RaceSpecific is the only phase HM.1's dose table would conceptually authorize this in, once/if the catalog gap closes | | | |

---

## 6. Stage ordering constraints

- **Intra-phase ordering is strict** via `relativeOrder`, directly reusing the real `WORKOUT_PROGRESSION` mechanism (`ten-k-workout-progression.v4.json`'s `FARTLEK_INTRO`(1)→`THRESHOLD_INTRO`(2), `TEN_K_SPECIFIC_INTRO`(1)→`GOAL_PACE_REHEARSAL`(2)→`CURRENT_FITNESS_SPECIFIC_REHEARSAL`(3)): `BUILD_FASTER_THAN_HM_INTRO` before `BUILD_THRESHOLD_DEVELOPMENT`; `RACE_SPECIFIC_HM_INTRODUCTION` before `RACE_SPECIFIC_HM_DEVELOPMENT` before `RACE_SPECIFIC_HM_REHEARSAL`.
- **Can HM-specific work precede threshold development? NO.** `OPEN-HM-03`'s own concept states Build (threshold) precedes RaceSpecific (HM-specific); real precedent confirms threshold-tier content is always sequenced before goal-pace/HM-pace-tier content, never after.
- **Can faster-than-HM work continue into RaceSpecific? NO — catalog-structural, not merely a policy preference.** `FARTLEK.eligiblePhases` excludes `RACE_SPECIFIC` in every version of the workout definition read. This independently confirms the phase-relative concept's own boundary (Build owns "optional faster-than-HM support"; RaceSpecific does not).
- **Must HM-specific introduction precede rehearsal? YES.** Direct `relativeOrder` requirement, matching `OPEN-HM-03`'s literal three-step concept text ("introduction → development → rehearsal") and the real `TEN_K_SPECIFIC_INTRO`→`GOAL_PACE_REHEARSAL` precedent.
- **Can Taper introduce a new stimulus? NO.** Governing-prompt principle ("no novel stimulus") plus real precedent: in both 10K taper variants read (`TAPER_SHARPEN`/`EASY_STANDARD` for 4D; `TAPER_PRIMARY_STAGE`/`GOAL_PACE_TEN_K` + `TAPER_SECONDARY_STAGE`/`FARTLEK` for 5D), the taper-phase workout family was **already exercised in an earlier phase** of the same plan — taper never introduces a family absent from Build/RaceSpecific. `TAPER_HM_ACTIVATION`'s `HM_PACE` choice (§9) is only valid because `HM_PACE` was already exercised in RaceSpecific.

---

## 7. Quality-long-run / hard-budget matrix

Directly formalizes `HM.1`'s already-frozen `HALF_MARATHON_QUALITY_LONG_RUN_POLICY` against this stage catalog's concrete `KEY_SESSION` content — no new numeric authority invented, only the interaction made explicit:

| Phase | LONG_RUN default state | Quality-LONG eligible? | KEY + quality-LONG coexistence | Prohibited combinations |
|---|---|---|---|---|
| FOUNDATION | Always EASY (`containsQualitySegment=false`) | No — no dose envelope authorizes quality-in-long this early (no "Threshold/Faster-inside-Long" row exists anywhere in HM-LIT-17's table) | N/A | Quality-LONG in Foundation |
| BUILD | Always EASY | No — same reasoning; only "HM Pace inside Long Run" exists as a distinct dose-envelope row, and HM Pace is a RaceSpecific-only concept per the phase-relative model | N/A | Quality-LONG in Build |
| RACE_SPECIFIC | EASY by default (§14 item 3) | Conceptually yes (HM.1's own dose table names "HM Pace inside Long Run" here) — **not exercised in the first slice** (§12, §14) | If ever exercised: allowed, per HM.1 §5.2 item 4 — `HARD+HARD` adjacency (KEY + quality LONG) is "not prohibited outright," bound by the ≥48h/preferred-≥72h spacing floor; calendar must not compress to fit `PreferredDays` | Quality-LONG scheduled <48h from that week's `KEY_SESSION` |
| TAPER | Always EASY, reduced volume | No — "reduced-volume activation" principle, no evidence supports quality-in-long here | N/A | Quality-LONG in Taper |

Structural note: since `RUN_LAYOUT_4D` has exactly one `KEY_SESSION` and one `LONG_RUN` per week, "two quality KEY workouts in the same week" is already structurally impossible (only one `KEY_SESSION` slot exists) — the only real prohibited-combination surface is the KEY-vs-quality-LONG spacing floor above, exactly as `HM.1 §5.2` item 5 already anticipated for this frequency.

---

## 8. STANDARD dose binding

No whole-session multiplier selected, per the prompt's explicit instruction — each quality stage maps to `workoutFamily + STANDARD dose envelope` exactly as `HM.1` froze it; where evidence supports only a range, the range is kept, not narrowed to an invented exact figure:

| Stage | Workout family | STANDARD dose envelope (main-set only) |
|---|---|---|
| `BUILD_FASTER_THAN_HM_INTRO` | Faster/5K-10K work | 12–18 min |
| `BUILD_THRESHOLD_DEVELOPMENT` | Threshold | 20–30 min |
| `RACE_SPECIFIC_HM_INTRODUCTION` | Threshold (bridging) or low-end HM Pace | 20–30 min (Threshold) or ~25 min (low-STANDARD HM Pace) |
| `RACE_SPECIFIC_HM_DEVELOPMENT` | HM Pace | 25–45 min |
| `RACE_SPECIFIC_HM_REHEARSAL` | HM Pace | 25–45 min |
| `TAPER_HM_ACTIVATION` | HM Pace, reduced | Reduced from 25–45 min STANDARD; exact reduced figure not decided (§14, non-blocking) |

Warm-up/cool-down (per HM-LIT-18, already frozen conceptually by reference) count toward total planned volume, not this main-set dose — consistent with `THRESHOLD_TEMPO`/`GOAL_PACE_TEN_K`'s own real `WARM_UP`/`MAIN_SET`/`COOL_DOWN` component structure. No exact interval count, repetition scheme, or split (e.g. "3×8min" or continuous) is asserted anywhere — the evidence supports only the envelope, not the internal structure, matching this phase's explicit constraint.

---

## 9. Taper-stage semantics

`TAPER_HM_ACTIVATION` (2 weeks, per HM.1's frozen allocation):

- **No novel stimulus**: satisfied by construction — `HM_PACE` was already exercised across all three RaceSpecific stages; nothing new is introduced.
- **Lower volume**: satisfied via reduced dose from the STANDARD HM Pace envelope (exact reduction magnitude not decided, §14 — non-blocking since "reduced" is directionally clear and the family/envelope ceiling is already bound).
- **Some intensity/specificity may remain**: satisfied by retaining `HM_PACE` (not degrading to pure `EASY`) as the KEY-lane content, per the governing prompt's supplied Taper principle.
- **Race-week activation should be short**: `HM-LIT-20`'s candidate race-relative schedule (D-14..D-10 last peak long run, D-7..D-5 last substantial KEY window, D-4..D-3 short activation/sharpening, D-2 easy/rest, D-1 rest-or-shakeout, D race) is directionally consistent with a short, front-loaded Taper KEY exposure — the exact day-by-day placement is calendar-materialization detail, out of this Core-horizon-authority phase's scope (unchanged from `HM.1`'s own boundary).
- **Real, disclosed open preference** (non-blocking, §14 item 2): 10K itself has two different, both-approved taper precedents for different frequency cells — 4D's own `TAPER_SHARPEN` uses plain `EASY_STANDARD` (no specificity retained at all), while 5D's dual-KEY taper retains `GOAL_PACE_TEN_K`. HM's Intermediate×4D could legitimately follow either precedent; this phase recommends the `HM_PACE`-retained shape (closer fit to "specificity may remain") but does not treat this as fully closed, frozen authority — it is a reversible product preference, not a research gap.
- **Catalog constraint respected**: `THRESHOLD_TEMPO.eligiblePhases` excludes `TAPER` in the real catalog (every version read) — Taper's KEY lane cannot use `THRESHOLD` regardless of preference; this is a hard structural fact, not a policy choice.

---

## 10. Pace-evidence fallbacks

| Family requiring pace evidence | Fallback | Mechanism (reused, not invented) |
|---|---|---|
| `HM_PACE` (both `RACE_SPECIFIC_HM_DEVELOPMENT` and `RACE_SPECIFIC_HM_REHEARSAL`) | Fallback to `THRESHOLD` (effort/fitness-anchored, not goal-pace-anchored) | Directly reuses the real, generic `requires`/`fallbackStageKey` mechanism already proven in `ten-k-workout-progression.v4.json`: `GOAL_PACE_REHEARSAL` requires `{GOAL_FEASIBILITY_IN: [REALISTIC, CHALLENGING]}` else falls back to `CURRENT_FITNESS_SPECIFIC_REHEARSAL` (`THRESHOLD_TEMPO`). HM's `HM_PACE` stages adopt the identical condition/fallback shape, using the real registry's actual value set (`RUNTIME_CONDITION_VALUES_V1 v2`: `REALISTIC, CHALLENGING, UNSUPPORTED, NOT_REQUESTED`) — `UNSUPPORTED`/`NOT_REQUESTED` route to the `THRESHOLD` fallback. No exact race pace is ever fabricated from a desired finish time (HM.1's own §4.5-reused invariant: "goal pace != starting training pace"). |
| `RACE_SPECIFIC_HM_INTRODUCTION`'s low-end HM Pace option | Fallback to plain `THRESHOLD` (its co-listed alternative) | The stage already lists `THRESHOLD` as a co-equal option, not a separate fallback branch — no additional mechanism needed. |
| `BUILD_FASTER_THAN_HM_INTRO` | None needed — optional, non-gated | `FARTLEK`'s `SURGE_AND_FLOAT` intensity descriptor is effort/feel-based, not pace-evidence-dependent; omission (never scheduled) is itself a valid, already-anticipated outcome ("optional"). |
| `BUILD_THRESHOLD_DEVELOPMENT` | None needed | Threshold effort derives from current fitness, not goal pace (HM-LIT-26's already-generic reused principle: "training pace derives from current fitness, not desired goal fitness") — no goal-feasibility gate applies to threshold content. |
| `LONG_RUN_WITH_HM_PACE` / `LONG_RUN_PROGRESSIVE` | Fallback to `LONG_RUN_EASY` (`LONG_RUN_STANDARD`) | Not a pace-evidence fallback but a **catalog-capability** fallback (§12): no version of the only `LONG_RUN`-family workout definition can express an embedded pace segment, so the first slice never attempts to schedule these families at all — `containsQualitySegment` resolves to `false` for every week by construction, trivially satisfying `HM.1`'s policy. |

---

## 11. Compression/extension properties

The real catalog exposes **two independent layers** of compression/extension authority (§1.3 finding 1) — both are reused, neither invented:

**Phase-level** (`PLAN_TEMPLATE.phases[]`, real fields `compressionPriority`/`extensionPriority`/`isCompressionProtected`, from `ten-k-master.v6.json`): Taper is the only phase with `isCompressionProtected: true` in the live 10K template; Foundation/Build/RaceSpecific are not. Applied to HM's own four phases with the same semantics:

| Phase | isCompressionProtected | Relative compression/extension priority |
|---|---|---|
| Foundation | false | Lowest priority to protect — most compressible/extendable of the four |
| Build | false | Compressible/extendable |
| RaceSpecific | false | Compressible/extendable, but `RACE_SPECIFIC_HM_REHEARSAL` (§3) is PROTECTED-leaning at the *stage* level regardless of the phase-level flag, since it anchors the peak-long-run week |
| Taper | **true** | Protected — matches the governing prompt's own frozen Taper principles (no novel stimulus, must not be compressed away) |

**Stage-level** (`WORKOUT_PROGRESSION.stages[].compressionBehavior`/`extensionBehavior`, real values `COMPRESSIBLE`/`PROTECTED` and `EXTENDABLE`/`FIXED_EXPOSURE`, from `ten-k-workout-progression.v4.json`), mapped onto the prompt's requested four-way vocabulary:

| StageKey | `MANDATORY_EXPOSURE`? | Real-vocabulary compression | Real-vocabulary extension | `REPEATABLE_FOR_EXTENSION` / `NON_REPEATABLE` |
|---|---|---|---|---|
| `FOUNDATION_AEROBIC_BASE` | Yes | `COMPRESSIBLE` | `EXTENDABLE` | `REPEATABLE_FOR_EXTENSION` (mirrors `FOUNDATION_EASY_BASE`'s own precedent) |
| `BUILD_FASTER_THAN_HM_INTRO` | No (optional) | `COMPRESSIBLE` | `EXTENDABLE` | `REPEATABLE_FOR_EXTENSION` |
| `BUILD_THRESHOLD_DEVELOPMENT` | Yes | `COMPRESSIBLE` | `EXTENDABLE` | `REPEATABLE_FOR_EXTENSION` |
| `RACE_SPECIFIC_HM_INTRODUCTION` | Yes | `COMPRESSIBLE` | `EXTENDABLE` | `REPEATABLE_FOR_EXTENSION` |
| `RACE_SPECIFIC_HM_DEVELOPMENT` | Yes | `COMPRESSIBLE` (with a floor — must leave room for rehearsal) | `EXTENDABLE` | `REPEATABLE_FOR_EXTENSION` |
| `RACE_SPECIFIC_HM_REHEARSAL` | Yes | `PROTECTED`-leaning (anchors peak long run + spacing) | `FIXED_EXPOSURE`-leaning | `NON_REPEATABLE` within a single 14W instance (one rehearsal window, not a repeatable lane) |
| `TAPER_HM_ACTIVATION` | Yes | `PROTECTED` | `FIXED_EXPOSURE` | `NON_REPEATABLE` |

This classification is deliberately **not** frozen as a 10–16-week dynamic-core allocation table (`OPEN-HM-01`'s remaining rows, still explicitly out of scope) — it supplies only the qualitative semantics a future `WAVE HM-D` allocator will need, per the governing prompt's own instruction. No exact `MinimumExposures`/`MaximumExposures` integer is asserted anywhere in this section (§14 item 1 discloses why that remains open).

---

## 12. Existing catalog capability matrix

| Required HM behavior | Classification | Basis |
|---|---|---|
| EASY (Foundation/Build/Taper base) | `EXISTING_WORKOUT_REUSE` | `EASY_STANDARD` — distance-neutral, all phases, `DISTANCE` mode |
| STRIDES (adjunct) | `UNSUPPORTED_BY_CURRENT_EVIDENCE` (catalog) — non-blocking | No `STRIDES`/`SHORT_HILL_STRIDES` workout definition exists anywhere in the catalog. Explicitly optional per HM-LIT-16, so absence does not block the first slice. |
| STRUCTURED_HILL_REPETITIONS | `UNSUPPORTED_BY_CURRENT_EVIDENCE` (catalog) — non-blocking | No hill-repetition workout definition exists anywhere in the catalog. Explicitly optional/not mandatory per HM-LIT-16, and classified `PROHIBITED` for RaceSpecific/Taper in this cell's proposal (§5) regardless, so absence does not block the first slice. |
| FASTER_THAN_HM / 5K-10K-style work (Build, optional) | `EXISTING_WORKOUT_REUSE` | `FARTLEK` — distance-neutral key, `SURGE_AND_FLOAT` main set already semantically matches "faster than target-race effort, controlled speed play," and is the exact real analog 10K itself uses for this identical Build-intro role (`FARTLEK_INTRO`). |
| THRESHOLD (Build, RaceSpecific-bridging) | `EXISTING_WORKOUT_REUSE` | `THRESHOLD_TEMPO` — distance-neutral key, `THRESHOLD` main-set descriptor, semantically exact match, real precedent for identical role in both 10K progressions read. |
| HM_PACE (RaceSpecific, Taper) | `EXISTING_WORKOUT_REQUIRES_HM_PARAMETERIZATION` | `GOAL_PACE_TEN_K`'s **schema** (`WARM_UP`/`MAIN_SET(GOAL_PACE)`/`COOL_DOWN`, `PACE_BASED`, `complexityTier 2`) is already 100% distance-generic — only the catalog *document identity* (`key: GOAL_PACE_TEN_K`) is 10K-bound. A new HM-specific catalog document reusing the identical schema/logic (not new behavior) is required — this is authoring work correctly deferred to `HM.2`, not a decision this phase can or should make (catalog authoring is barred here). Semantic compatibility, not merely structural, is confirmed: the `GOAL_PACE` intensity descriptor is already a generic concept, not a 10K-specific one. |
| LONG_RUN_EASY | `EXISTING_WORKOUT_REUSE` | `LONG_RUN_STANDARD` — distance-neutral, all phases, `DISTANCE` mode. |
| LONG_RUN_PROGRESSIVE | `UNSUPPORTED_BY_CURRENT_EVIDENCE` (catalog-structural) | No version of `LONG_RUN_STANDARD` (v1 through v6, the only `LONG_RUN`-family key in the entire catalog) has ever supported an internal pace-build/segment structure. v1 had a flat `STEADY`-only main set; v6 has none. Non-blocking for the first slice (§14 item 3 — resolved as not required). |
| LONG_RUN_WITH_HM_PACE | `UNSUPPORTED_BY_CURRENT_EVIDENCE` (catalog-structural), `NEW_HM_WORKOUT_DEFINITION_REQUIRED` if/when built | Same root cause as above — no `LONG_RUN`-family workout has ever carried a `MIXED`-mode embedded quality segment. Non-blocking for the first slice (§7, §14 item 3 — the first slice never exercises `containsQualitySegment=true`). |

No 10K workout is reused merely because its schema is structurally capable of representing HM content — each `EXISTING_WORKOUT_REUSE` classification above is grounded in the workout's own distance-neutral key/descriptor semantics (`EASY_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO`, `LONG_RUN_STANDARD` carry no distance-specific naming or behavior anywhere in their definitions), not merely "the JSON shape fits."

---

## 13. Conceptual 14-week stage reachability proof

This is a **structural/cardinality feasibility argument**, not a concrete week-by-week schedule — no exact distances, paces, interval repetitions, or calendar dates are assigned, and no exposure-count integer is invented, per the prompt's own explicit instruction.

- **Foundation (3 weeks)**: 1 mandatory stage (`FOUNDATION_AEROBIC_BASE`) occupies the `KEY_SESSION` lane every week — cardinality 1 stage ≤ 3 weeks, trivially satisfied, identical in shape to the real `FOUNDATION_EASY_BASE` precedent (a single stage spanning an entire phase). No two incompatible KEY workouts compete for the same week since only one stage exists. Optional STRIDES adjunct never touches the KEY slot.
- **Build (4 weeks)**: 2 stages, 1 optional + 1 required, strictly ordered — cardinality 2 stages ≤ 4 weeks. Even in the degenerate case where `BUILD_FASTER_THAN_HM_INTRO` occupies exactly 1 week and `BUILD_THRESHOLD_DEVELOPMENT` occupies the remaining 3, or the optional stage is skipped entirely and `BUILD_THRESHOLD_DEVELOPMENT` spans all 4, both are structurally valid under this cell's own `COMPRESSIBLE`/`EXTENDABLE` classification (§11) — no contradiction, no forced invention of an exact split.
- **RaceSpecific (5 weeks)**: 3 mandatory, strictly-ordered stages — cardinality 3 stages ≤ 5 weeks, with 2 weeks of slack distributable across the introduction/development stages (both `COMPRESSIBLE`/`EXTENDABLE`) while `RACE_SPECIFIC_HM_REHEARSAL` (`NON_REPEATABLE`) occupies exactly the week(s) needed to align with the 19.0km peak-long-run ceiling window — no numeric commitment is made here beyond "at least 1 week each, rehearsal last." The single `KEY_SESSION`/week structural limit (`RUN_LAYOUT_4D`) guarantees no two quality KEY stages are ever forced into the same week. The optional `RACE_SPECIFIC` quality-long-run family (§7) is never exercised in this proof (§14 item 3), so it introduces no additional weekly-hard-budget pressure beyond the existing `KEY_SESSION` + `LONG_RUN`(easy) pair.
- **Taper (2 weeks)**: 1 mandatory, `NON_REPEATABLE`/`PROTECTED` stage (`TAPER_HM_ACTIVATION`) — cardinality 1 stage ≤ 2 weeks, reduced dose, no new family (§6), no capacity conflict.
- **No duplicate unsupported progression**: every stage traces to a distinct citation (§4); none repeats another stage's evidence basis under a different name.
- **No stage introduced and immediately discarded**: every stage occupies at least the minimum viable footprint of its phase (Foundation's single stage spans the whole phase; Build's two stages are sequential, not overlapping-and-abandoned; RaceSpecific's three sub-stages match the source document's own explicit three-step concept one-for-one; Taper's single stage spans the whole phase).
- **No weekly hard-budget excess**: at most one `KEY_SESSION`-tier stimulus per week is ever assigned (structural limit of `RUN_LAYOUT_4D`), and the only place a second hard stimulus (quality LONG) could coexist is gated by the already-frozen ≥48h/≥72h spacing rule (§7), never exercised in this first-slice proof at all.
- **No incompatible quality-workout pairing**: `FARTLEK` (Build-only) and `HM_PACE`/`THRESHOLD` (RaceSpecific) never share a phase in this proposal (§5, §6) — the only phase where `THRESHOLD` and `HM_PACE` coexist as options is `RACE_SPECIFIC_HM_INTRODUCTION`, where they are alternatives for the *same* stage, not simultaneous.
- **No workout the current catalog cannot express**: every stage's *required* content (§3, marked `REQUIRED`) maps to an `EXISTING_WORKOUT_REUSE` or `EXISTING_WORKOUT_REQUIRES_HM_PARAMETERIZATION` classification (§12) — the two `UNSUPPORTED_BY_CURRENT_EVIDENCE` catalog gaps (`LONG_RUN_PROGRESSIVE`, `LONG_RUN_WITH_HM_PACE`) are both classified `OPTIONAL`/never-exercised in this proof, so the reachability proof itself requires zero catalog content that does not already exist or is not already a clean, same-schema parameterization task correctly deferred to `HM.2`.
- **No forced quality-dose growth merely to fill weeks**: the slack described above (Build's 4 weeks vs. 2 stages, RaceSpecific's 5 weeks vs. 3 stages) is explicitly left as `COMPRESSIBLE`/`EXTENDABLE` distribution room for a future allocator, not pre-filled with invented extra exposures.

**Conclusion: the 14-week preferred core can structurally contain this stage catalog with zero contradiction**, using only real, evidence-traced stages and real-or-cleanly-classified catalog content.

---

## 14. Remaining unsupported questions

Per the honesty rule: a gap blocks the first slice only if deterministic generation is impossible without inventing behavior.

**Item 1 — exposure counts (BLOCKING):**
- `QUESTION`: What are the exact `MinimumExposures`/`MaximumExposures` (or, since this phase is scoped to the fixed 14-week preferred core rather than the general 10–16-week dynamic range, a simpler fixed per-stage week-count) for each `KEY_SESSION`-lane stage within Foundation (3wk) / Build (4wk) / RaceSpecific (5wk) / Taper (2wk) for this exact cell?
- `WHY_EXISTING_EVIDENCE_IS_INSUFFICIENT`: Neither the handoff document (searched in full, including confirming the nonexistent `HM-LIT-06`–`11` range, §1.1) nor `HM.1`/`HM.1.1` supply any exposure-count number for this cell. `HM.1 §4.2` already explicitly disclosed this exact absence and declined to invent it; this phase's own full re-read confirms no new evidence has since appeared anywhere in the permitted source boundary.
- `WHETHER_THE_FIRST_SLICE_REQUIRES_IT`: **YES.** Real workout binding — assigning an actual catalog workout key to each of the 14 weeks' `KEY_SESSION` slot — cannot be made fully deterministic without some concrete per-stage exposure count or week-count rule. Without it, whoever authors the real catalog `WORKOUT_PROGRESSION` document would have to invent the count themselves, which this phase's own hard constraint (and the engagement's standing discipline) forbids doing unilaterally.
- `SMALLEST_FOLLOW-UP_DECISION_NEEDED`: A single, narrowly-scoped numeric product decision — mirroring `HM.1.1`'s own direct-decision pattern, not new research — fixing, for `Intermediate×4D×14W` only, either (a) exact `MinimumExposures`/`MaximumExposures` per stage, or (b) a simpler fixed week-count-per-stage table for this one horizon (deferring the general min/max-range mechanism the dynamic 10–16-week core will eventually need, per `WAVE HM-D`, to a later phase). This is plausibly resolvable by direct user/product decision without reopening research.

**Item 2 — Taper KEY-lane family choice (non-blocking preference):**
- `QUESTION`: Should `TAPER_HM_ACTIVATION` retain `HM_PACE` (mirroring 10K's 5D-taper `GOAL_PACE_TEN_K` precedent) or revert to plain `EASY` (mirroring 10K's 4D-taper `TAPER_SHARPEN`/`EASY_STANDARD` precedent)?
- `WHY_EXISTING_EVIDENCE_IS_INSUFFICIENT`: Both are real, already-approved 10K precedents for different frequency cells within the same distance; the governing prompt's own Taper principle ("some intensity/specificity may remain") permits either without dictating one.
- `WHETHER_THE_FIRST_SLICE_REQUIRES_IT`: Only to the extent of picking one for determinism — but **both choices are already catalog-representable today** (no capability gap either way), so this is a reversible preference, not a blocking gap. This report recommends `HM_PACE`-retained (closer fit to "specificity may remain") without treating it as frozen.
- `SMALLEST_FOLLOW-UP_DECISION_NEEDED`: A one-line product-preference confirmation; can be folded into Item 1's own decision round rather than requiring a separate phase.

**Items resolved within this phase's own evidence-respecting discretion (not left open):**
- Whether quality-content-inside-`LONG_RUN` is required for a "full" first slice: **resolved NO** — `HM.1`'s frozen policy states a resolution rule for long runs that already contain quality content, it never mandates that any week's long run must contain it, and the catalog cannot express it today regardless (§12). The first slice places all RaceSpecific specificity in the `KEY_SESSION` lane and keeps `LONG_RUN` on `LONG_RUN_STANDARD`/easy throughout, satisfying `HM.1`'s policy trivially. This closes what would otherwise have been a blocking gap without inventing any pace, distance, or segment structure.
- The exact HM-specific catalog document needed for `HM_PACE`: **resolved as a classification** (`EXISTING_WORKOUT_REQUIRES_HM_PARAMETERIZATION`, §12) — not a remaining decision, only remaining authoring work, correctly scoped to `HM.2` per `HM.1.1`'s own gating condition.

---

## 15. Exact gating conclusion for the future implementation phase

- **A master/core/numeric-only `HM.2` scope** (no workout-catalog binding) may proceed directly, unaffected by anything in this phase — unchanged from `HM.1.1`'s own gating condition.
- **A workout-materializing `HM.2` scope** (binding real workout content to the plan) is now **substantially closer to unblocked** than it was after `HM.1`: stage identities, ordering, phase×family eligibility, hard-budget interaction, dose-envelope binding, taper semantics, pace-evidence fallback, compression/extension semantics, and the catalog-capability audit are all now closed, evidence-traced authority (§3–§13). **It remains blocked on exactly one small, precisely-scoped numeric item**: per-stage exposure counts / week-counts for this one 14-week cell (§14 item 1). This is a direct-decision-shaped gap (mirroring `HM.1.1`'s peak-long-run resolution), not a research gap — no book, literature, or web source needs to be consulted to close it.
- Catalog authoring itself (the actual `WORKOUT_PROGRESSION`/`PLAN_TEMPLATE`/new HM-specific `HM_PACE` workout-definition JSON documents) remains barred from this phase and from `HM.2` until Item 1 closes, per this engagement's standing discipline of decision-before-authoring.

---

## What this phase explicitly did NOT do

- Did not perform any new external research, literature search, or web fetch — used only `HM_21K_Claude_Handoff_and_Build_Plan.md` (already-approved), `HM.1`/`HM.1.1`'s frozen text, and real repository catalog files.
- Did not invent any exact interval count, recovery duration, pace zone, stage exposure minimum/maximum, or taper dose-reduction figure.
- Did not author, modify, or publish any catalog artifact (`WORKOUT_PROGRESSION`, `PLAN_TEMPLATE`, or workout-definition JSON).
- Did not resolve `OPEN-HM-01`'s remaining 10/11/12/13/15/16-week rows, `OPEN-HM-05` (2D), `OPEN-HM-06` (6D), or `OPEN-HM-07` (>16wk) — all remain correctly out of scope.
- Did not touch any 10K-reachable code, catalog content, or public routing surface.
- Did not force a `CLOSED` classification to unblock a waiting future phase — the one real remaining gap (§14 item 1) is disclosed precisely, not silently patched over.

---

## Final classification

```
HM_WORKOUT_PROGRESSION_STAGE_CATALOG_BLOCKED
```

**Smallest unresolved decision**: exact `MinimumExposures`/`MaximumExposures` (or fixed week-count) per `KEY_SESSION`-lane stage, for `HALF_MARATHON × INTERMEDIATE × 4D × PREFERRED-14W` only — §14 item 1. Everything else the governing prompt's 15 required sections asked for (stage inventory, evidence trace, phase×family eligibility, ordering, hard-budget interaction, STANDARD dose binding, taper semantics, pace-evidence fallback, compression/extension semantics, catalog-capability audit, and the 14-week structural reachability proof) is genuinely closed, evidence-traced authority from the permitted source boundary — a materially smaller, more precisely-scoped remainder than `HM.1` left `OPEN-HM-03` in. No production code, catalog artifact, or public routing change in this phase. No 10K authority, code, or catalog touched.
