# PHASE 10K-FREQ.6D.4D.5E — Intermediate×5D Public Workout-Type Mapping Completeness Audit & Product Taxonomy Decision

**Type:** EVIDENCE + PRODUCT_DECISION
**Scope:** Evidence and product decision only. NO PRODUCTION CODE was touched in this phase.
**Parent phase:** FREQ.6D.4D.5D
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. Everything below is re-derived from the current repository state, not from memory of prior sessions.

---

## 0. Preflight

- `git rev-parse HEAD`: `30814c87ff7a695b1f6b87c201e2bcf5522eb2f1`
- `git branch --show-current`: `main`
- `git status --short` (non-build-output): `m baseline_tmp`, `M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.json`, `M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.md` — all three pre-existing, unrelated to this engagement, preserved untouched.
- `git rev-list --left-right --count origin/main...HEAD`: `0  10` — 10 commits ahead of `origin/main`, 0 behind. Consistent with the last Gate B push (`13594ac`) plus 5A/5B/5C/5D.
- `git diff --check`: clean (no conflict markers).
- `git merge-base --is-ancestor 7acb580 HEAD` → true. `4292687` → true. `30814c8` → true. All three `FREQ.6D.4D.5D` commits reachable from current `HEAD`.
- `PHASE_LEDGER.md` row 81 (`FREQ.6D.4D.5D`) re-read in full; final classification confirmed as `FREQ6D4D5D_TAPER_FIXED_PUBLIC_ACTIVATION_BLOCKED_ELSEWHERE`.
- `FREQ.6D.4D.5C` (`042dac7`/`d9adb9a`) and `FREQ.6D.4D.5D` reports re-read in full.

### Confirmed repository-truth facts (all 10, from ledger row 81 + fresh source reads this phase)

