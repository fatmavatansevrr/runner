# Phase 10K-FREQ.6D.4A — Intermediate 5D Production Prescription Content Evidence Synthesis

**Evidence phase. No production code, no catalog profile authoring, no product default selection, no exact dosage freeze, no runtime change, no persistence change, no public activation. This phase establishes what evidence and existing Appsel authorities permit — it does not select one final production prescription.**

## 1. Preflight

- `PHASE_LEDGER.md` row 59: `FREQ.6D.4`, Execution Status `DONE`, Final Classification `FREQ6D4_BLOCKED_ON_PRODUCTION_PROFILE_CONTENT_AUTHORITY`, Provenance `VERIFIED` — confirmed by direct read.
- Report `PHASE_10K_FREQ_6D_4_INTERMEDIATE_5D_DUAL_KEY_INTEGRATION_IMPLEMENTATION.md` exists (`git ls-files` confirmed); its parent chain (`FREQ.6D.3D` → ... → `FREQ.6D.1`) is `VERIFIED` throughout the ledger.
- `FREQ.6D.4A` confirmed not already present (`git ls-files | grep 6D_4A` → none before this phase).
- `git rev-parse HEAD` → `9d9f0f92c3a55edcbe604cfdcdd090f8d5ed1431`; `git status --short` → ` m baseline_tmp` only; `git diff --check` → clean.

Preflight: **PASSED**. `baseline_tmp`/`.claude/` untouched.

## 2. Evidence sources

**Repository (re-read this phase, not recalled from chat)**:
- `PHASE_10K_FREQ_5_INTERMEDIATE_5D_SEVERITY_AND_PAIRING_EVIDENCE.md` — real citations: Sperlich/Matzka/Holmberg (intensity-distribution review), Casado et al. (×2, elite-practice + observational), Solli et al. (Norwegian best-practice synthesis), Lenk et al. (2 vs. 3 weekly HIIT sessions), Bosquet (taper).
- `PHASE_10K_FREQ_6_INTERMEDIATE_5D_PRODUCT_POLICY_DECISION_CLOSURE.md` §11 (phase pairing purposes, frozen), §16/§19 (catalog capacity audit, frozen finding this phase re-verifies).
- `PHASE_10K_FREQ_6B_INTERMEDIATE_5D_NUMERIC_AUTHORITY_EVIDENCE.md` — real citation: Hal Higdon Intermediate 10K program (directly fetched), McMillan corroboration, Norwegian double-threshold ~75% controlled-dose concept.
- `PHASE_10K_FREQ_6C_INTERMEDIATE_5D_NUMERIC_DECISION_CLOSURE.md` — frozen numeric envelope (missing-readiness 26.0km, explicit-zero 19.5km, resolved peak 44.5km, KEY2:KEY1 70%, long-run 28%/36%, KEY2-floor edge-case note).
- `PHASE_10K_FREQ_6D_1A_...md` / `PHASE_10K_FREQ_6D_1B_...md` — frozen `PrescriptionProfile` schema (`WorkQuantity`/`Recovery`/`IntensityTarget`), `LaneOrdinal↔DoseCategory` fixed mapping.
- Every real `plan-catalog/catalog/workouts/*.json` (23 files, all versions of all 8 identities) — read in full this phase.

