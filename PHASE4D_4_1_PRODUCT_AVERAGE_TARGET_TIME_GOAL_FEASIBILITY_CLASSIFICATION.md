# Backend Integration Phase 4D.4.1 — Product-Average Target-Time GOAL_FEASIBILITY_IN Classification

Amends `GoalFeasibilityResolver` (Phase 4D.4) to fix acceptance test **SW-02**: a fully valid
10K/Intermediate/4-day/12-week race preview request — using the Flutter client's "Go with average"
canonical target time (3480s for 10K), with readiness evidence but no recent race — was rejected with
HTTP 422 (`RUNTIME_CONDITION_UNSUPPORTED`, `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`).

## The problem, precisely

`PaceSourceResolver` only ever produces `PACE_SOURCE_IN=TARGET_TIME` when a target finish time is present
and no complete recent-race evidence exists — it does not (and, per its own V1 scope, should not) know
*why* the target time exists. `GoalFeasibilityResolver`'s `TARGET_TIME` branch (Phase 4D.4, Section 6 of
its own task instruction — "target time is a user goal, not independent current-fitness evidence — never
validate it against itself") therefore treated every target time identically: `NotEvaluated`/
`PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`, classified `Unsupported` by `NotEvaluatedReasonClassifier`,
which `CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy` turns into a 422.

That Section 6 instruction is correct for a **user-typed** target time — a user's stated goal is not
evidence of their current fitness, and validating a claim against itself would be circular. It was never
evidenced (in the golden fixture, in any PHASE4D document, or anywhere else in this repository — confirmed
by a full-repository search for "product average"/"average pace"/"target_finish_time_source", zero hits
outside this doc and the code it accompanies) to also apply to the **product's own "go with average"
default**. That value is not a claim about the runner's fitness at all — it is Appsel's own canonical
planning reference for the selected goal distance (see `CanonicalTargetFinishTimePolicy`,
`backend/RunningApp.Application/Common/CanonicalTargetFinishTimePolicy.cs`), shown to every user who
declines to enter a custom time. Treating it identically to an unverified personal claim was a contract
gap (no way to distinguish the two), not a deliberate feasibility judgment — SW-02 exposed that gap.

## The decision (product/governance, not a code trick)

**Introduce `TargetFinishTimeSource` (`ProductAverage` | `UserDefined`), required on every Race request.**
When `PACE_SOURCE_IN=TARGET_TIME` and the source is `ProductAverage`, `GoalFeasibilityResolver` now returns
an **`Evaluated`** result — never `NotEvaluated` — with:

- `OutputValue = CHALLENGING` (never `REALISTIC`, which Section 7's Riegel-ratio classification reserves
  for a projection ≤3% off an *evidenced* recent-race pace — there is no evidence to compute a ratio from
  here; and never `UNSUPPORTED`, which for a `NotEvaluated`-adjacent case would just be a confusing label
  for a result the product itself recommends as the default and does not want rejected).
- `ReasonCode = "PACE_SOURCE_TARGET_TIME_PRODUCT_AVERAGE_ACCEPTED"` (new, stable, distinct from the
  unchanged `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`).
- `Metadata["ruleCode"] = "GOAL_FEASIBILITY_PRODUCT_AVERAGE_V1"`, `Metadata["targetFinishTimeSource"] =
  "PRODUCT_AVERAGE"`, `Metadata["governanceDoc"]` pointing back to this file — so any decision-trace
  consumer can see exactly which approved rule produced the result and why, not merely that it was
  accepted.

`TargetFinishTimeSource.UserDefined` (and `null`, e.g. any resolver-level test/caller that predates this
field) is **completely unchanged**: still `NotEvaluated`/`PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`.
The existing 422 safety behavior for an unverified user claim is not weakened in any way.

## What this decision explicitly does NOT do

- Does not treat a product-average target as demonstrated athlete capability — it is accepted as a
  **planning reference**, classified `CHALLENGING` (not `REALISTIC`) precisely to record that no evidence
  backs it.
- Does not require independent recent-race evidence merely because a product-average target exists.
- Does not change `UserDefined` behavior in any way — a user-typed target with no independent evidence
  still returns the existing typed 422.
- Does not silently infer source from the numeric value or from recent-race presence/absence — the source
  is an explicit, required field on the request, validated against the canonical value
  (`GenerateRacePlanPreviewRequestValidator`: `product_average` + a mismatched
  `target_finish_time_seconds` → HTTP 400, before this resolver is ever reached).
- Does not change `runtime-condition-values.v2.json` — `GOAL_FEASIBILITY_IN`'s four registry values are
  unchanged; this decision only adds a new, explicit path to reach one of the existing four (`CHALLENGING`).
- Does not change candidate `TEN_K__4D__INTERMEDIATE`'s `DRAFT` status.
- Does not add a database migration — `TargetFinishTimeSource` is a classification input, preserved in the
  `PlanPreview.RequestPayloadJson` snapshot for decision-trace/audit review, not a new relational column.

## Files changed

- `backend/RunningApp.Domain/Enums/TargetFinishTimeSource.cs` (new enum).
- `backend/RunningApp.Application/Common/CanonicalTargetFinishTimePolicy.cs` (new; single backend source of
  truth for the 4 canonical values, parity-tested against the Flutter client's `AverageFinishTimePolicy`).
- `backend/RunningApp.Application/RuntimeCatalog/Resolvers/ResolverInputSnapshot.cs` (+`TargetFinishTimeSource` field).
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs` (`BuildInputSnapshot` threads the field through).
- `backend/RunningApp.Application/RuntimeCatalog/Resolvers/GoalFeasibilityResolver.cs` (this decision, implemented).
- `backend/RunningApp.Application/DTOs/Plan/GenerateRacePlanPreviewRequest.cs` (new public race DTO, `target_finish_time_source` required).
- `backend/RunningApp.Application/Validation/GenerateRacePlanPreviewRequestValidator.cs` (source/value consistency check).

## Test coverage

- `GoalFeasibilityResolverTests`: product-average + no recent race → `Evaluated`/`CHALLENGING`/new reason
  code; user-defined + no recent race → unchanged `NotEvaluated` behavior; user-defined + valid recent race
  → normal Riegel path unaffected; metadata includes source + rule code + governance doc reference.
- `GenerateRacePlanPreviewRequestValidatorTests`: 10K+3480+product_average → valid; 10K+3600+product_average
  → 400; 5K+3480+product_average → 400; user_defined never coerced.
- `CanonicalTargetFinishTimePolicyTests`: backend/Flutter parity.
- `Sw02ProductAverageEndToEndTests`: the exact SW-02 payload → HTTP 200, 12 weeks, 4 sessions/week, 48
  sessions, decision trace shows the new reason code.

## Confirmations

- No plan-catalog artifact was modified.
- No runtime registry value was changed (`GOAL_FEASIBILITY_IN` stays `REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED`).
- No golden fixture was changed.
- `TD-PACESOURCE-001`/`TD-PACESOURCE-002` unaffected — `PaceSourceResolver`'s own output priority and
  `ESTIMATED`/`AsOfDate` gaps are untouched by this change.
- Candidate `TEN_K__4D__INTERMEDIATE` remains `DRAFT` in the real catalog; only the Development-only local
  acceptance override (pre-existing, unrelated to this change) treats it as published for local testing.

See `plan-catalog/artifacts/audits/activation-readiness-risks.md` (`TD-GOAL-FEASIBILITY-001`, closed by
this phase) for the tracked-risk cross-reference.
