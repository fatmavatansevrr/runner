# Phase 4G.6A.2A — 10K AerobicStrength Catalog Capacity, Second-Week Threshold and Transition-Bridge Resolution

Scope: `TEN_K__4D__INTERMEDIATE v10` only. Documentation, registry, and design-contract pass only. No runtime allocator, workout binding, catalog authoring, schema change, persistence change, or public HTTP behavior change was made in this phase.

## 1. Executive Result

**`TEN_K_AEROBIC_STRENGTH_TWO_WEEK_CAPACITY_AND_PRE_SPECIFIC_TRANSITION_BRIDGE_RESOLVED_WITH_THRESHOLD_FIVE_AND_PHASE_4G_6A_2_UNBLOCKED_FOR_DARK_ALLOCATOR`**

AEROBIC_STRENGTH is **retained** (Option A), its second-week threshold is **approved at `RunwayWeeks >= 5`**, and PRE_SPECIFIC_TRANSITION's remaining capacity gap is **resolved** using existing catalog content and a direct repository precedent. The allocation policy and full catalog *contract* (not yet the catalog *content*) are fully specified. Per this phase's own explicit permission ("Phase 4G.6A.3 may be unblocked for allocator implementation if the allocation policy and catalog contract are fully specified, even if the actual new workout definition is scheduled for Phase 4G.6A.4"), Phase 4G.6A.2 is unblocked for future dark-allocator design work. **Runtime activation remains blocked** until the AEROBIC_STRENGTH workout definition and schema extension described in section 9 are actually authored — that work is explicitly deferred to Phase 4G.6A.4, not implemented here.

## 2. Current Blocker Inherited from Phase 4G.6A.2

`TEN_K_PREPARATION_RUNWAY_POLICY_REMAINS_BLOCKED_BY_EXPLICIT_SEMANTIC_TYPED_SIGNAL_OR_CATALOG_CAPACITY_GAP`, with the exact blocker being `AEROBIC_STRENGTH`'s catalog-`UNSUPPORTED` status under either semantic interpretation, plus a disproven candidate second-week threshold (`RunwayWeeks >= 6`, actually `>= 5` under the described mechanism). This phase resolves both.

## 3. Pilot Scope

- `GoalType`: Race
- `RequestedTargetDistanceKm`: exact 10.0 km
- `CanonicalDistanceFamily`: `TEN_K`
- `DaysPerWeek`: 4
- `RunningBackground`: Intermediate
- Candidate: `TEN_K__4D__INTERMEDIATE v10`
- `PreferredCoreWeeks`: 12
- Direct Preparation Runway: 3-8 full weeks

No numeric decision in this document is generalized to any other candidate.

## 4. Keep-versus-Drop Analysis

**Option A (keep AEROBIC_STRENGTH)** requires `GENERAL_ENDURANCE.MaximumWeeks = 5` (already used and simulation-confirmed in Phase 4G.6A.2) plus a new, narrowly-scoped 2-week AEROBIC_STRENGTH content contract.

**Option B (drop AEROBIC_STRENGTH)** would require `GENERAL_ENDURANCE.MaximumWeeks = 7` — a strictly larger, entirely unevidenced capacity extension (no simulation, no catalog-safety argument, no precedent anywhere in the repository for a 7-consecutive-week undifferentiated easy-running block). It would also remove the runway's only differentiation for the `CORE_ENTRY_READY` profile, contradicting the source evidence direction that General Endurance should be "dominant," not "exclusive" — "dominant" presupposes at least one other block exists to be dominant over.

**Decision: Option A (KEEP).** It requires a smaller, already-verified `GeneralEndurance` capacity, preserves the profile's product differentiation, and its remaining gap (catalog content, not policy) is a bounded, well-specified, deferrable next step rather than an unevidenced capacity increase.

## 5. AerobicStrength Semantic Baseline

