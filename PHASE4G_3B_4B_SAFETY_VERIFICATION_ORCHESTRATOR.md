# Phase 4G.3B.4b — Standalone Safety Verification Orchestrator

## 1. Purpose and non-goals

A single, dark, standalone composition layer (`SafetyVerificationOrchestrator`)
that invokes all nine canonical safety verifiers against one already-produced
typed context, preserves every typed result, and aggregates normalized
outcomes/findings. Composition and aggregation ONLY.

Non-goals: no new safety calculation; no materialization/allocation/binding/
policy/condition resolution; no support registry; no live wiring into
`CatalogPreviewGenerator`/`PlanServices`/any controller/DI/startup; no
verifier logic modification; no new horizon; no governance-risk closure.

## 2. `SafetyVerificationContext` surface

```csharp
internal sealed record SafetyVerificationContext(
    PhaseAllocationResult Allocation,
    CatalogWorkoutProgressionDefinition Progression,
    IReadOnlyList<string> WeeklySlotRoles,
    IReadOnlyList<RuntimeConditionResolutionResult> RuntimeConditions,
    DatedGeneratedCatalogPlanSkeleton DatedSchedule,
    BoundCatalogPlan BoundPlan,
    CatalogWorkoutProgressionStage GoalPaceStage,
    IReadOnlySet<string> RegisteredGoalFeasibilityValues,
    CatalogWeeklyVolumePlan VolumePlan,
    VolumeSafetyPolicy Policy,
    CatalogLongRunProgression LongRunPlan,
    DateOnly RaceDate);
```

Immutable record. No service object, DbContext, IServiceProvider, controller/
request DTO, filesystem path, catalog root, clock, environment/configuration
object, governance document representation, or lazy factory. Every field
already exists as-is in some verifier's exact current signature (see §3).

## 3. Input-to-verifier dependency table

| Field | Consumed by |
|---|---|
| `Allocation` | PhaseConstraint, RaceSpecificCapacity, StageReachability, WorkoutExposure, GoalPaceReachability, ReadinessEligibility |
| `Progression` | RaceSpecificCapacity, StageReachability, WorkoutExposure, GoalPaceReachability |
| `WeeklySlotRoles` | RaceSpecificCapacity, StageReachability, WorkoutExposure, GoalPaceReachability |
| `RuntimeConditions` | StageReachability |
| `DatedSchedule` | WorkoutExposure, RaceDateAlignment |
| `BoundPlan` | WorkoutExposure |
| `GoalPaceStage` | GoalPaceReachability |
| `RegisteredGoalFeasibilityValues` | GoalPaceReachability |
| `VolumePlan` | VolumeProgression, LongRunProgression |
| `Policy` | VolumeProgression, LongRunProgression |
| `LongRunPlan` | LongRunProgression |
| `RaceDate` | RaceDateAlignment |

## 4. Signature audit (verified from source, not inherited)

