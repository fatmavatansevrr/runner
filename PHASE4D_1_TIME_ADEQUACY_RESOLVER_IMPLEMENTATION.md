# Backend Integration Phase 4D.1 — TIME_ADEQUACY_IN Resolver Implementation

Implements the first concrete runtime-condition resolver: `TimeAdequacyResolver`, producing
`TIME_ADEQUACY_IN` only. No other condition type's resolver logic was implemented. No `TrainingWeek`/
`TrainingDay` is generated. Not wired into `IPlanGenerationEngine`/`PlanServices`/
`PlaceholderPlanGenerationEngine`.

## Dynamic core-cycle source used (not hardcoded)

**Repository evidence found the exact dynamic source the task anticipated, so no `TD-TIMEADEQ-001` risk
was needed.** `plan-catalog/catalog/templates/ten-k-master.v6.json` already contains:

```json
"coreCycle": { "minimumWeeks": 8, "defaultWeeks": 12, "maximumWeeks": 14 }
```

This is read verbatim by a new `ReadCoreCycle` helper added to
`RunningApp.Application/RuntimeCatalog/PlanCatalogBundleLoader.cs`, and exposed on
`PlanCatalogCandidateSummary.CoreCycle` (a new `PlanCatalogCoreCycle(int MinimumWeeks, int DefaultWeeks,
int? MaximumWeeks)` record, in `PlanCatalogCandidateSummary.cs`). `TimeAdequacyResolver.Resolve` takes a
`PlanCatalogCoreCycle` as an explicit parameter — it never references `"TEN_K"`, `8`, `12`, or `14`
anywhere in its own source. The exact same resolver code will work unmodified for `FIVE_K`,
`HALF_MARATHON`, or `MARATHON` once a candidate for each of those exists and exposes its own `coreCycle`.

**Exact source field/path:** `templates/ten-k-master.v6.json` → `$.coreCycle.minimumWeeks` /
`$.coreCycle.defaultWeeks` / `$.coreCycle.maximumWeeks`. `maximumWeeks` is treated as optional at the
loader level (parsed only if present) since the task described it as "if available," even though it is in
fact present for `TEN_K_MASTER v6`.

**`TD-TIMEADEQ-001` was NOT added** — the guardrail's own preference ("prefer dynamic reading over creating
this risk") was satisfied; hardcoding was not used anywhere in this phase.

## `availableWeeks` calculation

`ResolverInputSnapshot` (Phase 4C) did not contain an `availableWeeks` field — it has `StartDate` and
`RaceDate` (both `DateOnly?`), from which `availableWeeks` is computed internally by
`TimeAdequacyResolver.Resolve`. No new public API field was added — `availableWeeks` exists only inside the
resolver's own computation and its output `Metadata`, never as a request/response DTO field.

**Rounding rule, documented explicitly per the task's "do not guess silently" instruction:**

```
availableDays = raceDate.DayNumber - startDate.DayNumber
availableWeeks = availableDays / 7   // C# integer division, truncation toward zero
```

Since `raceDate >= startDate` is enforced before this line runs (see "Invalid input" below),
`availableDays` is always `>= 0`, making C#'s integer-division truncation exactly equivalent to
`Math.Floor(availableDays / 7.0)`. This is recorded in the output metadata as
`roundingRule = "FLOOR_AVAILABLE_DAYS_DIV_7"` so the rule is inspectable from the resolver's own output,
not just from source code.

**Verified boundary cases (tested against the real loaded `TEN_K__4D__INTERMEDIATE v10` coreCycle):**
- 84 days (exactly 12 weeks = `defaultWeeks`) → `ADEQUATE`.
- 91 and 98 days (13 and 14 weeks) → `ADEQUATE`.
- 56 days (exactly 8 weeks = `minimumWeeks`) → `COMPRESSED`.
- 70 and 83 days (10 weeks; 11 weeks + 6 days) → `COMPRESSED`.
- 49 and 7 days (7 weeks; 1 week, below `minimumWeeks`) → `INSUFFICIENT`.

## Output mapping

