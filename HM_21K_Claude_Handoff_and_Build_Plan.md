# APPSEL — 21.1K / HALF-MARATHON
## Claude Handoff + Fast Build Strategy
### Status: research handoff / architecture reuse plan — not yet HM production authority

## 0. Objective

Extend Appsel from the already-generalized 10K system to Half Marathon (21.1 km) without rebuilding the Level × Frequency architecture or creating one-off plan classes per combination.

The main principle is:

```text
Distance
× Level
× Frequency
× HorizonFamily
```

must remain orthogonal.

For HM, reuse the generic machinery already proven by 10K and introduce only the genuinely distance-specific authorities.

Do NOT:
- clone the 10K implementation into HM-specific per-cell classes;
- create `HalfMarathonIntermediate4D`, `HalfMarathonBeginner3D`, etc. as independent algorithmic authorities;
- copy 10K numeric values into HM merely because the architecture is reusable;
- reopen the book corpus unless the HM gap ledger proves a genuinely unsupported atomic decision;
- silently promote research candidates into production authority.

---

# 1. Current 10K architecture / rollout proof

Treat this as the current achieved architecture state supplied by the product owner:

```text
              2D              3D          4D      5D      6D      7D
Beginner      PUBLIC (tam)    PUBLIC*     PUBLIC  ⛔      ⛔      ⛔
Intermediate  PUBLIC (tam)    PUBLIC      PUBLIC  PUBLIC  PUBLIC  ⛔
Advanced      V1 dışı         PUBLIC      PUBLIC  PUBLIC  PUBLIC  ⛔

* explicit-zero readiness hariç (bilinçli, PRODUCT_INELIGIBLE)
```

This matrix proves that Appsel has already paid most of the architecture cost for:
- Level as an independent axis;
- Frequency as an independent axis;
- 2D/3D/4D/5D/6D structural layouts;
- multi-KEY support where required;
- calendar assignment by preferred days;
- role-aware adaptation by frequency;
- typed product eligibility;
- public rollout gates per combination.

For HM, this architecture should be reused, not recreated.

Important: the 10K public eligibility matrix is architecture/product precedent, not automatic HM eligibility. HM must select its own numeric/readiness authorities.

---

# 2. Target architecture for HM

Preferred conceptual composition:

```text
HALF_MARATHON_MASTER
+
RUN_LAYOUT_{2D|3D|4D|5D|6D}
+
{BEGINNER|INTERMEDIATE|ADVANCED}_MODIFIER
+
HALF_MARATHON_WORKOUT_PROGRESSION
+
DISTANCE/FREQUENCY/LEVEL NUMERIC POLICIES
+
GENERIC RUNTIME RESOLVERS
```

The goal is to make adding HM mostly:
- new distance data;
- new distance-specific workout progression;
- new distance-specific phase/horizon rules;
- a small amount of parameterization where 10K literals still exist.

It must not become a second plan-generation engine.

---

# 3. What should be reusable from 10K

Audit and reuse these unless the real repository proves hidden distance coupling:

## Generic structural/runtime mechanisms
- RunLayout / weekly role cardinality.
- Level/Frequency compatibility and public rollout gate mechanism.
- PreferredDays handling.
- LongRunDayPreference binding.
- Calendar materialization.
- Mid-week StartDate behavior.
- Role/cardinality validation.
- Catalog loading/versioning/hash/publish mechanics.
- Catalog binding.
- Preview / confirm lifecycle.
- Persistence mapping.
- TrainingDay completion / not-today plumbing.
- Frequency-aware adaptation semantics.
- Multi-KEY counting/support.
- Typed eligibility / fail-closed behavior.
- Partial-day race-alignment mechanism.
- Pace evidence source hierarchy mechanism.
- Recent-volume / recent-longest / recent-race resolver separation.
- Single-session history safeguard mechanism.
- Bounded weekly progression engine.
- Runway/rolling-horizon *mechanisms*.