| Verifier | Exact `Verify` signature | Result type | Outcome enum | Finding type |
|---|---|---|---|---|
| PhaseConstraintVerifier | `Verify(PhaseAllocationResult allocation)` | `PhaseConstraintVerificationResult` | `Pass, Fail, NotApplicable` | `IReadOnlyList<string>` |
| RaceSpecificCapacityVerifier | `Verify(PhaseAllocationResult, CatalogWorkoutProgressionDefinition, IReadOnlyList<string> weeklySlotRoles)` | `RaceSpecificCapacityVerificationResult` | `Pass, Fail, DecisionRequired, NotApplicable` | `RaceSpecificCapacityFinding` (typed `Code` enum) |
| StageReachabilityVerifier | `Verify(PhaseAllocationResult, CatalogWorkoutProgressionDefinition, IReadOnlyList<RuntimeConditionResolutionResult>, IReadOnlyList<string> weeklySlotRoles)` | `StageReachabilityVerificationResult` | `Pass, Fail, DecisionRequired, NotApplicable` | `StageReachabilityFinding` (typed `Code` enum) |
| WorkoutExposureVerifier | `Verify(PhaseAllocationResult, DatedGeneratedCatalogPlanSkeleton, BoundCatalogPlan, CatalogWorkoutProgressionDefinition, IReadOnlyList<string> expectedWeeklySlotRoles)` | `WorkoutExposureVerificationResult` | `Pass, Fail, DecisionRequired, NotApplicable` | `WorkoutExposureFinding` (typed `Code` enum) |
| GoalPaceReachabilityVerifier | `Verify(PhaseAllocationResult, CatalogWorkoutProgressionDefinition, IReadOnlyList<string> weeklySlotRoles, CatalogWorkoutProgressionStage goalPaceStage, IReadOnlySet<string> registeredGoalFeasibilityValues)` | `GoalPaceReachabilityVerificationResult` | `Pass, PassWithOpenRisk, Fail, NotApplicable` | `Findings` always empty; real content in `OutcomeChecks: IReadOnlyList<GoalPaceOutcomeCheck>` (typed `Status` enum) |
| ReadinessEligibilityVerifier | `Verify(PhaseAllocationResult allocation)` | `ReadinessEligibilityVerificationResult` | `Pass, DecisionRequired, NotApplicable` (no `Fail`) | `IReadOnlyList<string>` |
| VolumeProgressionVerifier | `Verify(CatalogWeeklyVolumePlan, VolumeSafetyPolicy)` | `VolumeProgressionVerificationResult` | `Pass, Fail, NotApplicable` | `IReadOnlyList<string>` |
| LongRunProgressionVerifier | `Verify(CatalogWeeklyVolumePlan, CatalogLongRunProgression, VolumeSafetyPolicy)` | `LongRunProgressionVerificationResult` | `Pass, Fail, NotApplicable` | `IReadOnlyList<string>` |
| RaceDateAlignmentVerifier | `Verify(DatedGeneratedCatalogPlanSkeleton datedSchedule, DateOnly raceDate)` | `RaceDateAlignmentVerificationResult` | `Pass, Fail, NotApplicable` | `IReadOnlyList<string>` |

## 5. Orchestrator/result surface

`SafetyVerificationOverallOutcome`, `CanonicalSafetyVerifier`,
`SafetyVerificationFinding`, `SafetyVerifierRunSummary`,
`SafetyVerificationPipelineResult`, `SafetyVerificationOrchestrator.Run` —
exactly the surface specified by the task, using real repository type names
throughout (see `SafetyVerificationOrchestrator.cs`). No `object RawResult`;
every raw result is strongly typed on `SafetyVerificationPipelineResult`.

## 6. Canonical execution order

1. PhaseConstraintVerifier
2. RaceSpecificCapacityVerifier
3. StageReachabilityVerifier
4. WorkoutExposureVerifier
5. GoalPaceReachabilityVerifier
6. ReadinessEligibilityVerifier
7. VolumeProgressionVerifier
8. LongRunProgressionVerifier
9. RaceDateAlignmentVerifier

All nine run exactly once, unconditionally, every call. No short-circuit.
Unexpected exceptions from a verifier are never caught/reinterpreted.

## 7. `AllocationOrderCorrectnessVerifier` exclusion

Confirmed against `PHASE4G_3B_3_SAFETY_VERIFICATION_PIPELINE_PLANNING.md`'s
own framing decision: it predates and sits outside the canonical nine — a
narrower question (does a specific target week count's allocation *order*
depend on the still-open `TD-ALLOCATION-PRIORITY-001` decision) distinct
from whether the *supplied* allocation and its generated artifacts satisfy
the nine post-allocation safety checks. Not invoked, not referenced (source-
verified: `SafetyVerificationOrchestrator.cs` contains no
`AllocationOrderCorrectnessVerifier.Verify(`/`new AllocationOrderCorrectnessVerifier(`),
not modified. An overall Pass for 9/10/11/13 weeks does **not** resolve
`TD-ALLOCATION-PRIORITY-001` — a later support-decision layer must combine
allocation-order correctness, this pipeline's result, support-registry
state, and open activation governance decisions.

## 8. Outcome-normalization table

