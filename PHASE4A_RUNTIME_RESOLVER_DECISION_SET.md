# Backend Integration Phase 4A — Runtime Resolver Approved Decision Set

Documentation-only. No resolver, generation, or catalog wiring code was written in this pass. This
document exists to give Phase 4B a bounded, evidence-based starting point — not to implement anything.

## 1. Resolver summary table

| Resolver | Feeds condition | Catalog stages that depend on it (evidence) | Readiness |
|---|---|---|---|
| GOAL_FEASIBILITY_IN | `GOAL_FEASIBILITY_IN` | `TEN_K_WORKOUT_PROGRESSION_V1 v5`, stage `GOAL_PACE_REHEARSAL` (`requires: [{GOAL_FEASIBILITY_IN: [REALISTIC, CHALLENGING]}]`, `fallbackStageKey: CURRENT_FITNESS_SPECIFIC_REHEARSAL`) | BLOCKED_BY_PRODUCT_DECISION |
| PACE_SOURCE_IN | `PACE_SOURCE_IN` | None found — declared in `RUNTIME_CONDITION_VALUES_V1 v2` but no `requires` clause references it anywhere in `catalog/` (combinations, templates, layouts, level-modifiers, progression-modifiers, workout-progressions, rule-packs all grepped) | BLOCKED_BY_MISSING_INPUT |
| TIME_ADEQUACY_IN | `TIME_ADEQUACY_IN` | None found (same scope as above) | BLOCKED_BY_PRODUCT_DECISION |
| CORE_ENTRY_READINESS_IN | `CORE_ENTRY_READINESS_IN` | None found (same scope as above) | BLOCKED_BY_MISSING_INPUT |

**Evidence for "no stages depend on it" claim:** `grep -rn "conditionType" catalog/layouts catalog/templates catalog/level-modifiers catalog/progression-modifiers catalog/policies catalog/combinations` returned zero matches; the only `requires` clause anywhere in the catalog tree is the single `GOAL_FEASIBILITY_IN` one shown above, in `ten-k-workout-progression.v5.json` lines 72–75. `APPSEL_RACE_PLAN_V1 v4` (`catalog/rule-packs/appsel-race-plan.v4.json`) declares `"policies": []` and `"rules": []` — the rule pack that would normally encode thresholds for these conditions is empty. This means PACE_SOURCE_IN, TIME_ADEQUACY_IN, and CORE_ENTRY_READINESS_IN are registered vocabulary with no consumer yet in `TEN_K__4D__INTERMEDIATE`'s dependency graph — their resolvers are prerequisites for *future* catalog content, not for v10 as it exists today.

## 2. Input availability matrix

Checked against `GeneratePreviewRequest.cs`, `TrainingPlan.cs`, `UserProfile.cs`, `WorkoutLog.cs` (the only backend entities/DTOs found that could carry resolver inputs).

| Input | Exists today? | Where | Notes |
|---|---|---|---|
| Goal distance / requested target distance | ✅ | `GeneratePreviewRequest.GoalDistance` (enum), Phase 1 `CanonicalDistanceFamilyResolver` computes `RequestedTargetDistanceKm` from a `double` — but **no request field currently carries an exact custom distance in km**; `GoalDistance` is enum-only (`FiveK/TenK/HalfMarathon/Marathon/Custom`) | `Custom` has no accompanying km value anywhere in the request DTO |
| Canonical distance family | ✅ (derivable) | Phase 1 `CanonicalDistanceFamilyResolver.Resolve()` | Only usable once a km value exists to resolve from |
| Race date | ✅ | `GeneratePreviewRequest.RaceDate` (`DateOnly?`) | Nullable — may be absent |
| Available weeks (race date − start date) | ⚠️ derivable | `RaceDate` exists; plan start date is implicit (`StartedAt`/generation time), not an explicit request field | Computable, not stored |
| Recent weekly volume | ❌ | Not found anywhere | No entity/DTO carries self-reported or logged pre-plan weekly mileage |
| Recent longest run | ❌ | Not found anywhere | Same — no onboarding or history field |
| Recent race result | ❌ | Not found anywhere | `WorkoutLog` only logs results against an already-generated `TrainingDayId` (post-generation history), not a pre-generation recent-race input |
| Target finish time | ✅ | `GeneratePreviewRequest.TargetFinishTimeSeconds` (`int?`) | Nullable |
| Running background / level | ✅ | `GeneratePreviewRequest.Level` (`RunningBackground`: `NewToRunning/UsedToRun/RunningRegularly`) | Non-nullable enum, always present |
| Days per week | ✅ | `GeneratePreviewRequest.DaysPerWeek` (`int`) | Always present |
| Start date | ⚠️ | Not an explicit request field; `TrainingPlan.StartedAt` is set at confirm/generation time | Available at generation time, not at preview-request time |
| Preferred days | ✅ | `GeneratePreviewRequest.PreferredDays` (`string?`, JSON array) | Nullable |
| Long run day preference | ✅ | `GeneratePreviewRequest.LongRunDay` (`string?`) | Nullable |
| Preferred pace | ✅ | `GeneratePreviewRequest.PreferredPace` (`double?`, min/km) | Nullable — closest existing thing to a pace estimate, but is a *comfort* pace, not a race-pace estimate |

