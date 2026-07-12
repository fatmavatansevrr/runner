# Backend Integration Phase 4D.5 — Runtime Condition Resolver Orchestration Service

Implements the first real `IRuntimeConditionResolutionService`, running the four existing resolvers
(`TimeAdequacyResolver` → `PaceSourceResolver` → `CoreEntryReadinessResolver` → `GoalFeasibilityResolver`)
in the approved order, automatically threading each result forward via `RuntimeResolverContext.PriorResults`.
Orchestration only — not wired into `PlanServices`/`PlaceholderPlanGenerationEngine`/live generation.

## Orchestration order

`TIME_ADEQUACY_IN` → `PACE_SOURCE_IN` → `CORE_ENTRY_READINESS_IN` → `GOAL_FEASIBILITY_IN`, exactly as
specified. Verified by a test asserting the exact `ConditionType` sequence in both
`ResolveAllResults`'s raw list and `ResolveAll`'s trace `StepIndex` ordering.

## Why orchestration is separate from live generation wiring

Every prior resolver phase (4D.1–4D.4) deliberately kept its resolver unregistered and unconsumed. This
phase, for the first time, **registers** the four resolvers and the orchestration service in `Program.cs`
DI — but registration is not the same as wiring: nothing in `RunningApp.Api`'s controllers, `PlanServices`,
or `PlaceholderPlanGenerationEngine` takes any of these five types as a constructor dependency. A service
sitting in the DI container that nothing resolves is inert with respect to any live HTTP request — this is
confirmed structurally (not just by omission) via reflection-based tests
(`RuntimeConditionResolutionServiceNotWiredToGenerationTests`) checking both consumer classes' constructor
parameter lists. Full live-generation wiring is out of scope for this and every prior resolver phase; this
phase only proves the resolver *chain* itself is a coherent, deterministic pipeline in isolation.

## `RuntimeResolverContext.PriorResults` population strategy

`RuntimeResolverContext` has no setters (init-only properties) — it cannot be mutated in place. Each step
of `ResolveAllResults` therefore constructs a **new** `RuntimeResolverContext` (via a private
`WithPriorResults` helper) carrying the same `InputSnapshot`/`CoreCycle`/`Config`/`AsOfDate` as the original
caller-supplied context, with `PriorResults` replaced by a fresh snapshot copy of the results accumulated
so far. The caller's original context object is never touched — verified by a test
(`ResolveAllResults_DoesNotMutateCallersOriginalContext`) asserting the original context's `PriorResults`
remains `null` and its `InputSnapshot` reference is unchanged after a full `ResolveAllResults` call.

Callers of `ResolveAllResults`/`ResolveAll` never need to hand-construct `PriorResults` for the standard
four-resolver pipeline — verified by a test that supplies a context with `PriorResults = null` and confirms
`GoalFeasibilityResolver` does not fall into any of its "missing dependency" `NotEvaluated` branches.

## Dependency handling

Unchanged from each resolver's own Phase 4D.1–4D.4 contract — the orchestrator does not reinterpret or
override any individual resolver's dependency-handling rules. It only supplies the mechanism
(`PriorResults`) those rules depend on.

## `NotEvaluated` propagation

