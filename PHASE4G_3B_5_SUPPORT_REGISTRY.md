# Phase 4G.3B.5 — Support Registry (dark, read-only, no live wiring)

## 1. Full public surface

```csharp
internal enum RaceCoreSupportStatus
{
    NotYetEvaluated, StructurallyInfeasible, Fail, DecisionRequired,
    MechanicallyPassed, Supported,
}

internal sealed record RaceCoreSupportEntry(
    int WeekCount,
    RaceCoreSupportStatus Status,
    SafetyVerificationPipelineResult? OrchestratorResult,
    string? AllocationOrderCorrectnessOutcome,
    string StatusReasonCode,
    IReadOnlyList<string> BlockingFindings);

internal sealed record RaceCoreSupportRegistry(
    string CandidateKey,
    IReadOnlyList<RaceCoreSupportEntry> Entries);

internal static class RaceCoreSupportRegistryBuilder
{
    public static RaceCoreSupportRegistry BuildFromMechanicalVerification(
        string candidateKey,
        IReadOnlyDictionary<int, SafetyVerificationPipelineResult> orchestratorResultsByWeekCount,
        IReadOnlyDictionary<int, AllocationOrderVerificationResult>? allocationOrderResultsByWeekCount);
}
```

Exactly as specified, using the real `AllocationOrderVerificationResult`
type name (the task's stub comment guessed
"AllocationOrderCorrectnessVerificationResult or equivalent" — the actual
repository type is `AllocationOrderVerificationResult`, defined in
`AllocationOrderCorrectnessVerifier.cs`).

## 2. AllocationOrderCorrectnessVerifier inclusion decision (Question 3)

**Decision: INCLUDED**, as a second, independent gate alongside
`SafetyVerificationOrchestrator`'s nine-verifier composition.

**Reasoning:** `PHASE4G_3B_3_SAFETY_VERIFICATION_PIPELINE_PLANNING.md`'s own
framing decision (written when `AllocationOrderCorrectnessVerifier` was
excluded from the orchestrator itself) explicitly states: *"support
activation will eventually need to consider both [allocation-order
correctness and the canonical nine-verifier pipeline], but that belongs to
the later support-registry/activation layer."* This registry **is** that
layer — deferring the decision further would leave it permanently
unaddressed. Excluding it here would cause every real, non-exhausted-
headroom week count (9, 10, 11, 13 for the current candidate) to show
`MechanicallyPassed` despite `TD-ALLOCATION-PRIORITY-001` remaining
genuinely open and directly bearing on whether that specific allocation's
per-phase distribution can be trusted — a materially misleading signal for
anyone consulting this registry, and in direct tension with this phase's
own "never silently upgrade" acceptance criterion. Inclusion is optional at
the call site (the second dictionary parameter is nullable) so a caller who
has not yet computed `AllocationOrderCorrectnessVerifier` results can still
use the registry with the gate reported as not-evaluated-for-that-week,
never silently treated as passing.

## 3. MechanicallyPassed vs. Supported

`Supported` exists in the enum only as a placeholder for a future,
explicitly separate activation step that this phase does not build.
`RaceCoreSupportRegistryBuilder.BuildFromMechanicalVerification` can
**never** produce it — there is no branch, default, or fallback anywhere in
the method (or its private helpers) that returns it; it is not a reachable
case of any switch this method evaluates (verified both by exhaustive
construction across every reachable input combination and by a bounded
source-text audit — see §6 of the final report).

**The already publicly-live 12-week horizon shows as `MechanicallyPassed`
in this registry, not `Supported` — this is deliberate, not an oversight.**
This registry has no independent knowledge of what `RaceHorizonPolicy`
actually enables today: it never reads that type, never reads any live
routing/policy state, and never reads `CatalogPreviewGenerator` or
`PlanServices`. It can only honestly report "the mechanical checks it knows
how to run currently pass" — never "this is publicly supported," since it
has no way to independently confirm that claim. In fact, as measured below,
the *real* 12-week registry entry today does not even reach
`MechanicallyPassed` — it is `DecisionRequired`, for a separate, deeper
reason (§4).

## 4. Real registry table — all 7 real feasible targets (8–14)