## Reuse the mechanism, not necessarily the numeric data
The following can be structurally generic while the selected values remain HM-specific:
- peak-volume bands;
- starting-volume caps;
- progression coefficients/caps;
- session-volume allocation;
- long-run share/cap;
- workout eligibility;
- workout dose;
- phase allocation;
- taper allocation;
- horizon thresholds;
- runway composition;
- goal-specific workout progression.

---

# 4. Existing HM research / canonical candidates

These are the current research conclusions and existing Appsel candidates. They are ready for governance adoption, but Claude must distinguish:
- existing repository authority,
- research-supported candidate,
- product default still needing explicit approval.

## 4.1 Core horizon candidate

```text
Half Marathon core:
minimum   = 10 weeks
preferred = 14 weeks
maximum   = 16 weeks
```

Preferred 14-week phase shape:

```text
Foundation    3
Build         4
RaceSpecific  5
Taper         2
```

Do not invent the exact 10/11/12/13/15/16-week allocation table yet.
That is an explicit remaining product-policy closure item.

## 4.2 HM peak weekly-volume candidate bands already available for 3D–5D

```text
                  3D       4D       5D
Beginner/New      22–32    28–38    32–44
Intermediate      28–40    36–50    44–58
Advanced          36–48    46–60    54–70
Experienced       40–52    50–66    60–76
```

These are product envelopes, not compulsory targets.

Invariant:

```text
peak_target must never be selected independently of starting_volume
```

If the reachable peak is below the typical band minimum, the plan may remain below the typical band rather than forcing unsafe acceleration.

No equivalent HM 2D or 6D peak-volume authority is considered frozen yet.

## 4.3 Starting long-run candidate caps

```text
Half Marathon starting absolute cap:

Beginner/New              8 km
Intermediate             12 km
Advanced/Experienced     15 km
```

Minimum sensible HM long-run start candidate:

```text
5 km
```

Starting long run must remain bounded by:
- recent longest-run evidence;
- weekly-volume compatibility;
- frequency-specific weekly-share cap;
- distance × level absolute cap;
- single-session spike safeguard.

These are *starting* caps, not peak-long-run caps.

## 4.4 Readiness / short-horizon candidates

Current HM candidate routing:

```text
STANDARD / CORE:
>= 10 weeks

COMPRESSED:
7–9 weeks

READINESS_ONLY:
<= 6 weeks
```

Candidate compressed-readiness entry:

```text
recent weekly volume >= ~22 km
recent longest run   >= ~10 km
```

These are product readiness thresholds, not guarantees that the user can safely complete the race.

## 4.5 Pace evidence hierarchy

Reuse the generic hierarchy:

```text
1. reliable recent race
2. time trial
3. structured test
4. normal pace
5. effort-only
```

Critical invariant:

```text
goal pace != starting training pace
```

If independent pace evidence is absent:
- do not fabricate exact pace from the desired finish time;
- allow effort-based prescription;
- preserve the user's race goal separately from the current feasible training target.

---

# 5. HM-LIT-15 → HM-LIT-27 research closure

These items were researched and are considered closed enough for product decision/adoption. They are not automatically repository authority until explicitly encoded.

## HM-LIT-15 — Easy / Recovery Running

Decision:

```text
EASY_SUPPORT = structural role
RECOVERY     = not a separate structural role
RECOVERY     = lower-dose / lower-intensity EASY_SUPPORT intent
```

Do not encode a rigid universal "exactly 80% easy" rule.

Required principle:

```text
LOW_INTENSITY_DOMINANCE
```

should emerge from:
- hard-session ceiling;
- quality-dose caps;
- easy structural slots;
- easy long-run default.

Effort candidate:

```text
easy:
conversational
RPE roughly 2–4 / 10
```

## HM-LIT-16 — Strides / Hills / Neuromuscular Work

Separate:

```text
STRIDES / SHORT_HILL_STRIDES
→ adjunct
→ short
→ controlled
→ full recovery
→ does not consume a full hard-session slot

STRUCTURED_HILL_REPETITIONS
→ KEY_SESSION
→ hard stimulus
```

Candidate strides:

```text
4–8 × 15–20 sec
controlled-fast
not sprinting
full easy recovery
```

Hill work is not mandatory for every HM user.

## HM-LIT-17 — Workout Dose & Session Load

Do not manage quality with a whole-session scalar.

Preferred model:

```text
workoutFamily
× progressionStage
× doseTier
```

Main-set dose excludes:
- warm-up;
- recovery jog;
- cool-down.

Those still count in total planned training volume.

Research/product candidate envelopes:

```text
Faster / 5K–10K work
LOW       8–12 min
STANDARD 12–18 min
HIGH     16–24 min

Threshold
LOW      15–20 min
STANDARD 20–30 min
HIGH     25–40 min

HM Pace
LOW      15–25 min
STANDARD 25–45 min
HIGH     35–60 min

HM Pace inside Long Run
LOW      10–20 min
STANDARD 20–35 min
HIGH     30–45 min
```

Candidate level mapping:

```text
Beginner/New  → LOW
Intermediate  → STANDARD
Advanced      → HIGH
```

Quality long run:
- threshold/HM-pace/structured-hard finish => consumes quality budget;
- controlled aerobic steady finish does not automatically need to count as a hard stimulus.

Do not increase weekly volume materially and quality dose materially in the same transition.

## HM-LIT-18 — Warm-up / Cool-down

KEY workout:

```text
warm-up:
required
~10–15 min easy

optional:
dynamic preparation
short controlled strides
```

KEY cool-down:

```text
~5–10 min easy
recommended
not a hard safety invariant
```

Easy/Long:
- no separate formal warm-up component required;
- first 5–10 min can naturally begin very easy.

Warm-up/cool-down count in total planned distance, not main-set quality dose.

## HM-LIT-19 — Race Execution & Pacing

Default race strategy:

```text
CONTROLLED_EVEN_EFFORT
```

Candidate execution model:

```text
0–3 km
CONTROLLED_START
goal pace is a ceiling, not something to beat

3–17 km
SETTLE
target race effort / feasible target pace

17–21.1 km
HOLD_OR_PROGRESS
increase only if still controlled
```

Do not force a negative split.
Do not force a positive split.

Terrain, wind, heat:
```text
EFFORT > PACE
```

If reliable pace evidence is missing:
- race execution remains effort-based.

## HM-LIT-20 — Race Week / Pre-Race Scheduling

Research conclusion:

```text
preferred HM taper = 2 weeks
minimum HM taper   = 1 week
```

Candidate race-relative scheduling:

```text
D-14 .. D-10
last peak-long-run window

D-7 .. D-5
last substantial KEY window

D-4 .. D-3
short activation / sharpening

D-2
easy or rest

D-1
rest
OR familiar short shakeout

D
RACE
```

D-1 shakeout should be optional and familiarity/frequency-dependent.

Race is its own load classification and should not be counted as a normal training hard-session in adherence logic.

## HM-LIT-21 — Calendar / Recovery Spacing

Normal KEY vs easy LONG:

```text
minimum:
>= 2 calendar days
```

Two true hard stimuli:

```text
minimum approximately 48h
```

KEY vs quality LONG:

```text
hard minimum ~48h
preferred >=72h
```

Adjacency:

```text
HARD + HARD
→ prohibited

HARD + EASY
→ allowed

EASY + LONG
→ allowed when LONG is easy
```

If PreferredDays cannot represent the required structure:
1. deterministic alternative placement;
2. downgrade quality-long complexity;
3. downgrade secondary quality;
4. typed fail-closed.

Do not compress two hard sessions together to satisfy the calendar.