**Never swallowed, never short-circuited.** All four resolvers always run, in order, regardless of any
earlier resolver's status. A `NotEvaluated` result from `TimeAdequacyResolver` (e.g. a habit plan with no
dates) is still added to `PriorResults` and handed to `PaceSourceResolver`, `CoreEntryReadinessResolver`,
and eventually `GoalFeasibilityResolver` — the latter's own documented `TIME_ADEQUACY_NOT_EVALUATED`
handling then fires correctly, verified end-to-end by test
(`EndToEnd_MissingRaceDate_HabitPlan_PropagatesNotEvaluated_ThroughToGoalFeasibility`). No second
short-circuit policy was invented on top of each resolver's own existing NotEvaluated handling — the task's
own instruction ("do not stop the pipeline merely because one resolver returns NotEvaluated unless
repository evidence says to short-circuit") was honored by simply not adding any such logic.

## Missing dependency behavior

Structurally impossible within `ResolveAllResults`/`ResolveAll` for `GoalFeasibilityResolver` specifically
— since the orchestrator always runs the three prior resolvers before it and always appends each result to
`PriorResults`, `GoalFeasibilityResolver`'s "missing result" branches (`MISSING_CORE_ENTRY_READINESS_RESULT`,
etc.) can only fire when a resolver is called directly/in isolation (as in each resolver's own Phase 4D
tests), never through this orchestrator. This is verified negatively — a test confirms none of the three
"missing" reasonCodes ever appear in the orchestrated `GOAL_FEASIBILITY_IN` result.

## `GoalFeasibilityResolver` dependency behavior through orchestration — a new finding

Running the full pipeline end-to-end surfaced a fact that was not visible when `GoalFeasibilityResolver`
was tested in isolation with a hand-built `PriorResults` list (Phase 4D.4): **`PACE_SOURCE_IN=NONE` can
never coexist with a requested target time when reached through this real orchestration.**
`PaceSourceResolver`'s own output priority is `RECENT_RACE > TARGET_TIME > NONE` — once
`TargetFinishTimeSeconds` is present, `PaceSourceResolver` can only emit `NONE` if it *also* has no target
time to fall back to, which means `GoalFeasibilityResolver` would already have returned `NOT_REQUESTED`
before ever consulting `PACE_SOURCE_IN` at all. `GoalFeasibilityResolver`'s documented
`PACE_SOURCE_NONE_TARGET_TIME_REQUESTED` branch therefore remains **defensive code, not a reachable path in
this composed pipeline as currently built** — it is correct to keep (a future resolver change, or a direct
non-orchestrated call, could still reach it), but it is not exercised end-to-end. The actually-reachable
"target time present, no independent current-fitness evidence" case is `PACE_SOURCE_IN=TARGET_TIME`, which
*is* exercised end-to-end and correctly returns `NotEvaluated`/`PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`.
This is recorded here as a documented finding, not silently discovered and ignored, and the test suite was
adjusted to test the actually-reachable scenario rather than asserting a false claim about the unreachable
one.

## Decision trace handling

`ResolveAll(RuntimeResolverContext) : ResolverDecisionTrace` builds an ordered, four-step trace directly
from `ResolveAllResults`'s output via the existing `ResolverDecisionTraceStep.FromResult` factory (Phase
4D.1.5) — no new trace-construction logic was invented; the existing factory already keeps a step's
`Status`/`OutputValue` in sync with the result it represents. Resolver keys used:
`TIME_ADEQUACY_RESOLVER`/`PACE_SOURCE_RESOLVER`/`CORE_ENTRY_READINESS_RESOLVER`/`GOAL_FEASIBILITY_RESOLVER`,
matching the naming style already used in the golden fixture's own decision trace steps. **Still not
exposed on any public API DTO** — confirmed by the same structural reflection test used in every prior
phase (`GeneratePreviewResponse` has no property of type `RuntimeConditionResolutionResult` or
`ResolverDecisionTrace`).

## Context requirements

- **Missing `CoreCycle`:** `TimeAdequacyResolver` still throws `InvalidOperationException` (Phase 4D.1.6's
  own contract) — the orchestrator does **not** catch or convert this into a `NotEvaluated` result; it
  propagates unchanged out of `ResolveAllResults`/`ResolveAll`, verified by test.
- **Missing `AsOfDate`:** does not fail orchestration at all — `PaceSourceResolver`'s existing
  `NOT_COMPUTED_NO_REFERENCE_DATE` behavior (Phase 4D.2) is preserved exactly, verified by test. A second
  test confirms a present `AsOfDate` correctly flows through to produce real `raceResultAgeDays`/
  `paceRecencyConfidence` metadata.

