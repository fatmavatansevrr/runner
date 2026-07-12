# Backend Integration Phase 4D.1.6 — Resolver Context Contract Alignment

Aligns the shared resolver contract with a `RuntimeResolverContext` type so future resolvers needing
additional context (catalog config, prior resolver results) do not each invent a condition-specific method
overload the way `TimeAdequacyResolver` had to in Phase 4D.1. No new resolver logic was implemented; no
resolver is wired to live generation.

## Why `ResolverInputSnapshot` alone is insufficient

`ResolverInputSnapshot` (Phase 4C) is, by design, the user/request/onboarding evidence snapshot —
distance identity, schedule dates, Phase 4B fitness-evidence fields. `TimeAdequacyResolver` needed a second
kind of input entirely: `PlanCatalogCoreCycle`, which comes from **Process A catalog state** (a
PLAN_TEMPLATE document), never from the user. Phase 4D.1/4D.1.5 solved this with a
TIME_ADEQUACY-specific `Resolve(ResolverInputSnapshot, PlanCatalogCoreCycle)` two-argument overload — which
meant `TimeAdequacyResolver` could not implement the shared `ITimeAdequacyResolver`/`IRuntimeConditionResolver`
interface's single-argument `Resolve` method at all, and the base overload existed only to throw
`NotSupportedException`. Left unaddressed, the next resolver needing extra context (a future
`CORE_ENTRY_READINESS_IN` needing readiness thresholds/config, or a future `GOAL_FEASIBILITY_IN` needing
prior resolver outputs and pace-projection context) would have invented its own second overload too —
exactly the fragmentation this phase's task explicitly warned against.

## Why `RuntimeResolverContext` was added

A single new type, `RuntimeResolverContext`, now carries everything a resolver might need to evaluate,
while keeping `ResolverInputSnapshot` unchanged and focused:

```csharp
public sealed class RuntimeResolverContext
{
    public required ResolverInputSnapshot InputSnapshot { get; init; }
    public PlanCatalogCoreCycle? CoreCycle { get; init; }
    public IReadOnlyList<RuntimeConditionResolutionResult>? PriorResults { get; init; }
    public IReadOnlyDictionary<string, string>? Config { get; init; }
}
```

Every field beyond `InputSnapshot` is optional at the **context-model** level — not every resolver needs
`CoreCycle` (a hypothetical `PACE_SOURCE_IN` resolver would not), and no resolver in this repository reads
`PriorResults` or `Config` yet. Each individual resolver is responsible for deciding, and documenting,
whether an optional context field it specifically needs is actually required for **it**, and how it treats
that field's absence — the context model itself does not encode per-resolver requiredness.

## Boundary between user input snapshot and catalog/policy context

- **`ResolverInputSnapshot`** (unchanged by this phase): only evidence that originates from the user or
  their request — goal identity, schedule dates, Phase 4B fitness-evidence fields. A new regression test
  (`ResolverInputSnapshot_HasNoCoreCycleProperty_CatalogContextStaysOffTheUserSnapshot`) structurally
  guards against `PlanCatalogCoreCycle` (or any future catalog/policy type) ever being absorbed into this
  type.
- **`RuntimeResolverContext`**: the execution context — the input snapshot plus whatever catalog/policy
  context and cross-resolver composition data a specific resolution pass needs. This is where
  `PlanCatalogCoreCycle`, and any future readiness-config or prior-resolver-output data, belongs.

## Resolver interface changes

`IRuntimeConditionResolver.Resolve` now takes a single `RuntimeResolverContext` parameter instead of a bare
`ResolverInputSnapshot`. `ITimeAdequacyResolver`/`IGoalFeasibilityResolver`/`IPaceSourceResolver`/
`ICoreEntryReadinessResolver`/`IPlanModeResolver` all inherit this signature unchanged (they add no members
of their own — this was already true in Phase 4C). `IRuntimeConditionResolutionService.ResolveAll` was
updated the same way: `ResolveAll(RuntimeResolverContext context) : ResolverDecisionTrace`.

**The old two-argument `TimeAdequacyResolver.Resolve(ResolverInputSnapshot, PlanCatalogCoreCycle)` overload
was removed entirely** (not deprecated/kept) — `TimeAdequacyResolver` now has exactly one public `Resolve`
method, matching the interface exactly. This was safe to remove outright rather than deprecate: the
resolver is not wired to any production caller (confirmed by the existing not-wired-to-generation tests),
so there was no live caller of the old overload to preserve compatibility for — only this repository's own
tests called it, and they were updated in the same pass. A structural test
(`TimeAdequacyResolver_Resolve_TakesSingleRuntimeResolverContextArgument`) confirms exactly one `Resolve`
method exists, with exactly one `RuntimeResolverContext` parameter, so this cannot silently regress.

## How `TimeAdequacyResolver` now receives `CoreCycle`

