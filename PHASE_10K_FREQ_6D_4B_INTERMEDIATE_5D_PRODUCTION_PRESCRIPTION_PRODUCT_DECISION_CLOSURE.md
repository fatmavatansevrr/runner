# Phase 10K-FREQ.6D.4B — Intermediate 5D Production Prescription Product Decision Closure

**Decision phase. No production code, no schema implementation, no catalog JSON authoring, no runtime/persistence change, no public activation. Every value below is a frozen product decision or explicit deferral — not an implementation.**

## 1. Preflight

- `PHASE_LEDGER.md` row 60: `FREQ.6D.4A`, `DONE`/`VERIFIED`, `FREQ6D4A_EVIDENCE_COMPLETE_WITH_CATALOG_CAPABILITY_GAPS` — confirmed.
- Its report (`PHASE_10K_FREQ_6D_4A_...md`) read directly; the D1-D18 inventory reproduced verbatim below by re-reading §19 of that file (not reconstructed from chat).
- `FREQ.6D.4B` confirmed not already ledgered.
- `git rev-parse HEAD` → `9e8e6dd32463c535802b9066b4c0398556185602`; `git status --short` → ` m baseline_tmp` only; `git diff --check` → clean.

## 2. Parent evidence

`PHASE_10K_FREQ_6D_4A_INTERMEDIATE_5D_PRODUCTION_PRESCRIPTION_CONTENT_EVIDENCE.md`, all 24 sections, used as the sole evidence input alongside frozen FREQ.6/FREQ.6C authority. No new evidence research was performed this phase — per its own scope ("Use ONLY: FREQ.6D.4A evidence envelopes..."), this phase transforms envelopes into exact values, it does not re-research.

## 3. Frozen authorities (confirmed untouched)

5D RunLayout (2K+2E+1L); `LaneOrdinal 0→PRIMARY/1→SECONDARY_CONTROLLED`; both structural roles `KEY_SESSION`; 24-state adherence severity; P1+P3 architecture; `BetweenRepetitions`/`AfterEachRepetition` semantics (N-1/N derivation not reopened); profile schema/execution contract/projector/RunningApp consumer; FREQ.6C's numeric envelope (missing-readiness 26.0km, explicit-zero 19.5km, resolved peak 44.5km, KEY2:KEY1=70%, long-run 28%/36km-cap). None modified below — used only as constraints.

## 4. D1-D18 resolution table

