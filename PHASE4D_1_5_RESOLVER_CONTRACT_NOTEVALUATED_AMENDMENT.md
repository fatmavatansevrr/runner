# Backend Integration Phase 4D.1.5 — Resolver Contract Amendment: NotEvaluated Outcome

Amends the shared resolver contract (`RuntimeConditionResolutionResult`, `ResolverDecisionTraceStep`) to
support a `NotEvaluated` status alongside `Evaluated`, then adapts `TimeAdequacyResolver` to implement
`ITimeAdequacyResolver` on top of the amended contract. No new resolver (`PACE_SOURCE_IN`,
`CORE_ENTRY_READINESS_IN`, `GOAL_FEASIBILITY_IN`, `PLAN_MODE_IN`) was implemented. No resolver is wired to
live generation.

## Why `NotEvaluated` was added

Phase 4D.1's `TimeAdequacyResolver` faced a missing-evidence case (no `raceDate`/`startDate`) that the
Phase 4C `RuntimeConditionResolutionResult` contract had no way to represent — its `OutputValue` was a
required, non-null string that had to be registry-valid, and `TIME_ADEQUACY_IN`'s registry has no "not
evaluated" value. Phase 4D.1 worked around this with a resolver-local `TimeAdequacyResolutionOutcome`
wrapper type, and consequently could not implement the shared `ITimeAdequacyResolver` interface at all.

## Why this is a shared resolver concern, not TIME_ADEQUACY-specific

Every future resolver faces the identical structural problem: `PACE_SOURCE_IN` when there's no recent race
and no target time; `CORE_ENTRY_READINESS_IN` when weekly volume/longest run/runs-per-week are all absent;
`GOAL_FEASIBILITY_IN` when a prerequisite resolver's output itself is missing. Left unamended, each future
resolver would have invented its own answer — `null`, an exception, a fake registry value, its own
bespoke `Outcome` type, or simply skipping its interface — exactly the fragmentation this phase's task
description warned against. Fixing the shared contract once, before implementing the next resolver, avoids
four (or five, counting `PLAN_MODE_IN`) independent, inconsistent solutions to the same problem.

## Exact status semantics

`RuntimeConditionResolutionStatus`: `Evaluated` | `NotEvaluated`.

- **`Evaluated`**: the resolver produced a concrete decision. `OutputValue` is non-null and must be a
  member of the runtime-condition-values registry's allowed values for `ConditionType` (validated via
  `RuntimeConditionRegistrySnapshot.IsValid`, not by the result type itself — registry membership is a
  separate, file-backed check).
- **`NotEvaluated`**: the resolver could not produce a decision because **optional** evidence or a
  prerequisite was missing. `OutputValue` is always null. `ReasonCode` is still required and must explain
  why. `NotEvaluated` is not itself a registry value and is never looked up against the registry.

## `outputValue` validation rules — enforced by construction, not just documented

`RuntimeConditionResolutionResult` now has a **private constructor**; the only way to create an instance
is via the two static factories:

- `Evaluated(conditionType, outputValue, reasonCode, ...)` — throws `ArgumentException` immediately if
  `outputValue` is null/empty/whitespace. There is no code path that can produce
  `Status = Evaluated` with a null `OutputValue`.
- `NotEvaluated(conditionType, reasonCode, ...)` — always sets `OutputValue = null`; there is no parameter
  to pass an output value at all, so it is structurally impossible to produce
  `Status = NotEvaluated` with a non-null `OutputValue`.

`RuntimeConditionRegistrySnapshot.IsValid(RuntimeConditionResolutionResult result)` (new in this phase)
encodes the registry-lookup rule directly: `Evaluated` results are checked against the real registry file;
`NotEvaluated` results always return `true` from this check (there is nothing to look up — a null output
value is not a registry-membership question).

## Difference between `NotEvaluated` and the registry value `NOT_REQUESTED`

These are unrelated concepts that must never be conflated, and the contract now makes them structurally
distinguishable:

- `GOAL_FEASIBILITY_IN.NOT_REQUESTED` is a **registry value** — an `Evaluated` result
  (`RuntimeConditionResolutionResult.Evaluated("GOAL_FEASIBILITY_IN", "NOT_REQUESTED", ...)`), meaning the
  resolver successfully determined that feasibility doesn't apply (e.g. no target time was requested by
  design). It is registry-valid and passes `IsValid`.
- `Status = NotEvaluated` means the resolver **could not decide at all**, for any condition type, because
  required optional evidence or a prerequisite output was missing. It carries no output value.

A new contract test (`RuntimeConditionResolutionResult_NotEvaluated_IsDistinctFromGoalFeasibilityNotRequested`)
constructs both side by side and asserts their `Status` and `OutputValue` differ, specifically to prevent
this exact confusion from being introduced later.

## Validation layering (owner-approved clarification, applied here)

- **Frontend** is the primary UX layer for blocking invalid onboarding input (race-plan `startDate`/
  `raceDate` required and ordered correctly; optional fitness-evidence numeric fields non-negative if
  provided). Outside this repository's scope.
- **Backend defensive validation** remains exactly where Phase 4B already placed it: `PlanServices.GeneratePreviewAsync`'s
  `ArgumentException`-based checks for the five fitness-evidence fields' positivity, mapped to `400
  VALIDATION_ERROR` by the existing `GlobalExceptionHandler` convention. **Unchanged and not duplicated** —
  this phase added zero new validation to `PlanServices`; those five checks remain the single place that
  rejects negative optional-evidence values before anything reaches a resolver.
- **Resolver** models evaluation outcomes only, never request validation. `TimeAdequacyResolver` now
  explicitly assumes a race-goal-type snapshot's required fields (`StartDate`/`RaceDate`) have already been
  validated upstream — if it sees one missing anyway, it throws `ArgumentException` (fail loudly on
  apparently-invalid input reaching it), it does **not** return `NotEvaluated` for that case. `NotEvaluated`
  is reserved for genuinely optional, legitimately-absent evidence.

## `ResolverInputSnapshot` race/non-race context finding

**Repository evidence found an existing signal — no new field was needed.** `ResolverInputSnapshot.GoalType`
(a nullable `RunningApp.Domain.Enums.GoalType`, already present since Phase 4C) directly distinguishes
`GoalType.Race` from `GoalType.Habit`. This mirrors an existing, established backend pattern:
`PlanServices.ConfirmPlanAsync` already branches on `var isRace = previewData.GoalType == GoalType.Race;`
and, for the non-race branch, unconditionally sets `raceDate = null` — i.e., the codebase's own existing
behavior already treats "habit plan, no race date" as the normal, structural state, not a data-entry
omission. This is direct repo evidence, not an invented distinction. No new public API field or new
internal snapshot field was added for this purpose.

## `TimeAdequacyResolver`'s missing-date behavior after this pass

| `GoalType` | Missing `StartDate` and/or `RaceDate` | Both dates present |
|---|---|---|
| `Race` | Throws `ArgumentException` — invalid input reaching the resolver; frontend/backend validation should have already rejected it. | Evaluates normally. |
| `Habit` | Returns `NotEvaluated`, `ReasonCode = "NOT_APPLICABLE_NON_RACE_PLAN"` — the missing date is the expected, structural state for a habit plan, not omitted input. | Evaluates normally (GoalType only gates the missing-date branch). |
| `null` (unknown) | Returns `NotEvaluated`, `ReasonCode = "MISSING_PLAN_TYPE_CONTEXT"` — the resolver cannot assume a race-plan validation guarantee for a plan type it cannot identify, so it does not throw on an unconfirmed assumption. | Evaluates normally. |

`raceDate < startDate` (when both are present) still throws `ArgumentException` unconditionally,
regardless of `GoalType` — an inverted date range is nonsensical input, not a missing-evidence question.

**Phase 4D.1's evaluated behavior is unchanged**: boundary thresholds, output mapping
(`ADEQUATE`/`COMPRESSED`/`INSUFFICIENT`), reason codes, and all metadata fields (`availableWeeks`,
`availableDays`, `minimumCoreWeeks`, `defaultCoreWeeks`, `maximumCoreWeeks`, `roundingRule`) are byte-for-byte
identical to Phase 4D.1 — re-verified by the same boundary-case tests, now asserting against
`RuntimeConditionResolutionStatus.Evaluated` + `OutputValue` instead of the removed
`TimeAdequacyResolutionOutcome.Result`.

No `DECISION_REQUIRED` classification was needed for this pass — the `GoalType` field supplied sufficient
context for both branches the task asked about (race and non-race).

## How `TimeAdequacyResolver` now implements the shared interface

`TimeAdequacyResolver : ITimeAdequacyResolver`. `ConditionType => "TIME_ADEQUACY_IN"` (instance property,
backed by a `public const string ConditionTypeValue`). The base interface method
`Resolve(ResolverInputSnapshot) : RuntimeConditionResolutionResult` (inherited from `IRuntimeConditionResolver`)
is implemented but throws `NotSupportedException` with an explanatory message: it has no source for the
`PlanCatalogCoreCycle` the resolver actually needs, and nothing in production calls this single-argument
overload (the resolver is not wired anywhere). The real entry point,
`Resolve(ResolverInputSnapshot, PlanCatalogCoreCycle) : RuntimeConditionResolutionResult`, is what all
tests and any future caller should use. This is flagged as a known, temporary interface-shape gap — see
"Remaining work" below — not silently glossed over.

## Decision trace support

`ResolverDecisionTraceStep` now carries `Status` (required) and a nullable `OutputValue`, exactly mirroring
`RuntimeConditionResolutionResult`. A new `ResolverDecisionTraceStep.FromResult(stepIndex, resolverKey, result)`
factory builds a step directly from a resolver's result, so a step's `Status`/`OutputValue` can never drift
out of sync with the result it represents. `ResolverContractTests` proves a trace can mix `Evaluated` and
`NotEvaluated` steps in the same ordered list. Still application-layer only — not exposed on any public API
DTO; `TimeAdequacyResolverNotWiredToGenerationTests.GeneratePreviewResponse_HasNoDecisionTraceProperty`
re-confirms this structurally.

## Registry validation behavior

`RuntimeConditionRegistrySnapshot.IsValid(RuntimeConditionResolutionResult result)` (new): `Evaluated` →
delegates to the existing `IsValidValue(conditionType, value)` check against the real registry file;
`NotEvaluated` → always `true` (contract-valid by definition, never registry-looked-up). Tests confirm,
against the real `runtime-condition-values.v2.json`: `READINESS_ONLY` remains invalid for
`TIME_ADEQUACY_IN` and valid for `PLAN_MODE_IN` (unchanged from Phase 4D.1); a `NotEvaluated` result from
`TimeAdequacyResolver` passes `IsValid` without any registry lookup being meaningful for it.

## Tests added/updated

- `RuntimeConditionResolutionResult`/`ResolverDecisionTrace` contract tests (`ResolverContractTests.cs`,
  rewritten): Evaluated/NotEvaluated construction, the blank-`outputValue` guard, richer-metadata-not-output-value
  cases (unchanged from Phase 4C), and the explicit `NotEvaluated` vs. `NOT_REQUESTED` distinction test.
- `TimeAdequacyResolverTests.cs` (rewritten): interface-implementation check, all Phase 4D.1 boundary cases
  re-verified against the amended contract, race-plan missing-date → `ArgumentException`, habit-plan
  missing-date → `NotEvaluated`/`NOT_APPLICABLE_NON_RACE_PLAN`, unknown-`GoalType` missing-date →
  `NotEvaluated`/`MISSING_PLAN_TYPE_CONTEXT`, habit-plan-with-both-dates still evaluates normally, and
  registry-validation tests including the new `IsValid(result)` path.
- `TimeAdequacyResolverNotWiredToGenerationTests.cs` (updated): removed the now-deleted
  `TimeAdequacyResolutionOutcome` reference from the structural DTO-exposure guard.
- `ContractOnlyFakeResolutionServiceTests.cs` (updated): added the now-required `Status` field to each fake
  trace step.

## Full test results

**176/176 tests passing** (163 prior + net 13 new/changed). Zero regressions in any pre-existing test.

## Confirmations

- No plan-catalog artifact was modified.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`.
- `EV-005` remains `PROPOSED`; `EV-006` remains `ACCEPTED_AS_SUPPORTING_EVIDENCE`.
- No `PACE_SOURCE_IN`, `CORE_ENTRY_READINESS_IN`, `GOAL_FEASIBILITY_IN`, or `PLAN_MODE_IN` resolver logic
  was implemented — this phase touched only the shared contract and `TimeAdequacyResolver`.