**Conclusion:** no backend entity or DTO captures recent weekly volume, recent longest run, or recent race result at all — this is a hard input gap, not a threshold gap, for any resolver that needs running history (`PACE_SOURCE_IN`'s `RECENT_RACE` value, `CORE_ENTRY_READINESS_IN` entirely).

## 3. Output vocabulary table

Sourced verbatim from `plan-catalog/catalog/registries/runtime-condition-values.v2.json` — no value invented.

| Condition | Allowed values (exact, from registry) |
|---|---|
| `GOAL_FEASIBILITY_IN` | `REALISTIC`, `CHALLENGING`, `UNSUPPORTED`, `NOT_REQUESTED` |
| `PACE_SOURCE_IN` | `NONE`, `RECENT_RACE`, `ESTIMATED`, `TARGET_TIME` |
| `TIME_ADEQUACY_IN` | `ADEQUATE`, `COMPRESSED`, `INSUFFICIENT` |
| `CORE_ENTRY_READINESS_IN` | `READY`, `CAUTION`, `NOT_READY` |

(Registry also defines `PLAN_MODE_IN` — `STANDARD/FOCUSED_CORE/COMPRESSED/READINESS_ONLY/COMPLETION_FOCUSED` — out of scope for this pass, not one of the 4 requested resolvers, noted only for completeness.)

---

## 4. Decision rules per resolver

### A. GOAL_FEASIBILITY_IN

**Purpose:** answers "is the user's stated goal (distance + target finish time, if any) realistic given their declared level?" Feeds `GOAL_FEASIBILITY_IN`. Consumed by `GOAL_PACE_REHEARSAL` stage — `REALISTIC`/`CHALLENGING` keep the goal-pace rehearsal workout; anything else falls back to `CURRENT_FITNESS_SPECIFIC_REHEARSAL`.

**Inputs:** `GoalDistance`/`RequestedTargetDistanceKm`, `TargetFinishTimeSeconds`, `Level` (`RunningBackground`), `DaysPerWeek`.

**Decision rules:**
- `REALISTIC` → DECISION_REQUIRED (no approved pace/level feasibility model exists in the repo)
- `CHALLENGING` → DECISION_REQUIRED
- `UNSUPPORTED` → DECISION_REQUIRED
- `NOT_REQUESTED` → the one rule inferable from field semantics without invention: if `TargetFinishTimeSeconds` is `null` (no explicit target time was requested), `NOT_REQUESTED` is the only value consistent with its name — but this is not confirmed by any repo document, so it is still marked DECISION_REQUIRED rather than asserted as approved.

### B. PACE_SOURCE_IN

**Purpose:** answers "where does the pace used for goal-pace workouts come from?" Feeds `PACE_SOURCE_IN`. No catalog stage currently depends on it (see §1).

**Inputs:** recent race result (❌ not available), `TargetFinishTimeSeconds` (✅ available), `PreferredPace` (✅ available, comfort pace not race-pace estimate).

**Decision rules:**
- `NONE` → DECISION_REQUIRED for the exact trigger condition; the only unambiguous case from field semantics is: no `TargetFinishTimeSeconds`, no recent race result (structurally always true today — the field doesn't exist), and no `PreferredPace`.
- `RECENT_RACE` → BLOCKED_BY_MISSING_INPUT — cannot ever be selected; no backend field carries a recent race result.
- `ESTIMATED` → DECISION_REQUIRED (would need to define what "estimated" means — from `PreferredPace`? From `Level`? Not specified anywhere.)
- `TARGET_TIME` → DECISION_REQUIRED for the exact condition, though the directionally obvious case is `TargetFinishTimeSeconds` present.

### C. TIME_ADEQUACY_IN

**Purpose:** answers "is there enough time between now/start and race date to safely execute the plan?" Feeds `TIME_ADEQUACY_IN`. No catalog stage currently depends on it (see §1).

**Inputs:** `RaceDate` (✅), plan start date (⚠️ implicit, not an explicit request field), `DaysPerWeek` (✅).

**Decision rules:**
- `ADEQUATE` → DECISION_REQUIRED (no approved minimum-weeks-for-10K threshold exists in any repo document)
- `COMPRESSED` → DECISION_REQUIRED
- `INSUFFICIENT` → DECISION_REQUIRED
- No week-count boundary of any kind is defined anywhere in `plan-catalog/` or `backend/` — `APPSEL_RACE_PLAN_V1 v4`'s `rules: []` is empty, confirming no threshold has been authored yet.

### D. CORE_ENTRY_READINESS_IN

**Purpose:** answers "is the user's current running fitness sufficient to safely enter this plan at all?" Feeds `CORE_ENTRY_READINESS_IN`. No catalog stage currently depends on it (see §1).

**Inputs:** recent weekly volume (❌ not available), recent longest run (❌ not available), `Level` (✅ available, only proxy).

**Decision rules:**
- `READY` → BLOCKED_BY_MISSING_INPUT — the two primary inputs (recent weekly volume, recent longest run) don't exist in the backend at all; `Level` alone is a coarse 3-value proxy with no approved mapping.
- `CAUTION` → BLOCKED_BY_MISSING_INPUT
- `NOT_READY` → BLOCKED_BY_MISSING_INPUT

---

## 5. Missing product decisions

1. Minimum/maximum weeks-until-race for `ADEQUATE` vs `COMPRESSED` vs `INSUFFICIENT`, specifically for the `TEN_K` family (no threshold exists in `APPSEL_RACE_PLAN_V1`'s empty `rules: []`).
2. What recent weekly volume (km/week) and/or recent longest run qualifies `READY` vs `CAUTION` vs `NOT_READY` — and, prior to that, **whether recent weekly volume / longest run will be collected as backend input at all** (currently no field exists).
3. How `Level` (`RunningBackground`: 3 values) should factor into `CORE_ENTRY_READINESS_IN` if volume/longest-run data remains unavailable — i.e., is `Level` an acceptable fallback proxy, or must the feature be blocked entirely until real history data is captured?
4. How target-time aggressiveness should be evaluated for `GOAL_FEASIBILITY_IN` (e.g., against `Level`, against a pace-per-km-per-level table) — no such table exists anywhere in the repo today.
5. How `RequestedTargetDistanceKm` (custom distances inside a family, e.g. 8K inside `TEN_K`) should affect `GOAL_FEASIBILITY_IN` — should an 8K request use different feasibility math than an exact 10K request? Undecided.
6. How "no pace input at all" should be handled for `PACE_SOURCE_IN` — is `PreferredPace` (a comfort pace) an acceptable substitute for `ESTIMATED`, or must `ESTIMATED` require a level-based pace table instead? Undecided.
7. Whether `RECENT_RACE` (`PACE_SOURCE_IN`) and any `CORE_ENTRY_READINESS_IN` value that depends on running history are in scope for the near-term roadmap at all, given no backend field currently captures that history — this is a scope/prioritization decision, not just a threshold decision.
8. Whether "start date" needs to become an explicit `GeneratePreviewRequest` field (for `TIME_ADEQUACY_IN`) rather than only existing implicitly as `TrainingPlan.StartedAt` at confirm time.

## 6. Safe default behavior

No behavior is implemented in this phase; the following is a **proposed** default policy for Phase 4B to approve or reject — none of it is approved yet.

| Missing input | Proposed safe default | Status |
|---|---|---|
| Missing recent race result | `PACE_SOURCE_IN` cannot resolve to `RECENT_RACE`; falls through to next available source | DECISION_REQUIRED (fallthrough order not approved) |
| Missing target time | `PACE_SOURCE_IN` cannot resolve to `TARGET_TIME`; `GOAL_FEASIBILITY_IN` likely `NOT_REQUESTED` (see §4A) | DECISION_REQUIRED |
| Missing recent weekly volume | `CORE_ENTRY_READINESS_IN` resolver must not silently default to `READY` — no-silent-fallback precedent (Phase 0) implies it should either throw/flag `UNKNOWN_FROM_CODE_EVIDENCE` at runtime or use the most conservative value (`CAUTION`/`NOT_READY`), never the most permissive | DECISION_REQUIRED (which conservative value, and whether "fail loudly" vs "conservative default" is the house rule) |
| Missing longest run | Same as above | DECISION_REQUIRED |
| No race date | `TIME_ADEQUACY_IN` cannot be computed; must not silently assume `ADEQUATE` | DECISION_REQUIRED (fail loudly vs. `INSUFFICIENT`-safe-default) |
| Invalid requested distance | Already handled and approved: Phase 1's `CanonicalDistanceFamilyResolver` throws `UnsupportedTargetDistanceException` (mapped to `400 UNSUPPORTED_TARGET_DISTANCE`) for `<=0` or `>42.2` km — this precedent (fail loudly, no silent coercion) is the strongest existing evidence for how missing/invalid resolver inputs should behave generally | READY (precedent only, not yet applied to the 4 new resolvers) |

The one **firm** precedent from existing code (Phase 0's removal of silent fallback, Phase 1's `UnsupportedTargetDistanceException`) is: **missing or invalid input must never silently produce a permissive/default output** — it must either fail loudly with a typed exception or resolve to the most conservative catalog value, never guessed. Which of those two strategies applies to each resolver is itself a DECISION_REQUIRED item, not something this pass may choose.

## 7. Decision trace proposal

Proposed shape (not implemented) for a future `ResolverDecisionTrace` record, one per resolver invocation:

```csharp
public sealed record ResolverDecisionTrace(
    string ConditionType,        // e.g. "GOAL_FEASIBILITY_IN"
    string OutputValue,          // one of the registry's allowedValues, or null if unresolved
    IReadOnlyDictionary<string, string?> InputsUsed,  // exact input values consulted, by name
    string ReasonCode,           // short machine-readable rule identifier, e.g. "NO_TARGET_TIME_NO_RECENT_RACE"
    string? FallbackBehavior,    // e.g. "FELL_THROUGH_TO_CURRENT_FITNESS_SPECIFIC_REHEARSAL", or null
    IReadOnlyList<string> Warnings // e.g. "recent weekly volume unavailable — used Level as proxy"
);
```

This mirrors the existing `PlanCatalogLoadException`/`PlanTemplateNotAvailableException` diagnostic-logging pattern from Phase 0/1 (structured reasoning captured, not a bare boolean) — proposed for consistency, not yet implemented or approved as final shape.

## 8. Implementation readiness classification

| Resolver | Classification | Why |
|---|---|---|
| GOAL_FEASIBILITY_IN | BLOCKED_BY_PRODUCT_DECISION | Inputs exist (distance, target time, level); zero thresholds/rules approved anywhere in the repo (`rules: []` in the rule pack) |
| PACE_SOURCE_IN | BLOCKED_BY_MISSING_INPUT | `RECENT_RACE` structurally unreachable (no field); remaining values also lack approved rules |
| TIME_ADEQUACY_IN | BLOCKED_BY_PRODUCT_DECISION | `RaceDate` exists; no week-count thresholds approved anywhere |
| CORE_ENTRY_READINESS_IN | BLOCKED_BY_MISSING_INPUT | Primary inputs (recent volume, recent longest run) don't exist in the backend at all |

No resolver is READY_TO_IMPLEMENT or PARTIAL_DECISION_SET — all 4 require at least one product decision, and 2 of the 4 additionally require new backend input fields that don't exist today.

## 9. Recommended Phase 4B scope

Given the above, Phase 4B should be a **product-decision-gathering pass**, not a code pass:
1. Get explicit product answers to the 8 items in §5, starting with #1 (time-adequacy thresholds) and #4 (feasibility model) since those two block the only resolver (`GOAL_FEASIBILITY_IN`) that a real catalog stage (`GOAL_PACE_REHEARSAL`) currently depends on.
2. Decide whether `RECENT_RACE`/history-dependent values are in scope before a "recent running history" input mechanism is designed and added to the backend (a separate, larger scoping decision — onboarding flow change, not a resolver change).
3. Once thresholds exist for `GOAL_FEASIBILITY_IN` specifically, a narrow Phase 4C could implement *only* that one resolver (since it's the only one with a live catalog consumer), leaving `PACE_SOURCE_IN`/`TIME_ADEQUACY_IN`/`CORE_ENTRY_READINESS_IN` for a later phase once their catalog consumers exist and their input gaps are resolved.

## 10. Items explicitly out of scope (this pass)

- No resolver was implemented.
- No threshold was invented or guessed.
- No `TrainingWeek`/`TrainingDay` was generated.
- No catalog artifact under `plan-catalog/` was modified.
- No plan generation wiring was added.
- Frontend/mobile implementation was not inspected (not needed — the input-availability question was fully answerable from backend DTOs/entities).

---

## Final report

**1. Files inspected:** `plan-catalog/catalog/registries/runtime-condition-values.v2.json`, `runtime-condition-values.v1.json`; `plan-catalog/catalog/rule-packs/appsel-race-plan.v4.json`; `plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v5.json`; grep across `catalog/layouts`, `catalog/templates`, `catalog/level-modifiers`, `catalog/progression-modifiers`, `catalog/policies`, `catalog/combinations` for condition-type references; `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs`, `GeneratePreviewResponse.cs`; `backend/RunningApp.Domain/Entities/TrainingPlan.cs`, `UserProfile.cs`, `WorkoutLog.cs`; `backend/RunningApp.Domain/Enums/RunningBackground.cs`, `GoalDistance.cs`; `backend/RunningApp.Application/Services/PlanServices.cs` (`GetGoalDistanceInKm`); `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogDomainMapper.cs` (Phase 2 reasoning references).

**2. Files changed:** One new file, `PHASE4A_RUNTIME_RESOLVER_DECISION_SET.md` (this document), at repo root. No code, migration, or plan-catalog artifact was modified.

**3. GOAL_FEASIBILITY_IN canonical values found:** `REALISTIC`, `CHALLENGING`, `UNSUPPORTED`, `NOT_REQUESTED` — read verbatim from `plan-catalog/catalog/registries/runtime-condition-values.v2.json`, no value invented.

**4. PACE_SOURCE_IN decision set:** Values `NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME` confirmed present in the registry as given in the task prompt. No thresholds approved for any of them. `RECENT_RACE` is additionally BLOCKED_BY_MISSING_INPUT (no backend field for recent race results).

**5. TIME_ADEQUACY_IN decision set:** Values `ADEQUATE`/`COMPRESSED`/`INSUFFICIENT` confirmed present in the registry. Inputs (`RaceDate`, implicit start date) largely available; zero week-count thresholds exist anywhere in the repo — `APPSEL_RACE_PLAN_V1 v4`'s `rules: []` is empty.

**6. CORE_ENTRY_READINESS_IN decision set:** Values `READY`/`CAUTION`/`NOT_READY` confirmed present in the registry. Blocked at the input layer, not just the threshold layer — recent weekly volume and recent longest run do not exist anywhere in backend entities/DTOs.

**7. Input availability matrix:** See §2 above. Available: goal distance (enum only, no custom-km field yet), race date, target finish time, running background/level, days per week, preferred days, long run day, preferred pace. Missing entirely: recent weekly volume, recent longest run, recent race result. Implicit-only: start date (exists as `TrainingPlan.StartedAt` at generation time, not as an explicit preview-request field).

**8. Missing product decisions:** 8 items listed in §5, the two highest-priority being (a) time-adequacy week thresholds for `TEN_K`, and (b) whether/how recent running-history data will ever be collected as backend input, since two of the four resolvers structurally cannot produce their full value set without it.

**9. Resolver readiness classification:** All 4 resolvers are blocked — `GOAL_FEASIBILITY_IN` and `TIME_ADEQUACY_IN` are `BLOCKED_BY_PRODUCT_DECISION` (inputs exist, thresholds don't); `PACE_SOURCE_IN` and `CORE_ENTRY_READINESS_IN` are `BLOCKED_BY_MISSING_INPUT` (thresholds AND some inputs are both missing). None are `READY_TO_IMPLEMENT` or `PARTIAL_DECISION_SET`.

**10. Recommended Phase 4B implementation scope:** Not a code phase — a product-decision-gathering phase, prioritizing `GOAL_FEASIBILITY_IN` thresholds first since it is the only resolver with a live catalog consumer (`GOAL_PACE_REHEARSAL` stage) in `TEN_K__4D__INTERMEDIATE` v10 today.

**11. Confirmation no generation was implemented:** Confirmed — no generation engine, resolver implementation, or wiring code was written.

**12. Confirmation no TrainingWeeks/TrainingDays were generated:** Confirmed — no runtime instance of either entity was created by this pass; only documentation was produced.

**13. Confirmation no plan-catalog artifacts were modified:** Confirmed — all plan-catalog files were read-only inspected (`registries/`, `rule-packs/`, `workout-progressions/`); none were edited.

**14. Anything not completed exactly as specified:** All requested sections were produced. One clarification on method: §1's "which catalog stages depend on it" was answered with direct negative evidence (grep across the full relevant catalog surface) for 3 of the 4 resolvers, since only `GOAL_FEASIBILITY_IN` has an actual `requires` consumer today — this is reported as a finding (registered-but-unconsumed vocabulary), not treated as a gap in the analysis itself.
