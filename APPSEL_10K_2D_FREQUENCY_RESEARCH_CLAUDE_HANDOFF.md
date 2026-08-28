# Appsel 10K V1 — 2D Frequency Capability Research & Claude Handoff

**Purpose:** Evidence-backed planning handoff for the future Appsel 10K 2-days/week capability.

**Audience:** Claude / repository implementation agent.

**Important governance note:** This document is a planning and evidence handoff, **not repository phase authority**. `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, committed phase reports, and the live repository remain canonical. Any future phase must read repository truth first.

**Prepared after:** the product decision to support 2D for **Beginner and Intermediate**, while the previously-started Advanced 3D–6D combined implementation/dark-verification work is still running.

---

# 1. Executive summary

Appsel has now made a new product-direction decision:

- `Beginner × 2D` must become a real supported 10K capability.
- `Intermediate × 2D` must become a real supported 10K capability.
- `Advanced × 2D` remains out of V1 scope.
- 2D must **not** be implemented as a degraded 3D plan or silent fallback.
- The user may be warned that 3D is recommended, but the product must allow the user to continue with 2D.
- The chosen 2D structural model is the user's explicit **Model B** decision:

```text
RUN_LAYOUT_2D

Pattern A
KEY_SESSION
LONG_RUN

Pattern B
EASY_SUPPORT
LONG_RUN

repeat A / B / A / B ...
```

This is a **frequency-owned multi-week structural pattern**. Beginner and Intermediate must use the same 2D structural pattern; their Level policies differ in eligibility, workout prescription, pace/dose, volume calibration, and other Level-owned/cross-axis authorities.

The web review strongly supports that two-run-per-week 10K preparation is a legitimate product capability. Exact 2D plans exist for beginner and intermediate runners, including 8-, 12-, 14-, 16- and 20-week offerings. Peer-reviewed training-frequency research also supports the proposition that two weekly endurance/HIIT sessions can generate meaningful aerobic adaptations, although this does **not** prove equivalence to 3+ running days for 10K race performance.

The selected A/B alternation itself is **an Appsel product choice, not a scientific consensus**. Several published 2D plans use a quality-oriented run plus a longer run every week. Appsel is intentionally choosing lower quality-session density by alternating a KEY week with an EASY week.

---

# 2. Where the project is now

## 2.1 Current 10K state before the new 2D work

### Intermediate

```text
3D  PUBLIC
4D  PUBLIC
5D  PUBLIC
6D  PUBLIC
7D  PRODUCT_NON_SUPPORT
```

Historical classification:

```text
INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE
```

That classification was correct for the old scope (3D–7D). Adding 2D is a **new product-scope expansion**. The old ledger closure remains historical truth; the current Intermediate frequency axis reopens only for the new 2D cell until 2D is implemented and public.

### Beginner

Current repository authority before the new product realignment:

```text
3D  historical non-representability under the then-approved V1 Core policy
4D  PUBLIC
5D  PRODUCT_NON_SUPPORT under GEN.6 policy
6D  PRODUCT_NON_SUPPORT
7D  PRODUCT_NON_SUPPORT
```

New product target eventually becomes:

```text
Beginner
2D  SUPPORT REQUIRED
3D  SUPPORT REQUIRED
4D  PUBLIC / preserve
5D  SUPPORT REQUIRED
6D  PRODUCT_NON_SUPPORT
7D  PRODUCT_NON_SUPPORT
```

The 3D and 5D realignment is a **later workstream**. Do not mix it into the first 2D authority phase.

### Advanced

GEN.7 and GEN.8 closed Advanced product/numeric/prescription authority for 3D–6D, with 7D product non-support. The Advanced combined implementation + dark-verification wave was already running when this 2D planning was performed.

**Do not interrupt or rewrite that phase.**

---

# 3. Final target 10K V1 frequency matrix

```text
                    2D        3D        4D        5D        6D        7D

Beginner            PUBLIC*   PUBLIC*   PUBLIC    PUBLIC*   NON-SUP   NON-SUP
Intermediate        PUBLIC*   PUBLIC    PUBLIC    PUBLIC    PUBLIC    NON-SUP
Advanced            OUT V1    PUBLIC†   PUBLIC†   PUBLIC†   PUBLIC†   NON-SUP
```

`*` = new work still required.

`†` = Advanced authority already closed; implementation/public status depends on the currently-running and subsequent Advanced phases.

---

# 4. User-frozen 2D product decisions

These are no longer candidates unless the user explicitly reopens them.

## 4.1 Supported Levels

```text
Beginner × 2D      SUPPORT REQUIRED
Intermediate × 2D  SUPPORT REQUIRED
Advanced × 2D      OUT OF V1 SCOPE
```

## 4.2 No silent fallback

Forbidden:

```text
2D request -> generate 3D
2D request -> add a third day
2D request -> delete one session from a 3D plan
nearest-frequency matching
```

2D must have a genuine canonical frequency authority.

## 4.3 Model B structural pattern

```text
Pattern A:
KEY_SESSION + LONG_RUN

