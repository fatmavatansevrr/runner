# PHASE HM.9 — Half-Marathon Intermediate 5D Second-KEY-Lane Authority Closure

Governance / product-decision phase. No production code, no catalog file, no schema was modified by this phase.

## 1. HM.6 blocker recap

HM.6 found `RUN_LAYOUT_5D` has two `KEY_SESSION` slots (not the naive KEY+3EASY assumption) and classified the resulting gap `HM_5D_SECOND_HARD_STIMULUS_AUTHORITY_MISSING`: HM's own `HALF_MARATHON_WORKOUT_PROGRESSION` is flat/single-lane, with no existing authority for what the second `KEY_SESSION` slot should contain. This phase resolves that authority.

## 2. Real RUN_LAYOUT_5D semantics

Re-confirmed by direct read: `{K, E, K, E, L}` (`sequenceOrder` 1–5), one `runsPerWeek: 5`, no per-slot metadata distinguishing the two `KEY_SESSION` entries beyond their `sequenceOrder` — the schema itself does not mark either slot "primary" or "secondary"; that distinction is a content/authority decision, not a structural one. No spacing constraint is encoded in the layout document itself (spacing is a downstream calendar concern, `HM-LIT-21`).

## 3. 10K multi-KEY mechanism audit

`TEN_K_WORKOUT_PROGRESSION_V1 v6` (referenced by `TEN_K_MASTER v7`, the version every currently-reachable 10K combination uses) is real, shipped, production-validated evidence, read directly in full. Every phase (`FOUNDATION`/`BUILD`/`RACE_SPECIFIC`/`TAPER`) carries a `lanes` array with `laneOrdinal 0` (primary) and `laneOrdinal 1` (secondary), each with its own `stages`/`workoutCandidates`/`prescriptionProfileCandidates`/`minimumExposures`/`maximumExposures`/`compressionBehavior`/`extensionBehavior`:

| Phase | Lane 0 (primary) | Lane 1 (secondary) |
|---|---|---|
| FOUNDATION | `AEROBIC_STRENGTH_CONTROLLED_INTRO` | `THRESHOLD_TEMPO` |
| BUILD | `THRESHOLD_TEMPO` | `FARTLEK` |
| RACE_SPECIFIC | `GOAL_PACE_TEN_K` | `THRESHOLD_TEMPO` |
| TAPER | `GOAL_PACE_TEN_K` (PROTECTED/FIXED) | `FARTLEK` (PROTECTED/FIXED) |