## HM-LIT-22 — Terrain / Hills / Surface Specificity

Do not create a separate HILLY_HALF_MARATHON_MASTER.

Optional runtime context:

```text
courseProfile:
UNKNOWN
FLAT
ROLLING
HILLY
```

Behavior candidate:

```text
UNKNOWN / FLAT
→ hill work optional

ROLLING
→ hill candidate priority increases

HILLY
→ hill exposure expected in Build/RaceSpecific
→ HM-pace on hills prescribed by effort
→ terrain-similar long run recommended
```

No exact elevation-gain matching in V1.

Technical trail is outside standard road-HM V1.

## HM-LIT-23 — Environmental Conditions

Environment is a runtime execution overlay, not part of HALF_MARATHON_MASTER.

Meaningful heat/humidity:

```text
PACE_PRIMARY  = false
EFFORT_PRIMARY = true
```

Do not invent a deterministic sec/km temperature adjustment in V1.

Hot-race preparation may surface a heat-acclimation advisory if enough time exists, without adding hidden training load.

Altitude context:
- reduce exact pace confidence;
- prefer effort;
- do not create an altitude-training subsystem in HM V1.

## HM-LIT-24 — Recovery / Fatigue / Adaptation

Do not create an HM-specific adaptation engine.

Reuse the generic hierarchy:

```text
1. safety
2. completion severity
3. role identity
4. perceived load / recovery
```

Missed workouts are not "caught up" by squeezing them into later days.

Role-aware progression principle:
- missing EASY may still allow progression if the rest of the week is completed;
- missing KEY or LONG should generally block automatic progression and lead to Maintain;
- high fatigue / unusually high RPE can override apparent completion and lead to Maintain/Reduce.

Reuse the already-generalized frequency-aware adaptation authority.

## HM-LIT-25 — Plan Entry / Baseline Assessment

Required baseline:

```text
RecentWeeklyVolume
→ last 28 days average

RecentLongestRun
→ last 30 days

RecentRunsPerWeek
→ last 28 days

SelectedRunsPerWeek
```

Optional:
```text
RecentRace
RecentTimeTrial
StructuredTest
```

Do not use demographic average pace as a synthetic training pace.

If pace evidence is unavailable:
```text
EFFORT_ONLY
```

Zero/near-zero running history:
- enough horizon => runway / consistency preparation;
- insufficient horizon => readiness-only or typed unsupported.

## HM-LIT-26 — Race Goal Feasibility

Keep:

```text
training pace derives from current fitness,
not desired goal fitness
```

Candidate goal-gap classification:

```text
goal <= current prediction
→ CONSERVATIVE

0–3% faster
→ REALISTIC

>3–6%
→ CHALLENGING

>6–10%
→ STRETCH

>10%
→ CURRENTLY_UNSUPPORTED
```

This classification must be evaluated together with:
- available weeks;
- weekly-volume readiness;
- long-run readiness;
- result recency.

CURRENTLY_UNSUPPORTED does not need to delete the user's goal.
It means the training target stays at current feasible fitness until evidence changes.

No independent pace evidence:
```text
goal feasibility = NOT_EVALUATED
exact goal-pace prescription = prohibited
effort-based specificity = allowed
```

## HM-LIT-27 — Safety / Scope Boundaries

Standard HM generator is not a medical rehabilitation system.

Use product safety states such as:

```text
NORMAL
CAUTION
STOP_AND_REVIEW
```

Examples requiring stop/review:
- exertional chest discomfort;
- fainting / near-fainting;
- unusual unexplained breathlessness;
- clinician restriction;
- acute injury;
- pain altering running gait.

Dedicated protocols outside standard HM V1:
- post-surgery return-to-run;
- fracture rehabilitation;
- active injury rehabilitation;
- pregnancy-specific training;
- postpartum return-to-running;
- clinician-directed cardiac rehabilitation.

