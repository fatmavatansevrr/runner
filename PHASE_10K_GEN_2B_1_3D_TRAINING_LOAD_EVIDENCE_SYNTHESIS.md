# Phase 10K-GEN.2B.1 — 3D Training-Load Evidence Synthesis

Status: research and decision preparation only. No production code, catalog, test, or canonical product value is changed by this document.

## 1. Scope and frozen assumptions

This synthesis covers `TEN_K / INTERMEDIATE / 3 running days / CORE_PATH / 8–14 weeks`, with the frozen weekly layout `KEY_SESSION + EASY_SUPPORT + LONG_RUN`, the canonical Foundation → Build → RaceSpecific → Taper phase model, and the existing one-KEY workout identities. Valid `RecentWeeklyVolumeKm > 0` remains the preferred starting evidence. The draft 3D peak band of 22–32 km/week is tested, not replaced.

The four open dimensions are fallback starting volume, progression controls, session allocation, and long-run controls. All numeric outputs below are candidate envelopes or decision options; none is an approved Appsel value.

## 2. Research methodology

Searches prioritized systematic reviews, prospective cohorts and original trials for load/injury questions; official coach, university or charity-hosted plans for practice; and the current repository for Appsel evidence. Claims were admitted only after separating: (A) scientific evidence, (B) established coaching practice, and (C) internal product evidence. Applicability is assessed against population, race context and frequency. Distances inferred from time-only plans were not converted to kilometres because pace would make the result runner-dependent.

This is a policy synthesis, not clinical advice. Injury research is used to constrain abrupt change, not to promise injury prevention or prescribe an alleged universal safe dose.

## 3. Source-quality table

| ID | Source | Type | Population / context | Frequency | Relevant claim | Directness | Limitations |
|---|---|---|---|---:|---|---|---|
| A1 | Damsted et al. (2018) | Systematic review, 4 studies | Adult runners, mixed ability/context | Mixed | Very limited evidence links sudden load changes to injury; >30% progression signal, but 10% vs 24% was not different | PARTIALLY_APPLICABLE | Sparse, heterogeneous studies; no exact 3D prescription |
| A2 | Fredette et al. (2022) | Systematic review, 36 prospective studies, 23,047 runners | Novice, recreational, competitive runners | Mixed | Associations for distance, duration, frequency, intensity and change were conflicting | PARTIALLY_APPLICABLE | Exposure definitions and injury outcomes heterogeneous |
| A3 | Nielsen et al. (2014) | Prospective cohort, novice runners | Novice runners | Mixed | No overall significant group difference; >30% over two weeks showed a non-significant signal for distance-related injuries | INDIRECT_CONTEXT_ONLY | Novice, not Intermediate; HR CI crossed 1 |
| A4 | Frandsen et al. (2025) | Prospective cohort, 5,205 adult runners / 588,071 sessions | Predominantly middle-aged, male recreational runners | Observed | A session >10% longer than the longest run in the prior 30 days was associated with higher overuse-injury rate; week-to-week ratio was not | PARTIALLY_APPLICABLE | Observational association; “10%” is a session-spike threshold in this design, not a weekly rule |
| A5 | Rosenblat et al. (2019) | Systematic review/meta-analysis of RCTs | Endurance-trained athletes | Mixed | Polarized distribution outperformed threshold distribution in a small evidence base | INDIRECT_CONTEXT_ONLY | Only four studies; trained athletes; does not specify session-distance shares |
| A6 | Kenneally et al. (2018) | Running-specific systematic review | Middle/long-distance runners | Mixed | Pyramidal/polarized organization generally outperformed threshold-heavy organization | PARTIALLY_APPLICABLE | Mostly not 3D 10K; zone methods vary |
| B1 | Hal Higdon Novice 10K | Established coach, official 8-week plan | Beginner or experienced runner wanting low mileage | 3 runs + 2 cross-training | Three-run 10K practice exists; first run 2.5 mi; longest 5.5 mi; conversational running | PARTIALLY_APPLICABLE | Not Intermediate and no weekly table exposed in the retrieved page |
| B2 | Furman FIRST | University-hosted coaching/research program | Varied road runners | 3 quality runs + 2 cross-training | Three-run racing plans exist, but all three runs are “quality”; cross-training is integral | PARTIALLY_APPLICABLE | Does not match frozen KEY+EASY+LONG; detailed 10K distances are book-gated |
| B3 | Shelter 10K Training Guide, Intermediate | Charity-hosted 8-week plan | Intermediate 10K | Usually 4–5 runs | Total-session quality examples include easy portions and warm-up/cool-down; long easy runs reach 60–70 min | PARTIALLY_APPLICABLE | Time-based and higher frequency; cannot yield kilometre shares without pace |
| C1 | `VolumeSafetyPolicy.Default` | Internal implementation contract | 10K/Intermediate/4D golden path | 4 | 7% preferred, 8% hard, 2.5 km absolute, 30–36% long preferred, 33% selected, 40% cap, 0.5 km rounding | INDIRECT_CONTEXT_ONLY | Explicitly derived from 4D fixture; ratio/caps informational in planner and verified downstream |
| C2 | `V1MissingReadinessStartingVolumePolicy` | Internal approved product default | 10K/Intermediate/4D | 4 | Missing=16 km; explicit zero=12 km | INDIRECT_CONTEXT_ONLY | Provenance explicitly 4D; not transferable by assumption |
| C3 | `V1FourDaySessionVolumeAllocationPolicy` and allocator | Internal implementation detail | 10K/Intermediate/4D | 4 | KEY minimum 3 km; each EASY minimum 1.5 km; residual split and 0.5 km rounding | INDIRECT_CONTEXT_ONLY | Two easies change the denominator and reconciliation behaviour |
| C4 | `peak-volume-bands.v3.json` / GEN.2B audit | Internal draft policy | 10K/Intermediate/3D | 3 | Candidate peak range 22–32 km/week | DIRECTLY_APPLICABLE | DRAFT, not canonical/published product authority |

