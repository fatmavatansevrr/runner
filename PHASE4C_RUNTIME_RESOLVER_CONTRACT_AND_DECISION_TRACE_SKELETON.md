# Backend Integration Phase 4C — Runtime Resolver Contract and Decision Trace Skeleton

Adds the structural contract a **future** resolver implementation (Phase 4D+) will fill in: an input
snapshot model, a generic resolver output model, a decision-trace model, resolver interfaces, and a
read-only registry validation helper. This phase implements **structure only** — no resolver produces a
real decision, nothing is wired into live generation, and no `TrainingWeek`/`TrainingDay` is created.

## Resolver input snapshot shape

`RunningApp.Application/RuntimeCatalog/Resolvers/ResolverInputSnapshot.cs` — a pure, all-nullable data
carrier assembled from evidence that already exists in the request/preview pipeline: distance identity
(`RequestedTargetDistanceKm`, `CanonicalDistanceFamily`, `GoalType`, `GoalDistance`, `GoalDistanceKm`),
schedule identity (`StartDate`, `RaceDate`, `TargetFinishTimeSeconds`, `DaysPerWeek`, `Level`), and all six
Phase 4B fitness-evidence fields verbatim (`RecentLongestRunKm`, `RecentWeeklyVolumeKm`,
`RecentRunsPerWeek`, `RecentRaceDistanceKm`, `RecentRaceFinishTimeSeconds`, `RecentRaceDate`). No new API
field was added — every field here is sourced from an existing `GeneratePreviewRequest` field or Phase 1's
`CanonicalDistanceFamilyResolver` output. Building a snapshot performs no I/O and calls no resolver.

## Resolver output shape

`RunningApp.Application/RuntimeCatalog/Resolvers/RuntimeConditionResolutionResult.cs` — `ConditionType`,
`OutputValue`, `ReasonCode`, optional `InputSnapshot`, `Warnings`, `FallbackApplied`, optional
`ConfidenceLabel`, and a free-form `Metadata` dictionary. **Enforced by convention and tested against the
real registry file, not just documented:** `OutputValue` must be one of the actual
`runtime-condition-values.v2.json` allowed values for `ConditionType` — richer Appsel V1 bands
(`CONSERVATIVE`/`STRETCH`, pace-recency confidence, etc.) go in `Metadata`/`ConfidenceLabel`, never in
`OutputValue`. `RegistryValidationTests.cs` proves this isn't just a comment: `CONSERVATIVE`/`STRETCH`/
`CURRENTLY_UNSUPPORTED` and pace-confidence labels (`HIGH`/`MODERATE`/`LOW`) are asserted **not** valid
values for their respective condition types, read live from the actual registry file.

## Decision trace shape

`RunningApp.Application/RuntimeCatalog/Resolvers/ResolverDecisionTrace.cs` —
`ResolverDecisionTraceStep` (`StepIndex`, `ResolverKey`, `ConditionType`, `InputSnapshot`, `OutputValue`,
`ReasonCode`, `Metadata`, `Warnings`, `FallbackApplied`) mirrors the shape plan-catalog's own
golden-fixture-v3 decision trace already uses (ordered steps, facts, result). `ResolverDecisionTrace` wraps
an ordered list of steps. **Kept application-layer only — not exposed on any public DTO in this phase.**
Future exposure, when real resolver logic exists, should follow Phase 3's own DTO-exposure deferral
rationale: prioritize a detail/debug response (e.g. a future `TrainingDayDetailResponse`-style or
dedicated preview-diagnostics endpoint), not the broad `GeneratePreviewResponse`/`HomeResponse` payloads.

## Registry-simple / metadata-rich V1 scope decision (restated, not re-decided)

This phase implements, at the code level, the V1 scope Phase 4A.3 already owner-approved: runtime registry
values stay simple (`REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED`;
`ADEQUATE`/`COMPRESSED`/`INSUFFICIENT`; `NONE`/`RECENT_RACE`/`ESTIMATED`/`TARGET_TIME`;
`READY`/`CAUTION`/`NOT_READY`), and richer Appsel V1 detail lives in `Metadata`. Nothing about that
decision was re-opened, re-argued, or changed here — this phase only gives it a concrete type shape.