1. Multi-KEY calendar materialization is implemented (`FREQ.6D.4D.5B`, `CatalogWeekSkeletonCalendarMaterializer` generalized to `keyCount≥1`).
2. Both Taper validators (`CatalogPrescriptionContextValidator`, `CatalogFinalPrescribedPlanValidator`) are partitioned along the Legacy/ProfileBacked axis (`FREQ.6D.4D.5D`, `7acb580`).
3. Both real 5D Taper lanes (`TAPER_PRIMARY_STAGE`/`TAPER_SECONDARY_STAGE`) pass both validators without stage-name special-casing — verified again this phase by re-reading `Freq6D4D5DTaperCompletenessPartitionTests.cs` and confirming the `NoStageNameAllowListExists_ValidatorLogicHasNoFiveDayStageComparisons` test exists and its assertion is a literal comparison-pattern grep, not a presence check.
4. The malformed-Legacy counterexample from `5C` still fails (`MalformedLegacyClassifiedFiveDayStage_StillFails` test present).
5. Public activation reached its furthest point yet in `5D` — past calendar materialization and both Taper validators — before a third, independent blocker stopped it.
6. The current first blocker is `V1CatalogPublicWorkoutTypeMappingPolicy` (confirmed by reading `CatalogPublicPreviewMaterializer.cs:288-306` this phase — unchanged since `5D`).
7. The first known unmapped workout is `AEROBIC_STRENGTH_CONTROLLED_INTRO` (confirmed: no case for this key in the policy's `switch`; falls to the `default` arm, throwing `CatalogPublicWorkoutTypeUnsupportedException`).
8. `V1CatalogPilotIdentityPolicy` routing was reverted a third time — confirmed by reading the file this phase: `IsSupportedLevelFrequency`/`ResolveCandidate` do not include `(Intermediate, 5)`; the `FiveDayCandidateKey`/`Version` constants remain retained-but-unused with the full three-revert history in a doc comment.
9. `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic (direct consequence of fact 8).
10. Commits `7acb580`/`4292687`/`30814c8` are reachable from `HEAD` (verified in §0 above).

All 10 confirmed. Proceeding.

---

## 1. `PUBLIC_5D_WORKOUT_TYPE_MAPPING_FAILURE_TRACE`

Exact call chain, re-traced from source (`backend/RunningApp.Application/RuntimeCatalog/Schedule/CatalogPublicPreviewMaterializer.cs`):

```
CatalogPublicPreviewMaterializer.Materialize(request)
  → MapWeek(planStartDate, week)                                              [line 104]
    → MapSession(session, order)                                              [line 132]
      → WorkoutType = V1CatalogPublicWorkoutTypeMappingPolicy.Map(session)    [line 136]
        → switch on (session.WorkoutDefinitionKey, session.StructuralRole, session.ProgressionStageKey)  [line 294]
        → no arm matches ("AEROBIC_STRENGTH_CONTROLLED_INTRO", "KEY_SESSION", "FOUNDATION_PRIMARY_STAGE")
        → default arm throws CatalogPublicWorkoutTypeUnsupportedException  [line 303-304]
```

- **Caller identity:** `CatalogPublicPreviewMaterializer.MapSession`, a `private static` method invoked once per session while building the public preview payload for every week of a materialized plan.
- **Exact failing input:** `WorkoutDefinitionKey = "AEROBIC_STRENGTH_CONTROLLED_INTRO"`, `StructuralRole = "KEY_SESSION"`, `ProgressionStageKey = "FOUNDATION_PRIMARY_STAGE"`.
- **Key-only vs key+version:** the switch pattern is `(session.WorkoutDefinitionKey, session.StructuralRole, session.ProgressionStageKey)` — it does **not** reference `session.WorkoutDefinitionVersion` anywhere. The mapping is **key-only**, not version-aware (see §7 for the full analysis and why this is correct, not a bug).
- **Downstream consumption:** the mapped `GeneratedCatalogWorkoutType` is written into `GeneratedCatalogTrainingDayPayload.WorkoutType` (`CatalogPublicPreviewMaterializer.cs:135`), which becomes part of `GeneratedCatalogPlanPayload` — the actual public preview/confirmation DTO surfaced to the API layer and, eventually, the Flutter client's active-plan and calendar views. There is exactly one production call site of `V1CatalogPublicWorkoutTypeMappingPolicy.Map` (confirmed via `Grep` for `V1CatalogPublicWorkoutTypeMappingPolicy` — only referenced inside `CatalogPublicPreviewMaterializer.cs` itself; the other two files that reference the general policy-name string, `V1CatalogPilotIdentityPolicy.cs` and `PreparationRunwayPersistablePlanMapper.cs`, only mention it in doc-comment prose, not as a call site).
- **Failure mode:** the exception is unhandled at this layer — it propagates out of `Materialize` and (per the `5D` report's E2E finding) surfaces as an HTTP 500 to the public preview/confirmation endpoint. Fail-closed, not silent-default — consistent with the codebase's established no-silent-coercion convention.

---

## 2. Provenance and original semantic intent of `V1CatalogPublicWorkoutTypeMappingPolicy`

Read in full (`CatalogPublicPreviewMaterializer.cs:288-306`). The policy itself carries no doc comment recording its originating phase, and a repository-wide search for its `PolicyKey` string (`V1_CATALOG_PUBLIC_WORKOUT_TYPE_MAPPING_POLICY`) returns only this one definition site — no phase report or ledger row names it directly by key. Its authorship is therefore reconstructed from the enum it targets and its existing entries, not from an explicit design document.

Evaluating the candidate interpretations (A–G) against the actual mapping table:

| Interpretation | Fit | Reasoning |
|---|---|---|
| A. Athlete-facing family (what the runner sees labeled on their calendar) | **Best fit** | `GeneratedCatalogWorkoutType` doc comment (`GeneratedCatalogPlanPayload.cs:5-17`) explicitly frames the type as "a running-session day type" consumed by calendar/schedule UI, with a deliberate design choice ("no Rest member... enforced at the type level") oriented around what gets *displayed*, not internal structure. |
| B. UI display category | Same evidence as A — A and B collapse into the same real mechanism here; there is no separate UI-specific enum. |
| C. Analytics category | No evidence of a distinct analytics taxonomy; `GeneratedCatalogWorkoutType` is the only enum in the payload, used uniformly. |
| D. Physiological family | Partial fit only — `Tempo` vs `Interval` does track physiological intent loosely (see §7), but `Easy`/`LongRun` are distance-role categories, not physiological-only. |
| E. Structural role | Rejected — `StructuralRole` (`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN`) is already a separate, existing field on the session and is explicitly one of the switch's own *inputs*, not its output. Collapsing them would violate the "axes must stay separate" instruction in §9. |
| F. Historical API compatibility | No evidence of a pre-catalog-era public enum this had to match; `GeneratedCatalogWorkoutType` is itself Phase 4F.1-era, contemporaneous with the pilot's own routing. |
| G. Other | Not needed — A explains all five existing arms without contradiction. |

**Conclusion:** the policy's real intent is **A — an athlete-facing display/scheduling family**, a small, deliberately coarse taxonomy (5 values) meant to let the client render an appropriate icon/label/instruction template per session, not to encode exact physiological or structural detail. This directly informs §11's evaluation: a mapping that is "a little broader than the internal workout's precise identity" is not automatically wrong under this policy's own intent — it is the intent.

---

## 3. `PUBLIC_WORKOUT_TYPE_TAXONOMY_TABLE`

Full enum, read from `GeneratedCatalogPlanPayload.cs:18-25`:

| Public value | Serialized form (JSON, default enum-to-string) | DTO property | Current mapped WorkoutDefinition keys | Provenance | Stability |
|---|---|---|---|---|---|
| `Easy` | `"Easy"` | `GeneratedCatalogTrainingDayPayload.WorkoutType` | `EASY_STANDARD` (any role/stage) | Phase 4F.1 | Stable, canonical |
| `Interval` | `"Interval"` | same | `FARTLEK`, `GOAL_PACE_TEN_K` | Phase 4F.1 | Stable, canonical |
| `Tempo` | `"Tempo"` | same | `THRESHOLD_TEMPO` | Phase 4F.1 | Stable, canonical |
| `LongRun` | `"LongRun"` | same | `LONG_RUN_STANDARD` | Phase 4F.1 | Stable, canonical |
| `RecoveryEasy` | `"RecoveryEasy"` | same | *(none currently mapped to it anywhere in the codebase — confirmed via `Grep` for `RecoveryEasy` returning only the enum declaration and DTO-generation/serialization plumbing, no mapping-policy arm)* | Phase 4F.1 | Defined but currently unused by any mapping policy arm — a pre-existing gap unrelated to this phase's scope, disclosed here for completeness, not investigated further (out of scope: it does not block Intermediate×5D, since no reachable 5D workout is a recovery-run type). |

No `Strength`, `Hills`, `Drills`, or similarly named member exists in the enum. This is directly relevant to §8/§11: any W2 (new-type) candidate for `AEROBIC_STRENGTH_CONTROLLED_INTRO` would have to introduce a genuinely new enum member, since none of the existing 5 values is a semantic near-miss beyond `Interval`.

---

## 4. `CURRENT_CATALOG_PUBLIC_WORKOUT_MAPPING_TABLE`

Every entry currently in `V1CatalogPublicWorkoutTypeMappingPolicy.Map`'s switch, classified:

| # | Pattern (WorkoutDefinitionKey, StructuralRole, ProgressionStageKey) | → Public type | Classification | Reasoning |
|---|---|---|---|---|
| 1 | `("EASY_STANDARD", "EASY_SUPPORT", _)` | `Easy` | `CANONICAL_PRODUCT_MAPPING` | The universal easy-support default across every frequency/level combination that has ever existed in this codebase. |
| 2 | `("EASY_STANDARD", "KEY_SESSION", "TAPER_SHARPEN")` | `Easy` | `LEGACY_COMPATIBILITY_MAPPING` | Only reachable via the 3D/4D/Beginner×4D pilot's `V1_TAPER_SHARPEN_PRESCRIPTION_POLICY` (see `5C`/`5D`); the stage-key literal `TAPER_SHARPEN` never occurs in real 5D content. |
| 3 | `("EASY_STANDARD", "KEY_SESSION", _)` | `Easy` | `CANONICAL_PRODUCT_MAPPING` | General fallback for any other EASY_STANDARD KEY_SESSION; subsumes case 2 but case 2 is kept as an explicit, intentionally-redundant arm (harmless — both produce the same result, so no behavioral divergence, only a documentation redundancy). |
| 4 | `("LONG_RUN_STANDARD", "LONG_RUN", _)` | `LongRun` | `CANONICAL_PRODUCT_MAPPING` | Universal long-run default. |
| 5 | `("FARTLEK", "KEY_SESSION", _)` | `Interval` | `CANONICAL_PRODUCT_MAPPING` | Reachable by real 5D Build-Secondary and Taper-Secondary lanes (this phase's closure, §6) as well as legacy 3D/4D content. |
| 6 | `("THRESHOLD_TEMPO", "KEY_SESSION", _)` | `Tempo` | `CANONICAL_PRODUCT_MAPPING` | Reachable by real 5D Foundation-Secondary, Build-Primary, Race-Specific-Secondary lanes as well as legacy content. |
| 7 | `("GOAL_PACE_TEN_K", "KEY_SESSION", _)` | `Interval` | `CANONICAL_PRODUCT_MAPPING` | Reachable by real 5D Race-Specific-Primary and Taper-Primary lanes as well as legacy content. |

No entry is `PILOT_SPECIFIC` in the sense of "only valid for a pilot and wrong for the general product" — entry 2 is legacy-compatibility (narrow-scoped, still correct for its narrow scope) but not wrong. No entry is `PROVENANCE_UNKNOWN` — all 7 trace cleanly to either the universal defaults (1, 3, 4) or a real `QUALITY`-family workout with an unambiguous structural/physiological identity (5, 6, 7).

---

## 5. `INTERMEDIATE_5D_REAL_WORKOUT_CLOSURE`

Derived fresh this phase directly from the repository files (not from memory), following the real dependency chain:

`TEN_K__5D__INTERMEDIATE` (`plan-catalog/catalog/combinations/ten-k-5d-intermediate.v1.json`) → `layout = RUN_LAYOUT_5D v1` (`plan-catalog/catalog/layouts/run-layout-5d.v1.json`) → 5 slots/week: `KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN` → workout progression `TEN_K_WORKOUT_PROGRESSION_V1 v6` (`plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v6.json`) → 4 phases × 2 lanes → each stage's single `prescriptionProfileCandidates` entry → each profile's `workoutDefinitionRef` (read from all 8 `intermediate-5d-*.v1.json` profile files this phase).

| Phase | Lane | Stage key | Profile key | WorkoutDefinition key | Version |
|---|---|---|---|---|---|
| FOUNDATION | 0 | `FOUNDATION_PRIMARY_STAGE` | `INTERMEDIATE_5D_FOUNDATION_PRIMARY` | `AEROBIC_STRENGTH_CONTROLLED_INTRO` | 3 |
| FOUNDATION | 1 | `FOUNDATION_SECONDARY_STAGE` | `INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED` | `THRESHOLD_TEMPO` | 5 |
| BUILD | 0 | `BUILD_PRIMARY_STAGE` | `INTERMEDIATE_5D_BUILD_PRIMARY` | `THRESHOLD_TEMPO` | 4 |
| BUILD | 1 | `BUILD_SECONDARY_STAGE` | `INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED` | `FARTLEK` | 5 |
| RACE_SPECIFIC | 0 | `RACE_SPECIFIC_PRIMARY_STAGE` | `INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY` | `GOAL_PACE_TEN_K` | 2 |
| RACE_SPECIFIC | 1 | `RACE_SPECIFIC_SECONDARY_STAGE` | `INTERMEDIATE_5D_RACE_SPECIFIC_SECONDARY_CONTROLLED` | `THRESHOLD_TEMPO` | 4 |
| TAPER | 0 | `TAPER_PRIMARY_STAGE` | `INTERMEDIATE_5D_TAPER_PRIMARY` | `GOAL_PACE_TEN_K` | 3 |
| TAPER | 1 | `TAPER_SECONDARY_STAGE` | `INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED` | `FARTLEK` | 5 |

Plus the fixed default workouts for the layout's non-KEY slots (`EASY_SUPPORT` → `EASY_STANDARD`, `LONG_RUN` → `LONG_RUN_STANDARD`; confirmed as the universal defaults used everywhere in the codebase, not 5D-specific catalog content — no 5D-specific override file exists for these roles).

**Distinct WorkoutDefinition keys reachable in the real Intermediate×5D closure: 5** — `AEROBIC_STRENGTH_CONTROLLED_INTRO`, `THRESHOLD_TEMPO`, `FARTLEK`, `GOAL_PACE_TEN_K`, plus the two fixed defaults `EASY_STANDARD`, `LONG_RUN_STANDARD` (7 total including the fixed defaults).

---

## 6. Cross-reference against the mapping policy — exhaustive, not stop-at-first

| WorkoutDefinition key | Structural role(s) | Reachable in policy? | Classification |
|---|---|---|---|
| `AEROBIC_STRENGTH_CONTROLLED_INTRO` | `KEY_SESSION` | **No** — no arm matches | `UNMAPPED` |
| `THRESHOLD_TEMPO` | `KEY_SESSION` | Yes, arm 6 (`Tempo`) | `MAPPED_VALID` |
| `FARTLEK` | `KEY_SESSION` | Yes, arm 5 (`Interval`) | `MAPPED_VALID` |
| `GOAL_PACE_TEN_K` | `KEY_SESSION` | Yes, arm 7 (`Interval`) | `MAPPED_VALID` |
| `EASY_STANDARD` | `EASY_SUPPORT` | Yes, arm 1 (`Easy`) | `MAPPED_VALID` |
| `LONG_RUN_STANDARD` | `LONG_RUN` | Yes, arm 4 (`LongRun`) | `MAPPED_VALID` |

**Exact counts:** 6 distinct (key, role) pairs reachable in the real Intermediate×5D closure. **5 `MAPPED_VALID`, 1 `UNMAPPED`, 0 `MAPPED_BUT_SEMANTICS_REQUIRE_REVIEW`, 0 `NOT_PUBLICLY_SURFACED`, 0 `UNKNOWN`.**

The instruction to not stop at the first missing key is satisfied: all 8 stage/lane combinations (§5) and all 6 distinct reachable (key, role) pairs were checked against the policy's switch, and exactly one gap exists — `AEROBIC_STRENGTH_CONTROLLED_INTRO`. No other real 5D workout is unmapped.

**Final invariant status:** for 5 of 6 reachable (key, role) pairs, `ExactlyOneValidPublicWorkoutTypeMapping == true`. For 1 (`AEROBIC_STRENGTH_CONTROLLED_INTRO`, `KEY_SESSION`), it is currently `false`. The mapping closure **cannot yet be declared complete** — this is the exact, sole, resolvable gap this phase's decision (§11-§12) closes.

---

## 7. Key-only vs key+version mapping ownership

The switch pattern never inspects `session.WorkoutDefinitionVersion`. Auditing whether this is safe against the real closure: `THRESHOLD_TEMPO` is reachable at both v4 and v5 (both map to `Tempo`); `GOAL_PACE_TEN_K` is reachable at both v2 and v3 (both map to `Interval`); `FARTLEK` is reachable only at v5 in the 5D closure but at multiple versions elsewhere in the codebase (all mapping to `Interval` — confirmed via the same single-arm-per-key structure).

Cross-referencing with the `5C`/`5D` evidence and `PHASE_10K_FREQ_6D_4B_INTERMEDIATE_5D_PRODUCTION_PRESCRIPTION_PRODUCT_DECISION_CLOSURE.md` (§73-76 of that report, quoted): *"Does phase eligibility change workout identity/meaning? No — same physiological intent... in the new phase."* Every version bump reachable in the 5D closure (`AEROBIC_STRENGTH_CONTROLLED_INTRO` v1→v3, `THRESHOLD_TEMPO` v1→v5, `GOAL_PACE_TEN_K` v1→v3) was an `eligiblePhases` extension only — never a change to `family`, `components`, or intensity semantics. There is no case in the real catalog where two versions of the same key carry different athlete-facing meaning.

**Decision: mapping ownership is correctly key-only.** This is not a bug to fix — version-awareness would be a speculative, currently-unjustified generalization (the codebase's own convention, per CLAUDE-level guidance, is to not add mechanism ahead of evidence). If a future version bump ever *does* change a workout's physiological identity, that would be exactly the kind of "real bug found along the way" this phase's own instructions permit fixing — but no such case exists today.

---

## 8. `AEROBIC_STRENGTH_CONTROLLED_INTRO` — real semantic investigation

Read directly from `plan-catalog/catalog/workouts/aerobic-strength-controlled-intro.v3.json` and its profile `intermediate-5d-foundation-primary.v1.json` (§ full JSON already quoted in the investigation; summarized here):

- **`family`:** `QUALITY` (same family as `FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K` — confirmed via a repository-wide grep of every `workouts/*.json` file's `family` field; `EASY_STANDARD`/`LONG_RUN_STANDARD` are the only two keys with a different family, `EASY`/`LONG_RUN` respectively).
- **`eligiblePhases`:** `["PREPARATION_RUNWAY", "LONG_HORIZON_GENERAL_ENDURANCE", "FOUNDATION"]`.
- **Component structure (v3, `WARM_UP` → `MAIN_SET` → `COOL_DOWN`):** `MAIN_SET` is `structureMode: REPEATED`, 6 repetitions of a 30-second work bout at `CONTROLLED_AEROBIC_POWER_INTRO` intensity, with 90-second jog recovery between repetitions (`recoveryPlacement: BETWEEN_REPETITIONS`).
- **Real production authority (`PHASE_10K_FREQ_6D_4B_INTERMEDIATE_5D_PRODUCTION_PRESCRIPTION_PRODUCT_DECISION_CLOSURE.md`, decision D2, and its per-slot narrative):** *"Foundation Primary (FND-P): AEROBIC_STRENGTH_CONTROLLED_INTRO v3 (new, eligibility-extended), 6×30s controlled effort / 90s jog, EffortBased, CONTROLLED_AEROBIC_POWER_INTRO... Weakest-evidenced numeric slot in the matrix, disclosed as such, not hidden."* Its numeric dosage traces to *"adjacent evidence only (McMillan's beginner-fartlek 6-8×30s convention)"* — i.e., the real product authority itself already treats this workout as structurally and evidentially a close relative of fartlek-style short-repeat training, not a distinct "strength" discipline (e.g. gym-based resistance training, hill sprints with a different physiological target, or plyometrics) — despite the `AEROBIC_STRENGTH` name.
- **Explicitly rejected reasoning (per this phase's own prohibition):** the key contains the substring `STRENGTH`. Per the phase's explicit instruction, this is **not** used as evidence for any mapping decision below. The actual evidence used is the `family` field, the component structure, and the FREQ.6D.4B product-decision narrative — all of which independently converge on "short repeated-effort quality work," never on resistance/strength training in the conventional sense.
- **Foundation Primary's real training purpose** (from the same report, decision context around D1-D2): the Foundation phase's Primary lane exists to introduce controlled, short-duration higher-intensity efforts before the Build phase's sustained threshold work — an "aerobic power" introduction, i.e. a beginner-appropriate on-ramp to quality running, distinguished from Foundation Secondary's continuous `THRESHOLD_TEMPO` (Tempo) by being short-interval/repeated rather than continuous.

---

## 9. `AEROBIC_STRENGTH_*` family precedent

Two other members exist in the family: `aerobic-strength-controlled-progressed.v1.json`/`v2.json` (`family: QUALITY`, not reachable in the real Intermediate×5D closure — used by the Preparation Runway subsystem per `PHASE4G_6A_*` reports, out of scope for this phase's decision but relevant as internal precedent). Both share the identical `WARM_UP`/`REPEATED MAIN_SET`/`COOL_DOWN` shape as `aerobic-strength-controlled-intro`. No prior phase mapped any `AEROBIC_STRENGTH_*` key into `V1CatalogPublicWorkoutTypeMappingPolicy` — Preparation Runway content is never routed through this policy (confirmed: `PreparationRunwayPersistablePlanMapper.cs` is a separate mapper entirely, not delegating to `V1CatalogPublicWorkoutTypeMappingPolicy`'s `Map` method — only mentioning the general policy name in a doc comment). So there is no existing internal *mapping* precedent to defer to, only structural/family precedent, which is what §8 and §11 rely on.

---

## 10. Structural/physiological axis separation

Kept explicitly separate per the phase's own requirement:

| Axis | Value for `AEROBIC_STRENGTH_CONTROLLED_INTRO` in the 5D closure | Independent of |
|---|---|---|
| `StructuralRole` | `KEY_SESSION` | Public WorkoutType — role is an *input* to the mapping switch, never derived from it |
| `LaneOrdinal` | 0 (Foundation Primary) | Public WorkoutType — no lane-based branching exists or is proposed |
| `DoseCategory` | `PRIMARY` | Public WorkoutType — not referenced by the mapping switch at all |
| `ProgressionStageKey` | `FOUNDATION_PRIMARY_STAGE` | The chosen mapping arm uses a stage wildcard (`_`), so this axis is deliberately *not* consulted for this decision — consistent with `5C`'s finding that stage-name-based branching is the wrong axis for this kind of policy |
| `WorkoutDefinition` identity/version | `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3 | Kept as the sole switch key (with `StructuralRole`) — no version dependency (§7) |
| Physiological purpose | Short repeated controlled-effort aerobic-power introduction | The actual basis for the mapping decision in §11 |
| Public WorkoutType | *(currently unmapped — this phase's decision)* | — |

No axis was collapsed into another to reach the decision below.

---

## 11. Mapping option evaluation (W1–W4)

- **W2 (add new public type, e.g. `Strength`)**: rejected. Per §2, the enum's real intent is a small, coarse, athlete-facing display taxonomy — not an exhaustive structural catalog. Adding a member would require auditing and changing: the backend enum (`GeneratedCatalogWorkoutType`), its JSON serialization contract (a wire-visible addition — additive, not breaking, but still a real API surface change), any exhaustive `switch` over the enum (only one exists in production code — `SegmentTypeFor`/pace-mapping switches operate on different, unrelated enums; a repository-wide check found no other exhaustive consumer switch over `GeneratedCatalogWorkoutType` besides the DTO itself), Flutter/mobile models (currently **absent** — no Flutter model mirrors `GeneratedCatalogWorkoutType` yet, confirmed via `Grep`, because `TEN_K__5D__INTERMEDIATE` has never been live long enough for a client contract to be built against it), and analytics event schemas (none found referencing this enum). Given the taxonomy is intentionally coarse (§2) and a same-type reuse candidate exists with strong structural precedent (`FARTLEK`), W2's "strong evidence" bar is not met.
- **W3 (intentional many-to-one collapse)**: this *is* effectively what W1 already is for this taxonomy (multiple distinct WorkoutDefinitions already collapse into `Interval` and `Tempo` — this is the established, working pattern, not a new architectural decision). Not treated as a separate option here since W1 already captures it.
- **W4 (no valid mapping)**: rejected. There is no evidence this workout is publicly ambiguous or ownerless — it has a clear, real, product-decided identity (§8) and a clear structural sibling already mapped (`FARTLEK`).
- **W1 (map to existing type — `Interval`)**: **selected.** Evidence:
  1. Same `family: QUALITY` as every other `Interval`/`Tempo`-mapped workout, distinguishing it from `Easy`/`LongRun` family workouts (§4, §8).
  2. Structurally near-identical to `FARTLEK` — both use `WARM_UP` → `REPEATED MAIN_SET` (short work bouts + between-repetition recovery) → `COOL_DOWN`, the exact structural shape the existing `Interval` mapping already covers (`FARTLEK`'s own `MAIN_SET`: 10×60s work / 60s jog; `AEROBIC_STRENGTH_CONTROLLED_INTRO`'s: 6×30s work / 90s jog — same shape, different work:recovery ratio and effort target, which is exactly the kind of *within-type* variation the coarse taxonomy (§2) already tolerates for other `Interval`-mapped workouts).
  3. Physiological framing (§8) — "aerobic power introduction," a repeated-short-effort quality session — is a closer semantic match to "Interval" (repeated structured effort) than to "Tempo" (sustained continuous effort, which `THRESHOLD_TEMPO` alone represents in this taxonomy) or to `Easy`/`LongRun` (neither applies at all).
  4. Not misleading to the athlete: the session *is* a genuine interval-structured session (repeated efforts with recovery), consistent with what "Interval" already communicates for `FARTLEK` and `GOAL_PACE_TEN_K` (note `GOAL_PACE_TEN_K` is itself `CONTINUOUS`, not `REPEATED`, yet already maps to `Interval` — confirming the existing taxonomy already uses `Interval` as "structured quality work distinguished from sustained Tempo," not strictly "has repetitions." `AEROBIC_STRENGTH_CONTROLLED_INTRO` fits this precedent at least as well as `GOAL_PACE_TEN_K` already does).

**Selected mapping: `AEROBIC_STRENGTH_CONTROLLED_INTRO` → `GeneratedCatalogWorkoutType.Interval`, key-only (any `StructuralRole == "KEY_SESSION"`, any `ProgressionStageKey`), consistent with every other existing arm's wildcard-stage pattern.**

---

## 12. `REAL_5D_PUBLIC_WORKOUT_MAPPING_DECISION_TABLE`

| WorkoutDefinitionKey | ReachableVersions | Phase(s) | Lane(s) | CurrentPublicMapping | CandidatePublicTypes | SelectedPublicType | Existing/New | SemanticAuthority | EvidenceStrength | APIImpact | UIImpact | AnalyticsImpact | DecisionStatus |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `AEROBIC_STRENGTH_CONTROLLED_INTRO` | 3 | FOUNDATION | 0 | *(none — throws)* | `Interval` (W1), new `Strength`-like type (W2) | `Interval` | Existing | `FREQ.6D.4B` product decision + structural/family precedent (this phase) | Strong (family + structure + product-decision narrative all converge) | None (existing enum value, additive switch arm only) | None (no Flutter model exists yet for this dark route) | None (no analytics consumer found) | `DECIDED` |
| `THRESHOLD_TEMPO` | 4, 5 | FOUNDATION, BUILD, RACE_SPECIFIC | 0, 1 | `Tempo` | — | `Tempo` (unchanged) | Existing | Already canonical | Strong | None | None | None | `NO_CHANGE_NEEDED` |
| `FARTLEK` | 5 | BUILD, TAPER | 1 | `Interval` | — | `Interval` (unchanged) | Existing | Already canonical | Strong | None | None | None | `NO_CHANGE_NEEDED` |
| `GOAL_PACE_TEN_K` | 2, 3 | RACE_SPECIFIC, TAPER | 0 | `Interval` | — | `Interval` (unchanged) | Existing | Already canonical | Strong | None | None | None | `NO_CHANGE_NEEDED` |
| `EASY_STANDARD` | (fixed default) | all | `EASY_SUPPORT` role | `Easy` | — | `Easy` (unchanged) | Existing | Already canonical | Strong | None | None | None | `NO_CHANGE_NEEDED` |
| `LONG_RUN_STANDARD` | (fixed default) | all | `LONG_RUN` role | `LongRun` | — | `LongRun` (unchanged) | Existing | Already canonical | Strong | None | None | None | `NO_CHANGE_NEEDED` |

Only one row requires an actual change: `AEROBIC_STRENGTH_CONTROLLED_INTRO`. This confirms the "resolve every other unmapped 5D workout in the same pass" instruction is trivially satisfied — there were no other gaps to resolve.

---

## 13-16. UI, API, analytics, and cross-frequency/distance impact

- **UI impact:** none identified. No Flutter model, label table, icon map, or detail-screen switch currently exists for `GeneratedCatalogWorkoutType` (the type has never shipped to a live client — `TEN_K__5D__INTERMEDIATE` has been dark for its entire existence, and no other frequency/level combination newly depends on this decision). When a future phase does build that client contract, `Interval` already has an established UI treatment from existing 3D/4D `FARTLEK`/`GOAL_PACE_TEN_K` usage, so no new UI design is required by this decision.
- **API contract impact:** none. `GeneratedCatalogWorkoutType.Interval` already exists and is already serialized; adding one more internal-mapping arm does not change the wire schema (no new enum value, no field shape change). Wire-compatible by construction.
- **Analytics impact:** none found — no analytics/event pipeline consumer of `GeneratedCatalogWorkoutType` exists in this repository (searched; only DTO/serialization/test code references the type).
- **Cross-frequency (6D/7D) / cross-distance (HM/Marathon) generalization:** `AEROBIC_STRENGTH_CONTROLLED_INTRO` is referenced in exactly one workout-progression file in the entire catalog (`ten-k-workout-progression.v6.json`, confirmed via `Grep` across `plan-catalog/catalog/workout-progressions/`), used only by `TEN_K__5D__INTERMEDIATE`'s Foundation-Primary lane. Since the mapping is key-only and role-only (§7, §11) — never phase/frequency/distance-branched — the identical mapping decision applies automatically and without modification to any future 6D/7D or HM/Marathon content that reuses the same `WorkoutDefinition` key. No frequency- or distance-specific branching was introduced or is needed.

---

## 17-19. Architecture confirmation

- `V1CatalogPublicWorkoutTypeMappingPolicy` remains the **sole** mapping owner — confirmed via the single-call-site trace in §1. No duplicate mapping logic exists elsewhere (`PreparationRunwayPersistablePlanMapper` and `V1CatalogPilotIdentityPolicy` were checked and do not independently re-implement any workout-type mapping).
- The explicit key→type switch-table architecture is **not** proven structurally wrong by this investigation — the entire real 5D closure (6 reachable pairs) required exactly one new arm, wildcarded identically to every existing arm. Migrating to typed metadata on `WorkoutDefinition` (e.g. a `PublicWorkoutTypeHint` field in the JSON schema) would be a larger, currently-unjustified architectural change for a taxonomy that has needed exactly one addition across the entire 10K Intermediate×5D closure. **Rejected as premature** — no structural inadequacy was found, only a genuine, narrow, one-arm completeness gap.
- Fail-closed behavior for truly unmapped workouts is preserved by construction: the `default` arm's `throw` is untouched by this decision; nothing here introduces a silent fallback.

---

## 20-21. Zero-delta and no-silent-coercion

No existing mapping arm (1–7 in §4) is modified. The 3D/4D/Beginner×4D legacy mappings are completely unaffected — `AEROBIC_STRENGTH_CONTROLLED_INTRO` is not eligible for legacy default resolution (`eligibleForLegacyDefaultResolution: false` in its own definition file, and it is not reachable by any 3D/4D/Beginner×4D progression — confirmed via the same `Grep` used in §16) and no existing switch arm's pattern changes. No new silent default/fallback is introduced; the new arm is exact-match, just like every other arm.

---

## 22. Exact mapping decision (implementation authority for the next phase)

> **Catalog WorkoutDefinition key:** `AEROBIC_STRENGTH_CONTROLLED_INTRO`
> **Reachable version(s) in the real Intermediate×5D closure:** 3 (key-only mapping — any future version is covered identically per §7)
> **Public WorkoutType:** `GeneratedCatalogWorkoutType.Interval`
> **Existing or New:** Existing (no enum change)
> **Authority:** `FREQ.6D.4B` real production-prescription product decision (family=`QUALITY`, structural shape) + this phase's (`FREQ.6D.4D.5E`) structural-precedent-and-taxonomy-intent decision
> **Athlete-facing semantic explanation (one sentence):** "A short, controlled, repeated-effort quality session (six 30-second efforts with jog recovery) introducing structured interval-style running early in the training block."

---

## 23. `WORKOUT_TYPE_MAPPING_DECISION_MATRIX` (per gap — only one gap exists)

| Gap | Decision | Confidence | Residual risk |
|---|---|---|---|
| `AEROBIC_STRENGTH_CONTROLLED_INTRO` unmapped | Add `("AEROBIC_STRENGTH_CONTROLLED_INTRO", "KEY_SESSION", _) => GeneratedCatalogWorkoutType.Interval` as a new arm | High — converging family/structure/authority evidence, zero-impact reuse | Low — the only meaningful risk is a future product/UX reviewer preferring a more descriptive future taxonomy (e.g. distinguishing "power intervals" from "pace intervals"); nothing in current evidence forces that distinction, and it can be revisited additively later without breaking this decision |

---

## 24. Pre-activation completeness gate (contract for the next phase)

The next implementation phase must add an **exhaustive real-5D closure test** — enumerating all 8 stage/lane combinations from §5 (or re-deriving them fresh from the catalog files, the same way this phase did) and asserting `V1CatalogPublicWorkoutTypeMappingPolicy.Map` succeeds (does not throw) for a representative `CatalogPrescribedSession` built for each — **before** any fourth public-activation routing-widening attempt. This replaces the prior pattern of discovering gaps serially via live E2E requests (which is how this exact gap, and the two Taper gaps before it, were each found one at a time across `5B`/`5D`). A single new arm plus this one exhaustive test closes the gap this phase found; the test additionally guards against any *future* catalog content silently reintroducing an unmapped-workout gap.

---

## 25. Implementation contract for the next phase

Narrow, additive-only fix (no W2 path needed, so no multi-consumer contract is required):

1. Add exactly one new arm to `V1CatalogPublicWorkoutTypeMappingPolicy.Map`'s switch:
   ```csharp
   ("AEROBIC_STRENGTH_CONTROLLED_INTRO", "KEY_SESSION", _) => GeneratedCatalogWorkoutType.Interval,
   ```
   inserted alongside the existing wildcard-stage arms (5, 6, 7 in §4), preserving their ordering convention.
2. No other file needs to change: no enum change, no DTO change, no Flutter change, no migration, no config.
3. Add the exhaustive closure test described in §24.
4. Re-attempt the exact previously-reverted routing widening (`V1CatalogPilotIdentityPolicy`'s `(Intermediate, 5)` arm) as the fourth activation attempt, per §26.
5. If, and only if, a fourth genuinely independent blocker is found during that E2E retry, STOP again under the same discipline established in `5B`/`5D` — do not patch around it inside that phase.

---

## 26. Fourth activation-retry contract

Identical routing change as attempted three times before (`Split E`, `5B`, `5D`): widen `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency`/`ResolveCandidate` to include `(Intermediate, 5)`, reusing the already-retained `FiveDayCandidateKey`/`FiveDayCandidateVersion` constants. No routing redesign. The next phase must re-run the same real E2E dev-catalog-root probe methodology used in `5D` (since the `PublishedCatalogTestRelease` fixture-shape gap noted in `5D` was never conclusively resolved) to reach a genuine pass/fail verdict.

---

## 27. No-4D-silent-coercion

Unaffected — this decision touches no 3D/4D content or logic.

---

## 28. Test manifest for the next implementation phase (18 items)

1. `AerobicStrengthControlledIntro_KeySession_MapsToInterval` — direct unit test of the new arm.
2. `AerobicStrengthControlledIntro_AnyProgressionStageKey_MapsToInterval` — theory over multiple stage-key values (including a synthetic non-`FOUNDATION_PRIMARY_STAGE` value) proving the wildcard, not a stage-specific special case.
3. `AerobicStrengthControlledIntro_AnyWorkoutDefinitionVersion_MapsIdentically` — theory over v1/v2/v3, confirming key-only mapping per §7.
4-9. Six tests, one per real closure pair from §6 (`THRESHOLD_TEMPO`/`FARTLEK`/`GOAL_PACE_TEN_K`/`EASY_STANDARD`/`LONG_RUN_STANDARD`, plus the now-fixed `AEROBIC_STRENGTH_CONTROLLED_INTRO`), each asserting no exception is thrown for a realistic session built from real catalog values.
10. `IntermediateFiveDayRealClosure_Exhaustive_AllReachablePairsMapWithoutException` — the single exhaustive gate test from §24, covering all 8 stage/lane combinations from §5 in one assertion sweep.
11. `MappingPolicy_UnknownWorkoutKey_StillThrowsFailClosed` — regression proving the `default` arm is untouched.
12. `MappingPolicy_LegacyTaperSharpenArm_StillMapsToEasy` — zero-delta regression for arm 2 (§4).
13. `MappingPolicy_EasyStandardKeySessionNonTaperStage_StillMapsToEasy` — zero-delta regression for arm 3.
14-17. Four legacy 3D/4D/Beginner×4D full-plan materialization regression tests (reused from existing suites), confirming zero behavioral delta from this change.
18. Fourth-activation E2E test (per §26) — real dev-catalog-root probe confirming the public preview materializer no longer throws `CatalogPublicWorkoutTypeUnsupportedException` for any real Intermediate×5D session, across at least one full 8/10/12/14-week generation each.

---

## 29. Report structure note

This report intentionally does not enumerate 32 numbered top-level sections 1:1 with the phase prompt's own enumeration; several closely related requirements (UI/API/analytics impact, architecture confirmation, zero-delta) are grouped into combined sections (§13-16, §17-19, §20-21) where the underlying evidence is identical, to avoid repeating the same finding under multiple headings. Every substantive requirement from the phase prompt is addressed above.

---

## 30. Ledger and roadmap update

See `PHASE_LEDGER.md` row 82 and `MASTER_ROADMAP.md`, updated alongside this report. `FREQ.6D.4D` is **not** marked complete — the implementation (§25) remains for the next phase.

---

## 31. Push gate

Per this phase's explicit instruction, no push occurs merely because this decision closes. This is the 5th phase since the last Gate B push (`13594ac`): `5A, 5B, 5C, 5D, 5E`. Still under the ~10-phase threshold; commits remain local, ahead of `origin/main`, exactly as after `5D`.

---

## 32. Final classification

**`INTERMEDIATE_5D_PUBLIC_WORKOUT_MAPPING_CLOSURE_APPROVED`**

Rationale: the complete real Intermediate×5D workout closure was derived fresh from the repository (§5), cross-referenced exhaustively against the mapping policy (§6) — finding exactly one gap, not merely the one gap already known from `5D` — and that gap was resolved via option W1 (map to the existing `Interval` type) with strong, converging repository evidence (§8, §11), zero taxonomy/architecture change (§17-19), zero legacy delta (§20-21), and zero UI/API/analytics impact (§13-16). No taxonomy extension was needed (ruling out `PUBLIC_WORKOUT_TYPE_TAXONOMY_EXTENSION_APPROVED`), no architectural blocker was found (ruling out `PUBLIC_WORKOUT_MAPPING_BLOCKED_ON_TAXONOMY_ARCHITECTURE`), and the decision is fully resolved, not merely narrowed (ruling out `PUBLIC_WORKOUT_TYPE_MAPPING_REMAINS_DECISION_REQUIRED`). `INTERMEDIATE_5D_PUBLIC_WORKOUT_TYPE_MAPPING_APPROVED` was considered but rejected in favor of the closure-scoped classification, since this phase's distinguishing achievement is proving *completeness* (no other gap exists), not merely approving one mapping in isolation.

`TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic pending the next phase's implementation (§25) and fourth activation retry (§26).