| Condition | `outputValue` | `reasonCode` |
|---|---|---|
| `availableWeeks >= defaultCoreWeeks` | `ADEQUATE` | `MEETS_DEFAULT_CORE_DURATION` |
| `minimumCoreWeeks <= availableWeeks < defaultCoreWeeks` | `COMPRESSED` | `BELOW_DEFAULT_BUT_MEETS_MINIMUM_CORE_DURATION` |
| `availableWeeks < minimumCoreWeeks` | `INSUFFICIENT` | `BELOW_MINIMUM_CORE_DURATION` |

Exactly the three-value mapping the task specified — no additional branch, no `READINESS_ONLY`, no
compressed-readiness-override logic. This resolver answers only "is there enough calendar time," nothing
about whether the user is otherwise ready to start.

## Metadata emitted

`RuntimeConditionResolutionResult.Metadata`: `availableWeeks`, `availableDays`, `minimumCoreWeeks`,
`defaultCoreWeeks`, `roundingRule`, and `maximumCoreWeeks` (when the loaded template provides one — it does
for `TEN_K_MASTER v6`). `InputSnapshot` is also attached to the result for traceability. No richer
Appsel V1 product detail exists for `TIME_ADEQUACY_IN` beyond what Phase 4A.2/4A.3 already scoped (the
5–7-week readiness-gated band), and that band is explicitly **not** implemented in this phase (see below).

## Missing-date behavior — documented, not guessed

`ResolverInputSnapshot` fields are optional; a caller may supply neither, either, or both dates.

**Missing `raceDate`:** repository evidence (`PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md` §6,
"Safe default behavior" table) already states: *"No race date | TIME_ADEQUACY_IN cannot be computed; must
not silently assume ADEQUATE | DECISION_REQUIRED (fail loudly vs. INSUFFICIENT-safe-default)"* — i.e. the
repo explicitly forbids silently defaulting to `ADEQUATE`, and does not resolve which of "fail loudly" vs.
"safe default" is correct. Per the task's own Option C ("mark DECISION_REQUIRED") and the explicit
instruction "do not invent a registry value," `TimeAdequacyResolver.Resolve` returns a
`TimeAdequacyResolutionOutcome` with `Status = NotEvaluated`, `NotEvaluatedReasonCode = "MISSING_RACE_DATE"`,
and `Result = null` — never a guessed `ADEQUATE`/`COMPRESSED`/`INSUFFICIENT`, and never an invented registry
value like a fabricated `"NOT_REQUESTED"` (which isn't even in `TIME_ADEQUACY_IN`'s registry set).

**Missing `startDate`:** no distinct repository guidance was found for this specific case, but by the same
reasoning (calendar math requires both endpoints) the identical `NotEvaluated` treatment applies:
`NotEvaluatedReasonCode = "MISSING_START_DATE"`. If both are missing, `MISSING_START_DATE` is reported
first (arbitrary but documented ordering — either is equally "not evaluated").