## Resolver interfaces

`RunningApp.Application/RuntimeCatalog/Resolvers/IRuntimeConditionResolver.cs` — a base
`IRuntimeConditionResolver` (`ConditionType`, `Resolve(ResolverInputSnapshot)`), four condition-specific
interfaces (`IGoalFeasibilityResolver`, `IPaceSourceResolver`, `ITimeAdequacyResolver`,
`ICoreEntryReadinessResolver`), and `IPlanModeResolver`, plus a composite
`IRuntimeConditionResolutionService.ResolveAll(ResolverInputSnapshot) : ResolverDecisionTrace`.

**Why `IPlanModeResolver` is included:** `PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md` §B
explicitly states "a future resolver implementation must evaluate `TIME_ADEQUACY_IN`,
`CORE_ENTRY_READINESS_IN`, and `PLAN_MODE_IN` together" for the sub-8-week / readiness-gated-compressed
case (`PLAN_MODE_IN.READINESS_ONLY`). This is direct, already-approved evidence that `PLAN_MODE_IN`
participates in resolver composition, satisfying the task's own conditional inclusion criterion.

**No production implementation exists for any of these interfaces.** Per the task's explicit menu of safe
placeholder strategies, this phase chose the lowest-risk option: **no concrete implementation ships in
`RunningApp.Application`/`RunningApp.Api` at all** (not even a `NotImplementedException`-throwing stub),
so there is nothing that could ever be accidentally registered in DI and explode — or worse, silently
succeed with wrong output — later. A contract-only fake implementation
(`ContractOnlyFakeResolutionService`) exists **only** in `RunningApp.IntegrationTests`, used solely to
prove `IRuntimeConditionResolutionService` is implementable and produces a well-formed trace; it is not
referenced by any production project and is never registered anywhere.

## Registry validation helper — added, not deferred

