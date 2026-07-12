# Backend Integration Phase 4D.4 — GOAL_FEASIBILITY_IN Resolver Implementation

Implements the fourth and final V1 condition resolver: `GoalFeasibilityResolver`, producing
`GOAL_FEASIBILITY_IN` only. Depends on prior `CORE_ENTRY_READINESS_IN`, `TIME_ADEQUACY_IN`, and
`PACE_SOURCE_IN` results via `RuntimeResolverContext.PriorResults`. Not wired into
`IPlanGenerationEngine`/`PlanServices`/`PlaceholderPlanGenerationEngine`.

## Whether approved goal-feasibility rules were found

**Partially yes — enough to implement the `RECENT_RACE` path with no invented parameters, but not enough
to resolve every dependency-composition edge case.** The golden fixture's `GOAL_FEASIBILITY_RESOLVER` step
was inspected directly (not from memory) and yields two categories of evidence:

1. **Fully evidenced and implemented:** the ratio-classification thresholds (`realisticMaxRatio=0.03`,
   `challengingMaxRatio=0.06`) and the Riegel projection exponent (`1.06`, from the same fixture's
   `PACE_CONVERSION`/`RIEGEL_CONVERSION_5K_TO_10K` step). These match the task's own stated fallback mapping
   exactly, so they were implemented directly, with exact source citations.
2. **Conceptually evidenced but NOT behaviorally evidenced:** the fixture's `GOAL_FEASIBILITY_RESOLVER`
   step also runs a `TIME_ADEQUACY_MODIFIER` rule and a `VOLUME_READINESS_MODIFIER` rule — both exist in
   the pipeline, but the only fixture instance available shows both as `"outcome": "NOT_APPLIED"` (because
   `timeAdequacy=ADEQUATE` and peak volume is within its typical band). **No fixture example shows what
   either modifier actually does when it DOES apply** (e.g. for `TIME_ADEQUACY_IN=INSUFFICIENT`). This is
   the basis for the one `DECISION_REQUIRED` item in this implementation (see "TIME_ADEQUACY_IN dependency"
   below) — the resolver was not blocked entirely, but this one sub-case was not guessed.

**Important scope correction:** the fixture's `VOLUME_READINESS_MODIFIER` compares
`provisionalReachablePeakKm` against a catalog `typicalBandKm` — a **Process A peak-volume-band concept**,
not the same thing as this backend's `CORE_ENTRY_READINESS_IN` (weekly-volume/longest-run thresholds). The
fixture does not evidence a "core entry readiness modifies goal feasibility" rule at all. The
`CORE_ENTRY_READINESS_IN=NOT_READY → UNSUPPORTED` composition rule implemented here comes directly from
this task's own explicit instruction (stated imperatively: "GOAL_FEASIBILITY_IN must evaluate to
UNSUPPORTED"), not from independently found fixture evidence — this distinction is recorded here
explicitly rather than conflating the two evidence sources.

## Dependency handling — summary table