**Classification: `GENERIC_MECHANISM_REUSE` for the `lanes`/`laneOrdinal` schema shape itself — confirmed real, already-shipped, EXISTING_SCHEMA_SUFFICIENT (§18). `10K_NUMERIC/TRAINING_AUTHORITY_NOT_REUSABLE` for every specific family-per-phase-per-lane choice** — 10K's own Foundation-secondary is a genuine `THRESHOLD_TEMPO` (real hard content even in Foundation), which is materially more aggressive than HM's own already-established, deliberately conservative Foundation philosophy (HM's primary Foundation has zero quality content today — `FOUNDATION_AEROBIC_BASE` only). This is not inherited for HM (§10). One consistent, reusable *pattern* is observed, not treated as binding authority: the secondary lane's `prescriptionProfileCandidates` are consistently named `..._SECONDARY_CONTROLLED`, implying dose/intensity is dialed back on the secondary lane relative to primary even when the *workout family* itself is a genuine hard one.

## 4. HM hard-session evidence

Internal Appsel evidence is **not silent** here (contrary to the governing prompt's own framing) — direct re-read of `HM_21K_Claude_Handoff_and_Build_Plan.md` found real, substantive, already-existing guidance:

- **`HM-LIT-16`**: separates `STRIDES/SHORT_HILL_STRIDES` (adjunct, short, controlled, full recovery, "does not consume a full hard-session slot") from `STRUCTURED_HILL_REPETITIONS` (a genuine `KEY_SESSION`/hard stimulus). No strides/hills workout family exists in HM's real catalog today — not invented in this phase (§8's own prohibition).
- **`HM-LIT-17`**: an explicit `workoutFamily × progressionStage × doseTier` dose model (LOW/STANDARD/HIGH), with Intermediate mapping to `STANDARD`. Explicitly: "Do not increase weekly volume materially and quality dose materially in the same transition."
- **`HM-LIT-21`**: real, explicit calendar/spacing evidence directly on point — **"Two true hard stimuli: minimum approximately 48h"**; **"Adjacency: HARD+HARD → prohibited, HARD+EASY → allowed"**; and a named, already-anticipated fallback ladder for when the preferred structure can't be placed: "1. deterministic alternative placement; 2. downgrade quality-long complexity; **3. downgrade secondary quality**; 4. typed fail-closed." The document's own vocabulary already names "secondary quality" as a real, expected, *reducible* concept — this phase's core question is not novel to the evidence base.

No internal evidence anywhere states or implies more than **two** true hard stimuli per week for any HM cell. This is the highest count the evidence discusses.

## 5. Current HM progression compatibility

Re-confirmed: `HALF_MARATHON_WORKOUT_PROGRESSION v1` has no `lanes` array — flat `stages` per phase, single-lane only. Fully sufficient and unchanged for 3D/4D (§4D governing precedent). Not sufficient, as authored today, to feed a second lane — this is a real, confirmed gap, not assumed.

## 6. Candidate second-KEY models — chosen model

Evaluated against §6's four candidate models: **Model D (not-yet-compatible) is rejected** — the schema mechanism is proven real and sufficient (§3/§18). **Model A (two true quality sessions, unconditionally) is rejected** — this is 10K's own real choice, but importing it wholesale would introduce real hard content into HM's Foundation and Taper phases, contradicting HM's own already-established conservative philosophy without any HM-specific evidence requiring it. **Model C (second KEY may always degrade to EASY) is too weak** — it would waste the real, evidence-supported "two true hard stimuli, ~48h apart" capacity the handoff document explicitly anticipates for phases where added stimulus is defensible (Build, RaceSpecific).

**Chosen: Model B, phase-varying** — the second KEY carries genuine (but dosed-back) quality only in Build and RaceSpecific; it is easy-equivalent (not a hard stimulus) in Foundation and Taper. This is the smallest model consistent with both the real 10K mechanism precedent and HM's own conservative-by-design philosophy, and requires no new workout family.

## 7–11. Phase-by-phase semantics

**Foundation** — `SecondaryKeyHardness = EASY_EQUIVALENT`. Rationale: HM's own primary Foundation has zero quality content; importing 10K's aggressive `THRESHOLD_TEMPO`-in-Foundation choice would contradict HM's own established, deliberate conservatism with no HM-specific evidence requiring it. The KEY2 slot still exists structurally (RunLayout doesn't vary by phase) and is filled with `EASY_STANDARD`-equivalent content — the slot is occupied, but carries no meaningful quality/hard classification. Classification: `PRODUCT_DEFAULT`.

**Build** — `SecondaryKeyHardness = LIGHT_QUALITY`, family = `FARTLEK` only (`THRESHOLD_TEMPO` explicitly `SECONDARY_PROHIBITED` here — stacking two threshold-type sessions in the same week would risk exceeding the evidence-supported two-true-hard-stimuli ceiling with two sessions of the *same*, most demanding character; `FARTLEK` mirrors 10K's own real Build-secondary choice, a genuinely real but categorically distinct hard stimulus from HM's own primary `BUILD_THRESHOLD_DEVELOPMENT`). Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (10K mechanism-pattern-informed, HM.7-conservative-consistent).

