# Backend Integration Phase 2 — Catalog Vocabulary → Backend Domain Mapping

Analysis-only phase. `PlanCatalogDomainMapper` consumes Phase 1's `PlanCatalogCandidateSummary` and reports,
per catalog vocabulary concept, whether the **current** backend domain/DTO model can represent it — without
generating any `TrainingWeek`/`TrainingDay` and without being wired into `IPlanGenerationEngine`.

## What is representable today

- **Distance family** (`TEN_K` etc.) — `GoalDistance` enum already has an exact match for all 4 canonical families.
- **Days per week** (the *number*, not the layout's identity) — `TrainingPlan.DaysPerWeek` (int).
- **`LONG_RUN`** (slot role) — coincidentally exact: `TrainingDayType.LongRun` already exists and is used by all 3 seeded SQL templates for the same concept (though it's a workout-*type* value being reused for a slot-*role* concept, not a dedicated field).
- **`BUILD`, `TAPER`** (phase keys) — exact matches: `TrainingWeekType.Build`, `TrainingWeekType.Taper`.
- **Candidate key as text** — `TrainingPlan.TemplateId` (free string) could technically hold it, but that field's real semantic purpose today is the SQL `PlanTemplate.TemplateId` identity — reusing it would be a semantic overload, not a clean representation.

## What is still missing

| Concept | Support | Why |
|---|---|---|
| `KEY_SESSION`, `EASY_SUPPORT` (slot roles) | `NOT_SUPPORTED` | No SlotRole concept exists in backend at all — `TrainingDayType` is a workout-type enum, not a slot-role enum. |
| `FOUNDATION`, `RACE_SPECIFIC` (phase keys) | `REQUIRES_ENUM_EXTENSION` | `TrainingWeekType` is the right *category* of enum but has no exact member; `Base`/`Peak`/`RaceWeek` are unresolved loose candidates. |
| `GOAL_PACE_TEN_K`, `EASY_STANDARD`, `LONG_RUN_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO` (exact workout key+version identity) | `REQUIRES_NEW_FIELD` | No field anywhere preserves *which catalog workout* produced a `TrainingDay` — only its loose type family (if any) is inferable. |
| `GOAL_PACE_REHEARSAL`, `CURRENT_FITNESS_SPECIFIC_REHEARSAL` (progression stage keys) | `REQUIRES_NEW_FIELD` | Not even exposed by the Phase 1 loader summary (which stops at the `workoutProgression` reference, not its internal stages) — would require extending the loader *and* adding a field. |
| `RUN_LAYOUT_4D` (layout identity) | `REQUIRES_NEW_FIELD` | Its derived effect (`daysPerWeek=4`) is representable; the catalog reference itself is not. |
| `INTERMEDIATE` (level) | `NOT_SUPPORTED` | `RunningBackground {NewToRunning, UsedToRun, RunningRegularly}` is a 3-tier running-habit self-description; plan-catalog's `RunningExperience {NEW, INTERMEDIATE, ADVANCED, EXPERIENCED}` is a 4-tier training-level taxonomy. These are two different axes with no documented mapping — not merely a missing enum value. |
| `PEAK_VOLUME_BANDS_V1`, `RUNTIME_CONDITION_VALUES_V1` (policy/registry artifacts) | `REQUIRES_NEW_TABLE_OR_JSON` | No structural home at all — no field, no table. |
| `PACE_SOURCE_IN`, `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN`, `GOAL_FEASIBILITY_IN` (runtime condition value groups) | `NOT_SUPPORTED` | Zero references anywhere in backend source (reconfirmed by fresh grep in this phase) — not even declared, let alone resolved. Notably `GOAL_FEASIBILITY_IN` is the one condition type plan-catalog's own workout progression actually gates on (`GOAL_PACE_REHEARSAL.requires`), yet backend has no representation for it at all. |
| `RequestedTargetDistanceKm` (Phase 1 resolver output) | `REQUIRES_NEW_FIELD` | `TrainingPlan.GoalDistanceKm` already exists but is populated from a **fixed, family-representative constant** (`PlanServices.GetGoalDistanceInKm`: 5.0/10.0/21.0975/42.195 — even `GoalDistance.Custom` silently falls through to 5.0), never the user's exact requested distance. Repurposing it would break its existing meaning. |

Full classification table (22 concepts) with per-concept reasoning is in code (`PlanCatalogDomainMapper`) and exercised by `PlanCatalogDomainMapperTests`.

## What Phase 3 must implement

1. **Additive fields** (non-breaking, no data loss): `CatalogCandidateKey` (string), `CatalogCandidateVersion` (int), `CatalogWorkoutKey` (string), `CatalogWorkoutVersion` (int), `CatalogStageKey` (string), `CatalogPhaseKey` (string), `CatalogSlotRole` (string), `RequestedTargetDistanceKm` (double) — likely nullable, on `TrainingPlan` (candidate/distance-level) and `TrainingDay` (workout/stage/slot-level).
2. **Enum extension** for `TrainingWeekType` (add `Foundation`, resolve `RaceSpecific` vs. existing `Peak`/`RaceWeek` — a product decision, not just an engineering one) — or introduce a dedicated `CatalogPhaseKey` string instead of forcing plan-catalog's 4-phase model into the existing 6-value `TrainingWeekType`.
3. **New table or JSON column** for peak-volume-band policy and runtime-condition-registry data, if Phase 3 needs to *evaluate* those policies at generation time (not just record which catalog version was used).
4. **A genuine SlotRole concept**, decoupled from `TrainingDayType`, since `KEY_SESSION`/`EASY_SUPPORT` have no home today and `LONG_RUN`'s current "native" support is coincidental (workout-type enum reused for role).
5. **Runtime-condition resolvers** — all 4 condition groups need real backend logic (not just data structures) before `AllowGoalPaceRehearsal`/`GOAL_FEASIBILITY_IN`-gated stages can be evaluated at runtime; this connects directly to `TD-D3-001` and `TD-WAVE5-001` recorded in `plan-catalog/artifacts/audits/activation-readiness-risks.json`.
6. **Product decision on `Level`/`RunningExperience` mapping** — engineering cannot resolve this alone; needs an explicit decision on which (if any) `RunningBackground` value corresponds to catalog `INTERMEDIATE`, or whether a new axis is needed.

## Are EF/domain changes required before real generation?

**Yes, but none were made in this phase.** The fields above are all additive/nullable and would not break the existing SQL `PlanTemplate` flow — a future migration is low-risk in isolation — but this phase deliberately did not implement one, since (a) no code in this phase actually needs to persist any of this data yet (the mapper is a pure, stateless analysis function), and (b) the task's exact set of new fields should be decided once Phase 3's actual generation design is fixed, not speculatively now. **Migration plan for Phase 3** (not applied): add nullable `CatalogCandidateKey`/`CatalogCandidateVersion`/`RequestedTargetDistanceKm` to `TrainingPlan`, and nullable `CatalogWorkoutKey`/`CatalogWorkoutVersion`/`CatalogStageKey`/`CatalogSlotRole` to `TrainingDay` — all additive columns, no existing column altered, no data backfill required (existing rows get `NULL`).

## Confirmations

- No plan-catalog artifact was modified.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog (structurally proven: `PlanCatalogMappingResult` has no such properties, verified by test).
- Existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still returns `PLAN_TEMPLATE_NOT_FOUND` (proven by the still-passing, unmodified `CatalogNotWiredToGenerationTests` from Phase 1).
- No EF migration was added.
