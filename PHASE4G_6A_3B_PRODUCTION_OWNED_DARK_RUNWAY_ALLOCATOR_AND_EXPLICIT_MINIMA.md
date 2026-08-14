# Phase 4G.6A.3B — Production-Owned Dark Preparation Runway Allocator Extraction and Explicit Eligibility-Dependent Minimum Correction

Scope: `TEN_K__4D__INTERMEDIATE v10` only. Narrow correction/extraction pass. No workout content, no runtime activation, no public behavior change, no persistence.

## 1. Executive Result

**`PRODUCTION_OWNED_DARK_PREPARATION_RUNWAY_ALLOCATOR_EXTRACTED_WITH_EXPLICIT_ELIGIBILITY_DEPENDENT_MINIMA_AND_NO_HIDDEN_EFFECTIVE_MINIMUM_RULE`**

The allocator now lives in `RunningApp.Application` (production assembly), the test-project-owned reference implementation from Phase 4G.6A.3 was deleted (no duplicate remains), the implicit `max(declaredMinimum, 1)` heuristic was removed from the engine, and the TEN_K policy factory now declares each conditional block's true mandatory participation honestly (`MinWeeks=1` when eligible, not `0`). Both approved target matrices are reproduced exactly by the corrected engine + corrected policy, with no engine-level compensation required.

## 2. Problems Inherited from Phase 4G.6A.3

(1) The reference allocator existed only in `RunningApp.IntegrationTests`, not owned by any production assembly. (2) The engine contained an undocumented-as-policy, engine-level rule (`EffectiveMinimum = max(declared MinWeeks, min(1, MaxWeeks))` for expandable positive-weight blocks) that silently converted `AEROBIC_STRENGTH`'s and `CONSISTENCY`'s declared `MinWeeks=0` into an effective `1` — mandatory participation was being inferred from `IsExpandable`/`PreferredWeight`, not expressed by policy metadata.

## 3. Production Ownership Decision

New folder: `backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayEngine/` (sibling to the existing `PreparationRunway`, `Materialization`, `Progression`, `Binding`, `Horizon` folders, matching this codebase's established one-folder-per-concern convention). Contains `PreparationRunwayBlockAllocationEngine.cs` (generic engine + typed contracts) and `TenKPreparationRunwayAllocationPolicyFactory.cs` (TEN_K-specific policy binding). Both classes are `internal`. No DI registration was added (a pure deterministic static function, per the task's own stated preference). No dependency on `RunningApp.IntegrationTests`, EF/persistence, the public API/DTO layer, or workout catalog content.

A genuine naming collision was found and corrected during implementation: the first attempt named the new namespace segment `PreparationRunwayAllocation`, which is *also* the name of an existing production record type (`PreparationRunway/PreparationRunwayContracts.cs`). Because C#'s unqualified-name lookup searches enclosing namespaces, this made the bare identifier `PreparationRunwayAllocation` ambiguous (namespace vs. type — CS0118) inside sibling files under the shared `...Schedule` namespace. The folder/namespace was renamed to `PreparationRunwayEngine` (matching its file/class names) to resolve this cleanly, with no effect on the algorithm or any type's own name.

## 4. Governance-Gate Finding

Inspected `PreparationRunwayContractsTests.cs` in full before writing any code. Two distinct gates exist, each with a narrower purpose than "no allocator anywhere in production":

- **`Neutrality_NoAllocatorRouteMapperOrSessionSelectionExistsInRunwayProductionFolder`**: asserts exactly 2 `.cs` files exist in the literal `Schedule/PreparationRunway` folder, and forbids a fixed substring list (including the literal token `"PreparationRunwayAllocator"`) inside those 2 files' content. **Purpose**: keep that one specific folder — the original Phase 4G.4B "dark contracts" boundary — pure (typed contracts + validators only, nothing behavioral). It says nothing about any other folder.
- **`DarkReachability_NoProductionContractConsumerOutsideContractFolder`** / **`DarkReachability_NoDiRegistrationOrLiveInvocation`**: scan every production `.cs` file *outside* `Schedule/PreparationRunway` (the exclusion is a literal path check, `!path.Contains(allowedRoot)` where `allowedRoot = "Schedule\PreparationRunway"`) for two fixed lists: seven concrete contract *type* names (`PreparationRunwayContext`, `PreparationRunwayAllocation`, `PreparationRunwayPlanningResult`, `PreparationRunwayBlockAllocation`, `PreparationRunwayLeadingPartialSpan`, `PreparationNeedProfile`, `RacePlanCompositionMetadata`) and eight concrete *behavior* symbols (`PreparationRunwayPlanningResultValidator`, `PreparationRunwayAllocationValidator`, `PreparationRunwayContextValidator`, `PreparationRunwayPlanner`, `PreparationRunwayAllocator`, `PreparationRunwayMaterializer`, `PreparationRunwayComposer`, `IPreparationRunwayAllocator`). **Purpose**: prevent silent, undocumented consumption or invocation of the *specific* dark contract types/validators from outside their folder — i.e., prevent premature/accidental live wiring of the *existing* contracts.