| ID | Decision | Result | Authority | Evidence | Compatibility | Downstream consequence |
|---|---|---|---|---|---|---|
| D1 | FND-P eligibility mechanism | `DECIDED`: `VERSIONED_WORKOUT_DEFINITION_ELIGIBILITY_EXTENSION_APPROVED` — new `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3, `eligiblePhases = ["PREPARATION_RUNWAY","LONG_HORIZON_GENERAL_ENDURANCE","FOUNDATION"]` (cumulative, matching `easy-standard.v5`'s real precedent) | `DERIVED_FROM_FROZEN_POLICY` | Existing identity's intent already matches FND-P purpose (§9, FREQ.6D.4A) | Version bump preserves v1/v2 historical bundles | WorkoutDefinition version bump (no skeleton change) |
| D2 | FND-P prescription | `DECIDED`: Repeated, 6×30s controlled effort, 90s recovery | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` | Adjacent evidence only (McMillan's beginner-fartlek 6-8×30s convention; no hill-repeat-specific source found) — disclosed as the weakest-sourced numeric decision in this table | Estimated-total distance, does not affect FREQ.6C weekly allocation | New `WorkoutPrescriptionProfile` |
| D3 | FND-S eligibility mechanism | `DECIDED`: `VERSIONED_WORKOUT_DEFINITION_ELIGIBILITY_EXTENSION_APPROVED` — new `THRESHOLD_TEMPO` v5, `eligiblePhases = ["BUILD","RACE_SPECIFIC","FOUNDATION"]` | `DERIVED_FROM_FROZEN_POLICY` | Same pattern as D1 | Preserves v1-v4 | WorkoutDefinition version bump |
| D4 | FND-S prescription | `DECIDED`: Continuous, 20 min (1200s) | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` | Derived via D16's reduction factor from D5's 40min (Higdon-anchored) Build value; no Foundation-specific duration source exists | Compatible | New `WorkoutPrescriptionProfile` |
| D5 | BLD-P prescription | `DECIDED`: Continuous, 40 min (2400s) | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` | Higdon's real 35-50min range (direct); 40min selected as a single representative dose (Appsel authors one profile per phase-lane, not a week-by-week progression at this decision layer) | Compatible | New `WorkoutPrescriptionProfile`, no WorkoutDefinition change (already `BUILD`-eligible) |
| D6 | BLD-S prescription | `DECIDED`: Repeated, 10×60s surge / 60s jog recovery | `DIRECT_EVIDENCE` | McMillan's named, specific convention ("10 to 12 one-minute surges, each followed by a one-minute jog") — 10 selected as the value inside both McMillan's 10-12 range and Higdon's structurally-analogous 8-10 interval-count range | Compatible | New `WorkoutPrescriptionProfile`, no WorkoutDefinition change |
| D7 | RS-P prescription | `DECIDED`: Continuous, 20 min (1200s) at goal pace | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` | Casado et al. supports race-specific quality work near competition (directional only); no exact duration source found for a 10K goal-pace rehearsal segment | Compatible | New `WorkoutPrescriptionProfile`, no WorkoutDefinition change |
| D8 | RS-S prescription | `DECIDED`: Continuous, 25 min (1500s) | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` | Reduced from D5's 40min per the frozen "secondary support, never a GOAL_PACE duplicate" rule (FREQ.6 §11); exact reduction magnitude is a disclosed product default | Compatible | New `WorkoutPrescriptionProfile`, no WorkoutDefinition change |
| D9 | TAP-P eligibility mechanism | `DECIDED`: `VERSIONED_WORKOUT_DEFINITION_ELIGIBILITY_EXTENSION_APPROVED` — new `GOAL_PACE_TEN_K` v3, `eligiblePhases = ["RACE_SPECIFIC","TAPER"]` | `DERIVED_FROM_FROZEN_POLICY` | Same pattern as D1/D3 | Preserves v1/v2 | WorkoutDefinition version bump |
| D10 | TAP-P reduction factor | `DECIDED`: 50% duration reduction from D7 → 10 min (600s), pace unchanged | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` | Mirrors the real evidenced taper-reduction magnitude found in FREQ.6D.4A §8/§12 (8×800→4×800 = 50% rep reduction, intensity/pace held constant) | Compatible; taper-week `keyTotal` figures (FREQ.6C §B) have more margin, not less | New `WorkoutPrescriptionProfile` |
| D11 | TAP-S identity | `DECIDED`: **Option A** — `EXISTING_WORKOUT_IDENTITY_SEMANTICALLY_VALID` (`FARTLEK`), new v5 adding `TAPER`: `eligiblePhases = ["BUILD","TAPER"]` | `DERIVED_FROM_FROZEN_POLICY` + product judgment (disclosed) | `FARTLEK`'s `SURGE_AND_FLOAT` descriptor is generic enough to represent a drastically-reduced-dose "strides-like" variant at the profile level, per this architecture's own "same WorkoutDefinition, distinct profile" pattern (FREQ.6D.1A §A4) — the fit is not a literal terminology match ("strides" vs. "fartlek"), disclosed honestly, but no NEW identity is required to represent the purpose | Compatible | WorkoutDefinition version bump (not a new identity) |
| D12 | TAP-S prescription | `DECIDED`: Repeated, 6×20s / 100s walk recovery | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` | Scaled down from D6's fartlek envelope per the same taper-reduction logic as D10; no strides-specific source exists (same weak-basis disclosure as D2) | Compatible | New `WorkoutPrescriptionProfile` |
| D13 | RecoveryMode per slot | `DECIDED` (see §11) | Mixed: `DIRECT_EVIDENCE` (BLD-S=Jog, McMillan explicit) / `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` (FND-P=Jog, TAP-S=Walk) | See §11 | N/A | Profile field value |
| D14 | RecoveryPlacement per slot | `DECIDED`: `BetweenRepetitions` uniformly (FND-P, BLD-S, TAP-S — the only Repeated slots) | `DERIVED_FROM_FROZEN_POLICY` | Matches FREQ.6D.3A.1's already-frozen `BETWEEN_REPETITIONS` normal preference; every real source found in 4A used this pattern | N/A | Profile field value |
| D15 | IntensityTarget.DescriptorKey vocabulary | `DECIDED` (see §17 exact matrix) | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` (new vocabulary strings, UPPER_SNAKE_CASE, consistent with existing `SURGE_AND_FLOAT`/`THRESHOLD`/`GOAL_PACE` convention) | Naming-convention consistency only, not a scientific claim | N/A | New descriptor-key vocabulary entries |
| D16 | Foundation/Taper reduction factor | `DECIDED`: Foundation ≈50% of Build's dose (D4 20min vs. D5 40min; D2's 6 reps vs D6's 10); Taper ≈50-60% of its phase-analogue (D10, D12) | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` | No exact recreational-10K-specific percentage was evidenced; 50% chosen as a clean, disclosed default consistent with both the real evidenced taper pattern's magnitude (8→4 = 50%) and general progressive-overload framing (not itself a cited number for Foundation specifically) | Compatible | Governs D2/D4/D10/D12's exact values above |
| D17 | Profile reuse (`THRESHOLD_TEMPO` across FND-S/BLD-P/RS-S) | `DECIDED`: `DISTINCT_PROFILE_REQUIRED` for each — WorkoutDefinition identity reused, profile content is phase-distinct | `DERIVED_FROM_FROZEN_POLICY` | FREQ.6 §11 assigns materially different purpose/dose per phase; FREQ.6D.4A §20 already classified cross-phase profile reuse `SEMANTICALLY_INVALID` | N/A | 3 distinct `WorkoutPrescriptionProfile` documents, 1 shared `WorkoutDefinition` identity (+ version variants per D3) |
| D18 | KEY2-floor protective test | `DECISION_DEFERRED` — explicitly out of this phase's scope (engineering item, not a product decision, per FREQ.6D.4A's own labeling) | N/A | N/A | Re-confirmed `PROVEN_UNREACHABLE` under the exact values selected here (§9 below) | Carried to `FREQ.6D.4` resumption as a required regression test |