**RaceSpecific** — `SecondaryKeyHardness = LIGHT_QUALITY`, family = `FARTLEK` only (`HM_PACE` explicitly `SECONDARY_PROHIBITED` — per the governing prompt's own explicit warning against `HM_PACE + HM_PACE` or doubling race-specific hard work; "race specificity does not automatically justify two race-specific hard sessions"). Classification: `PRODUCT_DEFAULT`.

**Taper** — `SecondaryKeyHardness = EASY_EQUIVALENT` (not 10K's own choice of protected `FARTLEK`). Rationale: HM's taper is deliberately built around a single protected activation to preserve freshness for race day (`HM.1.4B`'s own non-chained, deepening-toward-race-day taper philosophy); introducing a second genuine hard stimulus during Taper, even a moderate one, runs directly against that established intent and against general taper science (reduce, don't add, load as race approaches). Classification: `PRODUCT_DEFAULT`.

## 12. Hard-session budget

**`MaximumTrueHardStimuliPerWeek = 2`.** Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` — `HM-LIT-21`'s own explicit "two true hard stimuli" spacing rule is the highest count the internal evidence discusses anywhere; no HM source ever contemplates three. For 5D specifically, this ceiling is reached only in Build/RaceSpecific (primary + `FARTLEK` secondary = 2); Foundation/Taper remain at 1 true hard stimulus (primary only, or none where the primary itself is protected/light), never exceeding the ceiling.

## 13. Workout-family eligibility (per phase, for the second KEY lane)

| Family | Foundation | Build | RaceSpecific | Taper |
|---|---|---|---|---|
| EASY_STANDARD | `SECONDARY_ALLOWED` (the only choice) | `SECONDARY_PROHIBITED`* | `SECONDARY_PROHIBITED`* | `SECONDARY_ALLOWED` (the only choice) |
| FARTLEK | `SECONDARY_PROHIBITED` | `SECONDARY_PREFERRED` | `SECONDARY_PREFERRED` | `SECONDARY_PROHIBITED` |
| THRESHOLD_TEMPO | `SECONDARY_PROHIBITED` | `SECONDARY_PROHIBITED` (would stack two threshold-type sessions) | `SECONDARY_PROHIBITED` | `SECONDARY_PROHIBITED` |
| HM_PACE | `SECONDARY_PROHIBITED` | `SECONDARY_PROHIBITED` | `SECONDARY_PROHIBITED` (would double race-specific hard work) | `SECONDARY_PROHIBITED` |
| LONG_RUN_STANDARD | `NOT_APPLICABLE` — never placed in a KEY lane, per §8's own explicit prohibition, in every phase | | | |

\* `EASY_STANDARD` is the fallback target (§14), not the preferred family, for Build/RaceSpecific.

## 14. Fallback behavior

If the preferred `FARTLEK` secondary is ineligible or unavailable (Build/RaceSpecific), fallback is `EASY_STANDARD` — directly matching `HM-LIT-21`'s own named "downgrade secondary quality" concept. Classification: the *concept* is `DIRECT_EXISTING_AUTHORITY` (named explicitly in the handoff document); the specific fallback target (`EASY_STANDARD`) is `PRODUCT_DEFAULT` (no formal `fallbackStageKey` chain for this exists in HM's catalog yet — would need authoring in the implementation phase). Plan generation must not fail merely because the optional secondary stimulus is unavailable — it degrades to easy, never blocks materialization.

## 15. Calendar/spacing feasibility

`HM-LIT-21`'s real, existing rules apply directly and are not reopened: minimum ~48h between two true hard stimuli; HARD+HARD adjacency prohibited; HARD+EASY adjacency allowed. `RUN_LAYOUT_5D`'s own slot sequence (`K,E,K,E,L`) places an `EASY_SUPPORT` slot between the two `KEY_SESSION` slots in *sequence order*, which is structurally consistent with (though not a formal proof of) achievable ≥48h calendar-day separation once real weekday assignment is applied. **This phase does not touch code and cannot formally simulate the real day-placement algorithm** — qualitative compatibility is confirmed, but the exact calendar-day feasibility proof (per §14's own instruction to "prove using actual 5D day-placement machinery") is explicitly deferred to the implementation phase (HM.10+), consistent with this phase's own "no production implementation" boundary. This is disclosed honestly as unverified-by-execution, not silently assumed closed.

## 16. Adaptation implications

Qualitative only, per this phase's own scope (no implementation). If Build/RaceSpecific's secondary `FARTLEK` is missed while the primary `THRESHOLD_DEVELOPMENT`/rehearsal is completed, existing role-aware generic adaptation (role = `KEY_SESSION` for both) would currently treat both KEY misses identically — it has no existing primary/secondary distinction. Whether HM eventually needs a role-subtype semantic (primary-KEY vs secondary-KEY) for adaptation scoring is flagged as a real, open downstream question for HM.10, not resolved or invented here.

## 17. Progression ownership

`HALF_MARATHON_WORKOUT_PROGRESSION` remains the sole primary-lane authority, completely unchanged (still flat, single-lane, 4D/3D-serving exactly as today). The smallest architecture consistent with real precedent is to widen this *same* document with a `lanes` array (mirroring `TEN_K_WORKOUT_PROGRESSION_V1 v6`'s exact real shape: lane 0 = today's existing content unchanged, lane 1 = new secondary content per §7–11) rather than a wholly separate secondary-lane document — matching how 10K's own single progression document already serves both single-lane (3D/4D) and dual-lane (5D/6D) frequencies today. This is a recommendation for the implementation phase, not enacted here.

## 18. Schema/binder capability

**`EXISTING_SCHEMA_SUFFICIENT`** — confirmed, not assumed, by direct read of `TEN_K_WORKOUT_PROGRESSION_V1 v6`, a real, currently-shipped, production-validated document already using exactly the `lanes`/`laneOrdinal` mechanism this phase's second-KEY model requires. No schema extension is needed.

## 19. Evidence/product-default classifications — summary

`DIRECT_EXISTING_AUTHORITY`: the `lanes` schema mechanism itself (already real, already shipped); the ~48h/HARD+HARD-prohibited spacing rules; the "downgrade secondary quality" fallback concept. `EVIDENCE_INFORMED_PRODUCT_DEFAULT`: `MaximumTrueHardStimuliPerWeek=2`; Build/RaceSpecific secondary family choice (`FARTLEK`, informed by but not copied from 10K's own real pattern). `PRODUCT_DEFAULT`, explicitly not overclaimed as evidence-backed: the specific per-phase hardness classification (Foundation/Taper = easy, Build/RaceSpecific = light-quality) and the specific `EASY_STANDARD` fallback target — genuine judgment calls, disclosed as such, chosen for consistency with HM's own already-established conservative philosophy rather than 10K's more aggressive real precedent.

## 20. Remaining gaps

Two, both explicitly deferred to the implementation phase, neither blocking this phase's own closure: (1) the exact calendar-day-placement feasibility proof (§15) requires real code execution this governance phase cannot perform; (2) whether adaptation needs a primary/secondary KEY role-subtype (§16) is a real open question, not required to resolve before HM.10 can begin authoring numeric policy.

## 21. HM.10 readiness conclusion

Every item required by §24 of the governing prompt is resolved: meaning of KEY2 (Model B, phase-varying); phase-by-phase activation (§7–11); hard-session count (2, §12); allowed workout families (§13); fallback behavior (§14); spacing (evidence-based rules re-confirmed, qualitative feasibility affirmed, formal proof deferred to implementation per §15's own honest disclosure); taper behavior (§11, easy-equivalent); progression ownership (§17, unchanged primary + new lanes-widened document); schema representability (§18, confirmed sufficient). HM.10 may proceed to author 5D's numeric volume/growth/long-run-share/taper/session-distribution policy, now with a settled, evidence-grounded second-KEY-lane content authority to build against.

## Final classification

`HM_INTERMEDIATE_5D_SECOND_KEY_AUTHORITY_CLOSED`