**`raceDate` before `startDate`:** treated as invalid input, not a missing-evidence case — throws
`ArgumentException`, mirroring the existing validation convention already used throughout `PlanServices`
(e.g. the `DaysPerWeek` check, and Phase 4B's five fitness-evidence positivity checks), all of which use
`ArgumentException` for malformed input rather than returning a "best guess" result.

**Why a new wrapper type instead of reusing Phase 4C's `ITimeAdequacyResolver`/`RuntimeConditionResolutionResult`
directly:** `RuntimeConditionResolutionResult.OutputValue` is a required, non-null string that must be
registry-valid — there is no registry value meaning "not evaluated." Forcing a return through that type
for the missing-date case would require either inventing a fake registry string (forbidden) or throwing an
exception even for the ordinary, expected "user hasn't picked a race date yet" case (too aggressive — this
isn't malformed input, it's simply absent evidence). `TimeAdequacyResolutionOutcome` (a small
`Status`/`Result`/`NotEvaluatedReasonCode` wrapper, new in this phase) cleanly separates "resolved to a
registry value" from "could not evaluate," without touching Phase 4C's existing interfaces.
`TimeAdequacyResolver` does **not** implement `ITimeAdequacyResolver` in this phase for this reason —
that interface's contract (a bare `RuntimeConditionResolutionResult`, always present) doesn't yet have a
"not evaluated" concept. Wiring `TimeAdequacyResolver` to that interface is deferred to a future phase, once
either (a) the missing-date question above is resolved with a definite registry-representable answer, or
(b) the interface itself is revisited to support this outcome shape.

## Why `READINESS_ONLY` is not a `TIME_ADEQUACY_IN` output

Confirmed directly against the real registry (`RegistryValidationTests`/`TimeAdequacyResolverTests`, reading
`runtime-condition-values.v2.json` live): `READINESS_ONLY` exists only under the unrelated `PLAN_MODE_IN`
condition type, never under `TIME_ADEQUACY_IN`. `TimeAdequacyResolver` never produces it. The 5–7-week
readiness-gated-compressed behavior and any `PLAN_MODE_IN.READINESS_ONLY` routing remain **entirely
unimplemented** in this phase, per the task's explicit instruction — this resolver produces
`COMPRESSED` uniformly for the whole `[minimumWeeks, defaultWeeks)` band, with no readiness gate or override
of any kind layered on top.

## Confirmation: not wired to live generation

- `Program.cs` registers no `TimeAdequacyResolver` (or any resolver) in DI — only the pre-existing
  Phase 4C `IRuntimeConditionRegistryReader` registration remains, unchanged.
- `PlanServices`/`PlaceholderPlanGenerationEngine` were not modified in this phase; new reflection-based
  tests (`TimeAdequacyResolverNotWiredToGenerationTests.PlanServices_ConstructorDependencies_DoNotIncludeTimeAdequacyResolver`
  and the `PlaceholderPlanGenerationEngine` equivalent) assert neither constructor takes a
  `TimeAdequacyResolver` parameter.
- Re-run, with a `RaceDate` present in the request (the exact input the resolver would need), of both the
  existing-supported-template preview flow and the `TEN_K`/`INTERMEDIATE`/4-day
  `PlanTemplateNotAvailableException` case — both behave exactly as before Phase 4D.1.
- `GeneratePreviewResponse_HasNoDecisionTraceProperty` structurally confirms no public response DTO
  exposes `TimeAdequacyResolutionOutcome`, `RuntimeConditionResolutionResult`, or `ResolverDecisionTrace`.

## Remaining work for Phase 4D.2

1. Resolve the still-open `TIME_ADEQUACY_IN` missing-`raceDate`/missing-`startDate` question (fail loudly
   vs. a defined safe default) with an explicit product decision — this phase deliberately left it as
   `NotEvaluated`/`DECISION_REQUIRED`, not resolved.
2. Design and implement the 5–7-week readiness-gated-compressed composition
   (`TIME_ADEQUACY_IN` + `CORE_ENTRY_READINESS_IN` + `PLAN_MODE_IN.READINESS_ONLY`) — blocked on
   `TD-REGISTRY-001` (`CORE_ENTRY_READINESS_IN`/`STANDARD` fixture defect) being resolved first.
3. Implement `PACE_SOURCE_IN`, `CORE_ENTRY_READINESS_IN`, `GOAL_FEASIBILITY_IN` resolvers — none
   implemented in this phase.
4. Decide whether/how `TimeAdequacyResolver` should be adapted to satisfy `ITimeAdequacyResolver` (Phase
   4C's interface), once the missing-date question is settled.
5. Only once all four condition resolvers exist and are individually validated should wiring into
   `IRuntimeConditionResolutionService.ResolveAll` (composing a full `ResolverDecisionTrace`) be attempted
   — still not done here.
6. Live generation wiring (`IPlanGenerationEngine` actually consuming resolver output) remains entirely
   out of scope until stage-to-week scheduling and workout generation also exist.

## Confirmations

- No plan-catalog artifact was modified — `templates/ten-k-master.v6.json` and
  `catalog/registries/runtime-condition-values.v2.json` were read-only inspected.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`.
- `EV-005` remains `PROPOSED`; `EV-006` remains `ACCEPTED_AS_SUPPORTING_EVIDENCE`.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still throws
  `PlanTemplateNotAvailableException`.

**Final classification: `BACKEND_HAS_TIME_ADEQUACY_RESOLVER_NOT_WIRED_TO_GENERATION`.**