Built from the real `TEN_K__4D__INTERMEDIATE` candidate's real
`SafetyVerificationOrchestrator` results (Phase 4G.3B.4b's real primary-path
construction, reused unchanged) and real `AllocationOrderCorrectnessVerifier`
results, both computed fresh in this phase.

| Weeks | Orchestrator OverallOutcome | AllocationOrderCorrectness | Registry Status | StatusReasonCode |
|---|---|---|---|---|
| 8 | DecisionRequired | Pass | **DecisionRequired** | `ORCHESTRATOR_DECISION_REQUIRED_AT_GoalPaceReachability` |
| 9 | DecisionRequired | DecisionRequired | **DecisionRequired** | `ORCHESTRATOR_DECISION_REQUIRED_AT_GoalPaceReachability` |
| 10 | DecisionRequired | DecisionRequired | **DecisionRequired** | `ORCHESTRATOR_DECISION_REQUIRED_AT_GoalPaceReachability` |
| 11 | DecisionRequired | DecisionRequired | **DecisionRequired** | `ORCHESTRATOR_DECISION_REQUIRED_AT_GoalPaceReachability` |
| 12 | DecisionRequired | Pass | **DecisionRequired** | `ORCHESTRATOR_DECISION_REQUIRED_AT_GoalPaceReachability` |
| 13 | DecisionRequired | DecisionRequired | **DecisionRequired** | `ORCHESTRATOR_DECISION_REQUIRED_AT_GoalPaceReachability` |
| 14 | DecisionRequired | Pass | **DecisionRequired** | `ORCHESTRATOR_DECISION_REQUIRED_AT_GoalPaceReachability` |

**Every real target — including the live 12-week horizon — currently shows
`DecisionRequired`, not `MechanicallyPassed`.** This is an honestly-measured
fact, not an assumption: `GoalPaceReachabilityVerifier` unconditionally
appends exactly one synthetic NotEvaluated check on every call (confirmed
structural, Phase 4G.3B.4b), and that check can only ever resolve to
`StructurallyUnreachable` or `UncertainNotEvaluated` — never a clean pass —
so `GoalPaceReachabilityOutcome.Pass` is unreachable for any
mathematically-feasible allocation using the real `GOAL_PACE_REHEARSAL`
stage. The orchestrator's own aggregation rule (DecisionRequired is
unconditional, independent of the allocation-order gate) means this single
verifier's structural finding drives every real target's registry status
today, regardless of that target's own `AllocationOrderCorrectnessVerifier`
result (9, 10, 11, 13 additionally have their own open
`TD-ALLOCATION-PRIORITY-001` finding, but it never becomes the *deciding*
factor because the orchestrator gate is checked first and is already
DecisionRequired).

`BlockingFindings` for every target is the same 5 `GoalPaceReachability`
outcome-check entries (`Eligible`×2, `FallbackConfirmed`×2,
`UncertainNotEvaluated`×1), reflecting `TD-NOTEVALUATED-FALLBACK-001`.

A genuine `MechanicallyPassed` entry is demonstrated in this phase's test
suite only via a narrowly constructed synthetic orchestrator result — the
same technique Phase 4G.3B.4b's own test suite already established as
necessary for exercising a state the real verifier logic cannot currently
reach.

## 5. What remains before any entry could ever become Supported

Open TDs currently forcing `DecisionRequired` for at least one real target:

- **`TD-NOTEVALUATED-FALLBACK-001`** — forces `DecisionRequired` for
  **every** real target 8–14 today (see §4). Until resolved, no real target
  can mechanically pass the orchestrator gate at all.
- **`TD-ALLOCATION-PRIORITY-001`** — forces `DecisionRequired` via the
  allocation-order gate for targets 9, 10, 11, 13 specifically (any target
  whose allocation is not at a compression/extension headroom boundary).
  Currently masked by `TD-NOTEVALUATED-FALLBACK-001` already forcing
  DecisionRequired first, but would still independently block those four
  targets even if the first TD were resolved.
- **`TD-FOUNDATION-COMPRESSION-001`** — not currently triggered for any real
  8–14 target (no phase is allocated below its own catalog minimum today),
  but remains open and would force `DecisionRequired` via
  `ReadinessEligibilityVerifier` for any future candidate/catalog revision
  where it is.