## Result collection shape

`IReadOnlyList<RuntimeConditionResolutionResult> ResolveAllResults(RuntimeResolverContext)` — a new public
method on the concrete `RuntimeConditionResolutionService` class (not part of the `IRuntimeConditionResolutionService`
interface, which still returns only `ResolverDecisionTrace` per its existing Phase 4C/4D.1.6 contract).
Exactly four results, one per condition type, in the approved order, verified by test to contain no
duplicate `ConditionType` values. `ResolveAll` is implemented as a thin wrapper that calls
`ResolveAllResults` and packages the results into a trace — the two methods can never drift out of sync
since one calls the other.

## Governance clarification: full-blocker vs. partial-decision-required TD tracking

Recorded here (Phase 4D.5 documentation), per the task's own instruction, rather than as a separate
governance file — this repository's existing TD-risk artifacts (`activation-readiness-risks.json`/`.md`)
already carry inline classification/severity fields per entry rather than a separate governance document,
so adding the convention to this phase's own doc matches that existing style.

**A. Full blocker** — a resolver *cannot* produce useful `Evaluated` outputs for its intended
responsibility at all. Example: `CoreEntryReadinessResolver` before Phase 4D.3.1's owner-approved
thresholds existed (it returned `NotEvaluated` unconditionally, for every input, including fully favorable
evidence). **Should create a formal `TD-*` activation risk** (e.g. `TD-CORE-READINESS-001`).

**B. Partial decision-required sub-case** — the resolver is mostly implemented and functions correctly for
its primary supported cases; one specific, narrow input combination remains `NotEvaluated`/
`DECISION_REQUIRED`. Example: `GoalFeasibilityResolver` + `TIME_ADEQUACY_IN=INSUFFICIENT` (Phase 4D.4) —
every other dependency combination classifies correctly; only this one sub-case is left open pending
evidence this repository does not yet have. **May be documented in the phase doc alone, without a formal
`TD-*` risk**, when all of the following hold: it does not prevent the resolver from functioning in its
primary supported cases (confirmed — `GoalFeasibilityResolver`'s `ADEQUATE`/`COMPRESSED`/`RECENT_RACE`/
`NOT_REQUESTED`/`NOT_READY` paths all work); behavior is explicit and tested (confirmed — a dedicated test
asserts the exact `NotEvaluated`/`TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED` result); the `NotEvaluated`
reasonCode is distinct and self-describing (confirmed — `TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED` is
unique and names the open question); and activation impact is documented (confirmed — Phase 4D.4's own doc
states this explicitly under its own `DECISION_REQUIRED` heading).

**Applying this convention retroactively: `TD-GOAL-FEASIBILITY-001` is NOT created**, per the convention
above — Phase 4D.4's `TIME_ADEQUACY_IN=INSUFFICIENT` case is category B (partial, already documented,
already tested, does not block the resolver's primary function), not category A. No prior evidence-log or
TD-risk entry was mutated to make this determination — this is a forward-looking governance note applied to
already-existing, already-correct behavior, not a retroactive reclassification of any existing artifact.

## Dependency injection

`Program.cs` now registers, for the first time: `ITimeAdequacyResolver → TimeAdequacyResolver`,
`IPaceSourceResolver → PaceSourceResolver`, `ICoreEntryReadinessResolver → CoreEntryReadinessResolver`,
`IGoalFeasibilityResolver → GoalFeasibilityResolver`, and `IRuntimeConditionResolutionService →
RuntimeConditionResolutionService`, all `AddScoped`, all documented in-line as internal-only with an
explicit pointer to the reflection-based regression tests that prove nothing consumes them. No existing
registration (`IPlanPreviewService`, `IPlanConfirmationService`, etc.) was changed.

## Tests added