| Verifier enum value | Normalized tier | Reasoning |
|---|---|---|
| `*.Pass` (all nine) | Pass | Clean verifier pass |
| `*.Fail` (all except ReadinessEligibility, which has no Fail) | Fail | Structural/safety failure |
| `RaceSpecificCapacity/StageReachability/WorkoutExposure.DecisionRequired` | DecisionRequired | Unresolved governance/runtime decision (WorkoutExposure's is declared but never produced by current logic — mapped exhaustively anyway) |
| `GoalPaceReachability.PassWithOpenRisk` | DecisionRequired | Open, not-yet-product-approved TD-NOTEVALUATED-FALLBACK-001 gap — explicit mapping principle, never Pass |
| `ReadinessEligibility.DecisionRequired` | DecisionRequired | TD-FOUNDATION-COMPRESSION-001 boundary |
| `*.NotApplicable` (all nine) | NotApplicable (per-summary tier only — see §8a) | See below |

**8a. Root vs. non-root NotApplicable.** Five verifiers' *only* NotApplicable
trigger is `!Allocation.IsMathematicallyFeasible` (PhaseConstraint,
RaceSpecificCapacity, StageReachability, WorkoutExposure,
ReadinessEligibility — root-only). Four verifiers have a NotApplicable
trigger structurally independent of the root allocation flag:
GoalPaceReachability ("unexpected stage shape" — the supplied
`GoalPaceStage` doesn't match the expected shape); VolumeProgression
(`volumePlan.Weeks.Count < 2`); LongRunProgression
(`longRunPlan.Weeks.Count == 0`); RaceDateAlignment (empty dated schedule).
Each per-verifier summary honestly preserves its own true NotApplicable
tier. Overall aggregation (§9) never uses these per-summary values directly
to decide NotApplicable — only the root allocation flag does.

## 9. Root-allocation NotApplicable behavior and aggregation precedence

```
if (!context.Allocation.IsMathematicallyFeasible)
    OverallOutcome = NotApplicable;   // regardless of any per-verifier tier
else
    OverallOutcome = Fail > DecisionRequired > Pass among the nine tiers,
        where any per-verifier NotApplicable (only possible here via a
        non-root trigger, §8a) is escalated to Fail for aggregation
        purposes only — the per-verifier summary itself is unchanged.
```

All nine typed results/summaries are always returned, in both branches —
proven executably by test I (root infeasibility) and test J (non-root
NotApplicable, GoalPaceReachability unexpected-stage-shape case, which
escalates to overall Fail, never silently to NotApplicable, while the root
allocation is feasible).

## 10. Finding aggregation and source attribution

Four verifiers (RaceSpecificCapacity, StageReachability, WorkoutExposure,
GoalPaceReachability) expose an already-typed code, used directly (never
re-derived from message text). Five verifiers (PhaseConstraint,
ReadinessEligibility, VolumeProgression, LongRunProgression,
RaceDateAlignment) expose plain strings only, each consistently prefixed
with an `UPPER_SNAKE_CASE: ` code token (verified by direct inspection of
every finding call site in all five source files); the exact string is
preserved verbatim as `Message`, the leading token extracted as `Code`.
`SourceVerifier` is always set structurally (`CanonicalSafetyVerifier`
enum), not only via message-string modification. No deduplication —
identical-looking findings from different phases/checks are distinct typed
source findings and are all preserved (see §14 for the measured
4×`ExactStageReachabilityFit` case).

## 11. Composition-only / no-materialization proof

`Run` calls only the nine verifiers' own public `Verify` methods plus pure
LINQ/adapter code. Source-verified (test N): the file contains none of
`new CatalogPhaseAllocationResolver(`, `new GenericPhaseAllocator(`,
`new CatalogStageToWeekMaterializer(`, `new ProgressionStageAllocator(`,
`new CatalogWeekSkeletonCalendarMaterializer(`, `new CatalogWorkoutBinder(`,
`new CatalogVolumeAndLongRunPlanner(`, any catalog loader constructor, or
any runtime-condition resolver constructor.

## 12–13. Real primary-path and fallback-path 8–14 matrix (measured, not assumed)

Both paths produced **identical** tier patterns for every target 8–14 —
GoalPaceReachabilityVerifier enumerates every registered
`GOAL_FEASIBILITY_IN` value itself and does not consume
`context.RuntimeConditions` at all, so its result (and the overall outcome)
is invariant to which single goal value the scheduling context used.

| Weeks | PhaseConstraint | RaceSpecificCapacity | StageReachability | WorkoutExposure | GoalPaceReachability | ReadinessEligibility | VolumeProgression | LongRunProgression | RaceDateAlignment | Overall |
|---|---|---|---|---|---|---|---|---|---|---|
| 8 | Pass | Pass (1 finding: ExactFitZeroWorstCaseSlack) | Pass | Pass | DecisionRequired (PassWithOpenRisk) | Pass | Pass | Pass | Pass | **DecisionRequired** |
| 9 | Pass | Pass | Pass | Pass | DecisionRequired | Pass | Pass | Pass | Pass | **DecisionRequired** |
| 10 | Pass | Pass | Pass | Pass | DecisionRequired | Pass | Pass | Pass | Pass | **DecisionRequired** |
| 11 | Pass | Pass | Pass | Pass | DecisionRequired | Pass | Pass | Pass | Pass | **DecisionRequired** |
| 12 | Pass | Pass | Pass | Pass | DecisionRequired | Pass | Pass | Pass | Pass | **DecisionRequired** |
| 13 | Pass | Pass | Pass | Pass | DecisionRequired | Pass | Pass | Pass | Pass | **DecisionRequired** |
| 14 | Pass | Pass | Pass | Pass | DecisionRequired | Pass | Pass | Pass | Pass | **DecisionRequired** |

## 14. Full per-verifier breakdown (12-week pilot, representative)

`AggregatedFindings.Count == 10`: 4×`ExactStageReachabilityFit` (one per
phase), 1×`ExactWorkoutExposureMatch`, 5×GoalPaceReachability outcome
checks (`Eligible`×2, `FallbackConfirmed`×2, `UncertainNotEvaluated`×1).
None from RaceSpecificCapacity/PhaseConstraint/ReadinessEligibility/
VolumeProgression/LongRunProgression/RaceDateAlignment at 12 weeks.

## 15. Targets with Pass that remain activation-blocked outside the nine

**No real 8–14 week target achieves a clean Pass, and this is structural,
not incidental.** `GoalPaceReachabilityVerifier.Verify` unconditionally
appends exactly one synthetic NotEvaluated check on every call
(`CheckNotEvaluated`, never behind a condition), and `CheckNotEvaluated`'s
own logic can only ever produce `GoalPaceOutcomeStatus.StructurallyUnreachable`
or `UncertainNotEvaluated` — never `Eligible`/`FallbackConfirmed`. Since
`GoalPaceReachabilityOutcome.Pass` requires the absence of both statuses
among *all* checks, and the always-present NotEvaluated check is guaranteed
to be one of exactly those two, `Pass` is unreachable for any
mathematically-feasible allocation using the real `GOAL_PACE_REHEARSAL`
stage — confirming that verifier's own doc comment ("Not achievable today").
Every 8–14 target that achieves `OverallOutcome == DecisionRequired` here
(all of them) is activation-blocked at minimum by `TD-NOTEVALUATED-FALLBACK-001`,
in addition to whatever `AllocationOrderCorrectnessVerifier`/support-registry
review a future phase requires for non-12-week horizons.

## 16. Open TDs surfaced by existing verifier results (not read from governance files at runtime)

`TD-NOTEVALUATED-FALLBACK-001` (GoalPaceReachabilityVerifier, every target).
`TD-FOUNDATION-COMPRESSION-001` (ReadinessEligibilityVerifier — only when a
phase is allocated below its catalog minimum; not observed for any real
8–14 week target today, but the finding text cites it whenever it fires —
proven via a mutated-context test, §21). Both citations are literal text
already present inside the real, unmodified verifiers' own finding
messages — the orchestrator never opens, parses, or queries any governance
file to determine this.

## 17. What remains before activation

- `AllocationOrderCorrectnessVerifier` evaluation (separate, not combined here).
- Support registry (not started).
- Explicit support decision per horizon.
- Resolution of applicable open TDs (`TD-NOTEVALUATED-FALLBACK-001`,
  `TD-FOUNDATION-COMPRESSION-001` when triggered, `TD-ALLOCATION-PRIORITY-001`).
- Live wiring (not started — this orchestrator remains dark).
- Persistence/public-contract review.
- Activation tests.

## 18. Explicit statement

**`OverallOutcome == Pass` does not mean public support is enabled.** It
means only that the nine canonical safety checks passed for the supplied
context — not publicly supported, not product-approved, not
allocation-priority approved, not enabled in a support registry, not safe
to persist, not ready for activation.

## 19. Reachability proof (dark)

Zero production call sites: grep across all of `RunningApp.Application` and
`RunningApp.Api` (excluding `SafetyVerificationOrchestrator.cs` itself)
found no `SafetyVerificationOrchestrator.Run(`. No DI registration:
`Program.cs` contains no mention of `SafetyVerificationOrchestrator`. Tests
are the only caller.

## 20. Confirmation no verifier logic changed

None of the nine verifier implementation files, nor
`AllocationOrderCorrectnessVerifier.cs`, was modified in this phase
(hash-verified byte-identical before/after — see final report §23).