| Dependency state | Result | Source |
|---|---|---|
| No `TargetFinishTimeSeconds` | `Evaluated`/`NOT_REQUESTED` (checked first, bypasses all dependencies) | Task-directed |
| `CORE_ENTRY_READINESS_IN` missing | `NotEvaluated`/`MISSING_CORE_ENTRY_READINESS_RESULT` | Task-directed (never silently ignore) |
| `CORE_ENTRY_READINESS_IN` = `NotEvaluated` | `NotEvaluated`/`CORE_ENTRY_READINESS_NOT_EVALUATED` | Task-directed |
| `CORE_ENTRY_READINESS_IN` = `NOT_READY` | `Evaluated`/`UNSUPPORTED`/`CORE_ENTRY_NOT_READY` | Task-directed |
| `CORE_ENTRY_READINESS_IN` = `CAUTION` | Continue, `Metadata["coreEntryReadinessCaution"]="true"`, never auto-upgrades | Task-directed |
| `CORE_ENTRY_READINESS_IN` = `READY` | Continue, no extra metadata | Task-directed |
| `TIME_ADEQUACY_IN` missing | `NotEvaluated`/`MISSING_TIME_ADEQUACY_RESULT` | Task-directed |
| `TIME_ADEQUACY_IN` = `NotEvaluated` | `NotEvaluated`/`TIME_ADEQUACY_NOT_EVALUATED` | Task-directed |
| `TIME_ADEQUACY_IN` = `INSUFFICIENT` | `NotEvaluated`/`TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED` | **`DECISION_REQUIRED`** — see below |
| `TIME_ADEQUACY_IN` = `COMPRESSED` | Continue, `Metadata["timeAdequacyCompressed"]="true"` | Task-directed |
| `TIME_ADEQUACY_IN` = `ADEQUATE` | Continue, no extra metadata | Fixture-evidenced (`TIME_ADEQUACY_MODIFIER` NOT_APPLIED case) |
| `PACE_SOURCE_IN` missing | `NotEvaluated`/`MISSING_PACE_SOURCE_RESULT` | Task-directed |
| `PACE_SOURCE_IN` = `NotEvaluated` | `NotEvaluated`/`PACE_SOURCE_NOT_EVALUATED` | Task-directed |
| `PACE_SOURCE_IN` = `NONE` (target requested) | `NotEvaluated`/`PACE_SOURCE_NONE_TARGET_TIME_REQUESTED` | Task-directed ("do not guess") |
| `PACE_SOURCE_IN` = `TARGET_TIME` | `NotEvaluated`/`PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE` | Task-directed (never compare target to itself) |
| `PACE_SOURCE_IN` = `ESTIMATED` | `NotEvaluated`/`PACE_SOURCE_ESTIMATED_NO_APPROVED_METHOD` | Task-directed; also consistent with `TD-PACESOURCE-001` (never emitted by `PaceSourceResolver` anyway) |
| `PACE_SOURCE_IN` = `RECENT_RACE` | Riegel projection + ratio classification (see below) | Fixture-evidenced |

## `TIME_ADEQUACY_IN=INSUFFICIENT` — the one `DECISION_REQUIRED` item

Per the task's own explicit instruction ("If repo/product evidence says insufficient time maps directly to
UNSUPPORTED, implement UNSUPPORTED... Otherwise return NotEvaluated or mark DECISION_REQUIRED. Do not
guess."): the golden fixture's `TIME_ADEQUACY_MODIFIER` rule exists in the pipeline but its only evidenced
instance is the `NOT_APPLIED`/`ADEQUATE` case — there is no fixture example of what happens for
`INSUFFICIENT` (or `COMPRESSED`, though that case was still allowed to continue per explicit task
instruction, distinctly from `INSUFFICIENT`). Implementing a guessed `UNSUPPORTED` mapping here would
contradict "do not guess," and implementing continued classification would risk emitting `REALISTIC`/
`CHALLENGING` for a target with insufficient training time — exactly what the task said not to do. The
resolver therefore returns `NotEvaluated`/`TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED`, and this
document records the open question explicitly: **does insufficient time map directly to `UNSUPPORTED`, or
is this meant to be handled by a future `PLAN_MODE_IN` resolver instead?** No `TD-GOAL-FEASIBILITY-001` risk
was created for this single sub-case, since the resolver as a whole was successfully implemented (not
blocked) — this is recorded as an in-document `DECISION_REQUIRED` item for a future phase to resolve, per
the task's own two-tier scope (a full-resolver-block gets a `TD-*` risk; a single evidenced-conceptually-
but-not-behaviorally sub-case gets a `DECISION_REQUIRED` note).

## `NOT_REQUESTED` behavior

Checked first, unconditionally — if `TargetFinishTimeSeconds` is `null`, the resolver returns
`Evaluated`/`NOT_REQUESTED`/`TARGET_FINISH_TIME_NOT_REQUESTED` regardless of `PriorResults` (verified by a
test passing zero prior results and still getting `NOT_REQUESTED`). This is a real, `Evaluated` registry
value — explicitly distinct from `NotEvaluated` (a resolver status), tested side-by-side in
`ResolverContractTests` (Phase 4D.1.5) and re-confirmed here.

## `CORE_ENTRY_READINESS_IN` dependency behavior

See summary table. `NOT_READY` → `UNSUPPORTED` unconditionally. `CAUTION` → continues with a metadata flag,
never silently upgrading to `REALISTIC` (verified by a test using `CAUTION` alongside a fully "realistic"
Riegel projection, confirming the metadata flag is present without altering the correctly-computed
`REALISTIC` output).

