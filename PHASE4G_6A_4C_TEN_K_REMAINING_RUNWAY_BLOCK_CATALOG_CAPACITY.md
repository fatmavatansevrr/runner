# Phase 4G.6A.4C — 10K Preparation Runway Remaining Block Catalog Progression Capacity

Scope: `TEN_K__4D__INTERMEDIATE v10` only. Catalog/schema capacity implementation for CONSISTENCY, GENERAL_ENDURANCE, PRE_SPECIFIC_TRANSITION. No orchestrator wiring, no runway-week generation.

## 1. Executive Result

**`TEN_K_PREPARATION_RUNWAY_CONSISTENCY_GENERAL_ENDURANCE_AND_TRANSITION_CATALOG_CAPACITY_IMPLEMENTED_AND_VALIDATED`**

All three remaining blocks now have valid, versioned, canonical catalog progression documents, consumed generically by the unmodified Phase 4G.6A.4B binder algorithm. All four Preparation Runway blocks are now catalog-`SUPPORTED`.

## 2. Inherited State

Allocator (4G.6A.3/3B) and both target matrices unchanged. AerobicStrength catalog capacity (4G.6A.4A) unchanged. Binder engine (4G.6A.4B) unchanged in algorithm; its separate workout-reference validator had one real defect found and fixed during this phase (section 12).

## 3. Pilot Scope

`TEN_K__4D__INTERMEDIATE v10`, Preparation Runway 3-8 full weeks, profiles `CONSISTENCY_NEEDED`/`CORE_ENTRY_READY`. No numeric/progression decision generalized to other candidates.

## 4. Artifacts Inspected

`PreparationRunwayBlockAllocationEngine.cs`, `TenKPreparationRunwayAllocationPolicyFactory.cs`, `PreparationRunwayBlockWorkoutBindingEngine.cs`, `PreparationRunwayBlockProgressionCatalogReader.cs`, `PreparationRunwayBlockWorkoutReferenceValidator.cs`; `aerobic-strength-controlled-*.v1.json`, `ten-k-aerobic-strength-progression.v1.json`, `preparation-runway-block-progression.schema.json`; `easy-standard.v4.json`, `long-run-standard.v4.json` (and all other 20 existing workout files); `ten-k-workout-progression.v5.json`, `ten-k-master.v6.json`, `ten-k-4d-intermediate.v10.json`; `workout-definition.schema.json`; `PhaseKey.cs`/`WorkoutFamily.cs`/`WorkoutComponentType.cs`; Phase 4G.6A.4A/4G.6A.4B test files and phase documents; `activation-readiness-risks.json`/`.md`.

## 5. Consistency Semantic Contract

Corrective, restorative: restores regular running frequency and controlled load re-entry for a runner whose recent-running evidence is Missing/CAUTION/NOT_READY — not a beginner or maximal-fitness phase.

## 6. Consistency Catalog Shape

Two steps, two different workout families (a genuine structural distinction, not a label): **Step 1** anchors `EASY_STANDARD` (frequency re-entry, no long-run role yet); **Step 2** anchors `LONG_RUN_STANDARD` (introduces the long-run role as the graduation signal toward `GENERAL_ENDURANCE`, which is itself entirely long-run-anchored — see section 7). `SUPPORTED_WITH_NEW_RUNWAY_PROGRESSION_MAPPING` for both existing workouts.

## 7. General Endurance Semantic Contract

Always-eligible, primary expandable block: low-intensity aerobic development, compatible with easy-support and controlled long-run work, non-race-specific, 1-5 weeks.

## 8. General Endurance Five-Step Capacity