`RuntimeConditionResolutionServiceTests.cs` (17 tests): orchestration order, no-duplicate-results, trace
step ordering, automatic `PriorResults` population, context-immutability, five end-to-end scenarios
(`NOT_REQUESTED`, `NOT_READY`→`UNSUPPORTED`, `REALISTIC`, `CHALLENGING`, `NotEvaluated` propagation through
a habit-plan `TIME_ADEQUACY_IN` gap, and the `TARGET_TIME`-not-`NONE` finding above), missing-`CoreCycle`
propagation, missing/present-`AsOfDate` behavior, registry validation across a full end-to-end scenario, and
a cross-cutting "`NotEvaluated` never has an `OutputValue`" check across all four results.
`RuntimeConditionResolutionServiceNotWiredToGenerationTests.cs` (6 tests): re-confirms existing SQL-template
and `TEN_K`/`INTERMEDIATE`/4-day behavior with full resolver-relevant evidence present, and structurally
confirms neither `PlanServices` nor `PlaceholderPlanGenerationEngine` nor `GeneratePreviewResponse`
references any of the five newly-registered types.

## Full test results

**326/326 tests passing** (304 prior + 22 new). One test failure was found and fixed during this phase
(the `PACE_SOURCE_IN=NONE`-with-target-time scenario, described above under "a new finding") — caught
immediately by the test run itself, corrected to test the actually-reachable equivalent scenario, not
silently worked around.

## Git safety note

`git status` was inspected before any code change and matched the exact expected accumulated state from
every prior phase (159 changed paths). No broad reset/checkout was performed. After building and testing,
the same narrow, `bin`/`obj`-only checkout used in every prior phase of this track was applied (documented
reason: this repository has tracked `bin/Debug`/`obj/Debug` build-artifact files, a pre-existing quirk
documented since Phase 3, unrelated to this phase's own changes) — confirmed afterward that only build
output paths were affected: `git status` showed the exact same 21 source-level change lines before and
after the checkout, with `Program.cs` correctly still showing as modified (this phase's actual DI
registration edit).

## Confirmations

- No plan-catalog artifact was modified (confirmed: 117 files, unchanged from before this phase).
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`, untouched.
- `TD-PACESOURCE-001`/`TD-PACESOURCE-002` remain `OPEN`, untouched.
- `TD-CORE-READINESS-001` remains `OPEN` (its existing `resolutionNote` annotation from Phase 4D.3.1 was
  not touched — this phase made zero edits to `activation-readiness-risks.json`/`.md`).
- `EV-006`/`EV-007`/`EV-008` unchanged — this phase made zero edits to `evidence-log.json`/`.md`.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still throws
  `PlanTemplateNotAvailableException`.
- No new public API field was added.

## Remaining work for Phase 4E

1. `PLAN_MODE_IN` remains the only one of the five originally-named condition types with no resolver at
   all.
2. The `TIME_ADEQUACY_IN=INSUFFICIENT`/`GOAL_FEASIBILITY_IN` `DECISION_REQUIRED` item (Phase 4D.4) remains
   open — likely requires `PLAN_MODE_IN` to exist first.
3. Live generation wiring for the orchestration service (or any individual resolver) remains entirely out
   of scope — this phase proves the pipeline is internally coherent, not that it is ready to affect a real
   user-facing plan.
4. Should a future phase wire this orchestrator into `PlanServices`, it will need to decide: what
   `RuntimeResolverContext` fields does a live request actually supply (`CoreCycle` from the Phase 1
   catalog loader; `AsOfDate` from... an as-yet-undecided source, per `TD-PACESOURCE-002`); and what
   happens to the resulting `ResolverDecisionTrace`/results (persisted? exposed via a future detail DTO?
   discarded?) — none of these are decided here.

**Final classification: `BACKEND_HAS_RUNTIME_CONDITION_RESOLVER_ORCHESTRATION_NOT_WIRED_TO_GENERATION`.**