Internal status classification: C1 combines `LEGACY/GOLDEN_FIXTURE` numeric provenance and `IMPLEMENTATION_DETAIL`; C2 is `APPROVED_PRODUCT_DEFAULT` for 4D only; C3 is `IMPLEMENTATION_DETAIL`; C4 is `DRAFT_POLICY`. None is a 3D `CANONICAL` value.

## 4. Atomic claim inventory

| Claim | Atomic claim | Result | Evidence class |
|---|---|---|---|
| CLAIM-001 | Established three-running-day 10K programs exist. | SUPPORTED | B1, B2 |
| CLAIM-002 | The exact frozen KEY+EASY+LONG Intermediate model is poorly represented in public plans. | SUPPORTED | B1–B3 comparison |
| CLAIM-003 | Science does not establish one universally safe weekly percentage increase. | SUPPORTED | A1–A3 |
| CLAIM-004 | The traditional weekly “10% rule” is not a proven universal injury-prevention law. | SUPPORTED | A1–A3 |
| CLAIM-005 | Single-session distance change may matter independently of weekly change. | SUPPORTED | A4 |
| CLAIM-006 | A4 proves every runner must be capped at exactly +10% per session. | CONTESTED | Observational design and reference definition prevent that inference |
| CLAIM-007 | Lower frequency concentrates weekly distance into fewer exposures. | SUPPORTED | Arithmetic, not causal science |
| CLAIM-008 | KEY total distance includes low-intensity warm-up, recovery and cool-down. | SUPPORTED | B3 and Appsel workout components |
| CLAIM-009 | Intensity distribution can be translated directly into KEY/EASY/LONG distance shares. | CONTESTED | A5–A6 measure intensity, not role allocation |
| CLAIM-010 | Science identifies a universal optimal long-run percentage. | INSUFFICIENT_EVIDENCE | A1–A4 |
| CLAIM-011 | Percentage-only long-run control ignores recent session history. | SUPPORTED | A4 plus arithmetic |
| CLAIM-012 | Missing load and explicit zero carry different readiness information. | SUPPORTED | Semantics and C2; magnitude remains a product decision |
| CLAIM-013 | The 22–32 km peak band can support three viable sessions under a bounded split. | SUPPORTED | Feasibility grid below |
| CLAIM-014 | Twelve km/week is feasible only near the lower edge of useful session sizes. | SUPPORTED | Feasibility grid below |
| CLAIM-015 | A 7–8% weekly envelope is evidence-compatible but not scientifically validated specifically for 3D. | SUPPORTED | A1–A4, C1 |
| CLAIM-016 | A 2.5 km absolute cap is a product guardrail, not a scientifically derived optimum. | SUPPORTED | Source absence and C1 provenance |

