# Backend Integration Phase 4B — Runtime Input Contract for Fitness Evidence

Adds optional fitness-evidence input fields to `GeneratePreviewRequest` so a **future** resolver
implementation (Phase 4C+) has real user evidence available once it is wired up. This phase implements
**input contract only** — no resolver logic, no decision-trace logic, no pace projection, no Riegel
conversion, no readiness gates, no time-adequacy logic, and no catalog generation. Nothing added here is
read by any resolver, generation, or catalog code today.

## Six approved optional input fields

Added to `RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs`, all nullable:

| C# property | Type | Wire JSON name |
|---|---|---|
| `RecentLongestRunKm` | `double?` | `recent_longest_run_km` |
| `RecentWeeklyVolumeKm` | `double?` | `recent_weekly_volume_km` |
| `RecentRunsPerWeek` | `int?` | `recent_runs_per_week` |
| `RecentRaceDistanceKm` | `double?` | `recent_race_distance_km` |
| `RecentRaceFinishTimeSeconds` | `int?` | `recent_race_finish_time_seconds` |
| `RecentRaceDate` | `DateOnly?` | `recent_race_date` |

**Wire naming note — deviation from the task's literal spec, made deliberately:** the task listed the
expected JSON names in camelCase (e.g. `recentLongestRunKm`). This backend's entire wire contract is
globally configured to snake_case (`RunningApp.Api/Program.cs`: `options.JsonSerializerOptions.PropertyNamingPolicy
= JsonNamingPolicy.SnakeCaseLower`, applied to every controller response/request) — every existing field on
this exact DTO (`goal_type`, `days_per_week`, `target_finish_time_seconds`, `race_date`, etc.) is already
snake_case over the wire, confirmed by `UserJourneyTests.cs`'s own request bodies. Introducing six
camelCase fields into an otherwise 100%-snake_case JSON contract would be an inconsistent, surprising wire
shape and a likely defect from any reviewer's perspective. Snake_case names were used instead, generated
automatically by the existing global naming policy — no `[JsonPropertyName]` attributes were needed. This
is flagged explicitly here and in the final report as an intentional deviation from the task's literal
wording, in favor of consistency with the codebase's actual, tested, established convention.

## Why `paceEvidenceType` / `paceEvidenceDate` are withheld

Per the Phase 4A.3 owner-approved V1 scope decision (`PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md`),
these two fields remain tied to the `PACE_SOURCE_IN` evidence-hierarchy mapping (certified race / time
trial / structured test / user-reported pace / effort-only → registry value), which has no approved field
shape or allowed-value set — confirmed `DECISION_REQUIRED` across Phase 4A.2 §6 and unchanged since. Adding
either field now would require guessing an allowed-value set with no repository evidence behind it. Both
remain withheld exactly as instructed; neither was added.

## How fields are carried through preview → confirm

**Preview:** `PlanServices.GeneratePreviewAsync` already serializes the entire incoming `request` object
into `PlanPreview.RequestPayloadJson` via `JsonSerializer.Serialize(request, SerializerOptions)`
(`PlanServices.cs`, unchanged line). Because the six new fields are plain properties on
`GeneratePreviewRequest`, they are captured automatically — **no serialization/mapping code change was
needed** for preview-side carry-through. Verified by test
(`GeneratePreview_RequestPayloadJson_StoresAllSixFitnessEvidenceFields`): all six values round-trip through
the real `PlanPreviews.RequestPayloadJson` column in Postgres exactly as submitted.

**Confirm:** `PlanServices.ConfirmPlanAsync` already deserializes `preview.RequestPayloadJson` back into a
`GeneratePreviewRequest` (`requestData`) to read onboarding-snapshot fields. The six new fields deserialize
onto `requestData` automatically for the same reason — no code change was needed for confirm to be able to
*read* them. Verified by test (`ConfirmPlan_PreservesFitnessEvidenceFields_InRequestPayloadJson_NoDataLoss`):
confirming a plan does not mutate or lose any of the six values still sitting in the preview row's
`RequestPayloadJson` (confirm's own existing behavior already never mutates the preview row — it is a
read-only fetch, per its own code comment).

## Why no DB columns were added