Pattern B:
EASY_SUPPORT + LONG_RUN
```

The pattern repeats continuously.

This is the chosen product model even though many published 2D plans use a quality-oriented session every week. The evidence supports **2D feasibility**; the A/B alternation is an explicit Appsel product calibration.

## 4.4 3D recommendation is UX, not backend eligibility

Appsel may recommend 3D before the user confirms 2D.

Desired user-facing concept:

> Two running days per week can be used to prepare for a 10K. With fewer running days there are fewer opportunities to distribute training load, and depending on current fitness and race date a longer preparation window may be recommended. If possible, Appsel recommends 3 days; you may still continue with 2 days.

Backend behavior:

```text
user chooses 2D
-> real 2D plan
```

Never:

```text
user chooses 2D
-> silently generate 3D
```

---

# 5. External evidence review

## 5.1 Strong evidence that 2D is a legitimate training frequency

### E1 — Alex Harrison, RP 10K Beginner 2 d/wk, TrainingPeaks plan ID tp-149821

Author credentials shown on TrainingPeaks include PhD Sport Physiology & Performance, USATF Level 2 Endurance, USAT Level 1 Triathlon, NSCA CSCS.

Published plan characteristics:

```text
Running frequency: 2 days/week
Length: 16 weeks
Starting weekly mileage: 4.5 mi = 7.24 km
Peak weekly mileage: 13.5 mi = 21.73 km
Minimum single session: 2 mi = 3.22 km
Maximum single session: 7.5 mi = 12.07 km
Recommended prerequisite:
  able to jog 1.5–2 mi continuously
```

This is direct evidence that a professionally-authored Beginner 10K product can be designed around exactly two weekly runs.

### E2 — Alex Harrison, RP 10K Intermediate 2 d/wk, TrainingPeaks plan ID tp-149844

Published characteristics:

```text
Running frequency: 2 days/week
Length: 16 weeks
Starting weekly mileage: 11 mi = 17.70 km
Peak weekly mileage: 20 mi = 32.19 km
Minimum single session: 4 mi = 6.44 km
Maximum single session: 11 mi = 17.70 km
Recommended prerequisite:
  previous 5K+ / equivalent
  or ability to run ~5 mi continuously
```

This directly supports Intermediate×2D product viability.

### E3 — Women's Running, "8-week 10K training plan on 2 runs a week"

The publication explicitly offers an 8-week 2-run/week plan to progress from 5K to 10K.

The published preview image shows, for example:

```text
Week 1:
Run 1 ~ 3 mi including faster repetitions
Run 2 ~ 4 mi

Week 3:
Run 1 ~ 4 mi with hill efforts
Run 2 ~ 5 mi

Week 6:
Run 1 ~ 5 mi with short hill efforts
Run 2 ~ 7 mi