## 5. Starting-volume synthesis

### STARTING_VOLUME_EVIDENCE_ENVELOPE

Coaching practice provides only a loose anchor. Hal's novice plan expects the runner to tolerate a 2.5-mile (4.0 km) first workout and explicitly recommends building a base if that is too difficult; FIRST requires current-fitness tailoring and supplemental cross-training. These sources support screening/base assumptions, not an Intermediate fallback weekly kilometre value.

The stronger constraints are logical and internal:

- `MISSING` means measurement uncertainty, not demonstrated detraining. A candidate 14–18 km/week envelope preserves a useful 4–6 km mean session while remaining below the draft 22 km peak floor.
- `EXPLICIT ZERO` indicates no recent running evidence. A candidate 10–14 km/week envelope is distinguishable and conservative, but 10 km cannot satisfy the proposed 4 km KEY + 3 km EASY + approximately 4–5 km LONG minima simultaneously; therefore 12–14 km is the operationally feasible sub-envelope for direct entry into this frozen layout.
- If recent-longest-run evidence exists, a fallback weekly total must not manufacture a long run that violates the session-history safeguard. That conflict should lower/defer the layout or require readiness handling; it should not silently raise the recent-longest-run anchor.

Evidence strength is `EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT` for the bounds and `PRODUCT_DEFAULT` for the selected value. Uncertainty is high because no direct study randomizes Intermediate 3D 10K starting mileage, and accessible coaching plans mix populations, frequencies and cross-training.

## 6. Progression synthesis

### PROGRESSION_EVIDENCE_ENVELOPE

Weekly-load evidence supports avoiding abrupt increases but does not validate a single universal cap. A1 found very limited evidence and no difference between average 10% and 24% progressions in one comparison; A2 found the broader literature conflicting; A3's >30% signal was injury-type-specific and statistically uncertain. Thus an Appsel preferred band of roughly 4–7% and a review/hard-bound candidate band of 8–10% are conservative product choices, not scientific cut-points.

An absolute cap of roughly 2.0–2.5 km/week is useful at this low volume because percentage-only rules can round poorly and because the existing Appsel value is internally understood. No external evidence establishes either endpoint. It is therefore `PRODUCT_DEFAULT`.

Single-session control is separately justified. A4 supports comparing a proposed run with the longest run in the preceding 30 days. Candidate policy should flag, clamp or require an explicit exception when a run would exceed that anchor by more than approximately 10%; the exact operational response and handling of stale/missing history remain product decisions. This must not be described as the weekly 10% rule.

Three-day frequency makes dual control more, not less, relevant: a small weekly increase can be concentrated into one run. Existing 4D 7%/8%/2.5 km values are conservative and reusable as decision candidates, but not proven frequency-independent.

## 7. Session-allocation synthesis

### SESSION_ALLOCATION_EVIDENCE_ENVELOPE

Direct scientific percentage evidence is absent. The candidate envelope below is produced by coaching structure, Appsel workout composition, and whole-system feasibility:

| Role | Candidate total-session share | Candidate viable minimum | Basis / strength |
|---|---:|---:|---|
| KEY | 30–40% | 4.0 km | Enough total distance for warm-up + main set/recoveries + cool-down at low totals; `PRODUCT_DEFAULT` |
| EASY | 20–30% | 3.0 km | Protects the sole easy exposure from becoming rounding residue; `PRODUCT_DEFAULT` |
| LONG | 35–45% | 4.5–5.0 km at low totals | Frequency-concentration and feasibility envelope; detailed limits in §8 |