`RunningApp.Application/RuntimeCatalog/Resolvers/RuntimeConditionRegistryReader.cs` — a read-only loader
(same scan-and-match-by-parsed-metadata pattern as Phase 1's `PlanCatalogBundleLoader`) that parses a
`RUNTIME_CONDITION_VALUE_REGISTRY` document's `conditionValueSets` into a
`RuntimeConditionRegistrySnapshot` with an `IsValidValue(conditionType, value)` check. Chose **registry
loading** over a hardcoded pure validator specifically to avoid a second, drift-prone copy of the
registry's actual values living in backend source — the validator's answers are only ever as current as
the real `catalog/registries/*.json` file it reads. Registered in `Program.cs` as a scoped service (same
low-risk profile as `IPlanCatalogBundleLoader`) since it is genuinely useful read-only infrastructure —
**no resolver implementation is registered alongside it.**

## Why no resolver logic is implemented yet

Per this phase's own explicit scope boundary, and because the underlying product/registry questions are
not all settled: `GOAL_FEASIBILITY_IN`/`TIME_ADEQUACY_IN`/`PACE_SOURCE_IN` thresholds remain
`DECISION_REQUIRED` per Phase 4A.2 (only the `>=12 weeks → ADEQUATE` and `realisticMaxRatio=0.03`/
`challengingMaxRatio=0.06` boundaries have any fixture evidence at all), and `CORE_ENTRY_READINESS_IN`
remains blocked by `TD-REGISTRY-001` (the `STANDARD` fixture defect) until a golden-fixture-v4 correction.
Implementing real logic now would mean guessing thresholds this reconciliation track has repeatedly and
deliberately refused to guess.

## Why no generation is implemented yet

Independent of resolver readiness, Phase 1/2's `IPlanCatalogBundleLoader`/`IPlanCatalogDomainMapper` are
still not wired into `IPlanGenerationEngine` at all (confirmed unchanged since Phase 1 —
`ResolverNotWiredToGenerationTests` re-proves `TEN_K`/`INTERMEDIATE`/4-day still throws
`PlanTemplateNotAvailableException`). Generation requires both a working resolver layer *and* stage-to-week
scheduling/workout-generation logic, neither of which exists — this phase adds only the resolver
*contract*, one of several prerequisites still missing.

## How Phase 4D should implement actual resolver logic

1. Resolve the outstanding product decisions from Phase 4A.2 (§5 owner-decision list): exact
   `TIME_ADEQUACY_IN` week bands beyond `>=12`, `GOAL_FEASIBILITY_IN`'s upper `UNSUPPORTED` boundary,
   `PACE_SOURCE_IN`'s recency-ladder boundaries and evidence-hierarchy mapping, and
   `CORE_ENTRY_READINESS_IN`'s threshold scope (A/B/C/D).
2. Fix `TD-REGISTRY-001` (golden-fixture-v4 correcting `STANDARD`→`READY`) before implementing
   `ICoreEntryReadinessResolver` for real, or implement it against `READY`/`CAUTION`/`NOT_READY` with an
   explicit, documented decision on how to treat the still-`STANDARD` fixture in the meantime.
3. Implement each condition-specific interface (`IGoalFeasibilityResolver`, etc.) in
   `RunningApp.Application`, each producing a `RuntimeConditionResolutionResult` whose `OutputValue` is
   validated against `RuntimeConditionRegistrySnapshot.IsValidValue` before being returned — never invented
   ad hoc.
4. Implement `IRuntimeConditionResolutionService` for real, composing the four (or five, with
   `IPlanModeResolver`) individual resolvers into a `ResolverDecisionTrace`.
5. Register the real implementations in `Program.cs`, but do **not** wire them into
   `PlaceholderPlanGenerationEngine`/`PlanServices` in the same step unless full stage-eligibility/
   scheduling logic is also ready — wiring a resolver whose output nothing consumes yet is a smaller, safer
   increment than wiring it directly into `GeneratePreviewAsync`.
6. Only once resolver + scheduling + workout-generation are all ready should `TrainingWeek`/`TrainingDay`
   generation from the catalog be implemented, and only against the still-`DRAFT`
   `TEN_K__4D__INTERMEDIATE v10` candidate with that status explicitly surfaced, per Phase 3's
   `CatalogCandidateStatusAtGenerationTime` field.

## What remains blocked by TD-REGISTRY-001

`ICoreEntryReadinessResolver`'s real implementation, and any composition that depends on
`CORE_ENTRY_READINESS_IN` (including the `TIME_ADEQUACY_IN` 5–7 week readiness-gated-compressed case) —
unchanged from Phase 4A.1/4A.2/4A.3. This phase's `RegistryValidationTests.IsValidValue_CoreEntryReadiness_STANDARD_IsNotAValidRegistryValue`
test demonstrates the validator correctly rejects `STANDARD` for `CORE_ENTRY_READINESS_IN` (and correctly
accepts it for `PLAN_MODE_IN`) — this **demonstrates** the defect at the code level, it does **not**
close the risk. `TD-REGISTRY-001` remains `OPEN`, untouched by this phase.

## What remains out of scope

- Stage-to-week scheduling, workout generation, pace projection, Riegel conversion — none implemented.
- `GOAL_PACE_REHEARSAL` activation or any other catalog stage-eligibility check — untouched, still governed
  solely by the existing `requires: [{GOAL_FEASIBILITY_IN: [REALISTIC, CHALLENGING]}]` clause, which no
  code in this phase reads or evaluates.
- `paceEvidenceType`/`paceEvidenceDate` — still withheld (Phase 4A.3 scope decision), not added as fields
  anywhere, not represented in `ResolverInputSnapshot`.
- Any registry v3, golden-fixture-v4, or plan-catalog artifact change — none made.

## Confirmations

- No plan-catalog artifact was modified.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`.
- `EV-005` remains `PROPOSED`; `EV-006` remains `ACCEPTED_AS_SUPPORTING_EVIDENCE`.
- No resolver logic was implemented; no resolver was invoked from live generation (structurally guarded by
  `ResolverNotWiredToGenerationTests`'s constructor-dependency reflection checks, in addition to `Program.cs`
  registering no resolver implementation).
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still returns
  `PlanTemplateNotAvailableException`/`PLAN_TEMPLATE_NOT_FOUND`, re-tested with Phase 4B fitness-evidence
  fields present to prove no interaction.

**Final classification: `BACKEND_HAS_RUNTIME_RESOLVER_CONTRACT_AND_TRACE_SKELETON_NOT_WIRED_TO_GENERATION`.**