- No resolver is registered in `Program.cs` or invoked from `PlanServices`/`PlaceholderPlanGenerationEngine`
  (re-confirmed by the existing reflection-based constructor-dependency tests, now also checking
  `ITimeAdequacyResolver`).
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still throws
  `PlanTemplateNotAvailableException`.

## How future PACE_SOURCE_IN / CORE_ENTRY_READINESS_IN / GOAL_FEASIBILITY_IN resolvers must use this pattern

1. Implement the condition-specific interface (`IPaceSourceResolver`, etc.) directly — no new
   condition-specific "Outcome" wrapper type should ever be created again; the shared
   `RuntimeConditionResolutionResult` now covers both cases.
2. Return `RuntimeConditionResolutionResult.Evaluated(...)` when a registry-valid decision can be made, or
   `.NotEvaluated(...)` when **optional** evidence is missing — never a fabricated registry value, never
   `null`, never a bespoke exception for the ordinary "evidence wasn't provided" case.
3. Reserve exceptions (`ArgumentException` or similar) for genuinely invalid input reaching the resolver —
   input that frontend/backend validation should already have rejected — not for absent optional evidence.
4. Before deciding whether a given missing-evidence case is `NotEvaluated` vs. a real registry value (e.g.
   `GOAL_FEASIBILITY_IN.NOT_REQUESTED`), check for existing repo/product evidence the way this phase did
   for `TimeAdequacyResolver`'s `GoalType` signal — do not invent the distinction without it.