**Result: 17 of 18 items `DECIDED` this phase; D18 explicitly, correctly deferred as engineering scope (not evidence-insufficient — it was never a product decision).**

## 5-12. Per-slot decisions (Foundation Primary through Taper Secondary)

See §4's table for each slot's decision and §17 for the exact executable row. Summary narrative, honoring §7-14's individual requirements:

- **Foundation Primary (FND-P)**: `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3 (new, eligibility-extended), 6×30s controlled effort / 90s jog, EffortBased, `CONTROLLED_AEROBIC_POWER_INTRO` (reusing the WorkoutDefinition's own existing intensity-descriptor string). Weakest-evidenced numeric slot in the matrix, disclosed as such, not hidden.
- **Foundation SecondaryControlled (FND-S)**: `THRESHOLD_TEMPO` v5 (new, eligibility-extended), Continuous 20min, EffortBased (not PaceBased — deliberately gentler for an "introduction," per §8's own instruction that it "remains a KEY stimulus" but is appropriately secondary), `CONTROLLED_THRESHOLD_INTRO`. Remains a genuine KEY-tier stimulus (sustained continuous effort, not EASY's undifferentiated single-descriptor shape) while sitting at half of BLD-P's dose (D16) — satisfies §8's three proof requirements together.
- **Build Primary (BLD-P)**: `THRESHOLD_TEMPO` (existing v4, no change needed), Continuous 40min, PaceBased, `THRESHOLD_PACE`. Every `WorkoutPrescriptionProfile` field frozen — no free-text-only prescription remains (§9).
- **Build SecondaryControlled (BLD-S)**: `FARTLEK` (existing v4, no change needed), decided as the **same WorkoutDefinition identity as BLD-P would need to differ from** — i.e., BLD-S uses a **distinct identity** (`FARTLEK`, not `THRESHOLD_TEMPO`) rather than a lower-dose profile of the same identity, because Higdon's real evidence shows Build's two quality days are materially different *formats* (continuous tempo vs. repeated interval), which is stronger, more direct evidence than the "same identity, lower profile" alternative — explicit reasoning per §10's requirement.
- **RaceSpecific Primary (RS-P)**: `GOAL_PACE_TEN_K` (existing v2, no change needed), Continuous 20min at goal pace, PaceBased, `GOAL_PACE_TEN_K`. All executable fields frozen (§11) — the WorkoutDefinition's own free-text descriptor is not treated as sufficient; a full profile is authored.
- **RaceSpecific SecondaryControlled (RS-S)**: `THRESHOLD_TEMPO` (existing v4, no change needed), Continuous 25min, PaceBased, `THRESHOLD_SUPPORT_PACE`. Complements (different format, reduced dose) rather than duplicates RS-P; frozen two-KEY structure preserved without forcing equal dose (§12).
- **Taper Primary (TAP-P)**: `GOAL_PACE_TEN_K` v3 (new, eligibility-extended), Continuous 10min at goal pace (intensity/pace retained, duration halved — §13's frozen rule applied exactly, no new taper progression policy introduced).
- **Taper SecondaryControlled (TAP-S)**: `FARTLEK` v5 (new, eligibility-extended), Repeated 6×20s / 100s walk, EffortBased, `CONTROLLED_STRIDES_SHARPENING`. Resolves §14's load-bearing question as **Option A**, with the exact versioned change identified (not merely asserted) and the honest disclosure that the naming fit ("strides" via a `FARTLEK` identity) is a product judgment call, not a forced or evidence-proven identity match.

## 13. WorkoutDefinition versioning strategy

Four version bumps required, all pure `eligiblePhases` additions to already-existing, already-suitable component skeletons (no new component type, no skeleton redesign):

| Identity | New version | Change |
|---|---|---|
| `AEROBIC_STRENGTH_CONTROLLED_INTRO` | v3 | `eligiblePhases` += `FOUNDATION` |
| `THRESHOLD_TEMPO` | v5 | `eligiblePhases` += `FOUNDATION` |
| `GOAL_PACE_TEN_K` | v3 | `eligiblePhases` += `TAPER` |
| `FARTLEK` | v5 | `eligiblePhases` += `TAPER` |

`FARTLEK` v4 and `THRESHOLD_TEMPO` v4 remain referenced, unchanged, by `BLD-S`/`BLD-P`/`RS-S` respectively — no already-published version is mutated in place (§16's fourth question: "Does a version bump preserve historical bundles?" — yes, confirmed for all four, since each is strictly additive).

## 14. Eligibility/versioning decisions — §16 four-question check (all four extensions)

| Question | Answer (all 4 extensions) |
|---|---|
| Does phase eligibility change workout identity/meaning? | No — same physiological intent (controlled aerobic-strength, threshold, goal-pace, fartlek) in the new phase |
| Is the existing skeleton already suitable? | Yes — all four use WARM_UP/MAIN_SET/COOL_DOWN (± embedded recovery), which the `PrescriptionProfile` schema's `Repeated`/`Continuous` component shapes already support without modification |
| Can phase-specific dosage live entirely in the profile? | Yes — every numeric difference between e.g. `BLD-P` and `FND-S` (both `THRESHOLD_TEMPO`) is a profile-level difference, not a WorkoutDefinition difference |
| Does a version bump preserve historical bundles? | Yes — additive, cumulative `eligiblePhases`, matching the real `easy-standard.v1→v6` precedent already in the catalog |

All four → **approved** per §16's own stated rule.

## 15. Exact prescription matrix

`PRODUCTION_PRESCRIPTION_EXACT_MATRIX`

| Slot | Phase | Lane | WorkoutDefinition (key/version) | Profile | StructureMode | Work Unit | Work Value | RepetitionCount | Recovery Unit | Recovery Value | Recovery Mode | RecoveryPlacement | IntensityMode | IntensityDescriptor | DoseCategory | DistanceAccountingMode | Authority |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FND-P | Foundation | 0 | `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3 (new) | Distinct, new | Repeated | Seconds | 30 | 6 | Seconds | 90 | Jog | BetweenRepetitions | EffortBased | `CONTROLLED_AEROBIC_POWER_INTRO` | Primary | EstimatedSessionTotal | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` |
| FND-S | Foundation | 1 | `THRESHOLD_TEMPO` v5 (new) | Distinct, new | Continuous | Seconds | 1200 | — | — | — | — | — | EffortBased | `CONTROLLED_THRESHOLD_INTRO` | SecondaryControlled | EstimatedSessionTotal | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` |
| BLD-P | Build | 0 | `THRESHOLD_TEMPO` v4 (existing) | Distinct, new | Continuous | Seconds | 2400 | — | — | — | — | — | PaceBased | `THRESHOLD_PACE` | Primary | EstimatedSessionTotal | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` |
| BLD-S | Build | 1 | `FARTLEK` v4 (existing) | Distinct, new | Repeated | Seconds | 60 | 10 | Seconds | 60 | Jog | BetweenRepetitions | EffortBased | `SURGE_FASTER_THAN_5K_EFFORT` | SecondaryControlled | EstimatedSessionTotal | `DIRECT_EVIDENCE` |
| RS-P | RaceSpecific | 0 | `GOAL_PACE_TEN_K` v2 (existing) | Distinct, new | Continuous | Seconds | 1200 | — | — | — | — | — | PaceBased | `GOAL_PACE_TEN_K` | Primary | EstimatedSessionTotal | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` |
| RS-S | RaceSpecific | 1 | `THRESHOLD_TEMPO` v4 (existing) | Distinct, new | Continuous | Seconds | 1500 | — | — | — | — | — | PaceBased | `THRESHOLD_SUPPORT_PACE` | SecondaryControlled | EstimatedSessionTotal | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` |
| TAP-P | Taper | 0 | `GOAL_PACE_TEN_K` v3 (new) | Distinct, new | Continuous | Seconds | 600 | — | — | — | — | — | PaceBased | `GOAL_PACE_TEN_K` | Primary | EstimatedSessionTotal | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` |
| TAP-S | Taper | 1 | `FARTLEK` v5 (new) | Distinct, new | Repeated | Seconds | 20 | 6 | Seconds | 100 | Walk | BetweenRepetitions | EffortBased | `CONTROLLED_STRIDES_SHARPENING` | SecondaryControlled | EstimatedSessionTotal | `SUPPORTED_RANGE_SELECTED_AS_PRODUCT_DEFAULT` |