Race week:
short easy run + race
```

The site describes easy running at roughly 50–60% effort and faster running around 80% effort.

Important: this plan supports 2D feasibility but does **not** validate Appsel Model B specifically; its visible non-race weeks generally pair a quality-oriented run with a longer run.

### E4 — Running Fanatic / TrainingPeaks 2-run/week Beginner 10K family

Search surfaced 8-, 12-, 14- and 20-week Beginner 10K plans with 2 runs/week. These plans generally expect an existing low-intensity base (for example, recent 1–2 weekly runs / ~30 min running).

This is useful evidence for horizon flexibility: 2D is not inherently tied to one exact plan length.

### E5 — Bob Seebohar, "8 weeks to your 10k!" TrainingPeaks plan

Beginner/intermediate product aimed at runners already running at least 2 times/week for approximately 2.5–3 miles/session.

This further supports 8-week 2D-adjacent readiness as feasible for already-prepared runners.

---

# 6. Scientific evidence relevant to 2D

## E6 — Two vs three weekly HIIT sessions

**Physiological Reports, 2025; PMID 40976973**

Exploratory study of 1, 2 or 3 weekly 4×4-min HIIT sessions in recreationally active participants.

Result: all frequencies improved some outcomes; the largest effects for VO2max and time-to-exhaustion were seen with 2–3 sessions/week, with no clear additional benefit from 3 vs 2 in this small study.

Interpretation for Appsel:

- supports physiological plausibility of 2 sessions/week;
- does **not** prove a 2D 10K plan is equivalent to 3D;
- does **not** validate Model B or exact 10K mileage.

## E7 — REHIT frequency RCT

**Thomas et al., 2020; PMID 32078337**

2, 3 and 4 sessions/week over 6 weeks. The 2-session group improved maximal aerobic capacity; no significant between-group difference in the magnitude of the change.

Again: supports low-frequency aerobic adaptation, not race-plan equivalence.

## E8 — Low vs high frequency with volume/intensity matched

**Tripp et al., 2025; PMID 39921357**

Two-session "weekend warrior" vs four-session training with volume/intensity matched on cycle ergometer. Two-session training was not inferior for VO2max improvement.

Useful for frequency plausibility; not running-specific 10K prescription authority.

## E9 — Historical exercise-frequency synthesis

**PMID 3529283**

Review found frequencies as low as 2/week can improve aerobic power in less-fit participants; higher initial fitness may require greater training frequency for maximal aerobic-power gains.

This supports Appsel's UX distinction:

```text
2D SUPPORTED
3D RECOMMENDED
```

rather than claiming that 2D and 3D have identical performance ceilings.

---

# 7. Taper decision

## 7.1 Evidence

### E10 — 2023 endurance taper systematic review/meta-analysis

**PMID 37163550**

Across 14 studies, tapering improved time-trial and time-to-exhaustion performance. Subgroup findings supported a strategy reducing training volume by approximately 41–60% while maintaining intensity and frequency.

### E11 — Bosquet et al. taper meta-analysis

**PMID 17762369**

The classic meta-analysis similarly supported reducing training volume by roughly 41–60% while maintaining training intensity and frequency.

## 7.2 2D implication

A normal A/B alternation can cause the only taper week to land on:

```text
EASY + LONG
```

That would eliminate the frequency's dedicated sharpening/quality stimulus solely because of parity.

That is a poor fit with taper evidence, which favors **reduced volume while maintaining intensity/frequency**.

## 7.3 Research-backed decision

Recommended to freeze:

```text
NORMAL 2D:
A = KEY + LONG
B = EASY + LONG