`TimeAdequacyResolver.Resolve(RuntimeResolverContext context)` reads `context.CoreCycle`. All Phase 4D.1
behavior is preserved exactly: dynamic core-cycle thresholds (read from the real
`TEN_K__4D__INTERMEDIATE v10` candidate's `templates/ten-k-master.v6.json` `coreCycle` object, unchanged),
the `floor(availableDays / 7)` `availableWeeks` calculation, the `Evaluated`/`NotEvaluated` status split
from Phase 4D.1.5, the race/non-race `GoalType`-based missing-date behavior from Phase 4D.1.5, and every
metadata field (`availableWeeks`, `availableDays`, `minimumCoreWeeks`, `defaultCoreWeeks`,
`maximumCoreWeeks`, `roundingRule`).

## Missing `CoreCycle` behavior

**Chosen: `InvalidOperationException`, checked before anything else in `Resolve`, including the
missing-date branch.** Rationale, per the task's own recommendation and repository convention:

- `NotEvaluated` (Phase 4D.1.5) is reserved for missing **optional user evidence** — a legitimate runtime
  state a real client request can produce (e.g. a habit-goal plan genuinely has no race date).
- A missing `CoreCycle` is categorically different: it means whoever is calling
  `TimeAdequacyResolver.Resolve` failed to load and attach a `PlanCatalogCoreCycle` before invoking it —
  this is not a state any real user request could cause on its own; it can only happen if the resolver is
  wired up incorrectly. Treating it as `NotEvaluated` would silently mask a wiring bug as if it were normal,
  expected, user-driven behavior.
- `InvalidOperationException` was chosen over `ArgumentException` because the `RuntimeResolverContext`
  object itself is not malformed as an argument — it is a valid object missing an optional field that this
  particular resolver happens to require; the problem is the resolver being invoked in a state
  (context/configuration) it does not support, which is exactly what `InvalidOperationException` is for by
  .NET convention. This is a judgment call, made and documented explicitly per the task's instruction to
  follow existing conventions if they differ from the suggestion — no existing repository convention was
  found that contradicts this choice (Phase 1's `CanonicalDistanceFamilyResolver` and this same resolver's
  own `ArgumentException` usage are both for malformed/invalid *argument values*, not missing *optional
  context*, so neither is a precedent that applies here).

Two tests confirm this: a missing `CoreCycle` alone throws `InvalidOperationException` with a message
naming `CoreCycle`; a missing `CoreCycle` **and** missing dates still throws `InvalidOperationException`
(not `NotEvaluated`) — the configuration-error check runs first.

## Why no live generation wiring was added

Unchanged from every prior phase in this track: `TimeAdequacyResolver` is still the only concrete resolver
implementation, it is registered nowhere in `Program.cs`, and `PlanServices`/`PlaceholderPlanGenerationEngine`
still take no dependency on it or on `RuntimeResolverContext`/`IRuntimeConditionResolutionService` (re-confirmed
by the pre-existing reflection-based constructor-dependency tests, which pass unchanged since neither class
was touched in this phase). This phase is a pure contract-shape alignment.

## How future resolvers should use the shared context instead of inventing overloads

1. Implement the condition-specific interface (`IPaceSourceResolver`, `ICoreEntryReadinessResolver`,
   `IGoalFeasibilityResolver`, `IPlanModeResolver`) with exactly one `Resolve(RuntimeResolverContext context)`
   method — never add a second, condition-specific overload.
2. Read whatever context fields the resolver actually needs directly off `context` (`context.InputSnapshot`,
   `context.CoreCycle`, `context.PriorResults`, `context.Config`) — do not request a new context field
   until a concrete, evidenced need exists (mirroring how `PriorResults`/`Config` were added here as
   forward-looking slots with no reader yet, rather than as a promise to be filled in blindly later).
3. For each context field the resolver requires, document explicitly whether its absence means
   "resolver configuration/context error" (throw, following `TimeAdequacyResolver`'s `CoreCycle` precedent)
   or "missing optional user evidence" (`RuntimeConditionResolutionResult.NotEvaluated(...)`, following the
   Phase 4D.1.5 date-handling precedent) — these are different failure classes and must not be conflated.
4. Keep `ResolverInputSnapshot` free of catalog/policy fields — if a resolver needs Process A catalog data,
   it belongs on `RuntimeResolverContext`, following the same boundary rule enforced here for `CoreCycle`.

## Tests added/updated

- `RuntimeResolverContext` contract shape (`TimeAdequacyResolverTests.cs`): carries `InputSnapshot`, can
  carry `CoreCycle`, and `ResolverInputSnapshot` is confirmed to have no `PlanCatalogCoreCycle` property.
- `TimeAdequacyResolver` interface shape: implements `ITimeAdequacyResolver`; exactly one `Resolve` method
  taking exactly one `RuntimeResolverContext` parameter.
- All Phase 4D.1/4D.1.5 boundary, metadata, missing-date, and registry-validation tests updated to call
  `resolver.Resolve(new RuntimeResolverContext { InputSnapshot = ..., CoreCycle = ... })` — same assertions,
  same expected values, unchanged behavior.
- Two new missing-`CoreCycle` tests: throws `InvalidOperationException`; takes precedence over the
  missing-date check.
- `ContractOnlyFakeResolutionServiceTests.cs` updated: `ResolveAll` now takes `RuntimeResolverContext`.

## Full test results

**182/182 tests passing** (176 prior + 6 net new). Zero regressions.

## Confirmations

- No plan-catalog artifact was modified.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`.
- `EV-005` remains `PROPOSED`; `EV-006` remains `ACCEPTED_AS_SUPPORTING_EVIDENCE`.
- No `PACE_SOURCE_IN`, `CORE_ENTRY_READINESS_IN`, `GOAL_FEASIBILITY_IN`, or `PLAN_MODE_IN` resolver logic
  was implemented.
- No resolver is registered in `Program.cs` or invoked from `PlanServices`/`PlaceholderPlanGenerationEngine`.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still throws
  `PlanTemplateNotAvailableException` (unaffected — `Program.cs`, `PlanServices.cs`, and
  `PlaceholderPlanGenerationEngine.cs` were not touched in this phase).

**Final classification: `BACKEND_HAS_SHARED_RESOLVER_CONTEXT_CONTRACT_NOT_WIRED_TO_GENERATION`.**
