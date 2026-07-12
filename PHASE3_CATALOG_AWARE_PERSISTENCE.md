# Backend Integration Phase 3 — Catalog-Aware Persistence and DTO Contract Preparation

Adds the minimal additive backend persistence fields needed so a **future** generation phase can preserve
Process A provenance. This phase does not generate anything — no `TrainingWeek`/`TrainingDay` is created
from the catalog, and no code today populates any of the new fields.

## New fields

### `TrainingPlan` — full Process A bundle provenance (21 new nullable fields: 9 key/version pairs = 18 fields, plus `CatalogCandidateStatusAtGenerationTime`, `CanonicalDistanceFamily`, `RequestedTargetDistanceKm`)

`CatalogCandidateKey`/`Version`, `CatalogCandidateStatusAtGenerationTime`, `CatalogTemplateKey`/`Version`,
`CatalogLayoutKey`/`Version`, `CatalogLevelModifierKey`/`Version`, `CatalogWorkoutProgressionKey`/`Version`,
`CatalogProgressionModifierKey`/`Version`, `CatalogRulePackKey`/`Version`,
`CatalogPeakVolumeBandPolicyKey`/`Version`, `CatalogRuntimeConditionRegistryKey`/`Version`,
`CanonicalDistanceFamily`, `RequestedTargetDistanceKm`.

**Why the full bundle, not just combination/template/layout/modifier/rule-pack:** a `TrainingPlan` generated
from a catalog candidate is meaningless to audit or reproduce without knowing *exactly* which version of
every dependency (peak-volume policy, runtime-condition registry, progression modifier) it was generated
under — these can each version independently of the combination itself (see plan-catalog's own immutable
version-cascade discipline). Recording only the top-level combination key/version would silently lose which
exact peak-volume bands or runtime-condition vocabulary applied to a specific user's plan.

**Why nullable:** every field is optional so the existing SQL `PlanTemplate` flow is completely unaffected —
a legacy plan simply has all catalog fields `null`. No backfill, no required migration of existing rows.

**Why `CatalogCandidateStatusAtGenerationTime` is stored, not derived:** it is an explicit **snapshot**, not
a live reference — plan-catalog's `TEMPLATE_COMBINATION.metadata.status` can change after a plan is
generated (e.g. `DRAFT` → `PUBLISHED`) without retroactively altering already-generated plans. Sourced
directly from the Phase 1 loader, which was extended this phase to capture `metadata.status` (previously
unread).

**Why `RequestedTargetDistanceKm` is separate from `CanonicalDistanceFamily`:** `TrainingPlan.GoalDistanceKm`
already exists, but is populated from a **fixed, family-representative constant**
(`PlanServices.GetGoalDistanceInKm`: `FiveK`=5.0, `TenK`=10.0, `HalfMarathon`=21.0975, `Marathon`=42.195 —
even `GoalDistance.Custom` silently falls through to 5.0 today), never the user's actual requested distance.
An 8K request must never be recorded as 10K, and a 15K request must never be recorded as 21.1K —
`RequestedTargetDistanceKm` exists specifically to prevent that. Verified: `RequestedTargetDistanceKm=8.0`
coexists with `CanonicalDistanceFamily="TEN_K"` without either overwriting the other (test-proven).

### `CatalogBundleHash` — explicitly deferred, with evidence

No source file under `plan-catalog/catalog/` (the DRAFT source tree the Phase 1 loader reads) carries a
`metadata.contentHash` value — confirmed by direct inspection of
`catalog/combinations/ten-k-4d-intermediate.v10.json`. That field is only populated at publish/stamp time
(`CatalogStamper`) and stored under `plan-catalog/artifacts/`, not in the draft source tree the loader
reads. `TEN_K__4D__INTERMEDIATE v10` has never been published, so **no stable hash is available to the
loader today**. Deferred until either (a) the loader is extended to also read from a published
release/bundle, or (b) Process A publishes v10.

### `TrainingWeek` — phase identity

`CatalogPhaseKey` (nullable string). Not an enum extension: Phase 2 found `FOUNDATION` and `RACE_SPECIFIC`
have no exact `TrainingWeekType` member (`Base`/`Peak`/`RaceWeek` are unresolved loose candidates) — forcing
them into the existing 6-value enum would either lose information or require a premature, unreviewed product
decision about which value means what. `TrainingWeekType` is untouched and remains fully usable
(test-proven: a week with `WeekType=Taper` and `CatalogPhaseKey=null` round-trips unchanged).