TAPER:
Reduced KEY + Reduced LONG
```

Taper is therefore an explicit structural override to normal A/B alternation.

The existing Appsel taper factor `0.53`, if repository authority confirms it remains canonical, corresponds to a ~47% volume reduction and sits inside the 41–60% evidence-supported reduction range.

**Do not invent a new 2D taper factor unless repository representability proves the canonical one cannot work.**

Status:

```text
TAPER STRUCTURE: READY TO APPROVE
TAPER FACTOR: REUSE EXISTING CANONICAL 0.53 IF REPO CONFIRMS
```

---

# 8. Long-run allocation decision

## 8.1 Why existing 3D–6D percentages cannot simply be reused

With only two sessions/week, a long run naturally occupies a much larger share of weekly distance.

Using a generic 25–40% long-run share would force the other single run to carry an unrealistically large majority of weekly distance, or would make the plan unable to reach appropriate long-run duration.

## 8.2 Direct 2D plan observations

Women's Running visible 2D sample:

```text
Week 1: long 4 / weekly 7 mi  = 57.1%
Week 3: long 5 / weekly 9 mi  = 55.6%
Week 6: long 7 / weekly 12 mi = 58.3%
```

Alex Harrison's published max-session vs max-week figures are also approximately 55% for both Beginner and Intermediate, although those two maxima are not guaranteed by the page to occur in the same week:

```text
Beginner: 7.5 / 13.5 = 55.6%
Intermediate: 11 / 20 = 55.0%
```

This is unusually consistent contextual evidence that a 2D long run around **half to the high-50s percent** of weekly running volume is realistic.

## 8.3 Safety evidence changes what the hard guard should mean

### E12 — Frandsen et al., BJSM 2025

**"How much running is too much? Identifying high-risk running sessions in a 5200-person cohort study."**

5,205 runners, 588,071 sessions.

A single-session distance increase of >10% relative to the runner's longest run in the previous 30 days was associated with significantly higher overuse-injury rates. Week-to-week distance ratios were not associated in the same way.

This is highly relevant to 2D because weekly volume is concentrated into only two sessions.

## 8.4 Decision

Recommended 2D product calibration:

```text
PreferredLongRunShare = 55%
HardProductAllocationCap = 60%
```

Apply the SAME allocation authority on:

```text
KEY + LONG week
EASY + LONG week
```

Do **not** invent separate KEY-week and EASY-week percentages without evidence.

Why same share?

- there is no evidence supporting two different distance-share rules;
- alternation already creates load variation through workout intensity/type;
- one shared allocation rule minimizes unnecessary degrees of freedom;
- it is more deterministic.

Important semantic warning:

```text
60% IS NOT A PHYSIOLOGICAL "SAFE LIMIT".
```

It is an Appsel product allocation cap.

The stronger safety guard for 2D long-run progression should remain the existing/revalidated **single-session longest-run progression authority**, anchored to the previous 30 days.

If repository code currently treats LongRunHardShare as a literal injury-safety threshold, the future authority phase must clarify semantics before implementation.

Status:

```text
SAME SHARE ON A/B WEEKS: READY TO APPROVE
55% PREFERRED / 60% HARD PRODUCT CAP: STRONG PRODUCT-DEFAULT CANDIDATE
SINGLE-SESSION 30-DAY GUARD: HIGH-PRIORITY SAFETY AUTHORITY
```

---

# 9. Adaptation decision

## 9.1 Evidence search result

No high-quality running literature was found that justifies a deterministic statement such as:

```text
missing LONG is always worse than missing KEY
```

or:

```text
missing KEY is always worse than missing LONG
```

for an adaptive 2-run/week 10K planner.

Some coaching material prioritizes long runs for beginner race completion, but that is not strong enough to create a universal Level-agnostic severity hierarchy.

The existing Appsel adaptation philosophy already uses a monotonic completion-count floor plus role gates where justified.

## 9.2 2D state space

### Pattern A

```text
KEY + LONG
```

Reachable completion states:

```text
KEY yes / LONG yes
KEY yes / LONG no
KEY no  / LONG yes
KEY no  / LONG no
```

### Pattern B

```text
EASY + LONG
```

Reachable states:

```text
EASY yes / LONG yes
EASY yes / LONG no
EASY no  / LONG yes
EASY no  / LONG no
```

## 9.3 Recommended frequency-owned state table

```text
2 / 2 completed -> PROGRESS
1 / 2 completed -> MAINTAIN
0 / 2 completed -> REDUCE
```

For V1:

```text
1/2 KEY-only  == MAINTAIN
1/2 LONG-only == MAINTAIN
1/2 EASY-only == MAINTAIN
```

Preserve the exact missed role in trace/evidence, but do not change the adaptation action without evidence.

This is conservative:

- 50% adherence never causes progression;
- completing both planned sessions allows progression;
- completing none reduces;
- it avoids unsupported role weighting.

## 9.4 Critical determinism rule

Adaptation must **not** shift the A/B structural pattern.

Example:

```text
Week 1 = Pattern A, KEY missed
Adaptation = MAINTAIN

Week 2 must still = Pattern B
```

Do not "repeat Pattern A until the KEY is completed."

Pattern identity is driven by canonical plan-week sequence, while Adaptation changes load/progression state.

Status:

```text
2/2 PROGRESS
1/2 MAINTAIN
0/2 REDUCE

READY AS PRODUCT DEFAULT
```

---

# 10. Calendar decision

## 10.1 Evidence

NHS Couch to 5K explicitly recommends a rest day between runs for beginner progression.

Older "weekend warrior" endurance research shows meaningful aerobic adaptation can occur even when two sessions are accumulated on consecutive weekend days, so adjacent-day running cannot honestly be called universally unsafe.

However, that evidence does not establish that a race-specific `KEY + LONG` back-to-back combination is equivalent or safe enough to overturn Appsel's already-approved KEY↔LONG spacing authority.

## 10.2 Recommended Appsel behavior

Do not create a new 2D calendar rule if the existing canonical rule is sufficient.

Use the same two preferred weekdays every week:

```text
Selected day X = non-long slot
Selected day Y = LONG_RUN day
```

Then:

```text
Pattern A:
X -> KEY
Y -> LONG