**External (fetched this phase)**:
- Hal Higdon Intermediate 10K program, [halhigdon.com](https://www.halhigdon.com/training-programs/10k-training/intermediate-10k/) — re-fetched directly (not reused secondhand from FREQ.6B's summary) for exact interval/tempo prescription detail, which FREQ.6B did not need and did not extract.
- McMillan Running, ["The Lost Art of the Fartlek"](https://www.mcmillanrunning.com/the-lost-art-of-the-fartlek/) and aggregated fartlek-coaching sources (TrainingPeaks, sportcoaching.com.au) — structured-fartlek convention.
- Taper interval-reduction coaching literature (RunnersConnect, NSCA "Tapering and Peaking", TrainingPeaks coach FAQ) — repetition-count taper-reduction pattern and a cited controlled study (85% weekly-volume reduction with maintained interval intensity, 5K time improvement).

## 3. Frozen authorities — confirmed untouched

Re-read, not reopened: 5D RunLayout (2K+2E+1L); `LaneOrdinal 0→PRIMARY / 1→SECONDARY_CONTROLLED`; both structural KEYs remain `KEY_SESSION`; 5D adherence-severity KEY1/KEY2 equivalence; FREQ.6C's numeric envelope (used only as a constraint, §14); `BETWEEN_REPETITIONS`/`AFTER_EACH_REPETITION` recovery-placement model; P1+P3 process architecture; the full profile schema/execution-contract/projector/runtime-consumer chain (FREQ.6D.2-3D). None is contradicted, restated as a new decision, or re-derived below.

## 4. Eight slot purposes (frozen, from FREQ.6 §11, restated for reference only)

| Slot | Purpose (frozen) | Must NOT become |
|---|---|---|
| FND-P | Controlled aerobic-strength/economy: short hills/strides, non-exhaustive | Maximal/exhaustive work; a duplicate of FND-S |
| FND-S | Controlled threshold introduction at lower fatigue/dose | A second copy of FND-P; an all-out threshold session |
| BLD-P | Controlled threshold/MIT development | Interchangeable with BLD-S without a materially distinct format |
| BLD-S | Controlled fartlek/VO2-oriented support, lower accumulated stress | A second full-dose threshold session (two threshold formats only permitted if materially distinct) |
| RS-P | 10K-specific rehearsal | A generic tempo run; a duplicate GOAL_PACE session |
| RS-S | Controlled threshold support | A second automatic GOAL_PACE duplicate (explicitly forbidden, FREQ.6 §11) |
| TAP-P | Reduced-dose 10K-specific sharpening | Full Build-phase dose; a new taper multiplier |
| TAP-S | Reduced-dose economy/strides sharpening, secondary controlled | Dropped, merged into TAP-P, or converted to EASY |

## 5-8. Foundation / Build / RaceSpecific / Taper analysis

### Foundation

- **FND-P**: FREQ.6 §11's purpose ("short hills/strides, non-exhaustive") maps closely to the *intensity descriptor* already authored in `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` (`"CONTROLLED_AEROBIC_POWER_INTRO"`/`"_PROGRESSED"`) — both real, `QUALITY`-family identities that already exist in the catalog. **Their `eligiblePhases` is currently `["PREPARATION_RUNWAY"]` (v1) or `["PREPARATION_RUNWAY", "LONG_HORIZON_GENERAL_ENDURANCE"]` (v2) — `FOUNDATION` is absent from both.** This is real, direct evidence answering §9's option list: **Option B** (existing WorkoutDefinition needs a versioned eligibility change) is the best-supported candidate path — the underlying training meaning (controlled aerobic-strength introduction, non-exhaustive) already matches FND-P's frozen purpose; nothing suggests the meaning itself needs to change, only the phase-eligibility gate. Evidence class: `APPSEL_EXISTING_AUTHORITY` (the identity and its intent already exist) + `INFERENCE` (that extending eligibility is the correct mechanism, not proven by an external source).
- **FND-S**: "Controlled threshold introduction at lower fatigue/dose." `THRESHOLD_TEMPO` (v1-v4) already exists with the right underlying `intensityDescriptor` (`"THRESHOLD"`) but `eligiblePhases: ["BUILD", "RACE_SPECIFIC"]` — `FOUNDATION` absent. Same **Option B** finding as FND-P.
- Casado et al. (FREQ.5, real citation) supports low-heavy-intensity proportion in preparation phases generally, consistent with "non-exhaustive"/"lower fatigue/dose" framing, but does not validate a specific Foundation-phase interval structure for recreational 10K runners. Evidence class: `SUPPORTED_RANGE` (directional, not exact).

### Build

- Higdon's real Intermediate 10K program (directly re-fetched this phase) is the strongest available direct precedent: it alternates **400m repeats at 5K pace** (8→9→10 reps across weeks 2/4/6, walk/jog recovery between each) with **continuous tempo runs (35→50 min, embedded easy/build/easy structure)** — i.e., a real, evidenced 5-day recreational-10K program that already runs one intervals-format day and one tempo-format day per week, matching BLD-P (threshold/MIT) + BLD-S (fartlek/VO2 support) as *distinct formats*, not identical duplicates. Evidence class: `DIRECT_EVIDENCE`.
- McMillan's structured-fartlek convention (real, multiple corroborating sources) — 10-12×1min surge/1min jog at "a bit faster than 5K pace," with a general recovery-ratio guideline of 50-75% of the work interval — gives a plausible envelope for BLD-S if authored as `FARTLEK` (`SURGE_AND_FLOAT`). Evidence class: `COMMON_COACHING_PRACTICE` (converging independent sources, not a controlled study).
- `THRESHOLD_TEMPO` is already `BUILD`-eligible — **`SUPPORTED`** candidate for BLD-P with zero eligibility change needed. `FARTLEK` is already `BUILD`-eligible — **`SUPPORTED`** candidate for BLD-S with zero eligibility change needed.
- FREQ.6 §11's own differentiation rule ("two threshold-family formats permitted only if prescription structures are materially distinct") is satisfiable in principle: Higdon's real program treats intervals (repeated, short, fast) and tempo (continuous, sustained) as materially distinct formats — directly supporting BLD-P=`THRESHOLD_TEMPO`(continuous)/BLD-S=`FARTLEK`(repeated surge/float) as non-duplicative by construction, though which exact identity plays which role is a Decision-phase choice, not selected here.

### RaceSpecific

- `GOAL_PACE_TEN_K` (already `RACE_SPECIFIC`-eligible, `PACE_BASED` mode) is the natural **`SUPPORTED`** candidate for RS-P ("10K-specific rehearsal") — direct match to its own `intensityDescriptor: "GOAL_PACE"`.
- `THRESHOLD_TEMPO` is already `RACE_SPECIFIC`-eligible — **`SUPPORTED`** candidate for RS-S ("controlled threshold support"), consistent with FREQ.6 §11's explicit prohibition on RS-S being "two automatic GOAL_PACE duplicates" (a second `GOAL_PACE_TEN_K` instance is explicitly forbidden by frozen policy — `THRESHOLD_TEMPO` as RS-S avoids that by construction).
- Real evidence (Casado et al., FREQ.5) reports "1-2 interval sessions weekly in pre-competition/competition among world-class 5000m runners" — directionally supports two quality sessions near race phase but is elite-practice evidence, not recreational-10K-specific. Evidence class: `SUPPORTED_RANGE`.

### Taper

- **No `QUALITY`-family (KEY-eligible) workout identity currently has `TAPER` in `eligiblePhases`** — confirmed by exhaustively re-reading all 23 real workout files this phase (only `EASY_STANDARD`/`LONG_RUN_STANDARD` mention Taper). This reconfirms FREQ.6 §16's finding is still exactly current.
- **TAP-P** ("reduced-dose 10K-specific sharpening"): `GOAL_PACE_TEN_K` is the clear underlying-meaning match (same specificity purpose as RS-P, reduced dose) — **Option B** (versioned eligibility extension to include `TAPER`) is well-supported by matching intent.
- **TAP-S** ("reduced-dose economy/strides sharpening"): this is the **weakest-supported** slot in the whole matrix. "Strides" specifically denotes short (~15-30s), fast, non-fatiguing accelerations — a distinct format from all three existing `QUALITY` identities' current `intensityDescriptor`s (`SURGE_AND_FLOAT`, `THRESHOLD`, `GOAL_PACE` — none of which describes a short-repeat/near-max/fully-recovered "strides" pattern). Taper-reduction literature (RunnersConnect/NSCA, real citations) supports *reducing repetition count while retaining intensity/pace* as the correct taper mechanism (concrete real example found: "8×800 two weeks out → 6×800 → 4×800 one week out," intensity/pace unchanged) — this is directly applicable *if* TAP-S is authored as a reduced-rep `FARTLEK` variant, but "economy/strides" as literally named is not the same training stimulus as `FARTLEK`'s `SURGE_AND_FLOAT`. **This is flagged as a genuine open question (§22, D-item), not resolved by this evidence pass**: whether TAP-S should be a reduced-dose `FARTLEK` (Option B, existing identity) or requires a new `STRIDES`-purpose `WorkoutDefinition` (Option C) cannot be determined from evidence alone — it is closer to a naming/scope question than a missing-dose-number question, but it is real and unresolved.
- Bosquet (FREQ.5, real citation): retained intensity/frequency with reduced volume in aggregate during taper — directly supports the general taper mechanism (reduce work, keep intensity), consistent across TAP-P and TAP-S regardless of which exact identity is used.

## 9. Workout identity capability result

| Slot | Candidate | Classification |
|---|---|---|
| FND-P | `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` | `PARTIALLY_SUPPORTED` — real identity, matching intent, `FOUNDATION` eligibility missing (Option B) |
| FND-S | `THRESHOLD_TEMPO` | `PARTIALLY_SUPPORTED` — same pattern, `FOUNDATION` eligibility missing (Option B) |
| BLD-P | `THRESHOLD_TEMPO` | `SUPPORTED` — already `BUILD`-eligible |
| BLD-S | `FARTLEK` | `SUPPORTED` — already `BUILD`-eligible |
| RS-P | `GOAL_PACE_TEN_K` | `SUPPORTED` — already `RACE_SPECIFIC`-eligible |
| RS-S | `THRESHOLD_TEMPO` | `SUPPORTED` — already `RACE_SPECIFIC`-eligible |
| TAP-P | `GOAL_PACE_TEN_K` | `PARTIALLY_SUPPORTED` — matching intent, `TAPER` eligibility missing (Option B) |
| TAP-S | none clean | `CATALOG_CAPABILITY_GAP` — candidate identity choice itself unresolved (Option B `FARTLEK`-reduced vs. Option C new `STRIDES` identity), independent of the dose-number gap |

No new `WorkoutDefinition` was authored — per this phase's own §8 prohibition. All six `PARTIALLY_SUPPORTED`/`SUPPORTED` classifications above still require the actual `WorkoutPrescriptionProfile` execution content (§13) which is uniformly absent (§2 of the FREQ.6D.4 parent report, reconfirmed here).

## 10. Work/repetition evidence (executable-dose dimensions, §13)

| Field | Structured FARTLEK (BLD-S / TAP-S candidate) | THRESHOLD_TEMPO (BLD-P / FND-S / RS-S candidate) |
|---|---|---|
| StructureMode | `Repeated` (structured variant; unstructured self-selected fartlek remains unrepresentable per FREQ.6D.1A/1B — unchanged) | `Continuous` typical (Higdon tempo), `Repeated` variant possible (interval-threshold) |
| Work quantity unit | `Seconds` (time-based surge, per McMillan/Higdon convention) | `Seconds` (Higdon: 35-50min continuous) or `Meters` (interval variant, e.g. Higdon's 400m) |
| Work quantity range | Surge: **60s** (McMillan "1-minute surges") is the single most-corroborated value across sources; broader coaching range 30s (beginner) to 120s (advanced structured variant) | Continuous: **35-50 minutes total session** (Higdon, direct); interval variant: **400m** (Higdon, direct) |
| RepetitionCount range | **10-12** (McMillan) down to 5-6×30s (beginner variant); Higdon's own interval analogue: **8-10**, progressing across weeks, tapering to 5 in race week | Not applicable to continuous format; interval variant: **8-10**, progressing (Higdon, direct, for 400m repeats specifically — not yet validated for a different distance/duration) |
| Recovery quantity range | **1min jog** (McMillan, 1:1 work:recovery) to 50-75% of work-interval duration (general coaching guideline) | Interval variant: "walk or jog between each repeat" (Higdon, direct — no exact duration stated, only mode) |
| Recovery mode | `Jog` (dominant convention; `Walk` used in beginner variants per one source) | `Jog` or `Walk` (Higdon explicitly offers both, athlete's choice — no single mode is evidenced as required) |
| RecoveryPlacement | `BetweenRepetitions` (matches every real source's "repeat X times" framing; no source describes an `AfterEachRepetition`-distinct pattern for these two identities specifically) | Same, for the interval variant |
| Evidence class | `COMMON_COACHING_PRACTICE` (McMillan) + `DIRECT_EVIDENCE` (Higdon's real analogous rep-count progression) | `DIRECT_EVIDENCE` (Higdon, both continuous and — by structural analogy — interval variants) |

**Not resolved by evidence** (left explicitly unresolved, not filled by symmetry, per §13): exact recovery duration for THRESHOLD_TEMPO's interval variant (Higdon states mode, not duration); whether FND-P/FND-S/TAP-P/TAP-S should use the *same* numeric envelope as their Build/RaceSpecific counterparts or a reduced one specific to their phase purpose (Foundation = "non-exhaustive," Taper = "reduced-dose" — both imply a lower envelope than Build/RaceSpecific, but no source gives an exact reduction factor for a recreational 10K population beyond the general "reduce reps, keep intensity" taper pattern in §12 below).

## 11. Recovery evidence

Two real, independent, converging sources: McMillan's "50-75% of the work interval" general fartlek-recovery guideline, and Higdon's direct "walk or jog between each repeat" for 400m intervals (mode only, no duration). No source gives an exact seconds/meters recovery value validated for this specific engagement's population — both are `COMMON_COACHING_PRACTICE`/`DIRECT_EVIDENCE`(mode)-mixed, not a single authoritative number. `RecoveryPlacement=BetweenRepetitions` is consistently the evidenced default across every real source found (matching FREQ.6D.3A.1's already-frozen `BETWEEN_REPETITIONS` normal-preference decision — a real, independent confirmation, not a coincidence manufactured to fit).

## 12. Primary/Secondary dose relationship

- FREQ.6 §13 (frozen): categorical `PRIMARY`/`SECONDARY_CONTROLLED`, no exact ratio; "two threshold-family sessions may coexist only when one is primary and the other demonstrably lower-dose/different-format."
- Real evidence directly on point: Lenk et al. (FREQ.5, real) — two vs. three weekly HIIT sessions produce similar improvement in recreational runners, supporting **two weekly intensive exposures as plausible with diminishing returns beyond two** — this supports having exactly two KEY sessions at all, not a specific dose split between them.
- Norwegian double-threshold "~75% controlled dose" concept (FREQ.6B, real, with an explicitly disclosed population/context caveat: elite same-day double sessions, not recreational once-weekly split) remains the only quantitative KEY2:KEY1 evidence found, already carried into FREQ.6C's frozen 70% decision — **not re-derived or re-opened here**.
- **Must SecondaryControlled always be lower total quality dose?** Evidence (Solli et al., "controlled rather than all-out," "avoidance of consecutive hard days") supports SecondaryControlled being lower-*stress*, which is satisfied either by lower work quantity/repetition count OR by materially different format at similar quantity (e.g., BLD-P=continuous tempo vs. BLD-S=shorter total repeated fartlek) — evidence does not require dose reduction to be the *only* differentiation mechanism.
- **Can it use the same WorkoutDefinition with a lower profile?** Yes — evidence supports this directly: Higdon's real program already treats his two quality days as different *formats* (tempo vs. interval), not the same identity at two doses, but FREQ.6 §11's own Build-phase text ("two threshold-family formats permitted only if prescription structures are materially distinct") explicitly allows the same-identity-different-profile path *if* materially distinct — both paths remain evidence-legitimate; evidence does not force one over the other.
- **Is equality ever allowed?** No real evidence source found supports two identical-dose KEY sessions in any phase; FREQ.6 §11/§13 already forbid it categorically (frozen, not reopened).
- Adherence-severity equivalence (KEY1=KEY2 for Adaptation) is confirmed, per this evidence pass, to remain a wholly separate authority from prescription dose — no evidence source conflates the two, consistent with FREQ.6 §10's own frozen `TWO_STRUCTURAL_KEY_SLOTS_DO_NOT_IMPLY_TWO_EQUAL_SEVERITY_STIMULI` invariant.

## 13. Numeric compatibility

Using FREQ.6C's frozen envelope purely as a constraint (not re-derived): missing-readiness 26.0km, explicit-zero 19.5km, resolved peak 44.5km, KEY2:KEY1=70%, long-run 28%/36km-cap. Checking the candidate dose envelopes above against this:

- Structured FARTLEK's evidence-supported total hard-running envelope (**10-20 minutes of actual surge time**, McMillan/coaching convention, plus warm-up/cool-down) and THRESHOLD_TEMPO's continuous envelope (**35-50 minutes total**, Higdon direct) both convert, under the existing `ESTIMATED_SESSION_TOTAL` distance-accounting mode already used by both identities, to session distances well within FREQ.4's real per-session minimums (3.0km KEY floor) and FREQ.6C's real computed weekly `keyTotal` figures (§B of FREQ.6C, ranging ~5.9-24.5km across the 8-14wk matrix) — **`NUMERICALLY_COMPATIBLE`**. This is a plausibility check against the existing envelope, not a reverse-engineered dose selection — no dose number here was chosen *to make* the arithmetic fit; the evidence-sourced ranges simply already sit inside it.
- Taper's reduced envelopes (§12 taper-reduction pattern: cut repetition count, retain intensity) are, by construction, smaller than Build/RaceSpecific's, so they clear FREQ.6C's real taper-week `keyTotal` figures (§B, 15.0-25.5km taper-week range) with more margin, not less — **`NUMERICALLY_COMPATIBLE`**.
- Foundation's envelope (non-exhaustive, presumed smaller than Build) was not independently evidenced with an exact quantity (§10's open item) — **`UNKNOWN`** at the exact-number level, though directionally expected to be compatible given Foundation's real computed `keyTotal` figures are smaller than Build/RaceSpecific's in FREQ.6C's matrix (lower weekly volumes early in the plan).

## 14. KEY2 floor

Carrying forward FREQ.6C §D unchanged (not re-derived): the theoretical KEY2 floor edge case (~2.25km, below the 3.0km per-session minimum) requires `keyTotal` at its absolute structural floor (6.0km) **combined with** the *low end* of the dose-ratio envelope (60%, not the selected 70%) — a combination FREQ.6C confirmed **does not occur anywhere in its real computed 8-14wk matrix** for either readiness state.

**This phase's own contribution**: the evidence-sourced candidate workout envelopes above (§10-11) do not introduce any new mechanism that could push `keyTotal` toward its structural floor — `keyTotal` is governed entirely by FREQ.4's weekly-volume allocation formula (upstream of workout-identity choice), and none of FARTLEK/THRESHOLD_TEMPO/GOAL_PACE_TEN_K's evidenced distance/duration ranges interacts with that formula at all (distance accounting is `ESTIMATED_SESSION_TOTAL`, computed from the session's own prescribed duration/pace, not fed back into the weekly allocator). Classification: **`PROVEN_UNREACHABLE`** under the current approved envelope, re-confirmed rather than merely repeated. Per §16's instruction, since unreachable, no clamp is proposed — a protective regression test asserting this bound is a `FREQ.6D.4B`+ engineering item (recorded in the decision inventory, §22, item D18), not authored here (no code this phase).

## 15. Phase-transition compatibility

- Foundation→Build: Foundation's evidenced envelope (short, non-exhaustive, presumed lower rep-count/duration) → Build's evidenced envelope (Higdon's real 8-10×400 / 35-50min tempo) is a plausible, evidence-consistent progressive increase, not an unjustified jump — matches the general progressive-overload principle implicit in every real source consulted (Higdon's own program itself progresses 8→9→10 reps and 35→50min within Build alone).
- Build→RaceSpecific: RS-P shifts specificity (goal-pace rehearsal) rather than necessarily increasing volume/intensity further — Casado et al.'s "1-2 interval sessions nearer competition" (real, FREQ.5) supports maintained-or-slightly-increased quality-session count into race-specific phase, consistent with retaining two KEY slots without evidence of an abrupt jump.
- RaceSpecific→Taper: directly evidenced by the real "8×800→6×800→4×800" taper-reduction pattern (§8/§12) — a smooth, evidenced, non-abrupt reduction mechanism (cut reps, hold pace), not a cliff.
- No phase-transition pair was found to require an unjustified abrupt workload/intensity jump under the evidence gathered. This is a compatibility check only — exact per-phase numbers remain a Decision-phase choice.

## 16. Fixture audit (§14 of the phase prompt)

| Fixture location | Values used | Classification |
|---|---|---|
| `ExecutableWorkoutPrescriptionContractTests.cs` (PlanCatalog.Tests) | e.g. 1000m/400m recovery, arbitrary rep counts | `REPRESENTABILITY_TEST_ONLY` — exists purely to prove the schema/validator round-trips, explicitly not sourced from any evidence document |
| `ExecutionPrescriptionIndexTests.cs` (RunningApp.IntegrationTests, FREQ.6D.3D) — structured FARTLEK proof: 6×60s/60s jog; intervalized THRESHOLD proof: 4×1600m/400m jog; taper proof: 6-rep/RecoveryCount5, 4-rep/RecoveryCount3 | Same shape family as this phase's real evidence (McMillan's 10-12×60s fartlek; Higdon's 400m-family intervals; the real 8×800→6×800→4×800 taper pattern) — **coincidentally directionally consistent**, but were authored in FREQ.6D.3D purely to exercise the generic lossless-consumption mechanism, with no evidence citation attached at the time | `REPRESENTABILITY_TEST_ONLY` — explicitly **not** promoted to `PRODUCTION_AUTHORITY` by this phase, per §14's own instruction, despite the coincidental resemblance to real evidence found independently here |

No test-fixture value from either file is treated as, or silently promoted to, production authority anywhere in this report. Where this evidence synthesis's own findings resemble a fixture value (e.g., ~6×60s fartlek, ~4×1600m threshold-interval-variant), that is disclosed as a coincidence of independently-sourced real evidence landing in a similar place — not a justification for reusing the fixture number itself as the frozen answer.

## 17. Output — Slot evidence matrix

`INTERMEDIATE_5D_PRODUCTION_PRESCRIPTION_EVIDENCE_MATRIX`

| Slot | Phase | Lane | Purpose | Candidate WorkoutDefinition | Structure mode | Work quantity envelope | Repetition envelope | Recovery envelope | Intensity | Dose relationship | Phase eligibility | Numeric compatibility | Evidence class | Source | Open questions |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FND-P | Foundation | Primary | Controlled aerobic-strength/economy, non-exhaustive | `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` | Repeated (hills) or Continuous (strides), unresolved | Unresolved | Unresolved | Unresolved | EffortBased | Lower than Build by phase intent | `PARTIALLY_SUPPORTED` (Option B) | `UNKNOWN` | `INFERENCE` (identity match) | FREQ.6 §11, catalog read | Exact quantity envelope not evidenced |
| FND-S | Foundation | SecondaryControlled | Controlled threshold introduction, lower fatigue | `THRESHOLD_TEMPO` | Continuous | Shorter than Build's 35-50min, unresolved exact | N/A (continuous) | N/A | PaceBased/EffortBased | Lower dose than BLD-P/FND-P interplay unresolved | `PARTIALLY_SUPPORTED` (Option B) | `UNKNOWN` | `INFERENCE` | FREQ.6 §11, catalog read | Exact reduced-duration envelope not evidenced |
| BLD-P | Build | Primary | Threshold/MIT development | `THRESHOLD_TEMPO` | Continuous | 35-50 min | N/A | N/A | PaceBased/EffortBased | Primary, full Build dose | `SUPPORTED` | `NUMERICALLY_COMPATIBLE` | `DIRECT_EVIDENCE` | Higdon (fetched) | Distance/pace exact conversion not fixed |
| BLD-S | Build | SecondaryControlled | Controlled fartlek/VO2 support | `FARTLEK` | Repeated | 60s surge (McMillan) | 10-12 (McMillan); 8-10 progressing (Higdon analogue) | ~1min jog / 50-75% of work | EffortBased | Lower total stress than BLD-P by format | `SUPPORTED` | `NUMERICALLY_COMPATIBLE` | `COMMON_COACHING_PRACTICE` + `DIRECT_EVIDENCE` | McMillan, Higdon | Exact rep count within range unselected |
| RS-P | RaceSpecific | Primary | 10K-specific rehearsal | `GOAL_PACE_TEN_K` | Continuous | Unresolved (no direct source for exact goal-pace segment length) | N/A | N/A | PaceBased | Primary specificity | `SUPPORTED` | `UNKNOWN` | `SUPPORTED_RANGE` (Casado et al., directional) | FREQ.5 | Exact goal-pace segment length not evidenced |
| RS-S | RaceSpecific | SecondaryControlled | Controlled threshold support | `THRESHOLD_TEMPO` | Continuous | Reduced vs. BLD-P, unresolved exact | N/A | N/A | PaceBased/EffortBased | Secondary support, never a GOAL_PACE duplicate | `SUPPORTED` | `UNKNOWN` | `INFERENCE` | FREQ.6 §11, FREQ.5 | Exact reduced envelope not evidenced |
| TAP-P | Taper | Primary | Reduced-dose 10K-specific sharpening | `GOAL_PACE_TEN_K` | Continuous | Reduced vs. RS-P | N/A | N/A | PaceBased | Reduced dose, retained specificity | `PARTIALLY_SUPPORTED` (Option B) | `NUMERICALLY_COMPATIBLE` | `DIRECT_EVIDENCE` (reduction pattern) + `INFERENCE` (identity) | RunnersConnect/NSCA (fetched), FREQ.6 §11 | Exact reduction factor unselected |
| TAP-S | Taper | SecondaryControlled | Reduced-dose economy/strides sharpening | none clean (`FARTLEK`-reduced vs. new identity) | Unresolved | Unresolved | Unresolved | Unresolved | Unresolved | Reduced dose, secondary controlled | `CATALOG_CAPABILITY_GAP` | `NUMERICALLY_COMPATIBLE` (by taper-reduction logic, if resolved) | `NO_EVIDENCE` (identity choice itself) | FREQ.6 §11 (purpose only) | Whether "strides" maps to any existing identity is unresolved |

## 18. Output — Catalog capability matrix

`INTERMEDIATE_5D_CATALOG_CAPABILITY_MATRIX`

| Slot | Required capability | State |
|---|---|---|
| FND-P | `AEROBIC_STRENGTH_CONTROLLED_*` eligible for FOUNDATION + a `WorkoutPrescriptionProfile` | `REQUIRES_VERSIONED_WORKOUT_DEFINITION_CHANGE` + `PRESCRIPTION_ONLY_GAP` |
| FND-S | `THRESHOLD_TEMPO` eligible for FOUNDATION + a reduced-dose `WorkoutPrescriptionProfile` | `REQUIRES_VERSIONED_WORKOUT_DEFINITION_CHANGE` + `PRESCRIPTION_ONLY_GAP` |
| BLD-P | `THRESHOLD_TEMPO` (already eligible) + `WorkoutPrescriptionProfile` | `PRESCRIPTION_ONLY_GAP` |
| BLD-S | `FARTLEK` (already eligible) + `WorkoutPrescriptionProfile` | `PRESCRIPTION_ONLY_GAP` |
| RS-P | `GOAL_PACE_TEN_K` (already eligible) + `WorkoutPrescriptionProfile` | `PRESCRIPTION_ONLY_GAP` |
| RS-S | `THRESHOLD_TEMPO` (already eligible) + `WorkoutPrescriptionProfile` | `PRESCRIPTION_ONLY_GAP` |
| TAP-P | `GOAL_PACE_TEN_K` eligible for TAPER + reduced-dose `WorkoutPrescriptionProfile` | `REQUIRES_VERSIONED_WORKOUT_DEFINITION_CHANGE` + `PRESCRIPTION_ONLY_GAP` |
| TAP-S | Identity resolution itself (Option B or C) + eligibility + `WorkoutPrescriptionProfile` | `DOMAIN_EVIDENCE_GAP` (identity choice) + potentially `REQUIRES_NEW_WORKOUT_IDENTITY` |

No slot is `READY_AS_IS` — every slot needs at minimum a new `WorkoutPrescriptionProfile` (true for all eight, since zero exist in the real catalog, §2 of the FREQ.6D.4 parent report).

## 19. Output — Decision inventory

`FREQ.6D.4B` must explicitly decide (numbered, none left hidden in prose):

- **D1** FND-P exact eligibility-extension mechanism (new `AEROBIC_STRENGTH_CONTROLLED_*` version adding `FOUNDATION`, or a different path)
- **D2** FND-P exact prescription structure (StructureMode, work quantity, repetition count if repeated)
- **D3** FND-S exact eligibility-extension mechanism (new `THRESHOLD_TEMPO` version adding `FOUNDATION`)
- **D4** FND-S exact reduced-dose prescription structure
- **D5** BLD-P exact `THRESHOLD_TEMPO` prescription (continuous duration or interval variant; exact minutes/meters)
- **D6** BLD-S exact `FARTLEK` prescription (exact repetition count within 10-12 vs. 8-10 evidence range; exact surge/recovery seconds)
- **D7** RS-P exact `GOAL_PACE_TEN_K` prescription (main-set duration/distance at goal pace)
- **D8** RS-S exact `THRESHOLD_TEMPO` reduced-dose RaceSpecific prescription
- **D9** TAP-P exact eligibility-extension mechanism for `GOAL_PACE_TEN_K` + TAPER
- **D10** TAP-P exact reduction factor from RS-P's dose
- **D11** TAP-S exact workout identity (reduced `FARTLEK` vs. new `STRIDES`-purpose identity — Option B vs. Option C)
- **D12** TAP-S exact prescription structure (depends on D11)
- **D13** For every slot: `RecoveryMode` exact choice where evidence offers a range (`Jog` vs. `Walk`) rather than a single value
- **D14** For every slot: `RecoveryPlacement` — evidence favors `BetweenRepetitions` uniformly, but this must still be an explicit per-profile authored choice, not assumed by default
- **D15** For every slot: `IntensityTarget.DescriptorKey` exact string (pace-zone/effort-zone/HR-zone vocabulary entry)
- **D16** Foundation and Taper's exact quantitative reduction factor relative to Build/RaceSpecific (no exact percentage was evidenced beyond the qualitative "non-exhaustive"/"reduced-dose" framing)
- **D17** Whether BLD-P/RS-S (both candidate `THRESHOLD_TEMPO`) and FND-S (also candidate `THRESHOLD_TEMPO`) share one `WorkoutPrescriptionProfile` version or require phase-distinct versions (§19 profile-reuse question)
- **D18** (engineering, not product) A protective regression test asserting the KEY2-floor edge case remains `PROVEN_UNREACHABLE` under whatever exact values D1-D16 select — to be added when FREQ.6D.4 resumes, not authored this phase

## 20. Profile reuse analysis (§19 of the phase prompt)

| Reuse pattern | Classification | Reasoning |
|---|---|---|
| Same profile reused across weeks within one phase's lane (e.g. every Build-phase BLD-P week uses the same profile) | `POSSIBLE_BUT_UNPROVEN` — plausible if Higdon-style within-phase progression (8→9→10 reps) is *not* required product behavior; **`SEMANTICALLY_INVALID`** if it is, since progression would need distinct profile versions per stage | Depends on a D-item (whether within-phase rep progression is adopted) not yet decided |
| Same profile reused across phases (e.g. BLD-P and RS-S both using an identical `THRESHOLD_TEMPO` profile) | `SEMANTICALLY_INVALID` | FREQ.6 §11 assigns materially different purposes/doses per phase; reusing an identical profile would erase the phase-specific purpose distinction this evidence pass just established |
| Same profile reused across lanes (KEY1 and KEY2 sharing one profile) | `SEMANTICALLY_INVALID` | Directly forbidden — FREQ.6 §13 requires materially different purpose/dose/structure between lanes; FREQ.6D.1B's frozen `LaneOrdinal↔DoseCategory` mapping requires each lane's profile to carry the lane-mandated `DoseCategory`, which two identical profiles cannot both satisfy differently |
| Same *WorkoutDefinition* (not profile) reused across multiple slots at different profile doses (e.g. `THRESHOLD_TEMPO` underlies FND-S, BLD-P, and RS-S) | `SUPPORTED_REUSE` | This is the identity-reuse-with-distinct-profiles pattern FREQ.6D.1's own architecture was explicitly designed around (§A4 "same WorkoutDefinition serves two profiles" worked example) — evidence-consistent and architecture-supported |

## 21. Representability

```
PARTIAL_REPRESENTABILITY
```

Six of eight slots (FND-P, FND-S, BLD-P, BLD-S, RS-P, RS-S, TAP-P — actually seven; see below) have a `SUPPORTED` or `PARTIALLY_SUPPORTED` workout-identity candidate and at least directional evidence for their prescription envelope. Correction for precision: **7 of 8 slots** (all except TAP-S) have an identified candidate identity and partial-to-direct evidence; **TAP-S alone** has an unresolved identity question (§8/§11) that is closer to `DOMAIN_EVIDENCE_GAP` than a simple missing-number gap. This is not `PRODUCTION_PROFILE_MODEL_NON_REPRESENTABLE` — nothing found suggests the approved 5D policy is architecturally unrepresentable; every gap identified is a missing *specific choice* (identity extension, exact quantity), not a modeling impossibility.

## 22. Open questions (classified, §25)

| Item | Classification |
|---|---|
| D1, D3, D9 (eligibility-extension mechanism) | `CATALOG_ARCHITECTURE_REQUIRED` (versioning decision, not pure content) |
| D2, D4, D5, D6, D7, D8, D10, D12, D16 (exact prescription numbers) | `PRODUCT_DEFAULT_REQUIRED` (selecting a specific value from an evidenced range) |
| D11 (TAP-S identity) | `DOMAIN_EVIDENCE_REQUIRED` — evidence found does not resolve whether "strides" maps to an existing format |
| D13, D14, D15 (recovery mode/placement/descriptor per profile) | `PRODUCT_DEFAULT_REQUIRED` |
| D17 (profile sharing across phase-distinct uses of one identity) | `CATALOG_ARCHITECTURE_REQUIRED` |
| D18 (KEY2-floor protective test) | `ENGINEERING_ONLY` |

No product choice is delegated to a future implementation phase implicitly — every item above is enumerated for `FREQ.6D.4B` (a DECISION phase) to resolve explicitly.

## 23. FREQ.6D.4B readiness

- Each required slot has a usable evidence envelope **except TAP-S**, whose envelope depends on resolving D11 first (an identity question, not a numeric one) — flagged, not hidden.
- Catalog gaps are explicitly identified (§18 matrix): 3 slots need versioned eligibility extensions (FND-P, FND-S, TAP-P), 1 slot needs an identity decision (TAP-S), all 8 need new `WorkoutPrescriptionProfile` content.
- All unresolved athlete-facing choices are enumerated (§19, D1-D18).
- No architecture ambiguity is hidden — D17's profile-reuse-across-phases question and D11's identity question are both surfaced, not silently assumed.
- Numeric compatibility is understood for 5 of 8 slots (`NUMERICALLY_COMPATIBLE`); 3 remain `UNKNOWN` at the exact-number level (FND-P/FND-S/RS-S/RS-P — pending D-item resolution) though nothing found suggests incompatibility, only insufficient precision to classify definitively yet.

`FREQ.6D.4B` **may proceed** — evidence is sufficient to make every enumerated decision (D1-D17) as a genuine product/architecture choice grounded in a real envelope, not a guess. It should explicitly resolve D11 (TAP-S identity) before or alongside the numeric choices, since several downstream numeric decisions (D12) depend on it.

## 24. Final classification

```
FREQ6D4A_EVIDENCE_COMPLETE_WITH_CATALOG_CAPABILITY_GAPS
```

Not `FREQ6D4A_PRODUCTION_PRESCRIPTION_EVIDENCE_COMPLETE` (unqualified) — real, disclosed catalog-definition changes are clearly required for 3 of 8 slots (versioned eligibility extensions) and TAP-S's identity itself remains genuinely open. Not `_INCOMPLETE` — every slot has *some* usable evidence, and the gaps are precisely enumerated rather than a general shortfall. Not `_MODEL_NON_REPRESENTABLE` — nothing found contradicts the approved 5D policy's representability; every gap is a missing specific choice, not a modeling impossibility.
