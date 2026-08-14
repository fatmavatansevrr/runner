# Phase 4G.6A.3 — Generic Deterministic Constrained Proportional Preparation Runway Allocator (Dark Only)

Scope: `TEN_K__4D__INTERMEDIATE v10` only. Dark-only implementation and test pass. No workout generation, no dated weeks, no persistence, no public behavior change, no 15-20 week activation.

## 1. Executive Result

**`GENERIC_DETERMINISTIC_CONSTRAINED_PROPORTIONAL_PREPARATION_RUNWAY_ALLOCATOR_IMPLEMENTED_DARK_ONLY_AND_PROVES_APPROVED_TEN_K_TARGET_MATRICES_WITHOUT_CATALOG_OR_RUNTIME_ACTIVATION`**

A generic, deterministic, constrained-proportional allocator is implemented, entirely inside the test project (`RunningApp.IntegrationTests`), reproducing both Phase 4G.6A.2/4G.6A.2A-approved target matrices from shared policy metadata and generic mechanics only — no `RunwayWeeks`-specific route table exists anywhere in the engine.

## 2. Phase Scope

Candidate `TEN_K__4D__INTERMEDIATE v10`, `PreferredCoreWeeks=12`, direct Preparation Runway 3-8 full weeks, profiles `CONSISTENCY_NEEDED`/`CORE_ENTRY_READY`, blocks `CONSISTENCY`/`GENERAL_ENDURANCE`/`AEROBIC_STRENGTH`/`PRE_SPECIFIC_TRANSITION`. No numeric conclusion here generalizes beyond this candidate.

## 3. Inherited Approved Policy

The exact metadata table from this phase's own prompt (Min/Max/PreferredWeight/AllocationPriority/CanonicalOrder per block, per profile) was taken as given and implemented without modification. `AerobicStrengthSecondWeekThreshold=RunwayWeeks>=5` (Phase 4G.6A.2A) and both target matrices (Phase 4G.6A.2/4G.6A.2A) were treated as the required proof target, not as inputs to be re-derived from first principles.

## 4. Generic Allocator Contract

Implemented in `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/PreparationRunway/PreparationRunwayBlockAllocator.cs`:

- `PreparationRunwayBlockAllocationPolicy<TKey>` — generic input: `BlockKey, IsEligible, MinWeeks, MaxWeeks, PreferredWeight, AllocationPriority, CanonicalOrder, IsExpandable`.
- `PreparationRunwayBlockAllocationOutcome<TKey>` — `BlockKey, AllocatedWeeks, CanonicalOrder`.
- `PreparationRunwayAllocationEngineResult<TKey>` — success/failure result; failure never carries a partial allocation (`Allocations` is `null` on failure).
- `PreparationRunwayAllocationFailureCode` — the six codes named in the prompt, verbatim.
- `PreparationRunwayBlockAllocator.Allocate<TKey>(int runwayWeeks, IReadOnlyList<PreparationRunwayBlockAllocationPolicy<TKey>> policies)` — the engine itself, generic over any `TKey : notnull`.

It knows nothing about TEN_K, calendar dates, workout IDs, Race Core phases, or persistence — verified by direct inspection (the file contains no such symbol) and by the genericity test (section 15).

**Existing typed contracts reused, not duplicated**: `PreparationRunwayBlockType` (the real, production enum: `Consistency, GeneralEndurance, AerobicStrength, PreSpecificTransition`) is used directly as `TKey` for the TEN_K instantiation — no parallel block-key vocabulary was invented. `PreparationRunwayAllocation`/`PreparationRunwayBlockAllocation`/`PreparationRunwayPrescriptionProfile` (the existing dark contract types from Phase 4G.4B) are proven compatible with this engine's output via a dedicated projection test (section 4/16 below) rather than reimplemented.

## 5. Algorithm Definition

Implements the prompt's 11-step sequence exactly, with one explicit, documented generalization of Step 3's "mandatory minima," described in full in section 7.

## 6. Eligibility Handling

`Allocate` partitions `policies` into `eligible` at the start; ineligible blocks are initialized to `0` and never appear in `eligible`, so they never contribute to minima, normalization, largest-remainder distribution, or overflow redistribution. Verified by test #11 (`IneligibleBlock_ReceivesZero_NeverParticipatesInMinimaOrNormalization`) and by both target-matrix suites (`AEROBIC_STRENGTH` is asserted `0` throughout `CONSISTENCY_NEEDED`; `CONSISTENCY` is asserted `0` throughout `CORE_ENTRY_READY`).