## `TIME_ADEQUACY_IN` dependency behavior

See summary table and the `DECISION_REQUIRED` section above.

## `PACE_SOURCE_IN` dependency behavior

See summary table. Every non-`RECENT_RACE` `Evaluated` value (`NONE`, `TARGET_TIME`, `ESTIMATED`) returns
`NotEvaluated` with a distinct reasonCode — none of them are silently treated as feasibility-computable, per
explicit task instruction on each.

## Missing dependency behavior

Every one of `CORE_ENTRY_READINESS_IN`/`TIME_ADEQUACY_IN`/`PACE_SOURCE_IN` being **absent** from
`PriorResults` (not just `NotEvaluated`) produces its own distinct `NotEvaluated` result with a
`MISSING_*_RESULT` reasonCode — verified by three dedicated tests, each supplying `PriorResults` with the
other dependencies present but one specifically omitted.

## `NotEvaluated` dependency behavior

Every one of the three dependencies being present but itself `Status=NotEvaluated` produces a distinct
`*_NOT_EVALUATED` reasonCode on this resolver's own `NotEvaluated` result — verified by three dedicated
tests. No dependency's `NotEvaluated` status is ever silently treated as if it were absent or as a
particular `Evaluated` value.

## Projection / Riegel behavior

Implemented exactly as evidenced in `docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`'s
`PACE_CONVERSION` step: `predictedTimeSeconds = sourceTimeSeconds * (targetDistanceKm / sourceDistanceKm) ^
1.06`. `sourceDistanceKm`/`sourceTimeSeconds` come from `RecentRaceDistanceKm`/`RecentRaceFinishTimeSeconds`
(guaranteed present together whenever `PaceSourceResolver` emits `RECENT_RACE`, by that resolver's own
contract). `targetDistanceKm` prefers `RequestedTargetDistanceKm` (the user's exact requested distance, per
Phase 1's own distinction from the fixed family-representative `GoalDistanceKm`), falling back to
`GoalDistanceKm` only if the former is absent. A test reproduces the fixture's own exact numbers
(`5K in 1450s → 10K goal 3000s → predicted≈3023.15s → REALISTIC`) to confirm the formula matches the
evidenced calculation precisely, not approximately.

**Caveat, stated explicitly:** the fixture evidences the `1.06` exponent for exactly one case
(`profile: INTERMEDIATE`). No other profile/exponent pairing exists anywhere in the repo, so it cannot be
proven from repo evidence alone whether `1.06` is a universal constant or specific to the `INTERMEDIATE`
profile. This resolver applies `1.06` universally (the only value repo evidence provides), which is the
most direct-evidence-consistent choice available — not an invented value, but a generalization of the one
data point that exists. This is flagged here explicitly rather than silently assumed.

## Ratio threshold behavior

`goalGapRatio = (predictedTimeSeconds - goalTimeSeconds) / predictedTimeSeconds` (matches the fixture's own
`goalGapRatio=0.007658` for its exact inputs, confirmed by test). Classification: `<=0.03 → REALISTIC`,
`<=0.06 → CHALLENGING`, `>0.06 → UNSUPPORTED` — the exact task-specified fallback, which is also exactly
the fixture's own evidenced `classificationThresholds`. A target time slower than or equal to the projected
time (negative or zero ratio) still classifies `REALISTIC` (task's own explicit fallback: "equal/slower
than projected ability → REALISTIC"), verified by test.

## `REALISTIC` / `CHALLENGING` / `UNSUPPORTED` behavior

Three-way split exactly as above; four tests cover the exact fixture case (`REALISTIC`), a moderately
aggressive goal (`CHALLENGING`), a very aggressive goal (`UNSUPPORTED`), and a slower-than-predicted goal
(`REALISTIC` again, with `CONSERVATIVE` metadata).

## Metadata bands behavior

`Metadata["aggressivenessBand"]` implements the full Appsel V1 5-class model
(`CONSERVATIVE`/`REALISTIC`/`CHALLENGING`/`STRETCH`/`CURRENTLY_UNSUPPORTED`), sourced from
`docs/canonical/appsel-v1-canonical-decisions.md` §B.1 and owner-approved for metadata-only use per
`PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md` §A. Confirmed by test that this metadata value
is never equal to `OutputValue`, and that `OutputValue` is always one of the four registry-simple values.
Additional projection metadata: `projectionMethod`, `riegelExponent`, `sourceDistanceKm`,
`sourceTimeSeconds`, `targetDistanceKm`, `projectedFinishTimeSeconds`, `targetFinishTimeSeconds`,
`targetDeltaPercent`.

## Registry-simple / metadata-rich V1 scope

Unchanged, re-confirmed at the code level: `GoalFeasibilityResolver.OutputValue` is always exactly one of
`REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUESTED` — verified against the real registry file.
`CONSERVATIVE`/`STRETCH`/`CURRENTLY_UNSUPPORTED` confirmed invalid as `GOAL_FEASIBILITY_IN` registry values
(they could never accidentally leak into `OutputValue`); other condition types' values
(`READY`/`CAUTION`/`NOT_READY`/`RECENT_RACE`/`TARGET_TIME`/`NONE`) also confirmed invalid for this
condition type.

## Confirmation: not wired to live generation

Unchanged pattern from every prior phase: `Program.cs` registers no `GoalFeasibilityResolver`;
reflection-based tests confirm neither `PlanServices` nor `PlaceholderPlanGenerationEngine` takes it as a
constructor dependency; the existing-supported-template preview flow and the `TEN_K`/`INTERMEDIATE`/4-day
`PlanTemplateNotAvailableException` case were both re-run with goal-evidence fields present and behave
exactly as before this phase; no public response DTO exposes `RuntimeConditionResolutionResult` or
`ResolverDecisionTrace`.

## Remaining work for `PLAN_MODE_IN`

1. `PLAN_MODE_IN` remains entirely unimplemented — the last of the five condition types named across this
   resolver track (`GOAL_FEASIBILITY_IN`, `PACE_SOURCE_IN`, `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN`,
   `PLAN_MODE_IN`).
2. The `TIME_ADEQUACY_IN=INSUFFICIENT` `DECISION_REQUIRED` item above is the most direct candidate for
   `PLAN_MODE_IN` involvement (e.g. routing to `PLAN_MODE_IN.READINESS_ONLY`, per Phase 4A.3's own framing
   of the sub-8-week composition) — resolving it will likely require implementing `PLAN_MODE_IN` first, not
   the other way around.
3. `IRuntimeConditionResolutionService.ResolveAll`, composing all four now-implemented resolvers into one
   `ResolverDecisionTrace`, remains unimplemented.
4. `TD-CORE-READINESS-001`'s residual "not yet wired" note and `TD-PACESOURCE-001`/`TD-PACESOURCE-002`
   remain relevant and unresolved for this new resolver's own dependencies.
5. Live generation wiring for any resolver remains entirely out of scope.

## Git safety note

Before any code change, `git status` was inspected and matched the expected accumulated state from every
prior phase in this track (158 changed paths, all previously known). No broad reset/checkout was performed.
After building and testing, the same narrow, scoped `git checkout` limited to `bin`/`obj` paths only (used
in every prior phase of this track, to revert the pre-existing tracked-Debug-build-artifact pollution
documented since Phase 3) was applied — **no source file was touched by it**, confirmed by
`git status` showing the exact same 21 source-level changes before and after. Mid-session, running
`dotnet test --no-build` immediately after this checkout once returned a stale result (18 tests, from an
older committed DLL) because the checkout had reverted the freshly-built binary; this was caught, a fresh
`dotnet build` was run, and the full 262/304 (progressively) count was re-confirmed correct before
proceeding — recorded here for transparency, not hidden.

## Confirmations

- No plan-catalog artifact was modified.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`.
- `TD-PACESOURCE-001`/`TD-PACESOURCE-002` remain `OPEN`.
- `TD-CORE-READINESS-001` remains `OPEN` (annotated, not closed, per this repo's established convention) —
  untouched by this phase.
- `EV-006`/`EV-007`/`EV-008` unchanged.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still throws
  `PlanTemplateNotAvailableException`.
- No new public API field was added.

**Final classification: `BACKEND_HAS_GOAL_FEASIBILITY_RESOLVER_NOT_WIRED_TO_GENERATION`.**