**Finding: neither gate needed to change.** The new engine (i) does not add a third file to `Schedule/PreparationRunway` (that folder is untouched, still exactly 2 files), and (ii) is named, and its types are named, such that none of the literal forbidden tokens appear (`PreparationRunwayBlockAllocationEngine` and `TenKPreparationRunwayAllocationPolicyFactory` do not match `PreparationRunwayAllocator`, `PreparationRunwayPlanner`, `PreparationRunwayMaterializer`, `PreparationRunwayComposer`, or `IPreparationRunwayAllocator` as whole-word substrings — confirmed by direct regex trace, not assumption). This is not a rename-to-bypass: the class is genuinely named for what it is (a generic allocation *engine*, not literally "the allocator" the old gate's authors were guarding against at a time when no real allocator existed anywhere), it defines its own new result/policy types rather than reusing the seven forbidden concrete contract types, and it lives in a folder the existing gate's own path-exclusion logic was never scoped to examine. **No gate text was modified.** Both gates were re-run unchanged after extraction and both still pass (section 12).

A new, additional, narrowly-scoped governance test file (`PreparationRunwayAllocationDarkGovernanceTests.cs`) was added to establish the same category of protection for the *new* folder going forward — proving production ownership, no DI registration, no live invocation, and that no duplicate test-project engine remains. This mirrors the old gate's pattern rather than replacing it.

## 5. Explicit Eligibility-Dependent Minima Decision

`TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies` now declares, per the task's own approved metadata: `CONSISTENCY.MinWeeks = 1` when eligible under `CONSISTENCY_NEEDED` (was `0`); `AEROBIC_STRENGTH.MinWeeks = 1` when eligible under `CORE_ENTRY_READY` (was `0`). Both remain `MinWeeks = 0` (and `IsEligible = false`) in the profile where they don't apply. `GENERAL_ENDURANCE` (`MinWeeks=1`, always eligible) and `PRE_SPECIFIC_TRANSITION` (`MinWeeks=1=MaxWeeks`, fixed) were already correct and are unchanged.

## 6. Removed Effective-Minimum Behavior

`PreparationRunwayBlockAllocationEngine.Allocate` no longer contains any `EffectiveMinimum`/baseline computation. Step 3 is now exactly: `allocated[block] = block.IsEligible ? block.MinWeeks : 0` — the declared value, unconditionally. No generic rule reads `IsExpandable` or `PreferredWeight` when computing minima; those fields are consulted only later, inside the expansion loop (Step 6 onward), exactly as the algorithm's own Step 6 already specified. Verified both structurally (the removed expression `Math.Max(p.MinWeeks, Math.Min(1, ...))` does not appear anywhere in the new file) and behaviorally (`EligibleExpandablePositiveWeightMinZeroBlock_MayValidlyRemainZero_WhenAnotherBlockWinsAllocation` and `NoHiddenEffectiveMinimumHeuristicRemainsAnywhereInProductionOrTestCode`, both passing — the latter constructs a scenario that would fail under the removed heuristic and passes under the corrected one).

## 7. Final Production Contracts

`PreparationRunwayBlockAllocationPolicy<TKey>` (`BlockKey, IsEligible, MinWeeks, MaxWeeks, PreferredWeight, AllocationPriority, CanonicalOrder, IsExpandable`), `PreparationRunwayBlockAllocationOutcome<TKey>` (`BlockKey, AllocatedWeeks, CanonicalOrder`), `PreparationRunwayAllocationEngineResult<TKey>` (success/failure, failure never carries `Allocations`), `PreparationRunwayAllocationFailureCode` (six values, unchanged from Phase 4G.6A.3), `PreparationRunwayAllocationProfile` (`ConsistencyNeeded`, `CoreEntryReady`) — all `internal`, all in the new `PreparationRunwayEngine` folder. `TenKPreparationRunwayAllocationPolicyFactory` reuses the real, existing `PreparationRunwayBlockType` enum (not duplicated) from the `PreparationRunway` contracts folder.

## 8. Preserved Algorithm

The 11-step sequence, tie-break order (fractional remainder desc → `AllocationPriority` desc → `CanonicalOrder` asc → key ordinal asc), and chronological output ordering (`CanonicalOrder` ascending, independent of `AllocationPriority`) are all unchanged from Phase 4G.6A.3 — only Step 3's minima computation was corrected (section 6). `OutputOrdering_FollowsCanonicalOrderAscending_NotAllocationPriority` explicitly re-proves the ordering/priority separation with a policy set whose two orderings are deliberately opposite.

## 9. Profile Policy Metadata

| Block | Profile | Eligible | MinWeeks | MaxWeeks | Weight | Priority | Order |
|---|---|---|---|---|---|---|---|
| Consistency | ConsistencyNeeded | true | **1** | 2 | 0.40 | 3 | 1 |
| Consistency | CoreEntryReady | false | 0 | 2 | 0.40 | 3 | 1 |
| GeneralEndurance | ConsistencyNeeded | true | 1 | 5 | 0.60 | 4 | 2 |
| GeneralEndurance | CoreEntryReady | true | 1 | 5 | 0.65 | 4 | 2 |
| AerobicStrength | ConsistencyNeeded | false | 0 | 2 | 0.35 | 2 | 3 |
| AerobicStrength | CoreEntryReady | true | **1** | 2 | 0.35 | 2 | 3 |
| PreSpecificTransition | both | true | 1 | 1 | 0 | 1 | 4 |

(Bold = corrected in this phase.) The factory does not inspect raw recent-running evidence, `RunningBackground`, `RaceDate`, `StartDate`, or `CoreEntryReadiness` inputs, and does not duplicate `CoreEntryReadinessResolver`.

## 10. Target-Matrix Proof

Both matrices reproduced exactly by the corrected engine + corrected policy (weeks 3-8):

`CONSISTENCY_NEEDED`: `1/1/0/1, 1/2/0/1, 2/2/0/1, 2/3/0/1, 2/4/0/1, 2/5/0/1` (Consistency/GeneralEndurance/AerobicStrength/Transition).
`CORE_ENTRY_READY`: `0/1/1/1, 0/2/1/1, 0/2/2/1, 0/3/2/1, 0/4/2/1, 0/5/2/1`.

Threshold 5 re-confirmed to emerge without any horizon-specific branch (`NoRunwayWeeksSpecificRouteTableExistsInTheProductionEngineSource`, a structural source scan of the production file, passing).

## 11. Generic Min=0 Regression Proof

`EligibleExpandablePositiveWeightMinZeroBlock_MayValidlyRemainZero_WhenAnotherBlockWinsAllocation` uses a synthetic `Alpha` (eligible, expandable, weight 0.01, `MinWeeks=0`) against `Beta` (weight 0.99) at `RunwayWeeks=1` — Alpha receives exactly `0`, proving the engine no longer forces a one-week floor on any block regardless of its declared minimum.

## 12. Dark/Public/Persistence Classification

**`PRODUCTION_OWNED`** (RunningApp.Application). **`DARK_UNWIRED`** (no DI registration; `DarkReachability_NoDiRegistrationOrLiveInvocationOfTheNewEngine` scans all production sources outside the new folder and finds zero references; `DarkReachability_AdversarialWiringWouldBeDetected` proves the scan itself is not vacuous). **`PUBLICLY_UNREACHABLE`** (no controller, DTO, or preview-generation code path references it). **`PERSISTENCE_UNREACHABLE`** (no EF/persistence dependency exists in either new file). Not classified as runtime-active. The pre-existing `PreparationRunway` contracts-folder gates were re-run unchanged and still pass (95/95 in the combined focused run, section-15 detail).

## 13. Catalog Limitation

Unchanged from Phase 4G.6A.3: the engine operates on block-week counts only, with no workout key, `eligiblePhases` reference, or catalog lookup anywhere in either new file.

## 14. Open TDs

`TD-AEROBIC-STRENGTH-SEMANTICS-001` status **unchanged, remains `OPEN`** — not touched by this phase. No new TD was created; this phase's own findings (a naming collision, a policy under-specification) were corrected within the phase itself, not left as open risks.

## 15. Exact Follow-up Phase

Unchanged: Phase 4G.6A.4 — author the deferred `AEROBIC_STRENGTH` workout definition(s) and the workout-definition schema's `eligiblePhases` extension, per `PHASE4G_6A_2A_...md` section 9.

## 16. Explicit Non-Implementation Statement

This phase implemented **no** `AerobicStrength` workout definition, **no** `eligiblePhases` schema extension, **no** workout progression mapping or binding, **no** volume/long-run progression, **no** pace resolution, **no** dated week/`TrainingWeek`/`TrainingDay` generation, **no** calendar assignment, **no** persistence or migration, **no** preview/confirmation response change, **no** 15-20 week runtime activation, **no** General Endurance staged-plan work, **no** 21-52 or 53+ behavior, **no** intermediate-distance projection, and **no** production deployment/configuration change. The allocator remains dark: no DI registration, no orchestrator invocation, exercised exclusively through its own direct unit tests.