Unchanged from Phase 4G.6A.2, not reopened (no new repository evidence was found that contradicts it): running-based, non-race-specific, controlled aerobic-power/economy stimulus (controlled hills, strides, controlled fartlek, short aerobic intervals). Not gym/resistance/plyometric strength, not maximal sprinting, not race-pace rehearsal, not aggressive threshold loading. Confirmed again this pass: the exhaustive workout-content grep from Phase 4G.6A.2 remains accurate — direct re-read of `fartlek.v4.json`, `easy-standard.v4.json`, `long-run-standard.v4.json`, and `threshold-tempo.v4.json` (the four current-version workouts referenced by `ten-k-workout-progression.v5.json`) found no hill/stride/plyometric/resistance content in any of them.

## 6. Second-Week Threshold Decision

**Approved: `AerobicStrengthSecondWeekThreshold = RunwayWeeks >= 5`.**

Rationale: this is the value the intended generic constrained-proportional/floor/largest-remainder mechanism actually produces (Phase 4G.6A.2, `Phase4G6A2CoefficientSimulationTests.cs`, re-verified this pass, section 12 below), across every tested coefficient pair including an extreme control. Forcing threshold 6 instead would require either an explicit `RunwayWeeks == 6` special case (rejected — the task requires no horizon-specific branch) or a fundamentally different, not-yet-designed threshold-gated allocator mechanism (out of scope for this phase; allocator implementation is prohibited). Threshold 5 is simpler, requires no special-case code, and is the mechanism's own honest output.

Semantic coherence check for the resulting matrix (`GE2/AS2/T1` at week 5): AEROBIC_STRENGTH still caps at exactly 2 weeks (never exceeds its `MaximumWeeks=2`), remains strictly smaller than `GeneralEndurance` at every runway length except the shortest (week 3, where both are 1), and the block's total footprint (max 2 of up to 8 weeks) keeps it clearly a secondary, non-dominant qualifier rather than the runway's central content — consistent with its "requires semantic and readiness qualification" evidence classification, never promoted to primary.

**Classification: `PRODUCT_DEFAULT`.** This is explicitly not presented as an evidence-backed physiological threshold — no external literature was consulted or is claimed; it is the deterministic output of an internally-specified mechanism, chosen for consistency and simplicity over an arbitrary alternative.

## 7. One-Week and Two-Week Progression Contract

Design-only (catalog-only implementation deferred, section 9). The contract:

- **Objective**: introduce, then modestly extend, a controlled running-based aerobic-power/economy stimulus, strictly subordinate to the runway's dominant `GENERAL_ENDURANCE` volume, never approaching race-specific intensity.
- **Allowed intensity**: controlled hill surges, strides, controlled fartlek-style pace variation, short aerobic intervals — all sub-threshold, non-maximal.
- **Forbidden intensity**: any race-pace-dependent content, maximal-effort uphill running, aggressive threshold loading (i.e. `THRESHOLD_TEMPO`'s `THRESHOLD` intensity descriptor is explicitly excluded).
- **Weekly role composition**: exactly one AEROBIC_STRENGTH-type session per week; the remaining `DaysPerWeek - 1` sessions in that week are ordinary `EASY`/`LONG_RUN` content, mirroring the existing 4-day cadence's own single-quality-session-per-week pattern.
- **Week 1 → Week 2 progression**: Week 1 is an introductory, lower-volume/lower-repetition exposure (technique/economy emphasis, no volume increase from baseline easy running); Week 2 is the same stimulus family with a bounded, modest increase in controlled quality volume or repetition count — never a large or unsupported jump.
- **Relationship to GENERAL_ENDURANCE**: subordinate and occasional (1 session/week vs. GeneralEndurance's every-other-session easy/long-run content); does not compete for dominance.
- **Relationship to PRE_SPECIFIC_TRANSITION**: strictly sequential and non-overlapping — AerobicStrength's 1-2 weeks occur before Transition's fixed final week; Transition itself carries no AerobicStrength-type content (see section 10).
- **Relationship to Core Week 1**: AerobicStrength content is never present in Core's own `FOUNDATION_EASY_BASE` stage (which is `EASY_STANDARD` only, per `ten-k-workout-progression.v5.json`); no overlap or duplication is possible since the two occur in strictly sequential, non-overlapping calendar weeks.
- **Repeat behavior**: exactly the `minimumExposures=1 / maximumExposures=2` shape already used by the existing `FARTLEK_INTRO` stage (`ten-k-workout-progression.v5.json`, `BUILD` phase) — directly reused as precedent, not invented, for a stage whose real duration is also 1-2 weeks.
- **Required provenance/tagging**: the future workout document(s) must declare `family` (see section 9 for the recommended value) and `eligiblePhases` per the schema gap analysis in section 9; `intensityDescriptor` values (a free-form string field, not a closed enum — confirmed directly in `workout-definition.schema.json`) may freely encode e.g. `CONTROLLED_HILL_SURGE` or `STRIDES` without any schema change to that specific field.

## 8. Current Catalog Workout Audit

Direct re-inspection of the four current-version workouts referenced by the live 10K progression, plus `goal-pace-ten-k.v2.json`:

| Key | Version | Family | eligiblePhases | Components/Intensity | Race-specific? | Runway-compatible? | Classification |
|---|---|---|---|---|---|---|---|
| `EASY_STANDARD` | 4 | `EASY` | FOUNDATION, BUILD, RACE_SPECIFIC, TAPER | none (bare distance) | No | Content-wise yes, but expresses no aerobic-power stimulus | `INELIGIBLE` for AEROBIC_STRENGTH (no stimulus content); reused for TRANSITION (section 10) |
| `LONG_RUN_STANDARD` | 4 | `LONG_RUN` | FOUNDATION, BUILD, RACE_SPECIFIC, TAPER | none (bare distance) | No | Same as above | `INELIGIBLE` for AEROBIC_STRENGTH; candidate for TRANSITION (section 10) |
| `FARTLEK` | 4 | `QUALITY` | **BUILD only** | WARM_UP(EASY) → MAIN_SET(`SURGE_AND_FLOAT`) → RECOVERY(EASY_JOG) → COOL_DOWN(EASY) | No | Closest existing content match (pace-variation aerobic stimulus), but phase-restricted to `BUILD` only and never expresses hill/stride content specifically | `INELIGIBLE` as-is; `REQUIRES_NEW_WORKOUT_DEFINITION` for a runway-eligible variant |
| `THRESHOLD_TEMPO` | 4 | `QUALITY` | BUILD, RACE_SPECIFIC | WARM_UP(EASY) → MAIN_SET(`THRESHOLD`) → COOL_DOWN(EASY) | Effectively yes (threshold-tempo) | No — explicitly forbidden intensity per section 7 | `INELIGIBLE` |
| `GOAL_PACE_TEN_K` | 2 | (race-pace-gated) | RACE_SPECIFIC only, feasibility-gated | — | Yes (race-pace rehearsal) | No | `INELIGIBLE` |

**Conclusion: no existing workout definition can be reused as-is or with configuration alone for AEROBIC_STRENGTH.** `FARTLEK` is the closest directional match (a running-based, sub-threshold pace-variation stimulus) but is both phase-restricted (`BUILD` only, by the closed `eligiblePhases` enum) and content-mismatched (surge/float, not hill/stride/short-interval). **Classification: `REQUIRES_NEW_WORKOUT_DEFINITION`.**

## 9. Schema/Contract Gap

Direct re-read of `plan-catalog/schemas/workout-definition.schema.json` confirms:

- `family` is a **closed enum**: `EASY, LONG_RUN, QUALITY, RACE`. A new AEROBIC_STRENGTH workout would most naturally use `QUALITY` (matching `FARTLEK`'s and `THRESHOLD_TEMPO`'s own family) — no enum extension needed for this field.
- `eligiblePhases` is a **closed enum**: `FOUNDATION, BUILD, RACE_SPECIFIC, TAPER`. **None of these four values correctly describes the Preparation Runway period**, which is chronologically *before* `FOUNDATION` begins. Tagging a runway-only workout as `FOUNDATION`-eligible would be factually incorrect (Foundation is Core's own first phase, not the runway), and no other value fits either.
- `intensityDescriptor` (inside `components`) is a **free-form string**, not a closed enum — confirmed directly in the schema (`"intensityDescriptor": { "type": "string" }`). New descriptor values (e.g. `CONTROLLED_HILL_SURGE`, `STRIDES`) require no schema change.

**Outcome: D — a new workout definition AND a schema extension are both required** (specifically, extending the closed `eligiblePhases` enum with a value that correctly represents the Preparation Runway period, e.g. a new `PREPARATION_RUNWAY` value, or an equivalent runway-eligibility mechanism decided independently of this document). This is a narrow, precisely-scoped, catalog/schema-only change — no runtime code, no DI, no allocator. Per the Implementation Boundary's catalog-implementation default ("Otherwise record the exact catalog contract for the next phase"), this work is **not performed in this phase**: it is recorded here as the exact contract required, and is explicitly deferred to **Phase 4G.6A.4**. The reason it is deferred rather than done now: whether any future runway allocator will even reuse the `eligiblePhases`-based binding mechanism at all (versus a direct key-based binding specific to the dark runway contract layer) is itself an undecided architecture question that this phase — which is prohibited from designing the allocator — cannot resolve; authoring the schema change before that architecture decision risks guessing at the wrong extension shape.

## 10. PreSpecificTransition Audit

Direct re-inspection of `ten-k-workout-progression.v5.json`'s `TAPER` phase found `TAPER_SHARPEN` (`EASY_STANDARD`, `minimumExposures=1/maximumExposures=2`, `compressionBehavior=PROTECTED`, `extensionBehavior=FIXED_EXPOSURE`) — the Core plan's own final, fixed, protected pre-race week already reuses the exact same content-free `EASY_STANDARD` workout that `FOUNDATION_EASY_BASE` (Core's own first week) also uses. This is direct, existing repository precedent that a fixed, protected, boundary-position week does **not** require unique workout content to be a real, distinct structural role — `TAPER_SHARPEN` is not considered "just another Foundation week" or "a duplicate of Foundation" despite sharing identical workout content, because its distinctiveness comes from its position, fixed/protected exposure count, and (once volume progression is applied) its distinct volume trajectory, not from unique per-week content.

Applying the same reasoning: `PRE_SPECIFIC_TRANSITION` is content-supported via `EASY_STANDARD` (and optionally `LONG_RUN_STANDARD` for its long-run session), and is functionally distinct from `GENERAL_ENDURANCE` and from Core's own `FOUNDATION_EASY_BASE` by three properties, not content:

1. **Fixed, non-expandable position** (`MinimumWeeks = MaximumWeeks = 1`), immediately preceding Core — unlike `GENERAL_ENDURANCE`, which is expandable and volume-progressing.
2. **Volume trajectory**: `GENERAL_ENDURANCE` trends upward toward its own peak; `PRE_SPECIFIC_TRANSITION` is defined to hold or slightly settle volume rather than continue the upward trend, priming freshness for Core's own Foundation-phase progression to begin cleanly — the same settle/hold pattern `TAPER_SHARPEN` already applies at the opposite end of the plan.
3. **Sequential non-overlap with Core Week 1**: the runway and Core occupy strictly disjoint, non-overlapping calendar weeks by construction (the runway entirely precedes `RaceDate - PreferredCoreWeeks*7` days); Transition's single week is never the same calendar week as Foundation's Week 1, so there is no duplication of any single week, only similar (not identical-content-by-week) workout family use across adjacent, distinct weeks — exactly as `FOUNDATION_EASY_BASE` (weeks 1-N of Foundation) and `TAPER_SHARPEN` (the plan's final week) already share workout content without being considered duplicates of each other.

**Classification upgraded from Phase 4G.6A.2's `PARTIALLY_SUPPORTED` to `SUPPORTED`** (content-supported via existing `EASY_STANDARD`/`LONG_RUN_STANDARD`, functionally distinguished by position and volume trajectory, matching a direct, already-accepted repository precedent — not a label-only transition).

## 11. Core Week 1 Duplication Analysis

**No duplication found.** Three independent reasons, each sufficient on its own: (a) the runway and Core occupy strictly sequential, non-overlapping calendar weeks (section 10, point 3); (b) even where workout family content is shared (`EASY_STANDARD`), the repository's own `TAPER_SHARPEN`/`FOUNDATION_EASY_BASE` precedent establishes that shared content across non-adjacent structural roles is not "duplication" in this catalog's existing design vocabulary; (c) `PRE_SPECIFIC_TRANSITION`'s defined volume-settle trajectory (section 10, point 2) is the opposite of Foundation Week 1's own volume-build trajectory, giving them different computed content even under the shared workout family, once volume progression exists.

## 12. Final CORE_ENTRY_READY Target Matrix

Confirmed unchanged from Phase 4G.6A.2's simulation output (re-run this pass, section 15 below — identical result, 0 regressions):

| RunwayWeeks | GENERAL_ENDURANCE | AEROBIC_STRENGTH | PRE_SPECIFIC_TRANSITION |
|---|---|---|---|
| 3 | 1 | 1 | 1 |
| 4 | 2 | 1 | 1 |
| 5 | 2 | 2 | 1 |
| 6 | 3 | 2 | 1 |
| 7 | 4 | 2 | 1 |
| 8 | 5 | 2 | 1 |

All required invariants hold: sum equals `RunwayWeeks` at every row; `AerobicStrength`'s second week begins exactly at `RunwayWeeks=5`; `AerobicStrength` never exceeds its `MaximumWeeks=2`; `GeneralEndurance` is present (`>=1`) at every row and is the primary expandable block (reaches its own `MaximumWeeks=5` at the horizon's upper end); `PreSpecificTransition` is exactly 1 at every row; no explicit `RunwayWeeks`-specific branch exists anywhere in the simulation code (confirmed by direct source inspection of `Allocate(...)` — its only control flow is the generic 8-step loop, with zero week-count-literal comparisons); no block requires unsupported repetition (section 7's `1 session/week` cadence, `minimumExposures=1/maximumExposures=2` shape, both directly precedented); no week duplicates Core Week 1 (section 11).

## 13. Consistency-Needed Matrix Preservation

No repository evidence contradicts the Phase 4G.6A.2 `CONSISTENCY_NEEDED` matrix. It is **preserved unchanged**:

| RunwayWeeks | CONSISTENCY | GENERAL_ENDURANCE | PRE_SPECIFIC_TRANSITION |
|---|---|---|---|
| 3 | 1 | 1 | 1 |
| 4 | 1 | 2 | 1 |
| 5 | 2 | 2 | 1 |
| 6 | 2 | 3 | 1 |
| 7 | 2 | 4 | 1 |
| 8 | 2 | 5 | 1 |

## 14. Coefficient Consequence

Per Phase 4G.6A.2's own confirmed finding (re-verified this pass, unchanged): coefficient choice does not materially change either profile's outcome within the tested families. No new coefficient search was performed in this phase, consistent with the task's own explicit instruction not to search for a "scientifically optimal decimal." Simplest stable coefficient families, already proven (Phase 4G.6A.2 simulation) to produce the matrices in sections 12-13:

- `CONSISTENCY_NEEDED`: `Consistency = 0.40`, `GeneralEndurance = 0.60`.
- `CORE_ENTRY_READY`: `GeneralEndurance = 0.65`, `AerobicStrength = 0.35`.

Both pairs are directly among the three candidate pairs already simulated and confirmed in Phase 4G.6A.2 (candidate B in each profile) — no new simulation run was required to validate them; they were already proven to produce the exact matrices in sections 12-13.

## 15. Evidence/Product-Default Classification

| Decision | Classification |
|---|---|
| AEROBIC_STRENGTH semantic (running-based, not gym) | `EVIDENCE_INFORMED` (source-summary direction; catalog-content evidence for what does *not* exist) |
| AerobicStrengthSecondWeekThreshold = `RunwayWeeks >= 5` | `PRODUCT_DEFAULT` (mechanism-derived, not physiologically evidenced) |
| Coefficient pairs (0.40/0.60, 0.65/0.35) | `PROVISIONAL_PRODUCT_DEFAULT` (behaviorally immaterial per section 14; chosen for explainability only) |
| PreSpecificTransition content-reuse via `EASY_STANDARD`/`LONG_RUN_STANDARD` | `REPOSITORY_SUPPORTED` (direct `TAPER_SHARPEN` precedent, section 10) |
| One-week/two-week AerobicStrength progression shape (`min1/max2`, 1 session/week) | `REPOSITORY_SUPPORTED` (direct `FARTLEK_INTRO` precedent, section 7/8) |
| New AerobicStrength workout content itself | `DESIGN_INTENT_NOT_YET_PROVEN` — contract specified, content not authored (section 9) |

No exact weight, threshold, or capacity value in this document is classified `EVIDENCE_BACKED`.

## 16. TD Closure or Remaining Blocker

`TD-AEROBIC-STRENGTH-SEMANTICS-001` **remains `OPEN`** (not closed) — the catalog-content and schema-extension work it originally flagged is still not done, only fully specified and deferred (section 9). An addendum was appended (append-only, not a rewrite) recording: threshold 5 approved; keep-vs-drop resolved as KEEP; full progression contract specified; catalog outcome D deferred explicitly to Phase 4G.6A.4; and — critically — that the *allocation policy and contract* are now approved and no longer block Phase 4G.6A.3's allocator-design work, even though the *catalog content* remains outstanding and continues to block any runtime activation. See section 22 of the risk registry entry (JSON) for the exact addendum text.

## 17. Phase 4G.6A.2 Closure Status

**Phase 4G.6A.2 is now closed as resolved-and-unblocked** (superseding its own prior `BLOCKED` result), on the strength of this phase's resolution of both of Phase 4G.6A.2's named remaining gaps (AEROBIC_STRENGTH threshold/contract, PRE_SPECIFIC_TRANSITION capacity). Its original document is not rewritten; this document supersedes its verdict for downstream purposes and is the authoritative decision record going forward.

## 18. Phase 4G.6A.3 Readiness

Phase 4G.6A.3 (design of the actual dark, unwired, generic Preparation Runway allocator, still not wired to any live path) is **unblocked to begin** with: the two-profile model (section 8/section 12-13 of Phase 4G.6A.2, unchanged), both final target matrices (sections 12-13 above), both final min/max tables (Phase 4G.6A.2 section 12, with `AerobicStrength`'s values now confirmed still applicable), both final coefficient families (section 14), and the final priority/canonical-order metadata (Phase 4G.6A.2 section 18, unchanged) all fully specified. Phase 4G.6A.3 must still not implement any workout binding for `AEROBIC_STRENGTH` weeks until Phase 4G.6A.4's catalog work lands — its allocator design may reference the block generically (by key/capacity) but must not assume specific workout content exists yet.

## 19. Explicit Non-Implementation Statement

This phase implemented **no** production or dark runtime allocator, **no** runtime largest-remainder service, **no** DI wiring, **no** generated runway week or workout, **no** public preview or confirmation activation, **no** database migration, **no** `TrainingPlan`/`TrainingWeek` schema change, **no** volume/long-run progression change, **no** pace logic change, **no** calendar assignment change, **no** General Endurance staged-plan work, **no** 21-52 or 53+ behavior, and **no** intermediate-distance projection. No new workout-definition JSON file and no schema JSON file were created or modified — the AEROBIC_STRENGTH catalog content and `eligiblePhases` schema extension described in section 9 are design-specified only, explicitly deferred to Phase 4G.6A.4.