## 7. Minimum Allocation

Step 3 is implemented as: `EffectiveMinimum = MinWeeks` for non-expandable (fixed) blocks, and `EffectiveMinimum = max(MinWeeks, min(1, MaxWeeks))` for expandable blocks with `PreferredWeight > 0`. This is an explicit, documented generalization beyond a literal reading of "allocate MinWeeks" — required because `AEROBIC_STRENGTH`'s own approved policy declares `MinWeeks=0` yet the approved target matrix requires it present (`=1`) at `RunwayWeeks=3`, the smallest horizon in the direct range. The rule states: an expandable block with positive weight cannot be meaningfully "proportionally represented" below a whole week, so it receives a one-week introductory presence in addition to (never instead of) any larger declared minimum. This is the exact mechanism Phase 4G.6A.2's own coefficient simulation already used and had verified correct; this phase formalizes it into the reusable engine rather than inventing new behavior. It never reduces or truncates a declared minimum (`max(...)` only raises it), and the final invariant check (Step 11) still validates every allocation against the *declared* `MinWeeks`/`MaxWeeks`, which this rule can only satisfy more easily, never violate.

## 8. Weight Normalization

Each loop iteration recomputes `open` (eligible, expandable, `PreferredWeight>0`, `allocated<MaxWeeks`) and normalizes `PreferredWeight` only over that currently-open set — ineligible, fixed, saturated, and zero-weight blocks are excluded by construction (they can never appear in `open`). Verified by test #10 (zero-weight fixed block) and test #16 (a zero-weight *expandable* block that contributes to the Step-4 maxima check but never enters `open`).

## 9. Floor and Largest Remainder

`IdealAdditionalWeeks = remaining * NormalizedWeight`, floored per block, remainder distributed one week at a time to `Take(leftover)` blocks ordered by the four-level tie-break in section 11. Verified by tests #3 (exact floor, no remainder) and #4 (non-trivial remainder).

## 10. Capacity Redistribution

Floor-plus-remainder shares are applied capped at each block's `MaxWeeks`; any cap-induced overflow is never separately tracked as a number — it is implicitly re-detected by recomputing `remaining = runwayWeeks - sum(allocated)` after applying the round, and the loop continues (recomputing `open`, excluding now-saturated blocks) until `remaining == 0` or no `open` blocks remain. Bounded by a `policies.Count + 2` iteration guard (provably sufficient: each round either finishes or saturates at least one block, so at most `N` rounds can occur for `N` blocks). Verified by tests #8 (saturated block excluded from later rounds) and #9 (overflow never lost, fully redistributed across two rounds).

## 11. Tie-Break Semantics

Exactly the four-level order from the prompt: `FractionalRemainder` descending → `AllocationPriority` descending → `CanonicalOrder` ascending → `BlockKey.ToString()` ordinal ascending. Each level proven independently by tests #5, #6, #7 (constructing scenarios where only that level can decide the outcome).

## 12. Canonical Ordering

`PreparationRunwayBlockAllocationOutcome.CanonicalOrder` is carried through unchanged from the input policy and the final result is sorted by it (`Consistency=1, GeneralEndurance=2, AerobicStrength=3, PreSpecificTransition=4` for the TEN_K factory) — this governs only the *output ordering/chronological sequence*, never the distribution tie-break priority (`AllocationPriority` is a separate field, used only inside the largest-remainder/redistribution logic, exactly as the prompt requires: "Do not let AllocationPriority determine chronological order").

## 13. Typed Failures