**Resolving every applicable open TD is necessary but not sufficient.**
Even a target whose entry reaches `MechanicallyPassed` (all mechanical
checks pass) still requires an explicit, separate activation step —
outside this registry's own mechanical logic, outside this phase's scope,
and requiring its own real product/governance authorization — before it
could ever be recorded as `Supported`. This phase builds no such step, no
approval workflow, and no configuration mechanism for ever flipping a
status; `Supported` remains an inert placeholder value.

## 6. Semantics clarification (Phase 4G.3B.6.5, documentation-only)

`DecisionRequired` ≠ the live request is failing or unsafe today.
`DecisionRequired` = universal mechanical/catalog-contract clearance is not
yet complete.

For the currently-enabled 12-week public pilot specifically, three observed
facts coexist without contradiction:

1. **Public status:** 12 weeks is the currently enabled standalone core.
2. **Registry (universal) status:** `DecisionRequired`, driven entirely by
   `GoalPaceReachabilityVerifier`'s theoretical-completeness check.
3. **Observed real runtime paths** (Phase 4G.3B.6.1, real HTTP end-to-end
   tests):
   - ProductAverage → HTTP 200, Evaluated/CHALLENGING (control case).
   - UserDefined + no recent race → HTTP 422 `RUNTIME_CONDITION_UNSUPPORTED`,
     rejected before scheduling (characterization case).

Neither real observed path is "unsafe" — the registry's `DecisionRequired`
reflects an unproven theoretical completeness claim, not an observed
runtime failure.

### Three named upstream invariants (quoted exactly, Phase 4G.3B.6.4 §3)

> "(1) `V1CatalogPilotIdentityPolicy` restricts catalog routing to
> `GoalType=Race`; (2) `PaceSourceResolver` never emits `NotEvaluated`,
> `NONE`-with-target-time-present, or `ESTIMATED`; (3) `RaceHorizonPolicy`
> rejects below-minimum horizons before the resolver pipeline runs."

**If any of these three invariants changes (e.g., a non-Race goal type
enters the catalog pipeline, or `PaceSourceResolver` gains a new
NotEvaluated-producing path such as the deferred `ESTIMATED` method tracked
by `TD-PACESOURCE-001`), this reachability analysis and Phase 4G.3B.6.4's
audit must be re-run before this documentation section is trusted as still
accurate.**

### Future Option C trigger conditions

Conditions under which splitting `UniversalCatalogCompleteness` from
`RuntimeReachableMechanicalSafety` as two distinct verifier outputs (Option
C, Phase 4G.3B.6.4 §5–6) should be revisited, drawn directly from that
audit's own findings (§3's contingency note, §6's recommendation, and §9's
open questions) rather than newly stated here:

1. Upstream invariant (1) changes — a future Habit-goal (or other non-Race
   goal type) catalog route is introduced, per Phase 4G.3B.6.4 §3's
   contingency note.
2. Upstream invariant (2) changes — `PaceSourceResolver` gains a new
   `NotEvaluated`-producing path, "such as the deferred ESTIMATED method
   tracked by TD-PACESOURCE-001" (Phase 4G.3B.6.4 §3, §9 item 4).
3. Upstream invariant (3) changes — `RaceHorizonPolicy`'s below-minimum
   rejection no longer occurs before the resolver pipeline runs, per Phase
   4G.3B.6.4 §3's contingency note.
4. "A real consumer of `RaceCoreSupportRegistry`... needs to act on real
   runtime risk specifically" — Phase 4G.3B.6.4 §6's own stated condition
   for when Option C's added cost becomes justified (no such consumer was
   found to exist as of that audit).
5. Product/engineering makes the recorded decision (Phase 4G.3B.6.4 §9,
   open question 1) that the registry's `DecisionRequired` should mean
   "runtime-reachable risk" rather than "theoretical completeness not yet
   proven" — this decision, not a code change alone, is what would make
   Option C's narrower signal the one actually consumed.

## Scope confirmation

Not wired into any live path. Does not enable any horizon. Does not mark
anything `Supported`. Does not read `activation-readiness-risks.json`,
`evidence-log.json`, or any governance file. Does not modify
`SafetyVerificationOrchestrator`, any of the nine verifiers, or
`AllocationOrderCorrectnessVerifier`. Implements no activation workflow,
approval process, or configuration mechanism.