These ranges are coupled, not independently selectable: the chosen shares must sum to 100%, respect minima, and leave the KEY workout phase-appropriate. At 12 km, the practical solution space is narrow. At 22–32 km, it is broad.

Half-kilometre granularity remains understandable and is at most 4.2% of a 12 km week and 1.6% of a 32 km week. Minima must be enforced before rounding. Reconciliation should choose the eligible role whose adjustment minimizes target-share deviation; EASY may be the normal absorber, but must not be forced below its minimum. This is a candidate principle, not an implementation decision.

## 8. Long-run synthesis

### LONG_RUN_EVIDENCE_ENVELOPE

Low-frequency plans necessarily give each run a large weekly share. Hal's three-run novice structure reaches a 5.5-mile long workout, but the retrieved material does not expose every weekly distance needed for reliable weekly-share calculation. FIRST demonstrates a long run within three runs but makes all three sessions quality-oriented and adds cross-training. The Shelter Intermediate plan commonly uses 60–70 minute long/easy runs, but at four or more running days. These practices show why a 3D long-run share may exceed a 4D preferred share; they do not establish a universal percentage.

Candidate envelope:

- preferred share: 35–42%;
- selectable centre envelope for GEN.2B.2: 38–40%;
- hard-ceiling decision envelope: 42–45%;
- minimum meaningful distance: approximately 4.5–5.0 km at low-volume entry, rising by weekly-share arithmetic;
- recent-history safeguard: compare the candidate distance with the longest run in the preceding 30 days and flag/clamp/route for review around a >10% increase, subject to history quality.

The share values are `PRODUCT_DEFAULT`, bounded by feasibility rather than direct causal science. The session-history safeguard is `EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT`. A percentage-only rule is not defensible: it can approve a sharp session spike when weekly volume rises or when a user has no comparable recent long run. A multi-constraint model—weekly share, recent-longest-run relation, session minimum, weekly total and race/context ceiling—is better supported. It must define missing/stale-history behavior before implementation.

## 9. Intensity-distribution contextual findings

A5 and A6 favour training distributions with most work below the first threshold over threshold-heavy organization, with limitations. Appsel should therefore ensure that one KEY day does not make most weekly distance moderate/high. This is compatible with the frozen model because KEY total-session distance includes warm-up, recoveries and cool-down, while EASY and most LONG distance are low intensity.

No 80/20 value is imported. `INTENSITY_DISTRIBUTION != SESSION_DISTANCE_DISTRIBUTION`; distance assigned to KEY is not identical to quality-work distance.

## 10. Mathematical feasibility analysis

### 10.1 Representative allocation grid

The grid uses one stress-test point, 35% KEY / 25% EASY / 40% LONG, rounded to 0.5 km by largest-error reconciliation. It is not a selected policy.

| Weekly km | KEY km | EASY km | LONG km | Long share | Viability |
|---:|---:|---:|---:|---:|---|
| 12 | 4.0 | 3.0 | 5.0 | 41.7% | Feasible, boundary case |
| 14 | 5.0 | 3.5 | 5.5 | 39.3% | Feasible |
| 16 | 5.5 | 4.0 | 6.5 | 40.6% | Feasible |
| 18 | 6.5 | 4.5 | 7.0 | 38.9% | Feasible |
| 20 | 7.0 | 5.0 | 8.0 | 40.0% | Feasible |
| 22 | 7.5 | 5.5 | 9.0 | 40.9% | Feasible |
| 24 | 8.5 | 6.0 | 9.5 | 39.6% | Feasible |
| 26 | 9.0 | 6.5 | 10.5 | 40.4% | Feasible |
| 28 | 10.0 | 7.0 | 11.0 | 39.3% | Feasible |
| 30 | 10.5 | 7.5 | 12.0 | 40.0% | Feasible |
| 32 | 11.0 | 8.0 | 13.0 | 40.6% | Feasible; long run is 1.3× race distance, requiring product approval/context cap |