**Decision: Option B** — all five steps reference the same workout, `LONG_RUN_STANDARD`, with distinct `stepKey`s (`GENERAL_ENDURANCE_STEP_1`.."STEP_5"). Justification, per the required five-step analysis: workout-definition documents in this catalog carry zero embedded numeric prescription for *any* existing workout (re-confirmed by inspection of all 20 pre-4G.6A.4C files) — quantitative dose (weekly volume, long-run distance, load trajectory) is exclusively a runtime-prescription concern, never catalog-authored data, for every workout in this repository without exception. Five distinct near-identical JSON files would therefore encode no real additional information over five ordered positions of the same workout — exactly the anti-pattern this phase is explicitly required to avoid. The five *positions themselves* carry the real, non-arbitrary meaning: strict monotonic `StepNumber` ordering (1→5), consumed by the existing exact-prefix binder, structurally represents "the Nth week of General Endurance development" — a genuine ordering fact, not a cosmetic label. Option A (five distinct workout definitions) was rejected as unsupported duplication with no catalog-representable content difference; Option C (a new GE-specific workout) was rejected as unnecessary — `LONG_RUN_STANDARD`'s own existing semantic already matches "controlled long-run work" exactly; Option D (an explicit settle/recovery position within GE) was rejected as unmotivated — GE's own semantic describes continuous development, not a deload need (that is `PRE_SPECIFIC_TRANSITION`'s distinct role).

## 9. General Endurance Repeat Policy

Repetition across all 5 steps is intentional. What later materialization (Phase 4G.6A.4D or beyond) will vary: weekly volume, long-run distance, total easy duration, load trajectory — none implemented here. The catalog progression remains meaningful before numeric materialization because (a) it fixes `LONG_RUN_STANDARD` as GE's canonical anchor workout (a real content decision, distinguishing GE from `CONSISTENCY`'s and `PRE_SPECIFIC_TRANSITION`'s own `EASY_STANDARD` anchors), and (b) it fixes the exact ordinal capacity (5, matching the approved `MaxWeeks=5`) the binder can exact-prefix-select against. Maximum consecutive use: all 5 (the entire progression) — explicitly validated, not accidental (`GeneralEndurance_RepeatsTheSameWorkoutReferenceAcrossAllFiveSteps_Intentionally`).

## 10. Transition Semantic Contract

Unchanged from Phase 4G.6A.2A: always eligible, exactly one week, fixed, non-expandable, immediately before Race Core, distinguished by fixed position and a volume-settle (not volume-build) trajectory.

## 11. Transition Catalog Shape

One step, anchors `EASY_STANDARD` — the same workout identity as `CONSISTENCY` Step 1, but at the opposite runway boundary (the absolute final week vs. the absolute first week), directly mirroring the repository's own established `TAPER_SHARPEN`/`FOUNDATION_EASY_BASE` precedent (Phase 4G.6A.2A) of non-adjacent, different-role content reuse. Not a label-only transition: its catalog objective (settle load immediately before Core) and its progression role (the runway's sole terminal step, block `PRE_SPECIFIC_TRANSITION`, never `CONSISTENCY`/`GENERAL_ENDURANCE`) are both explicit in the catalog data itself (distinct `blockType`, distinct progression document), independent of any future volume materialization.

## 12. Existing-Workout Reuse Audit

| Workout | Objective | Family | EligiblePhases (v5) | Runway fit | Classification |
|---|---|---|---|---|---|
| `EASY_STANDARD v5` | Plain controlled easy run | EASY | FOUNDATION,BUILD,RACE_SPECIFIC,TAPER,**PREPARATION_RUNWAY** | Yes | `SUPPORTED_WITH_NEW_RUNWAY_PROGRESSION_MAPPING` |
| `LONG_RUN_STANDARD v5` | Plain controlled long run | LONG_RUN | FOUNDATION,BUILD,RACE_SPECIFIC,TAPER,**PREPARATION_RUNWAY** | Yes | `SUPPORTED_WITH_NEW_RUNWAY_PROGRESSION_MAPPING` |

`v4` of both remains completely unchanged (re-verified by direct assertion) — the eligibility extension is version-scoped, not a blanket change; existing Core use (pinned to v4 everywhere in `ten-k-workout-progression.v5.json`) is unaffected.

**Defect found and fixed during this audit**: the Phase 4G.6A.4B `PreparationRunwayBlockWorkoutReferenceValidator` incorrectly (a) treated `RACE_SPECIFIC` membership in `eligiblePhases` as disqualifying (confusing a Core-phase *scheduling* marker with actual race-specific *content*), and (b) hardcoded `QUALITY` as the only acceptable family (true only for `AEROBIC_STRENGTH`, never tested against `EASY`/`LONG_RUN` family content until this phase). Both defects were real — proven by `EasyStandardV5AndLongRunStandardV5_PassTheExistingReferenceValidator` failing before the fix — and are now corrected: `eligiblePhases` is checked only for `PREPARATION_RUNWAY` presence; `family` is accepted for any non-`RACE` value (`EASY`, `LONG_RUN`, `QUALITY`). This is a validator bug fix, not a binder algorithm change — the pure binding-selection algorithm itself was not touched.

## 13. New-Workout Decision

**None required.** Both existing generic workouts (`EASY_STANDARD`, `LONG_RUN_STANDARD`) fully support all three blocks' catalog contracts once version-bumped for eligibility.

## 14. Schema Sufficiency

`preparation-runway-block-progression.schema.json` (Phase 4G.6A.4A) already supports `blockType ∈ {CONSISTENCY, GENERAL_ENDURANCE, AEROBIC_STRENGTH, PRE_SPECIFIC_TRANSITION}` (all four values were already present), arbitrary step counts, and repeated `workoutCandidates` references across steps (nothing in the schema forbids two steps referencing the same key/version). **No schema change was needed or made.**

## 15. Progression Artifacts

- `plan-catalog/catalog/preparation-runway-progressions/ten-k-consistency-progression.v1.json` — `TEN_K_CONSISTENCY_PROGRESSION v1`, `blockType=CONSISTENCY`, 2 steps.
- `plan-catalog/catalog/preparation-runway-progressions/ten-k-general-endurance-progression.v1.json` — `TEN_K_GENERAL_ENDURANCE_PROGRESSION v1`, `blockType=GENERAL_ENDURANCE`, 5 steps.
- `plan-catalog/catalog/preparation-runway-progressions/ten-k-pre-specific-transition-progression.v1.json` — `TEN_K_PRE_SPECIFIC_TRANSITION_PROGRESSION v1`, `blockType=PRE_SPECIFIC_TRANSITION`, 1 step.