Pattern B:
X -> EASY
Y -> LONG
```

Because the day positions are established against the stricter Pattern-A `KEY↔LONG` spacing requirement, Pattern B automatically inherits a well-spaced EASY/LONG schedule.

Recommended:

```text
reuse canonical KEY↔LONG minimum spacing
do not relax it only because frequency=2
do not invent an additional spacing rule
```

If the user's only two preferred days cannot satisfy the canonical KEY↔LONG spacing:

- identity `Beginner/Intermediate × 2D` remains supported;
- that specific calendar request is invalid/ineligible under scheduling constraints;
- UI should ask the user to move one selected day;
- backend must not silently add a third day.

Status:

```text
CALENDAR: REUSE EXISTING AUTHORITY
```

---

# 11. Pattern continuity across phases

This is a necessary implementation/domain detail created by Model B.

Recommended deterministic authority:

```text
Plan Week 1 -> A
Plan Week 2 -> B
Plan Week 3 -> A
Plan Week 4 -> B
...
```

The pattern should **not reset at Foundation/Build/RaceSpecific boundaries**.

Reason:

A phase reset can accidentally create:

```text
... Pattern A
new phase -> Pattern A
```

and therefore consecutive KEY weeks, defeating the intended A/B rhythm.

The only normal override is Taper:

```text
Taper -> Reduced KEY + Reduced LONG
```

For Runway/LongHorizon, the future authority phase should prefer a **global structural week ordinal** across GE → Runway → Core so the pattern remains deterministic through boundaries and fresh PostgreSQL/JIT restart.

This may require the current fixed-week `RunLayout` model to evolve into a repeating multi-week pattern abstraction.

Candidate conceptual model:

```text
RunLayout2D
  DaysPerWeek = 2
  PatternPeriodWeeks = 2

  Pattern[0] = [KEY_SESSION, LONG_RUN]
  Pattern[1] = [EASY_SUPPORT, LONG_RUN]

  TaperOverride = [KEY_SESSION, LONG_RUN]
```

Do not implement this exact schema blindly. The future implementation phase must first inspect the live catalog/domain contracts.

---

# 12. Beginner × 2D numeric authority

## 12.1 What external evidence supports

Strong exact 2D Beginner anchor from E1:

```text
start weekly = 7.24 km
peak weekly = 21.73 km
min session = 3.22 km
max session = 12.07 km
prerequisite = ~2.4–3.2 km continuous running
```

Women's Running provides a near-match 2D 5K→10K progression with visible weekly totals in the high teens km by later training.

## 12.2 Readiness recommendation

The strong 2D plans found are **not true zero-running race-plan starts**. They assume some current running ability/base.

Because 2D concentrates weekly load into two larger sessions, a fabricated starting-volume fallback is particularly undesirable.

Recommended:

```text
positive observed recent running evidence -> supported path

explicit zero recent running readiness -> PRODUCT_INELIGIBLE