### `TrainingDay` — workout/stage/slot identity

`CatalogWorkoutKey`, `CatalogWorkoutVersion`, `CatalogStageKey`, `CatalogSlotRole`, `CatalogWorkoutFamily`,
`CatalogIntensityKey` (all nullable). `TrainingDayType` is untouched — Phase 2 found `KEY_SESSION` and
`EASY_SUPPORT` have **no** slot-role equivalent in the existing type-only enum, and even where a loose
family match exists (e.g. `GOAL_PACE_TEN_K` ~ `Tempo`/`Interval`), `DayType` alone cannot preserve *which
exact catalog workout* produced a day. Verified: `CatalogWorkoutKey="GOAL_PACE_TEN_K"` and
`CatalogStageKey="GOAL_PACE_REHEARSAL"` round-trip exactly (test-proven), and `CatalogSlotRole` distinguishes
`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN` (test-proven, all three).

## `PlanPreview` — no new columns (evidence-based decision)

- `PreviewPayloadJson`/`RequestPayloadJson` can already carry full catalog provenance until confirm — they
  are unstructured JSON blobs; adding catalog fields to `GeneratePreviewResponse`/`GeneratePreviewRequest`
  in a future phase would automatically flow through with zero schema change to `PlanPreview` itself.
- `PlanPreview` is **not** queried by catalog candidate/version anywhere in current code — the only queries
  against it are `Id + InternalUserId` (confirm) and `InternalUserId` (test reset), confirmed by
  repository-wide grep.
- Adding `TrainingPlan`-style provenance columns to `PlanPreview` now would be **overbuild**: no code
  populates them, and the JSON-blob path already suffices for the confirm flow's actual need (deserialize →
  copy onto `TrainingPlan` at confirm time, exactly as the existing onboarding-snapshot fields already work).

## DTO/API — deferred, not exposed this phase

`GeneratePreviewResponse`, `HomeResponse`, `TrainingDayDetailResponse` were **not** changed. Every new field
would serialize as `null` today (nothing populates them), so exposing them now adds API surface with zero
functional value and risks premature client assumptions. They will be exposed once a future phase actually
populates them — most likely first on `TrainingDayDetailResponse` (an internal/detail DTO, not the broad
`HomeResponse` summary payload), matching the task's own "prioritize detail/debug DTOs" guidance.

## EF migration

`20260710072851_AddPlanCatalogProvenanceFields` — 28 `AddColumn` calls (21 on `TrainingPlans`, 1 on
`TrainingWeeks`, 6 on `TrainingDays`), all nullable, no destructive changes, no enum replacement, no
backfill. Applied to the `antigravity_dev` database via `dotnet ef database update` so the existing
integration test suite (which writes real `TrainingPlan`/`TrainingDay` rows against real Postgres) continues
to pass unmodified.

## Runtime condition resolvers — explicitly out of scope

Not implemented in this phase. A `TODO` comment was added directly in
`PlanCatalogDomainMapper.ClassifyRuntimeConditionGroups` naming the four resolvers a future generation phase
must implement (`GOAL_FEASIBILITY_IN`, `PACE_SOURCE_IN`, `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN`), and
explicitly instructing that an unresolved condition must fail loudly rather than fake/hardcode a value —
mirroring the no-silent-fallback precedent set in Backend Phase 0.

## Remaining work before runtime generation

1. Wire `IPlanCatalogBundleLoader` + `IPlanCatalogDomainMapper` into a real generation engine.
2. Implement the four runtime-condition resolvers (currently `NOT_SUPPORTED`, per Phase 2).
3. Implement actual stage→week/day assignment, load progression, taper handling.
4. Resolve the `INTERMEDIATE`/`RunningBackground` taxonomy mismatch (product decision, not engineering).
5. Expose the new catalog fields via DTOs once real values exist to populate them.
6. Address `TD-D3-001`/`TD-WAVE5-001` (`plan-catalog/artifacts/audits/activation-readiness-risks.json`).

## Confirmations

- No plan-catalog artifact was modified.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- Existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still returns
  `PLAN_TEMPLATE_NOT_FOUND` (unmodified `CatalogNotWiredToGenerationTests` still pass).
- No runtime condition resolver was invoked or faked.