5. Validate the final `Evaluated` result against a real `RuntimeConditionRegistrySnapshot.IsValid(result)`
   call before treating it as trustworthy — never assume a hand-written output string is registry-correct.

## Remaining work for Phase 4D.2+

1. Decide how (or whether) `PlanCatalogCoreCycle` should reach the base `IRuntimeConditionResolver.Resolve(ResolverInputSnapshot)`
   single-argument overload — currently `NotSupportedException`. Options include embedding core-cycle data
   directly onto `ResolverInputSnapshot`, or accepting that `TimeAdequacyResolver`'s real entry point is
   permanently the two-argument overload and the base interface method is simply unused for this resolver.
   Not decided in this pass.
2. Implement `PACE_SOURCE_IN`, `CORE_ENTRY_READINESS_IN`, `GOAL_FEASIBILITY_IN` resolvers using this now-shared
   pattern — none implemented here.
3. `CORE_ENTRY_READINESS_IN` implementation remains blocked on `TD-REGISTRY-001`.
4. The 5–7-week readiness-gated-compressed composition, and any `IRuntimeConditionResolutionService.ResolveAll`
   implementation composing multiple resolvers into one trace, remain unimplemented.
5. Live generation wiring remains entirely out of scope.

**Final classification: `BACKEND_HAS_SHARED_NOTEVALUATED_RESOLVER_CONTRACT_NOT_WIRED_TO_GENERATION`.**