missing load readiness -> PRODUCT_INELIGIBLE
unless a future canonical resolver explicitly proves another current-load
anchor is independently sufficient
```

In particular, do not infer weekly load tolerance from a race result alone.

This still satisfies:

```text
Beginner × 2D = SUPPORTED PRODUCT IDENTITY
```

because support and request eligibility are distinct.

## 12.3 PeakVolumeBand and ResolvedPeakReference

The web evidence provides useful anchors, but not enough independent data to responsibly freeze exact Appsel catalog-band endpoints.

Evidence anchor:

```text
credible Beginner 2D peak vicinity ≈ 19–22 km/week
```

But:

```text
PeakVolumeBand != one coach's peak week
ResolvedPeakReference != automatically the midpoint
```

Therefore the future repo authority phase must deliberately author exact values after comparing:

- existing Beginner×4D canonical band/reference;
- historical peak-band governance;
- current minimum session/catalog capacity;
- E1/E3 external evidence.

Do not invent a formula like:

```text
Beginner4D band minus X km per missing run day
```

Status:

```text
Beginner2D exact PeakVolumeBand: DECISION_REQUIRED IN AUTHORITY PHASE
Beginner2D exact ResolvedPeakReference: DECISION_REQUIRED IN AUTHORITY PHASE
```

## 12.4 Taper/minimum volume

Recommended:

```text
reuse canonical taper factor 0.53 if repo confirms
```

Exact minimum representable weekly volume must be calculated from the final 2D KEY/EASY/LONG prescriptions and existing catalog minima.

Do not derive a readiness threshold from the minimum representable volume.

---

# 13. Intermediate × 2D numeric authority

## 13.1 Strong evidence anchor

E2:

```text
start weekly = 17.70 km
peak weekly = 32.19 km
min single session = 6.44 km
max single session = 17.70 km
```

The source expects materially more readiness than the Beginner plan.

## 13.2 Readiness recommendation

For Intermediate×2D, the evidence for requiring current running readiness is stronger.

Recommended:

```text
positive observed recent load -> supported path
missing -> PRODUCT_INELIGIBLE
explicit zero -> PRODUCT_INELIGIBLE
```

No default start volume.

Do not use recent race performance as a substitute for current load tolerance unless an existing canonical resolver already authorizes it.

## 13.3 PeakVolumeBand / reference

E2's peak of ~32.2 km is a strong external calibration anchor.

However, current Appsel Intermediate×3D canonical authority must be considered. External coaching plans can use much higher volumes than Appsel's chosen product envelope, so one source cannot directly become the canonical band.

Required future decision:

```text
Intermediate2D PeakVolumeBand: deliberate catalog row
Intermediate2D ResolvedPeakReference: deliberate calibration point
```

No interpolation.

No automatic reuse of 3D.

No `3D - X km` formula.

Status:

```text
exact band/reference remain open until repo authority phase
```

---

# 14. Horizon eligibility

## 14.1 Core 8–14

External evidence includes exact 2-run/week 10K products at:

```text
8 weeks
12 weeks
14 weeks
16 weeks
20 weeks
```

Therefore there is no evidence-based reason to declare 8–14 2D identity-level non-support.

Recommended:

```text
Beginner×2D Core 8–14 = SUPPORTED
Intermediate×2D Core 8–14 = SUPPORTED
```

but only for requests whose readiness makes the numeric plan representable.

An 8-week Beginner plan may require substantially better initial readiness than a 14-week plan.

Correct classification when readiness is insufficient:

```text
SUPPORTED IDENTITY
+
PRODUCT_INELIGIBLE REQUEST
```

not `PRODUCT_NON_SUPPORT`.

## 14.2 Preparation Runway 15–20

Exact 16- and 20-week 2D 10K plans exist.

Recommended:

```text
15–20 = SUPPORTED
```

subject to the same readiness and representability checks.

Runway should retain the same A/B pattern rather than inventing a separate frequency ramp.

## 14.3 LongHorizon 21–52

Direct 2D 10K plan evidence beyond 20 weeks is limited.

However:

- longer 2D endurance plans exist in other race-distance contexts;
- low-frequency endurance adaptation is physiologically plausible;
- Appsel LongHorizon GE is a target-capped general-preparation mechanism rather than 52 weeks of continuous race-specific escalation.

Recommended product direction:

```text
21–52 LongHorizon = SUPPORTED
```

with important constraints:

```text
positive observed readiness required
missing/zero -> PRODUCT_INELIGIBLE
target-capped GE
no uncapped weekly growth
single-session longest-run progression guard
```

This is an **Appsel product default supported by general evidence**, not direct proof from a 52-week 2D 10K trial.

The authority phase must still run full 21–52 representability after exact PeakReference/Band/session minima are frozen.

---

# 15. Current answer to the seven open questions

| Question | Current research-backed answer | Status |
|---|---|---|
| Taper | Override alternation; use `Reduced KEY + Reduced LONG`; reuse 0.53 if canonical | Ready to freeze |
| Long-run allocation | Same allocation on A/B; 55% preferred / 60% product cap is strongest candidate; session-specific 30-day progression remains real safety guard | Strong candidate |
| Adaptation | `2/2 Progress`, `1/2 Maintain`, `0/2 Reduce`; no role asymmetry in V1, but retain missed-role trace | Ready as product default |
| Calendar | Reuse canonical KEY↔LONG spacing; bind same two weekdays across A/B; no new 2D spacing rule | Ready to freeze |
| Beginner×2D numeric | Positive observed readiness; missing/zero ineligible recommended; taper reusable; exact PeakBand/reference still needs repo decision | Partial |
| Intermediate×2D numeric | Positive observed readiness; missing/zero ineligible recommended; exact PeakBand/reference still needs repo decision | Partial |
| Horizon eligibility | Identity-level support for Core 8–14, Runway 15–20, LongHorizon 21–52; low-readiness requests may be PRODUCT_INELIGIBLE | Ready as product direction |

---

# 16. Evidence caveat about Model B

Claude must not claim:

```text
research proves A/B alternation is the optimal 2D structure
```

It does not.

Published exact 2D 10K plans often use some form of quality-oriented work plus a longer run every week.

The Appsel decision:

```text
A = KEY + LONG
B = EASY + LONG
```

is an explicit product choice intended to:

- reduce the density of quality-oriented sessions when only two running days exist;
- provide a clear aerobic/recovery microcycle;
- keep a dedicated long-run stimulus every week;
- create a deterministic shared frequency structure for Beginner and Intermediate.

The expected trade-off is that an Intermediate 2D plan may have a lower race-performance ceiling than a weekly-quality 2D program.

This reinforces the UI recommendation:

```text
3D recommended
2D supported
```

Do not hide this trade-off.

---

# 17. Architecture implications

2D is the first Appsel RunLayout that does not have one fixed weekly role list.

Existing conceptual model:

```text
RunLayout
-> one weekly role list
```

2D requires:

```text
RunLayout
-> repeating weekly structural pattern sequence
```

Future implementation must audit whether to introduce a generic concept such as:

```text
WeeklyRolePattern[]
PatternPeriodWeeks
PatternOrdinal
```

or another equivalent generic mechanism.

Avoid a one-off runtime hack:

```text
if DaysPerWeek == 2 && weekNumber % 2 == ...
```

if the catalog/domain model can own the pattern declaratively.

Important runtime surfaces to inspect later:

- catalog RunLayout schema;
- phase skeleton materializer;
- dynamic Core allocator;
- Preparation Runway;
- LongHorizon GE/JIT;
- calendar materializer;
- adaptation input/cardinality;
- persistence/reload;
- repair;
- structural lineage;
- checkpoint continuation;
- template/candidate manifest resolution;
- public-gate isolation.

Expected schema impact must be audited rather than assumed.

---

# 18. Recommended execution sequence after the currently-running Advanced phase

Do not start 2D work until the current Advanced combined implementation/dark wave reaches its governed closure.

Then:

## Phase A — 2D Beginner + Intermediate full authority

Evidence/product/domain only.

Must freeze:

```text
RUN_LAYOUT_2D repeating A/B authority
global pattern continuity
Taper override
55/60 long-run policy or exact alternative
2-session adaptation
calendar
Beginner readiness behavior
Intermediate readiness behavior
Beginner PeakVolumeBand
Beginner ResolvedPeakReference
Intermediate PeakVolumeBand
Intermediate ResolvedPeakReference
exact KEY/EASY/LONG prescriptions
minimum representable volume
Core 8–14
Runway 15–20
LongHorizon 21–52
```

No production code.

## Phase B — Beginner×2D + Intermediate×2D combined implementation/dark

One implementation wave.

Expected scope:

```text
generic repeating RunLayout support
RUN_LAYOUT_2D
candidate/manifests
catalog rows
2D prescription content
calendar
Adaptation
Core
Runway
LongHorizon
real PostgreSQL
JIT/restart
TargetFinishTimeSource
repair
full 8–52 dark matrices
existing frequencies zero-delta
public gates CLOSED
```

## Phase C — Beginner 3D + 5D product realignment authority

Separate from 2D.

- 3D: supersede old non-representability by auditing which old constraint is mutable.
- 5D: preserve `2 KEY + 2 EASY + LONG`, but define Beginner-safe `SECONDARY_CONTROLLED` prescription.

## Phase D — Beginner 3D + 5D combined implementation/dark

Keep Beginner4D as zero-delta control.

## Phase E — Combined new-frequency public activation

Activate together:

```text
Beginner 2D
Beginner 3D
Beginner 5D
Intermediate 2D
```

Preserve Beginner4D.

## Phase F — Advanced combined public activation

After Advanced dark implementation and after shared-path regressions from the new frequency work are clean.

## Phase G — Final 10K V1 matrix/regression/governance closure

Final desired matrix:

```text
Beginner:     2D 3D 4D 5D PUBLIC; 6D 7D non-support
Intermediate: 2D 3D 4D 5D 6D PUBLIC; 7D non-support
Advanced:     3D 4D 5D 6D PUBLIC; 2D out of V1; 7D non-support
```

---

# 19. Required next Claude authority-phase behavior

When this work is scheduled, Claude should:

1. Read `PHASE_LEDGER.md` and `MASTER_ROADMAP.md` first.
2. Verify the currently-running Advanced phase has closed.
3. Determine the next free phase ID from repository truth; never assume it.
4. Treat this handoff as planning context only.
5. Preserve these user-frozen decisions:
   - Beginner+Intermediate 2D support required;
   - Advanced2D out of V1;
   - Model B A/B pattern;
   - no silent fallback;
   - 3D recommended but not mandatory.
6. Audit live repository authority before freezing exact numerics.
7. Resolve the still-open PeakBand/reference/session-minimum/prescription values without interpolation.
8. Keep support distinct from request-level eligibility.
9. Produce full Core/Runway/LongHorizon representability only **after** numerics are frozen.
10. No code in the authority phase.

---

# 20. Source inventory

## Peer-reviewed / high authority

**[E6]** "Impact of weekly frequency of high-intensity interval training on cardiorespiratory, metabolic, and performance measures in recreational runners — An exploratory study." Physiological Reports. PMID **40976973**.

**[E7]** Thomas et al. "Reducing training frequency from 3 or 4 sessions/week to 2 sessions/week does not attenuate improvements in maximal aerobic capacity with reduced-exertion high-intensity interval training (REHIT)." 2020. PMID **32078337**.

**[E8]** Tripp et al. "Cardiorespiratory Fitness Improvements Following Low-Frequency Training Are Not Inferior to High-Frequency Training Matched for Intensity and Volume." 2025. PMID **39921357**.

**[E9]** "The interactions of intensity, frequency and duration of exercise training in altering cardiorespiratory fitness." PMID **3529283**.

**[E10]** "Effects of tapering on performance in endurance athletes: A systematic review and meta-analysis." PMID **37163550**.

**[E11]** Bosquet et al. "Effects of tapering on performance: a meta-analysis." PMID **17762369**.

**[E12]** Frandsen et al. "How much running is too much? Identifying high-risk running sessions in a 5200-person cohort study." British Journal of Sports Medicine, 2025;59:1203–1210. DOI **10.1136/bjsports-2024-109380**. PubMed PMID **40623829**.

**[E13]** "Does cumulating endurance training at the weekends impair training effectiveness?" PMID **16874148**. Useful contextual evidence that two accumulated weekly sessions can still improve endurance; not authority to override Appsel KEY↔LONG spacing.

**[E14]** NURMI: "Training and Racing Behavior of Recreational Runners by Race Distance." Large observational sample; median weekly frequency for <21 km runners was 3, supporting 3D as a normal recommendation while not establishing 2D as invalid.

## Professional / plan evidence

**[E1]** Dr. Alex Harrison — TrainingPeaks — `RP 10k Beginner 2 d/wk`, plan ID **tp-149821**.

**[E2]** Dr. Alex Harrison — TrainingPeaks — `RP 10k Intermediate 2 d/wk`, plan ID **tp-149844**.

**[E3]** Women's Running — `8-week 10K training plan on 2 runs a week`.

**[E4]** Running Fanatic / TrainingPeaks — 2-run/week Beginner 10K plans at 8/12/14/20 weeks.

**[E5]** Bob Seebohar / TrainingPeaks — `8 weeks to your 10k!`, plan ID **tp-125656**.

**[E15]** NHS Better Health — Couch to 5K — 3 runs/week with rest day between runs. Relevant as recovery guidance and support for 3D recommendation, not a 2D 10K authority.

---

# 21. Bottom-line handoff

The project has moved from:

```text
2D = backlog / unsupported
```

to:

```text
2D = required first-class V1 frequency capability
for Beginner and Intermediate.
```

The structural question is closed by user decision:

```text
A: KEY + LONG
B: EASY + LONG
repeat
```

The research now supports the following direction:

```text
Taper:
Reduced KEY + Reduced LONG

Long-run:
same allocation on A and B
55% preferred / 60% product cap is the strongest evidence-backed candidate
single-session 30-day progression remains the safety-critical guard

Adaptation:
2/2 Progress
1/2 Maintain
0/2 Reduce
role traced, no unsupported role weighting

Calendar:
reuse canonical KEY↔LONG spacing
same selected weekdays across A/B

Readiness:
positive recent running evidence should be required for 2D race-plan entry
missing/zero should fail request-level eligibility rather than trigger a
fabricated default

Horizons:
Core 8–14 supported
Runway 15–20 supported
LongHorizon 21–52 supported as a target-capped product direction,
subject to final numeric representability
```

Still deliberately unresolved until the repository authority phase:

```text
Beginner2D exact PeakVolumeBand
Beginner2D exact ResolvedPeakReference
Intermediate2D exact PeakVolumeBand
Intermediate2D exact ResolvedPeakReference
exact minimum representable weekly volumes
exact phase/Level KEY/EASY/LONG prescription bindings
final catalog/schema representation of repeating RunLayout patterns
```

Those values must be frozen from repository truth + the evidence above before implementation.