Every row reconciles exactly. The draft 22–32 km peak band is allocation-feasible. Feasibility does not itself prove performance optimality or session-history safety.

### 10.2 Core trajectory lower-bound test

The table shows the constant compound increase required if all weeks before the final peak were growth opportunities. It deliberately excludes taper and phase/cutback constraints, so it is an optimistic mathematical lower bound, not the actual Core schedule. The current Core has Taper reduction and no recurring recovery rule; actual generation must be simulated after values are approved.

| Trajectory | 8w (6 growth steps) | 10w (8) | 12w (10) | 14w (12) |
|---|---:|---:|---:|---:|
| 12 → 22 km | 10.6% | 7.9% | 6.3% | 5.2% |
| 16 → 27 km | 9.1% | 6.8% | 5.4% | 4.5% |
| 18 → 32 km | 10.1% | 7.5% | 5.9% | 4.9% |

Consequences: an 8-week low-start path cannot reach 22 km under a 7–8% envelope without more growth opportunities, an exception, or a lower peak. Ten-to-fourteen-week mid/higher trajectories are broadly compatible. Peak is a band, not a mandatory target: inability to reach its upper edge is not intrinsically a contradiction.

## 11. Contradiction tests

| Test | Result | Implication |
|---|---|---|
| LONG 5 + KEY 4 + EASY 3 > 12 | No; equals 12 | 12 km is feasible but has no allocation slack |
| Same minima at 10 km | Yes; total is 12 | Direct frozen-layout entry below 12 km is infeasible under these candidate minima |
| 22–32 peak produces impossible session sizes | No | All grid rows reconcile; workout-specific dosage still needs later validation |
| 12 → 22 in short Core under ≤8% | Yes for optimistic 8-week test | Do not force peak floor; readiness/path/target needs explicit behavior |
| 40% long share ensures recent-history safety | No | Percentage must be combined with session-history control |
| 0.5 km rounding always preserves share cap | No | Example: 12 km yields 5 km = 41.7%; validate after rounding |
| 32 km × 40% long is automatically suitable for 10K | No | 12.8≈13 km is feasible but needs race/context and recent-history ceiling decisions |

No contradiction invalidates the draft peak band. The material contradictions occur at low entry volume, short-horizon forced peak attainment, and percentage-only long-run logic.

## 12. Source-conflict table

| Topic | Source A | Source B | Conflict | Likely reason | Appsel implication |
|---|---|---|---|---|---|
| Weekly increase | A3 suggests concern above 30% | A1/A2 report limited/conflicting evidence | Precise safe threshold absent | Injury definitions, populations, exposure windows | Use conservative envelope; do not claim causal 10% law |
| Weekly vs session spike | Traditional weekly rule framing | A4 finds session ratio association and no week-to-week association | Unit of load differs | Fewer exposures can hide concentration | Maintain separate weekly and session controls |
| Three-run structure | B1 has mostly easy three-run beginner structure | B2 uses three quality runs plus cross-training | Same frequency, different stress | Population/philosophy and cross-training | Neither directly dictates frozen allocation |
| Long-run share | C1 prefers 30–36%, cap 40% | Low-frequency arithmetic often needs ~40% | 4D denominator vs 3D denominator | One fewer easy run | Treat C1 as 4D-specific candidate context |
| Intensity | A5/A6 favour low-intensity dominance | B2 describes three quality runs | Apparent concentration conflict | Cross-training and “quality” definitions | Do not use FIRST as direct KEY+EASY+LONG dosage authority |

## 13. Existing Appsel policy comparison