`InvalidPolicy` (negative `RunwayWeeks`, invalid `Min/Max/Weight`, or a non-expandable block whose `Min != Max`), `DuplicateBlockKey`, `MinimumCapacityExceedsRunway`, `MaximumCapacityBelowRunway`, `NoExpandableCapacity`, `AllocationInvariantViolation` (defensive only — never observed to trigger in any passing test; reserved for the iteration-guard exhaustion case, which the algorithm's own termination proof shows cannot occur for valid input). Every failure result carries `Allocations = null` — no partial success is ever returned. Verified by tests #12-#16.

## 14. Target-Matrix Proof

`ConsistencyNeeded_MatrixIsReproducedByGenericMechanism` (6 cases, weeks 3-8) and `CoreEntryReady_MatrixIsReproducedByGenericMechanism` (6 cases, weeks 3-8) both pass, reproducing every cell of both approved matrices exactly. `CoreEntryReady_AerobicStrengthSecondWeek_EmergesExactlyAtRunwayFive` isolates and confirms the threshold-5 finding specifically.

## 15. Generic Synthetic Proof

`Genericity_ArbitrarySyntheticPolicyAndHorizon_ProducesAValidDeterministicAllocation` uses a private, test-local `SyntheticBlock { Alpha, Beta, Gamma, Delta }` enum, horizons 10-16 (never 3-8), and policy shapes unrelated to any of the four production block names or their approved weights — proving the engine's correctness does not depend on TEN_K-specific values.

## 16. Dark/Public/Persistence Classification

**Dark, entirely test-project-local.** `PreparationRunwayBlockAllocator.cs` and `PreparationRunwayBlockAllocatorTests.cs` live in `RunningApp.IntegrationTests`, not `RunningApp.Application`. They are not referenced by, and do not reference, any production runtime or DI composition. This placement is not incidental: the existing governance tests in `PreparationRunwayContractsTests.cs` (`Neutrality_NoAllocatorRouteMapperOrSessionSelectionExistsInRunwayProductionFolder`, which asserts exactly 2 `.cs` files exist in the production `Schedule/PreparationRunway` folder and forbids the literal substring `"PreparationRunwayAllocator"` inside them; `DarkReachability_NoDiRegistrationOrLiveInvocation`, which forbids that same symbol anywhere else in `RunningApp.Application/Api/Infrastructure/Persistence`) make any production-project placement of an allocator with this name structurally impossible without weakening an established guard — which this phase does not do. The engine does reuse the real, production `PreparationRunwayBlockType` enum and (in one proof test) the real `PreparationRunwayAllocation`/`PreparationRunwayBlockAllocation`/`PreparationRunwayAllocationValidator` types, exactly as the existing `PreparationRunwayContractsTests.cs` file already does — this is reading/consuming contract types from the test project, which the dark-reachability scans (scoped to production projects only) do not and were never intended to forbid.

## 17. Catalog Limitation

The allocator operates exclusively on block-week counts. It contains no workout key, no `eligiblePhases` reference, no catalog lookup, and no assumption that `AEROBIC_STRENGTH` catalog content exists. `TD-AEROBIC-STRENGTH-SEMANTICS-001`'s catalog-capacity gap (new workout definition + schema extension, deferred to Phase 4G.6A.4) is entirely unaffected by this phase: the allocator's ability to compute `AerobicStrength=2` weeks does not imply any workout can yet be bound to those weeks.

## 18. Open TDs

`TD-AEROBIC-STRENGTH-SEMANTICS-001` remains `OPEN`, unmodified in status by this phase (its own catalog-capacity portion is explicitly out of this phase's scope, per the prompt's own instruction not to close it "merely because the allocator can allocate block counts"). No new TD was created — this phase's own scope (a dark, fully-tested, fully-specified engine) introduced no new unresolved risk requiring one.

## 19. Follow-up Phase

Phase 4G.6A.4: author the deferred `AEROBIC_STRENGTH` workout definition(s) and the workout-definition schema's `eligiblePhases` extension (or equivalent runway-eligibility mechanism), per the exact contract specified in `PHASE4G_6A_2A_TEN_K_AEROBIC_STRENGTH_AND_TRANSITION_CAPACITY_DECISION.md` section 9. Only after that phase closes should any future phase consider wiring this (or an equivalent) allocator into a live or dark orchestrator seam.

## 20. Explicit Non-Implementation Statement

This phase implemented **no** production/DI-registered allocator, **no** workout definition, **no** workout-definition schema extension, **no** workout-progression mapping or binding, **no** volume/long-run progression, **no** pace resolution, **no** calendar assignment, **no** `TrainingWeek`/`TrainingDay` generation, **no** persistence or migration, **no** read-model change, **no** public preview/confirmation change, **no** 15-20 week activation, **no** 21-52 General Endurance staged-plan work, **no** 53+ behavior, and **no** intermediate-distance projection. Nothing was wired into any dark Preparation Runway orchestration seam — the allocator is exercised exclusively through its own direct unit/integration tests.