All 8 profiles are **distinct** (`DISTINCT_PROFILE_REQUIRED`, §6/D17) — zero profile reuse across slots, matching the frozen "no forced reuse, no forced uniqueness" instruction: every slot's purpose (§4 of FREQ.6D.4A) is genuinely different from every other slot's, so no reuse qualifies under §19/§20's `SEMANTICALLY_INVALID` test.

## 16. Recovery decisions

`RecoveryPlacement = BetweenRepetitions` for all three `Repeated` slots (FND-P, BLD-S, TAP-S) — no `AfterEachRepetition` slot exists in this matrix, since every real evidence source found in FREQ.6D.4A used the "repeat N times, recover between" framing, never the "recover after every rep including the last" framing. `RecoveryMode`: `Jog` for BLD-S (direct McMillan citation), `Jog` for FND-P (weaker basis, product default matching the general "easy jog" convention used everywhere else in this catalog's `RECOVERY`/`EASY_JOG` vocabulary), `Walk` for TAP-S (deliberately distinguished — near-max short strides conventionally pair with fuller walk recovery, a disclosed product judgment, not a literature citation). `N-1`/`N` derivation itself is not reopened — `RecoveryCount` remains a derived execution value per the already-frozen FREQ.6D.3A.1 mechanism.

## 17. Primary/Secondary relationships (frozen per phase, §22)

| Phase | Relationship |
|---|---|
| Foundation | Different stimulus identity (aerobic-strength/hills vs. threshold-introduction) + lower total dose for both relative to Build; not a pure intensity difference |
| Build | Different stimulus identity (continuous tempo vs. repeated fartlek) at materially different total work (40min continuous vs. ~10min total surge time); this is the combination case, not a single-axis ratio |
| RaceSpecific | Different stimulus identity retained (goal-pace specificity vs. threshold support) with a moderate duration reduction (20min vs. 25min — closer parity than Build, since both are legitimate race-adjacent stimuli, but RS-S still never duplicates RS-P's exact goal-pace purpose) |
| Taper | Same identity-pairing pattern as RaceSpecific/Build respectively, both at ~50% reduced dose, intensity/pace retained per §13's frozen rule |

No single universal PRIMARY:SECONDARY ratio was encoded — each phase's relationship is a distinct, disclosed combination of identity-difference and dose-difference, per §22's explicit instruction not to force one ratio.

## 18. Numeric compatibility (§19)

FREQ.6C's starting volume/peak reference/allocation policy/long-run authority/taper authority were not touched. Every slot above uses `DistanceAccountingMode.EstimatedSessionTotal` — confirmed (FREQ.6D.4A §14, re-confirmed here) that this accounting mode computes an estimated session distance from the session's own duration/pace at runtime; it does not feed back into or alter FREQ.4's weekly `keyTotal` allocation, which remains governed solely by weekly volume × the frozen 70/30 split. Checked qualitatively at the three required checkpoints:

- **Minimum supported week** (8wk, explicit-zero, weekly=28.0km, `keyTotal`=10.0km, FREQ.6C §D): the smallest KEY session in this matrix (TAP-P, 10min goal-pace ≈ realistic recreational pace) and largest (BLD-P, 40min threshold) both produce plausible estimated distances well inside the real per-session 3.0km floor to `keyTotal`-share range — no session's estimated distance is so large it would need to exceed its allocated share, nor so small it can't clear the 3.0km floor.
- **Peak week** (12wk, missing-readiness, weekly=44.5km): same reasoning, larger `keyTotal` headroom, still compatible.
- **Taper week**: TAP-P/TAP-S's deliberately-reduced (50%) durations are, by construction, smaller than their Build/RaceSpecific analogues — clears the real taper-week `keyTotal` figures (15.0-25.5km range, FREQ.6C §B) with more margin, not less.

**`NUMERICALLY_COMPATIBLE`** for all 8 slots. No exact profile dose required violating FREQ.6C to fit — FREQ.6C was not modified.

## 19. KEY2 floor

`PROVEN_UNREACHABLE` (FREQ.6D.4A §14) is **preserved**. None of the 8 selected profiles alters `FourDaySessionDistanceAllocationPolicy`'s mechanism, the 70/30 split, or the structural 3.0km/6.0km floor values — the theoretical edge (keyTotal at its 6.0km absolute floor combined with a 60%, not 70%, ratio) remains purely a function of weekly-volume arithmetic untouched by this phase's workout-content decisions. `FREQ6D4B_NUMERIC_CONTRADICTION_FOUND` does **not** apply.

## 20. Phase transitions (§21)

- **Foundation→Build**: dose progression (6×30s/20min → 10×60s/40min, roughly doubling) with intensity also progressing (EffortBased-only Foundation → PaceBased BLD-P) — both axes step up together, evidence-consistent with Higdon's own real within-Build progression pattern (35→50min across weeks), not an unjustified jump.
- **Build→RaceSpecific**: primarily a **specificity** shift (threshold-format Primary → goal-pace-format Primary), not a further dose increase — RS-P (20min) and RS-S (25min) sit close to or below BLD-P/BLD-S's dose, consistent with Casado et al.'s real finding that phase change nearer competition is about specificity/format, not necessarily raw volume increase.
- **RaceSpecific→Taper**: pure **dose** reduction (50%) with intensity/pace explicitly **retained** — the clearest, most directly-evidenced transition in the whole sequence (real 8×800→4×800 pattern).

Intensity progression (Foundation's EffortBased-only → Build/RaceSpecific's PaceBased for Primary lanes) is explicitly distinguished from dose progression (duration/rep-count changes) throughout, per §21's requirement.

## 21. Profile reuse

All 8 profiles: `DISTINCT_PROFILE_REQUIRED` (§15/D17). Zero `PROFILE_REUSE_APPROVED` decisions — every slot's purpose, phase, and (for the 3 `THRESHOLD_TEMPO`-based slots) dose differ genuinely, so no reuse candidate survives the `SEMANTICALLY_INVALID` test carried forward from FREQ.6D.4A §20. `WorkoutDefinition`-level reuse (3 identities each serving 2-3 slots) is real and approved — this is the `SUPPORTED_REUSE` pattern, distinct from profile-level reuse, and matches FREQ.6D.1's own architecture design exactly.

## 22. Implementation manifest

`PRODUCTION_PRESCRIPTION_IMPLEMENTATION_MANIFEST`

| Artifact | Classification | Depends on |
|---|---|---|
| `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3 | `WORKOUT_DEFINITION_VERSION_BUMP` | — |
| `THRESHOLD_TEMPO` v5 | `WORKOUT_DEFINITION_VERSION_BUMP` | — |
| `GOAL_PACE_TEN_K` v3 | `WORKOUT_DEFINITION_VERSION_BUMP` | — |
| `FARTLEK` v5 | `WORKOUT_DEFINITION_VERSION_BUMP` | — |
| `FARTLEK` v4 (BLD-S use) | `NO_CHANGE` | — |
| `THRESHOLD_TEMPO` v4 (BLD-P/RS-S use) | `NO_CHANGE` | — |
| `GOAL_PACE_TEN_K` v2 (RS-P use) | `NO_CHANGE` | — |
| FND-P profile | `NEW_PROFILE` | `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3 |
| FND-S profile | `NEW_PROFILE` | `THRESHOLD_TEMPO` v5 |
| BLD-P profile | `NEW_PROFILE` | `THRESHOLD_TEMPO` v4 (no bump) |
| BLD-S profile | `NEW_PROFILE` | `FARTLEK` v4 (no bump) |
| RS-P profile | `NEW_PROFILE` | `GOAL_PACE_TEN_K` v2 (no bump) |
| RS-S profile | `NEW_PROFILE` | `THRESHOLD_TEMPO` v4 (no bump) |
| TAP-P profile | `NEW_PROFILE` | `GOAL_PACE_TEN_K` v3 |
| TAP-S profile | `NEW_PROFILE` | `FARTLEK` v5 |
| Profile schema validation (all 8) | (engineering step) | all 8 `NEW_PROFILE` entries |
| Dual-lane progression exact refs (Week × LaneOrdinal → profile ref) | (`FREQ.6D.4`'s own scope, not this manifest's) | validated profiles + `FREQ.6D.4`'s dual-lane engineering |

**Dependency order**: WorkoutDefinition version bumps (4, independent of each other) → profile authoring (8, each depends only on its own WorkoutDefinition version, otherwise independent/parallelizable) → profile validation → progression exact refs (`FREQ.6D.4`'s resumed scope). No `NEW_WORKOUT_DEFINITION` artifact appears anywhere in this manifest — confirmed zero new-identity requirement, consistent with D11's Option A resolution.

## 23. Next phase type

**`IMPLEMENTATION`** (a single coherent phase, tentatively `FREQ.6D.4C`) — not `ARCHITECTURE_DESIGN`. Reasoning per §27's own decision rule: all four required WorkoutDefinition changes are pure `eligiblePhases` extensions to already-suitable, unmodified component skeletons (§14's four-question check passed cleanly for all four) — no new component type, no new semantic/structural design is needed. §27's explicit permission ("If only versioned eligibility extensions are needed: a narrow implementation phase may handle them together with profiles if scope remains coherent") applies directly. The scope (4 version bumps + 8 profile documents, all governed by this phase's exact matrix) is coherent and boundable as one implementation phase, distinct from `FREQ.6D.4`'s own remaining engineering scope (dual-lane progression/persistence/adaptation/RunningApp wiring), which still requires its own separate resumption.

## 24. Remaining blockers

None at the product-decision level. The only carried-forward item is **D18** (KEY2-floor protective regression test), explicitly engineering scope, to be added when `FREQ.6D.4` resumes — not a blocker to catalog authoring.

## 25. Representability

```
ALL_8_SLOTS_PRODUCT_RESOLVED
```

## 26. Final classification

```
FREQ6D4B_PRODUCTION_PRESCRIPTION_POLICY_APPROVED
```

Full closure — not the catalog-identity-gap variant (D11 resolved to an existing identity, zero new `WorkoutDefinition` identity required) and not the TAP-S-unresolved variant (D11/D12 both closed). 17 of 18 inventory items `DECIDED`; the 18th (D18) was never a product decision and is correctly carried to `FREQ.6D.4`'s engineering resumption. No implementation agent following this report needs to choose a numeric prescription, a workout identity, or invent phase eligibility — every athlete-facing value is frozen in §15's exact matrix, and every versioning/reuse consequence is explicit in §13/§21/§22.