Do not infer that these users can never run; only that the generic HM algorithm is not the correct authority.

---

# 6. Important HM decisions that are NOT frozen yet

Claude must not invent these.

## OPEN-HM-01 — Exact 10–16 week phase allocation

Known:

```text
14 weeks = 3 Foundation / 4 Build / 5 RaceSpecific / 2 Taper
```

Unknown/final decision required:

```text
10 weeks → ?
11 weeks → ?
12 weeks → ?
13 weeks → ?
15 weeks → ?
16 weeks → ?
```

Use existing phase-compression/extension principles, but require explicit HM governance approval.

## OPEN-HM-02 — Peak long-run authority

Legacy candidates include approximately 18–19 km, but this is not yet treated as a frozen canonical peak-long-run authority.

Need to determine:
- absolute distance vs duration vs weekly-share interaction;
- level effects;
- frequency effects;
- peak placement;
- whether any users can/should exceed 21.1 km in training;
- how quality-long-run content affects the cap.

Do not confuse starting long-run caps with peak-long-run caps.

## OPEN-HM-03 — Exact HM workout progression

Need a frozen phase-relative progression, e.g. conceptually:

```text
Foundation
→ aerobic / strides / basic quality

Build
→ threshold development

RaceSpecific
→ HM-specific introduction
→ HM-specific development
→ HM-specific rehearsal

Taper
→ reduced-volume HM activation
```

But exact stages, candidate workout identities and exposure minima/maxima must be adopted explicitly.

## OPEN-HM-04 — Quality-long-run vs weekly hard-budget policy

Need exact product rule for:
- easy long run;
- steady/progressive long run;
- HM-pace long run;
- threshold-containing long run.

Especially:
- when does LONG consume a hard-session slot?
- can it coexist with one KEY?
- what changes by 3D/4D/5D/6D?

## OPEN-HM-05 — HM 2D numeric policy

Architecture exists from 10K, but HM still needs explicit numeric authority for at least:
- peak weekly volume;
- session concentration;
- long-run share/cap;
- KEY vs LONG dosage;
- readiness threshold.

Do not derive by mechanically copying 10K 2D numbers.

## OPEN-HM-06 — HM 6D numeric policy

Architecture exists from 10K, but HM needs explicit authority for:
- peak volume;
- long-run share/cap;
- two-KEY interaction;
- threshold vs HM-pace sequencing;
- quality budget;
- session allocation.

Do not extrapolate the 5D HM bands without governance.

## OPEN-HM-07 — >16 week HM composition

The generic horizon mechanism is reusable, but HM needs the distance-specific policy:

```text
>16 weeks
→ runway + core
```

Open:
- composed core = preferred 14 or sometimes extended 15/16?
- minimum runway?
- maximum supported horizon?
- exact runway block route?
- core-entry deload?

Do not copy 10K's "15+ => runway + 12W core" rule.

---

# 7. Fastest safe build strategy

Do not attempt the full HM matrix in one phase.

The 10K work already proved the cross-product architecture. HM should now be built as a distance vertical slice and then expanded mostly through policy data.

## WAVE HM-A — Distance reuse audit
NO CODE.

Objective:
prove exactly which current mechanisms are:
- distance-generic;
- 10K-specific data;
- 10K-specific control flow;
- hidden hardcoded 10K coupling.

Search especially:
- `TenK`;
- `TEN_K`;
- 10.0 / target distance literals;
- 12-week assumptions;
- 8–14 horizon assumptions;
- 10K progression keys;
- 10K pace/workout keys;
- 10K-specific eligibility gates.

Output:
`HM_DISTANCE_GENERALIZATION_REUSE_AUDIT.md`

Classification:
```text
GENERIC_REUSE
PARAMETERIZE_BY_DISTANCE
NEW_HM_POLICY_DATA
HM_SPECIFIC_WORKOUT_PROGRESSION
BLOCKING_HARDCODE
```