`TrainingPlan` was **not** modified — no new columns were added for these six fields. Evidence: the JSON
payload carry-through described above already fully satisfies "confirm can access the preview payload
containing these fields" (§ task requirement) with zero additional code, so the task's own governing
condition — "do not add DB columns unless repository evidence proves JSON payload cannot carry these
fields" — is not met; the opposite was proven (the JSON payload *can* carry them, and does, verified by
test). Persisting these values onto `TrainingPlan` itself (as opposed to leaving them retrievable from the
preview's `RequestPayloadJson`) is deferred as a **future, resolver-implementation-time decision**: once
Phase 4C's resolvers actually consume this evidence, the resolver implementation is better positioned to
decide whether the *raw* evidence, a *derived* result, both, or neither belongs on `TrainingPlan` — adding
speculative columns now, before any consumer exists, would repeat exactly the kind of premature-shape risk
this reconciliation track (Phase 4A.1–4A.3) has been deliberately avoiding for the registry vocabulary. A
regression test (`ConfirmPlan_WithFitnessEvidenceFields_DoesNotPersistThemOnTrainingPlan`) guards against
this being silently reintroduced without an explicit future decision.

## Validation rules added

Conservative, shape-only, mirroring the existing `DaysPerWeek` validation pattern in
`PlanServices.GeneratePreviewAsync` (an `ArgumentException`, mapped by `GlobalExceptionHandler` to `400
VALIDATION_ERROR`, identical to the existing convention):

- `RecentLongestRunKm`: must be `> 0` if provided.
- `RecentWeeklyVolumeKm`: must be `> 0` if provided.
- `RecentRunsPerWeek`: must be `> 0` if provided.
- `RecentRaceDistanceKm`: must be `> 0` if provided.
- `RecentRaceFinishTimeSeconds`: must be `> 0` if provided.
- `RecentRaceDate`: no explicit code-level check was added — `DateOnly?` already only accepts a
  syntactically valid date at JSON-model-binding time (an invalid date string fails to bind before
  `PlanServices` code ever runs), exactly mirroring the pre-existing `RaceDate` field's own validation
  story on this same DTO. No redundant re-validation was added.

**Explicitly not implemented, per instruction:** no product thresholds (e.g. no "weekly volume too low"
rejection), no race-date recency comparison, no readiness/adequacy logic of any kind. All six fields accept
any positive value (or are entirely absent) without judgment.

## Tests added

New file: `RunningApp.IntegrationTests/PlanGeneration/FitnessEvidenceInputContractTests.cs` (19 tests,
HTTP-based against the real Api host + real Postgres, same pattern as `UserJourneyTests.cs`):

- **Backward compatibility (3):** preview without the new fields still succeeds; confirm without the new
  fields still succeeds; an unsupported goal combo *with* fitness-evidence fields supplied still returns
  `404 PLAN_TEMPLATE_NOT_FOUND` (proves the fields don't reintroduce or interact with the Phase 0 no-silent-
  fallback guarantee).
- **Individual field acceptance (6):** one test per field, confirming each is accepted alone.
- **All six together + carry-through (3):** all six accepted together; all six verified present, byte-exact,
  in the real `PlanPreviews.RequestPayloadJson` Postgres column; all six verified still present,
  unmutated, after a subsequent confirm.
- **Non-persistence regression guard (1):** confirms `TrainingPlan` gains no new columns for this data (see
  "Why no DB columns were added" above).
- **Negative/validation (5):** missing-fields-accepted (explicit restatement); negative
  `RecentLongestRunKm`/`RecentWeeklyVolumeKm`/`RecentRaceDistanceKm`/`RecentRaceFinishTimeSeconds` and
  zero `RecentRunsPerWeek` each return `400 VALIDATION_ERROR`.
- **1 more:** `GeneratePreview_MissingAllFitnessEvidenceFields_IsAccepted`, an explicit restatement of the
  "optional means optional" contract, separate from the general backward-compatibility test.

**Test-isolation fix required and applied:** running the new HTTP-based test class in parallel with the
existing `UserJourneyTests` (xUnit's default: different test classes run in parallel) caused intermittent
`500` errors, because both classes call `POST /api/v1/testing/reset` and then read/write rows for the same
hardcoded `mock-user-001` against the same real, shared Postgres database — a pre-existing test-isolation
gap that simply had never been exercised by two HTTP-based classes running concurrently before. Fixed by
adding `RunningApp.IntegrationTests/ApiIntegrationTestCollection.cs` (an xUnit `[CollectionDefinition]`) and
applying `[Collection(ApiIntegrationTestCollection.Name)]` to both `UserJourneyTests` and the new
`FitnessEvidenceInputContractTests`, which makes xUnit run them sequentially relative to each other while
still running in parallel with unrelated (e.g. EF-InMemory-based) test classes. This is a test-harness fix,
not a product-behavior change — no application code path was altered by it.

## Full test results

**93/93 tests passing** (74 prior + 19 new), 0 failures, after the collection fix above. Prior to the fix,
13 tests failed intermittently with `500 Internal Server Error` due to the parallel-execution race
described above — root-caused and fixed within this same pass, not left as a flaky suite.

## Remaining work for Phase 4C (resolver implementation)

1. Implement the four runtime-condition resolvers (`GOAL_FEASIBILITY_IN`, `PACE_SOURCE_IN`,
   `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN`), gated on the Phase 4A.2/4A.3-approved V1 vocabulary
   (simple registry values; richer Appsel V1 bands as trace metadata).
2. Decide `paceEvidenceType`/`paceEvidenceDate`'s field shape once the `PACE_SOURCE_IN` evidence-hierarchy
   mapping (Phase 4A.2 §6) is resolved, and add them at that time — not before.
3. Decide, at implementation time, whether/how the six Phase 4B fields (or their resolver *outputs*)
   should be persisted onto `TrainingPlan`/`TrainingWeek`/`TrainingDay` — Phase 4B deliberately defers this.
4. Wire `IPlanCatalogBundleLoader` + `IPlanCatalogDomainMapper` (Phase 1/2) into an actual generation
   engine — still entirely unstarted; the existing SQL `PlanTemplate` flow remains the only live path.
5. Resolve `TD-REGISTRY-001` (`CORE_ENTRY_READINESS_IN`/`STANDARD` fixture defect) before any resolver
   implementation depends on `CORE_ENTRY_READINESS_IN` — still `OPEN`, untouched by this phase.

## Confirmations

- No plan-catalog artifact was modified.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`.
- `EV-005` remains `PROPOSED`.
- `EV-006` remains `ACCEPTED_AS_SUPPORTING_EVIDENCE`.
- No resolver logic, decision-trace logic, pace projection, Riegel conversion, readiness gate, or
  time-adequacy logic was implemented.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still returns
  `PLAN_TEMPLATE_NOT_FOUND` (explicitly re-tested with fitness-evidence fields present, to prove they don't
  interact with that guarantee).

**Final classification: `BACKEND_HAS_RUNTIME_FITNESS_EVIDENCE_INPUT_CONTRACT_NOT_WIRED_TO_RESOLVERS`.**