All three validate against the unchanged schema; none contains a `RunwayWeeks`/profile-name/allocation-weight token (verified by direct grep test); all are independently loadable by the existing, unmodified `PreparationRunwayBlockProgressionCatalogReader`.

## 16. Binder Compatibility

The existing, unmodified `PreparationRunwayBlockWorkoutBindingEngine` consumed all three new progression definitions with zero source changes, exact-prefix-selecting correctly for every supported allocation count and rejecting every over-capacity count with `ProgressionCapacityExceeded` — proven end-to-end against the real catalog (loader → binder), not synthetic fixtures. A dedicated test confirms the binder's own source file contains no block-name string literal for any of the four blocks (no per-block special-casing).

## 17. Adjacency Audit

- **Consistency → General Endurance**: Consistency Step 2 and GE Step 1 share workout identity (`LONG_RUN_STANDARD`) but are distinguished by block identity and progression position (Consistency's own terminal step vs. GE's own opening step) — the same non-adjacent-reuse pattern already established by `TAPER_SHARPEN`/`FOUNDATION_EASY_BASE`.
- **General Endurance → AerobicStrength**: different workout families entirely (`LONG_RUN` vs. `QUALITY`), confirmed by direct catalog-content assertion.
- **AerobicStrength → Transition**: AerobicStrength's own controlled-quality content (family `QUALITY`) is structurally distinct from Transition's plain easy content (family `EASY`).
- **General Endurance → Transition**: different workout families (`LONG_RUN_STANDARD` vs. `EASY_STANDARD`), confirmed by direct assertion.
- **Transition → Core Week 1**: Transition references `EASY_STANDARD v5`; Foundation Week 1's real stage (`FOUNDATION_EASY_BASE`) references `EASY_STANDARD v4` — different catalog versions, confirmed by direct inspection of the live `ten-k-workout-progression.v5.json`, and `v4` carries no `PREPARATION_RUNWAY` eligibility at all, so no live mechanism could ever conflate the two. Numeric continuity (volume trajectory) between Transition and Foundation Week 1 is explicitly a future materialization-phase concern, not claimed as implemented here.

## 18. Core Week 1 Comparison

Confirmed unaffected and unchanged: `FOUNDATION_EASY_BASE` still candidates only `EASY_STANDARD v4` (re-verified by direct test against the live, untouched progression document).

## 19. Catalog/Version Integrity

`easy-standard.v5.json`/`long-run-standard.v5.json` are the smallest valid version bump (content identical to v4 except the added `PREPARATION_RUNWAY` eligiblePhases value). No other catalog file was modified. `TEN_K__4D__INTERMEDIATE v10`'s runtime references are untouched — confirmed the new v5 workouts and all three new progression documents are absent from the real, constructed published bundle, and the new `preparation-runway-progressions` folder remains unread by the general catalog loader (re-confirmed).

## 20. Per-Block Support Classification

`CONSISTENCY`: **SUPPORTED**. `GENERAL_ENDURANCE`: **SUPPORTED** (five-step contract is semantically defensible per section 8-9, not merely five prefix entries). `AEROBIC_STRENGTH`: **SUPPORTED** (inherited, unchanged). `PRE_SPECIFIC_TRANSITION`: **SUPPORTED**.

## 21. Full Runway Catalog Readiness

**All four blocks are catalog-`SUPPORTED`.** Full Preparation Runway catalog progression capacity for `TEN_K__4D__INTERMEDIATE v10` is complete.

## 22. Runtime/Public/Persistence Classification

`CATALOG_AVAILABLE`, `DARK_LOADABLE`, `NOT_PUBLISHED_IN_LIVE_CANDIDATE`, `NOT_RUNTIME_BOUND` for all three new progression documents and both new workout versions — matching the required classification exactly. No public DTO, controller, or persistence project references any new symbol (re-verified via the existing Phase 4G.6A.4B dark-governance test suite, which still passes unchanged).

## 23. Deferred Full Week-Materialization Work

Full runway-week materialization remains out of scope: EASY_SUPPORT/LONG_RUN slot population (RUN_LAYOUT_4D's 4-session weekly structure), volume/long-run-distance numeric progression, pace, and calendar assignment. This phase's progression documents represent each block's *defining/anchor* workout only (the narrow interpretation explicitly preferred by this phase's own prompt) — the same model AerobicStrength's own progression already uses.

## 24. Explicit Non-Implementation Statement

This phase implemented **no** allocator change, **no** allocation-policy or target-matrix change, **no** profile-eligibility change, **no** binder algorithm change (only a workout-reference *validator* bug fix, justified in section 12), **no** orchestrator wiring, **no** full block-sequence binding, **no** runway-week skeleton generation, **no** EASY_SUPPORT/LONG_RUN slot materialization, **no** volume/long-run/pace numeric progression, **no** calendar assignment, **no** public preview/confirm change, **no** persistence or migration, **no** 15-20 week activation, **no** General Endurance 21-52 staged-plan work, and **no** intermediate-distance projection.