| Existing item | 3D transferability | Reason |
|---|---|---|
| Missing 16 / zero 12 km | PLAUSIBLE_BUT_NOT_PROVEN | Both fall in/near candidate envelopes; explicit 4D provenance |
| Preferred 7%, hard 8% | PLAUSIBLE_BUT_NOT_PROVEN | Conservative and compatible; no 3D-specific causal validation |
| Absolute 2.5 km | INSUFFICIENT_EVIDENCE | Useful guardrail but product-derived and relatively large at low totals |
| Long preferred 30–36%, selected 33%, cap 40% | LIKELY_4D_SPECIFIC | 3D concentration makes 33% difficult with KEY/EASY minima at low volume; 40% remains a plausible centre/cap candidate |
| 4D allocation/minima | LIKELY_4D_SPECIFIC | Two EASY sessions and residual splitting do not transfer; KEY 3/EASY 1.5 are weak context only |
| 0.5 km granularity | SUPPORTED_AS_FREQUENCY_INDEPENDENT | Operationally coherent if post-round validation and flexible reconciliation are retained |
| 3D peak 22–32 km | PLAUSIBLE_BUT_NOT_PROVEN | Mathematically viable throughout; DRAFT status and performance evidence remain unresolved |

## 14. Integrated 3D candidate policy envelope

This is one coupled envelope for GEN.2B.2, not four unrelated recommendations:

```text
Missing readiness candidate envelope: 14–18 km/week
Explicit-zero candidate envelope: 12–14 km/week

Preferred weekly progression envelope: 4–7%
Hard/review weekly envelope: 8–10%
Absolute weekly cap candidate envelope: 2.0–2.5 km
Single-session safeguard: compare with longest run in preceding 30 days;
  around >10% increase requires clamp/flag/explicit exception policy

KEY total-session share envelope: 30–40%; minimum candidate 4.0 km
EASY total-session share envelope: 20–30%; minimum candidate 3.0 km
LONG preferred envelope: 35–42%
LONG selected-share decision envelope: 38–40%
LONG hard-ceiling decision envelope: 42–45%

All selected shares must sum to 100%; apply minima before rounding;
round to 0.5 km; reconcile to the eligible role that minimizes deviation;
revalidate total, minima, share ceiling and recent-longest-run safeguard.
```

The integrated envelope implies a 12 km operational floor for this direct three-session prescription. It does not imply that an explicitly-zero user is clinically ready for a KEY workout; entry eligibility remains a separate frozen/readiness authority.

## 15. GEN.2B.2 product-decision options

### D1 — Missing / zero starting volume

| Option | Values | Evidence / advantages | Risks |
|---|---|---|---|
| A Conservative | missing 14–16; zero 12 | Low session loads; preserves semantic distinction | Short Core may not reach 22 without exceeding progression envelope |
| B Balanced — best-supported candidate | missing 16; zero 12–14 | Aligns internal defaults and feasibility floor | Still product-derived; zero may be too aggressive without base screening |
| C Higher-entry | missing 18; zero 14 | Better peak reachability and session viability | Greater unsupported assumed readiness |

### D2 — Progression

| Option | Values | Evidence / advantages | Risks |
|---|---|---|---|
| A Conservative | preferred 4–6%, hard 8%, 2.0 km plus session-history guard | Strongest caution | More short-horizon under-reach |
| B Balanced — best-supported candidate | preferred 4–7%, hard 8%, 2.0–2.5 km plus guard | Reuses understood Appsel envelope while adding session control | 8% is not scientifically proven |
| C Flexible | preferred 5–8%, review at 10%, 2.5 km plus strict session guard | Improves short-horizon reach | Higher load change and greater exception complexity |

### D3 — Session allocation

| Option | Target family | Evidence / advantages | Risks |
|---|---|---|---|
| A Easy-protective | KEY 30–35, EASY 27–30, LONG 38–40 | Preserves sole easy run | Some KEY workouts may not fit at 12–14 km |
| B Balanced — best-supported candidate | KEY 33–37, EASY 23–27, LONG 38–42 | Matches feasibility grid and role purposes | Narrow low-volume rounding space |
| C KEY-protective | KEY 37–40, EASY 20–23, LONG 38–40 | Supports total workout distance | Easy can become product-poor; intensity concentration risk |

Every chosen triple must reconcile to 100%; displayed ranges are option families, not permission to select arbitrary non-summing endpoints.