No production change.

## WAVE HM-B — Adopt HM authorities
Still avoid public activation.

Create/freeze only the required distance authorities:
- HALF_MARATHON_MASTER;
- HALF_MARATHON_WORKOUT_PROGRESSION;
- HM phase-allocation policy;
- HM horizon thresholds;
- HM peak-volume bands;
- HM starting-long-run caps;
- HM taper policy;
- HM workout-dose/eligibility policy;
- HM goal-pace workout semantics.

Do not duplicate generic RunLayout or LevelModifier artifacts.

## WAVE HM-C — First dark vertical slice

Target:

```text
HALF_MARATHON
× INTERMEDIATE
× 4D
× 14-WEEK PREFERRED CORE
```

Why first:
- preferred 14-week shape already has the clearest candidate;
- 4D is the most mature path in the existing architecture;
- Intermediate is the least edge-case-heavy level;
- HM 4D Intermediate peak-volume band already exists;
- it gives one deterministic golden fixture for the entire distance family.

Acceptance:
- same generic calendar/materialization pipeline;
- same generic level/frequency machinery;
- no public route;
- no 10K behavior change;
- deterministic HM golden fixture;
- no fabricated pace;
- all HM-specific values trace to a typed authority.

## WAVE HM-D — Dynamic 10–16 week core

Only after HM-C is green.

Freeze the exact HM allocation table:

```text
10
11
12
13
14
15
16 weeks
```

Then reuse the dynamic core orchestrator/mechanism.

Do not build separate static templates for each week count.

## WAVE HM-E — Expand first to the HM cells that already have 3D–5D numeric evidence

Recommended first rollout set:

```text
              3D      4D      5D
Beginner      TARGET  TARGET  —
Intermediate  TARGET  TARGET  TARGET
Advanced      TARGET  TARGET  TARGET
```

That is 8 HM cells:

```text
Beginner:
3D, 4D

Intermediate:
3D, 4D, 5D

Advanced:
3D, 4D, 5D
```

Reason:
existing HM peak-volume candidate bands already cover 3D/4D/5D.
This avoids inventing 2D/6D HM volume policy merely to mirror the 10K product matrix.

Reuse the already-existing:
- 3D/4D/5D layouts;
- adaptation semantics;
- calendar logic;
- public compatibility-gate pattern.

Only HM policy data should differ.

## WAVE HM-F — Targeted 2D / 6D policy closure

After 3D–5D HM is stable, close only the missing numeric authorities for:

```text
Beginner 2D
Intermediate 2D
Intermediate 6D
Advanced 6D
```

This would let HM eventually mirror the proven 10K architecture matrix without reopening the full research program.

This phase should be mostly product-policy/evidence closure, not architecture work.

## WAVE HM-G — Short horizon and readiness

Then add:
- 7–9 week compressed HM;
- <=6 week readiness-only;
- current-readiness gating;
- no missing-fitness chasing.

Keep these behind typed eligibility until fully tested.

## WAVE HM-H — >16 runway / rolling horizon

Only after standalone HM Core is stable.

Reuse generic runway/long-horizon runtime mechanics, but introduce HM-specific composition data.

Do not copy the 10K preferred-core composition rule automatically.

## WAVE HM-I — Public rollout

Use a matrix gate just like 10K.

Public activation must be cell-by-cell and require:
- authority exists;
- catalog can express the combination;
- core/horizon feasible;
- readiness valid;
- regression green.

---

# 8. Recommended HM rollout order

Fastest useful production path:

```text
1. Intermediate 4D / 14W
2. Intermediate 4D / dynamic 10–16W
3. Intermediate 3D + 5D
4. Beginner 3D + 4D
5. Advanced 3D + 4D + 5D
6. Beginner 2D
7. Intermediate 2D + 6D
8. Advanced 6D
9. compressed/readiness horizons
10. >16 runway
11. rolling long-horizon
```