### D4 — Long run

| Option | Values | Evidence / advantages | Risks |
|---|---|---|---|
| A Conservative | preferred 35–38, selected candidate 38, cap 42 + history guard | Lowest concentration | Less long-run exposure; can enlarge KEY/EASY |
| B Balanced — best-supported candidate | preferred 38–42, selected candidate 40, cap 42–45 + history guard | Works across grid and reflects 3D denominator | Cap endpoint remains product judgment |
| C Phase-flexible | selected band 38–42 by phase, cap 45 + history/race constraints | Responds to phase and low-frequency reality | More policy complexity and testing burden |

## 16. Remaining evidence gaps

No public source located directly studies the exact Intermediate 10K KEY+EASY+LONG three-day allocation, minimum useful easy-run distance, or an optimal long-run weekly share. Accessible established plans often are beginner, use more days, are time-based, require cross-training, or place all three runs under a quality label. No causal evidence supports Appsel's exact absolute-km cap or rounding rule. A direct audit of licensed/book-only Daniels, Pfitzinger, McMillan and FIRST 10K tables could refine coaching-practice distributions but is not necessary to bound GEN.2B.2 honestly.

Implementation must later simulate the actual 8–14 phase allocation, workout-specific component minima, taper and post-round single-session changes for every selected option. That is validation after product choice, not a reason to invent evidence here.

## 17. Sources and references

Scientific:

1. Damsted C, et al. “Is there evidence for an association between changes in training load and running-related injuries?” *International Journal of Sports Physical Therapy* (2018). https://pubmed.ncbi.nlm.nih.gov/30534459/
2. Fredette A, et al. “The Association Between Running Injuries and Training Parameters: A Systematic Review.” *Journal of Athletic Training* (2022). https://pubmed.ncbi.nlm.nih.gov/34478518/
3. Nielsen RO, et al. “Excessive progression in weekly running distance and risk of running-related injuries.” *JOSPT* (2014). https://pubmed.ncbi.nlm.nih.gov/25155475/
4. Frandsen J, et al. “How much running is too much? Identifying high-risk running sessions in a 5200-person cohort study.” *British Journal of Sports Medicine* (2025). https://bjsm.bmj.com/content/59/17/1203
5. Rosenblat MA, et al. “Polarized vs. Threshold Training Intensity Distribution…” *Journal of Strength and Conditioning Research* (2019). https://pubmed.ncbi.nlm.nih.gov/29863593/
6. Kenneally M, et al. “The Effect of Periodization and Training Intensity Distribution on Middle- and Long-Distance Running Performance.” *IJSPP* (2018). https://pubmed.ncbi.nlm.nih.gov/29182410/

Established coaching/practice:

7. Hal Higdon, Novice 10K Training Program. https://www.halhigdon.com/training-programs/10k-training/novice-10k/
8. Furman Institute of Running and Scientific Training, Services and Training Programs. https://www.furman.edu/first/services-training-programs/
9. Shelter, 10K Training Guide (Intermediate table included). https://assets.ctfassets.net/6sxvmndnpn0s/7ssoCff6tg4g3ySvFBNUsn/864b080219487723b7b2549a7b6bddac/10k_Training_Plan.pdf

Internal repository evidence:

10. `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs`
11. `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/V1MissingReadinessStartingVolumePolicy.cs`
12. `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1FourDaySessionVolumeAllocationPolicy.cs`
13. `plan-catalog/docs/canonical/golden-fixture-v3/progression_rules_v2.yaml`
14. `PHASE_10K_GEN_2B_3D_GENERALIZATION_POLICY.md`

## 18. Final classification

```text
10K_GEN_2B_1_3D_TRAINING_LOAD_EVIDENCE_READY_FOR_PRODUCT_DECISION
```

The evidence does not yield canonical numbers, but it is sufficient to bound defensible options for all four decisions. GEN.2B.2 may proceed, provided it records the chosen values as product decisions, preserves weekly/session-load distinction, and does not relabel the candidate percentages as scientific optima.