This sequence maximizes reuse and postpones the two policy-poor frequency edges (2D and 6D).

---

# 9. Regression / acceptance philosophy

Every HM phase must preserve 10K behavior.

Required invariants:

```text
NO 10K numeric delta
NO 10K rollout delta
NO 10K calendar delta
NO 10K adaptation delta
NO 10K horizon delta
```

unless an explicit separate fix is approved.

Prefer:
- parameterization with distance-keyed policies;
- additive HM artifacts;
- shared generic engines.

Avoid:
- branching the whole pipeline on `if distance == HalfMarathon`;
- copied 10K classes renamed HM;
- per-cell generators.

---

# 10. First Claude task to execute now

## PHASE HM.0 — DISTANCE GENERALIZATION REUSE AUDIT
### REAL APPSEL .NET BACKEND — NO CODE

Objective:

Determine the minimum set of changes required to add Half Marathon as a new distance family while reusing the already-generalized Level × Frequency architecture.

Treat the current 10K matrix supplied above as frozen evidence that Level/Frequency generalization is already solved.

Inspect the real repository and classify every relevant current authority into:

```text
A. DISTANCE_GENERIC_REUSE
B. TEN_K_DISTANCE_POLICY
C. TEN_K_HARDCODE_REQUIRES_PARAMETERIZATION
D. HM_NEW_DISTANCE_AUTHORITY_REQUIRED
E. OPEN_HM_PRODUCT_DECISION
```

Mandatory audit areas:

1. Candidate identity and compatibility
2. RunLayout ownership
3. Level policy dispatch
4. Frequency policy dispatch
5. Core horizon classifier
6. Dynamic core allocation
7. Runway composition
8. Rolling long-horizon
9. Starting-volume resolution
10. Peak-volume resolution
11. Progression safety
12. Session allocation
13. Long-run resolution
14. Workout progression
15. Workout eligibility
16. Workout dose
17. Pace-source / goal-feasibility logic
18. Calendar materialization
19. Adaptation
20. Preview/confirm/persistence/public routing

For every 10K-specific hit, answer:

```text
Is the mechanism actually 10K-specific?
or
is only the selected policy data 10K-specific?
```

The preferred outcome is:

```text
generic engine
+
distance-keyed policy
```

not copied HM control flow.

Explicitly identify the exact repository changes required for the first target only:

```text
HALF_MARATHON
× INTERMEDIATE
× 4D
× 14-WEEK PREFERRED CORE
× DARK ONLY
```

Do NOT:
- write production code;
- add HM catalog artifacts;
- change public routing;
- modify the 10K matrix;
- decide any OPEN-HM values;
- copy 10K numeric policy to HM;
- reopen external research.

Required output:

`PHASE_HM_0_DISTANCE_GENERALIZATION_REUSE_AUDIT.md`

with:

1. Architecture map
2. Reusable components
3. 10K hardcodes
4. HM authorities required
5. Open HM product decisions
6. Minimal file-change forecast for HM Intermediate 4D 14W
7. Risks of accidental 10K regression
8. Recommended exact next implementation phase

Final classification:

```text
HM_DISTANCE_GENERALIZATION_READY_FOR_FIRST_DARK_VERTICAL_SLICE
```

only if the first HM slice can be implemented without a second parallel plan-generation engine.

Otherwise:

```text
HM_DISTANCE_GENERALIZATION_ARCHITECTURE_BLOCKED
```

and identify the blocking coupling precisely.

---

# 11. Key instruction to Claude

The goal is not "build 21K the same way 10K was built historically."

The goal is:

```text
Use the architecture that 10K generalization already paid for.
Add HM as a new distance authority.
```

If an implementation proposal creates a new class/service per Level × Frequency cell, stop and reconsider.

The desired long-term shape is data/policy expansion across a shared engine, with typed distance-specific authorities and cell-by-cell rollout.
